"""Draw the ability icon set: coloured, cel-shaded illustrations with an ink keyline.

Why (owner, 2026-09-23: "i want to improve all skill icons too", then, of a flat
single-colour redraw, "do a drawing for all ... that shit is so ugly"). The procedural icons
in AbilityIcons.cs were thin hairlines; a flat silhouette set fixed the weight but was still
a pictogram. Brawl Stars' gadget and star-power icons and Fall Guys' reward icons (inspected,
docs/reports/ui-hud-review-2026-09-23/research.md) are small ILLUSTRATIONS: each part in its
own colour, lit from the upper left, a heavy dark keyline around the whole, and a glint.

How each icon is drawn, so the set stays one family:
  * Parts, each one flat colour, painted back to front. A part's shading is computed from
    its own silhouette: the rim within SHADE of its lower-right edge takes the hue-shifted
    shadow, the rim within LIGHT of its upper-left edge the lit colour (the same hue-shift
    rule as HubStyle.Lit/Deep: light warms toward yellow, shadow toward red, never grey).
  * Every part is separated from what is under it by a thin ink line (PART_LINE).
  * The whole is wrapped in one heavy ink keyline (KEY) and sits on a soft ink drop.
  * Colour rule from CLAUDE.md section 6.4: no swatch has more blue than red.
  * One idea per icon: the picture says what the power DOES (VISION.md section 3 rule 1).

Output: 256 px RGBA per glyph, named like the AbilityGlyph member, in
Assets/TumbangPreso/Resources/UI/ability-icons. Drawn at 3x and downsampled.

Run:  python tools/build_ability_icons.py [--sheet review.png] [GlyphName ...]
"""
import colorsys
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "UI", "ability-icons")
S = 768                 # working canvas (3x of the 256 output); coordinates below are 0..1024
K = S / 1024.0
KEY = 28                # outer ink keyline, working pixels
PART_LINE = 8           # ink between parts
SHADE = 38              # shadow rim depth
LIGHT = 14              # highlight rim depth

INK = (28, 15, 6)


def hexc(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def shift_hue(h, target, step):
    d = (target - h + 0.5) % 1.0 - 0.5
    return (h + max(-step, min(step, d))) % 1.0


def lit(c, amount=1.0):
    h, s, v = colorsys.rgb_to_hsv(*[x / 255 for x in c])
    h = shift_hue(h, 1 / 6, 0.03 * amount)
    s *= 1 - 0.18 * amount
    v = min(1.0, v + (0.14 + 0.12 * (1 - v)) * amount)
    return tuple(int(x * 255) for x in colorsys.hsv_to_rgb(h, s, v))


def deep(c, amount=1.0):
    h, s, v = colorsys.rgb_to_hsv(*[x / 255 for x in c])
    h = shift_hue(h, 0.0, 0.04 * amount)
    s = min(1.0, s * (1 + 0.18 * amount) + 0.05 * amount)
    v *= 1 - 0.32 * amount
    return tuple(int(x * 255) for x in colorsys.hsv_to_rgb(h, s, v))


# The family's swatches. Every one has red >= blue (CLAUDE.md 6.4).
GOLD, HONEY, PERSIMMON, RIMRED, DEEPRED = hexc("F5B521"), hexc("FCD39F"), hexc("FD8041"), hexc("C32E0D"), hexc("980715")
LEMON, CREAM = hexc("FFE45C"), hexc("FFF4DC")
LEATHER, SOLE, STONE, STONE_DK = hexc("8B5227"), hexc("4A2A14"), hexc("C9A27A"), hexc("8A6A4A")
EMBER = hexc("FF6A1A")
ICE, ICE_SH = hexc("F4FAEE"), hexc("CFE6C8")
PLUM, LILAC, KURO = hexc("A2419A"), hexc("E6B3DC"), hexc("3A2338")
MAGENTA, WITCH = hexc("E0307A"), hexc("6E2A66")
SEA, FOAM = hexc("5FA24F"), hexc("FFF4DC")
STORM = hexc("6E5A52")
HORN = hexc("3A2A28")


def _odd(n):
    return n if n % 2 else n + 1


def dilate(mask, px):
    img = Image.fromarray((np.clip(mask, 0, 1) * 255).astype(np.uint8))
    step = 4
    for _ in range(max(1, int(px) // step)):
        img = img.filter(ImageFilter.MaxFilter(_odd(step * 2 + 1)))
    return np.asarray(img).astype(np.float32) / 255.0


def shifted(mask, dx, dy):
    """out(x, y) = mask(x + dx, y + dy), zero outside."""
    out = np.zeros_like(mask)
    h, w = mask.shape
    xs = slice(max(0, dx), w + min(0, dx)); xd = slice(max(0, -dx), w + min(0, -dx))
    ys = slice(max(0, dy), h + min(0, dy)); yd = slice(max(0, -dy), h + min(0, -dy))
    out[yd, xd] = mask[ys, xs]
    return out


class Art:
    """One icon: parts painted back to front, ink strokes, glints."""

    def __init__(self):
        self.parts = []      # (mask, colour, shade)
        self.strokes = []
        self.glints = []

    @staticmethod
    def _pts(pts):
        return [(x * K, y * K) for x, y in pts]

    def _mask(self, draw_fn):
        img = Image.new("L", (S, S), 0)
        draw_fn(ImageDraw.Draw(img))
        return np.asarray(img).astype(np.float32) / 255.0

    def poly(self, pts):
        return self._mask(lambda d: d.polygon(self._pts(pts), fill=255))

    def ellipse(self, cx, cy, rx, ry):
        return self._mask(lambda d: d.ellipse([(cx - rx) * K, (cy - ry) * K, (cx + rx) * K, (cy + ry) * K], fill=255))

    def circle(self, cx, cy, r):
        return self.ellipse(cx, cy, r, r)

    def rect(self, x0, y0, x1, y1):
        return self._mask(lambda d: d.rectangle([x0 * K, y0 * K, x1 * K, y1 * K], fill=255))

    def band(self, pts, w):
        def f(d):
            p = self._pts(pts)
            d.line(p, fill=255, width=max(1, int(w * K)), joint="curve")
            for x, y in (p[0], p[-1]):
                r = w * K / 2
                d.ellipse([x - r, y - r, x + r, y + r], fill=255)
        return self._mask(f)

    def arc_band(self, cx, cy, r, a0, a1, w, steps=60):
        pts = [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * k / steps)),
                cy + r * math.sin(math.radians(a0 + (a1 - a0) * k / steps))) for k in range(steps + 1)]
        return self.band(pts, w)

    def add(self, mask, colour, shade=True):
        mask = np.clip(mask, 0, 1)
        self.parts.append((mask, colour, shade))
        return mask

    def ink(self, pts, w=22):
        self.strokes.append(self.band(pts, w))

    def ink_mask(self, mask):
        self.strokes.append(np.clip(mask, 0, 1))

    def glint(self, cx, cy, rx, ry=None):
        self.glints.append(self.ellipse(cx, cy, rx, ry if ry is not None else rx))

    def render(self, size=256):
        union = np.zeros((S, S), np.float32)
        for m, _, _ in self.parts:
            union = np.maximum(union, m)
        key = dilate(union, KEY)
        ink = np.array(INK, np.float32)
        rgb = np.zeros((S, S, 3), np.float32) + ink
        drop = shifted(key, -8, -11) * 0.35
        alpha = np.maximum(drop, key)

        for m, colour, shade in self.parts:
            ring = np.clip(dilate(m, PART_LINE) - m, 0, 1) * union
            rgb = rgb * (1 - ring[..., None]) + ink * ring[..., None]
            layer = np.zeros((S, S, 3), np.float32) + np.array(colour, np.float32)
            if shade:
                sh = m * (1 - shifted(m, SHADE, SHADE))       # lower-right rim
                hi = m * (1 - shifted(m, -LIGHT, -LIGHT))     # upper-left rim
                layer = layer * (1 - sh[..., None]) + np.array(deep(colour), np.float32) * sh[..., None]
                layer = layer * (1 - hi[..., None]) + np.array(lit(colour), np.float32) * hi[..., None]
            rgb = rgb * (1 - m[..., None]) + layer * m[..., None]

        for s in self.strokes:
            s = s * key
            rgb = rgb * (1 - s[..., None]) + ink * s[..., None]
        for g in self.glints:
            g = g * union
            rgb = rgb * (1 - g[..., None]) + np.array((255, 252, 240), np.float32) * g[..., None]

        # outside the keyline only the drop remains, drawn in ink
        out = np.dstack([np.clip(rgb, 0, 255), np.clip(alpha, 0, 1) * 255]).astype(np.uint8)
        return Image.fromarray(out, "RGBA").resize((size, size), Image.LANCZOS)


def star_pts(cx, cy, r_out, r_in, n, rot=-90):
    return [(cx + (r_out if k % 2 == 0 else r_in) * math.cos(math.radians(rot + k * 180 / n)),
             cy + (r_out if k % 2 == 0 else r_in) * math.sin(math.radians(rot + k * 180 / n))) for k in range(n * 2)]


def rot(pts, cx, cy, deg):
    a = math.radians(deg); c, s = math.cos(a), math.sin(a)
    return [(cx + (x - cx) * c - (y - cy) * s, cy + (x - cx) * s + (y - cy) * c) for x, y in pts]


def flame_pts(cx, base_y, w, h, tongues=3):
    """A flame: a round belly rising into tongues, tallest in the middle."""
    pts = [(cx + w / 2 * math.cos(math.radians(k)), base_y - w * 0.4 + w * 0.4 * math.sin(math.radians(k)))
           for k in range(0, 181, 10)]
    for i in range(tongues):
        x = cx - w / 2 + w * (i + 0.5) / tongues
        tip = h * (1.0 if i == tongues // 2 else 0.7)
        pts.append((x - w * 0.12 / tongues, base_y - h * 0.45))
        pts.append((x + w * 0.05, base_y - tip))
    pts.append((cx + w / 2, base_y - w * 0.4))
    return pts


def slipper_mask(a, cx, cy, length, angle):
    w = length * 0.46
    pts = []
    for k in range(72):
        t = k / 72 * math.pi * 2
        y = length / 2 * math.sin(t)
        f = y / (length / 2)
        width = w / 2 * (1.0 - 0.18 * math.exp(-((f - 0.15) ** 2) / 0.08)) * (1.06 if f < 0 else 0.94)
        pts.append((cx + width * math.cos(t), cy + y))
    return a.poly(rot(pts, cx, cy, angle))


def slipper(a, cx, cy, length, angle, sole, strap):
    """The tsinelas from above: a waisted capsule sole, a thong strap V meeting at a toe post."""
    a.add(slipper_mask(a, cx, cy, length, angle), sole)
    w = length * 0.46
    post = rot([(cx, cy - length * 0.27)], cx, cy, angle)[0]
    left = rot([(cx - w * 0.46, cy + length * 0.05)], cx, cy, angle)[0]
    right = rot([(cx + w * 0.46, cy + length * 0.05)], cx, cy, angle)[0]
    a.add(a.band([left, post, right], length * 0.08), strap, shade=False)
    a.add(a.circle(post[0], post[1], length * 0.055), strap, shade=False)


# ---------------------------------------------------------------------- the icons

def dante_stomp(a):
    a.add(a.rect(120, 800, 904, 890), STONE)                                   # the street
    for x0, x1, y1 in ((250, 140, 560), (190, 110, 700), (774, 884, 560), (834, 914, 700)):
        a.add(a.band([(x0, 790), (x1, y1)], 56), GOLD)                         # impact spikes
    a.add(a.poly([(360, 120), (620, 120), (620, 470), (790, 520), (830, 600), (830, 690), (340, 690), (340, 470)]), LEATHER)
    a.add(a.rect(300, 690, 870, 770), SOLE)
    a.add(a.rect(360, 120, 620, 200), SOLE)                                    # the cuff
    a.ink([(460, 330), (560, 330)], 20)
    a.ink([(460, 420), (560, 420)], 20)
    a.glint(410, 260, 22, 56)


def dante_shield(a):
    for sx in (-1, 1):                                                         # the horn crest
        base = 512 + sx * 230
        a.add(a.poly([(base - sx * 60, 380), (base + sx * 150, 100), (base + sx * 130, 250), (base + sx * 60, 400)]), HORN)
    a.add(a.poly([(512, 930), (200, 700), (190, 300), (512, 220), (834, 300), (824, 700)]), GOLD)
    a.add(a.poly([(512, 850), (270, 670), (262, 350), (512, 290), (762, 350), (754, 670)]), STONE_DK)
    a.add(a.band([(512, 340), (460, 480), (570, 570), (500, 760)], 36), EMBER, shade=False)   # a glowing crack
    a.glint(320, 400, 26, 76)


def dante_fissure(a):
    # The street split wide open, molten light in the crack, slabs and rocks thrown up.
    for pts in (((430, 110), (570, 90), (600, 220), (460, 250)), ((660, 250), (770, 230), (790, 340), (680, 360)),
                ((240, 250), (340, 230), (350, 330), (250, 350))):
        a.add(a.poly(pts), STONE)
    a.add(a.poly([(110, 560), (430, 470), (470, 860), (110, 860)]), STONE_DK)          # left slab, tilted up
    a.add(a.poly([(110, 560), (430, 470), (450, 560), (110, 640)]), STONE)
    a.add(a.poly([(914, 560), (600, 500), (570, 860), (914, 860)]), STONE_DK)          # right slab
    a.add(a.poly([(914, 560), (600, 500), (590, 590), (914, 640)]), STONE)
    a.add(a.poly([(430, 470), (520, 420), (600, 500), (570, 860), (470, 860)]), EMBER, shade=False)
    a.add(a.poly([(480, 520), (525, 470), (560, 520), (535, 860), (500, 860)]), LEMON, shade=False)


def sean_rush(a):
    for y, x0, x1 in ((330, 100, 330), (512, 80, 290), (694, 100, 330)):
        a.add(a.band([(x0, y), (x1, y)], 60), PERSIMMON, shade=False)
    head = [(640 + 240 * math.cos(math.radians(t)), 512 + 240 * math.sin(math.radians(t))) for t in range(-90, 91, 10)]
    tail = [(640, 752), (320, 660), (440, 590), (250, 512), (440, 434), (320, 364), (640, 272)]
    a.add(a.poly(head + tail), RIMRED)
    core = [(650 + 160 * math.cos(math.radians(t)), 512 + 160 * math.sin(math.radians(t))) for t in range(-90, 91, 10)]
    a.add(a.poly(core + [(650, 672), (470, 590), (540, 512), (470, 434), (650, 352)]), PERSIMMON)
    a.add(a.circle(690, 512, 84), LEMON, shade=False)
    a.glint(720, 470, 22)


def sean_ignite(a):
    slipper(a, 400, 620, 560, -32, RIMRED, HONEY)
    a.add(a.poly(flame_pts(700, 600, 300, 520, 3)), PERSIMMON)
    a.add(a.poly(flame_pts(700, 590, 190, 330, 2)), GOLD)
    a.add(a.poly(flame_pts(700, 580, 90, 170, 1)), LEMON, shade=False)


def sean_supernova(a):
    # Sean comes down from above as a comet and bursts on the street.
    a.add(a.poly(star_pts(610, 690, 310, 160, 8)), PERSIMMON)
    a.add(a.poly(star_pts(610, 690, 205, 112, 8, rot=-67.5)), GOLD)
    a.add(a.circle(610, 690, 84), LEMON, shade=False)
    # a comet: a round head trailing a flame that fans out behind it, toward the upper left
    def tail(hx, hy, r, length):
        ang = math.atan2(-1.0, -0.8)
        ux, uy = math.cos(ang), math.sin(ang)
        px, py = -uy, ux
        tip = (hx + ux * length, hy + uy * length)
        side = [(hx + px * r * 1.05, hy + py * r * 1.05),
                (hx + ux * length * 0.45 + px * r * 1.25, hy + uy * length * 0.45 + py * r * 1.25),
                tip,
                (hx + ux * length * 0.45 - px * r * 1.25, hy + uy * length * 0.45 - py * r * 1.25),
                (hx - px * r * 1.05, hy - py * r * 1.05)]
        return side
    a.add(np.maximum(a.poly(tail(450, 470, 120, 460)), a.circle(450, 470, 125)), RIMRED)
    a.add(np.maximum(a.poly(tail(455, 465, 75, 300)), a.circle(455, 465, 80)), PERSIMMON, shade=False)
    a.add(a.circle(465, 455, 42), LEMON, shade=False)
    a.glint(560, 630, 20)


def cheska_frost(a):
    a.add(a.ellipse(512, 730, 410, 165), ICE_SH)
    a.add(a.ellipse(512, 705, 370, 132), ICE)
    for cx, top, w in ((330, 430, 120), (700, 460, 120), (512, 220, 170)):
        a.add(a.poly([(cx, top), (cx + w / 2, top + 130), (cx + w / 2 - 14, 700), (cx - w / 2 + 14, 700), (cx - w / 2, top + 130)]), ICE)
        a.ink([(cx, top + 90), (cx, 640)], 12)
    a.glint(470, 330, 18, 56)
    a.glint(360, 700, 60, 16)


def cheska_barricade(a):
    a.add(a.rect(130, 820, 894, 900), ICE_SH)
    for cx, top, w in ((290, 400, 170), (734, 350, 170), (512, 150, 210)):
        a.add(a.poly([(cx, top), (cx + w / 2, top + 120), (cx + w / 2, 840), (cx - w / 2, 840), (cx - w / 2, top + 120)]), ICE)
        a.ink([(cx, top + 70), (cx, 800)], 12)
        a.glint(cx - w * 0.25, top + 220, 16, 66)


def cheska_nova(a):
    for k in range(6):
        ang = math.radians(-90 + k * 60)
        tip = (512 + 400 * math.cos(ang), 512 + 400 * math.sin(ang))
        a.add(a.band([(512, 512), tip], 110), ICE)
        mid = (512 + 250 * math.cos(ang), 512 + 250 * math.sin(ang))
        for side in (-1, 1):
            b = ang + side * math.radians(48)
            a.add(a.band([mid, (mid[0] + 130 * math.cos(b), mid[1] + 130 * math.sin(b))], 76), ICE)
    a.add(a.circle(512, 512, 140), ICE_SH)
    a.add(a.poly(star_pts(512, 512, 110, 50, 6)), ICE)
    a.glint(440, 300, 20)


def zack_sprint(a):
    for y, x0 in ((360, 90), (520, 70), (680, 100)):
        a.add(a.band([(x0, y), (x0 + 170, y)], 58), GOLD, shade=False)
    a.add(a.poly([(620, 90), (330, 540), (500, 540), (390, 940), (780, 440), (590, 440), (720, 90)]), LEMON)
    a.add(a.poly([(640, 160), (440, 500), (545, 500), (475, 760), (700, 470), (560, 470), (670, 160)]), GOLD, shade=False)
    a.glint(600, 180, 16, 40)


def zack_magnet(a):
    a.add(a.poly(star_pts(880, 512, 110, 40, 4, rot=0)), LEMON)
    a.add(np.maximum(np.maximum(a.arc_band(460, 512, 250, 90, 270, 180), a.band([(460, 262), (720, 262)], 180)),
                     a.band([(460, 762), (720, 762)], 180)), RIMRED)
    a.add(a.rect(690, 172, 800, 352), CREAM)
    a.add(a.rect(690, 672, 800, 852), CREAM)
    a.glint(320, 420, 22, 70)


def zack_thunder(a):
    a.add(a.poly([(560, 440), (400, 740), (510, 740), (430, 950), (720, 620), (590, 620), (680, 440)]), LEMON)
    cloud = a.rect(210, 360, 880, 500)
    for cx, cy, r in ((360, 350, 160), (560, 270, 200), (730, 370, 150), (520, 420, 170)):
        cloud = np.maximum(cloud, a.circle(cx, cy, r))
    a.add(cloud, STORM)
    a.glint(500, 170, 34, 20)


def nemu_phase(a):
    for y in (560, 700):
        a.add(a.band([(90, y), (240, y)], 54), LILAC, shade=False)
    body = [(300, 830), (300, 440)] + [(512 + 212 * math.cos(math.radians(180 + k)), 440 + 250 * math.sin(math.radians(180 + k))) for k in range(0, 181, 6)]
    body += [(724, 830), (654, 770), (584, 840), (512, 770), (440, 840), (370, 770)]
    a.add(a.poly(body), LILAC)
    a.ink_mask(a.ellipse(440, 450, 40, 56))
    a.ink_mask(a.ellipse(584, 450, 40, 56))
    a.ink_mask(a.ellipse(512, 560, 36, 26))
    a.glint(370, 300, 20, 50)


def nemu_pet(a):
    a.add(a.arc_band(512, 540, 410, 120, 170, 60), LILAC, shade=False)
    a.add(a.poly([(120, 720), (250, 830), (290, 690)]), LILAC, shade=False)
    head = [(250, 330), (330, 140), (440, 290), (584, 290), (694, 140), (774, 330), (800, 530), (700, 760), (324, 760), (224, 530)]
    a.add(a.poly(head), KURO)
    a.add(a.poly([(300, 260), (330, 190), (380, 280)]), PLUM, shade=False)
    a.add(a.poly([(724, 260), (694, 190), (644, 280)]), PLUM, shade=False)
    a.add(a.ellipse(410, 500, 60, 46), GOLD, shade=False)
    a.add(a.ellipse(614, 500, 60, 46), GOLD, shade=False)
    a.ink_mask(a.ellipse(410, 500, 14, 38))
    a.ink_mask(a.ellipse(614, 500, 14, 38))
    a.add(a.poly([(490, 600), (534, 600), (512, 630)]), PLUM, shade=False)


def nemu_seance(a):
    for k, col in ((0, PLUM), (1, LILAC), (2, PLUM)):
        base = k * 120
        pts = [(512 + (90 + t / 39 * 320) * math.cos(math.radians(base + t / 39 * 250)),
                512 + (90 + t / 39 * 320) * math.sin(math.radians(base + t / 39 * 250))) for t in range(40)]
        a.add(a.band(pts, 110), col)
    a.add(a.circle(512, 512, 130), KURO)
    a.add(a.ellipse(512, 512, 70, 46), GOLD, shade=False)
    a.ink_mask(a.ellipse(512, 512, 16, 40))


def phaister_hex(a):
    a.add(a.circle(512, 512, 400), MAGENTA)
    a.add(a.circle(512, 512, 320), WITCH)
    hexagon = [(512 + 250 * math.cos(math.radians(-90 + k * 60)), 512 + 250 * math.sin(math.radians(-90 + k * 60))) for k in range(6)]
    a.add(a.poly(hexagon), GOLD)
    inner = [(512 + 150 * math.cos(math.radians(-90 + k * 60)), 512 + 150 * math.sin(math.radians(-90 + k * 60))) for k in range(6)]
    a.add(a.poly(inner), WITCH, shade=False)
    a.add(a.poly(star_pts(512, 512, 100, 40, 4)), MAGENTA, shade=False)
    a.glint(300, 260, 24)


def phaister_blink(a):
    a.add(a.arc_band(512, 780, 340, 200, 300, 60), LILAC, shade=False)
    puff = a.circle(240, 720, 110)
    for cx, cy, r in ((320, 800, 80), (170, 800, 70)):
        puff = np.maximum(puff, a.circle(cx, cy, r))
    a.add(puff, WITCH)
    a.add(a.poly(star_pts(700, 370, 280, 80, 4)), GOLD)
    a.add(a.poly(star_pts(700, 370, 150, 50, 4)), LEMON, shade=False)
    a.glint(650, 250, 16, 40)


def phaister_coven(a):
    a.add(a.poly(star_pts(512, 512, 430, 310, 12)), MAGENTA)
    a.add(a.circle(512, 512, 300), GOLD)
    a.add(a.circle(545, 490, 250), KURO, shade=False)
    a.glint(320, 360, 26, 66)


def phaister_witchfire(a):
    a.add(a.poly(flame_pts(512, 900, 520, 820, 3)), MAGENTA)
    a.add(a.poly(flame_pts(512, 880, 320, 540, 2)), PLUM)
    a.add(a.poly(flame_pts(512, 860, 150, 270, 1)), LILAC, shade=False)


def rafi_crosscurrent(a):
    current = np.maximum(a.band([(120, 800), (420, 700)], 120), a.arc_band(430, 380, 320, 90, -8, 120))
    a.add(np.maximum(current, a.poly([(690, 230), (920, 400), (650, 480)])), SEA)
    a.add(a.arc_band(430, 380, 320, 70, 10, 30), FOAM, shade=False)
    slipper(a, 240, 380, 290, 70, PERSIMMON, CREAM)


def rafi_mirrorwake(a):
    def drop(cx, cy, r):
        return [(cx, cy - r * 1.75)] + [(cx + r * math.cos(math.radians(-25 + k * 230 / 40)), cy + r * math.sin(math.radians(-25 + k * 230 / 40))) for k in range(41)]
    a.add(a.band([(110, 880), (270, 830), (430, 880), (590, 830), (750, 880), (910, 830)], 50), SEA, shade=False)
    a.add(np.clip(a.poly(drop(360, 620, 190)) - a.poly(drop(360, 620, 115)), 0, 1), FOAM)   # the echo
    a.add(a.poly(drop(650, 580, 220)), SEA)
    a.glint(590, 520, 28, 58)


def rafi_breakwater(a):
    # One wave breaking: a tall green wall whose crest curls over, foam on the lip.
    body = [(90, 900), (90, 700), (250, 560), (420, 360), (560, 190), (720, 140), (860, 200), (920, 330),
            (860, 300), (770, 300), (700, 360), (690, 460), (760, 520), (840, 500), (820, 600), (720, 640),
            (760, 760), (934, 900)]
    a.add(a.poly(body), SEA)
    a.add(a.band([(300, 540), (470, 330), (600, 200), (730, 160), (850, 210), (900, 300)], 58), FOAM, shade=False)
    a.add(a.band([(700, 380), (720, 470), (790, 500)], 44), FOAM, shade=False)
    for cx, cy, r in ((950, 250, 30), (900, 150, 24), (980, 360, 20)):
        a.add(a.circle(cx, cy, r), FOAM, shade=False)
    a.add(a.band([(140, 850), (300, 800), (460, 850), (620, 800)], 40), FOAM, shade=False)


# the nine job glyphs, for any power without a bespoke picture

def zone(a):
    a.add(a.ellipse(512, 660, 400, 200), GOLD)
    a.add(a.ellipse(512, 650, 260, 120), PERSIMMON)
    a.add(a.ellipse(512, 640, 110, 50), LEMON, shade=False)


def wall(a):
    for row, y in enumerate((300, 440, 580, 720)):
        xs = [150, 390, 630, 874] if row % 2 == 0 else [150, 270, 510, 750, 874]
        for x0, x1 in zip(xs, xs[1:]):
            a.add(a.rect(x0 + 6, y + 6, x1 - 6, y + 134), PERSIMMON)


def dash(a):
    for x, col in ((200, PERSIMMON), (470, GOLD)):
        a.add(a.poly([(x, 200), (x + 180, 200), (x + 400, 512), (x + 180, 824), (x, 824), (x + 220, 512)]), col)


def shield(a):
    a.add(a.poly([(512, 920), (220, 700), (205, 260), (512, 160), (819, 260), (804, 700)]), GOLD)
    a.add(a.poly([(512, 830), (290, 670), (280, 320), (512, 240), (744, 320), (734, 670)]), PERSIMMON)
    a.glint(360, 380, 24, 76)


def burst(a):
    a.add(a.poly(star_pts(512, 512, 430, 210, 8)), GOLD)
    a.add(a.poly(star_pts(512, 512, 250, 130, 8, rot=-67.5)), PERSIMMON)
    a.add(a.circle(512, 512, 90), LEMON, shade=False)


def projectile(a):
    a.add(a.poly([(120, 430), (560, 430), (560, 240), (920, 512), (560, 784), (560, 594), (120, 594)]), GOLD)
    a.add(a.poly([(120, 380), (260, 430), (260, 594), (120, 644)]), PERSIMMON)


def phase(a):
    ring = np.zeros((S, S), np.float32)
    for k in range(10):
        ring = np.maximum(ring, a.arc_band(512, 512, 340, k * 36, k * 36 + 20, 90))
    a.add(ring, LILAC, shade=False)
    a.add(a.circle(512, 512, 170), PLUM)
    a.glint(460, 450, 24)


def slam(a):
    a.add(a.rect(120, 790, 904, 880), STONE)
    a.add(a.poly([(400, 110), (624, 110), (624, 430), (790, 430), (512, 730), (234, 430), (400, 430)]), GOLD)


def empower(a):
    slipper(a, 420, 580, 560, -30, PERSIMMON, HONEY)
    a.add(a.poly(star_pts(770, 280, 190, 60, 4, rot=0)), LEMON)


GLYPHS = {
    "Zone": zone, "Wall": wall, "Dash": dash, "Shield": shield, "Burst": burst,
    "Projectile": projectile, "Phase": phase, "Slam": slam, "Empower": empower,
    "DanteStomp": dante_stomp, "DanteShield": dante_shield, "DanteFissure": dante_fissure,
    "SeanRush": sean_rush, "SeanIgnite": sean_ignite, "SeanSupernova": sean_supernova,
    "CheskaFrostSheet": cheska_frost, "CheskaBarricade": cheska_barricade, "CheskaNova": cheska_nova,
    "ZackSprint": zack_sprint, "ZackOvercharge": zack_magnet, "ZackThunderstrike": zack_thunder,
    "NemuPhase": nemu_phase, "NemuAstralPet": nemu_pet, "NemuSeanceVoid": nemu_seance,
    "PhaisterHexSigil": phaister_hex, "PhaisterShadowBlink": phaister_blink,
    "PhaisterEclipse": phaister_coven, "PhaisterWitchfire": phaister_witchfire,
    "RafiCrosscurrent": rafi_crosscurrent, "RafiMirrorwake": rafi_mirrorwake,
    "RafiBreakwater": rafi_breakwater,
}


def build(name):
    a = Art()
    GLYPHS[name](a)
    return a.render()


def sheet(icons, path):
    """Each icon on the grounds it actually appears on: the dark HUD ring, a honey tile and
    the maroon screen ground, at 128, 72 and 44 px."""
    grounds = [(42, 22, 11), (252, 211, 159), (91, 15, 15)]
    names = list(icons)
    cell = 128 + 72 + 44 + 40
    img = Image.new("RGB", (cell * 8, ((len(names) + 7) // 8) * len(grounds) * 140 + 10), (20, 12, 6))
    d = ImageDraw.Draw(img)
    for k, n in enumerate(names):
        col, row = k % 8, k // 8
        for gi, ground in enumerate(grounds):
            x0, y0 = col * cell, (row * len(grounds) + gi) * 140
            d.rectangle([x0 + 2, y0 + 2, x0 + cell - 2, y0 + 138], fill=ground)
            x = x0 + 8
            for size in (128, 72, 44):
                im = icons[n].resize((size, size), Image.LANCZOS)
                img.paste(im, (x, y0 + 6), im)
                x += size + 8
            if gi == 0:
                d.text((x0 + 8, y0 + 124), n, fill=(255, 255, 255))
    img.save(path)


def main():
    os.makedirs(OUT, exist_ok=True)
    only = [a for a in sys.argv[1:] if a in GLYPHS]
    icons = {n: build(n) for n in (only or GLYPHS)}
    for n, im in icons.items():
        im.save(os.path.join(OUT, n + ".png"))
    if "--sheet" in sys.argv:
        sheet(icons, sys.argv[sys.argv.index("--sheet") + 1])
    print("wrote", len(icons), "icons to", OUT)


if __name__ == "__main__":
    main()
