
namespace TrangSach.Shared.DTO
{
    public class CreateAdminOrderDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public IReadOnlyList<CreateAdminOrderLineDto> Lines { get; set; } = new List<CreateAdminOrderLineDto>();

        public CreateAdminOrderDto()
        {
        }

        public CreateAdminOrderDto(string customerId, string shippingAddress, string receiverPhone, string paymentMethod, IReadOnlyList<CreateAdminOrderLineDto> lines)
        {
            this.CustomerId = customerId;
            this.ShippingAddress = shippingAddress;
            this.ReceiverPhone = receiverPhone;
            this.PaymentMethod = paymentMethod;
            this.Lines = lines;
        }
    }
}

