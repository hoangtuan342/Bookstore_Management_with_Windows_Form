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
        var dao = BookStoreDao.Create();
        var result = new AuthenticationService(dao, passwordHasher).LoginCustomer(new LoginCommandDto(userNameOrEmail, password));
        return SetSession(result, "Tên đăng nhập hoặc mật khẩu không hợp lệ cho khu vực khách hàng.");
    }

    public StoreApiResultDto AdminLogin(string userNameOrEmail, string password)
    {
        var dao = BookStoreDao.Create();
        var result = new AuthenticationService(dao, passwordHasher).LoginEmployee(new LoginCommandDto(userNameOrEmail, password));
        return SetSession(result, "Thông tin đăng nhập không hợp lệ hoặc tài khoản không có quyền nhân viên.");
    }

    public bool HasManagerAccount()
    {
        var dao = BookStoreDao.Create();
        return new AuthenticationService(dao, passwordHasher).HasManagerAccount();
    }

    public StoreApiResultDto SetupFirstManager(RegisterAccountCommandDto command)
    {
        var dao = BookStoreDao.Create();
        var result = new AuthenticationService(dao, passwordHasher).SetupFirstManager(command);
        return SetSession(result, "Không thể tạo tài khoản quản lý đầu tiên.");
    }

    private StoreApiResultDto SetSession(OperationResult<AuthenticatedAccountDto> result, string fallback)
    {
        if (!result.Succeeded || result.Value is null) return new(false, result.Message ?? fallback);
        AccountId = result.Value.Id;
        CustomerId = result.Value.CustomerId;
        Role = result.Value.Role;
        return new(true, $"Xin chào {result.Value.UserName}!", Role);
    }

    public void SignOut()
    {
        CustomerId = null;
        Role = null;
        AccountId = null;
    }
}
