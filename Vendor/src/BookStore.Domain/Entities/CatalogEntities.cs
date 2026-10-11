namespace BookStore.Domain.Entities;

public sealed class BookCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<Book> Books { get; set; } = new List<Book>();
}

public sealed class Publisher
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public ICollection<Book> Books { get; set; } = new List<Book>();
}

public sealed class Author
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public ICollection<BookAuthor> Books { get; set; } = new List<BookAuthor>();
}

public sealed class BookAuthor
{
    public string BookId { get; set; } = string.Empty;
    public Book Book { get; set; } = null!;
    public int AuthorId { get; set; }
    public Author Author { get; set; } = null!;
}

public sealed class Book
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal SalePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public string Unit { get; set; } = "cuốn";
    public string CategoryId { get; set; } = string.Empty;
    public BookCategory Category { get; set; } = null!;
    public int? PublisherId { get; set; }
    public Publisher? Publisher { get; set; }
    public ICollection<BookAuthor> Authors { get; set; } = new List<BookAuthor>();
}
