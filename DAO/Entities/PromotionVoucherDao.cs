using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;

namespace TrangSach.DAO.Entities;

public sealed class PromotionVoucherDao(BookStoreDbContext context) : EntityDao<PromotionVoucher>(context) { }
