"""Build the Paete redesign PROTOTYPE: the same tree guardian, every plank cut and painted.

    py -3 tools/author_character_redesign_paete_textures.py
    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/author_character_redesign_paete.py

Writes
    Assets/TumbangPreso/Art/CharacterRedesign/paete/paete-redesign.glb
    ArtSource/paete/redesign-20261005/paete_redesign.blend
beside the atlas the .glb names.

WHY. docs/CHARACTER_REDESIGN_DANTE.md sections 13 and 15. Paete was held back as finalized and
protected; the owner lifted that for a prototype only. This is his own copy of the model script,
rewritten for him; it imports nothing of another hero's.

⚠️ A PROTOTYPE. NOTHING IN THE GAME LOADS IT. The folder is outside Resources, no roster row
points here, and team-paete.glb, build_paete_voxel.py, person_paete.asset, characters/paete-motion
and every runtime file are not touched.

⚠️⚠️ THE VINE TIPS, FOR WHOEVER WIRES THE ELBOW INTO THE GAME. `Runtime/Visual/PaeteVfx.cs`
(`PaeteVineReach.Hand`, and the palm glow) finds `arm-left` / `arm-right` and takes the point
0.47 m out along the bone's local x as the place the vines leave from. In the rest pose that is
    his left   (+0.620, 0.470, -0.01725)  in the file's glTF space (+y up, +z the way he faces)
    his right  (-0.620, 0.470, -0.01725)
and the four points of each braid are kept where the original has them (`verify` fails the build
if a tip moves more than 5 mm): they end at x 0.612 to 0.632, heights 0.448 to 0.494. On this rig
the braid rides `forearm-left` / `forearm-right`, whose joints sit at (+-0.360, 0.470, 0.0), so
the same place is (+-0.260, 0.0, -0.01725) in the FOREARM bone's space. With a bent elbow
the arm bone's 0.47 m point is no longer in the braid; the runtime must be pointed at the forearm
bone with that offset. No runtime code is changed here.

WHAT IS KEPT, row for row from tools/build_paete_voxel.py (its tables are retyped below in ITS
table space, x his left, y up, z negative toward the face, so a number here can be held against a
number there):
  * the rig: the .glb is team-paete.glb with its two meshes replaced and two elbow bones appended
    after the seven; skeleton, bind matrices and every other clip are copied across;
  * his own long-limbed proportions (hips 0.260, shoulders 0.470, neck 0.495, crown 0.668), feet
    on zero, facing -y in Blender;
  * every piece: the plank mask of a head with its two branch antlers and crown leaves, the trunk
    of planks round a dark core, the two chest slabs in a V round the diamond boss, the layered
    slanted shoulder planks, the plank-bundle upper arms and legs, the knee plates, the tassets,
    root feet with four roots each, the moss clumps, the leaf clusters (each shoulder, the collar,
    the left hip), the thick vine across the chest, the vine up his right leg, the little branch
    on his back, and THE BRAIDED VINE FOREARMS, four strands and a tendril an arm, on the
    original's own typed paths.

WHAT CHANGES:
  * every plank is a CUT plank: its long edges chamfered, most of them tapered or leaning a
    little, each its own; the chamfer is one flat lighter tone, the sawn end one flat darker one;
  * every plank face shows its own piece of painted grain (the textures script);
  * the braid strands are the same paths drawn through twice as many rings, so they curve instead
    of kinking, they end in true points, and each wears fibres that run its length;
  * leaves are a veined top over a keel instead of a flat prism (fewer triangles, more leaf);
  * HIS HEAD KEEPS ITS OWN SILHOUETTE, a tall narrow plank mask, not Dante's rounded head;
  * two exceptions to the cast's rules, both the owner's yes on 2026-10-05 after seeing v04:
    HIS HEAD IS NOT SCALED (`HEAD_SCALE` 1.0; it was already small for his body and at 0.84 he
    looked pin-headed), and HIS FACE IS NOT A FLAT PLANE (he is a carved mask, not a kid's face:
    the brow, cheek, nose-seam and forehead planks are real blocks standing proud of a dark core,
    at the original's positions, tilts and depths, so the eyes sit as slits under the brow's
    ledge again, at the original's measured size);
  * two elbows, at the elbow block, the braid skinned to the forearm bone;
  * the tassets bend toward the thigh they hang over.
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
import author_character_redesign_paete_textures as tex  # noqa: E402  the island layout
import author_character_redesign_paete_clips as clips  # noqa: E402  the prototype's own locomotion clips

BASE = os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons/team-paete.glb")
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/paete")
OUT = os.path.join(FOLDER, "paete-redesign.glb")
BLEND = os.path.join(ROOT, "ArtSource/paete/redesign-20261005/paete_redesign.blend")
NAME = "paete-redesign"

BONES = ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]
# Two bones the cast does not have: an elbow under each arm bone, appended AFTER the seven so
# their indices and names do not move. HIS ELBOW IS AT 0.360: the original's braid starts there,
# inside the elbow block (0.326 to 0.376), which stays on the arm bone and hides the joint.
ELBOW_X = 0.360
ARM_Y, ARM_Z = 0.0, 0.470
EXTRA_BONES = [("forearm-left", "arm-left", 1), ("forearm-right", "arm-right", -1)]
ALL_BONES = BONES + [name for name, _, _ in EXTRA_BONES]
# ⚠️ 1.0 FOR HIM, NOT THE CAST'S 0.84. Owner, 2026-10-05, on v04: the head goes back to full size.
# The cast's heads are 53 per cent of their height and came in; his is a narrow mask on a body
# 0.57 wide at the shoulders and at 0.84 he looked pin-headed. The mechanism is left in place.
HEAD_SCALE = 1.0
HEAD_JOINT = Vector((0.0, 0.00236, 0.495))   # the `head` bone of team-paete.glb, in Blender space
FULL_TOP = 0.7912        # the original's top, antler tips included (team-paete.glb head-mesh bounds)
HEIGHT = HEAD_JOINT.z + (FULL_TOP - HEAD_JOINT.z) * HEAD_SCALE   # the original's own top while the scale is 1.0
SHARP_ANGLE = 40.0
TRIANGLE_BUDGET = 6000   # the original is 9,892
# where each braid's four points end in the original (table space), for `verify`
TIPS = {1: [(0.630, 0.478, 0.004), (0.624, 0.456, 0.008), (0.612, 0.448, 0.020), (0.616, 0.484, -0.020)],
        -1: [(-0.632, 0.458, -0.008), (-0.626, 0.480, 0.010), (-0.610, 0.494, -0.018), (-0.614, 0.456, 0.022)]}

X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
# a TABLE axis as a Blender direction: table y is up, table z runs from the face (-) to the back
T_AXIS = {"x": X, "y": Z, "z": Y}

# the builder's palette slots, as the names the rows use
MOSS, MOSS_DARK, LEAF, LEAF_DARK, VINE, ROOT_T, MOSS_LIT = "moss", "moss_dark", "leaf", "leaf_dark", "vine", "root", "moss_lit"
BARK, BARK_DARK, BARK_LIT = "bark", "bark_dark", "bark_lit"
#   tone -> (the wood swatch its faces take a window of, its chamfer tone, its sawn-end tone)
WOOD = {BARK: ("wood", "bark_edge_tone", "bark_end_tone"),
        BARK_LIT: ("wood_lit", "lit_edge_tone", "lit_end_tone"),
        ROOT_T: ("wood_root", "root_edge_tone", "root_dark_tone")}
#   moss: (faces turned up, sides, under)
MOSS_TONES = {MOSS: ("moss_lit_tone", "moss_tone", "moss_dark_tone"),
              MOSS_LIT: ("moss_lit_tone", "moss_lit_tone", "moss_tone"),
              MOSS_DARK: ("moss_tone", "moss_dark_tone", "moss_dark_tone")}


def B(p):
    """A point of the builder's table space in Blender's."""
    return Vector((p[0], p[2], p[1]))


def _pair(h):
    return h if isinstance(h, tuple) else (h, h)


def rect_ring(centre, u, v, hu, hv, chamfer):
    """A rectangle `hu` by `hv` (a half size, or a (negative, positive) pair) with each corner
    cut back `chamfer`: eight points, or the four plain corners when the chamfer is zero."""
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


def proj_facing(group, direction, name):
    """The group's island for the faces that SQUARELY face `direction`, one flat tone for the rest
    (section 15.3 rule 8: a chamfer at 45 degrees takes a tone, never a smear of the drawing)."""
    return ("facing", group, direction, name)


def tones(top, side, under):
    """No drawing at all: one flat tone for faces turned up, one for the sides, one for under."""
    return ("tones", top, side, under)


def _frac(x):
    return x - math.floor(x)


def _smooth(path, radii, steps):
    """A Catmull-Rom curve through typed points (and their radii), `steps` pieces a span, so a
    strand curves through the original's own points instead of kinking at them."""
    pts = [Vector(p) for p in path]
    n = len(pts)
    out_p, out_r = [], []
    for i in range(n - 1):
        p0, p1, p2, p3 = pts[max(i - 1, 0)], pts[i], pts[i + 1], pts[min(i + 2, n - 1)]
        for k in range(steps):
            t = k / float(steps)
            t2, t3 = t * t, t * t * t
            out_p.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
            out_r.append(radii[i] + (radii[i + 1] - radii[i]) * t)
    out_p.append(pts[-1]); out_r.append(radii[-1])
    return out_p, out_r


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

    def _vert(self, p, bone):
        v = self.bm.verts.new(p)
        v[self.bone] = ALL_BONES.index(bone)
        return v

    def _done(self, name, faces):
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self.pieces.append((name, sum(len(f.verts) - 2 for f in faces)))
        return faces

    def loft(self, name, bone, rings, mapping, caps=(True, True), tip=None, params=None):
        """Skin `rings` in order. `mapping` is one spec or f(segment, column) -> spec."""
        bm = self.bm
        n = len(rings[0])
        count = len(rings)
        if params is None:
            params = [i / float(max(count - 1, 1)) for i in range(count)]
        widest = max(rings, key=lambda r: sum((r[j] - r[(j + 1) % n]).length for j in range(n)))
        run, across = 0.0, [0.0]
        for j in range(n):
            run += (widest[j] - widest[(j + 1) % n]).length
            across.append(run)
        across = [a / (run or 1.0) for a in across]
        rows = [[self._vert(p, bone) for p in r] for r in rings]
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
            centre = self._vert(at, bone)
            for j in range(n):
                k = (j + 1) % n
                tri = (row[j], row[k], centre)
                f = bm.faces.new(tri)
                uvp = [(across[j], params[i]), (across[j + 1], params[i]), (0.5 * (across[j] + across[j + 1]), v_at)]
                self.jobs.append((f, spec(seg, j), uvp, tri))
                faces.append(f)

        def lid(row, seg):
            f = bm.faces.new(row)
            self.jobs.append((f, spec(seg, 0), [(0.5, 0.5)] * len(row), tuple(row)))
            faces.append(f)

        if caps[0] == "lid":
            lid(rows[0], 0)
        elif caps[0]:
            fan(rows[0], 0, sum(rings[0], Vector()) / n, 0, params[0])
        if tip is not None:
            fan(rows[-1], count - 1, Vector(tip), count - 2, 1.0)
        elif caps[1] == "lid":
            lid(rows[-1], count - 2)
        elif caps[1]:
            fan(rows[-1], count - 1, sum(rings[-1], Vector()) / n, count - 2, params[-1])
        return self._done(name, faces)

    def plank(self, name, bone, lo, hi, tone, grain="y", tilt=0.0, low=(1.0, 1.0), high=(1.0, 1.0),
              chamfer=0.005, lean=(0.0, 0.0), art=None, caps=(True, True), bevel=(0.0, 0.0), ends=None, edge=None):
        """One of his planks, from the original's own box (`lo`, `hi` in TABLE space).

        `grain` is the table axis the wood runs along. `low` and `high` scale the plank's two
        across sizes at its two ends (a taper), `lean` slides the far end across (a slanted cut),
        `tilt` turns it about the front axis as the builder's BOX_TILTS does (a positive angle
        lifts the +x end), `bevel` cuts the two ends back. `art` = (swatch, "front" or "back")
        lays an engraved drawing on that face, by position. A face of the plank takes its own
        window of the tone's wood swatch; chamfers and ends take one flat tone each. `ends`
        names a flat tone for the two end faces of a dark core block that can be seen end on,
        `edge` one for the chamfers in place of the tone's lighter worn edge.
        """
        bm = self.bm
        centre = B(tuple(0.5 * (lo[a] + hi[a]) for a in range(3)))
        half = {"x": 0.5 * (hi[0] - lo[0]), "y": 0.5 * (hi[1] - lo[1]), "z": 0.5 * (hi[2] - lo[2])}
        a_name, b_name = {"y": ("x", "z"), "x": ("z", "y"), "z": ("x", "y")}[grain]
        L, A, Bv = T_AXIS[grain], T_AXIS[a_name], T_AXIS[b_name]
        hl, ha, hb = half[grain], half[a_name], half[b_name]

        stations = []   # (centre, half a, half b, chamfer, is a main ring)
        if bevel[0] > 0.0:
            stations.append((centre - L * hl, max(ha * low[0] - bevel[0], 0.002), max(hb * low[1] - bevel[0], 0.002), chamfer * 0.5, False))
        stations.append((centre - L * (hl - bevel[0]), ha * low[0], hb * low[1], chamfer, True))
        end = centre + A * lean[0] + Bv * lean[1]
        stations.append((end + L * (hl - bevel[1]), ha * high[0], hb * high[1], chamfer, True))
        if bevel[1] > 0.0:
            stations.append((end + L * hl, max(ha * high[0] - bevel[1], 0.002), max(hb * high[1] - bevel[1], 0.002), chamfer * 0.5, False))
        rings = [rect_ring(c, A, Bv, a, b, ch) for c, a, b, ch, _ in stations]
        n = len(rings[0])
        if any(len(r) != n for r in rings):
            raise SystemExit("plank %s: its rings do not match" % name)
        rows = [[self._vert(p, bone) for p in r] for r in rings]

        is_moss = tone in MOSS_TONES
        is_dark = tone == BARK_DARK
        if is_moss:
            body_spec = tones(*MOSS_TONES[tone])
        elif is_dark:
            body_spec = flat("core_tone")
        else:
            wood, edge_tone, end_tone = WOOD[tone]
            edge_tone = edge or edge_tone
            sw_w, sw_h = tex.SWATCHES[wood][2:4]
        seed = _frac(centre.x * 9.71 + centre.z * 5.33 + centre.y * 3.17)
        # which way each flat side of the ring looks
        outward = [A, Bv, -A, -Bv] if n == 4 else [A, None, Bv, None, -A, None, -Bv, None]
        want = {"front": -Y, "back": Y}.get(art[1]) if art else None
        art_spec = None if not art else (proj(art[0]) if art[0] in tex.GROUPS else swatch(art[0]))

        def art_uv(v):
            return ((v.co.x - lo[0]) / (hi[0] - lo[0]), (v.co.z - lo[1]) / (hi[1] - lo[1]))

        faces = []
        for i in range(len(rows) - 1):
            main = stations[i][4] and stations[i + 1][4]
            for j in range(n):
                k = (j + 1) % n
                quad = (rows[i][j], rows[i][k], rows[i + 1][k], rows[i + 1][j])
                f = bm.faces.new(quad)
                faces.append(f)
                if is_moss or is_dark:
                    self.jobs.append((f, body_spec, [(0.5, 0.5)] * 4, quad))
                elif not main or outward[j] is None:
                    self.jobs.append((f, flat(edge_tone), [(0.5, 0.5)] * 4, quad))
                elif want is not None and outward[j].dot(want) > 0.9:
                    self.jobs.append((f, art_spec, [art_uv(v) for v in quad], quad))
                else:
                    length = 2.0 * hl
                    width = (rows[i][j].co - rows[i][k].co).length
                    du, dv = min(length / sw_w, 1.0), min(width / sw_h, 1.0)
                    u0 = _frac(seed + 0.37 * j) * (1.0 - du)
                    v0 = _frac(seed * 3.0 + 0.23 * j) * (1.0 - dv)
                    self.jobs.append((f, swatch(wood, (u0, u0 + du), (v0, v0 + dv)),
                                      [(0.0, 0.0), (0.0, 1.0), (1.0, 1.0), (1.0, 0.0)], quad))
        for want_cap, row in ((caps[0], rows[0]), (caps[1], rows[-1])):
            if not want_cap:
                continue
            f = bm.faces.new(row)
            faces.append(f)
            if is_moss and row is rows[-1] and grain == "y":
                uvp = [(min((v.co.x - lo[0]) / 0.11, 1.0), min((v.co.y - lo[2]) / 0.11, 1.0)) for v in row]
                self.jobs.append((f, swatch("moss_top"), uvp, tuple(row)))
            elif is_moss or is_dark:
                self.jobs.append((f, flat(ends) if ends else body_spec, [(0.5, 0.5)] * n, tuple(row)))
            else:
                self.jobs.append((f, flat(end_tone), [(0.5, 0.5)] * n, tuple(row)))
        if tilt:
            t = math.radians(tilt)
            cs, sn = math.cos(t), math.sin(t)
            for row in rows:
                for v in row:
                    d = v.co - centre
                    v.co = centre + Vector((d.x * cs - d.z * sn, d.y, d.x * sn + d.z * cs))
        return self._done(name, faces)

    def tube(self, name, bone, path, radii, mapping, sides=6, smooth=1, square=False, start_cap=False):
        """A strand, a vine, a root or an antler branch along typed TABLE points, one typed radius
        a point, ending in a POINT at the last one. `square` gives the four flat sides of a carved
        branch, one of them facing the front."""
        pts = [B(p) for p in path]
        rad = list(radii)
        if smooth > 1:
            pts, rad = _smooth(pts, rad, smooth)
        rings, params, run = [], [], 0.0
        total = sum((pts[i + 1] - pts[i]).length for i in range(len(pts) - 1))
        for i, c in enumerate(pts[:-1]):
            tangent = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
            # the section is CARRIED along the path, never rebuilt from a fixed helper: v01 to v03
            # picked the helper afresh at every ring, and a root that ran toward the camera
            # switched helpers between two rings and wrung itself into a black star
            if i == 0:
                helper = Y if abs(tangent.y) < 0.9 else Z
                normal = tangent.cross(helper).normalized()
            else:
                normal = (normal - tangent * normal.dot(tangent)).normalized()
            side = tangent.cross(normal).normalized()
            if square:
                r = rad[i] * 0.86
                rings.append([c + normal * (a * r) + side * (b * r) for a, b in ((1, -1), (1, 1), (-1, 1), (-1, -1))])
            else:
                rings.append([c + normal * (rad[i] * math.cos(j * math.tau / sides)) + side * (rad[i] * math.sin(j * math.tau / sides))
                              for j in range(sides)])
            params.append(run / total)
            run += (pts[i + 1] - c).length
        return self.loft(name, bone, rings, mapping, caps=(start_cap, False), tip=pts[-1], params=params)

    def leaf(self, name, bone, centre, length, width, thickness, yaw, pitch, roll, kind):
        """One leaf, in the original's place and at the original's three angles: a six-sided top
        that wears the veined drawing, over a keel that comes to a ridge point underneath."""
        def turn(p):
            x, y, z = p
            r = math.radians(roll)
            y, z = y * math.cos(r) - z * math.sin(r), y * math.sin(r) + z * math.cos(r)
            q = math.radians(pitch)
            x, y = x * math.cos(q) - y * math.sin(q), x * math.sin(q) + y * math.cos(q)
            w = math.radians(yaw)
            x, z = x * math.cos(w) + z * math.sin(w), -x * math.sin(w) + z * math.cos(w)
            return B((centre[0] + x, centre[1] + y, centre[2] + z))

        outline = [(-0.50, 0.0), (-0.22, 0.42), (0.18, 0.50), (0.50, 0.0), (0.18, -0.50), (-0.22, -0.42)]
        top = [self._vert(turn((a * length, 0.5 * thickness, b * width)), bone) for a, b in outline]
        keel = self._vert(turn((0.02 * length, -1.1 * thickness, 0.0)), bone)
        drawing = {LEAF: "leaf", LEAF_DARK: "leaf_dark", MOSS_LIT: "leaf_moss"}[kind]
        under = {LEAF: "leaf_dark_tone", LEAF_DARK: "leaf_under_tone", MOSS_LIT: "moss_tone"}[kind]
        faces = []
        f = self.bm.faces.new(top)
        self.jobs.append((f, swatch(drawing), [(a + 0.5, b + 0.5) for a, b in outline], tuple(top)))
        faces.append(f)
        for j in range(6):
            tri = (top[j], top[(j + 1) % 6], keel)
            f = self.bm.faces.new(tri)
            self.jobs.append((f, flat(under), [(0.5, 0.5)] * 3, tri))
            faces.append(f)
        return self._done(name, faces)

    def bend(self, faces, weights):
        """Make a piece soft: `weights(position)` gives [(bone, weight)] for each of its vertices."""
        for f in faces:
            for v in f.verts:
                self.blend[v] = weights(v.co)

    def resolve_uvs(self):
        self.bm.normal_update()
        for face, spec, uvp, verts in self.jobs:
            kind = spec[0]
            if kind == "facing":
                spec = proj(spec[1]) if face.normal.dot(Vector(spec[2])) > 0.92 else flat(spec[3])
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


# ---------------------------------------------------------------------------
# THE HEAD. His own silhouette: a tall narrow mask (0.490 to 0.672), not the cast's wide box and
# not Dante's rounded rows. Built as the original builds it (owner, 2026-10-05): a dark core that
# narrows to the jaw, its front at 0.100, and the face planks of `tex.MASK` standing 26 to 42 mm
# proud of it. The eyes are painted on the core, so they sit sunk between the slanted brow and the
# cheek planks by construction. The planks' fronts wear the `mask` island; their sides and
# chamfers are flat tones.
# ---------------------------------------------------------------------------
CORE_FRONT, CORE_BACK = 0.100, 0.094


def build_head(part):
    eyes = proj_facing("head", (0, -1, 0), "core_tone")
    core = [rect_ring(B((0.0, y, 0.0)), X, Y, hw, (CORE_FRONT, CORE_BACK), 0.0) for y, hw in ((0.490, 0.074), (0.660, 0.100))]
    part.loft("head-core", "head", core, eyes, caps=("lid", False))
    tone_of = {"bark": BARK, "bark_lit": BARK_LIT}
    for name, (lo, hi, kind, tilt, grain) in tex.MASK.items():
        low = (tex.NOSE_FOOT, 1.0) if name == "nose-ridge" else (1.0, 1.0)
        part.plank(name, "head", lo, hi, tone_of[kind], grain=grain, tilt=tilt, low=low,
                   chamfer=0.003, art=("mask", "front"), edge=kind + "_tone")
        # the mask's planks keep their OWN tone on the chamfer: with the body's lighter worn edge
        # (v06) each brow wore a pale strip along its top and read as a thin slat, not a heavy block   # a small cut: v05's 6 mm lit chamfer made the brow a thin slat

    P = part.plank
    # the sides: two planks each, at different depths and heights, each tapering its own way
    P("head-side-left", "head", (0.094, 0.500, -0.090), (0.118, 0.676, -0.004), BARK, low=(1.0, 0.90), bevel=(0.004, 0.006))
    P("head-side-left-rear", "head", (0.090, 0.508, 0.004), (0.112, 0.652, 0.088), BARK_LIT, high=(1.0, 0.90), lean=(0.0, -0.004))
    P("head-side-right", "head", (-0.118, 0.498, -0.086), (-0.094, 0.678, 0.010), BARK_LIT, low=(1.0, 0.92), bevel=(0.004, 0.006))
    P("head-side-right-rear", "head", (-0.114, 0.504, 0.018), (-0.090, 0.644, 0.090), BARK, high=(1.0, 0.88))
    P("head-back-left", "head", (0.002, 0.500, 0.090), (0.100, 0.652, 0.112), BARK, low=(0.94, 1.0))
    P("head-back-right", "head", (-0.100, 0.496, 0.092), (-0.002, 0.658, 0.114), BARK_LIT, low=(0.92, 1.0))
    P("crown", "head", (-0.092, 0.652, -0.090), (0.092, 0.670, 0.088), BARK, grain="z", chamfer=0.004)
    P("crown-moss", "head", (-0.054, 0.666, -0.030), (0.046, 0.680, 0.070), MOSS, high=(0.84, 0.86), chamfer=0.006, caps=(False, True))
    P("head-moss-back", "head", (-0.086, 0.612, 0.108), (-0.060, 0.634, 0.120), MOSS_LIT, chamfer=0.0)
    P("nape-moss", "head", (-0.050, 0.496, 0.104), (0.060, 0.510, 0.120), MOSS, chamfer=0.0)
    P("head-back-knot", "head", (0.030, 0.560, 0.110), (0.060, 0.586, 0.122), BARK_DARK, chamfer=0.004)
    P("head-moss-side", "head", (0.110, 0.640, 0.020), (0.124, 0.664, 0.052), MOSS, chamfer=0.0)

    # THE ANTLERS: square branches with right-angle elbows, different each side, on the original's
    # own points. Each ends in a point now instead of a sawn stub.
    bark = swatch("strand_bark", (0.0, 0.55), (0.0, 1.0), "columns")
    lit = swatch("strand_lit", (0.30, 0.75), (0.0, 1.0), "columns")
    T = part.tube
    T("horn-left", "head", [(0.086, 0.664, 0.000), (0.094, 0.700, 0.000), (0.150, 0.708, 0.000), (0.160, 0.746, 0.000),
                            (0.156, 0.784, 0.000), (0.155, 0.790, 0.000)], [0.030, 0.026, 0.023, 0.019, 0.014, 0.0], bark, square=True)
    T("horn-left-tine", "head", [(0.120, 0.705, 0.000), (0.116, 0.752, 0.006), (0.100, 0.778, 0.006), (0.097, 0.783, 0.006)],
      [0.017, 0.014, 0.010, 0.0], lit, square=True)
    T("horn-left-spur", "head", [(0.158, 0.744, 0.000), (0.198, 0.752, 0.000), (0.206, 0.772, 0.000), (0.208, 0.777, 0.000)],
      [0.014, 0.012, 0.009, 0.0], bark, square=True)
    T("horn-right", "head", [(-0.086, 0.662, 0.004), (-0.098, 0.706, 0.004), (-0.160, 0.716, 0.000), (-0.170, 0.752, 0.004),
                             (-0.162, 0.786, 0.006), (-0.160, 0.791, 0.006)], [0.031, 0.026, 0.022, 0.018, 0.013, 0.0], bark, square=True)
    T("horn-right-tine", "head", [(-0.128, 0.712, 0.002), (-0.126, 0.756, -0.004), (-0.110, 0.780, -0.004), (-0.107, 0.785, -0.004)],
      [0.016, 0.013, 0.009, 0.0], lit, square=True)
    T("horn-right-spur", "head", [(-0.170, 0.750, 0.004), (-0.210, 0.744, 0.004), (-0.222, 0.764, 0.004), (-0.225, 0.769, 0.004)],
      [0.014, 0.011, 0.008, 0.0], bark, square=True)
    # leaves on the horns and out of the crown moss, each its own size and angle (the original's)
    F = part.leaf
    F("leaf-horn-left", "head", (0.212, 0.774, 0.004), 0.037, 0.023, 0.006, 30, 34, 35, LEAF)
    F("leaf-tine-left", "head", (0.096, 0.782, 0.008), 0.029, 0.018, 0.005, 150, 24, -40, LEAF_DARK)
    F("leaf-horn-right", "head", (-0.228, 0.766, 0.004), 0.037, 0.023, 0.006, 160, 30, 50, LEAF)
    F("leaf-tine-right", "head", (-0.104, 0.782, -0.004), 0.029, 0.018, 0.005, 60, 20, -30, LEAF_DARK)
    F("leaf-crown-a", "head", (0.028, 0.692, 0.030), 0.064, 0.038, 0.007, -50, 42, 55, LEAF)
    F("leaf-crown-b", "head", (-0.034, 0.690, 0.050), 0.058, 0.035, 0.007, 130, 38, -45, LEAF_DARK)
    F("leaf-crown-c", "head", (0.004, 0.694, -0.020), 0.052, 0.032, 0.006, -100, 50, 30, LEAF)


# ---------------------------------------------------------------------------
# THE TORSO: a narrow plank waist, an abdomen of two slanted planks, a broad chest of two slabs
# meeting in a V round a diamond boss, plank back, moss in the crevices. The original's rows.
# ---------------------------------------------------------------------------
TASSET_FOLLOW = 0.55


def tasset_weights(leg):
    def weights(co):
        w = TASSET_FOLLOW * _ramp(0.252, 0.196, co.z)
        return [("torso", 1.0 - w), (leg, w)]
    return weights


def _special(part, bone, key, tone, view, **more):
    lo, hi, tilt = tex.SPECIAL[key]
    return part.plank(key.replace("_", "-"), bone, lo, hi, tone, tilt=tilt, art=(key, view), **more)


def build_torso(part):
    P = part.plank
    F = part.leaf
    P("waist-core", "torso", (-0.110, 0.228, -0.078), (0.110, 0.302, 0.074), BARK_DARK, low=(0.86, 1.0), chamfer=0.0, caps=(True, False))
    P("hip-plank-left", "torso", (0.018, 0.228, -0.094), (0.118, 0.292, -0.070), BARK, tilt=6.0, low=(0.90, 1.0))
    P("hip-plank-right", "torso", (-0.120, 0.232, -0.092), (-0.016, 0.290, -0.070), BARK_LIT, tilt=-5.0, low=(0.88, 1.0))
    # the tassets hang from the waist over the thighs; each bends toward the thigh under it
    for name, lo, hi, tone, tilt, low, leg in (
            ("tasset-left", (0.040, 0.196, -0.104), (0.126, 0.258, -0.086), BARK, 7.0, (0.80, 1.0), "leg-left"),
            ("tasset-left-outer", (0.112, 0.202, -0.080), (0.150, 0.256, 0.020), BARK_LIT, 4.0, (1.0, 0.78), "leg-left"),
            ("tasset-right", (-0.124, 0.190, -0.102), (-0.044, 0.254, -0.084), BARK_LIT, -6.0, (0.76, 1.0), "leg-right"),
            ("tasset-right-outer", (-0.152, 0.206, -0.070), (-0.114, 0.254, 0.030), BARK, -5.0, (1.0, 0.82), "leg-right")):
        part.bend(P(name, "torso", lo, hi, tone, tilt=tilt, low=low, bevel=(0.005, 0.0)), tasset_weights(leg))
    P("ab-plank-left", "torso", (0.008, 0.290, -0.106), (0.106, 0.352, -0.082), BARK, tilt=-3.0, low=(0.92, 1.0), bevel=(0.004, 0.004))
    P("ab-plank-right", "torso", (-0.108, 0.286, -0.104), (-0.006, 0.348, -0.080), BARK, tilt=3.0, low=(0.90, 1.0), bevel=(0.004, 0.004))
    P("chest-core", "torso", (-0.160, 0.340, -0.100), (0.160, 0.480, 0.096), BARK_DARK, low=(0.92, 0.94), chamfer=0.010)
    # the two chest slabs and the boss between them, each wearing its own carved cut
    _special(part, "torso", "pec_left", BARK_LIT, "front", grain="x", chamfer=0.007, bevel=(0.006, 0.006))
    _special(part, "torso", "pec_right", BARK, "front", grain="x", chamfer=0.007, bevel=(0.006, 0.006))
    _special(part, "torso", "boss", BARK, "front", chamfer=0.006, bevel=(0.005, 0.005))
    _special(part, "torso", "back_left", BARK, "back", chamfer=0.006, bevel=(0.005, 0.005), low=(0.94, 1.0))
    _special(part, "torso", "back_right", BARK_LIT, "back", chamfer=0.006, bevel=(0.005, 0.005), low=(0.92, 1.0))
    P("back-lower", "torso", (-0.100, 0.262, 0.074), (0.100, 0.346, 0.098), BARK, low=(0.90, 1.0))
    P("spine-plank", "torso", (-0.020, 0.300, 0.110), (0.022, 0.470, 0.130), BARK_LIT, low=(0.80, 1.0), bevel=(0.004, 0.006))
    P("blade-left", "torso", (0.040, 0.390, 0.116), (0.140, 0.454, 0.136), BARK, grain="x", tilt=-9.0, high=(1.0, 0.82))
    P("blade-right", "torso", (-0.142, 0.384, 0.118), (-0.042, 0.448, 0.138), BARK, grain="x", tilt=10.0, low=(1.0, 0.80))
    _special(part, "torso", "small_back", BARK_LIT, "back", grain="x", chamfer=0.005, bevel=(0.004, 0.004))
    P("back-moss-seam", "torso", (0.024, 0.360, 0.130), (0.090, 0.372, 0.142), MOSS, chamfer=0.0)
    P("back-moss-low", "torso", (-0.090, 0.318, 0.106), (-0.034, 0.330, 0.120), MOSS_DARK, chamfer=0.0)
    P("chest-strand-a", "torso", (0.130, 0.410, -0.132), (0.144, 0.446, -0.122), MOSS, chamfer=0.0, low=(0.6, 1.0))
    P("chest-strand-b", "torso", (-0.146, 0.360, -0.132), (-0.134, 0.392, -0.122), MOSS_LIT, chamfer=0.0, low=(0.6, 1.0))
    P("neck", "torso", (-0.060, 0.466, -0.050), (0.060, 0.512, 0.050), BARK_DARK, chamfer=0.008, caps=(False, False))
    # moss in the crevices, raised clumps of two greens, each its own
    P("collar-moss-left", "torso", (0.040, 0.470, -0.108), (0.142, 0.494, -0.030), MOSS, high=(0.86, 0.84), chamfer=0.006, caps=(False, True))
    P("collar-moss-right", "torso", (-0.132, 0.466, -0.100), (-0.050, 0.488, -0.020), MOSS_LIT, high=(0.84, 0.86), chamfer=0.006, caps=(False, True))
    P("collar-moss-back", "torso", (-0.080, 0.454, 0.110), (0.030, 0.480, 0.128), MOSS, chamfer=0.0)
    P("pec-moss-left", "torso", (0.122, 0.440, -0.138), (0.162, 0.468, -0.122), MOSS, chamfer=0.0)
    P("pec-moss-right", "torso", (-0.156, 0.380, -0.134), (-0.090, 0.392, -0.120), MOSS_DARK, chamfer=0.0)
    P("ab-moss", "torso", (-0.010, 0.292, -0.110), (0.004, 0.344, -0.098), MOSS, chamfer=0.0)
    P("hip-moss", "torso", (0.030, 0.286, -0.108), (0.104, 0.296, -0.094), MOSS_LIT, chamfer=0.0)
    P("pec-edge-moss-left", "torso", (0.020, 0.470, -0.130), (0.060, 0.484, -0.112), MOSS_LIT, chamfer=0.0)
    P("pec-edge-moss-right", "torso", (-0.074, 0.462, -0.126), (-0.044, 0.476, -0.110), MOSS, chamfer=0.0)

    # THE TORSO VINE: over his right shoulder, down across the chest under the boss, round his left
    # side and onto the back. Thick. The original's nine points, drawn through twice as many rings,
    # with one point added at each end so it starts inside the chest and ends in a point.
    part.tube("torso-vine", "torso",
              [(-0.146, 0.462, 0.082), (-0.152, 0.476, 0.060), (-0.156, 0.470, -0.080), (-0.090, 0.428, -0.148), (-0.010, 0.344, -0.146),
               (0.070, 0.296, -0.126), (0.146, 0.262, -0.088), (0.176, 0.252, 0.000), (0.150, 0.256, 0.090), (0.060, 0.272, 0.112),
               (0.040, 0.276, 0.112)],
              [0.013, 0.014, 0.015, 0.016, 0.016, 0.015, 0.015, 0.014, 0.013, 0.012, 0.0],
              swatch("strand_vine", (0.0, 1.0), (0.0, 1.0), "columns"), sides=5, smooth=2)
    # a small branch sprouting from his back, off the spine plank, with its two leaves
    part.tube("back-branch", "torso", [(-0.010, 0.420, 0.128), (-0.030, 0.450, 0.168), (-0.070, 0.470, 0.186), (-0.084, 0.500, 0.190),
                                       (-0.086, 0.506, 0.190)], [0.016, 0.013, 0.010, 0.007, 0.0],
              swatch("strand_bark", (0.1, 0.5), (0.0, 1.0), "columns"), square=True)
    F("leaf-back-a", "torso", (-0.090, 0.512, 0.192), 0.050, 0.030, 0.007, 120, 40, 30, LEAF)
    F("leaf-back-b", "torso", (-0.050, 0.466, 0.196), 0.044, 0.026, 0.007, 60, 20, -40, LEAF_DARK)
    # leaf clusters: the collar and the left hip (the shoulders' are with the arms)
    F("leaf-collar-a", "torso", (0.110, 0.504, -0.084), 0.070, 0.044, 0.008, -60, 34, 40, LEAF)
    F("leaf-collar-b", "torso", (0.136, 0.500, -0.054), 0.062, 0.038, 0.007, -10, 38, -36, LEAF_DARK)
    F("leaf-collar-c", "torso", (0.082, 0.498, -0.104), 0.056, 0.034, 0.007, -110, 26, 46, LEAF)
    F("leaf-hip-a", "torso", (0.112, 0.262, -0.104), 0.060, 0.036, 0.007, -80, -16, -40, LEAF)
    F("leaf-hip-b", "torso", (0.132, 0.252, -0.084), 0.052, 0.032, 0.006, -20, -8, 46, LEAF_DARK)
    F("leaf-hip-c", "torso", (0.094, 0.248, -0.110), 0.046, 0.028, 0.006, -130, -20, 30, MOSS_LIT)


# ---------------------------------------------------------------------------
# THE ARMS: slanted shoulder planks over a dark block, an upper-arm bundle, an elbow block, and
# from the elbow down THE BRAID: four strands that leave the elbow thick, twist round each other
# and taper to separate points, and one thin tendril. The two arms are typed separately and twist
# differently. Every point and radius below is the original's (`_forms` in the builder); only the
# last point of each strand is new, a true tip a few millimetres on from where the stub ended.
# ---------------------------------------------------------------------------

def build_arm_left(part):
    P = part.plank
    F = part.leaf
    arm, fore = "arm-left", "forearm-left"
    P("pauldron-left", arm, (0.118, 0.418, -0.094), (0.250, 0.522, 0.094), BARK_DARK, grain="x", chamfer=0.010, ends="bark_end_tone")
    P("pauldron-top-left", arm, (0.112, 0.512, -0.092), (0.262, 0.540, 0.088), BARK, grain="x", tilt=-8.0, high=(0.90, 1.0), chamfer=0.007, bevel=(0.006, 0.006))
    _special(part, arm, "pauldron_spiral", BARK_LIT, "front", grain="x", chamfer=0.006, bevel=(0.005, 0.005), high=(1.0, 0.90))
    P("pauldron-back-left", arm, (0.130, 0.436, 0.088), (0.246, 0.516, 0.108), BARK, grain="x", tilt=-5.0, high=(1.0, 0.86))
    P("pauldron-moss-left-a", arm, (0.150, 0.534, -0.050), (0.196, 0.552, 0.004), MOSS, chamfer=0.005, high=(0.8, 0.8), caps=(False, True))
    P("pauldron-moss-left-b", arm, (0.190, 0.532, 0.020), (0.232, 0.548, 0.060), MOSS_LIT, chamfer=0.005, high=(0.8, 0.8), caps=(False, True))
    P("pauldron-drip-left", arm, (0.198, 0.488, -0.118), (0.240, 0.520, -0.102), MOSS_LIT, chamfer=0.0, low=(0.6, 1.0))
    P("pauldron-lower-left", arm, (0.200, 0.470, -0.100), (0.284, 0.494, 0.096), BARK, grain="z", tilt=-14.0, chamfer=0.005, bevel=(0.004, 0.004))
    P("moss-strand-left-a", arm, (0.262, 0.444, -0.104), (0.278, 0.488, -0.094), MOSS, chamfer=0.0, low=(0.5, 1.0))
    P("moss-strand-left-b", arm, (0.270, 0.430, -0.060), (0.284, 0.486, -0.050), MOSS_DARK, chamfer=0.0, low=(0.5, 1.0))
    P("moss-strand-left-c", arm, (0.266, 0.452, 0.040), (0.280, 0.488, 0.050), MOSS_LIT, chamfer=0.0, low=(0.5, 1.0))
    P("upper-arm-left", arm, (0.240, 0.430, -0.052), (0.340, 0.508, 0.052), BARK_DARK, grain="x", chamfer=0.0, caps=(False, False))
    P("upper-arm-top-left", arm, (0.244, 0.500, -0.040), (0.336, 0.520, 0.036), BARK, grain="x", high=(0.86, 1.0))
    P("upper-arm-front-left", arm, (0.246, 0.440, -0.066), (0.334, 0.496, -0.048), BARK_LIT, grain="x", high=(1.0, 0.84))
    P("elbow-left", arm, (0.326, 0.422, -0.068), (0.376, 0.518, 0.068), BARK, chamfer=0.010, bevel=(0.008, 0.008))
    P("elbow-moss-left", arm, (0.360, 0.508, -0.050), (0.384, 0.524, 0.030), MOSS, chamfer=0.0)
    T = part.tube
    T("braid-left-a", fore, [(0.360, 0.500, -0.036), (0.404, 0.506, 0.010), (0.448, 0.476, 0.044), (0.492, 0.440, 0.022),
                             (0.534, 0.438, -0.020), (0.572, 0.458, -0.030), (0.604, 0.474, -0.012), (0.630, 0.478, 0.004)],
      [0.034, 0.031, 0.028, 0.025, 0.020, 0.015, 0.009, 0.0], swatch("strand_bark", (0.0, 1.0), (0.0, 1.0), "columns"), smooth=2, start_cap=True)
    T("braid-left-b", fore, [(0.360, 0.440, 0.030), (0.404, 0.434, -0.012), (0.448, 0.458, -0.046), (0.492, 0.494, -0.030),
                             (0.534, 0.502, 0.012), (0.570, 0.486, 0.034), (0.600, 0.466, 0.022), (0.624, 0.456, 0.008)],
      [0.032, 0.030, 0.027, 0.024, 0.019, 0.014, 0.009, 0.0], swatch("strand_lit", (0.0, 1.0), (0.0, 1.0), "columns"), smooth=2, start_cap=True)
    T("braid-left-c", fore, [(0.362, 0.470, 0.050), (0.404, 0.486, 0.052), (0.446, 0.508, 0.014), (0.488, 0.494, -0.030),
                             (0.528, 0.462, -0.040), (0.562, 0.440, -0.014), (0.590, 0.440, 0.010), (0.612, 0.448, 0.020)],
      [0.028, 0.026, 0.024, 0.021, 0.017, 0.012, 0.008, 0.0], swatch("strand_bark", (1.0, 0.0), (0.0, 1.0), "columns"), smooth=2, start_cap=True)
    T("braid-left-vine", fore, [(0.362, 0.476, -0.054), (0.406, 0.450, -0.050), (0.448, 0.434, -0.010), (0.490, 0.450, 0.036),
                                (0.530, 0.484, 0.040), (0.566, 0.504, 0.008), (0.596, 0.496, -0.016), (0.616, 0.484, -0.020)],
      [0.024, 0.023, 0.021, 0.019, 0.016, 0.012, 0.008, 0.0], swatch("strand_vine", (0.0, 0.9), (0.0, 1.0), "columns"), smooth=2, start_cap=True)
    T("tendril-left", fore, [(0.520, 0.500, 0.036), (0.548, 0.524, 0.056), (0.566, 0.542, 0.044), (0.574, 0.550, 0.028)],
      [0.009, 0.007, 0.005, 0.0], flat("vine_lit_tone"), sides=4)
    # the shoulder's leaf cluster
    F("leaf-shoulder-left-a", arm, (0.186, 0.566, -0.030), 0.074, 0.046, 0.008, 10, 42, -40, LEAF)
    F("leaf-shoulder-left-b", arm, (0.214, 0.562, 0.024), 0.066, 0.040, 0.008, -40, 34, 36, LEAF_DARK)
    F("leaf-shoulder-left-c", arm, (0.160, 0.560, 0.030), 0.060, 0.036, 0.007, 120, 30, -30, LEAF)
    F("leaf-shoulder-left-d", arm, (0.236, 0.556, -0.040), 0.052, 0.032, 0.007, -10, 26, 44, MOSS_LIT)


def build_arm_right(part):
    P = part.plank
    F = part.leaf
    arm, fore = "arm-right", "forearm-right"
    P("pauldron-right", arm, (-0.252, 0.416, -0.092), (-0.116, 0.520, 0.096), BARK_DARK, grain="x", chamfer=0.010, ends="bark_end_tone")
    P("pauldron-top-right", arm, (-0.264, 0.510, -0.086), (-0.110, 0.538, 0.092), BARK_LIT, grain="x", tilt=7.0, low=(0.90, 1.0), chamfer=0.007, bevel=(0.006, 0.006))
    P("pauldron-front-right", arm, (-0.254, 0.434, -0.110), (-0.122, 0.518, -0.088), BARK, grain="x", tilt=5.0, low=(1.0, 0.88), chamfer=0.006, bevel=(0.005, 0.005))
    P("pauldron-back-right", arm, (-0.248, 0.438, 0.090), (-0.128, 0.512, 0.110), BARK, grain="x", tilt=6.0, low=(1.0, 0.84))
    P("pauldron-moss-right-a", arm, (-0.214, 0.532, 0.010), (-0.168, 0.552, 0.062), MOSS_LIT, chamfer=0.005, high=(0.8, 0.8), caps=(False, True))
    P("pauldron-moss-right-b", arm, (-0.176, 0.530, -0.040), (-0.140, 0.546, -0.004), MOSS, chamfer=0.005, high=(0.8, 0.8), caps=(False, True))
    P("pauldron-moss-right-c", arm, (-0.252, 0.528, -0.060), (-0.226, 0.544, -0.030), MOSS_DARK, chamfer=0.0)
    P("pauldron-drip-right", arm, (-0.254, 0.430, -0.116), (-0.218, 0.464, -0.100), MOSS, chamfer=0.0, low=(0.6, 1.0))
    P("pauldron-lower-right", arm, (-0.288, 0.466, -0.094), (-0.204, 0.492, 0.100), BARK_LIT, grain="z", tilt=13.0, chamfer=0.005, bevel=(0.004, 0.004))
    P("moss-strand-right-a", arm, (-0.282, 0.426, -0.090), (-0.268, 0.484, -0.080), MOSS, chamfer=0.0, low=(0.5, 1.0))
    P("moss-strand-right-b", arm, (-0.286, 0.446, 0.010), (-0.272, 0.484, 0.020), MOSS_LIT, chamfer=0.0, low=(0.5, 1.0))
    P("upper-arm-right", arm, (-0.342, 0.428, -0.050), (-0.242, 0.506, 0.054), BARK_DARK, grain="x", chamfer=0.0, caps=(False, False))
    P("upper-arm-top-right", arm, (-0.338, 0.498, -0.034), (-0.246, 0.518, 0.042), BARK_LIT, grain="x", low=(0.84, 1.0))
    P("upper-arm-back-right", arm, (-0.334, 0.444, 0.050), (-0.248, 0.500, 0.068), BARK, grain="x", low=(1.0, 0.86))
    P("elbow-right", arm, (-0.378, 0.420, -0.066), (-0.328, 0.516, 0.070), BARK, chamfer=0.010, bevel=(0.008, 0.008))
    P("elbow-moss-right", arm, (-0.386, 0.418, -0.040), (-0.362, 0.434, 0.050), MOSS_LIT, chamfer=0.0)
    T = part.tube
    T("braid-right-a", fore, [(-0.362, 0.444, -0.034), (-0.406, 0.438, 0.012), (-0.450, 0.462, 0.046), (-0.494, 0.498, 0.030),
                              (-0.536, 0.504, -0.010), (-0.574, 0.486, -0.034), (-0.606, 0.466, -0.024), (-0.632, 0.458, -0.008)],
      [0.034, 0.031, 0.028, 0.025, 0.020, 0.015, 0.009, 0.0], swatch("strand_bark", (0.05, 1.0), (1.0, 0.0), "columns"), smooth=2, start_cap=True)
    T("braid-right-b", fore, [(-0.360, 0.500, 0.032), (-0.402, 0.508, -0.010), (-0.446, 0.484, -0.046), (-0.490, 0.448, -0.034),
                              (-0.532, 0.436, 0.006), (-0.568, 0.448, 0.032), (-0.600, 0.468, 0.026), (-0.626, 0.480, 0.010)],
      [0.032, 0.030, 0.027, 0.023, 0.019, 0.014, 0.009, 0.0], swatch("strand_lit", (1.0, 0.0), (0.0, 1.0), "columns"), smooth=2, start_cap=True)
    T("braid-right-c", fore, [(-0.364, 0.470, -0.052), (-0.406, 0.456, -0.054), (-0.448, 0.436, -0.018), (-0.490, 0.444, 0.028),
                              (-0.528, 0.474, 0.042), (-0.560, 0.500, 0.020), (-0.588, 0.502, -0.006), (-0.610, 0.494, -0.018)],
      [0.028, 0.026, 0.024, 0.021, 0.017, 0.012, 0.008, 0.0], swatch("strand_bark", (0.0, 0.92), (0.0, 1.0), "columns"), smooth=2, start_cap=True)
    T("braid-right-vine", fore, [(-0.362, 0.470, 0.056), (-0.404, 0.496, 0.048), (-0.446, 0.510, 0.006), (-0.488, 0.494, -0.036),
                                 (-0.528, 0.462, -0.042), (-0.564, 0.440, -0.010), (-0.594, 0.444, 0.016), (-0.614, 0.456, 0.022)],
      [0.024, 0.023, 0.021, 0.019, 0.016, 0.012, 0.008, 0.0], swatch("strand_vine", (1.0, 0.1), (0.0, 1.0), "columns"), smooth=2, start_cap=True)
    T("tendril-right", fore, [(-0.530, 0.440, -0.040), (-0.556, 0.420, -0.058), (-0.572, 0.404, -0.046), (-0.580, 0.398, -0.030)],
      [0.009, 0.007, 0.005, 0.0], flat("vine_lit_tone"), sides=4)
    F("leaf-shoulder-right-a", arm, (-0.186, 0.564, 0.030), 0.074, 0.046, 0.008, 170, 40, -44, LEAF)
    F("leaf-shoulder-right-b", arm, (-0.214, 0.558, -0.030), 0.062, 0.038, 0.007, -150, 30, 34, LEAF_DARK)
    F("leaf-shoulder-right-c", arm, (-0.156, 0.558, -0.040), 0.056, 0.034, 0.007, 60, 28, 40, LEAF)


# ---------------------------------------------------------------------------
# THE LEGS: a bundle of upright planks round a dark core, a knee plate, a root foot with four
# roots. The shins widen toward the ground like a trunk into its roots. Typed separately.
# ---------------------------------------------------------------------------

def build_leg_left(part):
    P = part.plank
    leg = "leg-left"
    root = swatch("strand_root", (0.0, 0.7), (0.0, 1.0), "columns")
    P("foot-left", leg, (0.014, 0.000, -0.114), (0.162, 0.044, 0.078), ROOT_T, high=(0.80, 0.82), chamfer=0.014, caps=(False, True))
    P("foot-moss-left", leg, (0.058, 0.034, -0.102), (0.112, 0.050, -0.074), MOSS_DARK, chamfer=0.0)
    P("shin-core-left", leg, (0.036, 0.036, -0.068), (0.140, 0.150, 0.064), BARK_DARK, low=(1.22, 1.0), chamfer=0.0, caps=(False, False))
    P("shin-plank-left-a", leg, (0.040, 0.030, -0.086), (0.084, 0.150, -0.064), BARK, tilt=2.0, low=(1.30, 1.0), bevel=(0.0, 0.004))
    P("shin-plank-left-b", leg, (0.090, 0.040, -0.084), (0.138, 0.138, -0.060), BARK_LIT, tilt=-2.0, low=(1.26, 1.0), bevel=(0.0, 0.004))
    P("shin-plank-left-c", leg, (0.132, 0.036, -0.056), (0.156, 0.146, 0.040), BARK, low=(1.0, 1.12), high=(1.0, 0.90), bevel=(0.0, 0.004))
    P("shin-plank-left-d", leg, (0.046, 0.040, 0.056), (0.130, 0.144, 0.080), BARK, low=(1.14, 1.0), bevel=(0.0, 0.004))
    P("knee-left", leg, (0.036, 0.130, -0.104), (0.140, 0.182, -0.066), BARK_LIT, grain="x", tilt=-4.0, chamfer=0.008, bevel=(0.006, 0.006))
    P("knee-moss-left", leg, (0.048, 0.176, -0.104), (0.128, 0.188, -0.090), MOSS, chamfer=0.0)
    P("thigh-core-left", leg, (0.034, 0.160, -0.066), (0.146, 0.290, 0.066), BARK_DARK, low=(0.86, 1.0), chamfer=0.0, caps=(False, True))
    P("thigh-plank-left-a", leg, (0.040, 0.172, -0.084), (0.096, 0.284, -0.062), BARK, high=(0.92, 1.0), bevel=(0.004, 0.004))
    P("thigh-plank-left-b", leg, (0.102, 0.166, -0.082), (0.148, 0.274, -0.058), BARK_LIT, tilt=-2.0, low=(0.82, 1.0), bevel=(0.004, 0.004))
    P("thigh-plank-left-c", leg, (0.138, 0.170, -0.050), (0.160, 0.278, 0.044), BARK, low=(1.0, 0.86))
    P("thigh-plank-left-d", leg, (0.044, 0.176, 0.056), (0.136, 0.286, 0.078), BARK, low=(0.88, 1.0))
    T = part.tube
    T("root-left-a", leg, [(0.120, 0.030, -0.080), (0.150, 0.023, -0.130), (0.164, 0.015, -0.164), (0.168, 0.008, -0.176)],
      [0.030, 0.022, 0.014, 0.0], root, square=True, start_cap=True)
    T("root-left-b", leg, [(0.050, 0.030, -0.090), (0.040, 0.023, -0.140), (0.030, 0.015, -0.170), (0.027, 0.008, -0.181)],
      [0.030, 0.021, 0.014, 0.0], root, square=True, start_cap=True)
    T("root-left-c", leg, [(0.150, 0.030, 0.000), (0.192, 0.017, 0.010), (0.214, 0.010, 0.018), (0.222, 0.005, 0.021)],
      [0.018, 0.013, 0.009, 0.0], root, square=True, start_cap=True)
    T("root-left-d", leg, [(0.090, 0.030, 0.070), (0.100, 0.016, 0.110), (0.104, 0.010, 0.130), (0.105, 0.005, 0.138)],
      [0.016, 0.012, 0.009, 0.0], root, square=True, start_cap=True)


def build_leg_right(part):
    P = part.plank
    leg = "leg-right"
    root = swatch("strand_root", (0.2, 0.9), (0.0, 1.0), "columns")
    P("foot-right", leg, (-0.164, 0.000, -0.116), (-0.012, 0.046, 0.076), ROOT_T, high=(0.82, 0.80), chamfer=0.014, caps=(False, True))
    P("shin-core-right", leg, (-0.142, 0.036, -0.066), (-0.034, 0.152, 0.066), BARK_DARK, low=(1.24, 1.0), chamfer=0.0, caps=(False, False))
    P("shin-plank-right-a", leg, (-0.080, 0.034, -0.088), (-0.036, 0.152, -0.066), BARK_LIT, tilt=-1.5, low=(1.28, 1.0), bevel=(0.0, 0.004))
    P("shin-plank-right-b", leg, (-0.140, 0.036, -0.082), (-0.088, 0.142, -0.058), BARK, tilt=2.5, low=(1.32, 1.0), bevel=(0.0, 0.004))
    P("shin-plank-right-c", leg, (-0.160, 0.040, -0.044), (-0.136, 0.140, 0.050), BARK_LIT, low=(1.0, 1.10), high=(1.0, 0.88), bevel=(0.0, 0.004))
    P("shin-plank-right-d", leg, (-0.128, 0.036, 0.058), (-0.044, 0.148, 0.082), BARK, low=(1.16, 1.0), bevel=(0.0, 0.004))
    P("knee-right", leg, (-0.144, 0.134, -0.102), (-0.036, 0.184, -0.066), BARK, grain="x", tilt=3.0, chamfer=0.008, bevel=(0.006, 0.006))
    P("knee-knot-right", leg, (-0.100, 0.150, -0.110), (-0.070, 0.172, -0.098), BARK_DARK, chamfer=0.005)
    P("shin-moss-right", leg, (-0.092, 0.050, -0.094), (-0.082, 0.126, -0.084), MOSS_LIT, chamfer=0.0)
    P("thigh-core-right", leg, (-0.148, 0.162, -0.064), (-0.034, 0.290, 0.068), BARK_DARK, low=(0.88, 1.0), chamfer=0.0, caps=(False, True))
    P("thigh-plank-right-a", leg, (-0.094, 0.168, -0.086), (-0.038, 0.282, -0.064), BARK, tilt=2.0, high=(0.90, 1.0), bevel=(0.004, 0.004))
    P("thigh-plank-right-b", leg, (-0.150, 0.174, -0.080), (-0.100, 0.280, -0.056), BARK, low=(0.84, 1.0), bevel=(0.004, 0.004))
    P("thigh-plank-right-c", leg, (-0.162, 0.168, -0.046), (-0.140, 0.272, 0.048), BARK_LIT, low=(1.0, 0.84))
    P("thigh-plank-right-d", leg, (-0.138, 0.170, 0.056), (-0.046, 0.284, 0.080), BARK, low=(0.90, 1.0))
    T = part.tube
    T("root-right-a", leg, [(-0.110, 0.030, -0.086), (-0.126, 0.023, -0.136), (-0.140, 0.015, -0.172), (-0.144, 0.008, -0.184)],
      [0.031, 0.022, 0.014, 0.0], root, square=True, start_cap=True)
    T("root-right-b", leg, [(-0.040, 0.030, -0.080), (-0.020, 0.017, -0.124), (-0.012, 0.010, -0.150), (-0.010, 0.005, -0.158)],
      [0.018, 0.013, 0.009, 0.0], root, square=True, start_cap=True)
    T("root-right-c", leg, [(-0.150, 0.030, -0.010), (-0.194, 0.018, -0.020), (-0.220, 0.010, -0.024), (-0.229, 0.005, -0.025)],
      [0.020, 0.015, 0.009, 0.0], root, square=True, start_cap=True)
    T("root-right-d", leg, [(-0.070, 0.030, 0.068), (-0.058, 0.016, 0.108), (-0.052, 0.010, 0.132), (-0.050, 0.005, 0.140)],
      [0.016, 0.012, 0.009, 0.0], root, square=True, start_cap=True)
    # the vine coiling up his right leg, the original's seven points
    T("leg-vine", leg, [(-0.024, 0.016, -0.066), (-0.030, 0.040, -0.080), (-0.080, 0.070, -0.100), (-0.150, 0.100, -0.084), (-0.168, 0.140, 0.000),
                        (-0.130, 0.180, 0.084), (-0.050, 0.210, 0.086), (-0.030, 0.240, 0.000), (-0.030, 0.250, -0.012)],
      [0.010, 0.011, 0.012, 0.012, 0.012, 0.011, 0.011, 0.010, 0.0], swatch("strand_vine", (0.1, 0.9), (0.0, 1.0), "columns"),
      sides=5, smooth=2)


# ---------------------------------------------------------------------------
# THE BUILD
# ---------------------------------------------------------------------------

def mesh_arrays(obj):
    """A mesh as glTF arrays: +y up, -z the way Blender's -y faces, v flipped, up to four bones a vertex."""
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
    """team-paete.glb with its meshes swapped: skeleton, bind matrices and clips copied across."""
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
        world = (side * ELBOW_X, ARM_Z, -ARM_Y)
        pw = [-matrices[BONES.index(parent)][12 + a] for a in range(3)]
        gltf["nodes"].append({"name": name, "translation": [world[a] - pw[a] for a in range(3)]})
        gltf["nodes"][node_of[parent]].setdefault("children", []).append(len(gltf["nodes"]) - 1)
        skin["joints"].append(len(gltf["nodes"]) - 1)
        matrices.append((1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -world[0], -world[1], -world[2], 1))
    binds = add([tuple(float(x) for x in m) for m in matrices], "f", "MAT4", 5126)
    for other in gltf["skins"]:
        other["joints"] = list(skin["joints"])
        other["inverseBindMatrices"] = binds

    # the prototype's own locomotion clips, in place of the ones copied across (the clips script)
    node_of = {n.get("name"): i for i, n in enumerate(gltf["nodes"])}
    kinds = {"rotation": "VEC4", "translation": "VEC3", "scale": "VEC3"}
    rest = {n.get("name"): n.get("translation", [0.0, 0.0, 0.0]) for n in gltf["nodes"]}
    for anim in gltf["animations"]:
        if anim["name"] not in clips.CLIPS:
            continue
        anim["samplers"], anim["channels"] = [], []
        for (bone, path), keys in clips.CLIPS[anim["name"]]().items():
            if path == "translation":
                # a clip types a bone's translation as an OFFSET from its rest place
                keys = [(t, tuple(rest[bone][a] + v[a] for a in range(3))) for t, v in keys]
            times = add([(t,) for t, _ in keys], "f", "SCALAR", 5126, minmax=True)
            values = add([v for _, v in keys], "f", kinds[path], 5126)
            anim["channels"].append({"sampler": len(anim["samplers"]), "target": {"node": node_of[bone], "path": path}})
            anim["samplers"].append({"input": times, "output": values, "interpolation": "LINEAR"})

    by_name = {m.get("name"): m for m in gltf["meshes"]}
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
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso character redesign prototype (paete)"}
    gltf["extras"] = {"prototype": "character-redesign-20261005", "replaces": "nothing; not loaded by the game"}
    gltf["images"] = [{"uri": tex.ATLAS_NAME, "name": NAME + "-atlas"}]
    gltf["textures"] = [{"sampler": 0, "source": 0, "name": NAME + "-atlas"}]
    gltf["samplers"] = [{"magFilter": 9729, "minFilter": 9987}]
    gltf["materials"][0]["name"] = NAME
    gltf["materials"][0]["doubleSided"] = False
    gltf["nodes"][0]["name"] = NAME
    gltf["scenes"][0]["name"] = NAME
    for attempt in range(5):
        try:
            bpv.write_glb(out, gltf, blob)
            break
        except OSError:
            time.sleep(1.0)
    else:
        raise SystemExit("could not write " + out)


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
    print("triangles %d  (budget %d, the original is 9892)" % (tris, TRIANGLE_BUDGET))
    if abs(lo.z) > 0.0015:
        raise SystemExit("the feet are at %.4f, not on zero" % lo.z)
    if abs(hi.z - HEIGHT) > 0.006:
        raise SystemExit("the top is at %.4f, it should be %.4f" % (hi.z, HEIGHT))
    if tris > TRIANGLE_BUDGET:
        raise SystemExit("%d triangles is over the budget of %d" % (tris, TRIANGLE_BUDGET))
    # THE VINE TIPS (PaeteVfx): every point of each braid must end where the original's does.
    body = objects[0]
    for side, bone in ((1, "forearm-left"), (-1, "forearm-right")):
        group = body.vertex_groups[bone].index
        fore = [v.co for v in body.data.vertices if v.groups[0].group == group]
        worst = 0.0
        for tip in TIPS[side]:
            want = B(tip)
            worst = max(worst, min((p - want).length for p in fore))
        far = max(fore, key=lambda p: side * p.x)
        print("%s: %d vertices, far point (%.3f, %.3f, %.3f), worst tip miss %.4f" % (bone, len(fore), far.x, far.y, far.z, worst))
        if worst > 0.005:
            raise SystemExit("a braid tip on %s moved %.4f from the original's" % (bone, worst))
        if min(side * p.x for p in fore) < ELBOW_X - 0.035:
            raise SystemExit("a forearm vertex on %s sits above the elbow" % bone)


def build(armature, material):
    body, head = Part("body-mesh"), Part("head-mesh")
    build_torso(body)
    build_arm_left(body)
    build_arm_right(body)
    build_leg_left(body)
    build_leg_right(body)
    build_head(head)
    objects = []
    for part in (body, head):
        part.resolve_uvs()
        if part is head:
            # AFTER the paint is placed (it is projected from the full-size shape): the head comes in.
            for v in part.bm.verts:
                mix = part.blend.get(v)
                share = 1.0 if not mix else sum(w for b, w in mix if b == "head") / sum(w for _, w in mix)
                v.co = HEAD_JOINT + (v.co - HEAD_JOINT) * (1.0 + (HEAD_SCALE - 1.0) * share)
        obj = part.to_object(armature)
        obj.data.materials.append(material)
        objects.append(obj)
        print(part.name, " ".join("%s:%d" % (n, t) for n, t in part.pieces))
    verify(objects)
    return objects


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=BASE, bone_heuristic="BLENDER")
    armature = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    for obj in [o for o in bpy.data.objects if o.type == "MESH"]:
        bpy.data.objects.remove(obj)
    armature.name = NAME
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
        raise SystemExit("paint the atlas first: py -3 tools/author_character_redesign_paete_textures.py")
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
