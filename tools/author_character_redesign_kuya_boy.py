"""Build the Kuya Boy redesign: the man redrawn AS IF HE WERE ONE OF THE NINE HEROES.

    py -3 tools/author_character_redesign_kuya_boy_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_kuya_boy.py
    ... -- --out other.glb     (writes there instead and leaves the .blend alone; needs the atlas beside it)

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/kuya_boy/kuya_boy-redesign.glb
    ArtSource/kuya_boy/redesign-20261006/kuya_boy_redesign.blend
beside the atlas painted by the textures script.

WHY. Owner, 2026-10-06: the nine heroes were redesigned and are the game's models; the twelve
Classic street characters are being redrawn as if they were heroes. Three went first as the
pattern (bayan, bebang, lola_pacing) and were approved; this is one of the other nine, built to
docs/reports/character-redesign/classic-brief.md. A copy of Bayan's author script (the approved
man) rewritten for him, on the HEROES' body proportions as Bebang's is; those files are left
alone (docs/CHARACTER_REDESIGN_DANTE.md sections 13, 15 and 15.9 part D).

NOTHING IN THE GAME LOADS IT YET. The folder is outside Resources, no roster row points here,
and character-male-b.glb, person_kuya_boy.asset, RosterBookBuilder.cs and every runtime script
are not touched.

WHO HE IS. A Classic character, not a hero: no powers, no gear. "The cool big brother: laid
back, leaning back a little, arms low and lazy, head cocked" (GaitStyles.KuyaBoy). "Eldest of
seven. He has been the defender since before he could count, and both the arm and the footwork
know it" (ConvertedCharacterSelect). "The heaviest hitter in the game" (GAME_OVERVIEW.md,
LAKAS 5). A young man of the neighbourhood, somebody's kuya.

WHAT IS KEPT OF HIM (the original is character-male-b.glb wearing person_kuya_boy.asset): his
shaved head; his big brown beard with a moustache, hanging to a jagged foot over his chest; his
big ears; the thing tucked behind his left ear; his navy collared shirt; his brown belt; his
mustard shorts; brown footwear; his brown skin.
WHAT IS DESIGNED, THE HEROES' WAY (each is argued where it is built):
  * THE HEAD is Dante's `shaped` rows, number for number (`HEAD_ROWS`), flat face plane at
    0.165, and 0.84 about the head joint after the UVs resolve (`HEAD_SCALE`);
  * THE FACE is the heroes': solid ink eye blocks, HALF CLOSED under a flat lid (he is relaxed),
    and one thin stroke of a mouth with one end lifted (the textures script);
  * HIS HAIR IS HIS BEARD, and it is built the way the heroes' hair is: a deep mass round the
    jaw, cheeks and sideburns rising off it to his ears, a moustache of two chunky wedges, and
    five tapered locks of three lengths lying on the mass and hanging off it over his chest
    (the original's jagged foot). Three tones by facing, the mass underneath one step deeper.
    Its hanging locks give to the torso (`Part.bend`) so the head can turn without the beard
    sawing through his shoulders. HIS HEAD STAYS SHAVED, and that is drawn too: the shadow of
    hair cut to the skin, a shell 3 mm proud in a tone between his skin and his beard, with a
    level line across his brow, a step round each ear, and one line parted on his right;
  * THE PENCIL behind his left ear: the original tucks a dark and brown stick there. It is a
    yellow carpenter's pencil now, the kuya who fixes everything in the house;
  * THE SHIRT is a navy polo with flat collar wings, a cream and mustard band at each sleeve's
    mouth, a chest pocket with a mustard edge, and a big 7 on the back: the eldest of seven;
  * A TOWEL over his right shoulder, cream with navy stripes (docs/reports/improvement-2026-09-10
    gave him "everyday towel details"). Bayan's hangs from the back of his belt; his lies on his
    shoulder, the way a man coming off the court carries it;
  * a brown leather belt through mustard loops with a steel buckle and its loose tail hanging;
  * MUSTARD WALKING SHORTS to below the knee with a turned cuff and a cargo pocket on each leg;
  * FOOTWEAR, decided deliberately: brown leather SANDALS, two wide straps and a heel strap on
    a leather footbed over a dark sole, his feet bare in them. The original's brown shoe has a
    skin-coloured top and reads as a sandal already; Bayan wears boots and Bebang sneakers.
He stays an everyday man. Nothing is a hero's: no gold trim, no emblem, no costume.

and still: a drawing only on a face that squarely faces its view (`proj_square`); no painted
hair shine; cloth across two bones bends (the belt's tail takes the leg, the beard's locks and
the top of his neck share the head and the torso); feet on zero; under 6,000 triangles. HE HAS
NO STANDING COLLAR: the wings lie flat on his chest.

THE HANDS, FOR THE GAME'S FIRST-PERSON ARMS (Editor/ViewmodelArmAuthor.cs cuts them from this
mesh and sizes a redesigned arm by its fist, the far 14 per cent of the arm's length):
  * the arm runs along x from 0.098 to 0.310 (near the heroes' length; the Classic rig's runs to
    0.384). The fist is a plain block at the FAR END of the forearm bone, x 0.252 to 0.310,
    104 by 116 mm, bare skin; the far 14 per cent (from x 0.280) is fist and nothing else;
  * the forearm narrows to 94 by 100 mm at the wrist, so nothing wider than the fist is beyond
    the wrist; the widest thing on the arm is the sleeve's band (142 mm);
  * the fist's top is 0.058 above the arm bone (`verify` holds it between 0.045 and 0.066,
    where a carried tsinelas is parked).

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
import author_character_redesign_kuya_boy_textures as tex  # noqa: E402  the island layout
import author_character_redesign_kuya_boy_clips as clips  # noqa: E402  his own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/character-male-b.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/kuya_boy")
OUT = os.path.join(FOLDER, "kuya_boy-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/kuya_boy/redesign-20261006/kuya_boy_redesign.blend")
NAME = "kuya_boy-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# TWO BONES THE STOCK RIG DOES NOT HAVE: an elbow in each arm, a child of the arm bone, appended
# AFTER the seven so their indices and names do not move, exactly as the heroes' are. A clip that
# does not key them leaves the arm straight. His sits just inside the mouth of the sleeve.
ELBOW_X = 0.190
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
# THE HEAD IS 84 PER CENT OF THE ORIGINAL'S (the heroes' size; owner, 2026-10-06: "head size
# stays the same as the heroes"). Everything is BUILT and PAINTED at the original's size, and
# only after the UVs are resolved is every vertex that rides the `head` bone drawn in toward the
# head JOINT (`build`).
HEAD_SCALE = 0.84
_ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
if "--head-scale" in _ARGS:
    HEAD_SCALE = float(_ARGS[_ARGS.index("--head-scale") + 1])
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))   # the `head` bone of character-male-b.glb, in Blender space
HEAD_TOP = 0.6635                           # the crown of his shaved scalp, before the head's scale
HEIGHT = HEAD_JOINT.z + (HEAD_TOP - HEAD_JOINT.z) * HEAD_SCALE     # 0.610
SHARP_ANGLE = 40.0       # degrees: a chamfer is a hard edge, a soft corner or a taper is one form
TRIANGLE_BUDGET = 6000   # the original is 600 or so
FIST_SHARE = 0.14        # ViewmodelArmAuthor: the fist is the far 14 per cent of the arm
FIST_ACROSS = 0.116      # the fist's wider measure, Dante's

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
AHEAD = (0, -1, 0)
BEHIND = (0, 1, 0)
SKIN_T = tones("skin_lit", "skin", "skin_shade")
BEARD_T = tones("beard_top", "beard", "beard_dark")          # the locks that lie on top
BEARD_UNDER = tones("beard", "beard_dark", "beard_deep")     # the mass underneath, one step deeper
LEATHER_T = tones("leather_lit", "leather", "leather_dark")
STEEL_T = tones("steel_lit", "steel", "steel_dark")
CREAM_T = tones("cream", "cream", "cream_shade")


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
    # Only the flat front takes the drawing (his face). The limit is 0.84 and not 0.93 so the
    # first three facets either side of the middle take it: his eyes reach out to |x| 0.120.
    # Every other face of the head is flat skin, and the faces that turn under at the jaw are
    # the SAME skin, not a shade. HIS HEAD IS SHAVED: the crown, where the box turns up to the
    # sky, is the one light tone (the last ring and the cap, above the forehead's flat plane).
    face = proj_square("head", "skin", "skin", 0.84)
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS],
              lambda i, j: flat("skin_lit") if i >= len(HEAD_ROWS) - 2 else face)
    # NO NOSE. HIS EARS are big, as the original's are, and stand a little back.
    for s in (1, -1):
        part.block("ear", "head", Y, Z, [((s * 0.166, 0.016, 0.458), 0.034, 0.046), ((s * 0.226, 0.026, 0.458), 0.025, 0.035)],
                   tones("skin_lit", "skin", "skin_shade"), 0.010)
    # THE PENCIL behind his LEFT ear, lying along the side of his head over the top of the ear,
    # its point forward and a little down: a yellow body, a steel ferrule and a pale rubber at
    # the back, shaved wood and a dark lead at the front.
    a, b = Vector((0.1810, -0.118, 0.4985)), Vector((0.1835, 0.066, 0.5225))
    at = lambda f: a + (b - a) * f
    seg = lambda name, f0, f1, h0, h1, paint: part.block(name, "head", X, Z, [(at(f0), h0, h0), (at(f1), h1, h1)], paint, 0.0018)
    seg("pencil-lead", 0.000, 0.050, 0.0016, 0.0030, flat("ink"))
    seg("pencil-wood", 0.045, 0.150, 0.0030, 0.0068, tones("wood", "wood", "cream_deep"))
    seg("pencil", 0.150, 0.880, 0.0068, 0.0068, tones("yellow", "yellow", "yellow_dark"))
    seg("pencil-ferrule", 0.875, 0.940, 0.0074, 0.0074, STEEL_T)
    seg("pencil-rubber", 0.935, 1.000, 0.0066, 0.0060, tones("cream_shade", "cream_shade", "cream_deep"))


# ---------------------------------------------------------------------------
# HIS BEARD IS HIS HAIR, and it is built the way the heroes' hair is: a deep MASS, and chunky
# pieces set by hand lying on it and hanging off it, each a tapered block with real depth. No
# drawing on any of it: a lighter tone on faces turned up, the base round the sides, a darker
# underside; the mass underneath is one step deeper again, so a lock reads as hair on hair.
#   the mass: a cup round his jaw from under one ear to under the other, its foot resting on
#     the top of his chest (z 0.343, the head joint, so it does not move when the head scales);
#   the cheeks: a block each side of the mouth's window, rising to CHEEK_TOP at the side of the
#     face with its inner edge cut on a slant, and a sideburn tapering up in front of each ear;
#   the moustache: two wedges falling from under where a nose would be to outside the mouth;
#   FIVE LOCKS lying on the front of the mass and hanging off it over his chest, the middle one
#     the longest: the original's jagged foot. They are the only part below the head joint, and
#     they give to the torso toward their tips, so a turned head carries the mass round and the
#     tips stay hanging in front of his chest.
# The head above it is SHAVED. That is his cut, and nothing is built on it.
# ---------------------------------------------------------------------------
BEARD_LINE = tex.BEARD_LINE
#   (middle x, tip z, base y, tip y, base half width, tip half width): his LEFT; x 0 is the middle one
#   No two are the same and they overlap, so the foot is one jagged mass and not a row of teeth.
LOCKS = [(0.004, 0.304, -0.172, -0.158, 0.050, 0.026), (0.078, 0.322, -0.169, -0.152, 0.042, 0.020), (-0.070, 0.312, -0.170, -0.153, 0.040, 0.022),
         (0.136, 0.342, -0.166, -0.146, 0.030, 0.015), (-0.132, 0.336, -0.166, -0.146, 0.032, 0.016)]
LOCK_BASE = 0.378
SCALP = tones("stubble_top", "stubble", "stubble_dark")


def lock_weights(co):
    give = 0.45 * _ramp(0.350, 0.304, co.z)
    return [("head", 1.0 - give), ("torso", give)]


def _head_row(z):
    """The head's (half width, front depth, back depth, exponent) at any height, between its rows."""
    rows = HEAD_ROWS
    for a, b in zip(rows, rows[1:]):
        if z <= b[0]:
            t = max(0.0, (z - a[0]) / (b[0] - a[0]))
            return tuple(a[k] + (b[k] - a[k]) * t for k in range(1, 5))
    return rows[-1][1:]


def build_hair(part):
    """His beard (the build calls this where a hero's hair is built)."""
    part.block("beard-mass", "head", X, Y, [((0, 0, 0.343), 0.150, (0.168, 0.020)), ((0, 0, BEARD_LINE), 0.189, (0.182, 0.034))],
               BEARD_UNDER, 0.034)
    for s in (1, -1):
        inner, outer = (0.070, 0.068) if s > 0 else (0.068, 0.070)
        top_in, top_out = (0.022, 0.068) if s > 0 else (0.068, 0.022)
        part.block("beard-cheek", "head", X, Y, [((s * 0.122, -0.118, 0.372), (inner, outer), 0.064),
                                                 ((s * 0.122, -0.118, tex.CHEEK_TOP), (top_in, top_out), 0.062)],
                   BEARD_UNDER, 0.026)
        # the sideburn: a short broad block from the cheek up to the scalp's shadow, in front of the ear
        part.wedge("beard-sideburn", "head", (s * 0.180, -0.084, 0.428), (s * 0.180, -0.070, 0.500), (0.013, 0.034), (0.010, 0.022),
                   BEARD_UNDER, 0.006)
        # the moustache: a wedge each side, 9 mm proud of his lip, falling to outside the mouth
        part.wedge("beard-moustache", "head", (s * 0.004, -0.1665, 0.4370), (s * 0.080, -0.1720, 0.4090), (0.0085, 0.0075), (0.0065, 0.0065),
                   BEARD_T, 0.003, across=Z)
    for x, tip, y0, y1, half, tip_half in LOCKS:
        faces = part.wedge("beard-lock", "head", (x, y0, LOCK_BASE), (x * 1.04, y1, tip), (half, 0.018), (tip_half, 0.010), BEARD_T, 0.009)
        part.bend(faces, lock_weights)
    # HIS SHAVED SCALP: the shadow of hair cut to the skin, a shell 3 mm proud of the head in a
    # tone between his skin and his beard. A cap over the crown whose foot is a level line across
    # his brow (a barber's line-up), and a band round the back and sides from behind each temple,
    # down to the nape, which the ears stand through and the sideburns run up into.
    # ONE SHELL, the head's own rings: a point of a ring stands 3 mm proud where the scalp is, and
    # is sunk 7 mm into the head where it is not, so the hairline is a crisp step and not a seam.
    def scalp_ring(z, front):
        row = _head_row(z)
        out_ring, in_ring = super_ring(z, row[0] + 0.003, row[1] + 0.003, row[2] + 0.003, row[3]), \
            super_ring(z, row[0] - 0.007, row[1] - 0.007, row[2] - 0.007, row[3])
        return [o if (front is not None and o.y >= front) else i for o, i in zip(out_ring, in_ring)]

    line = tex.HAIRLINE
    rings = [scalp_ring(0.446, None), scalp_ring(0.450, 0.040), scalp_ring(0.497, 0.040), scalp_ring(0.503, -0.064),
             scalp_ring(line - 0.003, -0.064), scalp_ring(line + 0.003, -1.0), scalp_ring(0.604, -1.0), scalp_ring(0.646, -1.0),
             super_ring(0.6635, 0.153, 0.149, 0.150, 3.6)]
    part.loft("scalp", "head", rings, SCALP, caps=(False, True))
    # THE PART: one line shaved to the skin on his right, from the brow back. A barber's flourish,
    # and the one thing on the scalp that is not symmetrical.
    part.block("scalp-part", "head", X, Z, [((-0.1735, -0.118, 0.5440), 0.0050, 0.0042), ((-0.1750, -0.012, 0.5520), 0.0050, 0.0042)],
               tones("skin_lit", "skin", "skin_shade"), 0.0015)


# ---------------------------------------------------------------------------
# THE TORSO. The heroes' torso block, a little wider at the shoulders than at the belt: he is
# the heaviest hitter of the twelve. A navy polo tucked into a brown belt. On it, each a piece
# of its own: the collar's two cream wings, the belt through four mustard loops, a steel buckle,
# the belt's loose tail, and the towel over his right shoulder.
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.343)
TORSO_LOW = (0.140, 0.090)     # half width, half depth at the belt
TORSO_HIGH = (0.156, 0.094)    # at the shoulders: he keeps this breadth as a trait
TORSO_CY = -0.002
ARM_Y, ARM_Z = 0.006, tex.ARM_Z   # the line his arms are built along (the arm bone is at z 0.288)
BELT_HALF = (0.147, 0.097)        # the belt's half width and half depth


def torso_half(z):
    t = (z - TORSO_Z[0]) / (TORSO_Z[1] - TORSO_Z[0])
    return (TORSO_LOW[0] + (TORSO_HIGH[0] - TORSO_LOW[0]) * t, TORSO_LOW[1] + (TORSO_HIGH[1] - TORSO_LOW[1]) * t)


def chest_y(z):
    return TORSO_CY - torso_half(z)[1]


def back_y(z):
    return TORSO_CY + torso_half(z)[1]


def hang_weights(co):
    """A thing hung from the belt over the top of a leg: its foot takes half of that leg."""
    leg = 0.5 * _ramp(tex.BELT[0], 0.130, co.z)
    return [("torso", 1.0 - leg), ("leg-left" if co.x > 0 else "leg-right", leg)]


def build_torso(part):
    shirt = proj_square("torso", "shirt", "shirt_dark", 0.90, ends=(2, "shirt_dark"))
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, TORSO_Z[0]), *TORSO_LOW), ((0, TORSO_CY, TORSO_Z[1]), *TORSO_HIGH)], shirt, 0.014)
    # the seat of his shorts, between the legs
    part.block("pelvis", "torso", X, Y, [((0, LEG_Y, 0.124), 0.058, 0.064), ((0, LEG_Y, 0.182), 0.060, 0.066)], flat("mustard_deep"), 0.0)
    # his neck, for when the head tips back and the beard lifts off his chest
    neck = part.block("neck", "torso", X, Y, [((0, TORSO_CY, 0.326), 0.056, 0.052), ((0, TORSO_CY, 0.356), 0.052, 0.048)],
                      flat("skin_shade"), 0.008)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.332, 0.350, co.z)), ("head", 0.7 * _ramp(0.332, 0.350, co.z))])
    # THE COLLAR: two wings in the shirt's light tone lying FLAT on his chest either side of the open neck. They do
    # not stand: the head and the beard turn over them.
    for s in (1, -1):
        part.wedge("collar-wing", "torso", (s * 0.038, chest_y(0.34) - 0.003, 0.347), (s * 0.060, chest_y(0.30) - 0.005, 0.297),
                   (0.021, 0.006), (0.008, 0.004), tones("shirt_lit", "shirt_lit", "shirt_dark"), 0.003)

    # THE BELT: brown leather, worn at the foot of the shirt, through four mustard loops
    leather = proj_square("torso", "leather", "leather_dark", 0.90, ends=(2, "leather_lit"))
    part.block("belt", "torso", X, Y, [((0, TORSO_CY, tex.BELT[0] - 0.003), *BELT_HALF), ((0, TORSO_CY, tex.BELT[1]), *BELT_HALF)], leather, 0.007)
    belt_front, belt_back = TORSO_CY - BELT_HALF[1], TORSO_CY + BELT_HALF[1]
    for x, y in ((0.064, belt_back + 0.001), (-0.064, belt_back + 0.001), (0.090, belt_front - 0.001), (-0.090, belt_front - 0.001)):
        part.block("belt-loop", "torso", X, Y, [((x, y, tex.BELT[0] - 0.006), 0.007, 0.004), ((x, y, tex.BELT[1] + 0.004), 0.007, 0.004)],
                   tones("mustard_lit", "mustard", "mustard_dark"), 0.0)
    # the buckle: a plain steel frame, the leather showing through it, one prong
    mid = 0.5 * (tex.BELT[0] + tex.BELT[1]) - 0.001
    part.block("buckle", "torso", X, Z, [((0, belt_front + 0.004, mid), 0.024, 0.0205), ((0, belt_front - 0.008, mid), 0.024, 0.0205)],
               STEEL_T, 0.005)
    part.block("buckle-slot", "torso", X, Z, [((0, belt_front, mid), 0.0145, 0.0115), ((0, belt_front - 0.0095, mid), 0.0145, 0.0115)],
               flat("leather_dark"), 0.0)
    part.block("buckle-prong", "torso", X, Z, [((0.003, belt_front, mid), 0.0125, 0.0032), ((0.003, belt_front - 0.0115, mid), 0.0125, 0.0032)],
               flat("steel_lit"), 0.0)
    # the belt's loose tail, hanging past the buckle on his left: he never threads it through
    faces = part.wedge("belt-tail", "torso", (0.047, belt_front - 0.003, 0.190), (0.066, -0.078, 0.146), (0.0125, 0.0045), (0.0090, 0.0035),
                       LEATHER_T, 0.003)
    part.bend(faces, hang_weights)

    # THE TOWEL over his RIGHT shoulder: three blocks that overlap at the shoulder, a long end on
    # his chest, the fold over the top, a shorter end down his back. Each end takes its own
    # drawn swatch on its one outward face; its edges are one flat tone.
    x0, x1 = tex.TOWEL_X
    cx, hw = 0.5 * (x0 + x1), 0.5 * (x1 - x0)
    proud = 0.005
    front = [((cx, chest_y(z) - proud + 0.001, z), hw, proud) for z in (0.238, 0.300, 0.350)]
    part.block("towel", "torso", X, Y, front, panel("towel", 0, 2, (x0, x1, 0.238, 0.350), AHEAD, "cream_shade"), 0.003)
    back = [((cx, back_y(z) + proud - 0.001, z), hw, proud) for z in (0.268, 0.310, 0.350)]
    part.block("towel", "torso", X, Y, back, panel("towel_back", 0, 2, (x1, x0, 0.268, 0.350), BEHIND, "cream_shade"), 0.003)
    part.block("towel", "torso", X, Z, [((cx, chest_y(0.343) - 2 * proud + 0.002, 0.3465), hw, 0.0065),
                                        ((cx, back_y(0.343) + 2 * proud - 0.002, 0.3465), hw, 0.0065)], CREAM_T, 0.003)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests, at the heroes' length. `s` is +1 for his left, -1 for
# his right. THICK: he hits the hardest of the twelve. From the shoulder out: the navy sleeve to
# 0.176 with a cream band and a mustard edge at its mouth, on the arm bone; then, on the elbow
# bone, his bare forearm narrowing to the wrist, and the fist from 0.240 to 0.298. Two rigid
# blocks overlap at the elbow, as a toy's joint does.
# ---------------------------------------------------------------------------

def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    end = tex.SLEEVE_END
    arm_block(part, "sleeve", bone, s, [(0.098, (0.056, 0.060)), (0.146, (0.063, 0.067)), (end, (0.064, 0.068))],
              proj_square(group, "shirt", "shirt_dark", 0.90, ends=(0, "shirt_dark")), 0.011)
    # the band at the sleeve's mouth: cream, with a mustard edge
    arm_block(part, "sleeve-band", bone, s, [(end - 0.021, (0.0665, 0.0705)), (end - 0.006, (0.0665, 0.0705))], CREAM_T, 0.003)
    arm_block(part, "sleeve-edge", bone, s, [(end - 0.007, (0.0670, 0.0710)), (end + 0.001, (0.0670, 0.0710))],
              tones("mustard_lit", "mustard", "mustard_dark"), 0.003)
    # the forearm starts inside the sleeve's mouth, so no gap opens when the elbow folds
    # (flat tones, not the arm's islands: its first 14 mm lie under the sleeve's paint on those
    # islands, and a folded elbow showed a navy line across the skin, v04)
    arm_block(part, "forearm", fore, s, [(ELBOW_X - 0.016, (0.053, 0.056)), (tex.WRIST + 0.006, (0.047, 0.050))], SKIN_T, 0.010)
    # the fist: a plain block, the far end of the forearm bone, its fingers and thumb drawn on
    # its flat front and back only
    arm_block(part, "hand", fore, s, [(tex.WRIST, (0.052, 0.058)), (tex.HAND_END, (0.052, 0.058))],
              proj_square(group, "skin", "skin_shade", 0.93), 0.013)


# ---------------------------------------------------------------------------
# THE LEGS. The heroes' short legs. Mustard walking shorts to below the knee with a turned cuff
# and a cargo pocket on the outside of each leg; a bare shin; and SANDALS: a dark sole, a
# leather footbed, his bare foot, a wide strap over the toes, a wider one over the instep and a
# strap round the heel. Feet straight ahead.
# ---------------------------------------------------------------------------
LEG_X = 0.0836
LEG_Y = 0.004


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    hem = tex.SHORTS_HEM
    part.block("shorts", bone, X, Y, [((s * LEG_X, LEG_Y, hem), 0.063, 0.070), ((s * (LEG_X - 0.004), LEG_Y, 0.184), 0.056, 0.066)],
               proj_square(group, "mustard", "mustard_dark", 0.90, ends=(2, "mustard_dark")), 0.010)
    part.block("shorts-cuff", bone, X, Y, [((s * LEG_X, LEG_Y, hem - 0.001), 0.0655, 0.0725), ((s * LEG_X, LEG_Y, hem + 0.014), 0.0650, 0.0720)],
               tones("mustard_lit", "mustard_lit", "mustard_dark"), 0.003)
    # the cargo pocket, a plate on the outside of the leg with its flap and button drawn on it
    py = LEG_Y + 0.002
    slab(part, "cargo", bone, (s * (LEG_X + 0.058), py, 0.136), Y, Z, 0.034, 0.028, 0.008, 0.0075,
         panel("cargo", 1, 2, (py - 0.034, py + 0.034, 0.108, 0.164), (s, 0, 0), "mustard_dark"), 0.003, corner=0.005, normal=(s, 0, 0))
    part.block("shin", bone, X, Y, [((s * LEG_X, LEG_Y, 0.040), 0.043, 0.047), ((s * LEG_X, LEG_Y, hem + 0.006), 0.047, 0.052)], SKIN_T, 0.010)

    def station(y, half, z0, z1, cut):
        return rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)

    # his bare foot: low at the toes, rising to the ankle
    foot = [(-0.116, 0.036, 0.020, 0.036, 0.006), (-0.104, 0.050, 0.020, 0.042, 0.010), (-0.060, 0.053, 0.020, 0.050, 0.012),
            (0.000, 0.050, 0.020, 0.058, 0.012), (0.050, 0.046, 0.020, 0.056, 0.012), (0.074, 0.036, 0.020, 0.046, 0.006)]
    part.loft("foot", bone, [station(*row) for row in foot], SKIN_T)
    # the straps: over the toes, over the instep, round the heel
    part.block("sandal-strap", bone, X, Z, [((s * LEG_X, -0.097, 0.0360), 0.0565, 0.0125), ((s * LEG_X, -0.067, 0.0390), 0.0575, 0.0150)],
               LEATHER_T, 0.004)
    part.block("sandal-strap", bone, X, Z, [((s * LEG_X, -0.032, 0.0410), 0.0565, 0.0170), ((s * LEG_X, 0.004, 0.0430), 0.0545, 0.0195)],
               LEATHER_T, 0.004)
    part.block("sandal-heel", bone, X, Z, [((s * LEG_X, 0.052, 0.0380), 0.0500, 0.0150), ((s * LEG_X, 0.081, 0.0360), 0.0420, 0.0130)],
               LEATHER_T, 0.004)
    # a steel stud on the outside of the instep strap
    part.block("sandal-stud", bone, Y, Z, [((s * (LEG_X + 0.054), -0.014, 0.040), 0.0055, 0.0055), ((s * (LEG_X + 0.0595), -0.014, 0.040), 0.0045, 0.0045)],
               STEEL_T, 0.0015)
    # the footbed, leather, and under it the dark sole, a little wider all round
    plan = lambda grow: [(-0.128 - grow, 0.040 + grow), (-0.108, 0.058 + grow), (-0.050, 0.0625 + grow), (0.060, 0.058 + grow), (0.088 + grow, 0.044 + grow)]
    part.block("footbed", bone, X, Z, [((s * LEG_X, y, 0.0155), hx, 0.0065) for y, hx in plan(0.0)], LEATHER_T, 0.003)
    part.block("sole", bone, X, Z, [((s * LEG_X, y, 0.0050), hx, 0.0050) for y, hx in plan(0.003)], tones("dark_lit", "dark", "dark_deep"), 0.002)


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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign (kuya_boy)"}
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
    print("triangles %d  (budget %d)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if abs(hi.z - HEIGHT) > 0.004:
        raise SystemExit("the top is at %.4f, not the %.4f of his crown with the head at %.2f" % (hi.z, HEIGHT, HEAD_SCALE))
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
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_kuya_boy_textures.py")
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
