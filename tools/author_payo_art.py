"""PAYO: THE ART (2026-10-12). The block kit from the layout, the pit, the gate, the mountain and its terraces, the far
world and the sky piece, checked and written out for the Unity scene builder.

  blender -b --python tools/author_payo_art.py -- [--review N] [--cams a,b] [--no-export] [--layout 0] [--quick]

Owner: "aztec/ancient tropic filipino civilization map ... rice terraces ... stone columns/monoliths that rise up from
... a deep pit ... rectangualr/square-ish ... more grey than sandy"; then "i dont think the whole map should be in an
enclosed basin space", "you can try having smaller moving platforms", "the map changes to night at half time", and of
Mayari "an ancient being in the sky" in "our current cartoony art style".

THE BLOCK ART IS GENERATED FROM THE LAYOUT (Assets/TumbangPreso/Art/Payo/payo_layout.json when it exists, else
tools/payo_layouts_draft.json): ONE PIECE PER BLOCK ID, its footprint exactly the block's, its origin the centre of the
block's COLLISION TOP and its own top 1 cm under that. So a piece placed at (x, top, z) cannot drift from its collider.
This file never writes the layout.

FILES: author_payo_art_kit.py (materials, the land's height function), author_payo_art_textures.py (run it first),
author_payo_art_check.py (the cover check that allows `hide_solids`, the coplanar check). The mesh buffer is Paete's
run's (author_paete_run_art_kit.py).

WRITES: Assets/TumbangPreso/Art/Payo/Models/*.glb, payo_art.json, materials_payo.json (no .blend is kept);
with --review N, Logs/payo/art/payo_art_<view>_vN.png (day and a night mock).

AXES: built in the layout's own frame (X = Unity x, Y = Unity z, Z = up). Every mesh is turned half a turn about Z as
it is exported, so that Unity (which reads a Blender glb as (-x, z, -y)) puts it back where the layout says. Placements
in payo_art.json are Unity: position (X, Z, Y), yaw in degrees = minus the yaw used here.
"""
import json
import math
import random
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Matrix, Vector

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import author_payo_art_kit as P                      # noqa: E402
import author_payo_art_check as CHK                  # noqa: E402
import author_payo_art_props as PR                   # noqa: E402

K, T, Buf = P.K, P.T, P.Buf
ROOT = HERE.parent
ART = ROOT / "Assets/TumbangPreso/Art/Payo"
MODELS = ART / "Models"
SOURCE = ROOT / "ArtSource/payo"
LOGS = ROOT / "Logs/payo/art"
ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
REVIEW = int(ARGV[ARGV.index("--review") + 1]) if "--review" in ARGV else 0
EXPORT = "--no-export" not in ARGV
ONLY = ARGV[ARGV.index("--cams") + 1].split(",") if "--cams" in ARGV else None
LAYOUT = int(ARGV[ARGV.index("--layout") + 1]) if "--layout" in ARGV else 0
QUICK = "--quick" in ARGV                            # skip the mountain, the forest and the far world (to look at the kit)

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
K.load_rock_kit()
rng = random.Random(11)

GAME = ART / "payo_layout.json"
LAY_PATH = GAME if GAME.exists() else ROOT / "tools/payo_layouts_draft.json"
DATA = json.loads(LAY_PATH.read_text(encoding="utf-8"))
LAYOUTS = DATA["layouts"]
PLAZA_Y, LEDGE_Y, BREACH_Y, BREACH_HALF, LIP, PIT, FLOOR = P.PLAZA_Y, P.LEDGE_Y, P.BREACH_Y, P.BREACH_HALF, P.LIP, P.PIT, P.FLOOR
SHAFT = 50.0                                         # every block's shaft: from its top to under the mist whatever its height

kitc = bpy.data.collections.new("kit"); sc.collection.children.link(kitc)
STATIC, KIT, PLACE, NIGHT_LIGHTS = {}, {}, [], []
TRIS = {}


def static(b, bevel=0.0, sharp=38.0, mode="wall"):
    o = b.obj(bevel=bevel, sharp=sharp)
    P.bake(o, b, mode)
    STATIC[b.name] = o
    return o


def kit(b, bevel=0.03, mode="prop"):
    o = b.obj(bevel=bevel, collection=kitc)
    P.bake(o, b, mode)
    KIT[b.name] = o
    return o


def put(model, x, y, z, yaw=0.0, scale=1.0, group="dress"):
    PLACE.append({"model": model, "p": (x, y, z), "yaw": yaw, "scale": scale, "group": group})


# =============================================================== a. the block kit, from the layout
def seed_of(s):
    return sum((i + 1) * ord(ch) for i, ch in enumerate(s))


def courses(b, x0, x1, y0, y1, z_top, z_bot, first=2.2):
    """The shaft: stacked courses in two greys, alternately set in, so the height reads and no face is one flat plane."""
    z, k = z_top, 0
    while z > z_bot + 1e-6:
        zn = max(z_bot, z - (first if k == 0 else 3.0))
        ins = 0.16 if k % 2 else 0.27
        b.box(x0 + ins, x1 - ins, y0 + ins, y1 - ins, zn, z, "py_block_side" if k % 2 else "py_block_side_dark")
        z, k = zn, k + 1


def hangings(b, r, x0, x1, y0, y1, base, dense=1.0, ins=0.0):
    """Moss hanging from under the cap, drip stains below it and lichen on the stone: decals a few millimetres off each
    face of a shaft set `ins` inside the footprint."""
    faces = ((T(0, y0 + ins, 0, 0.0), x1 - x0), (T(x1 - ins, 0, 0, math.pi / 2), y1 - y0), (T(0, y1 - ins, 0, math.pi), x1 - x0), (T(x0 + ins, 0, 0, -math.pi / 2), y1 - y0))
    for M, length in faces:
        usable = length - 2 * ins - 0.4
        n = max(1, int(usable / 1.7 * dense))
        for k in range(n):
            t = -usable / 2 + (k + r.uniform(0.2, 0.8)) * usable / n
            w = min(usable / n * 0.95, r.uniform(1.0, 1.9))
            kind = r.random()
            if kind < 0.5:
                P.decal(b, M @ T(t, 0, base + 0.02), w, w * r.uniform(0.9, 1.5), (r.randrange(4), 1))
            elif kind < 0.8:
                P.decal(b, M @ T(t, 0, base - r.uniform(0.2, 1.0)), w * 0.8, w * r.uniform(1.6, 2.6), (r.randrange(4), 2), off=0.004)
            else:
                P.decal(b, M @ T(t, 0, base - r.uniform(1.0, 5.0)), w, w, (r.randrange(4), 0), off=0.005)


def carved_face(b, M, w, paint, r):
    """A carved guardian face on a shaft: heavy brow, ring eyes with turquoise inlay, a block nose, a mouth with teeth,
    side fins and a crown of leaves, in worn paint. M sits on the stone's surface: x along it, +y OUT of it, z up.
    About 0.38 m of relief, so it stays inside a block's footprint."""
    s = w / 3.0
    def slab(x, z0, z1, hx, depth, mat, taper=1.0, skew=(0.0, 0.0)):
        b.block(M @ T(x, depth / 2, 0), hx, depth / 2, z0, z1, mat, taper=taper, skew=skew)
    b.tint = (1, 1, 1)
    slab(0, 0.5 * s, 0.86 * s, 1.18 * s, 0.36, "py_block_edge", taper=0.94)                 # the brow
    slab(0, -0.42 * s, 0.56 * s, 0.17 * s, 0.38, "py_block_edge", taper=0.62)                # the nose
    slab(0, -0.92 * s, -0.56 * s, 0.86 * s, 0.10, "py_block_dark")                           # the mouth
    for k in range(5):
        slab((-0.6 + k * 0.3) * s, -0.9 * s, -0.66 * s, 0.1 * s, 0.2, "py_block_edge", taper=0.8)
    for sx in (-1, 1):
        Me = M @ T(sx * 0.56 * s, 0, 0.16 * s) @ Matrix.Rotation(-math.pi / 2, 4, "X")
        b.lathe(Me, [(0.15 * s, 0.0), (0.33 * s, 0.0), (0.35 * s, 0.26), (0.2 * s, 0.3), (0.15 * s, 0.0)], 10, "py_block_edge", smooth=False)
        b.lathe(Me, [(0.0, 0.0), (0.16 * s, 0.0), (0.16 * s, 0.12), (0.0, 0.12)], 9, "py_inlay", smooth=False)
    b.tint = tuple(0.45 + 0.55 * c for c in paint)
    for sx in (-1, 1):                                                                      # painted fins beside the face
        slab(sx * 1.34 * s, -0.7 * s, 0.9 * s, 0.13 * s, 0.26, "py_block_paint", taper=0.7, skew=(sx * 0.06 * s, 0))
    for k, x in enumerate((-0.8, -0.4, 0.0, 0.4, 0.8)):                                     # a crown of leaves
        slab(x * s, 0.86 * s, (1.16 + 0.2 * (2 - abs(k - 2))) * s, 0.17 * s, 0.24, "py_block_paint", taper=0.4)
    b.tint = (1, 1, 1)


def roots(b, r, x0, x1, y0, y1, n=3, ins=0.45):
    """Roots and vines gripping a shaft: fat tubes wandering down from under the cap."""
    for _ in range(n):
        side = r.randrange(4)
        t = r.uniform(-0.4, 0.4)
        px = (x0 + ins - 0.06, x1 - ins + 0.06)[side % 2] if side < 2 else (x0 + x1) / 2 + t * (x1 - x0 - 2 * ins)
        py = (y0 + y1) / 2 + t * (y1 - y0 - 2 * ins) if side < 2 else (y0 + ins - 0.06, y1 - ins + 0.06)[side % 2]
        pts, z = [], -0.6
        for k in range(6):
            jx, jy = (0.0, r.uniform(-0.5, 0.5)) if side < 2 else (r.uniform(-0.5, 0.5), 0.0)
            pts.append((max(x0 + 0.2, min(x1 - 0.2, px + jx)), max(y0 + 0.2, min(y1 - 0.2, py + jy)), z))
            z -= r.uniform(1.2, 2.6)
        b.tube(pts, r.uniform(0.07, 0.14), "py_vine" if r.random() < 0.5 else "py_trunk", n=5, r1=0.03)


def block_piece(pid, sx, sy, stair=None):
    """One monolith, its collision top's centre at the origin, its own top 1 cm under: the top is exactly the layout's
    rectangle and flat. UNDER THE TOP EVERY BLOCK IS ITS OWN (owner's reference 239: "how organic and unique the
    building shapes are"): the shaft stands 0.45 m inside the footprint, so whatever it carries (a carved face, fins,
    corbels, a balcony, roots) can never touch a neighbour, and it is square, tapered, round or a leaning stack, with
    its own worn paint colour. `stair` = (direction, drop) makes the top a ramp."""
    name = "block_%s%s" % (pid, "_stair" if stair else "")
    b = Buf(name, seed=seed_of(name))
    r = random.Random(seed_of(pid) * 7 + 3)
    x0, x1, y0, y1 = -sx / 2, sx / 2, -sy / 2, sy / 2
    court = pid == "core" or pid.startswith("slab") or pid.startswith("corner")
    big = min(sx, sy) >= 6
    paint = PR.PASTEL[seed_of(pid) % len(PR.PASTEL)]
    style = "square" if court else ("drum" if pid.startswith("stone") and seed_of(pid) % 2 else ("stack" if pid.startswith("stone") else ("taper" if pid.startswith("mid") or pid.startswith("bridge") else "tower")))
    tone = 0.92 + 0.12 * ((seed_of(pid) * 37) % 10) / 10.0
    b.tint = (tone, tone, tone * 1.01)
    top = "py_block_top" if court else "py_block_top_b"
    if stair:
        d, drop = stair
        ax = 0 if d[1] == "x" else 1
        sg = 1 if d[0] == "+" else -1

        def zr(x, y):                                          # the ramp, 1 cm under the collider's
            t = ((x - x0) / sx if ax == 0 else (y - y0) / sy)
            t = t if sg > 0 else 1.0 - t
            return -0.01 - drop * t

        c = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
        b.quad([(x, y, zr(x, y)) for (x, y) in c], top, uvs=[(x, y) for (x, y) in c])
        zb = -drop - 0.62
        for i in range(4):
            (ax_, ay_), (bx_, by_) = c[i], c[(i + 1) % 4]
            b.quad([(ax_, ay_, zb), (bx_, by_, zb), (bx_, by_, zr(bx_, by_)), (ax_, ay_, zr(ax_, ay_))], "py_block_edge")
        n = int(round((sx if ax == 0 else sy) / 0.5))
        for k in range(1, n):                                  # cut lines across the ramp: treads
            t = k / n
            if ax == 0:
                xx = x0 + sx * t
                b.quad([(xx - 0.035, y0 + 0.2, zr(xx - 0.035, 0) + 0.004), (xx + 0.035, y0 + 0.2, zr(xx + 0.035, 0) + 0.004),
                        (xx + 0.035, y1 - 0.2, zr(xx + 0.035, 0) + 0.004), (xx - 0.035, y1 - 0.2, zr(xx - 0.035, 0) + 0.004)], "py_block_dark")
            else:
                yy = y0 + sy * t
                b.quad([(x0 + 0.2, yy - 0.035, zr(0, yy - 0.035) + 0.004), (x1 - 0.2, yy - 0.035, zr(0, yy - 0.035) + 0.004),
                        (x1 - 0.2, yy + 0.035, zr(0, yy + 0.035) + 0.004), (x0 + 0.2, yy + 0.035, zr(0, yy + 0.035) + 0.004)], "py_block_dark")
        body = zb
    else:
        b.box(x0, x1, y0, y1, -0.5, -0.014, "py_block_edge", top=top)
        body = -0.5
        if pid == "core":                                      # a carved floor medallion round the can: two rings and eight rays, inlaid flush
            for (ra, rb, mat, tn) in ((2.2, 2.5, "py_block_dark", (1, 1, 1)), (3.5, 3.72, "py_block_dark", (1, 1, 1)), (2.56, 2.72, "py_block_paint", (0.96, 0.74, 0.40))):
                b.tint = tn
                for k in range(24):
                    a0, a1 = math.tau * k / 24, math.tau * (k + 1) / 24
                    b.flat([(ra * math.cos(a0), ra * math.sin(a0)), (rb * math.cos(a0), rb * math.sin(a0)), (rb * math.cos(a1), rb * math.sin(a1)), (ra * math.cos(a1), ra * math.sin(a1))], -0.010, mat)
            b.tint = (0.96, 0.60, 0.46)
            for k in range(8):
                a = math.tau * (k + 0.5) / 8
                b.flat([(2.8 * math.cos(a - 0.12), 2.8 * math.sin(a - 0.12)), (3.42 * math.cos(a), 3.42 * math.sin(a)), (2.8 * math.cos(a + 0.12), 2.8 * math.sin(a + 0.12))], -0.010, "py_block_paint")
            b.tint = (tone, tone, tone)
        elif big:                                              # an inlaid border on the walking face
            a, c = 0.9, 1.06
            for poly in ([(x0 + a, y0 + a), (x1 - a, y0 + a), (x1 - a, y0 + c), (x0 + a, y0 + c)], [(x0 + a, y1 - c), (x1 - a, y1 - c), (x1 - a, y1 - a), (x0 + a, y1 - a)],
                         [(x0 + a, y0 + c), (x0 + c, y0 + c), (x0 + c, y1 - c), (x0 + a, y1 - c)], [(x1 - c, y0 + c), (x1 - a, y0 + c), (x1 - a, y1 - c), (x1 - c, y1 - c)]):
                b.flat(poly, -0.010, "py_block_dark")
        # what time has done to the top: lichen, a crack, moss at the rim, leaves. One decal a 2.4 m cell at most, never two in one place.
        nx, ny = max(1, int(sx / 2.4)), max(1, int(sy / 2.4))
        for i in range(nx):
            for j in range(ny):
                if r.random() < (0.3 if court else 0.6):
                    cx = x0 + (i + r.uniform(0.3, 0.7)) * sx / nx
                    cy = y0 + (j + r.uniform(0.3, 0.7)) * sy / ny
                    if pid == "core" and math.hypot(cx, cy) < 4.2:
                        continue
                    cell = r.choice(((0, 0), (1, 0), (2, 0), (3, 0), (0, 3), (1, 3), (3, 3), (r.randrange(4), 0)))
                    P.decal_flat(b, cx, cy, -0.006, min(sx / nx, sy / ny) * r.uniform(0.6, 0.86), cell, r.uniform(0, 6.28))
    # corbels: the cap steps in to the shaft
    b.box(x0 + 0.14, x1 - 0.14, y0 + 0.14, y1 - 0.14, body - 0.26, body, "py_block_dark")
    b.box(x0 + 0.3, x1 - 0.3, y0 + 0.3, y1 - 0.3, body - 0.56, body - 0.26, "py_block_side")
    bt = body - 0.56
    I = 0.45
    ax0, ax1, ay0, ay1 = x0 + I, x1 - I, y0 + I, y1 - I
    b.box(ax0, ax1, ay0, ay1, bt - 1.1, bt, "py_block_side")                                  # the relief band's stone
    P.band(b, ax0, ax1, ay0, ay1, bt - 1.1, bt, "py_carve", period=4.4, proud=0.006)
    b.tint = tuple(0.5 + 0.5 * c for c in paint)                                              # a painted course under the band, worn
    b.box(ax0 - 0.06, ax1 + 0.06, ay0 - 0.06, ay1 + 0.06, bt - 1.42, bt - 1.1, "py_block_paint")
    b.tint = (tone, tone, tone)
    zs = bt - 1.42
    if style == "drum":
        rad = min(sx, sy) / 2 - I - 0.05
        b.lathe(T(0, 0, 0), [(rad * 0.86, -SHAFT), (rad * 0.9, zs - 9.0), (rad, zs - 8.6), (rad * 0.92, zs - 5.0), (rad, zs - 4.6), (rad * 0.94, zs - 0.4), (rad + 0.2, zs)], 10, "py_block_side",
                squash=(sy / 2 - I - 0.05) / max(0.1, (sx / 2 - I - 0.05)) if abs(sx - sy) > 0.1 else 1.0, smooth=False)
    elif style == "stack":
        z, k = zs, 0
        while z > -SHAFT:
            zn = max(-SHAFT, z - r.uniform(2.2, 3.4))
            ins = I + r.uniform(0.0, 0.22)
            M = T(r.uniform(-0.1, 0.1), r.uniform(-0.1, 0.1), 0, r.uniform(-0.09, 0.09)) if z > -16 else T(0, 0, 0)
            b.block(M, sx / 2 - ins - 0.1, sy / 2 - ins - 0.1, zn, z, "py_block_side" if k % 2 else "py_block_side_dark", taper=r.uniform(0.94, 1.04))
            z, k = zn, k + 1
    elif style == "taper":
        z, k = zs, 0
        hx, hy = sx / 2 - I, sy / 2 - I
        for (dz, shrink) in ((4.0, 0.5), (5.0, 0.4), (SHAFT, 0.0)):
            zn = max(-SHAFT, z - dz)
            b.block(T(0, 0, 0), hx - shrink, hy - shrink, zn, z, "py_block_side" if k % 2 else "py_block_side_dark", taper=hx / (hx - shrink) if shrink else 1.0)
            b.box(-hx - 0.05 + shrink, hx + 0.05 - shrink, -hy - 0.05 + shrink, hy + 0.05 - shrink, zn - 0.3, zn, "py_block_edge")
            hx, hy, z, k = hx - shrink, hy - shrink, zn - 0.3, k + 1
            if z <= -SHAFT:
                break
    else:                                                      # square courses, alternately set in; towers get windows and a balcony
        z, k = zs, 0
        while z > -SHAFT + 1e-6:
            zn = max(-SHAFT, z - (2.4 if k == 0 else 3.0))
            ins = I + (0.0 if k % 2 else 0.11)
            b.box(x0 + ins, x1 - ins, y0 + ins, y1 - ins, zn, z, "py_block_side" if k % 2 else "py_block_side_dark")
            z, k = zn, k + 1
        if style == "tower":
            b.box(x0 + 0.12, x1 - 0.12, y0 + 0.12, y1 - 0.12, zs - 5.3, zs - 5.0, "py_block_edge")          # a balcony ledge
            for (M, ln) in ((T(0, y0 + I, 0, 0.0), sx), (T(x1 - I, 0, 0, math.pi / 2), sy), (T(0, y1 - I, 0, math.pi), sx), (T(x0 + I, 0, 0, -math.pi / 2), sy)):
                for t in (-ln * 0.22, ln * 0.22):                 # two dark window slots a side, framed pale, the sill painted
                    b.block(M @ T(t, -0.03, 0), 0.34, 0.03, zs - 4.2, zs - 2.9, "py_block_dark")
                    b.block(M @ T(t, -0.09, 0), 0.46, 0.09, zs - 2.9, zs - 2.66, "py_block_edge", taper=0.9)
                    b.tint = tuple(0.5 + 0.5 * c for c in paint)
                    b.block(M @ T(t, -0.09, 0), 0.44, 0.09, zs - 4.44, zs - 4.2, "py_block_paint")
                    b.tint = (tone, tone, tone)
    # faces: the court's blocks one on each long side, mids two, towers four, stones one
    sides = [(T(0, y0 + I, 0, math.pi), sx), (T(x1 - I, 0, 0, -math.pi / 2), sy), (T(0, y1 - I, 0, 0.0), sx), (T(x0 + I, 0, 0, math.pi / 2), sy)]
    want = {"tower": (0, 1, 2, 3), "taper": (0, 2) if sx >= sy else (1, 3), "square": (0, 1, 2, 3) if pid == "core" else ((0, 2) if sx >= sy else (1, 3)), "drum": (), "stack": (r.randrange(4),)}[style]
    if pid.startswith("bridge"):
        want = ()
    for i in want:
        M, ln = sides[i]
        w = min(3.4, ln - 2 * I - 0.9)
        if w < 1.3:
            continue
        zc = zs - (6.9 if style == "tower" else 1.7) - w / 3.0 * 1.0
        carved_face(b, M @ T(0, 0, zc), w, paint, r)
    b.tint = (tone, tone, tone)
    b.box(x0 + I - 0.06, x1 - I + 0.06, y0 + I - 0.06, y1 - I + 0.06, zs - 12.6, zs - 12.0, "py_block_dark") if style in ("square", "tower") else None
    roots(b, r, x0, x1, y0, y1, n=2 if court else 4)
    hangings(b, r, x0, x1, y0, y1, bt, ins=0.3 if False else I)
    b.tint = (1, 1, 1)
    return b


def mover_piece(name, sx, sy):
    b = Buf(name, seed=seed_of(name))
    r = random.Random(seed_of(name))
    x0, x1, y0, y1 = -sx / 2, sx / 2, -sy / 2, sy / 2
    b.tint = (0.9, 1.0, 0.98)
    b.box(x0, x1, y0, y1, -0.42, -0.014, "py_block_edge", top="py_mover")
    for cx in (x0 + 0.34, x1 - 0.34):                          # ochre studs set flush in the corners: this one moves
        for cy in (y0 + 0.34, y1 - 0.34):
            b.flat([(cx - 0.16, cy - 0.16), (cx + 0.16, cy - 0.16), (cx + 0.16, cy + 0.16), (cx - 0.16, cy + 0.16)], -0.010, "py_pad_speed")
    b.box(x0 + 0.22, x1 - 0.22, y0 + 0.22, y1 - 0.22, -0.72, -0.42, "py_block_dark")
    b.box(x0 + 0.5, x1 - 0.5, y0 + 0.5, y1 - 0.5, -1.25, -0.72, "py_block_side")
    P.band(b, x0 + 0.5, x1 - 0.5, y0 + 0.5, y1 - 0.5, -1.2, -0.76, "py_carve", period=0.9, proud=0.006)
    b.box(x0 + 0.9, x1 - 0.9, y0 + 0.9, y1 - 0.9, -1.7, -1.25, "py_block_side_dark")
    for _ in range(4):                                         # roots hanging under it
        ax, ay = r.uniform(x0 + 1, x1 - 1), r.uniform(y0 + 1, y1 - 1)
        b.tube([(ax, ay, -1.6), (ax + r.uniform(-0.3, 0.3), ay + r.uniform(-0.3, 0.3), -2.6), (ax + r.uniform(-0.5, 0.5), ay + r.uniform(-0.5, 0.5), -r.uniform(3.4, 4.6))], 0.07, "py_vine", n=5, r1=0.02)
    hangings(b, r, x0, x1, y0, y1, -0.42, dense=0.5, ins=0.0)
    return b


def pad_pieces():
    b = Buf("pad_geyser", seed=3)                              # a carved stone bowl set in the floor, standing water in it: origin on the floor
    b.lathe(T(0, 0, 0), [(1.2, 0.0), (1.2, 0.05), (1.06, 0.085), (0.9, 0.085), (0.82, 0.02)], 16, "py_block_edge", cap_bot=None, smooth=False)
    b.drum(T(0, 0, 0), 0.82, 0.0, 0.02, "py_block_dark", top="py_block_dark", n=16)
    b.drum(T(0, 0, 0), 0.8, 0.02, 0.05, "py_water", top="py_water", n=16)                # Unity adds the jet and the steam over this
    b.lathe(T(0, 0, 0), [(0.86, 0.086), (0.86, 0.09), (0.9, 0.09)], 16, "py_inlay", smooth=False)
    for k in range(8):
        a = math.tau * k / 8
        b.box(-0.09, 0.09, -0.05, 0.05, 0.0, 0.1, "py_pad_speed", yaw=0)
        b.v[-8:] = [tuple(T(0.94 * math.cos(a), 0.94 * math.sin(a), 0, a) @ Vector(v)) for v in b.v[-8:]]
    kit(b, 0.012)
    b = Buf("pad_speed", seed=4)                               # travel is local +Y; 1.5 across, 3 along
    b.box(-0.75, 0.75, -1.5, 1.5, 0.0, 0.035, "py_block_edge", top="py_pad_speed")
    for k in (-1, 0, 1):
        y = k * 0.9
        b.flat([(-0.55, y - 0.28), (0.0, y + 0.1), (0.0, y + 0.34), (-0.55, y - 0.04)], 0.039, "py_ink")
        b.flat([(0.0, y + 0.1), (0.55, y - 0.28), (0.55, y - 0.04), (0.0, y + 0.34)], 0.039, "py_ink")
    kit(b, 0.01)
    b = Buf("pickup_plinth", seed=5)                           # a low offering stone the stamina pickup floats over
    b.lathe(T(0, 0, 0), [(0.46, 0.0), (0.46, 0.08), (0.36, 0.12), (0.36, 0.26), (0.42, 0.30), (0.42, 0.36)], 10, "py_block_side", cap_top="py_block_edge", smooth=False)
    b.lathe(T(0, 0, 0), [(0.37, 0.13), (0.37, 0.25)], 10, "py_weave", smooth=False)
    kit(b, 0.012)


def blocks():
    plan, stairs = {}, {}
    for lay in LAYOUTS:
        for p in lay["pieces"]:
            plan[p["id"]] = (p["x"], p["z"], p["sx"], p["sz"])
            if "stairDown" in p:
                stairs[p["id"]] = (p["stairDown"].replace("z", "y"), round(p["top"] - p["low"], 4))
    for pid, (x, z, sx, sz) in plan.items():
        kit(block_piece(pid, sx, sz), 0.035)
    for pid, st in stairs.items():
        kit(block_piece(pid, plan[pid][2], plan[pid][3], stair=st), 0.035)
    movers = {}
    for lay in LAYOUTS:
        for m in lay.get("movers", []):
            base = "mover_" + m["id"].rstrip("NSEWab") + "_%gx%g" % (m["sx"], m["sz"])
            movers[m["id"]] = base
            if base not in KIT:
                kit(mover_piece(base, m["sx"], m["sz"]), 0.03)
    pad_pieces()
    return plan, stairs, movers


def place_layout(lay, stairs, movers):
    """The review's stage: the layout's pieces where the game will put them (movers part way along their travel)."""
    for p in lay["pieces"]:
        put("block_%s%s" % (p["id"], "_stair" if "stairDown" in p else ""), p["x"], p["z"], p["top"], group="block")
    for m in lay.get("movers", []):
        f = 0.42 if (m["from"][0], m["from"][2]) != (m["to"][0], m["to"][2]) else (1.0 if m["phase"] == 0 else 0.0)
        q = [m["from"][i] + (m["to"][i] - m["from"][i]) * f for i in range(3)]
        put(movers[m["id"]], q[0], q[2], q[1], group="block")
    for j in lay["jumpPads"]:
        put("pad_geyser", j["x"], j["z"], j["y"], group="block")
    for s in lay["speedPads"]:
        put("pad_speed", s["x"], s["z"], s["y"], yaw={"+z": 0.0, "-z": math.pi, "+x": -math.pi / 2, "-x": math.pi / 2}[s["along"]], group="block")
    for k in lay["pickups"]:
        put("pickup_plinth", k["x"], k["z"], k["y"], group="block")


# =============================================================== the pit, its lip, the broken north wall
def pit():
    b = Buf("payo_pit", seed=21)

    def wall(horiz, sg, m, a, t0, t1, z0, z1, proud=0.0, thick=0.6, top=None):
        if horiz:
            t0 = t0 if t0 > -a + 1e-3 else -(a + thick)
            t1 = t1 if t1 < a - 1e-3 else (a + thick)
        else:
            t0 = t0 if t0 > -a + 1e-3 else -(a - proud)
            t1 = t1 if t1 < a - 1e-3 else (a - proud)
        lo, hi = sorted((sg * (a - proud), sg * (a + thick)))
        if horiz:
            b.box(lo, hi, t0, t1, z0, z1, m, top=top)
        else:
            b.box(t0, t1, lo, hi, z0, z1, m, top=top)

    for sg in (-1, 1):
        for horiz in (True, False):
            north = (not horiz) and sg > 0
            spans = [(-LIP, -BREACH_HALF), (BREACH_HALF, LIP)] if north else [(-LIP, LIP)]
            for (t0, t1) in spans:
                p0, p1 = max(t0, -PIT), min(t1, PIT)
                if not north:
                    wall(horiz, sg, "py_pit_wall", LIP, t0, t1, LEDGE_Y - 0.6, PLAZA_Y - 0.35)                       # the upper step
                    wall(horiz, sg, "py_block_edge", LIP, t0, t1, PLAZA_Y - 0.35, PLAZA_Y + 0.28, proud=0.22, thick=0.75)   # the pale kerb
                    wall(horiz, sg, "py_pit_wall_dark", PIT, t0, t1, LEDGE_Y - 0.6, LEDGE_Y, thick=LIP - PIT - 0.001, top="py_pave")   # the ledge
                z, k = LEDGE_Y - 0.6, 0
                while z > FLOOR - 5.0:                                                                           # the sheer wall, in courses
                    zn = z - 3.2
                    wall(horiz, sg, "py_pit_wall" if k % 2 else "py_pit_wall_dark", PIT, p0, p1, zn, z, proud=0.07 if k % 2 else 0.0)
                    z, k = zn, k + 1
                bx = (sg * (PIT - 0.21), sg * (PIT + 0.2))
                lo, hi = min(bx), max(bx)
                if horiz:                                                                                       # the carved band round the shaft
                    b.box(lo, hi, p0 - 0.27, p1 + 0.27, -14.3, -13.1, "py_block_side")
                    P.band(b, lo, hi, p0 - 0.27, p1 + 0.27, -14.3, -13.1, "py_carve", proud=0.006)
                else:
                    q0, q1 = p0 + (0.215 if p0 <= -PIT + 1e-3 else 0.05), p1 - (0.215 if p1 >= PIT - 1e-3 else 0.05)
                    b.box(q0, q1, lo, hi, -14.3, -13.1, "py_block_side")
                    P.band(b, q0, q1, lo, hi, -14.3, -13.1, "py_carve", proud=0.006)
                if not north:
                    wall(horiz, sg, "py_block_dark", LIP, t0, t1, -8.3, -7.6, proud=0.12, thick=0.2)
                span = p1 - p0
                n = max(2, int(span / 2.2))
                for _ in range(n):
                    t = rng.uniform(p0 + 0.5, p1 - 3.3)
                    wall(horiz, sg, "py_moss", PIT, t, t + rng.uniform(0.8, 3.2), LEDGE_Y - rng.uniform(1.4, 5.5), LEDGE_Y - 0.66, proud=0.13, thick=0.02)
                for _ in range(int(n * 0.6)):
                    t = rng.uniform(p0 + 0.5, p1 - 1)
                    wall(horiz, sg, "py_vine", PIT, t, t + rng.uniform(0.25, 0.45), LEDGE_Y - rng.uniform(5, 19), LEDGE_Y - 0.7, proud=0.3, thick=-0.14)
                for _ in range(0):
                    t = rng.uniform(p0 + 0.5, p1 - 2)
                    wall(horiz, sg, "py_stone_wet", PIT, t, t + rng.uniform(0.5, 1.3), -rng.uniform(26, 40), -14.5, proud=0.11, thick=-0.02)
                for _ in range(0 if north else int(n * 0.3)):
                    t = rng.uniform(t0 + 0.5, t1 - 4.5)
                    wall(horiz, sg, "py_moss", LIP, t, t + rng.uniform(1, 4), PLAZA_Y - rng.uniform(1.2, 3.2), PLAZA_Y - 0.4, proud=0.1, thick=0.02)
    # THE NORTH WALL STANDS ABOVE THE COURT, broken: two stubs of a great wall either side of the notch the valley is seen through
    prof = ((32.86, 28.0, 8.0), (28.0, 24.5, 9.2), (24.5, 21.0, 7.0), (21.0, 18.0, 8.2), (18.0, 15.6, 4.6), (15.6, 13.6, 1.0), (13.6, 12.4, -3.0))
    for sx in (-1, 1):
        for q, (xa, xb, top) in enumerate(prof):
            x0, x1 = sorted((sx * xa, sx * xb))
            z, k = LEDGE_Y - 0.587, 0
            while z < top - 1e-6:
                zn = min(top, z + 1.7)
                ins = 0.0 if (k + q) % 2 else 0.12
                b.box(x0, x1, 30.6 + ins, 35.0 - ins, z, zn, "py_pit_wall" if (k + q) % 2 else "py_pit_wall_dark")
                z, k = zn, k + 1
            b.box(x0 - 0.06, x1 + 0.06, 30.42, 35.18, top, top + 0.32, "py_block_edge")
            if q in (1, 3):
                b.box(x0 + 0.6, x1 - 0.6, 30.9, 34.7, top + 0.32, top + 1.5, "py_block_edge")                      # merlons left standing
            for _ in range(3):
                t = rng.uniform(x0, x1 - 1.0)
                b.box(t, min(x1, t + rng.uniform(0.6, 1.8)), 30.44, 30.58, top - rng.uniform(0.8, 4.5), top - 0.02, "py_moss")
                tv = rng.uniform(x0 + 0.1, x1 - 0.5)
                b.box(tv, tv + 0.3, 30.30, 30.44, top - rng.uniform(3, 10), top - 0.03, "py_vine")
        xa, xb = sorted((sx * 29.2, sx * 18.2))
        b.box(xa, xb, 30.50, 30.6, 2.03, 3.17, "py_block_side")
        P.band(b, xa, xb, 30.50, 30.6, 2.03, 3.17, "py_carve", proud=0.006)
        # the corner tower, a guardian's seat on the brink
        cx = sx * 33.0
        z, k = -70.37, 0
        while z < 8.6:
            zn = min(8.6, z + 3.3)
            ins = 0.0 if k % 2 else 0.14
            b.box(cx - 3.5 + ins, cx + 3.5 - ins, 33 - 3.5 + ins, 33 + 3.5 - ins, z, zn, "py_pit_wall" if k % 2 else "py_pit_wall_dark")
            z, k = zn, k + 1
        b.box(cx - 3.62, cx + 3.62, 33 - 3.62, 33 + 3.62, 2.0, 3.2, "py_block_side")
        P.band(b, cx - 3.62, cx + 3.62, 33 - 3.62, 33 + 3.62, 2.0, 3.2, "py_carve", proud=0.006)
        b.box(cx - 3.8, cx + 3.8, 33 - 3.8, 33 + 3.8, 8.6, 9.0, "py_block_edge", top="py_block_top_b")
        put("bulul", cx, 33.0, 9.0, yaw=math.atan2(-1, -sx) + math.pi / 2, scale=7.5 / 9.0, group="static")
        b.box(sx * 13.4 - 0.9, sx * 13.4 + 0.9, 31.6, 34.4, 0.15, 0.93, "py_block_dark")                         # the spout in each broken end
        # the break's ragged sides stepping down to the sill
        for k in range(7):
            x0, x1 = sorted((sx * (BREACH_HALF - 0.07 - k * 1.5), sx * (BREACH_HALF - (k + 1) * 1.5 - rng.uniform(0.1, 0.8))))
            top = PLAZA_Y - 1.0 - k * 2.4 - rng.uniform(0, 1.2)
            b.box(x0, x1, PIT - 0.05 - 0.047 * k, LIP + 0.93 + 0.083 * k + (0.04 if sx > 0 else 0.0), BREACH_Y - 0.2 - 0.013 * k, top, "py_pit_wall" if k % 2 else "py_pit_wall_dark")
        for _ in range(8):                                                                                     # fallen stones on the sill
            s = rng.uniform(1.4, 3.0)
            m = T(sx * rng.uniform(5.5, BREACH_HALF - 1), rng.uniform(PIT + 0.6, LIP + 0.4), 0, rng.uniform(0, 6.28))
            rk = K.ROCKS[rng.choice(list(K.BOULDERS) + list(K.SLABS))]
            sc_ = s / 2.5
            m = m @ Matrix.Translation((0, 0, BREACH_Y - rk["bottom"] * sc_ - 0.15)) @ Matrix.Diagonal((sc_, sc_, sc_, 1))
            b.rock(K.ROCKS.index(rk), m, mat="py_rock")
    b.box(-BREACH_HALF, BREACH_HALF, PIT + 0.001, LIP + 0.9, FLOOR - 5.0, BREACH_Y, "py_pit_wall_dark", top="py_stone_wet")       # the sill
    # the east wall stands up to the water shelf
    ya, yb = P.shelf_span(35.0)[0] - 0.6, P.shelf_span(35.0)[1] + 0.6
    b.box(LIP + 0.76, LIP + 2.0, ya, yb, PLAZA_Y - 0.2, P.SHELF_Y - 0.3, "py_pit_wall")
    b.box(LIP + 0.6, LIP + 2.0, ya - 0.1, yb + 0.1, P.SHELF_Y - 0.3, P.SHELF_Y + 0.3, "py_block_edge")
    pit_dressing(b)
    static(b, 0.04)


SPOUTS = []


def pit_dressing(b):
    """What makes the pit a made place: niches with offerings and small guardians, carved heads that pour water,
    stalactite moss under the ledge, roots coming over the lip, stains, rubble on the ledge."""
    walls = ((1, 0, -math.pi / 2), (-1, 0, math.pi / 2), (0, -1, math.pi), (0, 1, 0.0))        # outward normal of each wall as seen from the pit, yaw to look into the pit
    for (nx, ny, yaw) in walls:
        north = ny > 0
        for k, t in enumerate((-21.0, -7.0, 7.0, 21.0)):
            if north and abs(t) < BREACH_HALF + 2:
                continue
            x, y = (nx * PIT, t) if nx else (t, ny * PIT)
            M = T(x, y, 0, yaw)                                 # local -Y looks into the pit
            if k % 2 == 0:                                      # a niche: lintel, jambs, a dark back, a jar and a small guardian
                b.block(M @ T(0, -0.02, 0), 1.0, 0.03, -18.57, -16.23, "py_block_dark")
                b.block(M @ T(0, -0.2, 0), 1.3, 0.2, -16.2, -15.8, "py_block_edge", taper=0.9)
                b.block(M @ T(0, -0.3, 0), 1.25, 0.3, -18.9, -18.6, "py_block_edge")
                for sx in (-1.1, 1.1):
                    b.block(M @ T(sx, -0.14, 0), 0.16, 0.14, -18.6, -16.2, "py_block_side", taper=0.9)
                b.xf = M @ T(0.45, -0.3, -18.6)
                PR.jar(b, 0, 0, 0, 0.8)
                b.xf = Matrix.Identity(4)
                p = M @ Vector((-0.3, -0.3, -18.6))
                put("bulul", p.x, p.y, p.z, yaw=yaw, scale=1.3 / 9.0, group="static")
                P.decal(b, M @ T(0, -0.02, -18.9), 2.4, 3.2, (k % 4, 2), off=0.004)
            else:                                               # a carved head with water coming out of its mouth
                p = M @ Vector((0, 0, -12.2))
                put("prop_spout_head", p.x, p.y, p.z, yaw=yaw, scale=1.5, group="static")
                SPOUTS.append((x, y, yaw))
            for _ in range(5):                                  # stalactite moss under the ledge
                tt = t + rng.uniform(-6, 6)
                q = M @ Vector((tt - t, -rng.uniform(0.2, 0.8), 0))
                P.ellipsoid(b, T(q.x, q.y, LEDGE_Y - 0.6 - 0.5), 0.3, 0.3, rng.uniform(0.5, 1.3), "py_moss", n=6, rings=3)
            for _ in range(2):                                  # a root over the lip
                tt = rng.uniform(-9, 9)
                q0, q1, q2 = M @ Vector((tt, 3.4, PLAZA_Y + 0.3)), M @ Vector((tt + rng.uniform(-1, 1), 2.6, PLAZA_Y - 3.0)), M @ Vector((tt + rng.uniform(-2, 2), -0.2, LEDGE_Y - rng.uniform(2, 9)))
                if not (north or (nx > 0 and -14 < tt + t < 30)):
                    b.tube([tuple(q0), tuple(q1), tuple(M @ Vector((tt, -0.25, LEDGE_Y + 0.1))), tuple(q2)], rng.uniform(0.1, 0.18), "py_trunk", n=5, r1=0.04)
            for _ in range(3):                                  # fallen stones on the ledge
                tt = t + rng.uniform(-6, 6)
                if north or (nx > 0 and -14 < tt < 30):
                    continue
                q = M @ Vector((tt - t, 1.5 + rng.uniform(-0.8, 0.8), 0))
                sz = rng.uniform(0.3, 0.7)
                b.block(T(q.x, q.y, 0, rng.uniform(0, 3)) @ Matrix.Rotation(rng.uniform(-0.3, 0.3), 4, "X"), sz, sz * 0.7, LEDGE_Y - 0.05, LEDGE_Y + sz * rng.uniform(0.7, 1.2), "py_block_side")


# =============================================================== the water shelf, east
def shelf():
    b = Buf("payo_shelf", seed=31)
    w = Buf("payo_shelf_water", seed=32)
    X = P.SHELF_X
    for k in range(6):
        x0, x1 = X[k], X[k + 1]
        z = P.SHELF_Y + 0.5 * k
        (a0, b0), (a1, b1) = P.shelf_span(x0), P.shelf_span(x1)
        b.prism([(x0, a0), (x1, a1), (x1, b1), (x0, b0)], z - 11.0, z - 0.02, "py_wall" if k % 2 else "py_wall_moss", top="py_mud")
        D = 0.42                                                 # half a dike
        b.prism([(x0, a0), (x0 + 0.9, a0), (x0 + 0.9, b0), (x0, b0)], z - 0.02, z + 0.3, "py_dike")                # the lip toward the pit
        if k == 5:
            b.prism([(x1 - 0.9, a1), (x1, a1), (x1, b1), (x1 - 0.9, b1)], z - 0.02, z + 0.3, "py_dike")
        xe = x1 - (0.9 if k == 5 else 0.0)
        nf = 3 + (k % 2)
        cuts = [q / nf for q in range(nf + 1)]
        for q, t in enumerate(cuts):                             # the side dikes and the cross dikes, each its own prism
            ya, yb = a0 + (b0 - a0) * t, a1 + (b1 - a1) * t
            lo, hi = (0.0, 2 * D) if q == 0 else ((-2 * D, 0.0) if q == nf else (-D, D))
            b.prism([(x0 + 0.9, ya + lo), (xe, yb + lo), (xe, yb + hi), (x0 + 0.9, ya + hi)], z - 0.02, z + 0.3, "py_dike")
        for q in range(nf):
            t0, t1 = cuts[q], cuts[q + 1]
            lo0 = 2 * D if q == 0 else D
            hi0 = 2 * D if q == nf - 1 else D
            poly = [(x0 + 0.9, a0 + (b0 - a0) * t0 + lo0), (xe, a1 + (b1 - a1) * t0 + lo0), (xe, a1 + (b1 - a1) * t1 - hi0), (x0 + 0.9, a0 + (b0 - a0) * t1 - hi0)]
            rice = (k, q) in ((1, 1), (2, 0), (3, 2), (4, 0), (5, 1), (5, 2))
            if rice:
                b.flat(poly, z + 0.2, "py_rice" if (k + q) % 2 else "py_rice_ripe")
            else:
                w.flat(poly, z + 0.12, "py_water")
    static(b, 0.03)
    static(w, mode="plain")
    for (x, y) in ((92, 6),):
        hamlet(x, y, P.shelf_z(x) + 0.3, -math.pi / 2)
    static_shelf_done = True


# =============================================================== kit: bulul, houses, fire
def bulul_piece():
    """A bulul, the Ifugao rice guardian, 9 m: seated on a waisted block, knees drawn up, forearms folded across the
    knees, a long back, a large calm head with a heavy brow. Faces local -Y. Round forms."""
    b = Buf("bulul", seed=41)
    S = 9.0
    M = Matrix.Scale(S, 4)
    m = "py_bulul"
    b.lathe(M, [(0.37, 0.0), (0.36, 0.06), (0.25, 0.09), (0.24, 0.13), (0.33, 0.16), (0.34, 0.19)], 10, m, cap_top=m, smooth=False)
    b.lathe(M @ T(0, 0.085, 0), [(0.12, 0.19), (0.19, 0.24), (0.165, 0.34), (0.16, 0.50), (0.185, 0.64), (0.14, 0.685), (0.062, 0.70), (0.058, 0.745)], 12, m, squash=0.72)
    for sx in (-1, 1):
        hip, knee, foot = (sx * 0.10, 0.02, 0.26), (sx * 0.115, -0.19, 0.52), (sx * 0.11, -0.21, 0.20)
        P.limb(b, M, hip, knee, 0.078, 0.062, m)
        P.limb(b, M, knee, foot, 0.06, 0.05, m)
        P.ellipsoid(b, M @ T(*knee), 0.066, 0.066, 0.066, m, n=8, rings=5)
        P.ellipsoid(b, M @ T(foot[0], foot[1] - 0.045, 0.205), 0.052, 0.088, 0.03, m, n=8, rings=4)
        sh, el = (sx * 0.185, 0.09, 0.655), (sx * 0.205, -0.07, 0.50)
        P.ellipsoid(b, M @ T(*sh), 0.064, 0.064, 0.062, m, n=8, rings=5)
        P.limb(b, M, sh, el, 0.052, 0.046, m)
        P.limb(b, M, el, (-sx * 0.07, -0.215, 0.575 + sx * 0.012), 0.044, 0.038, m)
        P.ellipsoid(b, M @ T(sx * 0.137, 0.065, 0.865), 0.02, 0.04, 0.055, m, n=6, rings=4)             # ears
    P.ellipsoid(b, M @ T(0, 0.06, 0.865), 0.132, 0.142, 0.155, m, n=12, rings=8)                         # the head, long and full
    P.ellipsoid(b, M @ T(0, 0.075, 0.975), 0.10, 0.11, 0.045, m, n=10, rings=4)                          # a low cap
    P.limb(b, M, (-0.105, -0.055, 0.905), (0.105, -0.055, 0.905), 0.026, 0.026, m, n=6)                  # the heavy brow
    P.limb(b, M, (0, -0.075, 0.895), (0, -0.105, 0.81), 0.02, 0.034, m, n=6)                             # the nose
    P.limb(b, M, (-0.045, -0.085, 0.775), (0.045, -0.085, 0.775), 0.014, 0.014, m, n=5)                  # the mouth
    b.lathe(M @ T(0, 0.085, 0), [(0.176, 0.275), (0.172, 0.335)], 12, "py_weave", squash=0.74, smooth=True)  # a woven band at the waist
    for _ in range(7):                                                                                   # moss on the shoulders, the cap, the knees
        a = rng.uniform(0, 6.28)
        rr = rng.uniform(0.02, 0.05)
        px, py, pz = rng.choice(((0.17, 0.09, 0.70), (-0.17, 0.09, 0.70), (0.0, 0.08, 1.0), (0.11, -0.17, 0.57), (-0.1, 0.2, 0.2)))
        P.ellipsoid(b, M @ T(px + 0.03 * math.cos(a), py + 0.03 * math.sin(a), pz), rr * 1.6, rr * 1.4, rr * 0.5, "py_moss", n=7, rings=3)
    kit(b, 0.0)


def village_kit():
    for name, fn in list(PR.PROPS.items()) + list(PR.BUILDINGS.items()) + list(PR.LIFE.items()):
        b = Buf(name, seed=seed_of(name))
        fn(b, random.Random(seed_of(name)))
        kit(b, 0.0 if name in PR.LIFE or name in ("fire_flame", "prop_mist_puff") else (0.025 if name in PR.BUILDINGS else 0.012))
    b = Buf("fire_bowl", seed=51)                                # a stone bowl on a short post: origin on the floor
    b.lathe(T(0, 0, 0), [(0.26, 0.0), (0.24, 0.06), (0.15, 0.12), (0.13, 0.86), (0.2, 0.92), (0.5, 1.2), (0.5, 1.26), (0.4, 1.26), (0.36, 1.14)], 10, "py_block_side", smooth=False)
    b.drum(T(0, 0, 0), 0.36, 1.12, 1.14, "py_block_dark", top="py_block_dark", n=10)
    b.lathe(T(0, 0, 0), [(0.145, 0.4), (0.145, 0.52)], 10, "py_inlay", smooth=False)
    kit(b, 0.012)
    # THE CATCH: a great hawk spirit that carries a fallen player back up. Three nodes in one glb: body, wing_L, wing_R.
    S_ = 2.6                                                     # wingspan about 11 m, body 5.2 m long; a 1.75 m player hangs in the talons under the origin
    body = Buf("catch_bird_body", seed=77)
    PR.bird_body(body, S_)
    ob = kit(body, 0.03)
    wings = []
    for side, nm in ((-1, "wing_L"), (1, "wing_R")):
        wb = Buf("catch_bird_" + nm, seed=78 + side)
        PR.bird_wing(wb, S_, side)
        ow = wb.obj(bevel=0.03, collection=kitc)
        P.bake(ow, wb, "plain")
        ow.parent = ob
        ow.location = (side * 0.3 * S_, -0.1 * S_, 0.42 * S_)
        wings.append(ow)
    CATCH["objects"] = [ob] + wings
    CATCH["info"] = {"model": "catch_bird", "nodes": ["catch_bird_body", "catch_bird_wing_L", "catch_bird_wing_R"], "wingspan": round(2 * (0.3 + 2.9) * S_, 2), "length": round(2.2 * S_, 2),
                     "pivot": "the body's origin is under its belly between the talons; it flies along local +Z in Unity (local -Y in Blender); each wing node's origin is its shoulder: flap by rotating the wing about its local Z (the body's long axis)",
                     "carry_point": [0.0, round(-0.62 * S_, 2), round(-0.05 * S_, 2)], "note": "talons hang 1.6 m under the origin: hold the player's shoulders there"}
    del KIT["catch_bird_body"]
    hb = Buf("hawk", seed=80)                                    # a small hawk to circle over the valley: one piece, wings spread
    PR.bird_body(hb, 0.3)
    for side in (-1, 1):
        hb.xf = T(side * 0.09, -0.03, 0.126)
        PR.bird_wing(hb, 0.3, side)
    hb.xf = Matrix.Identity(4)
    kit(hb, 0.0)
    pine()
    fig_trees()


CATCH = {}


def fig_trees():
    """Our own broad tree, light enough to plant a hillside with: a curling trunk, three dark crown masses, and leaf
    cards standing off them so the edge is leaves and not a ball."""
    for name, h, seed in (("tree_fig_a", 7.5, 1), ("tree_fig_b", 9.5, 2), ("tree_fig_c", 6.0, 3)):
        b = Buf(name, seed=seed_of(name))
        r = random.Random(seed)
        bend = r.uniform(0.6, 1.2) * (1 if seed % 2 else -1)
        b.tube([(0, 0, -0.4), (bend * 0.5, 0.1, h * 0.25), (-bend * 0.3, 0.2, h * 0.45), (bend * 0.2, 0, h * 0.62)], h * 0.05, "py_trunk", n=6, r1=h * 0.024)
        for q in range(3):
            a = math.tau * q / 3 + r.uniform(-0.3, 0.3)
            o = h * 0.2
            c = Vector((bend * 0.2 + o * math.cos(a), o * math.sin(a), h * (0.7 + 0.08 * q)))
            rad = h * r.uniform(0.2, 0.26)
            b.tube([(bend * 0.2, 0, h * 0.6), tuple(c)], h * 0.02, "py_trunk", n=5)
            P.ellipsoid(b, T(*c), rad, rad, rad * 0.72, "py_tree_core", n=8, rings=5)
            for i in range(11):
                th, ph = r.uniform(0, math.tau), r.uniform(-0.4, 1.3)
                d = Vector((math.cos(th) * math.cos(ph), math.sin(th) * math.cos(ph), math.sin(ph) * 0.72))
                p = c + d * rad * 0.82
                side = d.cross(Vector((0, 0, 1)))
                side = side.normalized() if side.length > 0.01 else Vector((1, 0, 0))
                up = side.cross(d).normalized()
                w = rad * r.uniform(0.5, 0.7)
                b.quad([tuple(p - side * w - up * w), tuple(p + side * w - up * w), tuple(p + side * w + up * w + d * w * 0.3), tuple(p - side * w + up * w + d * w * 0.3)],
                       "py_tree_leaf_light" if i % 3 else "py_tree_leaf_dark", uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])
        o_ = b.obj(bevel=0.0, collection=kitc, sharp=80.0)
        P.bake(o_, b, "plain")
        KIT[name] = o_


def pine():
    """Benguet pine: a tall bare trunk, boughs of needle cards in tiers, a dark core so it is never see-through."""
    for name, h in (("tree_pine_a", 15.0), ("tree_pine_b", 11.0)):
        b = Buf(name, seed=seed_of(name))
        r = random.Random(seed_of(name))
        b.tube([(0, 0, -0.5), (0.3, 0.1, h * 0.5), (0.2, -0.1, h)], h * 0.03, "py_trunk", n=6, r1=h * 0.008)
        for k in range(6):
            z = h * (0.45 + 0.09 * k)
            ln = h * (0.26 - 0.03 * k)
            P.ellipsoid(b, T(0.25, 0, z + 0.3), ln * 0.42, ln * 0.42, h * 0.05, "py_tree_core", n=7, rings=3)
            for q in range(5):
                a = math.tau * q / 5 + k * 0.7 + r.uniform(-0.2, 0.2)
                c = Vector((0.25, 0, z))
                d = Vector((math.cos(a), math.sin(a), 0))
                side = Vector((-d.y, d.x, 0)) * (ln * 0.34)
                tip = c + d * ln - Vector((0, 0, ln * 0.2))
                for (p, q2) in ((side, -side),):
                    pts = [tuple(c - side * 0.2), tuple(tip - side), tuple(tip + side + Vector((0, 0, ln * 0.2))), tuple(c + side * 0.2 + Vector((0, 0, ln * 0.14)))]
                    b.quad(pts, "py_pine", uvs=[(0.3, 0.0), (0.0, 1.0), (1.0, 1.0), (0.7, 0.0)])
        o = b.obj(bevel=0.0, collection=kitc, sharp=80.0)
        P.bake(o, b, "plain")
        KIT[name] = o


def fire(x, y, z, scale=1.0, power=900.0):
    put("fire_bowl", x, y, z, scale=scale, group="static")
    put("fire_flame", x, y, z + 1.16 * scale, scale=scale, group="night")
    NIGHT_LIGHTS.append((x, y, z + 1.9 * scale, power, 0.5 * scale))


def hamlet(x, y, z, yaw):
    """A few buildings on a paddy, each its own kind, on a stone standing, with the things people leave about."""
    M = T(x, y, z, yaw)
    names = list(PR.BUILDINGS)
    k0 = len(HAMLETS) * 3
    for i, ox in enumerate((-4.3, 0.2, 4.4)):
        model = names[(k0 + i * 5 + (i * i)) % len(names)]
        if model in ("slab_gate",):
            model = "house_bale"
        sc = 0.62 if model.startswith("shrine_step") else (0.8 if model.startswith("house_long") else 0.9)
        p = M @ Vector((ox, 0.3, 0.45))
        put(model, p.x, p.y, p.z, yaw + rng.uniform(-0.25, 0.25), scale=sc, group="static")
        w = M @ Vector((ox, -1.6, 2.5))
        NIGHT_LIGHTS.append((w.x, w.y, w.z + 0.45, 220.0, 0.4))
    for (ox, oy, model, sc) in ((-6.3, -1.6, "prop_jars", 1.0), (6.3, -1.5, "prop_baskets", 1.0), (2.3, -1.9, ("prop_banner_red", "prop_banner_black", "prop_banner_white")[len(HAMLETS) % 3], 1.0),
                                (-2.2, -1.9, ("prop_mortar", "prop_rice_rack", "prop_coop", "prop_firewood", "prop_drum")[len(HAMLETS) % 5], 1.0), (0.3, -2.1, "prop_lantern", 1.0)):
        q = M @ Vector((ox, oy, 0.45))
        put(model, q.x, q.y, q.z, yaw + rng.uniform(-0.5, 0.5), scale=sc, group="static")
    q = M @ Vector((-5.6, -1.9, 0.45))
    put("bulul", q.x, q.y, q.z, yaw, scale=1.7 / 9.0, group="static")
    for (ox, oy, kind, do) in ((1.2, -2.0, "villager_stand", "stands talking, turns to watch a throw"), (-1.0, -2.2, "chicken", "pecks, scatters when someone runs past"),
                               (3.4, -2.1, "child_wave", "waves at the players, jumps when the can falls")):
        if (len(HAMLETS) + len(kind)) % 2 == 0:
            q = M @ Vector((ox, oy, 0.45))
            life(kind, q.x, q.y, q.z, yaw + math.pi + rng.uniform(-0.5, 0.5), do)
    HAMLETS.append((x, y, z, yaw))


LIFE_LIST = []


def life(kind, x, y, z, yaw, do):
    put(kind, x, y, z, yaw, group="life")
    PLACE[-1]["do"] = do


HAMLETS = []


def hamlet_stones():
    b = Buf("payo_hamlets", seed=61)
    for (x, y, z, yaw) in HAMLETS:
        b.block(T(x, y, 0, yaw), 6.6, 2.5, z - 1.6, z + 0.45, "py_wall", top="py_pave")
    static(b, 0.03)


# =============================================================== the gate, south, uphill
def gate():
    """A dry stone platform in two low steps, like the paddy walls it grows out of, and on it a great Ifugao house
    raised on four carved posts with rat guard discs, which the way up the mountain passes under. Granaries flank it.
    Behind it the cut in the slope is held by a tall dry stone wall."""
    b = Buf("payo_gate", seed=71)
    S = lambda s: -s
    b.box(-27, 27, S(98), S(48), PLAZA_Y - 0.5, -0.02, "py_wall", top="py_pave")
    b.box(-22, 22, S(98), S(58), -0.02, 5.98, "py_wall_moss", top="py_pave")
    for (hw, s, zt) in ((27.0, 48.0, 0.0), (22.0, 58.0, 6.0)):
        b.box(-hw - 0.2, hw + 0.2, S(98.2), S(s - 0.2), zt - 1.5, zt - 0.3, "py_block_side")
        P.band(b, -hw - 0.2, hw + 0.2, S(98.2), S(s - 0.2), zt - 1.5, zt - 0.3, "py_carve", proud=0.006)
        b.box(-hw - 0.32, hw + 0.32, S(98.32), S(s - 0.32), zt - 0.3, zt, "py_block_edge", top="py_pave")
    n, s0, s1 = 30, 37.87, 57.87                                # the stair, wide and easy: 12 m up in 20 (its risers miss the platform's own faces)
    for k in range(n):
        s = s0 + (s1 - s0) * k / n
        b.box(-5.5, 5.5, S(s + (s1 - s0) / n + 0.02), S(s), PLAZA_Y - 0.38 - 0.003 * k, PLAZA_Y + 11.985 * (k + 1) / n, "py_block_side", top="py_block_top")
    for sx in (-1, 1):
        xa, xb = sorted((sx * 5.52, sx * 7.2))
        for k in range(6):
            s = s0 - 0.8 + (s1 - s0) * k / 6
            b.box(xa, xb, S(s + (s1 - s0) / 6), S(s), PLAZA_Y - 0.37 - 0.004 * k, PLAZA_Y + 12.0 * (k + 1) / 6 + 0.9, "py_pit_wall", top="py_block_edge")
        b.lathe(T(sx * 10.5, S(43.0), 0), [(4.0, PLAZA_Y - 0.3), (3.7, PLAZA_Y + 0.9)], 8, "py_pit_wall_dark", cap_top="py_pave", smooth=False)
        put("bulul", sx * 10.5, S(43.0), PLAZA_Y + 0.9, yaw=0.0, scale=6.5 / 9.0, group="static")
        fire(sx * 6.4, S(36.6), PLAZA_Y, 1.6, 2600.0)
        fire(sx * 6.4, S(61.5), 6.0, 1.6, 2600.0)
    top = 6.0
    for sx in (-1, 1):                                          # four carved posts
        for s in (68.0, 84.0):
            M = T(sx * 9.0, S(s), top)
            b.lathe(M, [(1.4, 0.0), (1.4, 0.5), (1.2, 0.7), (1.12, 3.2), (1.25, 3.3), (1.25, 4.9), (1.12, 5.0), (1.05, 9.3), (1.2, 9.5), (1.2, 10.2), (1.02, 10.4), (1.0, 11.6)], 12,
                    "py_timber", smooth=False)
            b.lathe(M, [(1.265, 3.34), (1.265, 4.86)], 12, "py_weave", smooth=False)
            b.lathe(M, [(1.215, 9.54), (1.215, 10.16)], 12, "py_cloth_red", smooth=False)
            b.lathe(M, [(1.0, 11.6), (3.2, 11.75), (3.2, 12.05), (2.2, 12.3), (1.1, 12.4), (1.0, 13.0)], 14, "py_timber_dark", cap_top="py_timber_dark", smooth=False)   # the rat guard
        put("bulul", sx * 9.0, S(63.2), top, yaw=0.0, scale=4.2 / 9.0, group="static")
    z0 = top + 13.0
    b.box(-13.0, 13.0, S(88.5), S(63.5), z0, z0 + 0.9, "py_timber_dark")
    b.box(-11.5, 11.5, S(87), S(65), z0 + 0.9, z0 + 6.4, "py_timber")
    P.band(b, -11.5, 11.5, S(87), S(65), z0 + 1.4, z0 + 4.0, "py_weave", period=2.6, proud=0.02)
    b.quad([(-1.6, S(65) + 0.03, z0 + 4.05), (1.6, S(65) + 0.03, z0 + 4.05), (1.6, S(65) + 0.03, z0 + 6.0), (-1.6, S(65) + 0.03, z0 + 6.0)], "py_window", uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])
    for sx in (-1, 1):
        b.quad([(sx * 6.5 - 0.9, S(65) + 0.03, z0 + 4.3), (sx * 6.5 + 0.9, S(65) + 0.03, z0 + 4.3), (sx * 6.5 + 0.9, S(65) + 0.03, z0 + 5.8), (sx * 6.5 - 0.9, S(65) + 0.03, z0 + 5.8)],
               "py_window", uvs=[(0, 0), (1, 0), (1, 1), (0, 1)])
        b.tube([(sx * 0.9, S(64.6), z0 + 0.9), (sx * 0.9, S(60.5), top)], 0.14, "py_timber_dark", n=6)
        NIGHT_LIGHTS.append((sx * 6.5, S(63.6), z0 + 5.0, 2400.0, 1.0))
    for k in range(9):
        t = (k + 0.5) / 9
        b.tube([(-0.9, S(64.6 - 4.1 * t), z0 + 0.9 - (z0 + 0.9 - top) * t), (0.9, S(64.6 - 4.1 * t), z0 + 0.9 - (z0 + 0.9 - top) * t)], 0.09, "py_timber_dark", n=5)
    zb, zt = z0 + 4.6, z0 + 27.0                                 # the great roof, in six courses of thatch
    Mr = T(0, S(76), 0)
    hx0, hy0, hx1, hy1 = 17.5, 17.0, 2.4, 1.2
    b.box(-18.0, 18.0, S(93.5), S(58.5), zb - 0.45, zb, "py_thatch_dark")
    N = 6
    for q in range(N):
        t0, t1 = q / N, (q + 1) / N
        ax, ay = hx0 + (hx1 - hx0) * t0, hy0 + (hy1 - hy0) * t0
        bx, by = hx0 + (hx1 - hx0) * t1 - 0.35, hy0 + (hy1 - hy0) * t1 - 0.35
        v0 = len(b.v)
        b.block(Mr, ax, ay, zb + (zt - zb) * t0, zb + (zt - zb) * t1, "py_thatch" if q % 2 == 0 else "py_thatch_dark", taper=bx / ax)
        for i in range(v0 + 4, v0 + 8):                          # the block's taper is one number: set the short side by hand
            vx, vy, vz = b.v[i]
            b.v[i] = (vx, S(76) + (by if vy > S(76) else -by), vz)
    b.box(-3.0, 3.0, S(77.6), S(74.4), zt, zt + 0.8, "py_timber_dark")
    for sx in (-1, 1):                                          # crossed finials, like horns
        b.tube([(sx * 1.6, S(76), zt + 0.6), (sx * 5.2, S(76), zt + 5.2)], 0.3, "py_timber_dark", n=6, r1=0.1)
        put("house_alang", sx * 19.5, S(53.0), 0.0, 0.0, scale=1.15, group="static")
        put("house_bale", sx * 18.0, S(72.0), 6.0, sx * 0.3, group="static")
        NIGHT_LIGHTS.append((sx * 19.5, S(55.4), 2.8, 300.0, 0.4)); NIGHT_LIGHTS.append((sx * 18.0, S(74.0), 8.6, 300.0, 0.4))
    # the wall that holds the cut slope, behind and beside the platform
    for (x0, x1, y0, y1, zt_) in ((-24.6, -22.3, S(98.29), S(58), 19.0), (22.3, 24.6, S(98.29), S(58), 19.0), (-24.6, 24.6, S(101.2), S(98.3), 19.5)):
        z, k = 5.9, 0
        while z < zt_:
            zn = min(zt_, z + 2.4)
            ins = 0.0 if k % 2 else 0.1
            b.box(x0 + ins, x1 - ins, y0 + ins, y1 - ins, z, zn, "py_wall" if k % 2 else "py_wall_moss")
            z, k = zn, k + 1
        b.box(x0 - 0.1, x1 + 0.1, y0 - 0.1, y1 + 0.1, zt_, zt_ + 0.3, "py_dike")
    for k in range(40):                                         # the way on up the mountain
        s = 101.3 + k * 1.1
        b.box(-2.2, 2.2, S(s + 1.1), S(s), 12.0 + k * 0.5, 19.9 + k * 0.52, "py_block_side", top="py_block_top_b")
    static(b, 0.035)


# =============================================================== the mountain: contoured terraces with walls and dikes
def clip(poly, i, t):
    """Split a convex polygon of vertex tuples where attribute i crosses t. (below, above, the cut's two points)."""
    lo, hi, cut = [], [], []
    n = len(poly)
    for j in range(n):
        a, c = poly[j], poly[(j + 1) % n]
        da, dc = a[i] - t, c[i] - t
        (lo if da < 0 else hi).append(a)
        if (da < 0) != (dc < 0):
            u = da / (da - dc)
            p = tuple(a[m] + (c[m] - a[m]) * u for m in range(len(a)))
            p = p[:i] + (t,) + p[i + 1:]
            lo.append(p); hi.append(p); cut.append(p)
    return lo, hi, cut


def mountain():
    """THE NEAR GROUND. Terraces are cut along the land's own contour lines: every 1.75 m cell that a level line
    crosses is split along it, so walls are smooth curves and upright, and each paddy is a clean polygon between its
    lip dike, its cross dikes and the wall above. Where the land is not terraced the cells are plain ground, and
    7 m cells where nothing changes. Vertex: (x, y, phi, h, T, psi)."""
    chunks = {}
    cur = [None]

    def at(x, y):
        key = (min(2, int((x + 350.0) // 233.4)), min(2, int((y + 350.0) // 233.4)))
        if key not in chunks:
            chunks[key] = Buf("payo_mountain_%d%d" % key, seed=81 + key[0] * 3 + key[1])
        cur[0] = chunks[key]

    wsheet = Buf("payo_paddy_water", seed=82)
    CS, NC, HALF = 1.75, 4, 350.0
    n = int(2 * HALF / CS)
    xs = np.arange(n + 1) * CS - HALF
    X, Y = np.meshgrid(xs, xs)
    F = P.field(X, Y)
    PHI, H, TT, E, RK, GATE, SHELFM = F["phi"], F["h"], F["T"], F["e"], F["rock"], F["gate"], F["shelf"]
    hole = 33.25
    STEP, DK = P.STEP, P.DIKE
    stats = {"fine": 0, "coarse": 0, "paddies": 0}

    def ground_mat(i, j, steep):
        if E[i, j] <= 0.0 and E[i + 1, j + 1] <= 0.0:
            return "py_pave"
        if GATE[i, j] or GATE[i + 1, j + 1]:
            return "py_pave" if not steep else "py_wall"
        if max(RK[i, j], RK[i + 1, j + 1]) > 0.3 or steep:
            return "py_rock"
        return "py_ground"

    def emit(buf, pts, mat):
        if len(pts) >= 3:
            buf.quad([(p[0], p[1], p[2]) for p in pts], mat, uvs=[(p[0], p[1]) for p in pts])

    def upright(p, q, za, zb, zc, zd, mat, toward):
        """A wall from the cut p-q, bottom heights za, zb, top heights zc, zd, facing the horizontal direction `toward`."""
        if max(zc - za, zd - zb) < 0.03:
            return
        ex, ey = q[0] - p[0], q[1] - p[1]
        if (ey * toward[0] - ex * toward[1]) < 0:               # the quad p, q, q', p' faces (ey, -ex)
            p, q, za, zb, zc, zd = q, p, zb, za, zd, zc
        ln = math.hypot(ex, ey)
        cur[0].quad([(p[0], p[1], za), (q[0], q[1], zb), (q[0], q[1], zd), (p[0], p[1], zc)], mat, uvs=[(p[0] + p[1], za), (p[0] + p[1] + ln, zb), (p[0] + p[1] + ln, zd), (p[0] + p[1], zc)])

    def zb_of(p, k, off):
        return p[3] + p[4] * (PLAZA_Y + STEP * k + off - p[3])

    nc = n // NC
    for ci in range(nc):
        for cj in range(nc):
            i0, j0 = ci * NC, cj * NC
            sl = (slice(i0, i0 + NC + 1), slice(j0, j0 + NC + 1))
            cx, cy = xs[j0] + CS * NC / 2, xs[i0] + CS * NC / 2
            if abs(cx) < hole and abs(cy) < hole:
                continue
            tmax = TT[sl].max()
            edge = (E[sl].min() <= 0.0) != (E[sl].max() <= 0.0)
            special = GATE[sl].any() or SHELFM[sl].any() or RK[sl].max() > 0.05 or edge
            near_hole = abs(cx) < hole + CS * NC and abs(cy) < hole + CS * NC
            if tmax < 0.02 and not special and not near_hole:   # one 7 m cell of plain ground
                c = [(xs[j0], xs[i0], H[i0, j0]), (xs[j0 + NC], xs[i0], H[i0, j0 + NC]), (xs[j0 + NC], xs[i0 + NC], H[i0 + NC, j0 + NC]), (xs[j0], xs[i0 + NC], H[i0 + NC, j0 + NC] * 0 + H[i0 + NC, j0])]
                spread = max(p[2] for p in c) - min(p[2] for p in c)
                at(cx, cy)
                emit(cur[0], c, ground_mat(i0, j0, spread > 6.0))
                stats["coarse"] += 1
                continue
            for i in range(i0, i0 + NC):
                for j in range(j0, j0 + NC):
                    x0, y0 = xs[j], xs[i]
                    if abs(x0 + CS / 2) < hole and abs(y0 + CS / 2) < hole:
                        continue
                    idx = ((i, j), (i, j + 1), (i + 1, j + 1), (i + 1, j))
                    cell = [(xs[c], xs[r], PHI[r, c], H[r, c], TT[r, c]) for (r, c) in idx]
                    tm = max(p[4] for p in cell)
                    stats["fine"] += 1
                    at(x0 + CS / 2, y0 + CS / 2)
                    b = cur[0]
                    if tm < 0.02:
                        zs = [p[3] for p in cell]
                        emit(b, [(p[0], p[1], p[3]) for p in cell], ground_mat(i, j, max(zs) - min(zs) > 1.6))
                        continue
                    lo_phi, hi_phi = min(p[2] for p in cell), max(p[2] for p in cell)
                    ts = []
                    for k in range(int(math.floor(lo_phi)), int(math.floor(hi_phi)) + 2):
                        for t in (float(k), k + DK):
                            if lo_phi < t < hi_phi:
                                ts.append(t)
                    gx = (cell[1][2] - cell[0][2] + cell[2][2] - cell[3][2]); gy = (cell[3][2] - cell[0][2] + cell[2][2] - cell[1][2])
                    rest, pieces = cell, []
                    for t in ts:
                        lo, hi, cut = clip(rest, 2, t)
                        if len(lo) >= 3:
                            pieces.append(lo)
                        if len(cut) == 2:
                            k = int(math.floor(t + 1e-9))
                            isw = abs(t - k) < 1e-9                 # a wall (level line) or the dike's inner edge
                            p, q = cut
                            if isw:
                                upright(p, q, zb_of(p, k - 1, 0.0), zb_of(q, k - 1, 0.0), zb_of(p, k, 0.3), zb_of(q, k, 0.3),
                                        "py_wall", (-gx, -gy))
                            else:
                                upright(p, q, zb_of(p, k, 0.0), zb_of(q, k, 0.0), zb_of(p, k, 0.3), zb_of(q, k, 0.3), "py_dike", (gx, gy))
                        rest = hi
                    if len(rest) >= 3:
                        pieces.append(rest)
                    for pc in pieces:
                        mphi = sum(p[2] for p in pc) / len(pc)
                        k = int(math.floor(mphi))
                        mt = sum(p[4] for p in pc) / len(pc)
                        if mphi - k < DK:                          # the lip dike: a grassy bund on a stone wall
                            emit(b, [(p[0], p[1], zb_of(p, k, 0.3)) for p in pc], "py_dike" if (mt > 0.5 and (k * 5 + int((pc[0][0] - pc[0][1]) / 17.0)) % 4 == 0) else "py_grass")
                            continue
                        if mt <= 0.5:                              # the last thin paddies, gone to grass
                            emit(b, [(p[0], p[1], zb_of(p, k, 0.0)) for p in pc], "py_grass")
                            continue
                        # fields: cut the paddy by cross dikes that run up and down the slope
                        mx, my = sum(p[0] for p in pc) / len(pc), sum(p[1] for p in pc) / len(pc)
                        th0 = math.atan2(my, mx)
                        rk = 41.0 + (6.5 * k if k > 0 else 9.0 * abs(k)) + 20.0
                        dth = 14.0 / rk
                        ph = 2.3 * math.sin(k * 1.7)
                        pc6 = []
                        for p in pc:
                            d = (math.atan2(p[1], p[0]) - th0 + math.pi) % math.tau - math.pi
                            pc6.append(p + ((th0 + d) / dth + ph,))
                        lo_s, hi_s = min(p[5] for p in pc6), max(p[5] for p in pc6)
                        w = 0.8 / 14.0
                        ts2 = []
                        for m_ in range(int(math.floor(lo_s)), int(math.floor(hi_s)) + 2):
                            for t in (float(m_), m_ + w):
                                if lo_s < t < hi_s:
                                    ts2.append(t)
                        tang = (-my, mx)
                        rest2, subs = pc6, []
                        for t in ts2:
                            lo, hi, cut = clip(rest2, 5, t)
                            if len(lo) >= 3:
                                subs.append(lo)
                            if len(cut) == 2:
                                p, q = cut
                                m_ = int(math.floor(t + 1e-9))
                                sgn = -1.0 if abs(t - m_) < 1e-9 else 1.0
                                upright(p, q, zb_of(p, k, 0.0), zb_of(q, k, 0.0), zb_of(p, k, 0.3), zb_of(q, k, 0.3), "py_dike", (tang[0] * sgn, tang[1] * sgn))
                            rest2 = hi
                        if len(rest2) >= 3:
                            subs.append(rest2)
                        for sp in subs:
                            ms = sum(p[5] for p in sp) / len(sp)
                            m_ = int(math.floor(ms))
                            if ms - m_ < w:
                                emit(b, [(p[0], p[1], zb_of(p, k, 0.3)) for p in sp], "py_dike")
                                continue
                            hsh = abs(math.sin(k * 12.9898 + m_ * 78.233) * 43758.5453) % 1.0
                            wet = 0.58 if k < 0 else 0.44
                            if hsh < wet:
                                emit(wsheet, [(p[0], p[1], zb_of(p, k, 0.12)) for p in sp], "py_water")
                            elif hsh < wet + 0.34:
                                emit(b, [(p[0], p[1], zb_of(p, k, 0.2)) for p in sp], "py_rice")
                            elif hsh < wet + 0.46:
                                emit(b, [(p[0], p[1], zb_of(p, k, 0.2)) for p in sp], "py_rice_ripe")
                            else:
                                emit(b, [(p[0], p[1], zb_of(p, k, 0.03)) for p in sp], "py_mud")
                            stats["paddies"] += 1
    print("[art] mountain cells: %d fine, %d coarse; %d paddy pieces; %d chunks" % (stats["fine"], stats["coarse"], stats["paddies"], len(chunks)))
    for key in sorted(chunks):
        static(chunks[key], mode="land")
    static(wsheet, mode="plain")


def far_world():
    """Beyond the near ground: the mountain's own slopes (coarse, forested), five ranges across the valley as five
    separate pieces at their own distances (real depth), and the sea of cloud lying between."""
    b = Buf("payo_slopes", seed=91)
    xs = np.arange(-80, 81) * 17.5                               # 17.5 m = 10 fine cells: the near ground's edge (350) is a line of this grid
    X, Y = np.meshgrid(xs, xs)
    Hh = P.field(X, Y)["h"]
    Hh = np.maximum(Hh, P.VALLEY_FLOOR - 20.0)
    nI = len(xs) - 1
    for i in range(nI):
        for j in range(nI):
            cx, cy = (xs[j] + xs[j + 1]) / 2, (xs[i] + xs[i + 1]) / 2
            if abs(cx) < 350 - 1 and abs(cy) < 350 - 1:
                continue
            zc = [Hh[i, j], Hh[i, j + 1], Hh[i + 1, j + 1], Hh[i + 1, j]]
            if max(zc) < P.CLOUD_Y - 25:
                continue
            b.quad([(xs[j], xs[i], zc[0]), (xs[j + 1], xs[i], zc[1]), (xs[j + 1], xs[i + 1], zc[2]), (xs[j], xs[i + 1], zc[3])],
                   "py_rock" if max(zc) - min(zc) > 26 else "py_far_slope")
    n, tries = 0, 0
    while n < 1500 and tries < 40000:                            # the forest on those slopes: big crowns
        tries += 1
        x, y = rng.uniform(-1400, 1400), rng.uniform(-1400, 1400)
        if abs(x) < 352 and abs(y) < 352:
            continue
        z = float(max(P.field([x], [y])["h"][0], P.VALLEY_FLOOR))
        if z < P.CLOUD_Y - 6:
            continue
        s = rng.uniform(7, 14) * (1.0 + math.hypot(x, y) / 1300.0)
        P.ellipsoid(b, T(x, y, z + s * 0.3), s, s * rng.uniform(0.85, 1.1), s * rng.uniform(0.5, 0.75), rng.choice(("py_canopy_a", "py_canopy_b", "py_canopy_c")), n=6, rings=3)
        n += 1
    static(b, sharp=80.0, mode="land")
    for i, (si, wi, ai, mat) in enumerate(P.RANGES):             # each range: a strip mesh in its own frame (q across, s down the valley)
        r = Buf("payo_range_%d" % (i + 1), seed=95 + i)
        qs = np.linspace(-3600, 3600, 97 if i else 145)
        ss = np.linspace(si - 2.3 * wi, si + 0.5 * wi, 13 if i else 37)
        Q, S_ = np.meshgrid(qs, ss)
        Z = P.range_height(i, Q, S_)
        if i == 0:                                               # the nearest range is terraced too: real steps, 13 m levels
            lv = (Z - P.VALLEY_FLOOR) / 13.0
            kk = np.floor(lv)
            patch = P.nz(Q / 260.0, S_ / 200.0, 8) > -0.2
            Z = np.where(patch, P.VALLEY_FLOOR + 13.0 * (kk + np.clip((lv - kk - 0.72) / 0.28, 0, 1)), Z)
        Xw = -S_ * P.UPX + Q * (-P.UPY)
        Yw = -S_ * P.UPY + Q * P.UPX
        for a in range(len(ss) - 1):
            for c in range(len(qs) - 1):
                zc = [Z[a, c], Z[a, c + 1], Z[a + 1, c + 1], Z[a + 1, c]]
                if max(zc) < P.CLOUD_Y - 30:
                    continue
                m = mat
                if i == 0 and max(zc) - min(zc) < 2.5 and min(zc) > P.CLOUD_Y:
                    m = "py_far_1b"
                r.quad([(Xw[a, c], Yw[a, c], zc[0]), (Xw[a, c + 1], Yw[a, c + 1], zc[1]), (Xw[a + 1, c + 1], Yw[a + 1, c + 1], zc[2]), (Xw[a + 1, c], Yw[a + 1, c], zc[3])], m)
        static(r, mode="land")
    c = Buf("payo_clouds", seed=99)
    for k in range(48):
        a0, a1 = math.tau * k / 48, math.tau * (k + 1) / 48
        c.quad([(0, 0, P.CLOUD_Y), (3400 * math.cos(a0), 3400 * math.sin(a0), P.CLOUD_Y), (3400 * math.cos(a1), 3400 * math.sin(a1), P.CLOUD_Y)], "py_cloud")
    n, tries = 0, 0
    while n < 170 and tries < 6000:
        tries += 1
        a, rr = rng.uniform(0, 6.28), rng.uniform(140, 2600)
        x, y = rr * math.cos(a), rr * math.sin(a)
        if float(P.field([x], [y])["h"][0]) > P.CLOUD_Y - 8.0:
            continue
        s = rng.uniform(30, 80) * (1 + rr / 1100.0)
        P.ellipsoid(c, T(x, y, P.CLOUD_Y + rng.uniform(-3, 4)), s, s * rng.uniform(0.6, 1.0), s * rng.uniform(0.16, 0.3), "py_cloud", n=8, rings=4)
        n += 1
    static(c, sharp=80.0, mode="plain")


def crown_tree(b, x, y, z, s):
    """A chunky tree: a leaning trunk and three or four fat crowns in two greens."""
    lean = rng.uniform(-0.25, 0.25) * s
    b.tube([(x, y, z - 0.3), (x + lean * 0.5, y + lean * 0.3, z + s * 0.9), (x + lean, y + lean * 0.5, z + s * 1.6)], s * 0.15, "py_trunk", n=6, r1=s * 0.09)
    for k in range(rng.randint(3, 4)):
        a, o, r = rng.uniform(0, 6.28), s * rng.uniform(0.3, 0.7), s * rng.uniform(0.6, 0.9)
        P.ellipsoid(b, T(x + lean + o * math.cos(a), y + o * math.sin(a), z + s * (1.7 + 0.35 * k)), r, r, r * 0.6, rng.choice(("py_canopy_a", "py_canopy_b", "py_canopy_c")), n=7, rings=4)


def pick(kind):
    return rng.choice(PLANTS[kind]) if PLANTS.get(kind) else None


def plant(kind, x, y, z, scale=1.0):
    m = pick(kind)
    if m:
        put(m, x, y, z, rng.uniform(0, 6.28), scale, group="plant")


def forest():
    """Plants. Near the stage and on the terraces: Lagoon Cove's palms, banana, taro, monstera, croton, bushes and grass,
    Kanto's broad trees, and pines on the high ground (leaf-card models, placed). Behind them the forest's mass is one
    merged piece of soft crowns in three greens."""
    b = Buf("payo_forest", seed=101)
    Pn = np.array([[rng.uniform(-346, 346), rng.uniform(-346, 346)] for _ in range(30000)])
    F = P.field(Pn[:, 0], Pn[:, 1])
    Z = P.surface(F)
    ok = (F["T"] < 0.3) & (F["e"] > 3.0) & (F["rock"] < 0.25) & ~F["gate"] & ~F["shelf"] & (Z > P.CLOUD_Y - 14)
    count = {"tree": 0, "pine": 0, "palm": 0, "banana": 0, "crown": 0}
    for (x, y), z, good, t, h in zip(Pn.tolist(), Z.tolist(), ok.tolist(), F["T"].tolist(), F["h"].tolist()):
        if not good:
            continue
        d = math.hypot(x, y)
        k = rng.random()
        if d < 190 and h > 26 and count["pine"] < 70 and k < 0.5:
            count["pine"] += 1
            plant("pine", x, y, z - 0.3, rng.uniform(0.8, 1.3))
        elif d < 175 and count["tree"] < 150 and k < 0.3:
            count["tree"] += 1
            plant("bigtree" if (d < 70 and count["tree"] % 9 == 0) else "tree", x, y, z - 0.2, rng.uniform(0.9, 1.5))
        elif d < 150 and count["palm"] < 40 and k < 0.36:
            count["palm"] += 1
            plant("palm", x, y, z - 0.1, rng.uniform(0.9, 1.4))
        elif d < 120 and count["banana"] < 34 and k < 0.42:
            count["banana"] += 1
            plant("banana", x, y, z, rng.uniform(1.0, 1.6))
        elif count["crown"] < 1900 and (d > 70 or k > 0.7):
            count["crown"] += 1
            sz = rng.uniform(3.0, 6.4)
            P.ellipsoid(b, T(x, y, z + sz * 0.35), sz, sz * rng.uniform(0.85, 1.1), sz * rng.uniform(0.5, 0.78), rng.choice(("py_canopy_a", "py_canopy_b", "py_canopy_c")), n=7, rings=4)
    # where the terraces thin out: boulders, last trees, bushes
    Pn = np.array([[rng.uniform(-300, 300), rng.uniform(-300, 300)] for _ in range(9000)])
    F = P.field(Pn[:, 0], Pn[:, 1])
    Z = P.surface(F)
    edge = (F["T"] > 0.12) & (F["T"] < 0.6) & (F["e"] > 3)
    k = 0
    for (x, y), z, good in zip(Pn.tolist(), Z.tolist(), edge.tolist()):
        if not good or k > 300:
            continue
        k += 1
        if k % 6 == 0:
            rk = rng.choice(list(K.BOULDERS) + list(K.COBBLES))
            sc_ = rng.uniform(1.2, 3.4) / 2.5
            b.rock(rk, T(x, y, z - 0.3 * sc_, rng.uniform(0, 6.28)) @ Matrix.Diagonal((sc_, sc_, sc_ * 0.8, 1)), mat="py_rock")
        elif k % 6 == 1:
            plant("tree", x, y, z - 0.2, rng.uniform(0.7, 1.1))
        elif k % 6 in (2, 3):
            plant("bush", x, y, z, rng.uniform(1.0, 1.8))
        elif k % 6 == 4:
            plant("banana", x, y, z, rng.uniform(1.0, 1.5))
        else:
            plant("ground", x, y, z, rng.uniform(1.2, 2.0))
    # palms, banana and grass on the bunds near the stage
    Pn = np.array([[rng.uniform(-150, 150), rng.uniform(-150, 190)] for _ in range(12000)])
    F = P.field(Pn[:, 0], Pn[:, 1])
    Z = P.surface(F)
    f = F["phi"] - np.floor(F["phi"])
    lipm = (F["T"] > 0.85) & (f > 0.02) & (f < P.DIKE - 0.03) & (F["e"] > 2)
    k = 0
    for (x, y), z, good, e in zip(Pn.tolist(), Z.tolist(), lipm.tolist(), F["e"].tolist()):
        if not good or k > 190:
            continue
        k += 1
        kind = ("tuft", "tuft", "ground", "palm", "banana", "tuft", "bush", "tuft")[k % 8]
        if kind == "palm" and e > 80:
            kind = "tuft"
        plant(kind, x, y, z - 0.03, rng.uniform(1.0, 1.6) if kind != "palm" else rng.uniform(0.8, 1.2))
    # round the plaza, just outside its paving: ferns, taro, croton, bushes, so the stone meets green
    for k in range(96):
        a = math.tau * k / 96 + rng.uniform(-0.02, 0.02)
        for rr in (42.5, 45.5):
            x, y = (rr + rng.uniform(-1, 1)) * math.cos(a) * 1.02, (rr + rng.uniform(-1, 1)) * math.sin(a) * 1.02
            Pr = P.probe(x, y)
            if Pr["e"] <= 0.3 or Pr["gate"] or Pr["shelf"] or Pr["rock"] > 0.2 or (abs(x) < 9 and y < 0):
                continue
            plant(("ground", "bush", "banana", "tuft", "ground", "tuft")[(k + int(rr)) % 6], x, y, Pr["z"] - 0.02, rng.uniform(1.0, 1.7))
    # rocks along the gorge and the cliff, ferns between them
    for _ in range(44):
        x, y = rng.uniform(-70, 70), rng.uniform(37, 100)
        Pr = P.probe(x, y)
        if Pr["rock"] < 0.4 or Pr["z"] < P.CLOUD_Y - 10:
            continue
        rk = rng.choice(list(K.BOULDERS) + list(K.STACKS) + list(K.SLABS))
        sc_ = rng.uniform(3.0, 8.0) / 2.5
        b.rock(rk, T(x, y, Pr["z"] - 0.5 * sc_, rng.uniform(0, 6.28)) @ Matrix.Diagonal((sc_, sc_, sc_, 1)), mat="py_rock")
        plant("ground", x + rng.uniform(-3, 3), y + rng.uniform(-3, 3), P.gz(x, y) + 0.5, rng.uniform(1.4, 2.4))
    print("[art] forest:", count, "and", sum(1 for pl in PLACE if pl["group"] == "plant"), "plants placed")
    static(b, sharp=80.0, mode="land")


# =============================================================== water with body
def fall(b, x, y, yaw, ztop, zbot, w, spread=1.0):
    """A waterfall: a thick bowed sheet in three leaves, brighter ropes of water down it, a rounded lip, foam at the
    foot. Local +Y points away from the wall it falls down. UV v is metres down the fall."""
    M = T(x, y, 0, yaw)
    h = ztop - zbot
    r = random.Random(int(x * 13 + y * 7 + ztop * 3))
    for (hw, d0, d1) in ((w * 0.5, -0.05, 0.34), (w * 0.36, 0.3, 0.5), (w * 0.2, 0.46, 0.6)):
        b.block(M, hw, (d1 - d0) / 2, zbot, ztop, "py_fall", taper=1.0)
        for i in range(len(b.v) - 8, len(b.v)):
            v = M.inverted() @ Vector(b.v[i])
            b.v[i] = tuple(M @ Vector((v.x * (spread if v.z < zbot + 0.01 else 1.0), v.y + (d0 + d1) / 2, v.z)))
    for k in range(max(2, int(w / 1.0))):
        cx = -w / 2 + (k + 0.5) * w / max(2, int(w / 1.0)) + r.uniform(-0.2, 0.2)
        z1 = ztop - r.uniform(0, h * 0.2)
        b.block(M @ T(cx, 0.62, 0), r.uniform(0.12, 0.26), 0.05, max(zbot, z1 - r.uniform(h * 0.35, h * 0.9)), z1, "py_foam")
    P.ellipsoid(b, M @ T(0, 0.2, ztop), w * 0.52, 0.55, 0.3, "py_fall", n=8, rings=4)
    for k in range(max(3, int(w * 1.1))):
        s = r.uniform(0.35, 0.8) * min(1.7, 0.6 + h / 14)
        P.ellipsoid(b, M @ T(r.uniform(-w * spread / 2, w * spread / 2), r.uniform(0.2, 1.3), zbot + s * 0.25), s, s, s * 0.55, "py_foam", n=7, rings=3)


def pit_fall(b, c, s, width, r_from):
    h = width / 2
    if abs(c) >= abs(s):
        sg = 1 if c > 0 else -1
        y = s / abs(c) * LIP
        b.box(min(sg * (LIP + 0.8), sg * r_from * abs(c)), max(sg * (LIP + 0.8), sg * r_from * abs(c)), y - h, y + h, PLAZA_Y - 0.3, PLAZA_Y + 0.05, "py_block_dark", top="py_fall")
        fall(b, sg * (LIP - 0.3), y, -sg * math.pi / 2 + math.pi, PLAZA_Y + 0.3, LEDGE_Y, width)
        fall(b, sg * (PIT - 0.1), y, -sg * math.pi / 2 + math.pi, LEDGE_Y + 0.05, FLOOR - 3, width * 0.9)
    else:
        sg = 1 if s > 0 else -1
        x = c / abs(s) * LIP
        b.box(x - h, x + h, min(sg * (LIP + 0.8), sg * r_from * abs(s)), max(sg * (LIP + 0.8), sg * r_from * abs(s)), PLAZA_Y - 0.3, PLAZA_Y + 0.05, "py_block_dark", top="py_fall")
        fall(b, x, sg * (LIP - 0.3), 0.0 if sg < 0 else math.pi, PLAZA_Y + 0.3, LEDGE_Y, width)
        fall(b, x, sg * (PIT - 0.1), 0.0 if sg < 0 else math.pi, LEDGE_Y + 0.05, FLOOR - 3, width * 0.9)


def waters():
    b = Buf("payo_falls", seed=111)

    def stream(x, y, width=3.0, to_pit=True, steps=420):
        z0 = P.gz(x, y)
        for _ in range(steps):
            d = P.downhill(x, y)
            nx, ny = x + d.x * 0.7, y + d.y * 0.7
            Pr = P.probe(nx, ny)
            if Pr["e"] <= 0.0:
                if to_pit:
                    r = math.hypot(nx, ny)
                    pit_fall(b, nx / r, ny / r, width, r)
                return
            if z0 - Pr["z"] > 1.0:
                fall(b, (x + nx) / 2, (y + ny) / 2, math.atan2(d.y, d.x) - math.pi / 2, z0 + 0.2, Pr["z"] + 0.05, width)
            if Pr["z"] < P.CLOUD_Y - 8:
                return
            x, y, z0 = nx, ny, Pr["z"]

    for a in (math.radians(197.0), math.radians(252.0), math.radians(292.0)):          # three channels cross the plaza and fall into the pit
        pit_fall(b, math.cos(a), math.sin(a), 2.4, 41.0)
    n = 7
    while n < 7:                                                # (paddies overflowing through notches: tried, and from the court they read as pale boxes)
        x, y = rng.uniform(-130, 130), rng.uniform(-135, 20)
        Pr = P.probe(x, y)
        if Pr["T"] > 0.9 and Pr["c"] > 0.2 and 0.3 < Pr["f"] < 0.6:
            stream(x, y, rng.uniform(1.1, 1.7), to_pit=False, steps=10)
            n += 1
    for sx in (-1, 1):                                          # out by the north: spouts in the broken wall, the brink either side
        fall(b, sx * 12.6, 33.0, sx * math.pi / 2, 0.9, BREACH_Y, 2.4)
        fall(b, sx * 37.0, 37.0, 0.0, PLAZA_Y - 0.05, P.gz(sx * 37.0, 62.0) - 6.0, 2.6, spread=2.4)
        for s_ in (58.0, 48.0):                                  # the gate's falls, down its platform's two steps
            pass
    fall(b, 0.0, LIP + 0.95, 0.0, BREACH_Y + 0.1, P.CLOUD_Y - 26.0, 9.0, spread=2.4)
    for (x, y, yaw) in SPOUTS:                                  # the carved heads in the pit's walls pour
        d = Vector((math.sin(yaw), -math.cos(yaw), 0))
        fall(b, x + d.x * 1.3, y + d.y * 1.3, yaw + math.pi, -12.9, FLOOR - 3.0, 1.1)
    for (x, y, z, sc_) in ((0.0, 42.0, P.gz(0.0, 46.0) + 1.0, 4.0), (-37.0, 40.0, P.gz(-37.0, 44.0) + 1.0, 2.4), (37.0, 40.0, P.gz(37.0, 44.0) + 1.0, 2.4),
                           (16.0, -46.6, PLAZA_Y, 1.4), (-16.0, -46.6, PLAZA_Y, 1.4), (16.0, -56.8, 0.0, 1.2), (-16.0, -56.8, 0.0, 1.2)):
        put("prop_mist_puff", x, y, z, rng.uniform(0, 6.28), sc_, group="static")
    b.box(-5.0, 5.0, PIT + 0.02, LIP + 1.0, BREACH_Y - 0.4, BREACH_Y + 0.08, "py_block_dark", top="py_fall")
    for sx in (-1, 1):
        fall(b, sx * 16.0, -98.25, 0.0, 17.5, 6.0, 3.6)
        fall(b, sx * 16.0, -57.6, 0.0, 6.05, 0.0, 3.6)
        fall(b, sx * 16.0, -47.6, 0.0, 0.05, PLAZA_Y, 3.6)
    static(b, mode="plain")


# =============================================================== the sky piece
def mayari():
    """MAYARI, variant A: three flat cut-out layers 16 m apart (hair and its stars; the far arm, head, moon and gold;
    the near arm), each a tile of the py_mayari atlas, hung beyond the last range over the valley."""
    b = Buf("payo_sky_mayari", seed=121)
    dist, az, size, zc = 2900.0, math.radians(80.0), 2000.0, 870.0
    for k, (u0, v0) in enumerate(((0.0, 0.5), (0.5, 0.5), (0.0, 0.0))):
        d = dist - k * 16.0
        c = Vector((d * math.cos(az), d * math.sin(az), zc))
        side = Vector((math.sin(az), -math.cos(az), 0)) * (size / 2)
        up = Vector((0, 0, size / 2))
        b.quad([tuple(c - side - up), tuple(c + side - up), tuple(c + side + up), tuple(c - side + up)], "py_mayari",
               uvs=[(u0, v0), (u0 + 0.5, v0), (u0 + 0.5, v0 + 0.5), (u0, v0 + 0.5)])
    static(b)
    return {"bearing_from_north_deg": 10.0, "distance": dist, "size": size, "centre_height": zc}


# =============================================================== borrowed plants
PLANTS = {}
PLANT_SOURCES = {
    "palm": ("LagoonCove", ("plant_palm_0", "plant_palm_2", "plant_palm_4")),
    "banana": ("LagoonCove", ("plant_broadleaf_0", "plant_broadleaf_1", "plant_broadleaf_3")),
    "bush": ("LagoonCove", ("plant_bush_0", "plant_bush_2", "plant_croton_1")),
    "ground": ("LagoonCove", ("plant_monstera_2", "plant_groundcover_0", "plant_groundcover_3")),
    "tuft": ("LagoonCove", ("plant_tuft_0", "plant_tuft_1", "plant_tuft_3")),
    "tree": ("Kanto", ("tree_broad_lime", "street_round_deep", "street_broad", "street_young")),
}


def py_material_for(old):
    """Lagoon's and Kanto's material names, as Payo's."""
    o = old.lower()
    dark = "dark" in o
    if "trunk" in o and "palm" in o or "coconut" in o:
        return "py_palm_trunk"
    if "frond_broad" in o:
        return "py_frond_dark" if dark else "py_frond_light"
    if "frond" in o:
        return "py_frond_small"
    if "paddle" in o or "monstera" in o or "croton" in o:
        return "py_paddle_dark" if dark else "py_paddle_light"
    if "flower" in o:
        return "py_flower_red"
    if "blade" in o:
        return "py_grass_dark" if dark else "py_grass_light"
    if "stem" in o or "stalk" in o:
        return "py_banana_stem"
    if "core" in o:
        return "py_tree_core"
    if "trunk" in o or "bark" in o or "branch" in o:
        return "py_tree_trunk"
    if "leaf" in o:
        return ("py_tree_leaf_dark" if dark else "py_tree_leaf_light") if "tree" in o or "kanto" in o or o.startswith("leaf") else ("py_leaf_dark" if dark else "py_leaf_light")
    return None


def borrow_plants():
    for kind, (folder, names) in PLANT_SOURCES.items():
        PLANTS[kind] = []
        for nm in names:
            f = ROOT / "Assets/TumbangPreso/Art" / folder / "Models" / (nm + ".glb")
            if not f.exists():
                continue
            before = set(bpy.data.objects)
            bpy.ops.import_scene.gltf(filepath=str(f))
            new = [o for o in bpy.data.objects if o not in before]
            meshes = [o for o in new if o.type == "MESH"]
            if not meshes:
                continue
            bpy.ops.object.select_all(action="DESELECT")
            for o in meshes:
                o.select_set(True)
            bpy.context.view_layer.objects.active = meshes[0]
            if len(meshes) > 1:
                bpy.ops.object.join()
            o = bpy.context.view_layer.objects.active
            bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
            bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
            unknown = []
            for sl in o.material_slots:
                old = sl.material.name.split(".")[0]
                py = py_material_for(old)
                if py is None:
                    unknown.append(old)
                    py = "py_tree_leaf_light"
                sl.material = P.material(py)
            if unknown:
                print("[art] plant materials guessed as leaf:", nm, unknown)
            for c in list(o.users_collection):
                c.objects.unlink(o)
            kitc.objects.link(o)
            o.name = "payo_" + nm
            o.data.name = "payo_" + nm
            P.bake(o, None, "plain")
            KIT[o.name] = o
            PLANTS[kind].append(o.name)
            for x in new:
                try:
                    if x != o and x.name in bpy.data.objects:
                        bpy.data.objects.remove(x)
                except ReferenceError:
                    pass
    PLANTS["bigtree"] = PLANTS["tree"]
    PLANTS["tree"] = ["tree_fig_a", "tree_fig_b", "tree_fig_c"]
    PLANTS["bush"] = [n for n in PLANTS["bush"] if "croton" not in n]
    PLANTS["pine"] = ["tree_pine_a", "tree_pine_b"]
    print("[art] plants borrowed:", {k: len(v) for k, v in PLANTS.items()}, {n: K.tris_of(KIT[n]) for v in PLANTS.values() for n in v if n in KIT})


def ground_z(x, y):
    Pr = P.probe(x, y)
    return PLAZA_Y if Pr["e"] <= 0.0 else Pr["z"]


def D(model, x, y, yaw_deg=0.0, scale=1.0, z=None, group="static"):
    put(model, x, y, ground_z(x, y) if z is None else z, math.radians(yaw_deg), scale, group=group)


def stair_between(x, y):
    """Stone steps up the wall nearest downhill of (x, y)."""
    z0 = P.gz(x, y)
    for _ in range(30):
        d = P.downhill(x, y)
        nx, ny = x + d.x * 0.5, y + d.y * 0.5
        z1 = P.gz(nx, ny)
        if z0 - z1 > 1.6:
            put("prop_stone_stair", nx + d.x * 2.6, ny + d.y * 2.6, z1, math.atan2(-d.y, -d.x) - math.pi / 2, 1.0, group="static")
            return True
        x, y, z0 = nx, ny, z1
    return False


def dress():
    """THE VILLAGE'S THINGS. Density target from the study: 6 to 10 props a 10 m square on the forecourt and the rims,
    something in every direction a player on the court can look, the court's own floor clear."""
    Pz = PLAZA_Y
    # ---- the gate's forecourt, either side of the stair's lane
    for sx in (-1, 1):
        D("prop_offering_table", sx * 9.6, -40.6, 180)
        D("prop_spear_rack", sx * 8.6, -38.2, 180 - sx * 20)
        D("prop_jars", sx * 12.0, -41.4 + sx * 0.8, sx * 40)
        D("prop_baskets", sx * 7.9, -42.8, sx * 70)
        D("prop_drum", sx * 12.6, -37.6, 0)
        D("prop_drum", sx * 13.6, -38.0, 0, 0.8)
        D("prop_lantern", sx * 7.7, -36.4, 90 + sx * 90)
        D("prop_lantern", sx * 24.0, -37.6, 90 + sx * 90)
        for k, x in enumerate((13.0, 19.0, 25.0)):
            D(("prop_banner_red", "prop_banner_black", "prop_banner_white")[(k + (sx > 0)) % 3], sx * x, -46.6, 180)
        D("prop_jars", sx * 24.0, -50.5, sx * 20, z=0.0)
        D("prop_baskets", sx * 12.5, -50.0, sx * 60, z=0.0)
        D("prop_spear_rack", sx * 9.2, -59.6, 180, z=6.0)
        D("prop_drum", sx * 4.0, -67.5, 0, 1.2, z=6.0)
        D("prop_banner_red" if sx < 0 else "prop_banner_white", sx * 21.0, -59.2, 180, z=6.0)
    D("prop_gong_rack", -15.6, -43.6, 160)
    D("prop_hagabi", 15.4, -44.4, 12)
    D("prop_mortar", -19.2, -40.4, 0)
    D("prop_cook_fire", 22.4, -41.2, 0)
    D("prop_firewood", 24.8, -42.6, 30)
    D("prop_coop", -23.6, -44.4, 0)
    D("prop_rice_rack", -28.2, -41.0, 90)
    D("prop_fallen_block", -25.0, -36.8, 0)
    D("prop_rubble", 27.2, -36.4, 0)
    D("prop_offering_table", 0.0, -70.5, 180, 1.3, z=6.0)
    D("prop_gong_rack", -5.4, -66.4, 200, z=6.0)
    D("prop_hagabi", 6.0, -67.4, 0, z=6.0)
    NIGHT_LIGHTS.append((22.4, -41.2, Pz + 0.8, 900.0, 0.4))
    put("fire_flame", 22.4, -41.2, Pz + 0.1, 0.0, 0.7, group="night")
    # ---- the plaza's west side: standing stones, a slab gate, rubble
    D("prop_leaning_slab", -37.4, -20.0, 30)
    D("prop_leaning_slab", -38.2, 8.4, 200, 1.2)
    D("slab_gate", -38.4, -4.0, 90)
    D("prop_banner_black", -36.6, 14.0, 90)
    D("prop_banner_red", -36.4, -12.0, 90)
    D("prop_rubble", -37.4, 24.0, 40)
    D("prop_fallen_block", -36.8, -27.4, 120)
    D("prop_lantern", -35.4, 17.0, 0)
    D("prop_lantern", -35.4, -16.0, 0)
    D("prop_jars", -36.2, 2.4, 0)
    D("prop_baskets", -36.6, -8.8, 0)
    D("prop_offering_table", -36.9, 20.0, -90)
    # ---- the plaza's south corners and the south east
    for (m, x, y, yw) in (("prop_rice_rack", 36.6, -24.0, 90), ("prop_leaning_slab", 37.2, -30.0, 60), ("prop_baskets", 35.8, -19.0, 0), ("prop_rubble", -30.5, -35.5, 0),
                          ("prop_fallen_block", 31.0, -36.2, 60), ("prop_jars", 36.4, -15.6, 0), ("prop_lantern", 35.2, -27.0, 180), ("prop_banner_white", 36.4, -21.4, -90),
                          ("prop_rubble", -36.6, 31.0, 0), ("prop_fallen_block", 36.4, 31.6, 30), ("prop_rubble", 35.6, 34.4, 0)):
        D(m, x, y, yw)
    # ---- the water shelf
    for (m, x, y, yw) in (("prop_scarecrow", 58.0, 6.0, 250), ("prop_scarecrow", 76.0, -8.0, 280), ("prop_scarecrow", 90.0, 18.0, 270), ("watch_hut", 66.0, 20.0, 270),
                          ("prop_bamboo_pipe", 39.4, -9.6, 90), ("prop_rice_rack", 50.6, -13.6, 0), ("prop_lantern", 40.0, 10.0, 180), ("prop_baskets", 51.4, 9.4, 0)):
        lo, hi = P.shelf_span(x)
        D(m, x, max(lo + 0.5, min(hi - 0.5, y)), yw, z=P.shelf_z(x) + 0.3)
    # ---- the terraces: stairs between the paddies, field things, single shrines and granaries of every kind
    made = sum(1 for (x, y, z) in paddy_points(26, 120.0, uphill=True) if stair_between(x, y))
    pts = paddy_points(40, 135.0)
    kinds = ["prop_scarecrow"] * 8 + ["prop_rice_rack"] * 5 + ["watch_hut"] * 4 + ["prop_bamboo_pipe"] * 4 + ["prop_lantern"] * 9 + \
            ["shrine_tower_ochre", "shrine_tower_turq", "shrine_step_coral", "shrine_step_mint", "granary_drum_rose", "granary_drum_ochre", "slab_gate", "house_long_coral", "house_bale_mint", "prop_leaning_slab"]
    for (x, y, z), m in zip(pts, kinds):
        d = P.downhill(x, y)
        put(m, x, y, z, math.atan2(-d.y, -d.x) - math.pi / 2 + rng.uniform(-0.3, 0.3), 0.9 if m.startswith("shrine") or m.startswith("granary") else 1.0, group="static")
    za, zb = P.gz(-41.5, -8.0), P.gz(-59.0, -8.0)
    put("prop_rope_bridge", -50.2, -8.0, max(za, zb) + 0.3, 0.0, 1.0, group="static")
    # ---- LIFE: models for the game to move. The crowd sits on the gate's stair and stands along the pit's rim.
    for sx in (-1, 1):
        for k in (0, 2, 4):
            sdist = 37.87 - 0.8 + 20.0 * k / 6 + 1.7
            life("villager_sit", sx * 6.36, -sdist, Pz + 12.0 * (k + 1) / 6 + 0.9, math.pi, "sits on the stair's side wall watching the game; claps and stands up when the can falls")
        life("villager_watch", sx * 14.0, -35.4, Pz, math.pi + sx * 0.3, "stands at the pit's rim shading the eyes; points at a player in the air")
        life("villager_stand", sx * 20.0, -35.6, Pz, math.pi - sx * 0.2, "stands with arms folded; turns the head after the slipper")
        life("child_wave", sx * 17.0, -35.2, Pz, math.pi, "waves and bounces; runs a few steps along the rim and back")
        life("chicken" if sx > 0 else "chicken_white", sx * 10.5, -43.6, Pz, 1.0, "pecks about the offering table; scatters from a loud hit")
        life("chicken", sx * 21.0, -44.0, Pz, 2.0, "pecks; follows the other hen")
    life("dog", -17.0, -38.6, Pz, 2.6, "lies by the mortar; trots to the rim and barks when the can falls")
    life("dog", 20.6, -39.6, Pz, 0.4, "sits by the cooking fire; scratches")
    life("villager_carry", 18.0, -46.9, Pz, -math.pi / 2, "walks the forecourt with a basket on the head, slowly, to the granary and back")
    life("villager_stand", -37.0, -1.0, Pz, -math.pi / 2, "stands in the slab gate watching the stage")
    life("villager_watch", -36.2, 11.0, Pz, -math.pi / 2, "watches; cheers")
    for (kind, x, y, yw, do) in (("villager_plant", 44.0, 3.0, 1.2, "bends planting rice, straightens, steps on"), ("villager_plant", 46.5, 8.0, 0.4, "bends planting rice"),
                                 ("villager_plant", 56.0, -3.0, 2.0, "bends planting rice"), ("villager_carry", 62.4, 12.0, -1.57, "walks the dike with a basket"),
                                 ("carabao_wallow", 70.0, 4.0, 0.6, "lies in the paddy, flicks an ear, snorts"), ("carabao_stand", 80.0, 14.0, 2.2, "stands chewing; swings the tail"),
                                 ("egret", 48.0, -6.0, 0.3, "stalks, strikes at the water, flies off if a player lands near"), ("egret", 60.0, 16.0, 2.4, "stands on one leg"),
                                 ("egret", 74.0, 2.0, 4.0, "rides near the carabao"), ("egret", 86.0, 10.0, 1.0, "stalks")):
        lo, hi = P.shelf_span(x)
        y = max(lo + 1.0, min(hi - 1.0, y))
        life(kind, x, y, P.shelf_z(x) + (0.3 if kind == "villager_carry" else 0.12), yw, do)
    for i, (x, y, z) in enumerate(paddy_points(12, 110.0)):
        kind = ("villager_plant", "carabao_stand", "egret", "villager_carry", "egret", "villager_plant")[i % 6]
        life(kind, x, y, z, rng.uniform(0, 6.28), {"villager_plant": "bends planting rice", "carabao_stand": "stands chewing", "egret": "stalks the paddy", "villager_carry": "walks the bund with a basket"}[kind])
    life("hawk", 60.0, 150.0, 46.0, 1.0, "circles over the valley on a 60 m ring, wings still, tilting")
    life("hawk", -90.0, 110.0, 70.0, 4.0, "circles higher and slower")
    print("[art] dressing: %d stairs; %d placed pieces, %d life" % (made, sum(1 for pl in PLACE if pl["group"] == "static"), sum(1 for pl in PLACE if pl["group"] == "life")))


def paddy_points(n, rmax=150.0, uphill=None):
    """n random spots standing in terraced paddies, clear of the gate's court."""
    out, tries = [], 0
    while len(out) < n and tries < 20000:
        tries += 1
        x, y = rng.uniform(-rmax, rmax), rng.uniform(-rmax, rmax)
        Pr = P.probe(x, y)
        if Pr["T"] > 0.9 and 0.3 < Pr["f"] < 0.7 and Pr["e"] > 5 and not Pr["gate"] and (uphill is None or (Pr["c"] > 0.2) == uphill) and all(math.hypot(x - q[0], y - q[1]) > 9 for q in out):
            out.append((x, y, Pr["z"]))
    return out


# =============================================================== build
plan, stairs, movers = blocks()
for o_ in list(KIT.values()):
    pass
bulul_piece()
village_kit()
borrow_plants()
place_layout(LAYOUTS[LAYOUT], stairs, movers)
pit()
shelf()
gate()
for sx in (-1, 1):                                              # the two guardians by the gate's stair stand on the plaza, offerings at their feet
    put("bulul", sx * 36.6, -36.6, PLAZA_Y + 1.4, yaw=math.atan2(1, -sx) + math.pi / 2, scale=1.0, group="static")
    put("prop_offering_table", sx * 33.6, -34.2, PLAZA_Y, math.atan2(1, -sx) + math.pi / 2, 1.0, group="static")
    put("prop_jars", sx * 35.0, -33.4, PLAZA_Y, 0.4, 1.0, group="static")
# fire: the court's corners and the towers (on whatever stands there in this layout), the brink, the wall's stubs
TOPS = {p["id"]: p["top"] for p in LAYOUTS[LAYOUT]["pieces"]}
STAGE_FIRES = [("cornerNE", 10.3, 10.3), ("cornerNW", -10.3, 10.3), ("cornerSE", 10.3, -10.3), ("cornerSW", -10.3, -10.3),
               ("towerNE", 21.2, 21.2), ("towerNW", -21.2, 21.2), ("towerSE", 21.2, -21.2), ("towerSW", -21.2, -21.2)]
for (pid, x, y) in STAGE_FIRES:
    if pid in TOPS:
        fire(x, y, TOPS[pid], 1.0, 700.0)
        PLACE[-1]["on_block"] = pid; PLACE[-2]["on_block"] = pid
for sx in (-1, 1):
    for t in (-22, 0, 22):
        if not (sx > 0 and -14 < t < 30):
            fire(sx * 34.6, t, PLAZA_Y + 0.28, 1.5, 1500.0)
        if sx > 0:
            fire(t, -34.6, PLAZA_Y + 0.28, 1.5, 1500.0)
    fire(sx * 26.2, 32.8, 9.52, 1.3, 1400.0)
if not QUICK:
    for (x, y) in ((-52, -66), (-88, -30), (58, -78), (-70, 12), (-30, -74), (34, -66), (-64, -44), (70, -40), (-100, 6)):
        d = P.downhill(x, y)
        best = None
        for k in range(-16, 17):
            px, py = x + d.x * k * 0.5, y + d.y * k * 0.5
            Pr = P.probe(px, py)
            score = abs(Pr["f"] - 0.5) + (0 if Pr["T"] > 0.7 else 5)
            if best is None or score < best[0]:
                best = (score, px, py, Pr["z"])
        d = P.downhill(best[1], best[2])
        hamlet(best[1], best[2], best[3], math.atan2(-d.y, -d.x) - math.pi / 2)
hamlet_stones()
sky = mayari()
if not QUICK:
    mountain()
    forest()
    dress()
    waters()
    far_world()
else:
    dress()

for name, o in list(STATIC.items()) + list(KIT.items()):
    TRIS[name] = K.tris_of(o)
TRIS["catch_bird"] = sum(K.tris_of(o) for o in CATCH["objects"])

# =============================================================== the review's scene: kit pieces set where they go
revc = bpy.data.collections.new("review"); sc.collection.children.link(revc)
INST = []
for pl in PLACE:
    src = KIT.get(pl["model"]) or KIT.get("payo_" + pl["model"])
    if src is None:
        print("[art] placement of a model that was not built:", pl["model"]); continue
    o = src.copy()
    revc.objects.link(o)
    o.matrix_world = T(*pl["p"], pl["yaw"]) @ Matrix.Scale(pl["scale"], 4)
    o["group"] = pl["group"]
    INST.append((o, pl))
for i, src in enumerate(CATCH["objects"]):                       # the catch bird, shown once in the review, hovering over the north blocks
    o = src.copy()
    revc.objects.link(o)
    base = T(6.0, 27.0, 9.0, math.radians(200.0))
    o.parent = None
    o.matrix_world = base @ (Matrix.Translation(src.location) if i else Matrix.Identity(4)) @ (Matrix.Rotation((-1 if i == 1 else 1) * 0.35, 4, "Y") if i else Matrix.Identity(4))
    INST.append((o, {"model": "catch_bird", "group": "review", "p": (6.0, 27.0, 9.0), "yaw": 0.0, "scale": 1.0}))
bpy.context.view_layer.update()                                  # the checks read matrix_world
kitc.hide_render = True

# =============================================================== checks
blk_objs = {n: o for n, o in KIT.items() if n.startswith("block_") or n.startswith("mover_")}
cover = CHK.cover(LAYOUTS, KIT, movers)
cop = CHK.coplanar([STATIC[n] for n in ("payo_pit", "payo_shelf", "payo_shelf_water", "payo_gate", "payo_hamlets") if n in STATIC] + [o for (o, pl) in INST if pl["group"] == "block"])
total_tris = sum(TRIS[n] for n in STATIC) + sum(TRIS.get(pl["model"], TRIS.get("payo_" + pl["model"], 0)) for pl in PLACE) + TRIS["catch_bird"]
print("[art] COVER:", cover["text"])
for row in cover["fail"][:20]:
    print("        ", row)
print("[art] COPLANAR:", cop["text"])
for row in cop["summary"][:30]:
    print("        ", row)
for row in cop["pairs"][:40]:
    print("        ", row)
mats_used = sorted({s.material.name for o in list(STATIC.values()) + list(KIT.values()) + CATCH["objects"] for s in o.material_slots if s.material})
print("[art] triangles: %d in the scene as placed for layout %d; %d materials" % (total_tris, LAYOUT, len(mats_used)))
print("[art] materials:", " ".join(mats_used))
by_group = {}
for pl in PLACE:
    by_group[pl["group"]] = by_group.get(pl["group"], 0) + TRIS.get(pl["model"], TRIS.get("payo_" + pl["model"], 0))
print("[art] triangles by group:", by_group, "static", sum(TRIS[n] for n in STATIC))
for n in sorted(TRIS, key=lambda k: -TRIS[k])[:18]:
    print("        %8d  %s" % (TRIS[n], n))

SOURCE.mkdir(parents=True, exist_ok=True)

# =============================================================== review pictures (before the meshes are turned for export)
if REVIEW:
    import author_payo_art_review as REV
    REV.render(sc, P, LOGS, REVIEW, ONLY, LAYOUTS[LAYOUT], NIGHT_LIGHTS, INST, STATIC)

# =============================================================== export
if EXPORT:
    MODELS.mkdir(parents=True, exist_ok=True)
    TURN = Matrix.Rotation(math.pi, 4, "Z")
    for o, pl in INST:
        o.hide_set(True)

    def export(o, name):
        bpy.ops.object.select_all(action="DESELECT")
        o.hide_set(False); o.hide_viewport = False
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        kw = dict(filepath=str(MODELS / (name + ".glb")), export_format="GLB", use_selection=True, export_apply=True, export_yup=True,
                  export_image_format="NONE", export_materials="EXPORT")
        for extra in (o.children if name == "catch_bird" else ()):
            extra.hide_set(False); extra.select_set(True)
        try:
            bpy.ops.export_scene.gltf(export_vertex_color="ACTIVE", export_all_vertex_colors=False, **kw)
        except TypeError:
            bpy.ops.export_scene.gltf(**kw)

    done = set()
    for name, o in list(STATIC.items()) + list(KIT.items()):
        if o.data.name not in done:
            o.data.transform(TURN)
            done.add(o.data.name)
        export(o, name)
    for o in CATCH["objects"]:                                   # body, wing_L, wing_R: one glb, three nodes
        if o.data.name not in done:
            o.data.transform(TURN)
            done.add(o.data.name)
        if o.parent is not None:
            o.location = (-o.location.x, -o.location.y, o.location.z)
    bpy.context.view_layer.update()
    export(CATCH["objects"][0], "catch_bird")

    def U(p):
        return [round(p[0], 4), round(p[2], 4), round(p[1], 4)]

    rows = []
    for name in STATIC:
        rows.append({"model": name, "position": [0.0, 0.0, 0.0], "yaw": 0.0, "scale": 1.0, "group": "static"})
    life_rows = []
    for pl in PLACE:
        if pl["group"] == "life":
            life_rows.append({"kind": pl["model"], "model": pl["model"], "position": U(pl["p"]), "yaw": round(-math.degrees(pl["yaw"]), 2), "do": pl.get("do", "")})
            continue
        if pl["group"] == "block":
            continue                                             # the stage's pieces are placed by the game, from the layout
        model = pl["model"] if pl["model"] in KIT else "payo_" + pl["model"]
        row = {"model": model, "position": U(pl["p"]), "yaw": round(-math.degrees(pl["yaw"]), 3), "scale": round(pl["scale"], 4), "group": pl["group"]}
        if "on_block" in pl:
            row["on_block"] = pl["on_block"]
        rows.append(row)
    block_rows = [{"id": pid, "model": "block_" + pid, "x": x, "z": z, "sx": sx, "sz": sz} for pid, (x, z, sx, sz) in plan.items()]
    for pid, (d, drop) in stairs.items():
        next(r for r in block_rows if r["id"] == pid)["stair"] = {"model": "block_%s_stair" % pid, "down": d.replace("y", "z"), "drop": drop}
    art = {
        "note": "Written by tools/author_payo_art.py. Unity axes. Every glb is already turned so that a model placed at a Unity position with a Unity yaw "
                "(degrees, clockwise from above) stands as drawn; static models go at the origin with no rotation.",
        "layout_read": str(LAY_PATH.relative_to(ROOT)).replace("\\", "/"),
        "placements": rows,
        "blocks": block_rows,
        "blocks_note": "ONE MODEL PER BLOCK ID. A model's origin is the centre of its block's COLLISION TOP and its own top is 1 cm under that, so place it at "
                       "(x, top, z) with no rotation and move it with the block. Its shaft is 50 m long. Where a layout makes a block a stair, draw the block's "
                       "`stair.model` instead: a ramp from the origin's height down `drop` metres toward `down`, 1 cm under a ramp collider of the same ends.",
        "movers": {mid: {"model": model} for mid, model in movers.items()},
        "movers_note": "Origin at the centre of the slab's collision top, top 1 cm under; place at the mover's current (x, y, z).",
        "pads": {"jumpPads": "pad_geyser", "speedPads": "pad_speed", "pickups": "pickup_plinth"},
        "pads_note": "Origin on the floor at the layout's (x, y, z). pad_speed points along local +Z: yaw 0 for along +z, 180 for -z, 90 for +x, -90 for -x. "
                     "pad_geyser is 8.5 cm high at its kerb and pickup_plinth 36 cm: neither has a collider of its own.",
        "life": life_rows,
        "life_note": "MODELS FOR THE GAME TO MOVE, each standing where it should begin, in the cast's manner (box heads, block bodies; all colour is vertex colour on py_flat "
                     "and py_weave). They are rigid single meshes with no bones: sway, bob, slide or swap them as the other maps' ambient life does. `do` says what each should do. "
                     "They are NOT in `placements`. Kinds: villager_stand, villager_watch, villager_carry, villager_plant, villager_sit, child_wave, carabao_stand, carabao_wallow, "
                     "chicken, chicken_white, dog, egret, hawk.",
        "catch": CATCH["info"],
        "vertex_colour": "EVERY MODEL CARRIES VERTEX COLOUR (COLOR_0) AND EVERY py_ MATERIAL EXCEPT THE LEAF CARDS, py_mayari AND py_spill MUST MULTIPLY BY IT "
                         "(LagoonPainted's 'Multiply by vertex colour'). It holds the worn pale tops and damp dark bases of the stone, stone-to-stone shifts, the hillside's "
                         "colour drift, every building's own paint, and all cloth, skin, hide and feather colour. Without it the map is one flat tone again.",
        "decals": "py_decal is an alpha-cut atlas (lichen, moss, drip stains, cracks, worn paint, leaf litter) on quads 4 to 8 mm off the stone, including on the blocks' tops "
                  "(6 mm under the collision top). Draw it cutout with a small depth offset and no shadow casting.",
        "chunks": "The near ground is payo_mountain_00 .. payo_mountain_22: nine pieces of a 3 x 3 grid, 233 m each, for culling.",
        "hide_solids": bool(cover["ok"]),
        "cover_check": cover["text"],
        "coplanar_check": cop["text"],
        "night": {
            "note": "The second half. Show the group 'night' placements (fire_flame over every fire_bowl) and give each a warm point light; light py_fire and "
                    "py_window (emissive); show payo_sky_mayari; tint py_cloud and the fog to moonlit blue; stars, shooting stars and fireflies are effects for the game "
                    "to draw. By day hide group 'night' and payo_sky_mayari and draw py_window dark.",
            "glow_materials": ["py_fire", "py_window", "py_mayari"],
            "lights": [{"position": U((x, y, z)), "watts_blender": power, "radius": rad} for (x, y, z, power, rad) in NIGHT_LIGHTS],
            "on_block_note": "A placement with `on_block` stands on that block's top in layout %d (%s) and must ride it, or be hidden when the block is sunk." % (LAYOUT, LAYOUTS[LAYOUT]["name"]),
        },
        "sky": dict(sky, model="payo_sky_mayari", material="py_mayari", texture="mayari_albedo.png",
                    note="Variant A (peeking; the moon is the eye she lost) as three cut-out layers 16 m apart, tiles of one 2048 atlas: top left hair and stars, "
                         "top right the figure, bottom left the near arm. It stands 2.9 km out over the valley, beyond the last range: the far clip must reach 3.6 km, or "
                         "scale the model about the can (the origin) and it subtends the same. Draw it unlit, alpha blended, without fog, BEHIND the far ranges, "
                         "so the last range covers where her arms fade. Variants B and C are mayari_b_albedo.png and mayari_c_albedo.png, whole pictures for one quad."),
        "far_note": "payo_range_1..5 are five separate pieces at 0.6, 1.0, 1.4, 2.0 and 2.7 km, all in the forest floor's material (py_ground; the nearest's terrace "
                    "steps in py_grass): the game's distance fog must pale them one after another, as the review pictures' haze does. payo_clouds is the sea of cloud at "
                    "y -50 out to 3.4 km. They are the draft's ranges brought to 0.56 of their distance and size.",
        "water": "payo_paddy_water.glb and payo_shelf_water.glb: flat sheets in py_water, one polygon set per flooded paddy, 12 cm over the paddy's floor and 18 cm under "
                 "its dikes. Nothing else lies in their planes. payo_falls.glb is every waterfall, in py_fall and py_foam, UV v in metres down the fall.",
        "sun": {"day_from_bearing_deg": 50.0, "day_elevation_deg": 31.0, "night_moon_from_bearing_deg": 13.0, "night_moon_elevation_deg": 30.0},
        "look": "DAY (first half): a clear highland morning, the sun out over the valley to the north east at about 30 degrees, neutral to slightly warm key so the stone "
                "stays GREY, pale blue distance fog from about 120 m that takes the ranges one after another, cloud lying in the valley 50 m under the court. "
                "NIGHT (from half time): moonlight from the valley side, deep blue fog, warm fire light on the stage and the gate, Mayari over the far ridge.",
        "triangles": {"as_placed_for_layout": total_tris, "by_model": {n: TRIS[n] for n in sorted(TRIS)}},
        "notes": [
            "The pit's floor is not modelled: shafts and walls run to y -50 and the game's mist hides the rest.",
            "payo_mountain.glb has a square hole 66.5 m across for the pit; payo_pit.glb's kerb and upper wall close it.",
            "The gate's falls and the streams are static meshes on py_fall (UV v runs down the fall in metres: scroll it); prop_mist_puff stands at the foot of the great falls.",
            "pad_geyser is a carved stone bowl with standing water (py_water) and a turquoise rim: the jet and the steam are the game's.",
            "fire_flame is layered tongues (py_fire, py_fire_core), a night placement over every fire_bowl and the cooking fire; prop_lantern's panels and every door are py_window.",
        ],
    }
    (ART / "payo_art.json").write_text(json.dumps(art, indent=1), encoding="utf-8")
    mats = []
    for n in mats_used:
        tex, tint, tiling, flags = P.MAT.get(n, ("", (1, 1, 1), 1.0, ""))
        row = {"name": n, "texture": tex, "tint": [round(c, 4) for c in tint], "tiling": tiling, "foliage": "f" in flags, "emissive": "e" in flags,
               "glossy": "g" in flags, "cutout": "c" in flags}
        if n in P.NOTE:
            row["shader_note"] = P.NOTE[n]
        mats.append(row)
    (ART / "materials_payo.json").write_text(json.dumps({"tint_space": "sRGB 0..1, multiplies the texture",
                                                         "note": "Textures are Assets/TumbangPreso/Art/Payo/Textures/<texture>_albedo.png (and _normal.png where it exists). "
                                                                 "UVs are metres: tiling is 1 / the tile in metres.", "materials": mats}, indent=1), encoding="utf-8")
    print("[art] exported %d models, %d placements, %d materials; hide_solids %s" % (len(STATIC) + len(KIT), len(rows), len(mats), cover["ok"]))
print("[art] done")
