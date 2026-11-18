using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrangSach.Shared.DTO;

namespace TrangSach.BUS
{
    public class AdminVoucherService
    {
        private readonly BookStoreDao dao;

        // Danh sách voucher được load vào RAM
        private List<VoucherSummaryDto> _vouchers = new();

        public AdminVoucherService(BookStoreDao dao)
        {
            this.dao = dao;
        }

        // ---- READ ----
        public IReadOnlyList<VoucherSummaryDto> GetVouchers() => _vouchers;

        // ----------------------------------------------------------------
        // Synchronous Methods (Phong cách thiết kế 3 lớp truyền thống)
        // ----------------------------------------------------------------
        public IReadOnlyList<VoucherSummaryDto> GetAllVouchersList()
        {
            if (_vouchers.Count == 0) _vouchers = dao.Promotions.GetAllVouchers();
            return _vouchers;
        }

        public OperationResult<Unit> Update(string id, VoucherUpdateCommandDto command)
        {
            if (string.IsNullOrWhiteSpace(command.Status) || command.Status.Trim().Length > 30
                || (command.MaxUsageCount != null && (command.MaxUsageCount < 1 || command.MaxUsageCount > 1000000)))
            {
                return OperationResult<Unit>.Invalid("Trạng thái hoặc giới hạn lượt dùng voucher không hợp lệ.");
            }

            if (_vouchers.Count == 0) GetAllVouchersList();
            var cached = _vouchers.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
            if (cached == null) return OperationResult<Unit>.NotFound();

            if (command.MaxUsageCount != null && command.MaxUsageCount < cached.UsageCount)
                return OperationResult<Unit>.Invalid("Giới hạn lượt dùng không được nhỏ hơn số lượt đã sử dụng.");

            dao.Promotions.UpdateVoucher(id, command.Status.Trim(), command.ExpiresAt, command.MaxUsageCount);

            cached.Status = command.Status.Trim();
            cached.ExpiresAt = command.ExpiresAt;
            cached.MaxUsageCount = command.MaxUsageCount;
            return OperationResult<Unit>.Success(new Unit());
        }

        public OperationResult<Unit> Delete(string id)
        {
            if (_vouchers.Count == 0) GetAllVouchersList();
            var cached = _vouchers.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
            if (cached == null) return OperationResult<Unit>.NotFound();

            if (dao.Promotions.VoucherHasBeenUsed(id))
                return OperationResult<Unit>.Conflict("Voucher đã được dùng trong đơn hàng; hãy chuyển trạng thái sang ngừng sử dụng.");

            dao.Promotions.DeleteVoucher(id);
            _vouchers.RemoveAll(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
            return OperationResult<Unit>.Success(new Unit());
        }

        public async Task<IReadOnlyList<VoucherSummaryDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            if (_vouchers.Count == 0)
                _vouchers = await dao.Promotions.GetAllVouchersAsync(cancellationToken).ConfigureAwait(false);
            return _vouchers;
        }

        // ---- UPDATE ----
        public async Task<OperationResult<Unit>> UpdateAsync(string id, VoucherUpdateCommandDto command, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.Status) || command.Status.Trim().Length > 30
                || (command.MaxUsageCount != null && (command.MaxUsageCount < 1 || command.MaxUsageCount > 1000000)))
            {
                return OperationResult<Unit>.Invalid("Trạng thái hoặc giới hạn lượt dùng voucher không hợp lệ.");
            }

            if (_vouchers.Count == 0) await GetAsync(cancellationToken).ConfigureAwait(false);
            var cached = _vouchers.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
            if (cached == null) return OperationResult<Unit>.NotFound();

            if (command.MaxUsageCount != null && command.MaxUsageCount < cached.UsageCount)
                return OperationResult<Unit>.Invalid("Giới hạn lượt dùng không được nhỏ hơn số lượt đã sử dụng.");

            await dao.Promotions.UpdateVoucherAsync(id, command.Status.Trim(), command.ExpiresAt, command.MaxUsageCount, cancellationToken).ConfigureAwait(false);

            cached.Status = command.Status.Trim();
            cached.ExpiresAt = command.ExpiresAt;
            cached.MaxUsageCount = command.MaxUsageCount;
            return OperationResult<Unit>.Success(new Unit());
        }

        // ---- DELETE ----
        public async Task<OperationResult<Unit>> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            if (_vouchers.Count == 0) await GetAsync(cancellationToken).ConfigureAwait(false);
            var cached = _vouchers.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
            if (cached == null) return OperationResult<Unit>.NotFound();

            if (await dao.Promotions.VoucherHasBeenUsedAsync(id, cancellationToken).ConfigureAwait(false))
                return OperationResult<Unit>.Conflict("Voucher đã được dùng trong đơn hàng; hãy chuyển trạng thái sang ngừng sử dụng.");

            await dao.Promotions.DeleteVoucherAsync(id, cancellationToken).ConfigureAwait(false);
            _vouchers.RemoveAll(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
            return OperationResult<Unit>.Success(new Unit());
        }
    }
}
