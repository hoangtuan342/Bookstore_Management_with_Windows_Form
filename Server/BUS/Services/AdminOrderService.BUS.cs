using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
        public List<ManagerOrderRowDto> GetOrders() => _orders;

        // ----------------------------------------------------------------
        // Synchronous Methods (Phong cách thiết kế 3 lớp truyền thống)
        // ----------------------------------------------------------------
        public List<ManagerOrderRowDto> GetOrdersList(string? status = null)
        {
            if (_orders.Count == 0) _orders = _dao.Orders.GetAllOrders();
            if (string.IsNullOrWhiteSpace(status)) return _orders;
            return _orders.Where(x => x.OrderStatus == status).ToList();
        }

        public List<ManagerOrderLineDto> GetLines(string orderId)
            => _dao.Orders.GetOrderLines(orderId);

        public AdminOrderCreated Create(CreateAdminOrderDto command, int? accountId)
        {
            var payment = command.PaymentMethod.Trim();
            if (payment != "CashOnDelivery" && payment != "BankTransfer")
                throw new ArgumentException("Phương thức thanh toán không hợp lệ.");

            if (string.IsNullOrWhiteSpace(command.CustomerId)
                || command.ShippingAddress.Trim().Length < 8 || command.ShippingAddress.Trim().Length > 500
                || command.ReceiverPhone.Length > 30
                || !new System.ComponentModel.DataAnnotations.PhoneAttribute().IsValid(command.ReceiverPhone))
                throw new ArgumentException("Khách hàng, địa chỉ giao hàng hoặc số điện thoại không hợp lệ.");

            if (command.Lines.Count < 1 || command.Lines.Count > 1000
                || command.Lines.Any(x => string.IsNullOrWhiteSpace(x.BookId) || x.Quantity < 1 || x.Quantity > 99))
                throw new ArgumentException("Đơn hàng cần có sách và số lượng hợp lệ.");

            if (command.Lines.Select(x => x.BookId).Distinct().Count() != command.Lines.Count)
                throw new ArgumentException("Mỗi đầu sách chỉ được xuất hiện một lần trong đơn.");

            var books = _dao.Catalog.GetBooksForOrder(command.Lines.Select(x => x.BookId).ToArray());
            if (books.Count != command.Lines.Count)
                throw new ArgumentException("Một hoặc nhiều đầu sách không tồn tại hoặc đang ẩn.");

            foreach (var line in command.Lines)
            {
                var book = books.FirstOrDefault(b => b.BookId == line.BookId);
                if (string.IsNullOrEmpty(book.BookId) || book.Stock < line.Quantity)
                    throw new InvalidOperationException($"Tồn kho '{line.BookId}' không đủ.");
            }

            var now = DateTime.UtcNow;
            var discounts = _pricing.GetActiveDiscounts(command.Lines.Select(x => x.BookId).ToList(), now);

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

            var employeeId = !string.IsNullOrWhiteSpace(command.EmployeeId) ? command.EmployeeId : null;

            // Xử lý thông tin khách hàng:
            // 1. Nếu CustomerId truyền vào là Mã KH đã có -> lấy thông tin KH đó
            // 2. Nếu CustomerId truyền vào là Tên khách mới (hoặc không khớp Mã KH có sẵn) -> tự động tạo hồ sơ KH mới
            var customerId = command.CustomerId;
            var customerName = !string.IsNullOrWhiteSpace(command.CustomerName) ? command.CustomerName.Trim() : command.CustomerId;

            var existingCustomer = _dao.Users.GetAllCustomers().FirstOrDefault(c =>
                c.Id.Equals(customerId, StringComparison.OrdinalIgnoreCase) ||
                c.FullName.Equals(customerName, StringComparison.OrdinalIgnoreCase));

            if (existingCustomer != null)
            {
                customerId = existingCustomer.Id;
                customerName = existingCustomer.FullName;
            }
            else
            {
                // Tự sinh mã khách hàng mới và lưu vào bảng KH để liên kết khóa ngoại
                var newCusId = $"KH{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}";
                try
                {
                    _dao.Users.InsertCustomer(newCusId, customerName, command.ReceiverPhone?.Trim(), command.ShippingAddress?.Trim(), null);
                    customerId = newCusId;
                }
                catch
                {
                    // Dự phòng nếu không ghi được KH thì lấy KH đầu tiên
                    customerId = _dao.Users.GetAllCustomers().FirstOrDefault()?.Id ?? "KH00000001";
                }
            }

            _dao.Orders.InsertOrder(orderId, now, customerId, employeeId, null,
                total, totalDisc, "Pending", payStatus, payment,
                command.ShippingAddress.Trim(), command.ReceiverPhone.Trim(),
                orderLines.Select(l => (l.BookId, l.Quantity, l.UnitPrice, l.LineTotal)).ToList());

            var employeeName = string.Empty;
            if (!string.IsNullOrWhiteSpace(employeeId))
            {
                var emp = _dao.Users.GetAllEmployees().FirstOrDefault(e => e.Id == employeeId);
                if (emp != null) employeeName = $"{emp.LastName} {emp.FirstName}".Trim();
            }

            var newRow = new ManagerOrderRowDto(orderId, now, "Pending", payStatus, payment, total,
                customerName, employeeName, command.ReceiverPhone, command.ShippingAddress, command.Lines.Sum(l => l.Quantity),
                orderLines.Select(l =>
                {
                    var book = books.First(b => b.BookId == l.BookId);
                    return new ManagerOrderLineDto(l.BookId, book.Title, l.Quantity, l.UnitPrice, l.LineTotal);
                }).ToList());
            _orders.Insert(0, newRow);

            return new AdminOrderCreated(orderId, total, now);
        }

        public void Update(string id, UpdateAdminOrderDto command)
        {
            if (_orders.Count == 0) GetOrdersList();
            var cached = _orders.FirstOrDefault(x => x.Id == id);
            if (cached == null) throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

            var payment = command.PaymentMethod?.Trim() ?? cached.PaymentMethod;
            var paymentStatus = command.PaymentStatus?.Trim() ?? cached.PaymentStatus;

            // Nếu người dùng có cập nhật danh sách mặt hàng (Lines)
            if (command.Lines != null && command.Lines.Count > 0)
            {
                if (cached.OrderStatus is "Completed" or "Cancelled")
                    throw new InvalidOperationException("Không thể sửa mặt hàng của đơn đã hoàn thành hoặc đã hủy.");

                if (command.Lines.Any(x => string.IsNullOrWhiteSpace(x.BookId) || x.Quantity < 1 || x.Quantity > 99))
                    throw new ArgumentException("Đơn hàng cần có sách và số lượng hợp lệ (1-99).");

                if (command.Lines.Select(x => x.BookId).Distinct().Count() != command.Lines.Count)
                    throw new ArgumentException("Mỗi đầu sách chỉ được xuất hiện một lần trong đơn.");

                var books = _dao.Catalog.GetBooksForOrder(command.Lines.Select(x => x.BookId).ToArray());
                if (books.Count != command.Lines.Count)
                    throw new ArgumentException("Một hoặc nhiều đầu sách không tồn tại hoặc đang ẩn.");

                var now = DateTime.UtcNow;
                var discounts = _pricing.GetActiveDiscounts(command.Lines.Select(x => x.BookId).ToList(), now);

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

                _dao.Orders.UpdateOrderFull(id, command.ShippingAddress.Trim(), command.ReceiverPhone.Trim(), payment, paymentStatus,
                    total, totalDisc, orderLines.Select(l => (l.BookId, l.Quantity, l.UnitPrice, l.LineTotal)).ToList(), command.EmployeeId);

                cached.ShippingAddress = command.ShippingAddress;
                cached.ReceiverPhone = command.ReceiverPhone;
                cached.PaymentMethod = payment;
                cached.PaymentStatus = paymentStatus;
                if (!string.IsNullOrWhiteSpace(command.EmployeeId))
                {
                    var emp = _dao.Users.GetAllEmployees().FirstOrDefault(e => e.Id == command.EmployeeId);
                    if (emp != null) cached.EmployeeName = $"{emp.LastName} {emp.FirstName}".Trim();
                }
                if (!string.IsNullOrWhiteSpace(command.CustomerName)) cached.CustomerName = command.CustomerName;
                cached.TotalAmount = total;
                cached.ItemCount = command.Lines.Sum(l => l.Quantity);
                cached.Lines = orderLines.Select(l =>
                {
                    var book = books.First(b => b.BookId == l.BookId);
                    return new ManagerOrderLineDto(l.BookId, book.Title, l.Quantity, l.UnitPrice, l.LineTotal);
                }).ToList();
            }
            else
            {
                _dao.Orders.UpdateOrderInfo(id, command.ShippingAddress.Trim(), command.ReceiverPhone.Trim(), payment, command.EmployeeId);
                cached.ShippingAddress = command.ShippingAddress;
                cached.ReceiverPhone = command.ReceiverPhone;
                cached.PaymentMethod = payment;
                if (!string.IsNullOrWhiteSpace(command.EmployeeId))
                {
                    var emp = _dao.Users.GetAllEmployees().FirstOrDefault(e => e.Id == command.EmployeeId);
                    if (emp != null) cached.EmployeeName = $"{emp.LastName} {emp.FirstName}".Trim();
                }
                if (!string.IsNullOrWhiteSpace(command.CustomerName)) cached.CustomerName = command.CustomerName;
                if (!string.IsNullOrWhiteSpace(command.PaymentStatus))
                {
                    cached.PaymentStatus = command.PaymentStatus;
                    _dao.Orders.UpdateOrderStatus(id, cached.OrderStatus, command.PaymentStatus);
                }
            }
        }

        public void UpdateStatus(string id, string status, int? accountId)
        {
            var next = status.Trim();
            if (next != "Confirmed" && next != "Shipping" && next != "Completed" && next != "Cancelled")
                throw new ArgumentException("Trạng thái đơn hàng không hợp lệ.");

            if (_orders.Count == 0) GetOrdersList();
            var cached = _orders.FirstOrDefault(x => x.Id == id);
            if (cached == null) throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

            bool allowed = cached.OrderStatus switch
            {
                "Pending" => next == "Confirmed" || next == "Cancelled",
                "Confirmed" => next == "Shipping" || next == "Cancelled",
                "Shipping" => next == "Completed",
                _ => false
            };
            if (!allowed) throw new InvalidOperationException($"Không thể chuyển đơn từ {cached.OrderStatus} sang {next}.");

            string? newPayStatus = null;
            if (next == "Cancelled")
            {
                _dao.Orders.RestoreStock(id);
                _dao.Orders.DecrementVoucherUsage(id);
                newPayStatus = cached.PaymentStatus == "Paid" ? "RefundPending" : "Cancelled";
            }
            if (next == "Completed" && cached.PaymentMethod == "CashOnDelivery")
                newPayStatus = "Paid";

            _dao.Orders.UpdateOrderStatus(id, next, newPayStatus);

            cached.OrderStatus = next;
            if (newPayStatus != null) cached.PaymentStatus = newPayStatus;
        }

        public void MarkBankTransferPaid(string id)
        {
            if (_orders.Count == 0) GetOrdersList();
            var cached = _orders.FirstOrDefault(x => x.Id == id);
            if (cached == null) throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
            if (cached.PaymentMethod != "BankTransfer" || cached.PaymentStatus != "AwaitingPayment")
                throw new InvalidOperationException("Đơn này không ở trạng thái chờ xác nhận chuyển khoản.");

            _dao.Orders.UpdateOrderStatus(id, cached.OrderStatus, "Paid");
            cached.PaymentStatus = "Paid";
        }

        public void MarkRefundCompleted(string id)
        {
            if (_orders.Count == 0) GetOrdersList();
            var cached = _orders.FirstOrDefault(x => x.Id == id);
            if (cached == null) throw new KeyNotFoundException("Không tìm thấy đơn hàng.");
            if (cached.OrderStatus != "Cancelled" || cached.PaymentStatus != "RefundPending")
                throw new InvalidOperationException("Đơn này không chờ ghi nhận hoàn tiền.");

            _dao.Orders.UpdateOrderStatus(id, "Cancelled", "Refunded");
            cached.PaymentStatus = "Refunded";
        }
    }

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
