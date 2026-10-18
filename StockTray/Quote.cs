namespace StockTray
{
    /// <summary>
    /// A single price snapshot for a ticker, or an error if the fetch failed.
    /// </summary>
    public class Quote
    {
        public required string Symbol { get; init; }
        public double Price { get; init; }
        public double PreviousClose { get; init; }
        public string? Error { get; init; }

        public bool Success => Error == null;

        public double ChangePercent => PreviousClose != 0
            ? (Price - PreviousClose) / PreviousClose * 100.0
            : 0.0;
    }
}
