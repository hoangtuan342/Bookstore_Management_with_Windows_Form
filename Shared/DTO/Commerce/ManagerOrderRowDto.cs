
namespace TrangSach.Shared.DTO
{
    public class ManagerOrderRowDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public IReadOnlyList<ManagerOrderLineDto> Lines { get; set; } = new List<ManagerOrderLineDto>();

        public ManagerOrderRowDto()
        {
        }

        public ManagerOrderRowDto(string id, DateTime createdAt, string orderStatus, string paymentStatus, string paymentMethod, decimal totalAmount, string customerName, string employeeName, string receiverPhone, string shippingAddress, int itemCount, IReadOnlyList<ManagerOrderLineDto> lines)
        {
            this.Id = id;
            this.CreatedAt = createdAt;
            this.OrderStatus = orderStatus;
            this.PaymentStatus = paymentStatus;
            this.PaymentMethod = paymentMethod;
            this.TotalAmount = totalAmount;
            this.CustomerName = customerName;
            this.EmployeeName = employeeName;
            this.ReceiverPhone = receiverPhone;
            this.ShippingAddress = shippingAddress;
            this.ItemCount = itemCount;
            this.Lines = lines;
        }
    }
}

