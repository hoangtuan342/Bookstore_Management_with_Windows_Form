
namespace TrangSach.Shared.DTO
{
    public class ManagerReceiptDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string SupplierId { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public List<ManagerReceiptLineDto> Lines { get; set; } = new List<ManagerReceiptLineDto>();

        public ManagerReceiptDto()
        {
        }

        public ManagerReceiptDto(string id, DateTime createdAt, string status, string supplierId, string supplierName, decimal totalAmount, List<ManagerReceiptLineDto> lines)
        {
            this.Id = id;
            this.CreatedAt = createdAt;
            this.Status = status;
            this.SupplierId = supplierId;
            this.SupplierName = supplierName;
            this.TotalAmount = totalAmount;
            this.Lines = lines;
        }
    }
}

