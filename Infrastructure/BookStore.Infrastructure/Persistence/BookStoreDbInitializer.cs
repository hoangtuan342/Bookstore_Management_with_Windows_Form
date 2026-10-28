using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Persistence;

public static class BookStoreDbInitializer
{
    public static async Task InitializeAsync(BookStoreDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        var categories = new[]
        {
            new BookCategory { Id = "VH", Name = "Văn học" },
            new BookCategory { Id = "KNS", Name = "Kỹ năng sống" },
            new BookCategory { Id = "TN", Name = "Thiếu nhi" },
            new BookCategory { Id = "TL", Name = "Tâm lý" }
        };
        foreach (var category in categories)
        {
            if (!await db.Categories.AnyAsync(item => item.Id == category.Id, cancellationToken))
                db.Categories.Add(category);
        }
        await db.SaveChangesAsync(cancellationToken);

        if (!await db.Publishers.AnyAsync(cancellationToken))
        {
            db.Publishers.Add(new Publisher { Name = "Nhà xuất bản Tổng hợp" });
            await db.SaveChangesAsync(cancellationToken);
        }

        var authorNames = new[] { "Paulo Coelho", "Rosie Nguyễn", "Tô Hoài", "Viktor E. Frankl", "José Mauro de Vasconcelos", "Daniel Kahneman" };
        foreach (var name in authorNames)
        {
            if (!await db.Authors.AnyAsync(item => item.Name == name, cancellationToken))
                db.Authors.Add(new Author { Name = name });
        }
        await db.SaveChangesAsync(cancellationToken);

        foreach (var name in new[] { "Customer", "Manager", "Sales", "Inventory", "Catalog", "Marketing", "Accountant" })
        {
            if (!await db.Roles.AnyAsync(item => item.Name == name, cancellationToken))
                db.Roles.Add(new UserRole { Name = name });
        }
        await db.SaveChangesAsync(cancellationToken);

        if (!await db.Books.AnyAsync(cancellationToken))
        {
            var publisherId = await db.Publishers.OrderBy(item => item.Id).Select(item => item.Id).FirstAsync(cancellationToken);
            var authorIds = await db.Authors.Where(item => authorNames.Contains(item.Name))
                .ToDictionaryAsync(item => item.Name, item => item.Id, cancellationToken);
            var books = new[]
            {
                (Id: "sach-01", Title: "Nhà giả kim", Category: "VH", Author: "Paulo Coelho", Price: 79000m, Stock: 25),
                (Id: "sach-02", Title: "Tuổi trẻ đáng giá bao nhiêu", Category: "KNS", Author: "Rosie Nguyễn", Price: 85000m, Stock: 20),
                (Id: "sach-03", Title: "Dế mèn phiêu lưu ký", Category: "TN", Author: "Tô Hoài", Price: 62000m, Stock: 30),
                (Id: "sach-04", Title: "Đi tìm lẽ sống", Category: "TL", Author: "Viktor E. Frankl", Price: 99000m, Stock: 18),
                (Id: "sach-05", Title: "Cây cam ngọt của tôi", Category: "VH", Author: "José Mauro de Vasconcelos", Price: 108000m, Stock: 16),
                (Id: "sach-06", Title: "Tư duy nhanh và chậm", Category: "TL", Author: "Daniel Kahneman", Price: 189000m, Stock: 12)
            };
            foreach (var item in books)
            {
                db.Books.Add(new Book { Id = item.Id, Title = item.Title, CategoryId = item.Category, PublisherId = publisherId, SalePrice = item.Price, StockQuantity = item.Stock });
                db.BookAuthors.Add(new BookAuthor { BookId = item.Id, AuthorId = authorIds[item.Author] });
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        await BookStoreDemoDataSeeder.SeedAsync(db, cancellationToken);
    }
}
