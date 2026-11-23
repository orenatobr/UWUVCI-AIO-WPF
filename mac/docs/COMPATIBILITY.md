# N64 compatibility

Two engines can run an injected N64 game. This page says what was actually run and what was not.

## Engines

**Nintendo Virtual Console (VC).** The emulator inside the official N64 titles. UWUVCI swaps the ROM and
writes a per game ini. The upstream list rates Conker, Jet Force Gemini, Turok 2, Turok 3 and Rage Wars as
not working, and GoldenEye and Banjo-Tooie as having issues. This build picks the ini automatically
(save type, rumble or controller pak, timing flags) from the ROM checksum and from the community inis in
UWUVCI-IMAGES. It was **not run** here: it needs an official base title and its title key, which this
project does not ship and the author did not have.

**Native (not64).** The not64-wiiu emulator packed as its own title, with the ROM on the SD card
(`sd:/not64/roms` and `sd:/not64/autoboot.txt`). It does not depend on the VC emulator, so it does not
inherit its limits. It needs only the Wii U common key to encrypt the title.

Both engines are selectable in the app on the Options step. The native choice in the UI was only checked with a fake service and headless renders. The results table below comes from `uwuvci-cli native-layout`, not from the UI.

## What was run

Titles were built by `uwuvci-cli native-layout` from the real ROMs and started in Cemu 2.6 on an Apple
M series Mac. Only the first minutes were watched. Nobody played through a level.

The FPS figures below are the counter in the Cemu window title, which is how fast the host presents frames.
They are not the game's own speed. The not64 accuracy log (`docs/ACCURACY.md` in not64-wiiu) measures speed in
VI per second and recorded Banjo-Tooie at 50 in game. The two numbers were not compared in this work.

| Game | Result in Cemu (native engine) |
|---|---|
| Conker's Bad Fur Day (EU) | Boots to the "For mature audiences only" screen, about 25 FPS |
| Jet Force Gemini (US) | Boots to the title screen, about 25 FPS |
| Banjo-Tooie (EU) | Runs the intro. The one frame captured showed a mostly dark scene with only some geometry visible. In a later 4 minute run the window counter ranged from 0.85 to 16.6 FPS, but no valid screenshots of that run exist. Not investigated further |
| GoldenEye 007 | Not tested, no ROM available |
| Turok, Turok 2, Turok 3, Rage Wars | Not tested, no ROM available |

None of this is "fully compatible". Speed is below full rate and the emulator is an interpreter.
Real hardware was not used.

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
* **Speed and scenery on the native engine:** one Banjo-Tooie frame looked incomplete and the window counter
  read about 10 FPS. Whether that is a real rendering problem or a dark intro scene was not established. Any
  fix would be emulator work in not64-wiiu, not in this app.
* **Real hardware:** every run so far is in Cemu.
