"""Build the Zack (displayed: Isagani) redesign PROTOTYPE: the same block kid, shaped and painted.

    py -3 tools/author_character_redesign_zack_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_zack.py

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/zack/zack-redesign.glb
    ArtSource/zack/redesign-20261005/zack_redesign.blend
beside the atlas the textures script paints.

WHY. docs/CHARACTER_REDESIGN_DANTE.md section 13. The owner, 2026-10-05, after Dante's rework:
"following dante's rework, redesign the rest of the characters". This is Zack's own copy of the
Dante builder, rewritten for him; it imports nothing from another hero's files.

A PROTOTYPE. NOTHING IN THE GAME LOADS IT. The folder is outside Resources, no roster row points
here, and team-zack.glb, build_zack_voxel.py and person_zack.asset are not touched.

THE ELEVEN RULES (section 13) AND WHERE EACH IS KEPT HERE:
  1  box head, `shaped` carving          HEAD_ROWS, build_head
  2  nothing invented                    every piece below is named after the box of team-zack.glb
                                         it replaces, and sits where that box sits
  3  no painted hair shine; blocks       build_hair: three flat tones, the back and the crown
                                         are layered slabs and graded chunks
  4  paint stops at its piece's edges    cuffs, bands, straps, trims are blocks in flat tones
  5  sewn marks hard edged               he has none; nothing is half hidden under another piece
  6  collars follow the head             COLLAR_FOLLOW, build_torso
  7  cloth across two bones bends        the wallet chain (torso to right leg), the nape points
  8  two elbow bones                     EXTRA_BONES; arms go up THROUGH the elbow (clips script)
  9  his own walk, sprint, jump, fall    tools/author_character_redesign_zack_clips.py
  10 feet on zero, original height       verify()
  11 render, look, fix                   tools/render_character_redesign_zack.py

WHAT WAS MEASURED OFF team-zack.glb (not assumed from Dante, and not from build_zack_voxel.py,
which the file has drifted from):
  * the head box 0.320 wide (Dante's is 0.340), 0.320 deep, from 0.343 to 0.661; ears out to
    x 0.227, set BEHIND the middle of the head (y -0.006 to 0.094), from 0.413 to 0.508
  * the hair's foot at 0.500 on the brow, its top (the quiff's peak) at 0.790
  * the torso from 0.176 to 0.343, the jacket 0.276 wide and 0.192 deep, the shirt 0.200 wide
  * arms straight out to 0.285 (Dante's reach 0.292), the sleeve to 0.182, the cuff to 0.196
  * the hand's top 0.062 above the arm bone; legs to 0.176; the toe at y -0.137
  * feet on zero, facing -y in Blender, +x his LEFT (the quiff, the earring, the lapel pin)

HOW A FACE FINDS ITS PAINT. A piece is tagged with a group ("head", "torso", "armL" ...) and each
of its faces goes to the island of the side its normal points at, at the place it sits in model
space. Loose blocks take flat tones instead: one for faces turned up, one for the sides, one under.
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
import author_character_redesign_zack_textures as tex  # noqa: E402  the island layout
import author_character_redesign_zack_clips as clips  # noqa: E402  the prototype's own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/team-zack.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/zack")
OUT = os.path.join(FOLDER, "zack-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/zack/redesign-20261005/zack_redesign.blend")
NAME = "zack-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# TWO BONES THE CAST DOES NOT HAVE (rule 8): an elbow in each arm, a child of the arm bone,
# appended AFTER the seven so their indices and names do not move. A clip that does not key them
# leaves the arm straight, exactly as before.
# HIS ELBOW IS NOT WHERE DANTE'S IS. Dante's sits at 0.172. Zack's sleeve runs to 0.182 and its
# cuff to 0.198, so an elbow at 0.172 would fold the sleeve itself. His is at 0.190, inside the
# cuff's mouth: the sleeve and cuff stay one rigid piece on the upper arm and the bare forearm
# turns inside the cuff, the way an arm turns inside a short sleeve.
# WHAT THIS COSTS IN THE GAME: `CharacterVisual.PalmCentre` finds the hand from `arm-right` alone.
# With the elbow bent the hand is not where that code parks a carried tsinelas. Not done here.
ELBOW_X = 0.190
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
# A SMALLER HEAD. Owner, 2026-10-05, after comparing sizes in the game on the whole lineup:
# "smaller heads are better", then "yes rebuild the heads at 84 for all seven". Everything below is
# still built and PAINTED at the original head's size; after the UVs are resolved every vertex that
# rides the `head` bone is scaled 0.84 about the head JOINT (see `shrink_head`). The paint keeps
# its place and nothing inside the head changes proportion, so the measured face is untouched.
HEAD_SCALE = 0.84
HEAD_JOINT = Vector((0.0, 0.00236, 0.343))   # the `head` bone of team-zack.glb, in Blender space
FULL_TOP = 0.792         # the quiff's peak as built (the original's is 0.790)
HEIGHT = HEAD_JOINT.z + (FULL_TOP - HEAD_JOINT.z) * HEAD_SCALE   # 0.720, the top after the scale
SHARP_ANGLE = 40.0       # degrees: a chamfer is a hard edge, a soft corner or a taper is one form
TRIANGLE_BUDGET = 6000   # the brief's ceiling; the original is 6,464

X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
AHEAD = (0, -1, 0)


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

    A block's END faces look along its own length, where the group has drawn something else.
    """
    return ("except", group, axis, name)


def proj_facing(group, direction, name):
    """The group's islands for the faces turned toward `direction`, one flat tone for the rest."""
    return ("facing", group, direction, name)


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

    def block(self, name, bone, u, v, stations, mapping, chamfer, move=None, ends=True):
        """A chamfered block through `stations` = [(centre, hu, hv)].

        Two stations of one size is a box. Different sizes taper it; moving the second centre
        off the axis leans it. `move` is applied to every point (the feet's turn-out).
        `ends=False` leaves the two end faces square: half the triangles, for small trims.
        """
        rings = []
        first, last = stations[0], stations[-1]
        along = (Vector(last[0]) - Vector(first[0]))
        length = along.length
        along = along.normalized()
        c = min(chamfer, 0.4 * length) if ends else 0.0

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
                rings.append(rect_ring(centre, u, v, hu, hv, chamfer))
        if move is not None:
            rings = [[move(p) for p in r] for r in rings]
        return self.loft(name, bone, rings, mapping)

    def bend(self, faces, weights):
        """Make a piece soft: `weights(position)` gives [(bone, weight)] for each of its vertices.

        The cast is rigid, one bone a vertex (Voxel_Person_Guide.md), and so is this model except
        where cloth, hair or a chain lies ACROSS two bones (rule 7).
        """
        for f in faces:
            for v in f.verts:
                self.blend[v] = weights(v.co)

    def wedge(self, name, bone, base, tip, base_half, tip_half, mapping, chamfer, across=X):
        """A block standing on `base` and narrowing to `tip`: a hair clump."""
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
                spec = proj(spec[1]) if face.normal.dot(Vector(spec[2])) > 0.6 else flat(spec[3])
            elif kind == "tones":
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


def shrink_head(part):
    """Scale what rides the head by HEAD_SCALE about the head joint. Call AFTER `resolve_uvs`.

    A vertex wholly on the head bone takes the whole scale. A vertex with blended head and torso
    weights (the collar, which grows out of the shoulders at full size and closes on the smaller
    head; the nape points, whose tips stay with the shoulders) takes the same share of the scale
    as it takes of the head, so nothing tears where the two meet.
    """
    layer = part.bm.verts.layers.int["bone"]
    head = ALL_BONES.index("head")
    for v in part.bm.verts:
        mix = part.blend.get(v)
        if mix:
            total = sum(w for _, w in mix) or 1.0
            share = sum(w for b, w in mix if b == "head") / total
        else:
            share = 1.0 if v[layer] == head else 0.0
        if share > 0.0:
            v.co = HEAD_JOINT + (v.co - HEAD_JOINT) * (1.0 + (HEAD_SCALE - 1.0) * share)


def _ramp(a, b, x):
    """0 at `a`, 1 at `b`, clamped."""
    return max(0.0, min(1.0, (x - a) / (b - a)))


# ---------------------------------------------------------------------------
# THE HEAD. The original's box (0.320 wide, 0.320 deep, 0.343 to 0.661), carved the way the owner
# chose for Dante (`shaped`: cheek fullness, a tapered jaw, soft corners) and still a box: every
# ring is a superellipse of exponent 3.4 or more (2 is a circle).
#   rows are (z, half width, depth to the front, depth to the back, exponent)
# NO NOSE AND NO BROW LEDGE (rule 12, the owner on the whole redesigned cast: "the faces look
# too realistic and look too human, like it lost its charm"). v01 to v08 had a nose wedge 20 mm
# proud and a 7 mm brow step at the hairline; both made the block read as an anatomical face and
# both are gone. The front is one nearly flat plane with a faint cheek fullness. The hair cap
# still stands 18 mm proud of it, and its edge does a brow's job (CHARACTER_MODEL_METHOD.md 2).
# ---------------------------------------------------------------------------
HEAD_CENTRE = Vector((0.0, 0.002, 0.50))
HEAD_N = 24
HEAD_ROWS = [
    # DANTE'S HEAD SHAPE, scaled to Zack's box (his is 0.320 wide where Dante's is 0.340). Owner,
    # 2026-10-05: "dante has a much more rounded face shape", and then: apply it to Zack. v12 and
    # v13 had squared the jaw to be rid of dark facets and left him a hard box. So, as on Dante:
    # superellipse rings of exponent 3.4 to 4.2, fullest at the cheeks (0.170, 6 per cent wider
    # than the 0.161 at the temples), narrowing gently to the crown, the lower corners rounding IN
    # toward the chin. No brow ledge and no nose (rule 12).
    # THE NO-DARK-SHAPES RULE STILL HOLDS: from the cheeks down to 0.358 the jaw leans in no more
    # than 18 degrees, which stays in the lit band; the tight closing rings are the last 15 mm,
    # down behind the collar and the jacket's shoulders.
    (0.343, 0.100, 0.112, 0.104, 3.4),
    (0.347, 0.130, 0.134, 0.128, 3.6),
    # THE FACE IS ONE FLAT PLANE: the front depth is the SAME (0.160) on every ring from 0.390,
    # just under the mouth, to 0.604, above the hairline. Found in the real game shader on Dante
    # (owner, with a Unity screenshot: "the eyebrow dent is making this weird shading artifact
    # where theres a straight line on his head"): in the two-band toon shader any ring where the
    # FRONT steps in or out flips the light band and draws a hard horizontal line across the
    # face. Blender's imitation hid it. Cheek fullness lives in the WIDTH only.
    (0.358, 0.147, 0.150, 0.142, 3.8),
    (0.390, 0.159, 0.160, 0.156, 4.0),
    (0.428, 0.170, 0.160, 0.162, 4.2),
    (0.470, 0.165, 0.160, 0.162, 4.2),
    (0.512, 0.161, 0.160, 0.162, 4.2),
    (0.604, 0.159, 0.160, 0.162, 4.2),
    (0.646, 0.152, 0.155, 0.158, 3.8),
    (0.661, 0.132, 0.134, 0.140, 3.4),
]
HAIRLINE = tex.HAIRLINE


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
    # ears: his are big and set back of the middle of the head, as the original's are
    for s in (1, -1):
        part.block("ear", "head", Y, Z, [((s * 0.156, 0.046, 0.460), 0.040, 0.044), ((s * 0.223, 0.050, 0.460), 0.030, 0.034)],
                   skin, 0.011)
    # the earring in his LEFT ear (the original's `earring-gold-hoop` and `earring-gold-drop`): a
    # chunky hoop through the lobe and a drop under it. The drop stops at 0.366, above the top of
    # the cuff and shoulder tab (0.364) when the arm is straight out.
    gold = tones("gold_lit", "gold_tone", "gold_dark")
    part.block("earring-hoop", "head", Y, Z, [((0.190, 0.034, 0.404), 0.023, 0.020), ((0.215, 0.036, 0.404), 0.023, 0.020)],
               gold, 0.009)
    part.block("earring-drop", "head", X, Y, [((0.203, 0.036, 0.388), 0.006, 0.010), ((0.203, 0.036, 0.366), 0.004, 0.006)],
               gold, 0.003, ends=False)


# ---------------------------------------------------------------------------
# THE HAIR. A spiky black mop with an electric yellow quiff, the original's, rebuilt as a low cap
# with chunky tapered clumps standing on it and hanging off it. Every clump is set by hand and is
# named after the box of team-zack.glb it replaces:
#     (name, base, tip, half size at the base, half size at the tip, the axis its width runs along)
# VOLUME ON TOP OVER TRIMMED SIDES (Voxel_Person_Guide.md 5.14: grown out all round it "looks like
# an afro which it isnt"). The side clumps stay within 45 mm of the cap; the height is on the crown.
# THE QUIFF REACHES THE SIDE (5.14: "the crimson has to reach the side faces"; the dye is yellow
# now but the rule is the same): `quiff-fringe-out` is on the cap's left corner.
# IT IS A BIG MOP. v02 beside the original (the trio render) had a lower cap and thinner clumps,
# and he read as a smaller head: the same kid with a haircut. The cap is at 0.700 now and the crown
# and side clumps reach 0.71 to 0.76, where the original's boxes do.
# NO SHINE (rule 3): three flat tones, by which way a face points, for the black and for the dye.
# ---------------------------------------------------------------------------
HAIR_CHAMFER = 0.012
CLUMPS_BLACK = [
    # his right side, stepped up toward the crown (spike-r-low, -mid, -upper, -top)
    ("r-low",     (-0.176, -0.012, 0.552), (-0.203, -0.020, 0.482), (0.040, 0.020), (0.022, 0.011), Y),
    ("r-mid",     (-0.168, -0.034, 0.566), (-0.224, -0.046, 0.640), (0.056, 0.034), (0.026, 0.014), Y),
    ("r-upper",   (-0.150, -0.022, 0.655), (-0.202, -0.030, 0.732), (0.054, 0.034), (0.023, 0.013), Y),
    ("r-top",     (-0.104, -0.012, 0.686), (-0.130, -0.020, 0.762), (0.042, 0.056), (0.018, 0.024), X),
    # one more a side, behind the ear line: from the side the cap was a flat black wall (rule 3)
    ("r-rear",    (-0.170, 0.096, 0.570), (-0.214, 0.118, 0.648), (0.048, 0.030), (0.022, 0.012), Y),
    ("l-rear",    (0.170, 0.104, 0.584), (0.208, 0.128, 0.652), (0.044, 0.028), (0.020, 0.011), Y),
    # his left side, behind the quiff (spike-l-low, -mid, -upper, -top-back)
    ("l-low",     (0.176, 0.000, 0.546), (0.200, -0.006, 0.486), (0.036, 0.019), (0.020, 0.010), Y),
    ("l-mid",     (0.168, -0.008, 0.556), (0.220, -0.014, 0.626), (0.052, 0.032), (0.024, 0.013), Y),
    ("l-upper",   (0.152, -0.016, 0.640), (0.204, -0.022, 0.710), (0.052, 0.032), (0.022, 0.012), Y),
    ("l-top",     (0.118, 0.030, 0.686), (0.144, 0.046, 0.750), (0.040, 0.050), (0.017, 0.021), X),
    # the crown behind the quiff, so the top is graded chunks and not one flat lid (rule 3)
    ("crown-r",   (-0.060, 0.094, 0.688), (-0.078, 0.122, 0.754), (0.054, 0.050), (0.023, 0.021), X),
    ("crown-l",   (0.056, 0.110, 0.688), (0.066, 0.136, 0.738), (0.046, 0.044), (0.019, 0.018), X),
    # the front, his right of the quiff (fringe-r-forehead, fringe-r-point, hairline-brow-peak)
    ("fringe-r",      (-0.100, -0.178, 0.664), (-0.120, -0.194, 0.512), (0.048, 0.022), (0.026, 0.012), X),
    ("fringe-peak",   (-0.034, -0.180, 0.652), (-0.012, -0.192, 0.500), (0.034, 0.020), (0.018, 0.011), X),
    ("fringe-temple", (-0.150, -0.166, 0.640), (-0.160, -0.176, 0.548), (0.022, 0.022), (0.013, 0.012), X),
    # the back: the jagged rear spikes (spike-back-top, -l, -r), then a layer of slabs hanging
    # over the back mass so it is not one flat black wall (rule 3)
    ("back-top",  (0.004, 0.150, 0.664), (0.012, 0.208, 0.736), (0.064, 0.036), (0.028, 0.014), X),
    ("back-l",    (0.100, 0.180, 0.652), (0.124, 0.210, 0.552), (0.046, 0.020), (0.024, 0.012), X),
    ("back-r",    (-0.098, 0.180, 0.660), (-0.128, 0.214, 0.566), (0.048, 0.020), (0.026, 0.012), X),
    ("back-mid",  (0.002, 0.190, 0.606), (-0.008, 0.208, 0.468), (0.056, 0.018), (0.032, 0.012), X),
    ("back-low-l", (0.074, 0.190, 0.524), (0.092, 0.206, 0.424), (0.046, 0.016), (0.024, 0.010), X),
    ("back-low-r", (-0.078, 0.190, 0.536), (-0.102, 0.208, 0.408), (0.048, 0.016), (0.026, 0.010), X),
]
#   the nape ends in three blunt points, each its own length, hanging OUTSIDE the collar's wall
NAPE = [
    ("nape-r", (-0.112, 0.180, 0.394), (-0.126, 0.188, 0.340), (0.044, 0.014), (0.022, 0.009), X),
    ("nape-m", (-0.006, 0.182, 0.394), (-0.002, 0.192, 0.326), (0.048, 0.014), (0.024, 0.009), X),
    ("nape-l", (0.102, 0.180, 0.394), (0.118, 0.186, 0.348), (0.040, 0.014), (0.020, 0.009), X),
]
CLUMPS_QUIFF = [
    # the fringe over his left brow, stepped down to a point above the eye (electric-fringe-root,
    # -main, -stepped, -tip), and one more lock on the cap's corner so the dye shows from the side
    ("quiff-fringe",     (0.064, -0.178, 0.686), (0.098, -0.212, 0.506), (0.066, 0.034), (0.032, 0.018), X),
    ("quiff-fringe-in",  (0.004, -0.180, 0.690), (0.034, -0.204, 0.552), (0.046, 0.030), (0.023, 0.014), X),
    ("quiff-fringe-out", (0.128, -0.166, 0.676), (0.152, -0.186, 0.560), (0.034, 0.030), (0.017, 0.014), X),
    # the crown: the tall spike that is his top (electric-spike-top-base and -peak), the accent
    # spike on his left (electric-spike-r-top), and the bridge running back (electric-crown-bridge)
    ("quiff-peak",       (0.002, -0.066, 0.686), (0.012, -0.092, 0.785), (0.066, 0.070), (0.030, 0.032), X),
    ("quiff-left",       (0.082, -0.032, 0.686), (0.106, -0.040, 0.754), (0.050, 0.056), (0.021, 0.024), X),
    ("quiff-bridge",     (0.000, 0.026, 0.686), (-0.004, 0.052, 0.730), (0.046, 0.038), (0.022, 0.016), X),
]


def build_hair(part):
    hair = tones("hair_top", "hair", "hair_under")
    # the mass underneath, down the back: one step deeper, so the layer over it reads as hair lying
    # on hair (CAST_CLOTHING_STYLE.md rule 7: the dark tone underneath and behind)
    deep = tones("hair", "hair_under", "hair_under")
    dye = tones("crest_top", "crest", "crest_under")
    part.block("hair-cap", "head", X, Y, [((0, 0.004, HAIRLINE), 0.178, (0.180, 0.180)), ((0, 0.006, 0.700), 0.164, (0.156, 0.164))],
               hair, 0.018)
    part.block("hair-back", "head", X, Y, [((0, 0.168, 0.368), 0.168, 0.022), ((0, 0.168, 0.512), 0.176, 0.024)],
               deep, HAIR_CHAMFER)
    # round each ear: a sideburn ahead of it and a panel behind it. The original laid one panel
    # straight across the ear's root; split, the ear block stands on skin and no hair touches it.
    for s in (1, -1):
        # (v01 ran the sideburn to 0.406 as a strap of one width; it read as a ribbon hanging by
        # the ear. It is a short point now.)
        part.block("sideburn", "head", X, Y, [((s * 0.168, -0.040, 0.436), 0.010, 0.008), ((s * 0.174, -0.034, 0.512), 0.014, 0.032)],
                   hair, 0.005, ends=False)
        # (as one width down to 0.392 it read as a strap hanging behind the ear; it narrows now)
        part.block("hair-behind-ear", "head", X, Y, [((s * 0.164, 0.150, 0.398), 0.012, 0.020), ((s * 0.170, 0.136, 0.512), 0.018, 0.046)],
                   deep, 0.007, ends=False)
    for name, base, tip, base_half, tip_half, across in CLUMPS_BLACK:
        part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, hair,
                   0.006 if name.startswith("fringe") else 0.010, across)
    for name, base, tip, base_half, tip_half, across in NAPE:
        faces = part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, deep, 0.006, across)
        # the points lie on his back: their tips stay with the shoulders when the head tips back.
        # (0.35 of the torso, not Dante's 0.6: his points are short, and at 0.6 a head tipped 22
        # degrees back drew each one out into a thin spike.)
        part.bend(faces, lambda co: [("head", 1.0 - 0.35 * _ramp(0.394, 0.330, co.z)), ("torso", 0.35 * _ramp(0.394, 0.330, co.z))])
    for name, base, tip, base_half, tip_half, across in CLUMPS_QUIFF:
        part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, dye,
                   0.007 if "fringe" in name else 0.010, across)


# ---------------------------------------------------------------------------
# THE TORSO. A black shirt block, and round it the open jacket as ONE shell with a wall, a hem band
# and a lining, so the opening has an edge and a depth instead of being a colour change.
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.343)
TORSO_CY = 0.0
SHIRT_LOW = (0.096, 0.083)      # half width, half depth at the belt
SHIRT_HIGH = (0.104, 0.087)     # at the shoulders
JACKET_TOP = (0.136, 0.100, 0.094)    # half width, depth to the front, depth to the back
JACKET_HEM = (0.141, 0.105, 0.098)    # a touch wider at the hem band, as the original's is
JACKET_WALL = 0.010
HEM = (0.174, 0.194)            # the ochre hem band (the original's `jacket-hem-*`: 0.174 to 0.192)
OPEN_AT = 4.25                  # degrees from straight ahead: the front edges sit at x 0.048
POCKET_Z = (0.204, 0.242)


def jacket_front_y(z):
    f = 1.0 - (z - TORSO_Z[0]) / (TORSO_Z[1] - TORSO_Z[0])
    return TORSO_CY - (JACKET_TOP[1] + (JACKET_HEM[1] - JACKET_TOP[1]) * f)


def shirt_front_y(z):
    t = (z - TORSO_Z[0]) / (TORSO_Z[1] - TORSO_Z[0])
    return TORSO_CY - (SHIRT_LOW[1] + (SHIRT_HIGH[1] - SHIRT_LOW[1]) * t)


#   THE COLLAR. The original has a turned-up band behind the neck (`collar-back`, 0.323 to 0.350)
#   and two lapels. The band stood rigid on the torso and ran 7 mm into the underside of the head.
#   Rule 6: it grows out of the shoulders and from the shelf up it is the HEAD's, so the head
#   cannot turn or nod through it. It is the same on both sides, and it is a BACK collar: it stops
#   at the sides (80 degrees of each side of straight ahead left open) where the lapels take over.
#   (s, top height) from his right tip (0) behind him to his left tip (1):
COLLAR_TOP = [(0.00, 0.345), (0.07, 0.353), (0.18, 0.359), (0.34, 0.360), (0.50, 0.360),
              (0.66, 0.360), (0.82, 0.359), (0.93, 0.353), (1.00, 0.345)]
COLLAR_BASE = 0.338      # 5 mm INTO the jacket's shoulders
COLLAR_SHELF = 0.347
COLLAR_WALL = 0.007
COLLAR_FOLLOW = 0.92
#   (height, half width, depth to the front, depth to the back) of its outer face.
#   IT HUGS THE TAPER UNDER THE JAW. v01 spread it 8 mm clear of the head's widest lower ring and
#   it stood out behind the neck as a flat ochre tray, 60 mm past the jacket's back. The head
#   narrows to 0.098 by 0.108 at its foot, so the collar can follow that taper 4 to 6 mm off it
#   and stay a band, which is what the original's `collar-back` is.
#   (Refitted to the rounded jaw: 6 to 8 mm off the head's own rings at each height.)
COLLAR_PROFILE = [(0.338, 0.124, 0.092, 0.090), (0.347, 0.138, 0.128, 0.134), (0.360, 0.156, 0.150, 0.150)]
COLLAR_GAP = 80.0


def build_torso(part):
    part.block("shirt", "torso", X, Y, [((0, TORSO_CY, TORSO_Z[0]), *SHIRT_LOW), ((0, TORSO_CY, TORSO_Z[1]), *SHIRT_HIGH)],
               proj_facing("shirt", AHEAD, "cloth_tone"), 0.012)

    # THE JACKET: sections round the body from his right front edge, behind him, to his left front
    # edge. Each section runs down the outside, round the hem band (4 mm proud, rule 3 of
    # CAST_CLOTHING_STYLE.md: a trim is geometry), and back up the lining.
    cloth = proj("torso")
    band = tones("ochre_lit", "ochre", "ochre_dark")
    lining = flat("ochre_dark")
    sections = []
    half = (OPEN_AT, 9, 16, 27, 45, 66, 90, 114, 135, 156, 172)
    for theta in half + (180,) + tuple(360 - a for a in reversed(half)):
        a = math.radians(theta)
        px = -math.copysign(abs(math.sin(a)) ** 0.4, math.sin(a))     # his right first, then behind
        py = -math.copysign(abs(math.cos(a)) ** 0.4, math.cos(a))     # -1 in front, +1 behind

        def at(z, inset):
            f = 1.0 - (z - TORSO_Z[0]) / (TORSO_Z[1] - TORSO_Z[0])
            r = [JACKET_TOP[k] + (JACKET_HEM[k] - JACKET_TOP[k]) * f - inset for k in range(3)]
            return Vector((px * r[0], TORSO_CY + py * (r[2] if py >= 0 else r[1]), z))

        w = JACKET_WALL
        sections.append([at(TORSO_Z[1], 0.0), at(0.262, 0.0), at(HEM[1], 0.0), at(HEM[1] - 0.0005, -0.004),
                         at(HEM[0], -0.004), at(HEM[0] + 0.002, w), at(0.262, w), at(TORSO_Z[1], w)])
    part.loft("jacket", "torso", sections, lambda i, j: (cloth, cloth, band, band, band, lining, lining, cloth)[j])

    # the lapels: ochre slabs lying on the front edges, wider toward the shoulder
    trim = tones("ochre_lit", "ochre", "ochre_dark")
    for s in (1, -1):
        part.block("lapel", "torso", X, Y,
                   [((s * 0.050, jacket_front_y(0.284) - 0.003, 0.284), 0.011, 0.006),
                    ((s * 0.064, jacket_front_y(0.341) - 0.003, 0.341), 0.030, 0.006)], trim, 0.004, ends=False)
    # the gold pin on his left lapel
    y = jacket_front_y(0.318) - 0.009
    part.block("lapel-pin", "torso", X, Z, [((0.062, y, 0.318), 0.009, 0.007), ((0.062, y - 0.006, 0.318), 0.009, 0.007)],
               tones("gold_lit", "gold_tone", "gold_dark"), 0.003, ends=False)

    # the two welt pockets: blocks 9 mm off the jacket; their fronts are drawn in the jacket's island
    zc, zh = 0.5 * (POCKET_Z[0] + POCKET_Z[1]), 0.5 * (POCKET_Z[1] - POCKET_Z[0])
    for s in (1, -1):
        y = jacket_front_y(zc)
        part.block("pocket", "torso", X, Z, [((s * 0.094, y + 0.002, zc), 0.032, zh), ((s * 0.094, y - 0.009, zc), 0.032, zh)],
                   proj_facing("torso", AHEAD, "ochre_dark"), 0.004, ends=False)

    # the belt, in the opening: a band 5 mm proud of the shirt, one step paler than it so it reads
    part.block("belt", "torso", X, Y, [((0, TORSO_CY, 0.177), 0.101, 0.088), ((0, TORSO_CY, 0.201), 0.102, 0.088)],
               tones("cloth_hi", "cloth_lit", "cloth_deep"), 0.004, ends=False)
    # the buckle: a gold frame, the dark slot standing off it, the gold pin standing off that
    # (the original's `buckle-gold-frame`, `buckle-center-slot`, `buckle-gold-pin`)
    y = shirt_front_y(0.189) - 0.005
    gold = tones("gold_lit", "gold_tone", "gold_dark")
    part.block("buckle", "torso", X, Z, [((0, y, 0.1885), 0.042, 0.0185), ((0, y - 0.016, 0.1885), 0.042, 0.0185)], gold, 0.006)
    part.block("buckle-slot", "torso", X, Z, [((0, y - 0.014, 0.1885), 0.024, 0.0105), ((0, y - 0.019, 0.1885), 0.024, 0.0105)],
               flat("cloth_tone"), 0.003, ends=False)
    part.block("buckle-pin", "torso", X, Z, [((0, y - 0.017, 0.1885), 0.005, 0.0105), ((0, y - 0.023, 0.1885), 0.005, 0.0105)],
               gold, 0.0, ends=False)

    # the necklace: two silver strands from under the jaw to a bail, and the crystal hanging from
    # it (`necklace-chain-l/r`, `-bail`, `-gem-body`, `-gem-core`, `-gem-tip`)
    silver = tones("silver_lit", "silver", "silver_shade")
    for s in (1, -1):
        a = Vector((s * 0.040, shirt_front_y(0.338) - 0.004, 0.338))
        b = Vector((s * 0.012, shirt_front_y(0.298) - 0.007, 0.298))
        w = (b - a).normalized()
        u = (X - w * X.dot(w)).normalized()
        part.block("necklace-strand", "torso", u, w.cross(u), [(a, 0.0045, 0.0045), (b, 0.0045, 0.0045)], silver, 0.0, ends=False)
    y = shirt_front_y(0.296) - 0.010
    part.block("necklace-bail", "torso", X, Y, [((0, y, 0.302), 0.012, 0.007), ((0, y, 0.290), 0.009, 0.007)], silver, 0.003, ends=False)
    # the crystal: six facets, a shoulder, a long taper to a point. One facet lit, the back dark.
    rings = []
    for z, r in ((0.293, 0.010), (0.284, 0.022), (0.266, 0.019)):
        rings.append([Vector((math.cos(t) * r, y - 0.004 + math.sin(t) * r * 0.62, z))
                      for t in (2.0 * math.pi * (j + 0.5) / 6 for j in range(6))])
    facet = {3: "crystal_lit", 4: "crystal", 5: "crystal", 0: "crystal_deep", 1: "crystal_deep", 2: "crystal"}
    part.loft("necklace-crystal", "torso", rings, lambda i, j: flat(facet[j]), caps=(True, False), tip=Vector((0.0, y - 0.004, 0.244)))

    # the collar (see COLLAR_TOP)
    def profile(height):
        rows = COLLAR_PROFILE
        for (z0, *lo), (z1, *hi) in zip(rows, rows[1:]):
            if height <= z1 or z1 == rows[-1][0]:
                f = (height - z0) / (z1 - z0)
                return [lo[k] + (hi[k] - lo[k]) * f for k in range(3)]

    sections = []
    for s, top in COLLAR_TOP:
        angle = math.radians((270.0 - COLLAR_GAP) - s * (360.0 - 2.0 * COLLAR_GAP))
        c, sn = math.cos(angle), math.sin(angle)
        # a squared path, so the collar turns the corners the head does
        px = math.copysign(abs(c) ** (2.0 / 5.0), c)
        py = math.copysign(abs(sn) ** (2.0 / 5.0), sn)

        def at(height, inset):
            rx, rf, rb = profile(height)
            return Vector((px * (rx - inset), 0.001 + py * ((rb if sn >= 0 else rf) - inset), height))

        w = COLLAR_WALL
        shelf = min(COLLAR_SHELF, top - 0.003)
        sections.append([at(COLLAR_BASE, 0.0), at(shelf, 0.0), at(top, 0.0), at(top, w), at(shelf, w), at(COLLAR_BASE, w)])
    collar = part.loft("collar", "torso", sections,
                       lambda i, j: flat(("ochre", "ochre", "ochre_lit", "ochre_dark", "ochre_dark", "ochre_dark")[j]))
    # FROM THE SHELF UP IT IS THE HEAD'S. A wall that turns WITH the box it surrounds cannot be
    # turned through; all of the twist is taken by the 12 mm between the shoulders and the shelf.
    part.bend(collar, lambda co: [("torso", 1.0 - COLLAR_FOLLOW * _ramp(COLLAR_BASE + 0.002, COLLAR_SHELF, co.z)),
                                  ("head", COLLAR_FOLLOW * _ramp(COLLAR_BASE + 0.002, COLLAR_SHELF, co.z))])


# ---------------------------------------------------------------------------
# THE WALLET CHAIN. Five silver links from a ring on his belt, down across the right thigh and up
# to the cargo pocket (the original's `chain-belt-ring` and `chain-loop-1..5`).
# IT LIES ACROSS TWO BONES, SO IT BENDS (rule 7). In the original the ring is the torso's and all
# five links are the leg's, so the chain snaps off its ring as the leg swings. Here each link is
# one rigid piece and takes more of the leg the further down the chain it is.
#   (centre, how much of the right leg it follows)
# ---------------------------------------------------------------------------
CHAIN = [
    ((-0.052, -0.111, 0.184), 0.0),      # the ring
    ((-0.068, -0.086, 0.162), 0.45),
    ((-0.094, -0.083, 0.141), 0.80),
    ((-0.122, -0.082, 0.125), 1.0),
    ((-0.147, -0.068, 0.118), 1.0),
    ((-0.160, -0.044, 0.123), 1.0),
]


def build_chain(part):
    silver = tones("silver_lit", "silver", "silver_shade")
    points = [Vector(p) for p, _ in CHAIN]
    for k, (centre, follow) in enumerate(CHAIN):
        here = Vector(centre)
        if k == 0:
            faces = part.block("chain-ring", "torso", X, Z, [(here + Vector((0, 0.006, 0)), 0.010, 0.009), (here - Vector((0, 0.006, 0)), 0.010, 0.009)],
                               silver, 0.004, ends=False)
        else:
            w = (points[min(k + 1, len(points) - 1)] - points[k - 1]).normalized()
            u = Z.cross(w).normalized()
            v = w.cross(u)
            # links lie flat and on edge by turns, as a chain's do
            size = (0.009, 0.0045) if k % 2 else (0.0045, 0.009)
            faces = part.block("chain-link", "leg-right", u, v, [(here - w * 0.015, *size), (here + w * 0.015, *size)],
                               silver, 0.004, ends=False)
        if 0.0 < follow < 1.0:
            part.bend(faces, lambda co, f=follow: [("torso", 1.0 - f), ("leg-right", f)])


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests. `s` is +1 for his left, -1 for his right.
# The mesh is centred 8 mm ahead of the arm bone (y 0.009 against 0.017), as the original's is.
# ---------------------------------------------------------------------------
ARM_Y, ARM_Z = 0.009, 0.288


def arm_block(part, name, bone, s, x0, x1, half0, half1, mapping, chamfer, ends=True):
    return part.block(name, bone, Y, Z, [((s * x0, ARM_Y, ARM_Z), half0[0], half0[1]), ((s * x1, ARM_Y, ARM_Z), half1[0], half1[1])],
                      mapping, chamfer, ends=ends)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    ochre = tones("ochre_lit", "ochre", "ochre_dark")
    # the sleeve and its cuff: one rigid piece on the upper arm
    arm_block(part, "sleeve", bone, s, 0.100, 0.188, (0.075, 0.070), (0.073, 0.068), proj_except(group, 0, "ochre_dark"), 0.013)
    arm_block(part, "cuff", bone, s, 0.181, 0.198, (0.081, 0.076), (0.081, 0.076), ochre, 0.006)
    # the tab on the shoulder (the original's `sleeve-stripe-*`), 5 mm proud of the sleeve's top
    part.block("shoulder-tab", bone, Y, Z, [((s * 0.114, ARM_Y, ARM_Z + 0.070), 0.056, 0.005), ((s * 0.177, ARM_Y, ARM_Z + 0.068), 0.054, 0.005)],
               ochre, 0.003, ends=False)
    # the bare forearm starts inside the cuff's mouth and turns there (see ELBOW_X)
    arm_block(part, "forearm", fore, s, 0.174, 0.232, (0.051, 0.061), (0.050, 0.060), proj_except(group, 0, "skin_tone"), 0.010)
    # the neon band under the cuff. team-zack.glb carries one on EACH arm (x 0.202 to 0.220), where
    # build_zack_voxel.py has one on the left wrist only. The file is the game, so both are kept.
    # It starts AT the cuff's mouth (the original's starts 6 mm past it): with a gap, the bent
    # forearm of `idle` showed a salmon stripe of skin between two yellow bands from behind.
    arm_block(part, "band", fore, s, 0.197, 0.217, (0.056, 0.066), (0.056, 0.066), tones("crest_top", "crest", "crest_under"), 0.004, ends=False)
    # the hand: a plain block, as the cast's are; its fingers and thumb are drawn
    arm_block(part, "hand", fore, s, 0.226, 0.285, (0.052, 0.062), (0.051, 0.062), proj(group), 0.013)


# ---------------------------------------------------------------------------
# THE LEGS. Short, the cast's 24 per cent. Feet turned out four degrees.
# ---------------------------------------------------------------------------
LEG_X = 0.084
TOE_OUT = 4.0


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    dark = tones("cloth_hi", "cloth_lit", "cloth_deep")
    # cargo trousers: a touch wider at the cuff than at the hip
    part.block("trouser", bone, X, Y, [((s * LEG_X, 0.0, 0.058), 0.067, 0.073), ((s * (LEG_X - 0.002), 0.0, 0.178), 0.063, 0.071)],
               proj_except(group, 2, "cloth_tone"), 0.010)
    part.block("trouser-cuff", bone, X, Y, [((s * LEG_X, 0.0, 0.052), 0.071, 0.078), ((s * LEG_X, 0.0, 0.067), 0.071, 0.078)],
               dark, 0.005, ends=False)
    # the thigh strap, the knee panel over it, the side pocket, its flap and its steel buckle
    part.block("cargo-strap", bone, X, Y, [((s * LEG_X, 0.0, 0.100), 0.0705, 0.0765), ((s * LEG_X, 0.0, 0.112), 0.0700, 0.0760)],
               dark, 0.004, ends=False)
    part.block("knee-panel", bone, X, Z, [((s * LEG_X, -0.070, 0.110), 0.056, 0.014), ((s * LEG_X, -0.083, 0.110), 0.054, 0.013)],
               dark, 0.005, ends=False)
    part.block("cargo-pocket", bone, Y, Z, [((s * 0.144, 0.0, 0.106), 0.045, 0.023), ((s * 0.161, 0.0, 0.106), 0.043, 0.022)],
               proj_facing(group, (s, 0, 0), "cloth_lit"), 0.005)
    part.block("cargo-flap", bone, Y, Z, [((s * 0.144, 0.0, 0.130), 0.048, 0.007), ((s * 0.166, 0.0, 0.129), 0.048, 0.007)],
               dark, 0.003, ends=False)
    part.block("cargo-buckle", bone, Y, Z, [((s * 0.158, 0.0, 0.106), 0.016, 0.008), ((s * 0.168, 0.0, 0.106), 0.016, 0.008)],
               flat("steel"), 0.003, ends=False)

    turn = math.radians(TOE_OUT) * s
    cs, sn = math.cos(turn), math.sin(turn)

    def turned(p):
        x, y = p.x - s * LEG_X, p.y - 0.02
        return Vector((s * LEG_X + x * cs + y * sn, 0.02 - x * sn + y * cs, p.z))

    # the shoe: a yellow upper that is low at the toe and rises to the ankle, on a wider white
    # sole, with a white toe cap, a white heel counter and two white laces (all the original's)
    def station(y, half, z0, z1, cut):
        return [turned(p) for p in rect_ring((s * (LEG_X - 0.002), y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)]

    upper = [(-0.129, 0.050, 0.018, 0.034, 0.005), (-0.116, 0.063, 0.016, 0.044, 0.010), (-0.080, 0.068, 0.016, 0.056, 0.012),
             (-0.010, 0.069, 0.016, 0.061, 0.012), (0.066, 0.067, 0.016, 0.061, 0.012), (0.078, 0.057, 0.018, 0.052, 0.005)]
    part.loft("shoe", bone, [station(*row) for row in upper], proj(group))
    white = tones("white", "white", "white_shade")
    x = s * (LEG_X - 0.002)
    part.block("sole", bone, X, Z, [((x, -0.137, 0.009), 0.060, 0.009), ((x, -0.092, 0.009), 0.076, 0.009),
                                   ((x, 0.000, 0.009), 0.076, 0.009), ((x, 0.082, 0.009), 0.069, 0.009)],
               white, 0.005, move=turned)
    # (v01 made the cap and the counter as tall as the upper; with the trouser cuff over the
    # ankle the shoe read as a white block. They are the original's heights now: the cap to 0.036,
    # the counter to 0.042, so the yellow upper shows over both.)
    part.block("toe-cap", bone, X, Z, [((x, -0.136, 0.025), 0.054, 0.008), ((x, -0.104, 0.0265), 0.0705, 0.0095)],
               white, 0.005, move=turned)
    # (it starts inside the upper, 2 mm narrower than it, so its front corner cannot break through
    # the yellow side as a white tooth; only the part behind y 0.058 stands proud)
    part.block("heel-counter", bone, X, Z, [((x, 0.040, 0.030), 0.065, 0.012), ((x, 0.060, 0.030), 0.0705, 0.012), ((x, 0.084, 0.029), 0.062, 0.011)],
               white, 0.005, move=turned)
    for y, z in ((-0.097, 0.0505), (-0.087, 0.0540)):
        part.block("lace", bone, Y, Z, [((x - 0.046, y, z), 0.004, 0.003), ((x + 0.046, y, z), 0.004, 0.003)],
                   white, 0.0, move=turned, ends=False)


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
    """team-zack.glb with its meshes swapped: skeleton, bind matrices and clips copied untouched,
    two elbow joints appended, and walk, sprint, jump and fall replaced by the clips script's."""
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
    raw = struct.unpack("<%df" % (16 * len(BONES)), accessor_raw)
    matrices = [raw[k * 16:(k + 1) * 16] for k in range(len(BONES))]
    added = []
    for name, parent, side in EXTRA_BONES:
        world = (side * ELBOW_X, ARM_Z, -ARM_Y)
        pw = [-matrices[BONES.index(parent)][12 + a] for a in range(3)]
        gltf["nodes"].append({"name": name, "translation": [world[a] - pw[a] for a in range(3)]})
        gltf["nodes"][node_of[parent]].setdefault("children", []).append(len(gltf["nodes"]) - 1)
        added.append(len(gltf["nodes"]) - 1)
        matrices.append((1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -world[0], -world[1], -world[2], 1))
    ibm = add([tuple(float(x) for x in m) for m in matrices], "f", "MAT4", 5126)
    for skin in gltf["skins"]:
        skin["joints"] = list(skin["joints"]) + added
        skin["inverseBindMatrices"] = ibm

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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (zack)"}
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
    print("triangles %d  (budget %d, the original is 6464)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if abs(hi.z - HEIGHT) > 0.004:
        raise SystemExit("the top is at %.4f, the original's is %.3f" % (hi.z, HEIGHT))
    if tris > TRIANGLE_BUDGET:
        raise SystemExit("%d triangles is over the budget of %d" % (tris, TRIANGLE_BUDGET))
    # CharacterVisual.PalmCentre: the arm must run along x, and the hand's top must sit where a
    # carried tsinelas is parked (the original's hand top is 0.062 above the arm bone).
    body = objects[0]
    group = (body.vertex_groups["arm-right"].index, body.vertex_groups["forearm-right"].index)
    arm = [v.co for v in body.data.vertices if v.groups[0].group in group]
    size = [max(p[a] for p in arm) - min(p[a] for p in arm) for a in range(3)]
    if not (size[0] > size[1] and size[0] > size[2]):
        raise SystemExit("arm-right no longer runs along x: %s" % size)
    far = min(p.x for p in arm)
    hand = [p for p in arm if p.x < far + size[0] / 8.0]
    top = max(p.z for p in hand) - ARM_Z
    print("arm reach %.4f (the original's is 0.2847); hand top %.4f above the arm bone (the original's is 0.062)" % (-far, top))
    if not 0.054 <= top <= 0.066:
        raise SystemExit("the hand's top moved; a carried tsinelas would float or sink")


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
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_zack_textures.py")
    image = bpy.data.images.load(image_path)
    material = bpy.data.materials.new(NAME)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    node = nodes.new("ShaderNodeTexImage")
    node.image = image
    material.node_tree.links.new(node.outputs[0], nodes["Principled BSDF"].inputs["Base Color"])
    nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0

    body, head = Part("body-mesh"), Part("head-mesh")
    build_torso(body)
    build_chain(body)
    build_arm(body, 1)
    build_arm(body, -1)
    build_leg(body, 1)
    build_leg(body, -1)
    build_head(head)
    build_hair(head)
    objects = []
    for part in (body, head):
        part.resolve_uvs()
        shrink_head(part)
        obj = part.to_object(armature)
        obj.data.materials.append(material)
        objects.append(obj)
        print(part.name, " ".join("%s:%d" % (n, t) for n, t in part.pieces))
    verify(objects)
    write_glb(mesh_arrays(objects[0]), mesh_arrays(objects[1]), OUT)
    print("wrote " + OUT)

    os.makedirs(os.path.dirname(BLEND), exist_ok=True)
    image.filepath = bpy.path.relpath(image_path, start=os.path.dirname(BLEND))
    bpy.context.preferences.filepaths.save_version = 0   # no .blend1 beside it
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)
    print("wrote " + BLEND)


if __name__ == "__main__":
    main()
