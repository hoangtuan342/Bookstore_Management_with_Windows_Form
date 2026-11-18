
namespace TrangSach.Shared.DTO
{
    public class StoreOrderDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public List<StoreOrderLineDto> Lines { get; set; } = new List<StoreOrderLineDto>();

        public StoreOrderDto()
        {
        }

        public StoreOrderDto(string id, DateTime createdAt, string status, string paymentStatus, string paymentMethod, string shippingAddress, string receiverPhone, decimal totalAmount, decimal discountAmount, List<StoreOrderLineDto> lines)
        {
            this.Id = id;
            this.CreatedAt = createdAt;
            this.Status = status;
            this.PaymentStatus = paymentStatus;
            this.PaymentMethod = paymentMethod;
            this.ShippingAddress = shippingAddress;
            this.ReceiverPhone = receiverPhone;
            this.TotalAmount = totalAmount;
            this.DiscountAmount = discountAmount;
            this.Lines = lines;
        }
    }
}

