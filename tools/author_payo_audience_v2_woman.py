"""Build the Payo audience villager `woman` (v2): a young Ifugao woman, the loudest supporter on the wall.

    py -3 tools/author_payo_audience_v2_woman_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_payo_audience_v2_woman.py
    ... -- --out other.glb     (writes there instead and leaves the .blend alone; needs the atlas beside it)

Writes
    ArtSource/payo/audience/v2/woman/audience_v2_woman.glb   (rigged, every clip)
    ArtSource/payo/audience/v2/woman/audience_v2_woman.blend
beside the atlas painted by the textures script. NOTHING IN THE GAME LOADS THEM YET: the Payo map
session imports them (docs/reports/character-redesign/payo-audience-brief.md).

WHY. The v1 crowd (tools/author_payo_audience.py) was six Blender box figures and the owner
rejected the construction: "you need to actually model them in the way we've modeled the rest of
the characters". So each villager is built EXACTLY like a Classic redesign: this file is a copy of
Inday's (tools/author_character_redesign_inday.py, itself a copy of Bebang's, a girl on the heroes'
body), with the same rig, blocks, paint rules, head at 0.84 and clip machinery, rewritten for her.

WHO SHE IS. A young Ifugao woman of the rice terraces, the LOUDEST supporter in the crowd: on her
toes, hands cupped to shout, nudging her neighbour, throwing a big V when her side scores.
WHAT SHE WEARS, PRECOLONIAL CORDILLERA (the brief's research notes, and each argued where built):
  * HAIR: long and BLACK (the heroes' black), drawn back off her face from a centre part, two
    locks swept back over the temples, the back swept up, and the length WOUND INTO A THICK COIL
    round her crown, the way Cordillera women wrap their hair, with four BEAD STRANDS wound into
    it (madder and white) and a short loose strand of beads ending in a brass bead at the back.
    Nobody in the row has a crown coil: Inday has a side ponytail, Aling Nena a top bun, Bebang
    a headband, Lola Pacing a grey bun. It gives her the tallest head shape of the crowd's women;
  * THE FACE in the heroes' hand: ink eye blocks whose top OUTER corners are cut away (open,
    delighted), a wide grin as one stroke, a blush (the textures script);
  * A SHORT WRAPPED TOP of undyed cotton, modest, to the waist: the wrap's crossing edge runs
    from her right shoulder to her left hip as GEOMETRY, bound in an indigo band with a thin
    madder line, the same banded edge round its hem and the mouths of the short sleeves;
  * THE TAPIS, knee length: an indigo-black wrap skirt, flared a little, its WOVEN BANDS built
    as proud rings (a white hem band, thin madder and white bands above it, a madder band near
    the belt), one painted row of small diamonds between them, the outer end of the wrap lying
    over her left thigh. Its hem takes the legs (cloth across two bones bends);
  * A WOVEN BELT of natural rattan, its two tied ends hanging at her left hip with madder tips;
  * THE FOCAL PIECE: A HEAVY BEAD NECKLACE, three strands of chunky beads stepped down her chest
    (madder and white; black and madder; white, madder and black) with a brass bead at the centre;
  * BRASS HOOP EARRINGS, the one metal;
  * BARE FEET, proper blocks with the toes a step lower, and a bead ANKLET round each ankle.
COLOUR IS QUIET (the brief): the ground is indigo-black and undyed cotton, red only in thin bands
and beads, brass the one metal; no hero's signature hue, the role hues nowhere.

THE RULES OF SECTION 15.3 OF docs/CHARACTER_REDESIGN_DANTE.md hold as they do in Inday's file:
the heroes' rounded head with one flat face plane, HEAD_SCALE 0.84 applied after the UVs, no
nose, drawings only on faces square to a view, flat tones elsewhere, no painted hair shine,
cloth across two bones bends, feet on zero, under 6,000 triangles, the seven bones plus
`forearm-left` and `forearm-right`, and the bare fist block in the far 14 per cent of the arm
(`verify` holds it, exactly as Inday's does).
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
import author_payo_audience_v2_woman_textures as tex  # noqa: E402  the island layout
import author_payo_audience_v2_woman_clips as clips  # noqa: E402  her clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/character-female-a.glb")
FOLDER = os.path.join(ROOT, "ArtSource/payo/audience/v2/woman")
OUT = os.path.join(FOLDER, "audience_v2_woman.glb")
BLEND = os.path.join(FOLDER, "audience_v2_woman.blend")
NAME = "audience_v2_woman"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
ELBOW_X = 0.180
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
HEAD_SCALE = 0.84
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))   # the `head` bone of character-female-a.glb, in Blender space
SHARP_ANGLE = 40.0
TRIANGLE_BUDGET = 6000
ARM_END = tex.HAND_END

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



WHITE_T = tones("bone", "bone", "bone_shade")
MADDER_T = tones("madder_lit", "madder", "madder_dark")
BRASS_T = tones("brass_lit", "brass", "brass_dark")
BLACK_T = tones("hair_top", "bead_black", "bead_black")
INDIGO_T = tones("indigo_lit", "indigo", "indigo_dark")
RATTAN_T = tones("rattan_lit", "rattan", "rattan_dark")
BEAD = {"r": MADDER_T, "w": WHITE_T, "k": BLACK_T, "b": BRASS_T}


def bead_rings(points, big, small):
    """A strand of chunky beads through `points` (the bead centres): a square ring at each bead
    and a narrower one halfway between, so each bead stands as its own block. Each ring stands
    square to the strand where it sits; along a stretch running front to back it is turned
    about Z instead of Y so it never folds."""
    pts = []
    for i, p in enumerate(points):
        pts.append((Vector(p), big))
        if i + 1 < len(points):
            pts.append(((Vector(p) + Vector(points[i + 1])) * 0.5, small))
    rings = []
    for i, (centre, half) in enumerate(pts):
        before = pts[max(i - 1, 0)][0]
        after = pts[min(i + 1, len(pts) - 1)][0]
        w = (after - before).normalized()
        ref = Y if abs(w.y) < 0.7 else Z
        u = (ref - w * ref.dot(w)).normalized()
        v = w.cross(u).normalized()
        rings.append(rect_ring(centre, u, v, half, half, 0.0))
    return rings


def bead_strand(part, name, bone, points, colours, big=0.0105, small=0.0062):
    """`colours` is one letter a bead (r madder, w white, k black, b brass), set by hand."""
    if len(colours) != len(points):
        raise SystemExit("%s: %d beads, %d colours" % (name, len(points), len(colours)))
    spec = lambda i, j: BEAD[colours[(i + 1) // 2]]
    return part.loft(name, bone, bead_rings(points, big, small), spec)


def hoop(part, name, bone, centre, normal, radius, half, paint, n=12):
    """A closed ring: a hoop earring, a coil of hair. `normal` is the axis the ring turns about."""
    nrm = Vector(normal).normalized()
    e1 = (Z - nrm * Z.dot(nrm)).cross(nrm).normalized() if abs(nrm.z) < 0.9 else X
    e2 = nrm.cross(e1).normalized()
    rings = []
    for k in range(n + 1):
        a = 2.0 * math.pi * k / n
        radial = e1 * math.cos(a) + e2 * math.sin(a)
        rings.append(rect_ring(Vector(centre) + radial * radius, radial, nrm, half, half, 0.0))
    return part.loft(name, bone, rings, paint, caps=(False, False))


# ---------------------------------------------------------------------------
# THE HAIR, BUILT THE WAY THE HEROES' IS, BLACK, and DRAWN BACK: a crown mass whose front edge
# stops at her hairline (no fringe: her forehead shows), two locks swept back from a centre part
# over the temples, a lock over each ear running back, three slabs swept UP the back of her head
# from the nape, and THE COIL: the length wound into a thick ring round her crown, sitting a
# little higher at the front, with four bead strands wound into it and its tucked end standing
# out at the back left. Three tones by facing and layer, nothing drawn.
# ---------------------------------------------------------------------------
HAIR_CHAMFER = 0.014
LIT = tones("hair_top", "hair", "hair_under")
DEEP = tones("hair", "hair_under", "hair_under")
WEDGES = [
    # the two swept locks: from the part, back over each temple (their tips stop short of the ear)
    ("sweep-right", (-0.044, -0.186, 0.684), (-0.170, -0.180, 0.610), (0.042, 0.017), (0.026, 0.013), LIT, 0.006),
    ("sweep-left", (0.044, -0.186, 0.684), (0.170, -0.180, 0.606), (0.042, 0.017), (0.026, 0.013), LIT, 0.006),
    # over each ear, running back to the swept-up back; the ear (its top 0.497) stays clear
    ("side-right", (-0.198, -0.126, 0.630), (-0.206, 0.104, 0.594), (0.021, 0.040), (0.017, 0.030), LIT, 0.010),
    ("side-left", (0.198, -0.126, 0.632), (0.206, 0.104, 0.590), (0.021, 0.040), (0.017, 0.030), LIT, 0.010),
    # three slabs swept UP the back, from the nape to the coil, each its own width and lean
    ("up-right", (-0.112, 0.204, 0.482), (-0.076, 0.184, 0.700), (0.050, 0.014), (0.034, 0.012), LIT, 0.008),
    ("up-mid", (0.002, 0.212, 0.470), (0.000, 0.190, 0.706), (0.056, 0.015), (0.038, 0.012), DEEP, 0.008),
    ("up-left", (0.114, 0.202, 0.488), (0.078, 0.182, 0.698), (0.050, 0.014), (0.034, 0.012), LIT, 0.008),
    # the coil's tucked END, standing out off the back left of the coil
    ("coil-end", (0.070, 0.118, 0.808), (0.150, 0.190, 0.770), (0.030, 0.028), (0.012, 0.010), LIT, 0.006),
]
#   THE COIL, in TWO TURNS (v02's one ring read as a flat black pillbox from the front): a wide
#   lower turn round the crown and a smaller upper turn stacked on it, so it reads as a rope of
#   hair wound on itself. Each turn: (name, centre y, radius across, radius front to back, height
#   at the front, height at the back, half thickness out, half thickness up, segments)
COIL_TURNS = [
    ("hair-coil-low", 0.020, 0.150, 0.140, 0.752, 0.724, 0.044, 0.040, 16),
    ("hair-coil-high", 0.034, 0.104, 0.096, 0.818, 0.792, 0.040, 0.036, 12),
]
#   the bead strands wound into it: (which turn, angle round it in degrees, 0 at her left, 90 at
#   her FRONT, the colour of its beads); set by hand so no two match their neighbours
COIL_WRAPS = [(0, 62.0, "r"), (0, 134.0, "w"), (0, 226.0, "r"), (0, 318.0, "w"), (1, 96.0, "w"), (1, 252.0, "r")]
#   the loose strand hanging off the back right of the coil, ending in a brass bead
LOOSE_STRAND = [(-0.116, 0.126, 0.716), (-0.128, 0.140, 0.688), (-0.138, 0.152, 0.660), (-0.146, 0.162, 0.632),
                (-0.152, 0.170, 0.602)]
LOOSE_COLOURS = "rwrwb"


def coil_point(turn, a):
    """The middle of the rope of `turn` at angle `a` (radians, 0 at her left, pi/2 at her front)."""
    _n, cy, rx, ry, zf, zb = turn[:6]
    x = rx * math.cos(a)
    y = cy - ry * math.sin(a)
    front = 0.5 + 0.5 * math.sin(a)          # 1 at the front, 0 at the back
    return Vector((x, y, zb + (zf - zb) * front))


def coil_out(turn, a):
    return Vector((math.cos(a) * turn[3], -math.sin(a) * turn[2], 0.0)).normalized()


def build_hair(part):
    part.block("hair-crown", "head", X, Y, [((0, 0, 0.594), 0.198, (0.176, 0.200)), ((0, 0, 0.704), 0.186, (0.166, 0.190))],
               LIT, HAIR_CHAMFER)
    part.block("hair-back", "head", X, Y, [((0, 0.178, 0.470), 0.188, 0.028), ((0, 0.176, 0.660), 0.200, 0.034)],
               DEEP, HAIR_CHAMFER)
    for name, base, tip, base_half, tip_half, paint, chamfer in WEDGES:
        part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, paint, chamfer)
    # the top of her head inside the upper turn: one low cap of hair
    part.block("hair-top", "head", X, Y, [((0, 0.030, 0.770), 0.078, 0.072), ((0, 0.030, 0.836), 0.060, 0.054)], DEEP, 0.014)

    for k, turn in enumerate(COIL_TURNS):
        name, hu, hv, n = turn[0], turn[6], turn[7], turn[8]
        rings = []
        for i in range(n + 1):
            a = 2.0 * math.pi * i / n
            rings.append(rect_ring(coil_point(turn, a), coil_out(turn, a), Z, hu, hv, 0.42 * min(hu, hv)))
        part.loft(name, "head", rings, LIT if k == 1 else DEEP, caps=(False, False))
    # the bead strands wound into it: each a short proud band round the rope
    for k, deg, colour in COIL_WRAPS:
        turn = COIL_TURNS[k]
        a = math.radians(deg)
        c = coil_point(turn, a)
        along = (coil_point(turn, a + 0.05) - coil_point(turn, a - 0.05)).normalized()
        out = coil_out(turn, a)
        u = (out - along * out.dot(along)).normalized()
        v = along.cross(u).normalized()
        part.block("coil-beads", "head", u, v, [(c - along * 0.0065, turn[6] + 0.005, turn[7] + 0.005),
                                                 (c + along * 0.0065, turn[6] + 0.005, turn[7] + 0.005)],
                   BEAD[colour], 0.010)
    bead_strand(part, "hair-loose-strand", "head", LOOSE_STRAND, LOOSE_COLOURS, big=0.0095, small=0.0055)

    # BRASS HOOPS, one below each ear, turned 35 degrees toward the front so they read from it
    for s in (1, -1):
        turn = math.radians(35.0)
        hoop(part, "earring", "head", (s * 0.206, 0.004, 0.388), (s * math.sin(turn), -math.cos(turn), 0.0), 0.027, 0.0048, BRASS_T)


# ---------------------------------------------------------------------------
# THE TORSO, THE TOP, THE BELT AND THE TAPIS. The heroes' torso block, a touch fuller at the hips
# than Inday's. Above the waist it is her undyed cotton top; under it the seat is a block of its
# own, under the tapis.
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.343)
TORSO_LOW = (0.140, 0.086)
TORSO_HIGH = (0.128, 0.081)
TORSO_CY = -0.002
ARM_Y, ARM_Z = 0.006, 0.288
WAIST = tex.WAIST
SKIRT_TOP, SKIRT_HEM = tex.SKIRT_TOP, tex.SKIRT_HEM
SKIRT_FOLLOW = 0.85
#   the tapis: (z, extra half width, extra half depth) over the torso's line: it flares to the hem
SKIRT_ROWS = [(SKIRT_TOP, 0.004, 0.004), (0.150, 0.010, 0.010), (SKIRT_HEM, 0.016, 0.016)]
#   its woven bands, built as proud rings: (from z, to z, how proud, colour). Set by hand.
SKIRT_BANDS = [
    (SKIRT_HEM - 0.001, SKIRT_HEM + 0.010, 0.0045, WHITE_T),      # the hem, a white selvedge band
    (0.0965, 0.1045, 0.0030, MADDER_T),
    (0.1455, 0.1530, 0.0030, MADDER_T),
]


def torso_half(z):
    t = (z - TORSO_Z[0]) / (TORSO_Z[1] - TORSO_Z[0])
    return (TORSO_LOW[0] + (TORSO_HIGH[0] - TORSO_LOW[0]) * t, TORSO_LOW[1] + (TORSO_HIGH[1] - TORSO_LOW[1]) * t)


def chest_y(z):
    return TORSO_CY - torso_half(z)[1]


def skirt_half(z):
    hx, hy = torso_half(min(max(z, TORSO_Z[0]), SKIRT_TOP))
    ex = _table([(r[0], r[1]) for r in reversed(SKIRT_ROWS)], z)
    ey = _table([(r[0], r[2]) for r in reversed(SKIRT_ROWS)], z)
    return (hx + ex, hy + ey)


def skirt_weights(co):
    down = SKIRT_FOLLOW * _ramp(SKIRT_TOP - 0.004, SKIRT_HEM, co.z) ** 0.9
    left = _smooth(_ramp(-0.070, 0.070, co.x))
    return [("torso", 1.0 - down), ("leg-left", down * left), ("leg-right", down * (1.0 - left))]


def strip(part, name, bone, a, b, width, out, depth, paint):
    """A flat band lying on the chest from point `a` to point `b` (x, z), `width` across, its face
    `out` ahead of the cloth and its back `depth` sunk into it."""
    a, b = Vector((a[0], 0, a[1])), Vector((b[0], 0, b[1]))
    d = (b - a).normalized()
    n = Vector((-d.z, 0, d.x)) * (0.5 * width)
    corners = [a - n, b - n, b + n, a + n]

    def ring(off):
        return [Vector((p.x, chest_y(p.z) + off, p.z)) for p in corners]

    return part.loft(name, bone, [ring(depth), ring(-out)], paint, caps=(False, True))


def build_torso(part):
    top = proj_square("torso", "cotton", "cotton_shade", 0.90, ends=(2, "cotton_lit"))
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, WAIST - 0.004), *torso_half(WAIST - 0.004)), ((0, TORSO_CY, TORSO_Z[1]), *TORSO_HIGH)],
               top, 0.014)
    part.block("hips", "torso", X, Y, [((0, TORSO_CY, TORSO_Z[0]), *TORSO_LOW), ((0, TORSO_CY, WAIST), *torso_half(WAIST))],
               INDIGO_T, 0.012)
    part.block("pelvis", "torso", X, Y, [((0, 0.0, 0.150), 0.058, 0.066), ((0, 0.0, 0.180), 0.058, 0.066)], flat("indigo_deep"), 0.0)
    neck = part.block("neck", "torso", X, Y, [((0, TORSO_CY, 0.326), 0.050, 0.048), ((0, TORSO_CY, 0.354), 0.048, 0.046)],
                      flat("skin_shade"), 0.008)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.332, 0.350, co.z)), ("head", 0.7 * _ramp(0.332, 0.350, co.z))])

    grow = lambda z, g: (torso_half(z)[0] + g, torso_half(z)[1] + g)
    # THE TOP'S BANDED HEM: an indigo band with a thin madder line under it, round her waist
    part.block("top-hem", "torso", X, Y, [((0, TORSO_CY, WAIST - 0.002), *grow(WAIST - 0.002, 0.004)), ((0, TORSO_CY, WAIST + 0.012), *grow(WAIST + 0.012, 0.004))],
               INDIGO_T, 0.004)
    part.block("top-hem-line", "torso", X, Y, [((0, TORSO_CY, WAIST - 0.008), *grow(WAIST - 0.008, 0.0045)), ((0, TORSO_CY, WAIST - 0.002), *grow(WAIST - 0.002, 0.0045))],
               MADDER_T, 0.002)
    # THE WRAP'S CROSSING EDGE: from her right shoulder down to her left hip, the same banded edge
    strip(part, "wrap-edge", "torso", (-0.068, 0.326), (0.116, 0.236), 0.016, 0.0040, 0.004, INDIGO_T)
    strip(part, "wrap-edge-line", "torso", (-0.072, 0.317), (0.112, 0.227), 0.0055, 0.0042, 0.004, MADDER_T)

    # THE NECKLACE, three strands stepped down her chest, the focal piece. Bead centres set by
    # hand: each strand starts on the top of the shoulders beside her neck, comes over the front
    # edge and hangs in its own curve. y is the chest's front, a bead sunk a little into the cloth.
    fy = lambda z: chest_y(z) - 0.0080
    strand_a = [(-0.058, -0.022, 0.351), (-0.066, -0.058, 0.351), (-0.068, fy(0.336) + 0.004, 0.338), (-0.058, fy(0.322), 0.322),
                (-0.036, fy(0.313), 0.313), (-0.012, fy(0.309), 0.309), (0.012, fy(0.309), 0.309), (0.036, fy(0.313), 0.313),
                (0.058, fy(0.322), 0.322), (0.068, fy(0.336) + 0.004, 0.338), (0.066, -0.058, 0.351), (0.058, -0.022, 0.351)]
    strand_b = [(-0.076, -0.024, 0.352), (-0.084, -0.060, 0.352), (-0.088, fy(0.334) + 0.004, 0.336), (-0.082, fy(0.313), 0.313),
                (-0.064, fy(0.297), 0.297), (-0.041, fy(0.287), 0.287), (-0.014, fy(0.283), 0.283), (0.014, fy(0.283), 0.283),
                (0.041, fy(0.287), 0.287), (0.064, fy(0.297), 0.297), (0.082, fy(0.313), 0.313), (0.088, fy(0.334) + 0.004, 0.336),
                (0.084, -0.060, 0.352), (0.076, -0.024, 0.352)]
    strand_c = [(-0.094, -0.026, 0.353), (-0.102, -0.062, 0.353), (-0.106, fy(0.332) + 0.004, 0.334), (-0.102, fy(0.309), 0.309),
                (-0.088, fy(0.287), 0.287), (-0.068, fy(0.270), 0.270), (-0.043, fy(0.260), 0.260), (-0.015, fy(0.255), 0.255),
                (0.015, fy(0.255), 0.255), (0.043, fy(0.260), 0.260), (0.068, fy(0.270), 0.270), (0.088, fy(0.287), 0.287),
                (0.102, fy(0.309), 0.309), (0.106, fy(0.332) + 0.004, 0.334), (0.102, -0.062, 0.353), (0.094, -0.026, 0.353)]
    bead_strand(part, "necklace-a", "torso", strand_a, "rrrrrrrrrrrr", big=0.0098, small=0.0058)
    bead_strand(part, "necklace-b", "torso", strand_b, "wwwwwkwwkwwwww", big=0.0108, small=0.0062)
    bead_strand(part, "necklace-c", "torso", strand_c, "rrrrrrwrrwrrrrrr", big=0.0118, small=0.0068)
    # and its brass bead, hanging under the middle of the lowest strand
    part.block("necklace-brass", "torso", X, Z, [((0, fy(0.238) - 0.006, 0.238), 0.0135, 0.0150), ((0, fy(0.238) + 0.010, 0.238), 0.0125, 0.0140)],
               BRASS_T, 0.005)

    # THE WOVEN BELT, natural rattan, round the top of the tapis
    belt = tex.BELT
    part.block("belt", "torso", X, Y, [((0, TORSO_CY, belt[0]), *(skirt_half(belt[0])[0] + 0.006, skirt_half(belt[0])[1] + 0.006)),
                                       ((0, TORSO_CY, belt[1]), *(skirt_half(belt[1])[0] + 0.005, skirt_half(belt[1])[1] + 0.005))],
               RATTAN_T, 0.005)

    # THE TAPIS, knee length, flared; its hem takes the legs
    soft = part.block("tapis", "torso", X, Y, [((0, TORSO_CY, z), *skirt_half(z)) for z in (SKIRT_TOP, 0.150, SKIRT_HEM)],
                      proj_square("skirt", "indigo", "indigo_dark", 0.90, ends=(2, "indigo_dark")), 0.012)
    for z0, z1, proud, paint in SKIRT_BANDS:
        soft += part.block("tapis-band", "torso", X, Y, [((0, TORSO_CY, z0), *(skirt_half(z0)[0] + proud, skirt_half(z0)[1] + proud)),
                                                         ((0, TORSO_CY, z1), *(skirt_half(z1)[0] + proud, skirt_half(z1)[1] + proud))],
                           paint, 0.010)
    # the belt's two tied ends, hanging at her left hip, each with a madder tip
    hx, hy = skirt_half(0.190)
    #   both ends hang on her left side, just behind the front corner (v02 had one on the front, where it read as a sheath)
    for name, base, tip, w, across in (
            ("belt-end-front", (hx + 0.009, TORSO_CY - hy + 0.024, 0.198), (hx + 0.015, TORSO_CY - hy + 0.020, 0.150), 0.0095, Y),
            ("belt-end-back", (hx + 0.009, TORSO_CY - hy + 0.050, 0.198), (hx + 0.014, TORSO_CY - hy + 0.058, 0.160), 0.0090, Y)):
        soft += part.wedge(name, "torso", base, tip, (w, 0.0045), (w * 0.9, 0.0040), RATTAN_T, 0.002, across=across)
        t = Vector(tip)
        side = Y if across is X else X  # the tip block's depth runs across the wedge
        soft += part.block(name + "-tip", "torso", across, side, [(t + Vector((0, 0, -0.014)), w * 0.95, 0.0055),
                                                                    (t + Vector((0, 0, 0.004)), w * 0.95, 0.0055)],
                           MADDER_T, 0.002)
    part.bend(soft, skirt_weights)


# ---------------------------------------------------------------------------
# THE ARMS, as Inday's: the wrapped top's short sleeve (cotton) with its banded edge (a madder
# line, then the indigo band at the mouth), her bare upper arm, bare forearm and the fist.
# ---------------------------------------------------------------------------
SLEEVE_END = tex.SLEEVE_END
FIST = (tex.WRIST, tex.HAND_END)


def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    arm_block(part, "sleeve", bone, s, [(0.098, (0.050, 0.052)), (0.120, (0.056, 0.059)), (SLEEVE_END, (0.059, 0.063))],
              proj_square(group, "cotton", "cotton_shade", 0.90, ends=(0, "cotton_shade")), 0.011)
    arm_block(part, "sleeve-line", bone, s, [(SLEEVE_END - 0.019, (0.0618, 0.0658)), (SLEEVE_END - 0.013, (0.0618, 0.0658))],
              MADDER_T, 0.003)
    arm_block(part, "sleeve-band", bone, s, [(SLEEVE_END - 0.013, (0.0630, 0.0670)), (SLEEVE_END + 0.002, (0.0640, 0.0680))],
              INDIGO_T, 0.004)
    arm_block(part, "upper-arm", bone, s, [(SLEEVE_END - 0.022, (0.0455, 0.0495)), (ELBOW_X + 0.008, (0.0452, 0.0492))],
              tones("skin", "skin", "skin_shade"), 0.010)
    arm_block(part, "forearm", fore, s, [(ELBOW_X - 0.016, (0.045, 0.049)), (FIST[0] + 0.006, (0.042, 0.045))],
              proj_square(group, "skin", "skin_shade", 0.93, ends=(0, "skin_shade")), 0.010)
    arm_block(part, "hand", fore, s, [(FIST[0], (0.049, 0.058)), (FIST[1], (0.049, 0.058))],
              proj_square(group, "skin", "skin_shade", 0.93), 0.013)


# ---------------------------------------------------------------------------
# THE LEGS, BARE, and BARE FEET (the brief: precolonial villagers go barefoot; "toes as a slight
# step is plenty"). The leg runs up under the tapis. A BEAD ANKLET round each ankle, two rows:
# madder beads under white ones. The foot is a block from heel to the ball, and the toes a lower
# block in front of it, a little wider; the three toe gaps are drawn on its top face. Turned out
# four degrees.
# ---------------------------------------------------------------------------
LEG_X = 0.0836
TOE_OUT = 4.0


def build_leg(part, s):
    bone, foot_group = ("leg-left", "footL") if s > 0 else ("leg-right", "footR")
    part.block("leg", bone, X, Y, [((s * LEG_X, 0.004, 0.026), 0.039, 0.042), ((s * LEG_X, 0.0, 0.196), 0.046, 0.051)],
               SKIN_T, 0.010)
    part.block("anklet", bone, X, Y, [((s * LEG_X, 0.004, 0.036), 0.0450, 0.0480), ((s * LEG_X, 0.004, 0.044), 0.0450, 0.0480)], MADDER_T, 0.004)
    part.block("anklet", bone, X, Y, [((s * LEG_X, 0.004, 0.044), 0.0440, 0.0470), ((s * LEG_X, 0.004, 0.051), 0.0440, 0.0470)], WHITE_T, 0.004)

    turn = math.radians(TOE_OUT) * s
    cs, sn = math.cos(turn), math.sin(turn)

    def turned(p):
        x, y = p.x - s * LEG_X, p.y - 0.02
        return Vector((s * LEG_X + x * cs - y * sn, 0.02 + x * sn + y * cs, p.z))

    part.block("foot", bone, X, Z, [((s * LEG_X, 0.046, 0.017), 0.038, 0.017), ((s * LEG_X, -0.020, 0.019), 0.041, 0.019),
                                    ((s * LEG_X, -0.080, 0.015), 0.043, 0.015)],
               tones("skin_lit", "skin", "skin_shade"), 0.009, move=turned)
    part.block("toes", bone, X, Z, [((s * LEG_X, -0.074, 0.0115), 0.0445, 0.0115), ((s * LEG_X, -0.116, 0.0105), 0.0430, 0.0105)],
               proj_square(foot_group, "skin", "skin_shade", 0.95), 0.006, move=turned)


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

    # her own clips, in place of the ones copied across (the clips script); the crowd's clips the
    # base rig has no slot for (`cheer`, `clap`) are appended as new animations
    node_of = {n.get("name"): i for i, n in enumerate(gltf["nodes"])}
    have = {anim["name"] for anim in gltf["animations"]}
    for name in clips.CLIPS:
        if name not in have:
            gltf["animations"].append({"name": name, "samplers": [], "channels": []})
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso Payo audience v2 (woman)"}
    gltf["extras"] = {"prototype": "payo-audience-v2", "replaces": "nothing; not loaded by the game yet"}
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
    print("her height (the top of the coil) is %.3f (the cast's range 0.6613 to 0.7928)" % hi.z)
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
        raise SystemExit("paint the atlas first: py -3 tools/author_payo_audience_v2_woman_textures.py")
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
