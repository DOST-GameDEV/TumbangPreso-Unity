"""Build the Jun-Jun redesign PROTOTYPE: Jun-Jun redrawn as if he were one of the heroes.

    py -3 tools/author_character_redesign_jun_jun_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_jun_jun.py
    ... -- --out other.glb     (writes there instead and leaves the .blend alone; needs the atlas beside it)

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/jun_jun/jun_jun-redesign.glb
    ArtSource/jun_jun/redesign-20261006/jun_jun_redesign.blend
beside the atlas painted by the textures script.

WHY. Owner, 2026-10-06: the nine heroes were redesigned and are the game's models; the twelve
Classic street characters are REDRAWN AS IF THEY WERE HEROES. The brief every Classic agent
works from is docs/reports/character-redesign/classic-brief.md, and the three approved pattern
characters are bayan, bebang and lola_pacing. THIS IS NOT THE ORIGINAL WITH MORE DETAIL. It is
the same person (who he is, his age, his colours, his recognisable pieces) DESIGNED IN THE
HEROES' VISUAL LANGUAGE. The block, loft, UV and glb code below is the approved Bebang
prototype's, copied (one character, one set of files, nobody edits another's); every shape and
every number from the hair down is his own.

A PROTOTYPE. NOTHING IN THE GAME LOADS IT. The folder is outside Resources, no roster row points
here, and character-male-d.glb, person_jun_jun.asset and every runtime script are not touched.

WHO HE IS. JUN-JUN, a Classic street character: "the youngest on the street. Small, slippery,
and impossible to corner. Also impossible to keep upright" (ConvertedCharacterSelect), and "the
sleepy kid dragged out to play: a shuffle, head down, arms hardly bothering; a reluctant jog"
(GaitStyles.JunJun). Tatag 2, the lightest grit on the roster. A boy of about seven. No powers,
no gear. An everyday person, not a costume.

WHAT IS KEPT OF HIM (the original is character-male-d.glb wearing person_jun_jun.asset): his
fair skin; his short light reddish hair with three tufts standing up at the back of the crown;
his big ears; his NAVY JACKET over a WHITE SHIRT with a GOLD TIE; the white shirt cuffs showing
at his wrists; his small pleased smile. He is the only child on the street dressed like that.
WHAT IS DESIGNED, THE HEROES' WAY (each is argued where it is built). The idea that holds it
together: HE WAS DRESSED UP THAT MORNING (church, a christening, a school programme) AND HAS
BEEN OUT PLAYING IN IT SINCE. Everything his mother did is still there and everything is a
little undone:
  * the heroes' body: their torso block (his a little narrower: he is the smallest), their arm
    length, their legs, on the same rig;
  * THE FACE in the heroes' hand: two ink blocks whose ONE cut is the lid, taken off flat and
    drooping to the outside, the right eye a little further shut than the left; one short thin
    stroke of a smile (the textures script);
  * THE HAIR built as the heroes' is: a crown mass, a combed SIDE PART (his mother's work) with
    chunky fringe locks swept to his left and getting longer as they go, short sides clear of
    his ears, a layered back, and THE THREE TUFTS at the back of the crown as a cowlick no comb
    has beaten; three tones of light copper brown;
  * THE JACKET as a boy's blazer a size too big: lapels, a breast pocket with his handkerchief
    stuffed in it in two points, one gold button, pocket flaps, a skirt that hangs over his
    hips and takes his legs, a half belt with two gold buttons across the back;
  * THE TIE pulled loose and hanging OUTSIDE the jacket, askew; the shirt collar open round it;
  * THE SHIRT TAIL out under the jacket, one corner in front on his right and one behind;
  * a mint STAR STICKER on his right chest, the kind a teacher gives;
  * GREY SHORT TROUSERS with a pressed crease and a turned cuff, bare knees, LONG PALE SOCKS
    with a navy band at the top;
  * FOOTWEAR, decided deliberately: black leather school shoes with a strap and a small gold
    buckle and a thick grey sole. The shoes that go with the jacket, and nobody else in the
    cast wears black leather.
  Nothing of it is another hero's or another Classic's outfit.

THE RULES OF SECTION 15.3, and where each lands in this file:
  1  the head is Dante's rounded `shaped` rows, the heroes' own numbers (`HEAD_ROWS`);
  2  THE FACE IS ONE FLAT PLANE: the front depth is one value, 0.165, from 0.400 to 0.604;
  3  the head is 0.84 of the cast's about the head joint, applied AFTER the UVs resolve
     (`HEAD_SCALE`, `build`). Everything on the head bone takes it;
  4  a cute face: no nose, flat skin, ink eyes, one stroke; no blush, he is a boy;
  8  A DRAWING ONLY ON A FACE THAT SQUARELY FACES ITS VIEW (`proj_square`): every chamfer,
     rounded corner and block end takes one flat tone.
and of section 13: no painted hair shine; cloth across two bones bends (`Part.bend`: the skirt
of the jacket and the shirt tails take the legs, the nape tips give to the torso); feet on zero;
under 6,000 triangles. Nothing stands round his jaw: the jacket's collar lies flat.

THE HAND, FOR THE FIRST-PERSON ARMS (Editor/ViewmodelArmAuthor.cs cuts them from this mesh and
sizes them by the fist, "the far 14 per cent of the arm"). His arm runs along x from 0.098 to
0.296. The fist is one clear block, bare skin, from 0.240 to 0.296, so the whole of the far 14
per cent (from 0.268) is fist and nothing else; it is 98 by 116 mm across (Dante's is 116), the
sleeve behind it 94 by 101 and the shirt cuff between them narrower still, so the wrist reads
and nothing past it is wider than the fist. `verify` holds all of this.

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
import author_character_redesign_jun_jun_textures as tex  # noqa: E402  the island layout
import author_character_redesign_jun_jun_clips as clips  # noqa: E402  his own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/character-male-d.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/jun_jun")
OUT = os.path.join(FOLDER, "jun_jun-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/jun_jun/redesign-20261006/jun_jun_redesign.blend")
NAME = "jun_jun-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# TWO BONES THE CAST DOES NOT HAVE: an elbow in each arm, a child of the arm bone, appended AFTER
# the seven so their indices and names do not move, exactly as the heroes' are. A clip that does
# not key them leaves the arm straight. His sits where the jacket's sleeve is cut in two.
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
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))   # the `head` bone of character-male-d.glb, in Blender space
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
    # NO NOSE. HIS EARS show (his hair is cut short and clear of them): the heroes' small blocks, a little
    # bigger, because big ears are his on the original.
    for s in (1, -1):
        # (v01 had Bebang's ear, and under a boy's side hair it did not show from the front at
        # all; his are the original's big ears, so they are taller and stand 16 mm further out)
        part.block("ear", "head", Y, Z, [((s * 0.166, 0.012, 0.452), 0.036, 0.052), ((s * 0.244, 0.020, 0.454), 0.029, 0.042)],
                   tones("skin", "skin", "skin_shade"), 0.012)
        slab(part, "ear-inner", "head", (s * 0.210, -0.016, 0.452), X, Z, 0.016, 0.022, 0.006, 0.003, flat("skin_shade"), 0.001, corner=0.006)



# ---------------------------------------------------------------------------
# THE HAIR, BUILT THE WAY THE HEROES' IS: a crown mass, and on it chunky tapered LOCKS with real
# depth, each set by hand; a designed fringe; a back in layers. His is a LIGHT COPPER BROWN, cut
# short, and it tells the same story as his clothes: combed that morning, slept on since.
#   * A SIDE PART on his right. Left of it three fat locks lie combed over the crown, and the
#     FRINGE hangs from the crown's front edge in five locks, all swept toward his left, each
#     longer than the one before, the last one down beside his left eye;
#   * short sides: a small lock in front of each ear and two over it that stop above the ear,
#     so his big ears stand clear (they are his on the original);
#   * the back in three slabs lying DOWN to three short tips at the nape;
#   * THE COWLICK: the original's three tufts at the back of the crown, the middle one tallest,
#     now three bent locks standing up and tipping back. The piece that makes his outline his.
# THREE TONES BY FACING AND BY LAYER, NOTHING DRAWN (rule 3).
# Every wedge: (name, base, tip, base half size, tip half size, which tones, chamfer).
# ---------------------------------------------------------------------------
HAIR_CHAMFER = 0.014
LIT = tones("hair_top", "hair_lit", "hair")
DARK = tones("hair_lit", "hair", "hair_dark")
DEEP = tones("hair", "hair_dark", "hair_under")
WEDGES = [
    # the fringe, hanging in front of the flat face (its front is at y -0.164), his right to his left
    ("fringe-1", (-0.152, -0.176, 0.668), (-0.178, -0.181, 0.596), (0.030, 0.022), (0.015, 0.012), DARK, 0.006),
    ("fringe-2", (-0.068, -0.181, 0.676), (-0.030, -0.188, 0.604), (0.044, 0.022), (0.029, 0.014), LIT, 0.006),
    ("fringe-3", (0.012, -0.183, 0.678), (0.056, -0.190, 0.586), (0.046, 0.024), (0.029, 0.015), DARK, 0.006),
    ("fringe-4", (0.092, -0.180, 0.676), (0.130, -0.186, 0.568), (0.044, 0.023), (0.026, 0.014), LIT, 0.006),
    ("fringe-5", (0.162, -0.170, 0.668), (0.180, -0.176, 0.544), (0.028, 0.028), (0.014, 0.014), DARK, 0.006),
    # in front of each ear, short
    ("sideburn-right", (-0.200, -0.104, 0.640), (-0.199, -0.098, 0.540), (0.022, 0.040), (0.013, 0.018), DARK, 0.008),
    ("sideburn-left", (0.200, -0.108, 0.640), (0.200, -0.100, 0.532), (0.022, 0.038), (0.014, 0.019), DARK, 0.008),
    # over each ear: cut short, these stop well ABOVE the ear block (its top is at 0.504)
    ("side-right-fore", (-0.203, 0.002, 0.648), (-0.204, 0.006, 0.556), (0.023, 0.052), (0.018, 0.036), DARK, 0.010),
    ("side-right-aft", (-0.201, 0.112, 0.648), (-0.203, 0.120, 0.528), (0.024, 0.060), (0.018, 0.040), DEEP, 0.010),
    ("side-left-fore", (0.203, 0.000, 0.648), (0.204, 0.004, 0.550), (0.023, 0.054), (0.018, 0.038), DARK, 0.010),
    ("side-left-aft", (0.201, 0.112, 0.648), (0.203, 0.118, 0.522), (0.024, 0.058), (0.018, 0.040), DEEP, 0.010),
    # the nape: three short tips, each its own length
    ("nape-right", (-0.116, 0.190, 0.462), (-0.128, 0.202, 0.428), (0.052, 0.022), (0.028, 0.014), DEEP, 0.008),
    ("nape-mid", (0.004, 0.192, 0.462), (0.010, 0.208, 0.416), (0.054, 0.022), (0.030, 0.014), DEEP, 0.008),
    ("nape-left", (0.120, 0.190, 0.462), (0.130, 0.202, 0.432), (0.050, 0.022), (0.026, 0.013), DEEP, 0.008),
]
#   the layer over the back, lying DOWN: (name, top, tip, half size at the top, at the tip)
BACK_SLABS = [
    ("back-right", (-0.132, 0.224, 0.668), (-0.138, 0.216, 0.492), (0.058, 0.016), (0.044, 0.014)),
    ("back-mid", (0.002, 0.228, 0.668), (0.000, 0.220, 0.468), (0.060, 0.017), (0.046, 0.014)),
    ("back-left", (0.136, 0.224, 0.668), (0.140, 0.216, 0.484), (0.058, 0.016), (0.044, 0.014)),
]
#   the cowlick: (foot, bend, tip, half size at the foot, at the bend, at the tip)
COWLICK = [
    ((0.004, 0.092, 0.698), (0.010, 0.118, 0.776), (0.034, 0.178, 0.816), (0.040, 0.032), (0.032, 0.025), (0.011, 0.009)),
    ((0.082, 0.086, 0.696), (0.108, 0.108, 0.750), (0.150, 0.146, 0.770), (0.034, 0.030), (0.026, 0.022), (0.009, 0.008)),
    ((-0.072, 0.090, 0.696), (-0.084, 0.118, 0.738), (-0.082, 0.170, 0.752), (0.032, 0.028), (0.024, 0.020), (0.009, 0.008)),
]
NAPE_FOLLOW = 0.35     # how much of the shoulders a nape tip keeps when the head turns
PART_X = -0.104        # the side part, on his right
#   the combed layer: three fat locks lying over the crown from the part, swept toward his left
#   AND forward, each (y at the part, y at its tip, how far left it reaches, the height of its tip, tones)
#   (v01 had one raised block here and it read as a plank laid on his head; v02 had four thin
#   level locks and they read as slats)
COMBED = [(-0.086, -0.150, 0.186, 0.670, LIT), (0.010, -0.058, 0.198, 0.662, DARK), (0.104, 0.040, 0.188, 0.668, LIT)]


def build_hair(part):
    # the crown mass: its front edge overhangs the forehead, the fringe hangs from that edge
    part.block("hair-crown", "head", X, Y, [((0, 0, 0.596), 0.198, (0.176, 0.200)), ((0, 0, 0.704), 0.186, (0.166, 0.190))],
               DARK, HAIR_CHAMFER)
    # the combed layer, left of the part: a step 14 mm proud of the crown, its right edge the part
    for y0, y1, reach, tip_z, paint in COMBED:
        part.wedge("hair-combed", "head", (PART_X, y0, 0.706), (reach, y1, tip_z), (0.052, 0.024), (0.034, 0.015), paint, 0.010, across=Y)
    # the mass down the back, one step deeper, so the slabs laid over it read as hair on hair
    part.block("hair-back", "head", X, Y, [((0, 0.178, 0.450), 0.196, 0.030), ((0, 0.176, 0.660), 0.200, 0.034)],
               DEEP, HAIR_CHAMFER)
    part.block("hair-nape-liner", "head", X, Y, [((0, 0.156, 0.392), 0.146, 0.010), ((0, 0.160, 0.456), 0.156, 0.011)],
               flat("hair_under"), 0.004)
    for name, base, tip, base_half, tip_half, paint, chamfer in WEDGES:
        faces = part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, paint, chamfer)
        if name.startswith("nape"):
            part.bend(faces, lambda co: [("head", 1.0 - NAPE_FOLLOW * _ramp(0.462, 0.410, co.z)),
                                         ("torso", NAPE_FOLLOW * _ramp(0.462, 0.410, co.z))])
    for name, top, tip, top_half, tip_half in BACK_SLABS:
        part.wedge("hair-" + name, "head", top, tip, top_half, tip_half, DARK, 0.008)
    # THE COWLICK: each tuft is two pieces, a foot standing up and a tip bent back off it
    for foot, bend, tip, foot_half, bend_half, tip_half in COWLICK:
        part.wedge("hair-cowlick", "head", foot, bend, foot_half, bend_half, DARK, 0.007)
        part.wedge("hair-cowlick", "head", bend, tip, bend_half, tip_half, LIT, 0.005)


# ---------------------------------------------------------------------------
# THE TORSO AND THE JACKET. The heroes' torso block, a little narrower than theirs (he is the
# smallest on the street). A NAVY JACKET, his own, DESIGNED as a boy's blazer bought to be grown
# into: two lapels round a deep V, the white shirt and its open collar in the V, the gold tie
# pulled loose and hanging outside the jacket off its middle, one gold button, a breast pocket
# with his handkerchief pushed into it, a flap pocket at each hip, a skirt that hangs over his
# hips. On his right chest, a mint star sticker. Out from under the jacket, his shirt tail.
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.343)
TORSO_LOW = (0.136, 0.086)     # half width, half depth at the hips
TORSO_HIGH = (0.128, 0.082)    # at the shoulders
TORSO_CY = -0.002
ARM_Y, ARM_Z = 0.006, 0.288    # the line his arms are built along (the arm bone is at z 0.288)
HEM_TOP, HEM = tex.HEM_TOP, tex.HEM
HEM_FOLLOW = 0.50              # how much of a leg's swing the jacket's skirt takes
HEM_N = 24
HEM_TOP_R = (0.141, 0.091)     # half width, half depth where the skirt leaves the torso
HEM_R = (0.153, 0.096)         # at the hem


def torso_half(z):
    t = (z - TORSO_Z[0]) / (TORSO_Z[1] - TORSO_Z[0])
    return (TORSO_LOW[0] + (TORSO_HIGH[0] - TORSO_LOW[0]) * t, TORSO_LOW[1] + (TORSO_HIGH[1] - TORSO_LOW[1]) * t)


def chest_y(z):
    return TORSO_CY - torso_half(z)[1]


def back_y(z):
    return TORSO_CY + torso_half(z)[1]


def hem_weights(co):
    down = _ramp(HEM_TOP - 0.006, HEM - 0.030, co.z) ** 0.8
    leg = HEM_FOLLOW * down
    left = _smooth(_ramp(-0.070, 0.070, co.x))
    return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]


def plate(part, name, points, y0, y1, paint):
    """A flat piece cut to a drawn outline, lying on the chest: `points` are (x, z), from depth
    `y0` (sunk in what it lies on) out to `y1`."""
    return part.loft(name, "torso", [[Vector((x, y0, z)) for x, z in points], [Vector((x, y1, z)) for x, z in points]],
                     paint, caps=(False, True))


def build_torso(part):
    jacket = proj_square("torso", "navy", "navy_deep", 0.90, ends=(2, "navy_mid"))
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, TORSO_Z[0]), *TORSO_LOW), ((0, TORSO_CY, TORSO_Z[1]), *TORSO_HIGH)],
               jacket, 0.014)
    # the seat of his shorts, between the legs under the jacket
    part.block("pelvis", "torso", X, Y, [((0, 0.0, 0.110), 0.058, 0.066), ((0, 0.0, 0.180), 0.058, 0.066)], flat("grey_dark"), 0.0)
    # his neck, for when the head tips back and the chin lifts off the collar
    neck = part.block("neck", "torso", X, Y, [((0, TORSO_CY, 0.326), 0.050, 0.048), ((0, TORSO_CY, 0.354), 0.048, 0.046)],
                      flat("skin_shade"), 0.008)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.332, 0.350, co.z)), ("head", 0.7 * _ramp(0.332, 0.350, co.z))])
    front = chest_y(0.300)

    # THE SHIRT in the V of the jacket: white, to a point at 0.256. Plates, because the top of
    # the V is on the torso block's chamfer, where no drawing may go.
    # (v01's V stopped at 0.254 and was lost under his chin; the original's is the widest thing on him)
    plate(part, "shirt", [(-0.066, 0.346), (0.066, 0.346), (0.0, 0.232)], front + 0.006, front - 0.0030, flat("white"))
    # THE LAPELS, one a side, 3 mm proud of the shirt: from the neck out to a notch and down to
    # the point of the V. The one on his left laps over the one on his right, as the jacket does.
    for s, lift in ((-1, 0.0), (1, 0.0012)):
        outline = [(s * 0.056, 0.346), (s * 0.104, 0.343), (s * 0.092, 0.296), (s * 0.000, 0.228)]
        plate(part, "lapel", outline, front + 0.006, front - 0.0062 - lift, tones("navy_lit", "navy_lit", "navy"))
        # the shirt's collar point, open, lying on the lapel
        plate(part, "shirt-collar", [(s * 0.012, 0.336), (s * 0.060, 0.346), (s * 0.044, 0.296)], front + 0.004, front - 0.0092,
              tones("white", "white", "white_shade"))
    # THE TIE. Gold, short, a child's tie on an elastic. The knot has been pulled down off the
    # collar and to his left, and the blade hangs OUTSIDE the jacket, off the middle.
    plate(part, "tie-knot", [(-0.009, 0.326), (0.018, 0.326), (0.014, 0.306), (-0.005, 0.306)], front + 0.004, front - 0.0125,
          flat("gold_dark"))
    plate(part, "tie", [(-0.005, 0.308), (0.014, 0.308), (0.036, 0.246), (0.024, 0.224), (0.006, 0.240)], front + 0.004, front - 0.0108,
          tones("gold_lit", "gold", "gold_dark"))
    # ONE GOLD BUTTON on the lapped front edge (the edge itself is drawn on the island)
    for x, z in ((0.010, 0.2165),):
        slab(part, "button", "torso", (x, chest_y(z) - 0.001, z), X, Z, 0.0068, 0.0068, 0.002, 0.0042, GOLD_T, 0.002, corner=0.003)
    # THE BREAST POCKET on his left: a welt, and his handkerchief pushed into it, two points up
    px, pz = 0.088, 0.262
    for outline, tone in (([(0.068, 0.262), (0.080, 0.285), (0.092, 0.262)], "white_shade"),
                          ([(0.084, 0.262), (0.099, 0.290), (0.109, 0.262)], "white")):
        plate(part, "handkerchief", outline, front + 0.004, chest_y(pz) - 0.0032, flat(tone))
    slab(part, "pocket-welt", "torso", (px, chest_y(pz), pz), X, Z, 0.0250, 0.0050, 0.004, 0.0052, tones("navy_lit", "navy_lit", "navy_deep"), 0.002)
    # A FLAP POCKET at each hip
    for s in (1, -1):
        slab(part, "pocket-flap", "torso", (s * 0.084, chest_y(0.222), 0.222), X, Z, 0.0260, 0.0075, 0.004, 0.0040,
             tones("navy_lit", "navy_lit", "navy_deep"), 0.002, corner=0.003)
    # THE STAR STICKER on his right chest: two squares, one turned on the other, and a gold middle
    sx, sz = -0.090, 0.270
    for turn, proud in ((0.0, 0.0030), (45.0, 0.0034)):
        a = math.radians(turn)
        u, v = Vector((math.cos(a), 0.0, math.sin(a))), Vector((-math.sin(a), 0.0, math.cos(a)))
        slab(part, "sticker", "torso", (sx, chest_y(sz), sz), u, v, 0.0105, 0.0105, 0.003, proud, flat("mint"), 0.0012)
    slab(part, "sticker-middle", "torso", (sx, chest_y(sz), sz), X, Z, 0.0046, 0.0046, 0.003, 0.0046, flat("gold_lit"), 0.001, corner=0.002)

    # THE BACK: the jacket's collar lying flat under his nape, and the two buttons of the half belt
    slab(part, "jacket-collar", "torso", (0, back_y(0.330) - 0.004, 0.330), X, Z, 0.066, 0.011, 0.006, 0.0050,
         tones("navy_lit", "navy_lit", "navy_deep"), 0.003, corner=0.004, normal=(0, 1, 0))
    for s in (1, -1):
        slab(part, "belt-button", "torso", (s * 0.058, back_y(0.235), 0.235), X, Z, 0.0064, 0.0064, 0.002, 0.0042, GOLD_T, 0.002, corner=0.003,
             normal=(0, 1, 0))

    # THE SKIRT OF THE JACKET: it is a size too big, so it hangs past his hips, a little wider
    # at the hem, with a turned edge. Its top is on the torso; toward the hem each side takes up
    # to HEM_FOLLOW of its own leg (rule 7), and across the middle the two legs are mixed.
    def ring(z, f, inset=0.0):
        hx = HEM_TOP_R[0] + (HEM_R[0] - HEM_TOP_R[0]) * f - inset
        hy = HEM_TOP_R[1] + (HEM_R[1] - HEM_TOP_R[1]) * f - inset
        return super_ring(z, hx, hy, hy, 4.4, HEM_N, TORSO_CY)

    edge = HEM + 0.009
    rings = [ring(HEM_TOP + 0.003, 0.0, 0.014), ring(HEM_TOP, 0.0), ring(edge, 0.88), ring(edge - 0.0005, 0.88, -0.0025),
             ring(HEM, 1.0, -0.0025), ring(HEM + 0.003, 1.0, 0.012), ring(HEM_TOP - 0.012, 0.15, 0.022)]

    def hem_paint(i, j):
        if i == 0:
            return flat("navy_lit")
        if i == 1:
            # round the ring: the faces toward the front and the back wear the base, the sides the shade
            return flat("navy" if (j % (HEM_N // 2)) not in (0, HEM_N // 2 - 1) else "navy_mid")
        if i == 2:
            return flat("navy_deep")        # the step of the turned edge
        if i == 3:
            return flat("navy_lit")         # the turned edge
        return flat("navy_deep")

    hem = part.loft("jacket-skirt", "torso", rings, hem_paint, caps=(False, False))
    part.bend(hem, hem_weights)
    # the lapped front edge carried down the skirt
    y_top, y_hem = TORSO_CY - HEM_TOP_R[1] - 0.0004, TORSO_CY - HEM_R[1] - 0.0004
    faces = part.block("jacket-edge", "torso", X, Y, [((-0.0085, y_top, HEM_TOP - 0.001), 0.0022, 0.0030), ((-0.0130, y_hem, edge + 0.001), 0.0022, 0.0030)],
                       flat("navy_deep"), 0.0)
    part.bend(faces, hem_weights)
    # HIS SHIRT TAIL, out: one corner hanging below the jacket in front on his right, one behind
    # on his left. They start up inside the skirt and take the legs with it.
    for name, base, tip, base_half, tip_half in (
            ("shirt-tail", (-0.072, -0.082, HEM + 0.010), (-0.082, -0.074, HEM - 0.030), (0.030, 0.006), (0.016, 0.004)),
            ("shirt-tail-back", (0.046, 0.080, HEM + 0.010), (0.056, 0.072, HEM - 0.024), (0.034, 0.006), (0.018, 0.004))):
        faces = part.wedge(name, "torso", base, tip, base_half, tip_half, tones("white", "white", "white_shade"), 0.002)
        part.bend(faces, hem_weights)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests, at the heroes' length. `s` is +1 for his left.
# THE JACKET'S SLEEVE runs all the way to the wrist: the upper half on the arm bone, the lower
# half on the elbow bone, starting inside the upper's mouth so no gap opens when the elbow folds.
# Out of it comes HIS WHITE SHIRT CUFF, 12 mm of it (the white hems of the original), and then
# THE FIST: one clear block at the far end, bare skin, the widest thing past the wrist.
# ---------------------------------------------------------------------------
SLEEVE_END = tex.SLEEVE_END
FIST = (tex.WRIST, tex.HAND_END)


def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    arm_block(part, "sleeve", bone, s, [(0.098, (0.050, 0.052)), (0.136, (0.055, 0.058)), (ELBOW_X + 0.008, (0.0525, 0.0565))],
              proj_square(group, "navy", "navy_deep", 0.90, ends=(0, "navy_deep")), 0.011)
    arm_block(part, "sleeve-lower", fore, s, [(ELBOW_X - 0.016, (0.0475, 0.0515)), (SLEEVE_END + 0.002, (0.0470, 0.0505))],
              proj_square(group, "navy", "navy_deep", 0.93, ends=(0, "navy_deep")), 0.009)
    # the shirt cuff, out of the sleeve's mouth
    arm_block(part, "shirt-cuff", fore, s, [(SLEEVE_END - 0.004, (0.0440, 0.0475)), (FIST[0] + 0.005, (0.0440, 0.0475))],
              tones("white", "white", "white_shade"), 0.003)
    arm_block(part, "hand", fore, s, [(FIST[0], (0.049, 0.058)), (FIST[1], (0.049, 0.058))],
              proj_square(group, "skin", "skin_shade", 0.93), 0.013)


# ---------------------------------------------------------------------------
# THE LEGS. Short, the cast's. GREY SHORT TROUSERS with a pressed crease (drawn) and a turned
# cuff, bare knees, LONG PALE SOCKS pulled up with a navy band round the top, and BLACK LEATHER
# SCHOOL SHOES: a rounded polished toe, one strap over the instep with a small gold buckle on
# the outside, a thick grey sole with a dark welt. Decided deliberately: they are the shoes that
# go with a jacket and tie, and nobody else in the cast wears black leather (Dante's and
# Cheska's sneakers are white, Bebang's burgundy, Bayan's boots brown). Feet turned out four degrees.
# ---------------------------------------------------------------------------
LEG_X = 0.0836
TOE_OUT = 4.0


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    part.block("shorts", bone, X, Y, [((s * LEG_X, 0.0, tex.SHORTS_HEM), 0.060, 0.066), ((s * (LEG_X - 0.006), 0.0, 0.184), 0.054, 0.064)],
               proj_square(group, "grey", "grey_dark", 0.90, ends=(2, "grey_dark")), 0.009)
    part.block("shorts-cuff", bone, X, Y, [((s * LEG_X, 0.0, tex.SHORTS_HEM - 0.001), 0.0625, 0.0685), ((s * LEG_X, 0.0, tex.SHORTS_HEM + 0.012), 0.0625, 0.0685)],
               tones("grey_lit", "grey_lit", "grey_dark"), 0.003)
    part.block("leg", bone, X, Y, [((s * LEG_X, 0.0, 0.050), 0.044, 0.048), ((s * LEG_X, 0.0, tex.SHORTS_HEM + 0.004), 0.047, 0.052)],
               SKIN_T, 0.010)
    sock_top = tex.SOCK_TOP
    part.block("sock", bone, X, Y, [((s * LEG_X, 0.0, 0.050), 0.0480, 0.0520), ((s * LEG_X, 0.0, sock_top), 0.0490, 0.0535)],
               tones("sock", "sock", "sock_shade"), 0.004)
    part.block("sock-cuff", bone, X, Y, [((s * LEG_X, 0.0, sock_top - 0.018), 0.0515, 0.0560), ((s * LEG_X, 0.0, sock_top + 0.001), 0.0515, 0.0560)],
               tones("sock", "sock", "sock_shade"), 0.004)
    part.block("sock-band", bone, X, Y, [((s * LEG_X, 0.0, sock_top - 0.012), 0.0522, 0.0567), ((s * LEG_X, 0.0, sock_top - 0.006), 0.0522, 0.0567)],
               flat("navy"), 0.0)

    turn = math.radians(TOE_OUT) * s
    cs, sn = math.cos(turn), math.sin(turn)

    def turned(p):
        x, y = p.x - s * LEG_X, p.y - 0.02
        return Vector((s * LEG_X + x * cs - y * sn, 0.02 + x * sn + y * cs, p.z))

    def station(y, half, z0, z1, cut):
        return [turned(p) for p in rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)]

    top = tex.SOLE_TOP - 0.002
    # the leather upper: from behind the toe back to the heel, rising to the ankle
    upper = [(-0.104, 0.058, top, 0.048, 0.012), (-0.064, 0.060, top, 0.056, 0.012), (0.036, 0.061, top, 0.064, 0.011),
             (0.068, 0.059, top, 0.064, 0.011), (0.078, 0.050, top + 0.002, 0.056, 0.006)]
    part.loft("shoe", bone, [station(*row) for row in upper], proj_square(group, "shoe", "shoe_deep", 0.88))
    # the toe: round, a little wider than the upper, polished (it takes the light tone on top)
    cap = [(-0.138, 0.042, top, 0.034, 0.010), (-0.128, 0.058, top, 0.043, 0.014), (-0.112, 0.0625, top, 0.049, 0.015), (-0.100, 0.0620, top, 0.050, 0.014)]
    part.loft("shoe-toe", bone, [station(*row) for row in cap], tones("shoe_lit", "shoe", "shoe_deep"))
    # the strap over the instep, 2 mm proud, and its buckle on the outside
    part.block("shoe-strap", bone, X, Y, [((s * LEG_X, -0.072, 0.030), 0.0632, 0.0100), ((s * LEG_X, -0.070, 0.0600), 0.0622, 0.0100)],
               tones("shoe_lit", "shoe_lit", "shoe"), 0.003, move=turned)
    part.block("shoe-buckle", bone, Y, Z, [((s * (LEG_X + 0.0625), -0.071, 0.046), 0.0075, 0.0075), ((s * (LEG_X + 0.0668), -0.071, 0.046), 0.0060, 0.0060)],
               GOLD_T, 0.002, move=turned)
    # the sole: thick grey rubber, wider than the shoe all round, a dark welt round its top
    h = 0.5 * tex.SOLE_TOP
    sole = lambda grow: [((s * LEG_X, -0.138 - grow, 0), 0.048 + grow, 0), ((s * LEG_X, -0.104, 0), 0.066 + grow, 0),
                         ((s * LEG_X, 0.040, 0), 0.066 + grow, 0), ((s * LEG_X, 0.084 + grow, 0), 0.057 + grow, 0)]
    part.block("shoe-sole", bone, X, Z, [((c[0], c[1], h), hx, h) for c, hx, _ in sole(0.0)], tones("sole", "sole", "shoe_deep"), 0.005, move=turned)
    part.block("shoe-welt", bone, X, Z, [((c[0], c[1], tex.SOLE_TOP - 0.004), hx, 0.0030) for c, hx, _ in sole(0.0014)], flat("shoe_deep"), 0.0, move=turned)


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
    """character-male-d.glb with its meshes swapped: skeleton, bind matrices and clips copied untouched,
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (jun_jun)"}
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
    print("the tip of his cowlick is at %.3f (Amihan's hair tops out at 0.715, Dante's at 0.716)" % hi.z)
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
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_jun_jun_textures.py")
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
