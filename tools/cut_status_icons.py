"""Cut the status icons out of the owner's sheet, as supplied.

Why (owner, 2026-11-04): the status icons are the owner's own sheet,
ArtSource/ui/status-icons/owner-sheet-2026-11-04.png, five across and three down, each a dark
badge in a cream ring with its name under it. Each ring is found from its cream pixels, cut out
round (everything outside the ring transparent, the name left behind) and scaled to the 256 px
sprite StatusIcons.cs loads. The sheet's rings are about 140 px across, so the sprites are
upscaled from that.

Run:  python tools/cut_status_icons.py
"""
import os

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SHEET = os.path.join(ROOT, "ArtSource", "ui", "status-icons", "owner-sheet-2026-11-04.png")
OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "UI", "status-icons")
GRID = [["Blinded", "Chilled", "Concussed", "Disoriented", "Drained"],
        ["Feared", "Frozen", "Haunted", "Hexed", "Possessed"],
        ["Rooted", "Tagged", "Vulnerable", "Whirled", "Zapped"]]
SIZE, WORK = 256, 1024


def runs(counts, floor=3):
    out, start = [], None
    for i, on in enumerate(counts > floor):
        if on and start is None:
            start = i
        if not on and start is not None:
            out.append((start, i))
            start = None
    return out


def main():
    sheet = Image.open(SHEET).convert("RGBA")
    rgb = np.asarray(sheet)[:, :, :3].astype(int)
    cream = (rgb[:, :, 0] > 220) & (rgb[:, :, 1] > 210) & (rgb[:, :, 2] > 180)
    cols = runs(cream.sum(0))
    rows = [r for r in runs(cream.sum(1)) if r[1] - r[0] > 100]  # the rings, not the names
    assert len(cols) == 5 and len(rows) == 3, (cols, rows)
    mask = Image.new("L", (WORK, WORK), 0)
    ImageDraw.Draw(mask).ellipse([4, 4, WORK - 5, WORK - 5], fill=255)
    for (y0, y1), names in zip(rows, GRID):
        for (x0, x1), name in zip(cols, names):
            cx, cy, r = (x0 + x1) / 2, (y0 + y1) / 2, max(x1 - x0, y1 - y0) / 2 + 1
            icon = sheet.crop((round(cx - r), round(cy - r), round(cx + r), round(cy + r))).resize((WORK, WORK), Image.LANCZOS)
            icon.putalpha(mask)
            icon.resize((SIZE, SIZE), Image.LANCZOS).save(os.path.join(OUT, "Status" + name + ".png"))
    print("cut 15 status icons into", OUT)


if __name__ == "__main__":
    main()
