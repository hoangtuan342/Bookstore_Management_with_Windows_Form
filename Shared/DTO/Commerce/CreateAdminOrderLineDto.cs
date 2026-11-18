
namespace TrangSach.Shared.DTO
{
    public class CreateAdminOrderLineDto
    {
        public string BookId { get; set; } = string.Empty;
        public int Quantity { get; set; }

        public CreateAdminOrderLineDto()
        {
        }

        public CreateAdminOrderLineDto(string bookId, int quantity)
        {
            this.BookId = bookId;
            this.Quantity = quantity;
        }
    }
}

