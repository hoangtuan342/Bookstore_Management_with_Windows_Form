using System.Data;
using System.Security.Cryptography;
using BookStore.Api.Contracts;
using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using BookStore.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public sealed class AdminInventoryController(BookStoreDbContext db) : ControllerBase
{
    [Authorize(Roles = StaffRoles.ManagerCatalogInventory)]
    [HttpGet("lookups")]
    public async Task<IActionResult> GetLookups(CancellationToken cancellationToken) => Ok(new
    {
        Categories = await db.Categories.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new { item.Id, item.Name }).ToListAsync(cancellationToken),
        Authors = await db.Authors.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new { item.Id, item.Name }).ToListAsync(cancellationToken),
        Publishers = await db.Publishers.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new { item.Id, item.Name }).ToListAsync(cancellationToken),
        Suppliers = await db.Suppliers.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new { item.Id, item.Name, item.PhoneNumber }).ToListAsync(cancellationToken)
    });

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers(CancellationToken cancellationToken) =>
        Ok(await db.Suppliers.AsNoTracking().OrderBy(item => item.Name).ToListAsync(cancellationToken));

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier(SupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = new Supplier
        {
            Id = $"NCC{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}",
            Name = request.Name.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim()
        };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, supplier);
    }

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPut("suppliers/{id}")]
    public async Task<IActionResult> UpdateSupplier(string id, SupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = await db.Suppliers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (supplier is null) return NotFound();
        supplier.Name = request.Name.Trim();
        supplier.PhoneNumber = request.PhoneNumber?.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpDelete("suppliers/{id}")]
    public async Task<IActionResult> DeleteSupplier(string id, CancellationToken cancellationToken)
    {
        var supplier = await db.Suppliers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (supplier is null) return NotFound();
        if (await db.PurchaseReceipts.AnyAsync(item => item.SupplierId == id, cancellationToken))
            return Conflict(new { message = "Nhà cung cấp đã có phiếu nhập; hãy giữ lại để tra cứu lịch sử." });
        db.Suppliers.Remove(supplier);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpGet("receipts")]
    public async Task<IActionResult> GetReceipts(CancellationToken cancellationToken)
    {
        var receipts = await db.PurchaseReceipts.AsNoTracking()
            .Include(receipt => receipt.Supplier)
            .Include(receipt => receipt.Lines).ThenInclude(line => line.Book)
            .OrderByDescending(receipt => receipt.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(receipts.Select(receipt => new
        {
            receipt.Id,
            receipt.CreatedAt,
            receipt.Status,
            receipt.SupplierId,
            SupplierName = receipt.Supplier.Name,
            receipt.TotalAmount,
            Lines = receipt.Lines.Select(line => new
            {
                line.BookId,
                BookTitle = line.Book.Title,
                line.Quantity,
                line.UnitCost,
                line.LineTotal
            })
        }));
    }

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPost("receipts")]
    public async Task<IActionResult> CreateReceipt(PurchaseReceiptRequest request, CancellationToken cancellationToken)
    {
        if (request.Lines.Select(line => line.BookId).Distinct().Count() != request.Lines.Count)
        {
            return BadRequest(new { message = "Mỗi đầu sách chỉ được xuất hiện một lần trong một phiếu nhập." });
        }

        var employeeAccountId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var employeeId = await db.Employees.Where(employee => employee.UserAccountId == employeeAccountId)
            .Select(employee => employee.Id).SingleOrDefaultAsync(cancellationToken);
        if (employeeId is null)
        {
            return Forbid();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var supplierExists = await db.Suppliers.AnyAsync(item => item.Id == request.SupplierId, cancellationToken);
        var requestedBookIds = request.Lines.Select(line => line.BookId).Distinct().ToArray();
        var books = await db.Books.Where(book => requestedBookIds.Contains(book.Id)).ToDictionaryAsync(book => book.Id, cancellationToken);
        if (!supplierExists || books.Count != requestedBookIds.Length)
        {
            return BadRequest(new { message = "Nhà cung cấp hoặc đầu sách không hợp lệ." });
        }

        var lines = request.Lines.Select(line => new PurchaseReceiptLine
        {
            BookId = line.BookId,
            Quantity = line.Quantity,
            UnitCost = line.UnitCost,
            LineTotal = line.UnitCost * line.Quantity
        }).ToList();

        var receipt = new PurchaseReceipt
        {
            Id = $"PNH{DateTime.UtcNow:yyyyMMddHHmmss}{Convert.ToHexString(RandomNumberGenerator.GetBytes(2))}",
            CreatedAt = DateTime.UtcNow,
            EmployeeId = employeeId,
            SupplierId = request.SupplierId,
            TotalAmount = lines.Sum(line => line.LineTotal),
            Lines = lines
        };

        foreach (var line in lines)
        {
            books[line.BookId].StockQuantity += line.Quantity;
        }

        db.PurchaseReceipts.Add(receipt);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { receipt.Id, receipt.TotalAmount, receipt.CreatedAt });
    }

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPut("receipts/{id}")]
    public async Task<IActionResult> UpdateReceipt(string id, PurchaseReceiptRequest request, CancellationToken cancellationToken)
    {
        if (request.Lines.Select(line => line.BookId).Distinct().Count() != request.Lines.Count)
            return BadRequest(new { message = "Mỗi đầu sách chỉ được xuất hiện một lần trong một phiếu nhập." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var receipt = await db.PurchaseReceipts.Include(item => item.Lines).ThenInclude(line => line.Book)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (receipt is null) return NotFound();
        if (receipt.Status != "Posted")
            return Conflict(new { message = "Phiếu đã đảo nên không thể sửa." });

        var requestedBookIds = request.Lines.Select(line => line.BookId).ToArray();
        var books = await db.Books.Where(book => requestedBookIds.Contains(book.Id))
            .ToDictionaryAsync(book => book.Id, cancellationToken);
        if (!await db.Suppliers.AnyAsync(item => item.Id == request.SupplierId, cancellationToken)
            || books.Count != requestedBookIds.Length)
            return BadRequest(new { message = "Nhà cung cấp hoặc đầu sách không hợp lệ." });

        var oldLines = receipt.Lines.ToDictionary(line => line.BookId);
        foreach (var bookId in oldLines.Keys.Union(requestedBookIds))
        {
            var oldQuantity = oldLines.GetValueOrDefault(bookId)?.Quantity ?? 0;
            var newQuantity = request.Lines.FirstOrDefault(line => line.BookId == bookId)?.Quantity ?? 0;
            var book = books.GetValueOrDefault(bookId) ?? oldLines[bookId].Book;
            if (book.StockQuantity + newQuantity - oldQuantity < 0)
                return Conflict(new { message = $"Không thể giảm '{book.Title}' xuống số lượng yêu cầu vì một phần hàng đã bán hoặc điều chỉnh." });
        }

        foreach (var bookId in oldLines.Keys.Union(requestedBookIds))
        {
            var oldQuantity = oldLines.GetValueOrDefault(bookId)?.Quantity ?? 0;
            var requestedLine = request.Lines.FirstOrDefault(line => line.BookId == bookId);
            var newQuantity = requestedLine?.Quantity ?? 0;
            var book = books.GetValueOrDefault(bookId) ?? oldLines[bookId].Book;
            book.StockQuantity += newQuantity - oldQuantity;

            if (oldLines.TryGetValue(bookId, out var existingLine))
            {
                if (requestedLine is null) db.PurchaseReceiptLines.Remove(existingLine);
                else
                {
                    existingLine.Quantity = requestedLine.Quantity;
                    existingLine.UnitCost = requestedLine.UnitCost;
                    existingLine.LineTotal = requestedLine.UnitCost * requestedLine.Quantity;
                }
            }
            else if (requestedLine is not null)
            {
                receipt.Lines.Add(new PurchaseReceiptLine
                {
                    ReceiptId = receipt.Id,
                    BookId = requestedLine.BookId,
                    Quantity = requestedLine.Quantity,
                    UnitCost = requestedLine.UnitCost,
                    LineTotal = requestedLine.UnitCost * requestedLine.Quantity
                });
            }
        }

        receipt.SupplierId = request.SupplierId;
        receipt.TotalAmount = request.Lines.Sum(line => line.UnitCost * line.Quantity);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpDelete("receipts/{id}")]
    [HttpPut("receipts/{id}/cancel")]
    public async Task<IActionResult> CancelReceipt(string id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var receipt = await db.PurchaseReceipts.Include(item => item.Lines).ThenInclude(line => line.Book)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (receipt is null) return NotFound();
        if (receipt.Status != "Posted") return Conflict(new { message = "Phiếu nhập này đã được hủy trước đó." });
        var unavailable = receipt.Lines.FirstOrDefault(line => line.Book.StockQuantity < line.Quantity);
        if (unavailable is not null)
            return Conflict(new { message = $"Không thể đảo phiếu: tồn '{unavailable.Book.Title}' hiện là {unavailable.Book.StockQuantity}, thấp hơn số lượng nhập {unavailable.Quantity}. Cần xử lý hàng đã bán/điều chỉnh trước." });

        foreach (var line in receipt.Lines) line.Book.StockQuantity -= line.Quantity;
        receipt.Status = "Cancelled";
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }
}
