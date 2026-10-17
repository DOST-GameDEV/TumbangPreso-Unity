"""The CALM land textures for Payo (Logs/payo/pieces/CALM_STYLE.md): tileable value and height maps for every land surface.

  python tools/payo_land_tex.py [--land=v55tex]      (system Python with numpy, scipy and Pillow)

Writes Logs/payo/terrain/tex_calm/<surface>_value.png, <surface>_height.png (16 bit), <surface>_normal.png and a
contact sheet. tools/payo_land_blend.py projects them from object coordinates (box projection), one tile per TILE[s]
metres, so the tile sizes below are the physical sizes the shapes were painted for.

VALUE maps are multiplied over the land's own vertex colour, which stays the base colour (its hues are tuned). They are
stored at half scale: grey 128 means 1.0, so a mean of 128 keeps the vertex colour exactly. Every surface stays inside
about plus or minus 10 percent, in big soft shapes only (the 40 m rule: nothing that turns into speckle).
HEIGHT maps hold the relief (0 = lowest, 65535 = highest; the depth in metres is DEPTH[s]); normals are baked from them
with the calm pieces' baker (tools/payo_pieces/calm_glb.py, OpenGL green up) for the game. In Blender the land has no
UVs, so the height drives a Bump node instead.
Everything here is periodic (shaped noise made in the frequency domain, cells measured with wrap-around), so a tile
meets its neighbour with no seam.
"""
import os
import sys

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "tools", "payo_pieces"))
from calm_glb import height_to_normal  # noqa: E402
import terrace_calm_paint as K         # noqa: E402  (the land kit's calm painter: stone_face, moss_patches, palette)
import terrace_layout as TL            # noqa: E402  (the kit's stone palette)

OUT = os.path.join(ROOT, "Logs", "payo", "terrain", "tex_calm")
N = 1024                     # pixels per tile, every surface
# metres per tile (one image), relief depth in metres; payo_land_blend.py keeps its own copy of these numbers
TILE = {"grass": 20.0, "paddy": 12.0, "flooded": 14.0, "rock": 16.0, "paving": 10.0,
        "water": 24.0, "foam": 10.0, "macro": 96.0}
DEPTH = {"grass": 0.12, "paddy": 0.02, "flooded": 0.025, "rock": 0.22, "paving": 0.05,
         "water": 0.10, "foam": 0.10, "macro": 0.0}


def soft_noise(seed, metres, tile, aniso=(1.0, 1.0)):
    """Periodic soft noise, unit spread: features about `metres` wide, aniso times longer along (x, y)."""
    rng = np.random.default_rng(seed)
    f = np.fft.fftfreq(N) * N / tile                                        # cycles per metre
    fx, fy = np.meshgrid(f * aniso[0], f * aniso[1])
    r = np.hypot(fx, fy) * metres
    amp = np.exp(-r * r * 2.0)
    amp[0, 0] = 0
    w = np.fft.ifft2(np.fft.fft2(rng.standard_normal((N, N))) * amp).real
    return (w / (w.std() + 1e-9)).astype(np.float32)


def blur_wrap(a, metres, tile):
    f = np.fft.fftfreq(N) * N / tile
    fx, fy = np.meshgrid(f, f)
    return np.fft.ifft2(np.fft.fft2(a) * np.exp(-2 * (np.pi * metres) ** 2 * (fx * fx + fy * fy))).real.astype(np.float32)


def grid(tile):
    t = (np.arange(N) + 0.5) / N * tile
    return np.meshgrid(t, t)                                            # X across, Y down the image, in metres


def cells(seed, count, tile, stretch=(1.0, 1.0)):
    """Periodic Voronoi: for every pixel the nearest and second nearest site distance and the nearest site's index."""
    rng = np.random.default_rng(seed)
    pts = rng.uniform(0, tile, (count, 2))
    X, Y = grid(tile)
    d1 = np.full((N, N), 1e9, np.float32); d2 = d1.copy(); idx = np.zeros((N, N), np.int32)
    for k, (px, py) in enumerate(pts):
        dx = (X - px + tile / 2) % tile - tile / 2
        dy = (Y - py + tile / 2) % tile - tile / 2
        d = np.hypot(dx * stretch[0], dy * stretch[1])
        closer = d < d1
        d2 = np.where(closer, d1, np.minimum(d2, d))
        idx = np.where(closer, k, idx)
        d1 = np.where(closer, d, d1)
    return d1, d2, idx, pts, rng


def norm(a):
    a = a - a.min()
    return a / (a.max() + 1e-9)


def value_from(v, spread):
    """A shape to a value map: mean exactly 1, range plus or minus `spread`."""
    v = v - v.mean()
    v = v / (np.abs(v).max() + 1e-9) * spread
    return 1.0 + v


# ------------------------------------------------------------------ the surfaces
def smoothstep(x):
    x = np.clip(x, 0, 1)
    return (x * x * (3 - 2 * x)).astype(np.float32)


def blotch(seed, metres, t, cover, ragged=0.22):
    """One layer of painted blotches (Lagoon Cove's ground): big shapes of one flat tone with a crisp but soft edge,
    the edge a little ragged like a dry brush. `cover` of the tile is inside a blotch."""
    n = soft_noise(seed, metres, t) + ragged * soft_noise(seed + 500, metres / 5.0, t)
    thr = np.quantile(n, 1.0 - cover)
    return smoothstep((n - thr) / 0.16 + 0.5)


def tint(layers):
    """Colour multiplier from (mask, (r, g, b) multiplier) layers painted one over another, mean exactly 1 per channel."""
    v = np.ones((N, N, 3), np.float32)
    for m, c in layers:
        v = v * (1 - m[..., None]) + v * np.array(c, np.float32) * m[..., None]
    return v / v.reshape(-1, 3).mean(0)


def grass():
    """Slopes: painted grass in big soft colour patches, as the land kit's calm ground_grass_tile does them (broad
    sunlit patches a little yellower, broad cooler hollows), laid as Lagoon Cove lays its ground: flat blotches of one
    tone, overlapping, with a soft crisp edge. 20 m a tile, the patches 4 to 8 m."""
    t = TILE["grass"]
    sun = blotch(1, 5.0, t, 0.34)
    hollow = blotch(2, 4.0, t, 0.30) * (1 - sun)
    v = tint([(sun, (1.08, 1.07, 0.94)), (hollow, (0.91, 0.94, 0.99))])
    h = norm(blur_wrap(sun - hollow, 0.35, t))
    return v, h


WALL_L, WALL_H, WALL_PPM = 12.0, 5.0, 128          # one wall picture: 12 m along, one 5 m terrace rise up
SHADE = [None]                                     # bank_wall's grass shade (1 = lit), saved as bank_shade.png


def bank_wall():
    """The terrace walls: the land kit's grey dry-stone courses (terrace_layout's palette and stone rules, painted with
    terrace_calm_paint.stone_face), scaled for this land's 5 m walls: bigger stones (about 1.1 m courses) in two double bands
    (a tall stone now and then fills a band), a row of paler cap stones, a grass lip hanging over the top in a few big
    soft tufts, and a few large soft moss shapes on the caps. One picture is exactly one terrace rise, so the caps land
    on every terrace's top edge (payo_land.py cuts the terraces every STEP = 5 m). Returns sRGB colour, the stone
    coverage (0 = the grass lip, which keeps the land's own grass colour) and the height in metres."""
    import random
    W, Hh, ppm = int(WALL_L * WALL_PPM), int(WALL_H * WALL_PPM), WALL_PPM
    r = random.Random(7)
    rng = np.random.default_rng(7)
    polys, tones = [], []
    JOINT = 0.07

    def add(s0, s1, t0, t1, cap=False):
        g = JOINT / 2
        j = lambda: r.uniform(-0.05, 0.05)
        pts = [(s0 + g + j(), t0 + g + j()), (s1 - g + j(), t0 + g + j()), (s1 - g + j(), t1 - g + (0 if cap else j())),
               (s0 + g + j(), t1 - g + (0 if cap else j()))]
        polys.append([(a * ppm, Hh - b * ppm) for a, b in pts])
        base = r.choice(TL.WARM) if r.random() < 0.05 else r.choice(TL.STONES)
        c = K.mix(K.f(K.STONE), K.f(base), 0.45) * r.uniform(0.95, 1.03)
        tones.append(c * (1.05 if cap else 1.0))

    def course(t0, t1, a, b, lo, hi, cap=False):
        x = a
        while x < b - 1e-6:
            w = r.uniform(lo, hi)
            if b - (x + w) < lo * 0.8:
                w = b - x
            add(x, x + w, t0, t1, cap)
            x += w

    t = 0.0
    for band in (2.2, 2.1):                             # two bands of two courses, about 1.1 m a course
        x = r.uniform(0.0, 0.5)
        end = x + WALL_L
        while x < end - 1e-6:
            if r.random() < 0.25:                         # one tall stone through the band
                w = min(r.uniform(1.2, 1.7), end - x)
                if end - (x + w) < 1.0:
                    w = end - x
                add(x, x + w, t, t + band)
                x += w
            else:
                w = r.uniform(2.4, 4.4)
                if end - (x + w) < 1.4:
                    w = end - x
                split = band / 2 + r.uniform(-0.15, 0.15)
                course(t, t + split, x, x + w, 1.2, 2.3)
                course(t + split, t + band, x, x + w, 1.0, 2.0)
                x += w
        t += band
    a = r.uniform(0.2, 0.6)
    course(t, 4.92, a, a + WALL_L, 1.2, 2.0, cap=True)   # the cap stones, flatter and paler
    col, H, alp = K.stone_face(Hh, W, polys, ppm, rng, r, tones=tones, joint=0.88, bev=0.09, wrap_ext=int(3.0 * ppm),
                               round_px=int(0.10 * ppm), big=3.0)
    H = H * 4.0                                          # the kit's pillows were cut for 0.4 m stones; these are 1.1 m
    rows = np.arange(Hh, dtype=np.float32)[:, None]
    up = 1 - rows / (Hh - 1)                              # 0 at the foot, 1 at the top
    col = col * (0.88 + 0.17 * up)[..., None] * (1 + (np.array([0.95, 0.97, 1.03], np.float32) - 1) * (1 - up)[..., None])
    # a few large soft moss shapes along the caps, the land kit's muted moss
    capy = Hh - 4.6 * ppm
    cen, x = [], r.uniform(0, 2.0) * ppm
    while x < W:
        cen.append((x, capy, r.uniform(0.8, 1.5), r.uniform(0.18, 0.30)))
        x += r.uniform(3.0, 5.0) * ppm
    mo = K.moss_patches((Hh, W), r, cen, ppm)
    col = K.mix(col, K.f(K.CMOSS), mo * 0.55)
    # the grass lip: the paddy's dike grass hanging over the caps in a few small soft tongues (owner, d46: the big
    # hanging blobs read as flat cut-outs; softer edges, darker at the lower edge, smaller, fewer)
    xs = np.arange(W, dtype=np.float32)
    hang = np.zeros(W, np.float32)
    x = r.uniform(0, 2.0)
    while x < WALL_L + 2:
        c, wd, dp = x * ppm, r.uniform(0.35, 0.8) * ppm, r.uniform(0.10, 0.45) * ppm
        for o in (-W, 0, W):
            hang = np.maximum(hang, dp * np.clip(1 - ((xs - c - o) / wd) ** 2, 0, 1) ** 0.8)
        x += r.uniform(2.6, 4.8)
    ragged = 0.06 * ppm * K.soft(rng, (Hh, W), (0.10 * ppm, 0.10 * ppm), True)       # a dry-brush edge, not a cut line
    edge = (Hh - 4.92 * ppm) + hang[None, :] + ragged      # rows above this are grass
    lip = smoothstep((edge - rows) / (0.10 * ppm) + 0.5)    # about 10 cm of soft edge
    top = lip.copy()
    # grass creeping up the foot in a few low soft mounds, and nothing in the middle of the face
    foot = np.zeros(W, np.float32)
    x = r.uniform(0, 3.0)
    while x < WALL_L + 2:
        c, wd, dp = x * ppm, r.uniform(0.6, 1.4) * ppm, r.uniform(0.15, 0.45) * ppm
        for o in (-W, 0, W):
            foot = np.maximum(foot, dp * np.clip(1 - ((xs - c - o) / wd) ** 2, 0, 1) ** 0.7)
        x += r.uniform(3.0, 6.0)
    fedge = (Hh - 1) - foot[None, :] - ragged
    lip = np.maximum(lip, smoothstep((rows - fedge) / (0.10 * ppm) + 0.5))
    cover = 1 - lip
    # the overhang's shade: the grass darker toward its lower edge, and a soft shadow cast on the stone under it
    below = np.clip((rows - (Hh - 4.92 * ppm)) / np.maximum(hang[None, :] + ragged, 1.0), 0, 1)   # 0 at the lip, 1 at a tongue's tip
    shade = 1.0 - 0.28 * smoothstep((below - 0.35) / 0.65) * top
    cast = np.clip((rows - edge) / (0.18 * ppm), 0, 1)
    cast = (1 - cast) * (rows > edge) * smoothstep(hang[None, :] / (0.15 * ppm))
    col = col * (1 - 0.22 * cast)[..., None]
    H = H * (1 - lip) + lip * (H.max() + 0.01)
    SHADE[0] = shade.astype(np.float32)
    return np.clip(col, 0, 1), cover.astype(np.float32), H, ppm


def paddy():
    """Rice: soft broad rows along x, a gentle wobble, a soft sheen variation across the field."""
    t = TILE["paddy"]
    X, Y = grid(t)
    wob = soft_noise(21, 3.0, t)
    rows = np.cos(2 * np.pi * (Y + 0.6 * wob) / (t / 6))                   # 2 m rows: broad brush strokes, not rice-scale lines
    sheen = soft_noise(22, 4.0, t)
    v = value_from(0.2 * rows + sheen, 0.07)          # rows faint: they cannot follow the curving fields
    h = norm(rows + 0.3 * sheen)
    return v, h


def flooded():
    """Calm water on a flooded paddy: very soft value drift and a few broad ripple bands."""
    t = TILE["flooded"]
    X, Y = grid(t)
    drift = soft_noise(31, 5.0, t)
    bend = soft_noise(32, 4.0, t)
    bands = np.cos(2 * np.pi * (Y + 0.8 * bend) / (t / 5)) * (0.5 + 0.5 * soft_noise(33, 3.5, t).clip(-1, 1))
    v = value_from(drift + 0.25 * bands, 0.05)
    h = norm(bands)
    return v, h


def rock():
    """Rock and banks: big stones, each a few flat planes on a rounded body, soft joints; two soft cracks as relief."""
    t = TILE["rock"]
    d1, d2, idx, pts, rng = cells(41, 16, t)
    X, Y = grid(t)
    edge = np.clip((d2 - d1) / 1.6, 0, 1)                                   # 0 at a joint, 1 a stone's width in
    dome = np.sqrt(1 - (1 - edge) ** 2)                                     # rounded body
    tilt = rng.normal(0, 0.10, (len(pts), 2))
    lift = rng.uniform(-0.3, 0.3, len(pts))
    dx = (X - pts[idx, 0] + t / 2) % t - t / 2; dy = (Y - pts[idx, 1] + t / 2) % t - t / 2
    plane = tilt[idx, 0] * dx + tilt[idx, 1] * dy + lift[idx]               # one flat facet per stone
    crack = np.exp(-(soft_noise(42, 3.0, t) / 0.06) ** 2) * (soft_noise(43, 4.0, t) > 0.3)
    crack = blur_wrap(crack.astype(np.float32), 0.08, t)
    h = norm(dome + 0.55 * plane - 0.5 * crack)
    tone = rng.uniform(-1, 1, len(pts))[idx]                                # each stone a hair lighter or darker
    v = value_from(0.5 * blur_wrap(tone, 0.35, t) + 0.7 * dome + 0.6 * soft_noise(44, 4.0, t), 0.10)
    return v, h


def paving():
    """Streets, the court platform top, stone works: big painted slabs 2 to 3 m in running courses, drawn with the land
    kit's own calm stone painter (terrace_calm_paint.stone_face: one tone a slab, a soft pale bevel, joints darker, a
    soft pillow in the height). Returned as a colour multiplier over the face's own stone colour."""
    import random
    t = TILE["paving"]
    ppm = N / t
    r = random.Random(51)
    rng = np.random.default_rng(51)
    polys, tones = [], []
    y = 0.0
    for hgt in (2.6, 2.4, 2.5, 2.5):
        x = r.uniform(0, 2.5)
        end = x + t
        while x < end - 1e-6:
            w = r.choice([2.0, 2.4, 2.8, 3.0])
            if end - (x + w) < 1.6:
                w = end - x
            g = 0.05
            j = lambda: r.uniform(-0.04, 0.04)
            polys.append([((x + g + j()) * ppm, (y + g + j()) * ppm), ((x + w - g + j()) * ppm, (y + g + j()) * ppm),
                          ((x + w - g + j()) * ppm, (y + hgt - g + j()) * ppm), ((x + g + j()) * ppm, (y + hgt - g + j()) * ppm)])
            tones.append(K.f(K.STONE) * r.uniform(0.94, 1.05) * np.array([1.0, 1.0, r.uniform(0.97, 1.02)], np.float32))
            x += w
        y += hgt
    col, H, alp = K.stone_face(N, N, polys, ppm, rng, r, tones=tones, joint=0.90, bev=0.08, wrap_ext=int(3.2 * ppm),
                               round_px=int(0.07 * ppm), big=4.0)
    v = col / col.reshape(-1, 3).mean(0)
    return v, norm(H)


def water():
    """River and pool: soft broad ripples (relief only) and a very gentle drift."""
    t = TILE["water"]
    r = soft_noise(61, 1.4, t, aniso=(2.2, 1.0)) + 0.5 * soft_noise(62, 3.0, t)
    v = value_from(soft_noise(63, 6.0, t), 0.05)
    return v, norm(r)


def foam():
    """Rapids: soft white foam in broad streaks down the flow (y), lighter and darker water between."""
    t = TILE["foam"]
    s = soft_noise(71, 1.0, t, aniso=(1.8, 1.0)) + 0.6 * soft_noise(72, 2.5, t)
    blobs = np.clip(s, -1.5, 1.5)
    v = value_from(blobs, 0.12)
    return v, norm(blobs)


def macro():
    """One very large, very soft drift laid over all the land so no tile repeats in a big field (value only)."""
    v = value_from(soft_noise(81, 22.0, TILE["macro"]) + 0.4 * soft_noise(82, 9.0, TILE["macro"]), 0.05)
    return v, np.zeros_like(v)


SURFACES = {"grass": grass, "paddy": paddy, "flooded": flooded, "rock": rock, "paving": paving,
            "water": water, "foam": foam, "macro": macro}


def bake_normal(h01, name):
    """The height (0..1 times DEPTH metres) to an OpenGL green-up normal map with the calm pieces' baker, wrapped."""
    H = np.tile(h01 * DEPTH[name], (3, 3)).astype(np.float32)
    nm = height_to_normal(H, [(0, 0, 3 * N, 3 * N, N / TILE[name])], strength=3.0, blur=1.5)
    return nm[N:2 * N, N:2 * N]


ROWS_PPM, ROWS_MAX = 1.0, 130.0                    # the paddy row map: 1 px a metre, distances to 130 m


def paddy_rows(tag):
    """paddy_rows.png: for every point of the paddy area, how far it is to the nearest terrace wall (16 bit, 0..ROWS_MAX
    m), so the shader can paint rice rows that run along each field, parallel to its walls, the way Ifugao paddies are
    planted. Measured on the land itself (land_<tag>.npz, faces of material 1); paddy_rows.txt holds the box."""
    from scipy import ndimage as ndi
    d = np.load(os.path.join(ROOT, "Logs", "payo", "terrain", "land_%s.npz" % tag))
    v, lo, tot, mat = d["verts"], d["loops"], d["totals"], d["mat"]
    st = np.concatenate([[0], np.cumsum(tot)[:-1]])
    pad = np.where((mat == 2) | (mat == 5))[0]
    pp = v[lo[st[pad]]]
    x0, y0 = np.floor(pp[:, :2].min(0)) - 30
    x1, y1 = np.ceil(pp[:, :2].max(0)) + 30
    w, h = int((x1 - x0) * ROWS_PPM), int((y1 - y0) * ROWS_PPM)
    wall = np.zeros((h, w), bool)
    f = np.where(mat == 1)[0]
    tri = np.stack([v[lo[st[f] + k]] for k in range(3)], 1)            # the walls are triangles
    for a_, b_ in [(i / 4, j / 4) for i in range(5) for j in range(5 - i)]:
        p = tri[:, 0] * (1 - a_ - b_) + tri[:, 1] * a_ + tri[:, 2] * b_
        ix = ((p[:, 0] - x0) * ROWS_PPM).astype(int); iy = ((y1 - p[:, 1]) * ROWS_PPM).astype(int)   # row 0 at the north
        ok = (ix >= 0) & (ix < w) & (iy >= 0) & (iy < h)
        wall[iy[ok], ix[ok]] = True
    dist = ndi.distance_transform_edt(~wall).astype(np.float32) / ROWS_PPM
    dist = ndi.gaussian_filter(dist, 1.2)
    Image.fromarray((np.clip(dist / ROWS_MAX, 0, 1) * 65535 + 0.5).astype(np.uint16)).save(os.path.join(OUT, "paddy_rows.png"))
    open(os.path.join(OUT, "paddy_rows.txt"), "w").write("%g %g %g %g %g\n" % (x0, y0, x1, y1, ROWS_MAX))
    print("rows     %d x %d px over x %g..%g, y %g..%g (paste into CALM_ROWS)" % (w, h, x0, x1, y0, y1))


RICE_PPM = 2.0                                     # the rice density map: 2 px a metre, world space


def rice_density(tag):
    """rice_density.png: where rice stands on the paddies, 0..1, in world space (rice_density.txt: x0 y0 x1 y1 of the
    picture, row 0 at y1, the north). Real Ifugao paddies (owner: "muddy brown with green patches"): every field gets
    its own planting, a few fully green, most dense or patchy, a few sparse or nearly bare mud; inside a field the rice
    grows in soft irregular clumps with a slightly ragged edge. Ripe gold fields stand full. Flooded paddies hold only a
    little young rice. The fields are told apart by their walls (connected paddy areas between terrace walls). The land
    shader paints mud where this is 0 and rice where it is 1; the Unity blade pass scatters blades from it."""
    from scipy import ndimage as ndi
    from scipy.special import ndtri
    d = np.load(os.path.join(ROOT, "Logs", "payo", "terrain", "land_%s.npz" % tag))
    v, lo, tot, mat, lc = d["verts"], d["loops"], d["totals"], d["mat"], d["lcol"]
    st = np.concatenate([[0], np.cumsum(tot)[:-1]])
    pad = np.where((mat == 2) | (mat == 5))[0]
    pp = v[lo[st[pad]]]
    x0, y0 = np.floor(pp[:, :2].min(0)) - 10
    x1, y1 = np.ceil(pp[:, :2].max(0)) + 10
    w, h = int((x1 - x0) * RICE_PPM), int((y1 - y0) * RICE_PPM)

    def raster(faces, value=None, n=7):
        """Paints faces into a (h, w) map by dense barycentric points; value per face (else True)."""
        out = np.zeros((h, w), np.float32 if value is not None else bool)
        tri = np.stack([v[lo[st[faces] + k]] for k in range(3)], 1)
        for i in range(n + 1):
            for j in range(n + 1 - i):
                a_, b_ = i / n, j / n
                q = tri[:, 0] * (1 - a_ - b_) + tri[:, 1] * a_ + tri[:, 2] * b_
                ix = ((q[:, 0] - x0) * RICE_PPM).astype(int); iy = ((y1 - q[:, 1]) * RICE_PPM).astype(int)
                ok = (ix >= 0) & (ix < w) & (iy >= 0) & (iy < h)
                out[iy[ok], ix[ok]] = True if value is None else value[ok]
        return out
    c = lc[st[pad]]
    gold = (c[:, 0] / np.maximum(c[:, 1], 1e-3) > 1.0).astype(np.float32)   # the ripe fields: red over green (haze stays under)
    flood = (mat[pad] == 5).astype(np.float32)
    raw = raster(pad)
    area = ndi.binary_closing(raw, iterations=1)
    goldm = ndi.grey_closing(raster(pad, gold), size=3)
    floodm = ndi.grey_closing(raster(pad, flood), size=3)
    walls = ndi.binary_dilation(raster(np.where(mat == 1)[0]), iterations=1)
    fields, n = ndi.label(raw & ~walls)              # the cross dikes (not paddy) part the plots
    rng = np.random.default_rng(54)
    # each field's planting: fully green, dense, patchy, sparse, nearly bare
    kinds = np.array([0.97, 0.80, 0.62, 0.45, 0.28, 0.06])
    share = np.array([0.08, 0.17, 0.25, 0.22, 0.16, 0.12])
    dens = np.concatenate([[0.0], rng.choice(kinds, n, p=share) * rng.uniform(0.92, 1.05, n)])[fields]
    dens = ndi.grey_dilation(dens, size=3)                             # the gaps the labels left take a neighbour's planting
    # the clumps: soft noise (8 to 15 m shapes, a ragged 2 m edge), thresholded at the field's own density
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32) / RICE_PPM
    sm = lambda sig: ndi.gaussian_filter(rng.standard_normal((h // 4 + 1, w // 4 + 1)).astype(np.float32), sig * RICE_PPM / 4)
    big = ndi.zoom(sm(5.0), 4, order=1)[:h, :w]
    fine = ndi.zoom(sm(1.2), 4, order=1)[:h, :w]
    nz = big / big.std() + 0.30 * fine / fine.std()
    nz /= nz.std()
    thr = ndtri(np.clip(1 - dens, 1e-4, 1 - 1e-4))
    rice = 1 / (1 + np.exp(-(nz - thr) / 0.15))                        # a soft edge about a metre wide
    rice = np.where(dens > 0.95, 1.0, rice)
    rice = np.maximum(rice, goldm)                                      # ripe fields stand full
    rice = np.where(floodm > 0.5, rice * 0.35, rice)                    # flooded: a little young rice only
    rice = np.where(area, rice, 0.0)
    rice = ndi.gaussian_filter(rice.astype(np.float32), 0.6)
    Image.fromarray((np.clip(rice, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(OUT, "rice_density.png"))
    open(os.path.join(OUT, "rice_density.txt"), "w").write(
        "%g %g %g %g\n# x0 y0 x1 y1 in metres (world = Blender object space of the land), %g px a metre, row 0 at y1 (north),"
        " column 0 at x0; 0 = mud, 1 = rice standing\n" % (x0, y0, x1, y1, RICE_PPM))
    print("rice     %d x %d px, %d fields, mean density on paddies %.2f" % (w, h, n, rice[area].mean()))


def save_wall():
    """bank_color.png (sRGB colour, alpha = stone coverage), bank_height.png (0..1 of DEPTH['bank']), bank_normal.png."""
    col, cover, H, ppm = bank_wall()
    rgba = np.dstack([col, cover])
    Image.fromarray((rgba * 255 + 0.5).astype(np.uint8), "RGBA").save(os.path.join(OUT, "bank_color.png"))
    Image.fromarray((SHADE[0] * 255 + 0.5).astype(np.uint8)).save(os.path.join(OUT, "bank_shade.png"))
    stone = cover > 0.5                                                 # the far-off look: the stones' average, its share
    open(os.path.join(OUT, "bank_mean.txt"), "w").write("%.4f %.4f %.4f %.4f\n" % (*col[stone].mean(0), cover.mean()))
    lo, hi = float(H.min()), float(H.max())
    Image.fromarray(((H - lo) / (hi - lo) * 65535 + 0.5).astype(np.uint16)).save(os.path.join(OUT, "bank_height.png"))
    Hw = np.tile(H, (1, 3)).astype(np.float32)                         # wraps along the wall, not up it
    nm = height_to_normal(Hw, [(0, 0, Hw.shape[1], Hw.shape[0], ppm)], strength=3.0, blur=1.5)[:, H.shape[1]:2 * H.shape[1]]
    Image.fromarray(nm).save(os.path.join(OUT, "bank_normal.png"))
    print("bank     wall %.0f x %.0f m  relief %.3f m (set CALM_TEX['bank'] depth to this)" % (WALL_L, WALL_H, hi - lo))
    return col, nm


def main():
    os.makedirs(OUT, exist_ok=True)
    sheet = []
    for name, fn in SURFACES.items():
        v, h = fn()
        v8 = np.clip(v * 0.5 * 255 + 0.5, 0, 255).astype(np.uint8)
        Image.fromarray(v8).save(os.path.join(OUT, "%s_value.png" % name))
        if DEPTH[name]:
            Image.fromarray((np.clip(h, 0, 1) * 65535 + 0.5).astype(np.uint16)).save(os.path.join(OUT, "%s_height.png" % name))
            nm = bake_normal(h, name)
            Image.fromarray(nm).save(os.path.join(OUT, "%s_normal.png" % name))
        else:
            nm = np.full((N, N, 3), (128, 128, 255), np.uint8)
        print("%-8s tile %5.1f m  value %.3f..%.3f (mean %.3f)  depth %.3f m" % (name, TILE[name], v.min(), v.max(), v.mean(), DEPTH[name]))
        a = v8 if v8.ndim == 3 else np.repeat(v8[..., None], 3, -1)
        sheet.append(np.concatenate([a[::4, ::4], nm[::4, ::4]], 0))
    col, nm = save_wall()
    strip = np.concatenate([(col * 255).astype(np.uint8), nm], 0)[::2, ::2]
    top = np.concatenate(sheet, 1)
    w = max(top.shape[1], strip.shape[1])
    pad = lambda a: np.pad(a, ((0, 0), (0, w - a.shape[1]), (0, 0)))
    Image.fromarray(np.concatenate([pad(top), pad(strip)], 0)).save(os.path.join(OUT, "sheet.png"))


if __name__ == "__main__":
    main()
    land = next((x.split("=", 1)[1] for x in sys.argv[1:] if x.startswith("--land=")), "v55tex")
    paddy_rows(land)
    rice_density(land)
