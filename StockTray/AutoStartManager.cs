using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace StockTray
{
    /// <summary>
    /// Registers (or unregisters) the app to launch automatically when the
    /// current user signs in, using the standard per-user Run registry key.
    /// This requires no admin rights and no installer.
    /// </summary>
    public static class AutoStartManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "StockTray";

        public static bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
                return key?.GetValue(ValueName) != null;
            }
            catch
            {
                return false;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
                if (key == null) return;

                if (enabled)
                {
                    string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
                    key.SetValue(ValueName, $"\"{exePath}\"");
                }
                else if (key.GetValue(ValueName) != null)
                {
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
                }
            }
            catch
            {
                // Best-effort: if the registry write fails (e.g. restricted policy),
                // the app still runs fine — it just won't auto-launch.
            }
        }
    }
}
