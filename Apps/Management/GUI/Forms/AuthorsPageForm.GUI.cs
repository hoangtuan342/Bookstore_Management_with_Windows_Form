namespace TrangSach.Management.GUI;

internal sealed class AuthorsPageForm : ManagementGridPageForm
{
    public AuthorsPageForm() : base("Tác giả")
    {
        SetSearchPlaceholder("Tìm theo tên tác giả, tiểu sử...");
        AddAction("Tải", "load");
        AddAction("Thêm", "add");
        AddAction("Sửa", "edit");
        AddAction("Xóa", "delete");
    }

    protected override void OnGridDataBound()
    {
        base.OnGridDataBound();
        try
        {
            void SetHeader(string propertyName, string headerText)
            {
                if (Grid.Columns[propertyName] is { } col)
                {
                    col.HeaderText = headerText;
                }
            }

            SetHeader(nameof(TrangSach.Shared.DTO.ManagerAuthorDto.Id), "Mã tác giả");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerAuthorDto.Name), "Tên tác giả");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerAuthorDto.Biography), "Tiểu sử");
        }
        catch { }
    }
}
