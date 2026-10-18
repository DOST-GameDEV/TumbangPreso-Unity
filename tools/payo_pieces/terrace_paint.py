"""PAYO land kit, step 1: paint the pictures the terrace, falls, rocks and props wear (system Python, numpy + Pillow).

  py -3 tools/payo_pieces/terrace_paint.py [wall ground plant fall rock wood wet foam]

Into Logs/payo/pieces/build/terrace/tex. Every stone of the wall is painted one at a time where terrace_layout.stones()
puts it: its own tone, a wide pale bevel on the lit edges, a dark one below, a crack or two, moss creeping up from the
joint. Palette is the owner's reference 238: cool blue-green stone, teal shadow, dark leaf green, a little ochre.
"""
import math
import random
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi

sys.path.insert(0, str(Path(__file__).resolve().parent))
import terrace_layout as TL  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
TEX = ROOT / "Logs/payo/pieces/build/terrace/tex"
TEX.mkdir(parents=True, exist_ok=True)

PALE, INK, DARK = (232, 228, 214), (44, 42, 44), (84, 80, 80)
MOSS, MOSS_L, MOSS_D = (78, 128, 60), (150, 190, 88), (44, 92, 58)


def f(c):
    return np.array(c, dtype=np.float32) / 255.0


def save(a, name):
    a = np.clip(a, 0, 1)
    Image.fromarray((a * 255 + 0.5).astype(np.uint8)).save(TEX / name)
    print("painted", name, a.shape[1], "x", a.shape[0])


def down(a, k=2):
    h, w = a.shape[:2]
    return a.reshape(h // k, k, w // k, k, -1).mean(axis=(1, 3))


def soft(rng, shape, sigma, wrap=False):
    """A soft field in -1..1: big brush blotches, not noise."""
    n = rng.standard_normal(shape).astype(np.float32)
    n = ndi.gaussian_filter(n, sigma, mode="wrap" if wrap else "reflect")
    return n / (np.abs(n).max() + 1e-6)


def mix(a, b, t):
    if np.ndim(t) == 2:
        t = t[..., None]
    return a * (1 - t) + b * t


# ------------------------------------------------------------------------------------------------ the wall
def wall():
    SS = 2
    W, Hh = 2048 * SS, 256 * SS
    ppm = W / TL.L
    ext = int(W * 13.2 / 12)
    rng = np.random.default_rng(11)
    r = random.Random(11)
    # joints: dark teal, a little green where damp
    bg = np.empty((Hh, W, 3), np.float32)
    bg[:] = f(INK)
    damp = np.clip(soft(rng, (Hh, W), (14, 40), True) * 2.2 - 0.5, 0, 1)
    bg = mix(bg, f(MOSS_D), np.clip(damp * 2 - 1.0, 0, 1) * 0.7)
    col = np.zeros((Hh, ext, 3), np.float32)
    alp = np.zeros((Hh, ext), np.float32)
    light = np.array([-0.45, -0.89])                  # from the upper left of the picture
    capy = Hh - TL.T / TL.TEX_V * Hh                   # the row of the top edge
    for st in TL.stones():
        pts = [(s * ppm, Hh - t / TL.TEX_V * Hh) for s, t in TL.corners(st)]
        xs, ys = [p[0] for p in pts], [p[1] for p in pts]
        m = 14
        x0, x1 = max(0, int(min(xs)) - m), min(ext, int(max(xs)) + m)
        y0, y1 = max(0, int(min(ys)) - m), min(Hh, int(max(ys)) + m)
        w, h = x1 - x0, y1 - y0
        im = Image.new("L", (w, h), 0)
        ImageDraw.Draw(im).polygon([(x - x0, y - y0) for x, y in pts], fill=255)
        mask = np.asarray(im, np.float32) / 255
        rad = r.uniform(9, 24)                          # rounder and squarer stones
        mask = np.clip((ndi.gaussian_filter(mask, rad) - 0.5) * 6 + 0.5, 0, 1)
        if st["top"]:                                   # a cap stone runs off the top of the picture: no rounding there
            mask[: max(0, int(capy - y0)), :] = np.maximum(mask[: max(0, int(capy - y0)), :], np.asarray(im, np.float32)[: max(0, int(capy - y0)), :] / 255)
        inside = mask > 0.5
        d = ndi.distance_transform_edt(inside).astype(np.float32)
        gy, gx = np.gradient(ndi.gaussian_filter(d, 2.0))
        n = np.sqrt(gx * gx + gy * gy) + 1e-6
        lit = (-gx / n) * light[0] * -1 + (-gy / n) * light[1] * -1   # outward normal . light direction
        yy = np.linspace(0, 1, h, dtype=np.float32)[:, None]
        base = f(st["tone"])
        c = np.empty((h, w, 3), np.float32)
        c[:] = base
        c *= (1.07 - 0.16 * yy)[..., None]
        c *= (1 + 0.10 * soft(rng, (h, w), 20))[..., None]
        c *= (1 + 0.03 * soft(rng, (h, w), (3, 11)))[..., None]       # brush strokes lying along the stone
        # a second tone washed in from one side: painted, not flat
        wash = np.clip(soft(rng, (h, w), 26) * 1.4, 0, 1)
        c = mix(c, c * f((176, 164, 146)) / f((150, 148, 144)), wash * 0.55)             # a warmer patch
        c *= (1 + 0.07 * soft(rng, (h, w), 5))[..., None]                                 # mottle: matte stone, not glass
        c *= (1 - 0.10 * np.clip(soft(rng, (h, w), 2.2) * 3 - 1.5, 0, 1))[..., None]      # pores
        # the bevel: a wide band, pale where the light catches it, dark teal underneath
        bw = 0.050 * ppm
        band = np.clip((bw - d) / 2.0, 0, 1) * inside
        up = np.clip(lit, 0, 1) ** 0.7
        dn = np.clip(-lit, 0, 1) ** 0.7
        brk = np.clip(0.75 + 0.6 * soft(rng, (h, w), (3, 9)), 0.25, 1)                   # the highlight breaks along the edge: worn, chipped
        c = mix(c, f(PALE), band * up * 0.62 * brk)
        c = mix(c, f(DARK), band * dn * 0.55)
        line = np.clip(1 - np.abs(d - bw) / 1.6, 0, 1) * inside       # the painter's line where the bevel meets the face
        c = mix(c, f(PALE), line * up * 0.10)
        c = mix(c, f(DARK), line * dn * 0.22)
        if st["top"]:                                   # the top edge and the cap: paler, it faces the sky
            rows = np.arange(y0, y1, dtype=np.float32)[:, None]
            cap = np.clip((capy - rows) / 3.0, 0, 1)
            c = mix(c, c * 1.16 + 0.03, cap)
            edge = np.clip(1 - np.abs(rows - capy - 0.02 * ppm) / (0.030 * ppm), 0, 1)
            c = mix(c, f(PALE), edge * 0.55 * np.clip(0.7 + 0.6 * soft(rng, (h, w), (2, 12)), 0.2, 1))
            creep = np.clip((capy - 0.10 * ppm - rows) / (0.10 * ppm), 0, 1) * np.clip(0.55 + soft(rng, (h, w), (6, 18)), 0, 1)
            c = mix(c, f(MOSS), np.clip(creep * 1.3, 0, 1) * 0.9)
            c = mix(c, f(MOSS_L), np.clip(creep * 1.3 - 0.75, 0, 1) * 1.5)
        # cracks: a dark line with a pale lip
        if r.random() < 0.38:
            cr = Image.new("L", (w, h), 0)
            dr = ImageDraw.Draw(cr)
            px, py = r.uniform(m, w - m), m + 2
            ang = r.uniform(1.1, 2.0)
            path = [(px, py)]
            for _ in range(r.randint(3, 6)):
                ang += r.uniform(-0.7, 0.7)
                px += math.cos(ang) * r.uniform(8, 22)
                py += abs(math.sin(ang)) * r.uniform(8, 22)
                path.append((px, py))
            dr.line(path, fill=255, width=3)
            crm = np.asarray(cr, np.float32) / 255 * inside
            c = mix(c, f(DARK) * 0.8, crm * 0.85)
            c = mix(c, f(PALE), np.roll(crm, (2, 2), (0, 1)) * (1 - crm) * inside * 0.45)
        # pits and pale chips: a few, placed
        sp = Image.new("L", (w, h), 128)
        dr = ImageDraw.Draw(sp)
        for _ in range(r.randint(6, 14)):
            px, py, q = r.uniform(m, w - m), r.uniform(m, h - m), r.uniform(2, 6.5)
            dr.ellipse((px - q, py - q * 0.7, px + q, py + q * 0.7), fill=r.choice((70, 80, 190, 200)))
        spa = (np.asarray(sp, np.float32) - 128) / 255
        c *= (1 + spa * 0.5 * inside)[..., None]
        # moss up from the joint underneath, and lichen spots
        if r.random() < 0.42:
            rows = np.linspace(0, 1, h, dtype=np.float32)[:, None]
            g = np.clip(rows * 1.5 - 1.0 + 0.55 * soft(rng, (h, w), (7, 14)), 0, 1) * inside
            g = np.clip(g * 2.0, 0, 1)
            c = mix(c, f(MOSS), g * 0.88)
            c = mix(c, f(MOSS_L), np.clip(g - 0.55, 0, 1) * np.clip(soft(rng, (h, w), 3) * 3, 0, 1) * 0.9)
        if r.random() < 0.25:
            li = np.clip(soft(rng, (h, w), 5) * 3 - 1.6, 0, 1) * inside
            c = mix(c, f((206, 190, 120)), li * 0.8)
        a = mask
        col[y0:y1, x0:x1] = mix(col[y0:y1, x0:x1], c, a)
        alp[y0:y1, x0:x1] = np.maximum(alp[y0:y1, x0:x1], a)
    out = mix(bg, col[:, :W], alp[:, :W])
    extra = ext - W
    out[:, :extra] = mix(out[:, :extra], col[:, W:], alp[:, W:])
    # the damp foot of the wall
    rows = np.linspace(0, 1, Hh, dtype=np.float32)[:, None]
    foot = np.clip((rows - 0.86) / 0.14, 0, 1) * np.clip(0.6 + soft(rng, (Hh, W), (5, 30), True), 0, 1)
    out = mix(out, f(MOSS_D), foot * 0.55)
    save(down(out, SS), "py_terrace_wall.png")


# ------------------------------------------------------------------------------------------------ dike grass and paddy mud
def strokes(img, r, n, box, lengths, colours, widths=(2, 4), lean=0.35, wrapw=None):
    dr = ImageDraw.Draw(img)
    W = img.size[0]
    for _ in range(n):
        x, y = r.uniform(box[0], box[2]), r.uniform(box[1], box[3])
        ln = r.uniform(*lengths)
        a = r.uniform(-lean, lean)
        c = r.choice(colours)
        w = r.randint(*widths)
        for ox in ((0, -W, W) if wrapw else (0,)):
            dr.line((x + ox, y, x + ox + math.sin(a) * ln, y - math.cos(a) * ln), fill=c, width=w)


def ground():
    SS = 2
    W, Hh = 1024 * SS, 512 * SS
    r = random.Random(5)
    rng = np.random.default_rng(5)
    img = Image.new("RGB", (W, Hh), (70, 150, 62))
    half = Hh // 2
    # GRASS, top half (v 0.5..1): the dike unfolded, wall side at the top row of the band
    greens = [(60, 138, 60), (84, 166, 66), (108, 186, 72), (48, 120, 62), (132, 200, 84), (70, 152, 70)]
    strokes(img, r, 5200, (0, 0, W, half + 30), (16, 46), greens, (3, 6), wrapw=True)
    strokes(img, r, 700, (0, int(half * 0.25), W, int(half * 0.65)), (14, 30), [(170, 220, 96), (190, 228, 110)], (2, 4), wrapw=True)
    a = np.asarray(img, np.float32) / 255
    rows = np.linspace(0, 1, half, dtype=np.float32)[:, None]
    crest = np.exp(-((rows - 0.45) / 0.22) ** 2)
    a[:half] *= (0.86 + 0.26 * crest)[..., None]
    a[:half] *= (1 + 0.10 * soft(rng, (half, W), (20, 60), True))[..., None]
    foot = np.clip((rows - 0.80) / 0.2, 0, 1) * np.clip(0.5 + soft(rng, (half, W), (4, 26), True), 0, 1)
    a[:half] = mix(a[:half], f((96, 84, 62)), np.clip(foot * 1.5, 0, 1) * 0.85)            # the wet foot by the water
    edge = np.clip((0.10 - rows) / 0.10, 0, 1) * np.clip(0.5 + soft(rng, (half, W), (4, 20), True), 0, 1)
    a[:half] = mix(a[:half], f((40, 104, 60)), edge * 0.7)                                   # dark where it hangs over the stone
    # MUD, bottom half (v 0..0.5): the paddy bed, seen through the water
    mud = np.empty((Hh - half, W, 3), np.float32)
    mud[:] = f((112, 96, 74))
    b = soft(rng, (Hh - half, W), (26, 40), True)
    mud = mix(mud, f((78, 66, 56)), np.clip(b, 0, 1) * 0.8)
    mud = mix(mud, f((146, 126, 92)), np.clip(-b, 0, 1) * 0.7)
    rip = np.clip(soft(rng, (Hh - half, W), (2, 22), True) * 3 - 1.4, 0, 1)
    mud = mix(mud, f((170, 152, 116)), rip * 0.5)
    a[half:] = mud
    a[half - 3:half + 3] = mix(a[half - 3:half + 3], a[half + 4:half + 5], 0.5)
    save(down(a, SS), "py_terrace_ground.png")


# ------------------------------------------------------------------------------------------------ blades and leaves
def plant():
    SS = 2
    cw, Hh = 64 * SS, 256 * SS
    cells = [((40, 108, 50), (186, 228, 84), "rib"),        # 0 young rice
             ((112, 132, 52), (232, 200, 86), "rib"),       # 1 ripe leaf
             ((214, 160, 52), (250, 214, 96), "grain"),     # 2 grain head
             ((36, 108, 58), (134, 200, 92), "rib"),        # 3 grass
             ((26, 96, 64), (104, 188, 100), "fern"),       # 4 fern
             ((30, 112, 82), (118, 204, 126), "taro"),      # 5 taro
             ((196, 44, 44), (250, 122, 80), "petal"),      # 6 red flower
             ((150, 118, 60), (220, 192, 112), "rib")]      # 7 dry stem
    a = np.zeros((Hh, cw * 8, 3), np.float32)
    r = random.Random(3)
    rng = np.random.default_rng(3)
    rows = np.linspace(1, 0, Hh, dtype=np.float32)[:, None]            # 0 at the base (bottom row), 1 at the tip
    xs = np.linspace(-1, 1, cw, dtype=np.float32)[None, :]
    for i, (c0, c1, kind) in enumerate(cells):
        t = rows ** 0.8 + 0 * xs
        c = mix(np.broadcast_to(f(c0), (Hh, cw, 3)).copy(), np.broadcast_to(f(c1), (Hh, cw, 3)), t)
        c *= (1 - 0.22 * np.abs(xs) ** 1.5)[..., None]
        c *= (1 + 0.06 * soft(rng, (Hh, cw), (30, 6)))[..., None]
        if kind == "rib":
            c = mix(c, c * 1.25 + 0.04, np.clip(1 - np.abs(xs) / 0.16, 0, 1) * 0.6)
        im = Image.fromarray((np.clip(c, 0, 1) * 255).astype(np.uint8))
        dr = ImageDraw.Draw(im)
        if kind == "grain":
            for _ in range(70):
                x, y = r.uniform(4, cw - 4), r.uniform(4, Hh - 4)
                dr.ellipse((x - 9, y - 13, x + 9, y + 13), fill=r.choice([(196, 138, 40), (238, 196, 78), (255, 226, 128), (220, 166, 56)]))
        if kind == "fern":
            for k in range(22):
                y = Hh - 10 - k * (Hh - 20) / 22
                dr.line((cw / 2, y, 2, y - 26), fill=(22, 84, 60), width=4)
                dr.line((cw / 2, y, cw - 2, y - 26), fill=(22, 84, 60), width=4)
            dr.line((cw / 2, Hh, cw / 2, 0), fill=(150, 206, 120), width=5)
        if kind == "taro":
            for k in range(-4, 5):
                dr.line((cw / 2, Hh * 0.72, cw / 2 + k * cw * 0.16, 6 if abs(k) < 3 else Hh * 0.4), fill=(170, 226, 160), width=3)
            dr.line((cw / 2, Hh, cw / 2, 0), fill=(186, 232, 170), width=5)
        if kind == "petal":
            dr.ellipse((cw / 2 - 26, Hh - 60, cw / 2 + 26, Hh + 20), fill=(250, 210, 80))
            dr.line((cw / 2, Hh - 50, cw / 2, 20), fill=(255, 150, 110), width=5)
        a[:, i * cw:(i + 1) * cw] = np.asarray(im, np.float32) / 255
    save(down(a, SS), "py_terrace_plant.png")


# ------------------------------------------------------------------------------------------------ falling water
def fall():
    """Falling water, tiling down the picture: mostly white, with pale aqua between and a few blue shadows. Long
    tapered brush streaks, not dashes."""
    SS = 2
    W, Hh = 256 * SS, 1024 * SS
    r = random.Random(9)
    rng = np.random.default_rng(9)
    base = np.empty((Hh, W, 3), np.float32)
    base[:] = f((150, 214, 232))
    band = soft(rng, (Hh, W), (220, 9), True)
    base = mix(base, f((84, 170, 206)), np.clip(band * 1.3, 0, 1) * 0.8)
    base = mix(base, f((214, 242, 246)), np.clip(-band * 1.3, 0, 1) * 0.8)
    im = Image.fromarray((np.clip(base, 0, 1) * 255).astype(np.uint8)).convert("RGBA")
    dr = ImageDraw.Draw(im)

    def streak(x, y, ln, w, c):
        # a lens: a point at each end, widest a third of the way down, leaning a little
        lean = r.uniform(-0.02, 0.02) * ln
        for ox in (-W, 0, W):
            for oy in (-Hh, 0, Hh):
                dr.polygon([(x + ox, y + oy), (x + ox + w / 2, y + oy + ln * 0.3), (x + ox + lean + w * 0.15, y + oy + ln),
                            (x + ox + lean - w * 0.15, y + oy + ln), (x + ox - w / 2, y + oy + ln * 0.3)], fill=c)

    for _ in range(26):
        streak(r.uniform(0, W), r.uniform(0, Hh), r.uniform(500, 1300), r.uniform(8, 18), (66, 150, 196, 255))
    for _ in range(60):
        streak(r.uniform(0, W), r.uniform(0, Hh), r.uniform(400, 1200), r.uniform(10, 26), (226, 246, 250, 255))
    for _ in range(54):
        streak(r.uniform(0, W), r.uniform(0, Hh), r.uniform(300, 1000), r.uniform(12, 34), (255, 255, 255, 255))
    for _ in range(30):
        streak(r.uniform(0, W), r.uniform(0, Hh), r.uniform(120, 300), r.uniform(6, 12), (255, 255, 255, 255))
    a = np.asarray(im, np.float32) / 255
    a[..., :3] = ndi.gaussian_filter(a[..., :3], (1.6, 1.0, 0), mode="wrap")
    slit = Image.new("L", (W, Hh), 255)
    ds = ImageDraw.Draw(slit)
    for _ in range(7):
        x, y, ln, w = r.uniform(0, W), r.uniform(0, Hh), r.uniform(300, 700), r.uniform(5, 10)
        for oy in (-Hh, 0, Hh):
            ds.polygon([(x, y + oy), (x + w / 2, y + oy + ln * 0.5), (x, y + oy + ln), (x - w / 2, y + oy + ln * 0.5)], fill=0)
    a[..., 3] = np.asarray(slit, np.float32) / 255
    save(down(a, SS), "py_fall.png")


def rock():
    SS = 2
    W = 512 * SS
    rng = np.random.default_rng(21)
    r = random.Random(21)
    a = np.empty((W, W, 3), np.float32)
    a[:] = f((152, 150, 144))
    b = soft(rng, (W, W), 46, True)
    a = mix(a, f((118, 116, 114)), np.clip(b, 0, 1) * 0.85)
    a = mix(a, f((186, 182, 172)), np.clip(-b, 0, 1) * 0.8)
    s = soft(rng, (W, W), (5, 26), True)
    a *= (1 + 0.07 * s)[..., None]
    warm = np.clip(soft(rng, (W, W), 60, True) * 2 - 1.0, 0, 1)
    a = mix(a, f((180, 166, 140)), warm * 0.5)
    im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
    dr = ImageDraw.Draw(im)
    for _ in range(16):                                                # cracks, dark with a pale lip
        x, y = r.uniform(0, W), r.uniform(0, W)
        ang = r.uniform(0, 6.28)
        path = [(x, y)]
        for _ in range(r.randint(3, 7)):
            ang += r.uniform(-0.8, 0.8)
            x += math.cos(ang) * r.uniform(20, 60)
            y += math.sin(ang) * r.uniform(20, 60)
            path.append((x, y))
        for ox in (-W, 0, W):
            for oy in (-W, 0, W):
                dr.line([(px + ox + 3, py + oy + 3) for px, py in path], fill=(214, 210, 198), width=3)
                dr.line([(px + ox, py + oy) for px, py in path], fill=(66, 62, 62), width=4)
    for _ in range(16):
        x, y, q = r.uniform(0, W), r.uniform(0, W), r.uniform(2, 5)
        dr.ellipse((x - q, y - q * 0.7, x + q, y + q * 0.7), fill=r.choice([(120, 118, 114), (196, 192, 182)]))
    save(down(np.asarray(im, np.float32) / 255, SS), "py_rock.png")


def wood():
    SS = 2
    W = 512 * SS
    q = W // 4
    r = random.Random(31)
    rng = np.random.default_rng(31)
    a = np.zeros((W, W, 3), np.float32)
    rows = np.linspace(0, 1, q, dtype=np.float32)[:, None]
    # band A (top rows, v 0.75..1): bamboo, u along the culm, a node every quarter of the width
    b = np.empty((q, W, 3), np.float32)
    b[:] = f((190, 180, 96))
    b *= (0.72 + 0.5 * np.clip(np.sin(rows * math.pi), 0, 1) ** 0.8)[..., None]        # round: dark at both edges of the band
    b = mix(b, f((232, 222, 140)), np.clip(1 - np.abs(rows - 0.38) / 0.10, 0, 1) * 0.6)
    b = mix(b, f((140, 168, 84)), np.clip(soft(rng, (q, W), (6, 50), True) * 2, 0, 1)[..., None] * 0.5)
    b *= (1 + 0.06 * soft(rng, (q, W), (3, 40), True))[..., None]
    xs = np.arange(W, dtype=np.float32)[None, :]
    node = np.clip(1 - np.abs(((xs + W / 16) % (W / 4)) - W / 8 + W / 8 - W / 8) / 5.0, 0, 1)
    node = np.clip(1 - np.abs((xs % (W / 4)) - W / 8) / 5.0, 0, 1)
    b = mix(b, f((96, 84, 48)), node * 0.9)
    b = mix(b, f((216, 222, 140)), np.roll(node, 9, 1) * 0.5)
    a[0:q] = b
    # band B (v 0.5..0.75): dark timber, grain along u
    t = np.empty((q, W, 3), np.float32)
    t[:] = f((118, 82, 58))
    t *= (1 + 0.16 * soft(rng, (q, W), (2, 60), True))[..., None]
    t = mix(t, f((70, 46, 38)), np.clip(soft(rng, (q, W), (1.5, 50), True) * 3 - 1.5, 0, 1) * 0.8)
    t = mix(t, f((168, 124, 84)), np.clip(soft(rng, (q, W), (1.5, 50), True) * 3 - 1.7, 0, 1) * 0.7)
    a[q:2 * q] = t
    # band C (v 0.25..0.5): straw, strands running down the band
    im = Image.new("RGB", (W, q), (186, 150, 82))
    strokes(im, r, 2600, (0, 0, W, q + 40), (40, 110), [(214, 180, 100), (160, 124, 64), (236, 206, 124), (196, 160, 92), (140, 104, 56)], (3, 6), lean=0.12, wrapw=True)
    a[2 * q:3 * q] = np.asarray(im, np.float32) / 255
    # band D (v 0..0.25): woven cloth with stripes (left three quarters), the scarecrow's sack face (right quarter)
    im = Image.new("RGB", (W, q), (184, 70, 54))
    dr = ImageDraw.Draw(im)
    for y0, hgt, c in ((18, 16, (34, 34, 42)), (40, 8, (236, 226, 198)), (60, 22, (34, 34, 42)), (96, 10, (222, 164, 62)), (128, 26, (34, 34, 42)), (166, 8, (236, 226, 198)), (190, 18, (64, 150, 140)), (222, 14, (34, 34, 42))):
        dr.rectangle((0, y0, W, y0 + hgt), fill=c)
    for x in range(0, W, 40):                                           # little woven diamonds on the widest black band
        dr.polygon([(x + 20, 130), (x + 30, 141), (x + 20, 152), (x + 10, 141)], fill=(236, 226, 198))
    fx = W * 3 // 4
    dr.rectangle((fx, 0, W, q), fill=(222, 208, 168))
    cx, cy = fx + q // 2, q // 2
    for sx in (-1, 1):                                                  # stitched cross eyes
        ex = cx + sx * 48
        dr.line((ex - 16, cy - 46, ex + 16, cy - 14), fill=(46, 40, 44), width=8)
        dr.line((ex - 16, cy - 14, ex + 16, cy - 46), fill=(46, 40, 44), width=8)
    dr.arc((cx - 60, cy - 30, cx + 60, cy + 60), 20, 160, fill=(46, 40, 44), width=8)
    for k in range(-2, 3):
        dr.line((cx + k * 22, cy + 36, cx + k * 22, cy + 64), fill=(46, 40, 44), width=5)
    dr.ellipse((cx - 96, cy - 4, cx - 60, cy + 26), fill=(226, 140, 120))
    dr.ellipse((cx + 60, cy - 4, cx + 96, cy + 26), fill=(226, 140, 120))
    c = np.asarray(im, np.float32) / 255
    c *= (1 + 0.05 * soft(rng, (q, W), (2, 2)))[..., None]
    a[3 * q:] = c
    save(down(a, SS), "py_terrace_wood.png")


def wet():
    W, Hh = 256, 512
    rng = np.random.default_rng(41)
    xs = np.linspace(-1, 1, W, dtype=np.float32)[None, :]
    ys = np.linspace(0, 1, Hh, dtype=np.float32)[:, None]
    ragged = 0.25 * soft(rng, (Hh, W), (40, 4))
    core = np.clip((0.86 - np.abs(xs) * (1.0 + 0.2 * ys) + ragged) * 3.0, 0, 1)
    al = core * np.clip((1.0 - ys) * 6, 0, 1) * np.clip(ys * 30, 0, 1)
    a = np.zeros((Hh, W, 4), np.float32)
    a[..., :3] = f((52, 50, 52))
    a[..., :3] = mix(a[..., :3], f((40, 104, 96)), np.clip(soft(rng, (Hh, W), (60, 6)) * 2, 0, 1) * 0.6)    # green slime threads
    a[..., :3] = mix(a[..., :3], f((120, 190, 200)), np.clip(soft(rng, (Hh, W), (50, 2)) * 3 - 1.8, 0, 1) * 0.7)  # wet glints
    a[..., 3] = al * 0.85
    save(a, "py_wet.png")


def foam():
    W = 128
    rng = np.random.default_rng(51)
    a = np.empty((W, W, 3), np.float32)
    a[:] = f((244, 252, 250))
    a = mix(a, f((190, 232, 236)), np.clip(soft(rng, (W, W), 10, True) * 1.5, 0, 1) * 0.7)
    save(a, "py_foam.png")


JOBS = dict(wall=wall, ground=ground, plant=plant, fall=fall, rock=rock, wood=wood, wet=wet, foam=foam)
if __name__ == "__main__":
    for name in (sys.argv[1:] or list(JOBS)):
        JOBS[name]()
