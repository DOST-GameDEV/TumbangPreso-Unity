"""Paint the ONE shared atlas of the Payo audience (six background villagers).

    py -3 tools/author_payo_audience_textures.py

Writes ArtSource/payo/audience/audience-atlas.png (512 x 512, an 8 x 8 grid of 64 px cells).
`CELLS` names every cell; tools/author_payo_audience.py maps faces into them by name.

WHY. The Payo map session asked for a crowd for the basin wall (docs/CHARACTER_REDESIGN_DANTE.md
section 15.9 part K, item 5): dozens on screen, so one material and one small atlas for all of them.
The textiles take the map's own palette (house_long.png: madder red, white, indigo and teal bands
with small zigzags) so the crowd sits in the same village. Painted, not flat: every cell gets a
light-to-dark wash top to bottom and a little brush grain, like Rafi's atlas.
"""
import os

import numpy as np
# PIL is imported in build() so Blender, which has no PIL, can read CELLS.

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "ArtSource/payo/audience/audience-atlas.png")
C = 64
GRID = 8

CELLS = {name: i for i, name in enumerate([
    "skin", "skin_dark", "skin_tattoo", "skin_old",
    "hair_black", "hair_gray", "hair_back_black", "hair_back_gray",
    "face_man", "face_woman", "face_elder_man", "face_elder_woman",
    "face_boy", "face_girl", "side_black", "side_gray",
    "bahag_indigo", "bahag_red", "tapis_red", "tapis_white",
    "blanket_indigo", "blanket_white", "headcloth_red", "beads",
    "basket", "wood", "iron", "betel_bag",
    "sole", "side_woman", "hair_bun", "feather",
])}

rng = np.random.default_rng(7)


def hexc(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], float)


SKIN = hexc("c98a5a")
SKIN_DARK = hexc("a8683f")
SKIN_OLD = hexc("b47b52")
INK = hexc("2a1c18")
HAIR = hexc("231b1c")
GRAY = hexc("b9b2a8")
RED = hexc("b8322a")
WHITE = hexc("ece4d2")
INDIGO = hexc("22304f")
TEAL = hexc("2aa3a6")
YELLOW = hexc("e8b440")
STRAW = hexc("d6a84e")
BROWN = hexc("6b4426")


def blank(col):
    return np.tile(col, (C, C, 1)).astype(float)


def bands(spec):
    """Horizontal bands top to bottom: a list of (colour, height in px). Edges wobble a pixel."""
    a = np.zeros((C, C, 3))
    y = 0
    rows = []
    for col, h in spec:
        rows += [col] * h
    rows = (rows * (C // max(1, len(rows)) + 1))[:C]
    for y in range(C):
        a[y, :] = rows[y]
    return a


def zigzag(a, y0, col, amp=2, period=8):
    for x in range(C):
        t = x % period
        dy = int(round(amp * (1 - abs(t - period / 2) / (period / 2) * 2)))
        for k in (0, 1):
            yy = y0 + dy + k
            if 0 <= yy < C:
                a[yy, x] = col


def diamonds(a, y0, col, step=10):
    for cx in range(step // 2, C, step):
        for d in range(3):
            for s in (-1, 1):
                for yy, xx in ((y0 - 2 + d, cx + s * d), (y0 + 2 - d, cx + s * d)):
                    if 0 <= yy < C and 0 <= xx < C:
                        a[yy, xx] = col


def rect(a, x0, y0, x1, y1, col):
    a[y0:y1, x0:x1] = col


def paint(a, wash=0.16, grain=7.0):
    """The painted finish: lighter at the top of the cell, darker at the foot, brush grain."""
    v = np.linspace(1 + wash / 2, 1 - wash / 2, C)[:, None, None]
    n = rng.normal(0, grain, (C, C // 4, 1)).repeat(4, axis=1)  # short horizontal strokes
    return np.clip(a * v + n, 0, 255)


def face(skin, hair, kind):
    a = blank(skin)
    # Hair over the top quarter, with a fringe.
    if kind in ("elder_woman", "woman", "girl"):
        rect(a, 0, 0, C, 14, hair)
        rect(a, 0, 14, 7, 34, hair)       # hair falling at the temples
        rect(a, C - 7, 14, C, 34, hair)
    elif kind == "boy":
        rect(a, 0, 0, C, 15, hair)
        for x in range(4, C - 4, 9):
            rect(a, x, 15, x + 5, 19, hair)  # a chopped fringe
    else:
        rect(a, 0, 0, C, 12, hair)
    eye = INK
    ey = 31 if kind in ("boy", "girl") else 29
    ew, eh = (7, 8) if kind in ("boy", "girl") else (7, 6)
    rect(a, 17, ey, 17 + ew, ey + eh, eye)
    rect(a, C - 17 - ew, ey, C - 17, ey + eh, eye)
    rect(a, 18, ey + 1, 20, ey + 3, WHITE)          # a catchlight like the cast's
    rect(a, C - 17 - ew + 1, ey + 1, C - 17 - ew + 3, ey + 3, WHITE)
    if kind.startswith("elder"):
        rect(a, 16, ey - 4, 25, ey - 2, GRAY)        # gray brows
        rect(a, C - 25, ey - 4, C - 16, ey - 2, GRAY)
        rect(a, 13, ey + 9, 18, ey + 10, skin * 0.8)  # creases under the eyes
        rect(a, C - 18, ey + 9, C - 13, ey + 10, skin * 0.8)
        rect(a, 26, 48, 38, 50, INK * 0.9 + skin * 0.1)
    else:
        rect(a, 27, 47, 37, 49, INK)
    if kind in ("woman", "girl", "boy"):
        cheek = skin * 0.85 + RED * 0.15
        rect(a, 11, 40, 18, 44, cheek)
        rect(a, C - 18, 40, C - 11, 44, cheek)
    return a


def side(skin, hair, long_hair=False):
    a = blank(skin)
    rect(a, 0, 0, C, 16, hair)
    rect(a, 0, 0, 22 if not long_hair else 40, C if long_hair else 36, hair)  # back of the head
    rect(a, 34, 26, 42, 38, skin * 0.88)                                       # the ear
    return a


def tattoo(skin):
    a = blank(skin)
    for y in (12, 24, 36, 48):
        zigzag(a, y, INK * 0.7 + skin * 0.3, amp=2, period=10)
    for x in (6, C - 8):
        rect(a, x, 8, x + 2, 56, INK * 0.7 + skin * 0.3)
    return a


def weave(base, line, step=6):
    a = blank(base)
    for y in range(C):
        for x in range(C):
            if (x + y) % step == 0 or (x - y) % step == 0:
                a[y, x] = line
    return a


def build():
    cells = {}
    cells["skin"] = blank(SKIN)
    cells["skin_dark"] = blank(SKIN_DARK)
    cells["skin_tattoo"] = tattoo(SKIN_DARK)
    cells["skin_old"] = blank(SKIN_OLD)
    cells["hair_black"] = blank(HAIR)
    cells["hair_gray"] = blank(GRAY)
    cells["hair_back_black"] = blank(HAIR)
    cells["hair_back_gray"] = blank(GRAY)
    cells["face_man"] = face(SKIN_DARK, HAIR, "man")
    cells["face_woman"] = face(SKIN, HAIR, "woman")
    cells["face_elder_man"] = face(SKIN_OLD, GRAY, "elder_man")
    cells["face_elder_woman"] = face(SKIN_OLD, GRAY, "elder_woman")
    cells["face_boy"] = face(SKIN, HAIR, "boy")
    cells["face_girl"] = face(SKIN, HAIR, "girl")
    cells["side_black"] = side(SKIN_DARK, HAIR)
    cells["side_gray"] = side(SKIN_OLD, GRAY)
    cells["side_woman"] = side(SKIN, HAIR, long_hair=True)

    b = bands([(INDIGO, 10), (RED, 4), (WHITE, 2), (INDIGO, 18), (WHITE, 2), (RED, 4), (INDIGO, 24)])
    zigzag(b, 27, WHITE * 0.9)
    cells["bahag_indigo"] = b
    b = bands([(RED, 12), (WHITE, 3), (INDIGO, 3), (RED, 26), (INDIGO, 3), (WHITE, 3), (RED, 14)])
    diamonds(b, 30, WHITE)
    cells["bahag_red"] = b
    t = bands([(RED, 8), (INDIGO, 4), (RED, 8), (WHITE, 3), (INDIGO, 6), (WHITE, 3), (RED, 8), (INDIGO, 4),
               (RED, 8), (YELLOW, 2), (RED, 10)])
    zigzag(t, 31, YELLOW)
    cells["tapis_red"] = t
    t = bands([(WHITE, 10), (INDIGO, 6), (WHITE, 4), (TEAL, 4), (WHITE, 10), (RED, 6), (WHITE, 10), (INDIGO, 6),
               (WHITE, 8)])
    zigzag(t, 42, RED)
    cells["tapis_white"] = t
    k = bands([(INDIGO, 9), (RED, 3), (WHITE, 2), (RED, 3), (INDIGO, 15), (TEAL, 2), (INDIGO, 15), (RED, 3),
               (WHITE, 2), (RED, 3), (INDIGO, 7)])
    diamonds(k, 31, WHITE)
    cells["blanket_indigo"] = k
    k = bands([(WHITE, 12), (INK, 3), (RED, 6), (INK, 3), (WHITE, 16), (INK, 3), (RED, 6), (INK, 3), (WHITE, 12)])
    zigzag(k, 31, INK)
    cells["blanket_white"] = k
    h = bands([(RED, 20), (YELLOW, 3), (RED, 18), (YELLOW, 3), (RED, 20)])
    cells["headcloth_red"] = h
    d = blank(INK)
    cols = [RED, YELLOW, WHITE, TEAL, RED, hexc("e07a2c")]
    for i, y in enumerate(range(2, C, 8)):
        for x in range(2, C, 8):
            rect(d, x, y, x + 6, y + 6, cols[(i + x // 8) % len(cols)])
    cells["beads"] = d
    cells["basket"] = weave(STRAW, BROWN, 6)
    w = blank(BROWN)
    for x in range(0, C, 5):
        w[:, x] = BROWN * 0.8
    cells["wood"] = w
    cells["iron"] = bands([(hexc("8d8f92"), 32), (hexc("5e6064"), 32)])
    bb = weave(hexc("8a5a2c"), hexc("3b2414"), 5)
    rect(bb, 0, 26, C, 32, RED)
    cells["betel_bag"] = bb
    s = blank(SKIN)
    rect(s, 0, 44, C, C, hexc("7a4e30"))
    cells["sole"] = s
    hb = blank(HAIR)
    for x in range(0, C, 10):
        rect(hb, x + 2, 28, x + 7, 34, [RED, YELLOW, WHITE][x // 10 % 3])
    cells["hair_bun"] = hb
    f = blank(WHITE)
    rect(f, 0, 40, C, C, INK)
    rect(f, 30, 0, 34, C, BROWN)
    cells["feather"] = f

    from PIL import Image
    atlas = np.zeros((C * GRID, C * GRID, 3))
    atlas[:] = hexc("808080")
    for name, i in CELLS.items():
        a = paint(cells[name], grain=4.0 if name.startswith("face") else 7.0)
        r, c = divmod(i, GRID)
        atlas[r * C:(r + 1) * C, c * C:(c + 1) * C] = a
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    Image.fromarray(atlas.astype(np.uint8)).save(OUT)
    print("wrote", OUT)


if __name__ == "__main__":
    build()
