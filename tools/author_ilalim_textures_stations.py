"""Paint the LRT-1 station and street-end textures, in the house illustrated style (ILALIM-1.3,
stations kit).

  py -3 tools/author_ilalim_textures_stations.py [--sheet N]

Writes ArtSource/ilalim/textures/stn_*.png and a swatch sheet
Logs/ilalim-blender/stn_swatches_vN.png (an existing sheet is never overwritten). The stations
and the street-end building rows are modelled by tools/author_ilalim_stations.py.

Owner, 2026-09-30, looking north along Taft from the court: "lrt way ending is visible from the
play area. need to figure out a way to end the view". He chose "Stations + haze": the real LRT-1
stations UNITED NATIONS (north) and PEDRO GIL (south), pulled in close so the guideway runs into
them, and behind each a row of buildings where Taft bends.

The concrete of the stations is the guideway's own (lrt_concrete, lrt_girder, lrt_pier,
lrt_soffit from tools/author_ilalim_textures.py), so the station reads as part of the line.
These are the surfaces the guideway does not have. The drawing helpers are the prop kit's
(tools/author_ilalim_textures_props.py), so the hand is the same as the street's.

THE STYLE (KANTO_DESIGN_GUIDE.md section 3, LAGOON_REWORK_GUIDE.md section 2): FLAT fills, a few
LARGE patches with FEATHERED organic edges, low contrast between a thing and its joints, no grain,
no noise, no streaks, no airbrushed blur. Every surface is its own drawing, its own function.
Thin things (louvre slats, window grilles, shutter ribs) are DRAWN here, never modelled.

  TILING (world UVs, 4 m per 1024 px tile, like the guideway):
    stn_wall     NEUTRAL precast wall panels of the station: two broad coats, a faint vertical
                 joint every 2 m, one broad damp field low down. Tinted cream in the model.
    stn_render   NEUTRAL cement render of the street-end buildings: two coats, a few squarish
                 repaint patches where a crack was filled, a sun-faded field. Tinted per building.
    stn_roof     NEUTRAL standing-seam roof sheet for the barrel roof and the stair canopies:
                 a raised seam every 0.5 m along u, broad weathered fields. Tinted bottle green.
    stn_floor    the platform and concourse floor: pale 0.5 m tiles, joints barely darker, a
                 worn track down the middle of each platform.
  BANDS (u tiles along the wall, v runs 0..1 across the band's height):
    stn_louvre   the big louvred screens of the platform walls, 2 m per bay: a cream frame, a
                 mullion, eleven dark grey-green slats with a pale lip. The slats are drawn.
    stn_windows  the concourse's window band, 3 m per bay: a cream sill and head, a big dark
                 teal-grey pane with one soft reflection band and a transom.
  ONE-OFF artwork (UV 0..1 on one face):
    stn_name_un    UNITED NATIONS / UN AVENUE: white letters on maroon, a white rule, bolts.
    stn_name_pgil  PEDRO GIL / TAFT AVENUE: the same sign system (a line has one sign style).
    stn_window_a   the street-end buildings' casement window: cream frame, dark glass, a
                   curtain, a sill. stn_window_b is a jalousie behind a green grille;
                   stn_window_c a sliding window with laundry-bar and a bottle-green grille.
    stn_shops      a 2 x 2 atlas of ground-floor shopfronts (a half-open roll-up shutter, a
                   hardware shop's open front, a closed shutter with a painted notice, a bakery
                   counter). Regions in SHOP_REGIONS.
    stn_shopsigns  an atlas of eight hand-painted shop boards, each 1024 x 128 (8:1), all
                   invented family names and trades (no real brands). Rows in SIGN_ROWS.

ROLE HUES (Art_Direction.md section 1): nothing near offence orange #f87020 or defence blue
#0080e8. The station boards are maroon, the roofs bottle green, the glass teal-grey, the
shutters grey-green; no mid blue anywhere, no orange.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
from author_ilalim_textures_props import (  # noqa: E402
    OUT, SHEETS, blobs, circle_mask, coat, drips, fill, frame_mask, hexcol, paint, patches, rect_mask,
    save, text_mask, weather, wobbly_lines, _noise1d,
)

BIG = 1024                     # 4 m tiles, 256 px per metre, the guideway's density
SHOP_REGIONS = {"shutter_half": 0, "hardware": 1, "shutter_notice": 2, "bakery": 3}
SIGN_ROWS = ["SANTOS HARDWARE", "ALING NENA BAKERY", "DELA CRUZ OPTICAL", "PRINT · XEROX · LAMINATE",
             "MAGSAYSAY DRUGSTORE", "LUCKY STAR TAILORING", "KAINAN NI MANG TONYO", "REYES ELECTRICAL"]


# ------------------------------------------------------------------ tiling surfaces (4 m tiles)

def stn_wall():
    img = fill(BIG, BIG, "efede8")
    img = coat(img, (0.968, 0.965, 0.958), 260, 0.30, seed=3101, feather=1.2)
    img = coat(img, (1.02, 1.02, 1.018), 180, 0.18, seed=3102, feather=1.0)
    # A precast panel joint every 2 m, faint and hand-drawn.
    joints = wobbly_lines(BIG, BIG, 512, 1.6, 3, seed=3103, axis="v")
    img = img * (1 - 0.05 * joints[..., None])
    # One broad damp field rising from the lower half.
    damp = patches(BIG, BIG, 300, 0.14, 3104, feather=1.0, stretch=(1.0, 0.5))
    yy = np.mgrid[0:BIG, 0:BIG][0] / BIG
    img = img * (1 - 0.05 * (damp * np.clip((yy - 0.1) * 1.2, 0, 1))[..., None])
    save("stn_wall", img)


def stn_render():
    img = fill(BIG, BIG, "eeebe4")
    img = coat(img, (0.962, 0.958, 0.95), 240, 0.32, seed=3201, feather=1.2)
    img = coat(img, (1.025, 1.024, 1.02), 150, 0.2, seed=3202, feather=1.0)
    rng = np.random.default_rng(3203)
    for k in range(4):
        x0, y0 = rng.uniform(30, BIG - 330), rng.uniform(30, BIG - 260)
        box = (x0, y0, x0 + rng.uniform(140, 300), y0 + rng.uniform(100, 230))
        m = rect_mask(BIG, BIG, box, radius=30, feather=10.0)
        img = img * (1 - 0.03 * m[..., None] * (1 if k % 2 else -0.7))
    fade = patches(BIG, BIG, 320, 0.2, 3210, feather=1.0, stretch=(1.0, 0.6))
    img = img * (1 + 0.035 * fade[..., None])
    save("stn_render", img)


def stn_roof():
    img = fill(BIG, BIG, "e6e6e2")
    img = coat(img, (1.04, 1.04, 1.035), 260, 0.28, seed=3301, feather=1.3)
    yy, xx = np.mgrid[0:BIG, 0:BIG].astype(float)
    pitch = BIG / 8                        # a seam every 0.5 m along u
    d = np.abs(((xx + pitch / 2) % pitch) - pitch / 2)
    seam = np.clip((5 - d) / 2.0, 0, 1)
    lit = np.clip((d - 5) / 3.0, 0, 1) * (((xx + pitch / 2) % pitch) < pitch / 2) * np.exp(-((d - 7) / 5) ** 2)
    img = img * (1 - 0.16 * seam[..., None]) * (1 + 0.05 * lit[..., None])
    # Weathering: a broad chalky fade and a few soft rust-brown fields, never spots.
    img = coat(img, (1.05, 1.05, 1.05), 300, 0.22, seed=3302, feather=1.2, stretch=(0.5, 1.0))
    rust = patches(BIG, BIG, 380, 0.07, 3303, feather=1.4, stretch=(0.35, 1.0))
    img = img * (1 - 0.16 * rust[..., None]) + hexcol("b39a84") * 0.16 * rust[..., None]
    save("stn_roof", img)


def stn_floor():
    img = fill(BIG, BIG, "d8d3c8")
    img = coat(img, (1.02, 1.02, 1.018), 320, 0.28, seed=3401, feather=1.4)
    # Tiles 0.5 m square: joints drawn as soft wobbly lines, barely darker.
    joints = np.maximum(wobbly_lines(BIG, BIG, 128, 1.4, 2, seed=3402, axis="h"),
                        wobbly_lines(BIG, BIG, 128, 1.4, 2, seed=3403, axis="v"))
    img = img * (1 - 0.07 * joints[..., None])
    # A few tiles a different batch: flat, whole-tile patches.
    rng = np.random.default_rng(3404)
    for _ in range(7):
        i, j = rng.integers(0, 8, 2)
        m = rect_mask(BIG, BIG, (j * 128 + 3, i * 128 + 3, j * 128 + 125, i * 128 + 125), 4)
        img = img * (1 - 0.04 * m[..., None] * rng.choice([-1, 1]))
    worn = patches(BIG, BIG, 380, 0.2, 3405, feather=1.5, stretch=(0.4, 1.0))
    img = img * (1 - 0.035 * worn[..., None])
    save("stn_floor", img)


# ------------------------------------------------------------------ bands

def stn_louvre():
    w, h = 512, 512                        # one 2 m bay; v spans the whole screen height
    img = fill(h, w, "e9e2d0")             # the cream frame
    img = coat(img, (0.97, 0.965, 0.955), 140, 0.3, seed=3501, feather=1.2)
    x0, x1, y0, y1 = 26, w - 26, 34, h - 40
    panel = rect_mask(h, w, (x0, y0, x1, y1), 6)
    img = img * (1 - panel[..., None]) + hexcol("4e5a50") * panel[..., None]
    n = 11
    s = (y1 - y0) / n
    yy = np.mgrid[0:h, 0:w][0].astype(float)
    for k in range(n):
        top = y0 + k * s
        # The pale lip along the top of each slat, and the shadow under it: two soft bands.
        lip = np.clip(1 - np.abs(yy - (top + 5)) / 4.0, 0, 1) * panel
        shade = np.clip(1 - np.abs(yy - (top + s - 5)) / 5.0, 0, 1) * panel
        img = img * (1 + 0.55 * lip[..., None]) * (1 - 0.25 * shade[..., None])
    img = coat(img, (1.06, 1.06, 1.05), 160, 0.2, seed=3502, feather=1.2)
    img = drips(img, [120, 330, 430], y0, 120, 9, "8a857a", seed=3503, strength=0.25)
    save("stn_louvre", img)


def stn_windows():
    w, h = 768, 512                        # one 3 m bay of the concourse window band
    img = fill(h, w, "ebe6d8")
    img = coat(img, (0.97, 0.965, 0.955), 160, 0.3, seed=3601, feather=1.2)
    x0, x1, y0, y1 = 44, w - 44, 70, h - 90
    glass = rect_mask(h, w, (x0, y0, x1, y1), 8)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    g = hexcol("465e5a") * (1 + 0.10 * ((yy - y0) / (y1 - y0)))[..., None]
    img = img * (1 - glass[..., None]) + g * glass[..., None]
    refl = np.clip(1 - np.abs((xx - yy * 0.9) - 250) / 60, 0, 1) * glass
    img = img * (1 + 0.18 * refl[..., None])
    # The frame: a transom and a centre mullion, cream, drawn.
    for box in ((x0, y0 + 120, x1, y0 + 136), (w / 2 - 8, y0, w / 2 + 8, y1)):
        img = paint(img, rect_mask(h, w, box, 2), "e2dccb", 3602, 0)
    img = paint(img, frame_mask(h, w, (x0 - 6, y0 - 6, x1 + 6, y1 + 6), 14, 8), "ddd6c4", 3603, 0)
    img = paint(img, rect_mask(h, w, (x0 - 16, y1 + 8, x1 + 16, y1 + 30), 4), "d4cdb9", 3604, 0)   # sill
    img = drips(img, [x0 + 40, w / 2 + 60, x1 - 30], y1 + 30, 70, 10, "b9b2a2", seed=3605, strength=0.3)
    save("stn_windows", img)


# ------------------------------------------------------------------ station name boards

def _name_board(name, big, small, seed):
    h, w = 360, 2400                       # 12 m x 1.8 m
    img = fill(h, w, "6b2530")
    img = coat(img, (1.05, 1.03, 1.03), 300, 0.3, seed=seed, feather=1.2)
    img = paint(img, frame_mask(h, w, (18, 18, w - 18, h - 18), 10, 18), "ece6da", seed + 1, 0)
    img = paint(img, text_mask(h, w, [big], "bahn", (130, 40, w - 130, 214), seed=seed + 2, wobble=1.0),
                "f1ece2", seed + 3, 0.02)
    img = paint(img, rect_mask(h, w, (300, 230, w - 300, 238), 3), "ece6da", seed + 4, 0)
    img = paint(img, text_mask(h, w, [small], "bahn", (560, 252, w - 560, 326), seed=seed + 5, wobble=0.8),
                "e9d9b8", seed + 6, 0.02)
    for x in (56, w - 56):
        for y in (56, h - 56):
            img = paint(img, circle_mask(h, w, x, y, 9), "4a1c24", seed + 7, 0)
    img = weather(img, seed + 8, fade=0.05)
    img = drips(img, [70, w - 70, 810, 1600], 64, 120, 10, "4f1b24", seed=seed + 9, strength=0.35)
    return img


def stn_name_un():
    save("stn_name_un", _name_board("stn_name_un", "UNITED NATIONS", "UN AVENUE", 3701))


def stn_name_pgil():
    save("stn_name_pgil", _name_board("stn_name_pgil", "PEDRO GIL", "TAFT AVENUE", 3711))


# ------------------------------------------------------------------ the street-end buildings

def stn_window_a():
    h, w = 410, 360                        # 1.4 m x 1.6 m casement
    img = fill(h, w, "e6dfcc")
    x0, y0, x1, y1 = 26, 26, w - 26, h - 50
    glass = rect_mask(h, w, (x0, y0, x1, y1), 4)
    img = img * (1 - glass[..., None]) + hexcol("3f5552") * glass[..., None]
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    curtain = glass * np.clip((xx - (w * 0.55 + 18 * np.sin(yy / 30))) / 6, 0, 1)
    img = img * (1 - 0.75 * curtain[..., None]) + hexcol("c9a39a") * 0.75 * curtain[..., None]
    img = paint(img, rect_mask(h, w, (w / 2 - 7, y0, w / 2 + 7, y1), 2), "e6dfcc", 3801, 0)
    img = paint(img, rect_mask(h, w, (x0, y0 + 110, x1, y0 + 122), 2), "e6dfcc", 3802, 0)
    img = paint(img, rect_mask(h, w, (6, h - 44, w - 6, h - 20), 3), "cfc6b1", 3803, 0)
    img = drips(img, [60, w - 70], h - 20, 20, 8, "a79f8e", seed=3804, strength=0.3)
    save("stn_window_a", img)


def stn_window_b():
    h, w = 360, 512                        # 2.0 m x 1.4 m jalousie behind a green grille
    img = fill(h, w, "b7bcb1")
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    n = 10
    for k in range(n):
        y0 = 24 + k * (h - 48) / n
        lip = np.clip(1 - np.abs(yy - (y0 + (h - 48) / n - 5)) / 3, 0, 1)
        img = img * (1 - 0.2 * lip[..., None])
    img = paint(img, frame_mask(h, w, (0, 0, w, h), 22, 0), "a2a59d", 3901, 0)
    # The grille: flat bars drawn in bottle green, with a simple diamond in the middle.
    bars = np.zeros((h, w))
    for x in np.linspace(40, w - 40, 7):
        bars = np.maximum(bars, rect_mask(h, w, (x - 5, 10, x + 5, h - 10), 2))
    for y in (h * 0.33, h * 0.66):
        bars = np.maximum(bars, rect_mask(h, w, (10, y - 5, w - 10, y + 5), 2))
    d = np.abs(np.abs(xx - w / 2) + np.abs(yy - h / 2) - 70)
    bars = np.maximum(bars, np.clip((5 - d) / 1.5, 0, 1))
    img = paint(img, bars, "2f4b36", 3902, 0.03)
    img = weather(img, 3903, fade=0.04)
    save("stn_window_b", img)


def stn_window_c():
    h, w = 360, 440                        # 1.7 m x 1.4 m sliding window with a laundry bar
    img = fill(h, w, "e2ddd0")
    x0, y0, x1, y1 = 22, 22, w - 22, h - 22
    glass = rect_mask(h, w, (x0, y0, x1, y1), 3)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    g = hexcol("52665f") * (1 + 0.12 * ((yy - y0) / (y1 - y0)))[..., None]
    img = img * (1 - glass[..., None]) + g * glass[..., None]
    img = paint(img, rect_mask(h, w, (w * 0.5 - 6, y0, w * 0.5 + 6, y1), 2), "d8d2c3", 4001, 0)
    # A towel and a shirt hung on the bar across the lower third: flat shapes.
    img = paint(img, rect_mask(h, w, (40, h * 0.62, 150, h * 0.95), 8), "c96e6e", 4002, 0.03)
    img = paint(img, rect_mask(h, w, (250, h * 0.6, 360, h * 0.9), 10), "e8e2c8", 4003, 0.03)
    img = paint(img, rect_mask(h, w, (10, h * 0.6 - 4, w - 10, h * 0.6 + 4), 2), "2f4b36", 4004, 0)
    img = weather(img, 4005, fade=0.04)
    save("stn_window_c", img)


def _shop(h, w, kind, seed):
    """One shopfront, 3.4 m x 2.8 m."""
    img = fill(h, w, "3b3733")                             # the dim interior
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    if kind in ("shutter_half", "shutter_notice"):
        down = h * (0.45 if kind == "shutter_half" else 1.0)
        sh = (yy < down).astype(float)
        base = np.ones((h, w, 3)) * hexcol("9aa293")
        ribs = np.abs(((yy + 6) % 22) - 11)
        base = base * (1 - 0.12 * np.clip((3 - ribs) / 2, 0, 1))[..., None]
        base = coat(base, (0.94, 0.94, 0.93), 120, 0.3, seed, feather=1.2)
        img = img * (1 - sh[..., None]) + base * sh[..., None]
        img = paint(img, rect_mask(h, w, (0, down - 16, w, down), 2), "6f766a", seed + 1, 0)
        if kind == "shutter_half":
            # Goods under the half-open shutter: crates and sacks, flat shapes.
            for k, (x, c) in enumerate(((40, "c9a45a"), (170, "d8d0bc"), (300, "a8583f"), (420, "d8d0bc"))):
                img = paint(img, rect_mask(h, w, (x, h * 0.72, x + 100, h), 10), c, seed + 2 + k, 0.04)
        else:
            img = paint(img, text_mask(h, w, ["FOR RENT", "inquire inside"], "ink", (70, 110, w - 70, 300),
                                       seed=seed + 3, wobble=2.5), "8e2626", seed + 4, 0.02)
    elif kind == "hardware":
        for k in range(4):                                  # shelves of goods, drawn
            y = 60 + k * 90
            img = paint(img, rect_mask(h, w, (20, y + 70, w - 20, y + 78), 2), "6d5a44", seed + k, 0)
            rng = np.random.default_rng(seed + 10 + k)
            x = 26.0
            while x < w - 60:
                bw = rng.uniform(26, 60)
                c = ["b33a33", "d7b547", "3f6e4a", "d8d0bc", "7a3f6e", "8a8f86"][rng.integers(6)]
                img = paint(img, rect_mask(h, w, (x, y + 70 - rng.uniform(30, 64), x + bw, y + 70), 4), c, int(x), 0)
                x += bw + rng.uniform(4, 12)
    else:                                                   # the bakery counter
        img = paint(img, rect_mask(h, w, (20, h * 0.55, w - 20, h), 6), "d9cfb6", seed, 0.03)
        img = paint(img, rect_mask(h, w, (40, h * 0.58, w - 40, h * 0.8), 6), "566a66", seed + 1, 0)
        for k in range(8):
            cx = 80 + k * 58
            img = paint(img, circle_mask(h, w, cx, h * 0.72, 20), "c9954f", seed + 2 + k, 0)
        img = paint(img, rect_mask(h, w, (20, 40, w - 20, h * 0.5), 6), "4c4038", seed + 20, 0)
    img = paint(img, frame_mask(h, w, (0, 0, w, h), 14, 0), "cfc8b6", seed + 30, 0)
    return weather(img, seed + 31, fade=0.04)


def stn_shops():
    h, w = 420, 512                        # each quarter of the 1024 atlas
    atlas = np.zeros((1024, 1024, 3))
    for kind, k in SHOP_REGIONS.items():
        tile = _shop(h, w, kind, 4100 + 40 * k)
        r, c = divmod(k, 2)
        atlas[r * 512:r * 512 + h, c * 512:c * 512 + w] = tile
    save("stn_shops", atlas)


def stn_shopsigns():
    atlas = np.zeros((1024, 1024, 3))
    styles = [("f0e3bf", "a0282a", "impact"), ("f2ecde", "5a3a22", "print"), ("2f4b36", "efe6cf", "black"),
              ("e8d487", "2b2626", "impact"), ("efe8da", "2f4b36", "black"), ("6b2530", "f1e6cf", "print"),
              ("f2e2b0", "8e2626", "ink"), ("d9ddd2", "3a3a3a", "bahn")]
    for k, text in enumerate(SIGN_ROWS):
        bg, fg, key = styles[k]
        h, w = 128, 1024
        img = fill(h, w, bg)
        img = coat(img, (1.04, 1.03, 1.02), 120, 0.3, seed=4201 + k, feather=1.2)
        img = paint(img, frame_mask(h, w, (6, 6, w - 6, h - 6), 8, 6), fg, 4211 + k, 0)
        img = paint(img, text_mask(h, w, [text], key, (60, 22, w - 60, h - 22), seed=4221 + k, wobble=1.6),
                    fg, 4231 + k, 0.03)
        img = weather(img, 4241 + k, fade=0.05)
        atlas[k * 128:(k + 1) * 128] = img
    save("stn_shopsigns", atlas)


TILING = [stn_wall, stn_render, stn_roof, stn_floor]
BANDS = [stn_louvre, stn_windows]
ARTWORK = [stn_name_un, stn_name_pgil, stn_window_a, stn_window_b, stn_window_c, stn_shops, stn_shopsigns]


def swatch_sheet(version):
    SHEETS.mkdir(parents=True, exist_ok=True)
    path = SHEETS / f"stn_swatches_v{version}.png"
    if path.exists():
        print("[ilalim-stn-tex] sheet exists, not overwriting:", path)
        return
    # Tinted as the kit uses them.
    tints = {"stn_wall": "ece2c8", "stn_render": "d9b8a8", "stn_roof": "5f7f68"}
    names = [p.__name__ for p in TILING + BANDS + ARTWORK]
    cell, pad, cols = 340, 16, 4
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (cell + pad) + pad, rows * (cell + pad + 26) + pad), (245, 242, 235))
    draw = ImageDraw.Draw(sheet)
    for k, name in enumerate(names):
        tile = Image.open(OUT / f"{name}.png").convert("RGB")
        if name in tints:
            a = np.asarray(tile) / 255 * hexcol(tints[name]) * 1.05
            tile = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
        if k < len(TILING) + len(BANDS):
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
    print("[ilalim-stn-tex] sheet", path)


def main():
    for p in TILING + BANDS + ARTWORK:
        p()
    version = int(sys.argv[sys.argv.index("--sheet") + 1]) if "--sheet" in sys.argv else 1
    swatch_sheet(version)


if __name__ == "__main__":
    main()
