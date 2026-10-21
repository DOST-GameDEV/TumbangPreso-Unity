"""Build the Payo audience v2 villager `boy`: a precolonial Ifugao village boy of about eight.

    py -3 tools/author_payo_audience_v2_boy_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_payo_audience_v2_boy.py
    ... -- --out other.glb     (writes there instead and leaves the .blend alone; needs the atlas beside it)

Writes
    ArtSource/payo/audience/v2/boy/audience_v2_boy.glb      (rigged, every clip)
    ArtSource/payo/audience/v2/boy/audience_v2_boy.blend
beside the atlas painted by the textures script. NOTHING IN THE GAME LOADS THEM; the Payo map
session will import them.

WHY. docs/reports/character-redesign/payo-audience-brief.md. The v1 audience (six box figures) was
REJECTED for its construction: "you need to actually model them in the way we've modeled the rest
of the characters". So each villager is built EXACTLY like a Classic redesign. This one is copied
from the Totoy redesign (tools/author_character_redesign_totoy*.py), the nearest approved Classic:
a small boy on the cast's rig. Every rule of classic-brief.md stands (head 0.84, ink eye blocks,
one mouth stroke, no blush on a boy, no nose, chunky layered black hair in three tones, the paint
rules, `forearm-left`/`forearm-right` and the fist block, cloth across two bones bends, feet on
zero, under 6,000 triangles); the audience brief wins where it differs (barefoot, quiet colour).

WHO HE IS. A village boy of the rice terraces, about eight, at the low end of the cast's height
range (the brief: about 0.67). Energetic: he waves at the players, points at the court, jumps
when they score. Precolonial Ifugao dress for a boy: a small BAHAG and nothing else on his body.

WHAT IS DESIGNED, AND WHY:
  * THE HAIR, a chunky Ifugao BOWL CUT. The bowl cut is the traditional Ifugao men's and boys'
    cut, so it reads as Cordillera at a glance. Built the heroes' way: a domed mass with a rim of
    chunky locks cut LEVEL all round above the ears (the bowl), and a CHOPPY FRINGE of seven locks
    of uneven length and width across his forehead: hacked with a blade, not a barber's scallops.
    No cowlick, no clipped nape, no glasses: that is Totoy's, and he must not be Totoy. Dante's
    black, three tones by facing.
  * THE FACE: the heroes' ink blocks with HIS cut, the top edge ARCHED (eyes wide, brows up:
    delight at the match), and one big grin, a touch higher at his left corner.
  * THE BAHAG in UNDYED COTTON: a belt wrapped TWICE round his hips (two tiers, a seam between),
    a front flap hanging from it to his knees and a longer back flap, each flaring a little toward
    its end (the clothing style's flared lower layer), each with ONE madder-red band near its end
    built as geometry, and the loin wrap between his legs. The KNOT where the belt is tied, on the
    front at his left hip, is the one focal fastening. Cloth across two bones bends: the front
    flap takes both legs, the back flap a little.
  * ONE BEAD STRING at the neck: chunky shell beads, a brass bead at the front, two red seeds.
    Geometry, every bead set by hand.
  * HIS PROP: a carved wooden spinning TOP tucked in his belt at his right hip, the cord wound
    round it. Ifugao children spin tops; it is the one thing he would carry.
  * BARE CHEST, no tattoos (a child has none); bare arms; BAREFOOT, the foot a proper block with
    the toes as a step.
  * HIS RIG HAS KNEES: `shin-left` and `shin-right` appended after the elbows (see the clips
    script), so he can sit on a ledge with his shins hanging and swinging.
Nobody in the test row wears a bahag but Rafi: Rafi's is dark with teal and silver over a
tattooed grown body with long tied hair; the boy's is pale cotton with one red band on a small
bare body under a black bowl.

HOW A FACE FINDS ITS PAINT. As Totoy's: a piece is tagged with a group and each of its faces
that squarely faces one of the group's views goes to that view's island; loose pieces take a
flat tone instead.
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
import author_payo_audience_v2_boy_textures as tex  # noqa: E402  the island layout
import author_payo_audience_v2_boy_clips as clips  # noqa: E402  his clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/character-male-a.glb")
FOLDER = os.path.join(ROOT, "ArtSource/payo/audience/v2/boy")
OUT = os.path.join(FOLDER, "audience_v2_boy.glb")
BLEND = os.path.join(FOLDER, "audience_v2_boy.blend")
NAME = "audience_v2_boy"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# FOUR BONES THE CAST DOES NOT HAVE, appended AFTER the seven so their indices and names do not
# move: an elbow in each arm (as the heroes' and every Classic's) and a KNEE in each leg (his
# own, for the ledge sit). (name, parent, world position in Blender space). A clip that does not
# key them leaves the limb straight; the writer keys them straight in every clip that does not.
ELBOW_X = 0.180
ARM_Y, ARM_Z = 0.006, 0.288    # the line his arms are built along (the arm bone is at z 0.288)
LEG_X = 0.0836
KNEE_Z = 0.080
EXTRA_BONES = [("forearm-left", "arm-left", (ELBOW_X, ARM_Y, ARM_Z)),
               ("forearm-right", "arm-right", (-ELBOW_X, ARM_Y, ARM_Z)),
               ("shin-left", "leg-left", (LEG_X, 0.0, KNEE_Z)),
               ("shin-right", "leg-right", (-LEG_X, 0.0, KNEE_Z))]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
HEAD_SCALE = 0.84
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))   # the `head` bone of character-male-a.glb, in Blender space
SHARP_ANGLE = 40.0
TRIANGLE_BUDGET = 6000
HEIGHT_RANGE = (0.6613, 0.7928)              # the cast's; a child sits at the low end (brief: about 0.67)
ARM_END = tex.HAND_END

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
# THE HEAD. The heroes' head (the rows every redesign uses), so his face is the same canvas.
#     rows are (z, half width, depth to the front, depth to the back, exponent)
# ---------------------------------------------------------------------------
HEAD_N = 24
HEAD_ROWS = [
    (0.343, 0.126, 0.132, 0.124, 3.4),
    (0.349, 0.154, 0.156, 0.148, 3.8),
    (0.372, 0.164, 0.162, 0.154, 4.0),
    (0.400, 0.173, 0.165, 0.159, 4.2),
    (0.432, 0.181, 0.165, 0.162, 4.2),
    (0.470, 0.176, 0.165, 0.162, 4.2),
    (0.520, 0.171, 0.165, 0.162, 4.2),
    (0.604, 0.169, 0.165, 0.162, 4.2),
    (0.646, 0.162, 0.158, 0.158, 3.8),
    (0.661, 0.140, 0.136, 0.140, 3.4),
]
FACE_Y = 0.001 - 0.165     # the flat face plane


def super_ring(z, rx, rf, rb, e, n=HEAD_N, cy=0.001, grow=None):
    out = []
    for j in range(n):
        t = 2.0 * math.pi * j / n
        c, s = math.cos(t), math.sin(t)
        a = math.copysign(abs(c) ** (2.0 / e), c)
        b = math.copysign(abs(s) ** (2.0 / e), s)
        k = grow(j) if grow else 1.0
        out.append(Vector((a * rx * k, cy + b * (rb if b >= 0 else rf) * k, z)))
    return out


AHEAD = (0, -1, 0)
SKIN_T = tones("skin_lit", "skin", "skin_shade")
COTTON_T = tones("cotton_lit", "cotton", "cotton_shade")
COTTON_DEEP_T = tones("cotton", "cotton_shade", "cotton_deep")
RED_T = tones("red_lit", "red", "red_dark")
WOOD_T = tones("wood_lit", "wood", "wood_dark")
BRASS_T = tones("brass_lit", "brass", "brass_dark")
BEAD_T = tones("bead", "bead", "bead_shade")
SEED_T = tones("seed", "seed", "seed")
RATTAN_T = tones("rattan_lit", "rattan", "rattan_dark")
EAR_Z = 0.452


def build_head(part):
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], proj_square("head", "skin", "skin", 0.84))
    # NO NOSE. The heroes' ear block, at its own size (the bowl stops above it)
    for s in (1, -1):
        part.block("ear", "head", Y, Z, [((s * 0.166, 0.014, EAR_Z), 0.030, 0.040), ((s * 0.214, 0.020, EAR_Z), 0.022, 0.030)],
                   SKIN_T, 0.010)


# ---------------------------------------------------------------------------
# THE HAIR: A CHUNKY IFUGAO BOWL CUT WITH A CHOPPY FRINGE. Dante's black, three tones by facing,
# nothing drawn. The dome, its rim of chunky locks cut LEVEL all round above his ears, a back
# mass, the crown's layers combed out from the whorl, and in front SEVEN fringe locks hacked to
# uneven lengths and widths, two of them leaning. Every lock is set by hand, none repeats.
#   (name, base, tip, base half size, tip half size, chamfer[, across])
# ---------------------------------------------------------------------------
HAIR_CHAMFER = 0.020
HAIR_T = tones("hair_top", "hair", "hair_under")
DEEP_T = tones("hair", "hair_under", "hair_under")
RIM = 0.585        # the dome's foot
WEDGES = [
    # THE CHOPPY FRINGE, his right (-x) to his left, in front of the flat face (y -0.164)
    ("fringe-1", (-0.164, -0.176, 0.664), (-0.170, -0.184, 0.562), (0.026, 0.021), (0.016, 0.012), 0.006),
    ("fringe-2", (-0.112, -0.180, 0.668), (-0.106, -0.188, 0.578), (0.030, 0.022), (0.018, 0.013), 0.006),
    ("fringe-3", (-0.058, -0.182, 0.670), (-0.066, -0.190, 0.552), (0.028, 0.023), (0.013, 0.013), 0.006),
    ("fringe-4", (-0.002, -0.183, 0.672), (0.004, -0.191, 0.582), (0.033, 0.023), (0.022, 0.013), 0.006),
    ("fringe-5", (0.054, -0.182, 0.670), (0.062, -0.190, 0.558), (0.027, 0.023), (0.012, 0.013), 0.006),
    ("fringe-6", (0.108, -0.180, 0.668), (0.106, -0.188, 0.576), (0.031, 0.022), (0.020, 0.013), 0.006),
    ("fringe-7", (0.160, -0.176, 0.664), (0.168, -0.184, 0.566), (0.025, 0.021), (0.015, 0.012), 0.006),
    # the front corners at the temples
    ("corner-r", (-0.188, -0.130, 0.650), (-0.192, -0.132, 0.534), (0.020, 0.036), (0.014, 0.024), 0.008),
    ("corner-l", (0.188, -0.130, 0.650), (0.192, -0.130, 0.540), (0.020, 0.036), (0.014, 0.024), 0.008),
    # THE BOWL'S RIM over the ears: level, stopping above the ear (its top is at 0.492)
    ("side-r-1", (-0.200, -0.064, 0.650), (-0.202, -0.062, 0.524), (0.022, 0.044), (0.017, 0.036), 0.010),
    ("side-r-2", (-0.201, 0.022, 0.650), (-0.203, 0.024, 0.518), (0.023, 0.046), (0.017, 0.038), 0.010),
    ("side-r-3", (-0.198, 0.110, 0.650), (-0.200, 0.114, 0.522), (0.022, 0.044), (0.016, 0.036), 0.010),
    ("side-l-1", (0.200, -0.064, 0.650), (0.202, -0.060, 0.520), (0.022, 0.044), (0.017, 0.036), 0.010),
    ("side-l-2", (0.201, 0.022, 0.650), (0.203, 0.026, 0.526), (0.023, 0.046), (0.017, 0.038), 0.010),
    ("side-l-3", (0.198, 0.110, 0.650), (0.200, 0.112, 0.518), (0.022, 0.044), (0.016, 0.036), 0.010),
    # down the back, the bowl's rim lower here, four locks of their own widths
    ("back-1", (-0.142, 0.210, 0.656), (-0.146, 0.215, 0.506), (0.046, 0.016), (0.040, 0.014), 0.009),
    ("back-2", (-0.048, 0.214, 0.660), (-0.050, 0.219, 0.494), (0.052, 0.017), (0.044, 0.014), 0.009),
    ("back-3", (0.048, 0.214, 0.660), (0.052, 0.219, 0.500), (0.048, 0.017), (0.040, 0.014), 0.009),
    ("back-4", (0.142, 0.210, 0.656), (0.146, 0.215, 0.510), (0.046, 0.016), (0.040, 0.014), 0.009),
    # THE CROWN'S LAYERS, combed out from a whorl at the back of the crown
    ("top-fl", (0.040, 0.010, 0.724), (0.092, -0.150, 0.684), (0.052, 0.018), (0.060, 0.014), 0.009),
    ("top-fr", (-0.046, 0.004, 0.722), (-0.088, -0.146, 0.678), (0.048, 0.018), (0.062, 0.014), 0.009),
    ("top-l", (0.064, 0.060, 0.722), (0.176, 0.044, 0.676), (0.058, 0.018), (0.068, 0.014), 0.009, Y),
    ("top-r", (-0.060, 0.054, 0.720), (-0.174, 0.038, 0.672), (0.056, 0.018), (0.066, 0.014), 0.009, Y),
    ("top-b", (0.000, 0.094, 0.720), (0.004, 0.190, 0.676), (0.070, 0.018), (0.082, 0.014), 0.009),
]


def build_hair(part):
    part.block("hair-dome", "head", X, Y, [((0, 0.000, RIM), 0.196, (0.180, 0.200)), ((0, 0.002, 0.650), 0.198, (0.178, 0.200)),
                                           ((0, 0.008, 0.688), 0.174, (0.150, 0.176)), ((0, 0.014, 0.716), 0.112, (0.090, 0.112))],
               HAIR_T, HAIR_CHAMFER)
    part.block("hair-back", "head", X, Y, [((0, 0.178, 0.500), 0.194, 0.030), ((0, 0.176, 0.650), 0.200, 0.034)], DEEP_T, 0.014)
    for name, base, tip, base_half, tip_half, chamfer, *across in WEDGES:
        part.wedge("hair-" + name.split("-")[0], "head", base, tip, base_half, tip_half, HAIR_T, chamfer, *across)


# ---------------------------------------------------------------------------
# THE TORSO, BARE. Totoy's torso block (the smallest of the row) in skin, a neck, and the ONE
# BEAD STRING lying round the base of his neck, every bead set by hand.
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.343)
TORSO_LOW = (0.118, 0.080)
TORSO_HIGH = (0.110, 0.076)
TORSO_CY = -0.002
#   (x, y, z, half size, tones): from the brass bead at the front round to the back of his neck
BEADS = [
    (0.000, -0.086, 0.298, 0.0125, BRASS_T),
    (0.024, -0.085, 0.303, 0.0105, BEAD_T), (-0.023, -0.085, 0.304, 0.0100, BEAD_T),
    (0.045, -0.083, 0.313, 0.0090, SEED_T), (-0.044, -0.083, 0.314, 0.0095, RED_T),
    (0.062, -0.080, 0.326, 0.0105, BEAD_T), (-0.061, -0.080, 0.327, 0.0100, BEAD_T),
    (0.073, -0.066, 0.343, 0.0095, RED_T), (-0.072, -0.066, 0.343, 0.0090, SEED_T),
    (0.077, -0.042, 0.350, 0.0100, BEAD_T), (-0.077, -0.041, 0.350, 0.0105, BEAD_T),
    (0.071, -0.012, 0.352, 0.0095, BEAD_T), (-0.072, -0.012, 0.352, 0.0090, BEAD_T),
    (0.062, 0.020, 0.352, 0.0100, BEAD_T), (-0.061, 0.021, 0.352, 0.0100, BEAD_T),
    (0.042, 0.048, 0.351, 0.0095, BEAD_T), (-0.040, 0.048, 0.351, 0.0090, BEAD_T),
    (0.000, 0.060, 0.351, 0.0100, BEAD_T),
]


def build_torso(part):
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, TORSO_Z[0]), *TORSO_LOW), ((0, TORSO_CY, TORSO_Z[1]), *TORSO_HIGH)],
               proj_square("torso", "skin", "skin_shade", 0.90, ends=(2, "skin_lit")), 0.016)
    neck = part.block("neck", "torso", X, Y, [((0, TORSO_CY, 0.326), 0.048, 0.046), ((0, TORSO_CY, 0.354), 0.046, 0.044)],
                      flat("skin_shade"), 0.008)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.332, 0.350, co.z)), ("head", 0.7 * _ramp(0.332, 0.350, co.z))])
    for x, y, z, h, paint in BEADS:
        part.block("beads", "torso", X, Y, [((x, y, z - h), h, h), ((x, y, z + h), h, h)], paint, 0.45 * h)


# ---------------------------------------------------------------------------
# THE BAHAG, undyed cotton. The belt wrapped twice (two tiers, a seam between), the loin wrap
# between his legs, a FRONT FLAP to his knees and a longer BACK FLAP, each flaring toward its
# end with ONE red band near it, and the KNOT at his left hip: the focal fastening. His spinning
# top tucked in at his right hip.
# ---------------------------------------------------------------------------
BELT_A = (0.138, 0.166, 0.132, 0.094)      # lower tier: foot, top, half width, half depth
BELT_B = (0.164, 0.196, 0.127, 0.089)      # upper tier, a step in
FLAP_F = (0.162, 0.046)                    # the front flap's top and foot
FLAP_B = (0.162, 0.030)


def front_flap_weights(co):
    down = _smooth(_ramp(0.166, 0.110, co.z))
    leg = 0.55 * down
    return [("torso", 1.0 - leg), ("leg-left", 0.5 * leg), ("leg-right", 0.5 * leg)]


def back_flap_weights(co):
    down = _smooth(_ramp(0.166, 0.090, co.z))
    leg = 0.30 * down
    return [("torso", 1.0 - leg), ("leg-left", 0.5 * leg), ("leg-right", 0.5 * leg)]


def build_bahag(part):
    # the loin wrap between his legs
    part.block("loin", "torso", X, Y, [((0, 0.0, 0.106), 0.054, 0.062), ((0, 0.0, 0.178), 0.058, 0.066)], COTTON_DEEP_T, 0.008)
    # the belt, wrapped twice
    for name, (z0, z1, hx, hy), paint in (("belt-a", BELT_A, COTTON_DEEP_T), ("belt-b", BELT_B, COTTON_T)):
        part.block(name, "torso", X, Y, [((0, TORSO_CY, z0), hx, hy), ((0, TORSO_CY, z1), hx, hy)], paint, 0.012)

    def flap_piece(name, side, top, foot, half_top, half_foot, band_z, window, weights):
        """A flap hanging from under the upper tier, flaring from `half_top` to `half_foot`, 9 mm
        thick, leaning `side` (-1 front, +1 back) 5 mm out at its foot, and its ONE red band,
        16 mm tall, standing 2 mm proud of it all round."""
        y0 = TORSO_CY + side * (BELT_A[3] + 0.004)
        y1 = y0 + side * 0.005

        def at(z):
            f = (top - z) / (top - foot)
            return y0 + (y1 - y0) * f, half_top + (half_foot - half_top) * f

        flap = part.block(name, "torso", X, Y, [((0, y0, top), half_top, 0.0045), ((0, y1, foot), half_foot, 0.0045)],
                          panel("flapF" if side < 0 else "flapB", 0, 2, window, (0, side, 0), "cotton_shade"), 0.002)
        part.bend(flap, weights)
        (ya, ha), (yb, hb) = at(band_z), at(band_z + 0.016)
        band = part.block(name + "-band", "torso", X, Y, [((0, ya, band_z), ha + 0.002, 0.0065), ((0, yb, band_z + 0.016), hb + 0.002, 0.0065)],
                          RED_T, 0.002)
        part.bend(band, weights)

    flap_piece("flap-front", -1, FLAP_F[0], FLAP_F[1], 0.052, 0.064, 0.060, (-0.070, 0.070, 0.045, 0.170), front_flap_weights)
    flap_piece("flap-back", 1, FLAP_B[0], FLAP_B[1], 0.056, 0.068, 0.044, (0.070, -0.070, 0.030, 0.170), back_flap_weights)

    # THE KNOT at his left hip, on the front of the belt: a rounded lump of cloth and two short
    # end hanging from it, FLARING to its cut edge, and a hem across that edge in the cotton's deep
    # tone. v02 had two thin ends and they read as fingers.
    kx, ky = 0.072, TORSO_CY - BELT_A[3]
    part.block("knot", "torso", X, Y, [((kx, ky - 0.008, 0.150), 0.022, 0.013), ((kx, ky - 0.010, 0.170), 0.026, 0.016),
                                       ((kx, ky - 0.008, 0.192), 0.020, 0.013)], COTTON_T, 0.010)
    part.wedge("knot-tail", "torso", (kx + 0.002, ky - 0.016, 0.150), (kx + 0.010, ky - 0.020, 0.104), (0.014, 0.0045), (0.021, 0.004),
               COTTON_DEEP_T, 0.002)
    part.wedge("knot-tail", "torso", (kx + 0.0095, ky - 0.0205, 0.112), (kx + 0.0105, ky - 0.0210, 0.100), (0.0225, 0.0055), (0.0225, 0.0055),
               tones("cotton_shade", "cotton_deep", "cotton_deep"), 0.0015)

    # HIS SPINNING TOP, tucked in at his right hip, its stem under the upper tier: a carved
    # wooden top, a dark band round its crown, the cord wound round its waist
    cx, cy = -0.112, TORSO_CY - BELT_A[3] - 0.026

    def octa(z, r):
        return rect_ring((cx, cy, z), X, Y, r, r, 0.586 * r)

    rings = [octa(0.204, 0.007), octa(0.186, 0.007), octa(0.183, 0.020), octa(0.176, 0.027), octa(0.160, 0.028),
             octa(0.144, 0.020), octa(0.128, 0.010)]

    def top_paint(i, j):
        return (WOOD_T, WOOD_T, flat("wood_dark"), WOOD_T, WOOD_T, WOOD_T)[min(i, 5)]

    part.loft("spin-top", "torso", rings, top_paint, caps=(True, False), tip=Vector((cx, cy, 0.114)))
    part.block("spin-top-cord", "torso", X, Y, [((cx, cy, 0.162), 0.0295, 0.0295), ((cx, cy, 0.172), 0.0295, 0.0295)],
               COTTON_T, 0.010)


# ---------------------------------------------------------------------------
# THE ARMS, bare. Straight out as the rig rests. The upper arm on the arm bone, the forearm and
# the fist on the elbow bone; THE FIST is one clear block at the far end, the widest thing past
# the wrist (the first-person arms are cut from it).
# ---------------------------------------------------------------------------
FIST = (tex.WRIST, tex.HAND_END)


def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    arm_block(part, "upper-arm", bone, s, [(0.098, (0.045, 0.048)), (ELBOW_X + 0.008, (0.041, 0.044))],
              proj_square(group, "skin", "skin_shade", 0.93, ends=(0, "skin_shade")), 0.012)
    arm_block(part, "forearm", fore, s, [(ELBOW_X - 0.014, (0.040, 0.043)), (FIST[0] + 0.006, (0.037, 0.040))],
              proj_square(group, "skin", "skin_shade", 0.93, ends=(0, "skin_shade")), 0.010)
    if s > 0:
        # A WOVEN RATTAN ARMLET high on his left arm, the one other thing he wears: two plaited
        # rows, the upper a step narrower, natural rattan
        arm_block(part, "armlet", bone, s, [(0.124, (0.0490, 0.0520)), (0.138, (0.0490, 0.0520))], RATTAN_T, 0.004)
        arm_block(part, "armlet", bone, s, [(0.140, (0.0475, 0.0505)), (0.150, (0.0475, 0.0505))], tones("rattan", "rattan_dark", "rattan_dark"), 0.003)
    arm_block(part, "hand", fore, s, [(FIST[0], (0.049, 0.056)), (FIST[1], (0.049, 0.056))],
              proj_square(group, "skin", "skin_shade", 0.93), 0.013)


# ---------------------------------------------------------------------------
# THE LEGS, bare, and BAREFOOT. The thigh on the leg bone, the shin and the foot on the knee bone;
# the foot a proper block from heel to ball, the toes a lower step ahead of it. Turned out 4 deg.
# ---------------------------------------------------------------------------
TOE_OUT = 4.0


def build_leg(part, s):
    bone, shin = ("leg-left", "shin-left") if s > 0 else ("leg-right", "shin-right")
    part.block("thigh", bone, X, Y, [((s * LEG_X, 0.0, KNEE_Z - 0.008), 0.040, 0.044), ((s * LEG_X, 0.0, 0.192), 0.044, 0.048)],
               SKIN_T, 0.011)
    part.block("shin", shin, X, Y, [((s * LEG_X, 0.0, 0.026), 0.036, 0.040), ((s * LEG_X, 0.0, KNEE_Z + 0.012), 0.039, 0.043)],
               SKIN_T, 0.010)
    turn = math.radians(TOE_OUT) * s
    cs, sn = math.cos(turn), math.sin(turn)

    def turned(p):
        x, y = p.x - s * LEG_X, p.y - 0.02
        return Vector((s * LEG_X + x * cs - y * sn, 0.02 + x * sn + y * cs, p.z))

    def station(y, half, z0, z1, cut):
        return [turned(p) for p in rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)]

    foot = [(0.060, 0.036, 0.0, 0.040, 0.009), (0.034, 0.043, 0.0, 0.048, 0.011), (-0.030, 0.046, 0.0, 0.040, 0.011),
            (-0.078, 0.046, 0.0, 0.030, 0.009)]
    part.loft("foot", shin, [station(*row) for row in foot], SKIN_T)
    toes = [(-0.074, 0.045, 0.0, 0.021, 0.006), (-0.100, 0.044, 0.0, 0.020, 0.007), (-0.112, 0.038, 0.0, 0.014, 0.005)]
    part.loft("toes", shin, [station(*row) for row in toes], SKIN_T)


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
    """character-male-a.glb with its meshes swapped: skeleton, bind matrices and clips copied untouched,
    two elbows appended, and the four locomotion clips replaced by his own."""
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
    for name, parent, at in EXTRA_BONES:
        world = (at[0], at[2], -at[1])          # Blender space to the file's (+y up, +z ahead)
        pw = [-matrices[BONES.index(parent)][12 + a] for a in range(3)]
        gltf["nodes"].append({"name": name, "translation": [world[a] - pw[a] for a in range(3)]})
        gltf["nodes"][node_of[parent]].setdefault("children", []).append(len(gltf["nodes"]) - 1)
        new_joints.append(len(gltf["nodes"]) - 1)
        matrices.append((1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -world[0], -world[1], -world[2], 1))
    ibm = add([tuple(float(x) for x in m) for m in matrices], "f", "MAT4", 5126)
    for skin in gltf["skins"]:
        skin["joints"] = list(skin["joints"]) + new_joints
        skin["inverseBindMatrices"] = ibm

    # his own locomotion clips, in place of the ones copied across (the clips script)
    node_of = {n.get("name"): i for i, n in enumerate(gltf["nodes"])}
    present = {anim["name"] for anim in gltf["animations"]}
    for name in clips.CLIPS:
        if name not in present:           # `cheer` and `clap` are new names
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
    # THE KNEES STRAIGHT in every clip that does not bend them, so no clip inherits another's knee
    straight_t = add([(0.0,)], "f", "SCALAR", 5126, minmax=True)
    straight_q = add([(0.0, 0.0, 0.0, 1.0)], "f", "VEC4", 5126)
    for anim in gltf["animations"]:
        keyed = {(c["target"]["node"], c["target"]["path"]) for c in anim["channels"]}
        for knee in ("shin-left", "shin-right"):
            if (node_of[knee], "rotation") not in keyed:
                anim["channels"].append({"sampler": len(anim["samplers"]), "target": {"node": node_of[knee], "path": "rotation"}})
                anim["samplers"].append({"input": straight_t, "output": straight_q, "interpolation": "LINEAR"})

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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso Payo audience v2 (boy)"}
    gltf["extras"] = {"prototype": "payo-audience-v2-20261019", "replaces": "nothing; not loaded by the game yet",
                      "sit_seat": "the seat (underside of the thighs) is at the root origin in the sit clip"}
    gltf["images"] = [{"uri": tex.ATLAS_NAME, "name": NAME + "-atlas"}]
    gltf["textures"] = [{"sampler": 0, "source": 0, "name": NAME + "-atlas"}]
    # Bilinear with mips, as the beggar's painted atlas is: the stock colormap's nearest filter
    # is for flat palette cells and would stair-step a drawn line.
    gltf["samplers"] = [{"magFilter": 9729, "minFilter": 9987}]
    gltf["materials"][0]["name"] = NAME
    gltf["materials"][0]["doubleSided"] = False
    # the shared rig's material carries an empty KHR_texture_transform on its texture; the atlas needs none
    gltf["materials"][0]["pbrMetallicRoughness"]["baseColorTexture"] = {"index": 0}
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
    print("triangles %d  (budget %d, the original is 820)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    print("HEIGHT %.4f (the cast's range %.4f to %.4f; a child aims at about 0.67)" % (hi.z, HEIGHT_RANGE[0], HEIGHT_RANGE[1]))
    if not HEIGHT_RANGE[0] <= hi.z <= HEIGHT_RANGE[1]:
        raise SystemExit("height %.4f is outside the cast's range" % hi.z)
    if tris > TRIANGLE_BUDGET:
        raise SystemExit("%d triangles is over the budget of %d" % (tris, TRIANGLE_BUDGET))
    # THE HAND, FOR THE FIRST-PERSON ARMS (see the docstring; the test is ViewmodelArmAuthor's own).
    # The arm must run along x; the far 14 per cent of it must be the fist block on the forearm
    # bone and nothing past the wrist may be wider than the fist; its top must sit where the
    # cast's does (0.045 to 0.066 above the arm bone, where a carried tsinelas is parked).
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
        first = min(p.x * sign for p in arm)
        far = max(p.x * sign for p in arm)
        if abs(far - max(p.x * sign for p in fore)) > 1e-6:
            raise SystemExit("the far end of arm-%s is not on the forearm bone" % side)
        cut = first + 0.86 * (far - first)
        hand = [p for p in arm if p.x * sign >= cut]
        if any(p not in fore for p in hand):
            raise SystemExit("something on the upper arm reaches the far 14 per cent of arm-%s" % side)
        across = max(max(p.y for p in hand) - min(p.y for p in hand), max(p.z for p in hand) - min(p.z for p in hand))
        wrist = [p for p in fore if FIST[0] - 0.030 < p.x * sign < FIST[0] - 0.002]
        behind = max(max(p.y for p in wrist) - min(p.y for p in wrist), max(p.z for p in wrist) - min(p.z for p in wrist))
        widest = max(max(p.y for p in arm) - min(p.y for p in arm), max(p.z for p in arm) - min(p.z for p in arm))
        top = max(p.z for p in hand) - ARM_Z
        print("hand %-5s arm %.4f..%.4f, the far 14 per cent starts at %.4f, the fist is %.4f across (the wrist %.4f, the "
              "widest of the arm %.4f), its top %.4f above the arm bone" % (side, first, far, cut, across, behind, widest, top))
        if cut < FIST[0] + 0.012:
            raise SystemExit("the fist block does not fill the far 14 per cent of the arm")
        if across <= behind + 0.010 or across < 0.5 * widest:
            raise SystemExit("the fist does not read: it must be wider than the wrist and over half the arm's widest")
        if not 0.045 <= top <= 0.066:
            raise SystemExit("the hand's top moved; a carried tsinelas would float or sink")
        if abs(far - ARM_END) > 0.003:
            raise SystemExit("the hand's far end moved off %.3f" % ARM_END)


def build(armature, material):
    body, head = Part("body-mesh"), Part("head-mesh")
    build_torso(body)
    build_bahag(body)
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
        # that shares the head with the torso (the top of the neck) by the same share, so nothing
        # tears where the two meet.
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
    for name, parent, at in EXTRA_BONES:
        eb = armature.data.edit_bones.new(name)
        eb.head = at
        if name.startswith("shin"):
            eb.tail = (at[0], at[1], at[2] - 0.06)
        else:
            eb.tail = (at[0] + math.copysign(0.06, at[0]), at[1], at[2])
        eb.parent = armature.data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    if armature.animation_data:
        armature.animation_data.action = None

    folder = os.path.dirname(out) if out else FOLDER
    image_path = os.path.join(folder, tex.ATLAS_NAME)
    if not os.path.exists(image_path):
        raise SystemExit("paint the atlas first: py -3 tools/author_payo_audience_v2_boy_textures.py")
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
