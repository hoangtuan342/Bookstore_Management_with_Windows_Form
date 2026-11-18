
namespace TrangSach.Shared.DTO
{
    public class CreateSupplierDto
    {
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        public CreateSupplierDto()
        {
        }

        public CreateSupplierDto(string name, string? phoneNumber)
        {
            this.Name = name;
            this.PhoneNumber = phoneNumber;
        }
    }
}

