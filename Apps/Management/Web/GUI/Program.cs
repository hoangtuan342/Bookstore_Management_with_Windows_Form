using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using TrangSach.Management.Web.BUS;

var builder = WebApplication.CreateBuilder(args);
var apiAddress = builder.Configuration["Api:BaseAddress"];
if (!Uri.TryCreate(apiAddress, UriKind.Absolute, out var apiUri) || apiUri.Scheme != Uri.UriSchemeHttps)
    throw new InvalidOperationException("Api:BaseAddress phải là URL HTTPS hợp lệ.");
builder.Services.AddRazorPages();
builder.Services.AddHttpClient<ManagementWebApiClient>(client =>
{
    client.BaseAddress = apiUri;
    client.Timeout = TimeSpan.FromSeconds(45);
});
builder.Services.AddScoped<ManagementSpreadsheetService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "TrangSach.Management.Web.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(90);
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
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (ManagementApiUnauthorizedException) when (!context.Response.HasStarted)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
        context.Response.Redirect($"/Account/Login?sessionExpired=true&returnUrl={returnUrl}");
    }
});
app.MapRazorPages();
app.Run();

public partial class Program { }
