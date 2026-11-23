# UWUVCI for macOS

A native Apple Silicon port of the UWUVCI Wii U Virtual Console injector, built with Avalonia 12 on .NET 10.
The conversion and packing logic lives in `src/UWUVCI.Core`. `src/UWUVCI.Mac` is the window around it.

You bring your own legally obtained ROM, base game and keys. The app ships none of them.

## Scope

Only Nintendo 64 works today. The other consoles are listed in the picker but disabled, because Core does not implement them yet. Each card has a small line art icon (a pad, handheld or keyboard silhouette drawn as vector geometry, no logos) in its own accent colour.

The flow is a single window with a step list: console, ROM, base, name and images, N64 options, output. You can drop a ROM or choose one, and a card shows what was detected (game, region, save type, controller accessory, Expansion Pak) with a read only preview of the ini that will be written. The base is chosen from a dropdown of the 23 N64 Virtual Console bases, as in the original UWUVCI. Choosing one downloads it automatically: the first time you type that base's title key (the app checks it against a stored hash and ships no keys), it is saved in `settings.json` and later picks download with no prompt. A base already downloaded under the app folder is reused at once. Downloads need the common key from Settings. A decrypted folder (`code`, `content`, `meta`) is still available below the dropdown as a secondary option. The Wii U common key is set once in Settings.

The Options step also picks the engine. Nintendo Virtual Console (default) injects into a base title you supply. Native runs the not64 emulator: no base or title key is needed, the Base step is skipped, widescreen, dark filter, Expansion Pak and custom ini are hidden, and the finished screen shows two folders, the packed title and an SD folder to copy to the root of the SD card. The native path downloads the emulator template from the not64-wiiu GitHub release when you press Inject.

Settings are stored in `~/Library/Application Support/UWUVCI/settings.json` with file mode 600. The log is `~/Library/Logs/UWUVCI/uwuvci.log`. Keys are never written to the log.

## Apple Silicon build

`scripts/package-macos.sh <version>` publishes a self-contained osx-arm64 build with ReadyToRun and
invariant globalization, wraps it in `UWUVCI.app`, ad-hoc signs it and zips it into `dist/`. ReadyToRun
cut the time to a visible window from about 1.85 s to about 1.24 s in a quick local measurement (3 launches
each, one M series Mac). The bundle grows from 109 MB to 128 MB. Packing and image work run natively on
arm64 with no Rosetta, Wine or Java.

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

- Unit tests cover the view models, the settings store (round trip, mode 600), path and file validation, and ROM detection. View model line coverage is about 96 percent.
- Headless Skia tests render each page in light and dark and fail if the XAML does not load. Screenshots are in `docs/screenshots`.
- The app was launched with `dotnet run` on macOS and its window rendered.

## End to end check

`tests/UWUVCI.Mac.Tests/EndToEndTests.cs` drives the real view model and service with no fakes: it downloads the
native template, injects a ROM you own, packs the title and checks the output (title files, SD folder, no ROM
inside the title). It does nothing unless `UWUVCI_E2E_ROM` points at a ROM:

```
UWUVCI_E2E_ROM=/path/to/game.z64 dotnet test tests/UWUVCI.Mac.Tests --filter EndToEndTests
```

It passed with a Conker ROM. The packed title is encrypted, so it was not booted; the same title layout built
unpacked by `uwuvci-cli native-layout` was booted in Cemu (see `docs/COMPATIBILITY.md`).

## What is not verified

- A full Nintendo Virtual Console inject against a real base was not run, because it needs a base title and its key. The native engine was run end to end through the real view model and inject service with a real ROM (see below), but not by clicking through the window.
- Downloading a base from the Nintendo CDN was not run.
- The native engine from the UI (template download, pack, SD folder) was not run end to end. The view model and service wiring are covered with a fake service, and the Core native path is covered by its own tests. `InjectService.InjectNativeAsync` has no automated test.
- Drag and drop and the native file panels were not exercised by hand or by automation.
- VoiceOver was not tried. Controls have automation names and keyboard focus styles, but that is not a substitute for testing.
- Cancel takes effect at the next step boundary of the injector, not instantly.
