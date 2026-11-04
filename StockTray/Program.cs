using System;
using System.Windows.Forms;
using Velopack;

namespace StockTray
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // Must be the first thing to run: handles Velopack's install/update/uninstall hooks
            // and may exit the process. Don't swallow exceptions here.
            VelopackApp.Build().Run();

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // No main form — this app lives entirely in the system tray.
            Application.Run(new TrayApplicationContext());
        }
    }
}
