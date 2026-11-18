
namespace TrangSach.Shared.DTO
{
    public class BookVisibilityCommandDto
    {
        public bool IsActive { get; set; }

        public BookVisibilityCommandDto()
        {
        }

        public BookVisibilityCommandDto(bool isActive)
        {
            this.IsActive = isActive;
        }
    }
}

