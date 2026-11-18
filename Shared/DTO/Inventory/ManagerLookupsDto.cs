
namespace TrangSach.Shared.DTO
{
    public class ManagerLookupsDto
    {
        public List<CategoryOptionDto> Categories { get; set; } = new List<CategoryOptionDto>();
        public List<IdNameOptionDto> Authors { get; set; } = new List<IdNameOptionDto>();
        public List<IdNameOptionDto> Publishers { get; set; } = new List<IdNameOptionDto>();
        public List<SupplierOptionDto> Suppliers { get; set; } = new List<SupplierOptionDto>();

        public ManagerLookupsDto()
        {
        }

        public ManagerLookupsDto(List<CategoryOptionDto> categories, List<IdNameOptionDto> authors, List<IdNameOptionDto> publishers, List<SupplierOptionDto> suppliers)
        {
            this.Categories = categories;
            this.Authors = authors;
            this.Publishers = publishers;
            this.Suppliers = suppliers;
        }
    }
}

