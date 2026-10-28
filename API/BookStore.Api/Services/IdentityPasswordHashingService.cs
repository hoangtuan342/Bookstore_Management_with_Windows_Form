using BookStore.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using TrangSach.BUS.Services;

namespace BookStore.Api.Services;

public sealed class IdentityPasswordHashingService : IPasswordHashingService
{
    private readonly PasswordHasher<UserAccount> hasher = new();
    public string Hash(UserAccount account, string password) => hasher.HashPassword(account, password);
    public bool Verify(UserAccount account, string password) =>
        hasher.VerifyHashedPassword(account, account.PasswordHash, password) != PasswordVerificationResult.Failed;
}
