using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;
using TrangSach.Shared.GUI;
using System.Globalization;

namespace TrangSach.Management.GUI;

public partial class ManagementForm
{
    private static readonly List<(string Id, string Name)> AccountStatusOptions = new()
    {
        ("Active", "Hoạt động (Active)"),
        ("Locked", "Đã khóa (Locked)"),
        ("Disabled", "Vô hiệu hóa (Disabled)")
    };

    private void EditCustomer(ManagerCustomerDto? old)
    {
        var fields = new List<EditDialogField>
        {
            new("id", "Mã khách hàng", old?.Id ?? "(Tự động sinh khi để trống)", null, isReadOnly: old != null),
            new("name", "Họ và tên", old?.FullName ?? ""),
            new("phone", "Số điện thoại", old?.PhoneNumber ?? ""),
            new("address", "Địa chỉ", old?.Address ?? ""),
            new("user", "Tên đăng nhập", old?.UserName ?? ""),
            new("email", "Email", old?.Email ?? ""),
            new("password", old is null ? "Mật khẩu ban đầu" : "Mật khẩu mới (để trống nếu không đổi)", ""),
            new("status", "Trạng thái tài khoản", old?.AccountStatus ?? "Active", AccountStatusOptions)
        };

        var values = EditDialog.Show(this, old is null ? "Thêm khách hàng mới" : $"Sửa khách hàng {old.Id}", fields);
        if (values is null) return;

        var idInput = old is null ? EmptyToNull(values["id"]) : old.Id;
        if (idInput != null && idInput.Contains("Tự động", StringComparison.OrdinalIgnoreCase)) idInput = null;

        _services.Manager.SaveCustomer(old?.Id, new SaveCustomerDto(
            idInput,
            values["name"],
            EmptyToNull(values["phone"]),
            EmptyToNull(values["address"]),
            EmptyToNull(values["user"]),
            EmptyToNull(values["email"]),
            EmptyToNull(values["password"]),
            values["status"]));

        LoadCustomers(true);
    }

    private void EditSelectedCustomer()
    {
        var item = Selected<ManagerCustomerDto>(_customersGrid) ?? throw new InvalidOperationException("Chọn khách hàng cần sửa.");
        EditCustomer(item);
    }

    private void DeleteSelectedCustomer()
    {
        var item = Selected<ManagerCustomerDto>(_customersGrid) ?? throw new InvalidOperationException("Chọn khách hàng cần xóa.");
        if (Confirm($"Xóa khách hàng {item.FullName}?")) { _services.Manager.DeleteCustomer(item.Id); LoadCustomers(true); }
    }

    private void EditEmployee(ManagerEmployeeDto? old)
    {
        var roles = _services.Manager.GetEmployeeRoles();
        var roleOptions = roles.Select(r => (r.Name, $"{r.Label} ({r.Name})")).ToList();

        var fields = new List<EditDialogField>
        {
            new("id", "Mã nhân viên", old?.Id ?? "(Tự động sinh khi để trống)", null, isReadOnly: old != null),
            new("last", "Họ đệm", old?.LastName ?? ""),
            new("first", "Tên", old?.FirstName ?? ""),
            new("birth", "Ngày sinh (yyyy-MM-dd)", old?.BirthDate?.ToString("yyyy-MM-dd") ?? ""),
            new("address", "Địa chỉ liên hệ", old?.Address ?? ""),
            new("salary", "Lương cơ bản (VNĐ)", old?.BaseSalary.ToString(CultureInfo.InvariantCulture) ?? "0"),
            new("role", "Chức danh / Vai trò", old?.RoleName ?? StaffRoles.Sales, roleOptions),
            new("status", "Trạng thái nhân sự", old?.AccountStatus ?? "Active", AccountStatusOptions),
            new("user", "Tên đăng nhập hệ thống", old?.UserName ?? ""),
            new("email", "Email", old?.Email ?? ""),
            new("password", old is null ? "Mật khẩu ban đầu (bắt buộc)" : "Mật khẩu mới (để trống nếu không đổi)", ""),
            new("loginStatus", "Trạng thái đăng nhập", old?.LoginStatus ?? "Active", AccountStatusOptions)
        };

        var values = EditDialog.Show(this, old is null ? "Thêm nhân viên mới" : $"Sửa nhân viên {old.Id}", fields);
        if (values is null) return;

        DateOnly? birth = DateOnly.TryParse(values["birth"], out var date) ? date : null;
        if (!decimal.TryParse(values["salary"], NumberStyles.Number, CultureInfo.InvariantCulture, out var salary) || salary < 0)
            throw new InvalidOperationException("Lương cơ bản phải là số không âm.");

        var idInput = old is null ? EmptyToNull(values["id"]) : old.Id;
        if (idInput != null && idInput.Contains("Tự động", StringComparison.OrdinalIgnoreCase)) idInput = null;

        _services.Manager.SaveEmployee(old?.Id, new SaveEmployeeDto(
            idInput,
            values["last"],
            values["first"],
            birth,
            null,
            EmptyToNull(values["address"]),
            values["status"],
            salary,
            EmptyToNull(values["user"]),
            EmptyToNull(values["email"]),
            EmptyToNull(values["password"]),
            values["loginStatus"],
            values["role"]));

        LoadEmployees(true);
    }

    private void EditSelectedEmployee()
    {
        var item = Selected<ManagerEmployeeDto>(_employeesGrid) ?? throw new InvalidOperationException("Chọn nhân viên cần sửa.");
        EditEmployee(item);
    }

    private void DeleteSelectedEmployee()
    {
        var item = Selected<ManagerEmployeeDto>(_employeesGrid) ?? throw new InvalidOperationException("Chọn nhân viên cần xóa.");
        if (Confirm($"Xóa nhân viên {item.LastName} {item.FirstName}?")) { _services.Manager.DeleteEmployee(item.Id); LoadEmployees(true); }
    }

    private void LoadReferences(bool refresh = false)
    {
        _referenceGrid.DataSource = _referenceType.SelectedIndex switch
        {
            0 => (object)_services.GetCategories(refresh).ToList(),
            1 => _services.GetAuthors(refresh).ToList(),
            _ => _services.GetPublishers(refresh).ToList()
        };
    }

    private void SaveReference(bool edit)
    {
        switch (_referenceType.SelectedIndex)
        {
            case 0:
            {
                var old = edit ? Selected<ManagerCategoryDto>(_referenceGrid) : null;
                if (edit && old is null) throw new InvalidOperationException("Chọn danh mục cần sửa.");
                var fields = new List<EditDialogField>
                {
                    new("id", "Mã danh mục (để trống tự sinh)", old?.Id ?? "", null, isReadOnly: edit),
                    new("name", "Tên danh mục", old?.Name ?? "")
                };
                var values = EditDialog.Show(this, edit ? $"Sửa danh mục {old?.Id}" : "Thêm danh mục", fields);
                if (values is null) return;
                _services.Manager.SaveCategory(old?.Id, new SaveCategoryDto(EmptyToNull(values["id"]) ?? "", values["name"]));
                break;
            }
            case 1:
            {
                var old = edit ? Selected<ManagerAuthorDto>(_referenceGrid) : null;
                if (edit && old is null) throw new InvalidOperationException("Chọn tác giả cần sửa.");
                var fields = new List<EditDialogField>
                {
                    new("name", "Tên tác giả", old?.Name ?? ""),
                    new("bio", "Tiểu sử", old?.Biography ?? "")
                };
                var values = EditDialog.Show(this, edit ? $"Sửa tác giả #{old?.Id}" : "Thêm tác giả", fields);
                if (values is null) return;
                _services.Manager.SaveAuthor(old?.Id, new SaveAuthorDto(values["name"], EmptyToNull(values["bio"])));
                break;
            }
            default:
            {
                var old = edit ? Selected<ManagerPublisherDto>(_referenceGrid) : null;
                if (edit && old is null) throw new InvalidOperationException("Chọn nhà xuất bản cần sửa.");
                var fields = new List<EditDialogField>
                {
                    new("name", "Tên nhà xuất bản", old?.Name ?? ""),
                    new("address", "Địa chỉ", old?.Address ?? "")
                };
                var values = EditDialog.Show(this, edit ? $"Sửa nhà xuất bản #{old?.Id}" : "Thêm nhà xuất bản", fields);
                if (values is null) return;
                _services.Manager.SavePublisher(old?.Id, new SavePublisherDto(values["name"], EmptyToNull(values["address"])));
                break;
            }
        }
        _services.Invalidate("categories", "authors", "publishers", "lookups");
        LoadReferences();
    }

    private void DeleteReference()
    {
        switch (_referenceType.SelectedIndex)
        {
            case 0:
                if (Selected<ManagerCategoryDto>(_referenceGrid) is not { } category) throw new InvalidOperationException("Chọn danh mục.");
                if (Confirm($"Xóa danh mục {category.Name}?")) { _services.Manager.DeleteCategory(category.Id); _services.Invalidate("categories", "lookups"); } break;
            case 1:
                if (Selected<ManagerAuthorDto>(_referenceGrid) is not { } author) throw new InvalidOperationException("Chọn tác giả.");
                if (Confirm($"Xóa tác giả {author.Name}?")) { _services.Manager.DeleteAuthor(author.Id); _services.Invalidate("authors", "lookups"); } break;
            default:
                if (Selected<ManagerPublisherDto>(_referenceGrid) is not { } publisher) throw new InvalidOperationException("Chọn nhà xuất bản.");
                if (Confirm($"Xóa nhà xuất bản {publisher.Name}?")) { _services.Manager.DeletePublisher(publisher.Id); _services.Invalidate("publishers", "lookups"); } break;
        }
        LoadReferences();
    }

    private void EditAuthor(ManagerAuthorDto? old)
    {
        var fields = new List<EditDialogField>
        {
            new("name", "Tên tác giả", old?.Name ?? ""),
            new("bio", "Tiểu sử", old?.Biography ?? "")
        };
        var values = EditDialog.Show(this, old != null ? $"Sửa tác giả #{old.Id}" : "Thêm tác giả mới", fields);
        if (values is null) return;
        _services.Manager.SaveAuthor(old?.Id, new SaveAuthorDto(values["name"], EmptyToNull(values["bio"])));
        _services.Invalidate("authors", "lookups");
        LoadAuthors(true);
        if (_referenceGrid.IsHandleCreated) LoadReferences(true);
    }

    private void EditSelectedAuthor()
    {
        var item = Selected<ManagerAuthorDto>(_authorsGrid) ?? throw new InvalidOperationException("Chọn tác giả cần sửa.");
        EditAuthor(item);
    }

    private void DeleteSelectedAuthor()
    {
        var item = Selected<ManagerAuthorDto>(_authorsGrid) ?? throw new InvalidOperationException("Chọn tác giả cần xóa.");
        if (Confirm($"Xóa tác giả {item.Name}?"))
        {
            _services.Manager.DeleteAuthor(item.Id);
            _services.Invalidate("authors", "lookups");
            LoadAuthors(true);
            if (_referenceGrid.IsHandleCreated) LoadReferences(true);
        }
    }

    private void SaveSupplier(SupplierOptionDto? old)
    {
        var fields = new List<EditDialogField>
        {
            new("name", "Tên nhà cung cấp", old?.Name ?? ""),
            new("phone", "Số điện thoại", old?.PhoneNumber ?? "")
        };
        var values = EditDialog.Show(this, old is null ? "Thêm nhà cung cấp mới" : $"Sửa nhà cung cấp {old.Id}", fields);
        if (values is null) return;
        if (old is null) _services.Manager.CreateSupplier(new CreateSupplierDto(values["name"], EmptyToNull(values["phone"])));
        else _services.Manager.UpdateSupplier(old.Id, new CreateSupplierDto(values["name"], EmptyToNull(values["phone"])));
        LoadSuppliers(true);
    }

    private void EditSelectedSupplier() => SaveSupplier(Selected<SupplierOptionDto>(_suppliersGrid) ?? throw new InvalidOperationException("Chọn nhà cung cấp."));

    private void DeleteSelectedSupplier()
    {
        var item = Selected<SupplierOptionDto>(_suppliersGrid) ?? throw new InvalidOperationException("Chọn nhà cung cấp.");
        if (Confirm($"Xóa nhà cung cấp {item.Name}?")) { _services.Manager.DeleteSupplier(item.Id); LoadSuppliers(true); }
    }
}
