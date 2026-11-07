"""Cut the HUD event symbols out of the owner's sheet into the atlas HudBadge draws from.

Why (owner, 2026-11-04): "some parts of the game still uses the old symbols make them use the
new symbols". The symbols are the owner's own sheet, ArtSource/ui/hud-symbols/owner-sheet-2026-11-04.png:
four across, three down, cream on navy with an orange second colour, each named underneath.

Each symbol is unmixed from the navy into two coverage masks, its cream part and its orange part
(every pixel solved as navy + a * (cream - navy) + b * (orange - navy)), so HudBadge can still
tint each part per use: the cream with the glyph's fill, the orange with its Accent. The cuts
(the can's bands, the shield's cross) stay transparent, as they are navy on the sheet.

Atlas (Resources/UI/hud-symbols/atlas.png): 6 x 5 cells of CELL px, top row first. Cells 0-11 are
the cream masks and 12-23 the orange masks, in HudBadge.Glyph order (Can first); cell 25 is the
hit burst's silhouette grown by a rim, drawn under it in the Rim colour; cell 29, the bottom-right
one and away from every symbol so it never bleeds into one, is solid white for the untextured
backing disc. Every cell keeps the sheet's
scale, so the symbols keep their sizes relative to each other.

Run:  python tools/cut_hud_symbols.py
"""
import os

import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SHEET = os.path.join(ROOT, "ArtSource", "ui", "hud-symbols", "owner-sheet-2026-11-04.png")
OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "UI", "hud-symbols", "atlas.png")
GRID = [["Tag", "Knock", "Restore", "Block"], ["Hit", "Can", "CanDown", "Slipper"], ["Crown", "Star", "Wave", "Dot"]]
ORDER = ["Can", "CanDown", "Slipper", "Crown", "Star", "Wave", "Dot", "Knock", "Restore", "Tag", "Block", "Hit"]
CELL, COLS, ROWS = 192, 6, 5
BOX = 168  # sheet pixels each cell covers, centred on its symbol (the widest symbol is 150)
CREAM, ORANGE = np.array([253, 242, 222.]), np.array([254, 124, 1.])


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
    rgb = np.asarray(Image.open(SHEET).convert("RGB")).astype(float)
    navy = rgb[5, 5].copy()
    basis = np.stack([CREAM - navy, ORANGE - navy], 1)          # 3 x 2
    solve = np.linalg.pinv(basis)                                # 2 x 3
    cov = np.clip(np.einsum("ij,hwj->hwi", solve, rgb - navy), 0, 1)
    ink = np.abs(rgb - navy).sum(2) > 60
    rows = [r for r in runs(ink.sum(1)) if r[1] - r[0] > 60]   # the symbols, not the names
    cols = runs(ink.sum(0))
    assert len(rows) == 3 and len(cols) == 4, (rows, cols)
    edges = [0] + [(cols[i][1] + cols[i + 1][0]) // 2 for i in range(3)] + [rgb.shape[1]]
    atlas = np.zeros((ROWS * CELL, COLS * CELL), np.float32)

    def put(index, mask):
        r, c = divmod(index, COLS)
        atlas[r * CELL:(r + 1) * CELL, c * CELL:(c + 1) * CELL] = mask

    for (y0, y1), names in zip(rows, GRID):
        for k, name in enumerate(names):
            x0, x1 = edges[k], edges[k + 1]
            ys, xs = np.nonzero(ink[y0:y1, x0:x1])
            cx, cy = x0 + (xs.min() + xs.max() + 1) / 2, y0 + (ys.min() + ys.max() + 1) / 2
            l, t = int(round(cx - BOX / 2)), int(round(cy - BOX / 2))
            masks = []
            for layer in (0, 1):
                m = np.zeros((BOX, BOX), np.float32)
                src = cov[max(t, y0 - 8):min(t + BOX, y1 + 8), max(l, x0):min(l + BOX, x1), layer]
                oy, ox = max(t, y0 - 8) - t, max(l, x0) - l
                m[oy:oy + src.shape[0], ox:ox + src.shape[1]] = src
                masks.append(np.asarray(Image.fromarray((m * 255).astype(np.uint8)).resize((CELL, CELL), Image.LANCZOS)) / 255.0)
            g = ORDER.index(name)
            put(g, masks[0])
            put(12 + g, masks[1])
            if name == "Hit":
                whole = Image.fromarray((np.clip(masks[0] + masks[1], 0, 1) * 255).astype(np.uint8))
                put(25, np.asarray(whole.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(1))) / 255.0)
    put(29, np.ones((CELL, CELL), np.float32))
    out = np.zeros(atlas.shape + (4,), np.uint8)
    out[..., :3] = 255
    out[..., 3] = (np.clip(atlas, 0, 1) * 255).astype(np.uint8)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    Image.fromarray(out, "RGBA").save(OUT)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
