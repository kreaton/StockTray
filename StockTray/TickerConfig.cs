namespace StockTray
{
    /// <summary>
    /// A single watched ticker and the +/- percentage move (vs. the previous
    /// close) that should trigger an alert for it.
    /// </summary>
    public class TickerConfig
    {
        public string Symbol { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public double ThresholdPercent { get; set; } = 5.0;

        public TickerConfig() { }

        public TickerConfig(string symbol, double thresholdPercent, string displayName = "")
        {
            Symbol = symbol;
            ThresholdPercent = thresholdPercent;
            DisplayName = displayName;
        }

        /// <summary>The display name if one is set, otherwise the raw symbol.</summary>
        public string Label => string.IsNullOrWhiteSpace(DisplayName) ? Symbol : DisplayName;

        /// <summary>Label followed by the symbol in parentheses, when a custom name is set.</summary>
        public string MenuLabel => string.IsNullOrWhiteSpace(DisplayName) ? Symbol : $"{DisplayName} ({Symbol})";

        public TickerConfig Clone() => new(Symbol, ThresholdPercent, DisplayName);
    }
}
