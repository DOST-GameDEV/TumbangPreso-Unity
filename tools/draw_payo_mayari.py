"""PAYO (draft): paints MAYARI for the night half, three variants, in the game's own cartoon style.

    py -3 tools/draw_payo_mayari.py [--v=N]

Owner 2026-10-12: "if you take a look at the ancient philippine gods mayari is depicted as a moon goddess, can we
incorporate a design for her as an ancient being in the sky", then on the first try (thin lines, star fields):
"make a mayari that actually matches our current cartoony art style". So: fat rounded forms, a bold dark outline,
flat colour with one shade and one highlight, a big simple friendly face in the cast's manner (the box head with soft
corners, a closed eye, a small smile, a blush), chunky solid gold, hair as big cloud locks holding a few fat stars,
a solid cartoon moon. A night palette of four: deep indigo, moon cream, gold, and one pink accent.

Writes Logs/payo/draft/payo_mayari_a.png, _b.png, _c.png (2048 x 2048, RGBA: tools/author_payo_draft.py --night
--mayari=a|b|c hangs one beyond the valley's last range) and the sheet payo_mayari_variants_vN.png.

  A  PEEKING. She rests her chin on her folded arms on the far ridge and watches the game. The moon sits where
     the eye she lost to Apolaki was, tucked under a lock of hair like a flower: the moon IS her eye.
  B  THE LANTERN. Head and shoulders; she cradles the moon in both hands and its light is on her face. One eye
     closed, the other under her hair.
  C  THE CRESCENT. The moon itself has her face, asleep in profile, with her diadem and her hair of cloud.

A still. In the game: almost no movement. A slow blink, hair drifting, a shooting star leaving her hair.
"""
import math, os, random, sys
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Logs", "payo", "draft")
VER = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--v=")), "1")
S = 2048
INK = (15, 20, 58)
SKIN, SKIN_S, SKIN_H = (126, 141, 226), (97, 113, 200), (170, 182, 245)
HAIR, HAIR_L, HAIR_H = (44, 48, 136), (60, 66, 166), (92, 102, 204)
MOON, MOON_S = (255, 242, 198), (232, 214, 152)
GOLD, GOLD_S, GOLD_H = (245, 188, 58), (206, 142, 30), (255, 226, 140)
PINK = (226, 98, 128)
DRESS, DRESS_S = (74, 80, 186), (56, 60, 150)


def L():
    return Image.new("L", (S, S), 0)


def put(img, mask, colour, alpha=1.0):
    m = mask if alpha == 1.0 else mask.point(lambda v: int(v * alpha))
    img.alpha_composite(Image.merge("RGBA", (*[Image.new("L", (S, S), c) for c in colour], m)))


def grow(mask, w):
    return mask.filter(ImageFilter.GaussianBlur(w * 0.55)).point(lambda v: 255 if v > 26 else 0).filter(ImageFilter.GaussianBlur(1.3))


def paint(img, mask, fill, ow=13, ink=INK):
    """The house style: the shape's ink outline first, then its flat colour."""
    if ow:
        put(img, grow(mask, ow), ink)
    put(img, mask.filter(ImageFilter.GaussianBlur(1.2)), fill)


def side(img, mask, colour, dx, dy):
    """One flat shade (or highlight) along the side of a shape that faces (dx, dy)."""
    reg = ImageChops.subtract(mask, ImageChops.offset(mask, -dx, -dy))
    put(img, ImageChops.multiply(reg, mask).filter(ImageFilter.GaussianBlur(1.2)), colour)


def circles(cs):
    m = L()
    d = ImageDraw.Draw(m)
    for (x, y, r) in cs:
        d.ellipse([x - r, y - r, x + r, y + r], fill=255)
    return m


def rrect(x0, y0, x1, y1, r):
    m = L()
    ImageDraw.Draw(m).rounded_rectangle([x0, y0, x1, y1], radius=r, fill=255)
    return m


def ell(x, y, rx, ry):
    m = L()
    ImageDraw.Draw(m).ellipse([x - rx, y - ry, x + rx, y + ry], fill=255)
    return m


def poly(pts):
    m = L()
    ImageDraw.Draw(m).polygon(pts, fill=255)
    return m


def arc_pts(cx, cy, rx, ry, a0, a1, n=40):
    return [(cx + rx * math.cos(math.radians(a0 + (a1 - a0) * k / n)), cy + ry * math.sin(math.radians(a0 + (a1 - a0) * k / n))) for k in range(n + 1)]


def stroke(pts, w):
    m = L()
    d = ImageDraw.Draw(m)
    d.line(pts, fill=255, width=w, joint="curve")
    for p in (pts[0], pts[-1]):
        d.ellipse([p[0] - w / 2, p[1] - w / 2, p[0] + w / 2, p[1] + w / 2], fill=255)
    return m


def ink_line(img, pts, w):
    put(img, stroke(pts, w).filter(ImageFilter.GaussianBlur(1.2)), INK)


def star(img, x, y, r, rot=0.0, colour=MOON, ow=9):
    pts = []
    for k in range(10):
        a = rot + math.pi * k / 5 - math.pi / 2
        rr = r if k % 2 == 0 else r * 0.52
        pts.append((x + rr * math.cos(a), y + rr * math.sin(a)))
    m = poly(pts).filter(ImageFilter.GaussianBlur(r * 0.07)).point(lambda v: 255 if v > 110 else 0)
    paint(img, m, colour, ow)
    put(img, ell(x - r * 0.18, y - r * 0.2, r * 0.16, r * 0.12), (255, 255, 240))


def sparkle(img, x, y, r):
    m = poly([(x, y - r), (x + r * 0.22, y - r * 0.22), (x + r, y), (x + r * 0.22, y + r * 0.22), (x, y + r), (x - r * 0.22, y + r * 0.22), (x - r, y), (x - r * 0.22, y - r * 0.22)])
    paint(img, m, MOON, 6)


def moon(img, x, y, r):
    m = ell(x, y, r, r)
    put(img, m.filter(ImageFilter.GaussianBlur(r * 0.5)), MOON, 0.55)          # its glow
    paint(img, m, MOON, 14)
    side(img, m, MOON_S, int(r * 0.16), int(r * 0.14))
    for (a, b, c) in ((-0.36, -0.22, 0.2), (0.26, -0.38, 0.13), (0.3, 0.2, 0.24), (-0.2, 0.36, 0.13), (-0.02, 0.02, 0.08)):
        cm = ell(x + a * r, y + b * r, c * r, c * r * 0.9)
        put(img, cm.filter(ImageFilter.GaussianBlur(1.2)), MOON_S)
        put(img, ImageChops.subtract(cm, ImageChops.offset(cm, 0, int(-c * r * 0.3))).filter(ImageFilter.GaussianBlur(1.0)), (212, 190, 126))
    put(img, ell(x - r * 0.45, y - r * 0.52, r * 0.2, r * 0.1), (255, 252, 232))


def cloud_lock(img, cs, colour=HAIR, hi=HAIR_L):
    """A lock of hair as a soft cloud: overlapping rounds, one outline, a lighter lobe on each round."""
    m = circles(cs)
    paint(img, m, colour, 13)
    for (x, y, r) in cs:
        put(img, ImageChops.multiply(ell(x - r * 0.18, y - r * 0.2, r * 0.66, r * 0.6), m).filter(ImageFilter.GaussianBlur(1.2)), hi)
    for (x, y, r) in cs[::2]:
        put(img, stroke(arc_pts(x - r * 0.1, y - r * 0.1, r * 0.5, r * 0.46, 200, 262, 12), max(8, int(r * 0.07))).filter(ImageFilter.GaussianBlur(1.2)), HAIR_H)


def gold_disc(img, x, y, r, gem=True):
    m = ell(x, y, r, r)
    paint(img, m, GOLD, 11)
    side(img, m, GOLD_S, int(r * 0.22), int(r * 0.2))
    put(img, ell(x - r * 0.3, y - r * 0.34, r * 0.24, r * 0.14), GOLD_H)
    if gem:
        paint(img, ell(x, y, r * 0.38, r * 0.38), PINK, 7)
        put(img, ell(x - r * 0.1, y - r * 0.12, r * 0.12, r * 0.08), (255, 190, 204))


def gold_band(img, pts, w):
    m = stroke(pts, w)
    paint(img, m, GOLD, 11)
    side(img, m, GOLD_S, 0, int(w * 0.3))
    put(img, stroke([(p[0], p[1] - w * 0.22) for p in pts[2:-2]], max(6, int(w * 0.14))).filter(ImageFilter.GaussianBlur(1.2)), GOLD_H)


def leaf(img, x, y, h, w, tilt=0.0):
    pts = []
    for k in range(21):
        t = k / 20
        a = math.pi * t
        px, py = math.sin(a) * w * (0.5 if t < 0.5 else 0.5), -t * h
        pts.append((px * (1 if True else 1), py))
    left = [(-math.sin(math.pi * k / 20) * w * 0.5, -k / 20 * h) for k in range(21)]
    right = [(math.sin(math.pi * k / 20) * w * 0.5, -k / 20 * h) for k in range(20, -1, -1)]
    c, s = math.cos(tilt), math.sin(tilt)
    m = poly([(x + px * c - py * s, y + px * s + py * c) for (px, py) in left + right])
    paint(img, m, GOLD, 10)
    side(img, m, GOLD_S, int(w * 0.14), 0)


def closed_eye(img, x, y, w, flip=1):
    """The cast's eye, asleep: one fat downward curve and three lashes at its outer end."""
    ink_line(img, arc_pts(x, y - w * 0.25, w, w * 0.62, 22, 158, 24), max(14, int(w * 0.24)))
    for k, a in enumerate((28, 52, 76)):
        aa = math.radians(a if flip > 0 else 180 - a)
        px, py = x + w * math.cos(aa), y - w * 0.25 + w * 0.62 * math.sin(aa)
        ink_line(img, [(px, py), (px + flip * w * 0.2 * (1.2 - k * 0.3), py + w * 0.32)], max(10, int(w * 0.16)))


def smile(img, x, y, w):
    ink_line(img, arc_pts(x, y - w * 0.3, w, w * 0.7, 28, 152, 20), max(12, int(w * 0.26)))


def blush(img, x, y, w):
    put(img, ell(x, y, w, w * 0.56).filter(ImageFilter.GaussianBlur(3)), PINK, 0.85)
    put(img, ell(x - w * 0.3, y - w * 0.16, w * 0.22, w * 0.12), (246, 160, 180))


def head(img, cx, cy, w=720, h=690, light_below=False):
    """The game's head: a box with soft corners, not a ball."""
    m = rrect(cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2, 200)
    paint(img, m, SKIN, 15)
    side(img, m, SKIN_S, 34, 26)
    if light_below:
        put(img, ImageChops.multiply(ell(cx, cy + h * 0.52, w * 0.5, h * 0.3), m).filter(ImageFilter.GaussianBlur(2)), SKIN_H)
    else:
        put(img, ImageChops.multiply(ell(cx - w * 0.2, cy - h * 0.12, w * 0.2, h * 0.14), m).filter(ImageFilter.GaussianBlur(2)), SKIN_H, 0.7)
    return m


def diadem(img, cx, cy, rx, ry, a0=214, a1=326):
    for k, a in enumerate((238, 254, 270, 286, 302)):
        px, py = cx + rx * math.cos(math.radians(a)), cy + ry * math.sin(math.radians(a))
        leaf(img, px, py + 10, (150, 200, 250, 200, 150)[k], (86, 96, 110, 96, 86)[k], math.radians((a - 270) * 0.7))
    gold_band(img, arc_pts(cx, cy, rx, ry, a0, a1, 40), 74)
    gold_disc(img, cx, cy - ry + 2, 62)
    for a in (236, 304):
        px, py = cx + rx * math.cos(math.radians(a)), cy + ry * math.sin(math.radians(a))
        paint(img, ell(px, py, 22, 22), PINK, 6)


def ear_drop(img, x, y):
    gold_disc(img, x, y, 74)
    gold_disc(img, x, y + 118, 42, gem=False)
    gold_disc(img, x, y + 190, 28, gem=False)


def fade(img, y0, y1, sides=380):
    a = img.getchannel("A")
    ramp = Image.new("L", (S, S), 255)
    d = ImageDraw.Draw(ramp)
    for y in range(int(y0), S):
        d.line([(0, y), (S, y)], fill=max(0, int(255 * (1 - (y - y0) / (y1 - y0)))))
    if sides:
        sd = Image.new("L", (S, S), 255)
        ds = ImageDraw.Draw(sd)
        for x in range(sides):
            v = int(255 * (x / sides) ** 1.3)
            ds.line([(x, 0), (x, S)], fill=v)
            ds.line([(S - 1 - x, 0), (S - 1 - x, S)], fill=v)
        ramp = ImageChops.multiply(ramp, sd)
    img.putalpha(ImageChops.multiply(a, ramp))


def painted_light(img):
    """So she is not a flat sticker: a moonlit rim along every upper left edge, a soft shade toward the lower right
    and the bottom, and a pale bloom high on the left where the moon's light falls."""
    a = img.getchannel("A")
    solid = a.point(lambda v: 255 if v > 128 else 0)
    low = ImageChops.multiply(solid, ImageChops.offset(solid, -34, -40))
    shade = ImageChops.subtract(solid, low).filter(ImageFilter.GaussianBlur(12))
    put(img, ImageChops.multiply(shade, solid), (22, 26, 84), 0.34)
    grad = Image.linear_gradient("L").resize((S, S))                       # black at the top, white at the bottom
    put(img, ImageChops.multiply(grad.point(lambda v: max(0, v - 110)), solid), (18, 22, 70), 0.5)
    rim = ImageChops.subtract(solid, ImageChops.offset(solid, 9, 11)).filter(ImageFilter.GaussianBlur(1.6))
    put(img, ImageChops.multiply(rim, solid), (196, 226, 255), 0.72)
    bloom = Image.new("L", (S, S), 0)
    ImageDraw.Draw(bloom).ellipse([340, 300, 1100, 1000], fill=255)
    put(img, ImageChops.multiply(bloom.filter(ImageFilter.GaussianBlur(150)), solid), (150, 176, 255), 0.22)
    img.putalpha(a)


def stars_in_hair(img, rnd, spots):
    for (x, y, r) in spots:
        star(img, x, y, r, rnd.uniform(-0.3, 0.3))


def variant_a_layers():
    """PEEKING over the ridge, chin on her folded arms; the moon is the eye she lost. Three layers, back to front:
    the hair and its stars; the far arm, the head and all it wears; the near arm and hand."""
    rnd = random.Random(5)
    hair_layer, figure_layer, front_layer = (Image.new("RGBA", (S, S), (0, 0, 0, 0)) for _ in range(3))
    img = hair_layer
    cx, cy = 1024, 1010
    # hair behind: a big cloud, and locks drifting off to the upper right like the star river
    cloud_lock(img, [(1540, 800, 290), (1720, 650, 230), (1836, 520, 170), (1900, 410, 110)], HAIR, HAIR_L)      # the lock that drifts off ends inside the picture, curling up
    cloud_lock(img, [(cx, 900, 520), (540, 1010, 300), (430, 1260, 250), (380, 1480, 200), (1500, 1030, 300), (1640, 1280, 240), (1680, 1500, 190),
                     (800, 500, 260), (1070, 430, 290), (1330, 520, 250)])
    stars_in_hair(img, rnd, [(640, 520, 74), (1420, 560, 60), (1710, 700, 86), (1900, 500, 58), (470, 1180, 64), (1620, 1200, 54), (1010, 330, 50)])
    for (x, y, r) in ((300, 760, 44), (1540, 330, 38), (1250, 250, 30), (220, 1320, 34)):
        sparkle(img, x, y, r)
    # the arm behind, then the head, then the arm in front, so her chin rests on them
    img = figure_layer
    back = rrect(300, 1500, 1750, 1790, 140)
    paint(img, back, SKIN, 15)
    side(img, back, SKIN_S, 0, 34)
    hm = head(img, cx, cy)
    # face: the open side has a sleeping eye; the lost side has the moon
    closed_eye(img, cx + 170, cy + 10, 82)
    smile(img, cx + 20, cy + 170, 58)
    blush(img, cx + 236, cy + 128, 68)
    moon(img, cx - 196, cy + 6, 150)
    # the fringe, with one fat lock that holds the moon in place
    cloud_lock(img, [(cx - 250, 700, 190), (cx - 40, 660, 200), (cx + 180, 680, 190), (cx + 330, 760, 140)], HAIR, HAIR_L)
    cloud_lock(img, [(cx - 350, 800, 130), (cx - 372, 960, 100), (cx - 356, 1090, 76)], HAIR, HAIR_L)
    diadem(img, cx, 930, 380, 400)
    ear_drop(img, cx + 380, cy + 110)
    img = front_layer
    front = rrect(450, 1420, 1600, 1670, 120)
    paint(img, front, SKIN, 15)
    side(img, front, SKIN_S, 0, 30)
    put(img, stroke([(560, 1470), (1480, 1470)], 22).filter(ImageFilter.GaussianBlur(1.2)), SKIN_H)
    for x in (640, 1340):                                   # chunky bracelets
        b = rrect(x, 1404, x + 96, 1686, 40)
        paint(img, b, GOLD, 11)
        side(img, b, GOLD_S, 20, 0)
        put(img, rrect(x + 16, 1430, x + 34, 1660, 9), GOLD_H)
    hand = rrect(1420, 1440, 1640, 1650, 100)                # a mitten hand over the other arm
    paint(img, hand, SKIN, 14)
    side(img, hand, SKIN_S, 0, 26)
    ink_line(img, [(1500, 1500), (1500, 1596)], 14)
    ink_line(img, [(1560, 1500), (1560, 1596)], 14)
    for layer in (hair_layer, figure_layer, front_layer):
        painted_light(layer)
        fade(layer, 1640, 1900, sides=60)
    return [hair_layer, figure_layer, front_layer]


def variant_a():
    out = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    for layer in variant_a_layers():
        out.alpha_composite(layer)
    return out


def variant_b():
    """THE LANTERN: she cradles the moon in both hands and its light is on her face."""
    rnd = random.Random(8)
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    cx, cy = 1024, 860
    cloud_lock(img, [(1580, 660, 290), (1830, 480, 240), (2000, 330, 190)], HAIR, HAIR_L)
    cloud_lock(img, [(cx, 760, 510), (540, 880, 300), (440, 1140, 260), (400, 1400, 220), (1500, 900, 300), (1620, 1170, 250), (1660, 1420, 210),
                     (800, 360, 250), (1070, 300, 280), (1320, 380, 240)])
    body = rrect(430, 1290, 1620, 2300, 300)
    paint(img, body, DRESS, 15)
    side(img, body, DRESS_S, 40, 0)
    neck = rrect(cx - 120, 1120, cx + 120, 1380, 60)
    paint(img, neck, SKIN, 14)
    side(img, neck, SKIN_S, 26, 0)
    collar = stroke(arc_pts(cx, 1250, 400, 250, 14, 166, 40), 110)       # a wide beaten collar
    paint(img, collar, GOLD, 12)
    side(img, collar, GOLD_S, 0, 34)
    put(img, stroke(arc_pts(cx, 1226, 400, 250, 24, 156, 40), 16).filter(ImageFilter.GaussianBlur(1.2)), GOLD_H)
    for a in (40, 65, 90, 115, 140):
        px, py = cx + 400 * math.cos(math.radians(a)), 1250 + 250 * math.sin(math.radians(a))
        paint(img, ell(px, py + 4, 24, 24), PINK, 6)
    hm = head(img, cx, cy, light_below=True)
    closed_eye(img, cx + 170, cy + 20, 82)
    smile(img, cx + 20, cy + 176, 58)
    blush(img, cx + 236, cy + 136, 68)
    cloud_lock(img, [(cx - 250, 560, 190), (cx - 40, 520, 200), (cx + 180, 540, 190), (cx + 330, 620, 140)], HAIR, HAIR_L)
    cloud_lock(img, [(cx - 300, 700, 170), (cx - 290, 880, 150), (cx - 250, 1030, 110)], HAIR, HAIR_L)   # the lock over her lost eye
    diadem(img, cx, 790, 380, 400)
    ear_drop(img, cx + 380, cy + 110)
    for sx in (-1, 1):                                       # arms coming up to hold the moon
        arm = poly([(cx + sx * 560, 1640), (cx + sx * 430, 1500), (cx + sx * 150, 1700), (cx + sx * 210, 1890), (cx + sx * 520, 1860)])
        arm = arm.filter(ImageFilter.GaussianBlur(30)).point(lambda v: 255 if v > 120 else 0)
        paint(img, arm, SKIN, 15)
        side(img, arm, SKIN_S, 0, 30)
        b = ell(cx + sx * 400, 1690, 60, 130)
        paint(img, b, GOLD, 11)
        side(img, b, GOLD_S, 18, 0)
    moon(img, cx, 1560, 230)
    for sx in (-1, 1):                                       # mitten hands cupped under it
        hand = ell(cx + sx * 170, 1790, 150, 96)
        paint(img, hand, SKIN, 14)
        side(img, hand, SKIN_S, 0, 26)
        ink_line(img, [(cx + sx * 150, 1740), (cx + sx * 120, 1800)], 14)
        ink_line(img, [(cx + sx * 215, 1730), (cx + sx * 200, 1800)], 14)
    stars_in_hair(img, rnd, [(640, 380, 74), (1420, 420, 60), (1720, 560, 86), (1910, 380, 58), (470, 1050, 64), (1620, 1080, 54), (1010, 200, 50)])
    for (x, y, r) in ((300, 640, 44), (1540, 200, 38), (720, 1560, 34), (1330, 1500, 30)):
        sparkle(img, x, y, r)
    fade(img, 1760, 2030)
    return img


def variant_c():
    """THE CRESCENT: the moon itself has her face, asleep in profile, with her diadem and her hair of cloud."""
    rnd = random.Random(13)
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    cx, cy, R = 1060, 1040, 600
    cloud_lock(img, [(600, 760, 300), (380, 620, 250), (200, 500, 200), (90, 380, 150)], HAIR, HAIR_L)
    cloud_lock(img, [(560, 1100, 290), (360, 1280, 250), (230, 1500, 200), (620, 1420, 220), (760, 560, 240)], HAIR, HAIR_L)
    full = ell(cx, cy, R, R)
    cut = ell(cx + 330, cy - 120, R * 0.9, R * 0.9)
    cres = ImageChops.subtract(full, cut)
    put(img, cres.filter(ImageFilter.GaussianBlur(120)), MOON, 0.5)
    paint(img, cres, MOON, 16)
    side(img, cres, MOON_S, 30, 26)
    for (a, b, c) in ((-0.74, -0.36, 0.1), (-0.8, 0.3, 0.13), (-0.5, 0.66, 0.1), (-0.36, -0.72, 0.09)):
        cm = ImageChops.multiply(ell(cx + a * R, cy + b * R, c * R, c * R * 0.9), cres)
        put(img, cm.filter(ImageFilter.GaussianBlur(1.2)), MOON_S)
    closed_eye(img, cx - 330, cy - 60, 92, flip=-1)
    smile(img, cx - 250, cy + 150, 60)
    blush(img, cx - 400, cy + 110, 76)
    gold_band(img, arc_pts(cx, cy, R * 0.99, R * 0.99, 196, 262, 40), 78)
    for k, a in enumerate((206, 224, 242)):
        px, py = cx + R * math.cos(math.radians(a)), cy + R * math.sin(math.radians(a))
        leaf(img, px, py, (170, 220, 170)[k], 96, math.radians(a - 270))
    gold_disc(img, cx + R * math.cos(math.radians(224)), cy + R * math.sin(math.radians(224)), 60)
    ear_drop(img, cx - 560, cy + 250)
    stars_in_hair(img, rnd, [(420, 520, 78), (220, 420, 56), (330, 1220, 70), (640, 1500, 56), (700, 420, 50), (1500, 560, 64), (1640, 1260, 50)])
    for (x, y, r) in ((1420, 900, 40), (1250, 1480, 34), (880, 300, 34), (150, 900, 36)):
        sparkle(img, x, y, r)
    fade(img, 1640, 1900, sides=260)
    return img


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    pics = [variant_a(), variant_b(), variant_c()]
    for im, k in zip(pics, "abc"):
        im.save(os.path.join(OUT, "payo_mayari_%s.png" % k))
    W = 2440
    sheet = Image.new("RGBA", (W, 900), (10, 16, 44, 255))
    rnd = random.Random(3)
    ds = ImageDraw.Draw(sheet)
    for y in range(900):                                   # the night's own gradient, and its small stars
        t = y / 900.0
        ds.line([(0, y), (W, y)], fill=(int(8 + 22 * t), int(13 + 30 * t), int(36 + 46 * t), 255))
    for _ in range(420):
        x, y, r = rnd.uniform(0, W), rnd.uniform(0, 900), rnd.choice((1, 1, 2, 2, 3))
        ds.ellipse([x - r, y - r, x + r, y + r], fill=(226, 232, 255, 255))
    for i, im in enumerate(pics):
        sheet.alpha_composite(im.resize((780, 780), Image.LANCZOS), (20 + i * 810, 96))
    try:
        f, f2 = ImageFont.truetype("arialbd.ttf", 30), ImageFont.truetype("arial.ttf", 21)
    except OSError:
        f = f2 = ImageFont.load_default()
    ds = ImageDraw.Draw(sheet)
    for i, (t, sub) in enumerate((("A  PEEKING", "chin on her arms on the far ridge; the moon is the eye she lost"),
                                  ("B  THE LANTERN", "she cradles the moon; one eye asleep, one under her hair"),
                                  ("C  THE CRESCENT", "the moon has her face, asleep in profile"))):
        ds.text((40 + i * 810, 18), "MAYARI  " + t, font=f, fill=(255, 242, 198))
        ds.text((40 + i * 810, 58), sub, font=f2, fill=(190, 202, 246))
    sheet.convert("RGB").save(os.path.join(OUT, "payo_mayari_variants_v%s.png" % VER))
    print("wrote three figures and the sheet")
