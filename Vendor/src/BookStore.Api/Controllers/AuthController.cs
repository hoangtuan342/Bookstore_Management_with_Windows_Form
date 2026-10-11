using System.IdentityModel.Tokens.Jwt;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BookStore.Api.Contracts;
using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BookStore.Shared.Security;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    BookStoreDbContext db,
    IPasswordHasher<UserAccount> passwordHasher,
    IConfiguration configuration,
    IHostEnvironment environment) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var userName = request.UserName.Trim();
        var email = request.Email.Trim();

        if (await db.Accounts.AnyAsync(account => account.UserName == userName || account.Email == email, cancellationToken))
        {
            return Conflict(new { message = "Tên đăng nhập hoặc email đã được sử dụng." });
        }

        var customerRole = await db.Roles.SingleAsync(role => role.Name == "Customer", cancellationToken);
        var account = new UserAccount
        {
            UserName = userName,
            Email = email,
            Status = "Active",
            RoleId = customerRole.Id
        };
        account.PasswordHash = passwordHasher.HashPassword(account, request.Password);

        var customer = new Customer
        {
            Id = $"KH{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}",
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            UserAccount = account
        };

        db.Customers.Add(customer);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Tên đăng nhập hoặc email đã được sử dụng." });
        }

        return StatusCode(StatusCodes.Status201Created, CreateToken(account, customer.Id, customerRole.Name));
    }

    [AllowAnonymous]
    [HttpPost("setup-manager")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> SetupFirstManager(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var managerRole = await db.Roles.SingleAsync(role => role.Name == "Manager", cancellationToken);
        if (await db.Accounts.AnyAsync(account => account.RoleId == managerRole.Id, cancellationToken))
        {
            return Conflict(new { message = "Tài khoản quản lý ban đầu đã được tạo." });
        }

        var userName = request.UserName.Trim();
        var email = request.Email.Trim();
        if (await db.Accounts.AnyAsync(account => account.UserName == userName || account.Email == email, cancellationToken))
        {
            return Conflict(new { message = "Tên đăng nhập hoặc email đã được sử dụng." });
        }

        var account = new UserAccount
        {
            UserName = userName,
            Email = email,
            Status = "Active",
            RoleId = managerRole.Id
        };
        account.PasswordHash = passwordHasher.HashPassword(account, request.Password);

        var nameParts = request.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var employee = new Employee
        {
            Id = $"NV{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}",
            LastName = nameParts.Length > 1 ? string.Join(' ', nameParts[..^1]) : string.Empty,
            FirstName = nameParts[^1],
            UserAccount = account
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, CreateToken(account, null, managerRole.Name));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return await LoginWithRoleAsync(request, StaffRoles.Customer, cancellationToken);
    }

    [AllowAnonymous]
    [HttpPost("admin-login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> AdminLogin(LoginRequest request, CancellationToken cancellationToken)
    {
        var identifier = request.UserNameOrEmail.Trim();
        var account = await FindAccountAsync(identifier, cancellationToken);
        if (!IsValidPassword(account, request.Password) || !StaffPermissions.IsEmployeeRole(account!.Role.Name))
        {
            return Unauthorized(new { message = "Thông tin đăng nhập không hợp lệ hoặc tài khoản không có quyền nhân viên." });
        }

        return Ok(CreateToken(account!, null, account!.Role.Name));
    }

    private async Task<ActionResult<AuthResponse>> LoginWithRoleAsync(LoginRequest request, string requiredRole, CancellationToken cancellationToken)
    {
        var identifier = request.UserNameOrEmail.Trim();
        var account = await FindAccountAsync(identifier, cancellationToken);

        if (!IsValidPassword(account, request.Password) || account!.Role.Name != requiredRole)
        {
            return Unauthorized(new { message = "Thông tin đăng nhập không hợp lệ cho khu vực khách hàng." });
        }

        return Ok(CreateToken(account!, account!.Customer?.Id, account!.Role.Name));
    }

    private Task<UserAccount?> FindAccountAsync(string identifier, CancellationToken cancellationToken) => db.Accounts
        .Include(item => item.Role)
        .Include(item => item.Customer)
        .Include(item => item.Employee)
        .SingleOrDefaultAsync(item => item.UserName == identifier || item.Email == identifier, cancellationToken);

    private bool IsValidPassword(UserAccount? account, string password) => account is not null
        && account.Status == "Active"
        && (account.Employee is null || account.Employee.AccountStatus == "Active")
        && passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password) != PasswordVerificationResult.Failed;

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId))
        {
            return Unauthorized();
        }

        var account = await db.Accounts.AsNoTracking()
            .Include(item => item.Role)
            .Include(item => item.Customer)
            .Include(item => item.Employee)
            .SingleOrDefaultAsync(item => item.Id == accountId, cancellationToken);
        if (account is null)
        {
            return Unauthorized();
        }

        var fullName = account.Customer?.FullName
            ?? (account.Employee is null ? account.UserName : $"{account.Employee.LastName} {account.Employee.FirstName}".Trim());
        return Ok(new
        {
            userName = account.UserName,
            email = account.Email,
            role = account.Role.Name,
            profileId = account.Customer?.Id ?? account.Employee?.Id ?? account.Id.ToString(),
            fullName,
            phoneNumber = account.Customer?.PhoneNumber,
            address = account.Customer?.ShippingAddress ?? account.Employee?.Address
        });
    }

    private AuthResponse CreateToken(UserAccount account, string? customerId, string role)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(configuration.GetValue("Jwt:AccessTokenMinutes", 90));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.UserName),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Email, account.Email)
        };
        if (customerId is not null)
        {
            claims.Add(new Claim("customer_id", customerId));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, account.UserName, role, customerId);
    }
}
