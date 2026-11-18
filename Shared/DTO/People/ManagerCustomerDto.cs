
namespace TrangSach.Shared.DTO
{
    public class ManagerCustomerDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public int? AccountId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? AccountStatus { get; set; }

        public ManagerCustomerDto()
        {
        }

        public ManagerCustomerDto(string id, string fullName, string? phoneNumber, string? address, int? accountId, string? userName, string? email, string? accountStatus)
        {
            this.Id = id;
            this.FullName = fullName;
            this.PhoneNumber = phoneNumber;
            this.Address = address;
            this.AccountId = accountId;
            this.UserName = userName;
            this.Email = email;
            this.AccountStatus = accountStatus;
        }
    }
}

