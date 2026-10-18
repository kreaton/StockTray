# Stock Tray

A Windows system-tray stock price watcher built with WinForms (.NET). No main
window — it lives entirely in the notification area. The tray icon badge
shows the rounded percentage move of your biggest mover: green for up, red
for down.

## Requirements

- Windows 10/11
- [.NET SDK](https://dotnet.microsoft.com/download) matching the
  `net10.0-windows7.0` target in `StockTray.csproj` (Windows-only, WinForms)
- Internet access to `query1.finance.yahoo.com`

## Build & run

```
cd StockTray
dotnet run
```

Or open the folder in Visual Studio and press F5.

To produce a standalone .exe:

```
dotnet publish -c Release -r win-x64 --self-contained false
```

The output lands in `bin\Release\net10.0-windows7.0\win-x64\publish\`.

## Using it

- The app starts minimized to the tray with a default watchlist (AAPL, MSFT,
  GOOGL) and fetches prices immediately.
- Right-click the tray icon to see each ticker's live price and % change vs.
  the previous close, refresh on demand, open Settings, or exit.
- Double-click the tray icon to refresh immediately.
- A balloon notification (plus an optional sound) fires the first time a
  ticker's move crosses its configured +/- percentage threshold. The alert
  re-arms once the move drops back under the threshold, so you're notified
  again only on a fresh crossing, not every poll.
- The tray icon badge and tooltip reflect whichever tracked ticker has moved
  the most (up or down) since the last close.

## Configurable settings (right-click → Settings...)

- The list of watched ticker symbols (Yahoo Finance symbols, e.g. `AAPL`,
  `MSFT`, `BTC-USD`, `^GSPC`)
- An optional custom display name per ticker (e.g. `AAPL` → "Apple"), shown
  in the menu, tray tooltip, and alert notifications instead of the raw
  symbol
- An alert threshold percentage per ticker
- How often to check prices (minutes)
- Whether a sound plays when an alert fires
- Whether the app starts automatically when Windows starts

Settings are saved to `%AppData%\StockTray\settings.json` and reloaded
automatically the next time the app starts.

## Data source

Prices come from Yahoo Finance's unofficial `chart` endpoint
(`query1.finance.yahoo.com/v8/finance/chart/{symbol}`). It needs no API key
or signup, but it's an undocumented, unofficial API that Yahoo can change or
rate-limit without notice — quotes may be delayed and, occasionally, a
symbol may fail to fetch. A failed ticker shows as "(unavailable)" in the
menu rather than crashing the app; it's retried on the next poll.

## Files

- `Program.cs` — entry point, starts the tray application context
- `TrayApplicationContext.cs` — poll timer, tray icon/menu, and the alert
  logic (threshold crossing, balloon tip, sound)
- `YahooFinanceClient.cs` — fetches and parses a quote for one symbol
- `Quote.cs` — a single price snapshot (or error) for a symbol
- `SettingsForm.cs` — the settings dialog (built entirely in code)
- `StockSettings.cs` / `TickerConfig.cs` — settings model with JSON load/save
- `AutoStartManager.cs` — optional "start with Windows" registry toggle
