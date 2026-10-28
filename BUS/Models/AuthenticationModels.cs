namespace TrangSach.BUS.Models;

public sealed record RegisterAccountCommand(string UserName, string Email, string Password, string FullName, string? PhoneNumber);
public sealed record LoginCommand(string UserNameOrEmail, string Password);
public sealed record AuthenticatedAccount(int Id, string UserName, string Email, string Role, string? CustomerId);
public sealed record AccountProfile(string UserName, string Email, string Role, string ProfileId, string FullName, string? PhoneNumber, string? Address);
