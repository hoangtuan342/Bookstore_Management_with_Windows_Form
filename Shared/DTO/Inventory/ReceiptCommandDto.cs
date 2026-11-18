
namespace TrangSach.Shared.DTO
{
    public class ReceiptCommandDto
    {
        public string SupplierId { get; set; } = string.Empty;
        public List<ReceiptLineCommandDto> Lines { get; set; } = new List<ReceiptLineCommandDto>();

        public ReceiptCommandDto()
        {
        }

        public ReceiptCommandDto(string supplierId, List<ReceiptLineCommandDto> lines)
        {
            this.SupplierId = supplierId;
            this.Lines = lines;
        }
    }
}

