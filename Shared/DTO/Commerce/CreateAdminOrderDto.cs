
namespace TrangSach.Shared.DTO
{
    public class CreateAdminOrderDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? EmployeeId { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public List<CreateAdminOrderLineDto> Lines { get; set; } = new List<CreateAdminOrderLineDto>();

        public CreateAdminOrderDto()
        {
        }

        public CreateAdminOrderDto(string customerId, string shippingAddress, string receiverPhone, string paymentMethod, List<CreateAdminOrderLineDto> lines)
        {
            this.CustomerId = customerId;
            this.ShippingAddress = shippingAddress;
            this.ReceiverPhone = receiverPhone;
            this.PaymentMethod = paymentMethod;
            this.Lines = lines;
        }

        public CreateAdminOrderDto(string customerId, string? employeeId, string shippingAddress, string receiverPhone, string paymentMethod, List<CreateAdminOrderLineDto> lines, string? customerName = null)
        {
            this.CustomerId = customerId;
            this.EmployeeId = employeeId;
            this.ShippingAddress = shippingAddress;
            this.ReceiverPhone = receiverPhone;
            this.PaymentMethod = paymentMethod;
            this.Lines = lines;
            this.CustomerName = customerName;
        }
    }
}

