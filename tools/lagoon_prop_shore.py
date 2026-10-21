"""Lagoon Court SHORE PROP KIT: paddles, driftwood, a stone anchor and firewood, models AND the few
textures nothing else in the Lagoon covers.

  py -3 tools/lagoon_prop_shore.py --paint                  # paint every ps_ texture
  py -3 tools/lagoon_prop_shore.py --paint ps_husk          # paint the named ones
  blender -b --python tools/lagoon_prop_shore.py -- --preview N

With --preview N the Blender run renders Logs/lagoon-blender/ps_{lineup,close,close2,far}_vN.png:
every kind with two seeds on warm sand beside a 1.6 m pink scale cylinder, two close-ups and the
lineup from 20 m at eye height (the readability test). It paints any missing ps_ texture first
(through `py -3`, since Blender's Python has no PIL). An existing render is never overwritten:
bump N. Nothing is saved to a .blend; the cove script calls build_prop and links the result.

WHY THIS KIT EXISTS (docs/LAGOON_REWORK_GUIDE.md § 7a item 1). The reference (Papaioanou, "Stylized
Fishing Village", ArtStation GvJv5a) has oars and shovels leaning on things, driftwood on the sand
and firewood by the doors: the small clutter of people who fish from this beach. These are the
Filipino versions: sagwan paddles, a stone anchor of the kind bangka fishermen still tie, split
firewood with a pile of coconut husks (bunot, the everyday fuel and smudge of a coastal barangay).

  import lagoon_prop_shore as PS
  col = PS.build_prop(kind, seed)      # a Collection, NOT linked; one root empty named by kind
  report = PS.check_prop(col)          # tris, non-manifold, coplanar overlaps, floating shells

KINDS, every one with its origin at the centre of its ground footprint, z = 0 the ground, +Y the
front (the side the camera and the court should see). Everything touching the ground sinks 1 to 3 cm
into it, so a prop set on uneven sand never shows a gap.

  * "oar_pair"      two fat bangka paddles (sagwan: a shaft swelling from grip to throat, a 26 to
                    30 cm leaf or round blade, a fat knob or a drooping T grip, each bent a little
                    in plan), 1.4 to 1.55 m. ODD seeds: lying on the sand, one crossed over the
                    other, its blade on the sand and its shaft resting on the first. EVEN seeds:
                    leaning on a paddle rest, a sagging bamboo pole in the forks of two tapered,
                    leaning timber stakes, their blades planted in the sand at the front.
  * "driftwood"     one fat, bleached log (1.4 to 1.8 m, a swollen root end tapering 40 %, a soft
                    S-bend, rounded ends, a gentle twisted lobe) half sunk in the sand, with one fat
                    broken stub on most seeds, and a plain fat branch with its foot on the sand and
                    its body lying back across the log.
  * "anchor_stone"  a traditional stone anchor with a fat 5.6 cm rope. ODD seeds: an egg-shaped
                    stone with a pecked groove round its waist, the rope lashed in the groove and
                    knotted on top. EVEN seeds: a flat rounded stone with a big hole bored through
                    one end, the rope looped through it and knotted. The free end runs down to the
                    sand and lies in one lazy S curve at the front.
  * "firewood"      a small pile of FAT firewood running front to back, so the front is a face of
                    ringed end grain: split quarters, thirds and halves (bark outside, pale split
                    faces) and whole unsplit rounds. ODD seeds 3-2-1, EVEN seeds 3-2. Most seeds add
                    three or four fat coconut husk segments beside it. (The village kit's
                    "firewood" is round logs on sleepers; this is the split pile and the husks.)

OWNER NOTES ON v1 (2026-09-27), and what they changed: "i dont like how detail some of the props
are ... stylized semi-cartoony" and "experiment more with being organic in how you shape things".
v1 had thin paddles, a flat-capped lobed root with three twigs and a forked branch, a thin rope in
a spiral coil, a twelve-piece crib of thin split wood and a tower of eleven small husks. Everything
is now fewer, fatter, rounder pieces with a taper, a bend or a lean each, and the textures lost
their fine patterning (Logs/lagoon-blender/ps_beforeafter_v6.png).

THE ROOT EMPTY carries prop_kind, prop_seed, prop_mount ("ground"), prop_variant and prop_box (xmin,
ymin, zmin, xmax, ymax, zmax, in the root's frame).

ONE MESH PER PROP, SEVERAL MATERIALS. A split log is ONE closed shell with bark on its arc, split
faces on its sides and end grain on its ends; cutting it into three objects would break the shell
open at every seam. So each prop is one mesh object parented to the root, its faces carrying the
material index of their surface, with one live Bevel modifier (hardened normals).

MATERIALS, looked up by NAME first so the kits share them in one file (an existing one is used as it
is): timber (timber_a) for the paddles and stakes, bamboo (bamboo_a) for the rail. The rest are this
kit's, built with render_lagoon_texture_preview.uv_material around a texture that already exists
wherever one fits (LAGOON_REWORK_GUIDE.md § 2: reuse before painting):

  ps_driftwood  timber_b (the grey weathered timber) multiplied 1.15 lighter: sun-bleached wood
  ps_stone      rock_a (the approved rock) multiplied toward grey-khaki, so a stone on the sand
                separates from it (§ 7a item 12)
  ps_bark       timber_c (warm red-brown, long soft feathered strokes: the Kanto bark J drawing)
  ps_splitwood  NEW: fresh split wood, pale honey with a few long soft fibre strokes and big
                patches. Nothing else in the Lagoon is freshly split: plank_c is sawn and painted
                with joints, timber_a is darker weathered timber.
  ps_endgrain   NEW, WHOLE (one disc, not tiling): the end of a round, wobbly growth rings from a
                darker heart to pale sapwood inside a bark rim. Each piece maps its end cap onto
                the disc around the round's own centre, so the rings are concentric on the log.
  ps_husk       NEW: coconut husk, a dusty brown with a few broad, soft fibre strokes.
  ps_rope       NEW: a fat laid rope as three broad, soft twist bands at low contrast. pb_rope
                (the beach kit's) has fine yarns, right for thin lines and nets but fine
                patterning on a 5.6 cm anchor rope.

A multiply is carried by the MATERIAL (Unity: the material's colour), not baked into the texture;
so is the height-bump depth (BUMP), since every saved height map spans its full range.

UVs: one map "UVMap", WORLD SCALE, 1 UV unit = 2 m, V along every member (paddle, log, rope, split
wood), U around it; the stone is box projected; the end caps are the one exception (the whole disc).

⚠️ NO TWO SURFACES SHARE A PLANE (KANTO_DESIGN_GUIDE.md § 2). Everything resting on something is
dropped onto it by ray casts and then sunk 1 to 1.5 cm into it, and flat pieces are rolled a few
degrees off each other, so no face lies within 4 mm of a parallel face of another shell. check_prop
proves it and proves that every shell is in a chain of intersections down to the ground.
"""
import math
import os
import random
import subprocess
import sys
from pathlib import Path

try:
    import bmesh
    import bpy
    from mathutils import Matrix, Vector
    from mathutils.bvhtree import BVHTree
except ImportError:          # plain Python: the --paint mode only
    bpy = None

ROOT = Path(__file__).resolve().parents[1]
TOOLS = ROOT / "tools"
TEX = ROOT / "ArtSource" / "lagoon" / "textures"
LOGS = ROOT / "Logs" / "lagoon-blender"
PREFIX = "ps"

KINDS = ("oar_pair", "driftwood", "anchor_stone", "firewood")
UV_METRES = 2.0

# slot: (material name, texture, multiply in linear RGB or None)
SLOTS = {
    "timber": ("timber", "timber_a", None),
    "bamboo": ("bamboo", "bamboo_a", None),
    "rope": ("ps_rope", "ps_rope", None),
    "driftwood": ("ps_driftwood", "timber_b", (1.15, 1.12, 1.05)),
    "stone": ("ps_stone", "rock_a", (0.80, 0.82, 0.80)),
    "bark": ("ps_bark", "timber_c", None),
    "splitwood": ("ps_splitwood", "ps_splitwood", None),
    "endgrain": ("ps_endgrain", "ps_endgrain", None),
    "husk": ("ps_husk", "ps_husk", None),
}
SLOT_ORDER = list(SLOTS)
# Height-bump depth per slot, metres (uv_material's default is 0.03). A saved height map is always
# normalised to its full range, so the depth is set on the MATERIAL (Unity: the height strength).
# v3 review: at 3 cm the fat rope's soft twist bands rendered as hard candy stripes and the husks
# as ribbed shells; the painted albedo alone carries them at this size.
BUMP = {"rope": 0.006, "husk": 0.01, "splitwood": 0.015, "endgrain": 0.01}
NEW_TEXTURES = ("ps_splitwood", "ps_endgrain", "ps_husk", "ps_rope")

# Bevel per kind: (width m, angle limit degrees). The stone's wider bevel is what keeps the bored
# hole from reading as a drilled machine part; paddles stay crisp enough to keep their blade edge.
FINISH = {"oar_pair": (0.006, 35.0), "driftwood": (0.010, 35.0), "anchor_stone": (0.012, 35.0),
          "firewood": (0.008, 30.0)}

# ================================================================ painting (numpy, PIL; py -3)
# The house style (KANTO_DESIGN_GUIDE.md § 3): flat fills, a few LARGE feathered patches, soft
# hand-drawn strokes, low contrast, no grain, no noise, no cracks. Every texture here is its own
# drawing (LAGOON_REWORK_GUIDE.md § 2), not another generator recoloured.

SIZE = 1024
TILE_M = 2.0


def _np():
    import numpy as np
    return np


def hexcol(h):
    np = _np()
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], dtype=np.float32)


def field(sx_m, seed, sy_m=None, w=SIZE, h=SIZE, wm=TILE_M, hm=TILE_M):
    """A periodic smooth random field, unit variance, features about `sx_m` across and `sy_m` down
    the image (V, the grain direction)."""
    np = _np()
    sy_m = sx_m if sy_m is None else sy_m
    white = np.random.default_rng(seed).standard_normal((h, w))
    fy = np.fft.fftfreq(h)[:, None] * (h / hm) * sy_m
    fx = np.fft.fftfreq(w)[None, :] * (w / wm) * sx_m
    n = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-(fx ** 2 + fy ** 2) * 2)))
    return ((n - n.mean()) / (n.std() + 1e-9)).astype(np.float32)


def smooth(a):
    return a * a * (3 - 2 * a)


def mask(n, coverage, feather):
    """The top `coverage` of field `n` as a feathered shape: 0 outside, 1 inside."""
    np = _np()
    t = np.quantile(n, 1 - coverage)
    return smooth(np.clip((n - t) / feather + 0.5, 0, 1))


def over(img, colour, m):
    return img * (1 - m[..., None]) + colour * m[..., None]


def ps_splitwood():
    """Fresh split wood. Research (stylized hand-painted wood splits, e.g. the chopped-log props in
    painted fantasy kits): a split face is PALE, lighter than any bark or sawn board, with a few
    long soft fibre strokes along the grain and one or two broad warmer patches where the wood has
    started to weather. OWNER on v1 ("i dont like how detail some of the props are ... stylized
    semi-cartoony"): fewer, broader, softer strokes at low contrast, 2 to 3 cm wide and 20 to 50 cm
    long, so a face reads as one pale plane with a hint of grain."""
    np = _np()
    img = np.ones((SIZE, SIZE, 3), np.float32) * hexcol("d8b684")
    warm = mask(field(0.14, 11, 0.5) + 0.3 * field(0.05, 12, 0.2), 0.3, 0.6)
    img = over(img, hexcol("cda773"), warm * 0.7)
    light = mask(field(0.016, 13, 0.3), 0.14, 0.6)
    img = over(img, hexcol("e5c893"), light * 0.6)
    dark = mask(field(0.014, 14, 0.26), 0.09, 0.6)
    img = over(img, hexcol("c49a66"), dark * 0.55)
    height = light * 0.5 - dark * 0.6 + 0.2 * warm
    return img, height


def ps_endgrain():
    """The end of a split round, WHOLE (one disc across the image). Research (painted log-end
    textures in stylized kits): a FEW wobbly rings, not dozens; a darker warm heart, pale sapwood,
    a dark bark rim; the rings as soft lines at low contrast; no cracks (house rule). Four rings
    since v2 (the owner's simplify note): six read as fine patterning at the fat pieces' size."""
    np = _np()
    S = 512
    y, x = np.mgrid[0:S, 0:S].astype(np.float32)
    dx, dy = (x - S / 2) / (S * 0.475), (y - S / 2) / (S * 0.475)
    r = np.sqrt(dx * dx + dy * dy)
    a = np.arctan2(dy, dx)
    # the rings wobble: two slow angular waves, so they are hand-drawn and not compass circles
    rw = r * (1 + 0.035 * np.sin(3 * a + 0.7) + 0.02 * np.sin(5 * a + 2.1))
    img = np.ones((S, S, 3), np.float32) * hexcol("dcc28f")
    heart = smooth(np.clip((0.48 - rw) / 0.16 + 0.5, 0, 1))
    img = over(img, hexcol("c89c68"), heart * 0.8)
    height = np.zeros((S, S), np.float32)
    for k, rk in enumerate((0.2, 0.42, 0.62, 0.8)):
        line = np.exp(-((rw - rk) / 0.024) ** 2)
        img = over(img, hexcol("b48857"), line * 0.4)
        height -= line * 0.5
    bark = smooth(np.clip((r - 0.925) / 0.03 + 0.5, 0, 1))
    img = over(img, hexcol("6a4630"), bark)
    height += bark * 0.8
    return img, height


def ps_husk():
    """Coconut husk (bunot). Research (the husk segments piled by kitchens and smudge fires; the
    same subject in stylized tropical kits): a dusty brown outer skin torn into coarse fibres that
    run the length of the segment. Since v2 (the owner's simplify note) painted as a flat brown with
    a FEW broad, soft fibre strokes at low contrast and broad worn patches: the lump's shape says
    "husk", the texture only hints at the fibre."""
    np = _np()
    img = np.ones((SIZE, SIZE, 3), np.float32) * hexcol("80603f")
    worn = mask(field(0.08, 21, 0.22), 0.3, 0.6)
    img = over(img, hexcol("93714d"), worn * 0.75)
    light = mask(field(0.01, 22, 0.14), 0.16, 0.55)
    img = over(img, hexcol("a3825a"), light * 0.4)
    dark = mask(field(0.01, 23, 0.12), 0.1, 0.55)
    img = over(img, hexcol("6b4d32"), dark * 0.35)
    height = light * 0.6 - dark * 0.6 + 0.2 * worn
    return img, height


def ps_rope():
    """A fat laid rope, drawn for this kit because the owner's simplify note asks for no individual
    strands: pb_rope's fine twisted yarns are right for the beach kit's thin lines and nets, but on
    a 5.6 cm anchor rope they read as fine patterning. Research (stylized rope in painted kits,
    e.g. the reference's mooring ropes): three broad strands as soft diagonal bands, each with one
    lit side and a soft shaded groove, in a warm straw colour at low contrast. The twist repeats
    every 10 cm along V (20 times in the 2 m tile, so the tile wraps)."""
    np = _np()
    y, x = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32)
    u, v = x * TILE_M / SIZE, y * TILE_M / SIZE
    t = (u + v) / (TILE_M / 20)
    ph = t - np.floor(t)
    groove = np.exp(-((ph - 0.5) / 0.2) ** 2)
    lit = np.exp(-((ph - 0.2) / 0.22) ** 2)
    img = np.ones((SIZE, SIZE, 3), np.float32) * hexcol("c9a56e")
    img = over(img, hexcol("b3915c"), groove * 0.3)
    img = over(img, hexcol("d6b882"), lit * 0.22)
    patches = mask(field(0.12, 31, 0.4), 0.3, 0.6)
    img = over(img, hexcol("bb9760"), patches * 0.5)
    height = 1.0 - groove
    return img, height


PAINTERS = {"ps_splitwood": ps_splitwood, "ps_endgrain": ps_endgrain, "ps_husk": ps_husk, "ps_rope": ps_rope}
STRENGTH = {"ps_splitwood": 2.0, "ps_endgrain": 2.0, "ps_husk": 0.6, "ps_rope": 0.8}


def normal_from_height(h, strength):
    np = _np()
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * strength
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * strength
    n = np.dstack([-gx, gy, np.ones_like(h)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n * 0.5 + 0.5


def save(name, albedo, height):
    np = _np()
    from PIL import Image
    TEX.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(albedo, 0, 1) * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_albedo.png")
    rng = np.ptp(height)
    h = (height - height.min()) / rng if rng > 1e-9 else np.full_like(height, 0.5)
    Image.fromarray((h * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_height.png")
    nrm = normal_from_height(h, STRENGTH[name])
    Image.fromarray((nrm * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_normal.png")
    print("[prop-shore] painted", name)


def paint(names=None):
    for n in names or PAINTERS:
        save(n, *PAINTERS[n]())


def _ensure_textures():
    """Blender's Python has no PIL: paint any missing ps_ texture through the system Python."""
    missing = [n for n in NEW_TEXTURES if not (TEX / f"{n}_albedo.png").exists()]
    if missing:
        subprocess.run(["py", "-3", str(Path(__file__).resolve()), "--paint", *missing], check=True)


# ================================================================ geometry (Blender)

if bpy is not None:
    if str(TOOLS) not in sys.path:
        sys.path.insert(0, str(TOOLS))
    import author_lagoon_boats as BK          # loft, sweep, _catmull, _boolean, _uv_frame
    from render_lagoon_texture_preview import uv_material

    Z = Vector((0.0, 0.0, 1.0))
    DOWN = Vector((0.0, 0.0, -1.0))

    class Prop:
        """Collects the pieces of one prop. Every face carries its slot as a material index into
        SLOT_ORDER; `add` stamps a single-slot piece, a multi-surface piece arrives pre-stamped.
        The interface matches the boat kit's builder, so its sweep and loft work unchanged."""

        def __init__(self, kind, seed):
            self.kind, self.seed = kind, seed
            self.rng = random.Random(f"lagoon-prop-shore:{kind}:{seed}")
            self.pieces = []
            self.variant = ""

        def add(self, slot, pc):
            if pc is None or not pc.bm.faces:
                return None
            if slot is not None:
                idx = SLOT_ORDER.index(slot)
                for f in pc.bm.faces:
                    f.material_index = idx
            if pc not in self.pieces:
                self.pieces.append(pc)
            return pc

        def uv_off(self):
            return (self.rng.random(), self.rng.random())

        def drop(self, pc):
            """Remove a piece from the prop (it is being re-added after a transform)."""
            if pc in self.pieces:
                self.pieces.remove(pc)

    def xform(pc, M):
        bmesh.ops.transform(pc.bm, matrix=M, verts=pc.bm.verts)
        pc.bm.normal_update()
        return pc

    def bvh(pc):
        return BVHTree.FromBMesh(pc.bm)

    def zmin(pc, sel=None):
        return min(v.co.z for v in pc.bm.verts if sel is None or sel(v.co))

    def rest_on(pc, supports, sink):
        """DROP a piece straight down onto the ground (z = 0) or the first support below it, then
        sink it `sink` further in. Rays go both ways (down from every vertex and face centre of the
        piece, UP from every vertex of each support), because a ridge of one piece can meet the
        middle of a broad face of the other, where the face has no vertex to cast from."""
        gap = zmin(pc)
        pts = [v.co.copy() for v in pc.bm.verts] + [f.calc_center_median() for f in pc.bm.faces]
        for sup in supports:
            t = bvh(sup)
            for q in pts:
                hit = t.ray_cast(q + Z * 1e-4, DOWN, gap + 0.01)
                if hit[0] is not None:
                    gap = min(gap, hit[3] - 1e-4)
        me = bvh(pc)
        for sup in supports:
            for v in sup.bm.verts:
                hit = me.ray_cast(v.co - Z * 1e-4, Z, gap + 0.01)
                if hit[0] is not None:
                    gap = min(gap, hit[3] - 1e-4)
        return xform(pc, Matrix.Translation((0, 0, -(gap + sink))))

    def coplanar_with(pc, others):
        """True when a face of `pc` lies within 4 mm of, and parallel to (within 2.6 degrees), a
        face of any of `others`: check_prop's z-fight test, run while a pile is still being built
        so a piece that lands flat on flat can be re-rolled instead of shipped."""
        for o in others:
            t = bvh(o)
            for f in pc.bm.faces:
                c = f.calc_center_median()
                for q in [c] + [c.lerp(v.co, 0.7) for v in f.verts]:
                    for _loc, nrm, idx, _d in t.find_nearest_range(q, 0.004):
                        if idx is not None and abs(nrm.dot(f.normal)) > 0.999:
                            return True
        return False

    def top_at(supports, x, y):
        """The highest surface of `supports` over (x, y)."""
        best = None
        for sup in supports:
            hit = bvh(sup).ray_cast(Vector((x, y, 20.0)), DOWN, 40.0)
            if hit[0] is not None and (best is None or hit[0].z > best):
                best = hit[0].z
        return best

    def lean_on(pc, foot, h, contact_xy, supports, foot_len, sink_ground=0.015, sink_support=0.012):
        """Rest a long piece with its FOOT on the ground and its body across a support. The piece
        arrives lying along the horizontal direction `h` with its foot end at `foot`; it is pitched
        up about the foot (the axis Z x h) until its underside over `contact_xy` sinks
        `sink_support` into the support's top there, and kept with its foot sunk `sink_ground` into
        the ground. Bisection, because the underside over a point of a tapering, bent piece has
        no closed form."""
        base = [v.co.copy() for v in pc.bm.verts]
        axis = Z.cross(h).normalized()
        target = top_at(supports, *contact_xy) - sink_support
        cx, cy = contact_xy

        def pose(theta):
            R = Matrix.Rotation(-theta, 4, axis)
            pts = [foot + R @ (q - foot) for q in base]
            fz = min(q.z for q, q0 in zip(pts, base) if (q0 - foot).dot(h) < foot_len)
            return [q - Z * (fz + sink_ground) for q in pts]

        def underside(theta):
            for v, q in zip(pc.bm.verts, pose(theta)):
                v.co = q
            hit = bvh(pc).ray_cast(Vector((cx, cy, -20.0)), Z, 40.0)
            return hit[0].z if hit[0] is not None else -1.0
        lo, hi = 0.0, 0.9
        for _ in range(36):
            mid = (lo + hi) / 2
            if underside(mid) < target:
                lo = mid
            else:
                hi = mid
        for v, q in zip(pc.bm.verts, pose(hi)):
            v.co = q
        pc.bm.normal_update()
        return pc

    def tube(p, slot, pts, radius, sides=8, closed=False, dome=True, step=0.03):
        """A round member along `pts`. Open: the boat kit's sweep (parallel-transported frame,
        domed ends). CLOSED: a ring of rings with no caps, the seam welded, for a rope loop; the
        loops here are planar, so the loop's own plane normal is a frame that never twists."""
        if not closed:
            return BK.sweep(p, slot, pts, radius, sides=sides, step=step, dome=dome)
        P = [Vector(q) for q in pts]
        n = len(P)
        c = sum(P, Vector()) / n
        N = Vector()
        for i in range(n):
            N += (P[i] - c).cross(P[(i + 1) % n] - c)
        N.normalize()
        pc = BK.Piece()
        bm, uvl = pc.bm, pc.uv
        rings, vs = [], [0.0]
        for i in range(n):
            T = (P[(i + 1) % n] - P[i - 1]).normalized()
            e2 = T.cross(N).normalized()
            rings.append([bm.verts.new(P[i] + (N * math.cos(a) + e2 * math.sin(a)) * radius)
                          for a in (math.tau * k / sides for k in range(sides))])
            vs.append(vs[-1] + (P[(i + 1) % n] - P[i]).length)
        ou, ov = p.uv_off()
        arc = math.tau * radius / sides
        s = 1.0 / UV_METRES
        for i in range(n):
            j = (i + 1) % n
            for k in range(sides):
                k2 = (k + 1) % sides
                f = bm.faces.new((rings[i][k], rings[i][k2], rings[j][k2], rings[j][k]))
                for loop, (u, v) in zip(f.loops, ((k * arc, vs[i]), ((k + 1) * arc, vs[i]),
                                                  ((k + 1) * arc, vs[i + 1]), (k * arc, vs[i + 1]))):
                    loop[uvl].uv = (u * s + ou, v * s + ov)
        bm.normal_update()
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        return p.add(slot, pc)

    def blob(p, slot, centre, radii, segs=(10, 6), lobes=None):
        """A small rounded lump (a knot, a husk segment): a UV sphere scaled to `radii`, optionally
        pushed by `lobes(direction)` for an irregular hand-made shape; box-projected UVs."""
        pc = BK.Piece()
        bmesh.ops.create_uvsphere(pc.bm, u_segments=segs[0], v_segments=segs[1], radius=1.0)
        for v in pc.bm.verts:
            d = v.co.normalized()
            k = lobes(d) if lobes else 1.0
            v.co = Vector((d.x * radii[0], d.y * radii[1], d.z * radii[2])) * k
        pc.uv = pc.bm.loops.layers.uv.get("UVMap") or pc.bm.loops.layers.uv.new("UVMap")
        BK._uv_frame(pc, Vector((1, 0, 0)), Vector((0, 1, 0)), Z, p.uv_off())
        xform(pc, Matrix.Translation(centre))
        return p.add(slot, pc)

    def coil_path(start, heading, r0, r1, turns, z, spin=1):
        """A loose rope coil lying on the sand: from `start`, a spiral whose centre sits ahead of it
        along `heading`, winding inward from r0 to r1 (the gap between turns stays wider than the
        rope, so it never passes through itself)."""
        h = Vector((heading.x, heading.y, 0)).normalized()
        c = start + h * r0
        a0 = math.atan2(-h.y, -h.x)
        n = int(turns * 28)
        pts = []
        for i in range(1, n + 1):
            f = i / n
            a = a0 + spin * f * turns * math.tau
            r = r0 + (r1 - r0) * f
            pts.append(Vector((c.x + r * math.cos(a), c.y + r * math.sin(a), z)))
        return pts

    # ------------------------------------------------------------ oar_pair

    def smooth_s(a):
        a = max(0.0, min(1.0, a))
        return a * a * (3 - 2 * a)

    def paddle(p, L, leaf, tgrip):
        """A bangka paddle (sagwan) lying along +Y from its grip (y = 0) to its blade tip (y = L),
        the blade flat in the XY plane. Returns (pieces, shaft radius at fraction f).

        ORGANIC AND CHUNKY (the owner's two notes on v1: "stylized semi-cartoony", "experiment more
        with being organic ... the reference isnt just a straight rectangular prism"): the shaft
        swells from a 3.4 cm grip to a 4 cm throat and flows into a fat 26 to 30 cm blade with no
        step; the whole paddle bends a few centimetres in the blade's plane; the blade is a leaf
        (pointed) or a round spoon, its edge thick enough to stay round under the bevel; the grip
        is a fat knob or a thick, drooping T crossbar."""
        rng = p.rng
        fb = rng.uniform(0.5, 0.56)
        W = rng.uniform(0.13, 0.15)
        r0, r1 = rng.uniform(0.032, 0.035), rng.uniform(0.039, 0.042)
        bend = rng.uniform(0.025, 0.045) * rng.choice((-1, 1))

        def rs(f):
            return r0 + (r1 - r0) * smooth_s(f / fb)

        def rad(s, Ln):
            f = s / Ln
            if f < fb:
                r = rs(f)
                if not tgrip and f < 0.08:
                    r += 0.02 * smooth_s(1.0 - f / 0.08)
                return r, r
            g = (f - fb) / (1.0 - fb)
            w = r1 + (W - r1) * smooth_s(min(1.0, g / 0.4))
            if leaf and g > 0.5:
                w = max(0.02, w * (1.0 - ((g - 0.5) / 0.5) ** 1.6) ** 0.5)
            elif not leaf and g > 0.72:
                w = max(0.025, w * max(0.0, 1.0 - ((g - 0.72) / 0.28) ** 2) ** 0.5)
            t = r1 - (r1 - 0.024) * smooth_s(min(1.0, g / 0.35))
            return w, min(t, w)
        pts = [(bend * math.sin(math.pi * i / 40), L * i / 40, 0.0) for i in range(41)]
        pcs = [BK.sweep(p, "timber", pts, rad, sides=12, step=0.04, up=Z, dome=True)]
        if tgrip:
            # a thick crossbar whose ends droop a little, tapering to rounded ends
            # (v3 check: at y = 0.04 one of its facets lay within 4 mm of, and parallel to, the
            # grip's domed end; 0.05 and a turned section clear it)
            pcs.append(BK.sweep(p, "timber", [(-0.09, 0.05, -0.012), (0.0, 0.05, 0.004), (0.09, 0.05, -0.012)],
                                lambda s, Ln: ((0.03 - 0.008 * abs(2 * s / Ln - 1)),) * 2, sides=10, step=0.03,
                                dome=True, phase=0.3))
        return pcs, rs

    def _paddle_pieces(p):
        """One paddle: its pieces, its length and its shaft-radius function."""
        L = p.rng.uniform(1.4, 1.55)
        leaf = p.rng.random() < 0.6
        tgrip = p.rng.random() < 0.45
        pcs, rs = paddle(p, L, leaf, tgrip)
        return pcs, L, rs

    def merge(p, pcs):
        """Join several pieces into one piece (so a paddle and its crossbar move and rest as one)."""
        out = BK.Piece()
        for pc in pcs:
            vmap = {v: out.bm.verts.new(v.co) for v in pc.bm.verts}
            for f in pc.bm.faces:
                nf = out.bm.faces.new([vmap[v] for v in f.verts])
                nf.material_index = f.material_index
                for a, c in zip(f.loops, nf.loops):
                    c[out.uv].uv = a[pc.uv].uv
            p.drop(pc)
            pc.bm.free()
        out.bm.normal_update()
        p.pieces.append(out)
        return out

    def build_oar_pair(p):
        rng = p.rng
        pa, La, rsa = _paddle_pieces(p)
        pb, Lb, rsb = _paddle_pieces(p)
        A, B = merge(p, pa), merge(p, pb)
        if p.seed % 2:
            p.variant = "lying_crossed"
            # A lies flat: pitched about its width axis until its grip end and its blade tip touch
            # the sand together (the shaft is thicker than the blade, so lying level it would rest
            # on the shaft and hold the blade up in the air).
            def pitch_gap(t):
                R = Matrix.Rotation(t, 4, "X")
                pts = [R @ v.co for v in A.bm.verts]
                lo_grip = min(q.z for q, v in zip(pts, A.bm.verts) if v.co.y < 0.2 * La)
                lo_tip = min(q.z for q, v in zip(pts, A.bm.verts) if v.co.y > 0.8 * La)
                return lo_tip - lo_grip
            lo, hi = -0.2, 0.2
            for _ in range(40):
                mid = (lo + hi) / 2
                if pitch_gap(mid) > 0:
                    lo = mid
                else:
                    hi = mid
            xform(A, Matrix.Rotation(hi, 4, "X"))
            ya = rng.uniform(-0.25, 0.25)
            xform(A, Matrix.Rotation(ya, 4, "Z") @ Matrix.Translation((0, -La / 2, -zmin(A) - 0.015)))
            # The crossing: 30 to 40 % of the way down A's shaft from its grip.
            fa = rng.uniform(0.3, 0.4)
            da = Vector((-math.sin(ya), math.cos(ya), 0))
            P = da * (fa * La - La / 2)
            # B crosses at 50 to 65 degrees, its blade on the sand to one side and its shaft over A.
            yb = ya + math.radians(rng.choice((-1, 1)) * rng.uniform(50, 65))
            db = Vector((-math.sin(yb), math.cos(yb), 0))
            fbx = rng.uniform(0.28, 0.34)
            # lay B along db with its TIP (local y = Lb) at the foot, grip toward the crossing
            # the paddle is bent in plan: centre B's shaft on its own axis where it will cross A
            cxs = [v.co.x for v in B.bm.verts if abs(v.co.y - fbx * Lb) < 0.03]
            xform(B, Matrix.Translation((-(min(cxs) + max(cxs)) / 2, 0, 0)))
            xform(B, Matrix.Translation((0, -Lb, 0)))                  # tip to the origin
            xform(B, Matrix.Rotation(math.pi, 4, "Z"))                 # grip now toward +Y
            xform(B, Matrix.Rotation(yb, 4, "Z"))                      # grip toward db
            foot = P - db * ((1.0 - fbx) * Lb)
            xform(B, Matrix.Translation(foot))
            lean_on(B, foot, db, (P.x, P.y), [A], 0.22 * Lb)
        else:
            p.variant = "leaning_on_rest"
            # THE PADDLE REST: a fat bamboo pole laid in the forks of two stout timber stakes. Each
            # stake tapers, leans a little and has two thick, curving prongs; the pole sags a
            # centimetre between them (the second note: never a straight rectangular prism).
            zr = rng.uniform(0.66, 0.72)
            rr = 0.047
            half = rng.uniform(0.46, 0.5)
            sag = rng.uniform(0.01, 0.018)
            for sx in (-half, half):
                x = sx + rng.uniform(-0.02, 0.02)
                lean = rng.uniform(-0.035, 0.035)
                BK.sweep(p, "timber", [(x - lean, 0, -0.14), (x, 0, zr - rr + 0.014)],
                         lambda s, Ln: ((0.056 - 0.012 * s / Ln),) * 2, sides=10, step=0.1, dome=True)
                for sy, top in ((1, 0.08), (-1, 0.1)):
                    BK.sweep(p, "timber", BK._catmull([(x, 0.0, zr - 0.17), (x + 0.004, sy * 0.05, zr - 0.02),
                                                       (x + 0.01, sy * 0.085, zr + top)], per=4),
                             lambda s, Ln: ((0.032 - 0.01 * s / Ln),) * 2, sides=9, step=0.04, dome=True,
                             phase=0.2 * sy)
            rail = [(-half - 0.18, 0.0, zr + 0.004), (0.0, 0.006, zr - sag), (half + 0.18, 0.004, zr + 0.006)]
            BK.sweep(p, "bamboo", BK._catmull(rail, per=6), rr, sides=10, step=0.08, nodes=0.6, dome=True)

            def rail_z(x):
                u = x / (half + 0.18)
                return zr - sag * (1.0 - u * u)
            # Each paddle leans on the pole with its blade planted in the sand in front: its axis
            # passes 1.2 cm inside the pole's surface (so the two intersect), and the lean angle is
            # solved so the blade tip sinks 2 cm into the sand.
            for pc, L, rs, x in ((A, La, rsa, rng.uniform(-0.27, -0.18)), (B, Lb, rsb, rng.uniform(0.16, 0.25))):
                yaw = rng.uniform(-0.12, 0.12)
                fc = rng.uniform(0.2, 0.26)
                yc = fc * L
                rsc = rs(fc)

                def place(alpha, commit=False):
                    d = Vector((0, math.cos(alpha), -math.sin(alpha)))          # grip -> tip
                    n = Vector((0, math.sin(alpha), math.cos(alpha)))           # up off the pole
                    Q = Vector((x, 0, rail_z(x))) + n * (rr + rsc - 0.012)
                    M = (Matrix.Translation(Q) @ Matrix.Rotation(yaw, 4, "Z")
                         @ Matrix.Rotation(-alpha, 4, "X") @ Matrix.Translation((0, -yc, 0)))
                    if commit:
                        xform(pc, M)
                        return 0.0
                    return min((M @ v.co).z for v in pc.bm.verts)
                lo, hi = math.radians(12), math.radians(80)
                for _ in range(40):
                    mid = (lo + hi) / 2
                    if place(mid) > -0.02:
                        lo = mid
                    else:
                        hi = mid
                place(hi, commit=True)

    # ------------------------------------------------------------ driftwood

    def gnarled(p, slot, pts, rfun, sides=12, step=0.08, amp=0.07, seed=0.0, sink=None, end_len=0.14):
        """A fat round member whose radius varies gently AROUND it as well as along it: two slow
        lobes twisting along the length (a sea-worn trunk), and BOTH ENDS DOMED over `end_len` (a
        circular profile), so it reads as a rounded, cartoony log and never as a sawn one (v1's
        flat root cap read as a boot). `sink(s)`, if given, lifts each ring so the UNDOMED ring's
        lowest point sits that far under z = 0: the log beds into the sand along its length while
        its domed ends stay round instead of drooping into the ground. Lofted with the boat kit's
        loft (V along, U around)."""
        P = [Vector(q) for q in pts]
        cum = [0.0]
        for a, c in zip(P, P[1:]):
            cum.append(cum[-1] + (c - a).length)
        L = cum[-1]

        def at(s):
            s = max(0.0, min(L, s))
            for i in range(len(P) - 1):
                if cum[i + 1] >= s:
                    seg = cum[i + 1] - cum[i]
                    return P[i].lerp(P[i + 1], 0.0 if seg < 1e-9 else (s - cum[i]) / seg)
            return P[-1]
        n = max(2, int(math.ceil(L / step)))
        ss = [L * i / n for i in range(n + 1) if end_len < L * i / n < L - end_len]
        for d in (0.0, 0.02, 0.05, 0.09, end_len):
            ss += [d, L - d]
        ss = sorted(set(round(s, 5) for s in ss))
        rings, uvs, prev = [], [], None
        ou, ov = p.uv_off()
        for s in ss:
            T = (at(s + 0.01) - at(s - 0.01)).normalized()
            if prev is None:
                e1, e2 = BK._perp(T)
            else:
                e2 = (prev - T * prev.dot(T)).normalized()
                e1 = T.cross(e2).normalized()
                e2 = e1.cross(T).normalized()
            prev = e2
            r = rfun(s, L)
            d = min(s, L - s)
            dome = max(0.3, math.sqrt(max(0.0, 1.0 - (1.0 - min(1.0, d / end_len)) ** 2)))
            full = []
            for k in range(sides):
                a = math.tau * k / sides
                k1 = 1 + amp * (math.sin(2 * a + s * 2.3 + seed) * 0.8 + math.sin(3 * a - s * 3.1 + 2 * seed) * 0.5)
                full.append((e1 * math.cos(a) + e2 * math.sin(a)) * r * k1)
            c = at(s)
            if sink is not None:
                c = c + Vector((0, 0, -(c.z + min(q.z for q in full)) - sink(s)))
            ring = [c + q * dome for q in full]
            rings.append(ring)
            row, acc = [(0.0, s)], 0.0
            for a, b in zip(ring, ring[1:] + ring[:1]):
                acc += (b - a).length
                row.append((acc, s))
            uvs.append(row)
        return p.add(slot, BK.loft(rings, uvs, off=(ou, ov)))

    def build_driftwood(p):
        """OWNER on v1 (the simplify note): "no twig detail on driftwood". v1 had a flared, lobed
        root with a flat cap, three thin stubs and a forked twig. Now: ONE fat, gently bent log
        with rounded ends, at most one fat stub, and one fat plain branch across it."""
        rng = p.rng
        p.variant = "log_and_branch"
        Lg = rng.uniform(1.4, 1.8)
        R0 = rng.uniform(0.17, 0.2)
        # a soft S-bend in plan (v2 review: a straight bolster read as a cushion, not a trunk)
        bend = rng.uniform(0.1, 0.15) * rng.choice((-1, 1))
        ctrl = [(-Lg / 2 + Lg * i / 4, bend * (0, 1, 0.2, -0.8, -0.3)[i], 0.0) for i in range(5)]
        pts = BK._catmull(ctrl, per=6)
        swell = rng.uniform(0.22, 0.32)

        def rfun(s, L):
            f = s / L
            return R0 * (1.0 + swell * smooth_s(1.0 - f / 0.35)) * (1.0 - 0.4 * f)
        ph = rng.uniform(0, 6)
        log = gnarled(p, "driftwood", pts, rfun, amp=0.06, seed=rng.uniform(0, 6),
                      sink=lambda s: 0.045 + 0.012 * math.sin(s * 2.1 + ph))

        def axis_y(x):
            """The log's centre line (it wanders in plan) at x."""
            return min(pts, key=lambda q: abs(q.x - x)).y

        # ONE FAT BROKEN STUB on most seeds, rising out of the log's upper side.
        if rng.random() < 0.65:
            x = -Lg / 2 + Lg * rng.uniform(0.35, 0.7)
            top = top_at([log], x, axis_y(x)) or R0
            side = rng.choice((-1, 1))
            base = Vector((x, axis_y(x), top - 0.12))
            d = Vector((rng.uniform(-0.3, 0.3), side * rng.uniform(0.4, 0.7), 1.0)).normalized()
            ln = rng.uniform(0.2, 0.26)
            BK.sweep(p, "driftwood", [base, base + d * ln], lambda s, L: ((0.075 - 0.015 * s / L),) * 2,
                     sides=10, step=0.05, dome=True)
        # THE SMALLER BRANCH: fat and plain, its foot on the sand in front of the log, lying back
        # across it at 55 to 75 degrees.
        cx = -Lg / 2 + Lg * rng.uniform(0.45, 0.7)
        ang = math.radians(rng.uniform(55, 75))
        h = Vector((math.cos(ang) * rng.choice((-1, 1)), -math.sin(ang), 0.0))
        Lb = rng.uniform(0.9, 1.1)
        rb = rng.uniform(0.06, 0.07)
        reach = rng.uniform(0.2, 0.28)   # how far it runs past the contact
        cy = axis_y(cx)
        foot = Vector((cx, cy, 0.0)) - h * (Lb - reach)
        lateral = Vector((-h.y, h.x, 0))
        fc = (Lb - reach) / Lb
        bpts = []
        for i in range(7):
            f = i / 6
            q = foot + h * (Lb * f)
            q.z = rb
            # one gentle bend in plan, zero at the contact so it really lies across the log there
            q += lateral * (0.1 * math.sin((f - fc) * 2.6))
            bpts.append(q)
        branch = BK.sweep(p, "driftwood", bpts, lambda s, L: ((rb * (1.0 - 0.3 * s / L)),) * 2, sides=10,
                          step=0.06, dome=True)
        lean_on(branch, foot, h, (cx, cy), [log], 0.2)

    # ------------------------------------------------------------ anchor_stone

    def hug_loop(stone, c, e1, e2, off, n=36, a0=0.0, a1=math.tau, smooth_passes=2, closed=True):
        """Points round a section of the stone in the plane (c, e1, e2), each `off` metres beyond
        the surface along its ray from `c`: a rope lying ON the stone, whatever its shape (the
        groove, or the bridge between a bored hole and the stone's end)."""
        t = bvh(stone)
        pts = []
        m = n if closed else n + 1
        for k in range(m):
            a = a0 + (a1 - a0) * k / n
            d = e1 * math.cos(a) + e2 * math.sin(a)
            hit = t.ray_cast(c, d, 2.0)
            pts.append((hit[0] if hit[0] is not None else c + d * 0.2) + d * off)
        for _ in range(smooth_passes):
            if closed:
                pts = [(pts[i - 1] + pts[i] * 2 + pts[(i + 1) % len(pts)]) / 4 for i in range(len(pts))]
            else:
                pts = [pts[0]] + [(pts[i - 1] + pts[i] * 2 + pts[i + 1]) / 4 for i in range(1, len(pts) - 1)] + [pts[-1]]
        return pts

    def lazy_tail(S, h, rr, rng, length):
        """The rope's free end on the sand: one lazy S curve from S along `h`, never a tight coil
        (v1's spiral was the kind of fine detail the owner's simplify note cuts)."""
        h = Vector((h.x, h.y, 0)).normalized()
        lat = Vector((-h.y, h.x, 0))
        z = rr - 0.008
        amp = rng.uniform(0.07, 0.11) * rng.choice((-1, 1))
        out = []
        for i in range(1, 7):
            f = i / 6
            q = S + h * (length * f) + lat * (amp * math.sin(f * math.pi * 1.5))
            q.z = z
            out.append(q)
        return out

    RR = 0.028      # rope radius: a fat 5.6 cm rope, readable at 20 m (v1's 3.6 cm read as a thread)

    def build_anchor_stone(p):
        rng = p.rng
        rr = RR
        if p.seed % 2:
            p.variant = "grooved"
            # THE STONE: a fat egg lying on its side, revolved about X, a pecked groove round its
            # waist, squashed a little in height so it lies stably, with two slow lobes so it is a
            # picked beach stone, not a turned one.
            L = rng.uniform(0.56, 0.64)
            R = rng.uniform(0.21, 0.23)
            sq = rng.uniform(0.8, 0.86)
            egg = rng.uniform(0.08, 0.14)
            ph = rng.uniform(0, 6)
            rings, uvs = [], []
            nst, sides = 20, 16
            for i in range(nst + 1):
                t = math.pi * (0.04 + 0.92 * i / nst)
                x = -math.cos(t) * L / 2
                r = R * math.sin(t) * (1 + egg * x / (L / 2))
                r *= 1.0 - 0.15 * math.exp(-(x / 0.045) ** 2)
                ring = []
                for k in range(sides):
                    a = math.tau * k / sides
                    kk = 1 + 0.04 * math.sin(2 * a + ph + x * 4) + 0.025 * math.sin(3 * a - ph)
                    ring.append(Vector((x, math.cos(a) * r * kk, math.sin(a) * r * kk * sq)))
                rings.append(ring)
                row, acc = [(0.0, x)], 0.0
                for a_, c_ in zip(ring, ring[1:] + ring[:1]):
                    acc += (c_ - a_).length
                    row.append((acc, x))
                uvs.append(row)
            stone = p.add("stone", BK.loft(rings, uvs, off=p.uv_off()))
            xform(stone, Matrix.Rotation(rng.uniform(-0.25, 0.25), 4, "Z"))
            xform(stone, Matrix.Translation((0, 0, -zmin(stone) - 0.025)))
            # the stone's long axis after the yaw, from its first and last ring centres
            stone.bm.verts.ensure_lookup_table()
            c_lo = sum((v.co for v in stone.bm.verts[:sides]), Vector()) / sides
            c_hi = sum((v.co for v in stone.bm.verts[nst * sides:(nst + 1) * sides]), Vector()) / sides
            ax = (c_hi - c_lo).normalized()
            c = (c_lo + c_hi) / 2
            side = Z.cross(ax).normalized()                 # the stone's side toward +Y
            if side.y < 0:
                side = -side
            # THE LASHING: one fat rope ring bedded in the groove (sunk 1 cm into the stone) and
            # one chunky knot on top; the rope's free end drops over the front shoulder to the sand.
            ring = hug_loop(stone, c, side, Z, rr - 0.01, n=32)
            tube(p, "rope", ring, rr, sides=10, closed=True)
            top = max(ring, key=lambda q: q.z)
            knot = top + Z * 0.015
            blob(p, "rope", knot, (0.06, 0.05, 0.042), lobes=lambda d: 1 + 0.08 * math.sin(3 * d.x + 2 * d.y))
            off = rng.uniform(0.08, 0.1) * rng.choice((-1, 1))
            wrap = hug_loop(stone, c + ax * off, side, Z, rr - 0.008, n=8, a0=math.radians(75),
                            a1=math.radians(-10), closed=False, smooth_passes=1)
            S = wrap[-1] + side * 0.1
            S.z = rr - 0.008
            tail = [knot + ax * (off * 0.3)] + wrap + [S] + lazy_tail(S, side + ax * 0.5 * (1 if off > 0 else -1), rr,
                                                                    rng, rng.uniform(0.45, 0.6))
            tube(p, "rope", BK._catmull(tail, per=3), rr, sides=10, step=0.04)
        else:
            p.variant = "bored"
            # THE STONE: a fat, flat rounded slab with a big hole bored through near one end.
            sx, sy, sz = rng.uniform(0.3, 0.33), rng.uniform(0.23, 0.26), rng.uniform(0.13, 0.15)
            ph = rng.uniform(0, 6)
            pc = BK.Piece()
            bmesh.ops.create_uvsphere(pc.bm, u_segments=24, v_segments=14, radius=1.0)
            for v in pc.bm.verts:
                d = v.co.normalized()
                a = math.atan2(d.y, d.x)
                k = 1 + 0.06 * math.sin(2 * a + ph) + 0.03 * math.sin(3 * a - ph * 1.7)
                z = d.z * sz * (0.72 if d.z < 0 else 1.0)       # a flatter underside: it lies stably
                v.co = Vector((d.x * sx * k, d.y * sy * k, z))
            xh = sx * rng.uniform(0.5, 0.54)
            rh = rng.uniform(0.06, 0.065)
            cut = BK.Piece()
            bmesh.ops.create_cone(cut.bm, cap_ends=True, segments=16, radius1=rh, radius2=rh, depth=0.6)
            xform(cut, Matrix.Translation((xh, 0, 0)))
            stone = BK._boolean(pc, cut)
            BK._uv_frame(stone, Vector((1, 0, 0)), Vector((0, 1, 0)), Z, p.uv_off())
            p.add("stone", stone)
            xform(stone, Matrix.Translation((0, 0, -zmin(stone) - 0.022)))
            # THE LOOP: through the hole and round the stone's end, bedded 8 mm into it.
            xend = max(v.co.x for v in stone.bm.verts if abs(v.co.y) < 0.04)
            zmid = sum(v.co.z for v in stone.bm.verts) / len(stone.bm.verts)
            c = Vector(((xh + rh + xend) / 2, 0.0, zmid))
            loop = hug_loop(stone, c, Vector((1, 0, 0)), Z, rr - 0.008, n=32)
            tube(p, "rope", loop, rr, sides=10, closed=True)
            outer = max(loop, key=lambda q: q.x)
            knot = outer + Vector((0.015, 0, 0.0))
            blob(p, "rope", knot, (0.042, 0.06, 0.052), lobes=lambda d: 1 + 0.08 * math.sin(3 * d.y + 2 * d.z))
            S = knot + Vector((0.14, rng.uniform(0.05, 0.1), 0))
            S.z = rr - 0.008
            tail = [knot + Vector((0.02, 0.0, 0.0)), knot + Vector((0.08, 0.02, -0.025)), S]
            tail += lazy_tail(S, Vector((0.4, 1.0, 0)), rr, rng, rng.uniform(0.45, 0.6))
            tube(p, "rope", BK._catmull(tail, per=3), rr, sides=10, step=0.04)
            xform_all(p, Matrix.Rotation(rng.uniform(-0.3, 0.3), 4, "Z"))

    def xform_all(p, M):
        for pc in p.pieces:
            xform(pc, M)

    # ------------------------------------------------------------ firewood

    def split_piece(p, R, frac, length, roll):
        """One split piece of a round of radius R: a sector of `frac` of the circle, `length` long
        along +Y, centred on its own middle. ONE closed shell with THREE surfaces: bark on the arc
        (ps_bark), split faces on the flat sides (ps_splitwood) and end grain on both ends
        (ps_endgrain, mapped onto the whole disc around the round's own centre). The profile
        wobbles a little from ring to ring, so no two pieces are extrusions of one shape. Rolled by
        `roll` about its length."""
        rng = p.rng
        pc = BK.Piece()
        bm, uvl = pc.bm, pc.uv
        half = math.pi * frac
        narc = 8
        # profile: the apex (the round's centre, pulled out 1.5 cm so the edge is blunt, not a
        # knife) then the arc; edge k joins point k to k+1 (wrapping)
        if frac >= 1.0:
            # a whole, unsplit round: all bark, no split faces
            narc = 14
            base = [(R * math.cos(math.tau * k / narc), R * math.sin(math.tau * k / narc)) for k in range(narc)]
            slot_of_edge = ["bark"] * narc
        else:
            base = [(0.0, 0.015)]
            for k in range(narc + 1):
                a = math.pi / 2 - half + 2 * half * k / narc
                base.append((R * math.cos(a), R * math.sin(a)))
            slot_of_edge = ["splitwood"] + ["bark"] * narc + ["splitwood"]
        npf = len(base)
        nrings = max(5, int(length / 0.09))
        s = 1.0 / UV_METRES
        ou, ov = p.uv_off()
        rings = []
        wob = [rng.uniform(-1, 1) for _ in range(4)]
        taper = rng.uniform(0.08, 0.16) * rng.choice((-1, 1))
        bow = rng.uniform(0.01, 0.02) * rng.choice((-1, 1))
        for j in range(nrings + 1):
            y = -length / 2 + length * j / nrings
            f = j / nrings
            # ORGANIC (the owner's second note): each piece tapers toward one end, bows a centimetre
            # or two along its length and wobbles a little in section.
            k = (1.0 + 0.04 * math.sin(f * 3.1 + wob[0] * 3) * wob[1]) * (1.0 + taper * (f - 0.5))
            dx = 0.008 * math.sin(f * 2.3 + wob[2] * 3)
            dz = bow * math.sin(math.pi * f)
            rings.append([bm.verts.new(Vector((x * k + dx, y, z * k + dz))) for x, z in base])
        girth = [0.0]
        for i in range(npf):
            a, c = base[i], base[(i + 1) % npf]
            girth.append(girth[-1] + math.hypot(c[0] - a[0], c[1] - a[1]))
        for j in range(nrings):
            y0, y1 = rings[j][0].co.y, rings[j + 1][0].co.y
            for i in range(npf):
                i2 = (i + 1) % npf
                f = bm.faces.new((rings[j][i], rings[j][i2], rings[j + 1][i2], rings[j + 1][i]))
                f.material_index = SLOT_ORDER.index(slot_of_edge[i])
                u0, u1 = girth[i], girth[i + 1]
                for loop, (u, v) in zip(f.loops, ((u0, y0), (u1, y0), (u1, y1), (u0, y1))):
                    loop[uvl].uv = (u * s + ou, v * s + ov)
        rot = rng.uniform(0, math.tau)
        for ring in (rings[0], rings[-1]):
            f = bm.faces.new(ring)
            f.material_index = SLOT_ORDER.index("endgrain")
            for loop in f.loops:
                q = loop.vert.co
                x = q.x * math.cos(rot) - q.z * math.sin(rot)
                z = q.x * math.sin(rot) + q.z * math.cos(rot)
                loop[uvl].uv = (0.5 + 0.475 * x / (R * 1.02), 0.5 + 0.475 * z / (R * 1.02))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4], quad_method="BEAUTY",
                              ngon_method="BEAUTY")
        cz = sum(v.co.z for v in bm.verts) / len(bm.verts)
        cx = sum(v.co.x for v in bm.verts) / len(bm.verts)
        xform(pc, Matrix.Rotation(roll, 4, "Y") @ Matrix.Translation((-cx, 0, -cz)))
        return p.add(None, pc)

    def _roll_for(frac, rng):
        """A roll that lays a piece on a stable side: bark down, or one split face down, a few
        degrees off true so no face is parallel to the one under it."""
        half = math.pi * frac
        jitter = math.radians(rng.uniform(4, 9)) * rng.choice((-1, 1))
        if frac >= 1.0:
            return rng.uniform(0, math.tau)
        if rng.random() < 0.4:
            return math.pi + jitter                       # bark down, split faces up
        # a split face down: the radial face at angle (pi/2 - half) turned to face -Z
        a = math.pi / 2 - half
        return (a if rng.random() < 0.5 else -a) + jitter

    def build_firewood(p):
        """OWNER on v1 (the simplify note): "firewood as a few fat logs". v1 was a crib of up to
        twelve thin pieces and a tower of eleven small husks. Now: five or six FAT split pieces
        (quarters and thirds of a 25 to 28 cm round) in a lengthwise pile, and three to five fat
        husk segments beside it on most seeds."""
        rng = p.rng
        placed = []
        rows = (3, 2, 1) if p.seed % 2 else (3, 2)
        p.variant = "pile_" + "".join(str(r) for r in rows)
        # v2 review: pieces lying across the view read as planks. They now run front to back
        # (along Y), so the front of the pile is a face of ringed END GRAIN, the one thing that
        # says "firewood" from across the court; and about a third are whole unsplit rounds, so
        # the pile is fat logs and not boards.
        for k, count in enumerate(rows):
            for i in range(count):
                whole = rng.random() < 0.35
                R = rng.uniform(0.1, 0.11) if whole else rng.uniform(0.13, 0.145)
                frac = 1.0 if whole else rng.choice((0.25, 0.33, 0.5))
                Lp = rng.uniform(0.56, 0.66)
                off = (i - (count - 1) / 2) * 0.23 + rng.uniform(-0.02, 0.02)
                dy, yaw = rng.uniform(-0.05, 0.05), rng.uniform(-0.12, 0.12)
                # A piece that lands flat on a flat face of the one below (v3: a split face on a
                # bark facet, 1.5 degrees apart) is re-rolled, up to eight times.
                for attempt in range(8):
                    pc = split_piece(p, R, frac, Lp, _roll_for(frac, rng))
                    xform(pc, Matrix.Translation((off, dy, 3.0 + k)) @ Matrix.Rotation(yaw, 4, "Z"))
                    rest_on(pc, placed, 0.014)
                    if attempt == 7 or not coplanar_with(pc, placed):
                        break
                    p.drop(pc)
                    pc.bm.free()
                placed.append(pc)
        # THE HUSK PILE: fat coconut husk segments (bunot), each a curved, flattened lens dropped
        # onto the pile one at a time, beside the stack.
        if rng.random() < 0.8:
            side = rng.choice((-1, 1))
            cx, cy = side * rng.uniform(0.5, 0.56), rng.uniform(0.1, 0.2)
            husks = []
            n = rng.randint(3, 4)
            for i in range(n):
                a = rng.uniform(0, math.tau) if i else 0.0
                r = 0.0 if i == 0 else rng.uniform(0.13, 0.19)
                ln, wd, th = rng.uniform(0.15, 0.17), rng.uniform(0.09, 0.1), rng.uniform(0.06, 0.07)
                bend = rng.uniform(0.2, 0.3)
                pc = blob(p, "husk", Vector((0, 0, 0)), (ln, wd, th), segs=(12, 8),
                          lobes=lambda d: 1 + 0.05 * math.sin(3 * d.x + 1.3 * d.y))
                for v in pc.bm.verts:          # curve it like a peeled segment of the husk
                    v.co.z += -bend * (v.co.x / ln) ** 2 * th + bend * th * 0.5
                M = (Matrix.Translation((cx + r * math.cos(a), cy + r * math.sin(a), 3.0))
                     @ Matrix.Rotation(rng.uniform(0, math.tau), 4, "Z")
                     @ Matrix.Rotation(rng.uniform(-0.35, 0.35), 4, "X"))
                xform(pc, M)
                rest_on(pc, husks, 0.014)
                husks.append(pc)
            p.variant += "+husks"

    BUILDERS = {"oar_pair": build_oar_pair, "driftwood": build_driftwood, "anchor_stone": build_anchor_stone,
                "firewood": build_firewood}

    # ------------------------------------------------------------ assembly

    def _tint(m, rgb):
        """Multiply the base colour by `rgb`: a Mix node between the image and the BSDF."""
        nt = m.node_tree
        bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
        link = next((l for l in nt.links if l.to_socket == bsdf.inputs["Base Color"]), None)
        if link is None:
            return
        mix = nt.nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        mix.inputs["B"].default_value = tuple(rgb) + (1.0,)
        src = link.from_socket
        nt.links.remove(link)
        nt.links.new(src, mix.inputs["A"])
        nt.links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])

    def material(slot):
        name, texture, tint = SLOTS[slot]
        m = bpy.data.materials.get(name)
        if m is not None:
            return m
        m = bpy.data.materials.new(name)
        uv_material(m, texture)
        for n in m.node_tree.nodes:
            if n.type == "DISPLACEMENT" and slot in BUMP:
                n.inputs["Scale"].default_value = BUMP[slot]
        if tint:
            _tint(m, tint)
        return m

    def build_prop(kind, seed=1):
        """Build one prop of `kind` (see KINDS) from `seed` and return its Collection, NOT linked to
        any scene: one mesh parented to one root empty named `kind` at the ground-contact centre,
        +Y the front."""
        if kind not in BUILDERS:
            raise ValueError(f"unknown prop kind {kind!r}; one of {KINDS}")
        _ensure_textures()
        p = Prop(kind, seed)
        BUILDERS[kind](p)
        bm = bmesh.new()
        uv = bm.loops.layers.uv.new("UVMap")
        used = []
        for pc in p.pieces:
            vmap = {v: bm.verts.new(v.co) for v in pc.bm.verts}
            for f in pc.bm.faces:
                try:
                    nf = bm.faces.new([vmap[v] for v in f.verts])
                except ValueError:
                    continue
                slot = SLOT_ORDER[f.material_index]
                if slot not in used:
                    used.append(slot)
                nf.material_index = used.index(slot)
                for a, c in zip(f.loops, nf.loops):
                    c[uv].uv = a[pc.uv].uv
            pc.bm.free()
        # the footprint's centre to the origin (x, y only: z = 0 stays the ground)
        xs = [v.co.x for v in bm.verts]
        ys = [v.co.y for v in bm.verts]
        bmesh.ops.translate(bm, vec=(-(min(xs) + max(xs)) / 2, -(min(ys) + max(ys)) / 2, 0), verts=bm.verts)
        bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
        bm.normal_update()
        width, sharp = FINISH[kind]
        lim = math.radians(sharp)
        for f in bm.faces:
            f.smooth = True
        for e in bm.edges:
            e.smooth = not (len(e.link_faces) == 2 and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > lim)
        me = bpy.data.meshes.new(f"{kind}_{seed}")
        bm.to_mesh(me)
        box = [min(v.co.x for v in bm.verts), min(v.co.y for v in bm.verts), min(v.co.z for v in bm.verts),
               max(v.co.x for v in bm.verts), max(v.co.y for v in bm.verts), max(v.co.z for v in bm.verts)]
        bm.free()
        for slot in used:
            me.materials.append(material(slot))
        col = bpy.data.collections.new(f"prop_{kind}_{seed}")
        root = bpy.data.objects.new(kind, None)
        root.empty_display_type, root.empty_display_size = "PLAIN_AXES", 0.5
        col.objects.link(root)
        root["prop_kind"], root["prop_seed"], root["prop_mount"] = kind, seed, "ground"
        root["prop_variant"] = p.variant
        root["prop_box"] = [round(v, 3) for v in box]
        ob = bpy.data.objects.new(me.name, me)
        ob.parent = root
        col.objects.link(ob)
        bev = ob.modifiers.new("Bevel", "BEVEL")
        bev.width, bev.segments, bev.limit_method = width, 2, "ANGLE"
        bev.angle_limit = math.radians(sharp)
        bev.harden_normals = True
        bev.use_clamp_overlap = True
        return col

    # ------------------------------------------------------------ checks

    def check_prop(col):
        """Tri counts (base and bevelled), non-manifold edges, COPLANAR overlaps (parallel faces of
        different shells within 4 mm over each other's interior: z-fighting) and FLOATING shells
        (in no chain of intersections to a shell that reaches the ground, z <= 0.01)."""
        root = next(o for o in col.objects if o.parent is None)
        V, P, shell = [], [], []
        grounded_s = set()
        out = {"tris": 0, "tris_bevelled": 0, "nonmanifold": 0}
        sid = 0
        dg = bpy.context.evaluated_depsgraph_get()
        for ob in col.objects:
            if ob.type != "MESH":
                continue
            me = ob.data
            out["tris"] += sum(len(p.vertices) - 2 for p in me.polygons)
            try:
                em = ob.evaluated_get(dg).to_mesh()
                out["tris_bevelled"] += sum(len(p.vertices) - 2 for p in em.polygons)
                ob.evaluated_get(dg).to_mesh_clear()
            except RuntimeError:
                out["tris_bevelled"] = None       # not in a scene yet: no evaluated mesh
            bm = bmesh.new()
            bm.from_mesh(me)
            out["nonmanifold"] += sum(1 for e in bm.edges if not e.is_manifold)
            bm.free()
            parent = list(range(len(me.vertices)))

            def find(a):
                while parent[a] != a:
                    parent[a] = parent[parent[a]]
                    a = parent[a]
                return a
            for e in me.edges:
                ra, rb = find(e.vertices[0]), find(e.vertices[1])
                if ra != rb:
                    parent[ra] = rb
            roots, base_v = {}, len(V)
            V.extend(v.co.copy() for v in me.vertices)
            for poly in me.polygons:
                r = find(poly.vertices[0])
                if r not in roots:
                    roots[r] = sid
                    sid += 1
                s = roots[r]
                P.append([base_v + i for i in poly.vertices])
                shell.append(s)
                if any(V[base_v + i].z <= 0.01 for i in poly.vertices):
                    grounded_s.add(s)
        out["shells"] = sid
        tree = BVHTree.FromPolygons(V, P, epsilon=0.0)
        normals = [(V[p[1]] - V[p[0]]).cross(V[p[2]] - V[p[0]]).normalized() for p in P]
        pairs = {}
        for i, p in enumerate(P):
            n = normals[i]
            if n.length < 0.5:
                continue
            c = sum((V[k] for k in p), Vector()) / len(p)
            for q in [c] + [c.lerp(V[k], 0.7) for k in p]:
                for _loc, nrm, idx, _dist in tree.find_nearest_range(q, 0.004):
                    if idx is not None and shell[idx] != shell[i] and abs(nrm.dot(n)) > 0.999:
                        key = (min(shell[i], shell[idx]), max(shell[i], shell[idx]))
                        pairs.setdefault(key, tuple(round(x, 3) for x in c))
        out["coplanar"] = len(pairs)
        out["coplanar_examples"] = sorted(pairs.values())[:4]
        par = list(range(sid))

        def f2(a):
            while par[a] != a:
                par[a] = par[par[a]]
                a = par[a]
            return a
        for a, b in tree.overlap(tree):
            sa, sb = shell[a], shell[b]
            if sa != sb:
                ra, rb = f2(sa), f2(sb)
                if ra != rb:
                    par[ra] = rb
        grounded = {f2(s) for s in grounded_s}
        floating = [s for s in range(sid) if f2(s) not in grounded]
        out["floating"] = len(floating)
        out["box"] = list(root["prop_box"])
        return out

    # ------------------------------------------------------------ preview

    def _plain_mat(name, colour, rough=0.85):
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        bsdf.inputs["Base Color"].default_value = colour + (1,)
        bsdf.inputs["Roughness"].default_value = rough
        return m

    def _preview_scene():
        scene = bpy.context.scene
        g = bpy.data.meshes.new("sand")
        bm = bmesh.new()
        bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=200)
        bm.to_mesh(g)
        bm.free()
        g.materials.append(_plain_mat("sand_warm", (0.80, 0.62, 0.36)))
        scene.collection.objects.link(bpy.data.objects.new("sand", g))
        ref = bpy.data.meshes.new("scale_ref_1m60")
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=0.25, radius2=0.25, depth=1.6)
        bmesh.ops.translate(bm, vec=(0, 0, 0.8), verts=bm.verts)
        bm.to_mesh(ref)
        bm.free()
        ref.materials.append(_plain_mat("scale_pink", (0.95, 0.30, 0.55), 0.6))
        o = bpy.data.objects.new("scale_ref_1m60", ref)
        o.location = (7.2, 1.4, -0.02)
        scene.collection.objects.link(o)
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
        sun.data.energy, sun.data.color = 4.2, (1.0, 0.88, 0.72)
        sun.data.angle = math.radians(3)
        sun.rotation_euler = (math.radians(50), 0, math.radians(-145))
        scene.collection.objects.link(sun)
        world = bpy.data.worlds.new("world")
        world.use_nodes = True
        world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.55, 0.6, 0.66, 1)
        world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.8
        scene.world = world
        cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
        cam.data.clip_end = 500
        scene.collection.objects.link(cam)
        scene.camera = cam
        scene.render.engine = "BLENDER_EEVEE"
        scene.render.resolution_x, scene.render.resolution_y = 1600, 900
        scene.view_settings.view_transform = "AgX"
        for look in ("AgX - Punchy", "Punchy"):
            try:
                scene.view_settings.look = look
                break
            except TypeError:
                continue
        return cam

    # Fronts face +Y, so the cameras stand on +Y (the village kit's rule). Seed 1 (the odd
    # variants) in the front row, seed 2 (the even ones) behind.
    COLUMNS = {"oar_pair": 4.5, "driftwood": 1.5, "anchor_stone": -1.5, "firewood": -4.5}
    ROWS = {1: 0.0, 2: -2.8}
    SHOTS = [("lineup", (0.0, 9.0, 4.4), (0.0, -1.3, 0.3), 30),
             ("close", (3.0, 4.2, 1.8), (3.0, -1.2, 0.3), 32),
             ("close2", (-3.0, 4.2, 1.8), (-3.0, -1.2, 0.3), 32),
             ("far", (0.0, 20.0, 1.3), (0.0, -1.3, 0.4), 45),
             ("tight", (0.0, 7.6, 3.2), (0.0, -1.4, 0.25), 25)]

    def main():
        argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
        version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
        bpy.ops.wm.read_factory_settings(use_empty=True)
        top = bpy.data.collections.new("lagoon_props_shore")
        bpy.context.scene.collection.children.link(top)
        for kind in KINDS:
            for seed in ROWS:
                col = build_prop(kind, seed)
                top.children.link(col)
                c = check_prop(col)
                root = next(o for o in col.objects if o.parent is None)
                print(f"[prop-shore] {kind} {seed} ({root['prop_variant']}): {c}")
                root.location = (COLUMNS[kind], ROWS[seed], 0.0)
        if not version:
            return
        LOGS.mkdir(parents=True, exist_ok=True)
        cam = _preview_scene()
        scene = bpy.context.scene
        for tag, pos, tgt, lens in SHOTS:
            path = LOGS / f"{PREFIX}_{tag}_v{version}.png"
            if path.exists():
                raise SystemExit(f"[prop-shore] {path} exists: never overwrite a render, bump --preview")
            cam.location = pos
            cam.data.lens = lens
            cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
            scene.render.filepath = str(path)
            bpy.ops.render.render(write_still=True)
            print("[prop-shore] preview", path)


if __name__ == "__main__":
    if bpy is None:
        args = sys.argv[1:]
        if args and args[0] == "--paint":
            paint(args[1:] or None)
        else:
            # only the usage paragraph: the rest carries a warning sign a cp1252 console cannot print
            print(__doc__.split("\n\n")[0])
    else:
        main()
