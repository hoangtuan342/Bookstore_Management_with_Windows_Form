namespace TrangSach.BUS.Models;

public sealed record BookEditDetails(string Id, string Title, decimal SalePrice, string Unit, string CategoryId,
    int? PublisherId, IReadOnlyList<int> AuthorIds, bool IsActive);

public sealed record BookManagementCommand(string Title, decimal SalePrice, string Unit, string CategoryId,
    int? PublisherId, IReadOnlyList<int> AuthorIds);

public sealed record BookVisibilityCommand(bool IsActive);
