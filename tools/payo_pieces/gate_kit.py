"""PAYO, ONE PIECE AT A TIME: the GATE family's kit (2026-10-12).

A gate piece is ONE object made of many separate chunky stones. Every stone is a bevelled block or a bevelled
silhouette slab with ITS OWN painted strip of the atlas (pale bevel on every edge, a darker foot, cracks, chips, moss
hanging from its top), and carving is painted on the flat faces AND stood proud as modelled slabs.

  1. py -3 tools/payo_pieces/gate_build.py <piece>                   parts/<piece>.glb, <piece>-atlas.png, <piece>.json
  2. blender -b --python tools/payo_pieces/gate_weld.py -- <piece>   Logs/payo/pieces/build/gate/<piece>.glb
  3. blender -b --python tools/payo_pieces/gate_review.py -- <piece> four shots with an ink line and a 1.75 m figure
  4. py -3 tools/payo_pieces/gate_build.py sheet <piece> <version>   Logs/payo/pieces/gate/<piece>_vN.png

Conventions: metres, y up, +z is the piece's FRONT (glTF). UVs: v down. Part kinds: "add" (unioned into the skin),
"cut" (taken out of it), "add2" (unioned after the cuts), "glow" (the turquoise inlay: its own mesh and material),
"loose" (leaves, rope: joined to the skin's mesh, not unioned).
"""
import json
import math
import random
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import hand_companion_kit as hk                      # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Logs/payo/pieces/build/gate"
SS = 2                                               # every region is painted at twice its size and brought down
NSTRENGTH = 3.0                                      # calm mode: the normal map's relief exaggerated for the toon light

# The owner, 2026-10-12: "the texture all reads as too green and teal. the reference i gave only had cool blue LIGHTING.
# the stones were the old grey". So stone is painted a natural neutral to slightly warm GREY; the cool tone is the light's.
STONES = [(128, 125, 120), (146, 142, 135), (112, 110, 108), (160, 155, 146), (136, 130, 121), (120, 117, 112), (168, 164, 156), (104, 102, 101), (150, 138, 122), (126, 120, 114)]
PALE, INK, DARK = (232, 227, 214), (44, 41, 40), (84, 80, 77)
OCHRE, RED, CORAL, TURQ, ROSE, MINT, CREAM = (226, 166, 60), (188, 70, 52), (234, 124, 88), (64, 214, 196), (226, 150, 150), (150, 216, 178), (238, 228, 200)
MOSS, MOSS_L, LEAF, LEAF_L, BARK = (84, 130, 62), (150, 190, 92), (56, 138, 80), (136, 204, 98), (104, 76, 58)
THATCH, TIMBER, GLOW, BLACK = (204, 164, 88), (122, 82, 56), (150, 255, 236), (40, 38, 46)
SHADOW = (70, 66, 66)

# ------------------------------------------------------------------ CALM mode (Logs/payo/pieces/CALM_STYLE.md)
# py -3 tools/payo_pieces/gate_build.py <piece> --calm   writes Logs/payo/pieces/calm/gate/ (never build/gate)
# The same pieces, the same random stream and so the same geometry and UV layout; only the paint changes: one soft
# warm colour per stone with one gradient, faint joints on a few large stones, no pitting, cracks, chips, streaks or
# moss tongues, a few big soft muted moss shapes on top edges, carving flat and bold, the bevel a subtle lighter edge.
# A height map is painted beside the colour (bevels round off, joints are grooves, carving stands proud or sinks,
# moss is raised) and baked to a normal map. The default (no flag) path is untouched.
CALM = "--calm" in sys.argv
CALM_NOW = False                                     # True only while a calm painter is running
HEIGHT = {}                                          # id(painted image) -> its height image (L, 128 = 0 m)
HU = 0.002                                           # metres per height level
if "--out" in sys.argv:
    OUT = Path(sys.argv[sys.argv.index("--out") + 1])
elif CALM:
    OUT = ROOT / "Logs/payo/pieces/calm/gate"
CMOSS = (126, 144, 92)
if CALM:
    def _soft(c, sat=0.80, warm=0.06):
        lum = 0.30 * c[0] + 0.55 * c[1] + 0.15 * c[2]
        c = [lum + (v - lum) * sat for v in c]
        return tuple(int(round(v + (w - v) * warm)) for v, w in zip(c, (245, 230, 204)))
    OCHRE, RED, CORAL, ROSE, MINT, CREAM = (_soft(OCHRE), _soft(RED, 0.85), _soft(CORAL), _soft(ROSE), _soft(MINT), _soft(CREAM, 1.0, 0.03))


MATCHED = "--matched" in sys.argv                     # calm roll-out: every stone on the ONE shared calm stone colour
CALM_STONE = (165, 156, 140)                         # the shared calm stone (CALM_STYLE.md); the monolith's value


def calm_stone_tone(c):
    """Stones drawn close together in value and warmed: soft warm grey to pale sand, never a patchwork.
    --matched (the gate roll-out after the pilot): the shared calm stone's hue exactly, only a quarter of the stone's
    own value difference kept, so the gate never runs pinker than the monolith. Without the flag (the approved
    gate_portal pilot) the tone is as it was."""
    if MATCHED and tuple(c) in GREYS:                # stone only: cream teeth, horn and paint keep their own tone
        lum = 0.30 * c[0] + 0.55 * c[1] + 0.15 * c[2]
        k = max(0.90, min(1.08, 1 + (lum - 138) / 138 * 0.25))
        return tuple(int(min(255, v * k)) for v in CALM_STONE)
    m = (136, 132, 126)
    c = [mv + (v - mv) * 0.45 for v, mv in zip(c, m)]
    return tuple(int(min(255, v * 0.82 + w)) for v, w in zip(c, (49, 45, 36)))


def hset(img, mask, metres, at=(0, 0)):
    """Paint a height (metres from the face) through a mask into this image's height canvas, when there is one."""
    H = HEIGHT.get(id(img))
    if H is not None:
        H.paste(int(max(0, min(255, 128 + metres / HU))), (at[0], at[1], at[0] + mask.size[0], at[1] + mask.size[1]), mask)


def shade(c, f):
    return tuple(max(0, min(255, int(v * f))) for v in c)


def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


# ------------------------------------------------------------------ the piece
class Piece:
    def __init__(self, name, size=2048, ppm=40, seed=1):
        self.name, self.S, self.ppm = name, size, ppm
        self.img = Image.new("RGB", (size, size), (140, 137, 131))
        self.x, self.y, self.row = 4, 4, 0
        self.shelves = []
        self.parts, self.shared, self.meta = [], {}, {}
        self.rnd = random.Random(seed)
        self.himg = Image.new("L", (size, size), 128) if CALM else None
        self.rects = []

    def alloc(self, w, h):
        """Shelves: a region goes on the first shelf about its own height that still has room, else on a new one."""
        w, h = int(max(8, w)), int(max(8, h))
        assert w + 8 <= self.S, "%s: a region %d px wide does not fit (lower its ppm)" % (self.name, w)
        for s in self.shelves:
            if s[1] >= h and h >= s[1] * 0.6 and s[2] + w + 4 <= self.S:
                r = (s[2], s[0], s[2] + w, s[0] + h)
                s[2] += w + 6
                return r
        assert self.y + h + 4 <= self.S, "%s: the atlas is full at part %d" % (self.name, len(self.parts))
        self.shelves.append([self.y, h, 4 + w + 6])
        r = (4, self.y, 4 + w, self.y + h)
        self.y += h + 6
        self.row = 0
        return r

    def paint(self, r, fn):
        """fn(img) draws on an image SS times the region's size."""
        w, h = r[2] - r[0], r[3] - r[1]
        if CALM:
            return self.paint_calm(r, fn)
        big = Image.new("RGB", (w * SS, h * SS), (150, 147, 141))
        big = fn(big) or big
        sub = big.resize((w, h), Image.LANCZOS)
        self.img.paste(sub.resize((w + 4, h + 4)), (r[0] - 2, r[1] - 2))
        self.img.paste(sub, (r[0], r[1]))

    def paint_calm(self, r, fn):
        """The approved painter runs first on a throwaway image, only so the random stream (which the geometry also
        draws from) advances exactly as it did; then the calm painter paints colour and height from the same state."""
        global CALM_NOW
        w, h = r[2] - r[0], r[3] - r[1]
        s0 = self.rnd.getstate()
        fn(Image.new("RGB", (w * SS, h * SS), (150, 147, 141)))
        s1 = self.rnd.getstate()
        self.rnd.setstate(s0)
        big = Image.new("RGB", (w * SS, h * SS), (150, 147, 141))
        hb = Image.new("L", big.size, 128)
        HEIGHT[id(big)] = hb
        CALM_NOW = True
        try:
            out = fn(big)
        finally:
            CALM_NOW = False
            HEIGHT.pop(id(big), None)
        big = out or big
        self.rnd.setstate(s1)
        sub = big.resize((w, h), Image.LANCZOS)
        self.img.paste(sub.resize((w + 4, h + 4)), (r[0] - 2, r[1] - 2))
        self.img.paste(sub, (r[0], r[1]))
        hs = hb.resize((w, h), Image.LANCZOS)
        self.himg.paste(hs, (r[0], r[1]))
        self.rects.append((r[0], r[1], r[2], r[3]))

    def swatch(self, colour, key=None, edge=None):
        """A small patch for small parts: the colour with a lighter top, and a pale band along its top for bevels."""
        key = ("sw", key or colour)
        if key not in self.shared:
            r = self.alloc(40, 40)
            edge = edge or mix(colour, PALE, 0.55 if not CALM else 0.25)

            def fn(img):
                d = ImageDraw.Draw(img)
                n = img.size[1]
                for y in range(n):
                    t = y / (n - 1.0)
                    d.line([(0, y), (img.size[0], y)], fill=edge if t < 0.2 else shade(colour, 1.12 - 0.34 * (t - 0.2) / 0.8))
            self.paint(r, fn)
            self.shared[key] = r
        return self.shared[key]

    def add(self, mesh, uv, kind="add"):
        self.parts.append((mesh, np.asarray(uv, np.float32), kind))
        return len(self.parts) - 1

    def mirror(self, start, end=None):
        """The parts from `start` again on the other side of x, wearing the same paint."""
        for mesh, uv, kind in list(self.parts[start:end]):
            self.parts.append((hk.mirror(mesh), uv.copy(), kind))

    def tris(self):
        return sum(len(m[2]) for m, _, _ in self.parts)

    def write(self, out=None):
        out = out or OUT
        m = hk.Model(self.name)
        m.folder = Path(out) / "parts"
        m.texture = self.name + "-atlas.png"
        for i, (mesh, uv, kind) in enumerate(self.parts):
            m.add("%04d_%s" % (i, kind), [(mesh, uv)])
        m.folder.mkdir(parents=True, exist_ok=True)
        self.img.save(m.folder / (self.name + "-atlas.png"))
        path = m.write()
        self.meta["parts"] = len(self.parts)
        self.meta["atlas_used"] = round((self.y + self.row) / self.S, 2)
        (m.folder / (self.name + ".json")).write_text(json.dumps(self.meta, indent=1))
        if CALM:                                          # the atlas, its height map and the normal map beside the glb
            import calm_glb
            self.img.save(Path(out) / (self.name + "-atlas.png"))
            H = (np.asarray(self.himg, np.float32) - 128) * HU
            calm_glb.save_height(H, Path(out) / (self.name + "_height.png"))
            rects = [(a, b, c, d, self.ppm) for a, b, c, d in self.rects]
            Image.fromarray(calm_glb.height_to_normal(H, rects, strength=NSTRENGTH, blur=1.4)).save(Path(out) / (self.name + "_normal.png"))
        print("atlas used to %.0f%% of its height" % (100 * (self.y + self.row) / self.S))
        return path


# ------------------------------------------------------------------ painting
def soft(img, mask, colour, alpha=1.0):
    m = mask if alpha == 1.0 else mask.point(lambda v: int(v * alpha))
    img.paste(Image.new("RGB", img.size, colour), (0, 0), m)


def blob(d, cx, cy, rx, ry, rnd, fill, n=9, rough=0.25):
    pts = []
    for k in range(n):
        a = math.tau * k / n
        f = 1 + rnd.uniform(-rough, rough)
        pts.append((cx + rx * f * math.cos(a), cy + ry * f * math.sin(a)))
    d.polygon(pts, fill=fill)


def crack(d, x, y, ln, rnd, ppm, colour=INK, down=True):
    if CALM_NOW:
        return                                            # calm: no crack lines
    pts = [(x, y)]
    a = rnd.uniform(1.1, 2.0) if down else rnd.uniform(-0.5, 0.5)
    for _ in range(rnd.randint(3, 5)):
        a += rnd.uniform(-0.7, 0.7)
        x += math.cos(a) * ln / 4
        y += math.sin(a) * ln / 4
        pts.append((x, y))
    wd = max(2, int(ppm * 0.022))
    d.line([(px + wd, py + wd * 0.6) for px, py in pts], fill=PALE, width=wd)
    d.line(pts, fill=colour, width=wd + 1)
    if rnd.random() < 0.6:
        k = rnd.randint(1, len(pts) - 2)
        bx, by = pts[k]
        d.line([(bx, by), (bx + rnd.uniform(-1, 1) * ln * 0.25, by + rnd.uniform(0.1, 0.3) * ln)], fill=colour, width=wd)


def stone(img, box, base, rnd, ppm, bpx, joints=0.0, moss=0.4, cracks=1.0, foot=0.3, bevel=1.0, chips=1.0):
    """One face of a block, painted: the stone (or a few fitted stones when `joints` is a course height in metres),
    a lighter worn patch, a darker foot, the wide PALE bevel round every edge, cracks, chips, moss from the top."""
    if CALM_NOW:
        return calm_stone(img, box, base, rnd, ppm, bpx, joints, moss, foot, bevel)
    x0, y0, x1, y1 = [int(v) for v in box]
    w, h = x1 - x0, y1 - y0
    if w < 4 or h < 4:
        return
    sub = Image.new("RGB", (w, h), INK)
    d = ImageDraw.Draw(sub)
    cells = []
    if joints:
        rows = max(1, round(h / (joints * ppm)))
        ch = h / rows
        for r in range(rows):
            xx = 0.0
            first = True
            while xx < w - 1:
                cw = rnd.uniform(1.3, 2.4) * joints * ppm
                if first and r % 2:
                    cw *= 0.55
                first = False
                if w - (xx + cw) < joints * ppm * 0.7:
                    cw = w - xx
                cells.append((xx, r * ch, min(w, xx + cw), (r + 1) * ch))
                xx += cw
    else:
        cells.append((0, 0, w, h))
    g = max(2, ppm * 0.03) if joints else 0
    for (a, b, c, e) in cells:
        tone = shade(base if not joints else mix(base, rnd.choice(STONES), 0.55), rnd.uniform(0.94, 1.07))
        j = (lambda: rnd.uniform(-1, 1) * ppm * 0.03) if joints else (lambda: 0)
        poly = [(a + g + j(), b + g + j()), (c - g + j(), b + g + j()), (c - g + j(), e - g + j()), (a + g + j(), e - g + j())]
        d.polygon(poly, fill=tone)
        cw, chh = c - a, e - b
        blob(d, a + cw * rnd.uniform(0.3, 0.6), b + chh * rnd.uniform(0.25, 0.5), cw * rnd.uniform(0.2, 0.36), chh * rnd.uniform(0.16, 0.3), rnd, shade(tone, 1.13))
        for _ in range(rnd.randint(1, 3)):
            blob(d, a + cw * rnd.uniform(0.1, 0.9), b + chh * rnd.uniform(0.5, 0.95), cw * rnd.uniform(0.06, 0.16), chh * rnd.uniform(0.04, 0.1), rnd, shade(tone, 0.84))
        if joints:                                        # each stone's own pale top and left edge, dark foot
            e2 = max(2, ppm * 0.045)
            d.polygon([poly[0], poly[1], (poly[1][0] - e2, poly[1][1] + e2), (poly[0][0] + e2, poly[0][1] + e2)], fill=mix(tone, PALE, 0.6))
            d.polygon([poly[0], (poly[0][0] + e2, poly[0][1] + e2), (poly[3][0] + e2, poly[3][1] - e2), poly[3]], fill=mix(tone, PALE, 0.4))
            d.polygon([poly[3], (poly[3][0] + e2, poly[3][1] - e2), (poly[2][0] - e2, poly[2][1] - e2), poly[2]], fill=shade(tone, 0.78))
    for _ in range(int(w * h / (ppm * ppm) * 5)):         # pitting
        px_, py_, pr = rnd.uniform(0, w), rnd.uniform(0, h), rnd.uniform(0.012, 0.03) * ppm
        d.ellipse([px_ - pr, py_ - pr, px_ + pr, py_ + pr], fill=shade(base, 0.7))
        d.ellipse([px_ - pr + pr * 0.6, py_ - pr + pr * 0.8, px_ + pr + pr * 0.6, py_ + pr + pr * 0.8], fill=mix(base, PALE, 0.45))
        d.ellipse([px_ - pr, py_ - pr, px_ + pr, py_ + pr], fill=shade(base, 0.7))
    if foot:                                              # the shadow gathering toward the foot
        gr = Image.linear_gradient("L").resize((w, h)).point(lambda v: int(max(0, v - 70) * 1.7 * foot))
        soft(sub, gr, SHADOW)
    d = ImageDraw.Draw(sub)
    for _ in range(int(rnd.uniform(0.3, 1.0) * cracks * max(1, w * h / (ppm * ppm) / 2.5))):
        crack(d, rnd.uniform(0.1, 0.9) * w, rnd.uniform(0.05, 0.6) * h, rnd.uniform(0.3, 0.8) * ppm, rnd, ppm)
    if bevel:
        bw = bpx * 1.45 * bevel
        wob = lambda: rnd.uniform(-0.12, 0.12) * bw
        top_c, side_c, foot_c = mix(base, PALE, 0.82), mix(base, PALE, 0.5), mix(base, PALE, 0.22)

        def band(p0, p1, inward, colour):
            n = max(2, int(math.hypot(p1[0] - p0[0], p1[1] - p0[1]) / (ppm * 0.35)))
            pts = [p0, p1]
            for k in range(n, -1, -1):
                t = k / n
                pts.append((p0[0] + (p1[0] - p0[0]) * t + inward[0] * (bw + wob()), p0[1] + (p1[1] - p0[1]) * t + inward[1] * (bw + wob())))
            d.polygon(pts, fill=colour)
        band((0, h), (w, h), (0, -1), foot_c)
        band((0, 0), (0, h), (1, 0), side_c)
        band((w, 0), (w, h), (-1, 0), mix(base, PALE, 0.34))
        band((0, 0), (w, 0), (0, 1), top_c)
        lw = max(2, int(ppm * 0.02))
        cut = shade(base, 0.62)                             # the cut line inside the bevel: the face reads as a sunk panel
        d.line([(bw * 1.05, bw * 1.08), (w - bw * 1.05, bw * 1.08)], fill=cut, width=lw + 1)
        d.line([(bw * 1.05, bw * 1.08), (bw * 1.05, h - bw * 1.05)], fill=cut, width=lw)
        d.line([(w - bw * 1.05, bw * 1.08), (w - bw * 1.05, h - bw * 1.05)], fill=mix(base, PALE, 0.7), width=lw)
        d.line([(bw * 1.05, h - bw * 1.05), (w - bw * 1.05, h - bw * 1.05)], fill=mix(base, PALE, 0.7), width=lw)
        for _ in range(int(chips * rnd.uniform(0.5, 1.2) * (w + h) / ppm)):                                  # chips out of the bevel
            side = rnd.choice("tlrb")
            s = rnd.uniform(0.05, 0.13) * ppm
            if side == "t":
                cx, cy = rnd.uniform(0, w), rnd.uniform(0, bw * 0.8)
            elif side == "b":
                cx, cy = rnd.uniform(0, w), h - rnd.uniform(0, bw * 0.8)
            elif side == "l":
                cx, cy = rnd.uniform(0, bw * 0.8), rnd.uniform(0, h)
            else:
                cx, cy = w - rnd.uniform(0, bw * 0.8), rnd.uniform(0, h)
            d.polygon([(cx - s, cy - s * 0.4), (cx + s * 0.8, cy - s * 0.7), (cx + s, cy + s * 0.5), (cx - s * 0.5, cy + s * 0.8)], fill=shade(base, 0.74))
            d.polygon([(cx - s, cy - s * 0.4), (cx + s * 0.8, cy - s * 0.7), (cx + s * 0.5, cy - s * 0.2), (cx - s * 0.6, cy)], fill=shade(base, 0.58))
    if moss:
        hang(sub, rnd, ppm, moss)
    img.paste(sub, (x0, y0))


def hang(sub, rnd, ppm, amount=0.5, colour=MOSS, light=MOSS_L, reach=1.0):
    """Moss creeping down from the top edge in scalloped tongues, lit along its upper lip."""
    if CALM_NOW:
        return calm_hang(sub, rnd, ppm, amount)
    w, h = sub.size
    m = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(m)
    for _ in range(max(0, int(round(w / ppm * amount * rnd.uniform(0.6, 1.4))))):
        x = rnd.uniform(0, w)
        span = rnd.uniform(0.3, 0.9) * ppm
        for k in range(rnd.randint(2, 5)):
            xx = x + rnd.uniform(-1, 1) * span
            ww = rnd.uniform(0.08, 0.2) * ppm
            ln = rnd.uniform(0.15, 0.75) * ppm * reach
            d.rectangle([xx - ww, -2, xx + ww, max(-1, ln - ww)], fill=255)
            d.ellipse([xx - ww, ln - 2 * ww, xx + ww, ln], fill=255)
        d.rectangle([x - span, -2, x + span, rnd.uniform(0.05, 0.12) * ppm], fill=255)
    soft(sub, ImageChops.offset(m, int(ppm * 0.02), int(ppm * 0.05)), shade(colour, 0.55), 0.7)
    soft(sub, m, colour)
    lip = ImageChops.multiply(m, ImageChops.invert(ImageChops.offset(m, 0, int(max(2, ppm * 0.07)))))
    soft(sub, lip, light)
    dark = ImageChops.multiply(m, ImageChops.invert(ImageChops.offset(m, 0, -int(max(2, ppm * 0.06)))))
    soft(sub, dark, shade(colour, 0.78))


def creep(img, box, rnd, ppm, amount=0.5):
    """Moss and grass climbing from the foot."""
    if CALM_NOW:
        return                                            # calm: moss only on top edges
    x0, y0, x1, y1 = [int(v) for v in box]
    w, h = x1 - x0, y1 - y0
    if w < 4 or h < 4:
        return
    sub = img.crop((x0, y0, x1, y1)).transpose(Image.FLIP_TOP_BOTTOM)
    hang(sub, rnd, ppm, amount, reach=0.7)
    img.paste(sub.transpose(Image.FLIP_TOP_BOTTOM), (x0, y0))


def streaks(img, box, rnd, ppm, n=3, colour=SHADOW, alpha=0.4):
    """Wet streaks running down from under a lip."""
    if CALM_NOW:
        return
    x0, y0, x1, y1 = [int(v) for v in box]
    m = Image.new("L", img.size, 0)
    d = ImageDraw.Draw(m)
    for _ in range(n):
        x = rnd.uniform(x0, x1)
        ww = rnd.uniform(0.05, 0.12) * ppm
        ln = rnd.uniform(0.35, 0.9) * (y1 - y0)
        d.polygon([(x - ww, y0), (x + ww, y0), (x + ww * 0.5, y0 + ln * 0.7), (x, y0 + ln), (x - ww * 0.5, y0 + ln * 0.7)], fill=255)
    soft(img, m, colour, alpha)


def relief(img, mask, raised, depth=4, paint=None, rnd=None, worn=0.5, lit=PALE):
    """A carved shape that reads as standing proud: an ink shadow down and right, a pale edge up and left, and old
    paint on it, worn off in patches."""
    m = mask
    if CALM_NOW:                                          # calm: one flat bold colour; the relief lives in the height map
        soft(img, m, mix(paint, raised, 0.12) if paint else raised)
        hset(img, m.filter(ImageFilter.GaussianBlur(max(1, depth * 0.7))), 0.022)
        return
    soft(img, ImageChops.offset(m, depth, int(depth * 1.3)), INK, 0.85)
    soft(img, m, raised)
    soft(img, ImageChops.multiply(m, ImageChops.invert(ImageChops.offset(m, depth, depth))), lit)
    soft(img, ImageChops.multiply(m, ImageChops.invert(ImageChops.offset(m, -depth, -depth))), shade(raised, 0.72))
    if paint:
        keep = m
        if rnd and worn:
            holes = Image.new("L", img.size, 0)
            d = ImageDraw.Draw(holes)
            bb = m.getbbox() or (0, 0, 1, 1)
            for _ in range(int((bb[2] - bb[0]) * (bb[3] - bb[1]) / 5000 * worn) + 2):
                blob(d, rnd.uniform(bb[0], bb[2]), rnd.uniform(bb[1], bb[3]), rnd.uniform(8, 30), rnd.uniform(6, 20), rnd, 255)
            keep = ImageChops.multiply(m, ImageChops.invert(holes))
        inner = ImageChops.multiply(keep, ImageChops.multiply(ImageChops.offset(m, depth, depth), ImageChops.offset(m, -depth, -depth)))
        soft(img, inner, paint)


def carve(img, mask, depth=4, colour=None):
    """A shape cut INTO the stone: dark inside, a shadow under its upper lip, a pale lower lip."""
    if CALM_NOW:
        soft(img, mask, colour or shade(DARK, 0.9))
        hset(img, mask.filter(ImageFilter.GaussianBlur(max(1, depth * 0.7))), -0.03)
        return
    soft(img, ImageChops.offset(mask, depth // 2, depth), PALE, 0.9)
    soft(img, mask, colour or shade(DARK, 0.8))
    soft(img, ImageChops.multiply(mask, ImageChops.invert(ImageChops.offset(mask, depth, depth))), INK)


def L(img):
    m = Image.new("L", img.size, 0)
    return m, ImageDraw.Draw(m)


# glyphs, each drawn into a mask's ImageDraw
def g_fret(d, box, unit, wd):
    """The stepped key: a row of squared spirals."""
    x0, y0, x1, y1 = box
    h = y1 - y0
    n = max(1, int((x1 - x0) / unit))
    unit = (x1 - x0) / n
    for k in range(n):
        ox = x0 + k * unit
        u = unit
        pts = [(ox, y1), (ox, y0), (ox + u * 0.78, y0), (ox + u * 0.78, y0 + h * 0.72), (ox + u * 0.3, y0 + h * 0.72), (ox + u * 0.3, y0 + h * 0.36), (ox + u * 0.52, y0 + h * 0.36)]
        d.line(pts, fill=255, width=wd, joint="curve")
        d.line([(ox, y1), (ox + u, y1)], fill=255, width=wd)


def g_zig(d, box, n, wd):
    x0, y0, x1, y1 = box
    pts = [(x0 + (x1 - x0) * k / (2 * n), y1 if k % 2 == 0 else y0) for k in range(2 * n + 1)]
    d.line(pts, fill=255, width=wd, joint="curve")


def g_tris(d, box, n, up=True, gap=0.12):
    x0, y0, x1, y1 = box
    u = (x1 - x0) / n
    for k in range(n):
        a, b = x0 + k * u + u * gap, x0 + (k + 1) * u - u * gap
        d.polygon([(a, y1), (b, y1), ((a + b) / 2, y0)] if up else [(a, y0), (b, y0), ((a + b) / 2, y1)], fill=255)


def g_spiral(d, cx, cy, r, turns, wd, start=0.0):
    pts = []
    n = int(turns * 26)
    for k in range(n + 1):
        t = k / n
        a = start + t * turns * math.tau
        pts.append((cx + r * t * math.cos(a), cy + r * t * math.sin(a)))
    d.line(pts, fill=255, width=wd, joint="curve")


def g_rays(d, cx, cy, r0, r1, n, wide=0.5, start=0.0):
    for k in range(n):
        a = start + math.tau * k / n
        da = math.tau / n * wide / 2
        d.polygon([(cx + r0 * math.cos(a - da), cy + r0 * math.sin(a - da)), (cx + r1 * math.cos(a), cy + r1 * math.sin(a)), (cx + r0 * math.cos(a + da), cy + r0 * math.sin(a + da))], fill=255)


def g_ring(d, cx, cy, r, wd):
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=255, width=wd)


def g_man(d, cx, cy, s, wd):
    """The dancing ancestor of the rice terraces: arms up, knees out."""
    d.ellipse([cx - s * 0.16, cy - s * 0.5, cx + s * 0.16, cy - s * 0.2], fill=255)
    d.line([(cx, cy - s * 0.2), (cx, cy + s * 0.15)], fill=255, width=int(wd * 1.5))
    d.line([(cx - s * 0.36, cy - s * 0.42), (cx - s * 0.3, cy - s * 0.1), (cx + s * 0.3, cy - s * 0.1), (cx + s * 0.36, cy - s * 0.42)], fill=255, width=wd, joint="curve")
    d.line([(cx - s * 0.3, cy + s * 0.5), (cx - s * 0.32, cy + s * 0.2), (cx, cy + s * 0.12), (cx + s * 0.32, cy + s * 0.2), (cx + s * 0.3, cy + s * 0.5)], fill=255, width=wd, joint="curve")


def g_lizard(d, cx, cy, s, wd):
    d.ellipse([cx - s * 0.1, cy - s * 0.3, cx + s * 0.1, cy + s * 0.2], fill=255)
    d.ellipse([cx - s * 0.08, cy - s * 0.5, cx + s * 0.08, cy - s * 0.3], fill=255)
    d.line([(cx, cy + s * 0.15), (cx + s * 0.05, cy + s * 0.35), (cx - s * 0.08, cy + s * 0.5)], fill=255, width=wd, joint="curve")
    for sx in (-1, 1):
        d.line([(cx, cy - s * 0.18), (cx + sx * s * 0.24, cy - s * 0.2), (cx + sx * s * 0.3, cy - s * 0.38)], fill=255, width=wd, joint="curve")
        d.line([(cx, cy + s * 0.1), (cx + sx * s * 0.24, cy + s * 0.12), (cx + sx * s * 0.3, cy + s * 0.3)], fill=255, width=wd, joint="curve")


def thatch(img, box, rnd, ppm, colour=THATCH, rows=None, bpx=0):
    """Layered cut grass: rows of ragged tufts, each row lit along its top and dark where the next overhangs it."""
    if CALM_NOW:
        return calm_thatch(img, box, rnd, ppm, colour, rows)
    x0, y0, x1, y1 = [int(v) for v in box]
    w, h = x1 - x0, y1 - y0
    if w < 4 or h < 4:
        return
    sub = Image.new("RGB", (w, h), shade(colour, 0.6))
    d = ImageDraw.Draw(sub)
    rows = rows or max(2, int(h / (ppm * 0.5)))
    rh = h / rows
    for r in range(rows):
        yy = r * rh
        tone = shade(mix(colour, rnd.choice(((224, 190, 104), (176, 140, 80), (196, 170, 96))), 0.5), rnd.uniform(0.92, 1.06))
        x = -rnd.uniform(0, 0.2) * ppm
        while x < w:
            tw = rnd.uniform(0.16, 0.34) * ppm
            ln = rh * rnd.uniform(0.86, 1.12)
            t2 = shade(tone, rnd.uniform(0.9, 1.1))
            d.polygon([(x, yy - 2), (x + tw, yy - 2), (x + tw * rnd.uniform(0.75, 1.0), yy + ln), (x + tw * rnd.uniform(0.2, 0.5), yy + ln * rnd.uniform(0.88, 1.0))], fill=t2)
            d.line([(x + tw * 0.5, yy), (x + tw * 0.55, yy + ln * 0.8)], fill=shade(t2, 0.8), width=max(1, int(ppm * 0.012)))
            x += tw * 0.92
        gr = Image.new("L", (w, h), 0)
        ImageDraw.Draw(gr).rectangle([0, yy, w, yy + rh * 0.22], fill=110)
        soft(sub, gr, (244, 220, 150))
    for _ in range(int(w * h / (ppm * ppm) * 0.25)):
        blob(d, rnd.uniform(0, w), rnd.uniform(0, h), rnd.uniform(0.12, 0.3) * ppm, rnd.uniform(0.06, 0.14) * ppm, rnd, rnd.choice((MOSS, MOSS_L, shade(MOSS, 0.85))))
    gr = Image.linear_gradient("L").resize((w, h)).point(lambda v: int(max(0, v - 120) * 1.1))   # each tier darkens to its ragged foot
    soft(sub, gr, (112, 84, 44))
    d = ImageDraw.Draw(sub)
    x = 0.0
    while x < w:                                           # pale straw ends along the lip
        tw = rnd.uniform(0.1, 0.24) * ppm
        d.polygon([(x, h), (x + tw, h), (x + tw * 0.5, h - rnd.uniform(0.1, 0.28) * ppm)], fill=(236, 214, 150))
        x += tw * rnd.uniform(1.0, 1.8)
    d.rectangle([0, 0, w, ppm * 0.1], fill=(240, 218, 150))
    img.paste(sub, (x0, y0))


def wood(img, box, rnd, ppm, colour=TIMBER, bpx=0, upright=True):
    if CALM_NOW:
        return calm_wood(img, box, rnd, ppm, colour, bpx, upright)
    x0, y0, x1, y1 = [int(v) for v in box]
    w, h = x1 - x0, y1 - y0
    if w < 4 or h < 4:
        return
    sub = Image.new("RGB", (w, h), colour)
    d = ImageDraw.Draw(sub)
    n = int((w if upright else h) / (ppm * 0.09)) + 2
    for k in range(n):
        t = rnd.uniform(0, 1)
        c = shade(colour, rnd.choice((0.82, 0.9, 1.1, 1.16)))
        wd = max(1, int(ppm * rnd.uniform(0.012, 0.03)))
        if upright:
            x = t * w
            d.line([(x, rnd.uniform(0, 0.4) * h), (x + rnd.uniform(-3, 3), rnd.uniform(0.6, 1) * h)], fill=c, width=wd)
        else:
            y = t * h
            d.line([(rnd.uniform(0, 0.4) * w, y), (rnd.uniform(0.6, 1) * w, y + rnd.uniform(-3, 3))], fill=c, width=wd)
    if bpx:
        bw = bpx * 1.3
        d.rectangle([0, 0, w, bw], fill=mix(colour, CREAM, 0.55))
        d.rectangle([0, 0, bw, h], fill=mix(colour, CREAM, 0.4))
        d.rectangle([w - bw, 0, w, h], fill=mix(colour, CREAM, 0.25))
        d.rectangle([0, h - bw, w, h], fill=shade(colour, 0.7))
    img.paste(sub, (x0, y0))


def weave(img, box, rnd, ppm):
    """The woven band under an eave: red and black diamonds on cream, between ochre lines."""
    x0, y0, x1, y1 = [int(v) for v in box]
    w, h = x1 - x0, y1 - y0
    if w < 4 or h < 4:
        return
    sub = Image.new("RGB", (w, h), CREAM)
    d = ImageDraw.Draw(sub)
    d.rectangle([0, 0, w, h * 0.16], fill=RED)
    d.rectangle([0, h * 0.84, w, h], fill=RED)
    d.rectangle([0, h * 0.16, w, h * 0.24], fill=BLACK)
    d.rectangle([0, h * 0.76, w, h * 0.84], fill=BLACK)
    u = h * 0.52
    n = max(1, int(w / u))
    u2 = w / n
    for k in range(n):
        cx, cy = (k + 0.5) * u2, h * 0.5
        d.polygon([(cx - u2 * 0.46, cy), (cx, cy - h * 0.24), (cx + u2 * 0.46, cy), (cx, cy + h * 0.24)], fill=RED if k % 2 else BLACK)
        d.polygon([(cx - u2 * 0.2, cy), (cx, cy - h * 0.1), (cx + u2 * 0.2, cy), (cx, cy + h * 0.1)], fill=OCHRE)
    for _ in (range(int(w / ppm * 1.5)) if not CALM_NOW else ()):
        blob(d, rnd.uniform(0, w), rnd.uniform(0, h), rnd.uniform(0.04, 0.12) * ppm, rnd.uniform(0.03, 0.08) * ppm, rnd, mix(CREAM, DARK, 0.35))
    img.paste(sub, (x0, y0))


# ------------------------------------------------------------------ calm painters
def _apply_light(sub, top=1.05, bottom=0.90, cool=True):
    """One soft gradient over the whole face: lighter at the top, a little darker and cooler toward the foot."""
    w, h = sub.size
    k = np.linspace(top, bottom, h, dtype=np.float32)[:, None, None]
    a = np.asarray(sub, np.float32) * k
    if cool:
        t = np.linspace(0, 1, h, dtype=np.float32)[:, None, None]
        a = a * (1 - 0.5 * t) + a * np.array([0.95, 0.97, 1.03], np.float32) * 0.5 * t
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))


def calm_stone(img, box, base, rnd, ppm, bpx, joints=0.0, moss=0.4, foot=0.3, bevel=1.0):
    x0, y0, x1, y1 = [int(v) for v in box]
    w, h = x1 - x0, y1 - y0
    if w < 4 or h < 4:
        return
    tone = calm_stone_tone(base)
    sub = Image.new("RGB", (w, h), tone)
    hs = Image.new("L", (w, h), 128)
    d = ImageDraw.Draw(sub)
    if joints:                                            # a few large stones, faint soft joints
        jm = Image.new("L", (w, h), 0)
        jd = ImageDraw.Draw(jm)
        ch = joints * 1.7 * ppm
        rows = max(1, round(h / ch))
        ch = h / rows
        for r in range(rows):
            xx = -rnd.uniform(0.2, 0.9) * ch * 1.8 if r % 2 else 0.0
            while xx < w:
                cw = rnd.uniform(1.6, 2.6) * ch
                d.rectangle([xx, r * ch, xx + cw, (r + 1) * ch], fill=shade(tone, rnd.uniform(0.975, 1.025)))
                jd.line([(xx, r * ch), (xx, (r + 1) * ch)], fill=255, width=max(2, int(ppm * 0.05)))
                xx += cw
            jd.line([(0, r * ch), (w, r * ch)], fill=255, width=max(2, int(ppm * 0.05)))
        jb = jm.filter(ImageFilter.GaussianBlur(max(1.0, ppm * 0.02)))
        soft(sub, jb, shade(tone, 0.90), 0.8)
        hs.paste(int(128 - 0.02 / HU), (0, 0, w, h), jb)
    sub = _apply_light(sub, 1.05, 0.91)
    if bevel:                                             # a subtle lighter edge; in the height map a rounded fall-off
        bw = max(2, bpx * 1.2 * bevel)
        e = Image.new("L", (w, h), 0)
        ImageDraw.Draw(e).rectangle([bw, bw, w - bw, h - bw], fill=255)
        eb = e.filter(ImageFilter.GaussianBlur(bw * 0.45))
        edge = ImageChops.invert(eb)
        top = Image.new("L", (w, h), 0)
        ImageDraw.Draw(top).rectangle([0, 0, w, bw * 1.2], fill=255)
        top = ImageChops.multiply(edge, top.filter(ImageFilter.GaussianBlur(bw * 0.4)))
        soft(sub, top, mix(tone, PALE, 0.35), 0.8)
        side = ImageChops.multiply(edge, ImageChops.invert(top))
        soft(sub, side, mix(tone, PALE, 0.18), 0.6)
        flat = Image.new("L", (w, h), int(128 - 0.035 / HU))
        flat.paste(hs, (0, 0), eb)
        hs = flat
    if moss:
        calm_hang(sub, rnd, ppm, moss, hsub=hs)
    img.paste(sub, (x0, y0))
    H = HEIGHT.get(id(img))
    if H is not None:
        H.paste(hs, (x0, y0))


def calm_hang(sub, rnd, ppm, amount=0.5, hsub=None):
    """At most a few big soft moss shapes along the top edge, one muted green, each one shape with a soft edge."""
    if amount < 0.45:
        return
    w, h = sub.size
    n = int(round(w / ppm * amount * 0.22 * rnd.uniform(0.6, 1.3)))
    if n <= 0:
        return
    m = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(m)
    for _ in range(n):
        x = rnd.uniform(0, w)
        span = rnd.uniform(0.6, 1.3) * ppm
        ln = rnd.uniform(0.18, 0.42) * ppm
        d.ellipse([x - span, -ln, x + span, ln], fill=255)
    mb = m.filter(ImageFilter.GaussianBlur(max(1.5, ppm * 0.05)))
    soft(sub, mb, CMOSS, 0.92)
    H = hsub if hsub is not None else HEIGHT.get(id(sub))
    if H is not None:
        H.paste(int(128 + 0.015 / HU), (0, 0, w, h), mb)


def calm_thatch(img, box, rnd, ppm, colour=THATCH, rows=None):
    """One golden straw colour, broad rows with a soft lit lip, a darker underside toward each row's foot."""
    x0, y0, x1, y1 = [int(v) for v in box]
    w, h = x1 - x0, y1 - y0
    if w < 4 or h < 4:
        return
    base = mix(colour, (214, 172, 96), 0.25)
    rows = rows or max(2, int(h / (ppm * 0.5)))
    rh = h / rows
    a = np.zeros((h, w, 3), np.float32) + np.array(base, np.float32)
    yy = np.arange(h, dtype=np.float32)[:, None]
    xx = np.arange(w, dtype=np.float32)[None, :]
    rag = 0.10 * rh * np.sin(xx / (ppm * 0.38) + np.floor(yy / rh) * 1.7) + 0.06 * rh * np.sin(xx / (ppm * 0.15) + 0.6)
    f = ((yy + rag) % rh) / rh                            # 0 at a row's top, 1 at its ragged foot (a soft wavy lip, not a plank)
    group = 0.5 + 0.5 * np.sin(xx / (ppm * 0.7) + np.floor(yy / rh) * 2.1) * np.sin(xx / (ppm * 1.9) + 1.3)
    k = (0.97 + 0.05 * group) * (1.06 - 0.16 * f ** 1.6)
    a = a * k[..., None]
    a = a * np.linspace(1.03, 0.92, h, dtype=np.float32)[:, None, None]
    img.paste(Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)), (x0, y0))
    H = HEIGHT.get(id(img))
    if H is not None:                                     # each row's lip proud, broad strand bundles
        hv = 128 + (0.05 * f + 0.02 * group) / HU
        H.paste(Image.fromarray(np.clip(hv, 0, 255).astype(np.uint8)), (x0, y0))


def calm_wood(img, box, rnd, ppm, colour=TIMBER, bpx=0, upright=True):
    x0, y0, x1, y1 = [int(v) for v in box]
    w, h = x1 - x0, y1 - y0
    if w < 4 or h < 4:
        return
    sub = _apply_light(Image.new("RGB", (w, h), colour), 1.05, 0.92, cool=False)
    hs = Image.new("L", (w, h), 128)
    g = ImageDraw.Draw(hs)
    step = ppm * 0.32                                     # a few soft plank grooves, no grain
    n = int((w if upright else h) / step)
    for k in range(1, n + 1):
        t = k * step
        if upright:
            g.line([(t, 0), (t, h)], fill=int(128 - 0.012 / HU), width=max(2, int(ppm * 0.04)))
        else:
            g.line([(0, t), (w, t)], fill=int(128 - 0.012 / HU), width=max(2, int(ppm * 0.04)))
    if bpx:
        e = Image.new("L", (w, h), 0)
        bw = bpx * 1.3
        ImageDraw.Draw(e).rectangle([0, 0, w, bw], fill=255)
        soft(sub, e.filter(ImageFilter.GaussianBlur(bw * 0.4)), mix(colour, CREAM, 0.25), 0.8)
    img.paste(sub, (x0, y0))
    H = HEIGHT.get(id(img))
    if H is not None:
        H.paste(hs.filter(ImageFilter.GaussianBlur(max(1, ppm * 0.02))), (x0, y0))


# ------------------------------------------------------------------ geometry
def _finish(P, mesh, uv, at, turn, kind):
    return P.add(hk.moved(mesh, at, turn), uv, kind)


_FACES = {
    "front": [(-1, 1, 1), (1, 1, 1), (1, -1, 1), (-1, -1, 1)],
    "right": [(1, 1, 1), (1, 1, -1), (1, -1, -1), (1, -1, 1)],
    "back": [(1, 1, -1), (-1, 1, -1), (-1, -1, -1), (1, -1, -1)],
    "left": [(-1, 1, -1), (-1, 1, 1), (-1, -1, 1), (-1, -1, -1)],
    "top": [(-1, 1, -1), (1, 1, -1), (1, 1, 1), (-1, 1, 1)],
    "bottom": [(-1, -1, 1), (1, -1, 1), (1, -1, -1), (-1, -1, -1)],
}
_SIDES = ("left", "front", "right", "back")


class Info(dict):
    __getattr__ = dict.__getitem__


def block(P, w, h, d, at, b=0.14, taper=(1.0, 1.0), lean=(0.0, 0.0), turn=(0, 0, 0), paint=None, base=None, top=True,
          ppm=None, kind="add", share=None, jit=0.0, moss=0.4, joints=0.0, raw=None, foot=0.3, cracks=1.0, top_moss=0.5, skew=0.0, top_joints=0.0, top_ppm=None, bottom=None):
    """A chunky block with a bevel on all twelve edges, its foot's centre at `at`. taper: its top's size against its
    foot's (x, z). lean: how far its top is pushed over (x, z). Its four sides are unrolled into ONE strip of the atlas
    (left, front, right, back) painted as stone with the wide pale bevel; then paint(img, info) draws its carving
    (info.front and the others are pixel boxes, info.ppm, info.bpx, info.rnd). raw(img, info) replaces the stone paint
    entirely (thatch, timber). top: True paints its own top, a colour gives a swatch, False a dark swatch."""
    rnd = P.rnd
    base = base or rnd.choice(STONES)
    b = min(b, w * 0.3, h * 0.3, d * 0.3)
    jit = jit or getattr(P, "default_jit", 0.0)       # a hair of unevenness also keeps faces from lying exactly on one another
    C = {}
    for sx in (-1, 1):
        for sy in (-1, 1):
            for sz in (-1, 1):
                tx, tz = (taper[0], taper[1]) if sy > 0 else (1.0, 1.0)
                p = [sx * w / 2 * tx + (lean[0] if sy > 0 else 0) + (skew * sz if sy > 0 else 0), h if sy > 0 else 0.0, sz * d / 2 * tz + (lean[1] if sy > 0 else 0)]
                if jit:
                    p = [p[0] + rnd.uniform(-jit, jit), p[1] + (rnd.uniform(-jit, jit) if sy > 0 else 0), p[2] + rnd.uniform(-jit, jit)]
                C[(sx, sy, sz)] = np.array(p, np.float64)
    centre = sum(C.values()) / 8.0

    def unit(v):
        return v / max(1e-9, np.linalg.norm(v))

    V = {}                                               # (face, corner) -> the face's own inset point
    for f, cs in _FACES.items():
        for i, c in enumerate(cs):
            p, nx, pv = C[c], C[cs[(i + 1) % 4]], C[cs[(i - 1) % 4]]
            V[(f, c)] = p + b * (unit(nx - p) + unit(pv - p))

    ppm = ppm or P.ppm
    key = ("block", share) if share else None
    if key and key in P.shared:
        rect, toprect, spans = P.shared[key]
    else:
        widths = {"front": w, "back": w, "left": d, "right": d}
        tw = sum(widths[s] for s in _SIDES) * ppm
        rect = P.alloc(tw, h * ppm)
        spans, x = {}, 0.0
        for s in _SIDES:
            spans[s] = (x, x + widths[s] * ppm)
            x += widths[s] * ppm
        H = rect[3] - rect[1]
        info = Info(ppm=ppm * SS, bpx=b * ppm * SS, rnd=rnd, w=(rect[2] - rect[0]) * SS, h=H * SS, base=base, W=w, H=h, D=d)
        for s in _SIDES:
            info[s] = (int(spans[s][0] * SS), 0, int(spans[s][1] * SS), H * SS)

        def fn(img):
            if raw:
                raw(img, info)
            else:
                for s in _SIDES:
                    stone(img, info[s], base, rnd, info.ppm, info.bpx, joints=joints, moss=moss, foot=foot, cracks=cracks)
            if paint:
                paint(img, info)
        P.paint(rect, fn)
        if top is True:
            tp = top_ppm or max(10, ppm * 0.6)
            toprect = P.alloc(w * tp, d * tp)

            def tfn(img):
                if raw:
                    raw(img, Info(ppm=tp * SS, bpx=b * tp * SS, rnd=rnd, w=img.size[0], h=img.size[1], base=base, top=(0, 0, img.size[0], img.size[1]), only_top=True, W=w, H=h, D=d))
                else:
                    stone(img, (0, 0, img.size[0], img.size[1]), shade(base, 1.1), rnd, tp * SS, b * tp * SS, moss=0, foot=0, cracks=0.6, joints=top_joints)
                    if top_moss and CALM_NOW:
                        calm_hang(img, rnd, tp * SS, min(1.0, top_moss * 0.6))
                    elif top_moss:
                        dd = ImageDraw.Draw(img)
                        for _ in range(int(w * d * top_moss * 1.2) + 1):
                            blob(dd, rnd.uniform(0, img.size[0]), rnd.uniform(0, img.size[1]), rnd.uniform(0.15, 0.4) * tp * SS, rnd.uniform(0.1, 0.3) * tp * SS, rnd, rnd.choice((MOSS, MOSS_L, shade(MOSS, 0.85))))
            P.paint(toprect, tfn)
        elif top is False:
            toprect = P.swatch(shade(base, 0.6))
        else:
            toprect = P.swatch(top)
        if key:
            P.shared[key] = (rect, toprect, spans)
    botrect = P.swatch(bottom or shade(base, 0.5))
    S = float(P.S)
    bp = b * ppm

    def side_uv(s, c, inset=True, lift=False):
        """Where corner `c` of side `s` sits in the strip; inset = the face's own point, lift = on the top/bottom edge."""
        cs = _FACES[s]
        i = cs.index(c)
        x0, x1 = spans[s]
        H = rect[3] - rect[1]
        left = i in (0, 3)
        topc = i in (0, 1)
        px = (x0 + bp if left else x1 - bp) if inset else (x0 if left else x1)
        if lift:
            py = 0.0 if topc else H
            px = x0 + bp if left else x1 - bp
        else:
            py = (bp if topc else H - bp)
        return ((rect[0] + px) / S, (rect[1] + py) / S)

    def cap_uv(r, f, c):
        cs = _FACES[f]
        i = cs.index(c)
        m = 1.5
        sw_ = (r[2] - r[0]) == 40 and (r[3] - r[1]) == 40    # a swatch: keep off its pale band
        return ((r[0] + m if i in (0, 3) else r[2] - m) / S, (r[1] + (12 if sw_ else m) if i in (0, 1) else r[3] - m) / S)

    pos, nrm, uvs, tris = [], [], [], []

    def vert(p, n, uv):
        pos.append(p); nrm.append(n); uvs.append(uv)
        return len(pos) - 1

    def tri(a, bb, c):
        pa, pb, pc = (np.array(pos[i]) for i in (a, bb, c))
        n = np.cross(pb - pa, pc - pa)
        if np.dot(n, (pa + pb + pc) / 3 - centre) < 0:
            tris.append((a, c, bb))
        else:
            tris.append((a, bb, c))

    fn_ = {}
    for f, cs in _FACES.items():
        q = [V[(f, c)] for c in cs]
        n = unit(np.cross(q[3] - q[0], q[1] - q[0]))
        if np.dot(n, (q[0] + q[2]) / 2 - centre) < 0:
            n = -n
        fn_[f] = n
        r = toprect if f == "top" else botrect
        ids = [vert(V[(f, c)], n, side_uv(f, c) if f in _SIDES else cap_uv(r, f, c)) for c in cs]
        tri(ids[0], ids[3], ids[2]); tri(ids[0], ids[2], ids[1])
    names = list(_FACES)
    for i, fa in enumerate(names):                         # the twelve bevels
        for fb in names[i + 1:]:
            both = [c for c in _FACES[fa] if c in _FACES[fb]]
            if len(both) != 2:
                continue
            c1, c2 = both
            if fa in _SIDES and fb in _SIDES:                # a corner bevel between two sides: they are neighbours in the strip
                order = sorted((fa, fb), key=_SIDES.index)
                if order == ["left", "back"]:                 # the strip's two ends
                    ua = [(rect[2] / S, side_uv("back", c)[1]) for c in (c1, c2)]
                    q = [vert(V[("back", c1)], fn_["back"], side_uv("back", c1)), vert(V[("back", c2)], fn_["back"], side_uv("back", c2)),
                         vert(V[("left", c2)], fn_["left"], ua[1]), vert(V[("left", c1)], fn_["left"], ua[0])]
                else:
                    q = [vert(V[(fa, c1)], fn_[fa], side_uv(fa, c1)), vert(V[(fa, c2)], fn_[fa], side_uv(fa, c2)),
                         vert(V[(fb, c2)], fn_[fb], side_uv(fb, c2)), vert(V[(fb, c1)], fn_[fb], side_uv(fb, c1))]
            else:
                s, cap = (fa, fb) if fa in _SIDES else (fb, fa)
                q = [vert(V[(s, c1)], fn_[s], side_uv(s, c1)), vert(V[(s, c2)], fn_[s], side_uv(s, c2)),
                     vert(V[(cap, c2)], fn_[cap], side_uv(s, c2, lift=True)), vert(V[(cap, c1)], fn_[cap], side_uv(s, c1, lift=True))]
            tri(q[0], q[1], q[2]); tri(q[0], q[2], q[3])
    for c in C:                                            # the eight corners
        fs = [f for f in names if c in _FACES[f]]
        sides = [f for f in fs if f in _SIDES]
        cap = [f for f in fs if f not in _SIDES][0]
        s0 = sides[0]
        t = [vert(V[(sides[0], c)], fn_[sides[0]], side_uv(s0, c)), vert(V[(sides[1], c)], fn_[sides[1]], side_uv(s0, c, inset=False)),
             vert(V[(cap, c)], fn_[cap], side_uv(s0, c, lift=True))]
        tri(*t)
    mesh = (np.array(pos, np.float32), np.array(nrm, np.float32), np.array(tris, np.uint32))
    return _finish(P, mesh, np.array(uvs, np.float32), at, turn, kind)


def _inset(pts, dist):
    """Each point of a counter-clockwise outline moved inward along its corner's bisector."""
    n = len(pts)
    out = []
    for i in range(n):
        p = np.array(pts[i], np.float64)
        a = np.array(pts[i - 1], np.float64)
        c = np.array(pts[(i + 1) % n], np.float64)
        e1, e2 = p - a, c - p
        e1 /= max(1e-9, np.linalg.norm(e1)); e2 /= max(1e-9, np.linalg.norm(e2))
        n1, n2 = np.array([-e1[1], e1[0]]), np.array([-e2[1], e2[0]])
        m = n1 + n2
        ln = np.linalg.norm(m)
        if ln < 1e-6:
            m, ln = n1, 1.0
        m /= ln
        k = dist / max(0.5, float(np.dot(m, n1)))
        out.append(tuple(p + m * k))
    return out


def _ears(pts):
    """Triangles of a simple counter-clockwise polygon."""
    idx = list(range(len(pts)))
    tris = []

    def area(a, b, c):
        return (pts[b][0] - pts[a][0]) * (pts[c][1] - pts[a][1]) - (pts[b][1] - pts[a][1]) * (pts[c][0] - pts[a][0])

    def inside(p, a, b, c):
        return area(a, b, p) > 1e-12 and area(b, c, p) > 1e-12 and area(c, a, p) > 1e-12
    guard = 0
    while len(idx) > 3 and guard < 5000:
        guard += 1
        done = False
        for k in range(len(idx)):
            a, b, c = idx[k - 1], idx[k], idx[(k + 1) % len(idx)]
            if area(a, b, c) <= 1e-12:
                continue
            if any(inside(p, a, b, c) for p in idx if p not in (a, b, c)):
                continue
            tris.append((a, b, c)); idx.pop(k); done = True
            break
        if not done:
            idx.pop(0)
    if len(idx) == 3:
        tris.append(tuple(idx))
    return tris


GREYS = STONES + [(128, 125, 120), (146, 142, 135), (110, 108, 106), (162, 157, 148), (150, 138, 122), (196, 188, 172)]


def circle(r, n=24, cx=0.0, cy=0.0, ry=None, start=0.0):
    ry = ry or r
    return [(cx + r * math.cos(start + math.tau * k / n), cy + ry * math.sin(start + math.tau * k / n)) for k in range(n)]


def slab(P, outline, depth, at, turn=(0, 0, 0), b=0.08, paint=None, base=None, ppm=None, kind="add", share=None, sw=None, face=None, side=None, bevel_c=None, wall=None):
    """A bevelled silhouette: `outline` (xy, counter-clockwise, in metres) standing in its own xy plane, from z = 0 at
    its back to z = depth at its FRONT, moved by turn then at. Its front is painted from straight in front in its own
    region: paint(img, info) with info.px(x, y) -> pixels, info.mask (its inner face), info.ppm, info.rnd; before that
    the region is the stone's colour inside a pale bevel. sw (a colour) gives it a shared swatch instead."""
    rnd = P.rnd
    base = base or rnd.choice(STONES)
    o = [tuple(map(float, p)) for p in outline]
    xs, ys = [p[0] for p in o], [p[1] for p in o]
    b = min(b, depth * 0.45, (max(xs) - min(xs)) * 0.22, (max(ys) - min(ys)) * 0.22)
    ins = _inset(o, b)
    ppm = ppm or P.ppm
    S = float(P.S)
    mg = 5
    if sw is not None:
        r = P.swatch(sw)
        mid_uv = ((r[0] + r[2]) / 2 / S, (r[1] + (r[3] - r[1]) * 0.6) / S)
        edge_uv = ((r[0] + r[2]) / 2 / S, (r[1] + (r[3] - r[1]) * 0.08) / S)
        low_uv = ((r[0] + r[2]) / 2 / S, (r[1] + (r[3] - r[1]) * 0.95) / S)
        uvF = [mid_uv] * len(o); uvO = [edge_uv] * len(o); uvW = [low_uv] * len(o); uvW1 = uvO
    else:
        key = ("slab", share) if share else None
        x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
        if key and key in P.shared:
            r = P.shared[key]
        else:
            r = P.alloc((x1 - x0) * ppm + 2 * mg, (y1 - y0) * ppm + 2 * mg)

            def px(x, y, s=SS):
                return ((mg + (x - x0) * ppm) * s, (mg + (y1 - y) * ppm) * s)

            def fn(img):
                d = ImageDraw.Draw(img)
                calm = CALM_NOW and kind != "glow"
                cb = calm_stone_tone(base) if calm and face is None and base in GREYS else base
                d.rectangle([0, 0, img.size[0], img.size[1]], fill=side or shade(cb, 0.66))
                d.polygon([px(*p) for p in o], fill=bevel_c or mix(cb, PALE, 0.8 if not calm else 0.3))
                m, md = L(img)
                md.polygon([px(*p) for p in ins], fill=255)
                info = Info(px=px, mask=m, ppm=ppm * SS, rnd=rnd, base=cb, w=img.size[0], h=img.size[1], bpx=b * ppm * SS, box=(mg * SS, mg * SS, img.size[0] - mg * SS, img.size[1] - mg * SS))
                fc = face or cb
                if calm:                                  # one colour, one gradient; the bevel rounds off in the height map
                    soft(img, m, fc)
                    gr = ImageChops.multiply(m, Image.linear_gradient("L").resize(img.size).point(lambda v: int(v * 0.35)))
                    soft(img, gr, shade(fc, 0.78))
                    hset(img, Image.new("L", img.size, 255), -0.035)
                    hset(img, m.filter(ImageFilter.GaussianBlur(max(1.5, b * ppm * SS * 0.45))), 0.0)
                    if paint:
                        paint(img, info)
                    return
                soft(img, m, fc)
                hi = ImageChops.multiply(m, ImageChops.invert(ImageChops.offset(m, int(ppm * SS * 0.05), int(ppm * SS * 0.07))))
                soft(img, hi, mix(fc, PALE, 0.5))
                gr = ImageChops.multiply(m, Image.linear_gradient("L").resize(img.size).point(lambda v: int(max(0, v - 110) * 0.9)))
                soft(img, gr, mix(fc, SHADOW, 0.6))
                if paint:
                    paint(img, info)
            P.paint(r, fn)
            if key:
                P.shared[key] = r

        def uv(p, push=0.0, i=0):
            u = (r[0] + mg + (p[0] - x0) * ppm) / S
            v = (r[1] + mg + (y1 - p[1]) * ppm) / S
            if push:
                q = _inset(o, -push / ppm)[i]
                u = (r[0] + mg + (q[0] - x0) * ppm) / S
                v = (r[1] + mg + (y1 - q[1]) * ppm) / S
            return (u, v)
        uvF = [uv(p) for p in ins]
        uvO = [uv(p) for p in o]
        wr = P.swatch(wall or base)                       # its thickness wears the stone's own colour, pale at the front edge
        uvW1 = [((wr[0] + 8 + 24 * (i % 2)) / S, (wr[1] + 9) / S) for i in range(len(o))]
        uvW = [((wr[0] + 8 + 24 * (i % 2)) / S, (wr[1] + 36) / S) for i in range(len(o))]
    n = len(o)
    pos, nrm, uvs, tris = [], [], [], []

    def ring(pts, z, uvl, nz):
        base_i = len(pos)
        for i, p in enumerate(pts):
            q = np.array(o[i]) - np.array(ins[i])
            q = q / max(1e-9, np.linalg.norm(q))
            pos.append((p[0], p[1], z))
            nrm.append((q[0] * (1 - abs(nz)), q[1] * (1 - abs(nz)), nz) if abs(nz) < 1 else (0, 0, nz))
            uvs.append(uvl[i])
        return base_i

    F = ring(ins, depth, uvF, 1.0)
    R1 = ring(o, depth - b, uvO, 0.0)
    R1w = ring(o, depth - b, uvW1, 0.0)
    R2w = ring(o, b, uvW, 0.0)
    R2 = ring(o, b, uvO, 0.0)
    Bk = ring(ins, 0.0, uvF, -1.0)
    for (a, bb, c) in _ears(ins):
        tris.append((F + a, F + bb, F + c))
        tris.append((Bk + a, Bk + c, Bk + bb))
    for i in range(n):
        j = (i + 1) % n
        for (A, Bq) in ((F, R1), (R1w, R2w), (R2, Bk)):
            tris.append((A + i, Bq + i, Bq + j)); tris.append((A + i, Bq + j, A + j))
    mesh = (np.array(pos, np.float32), np.array(nrm, np.float32), np.array(tris, np.uint32))
    return _finish(P, mesh, np.array(uvs, np.float32), at, turn, kind)


def curve(pts, n=16):
    """A smooth path through the points."""
    p = np.array(pts, np.float64)
    out = []
    ext = np.vstack([p[0] * 2 - p[1], p, p[-1] * 2 - p[-2]])
    segs = len(p) - 1
    for k in range(n + 1):
        t = k / n * segs
        i = min(int(t), segs - 1)
        f = t - i
        a, b, c, d = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        out.append(0.5 * ((2 * b) + (-a + c) * f + (2 * a - 5 * b + 4 * c - d) * f * f + (-a + 3 * b - 3 * c + d) * f ** 3))
    return [tuple(v) for v in out]


def pipe(P, points, radius, colour, seg=6, kind="loose", paint=None, ppm=None, key=None):
    """A vine, a rope, a horn (radius may be a list)."""
    mesh = hk.tube(points, radius, seg=seg)
    if paint:
        ppm = ppm or P.ppm
        ln = sum(math.dist(a, b) for a, b in zip(points, points[1:]))
        rmax = radius if np.isscalar(radius) else max(radius)
        k = ("pipe", key) if key else None
        if k and k in P.shared:
            r = P.shared[k]
        else:
            r = P.alloc(math.tau * rmax * ppm, ln * ppm)
            P.paint(r, lambda img: paint(img, Info(ppm=ppm * SS, rnd=P.rnd, w=img.size[0], h=img.size[1])))
            if k:
                P.shared[k] = r
        S = float(P.S)
        uv = hk.grid_uv(mesh, ((r[0] + 1) / S, (r[1] + 1) / S, (r[2] - 1) / S, (r[3] - 1) / S), seg, along="rows")
    else:
        r = P.swatch(colour)
        S = float(P.S)
        uv = hk.grid_uv(mesh, ((r[0] + 3) / S, (r[1] + 12) / S, (r[2] - 3) / S, (r[3] - 3) / S), seg, along="rows")
        uv = uv[:, ::-1].copy() if False else uv
        # round the tube = down the swatch (light on one side, dark on the other)
        cols = seg + 1
        for i in range(len(uv)):
            c = i % cols
            t = 0.5 - 0.5 * math.cos(math.tau * c / seg)
            uv[i] = ((r[0] + 20) / S, (r[1] + 12 + (r[3] - r[1] - 16) * t) / S)
    return P.add(mesh, uv, kind)


def turned(P, profile, at, colour=None, seg=12, turn=(0, 0, 0), paint=None, ppm=None, kind="add", squash=1.0, key=None):
    """A lathe: profile of (radius, y) from bottom to top. paint(img, info) gets its unrolled side."""
    mesh = hk.lathe(profile, seg=seg, squash_z=squash)
    S = float(P.S)
    if paint:
        ppm = ppm or P.ppm
        rmax = max(p[0] for p in profile)
        ln = sum(math.hypot(b[0] - a[0], b[1] - a[1]) for a, b in zip(profile, profile[1:]))
        k = ("turned", key) if key else None
        if k and k in P.shared:
            r = P.shared[k]
        else:
            r = P.alloc(math.tau * rmax * ppm, ln * ppm)
            P.paint(r, lambda img: paint(img, Info(ppm=ppm * SS, rnd=P.rnd, w=img.size[0], h=img.size[1])))
            if k:
                P.shared[k] = r
        uv = hk.grid_uv(mesh, ((r[0] + 1) / S, (r[1] + 1) / S, (r[2] - 1) / S, (r[3] - 1) / S), seg, along="axis")
    else:
        r = P.swatch(colour)
        uv = hk.grid_uv(mesh, ((r[0] + 3) / S, (r[1] + 10) / S, (r[2] - 3) / S, (r[3] - 3) / S), seg, along="axis")
    return _finish(P, mesh, uv, at, turn, kind)


def ball(P, half, at, colour, seg=10, rings=6, turn=(0, 0, 0), kind="add"):
    mesh = hk.ellipsoid(half, seg=seg, rings=rings)
    r = P.swatch(colour)
    S = float(P.S)
    uv = hk.grid_uv(mesh, ((r[0] + 3) / S, (r[1] + 3) / S, (r[2] - 3) / S, (r[3] - 3) / S), seg, along="rows")
    return _finish(P, mesh, uv, at, turn, kind)


# ------------------------------------------------------------------ things many pieces share
LEAF_SHAPE = [(0.0, 0.0), (0.16, 0.1), (0.24, 0.3), (0.22, 0.52), (0.12, 0.76), (0.0, 1.0), (-0.12, 0.76), (-0.22, 0.52), (-0.24, 0.3), (-0.16, 0.1)]
FROND_SHAPE = [(0.0, 0.0), (0.1, 0.12), (0.3, 0.2), (0.2, 0.34), (0.36, 0.5), (0.2, 0.6), (0.3, 0.8), (0.12, 0.84), (0.0, 1.1), (-0.12, 0.84), (-0.3, 0.8), (-0.2, 0.6), (-0.36, 0.5), (-0.2, 0.34), (-0.3, 0.2), (-0.1, 0.12)]


def _leaf_paint(colour, light):
    def fn(img, info):
        d = ImageDraw.Draw(img)
        soft(img, info.mask, colour)
        a, bq = info.px(0, 0.02), info.px(0, 0.95)
        half, hd = L(img)
        hd.polygon([a, bq, (img.size[0], bq[1]), (img.size[0], a[1])], fill=255)
        soft(img, ImageChops.multiply(half, info.mask), light)
        if CALM_NOW:
            return                                        # calm: no vein linework
        d.line([a, bq], fill=shade(colour, 0.6), width=max(2, int(info.ppm * 0.03)))
        for k in range(1, 5):
            y = 0.15 + k * 0.16
            for sx in (-1, 1):
                d.line([info.px(0, y), info.px(sx * 0.17, y + 0.12)], fill=shade(colour, 0.72), width=max(1, int(info.ppm * 0.015)))
    return fn


def leaf(P, at, size=0.5, turn=(0, 0, 0), kind_shape="leaf", tone=0):
    """A leaf: a light two sided card of the unit leaf (so every leaf shares one painting), bent a little along its
    length, scaled about its stem, turned and placed."""
    cols = [(LEAF, LEAF_L), (shade(LEAF, 0.85), LEAF), (MOSS_L, (190, 226, 120))][tone % 3]
    shape = LEAF_SHAPE if kind_shape == "leaf" else FROND_SHAPE
    key = ("leafcard", kind_shape, tone % 3)
    ppm, mg = 80, 4
    xs, ys = [q[0] for q in shape], [q[1] for q in shape]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    if key not in P.shared:
        r = P.alloc((x1 - x0) * ppm + 2 * mg, (y1 - y0) * ppm + 2 * mg)

        def px(x, y):
            return ((mg + (x - x0) * ppm) * SS, (mg + (y1 - y) * ppm) * SS)

        def fn(img):
            ImageDraw.Draw(img).rectangle([0, 0, img.size[0], img.size[1]], fill=shade(cols[0], 0.62))
            m, md = L(img)
            md.polygon([px(*q) for q in _inset(shape, 0.012)], fill=255)
            _leaf_paint(*cols)(img, Info(px=px, mask=m, ppm=ppm * SS, rnd=P.rnd))
        P.paint(r, fn)
        P.shared[key] = r
    r = P.shared[key]
    S = float(P.S)
    pos, nrm, uvs, tris = [], [], [], []
    n = len(shape)
    for sgn in (1, -1):
        b0 = len(pos)
        for (x, y) in shape + [(0.0, 0.45)]:
            pos.append((x, y, -0.25 * (y - 0.5) ** 2 + 0.12 * abs(x) + sgn * 0.004)); nrm.append((0, 0, sgn))
            uvs.append(((r[0] + mg + (x - x0) * ppm) / S, (r[1] + mg + (y1 - y) * ppm) / S))
        for i in range(n):
            j = (i + 1) % n
            tris.append((b0 + i, b0 + j, b0 + n) if sgn > 0 else (b0 + j, b0 + i, b0 + n))
    mesh = (np.array(pos, np.float32), np.array(nrm, np.float32), np.array(tris, np.uint32))
    return P.add(hk.moved(mesh, at, turn, scale=(size, size, size)), np.array(uvs, np.float32), "loose")


def vine(P, pts, radius=0.06, leaves=6, size=0.45, colour=None, seed=0, tone=0, out=(0, 0, 1), wander=0.3):
    """A vine along a smooth path, with leaves hanging off it, facing `out`."""
    rnd = random.Random(seed)
    wob = [pts[0]]
    for a, b in zip(pts, pts[1:]):
        mid = [(a[i] + b[i]) / 2 for i in range(3)]
        side = (out[2], 0, -out[0])
        k = rnd.uniform(-1, 1) * wander
        wob += [(mid[0] + side[0] * k, mid[1] + rnd.uniform(-0.1, 0.1), mid[2] + side[2] * k), b]
    path = curve(wob, max(10, len(wob) * 4))
    pipe(P, path, radius, colour or shade(LEAF, 0.72), seg=5)
    yaw = math.degrees(math.atan2(out[0], out[2]))
    for k in range(leaves):
        p = path[int((k + 0.5) / leaves * (len(path) - 1))]
        s = size * rnd.uniform(0.7, 1.25)
        for sgn in ((-1, 1) if k % 3 else (1,)):
            leaf(P, (p[0] + out[0] * 0.05, p[1], p[2] + out[2] * 0.05), s * (1.0 if sgn > 0 else 0.8), turn=(rnd.uniform(20, 45), yaw + rnd.uniform(-25, 25), 180 + sgn * rnd.uniform(35, 75)), tone=tone + k + (sgn > 0))


def glow_bar(P, w, h, at, turn=(0, 0, 0), depth=0.1, shape=None):
    """A turquoise inlay: its own mesh and material in the finished model, so it can be lit at night."""
    def fn(img, info):
        soft(img, info.mask, TURQ)
        inner = ImageChops.multiply(info.mask, ImageChops.multiply(ImageChops.offset(info.mask, 5, 5), ImageChops.offset(info.mask, -5, -5)))
        soft(img, inner, GLOW, 0.75)
    o = shape or [(-w / 2, -h / 2), (w / 2, -h / 2), (w / 2, h / 2), (-w / 2, h / 2)]
    return slab(P, o, depth, at, turn=turn, b=0.03, paint=fn, kind="glow", ppm=30, share=("glow", round(w, 2), round(h, 2), shape is not None and len(shape)),
                base=TURQ, bevel_c=shade(TURQ, 0.8), side=shade(TURQ, 0.6), face=TURQ)


# ------------------------------------------------------------------ the sheet
def sheet(name, version, note="", shots=None, out_dir=None):
    shots = shots or (ROOT / "Logs/payo/pieces/gate/shots")
    out_dir = out_dir or (ROOT / "Logs/payo/pieces/gate")
    stats = json.loads((OUT / "stats.json").read_text()).get(name, {})
    views = [("front", "front three-quarter, from 1.7 m"), ("back", "back three-quarter"), ("close", "its best detail"), ("above", "from above")]
    tiles = [Image.open(shots / ("%s_%s.png" % (name, v))).convert("RGB") for v, _ in views]
    w, h = tiles[0].size
    img = Image.new("RGB", (w * 2, h * 2 + 60), (20, 22, 30))
    d = ImageDraw.Draw(img)
    try:
        from PIL import ImageFont
        big, small = ImageFont.truetype("arialbd.ttf", 30), ImageFont.truetype("arial.ttf", 17)
    except Exception:
        big = small = None
    d.text((14, 12), "%s  v%s" % (name.upper(), version), fill=(250, 240, 220), font=big)
    d.text((430, 20), "%s   %s triangles, %s KB, %s open edges, atlas %s   %s" % (stats.get("size", ""), stats.get("triangles"), stats.get("kb"), stats.get("open_edges"), stats.get("atlas", ""), note), fill=(210, 214, 220), font=small)
    for k, (im, (_, label)) in enumerate(zip(tiles, views)):
        x, y = (k % 2) * w, 60 + (k // 2) * h
        img.paste(im, (x, y))
        d.rectangle([x + 6, y + 6, x + 300, y + 30], fill=(20, 22, 30))
        d.text((x + 12, y + 9), label, fill=(240, 240, 240), font=small)
    path = out_dir / ("%s_v%s.png" % (name, version))
    img.save(path)
    print("sheet", path)
    return path


def compare(name, version, ref):
    a = Image.open(ROOT / "Logs/payo/pieces/gate" / ("%s_v%s.png" % (name, version))).convert("RGB")
    b = Image.open(ref).convert("RGB")
    b = b.resize((int(b.size[0] * a.size[1] / b.size[1]), a.size[1]), Image.LANCZOS)
    img = Image.new("RGB", (a.size[0] + b.size[0], a.size[1]), (20, 22, 30))
    img.paste(a, (0, 0)); img.paste(b, (a.size[0], 0))
    path = ROOT / "Logs/payo/pieces/gate" / ("%s_v%s_vs_238.png" % (name, version))
    img.save(path)
    print("compare", path)
    return path
