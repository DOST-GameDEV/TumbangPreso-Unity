"""PAYO land kit, the CALM texture pass (Logs/payo/pieces/CALM_STYLE.md). System Python, numpy + Pillow + scipy.

  py -3 tools/payo_pieces/terrace_calm_paint.py [wall farwall ground plant fall foam wet rock wood far pit grass forest]

Re-paints every tiling picture of the terrace and land kit on the SAME UV layout (the same stone lists from
terrace_layout and terrace_pit_layout, the same bands and cells), the calm way: one base colour per surface and one
soft gradient, the stones close in value with faint soft joints, no cracks, pores, chips, lichen, moss creep, speckle,
blade strokes, leaf litter or pebbles. Relief goes into a height map painted beside each picture (metres), baked to a
tangent-space normal map (OpenGL, green up), the gradient taken with wrap-around where the picture tiles.

Writes ONLY to Logs/payo/pieces/calm/terrace/tex/:  <name>.png, <name>_height.png (16 bit), <name>_normal.png.
Never reads or writes the approved pictures, except py_terrace_far's ALPHA (the cut-out silhouettes of the far cards,
read only, so the cards keep their exact shape while their colour is painted new).
"""
import math
import random
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage as ndi

sys.path.insert(0, str(Path(__file__).resolve().parent))
import terrace_layout as TL       # noqa: E402
import terrace_pit_layout as PL   # noqa: E402
import calm_glb                   # noqa: E402  (save_height only)

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Logs/payo/pieces/build/terrace/tex"          # read only (the far cards' alpha)
OUT = ROOT / "Logs/payo/pieces/calm/terrace/tex"
OUT.mkdir(parents=True, exist_ok=True)
assert "build" not in OUT.parts and "pieces-approved-2026-10-12" not in OUT.parts

# the shared calm palette (pilot: stone about 165, 156, 140; moss muted; pale bevel soft)
STONE = (165, 156, 140)
PALE = (226, 216, 196)
CMOSS = (126, 144, 92)
GRASS = (112, 146, 76)
NSTRENGTH = 3.0


def f(c):
    return np.array(c, dtype=np.float32) / 255.0


def mix(a, b, t):
    if np.ndim(t) == 2:
        t = t[..., None]
    return a * (1 - t) + b * t


def soft(rng, shape, sigma, wrap=True):
    """A soft field in -1..1: big brush blotches, never noise."""
    n = rng.standard_normal(shape).astype(np.float32)
    n = ndi.gaussian_filter(n, sigma, mode="wrap" if wrap else "reflect")
    return n / (np.abs(n).max() + 1e-6)


def smooth(x):
    x = np.clip(x, 0, 1)
    return x * x * (3 - 2 * x)


def down(a, k=2):
    h, w = a.shape[:2]
    return a.reshape(h // k, k, w // k, k, *a.shape[2:]).mean(axis=(1, 3))


def bake(H, ppm, wrap=(True, True), strength=NSTRENGTH, blur=1.5):
    """Height (metres per pixel) to an RGB normal map, OpenGL green up, the same convention as calm_glb.height_to_normal
    (n = (-dh/dx, +dh/drow, 1) times ppm x strength), but the picture's edges wrap where it tiles."""
    modes = ("wrap" if wrap[0] else "nearest", "wrap" if wrap[1] else "nearest")
    if blur:
        H = ndi.gaussian_filter(H.astype(np.float32), blur, mode=modes)
    if wrap[1]:
        gx = (np.roll(H, -1, 1) - np.roll(H, 1, 1)) / 2
    else:
        gx = np.gradient(H, axis=1)
    if wrap[0]:
        gy = (np.roll(H, -1, 0) - np.roll(H, 1, 0)) / 2
    else:
        gy = np.gradient(H, axis=0)
    k = ppm * strength
    n = np.stack([-gx * k, gy * k, np.ones_like(H)], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return (np.clip(n * 0.5 + 0.5, 0, 1) * 255 + 0.5).astype(np.uint8)


def save(name, rgb, H=None, ppm=None, wrap=(True, True), alpha=None, strength=NSTRENGTH):
    a = np.clip(rgb, 0, 1)
    if alpha is not None:
        a = np.dstack([a, np.clip(alpha, 0, 1)])
    Image.fromarray((a * 255 + 0.5).astype(np.uint8)).save(OUT / (name + ".png"))
    msg = "painted %s %d x %d" % (name, a.shape[1], a.shape[0])
    if H is not None:
        calm_glb.save_height(H, OUT / (name + "_height.png"))
        Image.fromarray(bake(H, ppm, wrap, strength)).save(OUT / (name + "_normal.png"))
        msg += ", height and normal"
    print(msg)


def face_light(h, top=1.05, bottom=0.90):
    """The one soft gradient across a whole face: lighter at the top, a little darker and cooler at the foot."""
    t = np.linspace(0, 1, h, dtype=np.float32)[:, None, None]
    g = top + (bottom - top) * t
    cool = 1 + (np.array([0.95, 0.97, 1.03], np.float32) - 1) * t
    return g * cool


# ------------------------------------------------------------------------------------------------ a dressed stone face
def stone_face(Hh, W, polys, ppm, rng, r, tones=None, joint=0.92, bev=0.05, wrap_ext=0, round_px=8, big=1.6):
    """Every stone polygon gets ONE colour (the shared stone, a +-2.5 percent nudge), the joints between sit 8 percent
    darker, and a broad soft field (about `big` metres) drifts the value across groups of stones, so a wall reads as a
    few large soft blocks. Returns colour (no light gradient yet), height (metres) and the stone mask."""
    ext = W + wrap_ext
    col = np.empty((Hh, ext, 3), np.float32)
    col[:] = f(STONE) * joint
    H = np.full((Hh, ext), -0.006, np.float32)
    alp = np.zeros((Hh, ext), np.float32)
    for i, pts in enumerate(polys):
        xs, ys = [p[0] for p in pts], [p[1] for p in pts]
        m = round_px * 3
        x0, x1 = max(0, int(min(xs)) - m), min(ext, int(max(xs)) + m)
        y0, y1 = max(0, int(min(ys)) - m), min(Hh, int(max(ys)) + m)
        if x1 - x0 < 3 or y1 - y0 < 3:
            continue
        im = Image.new("L", (x1 - x0, y1 - y0), 0)
        ImageDraw.Draw(im).polygon([(x - x0, y - y0) for x, y in pts], fill=255)
        mask = np.asarray(im, np.float32) / 255
        mask = np.clip((ndi.gaussian_filter(mask, round_px) - 0.5) * 5 + 0.5, 0, 1)     # rounded corners
        inside = mask > 0.5
        d = ndi.distance_transform_edt(inside).astype(np.float32)
        tone = f(STONE) * r.uniform(0.975, 1.025)
        if tones is not None and tones[i] is not None:
            tone = tones[i]
        c = np.empty(mask.shape + (3,), np.float32)
        c[:] = tone
        bw = bev * ppm
        band = smooth(1 - d / max(1.0, bw)) * inside                                   # the bevel band, just inside the edge
        c = mix(c, f(PALE), band * 0.10)                                                 # a subtle lighter edge, not a stripe
        hs = 0.010 * smooth(d / max(1.0, bw * 1.6))                                      # a soft rounded pillow
        sl = (slice(y0, y1), slice(x0, x1))
        col[sl] = mix(col[sl], c, mask)
        H[sl] = np.where(mask > 0.02, np.maximum(H[sl], -0.006 + (hs + 0.006) * mask), H[sl])
        alp[sl] = np.maximum(alp[sl], mask)
    if wrap_ext:                                                                         # stones that run past the tile edge come in at the left
        e = wrap_ext
        col[:, :e] = mix(col[:, :e], col[:, W:], alp[:, W:])
        H[:, :e] = np.maximum(H[:, :e], H[:, W:])
        alp[:, :e] = np.maximum(alp[:, :e], alp[:, W:])
        col, H, alp = col[:, :W], H[:, :W], alp[:, :W]
    groups = soft(rng, (Hh, W), (big * ppm * 0.6, big * ppm), True)
    col *= (1 + 0.04 * groups)[..., None]
    H += 0.003 * soft(rng, (Hh, W), 0.25 * ppm, True)                                    # big soft planes in the stone, never pores
    return col, H, alp


def moss_patches(shape, r, centres, ppm):
    """A few large soft moss shapes, each one painted shape with a soft edge. centres: (x px, y px, half-width m, half-height m)."""
    Hh, W = shape
    m = Image.new("L", (W, Hh), 0)
    d = ImageDraw.Draw(m)
    for x, y, sw, sh in centres:
        for ox in (-W, 0, W):
            d.ellipse([x + ox - sw * ppm, y - sh * ppm, x + ox + sw * ppm, y + sh * ppm], fill=255)
    return np.asarray(m.filter(ImageFilter.GaussianBlur(max(2.0, ppm * 0.05))), np.float32) / 255


# ------------------------------------------------------------------------------------------------ the terrace wall
def wall_polys(W, Hh, ppm):
    polys = []
    for st in TL.stones():
        polys.append([(s * ppm, Hh - t / TL.TEX_V * Hh) for s, t in TL.corners(st)])
    return polys


def paint_wall(W=2048, Hh=256, joint=0.92, darker=1.0, cap_green=0.0, name="py_terrace_wall"):
    ppm = W / TL.L
    rng = np.random.default_rng(111)
    r = random.Random(111)
    col, H, alp = stone_face(Hh, W, wall_polys(W, Hh, ppm), ppm, rng, r, joint=joint, bev=0.05, wrap_ext=int(W * 1.2 / 12), round_px=max(3, int(ppm * 0.04)))
    capy = Hh - TL.T / TL.TEX_V * Hh                                   # rows above this are the cap stones' tops (they face the sky)
    rows = np.arange(Hh, dtype=np.float32)[:, None]
    face_rows = np.clip(rows / max(1.0, Hh - 1), 0, 1)
    # the face gradient runs over the leaning face only (cap row to foot); the cap tops are paler
    t = np.clip((rows - capy) / (Hh - capy), 0, 1)
    g = 1.05 + (0.90 - 1.05) * t
    cool = 1 + (np.array([0.95, 0.97, 1.03], np.float32) - 1) * t[..., None]
    col = col * g[..., None] * cool
    top = np.clip((capy - rows) / 4.0, 0, 1)
    col = mix(col, col * 1.06 + 0.01, top * 0.9)
    # a few big soft moss shapes along the cap's top edge, nothing else
    cen = []
    x = r.uniform(0, 2.0) * ppm
    while x < W:
        cen.append((x, capy + 0.04 * ppm, r.uniform(0.5, 0.9), r.uniform(0.14, 0.22)))
        x += r.uniform(2.6, 4.2) * ppm
    mo = moss_patches((Hh, W), r, cen, ppm) * alp
    col = mix(col, f(CMOSS), mo * 0.65)
    H += 0.006 * mo
    if cap_green:                                                       # the far strip: the cap is under its weeds card
        col = mix(col, f((104, 138, 72)), np.clip((capy + 0.06 * ppm - rows) / (0.06 * ppm), 0, 1) * cap_green)
    col *= darker
    return col, H, ppm


def wall():
    col, H, ppm = paint_wall()
    save("py_terrace_wall", col, H, ppm, wrap=(False, True))


def farwall():
    """The far strip: the calm wall seen flat, at half size: a few large soft blocks, joints fainter still, a touch
    darker because the flat strip gets no shade of its own, its cap green where the weeds hang over."""
    col, H, ppm = paint_wall(joint=0.95, darker=0.94, cap_green=0.5)
    save("py_terrace_far_wall", down(col, 2), down(H, 2), ppm / 2, wrap=(False, True))


# ------------------------------------------------------------------------------------------------ dike and paddy bed
def ground():
    W, Hh = 1024, 512
    ppm = W / 4.0
    rng = np.random.default_rng(105)
    half = Hh // 2
    a = np.empty((Hh, W, 3), np.float32)
    # GRASS, top half: the dike unfolded, the wall side at the top row. Crest lightest, a soft gradient to either side
    rows = np.linspace(0, 1, half, dtype=np.float32)[:, None]
    crest = np.exp(-((rows - 0.42) / 0.30) ** 2)
    g = mix(np.broadcast_to(f((92, 126, 68)), (half, W, 3)), np.broadcast_to(f((132, 162, 86)), (half, W, 3)), crest)
    g *= (1 + 0.04 * soft(rng, (half, W), (0.3 * ppm, 1.0 * ppm), True))[..., None]
    foot = smooth((rows - 0.78) / 0.22) * np.ones((1, W), np.float32)
    g = mix(g, f((124, 110, 82)), foot * 0.55)                          # the damp foot by the water: one soft band
    a[:half] = g
    # MUD, bottom half: the paddy bed under the water, one soft brown
    mud = np.empty((Hh - half, W, 3), np.float32)
    mud[:] = f((120, 104, 82))
    mud *= (1 + 0.05 * soft(rng, (Hh - half, W), (0.4 * ppm, 0.8 * ppm), True))[..., None]
    a[half:] = mud
    a[half - 3:half + 3] = mix(a[half - 3:half + 3], a[half + 4:half + 5], 0.5)
    H = np.zeros((Hh, W), np.float32)
    H[:half] = 0.006 * soft(rng, (half, W), (0.06 * ppm, 0.10 * ppm), True) + 0.004 * crest   # soft grass clumps, broad
    H[half:] = 0.002 * soft(rng, (Hh - half, W), 0.2 * ppm, True)
    save("py_terrace_ground", a, H, ppm, wrap=(False, True))


# ------------------------------------------------------------------------------------------------ blades and leaves
def plant():
    cw, Hh = 64, 256
    cells = [((96, 140, 64), (148, 180, 86), "rib"),        # 0 young rice
             ((150, 146, 76), (204, 182, 98), "rib"),       # 1 ripe leaf
             ((200, 162, 80), (226, 194, 108), "grain"),    # 2 grain head
             ((90, 132, 68), (136, 168, 86), "rib"),        # 3 grass
             ((72, 120, 74), (118, 160, 94), "fern"),       # 4 fern
             ((74, 126, 92), (124, 168, 112), "taro"),      # 5 taro
             ((178, 70, 56), (210, 108, 78), "petal"),      # 6 red flower
             ((160, 132, 82), (204, 180, 122), "rib")]      # 7 dry stem
    a = np.zeros((Hh, cw * 8, 3), np.float32)
    H = np.zeros((Hh, cw * 8), np.float32)
    rows = np.linspace(1, 0, Hh, dtype=np.float32)[:, None]            # 0 at the base (bottom row), 1 at the tip
    xs = np.linspace(-1, 1, cw, dtype=np.float32)[None, :]
    for i, (c0, c1, kind) in enumerate(cells):
        t = smooth(rows ** 0.9) + 0 * xs
        c = mix(np.broadcast_to(f(c0), (Hh, cw, 3)).copy(), np.broadcast_to(f(c1), (Hh, cw, 3)), t)
        c *= (1 - 0.08 * np.abs(xs) ** 2)[..., None]                    # a little rounder at the edges
        if kind in ("rib", "fern", "taro"):
            c = mix(c, c * 1.07 + 0.01, np.clip(1 - np.abs(xs) / 0.22, 0, 1) * 0.5)    # a soft pale midrib
        if kind == "petal":
            c = mix(c, f((226, 190, 96)), smooth((0.22 - rows) / 0.22) * np.clip(1 - np.abs(xs) / 0.6, 0, 1) * 0.8)
        a[:, i * cw:(i + 1) * cw] = c
        dome = 0.004 * (1 - xs ** 2) + 0 * rows                          # the blade is round across, folded at the rib
        if kind in ("rib", "fern", "taro"):
            dome = dome - 0.0015 * np.clip(1 - np.abs(xs) / 0.2, 0, 1)
        H[:, i * cw:(i + 1) * cw] = dome
    save("py_terrace_plant", a, H, 700.0, wrap=(False, False), strength=2.0)


# ------------------------------------------------------------------------------------------------ falling water
def fall():
    """Falling water, tiling down the picture: a pale aqua body, a handful of BROAD soft strands lighter and a few
    soft shadows between, nothing thin. Alpha stays whole (the sheet's shape is its model)."""
    W, Hh = 256, 1024
    rng = np.random.default_rng(109)
    r = random.Random(109)
    base = np.empty((Hh, W, 3), np.float32)
    base[:] = f((196, 232, 240))
    band = soft(rng, (Hh, W), (260, 26), True)                          # broad bands running down
    base = mix(base, f((150, 204, 222)), np.clip(band * 1.4, 0, 1) * 0.5)
    base = mix(base, f((226, 244, 246)), np.clip(-band * 1.4, 0, 1) * 0.65)
    m = Image.new("L", (W, Hh), 0)
    d = ImageDraw.Draw(m)
    for _ in range(9):                                                  # broad soft strands: wide lenses, long
        x, y, ln, w = r.uniform(0, W), r.uniform(0, Hh), r.uniform(500, 900), r.uniform(26, 44)
        for ox in (-W, 0, W):
            for oy in (-Hh, 0, Hh):
                d.polygon([(x + ox, y + oy), (x + ox + w / 2, y + oy + ln * 0.35), (x + ox, y + oy + ln), (x + ox - w / 2, y + oy + ln * 0.35)], fill=255)
    st = ndi.gaussian_filter(np.asarray(m, np.float32) / 255, (10, 6), mode="wrap")
    base = mix(base, f((248, 253, 253)), st * 0.85)
    save("py_fall", base, alpha=np.ones((Hh, W), np.float32))


def foam():
    W = 128
    rng = np.random.default_rng(151)
    a = np.empty((W, W, 3), np.float32)
    a[:] = f((240, 248, 248))
    s = soft(rng, (W, W), 18, True)
    a = mix(a, f((206, 234, 238)), np.clip(s * 1.3, 0, 1) * 0.5)
    H = 0.01 * soft(rng, (W, W), 14, True)
    save("py_foam", a, H, 128.0, strength=1.5)


def wet():
    """The stain decal: one soft dark shape, no threads, no glints."""
    W, Hh = 256, 512
    rng = np.random.default_rng(141)
    xs = np.linspace(-1, 1, W, dtype=np.float32)[None, :]
    ys = np.linspace(0, 1, Hh, dtype=np.float32)[:, None]
    ragged = 0.08 * soft(rng, (Hh, W), (60, 20))
    core = smooth((0.86 - np.abs(xs) * (1.0 + 0.2 * ys) + ragged) * 2.5)
    al = core * smooth((1.0 - ys) * 5) * smooth(ys * 20)
    a = np.zeros((Hh, W, 3), np.float32)
    a[:] = f((78, 82, 74))
    save("py_wet", a, alpha=al * 0.55)


# ------------------------------------------------------------------------------------------------ rock
def rock():
    """Calm warm grey with big soft planes: about a dozen large facets a tile (2.5 m), each a gently tilted plane in
    the height map and a hair lighter or darker in colour, their edges softened. Vertex colour does the rest."""
    W = 512
    ppm = W / 2.5
    rng = np.random.default_rng(121)
    r = random.Random(121)
    n = 14
    pts = np.array([(r.uniform(0, W), r.uniform(0, W)) for _ in range(n)], np.float32)
    yy, xx = np.mgrid[0:W, 0:W].astype(np.float32)
    best = np.full((W, W), 1e9, np.float32)
    second = np.full((W, W), 1e9, np.float32)
    idx = np.zeros((W, W), np.int32)
    for i, (px, py) in enumerate(pts):
        dx = np.abs(xx - px); dx = np.minimum(dx, W - dx)
        dy = np.abs(yy - py); dy = np.minimum(dy, W - dy)
        dd = np.sqrt(dx * dx + dy * dy)
        closer = dd < best
        second = np.where(closer, best, np.minimum(second, dd))
        idx = np.where(closer, i, idx)
        best = np.minimum(best, dd)
    tone = np.array([r.uniform(0.965, 1.035) for _ in range(n)], np.float32)
    tilt = np.array([(r.uniform(-0.10, 0.10), r.uniform(-0.10, 0.10)) for _ in range(n)], np.float32)
    a = np.empty((W, W, 3), np.float32)
    a[:] = f((172, 165, 152))
    k = tone[idx]
    k = ndi.gaussian_filter(k, 14, mode="wrap")
    a *= k[..., None]
    a *= (1 + 0.03 * soft(rng, (W, W), 60, True))[..., None]
    # each facet a tilted plane, measured from its own seed point (wrapped), ridged softly where facets meet
    dx = xx - pts[idx, 0]; dx = (dx + W / 2) % W - W / 2
    dy = yy - pts[idx, 1]; dy = (dy + W / 2) % W - W / 2
    H = (tilt[idx, 0] * dx + tilt[idx, 1] * dy) / ppm
    H = H - H.mean()
    H = ndi.gaussian_filter(H, 0.07 * ppm, mode="wrap")           # the facets meet in soft creases, never drawn lines
    save("py_rock", a, H.astype(np.float32), ppm, strength=2.2)


# ------------------------------------------------------------------------------------------------ bamboo, timber, straw, cloth
def wood():
    W = 512
    q = W // 4
    rng = np.random.default_rng(131)
    a = np.zeros((W, W, 3), np.float32)
    H = np.zeros((W, W), np.float32)
    ppm = W / 1.6
    rows = np.linspace(0, 1, q, dtype=np.float32)[:, None]
    xs = np.arange(W, dtype=np.float32)[None, :]
    # A bamboo: one green-gold, round across the band, a soft node ring every quarter
    b = np.empty((q, W, 3), np.float32)
    b[:] = f((190, 178, 104))
    roundness = np.clip(np.sin(rows * math.pi), 0, 1) ** 0.8
    b *= (0.86 + 0.20 * roundness)[..., None]
    node = smooth(1 - np.abs((xs % (W / 4)) - W / 8) / 6.0)
    b = mix(b, f((150, 130, 76)), node * 0.55 * np.ones((q, 1), np.float32))
    a[0:q] = b
    H[0:q] = 0.006 * roundness + 0.002 * node
    # B timber: one warm brown, three or four broad grain bands
    t = np.empty((q, W, 3), np.float32)
    t[:] = f((128, 94, 68))
    gr = soft(rng, (q, W), (6, 140), True)
    t *= (1 + 0.06 * gr)[..., None]
    a[q:2 * q] = t
    H[q:2 * q] = 0.0015 * gr
    # C straw: one golden straw, broad strand groups running down the band, darker at the underside
    s = np.empty((q, W, 3), np.float32)
    s[:] = f((206, 172, 100))
    bundles = soft(rng, (q, W), (60, 7), True)
    s *= (1 + 0.05 * bundles)[..., None]
    s *= (1.03 - 0.10 * rows)[..., None]
    a[2 * q:3 * q] = s
    H[2 * q:3 * q] = 0.002 * bundles
    # D cloth: the woven red, black and cream band, bold stripes only (left three quarters); the sack face (right quarter)
    im = Image.new("RGB", (W, q), (176, 74, 60))
    dr = ImageDraw.Draw(im)
    for y0, hgt, c in ((8, 10, (62, 54, 58)), (24, 6, (226, 214, 188)), (40, 14, (62, 54, 58)), (66, 8, (206, 160, 84)),
                       (84, 16, (62, 54, 58)), (106, 6, (226, 214, 188)), (116, 10, (84, 148, 138))):
        dr.rectangle((0, y0, W, y0 + hgt), fill=c)
    fx = W * 3 // 4
    dr.rectangle((fx, 0, W, q), fill=(214, 200, 164))
    cx, cy = fx + q // 2, q // 2
    ink = (70, 60, 62)
    for sx in (-1, 1):                                                  # the stitched cross eyes, bold
        ex = cx + sx * 24
        dr.line((ex - 9, cy - 24, ex + 9, cy - 6), fill=ink, width=6)
        dr.line((ex - 9, cy - 6, ex + 9, cy - 24), fill=ink, width=6)
    dr.arc((cx - 30, cy - 14, cx + 30, cy + 30), 20, 160, fill=ink, width=6)
    dr.ellipse((cx - 48, cy - 2, cx - 30, cy + 14), fill=(214, 150, 128))
    dr.ellipse((cx + 30, cy - 2, cx + 48, cy + 14), fill=(214, 150, 128))
    c = np.asarray(im.filter(ImageFilter.GaussianBlur(0.8)), np.float32) / 255
    a[3 * q:] = c
    lum = c.mean(-1)
    H[3 * q:] = 0.001 * ndi.gaussian_filter(lum, 1.5)
    save("py_terrace_wood", a, H, ppm, wrap=(False, True), strength=2.0)


# ------------------------------------------------------------------------------------------------ the far cards
def far():
    """The far terrace's cards keep their exact cut-out (alpha read from the approved picture), and every band is
    painted as ONE soft colour field: rice is a broad band of colour, darker at its foot, not blades."""
    src = np.asarray(Image.open(SRC / "py_terrace_far.png").convert("RGBA"), np.float32) / 255
    al = src[..., 3]
    Hh, W = al.shape
    b = Hh // 8                                                         # 256 rows a band
    q = b // 4
    rng = np.random.default_rng(161)
    a = np.zeros((Hh, W, 3), np.float32)
    TONE = 0.96                                                         # the cards get no shadow of their own

    def band(y0, y1, c_foot, c_top, var=0.04, foot_at_bottom=True):
        hh = y1 - y0
        t = np.linspace(0, 1, hh, dtype=np.float32)[:, None]
        if foot_at_bottom:
            t = 1 - t
        t = smooth(t ** 0.8)
        c = mix(np.broadcast_to(f(c_foot), (hh, W, 3)).copy(), np.broadcast_to(f(c_top), (hh, W, 3)), t * np.ones((1, W), np.float32))
        c *= (1 + var * soft(rng, (hh, W), (hh * 0.3 + 2, 0.8 * 256), True))[..., None]
        a[y0:y1] = c * TONE

    band(0, b, (104, 146, 68), (156, 188, 92))                           # young rice from the side
    band(b, 2 * b, (164, 148, 76), (212, 186, 100))                     # ripe rice from the side
    band(2 * b, 4 * b, (112, 152, 72), (150, 184, 90), foot_at_bottom=False)   # young from above
    band(4 * b, 6 * b, (184, 162, 84), (210, 184, 100), foot_at_bottom=False)  # ripe from above
    band(6 * b, 6 * b + 2 * q, (92, 128, 66), (110, 144, 74), foot_at_bottom=False)   # weeds hanging over the cap
    band(6 * b + 2 * q, 6 * b + 3 * q, (118, 156, 74), (128, 164, 80), var=0.03)       # the young canopy
    band(6 * b + 3 * q, 7 * b, (198, 172, 92), (208, 182, 98), var=0.03)               # the ripe canopy
    band(7 * b, 7 * b + 2 * q, (168, 204, 216), (184, 214, 222), var=0.03)             # flooded: a strip of sky
    band(7 * b + 2 * q, 8 * b, (100, 138, 70), (132, 166, 84))                         # the dike's grass
    # the few red flowers of the dike grass: one soft red where the old picture had them is not worth the speckle; gone
    save("py_terrace_far", a, alpha=al)


# ------------------------------------------------------------------------------------------------ the pit wall
def pit_carve(col, H, x0, y0, x1, y1, k, ppm):
    """One block of the carved band: a BOLD stepped fret (even) or a bold diamond with stepped wings (odd), thick
    shapes cut in relief, a soft ochre or terracotta left in the cut. No frame, no fine lines."""
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
    cm = np.asarray(cut.filter(ImageFilter.GaussianBlur(max(1.5, u * 0.18))), np.float32) / 255
    paint = f((178, 108, 80)) if k % 2 else f((196, 160, 96))
    sl = (slice(y0, y1), slice(x0, x1))
    col[sl] = mix(col[sl], mix(col[sl], paint[None, None, :] * 0.92, 0.75), cm)
    H[sl] = H[sl] - 0.014 * cm


def pit():
    W = 2048
    ppm = W / PL.W
    rng = np.random.default_rng(171)
    r = random.Random(171)
    sts = PL.stones()
    polys = [[(s * ppm, W - t / PL.Hh * W) for s, t in PL.corners(st)] for st in sts]
    col, H, alp = stone_face(W, W, polys, ppm, rng, r, joint=0.92, bev=0.07, round_px=int(ppm * 0.05), big=2.5)
    for k, st in enumerate([s for s in sts if s["kind"] == "band"]):
        c = PL.corners(st)
        x0, x1 = int(c[0][0] * ppm), int(c[1][0] * ppm)
        y0, y1 = int(W - c[2][1] / PL.Hh * W), int(W - c[0][1] / PL.Hh * W)
        pit_carve(col, H, x0, y0, x1, y1, k, ppm)
    col *= face_light(W, 1.03, 0.95)                                     # gentle: the modules stack every 12 m
    # a few big soft moss shapes on top edges: under the ledge and at the wall's top, nothing elsewhere
    cen = []
    for tt in (PL.LEDGE[0] - 0.05, PL.Hh - 0.02):
        x = r.uniform(0.5, 3.0) * ppm
        while x < W:
            cen.append((x, W - tt / PL.Hh * W + 0.12 * ppm, r.uniform(0.6, 1.1), r.uniform(0.22, 0.34)))
            x += r.uniform(3.0, 5.0) * ppm
    mo = moss_patches((W, W), r, cen, ppm) * np.clip(alp * 1.2, 0, 1)
    col = mix(col, f(CMOSS), mo * 0.7)
    H += 0.005 * mo
    save("py_pit_wall", col, H, ppm)


# ------------------------------------------------------------------------------------------------ open ground
def grass():
    W = 1024
    ppm = W / 4.0
    rng = np.random.default_rng(181)
    a = np.empty((W, W, 3), np.float32)
    a[:] = f(GRASS)
    big = soft(rng, (W, W), 1.0 * ppm, True)
    a = mix(a, f((132, 162, 84)), np.clip(-big * 1.5, 0, 1) * 0.75)      # broad sunlit patches, a little yellower
    a = mix(a, f((94, 130, 72)), np.clip(big * 1.5, 0, 1) * 0.75)        # broad cooler hollows
    a *= (1 + 0.025 * soft(rng, (W, W), 0.35 * ppm, True))[..., None]
    H = 0.012 * soft(rng, (W, W), 0.45 * ppm, True) + 0.004 * soft(rng, (W, W), 0.12 * ppm, True)
    save("py_ground_grass", a, H, ppm)


def forest():
    W = 1024
    ppm = W / 4.0
    rng = np.random.default_rng(191)
    a = np.empty((W, W, 3), np.float32)
    a[:] = f((100, 84, 62))
    b = soft(rng, (W, W), 0.8 * ppm, True)
    a = mix(a, f((82, 70, 56)), np.clip(b * 1.5, 0, 1) * 0.6)            # damp darker hollows
    a = mix(a, f((124, 102, 72)), np.clip(-b * 1.5, 0, 1) * 0.45)        # broad drier patches, warm litter tone
    mo = smooth(soft(rng, (W, W), 0.55 * ppm, True) * 2.6 - 0.7)
    a = mix(a, f((98, 118, 70)), mo * 0.7)                               # moss as a few large soft shapes
    H = 0.012 * soft(rng, (W, W), 0.5 * ppm, True) + 0.004 * mo
    save("py_ground_forest", a, H, ppm)


JOBS = dict(wall=wall, farwall=farwall, ground=ground, plant=plant, fall=fall, foam=foam, wet=wet, rock=rock, wood=wood, far=far,
            pit=pit, grass=grass, forest=forest)
if __name__ == "__main__":
    for name in (sys.argv[1:] or list(JOBS)):
        JOBS[name]()
