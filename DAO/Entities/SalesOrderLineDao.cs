using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;

namespace TrangSach.DAO.Entities;

public sealed class SalesOrderLineDao(BookStoreDbContext context) : EntityDao<SalesOrderLine>(context) { }
