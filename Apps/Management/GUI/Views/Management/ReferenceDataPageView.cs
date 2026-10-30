namespace TrangSach.Management.Views;

internal sealed class ReferenceDataPageView : ManagementGridPageView
{
    public ComboBox ReferenceType { get; } = new();

    public ReferenceDataPageView() : base("Danh mục tham chiếu")
    {
        ReferenceType.DropDownStyle = ComboBoxStyle.DropDownList;
        ReferenceType.Items.AddRange(["Danh mục", "Tác giả", "Nhà xuất bản"]);
        ReferenceType.SelectedIndex = 0;
        ReferenceType.Width = 150;
        AddToolbarControl(ReferenceType);
        AddAction("Tải", "load");
        AddAction("Thêm", "add");
        AddAction("Sửa", "edit");
        AddAction("Xóa", "delete");
    }
}

