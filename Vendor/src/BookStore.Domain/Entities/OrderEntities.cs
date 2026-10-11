namespace BookStore.Domain.Entities;

public sealed class PurchaseReceipt
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public Employee Employee { get; set; } = null!;
    public string SupplierId { get; set; } = string.Empty;
    public Supplier Supplier { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Posted";
    public ICollection<PurchaseReceiptLine> Lines { get; set; } = new List<PurchaseReceiptLine>();
}

public sealed class PurchaseReceiptLine
{
    public string ReceiptId { get; set; } = string.Empty;
    public PurchaseReceipt Receipt { get; set; } = null!;
    public string BookId { get; set; } = string.Empty;
    public Book Book { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class SalesOrder
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public Customer Customer { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string OrderStatus { get; set; } = "Pending";
    public string PaymentStatus { get; set; } = "Unpaid";
    public string PaymentMethod { get; set; } = "CashOnDelivery";
    public string ShippingAddress { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string? VoucherId { get; set; }
    public Voucher? Voucher { get; set; }
    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
}

public sealed class SalesOrderLine
{
    public string OrderId { get; set; } = string.Empty;
    public SalesOrder Order { get; set; } = null!;
    public string BookId { get; set; } = string.Empty;
    public Book Book { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class CartItem
{
    public string CustomerId { get; set; } = string.Empty;
    public Customer Customer { get; set; } = null!;
    public string BookId { get; set; } = string.Empty;
    public Book Book { get; set; } = null!;
    public int Quantity { get; set; }
}
