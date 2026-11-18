
namespace TrangSach.Shared.DTO
{
    public class ManagerPublisherDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }

        public ManagerPublisherDto()
        {
        }

        public ManagerPublisherDto(int id, string name, string? address)
        {
            this.Id = id;
            this.Name = name;
            this.Address = address;
        }
    }
}

