using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookStore.Api.Contracts;
using BookStore.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TrangSach.BUS.Models;
using TrangSach.BUS.Results;
using TrangSach.BUS.Services;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthenticationService service, IConfiguration configuration, IHostEnvironment environment) : ControllerBase
{
    [AllowAnonymous, HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await service.RegisterAsync(ToCommand(request), ct);
        return result.Status switch { OperationStatus.Created => StatusCode(StatusCodes.Status201Created, CreateToken(result.Value!)),
            OperationStatus.Conflict => Conflict(new { message = result.Message }), OperationStatus.Invalid => BadRequest(new { message = result.Message }), _ => StatusCode(500) };
    }

    [AllowAnonymous, HttpPost("setup-manager")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetupFirstManager(RegisterRequest request, CancellationToken ct)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var result = await service.SetupFirstManagerAsync(ToCommand(request), ct);
        return result.Status switch { OperationStatus.Created => StatusCode(StatusCodes.Status201Created, CreateToken(result.Value!)),
            OperationStatus.Conflict => Conflict(new { message = result.Message }), _ => StatusCode(500) };
    }

    [AllowAnonymous, HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await service.LoginCustomerAsync(new LoginCommand(request.UserNameOrEmail, request.Password), ct);
        return result.Succeeded ? Ok(CreateToken(result.Value!)) : Unauthorized(new { message = result.Message });
    }

    [AllowAnonymous, HttpPost("admin-login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AdminLogin(LoginRequest request, CancellationToken ct)
    {
        var result = await service.LoginEmployeeAsync(new LoginCommand(request.UserNameOrEmail, request.Password), ct);
        return result.Succeeded ? Ok(CreateToken(result.Value!)) : Unauthorized(new { message = result.Message });
    }

    [Authorize, HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Unauthorized();
        var result = await service.GetCurrentUserAsync(id, ct);
        return result.Succeeded ? Ok(result.Value) : Unauthorized();
    }

    private static RegisterAccountCommand ToCommand(RegisterRequest request) =>
        new(request.UserName, request.Email, request.Password, request.FullName, request.PhoneNumber);

    private AuthResponse CreateToken(AuthenticatedAccount account)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(configuration.GetValue("Jwt:AccessTokenMinutes", 90));
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(ClaimTypes.NameIdentifier, account.Id.ToString()), new(ClaimTypes.Name, account.UserName),
            new(ClaimTypes.Role, account.Role), new(JwtRegisteredClaimNames.Email, account.Email) };
        if (account.CustomerId is not null) claims.Add(new Claim("customer_id", account.CustomerId));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims,
            notBefore: now, expires: expires, signingCredentials: creds);
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, account.UserName, account.Role, account.CustomerId);
    }
}
