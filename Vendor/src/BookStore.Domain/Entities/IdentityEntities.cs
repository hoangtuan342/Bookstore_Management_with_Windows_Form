namespace BookStore.Domain.Entities;

public sealed class UserRole
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<UserAccount> Accounts { get; set; } = new List<UserAccount>();
}

public sealed class UserAccount
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public int RoleId { get; set; }
    public UserRole Role { get; set; } = null!;
    public Employee? Employee { get; set; }
    public Customer? Customer { get; set; }
}

public sealed class Employee
{
    public string Id { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string? AvatarPath { get; set; }
    public string? Address { get; set; }
    public string AccountStatus { get; set; } = "Active";
    public decimal BaseSalary { get; set; }
    public int? UserAccountId { get; set; }
    public UserAccount? UserAccount { get; set; }
}

public sealed class Customer
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int? UserAccountId { get; set; }
    public UserAccount? UserAccount { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ShippingAddress { get; set; }
    public ICollection<SalesOrder> Orders { get; set; } = new List<SalesOrder>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}

public sealed class Supplier
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}
