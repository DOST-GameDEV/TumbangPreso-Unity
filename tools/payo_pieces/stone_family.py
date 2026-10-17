"""PAYO's stage stone family after the hero monolith (2026-10-12): the core block and the head tower.

Imported by tools/payo_pieces/stone_build.py, which holds the shared painters (stone_wall, carve_band, motif, face, root).
Each function here types ONE piece. A block's top is exactly the layout's rectangle, flat, at y 0 (the origin is its
centre); everything below the top is its own.
"""
import math
import random

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter

import stone_kit as K
from stone_kit import (shade, Piece, ball, box, lid, pipe, relief, soft, stones, turned, weather,
                       BARK, CORAL, DARK, INK, LEAF, MOSS, OCHRE, PALE, RED, TURQ)


def paved_top(S, seed=5, extra=None, course=1.5, length=(1.3, 2.1)):
    """A walked top: big slabs, a pale worn rim, a cut groove inside it, corner diamonds, moss creeping in. `extra`
    draws the block's own carving on it."""
    def fn(img, info):
        r2 = random.Random(seed)
        stones(img, info["ppm"], r2, course=course, length=length, tone=1.08)
        w, h = img.size
        m = info["margin"]
        weather(img, info["ppm"], r2, moss=0.0, drips=0.0, damp=0.0, lichen=0.7)
        if extra:
            extra(img, info, r2)
        ring = Image.new("L", (w, h), 0)
        d = ImageDraw.Draw(ring)
        d.rectangle([0, 0, w, h], fill=255)
        d.rectangle([m + 9, m + 9, w - m - 9, h - m - 9], fill=0)
        soft(img, ring.filter(ImageFilter.GaussianBlur(2.5)), PALE, 0.9)
        g = Image.new("L", (w, h), 0)
        o = m + 0.62 * info["ppm"]
        ImageDraw.Draw(g).rectangle([o, o, w - o, h - o], outline=255, width=max(3, int(info["ppm"] * 0.08)))
        if K.CALM:                                        # the groove as a soft shallow cut (as the monolith's calm top)
            soft(img, g.filter(ImageFilter.GaussianBlur(2.0)), shade(K.calm_tone((150, 148, 144)), 0.86), 0.8)
            K.hset(img, g.filter(ImageFilter.GaussianBlur(2.0)), -0.03)
        else:
            soft(img, g.filter(ImageFilter.GaussianBlur(0.8)), INK, 0.8)
        soft(img, ImageChops.offset(g, -2, -2).filter(ImageFilter.GaussianBlur(0.8)), PALE, 0.45)
        for (cx, cy) in ((o, o), (w - o, o), (w - o, h - o), (o, h - o)):
            mk = Image.new("L", (w, h), 0)
            S.motif(ImageDraw.Draw(mk), "diamond", cx, cy, info["ppm"] * 0.36)
            relief(img, mk, paint=OCHRE, rnd=r2, depth=2)
        for _ in (range(int((info["W"] + info["D"]) * 0.8)) if not K.CALM else ()):       # calm: no moss creeping in
            side = r2.randrange(4)
            t = r2.uniform(0.1, 0.9)
            x, y = ((t * w, m + 4), (w - m - 4, t * h), (t * w, h - m - 4), (m + 4, t * h))[side]
            mk = Image.new("L", (w, h), 0)
            dm = ImageDraw.Draw(mk)
            for k in range(4):
                rr = r2.uniform(5, 13)
                ox, oy = r2.uniform(-14, 14), r2.uniform(-8, 8)
                dm.ellipse([x + ox - rr, y + oy - rr, x + ox + rr, y + oy + rr], fill=255)
            soft(img, mk.filter(ImageFilter.GaussianBlur(1.2)), MOSS)
        return img
    return fn


def calm_medallion(S, img, info):
    """The court's medallion, calm: a smooth pale centre for the can, two soft cut rings, a broad turquoise inlay ring,
    sixteen flat ochre rays, four bold carvings at the quarters. Relief in the height map, no ink, no worn holes."""
    w, h = img.size
    ppm = info["ppm"]
    cx, cy = w / 2, h / 2
    base = K.calm_tone((150, 148, 144))
    plain = Image.new("L", (w, h), 0)
    ImageDraw.Draw(plain).ellipse([cx - 2.0 * ppm, cy - 2.0 * ppm, cx + 2.0 * ppm, cy + 2.0 * ppm], fill=255)
    soft(img, plain.filter(ImageFilter.GaussianBlur(ppm * 0.25)), K.mix3(PALE, base, 0.25), 0.85)        # worn smooth where the can stands
    for (rr, wd) in ((2.1, 0.12), (4.55, 0.12)):
        g = Image.new("L", (w, h), 0)
        ImageDraw.Draw(g).ellipse([cx - rr * ppm, cy - rr * ppm, cx + rr * ppm, cy + rr * ppm], outline=255, width=max(3, int(wd * ppm)))
        gb = g.filter(ImageFilter.GaussianBlur(2.0))
        soft(img, gb, shade(base, 0.86), 0.8)
        K.hset(img, gb, -0.025)
    g = Image.new("L", (w, h), 0)                       # the inlay: one broad ring
    ImageDraw.Draw(g).ellipse([cx - 3.95 * ppm, cy - 3.95 * ppm, cx + 3.95 * ppm, cy + 3.95 * ppm], outline=255, width=max(4, int(0.2 * ppm)))
    soft(img, g.filter(ImageFilter.GaussianBlur(1.0)), TURQ)
    K.hset(img, g.filter(ImageFilter.GaussianBlur(1.5)), 0.006)
    m = Image.new("L", (w, h), 0)
    dm = ImageDraw.Draw(m)
    for k in range(16):
        a = math.tau * (k + 0.5) / 16
        dm.polygon([(cx + 2.3 * ppm * math.cos(a - 0.13), cy + 2.3 * ppm * math.sin(a - 0.13)), (cx + 3.6 * ppm * math.cos(a), cy + 3.6 * ppm * math.sin(a)),
                    (cx + 2.3 * ppm * math.cos(a + 0.13), cy + 2.3 * ppm * math.sin(a + 0.13))], fill=255)
    relief(img, m, paint=OCHRE, depth=3)
    for k, (kind, paint) in enumerate((("ancestor", RED), ("lizard", OCHRE), ("ancestor", CORAL), ("rice", OCHRE))):
        a = math.tau * (k + 0.5) / 4                      # on the diagonals, in the corners outside the rings
        m = Image.new("L", (w, h), 0)
        S.calm_motif(ImageDraw.Draw(m), kind, cx + 5.3 * ppm * math.cos(a), cy + 5.3 * ppm * math.sin(a), 0.44 * ppm)
        relief(img, m, paint=paint, depth=3)


def core_lower(S):
    wall = S.stone_wall(seed=36, damp=0.6, moss=0.7, course=0.8, length=(1.2, 2.2))
    if not K.CALM:
        return S.big_motif(wall, {"front": ("sun", OCHRE), "right": ("lizard", RED), "back": ("spiral", TURQ), "left": ("rice", OCHRE)})
    field, ink = S.CALM_PAIRS[3]
    return S.panels(wall, {"front": ("sun", field, ink, 0.5), "right": ("lizard", field, ink, 0.5), "back": ("spiral", field, ink, 0.5), "left": ("rice", field, ink, 0.5)})


def head_lower(S):
    wall = S.stone_wall(seed=56, damp=0.6, moss=0.7, drips=0.8, course=0.85, length=(1.3, 2.3), tone=0.94)
    if not K.CALM:
        return S.big_motif(wall, {"back": ("sun", OCHRE)})
    field, ink = S.CALM_PAIRS[1]
    return S.panels(wall, {"back": ("sun", field, ink, 0.5), "left": ("fret", field, ink, 0.5), "right": ("eye", field, ink, 0.5)})


def core(S):
    """The stage's centre block, 10 x 10 m, the can at its middle. Its top carries the court's medallion: a plain worn
    centre for the can and its chalk ring, a ring of sun rays, a line of turquoise inlay, four carvings at the
    quarters. Below: a guardian face on every side, a round buttress at each corner, three tiers. 13.7 m modelled."""
    P = Piece("core", size=2048, ppm=50, seed=21)
    rnd = P.rnd

    def medallion(img, info, r2):
        if K.CALM:
            return calm_medallion(S, img, info)
        w, h = img.size
        ppm = info["ppm"]
        cx, cy = w / 2, h / 2
        plain = Image.new("L", (w, h), 0)
        ImageDraw.Draw(plain).ellipse([cx - 1.9 * ppm, cy - 1.9 * ppm, cx + 1.9 * ppm, cy + 1.9 * ppm], fill=255)
        soft(img, plain.filter(ImageFilter.GaussianBlur(ppm * 0.2)), (202, 198, 190), 0.7)            # worn smooth where the can stands
        for (rr, wd) in ((2.1, 0.12), (3.75, 0.1), (4.55, 0.1)):
            g = Image.new("L", (w, h), 0)
            ImageDraw.Draw(g).ellipse([cx - rr * ppm, cy - rr * ppm, cx + rr * ppm, cy + rr * ppm], outline=255, width=max(3, int(wd * ppm)))
            soft(img, g.filter(ImageFilter.GaussianBlur(0.8)), INK, 0.85)
            soft(img, ImageChops.offset(g, -2, -2).filter(ImageFilter.GaussianBlur(0.8)), PALE, 0.5)
        g = Image.new("L", (w, h), 0)
        ImageDraw.Draw(g).ellipse([cx - 3.92 * ppm, cy - 3.92 * ppm, cx + 3.92 * ppm, cy + 3.92 * ppm], outline=255, width=max(3, int(0.09 * ppm)))
        soft(img, g, TURQ)
        m = Image.new("L", (w, h), 0)
        dm = ImageDraw.Draw(m)
        for k in range(16):
            a = math.tau * (k + 0.5) / 16
            dm.polygon([(cx + 2.3 * ppm * math.cos(a - 0.13), cy + 2.3 * ppm * math.sin(a - 0.13)), (cx + 3.55 * ppm * math.cos(a), cy + 3.55 * ppm * math.sin(a)),
                        (cx + 2.3 * ppm * math.cos(a + 0.13), cy + 2.3 * ppm * math.sin(a + 0.13))], fill=255)
        relief(img, m, paint=OCHRE, rnd=r2, depth=2, worn=0.6)
        for k, (kind, paint) in enumerate((("ancestor", RED), ("lizard", OCHRE), ("ancestor", CORAL), ("rice", OCHRE))):
            a = math.tau * k / 4
            m = Image.new("L", (w, h), 0)
            S.motif(ImageDraw.Draw(m), kind, cx + 4.22 * ppm * math.cos(a), cy + 4.22 * ppm * math.sin(a), 0.3 * ppm)
            relief(img, m, paint=paint, rnd=r2, depth=2)

    def drum_paint(q):
        def fn(img, info):
            stones(img, info["ppm"], random.Random(40 + q), course=0.7, length=(0.8, 1.4), tone=1.05)
            weather(img, info["ppm"], random.Random(q), moss=0.5, drips=0.6, damp=0.5)
            return img
        return fn

    lid(P, 5.0, 5.0, 0.0, 0.3, paved_top(S, seed=31, extra=medallion, course=1.7, length=(1.5, 2.4)), ppm=72)
    box(P, (5.0, 0.46, 5.0), 0.2, (0, -0.52, 0), paint=S.stone_wall(course=0.46, length=(1.3, 2.2), tone=1.14, moss=0.5, drips=0.2, damp=0.0, seed=32))
    box(P, (4.7, 0.22, 4.7), 0.06, (0, -1.1, 0), sw=DARK)
    box(P, (4.84, 0.66, 4.84), 0.12, (0, -1.86, 0), paint=S.carve_band(seed=33), ppm=50)
    box(P, (4.93, 0.17, 4.93), 0.09, (0, -2.6, 0), paint=S.worn_paint(CORAL, seed=34))
    box(P, (4.5, 2.1, 4.5), 0.36, (0, -4.75, 0), paint=S.stone_wall(seed=35, moss=0.4, course=0.8, length=(1.2, 2.2)), taper=(0.93, 1.0), bulge=0.03)
    for k, yaw in enumerate((0, 90, 180, 270)):
        a = math.radians(yaw)
        S.face(P, 4.46 * math.sin(a), -4.7, 4.46 * math.cos(a), s=1.25, yaw=yaw, fins=(CORAL, TURQ, CORAL, OCHRE)[k], crown=((OCHRE, TURQ), (RED, OCHRE), (OCHRE, TURQ), (TURQ, CORAL))[k])
    for (sx, sz) in ((1, 1), (-1, 1), (-1, -1), (1, -1)):                              # a round buttress at each corner, inside the footprint
        turned(P, [(0.62, -11.2), (0.74, -7.0), (0.86, -3.3), (0.95, -3.05), (0.95, -2.8)], (sx * 4.02, 0, sz * 4.02), seg=12, paint=drum_paint(sx * 3 + sz))
        turned(P, [(0.99, -3.2), (1.02, -3.05), (0.99, -2.9)], (sx * 4.02, 0, sz * 4.02), sw=TURQ, seg=12)
    box(P, (4.56, 0.18, 4.56), 0.1, (0, -6.95, 0), sw=PALE)
    box(P, (4.1, 2.2, 4.1), 0.36, (0, -9.25, 0), paint=core_lower(S),
        taper=(0.9, 1.0), turn=(0, 4, 0))
    box(P, (3.8, 0.18, 3.8), 0.1, (0, -11.5, 0), sw=PALE, turn=(0, 4, 0))
    box(P, (3.5, 1.1, 3.5), 0.34, (0, -12.6, 0), paint=S.stone_wall(seed=37, damp=0.8, moss=0.8, drips=0.9, tone=0.88, course=0.8, length=(1.2, 2.2)), taper=(1.08, 1.0), turn=(0, -3, 0))
    for (x, z, col, drop) in ((4.72, 2.9, BARK, 9.0), (4.74, -3.3, LEAF, 5.5), (-2.6, -4.74, BARK, 7.5), (-4.72, 1.4, LEAF, 6.0), (3.4, 4.74, LEAF, 4.0), (-4.74, -3.6, BARK, 10.0)):
        S.root(P, rnd, (x, -0.95, z), drop, r=rnd.uniform(0.1, 0.16), colour=col, wander=0.6)
    for (x, y, z, sc) in ((3.9, -6.82, 3.2, 0.7), (-3.4, -6.82, -4.0, 0.6), (-4.0, -6.82, 3.0, 0.5), (3.2, -11.36, -3.3, 0.7)):
        S.moss_pad(P, x, y, z, sc)
    S.fern(P, rnd, 4.2, -6.75, -3.6, 1.0, n=6)
    S.fern(P, rnd, -3.5, -11.3, 3.4, 1.1, n=6)
    P.meta.update({"size": "top 10.0 x 10.0 m exactly, 13.7 m modelled", "pivot": "the centre of the top (y 0): the can stands here", "ground": -13.8, "ink": 0.04, "width": 10.4,
                   "eye_z": 1.7, "look_z": -4.0, "distance": 19.0, "figure": (2.2, -1.8), "figure_z": 0.0, "fov": 62,
                   "close": [(0.0, 0.0, 0.5), (4.5, 7.5, 7.0)]})
    return P


def tower_head(S):
    """A corner tower whose shaft is a giant carved head. Top: a flat 8 x 8 m square exactly. Under the cap a head 7 m
    across and 6 m tall with a face 4 m wide, great painted ears with turquoise ear discs, a neck, a painted collar of
    beads, and shoulders with the arms folded, as the bulul sits. 13.5 m modelled."""
    P = Piece("tower_head", size=2048, ppm=62, seed=41)
    rnd = P.rnd
    lid(P, 4.0, 4.0, 0.0, 0.3, paved_top(S, seed=51), ppm=70)
    box(P, (4.0, 0.46, 4.0), 0.2, (0, -0.52, 0), paint=S.stone_wall(course=0.46, length=(1.3, 2.2), tone=1.14, moss=0.5, drips=0.2, damp=0.0, seed=52))
    box(P, (3.6, 0.2, 3.6), 0.06, (0, -1.08, 0), sw=DARK)
    box(P, (3.86, 0.36, 3.86), 0.14, (0, -1.56, 0), paint=S.worn_paint(OCHRE, seed=53))               # the headband
    for k in range(7):                                                                              # a crown of leaves on the headband, front and back
        for sz in (1, -1):
            hgt = 0.34 + 0.12 * (3 - abs(k - 3))
            box(P, (0.36, hgt, 0.14), 0.09, (-3.0 + k * 1.0, -2.0 - hgt, sz * 3.84), sw=(TURQ, CORAL)[k % 2], taper=(0.4, 1.0))
    box(P, (3.5, 3.0, 3.5), 0.7, (0, -4.9, 0), paint=S.stone_wall(seed=54, moss=0.5, course=0.85, length=(1.3, 2.3), tone=1.04), taper=(0.9, 1.0), bulge=0.06)
    S.face(P, 0.0, -4.9, 3.46, s=1.9)
    S.face(P, 0.0, -4.7, -3.46, s=1.2, yaw=180, fins=TURQ, crown=(RED, OCHRE))
    for sx in (-1, 1):                                                                              # the ears, and their discs
        box(P, (0.3, 1.5, 0.95), 0.2, (sx * 3.62, -4.8, 0.0), sw=CORAL, taper=(0.7, 1.0))
        turned(P, [(0.0, -0.12), (0.62, -0.12), (0.7, 0.0), (0.62, 0.12), (0.0, 0.12)], (sx * 3.84, -6.5, 0.0), sw=TURQ, turn=(0, 0, 90), seg=12)
        ball(P, (0.2, 0.3, 0.3), (sx * 3.98, -6.5, 0.0), OCHRE, seg=8, rings=6)
    box(P, (2.3, 0.6, 2.3), 0.3, (0, -8.25, 0), sw=DARK)                                              # the neck
    box(P, (3.1, 0.34, 3.1), 0.2, (0, -8.95, 0), paint=S.worn_paint(RED, seed=55))                  # the collar, and its beads
    for i in range(-2, 3):
        for (dx, dz) in ((i * 1.2, 3.12), (i * 1.2, -3.12), (3.12, i * 1.2), (-3.12, i * 1.2)):
            ball(P, (0.24, 0.24, 0.24), (dx, -8.95, dz), TURQ if i % 2 == 0 else OCHRE, seg=8, rings=6)
    box(P, (2.95, 2.2, 2.95), 0.5, (0, -11.3, 0), paint=head_lower(S), taper=(0.84, 1.0))
    for sx in (-1, 1):                                                                              # the arms folded across the chest
        pipe(P, [(sx * 2.7, -9.6, 1.2), (sx * 2.85, -10.6, 2.2), (sx * 1.6, -11.2, 2.95), (-sx * 1.2, -11.0 - sx * 0.25, 3.0)], [0.6, 0.55, 0.5, 0.42], PALE, seg=10)
        ball(P, (0.5, 0.42, 0.4), (-sx * 1.5, -11.0 - sx * 0.25, 3.02), PALE, seg=10, rings=6)
    for (x, z, col, drop) in ((3.74, 2.4, BARK, 9.0), (-3.74, -2.6, LEAF, 6.0), (-1.9, -3.76, BARK, 7.0), (2.6, 3.76, LEAF, 3.4)):
        S.root(P, rnd, (x, -0.95, z), drop, r=rnd.uniform(0.1, 0.15), colour=col, wander=0.6)
    S.moss_pad(P, 2.6, -8.62, 2.2, 0.6)
    S.moss_pad(P, -2.4, -8.62, -2.0, 0.6)
    S.fern(P, rnd, -2.7, -8.55, 2.4, 1.0, n=6)
    P.meta.update({"size": "top 8.0 x 8.0 m exactly, 13.5 m modelled", "pivot": "the centre of the top (y 0)", "ground": -13.6, "ink": 0.04, "width": 8.6,
                   "eye_z": 1.7, "look_z": -4.6, "distance": 16.0, "figure": (2.0, -1.5), "figure_z": 0.0, "fov": 62,
                   "close": [(0.0, -4.9, 3.6), (5.5, -3.2, 12.0)]})
    return P
