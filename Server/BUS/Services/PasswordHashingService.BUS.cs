using Microsoft.AspNetCore.Identity;

namespace TrangSach.BUS
{
    /// <summary>Business services use this contract to hash and verify account passwords.</summary>
    public interface IPasswordHashingService
    {
        string Hash(string username, string password);
        bool Verify(string passwordHash, string password);
    }

    /// <summary>Hashes and verifies account passwords for the server-side business services.</summary>
    public class PasswordHashingService : IPasswordHashingService
    {
        private readonly PasswordHasher<string> _hasher = new PasswordHasher<string>();

        public string Hash(string username, string password)
        {
            return _hasher.HashPassword(username, password);
        }

        public bool Verify(string passwordHash, string password)
        {
            return _hasher.VerifyHashedPassword(string.Empty, passwordHash, password) != PasswordVerificationResult.Failed;
        }
    }
}
