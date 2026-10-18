using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace StockTray
{
    /// <summary>
    /// "About" dialog showing the app icon, name, version, and a short description.
    /// </summary>
    public class AboutForm : Form
    {
        public AboutForm()
        {
            Text = "About Stock Tray";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(380, 360);
            Font = new Font("Segoe UI", 9F);

            var appIcon = LoadAppIcon();
            Icon = appIcon;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            string versionText = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

            var iconBox = new PictureBox
            {
                Image = LoadEmbeddedImage("preview_master.png") ?? appIcon.ToBitmap(),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(64, 64),
                Location = new Point(20, 20)
            };
            Controls.Add(iconBox);

            var titleLabel = new Label
            {
                Text = "Stock Tray",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(100, 20)
            };
            Controls.Add(titleLabel);

            var versionLabel = new Label
            {
                Text = $"Version {versionText}, (c) Per Hejndorf",
                ForeColor = Color.DimGray,
                AutoSize = true,
                Location = new Point(100, 54)
            };
            Controls.Add(versionLabel);

            var descLabel = new Label
            {
                Text = "A configurable stock price watcher that lives in the system tray. "
                     + "It polls Yahoo Finance for your chosen tickers and alerts you when "
                     + "a price moves beyond a +/- percentage threshold you set per ticker.\n\n"
                     + "Right-click the tray icon for live prices (with an optional custom "
                     + "name per ticker), the day's best and worst movers, on-demand refresh, "
                     + "Settings, and Exit.\n\n"
                     + "Quotes come from Yahoo Finance's unofficial, unauthenticated endpoint, "
                     + "so they may be delayed and occasionally unavailable for a symbol.",
                AutoSize = false,
                Size = new Size(340, 220),
                Location = new Point(20, 96)
            };
            Controls.Add(descLabel);

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(284, 324),
                Width = 76
            };
            Controls.Add(okButton);

            AcceptButton = okButton;
            CancelButton = okButton;
        }

        private static Icon LoadAppIcon()
        {
            using var stream = FindEmbeddedResourceStream("stock_chart_icon.ico");
            return stream != null ? new Icon(stream) : SystemIcons.Application;
        }

        private static Image? LoadEmbeddedImage(string fileName)
        {
            using var stream = FindEmbeddedResourceStream(fileName);
            if (stream == null) return null;

            // Clone into a standalone bitmap so the image stays valid after the
            // resource stream (used to construct it) is disposed.
            using var loaded = Image.FromStream(stream);
            return new Bitmap(loaded);
        }

        private static System.IO.Stream? FindEmbeddedResourceStream(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string? resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, System.StringComparison.OrdinalIgnoreCase));

            return resourceName != null ? assembly.GetManifestResourceStream(resourceName) : null;
        }
    }
}
