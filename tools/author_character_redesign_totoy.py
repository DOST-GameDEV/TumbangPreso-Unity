"""Build the Totoy redesign PROTOTYPE: Totoy redrawn as if he were one of the heroes.

    py -3 tools/author_character_redesign_totoy_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_totoy.py
    ... -- --out other.glb     (writes there instead and leaves the .blend alone; needs the atlas beside it)

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/totoy/totoy-redesign.glb
    ArtSource/totoy/redesign-20261006/totoy_redesign.blend
beside the atlas painted by the textures script.

WHY. Owner, 2026-10-06: the nine heroes were redesigned and are the game's models; the twelve
Classic street characters are REDRAWN AS IF THEY WERE HEROES (docs/CHARACTER_REDESIGN_DANTE.md
section 15.9 part D). Bayan, Bebang and Lola Pacing went first and were approved ("everything
looks good"). This is Totoy, one of the other nine, built on Bebang's files (a child on the
heroes' body). It is NOT the original with more detail: it is the same person (who he is, his
age, his colours, his recognisable pieces) DESIGNED IN THE HEROES' VISUAL LANGUAGE. For the
Classic cast the "nothing invented" and "measure the original's eyes" rules of section 13 are
LIFTED. HEAD_SCALE stays 0.84. Every other rule of sections 13 and 15.3 stands.

A PROTOTYPE. NOTHING IN THE GAME LOADS IT. The folder is outside Resources, no roster row points
here, and character-male-a.glb, person_totoy.asset and every runtime script are not touched.

WHO HE IS. TOTOY, a Classic street character: "the little boy of the street: all energy.
Bouncing, head bobbing, arms everywhere; the run flails" (GaitStyles.Totoy); speed 5, the top of
the roster's scale (GAME_OVERVIEW.md); "Raised barefoot on this street. Nobody in this town has
caught him twice" (ConvertedCharacterSelect). A boy of about nine. No powers, no gear.

WHAT IS KEPT OF HIM (the original is character-male-a.glb wearing person_totoy.asset): his deep
brown skin; his black hair with its scalloped fringe; HIS BIG ROUND GLASSES with orange rims,
the first thing anyone sees of him; his big ears; his green V-necked shirt; his slate shorts;
mustard on his feet over a white sole; his smile.
WHAT IS DESIGNED, THE HEROES' WAY (each is argued where it is built):
  * the heroes' body (Bebang's build of it), a little narrower: he is the smallest of them;
  * THE FACE in the heroes' hand: two upright ink blocks with ONE cut (the foot arched up, a
    laughing eye) and one thin grin. The eyes are seen THROUGH his glasses, drawn on the lenses;
  * THE GLASSES as two simple octagons: an orange rim, a pale lens, a bridge, temples back to
    the ears, and an elastic STRAP round the back of his head to keep them on while he runs;
  * THE HAIR as a GUPIT BAO, the bowl cut every barbershop on the street gives a small boy,
    built as the heroes' hair is: a domed mass, a rim of chunky locks all round it, the fringe
    in three broad scallops (the original's), the nape clipped short under the rim, and a
    cowlick standing up off the crown that no comb has ever beaten. Dante's black;
  * THE SHIRT as a liga jersey tee a size too big, a hand-me-down: mustard V neck, mustard
    sleeve bands with a white edge, a loose hem hanging over his shorts that swings with his
    legs, his number 5 small on the chest and big on the back;
  * THE SHORTS as slate basketball shorts to the knee, a white stripe and a mustard line down
    each outer side, a white hem;
  * one small thing that is HIS: a stack of rubber bands on his left wrist (the street's
    currency; boys win them off each other and wear the winnings);
  * FOOTWEAR, decided deliberately: mustard rubber TSINELAS on a white sole, his bare toes in
    them. He was "raised barefoot on this street", the original's feet were mustard over a
    white sole, and tumbang preso is played with a slipper. Nobody else in the row wears them
    (Amihan's are woven sandals, Bebang's canvas sneakers, Bayan's boots).
  Nothing of it is another hero's or another Classic's outfit. Bayan also wears green and has
  black hair: his is a collared work shirt under braces and a flat-top with spikes; Totoy's is
  a jersey with a number and a bowl cut under glasses.

THE RULES OF SECTION 15.3, and where each lands in this file:
  1  the head is Dante's rounded `shaped` rows, the heroes' own numbers (`HEAD_ROWS`);
  2  THE FACE IS ONE FLAT PLANE: the front depth is one value, 0.165, from 0.400 to 0.604;
  3  the head is 0.84 of the cast's about the head joint, applied AFTER the UVs resolve
     (`HEAD_SCALE`, `build`). Everything on the head bone takes it, his glasses too;
  4  a cute face: no nose, flat skin, ink eyes, one stroke, no blush (a boy);
  8  A DRAWING ONLY ON A FACE THAT SQUARELY FACES ITS VIEW (`proj_square`): every chamfer,
     rounded corner and block end takes one flat tone.
and of section 13: no painted hair shine; cloth across two bones bends (`Part.bend`: the shirt's
hem takes the legs); feet on zero; under 6,000 triangles. He has no collar: nothing stands
round his jaw.

THE HAND, FOR THE FIRST-PERSON ARMS (Editor/ViewmodelArmAuthor.cs cuts them from this mesh and
sizes them by the fist, "the far 14 per cent of the arm"). His arm runs along x from 0.098 to
0.296. The fist is one clear block, bare skin, from 0.240 to 0.296, so the whole of the far 14
per cent (from 0.268) is fist and nothing else; it is 98 by 116 mm across (Dante's is 116) and
the forearm behind it 84 by 90, so the wrist reads and nothing past it is wider than the fist.
His rubber bands sit on the wrist, behind the fist, and are narrower than it. `verify` holds all
of this.

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
import author_character_redesign_totoy_textures as tex  # noqa: E402  the island layout
import author_character_redesign_totoy_clips as clips  # noqa: E402  his own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/character-male-a.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/totoy")
OUT = os.path.join(FOLDER, "totoy-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/totoy/redesign-20261006/totoy_redesign.blend")
NAME = "totoy-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# TWO BONES THE CAST DOES NOT HAVE: an elbow in each arm, a child of the arm bone, appended AFTER
# the seven so their indices and names do not move, exactly as the heroes' are. A clip that does
# not key them leaves the arm straight. Hers sits at the mouth of the rolled sleeve.
ELBOW_X = 0.180
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
# THE HEAD IS 84 PER CENT OF THE ORIGINAL'S (owner, 2026-10-05: "smaller heads are better", "yes
# rebuild the heads at 84"). Everything is BUILT and PAINTED at the original's size, and only
# after the UVs are resolved is every vertex that rides the `head` bone drawn in toward the head
# JOINT (`build`).
HEAD_SCALE = 0.84
if "--head-scale" in sys.argv:      # for the one comparison render the report asks for
    HEAD_SCALE = float(sys.argv[sys.argv.index("--head-scale") + 1])
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))   # the `head` bone of character-male-a.glb, in Blender space
SHARP_ANGLE = 40.0       # degrees: a chamfer is a hard edge, a soft corner or a taper is one form
TRIANGLE_BUDGET = 6000   # the original is 820
ARM_END = tex.HAND_END   # the far end of his fist

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
# THE HEAD. The heroes' head: the donor skull the whole cast wears (0.343 to 0.661) in the
# `shaped` carving the owner picked on Dante, the very rows the redesigned heroes use, so his
# face is the same size and shape of canvas as theirs.
#     rows are (z, half width, depth to the front, depth to the back, exponent)
# ---------------------------------------------------------------------------
HEAD_N = 24
HEAD_ROWS = [
    # soft superellipse corners, fullest at the cheeks (0.181 against 0.171 at the temples), a jaw
    # that rounds in gently and closes low. THE FACE IS ONE FLAT PLANE: from 0.400 to 0.604 the
    # front depth is ONE value, 0.165. No brow ledge, no cheek standing proud.
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
    """A superellipse ring. `grow(j)` scales a point about the centre."""
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
WHITE_T = tones("white", "white", "white_shade")
GOLD_T = tones("gold_lit", "gold", "gold_dark")
ORANGE_T = tones("orange_lit", "orange", "orange_dark")


def slab(part, name, bone, centre, u, v, hu, hv, back, front, paint, cut=0.004, corner=0.0, normal=AHEAD):
    """A plate lying on a surface that faces `normal`: a ring `back` behind `centre`, its face
    `front` ahead of it with the edge cut. Three rings, no back face (it is sunk in what it lies on)."""
    n = Vector(normal)
    c = Vector(centre)
    rings = [rect_ring(c - n * back, u, v, hu, hv, corner),
             rect_ring(c + n * (front - cut), u, v, hu, hv, corner),
             rect_ring(c + n * front, u, v, hu - cut, hv - cut, max(corner - 0.4 * cut, 0.0))]
    return part.loft(name, bone, rings, paint, caps=(False, True))


def ribbon(part, name, bone, points, half_thick, half_tall, paint):
    """A flat band laid along `points` round the head: `half_thick` out from the head,
    `half_tall` up and down. The band's flat side always faces away from the head's middle."""
    pts = [Vector(p) for p in points]
    rings = []
    for i, p in enumerate(pts):
        t = pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]
        out = Vector((t.y, -t.x, 0.0)).normalized()
        if out.dot(Vector((p.x, p.y, 0.0))) < 0.0:
            out = -out
        rings.append(rect_ring(p, out, Z, half_thick, half_tall, 0.0))
    return part.loft(name, bone, rings, paint)


EAR_Z = 0.452


def build_head(part):
    # Only the flat front takes the drawing (his mouth); the limit is 0.84 so the first facets
    # either side of the middle take it too. Every other face of the head is flat skin, and the
    # faces that turn under at the jaw are the SAME skin, not a shade (no dark facets).
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], proj_square("head", "skin", "skin", 0.84))
    # NO NOSE. HIS EARS are his on the original, big and standing straight out, and his bowl cut
    # stops above them: the heroes' ear block, a third bigger than theirs.
    for s in (1, -1):
        part.block("ear", "head", Y, Z, [((s * 0.166, 0.016, EAR_Z), 0.036, 0.048), ((s * 0.230, 0.024, EAR_Z), 0.027, 0.036)],
                   tones("skin_lit", "skin", "skin_shade"), 0.012)


# ---------------------------------------------------------------------------
# THE HAIR, BUILT THE WAY THE HEROES' IS AND CUT HIS OWN WAY: A GUPIT BAO, the bowl cut. Dante's
# black, three tones by facing, nothing drawn. From the top down:
#   * a COWLICK of two locks standing up and back off the crown;
#   * the DOME: one mass, widest at its rim and rounding in to the top;
#   * the RIM, chunky tapered locks hung all round the dome, each set by hand: a FRINGE of three
#     broad scallops (the original's three, cut high so his forehead shows over his glasses), a
#     lock at each front corner, two over each ear that stop ABOVE it, three down the back;
#   * under the rim, the CLIPPED NAPE: a thin shell of hair cut to the skin, in a warmer dark,
#     from behind his sideburns round the back of his head. The line between the bowl and the
#     clipped hair is the haircut.
# Bayan's black hair is a flat-top with five spikes and Dante's a long heavy fringe; this is
# round, short and high, with the ears and the back of the neck bare.
# Every wedge: (name, base, tip, base half size, tip half size, which tones, chamfer).
# ---------------------------------------------------------------------------
HAIR_CHAMFER = 0.020
HAIR_T = tones("hair_top", "hair", "hair_under")
DEEP_T = tones("hair", "hair_under", "hair_under")
RIM = 0.610        # the dome's foot
WEDGES = [
    # the fringe, hanging in front of the flat face (its front is at y -0.164), his left to his right
    ("fringe-l", (0.116, -0.178, 0.676), (0.124, -0.186, 0.578), (0.052, 0.022), (0.026, 0.013), HAIR_T, 0.007),
    ("fringe-m", (0.006, -0.182, 0.680), (0.006, -0.190, 0.592), (0.062, 0.024), (0.036, 0.014), HAIR_T, 0.007),
    ("fringe-r", (-0.110, -0.178, 0.676), (-0.116, -0.186, 0.586), (0.052, 0.022), (0.024, 0.013), HAIR_T, 0.007),
    # the front corners, hugging the temples
    ("corner-l", (0.172, -0.150, 0.670), (0.176, -0.150, 0.560), (0.024, 0.040), (0.014, 0.022), HAIR_T, 0.008),
    ("corner-r", (-0.172, -0.150, 0.670), (-0.176, -0.150, 0.552), (0.024, 0.040), (0.014, 0.022), HAIR_T, 0.008),
    # over each ear: these stop just ABOVE the ear block (its top is at 0.502) and his temples
    ("side-l-fore", (0.194, -0.044, 0.660), (0.196, -0.040, 0.530), (0.022, 0.060), (0.017, 0.044), HAIR_T, 0.010),
    ("side-l-aft", (0.194, 0.084, 0.660), (0.196, 0.090, 0.518), (0.023, 0.066), (0.017, 0.050), HAIR_T, 0.010),
    ("side-r-fore", (-0.194, -0.044, 0.660), (-0.196, -0.040, 0.524), (0.022, 0.060), (0.017, 0.044), HAIR_T, 0.010),
    ("side-r-aft", (-0.194, 0.084, 0.660), (-0.196, 0.090, 0.522), (0.023, 0.066), (0.017, 0.050), HAIR_T, 0.010),
    # THE CROWN'S LAYERS: five broad locks lying on the dome, combed out from the whorl where
    # the cowlick stands, each ending short of the rim so the dome reads round and layered
    ("top-fl", (0.046, -0.004, 0.752), (0.084, -0.150, 0.704), (0.050, 0.018), (0.056, 0.014), HAIR_T, 0.009),
    ("top-fr", (-0.050, -0.010, 0.750), (-0.080, -0.146, 0.698), (0.046, 0.018), (0.058, 0.014), HAIR_T, 0.009),
    ("top-l", (0.066, 0.052, 0.750), (0.172, 0.036, 0.700), (0.058, 0.018), (0.070, 0.014), HAIR_T, 0.009, Y),
    ("top-r", (-0.062, 0.046, 0.748), (-0.170, 0.030, 0.696), (0.056, 0.018), (0.068, 0.014), HAIR_T, 0.009, Y),
    ("top-b", (0.000, 0.090, 0.748), (0.004, 0.186, 0.700), (0.070, 0.018), (0.084, 0.014), HAIR_T, 0.009),
    # down the back, over the back mass: the bowl's rim, the middle one a little longer
    ("back-r", (-0.130, 0.212, 0.672), (-0.134, 0.217, 0.516), (0.060, 0.016), (0.050, 0.014), HAIR_T, 0.009),
    ("back-m", (0.002, 0.216, 0.676), (0.002, 0.221, 0.502), (0.062, 0.017), (0.052, 0.014), HAIR_T, 0.009),
    ("back-l", (0.132, 0.212, 0.672), (0.136, 0.217, 0.522), (0.060, 0.016), (0.050, 0.014), HAIR_T, 0.009),
    # the cowlick
    ("cowlick-1", (0.022, 0.036, 0.740), (0.070, 0.104, 0.826), (0.034, 0.030), (0.011, 0.010), HAIR_T, 0.006),
    ("cowlick-2", (-0.020, 0.060, 0.740), (-0.046, 0.132, 0.796), (0.028, 0.026), (0.010, 0.009), HAIR_T, 0.006),
]
#   the clipped nape: a shell about a middle 30 mm behind the head's, so it is INSIDE the head
#   at the face and 4 mm proud of it from just ahead of the ears back. ITS LOWER EDGE IS A
#   HAIRLINE, not a level: low on the nape (0.404) and sweeping up to the temples (0.500).
#   (v01's level edge at 0.410 read as a dark band tied round his head.)
BUZZ_CY = 0.030
BUZZ_N = 32
BUZZ_HALF = [(0.343, 0.150), (0.400, 0.173), (0.432, 0.181), (0.470, 0.176), (0.520, 0.171), (0.604, 0.169)]   # the head's own half widths


def buzz_ring(f):
    """The shell's ring `f` of the way up from its hairline to under the bowl."""
    out = []
    for j in range(BUZZ_N):
        t = 2.0 * math.pi * j / BUZZ_N
        c, s = math.cos(t), math.sin(t)
        a = math.copysign(abs(c) ** (2.0 / 4.2), c)
        b = math.copysign(abs(s) ** (2.0 / 4.2), s)
        y = BUZZ_CY + b * (0.136 if b >= 0 else 0.150)
        foot = 0.404 + 0.096 * _smooth(_ramp(0.110, -0.050, y))
        z = foot + (0.604 - foot) * f
        out.append(Vector((a * (_table(BUZZ_HALF, z) + 0.004), y, z)))
    return out


def build_hair(part):
    # THE DOME: widest at its rim, rounding in to the top; its front edge overhangs the forehead
    part.block("hair-dome", "head", X, Y, [((0, 0.000, RIM), 0.194, (0.178, 0.198)), ((0, 0.002, 0.664), 0.196, (0.176, 0.198)),
                                           ((0, 0.008, 0.712), 0.176, (0.152, 0.178)), ((0, 0.014, 0.744), 0.122, (0.100, 0.124))],
               HAIR_T, HAIR_CHAMFER)
    # the mass down the back, one step deeper, so the locks laid over it read as hair on hair
    part.block("hair-back", "head", X, Y, [((0, 0.178, 0.528), 0.194, 0.030), ((0, 0.176, 0.664), 0.200, 0.034)], DEEP_T, 0.014)
    part.loft("hair-buzz", "head", [buzz_ring(f) for f in (0.0, 0.25, 0.5, 1.0)], flat("hair_buzz"), caps=(False, False))
    for name, base, tip, base_half, tip_half, paint, chamfer, *across in WEDGES:
        part.wedge("hair-" + name.split("-")[0], "head", base, tip, base_half, tip_half, paint, chamfer, *across)


# ---------------------------------------------------------------------------
# HIS GLASSES. Allowed because they are truly him: on the original two white octagons with an
# orange bar are the whole of his face. Here they are a simple DESIGNED shape and nothing more:
# two octagonal orange rims 9 mm wide standing 16 mm off the flat face, a pale lens sunk 5 mm in
# each, a bridge, a temple from each rim round the cheek to the top of the ear, and from behind
# each ear a mustard ELASTIC STRAP round the back of his head under the bowl's rim, with a
# white toggle in the middle. (The fastest boy on the street ties his glasses on.)
# THE EYES ARE DRAWN ON THE LENSES (`tex.paint_lens`): each lens's flat front is a swatch of
# its own, so the heroes' ink blocks sit where his eyes are and look out through the glass.
# ---------------------------------------------------------------------------
LENS_R = tex.LENS_R
RIM_R = LENS_R + 0.009
GLASS_Z = tex.LENS_AT[1]
STRAP_Z = 0.480


def build_glasses(part):
    def octagon(cx, r, y):
        return rect_ring((cx, y, GLASS_Z), X, Z, r, r, 0.586 * r)

    for s, swatch_name in ((1, "lensL"), (-1, "lensR")):
        cx = s * tex.LENS_AT[0]
        rings = [octagon(cx, RIM_R, FACE_Y + 0.004), octagon(cx, RIM_R, FACE_Y - 0.013), octagon(cx, RIM_R - 0.003, FACE_Y - 0.016),
                 octagon(cx, LENS_R, FACE_Y - 0.016), octagon(cx, LENS_R, FACE_Y - 0.010)]

        def rim_paint(i, j):
            return (ORANGE_T, flat("orange_lit"), flat("orange"), flat("orange_dark"))[i]

        part.loft("glasses-rim", "head", rings, rim_paint, caps=(False, False))
        window = (cx - LENS_R, cx + LENS_R, GLASS_Z - LENS_R, GLASS_Z + LENS_R)
        slab(part, "glasses-lens", "head", (cx, FACE_Y, GLASS_Z), X, Z, LENS_R + 0.003, LENS_R + 0.003, 0.0, 0.011,
             panel(swatch_name, 0, 2, window, AHEAD, "lens_edge"), 0.003, corner=0.586 * (LENS_R + 0.003))
        # the temple: round the cheek to the top of the ear
        ring = super_ring(GLASS_Z + 0.004, 0.185, 0.172, 0.170, 4.2, 40)
        path = [p for p in ring if p.y < 0.0 and p.x >= 0.142]
        path.sort(key=lambda p: p.y)
        path.insert(0, Vector((0.134, FACE_Y - 0.007, GLASS_Z + 0.004)))
        path.append(Vector((0.187, 0.018, GLASS_Z + 0.004)))
        ribbon(part, "glasses-temple", "head", [Vector((s * p.x, p.y, p.z)) for p in path], 0.0045, 0.0065, ORANGE_T)
    part.block("glasses-bridge", "head", Y, Z, [((-0.026, FACE_Y - 0.0095, GLASS_Z + 0.010), 0.005, 0.0065),
                                                ((0.026, FACE_Y - 0.0095, GLASS_Z + 0.010), 0.005, 0.0065)], ORANGE_T, 0.0)
    # the strap: from behind one ear round the back of his head to behind the other, lying on
    # the clipped hair just under the bowl's rim
    ring = super_ring(STRAP_Z, 0.183, 0.150, 0.139, 4.2, 40, BUZZ_CY)
    path = [p for p in ring if p.y >= 0.060]
    path.sort(key=lambda p: -math.atan2(p.y - BUZZ_CY, p.x))
    ribbon(part, "glasses-strap", "head", path, 0.0035, 0.0085, GOLD_T)
    back = max(p.y for p in path)
    part.block("glasses-toggle", "head", X, Z, [((0, back - 0.002, STRAP_Z), 0.013, 0.012), ((0, back + 0.009, STRAP_Z), 0.011, 0.010)],
               WHITE_T, 0.003)


# ---------------------------------------------------------------------------
# THE TORSO AND THE SHIRT. The heroes' torso block, narrower than Bebang's: he is the smallest
# of the row. A green LIGA JERSEY TEE a size too big for him, somebody's before it was his: a
# mustard V neck, and a HEM that hangs loose over his shorts, wider than his hips, and takes his
# legs as they swing (cloth across two bones bends). His number is drawn on it, front and back.
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.343)
TORSO_LOW = (0.130, 0.086)     # half width, half depth at the hips
TORSO_HIGH = (0.122, 0.082)    # at the shoulders
TORSO_CY = -0.002
ARM_Y, ARM_Z = 0.006, 0.288    # the line his arms are built along (the arm bone is at z 0.288)
HEM_TOP, HEM_FOOT = 0.210, 0.154
HEM_FOLLOW = 0.55              # how much of a leg's swing the hem takes
HEM_R = (0.150, 0.102)         # half width, half depth at the hem's foot


def torso_half(z):
    t = (z - TORSO_Z[0]) / (TORSO_Z[1] - TORSO_Z[0])
    return (TORSO_LOW[0] + (TORSO_HIGH[0] - TORSO_LOW[0]) * t, TORSO_LOW[1] + (TORSO_HIGH[1] - TORSO_LOW[1]) * t)


def chest_y(z):
    return TORSO_CY - torso_half(z)[1]


def hem_weights(co):
    down = _ramp(HEM_TOP - 0.010, HEM_FOOT, co.z) ** 0.8
    leg = HEM_FOLLOW * down
    left = _smooth(_ramp(-0.070, 0.070, co.x))
    return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]


def build_torso(part):
    shirt = proj_square("torso", "shirt", "shirt_deep", 0.90, ends=(2, "shirt_mid"))
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, TORSO_Z[0]), *TORSO_LOW), ((0, TORSO_CY, TORSO_Z[1]), *TORSO_HIGH)],
               shirt, 0.014)
    # his shorts' seat, between the legs under the hem
    part.block("pelvis", "torso", X, Y, [((0, 0.0, 0.104), 0.058, 0.066), ((0, 0.0, 0.180), 0.058, 0.066)], flat("navy_dark"), 0.0)
    # his neck, for when the head tips back and the chin lifts off the shirt
    neck = part.block("neck", "torso", X, Y, [((0, TORSO_CY, 0.326), 0.048, 0.046), ((0, TORSO_CY, 0.354), 0.046, 0.044)],
                      flat("skin_shade"), 0.008)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.332, 0.350, co.z)), ("head", 0.7 * _ramp(0.332, 0.350, co.z))])
    front = chest_y(0.300)

    # THE V NECK: skin, and round it a mustard rib. Plates, because the top of the V is on the
    # torso block's chamfer, where no drawing may go.
    def vee(half, top, point, y):
        return [Vector((-half, y, top)), Vector((half, y, top)), Vector((0.0, y, point))]

    part.loft("neck-rib", "torso", [vee(0.060, 0.346, 0.284, front + 0.006), vee(0.060, 0.346, 0.284, front - 0.0032)],
              flat("gold"), caps=(False, True))
    part.loft("neck-piping", "torso", [vee(0.0505, 0.346, 0.2955, front + 0.006), vee(0.0505, 0.346, 0.2955, front - 0.0041)],
              flat("white"), caps=(False, True))
    part.loft("neck-skin", "torso", [vee(0.046, 0.346, 0.3005, front + 0.006), vee(0.046, 0.346, 0.3005, front - 0.0050)],
              flat("skin_shade"), caps=(False, True))
    # the rib carries on over each shoulder and across the back of the neck
    part.block("neck-rib-back", "torso", X, Y, [((0, TORSO_CY, 0.340), 0.060, (0.079, 0.056)), ((0, TORSO_CY, 0.347), 0.058, (0.079, 0.054))],
               tones("gold", "gold", "gold_dark"), 0.004)

    # THE HEM. Its top is on the torso; toward its foot each side takes up to HEM_FOLLOW of its
    # own leg, and across the middle the two legs are mixed.
    def ring(z, f, inset=0.0):
        low = torso_half(HEM_TOP)
        hx = low[0] + 0.002 + (HEM_R[0] - low[0]) * f - inset
        hy = low[1] + 0.002 + (HEM_R[1] - low[1]) * f - inset
        return rect_ring((0, TORSO_CY, z), X, Y, hx, hy, 0.018)

    band = HEM_FOOT + 0.014
    rings = [ring(HEM_TOP + 0.004, 0.0, 0.014), ring(HEM_TOP, 0.0), ring(band, 0.82), ring(band - 0.0005, 0.82, -0.002),
             ring(HEM_FOOT, 1.0, -0.002), ring(HEM_FOOT + 0.002, 1.0, 0.008), ring(HEM_TOP - 0.010, 0.10, 0.016)]

    def hem_paint(i, j):
        if i == 0:
            return flat("shirt_lit")
        if i == 1:
            # the flat front, back and sides in the shirt's tone, the four corners a step down
            return flat("shirt" if j % 2 == 0 else "shirt_mid")
        if i == 2:
            return flat("shirt_deep")
        if i == 3:
            return flat("gold" if j % 2 == 0 else "gold_dark")       # the hem's mustard band, as on the sleeves
        return flat("shirt_deep")

    hem = part.loft("shirt-hem", "torso", rings, hem_paint, caps=(False, False))
    part.bend(hem, hem_weights)
    # and the white edge over the band, a thin ring standing 1.5 mm proud of the cloth
    # THE DRAWSTRING of his shorts, hanging out from under the hem: two white cords of two
    # lengths, each with a mustard knot. On the torso, in front of the shorts' seat.
    for x, foot in ((-0.013, 0.118), (0.011, 0.128)):
        part.block("drawstring", "torso", X, Y, [((x, -0.0725, foot), 0.0036, 0.0036), ((x * 0.7, -0.0700, HEM_FOOT + 0.010), 0.0036, 0.0036)],
                   WHITE_T, 0.0)
        part.block("drawstring-knot", "torso", X, Y, [((x, -0.0725, foot - 0.008), 0.0056, 0.0056), ((x, -0.0725, foot + 0.003), 0.0056, 0.0056)],
                   GOLD_T, 0.0)
    edge = part.loft("shirt-hem-edge", "torso", [ring(band + 0.0065, 0.74, -0.0015), ring(band + 0.0005, 0.82, -0.0035)],
                     flat("white"), caps=(False, False))
    part.bend(edge, hem_weights)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests, at the heroes' length. `s` is +1 for his left.
# A wide short sleeve on the arm bone, too big for his arm, with a mustard band at its mouth and
# a white edge under it. The bare forearm and the fist ride the elbow bone. On his LEFT wrist a
# stack of three rubber bands, red, mustard and white, narrower than the fist.
# THE FIST is one clear block at the far end, bare skin, the widest thing past the wrist.
# ---------------------------------------------------------------------------
SLEEVE_END = tex.SLEEVE_END
FIST = (tex.WRIST, tex.HAND_END)


def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    arm_block(part, "sleeve", bone, s, [(0.098, (0.050, 0.052)), (0.136, (0.058, 0.061)), (SLEEVE_END, (0.061, 0.065))],
              proj_square(group, "shirt", "shirt_deep", 0.90, ends=(0, "shirt_deep")), 0.011)
    # the mustard band, 4 mm proud, and the white edge under it
    arm_block(part, "sleeve-band", bone, s, [(SLEEVE_END - 0.024, (0.0645, 0.0685)), (SLEEVE_END - 0.002, (0.0655, 0.0695))],
              tones("gold_lit", "gold", "gold_dark"), 0.004)
    arm_block(part, "sleeve-edge", bone, s, [(SLEEVE_END - 0.003, (0.0620, 0.0660)), (SLEEVE_END + 0.003, (0.0610, 0.0650))],
              tones("white", "white", "white_shade"), 0.0)
    # the forearm starts inside the sleeve's mouth, so no gap opens when the elbow folds
    arm_block(part, "forearm", fore, s, [(ELBOW_X - 0.016, (0.043, 0.047)), (FIST[0] + 0.006, (0.041, 0.044))],
              proj_square(group, "skin", "skin_shade", 0.93, ends=(0, "skin_shade")), 0.010)
    if s > 0:
        for at, paint in ((0.034, tones("red", "red", "red_dark")), (0.024, tones("gold_lit", "gold", "gold_dark")),
                          (0.014, tones("white", "white", "white_shade"))):
            arm_block(part, "rubber-band", fore, s, [(FIST[0] - at, (0.0450, 0.0485)), (FIST[0] - at + 0.0075, (0.0450, 0.0485))],
                      paint, 0.0025)
    arm_block(part, "hand", fore, s, [(FIST[0], (0.049, 0.058)), (FIST[1], (0.049, 0.058))],
              proj_square(group, "skin", "skin_shade", 0.93), 0.013)


# ---------------------------------------------------------------------------
# THE LEGS. Short, the cast's. Slate BASKETBALL SHORTS down to the knee, loose, with a white
# hem; thin bare shins; and TSINELAS: his bare feet, the big toe set apart from the rest, on a
# rubber slipper with a white sole and a mustard top, a mustard strap from between the toes
# back to each side. Decided deliberately (see the docstring). Feet turned out four degrees.
# ---------------------------------------------------------------------------
LEG_X = 0.0836
TOE_OUT = tex.TOE_OUT


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    hem = tex.SHORTS_HEM
    part.block("shorts", bone, X, Y, [((s * LEG_X, 0.0, hem), 0.064, 0.071), ((s * (LEG_X - 0.006), 0.0, 0.184), 0.056, 0.066)],
               proj_square(group, "navy", "navy_dark", 0.90, ends=(2, "navy_dark")), 0.009)
    part.block("shorts-hem", bone, X, Y, [((s * LEG_X, 0.0, hem - 0.001), 0.0665, 0.0735), ((s * LEG_X, 0.0, hem + 0.011), 0.0665, 0.0735)],
               tones("white", "white", "white_shade"), 0.003)
    part.block("leg", bone, X, Y, [((s * LEG_X, 0.0, 0.040), 0.040, 0.044), ((s * LEG_X, 0.0, hem + 0.004), 0.044, 0.049)],
               SKIN_T, 0.010)

    turn = math.radians(TOE_OUT) * s
    cs, sn = math.cos(turn), math.sin(turn)

    def turned(p):
        x, y = p.x - s * LEG_X, p.y - 0.02
        return Vector((s * LEG_X + x * cs - y * sn, 0.02 + x * sn + y * cs, p.z))

    def station(y, half, z0, z1, cut, dx=0.0):
        return [turned(p) for p in rect_ring((s * (LEG_X + dx), y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)]

    top = tex.SOLE_TOP - 0.002
    # his foot: from the ball back to the heel, rising to the ankle
    foot = [(-0.098, 0.053, top, 0.046, 0.010), (-0.040, 0.055, top, 0.052, 0.011), (0.030, 0.052, top, 0.058, 0.011),
            (0.062, 0.046, top, 0.056, 0.010), (0.072, 0.038, top + 0.002, 0.050, 0.006)]
    part.loft("foot", bone, [station(*row) for row in foot], SKIN_T)
    # the big toe (inside) and the other four as one block (outside), a gap between them
    big = [(-0.136, 0.014, top, 0.036, 0.006), (-0.126, 0.021, top, 0.043, 0.009), (-0.094, 0.021, top, 0.045, 0.009)]
    part.loft("toe", bone, [station(*row, dx=-0.031) for row in big], SKIN_T)
    rest = [(-0.126, 0.022, top, 0.034, 0.006), (-0.116, 0.029, top, 0.040, 0.009), (-0.094, 0.029, top, 0.044, 0.009)]
    part.loft("toe", bone, [station(*row, dx=0.024) for row in rest], SKIN_T)

    # THE SLIPPER: a white sole and a mustard top layer, longer and wider than the foot all round
    def layer(z0, z1, grow):
        h = 0.5 * (z1 - z0)
        return [((s * LEG_X, -0.146 - grow, z0 + h), 0.046 + grow, h), ((s * LEG_X, -0.112, z0 + h), 0.064 + grow, h),
                ((s * LEG_X, 0.040, z0 + h), 0.061 + grow, h), ((s * LEG_X, 0.084 + grow, z0 + h), 0.050 + grow, h)]

    part.block("slipper-sole", bone, X, Z, layer(0.0, 0.0115, 0.002), tones("white", "white", "white_shade"), 0.004, move=turned)
    part.block("slipper-top", bone, X, Z, layer(0.011, tex.SOLE_TOP, 0.0), tones("gold_lit", "gold", "gold_dark"), 0.004, move=turned)
    # the strap: a post between the toes, and from it a band over the foot to each side
    strap = tones("gold_lit", "gold", "gold_dark")
    post = Vector((s * (LEG_X - 0.0065), -0.104, 0.0))

    def bar(name, a, b, half_w, half_h):
        a, b = Vector(a), Vector(b)
        w = (b - a).normalized()
        u = w.cross(Z).normalized()
        part.block(name, bone, u, Z, [(a, half_w, half_h), (b, half_w, half_h)], strap, 0.002, move=turned)

    part.block("slipper-post", bone, X, Y, [((post.x, post.y, top), 0.0055, 0.0055), ((post.x, post.y, 0.054), 0.0055, 0.0055)],
               strap, 0.002, move=turned)
    for side, reach, back in ((s, 0.0545, -0.034), (-s, 0.0545, -0.046)):
        end = Vector((s * LEG_X + side * reach, back, 0.0))
        bar("slipper-strap", (post.x, post.y, 0.0505), (end.x - side * 0.004, end.y, 0.0550), 0.0065, 0.0040)
        part.block("slipper-strap", bone, X, Y, [((end.x, end.y, top), 0.0045, 0.0075), ((end.x, end.y, 0.0585), 0.0045, 0.0075)],
                   strap, 0.002, move=turned)
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (totoy)"}
    gltf["extras"] = {"prototype": "character-redesign-20261006", "replaces": "nothing; not loaded by the game"}
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
    print("the top of his cowlick is at %.3f (Amihan's hair tops out at 0.715, Dante's at 0.716)" % hi.z)
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
    build_glasses(head)
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
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_totoy_textures.py")
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
