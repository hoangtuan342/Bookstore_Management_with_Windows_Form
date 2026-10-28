namespace TrangSach.Management.Views;

internal sealed record ManagementSearchFilterOption(string? Value, string Caption);

internal static class ManagementSearchFilterControls
{
    public static ComboBox Choice(int width, params ManagementSearchFilterOption[] options)
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = width,
            Font = new Font("Segoe UI", 8.5f),
            Margin = new Padding(2, 3, 5, 0),
            DisplayMember = nameof(ManagementSearchFilterOption.Caption)
        };
        combo.Items.AddRange(options);
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        return combo;
    }

    public static DateTimePicker Date(int width = 105) => new()
    {
        Format = DateTimePickerFormat.Short,
        ShowCheckBox = true,
        Checked = false,
        Width = width,
        Font = new Font("Segoe UI", 8.5f),
        Margin = new Padding(2, 3, 5, 0)
    };

    public static Label Label(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(3, 8, 2, 0),
        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
        ForeColor = Color.FromArgb(72, 88, 104)
    };

    public static string? Value(ComboBox combo) => (combo.SelectedItem as ManagementSearchFilterOption)?.Value;

    public static void SetValues(ComboBox combo, IEnumerable<string?> values, string allCaption, Func<string, string>? caption = null)
    {
        var selected = Value(combo);
        combo.BeginUpdate();
        try
        {
            combo.Items.Clear();
            combo.Items.Add(new ManagementSearchFilterOption(null, allCaption));
            foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(value => value))
                combo.Items.Add(new ManagementSearchFilterOption(value, caption?.Invoke(value!) ?? value!));
            combo.SelectedItem = combo.Items.Cast<ManagementSearchFilterOption>().FirstOrDefault(item => item.Value == selected)
                ?? combo.Items[0];
        }
        finally { combo.EndUpdate(); }
    }

    public static string StatusCaption(string value) => value switch
    {
        "Active" => "Đang hoạt động",
        "Disabled" => "Đã khóa",
        "Inactive" => "Ngừng hoạt động",
        "Posted" => "Đã ghi nhận",
        "Cancelled" => "Đã hủy",
        "Expired" => "Đã hết hạn",
        _ => value
    };
}
