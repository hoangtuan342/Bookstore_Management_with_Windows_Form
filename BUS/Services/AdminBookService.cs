using System.Security.Cryptography;
using BookStore.Domain.Entities;
using BookStore.Shared.Models;
using TrangSach.BUS.Models;
using TrangSach.BUS.Results;
using TrangSach.DAO.Entities;

namespace TrangSach.BUS.Services;

public sealed class AdminBookService(BookStoreDao dao)
{
    public async Task<OperationResult<BookEditDetails>> GetBookForEditAsync(string id, CancellationToken cancellationToken = default)
    {
        var book = await dao.Books.FindForManagementAsync(id, cancellationToken);
        return book is null
            ? OperationResult<BookEditDetails>.NotFound()
            : OperationResult<BookEditDetails>.Success(new BookEditDetails(book.Id, book.Title, book.SalePrice,
                book.Unit, book.CategoryId, book.PublisherId, book.Authors.Select(link => link.AuthorId).ToArray(), book.IsActive));
    }

    public async Task<IReadOnlyList<BookItem>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        var entities = await dao.Books.GetManagementListAsync(cancellationToken);
        return entities.Select(book => new BookItem(book.Id, book.Title,
            book.Authors.OrderBy(author => author.Author.Name).Select(author => author.Author.Name).FirstOrDefault() ?? "Chưa cập nhật",
            book.Category.Name, book.SalePrice, "cover-sage", book.IsActive ? "ĐANG BÁN" : "ĐÃ ẨN", string.Empty,
            book.StockQuantity)).ToArray();
    }

    public async Task<OperationResult<BookItem>> CreateBookAsync(BookManagementCommand command, CancellationToken cancellationToken = default)
    {
        var validation = Validate(command);
        if (validation is not null) return OperationResult<BookItem>.Invalid(validation);
        var referencesValid = await dao.Categories.ExistsAsync(category => category.Id == command.CategoryId, cancellationToken)
            && (command.PublisherId is null || await dao.Publishers.ExistsAsync(publisher => publisher.Id == command.PublisherId.Value, cancellationToken));
        var authors = await dao.Authors.GetByIdsAsync(command.AuthorIds, cancellationToken);
        if (!referencesValid || authors.Count != command.AuthorIds.Distinct().Count())
            return OperationResult<BookItem>.Invalid("Thể loại, nhà xuất bản hoặc tác giả không hợp lệ.");

        var book = new Book
        {
            Id = $"S{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}",
            Title = command.Title.Trim(), CategoryId = command.CategoryId, PublisherId = command.PublisherId,
            SalePrice = command.SalePrice, Unit = command.Unit.Trim(), StockQuantity = 0,
            Authors = authors.Select(author => new BookAuthor { Author = author }).ToList()
        };
        dao.Books.Add(book);
        await dao.SaveChangesAsync(cancellationToken);
        var categoryName = await dao.Categories.GetNameAsync(book.CategoryId, cancellationToken);
        var result = new BookItem(book.Id, book.Title,
            authors.OrderBy(author => author.Name).Select(author => author.Name).FirstOrDefault() ?? "Chưa cập nhật",
            categoryName, book.SalePrice, "cover-sage", "ĐANG BÁN", string.Empty, book.StockQuantity);
        return OperationResult<BookItem>.Created(result);
    }

    public async Task<OperationResult<Unit>> UpdateBookAsync(string id, BookManagementCommand command, CancellationToken cancellationToken = default)
    {
        var validation = Validate(command);
        if (validation is not null) return OperationResult<Unit>.Invalid(validation);
        var book = await dao.Books.FindForManagementAsync(id, cancellationToken);
        if (book is null) return OperationResult<Unit>.NotFound();
        var referencesValid = await dao.Categories.ExistsAsync(category => category.Id == command.CategoryId, cancellationToken)
            && (command.PublisherId is null || await dao.Publishers.ExistsAsync(publisher => publisher.Id == command.PublisherId.Value, cancellationToken));
        var authors = await dao.Authors.GetByIdsAsync(command.AuthorIds, cancellationToken);
        if (!referencesValid || authors.Count != command.AuthorIds.Distinct().Count())
            return OperationResult<Unit>.Invalid("Thể loại, nhà xuất bản hoặc tác giả không hợp lệ.");

        book.Title = command.Title.Trim(); book.CategoryId = command.CategoryId; book.PublisherId = command.PublisherId;
        book.SalePrice = command.SalePrice; book.Unit = command.Unit.Trim();
        dao.BookAuthors.RemoveRange(book.Authors);
        book.Authors = authors.Select(author => new BookAuthor { BookId = book.Id, AuthorId = author.Id, Author = author }).ToList();
        await dao.SaveChangesAsync(cancellationToken);
        return OperationResult<Unit>.Success(new Unit());
    }

    public async Task<OperationResult<Unit>> HideBookAsync(string id, CancellationToken cancellationToken = default)
    {
        var book = await dao.Books.FindAsync([id], cancellationToken);
        if (book is null) return OperationResult<Unit>.NotFound();
        book.IsActive = false;
        await dao.SaveChangesAsync(cancellationToken);
        return OperationResult<Unit>.Success(new Unit());
    }

    public async Task<OperationResult<Unit>> SetVisibilityAsync(string id, BookVisibilityCommand command, CancellationToken cancellationToken = default)
    {
        var book = await dao.Books.FindAsync([id], cancellationToken);
        if (book is null) return OperationResult<Unit>.NotFound();
        book.IsActive = command.IsActive;
        await dao.SaveChangesAsync(cancellationToken);
        return OperationResult<Unit>.Success(new Unit());
    }

    private static string? Validate(BookManagementCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Title) || command.Title.Trim().Length > 250) return "Tên sách phải có từ 1 đến 250 ký tự.";
        if (command.SalePrice <= 0 || command.SalePrice > 999999999) return "Giá bán phải lớn hơn 0 và không vượt quá giới hạn.";
        if (string.IsNullOrWhiteSpace(command.Unit) || command.Unit.Trim().Length > 30) return "Đơn vị tính không hợp lệ.";
        if (string.IsNullOrWhiteSpace(command.CategoryId) || command.CategoryId.Trim().Length > 20) return "Mã thể loại không hợp lệ.";
        if (command.AuthorIds.Distinct().Count() != command.AuthorIds.Count) return "Không được chọn trùng tác giả.";
        return null;
    }
}
