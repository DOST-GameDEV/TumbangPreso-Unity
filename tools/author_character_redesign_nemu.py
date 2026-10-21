"""Build the Nemu redesign PROTOTYPE: the same sleepy ghost, cut and painted, on her own shapes.

    py -3 tools/author_character_redesign_nemu_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_nemu.py
    ... -- --where        (also print how high and low every piece reaches)

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/nemu/nemu-redesign.glb
    ArtSource/nemu/redesign-20261005/nemu_redesign.blend
beside the atlas the textures script paints.

WHY. docs/CHARACTER_REDESIGN_DANTE.md section 13. The owner had Dante reworked ("the same kid,
with a lot more detail", not another art style) and then asked for the rest of the cast to
follow. This is Nemu's own copy of Dante's builder, rewritten for her; it imports nothing of his.

⚠️⚠️ NEMU IS THE HERO A REWORK ALREADY RUINED ONCE. docs/CAST_CLOTHING_STYLE.md: a restyle
lowered her cowl, lifted her fringe and grew her head, and the owner said "u removed the jacket
that covered half of nemu's facee / that was on pruposee", "u ruined nemuu", then "nahh keep
nemu js improve her animations". HER COWL OVER HER LOWER FACE IS HER IDENTITY. So this file
does not design a silhouette. Every outer measurement below is read off team-nemu.glb
(tools/build_nemu_voxel.py), and the three that decide how much of her face shows are held to
the millimetre and checked by `verify`:
  * THE COWL'S RIM at z 0.298, its front at y -0.134, as wide as the original (x 0.118);
  * THE FRINGE'S FOOT at 0.348 (the two outer strands) and 0.358 (the two middle ones), its
    front at y -0.140 and -0.142; the paper tag on her right hangs to 0.350;
  * THE HEAD BOX, 0.250 wide and 0.204 deep from 0.275 to 0.460, its face at y -0.114, under a
    hair mass that tops out at 0.598. She is 0.598 tall. Dante is 0.785. She is not scaled up.
What changes is what the blocks are made of, never where they end.

A PROTOTYPE. NOTHING IN THE GAME LOADS IT. The folder is outside Resources, no roster row points
here, and team-nemu.glb, build_nemu_voxel.py and person_nemu.asset are not touched.

WHAT IS KEPT:
  * THE RIG. The .glb is team-nemu.glb with its two meshes replaced: the seven bones, their rest
    pose, the bind matrices and every clip but four are copied across untouched.
  * THE HAND where the original has it. Hers is not on the arm's axis as Dante's is: it is a
    small block tucked inside the bell of the sleeve, x 0.236 to 0.288, its top at z 0.210, which
    is 40 mm BELOW the arm bone. `verify` checks that top, so a carried tsinelas sits as today.
  * EVERYTHING SHE ALREADY HAS, and nothing she does not: the hime cut (a blunt four strand
    fringe, two long side locks, a blunt back), the paper talisman and its clip on her right
    fringe, two sleepy eye bars, the puffy cowl and its lavender bead, the oversized A-line
    hoodie with its square C print and the eye on its back, the lavender hem stripe, the three
    step bell sleeves with their lavender band and dark hollow, tucked block hands, bare calves,
    chunky violet sneakers on white soles with a lavender ankle strap and its pale tab.
    No nose and no mouth: the cowl covers them and the original draws neither. No visible ear:
    the original's ear blocks sit inside the side locks, so none is built.

WHAT CHANGES:
  * EVERY BLOCK IS STILL A BLOCK, BUT A CUT ONE: the head is the `shaped` carved box (rule 1),
    the sleeves' three steps each flare, the sneaker rises from toe to ankle, the hem is a
    skirt that kicks out, the crown carries three graded slabs, the back of the hair four hand
    set panels over a deeper mass (rule 3: a flat back or top gets BLOCKS, not drawing).
  * TRIM AND FASTENINGS ARE GEOMETRY (docs/CAST_CLOTHING_STYLE.md rule 3): the hem stripe and
    the cuff bands stand 3 to 4 mm proud, the cuff is a real hollow, the bead and its glint are
    blocks, the strap and its tab are blocks, the talisman is a slab hung from a clip.
  * PAINT. One atlas of her own (the textures script), in the half of UV space where
    `Toon.shader` reads the texture and not the palette.
  * CLOTH BENDS (rule 7, `Part.bend`): the skirt takes the legs toward its hem; the tails of the
    two side locks take the torso, so the head can turn without dragging them through the
    sleeves; and THE COWL IS THE HEAD'S (rule 6): only the ring where its underside meets the
    chest belongs to the torso. It turns and nods with her face, so her face is covered to the
    same line whatever the head does, and the head cannot turn through it.
  * TWO ELBOWS (rule 8), `forearm-left` and `forearm-right`, appended after the seven. The
    joint sits between the second and third step of the bell, where the sleeve droops.

HOW A FACE FINDS ITS PAINT. A piece is tagged with a group ("head", "torso", "armL" ...) and each
of its faces goes to the island of the side its normal points at, at the place it sits in model
space. Loose blocks take a swatch or a flat tone instead.
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
import author_character_redesign_nemu_textures as tex  # noqa: E402  the island layout
import author_character_redesign_nemu_clips as clips  # noqa: E402  the prototype's own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/team-nemu.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/nemu")
OUT = os.path.join(FOLDER, "nemu-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/nemu/redesign-20261005/nemu_redesign.blend")
NAME = "nemu-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# ⚠️ TWO BONES THE CAST DOES NOT HAVE (docs/CHARACTER_REDESIGN_DANTE.md rule 8). Each is a child
# of an arm bone, appended AFTER the seven so their indices and names do not move. A clip that
# does not key them leaves the arm straight, exactly as before.
# ⚠️ HER ELBOW IS NOT ON THE ARM'S AXIS. The arm bone is at z 0.250 but the bell droops: its
# third step is centred at 0.185. The joint is put where the second step meets the third, in
# the middle of the cloth there, so a fold turns the bell about its own throat.
# ⚠️ WHAT THIS COSTS IN THE GAME: `CharacterVisual.PalmCentre` finds the hand from `arm-right`
# alone; with the elbow bent the hand is no longer where that code parks a carried tsinelas.
ELBOW = (0.165, 0.0, 0.215)        # x (her left), y, z in Blender space
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
# ⚠️⚠️ THE HEAD IS BUILT AT THE ORIGINAL'S SIZE AND THEN BROUGHT IN TO 0.84 OF IT. The owner, after
# comparing the redesigned lineup in the game, Nemu included: "smaller heads are better", then
# "yes rebuild the heads at 84 for all seven". This is the owner's own word and it OVERRIDES
# the earlier rule that her head size must not change (docs/CAST_CLOTHING_STYLE.md). What that
# rule protected still holds, IN PROPORTION: everything skinned to the `head` bone (the head
# block, all the hair, the talisman and clip, and the COWL, which is the head's) is scaled as
# one piece about the head joint, AFTER the paint is placed. So the strip of face between the
# fringe's foot and the cowl's rim is the same shape, the eye bars sit in it the same way,
# and the cowl covers the same share of her face. Every measure in this file is therefore
# still written at the ORIGINAL size, and `verify` still holds those numbers to the original.
HEAD_SCALE = 0.84
HEAD_JOINT = Vector((0.0, 0.0024, 0.280))   # the `head` bone of team-nemu.glb, in Blender space
ORIGINAL_TOP = 0.598     # the original's top (team-nemu.glb head-mesh bounds)
HEIGHT = HEAD_JOINT.z + (ORIGINAL_TOP - HEAD_JOINT.z) * HEAD_SCALE   # 0.547: the top of her hair after the scale
COWL_FOOT_FIT = 0.92     # the one torso ring of the cowl closes in part of the way toward the smaller cowl
SHARP_ANGLE = 40.0       # degrees: a chamfer is a hard edge, a soft corner or a taper is one form
TRIANGLE_BUDGET = 6000   # the brief's ceiling; the original is 5,978

X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
AHEAD = (0, -1, 0)


# ---------------------------------------------------------------------------
# THE BLOCK. A ring is a rectangle with its four corners cut; a block is rings skinned in order.
# ---------------------------------------------------------------------------

def _pair(h):
    return h if isinstance(h, tuple) else (h, h)


def rect_ring(centre, u, v, hu, hv, chamfer):
    """Eight points: a rectangle `hu` by `hv` (a half size, or a (negative, positive) pair) with
    each corner cut back `chamfer`."""
    un, up = _pair(hu)
    vn, vp = _pair(hv)
    c = min(chamfer, 0.45 * (un + up), 0.45 * (vn + vp))
    c = max(c, 1e-5)
    flat = [(up, -vn + c), (up, vp - c), (up - c, vp), (-un + c, vp),
            (-un, vp - c), (-un, -vn + c), (-un + c, -vn), (up - c, -vn)]
    return [Vector(centre) + u * a + v * b for a, b in flat]


def proj(group):
    return ("proj", group)


def flat(name):
    return ("flat", name)


def strip(name, v0, v1, u=(0.0, 1.0)):
    """A swatch laid along a loft: u runs with the loft, v goes from `v0` to `v1` across the face."""
    return ("strip", name, v0, v1, u)


def proj_except(group, axis, name):
    """The group's islands, but a face turned along `axis` (0 x, 1 y, 2 z) takes one flat tone.

    A block's END faces look along its own length, where the group has drawn something else."""
    return ("except", group, axis, name)


def proj_facing(group, direction, name):
    """The group's islands for the faces turned toward `direction`, one flat tone for the rest."""
    return ("facing", group, direction, name)


def proj_away(group, direction, name):
    """The group's islands, but a face turned toward `direction` takes one flat tone."""
    return ("away", group, direction, name)


def tones(top, side, under):
    """No drawing at all: one flat tone for faces turned up, one for the sides, one for under."""
    return ("tones", top, side, under)


def lit(top, side, under):
    """`tones`, but a face turned to the FRONT takes the top tone too. Her fringe and the fronts
    of her side locks are the lit tone on the original (`hair-*-top`, `hair-bangs-brow-bevel`),
    and docs/CAST_CLOTHING_STYLE.md rule 7 asks for exactly that: a lit tone on the top locks and
    fringe, the dark tone underneath and behind."""
    return ("lit", top, side, under)


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
        self.rows = []      # the last loft's vertices, ring by ring
        self.fit = {}       # vertex -> its own head scale, for the ring where the cowl meets the chest

    def loft(self, name, bone, rings, mapping, caps=(True, True), tip=None, params=None):
        """Skin `rings` in order. `mapping` is one spec or f(segment, column) -> spec."""
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
        self.rows = rows
        faces = []
        spec = mapping if callable(mapping) else (lambda i, j: mapping)
        for i in range(count - 1):
            for j in range(n):
                k = (j + 1) % n
                quad = (rows[i][j], rows[i][k], rows[i + 1][k], rows[i + 1][j])
                f = bm.faces.new(quad)
                # (round the ring, along the loft, 0 at column j and 1 at column j + 1)
                uvp = [(across[j], params[i], 0.0), (across[j + 1], params[i], 1.0),
                       (across[j + 1], params[i + 1], 1.0), (across[j], params[i + 1], 0.0)]
                self.jobs.append((f, spec(i, j), uvp, quad))
                faces.append(f)

        def fan(row, i, at, seg, v_at):
            centre = vert(at)
            for j in range(n):
                k = (j + 1) % n
                tri = (row[j], row[k], centre)
                f = bm.faces.new(tri)
                uvp = [(across[j], params[i], 0.0), (across[j + 1], params[i], 1.0),
                       (0.5 * (across[j] + across[j + 1]), v_at, 0.5)]
                self.jobs.append((f, spec(seg, j), uvp, tri))
                faces.append(f)

        if caps[0]:
            fan(rows[0], 0, sum(rings[0], Vector()) / n, 0, params[0])
        if tip is not None:
            fan(rows[-1], count - 1, tip, count - 2, 1.0)
        elif caps[1]:
            fan(rows[-1], count - 1, sum(rings[-1], Vector()) / n, count - 2, params[-1])

        bmesh.ops.recalc_face_normals(bm, faces=faces)
        if "--where" in sys.argv:
            zs = [v.co.z for f in faces for v in f.verts]
            print("  %-26s z %.4f..%.4f" % (name, min(zs), max(zs)))
        self.pieces.append((name, sum(len(f.verts) - 2 for f in faces)))
        return faces

    def block(self, name, bone, u, v, stations, mapping, chamfer, caps=(True, True)):
        """A chamfered block through `stations` = [(centre, hu, hv)], its two ends chamfered too."""
        rings, params = [], []
        first, last = stations[0], stations[-1]
        along = (Vector(last[0]) - Vector(first[0]))
        length = along.length
        along = along.normalized()
        c = min(chamfer, 0.4 * length)

        def shrink(h):
            a, b = _pair(h)
            return (max(a - c, 0.0005), max(b - c, 0.0005))

        rings.append(rect_ring(first[0], u, v, shrink(first[1]), shrink(first[2]), chamfer * 0.4)); params.append(0.0)
        rings.append(rect_ring(Vector(first[0]) + along * c, u, v, first[1], first[2], chamfer)); params.append(c / length)
        for centre, hu, hv in stations[1:-1]:
            rings.append(rect_ring(centre, u, v, hu, hv, chamfer))
            params.append((Vector(centre) - Vector(first[0])).length / length)
        rings.append(rect_ring(Vector(last[0]) - along * c, u, v, last[1], last[2], chamfer)); params.append(1.0 - c / length)
        rings.append(rect_ring(last[0], u, v, shrink(last[1]), shrink(last[2]), chamfer * 0.4)); params.append(1.0)
        return self.loft(name, bone, rings, mapping, caps=caps, params=params)

    def bend(self, faces, weights):
        """Make a piece soft: `weights(position)` gives [(bone, weight)] for each of its vertices.

        The cast is rigid, one bone a vertex (Voxel_Person_Guide.md), and so is this model except
        where cloth or hair lies ACROSS two bones (docs/CHARACTER_REDESIGN_DANTE.md rule 7).
        """
        for f in faces:
            for v in f.verts:
                self.blend[v] = weights(v.co)

    def wedge(self, name, bone, base, tip, base_half, tip_half, mapping, chamfer, across=X):
        """A block standing on `base` and narrowing to `tip`: a hair strand, a slab."""
        w = (Vector(tip) - Vector(base)).normalized()
        u = (across - w * across.dot(w)).normalized()
        v = w.cross(u).normalized()
        return self.block(name, bone, u, v, [(base, base_half[0], base_half[1]), (tip, tip_half[0], tip_half[1])],
                          mapping, chamfer)

    def resolve_uvs(self):
        self.bm.normal_update()
        for face, spec, uvp, verts in self.jobs:
            kind = spec[0]
            n = face.normal
            if kind == "except":
                spec = flat(spec[3]) if abs(n[spec[2]]) > 0.6 else proj(spec[1])
            elif kind == "facing":
                spec = proj(spec[1]) if n.dot(Vector(spec[2])) > 0.6 else flat(spec[3])
            elif kind == "away":
                spec = flat(spec[3]) if n.dot(Vector(spec[2])) > 0.6 else proj(spec[1])
            elif kind == "tones":
                spec = flat(spec[1] if n.z > 0.45 else (spec[3] if n.z < -0.45 else spec[2]))
            elif kind == "lit":
                spec = flat(spec[1] if (n.z > 0.45 or n.y < -0.6) else (spec[3] if n.z < -0.45 else spec[2]))
            kind = spec[0]
            if kind == "proj":
                group = spec[1]
                best, score = None, -1e9
                for view in tex.GROUPS[group]:
                    d = Vector(tex.VIEW_DIR[view])
                    s = n.dot(d) * tex.VIEW_BIAS.get((group, view), 1.0)
                    if s > score:
                        best, score = view, s
                a, b = tex.VIEW_AXES[best]
                for loop in face.loops:
                    co = loop.vert.co
                    loop[self.uv].uv = tex.atlas_uv(group + "." + best, co[a], co[b])
            elif kind == "flat":
                for loop in face.loops:
                    loop[self.uv].uv = tex.atlas_uv(spec[1], 0.5, 0.5)
            else:   # strip
                _, name, v0, v1, ur = spec
                # recalc_face_normals may have reversed the loop order; pair params by vertex.
                order = {v: p for v, p in zip(verts, uvp)}
                for loop in face.loops:
                    _, along, t = order[loop.vert]
                    loop[self.uv].uv = tex.atlas_uv(name, ur[0] + (ur[1] - ur[0]) * along, v0 + (v1 - v0) * t)

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


def _pow(c, e):
    return math.copysign(abs(c) ** (2.0 / e), c)


def hoop(part, name, bone, profile, centre_y, exponent, mapping, count=28):
    """A closed band round her body: `profile` is [(z, half width, depth to the front, depth to
    the back)], swept round a squared path that starts and ends at the middle of her FRONT and
    goes round by her right. Its last section is its first again, so it has no ends to cap.
    """
    sections, params = [], []
    for k in range(count + 1):
        a = 2.0 * math.pi * (k % count) / count
        px = -_pow(math.sin(a), exponent)
        py = -_pow(math.cos(a), exponent)      # -1 in front, +1 behind
        sections.append([Vector((px * rx, centre_y + py * (rb if py >= 0 else rf), z)) for z, rx, rf, rb in profile])
        params.append(k / float(count))
    return part.loft(name, bone, sections, mapping, caps=(False, False), params=params)


# ---------------------------------------------------------------------------
# THE HEAD. The original's box, CARVED to Dante's `shaped` depth (rule 1): soft corners, cheeks
# a little full, a jaw that tucks in under the cowl. Read off team-nemu.glb: `face-core` is x
# 0.125 either side, y -0.114 to 0.090, z 0.285 to 0.460, with `face-chin-taper` under it to 0.275.
# Every ring is a superellipse of exponent 3.4 or more (2 is a circle), so the box wins.
# ⚠️ NO BROW LEDGE AND NO NOSE, which Dante's `shaped` head has. Her brow is behind the fringe
# and her nose behind the cowl; a ledge would only push the fringe out and a nose would be a
# lump on the one strip of face that shows.
# ⚠️ THE JAW IS NARROWER THAN THE ORIGINAL'S BELOW THE COWL'S RIM (0.106 against 0.125 at 0.298).
# The original's head is wider than its own collar and comes out through the collar's sides,
# hidden by the side locks. A cowl that turns with the head has to go ROUND the jaw.
#     rows are (z, half width, depth to the front, depth to the back, exponent)
# ---------------------------------------------------------------------------
HEAD_CY = -0.012
HEAD_N = 24
FACE_Y = -0.114
HEAD_ROWS = [
    (0.275, 0.080, 0.080, 0.074, 3.4),
    (0.283, 0.097, 0.094, 0.088, 3.8),
    (0.298, 0.106, 0.1000, 0.096, 4.0),
    (0.318, 0.124, 0.1020, 0.101, 4.2),
    (0.345, 0.125, 0.1020, 0.102, 4.2),
    (0.400, 0.124, 0.1010, 0.102, 4.2),
    (0.445, 0.120, 0.0980, 0.099, 4.0),
    (0.460, 0.104, 0.0860, 0.088, 3.4),
]


def super_ring(z, rx, rf, rb, e, n=HEAD_N):
    out = []
    for j in range(n):
        t = 2.0 * math.pi * j / n
        a = _pow(math.cos(t), e)
        b = _pow(math.sin(t), e)
        out.append(Vector((a * rx, HEAD_CY + b * (rb if b >= 0 else rf), z)))
    return out


def build_head(part):
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], proj("head"))


# ---------------------------------------------------------------------------
# THE HAIR: A HIME CUT, IN BLOCKS. No drawing on it at all (rule 3): each piece is a lighter
# plane where it is turned up (and, for the fringe and the locks, to the front), the flat tone
# round its sides, a darker one underneath. Every piece is set by hand.
# ---------------------------------------------------------------------------
HAIR_TOP = 0.598
CROWN_Z = (0.382, 0.585)
#   (name, base, tip, half size at the base, half size at the tip): three slabs on the crown, no
#   two the same height, all inside the original `hair-crown-top-apex` (x 0.120, y -0.095 to
#   0.105), the tallest reaching the original's 0.598 and nothing above it
CROWN_SLABS = [
    # ⚠️ between them the left and the back slab reach 0.598 across her whole width, so from the
    # front and the back her top edge is the original's; the right one is the low one
    ("top-right", (-0.060, -0.032, 0.578), (-0.060, -0.030, 0.5915), (0.058, 0.061), (0.050, 0.053)),
    ("top-left",  (0.066, -0.004, 0.578),  (0.068, 0.000, 0.598), (0.050, 0.088), (0.046, 0.080)),
    ("top-back",  (-0.046, 0.072, 0.578),  (-0.044, 0.074, 0.598), (0.072, 0.030), (0.068, 0.026)),
]
#   THE FRINGE. (name, centre x, half width, front y, foot z). The feet and the fronts are the
#   original's `hair-strand-*` boxes. ⚠️ DO NOT MOVE A FOOT. They are how much of her eyes show.
FRINGE_TOP = 0.452
FRINGE = [
    ("outer-left",  0.098,  0.032, -0.140, 0.348),
    ("mid-left",    0.033,  0.029, -0.142, 0.358),
    ("mid-right",   -0.033, 0.029, -0.142, 0.358),
    ("outer-right", -0.098, 0.032, -0.140, 0.348),
]
FRINGE_DEPTH = 0.022
#   THE BACK. A deeper mass, then four panels hung over it, cut where the original draws its
#   three seams (x -0.043, 0 and 0.043), each standing its own distance proud so each throws an
#   ink edge. Their feet are all the original's 0.315: a hime cut is blunt.
#   (name, x from, x to at the top, x from, x to at the foot, back face y, top z)
BACK_FOOT = 0.315
BACK_PANELS = [
    ("back-right-outer", -0.144, -0.050, -0.138, -0.049, 0.1385, 0.492),
    ("back-right-inner", -0.045, -0.004, -0.044, -0.004, 0.1410, 0.486),
    ("back-left-inner",  0.004,  0.040,  0.004,  0.039,  0.1400, 0.496),
    ("back-left-outer",  0.046,  0.144,  0.045,  0.138,  0.1380, 0.489),
]
#   THE SIDE LOCKS. Each is one piece: full depth beside the cheek (the original's
#   `hair-sidelock-main`, y -0.122 to 0.010, with `-front` reaching -0.134), then, below the
#   shoulder line, only the front part of it, hanging IN FRONT of the sleeve.
#   ⚠️ The original's lock is full depth all the way down to 0.200 and so stands inside the
#   sleeve's shoulder. Below 0.262 this one keeps only what the original shows from the front
#   (`hair-sidelock-front`, y to -0.096 here), which is clear of every sleeve block (their
#   fronts are at -0.078 to -0.102, but the nearest to the lock is the shoulder's, at -0.078).
#   ⚠️ REFITTED FOR THE 0.84 HEAD. Scaled, the lock comes in to x 0.100 to 0.123, over the sleeve's
#   shoulder instead of outside it. So the full depth part now stops at 0.289 (0.2876 after the
#   scale, above the shoulder's 0.285) and the tail keeps only y -0.136 to -0.111, which lands
#   at -0.114 to -0.094 after the scale: in front of the chest (-0.096) and of every sleeve block.
LOCK_X = (0.1195, 0.146)
LOCK_FOOT = 0.200
LOCK_TAIL_FROM = 0.268
# ⚠️ 0.12, NOT DANTE'S 0.60. His nape points are short and lie on his back. Her tail is 60 mm of
# hair whose root swings 140 mm when the head turns 55 degrees. At 0.70 (v02) the far lock
# was pulled into a string drawn from her cheek to her shoulder; at 0.30 (v05) it still hung
# as a bent hook across her chest, which the coordinator asked to have gone. A lock that goes
# WITH the head hangs straight in front of the cowl in free air, which is where hair is. What
# is left of the bend only leans the tip. The cost is on the other side: the near lock's tip
# dips about 20 mm into the top of that sleeve at the end of a 55 degree turn, violet in
# violet, and less than the original's lock stands in that sleeve at rest.
LOCK_TAIL_FOLLOW = 0.12   # how much of the torso the very tip takes


def build_hair(part):
    hair = tones("hair_top", "hair", "hair_under")
    front = lit("hair_top", "hair", "hair_under")
    deep = tones("hair", "hair_under", "hair_under")
    # the crown: the original's `hair-crown-core` with its side panels, one cut block
    # ⚠️ ITS TOP IS THE FLAT TONE, NOT THE LIT ONE. With the crown and the three slabs on it all
    # lit (v03) the top of her head from above was one pale square with three hairlines on it.
    # The slabs are the lit hair; the crown between them is the parting, in shade.
    part.block("hair-crown", "head", X, Y, [((0, 0.006, CROWN_Z[0]), 0.144, 0.122), ((0, 0.006, CROWN_Z[1]), 0.141, 0.120)],
               tones("hair", "hair", "hair_under"), 0.007)
    for name, base, tip, base_half, tip_half in CROWN_SLABS:
        # rings kept LEVEL (a block, not a wedge): a wedge's rings tilt with its lean and its
        # high corner then stands above the original's 0.598
        part.block("hair-" + name, "head", X, Y, [(base, *base_half), (tip, *tip_half)], hair, 0.006)

    # the forehead plate the strands hang from (`hair-bangs-brow-base` and its lit bevel). Its
    # lower half, behind the strands, is the deep tone: what shows in the gaps between strands
    # is shadow, as the original's `hair-bangs-gap-*` slivers are.
    part.block("hair-fringe-plate", "head", X, Y, [((0, -0.118, 0.448), 0.1335, 0.018), ((0, -0.1145, 0.573), 0.131, 0.0145)],
               front, 0.006)
    part.block("hair-fringe-under", "head", X, Y, [((0, -0.116, 0.384), 0.133, 0.016), ((0, -0.116, 0.452), 0.133, 0.016)],
               deep, 0.004)
    # ⚠️ EACH STRAND IS TWO BLOCKS, AS THE ORIGINAL'S IS (`hair-strand-*` and `hair-strand-*-top`):
    # a dark strand, and a lit face 4 mm proud of it and 5 mm in from its edges. Built as one lit
    # block each (v01) the four ran together into a single pale slab and her fringe lost the
    # tiles that make it read as a cut. The lit face is what reaches the original's front
    # (-0.140 and -0.142) and the dark strand is what reaches the original's foot.
    for name, cx, half, face, foot in FRINGE:
        cy = face + 0.004 + 0.5 * (FRINGE_DEPTH - 0.004)
        hy = 0.5 * (FRINGE_DEPTH - 0.004)
        part.wedge("hair-fringe-" + name, "head", (cx, cy, FRINGE_TOP), (cx, cy, foot), (half, hy), (half, hy),
                   tones("hair", "hair", "hair_under"), 0.004)
        part.wedge("hair-fringe-" + name + "-face", "head", (cx, face + 0.005, FRINGE_TOP - 0.006), (cx, face + 0.005, foot + 0.006),
                   (half - 0.005, 0.005), (half - 0.005, 0.005), flat("hair_top"), 0.0035)

    # the back: the mass, then the panels
    part.block("hair-back", "head", X, Y, [((0, 0.107, BACK_FOOT + 0.004), 0.133, 0.023), ((0, 0.107, 0.490), 0.138, 0.023)],
               deep, 0.006)
    for name, a0, a1, b0, b1, face, top in BACK_PANELS:
        depth = 0.029
        part.block("hair-" + name, "head", X, Y,
                   [((0.5 * (a0 + a1), face - depth, top), 0.5 * (a1 - a0), depth),
                    ((0.5 * (b0 + b1), face - depth, BACK_FOOT), 0.5 * (b1 - b0), depth)], hair, 0.005)

    # the side locks
    for s in (1, -1):
        cx = s * 0.5 * (LOCK_X[0] + LOCK_X[1])
        hx = 0.5 * (LOCK_X[1] - LOCK_X[0])
        faces = part.block("hair-lock", "head", X, Y,
                           [((cx, -0.062, 0.455), hx, 0.072), ((cx, -0.062, 0.289), hx, 0.072),
                            ((cx, -0.1235, 0.273), hx, 0.0125), ((cx + s * 0.0005, -0.1235, LOCK_FOOT), hx - 0.001, 0.0120)],
                           front, 0.005)
        # the tail lies on her chest: it stays with the shoulders when the head turns
        part.bend(faces, lambda co: [("head", 1.0 - LOCK_TAIL_FOLLOW * _ramp(LOCK_TAIL_FROM, LOCK_FOOT + 0.010, co.z)),
                                     ("torso", LOCK_TAIL_FOLLOW * _ramp(LOCK_TAIL_FROM, LOCK_FOOT + 0.010, co.z))])


def build_ofuda(part):
    """The paper talisman on her right fringe and the clip that holds it: the original's
    `ofuda-paper-body` (x -0.126 to -0.070, z 0.350 to 0.495) and `ofuda-clip-main`. A slab
    hung from a block; its writing is paint on the slab's front, its edges one flat tone."""
    part.block("ofuda-paper", "head", X, Y, [((-0.098, -0.1430, 0.495), 0.028, 0.0016), ((-0.098, -0.1434, 0.350), 0.028, 0.0016)],
               proj_facing("ofuda", AHEAD, "paper_edge"), 0.0008)
    part.block("ofuda-clip", "head", X, Y, [((-0.098, -0.138, 0.490), 0.026, 0.008), ((-0.098, -0.138, 0.542), 0.025, 0.008)],
               proj_facing("ofuda", AHEAD, "clip_tone"), 0.0035)


# ---------------------------------------------------------------------------
# THE HOODIE. One block for the body, a skirt for the flare, the cowl.
# ---------------------------------------------------------------------------
TORSO_Z = (0.135, 0.285)
TORSO_LOW = (0.113, 0.097)
TORSO_HIGH = (0.112, 0.096)
SKIRT_FOLLOW = 0.55   # how much of a leg's swing the hem takes
#   the skirt, top to hem and back up inside: (z, half width, depth to the front, depth to the
#   back). The original's `hoodie-skirt-hem` is 0.135 by 0.102 from 0.165 down, its lavender
#   `-stripe` 0.138 by 0.105 from 0.146 to 0.126, its `-shadow` foot at 0.118.
SKIRT = [
    (0.1700, 0.111, 0.095, 0.095),
    (0.1650, 0.120, 0.099, 0.099),
    (0.1510, 0.1335, 0.1015, 0.1015),
    (0.1465, 0.135, 0.102, 0.102),
    (0.1465, 0.138, 0.105, 0.105),
    (0.1260, 0.138, 0.105, 0.105),
    (0.1248, 0.134, 0.101, 0.101),
    (0.1180, 0.130, 0.098, 0.098),
    (0.1200, 0.122, 0.090, 0.090),
    (0.1500, 0.108, 0.088, 0.088),
]
#   THE COWL. (z, half width, depth to the front, depth to the back) about `COWL_CY`, from the
#   ring that meets the chest, out under the roll, up its wall, over its lip and down inside.
#   The original: `hoodie-collar-main` x 0.118, y -0.128 to 0.105, z 0.260 to 0.298, with
#   `-front` and `-rim` carrying its middle forward to -0.134 and -0.136.
#   ⚠️⚠️ THE RIM IS AT 0.298 AND ITS FRONT AT -0.134. THOSE TWO NUMBERS ARE HER FACE.
COWL_CY = -0.0115
COWL_RIM = 0.298
COWL = [
    (0.2660, 0.108, 0.082, 0.104),     # on the chest: the only ring that is the torso's
    (0.2600, 0.1165, 0.1195, 0.1150),
    (0.2640, 0.1185, 0.1225, 0.1165),
    (0.2915, 0.1185, 0.1225, 0.1165),
    (0.2980, 0.1140, 0.1175, 0.1120),
    (0.2972, 0.1095, 0.1070, 0.1020),
    (0.2760, 0.1095, 0.1070, 0.1020),
]


def build_torso(part):
    cloth = proj("torso")
    part.block("torso", "torso", X, Y, [((0, 0, TORSO_Z[0]), *TORSO_LOW), ((0, 0, TORSO_Z[1]), *TORSO_HIGH)], cloth, 0.012)

    # THE SKIRT: the flare of the hoodie below the waist, its lavender stripe standing 3 mm proud
    # (docs/CAST_CLOTHING_STYLE.md rule 3). It is hollow, and it BENDS: its top ring is the
    # torso's, toward the hem each side takes up to SKIRT_FOLLOW of its own leg.
    def skirt_paint(i, j):
        return (cloth, cloth, cloth, flat("lav_pale"), strip("trim", 1.0, 0.0), flat("lav_dark"),
                flat("hood_deep"), flat("hood_deep"), flat("hood_deep"), flat("hood_deep"))[j]

    skirt = hoop(part, "skirt", "torso", SKIRT, 0.0, 7.0, skirt_paint)

    def skirt_weights(co):
        leg = SKIRT_FOLLOW * _ramp(0.166, 0.120, co.z) ** 0.8
        left = _ramp(-0.060, 0.060, co.x)
        left = left * left * (3.0 - 2.0 * left)
        return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]

    part.bend(skirt, skirt_weights)

    # THE COWL. ⚠️ IT IS THE HEAD'S (rule 6). Only ring 0, where its underside meets the chest,
    # is the torso's. Everything the eye sees of it turns and nods with her face, so the line
    # it draws across her face never moves and the jaw cannot turn through it. All of the twist
    # is taken by the underside, in shadow under the roll.
    # ⚠️ NOT 0.92 OF THE HEAD, AS DANTE'S COLLAR IS. A square ring 4 mm off a square head, left
    # 8 per cent behind, is 4 degrees out at a 55 degree turn and its corner is in her cheek.
    def cowl_paint(i, j):
        return (flat("hood_deep"), strip("cowl_out", 0.0, 0.12), strip("cowl_out", 0.12, 0.84), strip("cowl_out", 0.84, 1.0),
                strip("cowl_in", 1.0, 0.0), flat("hood_deep"), flat("hood_deep"))[j]

    hoop(part, "cowl", "head", COWL, COWL_CY, 5.5, cowl_paint, count=32)
    for row in part.rows:
        part.blend[row[0]] = [("torso", 1.0)]
        part.fit[row[0]] = COWL_FOOT_FIT

    # the bead at the cowl's front and its glint: the original's `hoodie-collar-pearl` and
    # `-glint`, x 0.015 either side, z 0.265 to 0.295, standing to y -0.148 and -0.152
    part.block("cowl-bead", "head", X, Z, [((0, -0.128, 0.280), 0.015, 0.015), ((0, -0.148, 0.280), 0.0135, 0.0135)],
               tones("lav_pale", "lav_tone", "lav_dark"), 0.004)
    part.block("cowl-bead-glint", "head", X, Z, [((0, -0.146, 0.2815), 0.007, 0.0065), ((0, -0.152, 0.2815), 0.006, 0.0055)],
               flat("lav_pale"), 0.002)


# ---------------------------------------------------------------------------
# THE SLEEVES, straight out as the rig rests. `s` is +1 for her left, -1 for her right.
# The original's three steps, each now flaring a little toward the hand:
#   `sleeve-shoulder`   x 0.060 to 0.130, z 0.200 to 0.285, y 0.078 either side
#   `sleeve-bell-mid`   x 0.110 to 0.180, z 0.160 to 0.270, y 0.092
#   `sleeve-cuff-outer` x 0.160 to 0.240, z 0.110 to 0.260, y 0.102
#   `sleeve-cuff-stripe` x 0.220 to 0.245, z 0.105 to 0.265, y 0.106, and the dark hollow in it
#   `hand-palm`         x 0.236 to 0.288, y -0.015 to 0.025, z 0.160 to 0.210
# ---------------------------------------------------------------------------

def sleeve_block(part, name, bone, s, group, x0, x1, low0, high0, half0, low1, high1, half1, chamfer):
    return part.block(name, bone, Y, Z,
                      [((s * x0, 0.0, 0.5 * (low0 + high0)), half0, 0.5 * (high0 - low0)),
                       ((s * x1, 0.0, 0.5 * (low1 + high1)), half1, 0.5 * (high1 - low1))],
                      proj_except(group, 0, "hood_tone"), chamfer)


def build_sleeve(part, s):
    arm, fore = ("arm-left", "forearm-left") if s > 0 else ("arm-right", "forearm-right")
    group, hand = ("armL", "handL") if s > 0 else ("armR", "handR")
    # ⚠️ the shoulder's inner end stops at 0.280, not 0.285: `idle` drops the arm 45 degrees and
    # that corner swings UP, to 0.297 on the original, which is the cowl's rim
    # (0.274 since the head came in to 0.84: the cowl's rim is now at 0.295 and narrower)
    sleeve_block(part, "sleeve-shoulder", arm, s, group, 0.060, 0.130, 0.204, 0.274, 0.074, 0.200, 0.285, 0.078, 0.012)
    # ⚠️ each step flares in DEPTH only (y). A flare in height too (v01) cut a wedge off the
    # underside of each step, and the bell's stepped lower edge is most of her outline in `idle`.
    # Their feet are the original's `-shad` and `-under` boxes (0.150 and 0.100), 10 mm below the
    # blocks named in the table above: the silhouette sheet found the strip that was missing.
    sleeve_block(part, "sleeve-mid", arm, s, group, 0.110, 0.180, 0.151, 0.270, 0.088, 0.150, 0.270, 0.092, 0.012)
    sleeve_block(part, "sleeve-cuff", fore, s, group, 0.160, 0.240, 0.101, 0.260, 0.098, 0.100, 0.260, 0.102, 0.012)
    # the band: 4 mm proud, and a real hollow in its end, 8 mm deep, the dark of the sleeve's inside
    zc, hy, hz = 0.185, 0.106, 0.080
    rings = [rect_ring((s * 0.2185, 0, zc), Y, Z, hy, hz, 0.011), rect_ring((s * 0.2460, 0, zc), Y, Z, hy, hz, 0.011),
             rect_ring((s * 0.2460, 0, zc), Y, Z, 0.0925, 0.0675, 0.008), rect_ring((s * 0.2380, 0, zc), Y, Z, 0.0925, 0.0675, 0.008)]
    part.loft("sleeve-band", fore, rings,
              lambda i, j: proj_away(group, (-s, 0, 0), "lav_dark") if i < 2 else flat("hood_deep"))
    # the hand: one tucked block, as the original's is, no fingers and no thumb
    part.block("hand", fore, Y, Z, [((s * 0.236, 0.005, 0.185), 0.020, 0.025), ((s * 0.288, 0.005, 0.185), 0.0195, 0.0245)],
               proj(hand), 0.006)


# ---------------------------------------------------------------------------
# THE LEGS. Short, a bare calf under the hem, a chunky sneaker. Feet straight, as hers are.
# ---------------------------------------------------------------------------
LEG_X = 0.070


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    cloth = proj(group)
    # the calf (`leg-skin`: x 0.042 to 0.098, y -0.055 to 0.005), run up INSIDE the torso so a
    # swung leg never shows its top
    part.block("calf", bone, X, Y, [((s * LEG_X, -0.025, 0.066), 0.026, 0.028), ((s * LEG_X, -0.025, 0.158), 0.028, 0.030)],
               proj_except(group, 2, "skin_tone"), 0.008)

    # the sneaker: low at the toe, rising to the ankle (`shoe-toe` 0.048, `shoe-upper` 0.058)
    def station(y, half, z0, z1, cut):
        return rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)

    upper = [(-0.1035, 0.036, 0.021, 0.036, 0.004), (-0.096, 0.0455, 0.020, 0.046, 0.009), (-0.060, 0.046, 0.020, 0.049, 0.010),
             (-0.040, 0.046, 0.020, 0.056, 0.010), (0.030, 0.046, 0.020, 0.056, 0.010), (0.050, 0.0455, 0.020, 0.054, 0.009),
             (0.0575, 0.038, 0.021, 0.046, 0.004)]
    part.loft("shoe", bone, [station(*row) for row in upper], cloth)
    # the heel counter stands above the strap behind (`shoe-heel`, to 0.062): its own block and
    # its own flat tones, or the strap's lavender band would be drawn across it
    part.block("shoe-heel", bone, X, Z, [((s * LEG_X, 0.022, 0.051), 0.0455, 0.011), ((s * LEG_X, 0.058, 0.050), 0.044, 0.010)],
               tones("shoe_lit", "shoe", "shoe_dark"), 0.004)
    # the ankle strap and its pale tab (`shoe-strap`, `shoe-strap-accent`)
    part.block("shoe-strap", bone, X, Y, [((s * LEG_X, -0.016, 0.052), 0.048, 0.064), ((s * LEG_X, -0.016, 0.074), 0.048, 0.064)],
               proj_except(group, 2, "lav_tone"), 0.005)
    part.block("shoe-strap-tab", bone, X, Z, [((s * LEG_X, -0.070, 0.064), 0.040, 0.006), ((s * LEG_X, -0.0855, 0.064), 0.038, 0.0055)],
               flat("lav_pale"), 0.002)
    # the sole (`shoe-sole`: x 0.020 to 0.120, y -0.100 to 0.060, to 0.022), a little boat shaped
    part.block("sole", bone, X, Z, [((s * LEG_X, -0.101, 0.011), 0.043, 0.011), ((s * LEG_X, -0.072, 0.011), 0.050, 0.011),
                                   ((s * LEG_X, 0.030, 0.011), 0.050, 0.011), ((s * LEG_X, 0.060, 0.011), 0.045, 0.011)],
               proj_except(group, 2, "sole_tone"), 0.005)


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
    """team-nemu.glb with its meshes swapped: skeleton, bind matrices and clips copied untouched."""
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
    for anim in gltf["animations"]:
        if anim["name"] in clips.CLIPS:
            continue
        for sampler in anim["samplers"]:
            sampler["input"] = keep(sampler["input"])
            sampler["output"] = keep(sampler["output"])

    # the two elbows: a node under each arm, a joint and a bind matrix appended after the seven.
    # Every rest rotation in this rig is identity, so a bind matrix is the inverse translation.
    node_of = {n.get("name"): i for i, n in enumerate(gltf["nodes"])}
    skin = gltf["skins"][0]
    raw = struct.unpack("<%df" % (16 * len(BONES)), accessor_raw)
    matrices = [raw[k * 16:(k + 1) * 16] for k in range(len(BONES))]
    for name, parent, side in EXTRA_BONES:
        world = (side * ELBOW[0], ELBOW[2], -ELBOW[1])
        pw = [-matrices[BONES.index(parent)][12 + a] for a in range(3)]
        gltf["nodes"].append({"name": name, "translation": [world[a] - pw[a] for a in range(3)]})
        gltf["nodes"][node_of[parent]].setdefault("children", []).append(len(gltf["nodes"]) - 1)
        # the base file has one skin a mesh, both over the same seven joints: both get the elbows
        for each in gltf["skins"]:
            each["joints"].append(len(gltf["nodes"]) - 1)
        matrices.append((1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -world[0], -world[1], -world[2], 1))
    binds = add([tuple(float(x) for x in m) for m in matrices], "f", "MAT4", 5126)
    for each in gltf["skins"]:
        each["inverseBindMatrices"] = binds

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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (nemu)"}
    gltf["extras"] = {"prototype": "character-redesign-20261005", "replaces": "nothing; not loaded by the game"}
    gltf["images"] = [{"uri": tex.ATLAS_NAME, "name": NAME + "-atlas"}]
    gltf["textures"] = [{"sampler": 0, "source": 0, "name": NAME + "-atlas"}]
    # Bilinear with mips, as the beggar's painted atlas is: the stock colormap's nearest filter
    # is for flat palette cells and would stair-step a drawn line.
    gltf["samplers"] = [{"magFilter": 9729, "minFilter": 9987}]
    material = gltf["materials"][0]
    material["name"] = NAME
    material["doubleSided"] = False
    # the stock material carries a texture transform for the shared colormap; this atlas has none
    material["pbrMetallicRoughness"]["baseColorTexture"] = {"index": 0}
    gltf["nodes"][0]["name"] = NAME
    gltf["scenes"][0]["name"] = NAME
    for key in ("extensionsUsed", "extensionsRequired"):
        if key in gltf:
            gltf[key] = [e for e in gltf[key] if e != "KHR_texture_transform"]
            if not gltf[key]:
                del gltf[key]
    bpv.write_glb(out, gltf, blob)


def verify(objects, head_part):
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
    print("          (the original: x -0.288..0.288  y -0.152..0.140  z 0.000..0.598; the head is now %.2f of it)" % HEAD_SCALE)
    print("triangles %d  (budget %d, the original is 5978)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if abs(hi.z - HEIGHT) > 0.002:
        raise SystemExit("the top is at %.4f; the original's %.3f at a %.2f head is %.4f" % (hi.z, ORIGINAL_TOP, HEAD_SCALE, HEIGHT))
    if abs(hi.x - 0.288) > 0.002 or abs(lo.x + 0.288) > 0.002:
        raise SystemExit("her reach moved: x %.4f..%.4f, the original's is 0.288 either side" % (lo.x, hi.x))
    if tris > TRIANGLE_BUDGET:
        raise SystemExit("%d triangles is over the budget of %d" % (tris, TRIANGLE_BUDGET))

    # THE HAND. Hers is tucked in the bell, its top 40 mm below the arm bone (team-nemu.glb
    # `hand-palm`, z 0.160 to 0.210). A carried tsinelas is parked off that top.
    body = objects[0]
    group = body.vertex_groups["forearm-right"].index
    hand = [v.co for v in body.data.vertices if v.groups[0].group == group and v.co.x < -0.250]
    top = max(p.z for p in hand)
    print("hand top %.4f (the original's is 0.2100), far end %.4f (0.288)" % (top, -min(p.x for p in hand)))
    if abs(top - 0.210) > 0.002:
        raise SystemExit("the hand's top moved; a carried tsinelas would float or sink")

    # HER FACE. The three measures that decide how much of it shows, against the original's.
    cowl_top = max(z for z, *_ in COWL)
    cowl_front = COWL_CY - max(rf for _, _, rf, _ in COWL)
    feet = {name: foot for name, _, _, _, foot in FRINGE}
    checks = [("cowl rim", cowl_top, 0.298), ("cowl front", cowl_front, -0.134),
              ("fringe foot, outer", feet["outer-left"], 0.348), ("fringe foot, outer right", feet["outer-right"], 0.348),
              ("fringe foot, middle", feet["mid-left"], 0.358), ("fringe foot, middle right", feet["mid-right"], 0.358),
              ("face plane", HEAD_CY - max(r[2] for r in HEAD_ROWS), FACE_Y),
              ("head top", HEAD_ROWS[-1][0], 0.460), ("head half width", max(r[1] for r in HEAD_ROWS), 0.125)]
    # ⚠️ these are the measures the head is BUILT at, held to the original's. The whole head is
    # then scaled as one piece, so holding them holds her proportions; the third column is where
    # each lands on the 0.84 head.
    def scaled(label, value):
        axis = 1 if ("front" in label or "plane" in label) else (0 if "width" in label else 2)
        return HEAD_JOINT[axis] + (value - HEAD_JOINT[axis]) * HEAD_SCALE

    for label, got, want in checks:
        print("  %-26s %.4f  (original %.4f)  at %.2f: %.4f" % (label, got, want, HEAD_SCALE, scaled(label, got)))
        if abs(got - want) > 0.0006:
            raise SystemExit("%s is %.4f; the original's is %.4f and that number is her face" % (label, got, want))


def build(armature, material):
    body, head = Part("body-mesh"), Part("head-mesh")
    build_torso(body)
    build_sleeve(body, 1)
    build_sleeve(body, -1)
    build_leg(body, 1)
    build_leg(body, -1)
    build_head(head)
    build_hair(head)
    build_ofuda(head)
    # ⚠️ the cowl and its bead ride the head bone but are built into the BODY mesh by
    # `build_torso`, as the original's collar is body-mesh: a mesh may weight to any joint.
    objects = []
    for part in (body, head):
        part.resolve_uvs()
        # AFTER the paint is placed (it is projected from the full size shape): the head comes
        # in. A vertex is scaled by as much of it as is the head's, so a lock's tail, which is
        # part torso, comes in a little less than its root and still joins it.
        layer = part.bm.verts.layers.int["bone"]
        for v in part.bm.verts:
            if v in part.fit:
                k = part.fit[v]
            else:
                mix = part.blend.get(v)
                if mix:
                    share = sum(w for b, w in mix if b == "head") / sum(w for _, w in mix)
                else:
                    share = 1.0 if ALL_BONES[v[layer]] == "head" else 0.0
                k = 1.0 + (HEAD_SCALE - 1.0) * share
            if k != 1.0:
                v.co = HEAD_JOINT + (v.co - HEAD_JOINT) * k
        obj = part.to_object(armature)
        obj.data.materials.append(material)
        objects.append(obj)
        print(part.name, " ".join("%s:%d" % (n, t) for n, t in part.pieces))
    verify(objects, head)
    return objects


def main():
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
        eb.head = (side * ELBOW[0], ELBOW[1], ELBOW[2])
        eb.tail = (side * (ELBOW[0] + 0.06), ELBOW[1], ELBOW[2])
        eb.parent = armature.data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    if armature.animation_data:
        armature.animation_data.action = None

    image_path = os.path.join(FOLDER, tex.ATLAS_NAME)
    if not os.path.exists(image_path):
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_nemu_textures.py")
    image = bpy.data.images.load(image_path)
    material = bpy.data.materials.new(NAME)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    node = nodes.new("ShaderNodeTexImage")
    node.image = image
    material.node_tree.links.new(node.outputs[0], nodes["Principled BSDF"].inputs["Base Color"])
    nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0

    objects = build(armature, material)
    write_glb(mesh_arrays(objects[0]), mesh_arrays(objects[1]), OUT)
    print("wrote " + OUT)
    os.makedirs(os.path.dirname(BLEND), exist_ok=True)
    image.filepath = bpy.path.relpath(image_path, start=os.path.dirname(BLEND))
    bpy.context.preferences.filepaths.save_version = 0   # no .blend1 beside it
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)
    print("wrote " + BLEND)


if __name__ == "__main__":
    main()
