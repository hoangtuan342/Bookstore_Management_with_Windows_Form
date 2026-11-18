
namespace TrangSach.Shared.DTO
{
    public class StoreAuthResultDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? CustomerId { get; set; }

        public StoreAuthResultDto()
        {
        }

        public StoreAuthResultDto(string accessToken, DateTime expiresAtUtc, string userName, string role, string? customerId)
        {
            this.AccessToken = accessToken;
            this.ExpiresAtUtc = expiresAtUtc;
            this.UserName = userName;
            this.Role = role;
            this.CustomerId = customerId;
        }
    }
}

