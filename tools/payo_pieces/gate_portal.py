"""PAYO: THE GATE PORTAL, and the pieces of it that also stand alone (the platform stair, the side shrine gate).
Built through tools/payo_pieces/gate_build.py (which holds the totem and the carving painters).
"""
import math
import random

from PIL import Image, ImageChops, ImageDraw, ImageFilter

import gate_kit as K
from gate_kit import (block, slab, pipe, turned, ball, leaf, vine, glow_bar, circle, curve, stone, relief, carve, soft, L, shade, mix,
                      PALE, INK, DARK, OCHRE, RED, CORAL, TURQ, CREAM, MOSS, MOSS_L, LEAF, THATCH, TIMBER, BLACK, STONES, SHADOW)
import gate_build as G
from gate_build import G1, G2, G3, G4, WARM, inset, glyph, band_fret, band_tris, soft_panel, fin_paint, wing_paint, totem, Info0

TURQ_DULL = (92, 176, 166)


# ------------------------------------------------------------------ painters
def raw_thatch(img, info):
    if info.get("only_top"):
        K.wood(img, info.top, info.rnd, info.ppm, colour=shade(THATCH, 0.7))
        return
    for s in ("left", "front", "right", "back"):
        K.thatch(img, info[s], info.rnd, info.ppm, bpx=0)


def raw_wood(colour=TIMBER, upright=False):
    def fn(img, info):
        if info.get("only_top"):
            K.wood(img, info.top, info.rnd, info.ppm, colour=colour, bpx=info.bpx)
            return
        for s in ("left", "front", "right", "back"):
            K.wood(img, info[s], info.rnd, info.ppm, colour=colour, bpx=info.bpx, upright=upright)
    return fn


def raw_weave(img, info):
    if info.get("only_top"):
        K.wood(img, info.top, info.rnd, info.ppm, colour=TIMBER)
        return
    for s in ("left", "front", "right", "back"):
        K.weave(img, info[s], info.rnd, info.ppm)


def post_paint(img, info):
    """A carved house post: dark timber, bands of red, cream and black, a zigzag cut round its middle."""
    w, h = img.size
    K.wood(img, (0, 0, w, h), info.rnd, info.ppm, colour=TIMBER)
    d = ImageDraw.Draw(img)
    for (a, b, col) in ((0.08, 0.14, CREAM), (0.14, 0.2, RED), (0.2, 0.23, BLACK), (0.62, 0.66, BLACK), (0.66, 0.74, RED), (0.74, 0.78, CREAM)):
        d.rectangle([0, h * a, w, h * b], fill=col)
    m, md = L(img)
    K.g_zig(md, (0, h * 0.3, w, h * 0.55), 6, max(3, int(info.ppm * 0.07)))
    relief(img, m, mix(TIMBER, CREAM, 0.5), depth=max(2, int(info.ppm * 0.03)), paint=OCHRE)


def cloth_paint(img, info):
    m = info.mask
    soft(img, m, RED)
    d = ImageDraw.Draw(img)
    w, h = img.size
    rows = [(0.06, 0.1, CREAM), (0.1, 0.13, BLACK), (0.3, 0.42, BLACK), (0.33, 0.39, OCHRE), (0.55, 0.58, CREAM), (0.7, 0.8, BLACK), (0.73, 0.77, CREAM)]
    band, bd = L(img)
    for a, b, col in rows:
        band2, b2 = L(img)
        b2.rectangle([0, h * a, w, h * b], fill=255)
        soft(img, ImageChops.multiply(band2, m), col)
    gr = ImageChops.multiply(m, Image.linear_gradient("L").resize(img.size).point(lambda v: int(max(0, v - 100) * 0.7)))
    soft(img, gr, shade(RED, 0.5))
    for k in (range(3) if not K.CALM_NOW else ()):
        x = w * (0.25 + 0.25 * k)
        d.line([(x, h * 0.05), (x + 3, h * 0.9)], fill=shade(RED, 0.72), width=max(2, int(info.ppm * 0.02)))


def paving(img, info):
    pass


def disc_paint(R):
    """The sun disc: a toothed rim, a ring of sixteen rays, a dark field for the ancestor's face."""
    def fn(img, info):
        ppm = info.ppm
        c = info.px(0, 0)
        k = abs(info.px(1, 0)[0] - c[0])
        d = ImageDraw.Draw(img)
        wd = max(4, int(ppm * 0.06))
        field, fd = L(img)                                   # the sunk field between rim and face
        r = R * 0.86 * k
        fd.ellipse([c[0] - r, c[1] - r, c[0] + r, c[1] + r], fill=255)
        r2 = R * 0.6 * k
        carve(img, field, depth=max(3, int(ppm * 0.04)), colour=(92, 88, 86))
        m, md = L(img)
        K.g_rays(md, c[0], c[1], R * 0.56 * k, R * 0.84 * k, 16, wide=0.72, start=math.pi / 16)
        relief(img, m, mix(info.base, PALE, 0.45), depth=max(3, int(ppm * 0.04)), paint=OCHRE, rnd=info.rnd, worn=0.5)
        m, md = L(img)
        K.g_rays(md, c[0], c[1], R * 0.56 * k, R * 0.72 * k, 16, wide=0.5, start=0)
        relief(img, m, mix(info.base, PALE, 0.45), depth=max(2, int(ppm * 0.03)), paint=CORAL, rnd=info.rnd, worn=0.5)
        m, md = L(img)
        md.ellipse([c[0] - r2, c[1] - r2, c[0] + r2, c[1] + r2], fill=255)
        relief(img, m, mix(info.base, PALE, 0.25), depth=max(3, int(ppm * 0.04)))
        m, md = L(img)                                       # the rim's teeth
        n = 40
        for i in range(n):
            a = math.tau * i / n
            ra, rb = R * 0.885 * k, R * 0.955 * k
            da = math.tau / n * 0.3
            md.polygon([(c[0] + ra * math.cos(a - da), c[1] + ra * math.sin(a - da)), (c[0] + rb * math.cos(a - da), c[1] + rb * math.sin(a - da)),
                        (c[0] + rb * math.cos(a + da), c[1] + rb * math.sin(a + da)), (c[0] + ra * math.cos(a + da), c[1] + ra * math.sin(a + da))], fill=255)
        if not K.CALM_NOW:
            carve(img, m, depth=max(2, int(ppm * 0.02)), colour=shade(CORAL, 0.8))
        for _ in range(5):
            a = info.rnd.uniform(0, math.tau)
            K.crack(d, c[0] + R * 0.7 * k * math.cos(a), c[1] + R * 0.7 * k * math.sin(a), ppm * 0.7, info.rnd, ppm)
        sub = img.crop((0, 0, img.size[0], int(c[1] - R * 0.55 * k)))
        K.hang(sub, info.rnd, ppm, 0.25)
        both = Image.composite(sub, img.crop((0, 0, sub.size[0], sub.size[1])), info.mask.crop((0, 0, sub.size[0], sub.size[1])))
        img.paste(both, (0, 0))
    return fn


def ancestor_paint(img, info):
    """The serene face in the middle of the sun: closed eyes, a painted brow band, worn cheek marks, lips."""
    ppm = info.ppm
    px = info.px
    d = ImageDraw.Draw(img)
    wd = max(4, int(ppm * 0.06))
    band, bd = L(img)                                        # the brow band
    bd.polygon([px(-1.4, 1.02), px(1.4, 1.02), px(1.4, 0.62), px(-1.4, 0.62)], fill=255)
    band = ImageChops.multiply(band, info.mask)
    soft(img, band, OCHRE)
    m, md = L(img)
    a, b = px(-1.05, 0.98), px(1.05, 0.68)
    K.g_tris(md, (a[0], a[1], b[0], b[1]), 7, up=False)
    relief(img, ImageChops.multiply(m, info.mask), CREAM, depth=max(2, int(ppm * 0.025)), paint=RED, rnd=info.rnd, worn=0.4)
    d.line([px(-1.3, 0.6), px(1.3, 0.6)], fill=shade(OCHRE, 0.55), width=wd)
    for sx in (-1, 1):                                       # closed eyes: a lid, a dark lash line curving down
        m, md = L(img)
        e0, e1 = px(sx * 0.2, 0.34), px(sx * 0.88, -0.02)
        box = [min(e0[0], e1[0]), min(e0[1], e1[1]), max(e0[0], e1[0]), max(e0[1], e1[1])]
        md.chord(box, 0, 180, fill=255)
        relief(img, m, mix(info.base, PALE, 0.6), depth=max(2, int(ppm * 0.03)))
        d.arc(box, 0, 180, fill=INK, width=int(wd * 1.3))
        for k, col in enumerate((CORAL, TURQ_DULL, CORAL)):  # cheek marks
            m, md = L(img)
            y = -0.3 - k * 0.17
            md.polygon([px(sx * 0.42, y), px(sx * 0.98, y + 0.1), px(sx * 0.98, y + 0.0), px(sx * 0.42, y - 0.1)], fill=255)
            relief(img, ImageChops.multiply(m, info.mask), mix(info.base, PALE, 0.4), depth=max(2, int(ppm * 0.02)), paint=col, rnd=info.rnd, worn=0.5)
    m, md = L(img)                                           # the mouth
    md.polygon([px(-0.42, -0.82), px(-0.2, -0.74), px(0, -0.78), px(0.2, -0.74), px(0.42, -0.82), px(0.2, -0.98), px(-0.2, -0.98)], fill=255)
    relief(img, m, mix(info.base, PALE, 0.5), depth=max(2, int(ppm * 0.03)), paint=RED, rnd=info.rnd, worn=0.3)
    d.line([px(-0.4, -0.84), px(0, -0.87), px(0.4, -0.84)], fill=INK, width=wd, joint="curve")
    K.crack(d, px(0.5, 0.55)[0], px(0.5, 0.55)[1], ppm * 0.6, info.rnd, ppm)


def head_shape(w=1.3, h=1.5):
    """A broad brow, full cheeks, a rounded chin."""
    pts = []
    n = 22
    for i in range(n):
        a = math.tau * i / n
        cx, sy = math.cos(a), math.sin(a)
        r = 1.0
        x = w * (abs(cx) ** 0.75) * (1 if cx >= 0 else -1) * (1.0 if sy > 0 else 0.9 + 0.1 * (1 + sy))
        y = h * (abs(sy) ** 0.8) * (1 if sy >= 0 else -1) * (0.85 if sy > 0 else 1.0)
        pts.append((x, y))
    return pts


# ------------------------------------------------------------------ the stair
def stair(P, z0, y_top, width=6.0, rise=0.25, run=0.5, cheeks=True, posts=True):
    """A flight from the ground up to y_top, its top step's back edge at z0, running out toward +z, between two
    sloped cheek walls with a post at each foot."""
    n = int(round(y_top / rise))

    def riser(img, info):
        x0, y0, x1, y1 = inset(info.front, info.bpx * 1.6)
        m, md = L(img)
        K.g_zig(md, (x0, y0, x1, y1), int(width * 3), max(3, int(info.ppm * 0.045)))
        if K.CALM_NOW:                                      # calm: a carved zigzag felt in the light, not a painted one
            K.hset(img, m.filter(ImageFilter.GaussianBlur(2)), 0.015)
            return
        relief(img, m, mix(info.base, PALE, 0.4), depth=max(2, int(info.ppm * 0.02)), paint=CORAL, rnd=info.rnd, worn=1.2)
    for k in range(n):
        d = (n - k) * run
        block(P, width + (0.5 if cheeks else 0), rise + 0.04, d + 0.2, (0, k * rise, z0 + d / 2 - 0.1), b=0.055, base=(G2, G1, G4, G2, WARM, G1, G4, G2)[k % 8], moss=0.5 if k % 3 == 0 else 0.2, cracks=0.5, foot=0.5,
              top_joints=1.3, top_moss=0.35, top_ppm=P.ppm * 0.7, jit=0.012, paint=riser if k % 2 == 1 else None)
    if not cheeks:
        return
    ln = n * run
    start = len(P.parts)
    prof = [(0, 0), (ln + 0.55, 0), (ln + 0.55, 0.75), (ln * 0.86, 0.95), (ln * 0.86 - 0.5, 1.45), (ln * 0.86 - 0.95, 1.45), (ln * 0.5, 1.75), (ln * 0.5 - 0.45, 2.25), (ln * 0.5 - 0.9, 2.25), (0.9, y_top + 0.45), (0, y_top + 0.45)]

    def cheek(img, info):
        ppm = info.ppm
        px = info.px
        stone_fill(img, info, joints=True)
        m, md = L(img)
        a, b = px(1.0, y_top * 0.55), px(ln * 0.62, 0.35)
        K.g_spiral(md, px(ln * 0.3, y_top * 0.42)[0], px(ln * 0.3, y_top * 0.42)[1], ppm * 0.42, 2.2, max(4, int(ppm * 0.09)))
        relief(img, ImageChops.multiply(m, info.mask), mix(info.base, PALE, 0.4), depth=max(2, int(ppm * 0.03)), paint=CORAL, rnd=info.rnd, worn=0.5)
        sub = img.copy()
        K.creep(sub, (0, 0, img.size[0], img.size[1] - 4), info.rnd, ppm, 0.5)
        img.paste(sub, (0, 0), info.mask)
    slab(P, prof, 1.15, (width / 2 + 1.15 + 0.0, 0, z0 - 0.3), b=0.13, base=G3, paint=cheek, turn=(0, -90, 0), share="cheek", wall=(196, 192, 182))
    if posts:
        cx, cz = width / 2 + 0.6, z0 + ln + 0.6

        def warden(img, info):                               # the stair's warden: a small stern face on each post
            glyph("sun", OCHRE, sides=("left", "right", "back"))(img, info)
            x0, y0, x1, y1 = info.front
            mx, ppm = (x0 + x1) / 2, info.ppm
            m, md = L(img)
            md.rectangle([mx - 0.42 * ppm, y1 - 0.62 * ppm, mx + 0.42 * ppm, y1 - 0.3 * ppm], fill=255)
            carve(img, m, depth=max(2, int(ppm * 0.03)))
            m, md = L(img)
            for t in range(4):
                md.rectangle([mx + (-0.38 + t * 0.2) * ppm, y1 - 0.58 * ppm, mx + (-0.24 + t * 0.2) * ppm, y1 - 0.36 * ppm], fill=255)
            relief(img, m, CREAM, depth=max(2, int(ppm * 0.015)))
            for sx in (-1, 1):
                for t, col in enumerate((CORAL, OCHRE)):
                    m, md = L(img)
                    md.rectangle([mx + sx * 0.5 * ppm - 0.06 * ppm, y1 - (0.74 - t * 0.14) * ppm, mx + sx * 0.5 * ppm + 0.06 * ppm, y1 - (0.64 - t * 0.14) * ppm], fill=255)
                    soft(img, m, col)
        block(P, 1.5, 1.55, 1.5, (cx, 0, cz), b=0.17, base=G2, taper=(0.9, 0.9), paint=warden, moss=0.4)
        for sx in (-1, 1):
            def ep(img, info):
                info["r"] = 0.25
                G.eye_paint(img, info)
            slab(P, circle(0.25, 10), 0.3, (cx + sx * 0.34, 1.02, cz + 0.52), b=0.07, base=G4, paint=ep, share="weye", ppm=44)
            glow_bar(P, 0, 0, (cx + sx * 0.34, 1.02, cz + 0.78), shape=circle(0.1, 6))
            block(P, 0.62, 0.2, 0.4, (cx + sx * 0.34, 1.27, cz + 0.6), b=0.05, base=G3, turn=(0, 0, sx * 12), moss=0, top=G3, share="wbrow", cracks=0.2)
        block(P, 0.3, 0.5, 0.36, (cx, 0.72, cz + 0.62), b=0.07, base=G4, taper=(0.6, 0.6), moss=0, top=G4, cracks=0.2, ppm=38)
        block(P, 1.8, 0.4, 1.8, (width / 2 + 0.6, 1.5, z0 + ln + 0.6), b=0.12, base=G4, moss=0.8, top_moss=1.5)
        glow_bar(P, 0, 0, (width / 2 + 0.6, 1.9, z0 + ln + 0.6), shape=circle(0.4, 10), turn=(-90, 0, 0), depth=0.14)
    P.mirror(start)


def stone_fill(img, info, joints=False):
    """A slab's face given the block's stone treatment inside its outline."""
    sub = Image.new("RGB", img.size, info.base)
    stone(sub, (0, 0, img.size[0], img.size[1]), info.base, info.rnd, info.ppm, 0, joints=0.7 if joints else 0, moss=0, bevel=0, foot=0.4)
    img.paste(sub, (0, 0), info.mask)


# ------------------------------------------------------------------ THE PORTAL
def portal(P):
    rnd = P.rnd
    PY = 2.0                                                  # the platform's top
    HW, HD = 8.8, 4.5
    # ---- the platform: one great mass of fitted stones, a chunky lip of separate stones, corner piers
    block(P, HW * 2, PY - 0.25, HD * 2, (0, 0, 0), b=0.12, base=G1, joints=0.62, moss=0.5, top=False, ppm=32, foot=0.45)
    xs = [-HW - 0.15, -5.4, -2.0, 1.7, 5.0, HW + 0.15]
    for i in range(5):                                         # the lip stones along the front and back
        w = xs[i + 1] - xs[i]
        for sz in (1, -1):
            block(P, w + 0.06, 0.5, 1.5, ((xs[i] + xs[i + 1]) / 2, PY - 0.45, sz * (HD - 0.55)), b=0.13, base=(G4, G2, G4, WARM, G2)[i], jit=0.02, moss=0.6, top_moss=0.6,
                  paint=band_fret(OCHRE, sides=("front", "back")) if i in (0, 4) else None, share=None, top_joints=0, top_ppm=26, ppm=38)
    block(P, HW * 2 - 0.2, 0.42, HD * 2 - 2.4, (0, PY - 0.42, 0), b=0.05, base=G2, top_joints=1.5, top_moss=0.25, top_ppm=28, ppm=18, moss=0, cracks=0.3)
    for sx in (-1, 1):                                         # the side lips
        block(P, 1.5, 0.5, HD * 2 - 2.8, (sx * (HW - 0.55), PY - 0.45, 0), b=0.13, base=G4, moss=0.6, top_moss=0.6, top_ppm=26, ppm=38)
    start = len(P.parts)
    for sz in (1, -1):                                         # corner piers
        block(P, 1.9, PY + 0.55, 1.9, (HW - 0.55, 0, sz * (HD - 0.55)), b=0.2, base=G3, taper=(0.9, 0.9), paint=glyph("spiral", CORAL, sides=("front", "right", "back", "left")), moss=0.5, top_moss=1.2, ppm=42)
        block(P, 2.2, 0.45, 2.2, (HW - 0.55, PY + 0.5, sz * (HD - 0.55)), b=0.14, base=G4, moss=0.9, top_moss=1.6, ppm=38)
    P.mirror(start)
    # ---- the stair
    stair(P, HD, PY, width=6.0)

    # ---- the totems (the same carving, mirrored)
    start = len(P.parts)
    totem(P, 6.3, PY - 0.05, 0.4, variant="a", side=1, greens=False)
    P.mirror(start)

    # ---- the door wall: big separate stones, none square to the next
    WZ = 1.25                                                 # the wall's half depth
    start = len(P.parts)
    yy = PY - 0.05
    rows = [(1.75, 2.15, G3, "man", CORAL), (1.6, 1.95, WARM, "lizard", OCHRE), (1.5, 1.75, G1, "sun", CORAL)]
    for i, (h, inner, tone, gl, col) in enumerate(rows):
        block(P, 5.15 - inner, h + 0.04, WZ * 2 + (0.1 if i == 1 else 0), ((5.15 + inner) / 2, yy, 0), b=0.19, base=tone, jit=0.03, turn=(0, (1.5, -1.2, 0.8)[i], 0),
              paint=glyph(gl, col, sides=("front", "back")), moss=0.3, top=False, ppm=48)
        yy += h
    # the door frame: two leaning painted jambs
    block(P, 0.95, 4.7, WZ * 2 + 0.55, (2.35, PY - 0.05, 0), b=0.15, base=WARM, lean=(-0.42, 0), paint=jamb_paint, moss=0.2, top=False, ppm=48)
    P.mirror(start)
    wall_top = yy                                             # 6.8
    # the lintel
    block(P, 6.3, 1.0, WZ * 2 + 0.8, (0, PY + 4.55, 0), b=0.2, base=G4, taper=(1.06, 1.0), paint=lintel_paint, moss=0.5, top_moss=0.8, ppm=48)
    for k in range(-2, 3):
        glow_bar(P, 0.42, 0.2, (k * 1.05, PY + 4.55 + 0.2, WZ + 0.36), depth=0.1)
        glow_bar(P, 0.42, 0.2, (k * 1.05, PY + 4.55 + 0.2, -WZ - 0.36), depth=0.1, turn=(0, 180, 0))
    # the upper wall behind the disc
    block(P, 10.2, 1.6, WZ * 2 - 0.1, (0, wall_top - 0.05, 0), b=0.2, base=G3, joints=0.75, moss=0.4, top=False, ppm=38)
    block(P, 10.6, 1.25, WZ * 2 + 0.3, (0, wall_top + 1.5, 0), b=0.2, base=G2, paint=band_tris(CORAL, OCHRE, sides=("front", "back")), moss=0.7, top_moss=1.0, ppm=42, taper=(1.02, 1.0))
    WT = wall_top + 2.75                                      # 9.55: where the house stands

    # ---- the sun disc and the ancestor
    R = 2.45
    DY = PY + 7.1
    DZ = WZ + 0.1
    slab(P, circle(R, 32), 0.85, (0, DY, DZ - 0.3), b=0.2, base=G4, paint=disc_paint(R), ppm=50, wall=G2)
    for i in range(12):                                        # the inlay ring
        a = math.tau * (i + 0.5) / 12
        glow_bar(P, 0.5, 0.15, (R * 0.5 * math.cos(a) * 1.0, DY + R * 0.5 * math.sin(a), DZ + 0.52), turn=(0, 0, math.degrees(a) + 90), depth=0.1)
    slab(P, head_shape(1.12, 1.3), 0.6, (0, DY - 0.02, DZ + 0.4), b=0.2, base=(196, 188, 172), paint=ancestor_paint, ppm=64, wall=G4)
    block(P, 0.46, 0.95, 0.5, (0, DY - 0.62, DZ + 0.92), b=0.1, base=(196, 188, 172), taper=(0.55, 0.5), lean=(0, -0.1), moss=0, top=(196, 188, 172), cracks=0.2, ppm=40, foot=0.2)
    for sx in (-1, 1):
        def sp(img, info):
            info["r"] = 0.36
            G.eye_paint(img, info)
        slab(P, circle(0.36, 12), 0.35, (sx * 1.2, DY - 0.35, DZ + 0.42), b=0.09, base=G2, paint=sp, share="spool", ppm=40)
        glow_bar(P, 0, 0, (sx * 1.2, DY - 0.35, DZ + 0.72), shape=circle(0.13, 6))
    for i, (ang, ln, wd, col) in enumerate(((-34, 1.25, 0.72, CORAL), (0, 1.55, 0.85, TURQ_DULL), (34, 1.25, 0.72, CORAL))):
        slab(P, G.kite(ln, wd), 0.4, (math.sin(math.radians(ang)) * 0.75, DY + 1.0 + math.cos(math.radians(ang)) * 0.1, DZ + 0.42), b=0.1, base=G4, paint=fin_paint(col), turn=(0, 0, -ang), share="dfin%d" % abs(i - 1), ppm=44)

    # ---- on the BACK, facing the court: Mayari's moon
    def moon(img, info):
        c = info.px(0, 0)
        k = abs(info.px(1, 0)[0] - c[0])
        f, fd = L(img)
        fd.ellipse([c[0] - 1.38 * k, c[1] - 1.38 * k, c[0] + 1.38 * k, c[1] + 1.38 * k], fill=255)
        carve(img, f, depth=max(3, int(info.ppm * 0.04)), colour=(84, 86, 104))
        m, md = L(img)
        for i in (range(9) if not K.CALM_NOW else ()):
            a = math.tau * i / 9 + 0.3
            rr = 1.05 * k
            K.g_rays(md, c[0] + rr * math.cos(a), c[1] + rr * math.sin(a), 0.05 * k, 0.17 * k, 4, wide=0.9, start=a)
        relief(img, m, CREAM, depth=max(2, int(info.ppm * 0.02)))
        m, md = L(img)
        md.ellipse([c[0] - 0.92 * k, c[1] - 0.92 * k, c[0] + 0.92 * k, c[1] + 0.92 * k], fill=255)
        md.ellipse([c[0] - 0.5 * k, c[1] - 0.95 * k, c[0] + 1.1 * k, c[1] + 0.65 * k], fill=0)
        relief(img, m, mix(info.base, PALE, 0.6), depth=max(3, int(info.ppm * 0.04)), paint=CREAM, rnd=info.rnd, worn=0.3)
    slab(P, circle(1.6, 24), 0.6, (0, wall_top + 1.15, -WZ + 0.2), b=0.16, base=G4, paint=moon, ppm=40, wall=G2, turn=(0, 180, 0))
    crescent = [(-0.78, 0.0), (-0.62, -0.5), (-0.2, -0.82), (-0.3, -0.5), (-0.38, 0.0), (-0.3, 0.5), (-0.2, 0.82), (-0.62, 0.5)]
    glow_bar(P, 0, 0, (0, wall_top + 1.15, -WZ - 0.36), shape=[(-x * 0.55 - 0.75, y * 0.6) for x, y in reversed(crescent)], turn=(0, 180, 0), depth=0.1)

    # ---- the great house on its carved posts
    start = len(P.parts)
    for sz in (1, -1):
        turned(P, [(0.34, 0), (0.3, 0.2), (0.24, 1.0), (0.26, 1.3)], (3.7, WT - 0.05, sz * 0.75), seg=8, paint=post_paint, key="post", ppm=40)
        turned(P, [(0.3, 0), (0.78, 0.06), (0.8, 0.16), (0.3, 0.24)], (3.7, WT + 1.05, sz * 0.75), colour=TIMBER, seg=10)      # the rat guard
    P.mirror(start)
    FY = WT + 1.25
    block(P, 9.4, 0.5, 4.0, (0, FY, 0), b=0.12, base=TIMBER, raw=raw_wood(TIMBER), ppm=34, top=False)
    for sx in (-1, 0, 1):                                      # the floor joists' carved ends
        for sz in (1, -1):
            block(P, 0.5, 0.42, 0.7, (sx * 3.9, FY + 0.04, sz * 2.1), b=0.08, base=TIMBER, raw=raw_wood(shade(TIMBER, 1.15)), share="joist", ppm=34, top=TIMBER)
    block(P, 8.6, 0.75, 3.5, (0, FY + 0.48, 0), b=0.08, base=CREAM, raw=raw_weave, ppm=40, top=False)
    RY = FY + 1.15
    UNDER = (78, 56, 40)
    block(P, 11.0, 1.0, 6.4, (0, RY - 0.2, 0), b=0.2, base=THATCH, raw=raw_thatch, taper=(0.84, 0.78), ppm=34, top=False, bottom=UNDER)     # the flared eave
    block(P, 9.3, 1.6, 5.1, (0, RY + 0.62, 0), b=0.22, base=THATCH, raw=raw_thatch, taper=(0.7, 0.64), ppm=34, top=False, bottom=UNDER)
    block(P, 6.6, 2.5, 3.4, (0, RY + 2.05, 0), b=0.22, base=THATCH, raw=raw_thatch, taper=(0.46, 0.34), ppm=34, top=shade(THATCH, 0.8), bottom=UNDER)
    TOPY = RY + 2.05 + 2.5
    block(P, 3.3, 0.5, 1.3, (0, TOPY - 0.2, 0), b=0.14, base=TIMBER, raw=raw_wood(shade(TIMBER, 0.9)), ppm=34)
    for sx in (-1, 1):                                         # carabao horns at the ridge
        pts = curve([(sx * 1.2, TOPY + 0.05, 0), (sx * 2.0, TOPY + 0.2, 0), (sx * 2.55, TOPY + 0.8, 0), (sx * 2.4, TOPY + 1.5, 0)], 10)
        pipe(P, pts, [0.3 - 0.265 * k / 10 for k in range(11)], CREAM, seg=6, kind="add")
    slab(P, G.kite(1.2, 0.8), 0.4, (0, TOPY + 0.2, -0.2), b=0.1, base=TIMBER, paint=fin_paint(RED), wall=TIMBER, ppm=38)

    # ---- cloth, pots
    for sx, ln in ((-1, 2.3), (1, 1.9)):
        slab(P, [(-0.45, 0), (0, -0.4), (0.45, 0), (0.45, ln), (-0.45, ln)], 0.08, (sx * 3.75, PY + 4.3 - ln, WZ + 0.12), b=0.02, base=RED, paint=cloth_paint, share="cloth%d" % sx, wall=shade(RED, 0.7), ppm=44,
             kind="loose", turn=(3, 0, sx * 2))
    for (px, pz, s) in ((-3.9, 2.9, 1.0), (-4.75, 2.5, 0.7), (4.3, 3.0, 0.85)):
        turned(P, [(0.2 * s, 0), (0.46 * s, 0.25 * s), (0.5 * s, 0.55 * s), (0.3 * s, 0.85 * s), (0.36 * s, 0.98 * s)], (px, PY - 0.03, pz), seg=9, paint=pot_paint, key="pot", ppm=40)

    # ---- what grows on it: vines pouring over the right shoulder, ferns at the piers
    vine(P, [(2.2, WT + 0.2, WZ + 0.4), (3.4, WT - 0.9, WZ + 0.55), (4.3, wall_top + 0.6, WZ + 0.5), (4.0, PY + 3.4, WZ + 0.55), (4.6, PY + 1.9, WZ + 0.5), (4.3, PY + 0.5, WZ + 0.6)], radius=0.11, leaves=13, size=0.95, seed=7, wander=0.5)
    vine(P, [(4.6, WT + 0.1, WZ + 0.3), (5.0, WT - 1.3, WZ + 0.6), (3.2, PY + 4.7, WZ + 0.75), (2.9, PY + 3.2, WZ + 0.7)], radius=0.09, leaves=10, size=0.85, seed=9, wander=0.4)
    vine(P, [(5.0, RY + 0.2, 3.0), (5.6, WT + 0.4, 2.2), (6.0, WT - 1.0, 1.95), (5.7, WT - 2.4, 2.1)], radius=0.09, leaves=8, size=0.9, seed=13, wander=0.5)
    vine(P, [(-4.3, WT + 0.15, WZ + 0.3), (-4.7, WT - 1.0, WZ + 0.45), (-4.4, wall_top + 0.3, WZ + 0.4)], radius=0.08, leaves=6, size=0.8, seed=21, wander=0.4)
    vine(P, [(-1.5, WT + 0.1, -WZ - 0.3), (-2.4, WT - 1.2, -WZ - 0.5), (-3.2, wall_top - 0.5, -WZ - 0.45), (-3.5, PY + 2.5, -WZ - 0.5)], radius=0.1, leaves=9, size=0.9, seed=17, wander=0.5, out=(0, 0, -1))
    r2 = random.Random(5)
    for (fx, fz, s) in ((HW + 0.3, HD + 0.2, 1.5), (-HW - 0.3, HD + 0.3, 1.3), (HW + 0.4, -HD, 1.4), (-HW - 0.2, -HD - 0.2, 1.5), (4.3, HD + 0.5, 1.1), (-4.5, HD + 0.4, 1.2), (7.6, PY, 2.9), (-7.9, PY, 3.1)):
        base_y = PY - 0.03 if abs(fz) < 4 and fx in (7.6, -7.9) else 0.0
        if fx in (7.6, -7.9):
            fx, base_y, fz = fx, PY - 0.03, fz
        for j in range(4):
            leaf(P, (fx, base_y, fz), s * r2.uniform(0.75, 1.1), turn=(r2.uniform(18, 50), j * 90 + r2.uniform(-25, 25), 0), kind_shape="frond", tone=j)
    for (bx, bz, s, yaw) in ((5.3, HD + 1.6, 1.0, 20), (6.1, HD + 2.3, 0.6, -30), (-7.0, HD + 1.2, 0.8, 50)):   # tumbled stones
        block(P, 0.9 * s, 0.6 * s, 0.75 * s, (bx, 0, bz), b=0.14 * s, base=rnd.choice((G1, G2, G4)), turn=(0, yaw, 5), moss=0.6, top_moss=1.5, ppm=34)
    return TOPY


def jamb_paint(img, info):
    """The door's leaning jambs: worn ochre with a red stepped key running up them."""
    glyph("zig", CORAL, sides=("left", "right"))(img, info)
    for s in ("front", "back"):
        soft_panel(img, info[s], mix(OCHRE, info.base, 0.3), info)
        x0, y0, x1, y1 = inset(info[s], info.bpx * 2.2)
        m, md = L(img)
        n = max(3, int((y1 - y0) / ((x1 - x0) * 0.9)))
        u = (y1 - y0) / n
        wd = max(4, int(info.ppm * 0.09))
        for k in range(n):
            ya = y0 + k * u
            md.line([(x0, ya + u * 0.85), (x0, ya + u * 0.1), (x1, ya + u * 0.1), (x1, ya + u * 0.6), ((x0 + x1) / 2, ya + u * 0.6), ((x0 + x1) / 2, ya + u * 0.38)], fill=255, width=wd, joint="curve")
        relief(img, m, mix(info.base, PALE, 0.45), depth=max(2, int(info.ppm * 0.03)), paint=RED, rnd=info.rnd, worn=0.5)


def lintel_paint(img, info):
    for s in ("front", "back"):
        soft_panel(img, info[s], CORAL, info)
        x0, y0, x1, y1 = inset(info[s], info.bpx * 1.9)
        m, md = L(img)
        K.g_zig(md, (x0, y0 + (y1 - y0) * 0.55, x1, y1), 14, max(4, int(info.ppm * 0.07)))
        relief(img, m, CREAM, depth=max(2, int(info.ppm * 0.025)), paint=OCHRE, rnd=info.rnd, worn=0.4)
        m, md = L(img)                                       # the dark beds the inlay sits in
        cx = (info[s][0] + info[s][2]) / 2
        for k in range(-2, 3):
            md.rectangle([cx + (k * 1.05 - 0.3) * info.ppm, y0 + (y1 - y0) * 0.02, cx + (k * 1.05 + 0.3) * info.ppm, y0 + (y1 - y0) * 0.42], fill=255)
        carve(img, m, depth=max(2, int(info.ppm * 0.03)))
    K.streaks(img, info.front, info.rnd, info.ppm, n=3)


def pot_paint(img, info):
    w, h = img.size
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, w, h], fill=(176, 104, 70))
    d.rectangle([0, 0, w, h * 0.12], fill=(214, 150, 110))
    d.rectangle([0, h * 0.7, w, h], fill=(138, 78, 56))
    m, md = L(img)
    K.g_zig(md, (0, h * 0.3, w, h * 0.5), 8, max(3, int(info.ppm * 0.05)))
    soft(img, m, CREAM)
    d.line([(0, h * 0.24), (w, h * 0.24)], fill=BLACK, width=max(2, int(info.ppm * 0.03)))
    d.line([(0, h * 0.56), (w, h * 0.56)], fill=BLACK, width=max(2, int(info.ppm * 0.03)))


def build_portal():
    P = K.Piece("gate_portal", size=2048, ppm=46, seed=7)
    top = portal(P)
    P.meta.update({"ink": 0.05, "figure": (2.0, 9.5), "distance": 23.0, "look": 0.44, "fov": 60, "back_up": 0.0,
                   "close": [(0, 8.6, 1.5), (3.2, 5.6, 12.5)], "close_fov": 50,
                   "extra": {"far": [(6.0, 1.7, 40.0), (0, 8.0, 0), 34], "door": [(1.5, 3.7, 9.0), (0, 6.2, 0), 62]}})
    P.write()


def build_stair():
    P = K.Piece("gate_stair", size=2048, ppm=50, seed=3)
    block(P, 9.0, 2.0, 1.2, (0, 0, -0.6), b=0.12, base=G1, joints=0.62, moss=0.5, top_joints=1.2, ppm=42)
    stair(P, 0.0, 2.0, width=6.0)
    vine(P, [(-4.0, 2.45, -0.2), (-3.9, 2.3, 1.2), (-4.25, 1.6, 2.4), (-4.2, 0.9, 3.4), (-4.3, 0.1, 4.0)], radius=0.07, leaves=9, size=0.6, seed=4, wander=0.3, out=(-1, 0, 0.3))
    r2 = random.Random(8)
    for (fx, fz, s) in ((4.6, 2.2, 1.0), (-4.7, 4.9, 0.9), (4.7, 5.2, 0.8)):
        for j in range(4):
            leaf(P, (fx, 0, fz), s * r2.uniform(0.75, 1.1), turn=(r2.uniform(18, 50), j * 90 + r2.uniform(-25, 25), 0), kind_shape="frond", tone=j)
    block(P, 0.7, 0.45, 0.6, (2.2, 0, 5.0), b=0.11, base=G4, turn=(0, 30, 4), moss=0.6, top_moss=1.5, ppm=38)
    P.meta.update({"ink": 0.03, "figure": (1.2, 5.6), "distance": 12.0, "look": 0.5, "fov": 56, "close": [(3.3, 0.95, 4.6), (5.6, 1.7, 9.2)], "close_fov": 46})
    P.write()


# ------------------------------------------------------------------ THE GUARDIAN BEASTS
def muzzle_paint(img, info):
    x0, y0, x1, y1 = info.front
    mx, ppm = (x0 + x1) / 2, info.ppm
    h = y1 - y0
    for sx in (-1, 1):
        m, md = L(img)
        cx, cy = mx + sx * 0.28 * ppm, y0 + h * 0.36
        md.ellipse([cx - 0.13 * ppm, cy - 0.09 * ppm, cx + 0.13 * ppm, cy + 0.09 * ppm], fill=255)
        carve(img, m, depth=max(2, int(ppm * 0.03)))
    d = ImageDraw.Draw(img)
    wd = max(4, int(ppm * 0.05))
    for s in ("left", "front", "right"):
        a = info[s]
        d.line([(a[0] + info.bpx, y0 + h * 0.72), (a[2] - info.bpx, y0 + h * 0.72)], fill=INK, width=wd)
        d.line([(a[0] + info.bpx, y0 + h * 0.72 + wd), (a[2] - info.bpx, y0 + h * 0.72 + wd)], fill=mix(info.base, PALE, 0.7), width=max(2, wd // 2))


def body_paint(img, info):
    glyph("spiral", CORAL, sides=("left", "right"))(img, info)
    glyph("lizard", OCHRE, sides=("back",))(img, info)
    soft_panel(img, info.front, mix(OCHRE, info.base, 0.25), info)
    glyph("sun", CREAM, sides=("front",))(img, info)


def guardian(P, x=0.0, z=0.0, side=1):
    """A carabao-headed guardian seated on its plinth, its foot's centre at (x, 0, z), facing +z."""
    T = lambda dx, dy, dz: (x + dx, dy, z + dz)
    block(P, 2.7, 0.9, 3.3, T(0, 0, 0), b=0.11, base=G3, paint=band_fret(OCHRE), taper=(0.93, 0.94), moss=0.5, top=False)
    block(P, 3.0, 0.32, 3.6, T(0, 0.82, 0), b=0.1, base=G4, moss=0.8, top_moss=1.4, cracks=0.4)
    Y = 1.07
    # the haunches and the chest, leaning forward
    block(P, 2.2, 1.0, 2.3, T(0, Y, -0.35), b=0.2, base=G1, taper=(0.96, 0.95), moss=0.3, top=False, paint=glyph("zig", CORAL, sides=("left", "right", "back")))
    block(P, 1.95, 1.25, 1.9, T(0, Y + 0.95, -0.2), b=0.2, base=G2, taper=(0.9, 0.85), lean=(0, 0.22), moss=0.3, top=False, paint=body_paint)
    for sx in (-1, 1):                                         # the forelegs and the paws
        block(P, 0.62, 1.75, 0.72, T(sx * 0.68, Y, 0.92), b=0.13, base=G4, taper=(0.85, 0.85), lean=(-sx * 0.05, -0.08), moss=0.15, top=G4, share="leg", cracks=0.5, paint=glyph("zig", OCHRE, sides=("front",)))
        block(P, 0.8, 0.42, 1.0, T(sx * 0.68, Y, 1.15), b=0.12, base=G2, moss=0.3, top=G2, share="paw", cracks=0.3,
              paint=lambda img, info: [ImageDraw.Draw(img).line([(info.front[0] + (info.front[2] - info.front[0]) * t, info.front[1] + info.bpx * 1.6), (info.front[0] + (info.front[2] - info.front[0]) * t, info.front[3] - info.bpx)], fill=INK, width=max(3, int(info.ppm * 0.04))) for t in (0.36, 0.64)] and None)
    # the collar, a bell hung from it
    CY = Y + 2.12
    HY0 = CY + 0.4
    block(P, 2.2, 0.62, 2.05, T(0, CY - 0.1, 0.08), b=0.085, base=G4, paint=band_tris(RED, OCHRE), moss=0.15, top=False, turn=(6, 0, 0))
    turned(P, [(0.12, 0.0), (0.28, 0.03), (0.25, 0.22), (0.15, 0.35), (0.06, 0.4)], T(0, CY - 0.36, 1.2), colour=OCHRE, seg=8)
    ring = [T(0.24 * math.cos(a), HY0 + 0.1 + 0.24 * math.sin(a), 2.0) for a in [math.tau * k / 10 for k in range(11)]]
    pipe(P, ring, 0.055, OCHRE, seg=5, kind="add")
    # the head
    HY = CY + 0.4
    block(P, 2.0, 1.55, 1.85, T(0, HY, 0.42), b=0.2, base=G2, taper=(1.08, 1.0), moss=0.2, top_moss=1.0, cracks=0.6,
          paint=head_paint)
    block(P, 1.3, 0.92, 1.05, T(0, HY - 0.05, 1.45), b=0.17, base=G4, taper=(0.86, 0.82), lean=(0, -0.05), moss=0, top=G4, paint=muzzle_paint, cracks=0.3)
    for sx in (-1, 1):
        def ep(img, info):
            info["r"] = 0.29
            G.eye_paint(img, info)
        slab(P, circle(0.29, 12), 0.32, T(sx * 0.6, HY + 1.02, 1.2), b=0.08, base=G4, paint=ep, share="geye", ppm=50)
        glow_bar(P, 0, 0, T(sx * 0.6, HY + 1.02, 1.49), shape=circle(0.115, 6))
        block(P, 0.8, 0.24, 0.5, T(sx * 0.6, HY + 1.3, 1.22), b=0.06, base=G3, turn=(0, 0, sx * 13), moss=0, top=G3, share="gbrow", cracks=0.2)
        # the horns: out, up, and curling back in
        pts = curve([T(sx * 0.85, HY + 1.3, 0.3), T(sx * 1.75, HY + 1.4, 0.3), T(sx * 2.3, HY + 2.0, 0.34), T(sx * 2.05, HY + 2.75, 0.4), T(sx * 1.55, HY + 3.0, 0.42)], 12)
        pipe(P, pts, [0.36 - 0.31 * k / 12 for k in range(13)], CREAM, seg=7, kind="add", paint=horn_paint, key="horn", ppm=38)
        ear = [(0, 0), (0.95, -0.28), (1.2, 0.0), (0.9, 0.36), (0, 0.42)]
        ear = ear if sx > 0 else [(-a, b) for a, b in reversed(ear)]
        slab(P, ear, 0.4, T(sx * 0.95, HY + 0.55, 0.15), b=0.09, base=G3, paint=wing_paint(CORAL, "none"), share="gear%d" % sx, turn=(0, sx * -18, sx * -12))
    glow_bar(P, 0, 0, T(0, HY + 1.18, 1.37), shape=[(0, -0.2), (0.14, 0), (0, 0.2), (-0.14, 0)])
    slab(P, G.kite(1.0, 0.7), 0.4, T(0, HY + 1.4, 0.2), b=0.1, base=G4, paint=fin_paint(OCHRE), share="gknot")
    # the tail, curled on the plinth
    pipe(P, curve([T(0, Y + 0.8, -1.45), T(side * 0.5, Y + 0.3, -1.62), T(side * 1.05, Y + 0.12, -1.2), T(side * 1.25, Y + 0.1, -0.5)], 8), [0.17 - 0.08 * k / 8 for k in range(9)], G4, seg=6, kind="add")
    ball(P, (0.22, 0.2, 0.3), T(side * 1.27, Y + 0.14, -0.32), shade(BLACK, 1.6), seg=7, rings=5)


def head_paint(img, info):
    glyph("spiral", CORAL, sides=("left", "right", "back"))(img, info)
    x0, y0, x1, y1 = info.front
    soft_panel(img, (x0 + info.bpx * 1.6, y0 + info.bpx * 1.6, x1 - info.bpx * 1.6, y0 + (y1 - y0) * 0.36), mix(CORAL, info.base, 0.15), Info0(info))
    for sx in (-1, 1):
        for k, col in enumerate((OCHRE, CORAL)):
            m, md = L(img)
            cx = (x0 + x1) / 2 + sx * 0.78 * info.ppm
            yy = y0 + (y1 - y0) * (0.62 + k * 0.12)
            md.rectangle([cx - 0.14 * info.ppm, yy, cx + 0.14 * info.ppm, yy + 0.09 * info.ppm], fill=255)
            relief(img, m, mix(info.base, PALE, 0.4), depth=max(2, int(info.ppm * 0.02)), paint=col, rnd=info.rnd, worn=0.4)


def horn_paint(img, info):
    w, h = img.size
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, w, h], fill=CREAM)
    d.rectangle([0, 0, w * 0.4, h], fill=mix(CREAM, (150, 130, 100), 0.5))
    for k in range(7):
        y = h * (0.1 + k * 0.1)
        d.line([(0, y), (w, y)], fill=(170, 148, 112), width=max(2, int(info.ppm * 0.035)))
    d.rectangle([0, h * 0.74, w, h * 0.8], fill=RED)
    d.rectangle([0, h * 0.8, w, h * 0.9], fill=OCHRE)
    d.rectangle([0, h * 0.9, w, h], fill=RED)
    d.rectangle([0, 0, w, h * 0.07], fill=(120, 96, 72))


def build_guardians():
    P = K.Piece("gate_guardians", size=2048, ppm=64, seed=31)
    P.default_jit = 0.008
    start = len(P.parts)
    guardian(P, 3.4, 0.0, side=1)
    P.mirror(start)
    vine(P, [(3.9, 4.6, 0.6), (4.6, 3.8, 0.9), (4.45, 2.6, 1.1), (4.75, 1.4, 1.3), (4.9, 0.5, 1.5)], radius=0.06, leaves=8, size=0.5, seed=2, wander=0.3, out=(0.6, 0, 0.8))
    r2 = random.Random(4)
    for (fx, fz, s) in ((-5.0, 1.2, 0.9), (-1.9, -1.5, 0.8), (4.9, -1.4, 1.0), (1.9, 1.6, 0.7)):
        for j in range(4):
            leaf(P, (fx, 0, fz), s * r2.uniform(0.75, 1.1), turn=(r2.uniform(18, 50), j * 90 + r2.uniform(-25, 25), 0), kind_shape="frond", tone=j)
    block(P, 0.6, 0.4, 0.5, (-1.7, 0, 1.6), b=0.1, base=G4, turn=(0, 20, 5), moss=0.6, top_moss=1.5, ppm=40)
    P.meta.update({"ink": 0.03, "figure": (0.0, 1.2), "distance": 11.0, "look": 0.45, "fov": 54, "close": [(3.4, 4.3, 0.8), (5.6, 3.6, 6.2)], "close_fov": 48})
    P.write()
