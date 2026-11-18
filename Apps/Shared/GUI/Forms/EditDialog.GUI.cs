using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TrangSach.Shared.GUI;

public sealed class EditDialogField
{
    public string Key { get; }
    public string Label { get; }
    public string Value { get; }
    public List<(string Id, string Name)>? Options { get; }
    public bool IsReadOnly { get; }

    public EditDialogField(string key, string label, string value, List<(string Id, string Name)>? options = null, bool isReadOnly = false)
    {
        Key = key;
        Label = label;
        Value = value ?? string.Empty;
        Options = options;
        IsReadOnly = isReadOnly;
    }
}

/// <summary>Reusable WinForms dialog for editing labeled values, supporting text inputs and select dropdowns.</summary>
public sealed class EditDialog : Form
{
    private readonly Dictionary<string, Control> _controls = new(StringComparer.Ordinal);

    private EditDialog(string title, List<EditDialogField> fields)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Width = 580;
        Height = Math.Min(680, Math.Max(200, fields.Count * 56 + 110));

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = fields.Count + 1,
            AutoScroll = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        for (var index = 0; index < fields.Count; index++)
        {
            var field = fields[index];
            var isLongText = field.Key.Equals("description", StringComparison.OrdinalIgnoreCase) || field.Key.Equals("address", StringComparison.OrdinalIgnoreCase);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, isLongText ? 90 : 42));

            var lbl = new Label
            {
                Text = field.Label,
                AutoSize = true,
                Anchor = isLongText ? AnchorStyles.Top | AnchorStyles.Left : AnchorStyles.Left
            };
            layout.Controls.Add(lbl, 0, index);

            if (field.Options != null && field.Options.Count > 0)
            {
                var combo = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                combo.DisplayMember = "Name";
                combo.ValueMember = "Id";

                var bindingList = field.Options.Select(o => new ComboBoxOption(o.Id, o.Name)).ToList();
                combo.DataSource = bindingList;

                if (!string.IsNullOrEmpty(field.Value))
                {
                    var selected = bindingList.FirstOrDefault(b => string.Equals(b.Id, field.Value, StringComparison.OrdinalIgnoreCase));
                    if (selected != null) combo.SelectedItem = selected;
                }

                _controls[field.Key] = combo;
                layout.Controls.Add(combo, 1, index);
            }
            else
            {
                var input = new TextBox
                {
                    Text = field.Value,
                    Dock = DockStyle.Fill,
                    Multiline = isLongText,
                    ScrollBars = isLongText ? ScrollBars.Vertical : ScrollBars.None,
                    ReadOnly = field.IsReadOnly,
                    UseSystemPasswordChar = field.Key.Contains("password", StringComparison.OrdinalIgnoreCase)
                };
                _controls[field.Key] = input;
                layout.Controls.Add(input, 1, index);
            }
        }

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 10, 0, 0)
        };
        var accept = new Button { Text = "Lưu", DialogResult = DialogResult.OK, Width = 95, Height = 32 };
        var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Width = 95, Height = 32 };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(accept);
        layout.Controls.Add(buttons, 0, fields.Count);
        layout.SetColumnSpan(buttons, 2);
        Controls.Add(layout);
        AcceptButton = accept;
        CancelButton = cancel;
    }

    private sealed class ComboBoxOption
    {
        public string Id { get; }
        public string Name { get; }
        public ComboBoxOption(string id, string name) { Id = id; Name = name; }
        public override string ToString() => Name;
    }

    public static Dictionary<string, string>? Show(
        IWin32Window owner,
        string title,
        params EditDialogField[] fields)
    {
        return Show(owner, title, fields.ToList());
    }

    public static Dictionary<string, string>? Show(
        IWin32Window owner,
        string title,
        List<EditDialogField> fields)
    {
        using var dialog = new EditDialog(title, fields);
        if (dialog.ShowDialog(owner) != DialogResult.OK) return null;

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, control) in dialog._controls)
        {
            if (control is ComboBox cb)
            {
                if (cb.SelectedItem is ComboBoxOption opt)
                    result[key] = opt.Id;
                else
                    result[key] = cb.SelectedValue?.ToString() ?? cb.Text;
            }
            else if (control is TextBox tb)
            {
                result[key] = tb.Text.Trim();
            }
        }
        return result;
    }

    public static Dictionary<string, string>? Show(
        IWin32Window owner,
        string title,
        List<(string Key, string Label, string Value)> fields)
    {
        return Show(owner, title, fields.Select(f => new EditDialogField(f.Key, f.Label, f.Value)).ToList());
    }
}
