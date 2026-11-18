
namespace TrangSach.Shared.DTO
{
    public class AccountProfileDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ProfileId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }

        public AccountProfileDto()
        {
        }

        public AccountProfileDto(string userName, string email, string role, string profileId, string fullName, string? phoneNumber, string? address)
        {
            this.UserName = userName;
            this.Email = email;
            this.Role = role;
            this.ProfileId = profileId;
            this.FullName = fullName;
            this.PhoneNumber = phoneNumber;
            this.Address = address;
        }
    }
}

