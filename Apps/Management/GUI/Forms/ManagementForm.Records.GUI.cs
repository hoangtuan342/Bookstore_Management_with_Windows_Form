using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;
using System.Globalization;

namespace TrangSach.Management.GUI;

public partial class ManagementForm
{
    private async Task EditCustomerAsync(ManagerCustomerDto? old)
    {
        var values = EditDialog.Show(this, old is null ? "Thêm khách hàng" : "Sửa khách hàng", [
            ("id", "Mã khách (trống để tự sinh)", old?.Id ?? ""), ("name", "Họ tên", old?.FullName ?? ""),
            ("phone", "Điện thoại", old?.PhoneNumber ?? ""), ("address", "Địa chỉ", old?.Address ?? ""),
            ("user", "Tên đăng nhập (tạo/cập nhật tài khoản)", old?.UserName ?? ""), ("email", "Email", old?.Email ?? ""),
            ("password", "Mật khẩu mới (bỏ trống nếu không đổi)", ""), ("status", "Trạng thái tài khoản", old?.AccountStatus ?? "Active")]);
        if (values is null) return;
        await _services.Manager.SaveCustomerAsync(old?.Id, new SaveCustomerDto(EmptyToNull(values["id"]), values["name"], EmptyToNull(values["phone"]), EmptyToNull(values["address"]), EmptyToNull(values["user"]), EmptyToNull(values["email"]), EmptyToNull(values["password"]), values["status"]));
        await LoadCustomersAsync(true);
    }

    private Task EditSelectedCustomerAsync()
    {
        var item = Selected<ManagerCustomerDto>(_customersGrid) ?? throw new InvalidOperationException("Chọn khách hàng cần sửa.");
        return EditCustomerAsync(item);
    }

    private async Task DeleteSelectedCustomerAsync()
    {
        var item = Selected<ManagerCustomerDto>(_customersGrid) ?? throw new InvalidOperationException("Chọn khách hàng cần xóa.");
        if (Confirm($"Xóa khách hàng {item.FullName}?")) { await _services.Manager.DeleteCustomerAsync(item.Id); await LoadCustomersAsync(true); }
    }

    private async Task EditEmployeeAsync(ManagerEmployeeDto? old)
    {
        var roles = await _services.Manager.GetEmployeeRolesAsync();
        var values = EditDialog.Show(this, old is null ? "Thêm nhân viên" : "Sửa nhân viên", [
            ("id", "Mã nhân viên (trống để tự sinh)", old?.Id ?? ""), ("last", "Họ", old?.LastName ?? ""), ("first", "Tên", old?.FirstName ?? ""),
            ("birth", "Ngày sinh (yyyy-MM-dd)", old?.BirthDate?.ToString("yyyy-MM-dd") ?? ""), ("address", "Địa chỉ", old?.Address ?? ""),
            ("salary", "Lương cơ bản", old?.BaseSalary.ToString(CultureInfo.InvariantCulture) ?? "0"), ("status", "Trạng thái nhân viên", old?.AccountStatus ?? "Active"),
            ("user", "Tên đăng nhập", old?.UserName ?? ""), ("email", "Email", old?.Email ?? ""), ("password", "Mật khẩu (bắt buộc khi tạo tài khoản)", ""),
            ("role", "Chức danh (" + string.Join(", ", roles.Select(x => x.Name)) + ")", old?.RoleName ?? "Sales"), ("loginStatus", "Trạng thái tài khoản", old?.LoginStatus ?? "Active")]);
        if (values is null) return;
        DateOnly? birth = DateOnly.TryParse(values["birth"], out var date) ? date : null;
        if (!decimal.TryParse(values["salary"], NumberStyles.Number, CultureInfo.InvariantCulture, out var salary)) throw new InvalidOperationException("Lương phải là số.");
        await _services.Manager.SaveEmployeeAsync(old?.Id, new SaveEmployeeDto(EmptyToNull(values["id"]), values["last"], values["first"], birth, null, EmptyToNull(values["address"]), values["status"], salary, EmptyToNull(values["user"]), EmptyToNull(values["email"]), EmptyToNull(values["password"]), values["loginStatus"], values["role"]));
        await LoadEmployeesAsync(true);
    }

    private Task EditSelectedEmployeeAsync()
    {
        var item = Selected<ManagerEmployeeDto>(_employeesGrid) ?? throw new InvalidOperationException("Chọn nhân viên cần sửa.");
        return EditEmployeeAsync(item);
    }

    private async Task DeleteSelectedEmployeeAsync()
    {
        var item = Selected<ManagerEmployeeDto>(_employeesGrid) ?? throw new InvalidOperationException("Chọn nhân viên cần xóa.");
        if (Confirm($"Xóa nhân viên {item.LastName} {item.FirstName}?")) { await _services.Manager.DeleteEmployeeAsync(item.Id); await LoadEmployeesAsync(true); }
    }

    private async Task LoadReferencesAsync(bool refresh = false)
    {
        _referenceGrid.DataSource = _referenceType.SelectedIndex switch
        {
            0 => (object)(await _services.GetCategoriesAsync(refresh)).ToList(),
            1 => (await _services.GetAuthorsAsync(refresh)).ToList(),
            _ => (await _services.GetPublishersAsync(refresh)).ToList()
        };
    }

    private async Task SaveReferenceAsync(bool edit)
    {
        switch (_referenceType.SelectedIndex)
        {
            case 0:
            {
                var old = edit ? Selected<ManagerCategoryDto>(_referenceGrid) : null;
                if (edit && old is null) throw new InvalidOperationException("Chọn danh mục cần sửa.");
                var values = EditDialog.Show(this, "Danh mục", [("id", "Mã danh mục", old?.Id ?? ""), ("name", "Tên danh mục", old?.Name ?? "")]);
                if (values is null) return;
                await _services.Manager.SaveCategoryAsync(old?.Id, new SaveCategoryDto(values["id"], values["name"])); break;
            }
            case 1:
            {
                var old = edit ? Selected<ManagerAuthorDto>(_referenceGrid) : null;
                if (edit && old is null) throw new InvalidOperationException("Chọn tác giả cần sửa.");
                var values = EditDialog.Show(this, "Tác giả", [("name", "Tên tác giả", old?.Name ?? ""), ("bio", "Tiểu sử", old?.Biography ?? "")]);
                if (values is null) return;
                await _services.Manager.SaveAuthorAsync(old?.Id, new SaveAuthorDto(values["name"], EmptyToNull(values["bio"]))); break;
            }
            default:
            {
                var old = edit ? Selected<ManagerPublisherDto>(_referenceGrid) : null;
                if (edit && old is null) throw new InvalidOperationException("Chọn nhà xuất bản cần sửa.");
                var values = EditDialog.Show(this, "Nhà xuất bản", [("name", "Tên nhà xuất bản", old?.Name ?? ""), ("address", "Địa chỉ", old?.Address ?? "")]);
                if (values is null) return;
                await _services.Manager.SavePublisherAsync(old?.Id, new SavePublisherDto(values["name"], EmptyToNull(values["address"]))); break;
            }
        }
        _services.Invalidate("categories", "authors", "publishers", "lookups");
        await LoadReferencesAsync();
    }

    private async Task DeleteReferenceAsync()
    {
        switch (_referenceType.SelectedIndex)
        {
            case 0:
                if (Selected<ManagerCategoryDto>(_referenceGrid) is not { } category) throw new InvalidOperationException("Chọn danh mục.");
                if (Confirm($"Xóa danh mục {category.Name}?")) { await _services.Manager.DeleteCategoryAsync(category.Id); _services.Invalidate("categories", "lookups"); } break;
            case 1:
                if (Selected<ManagerAuthorDto>(_referenceGrid) is not { } author) throw new InvalidOperationException("Chọn tác giả.");
                if (Confirm($"Xóa tác giả {author.Name}?")) { await _services.Manager.DeleteAuthorAsync(author.Id); _services.Invalidate("authors", "lookups"); } break;
            default:
                if (Selected<ManagerPublisherDto>(_referenceGrid) is not { } publisher) throw new InvalidOperationException("Chọn nhà xuất bản.");
                if (Confirm($"Xóa nhà xuất bản {publisher.Name}?")) { await _services.Manager.DeletePublisherAsync(publisher.Id); _services.Invalidate("publishers", "lookups"); } break;
        }
        await LoadReferencesAsync();
    }

    private async Task SaveSupplierAsync(SupplierOptionDto? old)
    {
        var values = EditDialog.Show(this, old is null ? "Thêm nhà cung cấp" : "Sửa nhà cung cấp", [("name", "Tên", old?.Name ?? ""), ("phone", "Điện thoại", old?.PhoneNumber ?? "")]);
        if (values is null) return;
        if (old is null) await _services.Manager.CreateSupplierAsync(new CreateSupplierDto(values["name"], EmptyToNull(values["phone"])));
        else await _services.Manager.UpdateSupplierAsync(old.Id, new CreateSupplierDto(values["name"], EmptyToNull(values["phone"])));
        await LoadSuppliersAsync(true);
    }

    private Task EditSelectedSupplierAsync() => SaveSupplierAsync(Selected<SupplierOptionDto>(_suppliersGrid) ?? throw new InvalidOperationException("Chọn nhà cung cấp."));

    private async Task DeleteSelectedSupplierAsync()
    {
        var item = Selected<SupplierOptionDto>(_suppliersGrid) ?? throw new InvalidOperationException("Chọn nhà cung cấp.");
        if (Confirm($"Xóa nhà cung cấp {item.Name}?")) { await _services.Manager.DeleteSupplierAsync(item.Id); await LoadSuppliersAsync(true); }
    }
}




