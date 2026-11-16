using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace StockTray
{
    /// <summary>
    /// Dialog for editing the watched tickers, their alert thresholds, and
    /// general behavior. Built entirely in code so no designer file is needed.
    /// </summary>
    public class SettingsForm : Form
    {
        private readonly DataGridView _grid;
        private readonly NumericUpDown _intervalUpDown;
        private readonly CheckBox _soundCheck;
        private readonly CheckBox _launchAtStartupCheck;
        private readonly TextBox _marketHomeUrlBox;
        private readonly TextBox _tickerQuoteUrlBox;

        public StockSettings ResultSettings { get; private set; }

        public SettingsForm(StockSettings current)
        {
            Text = "Stock Tray Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(420, 512);
            Font = new Font("Segoe UI", 9F);

            var gridLabel = new Label
            {
                Text = "Watched tickers, an optional display name, and alert threshold (% move vs. previous close):",
                AutoSize = false,
                Size = new Size(396, 32),
                Location = new Point(12, 12)
            };
            Controls.Add(gridLabel);

            _grid = new DataGridView
            {
                Location = new Point(12, 46),
                Size = new Size(396, 200),
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Symbol",
                HeaderText = "Symbol",
                Width = 100
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DisplayName",
                HeaderText = "Display name (optional)",
                Width = 190
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Threshold",
                HeaderText = "Alert %",
                Width = 90
            });

            FillGrid(current.Tickers);

            Controls.Add(_grid);

            var hintLabel = new Label
            {
                Text = "Select a row and press Delete to remove a ticker.",
                ForeColor = Color.DimGray,
                AutoSize = true,
                Location = new Point(12, 250)
            };
            Controls.Add(hintLabel);

            var intervalLabel = new Label
            {
                Text = "Check every (minutes):",
                AutoSize = true,
                Location = new Point(12, 282)
            };
            Controls.Add(intervalLabel);

            _intervalUpDown = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 120,
                Value = Math.Min(Math.Max(current.PollIntervalMinutes, 1), 120),
                Width = 60,
                Location = new Point(180, 280)
            };
            Controls.Add(_intervalUpDown);

            _soundCheck = new CheckBox
            {
                Text = "Play a sound when an alert fires",
                Checked = current.PlaySound,
                AutoSize = true,
                Location = new Point(12, 316)
            };
            Controls.Add(_soundCheck);

            _launchAtStartupCheck = new CheckBox
            {
                Text = "Start automatically when Windows starts",
                Checked = AutoStartManager.IsEnabled(),
                AutoSize = true,
                Location = new Point(12, 343)
            };
            Controls.Add(_launchAtStartupCheck);

            var marketHomeLabel = new Label
            {
                Text = "Home page link (opened from the \"Updated\" menu item):",
                AutoSize = true,
                Location = new Point(12, 375)
            };
            Controls.Add(marketHomeLabel);

            _marketHomeUrlBox = new TextBox
            {
                Text = current.MarketHomeUrl,
                Width = 396,
                Location = new Point(12, 393)
            };
            Controls.Add(_marketHomeUrlBox);

            var tickerUrlLabel = new Label
            {
                Text = "Per-ticker quote link (%ASIN% is replaced with the symbol):",
                AutoSize = true,
                Location = new Point(12, 422)
            };
            Controls.Add(tickerUrlLabel);

            _tickerQuoteUrlBox = new TextBox
            {
                Text = current.TickerQuoteUrlTemplate,
                Width = 396,
                Location = new Point(12, 440)
            };
            Controls.Add(_tickerQuoteUrlBox);

            var importButton = new Button
            {
                Text = "Import...",
                Location = new Point(12, 472),
                Width = 76
            };
            importButton.Click += OnImport;
            Controls.Add(importButton);

            var exportButton = new Button
            {
                Text = "Export...",
                Location = new Point(92, 472),
                Width = 76
            };
            exportButton.Click += OnExport;
            Controls.Add(exportButton);

            var toolTip = new ToolTip();
            toolTip.SetToolTip(importButton, "Load settings from a JSON file into this dialog. Press OK to apply them.");
            toolTip.SetToolTip(exportButton, "Save the settings shown in this dialog to a JSON file.");

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(252, 472),
                Width = 76
            };
            okButton.Click += OnOk;
            Controls.Add(okButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(332, 472),
                Width = 76
            };
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            ResultSettings = current.Clone();
        }

        private void OnOk(object? sender, EventArgs e)
        {
            ResultSettings = BuildSettingsFromControls();
            AutoStartManager.SetEnabled(_launchAtStartupCheck.Checked);
        }

        private void FillGrid(IEnumerable<TickerConfig> tickers)
        {
            _grid.Rows.Clear();
            foreach (var ticker in tickers)
            {
                _grid.Rows.Add(ticker.Symbol, ticker.DisplayName, ticker.ThresholdPercent);
            }
        }

        private void LoadControls(StockSettings settings)
        {
            FillGrid(settings.Tickers);
            _intervalUpDown.Value = Math.Min(Math.Max(settings.PollIntervalMinutes, 1), 120);
            _soundCheck.Checked = settings.PlaySound;
            _marketHomeUrlBox.Text = settings.MarketHomeUrl;
            _tickerQuoteUrlBox.Text = settings.TickerQuoteUrlTemplate;
        }

        private void OnExport(object? sender, EventArgs e)
        {
            _grid.EndEdit();

            using var dialog = new SaveFileDialog
            {
                Title = "Export Stock Tray settings",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"StockTray-settings-{DateTime.Now:yyyy-MM-dd}.json",
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                BuildSettingsFromControls().ExportTo(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not export settings:\n\n{ex.Message}",
                    "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnImport(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Import Stock Tray settings",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            StockSettings imported;
            try
            {
                imported = StockSettings.ImportFrom(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not import settings:\n\n{ex.Message}",
                    "Import failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _grid.CancelEdit();
            LoadControls(imported);

            MessageBox.Show(this,
                $"Imported {imported.Tickers.Count} ticker(s) and general settings.\n\nPress OK to apply them, or Cancel to discard.",
                "Settings imported", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private StockSettings BuildSettingsFromControls()
        {
            var tickers = new List<TickerConfig>();

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;

                string symbol = Convert.ToString(row.Cells["Symbol"].Value)?.Trim().ToUpperInvariant() ?? "";
                if (symbol.Length == 0) continue;

                string displayName = Convert.ToString(row.Cells["DisplayName"].Value)?.Trim() ?? "";

                if (!double.TryParse(Convert.ToString(row.Cells["Threshold"].Value), out double threshold) || threshold <= 0)
                {
                    threshold = 5.0;
                }

                if (!tickers.Any(t => t.Symbol == symbol))
                {
                    tickers.Add(new TickerConfig(symbol, threshold, displayName));
                }
            }

            string marketHomeUrl = _marketHomeUrlBox.Text.Trim();
            string tickerQuoteUrlTemplate = _tickerQuoteUrlBox.Text.Trim();

            return new StockSettings
            {
                Tickers = tickers,
                PollIntervalMinutes = (int)_intervalUpDown.Value,
                PlaySound = _soundCheck.Checked,
                MarketHomeUrl = marketHomeUrl.Length > 0 ? marketHomeUrl : new StockSettings().MarketHomeUrl,
                TickerQuoteUrlTemplate = tickerQuoteUrlTemplate.Length > 0 ? tickerQuoteUrlTemplate : new StockSettings().TickerQuoteUrlTemplate
            };
        }
    }
}
