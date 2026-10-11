using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookStore.Infrastructure.Persistence;

public sealed class BookStoreDesignTimeFactory : IDesignTimeDbContextFactory<BookStoreDbContext>
{
    public BookStoreDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseSqlServer("Server=localhost;Database=TrangSachDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true")
            .Options;
        return new BookStoreDbContext(options);
    }
}
