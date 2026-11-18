using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TrangSach.Shared.DTO;

namespace TrangSach.BUS
{
    public class AdminOrderService
    {
        private readonly BookStoreDao _dao;
        private readonly BookPricingService _pricing;

        // Danh sách đơn hàng được load vào RAM
        private List<ManagerOrderRowDto> _orders = new();

        public AdminOrderService(BookStoreDao dao, BookPricingService pricing)
        {
            _dao = dao;
            _pricing = pricing;
        }

        // ---- READ ----
        public IReadOnlyList<ManagerOrderRowDto> GetOrders() => _orders;

        public async Task<IReadOnlyList<ManagerOrderRowDto>> GetAsync(string? status, CancellationToken ct = default)
        {
            if (_orders.Count == 0)
            {
                _orders = await _dao.Orders.GetAllOrdersAsync(ct).ConfigureAwait(false);
            }
            if (string.IsNullOrWhiteSpace(status)) return _orders;
            return _orders.Where(x => x.OrderStatus == status).ToList();
        }

        public async Task<IReadOnlyList<ManagerOrderLineDto>> GetLinesAsync(string orderId, CancellationToken ct = default)
            => await _dao.Orders.GetOrderLinesAsync(orderId, ct).ConfigureAwait(false);

        // ---- CREATE ----
        public async Task<OperationResult<AdminOrderCreated>> CreateAsync(CreateAdminOrderDto command, int? accountId, CancellationToken ct = default)
        {
            var payment = command.PaymentMethod.Trim();
            if (payment != "CashOnDelivery" && payment != "BankTransfer")
                return OperationResult<AdminOrderCreated>.Invalid("Phương thức thanh toán không hợp lệ.");

            if (string.IsNullOrWhiteSpace(command.CustomerId)
                || command.ShippingAddress.Trim().Length < 8 || command.ShippingAddress.Trim().Length > 500
                || command.ReceiverPhone.Length > 30
                || !new System.ComponentModel.DataAnnotations.PhoneAttribute().IsValid(command.ReceiverPhone))
                return OperationResult<AdminOrderCreated>.Invalid("Khách hàng, địa chỉ giao hàng hoặc số điện thoại không hợp lệ.");

            if (command.Lines.Count < 1 || command.Lines.Count > 1000
                || command.Lines.Any(x => string.IsNullOrWhiteSpace(x.BookId) || x.Quantity < 1 || x.Quantity > 99))
                return OperationResult<AdminOrderCreated>.Invalid("Đơn hàng cần có sách và số lượng hợp lệ.");

            if (command.Lines.Select(x => x.BookId).Distinct().Count() != command.Lines.Count)
                return OperationResult<AdminOrderCreated>.Invalid("Mỗi đầu sách chỉ được xuất hiện một lần trong đơn.");

            var books = await _dao.Catalog.GetBooksForOrderAsync(command.Lines.Select(x => x.BookId).ToArray(), ct).ConfigureAwait(false);
            if (books.Count != command.Lines.Count)
                return OperationResult<AdminOrderCreated>.Invalid("Một hoặc nhiều đầu sách không tồn tại hoặc đang ẩn.");

            // Kiểm tra tồn kho
            foreach (var line in command.Lines)
            {
                var book = books.FirstOrDefault(b => b.BookId == line.BookId);
                if (string.IsNullOrEmpty(book.BookId) || book.Stock < line.Quantity)
                    return OperationResult<AdminOrderCreated>.Conflict($"Tồn kho '{line.BookId}' không đủ.");
            }

            var now = DateTime.UtcNow;
            var discounts = _pricing.GetActiveDiscounts(command.Lines.Select(x => x.BookId).ToArray(), now, ct);

            var orderLines = command.Lines.Select(x =>
            {
                var book = books.First(b => b.BookId == x.BookId);
                decimal pct = discounts.TryGetValue(x.BookId, out var d) ? d : 0;
                var unit = BookPricingService.ApplyDiscount(book.Price, pct);
                return (BookId: x.BookId, Quantity: x.Quantity, UnitPrice: unit, LineTotal: unit * x.Quantity);
            }).ToList();

            var subtotal = orderLines.Sum(l => l.LineTotal);
            var orderDiscPct = _pricing.GetOrderDiscount(subtotal, now);
            var orderDisc = decimal.Round(subtotal * orderDiscPct / 100m, 2, MidpointRounding.AwayFromZero);
            var total = subtotal - orderDisc;
            var itemDisc = orderLines.Sum(l =>
            {
                var book = books.First(b => b.BookId == l.BookId);
                decimal pct = discounts.TryGetValue(l.BookId, out var d) ? d : 0;
                return (book.Price - BookPricingService.ApplyDiscount(book.Price, pct)) * l.Quantity;
            });
            var totalDisc = itemDisc + orderDisc;

            var orderId = $"HD{now:yyyyMMddHHmmss}{Convert.ToHexString(RandomNumberGenerator.GetBytes(3))}";
            var payStatus = payment == "CashOnDelivery" ? "Unpaid" : "AwaitingPayment";

            await _dao.Orders.InsertOrderAsync(orderId, now, command.CustomerId, null, null,
                total, totalDisc, "Pending", payStatus, payment,
                command.ShippingAddress.Trim(), command.ReceiverPhone.Trim(),
                orderLines.Select(l => (l.BookId, l.Quantity, l.UnitPrice, l.LineTotal)).ToList(), ct).ConfigureAwait(false);

            var customerName = command.CustomerId;
            var newRow = new ManagerOrderRowDto(orderId, now, "Pending", payStatus, payment, total,
                customerName, string.Empty, command.ReceiverPhone, command.ShippingAddress, command.Lines.Sum(l => l.Quantity),
                orderLines.Select(l =>
                {
                    var book = books.First(b => b.BookId == l.BookId);
                    return new ManagerOrderLineDto(l.BookId, book.Title, l.Quantity, l.UnitPrice, l.LineTotal);
                }).ToArray());
            _orders.Insert(0, newRow);

            return OperationResult<AdminOrderCreated>.Created(new AdminOrderCreated(orderId, total, now));
        }

        // ---- UPDATE ----
        public async Task<OperationResult<Unit>> UpdateAsync(string id, UpdateAdminOrderDto command, CancellationToken ct = default)
        {
            if (_orders.Count == 0)
                await GetAsync(null, ct).ConfigureAwait(false);

            var cached = _orders.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            cached.ShippingAddress = command.ShippingAddress;
            cached.ReceiverPhone = command.ReceiverPhone;
            cached.PaymentMethod = command.PaymentMethod;
            return OperationResult<Unit>.Success(new Unit());
        }

        // ---- UPDATE STATUS ----
        public async Task<OperationResult<Unit>> UpdateStatusAsync(string id, string status, int? accountId, CancellationToken ct = default)
        {
            var next = status.Trim();
            if (next != "Confirmed" && next != "Shipping" && next != "Completed" && next != "Cancelled")
                return OperationResult<Unit>.Invalid("Trạng thái đơn hàng không hợp lệ.");

            if (_orders.Count == 0)
                await GetAsync(null, ct).ConfigureAwait(false);

            var cached = _orders.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            bool allowed = cached.OrderStatus switch
            {
                "Pending" => next == "Confirmed" || next == "Cancelled",
                "Confirmed" => next == "Shipping" || next == "Cancelled",
                "Shipping" => next == "Completed",
                _ => false
            };
            if (!allowed) return OperationResult<Unit>.Conflict($"Không thể chuyển đơn từ {cached.OrderStatus} sang {next}.");

            string? newPayStatus = null;
            if (next == "Cancelled")
            {
                await _dao.Orders.RestoreStockAsync(id, ct).ConfigureAwait(false);
                await _dao.Orders.DecrementVoucherUsageAsync(id, ct).ConfigureAwait(false);
                newPayStatus = cached.PaymentStatus == "Paid" ? "RefundPending" : "Cancelled";
            }
            if (next == "Completed" && cached.PaymentMethod == "CashOnDelivery")
                newPayStatus = "Paid";

            await _dao.Orders.UpdateOrderStatusAsync(id, next, newPayStatus, ct).ConfigureAwait(false);

            cached.OrderStatus = next;
            if (newPayStatus != null) cached.PaymentStatus = newPayStatus;
            return OperationResult<Unit>.Success(new Unit());
        }

        // ---- MARK PAID ----
        public async Task<OperationResult<Unit>> MarkBankTransferPaidAsync(string id, CancellationToken ct = default)
        {
            if (_orders.Count == 0)
                await GetAsync(null, ct).ConfigureAwait(false);

            var cached = _orders.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();
            if (cached.PaymentMethod != "BankTransfer" || cached.PaymentStatus != "AwaitingPayment")
                return OperationResult<Unit>.Conflict("Đơn này không ở trạng thái chờ xác nhận chuyển khoản.");

            await _dao.Orders.UpdateOrderStatusAsync(id, cached.OrderStatus, "Paid", ct).ConfigureAwait(false);
            cached.PaymentStatus = "Paid";
            return OperationResult<Unit>.Success(new Unit());
        }

        // ---- MARK REFUND ----
        public async Task<OperationResult<Unit>> MarkRefundCompletedAsync(string id, CancellationToken ct = default)
        {
            if (_orders.Count == 0)
                await GetAsync(null, ct).ConfigureAwait(false);

            var cached = _orders.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();
            if (cached.OrderStatus != "Cancelled" || cached.PaymentStatus != "RefundPending")
                return OperationResult<Unit>.Conflict("Đơn này không chờ ghi nhận hoàn tiền.");

            await _dao.Orders.UpdateOrderStatusAsync(id, "Cancelled", "Refunded", ct).ConfigureAwait(false);
            cached.PaymentStatus = "Refunded";
            return OperationResult<Unit>.Success(new Unit());
        }
    }

    // DTO helper cho kết quả tạo đơn
    public class AdminOrderCreated
    {
        public string Id { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }

        public AdminOrderCreated() { }

        public AdminOrderCreated(string id, decimal totalAmount, DateTime createdAt)
        {
            Id = id;
            TotalAmount = totalAmount;
            CreatedAt = createdAt;
        }
    }
}
