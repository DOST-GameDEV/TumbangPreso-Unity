"""PAYO's STAGE STONE FAMILY, one piece at a time (2026-10-12). See tools/payo_pieces/stone_kit.py for the method.

  py -3 tools/payo_pieces/stone_build.py <piece> [...]            types the piece and paints its atlas
  py -3 tools/payo_pieces/stone_build.py sheet <piece> <N> [ref]  lays its shots into Logs/payo/pieces/stone/<piece>_vN.png

Sizes are metres. A block's origin is the centre of its flat top (y 0), which is exactly the layout's rectangle; below
the top everything is free. Front is +z.
"""
import json
import math
import random
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
import stone_kit as K                                # noqa: E402
from stone_kit import (Piece, ball, bevels, box, disc, lid, pipe, relief, shade, soft, stones, strip, tile_from, turned, weather,  # noqa: E402
                            BARK, BLACK, CORAL, CREAM, DARK, FOAM, GLOW, INK, LEAF, LEAF_L, MINT, MOSS, MOSS_L, MUD, OCHRE, PALE, RED, RICE, RICE_L, ROSE, SKIN,
                            STONES, THATCH, TIMBER, TURQ, WATER)

ROOT = Path(__file__).resolve().parents[2]
PIECES = ROOT / "Logs/payo/pieces/stone"


# ------------------------------------------------------------------ shared painting
def stone_wall(course=0.58, length=(0.9, 1.6), tone=1.0, moss=0.6, drips=0.5, damp=0.35, top=True, seed=1):
    def fn(img, info):
        rnd = random.Random(seed)
        stones(img, info["ppm"], rnd, course=course, length=length, tone=tone)
        weather(img, info["ppm"], rnd, moss=moss, drips=drips, damp=damp)
        if "corners" in info:
            bevels(img, info, top=top)
        return img
    return fn


def motif(d, kind, cx, cy, s):
    """A Cordillera motif as a filled shape, s = its half height in pixels."""
    if kind == "lizard":
        d.ellipse([cx - 0.24 * s, cy - 0.5 * s, cx + 0.24 * s, cy + 0.34 * s], fill=255)
        d.ellipse([cx - 0.18 * s, cy - 0.8 * s, cx + 0.18 * s, cy - 0.42 * s], fill=255)
        d.polygon([(cx - 0.1 * s, cy + 0.2 * s), (cx + 0.1 * s, cy + 0.2 * s), (cx + 0.26 * s, cy + 0.84 * s), (cx + 0.1 * s, cy + 0.84 * s)], fill=255)
        for sx in (-1, 1):
            for (y0, y1) in ((-0.3, -0.62), (0.14, 0.5)):
                d.line([(cx + sx * 0.16 * s, cy + y0 * s), (cx + sx * 0.56 * s, cy + y0 * s), (cx + sx * 0.56 * s, cy + y1 * s)], fill=255, width=max(3, int(0.13 * s)))
    elif kind == "sun":
        d.ellipse([cx - 0.36 * s, cy - 0.36 * s, cx + 0.36 * s, cy + 0.36 * s], fill=255)
        for k in range(10):
            a = math.tau * k / 10
            d.polygon([(cx + 0.46 * s * math.cos(a - 0.17), cy + 0.46 * s * math.sin(a - 0.17)), (cx + 0.86 * s * math.cos(a), cy + 0.86 * s * math.sin(a)), (cx + 0.46 * s * math.cos(a + 0.17), cy + 0.46 * s * math.sin(a + 0.17))], fill=255)
    elif kind == "rice":
        for (ox, lean) in ((-0.44, -0.16), (0, 0), (0.44, 0.16)):
            d.line([(cx + ox * s, cy + 0.84 * s), (cx + (ox + lean) * s, cy - 0.7 * s)], fill=255, width=max(3, int(0.09 * s)))
            for k in range(4):
                for sx in (-1, 1):
                    gx, gy = cx + (ox + lean * (0.5 + k * 0.12)) * s + sx * 0.15 * s, cy - (0.1 + k * 0.2) * s
                    d.ellipse([gx - 0.12 * s, gy - 0.075 * s, gx + 0.12 * s, gy + 0.075 * s], fill=255)
    elif kind == "ancestor":
        d.ellipse([cx - 0.22 * s, cy - 0.86 * s, cx + 0.22 * s, cy - 0.44 * s], fill=255)
        d.polygon([(cx - 0.2 * s, cy - 0.4 * s), (cx + 0.2 * s, cy - 0.4 * s), (cx + 0.14 * s, cy + 0.2 * s), (cx - 0.14 * s, cy + 0.2 * s)], fill=255)
        for sx in (-1, 1):
            d.line([(cx + sx * 0.16 * s, cy - 0.3 * s), (cx + sx * 0.54 * s, cy - 0.3 * s), (cx + sx * 0.54 * s, cy - 0.7 * s)], fill=255, width=max(3, int(0.13 * s)))
            d.line([(cx + sx * 0.1 * s, cy + 0.16 * s), (cx + sx * 0.44 * s, cy + 0.32 * s), (cx + sx * 0.44 * s, cy + 0.84 * s)], fill=255, width=max(3, int(0.14 * s)))
    elif kind == "spiral":
        pts = []
        for k in range(40):
            a = k * 0.42
            rr = 0.1 * s + 0.019 * s * k
            pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
        d.line(pts, fill=255, width=max(3, int(0.12 * s)), joint="curve")
    elif kind == "diamond":
        d.polygon([(cx, cy - 0.8 * s), (cx + 0.5 * s, cy), (cx, cy + 0.8 * s), (cx - 0.5 * s, cy)], fill=255)


def carve_band(seed=1, kinds=("lizard", "sun", "rice", "ancestor", "spiral"), paints=(OCHRE, RED, OCHRE, CORAL, TURQ), base=(112, 110, 110)):
    """A relief band round a block: woven borders with a line of turquoise inlay, a motif to a panel, old paint."""
    def fn(img, info):
        rnd = random.Random(seed)
        w, h = img.size
        d = ImageDraw.Draw(img)
        d.rectangle([0, 0, w, h], fill=base)
        for _ in range(int(w / 40)):
            x, y, r = rnd.uniform(0, w), rnd.uniform(0, h), rnd.uniform(14, 40)
            d.ellipse([x - r, y - r * 0.6, x + r, y + r * 0.6], fill=shade(base, rnd.choice((0.9, 1.08, 1.14))))
        img2 = img.filter(ImageFilter.GaussianBlur(4))
        img.paste(img2)
        if K.CALM:
            d.rectangle([0, 0, w, h], fill=shade(K.calm_tone(base), 0.80))
        bh = h * 0.13
        for (y0, flip) in ((bh * 0.6, 1), (h - bh * 0.6, -1)):
            m = Image.new("L", (w, h), 0)
            ImageDraw.Draw(m).rectangle([0, y0 - bh * 0.34, w, y0 + bh * 0.34], fill=255)
            relief(img, m, depth=2)
            ImageDraw.Draw(img).rectangle([0, y0 - bh * (0.1 if not K.CALM else 0.16), w, y0 + bh * (0.1 if not K.CALM else 0.16)], fill=TURQ)
            m = Image.new("L", (w, h), 0)
            dm = ImageDraw.Draw(m)
            step = bh * 1.5
            if K.CALM:                                     # calm: the fine zigzag border becomes one bold stripe
                a0, a1 = sorted((y0 + flip * bh * 0.6, y0 + flip * bh * 1.7))
                dm.rectangle([0, a0, w, a1], fill=255)
            for k in (range(int(w / step) + 2) if not K.CALM else ()):
                x = k * step
                dm.polygon([(x, y0 + flip * bh * 0.6), (x + step / 2, y0 + flip * bh * 1.5), (x + step, y0 + flip * bh * 0.6), (x + step, y0 + flip * bh * 0.95), (x + step / 2, y0 + flip * bh * 1.85), (x, y0 + flip * bh * 0.95)], fill=255)
            relief(img, m, paint=RED if flip > 0 else OCHRE, rnd=rnd, depth=2)
        s = h * (0.3 if not K.CALM else 0.30)
        faces = info.get("faces", {"front": (0, w)})
        k = 0
        for name, (x0, x1) in sorted(faces.items(), key=lambda kv: kv[1][0]):
            n = max(1, int((x1 - x0) / (s * 2.9))) if not K.CALM else 1
            for i in range(n):
                cx = x0 + (i + 0.5) * (x1 - x0) / n
                m = Image.new("L", (w, h), 0)
                motif(ImageDraw.Draw(m), kinds[k % len(kinds)], cx, h / 2, s)
                relief(img, m, paint=paints[k % len(paints)], rnd=rnd, depth=max(2, int(s * 0.07)))
                if i:
                    p = Image.new("L", (w, h), 0)
                    px = x0 + i * (x1 - x0) / n
                    ImageDraw.Draw(p).rectangle([px - s * 0.07, h * 0.3, px + s * 0.07, h * 0.7], fill=255)
                    relief(img, p, depth=2)
                k += 1
        if K.CALM:
            K.light(img, 1.04, 0.94)
        else:
            weather(img, info["ppm"], rnd, moss=0.25, drips=0.2, damp=0.0, lichen=0.3)
        if "corners" in info:
            bevels(img, info, top=False, alpha=0.7)
        return img
    return fn


def worn_paint(colour, seed=1, base=(156, 154, 148)):
    def fn(img, info):
        rnd = random.Random(seed)
        w, h = img.size
        d = ImageDraw.Draw(img)
        d.rectangle([0, 0, w, h], fill=colour)
        for y in range(h):
            d.line([(0, y), (w, y)], fill=shade(colour, 1.12 - 0.3 * y / max(1, h)) if not K.CALM else shade(colour, 1.05 - 0.12 * y / max(1, h)))
        if K.CALM:
            if "corners" in info:
                bevels(img, info, top=True, alpha=0.6)
            return img
        holes = Image.new("L", (w, h), 0)
        dh = ImageDraw.Draw(holes)
        for _ in range(int(w / 26)):
            x, y, r = rnd.uniform(0, w), rnd.uniform(0, h), rnd.uniform(8, 26)
            dh.ellipse([x - r, y - r * 0.5, x + r, y + r * 0.5], fill=255)
        soft(img, holes.filter(ImageFilter.GaussianBlur(2)).point(lambda v: 255 if v > 110 else 0).filter(ImageFilter.GaussianBlur(1)), base)
        if "corners" in info:
            bevels(img, info, top=True, alpha=0.6)
        return img
    return fn


def big_motif(under, which):
    """A wall painter with one large painted relief on the faces named: {face: (kind, paint)}."""
    def fn(img, info):
        under(img, info)
        r2 = random.Random(len(which) * 7 + 1)
        for name, (kind, paint) in which.items():
            if name not in info.get("faces", {}):
                continue
            x0, x1 = info["faces"][name]
            m = Image.new("L", img.size, 0)
            motif(ImageDraw.Draw(m), kind, (x0 + x1) / 2, img.size[1] * 0.5, img.size[1] * 0.36)
            relief(img, m, paint=paint, rnd=r2, depth=max(2, int(info["ppm"] * 0.07)))
        return img
    return fn


# ------------------------------------------------------------------ CALM: one big bold panel to a face
# The pilot's note: the monolith's lower tiers went almost blank from the front. So in calm mode a block's walls carry
# ONE big carved panel per side (a raised pale frame, a field of the block's one accent colour, a thick two-colour
# motif), never small repeated glyphs. Each block has its own accent pair and its own motifs, so neighbours differ.
CALM_KINDS = ["lizard", "sun", "rice", "ancestor", "spiral", "diamond", "fret", "eye", "chevrons"]
_DEEP_RED, _DEEP_TEAL = (150, 66, 56), (52, 118, 110)
# (field, motif): one accent pair per block
CALM_PAIRS = [
    (K._soft((226, 150, 150)), _DEEP_RED),            # dusty rose, deep red
    (K._soft((198, 112, 82)), (240, 228, 204)),                             # terracotta, cream
    (K._soft((150, 210, 178)), _DEEP_TEAL),                                 # mint, deep teal
    (K._soft((222, 170, 76)), _DEEP_RED),                                   # ochre, deep red
    ((236, 224, 198), K._soft((198, 112, 82))),                             # cream, terracotta
    (K._soft((86, 196, 182), 0.8, 0.04), (240, 232, 210)),                 # turquoise, cream
    ((126, 120, 112), K._soft((222, 170, 76))),                             # a dark carved recess, ochre
    ((132, 124, 116), K._soft((232, 128, 96))),                             # a dark carved recess, coral
]


def calm_motif(d, kind, cx, cy, s):
    """The calm motifs: thick, few parts, legible from 40 m. s is the half height in pixels."""
    W = lambda k: max(3, int(k * s))
    if kind == "lizard":
        d.ellipse([cx - 0.3 * s, cy - 0.5 * s, cx + 0.3 * s, cy + 0.36 * s], fill=255)
        d.ellipse([cx - 0.22 * s, cy - 0.86 * s, cx + 0.22 * s, cy - 0.4 * s], fill=255)
        d.polygon([(cx - 0.14 * s, cy + 0.2 * s), (cx + 0.14 * s, cy + 0.2 * s), (cx + 0.3 * s, cy + 0.9 * s), (cx + 0.08 * s, cy + 0.9 * s)], fill=255)
        for sx in (-1, 1):
            for (y0, y1) in ((-0.3, -0.66), (0.14, 0.52)):
                d.line([(cx + sx * 0.2 * s, cy + y0 * s), (cx + sx * 0.6 * s, cy + y0 * s), (cx + sx * 0.6 * s, cy + y1 * s)], fill=255, width=W(0.2), joint="curve")
    elif kind == "sun":
        d.ellipse([cx - 0.42 * s, cy - 0.42 * s, cx + 0.42 * s, cy + 0.42 * s], fill=255)
        for k in range(8):
            a = math.tau * (k + 0.5) / 8
            d.polygon([(cx + 0.5 * s * math.cos(a - 0.26), cy + 0.5 * s * math.sin(a - 0.26)), (cx + 0.92 * s * math.cos(a), cy + 0.92 * s * math.sin(a)), (cx + 0.5 * s * math.cos(a + 0.26), cy + 0.5 * s * math.sin(a + 0.26))], fill=255)
    elif kind == "rice":
        for (ox, lean) in ((-0.42, -0.14), (0, 0), (0.42, 0.14)):
            d.line([(cx + ox * s, cy + 0.88 * s), (cx + (ox + lean) * s, cy - 0.6 * s)], fill=255, width=W(0.14))
            for k in range(3):
                for sx in (-1, 1):
                    gx, gy = cx + (ox + lean * (0.55 + k * 0.15)) * s + sx * 0.14 * s, cy - (0.1 + k * 0.28) * s
                    d.ellipse([gx - 0.15 * s, gy - 0.1 * s, gx + 0.15 * s, gy + 0.1 * s], fill=255)
    elif kind == "ancestor":
        d.ellipse([cx - 0.26 * s, cy - 0.9 * s, cx + 0.26 * s, cy - 0.4 * s], fill=255)
        d.polygon([(cx - 0.26 * s, cy - 0.38 * s), (cx + 0.26 * s, cy - 0.38 * s), (cx + 0.18 * s, cy + 0.24 * s), (cx - 0.18 * s, cy + 0.24 * s)], fill=255)
        for sx in (-1, 1):
            d.line([(cx + sx * 0.2 * s, cy - 0.26 * s), (cx + sx * 0.6 * s, cy - 0.26 * s), (cx + sx * 0.6 * s, cy - 0.74 * s)], fill=255, width=W(0.2), joint="curve")
            d.line([(cx + sx * 0.12 * s, cy + 0.16 * s), (cx + sx * 0.48 * s, cy + 0.34 * s), (cx + sx * 0.48 * s, cy + 0.88 * s)], fill=255, width=W(0.2), joint="curve")
    elif kind == "spiral":
        pts = []
        for k in range(30):
            a = k * 0.42
            rr = 0.1 * s + 0.026 * s * k
            pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
        d.line(pts, fill=255, width=W(0.2), joint="curve")
    elif kind == "diamond":
        d.polygon([(cx, cy - 0.9 * s), (cx + 0.62 * s, cy), (cx, cy + 0.9 * s), (cx - 0.62 * s, cy)], fill=255)
        d.polygon([(cx, cy - 0.56 * s), (cx + 0.38 * s, cy), (cx, cy + 0.56 * s), (cx - 0.38 * s, cy)], fill=0)
        d.polygon([(cx, cy - 0.26 * s), (cx + 0.18 * s, cy), (cx, cy + 0.26 * s), (cx - 0.18 * s, cy)], fill=255)
    elif kind == "fret":
        pts = [(-0.62, 0.7), (-0.62, -0.7), (0.62, -0.7), (0.62, 0.3), (-0.18, 0.3), (-0.18, -0.24), (0.2, -0.24)]
        d.line([(cx + x * s, cy + y * s) for (x, y) in pts], fill=255, width=W(0.22), joint="curve")
    elif kind == "eye":
        d.ellipse([cx - 0.8 * s, cy - 0.5 * s, cx + 0.8 * s, cy + 0.5 * s], fill=255)
        d.ellipse([cx - 0.58 * s, cy - 0.3 * s, cx + 0.58 * s, cy + 0.3 * s], fill=0)
        d.ellipse([cx - 0.24 * s, cy - 0.24 * s, cx + 0.24 * s, cy + 0.24 * s], fill=255)
    elif kind == "chevrons":
        for k in range(3):
            y = cy + (-0.62 + k * 0.56) * s
            d.polygon([(cx - 0.62 * s, y), (cx, y + 0.42 * s), (cx + 0.62 * s, y), (cx + 0.62 * s, y + 0.24 * s), (cx, y + 0.66 * s), (cx - 0.62 * s, y + 0.24 * s)], fill=255)
    else:
        motif(d, kind, cx, cy, s)


def calm_panel(img, info, cx, cy, pw, ph, kind, field, ink):
    """One carved panel centred at (cx, cy), pw x ph pixels: a raised pale frame, the field, a thick motif."""
    w, h = img.size
    fr = max(3, int(min(pw, ph) * 0.09))
    outer = Image.new("L", (w, h), 0)
    ImageDraw.Draw(outer).rounded_rectangle([cx - pw / 2, cy - ph / 2, cx + pw / 2, cy + ph / 2], radius=fr * 1.4, fill=255)
    inner = Image.new("L", (w, h), 0)
    ImageDraw.Draw(inner).rounded_rectangle([cx - pw / 2 + fr, cy - ph / 2 + fr, cx + pw / 2 - fr, cy + ph / 2 - fr], radius=fr * 0.8, fill=255)
    ob, ib = outer.filter(ImageFilter.GaussianBlur(1.0)), inner.filter(ImageFilter.GaussianBlur(1.0))
    soft(img, ob, K.mix3(K.mix3(K.calm_tone((150, 148, 144)), K.PALE, 0.5), field, 0.10))   # a carved stone frame, a little lighter
    K.hset(img, outer.filter(ImageFilter.GaussianBlur(max(1.2, fr * 0.3))), 0.022)
    # the field: its own soft gradient, lighter at the top
    grad = Image.new("RGB", (w, h))
    gd = ImageDraw.Draw(grad)
    for y in range(h):
        t = min(1.0, max(0.0, (y - (cy - ph / 2)) / max(1.0, ph)))
        gd.line([(0, y), (w, y)], fill=shade(field, 1.04 - 0.10 * t))
    img.paste(grad, (0, 0), ib)
    K.hset(img, inner.filter(ImageFilter.GaussianBlur(max(1.5, fr * 0.4))), 0.004)
    m = Image.new("L", (w, h), 0)
    calm_motif(ImageDraw.Draw(m), kind, cx, cy + ph * 0.02, min(ph - 2 * fr, (pw - 2 * fr) * 1.15) * 0.40)
    m = ImageChops.multiply(m, inner)
    mb = m.filter(ImageFilter.GaussianBlur(1.0))
    soft(img, mb, ink)
    hm = Image.new("L", (w, h), 0)
    hm.paste(m.filter(ImageFilter.GaussianBlur(max(1.5, fr * 0.35))))
    H = K.HEIGHT.get(id(img))
    if H is not None:                                    # the motif stands proud of the field
        a = np.asarray(H, np.float32) + np.asarray(hm, np.float32) / 255.0 * (0.022 / K.HU)
        H.paste(Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)))


def panels(under, which, max_h=4.2):
    """A wall painter (calm): `under` first, then one panel on each face named: {face: (kind, field, ink, v)}, v the
    panel's centre down the face (0 top, 1 foot), or (v, max_h) to fit a panel under a ledge."""
    def fn(img, info):
        under(img, info)
        w, h = img.size
        ppm = info["ppm"]
        for name, (kind, field, ink, v) in which.items():
            if name not in info.get("faces", {}):
                continue
            v, mh = v if isinstance(v, tuple) else (v, max_h)
            x0, x1 = info["faces"][name]
            fw = x1 - x0
            ph = min(h * 0.70, mh * ppm, fw * 0.95)
            pw = min(fw * 0.62, ph * 0.9)
            cy = min(max(h * v, ph / 2 + min(0.12 * h, 0.45 * ppm)), h - ph / 2 - min(0.06 * h, 0.3 * ppm))
            calm_panel(img, info, (x0 + x1) / 2, cy, pw, ph, kind, field, ink)
        return img
    return fn


def face(P, x, y, z, s=1.0, yaw=0, fins=CORAL, crown=(OCHRE, TURQ)):
    """A carved guardian face standing out of a wall whose surface is at z (it looks along +z; `yaw` turns it about y):
    heavy brow, ring eyes with turquoise inlay, a broad nose, a mouth with teeth, painted fins and a crown of leaves."""
    def at(dx, dy, dz):
        a = math.radians(yaw)
        return (x + dx * math.cos(a) + dz * math.sin(a), y + dy, z - dx * math.sin(a) + dz * math.cos(a))
    t = (0, yaw, 0)
    box(P, (1.5 * s, 0.27 * s, 0.36 * s), 0.14 * s, at(0, 0.62 * s, 0.14 * s), sw=PALE, turn=t)
    box(P, (0.3 * s, 0.62 * s, 0.34 * s), 0.14 * s, at(0, -0.1 * s, 0.14 * s), sw=PALE, turn=t, taper=(1.3, 0.7))
    for sx in (-1, 1):
        ball(P, (0.46 * s, 0.46 * s, 0.2 * s), at(sx * 0.86 * s, 0.02 * s, 0.06 * s), PALE, turn=t)
        ball(P, (0.3 * s, 0.3 * s, 0.1 * s), at(sx * 0.86 * s, 0.02 * s, 0.2 * s), DARK, turn=t)
        ball(P, (0.13 * s, 0.13 * s, 0.07 * s), at(sx * 0.86 * s, 0.02 * s, 0.29 * s), TURQ, turn=t)
        box(P, (0.2 * s, 0.86 * s, 0.22 * s), 0.1 * s, at(sx * 1.72 * s, -0.2 * s, 0.06 * s), sw=fins, turn=t, taper=(0.55, 1.0))
    box(P, (0.98 * s, 0.24 * s, 0.14 * s), 0.07 * s, at(0, -1.02 * s, 0.03 * s), sw=DARK, turn=t)
    for k in range(4):
        box(P, (0.17 * s, 0.17 * s, 0.14 * s), 0.06 * s, at((-0.62 + k * 0.41) * s, -0.96 * s, 0.1 * s), sw=PALE, turn=t)
    for k, dx in enumerate((-0.92, -0.46, 0.0, 0.46, 0.92)):
        hgt = (0.34, 0.42, 0.52, 0.42, 0.34)[k] * s
        box(P, (0.2 * s, hgt, 0.16 * s), 0.07 * s, at(dx * s, 0.9 * s + hgt, 0.08 * s), sw=crown[k % 2], turn=t, taper=(1.0, 0.35))


def root(P, rnd, start, drop, r=0.16, colour=BARK, wander=0.5, leaves=True):
    pts, radii = [], []
    x, y, z = start
    n = 11
    ph, amp = rnd.uniform(0, 6.28), rnd.uniform(0.5, 1.0) * wander
    ax = 0 if abs(z) > abs(x) else 2                     # it wanders ALONG the face it hangs on
    for k in range(n):
        t = k / (n - 1)
        off = amp * math.sin(ph + t * 5.0) * (0.3 + t)
        pts.append((x + (off if ax == 0 else 0.0), y, z + (off if ax == 2 else 0.0)))
        radii.append(r * (1.25 - 1.0 * t))
        y -= drop / (n - 1)
    pipe(P, pts, radii, colour)
    if leaves:
        for k in (2, 4, 5):
            ball(P, (0.26, 0.1, 0.2), (pts[k][0] + rnd.uniform(-0.2, 0.2), pts[k][1], pts[k][2] + 0.12), LEAF_L if k % 2 else LEAF, turn=(rnd.uniform(-30, 30), rnd.uniform(0, 90), 0), seg=8, rings=5)


def moss_pad(P, x, y, z, s=0.5, squash=0.3):
    ball(P, (s, s * squash, s * 0.8), (x, y, z), MOSS, seg=10, rings=6)
    ball(P, (s * 0.55, s * squash * 0.8, s * 0.5), (x + s * 0.3, y + s * squash * 0.4, z + s * 0.1), MOSS_L, seg=8, rings=5)


def fern(P, rnd, x, y, z, s=1.0, n=6, colour=(LEAF, LEAF_L)):
    """A clump of big graphic leaves: each a bent paddle of three rows."""
    for k in range(n):
        a = math.tau * k / n + rnd.uniform(-0.3, 0.3)
        L = s * rnd.uniform(0.9, 1.4)
        rows = []
        for t in (0.0, 0.35, 0.7, 1.0):
            r = L * t
            h = s * (0.75 * math.sin(math.pi * min(1.0, t * 0.9)) + 0.15 * t) - (0.35 * s * t * t)
            w = s * 0.34 * math.sin(math.pi * (0.12 + 0.88 * t) ** 0.8) + 0.02
            c = np.array((x + r * math.cos(a), y + h, z + r * math.sin(a)))
            side = np.array((-math.sin(a), 0, math.cos(a)))
            rows.append([c - side * w, c + np.array((0, 0.05 * s, 0)), c + side * w])
        strip(P, rows, sw=colour[k % 2])


# ------------------------------------------------------------------ 1. the monolith
def monolith():
    """The stage's corner block. Top: a flat square, 6 x 6 m exactly, at y 0 (the origin is its centre). Below the top
    everything is its own: a pale cap, a groove, a relief band, a painted course, a guardian face, three tapering
    tiers with pale ledges, a sun disc on its left, roots and moss. 13.6 m of it is modelled."""
    P = Piece("monolith", size=2048, ppm=78, seed=11)
    rnd = P.rnd

    def top(img, info):
        r2 = random.Random(5)
        stones(img, info["ppm"], r2, course=1.5, length=(1.3, 2.1), tone=1.08)
        w, h = img.size
        m = info["margin"]
        weather(img, info["ppm"], r2, moss=0.0, drips=0.0, damp=0.0, lichen=0.7)
        ring = Image.new("L", (w, h), 0)
        d = ImageDraw.Draw(ring)
        d.rectangle([0, 0, w, h], fill=255)
        d.rectangle([m + 9, m + 9, w - m - 9, h - m - 9], fill=0)
        soft(img, ring.filter(ImageFilter.GaussianBlur(2.5)), PALE, 0.9)                      # the pale worn rim
        g = Image.new("L", (w, h), 0)
        d = ImageDraw.Draw(g)
        o = m + 0.62 * info["ppm"]
        d.rectangle([o, o, w - o, h - o], outline=255, width=max(3, int(info["ppm"] * 0.08)))
        soft(img, g.filter(ImageFilter.GaussianBlur(0.8 if not K.CALM else 2.0)), INK if not K.CALM else shade(K.calm_tone((150, 148, 144)), 0.86), 0.8)   # a cut groove inside it
        K.hset(img, g.filter(ImageFilter.GaussianBlur(2.0)), -0.03)
        soft(img, ImageChops.offset(g, -2, -2).filter(ImageFilter.GaussianBlur(0.8)), PALE, 0.45)
        for (cx, cy) in ((o, o), (w - o, o), (w - o, h - o), (o, h - o)):                      # inlaid corner diamonds, the paint half gone
            mk = Image.new("L", (w, h), 0)
            motif(ImageDraw.Draw(mk), "diamond", cx, cy, info["ppm"] * 0.36)
            relief(img, mk, paint=OCHRE, rnd=r2, depth=2)
        for _ in (range(9) if not K.CALM else ()):                                            # moss creeping in from the rim
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

    lid(P, 3.0, 3.0, 0.0, 0.3, top, ppm=84)
    box(P, (3.0, 0.46, 3.0), 0.2, (0, -0.52, 0), paint=stone_wall(course=0.46, length=(1.3, 2.2), tone=1.14, moss=0.5, drips=0.2, damp=0.0, seed=2))
    box(P, (2.7, 0.22, 2.7), 0.06, (0, -1.1, 0), sw=DARK)
    box(P, (2.84, 0.62, 2.84), 0.12, (0, -1.82, 0), paint=carve_band(seed=3), ppm=84)
    box(P, (2.93, 0.17, 2.93), 0.09, (0, -2.52, 0), paint=worn_paint(OCHRE, seed=4))
    box(P, (2.62, 1.75, 2.62), 0.34, (0, -4.3, 0), paint=stone_wall(seed=6, moss=0.4, course=0.72, length=(1.1, 1.9)), taper=(0.9, 1.0), bulge=0.05)
    for (sx, sz) in ((1, 1), (-1, 1), (-1, -1), (1, -1)):                               # corbels under the painted course, one a corner
        box(P, (0.42, 0.5, 0.42), 0.16, (sx * 2.5, -3.1, sz * 2.5), sw=PALE, taper=(0.5, 1.0))
        ball(P, (0.2, 0.2, 0.2), (sx * 2.62, -3.72, sz * 2.62), TURQ, seg=8, rings=6)
    box(P, (2.66, 0.17, 2.66), 0.1, (0, -6.08, 0), sw=PALE)
    box(P, (2.3, 1.9, 2.3), 0.34, (0.12, -8.0, -0.1), paint=big_motif(stone_wall(seed=7, damp=0.5, moss=0.7, course=0.72, length=(1.1, 1.9)), {"right": ("lizard", OCHRE), "back": ("ancestor", RED), "left": ("rice", OCHRE)}), taper=(0.86, 1.0), turn=(0, 7, 0), lean=(-0.12, 0.08))
    box(P, (2.2, 0.17, 2.2), 0.1, (0.12, -9.9, -0.1), sw=PALE, turn=(0, 7, 0))
    box(P, (1.95, 1.95, 1.95), 0.34, (0.0, -11.75, 0.0), paint=stone_wall(seed=8, damp=0.7, moss=0.8, drips=0.8, tone=0.9, course=0.72, length=(1.1, 1.9)), taper=(1.12, 1.0), turn=(0, -5, 0))
    face(P, 0.0, -4.35, 2.6, s=1.0)
    # the back: a shrine niche cut into the top tier, an offering jar in it, a pale lintel and sill
    box(P, (0.8, 0.95, 0.5), 0.1, (0.0, -4.2, -2.5), sw=DARK, cut=True)
    box(P, (1.1, 0.16, 0.3), 0.08, (0.0, -3.1, -2.62), sw=PALE)
    box(P, (1.0, 0.13, 0.36), 0.08, (0.0, -5.22, -2.6), sw=PALE)
    turned(P, [(0.2, 0.0), (0.4, 0.2), (0.44, 0.55), (0.3, 0.86), (0.2, 0.95), (0.25, 1.05)], (0.0, -5.12, -2.36), sw=CORAL)
    box(P, (0.42, 0.06, 0.06), 0.03, (0.0, -4.62, -2.02), sw=TURQ)

    def sun(img, info):
        w, h = img.size
        d = ImageDraw.Draw(img)
        d.rectangle([0, 0, w, h], fill=(150, 148, 144))
        r2 = random.Random(9)
        for (rr, col) in (((0.98, PALE), (0.86, (128, 126, 124)), (0.5, PALE), (0.42, (158, 156, 150))) if not K.CALM else
                          ((0.98, K.PALE), (0.86, K.calm_tone((128, 126, 124))), (0.5, K.PALE), (0.42, K.calm_tone((158, 156, 150))))):
            d.ellipse([w / 2 - rr * w / 2, h / 2 - rr * h / 2, w / 2 + rr * w / 2, h / 2 + rr * h / 2], fill=col)
        m = Image.new("L", (w, h), 0)
        dm = ImageDraw.Draw(m)
        for k in range(12):
            a = math.tau * k / 12
            dm.polygon([(w / 2 + 0.27 * w * math.cos(a - 0.14), h / 2 + 0.27 * h * math.sin(a - 0.14)), (w / 2 + 0.41 * w * math.cos(a), h / 2 + 0.41 * h * math.sin(a)),
                        (w / 2 + 0.27 * w * math.cos(a + 0.14), h / 2 + 0.27 * h * math.sin(a + 0.14))], fill=255)
        relief(img, m, paint=OCHRE, rnd=r2, depth=2)
        m = Image.new("L", (w, h), 0)
        motif(ImageDraw.Draw(m), "spiral", w / 2, h / 2, h * 0.19)
        relief(img, m, paint=RED, rnd=r2, depth=2)
        ImageDraw.Draw(img).ellipse([w / 2 - 0.445 * w, h / 2 - 0.445 * h, w / 2 + 0.445 * w, h / 2 + 0.445 * h], outline=TURQ, width=max(2, int(w * (0.018 if not K.CALM else 0.028))))
        return img

    disc(P, 1.25, 0.36, (-2.62, -4.25, 0.0), sun, rim=0.2, ppm=110, turn=(90, -90, 0))
    for (x, z, col, drop) in ((2.74, 1.6, BARK, 10.5), (2.76, -1.2, LEAF, 7.0), (1.7, -2.76, BARK, 8.5), (-1.9, -2.74, LEAF, 5.0), (-2.74, -1.9, BARK, 11.0), (2.5, 2.74, LEAF, 4.2)):
        root(P, rnd, (x, -0.95, z), drop, r=rnd.uniform(0.09, 0.14), colour=col, wander=0.5)
    for (x, y, z, s) in ((2.2, -5.95, 2.3, 0.6), (-2.3, -5.95, -2.0, 0.5), (1.9, -9.78, -2.0, 0.6), (-2.0, -9.78, 2.1, 0.7), (-1.1, -3.46, 2.86, 0.34), (2.5, -0.06, -2.4, 0.0)):
        if s:
            moss_pad(P, x, y, z, s)
    fern(P, rnd, 2.45, -5.9, -2.3, 0.9, n=5)
    fern(P, rnd, -2.2, -9.7, -2.1, 1.0, n=6)
    P.meta.update({"size": "top 6.0 x 6.0 m exactly, 13.6 m modelled", "pivot": "the centre of the top (y 0)", "ground": -13.7, "ink": 0.035, "width": 6.4,
                   "eye_z": 1.7, "look_z": -4.2, "distance": 13.5, "figure": (1.6, -1.2), "figure_z": 0.0, "fov": 62,
                   "close": [(0.0, -3.9, 2.7), (4.2, -2.4, 9.0)]})
    return P


import stone_family as FAM                           # noqa: E402
_me = sys.modules[__name__]
BUILDERS = {"monolith": monolith, "core": lambda: FAM.core(_me), "tower_head": lambda: FAM.tower_head(_me)}
LOCKED = ("monolith", "core", "tower_head")          # approved by the owner 2026-10-12: never rebuilt (backup: ArtSource/payo/pieces-approved-2026-10-12/stone)
import stone_blocks as BLK                           # noqa: E402
BUILDERS.update(BLK.builders(_me))


# ------------------------------------------------------------------ sheets
def font(n, bold=True):
    try:
        return ImageFont.truetype("arialbd.ttf" if bold else "arial.ttf", n)
    except OSError:
        return ImageFont.load_default()


def sheet(name, version, ref=None, notes=""):
    shots = PIECES / "shots"
    stats = json.loads((K.OUT / "stats.json").read_text()).get(name, {})
    meta = json.loads((K.OUT / "parts" / (name + ".json")).read_text())
    tiles = [("front", "front three-quarter, from 1.7 m"), ("back", "back three-quarter"), ("close", "its best detail"), ("above", "from above")]
    extra = [(k, k.replace("_", " ")) for k in meta.get("extra", {})]
    tiles += extra
    W, H = 1100, 800
    cols = 2
    rows = (len(tiles) + 1) // 2
    refw = 0
    rim = None
    if ref and Path(ref).exists():
        rim = Image.open(ref).convert("RGB")
        refw = 900
    out = Image.new("RGB", (cols * W + refw, rows * H + 70), (22, 26, 32))
    d = ImageDraw.Draw(out)
    d.text((16, 10), "%s  v%s" % (name.upper(), version), font=font(34), fill=(240, 236, 220))
    d.text((16 + 30 * len(name) + 150, 22), "%s   %d triangles, %d KB, %d open edges   %s" % (meta.get("size", ""), stats.get("triangles", 0), stats.get("kb", 0), stats.get("open_edges", -1), notes),
           font=font(20, False), fill=(190, 206, 204))
    for i, (key, cap) in enumerate(tiles):
        p = shots / ("%s_%s.png" % (name, key))
        if not p.exists():
            continue
        im = Image.open(p).convert("RGB").resize((W, H), Image.LANCZOS)
        x, y = (i % cols) * W, 70 + (i // cols) * H
        out.paste(im, (x, y))
        d.rectangle([x + 8, y + 8, x + 16 + 11 * len(cap), y + 36], fill=(22, 26, 32))
        d.text((x + 14, y + 11), cap, font=font(18, False), fill=(236, 236, 228))
    atlas = K.OUT / "parts" / (name + "-atlas.png")
    if refw and atlas.exists():
        at = Image.open(atlas).convert("RGB").resize((700, 700), Image.LANCZOS)
        bg = Image.new("RGB", (760, 760), (128, 128, 128))
        bg.paste(at, (30, 30))
        out.paste(bg, (cols * W + 70, 70 + rows * H - 800))
        d.text((cols * W + 74, 70 + rows * H - 830), "its painted atlas on neutral grey: the stone is grey", font=font(18, False), fill=(236, 236, 228))
    if rim is not None:
        k = min(refw / rim.size[0], (rows * H) / rim.size[1])
        rim = rim.resize((int(rim.size[0] * k), int(rim.size[1] * k)), Image.LANCZOS)
        out.paste(rim, (cols * W + (refw - rim.size[0]) // 2, 70 + 40))
        d.text((cols * W + 14, 76), "the owner's reference, for finish", font=font(18, False), fill=(236, 236, 228))
    PIECES.mkdir(parents=True, exist_ok=True)
    path = PIECES / ("%s_v%s.png" % (name, version))
    out.save(path)
    print("wrote", path)


if __name__ == "__main__":
    words = [a for i, a in enumerate(sys.argv[1:]) if not a.startswith("--") and sys.argv[i] != "--out"]
    if words and words[0] == "sheet":
        sheet(words[1], words[2], words[3] if len(words) > 3 else None)
    elif words and words[0] == "all":
        im = Image.open(PIECES / "shots" / "five_all.png").convert("RGB")
        out = Image.new("RGB", (im.size[0], im.size[1] + 60), (22, 26, 32))
        out.paste(im, (0, 60))
        ImageDraw.Draw(out).text((16, 12), "PAYO: the five hero pieces at one scale (each with a 1.75 m figure)", font=font(30), fill=(240, 236, 220))
        out.save(PIECES / ("five_together_v%s.png" % words[1]))
    else:
        for name in words:
            if name in LOCKED and "--unlock" not in sys.argv and not K.CALM:
                raise SystemExit(name + " is LOCKED (approved); it is not rebuilt")
            BUILDERS[name]().write()
