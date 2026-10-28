using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using BookStore.Domain.Entities;
using BookStore.Shared.Models;
using TrangSach.BUS.Results;
using TrangSach.DAO.Entities;

namespace TrangSach.BUS.Services;

public sealed record AdminOrderCreated(string Id, decimal TotalAmount, DateTime CreatedAt);

public sealed class AdminOrderService(BookStoreDao dao, BookPricingService pricing)
{
    public async Task<IReadOnlyList<ManagerOrderRow>> GetAsync(string? status, CancellationToken ct = default)
    {
        var orders = await dao.Orders.GetManagementOrdersAsync(status, ct);
        return orders.Select(x => new ManagerOrderRow(x.Id, x.CreatedAt, x.OrderStatus, x.PaymentStatus, x.PaymentMethod, x.TotalAmount,
            x.Customer.FullName, x.Employee is null ? string.Empty : $"{x.Employee.FirstName} {x.Employee.LastName}".Trim(), x.ReceiverPhone, x.ShippingAddress, x.Lines.Sum(y => y.Quantity),
            x.Lines.Select(y => new ManagerOrderLine(y.BookId, y.Book.Title, y.Quantity, y.UnitPrice, y.LineTotal)).ToArray())).ToArray();
    }

    public async Task<OperationResult<AdminOrderCreated>> CreateAsync(CreateAdminOrder command, int? accountId, CancellationToken ct = default)
    {
        var payment = command.PaymentMethod.Trim();
        if (payment is not ("CashOnDelivery" or "BankTransfer")) return OperationResult<AdminOrderCreated>.Invalid("Phương thức thanh toán không hợp lệ.");
        if (string.IsNullOrWhiteSpace(command.CustomerId) || command.ShippingAddress.Trim().Length is < 8 or > 500
            || command.ReceiverPhone.Length > 30 || !new PhoneAttribute().IsValid(command.ReceiverPhone))
            return OperationResult<AdminOrderCreated>.Invalid("Khách hàng, địa chỉ giao hàng hoặc số điện thoại không hợp lệ.");
        if (command.Lines.Count is < 1 or > 1000 || command.Lines.Any(x => string.IsNullOrWhiteSpace(x.BookId) || x.Quantity is < 1 or > 99))
            return OperationResult<AdminOrderCreated>.Invalid("Đơn hàng cần có sách và số lượng hợp lệ.");
        if (command.Lines.Select(x => x.BookId).Distinct().Count() != command.Lines.Count) return OperationResult<AdminOrderCreated>.Invalid("Mỗi đầu sách chỉ được xuất hiện một lần trong đơn.");
        await using var tx = await dao.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var customer = await dao.Customers.FindByIdAsync(command.CustomerId, ct);
        var bookIds = command.Lines.Select(x => x.BookId).ToArray();
        var books = await dao.Books.GetActiveByIdsAsync(bookIds, ct);
        if (customer is null || books.Count != bookIds.Length) return OperationResult<AdminOrderCreated>.Invalid("Khách hàng hoặc đầu sách không hợp lệ/đang ẩn.");
        var unavailable = command.Lines.FirstOrDefault(x => books[x.BookId].StockQuantity < x.Quantity);
        if (unavailable is not null) return OperationResult<AdminOrderCreated>.Conflict($"Tồn kho '{books[unavailable.BookId].Title}' không đủ.");
        var now = DateTime.UtcNow;
        var discounts = await pricing.GetActiveDiscountsAsync(bookIds, now, ct);
        var lines = command.Lines.Select(x =>
        {
            var book = books[x.BookId]; var unit = BookPricingService.ApplyDiscount(book.SalePrice, discounts.GetValueOrDefault(x.BookId));
            return new SalesOrderLine { BookId = x.BookId, Quantity = x.Quantity, UnitPrice = unit, LineTotal = unit * x.Quantity };
        }).ToList();
        var subtotal = lines.Sum(x => x.LineTotal);
        var orderRate = await dao.OrderPromotions.GetBestDiscountAsync(subtotal, now, ct);
        var orderDiscount = decimal.Round(subtotal * orderRate / 100m, 2, MidpointRounding.AwayFromZero);
        var itemDiscount = command.Lines.Sum(x => (books[x.BookId].SalePrice - BookPricingService.ApplyDiscount(books[x.BookId].SalePrice, discounts.GetValueOrDefault(x.BookId))) * x.Quantity);
        var employeeId = accountId is int id ? await dao.Employees.FindFirstIdByAccountAsync(id, ct) : null;
        var order = new SalesOrder { Id = $"HD{now:yyyyMMddHHmmss}{Convert.ToHexString(RandomNumberGenerator.GetBytes(3))}", CreatedAt = now,
            EmployeeId = employeeId, CustomerId = customer.Id, TotalAmount = subtotal - orderDiscount, DiscountAmount = itemDiscount + orderDiscount,
            OrderStatus = "Pending", PaymentStatus = payment == "CashOnDelivery" ? "Unpaid" : "AwaitingPayment", PaymentMethod = payment,
            ShippingAddress = command.ShippingAddress.Trim(), ReceiverPhone = command.ReceiverPhone.Trim(), Lines = lines };
        foreach (var line in command.Lines) books[line.BookId].StockQuantity -= line.Quantity;
        dao.Orders.Add(order); await dao.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return OperationResult<AdminOrderCreated>.Created(new(order.Id, order.TotalAmount, order.CreatedAt));
    }

    public async Task<OperationResult<Unit>> UpdateAsync(string id, UpdateAdminOrder command, CancellationToken ct = default)
    {
        var payment = command.PaymentMethod.Trim();
        if (payment is not ("CashOnDelivery" or "BankTransfer")) return OperationResult<Unit>.Invalid("Phương thức thanh toán không hợp lệ.");
        if (command.ShippingAddress.Trim().Length is < 8 or > 500 || command.ReceiverPhone.Length > 30 || !new PhoneAttribute().IsValid(command.ReceiverPhone))
            return OperationResult<Unit>.Invalid("Địa chỉ giao hàng hoặc số điện thoại không hợp lệ.");
        await using var tx = await dao.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await dao.Orders.FindSingleAsync(x => x.Id == id, ct);
        if (order is null) return OperationResult<Unit>.NotFound();
        if (order.OrderStatus is not ("Pending" or "Confirmed")) return OperationResult<Unit>.Conflict("Chỉ được sửa thông tin đơn trước khi bắt đầu giao.");
        if (order.PaymentStatus is "Paid" or "RefundPending" or "Refunded") return OperationResult<Unit>.Conflict("Đơn đã thanh toán; cần xử lý thanh toán riêng trước khi thay đổi thông tin.");
        order.ShippingAddress = command.ShippingAddress.Trim(); order.ReceiverPhone = command.ReceiverPhone.Trim(); order.PaymentMethod = payment;
        order.PaymentStatus = payment == "CashOnDelivery" ? "Unpaid" : "AwaitingPayment";
        await dao.SaveChangesAsync(ct); await tx.CommitAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }

    public async Task<OperationResult<Unit>> UpdateStatusAsync(string id, string status, int? accountId, CancellationToken ct = default)
    {
        var next = status.Trim();
        if (next is not ("Confirmed" or "Shipping" or "Completed" or "Cancelled")) return OperationResult<Unit>.Invalid("Trạng thái đơn hàng không hợp lệ.");
        await using var tx = await dao.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await dao.Orders.FindWithLinesAndVoucherAsync(id, ct);
        if (order is null) return OperationResult<Unit>.NotFound();
        var allowed = order.OrderStatus switch { "Pending" => next is "Confirmed" or "Cancelled", "Confirmed" => next is "Shipping" or "Cancelled", "Shipping" => next == "Completed", _ => false };
        if (!allowed) return OperationResult<Unit>.Conflict($"Không thể chuyển đơn từ {order.OrderStatus} sang {next}.");
        if (next == "Cancelled")
        {
            foreach (var line in order.Lines) line.Book.StockQuantity += line.Quantity;
            if (order.Voucher is not null && order.Voucher.UsageCount > 0) order.Voucher.UsageCount--;
            order.PaymentStatus = order.PaymentStatus == "Paid" ? "RefundPending" : "Cancelled";
        }
        order.OrderStatus = next;
        if (next == "Completed" && order.PaymentMethod == "CashOnDelivery") order.PaymentStatus = "Paid";
        if (order.EmployeeId is null && accountId is int current) order.EmployeeId = await dao.Employees.FindFirstIdByAccountAsync(current, ct);
        await dao.SaveChangesAsync(ct); await tx.CommitAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }

    public async Task<OperationResult<Unit>> MarkBankTransferPaidAsync(string id, CancellationToken ct = default)
    {
        var order = await dao.Orders.FindSingleAsync(x => x.Id == id, ct);
        if (order is null) return OperationResult<Unit>.NotFound();
        if (order.PaymentMethod != "BankTransfer" || order.PaymentStatus != "AwaitingPayment") return OperationResult<Unit>.Conflict("Đơn này không ở trạng thái chờ xác nhận chuyển khoản.");
        order.PaymentStatus = "Paid"; await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }

    public async Task<OperationResult<Unit>> MarkRefundCompletedAsync(string id, CancellationToken ct = default)
    {
        var order = await dao.Orders.FindSingleAsync(x => x.Id == id, ct);
        if (order is null) return OperationResult<Unit>.NotFound();
        if (order.OrderStatus != "Cancelled" || order.PaymentStatus != "RefundPending") return OperationResult<Unit>.Conflict("Đơn này không chờ ghi nhận hoàn tiền.");
        order.PaymentStatus = "Refunded"; await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }
}
