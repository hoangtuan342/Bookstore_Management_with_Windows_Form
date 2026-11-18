
namespace TrangSach.Shared.DTO
{
    public class ManagerCategoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public ManagerCategoryDto()
        {
        }

        public ManagerCategoryDto(string id, string name)
        {
            this.Id = id;
            this.Name = name;
        }
    }
}

