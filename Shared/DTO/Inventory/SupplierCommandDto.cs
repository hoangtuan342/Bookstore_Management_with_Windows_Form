
namespace TrangSach.Shared.DTO
{
    public class SupplierCommandDto
    {
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        public SupplierCommandDto()
        {
        }

        public SupplierCommandDto(string name, string? phoneNumber)
        {
            this.Name = name;
            this.PhoneNumber = phoneNumber;
        }
    }
}

