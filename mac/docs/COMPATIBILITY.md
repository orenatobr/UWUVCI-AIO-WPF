# N64 compatibility

Two engines can run an injected N64 game. This page says what was actually run and what was not.

## Engines

**Nintendo Virtual Console (VC) is the engine this project is about.** The requested deliverable is these
games ported through the VC injection, like the other UWUVCI games: the ROM goes into an official N64 base
title and runs on Nintendo's own emulator, configured by a per game ini. The upstream list rates Conker, Jet
Force Gemini, Turok 2, Turok 3 and Rage Wars as not working there, and GoldenEye and Banjo-Tooie as having
issues. This build picks the ini automatically (save type, rumble or controller pak, timing flags) from the ROM
checksum and from the community inis in UWUVCI-IMAGES. **It was never run here**: it needs an official base
title and its title key, which this project does not ship and which were not available on the machine that
built it. So none of the five games has been verified on this path.

**Native (not64) is an extra, experimental fallback, not the VC port.** It packs the not64-wiiu emulator as its
own title and keeps the ROM on the SD card. It does not depend on the VC emulator, so it does not inherit the
VC limits, but it is a different thing from what was asked and it was only used here because a VC test was not
possible. Everything under "What was run" below is about this fallback.

## What was run

Titles were built by `uwuvci-cli native-layout` from the real ROMs and started in Cemu 2.6 on an Apple
M series Mac. Only the first minutes were watched. Nobody played through a level.

The FPS figures are the counter in the Cemu window title, which is how fast the host presents frames. They are
not the game's own speed. The not64 accuracy log (`docs/ACCURACY.md` in not64-wiiu) measures speed in VI per
second and recorded Banjo-Tooie at 50 in game. The two numbers were not compared in this work.

**not64-wiiu 0.6.0 fixed a rendering bug** that the first runs hit. Texture scales and 8 bit colours were not
scaled (a `G_TEXTURE` scale of 0x8000 came out as 32768.0 instead of 0.5), so textured triangles sampled far
outside their textures. That drew a fixed grid of flat coloured squares over Banjo-Tooie's intro and made
surfaces noisy. The title the app builds downloads the latest not64 release, so new injects already carry the
fix. Details and the host test are in the not64-wiiu repository.

| Game | Result in Cemu (native engine) |
|---|---|
| Banjo-Tooie (EU) | Before 0.6.0: the intro showed a grid of flat coloured blocks. With 0.6.0 and a title built by this app: the intro terrain is textured and the title screen shows the logo and Banjo, window counter about 16 to 21 FPS. Nobody played further |
| Jet Force Gemini (US) | Reaches its title screen (before and after 0.6.0), now with the "press start" prompt and textured lighting in the frame captured with 0.6.0 |
| Conker's Bad Fur Day (EU) | Reaches its intro (the squirrel and the big N) and the main menu ("GAME 1", "Insert Rumble Pak"). The old emulator (0.5.2) reached the same menu in a control run but with broken textures (noise, a missing clock face, garbled walls); 0.6.0 shows them correctly. The earlier black frames were the game's loading phase, not a failure |
| GoldenEye 007 | Not tested, no ROM available |
| Turok, Turok 2, Turok 3, Rage Wars | Not tested, no ROM available |

None of this is "fully compatible". Speed is below full rate and the emulator is an interpreter. Real hardware
was not used.

## Choices in the VC profile that are guesses

* `RamSize = 0x800000` (Expansion Pak) is **off by default for every game**. No published ini sets this key
  and its effect on the VC emulator is unverified. The Options step has an Auto, On, Off switch so it can
  be tried on games that fail without it, such as Conker and Jet Force Gemini.
* The GoldenEye break block hack is copied from a Japanese ini and only applied to Japanese ROMs.
* Turok, Turok 2, Turok 3 and Rage Wars have no community ini. They get the save type and controller pak
  setting from the checksum database and nothing else.

## What blocks "fully compatible"

These need something only the owner can provide:

* **GoldenEye 007 and the Turok games:** a legally owned ROM of each, to run them in Cemu and write or fix
  their ini. Without it nothing here is known to work for them.
* **The official Virtual Console engine:** an official N64 base title and its title key, to test the ini
  profiles and the Expansion Pak switch for real.
* **Speed on the native engine:** the emulator interprets the N64 CPU and the recompiler is off, so heavy
  games run well under full speed. That is emulator work in not64-wiiu, not in this app. Conker's later
  frames in my runs were black and the cause was not found.
* **Real hardware:** every run so far is in Cemu.

## Tested vs untested (2026-11-24)

Official Virtual Console engine, run in Cemu 2.6 on an M1 Pro (see TESTING_VC.md for the Cemu workaround):

| Item | Result |
| --- | --- |
| Base download from the Nintendo CDN with real title keys (F-Zero X US, DK64 US/EU) | Works, seconds |
| `vc-layout` + `vc-pack` on a real DK64 US base with the Conker ROM | Produces an encrypted WUP (86 MB) in about 1.5 s, twice. The WUP itself was not installed or booted. |
| Unmodified F-Zero X base (control) | Runs at 46 to 59 FPS, 4-player race visible |
| Banjo-Tooie on the F-Zero X base | Boots and renders textured intro scenery (up to 25 FPS). START was pressed and the picture changed; menu and gameplay not confirmed. Six ini variants (TrueBoot, UseTimer, RetraceByVsync, RamSize) gave the same picture, so none of them is shown to matter. |
| Banjo-Tooie on the DK64 US base | Black screen after 4 min while shaders compile; not confirmed either way |
| Conker on the DK64 US base | Shows the DK64 base's Virtual Console info screen, not Conker; not confirmed |
| Jet Force Gemini on the DK64 EU base | Runs at 9 to 25 FPS but the picture stays black. 13 ini variants (TrueBoot, UseTimer, RetraceByVsync, filter, copy, flush, RSP skip, RamSize, MemPak, AI timer) all stayed black. Unresolved; the cause is not known. |
| GoldenEye 007, Turok | Not run on any path. Unresolved blocker: no VC result exists for them, and the user asked to focus on the other three first. |

Nothing above is "100% compatible". No game has been played past its first screens on the VC engine yet.

Pipeline cost on Apple Silicon: layout plus pack of a 53 MB base takes about 1.5 s of wall time, so there is no hot path worth tuning. The startup gain comes from ReadyToRun arm64 precompilation (1.85 s to 1.24 s).

Title keys: five keys from the public database at utik.91wii.com were saved into the user's settings.json (mode 600) after the user pointed to that site. They are checked against the SHA-1 list before use and are never printed or committed.
