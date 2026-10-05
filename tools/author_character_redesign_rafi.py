"""Build the Rafi (displayed: Ilyas) redesign PROTOTYPE: the same block kid, shaped and painted.

    py -3 tools/author_character_redesign_rafi_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_rafi.py

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/rafi/rafi-redesign.glb
    ArtSource/rafi/redesign-20261005/rafi_redesign.blend
beside the atlas the .glb names.

WHY. docs/CHARACTER_REDESIGN_DANTE.md section 13: the owner asked for the rest of the cast to be
redesigned the way Dante was, one hero an agent, each in its own files. This is Rafi's own copy
of Dante's model script, rewritten for him; it imports nothing of Dante's. The eleven rules of
that section are the brief. The short of them: the head stays the game's BOX head (the `shaped`
carving), detail is paint and pieces ADDED to blocks, nothing is invented that his current
model does not have, and a player should say "that is the same kid, with a lot more detail".

⚠️ A PROTOTYPE. NOTHING IN THE GAME LOADS IT. The folder is outside Resources, no roster row
points here, and team-rafi.glb, build_rafi_voxel.py and person_rafi.asset are not touched.

WHAT IS KEPT, measured off team-rafi.glb and its builder's tables (through the builder's own
remap, see `RY` and `AX` in the textures script):
  * the rig: the .glb is team-rafi.glb with its two meshes replaced and two elbow bones
    appended after the seven; skeleton, bind matrices and every other clip are copied across;
  * his measures: the head box 0.340 wide and 0.322 deep from 0.343 to 0.661, the torso from
    0.176 to 0.341 and wider at the shoulder (0.132) than the hip, legs to 0.176, arms straight
    out to 0.285, the top of his hair at 0.790, feet on zero, facing -y;
  * everything he already has and nothing he does not: a big mop of black hair as a core with
    a ring of chunky curls, the putong tied round the OUTSIDE of it with its knot and two tails
    behind his right ear, the long hair gathered into one tied tail so his back shows, silver
    ear drops, the shark tooth necklace on its silver chain, pectoral slabs, the two-tier bahag
    belt with its silver medallion and its knot, the front and back flaps piped in silver, a
    silver cuff on each wrist, bare legs, tsinelas with one strap. And his tattoos, mark for
    mark, now paint (the textures script).

WHAT CHANGES:
  * every block is a cut block: the torso narrows to the waist, limbs narrow to the wrist and
    ankle, hands and feet are their own blocks, curls are wedges of ten different heights;
  * the flat back of the hair gets BLOCKS, not drawing (rule 3): three hand-set slabs hang
    from under the band to the tail's root;
  * cloth and hair that lie across two bones BEND (rule 7): both flaps and the knot's tail
    take the legs toward the hem, the tied tail and the side tips take the torso;
  * two elbows (rule 8), placed between the armlet and the fern so no tattoo straddles the cut.

HOW A FACE FINDS ITS PAINT. A piece is tagged with a group ("head", "torso", "legL" ...) and each
of its faces goes to the island of the side its normal points at, at the place it sits in model
space. Loose blocks take a swatch or flat tones instead.
"""
import math
import os
import struct
import sys

import bpy
import bmesh
from mathutils import Vector

TOOLS = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(TOOLS)
sys.path.insert(0, TOOLS)
import build_person_voxel as bpv  # noqa: E402  the cast's glb reader and writer, not edited
import author_character_redesign_rafi_textures as tex  # noqa: E402  the island layout
import author_character_redesign_rafi_clips as clips  # noqa: E402  the prototype's own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/team-rafi.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/rafi")
OUT = os.path.join(FOLDER, "rafi-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/rafi/redesign-20261005/rafi_redesign.blend")
NAME = "rafi-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# Two bones the cast does not have (rule 8): an elbow under each arm bone, appended AFTER the
# seven so their indices and names do not move. A clip that does not key them leaves the arm
# straight. What it costs in the game is in docs/CHARACTER_REDESIGN_DANTE.md section 12
# (`CharacterVisual.PalmCentre` finds the hand from `arm-right` alone).
# ⚠️ HIS ELBOW IS AT 0.176, NOT DANTE'S 0.172. The armlet's outer rail ends at 0.174 and the
# fern on the forearm starts at 0.178; the joint goes between them so neither is cut in two.
ELBOW_X = tex.ELBOW_X
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
# SMALLER HEADS. Owner, 2026-10-05, after comparing sizes in the game on the whole lineup:
# "smaller heads are better", then "yes rebuild the heads at 84 for all seven". Everything is
# built and painted at the original's size, then every vertex that rides the `head` bone is
# scaled 0.84 about the head JOINT (see `build`). The paint keeps its place and nothing inside
# the head changes proportion, so the face, the measured eyes and the mouth are untouched.
HEAD_SCALE = 0.84
HEAD_JOINT = Vector((0.0, 0.00236, 0.343))   # the `head` bone of team-rafi.glb, in Blender space
FULL_TOP = 0.790         # the original's top, hair included (team-rafi.glb head-mesh bounds)
HEIGHT = HEAD_JOINT.z + (FULL_TOP - HEAD_JOINT.z) * HEAD_SCALE   # 0.7185: the top of his hair after the scale
SHARP_ANGLE = 40.0       # degrees: a chamfer is a hard edge, a soft corner or a taper is one form
TRIANGLE_BUDGET = 6000   # rule 10; the original is 6,256

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
    a cuff's end would land on the fingertips, a belt's ledge on the shoulders (rule 4).
    """
    return ("except", group, axis, name)


def proj_facing(group, direction, name):
    """The group's islands for the faces turned toward `direction`, one flat tone for the rest."""
    return ("facing", group, direction, name)


def proj_over(group, name):
    """The group's islands, but a face turned DOWN takes one flat tone: the under side of a slab
    that stands off the body (a pectoral) is in its own shade, not a smear of the front's paint."""
    return ("over", group, name)


def tones(top, side, under):
    """No drawing at all: one flat tone for faces turned up, one for the sides, one for under."""
    return ("tones", top, side, under)


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
        """Skin `rings` in order. `mapping` is one spec or f(segment, column) -> spec."""
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
        """A chamfered block through `stations` = [(centre, hu, hv)], its two ends chamfered too.

        Two stations of one size is a box. Different sizes taper it; moving the second centre
        off the axis leans it.
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

        The cast is rigid, one bone a vertex, and so is this model except where cloth or hair
        lies ACROSS two bones (rule 7): a flap rigid to the torso is walked through by the legs.
        """
        for f in faces:
            for v in f.verts:
                self.blend[v] = weights(v.co)

    def wedge(self, name, bone, base, tip, base_half, tip_half, mapping, chamfer, across=X):
        """A block standing on `base` and narrowing to `tip`: a hair clump, a loose cloth end."""
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
            elif kind == "facing":
                spec = proj(spec[1]) if face.normal.dot(Vector(spec[2])) > 0.6 else flat(spec[3])
            elif kind == "over":
                spec = flat(spec[2]) if face.normal.z < -0.6 else proj(spec[1])
            elif kind == "tones":
                spec = flat(spec[1] if face.normal.z > 0.45 else (spec[3] if face.normal.z < -0.45 else spec[2]))
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
            else:
                _, name, ur, vr, along = spec
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


# ---------------------------------------------------------------------------
# THE HEAD. The game's box (0.340 wide, 0.322 deep, 0.343 to 0.661), in the `shaped` carving the
# owner picked on Dante (rule 1): superellipse rings of exponent 3.4 to 4.2, cheek fullness at
# 0.428, a jaw that tapers to the neck. The brow ledge at 0.524 is cut from 11 mm to 4 (rule 12:
# anything that makes the block read as an anatomical face is softened) and there is no nose.
#     rows are (z, half width, depth to the front, depth to the back, exponent)
# ---------------------------------------------------------------------------
HEAD_CENTRE = Vector((0.0, 0.0, 0.50))
HEAD_N = 20
HEAD_ROWS = [
    # DANTE'S HEAD SHAPE, on Rafi's box (the same donor box: 0.340 by 0.322, 0.343 to 0.661).
    # Owner, 2026-10-05: "dante has a much more rounded face shape", then: apply it to Rafi.
    # These follow Dante's `shaped` rows: soft superellipse corners (3.4 to 4.2), fullest at the
    # cheeks (0.181 against 0.171 at the temples, 6 per cent), narrowing gently to the crown,
    # the lower corners ROUNDED IN toward the chin.
    # WHAT DIFFERS FROM DANTE'S, AND WHY:
    #   THE JAW ROUNDS IN GENTLY AND CLOSES IN THE CHEST. Dante's jaw turns under about 38
    #   degrees from 0.388 down and his standing collar hides it. Rafi has no collar. v01 to v09
    #   had Dante's taper and it caught the toon shadow as dark facets along the jaw; v10 and v11
    #   squared the jaw, which cured that and left him a hard box. Now the taper is spread over
    #   the last third of the head (0.432 down to 0.372, 17 mm in over 60: about 16 degrees),
    #   and the tight closing rings sit at 0.345 and 0.336, the last one INSIDE the chest (its
    #   top is 0.3406), behind the necklace.
    #   THE FACE IS ONE FLAT PLANE. From 0.400 (under the mouth) to 0.604 (behind the putong) the
    #   front depth is one value, 0.166. In the game's two-band toon shader any ring where the front
    #   steps in or out, a brow swell or a cheek standing proud, flips the light band and draws a
    #   straight line across the face (the owner saw it on Dante in Unity; Blender hid it). Cheek
    #   fullness lives only in the WIDTH. No brow, no front cheek bulge.
    (0.336, 0.118, 0.124, 0.116, 3.4),
    (0.345, 0.150, 0.153, 0.145, 3.8),
    (0.372, 0.164, 0.162, 0.154, 4.0),
    (0.400, 0.173, 0.166, 0.159, 4.2),
    (0.432, 0.181, 0.166, 0.162, 4.2),
    (0.470, 0.176, 0.166, 0.162, 4.2),
    (0.512, 0.171, 0.166, 0.162, 4.2),
    (0.530, 0.171, 0.166, 0.162, 4.2),
    (0.604, 0.169, 0.166, 0.162, 4.2),
    (0.646, 0.162, 0.158, 0.158, 3.8),
    (0.661, 0.140, 0.136, 0.140, 3.4),
]


def super_ring(z, rx, rf, rb, e, n=HEAD_N):
    out = []
    for j in range(n):
        t = 2.0 * math.pi * j / n
        c, s = math.cos(t), math.sin(t)
        a = math.copysign(abs(c) ** (2.0 / e), c)
        b = math.copysign(abs(s) ** (2.0 / e), s)
        out.append(Vector((a * rx, 0.001 + b * (rb if b >= 0 else rf), z)))
    return out


def build_head(part):
    skin = proj("head")
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], skin)
    # NO NOSE (rule 12). A wedge stood here through v07; the owner found the redesigned faces
    # "too human". The cast has no nose and neither does he.
    silver = tones("silver_lit_tone", "silver_tone", "silver_dark_tone")
    for s in (1, -1):
        # ears: small blocks, as the original's are, narrowing a little outward
        part.block("ear", "head", Y, Z, [((s * 0.164, 0.012, 0.456), 0.030, 0.042), ((s * 0.216, 0.016, 0.456), 0.024, 0.034)],
                   skin, 0.010)
        # his silver ear drop, hung under the lobe (the original's `earring-*` box, tapered)
        part.block("ear-drop", "head", X, Y, [((s * 0.190, 0.012, 0.420), 0.011, 0.010), ((s * 0.190, 0.012, 0.372), 0.008, 0.007)],
                   silver, 0.004)


# ---------------------------------------------------------------------------
# THE HAIR. His is the biggest in the cast and most of his silhouette: a CORE with a ring of
# chunky curls standing on it at graded heights (CHARACTER_MODEL_METHOD.md section 2; the
# builder's v16 note: stacked layers read as a cake, lumps of one height as a crate), a fringe
# over the band, a lock in front of each ear, the sides falling BEHIND the ears to the shoulder,
# and below the band the long hair gathered into ONE tied tail so his back tattoo can be seen.
# All of it is three flat tones by which way a face points (rule 3). Every piece is set by hand.
#   curls: (name, foot, top, half size at the foot, half size at the top)
# The original's ten are in the same places; their tops are now ten different heights, the
# tallest (0.790) still the middle of the front arc.
# ---------------------------------------------------------------------------
CURLS = [
    ("front-left",  (0.148, -0.044, 0.660), (0.156, -0.052, 0.734), (0.052, 0.052), (0.036, 0.036)),
    ("front-midl",  (0.078, -0.054, 0.676), (0.084, -0.062, 0.774), (0.054, 0.054), (0.038, 0.038)),
    ("front-mid",   (0.000, -0.056, 0.690), (-0.004, -0.064, 0.790), (0.054, 0.054), (0.038, 0.038)),
    ("front-midr",  (-0.080, -0.054, 0.672), (-0.088, -0.060, 0.766), (0.054, 0.054), (0.037, 0.037)),
    ("front-right", (-0.150, -0.044, 0.660), (-0.158, -0.050, 0.742), (0.052, 0.052), (0.035, 0.035)),
    ("ridge-left",  (0.066, 0.066, 0.686), (0.070, 0.074, 0.782), (0.054, 0.054), (0.038, 0.038)),
    ("ridge-right", (-0.068, 0.068, 0.684), (-0.074, 0.076, 0.776), (0.054, 0.054), (0.037, 0.037)),
    ("back-left",   (0.128, 0.148, 0.660), (0.136, 0.158, 0.736), (0.052, 0.052), (0.035, 0.035)),
    ("back-mid",    (0.000, 0.164, 0.674), (0.004, 0.176, 0.762), (0.054, 0.054), (0.037, 0.037)),
    ("back-right",  (-0.130, 0.148, 0.660), (-0.140, 0.156, 0.746), (0.052, 0.052), (0.036, 0.036)),
]
#   the fringe over the band: three slabs, three heights (the original is one box)
FRINGE = [
    ("fringe-left",  (0.112, -0.150, 0.600), (0.112, -0.146, 0.706), (0.058, 0.026), (0.054, 0.022)),
    ("fringe-mid",   (0.000, -0.153, 0.600), (0.000, -0.148, 0.716), (0.056, 0.027), (0.052, 0.023)),
    ("fringe-right", (-0.112, -0.149, 0.600), (-0.112, -0.145, 0.698), (0.058, 0.025), (0.054, 0.021)),
]
#   below the band, behind: three slabs laid over the back mass, running in to the tail's root
BACK_SLABS = [
    ("back-slab-left",  (0.134, 0.238, 0.562), (0.124, 0.241, 0.430), (0.076, 0.014), (0.056, 0.011)),
    ("back-slab-mid",   (0.000, 0.240, 0.562), (0.000, 0.244, 0.466), (0.058, 0.015), (0.050, 0.013)),
    ("back-slab-right", (-0.136, 0.238, 0.562), (-0.144, 0.241, 0.452), (0.072, 0.014), (0.050, 0.011)),
]
# 0.22, NOT 0.72. At 0.72 the tip stayed on his back while the root went with the head: turned
# 55 degrees, the tail was pulled into a thin diagonal spike from the nape to the belt. A tied
# tail swings WITH the head; the tip only lags a little.
TAIL_FOLLOW = 0.22    # how much of the torso the tail's tip takes (rule 7)


def hair_weights(co):
    """Hair that hangs below the jaw goes with the shoulders toward its tip: the tied tail lies
    down his back when the head turns instead of sweeping round like a rod."""
    t = TAIL_FOLLOW * _ramp(0.400, 0.230, co.z)
    return [("head", 1.0 - t), ("torso", t)]


def build_hair(part):
    hair = tones("hair_top", "hair", "hair_under")
    deep = tones("hair", "hair_under", "hair_under")
    cloth = tones("putong_lit_tone", "putong_tone", "putong_dark_tone")
    part.block("hair-core", "head", X, Y, [((0, 0.034, 0.604), 0.212, 0.182), ((0, 0.034, 0.722), 0.196, 0.164)],
               deep, 0.022)
    for name, foot, top, foot_half, top_half in CURLS:
        part.wedge("hair-curl-" + name, "head", foot, top, foot_half, top_half, hair, 0.016)
    for name, foot, top, foot_half, top_half in FRINGE:
        part.wedge("hair-" + name, "head", foot, top, foot_half, top_half, hair, 0.010)
    # a lock in front of each ear, a mass behind it, and that mass's tip on the shoulder; his
    # left tip hangs lower than his right, as in the original
    for s, tip_z, tip_y in ((1, 0.326, 0.156), (-1, 0.344, 0.146)):
        part.wedge("hair-temple", "head", (s * 0.197, -0.070, 0.642), (s * 0.196, -0.066, 0.500), (0.022, 0.040), (0.014, 0.022),
                   hair, 0.009)
        part.block("hair-side", "head", X, Y, [((s * 0.210, 0.128, 0.420), 0.020, 0.074), ((s * 0.212, 0.128, 0.602), 0.024, 0.078)],
                   hair, 0.011)
        faces = part.wedge("hair-side-tip", "head", (s * 0.209, 0.150, 0.432), (s * 0.207, tip_y, tip_z), (0.019, 0.044), (0.013, 0.021),
                           hair, 0.008)
        part.bend(faces, lambda co: [("head", 1.0 - 0.25 * _ramp(0.400, 0.326, co.z)), ("torso", 0.25 * _ramp(0.400, 0.326, co.z))])
    # the mass down the back of the head, one tone deeper, and the slabs that lie on it
    part.block("hair-back", "head", X, Y, [((0, 0.171, 0.430), 0.208, 0.061), ((0, 0.171, 0.650), 0.220, 0.061)],
               deep, 0.014)
    for name, top, foot, top_half, foot_half in BACK_SLABS:
        part.wedge("hair-" + name, "head", top, foot, top_half, foot_half, hair, 0.008)
    # THE TAIL: a root at the nape, the tie in his putong's cloth, then the tail to the belt
    part.block("hair-tail-root", "head", X, Y, [((0, 0.212, 0.472), 0.050, 0.042), ((0, 0.214, 0.398), 0.045, 0.040)],
               hair, 0.012)
    part.block("tail-tie", "head", X, Y, [((0, 0.216, 0.405), 0.048, 0.047), ((0, 0.216, 0.379), 0.046, 0.045)],
               cloth, 0.006)
    tail = part.block("hair-tail", "head", X, Y,
                      [((0, 0.215, 0.386), 0.039, 0.037), ((0, 0.218, 0.334), 0.036, 0.035), ((0, 0.223, 0.284), 0.031, 0.032),
                       ((0.002, 0.228, 0.246), 0.025, 0.027), ((0.004, 0.233, 0.208), 0.011, 0.014)], hair, 0.009)
    part.bend(tail, hair_weights)


# ---------------------------------------------------------------------------
# THE PUTONG. A headcloth tied round the OUTSIDE of the hair (the builder's `_rafi_headband`):
# a ring that clears the side hair and the back mass, rides 14 mm up the brow and drops 20 mm
# to the knot, the knot behind his right ear, two tails over the back hair, each its own
# length and lean. The ring's outer face wears a drawn swatch; the rest are flat tones.
# ---------------------------------------------------------------------------
BAND_RX, BAND_RF, BAND_RB, BAND_Y0 = 0.239, 0.216, 0.215, 0.029
BAND_HALF = (0.012, 0.024)    # half its thickness, half its height
BAND_TIP = 0.08               # metres of rise per metre toward the face


def band_point(theta):
    c, s = math.cos(theta), math.sin(theta)
    px = math.copysign(abs(c) ** (2.0 / 5.0), c) * BAND_RX
    py = BAND_Y0 + math.copysign(abs(s) ** (2.0 / 5.0), s) * (BAND_RB if s >= 0 else BAND_RF)
    return Vector((px, py, 0.582 - BAND_TIP * py))


def build_putong(part):
    cloth = tones("putong_lit_tone", "putong_tone", "putong_dark_tone")
    count = 20
    start = math.radians(103.0)       # the seam, under the knot (behind, a little to his right)
    sections, params = [], []
    for i in range(count + 1):
        theta = start + 2.0 * math.pi * i / count
        p = band_point(theta)
        tangent = band_point(theta + 0.02) - band_point(theta - 0.02)
        n = Vector((tangent.y, -tangent.x, 0.0)).normalized()
        if n.dot(Vector((p.x, p.y - BAND_Y0, 0.0))) < 0:
            n = -n
        sections.append(rect_ring(p, n, Z, BAND_HALF[0], BAND_HALF[1], 0.005))
        params.append(i / float(count))
    # round a section: 0 is the outer face, 1 and 2 the top, 3 to 5 the inside, 6 and 7 under
    ring = sections[0]
    lengths = [(ring[j] - ring[(j + 1) % 8]).length for j in range(8)]
    outer = lengths[0] / sum(lengths)

    def paint(i, j):
        if j == 0:
            return swatch("putong", (0.0, 1.0), (0.10, 0.10 + 0.80 / outer), "columns")
        return flat("putong_lit_tone" if j in (1, 2) else "putong_dark_tone")

    part.loft("putong", "head", sections, paint, caps=(False, False), params=params)
    part.block("putong-knot", "head", X, Y, [((-0.057, 0.264, 0.524), 0.044, 0.022), ((-0.057, 0.267, 0.602), 0.050, 0.026)],
               cloth, 0.010)
    part.wedge("putong-tail-a", "head", (-0.076, 0.269, 0.538), (-0.102, 0.271, 0.378), (0.019, 0.009), (0.014, 0.007), cloth, 0.004)
    part.wedge("putong-tail-b", "head", (-0.036, 0.269, 0.534), (-0.019, 0.271, 0.404), (0.018, 0.009), (0.013, 0.007), cloth, 0.004)


# ---------------------------------------------------------------------------
# THE TORSO. Bare. Wider at the shoulder than the hip (the builder's 0.82 taper), with a waist.
#     rows are (z, half width, half depth)
# ---------------------------------------------------------------------------
TORSO_CY = 0.001
TORSO_ROWS = [(0.176, 0.106, 0.088), (0.222, 0.109, 0.088), (0.300, 0.129, 0.089), (0.3406, 0.132, 0.087)]


def torso_half(z):
    for (z0, w0, d0), (z1, w1, d1) in zip(TORSO_ROWS, TORSO_ROWS[1:]):
        if z <= z1 or z1 == TORSO_ROWS[-1][0]:
            f = (z - z0) / (z1 - z0)
            return (w0 + (w1 - w0) * f, d0 + (d1 - d0) * f)


FLAP_FOLLOW = 0.62    # how much of a leg's swing a flap's hem takes


def flap_weights(co):
    """A flap hangs from the belt (torso) and its hem goes with the legs: his left half mostly
    with his left leg, his right mostly with his right, so it turns a little as he strides.

    ⚠️ THE MIX IS WIDER THAN THE FLAP ON PURPOSE. v01 gave each edge wholly to its own leg (a
    ramp from -0.050 to 0.050, the flap's own width). In `walk` the legs swing opposite ways, so
    one edge went forward and the other back and the flap wrung itself into a thin diagonal
    sliver. v03 widened the mix to 0.075 and it still came to a point. Across -0.110 to 0.110 each edge
    is 0.72 its own leg and 0.28 the other, a quarter of the swing: it stays a flap, and the
    price is that the inner edge of a thigh swung far forward comes through its corner.
    """
    down = _ramp(0.186, 0.085, co.z) ** 0.8
    leg = FLAP_FOLLOW * down
    left = _ramp(-0.110, 0.110, co.x)
    return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]


def build_torso(part):
    skin = proj("torso")
    silver = tones("silver_lit_tone", "silver_tone", "silver_dark_tone")
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, z), w, d) for z, w, d in TORSO_ROWS], skin, 0.014)
    # the pectoral slabs: muscle as FORM (the builder's ruling), 10 mm proud, "a bit" and no more
    for s in (1, -1):
        part.block("pectoral", "torso", X, Z, [((s * 0.065, -0.083, 0.2834), 0.057, 0.0274), ((s * 0.065, -0.0985, 0.2834), 0.054, 0.0250)],
                   proj_over("torso", "skin_shade_tone"), 0.007)

    # THE NECKLACE. The silver chain lies on the body: round the back of the neck, over the
    # shoulders, down the pectorals to the sternum, 6 mm off what it crosses (the builder's v31).
    half = [(0.0, 0.068, 0.3485), (0.044, 0.054, 0.3485), (0.066, 0.012, 0.3485), (0.064, -0.044, 0.3443),
            (0.056, -0.082, 0.3328), (0.042, -0.106, 0.3140), (0.028, -0.111, 0.2952), (0.014, -0.112, 0.2811),
            (0.0, -0.112, 0.2748)]
    path = [Vector(p) for p in half] + [Vector((-x, y, z)) for x, y, z in half[-2:0:-1]]
    rings = []
    for i in range(len(path) + 1):
        p = path[i % len(path)]
        tangent = (path[(i + 1) % len(path)] - path[(i - 1) % len(path)]).normalized()
        u = tangent.cross(Z)
        u = (u if u.length > 1e-6 else X).normalized()
        v = tangent.cross(u).normalized()
        rings.append([p + u * (math.cos(a) * 0.0062) + v * (math.sin(a) * 0.0062)
                      for a in (2.0 * math.pi * (j + 0.5) / 4 for j in range(4))])
    part.loft("chain", "torso", rings, silver, caps=(False, False))
    # three shark teeth, each its own size and lean, the big one on a silver bail
    part.block("tooth-bail", "torso", X, Y, [((0, -0.124, 0.2660), 0.008, 0.009), ((0, -0.124, 0.2810), 0.008, 0.009)], silver, 0.003)
    part.wedge("shark-tooth", "torso", (0.0, -0.126, 0.2700), (0.001, -0.128, 0.2320), (0.018, 0.008), (0.0025, 0.003),
               swatch("tooth"), 0.003)
    teeth = tones("cream_tone", "cream_tone", "cream_shade_tone")
    part.wedge("shark-tooth-l", "torso", (0.031, -0.123, 0.2880), (0.038, -0.125, 0.2640), (0.009, 0.006), (0.002, 0.002), teeth, 0.002)
    part.wedge("shark-tooth-r", "torso", (-0.031, -0.123, 0.2870), (-0.035, -0.125, 0.2665), (0.009, 0.006), (0.002, 0.002), teeth, 0.002)

    # THE BAHAG'S WAIST: two tiers and a dark silver seam, each a block standing off the body
    # (CAST_CLOTHING_STYLE.md rule 4). Their drawing is the torso's islands at their heights.
    for name, (lo, hi), proud, tone in (("belt-low", tex.BELT_LOW, 0.012, "teal_dark_tone"),
                                        ("belt-high", tex.BELT_HIGH, 0.012, "teal_deep_tone"),
                                        ("belt-seam", tex.BELT_SEAM, 0.0145, "silver_dark_tone")):
        (wl, dl), (wh, dh) = torso_half(lo), torso_half(hi)
        part.block(name, "torso", X, Y, [((0, TORSO_CY, lo), wl + proud, dl + proud), ((0, TORSO_CY, hi), wh + proud, dh + proud)],
                   proj_except("torso", 2, tone), 0.004 if name != "belt-seam" else 0.0015)
    # the medallion: his one big fastening, a silver plate with a sea teal inset
    y0 = TORSO_CY - torso_half(0.205)[1] - 0.012
    ahead = (0, -1, 0)
    zc = 0.5 * (tex.RY(0.240) + tex.RY(0.300))
    hz = 0.5 * (tex.RY(0.300) - tex.RY(0.240))
    part.block("medallion", "torso", X, Z, [((0, y0 + 0.002, zc), 0.034, hz), ((0, y0 - 0.017, zc), 0.034, hz)],
               proj_facing("torso", ahead, "silver_dark_tone"), 0.006)
    part.block("medallion-inset", "torso", X, Z, [((0, y0 - 0.015, zc), 0.0175, 0.0125), ((0, y0 - 0.0235, zc), 0.014, 0.0095)],
               proj_facing("torso", ahead, "teal_dark_tone"), 0.004)
    # the bahag's knot, on his right hip, and its loose end over the thigh.
    # ⚠️ MOVED 24 mm FORWARD of the original's. There it sat under the arm, and `idle` drops the
    # arm straight onto it: a knot nobody could see, cut through by the forearm. At the belt's
    # front corner it clears the arm (whose front face is at y -0.058) and reads from the front.
    cloth = tones("teal_tone", "teal_dark_tone", "teal_deep_tone")
    part.block("bahag-knot", "torso", X, Y, [((-0.112, -0.083, 0.183), 0.021, 0.022), ((-0.113, -0.083, 0.223), 0.023, 0.024)],
               cloth, 0.009)
    faces = part.wedge("bahag-knot-tail", "torso", (-0.113, -0.088, 0.190), (-0.121, -0.090, 0.146), (0.012, 0.010), (0.007, 0.007),
                       cloth, 0.004)
    part.bend(faces, lambda co: [("torso", 1.0 - 0.8 * _ramp(0.186, 0.146, co.z)), ("leg-right", 0.8 * _ramp(0.186, 0.146, co.z))])

    # THE FLAPS. Front and back, each a hanging panel that leans out a little toward its hem,
    # piped in silver built as GEOMETRY (clothing doc rule 3): two edges and a hem in front, a
    # hem behind, as the original has them. They BEND (rule 7).
    soft = []
    soft += part.block("flap-front", "torso", X, Y, [((0, -0.1080, 0.200), 0.048, 0.008), ((0, -0.1100, 0.120), 0.046, 0.008),
                                                    ((0, -0.1150, 0.0765), 0.044, 0.008)],
                       proj_facing("flap", ahead, "teal_deep_tone"), 0.004)
    for s in (1, -1):
        soft += part.block("flap-front-edge", "torso", X, Y, [((s * 0.0465, -0.1090, 0.198), 0.0065, 0.0115), ((s * 0.0445, -0.1110, 0.120), 0.0065, 0.0115),
                                                             ((s * 0.0425, -0.1160, 0.084), 0.0065, 0.0115)], silver, 0.003)
    soft += part.block("flap-front-hem", "torso", Y, Z, [((-0.051, -0.1165, 0.0775), 0.0125, 0.0065), ((0.051, -0.1165, 0.0775), 0.0125, 0.0065)],
                       silver, 0.003)
    soft += part.block("flap-back", "torso", X, Y, [((0, 0.1100, 0.188), 0.062, 0.008), ((0, 0.1120, 0.135), 0.060, 0.008),
                                                   ((0, 0.1160, 0.0990), 0.058, 0.008)],
                       proj_facing("flap", (0, 1, 0), "teal_deep_tone"), 0.004)
    soft += part.block("flap-back-hem", "torso", Y, Z, [((-0.065, 0.1170, 0.1000), 0.0125, 0.0062), ((0.065, 0.1170, 0.1000), 0.0125, 0.0062)],
                       silver, 0.003)
    part.bend(soft, flap_weights)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests. `s` is +1 for his left, -1 for his right. The arm
# block is centred on y 0 (the original's is), though the bone sits 17 mm behind it.
# ---------------------------------------------------------------------------
ARM_Y, ARM_Z = 0.0, 0.288


def arm_block(part, name, bone, s, x0, x1, half0, half1, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x0, ARM_Y, ARM_Z), half0[0], half0[1]), ((s * x1, ARM_Y, ARM_Z), half1[0], half1[1])],
                      mapping, chamfer)


def build_arm(part, s):
    side = "L" if s > 0 else "R"
    bone, fore = ("arm-left", "forearm-left") if s > 0 else ("arm-right", "forearm-right")
    upper = proj_except("uarm" + side, 0, "skin_shade_tone")
    lower = proj_except("farm" + side, 0, "skin_shade_tone")
    # a deltoid cap just fuller than the arm, then two rigid blocks that overlap at the elbow as
    # a toy's joint does; each block has its own drawing (see the textures script)
    arm_block(part, "deltoid", bone, s, 0.100, 0.147, (0.064, 0.066), (0.062, 0.064), upper, 0.016)
    arm_block(part, "arm", bone, s, 0.104, ELBOW_X + 0.010, (0.058, 0.060), (0.056, 0.058), upper, 0.012)
    arm_block(part, "forearm", fore, s, ELBOW_X - 0.012, 0.224, (0.055, 0.057), (0.053, 0.055), lower, 0.011)
    arm_block(part, "cuff", fore, s, tex.CUFF_X[0], tex.CUFF_X[1], (0.066, 0.068), (0.066, 0.068),
              proj_except("farm" + side, 0, "silver_dark_tone"), 0.005)
    # a plain block hand, narrowing to the fingers; fingers and thumb are drawn
    arm_block(part, "hand", fore, s, 0.219, 0.285, (0.054, 0.060), (0.043, 0.055), lower, 0.013)


# ---------------------------------------------------------------------------
# THE LEGS. Short, the cast's 24 per cent, bare: a thigh that stands over a narrower shin, a
# foot block that is low at the toes, and under it his tsinelas: a cream sole, a brown bed, one
# sea teal strap.
# ---------------------------------------------------------------------------
LEG_X = 0.084


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    skin = proj_except(group, 2, "skin_shade_tone")
    part.block("shin", bone, X, Y, [((s * LEG_X, -0.001, 0.050), 0.052, 0.059), ((s * LEG_X, -0.001, 0.150), 0.055, 0.062)],
               skin, 0.010)
    part.block("thigh", bone, X, Y, [((s * LEG_X, 0.0, 0.1366), 0.062, 0.072), ((s * LEG_X, 0.0, 0.200), 0.064, 0.074)],
               skin, 0.012)

    def station(y, half, z0, z1, cut):
        return rect_ring((s * 0.083, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)

    foot = [(-0.130, 0.048, 0.029, 0.045, 0.005), (-0.119, 0.060, 0.027, 0.054, 0.010), (-0.060, 0.063, 0.027, 0.0637, 0.011),
            (0.058, 0.060, 0.027, 0.0637, 0.011), (0.067, 0.052, 0.029, 0.058, 0.005)]
    part.loft("foot", bone, [station(*row) for row in foot], proj(group))
    part.block("tsinelas-strap", bone, Y, Z, [((s * 0.021, -0.086, 0.0525), 0.014, 0.0115), ((s * 0.145, -0.086, 0.0525), 0.014, 0.0115)],
               tones("teal_tone", "teal_tone", "teal_dark_tone"), 0.005)
    part.block("tsinelas-bed", bone, X, Z, [((s * 0.083, -0.138, 0.0275), 0.062, 0.0048), ((s * 0.083, -0.092, 0.0275), 0.073, 0.0048),
                                           ((s * 0.083, 0.084, 0.0275), 0.070, 0.0048)],
               tones("bed_tone", "bed_tone", "bed_dark_tone"), 0.003)
    part.block("tsinelas-sole", bone, X, Z, [((s * 0.083, -0.144, 0.0125), 0.065, 0.0125), ((s * 0.083, -0.094, 0.0125), 0.079, 0.0125),
                                            ((s * 0.083, 0.090, 0.0125), 0.075, 0.0125)],
               tones("cream_tone", "cream_tone", "cream_shade_tone"), 0.005)


# ---------------------------------------------------------------------------
# THE BUILD
# ---------------------------------------------------------------------------

def mesh_arrays(obj):
    """A mesh as glTF arrays: +y up, -z the way Blender's -y faces, v flipped, up to four bones a vertex."""
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
    """team-rafi.glb with its meshes swapped: skeleton, bind matrices and clips copied across."""
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

    # the two elbows: a node under each arm, a joint and a bind matrix appended after the seven.
    # Every rest rotation in this rig is identity, so a bind matrix is the inverse translation.
    node_of = {n.get("name"): i for i, n in enumerate(gltf["nodes"])}
    skin = gltf["skins"][0]
    raw = struct.unpack("<%df" % (16 * len(BONES)), accessor_raw)
    matrices = [raw[k * 16:(k + 1) * 16] for k in range(len(BONES))]
    for name, parent, side in EXTRA_BONES:
        world = (side * ELBOW_X, ARM_Z, -ARM_Y)
        pw = [-matrices[BONES.index(parent)][12 + a] for a in range(3)]
        gltf["nodes"].append({"name": name, "translation": [world[a] - pw[a] for a in range(3)]})
        gltf["nodes"][node_of[parent]].setdefault("children", []).append(len(gltf["nodes"]) - 1)
        skin["joints"].append(len(gltf["nodes"]) - 1)
        matrices.append((1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -world[0], -world[1], -world[2], 1))
    binds = add([tuple(float(x) for x in m) for m in matrices], "f", "MAT4", 5126)
    for other in gltf["skins"]:
        # every skin of the file is the same seven joints; all of them take the nine
        other["joints"] = list(skin["joints"])
        other["inverseBindMatrices"] = binds

    # the prototype's own locomotion clips, in place of the ones copied across (the clips script)
    node_of = {n.get("name"): i for i, n in enumerate(gltf["nodes"])}
    kinds = {"rotation": "VEC4", "translation": "VEC3", "scale": "VEC3"}
    for anim in gltf["animations"]:
        if anim["name"] not in clips.CLIPS:
            continue
        anim["samplers"], anim["channels"] = [], []
        for (bone, path), keys in clips.CLIPS[anim["name"]]().items():
            times = add([(t,) for t, _ in keys], "f", "SCALAR", 5126, minmax=True)
            values = add([v for _, v in keys], "f", kinds[path], 5126)
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (rafi)"}
    gltf["extras"] = {"prototype": "character-redesign-20261005", "replaces": "nothing; not loaded by the game"}
    gltf["images"] = [{"uri": tex.ATLAS_NAME, "name": NAME + "-atlas"}]
    gltf["textures"] = [{"sampler": 0, "source": 0, "name": NAME + "-atlas"}]
    # Bilinear with mips: the stock colormap's nearest filter would stair-step a drawn line.
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
    print("triangles %d  (budget %d, the original is 6256)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if abs(hi.z - HEIGHT) > 0.004:
        raise SystemExit("the top is at %.4f, the original's is %.3f" % (hi.z, HEIGHT))
    if tris > TRIANGLE_BUDGET:
        raise SystemExit("%d triangles is over the budget of %d" % (tris, TRIANGLE_BUDGET))
    # CharacterVisual.PalmCentre: the arm must run along x, and the hand's top must sit where a
    # carried tsinelas is parked (bone-space +0.0555, with HandTopLift 0.0617 over the palm).
    body = objects[0]
    group = (body.vertex_groups["arm-right"].index, body.vertex_groups["forearm-right"].index)
    arm = [v.co for v in body.data.vertices if v.groups[0].group in group]
    size = [max(p[a] for p in arm) - min(p[a] for p in arm) for a in range(3)]
    if not (size[0] > size[1] and size[0] > size[2]):
        raise SystemExit("arm-right no longer runs along x: %s" % size)
    far = min(p.x for p in arm)
    hand = [p for p in arm if p.x < far + size[0] / 8.0]
    top = max(p.z for p in hand) - ARM_Z
    print("hand top %.4f above the arm bone (the original's is 0.0617)" % top)
    if not 0.045 <= top <= 0.066:
        raise SystemExit("the hand's top moved; a carried tsinelas would float or sink")


def build(armature, material):
    body, head = Part("body-mesh"), Part("head-mesh")
    build_torso(body)
    build_arm(body, 1)
    build_arm(body, -1)
    build_leg(body, 1)
    build_leg(body, -1)
    build_head(head)
    build_hair(head)
    build_putong(head)
    objects = []
    for part in (body, head):
        part.resolve_uvs()
        if part is head:
            # AFTER the paint is placed (it is projected from the full-size shape): the head comes
            # in. A piece that bends (the tied tail, the side tips) is scaled by its own share of
            # the head bone, so its root shrinks with the head and its tip stays with the back.
            for v in part.bm.verts:
                mix = part.blend.get(v)
                share = 1.0 if not mix else sum(w for b, w in mix if b == "head") / sum(w for _, w in mix)
                v.co = HEAD_JOINT + (v.co - HEAD_JOINT) * (1.0 + (HEAD_SCALE - 1.0) * share)
        obj = part.to_object(armature)
        obj.data.materials.append(material)
        objects.append(obj)
        print(part.name, " ".join("%s:%d" % (n, t) for n, t in part.pieces))
    verify(objects)
    return objects


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    # The rig, for the .blend and for posing: the original's, its meshes dropped.
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
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_rafi_textures.py")
    image = bpy.data.images.load(image_path)
    material = bpy.data.materials.new(NAME)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    node = nodes.new("ShaderNodeTexImage")
    node.image = image
    material.node_tree.links.new(node.outputs[0], nodes["Principled BSDF"].inputs["Base Color"])
    nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0

    objects = build(armature, material)
    write_glb(mesh_arrays(objects[0]), mesh_arrays(objects[1]), OUT)
    print("wrote " + OUT)
    os.makedirs(os.path.dirname(BLEND), exist_ok=True)
    image.filepath = bpy.path.relpath(image_path, start=os.path.dirname(BLEND))
    bpy.context.preferences.filepaths.save_version = 0   # no .blend1 beside it
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)
    print("wrote " + BLEND)


if __name__ == "__main__":
    main()
