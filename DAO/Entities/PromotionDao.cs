using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class PromotionDao(BookStoreDbContext context) : EntityDao<Promotion>(context)
{
    public Task<List<Promotion>> GetAllWithRulesAsync(CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Include(item => item.Books).ThenInclude(link => link.Book)
            .Include(item => item.OrderDiscounts).Include(item => item.Vouchers).ThenInclude(link => link.Voucher)
            .OrderByDescending(item => item.StartsAt).ToListAsync(cancellationToken);

    public Task<Promotion?> FindWithRulesAsync(string id, CancellationToken cancellationToken = default) =>
        Query.Include(item => item.Books).Include(item => item.OrderDiscounts).Include(item => item.Vouchers)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
}
