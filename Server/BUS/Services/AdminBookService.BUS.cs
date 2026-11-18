using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TrangSach.Shared.DTO;

namespace TrangSach.BUS
{
    /// <summary>
    /// Service quản lý sách. Giữ danh sách BookItemDto trong RAM,
    /// nạp bất đồng bộ không gây deadlock luồng UI, cập nhật List khi có thay đổi.
    /// </summary>
    public sealed class AdminBookService
    {
        private readonly BookStoreDao _dao;
        private List<BookItemDto> _books = new();

        public AdminBookService(BookStoreDao dao)
        {
            _dao = dao;
        }

        public IReadOnlyList<BookItemDto> Books => _books;

        // ----------------------------------------------------------------
        // GET books
        public async Task<IReadOnlyList<BookItemDto>> GetBooksAsync(CancellationToken ct = default)
        {
            if (_books.Count == 0)
            {
                _books = await _dao.Catalog.GetAllBooksAsync(ct).ConfigureAwait(false);
            }
            return _books;
        }

        // ----------------------------------------------------------------
        // GET for edit
        public async Task<OperationResult<BookEditDetailsDto>> GetBookForEditAsync(string id, CancellationToken ct = default)
        {
            var dto = await _dao.Catalog.GetBookForEditAsync(id, ct).ConfigureAwait(false);
            if (dto == null) return OperationResult<BookEditDetailsDto>.NotFound();
            return OperationResult<BookEditDetailsDto>.Success(dto);
        }

        // ----------------------------------------------------------------
        // CREATE
        public async Task<OperationResult<BookItemDto>> CreateBookAsync(BookManagementCommandDto command, CancellationToken ct = default)
        {
            var err = Validate(command);
            if (err != null) return OperationResult<BookItemDto>.Invalid(err);

            var id = $"S{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}";
            await _dao.Catalog.InsertBookAsync(id, command, ct).ConfigureAwait(false);

            // Reload list để lấy thông tin join (tên tác giả, loại, NXB)
            _books = await _dao.Catalog.GetAllBooksAsync(ct).ConfigureAwait(false);
            var created = _books.FirstOrDefault(b => b.Id == id);
            if (created == null) return OperationResult<BookItemDto>.Invalid("Không thể tải sách vừa tạo.");
            return OperationResult<BookItemDto>.Created(created);
        }

        // ----------------------------------------------------------------
        // UPDATE
        public async Task<OperationResult<Unit>> UpdateBookAsync(string id, BookManagementCommandDto command, CancellationToken ct = default)
        {
            var err = Validate(command);
            if (err != null) return OperationResult<Unit>.Invalid(err);

            if (_books.Count == 0)
                await GetBooksAsync(ct).ConfigureAwait(false);

            var existing = _books.FirstOrDefault(b => b.Id == id);
            if (existing == null) return OperationResult<Unit>.NotFound();

            await _dao.Catalog.UpdateBookAsync(id, command, ct).ConfigureAwait(false);

            _books = await _dao.Catalog.GetAllBooksAsync(ct).ConfigureAwait(false);
            return OperationResult<Unit>.Success(new Unit());
        }

        // ----------------------------------------------------------------
        // HIDE
        public async Task<OperationResult<Unit>> HideBookAsync(string id, CancellationToken ct = default)
        {
            if (_books.Count == 0)
                await GetBooksAsync(ct).ConfigureAwait(false);

            var existing = _books.FirstOrDefault(b => b.Id == id);
            if (existing == null) return OperationResult<Unit>.NotFound();

            await _dao.Catalog.SetBookVisibilityAsync(id, false, ct).ConfigureAwait(false);
            _books = await _dao.Catalog.GetAllBooksAsync(ct).ConfigureAwait(false);
            return OperationResult<Unit>.Success(new Unit());
        }

        // ----------------------------------------------------------------
        // SET VISIBILITY
        public async Task<OperationResult<Unit>> SetVisibilityAsync(string id, BookVisibilityCommandDto command, CancellationToken ct = default)
        {
            if (_books.Count == 0)
                await GetBooksAsync(ct).ConfigureAwait(false);

            var existing = _books.FirstOrDefault(b => b.Id == id);
            if (existing == null) return OperationResult<Unit>.NotFound();

            await _dao.Catalog.SetBookVisibilityAsync(id, command.IsActive, ct).ConfigureAwait(false);
            _books = await _dao.Catalog.GetAllBooksAsync(ct).ConfigureAwait(false);
            return OperationResult<Unit>.Success(new Unit());
        }

        // ----------------------------------------------------------------
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
