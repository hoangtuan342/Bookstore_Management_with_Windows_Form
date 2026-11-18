
namespace TrangSach.Shared.DTO
{
    public class AuthenticatedAccountDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? CustomerId { get; set; }

        public AuthenticatedAccountDto()
        {
        }

        public AuthenticatedAccountDto(int id, string userName, string email, string role, string? customerId)
        {
            this.Id = id;
            this.UserName = userName;
            this.Email = email;
            this.Role = role;
            this.CustomerId = customerId;
        }
    }
}

