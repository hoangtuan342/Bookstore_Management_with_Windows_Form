namespace TrangSach.GUI.Common;

public static class MoneyFormatter
{
    public static string ToVnd(decimal amount) => amount.ToString("N0") + " ₫";
}
