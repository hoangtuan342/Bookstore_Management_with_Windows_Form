
namespace TrangSach.Shared.DTO
{
    public class ManagerEmployeeDto
    {
        public string Id { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public DateOnly? BirthDate { get; set; }
        public string? AvatarPath { get; set; }
        public string? Address { get; set; }
        public string AccountStatus { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public int? AccountId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? LoginStatus { get; set; }
        public string? RoleName { get; set; }

        public ManagerEmployeeDto()
        {
        }

        public ManagerEmployeeDto(string id, string lastName, string firstName, DateOnly? birthDate, string? avatarPath, string? address, string accountStatus, decimal baseSalary, int? accountId, string? userName, string? email, string? loginStatus, string? roleName)
        {
            this.Id = id;
            this.LastName = lastName;
            this.FirstName = firstName;
            this.BirthDate = birthDate;
            this.AvatarPath = avatarPath;
            this.Address = address;
            this.AccountStatus = accountStatus;
            this.BaseSalary = baseSalary;
            this.AccountId = accountId;
            this.UserName = userName;
            this.Email = email;
            this.LoginStatus = loginStatus;
            this.RoleName = roleName;
        }
    }
}

