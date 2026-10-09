"""Build the Lola Pacing redesign PROTOTYPE: Lola Pacing redrawn as if she were one of the heroes.

    py -3 tools/author_character_redesign_lola_pacing_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_lola_pacing.py
    ... -- --out other.glb     (writes there instead and leaves the .blend alone; needs the atlas beside it)

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/lola_pacing/lola_pacing-redesign.glb
    ArtSource/lola_pacing/redesign-20261006/lola_pacing_redesign.blend
beside the atlas painted by the textures script.

THE BRIEF, AS CORRECTED AT v05. Owner, 2026-10-06, on the first three Classic patterns: "head
size stays the same as the heroes. my issue is they focus on resembling closer to the original
design instead of being reworked to be more in the heroes' style. the eyes and face design are
different from the heros". So she is not the original with more detail. She is THE SAME PERSON
DESIGNED IN THE HEROES' VISUAL LANGUAGE. For the Classic cast "nothing invented" is LIFTED;
HEAD_SCALE stays 0.84. v01 to v04 followed the first brief and are in Logs for comparison.

A PROTOTYPE. NOTHING IN THE GAME LOADS IT. The folder is outside Resources, no roster row points
here, and character-female-d.glb, person_lola_pacing.asset and every runtime script are not
touched.

WHO SHE IS (`ConvertedCharacterSelect`, docs/GAME_OVERVIEW.md; docs/CHARACTER_ORIGINS.md has
no entry for the Classic cast): "Watches from the window most afternoons. On the good ones she
comes down to play, and she does not miss twice." The slowest in the game and the hardest to
move. A neat older lady of the street, an everyday person: no powers, no gear, no costume.

WHAT IS HERS AND IS KEPT, from character-female-d.glb and her palette: the big BUN high on the
back of her head (her silhouette); her ears with a square white earring under each; a neat
taupe jacket with lapels over a white blouse, with a short flared skirt from the waist; sleeves
that open into a bell over a white cuff; taupe trousers with a flared hem; the body's own
measures (torso, legs, arm lengths, the 111 mm fist); feet on zero, facing -y.

WHAT IS DESIGNED, at the heroes' level, each a thing a Filipino lola has:
  * HER HAIR IS SILVER (her palette's slot 11; a decision of 2026-10-06 pending the owner's
    word, see the textures script) and is built the way the heroes' is: a swept fringe of five
    graded locks, set curls stacked over each temple, behind each ear and in two rows across the
    back of her head, the bun in three wound coils, and a red COMB (a payneta) standing in front
    of the bun;
  * a red BROOCH pinned on the blouse at her throat;
  * the jacket has red PIPING down its lapels, at its hem and round the mouth of each sleeve, a waistband, and a
    small flower PRINT on its skirt and bells (drawn, in the textures script);
  * a folded FAN (her pamaypay) tucked in the waistband at her left hip. Her idle fans herself;
  * SLIPPERS, the closed velvet house slipper (alfombra) in her red on a dark sole with a white
    edge, her heel bare behind it. The original's dark shoes are gone.
One accent only, her palette's slot 4 red, on taupe and white. She copies no hero's outfit.

THE RULES OF SECTION 15.3 STILL IN FORCE: the head is Dante's rounded `shaped` rows
(`HEAD_ROWS`); THE FACE IS ONE FLAT PLANE (front depth 0.158 from 0.392 to 0.600); the head is
0.84 about the head joint, applied after the UVs resolve; a cute face, no nose, nothing of age
drawn; a drawing only on a face that squarely faces its view (`proj_square`); no painted hair
shine; cloth across two bones bends (`Part.bend`: the skirt of her jacket takes the legs, a
curl hanging below the jaw takes the torso); under 6,000 triangles.

ONE THING IS STRAIGHTENED. The original's forearm is modelled BENT 25.6 degrees forward at the
elbow, in the bind pose itself. Here the arm is straight along x in the bind pose (the first
person arms are cut from this mesh and need that) with the same lengths: shoulder 0.100, elbow
0.210, far end 0.383. The bend is put back by the clips. A clip copied from the original that
does not key the forearm plays with a straight arm.

THE HANDS. The fist is the far 14 per cent of the arm (x 0.344 to 0.383), bare skin, 111 mm
square as the original's hand is, and nothing past the wrist is wider than it: the cuff ends at
0.312 and a bare wrist 90 mm across joins them. `ViewmodelArmAuthor` sizes a cut arm by that
fist. Its top is 0.0555 above the arm bone, where a carried tsinelas is parked.

HOW A FACE FINDS ITS PAINT. A piece is tagged with a group ("head", "torso", "armL" ...) and each
of its faces that squarely faces one of the group's views goes to that view's island, at the
place it sits in model space. Loose pieces take a flat tone instead.
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
import author_character_redesign_lola_pacing_textures as tex  # noqa: E402  the island layout
import author_character_redesign_lola_pacing_clips as clips  # noqa: E402  her own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/character-female-d.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/lola_pacing")
OUT = os.path.join(FOLDER, "lola_pacing-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/lola_pacing/redesign-20261006/lola_pacing_redesign.blend")
NAME = "lola_pacing-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# TWO BONES THE CLASSIC RIG DOES NOT HAVE: an elbow in each arm, a child of the arm bone, appended
# AFTER the seven so their indices and names do not move, exactly as the heroes' are. A clip that
# does not key them leaves the arm straight. Hers sits where the narrow sleeve meets its bell.
ELBOW_X = tex.ELBOW
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
# THE HEAD IS 84 PER CENT OF THE ORIGINAL'S (owner, 2026-10-05: "smaller heads are better").
# Everything is BUILT and PAINTED at the original's size, and only after the UVs are resolved is
# every vertex that rides the `head` bone drawn in toward the head JOINT (`build`).
HEAD_SCALE = 0.84
if "--head-scale" in sys.argv:          # for the one comparison picture at full size; not the rule
    HEAD_SCALE = float(sys.argv[sys.argv.index("--head-scale") + 1])
HEAD_JOINT = Vector((0.0, 0.0024, 0.3432))   # the `head` bone of character-female-d.glb, in Blender space
ORIGINAL_TOP = 0.7755    # the top of her bun (head-mesh bounds)
HEIGHT = HEAD_JOINT.z + (ORIGINAL_TOP - HEAD_JOINT.z) * HEAD_SCALE    # 0.706 at 0.84
SHARP_ANGLE = 40.0       # degrees: a chamfer is a hard edge, a soft corner or a taper is one form
TRIANGLE_BUDGET = 6000   # the original is 885

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
        instead.
        """
        bm = self.bm
        n = len(rings[0])
        count = len(rings)
        if params is None:
            params = [i / float(max(count - 1, 1)) for i in range(count)]
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

        # Whichever way the rings were wound, the shell faces out.
        bmesh.ops.recalc_face_normals(bm, faces=faces)
        self.pieces.append((name, sum(len(f.verts) - 2 for f in faces)))
        return faces

    def block(self, name, bone, u, v, stations, mapping, chamfer, move=None, caps=(True, True)):
        """A chamfered block through `stations` = [(centre, hu, hv)], its two ends chamfered too.

        Two stations of one size is a box. Different sizes taper it; moving the second centre
        off the axis leans it. `move` is applied to every point.
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
        """Make a piece soft: `weights(position)` gives [(bone, weight)] for each of its vertices.

        The cast is rigid, one bone a vertex (Voxel_Person_Guide.md), and so is this model except
        where cloth lies ACROSS two bones. Owner, 2026-10-05, on Dante: "make it so the clothes
        will bend/distort to follow his body".
        """
        for f in faces:
            for v in f.verts:
                self.blend[v] = weights(v.co)

    def wedge(self, name, bone, base, tip, base_half, tip_half, mapping, chamfer, across=X):
        """A block standing on `base` and narrowing to `tip`: a hair lock."""
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
# THE HEAD. The original's box (0.343 to 0.661, 0.320 wide, its face at y -0.158), in the `shaped`
# carving the owner picked on Dante: cheek fullness, soft corners, a jaw rounded in to the chin,
# every ring a superellipse of exponent 3.4 or more so the box wins.
#     rows are (z, half width, depth to the front, depth to the back, exponent)
# ---------------------------------------------------------------------------
HEAD_N = 24
HEAD_CY = 0.0024
HEAD_ROWS = [
    # Fullest at the cheeks (0.166 against 0.160 at the temples, the original's own half width),
    # narrowing gently to the crown. THE JAW ROUNDS IN GENTLY AND CLOSES LOW, as Amihan's and
    # Phaister's do and for their reason: she has no collar round her jaw to hide a hard turn
    # under, and in the toon shader a jaw that turns under sharply catches the shadow band as
    # dark wedges. The tight closing ring is from 0.349 down, on the shoulders.
    # THE FACE IS ONE FLAT PLANE. From 0.392 (under her mouth, whose foot is at 0.402) to 0.600
    # (the hairline is at 0.554, under her roll) the front depth is ONE value, 0.158. No brow
    # ledge, no cheek standing proud. Cheek fullness lives only in the WIDTH.
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
FACE_Y = HEAD_CY - 0.158     # the flat face plane


def super_point(t, z, rx, rf, rb, e):
    c, s = math.cos(t), math.sin(t)
    a = math.copysign(abs(c) ** (2.0 / e), c)
    b = math.copysign(abs(s) ** (2.0 / e), s)
    return Vector((a * rx, HEAD_CY + b * (rb if b >= 0 else rf), z))


def super_ring(z, rx, rf, rb, e, n=HEAD_N):
    return [super_point(2.0 * math.pi * j / n, z, rx, rf, rb, e) for j in range(n)]


def build_head(part):
    # Only the flat front takes the drawing (her face). Every other face of the head is flat
    # skin, and the faces that turn under at the jaw are the SAME skin, not a shade (no dark
    # facets on the lower face).
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], proj_square("head", "skin", "skin", 0.84))
    # NO NOSE. HER EARS, as the original has them: an eight-sided block each side, 94 mm tall,
    # standing 67 mm out. Flat skin, nothing drawn or cut in them (v01's sunk hollow read as a stray pixel).
    for s in (1, -1):
        part.block("ear", "head", Y, Z, [((s * 0.150, 0.044, 0.4606), 0.047, 0.046), ((s * 0.2268, 0.044, 0.4606), 0.036, 0.034)],
                   flat("skin"), 0.014)
        # HER EARRINGS: a white square stud under each ear, set cornerwise as the original's is
        # (a 35 mm box turned 45 degrees), 40 mm tall. It hangs 8 mm further out than the
        # original's, because her cheeks are 6 mm fuller here and half hid it (v02)
        u = Vector((1, 1, 0)).normalized()
        v = Vector((-1, 1, 0)).normalized()
        part.block("earring", "head", u, v, [((s * 0.1940, 0.0203, 0.3934), 0.0175, 0.0175), ((s * 0.1940, 0.0203, 0.4334), 0.0175, 0.0175)],
                   tones("white", "white", "white_shade"), 0.005)


# ---------------------------------------------------------------------------
# THE HAIR, BUILT AS THE HEROES' IS: chunky pieces in layers, each one a block with real depth,
# three tones by piece and by facing, nothing drawn and no shine (rule 3). Amihan's is wedge
# locks over layered slabs; Phaister's is curls stacked in tiers, each swelling to its foot and
# turning under. Hers borrows both ways of building and neither design: it is an older woman's
# SET hair, swept up off the neck into the bun.
#   under everything, a cap over the top of her head and a shell down the sides and back
#   THE FRINGE: five locks swept across her brow from a parting over her left eye, the middle
#       one the fullest, the two outer ones the longest, hung from one puff of hair above them
#   SET CURLS: two tiers before each ear, two behind it, and two rows across the back of her
#       head (three wide curls over four smaller ones at the nape), each tier tucked inside the
#       one above so the back steps down in shingles
#   THE BUN, her silhouette: three wound coils at the original's size and place
#   HER COMB: a red payneta standing in the hair in front of the bun
# (v01 to v04 had a three-lobed roll over the brow and upright slabs on the back and sides; the
# slabs read as planks.)
# ---------------------------------------------------------------------------
LIT = tones("hair_lit", "hair", "hair_dark")
DARK = tones("hair", "hair_dark", "hair_under")
DEEP = tones("hair_dark", "hair_under", "hair_under")
ROSE_T = tones("rose_lit", "rose", "rose_dark")
HAIRLINE = tex.HAIRLINE
CAP = (0.171, 0.168, 0.172, 4.2)       # the hair's half width, depth to the front, depth to the back, exponent
#   where the shell's front edge is on the side of her head, by height: (z, y). Behind the ear it
#   hangs to the nape; above the ear it slants forward to the temple.
SIDE_EDGE = [(0.3932, 0.0274), (0.4360, 0.0274), (0.4380, -0.0551), (0.4579, -0.1109), (0.5092, -0.1276), (0.5560, -0.1276)]
BUN_AT = Vector((0.0, 0.165, 0.650))
BUN_AXIS = Vector((0.0, 0.55, 0.835)).normalized()
#   three coils along the bun's axis: (distance along it, radius) for the foot, the swell and the top of each
BUN_COILS = [
    ("bun-base", DARK, [(-0.088, 0.082), (-0.050, 0.124), (-0.012, 0.118)]),
    ("bun-mid", LIT, [(-0.024, 0.110), (0.022, 0.128), (0.062, 0.112)]),
    ("bun-top", DARK, [(0.048, 0.100), (0.082, 0.104), (0.112, 0.062)]),
]
#   THE FRINGE, her left to her right: (where it hangs from, its foot, half size at the top, at the foot, tone)
FRINGE_LOCKS = [
    ((0.128, -0.172, 0.628), (0.160, -0.166, 0.522), (0.034, 0.018), (0.015, 0.010), DARK),
    ((0.066, -0.180, 0.632), (0.094, -0.182, 0.556), (0.034, 0.020), (0.018, 0.011), LIT),
    ((-0.004, -0.184, 0.634), (-0.030, -0.188, 0.542), (0.040, 0.022), (0.021, 0.012), LIT),
    ((-0.074, -0.180, 0.632), (-0.102, -0.182, 0.534), (0.034, 0.020), (0.017, 0.011), DARK),
    ((-0.130, -0.172, 0.628), (-0.160, -0.166, 0.516), (0.032, 0.018), (0.014, 0.010), LIT),
]
#   SET CURLS, for her LEFT side (+x); the right is the mirror. Each strand is tiers of boxes
#   (x from, x to, y from, y to, z foot, z top), the top tier first.
SIDE_CURLS = [
    ("curl-temple", LIT, [(0.158, 0.204, -0.134, -0.058, 0.470, 0.566), (0.162, 0.199, -0.122, -0.066, 0.412, 0.482)]),
    ("curl-behind-ear", DARK, [(0.158, 0.204, 0.098, 0.172, 0.462, 0.566), (0.162, 0.199, 0.104, 0.166, 0.396, 0.474)]),
]
#   and across the back: (x from, x to, y from, y to, z foot, z top)
BACK_CURLS = [
    ("curl-back-high", LIT, [(-0.156, -0.054, 0.150, 0.204, 0.466, 0.566), (-0.050, 0.050, 0.152, 0.210, 0.458, 0.566),
                             (0.054, 0.156, 0.150, 0.204, 0.466, 0.566)]),
    ("curl-nape", DARK, [(-0.146, -0.074, 0.150, 0.198, 0.396, 0.478), (-0.070, -0.002, 0.152, 0.202, 0.388, 0.472),
                         (0.002, 0.070, 0.152, 0.202, 0.388, 0.472), (0.074, 0.146, 0.150, 0.198, 0.396, 0.478)]),
]
CURL_SWELL = 0.006     # how far a curl's foot stands out past its box
HAIR_FOLLOW = 0.5      # how much of the shoulders a curl below the jaw keeps when the head turns


def _edge_y(z):
    return _table(SIDE_EDGE, z)


def hair_weights(co):
    """Hair above the jaw is the head's. A curl hanging below it gives up to half of itself to the
    torso, so a turned head does not sweep a rigid curl through her shoulder (rule 7)."""
    w = HAIR_FOLLOW * _ramp(0.400, 0.372, co.z)
    return [("head", 1.0 - w), ("torso", w)]


def build_hair(part):
    rx, rf, rb, e = CAP
    # THE CAP over the top of her head, from the hairline up, all the way round
    cap = [(HAIRLINE, rx - 0.002, rf - 0.004, rb, e), (0.600, rx, rf, rb, e), (0.646, 0.164, 0.160, 0.166, 3.8),
           (0.6713, 0.138, 0.132, 0.140, 3.4)]
    part.loft("hair-cap", "head", [super_ring(*row) for row in cap], DARK)

    # THE SHELL down the sides and the back of her head, open at the front: what shows between
    # the curls is hair. Each ring is its outside from one front edge round the back to the
    # other, and then its inside (just under the skull's surface) back again.
    def shell(z):
        v = (_edge_y(z) - HEAD_CY) / (rb if _edge_y(z) >= HEAD_CY else rf)
        start = math.asin(math.copysign(abs(v) ** (e / 2.0), v))
        steps = 14
        angles = [start + (math.pi - 2.0 * start) * k / steps for k in range(steps + 1)]
        outer = [super_point(t, z, rx, rf, rb, e) for t in angles]
        inner = [super_point(t, z, rx - 0.016, rf - 0.016, rb - 0.016, e) for t in reversed(angles)]
        return outer + inner

    heights = [0.3932, 0.4360, 0.4380, 0.4579, 0.5092, 0.5560]
    part.loft("hair-shell", "head", [shell(z) for z in heights], DEEP)

    # THE FRINGE. One puff of hair across the front of her crown, the locks hung from under it.
    part.block("hair-puff", "head", Y, Z, [((-0.158, -0.140, 0.630), 0.026, 0.026), ((-0.060, -0.158, 0.640), 0.032, 0.032),
                                           ((0.040, -0.160, 0.642), 0.034, 0.033), ((0.158, -0.140, 0.630), 0.026, 0.026)], DARK, 0.013)
    for base, tip, top_half, foot_half, paint in FRINGE_LOCKS:
        part.wedge("hair-fringe", "head", base, tip, top_half, foot_half, paint, 0.008)

    def stack(name, paint, tiers, s=1):
        for x0, x1, y0, y1, foot, top in tiers:
            lo, hi = sorted((s * x0, s * x1))
            cx, cy = 0.5 * (lo + hi), 0.5 * (y0 + y1)
            hx, hy = 0.5 * (hi - lo), 0.5 * (y1 - y0)
            # from its top (open, inside the tier above) out to the swell just above its foot,
            # then turned under and closed
            rings = [rect_ring((cx, cy, top), X, Y, hx - 0.004, hy - 0.004, 0.012),
                     rect_ring((cx, cy, foot + 0.018), X, Y, hx + CURL_SWELL, hy + CURL_SWELL, 0.016),
                     rect_ring((cx, cy, foot), X, Y, hx - 0.008, hy - 0.008, 0.010)]
            faces = part.loft("hair-" + name, "head", rings, paint, caps=(False, True))
            part.bend(faces, hair_weights)

    for name, paint, tiers in SIDE_CURLS:
        for s in (1, -1):
            for k, tier in enumerate(tiers):
                stack(name, (paint, DARK if paint is LIT else DEEP)[k], [tier], s)
    for name, paint, tiers in BACK_CURLS:
        for k, tier in enumerate(tiers):
            # the curls of a row alternate, so each reads against its neighbour
            stack(name, paint if k % 2 == 0 else (DARK if paint is LIT else DEEP), [tier])

    # THE BUN: three wound coils on an axis tipped back, each one swelling and turning under
    # before the next starts inside it, so it reads as hair wound round and not as a ball.
    w = BUN_AXIS
    u = X
    v = w.cross(u).normalized()
    sides = 12
    for name, paint, profile in BUN_COILS:
        rings = []
        for along, radius in profile:
            at = BUN_AT + w * along
            rings.append([at + (u * math.cos(2.0 * math.pi * k / sides) + v * math.sin(2.0 * math.pi * k / sides)) * radius
                          for k in range(sides)])
        part.loft("hair-" + name, "head", rings, paint, caps=(name == "bun-base", True))

    # HER COMB, a payneta: a low red arch standing in the hair in front of the bun, its teeth in
    # the hair. 140 mm across at its foot (v06 was 124 and was lost in the hair from the front).
    part.block("comb", "head", X, Y, [((0.0, 0.028, 0.652), 0.070, 0.009), ((0.0, 0.040, 0.694), 0.064, 0.008),
                                      ((0.0, 0.050, 0.714), 0.040, 0.007)], ROSE_T, 0.010)


# ---------------------------------------------------------------------------
# THE TORSO AND THE JACKET. One taupe block with a flat front, and on it, in layers: the white
# blouse in the V, the two piped lapels, her brooch, the
# waistband with her fan tucked in it, and the short flared skirt of the jacket with red piping
# at its hem.
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.3432)
TORSO_CY = 0.0288              # the torso bone; the block is as deep behind it as in front
TORSO_HALF = 0.126             # the original's: 0.252 wide to 0.253, drawing in to 0.200 at the shoulders
TORSO_FRONT = TORSO_CY - tex.FRONT_Y       # 0.1092: depth from the bone to the flat front
ARM_Y, ARM_Z = 0.01725, 0.28775            # the line her arms are built along (the arm bone)

SKIRT_TOP = tex.WAIST
SKIRT_FOLLOW = 0.55                    # how much of a leg's swing the hem takes
SKIRT_TOP_R = (0.139, 0.1234, 0.1233)  # half width, depth to the front, depth to the back (the original's rings)
SKIRT_HEM_R = (0.1668, 0.1481, 0.1480)
SKIRT_BAND = 0.013                     # the red piping at the hem
SKIRT_MID = 0.5 * (SKIRT_TOP + tex.HEM + SKIRT_BAND)


def skirt_f(z):
    """How far down the flare a height is: 0 at the waist, 1 at the hem (a straight cone)."""
    return _table([(tex.HEM, 1.0), (SKIRT_TOP, 0.0)], z)


def skirt_weights(co):
    down = _ramp(SKIRT_TOP - 0.010, tex.HEM, co.z) ** 0.8
    leg = SKIRT_FOLLOW * down
    left = _smooth(_ramp(-0.070, 0.070, co.x))
    return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]


def build_torso(part):
    cloth = proj_square("torso", "taupe", "taupe_deep", 0.90, ends=(2, "taupe_mid"))
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, TORSO_Z[0]), TORSO_HALF, (TORSO_FRONT, 0.1091)),
                                        ((0, TORSO_CY, 0.2531), TORSO_HALF, (TORSO_FRONT, 0.1091)),
                                        ((0, TORSO_CY, TORSO_Z[1]), 0.104, (TORSO_FRONT, 0.0861))], cloth, 0.014)
    # the seat of her trousers, between the legs under the jacket's skirt
    part.block("pelvis", "torso", X, Y, [((0, TORSO_CY, 0.120), 0.060, 0.070), ((0, TORSO_CY, 0.180), 0.060, 0.070)], flat("taupe_deep"), 0.0)
    # her neck, for when the head tips back and the chin lifts off the blouse
    neck = part.block("neck", "torso", X, Y, [((0, HEAD_CY, 0.326), 0.050, 0.048), ((0, HEAD_CY, 0.354), 0.048, 0.046)],
                      flat("skin_shade"), 0.008)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.332, 0.350, co.z)), ("head", 0.7 * _ramp(0.332, 0.350, co.z))])
    # HER BLOUSE, the V of white in the jacket's neck: a plate of its own, 1 mm proud, from the
    # point of the V to her chin.
    part.block("blouse", "torso", X, Y, [((0, tex.FRONT_Y + 0.020, tex.WAIST + 0.012), 0.0030, (0.0210, 0.020)),
                                         ((0, tex.FRONT_Y + 0.020, 0.3440), 0.0990, (0.0210, 0.020))], flat("white"), 0.0)

    # HER LAPELS: a wide flap each side of the V, 7 to 10 mm proud of the jacket, widest at 0.253,
    # narrowing to the shoulder. A paler step of the taupe, so the jacket reads in layers.
    for s in (1, -1):
        def at(t, s=s):
            return (s * 0.0999 * t, tex.FRONT_Y - 0.001, tex.WAIST + (TORSO_Z[1] - tex.WAIST) * t)

        part.block("lapel", "torso", X * s, Y, [(at(0.12), (0.001, 0.010), (0.006, 0.004)), (at(0.32), (0.001, 0.054), (0.009, 0.004)),
                                               (at(0.62), (0.001, 0.046), (0.009, 0.004)), (at(0.94), (0.001, 0.014), (0.007, 0.004))],
                   facing(AHEAD, "taupe_lit", "taupe_mid"), 0.003)
        # red PIPING down the lapel's outer edge, 2 mm prouder than the lapel
        part.block("lapel-piping", "torso", X * s, Y, [(at(0.12), (-0.006, 0.0125), (0.008, 0.004)), (at(0.32), (-0.049, 0.0565), (0.011, 0.004)),
                                                      (at(0.62), (-0.041, 0.0485), (0.011, 0.004)), (at(0.94), (-0.009, 0.0165), (0.009, 0.004))],
                   facing(AHEAD, "rose", "rose_dark"), 0.0)
    # HER BROOCH, pinned on the blouse at her throat: one red stone with its corners cut.
    # (v05 also laid a round white collar leaf each side of it; the three together read as a bow
    # tie. The collar is gone and the lapels carry piping instead.)
    part.block("brooch", "torso", X, Z, [((0, tex.FRONT_Y - 0.001, 0.3150), 0.0150, 0.0165), ((0, tex.FRONT_Y - 0.012, 0.3150), 0.0150, 0.0165)],
               ROSE_T, 0.006)

    # THE SKIRT OF THE JACKET, from the waist to 0.138, flaring. Its hem is a band of red piping
    # 4 mm proud. Toward the hem each side takes up to SKIRT_FOLLOW of its own leg, and across
    # the middle the two legs are mixed (rule 7).
    rounds = []
    for k in range(24):
        a = 2.0 * math.pi * k / 24.0
        c, sn = math.cos(a), math.sin(a)
        rounds.append((math.copysign(abs(sn) ** 0.45, sn), math.copysign(abs(c) ** 0.45, c)))    # (across, fore): k 0 is dead ahead
    rounds.append(rounds[0])
    hem = tex.HEM
    sections = []
    for px, py in rounds:
        def at(z, inset, px=px, py=py):
            f = skirt_f(z)
            r = [SKIRT_TOP_R[k] + (SKIRT_HEM_R[k] - SKIRT_TOP_R[k]) * f - inset for k in range(3)]
            return Vector((px * r[0], TORSO_CY - py * (r[1] if py > 0 else r[2]), z))

        sections.append([at(SKIRT_TOP + 0.006, 0.016), at(SKIRT_TOP, 0.0), at(SKIRT_MID, 0.0), at(hem + SKIRT_BAND, 0.0),
                         at(hem + SKIRT_BAND + 0.0005, -0.004), at(hem, -0.004), at(hem + 0.003, 0.010), at(SKIRT_MID, 0.012)])
    cloth = proj_square("torso", "taupe", "taupe_deep", 0.86)

    def skirt_paint(i, j):
        return (flat("taupe_lit"), cloth, cloth, flat("rose_lit"), flat("rose"), flat("rose_dark"), flat("taupe_deep"),
                flat("taupe_deep"))[j]

    skirt = part.loft("jacket-skirt", "torso", sections, skirt_paint, caps=(False, False))
    part.bend(skirt, skirt_weights)

    # THE WAISTBAND, a darker step of the taupe, standing 5 mm proud of the skirt's top
    band = []
    for px, py in rounds:
        def at(z, out, px=px, py=py):
            r = [SKIRT_TOP_R[k] + out for k in range(3)]
            return Vector((px * r[0], TORSO_CY - py * (r[1] if py > 0 else r[2]), z))

        band.append([at(SKIRT_TOP + 0.014, -0.014), at(SKIRT_TOP + 0.012, 0.005), at(SKIRT_TOP - 0.008, 0.006), at(SKIRT_TOP - 0.010, -0.004)])
    part.loft("waistband", "torso", band, lambda i, j: (flat("taupe_mid"), flat("taupe_deep"), flat("taupe_deep"), flat("taupe_deep"))[j],
              caps=(False, False))

    # HER FAN (a pamaypay, folded), tucked in the waistband at her left hip: red leaves between
    # two pale guards, a dark rivet end. Her idle fans herself with her hand; this is why.
    front = TORSO_CY - SKIRT_TOP_R[1] - 0.012
    foot, head = Vector((0.090, front - 0.004, 0.170)), Vector((0.126, front + 0.006, 0.262))     # v05's stood up over her chest and read as a thermometer
    lean = (head - foot).normalized()
    across = (X - lean * X.dot(lean)).normalized()
    deep = lean.cross(across).normalized()
    part.block("fan", "torso", across, deep, [(tuple(foot), 0.0085, 0.0075), (tuple(foot + lean * 0.030), 0.0100, 0.0080),
                                             (tuple(head), 0.0170, 0.0085)], ROSE_T, 0.004)
    part.block("fan-guard", "torso", across, deep, [(tuple(foot + lean * 0.006 - deep * 0.0015), 0.0040, 0.0090),
                                                   (tuple(head + lean * 0.004 - deep * 0.0015), 0.0052, 0.0100)],
               tones("white", "white_shade", "white_deep"), 0.0)
    part.block("fan-rivet", "torso", across, deep, [(tuple(foot - lean * 0.008), 0.0070, 0.0085), (tuple(foot + lean * 0.004), 0.0070, 0.0085)],
               tones("shoe_lit", "shoe", "shoe_deep"), 0.003)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as this rig rests. `s` is +1 for her left, -1 for her right.
# From the shoulder out: the sleeve to the elbow (0.210), on the arm bone; then, on the elbow
# bone, the BELL: one cone opening from the elbow to a mouth 161 mm across at 0.272, so it reads
# as a sleeve (v01 to v04 kept the original's short wide ring and it read as a ring), red
# piping round its mouth, the white cuff inside it to 0.312, her bare wrist, and her fist from
# 0.344 to 0.383.
# ---------------------------------------------------------------------------

def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    arm_block(part, "sleeve", bone, s, [(0.098, (0.050, 0.052)), (0.150, (0.054, 0.054)), (0.218, (0.057, 0.057))],
              proj_square(group, "taupe", "taupe_mid", 0.90, ends=(0, "taupe")), 0.012)   # its end is her elbow when the arm folds
    # the bell starts 8 mm INSIDE the elbow and is wider than the sleeve there, so when the elbow
    # folds it turns round the sleeve's end like a cap and no gap opens on the outside of the bend
    arm_block(part, "sleeve-bell", fore, s, [(tex.BELL[0] - 0.008, (0.060, 0.060)), (tex.BELL[1] - 0.004, (0.0795, 0.0795))],
              proj_square(group, "taupe", "taupe_mid", 0.88, ends=(0, "taupe_deep")), 0.010)
    arm_block(part, "sleeve-piping", fore, s, [(tex.BELL[1] - 0.014, (0.0795, 0.0795)), (tex.BELL[1], (0.0815, 0.0815))],
              tones("rose_lit", "rose", "rose_dark"), 0.006)
    arm_block(part, "cuff", fore, s, [(tex.CUFF[0] - 0.010, (0.0610, 0.0610)), (tex.CUFF[1], (0.0610, 0.0610))],
              proj_square(group, "white", "white_shade", 0.90, ends=(0, "white_shade")), 0.010)
    # her wrist, bare: a flat tone, narrower than the fist
    arm_block(part, "wrist", fore, s, [(0.300, (0.045, 0.045)), (tex.WRIST + 0.004, (0.045, 0.045))], tones("skin", "skin", "skin_shade"), 0.0)
    # THE FIST: a plain block, the far 14 per cent of the arm, the original hand's own 111 mm
    # square. Its fingers and thumb are drawn on its flat front and back only.
    arm_block(part, "hand", fore, s, [(tex.WRIST, (0.0555, 0.0555)), (tex.HAND_END, (0.0555, 0.0555))],
              proj_square(group, "skin", "skin_shade", 0.93), 0.013)


# ---------------------------------------------------------------------------
# THE LEGS. Short, the cast's: a taupe trouser leg, a flared hem cut on a slant (high over the
# instep, low over the heel), and HER SLIPPERS: a closed velvet house slipper in her red on a
# dark sole, a white edge round its mouth, her bare heel behind it.
# ---------------------------------------------------------------------------
LEG_X = 0.0829
LEG_CY = 0.0288


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    part.block("trouser", bone, X, Y, [((s * LEG_X, LEG_CY, tex.TROUSER_FOOT - 0.004), 0.0532, 0.0738),
                                       ((s * LEG_X, LEG_CY, 0.182), 0.0452, 0.0738)],
               proj_square(group, "taupe", "taupe_deep", 0.90, ends=(2, "taupe_deep")), 0.018)

    # the flared hem: its foot runs from 0.076 over the instep down to 0.040 behind the heel
    def slanted(cy, hu, hv, cut, front, back, lift=0.0, inset=0.0):
        ring = rect_ring((s * LEG_X, cy, 0.0), X, Y, hu - inset, hv - inset, max(cut - 0.4 * inset, 0.004))
        y0, y1 = cy - hv, cy + hv
        return [Vector((p.x, p.y, lift + front + (back - front) * (p.y - y0) / (y1 - y0))) for p in ring]

    rings = [slanted(0.0294, 0.0653, 0.0891, 0.030, 0.1094, 0.0777, 0.004, 0.014),
             slanted(0.0294, 0.0653, 0.0891, 0.030, 0.1094, 0.0777),
             slanted(0.0230, 0.0725, 0.0990, 0.034, 0.0758, 0.0405),
             slanted(0.0230, 0.0725, 0.0990, 0.034, 0.0758, 0.0405, 0.003, 0.012)]
    part.loft("trouser-hem", bone, rings, lambda i, j: (flat("taupe_lit"), tones("taupe_lit", "taupe_mid", "taupe_deep"), flat("taupe_deep"))[i],
              caps=(False, False))

    def station(y, half, z0, z1, cut):
        return rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)

    # the sole, 207 mm long, a little wider than the slipper all round
    sole = [(-0.1050, 0.046, 0.0, 0.014, 0.004), (-0.0880, 0.062, 0.0, 0.014, 0.005), (-0.0400, 0.068, 0.0, 0.014, 0.005),
            (0.0800, 0.064, 0.0, 0.014, 0.005), (0.1020, 0.050, 0.0, 0.014, 0.004)]
    part.loft("slipper-sole", bone, [station(*row) for row in sole], tones("shoe_lit", "shoe", "shoe_deep"))
    # the slipper's upper, over her toes and instep: low at the toe, rising to its mouth
    upper = [(-0.1000, 0.038, 0.012, 0.038, 0.010), (-0.0840, 0.056, 0.012, 0.050, 0.014), (-0.0400, 0.063, 0.012, 0.060, 0.018),
             (0.0060, 0.063, 0.012, 0.062, 0.018)]
    part.loft("slipper", bone, [station(*row) for row in upper], tones("rose_lit", "rose", "rose_dark"))
    # the white edge round its mouth
    part.block("slipper-edge", bone, X, Z, [((s * LEG_X, 0.0000, 0.0380), 0.0655, 0.0265), ((s * LEG_X, 0.0120, 0.0380), 0.0655, 0.0265)],
               tones("white", "white", "white_shade"), 0.018)
    # her heel, bare, behind the slipper
    part.block("heel", bone, X, Z, [((s * LEG_X, 0.0060, 0.0340), 0.050, 0.022), ((s * LEG_X, 0.0900, 0.0340), 0.046, 0.022)],
               tones("skin", "skin", "skin_shade"), 0.012)


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
    """character-female-d.glb with its meshes swapped: skeleton, bind matrices and clips copied
    untouched, two elbows appended, and the five locomotion clips replaced by her own."""
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

    # her own locomotion clips, in place of the ones copied across (the clips script)
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (lola_pacing)"}
    gltf["extras"] = {"prototype": "character-redesign-20261006", "replaces": "nothing; not loaded by the game"}
    gltf["images"] = [{"uri": tex.ATLAS_NAME, "name": NAME + "-atlas"}]
    gltf["textures"] = [{"sampler": 0, "source": 0, "name": NAME + "-atlas"}]
    # Bilinear with mips: the stock colormap's nearest filter is for flat palette cells and would
    # stair-step a drawn line.
    gltf["samplers"] = [{"magFilter": 9729, "minFilter": 9987}]
    # the stock material carries a texture-transform extension for the shared colormap; hers is a
    # plain painted atlas, as the heroes' are
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
    print("triangles %d  (budget %d, the original is 885)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if abs(hi.z - HEIGHT) > 0.008:
        raise SystemExit("the top is at %.4f, not the %.4f of the original's bun on a head at %.2f" % (hi.z, HEIGHT, HEAD_SCALE))
    if tris > TRIANGLE_BUDGET:
        raise SystemExit("%d triangles is over the budget of %d" % (tris, TRIANGLE_BUDGET))
    # THE HAND. The arm must run along x; the fist must be the far end of the forearm bone, and in
    # the far 14 per cent of the arm nothing may be wider than it (`ViewmodelArmAuthor.Extract`
    # sizes a first-person arm by what it finds there); its top must sit where a carried
    # tsinelas is parked (0.045 to 0.066 above the arm bone).
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
            raise SystemExit("the fist is under half the arm's widest section; the first-person cut would not size by it")
        if not 0.045 <= top <= 0.066:
            raise SystemExit("the hand's top moved; a carried tsinelas would float or sink")
        if abs(far - tex.HAND_END) > 0.003:
            raise SystemExit("the hand's far end moved off the original's reach")


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
        # that shares the head with the torso (the top of the neck) by the same share.
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
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_lola_pacing_textures.py")
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
