"""PAYO land kit, second set of pictures (system Python, numpy + Pillow). NEW names only: the first set
(terrace_paint.py) is approved and locked, and this script never writes those files.

  py -3 tools/payo_pieces/terrace_paint2.py [far pit ground]

  py_terrace_far.png   1024 x 2048 RGBA, the far terrace's cards: rice rows seen from the side and from above (young
                       and ripe), the weeds hanging over the cap, the grass on the dike. 256 px a metre, tiles along u (4 m).
  py_pit_wall.png      2048 x 2048, the pit wall's whole face (12 x 12 m), every stone painted where
                       terrace_pit_layout.stones() puts it, with the carved band.
  py_ground_grass.png, py_ground_forest.png   1024 x 1024, tiling both ways, 4 m a tile.
"""
import math
import random
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi

sys.path.insert(0, str(Path(__file__).resolve().parent))
import terrace_paint as P1        # noqa: E402  (helpers only; its painters are not called)
import terrace_pit_layout as PL   # noqa: E402

f, soft, mix, down, save = P1.f, P1.soft, P1.mix, P1.down, P1.save
PALE, INK, DARK, MOSS, MOSS_L, MOSS_D = P1.PALE, P1.INK, P1.DARK, P1.MOSS, P1.MOSS_L, P1.MOSS_D
LOCKED = {"py_terrace_wall.png", "py_terrace_ground.png", "py_terrace_plant.png", "py_fall.png", "py_rock.png", "py_terrace_wood.png", "py_wet.png", "py_foam.png"}


def keep(a, name):
    assert name not in LOCKED, name
    save(a, name)


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


# ------------------------------------------------------------------------------------------------ far terrace cards
def far():
    SS = 2
    W, Hh = 1024 * SS, 2048 * SS
    ppm = 256 * SS
    final = Image.new("RGBA", (W, Hh), (0, 0, 0, 0))
    im = Image.new("RGBA", (W, Hh), (0, 0, 0, 0))
    dr = ImageDraw.Draw(im)
    r = random.Random(61)
    TONE = 0.92                                                         # the cards sit a little darker than the paint: they get no shadow of their own

    def band_done(y0, y1):
        """Keep only this band's rows of what was just drawn: nothing may spill into the band below."""
        nonlocal im, dr
        final.paste(im.crop((0, y0, W, y1)), (0, y0))
        im = Image.new("RGBA", (W, Hh), (0, 0, 0, 0))
        dr = ImageDraw.Draw(im)

    def poly(pts, c):
        g = sum(c) / 3.0
        c = tuple(max(0, min(255, int((g + (v - g) * 1.18) * TONE))) for v in c)        # a little more colour than the near paint
        for ox in (-W, 0, W):
            dr.polygon([(x + ox, y) for x, y in pts], fill=c + (255,))

    def side_blade(x, yb, h, w, lean, c0, c1, droop=0.0):
        mx, my = x + lean * h * 0.30, yb - h * 0.58
        tx, ty = x + lean * h * (1 + droop), yb - h * (1 - droop)
        poly([(x - w / 2, yb), (x + w / 2, yb), (mx + w * 0.42, my), (tx, ty), (mx - w * 0.42, my)], lerp(c0, c1, 0.35))
        poly([(mx - w * 0.42, my), (mx + w * 0.42, my), (tx, ty)], c1)
        poly([(x - w * 0.12, yb), (x + w * 0.12, yb), (mx + w * 0.1, my), (mx - w * 0.1, my)], lerp(c0, c1, 0.6))

    def side_row(y0, band, kind):
        yb = y0 + band - 2
        for depth, shade, step, tall in ((1, 0.80, 0.28, 0.52), (0, 1.0, 0.22, 0.74)) if kind == "young" else ((1, 0.82, 0.33, 0.66), (0, 1.0, 0.26, 0.74)):
            x = 0.1 * ppm
            while x < W:
                h = tall * r.uniform(0.9, 1.1) * ppm
                c0 = (40, 108, 50) if kind == "young" else (112, 132, 52)
                c1 = (186, 228, 84) if kind == "young" else (232, 200, 86)
                c0 = tuple(int(v * shade * 0.75) for v in c0); c1 = tuple(int(v * shade) for v in c1)
                for k in range(6 if kind == "young" else 4):
                    side_blade(x + r.uniform(-0.03, 0.03) * ppm, yb, h * r.uniform(0.8, 1.08), 0.085 * ppm, r.uniform(-0.5, 0.5), c0, c1)
                if kind == "ripe":
                    for k in range(3):
                        side_blade(x, yb, h * 1.05, 0.075 * ppm, r.choice((-1, 1)) * r.uniform(0.45, 0.8), (214, 160, 52), (250, 214, 96), droop=0.16)
                x += step * ppm * r.uniform(0.9, 1.1)

    def top_rows(y0, band, kind):
        row = 0
        d = 0.17
        while d < band / ppm - 0.1:
            y = y0 + band - d * ppm
            x = 0.1 * ppm
            while x < W:
                n = (6 if row == 0 else 5 if row == 1 else 4) if kind == "young" else 4
                tall = ((0.74 if row == 0 else 0.56 if row == 1 else 0.44) if kind == "young" else 0.7) * r.uniform(0.9, 1.1)
                a0 = r.uniform(0, 6.28)
                cx, cy = x + r.uniform(-0.04, 0.04) * ppm, y + r.uniform(-0.03, 0.03) * ppm
                c0 = (40, 108, 50) if kind == "young" else (112, 132, 52)
                c1 = (186, 228, 84) if kind == "young" else (232, 200, 86)
                sh = 1.0 if row == 0 else 0.92
                for k in range(n):
                    a = a0 + k * 6.283 / n + r.uniform(-0.3, 0.3)
                    ln = 0.42 * tall * r.uniform(0.6, 1.3) * ppm
                    w = 0.075 * ppm
                    ux, uy = math.cos(a), math.sin(a)
                    px, py = -uy, ux
                    b = (cx + ux * 0.03 * ppm, cy + uy * 0.03 * ppm)
                    m = (b[0] + ux * ln * 0.3, b[1] + uy * ln * 0.3)
                    t = (b[0] + ux * ln, b[1] + uy * ln)
                    poly([(b[0] - px * w / 2, b[1] - py * w / 2), (b[0] + px * w / 2, b[1] + py * w / 2), (m[0] + px * w * 0.42, m[1] + py * w * 0.42),
                          (m[0] - px * w * 0.42, m[1] - py * w * 0.42)], tuple(int(v * 0.62 * sh) for v in lerp(c0, c1, 0.3)))
                    poly([(m[0] + px * w * 0.42, m[1] + py * w * 0.42), t, (m[0] - px * w * 0.42, m[1] - py * w * 0.42)], tuple(int(v * sh) for v in lerp(c0, c1, 0.75)))
                if kind == "ripe":
                    for k in range(3):
                        a = r.uniform(0, 6.28)
                        ln = 0.75 * tall * 1.16 * ppm * r.uniform(0.7, 1.1)
                        ux, uy = math.cos(a), math.sin(a)
                        w = 0.075 * ppm
                        poly([(cx - uy * w / 2, cy + ux * w / 2), (cx + uy * w / 2, cy - ux * w / 2), (cx + ux * ln, cy + uy * ln)], r.choice([(238, 196, 78), (250, 214, 96), (220, 166, 56)]))
                x += ((0.22 if row == 0 else 0.28) if kind == "young" else (0.26 if row == 0 else 0.33)) * ppm
            d += 0.30
            row += 1

    b = 256 * SS
    side_row(0, b, "young"); band_done(0, b)
    side_row(b, b, "ripe"); band_done(b, 2 * b)
    top_rows(2 * b, 2 * b, "young"); band_done(2 * b, 4 * b)
    top_rows(4 * b, 2 * b, "ripe"); band_done(4 * b, 6 * b)
    # the weeds hanging over the cap stones: they hang from the TOP edge of their band
    y0 = 6 * b
    x = 0.0
    while x < W:
        if r.random() < 0.72:
            for k in range(r.randint(1, 3)):
                xx = x + r.uniform(-0.06, 0.06) * ppm
                ln = r.uniform(0.14, 0.40) * ppm * 1.1
                w = r.uniform(0.07, 0.10) * ppm
                sk = r.uniform(-0.35, 0.35) * ln * 0.5
                sh = r.uniform(0.72, 1.0)
                poly([(xx - w / 2, y0 + 4), (xx + w / 2, y0 + 4), (xx + w * 0.4 + sk * 0.2, y0 + ln * 0.25), (xx + sk, y0 + ln), (xx - w * 0.4 + sk * 0.2, y0 + ln * 0.25)],
                     tuple(int(v * sh) for v in (96, 172, 80)))
                poly([(xx - w / 2, y0), (xx + w / 2, y0), (xx + w * 0.4 + sk * 0.2, y0 + ln * 0.25), (xx - w * 0.4 + sk * 0.2, y0 + ln * 0.25)], tuple(int(v * sh) for v in (60, 132, 62)))
        x += 0.15 * ppm
    band_done(6 * b, 6 * b + b // 2)
    # FAR ONLY: the paddy seen from a distance as one canopy (young, then ripe), and the flooded paddy as a strip of sky.
    # Soft broken streaks along the rows, never ruled lines: 2,600 of these must not draw a grid.
    rngf = np.random.default_rng(62)

    def fill(y0, y1, base, light, dark, rows):
        hh = y1 - y0
        a = np.empty((hh, W, 3), np.float32)
        a[:] = f(base)
        yy = np.linspace(0, 1, hh, dtype=np.float32)[:, None]
        wob = 0.6 * soft(rngf, (hh, W), (2, 90), True)
        st = np.sin((yy * rows + wob) * 6.283)
        a = mix(a, f(light), np.clip(st, 0, 1) * 0.45)
        a = mix(a, f(dark), np.clip(-st, 0, 1) * 0.40)
        a *= (1 + 0.10 * soft(rngf, (hh, W), (10, 60), True))[..., None]
        im.paste(Image.fromarray((np.clip(np.dstack([a, np.ones((hh, W, 1), np.float32)]), 0, 1) * 255).astype(np.uint8), "RGBA"), (0, y0))

    q = b // 4
    fill(6 * b + 2 * q, 6 * b + 3 * q, (104, 182, 62), (158, 216, 78), (62, 138, 56), 3)
    fill(6 * b + 3 * q, 7 * b, (218, 180, 70), (240, 206, 96), (168, 140, 58), 3)
    fill(7 * b, 7 * b + 2 * q, (150, 204, 232), (192, 228, 240), (112, 176, 214), 1)
    band_done(6 * b + 2 * q, 7 * b + 2 * q)
    # grass standing on the dike crest, a red flower now and then
    yb = 8 * b - 2
    x = 0.0
    while x < W:
        if r.random() < 0.45:
            h = r.uniform(0.16, 0.28) * ppm
            for k in range(5):
                side_blade(x, yb, h * r.uniform(0.8, 1.1), 0.045 * ppm, r.uniform(-0.8, 0.8), (30, 90, 48), (134, 200, 92))
            if r.random() < 0.35:
                fy = yb - r.uniform(0.26, 0.34) * ppm
                dr.line((x, yb, x + 4, fy), fill=(70, 140, 70, 255), width=5)
                for ox in (-W, 0, W):
                    dr.ellipse((x + ox - 16, fy - 14, x + ox + 22, fy + 16), fill=(230, 70, 54, 255))
                    dr.ellipse((x + ox - 2, fy - 4, x + ox + 8, fy + 6), fill=(250, 210, 80, 255))
        x += r.uniform(0.22, 0.42) * ppm
    band_done(7 * b + 2 * q, 8 * b)
    a = np.asarray(final, np.float32) / 255
    # bleed colour under the transparent pixels so the cut-out has no dark fringe
    rgb, al = a[..., :3], a[..., 3]
    filled = rgb.copy()
    inside = al > 0.5
    idx = ndi.distance_transform_edt(~inside, return_distances=False, return_indices=True)
    filled = rgb[idx[0], idx[1]]
    a[..., :3] = filled
    keep(down(a, SS), "py_terrace_far.png")


# ------------------------------------------------------------------------------------------------ one stone, painted
def stone(col, alp, pts, tone, r, rng, ppm, moss_p=0.4, cap=False, bev=0.05):
    Hh, ext = alp.shape
    light = np.array([-0.45, -0.89])
    xs, ys = [p[0] for p in pts], [p[1] for p in pts]
    m = 16
    x0, x1 = max(0, int(min(xs)) - m), min(ext, int(max(xs)) + m)
    y0, y1 = max(0, int(min(ys)) - m), min(Hh, int(max(ys)) + m)
    w, h = x1 - x0, y1 - y0
    im = Image.new("L", (w, h), 0)
    ImageDraw.Draw(im).polygon([(x - x0, y - y0) for x, y in pts], fill=255)
    mask = np.asarray(im, np.float32) / 255
    mask = np.clip((ndi.gaussian_filter(mask, r.uniform(9, 24)) - 0.5) * 6 + 0.5, 0, 1)
    inside = mask > 0.5
    d = ndi.distance_transform_edt(inside).astype(np.float32)
    gy, gx = np.gradient(ndi.gaussian_filter(d, 2.0))
    n = np.sqrt(gx * gx + gy * gy) + 1e-6
    lit = (gx / n) * light[0] + (gy / n) * light[1]
    # big stones get a second, broader mottle so a megalith is not one flat tone
    big_stone = h > 0.9 * ppm
    yy = np.linspace(0, 1, h, dtype=np.float32)[:, None]
    c = np.empty((h, w, 3), np.float32)
    c[:] = f(tone)
    c *= (1.07 - 0.16 * yy)[..., None]
    c *= (1 + 0.10 * soft(rng, (h, w), 20))[..., None]
    if big_stone:
        c *= (1 + 0.09 * soft(rng, (h, w), 60))[..., None]
    c *= (1 + 0.03 * soft(rng, (h, w), (3, 11)))[..., None]
    wash = np.clip(soft(rng, (h, w), 26) * 1.4, 0, 1)
    c = mix(c, c * f((176, 164, 146)) / f((150, 148, 144)), wash * 0.55)
    c *= (1 + 0.07 * soft(rng, (h, w), 5))[..., None]
    c *= (1 - 0.10 * np.clip(soft(rng, (h, w), 2.2) * 3 - 1.5, 0, 1))[..., None]
    bw = bev * ppm
    band = np.clip((bw - d) / 2.0, 0, 1) * inside
    up = np.clip(lit, 0, 1) ** 0.7
    dn = np.clip(-lit, 0, 1) ** 0.7
    brk = np.clip(0.75 + 0.6 * soft(rng, (h, w), (3, 9)), 0.25, 1)
    c = mix(c, f(PALE), band * up * 0.62 * brk)
    c = mix(c, f(DARK), band * dn * 0.55)
    line = np.clip(1 - np.abs(d - bw) / 1.6, 0, 1) * inside
    c = mix(c, f(PALE), line * up * 0.10)
    c = mix(c, f(DARK), line * dn * 0.22)
    if r.random() < 0.38:
        cr = Image.new("L", (w, h), 0)
        dc = ImageDraw.Draw(cr)
        px, py = r.uniform(m, w - m), m + 2
        ang = r.uniform(1.1, 2.0)
        path = [(px, py)]
        for _ in range(r.randint(3, 6)):
            ang += r.uniform(-0.7, 0.7)
            px += math.cos(ang) * r.uniform(8, 22); py += abs(math.sin(ang)) * r.uniform(8, 22)
            path.append((px, py))
        dc.line(path, fill=255, width=3)
        crm = np.asarray(cr, np.float32) / 255 * inside
        c = mix(c, f(DARK) * 0.8, crm * 0.85)
        c = mix(c, f(PALE), np.roll(crm, (2, 2), (0, 1)) * (1 - crm) * inside * 0.45)
    sp = Image.new("L", (w, h), 128)
    dp = ImageDraw.Draw(sp)
    for _ in range(r.randint(6, 14)):
        px, py, q = r.uniform(m, w - m), r.uniform(m, h - m), r.uniform(2, 6.5)
        dp.ellipse((px - q, py - q * 0.7, px + q, py + q * 0.7), fill=r.choice((70, 80, 190, 200)))
    c *= (1 + (np.asarray(sp, np.float32) - 128) / 255 * 0.5 * inside)[..., None]
    if r.random() < moss_p:
        g = np.clip(yy * 1.5 - 1.0 + 0.55 * soft(rng, (h, w), (7, 14)), 0, 1) * inside
        g = np.clip(g * 2.0, 0, 1)
        c = mix(c, f(MOSS), g * 0.88)
        c = mix(c, f(MOSS_L), np.clip(g - 0.55, 0, 1) * np.clip(soft(rng, (h, w), 3) * 3, 0, 1) * 0.9)
    if r.random() < 0.25:
        li = np.clip(soft(rng, (h, w), 5) * 3 - 1.6, 0, 1) * inside
        c = mix(c, f((206, 190, 120)), li * 0.8)
    col[y0:y1, x0:x1] = mix(col[y0:y1, x0:x1], c, mask)
    alp[y0:y1, x0:x1] = np.maximum(alp[y0:y1, x0:x1], mask)
    return (x0, y0, x1, y1), inside


def carve(col, box, r, k):
    """The carved band's block: a stepped fret, a diamond with a lizard's four legs, worn ochre and red in the cuts."""
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    cut = Image.new("L", (w, h), 0)
    dr = ImageDraw.Draw(cut)
    cx, cy = w / 2, h / 2
    u = h / 9.0
    lw = int(u * 0.75)
    dr.rectangle((u * 1.2, u * 1.2, w - u * 1.2, h - u * 1.2), outline=255, width=max(3, lw // 2))        # the frame
    if k % 2 == 0:
        for sx in (-1, 1):                                                                                # stepped fret, mirrored
            px = cx + sx * u * 2.0
            pts = [(px, cy + u * 2.2), (px + sx * u * 6.5, cy + u * 2.2), (px + sx * u * 6.5, cy - u * 2.2), (px + sx * u * 2.0, cy - u * 2.2),
                   (px + sx * u * 2.0, cy + u * 0.4), (px + sx * u * 4.3, cy + u * 0.4), (px + sx * u * 4.3, cy - u * 0.8)]
            dr.line(pts, fill=255, width=lw, joint="curve")
        dr.polygon([(cx, cy - u * 1.6), (cx + u * 1.2, cy), (cx, cy + u * 1.6), (cx - u * 1.2, cy)], outline=255, width=lw // 2 + 2)
    else:
        dr.polygon([(cx, cy - u * 2.4), (cx + u * 2.2, cy), (cx, cy + u * 2.4), (cx - u * 2.2, cy)], outline=255, width=lw)   # diamond
        dr.ellipse((cx - u * 0.7, cy - u * 0.7, cx + u * 0.7, cy + u * 0.7), fill=255)
        for sx in (-1, 1):                                                                                # the lizard's legs and stepped wings
            for sy in (-1, 1):
                dr.line([(cx + sx * u * 2.2, cy), (cx + sx * u * 3.6, cy + sy * u * 1.4), (cx + sx * u * 3.6, cy + sy * u * 2.3)], fill=255, width=lw, joint="curve")
            for q in range(3):
                xx = cx + sx * u * (5.0 + q * 1.5)
                dr.line([(xx, cy + u * (2.2 - q * 0.7)), (xx, cy - u * (2.2 - q * 0.7))], fill=255, width=lw)
    cm = np.asarray(cut, np.float32) / 255
    cm = np.clip(ndi.gaussian_filter(cm, 1.2) * 1.3, 0, 1)
    c = col[y0:y1, x0:x1]
    paint = f((190, 96, 62)) if k % 2 else f((206, 160, 78))                                              # worn paint left in the cut
    deep = mix(np.broadcast_to(f(DARK) * 0.75, c.shape), np.broadcast_to(paint, c.shape), 0.45)
    c2 = mix(c, deep, cm * 0.9)
    lip = np.clip(np.roll(cm, (5, 5), (0, 1)) - cm, 0, 1)                                                  # the lit lower-right lip of each cut
    c2 = mix(c2, f(PALE), lip * 0.6)
    shade = np.clip(np.roll(cm, (-4, -4), (0, 1)) - cm, 0, 1)
    c2 = mix(c2, f(DARK), shade * 0.35)
    col[y0:y1, x0:x1] = c2


def pit():
    SS = 2
    W = 2048 * SS
    ppm = W / PL.W
    rng = np.random.default_rng(71)
    r = random.Random(71)
    bg = np.empty((W, W, 3), np.float32)
    bg[:] = f(INK)
    damp = np.clip(soft(rng, (W, W), (30, 60), True) * 2.2 - 0.5, 0, 1)
    bg = mix(bg, f(MOSS_D), np.clip(damp * 2 - 1.0, 0, 1) * 0.7)
    col = np.zeros((W, W, 3), np.float32)
    alp = np.zeros((W, W), np.float32)
    k = 0
    for st in PL.stones():
        pts = [(s * ppm, W - t / PL.Hh * W) for s, t in PL.corners(st)]
        under_ledge = abs(st["t1"] - PL.LEDGE[0]) < 0.01 or st["t0"] < 0.01
        box, inside = stone(col, alp, pts, st["tone"], r, rng, ppm, moss_p=0.75 if under_ledge else 0.30, bev=st["bev"] - 0.01)
        if st["kind"] == "band":
            carve(col, box, r, k)
            k += 1
    out = mix(bg, col, alp)
    rows = np.linspace(0, 1, W, dtype=np.float32)[:, None]
    # long damp streaks running down from the ledge and the top, green then dark
    streak = np.clip(soft(rng, (W, W), (120, 5), True) * 3 - 1.5, 0, 1)
    for top in (0.0, 1 - PL.LEDGE[1] / PL.Hh):
        fade = np.clip(1 - (rows - top) / 0.22, 0, 1) * (rows >= top)
        out = mix(out, f(MOSS_D), streak * fade * 0.55)
        out = mix(out, f(MOSS), np.clip(streak * fade * 2 - 1.2, 0, 1) * 0.6)
    foot = np.clip((rows - 0.965) / 0.035, 0, 1) * np.clip(0.6 + soft(rng, (W, W), (6, 40), True), 0, 1)
    out = mix(out, f(MOSS_D), foot * 0.6)
    keep(down(out, SS), "py_pit_wall.png")


# ------------------------------------------------------------------------------------------------ ground
def wrap_draw(dr, W, fn):
    for ox in (-W, 0, W):
        for oy in (-W, 0, W):
            fn(ox, oy)


def ground():
    SS = 2
    W = 1024 * SS
    r = random.Random(81)
    rng = np.random.default_rng(81)
    # GRASS: mid green, clumps of darker and of sunlit yellow-green, short strokes all leaning a little one way
    im = Image.new("RGB", (W, W), (74, 148, 62))
    dr = ImageDraw.Draw(im)
    greens = [(60, 134, 58), (86, 164, 66), (104, 180, 72), (50, 118, 58), (124, 192, 80), (70, 150, 66)]
    for _ in range(11000):
        x, y, ln, a = r.uniform(0, W), r.uniform(0, W), r.uniform(18, 50), r.uniform(-0.2, 0.55)
        c, w = r.choice(greens), r.randint(3, 6)
        wrap_draw(dr, W, lambda ox, oy: dr.line((x + ox, y + oy, x + ox + math.sin(a) * ln, y + oy - math.cos(a) * ln), fill=c, width=w))
    a = np.asarray(im, np.float32) / 255
    big = soft(rng, (W, W), 90, True)
    a = mix(a, a * f((52, 110, 60)) / f((74, 148, 62)), np.clip(big * 1.6, 0, 1) * 0.7)
    a = mix(a, a * f((132, 190, 76)) / f((74, 148, 62)), np.clip(-big * 1.6, 0, 1) * 0.6)
    im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
    dr = ImageDraw.Draw(im)
    for _ in range(900):                                                   # sunlit blades on top
        x, y, ln, an = r.uniform(0, W), r.uniform(0, W), r.uniform(16, 36), r.uniform(-0.2, 0.55)
        wrap_draw(dr, W, lambda ox, oy: dr.line((x + ox, y + oy, x + ox + math.sin(an) * ln, y + oy - math.cos(an) * ln), fill=(170, 220, 96), width=3))
    for _ in range(26):                                                    # a few tiny flowers
        x, y, c = r.uniform(0, W), r.uniform(0, W), r.choice([(230, 70, 54), (250, 226, 120), (244, 244, 236)])
        wrap_draw(dr, W, lambda ox, oy: dr.ellipse((x + ox - 7, y + oy - 7, x + ox + 7, y + oy + 7), fill=c))
    keep(down(np.asarray(im, np.float32) / 255, SS), "py_ground_grass.png")

    # FOREST FLOOR: dark damp earth, moss, a litter of fallen leaves and twigs
    a = np.empty((W, W, 3), np.float32)
    a[:] = f((86, 68, 48))
    b = soft(rng, (W, W), 70, True)
    a = mix(a, f((54, 46, 38)), np.clip(b * 1.5, 0, 1) * 0.85)
    a = mix(a, f((112, 88, 58)), np.clip(-b * 1.5, 0, 1) * 0.6)
    mo = np.clip(soft(rng, (W, W), 40, True) * 2.4 - 0.9, 0, 1)
    a = mix(a, f((66, 106, 54)), mo * 0.85)
    a = mix(a, f((120, 160, 70)), np.clip(mo * 2 - 1.3, 0, 1) * np.clip(soft(rng, (W, W), 4, True) * 3, 0, 1) * 0.8)
    a *= (1 + 0.06 * soft(rng, (W, W), 6, True))[..., None]
    im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
    dr = ImageDraw.Draw(im)
    for _ in range(40):                                                    # twigs
        x, y, ln, an = r.uniform(0, W), r.uniform(0, W), r.uniform(60, 190), r.uniform(0, 6.28)
        wrap_draw(dr, W, lambda ox, oy: dr.line((x + ox, y + oy, x + ox + math.cos(an) * ln, y + oy + math.sin(an) * ln), fill=(58, 44, 36), width=7))
        wrap_draw(dr, W, lambda ox, oy: dr.line((x + ox + 2, y + oy - 2, x + ox + 2 + math.cos(an) * ln, y + oy - 2 + math.sin(an) * ln), fill=(126, 98, 70), width=2))
    leafc = [(170, 98, 48), (198, 158, 72), (124, 128, 60), (100, 74, 48), (150, 76, 44), (214, 180, 96), (86, 110, 56)]
    for _ in range(520):
        x, y, ln, an = r.uniform(0, W), r.uniform(0, W), r.uniform(26, 58), r.uniform(0, 6.28)
        c = r.choice(leafc)
        wd = ln * r.uniform(0.32, 0.5)
        ux, uy = math.cos(an), math.sin(an)
        pts = [(x - ux * ln / 2, y - uy * ln / 2), (x - uy * wd / 2, y + ux * wd / 2), (x + ux * ln / 2, y + uy * ln / 2), (x + uy * wd / 2, y - ux * wd / 2)]
        dk = tuple(int(v * 0.62) for v in c)
        lt = tuple(min(255, int(v * 1.25)) for v in c)
        wrap_draw(dr, W, lambda ox, oy: dr.polygon([(px + ox + 4, py + oy + 5) for px, py in pts], fill=(40, 34, 30)))
        wrap_draw(dr, W, lambda ox, oy: dr.polygon([(px + ox, py + oy) for px, py in pts], fill=c, outline=dk))
        wrap_draw(dr, W, lambda ox, oy: dr.line((pts[0][0] + ox, pts[0][1] + oy, pts[2][0] + ox, pts[2][1] + oy), fill=lt, width=3))
    for _ in range(30):                                                    # small pale pebbles
        x, y, q = r.uniform(0, W), r.uniform(0, W), r.uniform(8, 18)
        wrap_draw(dr, W, lambda ox, oy: dr.ellipse((x + ox - q, y + oy - q * 0.75, x + ox + q, y + oy + q * 0.75), fill=(150, 148, 142), outline=(70, 66, 64)))
    keep(down(np.asarray(im, np.float32) / 255, SS), "py_ground_forest.png")


def farwall():
    """The far terrace's wall strip: the approved wall picture (read, never written), flattened, darkened and mossed, so
    a hillside of them is a quiet grey-green band with no pale stones or joints to line up."""
    src = np.asarray(Image.open(P1.TEX / "py_terrace_wall.png").convert("RGB"), np.float32) / 255
    rng = np.random.default_rng(63)
    hh, ww = src.shape[:2]
    a = ndi.gaussian_filter(src, (2.0, 5.0, 0), mode="wrap")
    mean = np.array([0.50, 0.50, 0.48], np.float32)
    a = mean + (a - mean) * 0.35
    a *= 0.82
    moss = np.clip(0.42 + 0.5 * soft(rng, (hh, ww), (10, 70), True), 0, 1)
    a = mix(a, f((70, 100, 66)), moss * 0.42)
    rows = np.linspace(0, 1, hh, dtype=np.float32)[:, None]
    cap = np.clip((0.26 - rows) / 0.05, 0, 1)                       # the cap and the top edge: green, weeds lie over it
    a = mix(a, f((70, 128, 56)), cap * 0.8)
    foot = np.clip((rows - 0.80) / 0.2, 0, 1)
    a = mix(a, f((50, 84, 52)), foot * 0.5)
    a = down(a, 2)
    keep(a, "py_terrace_far_wall.png")


JOBS = dict(far=far, pit=pit, ground=ground, farwall=farwall)
if __name__ == "__main__":
    for name in (sys.argv[1:] or list(JOBS)):
        JOBS[name]()
