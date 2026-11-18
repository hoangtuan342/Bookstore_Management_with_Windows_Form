using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TrangSach.API.Contracts;
using TrangSach.Shared.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
namespace TrangSach.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthenticationService service, IConfiguration configuration, IHostEnvironment environment) : ControllerBase
{
    [AllowAnonymous, HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Register(RegisterRequest request)
    {
        var result = service.Register(ToCommand(request));
        return result.Status switch { OperationStatus.Created => StatusCode(StatusCodes.Status201Created, CreateToken(result.Value!)),
            OperationStatus.Conflict => Conflict(new { message = result.Message }), OperationStatus.Invalid => BadRequest(new { message = result.Message }), _ => StatusCode(500) };
    }

    [AllowAnonymous, HttpPost("setup-manager")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult SetupFirstManager(RegisterRequest request)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var result = service.SetupFirstManager(ToCommand(request));
        return result.Status switch { OperationStatus.Created => StatusCode(StatusCodes.Status201Created, CreateToken(result.Value!)),
            OperationStatus.Conflict => Conflict(new { message = result.Message }), _ => StatusCode(500) };
    }

    [AllowAnonymous, HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login(LoginRequest request)
    {
        var result = service.LoginCustomer(new LoginCommandDto(request.UserNameOrEmail, request.Password));
        return result.Succeeded ? Ok(CreateToken(result.Value!)) : Unauthorized(new { message = result.Message });
    }

    [AllowAnonymous, HttpPost("admin-login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult AdminLogin(LoginRequest request)
    {
        var result = service.LoginEmployee(new LoginCommandDto(request.UserNameOrEmail, request.Password));
        return result.Succeeded ? Ok(CreateToken(result.Value!)) : Unauthorized(new { message = result.Message });
    }

    [Authorize, HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Unauthorized();
        var result = service.GetCurrentUser(id);
        return result.Succeeded ? Ok(result.Value) : Unauthorized();
    }

    private static RegisterAccountCommandDto ToCommand(RegisterRequest request) =>
        new(request.UserName, request.Email, request.Password, request.FullName, request.PhoneNumber);

    private AuthResponse CreateToken(AuthenticatedAccountDto account)
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
