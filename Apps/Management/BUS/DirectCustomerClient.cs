using BookStore.Shared.Models;
using TrangSach.BUS.Models;
using TrangSach.BUS.Services;
using TrangSach.DAO.Entities;

namespace TrangSach.Management.BUS;

/// <summary>WinForms-facing customer application service. All persistence and rules are delegated to BUS services.</summary>
public sealed class DirectCustomerClient(IPasswordHashingService passwordHasher)
{
    public string? CustomerId { get; private set; }
    public string? Role { get; private set; }
    public int? AccountId { get; private set; }

    public async Task<StoreApiResult> LoginAsync(string userNameOrEmail, string password, CancellationToken cancellationToken = default)
    {
        await using var dao = BookStoreDao.Create();
        var result = await new AuthenticationService(dao, passwordHasher).LoginCustomerAsync(new LoginCommand(userNameOrEmail, password), cancellationToken);
        return SetSession(result, "Tên đăng nhập hoặc mật khẩu không hợp lệ cho khu vực khách hàng.");
    }

    public async Task<StoreApiResult> AdminLoginAsync(string userNameOrEmail, string password, CancellationToken cancellationToken = default)
    {
        await using var dao = BookStoreDao.Create();
        var result = await new AuthenticationService(dao, passwordHasher).LoginEmployeeAsync(new LoginCommand(userNameOrEmail, password), cancellationToken);
        return SetSession(result, "Thông tin đăng nhập không hợp lệ hoặc tài khoản không có quyền nhân viên.");
    }

    public async Task<bool> HasManagerAccountAsync(CancellationToken cancellationToken = default)
    {
        await using var dao = BookStoreDao.Create();
        return await new AuthenticationService(dao, passwordHasher).HasManagerAccountAsync(cancellationToken);
    }

    public async Task<StoreApiResult> SetupFirstManagerAsync(RegisterAccountCommand command, CancellationToken cancellationToken = default)
    {
        await using var dao = BookStoreDao.Create();
        var result = await new AuthenticationService(dao, passwordHasher).SetupFirstManagerAsync(command, cancellationToken);
        return SetSession(result, "Không thể tạo tài khoản quản lý đầu tiên.");
    }

    public async Task<StoreApiResult> RegisterAsync(string userName, string email, string password, string fullName, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        await using var dao = BookStoreDao.Create();
        var result = await new AuthenticationService(dao, passwordHasher).RegisterAsync(
            new RegisterAccountCommand(userName, email, password, fullName, phoneNumber), cancellationToken);
        var response = SetSession(result, "Tên đăng nhập hoặc email đã được sử dụng.");
        return response.Succeeded ? response with { Message = "Tạo tài khoản thành công." } : response;
    }

    private StoreApiResult SetSession(TrangSach.BUS.Results.OperationResult<AuthenticatedAccount> result, string fallback)
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

    public async Task<IReadOnlyList<StoreCartLine>> GetCartAsync(CancellationToken cancellationToken = default)
    {
        var customerId = RequireCustomer();
        await using var dao = BookStoreDao.Create();
        return await new CustomerCommerceService(dao, new BookPricingService(dao)).GetCartAsync(customerId, cancellationToken);
    }

    public async Task SetCartQuantityAsync(string bookId, int quantity, CancellationToken cancellationToken = default)
    {
        var customerId = RequireCustomer();
        await using var dao = BookStoreDao.Create();
        var result = await new CustomerCommerceService(dao, new BookPricingService(dao)).SetCartQuantityAsync(customerId, bookId, quantity, cancellationToken);
        if (!result.Succeeded) throw new InvalidOperationException(result.Message ?? "Không thể cập nhật giỏ hàng.");
    }

    public async Task<IReadOnlyList<StoreOrder>> GetMyOrdersAsync(CancellationToken cancellationToken = default)
    {
        var customerId = RequireCustomer();
        await using var dao = BookStoreDao.Create();
        return await new CustomerCommerceService(dao, new BookPricingService(dao)).GetOrdersAsync(customerId, cancellationToken);
    }

    public async Task<StoreOrder> CheckoutAsync(CheckoutDetails details, CancellationToken cancellationToken = default)
    {
        var customerId = RequireCustomer();
        await using var dao = BookStoreDao.Create();
        var result = await new CustomerCommerceService(dao, new BookPricingService(dao)).CheckoutAsync(customerId, details, cancellationToken);
        if (!result.Succeeded || result.Value is null) throw new InvalidOperationException(result.Message ?? "Không thể tạo đơn hàng.");
        return result.Value;
    }

    private string RequireCustomer() => Role == "Customer" && CustomerId is not null
        ? CustomerId : throw new UnauthorizedAccessException("Vui lòng đăng nhập bằng tài khoản khách hàng.");
}

public sealed class DirectBookCatalog
{
    public async Task<IReadOnlyList<BookItem>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        await using var dao = BookStoreDao.Create();
        return await new BookCatalogService(dao, new BookPricingService(dao)).GetBooksAsync(cancellationToken: cancellationToken);
    }
}
