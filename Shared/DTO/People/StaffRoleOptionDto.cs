
namespace TrangSach.Shared.DTO
{
    public class StaffRoleOptionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;

        public StaffRoleOptionDto()
        {
        }

        public StaffRoleOptionDto(string name, string label)
        {
            this.Name = name;
            this.Label = label;
        }
    }
}

