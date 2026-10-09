"""Build the Inday redesign PROTOTYPE: Inday redrawn as if she were one of the heroes.

    py -3 tools/author_character_redesign_inday_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_inday.py
    ... -- --out other.glb     (writes there instead and leaves the .blend alone; needs the atlas beside it)

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/inday/inday-redesign.glb
    ArtSource/inday/redesign-20261006/inday_redesign.blend
beside the atlas painted by the textures script.

WHY. Owner, 2026-10-06: the nine heroes were redesigned and are the game's models; the twelve
Classic street characters are REDRAWN AS IF THEY WERE HEROES (docs/CHARACTER_REDESIGN_DANTE.md
section 15.9 part D). Three were approved as the pattern (bayan, bebang, lola_pacing: "everything
looks good"). This file is a copy of Bebang's, the nearest of the three (a girl on the heroes'
body), rewritten for Inday. It is NOT the original with more detail: it is the same person (who
she is, her colours, her recognisable pieces) DESIGNED IN THE HEROES' VISUAL LANGUAGE.
HEAD_SCALE is 0.84. Every other rule of sections 13 and 15.3 stands.

A PROTOTYPE. NOTHING IN THE GAME LOADS IT. The folder is outside Resources, no roster row points
here, and character-female-a.glb, person_inday.asset and every runtime script are not touched.

WHICH ORIGINAL WAS FOLLOWED. There are two. character-female-a.glb wearing person_inday.asset is
what the roster draws (RosterBookBuilder.PersonModels): a girl with deep brown skin, black hair
with a full fringe, a bob to the jaw and a big tied-up puff high on the back right of her head,
a gold V-necked sleeveless top, a clay waistband, red shorts with turned cuffs, tan shoes on
white soles. team-inday.glb is the older detailed build of her: a cream aviator cap with goggles,
a "w" mouth and an ice box on her back. THAT ONE BECAME CHESKA (docs/CHARACTER_MODEL_METHOD.md:
"team-cheska.glb from build_person_voxel.py's Inday recipe"), and Cheska is one of the nine
heroes she must stand beside. So THIS FOLLOWS character-female-a.glb; nothing of the cap, the
goggles, the ice box or the "w" is taken, because each is Cheska's now.

WHO SHE IS. INDAY, a Classic street character, a girl of about twelve: she "minds the corner
stall and is afraid of absolutely nothing that walks past it" (ConvertedCharacterSelect); "the
playful girl: neat little skipping steps, a tilted head, a light bouncing run"
(GaitStyles.Inday); "the all-rounder with no weak column" (GAME_OVERVIEW.md); a "bakery prodigy
with an unstoppable arm ... a cheeky cat smirk" (her roster tagline). An everyday person.

WHAT IS KEPT OF HER: her deep brown skin; her black hair, its fringe, its bob and the tied-up
puff on the back right of her head; her gold top with its V neck; the clay band at her waist;
her red shorts and their cuffs; tan shoes on white soles; her smile.
WHAT IS DESIGNED, THE HEROES' WAY (each is argued where it is built):
  * the heroes' body (Bebang's build of it, a little narrower: Inday is the all-rounder);
  * THE FACE in the heroes' hand: two ink blocks HALF CLOSED under a level lid with one lash,
    one thin stroke hooked up at one end for the cat smirk, a blush (the textures script);
  * THE HAIR built as the heroes' is: a crown mass, chunky tapered locks with depth, a fringe
    parted on her right and swept to her left, two locks framing her face to the jaw, a bob in
    three layers down the back, and the puff DESIGNED as a high side ponytail that stands up
    off the back right of her crown and falls in a thick arc. Black is the heroes' BLACK;
  * A RIBBON, pale blue (her palette's unused slot 6), tied round the root of the ponytail in
    a bow that shows from the front;
  * THE TOP with a white bound V neck, two gold buttons, cap sleeves with a turned edge;
  * A HALF APRON: the original's clay waistband read as nothing in particular. She minds a
    bakery stall, so it is now the tie of a short cream apron, knotted in a bow behind, with a
    clay hem, a pocket, an embroidered pandesal, and a striped towel tucked in at her hip;
  * red shorts with turned cuffs; short white socks with a yellow frilled top;
  * FOOTWEAR, decided deliberately: tan strap shoes (a bar over the instep, a gold button) on
    white soles, the shoes a girl is sent to the stall in. The original's were tan blocks.
    Nobody in the row wears them (sneakers: Dante, Cheska, Bebang; sandals: Amihan, Lola);
  * two thread bracelets, mint and blue, on her left wrist.
  Nothing of it is another hero's outfit or another Classic's.

THE RULES OF SECTION 15.3, and where each lands in this file:
  1  the head is Dante's rounded `shaped` rows, the heroes' own numbers (`HEAD_ROWS`);
  2  THE FACE IS ONE FLAT PLANE: the front depth is one value, 0.165, from 0.400 to 0.604;
  3  the head is 0.84 of the cast's about the head joint, applied AFTER the UVs resolve
     (`HEAD_SCALE`, `build`). Everything on the head bone takes it;
  4  a cute face: no nose, flat skin, ink eyes, one stroke, a blush;
  8  A DRAWING ONLY ON A FACE THAT SQUARELY FACES ITS VIEW (`proj_square`): every chamfer,
     rounded corner and block end takes one flat tone.
and of section 13: no painted hair shine; cloth across two bones bends (`Part.bend`: the apron
and the towel take the legs toward their hems, the nape tips give to the torso); feet on zero;
under 6,000 triangles. She has no collar: nothing stands round her jaw.

THE HAND, FOR THE FIRST-PERSON ARMS (Editor/ViewmodelArmAuthor.cs cuts them from this mesh and
sizes them by the fist, "the far 14 per cent of the arm"). Her arm runs along x from 0.098 to
0.296. The fist is one clear block, bare skin, from 0.240 to 0.296, so the whole of the far 14
per cent (from 0.268) is fist and nothing else; it is 98 by 116 mm across (Dante's is 116) and
the forearm behind it 84 by 90, so the wrist reads and nothing past it is wider than the fist.
Her bracelets sit on the wrist, behind the fist, and are narrower than it. `verify` holds all
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
import author_character_redesign_inday_textures as tex  # noqa: E402  the island layout
import author_character_redesign_inday_clips as clips  # noqa: E402  her own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/character-female-a.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/inday")
OUT = os.path.join(FOLDER, "inday-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/inday/redesign-20261006/inday_redesign.blend")
NAME = "inday-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# TWO BONES THE CAST DOES NOT HAVE: an elbow in each arm, a child of the arm bone, appended AFTER
# the seven so their indices and names do not move, exactly as the heroes' are. A clip that does
# not key them leaves the arm straight. Hers is 38 mm past the mouth of her cap sleeve.
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
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))   # the `head` bone of character-female-a.glb, in Blender space
SHARP_ANGLE = 40.0       # degrees: a chamfer is a hard edge, a soft corner or a taper is one form
TRIANGLE_BUDGET = 6000
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
    # Only the flat front takes the drawing (her face); the limit is 0.84 so the first facets
    # either side of the middle take it too. Every other face of the head is flat skin, and the
    # faces that turn under at the jaw are the SAME skin, not a shade (no dark facets).
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], proj_square("head", "skin", "skin", 0.84))
    # NO NOSE. HER EARS show between the framing lock and the bob: the heroes' small blocks.
    for s in (1, -1):
        part.block("ear", "head", Y, Z, [((s * 0.166, 0.016, 0.456), 0.031, 0.041), ((s * 0.218, 0.020, 0.456), 0.023, 0.031)],
                   tones("skin", "skin", "skin_shade"), 0.011)


# ---------------------------------------------------------------------------
# THE HAIR, BUILT THE WAY THE HEROES' IS: a crown mass, and on it chunky tapered LOCKS with real
# depth, each set by hand; a designed fringe; a back in layers. Hers is BLACK (the heroes'
# values, Dante's), and its cut is the original's: a full fringe, a bob to the jaw, and a big
# tied-up puff high on the back right of her head. Drawn as a hero's:
#   * a FRINGE of five locks, PARTED over her right eye and swept toward her left, each longer
#     than the one before, the last reaching her left cheekbone;
#   * a long lock framing her face on each side, in front of the ear, to the jaw;
#   * the hair over each ear, and a BOB down the back in three slabs of three lengths with
#     three loose tips under them at the nape;
#   * THE PONYTAIL: the original's puff, designed. A thick root stands up off the back right of
#     the crown, tied with her ribbon; the tail rises out of the tie, arcs out past her right
#     ear and falls, ending in a point and two loose locks.
# THREE TONES BY FACING AND BY LAYER, NOTHING DRAWN: the locks that stand proud wear the black
# with the blue-black light on any face turned up; the masses behind them sit a step deeper.
# Every wedge: (name, base, tip, base half size, tip half size, which tones, chamfer).
# ---------------------------------------------------------------------------
HAIR_CHAMFER = 0.014
LIT = tones("hair_top", "hair", "hair_under")
DEEP = tones("hair", "hair_under", "hair_under")
WEDGES = [
    # the fringe, hanging in front of the flat face (its front is at y -0.164), her right to her left
    # (v01's five were all one length and read as Bayan's and Dante's jagged black fringe; these
    # step down from the part, 60 mm at her right temple to 150 at her left cheekbone, and every
    # tip is carried toward her left)
    ("fringe-1", (-0.156, -0.176, 0.672), (-0.170, -0.182, 0.614), (0.032, 0.022), (0.018, 0.013), LIT, 0.006),
    ("fringe-2", (-0.090, -0.180, 0.676), (-0.056, -0.186, 0.592), (0.040, 0.023), (0.022, 0.014), DEEP, 0.006),
    ("fringe-3", (-0.014, -0.183, 0.676), (0.030, -0.190, 0.568), (0.046, 0.025), (0.026, 0.015), LIT, 0.006),
    ("fringe-4", (0.072, -0.181, 0.676), (0.112, -0.188, 0.544), (0.046, 0.024), (0.026, 0.014), DEEP, 0.006),
    ("fringe-5", (0.146, -0.174, 0.670), (0.172, -0.182, 0.520), (0.038, 0.026), (0.018, 0.014), LIT, 0.006),
    # ONE lock framing her face, on her left, the side the fringe is swept to (v01 had one on
    # each side as well as the hair over the ears: three bars a side, and her ears were gone)
    ("frame-left", (0.200, -0.112, 0.640), (0.199, -0.124, 0.440), (0.020, 0.034), (0.012, 0.016), LIT, 0.008),
    # over each ear: the fore lock stops ABOVE the ear block (its top is at 0.497), the aft one
    # falls behind it into the bob
    ("side-right-fore", (-0.203, 0.002, 0.648), (-0.205, 0.010, 0.522), (0.023, 0.050), (0.019, 0.036), LIT, 0.010),
    ("side-right-aft", (-0.201, 0.118, 0.648), (-0.204, 0.128, 0.446), (0.024, 0.056), (0.018, 0.036), DEEP, 0.010),
    ("side-left-fore", (0.203, 0.000, 0.648), (0.205, 0.008, 0.526), (0.023, 0.052), (0.019, 0.038), LIT, 0.010),
    ("side-left-aft", (0.201, 0.118, 0.648), (0.204, 0.126, 0.434), (0.024, 0.056), (0.018, 0.036), DEEP, 0.010),
    # the bob: three slabs laid over the back mass, top to tip, three lengths
    ("bob-right", (-0.128, 0.222, 0.668), (-0.136, 0.216, 0.436), (0.056, 0.016), (0.040, 0.014), LIT, 0.008),
    ("bob-mid", (0.002, 0.226, 0.668), (0.006, 0.220, 0.412), (0.060, 0.017), (0.042, 0.014), LIT, 0.008),
    ("bob-left", (0.130, 0.222, 0.668), (0.140, 0.216, 0.448), (0.056, 0.016), (0.040, 0.014), LIT, 0.008),
    # the nape: three loose tips under the bob, each its own length
    ("nape-right", (-0.100, 0.186, 0.452), (-0.112, 0.200, 0.404), (0.050, 0.020), (0.024, 0.012), DEEP, 0.008),
    ("nape-mid", (0.004, 0.188, 0.452), (0.010, 0.204, 0.392), (0.050, 0.020), (0.024, 0.012), DEEP, 0.008),
    ("nape-left", (0.104, 0.186, 0.452), (0.114, 0.198, 0.410), (0.048, 0.020), (0.022, 0.012), DEEP, 0.008),
    # two loose locks out of the tail's fall
    ("pony-lock-out", (-0.262, 0.150, 0.690), (-0.298, 0.148, 0.596), (0.030, 0.026), (0.012, 0.010), LIT, 0.006),
    ("pony-lock-back", (-0.246, 0.196, 0.676), (-0.232, 0.226, 0.580), (0.028, 0.026), (0.011, 0.010), DEEP, 0.006),
]
#   two locks laid OVER the tail, along its arc, so it reads as gathered hair and not one lump
#   (v01): (name, base, tip, base half size, tip half size)
PONY_OVER = [
    ("pony-over-top", (-0.150, 0.126, 0.846), (-0.280, 0.152, 0.742), (0.040, 0.022), (0.022, 0.014)),
    ("pony-over-front", (-0.168, 0.084, 0.800), (-0.276, 0.116, 0.640), (0.018, 0.036), (0.011, 0.016)),
]
NAPE_FOLLOW = 0.35     # how much of the shoulders a nape tip keeps when the head turns
#   the ponytail: its root, and the path of the tail: (centre, half width across, half depth)
PONY_ROOT = ((-0.080, 0.095, 0.690), (-0.126, 0.124, 0.768))
PONY_PATH = [((-0.120, 0.120, 0.762), 0.046, 0.046), ((-0.176, 0.140, 0.802), 0.060, 0.058), ((-0.226, 0.156, 0.776), 0.066, 0.062),
             ((-0.262, 0.168, 0.702), 0.060, 0.056), ((-0.270, 0.176, 0.624), 0.044, 0.042)]
PONY_TIP = (-0.262, 0.182, 0.556)
BLUE_T = tones("blue_lit", "blue", "blue_dark")


def tube_rings(path, chamfer):
    """Rings along a bent path: each stands square to the path where it sits."""
    rings = []
    for i, (centre, hu, hv) in enumerate(path):
        before = Vector(path[max(i - 1, 0)][0])
        after = Vector(path[min(i + 1, len(path) - 1)][0])
        w = (after - before).normalized()
        u = (Y - w * Y.dot(w)).normalized()
        v = w.cross(u).normalized()
        rings.append(rect_ring(centre, u, v, hu, hv, chamfer * min(hu, hv)))
    return rings


def build_hair(part):
    # the crown mass: its front edge overhangs the forehead, the fringe hangs from that edge
    part.block("hair-crown", "head", X, Y, [((0, 0, 0.596), 0.198, (0.176, 0.200)), ((0, 0, 0.704), 0.186, (0.166, 0.190))],
               LIT, HAIR_CHAMFER)
    # the mass down the back, one step deeper, so the slabs laid over it read as hair on hair
    part.block("hair-back", "head", X, Y, [((0, 0.178, 0.440), 0.196, 0.030), ((0, 0.176, 0.660), 0.200, 0.034)],
               DEEP, HAIR_CHAMFER)
    part.block("hair-nape-liner", "head", X, Y, [((0, 0.156, 0.392), 0.146, 0.010), ((0, 0.160, 0.456), 0.156, 0.011)],
               flat("hair_under"), 0.004)
    for name, base, tip, base_half, tip_half, paint, chamfer in WEDGES:
        faces = part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, paint, chamfer)
        if name.startswith("nape"):
            part.bend(faces, lambda co: [("head", 1.0 - NAPE_FOLLOW * _ramp(0.452, 0.396, co.z)),
                                         ("torso", NAPE_FOLLOW * _ramp(0.452, 0.396, co.z))])

    # THE CROWN'S TOP: three locks combed across it from the part toward her left, so the top
    # of her head is hair in layers and not one flat plate (v02, seen from above)
    for y, x0, x1, half in ((-0.106, -0.118, 0.158, 0.044), (-0.012, -0.096, 0.166, 0.046), (0.090, -0.016, 0.160, 0.044)):
        part.wedge("hair-top", "head", (x0, y, 0.706), (x1, y - 0.014, 0.690), (half, 0.014), (0.6 * half, 0.010), LIT, 0.006, across=Y)

    # THE PONYTAIL. The root, standing up and out off the crown; the tail, one bent tube that
    # swells out of the tie, arcs over and falls to a point.
    part.wedge("hair-pony-root", "head", PONY_ROOT[0], PONY_ROOT[1], (0.050, 0.050), (0.042, 0.042), DEEP, 0.012)
    part.loft("hair-pony", "head", tube_rings(PONY_PATH, 0.42), DEEP, caps=(True, False), tip=Vector(PONY_TIP))
    for name, base, tip, base_half, tip_half in PONY_OVER:
        part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, LIT, 0.006, across=Y)

    # HER RIBBON: a pale blue band round the root where it is tied, and a BOW on the side of it
    # that faces forward and out, so it shows over her fringe from the front. A knot, two loops
    # that widen away from it, two short tails.
    a, b = Vector(PONY_ROOT[0]), Vector(PONY_ROOT[1])
    part.wedge("ribbon-band", "head", a + (b - a) * 0.46, a + (b - a) * 0.80, (0.0520, 0.0520), (0.0490, 0.0490),
               tones("blue_lit", "blue_dark", "blue_deep"), 0.006)
    knot = Vector((-0.118, 0.050, 0.742))
    part.block("ribbon-knot", "head", X, Z, [(knot + Vector((0, 0.014, 0)), 0.014, 0.013), (knot - Vector((0, 0.010, 0)), 0.011, 0.010)],
               BLUE_T, 0.005)
    # (v01's loops went up and its tails down and out: an X. The loops lie nearly level now and
    # the tails are short and close together, lying forward on the crown.)
    for tip, tail in (((-0.190, 0.046, 0.756), (-0.146, 0.008, 0.712)), ((-0.050, 0.046, 0.764), (-0.096, 0.006, 0.712))):
        part.wedge("ribbon-loop", "head", knot, tip, (0.007, 0.010), (0.011, 0.030), BLUE_T, 0.004, across=Y)
        part.wedge("ribbon-tail", "head", knot + Vector((0, -0.004, -0.006)), tail, (0.010, 0.004), (0.014, 0.004),
                   tones("blue", "blue_dark", "blue_deep"), 0.0)


# ---------------------------------------------------------------------------
# THE TORSO, THE TOP AND THE APRON. The heroes' torso block, a little narrower than Bebang's.
# A gold-yellow top, the same one, DESIGNED: a V neck bound in white, two gold buttons under
# it. Its foot is at the waist; under it the seat of her shorts is a block of its own, so the
# rounded corners of each can take its own flat tone.
# THE APRON is the original's clay waistband given a reason: the TIE of a short cream half
# apron (she minds the corner bakery's stall), knotted in a bow at her back. The apron is one
# flat plate hanging in front of her shorts, with a clay band along its hem, a patch pocket on
# her left, an embroidered pandesal on her right (painted: the plate squarely faces the front)
# and a white towel with two blue stripes tucked over the tie at her right hip.
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.343)
TORSO_LOW = (0.138, 0.085)     # half width, half depth at the hips
TORSO_HIGH = (0.128, 0.081)    # at the shoulders
TORSO_CY = -0.002
ARM_Y, ARM_Z = 0.006, 0.288    # the line her arms are built along (the arm bone is at z 0.288)
WAIST = tex.WAIST
APRON_TOP, APRON_HEM = tex.APRON_TOP, tex.APRON_HEM
APRON_FOLLOW = 0.50            # how much of a leg's swing the apron's hem takes
CLAY_T = tones("clay_lit", "clay", "clay_dark")


def torso_half(z):
    t = (z - TORSO_Z[0]) / (TORSO_Z[1] - TORSO_Z[0])
    return (TORSO_LOW[0] + (TORSO_HIGH[0] - TORSO_LOW[0]) * t, TORSO_LOW[1] + (TORSO_HIGH[1] - TORSO_LOW[1]) * t)


def chest_y(z):
    return TORSO_CY - torso_half(z)[1]


def apron_y(z):
    """The middle of the apron's plate: it hangs a little forward toward its hem."""
    return chest_y(APRON_TOP) - 0.006 - 0.012 * _ramp(APRON_TOP, APRON_HEM, z)


def apron_weights(co):
    down = _ramp(APRON_TOP - 0.022, APRON_HEM, co.z) ** 0.8
    leg = APRON_FOLLOW * down
    left = _smooth(_ramp(-0.070, 0.070, co.x))
    return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]


def build_torso(part):
    top = proj_square("torso", "top", "top_deep", 0.90, ends=(2, "top_mid"))
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, WAIST - 0.004), *torso_half(WAIST - 0.004)), ((0, TORSO_CY, TORSO_Z[1]), *TORSO_HIGH)],
               top, 0.014)
    # the seat of her shorts, and between the legs
    part.block("hips", "torso", X, Y, [((0, TORSO_CY, TORSO_Z[0]), *TORSO_LOW), ((0, TORSO_CY, WAIST), *torso_half(WAIST))],
               tones("red", "red", "red_dark"), 0.012)
    part.block("pelvis", "torso", X, Y, [((0, 0.0, 0.110), 0.058, 0.066), ((0, 0.0, 0.180), 0.058, 0.066)], flat("red_deep"), 0.0)
    # her neck, for when the head tips back and the chin lifts off the top
    neck = part.block("neck", "torso", X, Y, [((0, TORSO_CY, 0.326), 0.050, 0.048), ((0, TORSO_CY, 0.354), 0.048, 0.046)],
                      flat("skin_shade"), 0.008)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.332, 0.350, co.z)), ("head", 0.7 * _ramp(0.332, 0.350, co.z))])
    front = chest_y(0.300)

    # THE V NECK: skin, bound in white. Plates, because the top of the V is on the torso
    # block's chamfer, where no drawing may go.
    def vee(half, top_z, point, y):
        return [Vector((-half, y, top_z)), Vector((half, y, top_z)), Vector((0.0, y, point))]

    part.loft("neck-binding", "torso", [vee(0.060, 0.346, 0.290, front + 0.006), vee(0.060, 0.346, 0.290, front - 0.0030)],
              flat("white"), caps=(False, True))
    part.loft("neck-skin", "torso", [vee(0.047, 0.346, 0.304, front + 0.006), vee(0.047, 0.346, 0.304, front - 0.0048)],
              flat("skin_shade"), caps=(False, True))
    # one gold button at the top of the keyhole behind her neck (the keyhole itself is drawn)
    slab(part, "button", "torso", (0, TORSO_CY + torso_half(0.318)[1], 0.318), X, Z, 0.0064, 0.0064, 0.002, 0.0045, GOLD_T, 0.002, corner=0.003,
         normal=(0, 1, 0))
    # two gold buttons under the point of the V
    for z in (0.272, 0.246):
        slab(part, "button", "torso", (0, chest_y(z), z), X, Z, 0.0068, 0.0068, 0.002, 0.0045, GOLD_T, 0.002, corner=0.003)

    # THE APRON'S TIE, all the way round her waist, 5 mm proud
    grow = lambda z: (torso_half(z)[0] + 0.005, torso_half(z)[1] + 0.005)
    part.block("apron-tie", "torso", X, Y, [((0, TORSO_CY, APRON_TOP - 0.022), *grow(APRON_TOP - 0.022)), ((0, TORSO_CY, APRON_TOP), *grow(APRON_TOP))],
               CLAY_T, 0.004)
    # THE APRON: one plate from under the tie to above her knees, a little wider at its hem.
    # Its hem takes the legs (cloth across two bones bends).
    soft = []
    rows = [(APRON_TOP - 0.018, 0.098), (0.165, 0.106), (APRON_HEM, tex.APRON_HALF)]
    soft += part.block("apron", "torso", X, Y, [((0, apron_y(z), z), hx, 0.005) for z, hx in reversed(rows)],
                       panel("apron", 0, 2, tex.APRON_WINDOW, AHEAD, "cream_shade"), 0.004)
    soft += part.block("apron-hem", "torso", X, Y, [((0, apron_y(APRON_HEM) - 0.0005, APRON_HEM - 0.002), tex.APRON_HALF + 0.002, 0.0072),
                                                    ((0, apron_y(APRON_HEM + 0.013) - 0.0005, APRON_HEM + 0.013), tex.APRON_HALF + 0.001, 0.0072)],
                       CLAY_T, 0.002)
    # the pocket on her left: a patch with a clay top edge
    px, pz = 0.056, 0.160
    soft += slab(part, "apron-pocket", "torso", (px, apron_y(pz) - 0.005, pz), X, Z, 0.031, 0.024, 0.003, 0.0040,
                 tones("cream_lit", "cream_shade", "cream_deep"), 0.003, corner=0.004)
    soft += slab(part, "apron-pocket-edge", "torso", (px, apron_y(pz + 0.019) - 0.005, pz + 0.019), X, Z, 0.033, 0.0056, 0.003, 0.0062, CLAY_T, 0.002)
    # THE TOWEL at her right hip: folded over the tie and hanging in front of the apron
    tx = -0.104
    # (v01's was a straight white bar and read as a label; v02 pinched it at the tie and it read
    # as a milk bottle. It is a folded cloth now: one width, square corners, a short turn of it
    # folded over the tie and the long fall hanging askew, a blue stripe at each end.)
    tow_x = lambda z: tx - 0.20 * (APRON_TOP - z)
    fall = [(APRON_TOP - 0.004, 0.0), (0.180, 0.003), (0.134, 0.006)]
    soft += part.block("towel", "torso", X, Y, [((tow_x(z), apron_y(z) - 0.012 - dy, z), 0.0235, 0.0050) for z, dy in fall],
                       tones("white", "white_shade", "white_deep"), 0.0012)
    soft += part.block("towel-fold", "torso", X, Y, [((tx + 0.003, apron_y(APRON_TOP) - 0.0155, APRON_TOP - 0.034), 0.0245, 0.0052),
                                                     ((tx + 0.001, apron_y(APRON_TOP) - 0.0150, APRON_TOP + 0.004), 0.0245, 0.0060)],
                       tones("white", "white", "white_shade"), 0.0012)
    for z, y_out, wide in ((0.146, 0.0185, 0.0242), (APRON_TOP - 0.026, 0.0215, 0.0252)):
        cx = tow_x(z) if z < 0.17 else tx + 0.003
        soft += part.block("towel-stripe", "torso", X, Y, [((cx, apron_y(z) - y_out, z - 0.0032), wide, 0.0056), ((cx, apron_y(z) - y_out, z + 0.0032), wide, 0.0056)],
                           tones("blue", "blue_dark", "blue_deep"), 0.0)
    part.bend(soft, apron_weights)
    # THE BOW behind: a knot on the tie, two loops, two tails
    back = TORSO_CY + grow(APRON_TOP - 0.011)[1]
    kz = APRON_TOP - 0.011
    part.block("apron-knot", "torso", X, Z, [((0, back - 0.004, kz), 0.014, 0.013), ((0, back + 0.012, kz), 0.011, 0.010)], CLAY_T, 0.004)
    for sx in (1, -1):
        part.wedge("apron-bow", "torso", (sx * 0.008, back + 0.006, kz), (sx * 0.062, back + 0.008, kz + 0.014), (0.005, 0.008), (0.007, 0.020),
                   CLAY_T, 0.003, across=Y)
        part.wedge("apron-bow-tail", "torso", (sx * 0.008, back + 0.005, kz - 0.006), (sx * 0.032, back + 0.010, kz - 0.062), (0.009, 0.004), (0.012, 0.004),
                   tones("clay_lit", "clay_lit", "clay"), 0.0)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests, at the heroes' length. `s` is +1 for her left.
# A CAP SLEEVE on the arm bone: the original's top was sleeveless and its shoulders were bare
# blocks; a short sleeve with a turned edge gives the shoulder a designed end and leaves her
# arms bare, as they were. Then her bare upper arm (the arm bone), her bare forearm and the
# fist (the elbow bone). On her LEFT wrist two thread bracelets, mint and blue, narrower than
# the fist.
# THE FIST is one clear block at the far end, bare skin, the widest thing past the wrist.
# ---------------------------------------------------------------------------
SLEEVE_END = tex.SLEEVE_END
FIST = (tex.WRIST, tex.HAND_END)


def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    arm_block(part, "sleeve", bone, s, [(0.098, (0.050, 0.052)), (0.122, (0.057, 0.060)), (SLEEVE_END, (0.060, 0.064))],
              proj_square(group, "top", "top_deep", 0.90, ends=(0, "top_deep")), 0.011)
    # the sleeve's turned edge, 3 mm proud
    arm_block(part, "sleeve-edge", bone, s, [(SLEEVE_END - 0.013, (0.0630, 0.0670)), (SLEEVE_END + 0.002, (0.0640, 0.0680))],
              tones("top_lit", "top_lit", "top_mid"), 0.004)
    # her bare upper arm, out of the sleeve's mouth to the elbow
    arm_block(part, "upper-arm", bone, s, [(SLEEVE_END - 0.022, (0.0455, 0.0495)), (ELBOW_X + 0.008, (0.0452, 0.0492))],
              tones("skin", "skin", "skin_shade"), 0.010)
    # the forearm starts inside the upper arm's end, so no gap opens when the elbow folds
    arm_block(part, "forearm", fore, s, [(ELBOW_X - 0.016, (0.045, 0.049)), (FIST[0] + 0.006, (0.042, 0.045))],
              proj_square(group, "skin", "skin_shade", 0.93, ends=(0, "skin_shade")), 0.010)
    if s > 0:
        arm_block(part, "bracelet", fore, s, [(FIST[0] - 0.029, (0.0455, 0.0490)), (FIST[0] - 0.018, (0.0455, 0.0490))],
                  tones("mint", "mint", "mint_dark"), 0.003)
        arm_block(part, "bracelet", fore, s, [(FIST[0] - 0.016, (0.0450, 0.0485)), (FIST[0] - 0.005, (0.0450, 0.0485))],
                  tones("blue_lit", "blue", "blue_dark"), 0.003)
    arm_block(part, "hand", fore, s, [(FIST[0], (0.049, 0.058)), (FIST[1], (0.049, 0.058))],
              proj_square(group, "skin", "skin_shade", 0.93), 0.013)


# ---------------------------------------------------------------------------
# THE LEGS. Short, the cast's. Red shorts with a turned cuff (the original's), bare knees,
# short WHITE SOCKS with a yellow frilled top, and STRAP SHOES: tan leather with a rounder,
# paler toe, a bar over the instep fastened with a gold button on the outer side, and a white
# sole with a clay welt. Decided deliberately: they are what a girl who is sent to mind the
# stall wears, and nobody in the row has them. Feet turned out four degrees.
# ---------------------------------------------------------------------------
LEG_X = 0.0836
TOE_OUT = 4.0


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    part.block("shorts", bone, X, Y, [((s * LEG_X, 0.0, tex.SHORTS_HEM), 0.060, 0.066), ((s * (LEG_X - 0.006), 0.0, 0.184), 0.054, 0.064)],
               proj_square(group, "red", "red_dark", 0.90, ends=(2, "red_dark")), 0.009)
    part.block("shorts-cuff", bone, X, Y, [((s * LEG_X, 0.0, tex.SHORTS_HEM - 0.001), 0.0628, 0.0688), ((s * LEG_X, 0.0, tex.SHORTS_HEM + 0.015), 0.0628, 0.0688)],
               tones("red_lit", "red_lit", "red_dark"), 0.003)
    part.block("leg", bone, X, Y, [((s * LEG_X, 0.0, 0.046), 0.044, 0.048), ((s * LEG_X, 0.0, tex.SHORTS_HEM + 0.004), 0.047, 0.052)],
               SKIN_T, 0.010)
    sock_top = tex.SOCK_TOP
    part.block("sock", bone, X, Y, [((s * LEG_X, 0.0, 0.044), 0.0480, 0.0520), ((s * LEG_X, 0.0, sock_top - 0.006), 0.0488, 0.0530)], WHITE_T, 0.004)
    # the frill: a yellow band that flares at the top of the sock
    part.block("sock-frill", bone, X, Y, [((s * LEG_X, 0.0, sock_top - 0.012), 0.0500, 0.0542), ((s * LEG_X, 0.0, sock_top + 0.002), 0.0550, 0.0595)],
               tones("top_lit", "top", "top_mid"), 0.003)

    turn = math.radians(TOE_OUT) * s
    cs, sn = math.cos(turn), math.sin(turn)

    def turned(p):
        x, y = p.x - s * LEG_X, p.y - 0.02
        return Vector((s * LEG_X + x * cs - y * sn, 0.02 + x * sn + y * cs, p.z))

    def station(y, half, z0, z1, cut):
        return [turned(p) for p in rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)]

    top = tex.SOLE_TOP - 0.002
    # the leather upper: low over the instep, rising to the heel
    upper = [(-0.104, 0.058, top, 0.043, 0.010), (-0.064, 0.060, top, 0.049, 0.011), (0.024, 0.060, top, 0.056, 0.011),
             (0.066, 0.058, top, 0.058, 0.011), (0.076, 0.050, top + 0.002, 0.052, 0.006)]
    part.loft("shoe", bone, [station(*row) for row in upper], proj_square(group, "tan", "tan_dark", 0.88))
    # the toe: rounder, a little wider and a tone paler than the rest
    cap = [(-0.134, 0.044, top, 0.033, 0.008), (-0.124, 0.058, top, 0.040, 0.012), (-0.110, 0.0615, top, 0.0445, 0.013), (-0.100, 0.0615, top, 0.0455, 0.013)]
    part.loft("shoe-toe", bone, [station(*row) for row in cap], tones("tan_lit", "tan", "tan_dark"))
    # the bar over the instep, and its gold button on the outer side
    part.block("shoe-strap", bone, X, Y, [((s * LEG_X, -0.046, 0.0470), 0.0632, 0.0125), ((s * LEG_X, -0.046, 0.0590), 0.0624, 0.0115)],
               tones("tan_dark", "tan_dark", "tan_deep"), 0.003, move=turned)
    button = turned(Vector((s * (LEG_X + 0.0632), -0.046, 0.0530)))
    slab(part, "shoe-button", bone, button, Y, Z, 0.0062, 0.0062, 0.002, 0.0042, GOLD_T, 0.002, corner=0.003, normal=(s, 0, 0))
    # the sole: white rubber, wider than the shoe all round, a clay welt round its top
    h = 0.5 * tex.SOLE_TOP
    sole = lambda grow: [((s * LEG_X, -0.136 - grow, 0), 0.048 + grow, 0), ((s * LEG_X, -0.104, 0), 0.066 + grow, 0),
                         ((s * LEG_X, 0.040, 0), 0.066 + grow, 0), ((s * LEG_X, 0.082 + grow, 0), 0.056 + grow, 0)]
    part.block("shoe-sole", bone, X, Z, [((c[0], c[1], h), hx, h) for c, hx, _ in sole(0.0)], tones("white", "white", "white_shade"), 0.005, move=turned)
    part.block("shoe-welt", bone, X, Z, [((c[0], c[1], tex.SOLE_TOP - 0.0030), hx, 0.0030) for c, hx, _ in sole(0.0016)], flat("clay"), 0.0, move=turned)


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
    """character-female-a.glb with its meshes swapped: skeleton, bind matrices and clips copied untouched,
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (inday)"}
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
    print("triangles %d  (budget %d)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    print("the top of her ponytail is at %.3f (Amihan's hair tops out at 0.715, Dante's at 0.716) and it reaches x %.3f" % (hi.z, lo.x))
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
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_inday_textures.py")
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
