"""Build the Aling Nena redesign PROTOTYPE: Aling Nena redrawn as if she were one of the heroes.

    py -3 tools/author_character_redesign_aling_nena_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_aling_nena.py
    ... -- --out other.glb     (writes there instead and leaves the .blend alone; needs the atlas beside it)

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/aling_nena/aling_nena-redesign.glb
    ArtSource/aling_nena/redesign-20261006/aling_nena_redesign.blend
beside the atlas painted by the textures script.

THE BRIEF is docs/reports/character-redesign/classic-brief.md (owner, 2026-10-06, after he
approved bayan, bebang and lola_pacing as the pattern): the Classic street characters are
REDRAWN AS IF THEY WERE HEROES. Same person (who she is, her age, her colours, her recognisable
pieces) in the heroes' visual language. This file started as a copy of Bebang's (a woman on the
heroes' body proportions, the approved pattern nearest her) and every character-specific part
was rewritten.

A PROTOTYPE. NOTHING IN THE GAME LOADS IT. The folder is outside Resources, no roster row points
here, and character-female-e.glb, person_aling_nena.asset and every runtime script are not touched.

WHO SHE IS. ALING NENA, a Classic street character, a woman of about fifty: "She owns the corner
store, so she owns the rules. Nobody has ever argued a call twice" (ConvertedCharacterSelect);
"the auntie on an errand: brisk and purposeful, leaning in, arms pumping short and
business-like" (GaitStyles); "slow and very hard to knock down" (GAME_OVERVIEW.md: speed 2,
power 3, grit 5). The map's sari-sari store carries her name. No powers, no gear. An everyday
person, not a costume.

WHAT IS KEPT OF HER (the original is character-female-e.glb wearing person_aling_nena.asset):
her black hair pulled up into one big HIGH PONYTAIL that hangs down behind her head (her
silhouette); her tan skin and her ears; a WHITE short-sleeved top worn open over BOTTLE GREEN;
two brown buttons at its front; dark trousers; white footwear on a green sole; a calm closed
smile.
WHAT IS DESIGNED, THE HEROES' WAY (each is argued where it is built):
  * the heroes' body (their torso block, arm length and legs, on the same rig), a little broad
    at the hip: she is the hardest in the roster to knock down;
  * THE FACE in the heroes' hand: two ink blocks whose ONE cut is a level lid, one thin stroke
    of a smile, a blush (the textures script);
  * THE HAIR built as the heroes' is, in the heroes' BLACK: a crown mass, a side parting over
    her left eye with four chunky locks swept across her brow to her right, the sides pulled
    back over her ears, the back swept up to the tie, and the ponytail as a thick fall of four
    layered pieces;
  * THE BLOUSE as a proper piece: a white camp blouse with a front band each side piped in
    mint, a breast pocket, a green band and mint edge on each short sleeve, a tail that flares
    over her hips with a green hem;
  * things a woman who keeps a sari-sari store has: a green half APRON tied in a bow behind,
    with one wide pocket and the store's NOTEBOOK (the listahan, where the street's credit is
    written) standing in it; a PENCIL behind her right ear; small gold EARRINGS and one gold
    BANGLE; an orange SCRUNCHIE;
  * FOOTWEAR, decided deliberately: rubber SLIPPERS, a thick green sole with a broad white
    strap, her toes and heels bare. The game is played with a slipper, and hers are the pair
    by the store's door. (The original's were white blocks on a green sole.)
  THE ORANGE. On the original her hands and forearms are orange (slot 5): no garment, a
  palette slot landing on skin. It is kept as her accent and taken off large areas (it is the
  offence role hue): the scrunchie, the notebook's cover and a stripe on each slipper strap.
  Nothing of it is a hero's or another Classic's outfit: no towel, no fan, no headband, no
  necklace, no sneakers.

THE RULES OF SECTION 15.3, and where each lands in this file:
  1  the head is Dante's rounded `shaped` rows, the heroes' own numbers (`HEAD_ROWS`);
  2  THE FACE IS ONE FLAT PLANE: the front depth is one value, 0.165, from 0.400 to 0.604;
  3  the head is 0.84 of the cast's about the head joint, applied AFTER the UVs resolve
     (`HEAD_SCALE`, `build`). Everything on the head bone takes it;
  4  a cute face: no nose, flat skin, ink eyes, one stroke, a blush; nothing of her age drawn;
  8  A DRAWING ONLY ON A FACE THAT SQUARELY FACES ITS VIEW (`proj_square`): every chamfer,
     rounded corner and block end takes one flat tone.
and of section 13: no painted hair shine; cloth across two bones bends (`Part.bend`: the tail
of her blouse and her apron take the legs toward their hems); feet on zero; under 6,000
triangles. She has no collar: nothing stands round her jaw.

THE HAND, FOR THE FIRST-PERSON ARMS (Editor/ViewmodelArmAuthor.cs cuts them from this mesh and
sizes them by the fist, "the far 14 per cent of the arm"). Her arm runs along x from 0.098 to
0.296. The fist is one clear block, bare skin, from 0.240 to 0.296, so the whole of the far 14
per cent (from 0.268) is fist and nothing else; it is 98 by 116 mm across (Dante's is 116) and
the forearm behind it 84 by 90, so the wrist reads and nothing past it is wider than the fist.
Her bangle sits on the left wrist, behind the fist, and is narrower than it. `verify` holds
all of this.

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
import author_character_redesign_aling_nena_textures as tex  # noqa: E402  the island layout
import author_character_redesign_aling_nena_clips as clips  # noqa: E402  her own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/character-female-e.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/aling_nena")
OUT = os.path.join(FOLDER, "aling_nena-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/aling_nena/redesign-20261006/aling_nena_redesign.blend")
NAME = "aling_nena-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# TWO BONES THE CAST DOES NOT HAVE: an elbow in each arm, a child of the arm bone, appended AFTER
# the seven so their indices and names do not move, exactly as the heroes' are. A clip that does
# not key them leaves the arm straight. Hers sits at the mouth of her short sleeve.
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
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))   # the `head` bone of character-female-e.glb, in Blender space
SHARP_ANGLE = 40.0       # degrees: a chamfer is a hard edge, a soft corner or a taper is one form
TRIANGLE_BUDGET = 6000   # the original is under 900
ARM_END = tex.HAND_END   # the far end of her fist

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
# `shaped` carving the owner picked on Dante, the very rows the redesigned heroes use, so her
# face is the same size and shape of canvas as theirs.
#     rows are (z, half width, depth to the front, depth to the back, exponent)
# ---------------------------------------------------------------------------
HEAD_N = 24
HEAD_ROWS = [
    # soft superellipse corners, fullest at the cheeks (0.181 against 0.171 at the temples), a jaw
    # that rounds in gently and closes low (nothing of hers stands round the jaw to hide a hard
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
    """A superellipse ring. `grow(j)` scales a point about the centre (the skirt's pleats)."""
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
WHITE_T = tones("white_lit", "white", "white_shade")
GOLD_T = tones("gold_lit", "gold", "gold_dark")
BROWN_T = tones("brown_lit", "brown", "brown_dark")
ORANGE_T = tones("orange_lit", "orange", "orange_dark")
GREEN_T = tones("green_lit", "green", "green_dark")


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
    # Only the flat front takes the drawing (her face); the limit is 0.84 so the first facets
    # either side of the middle take it too. Every other face of the head is flat skin, and the
    # faces that turn under at the jaw are the SAME skin, not a shade (no dark facets).
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], proj_square("head", "skin", "skin", 0.84))
    # NO NOSE. HER EARS show (her hair is pulled back over them): the heroes' small blocks.
    for s in (1, -1):
        part.block("ear", "head", Y, Z, [((s * 0.166, 0.016, 0.456), 0.031, 0.042), ((s * 0.220, 0.020, 0.456), 0.024, 0.032)],
                   tones("skin", "skin", "skin_shade"), 0.011)
        # HER EARRINGS: one small gold drop under each lobe, a cut cube 22 mm across
        part.block("earring", "head", X, Y, [((s * 0.203, 0.018, 0.396), 0.011, 0.011), ((s * 0.203, 0.018, 0.420), 0.011, 0.011)],
                   GOLD_T, 0.005)
    # HER PENCIL, behind her right ear, the point forward: she keeps the street's credit in a
    # notebook and the pencil is never anywhere else. It lies on top of the ear under the hair
    # pulled back over it, tipped up a little toward the point.
    back, tip = Vector((-0.2360, 0.086, 0.5060)), Vector((-0.2300, -0.128, 0.5300))
    w = (tip - back).normalized()
    u = (X - w * X.dot(w)).normalized()
    v = w.cross(u).normalized()
    at = lambda k: tuple(back + (tip - back) * k)
    part.block("pencil-eraser", "head", u, v, [(at(0.0), 0.0088, 0.0088), (at(0.10), 0.0088, 0.0088)], ORANGE_T, 0.003)
    part.block("pencil", "head", u, v, [(at(0.10), 0.0082, 0.0082), (at(0.84), 0.0082, 0.0082)], GOLD_T, 0.003)
    part.block("pencil-point", "head", u, v, [(at(0.84), 0.0078, 0.0078), (at(1.0), 0.0022, 0.0022)], tones("cream", "cream", "cream_shade"), 0.0)
    part.block("pencil-lead", "head", u, v, [(at(0.955), 0.0036, 0.0036), (at(1.02), 0.0012, 0.0012)], flat("ink"), 0.0)


# ---------------------------------------------------------------------------
# THE HAIR, BUILT THE WAY THE HEROES' IS (Amihan's is the pattern): a crown mass, and on it
# chunky tapered LOCKS with real depth, each set by hand; a designed fringe; a back in layers.
# Hers is the heroes' BLACK, all of it pulled up and back into ONE HIGH PONYTAIL, the way a
# woman who works all day wears it and the way the original has it:
#   * A SIDE PARTING over her left eye. From it four locks are swept across her brow to her
#     RIGHT, each lower than the last, the fourth coming down past her right temple; one short
#     lock goes the other way to her left temple. Her forehead shows under the parting.
#   * a lock in front of each ear and the hair over each ear pulled back;
#   * the back of the head in three slabs SWEPT UP to the tie;
#   * THE TIE, an orange scrunchie high on the back of her crown;
#   * THE PONYTAIL: it leaves the tie upward, turns over and falls behind her head to the height
#     of her jaw. One thick core and three locks laid over it, of three lengths, and a loose end
#     each side at its foot.
# THREE TONES BY FACING AND BY LAYER, NOTHING DRAWN (rule 3): the locks that stand proud wear the
# lighter black with the palest tone on any face turned up; the masses behind them the shade.
# Every wedge: (name, base, tip, base half size, tip half size, which tones, chamfer).
# ---------------------------------------------------------------------------
HAIR_CHAMFER = 0.014
LIT = tones("hair_top", "hair_lit", "hair")
DARK = tones("hair_lit", "hair", "hair_dark")
DEEP = tones("hair", "hair_dark", "hair_under")
WEDGES = [
    # the fringe, hanging in front of the flat face (its front is at y -0.164), her left to her right
    ("fringe-left", (0.158, -0.170, 0.668), (0.180, -0.172, 0.566), (0.030, 0.026), (0.015, 0.014), DARK, 0.006),
    ("fringe-1", (0.092, -0.180, 0.676), (0.056, -0.186, 0.622), (0.036, 0.022), (0.024, 0.013), LIT, 0.006),
    ("fringe-2", (0.022, -0.183, 0.676), (-0.030, -0.190, 0.596), (0.044, 0.024), (0.028, 0.014), DARK, 0.006),
    ("fringe-3", (-0.060, -0.182, 0.676), (-0.112, -0.188, 0.566), (0.044, 0.024), (0.026, 0.014), LIT, 0.006),
    ("fringe-4", (-0.136, -0.174, 0.670), (-0.176, -0.176, 0.512), (0.036, 0.028), (0.016, 0.014), DARK, 0.006),
    # in front of each ear
    ("sideburn-right", (-0.200, -0.100, 0.640), (-0.199, -0.090, 0.524), (0.022, 0.036), (0.013, 0.016), DEEP, 0.008),
    ("sideburn-left", (0.200, -0.104, 0.640), (0.200, -0.094, 0.510), (0.022, 0.038), (0.014, 0.018), DARK, 0.008),
    # over each ear, pulled back and up toward the tie: these stop ABOVE the ear block (its top is at 0.498)
    ("side-right-fore", (-0.203, 0.004, 0.648), (-0.205, 0.030, 0.532), (0.023, 0.054), (0.019, 0.036), DARK, 0.010),
    ("side-right-aft", (-0.201, 0.112, 0.648), (-0.204, 0.134, 0.500), (0.024, 0.060), (0.018, 0.038), DEEP, 0.010),
    ("side-left-fore", (0.203, 0.002, 0.648), (0.205, 0.028, 0.528), (0.023, 0.054), (0.019, 0.038), DARK, 0.010),
    ("side-left-aft", (0.201, 0.112, 0.648), (0.204, 0.132, 0.496), (0.024, 0.058), (0.018, 0.040), DEEP, 0.010),
]
#   the layer over the back, swept UP to the tie: (name, foot, top, half size at the foot, at the top)
BACK_SLABS = [
    ("back-right", (-0.140, 0.214, 0.452), (-0.064, 0.224, 0.672), (0.054, 0.016), (0.036, 0.014)),
    ("back-mid", (0.002, 0.218, 0.436), (0.000, 0.228, 0.664), (0.058, 0.017), (0.040, 0.014)),
    ("back-left", (0.140, 0.214, 0.458), (0.064, 0.224, 0.672), (0.054, 0.016), (0.036, 0.014)),
]
#   THE PONYTAIL's core, from the tie: (y, z, half width, half thickness)
PONY = [(0.204, 0.636, 0.058, 0.044), (0.246, 0.692, 0.084, 0.052), (0.296, 0.712, 0.102, 0.056), (0.332, 0.668, 0.108, 0.054),
        (0.340, 0.590, 0.100, 0.050), (0.330, 0.516, 0.078, 0.038)]
#   the locks laid over it, down its outside, each to its own length: they, not the core, are
#   the foot of the ponytail, so from behind it ends in teeth and not in a square edge (v02)
PONY_LOCKS = [
    ("pony-lock-mid", (0.006, 0.352, 0.700), (0.012, 0.366, 0.430), (0.046, 0.030), (0.020, 0.014), LIT),
    ("pony-lock-right", (-0.076, 0.332, 0.692), (-0.088, 0.344, 0.474), (0.036, 0.030), (0.016, 0.012), DARK),
    ("pony-lock-left", (0.076, 0.332, 0.692), (0.084, 0.340, 0.402), (0.036, 0.030), (0.016, 0.012), DARK),
    ("pony-end-right", (-0.040, 0.324, 0.540), (-0.046, 0.316, 0.448), (0.030, 0.024), (0.012, 0.010), DEEP),
    ("pony-end-left", (0.044, 0.324, 0.540), (0.040, 0.314, 0.470), (0.030, 0.024), (0.012, 0.010), DEEP),
]


def build_hair(part):
    # the crown mass: its front edge overhangs the forehead, the fringe hangs from that edge
    part.block("hair-crown", "head", X, Y, [((0, 0, 0.596), 0.198, (0.176, 0.200)), ((0, 0, 0.704), 0.186, (0.166, 0.190))],
               DARK, HAIR_CHAMFER)
    # the mass down the back, one step deeper, so the slabs laid over it read as hair on hair
    part.block("hair-back", "head", X, Y, [((0, 0.178, 0.440), 0.194, 0.030), ((0, 0.176, 0.660), 0.200, 0.034)],
               DEEP, HAIR_CHAMFER)
    part.block("hair-nape-liner", "head", X, Y, [((0, 0.156, 0.400), 0.140, 0.010), ((0, 0.160, 0.456), 0.156, 0.011)],
               flat("hair_under"), 0.004)
    for name, base, tip, base_half, tip_half, paint, chamfer in WEDGES:
        part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, paint, chamfer)
    for name, foot, top, foot_half, top_half in BACK_SLABS:
        part.wedge("hair-" + name, "head", foot, top, foot_half, top_half, DARK, 0.008)

    # THE PONYTAIL's core: one thick fall, its rings square to the path it takes
    rings = []
    for k, (y, z, hw, ht) in enumerate(PONY):
        y0, z0 = PONY[max(k - 1, 0)][:2]
        y1, z1 = PONY[min(k + 1, len(PONY) - 1)][:2]
        along = Vector((0.0, y1 - y0, z1 - z0)).normalized()
        rings.append(rect_ring((0.0, y, z), X, along.cross(X).normalized(), hw, ht, 0.62 * ht))    # well rounded: v02's read as a box on her head
    part.loft("hair-ponytail", "head", rings, DEEP)
    for name, base, tip, base_half, tip_half, paint in PONY_LOCKS:
        part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, paint, 0.010)
    # THE TIE: an orange scrunchie round the foot of the ponytail, gathered (two fat rolls). It
    # sits BEHIND the crown, below its top: v01 to v03 had it level with the crown and from the
    # front it was an orange bar on top of her head, like the handle of a bag.
    w = Vector((0.0, 0.61, 0.79)).normalized()
    v = w.cross(X).normalized()
    for k, grow in ((0.0, 0.0), (0.020, 0.003)):
        at = Vector((0.0, 0.214, 0.634)) + w * k
        part.block("scrunchie", "head", X, v, [(tuple(at), 0.074 + grow, 0.054 + grow), (tuple(at + w * 0.020), 0.074 + grow, 0.054 + grow)],
                   ORANGE_T, 0.018)


# ---------------------------------------------------------------------------
# THE TORSO AND THE BLOUSE. The heroes' torso block, a little broad at the hip. A WHITE camp
# blouse worn open over a BOTTLE GREEN top, as the original has it, DESIGNED: the green shows in
# a long opening from her throat to her waist; down each side of it runs the blouse's front band,
# a step whiter and 3 mm proud, piped in mint on its inner edge, with one brown button low on
# each (the original's two); a breast pocket and the seams are drawn. Below the waist the blouse
# has a TAIL that flares over her hips with a green hem.
# HER APRON, over it: a green half apron on a waistband that goes all the way round and ties in
# a BOW in the small of her back. It covers her front from hip to hip, has a mint band along its
# hem and one wide pocket, and in the pocket, at her left, stands the store's NOTEBOOK.
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.343)
TORSO_LOW = (0.148, 0.090)     # half width, half depth at the hips
TORSO_HIGH = (0.136, 0.084)    # at the shoulders
TORSO_CY = -0.002
ARM_Y, ARM_Z = 0.006, 0.288    # the line her arms are built along (the arm bone is at z 0.288)
WAIST, TAIL_HEM, APRON_HEM = tex.WAIST, tex.TAIL_HEM, tex.APRON_HEM
SKIRT_FOLLOW = 0.55            # how much of a leg's swing a hem takes
SKIRT_N = 24
TAIL_TOP_R = (0.152, 0.094)    # the blouse's tail: half width, half depth at the waist
TAIL_HEM_R = (0.172, 0.111)    # and at its hem
APRON_OUT = 0.006              # how far the apron stands off the tail
APRON_WRAP = 100.0             # degrees each side of dead ahead that it covers
OPEN_TOP, OPEN_FOOT = (0.048, 0.346), (0.028, 0.204)    # the opening of the blouse: (half width, z)


def torso_half(z):
    t = (z - TORSO_Z[0]) / (TORSO_Z[1] - TORSO_Z[0])
    return (TORSO_LOW[0] + (TORSO_HIGH[0] - TORSO_LOW[0]) * t, TORSO_LOW[1] + (TORSO_HIGH[1] - TORSO_LOW[1]) * t)


def chest_y(z):
    return TORSO_CY - torso_half(z)[1]


def flare(z):
    """The tail's (half width, half depth) at a height; the apron hangs on the same cone below it."""
    f = (WAIST - z) / (WAIST - TAIL_HEM)
    return (TAIL_TOP_R[0] + (TAIL_HEM_R[0] - TAIL_TOP_R[0]) * f, TAIL_TOP_R[1] + (TAIL_HEM_R[1] - TAIL_TOP_R[1]) * f)


def skirt_weights(co):
    down = _ramp(WAIST - 0.016, APRON_HEM, co.z) ** 0.8
    leg = SKIRT_FOLLOW * down
    left = _smooth(_ramp(-0.070, 0.070, co.x))
    return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]


def build_torso(part):
    blouse = proj_square("torso", "white", "white_shade", 0.90, ends=(2, "white_shade"))
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, TORSO_Z[0]), *TORSO_LOW), ((0, TORSO_CY, TORSO_Z[1]), *TORSO_HIGH)],
               blouse, 0.014)
    # the seat of her trousers, between the legs under the tail
    part.block("pelvis", "torso", X, Y, [((0, 0.0, 0.110), 0.058, 0.066), ((0, 0.0, 0.180), 0.058, 0.066)], flat("plum_dark"), 0.0)
    # her neck, for when the head tips back and the chin lifts off the blouse
    neck = part.block("neck", "torso", X, Y, [((0, TORSO_CY, 0.326), 0.050, 0.048), ((0, TORSO_CY, 0.354), 0.048, 0.046)],
                      flat("skin_shade"), 0.008)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.332, 0.350, co.z)), ("head", 0.7 * _ramp(0.332, 0.350, co.z))])

    # THE GREEN TOP in the blouse's opening: plates, because the head of the opening is on the
    # torso block's chamfer, where no drawing may go. On it, her bare throat in a scoop neck with
    # a mint edge.
    def plate(half_top, top, half_foot, foot, proud):
        return [Vector((-half_top, chest_y(top) - proud, top)), Vector((half_top, chest_y(top) - proud, top)),
                Vector((half_foot, chest_y(foot) - proud, foot)), Vector((-half_foot, chest_y(foot) - proud, foot))]

    def plates(name, half_top, top, half_foot, foot, proud, tone):
        part.loft(name, "torso", [plate(half_top, top, half_foot, foot, -0.006), plate(half_top, top, half_foot, foot, proud)],
                  flat(tone), caps=(False, True))

    plates("top", OPEN_TOP[0] + 0.004, OPEN_TOP[1], OPEN_FOOT[0] + 0.004, OPEN_FOOT[1], 0.0020, "green")
    plates("top-edge", 0.041, 0.346, 0.030, 0.3040, 0.0032, "mint")
    plates("top-throat", 0.036, 0.346, 0.025, 0.3095, 0.0044, "skin_shade")

    # THE FRONT BANDS of the blouse, one each side of the opening, and the mint piping on the
    # inner edge of each. They run on a slant, so each is a block between two stations.
    def along(s, z, off):
        k = (z - OPEN_FOOT[1]) / (OPEN_TOP[1] - OPEN_FOOT[1])
        return (s * (OPEN_FOOT[0] + (OPEN_TOP[0] - OPEN_FOOT[0]) * k + off), chest_y(z) - 0.0005, z)

    for s in (1, -1):
        part.block("front-band", "torso", X * s, Y, [(along(s, 0.208, 0.0115), 0.0105, 0.0038), (along(s, 0.338, 0.0115), 0.0105, 0.0038)],
                   facing(AHEAD, "white_lit", "white_shade"), 0.0)
        part.block("front-piping", "torso", X * s, Y, [(along(s, 0.208, 0.0), 0.0032, 0.0052), (along(s, 0.338, 0.0), 0.0032, 0.0052)],
                   facing(AHEAD, "mint", "mint_shade"), 0.0)
        # one brown button low on each band (the original's two)
        cx, cy, cz = along(s, 0.246, 0.0115)
        slab(part, "button", "torso", (cx, cy - 0.0036, cz), X, Z, 0.0074, 0.0074, 0.002, 0.0042, BROWN_T, 0.002, corner=0.0034)

    # THE TAIL of the blouse, from the waist to 0.160, flaring, with a green hem band. Toward the
    # hem each side takes up to SKIRT_FOLLOW of its own leg (rule 7).
    def ring(z, inset=0.0):
        hx, hy = flare(z)
        return super_ring(z, hx - inset, hy - inset, hy - inset, 4.4, SKIRT_N, TORSO_CY)

    trim = TAIL_HEM + 0.012
    rings = [ring(WAIST + 0.002, 0.020), ring(WAIST, 0.0), ring(trim, 0.0), ring(trim - 0.0005, -0.003), ring(TAIL_HEM, -0.003),
             ring(TAIL_HEM + 0.003, 0.010), ring(WAIST - 0.010, 0.020)]

    def tail_paint(i, j):
        return flat(("white_shade", "white", "white_deep", "green", "green_deep", "white_deep")[i])

    tail = part.loft("blouse-tail", "torso", rings, tail_paint, caps=(False, False))
    part.bend(tail, skirt_weights)

    # THE APRON: sections round her front, each a closed outline (its face, its mint hem band, its
    # underside and its inside), from one hip to the other.
    # Its sections stand on the SAME 24 corners as the tail and the waistband (v02 to v04 walked
    # round by angle instead, and where the apron met the band the two outlines crossed in a wave).
    def around(j, z, out):
        hx, hy = flare(z)
        t = 2.0 * math.pi * j / SKIRT_N
        c, sn = math.cos(t), math.sin(t)
        return Vector((math.copysign(abs(c) ** (2.0 / 4.4), c) * (hx + out), TORSO_CY + math.copysign(abs(sn) ** (2.0 / 4.4), sn) * (hy + out), z))

    band = APRON_HEM + 0.011
    sections = []
    for j in [11.4] + list(range(12, 25)) + [24.6]:     # 12 is her right hip, 18 dead ahead, 24 her left hip
        sections.append([around(j, WAIST + 0.006, 0.001), around(j, WAIST + 0.004, APRON_OUT), around(j, band, APRON_OUT),
                         around(j, band - 0.0005, APRON_OUT + 0.003), around(j, APRON_HEM, APRON_OUT + 0.003), around(j, APRON_HEM + 0.003, 0.0)])

    def apron_paint(i, j):
        return flat(("green_lit", "green", "green_dark", "mint", "green_deep", "green_deep")[j])

    apron = part.loft("apron", "torso", sections, apron_paint)
    part.bend(apron, skirt_weights)

    # THE POCKET, wide, across the apron's front, lying on its slope; a mint band along its mouth
    slope = math.atan2(TAIL_HEM_R[1] - TAIL_TOP_R[1], WAIST - TAIL_HEM)
    out = Vector((0.0, -math.cos(slope), math.sin(slope)))
    up = Vector((0.0, math.sin(slope), math.cos(slope)))

    def on_apron(x, z, proud=0.0):
        return Vector((x, TORSO_CY - flare(z)[1] - APRON_OUT, z)) + out * proud

    pieces = []
    # THE NOTEBOOK standing in it at her left: an orange cover, the cream edge of its pages on top
    pieces += slab(part, "notebook", "torso", on_apron(0.030, 0.176), X, up, 0.0210, 0.0180, 0.003, 0.0040, ORANGE_T, 0.002, corner=0.003, normal=out)
    pieces += slab(part, "notebook-pages", "torso", on_apron(0.030, 0.1955), X, up, 0.0180, 0.0030, 0.003, 0.0052, tones("cream", "cream", "cream_shade"), 0.001, normal=out)
    pieces += slab(part, "apron-pocket", "torso", on_apron(0.0, 0.143), X, up, 0.0640, 0.0250, 0.003, 0.0058, tones("green_lit", "green_mid", "green_deep"), 0.003, corner=0.007, normal=out)
    pieces += slab(part, "apron-pocket-band", "torso", on_apron(0.0, 0.1645), X, up, 0.0660, 0.0046, 0.003, 0.0080, tones("mint", "mint", "mint_shade"), 0.002, normal=out)
    part.bend(pieces, skirt_weights)

    # THE WAISTBAND, all the way round, 8 mm proud of the tail, and THE BOW in the small of her back
    def girdle(z, out):
        return super_ring(z, TAIL_TOP_R[0] + out, TAIL_TOP_R[1] + out, TAIL_TOP_R[1] + out, 4.4, SKIRT_N, TORSO_CY)

    part.loft("apron-band", "torso", [girdle(WAIST + 0.011, -0.010), girdle(WAIST + 0.010, 0.0085), girdle(WAIST - 0.010, 0.0085), girdle(WAIST - 0.0108, 0.0030)],
              lambda i, j: flat(("green_lit", "green_dark", "green_deep")[i]), caps=(False, False))
    back = TORSO_CY + TAIL_TOP_R[1] + 0.0085
    part.block("bow-knot", "torso", X, Z, [((0, back - 0.004, WAIST), 0.013, 0.012), ((0, back + 0.014, WAIST), 0.011, 0.010)], GREEN_T, 0.004)
    for s in (1, -1):
        part.wedge("bow-loop", "torso", (s * 0.008, back + 0.006, WAIST), (s * 0.060, back + 0.004, WAIST + 0.012), (0.008, 0.006), (0.021, 0.007),
                   tones("green_lit", "green_lit", "green_dark"), 0.004, across=Z)
        part.wedge("bow-tail", "torso", (s * 0.008, back + 0.006, WAIST - 0.006), (s * 0.032, back + 0.014, WAIST - 0.066), (0.010, 0.004), (0.014, 0.004),
                   tones("green_lit", "green", "green_dark"), 0.0)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests, at the heroes' length. `s` is +1 for her left.
# A short white sleeve on the arm bone with a green band round its mouth and a mint edge. The
# bare forearm and the fist ride the elbow bone. On her LEFT wrist a gold bangle, narrower than
# the fist.
# THE FIST is one clear block at the far end, bare skin, the widest thing past the wrist.
# ---------------------------------------------------------------------------
SLEEVE_END = tex.SLEEVE_END
FIST = (tex.WRIST, tex.HAND_END)


def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    arm_block(part, "sleeve", bone, s, [(0.098, (0.050, 0.052)), (0.136, (0.057, 0.060)), (SLEEVE_END, (0.059, 0.063))],
              proj_square(group, "white", "white_shade", 0.90, ends=(0, "white_deep")), 0.011)
    # the green band round the sleeve's mouth, 4 mm proud, and the mint edge under it
    arm_block(part, "sleeve-band", bone, s, [(SLEEVE_END - 0.020, (0.0625, 0.0665)), (SLEEVE_END - 0.002, (0.0635, 0.0675))], GREEN_T, 0.004)
    arm_block(part, "sleeve-edge", bone, s, [(SLEEVE_END - 0.003, (0.0600, 0.0640)), (SLEEVE_END + 0.002, (0.0590, 0.0630))],
              tones("mint", "mint", "mint_shade"), 0.0)
    # the forearm starts inside the sleeve's mouth, so no gap opens when the elbow folds
    arm_block(part, "forearm", fore, s, [(ELBOW_X - 0.016, (0.045, 0.049)), (FIST[0] + 0.006, (0.042, 0.045))],
              proj_square(group, "skin", "skin_shade", 0.93, ends=(0, "skin_shade")), 0.010)
    if s > 0:
        arm_block(part, "bangle", fore, s, [(FIST[0] - 0.024, (0.0455, 0.0490)), (FIST[0] - 0.010, (0.0455, 0.0490))], GOLD_T, 0.004)
    arm_block(part, "hand", fore, s, [(FIST[0], (0.049, 0.058)), (FIST[1], (0.049, 0.058))],
              proj_square(group, "skin", "skin_shade", 0.93), 0.013)


# ---------------------------------------------------------------------------
# THE LEGS. Short, the cast's. Plum trousers with a turned cuff, cut above the ankle; her bare
# ankles and feet; and HER SLIPPERS: a thick green rubber sole with a mint line round it and one
# broad white strap over the instep with an orange stripe across it. Her toes show in front of
# the strap and her heel behind it. Decided deliberately (see the docstring). Feet turned out
# four degrees.
# ---------------------------------------------------------------------------
LEG_X = 0.0836
TOE_OUT = 4.0


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    hem = tex.TROUSER_HEM
    part.block("trouser", bone, X, Y, [((s * LEG_X, 0.0, hem), 0.060, 0.067), ((s * (LEG_X - 0.006), 0.0, 0.184), 0.054, 0.064)],
               proj_square(group, "plum", "plum_dark", 0.90, ends=(2, "plum_deep")), 0.009)
    part.block("trouser-cuff", bone, X, Y, [((s * LEG_X, 0.0, hem - 0.001), 0.0630, 0.0700), ((s * LEG_X, 0.0, hem + 0.016), 0.0630, 0.0700)],
               tones("plum_lit", "plum_lit", "plum_dark"), 0.003)
    part.block("ankle", bone, X, Y, [((s * LEG_X, 0.008, 0.026), 0.039, 0.043), ((s * LEG_X, 0.004, hem + 0.004), 0.042, 0.047)],
               SKIN_T, 0.010)

    turn = math.radians(TOE_OUT) * s
    cs, sn = math.cos(turn), math.sin(turn)

    def turned(p):
        x, y = p.x - s * LEG_X, p.y - 0.02
        return Vector((s * LEG_X + x * cs - y * sn, 0.02 + x * sn + y * cs, p.z))

    def station(y, half, z0, z1, cut):
        return [turned(p) for p in rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)]

    top = tex.SOLE_TOP - 0.002
    # her foot: low and wide at the toes, rising to the instep, her heel behind
    foot = [(-0.124, 0.040, top, 0.034, 0.008), (-0.110, 0.054, top, 0.040, 0.012), (-0.044, 0.056, top, 0.047, 0.014),
            (0.052, 0.049, top, 0.045, 0.013), (0.068, 0.038, top, 0.040, 0.008)]
    part.loft("foot", bone, [station(*row) for row in foot], SKIN_T)
    # the strap: one broad white band over the instep, and the orange stripe across its middle
    strap = [(-0.096, 0.059, top, 0.047, 0.008), (-0.088, 0.0625, top, 0.052, 0.013), (-0.036, 0.0625, top, 0.056, 0.013),
             (-0.028, 0.059, top, 0.053, 0.008)]
    part.loft("slipper-strap", bone, [station(*row) for row in strap], WHITE_T)
    stripe = [(-0.071, 0.0640, top, 0.0548, 0.013), (-0.055, 0.0640, top, 0.0562, 0.013)]
    part.loft("slipper-stripe", bone, [station(*row) for row in stripe], ORANGE_T)
    # the sole: thick green rubber, wider than her foot all round, a mint line round its middle
    h = 0.5 * tex.SOLE_TOP
    sole = lambda grow: [((s * LEG_X, -0.140 - grow, 0), 0.050 + grow, 0), ((s * LEG_X, -0.106, 0), 0.068 + grow, 0),
                         ((s * LEG_X, 0.044, 0), 0.066 + grow, 0), ((s * LEG_X, 0.086 + grow, 0), 0.054 + grow, 0)]
    part.block("slipper-sole", bone, X, Z, [((c[0], c[1], h), hx, h) for c, hx, _ in sole(0.0)], GREEN_T, 0.005, move=turned)
    part.block("slipper-line", bone, X, Z, [((c[0], c[1], h + 0.001), hx, 0.0030) for c, hx, _ in sole(0.0016)], flat("mint"), 0.0, move=turned)


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
    """character-female-e.glb with its meshes swapped: skeleton, bind matrices and clips copied untouched,
    two elbows appended, and the four locomotion clips replaced by her own."""
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (aling_nena)"}
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
    print("triangles %d  (budget %d, the original is under 900)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    print("the top of her ponytail is at %.3f (Amihan's hair tops out at 0.715, Dante's at 0.716)" % hi.z)
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
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_aling_nena_textures.py")
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
