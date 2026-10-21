"""Lagoon Court CLAY PROP KIT: water jars, pot clusters and potted plants, with world-scale UVs.

  py -3 tools/lagoon_prop_clay.py --paint                   # paint every pc_ texture
  py -3 tools/lagoon_prop_clay.py --paint pc_soil           # paint the named ones
  blender -b --python tools/lagoon_prop_clay.py -- --preview N

The Blender run builds every kind (two seeds each in a lineup), checks every seed 1..6, and with
--preview N renders in Logs/lagoon-blender/: pc_lineup_vN.png (every kind, two seeds each, and
the other potted plants behind, on warm sand beside a 1.6 m pink scale cylinder), pc_close_vN.png
(the jars up close), pc_close_plants_vN.png (the potted plants up close), pc_far_vN.png (the lineup
from 20 m at the game's eye height: the readability test) and pc_swatches_vN.png (the pc_ textures
flat: a detail crop and a 2 x 2 repeat). A missing texture is painted first under py -3 (Blender's
Python has no PIL). An existing render is never overwritten: bump N. Nothing is saved to a .blend:
the cove script builds these into its own file.

WHY (docs/LAGOON_REWORK_GUIDE.md § 7a items 1 and 8): the reference village (ArtStation GvJv5a)
is dense with pots and jars, stood by doors, clustered on piers, planted up on steps, and ours had
none. These are the FILIPINO versions: the banga (the household's terracotta water jar, often with
a wooden lid), the palayok (the round-bottomed clay cooking pot, which cannot stand on its own and
sits in a ring), the Ilocos burnay (dark stoneware storage jar), and a clay paso planted with the
plants every Filipino house keeps in a pot: croton, tanglad (lemongrass), gumamela, a monstera, a
trailing ground cover.

⚠️ SEMI-CARTOONY, FEW BIG SHAPES. OWNER, 2026-09-27, on the first prop renders: "i dont like how
details some of the props are. again we're going for a stylized semi-cartoony environment style".
Every vessel is a fat, exaggerated body with a thick rolled lip and at most one chunky lid; the
first round's shoulder beads, dipper, lug handles and fine texture patterning (throwing rings,
paddle dimples, soil crumbs) were cut, and every pot was made lopsided (organic()). Before and
after: pc_lineup_v3.png against pc_lineup_v5.png
(pc_beforeafter_v5.png puts them side by side).

IMPORTABLE WITH NO SIDE EFFECTS:

  build_prop(kind, seed=1) -> Collection   one root empty named `kind` (property prop_kind), every
                                           part parented to it, NOT linked anywhere
  check_prop(col) -> dict                  tris, non-manifold, coplanar overlaps, loose parts

KINDS, origin at the GROUND CONTACT CENTRE (z = 0 is the ground; a foot sinks 1.2 cm into it),
FRONT toward +Y. Sizes are typical; per seed they vary:

  banga         a terracotta water jar: a FAT round belly, a stubby neck, a thick rolled lip. Tall
                (0.72 m) or squat (0.6 m) by seed, ~0.75 m across; a chunky timber lid on some.
  pot_cluster   two or three vessels standing together, a banga always among them: a palayok in
                its fat abaca ring (a clay lid on some), a dark burnay, a small open pot; on some
                seeds one more small pot lies tipped over on its side in front.
                ~1.1 to 1.7 x 1.1 to 1.9 x 0.6 to 0.8 m.
  potted_plant  a clay pot (a tapered paso with a collar, a fat round planter, or a dark burnay
                planter) filled with soil 4 cm under the rim and planted with one plant from the
                cove's own planting kit (tools/lagoon_cove_planting.py), at pot scale: croton,
                tanglad (the grass tuft), gumamela (the flower bush), monstera or ground cover,
                cycling by seed. ~0.4 m pot, 0.65 to 1.1 m to the top of the plant.

MATERIAL SLOTS (exact names, one object per slot per prop, a live Bevel on the solids). REUSED by
name, so they are shared with the other kits in one file:
  timber                  the shared kit material (timber_a), as the boat and house kits: the lids
  pb_rope                 the beach kit's three-strand rope (tools/author_lagoon_props_beach.py,
                          whose --paint builds it if missing): the palayok's twisted ring
  plant parts             the planting kit's own lagoon_leaf_* / lagoon_stalk_* / lagoon_flower_*
NEW, their own drawings (prefix pc_), because nothing painted covers them:
  pc_clay       open-fired terracotta: a dark dusty brown-red, big soft dry and damp coats, the
                odd soft grey fire cloud from the bonfire
  pc_burnay     the Ilocos stoneware: near-black umber with big soft warm kiln-flash patches and
                a faint ash dusting
  pc_soil       damp potting soil: dark humus, big soft dry and damp patches, a few soft pebbles

UVs: one map "UVMap", 1 UV unit = 2 m (the house and boat kits' rule), with one exception: a
lathed vessel's girth wraps a WHOLE number of 2 m tiles (the nearest to its true girth, see
_wrap_tiles) so no painted patch breaks at the seam; V runs up the profile in metres. Lids and soil:
projected flat. Every piece: a random offset.

THE HOUSE STYLE (Art_Direction.md § 0; KANTO_DESIGN_GUIDE.md § 2, § 3; LAGOON_REWORK_GUIDE.md § 2):
chunky, slightly irregular hand-made vessels from a few clear forms. Every pot wobbles a little per
angle (hand-thrown, never a lathe-perfect circle) and the free-standing ones lean a degree or two;
the belly is drawn a size fatter than a real jar so the silhouette reads as a banga at 20 m. Planters
are HOLLOW (KANTO_DESIGN_GUIDE.md § 2: "floor, walls, soil 4 cm below the rim"); a jar's inside runs
down the neck and closes out of sight. ⚠️ ROLE HUES: nothing near offence orange #f87020 or defence
blue #0080e8; the terracotta is pc_clay's dark dusty brown-red, the burnay near black, no blue anywhere.

⚠️ NO TWO SURFACES SHARE A PLANE. Lids sit 6 mm INTO the lip they rest on, the soil disc runs 6 mm
into the pot wall, the palayok's belly 8 mm into its ring. `check_prop` proves it (coplanar: parallel faces of
different shells within 4 mm over each other) and proves nothing floats (loose: shells in no chain
of intersections to one that touches the ground). The plant is a shared planting-kit mesh of
see-through leaf cards and is checked on its own terms: it must start inside its pot, and it is
seated (_seat) so no leaf outside the pot's mouth dips under the rim.
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
except ImportError:      # plain Python: the texture painters only
    bpy = None

TOOLS = Path(__file__).resolve().parent
ROOT = TOOLS.parent
if str(TOOLS) not in sys.path:
    sys.path.insert(0, str(TOOLS))
SOURCE = ROOT / "ArtSource" / "lagoon"
TEX = Path(os.environ.get("LAGOON_TEX_OUT", SOURCE / "textures"))
PREVIEWS = ROOT / "Logs" / "lagoon-blender"
PREFIX = "pc"

KINDS = ("banga", "pot_cluster", "potted_plant")
TEXTURES = ("pc_clay", "pc_burnay", "pc_soil")
# Painted by the beach kit, borrowed here by name (its --paint makes it if it is missing).
BORROWED = ("pb_rope",)
UV_METRES = 2.0       # 1 UV unit = 2 m, as the house and boat kits


# ================================================================ painting (numpy, PIL)
# Run under py -3. Each painter returns (albedo HxWx3 in 0..1, height HxW).

def _np():
    import numpy as np
    return np


def _P():
    """lagoon_paint_materials: PLUMBING only (the 2 m periodic field, feathered patch, hand-drawn
    blob, the pixel grid in metres). None of its painters is called."""
    import lagoon_paint_materials as P
    return P


# Normal strength per texture: the owner asked that normal maps READ ("make sure it has
# depth/normal maps"); these drawings are broad and soft, so the push is gentle.
STRENGTH = {"pc_clay": 0.8, "pc_burnay": 0.8, "pc_soil": 2.0}


def _mix(img, col, m):
    return img * (1 - m[..., None]) + col * m[..., None]


def _cells(x, y, cell, seed):
    """A jittered cell layout for a few scattered hand-drawn marks (pebbles): per pixel, its
    cell's (fx, fy) position in the cell (0..1), the cell's random jitter and two random per-cell
    numbers. Rows are staggered half a cell, like brickwork, so the marks never line up. 2 m / cell
    is whole, so the layout tiles; every mark stays inside its own cell."""
    np = _np()
    n = int(round(2.0 / cell))
    rng = np.random.default_rng(seed)
    j = np.floor(y / cell).astype(int)
    xs = x + np.where(j % 2 == 1, cell / 2, 0.0)
    i = np.floor(xs / cell).astype(int)
    fx, fy = xs / cell - i, y / cell - j
    i, j = i % n, j % n
    jx, jy = rng.uniform(-1, 1, (n, n)).astype(np.float32), rng.uniform(-1, 1, (n, n)).astype(np.float32)
    r1, r2 = rng.random((n, n)).astype(np.float32), rng.random((n, n)).astype(np.float32)
    return fx, fy, jx[i, j], jy[i, j], r1[i, j], r2[i, j]


# OWNER, 2026-09-27, on the first prop renders: "i dont like how details some of the props are.
# again we're going for a stylized semi-cartoony environment style". So all three drawings are a
# flat base and a FEW BIG, SOFT, LOW-CONTRAST patches; the fine patterning of the first round (22
# then 14 throwing rings, paddle dimples on the burnay, crumbs and leaf bits in the soil) is gone.
# The texture suggests the material; the chunky shape carries the object.

# ---------------------------------------------------------------- pc_clay
# Research (Filipino palayok and banga; stylized pottery in hand-painted kits): open-fired,
# unglazed terracotta is a warm brown-red that dries LIGHTER and dustier in large soft patches, with
# the odd soft grey-brown FIRE CLOUD where the fuel touched the pot in the bonfire. Stylized kits
# paint exactly that and nothing finer. Kept dark and dusty: offence orange #f87020 is a gameplay
# colour, and this sits well under it in value and saturation.

def paint_clay():
    np, P = _np(), _P()
    img = np.broadcast_to(P.hexcol("905a40"), (P.SIZE, P.SIZE, 3)).astype(np.float32).copy()
    dry = P.patch(0.6, 0.35, 402, feather=1.0)
    img = P.tint(img, np.array([1.12, 1.11, 1.11]), dry)                           # dry, dusty
    img = P.tint(img, np.array([0.92, 0.91, 0.91]), P.patch(0.45, 0.16, 403, feather=1.0))  # damp
    cloud = P.patch(0.5, 0.06, 404, feather=1.2)
    img = _mix(img, P.hexcol("6a4a3a"), cloud * 0.35)                               # fire cloud
    return img, 0.4 * dry + 0.2 * P.field(0.6, 405)


# ---------------------------------------------------------------- pc_burnay
# Research (Ilocos burnay jars, Vigan): unglazed stoneware fired hot in a dragon kiln, a deep matte
# umber, nearly black, with large soft WARMER patches where the flame licked it (kiln flash) and a
# pale ASH dusting settled on the shoulders. Its own drawing, not pc_clay's: a dark ground with
# warm flashes rather than a red ground with dusty ones.

def paint_burnay():
    np, P = _np(), _P()
    img = np.broadcast_to(P.hexcol("3d2c23"), (P.SIZE, P.SIZE, 3)).astype(np.float32).copy()
    flash = P.patch(0.7, 0.3, 201, feather=1.1)
    img = P.tint(img, np.array([1.2, 1.15, 1.1]), flash)                            # kiln flash
    img = P.tint(img, np.array([0.9, 0.89, 0.89]), P.patch(0.5, 0.2, 203, feather=1.1))   # reduced
    ash = P.patch(0.45, 0.08, 205, feather=1.2)
    img = _mix(img, P.hexcol("6e6258"), ash * 0.25)                                 # settled ash
    return img, 0.3 * flash + 0.2 * P.field(0.6, 211)


# ---------------------------------------------------------------- pc_soil
# Research (stylized planters and garden beds in hand-painted kits): potting soil is a dark warm
# brown with two or three LARGE soft patches (damp darker, dry lighter) and a few PEBBLES, each a
# drawn shape. The pot shows about 0.4 m of it, so there are one or two pebbles a pot.

def paint_soil():
    np, P = _np(), _P()
    x, y = P.grid()
    img = np.broadcast_to(P.hexcol("4b372b"), (P.SIZE, P.SIZE, 3)).astype(np.float32).copy()
    dry = P.patch(0.3, 0.3, 301, feather=1.0)
    damp = P.patch(0.35, 0.25, 303, feather=1.0)
    img = P.tint(img, np.array([1.16, 1.13, 1.09]), dry)
    img = P.tint(img, np.array([0.84, 0.83, 0.83]), damp)
    height = 0.3 * dry - 0.2 * damp
    # Pebbles: 25 cm cells, one in four, a soft grey-tan stone with a lit top.
    fx, fy, jx, jy, r1, r2 = _cells(x, y + 0.01 * P.field(0.2, 307), 2.0 / 8, 308)
    rx, ry = 0.12 + 0.06 * r1, 0.09 + 0.05 * r2
    d = np.hypot((fx - 0.5 - 0.12 * jx) / rx, (fy - 0.5 - 0.12 * jy) / ry)
    on = ((r1 * 7.31 + r2 * 3.7) % 1.0) < 0.25
    peb = P.smooth(np.clip((1.0 - d) / 0.4, 0, 1)) * on
    pcol = _mix(np.broadcast_to(P.hexcol("7e705f"), img.shape).astype(np.float32).copy(),
                P.hexcol("9a8b76"), P.smooth(np.clip((0.45 - fy + 0.12 * jy) / 0.3, 0, 1)))
    img = _mix(img, pcol, peb * 0.9)
    return img, height + 1.2 * peb


PAINTERS = {"pc_clay": paint_clay, "pc_burnay": paint_burnay, "pc_soil": paint_soil}


def save_texture(name, albedo, height):
    """<name>_albedo.png, _height.png, _normal.png (OpenGL, green up): the same three files and
    conventions as the other Lagoon texture scripts."""
    np, P = _np(), _P()
    from PIL import Image
    TEX.mkdir(parents=True, exist_ok=True)
    albedo = np.clip(albedo, 0, 1)
    r = np.ptp(height)
    h = (height - height.min()) / r if r > 1e-9 else np.full_like(height, 0.5)
    Image.fromarray((albedo * 255 + 0.5).astype(np.uint8), "RGB").save(TEX / f"{name}_albedo.png")
    Image.fromarray((h * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_height.png")
    Image.fromarray((P.normal_from_height(h, STRENGTH[name]) * 255 + 0.5).astype(np.uint8)).save(
        TEX / f"{name}_normal.png")
    print("[prop-clay] painted", name)


def paint(names=None):
    for n in names or TEXTURES:
        save_texture(n, *PAINTERS[n]())


# ================================================================ modelling (bpy)

if bpy is not None:
    import author_lagoon_boats as BK      # noqa: E402  Piece, loft, sweep, _uv_frame, _catmull, _append
    import lagoon_cove_planting as PL     # noqa: E402  the cove's plant kit, reused at pot scale
    Z = Vector((0.0, 0.0, 1.0))
    X = Vector((1.0, 0.0, 0.0))
    Y = Vector((0.0, 1.0, 0.0))

# The shared kit materials reused by name, and the texture each is built from when the file has
# none yet (the cove's choices: timber_a and bamboo_a).
SHARED = {"timber": "timber_a", "bamboo": "bamboo_a"}
# Per slot: (bevel width m or None, angle above which an edge is sharp, harden normals). The
# vessels are smooth lathes: the bevel only softens the few real corners (a foot, a collar).
FINISH = {
    "pc_clay": (0.005, 40.0, False),
    "pc_burnay": (0.005, 40.0, False),
    "pb_rope": (None, 60.0, False),
    "timber": (0.004, 35.0, False),
    "bamboo": (0.004, 50.0, False),
    "pc_soil": (None, 60.0, False),
}


class Prop:
    """Collects pieces per material slot for one prop. Duck-types the boat kit's `Boat` (rng, add,
    uv_off), so author_lagoon_boats.sweep builds straight into it."""

    def __init__(self, kind, seed):
        self.kind, self.seed = kind, seed
        self.rng = random.Random(f"lagoon-propclay:{kind}:{seed}")
        self.pieces = {}
        self.info = {}
        self.plants = []            # (mesh, location, yaw, scale, soil z, rim z)

    def add(self, slot, pc):
        if pc is None or not pc.bm.faces:
            return None
        self.pieces.setdefault(slot, []).append(pc)
        return pc

    def uv_off(self):
        return (self.rng.random(), self.rng.random())

    def mark(self):
        return {s: len(v) for s, v in self.pieces.items()}

    def since(self, mark):
        return [pc for s, v in self.pieces.items() for pc in v[mark.get(s, 0):]]


def _xform(pc, M):
    if M is not None:
        bmesh.ops.transform(pc.bm, matrix=M, verts=pc.bm.verts)
        pc.bm.normal_update()
    return pc


def place_group(pieces, M, sink):
    """Turn a vessel built upright at the origin (with its lid or ring) by M, then drop it so
    its lowest point sits `sink` under the ground. Doing the grounding AFTER the lean means a pot
    tilted two degrees still has its low edge in the ground and its high edge just off it, as a
    real pot leaning on a stone would, instead of a foot hanging in the air."""
    for pc in pieces:
        _xform(pc, M)
    zmin = min(v.co.z for pc in pieces for v in pc.bm.verts)
    for pc in pieces:
        bmesh.ops.translate(pc.bm, vec=(0.0, 0.0, -sink - zmin), verts=pc.bm.verts)


def _wrap_tiles(max_r):
    """How many whole 2 m tiles a lathe's girth wraps: the nearest whole number to its widest
    circumference, at least one. WHY NOT PLAIN WORLD SCALE: a pot's girth is not a whole number of
    2 m tiles, so world-scale U broke every painted patch at the lathe's seam, and a dark jar showed
    a hard vertical line down its side (render v3, the burnay planter). Wrapping a whole number of
    tiles closes the seam; the cost is a patch drawn up to twice as wide on a small pot, which soft
    patches do not show."""
    return max(1, round(math.tau * max_r / UV_METRES))


def lathe(p, slot, prof, sides=20, wobble=None, caps=True, off=None, wrap=True):
    """A closed solid of revolution round Z through `prof` [(r, z), ...], both ends capped (the
    profiles start and end a hair off the axis, so the caps are tiny). U round the girth, a whole
    number of tiles (see _wrap_tiles; wrap=False gives plain world scale), V along the profile in
    metres. `wobble(a, z)` scales the radius per angle: hand-thrown."""
    ou, ov = off or p.uv_off()
    rings, uvs = [], []
    vacc, prev = 0.0, None
    angles = [math.tau * k / sides for k in range(sides)]
    girth = _wrap_tiles(max(r for r, _ in prof)) * UV_METRES
    for r, z in prof:
        if prev is not None:
            vacc += math.hypot(r - prev[0], z - prev[1])
        prev = (r, z)
        ring = []
        for a in angles:
            w = wobble(a, z) if wobble else 1.0
            ring.append(Vector((r * w * math.cos(a), r * w * math.sin(a), z)))
        rings.append(ring)
        arc = girth / sides if wrap else math.tau * r / sides
        uvs.append([(k * arc, vacc) for k in range(sides + 1)])
    return p.add(slot, BK.loft(rings, uvs, caps=caps, off=(ou, ov)))


def hoop(p, slot, R, z, tr, sides=24, tsides=8):
    """A fat round-section ring of radius R at height z, welded both ways (a proper torus): the
    rope ring a palayok sits in. V runs along the ring (rope's rule: V along its length), a whole
    number of tiles so the lay never steps at a seam; U round the section."""
    pc = BK.Piece()
    bm, uvl = pc.bm, pc.uv
    ou, ov = p.uv_off()
    s = 1.0 / UV_METRES
    loop = [(R + tr * math.cos(math.tau * k / tsides), z + tr * math.sin(math.tau * k / tsides))
            for k in range(tsides)]
    V = []
    for k in range(sides):
        a = math.tau * k / sides
        V.append([bm.verts.new((r * math.cos(a), r * math.sin(a), zz)) for r, zz in loop])
    arc, sec = _wrap_tiles(R) * UV_METRES / sides, math.tau * tr / tsides
    for k in range(sides):
        k2 = (k + 1) % sides
        for j in range(tsides):
            j2 = (j + 1) % tsides
            f = bm.faces.new((V[k][j], V[k2][j], V[k2][j2], V[k][j2]))
            for lp, (u, v) in zip(f.loops, ((k, j), (k + 1, j), (k + 1, j + 1), (k, j + 1))):
                lp[uvl].uv = (v * sec * s + ou, u * arc * s + ov)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return p.add(slot, pc)


def _wob(rng, amp=0.028):
    """Hand-thrown: two low harmonics round the pot, drifting with height, so no two pots are the
    same ellipse and none is a circle. 2.8 per cent (was 1.6): the owner's second note on the
    props asked for shapes that are organic rather than turned perfect."""
    ph = [rng.uniform(0, math.tau) for _ in range(2)]
    return lambda a, z: 1 + amp * math.sin(2 * a + ph[0] + z * 4) + amp * 0.6 * math.sin(3 * a + ph[1] + z * 2)


def organic(rng, h, amount=1.0):
    """A HAND-MADE LOPSIDEDNESS for a vessel h tall, built upright at the origin: returns a
    function taking a point to its deformed place. OWNER, 2026-09-27: "you should really experiment
    more with being organic in how you shape things ... the reference isnt just a straight
    rectangular prism". Three soft moves, each zero at the foot (so a pot still stands, and the
    palayok still sits in its ring) and growing up the pot:
      * the belly swells to one side (a potter's hand pressed harder there), up to 5 per cent;
      * the upper body SLUMPS sideways, up to 4 per cent of the height at the lip;
      * the mouth TILTS 2 to 5 degrees, so the lip is never a level ring.
    One continuous deformation of the whole group (pot, lid, soil) keeps every contact: a lid still
    sits 6 mm into its lip, the soil still runs into the wall."""
    phi, psi, chi = (rng.uniform(0, math.tau) for _ in range(3))
    A = rng.uniform(0.03, 0.05) * amount
    S = rng.uniform(0.02, 0.04) * h * amount
    T = math.tan(math.radians(rng.uniform(2.0, 5.0) * amount))
    sx, sy = S * math.cos(psi), S * math.sin(psi)

    def D(q):
        t = max(0.0, min(1.25, q.z / h))
        r, a = math.hypot(q.x, q.y), math.atan2(q.y, q.x)
        r *= 1 + A * math.cos(a - phi) * math.sin(math.pi * min(t, 1.0))
        x, y = r * math.cos(a) + sx * t * t, r * math.sin(a) + sy * t * t
        m = max(0.0, min(1.0, (t - 0.55) / 0.4))
        z = q.z + T * (q.x * math.cos(chi) + q.y * math.sin(chi)) * m * m * (3 - 2 * m)
        return Vector((x, y, z))
    return D


def deform(pieces, D):
    for pc in pieces:
        for v in pc.bm.verts:
            v.co = D(v.co)
        pc.bm.normal_update()


def _r_at(outer, z):
    """The outer radius at height z along a profile whose z only climbs (every vessel here)."""
    for (r0, z0), (r1, z1) in zip(outer, outer[1:]):
        if z0 <= z <= z1:
            return r0 + (r1 - r0) * ((z - z0) / (z1 - z0) if z1 > z0 else 0.0)
    return outer[-1][0] if z > outer[-1][1] else outer[0][0]


def vessel(p, slot, ctrl, wall=0.025, depth=0.12, lip=0.03, sides=22, wobble=None, per=3):
    """ONE thrown vessel as one welded solid: the outer wall through `ctrl` (Catmull-smoothed,
    `per` points a span; per=1 keeps a sharp collar), a THICK ROLLED LIP (a fat bead turned over at
    the mouth: the one feature that says "pot" at 20 m, so it is drawn big, 3 cm and more), then the
    inner wall one `wall` in, down `depth` under the lip and closed out of sight. A ctrl starting on
    the axis gives a round bottom (the palayok); otherwise the foot is flat.
    Returns (outer, info): info has the lip's top, the rim's outer radius and the mouth's radius."""
    pts = BK._catmull([Vector((r, z, 0)) for r, z in ctrl], per=per) if per > 1 else [Vector((r, z, 0)) for r, z in ctrl]
    outer = [(q.x, q.y) for q in pts]
    rl, top = outer[-1]
    prof = ([(0.012, ctrl[0][1])] if ctrl[0][0] > 0.03 else []) + outer
    lip_top = top + lip * 1.2
    prof += [(rl + lip * 0.45, top + lip * 0.15), (rl + lip * 0.7, top + lip * 0.6),
             (rl + lip * 0.55, top + lip * 1.05), (rl + lip * 0.1, lip_top), (rl - wall * 0.7, top + lip * 0.8)]
    zd = top - depth
    inner = [(max(0.02, r - wall), z) for r, z in reversed(outer) if z >= zd]
    prof += inner
    prof += [(0.012, inner[-1][1] - 0.012)]
    lathe(p, slot, prof, sides=sides, wobble=wobble)
    return outer, {"top": lip_top, "rim_r": rl + lip * 0.7, "mouth_r": rl - wall}


def lid(p, slot, R, zt, dome=0.05):
    """A chunky lid resting on a lip: a fat, rounded dome whose flat underside sits 6 mm down INTO
    the rolled lip it rests on (never on the lip's top plane), with one fat round knob, both big
    enough to read as a lid from the court. Projected flat UVs."""
    prof = [(0.012, zt - 0.006), (R - 0.008, zt - 0.006), (R, zt + 0.006), (R - 0.01, zt + 0.02),
            (R * 0.6, zt + dome * 0.85), (0.05, zt + dome), (0.05, zt + dome + 0.012),
            (0.055, zt + dome + 0.035), (0.035, zt + dome + 0.058), (0.012, zt + dome + 0.062)]
    pc = lathe(p, slot, prof, sides=20)
    BK._uv_frame(pc, X, Y, Z, p.uv_off())
    return zt + dome + 0.062


# ---------------------------------------------------------------- the vessels
# OWNER, 2026-09-27, on the first renders: "i dont like how details some of the props are. again
# we're going for a stylized semi-cartoony environment style". So every vessel is a FEW BIG shapes:
# a fat exaggerated body, a thick rolled lip, at most one chunky lid. Cut in that pass: the shoulder
# bead, the tabo (dipper) on the lid, the burnay's lug handles; the rope ring under the palayok stays
# because the pot cannot stand without it, and is drawn fat. Each vessel builds upright at the
# origin and returns its footprint radius; the caller leans, grounds and places it with place_group.
# Proportions are exaggerated (a real banga's belly is about 0.45 m across; these are 0.7 to 0.75)
# so the silhouette survives 20 m.

def build_banga_body(p, s=1.0, tall=None, dress=True):
    """A banga: a FAT round belly, a short stubby neck, a thick rolled lip. Tall or squat by seed; a
    chunky timber lid on some."""
    rng = p.rng
    tall = rng.random() < 0.5 if tall is None else tall
    if tall:
        ctrl = [(0.17, 0.0), (0.3, 0.07), (0.37, 0.26), (0.345, 0.44), (0.23, 0.58), (0.155, 0.63),
                (0.15, 0.665)]
    else:
        ctrl = [(0.18, 0.0), (0.32, 0.055), (0.385, 0.2), (0.36, 0.33), (0.235, 0.44), (0.165, 0.485),
                (0.16, 0.515)]
    sc = s * rng.uniform(0.95, 1.05)
    ctrl = [(r * sc, z * sc) for r, z in ctrl]
    mk = p.mark()
    outer, info = vessel(p, "pc_clay", ctrl, wall=0.025 * sc, depth=0.12 * sc, lip=0.034 * sc,
                         wobble=_wob(rng))
    top = info["top"]
    if dress and rng.random() < 0.45:          # a chunky timber lid
        top = lid(p, "timber", info["rim_r"] + 0.02, top, dome=0.045)
    deform(p.since(mk), organic(rng, ctrl[-1][1]))
    p.info.setdefault("tall", int(tall))
    return max(r for r, _ in outer) * 1.08, top


def build_palayok(p, s=1.0):
    """A palayok: the round-bottomed clay cooking pot, wide and low. It cannot stand on its own, so
    it sits in a fat twisted abaca ring (the dikin), its belly 8 mm into the ring. A clay lid on
    some."""
    rng = p.rng
    sc = s * rng.uniform(0.95, 1.06)
    zb = 0.02         # the bottom's centre, just off the ground inside the ring's hole
    ctrl = [(0.0, zb), (0.13, zb + 0.014), (0.225, zb + 0.07), (0.27, zb + 0.15), (0.245, zb + 0.23),
            (0.2, zb + 0.27), (0.195, zb + 0.29)]
    ctrl = [(r * sc, zb + (z - zb) * sc) for r, z in ctrl]
    mk = p.mark()
    outer, info = vessel(p, "pc_clay", ctrl, wall=0.022 * sc, depth=0.17 * sc, lip=0.032 * sc,
                         wobble=_wob(rng))
    # The ring: where the belly's surface passes z = ring top - 8 mm is where it bites in.
    tr = 0.036
    zr = tr - 0.012
    Rr = _r_at(outer, zr + tr - 0.008)
    top = info["top"]
    if rng.random() < 0.5:
        top = lid(p, "pc_clay", info["rim_r"] - 0.006, top, dome=0.05)
    # The ring is laid AFTER the lopsiding: the ring's bite is measured on the undeformed belly,
    # where the deformation is all but zero, and the ring itself must stay a level ring.
    deform(p.since(mk), organic(rng, ctrl[-1][1]))
    hoop(p, "pb_rope", Rr, zr, tr, sides=24, tsides=8)
    return max(max(r for r, _ in outer), Rr + tr) * 1.08, top


def build_burnay(p, s=1.0):
    """An Ilocos burnay: dark stoneware, a broad high shoulder over a tapering body, a short stubby
    neck and a thick lip. Its colour and its top-heavy shape are what tell it from a banga."""
    rng = p.rng
    sc = s * rng.uniform(0.95, 1.05)
    ctrl = [(0.15, 0.0), (0.23, 0.09), (0.3, 0.3), (0.32, 0.45), (0.24, 0.57), (0.14, 0.625),
            (0.13, 0.66)]
    ctrl = [(r * sc, z * sc) for r, z in ctrl]
    mk = p.mark()
    outer, info = vessel(p, "pc_burnay", ctrl, wall=0.025 * sc, depth=0.1 * sc, lip=0.03 * sc,
                         wobble=_wob(rng))
    deform(p.since(mk), organic(rng, ctrl[-1][1]))
    return max(r for r, _ in outer) * 1.08, info["top"]


def build_small_pot(p, s=1.0):
    """A small open pot (a kolon or water cup), squat and round, wide-mouthed, flat foot."""
    rng = p.rng
    sc = s * rng.uniform(0.94, 1.08)
    ctrl = [(0.11, 0.0), (0.18, 0.05), (0.205, 0.12), (0.18, 0.19), (0.15, 0.22), (0.15, 0.235)]
    ctrl = [(r * sc, z * sc) for r, z in ctrl]
    mk = p.mark()
    outer, info = vessel(p, "pc_clay", ctrl, wall=0.02 * sc, depth=0.13 * sc, lip=0.028 * sc,
                         wobble=_wob(rng, 0.032))
    deform(p.since(mk), organic(rng, ctrl[-1][1], 1.2))
    return max(r for r, _ in outer) * 1.08, info["top"]


# ---------------------------------------------------------------- the kinds

def build_banga(p):
    """One banga on its own, leaning a degree or two."""
    rng = p.rng
    mk = p.mark()
    R, top = build_banga_body(p)
    M = Matrix.Rotation(rng.uniform(0, math.tau), 4, "Z") @ Matrix.Rotation(rng.uniform(0.01, 0.03), 4, "X")
    place_group(p.since(mk), M, 0.012)
    p.info.update(radius=R, height=top)


def build_pot_cluster(p):
    """Two or three vessels standing together, a banga always among them (the household jar the
    rest gather round). Each is built upright, leaned, grounded, then placed on a ring round the
    banga with a 2 to 5 cm gap, the first free angle tried from a seeded start, so they huddle
    without touching. On some seeds one small pot lies tipped over on its side in front. Kept to a
    few big pots (the simplify pass): v3's four-plus-one clusters read as clutter at range."""
    rng = p.rng
    n = rng.choice((2, 3, 3))
    pool = ["palayok", "burnay", "small"]
    rng.shuffle(pool)
    kinds = ["banga"] + pool[:n - 1]
    lying = rng.random() < 0.4
    placed = []                              # (x, y, R)
    for i, kind in enumerate(kinds):
        mk = p.mark()
        if kind == "banga":
            R, _ = build_banga_body(p, s=rng.uniform(0.97, 1.05))
        elif kind == "palayok":
            R, _ = build_palayok(p, s=rng.uniform(0.9, 1.0))
        elif kind == "burnay":
            R, _ = build_burnay(p, s=rng.uniform(0.85, 0.97))
        else:
            R, _ = build_small_pot(p, s=rng.uniform(0.95, 1.1))
        tilt = 0.0 if kind == "palayok" else rng.uniform(0.0, 0.03)
        M = Matrix.Rotation(rng.uniform(0, math.tau), 4, "Z") @ Matrix.Rotation(tilt, 4, "X")
        place_group(p.since(mk), M, 0.012)
        if i == 0:
            x, y = 0.0, 0.0
        else:
            a0 = rng.uniform(0, math.tau)
            for k in range(36):
                a = a0 + (k // 2 + 1) * math.radians(14) * (1 if k % 2 == 0 else -1) if k else a0
                dist = placed[0][2] + R + rng.uniform(0.02, 0.05)
                x, y = math.cos(a) * dist, math.sin(a) * dist
                if all(math.hypot(x - qx, y - qy) > R + qr + 0.02 for qx, qy, qr in placed):
                    break
        for pc in p.since(mk):
            bmesh.ops.translate(pc.bm, vec=(x, y, 0.0), verts=pc.bm.verts)
        placed.append((x, y, R))
    if lying:
        mk = p.mark()
        R, _ = build_small_pot(p, s=rng.uniform(0.85, 0.95))
        # On its side, mouth out toward the front (+Y), rolled a little so it rests on its belly.
        M = Matrix.Rotation(rng.uniform(-0.5, 0.5), 4, "Z") @ Matrix.Rotation(-math.pi / 2 + 0.12, 4, "X")
        place_group(p.since(mk), M, 0.012)
        pcs = p.since(mk)
        xs = [v.co.x for pc in pcs for v in pc.bm.verts]
        ys = [v.co.y for pc in pcs for v in pc.bm.verts]
        half = max(max(xs) - min(xs), max(ys) - min(ys)) / 2
        cx0, cy0 = (max(xs) + min(xs)) / 2, (max(ys) + min(ys)) / 2
        front = max(qy + qr for qx, qy, qr in placed)
        x, y = rng.uniform(-0.2, 0.2), front + half + 0.04
        for pc in pcs:
            bmesh.ops.translate(pc.bm, vec=(x - cx0, y - cy0, 0.0), verts=pc.bm.verts)
        placed.append((x, y, half))
    # Centre the footprint on the origin.
    xs = [q[0] - q[2] for q in placed] + [q[0] + q[2] for q in placed]
    ys = [q[1] - q[2] for q in placed] + [q[1] + q[2] for q in placed]
    cx, cy = (max(xs) + min(xs)) / 2, (max(ys) + min(ys)) / 2
    for pcs in p.pieces.values():
        for pc in pcs:
            bmesh.ops.translate(pc.bm, vec=(-cx, -cy, 0.0), verts=pc.bm.verts)
    p.info.update(count=len(placed), lying=int(lying), kinds=",".join(kinds))


# The planters: (name, slot, outer ctrl, Catmull per). A paso is a tapered flowerpot with one thick
# COLLAR band under the lip (kept sharp, per=1): the shape every Filipino house keeps a plant in.
# The round planter is a cut-down fat banga; the burnay planter is the dark stoneware one.
PLANTERS = [
    ("paso", "pc_clay", [(0.15, 0.0), (0.16, 0.03), (0.2, 0.24), (0.206, 0.27), (0.245, 0.28),
                         (0.25, 0.345)], 1),
    ("round", "pc_clay", [(0.16, 0.0), (0.26, 0.06), (0.295, 0.16), (0.27, 0.27), (0.23, 0.31),
                          (0.225, 0.33)], 3),
    ("burnay", "pc_burnay", [(0.15, 0.0), (0.225, 0.07), (0.27, 0.2), (0.26, 0.3), (0.22, 0.345),
                             (0.215, 0.36)], 3),
]
# The plants, cycling by seed: (planting-kit type, its kit builder, kit variants usable, scale,
# habit). The cove's own plants at pot scale, so a potted croton is the croton on the rocks. The
# habit decides how the plant is seated (see _seat): a "stem" plant stands in the soil with its
# stems starting under it; a "ball" (gumamela, ground cover) has no stem to show and sits up at the
# rim, its lowest leaves spilling over it.
PLANTS = [
    ("croton", "_croton_mesh", (0, 1, 2, 3), 0.68, "stem"),
    ("tuft", "_tuft_mesh", (0, 1, 2, 3), 0.7, "stem"),
    ("bush", "_bush_mesh", (0, 2), 0.42, "ball"),        # even kit variants are gumamela
    ("monstera", "_monstera_mesh", (0, 1, 2, 3), 0.55, "stem"),
    ("groundcover", "_groundcover_mesh", (0, 1, 2, 3), 0.42, "ball"),
]


def _seat(mesh, scale, mouth_r, rim_h, habit):
    """How far over the soil to raise a plant, from its own mesh: far enough that no leaf outside
    the pot's mouth is under the rim (render v2's croton put its flat skirt of old leaves out
    THROUGH the pot wall), but for a stemmed plant never so far that its lowest point leaves the
    soil (the soil mound is 2 cm at its centre; the stems stay 5 mm in). Returns (lift, clip): clip
    is how far a leaf still dips under the rim outside the mouth (0 when it clears)."""
    pts = [v.co * scale for v in mesh.vertices]
    outside = [q.z for q in pts if math.hypot(q.x, q.y) > mouth_r - 0.03]
    need = max(0.0, rim_h + 0.005 - min(outside)) if outside else 0.0
    # A ball's lowest leaves stay 1.5 cm under the rim, INSIDE the pot (render v4's gumamela,
    # lifted clear of every clip, floated a hand over its pot).
    cap = (rim_h - 0.015 if habit == "ball" else 0.015) - min(q.z for q in pts)
    lift = max(0.0, min(need, cap))
    return lift, max(0.0, need - lift)


def potted_mesh(mesh, drop_z=-0.02):
    """The planting-kit mesh for a pot: a copy with every leaf CARD whose lowest point is under
    `drop_z` (mesh units) removed, cached by name. The croton skirts its stems with a ring of old,
    flat leaves at ground level, which on the rocks hides the stems and in a pot runs straight out
    through the wall (render v2) or, lifted clear, left the stems in mid air (render v4). Cards are
    found as connected islands of faces in a leaf slot (every slot but 0, the stalks); the stems and
    every upper leaf are kept exactly."""
    name = mesh.name + "_potted"
    got = bpy.data.meshes.get(name)
    if got is not None:
        return got
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.faces.ensure_lookup_table()
    seen, drop = set(), []
    for f in bm.faces:
        if f.index in seen or f.material_index == 0:
            continue
        island, stack = [], [f]
        seen.add(f.index)
        while stack:
            g = stack.pop()
            island.append(g)
            for e in g.edges:
                for h in e.link_faces:
                    if h.index not in seen and h.material_index != 0:
                        seen.add(h.index)
                        stack.append(h)
        if min(v.co.z for g in island for v in g.verts) < drop_z:
            drop += island
    bmesh.ops.delete(bm, geom=drop, context="FACES")
    me = mesh.copy()
    me.name = name
    bm.to_mesh(me)
    bm.free()
    return me


def build_potted_plant(p):
    """A planter, hollow, soil 4 cm under the lip, and one plant from the cove's planting kit."""
    rng = p.rng
    name, slot, ctrl, per = PLANTERS[rng.randrange(len(PLANTERS))]
    sc = rng.uniform(1.0, 1.1)
    ctrl = [(r * sc, z * sc) for r, z in ctrl]
    wall = 0.025 * sc
    mk = p.mark()
    top_z = ctrl[-1][1]
    soil = top_z - 0.04
    outer, info = vessel(p, slot, ctrl, wall=wall, depth=0.04 + 0.06, lip=0.03 * sc, sides=22,
                         wobble=_wob(rng, 0.022), per=per)
    ri = _r_at(outer, soil) - wall
    # The soil: a low mound, its edge 6 mm INTO the inner wall, its flat underside 1.2 cm down, over
    # the inner wall's hidden conical floor 5 to 6 cm under the soil (so no two faces share a plane).
    pc = lathe(p, "pc_soil", [(0.012, soil - 0.012), (ri + 0.006, soil - 0.012), (ri + 0.006, soil),
                              (ri * 0.7, soil + 0.012), (ri * 0.3, soil + 0.02), (0.012, soil + 0.022)], sides=22)
    BK._uv_frame(pc, X, Y, Z, p.uv_off())
    D = organic(rng, top_z, 0.8)
    deform(p.since(mk), D)
    M = Matrix.Rotation(rng.uniform(0, math.tau), 4, "Z") @ Matrix.Rotation(rng.uniform(0.0, 0.02), 4, "X")
    place_group(p.since(mk), M, 0.012)
    # Where the soil's surface centre and the rim ended up after the lopsiding, the lean and the
    # grounding.
    dz = min(v.co.z for pc in p.since(mk) for v in pc.bm.verts) + 0.012   # 0 unless the lean lifted it
    c = M @ D(Vector((0, 0, soil)))
    soil_z = c.z + dz
    rim = info["top"] + dz
    plant, fn, variants, scale, habit = PLANTS[(p.seed - 1) % len(PLANTS)]
    kit = PL._kit(plant, 4, getattr(PL, fn))
    mesh = kit[variants[rng.randrange(len(variants))]]
    if plant == "croton":
        mesh = potted_mesh(mesh)
    scale *= rng.uniform(0.92, 1.08)
    lift, clip = _seat(mesh, scale, info["mouth_r"], rim - soil_z, habit)
    p.plants.append((mesh, (c.x, c.y, soil_z + lift), rng.uniform(0, math.tau), scale, soil_z, rim))
    p.info.update(planter=name, plant=plant, pot_height=rim, pot_radius=max(r for r, _ in outer),
                  plant_lift=lift, plant_clip=clip)


BUILDERS = {"banga": build_banga, "pot_cluster": build_pot_cluster, "potted_plant": build_potted_plant}


# ---------------------------------------------------------------- materials and assembly

def _ensure_textures():
    """Paint what is missing: this kit's own pc_ textures here, the borrowed pb_ ones by the beach
    kit's own painter (they are its drawings; this kit never repaints them)."""
    mine = [t for t in TEXTURES if not (TEX / f"{t}_albedo.png").exists()]
    if mine:
        print("[prop-clay] painting missing textures:", mine)
        subprocess.run(["py", "-3", str(Path(__file__).resolve()), "--paint", ",".join(mine)], check=True)
    theirs = [t for t in BORROWED if not (TEX / f"{t}_albedo.png").exists()]
    if theirs:
        print("[prop-clay] painting the beach kit's missing textures:", theirs)
        subprocess.run(["py", "-3", str(TOOLS / "author_lagoon_props_beach.py"), "--paint", ",".join(theirs)],
                       check=True)


def prop_material(slot):
    """The slot's material, looked up by name first (so the house, boat and beach kits and this one
    share timber and pb_rope in one file), else built by the Lagoon UV material
    (albedo, normal, height bump) round its texture."""
    m = bpy.data.materials.get(slot)
    if m is not None:
        return m
    import render_lagoon_texture_preview as T
    m = bpy.data.materials.new(slot)
    T.uv_material(m, SHARED.get(slot, slot))
    return m


def build_prop(kind, seed=1):
    """Build one prop of `kind` (see KINDS) from `seed` and return its Collection, NOT linked to any
    scene: every part is parented to one root empty named `kind` at the ground contact centre,
    front toward +Y. The root carries prop_kind, prop_seed, prop_radius (the footprint's radius, for
    grounding), prop_top (the height of its top) and the builder's dimensions."""
    if kind not in BUILDERS:
        raise ValueError(f"unknown prop kind {kind!r}; one of {KINDS}")
    p = Prop(kind, seed)
    BUILDERS[kind](p)
    col = bpy.data.collections.new(f"{PREFIX}_{kind}_{seed}")
    root = bpy.data.objects.new(kind, None)
    root.empty_display_type, root.empty_display_size = "PLAIN_AXES", 0.4
    col.objects.link(root)
    root["prop_kind"], root["prop_seed"] = kind, seed
    for k, v in p.info.items():
        if isinstance(v, (int, float, str)):
            root[f"prop_{k}"] = round(v, 3) if isinstance(v, float) else v
    zmax, rmax, xs, ys = 0.0, 0.0, [], []
    for slot, pcs in p.pieces.items():
        bm = bmesh.new()
        uv = bm.loops.layers.uv.new("UVMap")
        for pc in pcs:
            BK._append(bm, uv, pc)
            pc.bm.free()
        bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
        bm.normal_update()
        width, sharp_deg, harden = FINISH[slot]
        lim = math.radians(sharp_deg)
        for f in bm.faces:
            f.smooth = True
        for e in bm.edges:
            e.smooth = not (len(e.link_faces) == 2 and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > lim)
        for v in bm.verts:
            zmax = max(zmax, v.co.z)
            rmax = max(rmax, math.hypot(v.co.x, v.co.y))
            xs.append(v.co.x)
            ys.append(v.co.y)
        me = bpy.data.meshes.new(f"{PREFIX}_{kind}_{seed}_{slot}")
        bm.to_mesh(me)
        bm.free()
        me.materials.append(prop_material(slot))
        ob = bpy.data.objects.new(me.name, me)
        ob.parent = root
        col.objects.link(ob)
        if width:
            bev = ob.modifiers.new("Bevel", "BEVEL")
            bev.width, bev.segments, bev.limit_method = width, 2, "ANGLE"
            bev.angle_limit = math.radians(sharp_deg)
            bev.harden_normals = harden
            bev.use_clamp_overlap = True
    for i, (mesh, loc, yaw, scale, soil_z, rim_z) in enumerate(p.plants):
        # The planting kit's shared mesh, linked (never copied), exactly as the cove places a plant:
        # one object with its own location, turn and scale. prop_part marks it for check_prop.
        ob = bpy.data.objects.new(f"{PREFIX}_{kind}_{seed}_plant{i}", mesh)
        ob.location, ob.rotation_euler, ob.scale = loc, (0.0, 0.0, yaw), (scale,) * 3
        ob.parent = root
        ob["prop_part"] = "plant"
        ob["prop_soil_z"], ob["prop_rim_z"] = round(soil_z, 3), round(rim_z, 3)
        col.objects.link(ob)
        pts = [ob.matrix_basis @ v.co for v in mesh.vertices]
        zmax = max(zmax, max(q.z for q in pts))
        rmax = max(rmax, max(math.hypot(q.x, q.y) for q in pts))
        xs += [q.x for q in pts]
        ys += [q.y for q in pts]
    root["prop_radius"] = round(rmax, 3)
    root["prop_top"] = round(zmax, 3)
    root["prop_size"] = (round(max(xs) - min(xs), 2), round(max(ys) - min(ys), 2), round(zmax, 2))
    return col


# ---------------------------------------------------------------- checks

def check_prop(col):
    """Tri counts (base and with the bevels), non-manifold edges, COPLANAR overlaps (parallel faces
    of different shells within 4 mm over each other: the z-fight), LOOSE shells (in no chain of
    intersections to a shell that touches the ground, lowest point at or under z = 4 mm). A planting
    kit plant is counted in the tris but checked on its own terms: its lowest point must lie under
    the pot's rim and its centre inside the pot (`plant_ok`), since leaf cards are open see-through
    surfaces the planting kit shingles on purpose."""
    V, P, shell, slot_of, shell_min = [], [], [], [], {}
    out = {"tris": 0, "tris_bevelled": 0, "tris_plant": 0, "nonmanifold": {}, "shells": 0, "plant_ok": None}
    sid = 0
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    for ob in col.objects:
        if ob.type != "MESH":
            continue
        me = ob.data
        tris = sum(len(q.vertices) - 2 for q in me.polygons)
        if ob.get("prop_part") == "plant":
            out["tris_plant"] += tris
            pts = [ob.matrix_basis @ v.co for v in me.vertices]
            low = min(q.z for q in pts)
            out["plant_ok"] = bool(low < ob["prop_rim_z"] and math.hypot(*ob.matrix_basis.translation.xy) < 0.1)
            out["plant_low"] = round(low, 3)
            continue
        out["tris"] += tris
        ev = ob.evaluated_get(dg)
        em = ev.to_mesh()
        out["tris_bevelled"] += sum(len(q.vertices) - 2 for q in em.polygons)
        ev.to_mesh_clear()
        slot = me.materials[0].name if me.materials else "?"
        bm = bmesh.new()
        bm.from_mesh(me)
        nm = sum(1 for e in bm.edges if not e.is_manifold)
        bm.free()
        if nm:
            out["nonmanifold"][slot] = nm
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
        roots = {}
        base_v = len(V)
        V.extend(ob.matrix_basis @ v.co for v in me.vertices)
        for q in me.polygons:
            r = find(q.vertices[0])
            if r not in roots:
                roots[r] = sid
                sid += 1
            s = roots[r]
            P.append([base_v + i for i in q.vertices])
            shell.append(s)
            slot_of.append(slot)
            shell_min[s] = min(shell_min.get(s, 9.0), min(V[base_v + i].z for i in q.vertices))
    out["shells"] = sid
    tree = BVHTree.FromPolygons(V, P, epsilon=0.0)
    normals = []
    for q in P:
        a, c, d = V[q[0]], V[q[1]], V[q[2]]
        normals.append((c - a).cross(d - a).normalized())
    where = {}
    for i, q in enumerate(P):
        n = normals[i]
        if n.length < 0.5:
            continue
        c = sum((V[k] for k in q), Vector()) / len(q)
        for pt in [c] + [c.lerp(V[k], 0.7) for k in q]:
            for _loc, nrm, idx, _dist in tree.find_nearest_range(pt, 0.004):
                if idx is not None and shell[idx] != shell[i] and abs(nrm.dot(n)) > 0.999:
                    key = (min(shell[i], shell[idx]), max(shell[i], shell[idx]))
                    where.setdefault(key, (slot_of[i], slot_of[idx], tuple(round(x, 3) for x in c)))
    out["coplanar"] = len(where)
    out["coplanar_examples"] = sorted(where.values())[:6]
    par = list(range(sid))

    def f2(a):
        while par[a] != a:
            par[a] = par[par[a]]
            a = par[a]
        return a
    for a, c in tree.overlap(tree):
        sa, sb = shell[a], shell[c]
        if sa != sb:
            ra, rb = f2(sa), f2(sb)
            if ra != rb:
                par[ra] = rb
    grounded = {f2(s) for s in range(sid) if shell_min[s] <= 0.004}
    loose = [s for s in range(sid) if f2(s) not in grounded]
    out["loose"] = len(loose)
    out["loose_examples"] = sorted({(slot_of[i], round(shell_min[s], 3)) for i, s in enumerate(shell) if s in loose})[:6]
    xs, ys, zs = [v.x for v in V], [v.y for v in V], [v.z for v in V]
    out["bbox"] = (round(max(xs) - min(xs), 2), round(max(ys) - min(ys), 2), round(min(zs), 3), round(max(zs), 2))
    return out


# ---------------------------------------------------------------- preview

def _flat_mat(name, colour, rough=0.8):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = colour + (1,)
    bsdf.inputs["Roughness"].default_value = rough
    return m


def _emit_mat(name, colour):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = colour + (1,)
    nt.links.new(em.outputs["Emission"], out.inputs["Surface"])
    return m


# (kind, seed, x, y, yaw degrees). The camera looks from +Y, so +X is on the LEFT of the picture:
# the front row runs banga, pot cluster, potted plant left to right, two seeds each (the cluster's
# seed 3 has the tipped-over pot); the back row shows the other three plants, so every plant the
# potted_plant cycle uses is in the picture.
LINEUP = [("banga", 1, 4.2, 0.0, 0), ("banga", 2, 3.1, 0.0, 0),
          ("pot_cluster", 1, 1.6, 0.0, 0), ("pot_cluster", 3, -0.2, -0.2, 0),
          ("potted_plant", 1, -1.9, 0.0, 0), ("potted_plant", 3, -3.0, 0.0, 0),
          ("potted_plant", 2, -1.3, -1.9, 0), ("potted_plant", 4, -2.4, -1.9, 0), ("potted_plant", 5, -3.5, -1.9, 0)]
SCALE_AT = (-4.6, 0.3)
SHOTS = [
    # (tag, camera, target, lens)
    ("lineup", (0.0, 8.6, 3.2), (-0.1, -0.5, 0.35), 32),
    ("close", (2.6, 3.1, 1.35), (2.2, 0.0, 0.35), 32),
    ("close_plants", (-2.4, 2.4, 1.5), (-2.4, -0.8, 0.4), 32),
    ("far", (0.0, 20.0, 1.25), (-0.1, 0.0, 0.5), 35),
]


def _preview_scene():
    scene = bpy.context.scene
    import render_lagoon_texture_preview as T
    ground = bpy.data.meshes.new("sand")
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=90)
    uvl = bm.loops.layers.uv.new("UVMap")
    for f in bm.faces:
        for lp in f.loops:
            lp[uvl].uv = (lp.vert.co.x / 4.0, lp.vert.co.y / 4.0)
    bm.to_mesh(ground)
    bm.free()
    sand = bpy.data.materials.new("preview_sand")
    T.uv_material(sand, "sand_a")
    ground.materials.append(sand)
    scene.collection.objects.link(bpy.data.objects.new("sand", ground))
    ref = bpy.data.meshes.new("scale_ref_1m60")
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=0.25, radius2=0.25, depth=1.6)
    bmesh.ops.translate(bm, vec=(0, 0, 0.8), verts=bm.verts)
    bm.to_mesh(ref)
    bm.free()
    ref.materials.append(_flat_mat("scale_pink", (0.95, 0.30, 0.55), 0.6))
    r = bpy.data.objects.new("scale_ref_1m60", ref)
    r.location = (SCALE_AT[0], SCALE_AT[1], -0.01)
    scene.collection.objects.link(r)
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.color = 4.5, (1.0, 0.9, 0.74)
    sun.data.angle = math.radians(3)
    sun.rotation_euler = (math.radians(48), 0, math.radians(150))     # from the camera side, upper left
    scene.collection.objects.link(sun)
    world = bpy.data.worlds.new("world")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.55, 0.6, 0.66, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 1.0
    scene.world = world
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 500
    scene.collection.objects.link(cam)
    scene.camera = cam
    scene.render.engine = "BLENDER_EEVEE"
    if hasattr(scene.eevee, "shadow_pool_size"):
        scene.eevee.shadow_pool_size = "1024"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    _look(scene, "AgX", ("AgX - Punchy", "Punchy"))
    return cam


def _look(scene, transform, looks):
    scene.view_settings.view_transform = transform
    for look in looks:
        try:
            scene.view_settings.look = look
            return
        except TypeError:
            continue


def _aim(cam, pos, tgt):
    cam.location = pos
    cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()


def _render(path):
    if path.exists():
        raise SystemExit(f"[prop-clay] {path} exists: never overwrite a render, bump --preview")
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    print("[prop-clay] preview", path)


def _swatch_mat(tex):
    """Unlit: exactly the albedo, for the swatch sheet."""
    m = bpy.data.materials.new(f"swatch_{tex}")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    uv = nt.nodes.new("ShaderNodeUVMap")
    uv.uv_map = "UVMap"
    img = nt.nodes.new("ShaderNodeTexImage")
    img.image = bpy.data.images.load(str(TEX / f"{tex}_albedo.png"), check_existing=True)
    img.interpolation = "Cubic"
    nt.links.new(uv.outputs["UV"], img.inputs["Vector"])
    nt.links.new(img.outputs["Color"], em.inputs["Color"])
    nt.links.new(em.outputs["Emission"], out.inputs["Surface"])
    return m


def _quad(name, x, y, w, h, uv_span, mat, z=0.0):
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new("UVMap")
    vs = [bm.verts.new((x + dx * w, y + dy * h, z)) for dx, dy in ((0, 0), (1, 0), (1, 1), (0, 1))]
    f = bm.faces.new(vs)
    for lp, (u, v) in zip(f.loops, ((0, 1), (1, 1), (1, 0), (0, 0))):
        lp[uvl].uv = (u * uv_span[0], v * uv_span[1])
    bm.to_mesh(me)
    bm.free()
    me.materials.append(mat)
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def _label(text, x, y, size, mat):
    cu = bpy.data.curves.new(f"lbl_{text[:12]}", "FONT")
    cu.body = text
    cu.size = size
    cu.align_x = "CENTER"
    ob = bpy.data.objects.new(cu.name, cu)
    ob.location = (x, y, 0.01)
    cu.materials.append(mat)
    bpy.context.scene.collection.objects.link(ob)
    return ob


# (texture, metres shown in the detail crop): about what one pot shows of it.
SWATCH_DETAIL = {"pc_clay": 0.8, "pc_burnay": 0.6, "pc_soil": 0.4}


def swatch_sheet(path):
    """The new pc_ textures flat and unlit: a DETAIL crop at the size one pot shows, and a 2 x 2
    REPEAT of the whole 2 m tile beside it, so any seam in the tiling shows."""
    scene = bpy.context.scene
    for o in scene.objects:
        o.hide_render = True
    ox = 1000.0
    board = _emit_mat("swatch_board", (0.95, 0.93, 0.88))
    ink = _emit_mat("swatch_ink", (0.2, 0.13, 0.08))
    cell, gap = 1.0, 0.18
    Wd = 2 * (cell + gap) + gap
    Ht = len(TEXTURES) * (cell + gap + 0.2) + gap + 0.3
    _quad("swatch_bg", ox - 0.2, -0.2, Wd + 0.4, Ht + 0.4, (1, 1), board, z=-0.01)
    for r, tex in enumerate(TEXTURES):
        y0 = Ht - (r + 1) * (cell + gap + 0.2)
        span = SWATCH_DETAIL[tex] / UV_METRES
        x0 = ox + gap
        m = _swatch_mat(tex)
        _quad(f"sw_{tex}_d", x0, y0, cell, cell, (span, span), m)
        _quad(f"sw_{tex}_r", x0 + cell + gap, y0, cell, cell, (2, 2), m)
        _label(f"{tex}  ({SWATCH_DETAIL[tex]:g} m crop)", x0 + cell / 2, y0 - 0.12, 0.075, ink)
        _label("2 x 2 tiles (4 m)", x0 + cell * 1.5 + gap, y0 - 0.12, 0.075, ink)
    cam = bpy.data.objects.new("swatch_cam", bpy.data.cameras.new("swatch_cam"))
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = max(Wd, Ht) + 0.6
    scene.collection.objects.link(cam)
    cam.location = (ox + Wd / 2, Ht / 2, 10.0)
    cam.rotation_euler = (0, 0, 0)
    scene.camera = cam
    scene.render.resolution_x = int(1000 * (Wd + 0.6) / (Ht + 0.6))
    scene.render.resolution_y = 1000
    _look(scene, "Standard", ("None",))
    _render(path)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    _ensure_textures()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    top = bpy.data.collections.new("lagoon_prop_clay")
    bpy.context.scene.collection.children.link(top)
    total = 0
    for kind, seed, x, y, yaw in LINEUP:
        col = build_prop(kind, seed)
        top.children.link(col)
        c = check_prop(col)
        root = next(o for o in col.objects if o.parent is None)
        info = {k[5:]: (tuple(v) if hasattr(v, "__len__") and not isinstance(v, str) else v)
                for k, v in root.items() if k.startswith("prop_")}
        print(f"[prop-clay] {kind} {seed}: {info}")
        print(f"[prop-clay]   {c}")
        total += c["tris_bevelled"] + c["tris_plant"]
        root.location = (x, y, 0.0)
        root.rotation_euler = (0, 0, math.radians(yaw))
    print(f"[prop-clay] lineup total tris (bevelled, plants included): {total}")
    # Every seed of every kind is checked too, not only the lineup's.
    bad = []
    for kind in KINDS:
        for seed in range(1, 7):
            col = build_prop(kind, seed)
            c = check_prop(col)
            if c["coplanar"] or c["loose"] or c["nonmanifold"] or c["plant_ok"] is False:
                bad.append((kind, seed, c["coplanar"], c["coplanar_examples"][:2], c["loose"], c["loose_examples"][:2],
                            c["nonmanifold"], c["plant_ok"]))
            print(f"[prop-clay] sweep {kind} {seed}: tris {c['tris_bevelled']} + plant {c['tris_plant']}, bbox {c['bbox']}")
            for o in list(col.objects):
                me = o.data if o.type == "MESH" and o.get("prop_part") != "plant" else None
                bpy.data.objects.remove(o)
                if me is not None:
                    bpy.data.meshes.remove(me)
            bpy.data.collections.remove(col)
    print("[prop-clay] seed sweep 1..6, failures:", bad if bad else "none")
    if not version:
        return
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    cam = _preview_scene()
    for tag, pos, tgt, lens in SHOTS:
        cam.data.lens = lens
        _aim(cam, pos, tgt)
        _render(PREVIEWS / f"{PREFIX}_{tag}_v{version}.png")
    swatch_sheet(PREVIEWS / f"{PREFIX}_swatches_v{version}.png")


if __name__ == "__main__":
    if bpy is None:
        args = sys.argv[1:]
        if "--paint" in args:
            i = args.index("--paint")
            names = args[i + 1].split(",") if i + 1 < len(args) and not args[i + 1].startswith("--") else None
            paint(names)
        else:
            print(__doc__)
    else:
        main()
