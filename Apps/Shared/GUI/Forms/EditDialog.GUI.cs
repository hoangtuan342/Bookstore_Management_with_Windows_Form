namespace TrangSach.Shared.GUI;

/// <summary>Small reusable WinForms dialog for editing a few labeled values.</summary>
public sealed class EditDialog : Form
{
    private readonly Dictionary<string, TextBox> _inputs = new(StringComparer.Ordinal);

    private EditDialog(string title, List<(string Key, string Label, string Value)> fields)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Width = 540;
        Height = Math.Max(180, fields.Count * 58 + 90);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 2,
            RowCount = fields.Count + 1,
            AutoScroll = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        for (var index = 0; index < fields.Count; index++)
        {
            var isLongText = fields[index].Key.Equals("description", StringComparison.OrdinalIgnoreCase);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, isLongText ? 92 : 38));
            layout.Controls.Add(new Label { Text = fields[index].Label, AutoSize = true, Anchor = isLongText ? AnchorStyles.Top | AnchorStyles.Left : AnchorStyles.Left }, 0, index);
            var input = new TextBox
            {
                Text = fields[index].Value,
                Dock = DockStyle.Fill,
                Multiline = isLongText,
                ScrollBars = isLongText ? ScrollBars.Vertical : ScrollBars.None,
                UseSystemPasswordChar = fields[index].Key.Contains("password", StringComparison.OrdinalIgnoreCase)
            };
            _inputs[fields[index].Key] = input;
            layout.Controls.Add(input, 1, index);
        }

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        var accept = new Button { Text = "Lưu", DialogResult = DialogResult.OK, Width = 90 };
        var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Width = 90 };
        buttons.Controls.Add(accept);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 0, fields.Count);
        layout.SetColumnSpan(buttons, 2);
        Controls.Add(layout);
        AcceptButton = accept;
        CancelButton = cancel;
    }

    public static Dictionary<string, string>? Show(
        IWin32Window owner,
        string title,
        params (string Key, string Label, string Value)[] fields)
    {
        using var dialog = new EditDialog(title, fields.ToList());
        if (dialog.ShowDialog(owner) != DialogResult.OK) return null;
        return dialog._inputs.ToDictionary(input => input.Key, input => input.Value.Text.Trim(), StringComparer.Ordinal);
    }
}


