"""Builds every HOME sticker from the owner's 2026-10-21 Canva board (TUMP, page 68).

Why the pieces are rebuilt instead of cropped: the Canva crops carry a pale rim,
stray specks and cut edges (owner, 2026-10-21). Here every edge is an exact
signed-distance shape with analytic anti-aliasing, drawn at twice the 1920x1080
layout so it is pixel-exact on a 4K screen. The owner's hand-painted knit is
printed on top from tools/weave.py, sampled only from the good tiles' interiors.

Each pressable sticker is two files: <name>_face.png and <name>_slab.png. The
slab is the extruded side; the game slides the face onto it when pressed.
Painted icons (hero, slipper and lata, cart, calendar, coin) are cut from the
export and defringed: the cream cutout rim is removed and the edge rebuilt.

Every output is audited (tools/audit_edges.py rules) and the build fails on
any fringe, speck or colour hidden under transparent pixels.

Usage: python -I tools/build_lobby_art.py
"""
import json
import zlib
import os
import sys

import cv2
import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(HERE, 'tools'))
import audit_edges  # noqa: E402

S = 2           # pixels per layout unit
PAD = 6         # transparent pixels around every sprite (mip and filter safety)
OUT = os.path.join(HERE, 'out')
WEAVE = np.load(os.path.join(HERE, 'work', 'weave.npy'))
EXPORT = os.path.join(HERE, 'canva', 'slide68@2x.png')


def hexc(h):
    h = h.lstrip('#')
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0


# ---------------------------------------------------------------- palette (sampled off page 68)
C = {
    'yellow': hexc('F0B947'), 'yellowSlabL': hexc('C2560F'), 'yellowSlabR': hexc('EF8D3B'),
    'orange': hexc('F19910'), 'orangeSlabL': hexc('B4470C'), 'orangeSlabR': hexc('EE8B3E'),
    'teal': hexc('469678'), 'tealSlabL': hexc('2F5E5E'), 'tealSlabR': hexc('2D8054'),
    'maroon': hexc('56071D'), 'maroonSlabL': hexc('2E0310'), 'maroonSlabR': hexc('420516'),
    'queue': hexc('4A0618'),
    'peach': hexc('FCD39F'), 'peachDeep': hexc('E9B57D'),
    'playA': hexc('A5B93D'), 'playB': hexc('80B04A'), 'playSlabL': hexc('3B7426'), 'playSlabR': hexc('4F9030'),
    'lime': hexc('A8B63E'), 'limeSlabL': hexc('5E7A2A'), 'limeSlabR': hexc('70902E'), 'limeInk': hexc('155E14'),
    'red': hexc('A31A24'), 'redSlabL': hexc('5E0A14'), 'redSlabR': hexc('7A111B'),
    'xRed': hexc('B43024'), 'xRedSlabL': hexc('6E1512'), 'xRedSlabR': hexc('8A1F18'), 'xInk': hexc('76151A'),
    'tealFillA': hexc('6FC5B8'), 'tealFillB': hexc('499C7D'),
    'honey': hexc('F5B521'), 'honeyRim': hexc('E8873A'),
    'chev': hexc('E9D3B0'),
}


# ---------------------------------------------------------------- geometry (all in pixels)

def grid(w, h):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    return xx + 0.5, yy + 0.5


def rrect_sdf(xx, yy, x, y, w, h, r):
    """Signed distance to a rounded rectangle; r is (tl, tr, br, bl) or one number."""
    if np.isscalar(r): r = (r, r, r, r)
    cx, cy = x + w / 2, y + h / 2
    px, py = xx - cx, yy - cy
    rr = np.where(px < 0, np.where(py < 0, r[0], r[3]), np.where(py < 0, r[1], r[2])).astype(np.float32)
    qx = np.abs(px) - w / 2 + rr
    qy = np.abs(py) - h / 2 + rr
    return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - rr


def poly_sdf(xx, yy, pts):
    """Signed distance to a convex or concave polygon (negative inside)."""
    pts = np.asarray(pts, np.float32)
    d = np.full(xx.shape, np.inf, np.float32)
    inside = np.zeros(xx.shape, bool)
    n = len(pts)
    for i in range(n):
        ax, ay = pts[i]; bx, by = pts[(i + 1) % n]
        ex, ey = bx - ax, by - ay
        wx, wy = xx - ax, yy - ay
        t = np.clip((wx * ex + wy * ey) / (ex * ex + ey * ey), 0, 1)
        dx, dy = wx - ex * t, wy - ey * t
        d = np.minimum(d, dx * dx + dy * dy)
        c1 = yy >= ay; c2 = yy < by; c3 = ex * wy > ey * wx
        flip = (c1 & c2 & c3) | (~c1 & ~c2 & ~c3)
        inside ^= flip
    return np.where(inside, -1, 1) * np.sqrt(d)


def cover(sdf):
    """Analytic anti-aliasing: one pixel wide ramp centred on the edge."""
    return np.clip(0.5 - sdf, 0, 1)


def capsule_sdf(xx, yy, x0, y0, x1, y1, r):
    ex, ey = x1 - x0, y1 - y0
    wx, wy = xx - x0, yy - y0
    t = np.clip((wx * ex + wy * ey) / max(ex * ex + ey * ey, 1e-6), 0, 1)
    return np.hypot(wx - ex * t, wy - ey * t) - r


# ---------------------------------------------------------------- paint

class Layer:
    """Premultiplied RGBA canvas in layout-sized pixels."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.rgb = np.zeros((h, w, 3), np.float32)
        self.a = np.zeros((h, w), np.float32)
        self.xx, self.yy = grid(w, h)

    def over(self, cov, colour):
        colour = np.broadcast_to(colour, (self.h, self.w, 3)) if colour.ndim == 1 else colour
        cov = cov[..., None]
        self.rgb = colour * cov + self.rgb * (1 - cov)
        self.a = cov[..., 0] + self.a * (1 - cov[..., 0])

    def png(self, path):
        a = self.a
        rgb = np.where(a[..., None] > 1e-4, self.rgb / np.maximum(a[..., None], 1e-4), 0)
        # ⚠️ BLEED THE EDGE COLOUR OUTWARD under fully transparent pixels. Unity's bilinear filter
        # and mip chain average across the edge; a black (or white) colour stored under alpha 0
        # is what draws a dark or pale halo around a sprite at any scale but 1:1.
        solid = a > 0.5
        if solid.any():
            _, (iy, ix) = ndimage.distance_transform_edt(~solid, return_indices=True)
            bled = rgb[iy, ix]
            rgb = np.where((a < 0.5)[..., None], bled, rgb)
        out = np.dstack([np.clip(rgb, 0, 1), np.clip(a, 0, 1)])
        Image.fromarray((out * 255 + 0.5).astype(np.uint8), 'RGBA').save(path, optimize=True)


def weave(h, w, key, amp):
    """A crop of the knit field, different for every sticker, as a multiplier."""
    rng = np.random.default_rng(zlib.crc32(key.encode()))
    oy = rng.integers(0, WEAVE.shape[0] - h); ox = rng.integers(0, WEAVE.shape[1] - w)
    return 1 + amp * WEAVE[oy:oy + h, ox:ox + w][..., None]


def hgrad(lay, left, right, x0, x1):
    t = np.clip((lay.xx - x0) / max(x1 - x0, 1), 0, 1)[..., None]
    return left * (1 - t) + right * t


# ⚠️ ONE AMPLITUDE FOR THE KNIT, MEASURED: the good tiles vary 3.3 to 3.8 per cent in luminance.
# The dark maroon gets more so its knit is still visible at a tenth of the light tiles' value.
KNIT = 0.036
KNIT_DARK = 0.085


def knit_for(colour):
    return KNIT if colour is None or colour.mean() >= 0.3 else KNIT_DARK


# ---------------------------------------------------------------- board geometry
#
# ⚠️⚠️ EVERY NUMBER BELOW IS MEASURED OFF THE OWNER'S LABELLED BOARD (Canva TUMP page 68, exported
# 2x; tools/measure_board.py and the Canva shape layers), in 1920x1080 layout units, top-left
# origin, y down. Owner, 2026-10-21: "exact font colors, positioning and sizes". A sprite records
# its own screen origin in manifest.json, so HubHome places it where the board has it without a
# second copy of these numbers drifting out of step.

MANIFEST = {}


def save(lay, name, origin, note=''):
    """origin = layout position of the canvas's first non-pad pixel."""
    path = os.path.join(OUT, name + '.png')
    lay.png(path)
    MANIFEST[name] = {'origin': [round(origin[0] - PAD / S, 2), round(origin[1] - PAD / S, 2)],
                      'size': [lay.w / S, lay.h / S], 'px': [lay.w, lay.h], 'note': note}
    return path


class Piece:
    """A canvas covering layout rect (x0, y0)-(x1, y1); shapes are given in layout units."""

    def __init__(self, x0, y0, x1, y1):
        self.x0, self.y0 = x0, y0
        self.W = int(np.ceil((x1 - x0) * S)) + 2 * PAD
        self.H = int(np.ceil((y1 - y0) * S)) + 2 * PAD

    def layer(self):
        return Layer(self.W, self.H)

    def px(self, x, y):
        return PAD + (x - self.x0) * S, PAD + (y - self.y0) * S

    def rrect(self, lay, rect, r):
        x, y, w, h = rect
        px, py = self.px(x, y)
        r = tuple(v * S for v in r) if not np.isscalar(r) else r * S
        return rrect_sdf(lay.xx, lay.yy, px, py, w * S, h * S, r)

    def poly(self, lay, pts, round_r=0.0):
        pp = [self.px(x, y) for x, y in pts]
        if round_r: return poly_sdf(lay.xx, lay.yy, _shrink(pp, round_r * S)) - round_r * S
        return poly_sdf(lay.xx, lay.yy, pp)

    def capsule(self, lay, a, b, r):
        (ax, ay), (bx, by) = self.px(*a), self.px(*b)
        return capsule_sdf(lay.xx, lay.yy, ax, ay, bx, by, r * S)

    def hgrad(self, lay, left, right, x_from, x_to):
        a, _ = self.px(x_from, 0); b, _ = self.px(x_to, 0)
        return hgrad(lay, left, right, a, b)


def knitted(lay, colour, key, amp=None):
    col = np.broadcast_to(colour, (lay.h, lay.w, 3)) if colour.ndim == 1 else colour
    if amp is None: amp = knit_for(colour if colour.ndim == 1 else colour.reshape(-1, 3).mean(0))
    return col * weave(lay.h, lay.w, key, amp)


def union(*rects):
    return (min(r[0] for r in rects), min(r[1] for r in rects),
            max(r[0] + r[2] for r in rects), max(r[1] + r[3] for r in rects))


def flat(lay, colour):
    return np.broadcast_to(colour, (lay.h, lay.w, 3))


def tile(name, face, face_r, face_col, slab, slab_r, slab_l, slab_r_col):
    """One of the board's good tiles: a knit face over a bigger knit slab with the Canva gradient."""
    pc = Piece(*union(face, slab))
    sl = pc.layer()
    sl.over(cover(pc.rrect(sl, slab, slab_r)), knitted(sl, pc.hgrad(sl, slab_l, slab_r_col, slab[0], slab[0] + slab[2]), name + 'slab', KNIT))
    save(sl, name + '_slab', (pc.x0, pc.y0))
    fl = pc.layer()
    fl.over(cover(pc.rrect(fl, face, face_r)), knitted(fl, face_col, name + 'face'))
    save(fl, name + '_face', (pc.x0, pc.y0))


def plate(name, face, r, colour, depth=(4, 6), slab_col=None):
    """An unfinished board piece finished in the tiles' manner: knit face plus a short extrusion
    down and right. The face keeps the board's exact rect; the slab only adds depth below it."""
    slab = [face[0] + depth[0], face[1] + depth[1], face[2], face[3]]
    pc = Piece(*union(face, slab))
    deep = slab_col if slab_col is not None else hexc('2E0310')
    sl = pc.layer()
    mid = [face[0] + depth[0] / 2, face[1] + depth[1] / 2, face[2], face[3]]
    sl.over(cover(np.minimum(pc.rrect(sl, slab, r), pc.rrect(sl, mid, r))), knitted(sl, deep, name + 'slab', KNIT_DARK))
    save(sl, name + '_slab', (pc.x0, pc.y0))
    fl = pc.layer()
    fl.over(cover(pc.rrect(fl, face, r)), knitted(fl, colour, name + 'face'))
    save(fl, name + '_face', (pc.x0, pc.y0))


def inlay(name, rect, r, colour, amp=0.0):
    """A flat printed inlay (wells, fields, tracks). ⚠️ NO KNIT ON PEACH: printed on the light
    peach it read as stains, not cloth (owner, 2026-10-21); the board's peach is flat."""
    pc = Piece(rect[0], rect[1], rect[0] + rect[2], rect[1] + rect[3])
    lay = pc.layer()
    lay.over(cover(pc.rrect(lay, rect, r)), knitted(lay, colour, name, amp))
    save(lay, name, (pc.x0, pc.y0))


def stripes(lay, a, b, period, angle_deg, x0=0):
    """Diagonal two-colour stripes with analytic edges."""
    th = np.radians(angle_deg)
    u = (lay.xx - x0) * np.cos(th) + lay.yy * np.sin(th)
    ph = np.mod(u, period) / period
    d = (np.abs(ph - 0.5) - 0.25) * period
    t = np.clip(0.5 + d, 0, 1)[..., None]
    return a * t + b * (1 - t)


def build_board():
    # ---- left column: the four good tiles (face rects from the export, slab rects and gradients
    # from the Canva shape layers)
    tile('tile_hero', [26, 449, 167, 141.5], 21, hexc('F0B947'), [26.2, 450.1, 193.3, 161.3], 43.6, hexc('BA490D'), hexc('EB8B33'))
    tile('tile_item', [26.5, 638.5, 166.5, 141], 21, hexc('F0B847'), [28.7, 638.3, 193.3, 161.3], 43.6, hexc('B44109'), hexc('EB8B33'))
    tile('tile_shop', [24.5, 831, 224, 203.5], 27.8, hexc('469678'), [25.3, 830.7, 253.4, 229.3], 42.5, hexc('325970'), hexc('2D8054'))
    tile('tile_task', [309, 906.5, 133.5, 132.5], 20.5, hexc('F19910'), [312.1, 908.8, 152.8, 151.2], 44.1, hexc('A64C13'), hexc('F48D4B'))

    # ---- PLAY: one rounded slab in the deep green, the striped face on top of it, ending in the
    # board's 45 degree bevel at the bottom right (the floor rises from y 1018 at x 1873)
    rect, r = [1471, 895, 430, 160], 23
    pc = Piece(*union(rect))
    sl = pc.layer(); sl.over(cover(pc.rrect(sl, rect, r)), knitted(sl, hexc('46842A'), 'playfloor', KNIT))
    save(sl, 'play_slab', (pc.x0, pc.y0))
    fl = pc.layer()
    shape = pc.rrect(fl, rect, r)
    fx, fy = pc.px(1873, 1018)
    below = fl.yy - fy
    bevel = ((fl.xx - fx) + (fl.yy - fy)) / np.sqrt(2)
    face_sdf = np.maximum(shape, np.maximum(below, bevel))
    fl.over(cover(face_sdf), knitted(fl, stripes(fl, hexc('A5B93D'), hexc('80B04A'), 96 * S, 40), 'playface', KNIT))
    save(fl, 'play_face', (pc.x0, pc.y0))

    # ---- mode card: the board's rounded top (r 52); the bottom keeps a small radius so it never
    # reads as a cut edge above PLAY
    plate('mode_card', [1474, 587.5, 421.5, 289.5], (52, 52, 10, 10), hexc('54071C'), (4, 6))
    inlay('art_window', [1501, 651.5, 370, 143.5], 3, hexc('2E0310'))

    # ---- top left
    plate('avatar_frame', [20, 20, 126, 126], 23.4, hexc('56071D'))
    inlay('avatar_well', [33.5, 34, 99.5, 99.5], 17.8, hexc('FCD39F'))
    plate('name_plate', [164.5, 18.5, 299.5, 127], 19.4, hexc('56071D'))
    inlay('xp_track', [190, 97, 245, 34], 6.5, hexc('FCD39F'))
    pc = Piece(190, 97, 435, 131); lay = pc.layer()
    lay.over(cover(pc.rrect(lay, [190, 97, 245, 34], 6.5)), knitted(lay, stripes(lay, hexc('6FC5B8'), hexc('499C7D'), 34 * S, 45), 'xpfill', KNIT))
    save(lay, 'xp_fill', (pc.x0, pc.y0), 'Image.Type Filled, horizontal, origin left')
    # level hexagon (flat top), honey on an orange rim, measured box 178,39.5 44.5x40.5
    pc = Piece(178, 39.5, 222.5, 80); lay = pc.layer()
    cx, cy = 200.25, 59.75

    def hexpts(R, sy=0.91):
        return [(cx + R * np.cos(np.radians(a)), cy + R * sy * np.sin(np.radians(a))) for a in range(0, 360, 60)]
    lay.over(cover(pc.poly(lay, hexpts(22.25), 3.0)), knitted(lay, hexc('E8873A'), 'hexrim', KNIT))
    lay.over(cover(pc.poly(lay, hexpts(16.5), 2.5)), knitted(lay, hexc('FBC14A'), 'hex', KNIT))
    save(lay, 'glyph_level', (pc.x0, pc.y0))

    # ---- top right
    plate('currency_bar', [1490, 29.5, 305.5, 83.5], 12, hexc('56071D'))
    inlay('currency_field', [1521.7, 41.7, 185.3, 57.5], 12, hexc('FCD39F'))
    plate('plus_button', [1717.5, 42, 66.5, 57.5], 9, hexc('A8B63E'), (2, 4), hexc('6E8A2C'))
    pc = Piece(1734.6, 57.2, 1765.3, 87.9); lay = pc.layer(); c = (1749.95, 72.55)
    d = np.minimum(pc.capsule(lay, (c[0] - 11.5, c[1]), (c[0] + 11.5, c[1]), 4.4), pc.capsule(lay, (c[0], c[1] - 11.5), (c[0], c[1] + 11.5), 4.4))
    lay.over(cover(d), flat(lay, hexc('0F5A13'))); save(lay, 'glyph_plus', (pc.x0, pc.y0))
    plate('menu_button', [1809, 30, 96, 83.5], 13, hexc('A31A24'), (3, 5), hexc('5E0A14'))
    pc = Piece(1830, 46, 1884, 96.5); lay = pc.layer()
    d = np.minimum.reduce([pc.rrect(lay, [1830, y, 54, 11], 4.5) for y in (46, 65.5, 85.5)])
    lay.over(cover(d), flat(lay, hexc('FCD39F'))); save(lay, 'glyph_menu', (pc.x0, pc.y0))

    # ---- queue plate: trapezoid hanging off the top edge (753..1158 at the top, 808..1110.5 at
    # y 103), bottom corners rounded; drawn 20 units past the screen edge so no top edge shows
    pc = Piece(742, -20, 1169, 110)
    for part, col, dy in (('slab', hexc('2E0310'), 6), ('face', hexc('4A0618'), 0)):
        lay = pc.layer()
        pts = [(753 - 10.6, -20 + dy), (1158 + 10.6, -20 + dy), (1110.5, 103 + dy), (808, 103 + dy)]
        lay.over(cover(pc.poly(lay, pts, 6)), knitted(lay, col, 'queue' + part, KNIT_DARK))
        save(lay, 'queue_plate_' + part, (pc.x0, pc.y0), 'top 20 units sit off screen')
    plate('cancel_button', [1056, 27, 49.5, 47], 7, hexc('B43024'), (2, 3), hexc('6E1512'))
    pc = Piece(1064, 34, 1098, 68); lay = pc.layer(); c = (1081, 51); k = 9.5
    d = np.minimum(pc.capsule(lay, (c[0] - k, c[1] - k), (c[0] + k, c[1] + k), 4.6), pc.capsule(lay, (c[0] - k, c[1] + k), (c[0] + k, c[1] - k), 4.6))
    lay.over(cover(d), flat(lay, hexc('76151A'))); save(lay, 'glyph_x', (pc.x0, pc.y0))

    # ---- chevron on the mode card (Canva rect 1837.7,812.7 33.9x49.5, #E1D2B7)
    pc = Piece(1837.7, 812.7, 1871.6, 862.2); lay = pc.layer()
    a, m, b = (1843.5, 818.5), (1865.5, 837.45), (1843.5, 856.4)
    d = np.minimum(pc.capsule(lay, a, m, 5.2), pc.capsule(lay, m, b, 5.2))
    lay.over(cover(d), flat(lay, hexc('E1D2B7'))); save(lay, 'glyph_chevron', (pc.x0, pc.y0))

    # ---- PLAY's sparks (HubSparks): knit dots and four-point stars in the board's own colours
    for name, colour in (('peach', 'FCD39F'), ('lime', 'A5B93D')):
        pc = Piece(0, 0, 32, 32); lay = pc.layer()
        lay.over(cover(pc.rrect(lay, [2, 2, 28, 28], 14)), knitted(lay, hexc(colour), 'dot' + name, KNIT))
        save(lay, 'spark_dot_' + name, (0, 0), 'particle')
    for name, colour in (('honey', 'F5B521'), ('peach', 'FCD39F')):
        pc = Piece(0, 0, 32, 32); lay = pc.layer()
        pts = []
        for i in range(8):
            a = np.radians(i * 45 - 90); r = 15 if i % 2 == 0 else 5.5
            pts.append((16 + r * np.cos(a), 16 + r * np.sin(a)))
        lay.over(cover(pc.poly(lay, pts, 1.2)), knitted(lay, hexc(colour), 'star' + name, KNIT))
        save(lay, 'spark_star_' + name, (0, 0), 'particle')


def _shrink(pts, r):
    """Move each vertex of a convex polygon inward by r along its angle bisector (for rounding)."""
    pts = np.asarray(pts, np.float64); n = len(pts); out = []
    for i in range(n):
        p, a, b = pts[i], pts[i - 1], pts[(i + 1) % n]
        u = (a - p) / np.linalg.norm(a - p); v = (b - p) / np.linalg.norm(b - p)
        bis = u + v; bis /= np.linalg.norm(bis)
        half = np.arccos(np.clip(np.dot(u, v), -1, 1)) / 2
        out.append(p + bis * r / np.sin(half))
    return out


# ---------------------------------------------------------------- painted icons (defringed)

ICONS = {
    # name: (bbox on the no-text 2x export (x, y, w, h), move to the labelled board in units).
    # The move is the Canva crop rect's position on page 68 minus its position on the no-text
    # page; the crops are the same size on both, so the icon is only translated, never scaled.
    'icon_hero': ((571, 703, 227, 315), (47.9 - 271.6, 387.0 - 342.3)),
    'icon_item': ((752, 1208, 244, 231), (33.7 - 363.9, 621.8 - 600.2)),
    'icon_shop': ((1062, 1323, 326, 370), (14.2 - 486.3, 786.9 - 646.5)),
    'icon_task': ((1536, 1818, 183, 202), (316.6 - 756.3, 875.8 - 889.7)),
    'icon_coin': ((2807, 27, 236, 221), (0.0, 0.0)),
}


def clean_icon(name, spec):
    (x, y, w, h), move = spec
    src = np.asarray(Image.open(EXPORT).convert('RGBA')).astype(np.float32) / 255
    m = 8
    crop = src[max(0, y - m):y + h + m, max(0, x - m):x + w + m].copy()
    rgb, a = crop[..., :3], crop[..., 3]

    if name == 'icon_coin':
        # the coin is fused to the currency bar on the board: keep only the gear disc
        hh, ww = a.shape; xx, yy = grid(ww, hh)
        cx, cy = 8 + 113, 8 + 106   # centroid of the gear's orange, measured
        maroon = (rgb[..., 0] < 0.5) & (rgb[..., 1] < 0.12)
        a = a * (np.hypot(xx - cx, yy - cy) < 111) * ~maroon

    solid = a > 0.5
    # largest piece plus anything big; specks go
    n, lab, st, _ = cv2.connectedComponentsWithStats(solid.astype(np.uint8), 8)
    keep = np.zeros_like(solid)
    for i in range(1, n):
        if st[i, 4] >= 400: keep |= lab == i
    solid = keep

    # ⚠️ THE CREAM CUTOUT RIM GOES (owner, 2026-10-21: no white on the UI). The board's rim is one
    # exact cream, #FFECD6, so a pixel is rim when it is near THAT colour (or a blend of it with the
    # art) AND reachable from the outside through other rim pixels within 14 px of the edge. A white
    # singlet, a can's highlight or the umbrella's grey pole (#B4B4B4) is never touched.
    cream = np.array([255, 236, 214], np.float32) / 255
    pale = solid & (np.linalg.norm(rgb - cream, axis=-1) < 0.16)
    # Repeat until stable: removing the outer rim can expose more of the same cream behind it.
    for _ in range(8):
        dist_in = ndimage.distance_transform_edt(solid)
        ring = pale & solid & (dist_in <= 14)
        seed = ring & (dist_in <= 3.0)
        if not seed.any(): break
        rim = ndimage.binary_propagation(seed, mask=ring)
        solid = solid & ~rim
    solid = ndimage.binary_opening(solid, iterations=1)
    n, lab, st, _ = cv2.connectedComponentsWithStats(solid.astype(np.uint8), 8)
    keep = np.zeros_like(solid)
    for i in range(1, n):
        if st[i, 4] >= 400: keep |= lab == i
    solid = keep

    # Smooth contour: upsample the hard mask, blur, threshold, area-downsample (no stair steps).
    hh, ww = solid.shape
    up = cv2.resize(solid.astype(np.float32), (ww * 4, hh * 4), interpolation=cv2.INTER_LINEAR)
    up = cv2.GaussianBlur(up, (0, 0), 3.0)
    alpha = cv2.resize((up > 0.5).astype(np.float32), (ww, hh), interpolation=cv2.INTER_AREA)

    # Edge colour comes from 2 px inside, so no contaminated pixel survives on the rim.
    core = ndimage.binary_erosion(alpha > 0.99, iterations=2)
    _, (iy, ix) = ndimage.distance_transform_edt(~core, return_indices=True)
    edge = (alpha > 0) & ~core
    rgb = np.where(edge[..., None], rgb[iy, ix], rgb)

    lay = Layer(ww + 2 * PAD, hh + 2 * PAD)
    lay.rgb[PAD:-PAD, PAD:-PAD] = rgb * alpha[..., None]
    lay.a[PAD:-PAD, PAD:-PAD] = alpha
    ox, oy = max(0, x - m) / S + move[0], max(0, y - m) / S + move[1]
    save(lay, name, (ox, oy), 'painted, defringed')


# ---------------------------------------------------------------- main

if __name__ == '__main__':
    # ⚠️ SUPERSEDED (owner, 2026-10-21): this redrew her tiles and retextured her slabs. The game's
    # pieces are HER originals now: extract_originals.py, refine_unfinished.py, assemble_board.py.
    # This module stays only as the drawing library build_mode_art.py uses for the window art.
    sys.exit('build_lobby_art.py no longer builds the board; run tools/assemble_board.py (see README.md)')
