"""Build the Phaister (Soraya) redesign PROTOTYPE: the same block kid, shaped and painted.

    py -3 tools/author_character_redesign_phaister_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_phaister.py
    ... -- --out other.glb     (writes there instead and leaves the .blend alone; needs the atlas beside it)

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/phaister/phaister-redesign.glb
    ArtSource/phaister/redesign-20261005/phaister_redesign.blend
beside the atlas painted by the textures script.

WHY. Phaister was "finalized and protected" (AGENTS.md). Owner, 2026-10-05, after the other seven
heroes were redesigned: he lifted that FOR A PROTOTYPE ONLY. This is a copy of Amihan's author
script (itself a copy of Dante's) rewritten for her; those files are left alone
(docs/CHARACTER_REDESIGN_DANTE.md sections 13 and 15).

A PROTOTYPE. NOTHING IN THE GAME LOADS IT. The folder is outside Resources, no roster row points
here, and team-phaister.glb, tools/build_phaister_voxel.py, person_phaister.asset, the files under
Art/characters/phaister-motion and every runtime script are not touched.

THE RULES OF SECTION 15.3, and where each lands in this file:
  1  the head is Dante's rounded `shaped` rows on her own box (`HEAD_ROWS`);
  2  THE FACE IS ONE FLAT PLANE: the front depth is one value, 0.165, from 0.400 (under her mouth)
     to 0.604 (the hairline). No brow ledge;
  3  the head is 0.84 of the original's about the head joint, applied AFTER the UVs resolve
     (`HEAD_SCALE`, `build`). HER HAT IS THE ONE EXCEPTION, with the owner's yes (2026-10-05, on
     v04: "the hat is her silhouette and it lost its shape"): it rides the head bone but does
     NOT take the scale. It keeps the original's size, brim 0.660 across, crown and tip as
     measured off team-phaister.glb, and is only LOWERED 0.0475 (`HAT_DROP`) so its brim sits on
     the smaller head where it sits on the original's (the brim's underside 21 mm below the top
     of the skull). Its tip is at 0.9525. Nothing of hers stands
     round the jaw (see THE COLLAR), so there is no collar to ramp; the hair tips that share the
     torso are drawn in by their head share, as Dante's collar is by its ramp;
  4  a cute face: no nose, no ears showing (her side hair covers them, as on the original),
     eyes and mouth measured off the original (the textures script);
  8  A DRAWING ONLY ON A FACE THAT SQUARELY FACES ITS VIEW (`proj_square`): every chamfer, every
     rounded corner and every block end takes one flat tone. This is applied to every drawn
     piece of hers, not only the arms.
and of section 13: nothing invented (every piece below is one team-phaister.glb has, measured
off that file, in the original palette's own hex values); no painted hair shine; cloth across
two bones bends (`Part.bend`: the coat skirt, the cape's tails and the doll on the skirt take
the legs, the hair tips below the jaw take the torso); feet on zero; under 6,000 triangles.

WHAT IS KEPT of the original, read off team-phaister.glb: the rig and every clip but the five
the clips script replaces; the head box from 0.343 to 0.661; the torso to 0.343; legs to 0.176;
the hat at its full size, seated 0.0475 lower on the smaller head (brim 0.5925, tip 0.9525); the arm
from 0.100 to 0.285; feet on zero, facing -y. The original is 15,020 triangles; this is under
6,000, so several of its stepped voxel pieces are one tapered block here.

THE HANDS, FOR HER KIT. Her skills hang props on her hands (Runtime/Visual/PhaisterHandProps.cs,
PhaisterManika.cs, VoodooSkyCircle.cs, VoodooSoulDraw.cs) and none of that code is changed here.
Where things are on this rig, in the rest pose, Blender space (x her left, z up):
  * the hand block is the FAR END of the forearm bone: x 0.236 to 0.285 (the original's hand is
    0.238 to 0.285), y -0.038 to 0.050, z 0.226 to 0.350;
  * THE PALM'S MIDDLE is at x +-0.2605, y 0.006, z 0.288: 0.1606 out from the arm bone (0.0999),
    0.0845 out from the elbow bone (`ELBOW_X` 0.176), level with both;
  * THE HAND'S TOP is 0.062 above the arm bone (the original's is 0.066; `verify` holds it
    between 0.045 and 0.066, where a carried tsinelas is parked);
  * the elbow is where the black sleeve meets the purple band. The band, the gold rim, the white
    cuff and the hand all ride `forearm-left` / `forearm-right`; only the black sleeve rides
    `arm-left` / `arm-right`.
  WHAT THIS COSTS IN THE GAME, NOT FIXED HERE: `CharacterVisual.PalmCentre` asked for `arm-left`
  by name (as PhaisterHandProps and PhaisterManika do) now finds only the black sleeve, so a prop
  measured that way sits at the elbow, about 85 mm short of the palm, until that code is taught
  the forearm bone (section 15.5 item 1, the elbow pass). `CharacterVisual.HandBones` already
  tries the forearm first.

HOW A FACE FINDS ITS PAINT. A piece is tagged with a group ("head", "torso", "cape" ...) and each
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
import author_character_redesign_phaister_textures as tex  # noqa: E402  the island layout
import author_character_redesign_phaister_clips as clips  # noqa: E402  her own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/team-phaister.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/phaister")
OUT = os.path.join(FOLDER, "phaister-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/phaister/redesign-20261005/phaister_redesign.blend")
NAME = "phaister-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# TWO BONES THE CAST DOES NOT HAVE: an elbow in each arm, a child of the arm bone, appended AFTER
# the seven so their indices and names do not move, exactly as Dante's are. A clip that does not
# key them leaves the arm straight. Hers sits where the black sleeve meets the purple band.
ELBOW_X = 0.176
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
# THE HEAD IS 84 PER CENT OF THE ORIGINAL'S (owner, 2026-10-05: "smaller heads are better", "yes
# rebuild the heads at 84"). Everything is BUILT and PAINTED at the original's size, and only
# after the UVs are resolved is every vertex that rides the `head` bone drawn in toward the head
# JOINT (`build`). Her hat is on the head bone too but keeps its size (see the docstring, rule 3).
HEAD_SCALE = 0.84
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))   # the `head` bone of team-phaister.glb, in Blender space
ORIGINAL_TOP = 1.000     # the tip of her hat (team-phaister.glb head-mesh bounds)
ORIGINAL_BRIM = 0.640    # the underside of its brim
# the hat is not scaled; it is lowered by what the head's scale took from under its brim
HAT_DROP = (ORIGINAL_BRIM - HEAD_JOINT.z) * (1.0 - HEAD_SCALE)       # 0.0475
HEIGHT = ORIGINAL_TOP - HAT_DROP                                      # 0.9525, the hat's tip
SHARP_ANGLE = 40.0       # degrees: a chamfer is a hard edge, a soft corner or a taper is one form
TRIANGLE_BUDGET = 6000   # the original is 15,020

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
# THE HEAD. The original's box (the donor skull the whole cast wears, 0.343 to 0.661), in the
# `shaped` carving the owner picked on Dante: cheek fullness, soft corners, a jaw rounded in to
# the chin, every ring a superellipse of exponent 3.4 or more so the box wins.
#     rows are (z, half width, depth to the front, depth to the back, exponent)
# ---------------------------------------------------------------------------
HEAD_N = 24
HEAD_ROWS = [
    # DANTE'S HEAD SHAPE, on her box: soft superellipse corners (3.4 to 4.2), fullest at the
    # cheeks (0.181 against 0.171 at the temples), narrowing gently to the crown.
    # THE JAW ROUNDS IN GENTLY AND CLOSES LOW, as Amihan's does and for her reason: nothing of
    # hers stands round the jaw to hide a hard turn under, and in the toon shader a jaw that turns
    # under sharply catches the shadow band as dark wedges. The tight closing ring is from 0.349
    # down, on the shoulders.
    # THE FACE IS ONE FLAT PLANE. From 0.400 (under her mouth, which is at 0.410 to 0.420) to 0.604
    # (the hairline; her hat's brim is at 0.640) the front depth is ONE value, 0.165. No brow
    # ledge, no cheek standing proud: in the game's two-band shader any step there draws a level
    # line across the face. Cheek fullness lives only in the WIDTH.
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
    # Only the flat front takes the drawing (her face). The limit is 0.84 and not 0.93 so the
    # first three facets either side of the middle take it (they are 2, 13 and 31 degrees off the
    # plane): the fringe's shadow tone has to reach in under the cheek locks. Nothing is drawn
    # past |x| 0.126 but that one flat tone. Every other face of the head is flat skin, and the
    # faces that turn under at the jaw are the SAME skin, not a shade (no dark facets).
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], proj_square("head", "skin", "skin", 0.84))
    # NO NOSE, and NO EARS: her side hair hangs over where they would be, as on the original.


# ---------------------------------------------------------------------------
# THE HAIR. Hers on the original is ONE magenta (slot 6; the highlight slot 7 is in her palette
# and no triangle wears it): a crown under the hat, a straight fringe with two steps a side and
# one lock dipping between her eyes, a narrow lock down each cheek, three stepped columns a side
# over the ears (the middle one the longest and the proudest), and a bob behind that ends in five
# locks of graded lengths, the middle one the longest. Every one of those is kept, as one tapered
# block where the original stacks two to four boxes.
# TWO TONES, BY PIECE AND BY FACING, NOTHING DRAWN (rule 3): the locks that stand proud wear her
# magenta with her highlight slot on any face turned up; the masses behind them wear a darker
# step, so a lock reads against what it lies on.
# Stations are (centre, half width, half depth); z runs DOWN each lock from under the hat brim.
# ---------------------------------------------------------------------------
HAIR_CHAMFER = 0.011
LIT = tones("hair_lit", "hair", "hair_dark")
DARK = tones("hair", "hair_dark", "hair_under")
DEEP = tones("hair_dark", "hair_under", "hair_under")
BRIM_UNDER = 0.648     # the locks start just inside the brim's underside (0.640)
FRINGE_TOP = 0.600     # the fringe is ONE mass from the brim down (`hair-brow`); its locks part only below this
FRINGE = [
    # her right to her left. Feet: two steps at 0.512, two at 0.532, the notch lock at 0.474
    # (the original's 0.515, 0.535 and 0.475), each lock narrowing a little to its foot.
    ("fringe-r2", [((-0.102, -0.184, FRINGE_TOP), 0.034, 0.015), ((-0.104, -0.186, 0.512), 0.027, 0.010)], LIT, 0.006),
    ("fringe-r1", [((-0.040, -0.185, FRINGE_TOP), 0.031, 0.016), ((-0.043, -0.187, 0.532), 0.025, 0.010)], LIT, 0.006),
    ("fringe-notch", [((0.019, -0.187, FRINGE_TOP), 0.029, 0.017), ((0.019, -0.188, 0.530), 0.026, 0.014),
                      ((0.018, -0.189, 0.474), 0.011, 0.009)], LIT, 0.006),
    ("fringe-l1", [((0.059, -0.184, FRINGE_TOP), 0.013, 0.015), ((0.060, -0.186, 0.534), 0.010, 0.010)], LIT, 0.005),
    ("fringe-l2", [((0.102, -0.184, FRINGE_TOP), 0.034, 0.015), ((0.105, -0.186, 0.512), 0.027, 0.010)], LIT, 0.006),
    # down each cheek, in front of the side columns: the original's three boxes stepping in to a
    # foot at 0.385
    ("cheek-right", [((-0.155, -0.166, BRIM_UNDER), 0.021, 0.026), ((-0.155, -0.166, 0.480), 0.019, 0.024),
                     ((-0.155, -0.166, 0.386), 0.009, 0.014)], LIT, 0.006),
    ("cheek-left", [((0.155, -0.166, BRIM_UNDER), 0.021, 0.026), ((0.155, -0.166, 0.480), 0.019, 0.024),
                    ((0.156, -0.166, 0.386), 0.009, 0.014)], LIT, 0.006),
]
#   THE STACKED CURLS over each ear, for her LEFT (+x); the right is the mirror, as the original's
#   is. Three strands, each a STACK of the original's own boxes (x from, x to, y from, y to, z foot,
#   z top, read off tools/build_phaister_voxel.py and checked against the .glb's bounds): three
#   tiers in the fore and aft strands, four in the middle one, which stands furthest out and
#   hangs lowest. v01 to v04 made each strand one tapered plank; owner, 2026-10-05: "the side hair
#   is plainer than the original". Each tier here is a curl: it swells outward to its foot and
#   turns under, and the tier below starts narrower and tucked 10 mm up inside it, so the side of
#   her head steps down in shingles as the original's does.
CURLS = [
    ("side-fore", DARK, [(0.155, 0.230, -0.160, -0.065, 0.465, BRIM_UNDER), (0.160, 0.225, -0.155, -0.070, 0.405, 0.475),
                         (0.165, 0.218, -0.150, -0.075, 0.360, 0.415)]),
    ("side-mid", LIT, [(0.165, 0.240, -0.065, 0.040, 0.465, BRIM_UNDER), (0.170, 0.235, -0.060, 0.035, 0.385, 0.475),
                       (0.175, 0.230, -0.055, 0.030, 0.330, 0.395), (0.180, 0.225, -0.050, 0.025, 0.295, 0.340)]),
    ("side-aft", DARK, [(0.155, 0.232, 0.040, 0.145, 0.465, BRIM_UNDER), (0.160, 0.226, 0.045, 0.140, 0.395, 0.475),
                        (0.165, 0.220, 0.050, 0.135, 0.345, 0.405)]),
]
CURL_SWELL = 0.006     # how far a curl's foot stands out past the original's box
#   THE FIVE LOCKS OF THE BOB, her right to her left, stacked the same way (v05 stacked the sides
#   and left these as planks, and from behind the two did not belong to one head of hair). The
#   original's boxes again: two tiers in the outer locks, three in the next, four in the middle one.
BACK_CURLS = [
    ("back-1", DARK, [(-0.210, -0.135, 0.125, 0.175, 0.435, BRIM_UNDER), (-0.200, -0.145, 0.130, 0.170, 0.385, 0.445)]),
    ("back-2", LIT, [(-0.135, -0.060, 0.130, 0.182, 0.415, BRIM_UNDER), (-0.128, -0.068, 0.135, 0.178, 0.355, 0.425),
                     (-0.120, -0.075, 0.138, 0.174, 0.325, 0.365)]),
    ("back-3", LIT, [(-0.060, 0.060, 0.135, 0.188, 0.425, BRIM_UNDER), (-0.052, 0.052, 0.138, 0.192, 0.355, 0.435),
                     (-0.042, 0.042, 0.142, 0.188, 0.310, 0.365), (-0.028, 0.028, 0.145, 0.184, 0.285, 0.320)]),
    ("back-4", LIT, [(0.060, 0.135, 0.130, 0.182, 0.415, BRIM_UNDER), (0.068, 0.128, 0.135, 0.178, 0.355, 0.425),
                     (0.075, 0.120, 0.138, 0.174, 0.325, 0.365)]),
    ("back-5", DARK, [(0.135, 0.210, 0.125, 0.175, 0.435, BRIM_UNDER), (0.145, 0.200, 0.130, 0.170, 0.385, 0.445)]),
]
HAIR_FOLLOW = 0.5      # how much of the shoulders a hair tip below the jaw keeps when the head turns


def hair_weights(co):
    """Hair above the jaw is the head's. A tip hanging below it gives up to half of itself to the
    torso, so a turned head does not sweep a rigid lock through her shoulder (rule 7)."""
    w = HAIR_FOLLOW * _ramp(0.372, 0.296, co.z)
    return [("head", 1.0 - w), ("torso", w)]


def build_hair(part):
    # the crown under the hat: its front stands 7 mm proud of the face plane, the fringe hangs
    # from that edge, and it shows between the fringe locks as the darker hair behind them
    part.block("hair-crown", "head", X, Y, [((0, 0.0, 0.545), 0.222, (0.172, 0.170)), ((0, 0.0, BRIM_UNDER), 0.222, (0.172, 0.170))],
               DARK, HAIR_CHAMFER)
    # the mass down the back, on the back of the head: what shows between the five locks is hair
    part.block("hair-back", "head", X, Y, [((0, 0.140, 0.340), 0.186, 0.027), ((0, 0.140, BRIM_UNDER), 0.208, 0.032)],
               DEEP, 0.0)
    # the fringe's mass under the brim. v01 hung each lock from the brim on its own and the fringe
    # read as a row of planks; the original's is one mass with steps cut in its foot.
    part.block("hair-brow", "head", X, Y, [((0, -0.181, 0.556), 0.150, 0.014), ((0, -0.181, BRIM_UNDER), 0.150, 0.014)], LIT, 0.006)
    # a thin layer of hair on each side of the head, behind the three columns: what shows between
    # their feet above the jaw is hair, not a strip of bare skin (v01, from the side)
    for s in (1, -1):
        part.block("hair-side-liner", "head", X, Y, [((s * 0.170, 0.004, 0.412), 0.016, 0.128), ((s * 0.170, 0.004, BRIM_UNDER), 0.016, 0.128)],
                   flat("hair_under"), 0.0)
    for name, stations, paint, chamfer in FRINGE:
        part.block("hair-" + name, "head", X, Y, stations, paint, chamfer)
    def stack(name, paint, tiers, s=1):
        for x0, x1, y0, y1, foot, top in tiers:
            cx, cy = s * 0.5 * (x0 + x1), 0.5 * (y0 + y1)
            hx, hy = 0.5 * (x1 - x0), 0.5 * (y1 - y0)
            # from its top (open, inside the tier above or the brim) out to the swell just above
            # its foot, then turned under and closed: 40 triangles a curl
            rings = [rect_ring((cx, cy, top), X, Y, hx - 0.003, hy - 0.003, 0.010),
                     rect_ring((cx, cy, foot + 0.014), X, Y, hx + CURL_SWELL, hy + CURL_SWELL, 0.013),
                     rect_ring((cx, cy, foot), X, Y, hx - 0.006, hy - 0.006, 0.008)]
            faces = part.loft("hair-" + name, "head", rings, paint, caps=(False, True))
            part.bend(faces, hair_weights)

    for name, paint, tiers in CURLS:
        for s in (1, -1):
            stack(name, paint, tiers, s)
    for name, paint, tiers in BACK_CURLS:
        stack(name, paint, tiers)


# ---------------------------------------------------------------------------
# THE HAT, HER SILHOUETTE. Black felt: an eight-sided brim 0.660 across at 0.640 to 0.680, a
# purple ribbon band to 0.755 with her gold buckle on its front, and a TALL STEPPED CONE of five
# tiers that walks BACK as it climbs to a flat tip at 1.000. Every tier is the original's own
# box (`TIERS`). On it, all the original's, at their own sizes and places: two wands tucked in
# the band on her LEFT, three hat pins lying out of the band on her RIGHT, two moths on the brim.
# It is built here at the original's heights and, in `build`, LOWERED by `HAT_DROP` and NOT
# scaled with the head (the docstring, rule 3).
# v01 to v04 made the cone one loft with eight-sided rings and soft creases and let it shrink
# with the head. Owner, 2026-10-05: "short, lumpy, blunt". So: square tiers with a small cut
# corner, walls that lean in only 5 per cent, level ledges, a flat top.
# ---------------------------------------------------------------------------
AHEAD = (0, -1, 0)
FELT = tones("coat_lit", "coat", "coat_deep")
GOLD_T = tones("gold_lit", "gold", "gold_dark")
LILAC_T = tones("lilac_lit", "lilac", "lilac_dark")
#   the cone's five tiers, the original's boxes: (z foot, z top, half width, y front, y back)
TIERS = [(0.755, 0.820, 0.185, -0.185, 0.185), (0.820, 0.880, 0.150, -0.120, 0.180), (0.880, 0.935, 0.110, -0.060, 0.165),
         (0.935, 0.975, 0.065, 0.010, 0.130), (0.975, 1.000, 0.028, 0.040, 0.105)]
TIER_LEAN = 0.05       # how much narrower a tier's top is than its foot


def spike(part, name, bone, stations, mapping, chamfer, across=X):
    """A block through `stations` = [(centre, half, half)] that may lean any way: a wand, a pin."""
    w = (Vector(stations[-1][0]) - Vector(stations[0][0])).normalized()
    u = (across - w * across.dot(w)).normalized()
    v = w.cross(u).normalized()
    return part.block(name, bone, u, v, stations, mapping, chamfer)


def slab(part, name, bone, centre, u, v, hu, hv, back, front, paint, cut=0.004, corner=0.0):
    """A plate lying on her front: a ring `back` behind `centre`, its face `front` ahead of it
    with the edge cut. Three rings, no back face (it is sunk in what it lies on)."""
    n = Vector(AHEAD)
    c = Vector(centre)
    rings = [rect_ring(c - n * back, u, v, hu, hv, corner),
             rect_ring(c + n * (front - cut), u, v, hu, hv, corner),
             rect_ring(c + n * front, u, v, hu - cut, hv - cut, max(corner - 0.4 * cut, 0.0))]
    return part.loft(name, bone, rings, paint, caps=(False, True))


def build_hat(part):
    def ring(z, half, cut, cy=0.0, deep=None):
        return rect_ring((0, cy, z), X, Y, half, deep if deep is not None else half, cut)

    # the brim: its edge rounded over. Its underside is closed (the head under it is smaller now
    # and the hole would show from below).
    part.loft("hat-brim", "head", [ring(0.642, 0.120, 0.030), ring(0.640, 0.316, 0.080), ring(0.648, 0.330, 0.084),
                                   ring(0.672, 0.330, 0.084), ring(0.680, 0.318, 0.080), ring(0.681, 0.226, 0.056)],
              FELT, caps=(True, False))
    # the ribbon band
    part.loft("hat-band", "head", [ring(0.680, 0.240, 0.062), ring(0.755, 0.236, 0.060)],
              swatch("ribbon", (0.0, 1.0), (0.0, 1.0)), caps=(False, True))
    # the cone: one skin over the five tiers, a foot ring and a top ring to each, so every wall
    # is one flat plane and every ledge is level
    rings = []
    for foot, top, hx, front, back in TIERS:
        mid, hy = 0.5 * (front + back), 0.5 * (back - front)
        cut = 0.16 * min(hx, hy)
        rings.append(rect_ring((0, mid, foot), X, Y, hx, hy, cut))
        rings.append(rect_ring((0, mid, top), X, Y, hx * (1.0 - TIER_LEAN), hy * (1.0 - TIER_LEAN), cut * (1.0 - TIER_LEAN)))
    part.loft("hat-cone", "head", rings, FELT, caps=(False, True))
    # her buckle, 124 by 65 mm, 16 mm proud of the band's front
    part.block("hat-buckle", "head", X, Z, [((0, -0.232, 0.7175), 0.062, 0.0325), ((0, -0.256, 0.7175), 0.062, 0.0325)],
               panel("buckle", 0, 2, (-0.062, 0.062, 0.685, 0.750), AHEAD, "gold_dark"), 0.005)

    # THE TWO WANDS, tucked in the band on her left: a wood shaft, a crimson wrap, a lilac crystal
    # cut to a point (the original's is two stacked boxes). The fore one is the taller.
    wood = tones("wood", "wood", "wood_dark")
    for foot, top, wrap_z, bulge, point in (((0.232, -0.084, 0.690), (0.240, -0.097, 0.884), (0.720, 0.745), 0.910, 0.957),
                                            ((0.232, 0.038, 0.690), (0.2415, 0.050, 0.859), (0.725, 0.750), 0.884, 0.932)):
        foot, top = Vector(foot), Vector(top)
        lean = (top - foot) / (top.z - foot.z)
        at = lambda z, foot=foot, lean=lean: tuple(foot + lean * (z - foot.z))
        spike(part, "wand-shaft", "head", [(at(foot.z), 0.0150, 0.0150), (at(top.z), 0.0120, 0.0120)], wood, 0.0)
        spike(part, "wand-wrap", "head", [(at(wrap_z[0]), 0.0178, 0.0178), (at(wrap_z[1]), 0.0172, 0.0172)], flat("wrap"), 0.0)
        spike(part, "wand-crystal", "head", [(at(top.z - 0.004), 0.0110, 0.0110), (at(bulge), 0.0205, 0.0205), (at(point), 0.0060, 0.0060)],
              LILAC_T, 0.0)

    # THE THREE HAT PINS on her right, each its own length, height, direction and head colour
    # (the original's): a gold shaft and a bead.
    for a, b, bead, tone in (((-0.228, -0.116, 0.740), (-0.302, -0.116, 0.740), 0.0115, LILAC_T),
                             ((-0.108, -0.246, 0.710), (-0.192, -0.249, 0.710), 0.0115, tones("crimson_lit", "crimson", "crimson_dark")),
                             ((-0.236, -0.026, 0.722), (-0.286, -0.026, 0.738), 0.0100, tones("white", "white", "white_shade"))):
        a, b = Vector(a), Vector(b)
        spike(part, "hatpin-shaft", "head", [(tuple(a), 0.0042, 0.0042), (tuple(b), 0.0042, 0.0042)], GOLD_T, 0.0, across=Z)
        d = (b - a).normalized()
        spike(part, "hatpin-bead", "head", [(tuple(b - d * 0.002), bead, bead), (tuple(b + d * (2.0 * bead - 0.002)), bead, bead)],
              tone, 0.0, across=Z)

    # THE TWO MOTHS resting on the brim, one front left facing out, one back right lying across:
    # a purple body and two lilac wings, each wing a fan lifted a little off the felt
    purple = tones("purple_lit", "purple", "purple_dark")
    top = 0.681
    for (cx, cy), along in (((0.160, -0.300), Y), ((-0.277, 0.212), X)):
        side = X if along is Y else Y
        c = Vector((cx, cy, top + 0.005))
        spike(part, "moth-body", "head", [(tuple(c - along * 0.021), 0.0050, 0.0050), (tuple(c + along * 0.021), 0.0055, 0.0055)],
              purple, 0.0, across=Z)
        for k in (1, -1):
            base = c + side * (0.006 * k) + along * 0.002
            tip = c + side * (0.047 * k) - along * 0.004 + Z * 0.013
            spike(part, "moth-wing", "head", [(tuple(base), 0.010, 0.0028), (tuple(tip), 0.021, 0.0028)], LILAC_T, 0.0, across=along)


# ---------------------------------------------------------------------------
# THE TORSO AND THE COAT. One block, black (CAST_CLOTHING_STYLE.md rule 1: a dark base garment).
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.343)
TORSO_LOW = (0.146, 0.086)     # half width, half depth at the hips (the original's coat core is 0.148 by 0.085)
TORSO_HIGH = (0.136, 0.084)    # at the shoulders
TORSO_CY = -0.002
ARM_Y, ARM_Z = 0.006, 0.288    # the line her arms are built along (the arm bone is at z 0.288)


def torso_half(z):
    t = (z - TORSO_Z[0]) / (TORSO_Z[1] - TORSO_Z[0])
    return (TORSO_LOW[0] + (TORSO_HIGH[0] - TORSO_LOW[0]) * t, TORSO_LOW[1] + (TORSO_HIGH[1] - TORSO_LOW[1]) * t)


def chest_y(z):
    return TORSO_CY - torso_half(z)[1]


# THE COAT'S SKIRT: one piece round the hips under the belt, split down the front (the
# original's two front panels with their diagonal purple trims), flaring sideways to a purple
# band at 0.070. The band and the two front edges are geometry, 4 mm proud.
SKIRT_TOP = tex.SKIRT_TOP
SKIRT_FOLLOW = 0.78                    # how much of a leg's swing the hem takes
SKIRT_TOP_R = (0.152, 0.096, 0.088)    # half width, depth to the front, depth to the back
SKIRT_HEM_R = (0.188, 0.104, 0.092)    # the original's hem: |x| 0.180 to 0.192, y -0.098
SKIRT_BAND = 0.018
SKIRT_MID = 0.5 * (SKIRT_TOP + tex.HEM + SKIRT_BAND)


def skirt_f(z):
    """How far down the flare a height is: 0 at the belt, 1 at the hem (a bell, not a cone)."""
    return _table([(tex.HEM, 1.0), (tex.HEM + SKIRT_BAND, 0.97), (SKIRT_MID, 0.55), (SKIRT_TOP, 0.0)], z)


def skirt_front_y(z):
    f = skirt_f(z)
    return TORSO_CY - (SKIRT_TOP_R[1] + (SKIRT_HEM_R[1] - SKIRT_TOP_R[1]) * f)


def skirt_weights(co):
    down = _ramp(SKIRT_TOP, 0.090, co.z) ** 0.8
    leg = SKIRT_FOLLOW * down
    left = _smooth(_ramp(-0.070, 0.070, co.x))
    return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]


# THE CAPE. Black outside, crimson inside, hung from the shoulders behind her and falling in TWO
# SWALLOWTAILS: the hem is an inverted V, 0.140 at the middle and 0.046 at the outer tips (the
# original's five stepped boxes a side), edged with a purple band (its stepped chevron trim). Its
# sides turn forward round her, so the crimson lining shows beside her legs from the front, as
# the original's lining wings and side flaps do. Her moon and stars are on its back (textures).
# Half of it, from the middle out: (x, y, degrees its outside is turned from straight back, the
# hem's height there, the top's height there).
CAPE = [(0.000, 0.102, 0.0, 0.140, 0.338), (0.024, 0.102, 0.0, 0.136, 0.338), (0.066, 0.102, 0.0, 0.118, 0.338),
        (0.110, 0.102, 0.0, 0.096, 0.338), (0.150, 0.101, 5.0, 0.076, 0.338), (0.184, 0.097, 18.0, 0.058, 0.334),
        (0.206, 0.086, 44.0, 0.046, 0.322), (0.216, 0.062, 78.0, 0.060, 0.296)]
CAPE_FLARE = 0.030      # how far the foot stands off further than the shoulders
CAPE_BAND = 0.022
CAPE_FOLLOW = 0.62      # how much of a leg's swing a tail takes


def cape_weights(co):
    down = _ramp(0.215, 0.060, co.z) ** 0.9
    leg = CAPE_FOLLOW * down
    left = _smooth(_ramp(-0.080, 0.080, co.x))
    return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]


def build_torso(part):
    coat = proj_square("torso", "coat", "coat_deep", 0.90, ends=(2, "coat_mid"))
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, TORSO_Z[0]), *TORSO_LOW), ((0, TORSO_CY, TORSO_Z[1]), *TORSO_HIGH)],
               coat, 0.014)
    # her trousers' seat, between the legs under the skirt
    part.block("pelvis", "torso", X, Y, [((0, 0.0, 0.100), 0.058, 0.068), ((0, 0.0, 0.178), 0.058, 0.068)], flat("coat_deep"), 0.0)
    # her neck, for when the head tips back and the chin lifts off the collar
    neck = part.block("neck", "torso", X, Y, [((0, TORSO_CY, 0.326), 0.050, 0.048), ((0, TORSO_CY, 0.354), 0.048, 0.046)],
                      flat("skin_shade"), 0.008)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.332, 0.350, co.z)), ("head", 0.7 * _ramp(0.332, 0.350, co.z))])

    # her throat, between the collar wings, up under the chin: skin in the chin's shade. Without it
    # the coat block's own top edge crossed the neck as a black band and read as a choker (v01).
    part.block("throat", "torso", X, Y, [((0, -0.060, 0.316), 0.045, (-chest_y(0.330) - 0.060 + 0.0015, 0.030)),
                                         ((0, -0.060, 0.347), 0.045, (-chest_y(0.330) - 0.060 + 0.0015, 0.030))],
               flat("skin_shade"), 0.0)

    # THE BELT, purple, worn over the coat (the original's is 20 mm proud in front), and HER BUCKLE,
    # her one big fastening (clothing rule 4): 104 by 60 mm
    (wl, dl), (wh, dh) = torso_half(tex.BELT[0]), torso_half(tex.BELT[1])
    part.block("belt", "torso", X, Y, [((0, TORSO_CY, tex.BELT[0]), wl + 0.010, (dl + 0.016, dl + 0.008)),
                                       ((0, TORSO_CY, tex.BELT[1]), wh + 0.010, (dh + 0.016, dh + 0.008))],
               proj_square("torso", "purple", "purple_dark", 0.90, ends=(2, "purple_lit")), 0.005)
    belt_front = TORSO_CY - (dl + 0.016)
    part.block("belt-buckle", "torso", X, Z, [((0, belt_front + 0.004, 0.184), 0.052, 0.030), ((0, belt_front - 0.016, 0.184), 0.052, 0.030)],
               panel("buckle", 0, 2, (-0.052, 0.052, 0.154, 0.214), AHEAD, "gold_dark"), 0.005)

    # THE COAT'S SKIRT. Its top ring is under the belt; toward the hem each side takes up to
    # SKIRT_FOLLOW of its own leg, and across the middle the two legs are mixed (rule 7).
    #   round from her right front edge, behind her, to her left front edge: (across, fore, proud)
    half = [(0.075, 1.0, True), (0.210, 1.0, True), (0.210, 1.0, False), (0.420, 1.0, False)]
    for theta in (24, 45, 66, 90, 114, 135, 156):
        a = math.radians(theta)
        half.append((abs(math.sin(a)) ** 0.4, math.copysign(abs(math.cos(a)) ** 0.4, math.cos(a)), False))
    rounds = [(-px, py, proud) for px, py, proud in half] + [(0.0, -1.0, False)] + [(px, py, proud) for px, py, proud in reversed(half)]
    hem = tex.HEM
    sections = []
    for px, py, proud in rounds:
        lift = -0.004 if proud else 0.0

        def at(z, inset, px=px, py=py, lift=lift):
            f = skirt_f(z)
            r = [SKIRT_TOP_R[k] + (SKIRT_HEM_R[k] - SKIRT_TOP_R[k]) * f - inset - lift for k in range(3)]
            return Vector((px * r[0], TORSO_CY - py * (r[1] if py > 0 else r[2]), z))

        sections.append([at(SKIRT_TOP, 0.0), at(SKIRT_MID, 0.0), at(hem + SKIRT_BAND, 0.0), at(hem + SKIRT_BAND + 0.0005, -0.004),
                         at(hem, -0.004), at(hem + 0.003, 0.008), at(SKIRT_MID, 0.009), at(SKIRT_TOP, 0.009)])
    last = len(rounds) - 2
    cloth = proj_square("torso", "coat", "coat_deep", 0.86)

    def skirt_paint(i, j):
        if j >= 5:
            return flat("coat_deep")
        if i <= 1 or i >= last - 1:
            return flat("purple")
        return (cloth, cloth, flat("purple_lit"), flat("purple"), flat("purple_dark"))[j]

    skirt = part.loft("coat-skirt", "torso", sections, skirt_paint)
    part.bend(skirt, skirt_weights)

    # THE COLLAR. The original's "high upturned witch collar" is five crimson boxes whose tops
    # (0.365) are INSIDE the head block (its foot is at 0.343 and it is wider and deeper than
    # they are): what shows is a crimson wing on the chest either side of the V of skin, up to
    # the jaw and no higher, and a strip behind the neck. So that is what is built: two wings
    # that widen as they rise to 0.345 and a low band behind. NOTHING OF IT STANDS ABOVE THE
    # HEAD'S FOOT, so the head turns OVER it and cannot turn through it at any yaw, as Amihan's
    # capelet lets hers. It is rigid on the torso and takes no weight from the head: a wing that
    # followed the head would shear sideways through the chain on a turn.
    cy = chest_y(0.310) - 0.004
    for s in (1, -1):
        part.block("collar-wing", "torso", X, Y, [((s * 0.091, cy, 0.282), 0.040, 0.010), ((s * 0.094, cy, 0.322), 0.044, 0.011),
                                                  ((s * 0.099, cy - 0.001, 0.345), 0.049, 0.012)],
                   panel("collar", 0, 2, (s * 0.048, s * 0.150, 0.279, 0.349), AHEAD, "crimson"), 0.004)
    part.block("collar-back", "torso", X, Y, [((0, 0.083, 0.300), 0.120, 0.013), ((0, 0.083, 0.345), 0.124, 0.013)],
               tones("crimson_lit", "crimson", "crimson_dark"), 0.0)

    # HER GOLD CHAIN: three links a side stepping down the collar to the pendant (the original's
    # six boxes), each link turned to lie along the V, the lower ones standing further proud.
    # Each link is short of the next by 5 to 7 mm: v01's links overlapped and read as one gold strap.
    wing_front = cy - 0.011
    for s in (1, -1):
        u = Vector((s * 0.810, 0.0, 0.587))
        v = Vector((-s * 0.587, 0.0, 0.810))
        for (x, z), hu, hv, proud in (((0.1050, 0.3235), 0.0185, 0.0125, 0.006), ((0.0705, 0.2985), 0.0175, 0.0125, 0.009),
                                      ((0.0395, 0.2760), 0.0150, 0.0115, 0.012)):
            slab(part, "chain-link", "torso", (s * x, wing_front, z), u, v, hu, hv, 0.018, proud, GOLD_T, 0.004)
    # THE PENDANT: a gold setting with its corners cut, a lilac stone cut to a table, the white
    # glint on the stone's upper right (the original's own white box), a gold drop under it
    slab(part, "pendant-frame", "torso", (0, wing_front, 0.258), X, Z, 0.028, 0.021, 0.018, 0.014, GOLD_T, 0.004, corner=0.008)
    stone = wing_front - 0.012
    part.block("pendant-stone", "torso", X, Z, [((0, stone, 0.258), 0.020, 0.0150), ((0, stone - 0.012, 0.258), 0.012, 0.0085)], LILAC_T, 0.0)
    part.block("pendant-glint", "torso", X, Z, [((-0.0065, stone - 0.011, 0.2615), 0.0034, 0.0034), ((-0.0065, stone - 0.0135, 0.2615), 0.0034, 0.0034)],
               flat("white"), 0.0)
    part.wedge("pendant-drop", "torso", (0, wing_front - 0.004, 0.240), (0, wing_front - 0.004, 0.223), (0.010, 0.007), (0.003, 0.004), GOLD_T, 0.0)

    # THE MANIKA at her left hip: the rag doll MANIKA MISCHIEF throws, hung from the belt on a
    # gold cord in front of the skirt, a hat pin through its body (all the original's pieces:
    # a head, a body, two arms, two stitched X eyes, a stitched mouth, a seam). It lies on the
    # skirt, so it takes the skirt's weights at the place it hangs and swings with it as ONE
    # rigid thing.
    mx, mz = 0.122, 0.140
    my = skirt_front_y(0.128) - 0.004
    doll = []
    burlap = "burlap_dark"
    doll += part.block("manika-head", "torso", X, Z, [((mx, my + 0.004, mz), 0.024, 0.018), ((mx, my - 0.036, mz), 0.024, 0.018)],
                       panel("manika", 0, 2, (mx - 0.024, mx + 0.024, mz - 0.018, mz + 0.018), AHEAD, burlap), 0.006)
    doll += part.block("manika-body", "torso", X, Z, [((mx, my + 0.004, 0.111), 0.018, 0.012), ((mx, my - 0.028, 0.111), 0.018, 0.012)],
                       panel("manika_body", 0, 2, (mx - 0.018, mx + 0.018, 0.099, 0.123), AHEAD, burlap), 0.003)
    rag = tones("burlap", "burlap", "burlap_dark")
    doll += part.block("manika-arm", "torso", X, Z, [((mx + 0.0235, my - 0.006, 0.110), 0.0065, 0.0060), ((mx + 0.0235, my - 0.022, 0.110), 0.0065, 0.0060)], rag, 0.0)
    doll += part.block("manika-arm", "torso", X, Z, [((mx - 0.0235, my - 0.006, 0.114), 0.0065, 0.0060), ((mx - 0.0235, my - 0.022, 0.114), 0.0065, 0.0060)], rag, 0.0)
    doll += part.block("manika-cord", "torso", X, Y, [((mx, my - 0.012, 0.156), 0.004, 0.004), ((mx, my - 0.012, 0.168), 0.004, 0.004)], GOLD_T, 0.0)
    doll += spike(part, "manika-pin", "torso", [((mx + 0.011, my - 0.010, 0.111), 0.0030, 0.0030), ((mx + 0.011, my - 0.066, 0.111), 0.0030, 0.0030)],
                  GOLD_T, 0.0, across=Z)
    doll += part.block("manika-pin-bead", "torso", X, Z, [((mx + 0.011, my - 0.064, 0.111), 0.0080, 0.0080), ((mx + 0.011, my - 0.080, 0.111), 0.0080, 0.0080)],
                       LILAC_T, 0.0)
    hang = skirt_weights(Vector((mx, my, 0.125)))
    part.bend(doll, lambda co: hang)

    # THE CAPE (see the note above CAPE)
    cape_cloth = proj_square("cape", "coat", "coat_deep", 0.85)
    columns = [(-x, y, -turn, low, high) for x, y, turn, low, high in reversed(CAPE[1:])] + list(CAPE)
    sections = []
    for x, y, turn, low, high in columns:
        a = math.radians(turn)
        out = Vector((math.sin(a), math.cos(a), 0.0))
        base = Vector((x, y, 0.0))

        def at(z, stand=0.0, base=base, out=out):
            flare = CAPE_FLARE * (1.0 - _ramp(0.045, 0.335, z))
            return base + out * (flare + stand) + Z * z

        mid = 0.5 * (high - 0.030 + low + CAPE_BAND)
        sections.append([at(high - 0.002, -0.030), at(high + 0.002, -0.004), at(high - 0.030), at(mid), at(low + CAPE_BAND),
                         at(low + CAPE_BAND + 0.0005, 0.004), at(low, 0.004), at(low + 0.002, -0.012), at(mid, -0.013)])

    def cape_paint(i, j):
        return (flat("coat_mid"), cape_cloth, cape_cloth, cape_cloth, flat("purple_lit"), flat("purple"), flat("purple_dark"),
                flat("crimson"), flat("crimson"))[j]

    cape = part.loft("cape", "torso", sections, cape_paint)
    part.bend(cape, cape_weights)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests. `s` is +1 for her left, -1 for her right.
# The original's sleeve is a box 156 mm tall from the shoulder. Here it is a BELL: narrow at the
# shoulder (so it sits under the smaller head and her side hair) and wide at the cuff, which
# keeps the original's size (166 mm tall, 176 deep). From the shoulder out: the black sleeve to
# 0.178, on the arm bone; then, on the elbow bone, the purple band with her gold cross from 0.172 to 0.207,
# the gold rim to 0.215, the white cuff to 0.238, and the hand from 0.236 to 0.285.
# ---------------------------------------------------------------------------

def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    arm_block(part, "sleeve", bone, s, [(0.098, (0.048, 0.050)), (0.140, (0.060, 0.064)), (0.180, (0.070, 0.076))],
              proj_square(group, "coat", "coat_deep", 0.90, ends=(0, "coat_deep")), 0.011)
    # the band starts 8 mm INSIDE the sleeve's mouth (0.180) and is wider than it, so when the elbow folds
    # the band turns round the sleeve's end like a cap and no gap opens on the outside of the bend
    arm_block(part, "sleeve-band", fore, s, [(tex.BAND[0], (0.0725, 0.0785)), (tex.BAND[1], (0.0745, 0.0805))],
              proj_square(group, "purple", "purple_dark", 0.90, ends=(0, "purple_dark")), 0.004)
    arm_block(part, "sleeve-rim", fore, s, [(tex.RIM[0] - 0.0005, (0.0775, 0.0835)), (tex.RIM[1] + 0.0005, (0.0780, 0.0840))],
              proj_square(group, "gold", "gold_dark", 0.90, ends=(0, "gold_dark")), 0.0)
    arm_block(part, "cuff", fore, s, [(tex.CUFF[0], (0.0800, 0.0860)), (tex.CUFF[1], (0.0820, 0.0880))],
              proj_square(group, "white", "white_shade", 0.90, ends=(0, "white_shade")), 0.005)
    # the crimson lining peeking 4 mm below the cuff along its hem (the original's under-cuff box).
    # v01 painted the whole mouth of the cuff crimson instead and the cuff read as a red slab.
    part.block("cuff-lining", fore, Y, Z, [((s * (tex.CUFF[0] + 0.0015), ARM_Y, ARM_Z - 0.0885), 0.072, 0.004),
                                           ((s * (tex.CUFF[1] - 0.0015), ARM_Y, ARM_Z - 0.0905), 0.074, 0.004)], flat("crimson"), 0.0)
    # her wrist, inside the cuff: a flat tone (rule 4)
    arm_block(part, "wrist", fore, s, [(0.212, (0.038, 0.046)), (0.246, (0.038, 0.046))], tones("skin", "skin", "skin_shade"), 0.0)
    # the hand: a plain block, the cast's, its fingers and thumb drawn on its flat front and back
    # only. It is the far end of the forearm bone and its top is 0.062 above the arm bone.
    arm_block(part, "hand", fore, s, [(tex.WRIST, (0.044, 0.062)), (tex.HAND_END, (0.044, 0.062))],
              proj_square(group, "skin", "skin_shade", 0.93), 0.013)


# ---------------------------------------------------------------------------
# THE LEGS. Short, the cast's: wide black trousers, a crimson band at the ankle, a purple shoe on
# a thick white sole. Feet turned out four degrees.
# ---------------------------------------------------------------------------
LEG_X = 0.0836
TOE_OUT = 4.0


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    part.block("trouser", bone, X, Y, [((s * LEG_X, 0.0, tex.ANKLE[1] - 0.002), 0.069, 0.078), ((s * (LEG_X - 0.004), 0.0, 0.182), 0.063, 0.073)],
               proj_square(group, "coat", "coat_deep", 0.90, ends=(2, "coat_deep")), 0.009)
    part.block("ankle-band", bone, X, Y, [((s * LEG_X, 0.0, tex.ANKLE[0] - 0.002), 0.0715, 0.0805), ((s * LEG_X, 0.0, tex.ANKLE[1] + 0.001), 0.0715, 0.0805)],
               tones("crimson_lit", "crimson", "crimson_dark"), 0.0)

    turn = math.radians(TOE_OUT) * s
    cs, sn = math.cos(turn), math.sin(turn)

    def turned(p):
        x, y = p.x - s * LEG_X, p.y - 0.02
        return Vector((s * LEG_X + x * cs - y * sn, 0.02 + x * sn + y * cs, p.z))

    def station(y, half, z0, z1, cut):
        return [turned(p) for p in rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)]

    # the shoe: low at the toe, rising to the ankle (the original's toe box is 0.048 high, the rest 0.060)
    shoe = [(-0.134, 0.056, 0.022, 0.040, 0.006), (-0.122, 0.069, 0.022, 0.049, 0.010), (-0.088, 0.071, 0.022, 0.059, 0.011),
            (0.040, 0.071, 0.022, 0.062, 0.011), (0.074, 0.069, 0.022, 0.062, 0.011), (0.082, 0.060, 0.024, 0.054, 0.006)]
    part.loft("shoe", bone, [station(*row) for row in shoe], proj_square(group, "purple", "purple_dark", 0.88))
    # the thick white sole, wider than the shoe all round (the original's is 155 by 220 mm, 24 thick)
    h = 0.5 * tex.SOLE_TOP
    part.block("shoe-sole", bone, X, Z, [((s * LEG_X, -0.138, h), 0.060, h), ((s * LEG_X, -0.100, h), 0.0775, h),
                                         ((s * LEG_X, 0.000, h), 0.0775, h), ((s * LEG_X, 0.086, h), 0.068, h)],
               tones("white", "white", "white_shade"), 0.005, move=turned)


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
    """team-phaister.glb with its meshes swapped: skeleton, bind matrices and clips copied untouched,
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (phaister)"}
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
    print("triangles %d  (budget %d, the original is 15020)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if abs(hi.z - HEIGHT) > 0.004:
        raise SystemExit("the top is at %.4f, not the %.4f of the full-size hat lowered %.4f" % (hi.z, HEIGHT, HAT_DROP))
    if tris > TRIANGLE_BUDGET:
        raise SystemExit("%d triangles is over the budget of %d" % (tris, TRIANGLE_BUDGET))
    # THE HAND, FOR HER KIT (see the docstring). The arm must run along x; the hand block must be
    # the far end of the forearm bone; its top must sit where the original's does (0.066 above the
    # arm bone; a carried tsinelas is parked at +0.0555 with HandTopLift 0.0617 over the palm).
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
        far = max(p.x * sign for p in arm)
        if abs(far - max(p.x * sign for p in fore)) > 1e-6:
            raise SystemExit("the far end of arm-%s is not on the forearm bone" % side)
        hand = [p for p in fore if p.x * sign > far - 0.040]
        top = max(p.z for p in hand) - ARM_Z
        palm = [0.5 * (max(p[a] for p in hand) + min(p[a] for p in hand)) for a in range(3)]
        print("hand %-5s far end x %.4f, top %.4f above the arm bone (the original's is 0.066), block middle %.4f %.4f %.4f"
              % (side, far * sign, top, palm[0], palm[1], palm[2]))
        if not 0.045 <= top <= 0.066:
            raise SystemExit("the hand's top moved; a carried tsinelas would float or sink")
        if abs(far - 0.285) > 0.003:
            raise SystemExit("the hand's far end moved off the original's 0.285")


def build(armature, material):
    body, head = Part("body-mesh"), Part("head-mesh")
    build_torso(body)
    build_arm(body, 1)
    build_arm(body, -1)
    build_leg(body, 1)
    build_leg(body, -1)
    build_head(head)
    build_hair(head)
    before = set(head.bm.verts)
    build_hat(head)
    hat = set(head.bm.verts) - before
    objects = []
    for part in (body, head):
        part.resolve_uvs()
        # AFTER the paint is placed (it is projected from the full-size shape): the head comes in.
        # A vertex wholly on the head bone is scaled by HEAD_SCALE about the head joint; a vertex
        # that shares the head with the torso (the hair tips below the jaw, the top of the neck) by
        # the same share, so nothing tears where the two meet. This is the ramp Dante's collar
        # takes. Her own collar takes no head weight and is not moved: its top (0.345) lies under
        # the head's foot (0.343 to 0.348 after the scale) and the neck stays closed.
        for v in part.bm.verts:
            if v in hat:
                # THE HAT IS NOT SCALED (the docstring, rule 3): it is lowered onto the smaller head
                v.co.z -= HAT_DROP
                continue
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
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_phaister_textures.py")
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
