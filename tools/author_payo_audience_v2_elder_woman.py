"""Build the Payo audience v2 villager `elder_woman`: an Ifugao grandmother, modelled the way the cast is.

    py -3 tools/author_payo_audience_v2_elder_woman_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_payo_audience_v2_elder_woman.py
    ... -- --out other.glb     (writes there instead and leaves the .blend alone; needs the atlas beside it)

Writes
    ArtSource/payo/audience/v2/elder_woman/audience_v2_elder_woman.glb    (rigged, every clip)
    ArtSource/payo/audience/v2/elder_woman/audience_v2_elder_woman.blend
beside the atlas painted by the textures script. NOTHING IN THE GAME LOADS IT YET; the Payo map
session imports it.

THE BRIEF: docs/reports/character-redesign/payo-audience-brief.md. The v1 crowd (Blender box figures)
was REJECTED for its construction; each villager is now built EXACTLY like a Classic redesign. This
is A COPY OF tools/author_character_redesign_lola_pacing.py (the approved elder Classic), its block,
loft, paint and rig machinery kept, every piece of her rewritten.

WHO SHE IS. A grandmother of the rice-terrace village watching the match from the basin wall:
content and warm, with her basket beside her. Precolonial Ifugao dress, kept modest:
  * GRAY HAIR, salt and pepper (warm grays; Lola Pacing's is a cool lavender silver), PARTED IN THE
    MIDDLE AND SWEPT BACK in chunky locks over a full crown, the forehead open, over the ears, and
    wound into a LOW, WIDE BUN AT THE NAPE with a BEAD STRING round it and three beads hanging from
    it. Lola Pacing has a fringe, set curls and a high bun with a comb: the opposite silhouette.
  * HER BLANKET, the focal piece: an undyed cream woven blanket over her shoulders and upper arms,
    its folded top edge rolled at her neck, banded at its hem with black and a thin madder red, its
    two ends crossing at her chest (the overlapping banded edge is a strip of geometry) and TIED IN
    A KNOT with two short tails: the one big fastening. A diamond field is woven across its back.
  * A FEW STRANDS OF OLD BEADS over it: bone and black, madder with one brass bead, black and red.
  * HER TAPIS: a wrapped knee-length skirt, a white ground with an indigo band carrying cream
    diamonds, a thin madder band, and an indigo hem standing proud; the wrap's overlapping end is a
    strip of geometry on her left; a woven black and red belt with its two ends hanging at her hip.
  * Under the blanket, a dark indigo wrapped top (only seen when her arms lift the blanket).
  * BRASS: drop earrings and bangles at her wrists (two coils on the left, one on the right).
  * BAREFOOT: proper foot blocks with the toes a slight step.
  * HER BASKET: a lidded rattan storage basket, a SEPARATE MESH (`basket-mesh`) skinned to `root`,
    standing at her left foot. Its foot is on z 0 at (0.315, 0.020); in the `sit` the root stays at
    its rest place and her body comes down to the ledge, so the basket stands on the ledge top
    beside her left hip. In `walk`, `sprint`, `jump` and `fall` it is carried on her left hip (see the
    clips script).

THE RULES IN FORCE (classic-brief.md, section 15.3, the audience brief): HEAD_SCALE 0.84 about the
head joint after the UVs resolve; the heroes' rounded head with ONE FLAT FACE PLANE; ink eye blocks,
one mouth stroke, blush, no nose, nothing of age; hair as chunky layered locks, base, shade and light
by facing, no painted shine; paint only on a face square to its view, flat tones elsewhere; cloth
across two bones bends (the tapis takes the legs, the blanket over her shoulders takes the arms, the
neck takes the head); the rig is the seven bones plus `forearm-left` and `forearm-right`, the fist the
far 14 per cent of the arm with nothing wider past the wrist; feet on zero; under 6,000 triangles.
THE FINISH (owner, 2026-10-19): clean flat tones per form, as the Classics; bands, belt, beads,
hems and the basket's rattan bands are geometry.
"""
import math
import os
import struct
import sys
import time

import bpy
import bmesh
from mathutils import Vector

TOOLS = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(TOOLS)
sys.path.insert(0, TOOLS)
import build_person_voxel as bpv  # noqa: E402  the cast's glb reader and writer, not edited
import author_payo_audience_v2_elder_woman_textures as tex  # noqa: E402  the island layout
import author_payo_audience_v2_elder_woman_clips as clips  # noqa: E402  her clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/character-female-d.glb")
FOLDER = os.path.join(ROOT, "ArtSource/payo/audience/v2/elder_woman")
OUT = os.path.join(FOLDER, "audience_v2_elder_woman.glb")
BLEND = os.path.join(FOLDER, "audience_v2_elder_woman.blend")
NAME = "audience_v2_elder_woman"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
ELBOW_X = tex.ELBOW
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
HEAD_SCALE = 0.84
HEAD_JOINT = Vector((0.0, 0.0024, 0.3432))
HEIGHT_RANGE = (0.6613, 0.7928)   # the cast's
SHARP_ANGLE = 40.0
TRIANGLE_BUDGET = 6000

X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
AHEAD = (0, -1, 0)


# ---------------------------------------------------------------------------
# THE BLOCK (Lola Pacing's, unchanged)
# ---------------------------------------------------------------------------

def _pair(h):
    return h if isinstance(h, tuple) else (h, h)


def rect_ring(centre, u, v, hu, hv, chamfer):
    un, up = _pair(hu)
    vn, vp = _pair(hv)
    c = min(chamfer, 0.45 * (un + up), 0.45 * (vn + vp))
    if c <= 0.0:
        flat_pts = [(up, -vn), (up, vp), (-un, vp), (-un, -vn)]
    else:
        flat_pts = [(up, -vn + c), (up, vp - c), (up - c, vp), (-un + c, vp),
                    (-un, vp - c), (-un, -vn + c), (-un + c, -vn), (up - c, -vn)]
    return [Vector(centre) + u * a + v * b for a, b in flat_pts]


def proj(group):
    return ("proj", group)


def flat(name):
    return ("flat", name)


def proj_square(group, flat_name, under_name, limit=0.93, ends=None):
    return ("square", group, flat_name, under_name, limit, ends)


def tones(top, side, under):
    return ("tones", top, side, under)


def facing(direction, front, other):
    return ("flatfacing", direction, front, other)


class Part:
    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.bone = self.bm.verts.layers.int.new("bone")
        self.jobs = []
        self.pieces = []
        self.blend = {}

    def loft(self, name, bone, rings, mapping, caps=(True, True), tip=None, params=None):
        bm = self.bm
        n = len(rings[0])
        count = len(rings)
        index = ALL_BONES.index(bone)

        def vert(p):
            v = bm.verts.new(p)
            v[self.bone] = index
            return v

        rows = [[vert(p) for p in r] for r in rings]
        faces = []
        spec = mapping if callable(mapping) else (lambda i, j: mapping)
        for i in range(count - 1):
            for j in range(n):
                k = (j + 1) % n
                quad = (rows[i][j], rows[i][k], rows[i + 1][k], rows[i + 1][j])
                f = bm.faces.new(quad)
                self.jobs.append((f, spec(i, j)))
                faces.append(f)

        def fan(row, at, seg):
            centre = vert(at)
            for j in range(n):
                k = (j + 1) % n
                f = bm.faces.new((row[j], row[k], centre))
                self.jobs.append((f, spec(seg, j)))
                faces.append(f)

        if caps[0]:
            fan(rows[0], sum(rings[0], Vector()) / n, 0)
        if tip is not None:
            fan(rows[-1], tip, count - 2)
        elif caps[1]:
            fan(rows[-1], sum(rings[-1], Vector()) / n, count - 2)
        bmesh.ops.recalc_face_normals(bm, faces=faces)
        self.pieces.append((name, sum(len(f.verts) - 2 for f in faces)))
        return faces

    def block(self, name, bone, u, v, stations, mapping, chamfer, move=None, caps=(True, True)):
        rings = []
        first, last = stations[0], stations[-1]
        along = (Vector(last[0]) - Vector(first[0]))
        length = along.length
        along = along.normalized()
        c = min(chamfer, 0.4 * length)

        def shrink(h):
            a, b = _pair(h)
            return (max(a - c, 0.001), max(b - c, 0.001))

        def station(centre, hu, hv, cut):
            return rect_ring(centre, u, v, hu, hv, cut)

        if c > 0.0:
            rings.append(station(first[0], shrink(first[1]), shrink(first[2]), chamfer * 0.4))
            rings.append(station(Vector(first[0]) + along * c, first[1], first[2], chamfer))
            for centre, hu, hv in stations[1:-1]:
                rings.append(station(centre, hu, hv, chamfer))
            rings.append(station(Vector(last[0]) - along * c, last[1], last[2], chamfer))
            rings.append(station(last[0], shrink(last[1]), shrink(last[2]), chamfer * 0.4))
        else:
            for centre, hu, hv in stations:
                rings.append(station(centre, hu, hv, 0.0))
        if move is not None:
            rings = [[move(p) for p in r] for r in rings]
        return self.loft(name, bone, rings, mapping, caps=caps)

    def bend(self, faces, weights):
        for f in faces:
            for v in f.verts:
                self.blend[v] = weights(v.co)

    def wedge(self, name, bone, base, tip, base_half, tip_half, mapping, chamfer, across=X):
        w = (Vector(tip) - Vector(base)).normalized()
        u = (across - w * across.dot(w)).normalized()
        v = w.cross(u).normalized()
        return self.block(name, bone, u, v, [(base, base_half[0], base_half[1]), (tip, tip_half[0], tip_half[1])],
                          mapping, chamfer)

    def resolve_uvs(self):
        self.bm.normal_update()
        for face, spec in self.jobs:
            kind = spec[0]
            if kind == "tones":
                spec = flat(spec[1] if face.normal.z > 0.45 else (spec[3] if face.normal.z < -0.45 else spec[2]))
                kind = spec[0]
            elif kind == "flatfacing":
                spec = flat(spec[2] if face.normal.dot(Vector(spec[1])) > 0.6 else spec[3])
                kind = spec[0]
            elif kind == "square":
                _, group, flat_name, under_name, limit, ends = spec
                if ends is not None and abs(face.normal[ends[0]]) > 0.6:
                    spec = flat(ends[1])
                elif max(face.normal.dot(Vector(tex.VIEW_DIR[view])) for view in tex.GROUPS[group]) >= limit:
                    spec = proj(group)
                else:
                    spec = flat(under_name if face.normal.z < -0.3 else flat_name)
                kind = spec[0]
            if kind == "proj":
                group = spec[1]
                best, score = None, -1e9
                for view in tex.GROUPS[group]:
                    d = Vector(tex.VIEW_DIR[view])
                    s = face.normal.dot(d) * tex.VIEW_BIAS.get((group, view), 1.0)
                    if s > score:
                        best, score = view, s
                a, b = tex.VIEW_AXES[best]
                for loop in face.loops:
                    co = loop.vert.co
                    loop[self.uv].uv = tex.atlas_uv(group + "." + best, co[a], co[b])
            else:
                for loop in face.loops:
                    loop[self.uv].uv = tex.atlas_uv(spec[1], 0.5, 0.5)

    def to_object(self, armature):
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        obj = bpy.data.objects.new(self.name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        for b in ALL_BONES:
            obj.vertex_groups.new(name=b)
        layer = self.bm.verts.layers.int["bone"]
        self.bm.verts.ensure_lookup_table()
        self.bm.verts.index_update()
        for v in self.bm.verts:
            mix = [(b, w) for b, w in self.blend.get(v, ()) if w > 0.001]
            if not mix:
                obj.vertex_groups[v[layer]].add([v.index], 1.0, "REPLACE")
                continue
            total = sum(w for _, w in mix)
            for b, w in mix:
                obj.vertex_groups[b].add([v.index], w / total, "REPLACE")
        for poly in mesh.polygons:
            poly.use_smooth = True
        mesh.set_sharp_from_angle(angle=math.radians(SHARP_ANGLE))
        mod = obj.modifiers.new("Armature", "ARMATURE")
        mod.object = armature
        obj.parent = armature
        return obj


def _ramp(a, b, x):
    return max(0.0, min(1.0, (x - a) / (b - a)))


def _smooth(t):
    return t * t * (3.0 - 2.0 * t)


def _table(rows, x):
    if x <= rows[0][0]:
        return rows[0][1]
    for (x0, v0), (x1, v1) in zip(rows, rows[1:]):
        if x <= x1:
            return v0 + (v1 - v0) * (x - x0) / (x1 - x0)
    return rows[-1][1]


# ---------------------------------------------------------------------------
# TUBES: a cord through hand-set points, for her bead strings. Each bead is a short bicone (a swell
# between two narrow necks), its colour from a hand-set sequence; geometry, not paint.
# ---------------------------------------------------------------------------

def _catmull(points, closed, steps=12):
    pts = [Vector(p) for p in points]
    n = len(pts)
    out = []
    last = n if closed else n - 1
    for i in range(last):
        p0 = pts[(i - 1) % n] if closed or i > 0 else pts[0]
        p1, p2 = pts[i], pts[(i + 1) % n]
        p3 = pts[(i + 2) % n] if closed or i + 2 < n else pts[-1]
        for k in range(steps):
            t = k / float(steps)
            out.append(0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t))
    if not closed:
        out.append(pts[-1])
    return out


def _resample(dense, count, closed):
    """`count` points at equal steps along a dense polyline."""
    seq = dense + ([dense[0]] if closed else [])
    lengths = [0.0]
    for a, b in zip(seq, seq[1:]):
        lengths.append(lengths[-1] + (b - a).length)
    total = lengths[-1]
    out = []
    steps = count if closed else count - 1
    for k in range(count):
        target = total * k / float(steps)
        for i in range(len(lengths) - 1):
            if lengths[i + 1] >= target:
                f = (target - lengths[i]) / max(lengths[i + 1] - lengths[i], 1e-9)
                out.append(seq[i].lerp(seq[i + 1], f))
                break
        else:
            out.append(seq[-1].copy())
    return out


def bead_string(part, name, bone, points, beads, colours, r_bead, r_neck, closed=True, sides=5):
    """`beads` beads along the curve through `points`; `colours` is a hand-set string, one letter a
    bead: r madder, b bone, k black, g brass."""
    centres = _resample(_catmull(points, closed), 2 * beads + (0 if closed else 1), closed)
    n = len(centres)
    rings = []
    for i, p in enumerate(centres):
        a = centres[(i - 1) % n] if closed or i > 0 else centres[0]
        b = centres[(i + 1) % n] if closed or i < n - 1 else centres[-1]
        t = (b - a).normalized()
        ref = Z if abs(t.dot(Z)) < 0.9 else X
        u = t.cross(ref).normalized()
        v = t.cross(u).normalized()
        r = r_bead if i % 2 == (1 if not closed else 0) else r_neck
        rings.append([p + (u * math.cos(2 * math.pi * k / sides) + v * math.sin(2 * math.pi * k / sides)) * r
                      for k in range(sides)])
    if closed:
        rings.append(rings[0])
    paint = {"r": tones("red_lit", "red", "red_dark"), "b": tones("bone", "bone", "bone_shade"),
             "k": tones("black_lit", "black", "black_deep"), "g": tones("brass_lit", "brass", "brass_dark")}

    def mapping(i, j):
        bead = ((i + 1) // 2) % beads if closed else min(i // 2, beads - 1)
        return paint[colours[bead % len(colours)]]

    return part.loft(name, bone, rings, mapping, caps=(not closed, not closed))


# ---------------------------------------------------------------------------
# THE HEAD (Lola Pacing's rows: the heroes' rounded head, one flat face plane)
# ---------------------------------------------------------------------------
HEAD_N = 24
HEAD_CY = 0.0024
HEAD_ROWS = [
    (0.3432, 0.112, 0.120, 0.114, 3.4),
    (0.3490, 0.140, 0.146, 0.140, 3.8),
    (0.3680, 0.153, 0.154, 0.150, 4.0),
    (0.3920, 0.161, 0.158, 0.157, 4.2),
    (0.4300, 0.166, 0.158, 0.160, 4.2),
    (0.4700, 0.163, 0.158, 0.160, 4.2),
    (0.5200, 0.160, 0.158, 0.160, 4.2),
    (0.6000, 0.158, 0.158, 0.160, 4.2),
    (0.6450, 0.150, 0.150, 0.154, 3.8),
    (0.6613, 0.128, 0.128, 0.134, 3.4),
]


def super_point(t, z, rx, rf, rb, e, cy=HEAD_CY):
    c, s = math.cos(t), math.sin(t)
    a = math.copysign(abs(c) ** (2.0 / e), c)
    b = math.copysign(abs(s) ** (2.0 / e), s)
    return Vector((a * rx, cy + b * (rb if b >= 0 else rf), z))


def super_ring(z, rx, rf, rb, e, n=HEAD_N):
    return [super_point(2.0 * math.pi * j / n, z, rx, rf, rb, e) for j in range(n)]


def build_head(part):
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], proj_square("head", "skin", "skin", 0.84))
    # NO EARS SHOWN: her hair is swept back over them. HER EARRINGS, brass drops below the hair at
    # each side of her jaw, a hand's width apart from Lola Pacing's white studs in shape and metal.
    brass = tones("brass_lit", "brass", "brass_dark")
    for s in (1, -1):
        part.block("earring-hook", "head", Y, Z, [((s * 0.166, 0.020, 0.404), 0.006, 0.006), ((s * 0.166, 0.020, 0.420), 0.006, 0.006)],
                   brass, 0.0)
        part.block("earring", "head", Y, X, [((s * 0.168, 0.020, 0.362), 0.010, 0.008), ((s * 0.168, 0.020, 0.380), 0.016, 0.010),
                                            ((s * 0.168, 0.020, 0.404), 0.009, 0.008)], brass, 0.0)


# ---------------------------------------------------------------------------
# THE HAIR. A raised cap (her full crown) under everything, a shell down the sides and back over the
# ears, then the SWEPT LOCKS: four a side, each a chunky lofted strip following her head from the
# hairline back to the bun, the two crown locks leaving a dark parting down the middle; then the LOW
# BUN in two wound coils, tipped back and down, wider than tall, and its BEAD STRING.
# ---------------------------------------------------------------------------
LIT = tones("hair_lit", "hair", "hair_dark")
DARK = tones("hair", "hair_dark", "hair_under")
DEEP = tones("hair_dark", "hair_under", "hair_under")
HAIRLINE = tex.HAIRLINE
CAP = (0.171, 0.166, 0.172, 4.2)
SIDE_EDGE = [(0.400, 0.060), (0.430, 0.010), (0.460, -0.060), (0.500, -0.110), (0.550, -0.134), (0.590, -0.140)]
HEAD_CENTRE = Vector((0.0, 0.003, 0.480))
#   her LEFT locks (the right are the mirror): points along the lock, the half width across it and
#   its thickness out from her head at each point, and its tone. The inner face is 10 mm under.
LOCKS = [
    ("lock-crown", LIT,
     [(0.030, -0.162, 0.600), (0.040, -0.142, 0.654), (0.045, -0.090, 0.697), (0.048, 0.000, 0.709),
      (0.046, 0.090, 0.690), (0.040, 0.158, 0.624), (0.030, 0.190, 0.548)],
     [0.030, 0.036, 0.040, 0.040, 0.038, 0.034, 0.026], [0.004, 0.013, 0.019, 0.020, 0.019, 0.016, 0.010]),
    ("lock-upper", DARK,
     [(0.092, -0.156, 0.592), (0.104, -0.134, 0.640), (0.111, -0.081, 0.674), (0.117, 0.005, 0.683),
      (0.113, 0.090, 0.665), (0.097, 0.160, 0.602), (0.068, 0.193, 0.532)],
     [0.028, 0.034, 0.038, 0.038, 0.036, 0.032, 0.025], [0.004, 0.012, 0.018, 0.018, 0.017, 0.014, 0.010]),
    ("lock-temple", LIT,
     [(0.142, -0.128, 0.580), (0.164, -0.096, 0.574), (0.180, -0.040, 0.556), (0.183, 0.030, 0.532),
      (0.170, 0.100, 0.505), (0.130, 0.165, 0.482), (0.080, 0.198, 0.470)],
     [0.024, 0.034, 0.040, 0.042, 0.040, 0.034, 0.026], [0.006, 0.010, 0.012, 0.012, 0.012, 0.011, 0.009]),
    ("lock-ear", DARK,
     [(0.156, -0.072, 0.500), (0.174, -0.030, 0.482), (0.176, 0.040, 0.462), (0.160, 0.110, 0.446),
      (0.120, 0.170, 0.440), (0.072, 0.199, 0.444)],
     [0.022, 0.030, 0.032, 0.031, 0.028, 0.022], [0.005, 0.008, 0.009, 0.009, 0.009, 0.008]),
]
BUN_AT = Vector((0.0, 0.214, 0.466))
BUN_AXIS = Vector((0.0, 0.82, -0.57)).normalized()
BUN_WIDE = 1.30     # the bun is this much wider across than it is tall
BUN_COILS = [
    ("bun-base", DARK, [(-0.050, 0.058), (-0.016, 0.090), (0.018, 0.085)]),
    ("bun-outer", LIT, [(0.004, 0.074), (0.034, 0.078), (0.060, 0.042)]),
]
HAIR_FOLLOW = 0.5


def _edge_y(z):
    return _table(SIDE_EDGE, z)


def hair_weights(co):
    w = HAIR_FOLLOW * _ramp(0.400, 0.372, co.z)
    return [("head", 1.0 - w), ("torso", w)]


def _lock(part, name, paint, pts, halves, thick, mirror):
    pts = [Vector((mirror * p[0], p[1], p[2])) for p in pts]
    rings = []
    for i, p in enumerate(pts):
        a = pts[max(i - 1, 0)]
        b = pts[min(i + 1, len(pts) - 1)]
        t = (b - a).normalized()
        n = (p - HEAD_CENTRE)
        n = (n - t * n.dot(t)).normalized()
        u = t.cross(n).normalized()
        rings.append(rect_ring(p, u, n, halves[i], (0.010, thick[i]), min(0.010, 0.4 * thick[i] + 0.004)))
    return part.loft("hair-" + name, "head", rings, paint)


def build_hair(part):
    rx, rf, rb, e = CAP
    # THE RAISED CAP, her full crown, from the hairline up; the locks lie on it and the parting is it
    cap = [(HAIRLINE, rx - 0.004, rf - 0.002, rb, e), (0.630, rx, rf, rb, e), (0.668, 0.166, 0.160, 0.168, 3.8),
           (0.692, 0.140, 0.130, 0.146, 3.2), (0.704, 0.090, 0.080, 0.098, 2.6)]
    part.loft("hair-cap", "head", [super_ring(*row) for row in cap], DEEP)

    def shell(z):
        v = (_edge_y(z) - HEAD_CY) / (rb if _edge_y(z) >= HEAD_CY else rf)
        start = math.asin(math.copysign(min(abs(v), 0.999) ** (e / 2.0), v))
        steps = 8
        angles = [start + (math.pi - 2.0 * start) * k / steps for k in range(steps + 1)]
        outer = [super_point(t, z, rx, rf, rb, e) for t in angles]
        inner = [super_point(t, z, rx - 0.016, rf - 0.016, rb - 0.016, e) for t in reversed(angles)]
        return outer + inner

    part.loft("hair-shell", "head", [shell(z) for z, _ in SIDE_EDGE], DEEP)

    for name, paint, pts, halves, thick in LOCKS:
        for s in (1, -1):
            # the right side alternates its tones, so the two halves are not one stamp
            tone = paint if s > 0 else (DARK if paint is LIT else LIT)
            if name == "lock-crown":
                tone = LIT
            faces = _lock(part, name, tone, pts, halves, thick, s)
            part.bend(faces, hair_weights)

    # THE BUN, low at her nape: two wound coils on an axis tipped back and down, each swelling and
    # turning under before the next starts inside it
    w = BUN_AXIS
    u = X
    v = w.cross(u).normalized()
    sides = 12
    for name, paint, profile in BUN_COILS:
        rings = []
        for along, radius in profile:
            at = BUN_AT + w * along
            rings.append([at + (u * BUN_WIDE * math.cos(2.0 * math.pi * k / sides) + v * math.sin(2.0 * math.pi * k / sides)) * radius
                          for k in range(sides)])
        part.loft("hair-" + name, "head", rings, paint, caps=(name == "bun-base", True))

    # HER BEAD STRING, wound once round the bun at its swell, and its end hanging from the bun's
    # lower right with three beads
    loop = []
    for k in range(10):
        a = 2.0 * math.pi * k / 10.0
        loop.append(BUN_AT + w * 0.000 + (u * BUN_WIDE * math.cos(a) + v * math.sin(a)) * 0.094)
    bead_string(part, "hair-beads", "head", loop, 12, "rbrgrbrbrgrb", 0.0110, 0.0045)
    a = math.radians(235.0)
    hang = BUN_AT + (u * BUN_WIDE * math.cos(a) + v * math.sin(a)) * 0.094
    bead_string(part, "hair-beads-end", "head", [hang, hang + Vector((-0.006, 0.012, -0.030)), hang + Vector((-0.010, 0.020, -0.062))],
                3, "brg", 0.0105, 0.0045, closed=False)


# ---------------------------------------------------------------------------
# THE TORSO. An indigo wrapped top (the body block), the neck, HER BLANKET over the shoulders, its
# banded edge crossing her chest and the knot, three bead strands, the belt, the tapis.
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.3432)
TORSO_CY = 0.0288
TORSO_HALF = 0.126
TORSO_FRONT = TORSO_CY - tex.FRONT_Y
ARM_Y, ARM_Z = 0.01725, 0.28775

SKIRT_TOP = tex.WAIST
SKIRT_FOLLOW = 0.85                    # how much of a leg the tapis' hem takes (it must lie on her thighs when she sits)
SKIRT_TOP_R = (0.139, 0.1234, 0.1233)
SKIRT_HEM_R = (0.150, 0.1340, 0.1340)

#   THE BLANKET'S PROFILE, from inside its rolled top edge down to its hem and under:
#   (z, centre y, half width, depth to the front, depth to the back, paint)
CREAM_T = tones("cream_lit", "cream", "cream_shade")
MANTLE = [
    (0.366, 0.012, 0.075, 0.075, 0.064, CREAM_T),            # the roll's top
    (0.351, 0.018, 0.094, 0.095, 0.083, CREAM_T),            # the roll's outer foot
    (0.349, 0.022, 0.124, 0.121, 0.110, CREAM_T),            # over her shoulders
    (0.318, 0.026, 0.150, 0.127, 0.126, proj_square("mantle", "cream", "cream_shade", 0.90)),
    (0.272, 0.0288, 0.154, 0.130, 0.130, CREAM_T),
    (0.266, 0.0288, 0.155, 0.130, 0.131, tones("red_lit", "red", "red_dark")),     # the madder band
    (0.256, 0.0288, 0.156, 0.131, 0.132, CREAM_T),
    (0.250, 0.0288, 0.157, 0.131, 0.133, tones("black_lit", "black", "black_deep")),  # the black hem band
    (0.234, 0.0288, 0.158, 0.132, 0.134, flat("black_deep")),
    (0.238, 0.0288, 0.144, 0.120, 0.122, None),
]


def _rounds(n=24):
    out = []
    for k in range(n):
        a = 2.0 * math.pi * k / float(n)
        c, sn = math.cos(a), math.sin(a)
        out.append((math.copysign(abs(sn) ** 0.45, sn), math.copysign(abs(c) ** 0.45, c)))
    out.append(out[0])
    return out


def skirt_f(z):
    return _table([(tex.TAPIS_HEM, 1.0), (SKIRT_TOP, 0.0)], z)


def skirt_r(z):
    f = skirt_f(z)
    return [SKIRT_TOP_R[k] + (SKIRT_HEM_R[k] - SKIRT_TOP_R[k]) * f for k in range(3)]


def skirt_weights(co):
    down = _ramp(SKIRT_TOP - 0.012, tex.TAPIS_HEM, co.z) ** 0.8
    # only the FRONT of the tapis follows the thighs; the back stays under her seat when she sits
    front = _smooth(_ramp(TORSO_CY + 0.070, TORSO_CY - 0.030, co.y))
    leg = SKIRT_FOLLOW * down * front
    left = _smooth(_ramp(-0.070, 0.070, co.x))
    return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]


def mantle_weights(co):
    """The blanket over her shoulders takes up to 0.6 of the arm beneath it, so a lifted or hanging
    arm carries the blanket with it instead of passing through it (rule 7)."""
    arm = 0.6 * _ramp(0.090, 0.150, abs(co.x)) * _ramp(0.245, 0.300, co.z)
    side = "arm-left" if co.x > 0 else "arm-right"
    return [("torso", 1.0 - arm), (side, arm)]


def build_torso(part):
    indigo = tones("indigo_lit", "indigo", "indigo_deep")
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, TORSO_Z[0]), TORSO_HALF, (TORSO_FRONT, 0.1091)),
                                        ((0, TORSO_CY, 0.2531), TORSO_HALF, (TORSO_FRONT, 0.1091)),
                                        ((0, TORSO_CY, TORSO_Z[1]), 0.104, (TORSO_FRONT, 0.0861))], indigo, 0.014)
    part.block("pelvis", "torso", X, Y, [((0, TORSO_CY, 0.120), 0.060, 0.070), ((0, TORSO_CY, 0.180), 0.060, 0.070)], flat("indigo_deep"), 0.0)
    neck = part.block("neck", "torso", X, Y, [((0, HEAD_CY, 0.326), 0.050, 0.048), ((0, HEAD_CY, 0.360), 0.048, 0.046)],
                      flat("skin_shade"), 0.0)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.338, 0.356, co.z)), ("head", 0.7 * _ramp(0.338, 0.356, co.z))])

    # HER BLANKET
    rounds = _rounds()
    sections = []
    for px, py in rounds:
        ring = []
        for z, cy, hx, hf, hb, _ in MANTLE:
            ring.append(Vector((px * hx, cy - py * (hf if py > 0 else hb), z)))
        sections.append(ring)

    def mantle_paint(i, j):
        return MANTLE[j][5] or flat("cream_deep")

    blanket = part.loft("blanket", "torso", sections, mantle_paint, caps=(False, False))
    part.bend(blanket, mantle_weights)

    # its two ends cross at her chest: the overlapping end's banded edge, black then madder, from
    # under the knot down to the hem
    front = TORSO_CY - 0.131 - 0.004
    black = tones("black_lit", "black", "black_deep")
    red = tones("red_lit", "red", "red_dark")
    part.block("blanket-edge", "torso", X, Y, [((-0.027, front, 0.236), 0.010, 0.0035), ((-0.027, front, 0.268), 0.010, 0.0035)], black, 0.0)
    part.block("blanket-edge-red", "torso", X, Y, [((-0.011, front, 0.236), 0.005, 0.003), ((-0.011, front, 0.268), 0.005, 0.003)], red, 0.0)
    # THE KNOT, the focal fastening: the two corners tied, a chunky block crossed by a black band,
    # and two short tails, each with a black end
    kf = front - 0.010
    part.block("blanket-knot", "torso", X, Y, [((-0.012, kf, 0.262), 0.026, 0.013), ((-0.012, kf - 0.002, 0.282), 0.031, 0.015),
                                               ((-0.012, kf, 0.302), 0.025, 0.013)], CREAM_T, 0.010)
    part.block("blanket-knot-band", "torso", X, Y, [((-0.012, kf - 0.002, 0.276), 0.032, 0.0165), ((-0.012, kf - 0.002, 0.288), 0.032, 0.0165)],
               black, 0.0)
    for base, mid, tip, half in (((-0.034, kf + 0.002, 0.268), (-0.046, kf + 0.004, 0.246), (-0.052, kf + 0.006, 0.234), (0.011, 0.006)),
                                 ((0.008, kf + 0.002, 0.266), (0.018, kf + 0.004, 0.246), (0.022, kf + 0.006, 0.236), (0.010, 0.006))):
        part.wedge("blanket-tail", "torso", base, mid, half, (half[0] * 0.9, half[1]), CREAM_T, 0.0)
        part.wedge("blanket-tail-end", "torso", mid, tip, (half[0] * 0.9, half[1] * 1.05), (half[0] * 0.75, half[1]), black, 0.0)

    # HER OLD BEADS: three strands round her neck over the blanket, each a hand-set curve (her left
    # half from the back of her neck round to the front, the right the mirror) and its own beads
    strands = [
        ("beads-bone", [(0, 0.104, 0.360), (0.062, 0.094, 0.360), (0.100, 0.036, 0.360), (0.100, -0.030, 0.358),
                        (0.074, -0.086, 0.352), (0.036, -0.110, 0.345), (0, -0.114, 0.342)], 14, "bkbbkbbkbbkbbk", 0.0085),
        ("beads-madder", [(0, 0.110, 0.357), (0.070, 0.100, 0.357), (0.110, 0.036, 0.356), (0.108, -0.036, 0.351),
                          (0.080, -0.094, 0.340), (0.040, -0.115, 0.330), (0, -0.118, 0.326)], 15, "rrrrrrrgrrrrrrr", 0.0095),
        ("beads-black", [(0, 0.116, 0.355), (0.078, 0.106, 0.354), (0.118, 0.036, 0.351), (0.116, -0.040, 0.344),
                         (0.086, -0.100, 0.330), (0.042, -0.119, 0.313), (0, -0.122, 0.308)], 17, "kkrkkrkkrkkrkkrkk", 0.0085),
    ]
    for name, half_pts, count, colours, r in strands:
        loop = half_pts + [(-p[0], p[1], p[2]) for p in reversed(half_pts[1:-1])]
        # start at the front so the hand-set colour sequence is centred on her chest
        start = len(half_pts) - 1
        loop = loop[start:] + loop[:start]
        bead_string(part, name, "torso", loop, count, colours, r, 0.0040, sides=4)

    # THE BELT: a woven band, black with a madder stripe, standing 6 mm proud of the tapis' top
    band = []
    for px, py in rounds:
        def at(z, out, px=px, py=py):
            r = [SKIRT_TOP_R[k] + out for k in range(3)]
            return Vector((px * r[0], TORSO_CY - py * (r[1] if py > 0 else r[2]), z))

        band.append([at(0.231, -0.012), at(0.229, 0.006), at(0.219, 0.0065), at(0.209, 0.0065), at(0.199, 0.006), at(0.197, -0.012)])
    belt_paint = (flat("black_lit"), black, red, black, flat("black_deep"), flat("black_deep"))
    part.loft("belt", "torso", band, lambda i, j: belt_paint[j], caps=(False, False))
    # its two ends hanging at her left hip, each tipped with madder
    for base, tip, half in (((0.112, -0.096, 0.202), (0.118, -0.101, 0.160), (0.011, 0.004)),
                            ((0.090, -0.104, 0.202), (0.093, -0.110, 0.168), (0.010, 0.004))):
        end = Vector(base).lerp(Vector(tip), 0.78)
        f1 = part.wedge("belt-end", "torso", base, tuple(end), half, half, black, 0.0)
        f2 = part.wedge("belt-end-tip", "torso", tuple(end), tip, half, (half[0] * 0.8, half[1]), red, 0.0)
        part.bend(f1 + f2, skirt_weights)

    # THE TAPIS: from the waist to her knee, its bands as profile segments in flat tones, the upper
    # indigo band carrying its drawn diamonds, its indigo hem standing 4 mm proud
    hem = tex.TAPIS_HEM
    rows = [(SKIRT_TOP + 0.006, 0.016), (SKIRT_TOP, 0.0), (0.176, 0.0), (0.150, 0.0), (0.137, 0.0), (0.112, 0.0),
            (0.1115, -0.004), (hem, -0.004), (hem + 0.003, 0.012)]
    sections = []
    for px, py in rounds:
        ring = []
        for z, inset in rows:
            r = [v - inset for v in skirt_r(z)]
            ring.append(Vector((px * r[0], TORSO_CY - py * (r[1] if py > 0 else r[2]), z)))
        sections.append(ring)
    white = tones("white_lit", "white", "white_shade")
    tapis_paint = (flat("white_lit"), white, proj_square("tapis", "indigo", "indigo_deep", 0.90), red, white,
                   flat("indigo_lit"), tones("indigo_lit", "indigo", "indigo_deep"), flat("indigo_deep"), flat("white_shade"),
                   flat("white_shade"))
    skirt = part.loft("tapis", "torso", sections, lambda i, j: tapis_paint[j], caps=(False, False))
    part.bend(skirt, skirt_weights)
    # the wrap's overlapping end, on her left: a strip standing 3 mm proud, its bands matching
    for z0, z1, paint in ((0.2000, 0.176, white), (0.176, 0.150, tones("indigo_lit", "indigo", "indigo_deep")), (0.150, 0.137, red),
                          (0.137, 0.112, white), (0.112, hem, tones("indigo_lit", "indigo", "indigo_deep"))):
        y0 = TORSO_CY - skirt_r(z0)[1] - 0.0035 - (0.004 if z1 == hem else 0.0)
        y1 = TORSO_CY - skirt_r(z1)[1] - 0.0035 - (0.004 if z1 == hem else 0.0)
        f = part.block("tapis-wrap", "torso", X, Y, [((0.054, y0, z0), 0.012, 0.0035), ((0.056, y1, z1), 0.012, 0.0035)], paint, 0.0)
        part.bend(f, skirt_weights)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests. The blanket's drape over the upper arm (on the arm bone),
# its hem banded madder and black like the blanket's; bare skin below it; brass bangles; the fist.
# ---------------------------------------------------------------------------

def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    skin = tones("skin_lit", "skin", "skin_shade")
    arm_block(part, "upper-arm", bone, s, [(0.098, (0.043, 0.043)), (0.218, (0.043, 0.043))], skin, 0.012)
    # the blanket's drape over the shoulder and upper arm
    rows = [(0.094, 0.058, 0.060, CREAM_T), (0.150, 0.063, 0.066, CREAM_T), (0.157, 0.0635, 0.0665, tones("red_lit", "red", "red_dark")),
            (0.166, 0.064, 0.067, CREAM_T), (0.170, 0.064, 0.067, tones("black_lit", "black", "black_deep")),
            (0.186, 0.064, 0.067, flat("black_deep")), (0.188, 0.052, 0.054, None)]
    rings = [rect_ring((s * x, ARM_Y, ARM_Z + 0.004), Y, Z, hy, hz, 0.022) for x, hy, hz, _ in rows]
    part.loft("arm-drape", bone, rings, lambda i, j: rows[i][3] or flat("cream_deep"), caps=(True, False))
    # the forearm starts 10 mm inside the elbow, a little wider than the upper arm, so it caps the
    # elbow's turn
    arm_block(part, "forearm", fore, s, [(ELBOW_X - 0.010, (0.045, 0.045)), (0.300, (0.039, 0.039))], skin, 0.012)
    brass = tones("brass_lit", "brass", "brass_dark")
    for x0, x1 in (((0.266, 0.277), (0.281, 0.292)) if s > 0 else ((0.276, 0.290),)):
        arm_block(part, "bangle", fore, s, [(x0, (0.0455, 0.0455)), (x1, (0.0450, 0.0450))], brass, 0.0)
    arm_block(part, "wrist", fore, s, [(0.296, (0.035, 0.035)), (tex.WRIST + 0.004, (0.035, 0.035))], skin, 0.0)
    arm_block(part, "hand", fore, s, [(tex.WRIST, (0.0555, 0.0555)), (tex.HAND_END, (0.0555, 0.0555))],
              proj_square(group, "skin", "skin_shade", 0.93), 0.013)


# ---------------------------------------------------------------------------
# THE LEGS AND HER BARE FEET
# ---------------------------------------------------------------------------
LEG_X = 0.0829
LEG_CY = 0.0288


def build_leg(part, s):
    bone = "leg-left" if s > 0 else "leg-right"
    skin = tones("skin_lit", "skin", "skin_shade")
    part.block("shin", bone, X, Y, [((s * LEG_X, LEG_CY, 0.030), 0.039, 0.042), ((s * LEG_X, LEG_CY, 0.182), 0.044, 0.050)], skin, 0.014)

    def station(y, half, z0, z1, cut):
        return rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)

    # the foot, from the heel to the ball, rising over the instep
    foot = [(0.072, 0.038, 0.0, 0.030, 0.010), (0.056, 0.045, 0.0, 0.042, 0.013), (0.000, 0.048, 0.0, 0.040, 0.013),
            (-0.060, 0.050, 0.0, 0.028, 0.010)]
    part.loft("foot", bone, [station(*row) for row in foot], skin)
    # the toes, one slight step lower and a little narrower
    toes = [(-0.054, 0.048, 0.0, 0.021, 0.008), (-0.098, 0.044, 0.0, 0.019, 0.007)]
    part.loft("toes", bone, [station(*row) for row in toes], tones("skin_lit", "skin", "skin_deep"))


# ---------------------------------------------------------------------------
# HER BASKET: a lidded rattan storage basket, its own mesh on `root`, standing at her left foot.
# ---------------------------------------------------------------------------

def build_basket(part):
    bx, by = tex.BASKET_C
    fx, fy = tex.BASKET_FOOT
    rx, ry = tex.BASKET_RIM
    top = tex.BASKET_TOP

    def half(z, out=0.0):
        f = (z - 0.006) / (top - 0.006)
        return (fx + (rx - fx) * f + out, fy + (ry - fy) * f + out)

    dark = tones("rattan_dark", "rattan_dark", "rattan_deep")
    part.block("basket", "root", X, Y, [((bx, by, 0.004),) + half(0.004), ((bx, by, top - 0.004),) + half(top - 0.004)],
               proj_square("basket", "rattan", "rattan_dark", 0.90, ends=(2, "rattan_lit")), 0.014)
    for z0, z1, out in ((0.000, 0.016, 0.004), (0.060, 0.072, 0.0035), (0.108, top + 0.002, 0.0045)):
        part.block("basket-band", "root", X, Y, [((bx, by, z0),) + half(z0, out), ((bx, by, z1),) + half(z1, out)], dark, 0.0)
    # the lid, overlapping the rim, rising to a low crown, a dark band round its skirt, a knob
    part.block("basket-lid", "root", X, Y, [((bx, by, top - 0.002), rx + 0.008, ry + 0.008), ((bx, by, top + 0.020), rx + 0.006, ry + 0.006),
                                            ((bx, by, tex.BASKET_LID), 0.052, 0.046)],
               tones("rattan_lit", "rattan", "rattan_dark"), 0.012)
    part.block("basket-lid-band", "root", X, Y, [((bx, by, top + 0.000), rx + 0.0095, ry + 0.0095), ((bx, by, top + 0.010), rx + 0.0095, ry + 0.0095)],
               dark, 0.0)
    part.block("basket-knob", "root", X, Y, [((bx, by, tex.BASKET_LID - 0.004), 0.016, 0.016), ((bx, by, tex.BASKET_KNOB), 0.013, 0.013)],
               dark, 0.0)
    # a lashing loop down the front, lid to body
    part.block("basket-lash", "root", X, Y, [((bx, by - ry - 0.008, 0.096), 0.008, 0.004), ((bx, by - ry - 0.012, top + 0.016), 0.008, 0.004)],
               dark, 0.0)


# ---------------------------------------------------------------------------
# THE BUILD
# ---------------------------------------------------------------------------

def mesh_arrays(obj):
    mesh = obj.data
    mesh.calc_loop_triangles()
    normals = mesh.corner_normals
    uvs = mesh.uv_layers[0].data
    mix_of = []
    for v in mesh.vertices:
        mix = sorted(((g.weight, ALL_BONES.index(obj.vertex_groups[g.group].name)) for g in v.groups if g.weight > 0.001), reverse=True)[:4]
        total = sum(wt for wt, _ in mix)
        mix_of.append([(b, wt / total) for wt, b in mix] + [(0, 0.0)] * (4 - len(mix)))
    seen, pos, nrm, uv, joints, weights, idx = {}, [], [], [], [], [], []
    for tri in mesh.loop_triangles:
        for loop in tri.loops:
            vi = mesh.loops[loop].vertex_index
            co = mesh.vertices[vi].co
            n = normals[loop].vector
            t = uvs[loop].uv
            key = (vi, round(n.x, 4), round(n.y, 4), round(n.z, 4), round(t.x, 6), round(t.y, 6))
            if key not in seen:
                seen[key] = len(pos)
                pos.append((co.x, co.z, -co.y))
                nrm.append((n.x, n.z, -n.y))
                uv.append((t.x, 1.0 - t.y))
                joints.append(tuple(b for b, _ in mix_of[vi]))
                weights.append(tuple(wt for _, wt in mix_of[vi]))
            idx.append(seen[key])
    return pos, nrm, uv, joints, weights, idx


def write_glb(body, head, basket, out):
    """character-female-d.glb with its meshes swapped and a third, the basket, added on the same
    skin: skeleton, bind matrices and the clips she does not replace copied untouched, two elbows
    appended, her own clips written in, `cheer` and `clap` added."""
    gltf, buffer = bpv.read_glb(BASE)
    names = [gltf["nodes"][i]["name"] for i in gltf["skins"][0]["joints"]]
    if names != BONES:
        raise SystemExit("the base rig's joints are %s, not %s" % (names, BONES))

    blob = bytearray()
    views, accessors, remap = [], [], {}

    def align():
        while len(blob) % 4:
            blob.append(0)

    def keep(old):
        if old in remap:
            return remap[old]
        acc = dict(gltf["accessors"][old])
        data = bpv.accessor_bytes(gltf, buffer, old)
        align()
        acc["bufferView"] = len(views)
        acc.pop("byteOffset", None)
        views.append({"buffer": 0, "byteOffset": len(blob), "byteLength": len(data)})
        blob.extend(data)
        remap[old] = len(accessors)
        accessors.append(acc)
        return remap[old]

    def add(values, fmt, kind, component, minmax=False):
        align()
        start = len(blob)
        for v in values:
            blob.extend(struct.pack("<" + fmt * len(v), *v))
        acc = {"bufferView": len(views), "componentType": component, "count": len(values), "type": kind}
        if minmax:
            acc["min"] = [min(v[a] for v in values) for a in range(len(values[0]))]
            acc["max"] = [max(v[a] for v in values) for a in range(len(values[0]))]
        views.append({"buffer": 0, "byteOffset": start, "byteLength": len(blob) - start})
        accessors.append(acc)
        return len(accessors) - 1

    accessor_raw = bpv.accessor_bytes(gltf, buffer, gltf["skins"][0]["inverseBindMatrices"])
    for anim in gltf["animations"]:
        if anim["name"] in clips.CLIPS:
            continue
        for sampler in anim["samplers"]:
            sampler["input"] = keep(sampler["input"])
            sampler["output"] = keep(sampler["output"])

    node_of = {n.get("name"): i for i, n in enumerate(gltf["nodes"])}
    raw = struct.unpack("<%df" % (16 * len(BONES)), accessor_raw)
    matrices = [raw[k * 16:(k + 1) * 16] for k in range(len(BONES))]
    new_joints = []
    for name, parent, side in EXTRA_BONES:
        world = (side * ELBOW_X, ARM_Z, -ARM_Y)
        pw = [-matrices[BONES.index(parent)][12 + a] for a in range(3)]
        gltf["nodes"].append({"name": name, "translation": [world[a] - pw[a] for a in range(3)]})
        gltf["nodes"][node_of[parent]].setdefault("children", []).append(len(gltf["nodes"]) - 1)
        new_joints.append(len(gltf["nodes"]) - 1)
        matrices.append((1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -world[0], -world[1], -world[2], 1))
    ibm = add([tuple(float(x) for x in m) for m in matrices], "f", "MAT4", 5126)
    for skin in gltf["skins"]:
        skin["joints"] = list(skin["joints"]) + new_joints
        skin["inverseBindMatrices"] = ibm

    node_of = {n.get("name"): i for i, n in enumerate(gltf["nodes"])}
    have = {a["name"] for a in gltf["animations"]}
    for name in clips.CLIPS:
        if name not in have:
            gltf["animations"].append({"name": name, "samplers": [], "channels": []})
    for anim in gltf["animations"]:
        if anim["name"] not in clips.CLIPS:
            continue
        anim["samplers"], anim["channels"] = [], []
        for (bone, path), keys in clips.CLIPS[anim["name"]]().items():
            times = add([(t,) for t, _ in keys], "f", "SCALAR", 5126, minmax=True)
            values = add([v for _, v in keys], "f", "VEC4" if path == "rotation" else "VEC3", 5126)
            anim["channels"].append({"sampler": len(anim["samplers"]), "target": {"node": node_of[bone], "path": path}})
            anim["samplers"].append({"input": times, "output": values, "interpolation": "LINEAR"})

    def primitive(built):
        pos, nrm, uv, joints, weights, idx = built
        return [{
            "attributes": {
                "POSITION": add(pos, "f", "VEC3", 5126, minmax=True),
                "NORMAL": add(nrm, "f", "VEC3", 5126),
                "TEXCOORD_0": add(uv, "f", "VEC2", 5126),
                "JOINTS_0": add(joints, "H", "VEC4", 5123),
                "WEIGHTS_0": add(weights, "f", "VEC4", 5126),
            },
            "indices": add([(i,) for i in idx], "I", "SCALAR", 5125),
            "material": 0,
            "mode": 4,
        }]

    by_name = {m.get("name"): m for m in gltf["meshes"]}
    for name, built in (("body-mesh", body), ("head-mesh", head)):
        by_name[name]["primitives"] = primitive(built)
    # THE BASKET: a third mesh and node on skin 0, every vertex on `root`
    gltf["meshes"].append({"name": "basket-mesh", "primitives": primitive(basket)})
    gltf["nodes"].append({"name": "basket-mesh", "mesh": len(gltf["meshes"]) - 1, "skin": 0})
    gltf["nodes"][0].setdefault("children", []).append(len(gltf["nodes"]) - 1)

    gltf["accessors"] = accessors
    gltf["bufferViews"] = views
    gltf["buffers"] = [{"byteLength": len(blob)}]
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso Payo audience v2 (elder_woman)"}
    gltf["extras"] = {"audience": "payo-audience-v2", "villager": "elder_woman", "replaces": "nothing; not loaded by the game yet",
                      "seat": "in `sit` the seat (underside of the hips) is at z 0 at the root origin; the root is at its rest place"}
    gltf["images"] = [{"uri": tex.ATLAS_NAME, "name": NAME + "-atlas"}]
    gltf["textures"] = [{"sampler": 0, "source": 0, "name": NAME + "-atlas"}]
    gltf["samplers"] = [{"magFilter": 9729, "minFilter": 9987}]
    gltf["materials"][0] = {"name": NAME, "doubleSided": False,
                            "pbrMetallicRoughness": {"baseColorTexture": {"index": 0}, "metallicFactor": 0.0, "roughnessFactor": 1.0}}
    for key in ("extensionsUsed", "extensionsRequired"):
        if key in gltf:
            gltf[key] = [e for e in gltf[key] if e != "KHR_texture_transform"]
            if not gltf[key]:
                del gltf[key]
    gltf["nodes"][0]["name"] = NAME
    gltf["scenes"][0]["name"] = NAME
    bpv.write_glb(out, gltf, blob)


def verify(objects):
    lo = Vector((1e9, 1e9, 1e9))
    hi = -lo
    tris = 0
    for obj in objects:
        obj.data.calc_loop_triangles()
        tris += len(obj.data.loop_triangles)
        for v in obj.data.vertices:
            for a in range(3):
                lo[a] = min(lo[a], v.co[a])
                hi[a] = max(hi[a], v.co[a])
    print("bounds  x %.3f..%.3f  y %.3f..%.3f  z %.3f..%.3f" % (lo.x, hi.x, lo.y, hi.y, lo.z, hi.z))
    print("HEIGHT %.4f   triangles %d  (budget %d)" % (hi.z, tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if not HEIGHT_RANGE[0] <= hi.z <= HEIGHT_RANGE[1]:
        raise SystemExit("her height %.4f is outside the cast's range %s" % (hi.z, HEIGHT_RANGE))
    if tris > TRIANGLE_BUDGET:
        raise SystemExit("%d triangles is over the budget of %d" % (tris, TRIANGLE_BUDGET))
    body = objects[0]
    for side, sign in (("right", -1.0), ("left", 1.0)):
        group = body.vertex_groups["forearm-" + side].index
        fore = [v.co for v in body.data.vertices if len(v.groups) == 1 and v.groups[0].group == group]
        upper_group = body.vertex_groups["arm-" + side].index
        upper = [v.co for v in body.data.vertices if len(v.groups) == 1 and v.groups[0].group == upper_group]
        arm = fore + upper
        size = [max(p[a] for p in arm) - min(p[a] for p in arm) for a in range(3)]
        if not (size[0] > size[1] and size[0] > size[2]):
            raise SystemExit("arm-%s no longer runs along x: %s" % (side, size))
        near = min(p.x * sign for p in arm)
        far = max(p.x * sign for p in arm)
        if abs(far - max(p.x * sign for p in fore)) > 1e-6:
            raise SystemExit("the far end of arm-%s is not on the forearm bone" % side)
        cut = near + 0.86 * (far - near)
        fist = [p for p in arm if p.x * sign >= cut]
        across = max(max(p.y for p in fist) - min(p.y for p in fist), max(p.z for p in fist) - min(p.z for p in fist))
        widest = max(size[1], size[2])
        top = max(p.z for p in fist) - ARM_Z
        print("hand %-5s arm x %.4f to %.4f, the far 14 per cent starts at %.4f; fist %.4f across (the arm's widest is %.4f), "
              "top %.4f above the arm bone" % (side, near, far, cut, across, widest, top))
        if abs(across - 0.111) > 0.002:
            raise SystemExit("what is in the far 14 per cent of arm-%s is %.4f across, not the fist's 0.111" % (side, across))
        if across <= 0.5 * widest:
            raise SystemExit("the fist is under half the arm's widest section")
        if not 0.045 <= top <= 0.066:
            raise SystemExit("the hand's top moved")
        if abs(far - tex.HAND_END) > 0.003:
            raise SystemExit("the hand's far end moved off the original's reach")


def build(armature, material):
    body, head, basket = Part("body-mesh"), Part("head-mesh"), Part("basket-mesh")
    build_torso(body)
    build_arm(body, 1)
    build_arm(body, -1)
    build_leg(body, 1)
    build_leg(body, -1)
    build_head(head)
    build_hair(head)
    build_basket(basket)
    objects = []
    for part in (body, head, basket):
        part.resolve_uvs()
        for v in part.bm.verts:
            mix = part.blend.get(v)
            if mix is None:
                share = 1.0 if ALL_BONES[v[part.bone]] == "head" else 0.0
            else:
                share = sum(w for b, w in mix if b == "head") / sum(w for _, w in mix)
            if share > 0.0:
                v.co = HEAD_JOINT + (v.co - HEAD_JOINT) * (1.0 + (HEAD_SCALE - 1.0) * share)
        obj = part.to_object(armature)
        obj.data.materials.append(material)
        objects.append(obj)
        merged = {}
        for n, t in part.pieces:
            merged[n] = merged.get(n, 0) + t
        print(part.name, sum(merged.values()), " ".join("%s:%d" % (n, t) for n, t in merged.items()))
    verify(objects)
    return objects


def _retry(action):
    for attempt in range(6):
        try:
            return action()
        except (OSError, RuntimeError) as error:
            print("write refused (%s), trying again" % error)
            time.sleep(1.0)
    return action()


def main():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out = args[args.index("--out") + 1] if "--out" in args else None
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=BASE, bone_heuristic="BLENDER")
    armature = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    for obj in [o for o in bpy.data.objects if o.type == "MESH"]:
        bpy.data.objects.remove(obj)
    armature.name = NAME
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode="EDIT")
    for name, parent, side in EXTRA_BONES:
        eb = armature.data.edit_bones.new(name)
        eb.head = (side * ELBOW_X, ARM_Y, ARM_Z)
        eb.tail = (side * (ELBOW_X + 0.06), ARM_Y, ARM_Z)
        eb.parent = armature.data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    if armature.animation_data:
        armature.animation_data.action = None

    folder = os.path.dirname(out) if out else FOLDER
    image_path = os.path.join(folder, tex.ATLAS_NAME)
    if not os.path.exists(image_path):
        raise SystemExit("paint the atlas first: py -3 tools/author_payo_audience_v2_elder_woman_textures.py")
    image = bpy.data.images.load(image_path)
    material = bpy.data.materials.new(NAME)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    node = nodes.new("ShaderNodeTexImage")
    node.image = image
    material.node_tree.links.new(node.outputs[0], nodes["Principled BSDF"].inputs["Base Color"])
    nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0

    objects = build(armature, material)
    arrays = [mesh_arrays(o) for o in objects]
    _retry(lambda: write_glb(arrays[0], arrays[1], arrays[2], out or OUT))
    print("wrote " + (out or OUT))
    if out is not None:
        return
    image.filepath = bpy.path.relpath(image_path, start=os.path.dirname(BLEND))
    bpy.context.preferences.filepaths.save_version = 0
    _retry(lambda: bpy.ops.wm.save_as_mainfile(filepath=BLEND))
    print("wrote " + BLEND)


if __name__ == "__main__":
    main()
