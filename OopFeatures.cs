using BookStore.Shared.Models;
using System.Globalization;
using System.Text;

namespace TrangSach.WinForms;

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
public sealed record BookCsvRow(string Title, decimal SalePrice, string Unit, string CategoryId, int? PublisherId, IReadOnlyList<int> AuthorIds);

public sealed class BookCsvFileService
{
    public void Export(IReadOnlyList<(BookItem Book, ManagerBookDetails Details)> books, string path)
    {
        var rows = new List<string> { "Title;Price;Unit;CategoryId;PublisherId;AuthorIds" };
        rows.AddRange(books.Select(row => string.Join(';',
            Escape(row.Details.Title), row.Details.SalePrice.ToString(CultureInfo.InvariantCulture), Escape(row.Details.Unit),
            Escape(row.Details.CategoryId), row.Details.PublisherId?.ToString(CultureInfo.InvariantCulture) ?? "", string.Join(',', row.Details.AuthorIds))));
        File.WriteAllLines(path, rows, new UTF8Encoding(true));
    }

    public IReadOnlyList<BookCsvRow> ReadForImport(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8).Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));
        var result = new List<BookCsvRow>();
        foreach (var line in lines)
        {
            var fields = line.Split(';');
            if (fields.Length < 6 || !decimal.TryParse(fields[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
                throw new InvalidDataException("Mỗi dòng cần có Title;Price;Unit;CategoryId;PublisherId;AuthorIds và Price phải là số.");
            int? publisherId = int.TryParse(fields[4], out var publisher) ? publisher : null;
            var authorIds = fields[5].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => int.TryParse(value, out var id) ? id : throw new InvalidDataException("AuthorIds phải là danh sách mã số, cách nhau bằng dấu phẩy."))
                .ToArray();
            result.Add(new BookCsvRow(fields[0].Trim(), price, fields[2].Trim(), fields[3].Trim(), publisherId, authorIds));
        }
        return result;
    }

    private static string Escape(string value) => value.Replace(";", ",").Replace("\r", " ").Replace("\n", " ");
}




