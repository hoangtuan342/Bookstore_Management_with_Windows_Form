namespace BookStore.Domain.Entities;

public sealed class Promotion
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public ICollection<BookPromotion> Books { get; set; } = new List<BookPromotion>();
    public ICollection<OrderPromotion> OrderDiscounts { get; set; } = new List<OrderPromotion>();
    public ICollection<PromotionVoucher> Vouchers { get; set; } = new List<PromotionVoucher>();
}

public sealed class BookPromotion
{
    public string PromotionId { get; set; } = string.Empty;
    public Promotion Promotion { get; set; } = null!;
    public string BookId { get; set; } = string.Empty;
    public Book Book { get; set; } = null!;
    public decimal DiscountPercent { get; set; }
}

public sealed class OrderPromotion
{
    public string PromotionId { get; set; } = string.Empty;
    public Promotion Promotion { get; set; } = null!;
    public decimal MinimumOrderAmount { get; set; }
    public decimal DiscountPercent { get; set; }
}

public sealed class Voucher
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime? ExpiresAt { get; set; }
    public int? MaxUsageCount { get; set; }
    public int UsageCount { get; set; }
    public ICollection<PromotionVoucher> Promotions { get; set; } = new List<PromotionVoucher>();
    public ICollection<SalesOrder> Orders { get; set; } = new List<SalesOrder>();
}

public sealed class PromotionVoucher
{
    public string PromotionId { get; set; } = string.Empty;
    public Promotion Promotion { get; set; } = null!;
    public string VoucherId { get; set; } = string.Empty;
    public Voucher Voucher { get; set; } = null!;
    public decimal DiscountPercent { get; set; }
}
