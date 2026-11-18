
namespace TrangSach.Shared.DTO
{
    public class SupplierOptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        public SupplierOptionDto()
        {
        }

        public SupplierOptionDto(string id, string name, string? phoneNumber)
        {
            this.Id = id;
            this.Name = name;
            this.PhoneNumber = phoneNumber;
        }
    }
}

