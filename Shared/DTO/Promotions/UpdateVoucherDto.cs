
namespace TrangSach.Shared.DTO
{
    public class UpdateVoucherDto
    {
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public int? MaxUsageCount { get; set; }

        public UpdateVoucherDto()
        {
        }

        public UpdateVoucherDto(string status, DateTime? expiresAt, int? maxUsageCount)
        {
            this.Status = status;
            this.ExpiresAt = expiresAt;
            this.MaxUsageCount = maxUsageCount;
        }
    }
}

