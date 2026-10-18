using System;
using System.Windows.Forms;

namespace StockTray
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // No main form — this app lives entirely in the system tray.
            Application.Run(new TrayApplicationContext());
        }
    }
}
