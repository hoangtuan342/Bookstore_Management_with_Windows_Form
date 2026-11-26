
namespace TrangSach.Shared.DTO
{
    public class ReceiptCommandDto
    {
        public string SupplierId { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        public List<ReceiptLineCommandDto> Lines { get; set; } = new List<ReceiptLineCommandDto>();

        public ReceiptCommandDto()
        {
        }

        public ReceiptCommandDto(string supplierId, List<ReceiptLineCommandDto> lines, string? employeeId = null)
        {
            this.SupplierId = supplierId;
            this.Lines = lines;
            this.EmployeeId = employeeId;
        }
    }
}

