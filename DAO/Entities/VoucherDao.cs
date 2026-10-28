using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class VoucherDao(BookStoreDbContext context) : EntityDao<Voucher>(context)
{
    public Task<Voucher?> FindForCheckoutAsync(string code, CancellationToken cancellationToken = default) =>
        Query.Include(voucher => voucher.Promotions).ThenInclude(link => link.Promotion)
            .SingleOrDefaultAsync(voucher => voucher.Id == code, cancellationToken);

    public Task<List<Voucher>> GetAllWithUsageAsync(CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Include(voucher => voucher.Promotions).ThenInclude(link => link.Promotion)
            .Include(voucher => voucher.Orders).OrderBy(voucher => voucher.Id).ToListAsync(cancellationToken);

    public Task<bool> HasBeenUsedByOrderAsync(string id, CancellationToken cancellationToken = default) =>
        Query.AnyAsync(voucher => voucher.Id == id && voucher.Orders.Any(), cancellationToken);

    public Task<Voucher?> FindByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Query.SingleOrDefaultAsync(voucher => voucher.Id == id, cancellationToken);

    public Task<int> CountByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default) =>
        Query.CountAsync(voucher => ids.Contains(voucher.Id), cancellationToken);
}
