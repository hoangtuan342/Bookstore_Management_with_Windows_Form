namespace BookStore.Shared.Models;

public sealed record BookItem(
    string Id,
    string Title,
    string Author,
    string Category,
    decimal Price,
    string CoverStyle,
    string Label,
    string Description,
    int AvailableStock = 0);
