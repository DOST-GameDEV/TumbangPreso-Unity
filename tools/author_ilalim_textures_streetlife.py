"""Paint the street-life textures, in the house illustrated style (ILALIM-1.3, street-life kit).

  py -3 tools/author_ilalim_textures_streetlife.py [--sheet N]

Writes ArtSource/ilalim/textures/life_*.png and a swatch sheet
Logs/ilalim-blender/life_swatches_vN.png (an existing sheet is never overwritten). The kit itself
is modelled by tools/author_ilalim_streetlife.py, which reads these files.

Owner, 2026-09-30: "can you think of a way to make the place look more lively, more unique
building shapes etc?", then "proceed". This kit is the STREET LIFE part of that pass: fiesta
banderitas across Padre Faura, parol lanterns (late September is when they go up), parked
jeepneys, tricycles and pedicabs, greeting tarpaulins, potted plants and hand-painted notices.
The drawing helpers (coats, patches, hand-warped lettering, grommets) are the prop kit's, imported
from tools/author_ilalim_textures_props.py, so the street life sits in the same hand as the props
and the sari-sari store.

THE STYLE (KANTO_DESIGN_GUIDE.md section 3, LAGOON_REWORK_GUIDE.md section 2): FLAT fills, a few
LARGE patches with FEATHERED organic edges, low contrast, no grain, no noise, no streaks, no
airbrushed blur. Every surface is its own drawing.

  TILING (world UVs, 2 m per 512 px tile):
    life_bamboo      a dry bamboo pole: straw yellow with a greener cast, a node ring every
                     0.4 m (v = world z / 2, so the rings sit exactly on the model's node bulges
                     at world z = 0.4 k), two broad sun-bleached fields and a few soft dark
                     weathering patches hugging the nodes.
    life_clay        terracotta pot clay, brick brown (kept red-brown, never near offence
                     orange), with broad pale lime-bloom fields and one damp shadow.
  ONE-OFF artwork (UV 0..1 regions, the model's numbers are the same as the ones here):
    life_pennants    the banderitas: a 4 x 2 atlas of 256 px cells, one pennant design per cell
                     (crimson with a cream sun, butter yellow with a crimson band, leaf green with a
                     white star, fuchsia with cream dots, cream with a maroon zigzag, violet with a
                     yellow star, jade with a cream band, maroon with a yellow sun). Each cell has
                     the fold band at its top, where the plastic wraps the rope.
    life_parols      the parol lanterns: a 2 x 2 atlas of 512 px cells, one colourway per cell
                     (crimson and gold, jade and cream, fuchsia and gold, cream and crimson). The
                     star face is drawn to the model's star (outer radius 256 px = 0.42 m, inner
                     0.19 m): a rim band, a lighter papel de hapon field, an inner star, the round
                     centre medallion with a sunburst. The free corner of each cell holds the
                     tail stripes (TAIL box) and the rim colour sample (RIM point).
    life_tarp_birthday   HAPPY 7th BIRTHDAY PRINCESS JOY! (a cake, balloons, confetti), 2.4 x 1.2
    life_tarp_grad       CONGRATULATIONS! an invented UP Manila nursing graduate, maroon and
                         green, 2.0 x 1.2
    life_tarp_fiesta     MALIGAYANG PIYESTA! from the invented Barangay 712 (the street kit's
                         board), 3.0 x 1.0
    life_tarp_rabies     LIBRENG ANTI-RABIES VACCINE, Barangay 671 health centre (the hoop's
                         barangay), with a drawn dog and cat, 2.0 x 1.2
    life_tarp_pasko      MALIGAYANG PASKO AT MANIGONG BAGONG TAON! from the invented Kag. Ruben
                         Dela Paz (the hoop's donor): the "ber months" greeting, 2.4 x 1.0
    life_sign_bedspace   plywood, marker and brush: BEDSPACE FOR RENT, FEMALE ONLY, 0.6 x 0.9
    life_sign_dahan      yellow painted tin: DAHAN-DAHAN / MAY MGA BATA, 0.9 x 0.6
    life_sign_videoke    white plank, red brush: BAWAL MAG-VIDEOKE PAGKATAPOS NG 10PM, 0.9 x 0.45
    life_sign_toda       green board, cream brush: TODA TERMINAL / PILA DITO, 0.9 x 0.5
    life_sign_pedicab    the pedicab's painted side panel: KUYA BOY, a heart and a route, 0.9 x 0.4

ROLE HUES (Art_Direction.md section 1): nothing near offence orange #f87020 or defence blue
#0080e8. The pennants and parols use crimson, butter yellow, leaf green, fuchsia, cream, violet,
jade (a green, well away from mid blue) and maroon; no orange and no mid blue anywhere.
Every name on a tarp or sign is invented; no real brand appears.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
from author_ilalim_textures_props import (  # noqa: E402
    OUT, SHEETS, TILE, circle_mask, coat, drips, fill, frame_mask, hexcol, paint, patches,
    rect_mask, save, smooth, text_mask, weather, _noise1d,
)

# ------------------------------------------------------------------ shared numbers (the model reads the same)

PENNANT_CELL = 256                     # 4 x 2 cells in a 1024 x 512 atlas
PENNANTS = [
    # (base, motif colour, motif)
    ("b8232e", "f0e8d2", "sun"),
    ("efc23c", "b8232e", "band"),
    ("4c9a3e", "f4f0e2", "star"),
    ("d9467f", "f6e6d8", "dots"),
    ("f0e8d2", "7a2032", "zigzag"),
    ("74479b", "efc23c", "star"),
    ("3a9a70", "f0e8d2", "band"),
    ("7a2032", "efc23c", "sun"),
]
PAROL_CELL = 512                       # 2 x 2 cells in a 1024 x 1024 atlas
PAROL_R, PAROL_RI, PAROL_CENTRE = 0.42, 0.19, 0.12   # metres: star outer, inner radius, medallion
PAROLS = [
    # (rim, field, inner star, medallion ring, medallion core, tail a, tail b)
    ("a01f2b", "e0463f", "f2c94a", "f2c94a", "a01f2b", "e0463f", "f2c94a"),
    ("2f7a52", "5fae6e", "f3eedb", "f3eedb", "2f7a52", "f3eedb", "5fae6e"),
    ("a52a66", "dc5c96", "f2c94a", "f2c94a", "7a2050", "f2c94a", "dc5c96"),
    ("efe6cf", "fbf5e4", "c42a33", "c42a33", "f2c94a", "c42a33", "efe6cf"),
]
TAIL = (14, 14, 110, 120)              # px box in each parol cell (x0, y0, x1, y1): the tail stripes
RIM = (455, 40)                        # px point in each parol cell: the rim colour


# ------------------------------------------------------------------ tiling surfaces (2 m tiles)

def life_bamboo():
    img = fill(TILE, TILE, "c9bb72")
    img = coat(img, (1.035, 1.035, 1.02), 170, 0.3, seed=3101, feather=1.6, stretch=(1.0, 0.35))
    img = coat(img, (0.965, 0.975, 0.95), 150, 0.2, seed=3102, feather=1.6, stretch=(1.0, 0.35))
    yy, xx = np.mgrid[0:TILE, 0:TILE].astype(float)
    node = TILE / 5                    # 0.4 m: 102.4 px, five nodes per tile
    # Row 0 is the top of the image; v = 0 is its bottom row, so a node at world z = 0.4 k sits at
    # image row TILE - k * node (mod TILE). The ring: a darker band with a pale ridge just above it.
    d = (TILE - yy) % node
    d = np.minimum(d, node - d)
    wob = 1.5 * smooth(1, TILE, 60, 3103)[0][None, :]
    ring = np.clip((3.2 + wob * 0.3 - d) / 1.6, 0, 1)
    img = img * (1 - 0.34 * ring[..., None]) + hexcol("7d6a3a") * 0.34 * ring[..., None]
    above = ((TILE - yy) % node)
    ridge = np.clip((6.0 - np.abs(above - 7.0)) / 3.0, 0, 1) * 0.5
    img = img * (1 + 0.08 * ridge[..., None])
    # Weathering: soft grey-brown patches that hug a few nodes, broad and few.
    rng = np.random.default_rng(3104)
    for k in range(4):
        cx = rng.uniform(0, TILE)
        cy = TILE - (int(rng.integers(0, 5)) * node) + rng.uniform(-6, 10)
        m = circle_mask(TILE, TILE, cx, cy, rng.uniform(26, 44), feather=14)
        img = img * (1 - 0.1 * m[..., None]) + hexcol("8f8358") * 0.1 * m[..., None]
    save("life_bamboo", img)


def life_clay():
    img = fill(TILE, TILE, "a45a41")
    img = coat(img, (1.03, 1.03, 1.02), 160, 0.3, seed=3201, feather=1.6)
    bloom = patches(TILE, TILE, 170, 0.12, 3202, feather=1.4, stretch=(1.0, 0.6))
    img = img * (1 - 0.2 * bloom[..., None]) + hexcol("d9c9ae") * 0.2 * bloom[..., None]
    damp = patches(TILE, TILE, 150, 0.14, 3203, feather=1.0)
    img = img * (1 - 0.1 * damp[..., None])
    save("life_clay", img)


# ------------------------------------------------------------------ pennants and parols

def _star_poly(cx, cy, R, r, rot=90.0):
    pts = []
    for k in range(10):
        a = np.radians(rot + k * 36)
        rr = R if k % 2 == 0 else r
        pts.append((cx + rr * np.cos(a), cy - rr * np.sin(a)))
    return pts


def poly_mask(h, w, pts, feather=1.2):
    """A feathered polygon mask (a supersampled PIL fill, softened by about a pixel)."""
    S = 2
    im = Image.new("L", (w * S, h * S), 0)
    ImageDraw.Draw(im).polygon([(x * S, y * S) for x, y in pts], fill=255)
    m = np.asarray(im.resize((w, h), Image.LANCZOS), dtype=float) / 255
    return np.clip(ndimage.gaussian_filter(m, feather * 0.5), 0, 1)


def life_pennants():
    c = PENNANT_CELL
    img = np.ones((2 * c, 4 * c, 3))
    for k, (base, motif, kind) in enumerate(PENNANTS):
        r, q = divmod(k, 4)
        cell = fill(c, c, base)
        cell = coat(cell, (1.035, 1.035, 1.03), 90, 0.25, seed=3301 + k, feather=1.6)     # sun-faded
        yy, xx = np.mgrid[0:c, 0:c].astype(float)
        cx, cy = c / 2, c * 0.36
        if kind == "sun":
            cell = paint(cell, circle_mask(c, c, cx, cy, 30), motif, 3310 + k, 0)
            for i in range(8):
                a = i / 8 * np.pi * 2
                cell = paint(cell, circle_mask(c, c, cx + 46 * np.cos(a), cy + 46 * np.sin(a), 8), motif, 3320 + k, 0)
        elif kind == "star":
            cell = paint(cell, poly_mask(c, c, _star_poly(cx, cy, 40, 17)), motif, 3330 + k, 0)
        elif kind == "band":
            cell = paint(cell, rect_mask(c, c, (-4, cy - 16, c + 4, cy + 16), 0, wobble=2.0, seed=3340 + k), motif,
                         3341 + k, 0)
        elif kind == "dots":
            for (dx, dy) in ((-40, -14), (40, -14), (0, 10), (-22, 44), (22, 44), (0, 80)):
                cell = paint(cell, circle_mask(c, c, cx + dx, cy + dy, 11), motif, 3350 + k, 0)
        elif kind == "zigzag":
            zz = cy + 14 * np.abs(((xx / 28) % 2) - 1) - 7
            m = np.clip((10 - np.abs(yy - zz)) / 1.4, 0, 1)
            cell = paint(cell, m, motif, 3360 + k, 0)
        # The fold band at the top where the plastic wraps the rope: a darker flat strip with a pale
        # crease line under it.
        fold = np.clip((30 - yy) / 1.5 + 0.5, 0, 1)
        cell = cell * (1 - 0.18 * fold[..., None])
        crease = np.clip((3 - np.abs(yy - 33)) / 1.2, 0, 1)
        cell = cell * (1 + 0.1 * crease[..., None])
        img[r * c:(r + 1) * c, q * c:(q + 1) * c] = cell
    save("life_pennants", img)


def life_parols():
    c = PAROL_CELL
    img = np.ones((2 * c, 2 * c, 3))
    px_per_m = (c / 2) / PAROL_R
    for k, (rim, field, inner, ring, core, ta, tb) in enumerate(PAROLS):
        r, q = divmod(k, 2)
        cell = fill(c, c, rim)
        cx = cy = c / 2
        R, Ri = PAROL_R * px_per_m, PAROL_RI * px_per_m
        # The field: the star shrunk by the rim band (the bamboo frame wrapped in paper).
        f = poly_mask(c, c, _star_poly(cx, cy, R * 0.84, Ri * 0.8))
        cell = paint(cell, f, field, 3401 + k, 0.04)
        cell = coat(cell, (1.06, 1.06, 1.04), 70, 0.3, seed=3410 + k, feather=1.2)
        # The inner star, and a paler glow patch inside it (light through the paper).
        cell = paint(cell, poly_mask(c, c, _star_poly(cx, cy, R * 0.56, Ri * 0.55)), inner, 3420 + k, 0.03)
        glow = circle_mask(c, c, cx, cy, R * 0.34, feather=18)
        cell = cell * (1 - 0.12 * glow[..., None]) + np.ones(3) * 0.12 * glow[..., None]
        # The medallion: ring, core and a sunburst of chunky dots round it.
        rc = PAROL_CENTRE * px_per_m
        cell = paint(cell, circle_mask(c, c, cx, cy, rc), ring, 3430 + k, 0)
        cell = paint(cell, circle_mask(c, c, cx, cy, rc * 0.7), core, 3431 + k, 0)
        cell = paint(cell, circle_mask(c, c, cx, cy, rc * 0.32), ring, 3432 + k, 0)
        for i in range(10):
            a = i / 10 * np.pi * 2 + np.pi / 2
            cell = paint(cell, circle_mask(c, c, cx + rc * 1.35 * np.cos(a), cy - rc * 1.35 * np.sin(a), 9), ring,
                         3440 + k, 0)
        # The tail stripes (TAIL box): broad bands across the tail, alternating its two colours.
        x0, y0, x1, y1 = TAIL
        yy = np.mgrid[0:c, 0:c][0].astype(float)
        band = ((yy - y0) // 18) % 2
        m = rect_mask(c, c, TAIL, 0, feather=0.6)
        cell = paint(cell, m * (band == 0), ta, 3450 + k, 0)
        cell = paint(cell, m * (band == 1), tb, 3451 + k, 0)
        # The rim sample stays the rim colour (the base fill); keep it clean.
        cell = paint(cell, circle_mask(c, c, RIM[0], RIM[1], 30), rim, 3460 + k, 0)
        img[r * c:(r + 1) * c, q * c:(q + 1) * c] = cell
    save("life_parols", img)


# ------------------------------------------------------------------ tarpaulins (digital prints, faded)

def grommets(img, inset=22, r=12):
    h, w = img.shape[:2]
    for gx, gy in ((inset, inset), (w - inset, inset), (inset, h - inset), (w - inset, h - inset),
                   (w / 2, inset), (w / 2, h - inset)):
        img = paint(img, circle_mask(h, w, gx, gy, r), "d8d3c8", 0, 0)
        img = paint(img, circle_mask(h, w, gx, gy, r * 0.5), "1d1c1e", 0, 0)
    return img


def life_tarp_birthday():
    h, w = 600, 1200                   # 2.4 x 1.2 m
    img = fill(h, w, "f4cbd6")
    img = coat(img, (1.04, 1.03, 1.03), 180, 0.35, seed=3501, feather=1.4)
    rng = np.random.default_rng(3502)
    for _ in range(38):                # big soft confetti dots
        col = ["f2c94a", "5fae6e", "a867c9", "ffffff", "e0463f"][int(rng.integers(5))]
        img = paint(img, circle_mask(h, w, rng.uniform(0, w), rng.uniform(0, h), rng.uniform(6, 13)), col, 0, 0)
    img = paint(img, rect_mask(h, w, (40, 40, w - 40, h - 40), 30), "fbeef2", 3503, 0.02)
    img = paint(img, text_mask(h, w, ["HAPPY 7th BIRTHDAY"], "impact", (330, 70, 960, 230), wobble=0), "b8336e",
                3504, 0)
    img = paint(img, text_mask(h, w, ["Princess Joy!"], "print", (380, 240, 940, 390), wobble=0), "6b3d8f", 3505, 0)
    img = paint(img, text_mask(h, w, ["SEPT 27, 2026  ·  12 NOON  ·  SA BAHAY NI LOLA ISING"], "bahn",
                               (380, 430, 940, 480), wobble=0), "6d4a5a", 3506, 0)
    # The cake: three flat tiers, a drip of icing, seven candles with flames.
    cx = 190
    for i, (tw, th, col) in enumerate(((230, 90, "f3e3c6"), (180, 80, "e98fb0"), (130, 70, "f3e3c6"))):
        y1 = 520 - sum(t[1] for t in ((230, 90), (180, 80), (130, 70))[:i])
        img = paint(img, rect_mask(h, w, (cx - tw / 2, y1 - th, cx + tw / 2, y1), 14), col, 3510 + i, 0)
        img = paint(img, rect_mask(h, w, (cx - tw / 2, y1 - th, cx + tw / 2, y1 - th + 16), 8), "fbf6ee", 3514 + i, 0)
    for i in range(7):
        x = cx - 54 + i * 18
        img = paint(img, rect_mask(h, w, (x - 4, 208, x + 4, 280), 3), "a867c9", 3520, 0)
        img = paint(img, circle_mask(h, w, x, 198, 7), "f2c94a", 3521, 0)
    # Balloons at the right end: flat ovals with a knot and a painted string.
    for i, (bx, by, col) in enumerate(((1060, 170, "e0463f"), (1120, 240, "f2c94a"), (1000, 250, "a867c9"))):
        yy, xx = np.mgrid[0:h, 0:w].astype(float)
        m = np.clip((1 - ((xx - bx) / 44) ** 2 - ((yy - by) / 56) ** 2) * 30, 0, 1)
        img = paint(img, m, col, 3530 + i, 0)
        img = paint(img, rect_mask(h, w, (bx - 1.5, by + 56, bx + 1.5, by + 200), 1), "8b7d80", 3535, 0)
    img = grommets(img)
    img = weather(img, 3540, fade=0.05, dirt_bottom=False)
    save("life_tarp_birthday", img)


def life_tarp_grad():
    h, w = 600, 1000                   # 2.0 x 1.2 m
    img = fill(h, w, "7a1f2e")
    img = coat(img, (1.1, 1.08, 1.08), 170, 0.3, seed=3601, feather=1.4)
    img = paint(img, rect_mask(h, w, (0, h - 110, w, h)), "2f6a45", 3602, 0.02)
    img = paint(img, rect_mask(h, w, (0, h - 118, w, h - 110)), "e6c64f", 3603, 0)
    img = paint(img, text_mask(h, w, ["CONGRATULATIONS!"], "impact", (60, 40, w - 60, 160), wobble=0), "e6c64f", 3604, 0)
    img = paint(img, text_mask(h, w, ["MA. KRISTINE D. VILLAROMAN"], "black", (300, 190, w - 50, 260), wobble=0),
                "f4efe2", 3605, 0)
    img = paint(img, text_mask(h, w, ["Bachelor of Science in Nursing", "Cum Laude  ·  Batch 2026"], "bahn",
                               (300, 280, w - 60, 400), wobble=0, spacing=0.25), "e8dcc0", 3606, 0)
    img = paint(img, text_mask(h, w, ["Proud of you! Papa, Mama, Kuya Jomar at Lola Ising"], "print",
                               (80, h - 100, w - 80, h - 24), wobble=0), "f4efe2", 3607, 0)
    # The mortarboard: a flat diamond board, the cap under it and a gold tassel.
    cx, cy = 160, 300
    img = paint(img, poly_mask(h, w, [(cx - 110, cy - 20), (cx, cy - 70), (cx + 110, cy - 20), (cx, cy + 30)]),
                "1c1a1d", 3610, 0)
    img = paint(img, rect_mask(h, w, (cx - 62, cy, cx + 62, cy + 70), 20), "2a2729", 3611, 0)
    img = paint(img, rect_mask(h, w, (cx + 70, cy - 22, cx + 78, cy + 60), 3), "e6c64f", 3612, 0)
    img = paint(img, circle_mask(h, w, cx + 74, cy + 66, 12), "e6c64f", 3613, 0)
    img = grommets(img)
    img = weather(img, 3620, fade=0.05, dirt_bottom=False)
    save("life_tarp_grad", img)


def life_tarp_fiesta():
    h, w = 400, 1200                   # 3.0 x 1.0 m
    img = fill(h, w, "f3ead4")
    img = coat(img, (0.97, 0.96, 0.94), 160, 0.3, seed=3701, feather=1.4)
    # Printed banderitas across the top: a drawn rope and flat triangles, alternating colours.
    cols = ["b8232e", "efc23c", "4c9a3e", "d9467f", "74479b"]
    for i in range(24):
        x = 25 + i * 48
        img = paint(img, poly_mask(h, w, [(x - 20, 36), (x + 20, 36), (x, 84)]), cols[i % len(cols)], 0, 0)
    img = paint(img, rect_mask(h, w, (0, 32, w, 38)), "8a6e4a", 0, 0)
    img = paint(img, text_mask(h, w, ["MALIGAYANG PIYESTA!"], "impact", (60, 100, w - 60, 240), wobble=0), "b8232e",
                3702, 0)
    img = paint(img, text_mask(h, w, ["BARANGAY 712  ·  ZONE 78  ·  DISTRITO V  ·  ERMITA"], "black",
                               (120, 256, w - 120, 300), wobble=0), "2f5e3a", 3703, 0)
    img = paint(img, text_mask(h, w, ["Mula kay Kap. Nenita \"Nita\" Bautista-Reyes at mga Kagawad"], "bahn",
                               (160, 322, w - 160, 360), wobble=0), "5a4a3a", 3704, 0)
    img = grommets(img, inset=18, r=10)
    img = weather(img, 3710, fade=0.04, dirt_bottom=False)
    save("life_tarp_fiesta", img)


def life_tarp_rabies():
    h, w = 600, 1000                   # 2.0 x 1.2 m
    img = fill(h, w, "f1efe6")
    img = paint(img, rect_mask(h, w, (0, 0, w, 150)), "2f7a52", 3801, 0.02)
    img = paint(img, text_mask(h, w, ["LIBRENG ANTI-RABIES VACCINE!"], "impact", (40, 26, w - 40, 126), wobble=0),
                "f6f2e4", 3802, 0)
    img = paint(img, text_mask(h, w, ["PARA SA ASO AT PUSA", "3 BUWAN PATAAS"], "black", (380, 180, w - 50, 330),
                               wobble=0, spacing=0.3), "2f5e3a", 3803, 0)
    img = paint(img, text_mask(h, w, ["TUWING SABADO  ·  8AM - 12NN", "BARANGAY 671 HEALTH CENTER"], "bahn",
                               (380, 360, w - 50, 460), wobble=0, spacing=0.3), "5a4a3a", 3804, 0)
    img = paint(img, rect_mask(h, w, (0, h - 70, w, h)), "a0282a", 3805, 0)
    img = paint(img, text_mask(h, w, ["MAGDALA NG VACCINATION CARD"], "black", (200, h - 60, w - 200, h - 16),
                               wobble=0), "f6f2e4", 3806, 0)
    # A flat dog (head, ears, snout) and a flat cat beside it.
    dx, dy = 150, 330
    img = paint(img, circle_mask(h, w, dx, dy, 90), "c89a62", 3810, 0)
    img = paint(img, poly_mask(h, w, [(dx - 90, dy - 60), (dx - 60, dy - 120), (dx - 30, dy - 70)]), "7a5638", 3811, 0)
    img = paint(img, poly_mask(h, w, [(dx + 90, dy - 60), (dx + 60, dy - 120), (dx + 30, dy - 70)]), "7a5638", 3812, 0)
    img = paint(img, circle_mask(h, w, dx, dy + 40, 42), "efe0c6", 3813, 0)
    img = paint(img, circle_mask(h, w, dx, dy + 22, 13), "2a2426", 3814, 0)
    for ex in (-34, 34):
        img = paint(img, circle_mask(h, w, dx + ex, dy - 20, 10), "2a2426", 3815, 0)
    cx, cy = 300, 400
    img = paint(img, circle_mask(h, w, cx, cy, 55), "8b8b88", 3820, 0)
    img = paint(img, poly_mask(h, w, [(cx - 52, cy - 20), (cx - 44, cy - 84), (cx - 12, cy - 50)]), "8b8b88", 3821, 0)
    img = paint(img, poly_mask(h, w, [(cx + 52, cy - 20), (cx + 44, cy - 84), (cx + 12, cy - 50)]), "8b8b88", 3822, 0)
    for ex in (-20, 20):
        img = paint(img, circle_mask(h, w, cx + ex, cy - 8, 8), "e6c64f", 3823, 0)
    img = grommets(img)
    img = weather(img, 3830, fade=0.06, dirt_bottom=False)
    save("life_tarp_rabies", img)


def life_tarp_pasko():
    h, w = 500, 1200                   # 2.4 x 1.0 m
    img = fill(h, w, "8e1d27")
    img = coat(img, (1.08, 1.06, 1.06), 170, 0.3, seed=3901, feather=1.4)
    rng = np.random.default_rng(3902)
    for _ in range(26):                # flat snow-dot stars
        img = paint(img, circle_mask(h, w, rng.uniform(0, w), rng.uniform(0, h * 0.7), rng.uniform(4, 8)), "f3e7cf", 0, 0)
    img = paint(img, rect_mask(h, w, (0, h - 120, w, h)), "2f6a45", 3903, 0.02)
    img = paint(img, text_mask(h, w, ["MALIGAYANG PASKO"], "impact", (300, 40, w - 60, 170), wobble=0), "f2c94a", 3904, 0)
    img = paint(img, text_mask(h, w, ["at Manigong Bagong Taon!"], "print", (320, 180, w - 80, 290), wobble=0),
                "f6f0e0", 3905, 0)
    img = paint(img, text_mask(h, w, ["Taos-pusong pagbati mula kay Kag. Ruben Dela Paz  ·  Barangay 671"], "bahn",
                               (300, h - 96, w - 60, h - 40), wobble=0), "f6f0e0", 3906, 0)
    # A printed parol at the left: star, medallion, two tails.
    cx, cy = 150, 170
    img = paint(img, poly_mask(h, w, _star_poly(cx, cy, 110, 50)), "f2c94a", 3910, 0)
    img = paint(img, poly_mask(h, w, _star_poly(cx, cy, 70, 32)), "e0463f", 3911, 0)
    img = paint(img, circle_mask(h, w, cx, cy, 26), "f2c94a", 3912, 0)
    for sx in (-40, 40):
        img = paint(img, rect_mask(h, w, (cx + sx - 8, cy + 90, cx + sx + 8, cy + 190), 6), "f2c94a", 3913, 0)
    img = grommets(img, inset=20, r=11)
    img = weather(img, 3920, fade=0.05, dirt_bottom=False)
    save("life_tarp_pasko", img)


# ------------------------------------------------------------------ hand-painted notices

def life_sign_bedspace():
    h, w = 540, 360                    # 0.6 x 0.9 m plywood
    img = fill(h, w, "e8dcc0")
    img = coat(img, (0.95, 0.93, 0.9), 70, 0.3, seed=4001, feather=1.2)
    img = paint(img, text_mask(h, w, ["BEDSPACE"], "black", (20, 24, w - 20, 110), seed=4002, wobble=2.4), "a0282a", 4003,
                0.03)
    img = paint(img, text_mask(h, w, ["FOR RENT"], "black", (40, 120, w - 40, 180), seed=4004, wobble=2.4), "2b2626",
                4005, 0.03)
    img = paint(img, text_mask(h, w, ["FEMALE ONLY", "P3,500 / BUWAN", "MAY WIFI"], "ink", (30, 200, w - 30, 400),
                               seed=4006, wobble=2.6, spacing=0.2, stroke=1), "2b2626", 4007, 0.03)
    img = paint(img, text_mask(h, w, ["Inquire: Aling Baby"], "ink", (30, 420, w - 30, 490), seed=4008, wobble=2.6),
                "a0282a", 4009, 0.03)
    for (x, y) in ((16, 16), (w - 16, 16), (16, h - 16), (w - 16, h - 16)):
        img = paint(img, circle_mask(h, w, x, y, 5), "4a3226", 0, 0)
    img = weather(img, 4010, fade=0.04, dirt_bottom=False)
    save("life_sign_bedspace", img)


def life_sign_dahan():
    h, w = 360, 540                    # 0.9 x 0.6 m painted tin
    img = fill(h, w, "e7c24a")
    img = coat(img, (1.04, 1.03, 1.0), 90, 0.3, seed=4101, feather=1.2)
    img = paint(img, frame_mask(h, w, (12, 12, w - 12, h - 12), 16, 10, 1.6, 4102), "1f1d1e", 4103, 0)
    img = paint(img, text_mask(h, w, ["DAHAN-DAHAN"], "impact", (40, 40, w - 40, 170), seed=4104, wobble=2.0), "1f1d1e",
                4105, 0.02)
    img = paint(img, text_mask(h, w, ["MAY MGA BATA"], "black", (60, 190, w - 60, 300), seed=4106, wobble=2.0), "a0282a",
                4107, 0.02)
    for (x, y) in ((30, 30), (w - 30, 30), (30, h - 30), (w - 30, h - 30)):
        img = paint(img, circle_mask(h, w, x, y, 6), "4a3226", 0, 0)
        img = drips(img, [x], y, 40, 6, "8a5a3c", seed=4110 + x, strength=0.35)
    img = weather(img, 4111, fade=0.06)
    save("life_sign_dahan", img)


def life_sign_videoke():
    h, w = 270, 540                    # 0.9 x 0.45 m plank
    img = fill(h, w, "efece2")
    img = coat(img, (0.96, 0.95, 0.93), 80, 0.3, seed=4201, feather=1.2)
    img = paint(img, text_mask(h, w, ["BAWAL MAG-VIDEOKE"], "black", (24, 20, w - 24, 110), seed=4202, wobble=2.0),
                "a0282a", 4203, 0.03)
    img = paint(img, text_mask(h, w, ["PAGKATAPOS NG 10PM"], "black", (50, 124, w - 50, 180), seed=4204, wobble=2.0),
                "2b2626", 4205, 0.03)
    img = paint(img, text_mask(h, w, ["- Brgy. 712"], "ink", (w - 230, 196, w - 30, 250), seed=4206, wobble=2.4), "2b2626",
                4207, 0.03)
    img = weather(img, 4208, fade=0.04, dirt_bottom=False)
    save("life_sign_videoke", img)


def life_sign_toda():
    h, w = 300, 540                    # 0.9 x 0.5 m
    img = fill(h, w, "2f5e3a")
    img = coat(img, (1.08, 1.07, 1.05), 90, 0.3, seed=4301, feather=1.2)
    img = paint(img, frame_mask(h, w, (10, 10, w - 10, h - 10), 12, 8, 1.4, 4302), "e8dcc0", 4303, 0)
    img = paint(img, text_mask(h, w, ["TODA TERMINAL"], "impact", (40, 30, w - 40, 130), seed=4304, wobble=1.8),
                "f1e8cf", 4305, 0.02)
    img = paint(img, text_mask(h, w, ["PILA DITO", "TAFT - P. FAURA - L. GUINTO"], "black", (50, 150, w - 50, 270),
                               seed=4306, wobble=1.8, spacing=0.25), "efc23c", 4307, 0.02)
    img = weather(img, 4308, fade=0.06)
    save("life_sign_toda", img)


def life_sign_pedicab():
    h, w = 240, 540                    # 0.9 x 0.4 m
    img = fill(h, w, "3f6e4a")
    img = coat(img, (1.08, 1.07, 1.05), 90, 0.3, seed=4401, feather=1.2)
    img = paint(img, rect_mask(h, w, (0, 0, w, 36)), "efc23c", 4402, 0)
    img = paint(img, rect_mask(h, w, (0, h - 36, w, h)), "efc23c", 4403, 0)
    img = paint(img, text_mask(h, w, ["KUYA BOY"], "impact", (150, 50, w - 30, 150), seed=4404, wobble=1.6), "f4efe2",
                4405, 0.02)
    img = paint(img, text_mask(h, w, ["P. FAURA - U.N."], "black", (160, 156, w - 40, 196), seed=4406, wobble=1.6),
                "efc23c", 4407, 0.02)
    # A painted heart with an arrow through it, the classic pedicab flourish.
    cx, cy = 80, 120
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    u, v = (xx - cx) / 48, -(yy - cy) / 48
    heart = np.clip(-((u * u + v * v - 1) ** 3 - u * u * v ** 3) * 40, 0, 1)
    img = paint(img, heart, "b8232e", 4410, 0)
    img = paint(img, rect_mask(h, w, (cx - 70, cy - 4, cx + 70, cy + 4), 2), "f4efe2", 4411, 0)
    img = weather(img, 4412, fade=0.06)
    save("life_sign_pedicab", img)


TILING = [life_bamboo, life_clay]
ARTWORK = [life_pennants, life_parols, life_tarp_birthday, life_tarp_grad, life_tarp_fiesta, life_tarp_rabies,
           life_tarp_pasko, life_sign_bedspace, life_sign_dahan, life_sign_videoke, life_sign_toda, life_sign_pedicab]


def swatch_sheet(version):
    SHEETS.mkdir(parents=True, exist_ok=True)
    path = SHEETS / f"life_swatches_v{version}.png"
    if path.exists():
        print("[ilalim-life-tex] sheet exists, not overwriting:", path)
        return
    names = [p.__name__ for p in TILING + ARTWORK]
    cell, pad, cols = 340, 16, 4
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (cell + pad) + pad, rows * (cell + pad + 26) + pad), (245, 242, 235))
    draw = ImageDraw.Draw(sheet)
    for k, name in enumerate(names):
        tile = Image.open(OUT / f"{name}.png").convert("RGB")
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
    print("[ilalim-life-tex] sheet", path)


def main():
    for p in TILING + ARTWORK:
        p()
    version = int(sys.argv[sys.argv.index("--sheet") + 1]) if "--sheet" in sys.argv else 1
    swatch_sheet(version)


if __name__ == "__main__":
    main()
