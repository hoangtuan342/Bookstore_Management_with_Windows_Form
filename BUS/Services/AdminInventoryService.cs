using System.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using BookStore.Domain.Entities;
using BookStore.Shared.Models;
using TrangSach.BUS.Models;
using TrangSach.BUS.Results;
using TrangSach.DAO.Entities;

namespace TrangSach.BUS.Services;

public sealed class AdminInventoryService(BookStoreDao dao)
{
    public async Task<ManagerLookups> GetLookupsAsync(CancellationToken ct = default)
    {
        var categories = await dao.Categories.GetAllAsync(ct);
        var authors = await dao.Authors.GetAllAsync(ct);
        var publishers = await dao.Publishers.GetAllAsync(ct);
        var suppliers = await dao.Suppliers.GetAllOrderedAsync(ct);
        return new(categories.OrderBy(x => x.Name).Select(x => new CategoryOption(x.Id, x.Name)).ToArray(),
            authors.OrderBy(x => x.Name).Select(x => new IdNameOption(x.Id, x.Name)).ToArray(),
            publishers.OrderBy(x => x.Name).Select(x => new IdNameOption(x.Id, x.Name)).ToArray(),
            suppliers.Select(x => new SupplierOption(x.Id, x.Name, x.PhoneNumber)).ToArray());
    }

    public async Task<IReadOnlyList<SupplierOption>> GetSuppliersAsync(CancellationToken ct = default) =>
        (await dao.Suppliers.GetAllOrderedAsync(ct)).Select(x => new SupplierOption(x.Id, x.Name, x.PhoneNumber)).ToArray();

    public async Task<OperationResult<SupplierOption>> CreateSupplierAsync(SupplierCommand command, CancellationToken ct = default)
    {
        var validation = ValidateSupplier(command);
        if (validation is not null) return OperationResult<SupplierOption>.Invalid(validation);
        var supplier = new Supplier { Id = $"NCC{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}", Name = command.Name.Trim(), PhoneNumber = command.PhoneNumber?.Trim() };
        dao.Suppliers.Add(supplier);
        await dao.SaveChangesAsync(ct);
        return OperationResult<SupplierOption>.Created(new(supplier.Id, supplier.Name, supplier.PhoneNumber));
    }

    public async Task<OperationResult<Unit>> UpdateSupplierAsync(string id, SupplierCommand command, CancellationToken ct = default)
    {
        var validation = ValidateSupplier(command);
        if (validation is not null) return OperationResult<Unit>.Invalid(validation);
        var supplier = await dao.Suppliers.FindSingleAsync(x => x.Id == id, ct);
        if (supplier is null) return OperationResult<Unit>.NotFound();
        supplier.Name = command.Name.Trim(); supplier.PhoneNumber = command.PhoneNumber?.Trim();
        await dao.SaveChangesAsync(ct);
        return OperationResult<Unit>.Success(new Unit());
    }

    public async Task<OperationResult<Unit>> DeleteSupplierAsync(string id, CancellationToken ct = default)
    {
        var supplier = await dao.Suppliers.FindSingleAsync(x => x.Id == id, ct);
        if (supplier is null) return OperationResult<Unit>.NotFound();
        if (await dao.PurchaseReceipts.HasSupplierAsync(id, ct)) return OperationResult<Unit>.Conflict("Nhà cung cấp đã có phiếu nhập; hãy giữ lại để tra cứu lịch sử.");
        dao.Suppliers.Remove(supplier); await dao.SaveChangesAsync(ct);
        return OperationResult<Unit>.Success(new Unit());
    }

    public async Task<IReadOnlyList<ManagerReceipt>> GetReceiptsAsync(CancellationToken ct = default)
    {
        var receipts = await dao.PurchaseReceipts.GetRecentWithDetailsAsync(100, ct);
        return receipts.Select(x => new ManagerReceipt(x.Id, x.CreatedAt, x.Status, x.SupplierId, x.Supplier.Name, x.TotalAmount,
            x.Lines.Select(line => new ManagerReceiptLine(line.BookId, line.Book.Title, line.Quantity, line.UnitCost, line.LineTotal)).ToArray())).ToArray();
    }

    public async Task<OperationResult<ReceiptCreated>> CreateReceiptAsync(ReceiptCommand command, int? accountId, CancellationToken ct = default)
    {
        var validation = ValidateReceipt(command);
        if (validation is not null) return OperationResult<ReceiptCreated>.Invalid(validation);
        if (command.Lines.Select(x => x.BookId).Distinct().Count() != command.Lines.Count)
            return OperationResult<ReceiptCreated>.Invalid("Mỗi đầu sách chỉ được xuất hiện một lần trong một phiếu nhập.");
        var employeeId = accountId is int id ? await dao.Employees.FindIdByAccountAsync(id, ct) : null;
        if (employeeId is null) return OperationResult<ReceiptCreated>.Forbidden();
        await using var transaction = await dao.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var supplierExists = await dao.Suppliers.ExistsAsync(x => x.Id == command.SupplierId, ct);
        var requestedIds = command.Lines.Select(x => x.BookId).Distinct().ToArray();
        var books = await dao.Books.GetByIdsAsync(requestedIds, ct);
        if (!supplierExists || books.Count != requestedIds.Length)
            return OperationResult<ReceiptCreated>.Invalid("Nhà cung cấp hoặc đầu sách không hợp lệ.");
        var lines = command.Lines.Select(x => new PurchaseReceiptLine { BookId = x.BookId, Quantity = x.Quantity, UnitCost = x.UnitCost, LineTotal = x.UnitCost * x.Quantity }).ToList();
        var now = DateTime.UtcNow;
        var receipt = new PurchaseReceipt { Id = $"PNH{now:yyyyMMddHHmmss}{Convert.ToHexString(RandomNumberGenerator.GetBytes(2))}", CreatedAt = now,
            EmployeeId = employeeId, SupplierId = command.SupplierId, TotalAmount = lines.Sum(x => x.LineTotal), Lines = lines };
        foreach (var line in lines) books[line.BookId].StockQuantity += line.Quantity;
        dao.PurchaseReceipts.Add(receipt); await dao.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return OperationResult<ReceiptCreated>.Created(new(receipt.Id, receipt.TotalAmount, receipt.CreatedAt));
    }

    public async Task<OperationResult<Unit>> UpdateReceiptAsync(string id, ReceiptCommand command, CancellationToken ct = default)
    {
        var validation = ValidateReceipt(command);
        if (validation is not null) return OperationResult<Unit>.Invalid(validation);
        if (command.Lines.Select(x => x.BookId).Distinct().Count() != command.Lines.Count)
            return OperationResult<Unit>.Invalid("Mỗi đầu sách chỉ được xuất hiện một lần trong một phiếu nhập.");
        await using var transaction = await dao.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var receipt = await dao.PurchaseReceipts.FindWithLinesAndBooksAsync(id, ct);
        if (receipt is null) return OperationResult<Unit>.NotFound();
        if (receipt.Status != "Posted") return OperationResult<Unit>.Conflict("Phiếu đã đảo nên không thể sửa.");
        var requestedIds = command.Lines.Select(x => x.BookId).ToArray();
        var books = await dao.Books.GetByIdsAsync(requestedIds, ct);
        if (!await dao.Suppliers.ExistsAsync(x => x.Id == command.SupplierId, ct) || books.Count != requestedIds.Length)
            return OperationResult<Unit>.Invalid("Nhà cung cấp hoặc đầu sách không hợp lệ.");
        var oldLines = receipt.Lines.ToDictionary(x => x.BookId);
        foreach (var bookId in oldLines.Keys.Union(requestedIds))
        {
            var oldQty = oldLines.GetValueOrDefault(bookId)?.Quantity ?? 0;
            var newLine = command.Lines.FirstOrDefault(x => x.BookId == bookId);
            var book = books.GetValueOrDefault(bookId) ?? oldLines[bookId].Book;
            if (book.StockQuantity + (newLine?.Quantity ?? 0) - oldQty < 0)
                return OperationResult<Unit>.Conflict($"Không thể giảm '{book.Title}' xuống số lượng yêu cầu vì một phần hàng đã bán hoặc điều chỉnh.");
        }
        foreach (var bookId in oldLines.Keys.Union(requestedIds))
        {
            var oldQty = oldLines.GetValueOrDefault(bookId)?.Quantity ?? 0;
            var newLine = command.Lines.FirstOrDefault(x => x.BookId == bookId);
            var newQty = newLine?.Quantity ?? 0;
            var book = books.GetValueOrDefault(bookId) ?? oldLines[bookId].Book;
            book.StockQuantity += newQty - oldQty;
            if (oldLines.TryGetValue(bookId, out var existing))
            {
                if (newLine is null) dao.PurchaseReceiptLines.Remove(existing);
                else { existing.Quantity = newQty; existing.UnitCost = newLine.UnitCost; existing.LineTotal = newLine.UnitCost * newQty; }
            }
            else if (newLine is not null) receipt.Lines.Add(new PurchaseReceiptLine { ReceiptId = receipt.Id, BookId = newLine.BookId, Quantity = newQty,
                UnitCost = newLine.UnitCost, LineTotal = newLine.UnitCost * newQty });
        }
        receipt.SupplierId = command.SupplierId; receipt.TotalAmount = command.Lines.Sum(x => x.UnitCost * x.Quantity);
        await dao.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return OperationResult<Unit>.Success(new Unit());
    }

    public async Task<OperationResult<Unit>> CancelReceiptAsync(string id, CancellationToken ct = default)
    {
        await using var transaction = await dao.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var receipt = await dao.PurchaseReceipts.FindWithLinesAndBooksAsync(id, ct);
        if (receipt is null) return OperationResult<Unit>.NotFound();
        if (receipt.Status != "Posted") return OperationResult<Unit>.Conflict("Phiếu nhập này đã được hủy trước đó.");
        var unavailable = receipt.Lines.FirstOrDefault(x => x.Book.StockQuantity < x.Quantity);
        if (unavailable is not null) return OperationResult<Unit>.Conflict($"Không thể đảo phiếu: tồn '{unavailable.Book.Title}' hiện là {unavailable.Book.StockQuantity}, thấp hơn số lượng nhập {unavailable.Quantity}. Cần xử lý hàng đã bán/điều chỉnh trước.");
        foreach (var line in receipt.Lines) line.Book.StockQuantity -= line.Quantity;
        receipt.Status = "Cancelled"; await dao.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return OperationResult<Unit>.Success(new Unit());
    }

    private static string? ValidateSupplier(SupplierCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length is < 2 or > 180) return "Tên nhà cung cấp cần từ 2 đến 180 ký tự.";
        if (command.PhoneNumber?.Length > 30 || !string.IsNullOrWhiteSpace(command.PhoneNumber) && !new PhoneAttribute().IsValid(command.PhoneNumber))
            return "Số điện thoại nhà cung cấp không hợp lệ.";
        return null;
    }

    private static string? ValidateReceipt(ReceiptCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.SupplierId)) return "Cần chọn nhà cung cấp.";
        if (command.Lines.Count is < 1 or > 1000) return "Phiếu nhập cần có từ 1 đến 1000 dòng sách.";
        if (command.Lines.Any(x => string.IsNullOrWhiteSpace(x.BookId) || x.Quantity is < 1 or > 100000 || x.UnitCost is <= 0 or > 999999999))
            return "Dòng phiếu nhập có sách, số lượng hoặc đơn giá không hợp lệ.";
        return null;
    }
}
