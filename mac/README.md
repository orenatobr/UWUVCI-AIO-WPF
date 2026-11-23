# UWUVCI for macOS

A native Apple Silicon port of the UWUVCI Wii U Virtual Console injector, built with Avalonia 12 on .NET 10.
The conversion and packing logic lives in `src/UWUVCI.Core`. `src/UWUVCI.Mac` is the window around it.

You bring your own legally obtained ROM, base game and keys. The app ships none of them.

## Scope

Only Nintendo 64 works today. The other consoles are listed in the picker but disabled, because Core does not implement them yet.

The flow is a single window with a step list: console, ROM, base, name and images, N64 options, output. You can drop a ROM or choose one, and a card shows what was detected (game, region, save type, controller accessory, Expansion Pak) with a read only preview of the ini that will be written. The base is either a decrypted folder (it must contain `code`, `content`, `meta`) or a download from a title id and title key. The Wii U common key is set once in Settings.

Settings are stored in `~/Library/Application Support/UWUVCI/settings.json` with file mode 600. The log is `~/Library/Logs/UWUVCI/uwuvci.log`. Keys are never written to the log.

## Build, run, test

```
cd mac
dotnet build -warnaserror
dotnet run --project src/UWUVCI.Mac
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

Set `UWUVCI_SCREENSHOT_DIR` before `dotnet test` to keep the PNGs rendered by the headless tests.

## What is verified

- Unit tests cover the view models, the settings store (round trip, mode 600), path and file validation, and ROM detection. View model line coverage is about 95 percent.
- Headless Skia tests render each page in light and dark and fail if the XAML does not load. Screenshots are in `docs/screenshots`.
- The app was launched with `dotnet run` on macOS and its window rendered.

## What is not verified

- A full inject against a real base and a real ROM was not run from the UI. Core has its own tests with synthetic data.
- Downloading a base from the Nintendo CDN was not run.
- Drag and drop and the native file panels were not exercised by hand or by automation.
- VoiceOver was not tried. Controls have automation names and keyboard focus styles, but that is not a substitute for testing.
- Cancel takes effect at the next step boundary of the injector, not instantly.
