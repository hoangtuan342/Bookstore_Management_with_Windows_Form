
namespace TrangSach.Shared.DTO
{
    public class IdNameOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public IdNameOptionDto()
        {
        }

        public IdNameOptionDto(int id, string name)
        {
            this.Id = id;
            this.Name = name;
        }
    }
}

