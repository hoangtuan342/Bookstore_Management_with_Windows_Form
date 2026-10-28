namespace TrangSach.Customer.Models;

public interface IPaymentOption
{
    string DisplayName { get; }
    string ApiCode { get; }
    string CustomerInstruction { get; }
}

public abstract class CheckoutPaymentOption : IPaymentOption
{
    protected CheckoutPaymentOption(string displayName) => DisplayName = displayName;
    public string DisplayName { get; }
    public abstract string ApiCode { get; }
    public abstract string CustomerInstruction { get; }
    public override string ToString() => DisplayName;
}

public sealed class CashOnDeliveryOption : CheckoutPaymentOption
{
    public CashOnDeliveryOption() : base("Thanh toán khi nhận hàng") { }
    public override string ApiCode => "CashOnDelivery";
    public override string CustomerInstruction => "Thanh toán tiền mặt khi nhận sách.";
}

public sealed class BankTransferOption : CheckoutPaymentOption
{
    public BankTransferOption() : base("Chuyển khoản (mô phỏng)") { }
    public override string ApiCode => "BankTransfer";
    public override string CustomerInstruction => "Đơn hàng sẽ chờ xác nhận thanh toán chuyển khoản.";
}

