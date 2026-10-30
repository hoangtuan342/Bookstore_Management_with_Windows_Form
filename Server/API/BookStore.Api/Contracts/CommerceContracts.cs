using System.ComponentModel.DataAnnotations;

namespace BookStore.Api.Contracts;

public sealed class SetCartQuantityRequest
{
    [Range(1, 99)]
    public int Quantity { get; init; }
}

public sealed class CheckoutRequest
{
    [Required, StringLength(500, MinimumLength = 8)]
    public string ShippingAddress { get; init; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string ReceiverPhone { get; init; } = string.Empty;

    [Required]
    public string PaymentMethod { get; init; } = "CashOnDelivery";

    [StringLength(40)]
    public string? VoucherCode { get; init; }
}

public sealed record CartLineResponse(
    string BookId,
    string Title,
    string Author,
    decimal UnitPrice,
    int Quantity,
    int AvailableStock,
    decimal LineTotal);

public sealed record OrderLineResponse(
    string BookId,
    string Title,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record OrderResponse(
    string Id,
    DateTime CreatedAt,
    string Status,
    string PaymentStatus,
    string PaymentMethod,
    string ShippingAddress,
    string ReceiverPhone,
    decimal TotalAmount,
    decimal DiscountAmount,
    IReadOnlyList<OrderLineResponse> Lines);

public sealed class BookManagementRequest
{
    [Required, StringLength(250, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Range(typeof(decimal), "1", "999999999")]
    public decimal SalePrice { get; init; }

    [Required, StringLength(30)]
    public string Unit { get; init; } = "cuốn";

    [Required, StringLength(20)]
    public string CategoryId { get; init; } = string.Empty;

    public int? PublisherId { get; init; }

    public IReadOnlyList<int> AuthorIds { get; init; } = [];
}

public sealed class UpdateOrderStatusRequest
{
    [Required]
    public string Status { get; init; } = string.Empty;
}

public sealed class UpdateAdminOrderRequest
{
    [Required, StringLength(500, MinimumLength = 8)]
    public string ShippingAddress { get; init; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string ReceiverPhone { get; init; } = string.Empty;

    [Required]
    public string PaymentMethod { get; init; } = string.Empty;
}

public sealed class AdminCreateOrderRequest
{
    [Required]
    public string CustomerId { get; init; } = string.Empty;

    [Required, StringLength(500, MinimumLength = 8)]
    public string ShippingAddress { get; init; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string ReceiverPhone { get; init; } = string.Empty;

    [Required]
    public string PaymentMethod { get; init; } = "CashOnDelivery";

    [MinLength(1)]
    public IReadOnlyList<AdminCreateOrderLineRequest> Lines { get; init; } = [];
}

public sealed class AdminCreateOrderLineRequest
{
    [Required]
    public string BookId { get; init; } = string.Empty;

    [Range(1, 99)]
    public int Quantity { get; init; }
}

public sealed class PurchaseReceiptRequest
{
    [Required]
    public string SupplierId { get; init; } = string.Empty;

    [MinLength(1)]
    public IReadOnlyList<PurchaseReceiptLineRequest> Lines { get; init; } = [];
}

public sealed class PurchaseReceiptLineRequest
{
    [Required]
    public string BookId { get; init; } = string.Empty;

    [Range(1, 100000)]
    public int Quantity { get; init; }

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal UnitCost { get; init; }
}

public sealed class SupplierRequest
{
    [Required, StringLength(180, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Phone, StringLength(30)]
    public string? PhoneNumber { get; init; }
}

public sealed class BookVisibilityRequest
{
    public bool IsActive { get; init; }
}
