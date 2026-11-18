
namespace TrangSach.Shared.DTO
{
    public class SaveEmployeeDto
    {
        public string? Id { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public DateOnly? BirthDate { get; set; }
        public string? AvatarPath { get; set; }
        public string? Address { get; set; }
        public string AccountStatus { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? AccountLoginStatus { get; set; }
        public string RoleName { get; set; } = string.Empty;

        public SaveEmployeeDto()
        {
        }

        public SaveEmployeeDto(string? id, string lastName, string firstName, DateOnly? birthDate, string? avatarPath, string? address, string accountStatus, decimal baseSalary, string? userName, string? email, string? password, string? accountLoginStatus, string roleName)
        {
            this.Id = id;
            this.LastName = lastName;
            this.FirstName = firstName;
            this.BirthDate = birthDate;
            this.AvatarPath = avatarPath;
            this.Address = address;
            this.AccountStatus = accountStatus;
            this.BaseSalary = baseSalary;
            this.UserName = userName;
            this.Email = email;
            this.Password = password;
            this.AccountLoginStatus = accountLoginStatus;
            this.RoleName = roleName;
        }
    }
}

