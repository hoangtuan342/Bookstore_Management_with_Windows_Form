using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Management.Web.BUS;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages.Admin.References;

[Authorize(Roles = StaffRoles.ManagerOrCatalog)]
public sealed class IndexModel(ManagementWebApiClient api) : AdminPageModel
{
    public List<ManagerCategoryDto> Categories { get; private set; } = [];
    public List<ManagerAuthorDto> Authors { get; private set; } = [];
    public List<ManagerPublisherDto> Publishers { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken ct) { await LoadAsync(ct); ViewData["Title"] = "Danh mục tham chiếu"; }

    public async Task<IActionResult> OnPostCategoryAsync(string? id, string name, CancellationToken ct)
    {
        try
        {
            var categoryId = Request.Form["categoryId"].ToString();
            await api.SaveCategoryAsync(Token, string.IsNullOrWhiteSpace(id) ? null : id, new SaveCategoryDto(categoryId, name), ct);
            TempData["Notice"] = "Đã lưu thể loại.";
        }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostDeleteCategoryAsync(string id, CancellationToken ct) { try { await api.DeleteCategoryAsync(Token, id, ct); TempData["Notice"] = "Đã xóa thể loại."; } catch (Exception ex) { TempData["Notice"] = ex.Message; } return RedirectToPage(); }
    public async Task<IActionResult> OnPostAuthorAsync(int? id, string name, string? biography, CancellationToken ct) { try { await api.SaveAuthorAsync(Token, id, new SaveAuthorDto(name, biography), ct); TempData["Notice"] = "Đã lưu tác giả."; } catch (Exception ex) { TempData["Notice"] = ex.Message; } return RedirectToPage(); }
    public async Task<IActionResult> OnPostDeleteAuthorAsync(int id, CancellationToken ct) { try { await api.DeleteAuthorAsync(Token, id, ct); TempData["Notice"] = "Đã xóa tác giả."; } catch (Exception ex) { TempData["Notice"] = ex.Message; } return RedirectToPage(); }
    public async Task<IActionResult> OnPostPublisherAsync(int? id, string name, string? address, CancellationToken ct) { try { await api.SavePublisherAsync(Token, id, new SavePublisherDto(name, address), ct); TempData["Notice"] = "Đã lưu nhà xuất bản."; } catch (Exception ex) { TempData["Notice"] = ex.Message; } return RedirectToPage(); }
    public async Task<IActionResult> OnPostDeletePublisherAsync(int id, CancellationToken ct) { try { await api.DeletePublisherAsync(Token, id, ct); TempData["Notice"] = "Đã xóa nhà xuất bản."; } catch (Exception ex) { TempData["Notice"] = ex.Message; } return RedirectToPage(); }

    private async Task LoadAsync(CancellationToken ct)
    {
        Categories = await api.GetCategoriesAsync(Token, ct);
        Authors = await api.GetAuthorsAsync(Token, ct);
        Publishers = await api.GetPublishersAsync(Token, ct);
    }
}
