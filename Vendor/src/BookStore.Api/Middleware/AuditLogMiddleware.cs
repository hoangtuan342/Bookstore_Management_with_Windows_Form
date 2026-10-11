using System.Security.Claims;
using System.Text.Json;
using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Middleware;

public sealed class AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsLoginRequest(context.Request))
        {
            context.Request.EnableBuffering();
        }

        Exception? requestException = null;
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            requestException = exception;
            throw;
        }
        finally
        {
            if (ShouldAudit(context.Request))
            {
                await WriteEntryAsync(context, requestException is null ? context.Response.StatusCode : StatusCodes.Status500InternalServerError);
            }
        }
    }

    private static bool ShouldAudit(HttpRequest request)
    {
        var method = request.Method;
        var path = request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/admin/", StringComparison.OrdinalIgnoreCase)
            && !path.TrimEnd('/').Equals("/api/admin/logs", StringComparison.OrdinalIgnoreCase))
        {
            return method is "GET" or "POST" or "PUT" or "PATCH" or "DELETE";
        }

        return method == "POST" && IsLoginRequest(request);
    }

    private static bool IsLoginRequest(HttpRequest request) =>
        (request.Path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase)
            || request.Path.Equals("/api/auth/admin-login", StringComparison.OrdinalIgnoreCase))
        && request.Method == "POST";

    private static string GetArea(HttpContext context)
    {
        var requestedArea = context.Request.Headers["X-Audit-Area"].FirstOrDefault()?.ToLowerInvariant();
        if (requestedArea is "promotions") return "Khuyến mãi";

        var path = context.Request.Path.Value ?? string.Empty;
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && parts[1].Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            return parts[2].ToLowerInvariant() switch
            {
                "books" => "Sách",
                "receipts" or "inventory" => "Nhập hàng",
                "orders" => "Đơn hàng",
                "promotions" or "vouchers" => "Khuyến mãi",
                "categories" or "authors" or "publishers" => "Danh mục sách",
                "customers" => "Khách hàng",
                "employees" => "Nhân viên",
                "dashboard" => "Tổng quan",
                "reports" => "Báo cáo",
                "lookups" => "Dữ liệu danh mục",
                _ => parts[2]
            };
        }
        return "Tài khoản";
    }

    private async Task WriteEntryAsync(HttpContext context, int statusCode)
    {
        try
        {
            var principal = context.User;
            int? accountId = int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId) ? parsedId : null;
            var path = context.Request.Path.Value ?? "/";
            var isLogin = path.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase);
            var method = isLogin ? "LOGIN" : context.Request.Method == "GET" ? "VIEW" : context.Request.Method;
            var resource = path.Length <= 300 ? path : path[..300];
            var db = context.RequestServices.GetRequiredService<BookStoreDbContext>();
            var loginIdentifier = isLogin ? await ReadLoginIdentifierAsync(context.Request) : null;
            var account = accountId is not null
                ? await db.Accounts.AsNoTracking().Where(item => item.Id == accountId.Value)
                    .Select(item => new
                    {
                        item.Id,
                        item.UserName,
                        RoleName = item.Role.Name,
                        EmployeeCode = item.Employee == null ? null : item.Employee.Id,
                        EmployeeName = item.Employee == null ? null : (item.Employee.LastName + " " + item.Employee.FirstName).Trim(),
                        CustomerCode = item.Customer == null ? null : item.Customer.Id,
                        CustomerName = item.Customer == null ? null : item.Customer.FullName
                    }).SingleOrDefaultAsync()
                : !string.IsNullOrWhiteSpace(loginIdentifier)
                    ? await db.Accounts.AsNoTracking().Where(item => item.UserName == loginIdentifier || item.Email == loginIdentifier)
                        .Select(item => new
                        {
                            item.Id,
                            item.UserName,
                            RoleName = item.Role.Name,
                            EmployeeCode = item.Employee == null ? null : item.Employee.Id,
                            EmployeeName = item.Employee == null ? null : (item.Employee.LastName + " " + item.Employee.FirstName).Trim(),
                            CustomerCode = item.Customer == null ? null : item.Customer.Id,
                            CustomerName = item.Customer == null ? null : item.Customer.FullName
                        }).SingleOrDefaultAsync()
                    : null;
            accountId ??= account?.Id;
            var actorName = account is null
                ? (string.IsNullOrWhiteSpace(loginIdentifier) ? "Chưa xác thực" : loginIdentifier)
                : !string.IsNullOrWhiteSpace(account.EmployeeName) ? account.EmployeeName
                : !string.IsNullOrWhiteSpace(account.CustomerName) ? account.CustomerName
                : account.UserName;
            var actorCode = account?.EmployeeCode ?? account?.CustomerCode;
            var actorRole = account?.RoleName ?? principal.FindFirstValue(ClaimTypes.Role) ?? "Unknown";
            var entry = new SystemAuditLog
            {
                OccurredAt = DateTime.UtcNow,
                ActorAccountId = accountId,
                ActorName = string.IsNullOrWhiteSpace(actorName) ? (accountId is null ? "Chưa xác thực" : $"Tài khoản #{accountId}") : actorName[..Math.Min(actorName.Length, 100)],
                ActorCode = actorCode,
                ActorRole = actorRole[..Math.Min(actorRole.Length, 60)],
                Action = method,
                Area = GetArea(context),
                Resource = resource,
                StatusCode = statusCode,
                IpAddress = context.Connection.RemoteIpAddress?.ToString()
            };

            db.SystemAuditLogs.Add(entry);
            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not persist audit log for {Path}", context.Request.Path);
        }
    }

    private static async Task<string?> ReadLoginIdentifierAsync(HttpRequest request)
    {
        if (request.ContentLength is > 4096) return null;
        try
        {
            request.Body.Position = 0;
            using var document = await JsonDocument.ParseAsync(request.Body, cancellationToken: CancellationToken.None);
            return document.RootElement.TryGetProperty("userNameOrEmail", out var property)
                ? property.GetString()?.Trim()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        finally
        {
            if (request.Body.CanSeek) request.Body.Position = 0;
        }
    }
}
