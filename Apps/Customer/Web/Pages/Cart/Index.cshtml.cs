using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Customer.BUS;
using TrangSach.Shared.DTO;

namespace TrangSach.Customer.Web.Pages.Cart;

public sealed class IndexModel(CustomerWebApiClient api) : CustomerPageModel
{
    public List<StoreCartLineDto> Lines { get; private set; } = [];
    public decimal Subtotal => Lines.Sum(line => line.LineTotal);

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
        ViewData["Title"] = "Giỏ hàng";
    }

    public async Task<IActionResult> OnPostUpdateAsync(string bookId, int quantity, CancellationToken ct)
    {
        try
        {
            var token = await EnsureCustomerTokenAsync(api, ct);
            await api.SetCartQuantityAsync(token, bookId, quantity, ct);
            TempData["Notice"] = "Đã cập nhật số lượng.";
        }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(string bookId, CancellationToken ct)
    {
        try
        {
            var token = await EnsureCustomerTokenAsync(api, ct);
            await api.RemoveCartItemAsync(token, bookId, ct);
            TempData["Notice"] = "Đã xóa sách khỏi giỏ.";
        }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            var token = await EnsureCustomerTokenAsync(api, ct);
            Lines = await api.GetCartAsync(token, ct);
        }
        catch { Lines = []; }
    }
}
