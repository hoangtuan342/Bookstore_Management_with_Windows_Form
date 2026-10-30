using TrangSach.GUI.Common;

namespace TrangSach.Customer.Forms;

public partial class CustomerForm
{
    private async Task SafeAsync(Func<Task> action)
    {
        try { UseWaitCursor = true; await action(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Không thực hiện được", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { UseWaitCursor = false; }
    }

    private static void StyleCustomerGrid(DataGridView grid)
    {
        grid.BackgroundColor = Color.FromArgb(251, 250, 247);
        grid.GridColor = Color.FromArgb(233, 231, 223);
        grid.DefaultCellStyle.BackColor = Color.White;
        grid.DefaultCellStyle.ForeColor = Color.FromArgb(38, 45, 37);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 237, 227);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(38, 45, 37);
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 244);
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(232, 237, 227);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(53, 86, 68);
        grid.RowTemplate.Height = 38;
    }

    private static TabPage CreateGridPage(string title, out DataGridView grid)
    {
        var page = new TabPage(title);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, Padding = Padding.Empty, GrowStyle = TableLayoutPanelGrowStyle.FixedSize };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid = new DataGridView
        {
            Dock = DockStyle.Fill, Margin = Padding.Empty, ReadOnly = true, AutoGenerateColumns = true,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White, RowHeadersVisible = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 36, EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(230, 238, 247), ForeColor = Color.FromArgb(25, 45, 68),
                Font = new Font("Segoe UI", 9, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 6, 0), WrapMode = DataGridViewTriState.False
            }
        };
        grid.RowTemplate.Height = 32;
        layout.Controls.Add(grid, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private static FlowLayoutPanel ToolBar(Control page)
    {
        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = new Padding(8, 6, 8, 4), WrapContents = false, AutoScroll = true };
        if (page.Controls.OfType<TableLayoutPanel>().FirstOrDefault() is { } layout) layout.Controls.Add(bar, 0, 0);
        else { bar.Dock = DockStyle.Top; bar.Height = 48; page.Controls.Add(bar); bar.BringToFront(); }
        return bar;
    }

    private static Button ActionButton(string text, Func<Task> action)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 32, Margin = new Padding(3), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(237, 242, 247) };
        button.Click += async (_, _) =>
        {
            var form = button.FindForm();
            try { if (form is not null) form.UseWaitCursor = true; await action(); }
            catch (Exception ex) { MessageBox.Show(form, ex.Message, "Không thực hiện được", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { if (form is not null) form.UseWaitCursor = false; }
        };
        return button;
    }

    private static Button HeaderButton(string text, int x, int y, int width) => new() { Text = text, Left = x, Top = y, Width = width, Height = 29, FlatStyle = FlatStyle.Flat };
}

