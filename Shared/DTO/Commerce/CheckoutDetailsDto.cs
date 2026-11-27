
namespace TrangSach.Shared.DTO
{
    public class CheckoutDetailsDto
    {
        public string? ReceiverName { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? VoucherCode { get; set; }

        public CheckoutDetailsDto()
        {
        }

        public CheckoutDetailsDto(string shippingAddress, string receiverPhone, string paymentMethod, string? voucherCode = null, string? receiverName = null)
        {
            this.ShippingAddress = shippingAddress;
            this.ReceiverPhone = receiverPhone;
            this.PaymentMethod = paymentMethod;
            this.VoucherCode = voucherCode;
            this.ReceiverName = receiverName;
        }
    }
}

