namespace TrangSach.Management.GUI;

internal sealed class AuthorsPageForm : ManagementGridPageForm
{
    public AuthorsPageForm() : base("Tác giả")
    {
        SetSearchPlaceholder("Tìm theo tên tác giả, tiểu sử...");
        AddAction("Tải", "load");
        AddAction("Thêm tác giả", "add");
        AddAction("Sửa", "edit");
        AddAction("Xóa", "delete");
    }
}
