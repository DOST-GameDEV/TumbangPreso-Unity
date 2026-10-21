"""PAYO court platform: paint its ONE atlas, the CALM way (Logs/payo/pieces/CALM_STYLE.md). System Python, numpy, Pillow, scipy.

  py tools/payo_pieces/court_paint.py

Writes Logs/payo/pieces/calm/court/court_platform-atlas.png, court_platform_height.png (16 bit) and
court_platform_normal.png (OpenGL, green up). The layout (a trim sheet: rows that tile in U) is court_layout.py.

The calm rules as this piece keeps them: one shared calm stone (165, 156, 140), stones close in value with faint soft
joints (8 percent), a broad soft drift across groups of stones, never per-stone noise; a pale bevel that is a subtle
lighter edge; the long light-to-dark gradient is the vertex colour's, so the rows here are even. Motifs: one bold
carved fret course (ochre and terracotta in the cut, as the old pit wall's), the red, black and white woven band on
the coping's outer face, turquoise only in the niches and spout panels, an ochre sun on the retaining walls' panels.
Relief goes into the height map (joints as soft grooves, pillowed blocks, the carving cut in), baked to normals.
"""
import random
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage as ndi

sys.path.insert(0, str(Path(__file__).resolve().parent))
import court_layout as L        # noqa: E402
import calm_glb                 # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Logs/payo/pieces/calm/court"
OUT.mkdir(parents=True, exist_ok=True)

STONE = (165, 156, 140)
PALE = (226, 216, 196)
CMOSS = (126, 144, 92)
OCHRE, TERRA = (196, 160, 96), (178, 108, 80)
TURQ = (78, 182, 172)
ROSE = (184, 146, 132)
RED, BLACK, CREAM = (172, 58, 48), (52, 44, 48), (234, 224, 202)
NSTRENGTH = 3.0
S = L.SIZE
P = L.PPM


def f(c):
    return np.array(c, np.float32) / 255.0


def mix(a, b, t):
    if np.ndim(t) == 2:
        t = t[..., None]
    return a * (1 - t) + b * t


def smooth(x):
    x = np.clip(x, 0, 1)
    return x * x * (3 - 2 * x)


def soft(rng, shape, sigma, wrap=(False, True)):
    """A soft field in -1..1: big brush blotches, never noise. Wraps along x (the rows tile in U)."""
    n = rng.standard_normal(shape).astype(np.float32)
    n = ndi.gaussian_filter(n, sigma, mode=("wrap" if wrap[0] else "reflect", "wrap" if wrap[1] else "reflect"))
    return n / (np.abs(n).max() + 1e-6)


COL = np.zeros((S, S, 3), np.float32)
COL[:] = f(STONE)
H = np.zeros((S, S), np.float32)


# ------------------------------------------------------------------------------------------------ coursed blocks
def courses(height_m, rows, rng, r, base=STONE, tone=0.012, joint=0.90, bev=0.06, pillow=0.012, groove=0.010,
            drift=0.025, drift_m=3.0, vtile=True):
    """A strip `height_m` tall and PERIOD wide (tiles in U; in V too when vtile). rows: [(y0 m, y1 m, [x edges m])]
    where the edges are cut points along a 32 m loop (the first edge wraps round). Returns colour, height."""
    hp, wp = int(round(height_m * P)), S
    ys = (np.arange(hp, dtype=np.float32) + 0.5) / P
    xs = (np.arange(wp, dtype=np.float32) + 0.5) / P
    col = np.zeros((hp, wp, 3), np.float32)
    Hh = np.zeros((hp, wp), np.float32)
    for (y0, y1, edges) in rows:
        sel = (ys >= y0) & (ys < y1)
        if not sel.any():
            continue
        yy = ys[sel][:, None]
        e = np.sort(np.asarray(edges, np.float32) % L.PERIOD)
        k = np.searchsorted(e, xs, side="right") - 1               # -1 = the stone that wraps round
        left = np.where(k >= 0, e[np.clip(k, 0, None)], e[-1] - L.PERIOD)
        right = np.where(k + 1 < len(e), e[np.clip(k + 1, None, len(e) - 1)], e[0] + L.PERIOD)
        k = np.where(k < 0, len(e) - 1, k)
        dx = np.minimum(xs - left, right - xs)[None, :]
        dy = np.minimum(yy - y0, y1 - yy)
        d = np.minimum(dx, dy)                                     # metres to the stone's edge
        tones = np.array([r.uniform(1 - tone, 1 + tone) for _ in e], np.float32)
        c = f(base)[None, None, :] * tones[k][None, :, None]
        band = smooth(1 - d / bev)                                 # the bevel band just inside the edge
        c = mix(c, f(PALE), band * 0.10)                           # a subtle lighter edge, not a stripe
        jm = smooth(1 - d / 0.035)                                 # the joint: soft, about 5 cm
        c = mix(c, c * joint, jm)
        col[sel] = np.broadcast_to(c, (yy.shape[0], wp, 3))
        Hh[sel] = pillow * smooth(d / (bev * 1.8)) - groove * jm
    g = soft(rng, (hp, wp), (drift_m * P * 0.5, drift_m * P), (vtile, True))
    col *= (1 + drift * g)[..., None]
    Hh += 0.003 * soft(rng, (hp, wp), 0.4 * P, (vtile, True))
    return col, Hh


def running(r, period, lo, hi, offset=0.0):
    """Cut points along a 32 m loop, stones lo..hi long, last one closing the loop."""
    n = max(1, int(round(period / ((lo + hi) / 2))))
    w = np.array([r.uniform(lo, hi) for _ in range(n)])
    w *= period / w.sum()
    return list((np.concatenate([[0], np.cumsum(w)[:-1]]) + offset) % period)


def place(y0, col, Hh, gutter=True, vtile=True):
    """Put a strip into the atlas at row y0 (+ gutter), filling the gutters with its wrap (or its edge rows)."""
    g = L.GUT if gutter else 0
    hp = col.shape[0]
    COL[y0 + g:y0 + g + hp] = col
    H[y0 + g:y0 + g + hp] = Hh
    if g:
        top = col[-g:] if vtile else np.repeat(col[:1], g, 0)
        bot = col[:g] if vtile else np.repeat(col[-1:], g, 0)
        COL[y0:y0 + g] = top; COL[y0 + g + hp:y0 + 2 * g + hp] = bot
        H[y0:y0 + g] = Hh[-g:] if vtile else np.repeat(Hh[:1], g, 0)
        H[y0 + g + hp:y0 + 2 * g + hp] = Hh[:g] if vtile else np.repeat(Hh[-1:], g, 0)


# ------------------------------------------------------------------------------------------------ the carved fret
def carve(col, Hh, x0, y0, x1, y1, k):
    """One block of the carved band: a BOLD stepped fret (even) or a bold diamond with stepped wings (odd), thick
    shapes cut in relief, soft ochre or terracotta in the cut (the old pit wall's motif, tools/payo_pieces/
    terrace_calm_paint.py pit_carve)."""
    w, h = x1 - x0, y1 - y0
    cut = Image.new("L", (w, h), 0)
    dr = ImageDraw.Draw(cut)
    cx, cy = w / 2, h / 2
    u = h / 9.0
    lw = int(u * 1.25)
    if k % 2 == 0:
        for sx in (-1, 1):
            px = cx + sx * u * 2.6
            pts = [(px, cy + u * 3.0), (px + sx * u * 6.9, cy + u * 3.0), (px + sx * u * 6.9, cy - u * 3.0), (px + sx * u * 2.4, cy - u * 3.0),
                   (px + sx * u * 2.4, cy + u * 0.5), (px + sx * u * 4.7, cy + u * 0.5), (px + sx * u * 4.7, cy - u * 0.9)]
            dr.line(pts, fill=255, width=lw, joint="curve")
        dr.polygon([(cx, cy - u * 2.4), (cx + u * 1.8, cy), (cx, cy + u * 2.4), (cx - u * 1.8, cy)], fill=255)
    else:
        dr.polygon([(cx, cy - u * 3.2), (cx + u * 4.2, cy), (cx, cy + u * 3.2), (cx - u * 4.2, cy)], outline=255, width=lw)
        dr.ellipse((cx - u * 1.0, cy - u * 1.0, cx + u * 1.0, cy + u * 1.0), fill=255)
        for sx in (-1, 1):
            for q in range(3):
                xx = cx + sx * u * (6.0 + q * 1.75)
                dr.line([(xx, cy + u * (3.0 - q * 0.7)), (xx, cy - u * (3.0 - q * 0.7))], fill=255, width=lw)
    cm = np.asarray(cut.filter(ImageFilter.GaussianBlur(max(1.2, u * 0.16))), np.float32) / 255
    paint = f(TERRA) if k % 2 else f(OCHRE)
    sl = (slice(y0, y1), slice(x0, x1))
    col[sl] = mix(col[sl], mix(col[sl], paint[None, None, :] * 0.92, 0.8), cm)
    Hh[sl] = Hh[sl] - 0.016 * cm


# ------------------------------------------------------------------------------------------------ rows
def paint_pave():
    rng = np.random.default_rng(301); r = random.Random(301)
    rows = [(0.0, 3.0, running(r, L.PERIOD, 2.2, 3.0)), (3.0, 6.0, running(r, L.PERIOD, 2.2, 3.0, 1.3))]
    base = tuple(int(c * 1.05) for c in STONE)
    col, Hh = courses(6.0, rows, rng, r, base=base, tone=0.014, joint=0.91, bev=0.08, pillow=0.010, drift=0.025, drift_m=6.0)
    place(L.PAVE[0], col, Hh)


def wall_rows(r, top, bottom, first_y=0.0):
    rows = []
    y = first_y
    off = 0.0
    while y < bottom - 1e-6:
        y1 = min(bottom, y + L.COURSE)
        off += r.uniform(0.9, 1.5)
        if y >= top - 1e-6:
            rows.append((y, y1, running(r, L.PERIOD, 2.2, 3.4, off)))
        y = y1
    return rows


def paint_wall():
    rng = np.random.default_rng(311); r = random.Random(311)
    col, Hh = courses(6.0, wall_rows(r, 0, 6.0), rng, r, drift_m=4.0)
    place(L.WALL[0], col, Hh)


def paint_wall_top():
    rng = np.random.default_rng(321); r = random.Random(321)
    f0, f1 = L.FRET
    rows = wall_rows(r, 0, 6.0)
    n = 12                                                       # the fret course: 12 carved blocks a period
    rows = [rw for rw in rows if not (rw[0] >= f0 - 1e-6 and rw[1] <= f1 + 1e-6)]
    rows.append((f0, f1, [i * L.PERIOD / n for i in range(n)]))
    col, Hh = courses(6.0, rows, rng, r, drift_m=4.0)
    y0, y1 = int(round(f0 * P)), int(round(f1 * P))
    # the carved course is a shade paler, a dressed band
    col[y0:y1] = mix(col[y0:y1], f(PALE), 0.12)
    m = int(0.10 * P)
    for i in range(n):
        x0 = int(round(i * L.PERIOD / n * P)) + m
        x1 = int(round((i + 1) * L.PERIOD / n * P)) - m
        carve(col, Hh, x0, y0 + m, x1, y1 - m, i)
    # a few big soft moss shapes just under the coping, nothing else
    mo = Image.new("L", (S, col.shape[0]), 0)
    d = ImageDraw.Draw(mo)
    for cx, sw in ((5.0, 1.4), (19.5, 1.0), (27.0, 0.7)):
        for ox in (-L.PERIOD, 0, L.PERIOD):
            d.ellipse([(cx + ox - sw) * P, -0.25 * P, (cx + ox + sw) * P, 0.35 * P], fill=255)
    mo = np.asarray(mo.filter(ImageFilter.GaussianBlur(P * 0.08)), np.float32) / 255
    col = mix(col, f(CMOSS), mo * 0.6)
    Hh += 0.005 * mo
    place(L.WALL_TOP[0], col, Hh, vtile=False)


def strip(name, edges, r, rng, base, tone=0.02, joint=0.93, bev=0.05, drift=0.03):
    y0, m = L.ROWS[name]
    col, Hh = courses(m, [(0.0, m, edges)], rng, r, base=base, tone=tone, joint=joint, bev=min(bev, m / 3), drift=drift,
                      drift_m=4.0, vtile=False)
    COL[y0:y0 + col.shape[0]] = col
    H[y0:y0 + col.shape[0]] = Hh
    return y0, col.shape[0]


def woven(name):
    """The woven band, bold, on the coping's outer face: a red ground, a chain of big black diamonds with cream
    hearts, a cream stripe and a black border each side. No thread lines."""
    y0, m = L.ROWS[name]
    hp = int(round(m * P))
    ys = (np.arange(hp, dtype=np.float32) + 0.5) / hp * 2 - 1            # -1 .. 1 across the face
    xs = (np.arange(S, dtype=np.float32) + 0.5) / P
    per = L.PERIOD / 54                                                  # 54 diamonds a period: about 0.59 m
    u = (xs / per) % 1.0
    a = np.abs(ys)[:, None]
    dia = np.abs(u - 0.5)[None, :] * 2 * 0.9 + a / 0.55
    c = np.empty((hp, S, 3), np.float32); c[:] = f(RED)
    c[dia < 0.80] = f(BLACK)
    c[dia < 0.40] = f(CREAM)
    edge = np.broadcast_to(a, dia.shape)
    c[(edge > 0.62) & (edge < 0.76)] = f(CREAM)
    c[edge >= 0.76] = f(BLACK)
    # soften the drawing a touch so it is painted, not printed
    c = ndi.gaussian_filter(c, (0.8, 0.8, 0))
    Hh = (0.004 * smooth((edge - 0.6) / 0.2) + 0.004 * (1 - smooth((dia - 0.7) / 0.2))).astype(np.float32)
    COL[y0:y0 + hp] = c * 0.96
    H[y0:y0 + hp] = Hh


def paint_trim():
    rng = np.random.default_rng(331); r = random.Random(331)
    cap = running(r, L.PERIOD, 2.2, 2.8)                          # the cap stones: their joints line up round the profile
    pale = tuple(int(a * 0.82 + b * 0.18) for a, b in zip(STONE, PALE))
    under = tuple(int(c * 0.86) for c in STONE)
    strip("border", running(r, L.PERIOD, 1.4, 1.9), r, rng, tuple(int(c * 1.03) for c in STONE), joint=0.92)
    strip("cap_under_in", cap, r, rng, under)
    strip("cap_in", cap, r, rng, pale)
    strip("cap_top", cap, r, rng, tuple(int(c * 1.02) for c in pale))
    woven("cap_out")
    strip("cap_under_out", cap, r, rng, under)
    lip = running(r, L.PERIOD, 1.8, 2.4)
    strip("lip_in", lip, r, rng, pale)
    strip("lip_top", lip, r, rng, tuple(int(c * 1.02) for c in pale))
    strip("lip_out", lip, r, rng, pale)
    strip("lip_under", lip, r, rng, under)
    strip("string", running(r, L.PERIOD, 2.2, 2.8), r, rng, pale)


def patch(rect, base, rng, drift=0.03):
    x0, y0, w, h = rect
    c = np.empty((h, w, 3), np.float32); c[:] = f(base)
    c *= (1 + drift * soft(rng, (h, w), P * 1.0, (False, False)))[..., None]
    COL[y0:y0 + h, x0:x0 + w] = c
    H[y0:y0 + h, x0:x0 + w] = 0.002 * soft(rng, (h, w), P * 0.4, (False, False))
    return x0, y0, w, h


def shape_mask(w, h, draw, blur=1.5):
    im = Image.new("L", (w, h), 0)
    draw(ImageDraw.Draw(im), w, h)
    return np.asarray(im.filter(ImageFilter.GaussianBlur(blur)), np.float32) / 255


def paint_patches():
    rng = np.random.default_rng(341)
    pale = tuple(int(a * 0.84 + b * 0.16) for a, b in zip(STONE, PALE))
    patch(L.PLAIN, pale, rng)
    patch(L.DARK, tuple(int(c * 0.97) for c in STONE), rng)
    # the niche back: a dusty rose panel with a stone frame and one bold turquoise stepped diamond
    x0, y0, w, h = patch(L.NICHE_BACK, ROSE, rng, 0.025)
    sl = (slice(y0, y0 + h), slice(x0, x0 + w))

    def diamond(d, w, h):
        cx, cy, s = w / 2, h * 0.46, w * 0.34
        for k in range(3):                                         # a stepped diamond: three nested steps
            q = s * (1 - k * 0.3)
            pts = []
            for ang in range(4):
                import math
                a = ang * math.pi / 2
                pts.append((cx + q * math.sin(a), cy - q * 1.25 * math.cos(a)))
            d.polygon(pts, fill=255 if k != 1 else 0)
    m = shape_mask(w, h, diamond)
    COL[sl] = mix(COL[sl], f(TURQ), m * 0.9)
    H[sl] -= 0.012 * m
    # the spout panel: a carved disc with a turquoise ring (the water mark), a pale centre
    x0, y0, w, h = patch(L.SPOUT_PANEL, pale, rng)
    sl = (slice(y0, y0 + h), slice(x0, x0 + w))
    ring = shape_mask(w, h, lambda d, w, h: (d.ellipse((w * 0.14, h * 0.14, w * 0.86, h * 0.86), fill=255),
                                             d.ellipse((w * 0.28, h * 0.28, w * 0.72, h * 0.72), fill=0)))
    core = shape_mask(w, h, lambda d, w, h: d.ellipse((w * 0.38, h * 0.38, w * 0.62, h * 0.62), fill=255))
    COL[sl] = mix(COL[sl], f(TURQ), ring * 0.9)
    COL[sl] = mix(COL[sl], f(CREAM), core * 0.8)
    H[sl] += -0.014 * ring + 0.006 * core
    # the retaining walls' glyph panel: an ochre sun with terracotta rays, on a pale stone
    x0, y0, w, h = patch(L.GLYPH_PANEL, pale, rng)
    sl = (slice(y0, y0 + h), slice(x0, x0 + w))

    def rays(d, w, h):
        import math
        cx, cy = w / 2, h / 2
        for i in range(8):
            a = i * math.pi / 4
            p = [(cx + math.cos(a) * w * 0.44, cy + math.sin(a) * h * 0.44),
                 (cx + math.cos(a + 0.24) * w * 0.27, cy + math.sin(a + 0.24) * h * 0.27),
                 (cx + math.cos(a - 0.24) * w * 0.27, cy + math.sin(a - 0.24) * h * 0.27)]
            d.polygon(p, fill=255)
    ry = shape_mask(w, h, rays)
    disc = shape_mask(w, h, lambda d, w, h: d.ellipse((w * 0.26, h * 0.26, w * 0.74, h * 0.74), fill=255))
    COL[sl] = mix(COL[sl], f(TERRA), ry * 0.9)
    COL[sl] = mix(COL[sl], f(OCHRE), disc * 0.92)
    H[sl] += -0.012 * ry - 0.008 * disc


def bake(Hm, strength=NSTRENGTH, blur=1.2):
    """Height (metres) to a tangent-space normal map, OpenGL green up (calm_glb's convention), wrapping in x."""
    Hb = ndi.gaussian_filter(Hm, blur, mode=("nearest", "wrap"))
    gx = (np.roll(Hb, -1, 1) - np.roll(Hb, 1, 1)) / 2
    gy = np.gradient(Hb, axis=0)
    k = P * strength
    n = np.stack([-gx * k, gy * k, np.ones_like(Hb)], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return (np.clip(n * 0.5 + 0.5, 0, 1) * 255 + 0.5).astype(np.uint8)


def main():
    paint_pave()
    paint_wall_top()
    paint_wall()
    paint_trim()
    paint_patches()
    Image.fromarray((np.clip(COL, 0, 1) * 255 + 0.5).astype(np.uint8)).save(OUT / "court_platform-atlas.png")
    calm_glb.save_height(H, OUT / "court_platform_height.png")
    Image.fromarray(bake(H)).save(OUT / "court_platform_normal.png")
    print("painted", OUT / "court_platform-atlas.png", "height %.4f .. %.4f m" % (H.min(), H.max()))


if __name__ == "__main__":
    main()
