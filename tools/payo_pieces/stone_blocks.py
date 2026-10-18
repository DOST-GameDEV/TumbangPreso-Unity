"""PAYO's stage stone family, the rest of it (2026-10-12): every block the layout names, the stairs, movers and pads.

The owner approved the look of the three heroes (monolith, core, tower_head: LOCKED, never rebuilt from here). These
are made with the same painters, lighter (29 blocks are on screen at once), one piece a block id so that neighbours
differ: six shaft styles, four face designs, their own relief motifs and paint.

  py -3 tools/payo_pieces/stone_build.py <piece>      any name in PLAN below, or stair_E, stair_W, mover_lift,
                                                      mover_ferry, mover_piston, pad_geyser, pad_speed, pickup_plinth, ext_<w>x<d>
  py -3 tools/payo_pieces/stone_blocks.py table       writes Logs/payo/pieces/build/stone/blocks.json

A block's top is exactly its footprint from Assets/TumbangPreso/Art/Payo/payo_layout.json (read only), flat, at y 0; the
origin is the top's centre; +x is the layout's +x and +z the layout's +z. 13.6 m is modelled; `ext_<w>x<d>` is a plain
40 m shaft to stand under a block (origin the same, it starts 12.6 m down) so a raised block never shows a foot.
"""
import json
import math
import random
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import stone_kit as K                                # noqa: E402
import stone_family as FAM                           # noqa: E402
from stone_kit import (Piece, ball, box, disc, lid, pipe, relief, soft, stones, turned, weather,  # noqa: E402
                       BARK, CORAL, CREAM, DARK, INK, LEAF, MINT, MOSS, OCHRE, PALE, RED, ROSE, TURQ, WATER)

ROOT = Path(__file__).resolve().parents[2]
LAYOUT = ROOT / "Assets/TumbangPreso/Art/Payo/payo_layout.json"
PAINTS = [OCHRE, CORAL, RED, ROSE, MINT, CREAM]
KINDS = ["lizard", "sun", "rice", "ancestor", "spiral"]
SG, RG = 12, 6                                      # the lighter rounding these blocks use
PPM_SCALE = 1.0                                     # lowered and tried again when a block's atlas will not fit

# block id: (glb, shaft style, face design or None). The corners, the core and one tower are the locked heroes.
PLAN = {
    "core": ("core", None, None),
    "cornerNE": ("monolith", None, None), "cornerSE": ("monolith", None, None), "cornerSW": ("monolith", None, None), "cornerNW": ("monolith", None, None),
    "slabN": ("slab_N", "tiers", 1), "slabS": ("slab_S", "fluted", 2), "slabE": ("slab_E", "stepped", 0), "slabW": ("slab_W", "arch", 3),
    "bridgeN": ("bridge_N", "arch", None), "bridgeS": ("bridge_S", "stack", None), "bridgeE": ("bridge_E", "drum", None), "bridgeW": ("bridge_W", "stepped", None),
    "midN": ("mid_N", "arch", 2), "midS": ("mid_S", "tiers", 0), "midE": ("mid_E", "fluted", 3), "midW": ("mid_W", "stack", 1),
    "stone1": ("stone_1", "drum", 3), "stone2": ("stone_2", "stack", 1), "stone3": ("stone_3", "stepped", 2), "stone4": ("stone_4", "tiers", 0),
    "stone5": ("stone_5", "drum", 1), "stone6": ("stone_6", "fluted", 3), "stone7": ("stone_7", "stack", 2), "stone8": ("stone_8", "stepped", 0),
    "towerNE": ("tower_head", None, None), "towerSE": ("tower_drum", "tower_drum", None), "towerSW": ("tower_owl", "tower_owl", None), "towerNW": ("tower_shrine", "tower_shrine", None),
}


def footprints():
    data = json.loads(LAYOUT.read_text(encoding="utf-8"))
    out = {}
    for lay in data["layouts"]:
        for p in lay["pieces"]:
            out[p["id"]] = (p["x"], p["z"], p["sx"], p["sz"])
    return out, data


def seed_of(s):
    return sum((i + 1) * ord(c) for i, c in enumerate(s))


# ------------------------------------------------------------------ CALM (Logs/payo/pieces/CALM_STYLE.md)
# One accent pair per block (S.CALM_PAIRS), one big carved panel per side of the block, each side its own motif; the
# trim swatch (ledges, corbels, the carved faces' pale parts) tinted a little toward the block's accent, so the same
# four carved face designs read as different stones across the stage. Geometry and UVs are untouched.
CTX = {}
SIDES = ["front", "right", "back", "left"]
YAW_SIDE = {0: "front", 90: "right", 180: "back", 270: "left"}
PAIR_OF = {"slab_N": 0, "slab_E": 5, "slab_S": 2, "slab_W": 1, "bridge_N": 6, "bridge_E": 7, "bridge_S": 7, "bridge_W": 6,
           "mid_N": 4, "mid_E": 1, "mid_S": 3, "mid_W": 5, "stone_1": 2, "stone_2": 3, "stone_3": 0, "stone_4": 4, "stone_5": 4,
           "stone_6": 0, "stone_7": 3, "stone_8": 1, "tower_drum": 1, "tower_owl": 2, "tower_shrine": 0,
           "stair_E": 5, "stair_W": 1, "mover_lift": 3, "mover_ferry": 1, "mover_piston": 0}   # glb -> S.CALM_PAIRS, neighbours differ


def calm_setup(S, glb, seed, face_sides=()):
    pair = PAIR_OF.get(glb, seed % len(S.CALM_PAIRS))
    field, ink = S.CALM_PAIRS[pair]
    ks = S.CALM_KINDS
    k0, step = seed % len(ks), (2, 4, 5, 7)[seed % 4]
    kinds = {sd: ks[(k0 + i * step) % len(ks)] for i, sd in enumerate(SIDES)}
    CTX.clear()
    CTX.update(field=field, ink=ink, kinds=kinds, face=set(face_sides), pair=pair, band=field if pair < 6 else ink, mark=field if pair in (1, 5) else ink)
    K.TINT.clear()
    tint = field if pair < 6 else ink
    K.TINT[PALE] = K.mix3(PALE, tint, 0.22)


def calm_wall(S, seed, damp=0.4, tone=1.0, sides=None):
    """A calm shaft wall: the soft stone, then one panel on each side named ({side: v})."""
    w = S.stone_wall(seed=seed, damp=damp, moss=0.5 + damp * 0.4, drips=0.4 + damp * 0.5, tone=tone, course=0.72, length=(1.0, 1.8))
    if not sides:
        return w
    return S.panels(w, {sd: (CTX["kinds"][sd], CTX["field"], CTX["ink"], v) for sd, v in sides.items()})


def calm_band(S, img, info, rows, metres, radius_k, thetas, kinds=None):
    """Panels on a lathe's band (the image rows `rows` of a `turned` paint, which spaces rings evenly, not by length):
    the band is lifted out at true scale, panels drawn at the angles `thetas` (degrees from +x toward +z), put back."""
    w, h = img.size
    r0, r1 = int(rows[0]), int(rows[1])
    if r1 - r0 < 2:
        return
    circ = 2 * math.pi * radius_k * w / (2 * math.pi * info["ppm"])        # metres round the band
    th = max(8, int(metres / circ * w))
    band = img.crop((0, r0, w, r1)).resize((w, th), Image.BICUBIC)
    H = K.HEIGHT.get(id(img))
    hb = H.crop((0, r0, w, r1)).resize((w, th), Image.BICUBIC) if H is not None else Image.new("L", (w, th), 128)
    K.HEIGHT[id(band)] = hb
    ppm_t = w / circ
    ph = min(th * 0.8, 3.0 * ppm_t)
    pw = ph * 0.86
    for i, t in enumerate(thetas):
        kind = (kinds or [CTX["kinds"][sd] for sd in SIDES])[i % 4]
        S.calm_panel(band, info, (t % 360) / 360.0 * w, th / 2, pw, ph, kind, CTX["field"], CTX["ink"])
    K.HEIGHT.pop(id(band), None)
    img.paste(band.resize((w, r1 - r0), Image.LANCZOS), (0, r0))
    if H is not None:
        H.paste(hb.resize((w, r1 - r0), Image.LANCZOS), (0, r0))


def calm_thatch(img, info):
    """The shrine's eaves, calm: one golden straw, broad strand bundles as soft vertical ridges, a soft darker foot."""
    w, h = img.size
    ppm = info["ppm"]
    base = K._soft((206, 166, 86), 0.85, 0.03)
    x = np.arange(w, dtype=np.float32)[None, :]
    y = np.arange(h, dtype=np.float32)[:, None]
    bund = 0.5 + 0.5 * np.sin(x / (0.55 * ppm) * math.tau + 0.6 * np.sin(x / (1.7 * ppm)))
    t = y / max(1, h - 1)
    f = (0.97 + 0.05 * bund) * (1.06 - 0.12 * t) * (1 - 0.16 * np.clip((t - 0.78) / 0.22, 0, 1))
    a = np.clip(np.array(base, np.float32)[None, None, :] * f[..., None], 0, 255).astype(np.uint8)
    img.paste(Image.fromarray(a))
    H = K.HEIGHT.get(id(img))
    if H is not None:
        hv = 128 + (0.012 * bund - 0.02 * np.clip((t - 0.8) / 0.2, 0, 1)) / K.HU + 0 * t
        H.paste(Image.fromarray(np.clip(hv, 0, 255).astype(np.uint8)))
    return img


# ------------------------------------------------------------------ faces, four designs, light
def face_lite(P, x, y, z, s, yaw, design, fins=CORAL, crown=(OCHRE, TURQ)):
    """A carved face standing out of a wall at z, looking along +z turned by `yaw`. 0 ring eyes, 1 the sleeper (slit
    eyes, a tongue), 2 the owl (great inlaid discs, a beak), 3 a sun disc with rays."""
    def at(dx, dy, dz):
        a = math.radians(yaw)
        return (x + dx * math.cos(a) + dz * math.sin(a), y + dy, z - dx * math.sin(a) + dz * math.cos(a))
    t = (0, yaw, 0)
    kw = dict(seg=10, rings=6)

    def b(half, r, pos, sw, **more):
        box(P, tuple(v * s for v in half), r * s, at(pos[0] * s, pos[1] * s, pos[2] * s), sw=sw, turn=t, **kw, **more)

    def o(half, pos, sw):
        ball(P, tuple(v * s for v in half), at(pos[0] * s, pos[1] * s, pos[2] * s), sw, turn=t, seg=10, rings=6)

    if design == 3:
        def sun(img, info):
            w, h = img.size
            d = ImageDraw.Draw(img)
            d.rectangle([0, 0, w, h], fill=(150, 148, 144))
            rings = ((0.98, PALE), (0.84, (128, 126, 124)), (0.5, PALE), (0.42, (160, 158, 152)))
            if K.CALM:
                rings = ((0.98, K.mix3(PALE, crown[0], 0.15)), (0.84, K.calm_tone((128, 126, 124))), (0.5, PALE), (0.42, K.calm_tone((160, 158, 152))))
            for (rr, col) in rings:
                d.ellipse([w / 2 - rr * w / 2, h / 2 - rr * h / 2, w / 2 + rr * w / 2, h / 2 + rr * h / 2], fill=col)
            r2 = random.Random(int(s * 100))
            m = Image.new("L", (w, h), 0)
            dm = ImageDraw.Draw(m)
            for k in range(12):
                a = math.tau * k / 12
                dm.polygon([(w / 2 + 0.27 * w * math.cos(a - 0.14), h / 2 + 0.27 * h * math.sin(a - 0.14)), (w / 2 + 0.41 * w * math.cos(a), h / 2 + 0.41 * h * math.sin(a)),
                            (w / 2 + 0.27 * w * math.cos(a + 0.14), h / 2 + 0.27 * h * math.sin(a + 0.14))], fill=255)
            relief(img, m, paint=crown[0], rnd=r2, depth=2)
            fc = INK if not K.CALM else K.shade(K.calm_tone((128, 126, 124)), 0.55)
            for sx in (-1, 1):                               # a small calm face in the middle
                d.ellipse([w / 2 + sx * 0.09 * w - 0.035 * w, h * 0.45, w / 2 + sx * 0.09 * w + 0.035 * w, h * 0.5], fill=fc)
            d.arc([w * 0.42, h * 0.46, w * 0.58, h * 0.6], 20, 160, fill=fc, width=max(2, int(w * (0.02 if not K.CALM else 0.03))))
            d.ellipse([w / 2 - 0.445 * w, h / 2 - 0.445 * h, w / 2 + 0.445 * w, h / 2 + 0.445 * h], outline=TURQ, width=max(2, int(w * (0.02 if not K.CALM else 0.03))))
            if K.CALM:
                rim = Image.new("L", (w, h), 0)
                ImageDraw.Draw(rim).ellipse([w * 0.08, h * 0.08, w * 0.92, h * 0.92], outline=255, width=max(2, int(w * 0.06)))
                K.hset(img, rim.filter(ImageFilter.GaussianBlur(2)), 0.015)
            return img
        disc(P, 1.25 * s, 0.34 * s, at(0, 0, 0.1 * s), sun, rim=0.2 * s, seg=18, ppm=min(110, P.ppm * 1.6), turn=(90, yaw, 0))
        return
    for k, dx in enumerate((-0.6, 0.0, 0.6)):                # a crown of three leaves
        hgt = (0.38, 0.5, 0.38)[k]
        b((0.24, hgt, 0.16), 0.07, (dx, 0.9 + hgt, 0.08), crown[k % 2], taper=(1.0, 0.35))
    for sx in (-1, 1):
        b((0.2, 0.86, 0.22), 0.1, (sx * 1.72, -0.2, 0.06), fins, taper=(0.55, 1.0))
    if design == 0:
        b((1.5, 0.27, 0.36), 0.14, (0, 0.62, 0.14), PALE)
        b((0.3, 0.62, 0.34), 0.14, (0, -0.1, 0.14), PALE, taper=(1.3, 0.7))
        for sx in (-1, 1):
            o((0.46, 0.46, 0.2), (sx * 0.86, 0.02, 0.06), PALE)
            o((0.3, 0.3, 0.1), (sx * 0.86, 0.02, 0.2), DARK)
            o((0.13, 0.13, 0.07), (sx * 0.86, 0.02, 0.29), TURQ)
        b((0.98, 0.24, 0.14), 0.07, (0, -1.02, 0.03), DARK)
        for dx in (-0.5, 0.0, 0.5):
            b((0.2, 0.17, 0.14), 0.06, (dx, -0.96, 0.1), PALE)
    elif design == 1:
        b((1.5, 0.3, 0.4), 0.14, (0, 0.6, 0.16), PALE, taper=(1.0, 0.9))
        b((0.3, 0.7, 0.36), 0.14, (0, -0.2, 0.16), PALE, taper=(1.5, 0.7))
        for sx in (-1, 1):
            o((0.5, 0.34, 0.2), (sx * 0.86, 0.0, 0.06), PALE)                        # a heavy closed lid
            box(P, (0.44 * s, 0.07 * s, 0.1 * s), 0.04 * s, at(sx * 0.86 * s, -0.12 * s, 0.24 * s), sw=DARK, turn=(0, yaw, sx * 8), **kw)
            o((0.22, 0.22, 0.16), (sx * 1.36, -0.7, 0.1), TURQ)
        b((0.9, 0.2, 0.14), 0.08, (0, -1.02, 0.05), DARK)
        b((0.36, 0.4, 0.14), 0.12, (0, -1.3, 0.14), CORAL, taper=(0.7, 1.0))
    else:
        for sx in (-1, 1):
            box(P, (0.8 * s, 0.2 * s, 0.34 * s), 0.1 * s, at(sx * 0.72 * s, 0.7 * s, 0.14 * s), sw=PALE, turn=(0, yaw, -sx * 16), **kw)
            turned(P, [(0.0, -0.12 * s), (0.56 * s, -0.12 * s), (0.62 * s, 0.0), (0.56 * s, 0.12 * s), (0.0, 0.12 * s)], at(sx * 0.8 * s, 0.0, 0.1 * s), sw=PALE, turn=(90, yaw, 0), seg=10)
            turned(P, [(0.0, -0.1 * s), (0.34 * s, -0.1 * s), (0.36 * s, 0.0), (0.34 * s, 0.1 * s), (0.0, 0.1 * s)], at(sx * 0.8 * s, 0.0, 0.2 * s), sw=TURQ, turn=(90, yaw, 0), seg=10)
            o((0.13, 0.13, 0.08), (sx * 0.8, 0.0, 0.3), DARK)
        b((0.26, 0.5, 0.4), 0.1, (0, -0.55, 0.22), OCHRE, taper=(0.3, 1.0))
        b((0.8, 0.14, 0.14), 0.06, (0, -1.16, 0.05), DARK)


# ------------------------------------------------------------------ the head every block shares, and the shafts
def top_motif(S, kind, paint):
    def fn(img, info, r2):
        w, h = img.size
        m = Image.new("L", (w, h), 0)
        (S.calm_motif if K.CALM else S.motif)(ImageDraw.Draw(m), kind, w / 2, h / 2, min(w, h) * 0.2)
        relief(img, m, paint=paint, rnd=r2, depth=2, worn=0.6)
    return fn


def head(S, P, hx, hz, seed, lid_ppm):
    r = random.Random(seed)
    k0 = seed % 5
    kinds = tuple(KINDS[(k0 + i * (1 + seed % 3)) % 5] for i in range(5))
    paints = tuple(r.sample([OCHRE, RED, CORAL, TURQ, OCHRE], 5))
    big = min(hx, hz) >= 2.4
    lid(P, hx, hz, 0.0, 0.3, FAM.paved_top(S, seed=seed, extra=top_motif(S, kinds[2], CTX["mark"] if K.CALM and CTX else paints[0]) if big else None, course=1.5 if big else 1.1, length=(1.3, 2.1) if big else (0.9, 1.5)), ppm=lid_ppm)
    box(P, (hx, 0.46, hz), 0.2, (0, -0.52, 0), paint=S.stone_wall(course=0.46, length=(1.3, 2.2), tone=1.14, moss=0.5, drips=0.2, damp=0.0, seed=seed + 1), seg=SG, rings=RG)
    box(P, (hx - 0.3, 0.22, hz - 0.3), 0.06, (0, -1.1, 0), sw=DARK, seg=8, rings=4)
    box(P, (hx - 0.16, 0.6, hz - 0.16), 0.12, (0, -1.82, 0), paint=S.carve_band(seed=seed + 2, kinds=kinds, paints=paints), ppm=P.ppm * 1.3, seg=SG, rings=RG)
    box(P, (hx - 0.07, 0.17, hz - 0.07), 0.09, (0, -2.52, 0), paint=S.worn_paint(CTX["band"] if K.CALM and CTX else PAINTS[seed % len(PAINTS)], seed=seed + 3), seg=SG, rings=RG)
    return -2.66


def wall(S, seed, damp=0.4, tone=1.0, motifs=None):
    w = S.stone_wall(seed=seed, damp=damp, moss=0.5 + damp * 0.4, drips=0.4 + damp * 0.5, tone=tone, course=0.72, length=(1.0, 1.8))
    return S.big_motif(w, motifs) if motifs else w


def shaft(S, P, style, hx, hz, y0, seed):
    """From y0 down to -13.6. Returns (the half sizes of the wall a face may sit on, that wall's centre height)."""
    r = random.Random(seed * 3 + 1)
    names = ["front", "right", "back", "left"]
    mot = {names[(seed + i) % 4]: (KINDS[(seed + i) % 5], [OCHRE, RED, TURQ, CORAL][(seed + i) % 4]) for i in range(2)}
    kw = dict(seg=SG, rings=RG)
    bot = -13.6
    I = 0.38
    if K.CALM:                                        # calm: one panel to a side, high where no carved face is, else lower
        up = [sd for sd in SIDES if sd not in CTX["face"]]
        lo = [sd for sd in SIDES if sd in CTX["face"]]

    else:
        up = lo = []

    def wl(S_, sd, damp=0.4, tone=1.0, motifs=None, sides=None):
        return calm_wall(S_, sd, damp, tone, sides) if K.CALM else wall(S_, sd, damp, tone, motifs)
    if style == "tiers":
        h1 = 2.5
        box(P, (hx - I, h1, hz - I), 0.3, (0, y0 - h1 + 0.1, 0), paint=wl(S, seed, sides={sd: 0.5 for sd in up}), taper=(0.92, 1.0), bulge=0.03, **kw)
        box(P, (hx - I + 0.04, 0.17, hz - I + 0.04), 0.1, (0, y0 - 2 * h1 + 0.1, 0), sw=PALE, seg=8, rings=4)
        h2 = (y0 - 2 * h1 - bot) / 2
        box(P, (hx - I - 0.3, h2, hz - I - 0.3), 0.3, (0.06, bot + h2, -0.05), paint=wl(S, seed + 1, 0.7, 0.92, mot, sides={sd: 0.3 for sd in lo}), taper=(0.88, 1.0), turn=(0, r.uniform(-7, 7), 0), **kw)
        return (hx - I, hz - I), y0 - h1 + 0.1
    if style == "stepped":
        hh = (y0 - bot) / 6
        for k in range(3):
            ins = I + k * 0.34
            box(P, (hx - ins, hh + 0.05, hz - ins), 0.28, (0, y0 - hh * (2 * k + 1), 0), paint=wl(S, seed + k, 0.3 + 0.25 * k, 1.0 - 0.05 * k, mot if k == 1 else None, sides={sd: 0.5 for sd in (up, lo, [])[k]}), turn=(0, r.uniform(-3, 3), 0), **kw)
            if k < 2:
                box(P, (hx - ins - 0.08, 0.16, hz - ins - 0.08), 0.1, (0, y0 - hh * (2 * k + 2), 0), sw=PALE, seg=8, rings=4)
        return (hx - I, hz - I), y0 - hh
    if style == "stack":
        hh = (y0 - bot) / 6
        for k in range(3):
            ox, oz = r.uniform(-0.16, 0.16), r.uniform(-0.16, 0.16)
            box(P, (hx - I - 0.12 * k, hh + 0.08, hz - I - 0.12 * k), 0.34, (ox, y0 - hh * (2 * k + 1), oz), paint=wl(S, seed + k, 0.3 + 0.25 * k, 1.0 - 0.05 * k, mot if k == 1 else None, sides={sd: 0.5 for sd in (up, lo, [])[k]}),
                taper=(r.uniform(0.9, 1.04), 1.0), turn=(0, r.uniform(-9, 9), 0), lean=(r.uniform(-0.1, 0.1), r.uniform(-0.1, 0.1)), **kw)
        return (hx - I - 0.1, hz - I - 0.1), y0 - hh
    if style == "fluted":
        hh = (y0 - bot) / 2
        box(P, (hx - I - 0.12, hh, hz - I - 0.12), 0.3, (0, y0 - hh, 0), paint=wl(S, seed, 0.6, 1.0, mot, sides={sd: (1.65 / (2 * hh), 2.5) for sd in up}), taper=(0.9, 1.0), **kw)   # calm: above the ledge; a carved face is its side's motif
        box(P, (hx - I + 0.04, 0.18, hz - I + 0.04), 0.1, (0, y0 - 3.4, 0), sw=PALE, seg=8, rings=4)
        grey = (176, 172, 164)
        for sx in (-1, 1):
            for sz in (-1, 1):                                   # a pilaster at every corner, and one mid way along the long sides
                box(P, (0.3, hh * 0.62, 0.3), 0.14, (sx * (hx - I - 0.16), y0 - hh * 0.62, sz * (hz - I - 0.16)), sw=grey, taper=(0.7, 1.0), seg=8, rings=4)
        if hx > hz + 0.5:
            for sz in (-1, 1):
                box(P, (0.3, hh * 0.5, 0.26), 0.14, (0.0, y0 - 3.4 - hh * 0.5, sz * (hz - I - 0.1)), sw=grey, taper=(0.7, 1.0), seg=8, rings=4)
        elif hz > hx + 0.5:
            for sx in (-1, 1):
                box(P, (0.26, hh * 0.5, 0.3), 0.14, (sx * (hx - I - 0.1), y0 - 3.4 - hh * 0.5, 0.0), sw=grey, taper=(0.7, 1.0), seg=8, rings=4)
        return (hx - I - 0.12, hz - I - 0.12), y0 - 1.7
    if style == "arch":
        lh = 1.9
        box(P, (hx - I, lh, hz - I), 0.3, (0, y0 - lh + 0.1, 0), paint=wl(S, seed, sides={sd: 0.5 for sd in up}), bulge=0.02, **kw)
        long_x = hx >= hz
        L, W = (hx, hz) if long_x else (hz, hx)
        ph = (y0 - 2 * lh - bot) / 2 + 0.2
        for sg in (-1, 1):
            pl = L * 0.27
            c = sg * (L - I - pl - 0.06)
            half = (pl, ph, W - I - 0.14) if long_x else (W - I - 0.14, ph, pl)
            pos = (c, bot + ph, 0) if long_x else (0, bot + ph, c)
            box(P, half, 0.3, pos, paint=wl(S, seed + 2 + sg, 0.7, 0.92, sides={sd: 0.4 for sd in lo}), taper=(0.9, 1.0), **kw)
            kh = (pl * 0.6, 0.5, W - I - 0.3) if long_x else (W - I - 0.3, 0.5, pl * 0.6)
            kp = (sg * (L - I - 2 * pl - 0.2), y0 - 2 * lh - 0.3, 0) if long_x else (0, y0 - 2 * lh - 0.3, sg * (L - I - 2 * pl - 0.2))
            box(P, kh, 0.2, kp, sw=PALE, taper=(0.5, 1.0), seg=8, rings=4)
        box(P, (0.34, 0.42, W - I + 0.05) if long_x else (W - I + 0.05, 0.42, 0.34), 0.14, (0, y0 - 2 * lh + 0.22, 0), sw=PAINTS[(seed + 2) % 6], taper=(0.7, 1.0), seg=8, rings=4)
        return (hx - I, hz - I), y0 - lh + 0.1
    if style == "drum":
        rx, rz = hx - I, hz - I

        def paint(img, info):
            stones(img, info["ppm"], random.Random(seed), course=0.72, length=(0.9, 1.6))
            weather(img, info["ppm"], random.Random(seed + 1), moss=0.6, drips=0.6, damp=0.6)
            if K.CALM:                                  # four panels round the drum's middle (-5.4 to -8.5), on the diagonals
                h = img.size[1]
                calm_band(S, img, info, (h * (1 - 6 / 12), h * (1 - 5 / 12)), 3.1, 0.89 / 1.02, (45, 135, 225, 315))
            return img
        prof = [(rx * 0.78, bot), (rx * 0.84, -9.6), (rx * 0.98, -9.2), (rx * 0.98, -8.9), (rx * 0.88, -8.5), (rx * 0.9, -5.4), (rx * 1.0, -5.0), (rx * 1.0, -4.7), (rx * 0.92, -4.3), (rx * 0.96, y0 - 0.3), (rx * 1.02, y0)]
        turned(P, prof, (0, 0, 0), seg=14, squash=rz / rx, paint=paint)
        for yy in (-4.85, -9.05):
            turned(P, [(rx * 1.02, yy - 0.12), (rx * 1.05, yy), (rx * 1.02, yy + 0.12)], (0, 0, 0), sw=PALE, seg=14, squash=rz / rx)
        return (rx * 0.9, rz * 0.9), y0 - 0.9
    raise ValueError(style)


def dress(S, P, hx, hz, seed, roots=3, moss=2, ferns=1, start=-0.95):
    r = random.Random(seed * 7 + 5)
    for k in range(roots):
        side = r.randrange(4)
        t = r.uniform(-0.7, 0.7)
        x, z = ((t * hx, hz - 0.3), (hx - 0.3, t * hz), (t * hx, -hz + 0.3), (-hx + 0.3, t * hz))[side]
        pts, radii = [], []
        ph, amp = r.uniform(0, 6.28), r.uniform(0.2, 0.5)
        drop = r.uniform(4.0, 10.0)
        r0 = r.uniform(0.09, 0.14)
        for i in range(7):
            u = i / 6
            off = amp * math.sin(ph + u * 5) * (0.3 + u)
            pts.append((x + (off if side % 2 == 0 else 0), start - drop * u, z + (off if side % 2 else 0)))
            radii.append(r0 * (1.25 - u))
        pipe(P, pts, radii, BARK if k % 2 == 0 else LEAF, seg=6)
    for k in range(moss):
        sx, sz = r.choice((-1, 1)), r.choice((-1, 1))
        ball(P, (0.5, 0.16, 0.42), (sx * (hx - 0.9), -2.36, sz * (hz - 0.9)), MOSS, seg=8, rings=4)
    for k in range(ferns):
        sx, sz = r.choice((-1, 1)), r.choice((-1, 1))
        S.fern(P, r, sx * (hx - 0.7), -2.5, sz * (hz - 0.7), 0.9, n=4)


def meta_for(P, hx, hz, depth=13.7, name=""):
    w = 2 * max(hx, hz)
    P.meta.update({"size": "top %.1f x %.1f m exactly, %.1f m modelled" % (2 * hx, 2 * hz, depth), "pivot": "the centre of the top (y 0)", "ground": -depth, "ink": 0.035, "width": w + 0.6,
                   "eye_z": 1.7, "look_z": -4.0, "distance": 8.0 + w * 1.05, "figure": (hx * 0.4, -hz * 0.3), "figure_z": 0.0, "fov": 62,
                   "close": [(0.0, -2.6, hz), (hx * 0.9 + 1.5, -1.2, hz + 4.0 + hx)]})
    return P


def face_yaws(bid, sx, sz, small):
    yaws = (0, 180) if sx >= sz else (90, 270)
    out = {"slabN": (0,), "slabS": (180,), "slabE": (90,), "slabW": (270,)}.get(bid)
    return out or yaws[:1 if small else 2]


def block(S, glb, bid, sx, sz, style, design):
    hx, hz = sx / 2, sz / 2
    seed = seed_of(bid)
    small = min(sx, sz) < 5.5
    P = Piece(glb, size=1024 if small else 2048, ppm=(40 if small else 46) * PPM_SCALE, seed=seed)
    if K.CALM:
        calm_setup(S, glb, seed, [YAW_SIDE[y] for y in face_yaws(bid, sx, sz, small)] if design is not None else ())
    y0 = head(S, P, hx, hz, seed, lid_ppm=P.ppm * 1.35)
    (fx, fz), fy = shaft(S, P, style, hx, hz, y0, seed)
    if style != "drum":
        for (cx, cz) in ((1, 1), (-1, 1), (-1, -1), (1, -1)):                              # corbels under the painted course, a bead of inlay under each
            box(P, (0.4, 0.46, 0.4), 0.16, (cx * (hx - 0.5), -3.06, cz * (hz - 0.5)), sw=PALE, taper=(0.5, 1.0), seg=8, rings=4)
            ball(P, (0.18, 0.18, 0.18), (cx * (hx - 0.4), -3.64, cz * (hz - 0.4)), TURQ, seg=8, rings=4)
    if design is not None:
        long_x = sx >= sz
        s = min(1.45, (max(sx, sz) - 1.6) / 2.6, 1.45 if style != "arch" else 1.0)
        if style == "arch":
            fy += 0.2
        yaws = face_yaws(bid, sx, sz, small)
        for i, yaw in enumerate(yaws):
            a = math.radians(yaw)
            d = fz if long_x else fx
            face_lite(P, d * math.sin(a), fy - (0.2 * s if style != "arch" else 0.0), d * math.cos(a), s, yaw, (design + i) % 4, fins=[CORAL, TURQ, OCHRE, RED][(seed + i) % 4], crown=((OCHRE, TURQ), (RED, OCHRE), (TURQ, CORAL))[(seed + i) % 3])
    dress(S, P, hx, hz, seed, roots=2 if small else 3, moss=1 if small else 2, ferns=1)
    return meta_for(P, hx, hz)


# ------------------------------------------------------------------ the three towers that are buildings
def tower(S, glb, kind):
    hx = hz = 4.0
    seed = seed_of(glb)
    P = Piece(glb, size=2048, ppm=44, seed=seed)
    if K.CALM:
        calm_setup(S, glb, seed, ("front", "back") if kind == "tower_owl" else ())
    y0 = head(S, P, hx, hz, seed, lid_ppm=56)
    r = random.Random(seed)
    kw = dict(seg=SG, rings=RG)
    if kind == "tower_shrine":
        # a house of stone under the cap: thatch eaves, four glowing windows, a balcony, then a waisted shaft
        thatch = calm_thatch if K.CALM else (lambda img, info: K.tile_from("thatch_payo", img, info["ppm"], 2.6, tint=(0.96, 0.92, 0.86)))
        for (yy, hh, ins, tp) in ((-3.3, 0.6, 0.02, (1.0, 0.82)), (-4.25, 0.5, 0.0, (1.0, 0.86))):       # two courses of eave, inside the footprint
            box(P, (hx - ins, hh, hz - ins), 0.16, (0, yy, 0), paint=thatch, taper=tp, **kw)
        box(P, (3.2, 1.9, 3.2), 0.3, (0, -6.3, 0), paint=wall(S, seed, 0.3), **kw)
        for yaw in (0, 90, 180, 270):
            a = math.radians(yaw)
            box(P, (0.62, 0.86, 0.12), 0.1, (3.2 * math.sin(a), -6.1, 3.2 * math.cos(a)), sw=K.GLOW, turn=(0, yaw, 0), seg=8, rings=4)          # a window that can glow
            box(P, (0.86, 0.14, 0.2), 0.08, (3.24 * math.sin(a), -5.1, 3.24 * math.cos(a)), sw=PALE, turn=(0, yaw, 0), seg=8, rings=4)
            box(P, (0.86, 0.12, 0.26), 0.08, (3.26 * math.sin(a), -7.1, 3.26 * math.cos(a)), sw=PAINTS[(seed + yaw // 90) % 6], turn=(0, yaw, 0), seg=8, rings=4)
        box(P, (3.6, 0.2, 3.6), 0.12, (0, -8.3, 0), sw=PALE, seg=8, rings=4)                              # the balcony
        for sx in (-1, 1):
            for sz in (-1, 1):
                box(P, (0.16, 0.5, 0.16), 0.08, (sx * 3.4, -7.7, sz * 3.4), sw=K.TIMBER, seg=8, rings=4)
        box(P, (2.7, 2.6, 2.7), 0.34, (0, -11.0, 0), paint=(calm_wall(S, seed + 1, 0.7, 0.92, {sd: 0.5 for sd in SIDES}) if K.CALM else wall(S, seed + 1, 0.7, 0.92, {"front": ("ancestor", RED), "back": ("rice", OCHRE)})), taper=(0.86, 1.0), **kw)
    elif kind == "tower_drum":
        # a round tower: a ring of gongs under the cap, a painted band, a waist, a flared foot
        def paint(img, info):
            stones(img, info["ppm"], random.Random(seed), course=0.75, length=(1.0, 1.8))
            weather(img, info["ppm"], random.Random(seed + 1), moss=0.6, drips=0.6, damp=0.55)
            if K.CALM:                                  # four panels round the waist (-10.6 to -8.4), between the slots
                h = img.size[1]
                calm_band(S, img, info, (h * (1 - 3 / 9), h * (1 - 2 / 9)), 2.2, 0.72 / 1.02, (112, 22, 292, 202))
            return img
        rr = 3.55
        turned(P, [(rr * 0.82, -13.6), (rr * 0.7, -10.6), (rr * 0.74, -8.4), (rr * 0.98, -7.9), (rr * 0.98, -7.5), (rr * 0.9, -7.1), (rr * 0.94, -3.2), (rr * 1.02, y0)], (0, 0, 0), seg=16, paint=paint)
        turned(P, [(rr * 1.0, -7.86), (rr * 1.04, -7.7), (rr * 1.0, -7.54)], (0, 0, 0), sw=PALE, seg=16)
        turned(P, [(rr * 0.955, -5.3), (rr * 0.975, -5.0), (rr * 0.955, -4.7)], (0, 0, 0), sw=RED, seg=16)
        for k in range(8):                                                                             # the gongs
            a = math.tau * (k + 0.5) / 8
            turned(P, [(0.0, -0.1), (0.62, -0.1), (0.66, 0.0), (0.62, 0.1), (0.2, 0.16), (0.0, 0.2)], (rr * 0.96 * math.sin(a), -3.95, rr * 0.96 * math.cos(a)), sw=OCHRE, turn=(90, math.degrees(a), 0), seg=10)
        for k in range(4):
            a = math.tau * k / 4 + 0.4
            box(P, (0.5, 0.9, 0.3), 0.14, (rr * 0.72 * math.sin(a), -9.4, rr * 0.72 * math.cos(a)), sw=K.GLOW if k % 2 else DARK, turn=(0, math.degrees(a), 0), seg=8, rings=4)   # slots, two lit
    else:
        # the owl tower: a head with great inlaid eyes on two sides, swept ear tufts, a feathered breast
        box(P, (3.5, 2.6, 3.5), 0.7, (0, -5.2, 0), paint=(calm_wall(S, seed, 0.3, 1.04, {"left": 0.62, "right": 0.62}) if K.CALM else wall(S, seed, 0.3, 1.04)), taper=(0.9, 1.0), bulge=0.05, **kw)
        face_lite(P, 0.0, -5.2, 3.46, 1.9, 0, 2, fins=TURQ, crown=(CORAL, OCHRE))
        face_lite(P, 0.0, -5.2, -3.46, 1.5, 180, 1, fins=OCHRE, crown=(RED, TURQ))
        for sx in (-1, 1):
            box(P, (0.36, 1.2, 0.8), 0.2, (sx * 3.6, -3.9, 0.0), sw=CORAL, taper=(1.0, 0.4), lean=(sx * 0.3, 0), seg=8, rings=4)
        box(P, (2.4, 0.5, 2.4), 0.3, (0, -8.2, 0), sw=DARK, seg=8, rings=4)

        def breast(img, info):
            if K.CALM:
                return calm_breast(img, info)
            wall(S, seed + 1, 0.6, 0.94)(img, info)
            w, h = img.size
            r2 = random.Random(seed)
            rows = 4
            for row in range(rows):
                m = Image.new("L", (w, h), 0)
                dm = ImageDraw.Draw(m)
                n = int(w / (h / rows * 0.9))
                for k in range(n + 1):
                    cx = (k + (0.5 if row % 2 else 0)) * w / n
                    cy = (row + 0.25) * h / rows
                    dm.pieslice([cx - w / n * 0.5, cy - h / rows * 0.5, cx + w / n * 0.5, cy + h / rows * 0.62], 0, 180, fill=255)
                relief(img, m, paint=[OCHRE, CORAL, TURQ, OCHRE][row], rnd=r2, depth=2, worn=0.7)
            return img
        def calm_breast(img, info):
            """Calm: two rows of broad feathers in the block's accent pair, soft, raised in the height map."""
            calm_wall(S, seed + 1, 0.6, 0.94)(img, info)
            w, h = img.size
            for row, col in enumerate((CTX["field"], K.mix3(CTX["field"], CTX["ink"], 0.6))):
                m = Image.new("L", (w, h), 0)
                dm = ImageDraw.Draw(m)
                n = max(4, int(w / (h * 0.42)))
                for k in range(n + 1):
                    cx = (k + (0.5 if row % 2 else 0)) * w / n
                    cy = h * (0.2 + 0.28 * row)
                    dm.pieslice([cx - w / n * 0.5, cy - h * 0.22, cx + w / n * 0.5, cy + h * 0.3], 0, 180, fill=255)
                relief(img, m, paint=col, depth=3)
            return img
        box(P, (3.0, 2.5, 3.0), 0.5, (0, -11.1, 0), paint=breast, taper=(0.8, 1.0), bulge=0.05, **kw)
    dress(S, P, hx, hz, seed, roots=4, moss=2, ferns=1)
    return meta_for(P, hx, hz)


# ------------------------------------------------------------------ the stair slabs
def stair(S, glb, down):
    """A slab whose top is a ramp, for the layout's stairDown: 6 m of run falling 1.2 m toward `down` (+x or -x), 10 m
    wide. The origin is the centre of the footprint at the HIGH edge's height (y 0), as the block's `top`; the art's
    ramp lies 1 cm under a ramp collider between the same two edges."""
    hx, hz, drop = 3.0, 5.0, 1.2
    sg = 1 if down == "+x" else -1
    seed = seed_of(glb)
    P = Piece(glb, size=1024, ppm=30, seed=seed)
    if K.CALM:
        calm_setup(S, glb, seed, ("right",) if sg > 0 else ("left",))

    def paint(img, info):
        FAM.paved_top(S, seed=seed, course=1.0, length=(1.4, 2.2))(img, info)
        w, h = img.size
        m = info["margin"]
        if K.CALM:                                               # calm: six broad treads, each a soft step in light and height
            n = 6
            base = K.calm_tone((150, 148, 144))
            for k in range(1, n):
                x = m + (w - 2 * m) * k / n
                g = Image.new("L", (w, h), 0)
                ImageDraw.Draw(g).rectangle([x - 2.5, m + 6, x + 2.5, h - m - 6], fill=255)
                gb = g.filter(ImageFilter.GaussianBlur(2.0))
                soft(img, gb, K.shade(base, 0.86), 0.7)
                soft(img, ImageChops.offset(g, -4 * sg, 0).filter(ImageFilter.GaussianBlur(2.0)), PALE, 0.35)
                K.hset(img, gb, -0.025)
            return img
        n = 12
        for k in range(1, n):                                    # treads: a cut line and its pale edge, every half metre
            x = m + (w - 2 * m) * k / n
            g = Image.new("L", (w, h), 0)
            ImageDraw.Draw(g).rectangle([x - 1.5, m + 6, x + 1.5, h - m - 6], fill=255)
            soft(img, g.filter(ImageFilter.GaussianBlur(0.7)), INK, 0.8)
            soft(img, ImageChops.offset(g, -3 * sg, 0).filter(ImageFilter.GaussianBlur(0.8)), PALE, 0.6)
        return img

    ppm = 40
    rect = P.alloc(2 * hx * ppm + 16, 2 * hz * ppm + 16)
    info = {"ppm": ppm, "W": 2 * hx, "D": 2 * hz, "w": rect[2] - rect[0], "h": rect[3] - rect[1], "margin": 8}
    P.paint(rect, paint, info)
    u0, v0, u1, v1 = P.uvrect(rect)
    mu = 8.0 / P.S

    def yt(x):
        t = (x + hx) / (2 * hx)
        return -0.0 - drop * (t if sg > 0 else 1 - t)

    yb = -drop - 0.5
    c = [(-hx, -hz), (hx, -hz), (hx, hz), (-hx, hz)]

    def uv(x, z, out=False):
        u = u0 + mu + (u1 - u0 - 2 * mu) * (x + hx) / (2 * hx) + (mu * 0.8 * (1 if x > 0 else -1) if out else 0)
        v = v0 + mu + (v1 - v0 - 2 * mu) * (z + hz) / (2 * hz) + (mu * 0.8 * (1 if z > 0 else -1) if out else 0)
        return (u, v)
    pos, nrm, uvs, tris = [], [], [], []

    def quad(pts, n, uu):
        b = len(pos)
        pos.extend(pts); nrm.extend([n] * 4); uvs.extend(uu)
        tris.extend([(b, b + 1, b + 2), (b, b + 2, b + 3)])
    ln = math.hypot(2 * hx, drop)
    quad([(x, yt(x), z) for (x, z) in reversed(c)], (sg * drop / ln, 2 * hx / ln, 0), [uv(x, z) for (x, z) in reversed(c)])
    quad([(x, yb, z) for (x, z) in c], (0, -1, 0), [uv(x, z, True) for (x, z) in c])
    for i in range(4):
        (ax, az), (bx, bz) = c[i], c[(i + 1) % 4]
        n = (az - bz, 0, bx - ax)
        l2 = math.hypot(n[0], n[2])
        quad([(ax, yb, az), (bx, yb, bz), (bx, yt(bx), bz), (ax, yt(ax), az)][::-1], (-n[0] / l2, 0, -n[2] / l2), [uv(ax, az, True), uv(bx, bz, True), uv(bx, bz), uv(ax, az)][::-1])
    P.add((np.array(pos, np.float32), np.array(nrm, np.float32), np.array(tris, np.uint32)), np.array(uvs, np.float32))
    y0 = yb + 0.06
    box(P, (hx - 0.3, 0.22, hz - 0.3), 0.06, (0, y0 - 0.2, 0), sw=DARK, seg=8, rings=4)
    box(P, (hx - 0.16, 0.6, hz - 0.16), 0.12, (0, y0 - 0.95, 0), paint=S.carve_band(seed=seed + 2), ppm=36, seg=SG, rings=RG)
    box(P, (hx - 0.07, 0.17, hz - 0.07), 0.09, (0, y0 - 1.66, 0), paint=S.worn_paint(PAINTS[seed % 6], seed=seed), seg=SG, rings=RG)
    shaft(S, P, "tiers", hx, hz, y0 - 1.8, seed)
    face_lite(P, sg * (hx - 0.38), y0 - 5.2, 0.0, 1.0, 90 if sg > 0 else 270, 0 if sg > 0 else 1)
    dress(S, P, hx, hz, seed, roots=3, moss=0, ferns=0, start=-2.4)
    meta_for(P, hx, hz)
    P.meta.update({"size": "footprint 6.0 x 10.0 m exactly; the top is a ramp from y 0 down 1.2 m toward %s" % down, "close": [(0.0, -0.6, 0.0), (sg * 7.5, 4.5, 9.0)]})
    return P


# ------------------------------------------------------------------ movers and pads
def mover(S, glb, sx, sz, accent):
    """A slab that moves during a round: a thin stone raft with a turquoise ring inlaid in its top and painted corner
    studs, so it reads as the thing that moves; a carved band; a hanging boss with roots. Origin at its top's centre."""
    hx, hz = sx / 2, sz / 2
    seed = seed_of(glb)
    P = Piece(glb, size=1024, ppm=70, seed=seed)
    if K.CALM:
        calm_setup(S, glb, seed)

    def extra(img, info, r2):
        w, h = img.size
        rr = min(w, h) * 0.3
        g = Image.new("L", (w, h), 0)
        ImageDraw.Draw(g).ellipse([w / 2 - rr, h / 2 - rr, w / 2 + rr, h / 2 + rr], outline=255, width=max(4, int(info["ppm"] * (0.09 if not K.CALM else 0.16))))
        if K.CALM:                                     # calm: a broad inlay ring, set in, no ink shadow
            gb = g.filter(ImageFilter.GaussianBlur(1.2))
            soft(img, gb, TURQ)
            K.hset(img, gb, -0.01)
        else:
            soft(img, ImageChops.offset(g, 2, 3), INK, 0.7)
            soft(img, g, TURQ)
        m = Image.new("L", (w, h), 0)
        (S.calm_motif if K.CALM else S.motif)(ImageDraw.Draw(m), "spiral", w / 2, h / 2, rr * 0.6)
        relief(img, m, paint=accent, rnd=r2, depth=2, worn=0.5)
    lid(P, hx, hz, 0.0, 0.26, FAM.paved_top(S, seed=seed, extra=extra, course=1.0, length=(0.9, 1.5)), ppm=80)
    box(P, (hx, 0.3, hz), 0.14, (0, -0.4, 0), paint=S.stone_wall(course=0.4, length=(0.8, 1.3), tone=1.12, moss=0.5, drips=0.2, damp=0.0, seed=seed), seg=SG, rings=RG)
    box(P, (hx - 0.14, 0.3, hz - 0.14), 0.1, (0, -0.9, 0), paint=S.carve_band(seed=seed + 1, kinds=("spiral", "sun", "diamond", "spiral", "sun")), seg=SG, rings=RG)
    box(P, (hx - 0.3, 0.45, hz - 0.3), 0.2, (0, -1.5, 0), paint=S.stone_wall(course=0.45, length=(0.7, 1.2), tone=0.96, moss=0.8, drips=0.7, damp=0.5, seed=seed + 2), taper=(0.55, 1.0), seg=SG, rings=RG)
    for sxx in (-1, 1):
        for szz in (-1, 1):
            ball(P, (0.17, 0.17, 0.17), (sxx * (hx - 0.05), -0.4, szz * (hz - 0.05)), accent, seg=8, rings=4)
    r = random.Random(seed)
    for k in range(3):
        x, z = r.uniform(-0.5, 0.5), r.uniform(-0.5, 0.5)
        pipe(P, [(x, -1.8, z), (x + r.uniform(-0.3, 0.3), -2.6, z + r.uniform(-0.3, 0.3)), (x + r.uniform(-0.5, 0.5), -r.uniform(3.2, 4.2), z + r.uniform(-0.5, 0.5))], [0.09, 0.06, 0.02], BARK if k else LEAF, seg=6)
    ball(P, (0.4, 0.14, 0.34), (hx - 0.5, -0.02, -hz + 0.5), MOSS, seg=8, rings=4)
    meta_for(P, hx, hz, depth=4.4)
    P.meta.update({"size": "top %.1f x %.1f m exactly, 4.2 m with its roots" % (sx, sz), "ground": -6.0, "distance": 7.5, "look_z": -1.0, "close": [(0.0, 0.0, 0.0), (2.6, 3.0, 4.2)]})
    return P


def pad(S, glb):
    seed = seed_of(glb)
    P = Piece(glb, size=512, ppm=110, seed=seed)
    if glb == "pad_geyser":
        # a carved stone bowl set on the floor, 2.4 m across and 12 cm high at its rim, standing water in it
        def bowl(img, info):
            stones(img, info["ppm"], random.Random(seed), course=0.3, length=(0.5, 0.9), tone=1.1)
            return img
        turned(P, [(1.2, 0.0), (1.2, 0.06), (1.08, 0.12), (0.92, 0.12), (0.84, 0.05), (0.0, 0.03)], (0, 0, 0), seg=18, paint=bowl)
        turned(P, [(0.0, 0.02), (0.86, 0.02), (0.86, 0.075), (0.0, 0.075)], (0, 0, 0), sw=WATER, seg=18)
        turned(P, [(0.9, 0.118), (0.94, 0.132), (0.98, 0.118)], (0, 0, 0), sw=TURQ, seg=18)
        for k in range(8):
            a = math.tau * k / 8
            box(P, (0.1, 0.05, 0.06), 0.03, (1.06 * math.cos(a), 0.12, 1.06 * math.sin(a)), sw=OCHRE, turn=(0, -math.degrees(a) + 90, 0), seg=8, rings=4)
        P.meta.update({"size": "2.4 m across, 0.14 m high; origin on the floor at its centre; the jet and steam are the game's"})
    elif glb == "pad_speed":
        # a low painted slab, 1.5 across and 3 along its travel (+z), three chevrons
        def top(img, info):
            if K.CALM:
                return calm_speed(img, info)
            w, h = img.size
            d = ImageDraw.Draw(img)
            d.rectangle([0, 0, w, h], fill=OCHRE)
            r2 = random.Random(seed)
            for _ in range(14):
                x, y, rr = r2.uniform(0, w), r2.uniform(0, h), r2.uniform(8, 22)
                d.ellipse([x - rr, y - rr * 0.6, x + rr, y + rr * 0.6], fill=K.shade(OCHRE, r2.choice((0.9, 1.1))))
            for k in range(3):
                cy = h * (0.24 + k * 0.26)
                d.polygon([(w * 0.2, cy - h * 0.08), (w * 0.5, cy + h * 0.02), (w * 0.8, cy - h * 0.08), (w * 0.8, cy - h * 0.0), (w * 0.5, cy + h * 0.1), (w * 0.2, cy)], fill=INK)
            d.rectangle([0, 0, w - 1, h - 1], outline=PALE, width=10)
            return img.filter(ImageFilter.GaussianBlur(0.7))
        def calm_speed(img, info):
            """Calm: one ochre with a soft gradient along the travel, three bold chevrons in deep red, raised."""
            w, h = img.size
            d = ImageDraw.Draw(img)
            for y in range(h):
                d.line([(0, y), (w, y)], fill=K.shade(OCHRE, 0.94 + 0.10 * y / max(1, h - 1)))
            m = Image.new("L", (w, h), 0)
            dm = ImageDraw.Draw(m)
            for k in range(3):
                cy = h * (0.24 + k * 0.26)
                dm.polygon([(w * 0.2, cy - h * 0.08), (w * 0.5, cy + h * 0.02), (w * 0.8, cy - h * 0.08), (w * 0.8, cy + h * 0.01), (w * 0.5, cy + h * 0.11), (w * 0.2, cy + h * 0.01)], fill=255)
            mb = m.filter(ImageFilter.GaussianBlur(1.0))
            soft(img, mb, (128, 58, 44))
            K.hset(img, mb.filter(ImageFilter.GaussianBlur(2.0)), 0.01)
            fr = Image.new("L", (w, h), 0)
            ImageDraw.Draw(fr).rectangle([0, 0, w - 1, h - 1], outline=255, width=12)
            soft(img, fr.filter(ImageFilter.GaussianBlur(3.0)), PALE, 0.5)
            return img
        lid(P, 0.75, 1.5, 0.05, 0.05, top, ppm=110)
        P.meta.update({"size": "1.5 x 3.0 m, 5 cm high; origin on the floor at its centre; travel is +z (chevrons point that way)"})
    else:
        # a low offering stone the stamina pickup floats over: 0.9 m across, 0.4 m high, a woven band
        turned(P, [(0.46, 0.0), (0.46, 0.08), (0.36, 0.13), (0.36, 0.27), (0.43, 0.32), (0.43, 0.38), (0.0, 0.4)], (0, 0, 0), sw=(170, 168, 162), seg=12)
        turned(P, [(0.375, 0.15), (0.385, 0.2), (0.375, 0.25)], (0, 0, 0), sw=RED, seg=12)
        turned(P, [(0.44, 0.33), (0.455, 0.35), (0.44, 0.37)], (0, 0, 0), sw=TURQ, seg=12)
        P.meta.update({"size": "0.9 m across, 0.4 m high; origin on the floor at its centre"})
    P.meta.update({"pivot": "on the floor at its centre", "ground": 0.0, "ink": 0.012, "width": 3.4, "distance": 5.0, "look_z": 0.1, "fov": 40, "figure": (1.9, -0.6), "close": [(0.0, 0.1, 0.0), (1.6, 1.3, 2.2)]})
    return P


def ext(S, glb, sx, sz):
    """A plain shaft to stand under a block: origin the block's top centre, it runs from 12.6 m down to 52.6 m down."""
    # 2026-10-18 (owner: "the textures look low res"): was 512 px at 7 px a metre, the softest paint on the stage.
    P = Piece(glb, size=2048, ppm=28, seed=seed_of(glb))
    box(P, (sx * 0.3, 20.0, sz * 0.3), 0.3, (0, -32.6, 0), paint=S.stone_wall(course=1.6, length=(2.0, 3.4), tone=0.78, moss=0.3, drips=0.6, damp=0.8, top=False, seed=3), seg=8, rings=4)
    P.meta.update({"size": "%.1f x %.1f m, from 12.6 to 52.6 m under the block's top" % (sx * 0.6, sz * 0.6), "pivot": "the block's top centre", "ground": -52.7, "close": [(0, -14, 0), (8, -10, 12)]})
    return P


def fitted(fn):
    global PPM_SCALE
    for k in (1.0, 0.86, 0.74, 0.64):
        PPM_SCALE = k
        try:
            return fn()
        except AssertionError:
            continue
    raise SystemExit("the atlas will not fit")


def builders(S):
    fp, _ = footprints()
    out = {}
    for bid, (glb, style, design) in PLAN.items():
        if style is None:
            continue
        x, z, sx, sz = fp[bid]
        if style.startswith("tower_"):
            out[glb] = (lambda g=glb, k=style: tower(S, g, k))
        else:
            out[glb] = (lambda g=glb, b=bid, a=sx, c=sz, st=style, de=design: fitted(lambda: block(S, g, b, a, c, st, de)))
    out["stair_E"] = lambda: stair(S, "stair_E", "+x")
    out["stair_W"] = lambda: stair(S, "stair_W", "-x")
    out["mover_lift"] = lambda: mover(S, "mover_lift", 3, 3, OCHRE)
    out["mover_ferry"] = lambda: mover(S, "mover_ferry", 3, 3, CORAL)
    out["mover_piston"] = lambda: mover(S, "mover_piston", 4, 4, RED)
    for g in ("pad_geyser", "pad_speed", "pickup_plinth"):
        out[g] = (lambda g=g: pad(S, g))
    for (sx, sz) in sorted({(v[2], v[3]) for v in fp.values()}):
        nm = "ext_%gx%g" % (sx, sz)
        out[nm] = (lambda n=nm, a=sx, c=sz: ext(S, n, a, c))
    return out


def table():
    fp, data = footprints()
    stats = json.loads((K.OUT / "stats.json").read_text())
    rows = []
    for bid, (glb, style, design) in PLAN.items():
        x, z, sx, sz = fp[bid]
        st = stats.get(glb, {})
        rows.append({"id": bid, "glb": glb + ".glb", "top": [sx, sz], "layout_xz": [x, z], "style": style or "hero (locked)", "triangles": st.get("triangles"), "open_edges": st.get("open_edges"),
                     "extension": "ext_%gx%g.glb" % (sx, sz)})
    total = sum(r["triangles"] or 0 for r in rows)
    other = {n: stats.get(n, {}).get("triangles") for n in ("stair_E", "stair_W", "mover_lift", "mover_ferry", "mover_piston", "pad_geyser", "pad_speed", "pickup_plinth")}
    doc = {
        "note": "Written by tools/payo_pieces/stone_blocks.py table. Metres. Every block glb: ORIGIN = the centre of the block's collision top; the art's top face is at the origin's "
                "height, so place it 1 cm UNDER the collider's top (y = top - 0.01). glTF axes: +x the layout's +x, +y up, +z the layout's +z. Tops are exactly the layout's "
                "footprints. 13.6 m is modelled under the top; stand `extension` at the same origin to carry the shaft down 40 m more.",
        "blocks": rows, "blocks_triangles": total,
        "stairs": {"slabE": {"glb": "stair_E.glb", "down": "+x", "drop": 1.2}, "slabW": {"glb": "stair_W.glb", "down": "-x", "drop": 1.2},
                   "note": "Where a layout gives slabE or slabW a stairDown, draw this in place of the slab. Origin: the footprint's centre at the HIGH edge's height (the block's `top`). "
                           "The ramp is flat from that height down `drop` across the 6 m; place it 1 cm under a ramp collider with the same two edges."},
        "movers": {"lift": "mover_lift.glb", "ferry": "mover_ferry.glb", "piston": "mover_piston.glb", "note": "Origin the centre of the slab's top; 1 cm under the collider's top."},
        "pads": {"jumpPads": "pad_geyser.glb", "speedPads": "pad_speed.glb", "pickups": "pickup_plinth.glb", "note": "Origin on the floor. pad_speed's travel is its +z."},
        "other_triangles": other,
        "extensions_triangles": {n: v.get("triangles") for n, v in stats.items() if n.startswith("ext_")},
    }
    (K.OUT / "blocks.json").write_text(json.dumps(doc, indent=1), encoding="utf-8")
    for r in rows:
        print("%-10s %-16s %4gx%-4g %-14s %s" % (r["id"], r["glb"], r["top"][0], r["top"][1], r["style"], r["triangles"]))
    print("blocks total", total, "| other", other)


if __name__ == "__main__":
    if sys.argv[1:] == ["table"]:
        table()
    elif sys.argv[1:] == ["names"]:
        import stone_build as SB
        print(" ".join(builders(SB).keys()))
