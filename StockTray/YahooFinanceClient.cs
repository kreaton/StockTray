using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace StockTray
{
    /// <summary>
    /// Fetches quotes from Yahoo Finance's unofficial "chart" endpoint, which
    /// needs no API key or signup. It's not an official/documented API, so
    /// individual requests are wrapped defensively — a broken response for
    /// one symbol never takes down the rest of the watchlist.
    /// </summary>
    public class YahooFinanceClient : IDisposable
    {
        private readonly HttpClient _http;

        public YahooFinanceClient()
        {
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) StockTray/1.0");
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }

        public async Task<Quote> GetQuoteAsync(string symbol)
        {
            try
            {
                string url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbol)}?interval=1d&range=1d";
                using var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return new Quote { Symbol = symbol, Error = $"HTTP {(int)response.StatusCode}" };
                }

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var chart = doc.RootElement.GetProperty("chart");

                if (chart.TryGetProperty("error", out var errorProp) && errorProp.ValueKind != JsonValueKind.Null)
                {
                    string description = errorProp.TryGetProperty("description", out var d)
                        ? d.GetString() ?? "Unknown symbol"
                        : "Unknown symbol";
                    return new Quote { Symbol = symbol, Error = description };
                }

                var results = chart.GetProperty("result");
                if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
                {
                    return new Quote { Symbol = symbol, Error = "No data returned" };
                }

                var meta = results[0].GetProperty("meta");

                if (!meta.TryGetProperty("regularMarketPrice", out var priceProp) || priceProp.ValueKind != JsonValueKind.Number)
                {
                    return new Quote { Symbol = symbol, Error = "Price unavailable" };
                }
                double price = priceProp.GetDouble();

                double previousClose;
                if (meta.TryGetProperty("previousClose", out var pc) && pc.ValueKind == JsonValueKind.Number)
                {
                    previousClose = pc.GetDouble();
                }
                else if (meta.TryGetProperty("chartPreviousClose", out var cpc) && cpc.ValueKind == JsonValueKind.Number)
                {
                    previousClose = cpc.GetDouble();
                }
                else
                {
                    previousClose = price;
                }

                return new Quote { Symbol = symbol, Price = price, PreviousClose = previousClose };
            }
            catch (Exception ex)
            {
                return new Quote { Symbol = symbol, Error = ex.Message };
            }
        }

        public void Dispose() => _http.Dispose();
    }
}
