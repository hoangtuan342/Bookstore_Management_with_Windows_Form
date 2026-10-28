using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookStore.Infrastructure.Persistence;

public sealed class BookStoreDesignTimeFactory : IDesignTimeDbContextFactory<BookStoreDbContext>
{
    public BookStoreDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseSqlServer("Server=LAPTOP-L9F9N1TE;Database=TrangSachDb;User Id=sa;Password=123456;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true")
            .Options;
        return new BookStoreDbContext(options);
    }
}
