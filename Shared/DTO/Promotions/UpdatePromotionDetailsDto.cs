
namespace TrangSach.Shared.DTO
{
    public class UpdatePromotionDetailsDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }

        public UpdatePromotionDetailsDto()
        {
        }

        public UpdatePromotionDetailsDto(string name, DateTime startsAt, DateTime endsAt)
        {
            this.Name = name;
            this.StartsAt = startsAt;
            this.EndsAt = endsAt;
        }
    }
}

