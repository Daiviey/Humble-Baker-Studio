using System.Text;

namespace HumbleBakerStudio;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogCrash(e.ExceptionObject as Exception);
        Application.ThreadException += (_, e) => LogCrash(e.Exception);

        try
        {
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            LogCrash(ex);
            MessageBox.Show(ex.ToString(), "Humble Baker Studio", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void LogCrash(Exception? ex)
    {
        if (ex == null) return;
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine(DateTime.Now.ToString("o"));
            sb.AppendLine(ex.ToString());
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "hbs-csharp-crash.log"), sb.ToString());
        }
        catch
        {
            // Never let crash logging take down the handler.
        }
    }
}
