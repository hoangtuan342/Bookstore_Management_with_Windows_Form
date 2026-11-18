using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TrangSach.Shared.DTO;

namespace TrangSach.BUS
{
    public class AdminInventoryService
    {
        private readonly BookStoreDao _dao;

        private List<SupplierOptionDto> _suppliers = new();
        private List<CategoryOptionDto> _categories = new();
        private List<IdNameOptionDto> _authors = new();
        private List<IdNameOptionDto> _publishers = new();
        private List<ManagerReceiptDto> _receipts = new();

        public AdminInventoryService(BookStoreDao dao)
        {
            _dao = dao;
        }

        public IReadOnlyList<SupplierOptionDto> GetSuppliers() => _suppliers;
        public IReadOnlyList<CategoryOptionDto> GetCategories() => _categories;
        public IReadOnlyList<IdNameOptionDto> GetAuthors() => _authors;
        public IReadOnlyList<IdNameOptionDto> GetPublishers() => _publishers;
        public IReadOnlyList<ManagerReceiptDto> GetReceipts() => _receipts;

        public async Task<ManagerLookupsDto> GetLookupsAsync(CancellationToken ct = default)
        {
            if (_categories.Count == 0)
                _categories = await _dao.Catalog.GetAllCategoriesAsync(ct).ConfigureAwait(false);
            if (_authors.Count == 0)
                _authors = await _dao.Catalog.GetAllAuthorsAsync(ct).ConfigureAwait(false);
            if (_publishers.Count == 0)
                _publishers = await _dao.Catalog.GetAllPublishersAsync(ct).ConfigureAwait(false);
            if (_suppliers.Count == 0)
                _suppliers = await _dao.Inventory.GetAllSuppliersAsync(ct).ConfigureAwait(false);

            return new ManagerLookupsDto(_categories.ToArray(), _authors.ToArray(), _publishers.ToArray(), _suppliers.ToArray());
        }

        public async Task<IReadOnlyList<SupplierOptionDto>> GetSuppliersAsync(CancellationToken ct = default)
        {
            if (_suppliers.Count == 0)
                _suppliers = await _dao.Inventory.GetAllSuppliersAsync(ct).ConfigureAwait(false);
            return _suppliers;
        }

        public async Task<IReadOnlyList<ManagerReceiptDto>> GetReceiptsAsync(CancellationToken ct = default)
        {
            if (_receipts.Count == 0)
                _receipts = await _dao.Inventory.GetRecentReceiptsAsync(100, ct).ConfigureAwait(false);
            return _receipts;
        }

        // ---- SUPPLIER CRUD ----
        public async Task<OperationResult<SupplierOptionDto>> CreateSupplierAsync(SupplierCommandDto command, CancellationToken ct = default)
        {
            var err = ValidateSupplier(command);
            if (err != null) return OperationResult<SupplierOptionDto>.Invalid(err);

            var id = $"NCC{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}";
            var dto = new SupplierOptionDto(id, command.Name.Trim(), command.PhoneNumber?.Trim());
            await _dao.Inventory.InsertSupplierAsync(dto, ct).ConfigureAwait(false);

            _suppliers.Add(dto);
            return OperationResult<SupplierOptionDto>.Created(dto);
        }

        public async Task<OperationResult<Unit>> UpdateSupplierAsync(string id, SupplierCommandDto command, CancellationToken ct = default)
        {
            var err = ValidateSupplier(command);
            if (err != null) return OperationResult<Unit>.Invalid(err);

            if (_suppliers.Count == 0)
                await GetSuppliersAsync(ct).ConfigureAwait(false);

            var cached = _suppliers.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            await _dao.Inventory.UpdateSupplierAsync(id, command, ct).ConfigureAwait(false);

            cached.Name = command.Name.Trim();
            cached.PhoneNumber = command.PhoneNumber?.Trim();
            return OperationResult<Unit>.Success(new Unit());
        }

        public async Task<OperationResult<Unit>> DeleteSupplierAsync(string id, CancellationToken ct = default)
        {
            if (_suppliers.Count == 0)
                await GetSuppliersAsync(ct).ConfigureAwait(false);

            var cached = _suppliers.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            if (await _dao.Inventory.SupplierHasReceiptsAsync(id, ct).ConfigureAwait(false))
                return OperationResult<Unit>.Conflict("Nhà cung cấp đã có phiếu nhập hàng; không thể xóa.");

            await _dao.Inventory.DeleteSupplierAsync(id, ct).ConfigureAwait(false);
            _suppliers.RemoveAll(x => x.Id == id);
            return OperationResult<Unit>.Success(new Unit());
        }

        // ---- RECEIPT ----
        public async Task<OperationResult<ManagerReceiptDto>> CreateReceiptAsync(ReceiptCommandDto command, int? accountId = null, CancellationToken ct = default)
        {
            var err = ValidateReceipt(command);
            if (err != null) return OperationResult<ManagerReceiptDto>.Invalid(err);

            if (_suppliers.Count == 0)
                await GetSuppliersAsync(ct).ConfigureAwait(false);

            var supplier = _suppliers.FirstOrDefault(x => x.Id == command.SupplierId);
            if (supplier == null) return OperationResult<ManagerReceiptDto>.Invalid("Nhà cung cấp không tồn tại.");

            var receiptId = $"PNH{DateTime.UtcNow:yyyyMMddHHmmss}{Convert.ToHexString(RandomNumberGenerator.GetBytes(2))}";
            var now = DateTime.UtcNow;
            var lines = command.Lines.Select(l =>
                (l.BookId, l.Quantity, l.UnitCost, l.UnitCost * l.Quantity)).ToList();
            var total = lines.Sum(l => l.Item4);

            await _dao.Inventory.InsertReceiptAsync(receiptId, now, null, command.SupplierId, total, lines, ct).ConfigureAwait(false);

            var linesDtos = command.Lines.Select(l =>
                new ManagerReceiptLineDto(l.BookId, l.BookId, l.Quantity, l.UnitCost, l.UnitCost * l.Quantity)).ToArray();
            var dto = new ManagerReceiptDto(receiptId, now, "Completed", command.SupplierId, supplier.Name, total, linesDtos);
            _receipts.Insert(0, dto);

            return OperationResult<ManagerReceiptDto>.Created(dto);
        }

        public async Task<OperationResult<ManagerReceiptDto>> CreateReceiptAsync(ReceiptCommandDto command, string? employeeId, CancellationToken ct = default)
            => await CreateReceiptAsync(command, (int?)null, ct);

        public async Task<OperationResult<Unit>> UpdateReceiptAsync(string id, ReceiptCommandDto command, CancellationToken ct = default)
        {
            if (_receipts.Count == 0)
                await GetReceiptsAsync(ct).ConfigureAwait(false);

            var cached = _receipts.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();
            return OperationResult<Unit>.Success(new Unit());
        }

        public async Task<OperationResult<Unit>> CancelReceiptAsync(string id, CancellationToken ct = default)
        {
            if (_receipts.Count == 0)
                await GetReceiptsAsync(ct).ConfigureAwait(false);

            var cached = _receipts.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();
            cached.Status = "Cancelled";
            return OperationResult<Unit>.Success(new Unit());
        }

        private static string? ValidateSupplier(SupplierCommandDto command)
        {
            if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length < 2 || command.Name.Trim().Length > 180)
                return "Tên nhà cung cấp cần từ 2 đến 180 ký tự.";
            if (command.PhoneNumber != null && command.PhoneNumber.Length > 30
                || (!string.IsNullOrWhiteSpace(command.PhoneNumber) && !new PhoneAttribute().IsValid(command.PhoneNumber)))
                return "Số điện thoại nhà cung cấp không hợp lệ.";
            return null;
        }

        private static string? ValidateReceipt(ReceiptCommandDto command)
        {
            if (string.IsNullOrWhiteSpace(command.SupplierId)) return "Cần chọn nhà cung cấp.";
            if (command.Lines.Count < 1 || command.Lines.Count > 1000) return "Phiếu nhập cần có từ 1 đến 1000 dòng sách.";
            if (command.Lines.Any(x => string.IsNullOrWhiteSpace(x.BookId) || x.Quantity < 1 || x.Quantity > 100000 || x.UnitCost <= 0 || x.UnitCost > 999_999_999))
                return "Dòng phiếu nhập có sách, số lượng hoặc đơn giá không hợp lệ.";
            return null;
        }
    }
}
