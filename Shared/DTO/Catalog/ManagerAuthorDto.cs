
namespace TrangSach.Shared.DTO
{
    public class ManagerAuthorDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Biography { get; set; }

        public ManagerAuthorDto()
        {
        }

        public ManagerAuthorDto(int id, string name, string? biography)
        {
            this.Id = id;
            this.Name = name;
            this.Biography = biography;
        }
    }
}

