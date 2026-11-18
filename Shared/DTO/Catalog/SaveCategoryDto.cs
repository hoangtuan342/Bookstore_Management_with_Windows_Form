
namespace TrangSach.Shared.DTO
{
    public class SaveCategoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public SaveCategoryDto()
        {
        }

        public SaveCategoryDto(string id, string name)
        {
            this.Id = id;
            this.Name = name;
        }
    }
}

