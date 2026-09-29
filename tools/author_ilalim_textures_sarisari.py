"""Paint the sari-sari store's textures, in the house illustrated style (ILALIM-1.3, sari-sari kit).

  py -3 tools/author_ilalim_textures_sarisari.py [--sheet N]

Writes ArtSource/ilalim/textures/sari_*.png and a swatch sheet
Logs/ilalim-blender/sari_swatches_vN.png. The store is modelled by tools/author_ilalim_sarisari.py.

Owner, 2026-09-29: "can you put a sari sari store somewhere". The store is a two-storey corner house
at Taft and Padre Faura whose ground-floor front room is the tindahan. The drawing helpers
(coats, patches, hand-warped lettering) are the prop kit's, imported from
tools/author_ilalim_textures_props.py, so the store sits in the same hand as the street props.

THE STYLE (KANTO_DESIGN_GUIDE.md section 3, LAGOON_REWORK_GUIDE.md section 2): FLAT fills, a few
LARGE patches with FEATHERED organic edges, low contrast, no grain, no noise, no streaks, no
airbrushed blur. Every surface is its own drawing.

  TILING (world UVs, 2 m per 512 px tile):
    sari_plaster    NEUTRAL plastered hollow block: two broad coats, squarish repaint patches
                    where a crack was filled, and one broad damp field.
    sari_siding     NEUTRAL lap siding on the upper floor: 20 cm boards, each board its own
                    value, a soft shadow under every lap, and broad faded and damp fields.
    sari_giroof     galvanised iron roofing: ribs every 7.6 cm down the slope, a lap seam every
                    0.9 m, and broad rust fields with a few rust tongues running downslope.
  ONE-OFF artwork (UV 0..1 on one face), each its own sign system:
    sari_sign_main  the painted signboard across the front: BEBANG'S / SARI-SARI STORE /
                    SOFTDRINKS · YELO · LOAD · BIGAS, cream board, red brush letters, green border.
    sari_sign_tin   a white tin plate nailed to the Taft wall: BIGAS / ASUKAL / MANTIKA with
                    prices (a different hand, rust bleeding from the nail holes).
    sari_sign_yelo  cardboard on the cooler: MAY YELO / ICE CANDY P10 in marker.
    sari_sign_utang the card inside the grille: BAWAL ANG UTANG / BUKAS PWEDE (the old joke).
    sari_stock      the back shelves' stock, drawn: jars, tins, bottles, packs, standing on the
                    shelf lines the model puts planks on (every 0.5 m). No real brands.
    sari_snacks     a hanging strip of snack bags (pillow packs with a clear window), invented.
    sari_jalousie   the upper-floor jalousie window: frosted glass slats, a curtain behind.
    sari_door       the house door: varnished wood, six raised panels, kick scuffs.
    sari_gate       the side gate: maroon painted sheet, NO PARKING / BAWAL PUMARADA in white.

ROLE HUES (Art_Direction.md section 1): nothing near offence orange #f87020 or defence blue
#0080e8. The reds here are brick and crimson, the greens bottle green; no mid blue anywhere.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
from author_ilalim_textures_props import (  # noqa: E402
    OUT, SHEETS, TILE, blobs, circle_mask, coat, drips, fill, frame_mask, hexcol, paint, patches,
    rect_mask, save, smooth, text_mask, weather, wobbly_lines, _noise1d,
)


# ------------------------------------------------------------------ tiling surfaces (2 m tiles)

def sari_plaster():
    img = fill(TILE, TILE, "eeede8")
    img = coat(img, (0.965, 0.962, 0.955), 120, 0.32, seed=1101, feather=1.2)
    img = coat(img, (1.02, 1.02, 1.018), 80, 0.2, seed=1102, feather=1.0)
    # Repaint patches: squarish, slightly off in value, where someone filled a crack and painted
    # only the patch.
    rng = np.random.default_rng(1103)
    for k in range(3):
        x0, y0 = rng.uniform(20, TILE - 160), rng.uniform(20, TILE - 140)
        box = (x0, y0, x0 + rng.uniform(70, 140), y0 + rng.uniform(50, 110))
        m = rect_mask(TILE, TILE, box, radius=8, wobble=4.0, seed=1104 + k, feather=2.5)
        img = img * (1 - 0.05 * m[..., None] * (1 if k % 2 else -0.6))
    # A long damp shadow rising from the foot of the wall: one broad feathered field.
    damp = patches(TILE, TILE, 150, 0.16, 1110, feather=1.0, stretch=(1.0, 0.5))
    img = img * (1 - 0.05 * damp[..., None])
    save("sari_plaster", img)


def sari_siding():
    img = fill(TILE, TILE, "efede7")
    board = 51                             # 20 cm boards on a 2 m tile (512 px)
    rng = np.random.default_rng(1201)
    for k in range(TILE // board + 1):
        y0, y1 = k * board, min(TILE, (k + 1) * board)
        img[y0:y1] *= 1 + rng.uniform(-0.035, 0.035)
    yy, xx = np.mgrid[0:TILE, 0:TILE].astype(float)
    # The shadow under each lap: a soft dark band at the bottom of every board.
    d = (yy % board)
    lap = np.clip((d - (board - 7)) / 3, 0, 1)
    img = img * (1 - 0.16 * lap[..., None])
    # Butt joints between board lengths, staggered.
    for k in range(TILE // board + 1):
        x = rng.uniform(0, TILE)
        a = np.clip((1.5 - np.abs(xx - x)) / 1.0, 0, 1) * ((yy >= k * board) & (yy < (k + 1) * board - 6))
        img = img * (1 - 0.12 * a[..., None])
    # Weathering: broad sun-faded and damp fields across several boards, never spots.
    img = coat(img, (1.03, 1.03, 1.02), 150, 0.25, seed=1202, feather=1.2, stretch=(1.0, 0.5))
    img = coat(img, (0.95, 0.94, 0.92), 130, 0.15, seed=1203, feather=1.2, stretch=(1.0, 0.5))
    save("sari_siding", img)


def sari_giroof():
    img = fill(TILE, TILE, "a3a5a1")
    img = coat(img, (1.06, 1.06, 1.05), 140, 0.3, seed=1301, feather=1.3)
    yy, xx = np.mgrid[0:TILE, 0:TILE].astype(float)
    pitch = TILE / 26                      # 7.7 cm ribs on a 2 m tile
    phase = (xx % pitch) / pitch
    rib = 0.5 + 0.5 * np.cos(phase * 2 * np.pi)
    img = img * (0.93 + 0.1 * rib[..., None])
    # Lap seams: every 0.9 m in the tile width is not a divisor of 2 m, so seams at 0 and half.
    for x in (0, TILE / 2):
        a = np.clip((2.5 - np.abs(((xx - x + TILE / 2) % TILE) - TILE / 2)) / 1.2, 0, 1)
        img = img * (1 - 0.18 * a[..., None])
    # Rust fields and tongues running down the slope (v is along the slope on the model).
    rust = patches(TILE, TILE, 160, 0.2, 1302, feather=1.0, stretch=(1.0, 0.45))
    img = img * (1 - 0.6 * rust[..., None]) + hexcol("8a5a3c") * 0.6 * rust[..., None]
    deep = patches(TILE, TILE, 120, 0.05, 1303, feather=0.8, stretch=(1.0, 0.5))
    img = img * (1 - 0.5 * deep[..., None]) + hexcol("6d4230") * 0.5 * deep[..., None]
    img = drips(img, list(np.random.default_rng(1304).uniform(0, TILE, 7)), 0, 150, 6, "7c5038", seed=1305,
                strength=0.4)
    save("sari_giroof", img)


# ------------------------------------------------------------------ one-off artwork

def sari_sign_main():
    h, w = 300, 1680                       # 4.2 m x 0.75 m
    img = fill(h, w, "efe2c2")
    img = coat(img, (1.03, 1.02, 1.0), 160, 0.3, seed=1401, feather=1.2)
    img = paint(img, frame_mask(h, w, (10, 10, w - 10, h - 10), 22, 20, 2.0, 1402), "2f5e3a", 1403)
    img = paint(img, text_mask(h, w, ["Bebang's"], "print", (120, 26, 560, 116), seed=1404, wobble=1.6,
                               lean=-0.05), "2f5e3a", 1405)
    img = paint(img, text_mask(h, w, ["SARI-SARI STORE"], "impact", (120, 96, w - 120, 226), seed=1406,
                               wobble=1.8), "a0282a", 1407)
    img = paint(img, text_mask(h, w, ["SOFTDRINKS  ·  YELO  ·  LOAD  ·  BIGAS"], "black", (260, 234, w - 260, 272),
                               seed=1408, wobble=1.6), "2b2626", 1409)
    # Two painted emblems at the ends: a bottle on the left and a candy on the right. Flat
    # shapes, no shading.
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    bottle = rect_mask(h, w, (54, 110, 96, 240), 14) + rect_mask(h, w, (66, 64, 84, 120), 6)
    img = paint(img, np.clip(bottle, 0, 1), "3c7048", 1410, 0)
    img = paint(img, rect_mask(h, w, (54, 150, 96, 184), 4), "efe2c2", 1411, 0)
    img = paint(img, rect_mask(h, w, (64, 56, 86, 70), 3), "a0282a", 1412, 0)
    cx, cy = w - 80, 150
    candy = circle_mask(h, w, cx, cy, 34)
    twist = np.clip(1 - (np.abs(yy - cy) / 26) - np.clip((np.abs(xx - cx) - 30) / 30, 0, 1) ** 0.8, 0, 1) \
        * (np.abs(xx - cx) > 28) * (np.abs(xx - cx) < 66)
    img = paint(img, np.clip(candy + (twist > 0.05), 0, 1), "d6a93a", 1413, 0)
    stripe = candy * np.clip((6 - np.abs((xx - cx) + (yy - cy) * 0.6)) / 1.5, 0, 1)
    img = paint(img, stripe, "a0282a", 1415, 0)
    img = weather(img, 1416, fade=0.06)
    # Rain streaks off the top edge would be streaks; instead two soft tide marks from the fascia.
    img = drips(img, [300, 760, 1310], 0, 60, 16, "b9aa8a", seed=1417, strength=0.3)
    save("sari_sign_main", img)


def sari_sign_tin():
    h, w = 360, 540                        # 0.9 m x 0.6 m
    img = fill(h, w, "ebe8df")
    img = coat(img, (0.97, 0.965, 0.955), 90, 0.3, seed=1501, feather=1.2)
    img = paint(img, text_mask(h, w, ["BIGAS", "ASUKAL", "MANTIKA"], "black", (40, 30, 330, 330), seed=1502,
                               wobble=2.2, align="left", spacing=0.25, lean=-0.04), "8c2525", 1503)
    img = paint(img, text_mask(h, w, ["P52/k", "P85/k", "P20"], "black", (360, 30, w - 40, 330), seed=1504,
                               wobble=2.2, align="right", spacing=0.25), "2b2626", 1505)
    # Rust: bleeding from the four nail holes and along the bottom edge.
    for (x, y) in ((24, 24), (w - 24, 24), (24, h - 24), (w - 24, h - 24)):
        img = paint(img, circle_mask(h, w, x, y, 6), "4a3226", 1506, 0)
        img = drips(img, [x], y, 50, 7, "8a5a3c", seed=1507 + x, strength=0.45)
    yy = np.mgrid[0:h, 0:w][0].astype(float)
    edge = h - 26 + 10 * _noise1d(w, 40, 1510)[None, :]
    a = np.clip((yy - edge) / 5 + 0.5, 0, 1) * 0.6
    img = img * (1 - a[..., None]) + hexcol("8a5a3c") * a[..., None]
    img = weather(img, 1511, dirt_bottom=False)
    save("sari_sign_tin", img)


def sari_sign_yelo():
    h, w = 300, 450                        # 0.45 m x 0.3 m cardboard
    img = fill(h, w, "bb996d")
    img = coat(img, (1.06, 1.05, 1.03), 70, 0.3, seed=1601, feather=1.2)
    img = paint(img, text_mask(h, w, ["MAY YELO"], "ink", (30, 26, w - 30, 150), seed=1602, wobble=3.0, stroke=3),
                "1d1b1c", 1603, 0.03)
    img = paint(img, text_mask(h, w, ["ICE CANDY P10"], "ink", (40, 170, w - 40, 262), seed=1604, wobble=3.0,
                               stroke=2), "8e2222", 1605, 0.03)
    img = coat(img, (0.88, 0.86, 0.84), 50, 0.1, seed=1606, feather=1.0)
    save("sari_sign_yelo", img)


def sari_sign_utang():
    h, w = 260, 400                        # 0.4 m x 0.26 m card
    img = fill(h, w, "f2efe6")
    img = coat(img, (0.96, 0.955, 0.94), 60, 0.25, seed=1701, feather=1.2)
    img = paint(img, text_mask(h, w, ["BAWAL ANG", "UTANG"], "print", (24, 18, w - 24, 170), seed=1702,
                               wobble=2.0, spacing=0.02), "a0282a", 1703, 0.02)
    img = paint(img, text_mask(h, w, ["bukas pwede :)"], "ink", (60, 180, w - 40, 240), seed=1704, wobble=2.4),
                "2b2626", 1705, 0.02)
    # Tape at the two top corners.
    img = paint(img, rect_mask(h, w, (-10, -6, 60, 26), 3), "d9d2b5", 1706, 0)
    img = paint(img, rect_mask(h, w, (w - 60, -6, w + 10, 26), 3), "d9d2b5", 1707, 0)
    save("sari_sign_utang", img)


def sari_stock():
    """The back shelf stock, 3.3 m x 2.0 m at 300 px per metre. Plank lines every 0.5 m from the
    bottom (the model puts its planks there); goods stand on each line."""
    h, w = 600, 990
    img = fill(h, w, "5a4a3d")                             # dim plywood backing
    img = coat(img, (1.1, 1.08, 1.05), 90, 0.25, seed=1801, feather=1.2)
    rng = np.random.default_rng(1802)
    goods = [("jar", "c9b27a"), ("jar", "a8c07a"), ("tin", "b33a33"), ("tin", "e1d6b8"), ("tin", "3f6e4a"),
             ("bottle", "3c6a45"), ("bottle", "6a3a2a"), ("bottle", "d9cfa6"), ("pack", "d7b547"),
             ("pack", "7a3f6e"), ("pack", "d25a5a"), ("pack", "efe7d2"), ("box", "b44436"), ("box", "e0c060"),
             ("box", "5d7f55")]
    for shelf in range(4):
        base = h - shelf * 150 - 4                         # every 0.5 m from the panel bottom
        x = 8.0
        while x < w - 30:
            kind, col = goods[rng.integers(len(goods))]
            if kind == "jar":
                bw, bh = rng.uniform(40, 52), rng.uniform(62, 80)
                m = rect_mask(h, w, (x, base - bh, x + bw, base), 10)
                img = paint(img, m, col, int(x) + shelf, 0.03)
                img = paint(img, rect_mask(h, w, (x + 4, base - bh - 10, x + bw - 4, base - bh + 4), 4),
                            ("a0282a", "3f6e4a", "d7b547")[rng.integers(3)], int(x) + 1, 0)
            elif kind == "tin":
                bw, bh = rng.uniform(26, 34), rng.uniform(38, 48)
                for k in range(int(rng.integers(2, 4))):
                    xx0 = x + k * (bw + 2)
                    img = paint(img, rect_mask(h, w, (xx0, base - bh, xx0 + bw, base), 3), col, int(xx0), 0)
                    img = paint(img, rect_mask(h, w, (xx0, base - bh * 0.62, xx0 + bw, base - bh * 0.3), 1),
                                "efe8d6", int(xx0) + 7, 0)
                bw = (bw + 2) * k + bw
            elif kind == "bottle":
                bw, bh = rng.uniform(18, 24), rng.uniform(80, 110)
                for k in range(int(rng.integers(3, 6))):
                    xx0 = x + k * (bw + 3)
                    body = rect_mask(h, w, (xx0, base - bh * 0.65, xx0 + bw, base), 6)
                    neck = rect_mask(h, w, (xx0 + bw * 0.3, base - bh, xx0 + bw * 0.7, base - bh * 0.6), 3)
                    img = paint(img, np.clip(body + neck, 0, 1), col, int(xx0), 0)
                    img = paint(img, rect_mask(h, w, (xx0 + 1, base - bh * 0.45, xx0 + bw - 1, base - bh * 0.25), 1),
                                "efe8d6", int(xx0) + 3, 0)
                bw = (bw + 3) * k + bw
            elif kind == "pack":
                bw, bh = rng.uniform(34, 46), rng.uniform(60, 90)
                for k in range(int(rng.integers(2, 4))):
                    xx0 = x + k * (bw - 8)
                    img = paint(img, rect_mask(h, w, (xx0, base - bh, xx0 + bw, base), 12), col, int(xx0), 0.04)
                    img = paint(img, circle_mask(h, w, xx0 + bw / 2, base - bh * 0.55, bw * 0.24), "efe8d6",
                                int(xx0) + 5, 0)
                bw = (bw - 8) * k + bw
            else:
                bw, bh = rng.uniform(70, 110), rng.uniform(50, 80)
                img = paint(img, rect_mask(h, w, (x, base - bh, x + bw, base), 2), col, int(x), 0.03)
                img = paint(img, rect_mask(h, w, (x + 8, base - bh + 10, x + bw - 8, base - bh + 26), 2),
                            "efe8d6", int(x) + 9, 0)
            x += bw + rng.uniform(6, 16)
    img = weather(img, 1810, fade=0.04, dirt_bottom=False)
    save("sari_stock", img)


def sari_snacks():
    h, w = 900, 200                        # 0.2 m x 0.9 m strip of five bags
    img = fill(h, w, "d9d4c6")
    colours = ["c8342e", "d9ad3c", "3f7a4f", "7a3f6e", "c8342e"]
    ph = h / 5
    for k, c in enumerate(colours):
        y0 = k * ph
        img = paint(img, rect_mask(h, w, (6, y0 + 6, w - 6, y0 + ph - 6), 26), c, 1901 + k, 0.04)
        img = paint(img, rect_mask(h, w, (6, y0 + 6, w - 6, y0 + 22), 6), "efe8d6", 1910 + k, 0)   # crimp
        img = paint(img, circle_mask(h, w, w / 2, y0 + ph * 0.55, 44), "efe8d6", 1920 + k, 0)       # window
        img = paint(img, circle_mask(h, w, w / 2, y0 + ph * 0.55, 30), ("d9ad3c", "a0602a", "e8d9a0")[k % 3],
                    1930 + k, 0)
    img = weather(img, 1940, dirt_bottom=False)
    save("sari_snacks", img)


def sari_jalousie():
    h, w = 480, 560                        # 1.4 m x 1.2 m
    img = fill(h, w, "b8bdb2")
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    # A curtain behind the glass on the right half: a broad faded floral pink, drawn as flat
    # patches through the frost.
    curtain = np.clip((xx - w * 0.45) / 30 + 0.5, 0, 1)
    img = img * (1 - 0.5 * curtain[..., None]) + hexcol("c79a98") * 0.5 * curtain[..., None]
    img = blobs(img, "d9bcb6", 10, 16, seed=2001, strength=0.35, wrap=False, region=(w * 0.5, 30, w - 30, h - 30))
    # Slats: 12 frosted bands, each with a darker lower lip.
    n = 12
    s = (h - 40) / n
    for k in range(n):
        y0 = 20 + k * s
        lip = np.clip((yy - (y0 + s - 7)) / 2 + 0.5, 0, 1) * (yy < y0 + s)
        img = img * (1 - 0.22 * lip[..., None])
        hi = np.clip(1 - np.abs(yy - (y0 + 4)) / 2.5, 0, 1)
        img = img * (1 + 0.08 * hi[..., None])
    img = paint(img, frame_mask(h, w, (0, 0, w, h), 20, 0), "9ea09a", 2002, 0)                     # aluminium
    img = paint(img, rect_mask(h, w, (w / 2 - 6, 0, w / 2 + 6, h), 0), "9ea09a", 2003, 0)           # mullion
    img = weather(img, 2004, fade=0.04)
    save("sari_jalousie", img)


def sari_door():
    h, w = 820, 360                        # 0.9 m x 2.05 m
    img = fill(h, w, "7a4e32")
    img = coat(img, (1.08, 1.06, 1.04), 100, 0.3, seed=2101, feather=1.2, stretch=(0.3, 1.0))
    grain = wobbly_lines(h, w, 30, 1.8, 8, seed=2102, axis="v")
    img = img * (1 - 0.06 * grain[..., None])
    for r in range(3):
        for c in range(2):
            x0, y0 = 36 + c * 152, 40 + r * 250
            box = (x0, y0, x0 + 136, y0 + 220)
            img = paint(img, frame_mask(h, w, box, 12, 6), "5e3a24", 2103 + r * 2 + c, 0.03)
            inner = rect_mask(h, w, (x0 + 12, y0 + 12, x0 + 124, y0 + 208), 4)
            img = img * (1 + 0.05 * inner[..., None])
    img = paint(img, circle_mask(h, w, w - 44, h * 0.52, 12), "b9a36a", 2110, 0)                    # knob
    edge = h - 70 + 12 * _noise1d(w, 40, 2111)[None, :]
    yy = np.mgrid[0:h, 0:w][0].astype(float)
    kick = np.clip((yy - edge) / 8 + 0.5, 0, 1) * 0.25
    img = img * (1 - kick[..., None]) + hexcol("9c8b76") * kick[..., None]
    save("sari_door", img)


def sari_gate():
    h, w = 540, 660                        # 2.2 m x 1.8 m
    img = fill(h, w, "6d2a2c")
    img = coat(img, (1.08, 1.05, 1.05), 110, 0.3, seed=2201, feather=1.2)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    ribs = (np.abs(((xx + 30) % 110) - 55) < 3).astype(float)
    img = img * (1 - 0.18 * ribs[..., None])
    img = paint(img, frame_mask(h, w, (0, 0, w, h), 18, 0), "5a2224", 2202, 0)
    img = paint(img, text_mask(h, w, ["NO PARKING"], "impact", (70, 120, w - 70, 260), seed=2203, wobble=2.2),
                "ece6d8", 2204, 0.03)
    img = paint(img, text_mask(h, w, ["BAWAL PUMARADA"], "black", (90, 290, w - 90, 360), seed=2205, wobble=2.2),
                "ece6d8", 2206, 0.03)
    edge = h - 80 + 16 * _noise1d(w, 50, 2207)[None, :]
    a = np.clip((yy - edge) / 10 + 0.5, 0, 1) * 0.55
    img = img * (1 - a[..., None]) + hexcol("7c5038") * a[..., None]
    img = drips(img, [120, 330, 520], 18, 70, 7, "7c5038", seed=2208, strength=0.4)
    save("sari_gate", img)


TILING = [sari_plaster, sari_siding, sari_giroof]
ARTWORK = [sari_sign_main, sari_sign_tin, sari_sign_yelo, sari_sign_utang, sari_stock, sari_snacks, sari_jalousie,
           sari_door, sari_gate]


def swatch_sheet(version):
    SHEETS.mkdir(parents=True, exist_ok=True)
    path = SHEETS / f"sari_swatches_v{version}.png"
    if path.exists():
        print("[ilalim-sari-tex] sheet exists, not overwriting:", path)
        return
    tints = {"sari_plaster": "b8d4a8", "sari_siding": "ebd79e"}
    names = [p.__name__ for p in TILING + ARTWORK]
    cell, pad, cols = 320, 16, 4
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (cell + pad) + pad, rows * (cell + pad + 26) + pad), (245, 242, 235))
    draw = ImageDraw.Draw(sheet)
    for k, name in enumerate(names):
        tile = Image.open(OUT / f"{name}.png").convert("RGB")
        if name in tints:
            a = np.asarray(tile) / 255 * hexcol(tints[name]) * 1.05
            tile = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
        if k < len(TILING):
            rep = Image.new("RGB", (tile.width * 2, tile.height * 2))
            for i in range(2):
                for j in range(2):
                    rep.paste(tile, (i * tile.width, j * tile.height))
            tile = rep
        tile.thumbnail((cell, cell), Image.LANCZOS)
        r, c = divmod(k, cols)
        x, y = pad + c * (cell + pad), pad + r * (cell + pad + 26)
        sheet.paste(tile, (x, y))
        draw.text((x, y + tile.height + 4), name, fill=(40, 40, 40))
    sheet.save(path)
    print("[ilalim-sari-tex] sheet", path)


def main():
    for p in TILING + ARTWORK:
        p()
    version = int(sys.argv[sys.argv.index("--sheet") + 1]) if "--sheet" in sys.argv else 1
    swatch_sheet(version)


if __name__ == "__main__":
    main()
