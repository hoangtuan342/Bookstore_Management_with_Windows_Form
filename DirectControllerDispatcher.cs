using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using BookStore.Infrastructure.Services;
using BookStore.Shared.Security;
using BookStore.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.WinForms;

/// <summary>
/// Executes the existing database-backed business actions in-process. No HTTP listener,
/// localhost endpoint, or separately running API process is used by the WinForms apps.
/// </summary>
public sealed class DirectControllerDispatcher(DirectCustomerClient session)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<T?> InvokeAsync<T>(string controllerName, string methodName, params object?[] suppliedArguments)
    {
        await using var db = DirectDatabase.CreateContext();
        EnsureAccess(controllerName, methodName);
        var controller = CreateController(controllerName, db);
        var method = controller.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.Name == methodName)
            .FirstOrDefault(x => x.GetParameters().Count(p => p.ParameterType != typeof(CancellationToken)) == suppliedArguments.Length)
            ?? throw new MissingMethodException(controllerName, methodName);
        var parameters = method.GetParameters();
        var arguments = new object?[parameters.Length];
        var suppliedIndex = 0;
        for (var i = 0; i < parameters.Length; i++)
            arguments[i] = parameters[i].ParameterType == typeof(CancellationToken)
                ? CancellationToken.None : suppliedArguments[suppliedIndex++];

        foreach (var argument in arguments.Where(x => x is not null && x is not CancellationToken))
        {
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(argument!, new ValidationContext(argument!), errors, validateAllProperties: true))
                throw new InvalidOperationException(string.Join(" ", errors.Select(x => x.ErrorMessage).Where(x => x is not null)));
        }

        controller.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext() };
        var invocation = method.Invoke(controller, arguments) ?? throw new InvalidOperationException("Nghiệp vụ không trả kết quả.");
        if (invocation is not Task task) throw new InvalidOperationException("Nghiệp vụ phải trả về Task.");
        await task;
        var raw = task.GetType().GetProperty("Result")?.GetValue(task);
        var value = Unwrap(raw);
        if (IsWriteAction(controllerName, methodName)) await WriteAuditAsync(db, controllerName, methodName, suppliedArguments, raw);
        if (value is null) return default;
        if (value is T typed) return typed;
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException("Không thể ánh xạ dữ liệu từ database sang màn hình.");
    }

    private void EnsureAccess(string controller, string action)
    {
        var role = session.Role;
        var area = (controller, action) switch
        {
            (nameof(AdminDashboardController), _) => "dashboard",
            (nameof(AdminReportsController), _) => "reports",
            (nameof(AdminLogsController), _) => "logs",
            (nameof(AdminBooksController), _) => "books",
            (nameof(AdminRecordsController), "Categories" or "CreateCategory" or "UpdateCategory" or "DeleteCategory"
                or "Authors" or "CreateAuthor" or "UpdateAuthor" or "DeleteAuthor"
                or "Publishers" or "CreatePublisher" or "UpdatePublisher" or "DeletePublisher") => "records",
            (nameof(AdminRecordsController), "Customers" or "CreateCustomer" or "UpdateCustomer" or "DeleteCustomer") => "customers",
            (nameof(AdminRecordsController), "Employees" or "EmployeeRoles" or "CreateEmployee" or "UpdateEmployee" or "DeleteEmployee") => "employees",
            (nameof(AdminOrdersController), _) => "orders",
            (nameof(AdminInventoryController), "GetLookups") => "lookups",
            (nameof(AdminInventoryController), _) => "inventory",
            (nameof(AdminPromotionsController), _) or (nameof(AdminVouchersController), _) => "promotions",
            _ => throw new UnauthorizedAccessException("Chức năng này không được cấp quyền cho tài khoản hiện tại.")
        };
        var permitted = area switch
        {
            "logs" or "employees" => role == StaffRoles.Manager,
            "lookups" => StaffPermissions.CanAccess(role, "inventory") || StaffPermissions.CanAccess(role, "books"),
            _ => StaffPermissions.CanAccess(role, area)
        };
        if (!permitted) throw new UnauthorizedAccessException("Chức danh hiện tại không có quyền sử dụng chức năng này.");
    }

    private static bool IsWriteAction(string controller, string action) =>
        !(controller == nameof(AdminDashboardController) || controller == nameof(AdminLogsController)
          || controller == nameof(AdminReportsController)
          || action is "GetBooks" or "GetBookForEdit" or "GetOrders" or "GetReceipts" or "GetLookups" or "GetSuppliers"
              or "Categories" or "Authors" or "Publishers" or "Customers" or "Employees" or "EmployeeRoles" or "Get" or "GetSummary" or "GetLogs");

    private async Task WriteAuditAsync(BookStore.Infrastructure.Persistence.BookStoreDbContext db, string controller, string action,
        object?[] args, object? result)
    {
        var accountId = session.AccountId;
        var account = accountId is null ? null : await db.Accounts.Include(x => x.Employee)
            .SingleOrDefaultAsync(x => x.Id == accountId.Value);
        var status = result is IActionResult actionResult ? actionResult switch
        {
            ObjectResult obj => obj.StatusCode ?? StatusCodes.Status200OK,
            StatusCodeResult code => code.StatusCode,
            _ => StatusCodes.Status200OK
        } : StatusCodes.Status200OK;
        db.SystemAuditLogs.Add(new SystemAuditLog
        {
            OccurredAt = DateTime.UtcNow,
            ActorAccountId = accountId,
            ActorName = account?.UserName ?? "Tài khoản WinForms",
            ActorCode = account?.Employee?.Id,
            ActorRole = session.Role ?? string.Empty,
            Action = action,
            Area = controller.Replace("Admin", "", StringComparison.Ordinal).Replace("Controller", "", StringComparison.Ordinal),
            Resource = args.FirstOrDefault()?.ToString() ?? string.Empty,
            StatusCode = status,
            IpAddress = "WinForms"
        });
        await db.SaveChangesAsync();
    }

    private ControllerBase CreateController(string name, BookStoreDbContext db)
    {
        var pricing = new BookPricingService(db);
        return name switch
        {
            nameof(AdminBooksController) => new AdminBooksController(db),
            nameof(AdminDashboardController) => new AdminDashboardController(db),
            nameof(AdminInventoryController) => new AdminInventoryController(db),
            nameof(AdminLogsController) => new AdminLogsController(db),
            nameof(AdminOrdersController) => new AdminOrdersController(db, pricing),
            nameof(AdminPromotionsController) => new AdminPromotionsController(db),
            nameof(AdminRecordsController) => new AdminRecordsController(db, new PasswordHasher<UserAccount>()),
            nameof(AdminReportsController) => new AdminReportsController(db),
            nameof(AdminVouchersController) => new AdminVouchersController(db),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Controller truy cập dữ liệu không được hỗ trợ.")
        };
    }

    private DefaultHttpContext CreateHttpContext()
    {
        var claims = new List<Claim>();
        if (session.AccountId is int accountId) claims.Add(new Claim(ClaimTypes.NameIdentifier, accountId.ToString()));
        if (session.Role is string role) claims.Add(new Claim(ClaimTypes.Role, role));
        if (session.CustomerId is string customerId) claims.Add(new Claim("customer_id", customerId));
        return new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "DirectDatabase")) };
    }

    private static object? Unwrap(object? result)
    {
        if (result is null) return null;
        var resultProperty = result.GetType().GetProperty("Result");
        var valueProperty = result.GetType().GetProperty("Value");
        if (resultProperty is not null && valueProperty is not null)
        {
            var actionResult = resultProperty.GetValue(result);
            if (actionResult is not null) return Unwrap(actionResult);
            return valueProperty.GetValue(result);
        }
        if (result is ObjectResult objectResult)
        {
            if (objectResult.StatusCode >= 400)
            {
                var message = objectResult.Value?.GetType().GetProperty("message")?.GetValue(objectResult.Value)?.ToString();
                throw new InvalidOperationException(message ?? $"Nghiệp vụ thất bại (HTTP {objectResult.StatusCode}).");
            }
            return objectResult.Value;
        }
        if (result is StatusCodeResult status && status.StatusCode >= 400)
            throw new InvalidOperationException($"Nghiệp vụ thất bại (HTTP {status.StatusCode}).");
        if (result is StatusCodeResult || result is EmptyResult) return null;
        return result;
    }
}
