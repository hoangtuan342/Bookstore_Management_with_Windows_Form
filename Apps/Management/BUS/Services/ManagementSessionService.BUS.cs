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

    public async Task<StoreApiResultDto> LoginAsync(string userNameOrEmail, string password, CancellationToken cancellationToken = default)
    {
        await using var dao = await BookStoreDao.CreateAsync();
        var result = await new AuthenticationService(dao, passwordHasher).LoginCustomerAsync(new LoginCommandDto(userNameOrEmail, password), cancellationToken);
        return SetSession(result, "Tên đăng nhập hoặc mật khẩu không hợp lệ cho khu vực khách hàng.");
    }

    public async Task<StoreApiResultDto> AdminLoginAsync(string userNameOrEmail, string password, CancellationToken cancellationToken = default)
    {
        await using var dao = await BookStoreDao.CreateAsync();
        var result = await new AuthenticationService(dao, passwordHasher).LoginEmployeeAsync(new LoginCommandDto(userNameOrEmail, password), cancellationToken);
        return SetSession(result, "Thông tin đăng nhập không hợp lệ hoặc tài khoản không có quyền nhân viên.");
    }

    public async Task<bool> HasManagerAccountAsync(CancellationToken cancellationToken = default)
    {
        await using var dao = await BookStoreDao.CreateAsync();
        return await new AuthenticationService(dao, passwordHasher).HasManagerAccountAsync(cancellationToken);
    }

    public async Task<StoreApiResultDto> SetupFirstManagerAsync(RegisterAccountCommandDto command, CancellationToken cancellationToken = default)
    {
        await using var dao = await BookStoreDao.CreateAsync();
        var result = await new AuthenticationService(dao, passwordHasher).SetupFirstManagerAsync(command, cancellationToken);
        return SetSession(result, "Không thể tạo tài khoản quản lý đầu tiên.");
    }

    public async Task<StoreApiResultDto> RegisterAsync(string userName, string email, string password, string fullName, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        await using var dao = await BookStoreDao.CreateAsync();
        var result = await new AuthenticationService(dao, passwordHasher).RegisterAsync(
            new RegisterAccountCommandDto(userName, email, password, fullName, phoneNumber), cancellationToken);
        var response = SetSession(result, "Tên đăng nhập hoặc email đã được sử dụng.");
        if (response.Succeeded) response.Message = "Tạo tài khoản thành công.";
        return response;
    }

    private StoreApiResultDto SetSession(TrangSach.BUS.OperationResult<AuthenticatedAccountDto> result, string fallback)
    {
        if (!result.Succeeded || result.Value is null) return new(false, result.Message ?? fallback);
        AccountId = result.Value.Id;
        CustomerId = result.Value.CustomerId;
        Role = result.Value.Role;
        return new(true, $"Xin chào {result.Value.UserName}!", Role);
    }

    public Task SignOutAsync()
    {
        CustomerId = null; AccountId = null; Role = null;
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<StoreCartLineDto>> GetCartAsync(CancellationToken cancellationToken = default)
    {
        var customerId = RequireCustomer();
        await using var dao = await BookStoreDao.CreateAsync();
        return await new CustomerCommerceService(dao, new BookPricingService(dao)).GetCartAsync(customerId, cancellationToken);
    }

    public async Task SetCartQuantityAsync(string bookId, int quantity, CancellationToken cancellationToken = default)
    {
        var customerId = RequireCustomer();
        await using var dao = await BookStoreDao.CreateAsync();
        var result = await new CustomerCommerceService(dao, new BookPricingService(dao)).SetCartQuantityAsync(customerId, bookId, quantity, cancellationToken);
        if (!result.Succeeded) throw new InvalidOperationException(result.Message ?? "Không thể cập nhật giỏ hàng.");
    }

    public async Task<IReadOnlyList<StoreOrderDto>> GetMyOrdersAsync(CancellationToken cancellationToken = default)
    {
        var customerId = RequireCustomer();
        await using var dao = await BookStoreDao.CreateAsync();
        return await new CustomerCommerceService(dao, new BookPricingService(dao)).GetOrdersAsync(customerId, cancellationToken);
    }

    public async Task<StoreOrderDto> CheckoutAsync(CheckoutDetailsDto details, CancellationToken cancellationToken = default)
    {
        var customerId = RequireCustomer();
        await using var dao = await BookStoreDao.CreateAsync();
        var result = await new CustomerCommerceService(dao, new BookPricingService(dao)).CheckoutAsync(customerId, details, cancellationToken);
        if (!result.Succeeded || result.Value is null) throw new InvalidOperationException(result.Message ?? "Không thể tạo đơn hàng.");
        return result.Value;
    }

    private string RequireCustomer() => Role == "Customer" && CustomerId is not null
        ? CustomerId : throw new UnauthorizedAccessException("Vui lòng đăng nhập bằng tài khoản khách hàng.");
}



