using System.ComponentModel.DataAnnotations;
using BookStore.Shared.Models;
using BookStore.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.BUS.Results;
using TrangSach.BUS.Services;

namespace BookStore.Api.Controllers;

[ApiController]
[Authorize(Roles = StaffRoles.ManagerOrMarketing)]
[Route("api/admin/promotions")]
public sealed class AdminPromotionsController(AdminPromotionService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ManagerPromotion>> Get(CancellationToken ct) => service.GetAsync(ct);

    [HttpPost]
    public async Task<IActionResult> Create(CreatePromotionRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(new CreatePromotion(request.Name, request.StartsAt, request.EndsAt, request.BookId,
            request.BookDiscountPercent, request.MinimumOrderAmount, request.OrderDiscountPercent, request.VoucherCode,
            request.VoucherDiscountPercent, request.VoucherMaxUses), ct);
        return result.Status switch
        {
            OperationStatus.Created => StatusCode(StatusCodes.Status201Created, new { Id = result.Value }),
            OperationStatus.Invalid => BadRequest(new { message = result.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, PromotionDetailsRequest request, CancellationToken ct) =>
        ToHttpResult(await service.UpdateAsync(id, new UpdatePromotionDetails(request.Name, request.StartsAt, request.EndsAt), ct));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct) => ToHttpResult(await service.DeleteAsync(id, ct));

    [HttpPut("{id}/rules")]
    public async Task<IActionResult> UpdateRules(string id, PromotionRulesRequest request, CancellationToken ct) =>
        ToHttpResult(await service.UpdateRulesAsync(id, new UpdatePromotionRules(
            request.Books.Select(x => new PromotionBookRule(x.BookId, x.DiscountPercent)).ToArray(),
            request.Orders.Select(x => new PromotionOrderRule(x.MinimumOrderAmount, x.DiscountPercent)).ToArray(),
            request.Vouchers.Select(x => new PromotionVoucherRule(x.VoucherId, x.DiscountPercent)).ToArray()), ct));

    private IActionResult ToHttpResult(OperationResult<Unit> result) => result.Status switch
    {
        OperationStatus.Success => NoContent(),
        OperationStatus.NotFound => NotFound(),
        OperationStatus.Invalid => BadRequest(new { message = result.Message }),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };
}

public sealed class CreatePromotionRequest
{
    [Required, StringLength(180, MinimumLength = 2)] public string Name { get; init; } = string.Empty;
    public DateTime StartsAt { get; init; }
    public DateTime EndsAt { get; init; }
    public string? BookId { get; init; }
    [Range(typeof(decimal), "0", "90")] public decimal? BookDiscountPercent { get; init; }
    [Range(typeof(decimal), "0", "999999999")] public decimal? MinimumOrderAmount { get; init; }
    [Range(typeof(decimal), "0", "90")] public decimal? OrderDiscountPercent { get; init; }
    [StringLength(40)] public string? VoucherCode { get; init; }
    [Range(typeof(decimal), "0", "90")] public decimal? VoucherDiscountPercent { get; init; }
    [Range(1, 1000000)] public int? VoucherMaxUses { get; init; }
}

public sealed class PromotionDetailsRequest
{
    [Required, StringLength(180, MinimumLength = 2)] public string Name { get; init; } = string.Empty;
    public DateTime StartsAt { get; init; }
    public DateTime EndsAt { get; init; }
}

public sealed class PromotionRulesRequest
{
    public IReadOnlyList<PromotionBookRuleRequest> Books { get; init; } = [];
    public IReadOnlyList<PromotionOrderRuleRequest> Orders { get; init; } = [];
    public IReadOnlyList<PromotionVoucherRuleRequest> Vouchers { get; init; } = [];
}
public sealed class PromotionBookRuleRequest { public string BookId { get; init; } = string.Empty; public decimal DiscountPercent { get; init; } }
public sealed class PromotionOrderRuleRequest { public decimal MinimumOrderAmount { get; init; } public decimal DiscountPercent { get; init; } }
public sealed class PromotionVoucherRuleRequest { public string VoucherId { get; init; } = string.Empty; public decimal DiscountPercent { get; init; } }
