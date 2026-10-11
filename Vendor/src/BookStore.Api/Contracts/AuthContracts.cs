using System.ComponentModel.DataAnnotations;

namespace BookStore.Api.Contracts;

public sealed class RegisterRequest
{
    [Required, StringLength(80, MinimumLength = 3)]
    public string UserName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [Required, StringLength(180, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [StringLength(30)]
    public string? PhoneNumber { get; init; }
}

public sealed class LoginRequest
{
    [Required]
    public string UserNameOrEmail { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string UserName,
    string Role,
    string? CustomerId);
