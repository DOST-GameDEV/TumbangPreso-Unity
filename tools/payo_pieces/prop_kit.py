"""PAYO, ONE PROP AT A TIME: the kit for the village's things (2026-10-12).

The owner on the second art pass: "the old process was to build the different buildings and props individually first to
refine them", "most of the props were developed in parallel". A prop here is ONE object typed as closed fat pieces, each
piece wearing paint made for it, all the pieces of a prop unioned into one closed skin (prop_weld.py), a set of props
sharing one atlas. The house way of making a model (tools/hand_companion_kit.py, tools/build_slippers.py), with its own
shapes because a prop needs UVs on every piece and several paint panels on a box.

  1. py -3 tools/payo_pieces/prop_set_<x>.py                       parts/<prop>.glb, set<x>-atlas.png, set<x>.json
  2. blender -b --python tools/payo_pieces/prop_weld.py -- <x>      Logs/payo/pieces/build/prop/<prop>.glb
  3. blender -b --python tools/payo_pieces/prop_review.py -- <x>    shots
  4. py -3 tools/payo_pieces/prop_sheet.py <x> <version>            Logs/payo/pieces/prop/set<x>_vN.png

CONVENTIONS: metres, y up, pivot on the ground at the centre. A prop is typed at its REAL size and written 15 percent
larger (SCALE) so it reads at a distance. A mesh is a dict: pos, tris, uv (0..1 in its own panel, v 0 at the bottom),
panel (which paint panel a point uses), nats (each panel's real width and height in metres: the atlas is cut to it).
"""
import json
import math
import random
import struct
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Logs/payo/pieces/build/prop"
SCALE = 1.15
SS = 2                                                # the paint is drawn twice as large and brought down: clean lines

# Owner, 2026-10-12: "the texture all reads as too green and teal. the reference i gave only had cool blue LIGHTING. the
# stones were the old grey". So STONE IS PAINTED A NATURAL, SLIGHTLY WARM GREY with a pale edge; the cool tone is the
# game's light. Wood is wood brown, bronze is bronze, cloth keeps the Cordillera red, black, white and yellow.
INK = (50, 36, 40)
STONE, STONE_D, STONE_L, PALE = (126, 122, 116), (86, 83, 82), (154, 150, 142), (188, 184, 172)
OCHRE, GOLD, ORANGE, RED, TURQ = (226, 160, 58), (246, 196, 66), (232, 112, 52), (196, 52, 44), (60, 204, 188)
WOOD, WOOD_D, WOOD_L, WOOD_R = (164, 104, 62), (112, 68, 48), (214, 160, 104), (146, 74, 50)
CREAM, WHITE, BLACK = (240, 228, 196), (250, 246, 232), (44, 38, 46)
BAMBOO, BAMBOO_D, BAMBOO_L = (190, 178, 84), (132, 132, 60), (232, 222, 136)
STRAW, STRAW_D, RATTAN, RATTAN_D = (232, 190, 92), (184, 132, 58), (214, 168, 104), (140, 92, 58)
CLAY, CLAY_D, LEAF, LEAF_L, MOSS = (206, 106, 66), (150, 70, 52), (70, 150, 84), (150, 206, 100), (104, 150, 78)
BRONZE, BRONZE_D, BRONZE_L, BONE = (214, 150, 62), (150, 92, 46), (250, 214, 120), (236, 226, 198)
CL_RED, CL_BLACK, CL_WHITE, CL_YELLOW = (202, 44, 42), (40, 34, 42), (246, 240, 226), (246, 198, 54)

# ------------------------------------------------------------------ CALM mode (Logs/payo/pieces/CALM_STYLE.md)
# py -3 tools/payo_pieces/prop_set_<x>.py --calm [--out <dir>]   writes Logs/payo/pieces/calm/prop/ (never build/prop)
# The SAME pieces, panels and atlas layout (the layout depends only on the shapes; every panel paints with its own
# seeded random, never the shapes'), new paint: the shared calm stone, softer accents, no speckle, grain lines, cracks
# or strand-by-strand weave; each prop keeps its fat shapes and one bold pattern (the jar's glaze band, a basket's
# weave as a few broad bands, the Cordillera weave as large blocks). Each panel also paints a height map (metres),
# baked to a tangent-space normal map per panel (calm_glb.height_to_normal), beside the set's atlas.
CALM = "--calm" in sys.argv
NSTRENGTH = 3.0
if "--out" in sys.argv:
    OUT = Path(sys.argv[sys.argv.index("--out") + 1])
elif CALM:
    OUT = ROOT / "Logs/payo/pieces/calm/prop"


def _soft(c, sat=0.80, warm=0.06):
    lum = 0.30 * c[0] + 0.55 * c[1] + 0.15 * c[2]
    c = [lum + (v - lum) * sat for v in c]
    return tuple(int(round(v + (w - v) * warm)) for v, w in zip(c, (245, 230, 204)))


if CALM:
    STONE, STONE_D, STONE_L, PALE = (165, 156, 140), (146, 138, 124), (178, 170, 154), (214, 205, 186)
    OCHRE, GOLD, ORANGE, RED = _soft(OCHRE), _soft(GOLD), _soft(ORANGE), _soft(RED, 0.85)
    WOOD, WOOD_D, WOOD_L, WOOD_R = _soft(WOOD, 0.85, 0.04), _soft(WOOD_D, 0.85, 0.04), _soft(WOOD_L, 0.85, 0.04), _soft(WOOD_R, 0.85, 0.04)
    CREAM, WHITE = _soft(CREAM, 1.0, 0.03), _soft(WHITE, 1.0, 0.04)
    BAMBOO, BAMBOO_D, BAMBOO_L = _soft(BAMBOO), _soft(BAMBOO_D), _soft(BAMBOO_L)
    STRAW, STRAW_D, RATTAN, RATTAN_D = _soft(STRAW), _soft(STRAW_D), _soft(RATTAN, 0.85), _soft(RATTAN_D, 0.85)
    CLAY, CLAY_D = _soft(CLAY), _soft(CLAY_D)
    LEAF, LEAF_L, MOSS = _soft(LEAF, 0.72, 0.04), _soft(LEAF_L, 0.72, 0.04), (126, 144, 92)
    BRONZE, BRONZE_D, BRONZE_L, BONE = _soft(BRONZE), _soft(BRONZE_D), _soft(BRONZE_L), _soft(BONE, 1.0, 0.02)
    CL_RED, CL_WHITE, CL_YELLOW = _soft(CL_RED, 0.88), _soft(CL_WHITE, 1.0, 0.02), _soft(CL_YELLOW)


def shade(c, f):
    return tuple(max(0, min(255, int(round(v * f)))) for v in c)


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


# ------------------------------------------------------------------ meshes

def M(pos, tris, uv, nat, panel=None, nats=None):
    pos = np.asarray(pos, np.float64).reshape(-1, 3)
    return {"pos": pos, "tris": np.asarray(tris, np.int64).reshape(-1, 3), "uv": np.asarray(uv, np.float64).reshape(-1, 2),
            "panel": np.zeros(len(pos), np.int64) if panel is None else np.asarray(panel, np.int64), "nats": nats or [nat]}


def lathe(profile, seg=14, sx=1.0, sz=1.0):
    """Turned about y from (radius, y) pairs, bottom to top; open ends are capped. u goes round, v along the profile.
    m["t"] is each GIVEN profile point's v, for the paint."""
    prof = [(float(r), float(y)) for r, y in profile]
    first = 0
    if prof[0][0] > 1e-6:
        prof.insert(0, (0.0, prof[0][1])); first = 1
    if prof[-1][0] > 1e-6:
        prof.append((0.0, prof[-1][1]))
    arc = [0.0]
    for a, b in zip(prof, prof[1:]):
        arc.append(arc[-1] + math.hypot(b[0] - a[0], b[1] - a[1]))
    total = arc[-1] or 1.0
    pos, uv, tris = [], [], []
    for k, (r, y) in enumerate(prof):
        for s in range(seg + 1):
            u = 2 * math.pi * (s % seg) / seg
            pos.append((r * math.cos(u) * sx, y, r * math.sin(u) * sz)); uv.append((s / seg, arc[k] / total))
    for k in range(len(prof) - 1):
        for s in range(seg):
            a = k * (seg + 1) + s
            b = a + seg + 1
            if prof[k + 1][0] > 1e-6:
                tris.append((a, b, b + 1))
            if prof[k][0] > 1e-6:
                tris.append((a, b + 1, a + 1))
    rmax = max(r for r, _ in prof)
    m = M(pos, tris, uv, (2 * math.pi * rmax * (sx + sz) * 0.5, total))
    m["t"] = [arc[first + i] / total for i in range(len(profile))]
    return m


def ball(half, seg=12, rings=7):
    prof = [(math.sin(math.pi * k / rings), -math.cos(math.pi * k / rings)) for k in range(rings + 1)]
    prof[0], prof[-1] = (0.0, -1.0), (0.0, 1.0)
    m = lathe(prof, seg)
    m["pos"] *= np.array(half, np.float64)
    m["nats"] = [(math.pi * (half[0] + half[2]), math.pi * half[1])]
    return m


def tube(points, radius, seg=8, cap=True):
    """A round tube along `points`, domed at both ends: a pole, a rope, a horn. u goes round, v along."""
    p = np.array(points, np.float64)
    n = len(p)
    rad = np.full(n, float(radius)) if np.isscalar(radius) else np.array(radius, np.float64)
    tan = np.gradient(p, axis=0)
    tan /= np.maximum(np.linalg.norm(tan, axis=1, keepdims=True), 1e-9)
    ref = np.array([0, 0, 1.0]) if abs(tan[0][2]) < 0.9 else np.array([1.0, 0, 0])
    side = np.cross(tan[0], ref)
    side /= np.linalg.norm(side)
    rows = []
    for i in range(n):
        side = side - tan[i] * float(np.dot(side, tan[i]))
        side /= max(float(np.linalg.norm(side)), 1e-9)
        rows.append((p[i], side.copy(), np.cross(tan[i], side), rad[i]))
    if cap:
        a, b = rows[0], rows[-1]
        rows = [(a[0] - tan[0] * a[3] * 0.9, a[1], a[2], 0.0), (a[0] - tan[0] * a[3] * 0.55, a[1], a[2], a[3] * 0.78)] + rows + \
               [(b[0] + tan[-1] * b[3] * 0.55, b[1], b[2], b[3] * 0.78), (b[0] + tan[-1] * b[3] * 0.9, b[1], b[2], 0.0)]
    else:
        rows = [(rows[0][0], rows[0][1], rows[0][2], 0.0)] + rows + [(rows[-1][0], rows[-1][1], rows[-1][2], 0.0)]
    length = [0.0]
    for q0, q1 in zip(rows, rows[1:]):
        length.append(length[-1] + float(np.linalg.norm(q1[0] - q0[0])) + abs(q1[3] - q0[3]) * (0.0 if cap else 1.0))
    total = length[-1] or 1.0
    pos, uv, tris = [], [], []
    for k, (c, a, b, r) in enumerate(rows):
        for s in range(seg + 1):
            u = 2 * math.pi * (s % seg) / seg
            pos.append(c + (a * math.cos(u) + b * math.sin(u)) * r); uv.append((s / seg, length[k] / total))
    for k in range(len(rows) - 1):
        for s in range(seg):
            a = k * (seg + 1) + s
            b = a + seg + 1
            if rows[k + 1][3] > 1e-9:
                tris.append((a, b, b + 1))
            if rows[k][3] > 1e-9:
                tris.append((a, b + 1, a + 1))
    return M(pos, tris, uv, (2 * math.pi * float(rad.max()), total))


def loft(rings, nat=None):
    """A skin over a list of rings (each K xyz, the same K and order), closed at both ends."""
    rings = [np.array(r, np.float64) for r in rings]
    k_n = len(rings[0])
    mids = [r.mean(axis=0) for r in rings]
    stations = [np.tile(mids[0], (k_n, 1))] + rings + [np.tile(mids[-1], (k_n, 1))]
    dist = [0.0]
    for a, b in zip(stations, stations[1:]):
        dist.append(dist[-1] + float(np.linalg.norm(b - a, axis=1).mean()))
    total = dist[-1] or 1.0
    pos, uv, tris = [], [], []
    for j, ring in enumerate(stations):
        for s in range(k_n + 1):
            pos.append(ring[s % k_n]); uv.append((s / k_n, dist[j] / total))
    for j in range(len(stations) - 1):
        for s in range(k_n):
            a = j * (k_n + 1) + s
            b = a + k_n + 1
            if j + 1 < len(stations) - 1:
                tris.append((a, b, b + 1))
            if j > 0:
                tris.append((a, b + 1, a + 1))
    per = max(float(np.linalg.norm(np.roll(r, -1, axis=0) - r, axis=1).sum()) for r in rings)
    return M(pos, tris, uv, nat or (per, total))


def sheet(fn, nu, nv, thick, nat):
    """A cloth or a leaf: fn(u, v) -> xyz over 0..1, given `thick` so it is a closed skin. Both faces wear the same
    paint: u left to right, v bottom to top."""
    grid = np.array([[fn(i / nu, j / nv) for i in range(nu + 1)] for j in range(nv + 1)], np.float64)
    du, dv = np.gradient(grid, axis=1), np.gradient(grid, axis=0)
    nrm = np.cross(du, dv)
    nrm /= np.maximum(np.linalg.norm(nrm, axis=2, keepdims=True), 1e-9)
    front, back = grid + nrm * thick * 0.5, grid - nrm * thick * 0.5
    uv = np.array([[(i / nu, j / nv) for i in range(nu + 1)] for j in range(nv + 1)], np.float64).reshape(-1, 2)
    count = (nu + 1) * (nv + 1)
    pos = np.concatenate([front.reshape(-1, 3), back.reshape(-1, 3)])
    tris = []
    for j in range(nv):
        for i in range(nu):
            a = j * (nu + 1) + i
            b, c, d = a + 1, a + nu + 2, a + nu + 1
            tris += [(a, b, c), (a, c, d), (a + count, c + count, b + count), (a + count, d + count, c + count)]
    edge = [j * (nu + 1) for j in range(nv + 1)] + [nv * (nu + 1) + i for i in range(1, nu + 1)] + \
           [j * (nu + 1) + nu for j in range(nv - 1, -1, -1)] + [i for i in range(nu - 1, 0, -1)]
    for a, b in zip(edge, edge[1:] + edge[:1]):
        tris += [(a, b, b + count), (a, b + count, a + count)]
    return M(pos, tris, np.concatenate([uv, uv]), nat)


def pillow(outline, thick, prof=((1, 0), (0.86, 0.62), (0.55, 0.93), (0, 1)), centre=None, back=None):
    """A fat flat thing from a silhouette (xy, star-shaped about `centre`), domed on both faces by `prof`: pairs of
    (how far out from the centre, how far up to half the thickness). It lies in the xy plane, its faces to +z and -z.
    The paint is one flat PANEL seen from the front (and worn by the back too), so a drawing lands as drawn."""
    o = np.array(outline, np.float64)
    c = np.array(centre, np.float64) if centre is not None else o.mean(axis=0)
    back = back or prof
    seq = [(s, -z) for s, z in reversed(back)] + [(s, z) for s, z in prof if not (abs(z) < 1e-9 and abs(back[0][1]) < 1e-9)]
    k_n = len(o)
    lo, hi = o.min(axis=0), o.max(axis=0)
    pos, uv, tris = [], [], []
    for s, z in seq:
        for i in range(k_n + 1):
            q = c + (o[i % k_n] - c) * s
            pos.append((q[0], q[1], z * thick * 0.5)); uv.append(((q[0] - lo[0]) / (hi[0] - lo[0]), (q[1] - lo[1]) / (hi[1] - lo[1])))
    for j in range(len(seq) - 1):
        for i in range(k_n):
            a = j * (k_n + 1) + i
            b = a + k_n + 1
            if seq[j + 1][0] > 1e-9:
                tris.append((a, b, b + 1))
            if seq[j][0] > 1e-9:
                tris.append((a, b + 1, a + 1))
    return M(pos, tris, uv, (hi[0] - lo[0], hi[1] - lo[1]))


def boxed(mesh, share=False):
    """Repaints any closed mesh as three flat panels (seen along x, along y, along z), each triangle on the panel it
    faces most. A box's bevels land on the rim of a panel, where the paint is the pale edge. `share` puts all three
    on ONE panel (a pole, a stone: the same paint whichever way it is seen)."""
    pos, tris = mesh["pos"], mesh["tris"]
    lo, hi = pos.min(axis=0), pos.max(axis=0)
    size = np.maximum(hi - lo, 1e-6)
    area = [size[2] * size[1], size[0] * size[2], size[0] * size[1]]
    axes = [(2, 1), (0, 2), (0, 1)]
    out, uv, panel = [], [], []
    for tri in tris:
        a, b, c = pos[tri[0]], pos[tri[1]], pos[tri[2]]
        n = np.abs(np.cross(b - a, c - a))
        n = n / max(float(np.linalg.norm(n)), 1e-12)
        best = max(range(3), key=lambda k: (round(float(n[k]), 3), area[k]))
        i, j = axes[best]
        for q in (a, b, c):
            out.append(q); uv.append(((q[i] - lo[i]) / size[i], (q[j] - lo[j]) / size[j])); panel.append(0 if share else best)
    nats = [(size[2], size[1]), (size[0], size[2]), (size[0], size[1])]
    if share:
        nats = [max(nats, key=lambda s: s[0] * s[1])]
    return M(out, np.arange(len(out)).reshape(-1, 3), uv, None, panel, nats)


def cbox(half, bevel, taper=1.0, share=False):
    """A box with every edge chamfered, centred. `taper` narrows (or widens) the top in x and z. Panels: 0 the faces
    seen along x, 1 along y (top and bottom), 2 along z."""
    hx, hy, hz = half
    b = min(bevel, hx * 0.49, hy * 0.49, hz * 0.49)
    h = (hx, hy, hz)
    pos = []
    for c in range(8):
        s = [1 if c >> k & 1 else -1 for k in range(3)]
        for a in range(3):
            pos.append([s[k] * (h[k] if k == a else h[k] - b) for k in range(3)])
    tris = []

    def quad(q):
        tris.append((q[0], q[1], q[2])); tris.append((q[0], q[2], q[3]))

    for a in range(3):
        o1, o2 = [k for k in range(3) if k != a]
        for sa in (0, 1):
            quad([(sa << a | b1 << o1 | b2 << o2) * 3 + a for b1, b2 in ((0, 0), (1, 0), (1, 1), (0, 1))])
    for a in range(3):
        for b_ in range(a + 1, 3):
            e = 3 - a - b_
            for sa in (0, 1):
                for sb in (0, 1):
                    c0 = sa << a | sb << b_
                    c1 = c0 | 1 << e
                    quad([c0 * 3 + a, c1 * 3 + a, c1 * 3 + b_, c0 * 3 + b_])
    for c in range(8):
        tris.append((c * 3, c * 3 + 1, c * 3 + 2))
    m = boxed(M(pos, tris, np.zeros((24, 2)), (1, 1)), share)
    if taper != 1.0:
        f = 1.0 + (taper - 1.0) * (m["pos"][:, 1] + hy) / (2 * hy)
        m["pos"][:, 0] *= f; m["pos"][:, 2] *= f
    return m


def planar(mesh, axis=1, flip=False):
    """Repaints a mesh as ONE flat panel seen along `axis` (both sides wear it)."""
    i, j = [(2, 1), (0, 2), (0, 1)][axis]
    pos = mesh["pos"]
    lo, hi = pos.min(axis=0), pos.max(axis=0)
    u = (pos[:, i] - lo[i]) / max(hi[i] - lo[i], 1e-9)
    v = (pos[:, j] - lo[j]) / max(hi[j] - lo[j], 1e-9)
    m = dict(mesh)
    m["uv"] = np.stack([1 - u if flip else u, v], axis=1)
    m["panel"] = np.zeros(len(pos), np.int64)
    m["nats"] = [(hi[i] - lo[i], hi[j] - lo[j])]
    return m


def rot_matrix(turn):
    rx, ry, rz = (math.radians(a) for a in turn)
    mx = np.array([[1, 0, 0], [0, math.cos(rx), -math.sin(rx)], [0, math.sin(rx), math.cos(rx)]])
    my = np.array([[math.cos(ry), 0, math.sin(ry)], [0, 1, 0], [-math.sin(ry), 0, math.cos(ry)]])
    mz = np.array([[math.cos(rz), -math.sin(rz), 0], [math.sin(rz), math.cos(rz), 0], [0, 0, 1]])
    return mz @ my @ mx


def T(mesh, at=(0, 0, 0), turn=(0, 0, 0), scale=(1, 1, 1)):
    """`mesh` scaled, turned by `turn` degrees about x, then y, then z, then moved to `at`."""
    m = dict(mesh)
    s = np.array((scale,) * 3 if np.isscalar(scale) else scale, np.float64)
    m["pos"] = (mesh["pos"] * s) @ rot_matrix(turn).T + np.array(at, np.float64)
    return m


def warp(mesh, fn):
    """Moves every point: fn(x, y, z) -> (x, y, z)."""
    m = dict(mesh)
    m["pos"] = np.array([fn(*p) for p in mesh["pos"]], np.float64)
    return m


def lumpy(mesh, amount, seed=1, freq=3.0):
    """Knocks a round shape out of true, the same way each time: a field stone, a gourd."""
    r = random.Random(seed)
    ph = [(r.uniform(0, 6.28), r.uniform(0, 6.28), r.uniform(0, 6.28)) for _ in range(3)]
    size = np.abs(mesh["pos"]).max(axis=0)

    def fn(x, y, z):
        u, v, w = x / size[0] * freq, y / size[1] * freq, z / size[2] * freq
        k = 1.0 + amount * (math.sin(u + ph[0][0]) * math.cos(v + ph[0][1]) + math.sin(v * 1.3 + ph[1][0]) * math.cos(w * 1.2 + ph[1][1]) + math.sin(w + ph[2][0]) * math.cos(u * 0.8 + ph[2][2])) / 3.0
        return x * k, y * k, z * k
    return warp(mesh, fn)


def arc(p0, p1, sag, count=9, up=(0, -1, 0)):
    """Points from p0 to p1 hanging `sag` in the middle."""
    p0, p1, up = np.array(p0, float), np.array(p1, float), np.array(up, float)
    return [p0 + (p1 - p0) * t + up * sag * 4 * t * (1 - t) for t in np.linspace(0, 1, count)]


def bez(p0, p1, p2, p3, count=10):
    p0, p1, p2, p3 = (np.array(p, float) for p in (p0, p1, p2, p3))
    return [((1 - t) ** 3) * p0 + 3 * ((1 - t) ** 2) * t * p1 + 3 * (1 - t) * t * t * p2 + (t ** 3) * p3 for t in np.linspace(0, 1, count)]


def ring(radius, y, count=16, sx=1.0, sz=1.0):
    return [(radius * math.cos(2 * math.pi * k / count) * sx, y, radius * math.sin(2 * math.pi * k / count) * sz) for k in range(count + 1)]


def torus(radius, thick, y=0.0, seg=16, sides=6, sx=1.0, sz=1.0):
    """A closed ring lying flat at height y: a rim, a rope tie, a hoop."""
    pos, uv, tris = [], [], []
    for i in range(seg + 1):
        a = 2 * math.pi * (i % seg) / seg
        for j in range(sides + 1):
            b = 2 * math.pi * (j % sides) / sides
            r = radius + thick * math.cos(b)
            pos.append((r * math.cos(a) * sx, y + thick * math.sin(b), r * math.sin(a) * sz)); uv.append((i / seg, j / sides))
    for i in range(seg):
        for j in range(sides):
            a = i * (sides + 1) + j
            b = a + sides + 1
            tris += [(a, b, b + 1), (a, b + 1, a + 1)]
    return M(pos, tris, uv, (2 * math.pi * radius, 2 * math.pi * thick))


# ------------------------------------------------------------------ paint

class Canvas:
    """One panel of the atlas while it is painted. t is 0 at the BOTTOM of the panel and 1 at the top, u 0..1 across.
    Lengths given in metres go through `px`."""

    def __init__(self, w, h, ppm, panel, nat, seed):
        self.w, self.h, self.ppm, self.panel, self.nat = w, h, ppm, panel, nat
        self.img = Image.new("RGB", (w, h), (128, 128, 128))
        self.d = ImageDraw.Draw(self.img)
        self.rnd = random.Random(seed)
        self.H = np.zeros((h, w), np.float32) if CALM else None          # calm: the panel's height, metres

    def relief(self, mask, metres, blur_m=0.006):
        """Calm: add `metres` of height where the L mask (or a float 0..1 array) is, its edge softened."""
        if self.H is None:
            return
        m = mask if isinstance(mask, Image.Image) else Image.fromarray((np.clip(mask, 0, 1) * 255).astype(np.uint8))
        m = m.filter(ImageFilter.GaussianBlur(max(1.0, self.px(blur_m))))
        self.H += np.asarray(m, np.float32) / 255.0 * metres

    def changed(self, before):
        """Calm: an L mask of the pixels that differ from the array `before` (what a drawing call just painted)."""
        a = np.asarray(self.img, np.int16)
        return Image.fromarray(((np.abs(a - before).sum(-1) > 6) * 255).astype(np.uint8))

    def px(self, metres):
        return metres * self.ppm

    def lw(self, metres=0.009):
        return max(2, int(round(metres * self.ppm)))

    def X(self, u):
        return u * self.w

    def Y(self, t):
        return (1.0 - t) * self.h

    def fill(self, col):
        self.d.rectangle([0, 0, self.w, self.h], fill=col)

    def band(self, t0, t1, col):
        self.d.rectangle([0, self.Y(t1), self.w, self.Y(t0)], fill=col)

    def box(self, u0, t0, u1, t1, col, outline=None, width=0):
        self.d.rectangle([self.X(u0), self.Y(t1), self.X(u1), self.Y(t0)], fill=col, outline=outline, width=width)

    def line(self, pts, col, width=None):
        if CALM and (width or self.lw()) < self.px(0.0105):                # calm: hairlines (veins, cracks, stalks) go
            return
        self.d.line([(self.X(u), self.Y(t)) for u, t in pts], fill=col, width=width or self.lw(), joint="curve")

    def poly(self, pts, col, outline=None):
        self.d.polygon([(self.X(u), self.Y(t)) for u, t in pts], fill=col, outline=outline)

    def dot(self, u, t, r_m, col, outline=None, width=0, squash=1.0):
        r = self.px(r_m)
        self.d.ellipse([self.X(u) - r, self.Y(t) - r * squash, self.X(u) + r, self.Y(t) + r * squash], fill=col, outline=outline, width=width)

    def oval(self, u, t, ru, rt, col=None, outline=None, width=0):
        self.d.ellipse([self.X(u - ru), self.Y(t + rt), self.X(u + ru), self.Y(t - rt)], fill=col, outline=outline, width=width)

    def shine(self, u, t0, t1, col, width=0.045):
        """The one highlight: a crisp lit streak with a dot under it, as a cartoon jar has."""
        hw = self.w * width * 0.5
        if CALM:                                                           # calm: one soft lift, not a crisp streak and dot
            m = Image.new("L", (self.w, self.h), 0)
            dm = ImageDraw.Draw(m)
            for du in (0.0, 0.5):
                x = self.X((u + du) % 1.0)
                dm.rounded_rectangle([x - hw * 1.6, self.Y(t1), x + hw * 1.6, self.Y(t0)], radius=hw * 1.6, fill=255)
            k = np.asarray(m.filter(ImageFilter.GaussianBlur(max(1.0, hw * 1.2))), np.float32)[..., None] / 255.0 * 0.38
            a = np.asarray(self.img, np.float32)
            self.redraw(a * (1 - k) + np.array(col, np.float32) * k)
            return
        for du in (0.0, 0.5):
            x = self.X((u + du) % 1.0)
            self.d.rounded_rectangle([x - hw, self.Y(t1), x + hw, self.Y(t0)], radius=hw, fill=col)
            y = self.Y(t1) - hw * 2.4
            self.d.ellipse([x - hw, y - hw, x + hw, y + hw], fill=col)

    def redraw(self, arr):
        self.img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB")
        self.d = ImageDraw.Draw(self.img)

    def vgrad(self, bottom=0.84, top=1.06):
        """The one shade: darker toward the bottom of the panel."""
        a = np.asarray(self.img, np.float32)
        if CALM:                                                           # calm: a gentler fall, a little cooler low
            bottom, top = 1.0 - (1.0 - bottom) * 0.8, 1.0 + (top - 1.0) * 0.6
            t = np.linspace(0, 1, self.h, dtype=np.float32)[:, None, None]
            a = a * (1 - 0.5 * t) + a * np.array([0.96, 0.98, 1.03], np.float32) * 0.5 * t
        f = np.linspace(top, bottom, self.h, dtype=np.float32)[:, None, None]
        self.redraw(a * f)

    def glow(self, u, t, ru, rt, col, strength=0.6):
        """A soft patch of `col` (a highlight, soot, moss): ru, rt in panel fractions."""
        a = np.asarray(self.img, np.float32)
        yy, xx = np.mgrid[0:self.h, 0:self.w].astype(np.float32)
        d = ((xx - self.X(u)) / max(1.0, ru * self.w)) ** 2 + ((yy - self.Y(t)) / max(1.0, rt * self.h)) ** 2
        k = np.clip(1.0 - d, 0, 1)[..., None] * strength
        self.redraw(a * (1 - k) + np.array(col, np.float32) * k)

    def motif(self, t0, t1, kind, col, n=None, bg=None, edge=None, pad=0.16):
        """One band of pattern right across the panel (it tiles: n whole repeats)."""
        if bg is not None:
            self.band(t0, t1, bg)
        before = np.asarray(self.img, np.int16) if CALM else None
        y0, y1 = self.Y(t1), self.Y(t0)
        bh = y1 - y0
        n = n or max(2, int(round(self.w / max(bh * 1.25, 1))))
        cw = self.w / n
        ya, yb = y0 + bh * pad, y1 - bh * pad
        lw = max(2, int(bh * 0.16))
        for k in range(-1, n + 1):
            x = k * cw
            if kind == "zig":
                self.d.line([(x, yb), (x + cw / 2, ya), (x + cw, yb)], fill=col, width=lw, joint="curve")
            elif kind == "dia":
                self.d.polygon([(x + cw / 2, ya), (x + cw * 0.9, (ya + yb) / 2), (x + cw / 2, yb), (x + cw * 0.1, (ya + yb) / 2)], fill=col)
            elif kind == "tri":
                self.d.polygon([(x + cw * 0.06, yb), (x + cw / 2, ya), (x + cw * 0.94, yb)], fill=col)
            elif kind == "tri2":
                self.d.polygon([(x + cw * 0.06, yb), (x + cw / 2, ya), (x + cw * 0.94, yb)], fill=col)
                self.d.polygon([(x + cw * 0.56, ya), (x + cw, yb - (yb - ya) * 0.12), (x + cw * 1.44, ya)], fill=edge or col)
            elif kind == "dot":
                r = min(cw, yb - ya) * 0.36
                self.d.ellipse([x + cw / 2 - r, (ya + yb) / 2 - r, x + cw / 2 + r, (ya + yb) / 2 + r], fill=col)
            elif kind == "x":
                self.d.line([(x + cw * 0.15, ya), (x + cw * 0.85, yb)], fill=col, width=lw)
                self.d.line([(x + cw * 0.15, yb), (x + cw * 0.85, ya)], fill=col, width=lw)
            elif kind == "bar":
                self.d.rectangle([x + cw * 0.25, ya, x + cw * 0.75, yb], fill=col)
            elif kind == "eye":                                                   # a diamond with a diamond in it
                self.d.polygon([(x + cw / 2, ya), (x + cw * 0.96, (ya + yb) / 2), (x + cw / 2, yb), (x + cw * 0.04, (ya + yb) / 2)], fill=col)
                self.d.polygon([(x + cw / 2, ya + (yb - ya) * 0.28), (x + cw * 0.72, (ya + yb) / 2), (x + cw / 2, yb - (yb - ya) * 0.28), (x + cw * 0.28, (ya + yb) / 2)], fill=edge or bg or INK)
            elif kind == "step":                                                  # a stepped terrace
                s = cw / 6.0
                hh = (yb - ya) / 3.0
                for i in range(3):
                    self.d.rectangle([x + s * i, yb - hh * (i + 1), x + cw - s * i, yb - hh * i], fill=col)
        if edge is not None and kind not in ("tri2", "eye"):
            e = max(2, int(bh * 0.07))
            self.d.rectangle([0, y0, self.w, y0 + e], fill=edge); self.d.rectangle([0, y1 - e, self.w, y1], fill=edge)
        if CALM:                                                           # the carved or woven figure stands up a little
            self.relief(self.changed(before), 0.0025, 0.004)

    def grain(self, dark, light, vertical, count=None, wobble=0.012):
        """Wood: long dark lines that wander a little, a few pale ones, a knot or two."""
        length, across = (self.h, self.w) if vertical else (self.w, self.h)
        if CALM:                                                           # calm: no grain lines; a few soft grooves in the relief
            m = Image.new("L", (self.w, self.h), 0)
            dm = ImageDraw.Draw(m)
            n = max(1, min(4, int(across / self.px(0.07))))
            for k in range(n):
                a = (k + 0.5 + self.rnd.uniform(-0.15, 0.15)) / n * across
                box = [a - self.px(0.006), 0, a + self.px(0.006), length]
                dm.rectangle(box if vertical else [box[1], box[0], box[3], box[2]], fill=255)
            self.relief(m, -0.0018, 0.008)
            return
        count = count or max(3, int(across / self.px(0.035)))
        for k in range(count):
            a = (k + self.rnd.uniform(0.2, 0.8)) / count * across
            col = light if k % 3 == 2 else dark
            pts, x = [], 0.0
            off = self.rnd.uniform(0, 6.28)
            start, stop = self.rnd.uniform(-0.1, 0.25) * length, self.rnd.uniform(0.75, 1.1) * length
            while x <= length:
                if start <= x <= stop:
                    w = a + math.sin(x / max(length, 1) * 7 + off) * self.px(wobble)
                    pts.append((w, x) if vertical else (x, w))
                x += max(4, length / 14)
            if len(pts) > 1:
                self.d.line(pts, fill=col, width=max(2, int(self.px(0.006))), joint="curve")

    def bevel(self, width_m, col, sides="ltrb", inner=None):
        """The pale painted edge of reference 238, round the rim of a panel."""
        b = max(3, int(self.px(width_m)))
        if CALM:                                                           # calm: a subtle lighter edge, rounded in the relief
            m = Image.new("L", (self.w, self.h), 0)
            dm = ImageDraw.Draw(m)
            for side, box_ in (("l", [0, 0, b, self.h]), ("r", [self.w - b, 0, self.w, self.h]), ("t", [0, 0, self.w, b]), ("b", [0, self.h - b, self.w, self.h])):
                if side in sides:
                    dm.rectangle(box_, fill=255)
            mb = np.asarray(m.filter(ImageFilter.GaussianBlur(max(1.0, b * 0.5))), np.float32)[..., None] / 255.0
            a = np.asarray(self.img, np.float32)
            self.redraw(a * (1 - mb * 0.38) + np.array(col, np.float32) * mb * 0.38)
            self.relief(m, -0.004, width_m * 0.5)
            return
        for side, box_ in (("l", [0, 0, b, self.h]), ("r", [self.w - b, 0, self.w, self.h]), ("t", [0, 0, self.w, b]), ("b", [0, self.h - b, self.w, self.h])):
            if side in sides:
                self.d.rectangle(box_, fill=col)
        if inner is not None:
            e = max(2, b // 4)
            self.d.rectangle([b, b, self.w - b, self.h - b], outline=inner, width=e)

    def specks(self, col, count, r_m=0.006, t0=0.0, t1=1.0, long=1.0):
        if CALM:                                                           # calm: no speckle
            return
        for _ in range(count):
            u, t = self.rnd.random(), self.rnd.uniform(t0, t1)
            r = self.px(r_m) * self.rnd.uniform(0.6, 1.3)
            a = self.rnd.uniform(0, math.pi)
            if long > 1.0:
                dx, dy = math.cos(a) * r * long, math.sin(a) * r * long
                self.d.line([(self.X(u) - dx, self.Y(t) - dy), (self.X(u) + dx, self.Y(t) + dy)], fill=col, width=max(2, int(r)))
            else:
                self.d.ellipse([self.X(u) - r, self.Y(t) - r, self.X(u) + r, self.Y(t) + r], fill=col)

    def drips(self, t_from, depth, col, count, edge=None):
        """Glaze run down from a line: fat rounded drops of uneven length (they tile)."""
        y0 = self.Y(t_from)
        cw = self.w / count
        before = np.asarray(self.img, np.int16) if CALM else None
        self.d.rectangle([0, 0, self.w, y0], fill=col)
        for k in range(-1, count + 1):
            x = (k + 0.5) * cw
            dl = depth * self.h * self.rnd.choice((0.35, 0.6, 1.0, 0.75, 0.5))
            half = cw * self.rnd.uniform(0.30, 0.46)
            for dx in (0, self.w, -self.w):
                self.d.rounded_rectangle([x - half + dx, y0 - half, x + half + dx, y0 + dl], radius=half, fill=col)
        if edge is not None:
            self.d.line([(0, self.Y(t_from) - self.h * 0.0), (self.w, self.Y(t_from))], fill=col, width=1)
        if CALM:                                                           # the glaze sits a little proud
            self.relief(self.changed(before), 0.0015, 0.005)

    def weave(self, a, b, cell_m=0.035, t0=0.0, t1=1.0, pattern=None, twill=False):
        """A basket's wall, strand by strand: a strand over, a strand under, each with its own lit and shaded edge.
        `pattern(i, j, cols, rows)` may answer a colour for a strand: the drawn figure."""
        y0, y1 = self.Y(t1), self.Y(t0)
        if CALM:                                                           # calm: a few broad bands, one bold band in the middle
            rows = max(2, int(round((y1 - y0) / self.px(0.10))))
            ch = (y1 - y0) / rows
            accent = None
            if pattern:
                seen = {}
                for j in range(12):
                    for i in range(24):
                        c = pattern(i, j, 24, 12)
                        if c is not None:
                            seen[c] = seen.get(c, 0) + 1
                accent = max(seen, key=seen.get) if seen else None
            b2 = mix(a, b, 0.6)
            for j in range(rows):
                col = a if j % 2 == 0 else b2
                if accent is not None and j == rows // 2:
                    col = accent
                self.d.rectangle([0, y0 + j * ch, self.w, y0 + (j + 1) * ch], fill=col)
            if self.H is not None:
                yy = np.arange(self.h, dtype=np.float32)
                ph = np.clip((yy - y0) / ch, 0, rows)
                rib = np.where((yy >= y0) & (yy <= y1), np.sin(np.pi * (ph % 1.0)) ** 0.7, 0)
                self.H += (rib * 0.004)[:, None].astype(np.float32)
                cell = max(4.0, self.px(0.06))                     # the weave lives in the relief only: broad soft cells
                xx = np.arange(self.w, dtype=np.float32)
                woven = np.sin(xx / cell * np.pi)[None, :] * np.sin(yy / cell * np.pi)[:, None]
                self.H += (woven * 0.0012 * ((yy >= y0) & (yy <= y1))[:, None]).astype(np.float32)
            return
        cols = max(4, int(round(self.w / self.px(cell_m) / 2)) * 2)
        rows = max(2, int(round((y1 - y0) / self.px(cell_m))))
        cw, ch = self.w / cols, (y1 - y0) / rows
        gap = max(1, int(min(cw, ch) * 0.10))
        for j in range(rows):
            for i in range(cols):
                over = ((i + j // 2) % 2 == 0) if twill else ((i + j) % 2 == 0)
                col = (pattern(i, rows - 1 - j, cols, rows) if pattern else None) or (a if over else b)
                x, y = i * cw, y0 + j * ch
                self.d.rectangle([x, y, x + cw, y + ch], fill=shade(col, 0.62))
                self.d.rectangle([x + gap, y + gap, x + cw - gap, y + ch - gap], fill=col)
                if over:
                    self.d.rectangle([x + gap, y + gap, x + cw - gap, y + gap + max(1, ch * 0.22)], fill=shade(col, 1.16))
                else:
                    self.d.rectangle([x + gap, y + gap, x + gap + max(1, cw * 0.22), y + ch - gap], fill=shade(col, 0.86))

    def rope(self, base, dark, twists=None):
        """Twisted cord: slanted strands across the panel's long way."""
        self.fill(base)
        vertical = self.h >= self.w
        length, across = (self.h, self.w) if vertical else (self.w, self.h)
        n = twists or max(3, int(length / self.px(0.03)))
        if CALM:                                                           # calm: broad soft twists, low contrast, in the relief
            n = max(2, min(n, int(length / self.px(0.07))))
            dark = mix(base, dark, 0.45)
            m = Image.new("L", (self.w, self.h), 0)
            dm = ImageDraw.Draw(m)
        step = length / n
        for k in range(-2, n + 2):
            p = [(0, k * step), (across, (k + 1.2) * step)]
            self.d.line(p if vertical else [(y, x) for x, y in p], fill=dark, width=max(2, int(step * 0.28)))
            if CALM:
                dm.line(p if vertical else [(y, x) for x, y in p], fill=255, width=max(2, int(step * 0.3)))
        if CALM:
            self.img = self.img.filter(ImageFilter.GaussianBlur(max(1.0, step * 0.08)))
            self.d = ImageDraw.Draw(self.img)
            self.relief(m, -0.003, 0.004)


def flat(col, grad=True):
    def fn(C):
        C.fill(col)
        if grad:
            C.vgrad(0.84, 1.06)
    return fn


def wood(base=WOOD, bevel=0.0, bands=(), grain="auto", ends=None, pale=None, top_plain=False):
    """Timber: grain the long way, carved bands (t0, t1, kind, colour, background), the pale edge."""
    def fn(C):
        C.fill(base)
        vertical = (C.h >= C.w) if grain == "auto" else grain == "v"
        C.grain(shade(base, 0.78), shade(base, 1.16), vertical)
        if not (top_plain and C.panel == 1):
            for t0, t1, kind, col, bg in bands:
                C.motif(t0, t1, kind, col, bg=bg, edge=shade(base, 0.6))
        if ends is not None:                                     # the cut end of a pole: pale with rings
            C.band(0.93, 1.0, ends); C.band(0.0, 0.07, ends)
        if C.panel != 1 or len(C.nat) == 0:
            C.vgrad(0.86, 1.05)
        if bevel:
            C.bevel(bevel, pale or mix(base, CREAM, 0.55))
    return fn


def bamboo(base=BAMBOO, nodes=0.34, dark=BAMBOO_D, light=BAMBOO_L, vertical=None, cut=True):
    """A bamboo pole: a lit stripe down it, a dark ring with a pale lip at every node, pale cut ends."""
    def fn(C):
        C.fill(base)
        vert = (C.h >= C.w) if vertical is None else vertical
        length = C.h if vert else C.w
        if vert:
            C.d.rectangle([C.w * 0.18, 0, C.w * 0.32, C.h], fill=light); C.d.rectangle([C.w * 0.62, 0, C.w * 0.95, C.h], fill=shade(base, 0.82))
        else:
            C.d.rectangle([0, C.h * 0.18, C.w, C.h * 0.32], fill=light); C.d.rectangle([0, C.h * 0.62, C.w, C.h * 0.95], fill=shade(base, 0.82))
        n = max(1, int(round(length / C.px(nodes))))
        e = max(2, int(C.px(0.012)))
        for k in range(n + 1):
            p = (k + 0.5) / n * length if n > 1 else length * 0.5
            if vert:
                C.d.rectangle([0, p - e, C.w, p], fill=dark); C.d.rectangle([0, p, C.w, p + e], fill=light)
            else:
                C.d.rectangle([p - e, 0, p, C.h], fill=dark); C.d.rectangle([p, 0, p + e, C.h], fill=light)
        if cut:
            c = max(2, int(length * 0.03))
            if vert:
                C.d.rectangle([0, 0, C.w, c], fill=CREAM); C.d.rectangle([0, C.h - c, C.w, C.h], fill=CREAM)
            else:
                C.d.rectangle([0, 0, c, C.h], fill=CREAM); C.d.rectangle([C.w - c, 0, C.w, C.h], fill=CREAM)
    return fn


def rope(base=STRAW, dark=STRAW_D, twists=None):
    def fn(C):
        C.rope(base, dark, twists)
    return fn


def stone(base=STONE, bevel=0.03, cracks=2, moss=0.0, bands=(), top_light=True):
    """The map's stone: a natural grey, a wide pale painted edge, a crack or two, speckle, moss creeping up."""
    def fn(C):
        if CALM:
            return stone_calm(C, base, bevel, moss, bands, top_light)
        C.fill(base)
        C.specks(shade(base, 0.86), 10, 0.02); C.specks(shade(base, 1.12), 8, 0.016)
        for _ in range(cracks):
            u, t = C.rnd.uniform(0.15, 0.85), C.rnd.uniform(0.3, 0.9)
            pts = [(u, t)]
            for _k in range(3):
                u, t = u + C.rnd.uniform(-0.12, 0.12), t - C.rnd.uniform(0.08, 0.2)
                pts.append((u, t))
            C.line(pts, STONE_D, C.lw(0.008))
        for t0, t1, kind, col, bg in bands:
            C.motif(t0, t1, kind, col, bg=bg, edge=STONE_D)
        if moss:
            for k in range(7):
                C.glow(C.rnd.random(), 0.0, 0.16, moss * C.rnd.uniform(0.5, 1.0), MOSS, 0.9)
        if not (C.panel == 1 and top_light):
            C.vgrad(0.84, 1.05)
        if bevel:
            C.bevel(bevel, PALE)
    return fn


def stone_calm(C, base, bevel, moss, bands, top_light):
    """Calm stone: the shared warm grey (darker or paler stones keep their step), no speckle or cracks, the band kept
    bold, a broad soft swell in the relief, a few big soft moss shapes at the TOP, the one soft gradient, a soft edge."""
    lum = sum(base) / 3.0
    k = 1.0 + (lum - 121.0) / 121.0 * 0.5                    # the approved stone is 121: darker / paler kept as a step
    C.fill(shade(STONE, k))
    for t0, t1, kind, col, bg in bands:
        C.motif(t0, t1, kind, col if col != STONE_L else shade(STONE, 1.08), bg=bg if bg != STONE_D else shade(STONE, 0.9), edge=shade(STONE, 0.88))
    if C.H is not None:
        yy, xx = np.mgrid[0:C.h, 0:C.w].astype(np.float32)
        r = C.rnd
        sw = np.sin(xx / max(1.0, C.px(0.22)) * 6.28 + r.uniform(0, 6.28)) * np.sin(yy / max(1.0, C.px(0.18)) * 6.28 + r.uniform(0, 6.28))
        C.H += (sw * 0.0025).astype(np.float32)
    if moss:
        for _ in range(max(1, int(round(moss * 4)))):
            u, rw = C.rnd.random(), C.rnd.uniform(0.12, 0.22)
            before = np.asarray(C.img, np.int16)
            C.glow(u, 1.0, rw, moss * C.rnd.uniform(0.35, 0.6), MOSS, 0.85)
            C.relief(C.changed(before), 0.003, 0.01)
    if not (C.panel == 1 and top_light):
        C.vgrad(0.84, 1.05)
    if bevel:
        C.bevel(bevel, PALE)


def cordillera(C, t0, t1, across=False):
    """The Cordillera weave between t0 and t1: a red ground, black borders, white and yellow figures in rows."""
    if CALM:                                                 # calm: large blocks: black edges, one row of big diamonds on the red
        rows = [(0.00, 0.11, None, None, CL_BLACK), (0.11, 0.20, None, None, CL_RED), (0.20, 0.80, "eye", CL_WHITE, CL_RED),
                (0.80, 0.89, None, None, CL_RED), (0.89, 1.00, None, None, CL_BLACK)]
        for a, b, kind, col, bg in rows:
            ta, tb = t0 + (t1 - t0) * a, t0 + (t1 - t0) * b
            if kind:
                bh = abs(C.Y(ta) - C.Y(tb))
                C.motif(ta, tb, kind, col, n=max(1, int(round(C.w / max(bh * 1.6, 1)))), bg=bg, edge=CL_BLACK, pad=0.04)
            else:
                C.band(ta, tb, bg)
        return
    C.band(t0, t1, CL_RED)
    rows = [(0.00, 0.07, None, None, CL_BLACK), (0.07, 0.10, None, None, CL_YELLOW), (0.10, 0.24, "eye", CL_WHITE, CL_BLACK),
            (0.24, 0.27, None, None, CL_YELLOW), (0.27, 0.42, "zig", CL_YELLOW, CL_RED), (0.42, 0.58, "dia", CL_WHITE, CL_RED),
            (0.58, 0.73, "zig", CL_YELLOW, CL_RED), (0.73, 0.76, None, None, CL_YELLOW), (0.76, 0.90, "eye", CL_WHITE, CL_BLACK),
            (0.90, 0.93, None, None, CL_YELLOW), (0.93, 1.00, None, None, CL_BLACK)]
    for a, b, kind, col, bg in rows:
        ta, tb = t0 + (t1 - t0) * a, t0 + (t1 - t0) * b
        if kind:
            C.motif(ta, tb, kind, col, bg=bg, edge=CL_RED if bg == CL_BLACK else None)
        else:
            C.band(ta, tb, bg)


# ------------------------------------------------------------------ a prop, a set

class Prop:
    def __init__(self, name, real, note="", at=(0.0, 0.0), yaw=0.0, k=1.0):
        self.name, self.real, self.note, self.at, self.yaw, self.k = name, real, note, at, yaw, k
        self.lift = 0.0                                # the review stands it this much above its ground (a bridge spans a gap)
        self.clip = True                               # nothing goes under the ground (a stone is cut flat where it sits)
        self.pieces = []

    def add(self, mesh, paint, key=None, k=1.0, op="add", group=""):
        """A closed piece and its paint. Pieces with the same `key` wear the same panel. `group` other than "" is a
        SEPARATE object of this prop (a flame hidden by day). `op` "cut" takes the piece out of what came before."""
        self.pieces.append({"mesh": mesh, "paint": paint, "key": key, "k": k, "op": op, "group": group})
        return mesh


class PropSet:
    def __init__(self, letter, size=2048, ppm=230):
        self.letter, self.size, self.ppm = letter, size, ppm
        self.props = []

    def prop(self, *a, **kw):
        p = Prop(*a, **kw)
        self.props.append(p)
        return p

    def build(self, only=None):
        (OUT / "parts").mkdir(parents=True, exist_ok=True)
        wants, order = {}, []
        for p in self.props:
            for n, piece in enumerate(p.pieces):
                for pi, nat in enumerate(piece["mesh"]["nats"]):
                    key = (p.name, piece["key"], pi) if piece["key"] else (p.name, n, pi)
                    piece.setdefault("rects", []).append(key)
                    if key not in wants:
                        wants[key] = (nat, piece["k"] * p.k, piece["paint"], pi)
                        order.append(key)
        ppm = self.ppm
        while True:
            sizes = {k: (int(min(760, max(14, wants[k][0][0] * ppm * wants[k][1]))), int(min(760, max(14, wants[k][0][1] * ppm * wants[k][1])))) for k in order}
            rects, x, y, row = {}, 4, 4, 0
            ok = True
            for k in sorted(order, key=lambda q: -sizes[q][1]):
                w, h = sizes[k]
                if x + w + 4 > self.size:
                    x, y, row = 4, y + row + 6, 0
                if y + h + 4 > self.size:
                    ok = False
                    break
                rects[k] = (x, y, w, h)
                x += w + 6
                row = max(row, h)
            if ok:
                break
            ppm *= 0.94
        used = (y + row) / self.size
        atlas = Image.new("RGB", (self.size, self.size), (120, 120, 120))
        Hatlas = np.zeros((self.size, self.size), np.float32) if CALM else None
        hrects = []
        for n, k in enumerate(order):
            x, y, w, h = rects[k]
            nat, mult, paint, pi = wants[k]
            C = Canvas(w * SS, h * SS, ppm * mult * SS, pi, nat, 1000 + n * 17)
            paint(C)
            tile = C.img.resize((w, h), Image.LANCZOS)
            atlas.paste(tile.resize((w + 4, h + 4)), (x - 2, y - 2))
            atlas.paste(tile, (x, y))
            if CALM:
                ht = np.asarray(Image.fromarray(C.H).resize((w, h), Image.BILINEAR), np.float32)
                Hatlas[y:y + h, x:x + w] = ht
                hrects.append((x, y, x + w, y + h, ppm * mult))
        atlas_path = OUT / "parts" / ("set%s-atlas.png" % self.letter)
        atlas.save(atlas_path)
        if CALM:                                           # beside the glbs: the atlas, its height map and normal map
            import calm_glb
            atlas.save(OUT / ("set%s-atlas.png" % self.letter))
            calm_glb.save_height(Hatlas, OUT / ("set%s_height.png" % self.letter))
            Image.fromarray(calm_glb.height_to_normal(Hatlas, hrects, strength=NSTRENGTH, blur=1.2)).save(OUT / ("set%s_normal.png" % self.letter))
            (OUT / "parts" / ("set%s-rects.json" % self.letter)).write_text(json.dumps({"%s|%s|%s" % k: rects[k] for k in order}))
        meta = {"atlas": atlas_path.name, "size": self.size, "ppm": round(ppm, 1), "used": round(used, 2), "props": []}
        for p in self.props:
            nodes = []
            for n, piece in enumerate(p.pieces):
                m = piece["mesh"]
                uv = np.zeros((len(m["pos"]), 2), np.float32)
                for pi, key in enumerate(piece["rects"]):
                    x, y, w, h = rects[key]
                    sel = m["panel"] == pi
                    uv[sel, 0] = (x + 0.5 + np.clip(m["uv"][sel, 0], 0, 1) * (w - 1)) / self.size
                    uv[sel, 1] = (y + 0.5 + (1.0 - np.clip(m["uv"][sel, 1], 0, 1)) * (h - 1)) / self.size
                pos = m["pos"] * SCALE
                if p.clip:
                    pos = pos.copy(); pos[:, 1] = np.maximum(pos[:, 1], 0.0)
                nodes.append(("%s|%03d_%s" % (piece["group"] or "main", n, piece["op"]), pos.astype(np.float32), uv, m["tris"].astype(np.uint32)))
            write_glb(OUT / "parts" / (p.name + ".glb"), nodes)
            pos = np.concatenate([q[1] for q in nodes])
            meta["props"].append({"name": p.name, "real": p.real, "note": p.note, "at": list(p.at), "yaw": p.yaw, "pieces": len(nodes), "lift": p.lift,
                                  "groups": sorted({pc["group"] for pc in p.pieces if pc["group"]}),
                                  "size": [round(float(v), 2) for v in (pos.max(axis=0) - pos.min(axis=0))]})
        (OUT / "parts" / ("set%s.json" % self.letter)).write_text(json.dumps(meta, indent=1))
        print("set %s: %d props, %d panels, %.0f px per metre, %d%% of the %d atlas" % (self.letter, len(self.props), len(order), ppm, used * 100, self.size))


def write_glb(path, nodes):
    doc = {"asset": {"version": "2.0", "generator": "tools/payo_pieces/prop_kit.py"}, "scene": 0, "scenes": [{"nodes": list(range(len(nodes)))}],
           "nodes": [], "meshes": [], "accessors": [], "bufferViews": [], "buffers": [{"byteLength": 0}]}
    blob = bytearray()

    def put(array, kind, target, bounds=False):
        while len(blob) % 4:
            blob.append(0)
        doc["bufferViews"].append({"buffer": 0, "byteOffset": len(blob), "byteLength": array.nbytes, "target": target})
        blob.extend(array.tobytes())
        a = {"bufferView": len(doc["bufferViews"]) - 1, "componentType": 5126 if array.dtype == np.float32 else 5125,
             "count": int(array.shape[0]) if array.ndim > 1 else int(array.size), "type": kind}
        if bounds:
            a["min"] = [float(v) for v in array.min(axis=0)]; a["max"] = [float(v) for v in array.max(axis=0)]
        doc["accessors"].append(a)
        return len(doc["accessors"]) - 1

    for name, pos, uv, tris in nodes:
        doc["meshes"].append({"name": name, "primitives": [{"attributes": {"POSITION": put(pos, "VEC3", 34962, True), "TEXCOORD_0": put(uv, "VEC2", 34962)},
                                                            "indices": put(tris.reshape(-1), "SCALAR", 34963)}]})
        doc["nodes"].append({"name": name, "mesh": len(doc["meshes"]) - 1})
    while len(blob) % 4:
        blob.append(0)
    doc["buffers"][0]["byteLength"] = len(blob)
    text = json.dumps(doc, separators=(",", ":")).encode()
    text += b" " * (-len(text) % 4)
    Path(path).write_bytes(struct.pack("<4sII", b"glTF", 2, 28 + len(text) + len(blob)) + struct.pack("<I4s", len(text), b"JSON") + text
                           + struct.pack("<I4s", len(blob), b"BIN\0") + bytes(blob))
