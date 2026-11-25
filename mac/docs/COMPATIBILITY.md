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
| Banjo-Tooie on the F-Zero X base (community ini) | Boots and draws a textured scene (the engine counts 50 frames/s, so it runs at full speed), but the scene never changes and no input is accepted. Tested 7 minutes with START/A, and 200 s cycling every mapped button and the D-pad. The earlier note that START changed the picture was wrong: that comparison used captures of different sizes. **Not playable.** |
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

### Banjo-Tooie: the frame pacing loop (lldb sampling, 2026-11-25)

Live sampling (`tools/lsample.py`, about 0.4 s per sample) of the hung Banjo-Tooie shows:

* The retrace message queue of the video thread (0x80083470) receives about 18 messages per second, so the emulated VI does deliver retraces.
* The graphics display lists alternate between two slots (0x80164840 and 0x8016bf10) that hold the same frame: 2006 standard F3DEX2 commands (VTX, TRI1, TRI2, LOADBLOCK, LOADTLUT, SETTIMG, FILLRECT...) with no TEXRECT and nothing unusual. The game keeps submitting this scene, and the audio tasks keep cycling.
* The video thread (id 6) sits in a pacing loop at 0x80015164: `while (*(0x80083550) < *(0x80083554) - *(0x80083b1c)) recv(retrace queue)`. The values never change: `*(0x80083550) = 1`, `*(0x80083554) = 2` (set to 2 by the setter at 0x800150ac, the number of frames allowed in flight) and `*(0x80083b1c) = 0`, so the loop needs `*(0x80083550)` to reach 2 and never leaves. `0x80083b1c` is only written by this same thread, after the loop.
* Nothing in the code takes the address 0x80083550 besides this function, so whatever raises it to 2 uses a pointer. The VI contexts are consistent (current and next framebuffer equal, retraceCount 1), so the framebuffer swap is not the blocker.

The game logic thread only runs when this loop exits, so the Dolby legal text (a later part of the frame) never gets drawn. What increments `*(0x80083550)` is the open question; it is a game-side counter, so the next step is a write watch on that word (an lldb watchpoint, if Rosetta allows one) or a scan for code that stores through a pointer to 0x80083550 to see which event or message it belongs to.
