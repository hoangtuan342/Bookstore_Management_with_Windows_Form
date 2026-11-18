
namespace TrangSach.Shared.DTO
{
    public class SaveAuthorDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Biography { get; set; }

        public SaveAuthorDto()
        {
        }

        public SaveAuthorDto(string name, string? biography)
        {
            this.Name = name;
            this.Biography = biography;
        }
    }
}

