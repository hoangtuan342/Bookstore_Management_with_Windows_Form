using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using TrangSach.Shared.DTO;

namespace TrangSach.BUS
{
    public class CustomerCommerceService
    {
        private readonly BookStoreDao dao;
        private readonly BookPricingService pricing;

        public CustomerCommerceService(BookStoreDao dao, BookPricingService pricing)
        {
            this.dao = dao;
            this.pricing = pricing;
        }

        public List<StoreCartLineDto> GetCart(string customerId)
        {
            var cart = dao.Users.GetCustomerCart(customerId);
            var discounts = pricing.GetActiveDiscounts(cart.Select(x => x.BookId).ToList(), DateTime.UtcNow);
            return cart.Select(x =>
            {
                decimal discount = discounts.ContainsKey(x.BookId) ? discounts[x.BookId] : 0;
                var unit = BookPricingService.ApplyDiscount(x.SalePrice, discount);
                return new StoreCartLineDto(x.BookId, x.Title, x.Author, unit, x.Quantity, x.AvailableStock, unit * x.Quantity);
            }).ToList();
        }

        public OperationResult<Unit> SetCartQuantity(string customerId, string bookId, int quantity)
        {
            var book = dao.Catalog.GetBookForCart(bookId);
            if (book == null)
            {
                return OperationResult<Unit>.NotFound("Không tìm thấy sách.");
            }
            if (quantity < 1 || quantity > 99)
            {
                return OperationResult<Unit>.Invalid("Số lượng phải từ 1 đến 99.");
            }
            if (quantity > book.Value.Stock)
            {
                return OperationResult<Unit>.Conflict("Số lượng vượt quá tồn kho hiện tại.");
            }

            dao.Users.SetCartItem(customerId, bookId, quantity);
            return OperationResult<Unit>.Success(new Unit());
        }

        public void RemoveCartItem(string customerId, string bookId)
        {
            dao.Users.RemoveCartItem(customerId, bookId);
        }

        public List<StoreOrderDto> GetOrders(string customerId)
        {
            return dao.Orders.GetCustomerOrders(customerId);
        }

        public StoreOrderDto? GetOrder(string orderId, string customerId)
        {
            return dao.Orders.FindCustomerOrder(orderId, customerId);
        }

        public OperationResult<StoreOrderDto> Checkout(string customerId, CheckoutDetailsDto command)
        {
            var payment = command.PaymentMethod.Trim();
            if (payment != "CashOnDelivery" && payment != "BankTransfer")
            {
                return OperationResult<StoreOrderDto>.Invalid("Phương thức thanh toán hiện hỗ trợ COD hoặc chuyển khoản mô phỏng.");
            }
            if (command.ShippingAddress.Trim().Length < 8 || command.ShippingAddress.Trim().Length > 500
                || !new PhoneAttribute().IsValid(command.ReceiverPhone) || command.ReceiverPhone.Length > 30
                || (command.VoucherCode != null && command.VoucherCode.Length > 40))
            {
                return OperationResult<StoreOrderDto>.Invalid("Địa chỉ giao hàng hoặc số điện thoại không hợp lệ.");
            }

            var cart = dao.Users.GetCustomerCart(customerId);
            if (cart.Count == 0)
            {
                return OperationResult<StoreOrderDto>.Invalid("Giỏ hàng đang trống.");
            }

            var unavailable = cart.FirstOrDefault(x => x.AvailableStock < x.Quantity);
            if (unavailable.BookId != null)
            {
                return OperationResult<StoreOrderDto>.Conflict($"Sách '{unavailable.Title}' không đủ tồn kho (còn {unavailable.AvailableStock}).");
            }

            var now = DateTime.UtcNow;
            var discounts = pricing.GetActiveDiscounts(cart.Select(x => x.BookId).ToList(), now);

            var lines = cart.Select(x =>
            {
                decimal discount = discounts.ContainsKey(x.BookId) ? discounts[x.BookId] : 0;
                var unit = BookPricingService.ApplyDiscount(x.SalePrice, discount);
                return (x.BookId, x.Quantity, unit, unit * x.Quantity);
            }).ToList();

            var subtotal = lines.Sum(x => x.Item4);
            var orderRate = pricing.GetOrderDiscount(subtotal, now);

            decimal voucherRate = 0;
            string? voucherCode = command.VoucherCode?.Trim();
            if (!string.IsNullOrWhiteSpace(voucherCode))
            {
                var v = dao.Promotions.FindVoucher(voucherCode);
                if (v == null || v.Status != "Active" || (v.ExpiresAt.HasValue && v.ExpiresAt.Value < now)
                    || (v.MaxUsageCount.HasValue && v.UsageCount >= v.MaxUsageCount.Value))
                {
                    return OperationResult<StoreOrderDto>.Invalid("Voucher không tồn tại, đã hết hạn hoặc đã hết lượt sử dụng.");
                }
                voucherRate = pricing.GetVoucherDiscount(voucherCode, now);
                if (voucherRate <= 0)
                {
                    return OperationResult<StoreOrderDto>.Invalid("Voucher hiện không áp dụng được.");
                }
            }

            var appliedRate = Math.Max(orderRate, voucherRate);
            var orderDiscount = decimal.Round(subtotal * appliedRate / 100m, 2, MidpointRounding.AwayFromZero);
            var itemDiscount = cart.Sum(x =>
            {
                decimal discount = discounts.ContainsKey(x.BookId) ? discounts[x.BookId] : 0;
                return (x.SalePrice - BookPricingService.ApplyDiscount(x.SalePrice, discount)) * x.Quantity;
            });

            string? finalVoucherId = (voucherRate >= orderRate && voucherRate > 0) ? voucherCode : null;
            var orderId = $"HD{now:yyyyMMddHHmmss}{Convert.ToHexString(RandomNumberGenerator.GetBytes(3))}";
            var totalAmount = subtotal - orderDiscount;
            var totalDiscount = itemDiscount + orderDiscount;
            var payStatus = payment == "CashOnDelivery" ? "Unpaid" : "AwaitingPayment";

            dao.Orders.InsertOrder(orderId, now, customerId, null, finalVoucherId,
                totalAmount, totalDiscount, "Pending", payStatus, payment,
                command.ShippingAddress.Trim(), command.ReceiverPhone.Trim(), lines);

            dao.Users.ClearCart(customerId);

            var storeLines = cart.Select(x =>
            {
                decimal discount = discounts.ContainsKey(x.BookId) ? discounts[x.BookId] : 0;
                var unit = BookPricingService.ApplyDiscount(x.SalePrice, discount);
                return new StoreOrderLineDto(x.BookId, x.Title, x.Quantity, unit, unit * x.Quantity);
            }).ToList();

            var orderDto = new StoreOrderDto(
                orderId, now, "Pending", payStatus, payment,
                command.ShippingAddress.Trim(), command.ReceiverPhone.Trim(),
                totalAmount, totalDiscount, storeLines);

            return OperationResult<StoreOrderDto>.Created(orderDto);
        }

        public OperationResult<Unit> CancelOrder(string id, string customerId)
        {
            var order = dao.Orders.FindCustomerOrder(id, customerId);
            if (order == null)
            {
                return OperationResult<Unit>.NotFound();
            }
            if (order.Status != "Pending" && order.Status != "Confirmed")
            {
                return OperationResult<Unit>.Conflict("Chỉ có thể hủy đơn đang chờ xác nhận hoặc đã xác nhận, trước khi bắt đầu giao.");
            }

            dao.Orders.RestoreStock(id);
            dao.Orders.DecrementVoucherUsage(id);

            var newPayStatus = order.PaymentStatus == "Paid" ? "RefundPending" : "Cancelled";
            dao.Orders.UpdateOrderStatus(id, "Cancelled", newPayStatus);

            return OperationResult<Unit>.Success(new Unit());
        }
    }
}
