using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using BookStore.Domain.Entities;
using BookStore.Infrastructure.Services;
using BookStore.Shared.Models;
using BookStore.Shared.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.WinForms;

/// <summary>Customer operations executed directly against the shared SQL Server database.</summary>
public sealed class DirectCustomerClient
{
    private readonly PasswordHasher<UserAccount> _passwordHasher = new();
    public string? CustomerId { get; private set; }
    public string? Role { get; private set; }
    public int? AccountId { get; private set; }

    public async Task<StoreApiResult> LoginAsync(string userNameOrEmail, string password, CancellationToken cancellationToken = default)
    {
        await using var db = DirectDatabase.CreateContext();
        var key = userNameOrEmail.Trim();
        var account = await db.Accounts.Include(x => x.Role).Include(x => x.Customer).SingleOrDefaultAsync(
            x => x.UserName == key || x.Email == key, cancellationToken);
        if (account is null || account.Status != "Active" || account.Role.Name != "Customer" || account.Customer is null
            || _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password) == PasswordVerificationResult.Failed)
            return new(false, "Tên đăng nhập hoặc mật khẩu không hợp lệ cho khu vực khách hàng.");
        CustomerId = account.Customer.Id;
        AccountId = account.Id;
        Role = account.Role.Name;
        return new(true, $"Xin chào {account.UserName}!", Role);
    }

    public async Task<StoreApiResult> AdminLoginAsync(string userNameOrEmail, string password, CancellationToken cancellationToken = default)
    {
        await using var db = DirectDatabase.CreateContext();
        var key = userNameOrEmail.Trim();
        var account = await db.Accounts.Include(x => x.Role).Include(x => x.Employee).SingleOrDefaultAsync(
            x => x.UserName == key || x.Email == key, cancellationToken);
        if (account is null) return new(false, "Thông tin đăng nhập không hợp lệ hoặc tài khoản không có quyền nhân viên.");
        if (account.Status != "Active" || account.Employee?.AccountStatus != "Active"
            || !StaffPermissions.IsEmployeeRole(account.Role.Name)
            || _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password) == PasswordVerificationResult.Failed)
            return new(false, "Thông tin đăng nhập không hợp lệ hoặc tài khoản không có quyền nhân viên.");
        Role = account.Role.Name;
        CustomerId = null;
        AccountId = account.Id;
        return new(true, $"Xin chào {account.UserName}!", Role);
    }

    public async Task<StoreApiResult> RegisterAsync(string userName, string email, string password, string fullName, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        await using var db = DirectDatabase.CreateContext();
        userName = userName.Trim(); email = email.Trim();
        if (userName.Length is < 3 or > 80 || password.Length < 8 || fullName.Trim().Length is < 2 or > 180
            || !new EmailAddressAttribute().IsValid(email))
            return new(false, "Tên đăng nhập cần 3–80 ký tự, email phải hợp lệ, họ tên cần ít nhất 2 ký tự và mật khẩu cần ít nhất 8 ký tự.");
        if (await db.Accounts.AnyAsync(x => x.UserName == userName || x.Email == email, cancellationToken))
            return new(false, "Tên đăng nhập hoặc email đã được sử dụng.");
        var role = await db.Roles.SingleAsync(x => x.Name == "Customer", cancellationToken);
        var account = new UserAccount { UserName = userName, Email = email, RoleId = role.Id, Status = "Active" };
        account.PasswordHash = _passwordHasher.HashPassword(account, password);
        var customer = new Customer
        {
            Id = $"KH{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}",
            FullName = fullName.Trim(), PhoneNumber = phoneNumber?.Trim(), UserAccount = account
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);
        CustomerId = customer.Id; AccountId = account.Id; Role = role.Name;
        return new(true, "Tạo tài khoản thành công.", Role);
    }

    public Task SignOutAsync() { CustomerId = null; AccountId = null; Role = null; return Task.CompletedTask; }

    public async Task<IReadOnlyList<StoreCartLine>> GetCartAsync(CancellationToken cancellationToken = default)
    {
        var customerId = RequireCustomer();
        await using var db = DirectDatabase.CreateContext();
        var rows = await db.CartItems.AsNoTracking().Where(x => x.CustomerId == customerId)
            .Include(x => x.Book).ThenInclude(x => x.Authors).ThenInclude(x => x.Author)
            .OrderBy(x => x.Book.Title).ToListAsync(cancellationToken);
        var discounts = await new BookPricingService(db).GetActiveDiscountsAsync(rows.Select(x => x.BookId).ToArray(), DateTime.UtcNow, cancellationToken);
        return rows.Select(x =>
        {
            var price = BookPricingService.ApplyDiscount(x.Book.SalePrice, discounts.GetValueOrDefault(x.BookId));
            return new StoreCartLine(x.BookId, x.Book.Title, x.Book.Authors.Select(a => a.Author.Name).FirstOrDefault() ?? "Chưa cập nhật", price,
                x.Quantity, x.Book.StockQuantity, price * x.Quantity);
        }).ToArray();
    }

    public async Task SetCartQuantityAsync(string bookId, int quantity, CancellationToken cancellationToken = default)
    {
        var customerId = RequireCustomer();
        await using var db = DirectDatabase.CreateContext();
        var book = await db.Books.SingleOrDefaultAsync(x => x.Id == bookId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy sách.");
        if (quantity < 1 || quantity > book.StockQuantity) throw new InvalidOperationException("Số lượng vượt quá tồn kho hiện tại.");
        var item = await db.CartItems.FindAsync([customerId, bookId], cancellationToken);
        if (item is null) db.CartItems.Add(new CartItem { CustomerId = customerId, BookId = bookId, Quantity = quantity });
        else item.Quantity = quantity;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoreOrder>> GetMyOrdersAsync(CancellationToken cancellationToken = default)
    {
        var customerId = RequireCustomer();
        await using var db = DirectDatabase.CreateContext();
        var rows = await db.Orders.AsNoTracking().Where(x => x.CustomerId == customerId)
            .Include(x => x.Lines).ThenInclude(x => x.Book).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return rows.Select(ToOrder).ToArray();
    }

    public async Task<StoreOrder> CheckoutAsync(CheckoutDetails details, CancellationToken cancellationToken = default)
    {
        var customerId = RequireCustomer();
        if (details.ShippingAddress.Trim().Length is < 8 or > 500 || !new PhoneAttribute().IsValid(details.ReceiverPhone)
            || details.ReceiverPhone.Length > 30 || details.PaymentMethod is not ("CashOnDelivery" or "BankTransfer"))
            throw new InvalidOperationException("Địa chỉ giao hàng, số điện thoại hoặc phương thức thanh toán không hợp lệ.");
        await using var db = DirectDatabase.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var cart = await db.CartItems.Where(x => x.CustomerId == customerId).Include(x => x.Book).ToListAsync(cancellationToken);
        if (cart.Count == 0) throw new InvalidOperationException("Giỏ hàng đang trống.");
        var unavailable = cart.FirstOrDefault(x => x.Quantity > x.Book.StockQuantity);
        if (unavailable is not null) throw new InvalidOperationException($"Số lượng '{unavailable.Book.Title}' vừa thay đổi. Hãy cập nhật giỏ hàng.");
        var pricing = new BookPricingService(db);
        var discounts = await pricing.GetActiveDiscountsAsync(cart.Select(x => x.BookId).ToArray(), DateTime.UtcNow, cancellationToken);
        var lines = cart.Select(x =>
        {
            var price = BookPricingService.ApplyDiscount(x.Book.SalePrice, discounts.GetValueOrDefault(x.BookId));
            return new SalesOrderLine { BookId = x.BookId, Quantity = x.Quantity, UnitPrice = price, LineTotal = price * x.Quantity };
        }).ToList();
        var subtotal = lines.Sum(x => x.LineTotal);
        var now = DateTime.UtcNow;
        var orderRate = await db.OrderPromotions.Where(x => x.MinimumOrderAmount <= subtotal && x.Promotion.StartsAt <= now && x.Promotion.EndsAt >= now)
            .Select(x => (decimal?)x.DiscountPercent).MaxAsync(cancellationToken) ?? 0m;
        Voucher? voucher = null; decimal voucherRate = 0m;
        var code = details.VoucherCode?.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(code))
        {
            voucher = await db.Vouchers.Include(x => x.Promotions).ThenInclude(x => x.Promotion).SingleOrDefaultAsync(x => x.Id == code, cancellationToken);
            if (voucher is null || voucher.Status != "Active" || voucher.ExpiresAt < now || voucher.MaxUsageCount.HasValue && voucher.UsageCount >= voucher.MaxUsageCount)
                throw new InvalidOperationException("Voucher không tồn tại, đã hết hạn hoặc đã hết lượt sử dụng.");
            voucherRate = voucher.Promotions.Where(x => x.Promotion.StartsAt <= now && x.Promotion.EndsAt >= now).Select(x => x.DiscountPercent).DefaultIfEmpty(0).Max();
            if (voucherRate <= 0) throw new InvalidOperationException("Voucher hiện không áp dụng được.");
        }
        var appliedRate = Math.Max(orderRate, voucherRate);
        var orderDiscount = decimal.Round(subtotal * appliedRate / 100m, 2, MidpointRounding.AwayFromZero);
        var itemDiscount = cart.Sum(x => (x.Book.SalePrice - BookPricingService.ApplyDiscount(x.Book.SalePrice, discounts.GetValueOrDefault(x.BookId))) * x.Quantity);
        var order = new SalesOrder
        {
            Id = $"HD{DateTime.UtcNow:yyyyMMddHHmmss}{Convert.ToHexString(RandomNumberGenerator.GetBytes(3))}",
            CreatedAt = now, CustomerId = customerId, TotalAmount = subtotal - orderDiscount, DiscountAmount = itemDiscount + orderDiscount,
            OrderStatus = "Pending", PaymentStatus = details.PaymentMethod == "CashOnDelivery" ? "Unpaid" : "AwaitingPayment",
            PaymentMethod = details.PaymentMethod, ShippingAddress = details.ShippingAddress.Trim(), ReceiverPhone = details.ReceiverPhone.Trim(),
            VoucherId = voucherRate >= orderRate && voucherRate > 0 ? code : null, Lines = lines
        };
        if (order.VoucherId is not null && voucher is not null) voucher.UsageCount++;
        foreach (var item in cart) item.Book.StockQuantity -= item.Quantity;
        db.Orders.Add(order); db.CartItems.RemoveRange(cart);
        await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        foreach (var line in order.Lines) line.Book = cart.Single(x => x.BookId == line.BookId).Book;
        return ToOrder(order);
    }

    private string RequireCustomer() => Role == "Customer" && CustomerId is not null
        ? CustomerId : throw new UnauthorizedAccessException("Vui lòng đăng nhập bằng tài khoản khách hàng.");

    private static StoreOrder ToOrder(SalesOrder row) => new(row.Id, row.CreatedAt, row.OrderStatus, row.PaymentStatus, row.PaymentMethod,
        row.ShippingAddress, row.ReceiverPhone, row.TotalAmount, row.DiscountAmount,
        row.Lines.Select(x => new StoreOrderLine(x.BookId, x.Book.Title, x.Quantity, x.UnitPrice, x.LineTotal)).ToArray());
}

public sealed class DirectBookCatalog
{
    public async Task<IReadOnlyList<BookItem>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        await using var db = DirectDatabase.CreateContext();
        var rows = await db.Books.AsNoTracking().Where(x => x.IsActive).Include(x => x.Category)
            .Include(x => x.Authors).ThenInclude(x => x.Author).OrderBy(x => x.Title).ToListAsync(cancellationToken);
        var discounts = await new BookPricingService(db).GetActiveDiscountsAsync(rows.Select(x => x.Id).ToArray(), DateTime.UtcNow, cancellationToken);
        return rows.Select(book =>
        {
            var discount = discounts.GetValueOrDefault(book.Id);
            var cover = book.Id switch { "sach-01" => "cover-sand", "sach-02" => "cover-coral", "sach-03" => "cover-sage", "sach-04" => "cover-ink", "sach-05" => "cover-blue", "sach-06" => "cover-plum", _ => "cover-sage" };
            return new BookItem(book.Id, book.Title, book.Authors.OrderBy(x => x.Author.Name).Select(x => x.Author.Name).FirstOrDefault() ?? "Chưa cập nhật",
                book.Category.Name, BookPricingService.ApplyDiscount(book.SalePrice, discount), cover, discount > 0 ? $"GIẢM {discount:0.#}%" : "SÁCH HAY", string.Empty, book.StockQuantity);
        }).ToArray();
    }
}
