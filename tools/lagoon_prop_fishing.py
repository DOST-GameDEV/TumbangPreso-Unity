"""Lagoon Court FISHING PROP GROUP: drying racks, bubo fish traps and woven baskets.

  py -3 tools/lagoon_prop_fishing.py --paint [pf_trap,pf_buri]   # paint the pf_ textures
  py -3 tools/lagoon_prop_fishing.py --compare A B               # stack lineup vA over vB
  blender -b --python tools/lagoon_prop_fishing.py -- --preview N [--save]

With --preview N the Blender run renders, in Logs/lagoon-blender/ (never overwriting; bump N):
  pf_lineup_vN.png     every kind, seeds 1 to 3, on warm sand beside a 1.6 m pink scale cylinder
  pf_close_vN.png      the traps up close;  pf_close2_vN.png  the baskets up close
  pf_rackclose_vN.png  the drying racks up close
  pf_far_vN.png        the lineup from 20 m at the game's eye height: the readability test
--save also writes ArtSource/lagoon/lagoon_prop_fishing.blend. A missing pf_ texture is painted
first (under py -3, because Blender's Python has no PIL).

WHY (docs/LAGOON_REWORK_GUIDE.md § 7a item 1): the reference village (ArtStation GvJv5a) is dense
with hand-made props and ours had boats and laundry only. This group is the fish trade of a
Filipino shore village, three things a player should recognise at a glance:

  fish_rack  a low bamboo drying rack for tuyo and daing: four fat splayed bamboo legs, two fat
             rails, ONE chunky tray slab, and on it ONE soft slab painted with rows of dried fish,
             ridged along each row. Seed 1: a flat bamboo tray. Seed 2: the tray tilted toward
             the sun (longer back legs). Seed 3: a green net laid over the tray. ~2.0 x 1.2 x 0.7 m.
  bubo       bamboo fish traps, two or three together, the way they are stored between trips:
             fat woven cylinders and bell-like cones, each a SOLID body painted with the weave,
             one fat mouth hoop, one body band, a rope-tied knob at the tail and a dark funnel
             mouth. Seed 1: a cylinder standing mouth down, a cone leaning on it, a cone lying in
             front. Seed 2: two cylinders lying side by side, a cone resting on top. Seed 3: a
             standing cone, a small cone leaning on it, a cylinder lying in front, mouth toward
             +Y. Each trap 0.75 to 1.0 m long; the group ~1.7 x 1.5 x 1.1 m.
  basket     a round woven bilao tray and a squat bayong-style basket. Seed 1: a bilao of five fat
             dried fish beside a bayong heaped with shells. Seed 2: a bayong of shells with an
             empty bilao leaning on it. Seed 3: a bilao heaped with shells beside an empty bayong.
             ~1.2 x 0.8 x 0.6 m.

⚠️⚠️ SIMPLIFIED FOR THE STYLE (owner, 2026-09-27, on v1 and v2: "i dont like how details some of
the props are. again we're going for a stylized semi-cartoony environment style"). v2 built a
trap from a see-through lattice, four hoops and eight ribs, a rack from 17 slats and ~45 fish
meshes, and baskets holding ~25 separate clams. v3 onward: FEW BIG READABLE SHAPES, chunky and
rounded, and the painted texture carries the detail. Do not add the small parts back: a trap is
a solid body with two fat hoops and a knob; fish on a rack are a painted layer; shells are a
painted heap. Before and after: `py -3 tools/lagoon_prop_fishing.py --compare 2 3`.

API (importable with no side effects; bpy is only touched inside the functions):

  build_prop(kind, seed=1) -> Collection  NOT linked anywhere (the caller links it); one root
                                          empty named `kind` carrying "prop_kind", "prop_seed",
                                          "prop_layout"; every part parented to it
  KINDS                                   the three kinds above
  check_prop(col) -> dict                 tris, non-manifold solids, coplanar overlaps, floating
                                          shells, bounding box

ORIGIN AND FRONT: the root sits at the ground contact centre, z = 0 is the ground, +Y is the
front. Legs sink 3 cm, anything resting on the ground sinks 0.8 cm.

MATERIAL SLOTS (exact names; one object per slot per prop). REUSED before painting:
  bamboo      the shared house/boat material (bamboo_a): legs, rails, the tray, hoops, knobs
  pb_net      the beach kit's opaque green net: the net laid over the seed-3 tray
  pb_rope     the beach kit's manila rope: the leg lashings and the traps' tail ties
NEW, prefix "pf_", because nothing covered these surfaces (the beach kit's pb_rattan, pb_fish and
pb_bubo were withdrawn when it was simplified, so this file paints its own):
  pf_rattan   the bilao's SQUARE cane weave: chunky 7 cm honey-brown strips over and under on the
              grid, each rounded (a lit band, soft dark edges), darker and warmer than pf_buri
  pf_fish     ONE dried fish drawn as a SPRITE for the five fat fish in a bilao, in the same colours
              as the tuyo on pf_fishbed: U along the fish (snout at 0, tail fin from 0.84); V round
              the girth (0 the back, 0.25 the upper flank, 0.5 the belly, 0.75 the lower flank)
  pf_trap     a bubo's woven wall: broad split-bamboo slats along V (the trap's length) crossed
              by a few soft binding bands. Opaque: the wall is a solid body now, and pb_bubo's
              alpha lattice was exactly the fine see-through detail the owner rejected.
  pf_fishbed  rows of dried fish painted on a split-bamboo tray, WORLD SCALE on a top projection
              whose origin is the layer's own corner, so every fish is whole: rows 0.333 m apart
              (along Y), fish 0.125 m apart (along X), heads alternating per row. The slab under
              it is ridged along the same rows, so each row stands up as one chunky shape.
  pf_shells   a heap of big soft clam shapes (cream, lilac, pale sand), for mounds in a basket
  pf_buri     the bayong's weave: wide flat buri strips in a soft DIAGONAL plain weave, pale
              straw (pf_rattan is the bilao's square cane weave; the two must differ or the
              bayong reads as a deep bilao). Its ONE DYED BAND is not in the texture: the mesh
              carries a corner colour "buri_tint" (white outside the band, a deep red, leaf green
              or aubergine inside it) and the material MULTIPLIES the weave by it, the boats'
              "trim_tint" trick. In Unity the shader must multiply albedo by vertex colour.

UVs: one map "UVMap", WORLD SCALE, 1 UV unit = 2 m (the house, boat and beach kits' rule), except
the pf_fish sprite. Swept members: V along the member. Lathed bodies: U round the girth (each
ring's real perimeter), V along the profile. Slabs: projected from above. Random UV offsets
everywhere except pf_fishbed (whole fish) so neighbours never repeat the same stretch.

THE HOUSE STYLE (Art_Direction.md § 0, KANTO_DESIGN_GUIDE.md § 2 and § 3): chunky, rounded,
exaggerated (legs 10 cm across on a 0.6 m high rack, trap hoops 6 cm thick), a little
irregular (legs splay, traps wobble, mounds lump). Textures flat and hand-illustrated: flat
fills, one flat highlight band, large soft shapes, large feathered patches, low contrast, no fine
patterning, no grain, no noise, no streaks. ⚠️ ROLE HUES: nothing near offence orange #f87020 or
defence blue #0080e8. The fish are a dull golden brown (hue ~35, well under orange's saturation),
the dyes a deep red (hue ~355), a leaf green and an aubergine (hue ~290); no blue anywhere.

⚠️ NO TWO SURFACES SHARE A PLANE (KANTO_DESIGN_GUIDE.md § 2). Round members are eight-sided with a
VERTEX on top and bottom, so a rail under the tray meets it on a line, sunk 1.2 cm. Slabs resting
on slabs sink 1.2 cm, never 0. Things that lean are rotated until they TOUCH what they lean on (a
BVH search), then a little further, so they neither float nor sink through. `check_prop` proves
it: `coplanar` counts parallel faces of different shells within 4 mm over each other, `loose`
counts shells in no chain of intersections to one that touches the ground.
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
except ImportError:      # plain Python: the texture painters and --compare only
    bpy = None

TOOLS = Path(__file__).resolve().parent
ROOT = TOOLS.parent
if str(TOOLS) not in sys.path:
    sys.path.insert(0, str(TOOLS))
SOURCE = ROOT / "ArtSource" / "lagoon"
TEX = Path(os.environ.get("LAGOON_TEX_OUT", SOURCE / "textures"))
PREVIEWS = ROOT / "Logs" / "lagoon-blender"

PREFIX = "pf"
KINDS = ("fish_rack", "bubo", "basket")
TEXTURES = ("pf_trap", "pf_fishbed", "pf_shells", "pf_buri", "pf_rattan", "pf_fish")
UV_METRES = 2.0
FISH_ROW = 2.0 / 6        # pf_fishbed: 6 rows of fish per 2 m tile
FISH_STEP = 2.0 / 16      # and 16 fish per row


# ================================================================ painting (py -3: numpy, PIL)
# Each painter returns (albedo HxWx3 in 0..1, height HxW). Research for all four: the reference
# kit's props (GvJv5a) and hand-painted stylized prop sets read at a distance from a FEW large
# value shapes, one flat light band per form and soft dark tucks; the pattern is suggested by
# big shapes a few centimetres to a decimetre across, never by strands or fibres.

def _np():
    import numpy as np
    return np


def _P():
    """lagoon_paint_materials: PLUMBING only (the 2 m periodic field, feathered patch, pixel grid
    in metres, normal from height). None of its painters is called: every surface here is its
    own drawing (owner: "did you just repurpose the brick texture?")."""
    import lagoon_paint_materials as P
    return P


STRENGTH = {"pf_trap": 2.5, "pf_fishbed": 2.5, "pf_shells": 3.0, "pf_buri": 2.5, "pf_rattan": 3.5,
            "pf_fish": 2.0}


def _mix(img, col, m):
    return img * (1 - m[..., None]) + col * m[..., None]


# ---------------------------------------------------------------- pf_trap
# A bubo's wall is split-bamboo slats running the length of the trap, held by woven binding
# courses. Drawn BIG: 6.25 cm slats (32 round a 2 m tile), each a flat tone with one soft light
# band, the seams a soft low-contrast darkening; binding courses every 25 cm as a warm band with
# a lit upper edge. No crossing strands.
TRAP_SLATS = 32
TRAP_BANDS = 8


def paint_trap():
    np, P = _np(), _P()
    x, y = P.grid()
    fx = x / 2.0 * TRAP_SLATS + 0.12 * P.field(0.8, 501, 2.0)
    k = np.floor(fx)
    f = fx - k
    rng = np.random.default_rng(502)
    tone = rng.uniform(-1, 1, TRAP_SLATS)[(k.astype(int)) % TRAP_SLATS]
    img = np.broadcast_to(P.hexcol("cbb679"), (P.SIZE, P.SIZE, 3)).astype(np.float32).copy()
    img = P.tint(img, 1 + 0.035 * tone[..., None], np.ones_like(x))
    lit = P.smooth(np.clip((0.2 - np.abs(f - 0.4)) / 0.1, 0, 1))
    img = _mix(img, P.hexcol("dccb91"), lit * 0.7)
    seam = P.smooth(np.clip((0.16 - np.minimum(f, 1 - f)) / 0.12, 0, 1))
    img = _mix(img, P.hexcol("ad9960"), seam * 0.55)
    fy = y / 2.0 * TRAP_BANDS + 0.05 * P.field(0.6, 503)
    g = fy - np.floor(fy)
    band = P.smooth(np.clip((0.075 - np.abs(g - 0.5)) / 0.03 + 0.5, 0, 1))
    img = _mix(img, P.hexcol("a88a55"), band * 0.8)
    rim = P.smooth(np.clip((0.02 - np.abs(g - 0.5 + 0.07)) / 0.015 + 0.5, 0, 1))
    img = _mix(img, P.hexcol("d8c48c"), rim * 0.6)
    img = P.tint(img, np.array([0.94, 0.93, 0.88]), P.patch(0.7, 0.3, 504, feather=0.5) * 0.8)
    height = 0.5 + 0.25 * np.sin(np.pi * f) - 0.3 * seam + 0.35 * band
    return img, height


# ---------------------------------------------------------------- pf_fishbed
# Research (tuyo and daing laid out on bamboo trays in Philippine fishing towns; stylized food
# props): rows of small split fish, all the same size, heads one way per row; a golden-brown
# flank with a darker back edge, a pale belly edge, a dark eye and a darker tail. Painted as big
# soft silhouettes, 27 cm long and 10 cm wide, each with a soft shadow on the pale tray under it.

def P_smooth(np, t):
    t = np.clip(t, 0, 1)
    return t * t * (3 - 2 * t)


def _fish_mask(np, s, w):
    """(s along the fish, 0 snout to 1 tail tip; w across, -1..1 of its half width) -> body and
    tail masks, soft-edged, plus the signed distance across (for the bands)."""
    # a blunt round head swelling to full width at mid body, narrowing to a 0.3 wide tail root,
    # then the tail fin flaring out: one CONTINUOUS outline (v3's body pinched to nothing before
    # the tail and the fin floated free as a bow tie)
    sc = np.clip(s, 0, 1)
    head = np.sqrt(np.clip(np.sin(np.pi * np.minimum(sc, 0.5)), 0, 1))
    waist = 1 - 0.7 * P_smooth(np, (sc - 0.5) / 0.3)
    prof = np.where(sc < 0.5, head, np.where(sc < 0.8, waist, 0.3 + 0.75 * (sc - 0.8) / 0.2))
    inside = np.abs(w) - np.where((s >= 0) & (s <= 1), prof, -1.0)
    body = np.clip(0.5 - inside / 0.12, 0, 1) * (s < 0.82)
    tail = np.clip(0.5 - inside / 0.12, 0, 1) * (s >= 0.8)
    return body, tail


def paint_fishbed():
    np, P = _np(), _P()
    x, y = P.grid()
    # the tray: pale split bamboo along X, broad and soft
    fy = y / 0.05 + 0.1 * P.field(0.6, 601, 0.2)
    fr = fy - np.floor(fy)
    seam = P.smooth(np.clip((0.12 - np.minimum(fr, 1 - fr)) / 0.1, 0, 1))
    img = np.broadcast_to(P.hexcol("d9c58f"), (P.SIZE, P.SIZE, 3)).astype(np.float32).copy()
    img = _mix(img, P.hexcol("c2ad76"), seam * 0.45)
    height = 0.3 - 0.1 * seam
    rng = np.random.default_rng(602)
    L, Wd = 0.27, 0.105
    half = int(0.17 * P.PXM)
    ys, xs = np.mgrid[-half:half, -half:half].astype(np.float32) / P.PXM
    for r in range(6):
        for i in range(16):
            if rng.random() < 0.06:
                continue                                  # an empty spot: somebody took one
            cx = (i + 0.5) * FISH_STEP + rng.uniform(-0.008, 0.008)
            cy = (r + 0.5) * FISH_ROW + rng.uniform(-0.01, 0.01)
            ang = (math.pi / 2 if r % 2 else -math.pi / 2) + rng.uniform(-0.1, 0.1)
            ca, sa = math.cos(ang), math.sin(ang)
            ix = (np.arange(-half, half) + int(round(cx * P.PXM))) % P.SIZE
            iy = (np.arange(-half, half) + int(round(cy * P.PXM))) % P.SIZE
            win = np.ix_(iy, ix)

            def masks(dx, dy):
                s = (dx * ca + dy * sa) / L + 0.5
                w = (-dx * sa + dy * ca) / (Wd / 2)
                return s, w
            s0, w0 = masks(xs - 0.012, ys - 0.012)        # the shadow, offset down-right
            b0, t0 = _fish_mask(np, s0, w0)
            sh = np.clip(b0 + t0, 0, 1)
            sub = img[win]
            sub = _mix(sub, P.hexcol("b09a66"), sh * 0.55)
            s, w = masks(xs, ys)
            body, tail = _fish_mask(np, s, w)
            col = P.hexcol("a68352") * rng.uniform(0.95, 1.05)
            sub = _mix(sub, col, body)
            back = body * P.smooth(np.clip((w - 0.35) / 0.25, 0, 1))
            sub = _mix(sub, P.hexcol("76593a"), back * 0.85)
            belly = body * P.smooth(np.clip((-w - 0.45) / 0.25, 0, 1))
            sub = _mix(sub, P.hexcol("d4bd8d"), belly * 0.8)
            lit = body * P.smooth(np.clip((0.16 - np.abs(w - 0.08)) / 0.1, 0, 1)) * (s > 0.18) * (s < 0.72)
            sub = _mix(sub, P.hexcol("c4a46e"), lit * 0.7)
            sub = _mix(sub, P.hexcol("7a5c3a"), tail * 0.9)
            eye = np.clip(0.5 - (np.hypot((s - 0.1) * L, w * Wd / 2 - 0.006) - 0.011) / 0.004, 0, 1)
            sub = _mix(sub, P.hexcol("2b2118"), eye)
            img[win] = sub
            hsub = height[win]
            hsub = np.maximum(hsub, 0.3 + 0.55 * np.clip(body + tail * 0.6, 0, 1)
                              * np.sqrt(np.clip(1 - np.abs(w), 0, 1)))
            height[win] = hsub
    img = P.tint(img, np.array([0.95, 0.94, 0.9]), P.patch(0.8, 0.25, 603, feather=0.5) * 0.7)
    return img, height


# ---------------------------------------------------------------- pf_shells
# Research (market bilao of lukan and tulya clams; stylized shell piles): a heap reads as big
# overlapping fans, cream, lilac and pale sand, each with three or four broad soft rib strokes
# out from the hinge and a lighter lip, dark soft gaps between them where the heap goes deep.

def paint_shells():
    np, P = _np(), _P()
    rng = np.random.default_rng(701)
    img = np.broadcast_to(P.hexcol("a99378"), (P.SIZE, P.SIZE, 3)).astype(np.float32).copy()
    height = np.zeros((P.SIZE, P.SIZE), np.float32)
    pal = [P.hexcol(h) for h in ("efe3c9", "f3ebdc", "dcb9c4", "d3c1a2", "e8d6c0")]
    half = int(0.075 * P.PXM)
    ys, xs = np.mgrid[-half:half, -half:half].astype(np.float32) / P.PXM
    n = 24
    pts = [((i + 0.5 + rng.uniform(-0.35, 0.35)) * 2.0 / n, (j + 0.5 + rng.uniform(-0.35, 0.35)) * 2.0 / n)
           for i in range(n) for j in range(n)]
    rng.shuffle(pts)
    for cx, cy in pts:
        R0 = rng.uniform(0.05, 0.065)
        ang = rng.uniform(0, math.tau)
        ca, sa = math.cos(ang), math.sin(ang)
        ix = (np.arange(-half, half) + int(round(cx * P.PXM))) % P.SIZE
        iy = (np.arange(-half, half) + int(round(cy * P.PXM))) % P.SIZE
        win = np.ix_(iy, ix)
        a = xs * ca + ys * sa + R0 * 0.45              # along, from the hinge
        b = -xs * sa + ys * ca
        r = np.hypot(a, b)
        phi = np.arctan2(b, a)
        fan = np.clip(0.5 - (r - R0) / 0.006, 0, 1) * np.clip(0.5 - (np.abs(phi) - 1.15) / 0.08, 0, 1)
        hinge = np.clip(0.5 - (np.hypot(a - R0 * 0.08, b) - R0 * 0.22) / 0.006, 0, 1)
        m = np.maximum(fan, hinge)
        sub = img[win]
        col = pal[rng.integers(len(pal))]
        dark = np.clip(0.5 - (r - R0 - 0.008) / 0.012, 0, 1) * np.clip(0.5 - (np.abs(phi) - 1.3) / 0.15, 0, 1)
        sub = _mix(sub, P.hexcol("8f7a62"), dark * 0.35 * (1 - m))    # the soft gap shadow round it
        sub = _mix(sub, col, m)
        rib = m * P.smooth(np.clip((-np.cos(phi * 4.5) - 0.3) / 0.5, 0, 1)) * np.clip(r / R0 * 2 - 0.4, 0, 1)
        sub = _mix(sub, col * 0.86, rib * 0.7)
        lip = m * P.smooth(np.clip((r / R0 - 0.8) / 0.12, 0, 1))
        sub = _mix(sub, np.minimum(col * 1.06, 1.0), lip * 0.6)
        img[win] = sub
        hsub = height[win]
        height[win] = np.where(m > 0.5, np.maximum(hsub * 0.4 + 0.3, 0.6 + 0.4 * (1 - np.clip(r / R0, 0, 1)) - 0.12 * rib), hsub)
    img = P.tint(img, np.array([0.95, 0.94, 0.92]), P.patch(0.8, 0.25, 702, feather=0.5) * 0.7)
    return img, height


# ---------------------------------------------------------------- pf_buri
# Research (bayong from Bohol and Antique markets; hand-painted woven-bag props): WIDE flat palm
# strips plaited DIAGONALLY over one, under one, pale straw; each visible strip piece is a flat
# lozenge with one flat highlight band, darkening softly where it dives under its neighbour.
# v2's 2.9 cm strips with dark seams read as a fine lattice; v3 draws 5 cm strips and no seam line.
BURI_N = 28            # cells per 2 m tile along each diagonal: strips ~5 cm wide


def paint_buri():
    np, P = _np(), _P()
    x, y = P.grid()
    c = 2.0 / BURI_N
    # p and q count cells along the two diagonals. A 2 m shift in x or y moves each by exactly
    # BURI_N (whole) and the over/under parity (i + j) by an even number, so the weave tiles.
    p = (x + y) / c + 0.08 * P.field(0.6, 301)
    q = (x - y) / c + 0.08 * P.field(0.6, 302)
    i, j = np.floor(p), np.floor(q)
    fp, fq = p - i, q - j
    a_top = ((i + j) % 2) == 0
    across = np.where(a_top, fp, fq)
    along = np.where(a_top, fq, fp)
    img = np.broadcast_to(P.hexcol("dac58f"), (P.SIZE, P.SIZE, 3)).astype(np.float32).copy()
    img = np.where(a_top[..., None], img, P.hexcol("d2bb84"))
    hi = P.smooth(np.clip((0.2 - np.abs(across - 0.42)) / 0.12 + 0.5, 0, 1))
    img = _mix(img, P.hexcol("e6d6a4"), hi * 0.6)
    tuck = P.smooth(np.clip((0.24 - np.minimum(along, 1 - along)) / 0.2, 0, 1))
    img = _mix(img, P.hexcol("bea46e"), tuck * 0.45)
    img = P.tint(img, np.array([0.95, 0.93, 0.88]), P.patch(0.7, 0.25, 311, feather=0.6) * 0.7)
    height = 0.5 + 0.3 * np.sin(np.pi * np.clip(across, 0, 1)) - 0.35 * tuck
    return img, height


# ---------------------------------------------------------------- pf_rattan
# Research (Philippine bilao, woven from split bamboo or rattan in a square over-under weave;
# hand-painted basket props): at game distance a bilao reads from a CHUNKY checker of rounded
# strips, each piece lit along its crown and soft-dark where it dives under its neighbour, in a
# warm honey brown. It must not read as pf_buri: axis-aligned where the buri is diagonal, rounded
# canes where the buri is flat strips, warmer and darker. 7 cm cells, 28 to a 2 m tile (40 read as fine cloth).
RATTAN_N = 28


def paint_rattan():
    np, P = _np(), _P()
    x, y = P.grid()
    c = 2.0 / RATTAN_N
    p = x / c + 0.06 * P.field(0.6, 801)
    q = y / c + 0.06 * P.field(0.6, 802)
    i, j = np.floor(p), np.floor(q)
    fp, fq = p - i, q - j
    a_top = ((i + j) % 2) == 0                  # the strip running along V is on top here
    across = np.where(a_top, fp, fq)
    along = np.where(a_top, fq, fp)
    img = np.broadcast_to(P.hexcol("c29a5c"), (P.SIZE, P.SIZE, 3)).astype(np.float32).copy()
    img = np.where(a_top[..., None], img, P.hexcol("b68e52"))
    crown = P.smooth(np.clip((0.18 - np.abs(across - 0.45)) / 0.14 + 0.5, 0, 1))
    img = _mix(img, P.hexcol("dcb77a"), crown * 0.65)                # ONE flat lit band per cane
    edge = P.smooth(np.clip((0.2 - np.minimum(across, 1 - across)) / 0.18, 0, 1))
    img = _mix(img, P.hexcol("946f3d"), edge * 0.45)
    tuck = P.smooth(np.clip((0.22 - np.minimum(along, 1 - along)) / 0.2, 0, 1))
    img = _mix(img, P.hexcol("8a6638"), tuck * 0.5)
    img = P.tint(img, np.array([0.94, 0.92, 0.88]), P.patch(0.7, 0.25, 803, feather=0.6) * 0.7)
    img = P.tint(img, np.array([1.05, 1.03, 1.0]), P.patch(0.5, 0.2, 804, feather=0.6) * 0.6)
    height = 0.5 + 0.35 * np.sqrt(np.clip(np.sin(np.pi * np.clip(across, 0, 1)), 0, 1)) - 0.4 * tuck
    return img, height


# ---------------------------------------------------------------- pf_fish
# ONE dried fish as a SPRITE for the fat bilao fish, painted in pf_fishbed's colours so the two
# kinds of tuyo match: golden-brown flank, a darker back, a pale belly, one flat lit band, a big
# dark eye on each flank and a darker tail fin. Image x = U along the fish (snout at 0, the fin
# from 0.84); image y = the girth (d = 0 at the back, 1 at the belly, symmetric about the middle
# row, so the image's upside-down V costs nothing).

def paint_fish():
    np, P = _np(), _P()
    x, y = P.grid()
    u, v = x / 2.0, y / 2.0
    d = 2 * np.minimum(v, 1 - v)
    wob = 0.03 * P.field(0.5, 901)
    img = np.broadcast_to(P.hexcol("a68352"), (P.SIZE, P.SIZE, 3)).astype(np.float32).copy()
    back = P.smooth(np.clip((0.3 + wob - d) / 0.06 + 0.5, 0, 1))
    img = _mix(img, P.hexcol("76593a"), back * 0.9)
    belly = P.smooth(np.clip((d - 0.78 - wob) / 0.06 + 0.5, 0, 1))
    img = _mix(img, P.hexcol("d4bd8d"), belly * 0.85)
    lit = (P.smooth(np.clip((0.08 - np.abs(d - 0.5)) / 0.04 + 0.5, 0, 1))
           * P.smooth(np.clip((u - 0.16) / 0.05, 0, 1)) * P.smooth(np.clip((0.74 - u) / 0.05, 0, 1)))
    img = _mix(img, P.hexcol("c4a46e"), lit * 0.7)
    gill = (P.smooth(np.clip((0.012 - np.abs(u - 0.2 - 0.03 * np.sin(np.pi * d))) / 0.008 + 0.5, 0, 1))
            * (d > 0.15) * (d < 0.85))
    img = _mix(img, P.hexcol("8a6a43"), gill * 0.6)
    tail = P.smooth(np.clip((u - 0.84) / 0.03, 0, 1))
    img = _mix(img, P.hexcol("7a5c3a"), tail * 0.9)
    eye = P.smooth(np.clip((1.0 - np.hypot((u - 0.09) / 0.03, (d - 0.42) / 0.09)) / 0.15 + 0.5, 0, 1))
    img = _mix(img, P.hexcol("2b2118"), eye)
    img = P.tint(img, np.array([0.95, 0.94, 0.9]), P.patch(0.6, 0.2, 902, feather=0.5) * 0.6)
    height = 0.5 + 0.3 * (1 - np.abs(2 * d - 1)) - 0.2 * gill - 0.15 * tail
    return img, height


PAINTERS = {"pf_trap": paint_trap, "pf_fishbed": paint_fishbed, "pf_shells": paint_shells, "pf_buri": paint_buri,
            "pf_rattan": paint_rattan, "pf_fish": paint_fish}



def save_texture(name, albedo, height):
    np, P = _np(), _P()
    from PIL import Image
    TEX.mkdir(parents=True, exist_ok=True)
    albedo = np.clip(albedo, 0, 1)
    r = np.ptp(height)
    h = (height - height.min()) / r if r > 1e-9 else np.full_like(height, 0.5)
    Image.fromarray((albedo * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_albedo.png")
    Image.fromarray((h * 255 + 0.5).astype(np.uint8)).save(TEX / f"{name}_height.png")
    Image.fromarray((P.normal_from_height(h, STRENGTH[name]) * 255 + 0.5).astype(np.uint8)).save(
        TEX / f"{name}_normal.png")
    print("[pf] painted", name)


def paint(names=None):
    for n in names or TEXTURES:
        save_texture(n, *PAINTERS[n]())


def compare(a, b):
    """Stack lineup vA over lineup vB, labelled: the before and after of a rework."""
    from PIL import Image, ImageDraw
    ims = [Image.open(PREVIEWS / f"{PREFIX}_lineup_v{v}.png").convert("RGB") for v in (a, b)]
    w, h = ims[0].size
    out = Image.new("RGB", (w, h * 2), (40, 40, 40))
    d = ImageDraw.Draw(out)
    for k, (im, v, label) in enumerate(zip(ims, (a, b), ("BEFORE", "AFTER"))):
        out.paste(im, (0, h * k))
        d.rectangle((0, h * k, 360, h * k + 44), fill=(30, 26, 22))
        d.text((12, h * k + 12), f"{label}: v{v}", fill=(245, 235, 215))
    path = PREVIEWS / f"{PREFIX}_before_after_v{a}_v{b}.png"
    out.save(path)
    print("[pf] compare", path)


# ================================================================ modelling (bpy)

if bpy is not None:
    import author_lagoon_boats as BK      # noqa: E402  Piece, loft, sweep, board, _uv_frame
    Z = Vector((0.0, 0.0, 1.0))
    X = Vector((1.0, 0.0, 0.0))
    Y = Vector((0.0, 1.0, 0.0))

# Slot -> the texture its material is built from when the file has none yet. The shared kit
# materials are looked up by name first, so these props wear the same bamboo as the houses.
TEXTURE_OF = {"bamboo": "bamboo_a", "pf_fish": "pf_fish", "pb_net": "pb_net", "pb_rope": "pb_rope",
              "pf_rattan": "pf_rattan", "pf_trap": "pf_trap", "pf_fishbed": "pf_fishbed",
              "pf_shells": "pf_shells", "pf_buri": "pf_buri"}
# Flat stand-ins, only if a texture is missing on disk (the beach kit repaints its own).
FALLBACK = {"bamboo": (0.72, 0.64, 0.36), "pf_fish": (0.62, 0.48, 0.28), "pb_net": (0.2, 0.42, 0.18),
            "pb_rope": (0.7, 0.58, 0.38), "pf_rattan": (0.66, 0.5, 0.3), "pf_trap": (0.8, 0.72, 0.47),
            "pf_fishbed": (0.72, 0.6, 0.4), "pf_shells": (0.92, 0.86, 0.78), "pf_buri": (0.86, 0.77, 0.56)}
ALPHA = ()                               # nothing see-through since the v3 simplification
OPEN = ()
# Per slot: (bevel width m or None, angle above which an edge is sharp, harden normals). The
# 1.5 cm bamboo bevel rounds the tray slab into a chunky board; the 8-sided poles (45 degrees
# between faces, under the 50 degree limit) stay smooth.
FINISH = {
    "bamboo": (0.015, 50.0, False),
    "pb_rope": (None, 60.0, False),
    "pf_fish": (None, 50.0, False),
    "pb_net": (0.015, 50.0, False),
    "pf_rattan": (0.008, 40.0, False),
    "pf_trap": (0.01, 40.0, False),
    "pf_fishbed": (0.01, 40.0, False),
    "pf_shells": (0.01, 40.0, False),
    "pf_buri": (0.008, 40.0, False),
}
# The bayong's dye, LINEAR RGB (index 0 = undyed). Clear of both role hues: a deep red at hue
# ~355, a leaf green at ~120 and an aubergine at ~290; no orange, no blue.
DYES = [(1.0, 1.0, 1.0), (0.42, 0.035, 0.05), (0.07, 0.28, 0.06), (0.2, 0.05, 0.22)]
SINK = 0.008


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


class Prop:
    """Collects pieces per material slot for one prop. Duck-types the boat kit's `Boat` (rng, add,
    uv_off), so author_lagoon_boats.sweep builds straight into it."""

    def __init__(self, kind, seed):
        self.kind, self.seed = kind, seed
        self.rng = random.Random(f"lagoon-propfishing:{kind}:{seed}")
        self.pieces = {}
        self.info = {}

    def add(self, slot, pc):
        if pc is None or not pc.bm.faces:
            return None
        self.pieces.setdefault(slot, []).append(pc)
        return pc

    def uv_off(self):
        return (self.rng.random(), self.rng.random())


class Group:
    """One object inside a prop (a trap, a tray) built in its OWN frame, so it can be stood,
    laid, leant or dropped as a whole before it joins the prop. Same interface as Prop."""

    def __init__(self, p):
        self.p, self.rng, self.items = p, p.rng, []

    def add(self, slot, pc):
        if pc is None or not pc.bm.faces:
            return None
        self.items.append((slot, pc))
        return pc

    def uv_off(self):
        return self.p.uv_off()

    def mesh(self):
        V, F = [], []
        for _slot, pc in self.items:
            base = len(V)
            pc.bm.verts.index_update()
            V.extend(v.co.copy() for v in pc.bm.verts)
            F.extend([base + v.index for v in f.verts] for f in pc.bm.faces)
        return V, F

    def commit(self, M):
        for slot, pc in self.items:
            bmesh.ops.transform(pc.bm, matrix=M, verts=pc.bm.verts)
            pc.bm.normal_update()
            self.p.add(slot, pc)
        self.items = []


def _xform(pc, M):
    if M is not None:
        bmesh.ops.transform(pc.bm, matrix=M, verts=pc.bm.verts)
        pc.bm.normal_update()
    return pc


# ---------------------------------------------------------------- primitives

def lathe(g, slot, prof, sides=16, sx=1.0, sy=1.0, wobble=None, caps=True, M=None, tint=None, phase=0.0,
          meridian=False):
    """A surface (caps=False) or solid of revolution round Z through `prof` [(r, z), ...], its
    section an ellipse sx:sy (the bayong is oval). U is each ring's real perimeter (world
    scale), V the distance along the profile. `wobble(a, z)` scales the radius per angle: hand
    made, never a perfect circle. `meridian` gives every ring the SAME U per side (the widest
    ring's), so a pattern running along V follows the meridians and narrows toward a thin end:
    v3's per-ring perimeters twisted the trap slats into spirals on every cone. `tint(z0, z1)` returns a dye index for the band of faces
    between two rings; it is stored in a face layer "tint" that build_prop turns into the
    "buri_tint" corner colour."""
    ou, ov = g.uv_off()
    rings, uvs = [], []
    vacc, prev = 0.0, None
    angles = [phase + math.tau * k / sides for k in range(sides)]
    for r, z in prof:
        if prev is not None:
            vacc += math.hypot(r - prev[0], z - prev[1])
        prev = (r, z)
        ring = []
        for a in angles:
            w = wobble(a, z) if wobble else 1.0
            ring.append(Vector((r * sx * w * math.cos(a), r * sy * w * math.sin(a), z)))
        rings.append(ring)
        us = [0.0]
        for k in range(sides):
            us.append(us[-1] + (ring[(k + 1) % sides] - ring[k]).length)
        uvs.append([(u, vacc) for u in us])
    if meridian:
        top = max(u[-1][0] for u in uvs)
        uvs = [[(top * k / sides, v) for k, (_u, v) in enumerate(row)] for row in uvs]
    pc = BK.loft(rings, uvs, caps=caps, off=(ou, ov))
    if tint is not None:
        lay = pc.bm.faces.layers.int.new("tint")
        pc.bm.verts.index_update()
        for f in pc.bm.faces:
            js = [v.index // sides for v in f.verts]
            lo, hi = min(js), max(js)
            f[lay] = 0 if lo == hi else tint(prof[lo][1], prof[hi][1], prof[lo][0], prof[hi][0])
    return g.add(slot, _xform(pc, M))


def revolve(g, slot, loop, sides=20, sx=1.0, sy=1.0, M=None):
    """A CLOSED section `loop` [(r, z), ...] swept round Z (on an sx:sy ellipse): a hoop, a rolled
    rim. Welded both ways (a proper torus), U round the ring, V round the section."""
    pc = BK.Piece()
    bm, uvl = pc.bm, pc.uv
    n = len(loop)
    ou, ov = g.uv_off()
    s = 1.0 / UV_METRES
    rmean = sum(r for r, _ in loop) / n
    arc = math.tau * rmean * (sx + sy) * 0.5 / sides
    vcum = [0.0]
    for a, b in zip(loop, loop[1:] + loop[:1]):
        vcum.append(vcum[-1] + math.hypot(b[0] - a[0], b[1] - a[1]))
    V = []
    for k in range(sides):
        a = math.tau * k / sides
        V.append([bm.verts.new((r * sx * math.cos(a), r * sy * math.sin(a), z)) for r, z in loop])
    for k in range(sides):
        k2 = (k + 1) % sides
        for j in range(n):
            j2 = (j + 1) % n
            f = bm.faces.new((V[k][j], V[k2][j], V[k2][j2], V[k][j2]))
            for lp, (u, v) in zip(f.loops, ((k * arc, vcum[j]), ((k + 1) * arc, vcum[j]),
                                            ((k + 1) * arc, vcum[j + 1]), (k * arc, vcum[j + 1]))):
                lp[uvl].uv = (u * s + ou, v * s + ov)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return g.add(slot, _xform(pc, M))


def hoop(g, slot, R, z, tr, sides=20, tsides=8, sx=1.0, sy=1.0, M=None):
    """A round-section ring of radius R at height z: a bamboo hoop, a rope binding."""
    loop = [(R + tr * math.cos(math.tau * k / tsides), z + tr * math.sin(math.tau * k / tsides))
            for k in range(tsides)]
    return revolve(g, slot, loop, sides, sx, sy, M)


def blob(g, slot, c, radii, subdiv=2, M=None):
    """A squashed ball: a rope knot, the tie round a trap's tail."""
    pc = BK.Piece()
    bmesh.ops.create_icosphere(pc.bm, subdivisions=subdiv, radius=1.0)
    bmesh.ops.scale(pc.bm, vec=radii, verts=pc.bm.verts)
    bmesh.ops.translate(pc.bm, vec=c, verts=pc.bm.verts)
    BK._uv_frame(pc, X, Y, Z, g.uv_off())
    return g.add(slot, _xform(pc, M))



def pole(g, slot, a, b, r, sides=8, nodes=0.0, bend=None, taper=0.14, phase=0.0):
    """An ORGANIC bamboo member from a to b (owner: "the reference isnt just a straight
    rectangular prism"): a small random BEND through a sideways-pushed midpoint, a TAPER (thicker
    at a by `taper`), a size jitter, and domed, rounded ends. The boat kit's sweep does the
    lofting (V along the member)."""
    rng = g.rng
    a, b = Vector(a), Vector(b)
    d = b - a
    Ln = d.length
    e1, e2 = BK._perp(d.normalized())
    bend = rng.uniform(0.012, 0.028) * Ln if bend is None else bend
    ang = rng.uniform(0, math.tau)
    mid = a.lerp(b, rng.uniform(0.4, 0.6)) + (e1 * math.cos(ang) + e2 * math.sin(ang)) * bend
    pts = BK._catmull([a, a.lerp(mid, 0.5), mid, mid.lerp(b, 0.5), b], per=4)
    r = r * rng.uniform(0.93, 1.07)

    def radius(s, L_):
        q = r * (1 + taper / 2 - taper * s / max(L_, 1e-6))
        return q, q
    return BK.sweep(g, slot, pts, radius, sides=sides, step=0.12, nodes=nodes, dome=True, phase=phase)


# ---------------------------------------------------------------- chunky dried fish
# (t along the fish from the snout, depth fraction, thickness fraction). Only the FIVE fish in a
# bilao are meshes, and they are drawn fat (30 cm, 5 cm thick): a cartoon fish, not a sardine.
# Six sides with a VERTEX top and bottom: a fish rests on a line, never lays a face on the tray.
FISH_ST = [(0.0, 0.5, 0.6), (0.1, 0.85, 0.9), (0.3, 1.0, 1.0), (0.58, 0.82, 0.8),
           (0.8, 0.38, 0.5), (0.87, 0.36, 0.42), (1.0, 1.0, 0.3)]


def fish(g, M, L=0.3, depth=0.13, thick=0.05, curl=0.0):
    """One dried fish on its side, snout toward local +X, centred on the origin, its lowest line
    at z = -thick/2. Sprite UVs (pf_fish): U = t along the fish, V = the angle round it / tau
    from the back, so the top vertex (a = pi/2) is V 0.25, the upper flank."""
    rings, uvs = [], []
    n = 6
    for t, df, tf in FISH_ST:
        x = L / 2 - t * L
        zc = curl * (2 * t - 1) ** 2               # a dried fish curls up a little at both ends
        ring = []
        for k in range(n):
            a = math.tau * k / n + math.pi / 2
            ring.append(Vector((x, math.cos(a) * depth / 2 * df, zc + math.sin(a) * thick / 2 * tf)))
        rings.append(ring)
        uvs.append([(t * UV_METRES, (k / n + 0.25) * UV_METRES) for k in range(n + 1)])
    pc = BK.loft(rings, uvs, off=(0.0, 0.0))
    return g.add("pf_fish", _xform(pc, M))


def lay_fish(g, x, y, z_support, yaw, L=0.3, sink=0.006):
    """A fat fish lying on a support whose top is z_support at (x, y), ROLLED 4 to 7 degrees onto
    one edge: its thin tail fin is nearly flat, and unrolled it lay parallel to the tray within
    4 mm (check_prop coplanar). A fish tipped onto one edge also looks tossed down."""
    rng = g.rng
    L = L * rng.uniform(0.92, 1.08)
    depth, thick = L * rng.uniform(0.4, 0.45), L * 0.17
    F = (Matrix.Translation((x, y, z_support + thick / 2 - sink)) @ Matrix.Rotation(yaw, 4, "Z")
         @ Matrix.Rotation(rng.choice((-1, 1)) * rng.uniform(0.07, 0.12), 4, "X"))
    return fish(g, F, L, depth, thick, curl=rng.uniform(0.005, 0.015))


# ---------------------------------------------------------------- slabs and heaps

def pillow(g, slot, hx, hy, z0, z1, round_=0.35, ridge=None, warp=0.0, uv_origin=None, side_v=None,
           nx=12, ny=8, M=None):
    """A soft SLAB, not a box: a grid whose footprint is a square pulled toward a disc by `round_`
    (rounded corners), a flat bottom at z0, a top at z1 plus `ridge(x, y)` and a gentle `warp`
    (the middle sags or bows), and sides joining the two perimeters. Top and bottom are projected
    from above at world scale; `uv_origin` pins that projection (pf_fishbed: whole fish from the
    layer's corner), else a random offset. Sides run U along the perimeter and V up it, starting
    at `side_v` metres (pf_fishbed: a gap between two fish rows)."""
    rng = g.rng
    pc = BK.Piece()
    bm, uvl = pc.bm, pc.uv
    s = 1.0 / UV_METRES
    if uv_origin is None:
        o = g.uv_off()
        ox, oy = -o[0] * UV_METRES, -o[1] * UV_METRES
    else:
        ox, oy = uv_origin
    ph = rng.uniform(0, math.tau)

    def foot(u, v):
        # square [-1, 1]^2 -> disc (the elliptical grid mapping), blended by round_
        du = u * math.sqrt(max(0.0, 1 - v * v / 2))
        dv = v * math.sqrt(max(0.0, 1 - u * u / 2))
        return hx * (u + (du - u) * round_), hy * (v + (dv - v) * round_)

    def topz(x, y):
        z = z1 + warp * (1 - (x / hx) ** 2) * (0.6 + 0.4 * math.sin(ph + y / hy))
        return z + (ridge(x, y) if ridge else 0.0)
    top, bot = {}, {}
    for i in range(nx + 1):
        for j in range(ny + 1):
            x, y = foot(-1 + 2 * i / nx, -1 + 2 * j / ny)
            top[i, j] = bm.verts.new((x, y, topz(x, y)))
            bot[i, j] = bm.verts.new((x, y, z0))
    faces = []
    for i in range(nx):
        for j in range(ny):
            faces.append(bm.faces.new((top[i, j], top[i + 1, j], top[i + 1, j + 1], top[i, j + 1])))
            faces.append(bm.faces.new((bot[i, j], bot[i, j + 1], bot[i + 1, j + 1], bot[i + 1, j])))
    for f in faces:
        for lp in f.loops:
            lp[uvl].uv = ((lp.vert.co.x - ox) * s, (lp.vert.co.y - oy) * s)
    ring = ([(i, 0) for i in range(nx)] + [(nx, j) for j in range(ny)]
            + [(i, ny) for i in range(nx, 0, -1)] + [(0, j) for j in range(ny, 0, -1)])
    acc = 0.0
    sv = side_v if side_v is not None else rng.random() * UV_METRES
    for k in range(len(ring)):
        a_, b_ = ring[k], ring[(k + 1) % len(ring)]
        seg = (top[b_].co.xy - top[a_].co.xy).length
        f = bm.faces.new((bot[a_], bot[b_], top[b_], top[a_]))
        for lp, (u, v) in zip(f.loops, ((acc, sv), (acc + seg, sv), (acc + seg, sv + (top[b_].co.z - z0)),
                                        (acc, sv + (top[a_].co.z - z0)))):
            lp[uvl].uv = (u * s, v * s)
        acc += seg
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return g.add(slot, _xform(pc, M))


def mound(g, slot, r, z_base, z_rim, z_top, sy=1.0, lumps=0.08, sides=24, flare=1.0):
    """A soft LUMPY HEAP (shells in a basket): a closed dome whose radius swells and dips round
    the girth (three and five lumps, fading in toward the top), its skirt `r` wide from z_base up
    to z_rim (widening by `flare`, to follow a flaring basket wall), then rounding over to z_top.
    The painted texture carries the shells."""
    rng = g.rng
    ph3, ph5 = rng.uniform(0, math.tau), rng.uniform(0, math.tau)
    h = max(1e-6, z_top - z_rim)
    prof = [(r, z_base), (r * flare, z_rim), (r * 0.86, z_rim + h * 0.42), (r * 0.62, z_rim + h * 0.76),
            (r * 0.34, z_rim + h * 0.95), (r * 0.1, z_top)]

    def wob(a, z):
        k = max(0.0, min(1.0, (z - z_rim) / h))
        return 1 + lumps * math.sin(math.pi * k) * (0.6 * math.sin(3 * a + ph3) + 0.4 * math.sin(5 * a + ph5))
    return lathe(g, slot, prof, sides=sides, sy=sy, wobble=wob)


# ---------------------------------------------------------------- placement solvers

def _tree(V, F, M):
    return BVHTree.FromPolygons([M @ v for v in V], F, epsilon=0.0)


def _minz(V, M):
    return min((M @ v).z for v in V)


def ground(V, M, sink=SINK):
    """Translate so the lowest point sinks `sink` into the ground (z = 0)."""
    return Matrix.Translation((0, 0, -sink - _minz(V, M))) @ M


def drop_onto(V, F, M, statics, extra=0.01, sink=SINK):
    """Lower the group from where M puts it until it touches a static or the ground, then press
    it `extra` further into what it touched: resting, never floating."""
    def hit(s):
        Ms = Matrix.Translation((0, 0, -s)) @ M
        if _minz(V, Ms) <= -sink:
            return "ground"
        t = _tree(V, F, Ms)
        return "static" if any(t.overlap(st) for st in statics) else None
    s, step = 0.0, 0.02
    while hit(s) is None and s < 5.0:
        s += step
    lo, hi = max(0.0, s - step), s
    for _ in range(18):
        mid = (lo + hi) / 2
        if hit(mid) is None:
            lo = mid
        else:
            hi = mid
    kind = hit(hi)
    if kind == "ground":
        return ground(V, M, sink)
    return Matrix.Translation((0, 0, -hi - extra)) @ M


def lean(V, F, M, pivot, direction, statics, extra_deg=0.8, max_deg=70.0):
    """Tip the group about a horizontal axis through `pivot` (its foot on the ground) toward
    `direction` until it TOUCHES a static, then `extra_deg` further: it rests on what it leans
    on. Rotating a point above the pivot about Z x d moves it toward d."""
    d = Vector((direction.x, direction.y, 0)).normalized()
    axis = Z.cross(d).normalized()
    pv = Vector(pivot)

    def at(deg):
        return (Matrix.Translation(pv) @ Matrix.Rotation(math.radians(deg), 4, axis)
                @ Matrix.Translation(-pv) @ M)

    def hit(deg):
        t = _tree(V, F, at(deg))
        return any(t.overlap(st) for st in statics)
    a = 0.0
    while a < max_deg and not hit(a):
        a += 1.0
    lo, hi = max(0.0, a - 1.0), a
    for _ in range(12):
        mid = (lo + hi) / 2
        if hit(mid):
            hi = mid
        else:
            lo = mid
    return at(min(max_deg, hi + extra_deg)), hi



# ---------------------------------------------------------------- fish_rack

def build_fish_rack(p):
    """A tuyo/daing drying rack, in FEW big shapes: two fat rails, one chunky tray slab resting on
    them, one soft fish slab on the tray (pf_fishbed, ridged along its rows), four fat splayed legs
    through the tray's corners, four rope lashings. The bed hangs in its own frame Mb (height h,
    tilted on seed 2); the legs stand VERTICAL from the ground up through each rail joint, so a
    tilted rack simply has longer back legs, as a real one does."""
    rng = p.rng
    layout = (p.seed - 1) % 3
    bed = ("bamboo", "bamboo", "pb_net")[layout]
    L, W = rng.uniform(1.75, 1.95), rng.uniform(0.9, 1.0)
    h = rng.uniform(0.52, 0.58)
    tilt = math.radians(rng.uniform(11, 14)) if layout == 1 else math.radians(rng.uniform(-1.5, 1.5))
    # Front (+Y) LOWER when tilted: a rotation about +X lifts +Y, so the angle is negative.
    Mb = Matrix.Translation((0, 0, h)) @ Matrix.Rotation(-tilt, 4, "X")
    lr, rr = 0.052, 0.045                        # leg and rail radii: fat, cartoon bamboo
    p.info.update(layout=layout, bed=bed, length=round(L, 2), width=round(W, 2))
    for side in (-1, 1):
        pole(p, "bamboo", Mb @ Vector((-L / 2 - 0.06, side * W / 2, 0.0)),
             Mb @ Vector((L / 2 + 0.06, side * W / 2, 0.0)), rr, nodes=0.7, bend=0.012)
    # The tray: a rounded slab 6 cm thick, overhanging the rails, sunk 1.2 cm onto their top line.
    t = 0.06
    z0 = rr - 0.012
    pillow(p, bed, L / 2, W / 2 + 0.07, z0, z0 + t, round_=0.18, warp=-0.012, nx=10, ny=6, M=Mb)
    # The fish: whole rows only, a soft slab ridged along each row, pinned to the painting.
    nf = int((L - 0.2) / FISH_STEP)
    rows = 2
    hx, hy = nf * FISH_STEP / 2, rows * FISH_ROW / 2

    def ridge(x, y):
        v = (y + hy) / FISH_ROW
        return 0.022 * math.sin(math.pi * (v - math.floor(v))) ** 1.5
    # bottom 3 cm under the tray top: the tray's own sag (warp) is 1.2 cm, and v3 sank the fish
    # slab only 1.2 cm, so at mid tray the two faces met in one plane (check_prop coplanar)
    pillow(p, "pf_fishbed", hx, hy, z0 + t - 0.03, z0 + t + 0.008, round_=0.06, ridge=ridge,
           uv_origin=(-hx, -hy), side_v=FISH_ROW - 0.035, nx=nf, ny=rows * 6, M=Mb)
    # Four fat legs, splayed, each through a corner of the tray, rope-lashed at the rail.
    off = W / 2 + (lr + rr) * 0.8
    top_z = z0 + t + rng.uniform(0.04, 0.06)
    for sx in (-1, 1):
        x = sx * (L / 2 - 0.2)
        for sy_ in (-1, 1):
            J = Mb @ Vector((x + rng.uniform(-0.02, 0.02), sy_ * off, 0.0))
            T = Mb @ Vector((x, sy_ * off, top_z))
            foot = Vector((J.x + sx * rng.uniform(0.03, 0.07), J.y + sy_ * rng.uniform(0.05, 0.09), -0.03))
            d = (J - foot).normalized()
            top = foot + d * ((T.z - foot.z) / max(0.2, d.z))
            pole(p, "bamboo", foot, top, lr, nodes=0.45, taper=0.18, phase=math.radians(rng.uniform(5, 17)))
            M = Matrix.Translation(J) @ Matrix.Rotation(rng.uniform(0, 1), 4, "Z")
            hoop(p, "pb_rope", lr + 0.006, 0.0, 0.02, sides=14, tsides=6, M=M)


# ---------------------------------------------------------------- bubo

def trap(p, shape, L, R):
    """One bubo in its own frame, axis +Z: the funnel MOUTH at z = 0, the tied TAIL at z = L. A
    SOLID woven body (pf_trap: outer wall, a thick lip, and a funnel dipping inside to a dark
    throat), one fat bamboo hoop round the mouth, one band round the body, a rope tie and a round
    bamboo knob at the tail. A cylinder bellies and gathers in over its last fifth; a cone flares
    at the mouth and tapers like a bell."""
    g = Group(p)
    rng = p.rng
    ph = rng.uniform(0, math.tau)

    if shape == "cyl":
        def r_at(z):
            t = z / L
            r = R * (1 + 0.09 * math.sin(math.pi * min(t / 0.8, 1.0)))
            if t > 0.78:
                r *= 1 - 0.62 * smooth((t - 0.78) / 0.22)
            return r
        band_at = 0.52
    else:
        def r_at(z):
            t = z / L
            return R * (1 - 0.66 * t ** 1.25) * (1 + 0.07 * (1 - smooth(t / 0.15)))
        band_at = 0.5

    def wob(a, z):
        return 1 + 0.02 * math.sin(2 * a + ph) * (0.3 + z / L)
    outer = [(r_at(L * k / 12), L * k / 12) for k in range(12, -1, -1)]
    inner = [(r_at(0) - 0.045, 0.03), (R * 0.56, 0.15 * L), (R * 0.3, 0.3 * L)]
    lathe(g, "pf_trap", outer + inner, sides=20, wobble=wob, meridian=True)
    hoop(g, "bamboo", r_at(0.02) - 0.008, 0.022, 0.034, sides=24, tsides=8)          # the fat mouth hoop
    hoop(g, "bamboo", r_at(band_at * L) * 1.0, band_at * L, 0.024, sides=22, tsides=8)
    rt = r_at(L)
    zt = L - 0.05
    hoop(g, "pb_rope", r_at(zt) + 0.004, zt, 0.028, sides=16, tsides=6)
    lathe(g, "bamboo", [(rt * 0.8, L - 0.1), (rt * 1.02, L - 0.01), (rt * 0.86, L + 0.06), (rt * 0.45, L + 0.1),
                        (rt * 0.12, L + 0.11)], sides=14)
    return g, r_at(0.02) + 0.026, r_at(zt) + 0.032


def _lay_flat(r0, r1, L, yaw, at):
    """A trap lying on its side: axis horizontal toward yaw, then tipped so both ends touch."""
    tip = math.atan2(r0 - r1, L)
    return (Matrix.Translation(at) @ Matrix.Rotation(yaw, 4, "Z")
            @ Matrix.Rotation(math.pi / 2 + tip, 4, "Y"))


def _stand(at, yaw=0.0):
    """Standing MOUTH DOWN, the way traps are stored, the tied knob up like a bell's handle. The
    trap's own frame already has the mouth at z = 0, so this is only a yaw. (v1 flipped it and
    stood every trap on its tail.)"""
    return Matrix.Translation(at) @ Matrix.Rotation(yaw, 4, "Z")


def build_bubo(p):
    rng = p.rng
    layout = (p.seed - 1) % 3
    p.info["layout"] = layout
    statics = []

    def settle(g, M, lean_to=None, pivot=None, drop=False):
        V, F = g.mesh()
        M = drop_onto(V, F, M, statics) if drop else ground(V, M)
        if lean_to is not None:
            M, _deg = lean(V, F, M, pivot, lean_to, statics)
        statics.append(_tree(V, F, M))
        g.commit(M)

    if layout == 0:
        L, R = rng.uniform(0.9, 1.0), rng.uniform(0.31, 0.35)
        g, r0, r1 = trap(p, "cyl", L, R)
        settle(g, _stand(Vector((0, 0, 0)), rng.uniform(0, 1)))
        Lc, Rc = rng.uniform(0.78, 0.86), rng.uniform(0.29, 0.32)
        g, r0c, r1c = trap(p, "cone", Lc, Rc)
        d = Vector((-1, 0.1, 0)).normalized()
        at = Vector((0, 0, 0)) - d * (r0 + r0c + 0.1)
        settle(g, _stand(at, rng.uniform(0, 1)), lean_to=d, pivot=at + d * r0c + Vector((0, 0, -SINK)))
        Ls, Rs = rng.uniform(0.7, 0.76), rng.uniform(0.25, 0.28)
        g, r0s, r1s = trap(p, "cone", Ls, Rs)
        settle(g, _lay_flat(r0s, r1s, Ls, math.radians(rng.uniform(150, 170)), Vector((0.15, 0.62, 0))))
    elif layout == 1:
        L, R = rng.uniform(0.88, 0.96), rng.uniform(0.3, 0.33)
        for side in (-1, 1):
            Li = L + rng.uniform(-0.04, 0.04)
            g, r0, r1 = trap(p, "cyl", Li, R)
            yaw = (0.0 if side < 0 else math.pi) + rng.uniform(-0.06, 0.06)
            M = _lay_flat(r0, r1, Li, yaw, Vector((-(Li / 2) * math.cos(yaw), side * (r0 - 0.014), 0)))
            settle(g, M)
        Lc, Rc = rng.uniform(0.76, 0.84), rng.uniform(0.27, 0.3)
        g, r0, r1 = trap(p, "cone", Lc, Rc)
        yaw = math.radians(rng.uniform(-12, 12))
        M = (Matrix.Translation((-Lc / 2 * math.cos(yaw) + 0.05, -Lc / 2 * math.sin(yaw), 1.5))
             @ Matrix.Rotation(yaw, 4, "Z") @ Matrix.Rotation(math.pi / 2, 4, "Y"))
        settle(g, M, drop=True)
    else:
        Lc, Rc = rng.uniform(0.82, 0.9), rng.uniform(0.31, 0.34)
        g, r0, r1 = trap(p, "cone", Lc, Rc)
        settle(g, _stand(Vector((0, -0.15, 0)), rng.uniform(0, 1)))
        Ls, Rs = rng.uniform(0.68, 0.74), rng.uniform(0.24, 0.27)
        g, r0s, r1s = trap(p, "cone", Ls, Rs)
        d = Vector((1, -0.15, 0)).normalized()
        at = Vector((0, -0.15, 0)) - d * (r0 + r0s + 0.12)
        settle(g, _stand(at, rng.uniform(0, 1)), lean_to=d, pivot=at + d * r0s + Vector((0, 0, -SINK)))
        L, R = rng.uniform(0.86, 0.94), rng.uniform(0.29, 0.32)
        g, r0, r1 = trap(p, "cyl", L, R)
        yaw = math.radians(rng.uniform(95, 115)) + math.pi   # tail back (-Y), mouth toward +Y
        settle(g, _lay_flat(r0, r1, L, yaw, Vector((0.35, 0.28 + L, 0))))


# ---------------------------------------------------------------- basket

def bilao(p, R, contents=None):
    """A bilao: a wide, shallow woven tray (a solid shell in pf_rattan, 2 cm wall) with one fat
    bamboo rim hoop, in its own frame, its underside at z = 0. contents: "fish" (five fat dried
    fish, heads out) or "shells" (a low lumpy heap painted with shells)."""
    g = Group(p)
    rng = p.rng
    t = 0.02
    rb, rim = R * 0.8, 0.09
    prof = [(rb, 0.0), (R * 0.95, 0.05), (R, rim),
            (R - t, rim), (R * 0.95 - t, 0.05 + 0.004), (rb - t * 0.6, t)]
    ph = rng.uniform(0, math.tau)
    lathe(g, "pf_rattan", prof, sides=36, wobble=lambda a, z: 1 + 0.018 * math.sin(3 * a + ph))
    hoop(g, "bamboo", R - t * 0.3, rim - 0.006, 0.028, sides=36, tsides=8)
    floor = t
    if contents == "fish":
        n = 5
        rc = R * 0.44
        for k in range(n):
            a = math.tau * k / n + rng.uniform(-0.08, 0.08)
            lay_fish(g, rc * math.cos(a), rc * math.sin(a), floor, a + math.pi, L=R * 0.8)
    elif contents == "shells":
        mound(g, "pf_shells", R * 0.8, floor - 0.012, floor + 0.02, floor + 0.1, lumps=0.07)
    return g


def bayong(p, H, contents=None):
    """A squat bayong-style basket: an OVAL woven bag (section 1 : 0.64), wider at the mouth, a fat
    rolled rim, two fat plaited handles over the long sides, in pf_buri with ONE wide dyed band
    (the "tint" face layer). A solid shell with a 2 cm wall and an inner floor, its underside at
    z = 0. contents: "shells" heaps a lumpy mound of painted shells above the rim."""
    g = Group(p)
    rng = p.rng
    sy = 0.64
    rb, rt = 0.2, rng.uniform(0.25, 0.27)
    t = 0.026                                          # thick enough to hold a heap 1.3 cm off both faces

    def r_out(z):
        if z < 0.06:
            return rb + 0.035 * math.sin(z / 0.06 * math.pi / 2)
        return rb + 0.035 + (rt - rb - 0.035) * ((z - 0.06) / (H - 0.06)) ** 0.85
    dye = rng.choice([1, 2, 3])
    band = (H * 0.46, H * 0.64)
    zs = sorted({0.0, 0.03, 0.06, H * 0.25, band[0], band[1], H * 0.85, H})
    outer = [(r_out(z), z) for z in zs]
    inner = [(r_out(z) - t, z) for z in reversed(zs) if z >= 0.06] + [(rb + 0.035 - t - 0.01, 0.035)]

    def tint(z0, z1, r0, r1):
        if min(r0, r1) < r_out(min(z0, z1)) - t * 0.5:
            return 0                                   # inner wall: undyed
        return dye if band[0] <= (z0 + z1) / 2 <= band[1] else 0
    ph = rng.uniform(0, math.tau)
    lathe(g, "pf_buri", outer + inner, sides=28, sy=sy, tint=tint,
          wobble=lambda a, z: 1 + 0.012 * math.sin(2 * a + ph) * (z / H))
    rr = 0.026                                         # the fat rolled rim of the same buri
    revolve(g, "pf_buri", [(rt - t * 0.5 + rr * math.cos(math.tau * k / 8), H + rr * 0.6 * math.sin(math.tau * k / 8))
                           for k in range(8)], sides=28, sy=sy)
    # Two fat handles, each an arch over a long side. Each end dives THROUGH the wall at a slant,
    # outside above to inside below: ends run parallel to the wall lay in its plane (coplanar).
    hx = rt * 0.42
    for side in (-1, 1):
        def wall_y(z, inset):
            r = r_out(min(z, H)) + inset
            return side * r * sy * math.sqrt(max(0.0, 1 - (hx / r) ** 2))
        z_in, z_out = H - 0.08, H + 0.035
        pts = [Vector((-hx, wall_y(z_in, -0.045), z_in)), Vector((-hx, wall_y(z_out, 0.02), z_out)),
               Vector((-hx * 0.75, wall_y(H, 0.01), H + 0.13)), Vector((0, wall_y(H, 0.0), H + 0.18)),
               Vector((hx * 0.75, wall_y(H, 0.01), H + 0.13)), Vector((hx, wall_y(z_out, 0.02), z_out)),
               Vector((hx, wall_y(z_in, -0.045), z_in))]
        BK.sweep(g, "pf_buri", BK._catmull(pts, 5), 0.022, sides=8, step=0.05, dome=True)
    if contents == "shells":
        # the heap's skirt runs up the MIDDLE of the wall, flaring with it, so it is held by the
        # wall and clear of both faces (v3 and v4 came within 4 mm of the inner face: coplanar)
        ri = r_out(H - 0.1) - t * 0.5
        mound(g, "pf_shells", ri, H - 0.1, H - 0.02, H + 0.085, sy=sy, lumps=0.06,
              flare=(r_out(H - 0.02) - t * 0.5) / ri)
    return g


def build_basket(p):
    rng = p.rng
    layout = (p.seed - 1) % 3
    p.info["layout"] = layout
    H = rng.uniform(0.32, 0.36)
    R = rng.uniform(0.34, 0.38)
    yaw_b = rng.uniform(-0.4, 0.4)
    if layout == 0:
        gb = bayong(p, H, "shells")
        gb.commit(Matrix.Translation((0.38, -0.14, -SINK)) @ Matrix.Rotation(yaw_b, 4, "Z"))
        gt = bilao(p, R, "fish")
        gt.commit(Matrix.Translation((-0.32, 0.12, -SINK)) @ Matrix.Rotation(rng.uniform(0, 1), 4, "Z"))
    elif layout == 1:
        gb = bayong(p, H, "shells")
        Mb = Matrix.Translation((0.0, -0.2, -SINK)) @ Matrix.Rotation(yaw_b, 4, "Z")
        V, F = gb.mesh()
        static = _tree(V, F, Mb)
        gb.commit(Mb)
        gt = bilao(p, R, None)
        # the tray stands on its rim in FRONT of the bag, its underside toward it, and is tipped
        # back until it rests against the rim or a handle
        ang = math.radians(90 + rng.uniform(-25, 25))
        d = -Vector((math.cos(ang), math.sin(ang), 0))           # from the tray toward the bag
        at = Vector((0.0, -0.2, 0)) - d * 0.36
        face = (-d).to_track_quat("Z", "Y").to_matrix().to_4x4()   # tray's inside faces away
        V, F = gt.mesh()
        M = ground(V, Matrix.Translation(at) @ face)
        low = min((M @ v for v in V), key=lambda w: w.z)
        M, _deg = lean(V, F, M, Vector((low.x, low.y, -SINK)), d, [static])
        gt.commit(M)
    else:
        gb = bayong(p, H, None)
        gb.commit(Matrix.Translation((0.4, -0.16, -SINK)) @ Matrix.Rotation(yaw_b, 4, "Z"))
        gt = bilao(p, R, "shells")
        gt.commit(Matrix.Translation((-0.3, 0.12, -SINK)) @ Matrix.Rotation(rng.uniform(0, 1), 4, "Z"))


BUILDERS = {"fish_rack": build_fish_rack, "bubo": build_bubo, "basket": build_basket}


# ---------------------------------------------------------------- materials and assembly

def material(slot):
    """The slot's material, looked up by name first (so the house, boat and beach kits share
    it), else built on the slot's texture with render_lagoon_texture_preview.uv_material. Alpha
    slots link the albedo's alpha and draw two-sided; pf_buri multiplies by "buri_tint"."""
    m = bpy.data.materials.get(slot)
    if m is not None:
        return m
    m = bpy.data.materials.new(slot)
    tex = TEXTURE_OF[slot]
    if not (TEX / f"{tex}_albedo.png").exists():
        print(f"[pf] WARNING {tex}_albedo.png missing: {slot} drawn flat")
        m.use_nodes = True
        m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = FALLBACK[slot] + (1,)
        return m
    import render_lagoon_texture_preview as RP
    RP.uv_material(m, tex)
    nt = m.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    img = next(n for n in nt.nodes if n.type == "TEX_IMAGE" and n.image and n.image.name.endswith("_albedo.png"))
    m.use_backface_culling = False
    if slot in ALPHA:
        nt.links.new(img.outputs["Alpha"], bsdf.inputs["Alpha"])
        if hasattr(m, "surface_render_method"):
            m.surface_render_method = "DITHERED"
    if slot == "pf_buri":
        attr = nt.nodes.new("ShaderNodeAttribute")
        attr.attribute_name = "buri_tint"
        mix = nt.nodes.new("ShaderNodeMix")
        mix.data_type = "RGBA"
        mix.blend_type = "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        nt.links.new(img.outputs["Color"], mix.inputs["A"])
        nt.links.new(attr.outputs["Color"], mix.inputs["B"])
        nt.links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
    m.diffuse_color = FALLBACK[slot] + (1,)
    return m


def _append(dst, duv, dtint, pc):
    vmap = {v: dst.verts.new(v.co) for v in pc.bm.verts}
    src_t = pc.bm.faces.layers.int.get("tint")
    for f in pc.bm.faces:
        try:
            nf = dst.faces.new([vmap[v] for v in f.verts])
        except ValueError:
            continue
        for a, c in zip(f.loops, nf.loops):
            c[duv].uv = a[pc.uv].uv
        nf[dtint] = f[src_t] if src_t is not None else 0


def _lin_to_srgb(c):
    return c * 12.92 if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055


def build_prop(kind, seed=1):
    """Build one prop of `kind` (see KINDS) from `seed`; return its Collection, NOT linked to any
    scene: one root empty named `kind` at the ground contact centre (+Y front), every part
    parented to it, one object per material slot, each solid with a live Bevel."""
    if kind not in BUILDERS:
        raise ValueError(f"unknown prop kind {kind!r}; one of {KINDS}")
    p = Prop(kind, seed)
    BUILDERS[kind](p)
    col = bpy.data.collections.new(f"{PREFIX}_{kind}_{seed}")
    root = bpy.data.objects.new(kind, None)
    root.empty_display_type, root.empty_display_size = "PLAIN_AXES", 0.5
    col.objects.link(root)
    root["prop_kind"], root["prop_seed"] = kind, seed
    root["prop_layout"] = p.info.get("layout", 0)
    for k, v in p.info.items():
        if isinstance(v, (int, float, str)) and k != "layout":
            root[f"prop_{k}"] = v
    for slot, pcs in p.pieces.items():
        bm = bmesh.new()
        uv = bm.loops.layers.uv.new("UVMap")
        tl = bm.faces.layers.int.new("tint")
        for pc in pcs:
            _append(bm, uv, tl, pc)
            pc.bm.free()
        bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
        bm.normal_update()
        width, sharp_deg, harden = FINISH[slot]
        lim = math.radians(sharp_deg)
        for f in bm.faces:
            f.smooth = True
        for e in bm.edges:
            e.smooth = not (len(e.link_faces) == 2 and e.link_faces[0].normal.angle(e.link_faces[1].normal, 0) > lim)
        tints = [f[tl] for f in bm.faces]
        bm.faces.layers.int.remove(tl)
        me = bpy.data.meshes.new(f"{kind}_{seed}_{slot}")
        bm.to_mesh(me)
        bm.free()
        me.materials.append(material(slot))
        if slot == "pf_buri":
            ca = me.color_attributes.new("buri_tint", "BYTE_COLOR", "CORNER")
            cols = []
            for poly, ti in zip(me.polygons, tints):
                rgb = tuple(_lin_to_srgb(c) for c in DYES[ti]) + (1.0,)
                cols.extend(rgb * poly.loop_total)
            ca.data.foreach_set("color_srgb", cols)
        ob = bpy.data.objects.new(me.name, me)
        ob.parent = root
        col.objects.link(ob)
        if width:
            bev = ob.modifiers.new("Bevel", "BEVEL")
            bev.width, bev.segments, bev.limit_method = width, 2, "ANGLE"
            bev.angle_limit = math.radians(sharp_deg)
            bev.harden_normals = harden
            bev.use_clamp_overlap = True
    return col


# ---------------------------------------------------------------- checks

def check_prop(col):
    """Tri counts (base and with the bevels), non-manifold edges on the SOLID slots, COPLANAR
    overlaps (parallel faces of different shells within 4 mm over each other: z-fighting),
    LOOSE shells (in no chain of intersections to a shell that reaches the ground, z <= 0), and
    the bounding box (width x, depth y, height)."""
    V, P, shell, slot_of = [], [], [], []
    out = {"tris": 0, "tris_bevelled": 0, "nonmanifold": {}, "shells": 0}
    sid = 0
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    shell_minz = {}
    for ob in col.objects:
        if ob.type != "MESH":
            continue
        me = ob.data
        out["tris"] += sum(len(p.vertices) - 2 for p in me.polygons)
        ev = ob.evaluated_get(dg)
        em = ev.to_mesh()
        out["tris_bevelled"] += sum(len(p.vertices) - 2 for p in em.polygons)
        ev.to_mesh_clear()
        slot = next((s for s in FINISH if ob.name.endswith("_" + s)), "")
        if slot not in OPEN:
            bm = bmesh.new()
            bm.from_mesh(me)
            nm = sum(1 for e in bm.edges if not e.is_manifold)
            bm.free()
            if nm:
                out["nonmanifold"][ob.name] = nm
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
        V.extend(v.co.copy() for v in me.vertices)
        for p in me.polygons:
            r = find(p.vertices[0])
            if r not in roots:
                roots[r] = sid
                sid += 1
            s = roots[r]
            P.append([base_v + i for i in p.vertices])
            shell.append(s)
            slot_of.append(ob.name)
            mz = min(V[base_v + i].z for i in p.vertices)
            shell_minz[s] = min(shell_minz.get(s, 9.0), mz)
    out["shells"] = sid
    tree = BVHTree.FromPolygons(V, P, epsilon=0.0)
    normals = []
    for p in P:
        a, c, d = V[p[0]], V[p[1]], V[p[2]]
        nn = (c - a).cross(d - a)
        normals.append(nn.normalized() if nn.length > 1e-12 else Vector())
    where = {}
    for i, p in enumerate(P):
        n = normals[i]
        if n.length < 0.5:
            continue
        c = sum((V[k] for k in p), Vector()) / len(p)
        for q in [c] + [c.lerp(V[k], 0.7) for k in p]:
            for _loc, nrm, idx, _dist in tree.find_nearest_range(q, 0.004):
                if idx is not None and shell[idx] != shell[i] and abs(nrm.dot(n)) > 0.999:
                    key = (min(shell[i], shell[idx]), max(shell[i], shell[idx]))
                    where.setdefault(key, (slot_of[i], slot_of[idx], tuple(round(x, 2) for x in c)))
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
    anchored = {f2(s) for s, z in shell_minz.items() if z <= 0.0}
    loose = [s for s in range(sid) if f2(s) not in anchored]
    out["loose"] = len(loose)
    out["loose_examples"] = sorted({slot_of[i] for i, s in enumerate(shell) if s in set(loose)})[:6]
    xs, ys, zs = [v.x for v in V], [v.y for v in V], [v.z for v in V]
    out["bbox"] = (round(max(xs) - min(xs), 2), round(max(ys) - min(ys), 2), round(min(zs), 3), round(max(zs), 2))
    return out


# ---------------------------------------------------------------- preview

def _mat(name, colour, rough=0.85):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = colour + (1,)
    bsdf.inputs["Roughness"].default_value = rough
    return m


# (kind, seed, x, y, yaw degrees). Racks in the back row, traps and baskets in front.
LINEUP = [("fish_rack", 1, -5.2, 2.6, 8.0), ("fish_rack", 2, -1.4, 2.8, -6.0), ("fish_rack", 3, 2.4, 2.6, 4.0),
          ("bubo", 1, -5.6, -0.6, 10.0), ("bubo", 2, -3.2, -0.4, -12.0), ("bubo", 3, -0.8, -0.8, 6.0),
          ("basket", 1, 1.2, -0.9, -8.0), ("basket", 2, 2.9, -0.7, 14.0), ("basket", 3, 4.6, -0.9, -4.0)]
SHOTS = [
    ("lineup", (-1.4, -10.5, 4.8), (-1.4, 0.9, 0.4), 28),
    ("close", (-3.2, -4.4, 1.9), (-3.2, -0.6, 0.4), 30),
    ("close2", (3.4, -3.4, 1.5), (3.0, -0.8, 0.25), 32),
    ("rackclose", (-3.4, -1.4, 2.2), (-1.4, 2.8, 0.55), 32),
    ("far", (-1.4, -20.0, 1.25), (-1.4, 0.9, 0.5), 40),
]


def _preview_scene():
    scene = bpy.context.scene
    sand = bpy.data.meshes.new("sand")
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=200)
    bm.to_mesh(sand)
    bm.free()
    sand.materials.append(_mat("warm_sand", (0.86, 0.72, 0.48)))
    scene.collection.objects.link(bpy.data.objects.new("sand", sand))
    ref = bpy.data.meshes.new("scale_ref_1m60")
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=0.25, radius2=0.25, depth=1.6)
    bmesh.ops.translate(bm, vec=(0, 0, 0.8), verts=bm.verts)
    bm.to_mesh(ref)
    bm.free()
    ref.materials.append(_mat("scale_pink", (0.95, 0.30, 0.55), 0.6))
    r = bpy.data.objects.new("scale_ref_1m60", ref)
    r.location = (-7.6, -0.6, -0.01)
    scene.collection.objects.link(r)
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.color = 4.5, (1.0, 0.9, 0.74)
    sun.data.angle = math.radians(3)
    sun.rotation_euler = (math.radians(50), 0, math.radians(35))
    scene.collection.objects.link(sun)
    world = bpy.data.worlds.new("world")
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.55, 0.6, 0.66, 1)
    bg.inputs["Strength"].default_value = 1.0
    scene.world = world
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 1000
    scene.collection.objects.link(cam)
    scene.camera = cam
    scene.render.engine = "BLENDER_EEVEE"
    if hasattr(scene.eevee, "shadow_pool_size"):
        scene.eevee.shadow_pool_size = "1024"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    scene.view_settings.view_transform = "AgX"
    for look in ("AgX - Punchy", "Punchy"):
        try:
            scene.view_settings.look = look
            break
        except TypeError:
            continue
    return cam


def _ensure_textures():
    missing = [t for t in TEXTURES if not (TEX / f"{t}_albedo.png").exists()]
    if missing:
        print("[pf] painting", missing)
        subprocess.run(["py", "-3", str(Path(__file__).resolve()), "--paint", ",".join(missing)], check=True)


def main():
    if bpy is None:
        args = sys.argv[1:]
        if "--compare" in args:
            i = args.index("--compare")
            compare(int(args[i + 1]), int(args[i + 2]))
            return
        names = None
        if "--paint" in args:
            i = args.index("--paint")
            if i + 1 < len(args):
                names = args[i + 1].split(",")
        paint(names)
        return
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    _ensure_textures()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    top = bpy.data.collections.new("lagoon_prop_fishing")
    bpy.context.scene.collection.children.link(top)
    for kind, seed, x, y, yaw in LINEUP:
        col = build_prop(kind, seed)
        top.children.link(col)
        c = check_prop(col)
        root = next(o for o in col.objects if o.parent is None)
        print(f"[pf] {kind} {seed} layout {root['prop_layout']}: {c}")
        root.location = (x, y, 0.0)
        root.rotation_euler = (0, 0, math.radians(yaw + 180.0))   # +Y front toward the camera
    if "--save" in argv:
        out = SOURCE / "lagoon_prop_fishing.blend"
        bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
        print("[pf] saved", out)
    if not version:
        return
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    cam = _preview_scene()
    scene = bpy.context.scene
    for tag, pos, tgt, lens in SHOTS:
        cam.location = pos
        cam.data.lens = lens
        cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        path = PREVIEWS / f"{PREFIX}_{tag}_v{version}.png"
        if path.exists():
            raise SystemExit(f"[pf] {path} exists: never overwrite a render, bump --preview")
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("[pf] preview", path)


if __name__ == "__main__":
    main()
