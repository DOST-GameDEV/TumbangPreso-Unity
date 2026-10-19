"""Build the Payo audience v2 GIRL: a village girl of the Payo crowd, modelled the way the cast is.

    py -3 tools/author_payo_audience_v2_girl_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_payo_audience_v2_girl.py
    ... -- --out other.glb     (writes there instead and leaves the .blend alone; needs the atlas beside it)

Writes
    ArtSource/payo/audience/v2/girl/audience_v2_girl.glb    (rigged, every clip)
    ArtSource/payo/audience/v2/girl/audience_v2_girl.blend
beside the atlas painted by the textures script.

WHY. The Payo map wants a crowd of precolonial Ifugao villagers on its basin wall. v1 was box
figures and the owner rejected the construction ("you need to actually model them in the way
we've modeled the rest of the characters"). So each villager is built EXACTLY like a Classic
redesign (docs/reports/character-redesign/classic-brief.md), under the audience brief
(docs/reports/character-redesign/payo-audience-brief.md), which wins where they differ. The
block, loft, UV and glb code below is the approved Bebang redesign's, copied (one villager, one
set of files); every shape from the hair down is hers.

NOTHING IN THE GAME LOADS IT. It lives under ArtSource; the Payo map session imports it.

WHO SHE IS. A village girl of about nine, in the crowd watching a match: shy but excited. She
holds her own elbows, rocks on her heels and peeks past the grown-ups, and when something
happens she claps, quick and small, hands up under her chin. That clap is her signature.

WHAT SHE WEARS (precolonial Ifugao; the brief's short of it):
  * LONG BLACK HAIR IN TWO TIED BUNCHES, low behind each ear, each tied with a brass ring and a
    madder bead and hanging to her shoulders in three chunky locks; a soft fringe of six uneven
    locks; a long lock framing each cheek. Bebang's is a blonde bun high on the crown with a
    headband: nothing of the silhouette is shared.
  * A SHORT WRAPPED TOP in indigo-black cotton with tiny cap sleeves, the wrap's edge crossing
    her chest from her right shoulder to her left hip as a woven white band.
  * A BEAD COLLAR, wide and flat on her shoulders: rows of white and madder beads with a brass
    bead at the front. Bebang's five tilted tiles are a V on a cord; this is a collar.
  * A SMALL WHITE-GROUND TAPIS, wrapped, just above the knee, with THIN indigo bands (a pair high
    up, a broad one and a fine one at the hem with a madder line between) and its wrap edge down
    the front on her left; a woven indigo belt tied in front on her right with two short ends
    and brass bead tips: her ONE focal fastening.
  * BAREFOOT: proper foot blocks with the toes as a step, toe lines drawn on its top; a brass
    anklet on her left ankle, a bead bracelet on her left wrist.
QUIET COLOUR: indigo-black, undyed cotton, thin madder, brass. No hero's hue, no role hue.

A CHILD, ON THE CAST'S RIG (the brief's Kids section): the same seven bones and two elbows, not
scaled. Her HEAD is 0.90 of the donor's about the head joint (the cast is 0.84): a bigger head
share, which is what a child's proportion is. Her torso is narrower than Bebang's, her limbs
slimmer. Her hair tops out at about 0.67, the low end of the cast's 0.6613 to 0.7928.

THE RULES (classic-brief.md, section 15.3 of docs/CHARACTER_REDESIGN_DANTE.md): the heroes'
rounded head with one flat face plane; ink eye blocks, one thin mouth stroke, blush (female), no
nose; chunky layered hair with base, shade and light tones by facing; a drawing only on a face
square to its view; cloth across two bones bends (the tapis takes the legs, the bunches' tips and
the nape give to the torso); feet on zero; under 6,000 triangles; the fist block check.
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
import author_payo_audience_v2_girl_textures as tex  # noqa: E402  the island layout
import author_payo_audience_v2_girl_clips as clips  # noqa: E402  her clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/character-female-c.glb")
FOLDER = os.path.join(ROOT, "ArtSource/payo/audience/v2/girl")
OUT = os.path.join(FOLDER, "audience_v2_girl.glb")
BLEND = os.path.join(FOLDER, "audience_v2_girl.blend")
NAME = "audience_v2_girl"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
ELBOW_X = 0.180
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
# A CHILD'S HEAD SHARE: 0.90 of the donor's (the cast's 0.84). Built and painted full size, drawn
# in about the head joint after the UVs resolve (`build`).
HEAD_SCALE = 0.90
if "--head-scale" in sys.argv:
    HEAD_SCALE = float(sys.argv[sys.argv.index("--head-scale") + 1])
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))
SHARP_ANGLE = 40.0
TRIANGLE_BUDGET = 6000
ARM_END = tex.HAND_END

X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))


# ---------------------------------------------------------------------------
# THE BLOCK (the Bebang script's machinery, unchanged)
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
# THE HEAD. The heroes' head (the donor skull, Dante's `shaped` rows, one flat face plane).
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
FACE_Y = 0.001 - 0.165


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
BRASS_T = tones("brass_lit", "brass", "brass_dark")
BEAD_T = tones("bead", "bead", "bead_shade")
MADDER_T = tones("madder", "madder", "madder_deep")
COTTON_T = tones("cotton_lit", "cotton", "cotton_shade")
INDIGO_T = tones("indigo_lit", "indigo", "indigo_deep")


def slab(part, name, bone, centre, u, v, hu, hv, back, front, paint, cut=0.004, corner=0.0, normal=AHEAD):
    n = Vector(normal)
    c = Vector(centre)
    rings = [rect_ring(c - n * back, u, v, hu, hv, corner),
             rect_ring(c + n * (front - cut), u, v, hu, hv, corner),
             rect_ring(c + n * front, u, v, hu - cut, hv - cut, max(corner - 0.4 * cut, 0.0))]
    return part.loft(name, bone, rings, paint, caps=(False, True))


def axis_block(part, name, bone, a, b, ha, hb, paint, chamfer):
    """A block from point `a` to point `b` (half sizes `ha` at a and `hb` at b), any direction."""
    w = (Vector(b) - Vector(a)).normalized()
    ref = X if abs(w.dot(X)) < 0.8 else Y
    u = (ref - w * ref.dot(w)).normalized()
    v = w.cross(u).normalized()
    return part.block(name, bone, u, v, [(a, ha[0], ha[1]), (b, hb[0], hb[1])], paint, chamfer)


def build_head(part):
    # only the flat front takes the drawing (her face); her ears are under her hair
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], proj_square("head", "skin", "skin", 0.84))


# ---------------------------------------------------------------------------
# THE HAIR. Long and BLACK (the heroes' black, three tones by facing), parted in the middle:
#   * the crown mass, and on it four chunky locks swept from the part down each side;
#   * a FRINGE of six uneven locks, longest at the two sides, the middle pair parted;
#   * a long lock framing each cheek, down past the jaw;
#   * the hair over each ear drawn back and down into a BUNCH low behind the ear;
#   * the back in two slabs a side swept out toward the bunches, the part showing between;
#   * EACH BUNCH tied with a brass ring and a madder bead, hanging to her shoulder in three
#     tapered locks of three lengths. The bunch tips give a little to the shoulders.
# Every wedge: (name, base, tip, base half size, tip half size, which tones, chamfer, across).
# ---------------------------------------------------------------------------
HAIR_CHAMFER = 0.014
LIT = tones("hair_top", "hair_lit", "hair")
DARK = tones("hair_lit", "hair", "hair_dark")
DEEP = tones("hair", "hair_dark", "hair_under")
WEDGES = [
    # four locks over the crown from the middle part, two a side
    ("top-left-front", (0.026, -0.112, 0.700), (0.186, -0.126, 0.626), (0.052, 0.028), (0.034, 0.018), LIT, 0.010, Y),
    ("top-left-back", (0.026, 0.030, 0.702), (0.192, 0.066, 0.606), (0.066, 0.028), (0.046, 0.018), DARK, 0.010, Y),
    ("top-right-front", (-0.026, -0.112, 0.700), (-0.186, -0.126, 0.626), (0.052, 0.028), (0.034, 0.018), LIT, 0.010, Y),
    ("top-right-back", (-0.026, 0.030, 0.702), (-0.192, 0.066, 0.606), (0.066, 0.028), (0.046, 0.018), DARK, 0.010, Y),
    # the fringe, hanging in front of the flat face (its front is at y -0.164), her right to her left
    ("fringe-1", (-0.168, -0.172, 0.666), (-0.182, -0.180, 0.540), (0.026, 0.024), (0.014, 0.013), DARK, 0.006, X),
    ("fringe-2", (-0.112, -0.180, 0.676), (-0.122, -0.186, 0.590), (0.036, 0.022), (0.022, 0.013), LIT, 0.006, X),
    ("fringe-3", (-0.042, -0.182, 0.680), (-0.052, -0.188, 0.578), (0.036, 0.024), (0.022, 0.014), DARK, 0.006, X),
    ("fringe-4", (0.034, -0.182, 0.680), (0.028, -0.188, 0.586), (0.036, 0.024), (0.022, 0.014), LIT, 0.006, X),
    ("fringe-5", (0.104, -0.180, 0.676), (0.100, -0.186, 0.576), (0.036, 0.022), (0.022, 0.013), DARK, 0.006, X),
    ("fringe-6", (0.166, -0.172, 0.666), (0.180, -0.180, 0.544), (0.026, 0.024), (0.014, 0.013), LIT, 0.006, X),
    # a long lock framing each cheek
    ("cheek-right", (-0.196, -0.120, 0.640), (-0.200, -0.124, 0.416), (0.020, 0.034), (0.013, 0.020), DARK, 0.008, X),
    ("cheek-left", (0.196, -0.120, 0.640), (0.200, -0.124, 0.424), (0.020, 0.034), (0.013, 0.020), DARK, 0.008, X),
    # over each ear, drawn back and down toward the bunch
    ("side-right-fore", (-0.203, -0.030, 0.650), (-0.206, 0.060, 0.486), (0.023, 0.050), (0.020, 0.040), DARK, 0.010, X),
    ("side-right-aft", (-0.201, 0.100, 0.656), (-0.203, 0.124, 0.500), (0.024, 0.056), (0.020, 0.040), DEEP, 0.010, X),
    ("side-left-fore", (0.203, -0.030, 0.650), (0.206, 0.060, 0.486), (0.023, 0.050), (0.020, 0.040), DARK, 0.010, X),
    ("side-left-aft", (0.201, 0.100, 0.656), (0.203, 0.124, 0.500), (0.024, 0.056), (0.020, 0.040), DEEP, 0.010, X),
]
#   the back, two slabs a side swept out toward the bunches; the part shows between them
BACK_SLABS = [
    ("back-right-in", (-0.042, 0.216, 0.692), (-0.082, 0.220, 0.452), (0.040, 0.016), (0.034, 0.013)),
    ("back-right-out", (-0.128, 0.212, 0.680), (-0.172, 0.196, 0.486), (0.044, 0.016), (0.036, 0.013)),
    ("back-left-in", (0.042, 0.216, 0.692), (0.082, 0.220, 0.452), (0.040, 0.016), (0.034, 0.013)),
    ("back-left-out", (0.128, 0.212, 0.680), (0.172, 0.196, 0.486), (0.044, 0.016), (0.036, 0.013)),
]
#   the bunches: where each is tied, and its three locks (tip, half size at the tie, at the tip, tones)
TIE = (0.214, 0.120, 0.478)
BUNCH_LOCKS = [
    ((0.282, 0.108, 0.330), (0.040, 0.034), (0.018, 0.016), DARK),
    ((0.262, 0.166, 0.318), (0.038, 0.032), (0.017, 0.015), DEEP),
    ((0.256, 0.078, 0.360), (0.034, 0.028), (0.015, 0.013), LIT),
]
NAPE_FOLLOW = 0.35
TIP_FOLLOW = 0.45


def build_hair(part):
    part.block("hair-crown", "head", X, Y, [((0, 0, 0.596), 0.198, (0.176, 0.200)), ((0, 0, 0.700), 0.184, (0.164, 0.188))],
               DARK, 0.024)
    part.block("hair-back", "head", X, Y, [((0, 0.178, 0.430), 0.190, 0.030), ((0, 0.176, 0.660), 0.200, 0.034)],
               DEEP, HAIR_CHAMFER)
    part.block("hair-nape-liner", "head", X, Y, [((0, 0.156, 0.392), 0.140, 0.010), ((0, 0.160, 0.452), 0.150, 0.011)],
               flat("hair_under"), 0.004)
    for name, base, tip, base_half, tip_half, paint, chamfer, across in WEDGES:
        part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, paint, chamfer, across)
    for name, foot, top, foot_half, top_half in BACK_SLABS:
        part.wedge("hair-" + name, "head", top, foot, top_half, foot_half, DARK, 0.008)
    for s in (1, -1):
        tie = Vector((s * TIE[0], TIE[1], TIE[2]))
        # the hair gathered into the tie from above
        part.wedge("hair-gather", "head", (s * 0.176, 0.090, 0.560), tie, (0.034, 0.040), (0.026, 0.026), DEEP, 0.010, X)
        for k, (tip, half_tie, half_tip, paint) in enumerate(BUNCH_LOCKS):
            tip = Vector((s * tip[0], tip[1], tip[2]))
            faces = part.wedge("hair-bunch", "head", tie + Vector((0, 0, 0.010)), tip, half_tie, half_tip, paint, 0.008, Y)
            part.bend(faces, lambda co, z0=tie.z, z1=tip.z: [("head", 1.0 - TIP_FOLLOW * _ramp(z0, z1, co.z)),
                                                              ("torso", TIP_FOLLOW * _ramp(z0, z1, co.z))])
        # the tie: a brass ring round the top of the bunch, and a madder bead on its outside
        down = (Vector((s * 0.270, 0.120, 0.340)) - tie).normalized()
        # the bunch swells just under the tie before it splits into its three locks
        bulb = axis_block(part, "hair-bulb", "head", tie + down * 0.012, tie + down * 0.060, (0.046, 0.042), (0.040, 0.036), DARK, 0.012)
        part.bend(bulb, lambda co, z0=tie.z: [("head", 1.0 - 0.2 * _ramp(z0, z0 - 0.06, co.z)), ("torso", 0.2 * _ramp(z0, z0 - 0.06, co.z))])
        axis_block(part, "hair-tie", "head", tie - down * 0.006, tie + down * 0.016, (0.040, 0.036), (0.040, 0.036), BRASS_T, 0.005)
        bead = tie + down * 0.005 + Vector((s * 0.044, -0.004, 0.0))
        part.block("hair-bead", "head", Y, Z, [(bead - Vector((s * 0.012, 0, 0)), 0.013, 0.013), (bead + Vector((s * 0.012, 0, 0)), 0.013, 0.013)],
                   MADDER_T, 0.0)


# ---------------------------------------------------------------------------
# THE TORSO AND THE TOP. A child's torso, narrower than Bebang's. A short wrapped top of
# indigo-black cotton; the wrap's edge crosses from her right shoulder to her left hip as a woven
# white band with a madder line down it. The BEAD COLLAR lies on her shoulders.
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.343)
TORSO_LOW = (0.128, 0.080)
TORSO_HIGH = (0.122, 0.076)
TORSO_CY = -0.002
ARM_Y, ARM_Z = 0.006, 0.288
SKIRT_TOP, SKIRT_HEM = tex.SKIRT_TOP, tex.SKIRT_HEM
SKIRT_FOLLOW = 0.62
SKIRT_N = 24
SKIRT_TOP_R = (0.140, 0.092)
SKIRT_HEM_R = (0.160, 0.106)
BELT = (0.204, 0.232)


def torso_half(z):
    t = (z - TORSO_Z[0]) / (TORSO_Z[1] - TORSO_Z[0])
    return (TORSO_LOW[0] + (TORSO_HIGH[0] - TORSO_LOW[0]) * t, TORSO_LOW[1] + (TORSO_HIGH[1] - TORSO_LOW[1]) * t)


def chest_y(z):
    return TORSO_CY - torso_half(z)[1]


def skirt_weights(co):
    down = _ramp(SKIRT_TOP - 0.010, SKIRT_HEM, co.z) ** 0.8
    leg = SKIRT_FOLLOW * down
    left = _smooth(_ramp(-0.070, 0.070, co.x))
    return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]


def skirt_r(f, inset=0.0):
    return (SKIRT_TOP_R[0] + (SKIRT_HEM_R[0] - SKIRT_TOP_R[0]) * f - inset,
            SKIRT_TOP_R[1] + (SKIRT_HEM_R[1] - SKIRT_TOP_R[1]) * f - inset)


def build_torso(part):
    top = proj_square("torso", "indigo", "indigo_deep", 0.90, ends=(2, "indigo_mid"))
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, TORSO_Z[0]), *TORSO_LOW), ((0, TORSO_CY, TORSO_Z[1]), *TORSO_HIGH)],
               top, 0.012)
    part.block("pelvis", "torso", X, Y, [((0, 0.0, 0.118), 0.054, 0.060), ((0, 0.0, 0.180), 0.054, 0.060)], flat("cotton_deep"), 0.0)
    neck = part.block("neck", "torso", X, Y, [((0, TORSO_CY, 0.326), 0.046, 0.044), ((0, TORSO_CY, 0.356), 0.044, 0.042)],
                      flat("skin_shade"), 0.008)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.332, 0.352, co.z)), ("head", 0.7 * _ramp(0.332, 0.352, co.z))])

    # THE WRAP'S EDGE: a woven white band 18 mm wide, 4 mm proud, from her right shoulder to her
    # left hip, and a madder line down its middle
    a, b = Vector((-0.064, 0.0, 0.326)), Vector((0.104, 0.0, BELT[1] - 0.004))
    mid = 0.5 * (a + b)
    u = (b - a).normalized()
    v = Vector((-u.z, 0.0, u.x))
    y = chest_y(mid.z)
    half = 0.5 * (b - a).length
    slab(part, "wrap-edge", "torso", (mid.x, y, mid.z), u, v, half, 0.009, 0.004, 0.004, COTTON_T, 0.0015)
    slab(part, "wrap-line", "torso", (mid.x, y, mid.z), u, v, half - 0.004, 0.0028, 0.004, 0.0056, flat("madder"), 0.001)

    # THE BEAD COLLAR: a wide flat collar lying on her shoulders and the top of her chest, its top
    # in columns of white and madder beads with brass at the front, its edge a madder row; and
    # below its front, a row of seven real beads, each its own block.
    def collar_ring(z, rx, rf, rb, droop):
        ring = super_ring(z, rx, rf, rb, 3.0, 20, TORSO_CY)
        for p in ring:
            if p.y < TORSO_CY:
                p.z -= droop * ((TORSO_CY - p.y) / rf) ** 2
        return ring

    A = collar_ring(0.352, 0.060, 0.056, 0.054, 0.010)
    B = collar_ring(0.342, 0.124, 0.088, 0.084, 0.034)
    C = [p - Vector((0, 0, 0.008)) for p in B]
    D = [p - Vector((0, 0, 0.008)) for p in A]

    def collar_paint(i, j):
        if i == 0:
            if j in (14, 15):
                return flat("brass")
            return flat("bead" if j % 2 == 0 else "madder")
        if i == 1:
            return flat("madder_deep")
        return flat("indigo_deep")

    part.loft("collar", "torso", [A, B, C, D, A], collar_paint, caps=(False, False))
    for k, (x, z, paint, r) in enumerate(((-0.066, 0.314, BEAD_T, 0.0095), (-0.044, 0.302, MADDER_T, 0.0095), (-0.022, 0.295, BEAD_T, 0.0095),
                                          (0.000, 0.290, BRASS_T, 0.0120), (0.022, 0.295, BEAD_T, 0.0095), (0.044, 0.302, MADDER_T, 0.0095),
                                          (0.066, 0.314, BEAD_T, 0.0095))):
        y = TORSO_CY - 0.090 - 0.002 * (k == 3)
        part.block("collar-bead", "torso", X, Z, [((x, y + 0.010, z), r, r), ((x, y - 0.006 - (0.004 if k == 3 else 0.0), z), r, r)], paint, 0.0)

    # THE BELT: woven indigo, a madder stripe round its middle, tied in front on her right
    def belt_ring(z, grow=0.0):
        hx, hy = TORSO_LOW[0] + 0.016 + grow, TORSO_LOW[1] + 0.016 + grow
        return super_ring(z, hx, hy, hy, 4.4, SKIRT_N, TORSO_CY)

    zs = (BELT[0], BELT[0] + 0.0115, BELT[0] + 0.0165, BELT[1])
    rings = [belt_ring(zs[0], -0.014), belt_ring(zs[0]), belt_ring(zs[1]), belt_ring(zs[1], 0.001), belt_ring(zs[2], 0.001),
             belt_ring(zs[2]), belt_ring(zs[3]), belt_ring(zs[3], -0.014)]
    belt_paint = lambda i, j: flat(("indigo_deep", "indigo", "madder", "madder", "madder", "indigo", "indigo_lit")[i])
    part.loft("belt", "torso", rings, belt_paint, caps=(False, False))
    kx, kz = -0.060, 0.216
    ky = TORSO_CY - (TORSO_LOW[1] + 0.016) - 0.006
    part.block("belt-knot", "torso", X, Z, [((kx, ky + 0.010, kz), 0.018, 0.016), ((kx, ky - 0.012, kz), 0.015, 0.013)], INDIGO_T, 0.006)
    for x0, x1, z1, lean in ((-0.068, -0.074, 0.150, -0.004), (-0.052, -0.044, 0.162, 0.004)):
        end = part.wedge("belt-end", "torso", (x0, ky - 0.010, kz - 0.010), (x1, ky - 0.014, z1), (0.010, 0.004), (0.008, 0.0035), INDIGO_T, 0.0)
        part.bend(end, skirt_weights)
        tip = part.block("belt-tip", "torso", X, Z, [((x1, ky - 0.008, z1 - 0.004), 0.0085, 0.0085), ((x1, ky - 0.024, z1 - 0.004), 0.0085, 0.0085)],
                         BRASS_T, 0.0)
        part.bend(tip, skirt_weights)

    # THE TAPIS: undyed cotton, wrapped, just above the knee, a little flared. Its bands are
    # GEOMETRY: each indigo band stands 2 mm proud of the cotton with its own edge.
    #   (from z, to z, tone, proud)
    bands = [(SKIRT_TOP, 0.180, "cotton", 0.0), (0.180, 0.172, "band", 0.002), (0.172, 0.167, "cotton", 0.0),
             (0.167, 0.159, "band", 0.002), (0.159, 0.130, "cotton", 0.0), (0.130, 0.114, "band", 0.0022),
             (0.114, 0.109, "madder", 0.0026), (0.109, 0.102, "band", 0.0022), (0.102, SKIRT_HEM, "cotton", 0.0)]

    def ring(z, extra=0.0):
        f = (SKIRT_TOP - z) / (SKIRT_TOP - SKIRT_HEM)
        hx, hy = skirt_r(f, -extra)
        return super_ring(z, hx, hy, hy, 4.4, SKIRT_N, TORSO_CY)

    rings, paints = [ring(SKIRT_TOP + 0.004, -0.024)], ["cotton_shade"]
    for k, (z0, z1, tone, proud) in enumerate(bands):
        rings.append(ring(z0, proud))
        paints.append(tone)                        # the band itself
        rings.append(ring(z1, proud))
        nxt = bands[k + 1][3] if k + 1 < len(bands) else None
        if nxt is not None:
            # the step to the next band: the edge of whichever is proud
            paints.append({"band": "band_deep", "madder": "madder_deep"}.get(tone if proud > nxt else bands[k + 1][2], "cotton_shade"))
    paints.append("cotton_shade")                 # the hem's underside
    rings.append(ring(SKIRT_HEM + 0.004, -0.010))
    skirt = part.loft("tapis", "torso", rings, lambda i, j: flat(paints[i]), caps=(False, False))
    part.bend(skirt, skirt_weights)
    # the wrap's edge, down the front on her left: a lit fold 16 mm wide, 3 mm proud
    for z0, z1 in ((SKIRT_TOP - 0.004, SKIRT_HEM + 0.002),):
        zt, zb = z0, z1
        ft, fb = (SKIRT_TOP - zt) / (SKIRT_TOP - SKIRT_HEM), (SKIRT_TOP - zb) / (SKIRT_TOP - SKIRT_HEM)
        yt = TORSO_CY - 0.90 * skirt_r(ft)[1] - 0.0026
        yb = TORSO_CY - 0.90 * skirt_r(fb)[1] - 0.0026
        wrap = part.block("tapis-wrap", "torso", X, Y, [((0.112, yt, zt), 0.008, 0.003), ((0.124, yb, zb), 0.008, 0.003)],
                          tones("cotton_lit", "cotton_lit", "cotton_shade"), 0.0)
        part.bend(wrap, skirt_weights)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests, at the heroes' length, a child's girth. A short cap
# sleeve of the indigo with a cotton edge; bare upper arm, forearm and the fist block. On her
# LEFT wrist a bead bracelet, narrower than the fist.
# ---------------------------------------------------------------------------
SLEEVE_END = tex.SLEEVE_END
FIST = (tex.WRIST, tex.HAND_END)


def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    arm_block(part, "sleeve", bone, s, [(0.098, (0.050, 0.054)), (SLEEVE_END, (0.054, 0.058))],
              proj_square(group, "indigo", "indigo_deep", 0.90, ends=(0, "indigo_deep")), 0.010)
    arm_block(part, "sleeve-edge", bone, s, [(SLEEVE_END - 0.006, (0.0565, 0.0605)), (SLEEVE_END + 0.002, (0.0555, 0.0595))],
              COTTON_T, 0.003)
    arm_block(part, "upper-arm", bone, s, [(SLEEVE_END - 0.010, (0.044, 0.047)), (ELBOW_X + 0.008, (0.042, 0.045))],
              proj_square(group, "skin", "skin_shade", 0.93, ends=(0, "skin_shade")), 0.010)
    arm_block(part, "forearm", fore, s, [(ELBOW_X - 0.016, (0.042, 0.045)), (FIST[0] + 0.006, (0.040, 0.043))],
              proj_square(group, "skin", "skin_shade", 0.93, ends=(0, "skin_shade")), 0.010)
    if s > 0:
        arm_block(part, "bracelet", fore, s, [(FIST[0] - 0.024, (0.0450, 0.0480)), (FIST[0] - 0.006, (0.0450, 0.0480))], BEAD_T, 0.004)
        arm_block(part, "bracelet-red", fore, s, [(FIST[0] - 0.0185, (0.0465, 0.0495)), (FIST[0] - 0.0115, (0.0465, 0.0495))], MADDER_T, 0.003)
    arm_block(part, "hand", fore, s, [(FIST[0], (0.048, 0.057)), (FIST[1], (0.048, 0.057))],
              proj_square(group, "skin", "skin_shade", 0.93), 0.013)


# ---------------------------------------------------------------------------
# THE LEGS. Bare, a child's, under the tapis. BAREFOOT (the audience brief overrides the
# Classics' footwear): a proper foot block, high at the heel and lower toward the front, then the
# toes as a STEP, with four toe lines drawn on its level top. A brass anklet on her left ankle.
# Feet turned out four degrees.
# ---------------------------------------------------------------------------
LEG_X = 0.0836
TOE_OUT = 4.0


def build_leg(part, s):
    bone = "leg-left" if s > 0 else "leg-right"
    part.block("leg", bone, X, Y, [((s * LEG_X, 0.0, 0.026), 0.040, 0.044), ((s * LEG_X, 0.0, 0.184), 0.044, 0.048)], SKIN_T, 0.010)
    if s > 0:
        part.block("anklet", bone, X, Y, [((s * LEG_X, 0.0, 0.040), 0.0445, 0.0485), ((s * LEG_X, 0.0, 0.050), 0.0445, 0.0485)], BRASS_T, 0.003)

    turn = math.radians(TOE_OUT) * s
    cs, sn = math.cos(turn), math.sin(turn)

    def turned(p):
        x, y = p.x - s * LEG_X, p.y - 0.02
        return Vector((s * LEG_X + x * cs - y * sn, 0.02 + x * sn + y * cs, p.z))

    # the foot: (y, half width, height of its top)
    foot = [(0.050, 0.038, 0.040), (0.020, 0.044, 0.044), (-0.050, 0.047, 0.036), (-0.112, 0.047, 0.030)]
    part.block("foot", bone, X, Z, [((s * LEG_X, y, 0.5 * top), hx, 0.5 * top) for y, hx, top in foot], SKIN_T, 0.009, move=turned)
    # the toes, a step lower and a little narrower
    part.block("toes", bone, X, Z, [((s * LEG_X, -0.106, 0.010), 0.045, 0.010), ((s * LEG_X, -0.142, 0.009), 0.040, 0.009)],
               proj_square("foot", "skin", "skin_shade", 0.93), 0.005, move=turned)



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
    """character-female-c.glb with its meshes swapped: skeleton, bind matrices and clips copied untouched,
    two elbows appended, and the four locomotion clips replaced by her own."""
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

    # her own clips, in place of the ones copied across (the clips script); the audience clips
    # the base rig does not have (`cheer`, `clap`) are appended as new animations
    node_of = {n.get("name"): i for i, n in enumerate(gltf["nodes"])}
    have = set(a["name"] for a in gltf["animations"])
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso Payo audience v2 (girl)"}
    gltf["extras"] = {"audience": "payo-audience-v2", "replaces": "nothing; not loaded by the game yet"}
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
    print("HEIGHT %.4f (the cast's range 0.6613 to 0.7928; a child aims about 0.67)" % hi.z)
    if not 0.6613 <= hi.z <= 0.7928:
        raise SystemExit("her height %.4f is outside the cast's range" % hi.z)
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
        raise SystemExit("paint the atlas first: py -3 tools/author_payo_audience_v2_girl_textures.py")
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
