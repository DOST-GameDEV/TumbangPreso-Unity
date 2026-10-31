"""Paint the atlas of Phaister's Voodoo Doll redesign PROTOTYPE: flat tones only.

    py -3 tools/author_character_redesign_phaister_doll_textures.py

Writes ArtSource/phaister/doll-redesign-20261031/phaister-doll-redesign-atlas.png. The model script imports
this file for the LAYOUT only (PIL is imported inside `paint`, Blender's Python has none). Paint first, then build.

WHY FLAT, AND THE ONE WEAVE. The v11 doll's burlap was noisy (owner: "they look noisy"). v05 was flat everywhere; the owner
on it: "needs a cleaner version of the basket weave". So each tan tone also has a WEAVE TILE: big plain over-under cells in
three close hard-edged steps of that tone. No noise, grain, jitter, gradient or dark gap. Everything else is one flat tone.
The model script lays the tile by true arc length round each sack form, so one strand size holds everywhere.

THE ONE MECHANISM is Dante's: `Toon.shader` remaps a UV to a palette slot only in Unity atlas rows 0 to 7, so
every cell lives in the TOP half of the file and the bottom half is left flat.
"""
import math
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "ArtSource" / "phaister" / "doll-redesign-20261031"
ATLAS_NAME = "phaister-doll-redesign-atlas.png"
ATLAS = 2048
CELL = 64     # the same share of the file as v05's 32 of 1024, so every flat tone keeps its UV

# THE WEAVE. One strand's width in rig units (the head's front is about 0.42 round), and the tile that holds it.
WEAVE_CELL = float(os.environ.get("WEAVE_CELL", "0.028"))
WEAVE_PERIOD = 2.0 * WEAVE_CELL
WEAVE_PX = 16                 # px of one strand cell
WEAVE_TILE = 256              # px of one tone's tile: 8 periods
WEAVE_TOP = 256               # px from the top of the file; still inside the top half
WEAVE_STEPS = (1.0, 0.94, 0.88)   # the strand on top, the strand going the other way, the strands' edges
WEAVE_TONES = ["sack", "sack_lit", "sack_shade", "sack_deep", "sack_b", "sack_b_lit", "sack_b_shade",
               "mitten", "mitten_lit", "mitten_shade"]

# Sack tan, then Phaister's own hexes (tools/author_character_redesign_phaister_textures.py).
TONES = {
    "sack": "d2a56c", "sack_lit": "e6c28a", "sack_shade": "b08450", "sack_deep": "8a6238", "sack_dark": "4c3320",
    "sack_b": "c4945c", "sack_b_lit": "d8b078", "sack_b_shade": "a2763f",
    "mitten": "b98a55", "mitten_lit": "d0a570", "mitten_shade": "97703f",
    "purple": "4a1e78", "purple_lit": "6535a0", "purple_dark": "36155a",
    "lilac": "9838d8", "lilac_lit": "bb74ea", "lilac_dark": "7226ac",
    "magenta": "d8186e", "magenta_lit": "e82882", "magenta_dark": "b4125a",
    "crimson": "ab2a3a", "crimson_lit": "c23c4c", "crimson_dark": "8c1424",
    "gold": "f8b824", "gold_lit": "fcd76c", "gold_dark": "b87814",
    "straw": "e8c868", "straw_lit": "f6e09a", "straw_dark": "bf9a3e",
    "bone": "efe2c2", "bone_shade": "cfbf9a",
    "coat": "2c2940", "coat_lit": "3c3854", "coat_dark": "181622",
    "ink": "14101c", "thread": "2a1a36", "thread_lit": "efdfb8",
    "gem_lilac": "a24ae6", "gem_lilac_lit": "c98cf4",
    "gem_magenta": "e82882", "gem_magenta_lit": "f878b4",
    "gem_crimson": "c01c3a", "gem_crimson_lit": "e2566c",
    "gem_gold": "f8b824", "gem_gold_lit": "fde49a",
}
_NAMES = list(TONES)
_COLS = ATLAS // CELL


def atlas_uv(name, u=0.5, v=0.5):
    """The middle of a tone's cell, in Blender's UV (v up, so the top half of the file is v above 0.5)."""
    i = _NAMES.index(name)
    col, row = i % _COLS, i // _COLS
    x = (col + 0.5) * CELL / ATLAS
    y = 1.0 - (row + 0.5) * CELL / ATLAS
    if y <= 0.5:
        raise SystemExit("the atlas's top half is full")
    return (x, y)


def weave_uv(name, u, v):
    """A point of the weave, in rig units from its face's own period (0 to 6 periods), as Blender's UV in that tone's tile."""
    i = WEAVE_TONES.index(name)
    px_per_unit = WEAVE_PX / WEAVE_CELL
    margin = 2 * WEAVE_PX     # one period clear of the tile's edge
    x = (i % 8) * WEAVE_TILE + margin + u * px_per_unit
    y = WEAVE_TOP + (i // 8) * WEAVE_TILE + margin + v * px_per_unit
    return (x / ATLAS, 1.0 - y / ATLAS)


def paint():
    from PIL import Image, ImageDraw
    im = Image.new("RGB", (ATLAS, ATLAS), (128, 120, 110))
    d = ImageDraw.Draw(im)
    for i, name in enumerate(_NAMES):
        col, row = i % _COLS, i // _COLS
        h = TONES[name]
        c = tuple(int(h[k:k + 2], 16) for k in (0, 2, 4))
        d.rectangle((col * CELL, row * CELL, (col + 1) * CELL - 1, (row + 1) * CELL - 1), fill=c)
    for i, name in enumerate(WEAVE_TONES):
        h = TONES[name]
        base = tuple(int(h[k:k + 2], 16) for k in (0, 2, 4))
        a, b, c = (tuple(int(round(ch * s)) for ch in base) for s in WEAVE_STEPS)
        x0, y0 = (i % 8) * WEAVE_TILE, WEAVE_TOP + (i // 8) * WEAVE_TILE
        count = WEAVE_TILE // WEAVE_PX
        for cy in range(count):
            for cx in range(count):
                x, y = x0 + cx * WEAVE_PX, y0 + cy * WEAVE_PX
                x1, y1 = x + WEAVE_PX - 1, y + WEAVE_PX - 1
                if (cx + cy) % 2 == 0:      # this strand runs across: its long edges are top and bottom
                    d.rectangle((x, y, x1, y1), fill=a)
                    d.rectangle((x, y, x1, y + 1), fill=c)
                    d.rectangle((x, y1 - 1, x1, y1), fill=c)
                else:                       # this one runs up: its long edges are left and right
                    d.rectangle((x, y, x1, y1), fill=b)
                    d.rectangle((x, y, x + 1, y1), fill=c)
                    d.rectangle((x1 - 1, y, x1, y1), fill=c)
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    im.save(OUT_DIR / ATLAS_NAME)
    print("wrote", OUT_DIR / ATLAS_NAME)


if __name__ == "__main__":
    paint()
