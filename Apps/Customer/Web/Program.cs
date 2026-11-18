using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using TrangSach.Customer.BUS;

var builder = WebApplication.CreateBuilder(args);
var apiAddress = builder.Configuration["Api:BaseAddress"];
if (!Uri.TryCreate(apiAddress, UriKind.Absolute, out var apiUri) || apiUri.Scheme != Uri.UriSchemeHttps)
    throw new InvalidOperationException("Cấu hình Api:BaseAddress phải là URL HTTPS hợp lệ.");

builder.Services.AddRazorPages();
builder.Services.AddHttpClient<CustomerWebApiClient>(client =>
{
    client.BaseAddress = apiUri;
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TrangSach.Customer.Web.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(90);
        // The API issues a fixed-lifetime JWT and does not provide refresh tokens.
        options.SlidingExpiration = false;
    });
builder.Services.AddAuthorization();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();

public partial class Program { }
