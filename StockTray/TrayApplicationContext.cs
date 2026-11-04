using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Velopack;
using Velopack.Sources;

namespace StockTray
{
    /// <summary>
    /// Runs the whole app: no main window, just a NotifyIcon whose badge
    /// reflects the biggest mover in the watchlist, plus a context menu
    /// listing live prices and a percentage-move alert per ticker.
    /// </summary>
    public class TrayApplicationContext : ApplicationContext
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr handle);

        private readonly NotifyIcon _trayIcon;
        private readonly System.Windows.Forms.Timer _timer;
        // Public GitHub repo whose releases BuildRelease.ps1 uploads to
        private const string RepoUrl = "https://github.com/kreaton/StockTray";

        private readonly UpdateManager _updateManager = new(new GithubSource(RepoUrl, accessToken: null, prerelease: false));
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _statusItem;
        private readonly ToolStripSeparator _afterStatusSeparator;
        private readonly ToolStripSeparator _beforeActionsSeparator;
        private readonly YahooFinanceClient _client = new();

        private readonly Dictionary<string, ToolStripMenuItem> _tickerItems = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _alerted = new(StringComparer.OrdinalIgnoreCase);

        private StockSettings _settings;
        private bool _isRefreshing;

        public TrayApplicationContext()
        {
            _settings = StockSettings.Load();

            _menu = new ContextMenuStrip();

            _statusItem = new ToolStripMenuItem("Stock Tray — starting…");
            _statusItem.Click += (s, e) => OpenInBrowser(null);
            _menu.Items.Add(_statusItem);

            _afterStatusSeparator = new ToolStripSeparator();
            _menu.Items.Add(_afterStatusSeparator);

            _beforeActionsSeparator = new ToolStripSeparator();
            _menu.Items.Add(_beforeActionsSeparator);

            _menu.Items.Add(new ToolStripMenuItem("Refresh now", null, OnRefreshNow));
            _menu.Items.Add(new ToolStripMenuItem("Settings...", null, OnSettings));
            _menu.Items.Add(new ToolStripMenuItem("Check for updates...", null, OnCheckForUpdates));
            _menu.Items.Add(new ToolStripMenuItem("About...", null, OnAbout));
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(new ToolStripMenuItem("Exit", null, OnExit));

            RebuildTickerItems();

            _trayIcon = new NotifyIcon
            {
                Text = "Stock Tray",
                Icon = DrawIcon(null, false),
                ContextMenuStrip = _menu,
                Visible = true
            };
            _trayIcon.DoubleClick += OnRefreshNow;

            _timer = new System.Windows.Forms.Timer { Interval = PollIntervalMs() };
            _timer.Tick += OnTick;
            _timer.Start();

            _ = RefreshAsync();
        }

        private int PollIntervalMs() => Math.Max(1, _settings.PollIntervalMinutes) * 60_000;

        // ---------- Menu construction ----------

        private void RebuildTickerItems()
        {
            foreach (var item in _tickerItems.Values)
            {
                _menu.Items.Remove(item);
            }
            _tickerItems.Clear();

            int insertIndex = _menu.Items.IndexOf(_afterStatusSeparator) + 1;

            if (_settings.Tickers.Count == 0)
            {
                var empty = new ToolStripMenuItem("(no tickers configured — see Settings...)") { Enabled = false };
                _menu.Items.Insert(insertIndex, empty);
                _tickerItems[""] = empty;
                return;
            }

            foreach (var ticker in _settings.Tickers)
            {
                var symbol = ticker.Symbol;
                var item = new ToolStripMenuItem($"{ticker.MenuLabel}   —");
                item.Click += (s, e) => OpenInBrowser(symbol);
                _menu.Items.Insert(insertIndex++, item);
                _tickerItems[symbol] = item;
            }
        }

        private void OpenInBrowser(string? symbol)
        {
            try
            {
                string url;
                if (string.IsNullOrEmpty(symbol))
                {
                    url = string.IsNullOrWhiteSpace(_settings.MarketHomeUrl)
                        ? "https://finance.yahoo.com/"
                        : _settings.MarketHomeUrl;
                }
                else
                {
                    string template = string.IsNullOrWhiteSpace(_settings.TickerQuoteUrlTemplate)
                        ? "https://finance.yahoo.com/quote/%ASIN%/"
                        : _settings.TickerQuoteUrlTemplate;
                    url = template.Replace("%ASIN%", Uri.EscapeDataString(symbol), StringComparison.OrdinalIgnoreCase);
                }

                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                // Best-effort — no browser available or launch failed.
            }
        }

        // ---------- Refresh loop ----------

        private async void OnTick(object? sender, EventArgs e)
        {
            try
            {
                await RefreshAsync();
            }
            catch
            {
                // A failed refresh just leaves the last known prices on screen.
            }
        }

        private void OnRefreshNow(object? sender, EventArgs e) => _ = RefreshAsync();

        private async Task RefreshAsync()
        {
            if (_isRefreshing) return;

            var tickers = _settings.Tickers;
            if (tickers.Count == 0)
            {
                _statusItem.Text = "No tickers configured — see Settings...";
                _trayIcon.Text = "Stock Tray — no tickers configured";
                return;
            }

            _isRefreshing = true;
            try
            {
                var quotes = await Task.WhenAll(tickers.Select(t => _client.GetQuoteAsync(t.Symbol)));

                // Tracked separately (not by absolute magnitude): "best" is the
                // highest % change, "worst" is the lowest, so each label is
                // actually correct even when every tracked ticker is up (or down).
                Quote? best = null;
                TickerConfig? bestConfig = null;
                Quote? worst = null;
                TickerConfig? worstConfig = null;
                int errorCount = 0;
                bool anyNegative = false;

                foreach (var quote in quotes)
                {
                    var config = tickers.FirstOrDefault(t => string.Equals(t.Symbol, quote.Symbol, StringComparison.OrdinalIgnoreCase));
                    string label = config?.MenuLabel ?? quote.Symbol;

                    if (_tickerItems.TryGetValue(quote.Symbol, out var item))
                    {
                        item.Text = quote.Success
                            ? $"{label}   (close {quote.PreviousClose:0.00})   {quote.Price:0.00} {FormatPercent(quote.ChangePercent)}"
                            : $"{label}   (unavailable)";
                    }

                    if (!quote.Success)
                    {
                        errorCount++;
                        continue;
                    }

                    if (config != null)
                    {
                        CheckAlert(quote, config);
                    }

                    if (quote.ChangePercent < 0)
                    {
                        anyNegative = true;
                    }

                    if (best == null || quote.ChangePercent > best.ChangePercent)
                    {
                        best = quote;
                        bestConfig = config;
                    }

                    if (worst == null || quote.ChangePercent < worst.ChangePercent)
                    {
                        worst = quote;
                        worstConfig = config;
                    }
                }

                _statusItem.Text = $"Updated from Yahoo Finance at " + DateTime.Now.ToString("HH':'mm':'ss", System.Globalization.CultureInfo.InvariantCulture);
                UpdateIconAndTooltip(best, bestConfig, worst, worstConfig, errorCount, anyNegative);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private static string FormatPercent(double changePercent)
        {
            string sign = changePercent > 0 ? "+" : "";
            return $"{sign}{changePercent:0.00}%";
        }

        // ---------- Alerting ----------

        private void CheckAlert(Quote quote, TickerConfig config)
        {
            bool breached = Math.Abs(quote.ChangePercent) >= config.ThresholdPercent;
            bool wasAlerted = _alerted.TryGetValue(quote.Symbol, out var v) && v;

            if (breached && !wasAlerted)
            {
                _alerted[quote.Symbol] = true;

                string direction = quote.ChangePercent >= 0 ? "up" : "down";
                string message = $"{config.Label} is {direction} {Math.Abs(quote.ChangePercent):0.00}% "
                                + $"(threshold {config.ThresholdPercent:0.##}%) — now {quote.Price:0.00}";

                _trayIcon.ShowBalloonTip(6000, Truncate($"Stock Tray alert — {config.Label}", 63), message, ToolTipIcon.Warning);

                if (_settings.PlaySound)
                {
                    SystemSounds.Exclamation.Play();
                }
            }
            else if (!breached && wasAlerted)
            {
                // Reset so a later crossing of the threshold alerts again.
                _alerted[quote.Symbol] = false;
            }
        }

        // ---------- Settings ----------

        private void OnSettings(object? sender, EventArgs e)
        {
            using var form = new SettingsForm(_settings);
            if (form.ShowDialog() == DialogResult.OK)
            {
                _settings = form.ResultSettings;
                _settings.Save();

                // Thresholds may have changed — start alert tracking fresh.
                _alerted.Clear();

                RebuildTickerItems();
                _timer.Interval = PollIntervalMs();

                _ = RefreshAsync();
            }
        }

        // ---------- About ----------

        private async void OnCheckForUpdates(object? sender, EventArgs e)
        {
            if (!_updateManager.IsInstalled)
            {
                MessageBox.Show("Updates are only available in the installed app.", "Update Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var newVersion = await _updateManager.CheckForUpdatesAsync();
                if (newVersion == null)
                {
                    MessageBox.Show("No updates available.", "Update Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var confirm = MessageBox.Show(
                    $"A new version ({newVersion.TargetFullRelease.Version}) is available. Do you want to download and install it now?",
                    "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                await _updateManager.DownloadUpdatesAsync(newVersion);

                // The restart ends the process without going through OnExit, so hide the icon here
                _trayIcon.Visible = false;
                _updateManager.ApplyUpdatesAndRestart(newVersion);
            }
            catch (Exception ex)
            {
                _trayIcon.Visible = true;
                MessageBox.Show($"Update failed: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnAbout(object? sender, EventArgs e)
        {
            using var form = new AboutForm();
            form.ShowDialog();
        }

        // ---------- Exit ----------

        private void OnExit(object? sender, EventArgs e)
        {
            _timer.Stop();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _client.Dispose();
            Application.Exit();
        }

        // ---------- Display ----------

        private void UpdateIconAndTooltip(Quote? best, TickerConfig? bestConfig, Quote? worst, TickerConfig? worstConfig, int errorCount, bool anyNegative)
        {
            // The badge highlights whichever extreme moved the most, in
            // either direction — it's a "biggest mover" indicator, not tied
            // to the "best"/"worst" wording used in the tooltip below.
            Quote? extreme = best != null && worst != null
                ? (Math.Abs(best.ChangePercent) >= Math.Abs(worst.ChangePercent) ? best : worst)
                : best ?? worst;

            var oldIcon = _trayIcon.Icon;
            var newIcon = DrawIcon(extreme, anyNegative);
            _trayIcon.Icon = newIcon;

            if (oldIcon != null)
            {
                DestroyIcon(oldIcon.Handle);
                oldIcon.Dispose();
            }

            string tooltip;
            if (best != null && worst != null)
            {
                string bestLabel = bestConfig?.Label ?? best.Symbol;
                string worstLabel = worstConfig?.Label ?? worst.Symbol;

                tooltip = best.Symbol == worst.Symbol
                    ? $"{bestLabel} {FormatPercent(best.ChangePercent)}"
                    : $"Best: {bestLabel} {FormatPercent(best.ChangePercent)}  Worst: {worstLabel} {FormatPercent(worst.ChangePercent)}";

                if (errorCount > 0) tooltip += $" ({errorCount} failed)";
            }
            else
            {
                tooltip = errorCount > 0 ? $"{errorCount} ticker(s) failed to update" : "Stock Tray";
            }

            _trayIcon.Text = Truncate(tooltip, 63);
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

        /// <summary>
        /// Renders a colored triangle badge showing the biggest mover's
        /// rounded percentage change and converts it into a Win32 icon for
        /// the notify icon: a downward red triangle when one or more
        /// tickers are down, an upward green triangle when all are flat or
        /// up, and a neutral gray upward triangle when no data is available
        /// yet.
        /// </summary>
        private static Icon DrawIcon(Quote? extreme, bool anyNegative)
        {
            //anyNegative = true; // Force red triangle for testing

            Color backgroundColor;
            string label;
            bool pointingDown;

            if (extreme == null)
            {
                backgroundColor = Color.Gray;
                label = "$";
                pointingDown = false;
            }
            else
            {
                //bg = anyNegative ? Color.FromArgb(231, 76, 60) : Color.FromArgb(46, 204, 113);
                
                backgroundColor = anyNegative ? Color.DarkRed : Color.DarkGreen;
                
                pointingDown = anyNegative;

                int rounded = Math.Clamp((int)Math.Round(extreme.ChangePercent), -99, 99);
                //label = (rounded > 0 ? "+" : "") + rounded;
                label = (rounded > 0 ? "" : "") + rounded;
            }

            // Wide edge at the top, apex at the bottom for "down"; mirrored for "up".
            PointF[] triangle = pointingDown
                ? new[] { new PointF(1, 3), new PointF(31, 3), new PointF(16, 29) }
                : new[] { new PointF(16, 3), new PointF(31, 29), new PointF(1, 29) };

            // The text should sit toward the triangle's wide edge, not the
            // 32x32 box center, so it reads as centered inside the shape.
            float centroidX = (triangle[0].X + triangle[1].X + triangle[2].X) / 3f;
            float centroidY = (triangle[0].Y + triangle[1].Y + triangle[2].Y) / 3f;

            using var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.Clear(Color.Transparent);

                using var bgBrush = new SolidBrush(backgroundColor);
                g.FillPolygon(bgBrush, triangle);

                //float fontSize = label.Length > 2 ? 9f : 12f;
                float fontSize = label.Length > 2 ? 16f : 18f;

                using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                var textSize = g.MeasureString(label, font);
                var point = new PointF(centroidX - textSize.Width / 2f, centroidY - textSize.Height / 2f);

                using var textBrush = new SolidBrush(Color.White);
                g.DrawString(label, font, textBrush, point);
            }

            IntPtr hIcon = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }
    }
}
