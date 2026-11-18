
namespace TrangSach.Shared.DTO
{
    public class RegisterAccountCommandDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        public RegisterAccountCommandDto()
        {
        }

        public RegisterAccountCommandDto(string userName, string email, string password, string fullName, string? phoneNumber)
        {
            this.UserName = userName;
            this.Email = email;
            this.Password = password;
            this.FullName = fullName;
            this.PhoneNumber = phoneNumber;
        }
    }
}

