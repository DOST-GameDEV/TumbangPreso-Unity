"""Paints the mode card's window art, one picture per HOME queue choice.

RANKED   a gold trophy on a red sunburst, Zack in front with Phaister and Sean
CASUAL   (Hero Strike) a persimmon burst cut by a bolt, Amihan and Nemu (owner, 2026-10-21: no Paete here)
CLASSIC  the street game itself: the owner's own painted lata and tsinelas on a
         chalk circle under banderitas, with three barangay players

⚠️ THE PEOPLE ARE THE GAME'S MODEL RENDERS (Resources/UI/portraits), never drawn
(docs/HOME_SCREEN_ANIMATION_METHOD.md 2). Only the backdrops are painted, as exact
shapes in smooth glossy colour (owner, 2026-10-21: no knit texture here, shiny and glossy). Portraits are
320 px and are only ever scaled DOWN here.

Usage: python -I tools/build_mode_art.py   (after build_lobby_art.py)
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(HERE, 'tools'))
import build_lobby_art as B  # noqa: E402
import audit_edges  # noqa: E402

REPO = os.path.dirname(os.path.dirname(HERE))
PORTRAITS = os.path.join(REPO, 'Assets', 'TumbangPreso', 'Resources', 'UI', 'portraits')
S = B.S
W_UNITS, H_UNITS, R_UNITS = 370, 143.5, 0   # her grey window is sharp cornered
WINDOW_AT = (1501, 651.5)   # the board's art window (canva page 68)
W, H = int(W_UNITS * S), int(H_UNITS * S)
RENDERS = os.path.join(HERE, 'renders')
C = B.C
hexc = B.hexc


def canvas():
    lay = B.Layer(W + 2 * B.PAD, H + 2 * B.PAD)
    return lay


def rays(lay, cx, cy, n, a, b, twist=0.0):
    ang = np.arctan2(lay.yy - cy, lay.xx - cx) + twist
    ph = np.mod(ang / (2 * np.pi) * n, 1.0)
    r = np.hypot(lay.xx - cx, lay.yy - cy)
    # half-period distance in pixels at this radius, for an analytic stripe edge
    d = (np.abs(ph - 0.5) - 0.25) * (2 * np.pi * np.maximum(r, 1) / n)
    t = np.clip(0.5 + d, 0, 1)[..., None]
    return a * t + b * (1 - t)


def fill(lay, sdf, colour, key=None, amp=None, shine=True):
    """⚠️ GLOSSY, NOT KNIT (owner, 2026-10-21: no texture on the mode pictures, "make it shiny and
    glossy"). A flat colour runs light at the top of its shape to deep at the bottom, and a soft
    white shine sits across the shape's upper part, inset from its edge. key and amp are unused."""
    cov = B.cover(sdf)
    if colour.ndim > 1:
        lay.over(cov, colour); return
    rows = np.nonzero((cov > 0.5).any(1))[0]
    if len(rows) == 0: return
    y0, y1 = float(rows[0]), float(rows[-1] + 1)
    t = np.clip((lay.yy - y0) / max(y1 - y0, 1), 0, 1)[..., None]
    top, deep = colour + (1 - colour) * 0.30, colour * 0.74
    # light in the top half, deep in the bottom half, the shape's own colour across the middle
    lay.over(cov, colour + (top - colour) * np.clip(1 - 2 * t, 0, 1) + (deep - colour) * np.clip(2 * t - 1, 0, 1))
    hgt = y1 - y0
    if shine and hgt > 14:
        inset = max(2.5, hgt * 0.07)
        inner = B.cover(sdf + inset)
        band = np.clip((y0 + hgt * 0.46 - lay.yy) / (hgt * 0.10), 0, 1)
        k = (inner * band * 0.42)[..., None]
        lay.rgb = lay.rgb + (1 - lay.rgb) * k * lay.a[..., None]


def sheen(lay):
    """A glossy backdrop: brighter toward the top, a soft diagonal light across it."""
    P = B.PAD
    v = np.clip((lay.yy - P) / H, 0, 1)
    d = ((lay.xx - P) / W * 0.6 - (lay.yy - P) / H)
    streak = np.exp(-((d + 0.05) / 0.12) ** 2) * 0.16
    k = (np.clip(0.5 - v, 0, 1) * 0.22 + streak)[..., None]
    lay.rgb = lay.rgb + (1 - lay.rgb) * k * lay.a[..., None]


def person(lay, name, cx, top, height, flip=False, shade=0.0):
    """Composites a baked model render (renders/<name>.png, HandmadeLobbyAuthor): the figure's top at
    `top` px, `height` px tall for the whole figure (the window cuts it at the waist), centred on cx."""
    im = Image.open(os.path.join(RENDERS, name + '.png')).convert('RGBA')
    im = im.crop(im.getbbox())
    if flip: im = im.transpose(Image.FLIP_LEFT_RIGHT)
    scale = height / im.height
    assert scale <= 1.0 + 1e-6, f'{name} would be upscaled ({scale:.2f}); never enlarge a render'
    im = im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS)
    a = np.asarray(im).astype(np.float32) / 255
    stamp(lay, a, int(cx - im.width / 2), int(top), shade)


def stamp(lay, a, x0, y0, shade=0.0):
    h, w = a.shape[:2]
    X0, Y0 = max(0, x0), max(0, y0); X1, Y1 = min(lay.w, x0 + w), min(lay.h, y0 + h)
    if X1 <= X0 or Y1 <= Y0: return
    sub = a[Y0 - y0:Y1 - y0, X0 - x0:X1 - x0]
    rgb = sub[..., :3] * (1 - shade) + np.array([0.18, 0.02, 0.06]) * shade
    al = sub[..., 3:4]
    lay.rgb[Y0:Y1, X0:X1] = rgb * al + lay.rgb[Y0:Y1, X0:X1] * (1 - al)
    lay.a[Y0:Y1, X0:X1] = al[..., 0] + lay.a[Y0:Y1, X0:X1] * (1 - al[..., 0])


def ground_shadow(lay, cx, cy, rx, ry, strength=0.35):
    d = np.hypot((lay.xx - cx) / rx, (lay.yy - cy) / ry)
    k = np.clip(1 - d, 0, 1) ** 0.6 * strength
    lay.rgb = lay.rgb * (1 - k[..., None]) + np.array([0.12, 0.01, 0.04]) * k[..., None] * lay.a[..., None]


def window_mask(lay):
    return B.cover(B.rrect_sdf(lay.xx, lay.yy, B.PAD, B.PAD, W, H, R_UNITS * S))


def finish(lay, name):
    # ⚠️ AN INSET, NOT A CUT: the picture darkens toward the window's edge (maroon, 7 px), so it
    # reads as set into the card and no light shirt or trim is ever left touching the border.
    sd = B.rrect_sdf(lay.xx, lay.yy, B.PAD, B.PAD, W, H, R_UNITS * S)
    k = (np.exp(np.clip(sd, -60, 0) / 4.5) * 0.62)[..., None]
    lay.rgb = lay.rgb * (1 - k) + np.array([0.18, 0.01, 0.06]) * k * lay.a[..., None]
    m = window_mask(lay)
    lay.rgb *= m[..., None]; lay.a *= m
    path = B.save(lay, name, WINDOW_AT, 'mode card window art')
    return path


# ---------------------------------------------------------------- shapes

def trophy_sdf(lay, cx, top, s):
    """A cup with two loop handles, a stem and a two-step base, s = cup width in px."""
    xx, yy = lay.xx, lay.yy
    cup = B.poly_sdf(xx, yy, B._shrink([(cx - s / 2, top), (cx + s / 2, top), (cx + s * 0.18, top + s * 0.62), (cx - s * 0.18, top + s * 0.62)], s * 0.05)) - s * 0.05
    ring_l = np.abs(np.hypot(xx - (cx - s * 0.48), yy - (top + s * 0.2)) - s * 0.16) - s * 0.045
    ring_r = np.abs(np.hypot(xx - (cx + s * 0.48), yy - (top + s * 0.2)) - s * 0.16) - s * 0.045
    stem = B.rrect_sdf(xx, yy, cx - s * 0.07, top + s * 0.55, s * 0.14, s * 0.3, s * 0.02)
    base1 = B.rrect_sdf(xx, yy, cx - s * 0.24, top + s * 0.82, s * 0.48, s * 0.1, s * 0.03)
    base2 = B.rrect_sdf(xx, yy, cx - s * 0.32, top + s * 0.9, s * 0.64, s * 0.12, s * 0.04)
    return np.minimum.reduce([cup, ring_l, ring_r, stem, base1, base2])


def bolt_sdf(lay, pts, r):
    return B.poly_sdf(lay.xx, lay.yy, B._shrink(pts, r)) - r


# ---------------------------------------------------------------- the three pictures
#
# ⚠️ ONE COMPOSITION FOR ALL THREE: the mode's object big on the LEFT third, the cast grouped on
# the RIGHT, busts cut by the window's bottom edge. A first pass put the cast in the middle and
# Zack's head covered the trophy (it read as a halo); the object has to own its own space.

CAST_BOTTOM = 1.06   # busts run off the window's bottom edge


def cast(lay, people):
    """people: (render, x as fraction of W, top as fraction of H, height as fraction of H, flip, shade),
    back to front. Heights over 1 are deliberate: the window cuts the figures at the waist so the
    faces, not the feet, fill it."""
    P = B.PAD
    for name, x, top, hgt, flip, shade in people:
        person(lay, name, P + W * x, P + H * top, H * hgt, flip, shade)


def backdrop(lay, colour_a, colour_b, cx, cy, n, key, twist=0.0):
    fill(lay, np.full(lay.xx.shape, -10.0, np.float32), rays(lay, cx, cy, n, colour_a, colour_b, twist))
    sheen(lay)


def ranked():
    lay = canvas(); P = B.PAD
    cx = P + W * 0.22
    backdrop(lay, hexc('A8182C'), hexc('86101F'), cx, P + H * 0.55, 20, 'rk-bg')
    glow = np.clip(1 - np.hypot(lay.xx - cx, lay.yy - (P + H * 0.45)) / (H * 0.6), 0, 1) ** 1.8
    lay.rgb = lay.rgb + (hexc('F5B521') - lay.rgb) * (glow[..., None] * 0.35)
    s = H * 0.66
    top = P + H * 0.16
    t = trophy_sdf(lay, cx, top, s)
    fill(lay, t, hexc('D9781A'))
    lit = B.poly_sdf(lay.xx, lay.yy, B._shrink([(cx - s / 2, top), (cx + s * 0.1, top), (cx - s * 0.04, top + s * 0.62), (cx - s * 0.18, top + s * 0.62)], s * 0.05)) - s * 0.05
    fill(lay, np.maximum(t, lit), hexc('F5B521'))
    # a star on the cup
    star = [(cx + np.cos(np.radians(a)) * (s * (0.11 if i % 2 == 0 else 0.045)), top + s * 0.26 + np.sin(np.radians(a)) * (s * (0.11 if i % 2 == 0 else 0.045)))
            for i, a in enumerate(range(-90, 270, 36))]
    fill(lay, B.poly_sdf(lay.xx, lay.yy, star), hexc('A8182C'))
    cast(lay, [('phaister', 0.50, 0.16, 0.92, False, 0.16), ('sean', 0.86, 0.20, 0.88, False, 0.16), ('zack', 0.68, 0.10, 0.98, False, 0.0)])
    return finish(lay, 'mode_art_ranked')


def casual():
    lay = canvas(); P = B.PAD
    cx = P + W * 0.22
    backdrop(lay, hexc('F08A3C'), hexc('E2702E'), cx, P + H * 0.5, 16, 'cs-bg', 0.1)
    # a proper six-point bolt, tilted, centred in the left third
    u = H * 0.86
    pts = [(0.62, 0.0), (0.18, 0.56), (0.47, 0.56), (0.30, 1.0), (0.82, 0.38), (0.53, 0.38), (0.72, 0.0)]
    bolt = [(cx - u * 0.45 + px * u * 0.9, P + H * 0.07 + py * u) for px, py in pts]
    fill(lay, bolt_sdf(lay, bolt, 5) - 7, hexc('8A2A12'), shine=False)
    fill(lay, bolt_sdf(lay, bolt, 5), hexc('F5E04A'))
    cast(lay, [('nemu', 0.57, 0.16, 0.92, False, 0.12), ('amihan', 0.80, 0.08, 1.0, False, 0.0)])
    return finish(lay, 'mode_art_casual')


def classic():
    lay = canvas(); P = B.PAD
    sky = np.clip((lay.yy - P) / H, 0, 1)[..., None]
    fill(lay, np.full(lay.xx.shape, -10.0, np.float32), hexc('F5B521') * (1 - sky) + hexc('EE8B3A') * sky)
    sheen(lay)
    # banderitas: ONE string sagging across, flags hanging point-down from it
    def yat(u): return P + H * (0.08 + 0.16 * 4 * u * (1 - u))
    xs = np.linspace(-0.02, 1.02, 200)
    string = np.full(lay.xx.shape, np.inf, np.float32)
    for a, b in zip(xs[:-1], xs[1:]):
        string = np.minimum(string, B.capsule_sdf(lay.xx, lay.yy, P + W * a, yat(a), P + W * b, yat(b), 1.6))
    fill(lay, string, hexc('5E2618'), shine=False)
    cols = ['A31A24', '469678', 'FCD39F', '56071D']
    n = 13
    for i in range(n):
        u0 = (i + 0.15) / n; u1 = (i + 0.85) / n; um = (u0 + u1) / 2
        tri = [(P + W * u0, yat(u0)), (P + W * u1, yat(u1)), (P + W * um, yat(um) + H * 0.13)]
        fill(lay, B.poly_sdf(lay.xx, lay.yy, B._shrink(tri, 1.5)) - 1.5, hexc(cols[i % 4]))
    fill(lay, B.rrect_sdf(lay.xx, lay.yy, P - 40, P + H * 0.72, W + 80, H, 0), hexc('7A4B36'), shine=False)
    cx = P + W * 0.22
    ring = np.abs(np.hypot(lay.xx - cx, (lay.yy - (P + H * 0.86)) / 0.3) - W * 0.12) - 3.0
    fill(lay, ring, hexc('FCD39F'))
    item = Image.open(os.path.join(B.OUT, 'icon_item.png')).convert('RGBA')
    target = H * 0.62
    sc = min(target, item.height) / item.height
    im = item.resize((round(item.width * sc), round(item.height * sc)), Image.LANCZOS)
    a = np.asarray(im).astype(np.float32) / 255
    ground_shadow(lay, cx, P + H * 0.88, W * 0.10, H * 0.05)
    stamp(lay, a, int(cx - im.width / 2), int(P + H * 0.93 - im.height))
    cast(lay, [('mang_kanor', 0.50, 0.18, 0.88, False, 0.16), ('inday', 0.865, 0.16, 0.92, False, 0.16), ('totoy', 0.68, 0.12, 0.96, False, 0.0)])
    return finish(lay, 'mode_art_classic')


def map_tag():
    """The small maroon tag the map name sits on, so it reads over any of the three pictures."""
    w, h = 150, 34
    lay = B.Layer(int(w * S) + 2 * B.PAD, int(h * S) + 2 * B.PAD)
    d = B.rrect_sdf(lay.xx, lay.yy, B.PAD, B.PAD, w * S, h * S, 10 * S)
    lay.over(B.cover(d), np.broadcast_to(C['maroon'], (lay.h, lay.w, 3)) * B.weave(lay.h, lay.w, 'maptag', B.KNIT_DARK))
    return B.save(lay, 'map_tag', (w, h), 'nine-slice: 10 unit border')


if __name__ == '__main__':
    paths = [ranked(), casual(), classic()]
    import json
    mp = os.path.join(B.OUT, 'manifest.json')
    manifest = json.load(open(mp, encoding='utf-8'))
    manifest.update(B.MANIFEST)
    json.dump(manifest, open(mp, 'w', encoding='utf-8'), indent=1)
    bad = sum(audit_edges.audit(p) for p in paths)
    print('FAIL' if bad else 'ALL CLEAN')
    sys.exit(1 if bad else 0)
