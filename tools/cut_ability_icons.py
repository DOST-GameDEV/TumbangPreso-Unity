"""Cut the ability icons out of the owner's sheets, as supplied.

Why (owner, 2026-11-05: "change the ability icons to match these instead, use these as is"). Three
sheets in ArtSource/ui/ability-icons, each on navy with every icon named underneath, laid in order
so a later sheet's icon replaces an earlier one's:
  1. owner-sheet-2026-11-05.png: six across, in the AbilityGlyph files' alphabetical order (ignoring
     case). Still the source of the older Phaister icons the later sheets leave out.
  2. owner-sheet-2026-11-05b.png ("use this new sheet (except for phaister)"): rows of different
     lengths, so each cell is found from its name's position (NEW_ROWS); Phaister's cells are skipped.
  3. phaister-sheet-2026-11-05.png: her revamped kit, left to right (PHAISTER).

The navy is lifted off by flooding it in from each cell's edge, so dark paint inside an icon (the
eclipse, the cats' faces) stays solid; only the flooded rim is blended back to transparent, with its
colour unmixed from the navy. Every icon keeps the sheet's scale in a common box, written as the
256 px sprite AbilityIcons.cs loads. The sheet's icons are about 110 px, so they are upscaled.

Each icon then gets a round black outline with a hard edge (owner, 2026-11-05): its silhouette
blurred and thresholded, laid under it. An outline-free copy goes to ability-icons-plain.

Run:  python tools/cut_ability_icons.py
"""
import os
import re
import time
import uuid

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "ArtSource", "ui", "ability-icons")
SHEET = os.path.join(ART, "owner-sheet-2026-11-05.png")
RES = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "UI")
OUT = os.path.join(RES, "ability-icons")
# The same icons with no outline, for the match feed (owner, 2026-11-05: "don't add the black outlines when they
# show on the match feed"). AbilityIcons.Plain loads these.
PLAIN = os.path.join(RES, "ability-icons-plain")
SIZE = 256
SOFT = 70     # colour distance from the navy below which a flooded pixel is part transparent
FILL = 1.24   # each cell's box against the largest icon on its sheet: one scale across sheets, room for the outline
# The silhouette blurred by OUTLINE_BLUR output px, then everything above OUTLINE_EDGE made solid black: a rounded
# outline (the blur rounds every corner) with a hard edge, OUTLINE_SOFT only wide enough to antialias it (owner,
# 2026-11-05: "hard edge should do", then twice "make the black outlines thicker").
OUTLINE_BLUR, OUTLINE_EDGE, OUTLINE_SOFT = 10.0, .04, .03
NEW_SHEET = os.path.join(ART, "owner-sheet-2026-11-05b.png")
# The second sheet's names row by row; None for a cell kept from elsewhere (Phaister's; its "Phaister Phase" is the
# shared Phase glyph, not hers).
NEW_ROWS = [
    ["AmihanQuickDash", "AmihanStormSurge", "AmihanUpdraft", "AmihanWhirlwind", "Burst", "CheskaBarricade",
     "CheskaFrostbite", "CheskaFrostSheet", "CheskaNova"],
    ["DanteBarrier", "DanteBoulder", "DanteFissure", "DanteShield", "DanteStomp", "Dash", "Empower", "NemuAstralPet",
     "NemuDontLook"],
    ["NemuKuroGuard", "NemuLookout", "NemuPhase", "NemuSeanceVoid", "NemuSkitter", "NemuTerrify", "PaeteSentry",
     "PaeteSprout", "PaeteThorn"],
    ["PaeteVine", None, None, None, None, "Phase", "Projectile", "RafiBreakwater", "RafiCrosscurrent"],
    ["RafiMirrorwake", "RafiSkim", "RafiWaterwall", "SeanCinderGate", "SeanIgnite", "SeanRush", "SeanSupernova",
     "SeanVolcanicBomb", "Shield"],
    ["Slam", "Wall", "ZackAfterimage", "ZackBacktrack", "ZackFullThrottle", "ZackLiveWire", "ZackOvercharge",
     "ZackOverclock", "ZackSprint", "ZackThunderstrike", "Zone"],
]
# Phaister's revamped kit (owner, 2026-11-05), its own sheet, left to right, over her cells from the first sheet.
PHAISTER = (os.path.join(ART, "phaister-sheet-2026-11-05.png"),
            ["PhaisterShadowBlink", "PhaisterCursedDoll", "PhaisterVulnerable", "PhaisterVoodooDoll"])
EXTRA = {"PhaisterVoodooDoll"}   # glyphs only a later sheet draws, not counted against the first


def save(icon, path):
    """An open Unity editor re-imports each icon as it lands and holds it for a moment; wait it out."""
    for attempt in range(40):
        try:
            icon.save(path)
            return
        except OSError:
            if attempt == 39:
                raise
            time.sleep(0.25)


def ensure_meta(path, template):
    """A new icon needs Unity's sprite import, or Resources.Load<Sprite> finds nothing: copy a sibling's, fresh ids."""
    if os.path.exists(path + ".meta"):
        return
    text = open(template, encoding="utf-8").read()
    text = re.sub(r"(?m)^guid: \w+", "guid: " + uuid.uuid4().hex, text)
    text = re.sub(r"spriteID: \w+", "spriteID: " + uuid.uuid4().hex, text)
    with open(path + ".meta", "w", encoding="utf-8", newline="\n") as out:
        out.write(text)


def outlined(icon):
    alpha = icon.getchannel("A")
    ring = alpha.filter(ImageFilter.GaussianBlur(OUTLINE_BLUR))
    ring = np.clip((np.asarray(ring) / 255.0 - OUTLINE_EDGE) / OUTLINE_SOFT, 0, 1)
    ring = Image.fromarray((ring * ring * (3 - 2 * ring) * 255).astype(np.uint8))
    out = Image.new("RGBA", icon.size, (0, 0, 0, 0))
    out.putalpha(ring)
    out.alpha_composite(icon)
    return out


def runs(counts):
    out, start = [], None
    for i, on in enumerate(counts > 0):
        if on and start is None:
            start = i
        if not on and start is not None:
            out.append((start, i))
            start = None
    return out


def cut(rgb, navy, ink, cells, names):
    w = rgb.shape[1]
    box = round(max(max(c[2] - c[0], c[3] - c[1]) for c in cells) * FILL)
    template = os.path.join(OUT, "PhaisterHexSigil.png.meta")
    for name, (l, t, r, b) in zip(names, cells):
        if name is None:
            continue
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
        plain = Image.fromarray(out, "RGBA").resize((SIZE, SIZE), Image.LANCZOS)
        path = os.path.join(OUT, name + ".png")
        save(outlined(plain), path)
        ensure_meta(path, template)
        flat = os.path.join(PLAIN, name + ".png")
        save(plain, flat)
        ensure_meta(flat, path + ".meta")
    return box


def sheet_cells(path, edges_for, min_row=60, top=0):
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(float)
    navy = rgb[-15, 15]
    ink = np.abs(rgb - navy).sum(2) > 45
    rows = [r for r in runs(ink.sum(1)) if r[1] - r[0] > min_row and r[0] > top]   # icons, not names or title
    edges = edges_for(rgb.shape[1])
    cells = []
    for y0, y1 in rows:
        for k in range(len(edges) - 1):
            x0, x1 = edges[k], edges[k + 1]
            ys, xs = np.nonzero(ink[y0:y1, x0:x1])
            if len(xs):
                cells.append((x0 + xs.min(), y0 + ys.min(), x0 + xs.max() + 1, y0 + ys.max() + 1))
    return rgb, navy, ink, cells


def labelled_cells(path):
    """Cells of a sheet whose rows differ in length: each row's names give its column centres, split halfway between."""
    rgb = np.asarray(Image.open(path).convert("RGB")).astype(float)
    navy = rgb[-15, 15]
    ink = np.abs(rgb - navy).max(2) > 40
    bands = runs(ink.sum(1))
    cells = []
    for (y0, y1), (n0, n1) in zip(bands, bands[1:]):
        if y1 - y0 < 60 or n1 - n0 > 30:
            continue   # an icon row is tall and its names come straight after it
        # The words of one name sit closer together than two names do.
        cols, start, last = [], None, None
        for x, on in enumerate(ink[n0:n1].sum(0) > 0):
            if on:
                start = x if start is None else start
                last = x
            elif start is not None and x - last > 14:
                cols.append((start + last) / 2)
                start = None
        if start is not None:
            cols.append((start + last) / 2)
        edges = [0] + [round((a + b) / 2) for a, b in zip(cols, cols[1:])] + [rgb.shape[1]]
        for x0, x1 in zip(edges, edges[1:]):
            ys, xs = np.nonzero(ink[y0:y1, x0:x1])
            cells.append((x0 + xs.min(), y0 + ys.min(), x0 + xs.max() + 1, y0 + ys.max() + 1))
    return rgb, navy, ink, cells


def main():
    os.makedirs(PLAIN, exist_ok=True)
    names = sorted((f[:-4] for f in os.listdir(OUT) if f.endswith(".png") and f[:-4] not in EXTRA), key=str.lower)
    # The sheet's columns are centred about 97 + 163 k, not on even sixths: an even split put Cheska Barricade's left
    # shards in Burst's cell.
    rgb, navy, ink, cells = sheet_cells(SHEET, lambda w: [0] + [round(97 + 163.2 * (k + .5)) for k in range(5)] + [w], top=85)
    assert len(cells) == len(names), (len(cells), len(names))
    box = cut(rgb, navy, ink, cells, names)
    rgb, navy, ink, cells = labelled_cells(NEW_SHEET)
    newer = [name for row in NEW_ROWS for name in row]
    assert len(cells) == len(newer), (len(cells), len(newer))
    cut(rgb, navy, ink, cells, newer)
    path, phaister = PHAISTER
    rgb, navy, ink, cells = sheet_cells(path, lambda w: [round(w * k / 4) for k in range(5)], min_row=120)
    assert len(cells) == len(phaister), (len(cells), phaister)
    cut(rgb, navy, ink, cells, phaister)
    print("cut", len(names), "ability icons and Phaister's", len(phaister), "into", OUT, "and", PLAIN, "box", box)


if __name__ == "__main__":
    main()
