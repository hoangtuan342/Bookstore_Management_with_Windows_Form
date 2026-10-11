namespace TrangSach.Management;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrangSach.WinForms.Form1(managementMode: true));
    }
}
