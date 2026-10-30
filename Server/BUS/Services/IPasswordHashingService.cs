using BookStore.Domain.Entities;

namespace TrangSach.BUS.Services;

/// <summary>Application-level abstraction so BUS rules do not depend on an identity framework.</summary>
public interface IPasswordHashingService
{
    string Hash(UserAccount account, string password);
    bool Verify(UserAccount account, string password);
}
