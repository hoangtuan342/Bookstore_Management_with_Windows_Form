namespace TrangSach.GUI.Common;

public static class EditDialog
{
    public static Dictionary<string, string>? Show(IWin32Window owner, string title, (string Key, string Label, string Value)[] fields)
    {
        using var dialog = new Form { Text = title, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false, Width = 540, Height = Math.Max(180, fields.Length * 58 + 90) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 2, RowCount = fields.Length + 1, AutoScroll = true };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var controls = new Dictionary<string, TextBox>();
        for (var i = 0; i < fields.Length; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.Controls.Add(new Label { Text = fields[i].Label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, i);
            var input = new TextBox { Text = fields[i].Value, Dock = DockStyle.Fill, UseSystemPasswordChar = fields[i].Key.Contains("password", StringComparison.OrdinalIgnoreCase) };
            controls[fields[i].Key] = input; layout.Controls.Add(input, 1, i);
        }
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        var ok = new Button { Text = "Lưu", DialogResult = DialogResult.OK, Width = 90 };
        var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Width = 90 };
        buttons.Controls.Add(ok); buttons.Controls.Add(cancel); layout.Controls.Add(buttons, 0, fields.Length); layout.SetColumnSpan(buttons, 2);
        dialog.Controls.Add(layout); dialog.AcceptButton = ok; dialog.CancelButton = cancel;
        if (dialog.ShowDialog(owner) != DialogResult.OK) return null;
        return controls.ToDictionary(pair => pair.Key, pair => pair.Value.Text.Trim());
    }
}


