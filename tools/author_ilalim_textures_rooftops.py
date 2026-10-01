"""Paint the rooftop kit's textures, in the house illustrated style (ILALIM-1.3, rooftops kit).

  py -3 tools/author_ilalim_textures_rooftops.py [--sheet N]

Writes ArtSource/ilalim/textures/roof_*.png and a swatch sheet
Logs/ilalim-blender/roof_swatches_vN.png. The rooftop clutter itself is modelled and placed by
tools/author_ilalim_rooftops.py, which reads these files.

Owner, 2026-09-30: "can you think of a way to make the place look more lively, more unique building
shapes etc?", then "proceed". The skyline over the east side was flat boxes; this kit puts Manila's
rooftop life on the flat roofs: water tanks on steel stands, plywood-and-GI shacks, hollow-block
penthouse rooms, laundry lines, billboards on lattice legs, stair bulkheads, satellite dishes, a
cell mast and potted gardens. The drawing helpers (coats, patches, hand-warped lettering) are the
prop kit's, imported from tools/author_ilalim_textures_props.py, so the roofs sit in the same hand
as the street props and the sari-sari store.

THE STYLE (KANTO_DESIGN_GUIDE.md section 3, LAGOON_REWORK_GUIDE.md section 2): FLAT fills, a few
LARGE patches with FEATHERED organic edges, low contrast, no grain, no noise, no streaks, no
airbrushed blur. Every surface is its own drawing, written here as its own function.

  TILING (world UVs, 2 m per 512 px tile, the prop kit's TILE_M):
    roof_plywood    plywood sheets, 1 m x 2 m on the tile: each sheet its own warm value, two or three
                    broad soft veneer arcs per sheet, a grey weathered sheet, one leftover square of
                    old green paint. The joints are soft dark lines.
    roof_gi         NEUTRAL painted corrugated GI: ribs every 7.7 cm (they run down the slope, along
                    v), two big fields where the paint has worn back to galvanised grey, and a few
                    broad rust tongues running downslope. Tinted red oxide or faded green per material.
    roof_block      NEUTRAL painted hollow block: faint wobbly coursing (0.2 m courses, 0.4 m blocks,
                    running bond) at very low contrast under two broad coats.
    roof_render     the bulkheads' grey cement render: broad trowel-pass fields, squarish patch
                    repairs, a big damp field. No arcs (those are the prop kit's concrete).
    roof_tank       NEUTRAL moulded polyethylene: two broad sun-faded fields and a dust cap. The
                    moulded ribs are geometry, never drawn.
    roof_galv       galvanised sheet steel (cabinets, the galvanised tank): cool grey, a soft seam
                    band every metre, broad dull and white-rust fields.
  ONE-OFF artwork (UV 0..1 on one face):
    roof_ad_soda      a HAND-PAINTED billboard, 8 m x 4 m: SARAP-LAMIG CALAMANSI SODA, a giant painted
                      bottle and three calamansi halves, cream on bottle green. Brush lettering.
    roof_ad_kape      a FADED PRINT billboard, 8 m x 4 m: KAPE NI LOLA NENA / 3-IN-1 / P8 LANG, a big
                      steaming mug, printed maroon and mustard on cream, sun-bleached in broad fields.
    roof_ad_hardware  a HAND-PAINTED sign board, 6 m x 3 m: MANG CALOY HARDWARE / HOLLOW BLOCKS - GI
                      SHEETS - PINTURA / PADRE FAURA, red letters on a yellow ground, a painted border.
    roof_board_back   the back of every billboard panel: grey steel sheet with the drawn stiffener
                      frame and a rust bloom or two.
    roof_lattice      RGBA CUTOUT: one 0.6 m bay of an angle-iron lattice (two fat chords, a fat
                      diagonal and a strut), painted rust-grey. Tiled up the billboard legs and the
                      cell mast, so the lattice is drawn, never modelled as thin members.
    roof_cloth        the laundry atlas, 4 x 2 cells: maroon shirt, mustard shirt, white sando,
                      green-striped towel, floral duster, red-and-cream checked blanket, khaki shorts,
                      lilac pillowcase. The garment outlines are the model's; each cell is a flat
                      drawing that fills its cell.
    roof_door         the shacks' plywood door: a painted number 3, a hasp and kick scuffs.
    roof_window       the penthouse window: an aluminium frame, a curtain, and a chunky painted grille.

ROLE HUES (Art_Direction.md section 1): nothing near offence orange #f87020 or defence blue #0080e8.
No blue tank, no blue tarp, no denim. The greens are bottle and olive (yellow-leaning), the reds
brick, crimson and maroon, the yellows mustard.
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


def save_rgba(name, rgb, alpha):
    OUT.mkdir(parents=True, exist_ok=True)
    a = np.dstack([np.clip(rgb, 0, 1), np.clip(alpha, 0, 1)[..., None]])
    Image.fromarray((a * 255).astype(np.uint8), "RGBA").save(OUT / f"{name}.png")
    print("[ilalim-roof-tex]", name)


# ------------------------------------------------------------------ tiling surfaces (2 m tiles)

def roof_plywood():
    img = fill(TILE, TILE, "b89a74")
    yy, xx = np.mgrid[0:TILE, 0:TILE].astype(float)
    half = TILE // 2                       # two 1 m x 2 m sheets on the tile
    rng = np.random.default_rng(3101)
    tones = ["b89a74", "a98b69", "b3a08a"]  # warm, darker warm, grey weathered
    for k in range(2):
        sheet = (xx >= k * half) & (xx < (k + 1) * half)
        img[sheet] = hexcol(tones[k + (1 if k == 1 and rng.uniform() < 0.5 else 0)])
        # The veneer figure: one or two broad, soft, vertically stretched fields per sheet, a
        # touch darker. (Thin veneer arcs read as scribbles, swatches v1.)
        m = patches(TILE, TILE, 70, 0.22, 3110 + k, feather=1.2, stretch=(1.0, 0.35)) * sheet
        img = img * (1 - 0.06 * m[..., None])
    # A leftover square of old green paint on the second sheet, and the sheets' soft joints.
    img = paint(img, rect_mask(TILE, TILE, (half + 50, 300, half + 190, 420), 6, 3.0, 3102, 2.5), "76855a", 3103, 0.05)
    for x in (0, half):
        a = np.clip((3.0 - np.abs(((xx - x + TILE / 2) % TILE) - TILE / 2)) / 1.5, 0, 1)
        img = img * (1 - 0.3 * a[..., None])
    a = np.clip((3.0 - np.minimum(yy, TILE - yy)) / 1.5, 0, 1)
    img = img * (1 - 0.3 * a[..., None])
    img = coat(img, (0.92, 0.91, 0.9), 150, 0.2, seed=3104, feather=1.2)      # a broad damp field
    save("roof_plywood", img)


def roof_gi():
    img = fill(TILE, TILE, "ecebe6")
    img = coat(img, (0.96, 0.955, 0.95), 130, 0.3, seed=3201, feather=1.2)
    yy, xx = np.mgrid[0:TILE, 0:TILE].astype(float)
    pitch = TILE / 26
    rib = 0.5 + 0.5 * np.cos((xx % pitch) / pitch * 2 * np.pi)
    img = img * (0.94 + 0.08 * rib[..., None])
    # Paint worn back to galvanised grey: two big feathered fields, stretched down the slope.
    worn = patches(TILE, TILE, 230, 0.12, 3202, feather=1.2, stretch=(1.0, 0.5))
    img = img * (1 - worn[..., None]) + hexcol("9fa19d") * worn[..., None] * (0.94 + 0.08 * rib[..., None])
    rust = patches(TILE, TILE, 200, 0.05, 3203, feather=1.1, stretch=(1.0, 0.45))
    img = img * (1 - 0.45 * rust[..., None]) + hexcol("8a5a3c") * 0.45 * rust[..., None]
    img = drips(img, list(np.random.default_rng(3204).uniform(0, TILE, 5)), 0, 170, 7, "7c5038", seed=3205,
                strength=0.35)
    save("roof_gi", img)


def roof_block():
    img = fill(TILE, TILE, "eeede9")
    yy, xx = np.mgrid[0:TILE, 0:TILE].astype(float)
    course = TILE / 10                     # 0.2 m courses
    block = TILE / 5                       # 0.4 m blocks
    wob = smooth(TILE, TILE, 60, 3301) * 2.0
    d = (yy + wob) % course
    bed = np.exp(-(np.minimum(d, course - d) / 1.6) ** 2)
    row = ((yy + wob) // course) % 2
    xs = (xx + wob * 0.7 + row * block / 2) % block
    head = np.exp(-(np.minimum(xs, block - xs) / 1.6) ** 2)
    img = img * (1 - 0.06 * np.maximum(bed, head)[..., None])
    img = coat(img, (0.965, 0.96, 0.955), 120, 0.3, seed=3302, feather=1.2)
    img = coat(img, (1.02, 1.02, 1.02), 80, 0.2, seed=3303, feather=1.1)
    save("roof_block", img)


def roof_render():
    img = fill(TILE, TILE, "b3afa6")
    # Trowel passes: broad lozenge fields, each a touch lighter or darker, stretched sideways.
    img = coat(img, (1.035, 1.035, 1.03), 170, 0.25, seed=3401, feather=1.3, stretch=(0.5, 1.0))
    img = coat(img, (0.965, 0.965, 0.96), 150, 0.18, seed=3402, feather=1.3, stretch=(0.5, 1.0))
    rng = np.random.default_rng(3403)
    for k in range(2):
        x0, y0 = rng.uniform(30, TILE - 170), rng.uniform(30, TILE - 150)
        box = (x0, y0, x0 + rng.uniform(90, 150), y0 + rng.uniform(70, 120))
        m = rect_mask(TILE, TILE, box, 10, 5.0, 3404 + k, 2.5)
        img = img * (1 + 0.06 * m[..., None] * (1 if k else -1))
    damp = patches(TILE, TILE, 160, 0.14, 3410, feather=1.0, stretch=(1.0, 0.6))
    img = img * (1 - 0.08 * damp[..., None])
    save("roof_render", img)


def roof_tank():
    img = fill(TILE, TILE, "ecebe8")
    img = coat(img, (1.05, 1.05, 1.05), 160, 0.25, seed=3501, feather=1.3)     # sun-faded
    img = coat(img, (0.93, 0.925, 0.915), 120, 0.15, seed=3502, feather=1.2)   # dust
    save("roof_tank", img)


def roof_galv():
    img = fill(TILE, TILE, "b7bbbb")
    img = coat(img, (1.05, 1.05, 1.05), 140, 0.28, seed=3601, feather=1.3)
    img = coat(img, (0.94, 0.94, 0.935), 110, 0.2, seed=3602, feather=1.2)
    yy = np.mgrid[0:TILE, 0:TILE][0].astype(float)
    for y in (TILE / 4, 3 * TILE / 4):     # a seam band every metre
        a = np.clip((4.0 - np.abs(yy - y)) / 1.5, 0, 1)
        img = img * (1 - 0.12 * a[..., None])
    img = coat(img, (1.06, 1.06, 1.055), 90, 0.08, seed=3603, feather=1.0)      # a white-rust field
    save("roof_galv", img)


# ------------------------------------------------------------------ billboards

def roof_ad_soda():
    h, w = 800, 1600                       # 8 m x 4 m at 200 px per metre
    img = fill(h, w, "2f5a3a")
    img = coat(img, (1.06, 1.05, 1.04), 220, 0.3, seed=3701, feather=1.2)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    # The giant bottle on the left: a flat cream-green body, a neck, a crimson cap, a label band.
    body = rect_mask(h, w, (120, 300, 330, 760), 70) + rect_mask(h, w, (185, 120, 265, 330), 26)
    img = paint(img, np.clip(body, 0, 1), "a9c98a", 3702, 0.04)
    img = paint(img, rect_mask(h, w, (178, 86, 272, 136), 12), "a0282a", 3703, 0.03)
    img = paint(img, rect_mask(h, w, (120, 470, 330, 600), 8), "efe6c8", 3704, 0.03)
    img = paint(img, text_mask(h, w, ["SL"], "impact", (150, 480, 300, 590), seed=3705, wobble=1.5), "2f5a3a", 3706, 0)
    # Three calamansi halves on the right, flat: rind, flesh, a few big segments.
    for k, (cx, cy, r) in enumerate(((1330, 560, 120), (1480, 420, 90), (1200, 690, 70))):
        img = paint(img, circle_mask(h, w, cx, cy, r), "7da33c", 3710 + k, 0.03)
        img = paint(img, circle_mask(h, w, cx, cy, r * 0.82), "e8d25a", 3720 + k, 0.03)
        ang = np.arctan2(yy - cy, xx - cx)
        seg = (np.abs(((ang * 8 / (2 * np.pi)) % 1) - 0.5) > 0.44) * circle_mask(h, w, cx, cy, r * 0.8)
        img = paint(img, seg.astype(float), "f3e7a8", 3730 + k, 0)
    img = paint(img, text_mask(h, w, ["SARAP-LAMIG!"], "impact", (400, 70, 1500, 310), seed=3740, wobble=2.2,
                               lean=-0.08), "efe6c8", 3741, 0.03)
    img = paint(img, text_mask(h, w, ["CALAMANSI SODA"], "black", (420, 330, 1130, 470), seed=3742, wobble=2.0),
                "e8d25a", 3743, 0.03)
    img = paint(img, text_mask(h, w, ["asim-tamis, tunay na kalamansi"], "print", (420, 500, 1100, 590), seed=3744,
                               wobble=1.8), "efe6c8", 3745, 0.02)
    img = paint(img, frame_mask(h, w, (0, 0, w, h), 24, 0, 3.0, 3746), "efe6c8", 3747, 0.03)
    img = weather(img, 3748, fade=0.08)
    img = drips(img, [260, 700, 1180, 1450], 24, 120, 14, "3c4a3c", seed=3749, strength=0.3)
    save("roof_ad_soda", img)


def roof_ad_kape():
    h, w = 800, 1600
    img = fill(h, w, "efe3c6")
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    # A printed maroon field on the right half behind the mug, and the mug itself.
    img = paint(img, rect_mask(h, w, (980, 0, w, h), 0), "7c2a30", 3801, 0.02)
    mug = rect_mask(h, w, (1080, 330, 1400, 700), 40)
    handle = np.clip(circle_mask(h, w, 1410, 500, 95) - circle_mask(h, w, 1410, 500, 55), 0, 1)
    img = paint(img, np.clip(mug + handle, 0, 1), "efe3c6", 3802, 0.02)
    img = paint(img, rect_mask(h, w, (1100, 330, 1380, 380), 20), "5a3424", 3803, 0.02)
    img = paint(img, rect_mask(h, w, (1150, 470, 1330, 560), 12), "d4a23a", 3804, 0.02)
    for k, x in enumerate((1170, 1240, 1310)):             # three big soft steam curls, flat cream
        curl = np.clip(1 - np.abs(xx - x - 22 * np.sin((yy - 120) / 45.0)) / 13, 0, 1) * (yy > 110) * (yy < 300)
        img = paint(img, curl, "f6eedb", 3805 + k, 0)
    img = paint(img, text_mask(h, w, ["KAPE NI", "LOLA NENA"], "black", (80, 70, 920, 420), seed=3810, wobble=0.8,
                               spacing=0.05), "7c2a30", 3811, 0.02)
    img = paint(img, text_mask(h, w, ["3-in-1"], "impact", (90, 460, 460, 640), seed=3812, wobble=0.6),
                "d4a23a", 3813, 0.02)
    img = paint(img, circle_mask(h, w, 700, 560, 150), "7c2a30", 3814, 0.02)
    img = paint(img, text_mask(h, w, ["P8", "lang!"], "black", (590, 440, 810, 680), seed=3815, wobble=0.6,
                               spacing=0.0), "efe3c6", 3816, 0.02)
    # The print has faded: big bleached fields, a sagging seam between two print strips.
    img = coat(img, (1.12, 1.1, 1.06), 260, 0.35, seed=3820, feather=1.3)
    a = np.clip((3.0 - np.abs(xx - 800 - 6 * _noise1d(h, 80, 3821)[:, None])) / 1.5, 0, 1)
    img = img * (1 - 0.25 * a[..., None])
    img = weather(img, 3822, fade=0.06)
    save("roof_ad_kape", img)


def roof_ad_hardware():
    h, w = 600, 1200                       # 6 m x 3 m
    img = fill(h, w, "e3bd48")
    img = coat(img, (1.05, 1.04, 1.0), 180, 0.3, seed=3901, feather=1.2)
    img = paint(img, frame_mask(h, w, (14, 14, w - 14, h - 14), 30, 20, 3.0, 3902), "8e2626", 3903, 0.04)
    img = paint(img, text_mask(h, w, ["MANG CALOY"], "impact", (120, 60, w - 120, 250), seed=3904, wobble=2.2,
                               lean=-0.05), "8e2626", 3905, 0.04)
    img = paint(img, text_mask(h, w, ["HARDWARE"], "black", (220, 250, w - 220, 380), seed=3906, wobble=2.0),
                "2b2626", 3907, 0.03)
    img = paint(img, text_mask(h, w, ["HOLLOW BLOCKS  ·  GI SHEETS  ·  PINTURA"], "black", (110, 410, w - 110, 470),
                               seed=3908, wobble=1.8), "8e2626", 3909, 0.03)
    img = paint(img, text_mask(h, w, ["Padre Faura, tapat ng simbahan"], "print", (260, 490, w - 260, 550),
                               seed=3910, wobble=1.8), "2b2626", 3911, 0.02)
    # Two painted emblems: a hammer on the left, a paint can on the right. Flat shapes.
    img = paint(img, np.clip(rect_mask(h, w, (70, 150, 96, 330), 8) + rect_mask(h, w, (40, 130, 128, 175), 10), 0, 1),
                "2b2626", 3912, 0)
    img = paint(img, rect_mask(h, w, (w - 125, 170, w - 45, 300), 10), "2f5e3a", 3913, 0)
    img = paint(img, rect_mask(h, w, (w - 128, 162, w - 42, 186), 6), "2b2626", 3914, 0)
    img = weather(img, 3915, fade=0.07)
    img = drips(img, [180, 520, 900], 30, 90, 10, "8a5a3c", seed=3916, strength=0.35)
    save("roof_ad_hardware", img)


def roof_board_back():
    h, w = 400, 800
    img = fill(h, w, "8f918e")
    img = coat(img, (1.06, 1.06, 1.05), 120, 0.3, seed=4001, feather=1.2)
    # The drawn stiffener frame: a perimeter band, three verticals and a mid rail, darker grey.
    m = frame_mask(h, w, (0, 0, w, h), 22, 0)
    for x in (w / 4, w / 2, 3 * w / 4):
        m = np.maximum(m, rect_mask(h, w, (x - 10, 0, x + 10, h), 0))
    m = np.maximum(m, rect_mask(h, w, (0, h / 2 - 9, w, h / 2 + 9), 0))
    img = img * (1 - 0.25 * m[..., None])
    img = blobs(img, "8a5a3c", 3, 18, seed=4002, strength=0.4, halo="a49c90")
    img = drips(img, [120, 430, 690], h / 2, 90, 7, "7c5038", seed=4003, strength=0.3)
    save("roof_board_back", img)


def roof_lattice():
    """One 0.6 m square bay of angle-iron lattice, RGBA cutout, tiling vertically: two fat chords at
    the edges, a fat diagonal, a strut at the bay line. Painted rust-grey with a lighter top edge."""
    n = 256
    yy, xx = np.mgrid[0:n, 0:n].astype(float)
    chord = 30                             # about 7 cm wide at 0.6 m per 256 px
    a = np.zeros((n, n))
    a = np.maximum(a, (xx < chord) | (xx > n - chord))
    a = np.maximum(a, (np.minimum(yy, n - yy) < 10))
    d = np.abs(xx - yy) / np.sqrt(2)
    a = np.maximum(a, np.clip((14 - d) / 1.5, 0, 1))
    rgb = fill(n, n, "6f6a64")
    rgb = coat(rgb, (1.1, 1.08, 1.05), 60, 0.3, seed=4101, feather=1.2)
    edge = ((xx > chord - 8) & (xx < chord)) | ((xx > n - chord) & (xx < n - chord + 8))
    rgb = rgb * (1 + 0.15 * edge[..., None])
    rgb = blobs(rgb, "8a5a3c", 3, 14, seed=4102, strength=0.45)
    save_rgba("roof_lattice", rgb, a)


# ------------------------------------------------------------------ small artwork

CLOTH = [  # (base colour, kind): cells left to right, top row then bottom row
    ("7c2a30", "shirt"), ("d4a23a", "shirt"), ("efece4", "sando"), ("efe6d0", "towel"),
    ("cf8f8c", "duster"), ("b8423a", "blanket"), ("a8966c", "shorts"), ("a893b0", "pillow"),
]


def roof_cloth():
    cell = 256
    h, w = cell * 2, cell * 4
    img = fill(h, w, "ffffff")
    for k, (col, kind) in enumerate(CLOTH):
        r, c = divmod(k, 4)
        x0, y0 = c * cell, r * cell
        tile = fill(cell, cell, col)
        tile = coat(tile, (1.07, 1.06, 1.05), 50, 0.3, seed=4200 + k, feather=1.2)   # sun-faded
        yy, xx = np.mgrid[0:cell, 0:cell].astype(float)
        if kind == "shirt":
            tile = paint(tile, rect_mask(cell, cell, (0, 110, cell, 140), 0), "efe6d0" if k else "d4a23a", 4210 + k, 0)
            tile = paint(tile, np.clip(circle_mask(cell, cell, cell / 2, -10, 50) - circle_mask(cell, cell, cell / 2, -10, 36), 0, 1),
                         "5a2024" if k == 0 else "a57e28", 4220 + k, 0)
        elif kind == "sando":
            tile = paint(tile, rect_mask(cell, cell, (0, cell - 18, cell, cell), 0), "d9d4c6", 4230, 0)
        elif kind == "towel":
            for s in (40, 70, 186, 216):
                tile = paint(tile, rect_mask(cell, cell, (0, s, cell, s + 16), 0, 2.0, 4240 + s), "4f7a44", 4241, 0.03)
        elif kind == "duster":
            rng = np.random.default_rng(4250)
            for f in range(5):
                cx, cy = rng.uniform(30, cell - 30), rng.uniform(30, cell - 30)
                for p in range(5):
                    a = p * 2 * np.pi / 5
                    tile = paint(tile, circle_mask(cell, cell, cx + 16 * np.cos(a), cy + 16 * np.sin(a), 12), "f1e3da", 4251, 0)
                tile = paint(tile, circle_mask(cell, cell, cx, cy, 8), "d4a23a", 4252, 0)
        elif kind == "blanket":
            band = ((xx // 64) % 2 == 0).astype(float) * 0.5 + ((yy // 64) % 2 == 0).astype(float) * 0.5
            tile = tile * (1 - 0.0 * band[..., None])
            cream = hexcol("efe3c6")
            m = np.clip(band - 0.25, 0, 1)
            tile = tile * (1 - 0.8 * m[..., None]) + cream * 0.8 * m[..., None]
        elif kind == "shorts":
            tile = paint(tile, rect_mask(cell, cell, (0, 0, cell, 26), 0), "8a7a58", 4260, 0)
        elif kind == "pillow":
            tile = paint(tile, frame_mask(cell, cell, (10, 10, cell - 10, cell - 10), 12, 6), "c2b0c8", 4270, 0)
        img[y0:y0 + cell, x0:x0 + cell] = tile
    save("roof_cloth", img)


def roof_door():
    h, w = 820, 360                        # 0.9 m x 2.05 m
    img = fill(h, w, "a98b69")
    img = coat(img, (1.06, 1.05, 1.04), 110, 0.3, seed=4301, feather=1.2)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    # Two battens across the back show through as the nail lines; a painted 3; a hasp.
    for y in (160, 640):
        a = np.clip((3.0 - np.abs(yy - y)) / 1.5, 0, 1) * ((xx % 60) < 8)
        img = img * (1 - 0.35 * a[..., None])
    img = paint(img, text_mask(h, w, ["3"], "impact", (110, 250, 250, 450), seed=4302, wobble=2.5), "8e2626", 4303, 0.03)
    img = paint(img, rect_mask(h, w, (w - 70, 400, w - 20, 440), 4), "4a4644", 4304, 0)
    edge = h - 90 + 14 * _noise1d(w, 40, 4305)[None, :]
    kick = np.clip((yy - edge) / 8 + 0.5, 0, 1) * 0.3
    img = img * (1 - kick[..., None]) + hexcol("7a6a58") * kick[..., None]
    save("roof_door", img)


def roof_window():
    h, w = 400, 480                        # 1.2 m x 1.0 m
    img = fill(h, w, "caa0a0")             # the curtain behind, faded rose
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    folds = 0.5 + 0.5 * np.cos(xx / w * 2 * np.pi * 5 + 0.6 * _noise1d(h, 60, 4401)[:, None])
    img = img * (0.9 + 0.1 * folds[..., None])
    img = paint(img, rect_mask(h, w, (0, 0, w * 0.18, h), 0), "4a4440", 4402, 0)      # the gap, dark room
    img = paint(img, frame_mask(h, w, (0, 0, w, h), 22, 0), "a9aba6", 4403, 0)        # aluminium frame
    img = paint(img, rect_mask(h, w, (w / 2 - 7, 0, w / 2 + 7, h), 0), "a9aba6", 4404, 0)
    # The grille, painted: a chunky dark green diamond pattern, low detail.
    d1 = np.abs(((xx + yy) % 120) - 60)
    d2 = np.abs(((xx - yy) % 120) - 60)
    g = np.clip((6 - np.minimum(d1, d2)) / 1.5, 0, 1) + frame_mask(h, w, (30, 30, w - 30, h - 30), 14, 0)
    img = paint(img, np.clip(g, 0, 1) * (rect_mask(h, w, (24, 24, w - 24, h - 24), 0)), "2f4a36", 4405, 0)
    img = weather(img, 4406, fade=0.04)
    save("roof_window", img)


TILING = [roof_plywood, roof_gi, roof_block, roof_render, roof_tank, roof_galv]
ARTWORK = [roof_ad_soda, roof_ad_kape, roof_ad_hardware, roof_board_back, roof_lattice, roof_cloth, roof_door,
           roof_window]


def swatch_sheet(version):
    SHEETS.mkdir(parents=True, exist_ok=True)
    path = SHEETS / f"roof_swatches_v{version}.png"
    if path.exists():
        print("[ilalim-roof-tex] sheet exists, not overwriting:", path)
        return
    tints = {"roof_gi": "a4453a", "roof_block": "b8cf9e", "roof_tank": "3a4a3a"}
    names = [p.__name__ for p in TILING + ARTWORK]
    cell, pad, cols = 360, 16, 4
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (cell + pad) + pad, rows * (cell + pad + 26) + pad), (245, 242, 235))
    draw = ImageDraw.Draw(sheet)
    for k, name in enumerate(names):
        tile = Image.open(OUT / f"{name}.png")
        if tile.mode == "RGBA":
            bg = Image.new("RGB", tile.size, (150, 190, 215))
            bg.paste(tile, mask=tile.split()[3])
            tile = bg
        tile = tile.convert("RGB")
        if name in tints:
            a = np.asarray(tile) / 255 * hexcol(tints[name]) * 1.05
            tile = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
        if k < len(TILING) or name == "roof_lattice":
            reps = 2
            rep = Image.new("RGB", (tile.width * reps, tile.height * reps))
            for i in range(reps):
                for j in range(reps):
                    rep.paste(tile, (i * tile.width, j * tile.height))
            tile = rep
        tile.thumbnail((cell, cell), Image.LANCZOS)
        r, c = divmod(k, cols)
        x, y = pad + c * (cell + pad), pad + r * (cell + pad + 26)
        sheet.paste(tile, (x, y))
        draw.text((x, y + tile.height + 4), name, fill=(40, 40, 40))
    sheet.save(path)
    print("[ilalim-roof-tex] sheet", path)


def main():
    for p in TILING + ARTWORK:
        p()
    version = int(sys.argv[sys.argv.index("--sheet") + 1]) if "--sheet" in sys.argv else 1
    swatch_sheet(version)


if __name__ == "__main__":
    main()
