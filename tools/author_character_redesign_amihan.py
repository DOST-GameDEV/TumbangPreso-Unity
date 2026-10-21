"""Build the Amihan redesign PROTOTYPE: the same block kid, shaped and painted.

    py -3 tools/author_character_redesign_amihan_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_amihan.py
    ... -- --out other.glb     (writes there instead and leaves the .blend alone; needs the atlas beside it)

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/amihan/amihan-redesign.glb
    ArtSource/amihan/redesign-20261005/amihan_redesign.blend
beside the atlas painted by the textures script.

WHY. Owner, 2026-10-05, after Dante's rework: "following dante's rework, redesign the rest of the
characters". This is a copy of tools/author_character_redesign_dante.py rewritten for her; that
file, and every other hero's, is left alone (docs/CHARACTER_REDESIGN_DANTE.md section 13).

A PROTOTYPE. NOTHING IN THE GAME LOADS IT. The folder is outside Resources, no roster row points
here, and team-amihan.glb, build_amihan_voxel.py and person_amihan.asset are not touched.

THE ELEVEN RULES OF SECTION 13, and where each lands in this file:
  1  the head is the game's BOX head, `shaped` carving: `HEAD_ROWS`, superellipse rings of
     exponent 3.4 to 4.2 on the original's box;
  2  nothing invented: every piece below is one team-amihan.glb has (measured off that file, see
     the numbers beside each block), in the original palette's own hex values;
  3  no painted hair shine: hair is flat tones by which way a face points, and her flat back and
     top are BLOCKS (`BACK_SLABS`, the waves on the crown);
  4  paint stops at its own piece: the forearm is a flat tone so it cannot carry the cuff's
     paint out of the cuff, block ends take a tone of their own (`proj_except`);
  6  the capelet is symmetric, as the original's is, and lies wholly BELOW the head block, so
     the head cannot turn through it (see THE CAPELET);
  7  cloth across two bones bends (`Part.bend`): the coat skirt and the sash take the legs, the
     capelet's shoulders take the arms, the nape tips take the torso;
  8  two elbow bones appended after the seven;
  10 feet on zero, the hand top where a tsinelas sits; the height is the original's body under
     a head at 0.84 (`HEAD_SCALE`), 0.715 against 0.786.

WHAT IS KEPT of the original, read off team-amihan.glb: the rig and every clip but the four the
clips script replaces; the head box 0.340 wide from 0.343 to 0.661; the torso from 0.176 to
0.343; legs to 0.176; arms straight out to 0.275 (hers are 15 mm shorter than Dante's); the
hair's top at 0.786; feet on zero, facing -y.

HOW A FACE FINDS ITS PAINT. A piece is tagged with a group ("head", "torso", "cape" ...) and each
of its faces goes to the island of the side its normal points at, at the place it sits in model
space. Loose pieces take a swatch, a panel or a flat tone instead.
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
import author_character_redesign_amihan_textures as tex  # noqa: E402  the island layout
import author_character_redesign_amihan_clips as clips  # noqa: E402  her own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/team-amihan.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/amihan")
OUT = os.path.join(FOLDER, "amihan-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/amihan/redesign-20261005/amihan_redesign.blend")
NAME = "amihan-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# TWO BONES THE CAST DOES NOT HAVE (rule 8): an elbow in each arm, a child of the arm bone,
# appended AFTER the seven so their indices and names do not move. A clip that does not key them
# leaves the arm straight. Hers sits at the mouth of the cuff: the upper arm is the whole bell
# sleeve and the forearm bone carries only the bare forearm and the hand, which slide out of the
# cuff as the elbow folds.
# WHAT THIS COSTS IN THE GAME: `CharacterVisual.PalmCentre` finds the hand from `arm-right` alone.
ELBOW_X = 0.186
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
# THE HEAD IS 84 PER CENT OF THE ORIGINAL'S. Owner, 2026-10-05, after comparing sizes in the game on
# the whole lineup: "smaller heads are better", then "yes rebuild the heads at 84 for all seven".
# Everything is still BUILT and PAINTED at the original's size (every number below is the full-size
# one, measured off team-amihan.glb), and only after the UVs are resolved is every vertex that rides
# the `head` bone drawn in toward the head JOINT (`build`). So the paint keeps its place and nothing
# inside the head changes proportion: the face, the eyes and the mouth are untouched.
HEAD_SCALE = 0.84
HEAD_JOINT = Vector((0.0, 0.0024, 0.343))   # the `head` bone of team-amihan.glb, in Blender space
ORIGINAL_TOP = 0.786     # the original's top, hair included (team-amihan.glb head-mesh bounds)
HEIGHT = HEAD_JOINT.z + (ORIGINAL_TOP - HEAD_JOINT.z) * HEAD_SCALE   # 0.715, the top of her hair after the scale
SHARP_ANGLE = 40.0       # degrees: a chamfer is a hard edge, a soft corner or a taper is one form
TRIANGLE_BUDGET = 6000   # rule 10; the original is 5,986

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


def tones(top, side, under):
    """No drawing at all: one flat tone for faces turned up, one for the sides, one for under."""
    return ("tones", top, side, under)


def panel(name, a, b, window, direction, other):
    """A drawn swatch laid flat on the faces turned toward `direction`: (a, b) are the two model
    axes it is seen along and `window` the metres it spans. Every other face takes a flat tone."""
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
            elif kind == "panel" and face.normal.dot(Vector(spec[5])) <= 0.6:
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
# THE HEAD. The original's box (0.340 wide, 0.322 deep, 0.343 to 0.661, the same donor skull the
# whole cast wears), in the `shaped` carving the owner picked on Dante: cheek fullness, soft
# corners, a jaw rounded in to the chin (see the rows), every ring a superellipse of exponent 3.4 or more so the box wins.
#     rows are (z, half width, depth to the front, depth to the back, exponent)
# ---------------------------------------------------------------------------
HEAD_CENTRE = Vector((0.0, 0.0, 0.50))
HEAD_N = 24
HEAD_ROWS = [
    # DANTE'S HEAD SHAPE, on her box. Owner, 2026-10-05: "why dont the other characters use the same
    # headshape as dante? dante has a much more rounded face shape compared to what amihan and
    # cheska have". These are his `shaped` rows: soft superellipse corners (3.4 to 4.2), fullest at
    # the cheeks (0.181 against 0.171 at the temples), narrowing gently to the crown, the lower
    # corners rounded in toward the chin.
    # TWO THINGS DIFFER FROM HIS, AND BOTH ARE HERS:
    #   THE JAW ROUNDS IN GENTLY AND CLOSES LOW. His jaw turns under 38 degrees from 0.388 down and
    #   a standing collar hides it. She has no collar: on her that turn caught the toon shadow as
    #   hard dark wedges at the jaw corners ("fix this random dark spots on amihan"). So her taper
    #   is spread over the last third of the head at no more than about 18 degrees, and the tight
    #   closing ring sits from 0.349 down, under the capelet's shoulder line. (A square jaw was
    #   tried in between; it cured the spots and lost his shape.)
    # THE FACE IS ONE FLAT PLANE. From 0.400 (under the mouth) to 0.604 (the hairline) the front depth
    # is ONE value, 0.165. First finding from the real game shader, on Dante: "the eyebrow dent is
    # making this weird shading artifact where theres a straight line on his head". In the two-band
    # toon shader any ring where the FRONT steps in or out, a brow ledge or a cheek standing proud,
    # flips the light band and draws a hard level line across the face; Blender's imitation hid
    # it. So there is no brow swell at all (it was 2 mm) and no front cheek bulge (it was 4 mm at
    # 0.432): cheek fullness lives only in the WIDTH. The front rounds over only below the mouth
    # and above the hairline.
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
    skin = proj("head")
    part.loft("head-box", "head", [super_ring(*row) for row in HEAD_ROWS], skin)
    # NO NOSE (rule 12). A small wedge stood here. Owner, 2026-10-05: the faces "look too human,
    # like it lost its charm.. they need to be more cutesy". The cast has no nose.
    # ears: small blocks, as the original's are, narrowing a little outward
    for s in (1, -1):
        part.block("ear", "head", Y, Z, [((s * 0.164, 0.012, 0.456), 0.030, 0.042), ((s * 0.216, 0.016, 0.456), 0.024, 0.034)],
                   skin, 0.010)


# ---------------------------------------------------------------------------
# THE HAIR. Hers on the original (dark from 0.394 to 0.752, |x| to 0.238, the lit tone on top to
# 0.786): a crown mass, three lit waves of graded heights on it and a dark one
# behind, a fringe of four locks swept from her LEFT across the brow and longest on her RIGHT,
# two locks hanging on each side, a back that stops at the nape in three uneven tips.
# TWO TONES (CAST_CLOTHING_STYLE.md rule 7): the top locks and the left of the fringe wear the
# lit brown, everything under and behind the dark brown. Each tone set is top, side and under;
# nothing is drawn on any of it (rule 3).
# Every wedge is set by hand: (name, base, tip, base half size, tip half size, which tones).
# SHE IS THE WIND HERO: every tip leans a little toward her right and back, the way the fringe
# is swept, as if the air came from her front left. That is the only liberty taken.
# ---------------------------------------------------------------------------
HAIR_CHAMFER = 0.013
LIT = tones("hairlit_top", "hairlit", "hair")
DARK = tones("hair_mid", "hair", "hair_under")
DEEP = tones("hair", "hair_under", "hair_under")
WEDGES = [
    # the fringe, hanging in front of the flat face (its front is at y -0.166)
    ("fringe-1", (0.090, -0.180, 0.674), (0.070, -0.186, 0.612), (0.050, 0.022), (0.040, 0.015), LIT, 0.006),
    ("fringe-2", (0.010, -0.182, 0.674), (-0.016, -0.189, 0.584), (0.050, 0.024), (0.034, 0.015), LIT, 0.006),
    ("fringe-3", (-0.072, -0.182, 0.670), (-0.094, -0.188, 0.550), (0.046, 0.024), (0.026, 0.014), DARK, 0.006),
    ("fringe-4", (-0.146, -0.172, 0.664), (-0.160, -0.180, 0.484), (0.036, 0.028), (0.017, 0.014), DARK, 0.006),
    # in front of each ear; her right one falls lower, as the original's side hair does
    ("sideburn-right", (-0.206, -0.094, 0.640), (-0.204, -0.082, 0.440), (0.026, 0.048), (0.016, 0.022), DARK, 0.009),
    ("sideburn-left", (0.206, -0.110, 0.644), (0.206, -0.100, 0.500), (0.024, 0.040), (0.017, 0.022), DARK, 0.009),
    # the waves on the crown: three lit chunks of three heights standing straight on the dark
    # crown, and a dark one behind them. They ARE her top: a lit slab under them was tried and from
    # above it read as a flat lid with tiles on it (rule 3: a flat top gets blocks). The dark crown
    # shows in the gaps between them.
    ("wave-right", (-0.114, -0.030, 0.704), (-0.122, -0.020, 0.752), (0.062, 0.112), (0.048, 0.084), LIT, 0.016),
    ("wave-mid", (-0.004, 0.006, 0.704), (-0.012, 0.018, 0.771), (0.058, 0.120), (0.044, 0.086), LIT, 0.016),
    ("wave-left", (0.108, -0.026, 0.704), (0.104, -0.016, 0.744), (0.062, 0.110), (0.046, 0.082), LIT, 0.016),
    ("wave-back", (0.000, 0.158, 0.702), (-0.008, 0.166, 0.738), (0.150, 0.048), (0.118, 0.032), DARK, 0.012),
    # the hair over each side: two locks a side, of two lengths. The one over the ear stops ABOVE
    # the ear block (its top is at 0.498) so the ear stays clear skin; the one behind it hangs
    # lower. (One block a side was tried first and read as a helmet's cheek flap.)
    ("side-right-fore", (-0.212, 0.024, 0.646), (-0.214, 0.026, 0.506), (0.027, 0.050), (0.022, 0.038), DARK, 0.010),
    ("side-right-aft", (-0.210, 0.128, 0.646), (-0.214, 0.136, 0.452), (0.028, 0.062), (0.020, 0.040), DARK, 0.010),
    ("side-left-fore", (0.212, 0.020, 0.646), (0.213, 0.022, 0.514), (0.027, 0.054), (0.022, 0.040), DARK, 0.010),
    ("side-left-aft", (0.210, 0.128, 0.646), (0.212, 0.134, 0.470), (0.028, 0.060), (0.020, 0.040), DARK, 0.010),
    # the back stops at the nape in three tips, each its own length (the middle one the longest)
    ("nape-right", (-0.136, 0.196, 0.454), (-0.158, 0.214, 0.400), (0.062, 0.024), (0.034, 0.016), DEEP, 0.008),
    ("nape-mid", (0.000, 0.198, 0.454), (-0.014, 0.220, 0.390), (0.066, 0.024), (0.036, 0.016), DEEP, 0.008),
    ("nape-left", (0.134, 0.196, 0.454), (0.142, 0.212, 0.410), (0.060, 0.024), (0.032, 0.015), DEEP, 0.008),
]
#   the layer over the back: (name, top, foot, half size at the top, half size at the foot).
#   Rule 3: the original's back is one flat slab, and the answer is blocks, not paint. Three slabs
#   hang from the crown over the back mass, 14 mm proud, each its own width and length, their feet
#   kicked out a little: a wave, cut the block way.
BACK_SLABS = [
    ("back-right", (-0.140, 0.222, 0.690), (-0.156, 0.232, 0.506), (0.062, 0.016), (0.046, 0.014)),
    ("back-mid", (-0.008, 0.224, 0.694), (-0.022, 0.236, 0.466), (0.064, 0.017), (0.048, 0.014)),
    ("back-left", (0.128, 0.222, 0.690), (0.132, 0.230, 0.536), (0.060, 0.016), (0.044, 0.014)),
]
NAPE_FOLLOW = 0.35   # how much of the shoulders a nape tip keeps when the head turns


def build_hair(part):
    # the crown mass: its front edge overhangs the forehead, the fringe hangs from that edge
    part.block("hair-crown", "head", X, Y, [((0, 0, 0.598), 0.214, (0.176, 0.216)), ((0, 0, 0.714), 0.200, (0.166, 0.204))],
               DARK, HAIR_CHAMFER)
    # the mass down the back, one step deeper, so the slabs laid over it read as hair on hair
    part.block("hair-back", "head", X, Y, [((0, 0.186, 0.444), 0.214, 0.032), ((0, 0.184, 0.664), 0.220, 0.036)],
               DEEP, HAIR_CHAMFER)
    # under the back mass, on the back of the head: what shows between the nape tips is hair
    part.block("hair-nape-liner", "head", X, Y, [((0, 0.158, 0.386), 0.150, 0.010), ((0, 0.162, 0.448), 0.160, 0.011)],
               flat("hair_under"), 0.004)
    for name, base, tip, base_half, tip_half, paint, chamfer in WEDGES:
        faces = part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, paint, chamfer)
        if name.startswith("nape"):
            part.bend(faces, lambda co: [("head", 1.0 - NAPE_FOLLOW * _ramp(0.454, 0.395, co.z)),
                                         ("torso", NAPE_FOLLOW * _ramp(0.454, 0.395, co.z))])
    for name, base, tip, base_half, tip_half in BACK_SLABS:
        part.wedge("hair-" + name, "head", base, tip, base_half, tip_half, DARK, 0.008)


# ---------------------------------------------------------------------------
# THE COTTON-BOLL PIN. Three flowers on her left temple, each FOUR PETALS ROUND A GOLD HEART (the
# original's, 78, 69 and 60 mm across, stepping down and out), and two gold tassels hanging from
# the last. Each petal is its own chamfered block, each flower is turned to face out from the
# corner of the hair it is pinned to: (centre, petal size, degrees turned toward her left).
# ---------------------------------------------------------------------------
FLOWERS = [
    ((0.150, -0.180, 0.692), 0.026, 6.0),
    ((0.200, -0.166, 0.638), 0.023, 34.0),
    ((0.232, -0.128, 0.584), 0.020, 62.0),
]


def build_flowers(part):
    cotton = tones("cotton", "cotton", "cotton_shade")
    heart = tones("gold_lit", "gold_tone", "gold_dark")
    for (cx, cy, cz), p, turn in FLOWERS:
        cs, sn = math.cos(math.radians(turn)), math.sin(math.radians(turn))

        def turned(q, cx=cx, cy=cy, cs=cs, sn=sn):
            x, y = q.x - cx, q.y - cy
            return Vector((cx + x * cs - y * sn, cy + x * sn + y * cs, q.z))

        def slab(name, x, z, half, back, front, paint):
            # a petal is a square slab with its front edge cut: three rings of four, 20 triangles
            # (a full chamfered block is 56, and twelve petals would break the budget)
            rings = [rect_ring((x, cy + back, z), X, Z, half, half, 0.0),
                     rect_ring((x, cy + front + 0.004, z), X, Z, half, half, 0.0),
                     rect_ring((x, cy + front, z), X, Z, half - 0.004, half - 0.004, 0.0)]
            part.loft(name, "head", [[turned(q) for q in r] for r in rings], paint, caps=(False, True))

        for dx, dz in ((0, p), (0, -p), (p, 0), (-p, 0)):
            slab("petal", cx + dx, cz + dz, 0.5 * p, 0.006, -0.010, cotton)
        slab("flower-heart", cx, cz, 0.40 * p, 0.004, -0.016, heart)
    # the tassels: two woven cords of two lengths, each ending in a wider bob
    for name, top, foot, bob in (("tassel-1", (0.240, -0.126, 0.566), (0.243, -0.130, 0.504), (0.245, -0.131, 0.474)),
                                ("tassel-2", (0.244, -0.106, 0.566), (0.249, -0.108, 0.516), (0.251, -0.108, 0.490))):
        part.wedge(name, "head", top, foot, (0.0045, 0.0045), (0.0045, 0.0045), heart, 0.0)
        part.wedge(name + "-end", "head", foot, bob, (0.0085, 0.0085), (0.0055, 0.0055), heart, 0.0)


# ---------------------------------------------------------------------------
# THE TORSO AND THE ROBE. One block, a dark teal base, a little wider at the shoulders.
# ---------------------------------------------------------------------------
TORSO_Z = (0.176, 0.343)
TORSO_LOW = (0.108, 0.082)     # half width, half depth at the hips (the original's is 0.116 by 0.086, tapered)
TORSO_HIGH = (0.120, 0.090)    # at the shoulders
TORSO_CY = 0.002
ARM_Y, ARM_Z = 0.0173, 0.288


def torso_half(z):
    t = (z - TORSO_Z[0]) / (TORSO_Z[1] - TORSO_Z[0])
    return (TORSO_LOW[0] + (TORSO_HIGH[0] - TORSO_LOW[0]) * t, TORSO_LOW[1] + (TORSO_HIGH[1] - TORSO_LOW[1]) * t)


def chest_y(z):
    return TORSO_CY - torso_half(z)[1]


# THE COAT'S SKIRT: one piece round the hips, open at the front over her shorts (the original's
# open coat), flaring to a gold hem band, its two front edges piped in gold. All geometry.
SKIRT_TOP = 0.206
SKIRT_FOLLOW = 0.78                    # how much of a leg's swing the hem takes
SKIRT_TOP_R = (0.116, 0.092, 0.092)    # half width, depth to the front, depth to the back
SKIRT_HEM_R = (0.150, 0.118, 0.114)    # the original's hem: |x| 0.150, y -0.110 to 0.106
SKIRT_MID = 0.5 * (SKIRT_TOP + tex.HEM + 0.016)


def skirt_f(z):
    """How far down the flare a height is: 0 at the belt, 1 at the hem (a bell, not a cone)."""
    return _table([(tex.HEM, 1.0), (tex.HEM + 0.016, 0.985), (SKIRT_MID, 0.60), (SKIRT_TOP, 0.0)], z)


def skirt_front_y(z):
    f = skirt_f(z)
    return TORSO_CY - (SKIRT_TOP_R[1] + (SKIRT_HEM_R[1] - SKIRT_TOP_R[1]) * f)


def skirt_weights(co):
    down = _ramp(SKIRT_TOP, 0.098, co.z) ** 0.8
    leg = SKIRT_FOLLOW * down
    left = _smooth(_ramp(-0.070, 0.070, co.x))
    return [("torso", 1.0 - leg), ("leg-left", leg * left), ("leg-right", leg * (1.0 - left))]


# THE CAPELET. On the original it is a slab under the chin (|x| 0.152, |y| 0.107, 0.290 to
# 0.350), a gold edge round its foot, two points falling in front either side of a gold brooch,
# and a drop behind to 0.253 that carries the kasikus. Here it is ONE lofted piece with those
# same measures. It lies across three bones, and each was decided and then tested:
#   THE HEAD. Its top is at 0.345 (0.349 on the shoulders) and the head block's flat foot is at
#   0.343 and wider than the capelet, so the capelet is under the box and closes the neck: the head turns OVER it and cannot turn through it, at
#   any yaw. It takes no weight from the head bone. (A weight was considered so the brooch would
#   dip with the chin on a nod; with linear skinning the same weight drags the front of the
#   capelet sideways through itself on a turn. A nod of 22 degrees lays the chin over the
#   brooch instead, as the original's does.)
#   THE ARMS. Its two shoulders lie on the sleeves. Rigid to the torso, a sleeve dropped 45
#   degrees in `idle` lifts its shoulder corner straight through the cloth. So the cloth over
#   the line of each arm takes that arm bone, fully by |x| 0.102, and the front and back of each
#   shoulder take only a fifth of it (`CAPE_SIDE`): an arm swung forward would otherwise carry
#   the capelet's front corner up into the cheek.
#   THE TORSO has the rest: the neck, the front with its points and the brooch, the back.
CAPE_TOP = 0.345
CAPE_R = (0.155, 0.111, 0.113)      # half width, depth to the front, depth to the back
CAPE_SIDE = 0.78                    # how much of the arm's pull is given up toward the front and back edges
CAPE_HEM_FRONT = [(0.0, 0.303), (0.012, 0.300), (0.030, 0.287), (0.056, 0.274), (0.085, 0.286), (0.112, 0.292), (0.2, 0.292)]
CAPE_HEM_BACK = [(0.0, 0.251), (0.050, 0.252), (0.085, 0.258), (0.112, 0.270), (0.135, 0.283), (0.2, 0.288)]


def cape_hem(x, y):
    back = _smooth(_ramp(-0.050, 0.050, y))
    return _table(CAPE_HEM_FRONT, abs(x)) * (1.0 - back) + _table(CAPE_HEM_BACK, abs(x)) * back


def cape_weights(co):
    side = _smooth(_ramp(0.062, 0.102, abs(co.x)))
    # a long, even fall-off: a short one (45 to 100 mm) sheared the shoulder into a crumpled facet
    edge = 1.0 - CAPE_SIDE * _smooth(_ramp(0.015, 0.110, abs(co.y - ARM_Y)))
    w = side * edge
    return [("torso", 1.0 - w), ("arm-left" if co.x > 0 else "arm-right", w)]


def build_torso(part):
    cloth = proj("torso")
    part.block("torso", "torso", X, Y, [((0, TORSO_CY, TORSO_Z[0]), *TORSO_LOW), ((0, TORSO_CY, TORSO_Z[1]), *TORSO_HIGH)],
               proj_except("torso", 2, "lining"), 0.014)
    # her neck, for when the head tips back and the chin lifts off the capelet
    neck = part.block("neck", "torso", X, Y, [((0, TORSO_CY, 0.326), 0.050, 0.048), ((0, TORSO_CY, 0.354), 0.048, 0.046)],
                      flat("skin_shade"), 0.008)
    part.bend(neck, lambda co: [("torso", 1.0 - 0.7 * _ramp(0.332, 0.350, co.z)), ("head", 0.7 * _ramp(0.332, 0.350, co.z))])

    # THE BELT: two rust tiers 8 mm proud with the dark seam sunk between them (the original's
    # belt-lower, belt-seam, belt-upper)
    for lo, hi in (tex.BELT_LOW, tex.BELT_HIGH):
        (wl, dl), (wh, dh) = torso_half(lo), torso_half(hi)
        part.block("belt-tier", "torso", X, Y, [((0, TORSO_CY, lo), wl + 0.008, dl + 0.008), ((0, TORSO_CY, hi), wh + 0.008, dh + 0.008)],
                   proj_except("torso", 2, "rust_dark"), 0.004)
    zs = 0.5 * (tex.BELT_LOW[1] + tex.BELT_HIGH[0])
    ws, ds = torso_half(zs)
    part.block("belt-seam", "torso", X, Y, [((0, TORSO_CY, tex.BELT_LOW[1] - 0.002), ws + 0.004, ds + 0.004),
                                            ((0, TORSO_CY, tex.BELT_HIGH[0] + 0.002), ws + 0.004, ds + 0.004)], flat("seam"), 0.0)
    belt_front = chest_y(zs) - 0.008
    ahead = (0, -1, 0)

    # THE MEDALLION, her one big fastening (clothing rule 4): the gold plate 72 by 45 mm and its
    # pale teal stone, the original's sizes. The stone is cut as a diamond, the kasikus's own
    # shape, and the plate is engraved with one more diamond round it.
    part.block("medallion", "torso", X, Z, [((0, belt_front + 0.004, 0.2145), 0.036, 0.0225), ((0, belt_front - 0.012, 0.2145), 0.036, 0.0225)],
               panel("medallion", 0, 2, (-0.036, 0.036, 0.192, 0.237), ahead, "gold_dark"), 0.006)
    du, dv = (X + Z).normalized(), (Z - X).normalized()
    part.block("medallion-stone", "torso", du, dv, [((0, belt_front - 0.010, 0.2145), 0.0115, 0.0115), ((0, belt_front - 0.020, 0.2145), 0.0075, 0.0075)],
               facing(ahead, "gem", "gem_dark"), 0.002)

    # the sash knot on her left hip and the small knot on her right, cream on the belt
    knot = tones("cream_lit", "cream", "cream_shade")
    part.block("sash-knot", "torso", X, Z, [((0.085, belt_front + 0.004, 0.209), 0.027, 0.0175), ((0.085, belt_front - 0.011, 0.209), 0.024, 0.0150)],
               knot, 0.006)
    part.block("hip-knot", "torso", X, Z, [((-0.089, belt_front + 0.004, 0.209), 0.019, 0.0165), ((-0.089, belt_front - 0.010, 0.209), 0.016, 0.0140)],
               knot, 0.006)

    # THE LAPELS: cream abel panels on the chest, each piped gold down BOTH edges (clothing rule
    # 3: gold as geometry, 14 mm wide, standing proud)
    gold = swatch("gold", (0.0, 1.0), (0.05, 0.95))
    for s in (1, -1):
        part.block("lapel", "torso", X, Y, [((s * 0.085, chest_y(0.232) - 0.002, 0.232), 0.027, 0.005), ((s * 0.085, chest_y(0.294) - 0.002, 0.294), 0.027, 0.005)],
                   panel("lapel", 0, 2, (s * 0.058, s * 0.112, 0.232, 0.294), ahead, "cream_shade"), 0.0)
        for name, x in (("lapel-trim-outer", 0.117), ("lapel-trim-inner", 0.055)):
            part.block(name, "torso", X, Y, [((s * x, chest_y(0.232) - 0.003, 0.232), 0.007, 0.0065), ((s * x, chest_y(0.296) - 0.003, 0.296), 0.007, 0.0065)],
                       gold, 0.0)

    # THE COAT'S SKIRT. Its top ring is the torso's; toward the hem each side takes up to
    # SKIRT_FOLLOW of its own leg, and across the middle the two legs are mixed, so the back
    # panel twists between them as she strides (rule 7).
    gold_run = swatch("gold", (0.0, 8.0), (0.05, 0.95), "columns")
    gold_edge = swatch("gold", (0.0, 1.0), (0.05, 0.95), "rings")
    #   degrees round from straight ahead, her right first; True where the piped edge stands proud
    rounds = [(2.6, True), (5.5, True), (5.5, False), (12, False), (24, False), (45, False), (66, False), (90, False), (114, False),
              (135, False), (156, False), (172, False), (180, False), (188, False), (204, False), (225, False), (246, False),
              (270, False), (294, False), (315, False), (336, False), (348, False), (354.5, False), (354.5, True), (357.4, True)]
    sections, params = [], []
    for theta, proud in rounds:
        a = math.radians(theta)
        px = -math.copysign(abs(math.sin(a)) ** 0.4, math.sin(a))     # her right first, then behind
        py = -math.copysign(abs(math.cos(a)) ** 0.4, math.cos(a))     # -1 in front, +1 behind
        lift = -0.004 if proud else 0.0

        def at(z, inset, px=px, py=py, lift=lift):
            f = skirt_f(z)
            r = [SKIRT_TOP_R[k] + (SKIRT_HEM_R[k] - SKIRT_TOP_R[k]) * f - inset - lift for k in range(3)]
            return Vector((px * r[0], TORSO_CY + py * (r[2] if py >= 0 else r[1]), z))

        hem = tex.HEM
        sections.append([at(SKIRT_TOP, 0.0), at(SKIRT_MID, 0.0), at(hem + 0.016, 0.0), at(hem + 0.0165, -0.004),
                         at(hem, -0.004), at(hem + 0.003, 0.008), at(SKIRT_MID, 0.009), at(SKIRT_TOP, 0.009)])
        params.append(theta / 360.0)
    last = len(rounds) - 2

    def skirt_paint(i, j):
        if j in (2, 3, 4):
            return gold_run
        if j >= 5:
            return flat("lining")
        return gold_edge if i <= 1 or i >= last - 1 else cloth

    skirt = part.loft("coat-skirt", "torso", sections, skirt_paint, params=params)
    part.bend(skirt, skirt_weights)

    # THE SASH: the patterned strip hanging from the knot on her left, lying on the skirt's front
    # and bending with it (it is tied at the belt and rides the left leg at its foot)
    stations = []
    for z, half in ((0.202, 0.021), (0.160, 0.0205), (0.126, 0.0195), (0.100, 0.0185)):
        stations.append(((0.085, skirt_front_y(z) - 0.0035, z), half, 0.0045))
    sash = part.block("sash", "torso", X, Y, stations, panel("sash", 0, 2, (0.064, 0.106, 0.098, 0.202), ahead, "cream_shade"), 0.0015)
    part.bend(sash, skirt_weights)

    # THE CAPELET (see the note above CAPE_TOP)
    def ax(x):
        return math.degrees(math.asin((x / CAPE_R[0]) ** 2.5))

    half_turn = [0.0, ax(0.012), ax(0.030), ax(0.056), ax(0.085), ax(0.112), 36.0, 45.0, 56.0, 70.0, 80.0, 90.0, 100.0, 110.0, 124.0, 135.0, 144.0,
                 180.0 - ax(0.112), 180.0 - ax(0.085), 180.0 - ax(0.050), 180.0 - ax(0.020)]
    turns = half_turn + [180.0] + [360.0 - t for t in reversed(half_turn)]
    sections, params = [], []
    for theta in turns:
        a = math.radians(theta)
        sn, cs = math.sin(a), math.cos(a)
        px = -math.copysign(abs(sn) ** 0.4, sn)
        py = -math.copysign(abs(cs) ** 0.4, cs)

        def at(k, z=None, drop=None, px=px, py=py):
            x = px * CAPE_R[0] * k
            y = TORSO_CY + py * (CAPE_R[2] if py >= 0 else CAPE_R[1]) * k
            return Vector((x, y, z if z is not None else cape_hem(px * CAPE_R[0], y) + drop))

        sections.append([at(0.30, CAPE_TOP - 0.002), at(0.68, CAPE_TOP + 0.004), at(0.93, CAPE_TOP), at(1.00, CAPE_TOP - 0.008),
                         at(1.02, drop=0.0125), at(1.05, drop=0.0130), at(1.05, drop=0.0), at(0.95, drop=0.002),
                         at(0.86, CAPE_TOP - 0.016)])
        params.append(theta / 360.0)
    cape_cloth = proj("cape")
    cape_gold = swatch("gold", (0.0, 9.0), (0.05, 0.95), "columns")

    def cape_paint(i, j):
        return (cape_cloth, cape_cloth, cape_cloth, cape_cloth, cape_gold, cape_gold, flat("gold_dark"), flat("cape_in"), flat("cape_in"))[j]

    cape = part.loft("capelet", "torso", sections, cape_paint, caps=(False, False), params=params)
    part.bend(cape, cape_weights)

    # THE BROOCH: a gold square with a cream heart, where the two points meet under her chin
    front = TORSO_CY - CAPE_R[1] * 1.02
    part.block("brooch", "torso", X, Z, [((0, front + 0.004, 0.310), 0.023, 0.018), ((0, front - 0.011, 0.310), 0.023, 0.018)],
               panel("brooch", 0, 2, (-0.023, 0.023, 0.292, 0.328), ahead, "gold_dark"), 0.005)
    part.block("brooch-core", "torso", X, Z, [((0, front - 0.009, 0.310), 0.0115, 0.0095), ((0, front - 0.017, 0.310), 0.0095, 0.0075)],
               facing(ahead, "cream", "cream_shade"), 0.002)


# ---------------------------------------------------------------------------
# THE ARMS, straight out as the rig rests. `s` is +1 for her left, -1 for her right.
# The original's sleeve is a box 140 mm tall from the shoulder. Here it is a BELL: narrow where
# the capelet lies on it (so the capelet can sit low on the shoulder, under the head) and wide
# at the cuff, which keeps the original's size (152 mm tall, 148 mm deep).
# ---------------------------------------------------------------------------

def arm_block(part, name, bone, s, stations, mapping, chamfer):
    return part.block(name, bone, Y, Z, [((s * x, ARM_Y, ARM_Z), h[0], h[1]) for x, h in stations], mapping, chamfer)


def build_arm(part, s):
    bone, fore, group = ("arm-left", "forearm-left", "armL") if s > 0 else ("arm-right", "forearm-right", "armR")
    arm_block(part, "sleeve", bone, s, [(0.098, (0.046, 0.046)), (0.150, (0.054, 0.055)), (0.184, (0.066, 0.068))],
              proj_except(group, 0, "cape_in"), 0.011)
    # the cuff: a gold edge 5 mm proud of the sleeve, then the cream band, wider still; its end
    # face is the dark inside of the sleeve
    arm_block(part, "cuff-gold", bone, s, [(tex.CUFF_GOLD[0], (0.0655, 0.0670)), (tex.CUFF_GOLD[1] + 0.001, (0.0690, 0.0710))],
              proj_except(group, 0, "gold_dark"), 0.003)
    arm_block(part, "cuff", bone, s, [(tex.CUFF[0], (0.0700, 0.0720)), (tex.CUFF[1], (0.0740, 0.0770))],
              proj_except(group, 0, "lining"), 0.006)
    # the bare forearm, on the elbow bone, starting inside the cuff: a flat tone (rule 4)
    skin = tones("skin_lit", "skin_tone", "skin_shade")
    arm_block(part, "forearm", fore, s, [(0.172, (0.042, 0.045)), (0.230, (0.040, 0.043))], skin, 0.009)
    # the hand: a plain block, the cast's, its fingers and thumb drawn (the original's hand is
    # 124 mm tall; its top is where a carried tsinelas rests)
    arm_block(part, "hand", fore, s, [(tex.WRIST - 0.002, (0.050, 0.060)), (0.275, (0.048, 0.060))], proj(group), 0.013)


# ---------------------------------------------------------------------------
# THE LEGS. Short, the cast's 24 per cent: dark teal shorts up under the coat, bare legs, bare
# feet on thick cream sandals with one rust strap. Feet turned out five degrees.
# ---------------------------------------------------------------------------
LEG_X = 0.0836
TOE_OUT = 5.0


def build_leg(part, s):
    bone, group = ("leg-left", "legL") if s > 0 else ("leg-right", "legR")
    cloth = proj(group)
    # the shorts narrow and lean IN toward the hip, so they stay inside the skirt's wall
    part.block("shorts", bone, X, Y, [((s * LEG_X, 0.0, tex.SHORTS[0]), 0.057, 0.064), ((s * (LEG_X - 0.008), 0.0, 0.198), 0.051, 0.064)],
               proj_except(group, 2, "lining"), 0.009)
    part.block("leg", bone, X, Y, [((s * LEG_X, 0.0, 0.050), 0.043, 0.047), ((s * LEG_X, 0.0, 0.120), 0.047, 0.052)],
               proj_except(group, 2, "skin_shade"), 0.010)

    turn = math.radians(TOE_OUT) * s
    cs, sn = math.cos(turn), math.sin(turn)

    def turned(p):
        x, y = p.x - s * LEG_X, p.y - 0.02
        return Vector((s * LEG_X + x * cs + y * sn, 0.02 - x * sn + y * cs, p.z))

    def station(y, half, z0, z1, cut):
        return [turned(p) for p in rect_ring((s * LEG_X, y, 0.5 * (z0 + z1)), X, Z, half, 0.5 * (z1 - z0), cut)]

    # the bare foot: low at the toes, rising to the ankle (the original's is 0.026 to 0.058)
    foot = [(-0.122, 0.044, 0.024, 0.038, 0.005), (-0.112, 0.055, 0.024, 0.046, 0.009), (-0.070, 0.058, 0.024, 0.054, 0.011),
            (-0.020, 0.056, 0.024, 0.062, 0.011), (0.052, 0.052, 0.024, 0.062, 0.011), (0.062, 0.044, 0.026, 0.054, 0.005)]
    part.loft("foot", bone, [station(*row) for row in foot], cloth)
    # the strap: a rust band over the foot, 4 mm proud
    rust = tones("rust_lit", "rust", "rust_dark")
    part.block("sandal-strap", bone, X, Z, [((s * LEG_X, tex.STRAP[0], 0.037), 0.062, 0.016), ((s * LEG_X, tex.STRAP[1], 0.0395), 0.063, 0.0185)],
               rust, 0.004, move=turned)
    # the thick sole, wider than the foot all round (the original's is 150 by 218 mm, 26 thick)
    h = 0.5 * tex.SOLE_TOP
    part.block("sandal-sole", bone, X, Z, [((s * LEG_X, -0.136, h), 0.056, h), ((s * LEG_X, -0.094, h), 0.074, h),
                                          ((s * LEG_X, 0.000, h), 0.074, h), ((s * LEG_X, 0.084, h), 0.064, h)],
               proj_except(group, 2, "cream_shade"), 0.005, move=turned)


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
    """team-amihan.glb with its meshes swapped: skeleton, bind matrices and clips copied untouched,
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (amihan)"}
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
    print("triangles %d  (budget %d, the original is 5986)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if abs(hi.z - HEIGHT) > 0.004:
        raise SystemExit("the top is at %.4f, not the %.3f a head at %.2f gives" % (hi.z, HEIGHT, HEAD_SCALE))
    if tris > TRIANGLE_BUDGET:
        raise SystemExit("%d triangles is over the budget of %d" % (tris, TRIANGLE_BUDGET))
    # CharacterVisual.PalmCentre: the arm must run along x, and the hand's top must sit where a
    # carried tsinelas is parked (bone-space +0.0555, with HandTopLift 0.0617 over the palm).
    # Only vertices wholly on the arm are measured: the capelet's shoulder is part arm, part torso.
    body = objects[0]
    group = (body.vertex_groups["arm-right"].index, body.vertex_groups["forearm-right"].index)
    arm = [v.co for v in body.data.vertices if len(v.groups) == 1 and v.groups[0].group in group]
    size = [max(p[a] for p in arm) - min(p[a] for p in arm) for a in range(3)]
    if not (size[0] > size[1] and size[0] > size[2]):
        raise SystemExit("arm-right no longer runs along x: %s" % size)
    far = min(p.x for p in arm)
    hand = [p for p in arm if p.x < far + size[0] / 8.0]
    top = max(p.z for p in hand) - ARM_Z
    print("hand top %.4f above the arm bone (the original's is 0.062)" % top)
    if not 0.045 <= top <= 0.066:
        raise SystemExit("the hand's top moved; a carried tsinelas would float or sink")


def build(armature, material):
    body, head = Part("body-mesh"), Part("head-mesh")
    build_torso(body)
    build_arm(body, 1)
    build_arm(body, -1)
    build_leg(body, 1)
    build_leg(body, -1)
    build_head(head)
    build_hair(head)
    build_flowers(head)
    objects = []
    for part in (body, head):
        part.resolve_uvs()
        # AFTER the paint is placed (it is projected from the full-size shape): the head comes in.
        # A vertex wholly on the head bone is scaled by HEAD_SCALE about the head joint; a vertex
        # that shares the head with the torso (the nape tips, the top of the neck) by the same
        # share, so nothing tears where the two meet. The capelet takes no head weight and is not
        # moved: its top (0.345 to 0.349) still lies above the head's foot (0.343) and under the
        # whole of it, so the neck stays closed, and the head's closing ring (0.343 to 0.348 now)
        # stays below the capelet's shoulder line.
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
        print(part.name, " ".join("%s:%d" % (n, t) for n, t in merged.items()))
    verify(objects)
    return objects


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
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_amihan_textures.py")
    image = bpy.data.images.load(image_path)
    material = bpy.data.materials.new(NAME)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    node = nodes.new("ShaderNodeTexImage")
    node.image = image
    material.node_tree.links.new(node.outputs[0], nodes["Principled BSDF"].inputs["Base Color"])
    nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0

    objects = build(armature, material)
    write_glb(mesh_arrays(objects[0]), mesh_arrays(objects[1]), out or OUT)
    print("wrote " + (out or OUT))
    if out is not None:
        return
    os.makedirs(os.path.dirname(BLEND), exist_ok=True)
    image.filepath = bpy.path.relpath(image_path, start=os.path.dirname(BLEND))
    bpy.context.preferences.filepaths.save_version = 0   # no .blend1 beside it
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)
    print("wrote " + BLEND)


if __name__ == "__main__":
    main()
