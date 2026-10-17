"""PAYO, the GATE family, one piece at a time (see tools/payo_pieces/gate_kit.py for the method and the four steps).

  py -3 tools/payo_pieces/gate_build.py <piece>                  totem_a, totem_b, portal, guardian, stair, shrine
  py -3 tools/payo_pieces/gate_build.py sheet <piece> <version> [note]
  py -3 tools/payo_pieces/gate_build.py compare <piece> <version> <reference picture>
"""
import math
import random
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import gate_kit as K                                  # noqa: E402
from gate_kit import (block, slab, pipe, turned, ball, leaf, vine, glow_bar, circle, curve, stone, relief, carve, soft, L, shade, mix,      # noqa: E402
                      PALE, INK, DARK, OCHRE, RED, CORAL, TURQ, CREAM, MOSS, MOSS_L, LEAF, THATCH, TIMBER, BLACK, STONES, SHADOW)

G1, G2, G3, G4, WARM = (128, 125, 120), (146, 142, 135), (110, 108, 106), (162, 157, 148), (150, 138, 122)


def inset(box, m):
    return (box[0] + m, box[1] + m, box[2] - m, box[3] - m)


# ------------------------------------------------------------------ carving painted on blocks
def band_fret(colour=OCHRE, sides=("left", "front", "right", "back")):
    def fn(img, info):
        for s in sides:
            x0, y0, x1, y1 = inset(info[s], info.bpx * 1.75)
            m, d = L(img)
            K.g_fret(d, (x0, y0, x1, y1), (y1 - y0) * 1.3, max(3, int(info.ppm * 0.12)))
            relief(img, m, mix(info.base, PALE, 0.35), depth=max(2, int(info.ppm * 0.035)), paint=colour, rnd=info.rnd, worn=0.6)
    return fn


def band_tris(colour=CORAL, colour2=OCHRE, sides=("left", "front", "right", "back"), slots=()):
    """A headband: a painted ground, a row of triangles standing proud, dark slots where the inlay sits."""
    def fn(img, info):
        for s in sides:
            x0, y0, x1, y1 = inset(info[s], info.bpx * 1.9)
            d = ImageDraw.Draw(img)
            g, gd = L(img)
            gd.rectangle([x0, y0, x1, y1], fill=255)
            holes, hd = L(img)
            for _ in range(int((x1 - x0) / info.ppm * 2.5)):
                K.blob(hd, info.rnd.uniform(x0, x1), info.rnd.uniform(y0, y1), info.rnd.uniform(0.05, 0.2) * info.ppm, info.rnd.uniform(0.04, 0.12) * info.ppm, info.rnd, 255)
            if K.CALM_NOW:
                soft(img, g, colour2)
            else:
                soft(img, ImageChops.multiply(g, ImageChops.invert(holes)), colour2)
                d.line([(x0, y0), (x1, y0)], fill=shade(colour2, 0.6), width=max(2, int(info.ppm * 0.03)))
            n = max(3, int((x1 - x0) / ((y1 - y0) * 0.9)))
            m, md = L(img)
            K.g_tris(md, (x0, y0 + (y1 - y0) * 0.12, x1, y1 - (y1 - y0) * 0.08), n, up=True, gap=0.1)
            relief(img, m, mix(info.base, PALE, 0.4), depth=max(2, int(info.ppm * 0.03)), paint=colour, rnd=info.rnd, worn=1.0)
    return fn


def face_paint(eyes, eye_r, cheeks=True, back="lizard"):
    """The face block: dark painted sockets under the modelled eye rings, worn cheek stripes, a carving on its back."""
    def fn(img, info):
        x0, y0, x1, y1 = info.front
        ppm = info.ppm
        W = info.W
        cx = (x0 + x1) / 2

        def px(x, y):                                     # metres from the face's centre line and its foot
            return (cx + x * ppm, y1 - y * ppm)
        d = ImageDraw.Draw(img)
        for (ex, ey) in eyes:
            m, md = L(img)
            a = px(ex, ey)
            r = eye_r * 1.25 * ppm
            md.ellipse([a[0] - r, a[1] - r, a[0] + r, a[1] + r], fill=255)
            carve(img, m, depth=max(3, int(ppm * 0.05)))
        if cheeks:
            for sx in (-1, 1):
                for k, col in enumerate((CORAL, OCHRE, CORAL)):
                    m, md = L(img)
                    yy = eyes[0][1] - eye_r * 1.5 - 0.1 - k * 0.2
                    a, b = px(sx * (W * 0.2), yy), px(sx * (W * 0.42), yy + 0.12)
                    md.polygon([a, (b[0], b[1]), (b[0], b[1] + ppm * 0.13), (a[0], a[1] + ppm * 0.13)], fill=255)
                    relief(img, m, mix(info.base, PALE, 0.3), depth=max(2, int(ppm * 0.02)), paint=col, rnd=info.rnd, worn=1.2)
        bx0, by0, bx1, by1 = inset(info.back, info.bpx * 2.5)
        m, md = L(img)
        s = min(bx1 - bx0, by1 - by0)
        if back == "lizard":
            K.g_lizard(md, (bx0 + bx1) / 2, (by0 + by1) / 2, s * 0.95, max(4, int(ppm * 0.09)))
        else:
            K.g_man(md, (bx0 + bx1) / 2, (by0 + by1) / 2, s * 0.9, max(4, int(ppm * 0.09)))
        relief(img, m, mix(info.base, PALE, 0.35), depth=max(2, int(ppm * 0.035)), paint=OCHRE, rnd=info.rnd, worn=1.0)
        for s_ in ("left", "right"):
            sx0, sy0, sx1, sy1 = inset(info[s_], info.bpx * 2.5)
            m, md = L(img)
            K.g_spiral(md, (sx0 + sx1) / 2, (sy0 + sy1) / 2, min(sx1 - sx0, sy1 - sy0) * 0.42, 2.2, max(4, int(ppm * 0.09)))
            relief(img, m, mix(info.base, PALE, 0.35), depth=max(2, int(ppm * 0.035)), paint=CORAL, rnd=info.rnd, worn=1.0)
    return fn


def mouth_paint(mw, mh, my):
    def fn(img, info):
        x0, y0, x1, y1 = info.front
        cx = (x0 + x1) / 2
        ppm = info.ppm
        lips = (cx - (mw / 2 + 0.3) * ppm, y1 - (my + mh + 0.2) * ppm, cx + (mw / 2 + 0.3) * ppm, y1 - (my - 0.2) * ppm)
        soft_panel(img, lips, mix(RED, info.base, 0.15), Info0(info))
        m, md = L(img)
        md.rectangle([cx - mw / 2 * ppm - 0.08 * ppm, y1 - (my + mh) * ppm - 0.08 * ppm, cx + mw / 2 * ppm + 0.08 * ppm, y1 - my * ppm + 0.08 * ppm], fill=255)
        soft(img, m, shade(DARK, 0.7))
        for s in ("left", "right"):
            bx = inset(info[s], info.bpx * 2.4)
            mm, dd = L(img)
            K.g_zig(dd, (bx[0], bx[1] + (bx[3] - bx[1]) * 0.25, bx[2], bx[3] - (bx[3] - bx[1]) * 0.25), 3, max(4, int(ppm * 0.09)))
            relief(img, mm, mix(info.base, PALE, 0.35), depth=max(2, int(ppm * 0.035)), paint=OCHRE, rnd=info.rnd, worn=1.0)
    return fn


def disc_paint(kind="sun"):
    def fn(img, info):
        ppm = info.ppm
        c = info.px(0, 0)
        R = abs(info.px(1, 0)[0] - c[0])                   # pixels per metre
        r = info.r * R
        wd = max(3, int(ppm * 0.06))
        m, md = L(img)
        K.g_ring(md, c[0], c[1], r * 0.9, wd)
        if kind == "sun":
            K.g_rays(md, c[0], c[1], r * 0.5, r * 0.82, 10, wide=0.6)
            K.g_ring(md, c[0], c[1], r * 0.42, wd)
        else:
            K.g_spiral(md, c[0], c[1], r * 0.78, 2.6, int(wd * 1.3))
        relief(img, m, mix(info.base, PALE, 0.4), depth=max(2, int(ppm * 0.03)), paint=OCHRE if kind == "sun" else CORAL, rnd=info.rnd, worn=0.8)
    return fn


def fin_paint(colour):
    """A crown fin: a painted inner panel, worn, a crack."""
    def fn(img, info):
        inner = info.mask
        k = int(info.ppm * 0.11)
        for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)):
            inner = ImageChops.multiply(inner, ImageChops.offset(info.mask, dx, dy))
        holes, hd = L(img)
        bb = inner.getbbox() or (0, 0, 1, 1)
        for _ in range(4):
            K.blob(hd, info.rnd.uniform(bb[0], bb[2]), info.rnd.uniform(bb[1], bb[3]), info.rnd.uniform(0.08, 0.2) * info.ppm, info.rnd.uniform(0.06, 0.14) * info.ppm, info.rnd, 255)
        if K.CALM_NOW:                                     # calm: one painted panel, sunk a little, no wear, no crack
            soft(img, inner, colour)
            K.hset(img, inner.filter(ImageFilter.GaussianBlur(max(1, info.ppm * 0.03))), -0.015)
            return
        carve(img, inner, depth=max(2, int(info.ppm * 0.025)), colour=shade(colour, 0.62))
        k2 = int(info.ppm * 0.03)
        soft(img, ImageChops.multiply(ImageChops.multiply(inner, ImageChops.offset(inner, -k2, -k2)), ImageChops.invert(holes)), colour)
        hi = ImageChops.multiply(inner, ImageChops.invert(ImageChops.offset(inner, 0, -int(info.ppm * 0.3))))
        soft(img, ImageChops.multiply(hi, ImageChops.invert(holes)), mix(colour, PALE, 0.35), 0.8)
        d = ImageDraw.Draw(img)
        K.crack(d, (bb[0] + bb[2]) / 2 + info.rnd.uniform(-10, 10), bb[1] + (bb[3] - bb[1]) * 0.3, info.ppm * 0.5, info.rnd, info.ppm)
    return fn


def wing_paint(colour, glyph="spiral"):
    def fn(img, info):
        inner = info.mask
        k = int(info.ppm * 0.1)
        for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)):
            inner = ImageChops.multiply(inner, ImageChops.offset(info.mask, dx, dy))
        holes, hd = L(img)
        bb = inner.getbbox() or (0, 0, 1, 1)
        for _ in range(6):
            K.blob(hd, info.rnd.uniform(bb[0], bb[2]), info.rnd.uniform(bb[1], bb[3]), info.rnd.uniform(0.08, 0.2) * info.ppm, info.rnd.uniform(0.06, 0.14) * info.ppm, info.rnd, 255)
        if K.CALM_NOW:
            soft(img, inner, colour)
            K.hset(img, inner.filter(ImageFilter.GaussianBlur(max(1, info.ppm * 0.03))), -0.015)
        else:
            carve(img, inner, depth=max(2, int(info.ppm * 0.025)), colour=shade(colour, 0.62))
            k2 = int(info.ppm * 0.03)
            soft(img, ImageChops.multiply(ImageChops.multiply(inner, ImageChops.offset(inner, -k2, -k2)), ImageChops.invert(holes)), colour)
        m, md = L(img)
        cx, cy = (bb[0] + bb[2]) / 2, (bb[1] + bb[3]) / 2
        if glyph == "spiral":
            K.g_spiral(md, cx, cy, min(bb[2] - bb[0], bb[3] - bb[1]) * 0.36, 2.0, max(4, int(info.ppm * 0.08)))
        elif glyph == "zig":
            K.g_zig(md, (bb[0] + k, cy - (bb[3] - bb[1]) * 0.18, bb[2] - k, cy + (bb[3] - bb[1]) * 0.18), 3, max(4, int(info.ppm * 0.08)))
        relief(img, ImageChops.multiply(m, inner), mix(colour, PALE, 0.6), depth=max(2, int(info.ppm * 0.025)))
    return fn


def beak_paint(img, info):
    inner = info.mask
    k = int(info.ppm * 0.08)
    for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)):
        inner = ImageChops.multiply(inner, ImageChops.offset(info.mask, dx, dy))
    holes, hd = L(img)
    bb = inner.getbbox() or (0, 0, 1, 1)
    for _ in range(7):
        K.blob(hd, info.rnd.uniform(bb[0], bb[2]), info.rnd.uniform(bb[1], bb[3]), info.rnd.uniform(0.08, 0.2) * info.ppm, info.rnd.uniform(0.06, 0.14) * info.ppm, info.rnd, 255)
    keep = ImageChops.multiply(inner, ImageChops.invert(holes)) if not K.CALM_NOW else inner
    soft(img, keep, OCHRE)
    root, rd = L(img)
    a = info.px(0.75, 0)
    rd.rectangle([0, 0, a[0], img.size[1]], fill=255)
    soft(img, ImageChops.multiply(keep, root), RED)
    hi, hd2 = L(img)
    hd2.polygon([info.px(0.2, 1.0), info.px(1.5, 0.8), info.px(2.3, 0.25), info.px(2.2, 0.05), info.px(1.4, 0.55), info.px(0.2, 0.75)], fill=255)
    soft(img, ImageChops.multiply(keep, hi), mix(OCHRE, PALE, 0.5))
    d = ImageDraw.Draw(img)
    d.line([info.px(0.1, 0.42), info.px(1.3, 0.3), info.px(2.1, -0.2), info.px(2.4, -0.7)], fill=INK, width=max(3, int(info.ppm * 0.05)), joint="curve")


def eye_paint(img, info):
    c = info.px(0, 0)
    R = abs(info.px(1, 0)[0] - c[0])
    r = info.r * R
    m, md = L(img)
    md.ellipse([c[0] - r * 0.52, c[1] - r * 0.52, c[0] + r * 0.52, c[1] + r * 0.52], fill=255)
    carve(img, m, depth=max(2, int(info.ppm * 0.04)), colour=shade(DARK, 0.6))


def glyph(kind, colour, sides=("front", "back")):
    """One carved figure in the middle of a stone's face."""
    def fn(img, info):
        for s in sides:
            x0, y0, x1, y1 = inset(info[s], info.bpx * 1.9)
            m, md = L(img)
            cx, cy, sz = (x0 + x1) / 2, (y0 + y1) / 2, min(x1 - x0, y1 - y0)
            wd = max(4, int(info.ppm * 0.1 * (1.5 if K.CALM_NOW else 1.0)))
            if kind == "man":
                K.g_man(md, cx, cy, sz * 0.95, wd)
            elif kind == "lizard":
                K.g_lizard(md, cx, cy, sz * 0.95, wd)
            elif kind == "spiral":
                K.g_spiral(md, cx, cy, sz * 0.42, 2.3, wd)
            elif kind == "sun":
                K.g_rays(md, cx, cy, sz * 0.26, sz * 0.46, 8, wide=0.6)
                K.g_ring(md, cx, cy, sz * 0.2, wd)
            else:
                K.g_zig(md, (x0, cy - sz * 0.3, x1, cy - sz * 0.05), 3, wd)
                K.g_zig(md, (x0, cy + sz * 0.05, x1, cy + sz * 0.3), 3, wd)
            relief(img, m, mix(info.base, PALE, 0.35), depth=max(2, int(info.ppm * 0.035)), paint=colour if not K.CALM_NOW else mix(colour, info.base, 0.35), rnd=info.rnd, worn=0.3)
    return fn


# ------------------------------------------------------------------ THE TOTEM PILLAR
def kite(ln, wd, tip=0.6, notch=0.0):
    """A crown fin: narrow at its foot, widest at `tip` of its length, a point at its top."""
    return [(-wd * 0.28, 0.0), (wd * 0.28, 0.0), (wd * 0.5, ln * tip), (0.0, ln), (-wd * 0.5, ln * tip)]


def wing_shape(w, h, steps=3):
    """An ear: a stepped flare, wider toward its top, its inner edge at x = 0 and pointing to +x."""
    pts = [(0.0, 0.0)]
    for k in range(steps):
        x = w * (0.45 + 0.55 * (k + 1) / steps)
        pts += [(x - w * 0.12, h * k / steps + (0.0 if k == 0 else 0.0)), (x, h * (k + 0.35) / steps), (x, h * (k + 1) / steps)]
    pts += [(0.0, h)]
    return pts


def waist(img, info):
    for s in ("left", "right", "back"):
        soft_panel(img, info[s], mix(OCHRE, info.base, 0.2), info)
    glyph("sun", CREAM, sides=("left", "right"))(img, info)
    glyph("lizard", CREAM, sides=("back",))(img, info)


def totem(P, x=0.0, y=0.0, z=0.0, variant="a", side=1, greens=True):
    """One totem pillar, its foot's centre at (x, y, z), facing +z. Variant a: the ANCESTOR (round eyes, a jaw of teeth,
    a fan of five crown fins, stepped ears). Variant b: the HORNBILL (a great beak, barred wings, a casque and three
    plumes). side is +1 or -1: which way it leans its asymmetry (the bigger ear, the vine)."""
    rnd = P.rnd
    T = lambda dx, dy, dz: (x + dx, y + dy, z + dz)
    a = variant == "a"
    # 1. the plinth, a fret running round it
    block(P, 3.9, 1.1, 3.5, T(0, 0, 0), b=0.17, base=G3, paint=band_fret(OCHRE), taper=(0.92, 0.92), moss=0.5, top_moss=1.0)
    # 2. the legs: separate fitted stones, none square to the next
    block(P, 1.6, 1.3, 2.6, T(-0.62, 1.05, 0.02), b=0.18, base=G1, turn=(0, 2, 0), jit=0.03, moss=0.25, top=False, paint=glyph("man", CORAL))
    block(P, 1.3, 1.3, 2.5, T(0.8, 1.05, -0.02), b=0.18, base=G4, turn=(0, -2.5, 0), jit=0.03, moss=0.25, top=False, paint=glyph("zig", OCHRE))
    block(P, 2.75, 1.15, 2.45, T(0.0, 2.3, 0.0), b=0.18, base=G2, taper=(1.1, 1.08), lean=(0.02 * side, 0), moss=0.5, top=False, joints=0 if a else 0.58, paint=waist)
    # the belly disc (the waist stone wears a worn ochre panel behind it)
    kind = "spiral" if a else "sun"

    def dp(img, info, kind=kind):
        info["r"] = 0.78
        disc_paint(kind)(img, info)
    slab(P, circle(0.78, 20), 0.5, T(0, 2.75, 1.0), b=0.12, base=G4, paint=dp, share="belly" + variant)
    glow_bar(P, 0, 0, T(0, 2.75, 1.45), shape=circle(0.17, 7))
    # 3. the jaw
    if a:
        block(P, 3.75, 1.4, 3.1, T(0, 3.4, 0.15), b=0.22, base=G1, taper=(0.92, 0.94), paint=mouth_paint(2.3, 0.62, 0.38), moss=0.3, top=False)
        block(P, 2.3, 0.62, 0.6, T(0, 3.78, 1.5), kind="cut", b=0.02, top=False, base=DARK)
        for k in range(5):
            tw = 0.36
            block(P, tw, 0.5 + 0.06 * (k % 2), 0.34, T(-0.92 + k * 0.46, 3.75, 1.36), b=0.07, base=CREAM, kind="add2", moss=0, cracks=0, share="tooth%d" % (k % 2), top=CREAM, foot=0.5, ppm=30)
    else:
        block(P, 3.4, 1.25, 2.9, T(0, 3.4, 0.0), b=0.2, base=G1, taper=(0.95, 0.95), paint=band_tris(RED, OCHRE), moss=0.3, top=False)
    # 4. the face
    fy = 4.75 if a else 4.6
    eyes = [(-0.8, 1.45), (0.8, 1.45)] if a else [(-0.92, 1.35), (0.92, 1.35)]
    er = 0.5 if a else 0.42
    block(P, 3.3, 2.4, 2.75, T(0, fy, 0), b=0.22, base=G2, taper=(1.07, 1.04), paint=face_paint(eyes, er, cheeks=a, back="lizard" if a else "man"), moss=0.2, top=False, cracks=0.6)
    for ex, ey in eyes:
        def ep(img, info, er=er):
            info["r"] = er
            eye_paint(img, info)
        slab(P, circle(er, 16), 0.42, T(ex * 1.04, fy + ey, 1.08), b=0.12, base=G4, paint=ep, share="eye" + variant)
        glow_bar(P, 0, 0, T(ex * 1.04, fy + ey, 1.45), shape=circle(er * 0.36, 7))
    if a:
        block(P, 0.95, 1.5, 1.0, T(0, fy + 0.15, 1.2), b=0.15, base=G4, taper=(0.5, 0.5), lean=(0, -0.2), moss=0, top=G4, cracks=0.4)
        # brows: two slabs leaning in over the eyes
        for sx in (-1, 1):
            block(P, 1.5, 0.36, 0.7, T(sx * 0.9, fy + 1.88, 1.15), b=0.1, base=G3, turn=(0, 0, sx * 11), moss=0, top=G3, share="brow", cracks=0.3)
    else:
        # the beak: a hooked profile, ochre over stone with a red root, and the casque riding on it
        beak = [(0, 0), (1.2, -0.12), (2.0, -0.55), (2.35, -1.0), (2.6, -0.5), (2.35, 0.35), (1.5, 0.95), (0, 1.15)]
        slab(P, beak, 1.2, T(0.6, fy + 0.5, 0.95), b=0.17, base=WARM, wall=OCHRE, paint=beak_paint, turn=(0, -90, 0), share="beak", ppm=P.ppm)
        casque = [(0, 0), (1.5, 0), (1.85, 0.3), (1.35, 0.8), (0, 0.62)]
        slab(P, casque, 0.62, T(0.31, fy + 1.5, 0.95), b=0.13, base=G4, paint=fin_paint(RED), turn=(0, -90, 0), share="casque", wall=mix(RED, G4, 0.3))
    # 5. the brow shelf and the headband
    by = fy + 2.36
    block(P, 4.1, 0.6, 3.5, T(0, by, 0.08), b=0.18, base=G4, turn=(3, 0, 0), moss=0.8, top_moss=1.2, cracks=0.5)
    block(P, 3.5, 0.9, 2.95, T(0, by + 0.55, 0), b=0.18, base=G1, paint=band_tris(CORAL, OCHRE) if a else band_fret(TURQ_DULL), moss=0.3, taper=(1.03, 1.03), top_moss=1.5)
    for k in (-1, 0, 1):
        glow_bar(P, 0.55, 0.18, T(k * 1.05, by + 0.55 + 0.16, 1.44), depth=0.1)
    # 6. the ears
    ey0 = fy + 0.1
    for sx in (-1, 1):
        big = 1.0 if sx == side else 0.86
        if a:
            shape = wing_shape(1.5 * big, 2.7 * big, 3)
            shape = shape if sx > 0 else [(-px_, py_) for px_, py_ in reversed(shape)]
            slab(P, shape, 0.7, T(sx * 1.5, ey0, -0.45), b=0.13, base=G3, paint=wing_paint(CORAL, "spiral"), share="ear%d%s" % (sx * side, variant), turn=(0, sx * -8, 0))
        else:
            for k in range(3):
                wl = (1.9 - k * 0.32) * big
                shape = [(0, 0), (wl, 0.06), (wl + 0.12, 0.3), (wl, 0.62), (0, 0.66)]
                shape = shape if sx > 0 else [(-px_, py_) for px_, py_ in reversed(shape)]
                slab(P, shape, 0.6, T(sx * 1.4, ey0 + 0.1 + k * 0.76, -0.5 + k * 0.05), b=0.1, base=(G3, G1, G4)[k], paint=wing_paint((OCHRE, CORAL, OCHRE)[k], "zig"), share="bar%d%d" % (sx * side, k),
                     turn=(0, sx * -10, sx * (k - 1) * -5))
    # 7. the crown
    cy = by + 1.35
    block(P, 2.7, 1.1, 2.1, T(0, cy - 0.05, -0.3), b=0.2, base=G3, taper=(0.6, 0.6), moss=0.7, top_moss=1.5)
    if a:
        fins = [(-50, 2.3, 1.25, CORAL, -0.15), (-25, 2.9, 1.4, OCHRE, 0.1), (0, 3.5, 1.6, CORAL, 0.35), (26, 2.95, 1.4, OCHRE, 0.1), (51, 2.25, 1.25, CORAL, -0.15)]
        for i, (ang, ln, wd, col, dz) in enumerate(fins):
            slab(P, kite(ln, wd), 0.6, T(math.sin(math.radians(ang)) * 0.6, cy - 0.35, 0.2 + dz), b=0.15, base=(G4, G2, G4, G1, G2)[i], paint=fin_paint(col), turn=(-6, 0, -ang), share="fin%d" % abs(i - 2))
    else:
        for i, (px_, ln, wd, dz) in enumerate(((-0.85, 2.6, 1.15, 0.0), (0.0, 3.3, 1.35, 0.3), (0.85, 2.4, 1.15, 0.0))):
            slab(P, [(-wd * 0.3, 0), (wd * 0.3, 0), (wd * 0.5, ln * 0.78), (wd * 0.2, ln), (-wd * 0.2, ln), (-wd * 0.5, ln * 0.78)], 0.6, T(px_, cy - 0.3, 0.1 + dz), b=0.14, base=(G2, G4, G1)[i],
                 paint=fin_paint((RED, OCHRE, RED)[i]), turn=(-5, 0, -px_ * 26), share="plume%d" % i)
    # 8. what has grown on it and fallen from it
    if greens:
        top_y = cy + 0.9
        vine(P, [T(side * 0.9, top_y, 0.5), T(side * 1.55, by + 0.9, 1.5), T(side * 1.75, by - 0.2, 1.66), T(side * 1.6, fy + 0.6, 1.5), T(side * 1.7, 3.6, 1.62), T(side * 1.45, 2.6, 1.48)], radius=0.09, leaves=10, size=0.7, seed=3, wander=0.45)
        vine(P, [T(-side * 1.2, by + 0.5, -1.0), T(-side * 1.8, by - 0.3, -1.3), T(-side * 1.72, fy + 0.4, -1.4), T(-side * 1.6, 3.2, -1.5)], radius=0.07, leaves=7, size=0.6, seed=5, out=(-side * 0.5, 0, -1))
        for k, (fx, fz, s, yaw) in enumerate(((-1.9 * side, 1.3, 1.0, -30 * side), (-1.5 * side, 1.75, 0.8, 10), (2.0 * side, -1.2, 0.9, 150 * side), (-2.05 * side, 0.6, 0.7, -70 * side))):
            for j in range(3):
                leaf(P, T(fx, 0.0, fz), s * (0.8 + 0.2 * j), turn=(22 + j * 16, yaw + (j - 1) * 50, 0), kind_shape="frond", tone=k + j)
        block(P, 0.75, 0.5, 0.6, T(1.95 * side, 0, 1.55), b=0.12, base=G4, turn=(0, 25, 4), moss=0.6, top_moss=1.5, ppm=30)
        block(P, 0.42, 0.3, 0.4, T(1.5 * side, 0, 2.05), b=0.09, base=G1, turn=(0, -15, 0), moss=0.4, ppm=30)
    return cy


TURQ_DULL = (92, 176, 166)


def Info0(info):
    """The same block, for a panel that is not to be inset by the bevel."""
    i = K.Info(info)
    i["bpx"] = 0
    return i


def soft_panel(img, box, colour, info):
    """Worn paint over a face: the colour inside the bevel, with patches of it gone."""
    x0, y0, x1, y1 = inset(box, info.bpx * 1.8)
    m, md = L(img)
    md.rectangle([x0, y0, x1, y1], fill=255)
    if K.CALM_NOW:
        soft(img, m, colour)
        gr = ImageChops.multiply(m, Image.linear_gradient("L").resize(img.size).point(lambda v: int(v * 0.4)))
        soft(img, gr, shade(colour, 0.80))
        return
    holes, hd = L(img)
    for _ in range(int((x1 - x0) * (y1 - y0) / (info.ppm ** 2) * 3) + 2):
        K.blob(hd, info.rnd.uniform(x0, x1), info.rnd.uniform(y0, y1), info.rnd.uniform(0.06, 0.2) * info.ppm, info.rnd.uniform(0.05, 0.14) * info.ppm, info.rnd, 255)
    keep = ImageChops.multiply(m, ImageChops.invert(holes))
    soft(img, keep, colour)
    soft(img, ImageChops.multiply(keep, ImageChops.invert(ImageChops.offset(keep, 0, int(info.ppm * 0.2)))), mix(colour, PALE, 0.3), 0.7)
    gr = ImageChops.multiply(keep, Image.linear_gradient("L").resize(img.size).point(lambda v: int(max(0, v - 120) * 0.8)))
    soft(img, gr, shade(colour, 0.7))


def build_totem(variant):
    P = K.Piece("gate_totem_" + variant, size=2048, ppm=58, seed=11 if variant == "a" else 23)
    top = totem(P, variant=variant, side=1 if variant == "a" else -1)
    P.meta.update({"ink": 0.035, "figure": (2.9, 1.6), "distance": 13.0, "look": 0.47, "fov": 56,
                   "close": [(0, 6.0, 0), (2.6, 5.2, 7.0)], "close_fov": 52})
    P.write()


PIECES = {"gate_totem_a": lambda: build_totem("a"), "gate_totem_b": lambda: build_totem("b")}

if __name__ == "__main__":
    if sys.argv[1] == "sheet":
        K.sheet(sys.argv[2], sys.argv[3], sys.argv[4] if len(sys.argv) > 4 else "")
    elif sys.argv[1] == "compare":
        K.compare(sys.argv[2], sys.argv[3], sys.argv[4])
    else:
        sys.modules["gate_build"] = sys.modules["__main__"]
        import gate_portal as GP
        PIECES.update({"gate_portal": GP.build_portal, "gate_stair": GP.build_stair, "gate_guardians": GP.build_guardians})
        PIECES[sys.argv[1]]()
