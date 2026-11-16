using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace StockTray
{
    /// <summary>
    /// User-configurable watchlist and behavior, persisted as JSON under
    /// %AppData%\StockTray\settings.json.
    /// </summary>
    public class StockSettings
    {
        public List<TickerConfig> Tickers { get; set; } = new()
        {
            new TickerConfig("AAPL", 5, "Apple"),
            new TickerConfig("MSFT", 5, "Microsoft"),
            new TickerConfig("GOOGL", 5, "Alphabet")
        };

        public int PollIntervalMinutes { get; set; } = 5;
        public bool PlaySound { get; set; } = true;

        /// <summary>Opened when the "Updated" menu item is clicked.</summary>
        public string MarketHomeUrl { get; set; } = "https://finance.yahoo.com/";

        /// <summary>
        /// Opened when a ticker menu item is clicked. "%ASIN%" is replaced
        /// with the ticker's symbol.
        /// </summary>
        public string TickerQuoteUrlTemplate { get; set; } = "https://finance.yahoo.com/quote/%ASIN%/";

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StockTray",
            "settings.json");

        public static StockSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    var loaded = JsonSerializer.Deserialize<StockSettings>(json);
                    if (loaded != null)
                    {
                        return loaded;
                    }
                }
            }
            catch
            {
                // Corrupt or unreadable settings file: fall back to defaults.
            }

            return new StockSettings();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // Persistence is best-effort; the app keeps running with in-memory settings.
            }
        }

        private static readonly JsonSerializerOptions ExportOptions = new() { WriteIndented = true };

        /// <summary>Writes these settings to an arbitrary JSON file. Throws on I/O errors.</summary>
        public void ExportTo(string path)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(this, ExportOptions));
        }

        /// <summary>
        /// Reads settings from a JSON file previously written by <see cref="ExportTo"/>.
        /// Missing values fall back to defaults. Throws <see cref="InvalidDataException"/>
        /// if the file is not a valid settings file.
        /// </summary>
        public static StockSettings ImportFrom(string path)
        {
            StockSettings? imported;
            try
            {
                imported = JsonSerializer.Deserialize<StockSettings>(
                    File.ReadAllText(path),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"The file is not valid settings JSON: {ex.Message}", ex);
            }

            if (imported == null)
            {
                throw new InvalidDataException("The file does not contain any settings.");
            }

            var defaults = new StockSettings();
            imported.Tickers = (imported.Tickers ?? new List<TickerConfig>())
                .Where(t => t != null && !string.IsNullOrWhiteSpace(t.Symbol))
                .Select(t => new TickerConfig(
                    t.Symbol.Trim().ToUpperInvariant(),
                    t.ThresholdPercent > 0 ? t.ThresholdPercent : 5.0,
                    t.DisplayName?.Trim() ?? ""))
                .GroupBy(t => t.Symbol)
                .Select(g => g.First())
                .ToList();
            imported.PollIntervalMinutes = Math.Clamp(imported.PollIntervalMinutes, 1, 120);
            if (string.IsNullOrWhiteSpace(imported.MarketHomeUrl))
            {
                imported.MarketHomeUrl = defaults.MarketHomeUrl;
            }
            if (string.IsNullOrWhiteSpace(imported.TickerQuoteUrlTemplate))
            {
                imported.TickerQuoteUrlTemplate = defaults.TickerQuoteUrlTemplate;
            }

            return imported;
        }

        public StockSettings Clone() => new()
        {
            Tickers = Tickers.Select(t => t.Clone()).ToList(),
            PollIntervalMinutes = PollIntervalMinutes,
            PlaySound = PlaySound,
            MarketHomeUrl = MarketHomeUrl,
            TickerQuoteUrlTemplate = TickerQuoteUrlTemplate
        };
    }
}
