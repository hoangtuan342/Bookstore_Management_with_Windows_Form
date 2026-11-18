using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using TrangSach.API.Middleware;
using TrangSach.BUS;
using TrangSach.DAO;

var builder = WebApplication.CreateBuilder(args);

// Use console logging in local development.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped(_ => BookStoreDao.Create());
builder.Services.AddScoped<BookPricingService>();
builder.Services.AddScoped<BookCatalogService>();
builder.Services.AddScoped<AdminDashboardService>();
builder.Services.AddScoped<AdminLogsService>();
builder.Services.AddScoped<AdminReportsService>();
builder.Services.AddScoped<AdminBookService>();
builder.Services.AddScoped<AdminVoucherService>();
builder.Services.AddScoped<AdminInventoryService>();
builder.Services.AddScoped<AdminPromotionService>();
builder.Services.AddScoped<AdminOrderService>();
builder.Services.AddScoped<AdminRecordsService>();
builder.Services.AddScoped<CustomerCommerceService>();
builder.Services.AddScoped<AuthenticationService>();

var jwtKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("JWT signing key is missing. Configure Jwt:SigningKey outside source control.");
    }

    // Local-only fallback. Set Jwt:SigningKey in User Secrets to keep tokens valid across API restarts.
    jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    builder.Configuration["Jwt:SigningKey"] = jwtKey;
}
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var accountIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(accountIdClaim, out var accountId))
                {
                    context.Fail("Tài khoản không hợp lệ.");
                    return;
                }

                var tokenRole = context.Principal?.FindFirstValue(ClaimTypes.Role);
                var authentication = context.HttpContext.RequestServices.GetRequiredService<AuthenticationService>();
                if (!authentication.IsTokenValid(accountId, tokenRole))
                {
                    context.Fail("Tài khoản đã bị khóa hoặc quyền truy cập đã thay đổi. Hãy đăng nhập lại.");
                }
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHashingService, PasswordHashingService>();

var app = builder.Build();

BookStoreDao.Initialize();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditLogMiddleware>();

app.MapControllers();

app.Run();
