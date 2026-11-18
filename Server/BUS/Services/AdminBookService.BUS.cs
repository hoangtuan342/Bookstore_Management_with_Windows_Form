using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using TrangSach.Shared.DTO;

namespace TrangSach.BUS
{
    /// <summary>
    /// Service quản lý sách. Giữ danh sách BookItemDto trong RAM,
    /// lưu danh sách trong RAM theo mô hình 3 lớp đồng bộ, cập nhật List khi có thay đổi.
    /// </summary>
    public sealed class AdminBookService
    {
        private readonly BookStoreDao _dao;
        private List<BookItemDto> _books = new();

        public AdminBookService(BookStoreDao dao)
        {
            _dao = dao;
        }

        public List<BookItemDto> Books => _books;

        // ----------------------------------------------------------------
        // Synchronous Methods (Phong cách thiết kế 3 lớp truyền thống)
        // ----------------------------------------------------------------
        public List<BookItemDto> GetBooks()
        {
            if (_books.Count == 0) _books = _dao.Catalog.GetAllBooks();
            return _books;
        }

        public OperationResult<BookEditDetailsDto> GetBookForEdit(string id)
        {
            var dto = _dao.Catalog.GetBookForEdit(id);
            if (dto == null) return OperationResult<BookEditDetailsDto>.NotFound();
            return OperationResult<BookEditDetailsDto>.Success(dto);
        }

        public OperationResult<BookItemDto> CreateBook(BookManagementCommandDto command)
        {
            var err = Validate(command);
            if (err != null) return OperationResult<BookItemDto>.Invalid(err);

            var id = $"S{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}";
            _dao.Catalog.InsertBook(id, command);

            _books = _dao.Catalog.GetAllBooks();
            var created = _books.FirstOrDefault(b => b.Id == id);
            if (created == null) return OperationResult<BookItemDto>.Invalid("Không thể tải sách vừa tạo.");
            return OperationResult<BookItemDto>.Created(created);
        }

        public OperationResult<Unit> UpdateBook(string id, BookManagementCommandDto command)
        {
            var err = Validate(command);
            if (err != null) return OperationResult<Unit>.Invalid(err);

            if (_books.Count == 0) GetBooks();
            var existing = _books.FirstOrDefault(b => b.Id == id);
            if (existing == null) return OperationResult<Unit>.NotFound();

            _dao.Catalog.UpdateBook(id, command);
            _books = _dao.Catalog.GetAllBooks();
            return OperationResult<Unit>.Success(new Unit());
        }

        public OperationResult<Unit> HideBook(string id)
        {
            if (_books.Count == 0) GetBooks();
            var existing = _books.FirstOrDefault(b => b.Id == id);
            if (existing == null) return OperationResult<Unit>.NotFound();

            _dao.Catalog.SetBookVisibility(id, false);
            _books = _dao.Catalog.GetAllBooks();
            return OperationResult<Unit>.Success(new Unit());
        }

        public OperationResult<Unit> SetVisibility(string id, BookVisibilityCommandDto command)
        {
            if (_books.Count == 0) GetBooks();
            var existing = _books.FirstOrDefault(b => b.Id == id);
            if (existing == null) return OperationResult<Unit>.NotFound();

            _dao.Catalog.SetBookVisibility(id, command.IsActive);
            _books = _dao.Catalog.GetAllBooks();
            return OperationResult<Unit>.Success(new Unit());
        }
        private static string? Validate(BookManagementCommandDto c)
        {
            if (string.IsNullOrWhiteSpace(c.Title) || c.Title.Trim().Length > 250)
                return "Tên sách phải có từ 1 đến 250 ký tự.";
            if (c.SalePrice <= 0 || c.SalePrice > 999_999_999)
                return "Giá bán phải lớn hơn 0 và không vượt quá giới hạn.";
            if (string.IsNullOrWhiteSpace(c.Unit) || c.Unit.Trim().Length > 30)
                return "Đơn vị tính không hợp lệ.";
            if (string.IsNullOrWhiteSpace(c.CategoryId) || c.CategoryId.Trim().Length > 20)
                return "Mã thể loại không hợp lệ.";
            if (c.Description != null && c.Description.Trim().Length > 4000)
                return "Mô tả không được vượt quá 4.000 ký tự.";
            if (c.AuthorIds.Distinct().Count() != c.AuthorIds.Count)
                return "Không được chọn trùng tác giả.";
            return null;
        }
    }
}
