
namespace TrangSach.Shared.DTO
{
    public class CreateReceiptDto
    {
        public string SupplierId { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        public List<CreateReceiptLineDto> Lines { get; set; } = new List<CreateReceiptLineDto>();

        public CreateReceiptDto()
        {
        }

        public CreateReceiptDto(string supplierId, List<CreateReceiptLineDto> lines, string? employeeId = null)
        {
            this.SupplierId = supplierId;
            this.Lines = lines;
            this.EmployeeId = employeeId;
        }
    }
}

