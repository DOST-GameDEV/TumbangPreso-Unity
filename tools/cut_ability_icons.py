"""Cut the ability icons out of the owner's sheet, as supplied.

Why (owner, 2026-11-05: "change the ability icons to match these instead, use these as is"). The
icons are the owner's sheet, ArtSource/ui/ability-icons/owner-sheet-2026-11-05.png: six across,
each named underneath, in the AbilityGlyph files' alphabetical order (ignoring case), on navy.

The navy is lifted off by flooding it in from each cell's edge, so dark paint inside an icon (the
eclipse, the cats' faces) stays solid; only the flooded rim is blended back to transparent, with its
colour unmixed from the navy. Every icon keeps the sheet's scale in a common box, written as the
256 px sprite AbilityIcons.cs loads. The sheet's icons are about 110 px, so they are upscaled.

Run:  python tools/cut_ability_icons.py
"""
import os

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SHEET = os.path.join(ROOT, "ArtSource", "ui", "ability-icons", "owner-sheet-2026-11-05.png")
OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "UI", "ability-icons")
SIZE = 256
SOFT = 70     # colour distance from the navy below which a flooded pixel is part transparent
PAD = 10      # sheet pixels of air around the largest icon


def runs(counts):
    out, start = [], None
    for i, on in enumerate(counts > 0):
        if on and start is None:
            start = i
        if not on and start is not None:
            out.append((start, i))
            start = None
    return out


def main():
    names = sorted((f[:-4] for f in os.listdir(OUT) if f.endswith(".png")), key=str.lower)
    rgb = np.asarray(Image.open(SHEET).convert("RGB")).astype(float)
    navy = rgb[-15, 15]
    ink = np.abs(rgb - navy).sum(2) > 45
    rows = [r for r in runs(ink.sum(1)) if r[1] - r[0] > 60 and r[0] > 85]  # icons, not names or title
    w = rgb.shape[1]
    # The sheet's columns are centred about 97 + 163 k, not on even sixths: an even split put Cheska Barricade's left
    # shards in Burst's cell.
    edges = [0] + [round(97 + 163.2 * (k + .5)) for k in range(5)] + [w]
    cells = []
    for y0, y1 in rows:
        for k in range(6):
            x0, x1 = edges[k], edges[k + 1]
            ys, xs = np.nonzero(ink[y0:y1, x0:x1])
            if len(xs) == 0:
                continue
            cells.append((x0 + xs.min(), y0 + ys.min(), x0 + xs.max() + 1, y0 + ys.max() + 1))
    assert len(cells) == len(names), (len(cells), len(names))
    box = max(max(c[2] - c[0], c[3] - c[1]) for c in cells) + 2 * PAD
    for name, (l, t, r, b) in zip(names, cells):
        cx, cy = (l + r) // 2, (t + b) // 2
        x0, y0 = cx - box // 2, cy - box // 2
        crop = np.full((box, box, 3), navy)
        sy0, sx0 = max(0, y0), max(0, x0)
        sy1, sx1 = min(rgb.shape[0], y0 + box), min(w, x0 + box)
        crop[sy0 - y0:sy1 - y0, sx0 - x0:sx1 - x0] = rgb[sy0:sy1, sx0:sx1]
        # Blank anything outside this icon's own bounds (a neighbour's edge, a name underneath).
        keep = np.zeros((box, box), bool)
        keep[max(0, t - y0 - 2):b - y0 + 2, max(0, l - x0 - 2):r - x0 + 2] = True
        crop[~keep] = navy
        dist = np.abs(crop - navy).max(2)
        # A copy: an image still backed by the numpy array ignores floodfill.
        flood = Image.fromarray(np.pad(np.where(dist < SOFT, 255, 0).astype(np.uint8), 1, constant_values=255)).copy()
        ImageDraw.floodfill(flood, (0, 0), 128)
        outside = np.asarray(flood)[1:-1, 1:-1] == 128
        alpha = np.where(outside, np.clip((dist - 12) / (SOFT - 12), 0, 1), 1.0)
        colour = np.where(alpha[..., None] > 0.02, navy + (crop - navy) / np.maximum(alpha[..., None], 0.02), 0)
        out = np.dstack([np.clip(colour, 0, 255), alpha * 255]).astype(np.uint8)
        Image.fromarray(out, "RGBA").resize((SIZE, SIZE), Image.LANCZOS).save(os.path.join(OUT, name + ".png"))
    print("cut", len(names), "ability icons into", OUT, "box", box)


if __name__ == "__main__":
    main()
