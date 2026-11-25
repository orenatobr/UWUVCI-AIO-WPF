"""Records what Cemu plays for a title and reports its level per channel, so "no sound" can be checked without ears.

usage: audio_check.py <title folder or .rpx> [seconds]   (default 60)

Needs ffmpeg and a virtual audio device (BlackHole 2ch). The script points Cemu's TV output at BlackHole for the run
(Cemu's settings.xml is saved and restored), records the device with ffmpeg and prints, per 10 s window, the RMS of the
left and right channel, the clipped share and the left to right ratio. Silence prints an RMS of 0.000.
Measured on the F-Zero X base and a Banjo-Tooie title (Cemu 2.6, stereo): sound is present from about 6 s,
the right channel is always 1/3 of the left with a correlation of 0.98 to 1.00, so it is a pan of the engine's mix under
Cemu and not a Banjo-Tooie problem; with mono output the channels are equal.
"""
import array, os, re, shutil, subprocess, sys, time, wave

CEMU = os.environ.get("CEMU", os.path.expanduser("~/Applications/Cemu.app/Contents/MacOS/Cemu"))
SETTINGS = os.path.expanduser("~/Library/Application Support/Cemu/settings.xml")


def rpx_of(path: str) -> str:
    return path if path.endswith(".rpx") else next(
        os.path.join(path, "code", f) for f in os.listdir(os.path.join(path, "code")) if f.endswith(".rpx"))


def report(wav_path: str) -> None:
    with wave.open(wav_path) as w:
        sr, n, ch = w.getframerate(), w.getnframes(), w.getnchannels()
        data = array.array("h"); data.frombytes(w.readframes(n))
    step = 10 * sr * ch
    for start in range(0, len(data) - step + 1, step):
        seg = data[start:start + step]
        left, right = seg[0::ch], seg[1::ch] if ch > 1 else seg[0::ch]
        rms = lambda s: (sum(x * x for x in s) / max(len(s), 1)) ** 0.5 / 32768
        rl, rr = rms(left), rms(right)
        clipped = sum(1 for x in seg if abs(x) > 32100) / len(seg)
        ratio = rl / rr if rr > 1e-6 else float("inf")
        print(f"{start // (sr * ch):4d}-{start // (sr * ch) + 10:4d}s  L {rl:.3f}  R {rr:.3f}  L/R {ratio:5.2f}  clipped {100 * clipped:.2f}%")


def main() -> None:
    rpx = rpx_of(sys.argv[1]); seconds = int(sys.argv[2]) if len(sys.argv) > 2 else 60
    saved = open(SETTINGS).read()
    patched = re.sub(r"<TVDevice>.*?</TVDevice>", "<TVDevice>BlackHole2ch_UID</TVDevice>", saved)
    patched = re.sub(r"<TVVolume>\d+</TVVolume>", "<TVVolume>100</TVVolume>", patched)
    wav = "/tmp/uwuvci-audio-check.wav"
    try:
        open(SETTINGS, "w").write(patched)
        rec = subprocess.Popen(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-f", "avfoundation", "-i", ":BlackHole 2ch",
                                "-t", str(seconds), "-ac", "2", wav])
        cemu = subprocess.Popen([CEMU, "-g", rpx], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        rec.wait(); cemu.kill()
    finally:
        open(SETTINGS, "w").write(saved)
    report(wav)


if __name__ == "__main__":
    main()
