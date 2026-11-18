
namespace TrangSach.Shared.DTO
{
    public class ReceiptCreatedDto
    {
        public string Id { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }

        public ReceiptCreatedDto()
        {
        }

        public ReceiptCreatedDto(string id, decimal totalAmount, DateTime createdAt)
        {
            this.Id = id;
            this.TotalAmount = totalAmount;
            this.CreatedAt = createdAt;
        }
    }
}

