
namespace TrangSach.Shared.DTO
{
    public class SavePublisherDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }

        public SavePublisherDto()
        {
        }

        public SavePublisherDto(string name, string? address)
        {
            this.Name = name;
            this.Address = address;
        }
    }
}

