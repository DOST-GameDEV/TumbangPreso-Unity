"""Build the Payo audience v2 villager `elder_man`: the village elder (lakay), as a Classic is built.

    py -3 tools/author_payo_audience_v2_elder_man_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_payo_audience_v2_elder_man.py

Writes
    ArtSource/payo/audience/v2/elder_man/audience_v2_elder_man.glb     (rigged, every clip)
    ArtSource/payo/audience/v2/elder_man/audience_v2_elder_man.blend
beside the atlas painted by the textures script.

WHY. docs/reports/character-redesign/payo-audience-brief.md: the v1 crowd (six Blender box
figures, tools/author_payo_audience.py) was REJECTED for its construction; every villager is
rebuilt EXACTLY as a Classic redesign is, with the same rules, rig, paint and clip machinery.
This is a copy of Mang Kanor's builder (tools/author_character_redesign_mang_kanor.py, an approved
Classic, a man) rewritten for the elder. NOT UNDER Assets/: nothing in the game loads it; the
Payo map session imports it (tools/bake_payo_audience_v2.py bakes the static crowd poses).

WHO HE IS. The lakay of the terrace village, on the basin wall, leaning on his spear to watch
the match. Dignified, a little stooped, watching. Precolonial Ifugao and Cordillera dress:
  * GREY HAIR in the Cordillera bowl cut, a straight fringe and a level line above the ears,
    chunky locks on the crown above
  * a HEAD CLOTH wound round the head twice, an indigo wrap over a madder-red one, with a KNOT
    over his right ear and its two TAILS hanging behind it (the red-banded head cloth, kept small)
  * an INDIGO-BLACK WOVEN BLANKET over both shoulders, an elder's: a plate down his back to the
    hips with level stripes, a cap over each shoulder, a panel down each side of his chest with
    the stripes running down; its hems and front edges PIPED IN COTTON with a madder band beside,
    as geometry (CAST_CLOTHING_STYLE rule 3)
  * a BAHAG: a wrapped indigo belt with a madder band round it, a fold over the belt at the front
    and the front and back FLAPS hanging from it, each with its woven end and a cotton hem. The
    front flap with its diamond is his one focal piece (rule 4), framed by the blanket's edges
  * BRASS EAR ORNAMENTS, his one metal (with the spear's binding ring)
  * BAREFOOT: proper foot blocks with a step for the toes and a big toe of its own
  * black BATOK routes on both forearms, a different fill on each (the textures script)
  * a SPEAR, a hardwood shaft with an iron butt and a leaf-shaped iron blade on a brass-bound
    socket, a SEPARATE MESH (`spear-mesh`) skinned rigidly to `forearm-right`. It passes through
    his right fist square to the forearm, the way a fist holds a staff, so the clips turn the
    forearm about its own axis to stand it up (`author_payo_audience_v2_elder_man_clips.py`)
Nothing of it is a hero's or another Classic's outfit (Rafi's bahag is teal with silver on a
tattooed chest; this is an old man's indigo, wrapped in a blanket).

THE CLASSIC RULES (classic-brief.md) all kept: the heroes' head at HEAD_SCALE 0.84 with its flat
face plane, ink eye blocks and one mouth stroke (painted), no nose, no wrinkles, no blush; chunky
layered hair in three tones by facing; a drawing only on a face square to its view; the rig is
character-male-e's seven bones plus `forearm-left` and `forearm-right`; the bare fist block at the
far end of the forearm with the build-time check; cloth across two bones bends (the bahag's flaps
take both legs, so seated they swing forward onto the lap; the blanket's shoulder caps take a
little of each arm; the neck shares head and torso); feet on zero; under 6,000 triangles.
THE FINISH: flat tones per form, the Classics' base, light and shade steps, no noise (owner,
2026-10-19).
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
import author_payo_audience_v2_elder_man_textures as tex  # noqa: E402
import author_payo_audience_v2_elder_man_clips as clips  # noqa: E402

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/character-male-e.glb")   # Mang Kanor's rig, read only
FOLDER = os.path.join(ROOT, "ArtSource/payo/audience/v2/elder_man")
OUT = os.path.join(FOLDER, "audience_v2_elder_man.glb")
BLEND = os.path.join(FOLDER, "audience_v2_elder_man.blend")
NAME = "audience_v2_elder_man"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
ELBOW_X = tex.ELBOW
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
HEAD_SCALE = 0.84
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))
HEIGHT_RANGE = (0.6613, 0.7928)    # the cast's
SHARP_ANGLE = 40.0
TRIANGLE_BUDGET = 6000
FIST_SHARE = 0.14

X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
AHEAD = (0, -1, 0)
BEHIND = (0, 1, 0)


# ---------------------------------------------------------------------------
# THE BLOCK (Mang Kanor's, unchanged)
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


def panel(name, a, b, window, direction, other):
    return ("panel", name, a, b, window, direction, other)


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
        if params is None:
            params = [i / float(max(count - 1, 1)) for i in range(count)]
        index = ALL_BONES.index(bone)
        widest = max(rings, key=lambda r: sum((r[j] - r[(j + 1) % n]).length for j in range(n)))
        run, across = 0.0, [0.0]
        for j in range(n):
            run += (widest[j] - widest[(j + 1) % n]).length
            across.append(run)
        across = [a / (run or 1.0) for a in across]

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
                uvp = [(across[j], params[i]), (across[j + 1], params[i]),
                       (across[j + 1], params[i + 1]), (across[j], params[i + 1])]
                self.jobs.append((f, spec(i, j), uvp, quad))
                faces.append(f)

        def fan(row, i, at, seg, v_at):
            centre = vert(at)
            for j in range(n):
                k = (j + 1) % n
                tri = (row[j], row[k], centre)
                f = bm.faces.new(tri)
                uvp = [(across[j], params[i]), (across[j + 1], params[i]), (0.5 * (across[j] + across[j + 1]), v_at)]
                self.jobs.append((f, spec(seg, j), uvp, tri))
                faces.append(f)

        if caps[0]:
            fan(rows[0], 0, sum(rings[0], Vector()) / n, 0, params[0])
        if tip is not None:
            fan(rows[-1], count - 1, tip, count - 2, 1.0)
        elif caps[1]:
            fan(rows[-1], count - 1, sum(rings[-1], Vector()) / n, count - 2, params[-1])
        bmesh.ops.recalc_face_normals(bm, faces=faces)
        self.pieces.append((name, sum(len(f.verts) - 2 for f in faces)))
        return faces

    def block(self, name, bone, u, v, stations, mapping, chamfer, move=None, caps=(True, True)):
        rings, params = [], []
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
            rings.append(station(first[0], shrink(first[1]), shrink(first[2]), chamfer * 0.4)); params.append(0.0)
            rings.append(station(Vector(first[0]) + along * c, first[1], first[2], chamfer)); params.append(c / length)
            for centre, hu, hv in stations[1:-1]:
                rings.append(station(centre, hu, hv, chamfer))
                params.append((Vector(centre) - Vector(first[0])).length / length)
            rings.append(station(Vector(last[0]) - along * c, last[1], last[2], chamfer)); params.append(1.0 - c / length)
            rings.append(station(last[0], shrink(last[1]), shrink(last[2]), chamfer * 0.4)); params.append(1.0)
        else:
            for centre, hu, hv in stations:
                rings.append(station(centre, hu, hv, 0.0))
                params.append((Vector(centre) - Vector(first[0])).length / length)
        if move is not None:
            rings = [[move(p) for p in r] for r in rings]
        return self.loft(name, bone, rings, mapping, caps=caps, params=params)

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
        for face, spec, uvp, verts in self.jobs:
            kind = spec[0]
            if kind == "tones":
                spec = flat(spec[1] if face.normal.z > 0.45 else (spec[3] if face.normal.z < -0.45 else spec[2]))
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
            elif kind == "panel" and face.normal.dot(Vector(spec[5])) <= 0.93:
                spec = flat(spec[6])
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
            elif kind == "flat":
                for loop in face.loops:
                    loop[self.uv].uv = tex.atlas_uv(spec[1], 0.5, 0.5)
            elif kind == "panel":
                _, name, a, b, (a0, a1, b0, b1), _d, _o = spec
                for loop in face.loops:
                    co = loop.vert.co
                    loop[self.uv].uv = tex.atlas_uv(name, (co[a] - a0) / (a1 - a0), (co[b] - b0) / (b1 - b0))
            else:
                raise SystemExit("unknown mapping " + str(spec))

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


def _table(rows, x):
    if x <= rows[0][0]:
        return rows[0][1]
    for (x0, v0), (x1, v1) in zip(rows, rows[1:]):
        if x <= x1:
            return v0 + (v1 - v0) * (x - x0) / (x1 - x0)
    return rows[-1][1]


SKIN_T = tones("skin_lit", "skin", "skin_shade")
HAIR_T = tones("hair_top", "hair", "hair_under")
HAIR_DEEP_T = tones("hair", "hair_under", "hair_under")
INDIGO_T = tones("indigo_lit", "indigo", "indigo_dark")
INDIGO_DEEP_T = tones("indigo", "indigo_dark", "indigo_deep")
MADDER_T = tones("madder_lit", "madder", "madder_dark")
COTTON_T = tones("cotton_lit", "cotton", "cotton_shade")
BRASS_T = tones("brass_lit", "brass", "brass_dark")
WOOD_T = tones("wood_lit", "wood", "wood_dark")
IRON_T = tones("iron_lit", "iron", "iron_dark")


# ---------------------------------------------------------------------------
# THE HEAD. The heroes' head, Dante's rows number for number, as Mang Kanor's.
# ---------------------------------------------------------------------------
HEAD_N = 24
HEAD_CY = 0.001
HEAD_ROWS = [
    (0.343, 0.104, 0.116, 0.108, 3.4),
    (0.357, 0.140, 0.144, 0.140, 3.8),
    (0.388, 0.164, 0.162, 0.156, 4.0),
    (0.428, 0.181, 0.165, 0.162, 4.2),
    (0.470, 0.175, 0.165, 0.162, 4.2),
    (0.520, 0.171, 0.165, 0.162, 4.2),
    (0.604, 0.169, 0.165, 0.162, 4.2),
    (0.646, 0.162, 0.158, 0.158, 3.8),
    (0.661, 0.140, 0.136, 0.140, 3.4),
]


def super_point(t, z, rx, rf, rb, e):
    c, s = math.cos(t), math.sin(t)
    a = math.copysign(abs(c) ** (2.0 / e), c)
    b = math.copysign(abs(s) ** (2.0 / e), s)
    return Vector((a * rx, HEAD_CY + b * (rb if b >= 0 else rf), z))


def super_ring(z, rx, rf, rb, e, n=HEAD_N):
    return [super_point(2.0 * math.pi * j / n, z, rx, rf, rb, e) for j in range(n)]


def build_head(part):
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], proj_square("head", "skin", "skin", 0.84))
    for s in (1, -1):
        # his ears, a little smaller than Mang Kanor's
        part.block("ear", "head", Y, Z, [((s * 0.166, 0.020, 0.468), 0.029, 0.040), ((s * 0.214, 0.028, 0.468), 0.021, 0.030)],
                   SKIN_T, 0.010)
        # THE BRASS EAR ORNAMENT: a short hook through the lobe and a flat drop below it, wider at
        # its foot, facing ahead so it reads from the front (32 by 30 mm before the head's scale)
        part.block("earring-hook", "head", X, Y, [((s * 0.206, 0.024, 0.432), 0.0065, 0.0065), ((s * 0.206, 0.024, 0.410), 0.0060, 0.0060)],
                   BRASS_T, 0.002)
        part.block("earring", "head", X, Y, [((s * 0.208, 0.024, 0.412), 0.011, 0.0065), ((s * 0.208, 0.024, 0.398), 0.016, 0.0070),
                                             ((s * 0.208, 0.024, 0.382), 0.014, 0.0065)], BRASS_T, 0.004)


# ---------------------------------------------------------------------------
# THE HAIR: GREY, the Cordillera BOWL CUT, in three tones by facing. A slab, a deeper mass down
# the back, a straight fringe of five locks under the head cloth, the level line of the bowl
# above his ears, and chunky locks on the crown standing out of the cloth at graded heights.
# THE HEAD CLOTH over it: two wraps, the indigo one on top standing 5 mm proud of the madder one
# under it, a knot over his right ear and two tails hanging behind it.
# ---------------------------------------------------------------------------
HAIR_CHAMFER = 0.012
#   THE CROWN above the cloth: a rounded CORE, and round its foot a RING of chunky locks lying
#   outward over the cloth's rim at graded heights, no two alike, and one lock at the front of
#   the crown flicked up (the silhouette's peak). v01 had seven lumps of one height on a flat
#   slab: from every side it read as a tray of stones ("lumps of one height read as a crate").
#       (name, base, tip, base half size (across, thick), tip half size)
CROWN = [
    ("crown-fl", (0.066, -0.084, 0.704), (0.118, -0.166, 0.684), (0.052, 0.021), (0.032, 0.012)),
    ("crown-fr", (-0.058, -0.090, 0.698), (-0.108, -0.170, 0.679), (0.048, 0.020), (0.030, 0.011)),
    ("crown-l", (0.104, 0.012, 0.700), (0.180, 0.024, 0.677), (0.056, 0.021), (0.034, 0.012)),
    ("crown-r", (-0.106, 0.024, 0.694), (-0.178, 0.034, 0.673), (0.050, 0.020), (0.030, 0.011)),
    ("crown-bl", (0.066, 0.100, 0.703), (0.112, 0.168, 0.681), (0.050, 0.020), (0.032, 0.012)),
    ("crown-br", (-0.060, 0.104, 0.708), (-0.098, 0.174, 0.686), (0.054, 0.021), (0.034, 0.012)),
    ("crown-b", (0.004, 0.120, 0.712), (0.006, 0.182, 0.690), (0.046, 0.019), (0.030, 0.011)),
]
#   the flick at the front of the crown
FLICK = ((0.012, -0.034, 0.736), (0.030, -0.098, 0.768), (0.050, 0.020), (0.030, 0.012))
#   down the back of the bowl, four locks over a deeper mass, their points at graded lengths
BACK_LOCKS = [(-0.126, 0.512), (-0.044, 0.492), (0.042, 0.500), (0.124, 0.518)]
#   beside each temple, two locks to the bowl's line above the ear: (y, tip z) for his left, his right
TEMPLE_LOCKS = {1: [(-0.050, 0.548), (0.078, 0.536)], -1: [(-0.046, 0.542), (0.082, 0.540)]}
BAND_LO = (0.604, 0.638, 0.193)      # the madder wrap: foot, top, half size
BAND_HI = (0.634, 0.670, 0.198)      # the indigo wrap over it


def build_hair(part):
    # the slab under the cloth, and the rounded core of the crown standing out of it
    part.block("hair-slab", "head", X, Y, [((0, 0.002, 0.598), 0.184, 0.183), ((0, 0.004, 0.662), 0.176, 0.176)],
               HAIR_T, HAIR_CHAMFER)
    part.block("hair-core", "head", X, Y, [((0, 0.004, 0.660), 0.160, 0.158), ((0, 0.004, 0.702), 0.140, 0.136),
                                           ((0, -0.004, 0.752), 0.090, 0.084)], HAIR_T, 0.032)
    part.block("hair-back", "head", X, Y, [((0, 0.160, 0.524), 0.170, 0.024), ((0, 0.160, 0.612), 0.180, 0.026)], HAIR_DEEP_T, HAIR_CHAMFER)
    for i, (x, tip) in enumerate(BACK_LOCKS):
        part.wedge("hair-back-lock", "head", (x, 0.184, 0.608), (x * 1.03, 0.190, tip), (0.046 - 0.003 * (i % 2), 0.014),
                   (0.026, 0.010), HAIR_T, 0.008)
    for s in (1, -1):
        for y, tip in TEMPLE_LOCKS[s]:
            part.wedge("hair-temple", "head", (s * 0.184, y, 0.606), (s * 0.190, y + 0.004, tip), (0.060, 0.013), (0.040, 0.009),
                       HAIR_T, 0.008, across=Y)
    for x, tip, half, tip_half in tex.FRINGE:
        part.block("hair-fringe", "head", X, Y, [((x, -0.177, tex.FRINGE_BASE), half, 0.018), ((x, -0.181, tip), tip_half, 0.011)],
                   HAIR_T, 0.006)
    for name, base, tip, base_half, tip_half in CROWN + [("crown-flick",) + FLICK]:
        out = Vector(tip) - Vector(base)
        across = Vector((-out.y, out.x, 0.0)).normalized() if name != "crown-flick" else X
        part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, HAIR_T, 0.010, across=across)

    # THE HEAD CLOTH
    lo, hi, h = BAND_LO
    part.block("cloth-wrap-red", "head", X, Y, [((0, 0.003, lo), h, h - 0.001), ((0, 0.003, hi), h - 0.001, h - 0.002)], MADDER_T, 0.030)
    lo, hi, h = BAND_HI
    part.block("cloth-wrap", "head", X, Y, [((0, 0.003, lo), h, h - 0.001), ((0, 0.003, hi), h - 0.004, h - 0.005)], INDIGO_T, 0.030)
    # the knot over his right ear, a little behind it, and the two tails hanging from it
    part.block("cloth-knot", "head", Y, Z, [((-0.198, 0.074, 0.644), 0.026, 0.024), ((-0.230, 0.078, 0.646), 0.020, 0.019)], INDIGO_T, 0.010)
    part.wedge("cloth-tail", "head", (-0.214, 0.090, 0.630), (-0.224, 0.126, 0.508), (0.022, 0.0075), (0.019, 0.0065), INDIGO_T, 0.004, across=Y)
    part.wedge("cloth-tail", "head", (-0.214, 0.060, 0.628), (-0.222, 0.070, 0.540), (0.018, 0.0070), (0.016, 0.0060), MADDER_T, 0.004, across=Y)
    # the woven end of the long tail: a cotton band, geometry
    part.block("cloth-tail-end", "head", Y, Z, [((-0.2245, 0.124, 0.530), 0.0215, 0.0085), ((-0.2245, 0.126, 0.514), 0.0205, 0.0080)],
               COTTON_T, 0.003, move=None)


# ---------------------------------------------------------------------------
# THE TORSO. A lean old man's bare chest (no belly: that was Mang Kanor's), his neck, the
# bahag's belt and flaps, and the blanket.
# ---------------------------------------------------------------------------
TORSO_ROWS = [(0.172, 0.102, 0.088, 0.024), (0.230, 0.106, 0.086, 0.020), (0.300, 0.112, 0.082, 0.018),
              (0.343, 0.098, 0.076, 0.018)]   # z, half width, half depth, middle y
ARM_Y, ARM_Z = 0.0173, tex.ARM_Z
BELT = (0.166, 0.206, 0.112, 0.098, 0.022)    # foot, top, half width, half depth, middle y


def chest_y(z):
    return _table([(r[0], r[3] - r[2]) for r in TORSO_ROWS], z)


def back_y(z):
    return _table([(r[0], r[3] + r[2]) for r in TORSO_ROWS], z)


def flap_weights(peak):
    """A bahag flap hangs between the legs: its foot takes BOTH legs equally (`peak` of it), so in
    a stride it barely moves and seated it swings forward with the thighs."""
    def weights(co):
        k = peak * _ramp(0.196, 0.080, co.z)
        return [("torso", 1.0 - k), ("leg-left", 0.5 * k), ("leg-right", 0.5 * k)]
    return weights


def cap_weights(co):
    """A shoulder cap of the blanket: its outer edge takes a little of the arm under it."""
    k = 0.35 * _ramp(0.100, 0.140, abs(co.x))
    return [("torso", 1.0 - k), ("arm-left" if co.x > 0 else "arm-right", k)]


def build_torso(part):
    part.block("torso", "torso", X, Y, [((0, cy, z), hw, hd) for z, hw, hd, cy in TORSO_ROWS], SKIN_T, 0.016)
    neck = part.block("neck", "torso", X, Y, [((0, 0.010, 0.326), 0.058, 0.054), ((0, 0.008, 0.356), 0.054, 0.050)],
                      flat("skin_shade"), 0.008)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.332, 0.350, co.z)), ("head", 0.7 * _ramp(0.332, 0.350, co.z))])

    # THE BAHAG. The cloth between the legs, the wrapped belt with a madder band round it, a fold
    # over the belt at the front, and the two flaps.
    part.block("bahag-seat", "torso", X, Y, [((0, 0.026, 0.126), 0.070, 0.058), ((0, 0.026, 0.176), 0.090, 0.068)], INDIGO_DEEP_T, 0.010)
    foot, top, hw, hd, cy = BELT
    part.block("belt", "torso", X, Y, [((0, cy, foot), hw, hd), ((0, cy, top), hw, hd)], INDIGO_T, 0.008)
    part.block("belt-band", "torso", X, Y, [((0, cy, 0.182), hw + 0.0035, hd + 0.0035), ((0, cy, 0.191), hw + 0.0035, hd + 0.0035)],
               MADDER_T, 0.003)
    front, back = cy - hd, cy + hd
    t, f, h0, h1 = tex.FLAP_FRONT
    part.block("flap-fold", "torso", X, Y, [((0, front - 0.006, 0.188), 0.038, 0.0055), ((0, front - 0.006, 0.212), 0.038, 0.0055)],
               INDIGO_T, 0.004)
    faces = part.block("flap-front", "torso", X, Y, [((0, front - 0.007, t), h0, 0.0045), ((0, front - 0.009, f + 0.004), h1, 0.0045)],
                       panel("flap_front", 0, 2, (-max(h0, h1), max(h0, h1), f, t), AHEAD, "indigo_dark"), 0.003)
    faces += part.block("flap-front-hem", "torso", X, Y, [((0, front - 0.0095, f), h1 + 0.002, 0.006), ((0, front - 0.0095, f + 0.011), h1 + 0.002, 0.006)],
                        COTTON_T, 0.003)
    part.bend(faces, flap_weights(0.9))
    t, f, h0, h1 = tex.FLAP_BACK
    faces = part.block("flap-back", "torso", X, Y, [((0, back + 0.006, t), h0, 0.0045), ((0, back + 0.008, f + 0.004), h1, 0.0045)],
                       panel("flap_back", 0, 2, (max(h0, h1), -max(h0, h1), f, t), BEHIND, "indigo_dark"), 0.003)
    faces += part.block("flap-back-hem", "torso", X, Y, [((0, back + 0.0085, f), h1 + 0.002, 0.006), ((0, back + 0.0085, f + 0.011), h1 + 0.002, 0.006)],
                        COTTON_T, 0.003)
    part.bend(faces, flap_weights(0.15))

    # THE BLANKET. The back plate stands clear of the belt and the back flap; its stripes and
    # diamonds are its own swatch, its hem is cotton piping with a madder band over it (geometry).
    z0, z1 = tex.BLANKET_BACK
    w0, w1 = tex.BLANKET_BACK_HALF
    plate_y = lambda z: max(back_y(z) + 0.016, 0.142 if z < 0.22 else 0.0)
    zs = [z0, 0.214, 0.280, z1]
    part.block("blanket-back", "torso", X, Y, [((0, plate_y(z), z), w0 + (w1 - w0) * (z - z0) / (z1 - z0), 0.0085) for z in zs],
               panel("blanket_back", 0, 2, (-0.140, 0.140, z0, z1), BEHIND, "indigo_dark"), 0.005)
    part.block("blanket-hem", "torso", X, Y, [((0, plate_y(z0) + 0.003, z0 - 0.004), w0 + 0.003, 0.0105),
                                              ((0, plate_y(z0) + 0.003, z0 + 0.012), w0 + 0.003, 0.0105)], COTTON_T, 0.004)
    part.block("blanket-hem-band", "torso", X, Y, [((0, plate_y(z0) + 0.002, z0 + 0.0135), w0 + 0.0015, 0.0098),
                                                   ((0, plate_y(z0) + 0.002, z0 + 0.0215), w0 + 0.0015, 0.0098)], MADDER_T, 0.002)
    for s in (1, -1):
        # the shoulder cap, front to back over the top of the shoulder and the root of the arm
        cap = part.block("blanket-cap", "torso", X, Z, [((s * 0.094, chest_y(0.343) - 0.018, 0.351), 0.046, 0.0125),
                                                        ((s * 0.094, back_y(0.343) + 0.022, 0.351), 0.046, 0.0125)], INDIGO_T, 0.008)
        part.bend(cap, cap_weights)
        # the front panel down the side of his chest; its stripes run down it
        a0, a1 = tex.BLANKET_FRONT_X
        cx, hw = s * 0.5 * (a0 + a1), 0.5 * (a1 - a0)
        fz0, fz1 = tex.BLANKET_FRONT
        fy = lambda z: chest_y(z) - 0.020
        win = (s * a0, s * a1, fz0, fz1)
        part.block("blanket-front", "torso", X, Y, [((cx, fy(z), z), hw, 0.0075) for z in (fz0, 0.270, fz1)],
                   panel("blanket_front", 0, 2, win, AHEAD, "indigo_dark"), 0.004)
        # its inner edge, piped in cotton with a madder band beside it (geometry), and its hem
        part.block("blanket-edge", "torso", X, Y, [((s * 0.047, fy(z) - 0.003, z), 0.0075, 0.0090) for z in (fz0 - 0.008, 0.270, fz1)],
                   COTTON_T, 0.003)
        part.block("blanket-edge-band", "torso", X, Y, [((s * 0.0595, fy(z) - 0.0015, z), 0.0045, 0.0080) for z in (fz0 - 0.004, 0.270, fz1)],
                   MADDER_T, 0.002)
        part.block("blanket-front-hem", "torso", X, Y, [((cx + s * 0.002, fy(fz0) - 0.002, fz0 - 0.008), hw + 0.002, 0.0090),
                                                        ((cx + s * 0.002, fy(fz0) - 0.002, fz0 + 0.004), hw + 0.002, 0.0090)], COTTON_T, 0.003)


# ---------------------------------------------------------------------------
# THE ARMS. Bare, thin, an old man's, at the heroes' length; the batok on the forearm islands.
# ---------------------------------------------------------------------------

def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    skin = proj_square(group, "skin", "skin_shade", 0.90, ends=(0, "skin_shade"))
    # the upper arm takes flat skin: the forearm islands carry the batok, and a drawing on the
    # upper arm where it overlaps the forearm at the elbow smeared when the elbow bent (v01)
    arm_block(part, "arm", bone, s, [(0.098, (0.046, 0.048)), (ELBOW_X + 0.014, (0.042, 0.044))], SKIN_T, 0.012)
    arm_block(part, "forearm", fore, s, [(ELBOW_X - 0.014, (0.044, 0.046)), (tex.WRIST + 0.004, (0.041, 0.043))], skin, 0.012)
    arm_block(part, "hand", fore, s, [(tex.WRIST, (0.0555, 0.0555)), (tex.HAND_END, (0.0555, 0.0555))],
              proj_square(group, "skin", "skin_shade", 0.93), 0.014)


# ---------------------------------------------------------------------------
# THE LEGS. Bare and thin, and BAREFOOT: a foot block, a lower step for the toes and a big toe of
# its own on the inside. Feet straight ahead.
# ---------------------------------------------------------------------------
LEG_X = 0.080


def build_leg(part, s):
    bone = "leg-left" if s > 0 else "leg-right"
    x = s * LEG_X
    part.block("leg", bone, X, Y, [((x, 0.024, 0.034), 0.040, 0.046), ((x, 0.026, 0.184), 0.047, 0.054)], SKIN_T, 0.012)
    part.block("foot", bone, X, Z, [((x, -0.074, 0.022), 0.047, 0.022), ((x, 0.064, 0.025), 0.045, 0.025)], SKIN_T, 0.010)
    part.block("toes", bone, X, Z, [((x - s * 0.006, -0.108, 0.0125), 0.039, 0.0125), ((x - s * 0.006, -0.070, 0.0130), 0.040, 0.0130)],
               SKIN_T, 0.006)
    part.block("big-toe", bone, X, Z, [((x - s * 0.034, -0.118, 0.0140), 0.0135, 0.0140), ((x - s * 0.034, -0.080, 0.0150), 0.0140, 0.0150)],
               SKIN_T, 0.006)


# ---------------------------------------------------------------------------
# THE SPEAR: its own mesh, on `forearm-right`. Built in the rest pose straight up through the
# middle of his right fist (x -0.266, the fist's middle), square to the forearm. The butt is
# GRIP_DROP below the fist's middle, so with the forearm level and the shaft upright his fist
# is that high when the butt is on the ground.
# ---------------------------------------------------------------------------
SPEAR_X = -0.5 * (tex.WRIST + tex.HAND_END)
GRIP_DROP = clips.GRIP_DROP
SHAFT_R = 0.0115


def build_spear(part):
    bone = "forearm-right"
    zb = ARM_Z - GRIP_DROP
    c = (SPEAR_X, ARM_Y)
    at = lambda z: (c[0], c[1], z)
    # the iron butt cap, pointed a little
    part.block("spear-butt", bone, X, Y, [(at(zb), 0.0075, 0.0075), (at(zb + 0.024), SHAFT_R + 0.0015, SHAFT_R + 0.0015)], IRON_T, 0.004)
    # the hardwood shaft, octagonal, slimming a touch toward the blade
    part.block("spear-shaft", bone, X, Y, [(at(zb + 0.020), SHAFT_R, SHAFT_R), (at(zb + 0.80), SHAFT_R - 0.0015, SHAFT_R - 0.0015)],
               WOOD_T, 0.0045)
    # the brass binding ring and the iron socket
    top = zb + 0.80
    part.block("spear-ring", bone, X, Y, [(at(top - 0.020), SHAFT_R + 0.003, SHAFT_R + 0.003), (at(top - 0.006), SHAFT_R + 0.003, SHAFT_R + 0.003)],
               BRASS_T, 0.003)
    part.block("spear-socket", bone, X, Y, [(at(top - 0.008), SHAFT_R + 0.0005, SHAFT_R + 0.0005), (at(top + 0.036), 0.0075, 0.0075)],
               IRON_T, 0.003)
    # the leaf-shaped blade, broad across Y in the rest pose (sideways once the shaft stands up in
    # his hand, so it reads as a leaf from the front), a ridge down its middle
    part.block("spear-blade", bone, Y, X, [(at(top + 0.030), 0.010, 0.0060), (at(top + 0.075), 0.028, 0.0070),
                                           (at(top + 0.125), 0.021, 0.0060), (at(top + 0.165), 0.003, 0.0025)], IRON_T, 0.004)
    part.block("spear-ridge", bone, Y, X, [(at(top + 0.032), 0.0040, 0.0085), (at(top + 0.150), 0.0025, 0.0045)], tones("iron_lit", "iron_dark", "iron_dark"), 0.002)


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


def write_glb(body, head, spear, out):
    """character-male-e.glb with its meshes swapped: skeleton, bind matrices and the clips this
    file does not write copied untouched, two elbows appended, a third mesh (the spear) on the
    same skin, his own clips in place of the ones of the same name and the new ones appended."""
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
    for skin in gltf["skins"]:
        skin["inverseBindMatrices"] = keep(skin["inverseBindMatrices"])
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
    present = set()
    for anim in gltf["animations"]:
        present.add(anim["name"])
    for name in clips.CLIPS:
        if name not in present:
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
    by_name["body-mesh"]["primitives"] = primitive(body)
    by_name["head-mesh"]["primitives"] = primitive(head)
    # THE SPEAR: a third mesh and node, skinned to the body's skin (its one joint is forearm-right)
    gltf["meshes"].append({"name": "spear-mesh", "primitives": primitive(spear)})
    body_node = node_of["body-mesh"]
    gltf["nodes"].append({"name": "spear-mesh", "mesh": len(gltf["meshes"]) - 1, "skin": gltf["nodes"][body_node]["skin"]})
    gltf["nodes"][0].setdefault("children", []).append(len(gltf["nodes"]) - 1)

    gltf["accessors"] = accessors
    gltf["bufferViews"] = views
    gltf["buffers"] = [{"byteLength": len(blob)}]
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso Payo audience v2 (elder_man)"}
    gltf["extras"] = {"payo_audience": "v2", "id": "elder_man", "seat": "the sit clip puts the underside of his hips at the root's origin"}
    gltf["images"] = [{"uri": tex.ATLAS_NAME, "name": NAME + "-atlas"}]
    gltf["textures"] = [{"sampler": 0, "source": 0, "name": NAME + "-atlas"}]
    gltf["samplers"] = [{"magFilter": 9729, "minFilter": 9987}]
    gltf["materials"][0]["name"] = NAME
    gltf["materials"][0]["doubleSided"] = False
    gltf["nodes"][0]["name"] = NAME
    gltf["scenes"][0]["name"] = NAME
    bpv.write_glb(out, gltf, blob)


def verify(objects):
    tris = 0
    for obj in objects:
        obj.data.calc_loop_triangles()
        tris += len(obj.data.loop_triangles)
    lo = Vector((1e9, 1e9, 1e9))
    hi = -lo
    for obj in objects[:2]:     # the figure; the spear stands above him
        for v in obj.data.vertices:
            for a in range(3):
                lo[a] = min(lo[a], v.co[a])
                hi[a] = max(hi[a], v.co[a])
    print("bounds  x %.3f..%.3f  y %.3f..%.3f  z %.3f..%.3f" % (lo.x, hi.x, lo.y, hi.y, lo.z, hi.z))
    print("HEIGHT %.4f   triangles %d  (budget %d; spear %d)" % (hi.z, tris, TRIANGLE_BUDGET, len(objects[2].data.loop_triangles)))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if not HEIGHT_RANGE[0] <= hi.z <= HEIGHT_RANGE[1]:
        raise SystemExit("height %.4f is outside the cast's range" % hi.z)
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
        near, far = min(p.x * sign for p in arm), max(p.x * sign for p in arm)
        if abs(far - max(p.x * sign for p in fore)) > 1e-6:
            raise SystemExit("the far end of arm-%s is not on the forearm bone" % side)
        hand = [p for p in arm if p.x * sign - near >= (far - near) * (1.0 - FIST_SHARE)]
        fist = max(max(p[a] for p in hand) - min(p[a] for p in hand) for a in (1, 2))
        widest = max(size[1], size[2])
        top = max(p.z for p in hand) - ARM_Z
        print("hand %-5s arm x %.4f to %.4f, far %d%% is %.4f across (widest section %.4f), top %.4f above the arm bone"
              % (side, near, far, round(100 * FIST_SHARE), fist, widest, top))
        if not 0.045 <= top <= 0.066:
            raise SystemExit("the hand's top moved")
        if abs(far - tex.HAND_END) > 0.003:
            raise SystemExit("the hand's far end moved off %.3f" % tex.HAND_END)
        if abs(fist - 0.111) > 0.004:
            raise SystemExit("the far %d%% of the arm is %.4f across, not the fist's 0.111" % (round(100 * FIST_SHARE), fist))
        if fist <= 0.5 * widest:
            raise SystemExit("the fist is under half the arm's widest section")


def build(armature, material):
    body, head, spear = Part("body-mesh"), Part("head-mesh"), Part("spear-mesh")
    build_torso(body)
    build_arm(body, 1)
    build_arm(body, -1)
    build_leg(body, 1)
    build_leg(body, -1)
    build_head(head)
    build_hair(head)
    build_spear(spear)
    objects = []
    for part in (body, head, spear):
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

    image_path = os.path.join(FOLDER, tex.ATLAS_NAME)
    if not os.path.exists(image_path):
        raise SystemExit("paint the atlas first: py -3 tools/author_payo_audience_v2_elder_man_textures.py")
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
    _retry(lambda: write_glb(arrays[0], arrays[1], arrays[2], OUT))
    print("wrote " + OUT)
    image.filepath = bpy.path.relpath(image_path, start=os.path.dirname(BLEND))
    bpy.context.preferences.filepaths.save_version = 0
    _retry(lambda: bpy.ops.wm.save_as_mainfile(filepath=BLEND))
    print("wrote " + BLEND)


if __name__ == "__main__":
    main()
