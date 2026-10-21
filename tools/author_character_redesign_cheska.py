"""Build the Cheska (displayed: Yasmin) redesign PROTOTYPE: the same block kid, shaped and painted.

    py -3 tools/author_character_redesign_cheska_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_cheska.py

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/cheska/cheska-redesign.glb
    ArtSource/cheska/redesign-20261005/cheska_redesign.blend
beside the atlas the .glb names.

WHY. docs/CHARACTER_REDESIGN_DANTE.md section 13: the owner asked for the rest of the cast to
follow Dante's rework, each hero in its own files. This is her copy of Dante's model script,
rewritten for her; it imports nothing of his.

⚠️ A PROTOTYPE. NOTHING IN THE GAME LOADS IT. The folder is outside Resources, no roster row
points here, and team-cheska.glb, build_person_voxel.py and person_cheska.asset are not touched.

WHO SHE IS, measured off team-cheska.glb before anything was designed (the measures are in the
tables below; none is Dante's): a head box 0.320 wide (his is 0.340), 0.320 deep, from 0.343 to
0.661; a torso 0.236 wide from 0.176 to 0.343; arms that END AT 0.280 (his reach 0.292); a
padded trapper hat that makes her 0.792 tall and 0.49 wide, wider than she is at the shoulders
by half; hair hanging in a blunt sheet to 0.148, below her hips.

THE RULES SHE INHERITS (section 13), and where each lands in this file:
  1  the head is the game's box head, `shaped`: `HEAD_ROWS`, scaled to HER box.
  2  nothing invented: every piece below names the piece of her original it stands for.
  3  no hair shine; a flat back gets BLOCKS: `BACK_SLABS`, three graded slabs over a deeper mass.
  4  paint stops at its own piece's edges: `ftones`, `proj_except`.
  6  what hangs round the neck follows the HEAD from the jaw up and the shoulders below: `hang`.
  7  cloth across two bones bends: the shorts' hem takes the legs, the long hair the torso.
  8  two elbows, appended after the seven.
  10 feet on zero, her height, her hand top, under 6,000 triangles: `verify`.

HOW A FACE FINDS ITS PAINT. A piece is tagged with a group ("head", "hat", "torso", "armL" ...)
and each of its faces goes to the island of the side its normal points at, at the place it sits
in model space. A small added piece takes flat tones instead, by which way each face points.
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
import author_character_redesign_cheska_textures as tex  # noqa: E402  the island layout
import author_character_redesign_cheska_clips as clips  # noqa: E402  the prototype's own clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/team-cheska.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/cheska")
OUT = os.path.join(FOLDER, "cheska-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/cheska/redesign-20261005/cheska_redesign.blend")
NAME = "cheska-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# TWO BONES THE CAST DOES NOT HAVE (rule 8): an elbow in each arm, a child of the arm bone,
# appended AFTER the seven so their indices and names do not move. Her elbow sits under the teal
# cuff of her short sleeve, which hides the joint.
# ⚠️ WHAT THIS COSTS IN THE GAME: `CharacterVisual.PalmCentre` finds the hand from `arm-right`
# alone. With the elbow bent the hand is not where that code parks a carried tsinelas.
ELBOW_X = 0.176
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
# ⚠️ A SMALLER HEAD. Owner, after comparing sizes in the game on the whole lineup: "smaller heads
# are better", then "yes rebuild the heads at 84 for all seven". Everything is built and painted
# at the size measured off her original, then, AFTER the paint is placed, every vertex that rides
# the head bone is drawn in to 0.84 about the head JOINT (see `build`). The paint keeps its place
# and nothing inside the head changes proportion, so her face is untouched.
HEAD_SCALE = 0.84
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))   # the `head` bone of team-cheska.glb, in Blender space
HEIGHT = HEAD_JOINT.z + (0.792 - HEAD_JOINT.z) * HEAD_SCALE   # the pompom's top after the scale
SHARP_ANGLE = 40.0
TRIANGLE_BUDGET = 6000   # the brief's ceiling; the original is 11,700

X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
AHEAD, BEHIND = (0, -1, 0), (0, 1, 0)


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


def flat(name):
    return ("flat", name)


def proj_except(group, axis, name):
    """The group's islands, but a face turned along `axis` (0 x, 1 y, 2 z) takes one flat tone.

    A block's END faces look along its own length, where the group has drawn something else:
    a sock's top would land on the shoe's laces, a sleeve's end on the fingertips.
    """
    return ("except", group, axis, name)


def tones(top, side, under):
    """No drawing at all: one flat tone for faces turned up, one for the sides, one for under."""
    return ("tones", top, side, under)


def ftones(group, direction, shades):
    """An ADDED piece: the face turned toward `direction` takes the group's drawing (the pocket's
    stitches, a lens), every other face one of three flat tones. So a piece's edges never show
    another piece's paint (rule 4)."""
    return ("ftones", group, direction, shades)


HAIR_T = ("hair_top", "hair", "hair_under")
HAIR_DEEP_T = ("hair", "hair_under", "hair_under")
WHITE_T = ("white", "white", "white_shade")
CYAN_T = ("cyan_lit", "cyan", "cyan_shade")
TEAL_T = ("teal", "teal", "teal_deep")
TRIM_T = ("trim", "trim", "trim_shade")
FROST_T = ("frost", "frost", "frost_shade")
SILVER_T = ("silver_lit", "silver", "silver_shade")
WOOD_T = ("wood", "wood", "wood_shade")


class Part:
    """One of the two meshes the rig carries (body-mesh, head-mesh)."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.bone = self.bm.verts.layers.int.new("bone")
        self.jobs = []      # (face, mapping)
        self.pieces = []    # (name, triangles)
        self.blend = {}     # vertex -> [(bone, weight)], for the pieces that BEND
        self.carried = {}   # vertex -> the head's share its whole piece is drawn in by (see `carry`)

    def loft(self, name, bone, rings, mapping, caps=(True, True)):
        """Skin `rings` in order. `mapping` is one spec or f(segment, column) -> spec."""
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
                f = bm.faces.new((row[j], row[(j + 1) % n], centre))
                self.jobs.append((f, spec(seg, j)))
                faces.append(f)

        if caps[0]:
            fan(rows[0], sum(rings[0], Vector()) / n, 0)
        if caps[1]:
            fan(rows[-1], sum(rings[-1], Vector()) / n, count - 2)

        # Whichever way the rings were wound, the shell faces out.
        bmesh.ops.recalc_face_normals(bm, faces=faces)
        self.pieces.append((name, sum(len(f.verts) - 2 for f in faces)))
        return faces

    def block(self, name, bone, u, v, stations, mapping, chamfer, move=None, caps=(True, True)):
        """A chamfered block through `stations` = [(centre, hu, hv)], its two ends chamfered too.

        Two stations of one size is a box. Different sizes taper it; moving a centre off the
        axis leans it. A chamfer of zero is a plain four sided box (16 triangles, for the
        small added pieces). `move` is applied to every point (the feet's turn).
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

        The cast is rigid, one bone a vertex (Voxel_Person_Guide.md), and so is this model
        except where cloth or hair lies ACROSS two bones (rule 7, the owner on Dante: "make it so
        the clothes will bend/distort to follow his body").
        """
        for f in faces:
            for v in f.verts:
                self.blend[v] = weights(v.co)
        return faces

    def carry(self, faces, at):
        """When the head is drawn in (HEAD_SCALE), move this piece as ONE unit, by the share of the
        head that the point `at` it hangs from has. Without this a cord's top came in with the
        flap and its bobble stayed out over the shoulder, so the cord hung on a slant."""
        mix = hang(Vector(at))
        share = sum(w for b, w in mix if b == "head") / sum(w for _, w in mix)
        for f in faces:
            for v in f.verts:
                self.carried[v] = share
        return faces

    def wedge(self, name, bone, base, tip, base_half, tip_half, mapping, chamfer, across=X):
        """A block standing on `base` and narrowing to `tip`: a hair clump, a lapel."""
        w = (Vector(tip) - Vector(base)).normalized()
        u = (Vector(across) - w * Vector(across).dot(w)).normalized()
        v = w.cross(u).normalized()
        return self.block(name, bone, u, v, [(base, base_half[0], base_half[1]), (tip, tip_half[0], tip_half[1])],
                          mapping, chamfer)

    def resolve_uvs(self):
        self.bm.normal_update()
        for face, spec in self.jobs:
            kind = spec[0]
            if kind == "except":
                spec = flat(spec[3]) if abs(face.normal[spec[2]]) > 0.6 else proj(spec[1])
            elif kind == "ftones":
                if face.normal.dot(Vector(spec[2])) > 0.6:
                    spec = proj(spec[1])
                else:
                    spec = tones(*spec[3])
            if spec[0] == "tones":
                spec = flat(spec[1] if face.normal.z > 0.45 else (spec[3] if face.normal.z < -0.45 else spec[2]))
            if spec[0] == "proj":
                group = spec[1]
                best, score = None, -1e9
                for view in tex.GROUPS[group]:
                    s = face.normal.dot(Vector(tex.VIEW_DIR[view])) * tex.VIEW_BIAS.get((group, view), 1.0)
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
    """0 at `a`, 1 at `b`, clamped."""
    return max(0.0, min(1.0, (x - a) / (b - a)))


# ---------------------------------------------------------------------------
# WHAT HANGS BELOW HER JAW. The hat's ear flaps and mantle, their fur, the cords, and her long
# hair all ride the HEAD bone in the original, and all of them reach below the jaw (0.343), over
# shoulders and a back that ride the TORSO. Turn the head 55 degrees and a rigid flap sweeps
# through the shoulder; tip it back and a rigid sheet of hair swings into her back.
# Rule 6 gives the answer for a collar: the head's from the jaw up, the shoulders' below. ONE
# weight is used for every such piece, a function of height only, so two of them at the same
# height always move together and never pass through each other.
# ---------------------------------------------------------------------------
HANG_FROM, HANG_TO, HANG = 0.396, 0.300, 0.88


def hang(co):
    t = HANG * _ramp(HANG_FROM, HANG_TO, co.z)
    return [("head", 1.0 - t), ("torso", t)]


# ---------------------------------------------------------------------------
# THE HEAD. The game's box head, `shaped` (rule 1): superellipse rings of exponent 3.4 to 4.2 on
# HER box, which is 0.320 wide where Dante's is 0.340, fullest at the cheeks, a flat face
# plane with NO brow ledge, and a jaw that rounds in gently (see the rows). NO EARS: her original's ear blocks are wholly inside the
# hat's flaps and never seen.
#     rows are (z, half width, depth to the front, depth to the back, exponent)
# ---------------------------------------------------------------------------
HEAD_N = 24
HEAD_ROWS = [
    # ⚠️ DANTE'S HEAD SHAPE, ON HER BOX. Two wrong turns came first. Her jaw tapered in sharply and
    # the rings that turned under caught the shadow band as hard dark facets ("random dark spots",
    # the owner on another hero). Squaring the jaw cured that and made her head a hard box; owner:
    # *"dante has a much more rounded face shape compared to what amihan and cheska have"*. So the
    # rows are his `shaped` rows scaled to her width (0.94): fullest at the cheeks (0.170, six per
    # cent over the temples), soft corners in plan (exponent 3.8 to 4.2), the lower corners rounded
    # IN toward the chin. What differs from his is only where the taper ends: hers eases in over
    # the last third and is still 0.132 wide at the neckline, and the tight closing ring is 13 mm
    # down inside the torso's top, so nothing visible turns under into shadow.
    (0.330, 0.098, 0.100, 0.100, 3.4),
    (0.343, 0.132, 0.140, 0.134, 3.8),
    (0.357, 0.148, 0.153, 0.147, 3.9),
    # ⚠️ THE FRONT OF THE FACE IS ONE FLAT PLANE: one depth, 0.163, from under the mouth (0.388) to
    # the hairline (0.604). It had a 3 mm brow ledge at 0.524 and a cheek 2 mm proud at 0.428. In
    # the game's two-band toon shader any ring where the front steps in or out flips the light
    # band and draws a hard straight line across the face (the owner saw it on Dante in Unity:
    # "a straight line on his head"); Blender's imitation hid it. Cheek fullness is WIDTH only.
    (0.388, 0.162, 0.163, 0.157, 4.0),
    (0.428, 0.170, 0.163, 0.162, 4.2),
    (0.470, 0.166, 0.163, 0.162, 4.2),
    (0.520, 0.161, 0.163, 0.162, 4.2),
    (0.604, 0.159, 0.163, 0.162, 4.2),
    (0.646, 0.153, 0.158, 0.158, 3.8),
    (0.661, 0.132, 0.136, 0.140, 3.4),
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
    # NO NOSE (rule 12: the owner found the redesigned faces "too realistic"; her original has none)


# ---------------------------------------------------------------------------
# THE HAIR. Hers is black, cut blunt: a stepped fringe under the hat's brim, a lock by each
# cheek whose tip is dipped in frost, and one long sheet down her back that ends in a frost hem.
# No drawing on any of it (rule 3): a lighter top plane, the flat tone, a deeper underside.
#   the fringe: (name, base, tip, base half size, tip half size, across). The original's five
#   steps (longest at the outside, 0.505; the centre shortest, 0.555), each now its own length.
#   The two outer slabs are turned round the head's soft corner, which a cube did not have.
# ---------------------------------------------------------------------------
FRINGE = [
    ("outer-right", (-0.148, -0.160, 0.616), (-0.154, -0.154, 0.498), (0.041, 0.014), (0.031, 0.011), (0.76, 0.65, 0.0)),
    ("mid-right",   (-0.077, -0.182, 0.620), (-0.079, -0.179, 0.537), (0.036, 0.015), (0.031, 0.011), (1, 0, 0)),
    ("centre",      (-0.001, -0.185, 0.622), (0.001, -0.182, 0.558),  (0.042, 0.015), (0.036, 0.011), (1, 0, 0)),
    ("mid-left",    (0.077, -0.182, 0.620),  (0.079, -0.179, 0.542),  (0.036, 0.015), (0.030, 0.011), (1, 0, 0)),
    ("outer-left",  (0.148, -0.160, 0.616),  (0.155, -0.154, 0.504),  (0.041, 0.014), (0.032, 0.011), (0.76, -0.65, 0.0)),
]
#   the sheet down her back: (name, x, half width at the top, at the foot, where it ends).
#   ⚠️ THE ORIGINAL'S BACK IS ONE FLAT BLACK SLAB with a band near its foot. Dante's was too, and
#   the owner circled it; rule 3 says a flat back gets BLOCKS. So: a deeper mass underneath and
#   three slabs laid over it, 14 mm proud, each cut blunt at its own length, the middle longest
#   (it reaches the original's 0.148). Each ends in the original's frost hem, as a dipped tip.
BACK_SLABS = [
    ("back-right", -0.108, 0.051, 0.044, 0.178),
    ("back-mid",   0.000,  0.050, 0.046, 0.150),
    ("back-left",  0.108,  0.051, 0.043, 0.166),
]
FROST_TIP = 0.020   # how much of each slab's foot is frost
LEAN = 0.022        # the sheet falls IN toward her back: hung plumb off the hat it stood off her like a board


def build_hair(part):
    hair = tones(*HAIR_T)
    deep = tones(*HAIR_DEEP_T)
    # under the brim, across the forehead: what the fringe hangs from (the original's brow base)
    part.block("hair-brow", "head", X, Y, [((0, -0.167, 0.588), 0.168, 0.017), ((0, -0.166, 0.640), 0.170, 0.019)], hair, 0.008)
    for name, base, tip, base_half, tip_half, across in FRINGE:
        part.wedge("hair-fringe-" + name, "head", base, tip, base_half, tip_half, hair, 0.006, across=across)
    for s in (1, -1):
        # at the temple, between the fringe and the flap (the original's side caps)
        part.block("hair-temple", "head", X, Y, [((s * 0.169, -0.112, 0.640), 0.011, 0.026), ((s * 0.169, -0.110, 0.492), 0.010, 0.022)],
                   deep, 0.006)
        # the lock by the cheek, in front of the flap's fur, its tip dipped in frost
        # (the original's hair-side-lock and hair-side-frost-lock)
        part.wedge("hair-cheek-lock", "head", (s * 0.176, -0.136, 0.500), (s * 0.177, -0.132, 0.446), (0.011, 0.016), (0.010, 0.013),
                   hair, 0.005)
        part.block("hair-cheek-frost", "head", X, Y, [((s * 0.177, -0.132, 0.458), 0.0115, 0.0145), ((s * 0.177, -0.131, 0.432), 0.0105, 0.0125)],
                   tones(*FROST_T), 0.004)

    # THE SHEET DOWN HER BACK. It comes out from under the mantle's fur (0.359) and BENDS (`hang`):
    # the head's where it leaves the hat, the torso's from the shoulders down, so it lies on her
    # back when the head turns or tips instead of swinging through it. Rings every 40 to 70 mm so
    # the twist is shared along its length.
    def column(name, x, top_w, foot_w, end, y, depth, mapping, frost):
        zs = [0.426, 0.386, 0.346, 0.306, 0.240] + ([end + FROST_TIP] if frost else []) + [end]
        stations = []
        for z in zs:
            f = _ramp(0.426, end, z)
            stations.append(((x * (1.0 - 0.06 * f), y - LEAN * f, z), top_w + (foot_w - top_w) * f, depth * (1.0 - 0.18 * f)))
        n = len(stations)
        spec = mapping
        if frost:
            # the last interior station is where the frost begins: rings n-1 to n+1
            spec = lambda i, j: (tones(*FROST_T) if i >= n - 1 else mapping)
        return part.bend(part.block(name, "head", X, Y, stations, spec, 0.008), hang)

    column("hair-back-mass", 0.0, 0.164, 0.150, 0.186, 0.172, 0.032, deep, False)
    for name, x, top_w, foot_w, end in BACK_SLABS:
        column("hair-" + name, x, top_w, foot_w, end, 0.212, 0.014, hair, True)
    # the clasp at her nape and its two ribbon tails (the original's star clasp and ribbon tails),
    # each tail its own length and lean, each ending in a frost tip
    part.bend(part.block("hair-clasp", "head", X, Z, [((0.0, 0.224, 0.336), 0.026, 0.017), ((0.0, 0.236, 0.336), 0.021, 0.013)],
                         tones(*FROST_T), 0.005), hang)
    for name, top, foot, frost in (("ribbon-left", (0.020, 0.2305, 0.330), (0.033, 0.2325, 0.236), 0.258),
                                   ("ribbon-right", (-0.019, 0.2305, 0.330), (-0.036, 0.2325, 0.216), 0.240)):
        mid = Vector(top).lerp(Vector(foot), _ramp(top[2], foot[2], frost))
        part.bend(part.block("hair-" + name, "head", X, Y, [(top, 0.011, 0.004), (tuple(mid), 0.013, 0.004), (foot, 0.014, 0.004)],
                             lambda i, j: tones(*(FROST_T if i >= 1 else TRIM_T)), 0.0), hang)


# ---------------------------------------------------------------------------
# THE HAT. The original's "expedition ushanka", piece for piece: a padded crown with a dome, a
# pompom, a fur brim with ski goggles pushed up on it, a strap round the crown, an ear flap each
# side with a quilted cushion and a fur foot, a cord and bobble under each, a two tier mantle
# over the nape with a fur foot and an adjusting strap. Fewer, larger blocks than the original's
# seventy, each cut and tapered; the quilting is drawn (the textures script).
# ---------------------------------------------------------------------------
FLAP_FOOT = 0.350   # ⚠️ the original's fur hangs to 0.315, INTO the top of the sleeve. Raised to clear
#                     the shoulder: a straight arm's top is at 0.354, and she must be able to lift one.


def build_hat(part):
    cloth = proj("hat")
    white = tones(*WHITE_T)
    # the crown and its dome
    part.block("hat-crown", "head", X, Y, [((0, 0.022, 0.628), 0.206, (0.176, 0.206)), ((0, 0.022, 0.704), 0.206, (0.176, 0.206)),
                                          ((0, 0.022, 0.744), 0.192, (0.164, 0.194)), ((0, 0.022, 0.768), 0.156, (0.134, 0.164))], cloth, 0.022)
    part.block("hat-pompom", "head", X, Y, [((0, 0, 0.760), 0.054, 0.054), ((0, 0, 0.781), 0.056, 0.056), ((0, 0, 0.792), 0.036, 0.036)],
               white, 0.010)
    part.block("hat-pompom-star", "head", X, Y, [((0, 0, 0.789), 0.015, 0.015), ((0, 0, 0.7945), 0.012, 0.012)], tones(*TRIM_T), 0.0)
    # the fur brim across the forehead
    part.block("hat-brim", "head", X, Z, [((0, -0.150, 0.657), 0.190, 0.049), ((0, -0.232, 0.657), 0.154, 0.046)], white, 0.014)

    # THE GOGGLES, pushed up on the brim: one pale frame, two lenses standing off it, the frost
    # star on the bridge, the frame turned round each corner to meet the strap
    part.block("goggle-frame", "head", X, Z, [((0, -0.200, 0.662), 0.170, 0.038), ((0, -0.243, 0.662), 0.166, 0.036)],
               ftones("gog", AHEAD, SILVER_T), 0.010)
    for s in (1, -1):
        part.block("goggle-lens", "head", X, Z, [((s * 0.089, -0.240, 0.663), 0.063, 0.027), ((s * 0.089, -0.2505, 0.663), 0.059, 0.024)],
                   ftones("gog", AHEAD, TRIM_T), 0.005)
        part.wedge("goggle-wrap", "head", (s * 0.170, -0.224, 0.661), (s * 0.211, -0.146, 0.657), (0.035, 0.009), (0.022, 0.006),
                   tones(*SILVER_T), 0.006, across=Z)
        # the strap along the crown's side (the back strap closes it)
        part.block("goggle-strap", "head", X, Z, [((s * 0.2075, -0.150, 0.656), 0.003, 0.016), ((s * 0.2075, 0.228, 0.656), 0.003, 0.016)],
                   ftones("hat", (s, 0, 0), TEAL_T), 0.0)
    part.block("goggle-star", "head", X, Z, [((0, -0.243, 0.664), 0.022, 0.017), ((0, -0.2515, 0.664), 0.020, 0.015)],
               ftones("gog", AHEAD, TRIM_T), 0.0)
    part.block("goggle-strap-back", "head", Y, Z, [((-0.204, 0.238, 0.656), 0.003, 0.016), ((0.204, 0.238, 0.656), 0.003, 0.016)],
               ftones("hat", BEHIND, TEAL_T), 0.0)

    # THE EAR FLAPS. Each widens as it comes down off the crown and narrows to its foot.
    for s in (1, -1):
        part.bend(part.block("hat-flap", "head", X, Y, [
            ((s * 0.192, 0.020, 0.652), 0.020, (0.128, 0.140)),
            ((s * 0.205, 0.020, 0.560), 0.031, (0.128, 0.140)),
            ((s * 0.205, 0.022, 0.440), 0.031, (0.122, 0.136)),
            ((s * 0.199, 0.025, FLAP_FOOT + 0.012), 0.027, (0.107, 0.130))], cloth, 0.014), hang)
        # the cushion: a dark quilted panel standing 10 mm off the flap, a frost gem in its star
        y0, y1, z0, z1 = tex.CUSHION
        cy, cz = 0.5 * (y0 + y1), 0.5 * (z0 + z1)
        part.block("hat-cushion", "head", Y, Z, [((s * 0.232, cy, cz), 0.5 * (y1 - y0), 0.5 * (z1 - z0)),
                                                ((s * 0.246, cy, cz), 0.5 * (y1 - y0) - 0.006, 0.5 * (z1 - z0) - 0.006)],
                   ftones("hat", (s, 0, 0), TEAL_T), 0.007)
        part.block("hat-cushion-gem", "head", Y, Z, [((s * 0.246, 0.010, 0.490), 0.009, 0.009), ((s * 0.2515, 0.010, 0.490), 0.007, 0.007)],
                   tones(*FROST_T), 0.0)
        # the fur along the flap's foot, and the fur strip up its front edge that frames her cheek
        f0, f1 = tex.FLAP_FUR
        part.bend(part.block("hat-flap-fur", "head", X, Z, [((s * 0.200, -0.094, 0.5 * (f0 + f1)), 0.037, 0.5 * (f1 - f0)),
                                                           ((s * 0.200, 0.162, 0.5 * (f0 + f1)), 0.037, 0.5 * (f1 - f0))],
                             ftones("hat", (s, 0, 0), WHITE_T), 0.012), hang)
        part.bend(part.block("hat-cheek-fur", "head", X, Y, [((s * 0.184, -0.090, 0.486), 0.020, 0.028), ((s * 0.184, -0.090, f0 + 0.002), 0.022, 0.030)],
                             white, 0.010), hang)
        # the cord and its bobble. ⚠️ The original's hang 115 mm straight down from the middle of
        # the flap and are buried in the sleeve. Here they hang from the FRONT of the flap, ahead
        # of the upper arm, and shorter, so they are seen and nothing swings through them.
        # ⚠️ With the head at 0.84 the cord and bobble are CARRIED by the point they hang from, the
        # flap's foot, and the cord is plumb: bobble under flap, not out over the shoulder.
        top = (s * 0.198, -0.105, f0 + 0.004)
        part.carry(part.bend(part.block("hat-cord", "head", X, Y, [(top, 0.005, 0.005), ((s * 0.198, -0.105, 0.304), 0.005, 0.005)],
                                        tones(*TRIM_T), 0.0), hang), top)
        part.carry(part.bend(part.block("hat-bobble", "head", X, Y, [((s * 0.198, -0.105, 0.308), 0.019, 0.019), ((s * 0.198, -0.105, 0.268), 0.019, 0.019)],
                                        white, 0.010), hang), top)

    # THE MANTLE over the nape: two tiers, the lower stepped in, fur along its foot
    part.block("hat-mantle-upper", "head", X, Y, [((0, 0.196, 0.676), 0.199, 0.040), ((0, 0.197, 0.492), 0.196, 0.040)], cloth, 0.016)
    part.bend(part.block("hat-mantle-lower", "head", X, Y, [((0, 0.198, 0.500), 0.188, 0.034), ((0, 0.199, 0.378), 0.184, 0.033)], cloth, 0.012),
              hang)
    m0, m1 = tex.MANTLE_FUR
    part.bend(part.block("hat-mantle-fur", "head", Y, Z, [((-0.206, 0.204, 0.5 * (m0 + m1)), 0.038, 0.5 * (m1 - m0)),
                                                         ((0.206, 0.204, 0.5 * (m0 + m1)), 0.038, 0.5 * (m1 - m0))],
                         ftones("hat", BEHIND, WHITE_T), 0.012), hang)
    part.block("hat-back-strap", "head", X, Y, [((0, 0.2375, 0.640), 0.015, 0.004), ((0, 0.2375, 0.413), 0.015, 0.004)],
               ftones("hat", BEHIND, TEAL_T), 0.0)


# ---------------------------------------------------------------------------
# THE TORSO: a white shirt, and over it cyan overall shorts with a bib.
# CAST_CLOTHING_STYLE.md: every layer's edge is piped as GEOMETRY and one fastening is the focal
# point. Hers already are, in her own pale colours (which stay): the bib's dark top hem, the
# pocket's rim, the shorts' hem band, the two silver buckles and the bow at her neck are blocks.
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.343)
TORSO_LOW = (0.114, 0.090)     # half width, half depth at the hips
TORSO_HIGH = (0.124, 0.094)    # at the shoulders (the original is 0.118 by 0.092 all the way)
SHORTS_FOLLOW = 0.5            # how much of a leg's swing the shorts' hem takes


def torso_half(z):
    t = (z - TORSO_Z[0]) / (TORSO_Z[1] - TORSO_Z[0])
    return (TORSO_LOW[0] + (TORSO_HIGH[0] - TORSO_LOW[0]) * t, TORSO_LOW[1] + (TORSO_HIGH[1] - TORSO_LOW[1]) * t)


def chest_y(z):
    return -torso_half(z)[1]


def build_torso(part):
    part.block("torso", "torso", X, Y, [((0, 0, TORSO_Z[0]), *TORSO_LOW), ((0, 0, TORSO_Z[1]), *TORSO_HIGH)], proj("torso"), 0.014)

    # THE SHORTS: wider than the shirt, flared a little to the hem. They lie across the hips, so
    # the hem BENDS (rule 7): each side takes half of its own leg's swing.
    shorts = part.block("shorts", "torso", X, Y, [((0, 0, 0.1725), 0.133, 0.101), ((0, 0, tex.SHORTS_TOP), 0.124, 0.097)],
                        proj_except("torso", 2, "cyan_shade"), 0.010)
    hem = part.block("shorts-hem", "torso", X, Y, [((0, 0, tex.HEM[0]), 0.137, 0.105), ((0, 0, tex.HEM[1]), 0.1355, 0.1035)],
                     proj_except("torso", 2, "trim_shade"), 0.005)

    def shorts_weights(co):
        leg = SHORTS_FOLLOW * _ramp(0.222, 0.172, co.z)
        left = _ramp(-0.050, 0.050, co.x)
        left = left * left * (3.0 - 2.0 * left)
        return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]

    part.bend(shorts, shorts_weights)
    part.bend(hem, shorts_weights)

    # the bib, 12 mm proud of the shirt, narrowing to its top, and its dark top hem
    b0, b1 = tex.BIB
    part.block("bib", "torso", X, Y, [((0, chest_y(b0) - 0.004, b0), 0.104, 0.010), ((0, chest_y(b1) - 0.004, b1), 0.094, 0.010)],
               ftones("torso", AHEAD, CYAN_T), 0.005)
    part.block("bib-hem", "torso", X, Y, [((0, chest_y(0.312) - 0.007, 0.311), 0.097, 0.011), ((0, chest_y(0.324) - 0.007, 0.324), 0.096, 0.011)],
               tones(*TEAL_T), 0.0)
    # the pocket, its rim, and the wooden spatula handle tucked in its right side
    x0, x1, z0, z1 = tex.POCKET
    yb = chest_y(0.27) - 0.014
    part.block("bib-pocket", "torso", X, Z, [((0, yb, 0.5 * (z0 + z1)), 0.075, 0.5 * (z1 - z0)), ((0, yb - 0.008, 0.5 * (z0 + z1)), 0.073, 0.5 * (z1 - z0) - 0.002)],
               ftones("torso", AHEAD, TEAL_T), 0.004)
    part.block("bib-pocket-rim", "torso", Y, Z, [((-0.078, yb - 0.004, 0.292), 0.008, 0.005), ((0.078, yb - 0.004, 0.292), 0.008, 0.005)],
               tones(*TRIM_T), 0.0)
    part.block("spatula-handle", "torso", X, Y, [((-0.055, yb - 0.006, 0.290), 0.0095, 0.006), ((-0.055, yb - 0.006, 0.334), 0.0105, 0.006)],
               tones(*WOOD_T), 0.0)
    part.block("spatula-neck", "torso", X, Y, [((-0.055, yb - 0.006, 0.297), 0.0115, 0.0075), ((-0.055, yb - 0.006, 0.304), 0.0115, 0.0075)],
               tones(*SILVER_T), 0.0)

    for s in (1, -1):
        # a strap up from the bib over the shoulder and down the back, and its buckle
        yf = chest_y(0.33) - 0.006
        part.block("strap-front", "torso", X, Y, [((s * 0.0785, yf, 0.300), 0.0165, 0.007), ((s * 0.0785, yf + 0.002, 0.3445), 0.0165, 0.007)],
                   ftones("torso", AHEAD, CYAN_T), 0.0)
        part.block("strap-shoulder", "torso", X, Z, [((s * 0.0785, -0.094, 0.3435), 0.0165, 0.0035), ((s * 0.0785, 0.094, 0.3435), 0.0165, 0.0035)],
                   tones(*CYAN_T), 0.0)
        part.block("strap-back", "torso", X, Y, [((s * 0.0785, 0.096, tex.SHORTS_TOP - 0.004), 0.0165, 0.006), ((s * 0.0785, 0.098, 0.3445), 0.0165, 0.006)],
                   ftones("torso", BEHIND, CYAN_T), 0.0)
        part.block("strap-buckle", "torso", X, Z, [((s * 0.078, yf - 0.006, 0.312), 0.020, 0.008), ((s * 0.078, yf - 0.012, 0.312), 0.019, 0.007)],
                   tones(*SILVER_T), 0.0)
        part.block("strap-buckle-pin", "torso", X, Z, [((s * 0.078, yf - 0.012, 0.312), 0.006, 0.004), ((s * 0.078, yf - 0.0155, 0.312), 0.006, 0.004)],
                   tones(*FROST_T), 0.0)
        # a collar wing: a dark teal lapel, wide at the shoulder and cut to a point
        yc = chest_y(0.32) - 0.006
        part.block("collar-wing", "torso", X, Y, [((s * 0.030, yc, 0.300), 0.004, 0.006), ((s * 0.040, yc + 0.002, 0.3435), 0.024, 0.006)],
                   tones(*TEAL_T), 0.0)
        # a loop of the bow: narrow at the knot, wider at its end
        part.block("bow-loop", "torso", Z, Y, [((s * 0.010, yc - 0.009, 0.312), 0.006, 0.005), ((s * 0.047, yc - 0.007, 0.3140), 0.0145, 0.005)],
                   tones(*TRIM_T), 0.0)
    part.block("strap-brace", "torso", Y, Z, [((-0.086, 0.100, 0.286), 0.005, 0.010), ((0.086, 0.100, 0.286), 0.005, 0.010)],
               ftones("torso", BEHIND, TEAL_T), 0.0)
    yc = chest_y(0.32) - 0.006
    part.block("bow-knot", "torso", X, Z, [((0, yc - 0.004, 0.312), 0.014, 0.011), ((0, yc - 0.016, 0.312), 0.012, 0.009)], tones(*TRIM_T), 0.004)
    part.block("bow-gem", "torso", X, Z, [((0, yc - 0.015, 0.312), 0.006, 0.005), ((0, yc - 0.0185, 0.312), 0.005, 0.004)], tones(*FROST_T), 0.0)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests. `s` is +1 for her left, -1 for her right.
# Read off the original: a sleeve to 0.168, a teal cuff to 0.185, the hand's end at 0.280, the
# hand's top 0.062 over the bone. The original forearm is a flat paddle (0.058 thick, 0.124
# tall); this one is a tapered block, a little slimmer than Dante's: she is the small one.
# ---------------------------------------------------------------------------
ARM_Y, ARM_Z = 0.0173, 0.288
ARM_END = 0.280


def arm_block(part, name, bone, s, x0, x1, half0, half1, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x0, ARM_Y, ARM_Z), half0[0], half0[1]), ((s * x1, ARM_Y, ARM_Z), half1[0], half1[1])],
                      mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    arm_block(part, "sleeve", bone, s, 0.100, 0.182, (0.062, 0.064), (0.065, 0.067), proj_except(group, 0, "white_shade"), 0.013)
    # the cuff: a dark teal band standing proud of the sleeve's mouth, hiding the elbow
    arm_block(part, "sleeve-cuff", bone, s, 0.166, 0.187, (0.070, 0.072), (0.070, 0.072), tones(*TEAL_T), 0.006)
    # ⚠️ the forearm starts AT the elbow, where the sleeve's white stops in the paint
    # (tex.SLEEVE_END), so no white is carried out on the skin when the arm folds
    arm_block(part, "forearm", fore, s, ELBOW_X, 0.244, (0.042, 0.050), (0.036, 0.045), proj_except(group, 0, "skin_tone"), 0.009)
    if s > 0:
        # the sweatband on her LEFT wrist (the original's, which sat at 0.195 on a longer arm)
        b0, b1 = tex.BAND
        arm_block(part, "sweatband", fore, s, b0, b1, (0.0425, 0.0505), (0.0415, 0.0495), proj_except(group, 0, "trim_shade"), 0.004)
    # a plain block hand, as the cast's are; its fingers and thumb are drawn
    arm_block(part, "hand", fore, s, tex.HAND, ARM_END, (0.038, 0.056), (0.036, 0.054), proj_except(group, 0, "skin_tone"), 0.012)


# ---------------------------------------------------------------------------
# THE LEGS. Short, the cast's 24 per cent. Bare to the socks, which do not match (her original's
# asymmetry, kept): a long one on her left, a short one with a turned cuff on her right.
# Toes turned IN four degrees: Dante's turn out seven; hers is the small kid's stance.
# ---------------------------------------------------------------------------
LEG_X = 0.083
TOE_IN = 4.0


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    cloth = proj(group)
    base, top = tex.SOCK_BASE, tex.SOCK_TOP[s]
    part.block("leg", bone, X, Y, [((s * LEG_X, 0.0, base + 0.004), 0.062, 0.067), ((s * (LEG_X - 0.004), 0.0, 0.182), 0.060, 0.068)],
               proj_except(group, 2, "skin_tone"), 0.010)
    part.block("sock", bone, X, Y, [((s * LEG_X, 0.0, base), 0.067, 0.072), ((s * LEG_X, 0.0, top), 0.0675, 0.0725)],
               proj_except(group, 2, "white_shade"), 0.006)
    if s < 0:
        c0, c1 = tex.SOCK_CUFF
        part.block("sock-cuff", bone, X, Y, [((s * LEG_X, 0.0, c0), 0.071, 0.076), ((s * LEG_X, 0.0, c1), 0.071, 0.076)],
                   proj_except(group, 2, "trim_shade"), 0.005)

    turn = -math.radians(TOE_IN) * s
    cs, sn = math.cos(turn), math.sin(turn)

    def turned(p):
        x, y = p.x - s * LEG_X, p.y - 0.02
        return Vector((s * LEG_X + x * cs + y * sn, 0.02 - x * sn + y * cs, p.z))

    # the shoe: low at the toe, rising to the ankle, on a wider sole
    def station(y, half, z0, z1, cut):
        return [turned(p) for p in rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)]

    upper = [(-0.131, 0.042, 0.016, 0.031, 0.004), (-0.120, 0.058, 0.014, 0.041, 0.010), (-0.082, 0.070, 0.014, 0.052, 0.012),
             (-0.020, 0.069, 0.014, 0.056, 0.012), (0.068, 0.066, 0.014, 0.056, 0.012), (0.079, 0.055, 0.017, 0.050, 0.005)]
    part.loft("shoe", bone, [station(*row) for row in upper], cloth)
    part.block("sole", bone, X, Z, [((s * LEG_X, -0.136, 0.009), 0.058, 0.009), ((s * LEG_X, -0.090, 0.009), 0.076, 0.009),
                                   ((s * LEG_X, 0.000, 0.009), 0.075, 0.009), ((s * LEG_X, 0.083, 0.009), 0.067, 0.009)],
               cloth, 0.005, move=turned)
    # the pull tab at the heel, and its frost gem
    part.block("shoe-tab", bone, X, Z, [((s * LEG_X, 0.076, 0.052), 0.013, 0.012), ((s * LEG_X, 0.088, 0.052), 0.012, 0.011)],
               tones(*TRIM_T), 0.0, move=turned)
    part.block("shoe-tab-gem", bone, X, Z, [((s * LEG_X, 0.088, 0.052), 0.006, 0.005), ((s * LEG_X, 0.0915, 0.052), 0.005, 0.004)],
               tones(*FROST_T), 0.0, move=turned)


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
    """team-cheska.glb with its meshes swapped: skeleton and every other clip copied untouched."""
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
    # ⚠️ her file carries TWO skins, one a mesh, over the same seven joints. Both take the nine.
    binds = add([tuple(float(x) for x in m) for m in matrices], "f", "MAT4", 5126)
    for other in gltf["skins"]:
        if [gltf["nodes"][i]["name"] for i in other["joints"][:len(BONES)]] != BONES:
            raise SystemExit("a skin of the base rig has other joints")
        other["joints"] = list(skin["joints"])
        other["inverseBindMatrices"] = binds

    # the prototype's own clips, in place of the ones copied across (the clips script)
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (cheska)"}
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
    print("triangles %d  (budget %d, the original is 11700)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if abs(hi.z - HEIGHT) > 0.004:
        raise SystemExit("the top is at %.4f, the original's is %.3f" % (hi.z, HEIGHT))
    if tris > TRIANGLE_BUDGET:
        raise SystemExit("%d triangles is over the budget of %d" % (tris, TRIANGLE_BUDGET))
    # CharacterVisual.PalmCentre: the arm must run along x, and the hand's top must sit where a
    # carried tsinelas is parked. The original's hand top is 0.062 over the bone.
    body = objects[0]
    group = (body.vertex_groups["arm-right"].index, body.vertex_groups["forearm-right"].index)
    arm = [v.co for v in body.data.vertices if len(v.groups) == 1 and v.groups[0].group in group]
    size = [max(p[a] for p in arm) - min(p[a] for p in arm) for a in range(3)]
    if not (size[0] > size[1] and size[0] > size[2]):
        raise SystemExit("arm-right no longer runs along x: %s" % size)
    far = min(p.x for p in arm)
    if abs(far + ARM_END) > 0.002:
        raise SystemExit("the arm ends at %.4f, the original's at %.3f" % (-far, ARM_END))
    hand = [p for p in arm if p.x < far + size[0] / 8.0]
    top = max(p.z for p in hand) - ARM_Z
    print("hand top %.4f above the arm bone (the original's is 0.062)" % top)
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
    build_hat(head)
    objects = []
    for part in (body, head):
        part.resolve_uvs()
        if part is head:
            # AFTER the paint is placed (it is projected from the full-size shape): the head comes
            # in. A piece that HANGS (flap feet, fur, cords, bobbles, her long hair) is the head's
            # only in part, and is scaled by exactly that part: its top closes in with the hat, its
            # foot stays where it lay on her shoulders and back, so the hair still clears the body.
            for v in part.bm.verts:
                mix = part.blend.get(v)
                share = 1.0 if mix is None else sum(w for b, w in mix if b == "head") / sum(w for _, w in mix)
                share = part.carried.get(v, share)
                v.co = HEAD_JOINT + (v.co - HEAD_JOINT) * (1.0 + (HEAD_SCALE - 1.0) * share)
        obj = part.to_object(armature)
        obj.data.materials.append(material)
        objects.append(obj)
        print(part.name, sum(t for _, t in part.pieces), " ".join("%s:%d" % (n, t) for n, t in part.pieces))
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
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_cheska_textures.py")
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
