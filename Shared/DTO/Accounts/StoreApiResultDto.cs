
namespace TrangSach.Shared.DTO
{
    public class StoreApiResultDto
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Role { get; set; }

        public StoreApiResultDto()
        {
        }

        public StoreApiResultDto(bool succeeded, string message, string? role = null)
        {
            this.Succeeded = succeeded;
            this.Message = message;
            this.Role = role;
        }
    }
}

