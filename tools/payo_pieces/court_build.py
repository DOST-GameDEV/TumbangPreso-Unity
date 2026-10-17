"""PAYO court platform, pit and gorge: ONE designed stone structure (Blender 5.0, headless).

  py tools/payo_pieces/court_paint.py                                         the atlas, height and normal map
  blender -b --python tools/payo_pieces/court_build.py                        the model, the .blend and a raw glb
  py tools/payo_pieces/calm_glb.py <raw glb> <atlas> <glb> <normal>           (court_run.sh does all of it)

Why (owner, 2026-10-18, of the old platform: terrain bent flat plus 12 m wall modules lined up against it): "do you
seriously not notice how bad the plated gorge basin looks?" and "it all looks so unpolished compared to the maps we've
made in the past". So this is authored the way Eskinita's architecture is (tools/author_eskinita_alley.py): a few big
one-piece shells with live bevels, every block with its own corners, nothing lined up from tiles.

Blender metres, x east, y north (the valley), z up; the court's playing top is z 0. The plan is a HORSESHOE: the
outline |x| <= 44, y -62 .. 56, with the opening |x| < 30, y > -30 (the pit, then the gorge running out north).

  paving              z -6.0, the horseshoe inside the copings (border course along every coping)
  outer walls         x = +-44, y = -62, and y = 56 for |x| >= 30: from the coping down to z -60
  outer coping        one mitred band round the outside: a low parapet (top -5.35) on the north half, and on the south
                      half a RETAINING WALL holding the town tier back, stepping up to a coping at +6.2 (piers at each
                      step); the gorge mouth corners have piers where the parapet meets the pit's rim lip
  pit                 walls x = +-30, y = -30 from -6.3 to the floor at -30 (floor |x| < 30, |y| < 30)
  gorge               x = +-30 walls carry on down to -46.4 for y 30 .. 56; floor -46.4; the pit floor's north face
                      (y = 30, -30 .. -46.4) has a flat-backed recess |x| < 22, z -46.4 .. -32, 1.5 m deep, for the
                      fall, its rocks and the two bulul spout heads
  carving             one carved fret course (-7.5 .. -8.7) round every wall, inside and out; a pale string course at
                      -30.3 .. -30.9 round the outside and across the recess lintel; quoins on every outside corner;
                      three niches in the pit's west wall, two spouts in its east wall; glyph panels on the retaining walls

Writes ArtSource/payo/court_platform.blend (live bevels) and Logs/payo/pieces/calm/court/court_platform_raw.glb.
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import court_layout as L  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = os.path.join(ROOT, "Logs", "payo", "pieces", "calm", "court")
ATLAS = os.path.join(OUT, "court_platform-atlas.png")
NORMAL = os.path.join(OUT, "court_platform_normal.png")
BLEND = os.path.join(ROOT, "ArtSource", "payo", "court_platform.blend")
RAW = os.path.join(OUT, "court_platform_raw.glb")
MAT = "py_court_stone"
S, PPM = L.SIZE, L.PPM

# ------------------------------------------------------------------------------------------------ the plan
X_OUT, Y_FRONT, Y_BACK = 44.0, 56.0, -62.0
X_PIT, Y_PIT_S, Y_PIT_N = 30.0, -30.0, 30.0
Z_PAVE, Z_WALL_TOP = -6.0, L.WALL_TOP_Z
Z_PIT, Z_GORGE, Z_FOOT = -30.0, -46.4, -60.0
RECESS = (22.0, -32.0, 1.5)                   # half width, top, depth
FRET = (-7.5, -8.7, 0.12)                     # top, bottom, proud
STRING = (-30.3, -30.9, 0.10)
CAP_T = L.CAP_T
LOW = -5.35                                   # the low parapet's top
QUOIN_P, QUOIN_T = 0.07, 0.25                 # quoin proud, depth into the wall

# the outer coping path (CCW seen from above, outward on the right), starting and ending inside the gorge mouth piers
PARA = [(-31.0, 56.0), (-44.0, 56.0), (-44.0, -62.0), (44.0, -62.0), (44.0, 56.0), (31.0, 56.0)]
# runs of constant coping top along it (s from 0 at the first point): the retaining walls step down the hill
RUNS = [(0, 66, LOW), (66, 76, -2.4), (76, 84, 0.8), (84, 91, 3.6), (91, 145, 6.2), (145, 153, 1.4), (153, 161, -2.0),
        (161, 171.6, LOW), (171.6, 178.4, None), (178.4, 189, LOW), (189, 197, -2.0), (197, 205, 1.4), (205, 259, 6.2), (259, 266, 3.6), (266, 274, 0.8),
        (274, 284, -2.4), (284, 350, LOW)]
# the water stair's inlet in the middle of the south edge (the None run above is the notch, |x| < 3.4 between its
# cheek stones): a sill and a spout stone pour into a sunken basin on the paving just inside
NOTCH = 3.0                                   # half width of the notch (the stair's channel bottom is 3 m half width)
BASIN = (4.0, -60.2, -57.5, -8.3)             # half width, south and north inner edges, floor (water at -7.6)
SILL = -6.95                                  # the weir's top: just under the stair's water (-7.0 outside)
BASIN_RIM = (4.5, -60.95, -57.0)              # the rim's outer half width, south and north edges (flush, z -6.0)
LIP = [(30.0, 55.4), (30.0, -30.0), (-30.0, -30.0), (-30.0, 55.4)]
HORSESHOE = [(44, -62), (44, 56), (30, 56), (30, -30), (-30, -30), (-30, 56), (-44, 56), (-44, -62)]
OUTSIDE = [(44, -62), (44, 56), (30, 56), (30, 30), (-30, 30), (-30, 56), (-44, 56), (-44, -62)]


# ------------------------------------------------------------------------------------------------ atlas mapping
def px(x, y):
    return (x / S, 1.0 - y / S)


def wall_uv(s, z, k, plain=False):
    """A vertical face's uv: s metres along it (left to right seen from the front), z height, k its 6 m band."""
    row = (L.WALL_TOP[0] if (k == 0 and not plain) else L.WALL[0]) + L.GUT
    m = (L.WALL_TOP_Z - k * L.BAND) - z
    return ((s + (0 if k == 0 else k * L.BAND_SHIFT)) / L.PERIOD, 1.0 - (row + m * PPM) / S)


def row_uv(name, s, t):
    """A coping profile face: s metres along, t metres down its row."""
    y0, m = L.ROWS[name]
    return (s / L.PERIOD, 1.0 - (y0 + t * PPM) / S)


def pave_uv(x, y, k, shift=0.0):
    """Paving: x east along U, y north; k the 6 m row the face sits in (y in [6k, 6k + 6])."""
    m = (k + 1) * 6.0 - y
    return ((x + shift) / L.PERIOD, 1.0 - (L.PAVE[0] + L.GUT + m * PPM) / S)


def patch_uv(rect, a, b, margin=0.25):
    x0, y0, w, h = rect
    return px(x0 + (margin + a) * PPM, y0 + (margin + b) * PPM)


# vertex colour: the CALM rule 1 gradient, light at the top, darker and a little cooler at the foot
def tint(g, cool):
    return (g * (1 - 0.04 * cool), g * (1 - 0.02 * cool), g * (1 + 0.02 * cool), 1.0)


def g_wall(z):
    t = min(1.0, max(0.0, (L.WALL_TOP_Z - z) / 54.0))
    return tint(1.03 - 0.15 * t, t)


def g_retain(z):
    t = min(1.0, max(0.0, (z - Z_PAVE) / 12.2))
    return tint(0.92 + 0.12 * t, 1 - t)


def g_const(v, cool=0.0):
    return lambda z: tint(v, cool)


# ------------------------------------------------------------------------------------------------ mesh builder
class MB:
    """Faces with their own corners per island (a block's corners are its own: islands never weld to each other)."""

    def __init__(self, name):
        self.name = name
        self.v, self.f, self.uv, self.col = [], [], [], []
        self.island = 0
        self.keys = {}

    def new_island(self):
        self.island += 1

    def vert(self, p):
        k = (self.island, round(p[0], 4), round(p[1], 4), round(p[2], 4))
        i = self.keys.get(k)
        if i is None:
            i = self.keys[k] = len(self.v)
            self.v.append(tuple(p))
        return i

    def face(self, pts, uvs, cols):
        idx = [self.vert(p) for p in pts]
        if len(set(idx)) < 3:
            return
        self.f.append(idx)
        self.uv.append(list(uvs))
        self.col.append(list(cols))

    def tris(self):
        return sum(len(f) - 2 for f in self.f)

    def obj(self, mat, bevel=0.0, segments=2):
        me = bpy.data.meshes.new(self.name)
        me.from_pydata(self.v, [], self.f)
        me.uv_layers.new(name="UVMap")
        me.color_attributes.new("Col", "BYTE_COLOR", "CORNER")
        uvl, ca = me.uv_layers["UVMap"], me.color_attributes["Col"]
        i = 0
        for fi, poly in enumerate(me.polygons):
            for k in range(len(self.f[fi])):
                uvl.data[i].uv = self.uv[fi][k]
                ca.data[i].color = self.col[fi][k]
                i += 1
        me.color_attributes.active_color = ca
        me.color_attributes.render_color_index = 0
        me.materials.append(mat)
        me.validate()
        me.update()
        o = bpy.data.objects.new(self.name, me)
        bpy.context.scene.collection.objects.link(o)
        if bevel > 0:
            mod = o.modifiers.new("Bevel", "BEVEL")
            mod.width, mod.segments, mod.limit_method, mod.angle_limit = bevel, segments, "ANGLE", math.radians(40)
            mod.harden_normals = True
            mod.miter_outer = "MITER_ARC"
            me.polygons.foreach_set("use_smooth", [True] * len(me.polygons))
        return o


def V2(p):
    return Vector((p[0], p[1]))


def normal2(a, b):
    d = (V2(b) - V2(a)).normalized()
    return Vector((d.y, -d.x))


def offset_poly(pts, d, closed=False):
    """The polyline pushed d metres outward (to the right of travel), mitred at every inside vertex."""
    n = len(pts)
    out = []
    for i in range(n):
        if closed or 0 < i < n - 1:
            a, b, c = pts[(i - 1) % n], pts[i], pts[(i + 1) % n]
            n0, n1 = normal2(a, b), normal2(b, c)
            m = (n0 + n1) / (1 + n0.dot(n1))
        else:
            m = normal2(pts[i], pts[i + 1]) if i == 0 else normal2(pts[i - 1], pts[i])
        out.append(V2(pts[i]) + m * d)
    return out


def subpath(pts, sa, sb):
    """The part of an open polyline from arc length sa to sb (points), keeping its corners."""
    out, s = [], 0.0
    for i in range(len(pts) - 1):
        a, b = V2(pts[i]), V2(pts[i + 1])
        ln = (b - a).length
        if s + ln >= sa and s <= sb:
            t0 = max(0.0, (sa - s) / ln)
            t1 = min(1.0, (sb - s) / ln)
            p0, p1 = a.lerp(b, t0), a.lerp(b, t1)
            if not out or (out[-1] - p0).length > 1e-6:
                out.append(p0)
            if (out[-1] - p1).length > 1e-6:
                out.append(p1)
        s += ln
    return [tuple(p) for p in out]


def path_at(pts, s):
    """Point, direction and outward normal at arc length s along an open polyline."""
    acc = 0.0
    for i in range(len(pts) - 1):
        a, b = V2(pts[i]), V2(pts[i + 1])
        ln = (b - a).length
        if s <= acc + ln + 1e-9:
            d = (b - a).normalized()
            return a + d * (s - acc), d, Vector((d.y, -d.x))
        acc += ln
    raise ValueError(s)


# ------------------------------------------------------------------------------------------------ walls
def zbreaks(z_lo, z_hi, extra=()):
    zs = {z_lo, z_hi}
    k = math.floor((L.WALL_TOP_Z - z_hi) / L.BAND)
    while True:
        z = L.WALL_TOP_Z - k * L.BAND
        if z < z_lo - 1e-6:
            break
        if z_lo < z < z_hi:
            zs.add(z)
        k += 1
    for z in (Z_PIT, Z_GORGE, Z_PAVE, Z_WALL_TOP) + tuple(extra):
        if z_lo < z < z_hi:
            zs.add(z)
    return sorted(zs)


def wall_seg(mb, a, b, z_lo, z_hi, s0, inward=False, openings=(), colf=g_wall, zextra=(), plain=False):
    """One straight vertical face from a to b (2D), outward to the right of a -> b (or facing the other way when
    inward), cut into cells at the 6 m bands and round rectangular openings (t0, t1, z0, z1) in metres along a -> b."""
    A, B = V2(a), V2(b)
    ln = (B - A).length
    d = (B - A) / ln
    ts = {0.0, ln}
    ez = []
    for (t0, t1, z0, z1) in openings:
        ts.update((t0, t1)); ez += [z0, z1]
    ts = sorted(ts)
    zs = zbreaks(z_lo, z_hi, tuple(ez) + tuple(zextra))
    for i in range(len(ts) - 1):
        for j in range(len(zs) - 1):
            t0, t1, z0, z1 = ts[i], ts[i + 1], zs[j], zs[j + 1]
            tm, zm = (t0 + t1) / 2, (z0 + z1) / 2
            if any(o[0] < tm < o[1] and o[2] < zm < o[3] for o in openings):
                continue
            k = L.wall_band(zm)
            corners = [(t0, z0), (t1, z0), (t1, z1), (t0, z1)]
            if inward:
                corners = corners[::-1]
            pts = [(A.x + d.x * t, A.y + d.y * t, z) for t, z in corners]
            uvs = [wall_uv((-(s0 + t)) if inward else (s0 + t), z, k, plain) for t, z in corners]
            mb.face(pts, uvs, [colf(z) for _, z in corners])


def wall_poly(mb, pts, z_lo, z_hi, s0=0.0, inward=False, colf=g_wall, closed=False):
    s = s0
    n = len(pts)
    for i in range(n if closed else n - 1):
        a, b = pts[i], pts[(i + 1) % n]
        wall_seg(mb, a, b, z_lo, z_hi, s, inward, colf=colf)
        s += (V2(b) - V2(a)).length


# ------------------------------------------------------------------------------------------------ sweeps
def sweep(mb, pts, closed, profile, s0=0.0, colf=g_const(1.03)):
    """A band swept along a polyline, mitred at every corner. profile: [(d0, z0), (d1, z1), row, (nd, nz)] per face:
    the face from (d0, z0) to (d1, z1) of the cross section (d outward, z up), painted on `row` (a ROWS name or a
    (y0 px, metres) pair), facing (nd, nz)."""
    n = len(pts)
    segs = n if closed else n - 1
    for (p0, p1, row, (nd, nz)) in profile:
        o0, o1 = offset_poly(pts, p0[0], closed), offset_poly(pts, p1[0], closed)
        s_a = s_b = s0
        y0, m = L.ROWS[row] if isinstance(row, str) else row
        width = math.hypot(p1[0] - p0[0], p1[1] - p0[1])
        for i in range(segs):
            j = (i + 1) % n
            la, lb = (o0[j] - o0[i]).length, (o1[j] - o1[i]).length
            q = [(o0[i].x, o0[i].y, p0[1]), (o0[j].x, o0[j].y, p0[1]), (o1[j].x, o1[j].y, p1[1]), (o1[i].x, o1[i].y, p1[1])]
            uv = [(s_a / L.PERIOD, 1 - y0 / S), ((s_a + la) / L.PERIOD, 1 - y0 / S),
                  ((s_b + lb) / L.PERIOD, 1 - (y0 + width * PPM) / S), (s_b / L.PERIOD, 1 - (y0 + width * PPM) / S)]
            # facing: compare the quad's normal with the profile's
            nrm = (Vector(q[1]) - Vector(q[0])).cross(Vector(q[3]) - Vector(q[0]))
            seg_n = normal2(pts[i], pts[j])
            want = Vector((seg_n.x * nd, seg_n.y * nd, nz))
            cols = [colf(c[2]) for c in q]
            if nrm.dot(want) < 0:
                q, uv, cols = q[::-1], uv[::-1], cols[::-1]
            mb.face(q, uv, cols)
            s_a += la; s_b += lb


# ------------------------------------------------------------------------------------------------ prisms
def prism(mb, ring, z0, z1, sides="plain", colf=g_const(1.0), front=None, cap_rect=L.PLAIN, s_off=0.0):
    """A closed upright prism (its own corners): ring CCW seen from above. sides 'plain' maps the side faces onto the
    plain patch, 'wall' onto the wall rows (tall piers). front = (edge index, patch rect): that side wears a patch."""
    mb.new_island()
    n = len(ring)
    area = sum(ring[i][0] * ring[(i + 1) % n][1] - ring[(i + 1) % n][0] * ring[i][1] for i in range(n))
    if area < 0:
        ring = ring[::-1]
        if front:
            front = (n - 2 - front[0], front[1])
    xs, ys = [p[0] for p in ring], [p[1] for p in ring]
    mx, my = min(xs), min(ys)
    top = [(p[0], p[1], z1) for p in ring]
    mb.face(top, [patch_uv(cap_rect, p[0] - mx, p[1] - my) for p in ring], [colf(z1)] * n)
    bot = top[::-1]
    mb.face([(p[0], p[1], z0) for p in bot], [patch_uv(cap_rect, p[0] - mx, p[1] - my) for p in bot], [colf(z0)] * n)
    s = s_off
    for i in range(n):
        a, b = ring[i], ring[(i + 1) % n]
        ln = (V2(b) - V2(a)).length
        if front and front[0] == i:
            x0, y0, w, h = front[1]
            corners = [(0, z0), (ln, z0), (ln, z1), (0, z1)]
            pts = [(a[0] + (b[0] - a[0]) * t / ln, a[1] + (b[1] - a[1]) * t / ln, z) for t, z in corners]
            uvs = [px(x0 + t / ln * w, y0 + (z1 - z) / (z1 - z0) * h) for t, z in corners]
            mb.face(pts, uvs, [colf(z) for _, z in corners])
        elif sides == "wall":
            wall_seg(mb, a, b, z0, z1, s, colf=colf)
        else:
            corners = [(0, z0), (ln, z0), (ln, z1), (0, z1)]
            pts = [(a[0] + (b[0] - a[0]) * t / ln, a[1] + (b[1] - a[1]) * t / ln, z) for t, z in corners]
            mb.face(pts, [patch_uv(L.PLAIN, (s - s_off) % 4.0 + t, z1 - z) for t, z in corners], [colf(z) for _, z in corners])
        s += ln


def obox(origin, along, out, a0, a1, d0, d1):
    """The ring of a box laid along a wall: `along` and `out` are unit 2D axes from `origin`."""
    O, A, N = V2(origin), V2(along), V2(out)
    return [tuple(O + A * a + N * d) for a, d in ((a0, d0), (a1, d0), (a1, d1), (a0, d1))]


# ------------------------------------------------------------------------------------------------ the parts
def build_shell(mb):
    """The horseshoe's walls: outer faces down to -60, the pit to its floor, the gorge to its floor. One shell."""
    n = len(HORSESHOE)
    s = 0.0
    for i in range(n):
        a, b = HORSESHOE[i], HORSESHOE[(i + 1) % n]
        ln = (V2(b) - V2(a)).length
        outer = a[0] in (44, -44) and b[0] in (44, -44) or a[1] == b[1] == 56 or a[1] == b[1] == -62
        if outer:
            op = []
            if a[1] == b[1] == -62:                              # the inlet's slot through the south wall's top
                op = [(-a[0] - NOTCH, -a[0] + NOTCH, SILL, Z_WALL_TOP)] if a[0] < 0 else [(a[0] - NOTCH, a[0] + NOTCH, SILL, Z_WALL_TOP)]
            wall_seg(mb, a, b, Z_FOOT, Z_WALL_TOP, s, openings=op)
        elif a[0] == b[0]:                                      # x = +-30: the pit part and the gorge part
            ya, yb = a[1], b[1]
            cut = Y_PIT_N
            # split at y 30: the gorge (to -46.4) and the pit (to -30)
            pa, pc, pb = V2(a), Vector((a[0], cut)), V2(b)
            l1 = (pc - pa).length
            if ya > cut:                                         # east side, running south: gorge first
                wall_seg(mb, a, tuple(pc), Z_GORGE, Z_WALL_TOP, s)
                wall_seg(mb, tuple(pc), b, Z_PIT, Z_WALL_TOP, s + l1)
            else:                                                # west side, running north: pit first, niches
                op = [(c - 1.3 + 30, c + 1.3 + 30, -19.4, -14.6) for c in NICHES]
                wall_seg(mb, a, tuple(pc), Z_PIT, Z_WALL_TOP, s, openings=op)
                wall_seg(mb, tuple(pc), b, Z_GORGE, Z_WALL_TOP, s + l1)
        else:                                                    # y = -30, the pit's south wall
            wall_seg(mb, a, b, Z_PIT, Z_WALL_TOP, s)
        s += ln


NICHES = (-14.0, 0.0, 14.0)
SPOUTS = (-12.0, 12.0)


def build_niches(shell, boxes):
    """Three deep niches in the pit's west wall (x = -30, facing east): dark jambs, a dusty rose back with one
    turquoise diamond; a stone surround (jambs, a sill, a stepped lintel) proud of the wall."""
    x, depth = -X_PIT, 1.4
    z0, z1 = -19.4, -14.6
    dark = g_const(0.80, 0.4)
    for c in NICHES:
        y0, y1 = c - 1.3, c + 1.3
        xb = x - depth
        # back (facing +x): the patch, the viewer's right is -y
        nb = L.NICHE_BACK
        q = [(xb, y0, z0), (xb, y1, z0), (xb, y1, z1), (xb, y0, z1)]
        uv = [px(nb[0], nb[1] + nb[3]), px(nb[0] + nb[2], nb[1] + nb[3]), px(nb[0] + nb[2], nb[1]), px(nb[0], nb[1])]
        shell.face(q, uv, [dark(0)] * 4)
        D = L.DARK
        # jambs (a: depth, b: down), soffit and sill (a: depth, b: along): on the dark plain patch
        jambs = [[(x, y0, z0), (xb, y0, z0), (xb, y0, z1), (x, y0, z1)],          # south jamb faces +y
                 [(xb, y1, z0), (x, y1, z0), (x, y1, z1), (xb, y1, z1)]]          # north jamb faces -y
        flats = [[(x, y0, z1), (xb, y0, z1), (xb, y1, z1), (x, y1, z1)],          # soffit faces down
                 [(x, y1, z0), (xb, y1, z0), (xb, y0, z0), (x, y0, z0)]]          # sill faces up
        for q in jambs:
            shell.face(q, [patch_uv(D, p[0] - xb, z1 - p[2]) for p in q], [dark(0)] * 4)
        for q in flats:
            shell.face(q, [patch_uv(D, p[0] - xb, p[1] - y0) for p in q], [dark(0)] * 4)
        # the surround: proud 0.14, its backs buried 0.1 in the wall
        e, o = (0.0, 1.0), (1.0, 0.0)                                         # along +y, out +x (into the pit)
        wall_x = (x, 0.0)
        ring = lambda a0, a1, d: obox(wall_x, e, o, a0, a1, -0.1, d)
        frame = g_const(1.02)
        prism(boxes, ring(y0 - 0.45, y0, 0.14), z0 - 0.05, z1 + 0.05, colf=frame)
        prism(boxes, ring(y1, y1 + 0.45, 0.14), z0 - 0.05, z1 + 0.05, colf=frame)
        prism(boxes, ring(y0 - 0.65, y1 + 0.65, 0.24), z0 - 0.45, z0 - 0.02, colf=frame)        # the sill, deeper
        prism(boxes, ring(y0 - 0.65, y1 + 0.65, 0.18), z1 + 0.02, z1 + 0.55, colf=frame)        # the lintel
        prism(boxes, ring(y0 - 0.15, y1 + 0.15, 0.12), z1 + 0.53, z1 + 0.95, colf=frame)        # its step


def build_spouts(boxes):
    """Two water spouts in the pit's east wall (x = 30, facing west): a carved disc panel, a trough on a corbel."""
    x = X_PIT
    e, o = (0.0, -1.0), (-1.0, 0.0)                                           # along -y (viewer's right), out -x
    for c in SPOUTS:
        org = (x, -c)                                                         # along -y: a = -(y) - (-c)... use origin at y = c
        org = (x, c)
        ring = lambda a0, a1, d0, d: obox(org, e, o, a0, a1, d0, d)
        st = g_const(1.0)
        # the panel: front face (the edge from (a1, d) to (a0, d)) wears the spout patch
        rg = ring(-1.3, 1.3, -0.1, 0.16)
        prism(boxes, rg, -20.6, -18.0, colf=st, front=(2, L.SPOUT_PANEL))
        # the trough: a floor and two cheeks, 1.8 m out
        prism(boxes, ring(-0.5, 0.5, -0.1, 1.8), -21.3, -21.0, colf=st)
        prism(boxes, ring(-0.5, -0.33, -0.1, 1.8), -21.0, -20.62, colf=st)
        prism(boxes, ring(0.33, 0.5, -0.1, 1.8), -21.0, -20.62, colf=st)
        # the corbel under it, stepped
        prism(boxes, ring(-0.38, 0.38, -0.1, 1.1), -21.75, -21.28, colf=st)
        prism(boxes, ring(-0.3, 0.3, -0.1, 0.6), -22.2, -21.73, colf=st)


def build_floors(mb):
    """The pit floor (-30, its north face to the gorge with the recess) and the gorge floor (-46.4) with its sill."""
    rw, rtop, rd = RECESS
    # pit floor top, 6 m rows
    xs = (-30.0, -rw, rw, 30.0)                                # split where the recess edges meet the floor's edge
    for k in range(-5, 5):
        y0, y1 = k * 6.0, k * 6.0 + 6.0
        for x0, x1 in zip(xs, xs[1:]):
            q = [(x0, y0, Z_PIT), (x1, y0, Z_PIT), (x1, y1, Z_PIT), (x0, y1, Z_PIT)]
            mb.face(q, [pave_uv(p[0], p[1], k, 11.3) for p in q], [tint(0.95, 0.2)] * 4)
    # its north face (y = 30, facing the gorge), the recess cut in at the bottom
    wall_seg(mb, (30, 30), (-30, 30), Z_GORGE, Z_PIT, 3.1, openings=[(30 - rw, 30 + rw, Z_GORGE, rtop)], zextra=(STRING[0], STRING[1]))
    # the recess: back, sides, soffit
    yb = Y_PIT_N - rd
    dk = lambda z: tint(0.80, 0.5)
    wall_seg(mb, (rw, yb), (-rw, yb), Z_GORGE, rtop, 3.1 + 30 - rw, colf=dk)
    wall_seg(mb, (rw, Y_PIT_N), (rw, yb), Z_GORGE, rtop, 1.0, colf=dk)
    wall_seg(mb, (-rw, yb), (-rw, Y_PIT_N), Z_GORGE, rtop, 7.0, colf=dk)
    q = [(-rw, yb, rtop), (-rw, Y_PIT_N, rtop), (rw, Y_PIT_N, rtop), (rw, yb, rtop)]          # facing down
    mb.face(q, [pave_uv(p[0], p[1] - yb, 0, 5.0) for p in q], [tint(0.74, 0.6)] * 4)
    # gorge floor (under the water): rows of 6 m, and the recess floor
    ys = [Y_PIT_N, 36.0, 42.0, 48.0, 54.0, Y_FRONT]
    for i in range(len(ys) - 1):
        y0, y1 = ys[i], ys[i + 1]
        k = math.floor((y0 + 1e-6) / 6.0)
        cut = xs if i == 0 else (-30.0, 30.0)                  # only the row that meets the recess floor is split
        for x0, x1 in zip(cut, cut[1:]):
            q = [(x0, y0, Z_GORGE), (x1, y0, Z_GORGE), (x1, y1, Z_GORGE), (x0, y1, Z_GORGE)]
            mb.face(q, [pave_uv(p[0], p[1], k, 3.7) for p in q], [tint(0.86, 0.5)] * 4)
    # the recess floor, split where the gorge floor's edge is so the two weld without a T
    q = [(-rw, yb, Z_GORGE), (rw, yb, Z_GORGE), (rw, Y_PIT_N, Z_GORGE), (-rw, Y_PIT_N, Z_GORGE)]
    mb.face(q, [pave_uv(p[0], p[1], 4, 3.7) for p in q], [tint(0.80, 0.5)] * 4)
    # the sill: the gorge floor's front face at the mouth, down to the foot
    wall_seg(mb, (30, Y_FRONT), (-30, Y_FRONT), Z_FOOT, Z_GORGE, 14.0)


def build_paving(mb):
    """The paving field inside the border courses, rows of 6 m."""
    ox, oy, ix, iy = X_OUT - 1.85, Y_BACK + 1.85, X_PIT + 1.65, Y_PIT_S - 1.65
    fy = Y_FRONT - 1.85
    bx, by0, by1 = BASIN_RIM
    rects = [(-ox, -bx, oy, iy), (bx, ox, oy, iy), (-bx, bx, by1, iy), (ix, ox, iy, fy), (-ox, -ix, iy, fy)]
    for (x0, x1, y0, y1) in rects:
        ys = sorted({y0, y1} | {6.0 * k for k in range(-12, 12) if y0 < 6.0 * k < y1})
        for i in range(len(ys) - 1):
            a, b = ys[i], ys[i + 1]
            k = math.floor((a + 1e-6) / 6.0)
            q = [(x0, a, Z_PAVE), (x1, a, Z_PAVE), (x1, b, Z_PAVE), (x0, b, Z_PAVE)]
            mb.face(q, [pave_uv(p[0], p[1], k) for p in q], [tint(1.0, 0)] * 4)
    # the border courses along the copings (paving level, own row)
    sb0, sb1 = 131 + 44 - BASIN_RIM[0], 131 + 44 + BASIN_RIM[0]          # the basin's rim takes the border's place
    for (a, b) in ((0, sb0), (sb1, 350)):
        sweep(mb, subpath(PARA, a, b), False, [((-1.85, Z_PAVE), (-1.05, Z_PAVE), "border", (0, 1))], a, colf=g_const(1.0))
    sweep(mb, LIP, False, [((-1.65, Z_PAVE), (-0.85, Z_PAVE), "border", (0, 1))], 3.3, colf=g_const(1.0))


def build_inlet(mb, boxes):
    """The water stair's inlet. The stair's water (-7.0 outside) spills over a 6 m stone weir (top -6.95) through a
    slot in the south wall's top, past a central spout trough, and falls into a sunken pool 1.6 m below the paving
    (water -7.6, floor -8.3, coursed walls, rim flush with the paving at -6.0). Cheek stones close the parapet either
    side, each with a carved water disc facing the basin and the gate. The pool is left without water."""
    wx, wy0, wy1, wf = BASIN
    rx, ry0, ry1 = BASIN_RIM
    for sx in (1, -1):
        x0, x1 = sorted((sx * NOTCH, sx * (NOTCH + 1.2)))
        prism(boxes, [(x0, -62.3), (x1, -62.3), (x1, -60.65), (x0, -60.65)], Z_WALL_TOP - 0.05, -4.95, colf=g_const(1.02))
        prism(boxes, [(x0 - 0.12, -62.42), (x1 + 0.12, -62.42), (x1 + 0.12, -60.53), (x0 - 0.12, -60.53)], -5.0, -4.7,
              colf=g_const(1.04))
        xc = sx * (NOTCH + 0.6)
        ring = [(xc - 0.48, -60.75), (xc + 0.48, -60.75), (xc + 0.48, -60.55), (xc - 0.48, -60.55)]
        prism(boxes, ring, -5.95, -4.99, colf=g_const(1.02), front=(2, L.SPOUT_PANEL))
    # the weir: one stone through the slot, a lip proud of the outer face and 5 cm over the pool
    prism(boxes, [(-NOTCH - 0.05, -62.25), (NOTCH + 0.05, -62.25), (NOTCH + 0.05, wy0 + 0.05), (-NOTCH - 0.05, wy0 + 0.05)],
          -7.4, SILL, colf=g_const(1.0))
    # the spout trough over the middle of the weir, a hair lower so the main stream runs through it; a corbel
    prism(boxes, [(-0.7, -60.6), (0.7, -60.6), (0.7, -59.55), (-0.7, -59.55)], -7.17, -6.97, colf=g_const(1.02))
    for a, b in ((-0.7, -0.52), (0.52, 0.7)):
        prism(boxes, [(a, -60.6), (b, -60.6), (b, -59.55), (a, -59.55)], -6.99, -6.69, colf=g_const(1.02))
    prism(boxes, [(-0.5, -60.6), (0.5, -60.6), (0.5, -59.85), (-0.5, -59.85)], -7.6, -7.15, colf=g_const(0.98))
    mb.new_island()
    # the slot's side walls (facing the slot), under the cheeks and, past them, up to the paving
    for sx in (1, -1):
        x = sx * NOTCH
        for (ya, yb, top) in ((-62.0, -60.65, Z_WALL_TOP - 0.05), (-60.65, wy0, Z_PAVE)):
            a, b = ((x, yb), (x, ya)) if sx > 0 else ((x, ya), (x, yb))
            wall_seg(mb, a, b, SILL - 0.05, top, 11.0, colf=g_const(0.9, 0.3), plain=True)
    # the rim, flush with the paving, open where the weir comes in
    y0 = L.ROWS["border"][0]

    def flat(x0, x1, ya, yb, along_x):
        q = [(x0, ya, Z_PAVE), (x1, ya, Z_PAVE), (x1, yb, Z_PAVE), (x0, yb, Z_PAVE)]
        if along_x:
            uv = [(p[0] / L.PERIOD, 1 - (y0 + (yb - p[1]) * PPM) / S) for p in q]
        else:
            uv = [(p[1] / L.PERIOD, 1 - (y0 + (abs(p[0]) - wx) * PPM) / S) for p in q]
        mb.face(q, uv, [tint(1.0, 0)] * 4)
    flat(-rx, rx, wy1, ry1, True)                                    # north
    for ya, yb in ((ry0, wy0), (wy0, wy1)):                            # west, east (split where the walls start)
        flat(-rx, -wx, ya, yb, False); flat(wx, rx, ya, yb, False)
    flat(-wx, -NOTCH, ry0, wy0, True); flat(NOTCH, wx, ry0, wy0, True)
    # the pool's coursed walls (facing the water) and its floor
    cool = lambda z: tint(0.84 + 0.10 * min(1.0, max(0.0, (z - wf) / (Z_PAVE - wf))), 0.4)
    wall_seg(mb, (-wx, wy0), (-wx, wy1), wf, Z_PAVE, 2.0, colf=cool, plain=True)            # west wall faces +x
    wall_seg(mb, (wx, wy1), (wx, wy0), wf, Z_PAVE, 9.0, colf=cool, plain=True)              # east wall faces -x
    wall_seg(mb, (-wx, wy1), (wx, wy1), wf, Z_PAVE, 4.0, colf=cool, plain=True)             # north wall faces -y
    wall_seg(mb, (-NOTCH, wy0), (-wx, wy0), wf, Z_PAVE, 13.0, colf=cool, plain=True)        # south wall (faces +y), beside the weir
    wall_seg(mb, (wx, wy0), (NOTCH, wy0), wf, Z_PAVE, 16.0, colf=cool, plain=True)
    wall_seg(mb, (NOTCH, wy0), (-NOTCH, wy0), wf, -7.4, 14.0, colf=cool, plain=True)        # under the weir
    q = [(-wx, wy0, wf), (wx, wy0, wf), (wx, wy1, wf), (-wx, wy1, wf)]
    mb.face(q, [pave_uv(p[0], p[1] + 60.2, 0, 2.0) for p in q], [tint(0.78, 0.5)] * 4)


def build_copings(cop, body, boxes):
    """The outer coping in runs of constant top (the retaining walls step down the hill), piers at the steps and at
    the gorge mouth, the pit's rim lip, the glyph panels on the tall retaining walls."""
    total = sum((V2(PARA[i + 1]) - V2(PARA[i])).length for i in range(len(PARA) - 1))
    for (sa, sb, T) in RUNS:
        if T is None:
            continue
        pts = subpath(PARA, sa, sb)
        cb = T - CAP_T
        sweep(cop, pts, False, [
            ((-1.05, cb), (-1.2, cb), "cap_under_in", (0, -1)),
            ((-1.2, cb), (-1.2, T), "cap_in", (-1, 0)),
            ((-1.2, T), (0.15, T), "cap_top", (0, 1)),
            ((0.15, T), (0.15, cb), "cap_out", (1, 0)),
            ((0.15, cb), (0.0, cb), "cap_under_out", (0, -1)),
        ], sa, colf=g_const(1.03))
        # the body under the cap: its inner face (seen from the paving) and its outer face (mostly in the land)
        inner = offset_poly(pts, -1.05)
        wall_poly(body, [tuple(p) for p in inner], Z_PAVE, cb, -sa, inward=True, colf=g_retain)
        wall_poly(body, pts, Z_WALL_TOP, cb, sa, colf=g_wall)
        if T > 6.0:                                            # the tall retaining walls carry the carved course too
            wt = L.WALL_TOP[0] + L.GUT
            sweep(cop, [tuple(p) for p in inner], False, [
                ((0.0, 4.5), (-0.12, 4.5), "string", (0, 1)),
                ((-0.12, 4.5), (-0.12, 3.3), (wt + L.FRET[0] * PPM, 1.2), (-1, 0)),
                ((-0.12, 3.3), (0.0, 3.3), "string", (0, -1)),
            ], sa, colf=g_const(1.03))
    # piers at every step: base on the paving, a cap over the higher coping
    for i in range(len(RUNS) - 1):
        if RUNS[i][2] is None or RUNS[i + 1][2] is None:
            continue
        s = RUNS[i][1]
        hi = max(RUNS[i][2], RUNS[i + 1][2])
        p, d, nrm = path_at(PARA, s)
        prism(boxes, obox(p, d, nrm, -0.9, 0.9, -1.4, 0.3), Z_WALL_TOP - 0.05, hi + 0.15, sides="wall", colf=g_retain, s_off=s)
        prism(boxes, obox(p, d, nrm, -1.05, 1.05, -1.55, 0.45), hi + 0.1, hi + 0.45, colf=g_const(1.04))
    # the gorge mouth piers: where the low parapet meets the rim lip
    for sx in (1, -1):
        ring = [(sx * 29.75, 53.9), (sx * 32.0, 53.9), (sx * 32.0, 56.25), (sx * 29.75, 56.25)]
        prism(boxes, ring, Z_WALL_TOP - 0.05, -4.85, colf=g_const(1.02))
        ring = [(sx * 29.6, 53.75), (sx * 32.15, 53.75), (sx * 32.15, 56.4), (sx * 29.6, 56.4)]
        prism(boxes, ring, -4.9, -4.55, colf=g_const(1.04))
    # the pit's rim lip
    sweep(cop, LIP, False, [
        ((-0.85, Z_PAVE), (-0.85, -5.65), "lip_in", (-1, 0)),
        ((-0.85, -5.65), (0.2, -5.65), "lip_top", (0, 1)),
        ((0.2, -5.65), (0.2, Z_WALL_TOP), "lip_out", (1, 0)),
        ((0.2, Z_WALL_TOP), (0.0, Z_WALL_TOP), "lip_under", (0, -1)),
    ], 1.7, colf=g_const(1.02))
    # glyph panels on the tall retaining walls (inner faces), one bold sun each
    for s in (101.0, 121.0, 137.5, 211.5, 227.0, 247.0):
        p, d, nrm = path_at(PARA, s)
        rg = obox(p, d, nrm, -1.3, 1.3, -1.2, -0.95)
        # the face at d = -1.2 is the ring's edge 2 (from (a1, d0) back to (a0, d0)) after CCW sorting: find it
        prism(boxes, rg, -1.9, 0.7, colf=g_const(1.02), front=(_face_towards(rg, -nrm), L.GLYPH_PANEL))


def _face_towards(ring, n):
    """The index of the ring edge (CCW order as prism will use it) whose outward normal is n."""
    r = ring
    area = sum(r[i][0] * r[(i + 1) % 4][1] - r[(i + 1) % 4][0] * r[i][1] for i in range(4))
    if area < 0:
        r = r[::-1]
    best = max(range(4), key=lambda i: normal2(r[i], r[(i + 1) % 4]).dot(n))
    # prism re-reverses `front` for a CW ring: hand it the index in the ring's own order
    return best if area >= 0 else 4 - 2 - best


def build_bands(fret, string):
    """The carved fret course round every wall (inside and out) and the pale string course round the outside."""
    t, b, p = FRET
    wt = L.WALL_TOP[0] + L.GUT
    sweep(fret, HORSESHOE, True, [
        ((0.0, t), (p, t), "string", (0, 1)),
        ((p, t), (p, b), (wt + L.FRET[0] * PPM, 1.2), (1, 0)),
        ((p, b), (0.0, b), "string", (0, -1)),
    ], colf=g_const(1.03))
    t, b, p = STRING
    sweep(string, OUTSIDE, True, [
        ((0.0, t), (p, t), (L.ROWS["string"][0], 0.1), (0, 1)),
        ((p, t), (p, b), (L.ROWS["string"][0] + 0.1 * PPM, 0.6), (1, 0)),
        ((p, b), (0.0, b), (L.ROWS["string"][0] + 0.7 * PPM, 0.1), (0, -1)),
    ], colf=lambda z: g_wall(z))


def build_quoins(mb):
    """Quoins on every outside corner: L-shaped blocks, long and short legs alternating course by course, proud of
    both faces; none where a band crosses the corner."""
    corners = [((44, -62), 5.7), ((44, 56), Z_WALL_TOP), ((30, 56), Z_WALL_TOP), ((-30, 56), Z_WALL_TOP),
               ((-44, 56), Z_WALL_TOP), ((-44, -62), 5.7)]
    n = len(HORSESHOE)
    for (c, top) in corners:
        i = HORSESHOE.index(c)
        prev, nxt = HORSESHOE[(i - 1) % n], HORSESHOE[(i + 1) % n]
        a = (V2(prev) - V2(c)).normalized()          # along the incoming face, away from the corner
        b = (V2(nxt) - V2(c)).normalized()
        foot = -44.3 if abs(c[0]) == 30 else Z_FOOT         # the gorge mouth corners stop at the pool rim (pool_build.py)
        C = V2(c)
        k = 0
        z = top
        while z > foot + 0.3:
            zt, zb = z - 0.01, max(foot, z - L.COURSE) + (0.01 if z - L.COURSE > foot else 0.0)
            blocked = any(not (zb >= bt or zt <= bb) for bt, bb in ((FRET[0] + 0.02, FRET[1] - 0.02), (STRING[0] + 0.02, STRING[1] - 0.02)))
            if abs(z - Z_WALL_TOP) < 1e-6:
                zt = Z_WALL_TOP - 0.02                      # clear of the coping's underside
            if not blocked:
                l1, l2 = (1.5, 0.75) if k % 2 == 0 else (0.75, 1.5)
                p, t = QUOIN_P, QUOIN_T
                ab = [(-p, -p), (l1, -p), (l1, t), (t, t), (t, l2), (-p, l2)]
                ring = [tuple(C + a * al + b * be) for al, be in ab]
                prism(mb, ring, zb, zt, colf=lambda zz: tint(g_wall(zz)[0] * 1.03, 0.3))
            k += 1
            z -= L.COURSE


# ------------------------------------------------------------------------------------------------ material, export
def material(with_normal):
    m = bpy.data.materials.get(MAT) or bpy.data.materials.new(MAT)
    m.use_nodes = True
    nt = m.node_tree
    for nd in list(nt.nodes):
        if nd.type != "OUTPUT_MATERIAL":
            nt.nodes.remove(nd)
    out = next(nd for nd in nt.nodes if nd.type == "OUTPUT_MATERIAL")
    b = nt.nodes.new("ShaderNodeBsdfPrincipled")
    nt.links.new(b.outputs[0], out.inputs[0])
    b.inputs["Roughness"].default_value = 0.92
    m.use_backface_culling = True                     # one sided (glTF doubleSided false)
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(ATLAS, check_existing=True)
    tex.extension = "REPEAT"
    if with_normal:
        vc = nt.nodes.new("ShaderNodeVertexColor"); vc.layer_name = "Col"
        mul = nt.nodes.new("ShaderNodeMix"); mul.data_type = "RGBA"; mul.blend_type = "MULTIPLY"
        mul.inputs[0].default_value = 1.0
        nt.links.new(tex.outputs["Color"], mul.inputs[6]); nt.links.new(vc.outputs["Color"], mul.inputs[7])
        nt.links.new(mul.outputs[2], b.inputs["Base Color"])
        ntex = nt.nodes.new("ShaderNodeTexImage")
        ntex.image = bpy.data.images.load(NORMAL, check_existing=True)
        ntex.image.colorspace_settings.name = "Non-Color"
        nm = nt.nodes.new("ShaderNodeNormalMap"); nm.inputs["Strength"].default_value = 1.0
        nt.links.new(ntex.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    else:
        nt.links.new(tex.outputs["Color"], b.inputs["Base Color"])
    return m


def export(objs, path=RAW, name="court_platform", extra=()):
    """Bevels applied on copies, the stone joined into one mesh named `name`; `extra` objects (the water) go along
    as they are."""
    bpy.ops.object.select_all(action="DESELECT")
    dups = []
    for o in objs:
        d = o.copy(); d.data = o.data.copy()
        bpy.context.scene.collection.objects.link(d)
        dups.append(d)
    bpy.context.view_layer.objects.active = dups[0]
    for d in dups:
        d.select_set(True)
    for d in dups:
        bpy.context.view_layer.objects.active = d
        for mod in list(d.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.context.view_layer.objects.active = dups[0]
    for d in dups:
        d.select_set(True)
    bpy.ops.object.join()
    j = bpy.context.view_layer.objects.active
    j.name = j.data.name = name
    me = j.data
    me.color_attributes.active_color = me.color_attributes["Col"]
    tris = sum(len(p.vertices) - 2 for p in me.polygons)
    for o in extra:
        o.select_set(True)
        tris += sum(len(p.vertices) - 2 for p in o.data.polygons)
    bpy.ops.export_scene.gltf(filepath=path, use_selection=True, export_format="GLB", export_yup=True, export_apply=True,
                              export_vertex_color="ACTIVE", export_all_vertex_colors=False, export_tangents=False)
    bpy.data.objects.remove(j, do_unlink=True)
    return tris


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mat = material(False)
    shell, floors, paving, cop, body, boxes, quoins, fret, string = (MB(n) for n in (
        "court_shell", "court_floors", "court_paving", "court_copings", "court_retaining", "court_blocks", "court_quoins",
        "court_fret_course", "court_string_course"))
    build_shell(shell)
    build_niches(shell, boxes)
    build_spouts(boxes)
    build_floors(floors)
    build_paving(paving)
    build_copings(cop, body, boxes)
    build_inlet(floors, boxes)
    build_bands(fret, string)
    build_quoins(quoins)
    objs = [shell.obj(mat, 0.12), floors.obj(mat, 0.12), paving.obj(mat, 0.0), cop.obj(mat, 0.05), body.obj(mat, 0.0),
            boxes.obj(mat, 0.05), quoins.obj(mat, 0.045), fret.obj(mat, 0.04), string.obj(mat, 0.035)]
    for o in objs:
        print("PART %-22s %6d faces" % (o.name, len(o.data.polygons)))
    tris = export(objs)
    material(True)
    os.makedirs(os.path.dirname(BLEND), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)
    print("COURT PLATFORM: %d triangles (bevels applied) -> %s, %s" % (tris, RAW, BLEND))


if __name__ == "__main__":
    main()
