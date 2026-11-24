#!/bin/bash
# Runs a title in Cemu and captures ONLY the Cemu window at the given times, so a test leaves evidence.
#
#   mac/tools/cemu-test.sh <path/to/app.rpx> <out-dir> <seconds> [<seconds> ...]
#
# Example for a Virtual Console inject (see mac/docs/TESTING_VC.md):
#   uwuvci-cli vc-layout game.z64 "/path/to/F-Zero X [US]" "My Game" out
#   mac/tools/cemu-test.sh out/base/code/*.rpx shots 30 60 120
#
# Needs Cemu (CEMU=/path/to/Cemu if it is not in /Applications) and swiftc. It stops only the Cemu it started,
# keeps the display awake while it runs, and writes <out-dir>/t<seconds>.png plus the window title (frame rate).
set -euo pipefail

rpx="${1:?path to the .rpx}"; out="${2:?output folder}"; shift 2
[ "$#" -ge 1 ] || { echo "give at least one capture time in seconds" >&2; exit 2; }
cemu="${CEMU:-/Applications/Cemu.app/Contents/MacOS/Cemu}"
[ -x "$cemu" ] || { echo "Cemu not found at $cemu (set CEMU=...)" >&2; exit 2; }

here="$(cd "$(dirname "$0")" && pwd)"
helper="$out/.winid"
mkdir -p "$out"
[ -x "$helper" ] || swiftc -O "$here/winid.swift" -o "$helper"

caffeinate -d -i -t $(( $(printf '%s\n' "$@" | sort -n | tail -1) + 30 )) &
keep=$!
"$cemu" -g "$rpx" >"$out/cemu.log" 2>&1 &
pid=$!
trap 'kill "$pid" "$keep" 2>/dev/null || true' EXIT

last=0
for t in "$@"; do
  sleep $(( t - last )); last=$t
  if wid=$("$helper" "$pid" 2>/dev/null); then
    screencapture -x -l "$wid" "$out/t$t.png"
    title=$(osascript -e "tell application \"System Events\" to get name of window 1 of (first process whose unix id is $pid)" 2>/dev/null || true)
    echo "t=${t}s captured | ${title:-no title}"
  else
    echo "t=${t}s no window (is the display asleep, or did Cemu exit?)"
  fi
done
