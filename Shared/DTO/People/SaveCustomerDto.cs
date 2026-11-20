
namespace TrangSach.Shared.DTO
{
    public class SaveCustomerDto
    {
        public string? Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateOnly? BirthDate { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? AccountStatus { get; set; }

        public SaveCustomerDto()
        {
        }

        public SaveCustomerDto(string? id, string fullName, string? phoneNumber, string? address, string? userName, string? email, string? password, string? accountStatus)
            : this(id, fullName, null, phoneNumber, address, userName, email, password, accountStatus)
        {
        }

        public SaveCustomerDto(string? id, string fullName, DateOnly? birthDate, string? phoneNumber, string? address, string? userName, string? email, string? password, string? accountStatus)
        {
            this.Id = id;
            this.FullName = fullName;
            this.BirthDate = birthDate;
            this.PhoneNumber = phoneNumber;
            this.Address = address;
            this.UserName = userName;
            this.Email = email;
            this.Password = password;
            this.AccountStatus = accountStatus;
        }
    }
}

