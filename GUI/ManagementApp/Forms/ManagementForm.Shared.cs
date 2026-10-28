namespace TrangSach.Management.Forms;

public partial class ManagementForm
{
    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private bool Confirm(string question) => MessageBox.Show(this, question, "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    private static T? Selected<T>(DataGridView grid) where T : class => grid.CurrentRow?.DataBoundItem as T;

    private async Task SafeAsync(Func<Task> action)
    {
        try { UseWaitCursor = true; await action(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Không thực hiện được", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { UseWaitCursor = false; }
    }

    private static Button HeaderButton(string text, int x, int y, int width) =>
        new() { Text = text, Left = x, Top = y, Width = width, Height = 29, FlatStyle = FlatStyle.Flat };
}
