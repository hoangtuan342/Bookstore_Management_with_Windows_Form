using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace TrangSach.Management.Views;

/// <summary>Small, safe in-memory search language for management lists.</summary>
internal static class ManagementSearchExpression
{
    private static readonly Regex TokenPattern = new("\\\"(?:\\\\.|[^\\\"])*\\\"|>=|<=|!=|[()=<>]|[^\\s()=<>!]+", RegexOptions.Compiled);
    private static readonly Dictionary<string, string[]> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tên sách"] = ["Title"], ["khách hàng"] = ["CustomerName"], ["nhân viên"] = ["EmployeeName"],
        ["nhà cung cấp"] = ["SupplierName"], ["mã đơn"] = ["Id"], ["mã sách"] = ["BookId", "Id"],
        ["giá"] = ["SalePrice", "UnitCost", "TotalAmount", "LineTotal", "Revenue"],
        ["giá bán"] = ["SalePrice", "UnitPrice"], ["giá nhập"] = ["UnitCost"],
        ["tồn kho"] = ["StockQuantity", "AvailableStock"], ["số lượng"] = ["Quantity", "ItemCount"],
        ["tổng tiền"] = ["TotalAmount", "Revenue"], ["doanh thu"] = ["Revenue", "TotalAmount"],
        ["ngày"] = ["CreatedAt", "Date", "OccurredAt"], ["ngày tạo"] = ["CreatedAt"],
        ["trạng thái"] = ["Status", "OrderStatus", "AccountStatus"], ["mã"] = ["Id", "BookId", "CustomerId"]
    };

    public static bool TryCreate(string query, object? sample, out Func<object, bool>? predicate, out string? error)
    {
        predicate = null;
        error = null;
        if (!IsExpression(query)) return false;
        var tokens = TokenPattern.Matches(query).Select(match => match.Value).ToArray();
        if (tokens.Length == 0) return false;
        try
        {
            var parser = new Parser(tokens, sample);
            predicate = parser.Parse();
            return true;
        }
        catch (FormatException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    public static bool IsExpression(string query) => TokenPattern.Matches(query).Any(match =>
        match.Value is "=" or "!=" or ">" or "<" or ">=" or "<=" or "(" or ")"
        || match.Value.Equals("AND", StringComparison.OrdinalIgnoreCase)
        || match.Value.Equals("OR", StringComparison.OrdinalIgnoreCase)
        || match.Value.Equals("NOT", StringComparison.OrdinalIgnoreCase));

    private sealed class Parser(string[] tokens, object? sample)
    {
        private int _index;

        public Func<object, bool> Parse()
        {
            var result = ParseOr();
            if (_index != tokens.Length) throw InvalidSyntax();
            return result;
        }

        private Func<object, bool> ParseOr()
        {
            var left = ParseAnd();
            while (TakeKeyword("OR"))
            {
                var first = left;
                var right = ParseAnd();
                left = item => first(item) || right(item);
            }
            return left;
        }

        private Func<object, bool> ParseAnd()
        {
            var left = ParseUnary();
            while (TakeKeyword("AND"))
            {
                var first = left;
                var right = ParseUnary();
                left = item => first(item) && right(item);
            }
            return left;
        }

        private Func<object, bool> ParseUnary()
        {
            if (TakeKeyword("NOT"))
            {
                var inner = ParseUnary();
                return item => !inner(item);
            }
            if (Take("("))
            {
                var inner = ParseOr();
                if (!Take(")")) throw InvalidSyntax();
                return inner;
            }

            var operatorIndex = _index;
            while (operatorIndex < tokens.Length
                && tokens[operatorIndex] is not ("=" or "!=" or ">" or "<" or ">=" or "<="))
            {
                if (tokens[operatorIndex] == ")"
                    || tokens[operatorIndex].Equals("AND", StringComparison.OrdinalIgnoreCase)
                    || tokens[operatorIndex].Equals("OR", StringComparison.OrdinalIgnoreCase))
                    throw InvalidSyntax();
                operatorIndex++;
            }
            if (operatorIndex == _index || operatorIndex >= tokens.Length) throw InvalidSyntax();
            var field = string.Join(' ', tokens[_index..operatorIndex].Select(Unquote));
            if (sample is not null && !HasProperty(sample, field))
                throw new FormatException($"Không tìm thấy trường '{field}' trên danh sách hiện tại.");
            var operation = tokens[operatorIndex];
            _index = operatorIndex + 1;
            var valueStart = _index;
            while (_index < tokens.Length && tokens[_index] != ")"
                && !tokens[_index].Equals("AND", StringComparison.OrdinalIgnoreCase)
                && !tokens[_index].Equals("OR", StringComparison.OrdinalIgnoreCase))
                _index++;
            if (valueStart == _index) throw InvalidSyntax();
            var expected = string.Join(' ', tokens[valueStart.._index].Select(Unquote));
            return item => Compare(Resolve(item, field), operation, expected);
        }

        private bool Take(string token)
        {
            if (_index >= tokens.Length || tokens[_index] != token) return false;
            _index++;
            return true;
        }

        private bool TakeKeyword(string keyword)
        {
            if (_index >= tokens.Length || !tokens[_index].Equals(keyword, StringComparison.OrdinalIgnoreCase)) return false;
            _index++;
            return true;
        }

        private static FormatException InvalidSyntax() => new("Cú pháp tìm kiếm chưa đúng. Ví dụ: Giá >= 100000 AND Tồn kho < 5");
    }

    private static object? Resolve(object item, string field)
    {
        return FindProperty(item, field)?.GetValue(item);
    }

    private static bool HasProperty(object item, string field) => FindProperty(item, field) is not null;

    private static PropertyInfo? FindProperty(object item, string field)
    {
        var key = Normalize(Unquote(field));
        var properties = item.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var names = new List<string> { Unquote(field) };
        foreach (var (alias, candidates) in Aliases)
            if (Normalize(alias) == key) names.AddRange(candidates);
        return properties.FirstOrDefault(candidate => names.Any(name =>
            Normalize(candidate.Name) == Normalize(name)));
    }

    private static bool Compare(object? actual, string operation, string expected)
    {
        if (actual is null) return operation == "!=" && expected.Length > 0;
        if (actual is DateTime date && DateTime.TryParse(expected, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var expectedDate))
            return CompareValues(date, expectedDate, operation);
        if (actual is DateOnly dateOnly && DateOnly.TryParse(expected, out var expectedDateOnly))
            return CompareValues(dateOnly, expectedDateOnly, operation);
        if (actual is IConvertible && decimal.TryParse(Convert.ToString(actual, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out var actualNumber)
            && decimal.TryParse(expected, NumberStyles.Number, CultureInfo.CurrentCulture, out var expectedNumber))
            return CompareValues(actualNumber, expectedNumber, operation);

        var actualText = Convert.ToString(actual, CultureInfo.CurrentCulture) ?? string.Empty;
        var comparison = StringComparer.CurrentCultureIgnoreCase.Compare(actualText, expected);
        return operation switch
        {
            "=" => comparison == 0,
            "!=" => comparison != 0,
            ">" => comparison > 0,
            "<" => comparison < 0,
            ">=" => comparison >= 0,
            "<=" => comparison <= 0,
            _ => false
        };
    }

    private static bool CompareValues<T>(T actual, T expected, string operation) where T : IComparable<T>
    {
        var comparison = actual.CompareTo(expected);
        return operation switch
        {
            "=" => comparison == 0,
            "!=" => comparison != 0,
            ">" => comparison > 0,
            "<" => comparison < 0,
            ">=" => comparison >= 0,
            "<=" => comparison <= 0,
            _ => false
        };
    }

    private static string Unquote(string value) => value.Length >= 2 && value[0] == '"' && value[^1] == '"'
        ? Regex.Unescape(value[1..^1]) : value;

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        return new string(decomposed.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark).ToArray())
            .Replace("đ", "d", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
    }
}
