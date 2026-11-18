
namespace TrangSach.Shared.DTO
{
    public class UpdateAdminOrderDto
    {
        public string ShippingAddress { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;

        public UpdateAdminOrderDto()
        {
        }

        public UpdateAdminOrderDto(string shippingAddress, string receiverPhone, string paymentMethod)
        {
            this.ShippingAddress = shippingAddress;
            this.ReceiverPhone = receiverPhone;
            this.PaymentMethod = paymentMethod;
        }
    }
}

