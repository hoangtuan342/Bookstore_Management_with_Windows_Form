using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;
using TrangSach.BUS;

namespace TrangSach.Management.BUS;

/// <summary>WinForms-facing customer application service. All persistence and rules are delegated to BUS services.</summary>
public sealed class ManagementSessionService(IPasswordHashingService passwordHasher)
{
    public string? CustomerId { get; private set; }
    public string? Role { get; private set; }
    public int? AccountId { get; private set; }

    // Synchronous authentication methods
    public StoreApiResultDto Login(string userNameOrEmail, string password)
    {
        try
        {
            var dao = BookStoreDao.Create();
            var acc = new AuthenticationService(dao, passwordHasher).LoginCustomer(new LoginCommandDto(userNameOrEmail, password));
            return SetSession(acc);
        }
        catch (Exception ex)
        {
            return new(false, ex.Message);
        }
    }

    public StoreApiResultDto AdminLogin(string userNameOrEmail, string password)
    {
        try
        {
            var dao = BookStoreDao.Create();
            var acc = new AuthenticationService(dao, passwordHasher).LoginEmployee(new LoginCommandDto(userNameOrEmail, password));
            return SetSession(acc);
        }
        catch (Exception ex)
        {
            return new(false, ex.Message);
        }
    }

    public bool HasManagerAccount()
    {
        var dao = BookStoreDao.Create();
        return new AuthenticationService(dao, passwordHasher).HasManagerAccount();
    }

    public StoreApiResultDto SetupFirstManager(RegisterAccountCommandDto command)
    {
        try
        {
            var dao = BookStoreDao.Create();
            var acc = new AuthenticationService(dao, passwordHasher).SetupFirstManager(command);
            return SetSession(acc);
        }
        catch (Exception ex)
        {
            return new(false, ex.Message);
        }
    }

    private StoreApiResultDto SetSession(AuthenticatedAccountDto acc)
    {
        AccountId = acc.Id;
        CustomerId = acc.CustomerId;
        Role = acc.Role;
        return new(true, $"Xin chào {acc.UserName}!", Role);
    }

    public void SignOut()
    {
        CustomerId = null;
        Role = null;
        AccountId = null;
    }
}
