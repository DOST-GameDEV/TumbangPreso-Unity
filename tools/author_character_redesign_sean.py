"""Build the Sean (displayed: Rago) redesign PROTOTYPE: the same block kid, shaped and painted.

    py -3 tools/author_character_redesign_sean_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_sean.py
    ... -- --out other.glb    (writes there instead, beside a copy of the atlas, and leaves the .blend alone)

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/sean/sean-redesign.glb
    ArtSource/sean/redesign-20261005/sean_redesign.blend
beside the atlas.

WHY. Owner, 2026-10-05 (docs/CHARACTER_REDESIGN_DANTE.md section 13): *"following dante's rework,
redesign the rest of the characters"*, with eleven rules that are each a correction the owner made
on Dante. This file is a copy of Dante's builder REWRITTEN for roster id `sean`. It imports
nothing from Dante's scripts, and nothing in the game loads what it writes: the folder is outside
Resources, no roster row points here, and team-sean.glb, build_iggy_voxel.py and
person_sean.asset are not touched.

WHO HE IS (tools/build_iggy_voxel.py, "the Heavyweight Fire Brawler", and team-sean.glb as it is
today, rendered and looked at from every side before a number here was typed). The cast's tallest
and widest kid, 0.848 to the tip of his flame against Dante's 0.785: longer legs (0.245) and a
longer torso (to 0.473) under the same head box. Everything he has is kept, nothing is added:
  bronze skin; a shaved head; a black mohawk from the forehead over the crown and down the back,
  ending in a tail on his nape; a red, orange and yellow flame fin on its front; three gold razor
  slits climbing over each ear on a buzzed patch; glaring ink eyes and a smirk hooked up on his
  left; big ears; a bare chest with raised pectorals; an open sleeveless red vest piped in gold
  (lapels, hem, shoulders, armholes) with a gold, orange and yellow sun on its back; a brown belt
  with gold rims and a big gold buckle with a leather core; red trunks with a gold stripe down
  the outside and a wide gold band at the thigh; a sliver of bare shin; red high-tops with an
  orange tongue and ankle strap on a white sole; heavy bare arms with a deltoid cap and a bicep;
  red bracers with gold rims, a gold plate, a gold crest and an orange strap; plain block fists.

WHAT IS MEASURED OFF team-sean.glb AND KEPT (rule 10): the head box 0.340 wide and 0.320 deep
from 0.473 to 0.791; the torso from 0.245 to 0.473; the hip at 0.245; the arm bone at x 0.180,
height 0.430, straight out to a fist that ends at 0.405; the fist's top 0.058 above the bone,
where `CharacterVisual.PalmCentre` parks a carried tsinelas; feet on zero, facing -y.

WHAT CHANGES, and which rule asks for it:
  1  THE HEAD IS THE GAME'S BOX, `shaped`: superellipse rings of exponent 3.6 to 4.4, a brow
     cheeks, a jaw that tapers; no brow ledge and no nose (rule 12). HIS is cut heavier than Dante's: the jaw keeps more of its
     width and the cheek ring is the widest of the head, because a brawler's head is a brick.
  3  THE MOHAWK IS BLOCKS AND FLAT TONES. The original's flame is three boxes stacked like a
     cake and its back is one flat black strip. Here the flame is a red bed with three tongues
     of three heights swept back, each an orange wedge with a yellow wedge in it, and the strip
     down the back carries three tufts of three sizes and a red ember. Nothing is drawn on it.
  6  He wears no collar. What rides round the neck is the mohawk's TAIL, which hangs from the
     head onto the back of the vest, and the vest's shoulder edge under the jaw. The vest's top
     stays under the foot of the head so the jaw turns over it, and the tail bends (rule 7).
  7  CLOTH ACROSS TWO BONES BENDS. Two pieces do: each leg of the trunks (the torso's at the
     waist, the leg's at the hem, so the seat does not split open at a stride) and the tail of
     the mohawk (the head's at its root, the torso's at its tip).
  8  TWO ELBOW BONES appended after the seven. His elbow is where the bracer starts.

HOW A FACE FINDS ITS PAINT. A piece is tagged with a group ("head", "torso", "vest", "armL" ...)
and each of its faces goes to the island of the side its normal points at, at the place it sits
in model space. Loose blocks (hair, flame, gold) take flat tones by which way a face points.
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
import author_character_redesign_sean_textures as tex  # noqa: E402  the island layout
import author_character_redesign_sean_clips as clips  # noqa: E402  the prototype's own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/team-sean.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/sean")
OUT = os.path.join(FOLDER, "sean-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/sean/redesign-20261005/sean_redesign.blend")
NAME = "sean-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# ⚠️ TWO BONES THE CAST DOES NOT HAVE (rule 8): an elbow in each arm, a child of the arm bone,
# appended AFTER the seven so their indices and names do not move. A clip that does not key them
# leaves the arm straight, exactly as before.
# ⚠️ WHAT THIS COSTS IN THE GAME: `CharacterVisual.PalmCentre` finds the hand from `arm-right`
# alone. With the elbow bent the hand is no longer where that code parks a carried tsinelas.
# HIS elbow sits where the bracer begins (the original's `bracer-inner-gold` at 0.242 to 0.260):
# the upper arm is short and the forearm, which is all bracer and fist, is long.
ELBOW_X = 0.248
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
# ⚠️ THE HEAD IS BUILT AND PAINTED AT THE ORIGINAL'S SIZE, THEN BROUGHT IN TO 0.84. Owner,
# 2026-10-05, after comparing the lineup in the game: *"smaller heads are better"*, then *"yes
# rebuild the heads at 84 for all seven"*. Every vertex that rides the `head` bone (the block,
# the ears, the slits, the mohawk and its flame) is scaled about the head JOINT after the UVs
# are resolved (`build`), so the paint keeps its place and nothing inside the head changes
# proportion: the face is untouched. The nape tail, which is part head and part torso, is scaled
# by the head's share of each vertex.
HEAD_SCALE = 0.84
HEAD_JOINT = Vector((0.0, 0.0024, 0.473))   # the `head` bone of team-sean.glb, in Blender space
HEIGHT = HEAD_JOINT.z + (0.848 - HEAD_JOINT.z) * HEAD_SCALE   # the flame's tip after the scale (0.788)
SHARP_ANGLE = 34.0       # degrees: a chamfer is a hard edge (the crown's cut is 41), a soft corner or a taper is one form
TRIANGLE_BUDGET = 6000   # rule 10; the original is 10,754

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
        flat_ring = [(up, -vn), (up, vp), (-un, vp), (-un, -vn)]
    else:
        flat_ring = [(up, -vn + c), (up, vp - c), (up - c, vp), (-un + c, vp),
                     (-un, vp - c), (-un, -vn + c), (-un + c, -vn), (up - c, -vn)]
    return [Vector(centre) + u * a + v * b for a, b in flat_ring]


def proj(group):
    return ("proj", group)


def flat(name):
    return ("flat", name)


def proj_except(group, axis, name):
    """The group's islands, but a face turned along `axis` (0 x, 1 y, 2 z) takes one flat tone.

    A block's END faces look along its own length, where the group has drawn something else:
    a bracer's end would land on the knuckles' island, a belt's ledge on the shoulders'.
    """
    return ("except", group, axis, name)


def proj_facing(group, direction, name):
    """The group's islands for the faces turned toward `direction`, one flat tone for the rest."""
    return ("facing", group, direction, name)


def proj_square(group, flat_name, under_name, limit=0.93):
    """The group's islands ONLY for faces that squarely face one of its views; every chamfer and
    rounded-over face takes one flat tone (`under_name` if it is turned down).

    ⚠️ WHY. An island is a flat orthographic drawing. A face that meets its view at a glancing
    angle stretches that drawing along itself: on the first builds the knuckle lines, the skin
    shading and the bracer's stripes were smeared into long streaks down every chamfer of the
    arm. Owner, 2026-10-05, circling the fist and bracer in the game: *"strange texturing
    artifact"*. A drawing belongs on the face it was drawn for and nowhere else.
    """
    return ("square", group, flat_name, under_name, limit)


def proj_one(group, direction, name, limit=0.95):
    """The group's island for the ONE face turned squarely toward `direction`; one flat tone for
    every other face of the piece, its chamfers and ends included."""
    return ("facing", group, direction, name, limit)


def tones(top, side, under):
    """No drawing at all: one flat tone for faces turned up, one for the sides, one for under."""
    return ("tones", top, side, under)


GOLD = tones("gold_lit", "gold_tone", "gold_dark")


class Part:
    """One of the two meshes the rig carries (body-mesh, head-mesh)."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.bone = self.bm.verts.layers.int.new("bone")
        self.jobs = []      # (face, mapping)
        self.pieces = []    # (name, triangles)
        self.blend = {}     # vertex -> [(bone, weight)], for the few pieces that BEND

    def loft(self, name, bone, rings, mapping, caps=(True, True), tip=None):
        """Skin `rings` in order. `mapping` is one spec or f(segment, column) -> spec.

        `caps` closes the first and last ring with a fan; `tip` draws the last ring to a point.
        """
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
                f = bm.faces.new((rows[i][j], rows[i][k], rows[i + 1][k], rows[i + 1][j]))
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

        # Whichever way the rings were wound, the shell faces out.
        bmesh.ops.recalc_face_normals(bm, faces=faces)
        self.pieces.append((name, sum(len(f.verts) - 2 for f in faces)))
        return faces

    def block(self, name, bone, u, v, stations, mapping, chamfer, move=None, caps=(True, True)):
        """A chamfered block through `stations` = [(centre, hu, hv)], its two ends chamfered too.

        Two stations of one size is a box. Different sizes taper it; moving the second centre
        off the axis leans it. `move` is applied to every point (the feet's turn-out).
        """
        rings = []
        first, last = stations[0], stations[-1]
        along = (Vector(last[0]) - Vector(first[0]))
        length = along.length
        along = along.normalized()
        c = min(chamfer, 0.4 * length)

        def shrink(h):
            a, b = _pair(h)
            return (max(a - c, 0.001), max(b - c, 0.001))

        if c > 0.0:
            rings.append(rect_ring(first[0], u, v, shrink(first[1]), shrink(first[2]), chamfer * 0.4))
            rings.append(rect_ring(Vector(first[0]) + along * c, u, v, first[1], first[2], chamfer))
            for centre, hu, hv in stations[1:-1]:
                rings.append(rect_ring(centre, u, v, hu, hv, chamfer))
            rings.append(rect_ring(Vector(last[0]) - along * c, u, v, last[1], last[2], chamfer))
            rings.append(rect_ring(last[0], u, v, shrink(last[1]), shrink(last[2]), chamfer * 0.4))
        else:
            for centre, hu, hv in stations:
                rings.append(rect_ring(centre, u, v, hu, hv, 0.0))
        if move is not None:
            rings = [[move(p) for p in r] for r in rings]
        return self.loft(name, bone, rings, mapping, caps=caps)

    def bend(self, faces, weights):
        """Make a piece soft: `weights(position)` gives [(bone, weight)] for each of its vertices.

        ⚠️ The cast is rigid, one bone a vertex (Voxel_Person_Guide.md), and so is this model
        except where cloth or hair lies ACROSS two bones (rule 7). Owner, 2026-10-05, on Dante:
        *"make it so the clothes will bend/distort to follow his body"*.
        """
        for f in faces:
            for v in f.verts:
                self.blend[v] = weights(v.co)

    def wedge(self, name, bone, base, tip, base_half, tip_half, mapping, chamfer, across=X):
        """A block standing on `base` and narrowing to `tip`: a hair tuft, a flame's tongue."""
        w = (Vector(tip) - Vector(base)).normalized()
        u = (across - w * across.dot(w)).normalized()
        v = w.cross(u).normalized()
        return self.block(name, bone, u, v, [(base, base_half[0], base_half[1]), (tip, tip_half[0], tip_half[1])],
                          mapping, chamfer)

    def resolve_uvs(self):
        self.bm.normal_update()
        for face, spec in self.jobs:
            kind = spec[0]
            if kind == "except":
                spec = flat(spec[3]) if abs(face.normal[spec[2]]) > 0.6 else proj(spec[1])
            elif kind == "facing":
                limit = spec[4] if len(spec) > 4 else 0.6
                spec = proj(spec[1]) if face.normal.dot(Vector(spec[2])) > limit else flat(spec[3])
            elif kind == "square":
                best = max(face.normal.dot(Vector(tex.VIEW_DIR[view])) for view in tex.GROUPS[spec[1]])
                if best >= spec[4]:
                    spec = proj(spec[1])
                else:
                    spec = flat(spec[3] if face.normal.z < -0.3 else spec[2])
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
    """0 at `a`, 1 at `b`, clamped and eased."""
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3.0 - 2.0 * t)


# ---------------------------------------------------------------------------
# THE HEAD. The original's box (0.340 wide, 0.320 deep, 0.473 to 0.791), `shaped` (rule 1): a
# block whose corners are softened and whose sides are worked, and the box has to win, so every
# ring is a superellipse of exponent 3.6 or more (2 is a circle).
#   rows are (z, half width, depth to the front, depth to the back, exponent)
# HIS OWN CUT. The chin ring keeps 0.150 of half width where Dante's keeps 0.140 and the cheek
# ring at 0.560 is the widest of the head (0.181).
# ⚠️ NO BROW LEDGE AND NO NOSE (rule 12). The first builds stepped the forehead 12 mm forward
# over his eyes and stood a nose wedge between them; with the painted sockets that made a
# man's face on a toy. Owner, 2026-10-05: the faces *"look too realistic and look too human,
# like it lost its charm"*. The front of the head is one plane now, from the cheek to the crown.
# ⚠️ THE JAW IS SQUARE AND THE FRONT IS ONE PLANE DOWN TO THE CHIN. The first cut tapered the jaw
# over 50 mm (half width 0.110 at its foot); the rings that turned under caught the toon shadow
# band as hard dark facets along the chin. Owner, 2026-10-05, on another hero's same jaw:
# "random dark spots". Now only the last 10 mm turns in, and that sits on the vest's shoulders.
# ---------------------------------------------------------------------------
# ⚠️ THE SILHOUETTE IS THE ORIGINAL'S, MEASURED (team-sean.glb, head-mesh, slot 0): 0.160 of half
# width from 0.523 to 0.744, then a BIG straight cut to 0.120 at the crown (40 mm in, 47 mm down,
# and the same on the depth). Through v09 this head was 0.172 to 0.181 wide with soft uncut top
# corners, a tall plain slab with a long blank forehead. Owner, 2026-10-05: *"sean's face looks
# too weird"*. From the front it is the original's octagon now; only the exponent softens it.
# ⚠️ THE FRONT IS ONE FLAT PLANE: the depth to the front is 0.159 on EVERY row from the foot to
# where the crown's cut begins, and the exponent is one value too. Owner, 2026-10-05, on Dante in
# the real game: *"the eyebrow dent is making this weird shading artifact where theres a straight
# line on his head"*. `TumbangPreso/Toon` has two light bands, so any ring where the front steps
# in or out flips the band and draws a hard line across the face; Blender's imitation hid it.
# The only change of plane is the crown's cut, ONE crease at 0.744, 82 mm above the top of his
# eyes (0.662). The sides still narrow a little toward the jaw; the front does not.
HEAD_FRONT = 0.159
HEAD_ROWS = [
    (0.471, 0.150, HEAD_FRONT, 0.154, 5.0),
    (0.523, 0.161, HEAD_FRONT, 0.161, 5.0),
    (0.560, 0.163, HEAD_FRONT, 0.162, 5.0),
    (0.640, 0.161, HEAD_FRONT, 0.162, 5.0),
    (0.744, 0.160, HEAD_FRONT, 0.162, 5.0),
    (0.791, 0.120, 0.118, 0.122, 5.0),
]
HEAD_CENTRE = Vector((0.0, 0.0, 0.63))
HEAD_N = 24


def super_ring(z, rx, rf, rb, e, n=HEAD_N):
    out = []
    for j in range(n):
        t = 2.0 * math.pi * j / n
        c, s = math.cos(t), math.sin(t)
        a = math.copysign(abs(c) ** (2.0 / e), c)
        b = math.copysign(abs(s) ** (2.0 / e), s)
        out.append(Vector((a * rx, 0.001 + b * (rb if b >= 0 else rf), z)))
    return out


#   the three gold razor slits over each ear: (y centre, z centre, half length, half height).
#   The original's climb from front to back (build_iggy_voxel.py `razor-slit-fwd/mid/aft`); here
#   lifted 40 mm so the lowest clears the top of the ear block instead of sitting against it.
SLITS = [(-0.045, 0.670, 0.023, 0.0105), (0.007, 0.696, 0.023, 0.0105), (0.059, 0.722, 0.023, 0.0105)]


def build_head(part):
    skin = proj("head")
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], skin)
    for s in (1, -1):
        # ears: big blocks, as the original's are (out to 0.227), narrowing a little outward
        # (measured: 0.543 to 0.638 high, out to 0.227, 100 mm deep and set BEHIND the middle)
        part.block("ear", "head", Y, Z, [((s * 0.152, 0.044, 0.5905), 0.048, 0.047), ((s * 0.209, 0.044, 0.5905), 0.046, 0.047),
                                         ((s * 0.227, 0.044, 0.5905), 0.040, 0.033)], skin, 0.009)
        # the razor slits: gold bars standing 7 mm off the buzzed patch
        for yc, zc, hy, hz in SLITS:
            part.block("slit", "head", Y, Z, [((s * 0.148, yc, zc), hy, hz), ((s * 0.168, yc, zc), hy, hz)], GOLD, 0.0)


# ---------------------------------------------------------------------------
# THE MOHAWK (rule 3: blocks and flat tones, no drawing).
# A black ridge 90 to 100 mm wide: over the crown, down the back of the skull, and a tail that
# hangs off the nape onto the vest. Then what stands on it, every piece set by hand:
#   (name, base, tip, base half size, tip half size)
# ---------------------------------------------------------------------------
TUFTS = [
    # down the back: three black tufts kicked out behind him, no two the same size
    ("tuft-crown", (0.000, 0.118, 0.798), (0.000, 0.200, 0.830), (0.040, 0.028), (0.017, 0.010)),
    ("tuft-high",  (0.003, 0.178, 0.716), (-0.004, 0.240, 0.742), (0.037, 0.030), (0.015, 0.010)),
    ("tuft-low",   (-0.003, 0.176, 0.626), (0.004, 0.224, 0.636), (0.033, 0.026), (0.013, 0.009)),
]
#   the ember at the back (the original's `hawk-rear-red`), between the top two tufts
EMBER = ("ember", (0.000, 0.186, 0.768), (0.000, 0.228, 0.792), (0.021, 0.018), (0.008, 0.006))
#   the flame: three tongues swept back off a red bed, front tallest. Each is an orange wedge
#   with a yellow wedge standing in it. The front one's tip is the top of the whole character.
TONGUES = [
    ("front", (0.000, -0.104, 0.808), (0.000, -0.074, 0.834), (0.035, 0.030), (0.021, 0.013),
              (0.000, -0.094, 0.820), (0.000, -0.056, 0.8425), (0.022, 0.019), (0.008, 0.006)),
    ("mid",   (0.002, -0.036, 0.810), (0.002, -0.004, 0.829), (0.032, 0.029), (0.019, 0.012),
              (0.002, -0.026, 0.819), (0.002, 0.012, 0.835), (0.019, 0.017), (0.007, 0.006)),
    ("rear",  (-0.002, 0.026, 0.806), (-0.002, 0.056, 0.820), (0.028, 0.026), (0.016, 0.011),
              (-0.002, 0.034, 0.813), (-0.002, 0.070, 0.825), (0.015, 0.014), (0.006, 0.005)),
]
TAIL_ROOT, TAIL_TIP = 0.530, 0.452     # heights between which the tail passes from head to torso
TAIL_FOLLOW = 0.8


def build_hair(part):
    hair = tones("hair_top", "hair", "hair_under")
    deep = tones("hair", "hair_under", "hair_under")
    # the ridge over the crown, from a blunt point over the forehead to the back of the skull
    # ⚠️ ITS FRONT IS A TALL BLACK BLOCK, as the original's `hawk-base-top-front` is (110 mm wide,
    # 0.745 to 0.818, standing on the crown's cut corner). A thin ridge there read from the front
    # as a small blob with the flame sat on bare skin.
    part.block("hawk-top", "head", X, Z, [((0, -0.143, 0.782), 0.053, 0.035), ((0, -0.092, 0.791), 0.053, 0.026),
                                          ((0, 0.090, 0.790), 0.048, 0.019), ((0, 0.172, 0.774), 0.044, 0.020)],
               hair, 0.012)
    # down the back: one step deeper, so the tufts laid on it read as hair lying on hair
    part.block("hawk-back", "head", X, Y, [((0, 0.166, 0.790), 0.044, 0.020), ((0, 0.171, 0.660), 0.040, 0.019),
                                           ((0, 0.168, 0.535), 0.034, 0.017)], deep, 0.011)
    # THE TAIL. It hangs from the nape to the back of the vest. Rigid to the head, it swung
    # through the vest's shoulder when the head turned (it sits 170 mm behind the neck); so its
    # tip is the torso's and only its root is the head's.
    tail = part.wedge("hawk-tail", "head", (0, 0.168, 0.548), (0, 0.138, 0.446), (0.032, 0.017), (0.017, 0.009), deep, 0.007)
    part.bend(tail, lambda co: [("head", 1.0 - TAIL_FOLLOW * _ramp(TAIL_ROOT, TAIL_TIP, co.z)),
                                ("torso", TAIL_FOLLOW * _ramp(TAIL_ROOT, TAIL_TIP, co.z))])
    for name, base, tip, base_half, tip_half in TUFTS:
        part.wedge("hawk-" + name, "head", base, tip, base_half, tip_half, hair, 0.009)
    name, base, tip, base_half, tip_half = EMBER
    part.wedge("hawk-" + name, "head", base, tip, base_half, tip_half, tones("flame_r_top", "flame_r", "flame_r_under"), 0.005)
    # the flame's bed, red, on the front two thirds of the ridge
    part.block("flame-bed", "head", X, Z, [((0, -0.140, 0.816), 0.046, 0.008), ((0, -0.040, 0.814), 0.046, 0.010),
                                           ((0, 0.062, 0.808), 0.036, 0.010)],
               tones("flame_r_top", "flame_r", "flame_r_under"), 0.006)
    orange = tones("flame_o_top", "flame_o", "flame_o_under")
    yellow = tones("flame_y_top", "flame_y", "flame_y_under")
    for name, ob, ot, obh, oth, yb, yt, ybh, yth in TONGUES:
        part.wedge("flame-" + name, "head", ob, ot, obh, oth, orange, 0.007)
        part.wedge("flame-" + name + "-core", "head", yb, yt, ybh, yth, yellow, 0.004)


# ---------------------------------------------------------------------------
# THE BODY. A V: the waist narrower than the chest, as the original's lats make it.
#   (height, half width, half depth)
# The top is at 0.470, under the foot of the head (0.469 to 0.473), and NOTHING on the torso bone
# stands higher than 0.474 within reach of the jaw: his original's traps and shoulder slabs rise
# to 0.495 INSIDE the head box, where a turning jaw cuts through them.
# ---------------------------------------------------------------------------
TORSO_ROWS = [(0.240, 0.150, 0.098), (0.400, 0.172, 0.104), (0.469, 0.166, 0.100)]
VEST_TOP = 0.466      # its piping's top (0.469) is under the square foot of the head (0.471)
VEST_OFF = (0.010, 0.016, 0.010)     # how far the vest stands off the body: sides, front, back
VEST_WALL = 0.008
VEST_OPEN_X = 0.062                  # half the width left open down the chest
# ⚠️ THE VEST'S PATH ROUND THE BODY IS NEARLY A RECTANGLE (a superellipse of exponent 2 / 0.22).
# The first build used Dante's skirt exponent, 0.4, whose corners are round: the torso block's
# chamfered corners came through the cloth at all four, as tan triangles beside the back panel.
VEST_SQUARE = 0.22


def torso_half(z):
    rows = TORSO_ROWS
    z = max(rows[0][0], min(rows[-1][0], z))
    for (z0, w0, d0), (z1, w1, d1) in zip(rows, rows[1:]):
        if z <= z1:
            f = (z - z0) / (z1 - z0)
            return (w0 + (w1 - w0) * f, d0 + (d1 - d0) * f)


def chest_y(z):
    return -torso_half(z)[1]


def build_torso(part):
    body = proj("torso")
    part.block("torso", "torso", X, Y, [((0, 0, z), w, d) for z, w, d in TORSO_ROWS], body, 0.014)
    # the pectorals: two slabs 14 mm proud. Muscle is FORM here, not shading
    # (CHARACTER_MODEL_METHOD.md section 2), and his original's stand 42 mm off the chest.
    for s in (1, -1):
        y = chest_y(0.412)
        part.block("pec", "torso", X, Z, [((s * 0.054, y + 0.006, 0.412), 0.045, 0.041), ((s * 0.053, y - 0.014, 0.414), 0.040, 0.035)],
                   body, 0.011)

    # the seat of the trunks, under the belt
    part.block("seat", "torso", X, Y, [((0, 0, 0.198), 0.160, 0.100), ((0, 0, 0.252), 0.156, 0.100)],
               proj_except("torso", 2, "red_tone"), 0.010)
    # the belt: a strap 7 mm proud between two gold rims 10 mm proud
    lo, hi = tex.BELT_Z
    (wl, dl), (wh, dh) = torso_half(lo), torso_half(hi)
    part.block("belt", "torso", X, Y, [((0, 0, lo - 0.004), wl + 0.007, dl + 0.007), ((0, 0, hi + 0.002), wh + 0.007, dh + 0.007)],
               proj_except("torso", 2, "belt_tone"), 0.004)
    for z in (lo - 0.001, hi):
        w, d = torso_half(z)
        part.block("belt-rim", "torso", X, Y, [((0, 0, z - 0.0045), w + 0.010, d + 0.010), ((0, 0, z + 0.0045), w + 0.010, d + 0.010)],
                   GOLD, 0.003)
    # the buckle: a gold plate standing 17 mm off the belt, its leather core painted on its face
    y0 = chest_y(0.267) - 0.006
    part.block("buckle", "torso", X, Z, [((0, y0, 0.267), 0.052, 0.035), ((0, y0 - 0.017, 0.267), 0.050, 0.033)],
               proj_facing("torso", (0, -1, 0), "gold_dark"), 0.007)

    # THE VEST. One shell round the body, open down the front, its back one piece (rule 7), its
    # hem and the edge over the shoulders piped in gold AS GEOMETRY (CAST_CLOTHING_STYLE.md rule
    # 3). It lies wholly on the torso bone, so it is rigid: the arms come out through its sides.
    cloth = proj("vest")
    lining = flat("lining")
    hem_lo, hem_hi = tex.HEM
    sections = []
    gap = math.degrees(math.asin((VEST_OPEN_X / (torso_half(0.40)[0] + VEST_OFF[0])) ** (1.0 / VEST_SQUARE)))
    angles = (gap, 2.5, 7, 16, 30, 45, 60, 76, 90, 104, 120, 135, 150, 166, 180, 194, 210, 225, 240, 256, 270, 284, 300, 315,
              330, 344, 353, 357.5, 360.0 - gap)
    for theta in angles:
        a = math.radians(theta)
        px = -math.copysign(abs(math.sin(a)) ** VEST_SQUARE, math.sin(a))     # his right first, then behind
        py = -math.copysign(abs(math.cos(a)) ** VEST_SQUARE, math.cos(a))     # -1 in front, +1 behind

        def at(z, inset):
            w, d = torso_half(z)
            depth = d + (VEST_OFF[2] if py >= 0 else VEST_OFF[1]) - inset
            return Vector((px * (w + VEST_OFF[0] - inset), py * depth, z))

        mid = 0.5 * (VEST_TOP + hem_hi)
        sections.append([at(VEST_TOP - 0.014, 0.0), at(mid, 0.0), at(hem_hi, 0.0), at(hem_hi - 0.0005, -0.004),
                         at(hem_lo, -0.004), at(hem_lo + 0.003, VEST_WALL), at(mid, VEST_WALL), at(VEST_TOP, VEST_WALL),
                         at(VEST_TOP + 0.003, -0.004), at(VEST_TOP - 0.0135, -0.004)])
    part.loft("vest", "torso", sections,
              lambda i, j: (cloth, cloth, GOLD, GOLD, GOLD, lining, lining, GOLD, GOLD, GOLD)[j])

    for s in (1, -1):
        # the lapels' gold edge down each side of the opening, bowed out round the pectoral
        part.block("lapel", "torso", X, Y,
                   [((s * 0.066, chest_y(0.300) - VEST_OFF[1] - 0.002, 0.300), 0.011, 0.006),
                    ((s * 0.076, chest_y(0.388) - VEST_OFF[1] - 0.002, 0.388), 0.011, 0.006),
                    ((s * 0.068, chest_y(0.466) - VEST_OFF[1] - 0.002, 0.466), 0.011, 0.006)], GOLD, 0.0)
        # the armhole's gold trim: a plate on the side of the vest that the arm comes out through
        x = torso_half(0.408)[0] + VEST_OFF[0] - 0.004
        # ⚠️ ONLY A RIM. As a plate the size of the original's (216 by 110 mm) it stood beside the
        # bracer's gold rims in the idle and the three read as one slab of gold. It is 10 mm
        # bigger than the deltoid all round, so what shows is a ring.
        part.block("armhole", "torso", Y, Z, [((s * x, ARM_Y, ARM_Z + 0.002), 0.100, 0.078), ((s * (x + 0.013), ARM_Y, ARM_Z + 0.002), 0.097, 0.075)],
                   GOLD, 0.016)
    # his sun, on the back: a gold plate 8 mm proud, its orange core and yellow dot painted on it
    y = torso_half(0.390)[1] + VEST_OFF[2] - 0.002
    part.block("sun", "torso", X, Z, [((0, y, 0.390), 0.042, 0.042), ((0, y + 0.010, 0.390), 0.040, 0.040)],
               proj_facing("vest", (0, 1, 0), "gold_dark"), 0.005)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests. `s` is +1 for his left, -1 for his right.
# The bone is at x 0.180; the deltoid cap starts inside the vest's armhole.
# ---------------------------------------------------------------------------
ARM_Y, ARM_Z = 0.0172, 0.430


def arm_block(part, name, bone, s, x0, x1, half0, half1, mapping, chamfer, dz=0.0):
    return part.block(name, bone, Y, Z, [((s * x0, ARM_Y, ARM_Z + dz), half0[0], half0[1]), ((s * x1, ARM_Y, ARM_Z + dz), half1[0], half1[1])],
                      mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    # (c) bare skin: muscle shading only on faces that squarely face a view; chamfers are flat
    skin = proj_square(group, "skin", "skin_tone")
    # the deltoid: a cap cut hard at its corners, its top no higher than 0.501 (the cheek above
    # it is 0.157 from the centre at that height, the cap's full height starts at 0.172)
    arm_block(part, "deltoid", bone, s, 0.150, 0.224, (0.090, 0.068), (0.084, 0.064), skin, 0.022, dz=0.003)
    # two rigid blocks that overlap at the elbow, as a toy's joint does
    arm_block(part, "arm", bone, s, 0.200, ELBOW_X + 0.014, (0.080, 0.062), (0.076, 0.058), skin, 0.012, dz=-0.002)
    # the bicep: a slab on the front of the upper arm
    y = ARM_Y - 0.074
    part.block("bicep", bone, X, Z, [((s * 0.223, y, 0.428), 0.026, 0.040), ((s * 0.223, y - 0.016, 0.430), 0.020, 0.032)],
               proj_one(group, (0, -1, 0), "skin"), 0.008)
    arm_block(part, "forearm", fore, s, ELBOW_X - 0.012, 0.342, (0.062, 0.056), (0.058, 0.054), tones("skin", "skin", "skin_tone"), 0.010)
    # THE BRACER: red cloth between two gold rims, a gold plate on its front, a gold crest on top
    lo, hi = tex.BRACER
    # ⚠️ EVERY BAND OF THE BRACER IS ITS OWN PIECE IN ITS OWN FLAT TONES (red cloth, the orange
    # strap, two gold rims), so a band is the same width at the same place on every facet round
    # the arm. They were drawn on the arm's projected islands, where on the chamfers between front,
    # top and back they stretched, shifted and did not meet: a jumble of offset slabs.
    arm_block(part, "bracer", fore, s, lo, hi, (0.076, 0.068), (0.073, 0.065), tones("red_lit", "red", "red_tone"), 0.008)
    s0, s1 = tex.STRAP
    arm_block(part, "bracer-strap", fore, s, s0, s1, (0.0770, 0.0690), (0.0765, 0.0685), tones("tongue", "tongue", "tongue_dark"), 0.008)
    arm_block(part, "bracer-rim", fore, s, lo, lo + 0.018, (0.080, 0.071), (0.080, 0.071), GOLD, 0.004)
    arm_block(part, "bracer-rim", fore, s, hi - 0.019, hi, (0.079, 0.071), (0.079, 0.071), GOLD, 0.004)
    px = 0.5 * (tex.PLATE_X[0] + tex.PLATE_X[1])
    pz = 0.5 * (tex.PLATE_Z[0] + tex.PLATE_Z[1])
    part.block("bracer-plate", fore, X, Z,
               [((s * px, ARM_Y - 0.072, pz), 0.022, 0.040), ((s * px, ARM_Y - 0.083, pz), 0.020, 0.037)],
               proj_one(group, (0, -1, 0), "gold_dark"), 0.005)
    part.block("bracer-crest", fore, X, Y, [((s * px, ARM_Y, ARM_Z + 0.064), 0.021, 0.044), ((s * px, ARM_Y, ARM_Z + 0.073), 0.018, 0.040)],
               GOLD, 0.004)
    # the fist: a plain block, its top 0.058 above the bone where a carried tsinelas sits. No
    # thumb block (on Dante its ink edge read as a ring); fingers and thumb are drawn.
    # ⚠️ ONE FLAT SKIN TONE ON EVERY FACE BUT THE FRONT, where the folded fingers are drawn. No
    # chamfer, end or top face samples a drawing (see `proj_square`).
    arm_block(part, "fist", fore, s, tex.FIST, 0.405, (0.060, 0.058), (0.058, 0.057), proj_one(group, (0, -1, 0), "skin"), 0.014)


# ---------------------------------------------------------------------------
# THE LEGS. Longer than the cast's (0.245), on the original's own hip. Feet turned out a little.
# ---------------------------------------------------------------------------
LEG_X = 0.097
TOE_OUT = 6.0
TOE_PIVOT = 0.050        # the feet turn about a point near the heel, so the two heels do not cross
TRUNKS_SOFT = (0.186, 0.244)    # heights between which a trunk leg passes from the leg to the torso


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    cloth = proj(group)
    band_lo, band_hi = tex.BAND
    # THE TRUNK LEG. ⚠️ IT BENDS (rule 7). Rigid to the leg, its top swung out of the seat at
    # every stride and left a step in the cloth at the hip; its waist is the torso's now.
    trunk = part.block("trunks", bone, X, Y, [((s * LEG_X, 0.0, band_hi - 0.006), 0.090, 0.109), ((s * (LEG_X - 0.005), 0.0, 0.214), 0.086, 0.106),
                                              ((s * (LEG_X - 0.008), 0.0, 0.248), 0.080, 0.102)],
                       proj_except(group, 2, "red_tone"), 0.010)
    part.bend(trunk, lambda co: [("torso", _ramp(TRUNKS_SOFT[0], TRUNKS_SOFT[1], co.z)),
                                 (bone, 1.0 - _ramp(TRUNKS_SOFT[0], TRUNKS_SOFT[1], co.z))])
    # the gold band at the thigh, 4 mm proud (the original's `thigh-trim`)
    part.block("thigh-band", bone, X, Y, [((s * LEG_X, 0.0, band_lo), 0.094, 0.113), ((s * LEG_X, 0.0, band_hi), 0.094, 0.113)],
               proj_except(group, 2, "gold_tone"), 0.006)
    # the bare shin between the band and the shoe
    part.block("shin", bone, X, Y, [((s * LEG_X, 0.004, 0.066), 0.078, 0.086), ((s * LEG_X, 0.0, band_lo + 0.006), 0.082, 0.092)],
               proj_except(group, 2, "skin_deep"), 0.010)

    turn = math.radians(TOE_OUT) * s
    cs, sn = math.cos(turn), math.sin(turn)

    def turned(p):
        x, y = p.x - s * LEG_X, p.y - TOE_PIVOT
        return Vector((s * LEG_X + x * cs + y * sn, TOE_PIVOT - x * sn + y * cs, p.z))

    # the high-top: low at the toe, rising to the ankle, on a wider sole
    def station(y, half, z0, z1, cut):
        return [turned(p) for p in rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)]

    upper = [(-0.158, 0.060, 0.022, 0.046, 0.005), (-0.146, 0.078, 0.020, 0.060, 0.012), (-0.096, 0.087, 0.020, 0.078, 0.014),
             (-0.066, 0.087, 0.020, 0.094, 0.014), (0.062, 0.085, 0.020, 0.094, 0.014), (0.100, 0.076, 0.022, 0.088, 0.007)]
    part.loft("shoe", bone, [station(*row) for row in upper], cloth)
    part.block("sole", bone, X, Z, [((s * LEG_X, -0.163, 0.013), 0.064, 0.013), ((s * LEG_X, -0.100, 0.013), 0.091, 0.013),
                                   ((s * LEG_X, 0.000, 0.013), 0.090, 0.013), ((s * LEG_X, 0.105, 0.013), 0.080, 0.013)],
               proj_except(group, 2, "sole"), 0.005, move=turned)   # the tread is grey: white, it flashed as a blank card at every stride
    # THE TONGUE AND THE STRAP, his two pieces of orange. ⚠️ KEPT SMALL: slot 15 `ff8800` sits on
    # the offence role hue, and the original's tongue is a 140 mm block over the whole front of
    # each shoe. Here it is a 70 mm riser standing off the shin and a 12 mm strap round the ankle.
    orange = tones("tongue", "tongue", "tongue_dark")
    faces = part.wedge("tongue", bone, (s * LEG_X, -0.118, 0.058), (s * LEG_X, -0.103, 0.103), (0.036, 0.011), (0.030, 0.008),
                       orange, 0.006)
    for v in {v for f in faces for v in f.verts}:
        v.co = turned(v.co)
    # the strap follows the shoe's own outline 3 mm proud. As a plain box it stood 14 mm off the
    # narrow heel, a shelf sticking out behind the ankle.
    band = [(-0.086, 0.0885, 0.064, 0.076, 0.003), (-0.060, 0.0900, 0.064, 0.076, 0.003), (0.062, 0.0880, 0.064, 0.076, 0.003),
            (0.1035, 0.0790, 0.064, 0.076, 0.003)]
    part.loft("strap", bone, [station(*row) for row in band], orange)


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
    """team-sean.glb with its meshes swapped: the seven bones, their bind matrices and every clip
    but the four rewritten ones are copied untouched; two elbow joints are appended."""
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

    skin = gltf["skins"][0]
    raw = struct.unpack("<%df" % (16 * len(BONES)), bpv.accessor_bytes(gltf, buffer, skin["inverseBindMatrices"]))
    matrices = [raw[k * 16:(k + 1) * 16] for k in range(len(BONES))]
    for anim in gltf["animations"]:
        if anim["name"] in clips.CLIPS:
            continue
        for sampler in anim["samplers"]:
            sampler["input"] = keep(sampler["input"])
            sampler["output"] = keep(sampler["output"])

    # the two elbows: a node under each arm, a joint and a bind matrix appended after the seven.
    # Every rest rotation in this rig is identity, so a bind matrix is the inverse translation.
    node_of = {n.get("name"): i for i, n in enumerate(gltf["nodes"])}
    for name, parent, side in EXTRA_BONES:
        world = (side * ELBOW_X, ARM_Z, -ARM_Y)
        pw = [-matrices[BONES.index(parent)][12 + a] for a in range(3)]
        gltf["nodes"].append({"name": name, "translation": [world[a] - pw[a] for a in range(3)]})
        gltf["nodes"][node_of[parent]].setdefault("children", []).append(len(gltf["nodes"]) - 1)
        skin["joints"].append(len(gltf["nodes"]) - 1)
        matrices.append((1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -world[0], -world[1], -world[2], 1))
    grown = add([tuple(float(x) for x in m) for m in matrices], "f", "MAT4", 5126)
    # every skin of the file gets the nine joints (the base carries one skin a mesh)
    for other in gltf["skins"]:
        other["joints"] = list(skin["joints"])
        other["inverseBindMatrices"] = grown

    # the prototype's own locomotion clips, in place of the ones copied across (the clips script)
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (sean)"}
    gltf["extras"] = {"prototype": "character-redesign-20261005", "replaces": "nothing; not loaded by the game"}
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
    print("wrote " + out)


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
    print("triangles %d  (budget %d, the original is 10754)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if abs(hi.z - HEIGHT) > 0.004:
        raise SystemExit("the top is at %.4f, the original's is %.3f" % (hi.z, HEIGHT))
    if tris > TRIANGLE_BUDGET:
        raise SystemExit("%d triangles is over the budget of %d" % (tris, TRIANGLE_BUDGET))
    # CharacterVisual.PalmCentre: the arm must run along x, and the hand's top must sit where a
    # carried tsinelas is parked (the original's fist top is 0.058 above the bone).
    body = objects[0]
    group = (body.vertex_groups["arm-right"].index, body.vertex_groups["forearm-right"].index)
    arm = [v.co for v in body.data.vertices if v.groups[0].group in group]
    size = [max(p[a] for p in arm) - min(p[a] for p in arm) for a in range(3)]
    if not (size[0] > size[1] and size[0] > size[2]):
        raise SystemExit("arm-right no longer runs along x: %s" % size)
    far = min(p.x for p in arm)
    hand = [p for p in arm if p.x < far + size[0] / 8.0]
    top = max(p.z for p in hand) - ARM_Z
    print("fist ends at %.4f (the original's 0.405); its top is %.4f above the arm bone (the original's 0.058)" % (-far, top))
    if not 0.050 <= top <= 0.064:
        raise SystemExit("the hand's top moved; a carried tsinelas would float or sink")
    # rule 6, the part of it a collarless hero can break: nothing on the torso bone may stand in
    # the head's way. Everything rigid to the torso within 0.16 of the neck stays under 0.4745.
    torso = body.vertex_groups["torso"].index
    worst = max((v.co.z for v in body.data.vertices
                 if len(v.groups) == 1 and v.groups[0].group == torso and math.hypot(v.co.x, v.co.y) < 0.16), default=0.0)
    print("highest torso point under the jaw %.4f (the head's foot is at 0.471, its half width now 0.126)" % worst)
    if worst > 0.4715:
        raise SystemExit("something on the torso stands up into the head")


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
        if part is head:
            # AFTER the paint is placed (it is projected from the full-size shape): the head comes in
            for v in part.bm.verts:
                share = dict(part.blend[v]).get("head", 0.0) if v in part.blend else 1.0
                v.co = HEAD_JOINT + (v.co - HEAD_JOINT) * (1.0 + (HEAD_SCALE - 1.0) * share)
        obj = part.to_object(armature)
        obj.data.materials.append(material)
        objects.append(obj)
        print(part.name, " ".join("%s:%d" % (n, t) for n, t in part.pieces))
    verify(objects)
    return objects


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

    image_path = os.path.join(FOLDER, tex.ATLAS_NAME)
    if not os.path.exists(image_path):
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_sean_textures.py")
    image = bpy.data.images.load(image_path)
    material = bpy.data.materials.new(NAME)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    node = nodes.new("ShaderNodeTexImage")
    node.image = image
    material.node_tree.links.new(node.outputs[0], nodes["Principled BSDF"].inputs["Base Color"])
    nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0

    objects = build(armature, material)
    if out is not None:
        write_glb(mesh_arrays(objects[0]), mesh_arrays(objects[1]), out)
        return
    write_glb(mesh_arrays(objects[0]), mesh_arrays(objects[1]), OUT)
    os.makedirs(os.path.dirname(BLEND), exist_ok=True)
    image.filepath = bpy.path.relpath(image_path, start=os.path.dirname(BLEND))
    bpy.context.preferences.filepaths.save_version = 0   # no .blend1 beside it
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)
    print("wrote " + BLEND)


if __name__ == "__main__":
    main()
