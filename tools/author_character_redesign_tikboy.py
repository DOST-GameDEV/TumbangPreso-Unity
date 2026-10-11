"""Build the Tikboy redesign PROTOTYPE: Tikboy redrawn as if he were one of the heroes.

    py -3 tools/author_character_redesign_tikboy_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_tikboy.py
    ... -- --out other.glb     (writes there instead and leaves the .blend alone; needs the atlas beside it)

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/tikboy/tikboy-redesign.glb
    ArtSource/tikboy/redesign-20261006/tikboy_redesign.blend
beside the atlas painted by the textures script.

WHY. Owner, 2026-10-06: the nine heroes were redesigned and are the game's models; the twelve
Classic street characters are REDRAWN AS IF THEY WERE HEROES (docs/reports/character-redesign/
classic-brief.md, docs/CHARACTER_REDESIGN_DANTE.md section 15.9 part D). Three went first as the
pattern (bayan, bebang, lola_pacing) and were approved. This file started as a copy of Bebang's
(a child on the heroes' body proportions) and every piece of the person was then drawn for him.
It is NOT the original with more detail: it is the same person (who he is, his age, his colours,
his recognisable pieces) designed in the heroes' visual language.

A PROTOTYPE. NOTHING IN THE GAME LOADS IT. The folder is outside Resources, no roster row points
here, and character-male-c.glb, person_tikboy.asset and every runtime script are not touched.

WHO HE IS. TIKBOY, a Classic street character, a neighbourhood boy of about eleven: "the
mischief-maker: hunched, quick small steps, eyes up; he scampers when he runs"
(GaitStyles.Tikboy); "always down to one slipper. Half the footwear, twice the throwing arm"
(ConvertedCharacterSelect); "fast and strong, and goes down easily" (GAME_OVERVIEW.md). No
powers, no gear. The original (character-male-c.glb wearing person_tikboy.asset) is the shared
"uniformed man" rig: a green peaked cap with a dark band and a brick chevron, copper hair, a
moustache, an olive short-sleeved shirt with a dark tie and a chevron on the chest, a dark belt
with a brick buckle, dark trousers, a dark band on the LEFT wrist.

WHAT IS KEPT OF HIM: his warm tan skin, his copper-brown hair, his big ears, the green peaked cap
with its dark band and brick chevron, the olive shirt, the dark tie, the chevron on his chest,
the dark belt and its brick buckle, dark legs, the band on his left wrist.
WHAT IS DESIGNED, THE HEROES' WAY (each is argued where it is built). The uniform is read as what
it would be on a boy of eleven in a Manila street: A SCHOOL UNIFORM, WORN BY THE KID WHO IS NEVER
IN CLASS.
  * the heroes' body and arm length (the Classic rig's arm runs to 0.384, his to 0.296);
  * THE FACE in the heroes' hand: two ink blocks, HIS CUT is one lid dropped half way (his
    right), the other eye wide open, and a smirk hooked up on the open eye's side. NO MOUSTACHE:
    it belonged to the shared rig's grown man, not to a boy;
  * THE HAIR built as the heroes' is: chunky spiked locks with depth escaping the cap at the
    front, over the ears, in two flicks out sideways and down the back; three tones of copper;
  * THE CAP as a proper piece, and WORN HIS WAY: pushed back off his forehead, cocked toward his
    left, peak tipped up. Band, stepped crown, top button, raised chevron badge;
  * THE SHIRT with an open collar, the tie pulled loose and hanging askew, a chest pocket with
    the chevron sewn on it, one shirt tail out over the belt;
  * dark knee shorts with a turned cuff (a boy's, not a guard's trousers), the belt and buckle;
  * a stack of RUBBER BANDS on his left wrist where the original has its dark band (street
    kids' currency and ammunition), and a wooden SLINGSHOT tucked in the back of his belt;
  * FOOTWEAR, decided deliberately: rubber TSINELAS, and A PAIR THAT DOES NOT MATCH (a dark one
    with a brick strap on his left foot, a pale one with a dark strap on his right). He is
    "always down to one slipper": he has thrown and lost so many that what is on his feet is
    two survivors of two pairs. Bare toes, a strap between them.
  Nothing of it is another hero's or another Classic's outfit (Bayan: suspenders and work
  trousers; Bebang: blouse, skirt and sneakers; Dante: a coat).

THE RULES OF SECTION 15.3, and where each lands in this file:
  1  the head is Dante's rounded `shaped` rows, the heroes' own numbers (`HEAD_ROWS`);
  2  THE FACE IS ONE FLAT PLANE: the front depth is one value, 0.165, from 0.400 to 0.604;
  3  the head is 0.84 of the cast's about the head joint, applied AFTER the UVs resolve
     (`HEAD_SCALE`, `build`). Everything on the head bone takes it, the cap included;
  4  a cute face: no nose, flat skin, ink eyes, one stroke. No blush (he is a boy);
  8  A DRAWING ONLY ON A FACE THAT SQUARELY FACES ITS VIEW (`proj_square`): every chamfer,
     rounded corner and block end takes one flat tone.
and of section 13: no painted hair shine; cloth across two bones bends (the nape tips give to
the torso); feet on zero; under 6,000 triangles. Nothing stands round his jaw.

THE HAND, FOR THE FIRST-PERSON ARMS (Editor/ViewmodelArmAuthor.cs cuts them from this mesh and
sizes them by the fist, "the far 14 per cent of the arm"). His arm runs along x from 0.098 to
0.296. The fist is one clear block, bare skin, from 0.240 to 0.296, so the whole of the far 14
per cent (from 0.268) is fist and nothing else; it is 98 by 116 mm across and the forearm behind
it 82 by 88, so the wrist reads and nothing past it is wider than the fist. His rubber bands sit
on the wrist, behind the fist, and are narrower than it. `verify` holds all of this.

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
from mathutils import Matrix, Vector

TOOLS = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(TOOLS)
sys.path.insert(0, TOOLS)
import build_person_voxel as bpv  # noqa: E402  the cast's glb reader and writer, not edited
import author_character_redesign_tikboy_textures as tex  # noqa: E402  the island layout
import author_character_redesign_tikboy_clips as clips  # noqa: E402  his own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/character-male-c.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/tikboy")
OUT = os.path.join(FOLDER, "tikboy-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/tikboy/redesign-20261006/tikboy_redesign.blend")
NAME = "tikboy-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# TWO BONES THE CAST DOES NOT HAVE: an elbow in each arm, a child of the arm bone, appended AFTER
# the seven so their indices and names do not move, exactly as the heroes' are. A clip that does
# not key them leaves the arm straight. His sits at the mouth of the sleeve.
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
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))   # the `head` bone of character-male-c.glb, in Blender space
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
    # that rounds in gently and closes low (nothing of his stands round the jaw to hide a hard
    # turn under). THE FACE IS ONE FLAT PLANE: from 0.400 to 0.604 the front depth is ONE value,
    # 0.165. No brow ledge, no cheek standing proud.
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
    """A superellipse ring. `grow(j)` scales a point about the centre (unused here)."""
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
    # Only the flat front takes the drawing (his face); the limit is 0.84 so the first facets
    # either side of the middle take it too. Every other face of the head is flat skin, and the
    # faces that turn under at the jaw are the SAME skin, not a shade (no dark facets).
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], proj_square("head", "skin", "skin", 0.84))
    # NO NOSE. HIS EARS show under the cap: the heroes' small blocks, a little bigger, because
    # big ears are his on the original.
    for s in (1, -1):
        part.block("ear", "head", Y, Z, [((s * 0.166, 0.016, 0.456), 0.034, 0.046), ((s * 0.226, 0.020, 0.456), 0.026, 0.035)],
                   tones("skin", "skin", "skin_shade"), 0.011)


# ---------------------------------------------------------------------------
# THE HAIR, BUILT THE WAY THE HEROES' IS: a crown mass, and on it chunky tapered LOCKS with real
# depth, each set by hand. His is COPPER BROWN and has not seen a comb: it escapes the cap
# everywhere the cap lets it.
#   * a FRINGE of five spiked locks under the peak, no two alike: the second and fourth long
#     and swept apart, the first and third short and kicked the other way (v01 and v02 hung
#     them straight and even, and they read as a row of teeth, Bebang's fringe in another colour);
#   * a short lock in front of each ear and the hair over each ear;
#   * A COWLICK that the cap cannot hold down, kicking up and out behind it on his right
#     (v01 had a flick out sideways over each ear: they read as a second pair of ears, pointed);
#   * the back in three chunky locks hanging from under the cap, and three short spikes at the
#     nape. Under them the nape is cropped (the deepest tone), a boy's haircut.
# THREE TONES BY FACING AND BY LAYER, NOTHING DRAWN: the locks that stand proud wear the light
# copper with a paler tone on any face turned up; the masses behind them wear the shade.
# Every wedge: (name, base, tip, base half size, tip half size, which tones, chamfer).
# ---------------------------------------------------------------------------
HAIR_CHAMFER = 0.012
LIT = tones("hair_top", "hair_lit", "hair")
DARK = tones("hair_lit", "hair", "hair_dark")
DEEP = tones("hair", "hair_dark", "hair_under")
WEDGES = [
    # the fringe, hanging in front of the flat face (its front is at y -0.164), his left to his right
    # (their roots step down toward his left, under the rolled cap: at one height the ones on his
    # left came up through the peak, v03)
    ("fringe-1", (0.122, -0.176, 0.630), (0.176, -0.186, 0.598), (0.040, 0.022), (0.012, 0.011), LIT, 0.006),
    ("fringe-2", (0.044, -0.180, 0.638), (0.094, -0.190, 0.546), (0.052, 0.023), (0.015, 0.012), DARK, 0.006),
    ("fringe-3", (-0.030, -0.183, 0.646), (-0.014, -0.192, 0.596), (0.038, 0.025), (0.013, 0.012), LIT, 0.006),
    ("fringe-4", (-0.106, -0.178, 0.654), (-0.148, -0.187, 0.552), (0.046, 0.022), (0.014, 0.012), DARK, 0.006),
    ("fringe-5", (-0.168, -0.166, 0.656), (-0.186, -0.174, 0.596), (0.024, 0.026), (0.011, 0.012), DARK, 0.006),
    # in front of each ear
    ("sideburn-right", (-0.200, -0.104, 0.640), (-0.200, -0.096, 0.530), (0.022, 0.038), (0.012, 0.016), DARK, 0.008),
    ("sideburn-left", (0.200, -0.108, 0.640), (0.200, -0.100, 0.540), (0.022, 0.038), (0.012, 0.016), DARK, 0.008),
    # over each ear: these stop ABOVE the ear block (its top is at 0.502)
    ("side-right-fore", (-0.203, 0.002, 0.648), (-0.206, 0.010, 0.528), (0.023, 0.052), (0.017, 0.034), DARK, 0.010),
    ("side-right-aft", (-0.201, 0.112, 0.648), (-0.206, 0.124, 0.506), (0.024, 0.060), (0.016, 0.036), DEEP, 0.010),
    ("side-left-fore", (0.203, 0.000, 0.648), (0.206, 0.008, 0.534), (0.023, 0.054), (0.017, 0.036), DARK, 0.010),
    ("side-left-aft", (0.201, 0.112, 0.648), (0.206, 0.122, 0.512), (0.024, 0.058), (0.016, 0.036), DEEP, 0.010),
    # the back, three chunky locks hanging from under the cap, each its own length
    ("back-right", (-0.130, 0.214, 0.640), (-0.142, 0.224, 0.492), (0.058, 0.018), (0.034, 0.013), DARK, 0.008),
    ("back-mid", (0.000, 0.220, 0.640), (0.008, 0.230, 0.472), (0.062, 0.018), (0.038, 0.013), LIT, 0.008),
    ("back-left", (0.132, 0.214, 0.640), (0.140, 0.224, 0.502), (0.056, 0.018), (0.032, 0.013), DARK, 0.008),
    # the nape: three short spikes kicking out
    ("nape-right", (-0.112, 0.190, 0.474), (-0.140, 0.218, 0.428), (0.048, 0.022), (0.016, 0.011), DEEP, 0.008),
    ("nape-mid", (0.006, 0.192, 0.474), (0.020, 0.226, 0.412), (0.050, 0.022), (0.016, 0.011), DARK, 0.008),
    ("nape-left", (0.118, 0.190, 0.474), (0.146, 0.216, 0.436), (0.046, 0.022), (0.015, 0.011), DEEP, 0.008),
]
#   the cowlick: (name, base, tip, base half size, tip half size), behind the cap on his right
FLICKS = [
    # (v02 and v03 threw the first one out past his ear and it showed from the front as a point)
    ("cowlick-1", (-0.090, 0.200, 0.620), (-0.112, 0.276, 0.700), (0.036, 0.022), (0.010, 0.008)),
    ("cowlick-2", (-0.030, 0.204, 0.612), (-0.038, 0.272, 0.662), (0.030, 0.020), (0.009, 0.008)),
]
NAPE_FOLLOW = 0.35     # how much of the shoulders a nape tip keeps when the head turns


def build_hair(part):
    # the crown mass: its front edge overhangs the forehead, the fringe hangs from that edge.
    # Most of it is under the cap, and from the cap's band up it draws in: the cap is cocked
    # twenty degrees and a square crown's corners came out through the band and the peak (v04).
    part.block("hair-crown", "head", X, Y, [((0, 0, 0.596), 0.198, (0.176, 0.200)), ((0, 0, 0.648), 0.198, (0.176, 0.200)),
                                            ((0, 0, 0.692), 0.150, (0.134, 0.150))],
               DARK, HAIR_CHAMFER)
    # the mass down the back, one step deeper, so the locks laid over it read as hair on hair
    part.block("hair-back", "head", X, Y, [((0, 0.178, 0.470), 0.196, 0.030), ((0, 0.176, 0.660), 0.200, 0.034)],
               DEEP, HAIR_CHAMFER)
    # the cropped nape under it
    part.block("hair-nape-liner", "head", X, Y, [((0, 0.156, 0.408), 0.146, 0.010), ((0, 0.160, 0.474), 0.156, 0.011)],
               flat("hair_under"), 0.004)
    for name, base, tip, base_half, tip_half, paint, chamfer in WEDGES:
        faces = part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, paint, chamfer)
        if name.startswith("nape"):
            part.bend(faces, lambda co: [("head", 1.0 - NAPE_FOLLOW * _ramp(0.474, 0.410, co.z)),
                                         ("torso", NAPE_FOLLOW * _ramp(0.474, 0.410, co.z))])
    for name, base, tip, base_half, tip_half in FLICKS:
        part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, LIT, 0.006)


# ---------------------------------------------------------------------------
# THE CAP. The original's green peaked cap with its dark band and brick chevron, as a proper
# piece: a dark band with a lip, a crown in three steps that stands taller at the front (a
# school cap, a little too big for him), a dark button on top, a stiff dark peak, and the
# chevron as a raised badge on the crown's front.
# WORN HIS WAY. It is built level and then turned as one piece about the top of his skull:
# tipped BACK a little (his fringe gets out under it), ROLLED toward his left, and COCKED so
# the peak points off to his front-left. Nobody who wears a cap like that is on his way to class.
# (v01 tipped it back ten degrees with a low crown: from the front the peak was a line, the band
# was hidden behind it and the whole thing read as a beret. Now the crown stands 45 mm taller at
# the front, the band is 40 mm deep and the peak dips, so all three show.)
# ---------------------------------------------------------------------------
CAP_PIVOT = Vector((0.0, 0.0, 0.664))
CAP_TURN = (Matrix.Rotation(math.radians(20.0), 3, "Z") @ Matrix.Rotation(math.radians(7.0), 3, "Y")
            @ Matrix.Rotation(math.radians(-4.0), 3, "X"))
CAP_AHEAD = tuple(CAP_TURN @ Vector((0.0, -1.0, 0.0)))
CHAR_T = tones("char_lit", "char", "char_dark")
GREEN_T = tones("shirt_lit", "shirt", "shirt_mid")
BRICK_T = tones("brick_lit", "brick", "brick_dark")


def capped(p):
    return CAP_PIVOT + CAP_TURN @ (Vector(p) - CAP_PIVOT)


def cap_ring(z, rx, rf, rb, lift=0.0):
    """A ring of the cap; `lift` raises its front, falling to nothing at the ears."""
    out = []
    for p in super_ring(z, rx, rf, rb, 4.6, HEAD_N, 0.006):
        out.append(capped((p.x, p.y, p.z + lift * max(0.0, 0.006 - p.y) / rf)))
    return out


def chevron(y, half, top, notch, mid, bottom, cx=0.0, move=None):
    pts = [(cx - half, y, top), (cx, y, notch), (cx + half, y, top), (cx + half, y, mid), (cx, y, bottom), (cx - half, y, mid)]
    return [move(p) if move else Vector(p) for p in pts]


def build_cap(part):
    part.loft("cap-band", "head", [cap_ring(0.648, 0.198, 0.188, 0.194), cap_ring(0.652, 0.210, 0.200, 0.206),
                                   cap_ring(0.692, 0.210, 0.200, 0.206), cap_ring(0.695, 0.204, 0.194, 0.200)],
              CHAR_T, caps=(False, False))
    # the crown's wall is a tone deeper than its top, so the cap is not one green mass with the shirt
    wall = tones("shirt", "shirt_mid", "shirt_deep")
    part.loft("cap-crown", "head", [cap_ring(0.693, 0.204, 0.194, 0.200), cap_ring(0.730, 0.210, 0.202, 0.202, 0.022),
                                    cap_ring(0.772, 0.202, 0.196, 0.188, 0.044), cap_ring(0.790, 0.170, 0.168, 0.156, 0.050)],
              lambda i, j: wall if i == 0 else GREEN_T, caps=(False, True))
    part.block("cap-button", "head", X, Y, [((0, 0.004, 0.792), 0.019, 0.019), ((0, 0.004, 0.806), 0.014, 0.014)],
               CHAR_T, 0.005, move=capped)
    # the size tag, sticking out of the band at the back where he never tucks it in
    part.block("cap-tag", "head", X, Z, [((0.034, 0.200, 0.704), 0.015, 0.013), ((0.034, 0.2135, 0.706), 0.015, 0.013)],
               tones("cream", "cream", "cream_shade"), 0.003, move=capped)
    # the peak: a stiff plate out of the band's front, narrowing and dipping toward its end
    part.block("cap-peak", "head", X, Z, [((0, -0.168, 0.664), 0.164, 0.010), ((0, -0.262, 0.650), 0.152, 0.009),
                                          ((0, -0.326, 0.632), 0.108, 0.008)],
               tones("char_lit", "char", "char_dark"), 0.006, move=capped)
    # the chevron badge, 12 mm proud of the crown's front
    part.loft("cap-badge", "head", [chevron(-0.180, 0.036, 0.800, 0.784, 0.764, 0.738, move=capped),
                                    chevron(-0.2100, 0.036, 0.800, 0.784, 0.764, 0.738, move=capped)],
              facing(CAP_AHEAD, "brick", "brick_dark"), caps=(False, True))


# ---------------------------------------------------------------------------
# THE TORSO AND THE SHIRT. The heroes' torso block, a little narrower than Bebang's: he is quick,
# not wide. An olive short-sleeved school shirt, DESIGNED, and worn by him:
#   * the collar open, two leaves lying on the chest either side of a V of skin;
#   * THE TIE, dark, pulled loose: its knot hangs under the V and the blade swings off to his
#     left and stops short, the way a tie does when it has been tied once this year;
#   * a chest pocket on his left with the brick chevron sewn on it (the original's chest mark);
#   * the dark belt with its brick buckle, and ONE SHIRT TAIL out over it on his right;
#   * tucked in the back of the belt, a wooden SLINGSHOT, its two prongs up, a rubber hanging.
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.343)
TORSO_LOW = (0.140, 0.086)     # half width, half depth at the hips
TORSO_HIGH = (0.131, 0.082)    # at the shoulders
TORSO_CY = -0.002
ARM_Y, ARM_Z = 0.006, 0.288    # the line his arms are built along (the arm bone is at z 0.288)
BELT_Z = (0.172, 0.198)


def torso_half(z):
    t = (z - TORSO_Z[0]) / (TORSO_Z[1] - TORSO_Z[0])
    return (TORSO_LOW[0] + (TORSO_HIGH[0] - TORSO_LOW[0]) * t, TORSO_LOW[1] + (TORSO_HIGH[1] - TORSO_LOW[1]) * t)


def chest_y(z):
    return TORSO_CY - torso_half(z)[1]


def plate(part, name, bone, outline, y_back, y_front, paint):
    """A flat piece cut to `outline` [(x, z)], standing from `y_back` to `y_front` on the chest."""
    rings = [[Vector((x, y, z)) for x, z in outline] for y in (y_back, y_front)]
    return part.loft(name, bone, rings, paint, caps=(False, True))


def build_torso(part):
    shirt = proj_square("torso", "shirt", "shirt_deep", 0.90, ends=(2, "shirt_mid"))
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, TORSO_Z[0]), *TORSO_LOW), ((0, TORSO_CY, TORSO_Z[1]), *TORSO_HIGH)],
               shirt, 0.014)
    # the seat of his shorts, between the legs
    part.block("pelvis", "torso", X, Y, [((0, 0.0, 0.110), 0.058, 0.066), ((0, 0.0, 0.180), 0.058, 0.066)], flat("char_dark"), 0.0)
    # his neck, for when the head tips back and the chin lifts off the shirt
    neck = part.block("neck", "torso", X, Y, [((0, TORSO_CY, 0.326), 0.050, 0.048), ((0, TORSO_CY, 0.354), 0.048, 0.046)],
                      flat("skin_shade"), 0.008)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.332, 0.350, co.z)), ("head", 0.7 * _ramp(0.332, 0.350, co.z))])
    front = chest_y(0.300)
    lit_front = facing(AHEAD, "shirt_lit", "shirt")

    # THE OPEN COLLAR: a V of skin, and a collar leaf lying either side of it
    plate(part, "neck-skin", "torso", [(-0.040, 0.346), (0.040, 0.346), (0.0, 0.300)], front + 0.006, front - 0.0030, flat("skin_shade"))
    for s in (1, -1):
        plate(part, "collar", "torso", [(s * 0.034, 0.347), (s * 0.082, 0.347), (s * 0.076, 0.306), (s * 0.006, 0.296)],
              front + 0.006, front - 0.0075, lit_front)
    # THE TIE, loose: the knot under the V, then the blade swung to his left, short, its end cut to a point
    dark_front = facing(AHEAD, "char", "char_dark")
    plate(part, "tie-knot", "torso", [(-0.015, 0.307), (0.019, 0.305), (0.015, 0.279), (-0.009, 0.281)], front + 0.004, front - 0.0125, dark_front)
    plate(part, "tie-blade", "torso", [(-0.008, 0.286), (0.014, 0.284), (0.052, 0.230), (0.040, 0.204), (0.014, 0.220)],
          front + 0.004, front - 0.0095, facing(AHEAD, "char_lit", "char_dark"))
    # one cream button showing under the tie
    slab(part, "button", "torso", (-0.002, chest_y(0.214) - 0.002, 0.214), X, Z, 0.0062, 0.0062, 0.002, 0.004,
         tones("cream", "cream", "cream_shade"), 0.002, corner=0.003)
    # THE POCKET on his left chest, a patch with a turned top edge, and the chevron sewn on it
    px, pz = 0.086, 0.256
    slab(part, "pocket", "torso", (px, chest_y(pz), pz), X, Z, 0.028, 0.025, 0.004, 0.0042, tones("shirt_lit", "shirt_mid", "shirt_deep"), 0.003, corner=0.004)
    slab(part, "pocket-edge", "torso", (px, chest_y(pz + 0.019), pz + 0.019), X, Z, 0.030, 0.0058, 0.004, 0.0066, tones("shirt_lit", "shirt_lit", "shirt"), 0.002)
    part.loft("pocket-badge", "torso", [chevron(chest_y(pz) - 0.002, 0.014, 0.266, 0.259, 0.251, 0.240, px),
                                        chevron(chest_y(pz) - 0.0085, 0.014, 0.266, 0.259, 0.251, 0.240, px)],
              facing(AHEAD, "brick", "brick_dark"), caps=(False, True))

    # THE BELT, a step wider than the shirt, and its buckle
    part.block("belt", "torso", X, Y, [((0, TORSO_CY, BELT_Z[0]), 0.1455, 0.0915), ((0, TORSO_CY, BELT_Z[1]), 0.1445, 0.0905)],
               CHAR_T, 0.012)
    by = TORSO_CY - 0.0915
    slab(part, "buckle", "torso", (0, by, 0.185), X, Z, 0.024, 0.018, 0.004, 0.0075, BRICK_T, 0.003, corner=0.005)
    slab(part, "buckle-inside", "torso", (0, by, 0.185), X, Z, 0.013, 0.008, 0.004, 0.0088, flat("brick_dark"), 0.002)
    # the shorts' belt loops: two in front, two behind
    for lx, n in ((0.070, AHEAD), (-0.034, AHEAD), (0.060, (0, 1, 0)), (-0.020, (0, 1, 0))):
        slab(part, "belt-loop", "torso", (lx, TORSO_CY + 0.0915 * n[1], 0.185), X, Z, 0.0065, 0.0165, 0.004, 0.0040,
             tones("char_lit", "char_lit", "char_dark"), 0.0015, normal=n)
    # ONE SHIRT TAIL out over the belt, on his right
    plate(part, "shirt-tail", "torso", [(-0.124, 0.206), (-0.046, 0.206), (-0.052, 0.166), (-0.090, 0.148), (-0.126, 0.170)],
          TORSO_CY - 0.080, by - 0.0065, facing(AHEAD, "shirt_lit", "shirt_mid"))

    # THE SLINGSHOT in the back of his belt, on his right: a forked stick, a brick rubber wound
    # round each prong and the sling hanging between them
    sx, sy = -0.066, TORSO_CY + 0.0915 + 0.010
    wood = tones("wood_lit", "wood", "wood_dark")
    part.block("slingshot", "torso", X, Y, [((sx, sy, 0.150), 0.0105, 0.0090), ((sx, sy, 0.222), 0.0115, 0.0095)], wood, 0.004)
    for k in (1, -1):
        tip = (sx + k * 0.034, sy + 0.003, 0.272)
        part.wedge("slingshot", "torso", (sx + k * 0.004, sy, 0.214), tip, (0.0095, 0.0090), (0.0075, 0.0075), wood, 0.003)
        part.block("slingshot-rubber", "torso", X, Y, [((tip[0] - k * 0.004, tip[1], 0.256), 0.0105, 0.0100), ((tip[0] - k * 0.002, tip[1], 0.266), 0.0100, 0.0100)],
                   flat("brick"), 0.0)
        part.wedge("slingshot-sling", "torso", (tip[0] - k * 0.004, sy + 0.012, 0.258), (sx, sy + 0.014, 0.232), (0.0045, 0.0030), (0.0060, 0.0030),
                   flat("char_dark"), 0.0)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests, at the heroes' length. `s` is +1 for his left.
# A short sleeve on the arm bone with a turned cuff. The bare forearm and the fist ride the
# elbow bone. On his LEFT wrist, where the original has its dark band: a stack of rubber bands,
# two dark and a brick one between them, narrower than the fist.
# THE FIST is one clear block at the far end, bare skin, the widest thing past the wrist.
# ---------------------------------------------------------------------------
SLEEVE_END = tex.SLEEVE_END
FIST = (tex.WRIST, tex.HAND_END)


def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    arm_block(part, "sleeve", bone, s, [(0.098, (0.050, 0.052)), (0.136, (0.056, 0.059)), (SLEEVE_END, (0.058, 0.062))],
              proj_square(group, "shirt", "shirt_deep", 0.90, ends=(0, "shirt_deep")), 0.011)
    # the turned cuff, 3 mm proud
    arm_block(part, "sleeve-cuff", bone, s, [(SLEEVE_END - 0.020, (0.0612, 0.0652)), (SLEEVE_END - 0.001, (0.0622, 0.0662))],
              tones("shirt_lit", "shirt_lit", "shirt"), 0.004)
    # the forearm starts inside the sleeve's mouth, so no gap opens when the elbow folds
    arm_block(part, "forearm", fore, s, [(ELBOW_X - 0.016, (0.044, 0.048)), (FIST[0] + 0.006, (0.041, 0.044))],
              proj_square(group, "skin", "skin_shade", 0.93, ends=(0, "skin_shade")), 0.010)
    if s > 0:
        for a, b, paint in ((0.036, 0.026, CHAR_T), (0.0245, 0.0155, BRICK_T), (0.014, 0.004, CHAR_T)):
            arm_block(part, "rubber-band", fore, s, [(FIST[0] - a, (0.0455, 0.0490)), (FIST[0] - b, (0.0455, 0.0490))], paint, 0.003)
    arm_block(part, "hand", fore, s, [(FIST[0], (0.049, 0.058)), (FIST[1], (0.049, 0.058))],
              proj_square(group, "skin", "skin_shade", 0.93), 0.013)


# ---------------------------------------------------------------------------
# THE LEGS. Short, the cast's. Dark knee shorts with a turned cuff, bare shins, and TSINELAS
# THAT DO NOT MATCH: on his left foot a dark one with a brick strap, on his right a pale one
# with a dark strap. He is "always down to one slipper", so the two he has on are what is left
# of two pairs. Each is a thick rubber sole, a bare foot on it with the big toe apart from the
# others, and a strap from between the toes back to each side. Feet turned out four degrees.
# ---------------------------------------------------------------------------
LEG_X = 0.0836
TOE_OUT = 4.0
SOLES = {1: (tones("char_lit", "char", "char_dark"), BRICK_T), -1: (tones("pale", "pale", "pale_shade"), CHAR_T)}


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    hem = tex.SHORTS_HEM
    part.block("shorts", bone, X, Y, [((s * LEG_X, 0.0, hem), 0.062, 0.068), ((s * (LEG_X - 0.006), 0.0, 0.184), 0.055, 0.064)],
               proj_square(group, "char", "char_dark", 0.90, ends=(2, "char_dark")), 0.009)
    part.block("shorts-cuff", bone, X, Y, [((s * LEG_X, 0.0, hem - 0.001), 0.0645, 0.0705), ((s * LEG_X, 0.0, hem + 0.012), 0.0645, 0.0705)],
               tones("char_lit", "char_lit", "char_dark"), 0.003)
    part.block("leg", bone, X, Y, [((s * LEG_X, 0.0, 0.040), 0.044, 0.048), ((s * LEG_X, 0.0, hem + 0.004), 0.047, 0.052)],
               SKIN_T, 0.010)

    turn = math.radians(TOE_OUT) * s
    cs, sn = math.cos(turn), math.sin(turn)

    def turned(p):
        p = Vector(p)
        x, y = p.x - s * LEG_X, p.y - 0.02
        return Vector((s * LEG_X + x * cs - y * sn, 0.02 + x * sn + y * cs, p.z))

    def station(y, half, z0, z1, cut):
        return [turned(p) for p in rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)]

    sole_paint, strap_paint = SOLES[s]
    top = tex.SOLE_TOP - 0.001
    h = 0.5 * tex.SOLE_TOP
    part.block("slipper-sole", bone, X, Z, [((s * LEG_X, -0.146, h), 0.048, h), ((s * LEG_X, -0.108, h), 0.064, h),
                                            ((s * LEG_X, 0.040, h), 0.062, h), ((s * LEG_X, 0.084, h), 0.050, h)],
               sole_paint, 0.004, move=turned)
    # the foot: heel and instep as one piece, then the big toe and the other toes with the strap's gap between
    foot = [(-0.094, 0.052, top, top + 0.027, 0.008), (-0.040, 0.053, top, top + 0.034, 0.010), (0.030, 0.052, top, top + 0.042, 0.010),
            (0.066, 0.045, top, top + 0.040, 0.010)]
    part.loft("foot", bone, [station(*row) for row in foot], SKIN_T)
    big, rest = s * (LEG_X - 0.031), s * (LEG_X + 0.022)
    part.block("toe-big", bone, X, Z, [((big, -0.090, top + 0.012), 0.018, 0.012), ((big, -0.134, top + 0.011), 0.017, 0.011)],
               SKIN_T, 0.006, move=turned)
    part.block("toes", bone, X, Z, [((rest, -0.090, top + 0.011), 0.030, 0.011), ((rest, -0.126, top + 0.009), 0.027, 0.009)],
               SKIN_T, 0.006, move=turned)
    # the strap: a post between the toes, and from it a band back over the foot to each side of the sole
    post = s * (LEG_X - 0.0105)
    part.block("slipper-post", bone, X, Y, [((post, -0.098, top), 0.0050, 0.0060), ((post, -0.098, top + 0.034), 0.0050, 0.0060)],
               strap_paint, 0.0, move=turned)
    for k in (1, -1):
        knuckle = turned((s * LEG_X + k * 0.043, -0.050, top + 0.0375))
        part.wedge("slipper-strap", bone, turned((post, -0.098, top + 0.0335)), knuckle, (0.0045, 0.0090), (0.0045, 0.0100), strap_paint, 0.0, across=Z)
        part.wedge("slipper-strap", bone, knuckle, turned((s * LEG_X + k * 0.058, -0.012, top + 0.006)), (0.0045, 0.0100), (0.0045, 0.0085), strap_paint, 0.0, across=Z)


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
    """character-male-c.glb with its meshes swapped: skeleton, bind matrices and clips copied untouched,
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (tikboy)"}
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
    print("the top of his cap is at %.3f (Amihan's hair tops out at 0.715, Dante's at 0.716)" % hi.z)
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
    build_cap(head)
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
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_tikboy_textures.py")
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
