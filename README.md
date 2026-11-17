# Stock Tray

[![Release](https://img.shields.io/github/v/release/kreaton/StockTray)](https://github.com/kreaton/StockTray/releases/latest)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
![Windows](https://img.shields.io/badge/platform-Windows-lightgrey)
![.NET 10](https://img.shields.io/badge/.NET-10-lightgrey)
![WinForms](https://img.shields.io/badge/UI-WinForms-lightgrey)

![Stock Tray: your watchlist in the Windows notification area](images/banner.png)

**Keep an eye on your stocks from the Windows notification area.**

The tray icon shows the biggest move in your watchlist, and a notification tells you when a stock moves more than you've chosen.

## Install

**[Download the installer](https://github.com/kreaton/StockTray/releases/latest/download/StockTray-win-Setup.exe)** and run it. If your PC doesn't have the .NET 10 Desktop Runtime yet, the installer adds it.

The installer isn't code-signed, so Windows may warn you about it. To continue, choose **More info → Run anyway**.

Windows may hide new tray icons behind the **^** arrow on the taskbar. Drag the icon out onto the taskbar to keep it in view.

To get a newer version later, right-click the icon and choose **Check for updates...**. To remove the app, use **Settings → Apps → Installed apps** in Windows.

## How to use it

Stock Tray starts with Apple, Microsoft and Alphabet on the watchlist and fetches their prices right away.

**The icon** shows the largest percentage move today, rounded to a whole number:

| Icon | Meaning |
|---|---|
| Red triangle pointing down | At least one stock is down |
| Green triangle pointing up | Nothing is down |
| Gray triangle with `$` | No prices yet |

Hover over the icon to see the best and worst performers, and how many stocks couldn't be fetched.

**The right-click menu** lists each stock's previous close, current price and percentage change. Click a stock to open its Yahoo Finance page, or click the "Updated" line at the top to open the Yahoo Finance home page. Choose **Refresh now**, or double-click the icon, to update right away. The menu also has **Settings...**, **Check for updates...**, **About...** and **Exit**.

**Alerts** appear the first time a stock's move since the previous close reaches your limit, up or down. You won't get another alert for that stock until its move has dropped back under the limit and reached it again. Saving the settings or restarting Stock Tray starts over, so a stock that's already past its limit alerts again.

## Settings

Right-click the icon and choose **Settings...**.

- **Watchlist:** Yahoo Finance symbols such as `AAPL`, `BTC-USD` or `^GSPC`. Symbols are saved in capitals, and a symbol listed twice is kept once. To remove a stock, select its row and press Delete.
- **Display name:** an optional name to show instead of the symbol, for example "Apple" for `AAPL`.
- **Alert %:** how big a move triggers an alert for each stock. The default is 5%, which is also used if you leave it empty.
- **Check every:** how often prices are fetched, from 1 to 120 minutes. The default is 5 minutes.
- **Sound:** play a sound when an alert fires. On by default.
- **Start automatically when Windows starts:** off by default.
- **Links:** the pages opened from the menu, if you prefer another site to Yahoo Finance. In the per-stock link, `%ASIN%` is replaced with the symbol. Leave a link empty to go back to Yahoo Finance.
- **Import... / Export...:** save your settings to a JSON file, or load them from one, for example to move your watchlist to another PC. An import fills in the dialog, and nothing changes until you press OK. The start-with-Windows option is not included.

## Where the prices come from

Prices come from an unofficial Yahoo Finance service that needs no account or API key. Yahoo can change or limit it without notice, and quotes may be delayed, so don't rely on them for trading decisions. If a stock can't be fetched, it shows as "(unavailable)" and is tried again at the next check.

---

## For developers

The app is built with WinForms on .NET 10 and only runs on Windows. Installs and updates use [Velopack](https://velopack.io). Settings are saved to `%AppData%\StockTray\settings.json`. The "start with Windows" option isn't in that file: it's a `StockTray` value under `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`.

Quotes come from `query1.finance.yahoo.com/v8/finance/chart/{symbol}`.

### Build and run

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). You can also open `StockTray.slnx` in Visual Studio and press F5.

```bash
dotnet run --project StockTray
```

### Releases

Every push to `main` builds the app on GitHub Actions and publishes a new release, one patch version higher than the last (`.github/workflows/release.yml`). To push without releasing, add `[skip ci]` to the commit message. You can also start a release by hand from the repository's Actions tab, or run `StockTray\BuildRelease.ps1` locally. Add `-LocalOnly` to build without uploading. The script needs the Velopack `vpk` tool, in the same version as the Velopack package; the comment at the top of the script has the install command.

The repository must stay public, because **Check for updates...** reads its releases without logging in.

### Source files

All of these are in the `StockTray` folder.

- `Program.cs`: entry point, starts the tray application
- `TrayApplicationContext.cs`: polling, tray icon, menu, alerts and updates
- `YahooFinanceClient.cs`: fetches and parses a quote for one symbol
- `Quote.cs`: a price snapshot, or the error, for one symbol
- `SettingsForm.cs`: settings dialog
- `AboutForm.cs`: about dialog
- `StockSettings.cs` and `TickerConfig.cs`: settings, loaded from and saved to JSON
- `AutoStartManager.cs`: the "start with Windows" option
- `BuildRelease.ps1`: builds, packs and publishes a release

The `tools` folder has two helper scripts for working copies that were downloaded rather than cloned. `Unblock-And-Rebuild.ps1` removes the Windows download mark (Mark of the Web) from every file, deletes the `bin` and `obj` folders and rebuilds. `MotwRemove.ps1` only lists which files in `tools` carry that mark.

## License & Authorship

This project is distributed under the terms of the [MIT License](LICENSE).

**AI authorship note:** Most of the source code in this repository was generated with AI coding agents (Claude Code), under my direction and review. Purely AI-generated material may not be eligible for copyright protection in some jurisdictions. To the extent any part of this project is protected by copyright, it is licensed under the MIT License, and the license's warranty and liability disclaimers apply to the project as a whole.

<!-- notices:start -->
### Independent development

StockTray is an independent, personal project developed by Per Hejndorf. It is not affiliated with, endorsed by or sponsored by Microsoft or any other rights holder (including Yahoo). Product names and logos are trademarks of their respective owners and are used only to identify the services and tools the project works with.

### Third-party notices and asset provenance

[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) lists every third-party library, font and online service the project uses, with version, licence, source and whether it was modified. It also records where the icons, images, audio and video came from and how they are licensed.
<!-- notices:end -->
