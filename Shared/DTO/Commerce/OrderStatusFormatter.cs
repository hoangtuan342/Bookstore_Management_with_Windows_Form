namespace TrangSach.Shared.DTO;

public static class OrderStatusFormatter
{
    public static string FormatOrderStatus(string? status) => status?.Trim() switch
    {
        "Submitted" => "Mới tạo",
        "Pending" => "Chờ xác nhận",
        "Confirmed" => "Đã xác nhận",
        "Shipping" => "Đang giao hàng",
        "Completed" => "Đã hoàn thành",
        "Cancelled" => "Đã hủy",
        _ => string.IsNullOrWhiteSpace(status) ? string.Empty : status
    };

    public static string FormatPaymentStatus(string? status) => status?.Trim() switch
    {
        "Pending" => "Chưa thanh toán",
        "Unpaid" => "Chưa thanh toán",
        "AwaitingPayment" => "Chờ thanh toán",
        "Paid" => "Đã thanh toán",
        "Refunded" => "Đã hoàn tiền",
        "Failed" => "Thất bại",
        _ => string.IsNullOrWhiteSpace(status) ? string.Empty : status
    };

    public static string FormatPaymentMethod(string? method) => method?.Trim() switch
    {
        "CashOnDelivery" or "COD" => "Khi nhận hàng (COD)",
        "BankTransfer" => "Chuyển khoản",
        _ => string.IsNullOrWhiteSpace(method) ? string.Empty : method
    };

    public static string FormatReceiptStatus(string? status) => status?.Trim() switch
    {
        "Posted" => "Đã ghi nhận",
        "Completed" => "Đã hoàn thành",
        "Cancelled" => "Đã hủy",
        "Reversed" => "Đã đảo phiếu",
        "Pending" => "Chờ xử lý",
        _ => string.IsNullOrWhiteSpace(status) ? string.Empty : status
    };
}
