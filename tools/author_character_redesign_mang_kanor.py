"""Build the Mang Kanor redesign PROTOTYPE: Mang Kanor redrawn as if he were one of the heroes.

    py -3 tools/author_character_redesign_mang_kanor_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_mang_kanor.py
    ... -- --out other.glb     (writes there instead and leaves the .blend alone; needs the atlas beside it)
    ... -- --head-scale 1.0 --out other.glb     (a comparison copy with the head at another size)

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/mang_kanor/mang_kanor-redesign.glb
    ArtSource/mang_kanor/redesign-20261006/mang_kanor_redesign.blend
beside the atlas painted by the textures script.

WHY. Owner, 2026-10-06: the nine heroes were redesigned and are the game's models; the twelve
Classic street characters are REDRAWN AS IF THEY WERE HEROES. Three went first as the pattern
(bayan, bebang, lola_pacing) and were approved; this is one of the other nine, built to
docs/reports/character-redesign/classic-brief.md from a copy of Bayan's files (a man). It is
NOT the original with more detail: the same person (who he is, his age, his colours, his
recognisable pieces) DESIGNED IN THE HEROES' VISUAL LANGUAGE.

A PROTOTYPE. NOTHING IN THE GAME LOADS IT. The folder is outside Resources, no roster row points
here, and character-male-e.glb, person_mang_kanor.asset and every runtime script are not touched.

WHO HE IS. MANG KANOR ("mang" is how you address an older man). "Tricycle driver. He knows every
corner of this town by its potholes and he takes them at speed. Braking was never the strong
suit." (ConvertedCharacterSelect). "The neighbourhood tito: belly first, leaning back, wide and
rolling, arms out; a huffing jog" (GaitStyles.MangKanor). Fast for his age, 5/3/2
(GAME_OVERVIEW.md). No powers, no gear. A cheerful older man of the street, not a costume.

WHAT IS KEPT OF HIM (the original is character-male-e.glb wearing person_mang_kanor.asset): his
brown skin; dark brown hair with a point in the middle of the hairline; big ears; ROUND GLASSES
in a dark frame; a moustache; a white shirt; blue denim overalls with a strap over each
shoulder; dark arms below the white sleeve; brown shoes.
WHAT IS DESIGNED, THE HEROES' WAY (each is argued where it is built):
  * the heroes' ARM LENGTH (0.098 to 0.300; the stock rig's runs to 0.384) on the stock rig's
    wide, short legs, with a BELLY: his torso stands 22 mm further forward above the waistband
    than at the chest, and he keeps that width as his trait;
  * THE FACE in the heroes' hand: two upright ink blocks with ONE cut (the top edge falls to the
    outer corner: kind eyes), one thin stroke for a mouth. The eyes are drawn on the LENSES;
  * HIS GLASSES as designed pieces: two eight-sided plates in a dark bevelled frame, a bridge,
    hinge bars and arms back to his ears;
  * HIS MOUSTACHE as one designed block shape;
  * THE HAIR built as the heroes' is, and an older man's: combed back in thick locks that rise
    to a crest, the corners of the hairline gone back, GREY in his sideburns and moustache;
  * THE OVERALLS as real overalls: a stitched bib with a pocket and a pen in it, a brass button
    at each corner, straps, a high back, a waistband with a leather maker's patch, hip buttons,
    faded knees, back pockets, a paler turned cuff;
  * the dark arms as ARM SLEEVES, the sun sleeves a tricycle driver wears, with a grey grip band
    at the wrist and his fists bare;
  * three things that are HIS: a leather BELT BAG for the day's fares, worn in front; the
    tricycle's KEYS on a ring at his right hip; the pen in his bib pocket;
  * FOOTWEAR, decided deliberately: brown loafers with a strap across the instep on a cream
    sole, over white socks. A tito's shoes; nobody else in the cast wears them.
  Nothing of it is a hero's or another Classic's outfit. (Bayan wears braces and a belt over a
  green shirt; Kanor's straps are denim, part of a bibbed garment, over white.)

and still: a drawing only on a face that squarely faces its view (`proj_square`, `panel`); no
painted hair shine; cloth across two bones bends (`Part.bend`: the keys take the leg, the nape
of his hair and the top of his neck share the head and the torso); feet on zero; under 6,000
triangles. HE HAS NO COLLAR: a round-necked T-shirt, so nothing stands round the jaw for the
head to turn through.

THE HANDS, FOR THE GAME'S FIRST-PERSON ARMS (Editor/ViewmodelArmAuthor.cs cuts them from this
mesh and sizes a redesigned arm by its fist, the far 14 per cent of the arm's length):
  * the fist is a plain block at the FAR END of the forearm bone, x 0.232 to 0.300, 111 mm
    square, bare skin; the far 14 per cent (from x 0.272) is fist and nothing else;
  * the arm sleeve narrows to 93 mm at the wrist and its grip band ends at 0.229, so nothing
    wider than the fist is beyond the wrist; the widest thing on the arm is the sleeve's hem;
  * the fist's top is 0.0555 above the arm bone (`verify` holds it between 0.045 and 0.066,
    where a carried tsinelas is parked).
The arm is built straight along x (shoulder 0.100, elbow 0.178, end of the fist 0.300); the
stock rig bakes a bend into its mesh, and the clips bend this one at the elbow bone.

HOW A FACE FINDS ITS PAINT. A piece is tagged with a group ("head", "torso", "armL" ...) and each
of its faces that squarely faces one of the group's views goes to that view's island, at the
place it sits in model space. Loose pieces take a swatch, a panel or a flat tone instead.
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
import author_character_redesign_mang_kanor_textures as tex  # noqa: E402  the island layout
import author_character_redesign_mang_kanor_clips as clips  # noqa: E402  his own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/character-male-e.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/mang_kanor")
OUT = os.path.join(FOLDER, "mang_kanor-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/mang_kanor/redesign-20261006/mang_kanor_redesign.blend")
NAME = "mang_kanor-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# TWO BONES THE STOCK RIG DOES NOT HAVE: an elbow in each arm, a child of the arm bone, appended
# AFTER the seven so their indices and names do not move, exactly as the heroes' are. A clip that
# does not key them leaves the arm straight. His sits a little past the white sleeve's hem.
ELBOW_X = 0.178
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
# THE HEAD IS 84 PER CENT OF THE ORIGINAL'S (owner, 2026-10-05: "smaller heads are better", "yes
# rebuild the heads at 84"). Everything is BUILT and PAINTED at the original's size, and only
# after the UVs are resolved is every vertex that rides the `head` bone drawn in toward the head
# JOINT (`build`).
HEAD_SCALE = 0.84
# `-- --head-scale 1.0 --out file.glb` builds a comparison copy with the head at another size (the
# owner asked to be SHOWN both if 0.84 looks wrong on an adult; it is not a second design)
_ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
if "--head-scale" in _ARGS:
    HEAD_SCALE = float(_ARGS[_ARGS.index("--head-scale") + 1])
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))   # the `head` bone of character-male-e.glb, in Blender space
HAIR_TOP = 0.755                            # the tallest corner of his combed-back locks, before the head's scale (the stock rig's hair ends at 0.676)
HEIGHT = HEAD_JOINT.z + (HAIR_TOP - HEAD_JOINT.z) * HEAD_SCALE     # 0.694
SHARP_ANGLE = 40.0       # degrees: a chamfer is a hard edge, a soft corner or a taper is one form
TRIANGLE_BUDGET = 6000   # the original is 789
FIST_SHARE = 0.14        # ViewmodelArmAuthor: the fist is the far 14 per cent of the arm

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
# THE HEAD. THE HEROES' HEAD: Dante's `shaped` rows, number for number (owner, 2026-10-06: "head
# size stays the same as the heroes"). Cheek fullness, soft corners, a jaw rounded in to the
# chin, every ring a superellipse of exponent 3.4 or more so the box wins.
#     rows are (z, half width, depth to the front, depth to the back, exponent)
# ---------------------------------------------------------------------------
HEAD_N = 24
HEAD_CY = 0.001          # the middle of the box, front to back
HEAD_ROWS = [
    # THE FACE IS ONE FLAT PLANE: from 0.428 to 0.604 the front depth is ONE value, 0.165, and it
    # is within 3 mm of that down to 0.388, under his mouth. No brow ledge, no cheek standing
    # proud: in the game's shader any step there draws a level line across the face.
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
FACE_Y = HEAD_CY - 0.165     # the face plane


def super_point(t, z, rx, rf, rb, e):
    """A point of a superellipse ring. `t` in radians: 0 is his left side, -90 degrees his front."""
    c, s = math.cos(t), math.sin(t)
    a = math.copysign(abs(c) ** (2.0 / e), c)
    b = math.copysign(abs(s) ** (2.0 / e), s)
    return Vector((a * rx, HEAD_CY + b * (rb if b >= 0 else rf), z))


def super_ring(z, rx, rf, rb, e, n=HEAD_N):
    return [super_point(2.0 * math.pi * j / n, z, rx, rf, rb, e) for j in range(n)]


FRAME_T = tones("frame_lit", "frame", "frame")


def build_head(part):
    # Only the flat front takes the drawing (his mouth and the shadows). Every other face of the
    # head is flat skin, and the faces that turn under at the jaw are the SAME skin, not a shade.
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], proj_square("head", "skin", "skin", 0.84))
    # NO NOSE. HIS EARS are big and stand out: they are his, and they carry his glasses.
    for s in (1, -1):
        part.block("ear", "head", Y, Z, [((s * 0.166, 0.018, 0.466), 0.032, 0.044), ((s * 0.222, 0.028, 0.466), 0.023, 0.033)],
                   tones("skin_lit", "skin", "skin_shade"), 0.010)

    # HIS GLASSES. Each lens is an eight-sided plate standing 11 mm off the face (the original's
    # lenses are octagons): its bevelled rim is the dark frame, its flat front is the glass, and
    # his eye is drawn on that front (`tex.paint_lens`). A bridge between them, a hinge bar out
    # to each temple and an arm back to each ear. All of it rides the head bone.
    cx, cz, hw, hh = tex.LENS_AT
    for s in (1, -1):
        window = (s * (cx - hw), s * (cx + hw), cz - hh, cz + hh)
        rings = [rect_ring((s * cx, FACE_Y + 0.006, cz), X, Z, hw, hh, 0.024),
                 rect_ring((s * cx, FACE_Y - 0.006, cz), X, Z, hw, hh, 0.024),
                 rect_ring((s * cx, FACE_Y - 0.011, cz), X, Z, hw - 0.0056, hh - 0.0056, 0.0205)]
        part.loft("glasses-lens", "head", rings, panel("lens", 0, 2, window, AHEAD, "frame"))
        part.block("glasses-hinge", "head", Y, Z, [((s * (cx + hw - 0.006), FACE_Y - 0.003, cz + 0.010), 0.0045, 0.0055),
                                                    ((s * 0.186, FACE_Y - 0.003, cz + 0.010), 0.0045, 0.0055)], FRAME_T, 0.0015)
        part.block("glasses-arm", "head", X, Z, [((s * 0.182, FACE_Y - 0.004, cz + 0.010), 0.0045, 0.0055),
                                                  ((s * 0.182, 0.020, cz + 0.008), 0.0045, 0.0055)], FRAME_T, 0.0015)
    part.block("glasses-bridge", "head", Y, Z, [((-(cx - hw) - 0.004, FACE_Y - 0.004, cz + 0.012), 0.0045, 0.0055),
                                                 ((cx - hw + 0.004, FACE_Y - 0.004, cz + 0.012), 0.0045, 0.0055)], FRAME_T, 0.0015)

    # HIS MOUSTACHE, GREY: one designed shape, two thick halves that droop a little at their ends
    mz = tex.MOUSTACHE_Z
    for s in (1, -1):
        part.block("moustache", "head", Z, Y, [((s * -0.008, FACE_Y - 0.004, mz + 0.001), 0.0125, 0.0075),
                                               ((s * 0.030, FACE_Y - 0.004, mz), 0.0115, 0.0070),
                                               ((s * 0.058, FACE_Y - 0.002, mz - 0.010), 0.0050, 0.0040)],
                   tones("grey_lit", "grey", "grey_dark"), 0.004)


# ---------------------------------------------------------------------------
# THE HAIR, BUILT THE WAY THE HEROES' IS AND CUT HIS OWN WAY. Block hair in three tones by
# facing: a slab, a darker mass down the back, then wedges standing on it and hanging off it,
# every one set by hand. HIS CUT is an older man's: PARTED on his right and COMBED OVER in thick
# locks that rise toward their ends, a point in the middle of the hairline (the original's) with
# one shorter lock either side and the CORNERS GONE BACK, and GREY in his sideburns and his
# moustache. No drawing on any of it.
# ---------------------------------------------------------------------------
HAIR_CHAMFER = 0.012
FRINGE_BASE = 0.676
#   A SIDE PARTING on his right and everything combed over to his left: three locks laid across
#   the top, each its own ARC. A lock starts low at the parting, swells to a crest and comes down
#   to a POINT that hangs past the slab over his left temple. No two have the same crest or the
#   same length, and there is a groove between each pair, so from above and from the front they
#   are three locks and not one board (v02 and v03 were parallel planks that merged into a lid).
#       (middle y, half width, [(x, z, half height)] from the parting to the point)
SWEEPS = [
    (-0.118, 0.052, [(-0.098, 0.694, 0.016), (0.030, 0.724, 0.031), (0.130, 0.716, 0.024), (0.210, 0.684, 0.008)]),
    (-0.008, 0.050, [(-0.106, 0.694, 0.016), (0.010, 0.714, 0.027), (0.120, 0.704, 0.022), (0.222, 0.664, 0.008)]),
    (0.100, 0.048, [(-0.096, 0.690, 0.014), (0.030, 0.704, 0.022), (0.110, 0.696, 0.018), (0.196, 0.662, 0.007)]),
]
#   (name, base, tip, base half size, tip half size, across)
TUFTS = [
    # on the short side of the parting, one low lock combed down to his right
    ("part-side", (-0.100, 0.000, 0.694), (-0.186, 0.002, 0.668), (0.150, 0.013), (0.136, 0.010), Y),
    # three slabs hanging over the back mass, cropped short, the middle one the longest
    ("back-r", (-0.116, 0.196, 0.684), (-0.122, 0.200, 0.560), (0.056, 0.014), (0.044, 0.011), X),
    ("back-m", (0.000, 0.198, 0.688), (0.000, 0.203, 0.534), (0.054, 0.015), (0.042, 0.011), X),
    ("back-l", (0.114, 0.196, 0.684), (0.118, 0.199, 0.566), (0.054, 0.014), (0.042, 0.011), X),
]
#   grey: in front of each ear a slim sideburn. (The nape was grey too until v03: from behind it
#   read as a row of teeth under his hair. It is his brown now.)
GREYS = [
    # (slim and lying on the head: v02's stood 26 mm off it and read as plates stuck to his temples)
    ("sideburn-r", (-0.176, -0.094, 0.640), (-0.176, -0.104, 0.524), (0.007, 0.036), (0.006, 0.018)),
    ("sideburn-l", (0.176, -0.094, 0.640), (0.176, -0.104, 0.524), (0.007, 0.036), (0.006, 0.018)),
]
NAPE = [
    ("nape-r", (-0.108, 0.180, 0.470), (-0.114, 0.180, 0.428), (0.052, 0.014), (0.030, 0.010)),
    ("nape-m", (0.000, 0.182, 0.470), (0.000, 0.182, 0.416), (0.056, 0.014), (0.032, 0.010)),
    ("nape-l", (0.108, 0.180, 0.470), (0.114, 0.180, 0.432), (0.052, 0.014), (0.030, 0.010)),
]


def build_hair(part):
    hair = tones("hair_top", "hair", "hair_under")
    # the masses underneath: one step deeper, so what lies over them reads as hair lying on hair
    deep = tones("hair", "hair_under", "hair_under")
    grey = tones("grey_lit", "grey", "grey_dark")
    # the slab: drawn in a little toward its top, the way combed hair sits
    part.block("hair-slab", "head", X, Y, [((0, 0.002, tex.SLAB_FOOT), 0.184, 0.182), ((0, 0.006, 0.698), 0.176, 0.176)],
               hair, HAIR_CHAMFER)
    part.block("hair-back", "head", X, Y, [((0, 0.170, 0.452), 0.176, 0.022), ((0, 0.170, 0.650), 0.184, 0.024)], deep, HAIR_CHAMFER)
    for s in (1, -1):
        # over the ear his own brown; GREY behind and below it, and in the sideburn
        # (v01 had the whole panel grey, and from the front it read as a strap down each side of his head)
        part.block("hair-temple", "head", X, Y, [((s * 0.180, 0.056, 0.524), 0.014, 0.112), ((s * 0.183, 0.056, 0.648), 0.016, 0.118)],
                   hair, 0.010)
        part.block("hair-temple", "head", X, Y, [((s * 0.178, 0.122, 0.456), 0.013, 0.052), ((s * 0.180, 0.122, 0.536), 0.014, 0.054)],
                   hair, 0.008)
    # THE HAIRLINE: a point in the middle and a shorter lock either side (`tex.FRINGE`, which the
    # face's shadow is cut from too). Outside them the slab's own edge is the hairline, higher up.
    for x, tip, half, tip_half in tex.FRINGE:
        part.block("hair-fringe", "head", X, Y, [((x, -0.174, FRINGE_BASE), half, 0.020), ((x, -0.176, tex.FRINGE_SHOULDER), half, 0.019),
                                                 ((x, -0.181, tip), tip_half, 0.009)], hair, 0.006)
    for y, half, line in SWEEPS:
        last = len(line) - 1
        part.block("hair-sweep", "head", Y, Z, [((x, y, z), half * (0.5 if i == last else 1.0), hz) for i, (x, z, hz) in enumerate(line)],
                   hair, 0.010)
    for name, base, tip, base_half, tip_half, across in TUFTS:
        part.wedge("hair-" + name.split("-")[0], "head", base, tip, base_half, tip_half, hair, 0.009, across=across)
    for name, base, tip, base_half, tip_half in GREYS:
        part.wedge("hair-sideburn", "head", base, tip, base_half, tip_half, grey, 0.006)
    for name, base, tip, base_half, tip_half in NAPE:
        faces = part.wedge("hair-nape", "head", base, tip, base_half, tip_half, hair, 0.008)
        # the points lie on the back of his neck: their tips stay with the shoulders when the head tips back
        part.bend(faces, lambda co: [("head", 1.0 - 0.3 * _ramp(0.470, 0.416, co.z)), ("torso", 0.3 * _ramp(0.470, 0.416, co.z))])


# ---------------------------------------------------------------------------
# THE TORSO. One block, his white T-shirt, with a BELLY: it is deepest and widest just above the
# waistband and stands 22 mm further forward there than at the chest ("belly first",
# GaitStyles.MangKanor). On it, each a piece of its own: the overalls' waistband, the bib with
# a brass button at each top corner, a strap over each shoulder, the high back, his belt bag on
# its strap, and the tricycle's keys at his right hip.
# ---------------------------------------------------------------------------
TORSO_ROWS = [(0.176, 0.120, 0.106, 0.020), (0.222, 0.126, 0.116, 0.014), (0.290, 0.112, 0.098, 0.018),
              (0.343, 0.098, 0.090, 0.018)]   # z, half width, half depth, middle y
ARM_Y, ARM_Z = 0.0173, tex.ARM_Z   # the line his arms are built along (the arm bone)
WAIST_ROWS = [(tex.WAIST[0] - 0.004, 0.126, 0.112, 0.021), (tex.WAIST[1], 0.132, 0.122, 0.015)]
STRAP_PROUD = 0.012
AHEAD = (0, -1, 0)
BEHIND = (0, 1, 0)
LEATHER_T = tones("leather_lit", "leather", "leather_dark")
BRASS_T = tones("brass_lit", "brass", "brass_dark")
DENIM_T = tones("denim_lit", "denim", "denim_dark")


def chest_y(z):
    return _table([(r[0], r[3] - r[2]) for r in TORSO_ROWS], z)


def back_y(z):
    return _table([(r[0], r[3] + r[2]) for r in TORSO_ROWS], z)


def waist_front(z):
    return _table([(r[0], r[3] - r[2]) for r in WAIST_ROWS], z)


def hang_weights(co):
    """A thing hung from the waistband over the top of a leg: its foot takes half of that leg."""
    leg = 0.5 * _ramp(tex.WAIST[0], 0.118, co.z)
    return [("torso", 1.0 - leg), ("leg-left" if co.x > 0 else "leg-right", leg)]


def build_torso(part):
    shirt = proj_square("torso", "white", "white_shade", 0.90, ends=(2, "white_shade"))
    part.block("torso", "torso", X, Y, [((0, cy, z), hw, hd) for z, hw, hd, cy in TORSO_ROWS], shirt, 0.014)
    # the seat of his overalls, between the legs
    part.block("pelvis", "torso", X, Y, [((0, tex.TROUSER_Y, 0.140), 0.080, 0.066), ((0, tex.TROUSER_Y, 0.182), 0.092, 0.070)],
               flat("denim_deep"), 0.0)
    # his neck, for when the head tips back and the chin lifts off the chest
    neck = part.block("neck", "torso", X, Y, [((0, 0.010, 0.326), 0.062, 0.058), ((0, 0.008, 0.356), 0.058, 0.054)],
                      flat("skin_shade"), 0.008)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.332, 0.350, co.z)), ("head", 0.7 * _ramp(0.332, 0.350, co.z))])

    # THE WAISTBAND: denim, stitched twice (the islands), wider than the shirt all round
    denim = proj_square("torso", "denim", "denim_dark", 0.90, ends=(2, "denim_lit"))
    part.block("waistband", "torso", X, Y, [((0, cy, z), hw, hd) for z, hw, hd, cy in WAIST_ROWS], denim, 0.007)

    # THE BIB: a plate lying on his chest and belly, narrowing to its top. Its front takes its own
    # swatch (the pocket, the pen, the stitching); its edges are one darker tone.
    z0, z1 = tex.BIB
    w0, w1 = tex.BIB_HALF
    zm = 0.290
    wm = w0 + (w1 - w0) * (zm - z0) / (z1 - z0)
    part.block("bib", "torso", X, Y, [((0, chest_y(z) - 0.004, z), w, 0.0065) for z, w in ((z0 - 0.004, w0), (zm, wm), (z1, w1))],
               panel("bib", 0, 2, (-0.072, 0.072, z0, z1), AHEAD, "denim_dark"), 0.004)

    # THE STRAPS: each is three blocks that overlap at the shoulder, front, top and back. A strap
    # takes the torso's front (or back) island on its one outward face, at the place it sits.
    cx, hw = 0.5 * (tex.STRAP[0] + tex.STRAP[1]), 0.5 * (tex.STRAP[1] - tex.STRAP[0])
    half = 0.5 * (STRAP_PROUD + 0.010)
    foot_f, foot_b = z1 - 0.010, tex.BACK_PANEL[1] - 0.008
    for s in (1, -1):
        fy = lambda z: chest_y(z) - STRAP_PROUD + half
        by = lambda z: back_y(z) + STRAP_PROUD - half
        part.block("strap-front", "torso", X, Y, [((s * cx, fy(foot_f), foot_f), hw, half), ((s * cx, fy(0.349), 0.349), hw, half)],
                   panel("torso.front", 0, 2, (0.0, 1.0, 0.0, 1.0), AHEAD, "denim_dark"), 0.003)
        part.block("strap-back", "torso", X, Y, [((s * cx, by(foot_b), foot_b), hw, half), ((s * cx, by(0.349), 0.349), hw, half)],
                   panel("torso.back", 0, 2, (0.0, 1.0, 0.0, 1.0), BEHIND, "denim_dark"), 0.003)
        part.block("strap-top", "torso", X, Z, [((s * cx, chest_y(0.343) - STRAP_PROUD + 0.002, 0.3440), hw, 0.008),
                                                ((s * cx, back_y(0.343) + STRAP_PROUD - 0.002, 0.3440), hw, 0.008)], DENIM_T, 0.003)
        # the brass button that holds the strap to the corner of the bib
        front = chest_y(z1 - 0.012) - STRAP_PROUD
        part.block("bib-button", "torso", X, Z, [((s * cx, front + 0.003, z1 - 0.012), 0.0095, 0.0095), ((s * cx, front - 0.0050, z1 - 0.012), 0.0095, 0.0095)],
                   BRASS_T, 0.004)

    # THE HIGH BACK: a plate up his back, narrowing to where the straps join it (the back island)
    b0, b1 = tex.BACK_PANEL
    h0, h1 = tex.BACK_HALF
    hm = h0 + (h1 - h0) * (zm - b0) / (b1 - b0)
    part.block("overall-back", "torso", X, Y, [((0, back_y(z) + 0.004, z), w, 0.0065) for z, w in ((b0 - 0.004, h0), (zm, hm), (b1, h1))],
               panel("torso.back", 0, 2, (0.0, 1.0, 0.0, 1.0), BEHIND, "denim_dark"), 0.004)

    # HIS BELT BAG, where a tricycle driver keeps the day's fares: worn in front, to his left of
    # the buckle line, on a leather strap round the waistband. Its face is a swatch.
    cy = WAIST_ROWS[1][3]
    part.block("bag-strap", "torso", X, Y, [((0, cy + 0.003, 0.1965), 0.1350, 0.1240), ((0, cy + 0.003, 0.2085), 0.1350, 0.1240)],
               LEATHER_T, 0.003)
    bx, bz = 0.050, 0.196
    front = waist_front(bz)
    part.block("bag", "torso", X, Z, [((bx, front + 0.006, bz), 0.040, 0.026), ((bx, front - 0.028, bz - 0.002), 0.037, 0.0235)],
               panel("bag", 0, 2, (bx - 0.040, bx + 0.040, bz - 0.028, bz + 0.024), AHEAD, "leather"), 0.008)
    # THE KEYS, on a ring at his right hip: the tricycle's and the house's. They swing with the leg.
    kx = -(WAIST_ROWS[1][1] + 0.005)
    faces = part.block("key-ring", "torso", X, Y, [((kx, 0.012, 0.166), 0.004, 0.010), ((kx, 0.012, 0.190), 0.004, 0.010)], BRASS_T, 0.003)
    faces += part.block("key", "torso", X, Y, [((kx - 0.001, 0.004, 0.126), 0.003, 0.0055), ((kx - 0.001, 0.006, 0.170), 0.003, 0.0085)],
                        tones("grey_lit", "grey", "grey_dark"), 0.002)
    faces += part.block("key", "torso", X, Y, [((kx - 0.002, 0.022, 0.136), 0.003, 0.0050), ((kx - 0.002, 0.019, 0.170), 0.003, 0.0080)],
                        BRASS_T, 0.002)
    part.bend(faces, hang_weights)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests, at the HEROES' length (Dante's runs 0.100 to 0.292;
# the stock rig's to 0.384). `s` is +1 for his left, -1 for his right. From the shoulder out:
# the white sleeve to 0.150 with a turned hem, then the dark arm sleeve to the elbow (0.178), on
# the arm bone; then, on the elbow bone, the arm sleeve down to a grey grip band at the wrist,
# and his bare fist from 0.232 to 0.300. Two rigid blocks overlap at the elbow, as a toy's joint does.
# ---------------------------------------------------------------------------

def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    cloth = proj_square(group, "sleeve", "sleeve_dark", 0.90, ends=(0, "sleeve_dark"))
    arm_block(part, "sleeve", bone, s, [(0.098, (0.052, 0.054)), (0.128, (0.058, 0.060)), (tex.SLEEVE_END, (0.058, 0.060))],
              proj_square(group, "white", "white_shade", 0.90, ends=(0, "white_shade")), 0.012)
    # the sleeve's turned hem
    arm_block(part, "sleeve-hem", bone, s, [(0.137, (0.0620, 0.0640)), (0.153, (0.0620, 0.0640))],
              tones("white_lit", "white_shade", "white_deep"), 0.005)
    arm_block(part, "armsleeve", bone, s, [(0.146, (0.0505, 0.0525)), (ELBOW_X + 0.014, (0.0495, 0.0515))], cloth, 0.010)
    arm_block(part, "armsleeve", fore, s, [(ELBOW_X - 0.014, (0.0505, 0.0525)), (tex.WRIST + 0.006, (0.0460, 0.0470))], cloth, 0.010)
    # the grip band at the wrist: paler, narrower than the fist
    arm_block(part, "armsleeve-band", fore, s, [(tex.WRIST - 0.017, (0.0490, 0.0500)), (tex.WRIST - 0.003, (0.0485, 0.0495))],
              tones("grey_lit", "grey", "grey_dark"), 0.003)
    # the fist: a plain block, the far end of the forearm bone, its fingers and thumb drawn on
    # its flat front and back only
    arm_block(part, "hand", fore, s, [(tex.WRIST, (0.0555, 0.0555)), (tex.HAND_END, (0.0555, 0.0555))],
              proj_square(group, "skin", "skin_shade", 0.93), 0.014)


# ---------------------------------------------------------------------------
# THE LEGS. Short and wide apart: denim, a paler turned cuff, a white sock, a brown loafer with
# a strap across the instep, on a cream sole wider than the shoe. Feet straight ahead.
# ---------------------------------------------------------------------------
LEG_X = 0.083


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    ty = tex.TROUSER_Y
    part.block("trouser", bone, X, Y, [((s * LEG_X, ty, tex.SOCK_TOP + 0.006), 0.058, 0.0760), ((s * LEG_X, ty, 0.181), 0.047, 0.0735)],
               proj_square(group, "denim", "denim_dark", 0.90, ends=(2, "denim_dark")), 0.012)
    part.block("trouser-cuff", bone, X, Y, [((s * LEG_X, ty, tex.SOCK_TOP), 0.0655, 0.0840), ((s * LEG_X, ty, tex.SOCK_TOP + 0.027), 0.0655, 0.0840)],
               tones("roll_lit", "roll", "roll_dark"), 0.007)
    part.block("sock", bone, X, Y, [((s * LEG_X, ty + 0.006, 0.036), 0.047, 0.055), ((s * LEG_X, ty + 0.004, tex.SOCK_TOP + 0.004), 0.050, 0.060)],
               tones("white_lit", "white", "white_shade"), 0.006)

    def station(y, half, z0, z1, cut):
        return rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)

    # the loafer: low at the toe, rising a little to the heel
    upper = [(-0.112, 0.036, 0.012, 0.036, 0.008), (-0.096, 0.055, 0.012, 0.044, 0.012), (-0.050, 0.065, 0.012, 0.050, 0.014),
             (0.004, 0.063, 0.012, 0.054, 0.014), (0.080, 0.061, 0.012, 0.054, 0.014), (0.102, 0.046, 0.014, 0.048, 0.008)]
    part.loft("shoe", bone, [station(*row) for row in upper], proj_square(group, "leather", "leather_dark", 0.88))
    # the strap across the instep, with the slit a coin goes in
    part.block("shoe-strap", bone, X, Y, [((s * LEG_X, -0.050, 0.0470), 0.0600, 0.0125), ((s * LEG_X, -0.050, 0.0545), 0.0560, 0.0110)],
               tones("leather_lit", "leather_dark", "leather_deep"), 0.003)
    part.block("shoe-slit", bone, X, Y, [((s * LEG_X, -0.050, 0.0540), 0.0150, 0.0040), ((s * LEG_X, -0.050, 0.0560), 0.0140, 0.0034)],
               flat("leather_deep"), 0.0)
    # the sole: cream crepe, 14 mm thick, wider than the shoe all round
    h = 0.007
    part.block("sole", bone, X, Z, [((s * LEG_X, -0.119, h), 0.038, h), ((s * LEG_X, -0.100, h), 0.059, h), ((s * LEG_X, -0.052, h), 0.0705, h),
                                    ((s * LEG_X, 0.082, h), 0.0685, h), ((s * LEG_X, 0.109, h), 0.046, h)],
               tones("sole", "sole", "sole_dark"), 0.004)


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
    """character-male-e.glb with its meshes swapped: skeleton, bind matrices and clips copied untouched,
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

    # his own locomotion clips, in place of the ones copied across (the clips script)
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign (mang_kanor)"}
    gltf["extras"] = {"prototype": "character-redesign-20261006", "replaces": "nothing yet; the owner judges it first"}
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
    print("triangles %d  (budget %d, the original is 789)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if abs(hi.z - HEIGHT) > 0.004:
        raise SystemExit("the top is at %.4f, not the %.4f of his hair with the head at %.2f" % (hi.z, HEIGHT, HEAD_SCALE))
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
        if abs(fist - 0.111) > 0.004:
            raise SystemExit("the far %d%% of the arm is %.4f across, not the fist's 0.111: something else is in it" % (round(100 * FIST_SHARE), fist))
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
        # that shares the head with the torso (the top of his neck, the nape of his hair) by the same
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
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_mang_kanor_textures.py")
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
