
namespace TrangSach.Shared.DTO
{
    public class UpdateAdminOrderDto
    {
        public string ShippingAddress { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentStatus { get; set; }
        public string? EmployeeId { get; set; }
        public string? CustomerName { get; set; }
        public List<CreateAdminOrderLineDto>? Lines { get; set; }

        public UpdateAdminOrderDto()
        {
        }

        public UpdateAdminOrderDto(string shippingAddress, string receiverPhone, string paymentMethod)
        {
            this.ShippingAddress = shippingAddress;
            this.ReceiverPhone = receiverPhone;
            this.PaymentMethod = paymentMethod;
        }

        public UpdateAdminOrderDto(string shippingAddress, string receiverPhone, string paymentMethod, string? paymentStatus, List<CreateAdminOrderLineDto>? lines, string? employeeId = null, string? customerName = null)
        {
            this.ShippingAddress = shippingAddress;
            this.ReceiverPhone = receiverPhone;
            this.PaymentMethod = paymentMethod;
            this.PaymentStatus = paymentStatus;
            this.Lines = lines;
            this.EmployeeId = employeeId;
            this.CustomerName = customerName;
        }
    }
}

