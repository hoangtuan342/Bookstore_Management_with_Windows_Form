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
        try
        {
            var account = service.Register(ToCommand(request));
            return StatusCode(StatusCodes.Status201Created, CreateToken(account));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [AllowAnonymous, HttpPost("setup-manager")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult SetupFirstManager(RegisterRequest request)
    {
        if (!environment.IsDevelopment()) return NotFound();
        try
        {
            var account = service.SetupFirstManager(ToCommand(request));
            return StatusCode(StatusCodes.Status201Created, CreateToken(account));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [AllowAnonymous, HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login(LoginRequest request)
    {
        try
        {
            var account = service.LoginCustomer(new LoginCommandDto(request.UserNameOrEmail, request.Password));
            return Ok(CreateToken(account));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous, HttpPost("admin-login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult AdminLogin(LoginRequest request)
    {
        try
        {
            var account = service.LoginEmployee(new LoginCommandDto(request.UserNameOrEmail, request.Password));
            return Ok(CreateToken(account));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous, HttpPost("guest")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public IActionResult CreateGuestToken([FromQuery] string? guestId)
    {
        var id = !string.IsNullOrWhiteSpace(guestId) && guestId.StartsWith("KH-GUEST-")
            ? guestId.Trim()
            : $"KH-GUEST-{Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4))}";
        
        var account = new AuthenticatedAccountDto(0, id, $"{id.ToLower()}@guest.trangsach.vn", "Customer", id);
        return Ok(CreateToken(account));
    }

    [Authorize, HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Unauthorized();
        try
        {
            var profile = service.GetCurrentUser(id);
            return Ok(profile);
        }
        catch (Exception)
        {
            return Unauthorized();
        }
    }

    [Authorize, HttpPut("me")]
    public IActionResult UpdateProfile(UpdateProfileRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Unauthorized();
        try
        {
            var updated = service.UpdateCustomerProfile(id, request.FullName, request.PhoneNumber, request.Address);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
