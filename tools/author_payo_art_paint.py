"""PAYO, the art's OWN DRAWINGS (2026-10-12, second art pass). Run after tools/author_payo_art_textures.py.

  py -3 tools/author_payo_art_paint.py [--sheet N]

The owner on the first pass: "everything looks untextured ... have you actually taken a look at how we made the other
maps?" The first pass greyed Kanto's stone and tinted it. This paints Payo's surfaces as the shipped maps' are painted
(Logs/payo/art/STUDY.md): flat hand-drawn shapes, a few large feathered patches, a soft dark joint, and EACH SURFACE ITS
OWN DRAWING, 1024 square at about 256 px a metre.

  stone_payo      fitted grey blocks: cool, warm and green-grey stones, a pale chipped edge on each, lichen, moss in joints
  paving_payo     the court's floor: big irregular slabs worn pale in the middle, moss in the joints
  riverstone      the paddy walls: rounded river stones fitted dry, earth and moss between
  carve_payo      2048 x 512, four relief panels (lizard, sun, rice, ancestor) between woven borders, ochre and red
                  paint half worn away. One tile of u is four panels.
  thatch_payo     cogon thatch in courses: strands, a shadow under each course's lip
  timber_payo     adzed boards: long grain strokes, a few knots, a darker carved chevron band
  rice_payo       young rice in rows standing in water: tufts, water between, small sky glints
  dike_payo       a bund's top: trodden grass with bare mud patches and tufts
  decals_payo     an alpha atlas, 4 x 4 cells: lichen, creeping moss, drip stains, cracks, worn paint, leaf litter
  pine_payo       a leaf card: a bough of pine needles
  spill_payo      warm light on a wall beside a window (night, alpha)
"""
import math
import random
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Assets/TumbangPreso/Art/Payo/Textures"
LOGS = ROOT / "Logs/payo/art"
S = 1024
INK = (52, 58, 68)
# THE PALETTE IS THE OWNER'S REFERENCE 238 ("follow that cool teal tropical tone"): stone in cool BLUE-GREEN greys, a few
# warmer stones among them, never neutral concrete grey; warm ochre and red only as paint on the carvings; turquoise inlay.
GREYS = [(126, 160, 162), (140, 172, 170), (112, 148, 154), (154, 180, 174), (132, 164, 156), (118, 152, 146), (160, 186, 184), (104, 140, 150), (168, 172, 158), (150, 160, 148)]
INK = (38, 62, 72)


def L(size=(S, S)):
    return Image.new("L", size, 0)


def put(img, mask, colour, alpha=1.0):
    m = mask if alpha == 1.0 else mask.point(lambda v: int(v * alpha))
    img.paste(Image.new("RGB", img.size, colour), (0, 0), m)


def blob_poly(cx, cy, rx, ry, rnd, n=11, jit=0.18):
    return [(cx + rx * (1 + rnd.uniform(-jit, jit)) * math.cos(math.tau * k / n), cy + ry * (1 + rnd.uniform(-jit, jit)) * math.sin(math.tau * k / n)) for k in range(n)]


def wrapped(draw_fn, size=(S, S)):
    """Draw with every wrap offset so the tile repeats."""
    for ox in (-size[0], 0, size[0]):
        for oy in (-size[1], 0, size[1]):
            draw_fn(ox, oy)


def shade(c, f):
    return tuple(max(0, min(255, int(v * f))) for v in c)


def stone_shape(img, poly, base, rnd, size=(S, S), chip=True):
    """One hand-drawn stone: its fill, a lighter worn patch, a darker foot, a pale chipped edge up and to the left."""
    m = L(size)
    ImageDraw.Draw(m).polygon(poly, fill=255)
    m = m.filter(ImageFilter.GaussianBlur(2.2)).point(lambda v: 255 if v > 120 else 0).filter(ImageFilter.GaussianBlur(1.0))
    put(img, m, base)
    xs, ys = [p[0] for p in poly], [p[1] for p in poly]
    cx, cy, w, h = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, max(xs) - min(xs), max(ys) - min(ys)
    p1 = L(size)
    ImageDraw.Draw(p1).polygon(blob_poly(cx + rnd.uniform(-0.2, 0.2) * w, cy + rnd.uniform(-0.25, 0.1) * h, w * rnd.uniform(0.22, 0.38), h * rnd.uniform(0.2, 0.34), rnd), fill=255)
    put(img, ImageChops.multiply(p1.filter(ImageFilter.GaussianBlur(9)), m), shade(base, 1.09), 0.9)
    foot = ImageChops.multiply(ImageChops.subtract(m, ImageChops.offset(m, -5, -9)), m)
    put(img, foot.filter(ImageFilter.GaussianBlur(2.5)), shade(base, 0.82), 0.85)
    edge = ImageChops.multiply(ImageChops.subtract(m, ImageChops.offset(m, 5, 6)), m)
    put(img, edge.filter(ImageFilter.GaussianBlur(1.6)), shade(base, 1.22), 0.9)
    if chip and rnd.random() < 0.55:                       # a chip off a corner, pale inside
        k = rnd.randrange(len(poly))
        px, py = poly[k]
        c = L(size)
        ImageDraw.Draw(c).polygon(blob_poly(px + (cx - px) * 0.12, py + (cy - py) * 0.12, rnd.uniform(9, 20), rnd.uniform(8, 16), rnd, n=7, jit=0.3), fill=255)
        put(img, ImageChops.multiply(c.filter(ImageFilter.GaussianBlur(1.2)), m), shade(base, 1.3))
    return m


def lichen_and_moss(img, rnd, joints, n_lichen=26, n_moss=34, size=(S, S)):
    def one(ox, oy):
        pass
    for _ in range(n_lichen):                              # lichen: pale lobed rosettes, sage and cream
        x, y, r = rnd.uniform(0, size[0]), rnd.uniform(0, size[1]), rnd.uniform(12, 34)
        col = rnd.choice(((196, 204, 178), (182, 196, 170), (206, 204, 186)))
        m = L(size)
        d = ImageDraw.Draw(m)
        for k in range(rnd.randint(4, 7)):
            a = rnd.uniform(0, math.tau)
            rr = r * rnd.uniform(0.35, 0.6)
            for ox in (-size[0], 0, size[0]):
                for oy in (-size[1], 0, size[1]):
                    d.ellipse([x + ox + r * 0.5 * math.cos(a) - rr, y + oy + r * 0.5 * math.sin(a) - rr * 0.8, x + ox + r * 0.5 * math.cos(a) + rr, y + oy + r * 0.5 * math.sin(a) + rr * 0.8], fill=255)
        put(img, m.filter(ImageFilter.GaussianBlur(1.6)), col, 0.75)
    for _ in range(n_moss):                                # moss creeping out of the joints
        (x, y) = rnd.choice(joints)
        m = L(size)
        d = ImageDraw.Draw(m)
        for k in range(rnd.randint(3, 6)):
            rr = rnd.uniform(7, 20)
            dx, dy = rnd.uniform(-26, 26), rnd.uniform(-10, 12)
            for ox in (-size[0], 0, size[0]):
                for oy in (-size[1], 0, size[1]):
                    d.ellipse([x + dx + ox - rr, y + dy + oy - rr * 0.7, x + dx + ox + rr, y + dy + oy + rr * 0.7], fill=255)
        mm = m.filter(ImageFilter.GaussianBlur(1.8))
        put(img, mm, rnd.choice(((96, 130, 66), (84, 116, 60), (110, 142, 74))))
        hi = ImageChops.multiply(ImageChops.subtract(mm, ImageChops.offset(mm, 3, 4)), mm)
        put(img, hi, (146, 176, 92), 0.8)


def stone_payo():
    rnd = random.Random(101)
    img = Image.new("RGB", (S, S), INK)
    joints = []
    heights = [124, 136, 118, 140, 128, 122, 132, 124]
    y = 0
    for row, h in enumerate(heights):
        x = -rnd.uniform(40, 200)
        while x < S + 40:
            w = rnd.uniform(150, 330)
            g = 5
            j = lambda: rnd.uniform(-5, 5)
            poly = [(x + g + j(), y + g + j()), (x + w / 2 + j(), y + g - 1 + j()), (x + w - g + j(), y + g + j()), (x + w - g + j(), y + h - g + j()), (x + w / 2 + j(), y + h - g + 1 + j()), (x + g + j(), y + h - g + j())]
            base = shade(rnd.choice(GREYS), rnd.uniform(0.92, 1.08))
            for ox in (-S, 0, S):
                for oy in (-S, 0, S):
                    if -360 < x + ox < S + 20 and -160 < y + oy < S + 20:
                        r2 = random.Random(int(x * 7 + y * 13))
                        stone_shape(img, [(px + ox, py + oy) for (px, py) in poly], base, r2)
            joints.append(((x + w) % S, (y + rnd.uniform(0, h)) % S))
            joints.append(((x + rnd.uniform(0, w)) % S, (y + h) % S))
            x += w
        y += h
    lichen_and_moss(img, rnd, joints)
    return img.filter(ImageFilter.GaussianBlur(0.6))


def paving_payo():
    rnd = random.Random(202)
    img = Image.new("RGB", (S, S), (60, 68, 72))
    n = 4
    P = [[(i * S / n + rnd.uniform(-34, 34), j * S / n + rnd.uniform(-34, 34)) for i in range(n)] for j in range(n)]

    def pt(i, j):
        x, y = P[j % n][i % n]
        return x + (i // n) * S, y + (j // n) * S

    joints = []
    for j in range(-1, n + 1):
        for i in range(-1, n + 1):
            a, b, c, d = pt(i, j), pt(i + 1, j), pt(i + 1, j + 1), pt(i, j + 1)
            cx, cy = (a[0] + b[0] + c[0] + d[0]) / 4, (a[1] + b[1] + c[1] + d[1]) / 4
            sh = lambda p: (p[0] + (cx - p[0]) * 0.035 + 0, p[1] + (cy - p[1]) * 0.035)
            mid = lambda p, q: ((p[0] + q[0]) / 2 + random.Random(int(p[0] * 3 + q[1])).uniform(-5, 5), (p[1] + q[1]) / 2 + random.Random(int(p[1] * 5 + q[0])).uniform(-5, 5))
            poly = [sh(a), sh(mid(a, b)), sh(b), sh(mid(b, c)), sh(c), sh(mid(c, d)), sh(d), sh(mid(d, a))]
            r2 = random.Random((i % n) * 31 + (j % n) * 57)
            base = shade(r2.choice(GREYS), r2.uniform(0.98, 1.1))
            m = stone_shape(img, poly, base, r2, chip=True)
            worn = L()
            ImageDraw.Draw(worn).polygon(blob_poly(cx, cy, S / n * 0.3, S / n * 0.26, r2), fill=255)       # worn pale where feet go
            put(img, ImageChops.multiply(worn.filter(ImageFilter.GaussianBlur(26)), m), shade(base, 1.12), 0.8)
            if r2.random() < 0.4:                               # a crack across the slab
                cr = L()
                x0, y0 = a[0] + (b[0] - a[0]) * r2.uniform(0.2, 0.8), a[1] + 6
                pts = [(x0, y0)]
                for k in range(6):
                    pts.append((pts[-1][0] + r2.uniform(-22, 22), pts[-1][1] + S / n / 6.5))
                ImageDraw.Draw(cr).line(pts, fill=255, width=4)
                put(img, ImageChops.multiply(cr.filter(ImageFilter.GaussianBlur(0.9)), m), shade(base, 0.62))
            joints.append((a[0] % S, a[1] % S)); joints.append((((a[0] + b[0]) / 2) % S, ((a[1] + b[1]) / 2) % S))
    lichen_and_moss(img, rnd, joints, n_lichen=12, n_moss=14)
    return img.filter(ImageFilter.GaussianBlur(0.6))


def riverstone():
    rnd = random.Random(303)
    img = Image.new("RGB", (S, S), (74, 78, 66))
    for _ in range(40):                                     # earth and moss between the stones
        x, y, r = rnd.uniform(0, S), rnd.uniform(0, S), rnd.uniform(40, 110)
        m = L()
        d = ImageDraw.Draw(m)
        for ox in (-S, 0, S):
            for oy in (-S, 0, S):
                d.ellipse([x + ox - r, y + oy - r * 0.6, x + ox + r, y + oy + r * 0.6], fill=255)
        put(img, m.filter(ImageFilter.GaussianBlur(14)), rnd.choice(((86, 104, 62), (70, 72, 62), (96, 116, 70))), 0.8)
    pts = []
    tries = 0
    while tries < 6000:
        tries += 1
        x, y, r = rnd.uniform(0, S), rnd.uniform(0, S), rnd.uniform(34, 70)
        ok = True
        for (px, py, pr) in pts:
            dx, dy = min(abs(x - px), S - abs(x - px)), min(abs(y - py), S - abs(y - py))
            if math.hypot(dx, dy * 1.25) < (r + pr) * 0.86:
                ok = False
                break
        if ok:
            pts.append((x, y, r))
    for (x, y, r) in pts:
        r2 = random.Random(int(x * 11 + y * 17))
        base = shade(r2.choice(GREYS), r2.uniform(0.92, 1.1))
        poly = blob_poly(0, 0, r * 0.98, r * 0.74, r2, n=10, jit=0.12)
        for ox in (-S, 0, S):
            for oy in (-S, 0, S):
                if -80 < x + ox < S + 80 and -80 < y + oy < S + 80:
                    stone_shape(img, [(px + x + ox, py + y + oy) for (px, py) in poly], base, random.Random(int(x * 11 + y * 17)), chip=False)
    lichen_and_moss(img, rnd, [(p[0], p[1] + p[2] * 0.7) for p in pts], n_lichen=22, n_moss=30)
    return img.filter(ImageFilter.GaussianBlur(0.6))


def carve_payo():
    """Four relief panels, 512 each, in a 2048 x 512 strip that tiles sideways."""
    W, H = 2048, 512
    rnd = random.Random(404)
    img = Image.new("RGB", (W, H), (96, 130, 138))
    d = ImageDraw.Draw(img)
    for _ in range(90):                                     # the stone's own patches
        x, y, r = rnd.uniform(0, W), rnd.uniform(0, H), rnd.uniform(30, 90)
        c = rnd.choice(((104, 140, 146), (88, 122, 132), (100, 136, 134), (110, 138, 130)))
        for ox in (-W, 0, W):
            d.ellipse([x + ox - r, y - r * 0.6, x + ox + r, y + r * 0.6], fill=c)
    img = img.filter(ImageFilter.GaussianBlur(6))
    RAISED, HI, PAINT_O, PAINT_R = (150, 182, 182), (192, 216, 212), (214, 160, 62), (176, 74, 56)
    TURQ = (70, 204, 186)

    def relief(mask, paint=None):
        m = mask.filter(ImageFilter.GaussianBlur(1.6)).point(lambda v: 255 if v > 110 else 0).filter(ImageFilter.GaussianBlur(1.0))
        img.paste(Image.new("RGB", (W, H), INK), (0, 0), ImageChops.offset(m, 6, 8))
        img.paste(Image.new("RGB", (W, H), RAISED), (0, 0), m)
        edge = ImageChops.multiply(ImageChops.subtract(m, ImageChops.offset(m, 4, 5)), m)
        img.paste(Image.new("RGB", (W, H), HI), (0, 0), edge)
        foot = ImageChops.multiply(ImageChops.subtract(m, ImageChops.offset(m, -4, -6)), m)
        img.paste(Image.new("RGB", (W, H), (108, 142, 148)), (0, 0), foot)
        if paint:                                           # old paint, worn off in patches
            worn = Image.new("L", (W, H), 0)
            dw = ImageDraw.Draw(worn)
            for _ in range(160):
                x, y, r = rnd.uniform(0, W), rnd.uniform(0, H), rnd.uniform(14, 44)
                dw.ellipse([x - r, y - r * 0.7, x + r, y + r * 0.7], fill=255)
            keep = ImageChops.multiply(m, worn.filter(ImageFilter.GaussianBlur(5)).point(lambda v: 255 if v > 90 else 0).filter(ImageFilter.GaussianBlur(2)))
            img.paste(Image.new("RGB", (W, H), paint), (0, 0), keep.point(lambda v: int(v * 0.86)))

    # woven borders top and bottom: a zigzag and a line
    for (y0, flip) in ((20, 1), (H - 20, -1)):
        m = Image.new("L", (W, H), 0)
        dm = ImageDraw.Draw(m)
        dm.rectangle([0, y0 - 9, W, y0 + 9], fill=255)
        relief(m)
        ImageDraw.Draw(img).rectangle([0, y0 - 3, W, y0 + 3], fill=TURQ)          # a line of turquoise inlay down the middle of each border
        m = Image.new("L", (W, H), 0)
        dm = ImageDraw.Draw(m)
        for k in range(W // 64 + 1):
            x = k * 64
            dm.polygon([(x, y0 + flip * 22), (x + 32, y0 + flip * 62), (x + 64, y0 + flip * 22), (x + 64, y0 + flip * 36), (x + 32, y0 + flip * 76), (x, y0 + flip * 36)], fill=255)
        relief(m, PAINT_R if flip > 0 else PAINT_O)
    cy = H / 2

    def panel(k, draw_motif, paint):
        cx = k * 512 + 256
        m = Image.new("L", (W, H), 0)
        draw_motif(ImageDraw.Draw(m), cx)
        relief(m, paint)
        for sx in (-1, 1):                                  # a post between panels
            mm = Image.new("L", (W, H), 0)
            ImageDraw.Draw(mm).rectangle([cx + sx * 244 - 9, 104, cx + sx * 244 + 9, H - 104], fill=255)
            relief(mm)

    def lizard(dm, cx):
        dm.ellipse([cx - 46, cy - 110, cx + 46, cy + 60], fill=255)
        dm.ellipse([cx - 34, cy - 160, cx + 34, cy - 92], fill=255)
        dm.polygon([(cx - 22, cy + 40), (cx + 22, cy + 40), (cx + 40, cy + 110), (cx + 10, cy + 150), (cx + 36, cy + 150), (cx + 64, cy + 104), (cx + 6, cy + 20)], fill=255)
        for sx in (-1, 1):
            for (y0, y1) in ((-70, -120), (20, 80)):
                dm.line([(cx + sx * 30, cy + y0), (cx + sx * 110, cy + y0), (cx + sx * 110, cy + y1)], fill=255, width=26)
                for t in (-22, 0, 22):
                    dm.line([(cx + sx * 110, cy + y1), (cx + sx * (110 + t), cy + y1 + (26 if y1 > 0 else -26))], fill=255, width=12)

    def sun(dm, cx):
        dm.ellipse([cx - 66, cy - 66, cx + 66, cy + 66], fill=255)
        for k in range(12):
            a = math.tau * k / 12
            dm.polygon([(cx + 82 * math.cos(a - 0.14), cy + 82 * math.sin(a - 0.14)), (cx + 150 * math.cos(a), cy + 150 * math.sin(a)), (cx + 82 * math.cos(a + 0.14), cy + 82 * math.sin(a + 0.14))], fill=255)

    def rice_(dm, cx):
        for (ox, lean) in ((-90, -30), (0, 0), (90, 30)):
            dm.line([(cx + ox, cy + 150), (cx + ox + lean * 0.4, cy), (cx + ox + lean, cy - 120)], fill=255, width=16)
            for k in range(5):
                t = k / 5
                for sx in (-1, 1):
                    gx, gy = cx + ox + lean * (0.5 + t * 0.5) + sx * 26, cy - 20 - t * 110
                    dm.ellipse([gx - 20, gy - 12, gx + 20, gy + 12], fill=255)

    def ancestor(dm, cx):
        dm.ellipse([cx - 40, cy - 160, cx + 40, cy - 84], fill=255)
        dm.polygon([(cx - 34, cy - 80), (cx + 34, cy - 80), (cx + 26, cy + 40), (cx - 26, cy + 40)], fill=255)
        for sx in (-1, 1):
            dm.line([(cx + sx * 30, cy - 60), (cx + sx * 100, cy - 60), (cx + sx * 100, cy - 130)], fill=255, width=26)
            dm.line([(cx + sx * 18, cy + 30), (cx + sx * 80, cy + 60), (cx + sx * 80, cy + 150)], fill=255, width=28)
        dm.polygon([(cx - 70, cy - 190), (cx, cy - 170), (cx + 70, cy - 190), (cx + 56, cy - 160), (cx - 56, cy - 160)], fill=255)

    panel(0, lizard, PAINT_O); panel(1, sun, PAINT_R); panel(2, rice_, PAINT_O); panel(3, ancestor, PAINT_R)
    rnd2 = random.Random(41)
    for _ in range(26):                                     # moss in the bottom corners, lichen on top
        x, y, r = rnd2.uniform(0, W), rnd2.choice((rnd2.uniform(H - 70, H - 10), rnd2.uniform(0, H))), rnd2.uniform(10, 28)
        m = Image.new("L", (W, H), 0)
        ImageDraw.Draw(m).polygon(blob_poly(x, y, r * 1.6, r, rnd2, n=9, jit=0.3), fill=255)
        img.paste(Image.new("RGB", (W, H), rnd2.choice(((92, 126, 64), (188, 198, 172)))), (0, 0), m.filter(ImageFilter.GaussianBlur(1.6)).point(lambda v: int(v * 0.8)))
    return img.filter(ImageFilter.GaussianBlur(0.6))


def thatch_payo():
    rnd = random.Random(505)
    img = Image.new("RGB", (S, S), (120, 100, 66))
    rows = 6
    rh = S / rows
    for row in range(rows + 1):
        y0 = row * rh - rh * 0.25
        band = L()
        ImageDraw.Draw(band).rectangle([0, y0 + rh * 0.86, S, y0 + rh * 1.1], fill=255)
        put(img, band.filter(ImageFilter.GaussianBlur(10)), (64, 52, 40), 0.85)          # the shadow under the course above
    for row in range(rows + 1):
        y0 = row * rh - rh * 0.25
        for k in range(46):
            x = (k + rnd.uniform(-0.3, 0.3)) * S / 46
            w = rnd.uniform(16, 34)
            ln = rh * rnd.uniform(0.86, 1.16)
            col = shade(rnd.choice(((176, 150, 96), (160, 136, 88), (190, 166, 112), (146, 128, 92), (168, 152, 116))), rnd.uniform(0.92, 1.08))
            m = L()
            d = ImageDraw.Draw(m)
            for ox in (-S, 0, S):
                for oy in (-S, 0, S):
                    d.polygon([(x + ox - w / 2, y0 + oy), (x + ox + w / 2, y0 + oy), (x + ox + w * 0.3 + rnd.uniform(-4, 4), y0 + oy + ln * 0.8), (x + ox + rnd.uniform(-5, 5), y0 + oy + ln), (x + ox - w * 0.3, y0 + oy + ln * 0.78)], fill=255)
            mm = m.filter(ImageFilter.GaussianBlur(0.9))
            put(img, ImageChops.offset(mm, 3, 4), (84, 68, 46), 0.7)
            put(img, mm, col)
            hi = ImageChops.multiply(ImageChops.subtract(mm, ImageChops.offset(mm, 4, 0)), mm)
            put(img, hi, shade(col, 1.16), 0.8)
    return img.filter(ImageFilter.GaussianBlur(0.7))


def timber_payo():
    rnd = random.Random(606)
    img = Image.new("RGB", (S, S), (112, 78, 56))
    boards = 4
    bw = S / boards
    for b in range(boards):
        base = shade(rnd.choice(((122, 86, 60), (106, 74, 54), (132, 96, 68), (116, 84, 62))), rnd.uniform(0.94, 1.06))
        m = L()
        ImageDraw.Draw(m).rectangle([b * bw + 5, 0, (b + 1) * bw - 5, S], fill=255)
        put(img, m.filter(ImageFilter.GaussianBlur(1.2)), base)
        for _ in range(22):                                 # long soft grain strokes
            x = b * bw + rnd.uniform(14, bw - 14)
            y, ln, w = rnd.uniform(0, S), rnd.uniform(160, 520), rnd.uniform(4, 12)
            g = L()
            dg = ImageDraw.Draw(g)
            for oy in (-S, 0, S):
                dg.line([(x, y + oy), (x + rnd.uniform(-10, 10), y + oy + ln)], fill=255, width=int(w))
            put(img, ImageChops.multiply(g.filter(ImageFilter.GaussianBlur(3)), m), shade(base, rnd.choice((0.84, 0.9, 1.1, 1.14))), 0.8)
        for _ in range(2):                                  # a knot
            x, y = b * bw + rnd.uniform(40, bw - 40), rnd.uniform(40, S - 40)
            for (r, f) in ((26, 0.78), (16, 0.64), (7, 0.5)):
                k = L()
                ImageDraw.Draw(k).ellipse([x - r * 0.7, y - r, x + r * 0.7, y + r], fill=255)
                put(img, k.filter(ImageFilter.GaussianBlur(1.6)), shade(base, f))
        e = L()
        ImageDraw.Draw(e).rectangle([b * bw + 5, 0, b * bw + 13, S], fill=255)
        put(img, e.filter(ImageFilter.GaussianBlur(1.4)), shade(base, 1.22), 0.8)
    band = L()                                              # a carved chevron band across every board
    db = ImageDraw.Draw(band)
    for k in range(17):
        x = k * 64
        db.polygon([(x, 470), (x + 32, 430), (x + 64, 470), (x + 64, 500), (x + 32, 460), (x, 500)], fill=255)
        db.polygon([(x, 540), (x + 32, 500), (x + 64, 540), (x + 64, 566), (x + 32, 526), (x, 566)], fill=255)
    put(img, ImageChops.offset(band, 3, 4).filter(ImageFilter.GaussianBlur(1.2)), (58, 40, 30))
    put(img, band.filter(ImageFilter.GaussianBlur(1.0)), (150, 112, 78))
    return img.filter(ImageFilter.GaussianBlur(0.6))


def rice_payo():
    rnd = random.Random(707)
    img = Image.new("RGB", (S, S), (74, 110, 104))                   # the water it stands in
    for _ in range(40):
        x, y, r = rnd.uniform(0, S), rnd.uniform(0, S), rnd.uniform(40, 120)
        m = L()
        d = ImageDraw.Draw(m)
        for ox in (-S, 0, S):
            for oy in (-S, 0, S):
                d.ellipse([x + ox - r, y + oy - r * 0.4, x + ox + r, y + oy + r * 0.4], fill=255)
        put(img, m.filter(ImageFilter.GaussianBlur(10)), rnd.choice(((92, 136, 132), (64, 98, 92), (118, 160, 156))), 0.8)
    rows = 8
    for row in range(rows):
        y = row * S / rows + S / rows / 2
        for k in range(8):
            x = k * S / 8 + (S / 16 if row % 2 else 0) + rnd.uniform(-10, 10)
            yy = y + rnd.uniform(-8, 8)
            for (rx, ry, col, dx, dy) in ((46, 34, (58, 104, 44), 4, 8), (42, 30, (112, 172, 62), 0, 0), (26, 18, (156, 208, 86), -8, -8)):
                m = L()
                d = ImageDraw.Draw(m)
                r2 = random.Random(int(x * 3 + yy * 5 + rx))
                for ox in (-S, 0, S):
                    for oy in (-S, 0, S):
                        d.polygon(blob_poly(x + dx + ox, yy + dy + oy, rx, ry, r2, n=12, jit=0.28), fill=255)
                put(img, m.filter(ImageFilter.GaussianBlur(1.2)), col)
    for _ in range(40):                                     # small glints of sky between the rows
        x, y, r = rnd.uniform(0, S), rnd.uniform(0, S), rnd.uniform(5, 12)
        m = L()
        ImageDraw.Draw(m).ellipse([x - r * 1.8, y - r * 0.6, x + r * 1.8, y + r * 0.6], fill=255)
        put(img, m.filter(ImageFilter.GaussianBlur(1.2)), (206, 230, 230), 0.8)
    return img.filter(ImageFilter.GaussianBlur(0.6))


def dike_payo():
    rnd = random.Random(808)
    img = Image.new("RGB", (S, S), (104, 142, 68))
    for _ in range(60):
        x, y, r = rnd.uniform(0, S), rnd.uniform(0, S), rnd.uniform(50, 130)
        m = L()
        d = ImageDraw.Draw(m)
        for ox in (-S, 0, S):
            for oy in (-S, 0, S):
                d.polygon(blob_poly(x + ox, y + oy, r, r * 0.7, random.Random(int(x + y * 3)), n=10, jit=0.3), fill=255)
        put(img, m.filter(ImageFilter.GaussianBlur(12)), rnd.choice(((118, 158, 76), (92, 130, 62), (130, 166, 84), (126, 114, 84), (112, 100, 78))), 0.85)
    for _ in range(150):                                    # tufts: three blades each
        x, y = rnd.uniform(0, S), rnd.uniform(0, S)
        col = rnd.choice(((150, 192, 92), (84, 122, 56), (166, 204, 104)))
        m = L()
        d = ImageDraw.Draw(m)
        for a in (-0.5, 0.0, 0.5):
            d.polygon([(x - 5, y), (x + 5, y), (x + 26 * math.sin(a), y - 30 * math.cos(a))], fill=255)
        put(img, m.filter(ImageFilter.GaussianBlur(0.9)), col)
    return img.filter(ImageFilter.GaussianBlur(0.6))


def decals_payo():
    """4 x 4 cells of 256. Row 0 lichen, row 1 creeping moss, row 2 drip stains (falling from the cell's top), row 3:
    two cracks, worn ochre paint, leaf litter."""
    rnd = random.Random(909)
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))

    def cell(cx, cy, mask, colour, alpha=1.0):
        layer = Image.new("RGBA", (S, S), colour + (0,))
        layer.putalpha(mask.point(lambda v: int(v * alpha)))
        img.alpha_composite(layer)

    for c in range(4):
        ox, oy = c * 256, 0
        m = L()
        d = ImageDraw.Draw(m)
        for k in range(rnd.randint(5, 9)):
            x, y, r = ox + rnd.uniform(60, 196), oy + rnd.uniform(60, 196), rnd.uniform(22, 52)
            for q in range(6):
                a = math.tau * q / 6 + rnd.uniform(-0.3, 0.3)
                rr = r * rnd.uniform(0.4, 0.62)
                d.ellipse([x + r * 0.5 * math.cos(a) - rr, y + r * 0.5 * math.sin(a) - rr, x + r * 0.5 * math.cos(a) + rr, y + r * 0.5 * math.sin(a) + rr], fill=255)
        mm = m.filter(ImageFilter.GaussianBlur(2))
        cell(0, 0, mm, ((198, 206, 180), (184, 198, 172), (210, 206, 186), (176, 192, 160))[c], 0.9)
        inner = ImageChops.multiply(mm, ImageChops.offset(mm, 6, 6))
        cell(0, 0, ImageChops.subtract(mm, inner), (226, 230, 208), 0.6)
        ox, oy = c * 256, 256                              # moss: lobes hanging from the top edge
        m = L()
        d = ImageDraw.Draw(m)
        for k in range(rnd.randint(10, 16)):
            x = ox + rnd.uniform(24, 232)
            ln = rnd.uniform(40, 190)
            w = rnd.uniform(18, 40)
            d.ellipse([x - w, oy + 8, x + w, oy + 8 + ln * 0.5], fill=255)
            d.ellipse([x - w * 0.7, oy + 8 + ln * 0.3, x + w * 0.7, oy + 8 + ln], fill=255)
        mm = m.filter(ImageFilter.GaussianBlur(2))
        cell(0, 0, mm, ((92, 128, 62), (80, 112, 58), (106, 140, 70), (88, 122, 64))[c])
        cell(0, 0, ImageChops.multiply(ImageChops.subtract(mm, ImageChops.offset(mm, 5, 7)), mm), (150, 182, 96), 0.85)
        ox, oy = c * 256, 512                              # drips: dark, tapering down
        m = L()
        d = ImageDraw.Draw(m)
        for k in range(rnd.randint(4, 7)):
            x = ox + rnd.uniform(30, 226)
            ln = rnd.uniform(110, 236)
            w = rnd.uniform(10, 26)
            d.polygon([(x - w, oy + 4), (x + w, oy + 4), (x + w * 0.5, oy + ln * 0.6), (x + rnd.uniform(-4, 4), oy + ln), (x - w * 0.5, oy + ln * 0.55)], fill=255)
        cell(0, 0, m.filter(ImageFilter.GaussianBlur(4)), ((58, 66, 74), (64, 74, 70), (52, 60, 70), (70, 76, 72))[c], 0.62)
    for c in range(2):                                     # cracks
        ox, oy = c * 256, 768
        m = L()
        d = ImageDraw.Draw(m)
        pts = [(ox + 128 + rnd.uniform(-30, 30), oy + 10)]
        for k in range(9):
            pts.append((pts[-1][0] + rnd.uniform(-26, 26), pts[-1][1] + 26))
        d.line(pts, fill=255, width=7)
        for k in (3, 6):
            d.line([pts[k], (pts[k][0] + rnd.choice((-1, 1)) * rnd.uniform(40, 80), pts[k][1] + rnd.uniform(20, 50))], fill=255, width=5)
        cell(0, 0, m.filter(ImageFilter.GaussianBlur(1.0)), (44, 50, 58))
        cell(0, 0, ImageChops.offset(m, -3, -3).filter(ImageFilter.GaussianBlur(1.2)), (198, 204, 208), 0.5)
    ox, oy = 512, 768                                      # worn ochre and red paint: a remnant of a painted figure
    m = L()
    d = ImageDraw.Draw(m)
    for k in range(5):
        d.polygon([(ox + 30 + k * 40, oy + 200), (ox + 50 + k * 40, oy + 60 + (k % 2) * 40), (ox + 70 + k * 40, oy + 200)], fill=255)
    worn = L()
    dw = ImageDraw.Draw(worn)
    for _ in range(60):
        x, y, r = ox + rnd.uniform(0, 256), oy + rnd.uniform(0, 256), rnd.uniform(10, 26)
        dw.ellipse([x - r, y - r, x + r, y + r], fill=255)
    cell(0, 0, ImageChops.multiply(m, worn.filter(ImageFilter.GaussianBlur(3))), (190, 140, 58), 0.85)
    ox, oy = 768, 768                                      # leaf litter
    for k in range(26):
        x, y, a = ox + rnd.uniform(30, 226), oy + rnd.uniform(30, 226), rnd.uniform(0, math.tau)
        m = L()
        ImageDraw.Draw(m).polygon([(x + 22 * math.cos(a), y + 22 * math.sin(a)), (x + 9 * math.cos(a + 1.57), y + 9 * math.sin(a + 1.57)), (x - 22 * math.cos(a), y - 22 * math.sin(a)), (x - 9 * math.cos(a + 1.57), y - 9 * math.sin(a + 1.57))], fill=255)
        cell(0, 0, m.filter(ImageFilter.GaussianBlur(0.9)), rnd.choice(((150, 126, 70), (112, 132, 62), (170, 150, 90), (96, 84, 60))))
    return img


def pine_payo():
    rnd = random.Random(111)
    img = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.line([(256, 500), (256, 60)], fill=(92, 70, 50, 255), width=14)
    for k in range(9):
        y = 440 - k * 44
        ln = 210 - k * 18
        for sx in (-1, 1):
            tip = (256 + sx * ln, y + 26)
            for (w, col) in ((62, (38, 86, 62, 255)), (46, (58, 116, 76, 255)), (26, (96, 150, 96, 255))):
                d.polygon([(256, y - w * 0.5), (256 + sx * ln * 0.5, y - w * 0.55 + 10), tip, (256 + sx * ln * 0.5, y + w * 0.5 + 12), (256, y + w * 0.4)], fill=col)
            for q in range(6):
                t = (q + 1) / 7
                px, py = 256 + sx * ln * t, y + 26 * t
                d.line([(px, py), (px + sx * 14, py + 26)], fill=(38, 86, 62, 255), width=7)
    return img.filter(ImageFilter.GaussianBlur(0.8))


def fall_payo():
    """Falling water, tiles down v: long soft streaks, teal to white, with ropes of foam."""
    rnd = random.Random(121)
    img = Image.new("RGB", (512, 1024), (96, 190, 196))
    for _ in range(70):
        x, w, y, ln = rnd.uniform(0, 512), rnd.uniform(8, 34), rnd.uniform(0, 1024), rnd.uniform(260, 900)
        col = rnd.choice(((150, 220, 220), (206, 240, 236), (74, 168, 180), (236, 250, 246), (120, 204, 208)))
        m = Image.new("L", (512, 1024), 0)
        d = ImageDraw.Draw(m)
        for ox in (-512, 0, 512):
            for oy in (-1024, 0, 1024):
                d.polygon([(x + ox - w / 2, y + oy), (x + ox + w / 2, y + oy), (x + ox + w * 0.3, y + oy + ln), (x + ox - w * 0.3, y + oy + ln)], fill=255)
        img.paste(Image.new("RGB", (512, 1024), col), (0, 0), m.filter(ImageFilter.GaussianBlur(4)).point(lambda v: int(v * 0.85)))
    return img.filter(ImageFilter.GaussianBlur(0.8))


def spill_payo():
    img = Image.new("RGBA", (512, 512), (255, 186, 96, 0))
    m = Image.new("L", (512, 512), 0)
    ImageDraw.Draw(m).ellipse([96, 60, 416, 470], fill=255)
    img.putalpha(m.filter(ImageFilter.GaussianBlur(70)).point(lambda v: int(v * 0.8)))
    return img


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    made = {"stone_payo": stone_payo, "paving_payo": paving_payo, "riverstone": riverstone, "carve_payo": carve_payo, "thatch_payo": thatch_payo, "timber_payo": timber_payo,
            "rice_payo": rice_payo, "dike_payo": dike_payo, "decals_payo": decals_payo, "pine_payo": pine_payo, "spill_payo": spill_payo, "fall_payo": fall_payo}
    pics = {}
    for name, fn in made.items():
        im = fn()
        im.save(OUT / (name + "_albedo.png"))
        pics[name] = im
        print("[payo-paint]", name, im.size)
    sheet_n = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--sheet=")), None)
    if sheet_n:
        LOGS.mkdir(parents=True, exist_ok=True)
        names = ["stone_payo", "paving_payo", "riverstone", "thatch_payo", "timber_payo", "rice_payo", "dike_payo", "decals_payo"]
        sh = Image.new("RGB", (4 * 512, 2 * 512 + 256), (40, 44, 50))
        for i, n in enumerate(names):
            im = pics[n].convert("RGBA").resize((512, 512), Image.LANCZOS)
            bg = Image.new("RGBA", (512, 512), (90, 96, 104, 255))
            bg.alpha_composite(im)
            sh.paste(bg.convert("RGB"), ((i % 4) * 512, (i // 4) * 512))
        sh.paste(pics["carve_payo"].resize((1024, 256), Image.LANCZOS), (0, 1024))
        rep = Image.new("RGB", (768, 256))
        t = pics["stone_payo"].resize((256, 256), Image.LANCZOS)
        for k in range(3):
            rep.paste(t, (k * 256, 0))
        sh.paste(rep, (1024 + 128, 1024))
        sh.save(LOGS / ("payo_art_swatches_v%s.png" % sheet_n))


if __name__ == "__main__":
    main()
