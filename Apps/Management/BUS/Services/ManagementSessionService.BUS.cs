using TrangSach.Shared.DTO;
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
        var user = dao.Users.FindAccountWithDetails(userNameOrEmail);
        if (user == null || !passwordHasher.Verify(password, user.Value.PasswordHash))
            return new(false, "Tên đăng nhập hoặc mật khẩu không hợp lệ cho khu vực khách hàng.");
        if (user.Value.Status != "Active")
            return new(false, "Tài khoản đang bị khóa.");
        if (user.Value.RoleName != "Customer")
            return new(false, "Tài khoản không phải là khách hàng.");

        CustomerId = user.Value.CustomerId;
        Role = user.Value.RoleName;
        AccountId = user.Value.Id;
        return new(true, "Đăng nhập thành công.");
    }

    public StoreApiResultDto AdminLogin(string userNameOrEmail, string password)
    {
        var dao = BookStoreDao.Create();
        var user = dao.Users.FindAccountWithDetails(userNameOrEmail);
        if (user == null || !passwordHasher.Verify(password, user.Value.PasswordHash))
            return new(false, "Thông tin đăng nhập không hợp lệ hoặc tài khoản không có quyền nhân viên.");
        if (user.Value.Status != "Active" || user.Value.EmployeeStatus == "Inactive")
            return new(false, "Tài khoản nhân viên đang bị tạm khóa hoặc ngừng hoạt động.");
        if (user.Value.RoleName != "Admin" && user.Value.RoleName != "Staff" && user.Value.RoleName != "InventoryManager")
            return new(false, "Tài khoản không có quyền nhân viên.");

        CustomerId = user.Value.EmployeeId;
        Role = user.Value.RoleName;
        AccountId = user.Value.Id;
        return new(true, "Đăng nhập thành công.");
    }

    public bool HasManagerAccount()
    {
        var dao = BookStoreDao.Create();
        return dao.Users.CountAccountsByRole("Admin") > 0;
    }

    public StoreApiResultDto SetupFirstManager(RegisterAccountCommandDto command)
    {
        var dao = BookStoreDao.Create();
        if (dao.Users.CountAccountsByRole("Admin") > 0)
            return new(false, "Hệ thống đã có tài khoản quản lý.");
        var roleId = dao.Users.GetRoleIdByName("Admin") ?? 1;
        var hash = passwordHasher.Hash(command.UserName.Trim(), command.Password);
        var accId = dao.Users.CreateAccount(command.UserName.Trim(), command.Email.Trim(), hash, roleId, "Active");
        dao.Users.InsertEmployee($"NV{DateTime.UtcNow:yyyyMMddHHmmss}", "Quản trị", "Viên", null, null, null, "Active", 0, accId);
        return new(true, "Tạo tài khoản quản trị thành công.");
    }

    public void SignOut()
    {
        CustomerId = null;
        Role = null;
        AccountId = null;
    }

}
