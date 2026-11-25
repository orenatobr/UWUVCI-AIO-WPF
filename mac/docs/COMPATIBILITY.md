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
| Banjo-Tooie on the F-Zero X base (community ini, `BackupType = 3`, EEPROM) | In Cemu it stays on its first scene (a hung EEPROM access, see the section at the end). On a Wii U the community reports it working with this ini. |
| Banjo-Tooie on the F-Zero X base with `BackupType = 1` (SRAM) or `2` (Flash) | **Works in Cemu**: title screen, intro cutscene and gameplay (Banjo and Kazooie on the path, swimming), checked with captures. The game prints NO CONTROLLER because no analog stick is mapped in Cemu. Saves are not EEPROM saves in this mode. |
| Same, with 20 ini variants | TrueBoot, UseTimer, RetraceByVsync (all combinations), RamSize 8 MB, backup type, Rumble, RSPMultiCore, AIUseTimer, Cmp and Sound options: all identical, so the ini is not the cause. |
| Banjo-Kazooie on the F-Zero X base (control) | Works in the same Cemu: intro cutscene, title screen, and START enters the game. This shows the Cemu setup, the interposer and the key sender are sound, and that the Banjo-Tooie hang is specific to that game. |
| ROM integrity | Banjo-Tooie and Conker header CRCs verify (CIC 6105), so the dumps are not the cause. |
| Banjo-Tooie on the DK64 US base | Black screen after 4 min while shaders compile; not confirmed either way |
| Conker on the DK64 US base | Shows the DK64 base's Virtual Console info screen, not Conker; not confirmed |
| Jet Force Gemini on the DK64 EU base | Runs at 9 to 25 FPS but the picture stays black (the community table also lists it as "only sound, no picture", and Conker as "black screen" or "no picture" on every host). 13 ini variants (TrueBoot, UseTimer, RetraceByVsync, filter, copy, flush, RSP skip, RamSize, MemPak, AI timer) all stayed black. Unresolved; the cause is not known. |
| GoldenEye 007, Turok | Not run on any path: there is no GoldenEye or Turok ROM on the development machine (searched the home folder), and ROMs cannot be downloaded. Drop the ROM next to the others and run `tools/vc-play.py` to test. The community table lists GoldenEye US as "corrupted textures, crashes" on the DK64 host and Turok Dinosaur Hunter as "glitchy, freezes". |

Nothing above is "100% compatible". No game has been played past its first screens on the VC engine yet.

Pipeline cost on Apple Silicon: layout plus pack of a 53 MB base takes about 1.5 s of wall time, so there is no hot path worth tuning. The startup gain comes from ReadyToRun arm64 precompilation (1.85 s to 1.24 s).

Title keys: five keys from the public database at utik.91wii.com were saved into the user's settings.json (mode 600) after the user pointed to that site. They are checked against the SHA-1 list before use and are never printed or committed.

## Debugging notes for the Banjo-Tooie hang (2026-11-24)

What is known:

* **Reference run in ares (accurate emulator).** After a loading puzzle piece, Banjo-Tooie shows the Dolby legal screen over a rock and grass hillside, with the legal text drawn on top, then moves on. In Cemu the VC engine reaches that same hillside 12 s after start, draws it **without the text and without the loading puzzle**, and never moves on. So the game is frozen at the very first frames of that screen. The 3D background (one graphics task) is drawn; the 2D text (a later task) is not, and the game seems to wait for it. This is a hypothesis, not proven: a graphics microcode the VC engine does not handle would explain it, and would also explain Jet Force Gemini and Conker (no picture at all).
* **Cemu's GDB stub works.** `cemu --enable-gdbstub -g <rpx>` listens on port 1337 and halts at start. `c` continues, a raw `0x03` byte halts a running title, `qfThreadInfo` lists threads, `Hg<id>` then `p40` / `p43` read PC and LR, `m<addr>,<len>` reads guest memory (about 1 MB/s). Reply parsing must read raw bytes; an earlier attempt wrongly concluded that the stub ignored the interrupt. Clients: `tools/gdb2.py`, `tools/gdb-sample.py`.
* All guest threads are parked in the OS in the same places for Banjo-Tooie and for Banjo-Kazooie (which works), so thread states alone say nothing.
* The N64 RDRAM could not be located. The readable guest ranges are 0x01800000-0x01C00000, 0x02000000-0x50000000, 0xE0000000-0xE4000000, 0xE8000000-0xEA000000, 0xF4000000-0xFA000000 and 0xFFC00000-0xFFD00000. A search for the ROM boot code (three byte orders) and a scan for MIPS function prologues found nothing, and the same scan also finds nothing for Banjo-Kazooie, so the method is not able to see the emulated RAM. `lldb` attach needs a macOS Developer Tools approval that only the user can give.

What would continue this work: a way to see the N64 RDRAM and program counter of the running title (an `lldb` session once approved, or a breakpoint at the VC engine's display list entry through the stub), then use the ini `[BreakBlockInst]` / `[Cheat]` hooks or a ROM patch for the instruction that blocks. Until then none of Banjo-Tooie, Conker or Jet Force Gemini works on the Virtual Console path, and GoldenEye 007 and Turok were not run.

## Status of the other goals (2026-11-24)

* **Apple Silicon build.** The app is a self-contained `osx-arm64` build with ReadyToRun precompilation (window in about 1.24 s instead of 1.85 s, median of 3 launches on an M1 Pro). Packing a 53 MB base takes about 1.5 s, so there is no other hot path to tune. No GPU or Metal specific work was done.
* **UI.** The window was reviewed against the `/ui-guidelines` Avalonia checklist (accessibility, keyboard, focus, dark mode). Findings: accessible names are present on icon buttons, inputs and tiles; visible focus styles exist on the custom buttons; the one gap was the keyboard, which is now fixed with macOS shortcuts (Back ⌘←, Continue and Inject ⌘↩, Settings ⌘,) and a test. Contrast was measured from the theme tokens: every text pair is at least 5.07:1 in both themes (WCAG AA needs 4.5:1). The card border is only 1.45:1 (light) and 1.33:1 (dark); it is decorative, since inputs and buttons carry their own borders.

## More negative results and what they mean for a real Wii U (2026-11-24)

* **ROM size is not the cause.** The three failing games have ROMs larger than 16 MB (32, 32 and 64 MB). Banjo-Kazooie padded with zeros to 32 MB runs exactly like the 16 MB version on the same base.
* **It is not just slow.** Banjo-Tooie left running for 28 minutes (START pressed every 30 s) shows the same scene; only the water texture changes.
* **The community table describes real Wii U hardware**, not Cemu (its notes mention Loadiine and GX2). For Banjo-Tooie (Europe) on the F-Zero X US base it lists "works with ini" using exactly the ini this app writes, with a known crash at one spot in the Terrydactyland train cutscene. So the Banjo-Tooie freeze seen here may be specific to Cemu. This cannot be confirmed without a Wii U, so the title the app builds follows the documented recipe, and the Cemu result is reported as a Cemu result. Conker and Jet Force Gemini are listed as not working on real hardware too (no picture), so no known-good recipe exists for them.

The full list of ini keys the engine reads, and what was tried with the graphics task keys, is in `VC_INI_KEYS.md`.

A scan of 1.3 GB of readable guest memory for the code the engine's JIT emits to reach the N64 RAM (`lis r27,0x1337; ori r27,r27,0x2760`) found no match in 29 minutes, so that route to the RDRAM base is also closed with the stub alone.

## Reading the emulated N64 with lldb (2026-11-25)

The macOS Developer Tools approval was given, so `lldb` can attach to the Cemu copy. `tools/lldump.py` dumps the guest memory (about 1.4 GB, 80 s) and `tools/n64-state.py` reads the emulated N64 out of the dump. What this showed for Banjo-Tooie (F-Zero X base, hung on the first scene):

* **Where the RDRAM is.** The engine global at guest 0x10054490 holds the host pointer of the N64 RDRAM (0xF546A014 in that run, inside the 0xF4000000 pool that the GDB stub could not read) and 0x100541a4 holds its size, 8 MB. N64 address 0x80000000+X is RDRAM byte X.
* **The N64 CPU is idle.** Its registers (guest 0x132E6CD0) show it inside libultra's boot thread idle loop (`b .` at 0x80013620). Eleven libultra threads exist; all but the idle one are WAITING in `osRecvMesg` (saved PC 0x8002d778) with empty message queues.
* **Graphics and audio keep running.** The pending graphics task uses microcode F3DEX2 2.08 (string `RSP Gfx ucode F3DEX.NoN fifo 2.08`) with ordinary F3DEX2 opcodes, and its fields change between two dumps, so the scheduler is alive and the picture keeps being redrawn.
* **Who waits for what.** The game logic thread (id 2, priority 35) runs a frame and then blocks on queue 0x80080e30, which is signaled by the video thread (id 6, priority 127) through the helper at 0x80013450, only when the flag at 0x80042ab0 is 1 (it is 1). The video thread is blocked at 0x80015178 inside its frame pacing loop, waiting for a vertical retrace message on queue 0x80083470, which stays empty. The event table (0x8008a1f0) routes COUNTER and VI to the VI manager queue 0x8008b420, SP and DP to the scheduler queue 0x80082800, and SI to the queue 0x80083f70.

Two dumps taken 11.5 minutes apart show the same threads waiting on the same queues, with the read index of the retrace, scheduler and other queues advanced, so messages do flow. The ring buffers wrap, so the index alone does not give a rate. Banjo-Tooie is therefore not spinning and not blocked on an event that never arrives; what is still unknown is how fast the emulated N64 advances (a very slow retrace rate would look exactly like a freeze for 28 minutes) and what the game logic is waiting for after its frame. Next steps: find a monotonic counter (the libultra retrace count or the game's frame counter) and read it in two dumps to get the real rate, then compare with Banjo-Kazooie. This is a diagnosis in progress, not a fix.

### Banjo-Tooie: the frame loop runs, the game state does not advance (lldb sampling, 2026-11-25)

An earlier version of this section said the video thread was stuck in its frame pacing loop. That was wrong: two static dumps had caught the counter at the same value by coincidence. Live sampling (`tools/lsample.py`, about 0.3 s per sample, 24 samples) shows the opposite:

* The retrace event queue of thread id 0 (0x80083410) and the video thread queue (0x80083470) each receive about 15 messages per second. The VI contexts are consistent (retraceCount 1, current and next framebuffer equal).
* Thread id 0 increments the counter at 0x80083550 for every retrace message, and the video thread resets it to 0 at the end of each frame (loop at 0x80015164 waits for it to reach 2). The sampled value alternates between 0 and 1. So a frame completes about every 2 retraces, roughly 8 frames per second, against 25 frames per second that a PAL title should run (retrace 50 per second, so the emulated N64 runs at about a third of real speed here).
* The graphics lists are the same 2006 standard F3DEX2 commands in both slots, with no TEXRECT, and the audio tasks keep cycling.

So Banjo-Tooie is not blocked in its pacing loop. The game is running frames, slowly, but its own state machine stays on the first scene for at least 28 minutes, and the Dolby legal text that comes later is never drawn. The wait is therefore at a higher level (a flag or message that the first scene needs, for example the end of a load) and has not been found. Loader style threads (ids 3, 4 and 8) and the PI manager are all idle with empty queues. Next steps: find the game's state variable (compare the RDRAM of the same game in ares at the same point) and see what flag the first scene waits for.

## Display stretch and a test contamination found (2026-11-25)

* **Stretch to fill the screen.** The VC frame layout holds the game picture in a `frame` pane that is 1440 x 1080 (4:3) in a 1920 x 1080 layout, which leaves black bars at the sides on a 16:9 output. The widescreen patch (`N64FrameLayoutPatcher`, as in the original UWUVCI) sets that pane to 1920 x 1080. It was only reachable from the app and had no test; it now has tests on a synthetic layout, and `vc-layout` and `vc-pack` accept `--widescreen` and `--no-dark-filter`. Measured in Cemu with Banjo-Tooie on the real F-Zero X base: the picture covers 71% of the window width without it and 95% with it (the side bars shrink from 733 px to 125 px; the rest is the picture's own side crop). Only 16:9 is covered; a 4:3 window shows the 4:3 picture without bars already.
* **Stale saves contaminated the earlier ini tests.** The engine creates `Ucfze0.242.sav` in the title's save folder at the first save access (after about 1 to 2 minutes). It is 131072 bytes with `BackupType = 3` and 512 bytes with `BackupType = 0` (automatic). The earlier ini matrices reused whatever save the previous variant had left. Re-running the unmodified recipe and the automatic type with the save deleted first still leaves Banjo-Tooie on its first scene for the full run (about 5 minutes each), so the freeze does not come from the save either. Delete `mlc01/usr/save/00050002/<title id>/user/80000001/*.sav` between variants when testing.

## Banjo-Tooie runs in Cemu with SRAM or Flash saves (2026-11-25)

With the save deleted before each variant, `BackupType = 1` (SRAM) and `BackupType = 2` (Flash) take Banjo-Tooie past its first scene (a 46 to 47% change between captures, then the title screen and the game), while `BackupType = 3` (EEPROM, any size: 512, 2048, 4K, 16K) and `0` (automatic) stay stuck. The earlier ini searches never tried SRAM or Flash for this game, and their results were also contaminated by saves left by the previous variant. The lldb data fits: the SI thread waits for an event that never arrives, and EEPROM access goes through the SI, while SRAM and Flash use the PI DMA. So the EEPROM path of the engine hangs under Cemu; the community table reports the EEPROM ini working on a real Wii U, so the default profile is unchanged.

* `vc-layout` and `vc-pack` take `--save-type auto|sram|flash|eeprom`, and the Options page has a Save hardware choice (Profile, Automatic, SRAM, Flash, EEPROM). Profile follows the game database. Use SRAM to test in Cemu.
* Verified end to end in Cemu: `vc-layout ... --save-type sram --widescreen --no-dark-filter` on the F-Zero X base gives a title that reaches the title screen and gameplay, with the picture filling 95% of the window width.
* Conker (EEPROM 16K) and Jet Force Gemini (Flash) have not been retried with this finding yet.

## Banjo-Tooie (EU): the picture was drawn 5% short of the frame (2026-11-25)

With the widescreen patch the picture stopped at 95% of the window width, with a black bar on the right (the same 5% shortfall exists in the 4:3 mode, where the picture is also pushed to the left). Adding `[Render]` `ClipRight = 55` to the ini, the value the community uses to centre Banjo-Kazooie, made the picture fill 100% of the width in Cemu (28 gave 99%, the Banjo-Kazooie set of four clip values did nothing). The profile of Banjo-Tooie (Europe) now writes it. The US version was not measured and does not get it.

Observed while testing: after a fresh start Banjo-Tooie spends a while on its first scene before the title screen (up to a few minutes, longer when other Cemu windows run at the same time), and then plays. The game prints NO CONTROLLER because the Cemu keyboard profile maps no analog stick, no ZL or ZR and no HOME; a controller profile is the next step.

## Controller, accessories and the lldb limits (2026-11-25)

* **Banjo-Tooie never sees the controller.** In every run the game shows NO CONTROLLER (title, cutscenes, gameplay), ignores START and all the keys, and plays its intro and attract sequence by itself. Keys do reach the emulator: the Banjo-Kazooie title started the game with START in the same Cemu. So the cause is not the Cemu keyboard profile. I added ZL (Z), ZR (V) and Home (H) to the profile and a hypothesis for the stick (GamePad button ids go from 1 to 19, so the left stick axes would be 20 to 23 and the right stick 24 to 27); it could not be checked, because the game does not read the pad at all.
* **Accessory variants (title screen frames):** with the default Rumble Pak and with no Rumble Pak the game reaches the title and shows NO CONTROLLER in both. With `MemPak = 1` or `NoCntPak = 1` it stays on its first scene for the whole run (68 s), like the EEPROM save did. So any SI based peripheral access (EEPROM, Controller Pak) hangs the engine under Cemu, which fits the SI thread waiting for an event that never arrives.
* **Memory probes.** `lldb` attach takes about 45 s and fails or crashes before about 60 s of run time (a race in lldb while stopping and continuing in asynchronous mode). Loops of stop and continue also crash it. What works: one `lldb` per sample, after 60 s, reading the RDRAM in 64 KB pieces (`tools/dumpfull.py`, `tools/ctrl-probe.py`). The GDB stub is not reliable for game state: `c` does not seem to resume the PPC threads, and it reads zeros where `lldb` reads the real values. My earlier thread findings from the stub should be read with that in mind.
* Looking for the pad structure in the RDRAM (the A button bit 0x8000 while J was held) only found pointer lists, so the structure was not located.

Next steps: try the Wii U Pro Controller type in Cemu instead of the GamePad (the VC has separate handling for the GamePad, Classic and Pro types), and launch the title under `lldb` from the start to read the SI state in the first scene.

Update: the same input test with the emulated controller set to the Wii U Pro Controller type instead of the GamePad gives the same result (NO CONTROLLER in every frame, START and the other keys ignored, the intro plays by itself), so the controller type is not the cause either. Not tried yet: launching the title under `lldb` from the start to read the SI state in the first scene, and checking the Banjo-Tooie ini on a real Wii U, where the community table reports the game working.

## Reliable testing, and what the Banjo-Tooie ini keys do (2026-11-25, afternoon)

**Use a direct launch for hang tests.** The same title that reaches the title screen at about 30 s when Cemu is started directly (`tools/repeat-launch.py`, `tools/ini-variants.py`: plain `Popen`, output to /dev/null, nothing else touching the window) stayed on its first scene for the whole run when started through my earlier `vc-play.py` and `vc-matrix2.py` harness. I could not find which detail of that harness causes it (bringing the window to the front does not, and a plain copy of the folder does not). Results taken with that harness that are only about hangs should be treated as unreliable; the ones that were repeated with a direct launch are listed below.

Confirmed with a direct launch:

* EEPROM save (`BackupType = 3`): stuck on the first scene in 5 of 5 runs (79 s each). SRAM (`BackupType = 1`): the title screen at about 26 to 37 s and the game by about 62 s, in every run seen (with a fresh save and with the save of a previous run alike).
* Keys that make the game stay on the first scene when present: `TimeIntDelay`, `AIUseTimer`, `BootPCChange = 0`, `PlayerNum` (any value, including the default 4), `SIDelay` (any value, including 0, in `[RomOption]`; in `[RSPG]` it does nothing), `MemPak = 1`, `NoCntPak = 1` and removing `TrueBoot`. `TrueBoot = 1` is required. No effect on the result: `RetraceByVsync = 0` with `UseTimer = 1`, removing `[Input]` `StickModify`, `Rumble` on or off.
* The Banjo-Tooie profile is therefore close to the only working set of keys, which matches the community recipe.

**NO CONTROLLER.** None of the variants above removes it. The game's controller command block (the libultra PIF RAM at 0x8008B48C: four `FF 01 04 01` read commands followed by `FE`) is identical in RDRAM dumps taken with no key and with 15 different keys held, and its data bytes stay zero. (Retracted on 2026-11-25: a full RDRAM dump of F-Zero X, which does react to the keys, shows the same block at 0x800F9C50 with the same zero data bytes, so the zero block does not tell the two games apart and is not evidence that the SI response is missing.) The same keys do reach other titles (F-Zero X). The dark translucent box seen behind the characters is drawn behind the NO CONTROLLER text (it moves with it), and none of ten `[Render]` options changes it, so it most likely goes away with the controller problem.

Framing: the first scene shows a thin black bar on the right and the title and cutscenes a wider band at the top than at the bottom; neither was changed by `ClipRight = 55` alone, and `ClipTop` and `ClipBottom` have still not been measured on a title or cutscene frame.

## Conker and Jet Force Gemini with SRAM/Flash, and what the engine log says (2026-11-25)

Both built with `vc-layout --save-type` on the F-Zero X US base (EU ROMs) and launched directly in Cemu 90 s each; the engine's own console lines are in Cemu's `log.txt` (`[OSConsole]`).

* **Jet Force Gemini (SRAM and Flash alike):** now draws the Rare logo, then a dark scene with a small pak icon in the corner (before: no picture at all). The log holds about 208,000 `undefined microcode G1_RESERVED1/G1_RESERVED2` lines (136,787 and 71,894), with commands such as `0x05110020` and `0x07070038`. Opcodes 0x05 and 0x07 are TRI1 and QUAD in F3DEX2, so the engine is reading the game's display lists with its generation 1 (F3DEX1) parser, which treats them as reserved and draws no geometry. The engine data holds the names `G2_VTX`, `G2_QUAD`, `RUT_S2DEX1` and `RUT_S2DEX2` and parsers for F3DEX1, S2DEX1 and S2DEX2 only; no ini key selects the microcode. Fixing it means making the engine choose the generation 2 path for this game (a code patch of VESSEL.rpx), or finding out how it identifies the microcode.
* **Conker (SRAM):** stays on the Virtual Console card for the whole run. The engine reads the 64 MB ROM and its ini (`g_RomId 0x4e465550`), logs no microcode errors, and then prints one line per frame with `frameinstct = 0 frameblockct = 1 g_nN64CPUCmpCheckCt 1` (2,345 frames in 50 s), so the emulated CPU is running one block per frame: most likely a loop waiting for something (the same shape as the controller wait in Banjo-Tooie). The real save type of the game is a 16 Kbit EEPROM; EEPROM hangs Banjo-Tooie in Cemu, SRAM was used here as the Cemu workaround.
* The Banjo-Tooie title picture has a 12.5% band at the top and 1.2% at the bottom, and `ClipTop`/`ClipBottom` (15, 30, 47) change neither.

## Where the engine picks the microcode (VESSEL.rpx, guest addresses, 2026-11-25)

Found by scanning the code dump for the loads of the `undefined microcode` and `RUT_S2DEX*` strings (the strings sit above 0x10008000, so the loads are `lis 0x1001` plus a negative `addi`).

* `0x2069ac4`: the task start routine. It keeps a list of known microcodes keyed by the RSP task's code and data addresses (`r24`, `r26`). A hit copies the type from the entry (`+0x60`) into the object at `+0x203c` (store at `0x2069b38`).
* A miss (`0x2069bbc`) sums the 64-bit words of the code and data into `r30:r31` and compares that checksum against a chain of constants (the first ones start at `0x2069cb8`); the result goes to `+0x203c` at `0x206a52c`.
* `0x2069a68`: reads `+0x203c`; types 9 and 10 take one path, all others the other. `0x2058310` reads the same field and treats 7 as S2DEX1. The F3DEX1 parser (`0x20570b8` prints `undefined microcode G1_RESERVED1`) handles the generation 1 opcodes.
* Jet Force Gemini's microcode is not in that chain, so it falls to the default, which is the F3DEX1 path.
* A patch has to make `0x206a52c` store the type that selects the generation 2 parser for this game's checksum, or add the game's checksum to the chain. The type numbers for F3DEX2 are not yet known: next step is to log `+0x203c` for a game that is known to use F3DEX2 and compare. A patched `VESSEL.rpx` is needed for a real Wii U; a Cemu graphic pack patch of the same instruction is enough to test the idea in Cemu first.

## Negative results and discarded evidence (2026-11-25)

* **JFG microcode type test (does not work).** A Cemu graphic pack patch (`moduleMatches = 0xC0C21308`, `0x0206A314 = li r0, 9`, the default type written at `+0x203c` for unknown microcode) was applied (the log confirms the group) and changed nothing: still about 180,000 `undefined microcode` lines and the same dark scene. The field that selects the parser is probably `+0x2044` (the 5 and 7 families set by the checksum chain), not `+0x203c`. Do not repeat this patch; the next try is to change what the chain stores in `+0x2044`.
* **Discarded: every result taken with `vc-play.py` and `vc-matrix2.py`** (the old test harness). Launched through it, the same Banjo-Tooie title stayed on its first scene while a direct launch reached the title in about 30 s. Only the results repeated with `tools/repeat-launch.py` and `tools/ini-variants.py` are kept; they are in the section "Reliable testing" above.
* **Banjo-Tooie is not at 100% in Cemu.** Open: NO CONTROLLER and no input, the vertical band (12.5% top, 1.2% bottom, not changed by `ClipTop` or `ClipBottom`), and the dark box behind the NO CONTROLLER text. The community table lists the game as working on a real Wii U, so the controller failure may be specific to Cemu's handling of this engine; it has not been tested on hardware.

## Vertical bands are not specific to Banjo-Tooie (2026-11-25, direct launch)

The F-Zero X base (a working game, launched directly in Cemu for 60 s, no input) shows the same uneven bands as Banjo-Tooie on its title screen and a balanced picture in the race:

| Scene | Top band | Bottom band | Window body |
| --- | --- | --- | --- |
| F-Zero X title (twice, 17 s and 57 s) | 173 px | 47 px | 1,422 px |
| F-Zero X race (30 s and 43 s) | 76 px | 95 px | 1,422 px |
| Banjo-Tooie title | 178 px | 17 px | about 1,420 px |

So the offset follows the scene (the picture's own vertical position in the frame), not this game's ini: `ClipTop` and `ClipBottom` leave it unchanged, and a game that is known to work has it too. Treat it as an engine and frame layout effect to compare on a real Wii U, not as a Banjo-Tooie defect.

The saved N64 CPU register block in the controller dumps (`ctrl/idle.bin`, read at engine address `0x132E6CD0`) is all zeros, so those dumps do not show where the game's CPU is waiting; the dump script's register address or layout is wrong.

## Retraction: the PIF command block does not explain NO CONTROLLER (2026-11-25)

Full 8 MB RDRAM dumps, taken once with lldb after 68 s, of F-Zero X (reacts to input) and Banjo-Tooie (NO CONTROLLER): both hold four `FF 01 04 01` read commands followed by `FE` with all data bytes zero (F-Zero X at `0x800F9C50`, Banjo-Tooie at `0x8008B490`). The earlier reading of that block as a missing SI response was wrong. What differs between the two games is still unknown; the next comparison is the pad status the game keeps after its read (the `OSContPad` array and `osContStartQuery` result), found by following the pointer the game passes to `osContGetReadData` in each dump.

## NO CONTROLLER: port 1 answers with CONT_NO_RESPONSE_ERROR (2026-11-25, full RDRAM dumps)

Searching both full dumps for the libultra `OSContStatus` array (4 entries of `type u16, status u8, errno u8`, written by `osContStartQuery`) separates the two games:

| Game | Array | Entries |
| --- | --- | --- |
| F-Zero X (reacts to input) | `0x800DCE70` | `00 05 01 00` four times: all four ports normal controllers, `errno 0` |
| Banjo-Tooie (NO CONTROLLER) | `0x80083FA0` | `00 05 01 08`, then `00 05 01 00` three times: port 1 has `errno 0x08`, ports 2 to 4 are fine |

`0x08` is `CONT_NO_RESPONSE_ERROR` in `os_cont.h`. So the game's own query result says that player 1 gave no response while the other three ports did, which is exactly what the NO CONTROLLER screen reports. The cause is therefore in what the engine's emulated PIF returns for port 1 in the query for this game (the zero PIF command block is the same in both games and is not the difference). Next: find which engine setting changes the status of port 1 alone (the pak selection at `0x204b280`, the device initialisation in `trlEmuShellInputInitDevice`, or the per game ini), and check the array after a variant that boots, one at a time with the dump.

Variants tested with `tools/port1-errno.py` (direct launch, one full RDRAM dump after 68 s each, F-Zero X is the working reference): the base SRAM Banjo-Tooie title and the same title with `Rumble = 0` give the identical array at `0x80083FA0` (`00 05 01 08` for port 1, ports 2 to 4 normal), so the rumble pak setting does not change the status of port 1. The variants that hang the first scene (`MemPak`, `NoCntPak`, `PlayerNum`, `SIDelay`, `TimeIntDelay`) cannot be read this way because the game never reaches the query. The Apple Silicon work (the port to Avalonia and .NET 10 arm64, the wizard UI built with the /ui-guidelines checks, the `--widescreen`, `--save-type` and EU clip options) is already in main since v1.4.1; this round shipped no new code.

## Port 1 status: two more variants and how to read the array (2026-11-25)

* `delrumble` (the `Rumble` line removed, so the engine picks the pak) and `tpak` (`Rumble = 0`, `TPak = 1`) give the same array as the base title: `00 05 01 08` for port 1, ports 2 to 4 normal. No pak setting changes the status of port 1.
* How to read the entry (hypothesis, not yet checked against a second dump): in libultra `type` and `status` are only written when the port answers, and `errno` is taken from the answer's error bits (`0x08` means no response). So the `00 05 01` in port 1's entry is probably left over from an earlier query that worked, and the `0x08` comes from a later one. Banjo-Tooie repeats the query while NO CONTROLLER is shown; F-Zero X does not need to. To check: dump twice, early (before the NO CONTROLLER text) and late, and see whether the `errno` of port 1 changes from 0 to 8.
* GoldenEye 007 and Turok cannot be tried on this machine: the ROMs are not here (only Banjo-Tooie, Conker, Jet Force Gemini, Banjo-Kazooie, Majora's Mask and Pokemon Stadium 2 were found).
* Early and late dump (`tools/port1-early-late.py`, same title, dumps about 60 s and 85 s after launch): both show `00 05 01 08` for port 1. The hypothesis above is not confirmed or refuted: the status was already `errno 0x08` before the first dump, and lldb cannot attach earlier than about 45 to 60 s. Seeing the change from 0 to 8 would need a dump from the first seconds (for example a Cemu debugger breakpoint, or a graphic pack patch that logs the write).
