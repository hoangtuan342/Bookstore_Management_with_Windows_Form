using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
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

        public List<SupplierOptionDto> GetSuppliers() => _suppliers;
        public List<CategoryOptionDto> GetCategories() => _categories;
        public List<IdNameOptionDto> GetAuthors() => _authors;
        public List<IdNameOptionDto> GetPublishers() => _publishers;
        public List<ManagerReceiptDto> GetReceipts() => _receipts;

        public ManagerLookupsDto GetLookups()
        {
            if (_categories.Count == 0) _categories = _dao.Catalog.GetAllCategories();
            if (_authors.Count == 0) _authors = _dao.Catalog.GetAllAuthors();
            if (_publishers.Count == 0) _publishers = _dao.Catalog.GetAllPublishers();
            if (_suppliers.Count == 0) _suppliers = _dao.Inventory.GetAllSuppliers();

            return new ManagerLookupsDto(_categories, _authors, _publishers, _suppliers);
        }

        public List<SupplierOptionDto> GetAllSuppliersList()
        {
            if (_suppliers.Count == 0) _suppliers = _dao.Inventory.GetAllSuppliers();
            return _suppliers;
        }

        public List<ManagerReceiptDto> GetAllReceiptsList()
        {
            if (_receipts.Count == 0) _receipts = _dao.Inventory.GetRecentReceipts(100);
            return _receipts;
        }

        public SupplierOptionDto CreateSupplier(SupplierCommandDto command)
        {
            var err = ValidateSupplier(command);
            if (err != null) throw new ArgumentException(err);

            var id = $"NCC{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}";
            var dto = new SupplierOptionDto(id, command.Name.Trim(), command.PhoneNumber?.Trim());
            _dao.Inventory.InsertSupplier(dto);
            _suppliers.Add(dto);
            return dto;
        }

        public void UpdateSupplier(string id, SupplierCommandDto command)
        {
            var err = ValidateSupplier(command);
            if (err != null) throw new ArgumentException(err);

            if (_suppliers.Count == 0) GetAllSuppliersList();
            var cached = _suppliers.FirstOrDefault(x => x.Id == id);
            if (cached == null) throw new KeyNotFoundException("Không tìm thấy nhà cung cấp.");

            _dao.Inventory.UpdateSupplier(id, command);
            cached.Name = command.Name.Trim();
            cached.PhoneNumber = command.PhoneNumber?.Trim();
        }

        public void DeleteSupplier(string id)
        {
            if (_suppliers.Count == 0) GetAllSuppliersList();
            var cached = _suppliers.FirstOrDefault(x => x.Id == id);
            if (cached == null) throw new KeyNotFoundException("Không tìm thấy nhà cung cấp.");

            if (_dao.Inventory.SupplierHasReceipts(id))
                throw new InvalidOperationException("Nhà cung cấp đã có phiếu nhập hàng; không thể xóa.");

            _dao.Inventory.DeleteSupplier(id);
            _suppliers.RemoveAll(x => x.Id == id);
        }

        public ManagerReceiptDto CreateReceipt(ReceiptCommandDto command, int? accountId = null)
        {
            var err = ValidateReceipt(command);
            if (err != null) throw new ArgumentException(err);

            if (_suppliers.Count == 0) GetAllSuppliersList();
            var supplier = _suppliers.FirstOrDefault(x => x.Id == command.SupplierId);
            if (supplier == null) throw new InvalidOperationException("Nhà cung cấp không tồn tại.");

            var receiptId = $"PNH{DateTime.UtcNow:yyyyMMddHHmmss}{Convert.ToHexString(RandomNumberGenerator.GetBytes(2))}";
            var now = DateTime.UtcNow;
            var lines = command.Lines.Select(l =>
                (l.BookId, l.Quantity, l.UnitCost, l.UnitCost * l.Quantity)).ToList();
            var total = lines.Sum(l => l.Item4);

            _dao.Inventory.InsertReceipt(receiptId, now, null, command.SupplierId, total, lines);

            var linesDtos = command.Lines.Select(l =>
                new ManagerReceiptLineDto(l.BookId, l.BookId, l.Quantity, l.UnitCost, l.UnitCost * l.Quantity)).ToList();
            var dto = new ManagerReceiptDto(receiptId, now, "Completed", command.SupplierId, supplier.Name, total, linesDtos);
            _receipts.Insert(0, dto);

            return dto;
        }

        public void UpdateReceipt(string id, ReceiptCommandDto command)
        {
            if (_receipts.Count == 0) GetAllReceiptsList();
            var cached = _receipts.FirstOrDefault(x => x.Id == id);
            if (cached == null) throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
        }

        public void CancelReceipt(string id)
        {
            if (_receipts.Count == 0) GetAllReceiptsList();
            var cached = _receipts.FirstOrDefault(x => x.Id == id);
            if (cached == null) throw new KeyNotFoundException("Không tìm thấy phiếu nhập.");
            cached.Status = "Cancelled";
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
