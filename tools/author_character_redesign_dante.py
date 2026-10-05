"""Build the Dante (displayed: Basilio) redesign PROTOTYPE: the same block kid, shaped and painted.

    py -3 tools/author_character_redesign_dante_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_dante.py
    ... -- --head block|carved|shaped --hair blocks|locks --out other.glb    (one variant, for a look)

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign.glb         (block hair)
    Assets/TumbangPreso/Art/CharacterRedesign/dante/dante-redesign-spiky.glb   (lock hair)
    ArtSource/dante/redesign-20261005/dante_redesign.blend                     (both heads)
beside the atlas the two share. Everything but the hair is the same in both.

WHY. Owner, 2026-10-05: *"can you try redesigning one of the normal blocky character models? add
texture, more unique shapes and be less oriented around the whole blocky aesthetic"*. The cast
is chamfered boxes in flat palette colours (docs/Voxel_Person_Guide.md); beside Paete's carved,
vine-wrapped blocks they read as plain boxes, and a flat face plate has *"no shading, texture or
any of that stylized character"*.

⚠️⚠️ THE HEAD STAYS A BOX. The first pass of this file lofted a round skull with a jaw and cheeks
and thin swept hair spikes. Owner, the same day, on its face close-up: *"i dont like the deviation
from the boxy head.. i know i said stray further from the original boxy design, but its somewhat
part of our game's identity"*. So the rule for the whole model: the big cube head, flat faces and
square corners are the game, and everything asked for goes ON the blocks, not instead of them.
Form is PAINTED onto the flat faces; "more unique shapes" are blocks ADDED to the silhouette. A
player should say "that is the same kid, with a lot more detail", not "that is another art style".

⚠️ A PROTOTYPE. NOTHING IN THE GAME LOADS IT. The folder is outside Resources, no roster row
points here, and team-dante.glb, build_bayan_voxel.py and person_dante.asset are not touched.

WHAT IS KEPT, so it could drop in later:
  * THE RIG, BYTE FOR BYTE. The .glb is team-dante.glb with its two meshes replaced: the seven
    bones (root, torso, head, arm-left, arm-right, leg-left, leg-right), their rest pose, the
    inverse bind matrices and every clip are copied across untouched, the way every cast builder
    does it (build_person_voxel.py, build_beggar_voxel.py). Every vertex is rigid to one bone.
  * THE ORIGINAL'S MEASURES, read off team-dante.glb: the head box 0.340 wide, 0.322 deep, from
    0.343 to 0.661; the torso from 0.176 to 0.343; legs to 0.176; arms straight out to 0.29, as
    the rig rests (`idle` drops them 45 degrees); the hair's top at 0.785; feet on zero, facing -y.
  * THE HAND where `CharacterVisual.PalmCentre` looks for it: the arm's long axis is still x, the
    far eighth of it is the hand, its top 0.058 above the bone, so a carried tsinelas
    (`HandTopLift` 0.0617) rests on it as it does today.
  * EVERYTHING HE ALREADY HAS, and nothing he does not: bronze skin, charcoal block hair with a
    fringe, the gold left eye and the scar through it, the shaved left temple, the obsidian horn
    (the original's `_add_horn_geometry`), ear studs, forest green piped in gold, the standing
    collar, frog fastenings, a jade buckle, coat-tails, leather-brown trousers, white shoes.

WHAT CHANGES:
  * EVERY BLOCK IS STILL A BLOCK, BUT A CUT ONE. A piece is a chamfered box that may taper, lean
    or end in a slant: the torso widens to the shoulders, limbs narrow to the wrist and ankle,
    hands and shoes are their own blocks, the hair is a slab with chunky wedges standing on it
    and hanging off it (CHARACTER_MODEL_METHOD.md section 2: "a core with a ring of separate
    chunky curls at graded heights reads as hair").
  * A SILHOUETTE OF HIS OWN, from blocks added: a stepped four-slab fringe, longest on his right,
    four crest chunks of different heights, a back mass ending in three points (or, in the second
    .glb, the swept pointed locks the owner asked to see on the same head), a collar that
    stands taller on the scar's side, swallowtail coat-tails cut open at the front, the left
    sleeve torn off, a wrapped left forearm, a rolled gold cuff on the right.
  * PAINT. One atlas of his own, hand drawn (see the textures script), sampled by `Toon.shader`
    in the half of UV space where it reads the texture and not the palette. The face is the
    flat front panel of the head box, with its depth drawn on.

HOW A FACE FINDS ITS PAINT. A piece is tagged with a group ("head", "torso", "armL" ...) and each
of its faces goes to the island of the side its normal points at, at the place it sits in model
space. Loose blocks (hair, horn, collar, piping) are tagged with a swatch instead.
"""
import math
import os
import struct
import sys

import bpy
import bmesh
from mathutils import Vector

TOOLS = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(TOOLS)
sys.path.insert(0, TOOLS)
import build_person_voxel as bpv  # noqa: E402  the cast's glb reader and writer, not edited
import author_character_redesign_dante_textures as tex  # noqa: E402  the island layout
import author_character_redesign_dante_clips as clips  # noqa: E402  the prototype's own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/team-dante.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/dante")
OUTS = {"blocks": os.path.join(FOLDER, "dante-redesign.glb"),
        "locks": os.path.join(FOLDER, "dante-redesign-spiky.glb")}
BLEND = os.path.join(ROOT, "ArtSource/dante/redesign-20261005/dante_redesign.blend")
NAME = "dante-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# ⚠️ TWO BONES THE CAST DOES NOT HAVE. Owner, 2026-10-05: *"should try bending the arms for
# animations, especially for the sprint and walk"*. One bone an arm cannot bend, so each arm gets
# an elbow: a child of the arm bone, appended AFTER the seven so their indices and names do not
# move. A clip that does not key them leaves the arm straight, exactly as before.
# ⚠️ WHAT THIS COSTS IN THE GAME: `CharacterVisual.PalmCentre` finds the hand from `arm-right`
# alone. With the elbow bent, the hand is no longer where that code parks a carried tsinelas.
# The hand anchor would have to hang off `forearm-right`. Not done; this is a prototype.
ELBOW_X = 0.172
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
# ⚠️⚠️ THE HEAD IS 0.84 OF THE ORIGINAL'S. Owner, 2026-10-05, after flipping between the two sizes
# in the game on the whole lineup: *"smaller heads are better"*, then *"yes rebuild the heads at
# 84 for all seven"*. Everything skinned to the head bone (the head block, ears, hair, horn) is
# built and painted at the original's size and then scaled about the head JOINT, so the paint
# keeps its place and the proportions inside the head do not change. The collar is drawn in with
# it. This DEPARTS from the cast's 24 / 23 / 53 proportions (Voxel_Person_Guide.md) on the owner's
# word: the head is now about 48 per cent of his height, and he stands 0.716 tall, not 0.785.
HEAD_SCALE = 0.84
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))   # the `head` bone of team-dante.glb, in Blender space
HEIGHT = HEAD_JOINT.z + (0.787 - HEAD_JOINT.z) * HEAD_SCALE   # the top of his hair after the scale
SHARP_ANGLE = 40.0       # degrees: a chamfer is a hard edge, a soft corner or a taper is one form
TRIANGLE_BUDGET = 6000   # the brief's ceiling; the original is 7,741

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
        flat = [(up, -vn), (up, vp), (-un, vp), (-un, -vn)]
    else:
        flat = [(up, -vn + c), (up, vp - c), (up - c, vp), (-un + c, vp),
                (-un, vp - c), (-un, -vn + c), (-un + c, -vn), (up - c, -vn)]
    return [Vector(centre) + u * a + v * b for a, b in flat]


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


def proj_facing(group, direction, name):
    """The group's islands for the faces turned toward `direction`, one flat tone for the rest."""
    return ("facing", group, direction, name)


def tones(top, side, under):
    """No drawing at all: one flat tone for faces turned up, one for the sides, one for under."""
    return ("tones", top, side, under)


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
        self.collar_verts = set()

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

        # round the ring a swatch is spread by LENGTH, so a narrow chamfer takes a narrow strip
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
        off the axis leans it. `move` is applied to every point (the feet's turn-out).
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

        ⚠️ The cast is rigid, one bone a vertex (Voxel_Person_Guide.md), and so is this model
        except where cloth or hair lies ACROSS two bones. Owner, 2026-10-05: *"make it so the
        clothes will bend/distort to follow his body"*. A coat-tail rigid to the torso is walked
        through by the legs; a collar rigid to the torso is turned through by the head.
        """
        for f in faces:
            for v in f.verts:
                self.blend[v] = weights(v.co)

    def wedge(self, name, bone, base, tip, base_half, tip_half, mapping, chamfer, across=X):
        """A block standing on `base` and narrowing to `tip`: a hair clump, a loose cloth end."""
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
            elif kind == "facing":
                spec = proj(spec[1]) if face.normal.dot(Vector(spec[2])) > 0.6 else flat(spec[3])
                kind = spec[0]
            elif kind == "tones":
                spec = flat(spec[1] if face.normal.z > 0.45 else (spec[3] if face.normal.z < -0.45 else spec[2]))
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


def bezier(p0, p1, p2, t):
    a = Vector(p0).lerp(Vector(p1), t)
    b = Vector(p1).lerp(Vector(p2), t)
    return a.lerp(b, t)


# ---------------------------------------------------------------------------
# THE HEAD. The original's box (0.340 wide, 0.322 deep, 0.343 to 0.661), CARVED. Owner,
# 2026-10-05, after the round skull was turned down and a plain cube was offered instead: *"not
# necessarily, just not entirely a cube"*. So it is a block whose corners are softened and whose
# sides are worked, and the box has to win: every ring is a superellipse of exponent 4 or more
# (2 is a circle, the old round skull sat near 3), and no ring is narrower than 0.8 of the box
# except the two that close it.
#
# THREE DEPTHS OF CARVING, so the owner can point at one (`--head`):
#   block   nearly the original cube: one chamfered box, flat faces, no nose
#   carved  the default: soft corners, a faintly bowed face, the jaw tucked in a little, a nose
#   shaped  as far as it goes while still a box: cheek fullness, a brow ledge, a tapered jaw
#     rows are (z, half width, depth to the front, depth to the back, exponent)
# ---------------------------------------------------------------------------
HEAD_HALF_X = 0.170
HEAD_Y = (-0.160, 0.162)
HEAD_Z = (0.343, 0.661)
HEAD_CHAMFER = 0.012
HEAD_CENTRE = Vector((0.0, 0.0, 0.50))
HEAD_N = 24
HEAD_ROWS = {
    "carved": [
        (0.343, 0.132, 0.130, 0.128, 4.0),
        (0.356, 0.158, 0.152, 0.152, 4.6),
        (0.386, 0.168, 0.160, 0.160, 5.0),
        (0.430, 0.172, 0.164, 0.162, 5.0),
        (0.520, 0.171, 0.163, 0.162, 5.0),
        (0.606, 0.170, 0.161, 0.162, 5.0),
        (0.646, 0.165, 0.157, 0.159, 4.6),
        (0.661, 0.148, 0.141, 0.144, 4.0),
    ],
    # ⚠️ THE FRONT OF THE FACE IS ONE FLAT PLANE: the same depth (0.165) from the jaw to the hair.
    # It had a brow ledge (the front stepping out 11 mm at 0.524) and a cheek that stood 7 mm
    # proud at 0.428. In the game's two-band toon shader each of those is a ring where the
    # light flips, so they drew STRAIGHT LINES across his face. Owner, 2026-10-05, from a
    # screenshot in Unity: *"the eyebrow dent is making this weird shading artifact where theres
    # a straight line on his head"*. The cheek fullness stays in the WIDTH, where it is silhouette.
    "shaped": [
        (0.343, 0.104, 0.116, 0.108, 3.4),
        (0.357, 0.140, 0.144, 0.140, 3.8),
        (0.388, 0.164, 0.162, 0.156, 4.0),
        (0.428, 0.181, 0.165, 0.162, 4.2),
        (0.470, 0.175, 0.165, 0.162, 4.2),
        (0.520, 0.171, 0.165, 0.162, 4.2),
        (0.604, 0.169, 0.165, 0.162, 4.2),
        (0.646, 0.162, 0.158, 0.158, 3.8),
        (0.661, 0.140, 0.136, 0.140, 3.4),
    ],
}
HEAD_VARIANT = "shaped"   # the owner picked `shaped` with hair A, 2026-10-05
NOSE = {"carved": 0.015, "shaped": 0.022}    # how far the foot of the nose stands off the face


def super_ring(z, rx, rf, rb, e, n=HEAD_N):
    out = []
    for j in range(n):
        t = 2.0 * math.pi * j / n
        c, s = math.cos(t), math.sin(t)
        a = math.copysign(abs(c) ** (2.0 / e), c)
        b = math.copysign(abs(s) ** (2.0 / e), s)
        out.append(Vector((a * rx, 0.001 + b * (rb if b >= 0 else rf), z)))
    return out


def build_head(part, variant):
    skin = proj("head")
    if variant == "block":
        cy = 0.5 * (HEAD_Y[0] + HEAD_Y[1])
        hy = 0.5 * (HEAD_Y[1] - HEAD_Y[0])
        part.block("head-box", "head", X, Y, [((0, cy, HEAD_Z[0]), HEAD_HALF_X, hy), ((0, cy, HEAD_Z[1]), HEAD_HALF_X, hy)],
                   skin, HEAD_CHAMFER)
    else:
        rows = HEAD_ROWS[variant]
        part.loft("head-box", "head", [super_ring(*row) for row in rows], skin)
        # ⚠️ NO NOSE. A wedge stood here, with its own ink line. Owner, 2026-10-05: the faces "look
        # too human, like it lost its charm.. they need to be more cutesy". The cast has no nose,
        # and a face with two big eyes and a mouth on a flat front is what makes it a toy.
    # ears: small blocks, as the original's are, narrowing a little outward
    for s in (1, -1):
        part.block("ear", "head", Y, Z, [((s * 0.164, 0.012, 0.456), 0.030, 0.042), ((s * 0.216, 0.016, 0.456), 0.024, 0.034)],
                   skin, 0.010)


# ---------------------------------------------------------------------------
# THE HAIR. Block hair, the original's kind, cut into clumps: a slab on the crown, a mass down
# the back, a panel over his right ear, then wedges standing on it and hanging off it. Every
# wedge is set by hand: (name, base, tip, base half size, tip half size).
# His left temple is shaved, so the slab stops short of that edge and the horn has room.
# ---------------------------------------------------------------------------
HAIR_CHAMFER = 0.013
WEDGES = [
    # the fringe: four slabs side by side over the forehead, cut to four lengths like the
    # original's stepped block fringe, longest on his right, the gold eye left clear
    # ⚠️ LONGER, AND WIDER AT THE FOOT. Owner, 2026-10-05, seeing him in the game: *"dante's
    # forehead looks too big and his head looks like a rectangle. should probably extend the
    # bangs"*. With the nose and the face shading gone, the bare skin between a short fringe and
    # the eyes was a tall blank panel. The slabs now come down to just over the eyes (their tops
    # are at 0.502 and 0.512) and a fifth slab covers the corner by the shaved temple.
    ("fringe-long",  (-0.128, -0.182, 0.672), (-0.138, -0.188, 0.506), (0.046, 0.026), (0.040, 0.018)),
    ("fringe-mid",   (-0.044, -0.184, 0.674), (-0.052, -0.190, 0.522), (0.040, 0.027), (0.036, 0.018)),
    ("fringe-notch", (0.030, -0.184, 0.676),  (0.026, -0.189, 0.552),  (0.036, 0.026), (0.033, 0.018)),
    ("fringe-short", (0.104, -0.182, 0.674),  (0.102, -0.187, 0.534),  (0.040, 0.024), (0.036, 0.017)),
    ("sideburn",     (-0.187, -0.104, 0.650), (-0.185, -0.118, 0.456), (0.021, 0.052), (0.015, 0.026)),
    # the crest: four chunks thrown up and back toward his right, no two the same height. They
    # stand on a slab kept LOW (0.738), so from above the crown is four stepped masses and not
    # one flat square with ridges on it
    ("crest-quiff",  (-0.044, -0.104, 0.706), (-0.068, -0.140, 0.760), (0.094, 0.070), (0.050, 0.036)),
    ("crest-high",   (0.006, 0.006, 0.706),   (-0.016, 0.026, 0.766),  (0.100, 0.080), (0.050, 0.042)),
    ("crest-back",   (-0.092, 0.106, 0.706),  (-0.122, 0.134, 0.764),  (0.080, 0.068), (0.042, 0.034)),
    ("crest-left",   (0.076, 0.078, 0.706),   (0.088, 0.098, 0.753),   (0.060, 0.060), (0.032, 0.032)),
    # the back mass ends in three blunt points, each its own length
    # ⚠️ their inner faces stay behind y 0.192: the collar's back wall and its piping reach 0.188
    ("nape-right",   (-0.126, 0.214, 0.420),  (-0.148, 0.224, 0.340),  (0.056, 0.022), (0.032, 0.015)),
    ("nape-mid",     (-0.010, 0.215, 0.420),  (-0.020, 0.228, 0.322),  (0.060, 0.022), (0.034, 0.015)),
    ("nape-left",    (0.106, 0.214, 0.420),   (0.118, 0.222, 0.358),   (0.050, 0.022), (0.030, 0.014)),
]
#   the layer over the back: (name, top, foot, half size at the top, half size at the foot)
BACK_SLABS = [
    ("back-right", (-0.122, 0.213, 0.676), (-0.130, 0.217, 0.522), (0.060, 0.017), (0.046, 0.013)),
    ("back-mid",   (-0.012, 0.215, 0.680), (-0.020, 0.220, 0.474), (0.056, 0.018), (0.044, 0.013)),
    ("back-left",  (0.098, 0.213, 0.676),  (0.102, 0.216, 0.552),  (0.054, 0.017), (0.042, 0.013)),
]


def build_hair(part):
    # Owner, 2026-10-05, on painted highlight streaks: "hair shimmer idk if i like it". So the hair
    # carries no drawing: each clump is a lighter top plane, the flat tone round its sides and a
    # darker underside, and the ink edge and the toon ramp do the rest, as on the original.
    hair = tones("hair_top", "hair", "hair_under")
    # the mass underneath, down the back: one step deeper, so the layer laid over it reads as
    # hair lying on hair (CAST_CLOTHING_STYLE.md rule 7: the dark tone underneath and behind)
    deep = tones("hair", "hair_under", "hair_under")
    part.block("hair-slab", "head", X, Y, [((0, 0, 0.646), (0.198, 0.150), 0.192), ((0, 0, 0.738), (0.190, 0.142), 0.182)],
               hair, HAIR_CHAMFER)
    part.block("hair-back", "head", X, Y, [((0, 0.178, 0.408), (0.190, 0.166), 0.034), ((0, 0.172, 0.664), (0.190, 0.160), 0.038)],
               deep, HAIR_CHAMFER)
    # the undercut at the nape: a thin dark layer on the back of the head, inside the collar,
    # so what shows over the collar's rim between the nape points is hair and not skin
    part.block("hair-nape-liner", "head", X, Y, [((0, 0.160, 0.366), 0.150, 0.010), ((0, 0.162, 0.412), 0.156, 0.011)],
               flat("hair_under"), 0.004)
    part.block("hair-side", "head", X, Y, [((-0.187, 0.045, 0.506), 0.020, 0.104), ((-0.187, 0.045, 0.662), 0.022, 0.110)],
               hair, 0.010)
    for name, base, tip, base_half, tip_half in WEDGES:
        # the fringe is cut square, the other clumps a little softer
        faces = part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, deep if name.startswith("nape") else hair,
                           0.006 if name.startswith("fringe") else 0.010)
        if name.startswith("nape"):
            # the points lie on his back: their tips stay with the shoulders when the head tips
            # back, instead of digging into the coat
            part.bend(faces, lambda co: [("head", 1.0 - 0.6 * _ramp(0.420, 0.335, co.z)), ("torso", 0.6 * _ramp(0.420, 0.335, co.z))])
    # ⚠️ THE BACK WAS ONE FLAT BLACK SLAB (handoff section 7, and the owner circled "the whole back
    # of the hair"). Paint is not the answer: the owner turned down drawn shine. So the back gets
    # what the fringe has, blocks: three slabs hanging from the crown over the back mass, each its
    # own width and length, the longest in the middle-right, standing 15 mm proud so each throws
    # an ink edge and a shadow on the mass under it. Set by hand, like the fringe.
    for name, base, tip, base_half, tip_half in BACK_SLABS:
        part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, hair, 0.008)


# ---------------------------------------------------------------------------
# THE OTHER HAIR: LOCKS. Owner, 2026-10-05, pointing at the first pass's swept, pointed hair:
# *"can we have one version with this non-blocky hair model?"* So the same carved block head can
# wear it instead: a thin cap that follows the box (its top, its back, the side over his right
# ear), and sixteen tapered six-sided locks rooted on that cap, each set by hand:
#   (name, root, bend, tip, width): a lock leaves the cap at `root`, leans through `bend`, ends at `tip`
# The fringe hangs in FRONT of the flat face (y -0.16), the shaved left temple and the horn's root
# are left bare, the lock over his right ear hangs behind the ear block and not through it.
# Its shading is the first pass's and nothing more: one lighter plane down the outward face of
# each lock, a darker underside, the ink edge. No drawn shine.
# ---------------------------------------------------------------------------
LOCKS = [
    # the fringe, swept across to HIS right so the gold eye and the scar stay clear
    ("fringe-long",  (-0.040, -0.150, 0.690), (-0.100, -0.216, 0.640), (-0.142, -0.188, 0.520), 0.076),
    ("fringe-mid",   (0.030, -0.150, 0.692),  (-0.010, -0.220, 0.650), (-0.055, -0.192, 0.556), 0.066),
    ("fringe-short", (0.096, -0.146, 0.692),  (0.076, -0.214, 0.666),  (0.040, -0.190, 0.598), 0.056),
    ("sideburn",     (-0.150, -0.120, 0.682), (-0.208, -0.168, 0.600), (-0.194, -0.130, 0.470), 0.062),
    # the crown: spikes thrown up and back, no two the same height
    ("crest-front",  (0.000, -0.080, 0.700),  (-0.010, -0.160, 0.766), (-0.070, -0.190, 0.748), 0.082),
    ("crest-right",  (-0.090, -0.030, 0.700), (-0.165, -0.060, 0.770), (-0.226, -0.020, 0.750), 0.086),
    ("crest-high",   (0.030, 0.010, 0.702),   (0.022, 0.020, 0.796),   (-0.034, 0.076, 0.776), 0.092),
    ("crest-left",   (0.100, 0.040, 0.700),   (0.140, 0.070, 0.768),   (0.118, 0.150, 0.758), 0.076),
    ("crest-back-r", (-0.080, 0.100, 0.700),  (-0.160, 0.160, 0.748),  (-0.226, 0.200, 0.704), 0.086),
    ("crest-back",   (0.020, 0.120, 0.700),   (0.030, 0.216, 0.738),   (0.000, 0.268, 0.700), 0.090),
    # the back mass, hanging to the nape, tips kicked out
    ("nape-right",   (-0.120, 0.160, 0.610),  (-0.190, 0.226, 0.520),  (-0.212, 0.198, 0.400), 0.090),
    ("nape-mid-r",   (-0.040, 0.172, 0.600),  (-0.058, 0.250, 0.500),  (-0.078, 0.228, 0.372), 0.094),
    ("nape-mid-l",   (0.055, 0.172, 0.600),   (0.080, 0.246, 0.508),   (0.064, 0.226, 0.388), 0.088),
    ("nape-left",    (0.126, 0.160, 0.610),   (0.186, 0.204, 0.540),   (0.190, 0.180, 0.440), 0.072),
    ("behind-ear",   (-0.176, 0.070, 0.660),  (-0.238, 0.090, 0.570),  (-0.226, 0.098, 0.452), 0.076),
    ("temple-right", (-0.176, -0.040, 0.664), (-0.226, -0.060, 0.600), (-0.212, -0.050, 0.520), 0.060),
]
LOCK_PROFILE = [(0.00, 0.80), (0.15, 1.00), (0.38, 0.90), (0.60, 0.66), (0.80, 0.36)]
LOCK_THIN = 0.45


def build_hair_locks(part):
    cap = tones("hair_top", "hair", "hair_under")
    part.block("hair-cap", "head", X, Y, [((0, 0.002, 0.628), (0.184, 0.150), 0.176), ((0, 0.002, 0.704), (0.170, 0.136), 0.160)],
               cap, 0.020)
    part.block("hair-cap-back", "head", X, Y, [((0, 0.170, 0.400), (0.184, 0.160), 0.018), ((0, 0.170, 0.660), (0.184, 0.156), 0.020)],
               cap, 0.010)
    part.block("hair-cap-side", "head", X, Y, [((-0.178, 0.030, 0.512), 0.014, 0.130), ((-0.178, 0.030, 0.660), 0.016, 0.134)],
               cap, 0.008)

    def lock_paint(i, j):
        # six facets round a lock: 3 is the one turned out from the head and up, 0 and 1 face in
        return flat("hair_top") if j == 3 else (flat("hair_under") if j in (0, 1) else flat("hair"))

    for name, root, bend, tip, width in LOCKS:
        rings, params = [], []
        for t, k in LOCK_PROFILE:
            p = bezier(root, bend, tip, t)
            tangent = (bezier(root, bend, tip, min(t + 0.02, 1.0)) - bezier(root, bend, tip, max(t - 0.02, 0.0))).normalized()
            u = tangent.cross(p - HEAD_CENTRE)
            if u.length < 1e-6:
                u = tangent.cross(X)
            u.normalize()
            v = tangent.cross(u).normalized()
            r = 0.5 * width * k
            rings.append([p + u * (math.cos(a) * r) + v * (math.sin(a) * r * LOCK_THIN)
                          for a in (2.0 * math.pi * (j + 0.5) / 6 for j in range(6))])
            params.append(t)
        part.loft("lock-" + name, "head", rings, lock_paint, caps=(False, False), tip=Vector(tip), params=params)


def build_horn(part):
    # The original's swept horn (build_bayan_voxel.py `_add_horn_geometry`): five flat planes round
    # it, stepped, same root on the shaved temple and the same tip.
    p0, p1, p2 = (0.166, -0.016, 0.574), (0.356, -0.030, 0.598), (0.276, 0.240, 0.765)
    rings, params = [], []
    for t, r in ((0.0, 0.060), (0.24, 0.055), (0.48, 0.045), (0.70, 0.032), (0.86, 0.019)):
        p = bezier(p0, p1, p2, t)
        tangent = (bezier(p0, p1, p2, min(t + 0.02, 1.0)) - bezier(p0, p1, p2, max(t - 0.02, 0.0))).normalized()
        u = tangent.cross(p - HEAD_CENTRE).normalized()
        v = tangent.cross(u).normalized()
        rings.append([p + u * (math.cos(a) * r) + v * (math.sin(a) * r * 0.82)
                      for a in (2.0 * math.pi * (j + 0.5) / 5 for j in range(5))])
        params.append(t)
    part.loft("horn", "head", rings, swatch("horn"), caps=(False, False), tip=Vector(p2), params=params)


# ---------------------------------------------------------------------------
# THE TORSO AND THE JACKET. One block, wider at the shoulders than at the belt.
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.343)
TORSO_LOW = (0.118, 0.092)     # half width, half depth at the hips
TORSO_HIGH = (0.138, 0.100)    # at the shoulders
TORSO_CY = 0.004


def torso_half(z):
    t = (z - TORSO_Z[0]) / (TORSO_Z[1] - TORSO_Z[0])
    return (TORSO_LOW[0] + (TORSO_HIGH[0] - TORSO_LOW[0]) * t, TORSO_LOW[1] + (TORSO_HIGH[1] - TORSO_LOW[1]) * t)


def chest_y(z):
    return TORSO_CY - torso_half(z)[1]


TAIL_TOP = 0.226
SKIRT_FOLLOW = 0.78   # how much of a leg's swing the hem takes
#   the collar round the neck, from his right tip (s = 0) behind him to his left tip (s = 1):
#   (s, top height). ⚠️ THE SAME ON BOTH SIDES. It used to stand taller on the scar's side and
#   fold low on the other, round a ring wider than the shoulders. Owner, 2026-10-05, circling
#   both ends of it: *"fix the asymmetrical and detached hood"*. Its top also stays under the
#   ear blocks (their foot is at 0.414).
#   Behind, it stops at 0.404, UNDER the foot of the hair's back mass (0.408), and the nape
#   points hang wholly outside its wall. A collar raised into the hair (tried, to hide the skin
#   that showed over the rim) was cut through by the hair as soon as the head turned 20 degrees:
#   the hair rides the head bone, the collar the torso. The skin is hidden by `hair-nape-liner`.
COLLAR_TOP = [(0.00, 0.372), (0.06, 0.398), (0.14, 0.408), (0.26, 0.405), (0.50, 0.404),
              (0.74, 0.405), (0.86, 0.408), (0.94, 0.398), (1.00, 0.372)]
COLLAR_BASE = 0.338   # 5 mm INTO the torso's top: it grows out of the shoulders
COLLAR_SHELF = 0.358
COLLAR_WALL = 0.008
COLLAR_FOLLOW = 0.92
#   (height, half width, depth to the front, depth to the back) of its outer face. It leaves the
#   shoulders (the torso is 0.138 by 0.100 there), spreads out under the jaw as a shelf, then
#   stands as a wall that follows the head's own taper 8 to 12 mm off it, so nothing floats.
COLLAR_PROFILE = [(0.338, 0.146, 0.108, 0.110), (0.358, 0.162, 0.162, 0.166), (0.420, 0.200, 0.188, 0.190)]
COLLAR_GAP = 40.0   # degrees each side of straight ahead left open


def build_torso(part):
    cloth = proj("torso")
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, TORSO_Z[0]), *TORSO_LOW), ((0, TORSO_CY, TORSO_Z[1]), *TORSO_HIGH)],
               cloth, 0.014)

    # the belt: 7 mm proud, so it throws its own ink line
    lo, hi = tex.BELT
    (wl, dl), (wh, dh) = torso_half(lo), torso_half(hi)
    part.block("belt", "torso", X, Y, [((0, TORSO_CY, lo), wl + 0.007, dl + 0.007), ((0, TORSO_CY, hi), wh + 0.007, dh + 0.007)],
               proj_except("torso", 2, "belt_tone"), 0.005)

    # the buckle: a gold plate standing off the belt, a jade stone standing off the plate
    y0 = chest_y(0.219) - 0.005
    ahead = (0, -1, 0)
    part.block("buckle", "torso", X, Z, [((0, y0, 0.219), 0.038, 0.030), ((0, y0 - 0.014, 0.219), 0.038, 0.030)],
               proj_facing("torso", ahead, "gold_tone"), 0.006)
    part.block("buckle-stone", "torso", X, Z, [((0, y0 - 0.012, 0.219), 0.022, 0.018), ((0, y0 - 0.023, 0.219), 0.017, 0.013)],
               proj_facing("torso", ahead, "jade"), 0.005)

    # THE COAT'S SKIRT. One piece round the hips, open only at the front, its hem lower behind
    # than in front, the hem a band of gold standing 4 mm proud (CAST_CLOTHING_STYLE.md rules 3
    # and 5). ⚠️ It was two separate tails split up the back. Owner, 2026-10-05, marking that
    # split: *"the back of his cloak is supposed to be connected, like a coat tail"*.
    # ⚠️ AND IT BENDS. Rigid to the torso, the legs walked straight through it. Its top ring is
    # the torso's; toward the hem each side takes up to SKIRT_FOLLOW of its own leg, and across
    # the middle the two legs are mixed, so the back panel twists between them as he strides.
    gold = swatch("gold", (0.0, 8.0), (0.05, 0.95), "columns")
    w, d = torso_half(TAIL_TOP)
    top_r = (w + 0.004, d + 0.004, d + 0.006)       # half width, depth to the front, to the back
    # ⚠️ the outer wall stays inside 0.160: `idle` drops the arms 45 degrees and a wider skirt
    # runs into the sleeve and the wrist, where two ink edges then fight
    hem_r = (0.158, 0.134, 0.152)
    sections, params = [], []
    angles = (1.2, 8, 24, 45, 66, 90, 114, 135, 156, 172, 180, 188, 204, 225, 246, 270, 294, 315, 336, 352, 358.8)
    for theta in angles:
        a = math.radians(theta)
        px = -math.copysign(abs(math.sin(a)) ** 0.4, math.sin(a))     # his right first, then behind
        py = -math.copysign(abs(math.cos(a)) ** 0.4, math.cos(a))     # -1 in front, +1 behind
        hem = 0.117 - 0.017 * py                                      # 0.134 in front, 0.100 behind

        def at(z, f, inset):
            r = [top_r[k] + (hem_r[k] - top_r[k]) * f - inset for k in range(3)]
            return Vector((px * r[0], TORSO_CY + py * (r[2] if py >= 0 else r[1]), z))

        mid = 0.5 * (TAIL_TOP + hem + 0.020)
        sections.append([at(TAIL_TOP, 0.0, 0.0), at(mid, 0.58, 0.0), at(hem + 0.020, 0.985, 0.0), at(hem + 0.0205, 1.0, -0.002),
                         at(hem, 1.0, -0.004), at(hem + 0.004, 1.0, 0.009), at(mid, 0.58, 0.010), at(TAIL_TOP, 0.0, 0.010)])
        params.append(theta / 360.0)
    skirt = part.loft("coat-skirt", "torso", sections,
                      lambda i, j: (cloth, cloth, gold, gold, gold, flat("lining"), flat("lining"), flat("lining"))[j], params=params)

    def skirt_weights(co):
        down = _ramp(TAIL_TOP, 0.105, co.z) ** 0.8
        leg = SKIRT_FOLLOW * down
        left = _ramp(-0.070, 0.070, co.x)
        left = left * left * (3.0 - 2.0 * left)
        return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]

    part.bend(skirt, skirt_weights)

    # the lapels' gold edge, as geometry, down the chest
    for s in (1, -1):
        part.block("lapel-edge", "torso", X, Y,
                   [((s * 0.029, chest_y(0.236) - 0.002, 0.236), 0.0085, 0.006), ((s * 0.056, chest_y(0.338) - 0.002, 0.338), 0.0085, 0.006)],
                   swatch("gold", (0.0, 1.0), (0.05, 0.95)), 0.0)

    # two frog fastenings across the opening, each a gold bar with a jade knot
    for z, half in ((0.262, 0.046), (0.296, 0.055)):
        y = chest_y(z) - 0.008
        part.block("frog-bar", "torso", Y, Z, [((-half, y, z), 0.006, 0.007), ((half, y, z), 0.006, 0.007)],
                   swatch("gold", (0.0, 1.0), (0.1, 0.9)), 0.0)
        part.block("frog-knot", "torso", X, Z, [((0, y - 0.002, z), 0.013, 0.013), ((0, y - 0.015, z), 0.013, 0.013)],
                   flat("jade"), 0.005)

    # the standing collar: flat panels that grow out of the shoulders and stand round the jaw,
    # piped along the top (the original's collar blocks sit in the same place)
    def profile(height):
        rows = COLLAR_PROFILE
        for (z0, *lo), (z1, *hi) in zip(rows, rows[1:]):
            if height <= z1 or z1 == rows[-1][0]:
                f = (height - z0) / (z1 - z0)
                return [lo[k] + (hi[k] - lo[k]) * f for k in range(3)]

    sections, params = [], []
    for s, top in COLLAR_TOP:
        angle = math.radians((270.0 - COLLAR_GAP) - s * (360.0 - 2.0 * COLLAR_GAP))
        c, sn = math.cos(angle), math.sin(angle)
        # a squared path, so the collar turns four corners as the head does
        px = math.copysign(abs(c) ** (2.0 / 5.0), c)
        py = math.copysign(abs(sn) ** (2.0 / 5.0), sn)

        def at(height, inset):
            rx, rf, rb = profile(height)
            return Vector((px * (rx - inset), 0.001 + py * ((rb if sn >= 0 else rf) - inset), height))

        pipe = min(0.014, max(0.004, (top - COLLAR_SHELF) * 0.5))
        w = COLLAR_WALL
        sections.append([at(COLLAR_BASE, 0.0), at(COLLAR_SHELF, 0.0), at(top - pipe, 0.0), at(top - pipe, -0.004),
                         at(top, -0.004), at(top, w), at(COLLAR_SHELF, w), at(COLLAR_BASE, w)])
        params.append(s)

    def collar_paint(i, j):
        return (swatch("collar_out", (0.0, 1.0), (0.0, 0.86), "columns"),
                swatch("collar_out", (0.0, 1.0), (0.0, 0.86), "columns"),
                swatch("gold", (0.0, 6.0), (0.05, 0.3), "columns"),
                swatch("gold", (0.0, 6.0), (0.3, 0.95), "columns"),
                swatch("gold", (0.0, 6.0), (0.95, 0.6), "columns"),
                swatch("collar_in", (0.0, 1.0), (1.0, 0.0), "columns"),
                flat("lining"),
                flat("lining"))[j]

    collar = part.loft("collar", "torso", sections, collar_paint, params=params)
    part.collar_verts = {v for f in collar for v in f.verts}
    # it grows out of the shoulders and its rim goes with the head: half of every turn and nod,
    # so the jaw and the hair at the nape do not turn through a wall that stood still
    # ⚠️ FROM THE SHELF UP IT IS THE HEAD'S. At 0.55 of the head, rising toward the rim, the jaw
    # still turned through the wall (owner: "the head still clips in the looking around"). A wall
    # that turns WITH the box it surrounds cannot be turned through; all of the twist is taken by
    # the 20 mm between the shoulders and the shelf, under the jaw.
    part.bend(collar, lambda co: [("torso", 1.0 - COLLAR_FOLLOW * _ramp(COLLAR_BASE + 0.002, COLLAR_SHELF, co.z)),
                                  ("head", COLLAR_FOLLOW * _ramp(COLLAR_BASE + 0.002, COLLAR_SHELF, co.z))])


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests. `s` is +1 for his left, -1 for his right.
# ---------------------------------------------------------------------------
ARM_Y, ARM_Z = 0.0173, 0.288


def arm_block(part, name, bone, s, x0, x1, half0, half1, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x0, ARM_Y, ARM_Z), half0[0], half0[1]), ((s * x1, ARM_Y, ARM_Z), half1[0], half1[1])],
                      mapping, chamfer)


def build_hand(part, s, bone, group):
    skin = proj(group)
    arm_block(part, "hand", bone, s, 0.222, 0.292, (0.050, 0.058), (0.048, 0.056), skin, 0.013)
    # No thumb block. One was tried and its ink edge read as a ring drawn on the fist; the cast's
    # hands are plain blocks, and this one's fingers and thumb are drawn (see the textures script).


def build_arm_left(part):
    bone, fore = "arm-left", "forearm-left"
    skin = proj_except("armL", 0, "skin_tone")
    # two rigid blocks that overlap at the elbow, as a toy's joint does; the cut faces are one tone
    arm_block(part, "arm", bone, 1, 0.100, ELBOW_X + 0.012, (0.052, 0.057), (0.049, 0.053), skin, 0.011)
    arm_block(part, "forearm", fore, 1, ELBOW_X - 0.014, 0.226, (0.048, 0.052), (0.044, 0.047), skin, 0.011)
    # what is left of the sleeve: a short block on the shoulder whose far edge is torn in teeth
    start = rect_ring((0.100, ARM_Y, ARM_Z), Y, Z, 0.048, 0.054, 0.006)
    full = rect_ring((0.106, ARM_Y, ARM_Z), Y, Z, 0.059, 0.065, 0.013)
    jag = (0.158, 0.144, 0.160, 0.146, 0.157, 0.143, 0.159, 0.145)
    edge = [Vector((jag[j], p.y, p.z)) for j, p in enumerate(rect_ring((0, ARM_Y, ARM_Z), Y, Z, 0.059, 0.065, 0.013))]
    under = [Vector((jag[j] - 0.004, p.y, p.z)) for j, p in enumerate(rect_ring((0, ARM_Y, ARM_Z), Y, Z, 0.050, 0.055, 0.011))]
    # it wears the jacket's paint, not the arm's, so the bare arm under its teeth stays skin
    part.loft("sleeve-stub", bone, [start, full, edge, under], lambda i, j: proj("torso") if i < 2 else flat("lining"),
              caps=(True, False))
    # the wrap, 5 mm proud of the forearm. (It had a loose end hanging by the hand; at the coat
    # hem it read as a stray pale wedge and was cut.)
    arm_block(part, "wrap", fore, 1, 0.176, 0.224, (0.0530, 0.0570), (0.0495, 0.0520), proj_except("armL", 0, "wrap_tone"), 0.005)
    build_hand(part, 1, fore, "armL")


def build_arm_right(part):
    bone, fore = "arm-right", "forearm-right"
    skin = proj_except("armR", 0, "skin_tone")
    arm_block(part, "sleeve", bone, -1, 0.100, ELBOW_X + 0.006, (0.058, 0.064), (0.056, 0.061), proj_except("armR", 0, "lining"), 0.013)
    # the roll: a fat gold block round the arm
    arm_block(part, "cuff", fore, -1, ELBOW_X - 0.004, 0.204, (0.066, 0.071), (0.066, 0.071), proj_except("armR", 0, "gold_tone"), 0.008)
    arm_block(part, "forearm", fore, -1, ELBOW_X - 0.010, 0.226, (0.047, 0.050), (0.044, 0.047), skin, 0.008)
    build_hand(part, -1, fore, "armR")


# ---------------------------------------------------------------------------
# THE LEGS. Short, the cast's 24 per cent. Feet turned out seven degrees.
# ---------------------------------------------------------------------------
LEG_X = 0.086
TOE_OUT = 7.0


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    cloth = proj(group)
    lo, hi = tex.CUFF
    # ⚠️ the trouser narrows and leans IN toward the hip: a leg as wide at the top as at the cuff
    # came out through the coat-tail's wall beside the wrist
    part.block("trouser", bone, X, Y, [((s * LEG_X, 0.0, hi - 0.008), 0.064, 0.072), ((s * (LEG_X - 0.012), 0.0, 0.180), 0.056, 0.072)],
               proj_except(group, 2, "belt_tone"), 0.009)
    part.block("cuff", bone, X, Y, [((s * LEG_X, 0.0, lo), 0.073, 0.081), ((s * LEG_X, 0.0, hi), 0.075, 0.083)],
               proj_except(group, 2, "cuff_tone"), 0.006)

    turn = math.radians(TOE_OUT) * s
    cs, sn = math.cos(turn), math.sin(turn)

    def turned(p):
        x, y = p.x - s * LEG_X, p.y - 0.02
        return Vector((s * LEG_X + x * cs + y * sn, 0.02 - x * sn + y * cs, p.z))

    # the shoe: a block that is low at the toe and rises to the ankle, on a wider sole
    def station(y, half, z0, z1, cut):
        return [turned(p) for p in rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)]

    upper = [(-0.137, 0.042, 0.017, 0.030, 0.004), (-0.127, 0.056, 0.014, 0.040, 0.010), (-0.084, 0.068, 0.014, 0.056, 0.012),
             (-0.020, 0.066, 0.014, 0.066, 0.012), (0.067, 0.062, 0.014, 0.064, 0.012), (0.078, 0.052, 0.017, 0.054, 0.005)]
    part.loft("shoe", bone, [station(*row) for row in upper], cloth)
    part.block("sole", bone, X, Z, [((s * LEG_X, -0.142, 0.0095), 0.060, 0.0095), ((s * LEG_X, -0.086, 0.0095), 0.075, 0.0095),
                                   ((s * LEG_X, 0.000, 0.0095), 0.073, 0.0095), ((s * LEG_X, 0.084, 0.0095), 0.066, 0.0095)],
               cloth, 0.005, move=turned)


# ---------------------------------------------------------------------------
# THE BUILD
# ---------------------------------------------------------------------------

def mesh_arrays(obj):
    """A mesh as glTF arrays: +y up, -z the way Blender's -y faces, v flipped, one bone a vertex."""
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
    """team-dante.glb with its meshes swapped: skeleton, bind matrices and clips copied untouched."""
    gltf, buffer = bpv.read_glb(BASE)
    names = [gltf["nodes"][i]["name"] for i in gltf["skins"][0]["joints"]]
    if names != BONES and names != ALL_BONES:
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
    if names == BONES:
        node_of = {n.get("name"): i for i, n in enumerate(gltf["nodes"])}
        skin = gltf["skins"][0]
        raw = struct.unpack("<%df" % (16 * len(BONES)), accessor_raw)
        matrices = [raw[k * 16:(k + 1) * 16] for k in range(len(BONES))]
        for name, parent, side in EXTRA_BONES:
            world = (side * ELBOW_X, ARM_Z, -ARM_Y)
            pw = [-matrices[BONES.index(parent)][12 + a] for a in range(3)]
            gltf["nodes"].append({"name": name, "translation": [world[a] - pw[a] for a in range(3)]})
            gltf["nodes"][node_of[parent]].setdefault("children", []).append(len(gltf["nodes"]) - 1)
            skin["joints"].append(len(gltf["nodes"]) - 1)
            matrices.append((1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -world[0], -world[1], -world[2], 1))
        skin["inverseBindMatrices"] = add([tuple(float(x) for x in m) for m in matrices], "f", "MAT4", 5126)

    # the prototype's own locomotion clips, in place of the ones copied across (the clips script)
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (dante)"}
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
    print("triangles %d  (budget %d, the original is 7741)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if abs(hi.z - HEIGHT) > 0.004:
        raise SystemExit("the top is at %.4f, the original's is %.3f" % (hi.z, HEIGHT))
    if tris > TRIANGLE_BUDGET:
        raise SystemExit("%d triangles is over the budget of %d" % (tris, TRIANGLE_BUDGET))
    # CharacterVisual.PalmCentre: the arm must run along x, and the hand's top must sit where a
    # carried tsinelas is parked (bone-space +0.0555, with HandTopLift 0.0617 over the palm).
    body = objects[0]
    group = (body.vertex_groups["arm-right"].index, body.vertex_groups["forearm-right"].index)
    arm = [v.co for v in body.data.vertices if v.groups[0].group in group]
    size = [max(p[a] for p in arm) - min(p[a] for p in arm) for a in range(3)]
    if not (size[0] > size[1] and size[0] > size[2]):
        raise SystemExit("arm-right no longer runs along x: %s" % size)
    far = min(p.x for p in arm)
    hand = [p for p in arm if p.x < far + size[0] / 8.0]
    top = max(p.z for p in hand) - ARM_Z
    print("hand top %.4f above the arm bone (the original's is 0.0555 to 0.060)" % top)
    if not 0.045 <= top <= 0.066:
        raise SystemExit("the hand's top moved; a carried tsinelas would float or sink")


def build(armature, material, head_variant, hair, suffix=""):
    """One whole model: the shared body, the head carved as asked, wearing one of the two hairs."""
    body, head = Part("body-mesh" + suffix), Part("head-mesh" + suffix)
    build_torso(body)
    build_arm_left(body)
    build_arm_right(body)
    build_leg(body, 1)
    build_leg(body, -1)
    build_head(head, head_variant)
    if hair == "locks":
        build_hair_locks(head)
    else:
        build_hair(head)
    build_horn(head)
    objects = []
    for part in (body, head):
        part.resolve_uvs()
        # AFTER the paint is placed (it is projected from the full-size shape): the head comes in.
        part.bm.verts.ensure_lookup_table()
        collar_low, collar_high = COLLAR_BASE, COLLAR_SHELF
        for v in part.bm.verts:
            if part is head:
                k = HEAD_SCALE
            elif v in body.collar_verts:
                # the collar grows out of the shoulders at full size and closes on the smaller head
                f = max(0.0, min(1.0, (v.co.z - collar_low) / (collar_high - collar_low)))
                k = 1.0 + (HEAD_SCALE - 1.0) * f
            else:
                continue
            v.co = HEAD_JOINT + (v.co - HEAD_JOINT) * k
        obj = part.to_object(armature)
        obj.data.materials.append(material)
        objects.append(obj)
        print(part.name, " ".join("%s:%d" % (n, t) for n, t in part.pieces))
    verify(objects)
    return objects


def main():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    variant = args[args.index("--head") + 1] if "--head" in args else HEAD_VARIANT
    # `--out` writes ONE variant for a side by side look and leaves the .blend alone. It must sit
    # beside the atlas, which the .glb names by a relative path.
    out = args[args.index("--out") + 1] if "--out" in args else None
    hair = args[args.index("--hair") + 1] if "--hair" in args else "blocks"
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

    image_path = os.path.join(FOLDER, tex.ATLAS_NAME)
    if not os.path.exists(image_path):
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_dante_textures.py")
    image = bpy.data.images.load(image_path)
    material = bpy.data.materials.new(NAME)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    node = nodes.new("ShaderNodeTexImage")
    node.image = image
    material.node_tree.links.new(node.outputs[0], nodes["Principled BSDF"].inputs["Base Color"])
    nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0

    if out is not None:
        objects = build(armature, material, variant, hair)
        write_glb(mesh_arrays(objects[0]), mesh_arrays(objects[1]), out)
        return

    # Both hairs, one .glb each. The .blend keeps both; the lock-haired pair is hidden.
    for style, suffix in (("blocks", ""), ("locks", "-spiky")):
        objects = build(armature, material, variant, style, suffix)
        write_glb(mesh_arrays(objects[0]), mesh_arrays(objects[1]), OUTS[style])
        if suffix:
            for obj in objects:
                obj.hide_viewport = obj.hide_render = True
    os.makedirs(os.path.dirname(BLEND), exist_ok=True)
    image.filepath = bpy.path.relpath(image_path, start=os.path.dirname(BLEND))
    bpy.context.preferences.filepaths.save_version = 0   # no .blend1 beside it
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)
    print("wrote " + BLEND)


if __name__ == "__main__":
    main()
