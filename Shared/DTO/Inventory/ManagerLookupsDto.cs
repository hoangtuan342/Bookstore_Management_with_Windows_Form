
namespace TrangSach.Shared.DTO
{
    public class ManagerLookupsDto
    {
        public IReadOnlyList<CategoryOptionDto> Categories { get; set; } = new List<CategoryOptionDto>();
        public IReadOnlyList<IdNameOptionDto> Authors { get; set; } = new List<IdNameOptionDto>();
        public IReadOnlyList<IdNameOptionDto> Publishers { get; set; } = new List<IdNameOptionDto>();
        public IReadOnlyList<SupplierOptionDto> Suppliers { get; set; } = new List<SupplierOptionDto>();

        public ManagerLookupsDto()
        {
        }

        public ManagerLookupsDto(IReadOnlyList<CategoryOptionDto> categories, IReadOnlyList<IdNameOptionDto> authors, IReadOnlyList<IdNameOptionDto> publishers, IReadOnlyList<SupplierOptionDto> suppliers)
        {
            this.Categories = categories;
            this.Authors = authors;
            this.Publishers = publishers;
            this.Suppliers = suppliers;
        }
    }
}

