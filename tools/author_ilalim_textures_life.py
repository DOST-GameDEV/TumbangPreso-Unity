"""Paint the Ilalim rebuild's SIDEWALK LIFE prop textures (the beggar's carton, bundle, bag and tin
cup, the magtataho's pole, rope rods, buckets and lids, the coin), in the house illustrated style.

  py -3 tools/author_ilalim_textures_life.py

Writes Assets/TumbangPreso/Art/IlalimRebuild/Life/life_*.png (IlalimSidewalkAuthor puts them on the
props' materials; SidewalkLife builds the carton's own mesh and UVs) and a swatch sheet
Logs/ilalim-unity/life_textures/life_swatches.png.

Owner, 2026-10-01: "the cardboard is untextured". Every life prop was a flat tint.

THE STYLE is Kanto's and the Lagoon's (KANTO_DESIGN_GUIDE.md section 3, LAGOON_REWORK_GUIDE.md
section 2), drawn with the Ilalim prop painter's own helpers (tools/author_ilalim_textures_props.py):
FLAT fills, a few LARGE patches with FEATHERED organic edges, low contrast, no grain, no noise, no
streaks, never leopard spots. Every surface has its own drawing.

THE UV LAYOUTS (they are the meshes', so they are fixed here):
  * life_carton (512 x 512): the carton's TOP FACE fills v 0.125..1 (image rows 0..447 from the
    top), u across its 0.62 m width, v along its 0.72 m length, the image's top edge its FAR end
    (where his feet point); the SIDE EDGES take the strip v 0..0.11 (the bottom 56 rows): the
    corrugated edge, flutes running across. Drawn in METRES so nothing is stretched on the board.
    The carton's middle is 0.12 m in front of his seat, so the worn patch sits at carton z -0.14.
    The mesh's far right corner is torn away (SidewalkLife.CartonMesh); the pale band follows it.
  * cylinders (tin cup, buckets, lids, pole, rod) are Unity's built-in Cylinder: the side wraps u
    once round, v along the height; each cap maps the whole square (planar), so a lid's rings are
    drawn round the image's centre.
  * spheres (bundle, knot, bag) are Unity's built-in Sphere: u round the equator, v pole to pole.

ROLE HUES (Art_Direction.md section 1): nothing near offence orange #f87020 or defence blue
#0080e8. The tin's label is maroon and cream, its rust a dark brown, never orange.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_ilalim_textures_props as P  # noqa: E402  (the Ilalim prop painter's helpers)

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "TumbangPreso" / "Art" / "IlalimRebuild" / "Life"
SHEET = ROOT / "Logs" / "ilalim-unity" / "life_textures"

hexcol, coat, fill, blobs, paint, text_mask = P.hexcol, P.coat, P.fill, P.blobs, P.paint, P.text_mask


def save(name, img):
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(OUT / f"{name}.png")
    print("[ilalim-life-tex]", name)
    return img


def feathered(mask, px):
    """A drawn edge: blurred by `px` then firmed, so a shape has a soft rim but a flat body."""
    m = ndimage.gaussian_filter(mask.astype(float), px)
    return np.clip((m - 0.5) * 2.2 + 0.5, 0, 1)


def mix(img, colour, alpha):
    return img * (1 - alpha[..., None]) + hexcol(colour) * alpha[..., None]


def shade(img, factor, alpha):
    return img * (1 + (np.asarray(factor, dtype=float) - 1) * alpha[..., None])


# ------------------------------------------------------------------ the beggar's carton

CARTON_W, CARTON_L = 0.62, 0.72          # metres (SidewalkLife.CartonMesh)
TOP_V0, SIDE_V1 = 0.125, 0.11            # the UV split (SidewalkLife.CartonMesh)


def life_carton():
    S = 512
    img = fill(S, S, "b8956a")
    top_rows = int(round(S * (1 - TOP_V0)))                  # 448 rows of top face
    # Metres over the top face: x across (-0.31..0.31), z along (+0.36 at the image top).
    yy, xx = np.mgrid[0:top_rows, 0:S].astype(float)
    X = (xx + 0.5) / S * CARTON_W - CARTON_W / 2
    Z = CARTON_L / 2 - (yy + 0.5) / top_rows * CARTON_L
    px_per_m = S / CARTON_W

    top = fill(top_rows, S, "b9966b")
    # Two coats: a sun-faded broad patch and a darker damp one, both big and soft.
    top = coat(top, (1.07, 1.06, 1.04), 150, 0.35, seed=4101, feather=1.6)
    top = coat(top, (0.94, 0.925, 0.91), 140, 0.22, seed=4102, feather=1.6)

    # The flattened box's fold creases: two across the length (the flaps), one along the width;
    # each a soft darker line with a paler lip beside it, wobbling a little.
    def crease(dist_m, width_m=0.006):
        d = np.abs(dist_m)
        line = np.exp(-(d / width_m) ** 2)
        lip = np.exp(-((dist_m - width_m * 2.2) / (width_m * 1.4)) ** 2)
        return line, lip
    wob = P.smooth(1, S, S / 5, 4103)[0] * 0.004
    for z0 in (-0.215, 0.205):
        line, lip = crease(Z - z0 - wob[None, :])
        top = shade(top, (0.8, 0.77, 0.74), line * 0.75)
        top = shade(top, (1.06, 1.05, 1.03), lip * 0.5)
    wob2 = P.smooth(top_rows, 1, top_rows / 5, 4104)[:, 0] * 0.004
    line, lip = crease(X - 0.02 - wob2[:, None])
    top = shade(top, (0.84, 0.81, 0.78), line * 0.55)
    top = shade(top, (1.05, 1.04, 1.02), lip * 0.4)

    # The faded print: an invented word and the "this way up" glyph (two arrows over a bar), in
    # a dull brown-black ink worn thin, on the middle flap, a little skewed.
    ink = np.zeros((top_rows, S))
    word = text_mask(top_rows, S, ["MARUPOK"], "impact", (70, 150, S - 70, 215), seed=4105, wobble=2.0)
    ink = np.maximum(ink, word)
    arrows = np.zeros((top_rows, S))
    for cx in (S * 0.40, S * 0.60):
        # an arrow: a shaft and a head, pointing to the far end
        shaft = (np.abs(xx - cx) < 7) & (yy > 262) & (yy < 318)
        head = (yy > 232) & (yy < 268) & (np.abs(xx - cx) < (yy - 232) * 0.95)
        arrows = np.maximum(arrows, (shaft | head).astype(float))
    bar = ((yy > 326) & (yy < 338) & (xx > S * 0.33) & (xx < S * 0.67)).astype(float)
    arrows = np.maximum(arrows, bar)
    rot = ndimage.rotate(np.maximum(ink, feathered(arrows, 0.8)), -4, reshape=False, order=1)
    wear = np.clip(0.55 + 0.35 * P.smooth(top_rows, S, 40, 4106), 0, 1)       # worn thin in patches
    top = mix(top, "4a3a2c", rot * 0.42 * wear)

    # Water stains: big rings, a darker tide line and a slightly darker middle.
    rng = np.random.default_rng(4107)
    for cx_m, cz_m, r_m in ((0.17, 0.08, 0.11), (-0.2, 0.27, 0.075), (-0.05, -0.3, 0.09)):
        ang = np.arctan2(Z - cz_m, X - cx_m)
        ph = rng.uniform(0, 6.28)
        rr = r_m * (1 + 0.12 * np.sin(3 * ang + ph) + 0.06 * np.sin(5 * ang + 2 * ph))
        d = np.hypot(X - cx_m, Z - cz_m)
        inside = np.clip((rr - d) * px_per_m / 6 + 0.5, 0, 1)
        tide = np.exp(-(((d - rr) * px_per_m) / 6.0) ** 2)
        top = shade(top, (0.95, 0.94, 0.93), inside * 0.8)
        top = shade(top, (0.88, 0.86, 0.85), tide * 0.6)

    # Pavement dirt: a soft grey-brown band along the near and side edges, and a sole-shaped
    # smudge where he stepped on it laying it down.
    edge = np.minimum.reduce([CARTON_W / 2 - np.abs(X), Z + CARTON_L / 2 + 0 * X])
    edge = edge + 0.012 * P.smooth(top_rows, S, 30, 4108)
    dirt = np.clip((0.05 - edge) / 0.03, 0, 1)
    top = mix(top, "7d7163", dirt * 0.35)
    sole = ((X - 0.12) / 0.045) ** 2 + ((Z - 0.24) / 0.1) ** 2
    top = mix(top, "83786b", feathered((sole < 1).astype(float), 2.5) * 0.28)

    # Where he sits: a darker, greyer, flattened patch round his seat (carton-local z -0.12).
    seat = ((X / 0.2) ** 2 + ((Z + 0.14) / 0.17) ** 2)
    seat = seat + 0.07 * P.smooth(top_rows, S, 60, 4109)
    worn = np.clip((1.0 - seat) / 0.6, 0, 1)
    worn = worn * worn * (3 - 2 * worn)
    top = shade(top, (0.8, 0.78, 0.77), worn * 0.9)

    # The torn corner (the mesh's far right corner is ripped away): the exposed inner ply, a
    # paler rough band along the tear.
    tear = np.hypot(X - CARTON_W / 2, Z - CARTON_L / 2)
    tear = tear + 0.008 * P.smooth(top_rows, S, 40, 4110)
    band = np.exp(-(((tear - 0.185) * px_per_m) / 5) ** 2) * (tear < 0.22)
    top = mix(top, "cfb68e", band * 0.5)

    img[:top_rows] = top

    # The side strip: the corrugated edge (flutes across), kraft with its liner lines.
    side_rows = int(round(S * SIDE_V1))
    sy, sx = np.mgrid[0:side_rows, 0:S].astype(float)
    side = fill(side_rows, S, "a9855b")
    flutes = 0.5 + 0.5 * np.cos(sx / 9.0 * 2 * np.pi)
    arch = np.clip(1 - np.abs((sy / side_rows - 0.5) * 2) ** 1.5, 0, 1)
    side = shade(side, (0.82, 0.8, 0.78), flutes * arch * 0.8)
    liner = np.exp(-((sy - 3) / 2.2) ** 2) + np.exp(-((sy - side_rows + 4) / 2.2) ** 2)
    side = mix(side, "c6a57a", np.clip(liner, 0, 1) * 0.8)
    img[S - side_rows:] = side
    # The gap between the two (never sampled but mip-mapped): the side colour.
    img[top_rows:S - side_rows] = hexcol("a9855b")
    return save("life_carton", img)


# ------------------------------------------------------------------ his bundle and bag

def life_cloth():
    """The tied bundle: an old faded malong-style cloth, plum-brown with broad soft checks."""
    S = 256
    img = fill(S, S, "7d5c50")
    yy, xx = np.mgrid[0:S, 0:S].astype(float)
    # Broad checks, low contrast, drawn with soft edges and a wobble (hand-woven, sun-faded).
    wx = P.smooth(1, S, S / 4, 4201)[0] * 5
    wy = P.smooth(S, 1, S / 4, 4202)[:, 0] * 5
    bx = 0.5 + 0.5 * np.cos((xx + wy[:, None]) / S * 2 * np.pi * 4)
    by = 0.5 + 0.5 * np.cos((yy + wx[None, :]) / S * 2 * np.pi * 3)
    img = shade(img, (1.12, 1.09, 1.06), np.clip((bx - 0.55) * 3, 0, 1) * 0.6)
    img = shade(img, (0.9, 0.9, 0.92), np.clip((by - 0.6) * 3, 0, 1) * 0.55)
    thin = np.exp(-(((xx + wy[:, None]) % (S / 4) - S / 8) / 2.0) ** 2)
    img = mix(img, "b59a6a", thin * 0.35)                                  # a faded ochre thread line
    img = coat(img, (1.1, 1.08, 1.06), 45, 0.3, seed=4203, feather=1.3)   # sun-faded crown
    # Grime low down (the sphere's lower half) where it sits on the pavement.
    low = np.clip((yy / S - 0.62) / 0.2, 0, 1) * (0.7 + 0.3 * P.smooth(S, S, 30, 4204))
    img = mix(img, "4f4440", np.clip(low, 0, 1) * 0.35)
    return save("life_cloth", img)


def life_bag():
    """A white plastic sando bag: soft grey creases, a faded red printed band, an invented word."""
    S = 256
    img = fill(S, S, "e2e0d8")
    img = coat(img, (0.94, 0.94, 0.95), 55, 0.24, seed=4301, feather=1.5, stretch=(1.0, 0.3))  # long soft creases
    img = coat(img, (1.03, 1.03, 1.03), 70, 0.25, seed=4302, feather=1.5)
    yy, xx = np.mgrid[0:S, 0:S].astype(float)
    band = ((yy > S * 0.42) & (yy < S * 0.58)).astype(float)
    img = mix(img, "a33a3a", feathered(band, 1.0) * 0.35)
    word = text_mask(S, S, ["SUKI"], "black", (S * 0.12, S * 0.44, S * 0.46, S * 0.56), seed=4303, wobble=1.5)
    img = mix(img, "eae6dc", word * 0.8)
    low = np.clip((yy / S - 0.7) / 0.2, 0, 1)
    img = mix(img, "8f8a80", low * 0.3)
    return save("life_bag", img)


# ------------------------------------------------------------------ the tin cup (a milk can)

def life_tin():
    """An old condensed-milk can for a cup, laid out for SidewalkLife.CupMesh: the SIDE (the
    image's top 0.615, v 0.385..1) a maroon-and-cream paper label worn through to the tin, rust at
    the rims; the TOP (the disc in the lower left square) the open can: a bright rolled rim, the
    dark inside, two coins at the bottom of it; the BOTTOM (the lower right disc) plain dull tin.
    The built-in Cylinder put the whole image on each cap, so the label showed on top."""
    side = life_tin_side()
    S = 256
    img = fill(S, S, "aaa598")
    top0, top1 = int(round(S * (1 - 0.995))), int(round(S * (1 - 0.385)))
    img[top0:top1] = np.asarray(Image.fromarray((np.clip(side, 0, 1) * 255).astype(np.uint8)).resize((S, top1 - top0), Image.LANCZOS), dtype=float) / 255
    img[:top0] = img[top0]
    yy, xx = np.mgrid[0:S, 0:S].astype(float)
    r_px = 0.17 * S
    # The top: the open can seen from above.
    cx, cy = 0.18 * S, (1 - 0.18) * S
    r = np.hypot(xx - cx, yy - cy) / r_px
    disc = r < 1.08
    inside = fill(S, S, "2e2924")
    inside = shade(inside, (1.5, 1.45, 1.4), np.clip(1 - r / 0.8, 0, 1) ** 1.5 * 0.5)   # the far floor catches a little light
    for (ox, oy, rr, col) in ((-0.22, 0.12, 0.2, "c9a440"), (0.18, -0.08, 0.17, "b8963c")):
        coin = np.hypot((xx - cx) / r_px - ox, ((yy - cy) / r_px - oy) * 1.25) < rr
        inside = mix(inside, col, feathered(coin.astype(float), 0.8))
        rim_c = np.abs(np.hypot((xx - cx) / r_px - ox, ((yy - cy) / r_px - oy) * 1.25) - rr * 0.82) < 0.025
        inside = mix(inside, "8a6d2a", rim_c.astype(float) * 0.6)
    rim = (r > 0.84) & (r < 1.08)
    top = np.where(disc[..., None], inside, img)
    top = mix(top, "cfcac0", feathered(rim.astype(float), 0.8))
    top = mix(top, "7a7469", np.exp(-((r - 0.84) / 0.025) ** 2) * 0.7)          # the lip's inner shadow
    top = mix(top, "7a5a44", (np.clip(1 - np.abs(r - 0.97) / 0.07, 0, 1) * (0.5 + 0.5 * P.smooth(S, S, 20, 4408))) * 0.35)  # rust on the rim
    img = np.where((r < 1.12)[..., None], top, img)
    # The bottom: plain dull tin with a pressed ring.
    bx = 0.68 * S
    rb = np.hypot(xx - bx, yy - cy) / r_px
    img = np.where((rb < 1.12)[..., None], shade(fill(S, S, "aaa598"), (0.88, 0.88, 0.88), np.exp(-((rb - 0.7) / 0.04) ** 2)), img)
    return save("life_tin", img)


def life_tin_side():
    """The can's side, drawn square; life_tin fits it into the side's UV band."""
    S = 256
    img = fill(S, S, "b5b0a4")
    img = coat(img, (1.08, 1.08, 1.08), 40, 0.25, seed=4401, feather=1.3, stretch=(0.4, 1.0))
    yy, xx = np.mgrid[0:S, 0:S].astype(float)
    label = ((yy > S * 0.24) & (yy < S * 0.76)).astype(float)
    torn = P.patches(S, S, 70, 0.14, 4402, feather=1.0)                  # the label worn through in two or three places
    label = feathered(label, 1.0) * (1 - torn)
    lab = fill(S, S, "e8dcc0")
    lab = coat(lab, (0.93, 0.9, 0.86), 35, 0.25, seed=4403, feather=1.2)
    stripe = ((yy > S * 0.30) & (yy < S * 0.40)) | ((yy > S * 0.62) & (yy < S * 0.70))
    lab = mix(lab, "7a2e36", feathered(stripe.astype(float), 0.8) * 0.8)
    word = text_mask(S, S, ["GATAS"], "impact", (S * 0.08, S * 0.42, S * 0.52, S * 0.6), seed=4404, wobble=1.2)
    lab = mix(lab, "7a2e36", word * 0.85)
    img = img * (1 - label[..., None]) + lab * label[..., None]
    # Rust at the rims: a soft dark-brown band along each rim with a ragged inner edge, not spots.
    for y0, seed in ((S * 0.05, 4405), (S * 0.95, 4406)):
        ragged = 10 + 7 * P.smooth(1, S, S / 4, seed)[0]
        rust = np.clip((ragged[None, :] - np.abs(yy - y0)) / 4 + 0.5, 0, 1)
        img = mix(img, "7a5a44", rust * 0.45)
    img = coat(img, (0.92, 0.91, 0.9), 90, 0.2, seed=4407, feather=1.5)  # handled grime
    return img


# ------------------------------------------------------------------ the magtataho's kit

def life_aluminium():
    """The taho buckets: dull aluminium, broad soft vertical sheen bands (round the side), two
    dents, and a dull tide band near the bottom where it stands in the wet."""
    S = 256
    img = fill(S, S, "c3c8cc")
    yy, xx = np.mgrid[0:S, 0:S].astype(float)
    sheen = 0.5 + 0.5 * np.cos(xx / S * 2 * np.pi * 2 + 0.7)
    img = shade(img, (1.08, 1.08, 1.08), np.clip((sheen - 0.6) * 2.5, 0, 1) * 0.8)
    img = shade(img, (0.9, 0.91, 0.92), np.clip((0.3 - sheen) * 2.5, 0, 1) * 0.7)
    for cx, cy, r in ((S * 0.3, S * 0.45, 20), (S * 0.78, S * 0.62, 14)):
        dent = np.hypot((xx - cx) / 1.3, yy - cy)
        img = shade(img, (0.93, 0.93, 0.94), feathered((dent < r).astype(float), 5) * 0.8)
    tide = np.clip((yy / S - 0.8) / 0.08, 0, 1) * (0.8 + 0.2 * P.smooth(S, S, 30, 4501))
    img = shade(img, (0.86, 0.85, 0.82), np.clip(tide, 0, 1) * 0.7)
    rim = np.exp(-((yy - 4) / 3.0) ** 2)
    img = shade(img, (0.82, 0.83, 0.85), rim)
    return save("life_aluminium", img)


def life_lid():
    """The bucket lids: on the cap (the square's middle) soft concentric pressed rings and a worn
    ring where hands lift it; the thin side is the same plain aluminium."""
    S = 256
    img = fill(S, S, "9aa1a8")
    yy, xx = np.mgrid[0:S, 0:S].astype(float)
    r = np.hypot(xx - S / 2, yy - S / 2) / (S / 2)
    rings = np.exp(-((r - 0.62) / 0.03) ** 2) + np.exp(-((r - 0.86) / 0.025) ** 2)
    img = shade(img, (0.86, 0.87, 0.88), np.clip(rings, 0, 1) * 0.8)
    img = shade(img, (1.09, 1.09, 1.09), np.exp(-((r - 0.66) / 0.025) ** 2) * 0.6)
    knob = r < 0.14
    img = shade(img, (0.78, 0.79, 0.8), feathered(knob.astype(float), 1.5) * 0.9)
    img = coat(img, (1.05, 1.05, 1.05), 45, 0.25, seed=4601, feather=1.3)
    return save("life_lid", img)


def life_bamboo():
    """The pingga: sun-yellowed bamboo, round the side u, along its length v; a node every 0.3 m
    (five on the 1.52 m pole), each a darker ring with a paler ridge; broad soft colour drift."""
    S = 256
    img = fill(S, S, "c9ae6e")
    img = coat(img, (1.06, 1.05, 1.0), 40, 0.3, seed=4701, feather=1.3, stretch=(1.0, 0.35))
    img = coat(img, (0.93, 0.92, 0.88), 50, 0.2, seed=4702, feather=1.2, stretch=(1.0, 0.35))
    yy, xx = np.mgrid[0:S, 0:S].astype(float)
    wob = P.smooth(1, S, S / 3, 4703)[0] * 1.5
    for k in range(1, 5):
        y0 = S * k / 5
        img = shade(img, (0.72, 0.68, 0.6), np.exp(-(((yy - y0 - wob[None, :]) / 1.8) ** 2)) * 0.85)
        img = shade(img, (1.08, 1.07, 1.03), np.exp(-(((yy - y0 - 4.5 - wob[None, :]) / 2.4) ** 2)) * 0.6)
    # The shoulder's wear: a darker, polished patch in the middle of the length.
    worn = np.clip(1 - np.abs(yy / S - 0.5) / 0.12, 0, 1)
    img = shade(img, (0.9, 0.86, 0.8), worn * worn * 0.8)
    return save("life_bamboo", img)


def life_rope():
    """The rods and ropes the buckets hang from: brown twisted fibre in broad soft diagonal bands."""
    S = 128
    img = fill(S, S, "8a7a5a")
    yy, xx = np.mgrid[0:S, 0:S].astype(float)
    twist = 0.5 + 0.5 * np.cos((xx + yy * 1.2) / S * 2 * np.pi * 5)
    img = shade(img, (0.84, 0.82, 0.8), np.clip((twist - 0.5) * 2, 0, 1) * 0.7)
    img = shade(img, (1.08, 1.06, 1.03), np.clip((0.25 - twist) * 3, 0, 1) * 0.5)
    img = coat(img, (0.92, 0.9, 0.88), 25, 0.25, seed=4801, feather=1.2)
    return save("life_rope", img)


def life_coin():
    """A one-peso coin as the cup sees it: brass, a rim ring and a plain raised disc."""
    S = 64
    img = fill(S, S, "e3c04a")
    yy, xx = np.mgrid[0:S, 0:S].astype(float)
    r = np.hypot(xx - S / 2, yy - S / 2) / (S / 2)
    img = shade(img, (0.82, 0.78, 0.7), np.exp(-((r - 0.84) / 0.06) ** 2) * 0.9)
    img = shade(img, (1.1, 1.08, 1.02), np.exp(-((r - 0.45) / 0.08) ** 2) * 0.6)
    return save("life_coin", img)


def sheet(images):
    SHEET.mkdir(parents=True, exist_ok=True)
    tiles = []
    for name, img in images:
        t = Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).resize((256, 256), Image.NEAREST)
        tiles.append(t)
    cols = 5
    rows = (len(tiles) + cols - 1) // cols
    out = Image.new("RGB", (cols * 264 + 8, rows * 264 + 8), (40, 40, 44))
    for i, t in enumerate(tiles):
        out.paste(t, (8 + (i % cols) * 264, 8 + (i // cols) * 264))
    out.save(SHEET / "life_swatches.png")
    print("[ilalim-life-tex] sheet", SHEET / "life_swatches.png")


def main():
    images = [
        ("carton", life_carton()), ("cloth", life_cloth()), ("bag", life_bag()), ("tin", life_tin()),
        ("aluminium", life_aluminium()), ("lid", life_lid()), ("bamboo", life_bamboo()),
        ("rope", life_rope()), ("coin", life_coin()),
    ]
    sheet(images)


if __name__ == "__main__":
    main()
