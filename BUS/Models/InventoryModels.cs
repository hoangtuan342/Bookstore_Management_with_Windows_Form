namespace TrangSach.BUS.Models;

public sealed record SupplierCommand(string Name, string? PhoneNumber);
public sealed record ReceiptLineCommand(string BookId, int Quantity, decimal UnitCost);
public sealed record ReceiptCommand(string SupplierId, IReadOnlyList<ReceiptLineCommand> Lines);
public sealed record ReceiptCreated(string Id, decimal TotalAmount, DateTime CreatedAt);
