
namespace TrangSach.Shared.DTO
{
    public class VoucherUpdateCommandDto
    {
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public int? MaxUsageCount { get; set; }

        public VoucherUpdateCommandDto()
        {
        }

        public VoucherUpdateCommandDto(string status, DateTime? expiresAt, int? maxUsageCount)
        {
            this.Status = status;
            this.ExpiresAt = expiresAt;
            this.MaxUsageCount = maxUsageCount;
        }
    }
}

