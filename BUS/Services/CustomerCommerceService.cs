using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;
using BookStore.Domain.Entities;
using BookStore.Shared.Models;
using TrangSach.BUS.Results;
using TrangSach.DAO.Entities;

namespace TrangSach.BUS.Services;

public sealed class CustomerCommerceService(BookStoreDao dao, BookPricingService pricing)
{
    public async Task<IReadOnlyList<StoreCartLine>> GetCartAsync(string customerId, CancellationToken ct = default)
    {
        var cart = await dao.CartItems.GetCustomerDisplayCartAsync(customerId, ct);
        var discounts = await pricing.GetActiveDiscountsAsync(cart.Select(x => x.BookId).ToArray(), DateTime.UtcNow, ct);
        return cart.Select(x =>
        {
            var unit = BookPricingService.ApplyDiscount(x.Book.SalePrice, discounts.GetValueOrDefault(x.BookId));
            return new StoreCartLine(x.BookId, x.Book.Title, x.Book.Authors.Select(y => y.Author.Name).FirstOrDefault() ?? "Chưa cập nhật", unit, x.Quantity, x.Book.StockQuantity, unit * x.Quantity);
        }).ToArray();
    }

    public async Task<OperationResult<Unit>> SetCartQuantityAsync(string customerId, string bookId, int quantity, CancellationToken ct = default)
    {
        var book = await dao.Books.FindAsync([bookId], ct);
        if (book is null) return OperationResult<Unit>.NotFound("Không tìm thấy sách.");
        if (quantity is < 1 or > 99) return OperationResult<Unit>.Invalid("Số lượng phải từ 1 đến 99.");
        if (quantity > book.StockQuantity) return OperationResult<Unit>.Conflict("Số lượng vượt quá tồn kho hiện tại.");
        var item = await dao.CartItems.FindByCustomerAndBookAsync(customerId, bookId, ct);
        if (item is null) dao.CartItems.Add(new CartItem { CustomerId = customerId, BookId = bookId, Quantity = quantity });
        else item.Quantity = quantity;
        await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }

    public async Task RemoveCartItemAsync(string customerId, string bookId, CancellationToken ct = default)
    {
        var item = await dao.CartItems.FindByCustomerAndBookAsync(customerId, bookId, ct);
        if (item is null) return;
        dao.CartItems.Remove(item); await dao.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<StoreOrder>> GetOrdersAsync(string customerId, CancellationToken ct = default) =>
        (await dao.Orders.GetCustomerOrdersAsync(customerId, ct)).Select(ToOrder).ToArray();

    public async Task<OperationResult<StoreOrder>> CheckoutAsync(string customerId, CheckoutDetails command, CancellationToken ct = default)
    {
        var payment = command.PaymentMethod.Trim();
        if (payment is not ("CashOnDelivery" or "BankTransfer")) return OperationResult<StoreOrder>.Invalid("Phương thức thanh toán hiện hỗ trợ COD hoặc chuyển khoản mô phỏng.");
        if (command.ShippingAddress.Trim().Length is < 8 or > 500 || !new PhoneAttribute().IsValid(command.ReceiverPhone) || command.ReceiverPhone.Length > 30
            || command.VoucherCode?.Length > 40)
            return OperationResult<StoreOrder>.Invalid("Địa chỉ giao hàng hoặc số điện thoại không hợp lệ.");
        await using var tx = await dao.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var cart = await dao.CartItems.GetCustomerCheckoutCartAsync(customerId, ct);
        if (cart.Count == 0) return OperationResult<StoreOrder>.Invalid("Giỏ hàng đang trống.");
        var unavailable = cart.FirstOrDefault(x => x.Quantity > x.Book.StockQuantity);
        if (unavailable is not null) return OperationResult<StoreOrder>.Conflict($"Số lượng '{unavailable.Book.Title}' vừa thay đổi. Hãy cập nhật giỏ hàng.");
        var now = DateTime.UtcNow;
        var discounts = await pricing.GetActiveDiscountsAsync(cart.Select(x => x.BookId).ToArray(), now, ct);
        var lines = cart.Select(x =>
        {
            var unit = BookPricingService.ApplyDiscount(x.Book.SalePrice, discounts.GetValueOrDefault(x.BookId));
            return new SalesOrderLine { BookId = x.BookId, Quantity = x.Quantity, UnitPrice = unit, LineTotal = unit * x.Quantity };
        }).ToList();
        var subtotal = lines.Sum(x => x.LineTotal);
        var orderRate = await dao.OrderPromotions.GetBestDiscountAsync(subtotal, now, ct);
        var voucherCode = command.VoucherCode?.Trim().ToUpperInvariant();
        Voucher? voucher = null; decimal voucherRate = 0;
        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            voucher = await dao.Vouchers.FindForCheckoutAsync(voucherCode, ct);
            if (voucher is null || voucher.Status != "Active" || voucher.ExpiresAt.HasValue && voucher.ExpiresAt.Value < now
                || voucher.MaxUsageCount.HasValue && voucher.UsageCount >= voucher.MaxUsageCount.Value)
                return OperationResult<StoreOrder>.Invalid("Voucher không tồn tại, đã hết hạn hoặc đã hết lượt sử dụng.");
            voucherRate = voucher.Promotions.Where(x => x.Promotion.StartsAt <= now && x.Promotion.EndsAt >= now).Select(x => x.DiscountPercent).DefaultIfEmpty(0m).Max();
            if (voucherRate <= 0) return OperationResult<StoreOrder>.Invalid("Voucher hiện không áp dụng được.");
        }
        var appliedRate = Math.Max(orderRate, voucherRate);
        var orderDiscount = decimal.Round(subtotal * appliedRate / 100m, 2, MidpointRounding.AwayFromZero);
        var itemDiscount = cart.Sum(x => (x.Book.SalePrice - BookPricingService.ApplyDiscount(x.Book.SalePrice, discounts.GetValueOrDefault(x.BookId))) * x.Quantity);
        var order = new SalesOrder { Id = $"HD{now:yyyyMMddHHmmss}{Convert.ToHexString(RandomNumberGenerator.GetBytes(3))}", CreatedAt = now,
            CustomerId = customerId, TotalAmount = subtotal - orderDiscount, DiscountAmount = itemDiscount + orderDiscount, OrderStatus = "Pending",
            PaymentStatus = payment == "CashOnDelivery" ? "Unpaid" : "AwaitingPayment", PaymentMethod = payment,
            ShippingAddress = command.ShippingAddress.Trim(), ReceiverPhone = command.ReceiverPhone.Trim(),
            VoucherId = voucherRate >= orderRate && voucherRate > 0 ? voucherCode : null, Lines = lines };
        if (order.VoucherId is not null && voucher is not null) voucher.UsageCount++;
        foreach (var item in cart) item.Book.StockQuantity -= item.Quantity;
        dao.Orders.Add(order); dao.CartItems.RemoveRange(cart); await dao.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        foreach (var line in order.Lines) line.Book = cart.Single(x => x.BookId == line.BookId).Book;
        return OperationResult<StoreOrder>.Created(ToOrder(order));
    }

    public async Task<OperationResult<Unit>> CancelOrderAsync(string id, string customerId, CancellationToken ct = default)
    {
        await using var tx = await dao.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await dao.Orders.FindCustomerOrderWithLinesAndVoucherAsync(id, customerId, ct);
        if (order is null) return OperationResult<Unit>.NotFound();
        if (order.OrderStatus is not ("Pending" or "Confirmed")) return OperationResult<Unit>.Conflict("Chỉ có thể hủy đơn đang chờ xác nhận hoặc đã xác nhận, trước khi bắt đầu giao.");
        foreach (var line in order.Lines) line.Book.StockQuantity += line.Quantity;
        if (order.Voucher is not null && order.Voucher.UsageCount > 0) order.Voucher.UsageCount--;
        order.OrderStatus = "Cancelled"; order.PaymentStatus = order.PaymentStatus == "Paid" ? "RefundPending" : "Cancelled";
        await dao.SaveChangesAsync(ct); await tx.CommitAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }

    private static StoreOrder ToOrder(SalesOrder row) => new(row.Id, row.CreatedAt, row.OrderStatus, row.PaymentStatus, row.PaymentMethod,
        row.ShippingAddress, row.ReceiverPhone, row.TotalAmount, row.DiscountAmount,
        row.Lines.Select(x => new StoreOrderLine(x.BookId, x.Book.Title, x.Quantity, x.UnitPrice, x.LineTotal)).ToArray());
}
