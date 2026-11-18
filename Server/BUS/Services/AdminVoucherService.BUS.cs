using System;
using System.Collections.Generic;
using System.Linq;
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
        public List<VoucherSummaryDto> GetVouchers() => _vouchers;

        // ----------------------------------------------------------------
        // Synchronous Methods (Phong cách thiết kế 3 lớp truyền thống)
        // ----------------------------------------------------------------
        public List<VoucherSummaryDto> GetAllVouchersList()
        {
            if (_vouchers.Count == 0) _vouchers = dao.Promotions.GetAllVouchers();
            return _vouchers;
        }

        public void Update(string id, VoucherUpdateCommandDto command)
        {
            if (string.IsNullOrWhiteSpace(command.Status) || command.Status.Trim().Length > 30
                || (command.MaxUsageCount != null && (command.MaxUsageCount < 1 || command.MaxUsageCount > 1000000)))
            {
                throw new ArgumentException("Trạng thái hoặc giới hạn lượt dùng voucher không hợp lệ.");
            }

            if (_vouchers.Count == 0) GetAllVouchersList();
            var cached = _vouchers.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
            if (cached == null) throw new KeyNotFoundException("Không tìm thấy voucher.");

            if (command.MaxUsageCount != null && command.MaxUsageCount < cached.UsageCount)
                throw new ArgumentException("Giới hạn lượt dùng không được nhỏ hơn số lượt đã sử dụng.");

            dao.Promotions.UpdateVoucher(id, command.Status.Trim(), command.ExpiresAt, command.MaxUsageCount);

            cached.Status = command.Status.Trim();
            cached.ExpiresAt = command.ExpiresAt;
            cached.MaxUsageCount = command.MaxUsageCount;
        }

        public void Delete(string id)
        {
            if (_vouchers.Count == 0) GetAllVouchersList();
            var cached = _vouchers.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
            if (cached == null) throw new KeyNotFoundException("Không tìm thấy voucher.");

            if (dao.Promotions.VoucherHasBeenUsed(id))
                throw new InvalidOperationException("Voucher đã được dùng trong đơn hàng; hãy chuyển trạng thái sang ngừng sử dụng.");

            dao.Promotions.DeleteVoucher(id);
            _vouchers.RemoveAll(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
