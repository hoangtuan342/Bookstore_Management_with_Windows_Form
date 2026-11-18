
namespace TrangSach.Shared.DTO
{
    public class CategoryOptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public CategoryOptionDto()
        {
        }

        public CategoryOptionDto(string id, string name)
        {
            this.Id = id;
            this.Name = name;
        }
    }
}

