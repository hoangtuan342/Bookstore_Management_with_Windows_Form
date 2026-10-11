using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using BookStore.Api.Contracts;
using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using BookStore.Infrastructure.Services;
using BookStore.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Controllers;

[ApiController]
[Authorize(Roles = StaffRoles.ManagerOrSales)]
[Route("api/admin/orders")]
public sealed class AdminOrdersController(BookStoreDbContext db, BookPricingService pricing) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder(AdminCreateOrderRequest request, CancellationToken cancellationToken)
    {
        var paymentMethod = request.PaymentMethod.Trim();
        if (paymentMethod is not ("CashOnDelivery" or "BankTransfer"))
            return BadRequest(new { message = "Phương thức thanh toán không hợp lệ." });
        if (request.Lines.Select(line => line.BookId).Distinct().Count() != request.Lines.Count)
            return BadRequest(new { message = "Mỗi đầu sách chỉ được xuất hiện một lần trong đơn." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var customer = await db.Customers.SingleOrDefaultAsync(item => item.Id == request.CustomerId, cancellationToken);
        var bookIds = request.Lines.Select(line => line.BookId).ToArray();
        var books = await db.Books.Where(book => bookIds.Contains(book.Id) && book.IsActive)
            .ToDictionaryAsync(book => book.Id, cancellationToken);
        if (customer is null || books.Count != bookIds.Length)
            return BadRequest(new { message = "Khách hàng hoặc đầu sách không hợp lệ/đang ẩn." });

        var unavailable = request.Lines.FirstOrDefault(line => books[line.BookId].StockQuantity < line.Quantity);
        if (unavailable is not null)
            return Conflict(new { message = $"Tồn kho '{books[unavailable.BookId].Title}' không đủ." });

        var now = DateTime.UtcNow;
        var discounts = await pricing.GetActiveDiscountsAsync(bookIds, now, cancellationToken);
        var lines = request.Lines.Select(item =>
        {
            var book = books[item.BookId];
            var unitPrice = BookPricingService.ApplyDiscount(book.SalePrice, discounts.GetValueOrDefault(item.BookId));
            return new SalesOrderLine { BookId = item.BookId, Quantity = item.Quantity, UnitPrice = unitPrice, LineTotal = unitPrice * item.Quantity };
        }).ToList();
        var subtotal = lines.Sum(line => line.LineTotal);
        var orderDiscountRate = await db.OrderPromotions.AsNoTracking()
            .Where(item => item.MinimumOrderAmount <= subtotal && item.Promotion.StartsAt <= now && item.Promotion.EndsAt >= now)
            .Select(item => (decimal?)item.DiscountPercent).MaxAsync(cancellationToken) ?? 0m;
        var orderDiscount = decimal.Round(subtotal * orderDiscountRate / 100m, 2, MidpointRounding.AwayFromZero);
        var itemDiscount = request.Lines.Sum(item =>
        {
            var book = books[item.BookId];
            return (book.SalePrice - BookPricingService.ApplyDiscount(book.SalePrice, discounts.GetValueOrDefault(item.BookId))) * item.Quantity;
        });
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var employeeId = int.TryParse(accountId, out var managerAccountId)
            ? await db.Employees.Where(employee => employee.UserAccountId == managerAccountId)
                .Select(employee => employee.Id).FirstOrDefaultAsync(cancellationToken)
            : null;

        var order = new SalesOrder
        {
            Id = $"HD{now:yyyyMMddHHmmss}{Convert.ToHexString(RandomNumberGenerator.GetBytes(3))}",
            CreatedAt = now,
            EmployeeId = employeeId,
            CustomerId = customer.Id,
            TotalAmount = subtotal - orderDiscount,
            DiscountAmount = itemDiscount + orderDiscount,
            OrderStatus = "Pending",
            PaymentStatus = paymentMethod == "CashOnDelivery" ? "Unpaid" : "AwaitingPayment",
            PaymentMethod = paymentMethod,
            ShippingAddress = request.ShippingAddress.Trim(),
            ReceiverPhone = request.ReceiverPhone.Trim(),
            Lines = lines
        };

        foreach (var item in request.Lines) books[item.BookId].StockQuantity -= item.Quantity;
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { order.Id, order.TotalAmount, order.CreatedAt });
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(order => order.OrderStatus == status);
        }

        var orders = await query.OrderByDescending(order => order.CreatedAt)
            .Select(order => new
            {
                order.Id,
                order.CreatedAt,
                order.OrderStatus,
                order.PaymentStatus,
                order.PaymentMethod,
                order.TotalAmount,
                CustomerName = order.Customer.FullName,
                order.ReceiverPhone,
                order.ShippingAddress,
                ItemCount = order.Lines.Sum(line => line.Quantity),
                Lines = order.Lines.Select(line => new
                {
                    line.BookId,
                    Title = line.Book.Title,
                    line.Quantity,
                    line.UnitPrice,
                    line.LineTotal
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(orders);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(string id, UpdateAdminOrderRequest request, CancellationToken cancellationToken)
    {
        var paymentMethod = request.PaymentMethod.Trim();
        if (paymentMethod is not ("CashOnDelivery" or "BankTransfer"))
            return BadRequest(new { message = "Phương thức thanh toán không hợp lệ." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var order = await db.Orders.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (order is null) return NotFound();
        if (order.OrderStatus is not ("Pending" or "Confirmed"))
            return Conflict(new { message = "Chỉ được sửa thông tin đơn trước khi bắt đầu giao." });
        if (order.PaymentStatus is "Paid" or "RefundPending" or "Refunded")
            return Conflict(new { message = "Đơn đã thanh toán; cần xử lý thanh toán riêng trước khi thay đổi thông tin." });

        order.ShippingAddress = request.ShippingAddress.Trim();
        order.ReceiverPhone = request.ReceiverPhone.Trim();
        order.PaymentMethod = paymentMethod;
        order.PaymentStatus = paymentMethod == "CashOnDelivery" ? "Unpaid" : "AwaitingPayment";
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public Task<IActionResult> DeleteAsCancelled(string id, CancellationToken cancellationToken) =>
        UpdateStatus(id, new UpdateOrderStatusRequest { Status = "Cancelled" }, cancellationToken);

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var nextStatus = request.Status.Trim();
        if (nextStatus is not ("Confirmed" or "Shipping" or "Completed" or "Cancelled"))
        {
            return BadRequest(new { message = "Trạng thái đơn hàng không hợp lệ." });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var order = await db.Orders.Include(item => item.Lines).ThenInclude(line => line.Book)
            .Include(item => item.Voucher)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var allowed = order.OrderStatus switch
        {
            "Pending" => nextStatus is "Confirmed" or "Cancelled",
            "Confirmed" => nextStatus is "Shipping" or "Cancelled",
            "Shipping" => nextStatus == "Completed",
            _ => false
        };
        if (!allowed)
        {
            return Conflict(new { message = $"Không thể chuyển đơn từ {order.OrderStatus} sang {nextStatus}." });
        }

        if (nextStatus == "Cancelled")
        {
            foreach (var line in order.Lines)
            {
                line.Book.StockQuantity += line.Quantity;
            }
            if (order.Voucher is not null && order.Voucher.UsageCount > 0) order.Voucher.UsageCount--;
            order.PaymentStatus = order.PaymentStatus == "Paid" ? "RefundPending" : "Cancelled";
        }

        order.OrderStatus = nextStatus;
        if (nextStatus == "Completed" && order.PaymentMethod == "CashOnDelivery")
        {
            order.PaymentStatus = "Paid";
        }

        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (order.EmployeeId is null && int.TryParse(accountId, out var parsedAccountId))
        {
            order.EmployeeId = await db.Employees.Where(employee => employee.UserAccountId == parsedAccountId)
                .Select(employee => employee.Id).FirstOrDefaultAsync(cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/payment")]
    public async Task<IActionResult> MarkBankTransferPaid(string id, CancellationToken cancellationToken)
    {
        var order = await db.Orders.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        if (order.PaymentMethod != "BankTransfer" || order.PaymentStatus != "AwaitingPayment")
        {
            return Conflict(new { message = "Đơn này không ở trạng thái chờ xác nhận chuyển khoản." });
        }

        order.PaymentStatus = "Paid";
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/refund")]
    public async Task<IActionResult> MarkRefundCompleted(string id, CancellationToken cancellationToken)
    {
        var order = await db.Orders.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (order is null) return NotFound();
        if (order.OrderStatus != "Cancelled" || order.PaymentStatus != "RefundPending")
            return Conflict(new { message = "Đơn này không chờ ghi nhận hoàn tiền." });
        order.PaymentStatus = "Refunded";
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
