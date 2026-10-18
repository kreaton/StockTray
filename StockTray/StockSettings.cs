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
