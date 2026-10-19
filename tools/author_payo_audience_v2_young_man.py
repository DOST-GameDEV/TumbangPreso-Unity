"""Build the Payo audience villager `young_man`: a young Ifugao farmer-warrior, modelled EXACTLY
the way the Classic street people were redesigned.

    py -3 tools/author_payo_audience_v2_young_man_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_payo_audience_v2_young_man.py
    ... -- --out other.glb     (writes there instead and leaves the .blend alone; needs the atlas beside it)

Writes
    ArtSource/payo/audience/v2/young_man/audience_v2_young_man.glb   (rigged, every clip)
    ArtSource/payo/audience/v2/young_man/audience_v2_young_man.blend
beside the atlas painted by the textures script. NOTHING IN THE GAME LOADS IT YET: nothing is
written under Assets/; the Payo map session imports it.

WHY. The v1 audience (tools/author_payo_audience.py, Blender box figures) was rejected by the
owner: "you need to actually model them in the way we've modeled the rest of the characters".
docs/reports/character-redesign/payo-audience-brief.md: each villager is built like a Classic
redesign, same rules, rig, paint and clip machinery. This is a COPY of the Kuya Boy redesign's
author script (tools/author_character_redesign_kuya_boy.py, the base Classic), its generic
machinery kept line for line and its character rewritten; Rafi's script was read for how a
bahag and batok were built, nothing is copied from his design. Those files are left alone.

WHO HE IS. A young Ifugao man, a farmer-warrior from the terraces, in the crowd on the basin
wall. He must belong beside the heroes and the Classics, read as precolonial Cordillera at a
glance, and NOT read as Rafi (the cast's Visayan islander with a bahag and tattoos):
  * HAIR: the Ifugao BOWL CROP, black: a rounded cap, a blunt fringe of six chunky locks of
    slightly different lengths, the sides cut level above the ears, the back cut level at the
    nape, and a ring of low chunky tufts on the crown above the band. Rafi's is a big curly mop
    with a teal putong round it; Bayan's a flat-top with spikes. Three tones by facing;
  * A WOVEN RATTAN BAND round the hair with ONE UPRIGHT FEATHER tucked in it over his left ear,
    cream with a black tip: the one thing that stands up out of his silhouette in a crowd;
  * BARE CHEST with his batok (the textures script plans its six routes); a lean build: the
    torso is narrower than Rafi's (0.122 against 0.132 at the shoulder), the pectorals 7 mm proud;
  * THE BAHAG: an indigo-black wrapped waist with thin white and madder bands, a WRAPPED BELT of
    two rattan cords over it tied in a KNOT at the front (his one focal fastening; Rafi's is a
    silver medallion), a front flap that flares to its hem and a shorter back flap, each with
    the Ifugao bands and a cream hem built as geometry;
  * THE PINUHHA: a small woven rattan bag at his right hip on a dark fibre cord that runs from
    it up across his chest, over his LEFT shoulder and down his back;
  * a BRASS ARMLET on his left upper arm, his one metal;
  * BAREFOOT: proper foot blocks on zero, the toes a slight step.
No teal, no silver, no gold trim, no costume. Colour is quiet: indigo, undyed cotton, rattan,
red only in thin bands.

and still (classic-brief): head 0.84 about the head joint after the UVs resolve; a drawing only
on a face that squarely faces its view (`proj_square`); no painted hair shine; cloth across two
bones bends (the flaps, the knot's ends and the bag take the legs; the neck takes the head);
feet on zero; under 6,000 triangles; the rig's seven bones plus `forearm-left` and
`forearm-right`; the fist is a bare block at the far end of the forearm (the far 14 per cent of
the arm), with the base's build-time check kept.
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
import author_payo_audience_v2_young_man_textures as tex  # noqa: E402  the island layout
import author_payo_audience_v2_young_man_clips as clips  # noqa: E402  his own clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/character-male-b.glb")   # read only
FOLDER = os.path.join(ROOT, "ArtSource/payo/audience/v2/young_man")
OUT = os.path.join(FOLDER, "audience_v2_young_man.glb")
BLEND = os.path.join(FOLDER, "audience_v2_young_man.blend")
NAME = "audience_v2_young_man"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
ELBOW_X = tex.ELBOW_X
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
HEAD_SCALE = 0.84
_ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))   # the `head` bone of character-male-b.glb, in Blender space
FEATHER_TOP = 0.856                         # the feather's tip, before the head's scale
HAIR_TOP = 0.754                            # the crown's tallest tuft, before the head's scale
HEIGHT = HEAD_JOINT.z + (FEATHER_TOP - HEAD_JOINT.z) * HEAD_SCALE     # 0.774, the feather's tip
HAIR_HEIGHT = HEAD_JOINT.z + (HAIR_TOP - HEAD_JOINT.z) * HEAD_SCALE   # 0.673, the top of his hair
SHARP_ANGLE = 40.0
TRIANGLE_BUDGET = 6000
FIST_SHARE = 0.14
FIST_ACROSS = 0.116

X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))


# ---------------------------------------------------------------------------
# THE BLOCK. A ring is a rectangle with its four corners cut; a block is rings skinned in order.
# ---------------------------------------------------------------------------

def _pair(h):
    return h if isinstance(h, tuple) else (h, h)


def rect_ring(centre, u, v, hu, hv, chamfer):
    """Eight points: a rectangle `hu` by `hv` (a half size, or a (negative, positive) pair) with
    each corner cut back `chamfer`. A chamfer of zero gives the four plain corners."""
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


def swatch(name, u=(0.0, 1.0), v=(0.0, 1.0), along="rings"):
    """u round the ring and v along the block, or (along="columns") u along it and v round it."""
    return ("swatch", name, u, v, along)


def flat(name):
    return ("flat", name)


def proj_except(group, axis, name):
    """The group's islands, but a face turned along `axis` (0 x, 1 y, 2 z) takes one flat tone.

    A block's END faces look along its own length, where the group has drawn something else:
    a cuff's end would land on the fingertips' island, a belt's ledge on the shoulders'.
    """
    return ("except", group, axis, name)


def proj_square(group, flat_name, under_name, limit=0.93, ends=None):
    """The group's islands ONLY for a face that squarely faces one of its views (the cosine of
    the angle between them is at least `limit`); every chamfer, rounded corner and slope takes
    the flat tone `flat_name` (`under_name` if it is turned down). `ends` = (axis, name) sends
    the faces turned along that axis, the block's two ends and their chamfers, to a tone of
    their own.

    WHY (section 15.3 rule 8). An island is a flat orthographic drawing. A face that meets its
    view at a glancing angle stretches that drawing along itself. Owner, 2026-10-05, circling a
    smear on Sean's fist in the game: "strange texturing artifact". A drawing belongs on the
    face it was drawn for and nowhere else.
    """
    return ("square", group, flat_name, under_name, limit, ends)


def tones(top, side, under):
    """No drawing at all: one flat tone for faces turned up, one for the sides, one for under."""
    return ("tones", top, side, under)


def panel(name, a, b, window, direction, other):
    """A drawn swatch laid flat on the faces turned toward `direction`: (a, b) are the two model
    axes it is seen along and `window` the metres it spans. Every other face, the piece's
    chamfers included, takes a flat tone (section 15.3 rule 8)."""
    return ("panel", name, a, b, window, direction, other)


def facing(direction, front, other):
    """One flat tone for the faces turned toward `direction`, another for the rest."""
    return ("flatfacing", direction, front, other)


class Part:
    """One of the two meshes the rig carries (body-mesh, head-mesh)."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.bone = self.bm.verts.layers.int.new("bone")
        self.jobs = []      # (face, mapping, swatch params, the vertices those params belong to)
        self.pieces = []    # (name, triangles)
        self.blend = {}     # vertex -> [(bone, weight)], for the few pieces that BEND

    def loft(self, name, bone, rings, mapping, caps=(True, True), tip=None, params=None):
        """Skin `rings` in order. `mapping` is one spec or f(segment, column) -> spec.

        `caps` closes the first and last ring with a fan; `tip` draws the last ring to a point
        instead. `params` are the rings' positions along a swatch, 0..1 (even by default).
        """
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

        # Whichever way the rings were wound, the shell faces out.
        bmesh.ops.recalc_face_normals(bm, faces=faces)
        self.pieces.append((name, sum(len(f.verts) - 2 for f in faces)))
        return faces

    def block(self, name, bone, u, v, stations, mapping, chamfer, move=None, caps=(True, True)):
        """A chamfered block through `stations` = [(centre, hu, hv)], its two ends chamfered too.

        Two stations of one size is a box. Different sizes taper it; moving the second centre
        off the axis leans it. `move` is applied to every point (a foot's turn-out, a flower's).
        """
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
        """Make a piece soft: `weights(position)` gives [(bone, weight)] for each of its vertices.

        The cast is rigid, one bone a vertex (Voxel_Person_Guide.md), and so is this model except
        where cloth or hair lies ACROSS two bones. Owner, 2026-10-05, on Dante: "make it so the
        clothes will bend/distort to follow his body".
        """
        for f in faces:
            for v in f.verts:
                self.blend[v] = weights(v.co)

    def wedge(self, name, bone, base, tip, base_half, tip_half, mapping, chamfer, across=X):
        """A block standing on `base` and narrowing to `tip`: a hair lock, a loose cloth end."""
        w = (Vector(tip) - Vector(base)).normalized()
        u = (across - w * across.dot(w)).normalized()
        v = w.cross(u).normalized()
        return self.block(name, bone, u, v, [(base, base_half[0], base_half[1]), (tip, tip_half[0], tip_half[1])],
                          mapping, chamfer)

    def resolve_uvs(self):
        self.bm.normal_update()
        for face, spec, uvp, verts in self.jobs:
            kind = spec[0]
            if kind == "except":
                spec = flat(spec[3]) if abs(face.normal[spec[2]]) > 0.6 else proj(spec[1])
                kind = spec[0]
            elif kind == "tones":
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
                _, name, ur, vr, along = spec
                # recalc_face_normals may have reversed the loop order; pair params by vertex.
                order = {v: p for v, p in zip(verts, uvp)}
                for loop in face.loops:
                    p, q = order[loop.vert]
                    if along == "columns":
                        p, q = q, p
                    loop[self.uv].uv = tex.atlas_uv(name, ur[0] + (ur[1] - ur[0]) * p, vr[0] + (vr[1] - vr[0]) * q)

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
    """0 at `a`, 1 at `b`, clamped."""
    return max(0.0, min(1.0, (x - a) / (b - a)))


def _smooth(t):
    return t * t * (3.0 - 2.0 * t)


def _table(rows, x):
    """Straight lines through hand-set (x, value) rows."""
    if x <= rows[0][0]:
        return rows[0][1]
    for (x0, v0), (x1, v1) in zip(rows, rows[1:]):
        if x <= x1:
            return v0 + (v1 - v0) * (x - x0) / (x1 - x0)
    return rows[-1][1]


# ---------------------------------------------------------------------------
# THE HEAD. THE HEROES' HEAD: Dante's `shaped` rows, number for number, as Kuya Boy's is.
#     rows are (z, half width, depth to the front, depth to the back, exponent)
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
AHEAD = (0, -1, 0)
BEHIND = (0, 1, 0)
SKIN_T = tones("skin_lit", "skin", "skin_shade")
HAIR_T = tones("hair_top", "hair", "hair_deep")
HAIR_UNDER = tones("hair", "hair_deep", "hair_deep")       # the masses underneath, one step deeper
RATTAN_T = tones("rattan_lit", "rattan", "rattan_dark")
INDIGO_T = tones("indigo_lit", "indigo", "indigo_dark")
WHITE_T = tones("white", "white", "white_shade")
CORD_T = tones("cord_lit", "cord", "cord_dark")
BRASS_T = tones("brass_lit", "brass", "brass_dark")


def super_point(t, z, rx, rf, rb, e):
    """A point of a superellipse ring. `t` in radians: 0 is his left side, -90 degrees his front."""
    c, s = math.cos(t), math.sin(t)
    a = math.copysign(abs(c) ** (2.0 / e), c)
    b = math.copysign(abs(s) ** (2.0 / e), s)
    return Vector((a * rx, HEAD_CY + b * (rb if b >= 0 else rf), z))


def super_ring(z, rx, rf, rb, e, n=HEAD_N):
    return [super_point(2.0 * math.pi * j / n, z, rx, rf, rb, e) for j in range(n)]


def slab(part, name, bone, centre, u, v, hu, hv, back, front, paint, cut=0.004, corner=0.0, normal=AHEAD):
    """A plate lying on a surface that faces `normal`: a ring `back` behind `centre`, its face
    `front` ahead of it with the edge cut. Three rings, no back face (it is sunk in what it lies on)."""
    n = Vector(normal)
    c = Vector(centre)
    rings = [rect_ring(c - n * back, u, v, hu, hv, corner),
             rect_ring(c + n * (front - cut), u, v, hu, hv, corner),
             rect_ring(c + n * front, u, v, hu - cut, hv - cut, max(corner - 0.4 * cut, 0.0))]
    return part.loft(name, bone, rings, paint, caps=(False, True))



def build_head(part):
    # Only the flat front takes the drawing (his face); every other face is flat skin.
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], proj_square("head", "skin", "skin", 0.84))
    # NO NOSE. His ears are the heroes' size (Kuya Boy's big ears are Kuya Boy's).
    for s in (1, -1):
        part.block("ear", "head", Y, Z, [((s * 0.166, 0.016, 0.458), 0.028, 0.040), ((s * 0.214, 0.024, 0.458), 0.021, 0.030)],
                   SKIN_T, 0.009)


# ---------------------------------------------------------------------------
# THE HAIR: THE IFUGAO BOWL CROP, built the way the heroes' hair is (a core, and chunky pieces
# set by hand on it at graded heights). No drawing on any of it: three tones by facing, the
# masses underneath one step deeper.
#   the cap: one rounded slab over the crown, down to the fringe's root;
#   the sides: a panel each side cut LEVEL above the ear; the back: a mass cut level at the nape;
#   the fringe: six blunt locks across the brow, each its own width and length (a bowl crop is
#     level, so they differ by millimetres, not by spikes);
#   over each ear two short locks, down the back four, each its own length;
#   on the crown, above the band, a ring of six low chunky tufts at graded heights.
# ---------------------------------------------------------------------------
#   (x, tip z, half width): left to right across his brow
FRINGE = [(-0.150, 0.574, 0.031), (-0.092, 0.566, 0.033), (-0.032, 0.572, 0.032), (0.028, 0.567, 0.033),
          (0.086, 0.575, 0.031), (0.144, 0.569, 0.032)]
#   (name, base, tip, base half size, tip half size)
TUFTS = [
    ("crown-c", (0.000, 0.004, 0.690), (0.004, -0.004, HAIR_TOP), (0.078, 0.078), (0.050, 0.048)),
    ("crown-fl", (0.090, -0.090, 0.688), (0.104, -0.106, 0.736), (0.064, 0.054), (0.038, 0.032)),
    ("crown-fr", (-0.086, -0.094, 0.688), (-0.100, -0.110, 0.742), (0.062, 0.052), (0.036, 0.030)),
    ("crown-bl", (0.094, 0.090, 0.688), (0.108, 0.104, 0.730), (0.062, 0.058), (0.038, 0.034)),
    ("crown-br", (-0.096, 0.086, 0.688), (-0.110, 0.100, 0.738), (0.060, 0.060), (0.036, 0.036)),
    ("crown-b", (0.004, 0.122, 0.688), (0.004, 0.140, 0.724), (0.072, 0.042), (0.046, 0.026)),
]
BACK_LOCKS = [(-0.122, 0.494, 0.044), (-0.042, 0.484, 0.046), (0.040, 0.490, 0.045), (0.120, 0.498, 0.043)]
BAND_Z = (0.596, 0.634)   # low, across the fringe's roots, as the Cordillera band is worn


def build_hair(part):
    part.block("hair-cap", "head", X, Y, [((0, 0.004, 0.580), 0.184, (0.180, 0.178)), ((0, 0.004, 0.702), 0.178, (0.174, 0.172))],
               HAIR_T, 0.030)
    part.block("hair-back", "head", X, Y, [((0, 0.158, 0.500), 0.166, 0.016), ((0, 0.160, 0.610), 0.176, 0.018)], HAIR_UNDER, 0.012)
    for s in (1, -1):
        # the side, cut level above the ear (his ear stands clear of it)
        part.block("hair-side", "head", X, Y, [((s * 0.180, 0.022, 0.520), 0.013, (0.128, 0.132)), ((s * 0.182, 0.022, 0.604), 0.014, (0.134, 0.136))],
                   HAIR_UNDER, 0.010)
    # the fringe: blunt locks, full width down to just above their tips, each tip cut square
    for x, tip, half in FRINGE:
        part.block("hair-fringe", "head", X, Y, [((x, -0.176, 0.642), half, 0.017), ((x, -0.178, tip + 0.012), half, 0.016),
                                                 ((x, -0.180, tip), half - 0.004, 0.013)], HAIR_T, 0.006)
    for s in (1, -1):
        part.wedge("hair-temple", "head", (s * 0.186, -0.082, 0.604), (s * 0.188, -0.086, 0.526 if s > 0 else 0.520), (0.012, 0.030), (0.010, 0.024),
                   HAIR_T, 0.006)
        part.wedge("hair-temple", "head", (s * 0.186, 0.064, 0.604), (s * 0.188, 0.066, 0.512 if s > 0 else 0.518), (0.012, 0.034), (0.010, 0.026),
                   HAIR_T, 0.006)
    for x, tip, half in BACK_LOCKS:
        part.wedge("hair-backlock", "head", (x, 0.178, 0.620), (x * 1.03, 0.181, tip), (half, 0.011), (half - 0.008, 0.009), HAIR_T, 0.007)
    for name, base, tip, base_half, tip_half in TUFTS:
        part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, HAIR_T, 0.010)

    # THE BAND: woven rattan round the hair, its own drawn swatch (a lit top, a dark foot, one
    # madder thread). Four rings: the foot, the outer wall, the top; the inside is in the hair.
    z0, z1 = BAND_Z
    inner = lambda z: rect_ring((0, 0.004, z), X, Y, 0.172, (0.168, 0.166), 0.020)
    # (it stands OUTSIDE the fringe's locks, which reach y -0.193, so it reads across the brow)
    outer = lambda z: rect_ring((0, 0.004, z), X, Y, 0.198, (0.203, 0.192), 0.036)
    part.loft("band", "head", [inner(z0), outer(z0), outer(z1), inner(z1)], swatch("band"), caps=(False, False),
              params=[0.0, 0.0, 1.0, 1.0])
    # THE FEATHER, tucked in the band over his left ear and standing up out of it: cream, its tip
    # black. Flat to the front, so a crowd seen from the field sees its whole width.
    cream = tones("feather", "feather", "feather_shade")
    part.block("feather", "head", X, Y, [((0.176, 0.060, 0.600), 0.008, 0.0040), ((0.179, 0.064, 0.700), 0.022, 0.0045),
                                         ((0.183, 0.070, 0.790), 0.021, 0.0040)], cream, 0.003)
    part.block("feather-tip", "head", X, Y, [((0.183, 0.070, 0.788), 0.0212, 0.0042), ((0.186, 0.074, 0.830), 0.015, 0.0036),
                                             ((0.188, 0.077, FEATHER_TOP), 0.004, 0.0020)], tones("hair_top", "hair", "hair_deep"), 0.002)
    # the quill where it enters the band, a short dark stub
    part.block("feather-quill", "head", X, Y, [((0.1755, 0.0595, 0.592), 0.0030, 0.0030), ((0.1765, 0.0610, 0.616), 0.0030, 0.0030)],
               flat("rattan_deep"), 0.001)


# ---------------------------------------------------------------------------
# THE TORSO. Bare and lean, narrowing to a waist; the pectorals as form, 7 mm proud.
#     rows are (z, half width, half depth)
# ---------------------------------------------------------------------------
TORSO_CY = 0.000
TORSO_ROWS = [(0.176, 0.098, 0.080), (0.222, 0.100, 0.080), (0.300, 0.118, 0.083), (0.343, 0.122, 0.081)]
ARM_Y, ARM_Z = 0.006, tex.ARM_Z
LEG_X = 0.0836
LEG_Y = 0.004
WAIST_PROUD = 0.010
CORD_PROUD = 0.014


def torso_half(z):
    for (z0, w0, d0), (z1, w1, d1) in zip(TORSO_ROWS, TORSO_ROWS[1:]):
        if z <= z1 or z1 == TORSO_ROWS[-1][0]:
            f = (z - z0) / (z1 - z0)
            return (w0 + (w1 - w0) * f, d0 + (d1 - d0) * f)


def chest_y(z):
    return TORSO_CY - torso_half(z)[1]


def back_y(z):
    return TORSO_CY + torso_half(z)[1]


FLAP_FOLLOW = 0.62


def flap_weights(co):
    """A flap hangs from the waist (torso) and its hem goes with the legs, each half mostly with
    its own leg and a quarter with the other, so it stays a flap in a stride (Rafi's rule)."""
    down = _ramp(0.186, 0.085, co.z) ** 0.8
    leg = FLAP_FOLLOW * down
    left = _ramp(-0.110, 0.110, co.x)
    return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]


def back_flap_weights(co):
    """The back flap follows the legs far less than the front (0.28 of their swing at the hem, not
    0.62): sitting, the legs go out ahead and a flap that went with them would swing down through
    the ledge under him."""
    down = _ramp(0.186, 0.085, co.z) ** 0.8
    leg = 0.28 * down
    left = _ramp(-0.110, 0.110, co.x)
    return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]


def hang_weights(co):
    """A thing hung at the hip over the top of a leg: its foot takes some of that leg."""
    leg = 0.45 * _ramp(0.190, 0.140, co.z)
    return [("torso", 1.0 - leg), ("leg-left" if co.x > 0 else "leg-right", leg)]


def tube(part, name, bone, path, radius, paint, sides=6):
    """A round cord along hand-set points (the cast's `_rafi_tube` way): a ring at each point,
    square to the path there."""
    pts = [Vector(p) for p in path]
    rings = []
    for i, p in enumerate(pts):
        a = pts[max(i - 1, 0)]
        b = pts[min(i + 1, len(pts) - 1)]
        tangent = (b - a).normalized()
        u = tangent.cross(Z)
        u = (u if u.length > 1e-6 else X).normalized()
        v = tangent.cross(u).normalized()
        rings.append([p + u * (math.cos(t) * radius) + v * (math.sin(t) * radius)
                      for t in (2.0 * math.pi * (j + 0.5) / sides for j in range(sides))])
    return part.loft(name, bone, rings, paint)


def build_torso(part):
    skin = proj_square("torso", "skin", "skin_shade", 0.90, ends=(2, "skin"))
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, z), w, d) for z, w, d in TORSO_ROWS], skin, 0.014)
    for s in (1, -1):
        y = chest_y(0.284)
        part.block("pectoral", "torso", X, Z, [((s * 0.056, y + 0.004, 0.284), 0.048, 0.024), ((s * 0.056, y - 0.007, 0.284), 0.045, 0.022)],
                   proj_square("torso", "skin_shade", "skin_shade", 0.90), 0.006)
    neck = part.block("neck", "torso", X, Y, [((0, TORSO_CY, 0.326), 0.046, 0.044), ((0, TORSO_CY, 0.356), 0.043, 0.041)],
                      flat("skin_shade"), 0.008)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.332, 0.350, co.z)), ("head", 0.7 * _ramp(0.332, 0.350, co.z))])
    # the bahag between his legs
    part.block("pelvis", "torso", X, Y, [((0, LEG_Y, 0.116), 0.058, 0.062), ((0, LEG_Y, 0.180), 0.062, 0.066)], INDIGO_T, 0.008)

    # THE WRAPPED WAIST: indigo, its thin bands drawn from the torso's islands at its height
    lo, hi = tex.WAIST
    (wl, dl), (wh, dh) = torso_half(lo), torso_half(hi)
    part.block("waist", "torso", X, Y, [((0, TORSO_CY, lo), wl + WAIST_PROUD, dl + WAIST_PROUD),
                                        ((0, TORSO_CY, hi), wh + WAIST_PROUD, dh + WAIST_PROUD)],
               proj_square("torso", "indigo", "indigo_dark", 0.90, ends=(2, "indigo_lit")), 0.005)
    # THE WRAPPED BELT: two rattan cords round the waist, each a round-cornered block of its own
    for z0, z1, proud in ((0.2005, 0.2095, CORD_PROUD), (0.2100, 0.2185, CORD_PROUD - 0.002)):
        w, d = torso_half(0.5 * (z0 + z1))
        part.block("belt-cord", "torso", X, Y, [((0, TORSO_CY, z0), w + proud, d + proud), ((0, TORSO_CY, z1), w + proud, d + proud)],
                   RATTAN_T, 0.0042)
    # THE KNOT at the front, his one focal fastening, and its two short ends over the flap
    fy = chest_y(0.21) - CORD_PROUD - 0.008
    part.block("belt-knot", "torso", X, Y, [((0, fy, 0.196), 0.016, 0.011), ((0.002, fy - 0.001, 0.223), 0.020, 0.013)], RATTAN_T, 0.006)
    ends = []
    ends += part.wedge("belt-end", "torso", (-0.008, fy - 0.004, 0.198), (-0.019, fy - 0.010, 0.160), (0.0065, 0.0055), (0.0050, 0.0045), RATTAN_T, 0.002)
    ends += part.wedge("belt-end", "torso", (0.009, fy - 0.004, 0.198), (0.016, fy - 0.009, 0.168), (0.0065, 0.0055), (0.0050, 0.0045), RATTAN_T, 0.002)
    part.bend(ends, flap_weights)

    # THE FLAPS: the front flares to its hem and hangs to the knee; the back is wider and shorter.
    # Each takes its own drawn swatch on its one outward face and a cream hem as geometry.
    soft = []
    front_faces = []
    front = chest_y(0.176) - WAIST_PROUD - 0.007
    soft += part.block("flap-front", "torso", X, Y, [((0, front, 0.200), 0.052, 0.006), ((0, front - 0.003, 0.125), 0.057, 0.006),
                                                    ((0, front - 0.009, 0.060), 0.063, 0.006)],
                       panel("flap_front", 0, 2, (-0.063, 0.063, 0.056, 0.200), AHEAD, "indigo_dark"), 0.003)
    soft += part.block("flap-front-hem", "torso", Y, Z, [((-0.066, front - 0.0095, 0.0600), 0.0085, 0.0060),
                                                        ((0.066, front - 0.0095, 0.0600), 0.0085, 0.0060)], WHITE_T, 0.0025)
    front_faces = list(soft)
    back = back_y(0.176) + WAIST_PROUD + 0.007
    soft += part.block("flap-back", "torso", X, Y, [((0, back, 0.196), 0.054, 0.006), ((0, back + 0.003, 0.140), 0.057, 0.006),
                                                   ((0, back + 0.007, 0.098), 0.060, 0.006)],
                       panel("flap_back", 0, 2, (0.060, -0.060, 0.096, 0.196), BEHIND, "indigo_dark"), 0.003)
    soft += part.block("flap-back-hem", "torso", Y, Z, [((-0.063, back + 0.0075, 0.0990), 0.0085, 0.0058),
                                                       ((0.063, back + 0.0075, 0.0990), 0.0085, 0.0058)], WHITE_T, 0.0025)
    part.bend(soft[:len(front_faces)], flap_weights)
    part.bend(soft[len(front_faces):], back_flap_weights)

    # THE PINUHHA: a small woven bag at his right hip, its drawn face to the front, a lid as a
    # block; its cord runs up across his chest, over his LEFT shoulder and down his back to the
    # far side of the bag. The bag's foot gives to his right leg.
    bag = []
    bag += part.block("bag", "torso", X, Z, [((-0.099, -0.098, 0.170), 0.029, 0.031), ((-0.099, -0.122, 0.170), 0.027, 0.029)],
                      panel("bag", 0, 2, (-0.128, -0.070, 0.139, 0.201), AHEAD, "rattan_dark"), 0.006)
    bag += part.block("bag-lid", "torso", X, Z, [((-0.099, -0.096, 0.196), 0.031, 0.010), ((-0.099, -0.125, 0.194), 0.030, 0.009)],
                      tones("rattan_dark", "rattan_dark", "rattan_deep"), 0.004)
    part.bend(bag, hang_weights)
    path = [(-0.079, -0.112, 0.204), (-0.074, -0.098, 0.222), (-0.044, -0.090, 0.242), (-0.010, -0.095, 0.263), (0.022, -0.100, 0.283),
            (0.052, -0.098, 0.305), (0.072, -0.090, 0.328), (0.082, -0.062, 0.345), (0.086, -0.012, 0.352), (0.086, 0.040, 0.350),
            (0.078, 0.082, 0.337), (0.044, 0.089, 0.307), (-0.010, 0.088, 0.270), (-0.068, 0.089, 0.238), (-0.110, 0.068, 0.217),
            (-0.125, 0.012, 0.212), (-0.120, -0.056, 0.208), (-0.117, -0.110, 0.203)]
    tube(part, "bag-cord", "torso", path, 0.0055, CORD_T)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests, at the heroes' length, lean. `s` is +1 for his left.
# A deltoid cap, the upper arm, then on the elbow bone the forearm and the fist (x 0.252 to
# 0.310). The brass armlet is on his LEFT upper arm. Two rigid blocks overlap at the elbow.
# ---------------------------------------------------------------------------

def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    paint = proj_square(group, "skin", "skin_shade", 0.90, ends=(0, "skin_shade"))
    arm_block(part, "deltoid", bone, s, [(0.096, (0.050, 0.054)), (0.122, (0.053, 0.057)), (0.142, (0.049, 0.052))], paint, 0.014)
    arm_block(part, "upper-arm", bone, s, [(0.104, (0.046, 0.050)), (ELBOW_X + 0.010, (0.044, 0.048))], paint, 0.011)
    if s > 0:
        arm_block(part, "armlet", bone, s, [(0.134, (0.058, 0.061)), (0.150, (0.058, 0.061))], BRASS_T, 0.005)
        arm_block(part, "armlet-rail", bone, s, [(0.1525, (0.0545, 0.0575)), (0.1575, (0.0545, 0.0575))], tones("brass", "brass_dark", "brass_dark"), 0.002)
    # the forearm starts inside the upper arm, so no gap opens when the elbow folds; flat tones
    arm_block(part, "forearm", fore, s, [(ELBOW_X - 0.016, (0.042, 0.045)), (tex.WRIST + 0.006, (0.040, 0.043))], SKIN_T, 0.010)
    arm_block(part, "hand", fore, s, [(tex.WRIST, (0.052, 0.058)), (tex.HAND_END, (0.052, 0.058))],
              proj_square(group, "skin", "skin_shade", 0.93), 0.013)


# ---------------------------------------------------------------------------
# THE LEGS. The heroes' short legs, bare and lean: a thigh over a narrower shin, and a BARE FOOT
# standing on zero, low at the front where the toes step down.
# ---------------------------------------------------------------------------

def build_leg(part, s):
    bone = "leg-left" if s > 0 else "leg-right"
    part.block("thigh", bone, X, Y, [((s * LEG_X, LEG_Y, 0.112), 0.054, 0.060), ((s * LEG_X, LEG_Y, 0.188), 0.058, 0.064)], SKIN_T, 0.012)
    part.block("shin", bone, X, Y, [((s * LEG_X, LEG_Y, 0.030), 0.042, 0.046), ((s * LEG_X, LEG_Y, 0.120), 0.047, 0.051)], SKIN_T, 0.010)

    def station(y, half, z0, z1, cut):
        return rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)

    foot = [(-0.088, 0.046, 0.0, 0.030, 0.008), (-0.060, 0.050, 0.0, 0.042, 0.010), (0.000, 0.050, 0.0, 0.052, 0.012),
            (0.046, 0.046, 0.0, 0.050, 0.012), (0.068, 0.038, 0.0, 0.040, 0.006)]
    part.loft("foot", bone, [station(*row) for row in foot], SKIN_T)
    part.block("toes", bone, X, Z, [((s * LEG_X, -0.118, 0.0105), 0.042, 0.0105), ((s * LEG_X, -0.084, 0.0115), 0.047, 0.0115)],
               SKIN_T, 0.005)


# ---------------------------------------------------------------------------
# THE BUILD
# ---------------------------------------------------------------------------

def mesh_arrays(obj):
    """A mesh as glTF arrays: +y up, -z the way Blender's -y faces, v flipped, four bones a vertex."""
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


def write_glb(body, head, out):
    """character-male-b.glb with its meshes swapped: skeleton, bind matrices and clips copied untouched,
    two elbows appended, the clips of his own replacing those of the same name, and his new clips
    (`cheer`, `clap`) appended after them."""
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

    # the two elbows: a node under each arm, a joint and a bind matrix appended after the seven.
    # Every rest rotation in this rig is identity, so a bind matrix is the inverse translation.
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

    # his NEW clips, which the stock rig does not have, appended as empty animations first
    have = {anim["name"] for anim in gltf["animations"]}
    for name in clips.CLIPS:
        if name not in have:
            gltf["animations"].append({"name": name, "samplers": [], "channels": []})
    # his own clips, in place of the ones copied across (the clips script)
    node_of = {n.get("name"): i for i, n in enumerate(gltf["nodes"])}
    for anim in gltf["animations"]:
        if anim["name"] not in clips.CLIPS:
            continue
        anim["samplers"], anim["channels"] = [], []
        for (bone, path), keys in clips.CLIPS[anim["name"]]().items():
            times = add([(t,) for t, _ in keys], "f", "SCALAR", 5126, minmax=True)
            values = add([v for _, v in keys], "f", "VEC4" if path == "rotation" else "VEC3", 5126)
            anim["channels"].append({"sampler": len(anim["samplers"]), "target": {"node": node_of[bone], "path": path}})
            anim["samplers"].append({"input": times, "output": values, "interpolation": "LINEAR"})

    by_name = {m.get("name"): m for m in gltf["meshes"]}   # the base file's own two mesh names
    for name, built in (("body-mesh", body), ("head-mesh", head)):
        pos, nrm, uv, joints, weights, idx = built
        by_name[name]["primitives"] = [{
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

    gltf["accessors"] = accessors
    gltf["bufferViews"] = views
    gltf["buffers"] = [{"byteLength": len(blob)}]
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso Payo audience v2 (young_man)"}
    gltf["extras"] = {"prototype": "payo-audience-v2-20261019", "replaces": "ArtSource/payo/audience/audience_young_man.glb (v1, rejected)"}
    gltf["images"] = [{"uri": tex.ATLAS_NAME, "name": NAME + "-atlas"}]
    gltf["textures"] = [{"sampler": 0, "source": 0, "name": NAME + "-atlas"}]
    # Bilinear with mips, as the beggar's painted atlas is: the stock colormap's nearest filter
    # is for flat palette cells and would stair-step a drawn line.
    gltf["samplers"] = [{"magFilter": 9729, "minFilter": 9987}]
    gltf["materials"][0]["name"] = NAME
    gltf["materials"][0]["doubleSided"] = False
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
    print("triangles %d  (budget %d)" % (tris, TRIANGLE_BUDGET))
    head_obj = objects[1]
    hair_top = max(v.co.z for v in head_obj.data.vertices if abs(v.co.x) < 0.12)
    print("height: feather tip %.4f, top of the hair %.4f (expected %.4f)" % (hi.z, hair_top, HAIR_HEIGHT))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if abs(hi.z - HEIGHT) > 0.004:
        raise SystemExit("the top is at %.4f, not the %.4f of his feather's tip with the head at %.2f" % (hi.z, HEIGHT, HEAD_SCALE))
    if tris > TRIANGLE_BUDGET:
        raise SystemExit("%d triangles is over the budget of %d" % (tris, TRIANGLE_BUDGET))
    # THE HAND (see the docstring). The arm must run along x; the fist must be the far end of the
    # forearm bone; its top must sit where the original's does; and the far 14 per cent of the
    # arm, which is what ViewmodelArmAuthor measures a first-person fist by, must be the fist and
    # nothing wider.
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
            raise SystemExit("the hand's top moved; a carried tsinelas would float or sink")
        if abs(far - tex.HAND_END) > 0.003:
            raise SystemExit("the hand's far end moved off %.3f" % tex.HAND_END)
        if abs(fist - FIST_ACROSS) > 0.004:
            raise SystemExit("the far %d%% of the arm is %.4f across, not the fist's %.3f: something else is in it" % (round(100 * FIST_SHARE), fist, FIST_ACROSS))
        if fist <= 0.5 * widest:
            raise SystemExit("the fist is under half the arm's widest section; the game would size the arm by length instead")


def build(armature, material):
    body, head = Part("body-mesh"), Part("head-mesh")
    build_torso(body)
    build_arm(body, 1)
    build_arm(body, -1)
    build_leg(body, 1)
    build_leg(body, -1)
    build_head(head)
    build_hair(head)
    objects = []
    for part in (body, head):
        part.resolve_uvs()
        # AFTER the paint is placed (it is projected from the full-size shape): the head comes in.
        # A vertex wholly on the head bone is scaled by HEAD_SCALE about the head joint; a vertex
        # that shares the head with the torso (the top of his neck, the locks of his beard) by the same
        # share, so nothing tears where the two meet.
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
    """Windows sometimes refuses a write (OSError 22) while the editor holds the file."""
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
    # The rig, for the .blend and for posing: the original's, its meshes dropped.
    bpy.ops.import_scene.gltf(filepath=BASE, bone_heuristic="BLENDER")
    armature = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    for obj in [o for o in bpy.data.objects if o.type == "MESH"]:
        bpy.data.objects.remove(obj)
    armature.name = NAME
    # the elbows, for the .blend (the .glb gets its own in `write_glb`)
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
        raise SystemExit("paint the atlas first: py -3 tools/author_payo_audience_v2_young_man_textures.py")
    image = bpy.data.images.load(image_path)
    material = bpy.data.materials.new(NAME)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    node = nodes.new("ShaderNodeTexImage")
    node.image = image
    material.node_tree.links.new(node.outputs[0], nodes["Principled BSDF"].inputs["Base Color"])
    nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0

    objects = build(armature, material)
    body, head = mesh_arrays(objects[0]), mesh_arrays(objects[1])
    _retry(lambda: write_glb(body, head, out or OUT))
    print("wrote " + (out or OUT))
    if out is not None:
        return
    os.makedirs(os.path.dirname(BLEND), exist_ok=True)
    image.filepath = bpy.path.relpath(image_path, start=os.path.dirname(BLEND))
    bpy.context.preferences.filepaths.save_version = 0   # no .blend1 beside it
    _retry(lambda: bpy.ops.wm.save_as_mainfile(filepath=BLEND))
    print("wrote " + BLEND)


if __name__ == "__main__":
    main()

