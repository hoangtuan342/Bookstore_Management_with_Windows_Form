using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using TrangSach.DAO;

namespace TrangSach.DAO.Entities;

/// <summary>Per-entity DAO registry and unit-of-work boundary for the bookstore schema.</summary>
public sealed class BookStoreDao : IAsyncDisposable
{
    private readonly BookStoreDbContext _context;
    private readonly bool _ownsContext;

    public BookStoreDao(BookStoreDbContext context) : this(context, ownsContext: false) { }

    private BookStoreDao(BookStoreDbContext context, bool ownsContext)
    {
        _context = context;
        _ownsContext = ownsContext;
        Categories = new(context); Books = new(context); Publishers = new(context); Authors = new(context);
        BookAuthors = new(context); Roles = new(context); Accounts = new(context); Employees = new(context);
        Customers = new(context); Suppliers = new(context); PurchaseReceipts = new(context); PurchaseReceiptLines = new(context);
        Orders = new(context); OrderLines = new(context); CartItems = new(context); Promotions = new(context);
        BookPromotions = new(context); OrderPromotions = new(context); Vouchers = new(context);
        PromotionVouchers = new(context); AuditLogs = new(context);
    }

    public BookCategoryDao Categories { get; }
    public BookDao Books { get; }
    public PublisherDao Publishers { get; }
    public AuthorDao Authors { get; }
    public BookAuthorDao BookAuthors { get; }
    public UserRoleDao Roles { get; }
    public UserAccountDao Accounts { get; }
    public EmployeeDao Employees { get; }
    public CustomerDao Customers { get; }
    public SupplierDao Suppliers { get; }
    public PurchaseReceiptDao PurchaseReceipts { get; }
    public PurchaseReceiptLineDao PurchaseReceiptLines { get; }
    public SalesOrderDao Orders { get; }
    public SalesOrderLineDao OrderLines { get; }
    public CartItemDao CartItems { get; }
    public PromotionDao Promotions { get; }
    public BookPromotionDao BookPromotions { get; }
    public OrderPromotionDao OrderPromotions { get; }
    public VoucherDao Vouchers { get; }
    public PromotionVoucherDao PromotionVouchers { get; }
    public SystemAuditLogDao AuditLogs { get; }

    /// <summary>Creates a DAO unit of work with a context owned by this instance.</summary>
    public static BookStoreDao Create() => new(DirectDatabase.CreateContext(), ownsContext: true);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new DaoUpdateException("Unable to save the requested data changes.", exception);
        }
    }

    public async Task<IDataTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        var transaction = await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions
            .BeginTransactionAsync(_context.Database, isolationLevel, cancellationToken);
        return new DataTransaction(transaction);
    }

    public ValueTask DisposeAsync() => _ownsContext ? _context.DisposeAsync() : ValueTask.CompletedTask;

    private sealed class DataTransaction(IDbContextTransaction transaction) : IDataTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}

public interface IDataTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}

public sealed class DaoUpdateException(string message, Exception innerException) : Exception(message, innerException);
