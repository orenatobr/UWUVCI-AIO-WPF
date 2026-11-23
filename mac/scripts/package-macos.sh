#!/bin/bash
# Builds UWUVCI.app for Apple Silicon and zips it into mac/dist.
# Usage: mac/scripts/package-macos.sh [version]
# ReadyToRun precompiles the app for arm64: the window opens in about 1.2 s instead of 1.85 s
# (median of 3 launches on an M series Mac). The bundle is ad-hoc signed. Developer ID signing and notarization need an Apple account
# and are not done here; macOS will ask the user to approve the first launch.
set -euo pipefail

cd "$(dirname "$0")/.."
VERSION="${1:-0.0.0}"
APP="dist/UWUVCI.app"
PROJECT="src/UWUVCI.Mac/UWUVCI.Mac.csproj"

rm -rf dist
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"

dotnet publish "$PROJECT" -c Release -r osx-arm64 --self-contained true \
  -p:PublishReadyToRun=true -p:InvariantGlobalization=true \
  -p:DebugType=none -p:Version="$VERSION" -o dist/publish

cp -R dist/publish/. "$APP/Contents/MacOS/"
EXE="$(basename "$PROJECT" .csproj)"
[ -f "$APP/Contents/MacOS/$EXE" ] || { echo "missing executable $EXE" >&2; exit 1; }
chmod +x "$APP/Contents/MacOS/$EXE"
[ -f src/UWUVCI.Mac/Assets/UWUVCI.icns ] && cp src/UWUVCI.Mac/Assets/UWUVCI.icns "$APP/Contents/Resources/"

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>CFBundleName</key><string>UWUVCI</string>
  <key>CFBundleDisplayName</key><string>UWUVCI</string>
  <key>CFBundleIdentifier</key><string>io.github.orenatobr.uwuvci</string>
  <key>CFBundleExecutable</key><string>$EXE</string>
  <key>CFBundleIconFile</key><string>UWUVCI</string>
  <key>CFBundleShortVersionString</key><string>$VERSION</string>
  <key>CFBundleVersion</key><string>$VERSION</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>LSApplicationCategoryType</key><string>public.app-category.utilities</string>
</dict></plist>
PLIST

rm -rf dist/publish
codesign --force --deep --sign - "$APP"
ditto -c -k --keepParent "$APP" "dist/UWUVCI-macOS-arm64-$VERSION.zip"
echo "Built dist/UWUVCI-macOS-arm64-$VERSION.zip"
