using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using BookStore.Api.Contracts;
using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using BookStore.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Controllers;

[ApiController]
[Authorize(Roles = "Customer")]
[Route("api/orders")]
public sealed class OrdersController(BookStoreDbContext db, BookPricingService pricing) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetMyOrders(CancellationToken cancellationToken)
    {
        var customerId = User.FindFirstValue("customer_id");
        if (customerId is null)
        {
            return Forbid();
        }

        var orders = await db.Orders.AsNoTracking()
            .Where(order => order.CustomerId == customerId)
            .Include(order => order.Lines).ThenInclude(line => line.Book)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(orders.Select(ToResponse).ToArray());
    }

    [HttpPost]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Checkout(CheckoutRequest request, CancellationToken cancellationToken)
    {
        var customerId = User.FindFirstValue("customer_id");
        if (customerId is null)
        {
            return Forbid();
        }

        var paymentMethod = request.PaymentMethod.Trim();
        if (paymentMethod is not ("CashOnDelivery" or "BankTransfer"))
        {
            return BadRequest(new { message = "Phương thức thanh toán hiện hỗ trợ COD hoặc chuyển khoản mô phỏng." });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var cart = await db.CartItems
            .Where(item => item.CustomerId == customerId)
            .Include(item => item.Book)
            .ToListAsync(cancellationToken);

        if (cart.Count == 0)
        {
            return BadRequest(new { message = "Giỏ hàng đang trống." });
        }

        var unavailable = cart.FirstOrDefault(item => item.Quantity > item.Book.StockQuantity);
        if (unavailable is not null)
        {
            return Conflict(new { message = $"Số lượng '{unavailable.Book.Title}' vừa thay đổi. Hãy cập nhật giỏ hàng." });
        }

        var discounts = await pricing.GetActiveDiscountsAsync(cart.Select(item => item.BookId).ToArray(), DateTime.UtcNow, cancellationToken);
        var lines = cart.Select(item =>
        {
            var unitPrice = BookPricingService.ApplyDiscount(item.Book.SalePrice, discounts.GetValueOrDefault(item.BookId));
            return new SalesOrderLine
            {
                BookId = item.BookId,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                LineTotal = unitPrice * item.Quantity
            };
        }).ToList();

        var subtotal = lines.Sum(line => line.LineTotal);
        var now = DateTime.UtcNow;
        var orderDiscountRate = await db.OrderPromotions.AsNoTracking()
            .Where(item => item.MinimumOrderAmount <= subtotal
                && item.Promotion.StartsAt <= now && item.Promotion.EndsAt >= now)
            .Select(item => (decimal?)item.DiscountPercent)
            .MaxAsync(cancellationToken) ?? 0m;
        var voucherCode = request.VoucherCode?.Trim().ToUpperInvariant();
        Voucher? voucher = null;
        decimal voucherDiscountRate = 0m;
        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            voucher = await db.Vouchers.Include(item => item.Promotions).ThenInclude(item => item.Promotion)
                .SingleOrDefaultAsync(item => item.Id == voucherCode, cancellationToken);
            if (voucher is null || voucher.Status != "Active"
                || (voucher.ExpiresAt.HasValue && voucher.ExpiresAt.Value < now)
                || (voucher.MaxUsageCount.HasValue && voucher.UsageCount >= voucher.MaxUsageCount.Value))
            {
                return BadRequest(new { message = "Voucher không tồn tại, đã hết hạn hoặc đã hết lượt sử dụng." });
            }

            voucherDiscountRate = voucher.Promotions
                .Where(item => item.Promotion.StartsAt <= now && item.Promotion.EndsAt >= now)
                .Select(item => item.DiscountPercent)
                .DefaultIfEmpty(0m)
                .Max();
            if (voucherDiscountRate <= 0m)
            {
                return BadRequest(new { message = "Voucher hiện không áp dụng được." });
            }
        }

        var orderDiscountRateApplied = Math.Max(orderDiscountRate, voucherDiscountRate);
        var orderDiscount = decimal.Round(subtotal * orderDiscountRateApplied / 100m, 2, MidpointRounding.AwayFromZero);
        var itemDiscount = cart.Sum(item => (item.Book.SalePrice - BookPricingService.ApplyDiscount(
            item.Book.SalePrice, discounts.GetValueOrDefault(item.BookId))) * item.Quantity);

        var order = new SalesOrder
        {
            Id = $"HD{DateTime.UtcNow:yyyyMMddHHmmss}{Convert.ToHexString(RandomNumberGenerator.GetBytes(3))}",
            CreatedAt = DateTime.UtcNow,
            CustomerId = customerId,
            TotalAmount = subtotal - orderDiscount,
            DiscountAmount = itemDiscount + orderDiscount,
            OrderStatus = "Pending",
            PaymentStatus = paymentMethod == "CashOnDelivery" ? "Unpaid" : "AwaitingPayment",
            PaymentMethod = paymentMethod,
            ShippingAddress = request.ShippingAddress.Trim(),
            ReceiverPhone = request.ReceiverPhone.Trim(),
            VoucherId = voucherDiscountRate >= orderDiscountRate && voucherDiscountRate > 0m ? voucherCode : null,
            Lines = lines
        };

        if (order.VoucherId is not null && voucher is not null)
        {
            voucher.UsageCount++;
        }

        foreach (var item in cart)
        {
            item.Book.StockQuantity -= item.Quantity;
        }

        db.Orders.Add(order);
        db.CartItems.RemoveRange(cart);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = ToResponse(order);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelMyOrder(string id, CancellationToken cancellationToken)
    {
        var customerId = User.FindFirstValue("customer_id");
        if (customerId is null) return Forbid();

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var order = await db.Orders.Include(item => item.Lines).ThenInclude(line => line.Book)
            .Include(item => item.Voucher)
            .SingleOrDefaultAsync(item => item.Id == id && item.CustomerId == customerId, cancellationToken);
        if (order is null) return NotFound();
        if (order.OrderStatus is not ("Pending" or "Confirmed"))
            return Conflict(new { message = "Chỉ có thể hủy đơn đang chờ xác nhận hoặc đã xác nhận, trước khi bắt đầu giao." });

        foreach (var line in order.Lines) line.Book.StockQuantity += line.Quantity;
        if (order.Voucher is not null && order.Voucher.UsageCount > 0) order.Voucher.UsageCount--;
        order.OrderStatus = "Cancelled";
        order.PaymentStatus = order.PaymentStatus == "Paid" ? "RefundPending" : "Cancelled";

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    private static OrderResponse ToResponse(SalesOrder order) => new(
        order.Id,
        order.CreatedAt,
        order.OrderStatus,
        order.PaymentStatus,
        order.PaymentMethod,
        order.ShippingAddress,
        order.ReceiverPhone,
        order.TotalAmount,
        order.DiscountAmount,
        order.Lines.Select(line => new OrderLineResponse(
            line.BookId,
            line.Book.Title,
            line.Quantity,
            line.UnitPrice,
            line.LineTotal)).ToArray());
}
