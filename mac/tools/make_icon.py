"""Draws the UWUVCI app icon and writes Assets/UWUVCI.icns (needs Pillow and macOS iconutil).

Usage: python3 tools/make_icon.py
The design is a blue gradient squircle with a white GamePad outline and a bold U.
"""
import subprocess
import tempfile
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

SIZE = 1024
SS = 2  # supersampling for smooth edges
OUT = Path(__file__).resolve().parent.parent / "src" / "UWUVCI.Mac" / "Assets"


def gradient(size: int) -> Image.Image:
    top, bottom = (36, 196, 255), (30, 52, 150)
    img = Image.new("RGB", (size, size))
    px = img.load()
    for y in range(size):
        t = y / (size - 1)
        row = tuple(round(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
        for x in range(size):
            px[x, y] = row
    return img


def render() -> Image.Image:
    s = SIZE * SS
    body = int(824 * SS)          # Apple icon grid: 824 px body inside 1024
    off = (s - body) // 2
    radius = int(body * 0.2237)

    mask = Image.new("L", (s, s), 0)
    ImageDraw.Draw(mask).rounded_rectangle((off, off, off + body, off + body), radius, fill=255)

    base = gradient(s).convert("RGBA")
    # soft top highlight: a vertical alpha ramp, no hard edge
    ramp = Image.linear_gradient("L").resize((s, s)).rotate(180).point(lambda v: v * 38 // 255)
    base.paste((255, 255, 255, 255), mask=ImageChops.multiply(ramp, mask))

    art = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(art)
    # GamePad outline
    pad = (off + body * 0.13, off + body * 0.27, off + body * 0.87, off + body * 0.73)
    d.rounded_rectangle(pad, radius=int(body * 0.12), outline=(255, 255, 255, 255), width=int(body * 0.035))
    # bold U inside the pad: filled outer U minus inner U, built on a mask
    cx, cy = s / 2, off + body * 0.5
    r, t = body * 0.17, body * 0.07           # outer radius, stroke
    top = cy - body * 0.15
    mid = cy + body * 0.15 - r                # centre of the bowl
    letter = Image.new("L", (s, s), 0)
    ld = ImageDraw.Draw(letter)
    ld.rectangle((cx - r, top, cx + r, mid), fill=255)
    ld.pieslice((cx - r, mid - r, cx + r, mid + r), 0, 180, fill=255)
    ld.rectangle((cx - r + t, top - 2, cx + r - t, mid), fill=0)
    ld.pieslice((cx - r + t, mid - (r - t), cx + r - t, mid + (r - t)), 0, 180, fill=0)
    for x in (cx - r + t / 2, cx + r - t / 2):  # round the stem tops
        ld.ellipse((x - t / 2, top - t / 2, x + t / 2, top + t / 2), fill=255)
    art.paste((255, 255, 255, 255), mask=letter)
    # shadow under the art
    shadow = art.filter(ImageFilter.GaussianBlur(s // 90))
    shadow = Image.merge("RGBA", (*[Image.new("L", (s, s), 0)] * 3, shadow.split()[3].point(lambda v: v // 3)))
    shadow = ImageChops.offset(shadow, 0, int(s * 0.006))

    out = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    out.paste(base, mask=mask)
    out = Image.alpha_composite(out, shadow)
    out = Image.alpha_composite(out, art)
    return out.resize((SIZE, SIZE), Image.LANCZOS)


def main() -> None:
    icon = render()
    OUT.mkdir(parents=True, exist_ok=True)
    icon.save(OUT / "UWUVCI.png")
    with tempfile.TemporaryDirectory() as tmp:
        iconset = Path(tmp) / "UWUVCI.iconset"
        iconset.mkdir()
        for base in (16, 32, 128, 256, 512):
            icon.resize((base, base), Image.LANCZOS).save(iconset / f"icon_{base}x{base}.png")
            icon.resize((base * 2, base * 2), Image.LANCZOS).save(iconset / f"icon_{base}x{base}@2x.png")
        subprocess.run(["iconutil", "-c", "icns", str(iconset), "-o", str(OUT / "UWUVCI.icns")], check=True)


if __name__ == "__main__":
    main()
