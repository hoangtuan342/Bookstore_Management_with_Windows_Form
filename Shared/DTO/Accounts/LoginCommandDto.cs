
namespace TrangSach.Shared.DTO
{
    public class LoginCommandDto
    {
        public string UserNameOrEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public LoginCommandDto()
        {
        }

        public LoginCommandDto(string userNameOrEmail, string password)
        {
            this.UserNameOrEmail = userNameOrEmail;
            this.Password = password;
        }
    }
}

