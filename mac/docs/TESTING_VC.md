# Testing a Virtual Console inject in Cemu

This is how to check a game on the Nintendo VC engine, which is the port this project is about. It has **not
been run yet**: it needs an official N64 base title, and none was available. Everything here is the procedure
that is ready for the day one is.

## You need

* An official N64 base title (for example F-Zero X, Mario Golf or Paper Mario) as a **decrypted folder** that has
  `code`, `content` and `meta`. Either copy the `bases/<name> [REGION]` folder that the Windows UWUVCI already
  downloaded, or download one in the app (choose the base in the dropdown; it asks for that base's title key
  once, plus the Wii U common key in Settings). The app ships no keys. The title key is checked against the SHA-1
  stored in the current UWUVCI base list, so a wrong or mistyped key is rejected before any download.
* A ROM you own, in any byte order (`.z64`, `.v64`, `.n64`).
* Cemu 2.6 and the repository built (`cd mac && dotnet build`).

## Steps

1. Inject into a copy of the base, unencrypted, in the layout Cemu reads:

   ```
   dotnet run --project src/UWUVCI.Cli -- detect game.z64          # shows the ini that will be used
   dotnet run --project src/UWUVCI.Cli -- vc-layout game.z64 "/path/to/Base [US]" "Game Name" out
   ```

   The base is not changed. The result is `out/base` with the ROM in `content/rom` and the ini in
   `content/config/<rom>.ini`. Add `--pak on` to try the Expansion Pak line (off by default, unverified).

2. Run it in Cemu and keep evidence of the window (frames and the title bar frame rate):

   ```
   tools/cemu-test.sh out/base/code/*.rpx out/shots 30 60 120
   ```

   It captures only the Cemu window, so nothing else on screen ends up in the images.

3. Tune the ini and repeat. The ini is `out/base/content/config/<rom>.ini`; edit it and run step 2 again. The
   keys known from the community and from the VCN64Config notes, none of them verified by this project:
   `UseTimer` (helps games that boot with sound but no picture), `RetraceByVsync` (turn it off if audio
   stutters), `TrueBoot`, `RamSize = 0x800000`, `BackupType` and `BackupSize`, `Rumble` or `MemPack`, and the
   `[Render]`, `[RSPG]` and `[Cmp]` sections. Per game starting points are in `N64Profiles.cs`.

4. When it works, make the installable title (needs your Wii U common key):

   ```
   dotnet run --project src/UWUVCI.Cli -- vc-pack game.z64 "/path/to/Base [US]" "Game Name" out --key <32 hex>
   ```

5. Write the result in `docs/COMPATIBILITY.md`: game, region, base used, ini, what you saw and for how long.
   "Booted" and "playable" are different results; say which.

## Notes

* Cemu can run an unencrypted title directly, so no encryption or install is needed for testing. That has been
  confirmed for the native engine's title; a VC base has not been tried.
* Nothing from the base, the ROM or the keys should be committed.
