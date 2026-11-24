#!/bin/bash
# Installs the latest UWUVCI release into /Applications and clears the quarantine flag, so Gatekeeper
# does not reject the ad-hoc signed app. The zip is checked against its published SHA-256 first.
#
#   curl -fsSL https://raw.githubusercontent.com/orenatobr/UWUVCI-AIO-WPF/main/mac/install.sh | bash
#
# Environment (optional): UWUVCI_BASE_URL overrides the download folder, UWUVCI_DEST the install folder.
set -euo pipefail

[ "$(uname -s)" = "Darwin" ] || { echo "UWUVCI for macOS only runs on macOS." >&2; exit 1; }
[ "$(uname -m)" = "arm64" ] || { echo "This build is for Apple Silicon (arm64) Macs only." >&2; exit 1; }

base="${UWUVCI_BASE_URL:-https://github.com/orenatobr/UWUVCI-AIO-WPF/releases/latest/download}"
name="UWUVCI-macOS-arm64.zip"
dest="${UWUVCI_DEST:-/Applications}"

if [ ! -w "$dest" ]; then
  dest="$HOME/Applications"
  mkdir -p "$dest"
  echo "/Applications is not writable, installing into $dest instead."
fi

tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

echo "Downloading $name ..."
curl -fL --progress-bar -o "$tmp/$name" "$base/$name"
curl -fsSL -o "$tmp/$name.sha256" "$base/$name.sha256"

expected=$(cut -d" " -f1 "$tmp/$name.sha256")
actual=$(shasum -a 256 "$tmp/$name" | cut -d" " -f1)
if [ "$expected" != "$actual" ]; then
  echo "SHA-256 mismatch, aborting. Expected $expected, got $actual." >&2
  exit 1
fi

ditto -x -k "$tmp/$name" "$tmp/out"
app=$(find "$tmp/out" -maxdepth 2 -name 'UWUVCI.app' | head -1)
[ -n "$app" ] || { echo "UWUVCI.app was not found in the download." >&2; exit 1; }

rm -rf "$dest/UWUVCI.app"
ditto "$app" "$dest/UWUVCI.app"
xattr -dr com.apple.quarantine "$dest/UWUVCI.app" 2>/dev/null || true

echo "Installed: $dest/UWUVCI.app"
