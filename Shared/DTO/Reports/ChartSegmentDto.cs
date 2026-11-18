
namespace TrangSach.Shared.DTO
{
    public class ChartSegmentDto
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Color { get; set; } = string.Empty;

        public ChartSegmentDto()
        {
        }

        public ChartSegmentDto(string label, int value, string color)
        {
            this.Label = label;
            this.Value = value;
            this.Color = color;
        }
    }
}

