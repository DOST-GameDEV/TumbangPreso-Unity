"""Builds phaister-doll.glb: Phaister's voodoo doll, awake (HERO-10 v3, the VOODOO DOLL ultimate). v11.

    python tools/build_phaister_doll_voxel.py

Owner, 2026-09-27, on v10: *"i hate that u js drew the pink glow in"*, *"i want it to actually look like its coming out of the
holes"*, *"remove the finger stoo we dont have fingers for anyone"* (twice), *"thoroughly texture it"*, *"i like the idea of this big
fat voodoo doll that's kinda sllow(to balance it) but make like paete size"*, *"give him a fatter belly"*, *"make the glow really look
like it comes from within not js drawn on"*, and a reference render to use as inspiration with the colours changed round
(`ArtSource/phaister/doll-20260927/owner-reference-20260927.png`); *"manually and thoroughly do each detail instead of mass
generating"*.

So v11:
  * PAETE'S SIZE (0.80 rig units with its tuft; Paete is 0.79), a big head, a stout sack body with a fat pot belly sagging over a
    rope belt, short thick legs, heavy arms ending in MITTEN STUMPS. No fingers, no thumbs, nowhere.
  * TEXTURED: a painted cloth atlas (`tools/paint_phaister_doll_cloth.py`) embedded in the glb: a burlap weave per panel, a coarser
    capelet, her purple wraps, a gold rope, straw, and six embroidered patches. Every textured face is projected onto its cloth at
    one density, so the weave is the same size everywhere.
  * THE LIGHT IS INSIDE. Every glowing place is an OPENING: the cloth is torn open, its two torn lips stand up off the surface, and
    the light sits down in the gap: the hottest line at the bottom, the walls lit and dimming toward the lips (vertex colours on
    `glow-mesh`, painted unlit by `SoulGlow`), and the light spilling out over the lips onto the cloth round it (`spill-mesh`,
    additive, `SoulSpill`). The stitches that hold it shut lie across the gap, on the lips, so they cut the light. The crown is the
    biggest opening: the sack's torn top, straw standing out of it, and tongues of light licking up between the straw.
  * THE REFERENCE, WITH THE COLOURS MOVED ROUND INTO HERS: its cream wraps are her royal purple; its natural twine is a gold rope; its
    brown patches are her charcoal coat cloth; its crimson belly patch is her hair's magenta; the head patch is crimson; the pins
    carry her gems (lilac, gold, magenta, crimson); the light is her magenta.

EVERY DETAIL IS TYPED BY HAND with its own numbers: each flap, stitch, wrap turn, pin, charm, lip and straw. Nothing is stamped round
a loop. The geometry helpers (a chamfered box, a disc, a gem, an opening along a typed path) only build what a row describes.

Axes as every builder here: +X is the doll's LEFT, its face is on -Z, feet on y = 0, rig units (the game draws people at 2.38).
It imports `build_phaister_voxel` for the glb reader and writer, the chamfer and the normal smoother only; that builder is never
run from here (re-running it loses Phaister's baked clips).
"""
import json
import math
import os
import struct
import sys
import io

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_phaister_voxel as V  # noqa: E402  (geometry helpers only; see the module note)
import paint_phaister_doll_cloth as CLOTH  # noqa: E402

OUT = "Assets/TumbangPreso/Art/characters/persons/phaister-doll.glb"
PALETTE_OUT = "ArtSource/phaister/doll-20260927/palette.json"
CLOTH_OUT = "ArtSource/phaister/doll-20260927/cloth.png"
# Which cloth the glb carries (`paint_phaister_doll_cloth.STYLES`); the owner picks from the review's `texture-options.png`.
CLOTH_STYLE = "chunky"      # felt (v17) vanished under the street's grade

# ---------------------------------------------------------------------------------------------------------------------
# ITS OWN SKELETON (the rig's seven names, so the gait, animator and bot can drive it). Paete's height: short thick legs, a
# stout body, shoulders wide at the top of the body, a big head sitting on them.
# ---------------------------------------------------------------------------------------------------------------------
SKELETON = {
    "root":      (0.0,    0.0,   0.0),
    "leg-left":  (0.100,  0.150, 0.0),
    "leg-right": (-0.100, 0.150, 0.0),
    "torso":     (0.0,    0.150, 0.0),
    "arm-left":  (0.270,  0.400, 0.0),
    "arm-right": (-0.270, 0.400, 0.0),
    "head":      (0.0,    0.450, 0.0),
}
PARENT = {"leg-left": "root", "leg-right": "root", "torso": "root",
          "arm-left": "torso", "arm-right": "torso", "head": "torso"}
MIN_HEIGHT, MAX_HEIGHT = 0.76, 0.84     # Paete stands 0.791

# ---------------------------------------------------------------------------------------------------------------------
# PALETTE: the FLAT parts only (threads, pins, gems, the button). Every cloth is painted (`CLOTH.REGIONS`). Slot 8 is ink.
# ---------------------------------------------------------------------------------------------------------------------
THREAD_TAN = 0      # loose burlap threads across the openings
WHIP = 1            # the pale thread of the whip stitches
GEM_LILAC = 2
GEM_GOLD = 3
GEM_MAGENTA = 4
GEM_CRIMSON = 5
PIN_SHAFT = 6       # dulled brass
STITCH = 7          # the heavy charcoal-violet thread that holds its openings shut
INK = 8
BUTTON = 9          # her purple
BUTTON_RIM = 10
GOLD_THREAD = 11    # the pouch's drawstring
BONE = 12
CHARCOAL = 13
GLOW = 14           # never drawn by the toon paint (the light is its own mesh); kept so the palette reads
GLOW_CORE = 15

PALETTE = {
    THREAD_TAN:  "c8a270",
    WHIP:        "efdfb8",
    GEM_LILAC:   "a24ae6",
    GEM_GOLD:    "f8b824",
    GEM_MAGENTA: "e82882",
    GEM_CRIMSON: "c01c3a",
    PIN_SHAFT:   "a07030",
    STITCH:      "2a1a36",
    INK:         "14101c",
    BUTTON:      "4a1e78",
    BUTTON_RIM:  "2a0f46",
    GOLD_THREAD: "d8a23a",
    BONE:        "efe2c2",
    CHARCOAL:    "26202e",
    GLOW:        "f03cc8",
    GLOW_CORE:   "ffe6fa",
}

# The light's colours (sRGB), from the bottom of a gap to the top of its walls, and what spills out.
LIGHT_CORE = "ffe8fb"     # the hottest line at the very bottom
LIGHT_HOT = "ff6ee8"      # the floor either side of it, the foot of the walls
LIGHT_SOUL = "f03cc8"     # half way up the walls
LIGHT_DEEP = "9a1684"     # half way down the walls
LIGHT_EMBER = "3a0a36"    # the walls just under the cloth: almost dark, so the mouth of a hole reads as an edge, not a glow
LIGHT_SPILL = "ff4ad8"    # what falls on the cloth outside

# ---------------------------------------------------------------------------------------------------------------------
# THE SACK: the big stuffed forms. Every opening, patch, flap, stitch and pin is placed ON these (ray cast onto the real,
# chamfered surface), so nothing floats or sinks. (name, bone, lo, hi, cloth[, bottom cloth])
# ---------------------------------------------------------------------------------------------------------------------
SACK = [
    # Legs: thick stuffed stumps, a little flare at the foot, a toe bump. The left in the second sack cloth, the right in the first.
    ("leg-l",        "leg-left",  (0.030, 0.030, -0.082), (0.172, 0.176, 0.080), "burlap-b"),
    ("foot-l",       "leg-left",  (0.024, 0.000, -0.098), (0.178, 0.052, 0.086), "burlap-b", "sole"),
    ("toe-l",        "leg-left",  (0.042, 0.000, -0.114), (0.158, 0.040, -0.060), "burlap-b", "sole"),
    ("leg-r",        "leg-right", (-0.174, 0.030, -0.080), (-0.028, 0.176, 0.082), "burlap"),
    ("foot-r",       "leg-right", (-0.180, 0.000, -0.096), (-0.022, 0.052, 0.088), "burlap", "sole"),
    ("toe-r",        "leg-right", (-0.162, 0.000, -0.110), (-0.044, 0.038, -0.058), "burlap", "sole"),

    # The body: a core, the FAT POT BELLY pushed forward and sagging low, flanks bulging at the sides, a rounded back, the
    # capelet over the shoulders, and the shoulders' slope up under the head.
    ("core",         "torso", (-0.198, 0.150, -0.150), (0.198, 0.432, 0.128), "burlap"),
    ("belly",        "torso", (-0.202, 0.168, -0.240), (0.202, 0.370, -0.030), "burlap"),
    ("belly-low",    "torso", (-0.170, 0.146, -0.216), (0.170, 0.210, -0.040), "burlap"),
    ("flank-l",      "torso", (0.150, 0.172, -0.176), (0.228, 0.362, 0.100), "burlap"),
    ("flank-r",      "torso", (-0.228, 0.176, -0.172), (-0.150, 0.358, 0.102), "burlap"),
    ("back",         "torso", (-0.184, 0.198, 0.030), (0.184, 0.418, 0.150), "burlap-b"),
    ("mantle",       "torso", (-0.214, 0.346, -0.184), (0.214, 0.470, 0.162), "mantle"),
    ("mantle-top",   "torso", (-0.180, 0.440, -0.150), (0.180, 0.476, 0.140), "mantle"),

    # Arms, laid out along X as the rig's bind pose has them (the gait hangs them): a thick upper arm, a forearm, a MITTEN STUMP
    # and its rounded end. No fingers.
    ("upper-l",      "arm-left",  (0.200, 0.344, -0.066), (0.358, 0.458, 0.066), "burlap"),
    ("fore-l",       "arm-left",  (0.342, 0.339, -0.070), (0.460, 0.463, 0.070), "burlap-b"),
    ("mitten-l",     "arm-left",  (0.448, 0.330, -0.077), (0.540, 0.470, 0.075), "mitten"),
    ("mitten-end-l", "arm-left",  (0.530, 0.344, -0.063), (0.562, 0.456, 0.061), "mitten"),
    ("upper-r",      "arm-right", (-0.358, 0.346, -0.064), (-0.200, 0.456, 0.068), "burlap-b"),
    ("fore-r",       "arm-right", (-0.464, 0.337, -0.068), (-0.342, 0.465, 0.072), "burlap"),
    ("mitten-r",     "arm-right", (-0.544, 0.328, -0.075), (-0.450, 0.472, 0.077), "mitten"),
    ("mitten-end-r", "arm-right", (-0.566, 0.341, -0.061), (-0.534, 0.458, 0.065), "mitten"),

    # The head: a big rounded sack, puffed at the cheeks, rounder at the back, gathered at the top into the torn crown.
    ("head",         "head", (-0.182, 0.448, -0.156), (0.182, 0.700, 0.152), "head"),
    ("cheek-l",      "head", (0.160, 0.458, -0.132), (0.198, 0.582, 0.060), "head"),
    ("cheek-r",      "head", (-0.200, 0.462, -0.128), (-0.160, 0.586, 0.064), "head"),
    ("head-back",    "head", (-0.152, 0.466, 0.104), (0.152, 0.668, 0.166), "head"),
    ("head-top",     "head", (-0.152, 0.684, -0.128), (0.152, 0.716, 0.124), "head"),
    ("gather",       "head", (-0.112, 0.708, -0.104), (0.112, 0.738, 0.094), "head"),
]

# How round each stuffed form is (its chamfer), typed per part; any part not named takes the shared rule (`V.bevel_for`).
SACK_BEVEL = {
    "leg-l": 0.045, "leg-r": 0.045, "foot-l": 0.024, "foot-r": 0.024, "toe-l": 0.016, "toe-r": 0.016,
    "core": 0.066, "belly": 0.070, "belly-low": 0.024, "flank-l": 0.040, "flank-r": 0.040, "back": 0.045,
    "mantle": 0.030, "mantle-top": 0.016,
    "upper-l": 0.042, "fore-l": 0.040, "mitten-l": 0.040, "mitten-end-l": 0.014,
    "upper-r": 0.042, "fore-r": 0.040, "mitten-r": 0.040, "mitten-end-r": 0.014,
    "head": 0.060, "head-top": 0.012, "cheek-l": 0.016, "cheek-r": 0.016, "head-back": 0.028, "gather": 0.010,
}

# =====================================================================================================================
# THE ENGINE: vectors, frames, a chamfered box in any orientation, the cloth projection, ray casts onto the sack, the openings.
# Nothing here decides where anything goes; the typed rows further down do.
# =====================================================================================================================
def _add(a, b):
    return (a[0] + b[0], a[1] + b[1], a[2] + b[2])


def _sub(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


def _scale(a, s):
    return (a[0] * s, a[1] * s, a[2] * s)


def _lerp(a, b, t):
    return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t)


_dot, _cross, _unit = V._dot, V._cross, V._unit


def _len(a):
    return math.sqrt(_dot(a, a))


def _rot(rx, ry, rz):
    """A 3x3 rotation, Z then Y then X, degrees."""
    ax, ay, az = (math.radians(a) for a in (rx, ry, rz))
    cx, sx, cy, sy, cz, sz = math.cos(ax), math.sin(ax), math.cos(ay), math.sin(ay), math.cos(az), math.sin(az)
    rxm = ((1, 0, 0), (0, cx, -sx), (0, sx, cx))
    rym = ((cy, 0, sy), (0, 1, 0), (-sy, 0, cy))
    rzm = ((cz, -sz, 0), (sz, cz, 0), (0, 0, 1))
    return _mul(rxm, _mul(rym, rzm))


def _mul(a, b):
    return tuple(tuple(sum(a[i][k] * b[k][j] for k in range(3)) for j in range(3)) for i in range(3))


def _apply(m, v):
    return tuple(sum(m[i][k] * v[k] for k in range(3)) for i in range(3))


def _columns(x, y, z):
    """The matrix whose columns are the local axes x, y, z (local to authored)."""
    return tuple(tuple((x, y, z)[k][i] for k in range(3)) for i in range(3))


def frame_on(normal, up=(0.0, 1.0, 0.0)):
    """A proper frame lying on a surface: z out of it, y as near `up` as the surface allows, x = y cross z. On a front face x
    points to the doll's RIGHT (the viewer's left); the cloth projection knows that."""
    z = _unit(normal)
    if abs(_dot(up, z)) > 0.92:
        up = (0.0, 0.0, -1.0)
    y = _unit(_sub(up, _scale(z, _dot(up, z))))
    return _columns(_cross(y, z), y, z)


def frame_along(along, normal):
    """A proper frame with x along a path, z out of the surface, y = z cross x (across the path, in the surface)."""
    z = _unit(normal)
    x = _unit(_sub(along, _scale(z, _dot(along, z))))
    return _columns(x, _cross(z, x), z)


def _axis(m, k):
    return (m[0][k], m[1][k], m[2][k])


def _seed(text):
    return sum((i + 1) * ord(c) for i, c in enumerate(text)) % 9973 / 9973.0


def _srgb(hexcode, alpha=1.0):
    """A hex colour as LINEAR rgba (glTF vertex colours are linear)."""
    def lin(c):
        c /= 255.0
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return tuple(lin(int(hexcode[i:i + 2], 16)) for i in (0, 2, 4)) + (alpha,)


# ---------------------------------------------------------------------------------------------------------------------
# Four meshes: the cloth of the body, the cloth of the head (the rig's head mesh), the light, and the light that spills.
# Everything is authored with the face on -Z and written on +Z (mirror Z, which also reverses the winding).
# ---------------------------------------------------------------------------------------------------------------------
class Mesh:
    def __init__(self):
        self.pos, self.nrm, self.uv, self.col, self.joints, self.weights, self.idx = [], [], [], [], [], [], []
        self.flat = set()

    def poly(self, bone, pts, normal, uvs, colours=None, smooth=True):
        order = list(range(len(pts)))
        if _dot(_newell(pts), normal) < 0:
            order.reverse()                                  # counter-clockwise round its normal, as authored
        order.reverse()                                      # and the Z mirror reverses the winding
        n = (normal[0], normal[1], -normal[2])
        j = V.BONE[bone]
        first = len(self.pos)
        for k in order:
            p = pts[k]
            self.pos.append((p[0], p[1], -p[2]))
            self.nrm.append(n)
            self.uv.append(uvs[k] if uvs else (0.0, 0.0))
            self.col.append(colours[k] if colours else (1.0, 1.0, 1.0, 1.0))
            self.joints.append((j, 0, 0, 0))
            self.weights.append((1.0, 0.0, 0.0, 0.0))
            if not smooth:
                self.flat.add(len(self.pos) - 1)
        for k in range(1, len(pts) - 1):
            self.idx += [first, first + k, first + k + 1]


def _newell(pts):
    """A polygon's area normal (robust to a straight run of corners)."""
    nx = ny = nz = 0.0
    for k in range(len(pts)):
        a, b = pts[k], pts[(k + 1) % len(pts)]
        nx += (a[1] - b[1]) * (a[2] + b[2])
        ny += (a[2] - b[2]) * (a[0] + b[0])
        nz += (a[0] - b[0]) * (a[1] + b[1])
    return (nx, ny, nz)


MESH = {"body": Mesh(), "head": Mesh(), "glow": Mesh(), "spill": Mesh()}
PLACED = []     # (name, bone, centre) of every part, for the bone check


def _cloth_mesh(bone):
    return MESH["head"] if bone == "head" else MESH["body"]


# ---------------------------------------------------------------------------------------------------------------------
# THE CLOTH PROJECTION. A face takes the two local axes across its normal, at `CLOTH.DENSITY` texels a rig unit, from an offset
# picked by the part's name so no two panels start their weave at the same thread. `fit` lays the whole region over the part's
# outward face (a patch, a tag); `long_v` runs the part's length down the region (the rope, the straw).
# ---------------------------------------------------------------------------------------------------------------------
def cloth_uvs(mat, name, pts, n, lo, hi, fit=False, long_v=False, bottom=None):
    if isinstance(mat, int):
        return [V.cell_uv(mat)] * len(pts)
    region = bottom if (bottom and n[1] < -0.7) else mat
    x0, y0, rw, rh = CLOTH.REGIONS[region]
    size = float(CLOTH.SIZE)
    if fit:
        # The outward face (local +z) fills the region, reading left to right as the viewer sees it (local -x).
        return [((x0 + (hi[0] - p[0]) / (hi[0] - lo[0]) * rw) / size,
                 (y0 + (hi[1] - p[1]) / (hi[1] - lo[1]) * rh) / size) for p in pts]
    a = max(range(3), key=lambda i: abs(n[i]))
    ua, va = {0: (2, 1), 1: (0, 2), 2: (0, 1)}[a]
    if long_v:
        ua, va = ({0: (2, 1), 1: (2, 0), 2: (1, 0)}[a])
    ext_u, ext_v = max(hi[ua] - lo[ua], 1e-4), max(hi[va] - lo[va], 1e-4)
    d = min(CLOTH.DENSITY, (rw - 4) / ext_u, (rh - 4) / ext_v)
    s = _seed(name + "uv" + str(a))
    off_u = (rw - ext_u * d) * ((s * 0.6180339) % 1.0)
    off_v = (rh - ext_v * d) * ((s * 0.4142135 + 0.3) % 1.0)
    return [((x0 + off_u + (p[ua] - lo[ua]) * d) / size, (y0 + off_v + (hi[va] - p[va]) * d) / size) for p in pts]


def obox(name, bone, centre, size, m, mat, bevel=None, smooth=True, fit=False, long_v=False, bottom=None):
    """A chamfered box of `size` (local x, y, z) at `centre`, turned by `m` (local to authored), in cloth or a palette slot."""
    h = tuple(s * 0.5 for s in size)
    lo = (-h[0], -h[1], -h[2])
    b = V.bevel_for(lo, h) if bevel is None else min(bevel, 0.95 * min(h))
    mesh = _cloth_mesh(bone)
    for n_local, pts_local in V.box_polygons(lo, h, -1, b):
        pts = [_add(centre, _apply(m, p)) for p in pts_local]
        n = _apply(m, n_local)
        mesh.poly(bone, pts, n, cloth_uvs(mat, name, pts_local, n_local, lo, h, fit, long_v, bottom), smooth=smooth)
    PLACED.append((name, bone, centre))


IDENTITY = _rot(0, 0, 0)


def abox(name, bone, lo, hi, mat, bottom=None, bevel=None):
    """An axis-aligned chamfered box from its corners."""
    centre = tuple((lo[i] + hi[i]) * 0.5 for i in range(3))
    obox(name, bone, centre, tuple(hi[i] - lo[i] for i in range(3)), IDENTITY, mat, bevel=bevel, bottom=bottom)


def bar(name, bone, a, b, width, thick, normal, mat, smooth=False):
    """A thin box running exactly from point a to point b (threads, stitches, straw, pin shafts); `normal` only turns its flat
    side toward whatever it lies on or stands out of."""
    x = _unit(_sub(b, a))
    y = _cross(normal, x)
    if _len(y) < 1e-4:
        y = _cross((1.0, 0.0, 0.0) if abs(x[0]) < 0.9 else (0.0, 1.0, 0.0), x)
    y = _unit(y)
    obox(name, bone, _lerp(a, b, 0.5), (_len(_sub(b, a)), width, thick), _columns(x, y, _cross(x, y)), mat,
         bevel=min(width, thick) * 0.3, smooth=smooth, long_v=True)


# ---------------------------------------------------------------------------------------------------------------------
# RAY CASTS ONTO THE SACK. A detail is typed as a point on a face (front and back: x, y; left and right: z, y; top: x, z) and
# lands on the real chamfered surface there, with that surface's normal.
# ---------------------------------------------------------------------------------------------------------------------
_SACK_POLYS = []


def _sack_polys():
    if not _SACK_POLYS:
        for row in SACK:
            name, bone, lo, hi = row[:4]
            smallest = 0.95 * min((hi[i] - lo[i]) * 0.5 for i in range(3))
            for n, pts in V.box_polygons(lo, hi, -1, min(SACK_BEVEL.get(name, V.bevel_for(lo, hi)), smallest)):
                _SACK_POLYS.append((name, bone, n, pts))
    return _SACK_POLYS


def _inside(pts, n, p):
    sign = 0
    for k in range(len(pts)):
        a, b = pts[k], pts[(k + 1) % len(pts)]
        c = _dot(_cross(_sub(b, a), _sub(p, a)), n)
        if abs(c) < 1e-12:
            continue
        s = 1 if c > 0 else -1
        if sign == 0:
            sign = s
        elif s != sign:
            return False
    return True


def cast(origin, direction, only=None):
    """The first sack surface along a ray: (point, normal). `only`: the sack part names it may land on."""
    best = None
    for name, bone, n, pts in _sack_polys():
        if only and name not in only:
            continue
        denom = _dot(direction, n)
        if denom >= -1e-9:
            continue
        t = _dot(_sub(pts[0], origin), n) / denom
        if t <= 0 or (best and t >= best[0]):
            continue
        hit = _add(origin, _scale(direction, t))
        if _inside(pts, n, hit):
            best = (t, hit, n)
    if best is None:
        raise SystemExit(f"nothing of {only} under {origin} along {direction}")
    return best[1], best[2]


FACE_RAYS = {
    "front": lambda a, b: ((a, b, -2.0), (0.0, 0.0, 1.0)),
    "back":  lambda a, b: ((a, b, 2.0), (0.0, 0.0, -1.0)),
    "left":  lambda a, b: ((2.0, b, a), (-1.0, 0.0, 0.0)),
    "right": lambda a, b: ((-2.0, b, a), (1.0, 0.0, 0.0)),
    "top":   lambda a, b: ((a, 2.0, b), (0.0, -1.0, 0.0)),
}


def on(face, a, b, only):
    origin, direction = FACE_RAYS[face](a, b)
    return cast(origin, direction, only)


def around(angle, y, only, centre=(0.0, 0.0, -0.030)):
    """A point on the sack at a height, seen from outside at an angle (0 is the front, 90 its left side)."""
    t = math.radians(angle)
    out = (math.sin(t), 0.0, -math.cos(t))
    origin = (centre[0] + out[0] * 2.0, y, centre[2] + out[2] * 2.0)
    return cast(origin, _scale(out, -1.0), only)


# =====================================================================================================================
# THE OPENINGS, where the light comes out. ⚠️⚠️ THEY ARE CUT INTO THE BODY. v10 painted the light on the cloth (*"u js drew the
# pink glow in"*); v11 to v16 stood torn lips up round each opening, so every seam, the grin and the eyes POPPED OUT of the doll
# like neon tubes (*"why does it pop out its the opposite it should look like its from withhin"*, *"same with the mouth"*, *"why
# do u pop out his features"*). Now nothing stands off the cloth. An opening is its MOUTH, a flat polygon lying on the cloth where
# the hole is, and its INSIDE, a cup of walls and a floor sunk into the doll below it; `SoulGlow` cuts the cloth away through
# the mouth and draws the cup, so the light is seen DOWN INSIDE the doll, with parallax as the camera moves. The walls are dim
# where they meet the cloth and hot toward the floor; the floor is white-hot along its middle. The light that falls out onto
# the cloth round a hole is the spill (additive). A stitch, a thread, the X or the button lying over a hole cuts the light.
# =====================================================================================================================
MOUTH_LIFT = 0.0008     # the mouth, a hair above the cloth it cuts
SPILL_LIFT = 0.0012     # the spill, above the cloth it lies on

C_CORE = _srgb(LIGHT_CORE, 1.0)     # alpha is how hard it breathes (`SoulGlow`)
C_HOT = _srgb(LIGHT_HOT, 0.8)
C_SOUL = _srgb(LIGHT_SOUL, 0.6)
C_DEEP = _srgb(LIGHT_DEEP, 0.3)
C_EMBER = _srgb(LIGHT_EMBER, 0.1)
MOUTH, INSIDE = 1.0, 0.0            # TEXCOORD0.x on `glow-mesh`: which pass draws a polygon


def _spill(alpha):
    return _srgb(LIGHT_SPILL, alpha)


def _glow(bone, pts, normal, colours, role=INSIDE):
    MESH["glow"].poly(bone, pts, normal, [(role, 0.0)] * len(pts), colours, smooth=False)


def _spill_poly(bone, pts, normal, colours, uvs=None):
    MESH["spill"].poly(bone, pts, normal, uvs or [(0.0, 0.0)] * len(pts), colours, smooth=False)


def seam(name, bone, face, points, gaps, depth, only, spread):
    """A tear cut into the body along typed points: `gaps` the half width of its mouth per segment, `depth` how far the light
    sits below the cloth. Returns the segments, for the stitches that hold it shut (they lie across the mouth, on the cloth)."""
    hits = [on(face, a, b, only) for a, b in points]
    segs = []
    e = 0.0015
    for k in range(len(hits) - 1):
        (p0, n0), (p1, n1) = hits[k], hits[k + 1]
        n = _unit(_add(n0, n1))
        m = frame_along(_sub(p1, p0), n)
        x, y = _axis(m, 0), _axis(m, 1)
        g, gb = gaps[k], gaps[k] * 1.2
        a0, a1 = _add(p0, _scale(x, -e)), _add(p1, _scale(x, e))
        segs.append({"p0": p0, "p1": p1, "m": m, "n": n, "x": x, "y": y, "L": _len(_sub(p1, p0)), "g": g, "h": 0.0,
                     "bone": bone})

        def at(p, across, up):
            return _add(_add(p, _scale(y, across)), _scale(n, up))

        _glow(bone, [at(a0, -g, MOUTH_LIFT), at(a1, -g, MOUTH_LIFT), at(a1, g, MOUTH_LIFT), at(a0, g, MOUTH_LIFT)], n,
              [C_DEEP] * 4, MOUTH)
        for side in (-1.0, 1.0):
            _glow(bone, [at(a0, side * g, 0.0), at(a1, side * g, 0.0), at(a1, side * gb, -depth * 0.45),
                         at(a0, side * gb, -depth * 0.45)], _scale(y, -side), [C_EMBER, C_EMBER, C_DEEP, C_DEEP])
            _glow(bone, [at(a0, side * gb, -depth * 0.45), at(a1, side * gb, -depth * 0.45), at(a1, side * gb, -depth),
                         at(a0, side * gb, -depth)], _scale(y, -side), [C_DEEP, C_DEEP, C_HOT, C_HOT])
        stops = ((-gb, C_SOUL), (-0.35 * gb, C_HOT), (0.0, C_CORE), (0.35 * gb, C_HOT), (gb, C_SOUL))
        for (u0, c0), (u1, c1) in zip(stops, stops[1:]):
            _glow(bone, [at(a0, u0, -depth), at(a1, u0, -depth), at(a1, u1, -depth), at(a0, u1, -depth)], n, [c0, c0, c1, c1])

    # The cup closes at both ends.
    for seg, end, out in ((segs[0], segs[0]["p0"], -1.0), (segs[-1], segs[-1]["p1"], 1.0)):
        x, y, n, g = seg["x"], seg["y"], seg["n"], seg["g"]
        tip = _add(end, _scale(x, out * e))
        _glow(bone, [_add(tip, _scale(y, -g)), _add(tip, _scale(y, g)), _add(_add(tip, _scale(y, g * 1.2)), _scale(n, -depth)),
                     _add(_add(tip, _scale(y, -g * 1.2)), _scale(n, -depth))], _scale(x, -out), [C_EMBER, C_EMBER, C_HOT, C_HOT])

    # The spill: one strip each side along the whole tear, from its edge out over the cloth, fading at both ends.
    count = len(hits)
    for side in (-1.0, 1.0):
        rows = []
        for k in range(count):
            near = [segs[j] for j in (k - 1, k) if 0 <= j < len(segs)]
            y = _scale(_unit(_add(near[0]["y"], near[-1]["y"])), side)
            n = _unit(_add(near[0]["n"], near[-1]["n"]))
            g = sum(sg["g"] for sg in near) / len(near)
            p = hits[k][0]
            fade = 0.3 if k in (0, count - 1) else 1.0
            rows.append((_add(_add(p, _scale(y, g)), _scale(n, SPILL_LIFT)),
                         _add(_add(p, _scale(y, g + spread)), _scale(n, SPILL_LIFT)), n, fade))
        for (i0, o0, n0, f0), (i1, o1, n1, f1) in zip(rows, rows[1:]):
            _spill_poly(bone, [i0, i1, o1, o0], _unit(_add(n0, n1)), [_spill(0.62 * f0), _spill(0.62 * f1), _spill(0.0), _spill(0.0)])
    return segs


def across_seam(name, segs, k, t, length, width, thick, angle, slot=STITCH):
    """A stitch lying across a seam's gap on its lips: segment k, t along it, turned `angle` from straight across."""
    seg = segs[k]
    centre = _add(_lerp(seg["p0"], seg["p1"], t), _scale(seg["n"], seg["h"] + thick * 0.5 + 0.0006))
    m = _mul(seg["m"], _rot(0, 0, 90 + angle))
    obox(name, seg["bone"], centre, (length, width, thick), m, slot, bevel=min(width, thick) * 0.3, smooth=False)


def hole(name, bone, face, centre, radii, depth, only, spread, turn=0.0, flaps=None, cloth=None):
    """A round hole cut into the body: typed radii at even turns round a centre, the light `depth` below the cloth in a cup that
    narrows as it goes in. `flaps` (height, width, lean out) per edge stand the torn cloth up round a hole that is the sack's own
    open top (the crown); no other hole has them. Returns (the centre on the surface, its normal, the rim points)."""
    c, cn = on(face, centre[0], centre[1], only)
    rim = []
    for k, r in enumerate(radii):
        t = math.radians(turn + 360.0 * k / len(radii))
        rim.append(on(face, centre[0] + r * math.cos(t), centre[1] + r * math.sin(t), only))
    count = len(rim)
    inward = _scale(cn, -depth)
    floor_c = _add(c, inward)
    lifted = [_add(p, _scale(n, MOUTH_LIFT)) for p, n in rim]
    bottom = [_add(_lerp(c, p, 1.25), inward) for p, n in rim]
    middle = [_add(_lerp(c, p, 1.12), _scale(inward, 0.45)) for p, n in rim]
    for k in range(count):
        k1 = (k + 1) % count
        _glow(bone, [_add(c, _scale(cn, MOUTH_LIFT)), lifted[k], lifted[k1]], cn, [C_DEEP] * 3, MOUTH)
        _glow(bone, [rim[k][0], rim[k1][0], middle[k1], middle[k]], cn, [C_EMBER, C_EMBER, C_DEEP, C_DEEP])
        _glow(bone, [middle[k], middle[k1], bottom[k1], bottom[k]], cn, [C_DEEP, C_DEEP, C_HOT, C_HOT])
        _glow(bone, [floor_c, _lerp(floor_c, bottom[k], 0.45), _lerp(floor_c, bottom[k1], 0.45)], cn, [C_CORE, C_HOT, C_HOT])
        _glow(bone, [_lerp(floor_c, bottom[k], 0.45), bottom[k], bottom[k1], _lerp(floor_c, bottom[k1], 0.45)], cn,
              [C_HOT, C_SOUL, C_SOUL, C_HOT])
    for k in range(count):
        (p0, n0), (p1, n1) = rim[k], rim[(k + 1) % count]
        r0 = _unit(_sub(_sub(p0, c), _scale(n0, _dot(_sub(p0, c), n0))))
        r1 = _unit(_sub(_sub(p1, c), _scale(n1, _dot(_sub(p1, c), n1))))
        w = 0.0
        if flaps:
            h, w, lean = flaps[k]
            n = _unit(_add(n0, n1))
            mid = _lerp(p0, p1, 0.5)
            outward = _sub(mid, c)
            outward = _unit(_sub(outward, _scale(n, _dot(outward, n))))
            m = frame_along(_sub(p1, p0), n)
            sign = 1.0 if _dot(_axis(m, 1), outward) > 0 else -1.0
            tilt = _mul(m, _rot(-sign * lean, 0, 0))
            base = _add(mid, _scale(outward, w * 0.5))
            obox(f"{name}-flap-{k}", bone, _add(base, _apply(tilt, (0.0, 0.0, h * 0.5))), (_len(_sub(p1, p0)) + 0.004, w, h),
                 tilt, cloth)
        _spill_poly(bone, [_add(_add(p0, _scale(r0, w)), _scale(n0, SPILL_LIFT)),
                           _add(_add(p1, _scale(r1, w)), _scale(n1, SPILL_LIFT)),
                           _add(_add(p1, _scale(r1, w + spread)), _scale(n1, SPILL_LIFT)),
                           _add(_add(p0, _scale(r0, w + spread)), _scale(n0, SPILL_LIFT))],
                    cn, [_spill(0.62), _spill(0.62), _spill(0.0), _spill(0.0)])
    return c, cn, rim


def pit(bone, centre, normal, radius, depth, sides):
    """A small hole with the light in it, cut into whatever it lies on (a buttonhole): a mouth, a short cup, a hot floor."""
    m = frame_on(normal)
    x, y = _axis(m, 0), _axis(m, 1)
    ring = [_add(centre, _add(_scale(x, radius * math.cos(2 * math.pi * k / sides)), _scale(y, radius * math.sin(2 * math.pi * k / sides))))
            for k in range(sides)]
    down = _scale(normal, -depth)
    floor_c = _add(centre, down)
    for k in range(sides):
        k1 = (k + 1) % sides
        _glow(bone, [_add(centre, _scale(normal, MOUTH_LIFT)), _add(ring[k], _scale(normal, MOUTH_LIFT)),
                     _add(ring[k1], _scale(normal, MOUTH_LIFT))], normal, [C_DEEP] * 3, MOUTH)
        _glow(bone, [ring[k], ring[k1], _add(ring[k1], down), _add(ring[k], down)], normal, [C_EMBER, C_EMBER, C_HOT, C_HOT])
        _glow(bone, [floor_c, _add(ring[k], down), _add(ring[k1], down)], normal, [C_CORE, C_HOT, C_HOT])


def glow_disc(bone, centre, normal, radius, sides, inner=C_CORE, outer=C_HOT, turn=0.0):
    """A small round light (the holes in the button), lying on whatever it is lifted off."""
    m = frame_on(normal)
    x, y = _axis(m, 0), _axis(m, 1)
    ring = [_add(centre, _add(_scale(x, radius * math.cos(math.radians(turn + 360.0 * k / sides))),
                              _scale(y, radius * math.sin(math.radians(turn + 360.0 * k / sides))))) for k in range(sides)]
    for k in range(sides):
        _glow(bone, [centre, ring[k], ring[(k + 1) % sides]], normal, [inner, outer, outer])


def halo(name, bone, centre, radius, alpha, planes, sides=14):
    """A soft ball of light hanging in the air: a round fan per typed plane (its normal), full at its middle, gone at its rim."""
    for normal in planes:
        m = frame_on(normal)
        x, y = _axis(m, 0), _axis(m, 1)
        rim = [_add(centre, _add(_scale(x, radius * math.cos(2 * math.pi * k / sides)),
                                 _scale(y, radius * math.sin(2 * math.pi * k / sides)))) for k in range(sides)]
        for k in range(sides):
            _spill_poly(bone, [centre, rim[k], rim[(k + 1) % sides]], _unit(normal),
                        [_srgb(LIGHT_HOT, alpha), _srgb(LIGHT_SOUL, 0.0), _srgb(LIGHT_SOUL, 0.0)])
    PLACED.append((name, bone, centre))


def flame(name, bone, base, height, width, lean, phase):
    """A tongue of light licking up out of the crown: two crossed blades, hot and thick at the foot, gone at the tip. The shader
    flickers it and sways its tip (`SoulSpill`, uv.y is how much a vertex sways)."""
    tip = (base[0] + lean[0], base[1] + height, base[2] + lean[1])
    for side in ((1.0, 0.0, 0.0), (0.0, 0.0, 1.0)):
        hw, tw = width * 0.5, width * 0.08
        pts = [_add(base, _scale(side, -hw)), _add(base, _scale(side, hw)), _add(tip, _scale(side, tw)),
               _add(tip, _scale(side, -tw))]
        normal = _cross(side, (0.0, 1.0, 0.0))
        _spill_poly(bone, pts, normal, [_srgb(LIGHT_HOT, 0.95), _srgb(LIGHT_HOT, 0.95), _srgb(LIGHT_SOUL, 0.0),
                                        _srgb(LIGHT_SOUL, 0.0)], [(phase, 0.0), (phase, 0.0), (phase, 1.0), (phase, 1.0)])
    PLACED.append((name, bone, base))


# ---------------------------------------------------------------------------------------------------------------------
# Small solids in a palette slot: a disc (the button), a ring (its rim), a gem (a pin's head).
# ---------------------------------------------------------------------------------------------------------------------
def _flat(bone, pts, n, slot):
    _cloth_mesh(bone).poly(bone, pts, n, [V.cell_uv(slot)] * len(pts), smooth=False)


def disc(name, bone, centre, m, radius, depth, sides, slot, turn=0.0):
    """A round slab, its faces on local +z and -z."""
    ring = [(radius * math.cos(math.radians(turn + 360.0 * k / sides)), radius * math.sin(math.radians(turn + 360.0 * k / sides)))
            for k in range(sides)]
    for z in (depth * 0.5, -depth * 0.5):
        _flat(bone, [_add(centre, _apply(m, (x, y, z))) for x, y in ring], _apply(m, (0.0, 0.0, 1.0 if z > 0 else -1.0)), slot)
    for k in range(sides):
        (x0, y0), (x1, y1) = ring[k], ring[(k + 1) % sides]
        out = _apply(m, _unit((x0 + x1, y0 + y1, 0.0)))
        _flat(bone, [_add(centre, _apply(m, p)) for p in ((x0, y0, -depth * 0.5), (x1, y1, -depth * 0.5), (x1, y1, depth * 0.5),
                                                        (x0, y0, depth * 0.5))], out, slot)
    PLACED.append((name, bone, centre))


def ring(name, bone, centre, m, r_out, r_in, depth, sides, slot):
    """A flat ring (the raised rim of a button)."""
    def at(r, k, z):
        t = math.radians(360.0 * k / sides)
        return _add(centre, _apply(m, (r * math.cos(t), r * math.sin(t), z)))
    for k in range(sides):
        k1 = (k + 1) % sides
        for z, nz in ((depth * 0.5, 1.0), (-depth * 0.5, -1.0)):
            _flat(bone, [at(r_in, k, z), at(r_out, k, z), at(r_out, k1, z), at(r_in, k1, z)], _apply(m, (0.0, 0.0, nz)), slot)
        mid = math.radians(360.0 * (k + 0.5) / sides)
        radial = _apply(m, (math.cos(mid), math.sin(mid), 0.0))
        _flat(bone, [at(r_out, k, -depth * 0.5), at(r_out, k1, -depth * 0.5), at(r_out, k1, depth * 0.5),
                     at(r_out, k, depth * 0.5)], radial, slot)
        _flat(bone, [at(r_in, k, -depth * 0.5), at(r_in, k1, -depth * 0.5), at(r_in, k1, depth * 0.5),
                     at(r_in, k, depth * 0.5)], _scale(radial, -1.0), slot)
    PLACED.append((name, bone, centre))


def gem(name, bone, centre, axis, size, slot, twist=0.0):
    """A cut gem on a pin: a point each end, a six-sided girdle, flat facets so the toon light catches them one by one."""
    y = _unit(axis)
    x = _unit(_cross(y, (1.0, 0.0, 0.0) if abs(y[0]) < 0.9 else (0.0, 0.0, 1.0)))
    m = _columns(x, y, _cross(x, y))
    tip, girdle, radius = size * 0.62, size * 0.18, size * 0.5
    up = [(radius * math.cos(math.radians(twist + 60.0 * k)), girdle, radius * math.sin(math.radians(twist + 60.0 * k)))
          for k in range(6)]
    lo = [(p[0], -girdle, p[2]) for p in up]
    faces = []
    for k in range(6):
        k1 = (k + 1) % 6
        faces.append([(0.0, tip, 0.0), up[k1], up[k]])
        faces.append([up[k], up[k1], lo[k1], lo[k]])
        faces.append([(0.0, -tip, 0.0), lo[k], lo[k1]])
    for f in faces:
        pts = [_add(centre, _apply(m, p)) for p in f]
        mid = _scale(_add(_add(f[0], f[1]), f[2]), 1.0 / 3.0)
        n = _unit(_newell(pts))
        if _dot(n, _apply(m, mid)) < 0:
            n = _scale(n, -1.0)
        _flat(bone, pts, n, slot)
    PLACED.append((name, bone, centre))


def flap(name, bone, face, a, b, width, length, tilt, twist, cloth, only, tongue=None):
    """A torn flap hanging from a point on the sack: `tilt` lifts its foot off the surface, `twist` turns it in the surface,
    `tongue` (side, width share, length share) tears one corner longer so no edge is straight."""
    p, n = on(face, a, b, only)
    m = _mul(frame_on(n), _mul(_rot(0, 0, twist), _rot(-tilt, 0, 0)))
    thick = 0.007
    base = _add(p, _scale(n, thick * 0.5 + 0.0012))
    obox(name, bone, _add(base, _apply(m, (0.0, -length * 0.5 + 0.006, 0.0))), (width, length, thick), m, cloth)
    if tongue:
        side, share_w, share_l = tongue
        tw, tl = width * share_w, length * share_l
        obox(name + "-tongue", bone, _add(base, _apply(m, (side * (width - tw) * 0.5, -length + 0.008 - tl * 0.5, 0.0))),
             (tw, tl, thick * 0.9), m, cloth)


def patch(name, bone, face, a, b, size, angle, region, only):
    """An embroidered patch sewn flat onto the sack; its painted region fills its face."""
    p, n = on(face, a, b, only)
    m = _mul(frame_on(n), _rot(0, 0, angle))
    thick = 0.004
    obox(name, bone, _add(p, _scale(n, thick * 0.5 + 0.0004)), (size[0], size[1], thick), m, region, bevel=0.0012, fit=True,
         smooth=False)


def whip(name, bone, face, a, b, angle, only, length=0.024):
    """One whip stitch: a short pale thread slanting over a seam."""
    p, n = on(face, a, b, only)
    m = _mul(frame_on(n), _rot(0, 0, angle))
    obox(name, bone, _add(p, _scale(n, 0.0017)), (length, 0.0048, 0.0034), m, WHIP, bevel=0.0012, smooth=False)


def pin(name, bone, face, a, b, direction, length, gem_slot, gem_size, only, twist=0.0):
    """A pin pushed into the sack: its shaft sunk in, its gem head out in the air."""
    p, n = on(face, a, b, only)
    d = _unit(direction)
    tip = _add(p, _scale(d, length))
    bar(name, bone, _add(p, _scale(d, -0.022)), tip, 0.0085, 0.0085, n, PIN_SHAFT)
    gem(name + "-gem", bone, _add(tip, _scale(d, gem_size * 0.5)), d, gem_size, gem_slot, twist)


def rope_belt(name, bone, stations, radius, only, centre=(0.0, 0.0, -0.030)):
    """A rope round the body: typed stations (angle from the front, height), each landed on the sack; the rope runs between
    them a little sunk in, where it is cinched. Returns the landed points and normals."""
    hits = [around(angle, y, only, centre) for angle, y in stations]
    for k in range(len(hits)):
        (p0, n0), (p1, n1) = hits[k], hits[(k + 1) % len(hits)]
        a = _add(p0, _scale(n0, radius * 0.8))
        b = _add(p1, _scale(n1, radius * 0.8))
        along = _sub(b, a)
        m = frame_along(along, _unit(_add(n0, n1)))
        obox(f"{name}-{k}", bone, _lerp(a, b, 0.5), (_len(along) + radius * 1.1, radius * 2.0, radius * 2.0), m, "rope",
             long_v=True, bevel=radius * 0.7)
    return hits


# =====================================================================================================================
# EVERY DETAIL, TYPED. Read top to bottom: the face, the crown, the body's openings, its cloth and bindings, its charms, pins.
# =====================================================================================================================
FACE = ["head"]


def build_details():
    # --- THE BUTTON EYE (its right): a torn socket, the light down in it, the button sewn over it standing off the face so the
    # light leaks all round its rim; four holes the light shows through; the thread crossed through them.
    c, cn, _ = hole("socket-button", "head", "front", (-0.064, 0.596),
        [0.043, 0.045, 0.042, 0.044, 0.046, 0.043, 0.041, 0.044, 0.045, 0.042, 0.044, 0.043], 0.034, FACE, 0.032, turn=8.0)
    bm = _mul(frame_on(cn), _rot(5, -6, 12))
    face_at = _add(c, _scale(cn, 0.0085))
    disc("button", "head", face_at, bm, 0.039, 0.010, 18, BUTTON)
    front = _add(face_at, _apply(bm, (0.0, 0.0, 0.005)))
    ring("button-rim", "head", _add(front, _apply(bm, (0.0, 0.0, 0.0018))), bm, 0.039, 0.031, 0.0036, 18, BUTTON_RIM)
    for k, (hx, hy) in enumerate(((0.0095, 0.0098), (-0.0098, 0.0094), (-0.0094, -0.0097), (0.0097, -0.0095))):
        pit("head", _add(front, _apply(bm, (hx, hy, 0.0))), _apply(bm, (0.0, 0.0, 1.0)), 0.0055, 0.007, 8)
    for k, (a, b) in enumerate((((0.0095, 0.0098), (-0.0094, -0.0097)), ((-0.0098, 0.0094), (0.0097, -0.0095)))):
        pa = _add(front, _apply(bm, (a[0], a[1], 0.0022)))
        pb = _add(front, _apply(bm, (b[0], b[1], 0.0022)))
        bar(f"button-thread-{k}", "head", _add(pa, _scale(_sub(pa, pb), 0.18)), _add(pb, _scale(_sub(pb, pa), 0.18)), 0.0042,
            0.003, _apply(bm, (0.0, 0.0, 1.0)), STITCH)

    # --- THE X EYE (its left): a smaller torn hole, the light in it, the heavy X stitched across it, knotted at its ends.
    c, cn, _ = hole("socket-x", "head", "front", (0.066, 0.594),
        [0.034, 0.030, 0.036, 0.032, 0.029, 0.035, 0.031, 0.036, 0.030, 0.033], 0.034, FACE, 0.026, turn=20.0)
    xm = frame_on(cn)
    for name, angle, half, lift in (("x-a", 41.0, 0.047, 0.0062), ("x-b", -44.0, 0.045, 0.0052)):
        d = _apply(xm, (math.cos(math.radians(angle)), math.sin(math.radians(angle)), 0.0))
        mid = _add(c, _scale(cn, lift))
        bar(name, "head", _add(mid, _scale(d, -half)), _add(mid, _scale(d, half)), 0.016, 0.009, cn, STITCH)
        for end, sign in (("a", -1.0), ("b", 1.0)):
            knot_at = _add(_add(c, _scale(d, sign * (half + 0.002))), _scale(cn, 0.005))
            obox(f"{name}-knot-{end}", "head", knot_at, (0.013, 0.013, 0.010), _mul(xm, _rot(0, 0, angle + 12 * sign)), STITCH,
                 smooth=False)

    # --- THE GRIN: a wide slit torn across the lower face, higher at its left end, the light inside, stitched shut.
    grin = seam("grin", "head", "front",
        [(-0.114, 0.544), (-0.094, 0.534), (-0.066, 0.526), (-0.034, 0.521), (0.000, 0.519), (0.034, 0.521),
         (0.066, 0.527), (0.094, 0.537), (0.116, 0.550)],
        [0.005, 0.008, 0.009, 0.010, 0.010, 0.009, 0.008, 0.006], 0.030, FACE, 0.028)
    for k, (seg, t, angle, length) in enumerate(((0, 0.55, -8, 0.040), (1, 0.50, -5, 0.046), (2, 0.35, -3, 0.050),
                                                (2, 0.95, 2, 0.052), (3, 0.60, 0, 0.054), (4, 0.40, 3, 0.054),
                                                (5, 0.10, -2, 0.052), (5, 0.80, 4, 0.050), (6, 0.65, 6, 0.046),
                                                (7, 0.50, 9, 0.040))):
        across_seam(f"grin-stitch-{k}", grin, seg, t, length, 0.0078, 0.0052, angle)
    across_seam("grin-fray-0", grin, 3, 0.2, 0.026, 0.0022, 0.0022, 24, THREAD_TAN)
    across_seam("grin-fray-1", grin, 6, 0.3, 0.024, 0.0022, 0.0022, -30, THREAD_TAN)

    # --- The face panel's side seams, whip-stitched in pale thread (none glow: the face panel is sewn, not split).
    for k, (x, y, angle) in enumerate(((-0.141, 0.628, 62), (-0.139, 0.606, 58), (-0.142, 0.584, 64), (-0.140, 0.561, 60),
                                       (-0.141, 0.538, 57), (-0.139, 0.516, 63))):
        whip(f"whip-face-r-{k}", "head", "front", x, y, angle, FACE)
    for k, (x, y, angle) in enumerate(((0.140, 0.630, -60), (0.142, 0.607, -64), (0.139, 0.585, -58), (0.141, 0.562, -62),
                                       (0.140, 0.540, -59), (0.142, 0.518, -63))):
        whip(f"whip-face-l-{k}", "head", "front", x, y, angle, FACE)

    # --- THE CROWN: the sack's torn top. Its flaps curl out; the light is down inside it; straw stands out of it and tongues of
    # light lick up between the straw. The tie cinches it below, knotted on its left.
    hole("crown", "head", "top", (0.0, -0.006),
        [0.074, 0.080, 0.072, 0.078, 0.082, 0.073, 0.077, 0.081, 0.074, 0.079, 0.076, 0.072], 0.030, ["gather"], 0.030,
        turn=4.0,
        flaps=[(0.040, 0.009, 30), (0.028, 0.008, 40), (0.046, 0.010, 24), (0.032, 0.008, 44), (0.042, 0.009, 28),
         (0.026, 0.008, 36), (0.044, 0.010, 26), (0.030, 0.008, 42), (0.040, 0.009, 32), (0.027, 0.008, 46),
          (0.045, 0.010, 22), (0.033, 0.008, 38)],
        cloth="head")
    for name, base, top, width, depth in (
            ("straw-1", (-0.046, 0.010), (-0.074, 0.800, 0.020), 0.013, 0.008),
            ("straw-2", (-0.028, -0.028), (-0.050, 0.814, -0.054), 0.011, 0.009),
            ("straw-3", (-0.008, 0.028), (-0.014, 0.808, 0.052), 0.015, 0.009),
            ("straw-4", (0.012, -0.020), (0.018, 0.822, -0.030), 0.012, 0.009),
            ("straw-5", (0.032, 0.018), (0.058, 0.806, 0.034), 0.014, 0.009),
            ("straw-6", (0.048, -0.028), (0.082, 0.794, -0.050), 0.011, 0.008),
            ("straw-7", (-0.056, -0.036), (-0.090, 0.786, -0.056), 0.010, 0.008),
            ("straw-8", (0.056, 0.028), (0.088, 0.784, 0.048), 0.011, 0.008),
            ("straw-9", (0.000, 0.000), (-0.004, 0.828, 0.004), 0.016, 0.010),
            ("straw-10", (-0.020, 0.052), (-0.034, 0.792, 0.080), 0.010, 0.008),
            ("straw-11", (0.022, -0.054), (0.034, 0.796, -0.084), 0.011, 0.008),
            ("straw-12", (0.060, -0.004), (0.094, 0.790, -0.004), 0.012, 0.008)):
        bar(name, "head", (base[0], 0.712, base[1]), top, width, depth, (0.0, 0.0, -1.0), "straw", smooth=True)
    for name, base, height, width, lean, phase in (
            ("flame-1", (0.010, -0.004), 0.118, 0.046, (0.004, -0.006), 0.00),
            ("flame-2", (-0.034, 0.012), 0.092, 0.040, (-0.012, 0.004), 0.31),
            ("flame-3", (0.040, 0.020), 0.100, 0.038, (0.014, 0.008), 0.57),
            ("flame-4", (-0.022, -0.040), 0.080, 0.034, (-0.006, -0.014), 0.83),
            ("flame-5", (0.046, -0.036), 0.086, 0.034, (0.014, -0.012), 0.12),
            ("flame-6", (-0.050, -0.012), 0.070, 0.030, (-0.016, 0.000), 0.66),
            ("flame-7", (0.004, 0.044), 0.076, 0.032, (0.000, 0.014), 0.44),
            ("flame-8", (-0.010, 0.020), 0.104, 0.040, (-0.004, 0.006), 0.21),
            ("flame-9", (0.024, -0.020), 0.094, 0.036, (0.008, -0.006), 0.74),
            ("flame-10", (-0.040, -0.030), 0.062, 0.028, (-0.012, -0.010), 0.93)):
        flame(name, "head", (base[0], 0.710, base[1]), height + 0.028, width, lean, phase)
    halo("crown-halo", "head", (0.0, 0.772, -0.006), 0.110, 0.55,
         [(0.0, 0.0, -1.0), (0.866, 0.0, -0.5), (-0.866, 0.0, -0.5), (0.0, 1.0, 0.0)])
    tie = rope_belt("crown-tie", "head", ((0, 0.716), (32, 0.717), (64, 0.718), (96, 0.717), (128, 0.716), (160, 0.715),
                                          (192, 0.715), (224, 0.716), (256, 0.717), (288, 0.718), (320, 0.717)),
                    0.0068, ["gather"], centre=(0.0, 0.0, -0.005))
    kp, kn = tie[1]
    knot = _add(kp, _scale(kn, 0.012))
    obox("crown-knot", "head", knot, (0.024, 0.020, 0.020), _mul(frame_on(kn), _rot(0, 0, 20)), "rope")
    bar("crown-end-1", "head", _add(knot, (0.004, -0.006, 0.0)), _add(knot, (0.022, -0.040, -0.012)), 0.010, 0.010, kn, "rope")
    bar("crown-end-2", "head", _add(knot, (-0.004, -0.006, 0.0)), _add(knot, (0.004, -0.036, -0.018)), 0.009, 0.009, kn, "rope")

    # --- THE BACK OF THE HEAD: split from the crown down, stitched with short dark bars; a crimson patch with a gold rune.
    crown_back = seam("seam-crown-back", "head", "back", [(0.004, 0.708), (-0.004, 0.698), (0.002, 0.688)], [0.005, 0.006],
        0.024, ["head-top", "head"], 0.022)
    head_back = seam("seam-head-back", "head", "back",
        [(0.004, 0.652), (-0.008, 0.630), (0.006, 0.606), (-0.006, 0.582), (0.004, 0.558), (-0.008, 0.534),
         (0.004, 0.510), (-0.002, 0.488)],
        [0.007, 0.009, 0.010, 0.009, 0.010, 0.008, 0.007], 0.032, ["head-back"], 0.026)
    across_seam("st-crown-back-0", crown_back, 0, 0.5, 0.036, 0.0070, 0.0050, 4)
    for k, (seg, t, angle, length) in enumerate(((0, 0.40, -6, 0.040), (1, 0.70, 5, 0.044), (2, 0.90, -3, 0.044),
                                                (4, 0.20, 7, 0.042), (5, 0.60, -4, 0.040), (6, 0.80, 3, 0.036))):
        across_seam(f"st-head-back-{k}", head_back, seg, t, length, 0.0070, 0.0050, angle)
    patch("patch-head", "head", "back", 0.082, 0.598, (0.058, 0.058), 45 + 7, "patch-head", ["head-back"])

    # --- THE FRONT SEAM: split from under its chin, over the capelet and down the belly to the belt, big X stitches across it.
    chest = seam("seam-chest", "torso", "front",
        [(-0.012, 0.456), (-0.020, 0.438), (-0.010, 0.418), (-0.018, 0.398), (-0.012, 0.380)], [0.007, 0.009, 0.010, 0.008],
        0.034, ["mantle", "mantle-top"], 0.030)
    front = seam("seam-front", "torso", "front",
        [(-0.014, 0.362), (-0.020, 0.340), (-0.010, 0.318), (-0.018, 0.296), (-0.008, 0.274), (-0.016, 0.252), (-0.010, 0.230)],
        [0.009, 0.011, 0.012, 0.011, 0.012, 0.010], 0.042, ["belly"], 0.034)
    for k, (seg, t, length, a1, a2) in enumerate(((1, 0.50, 0.044, 38, -42), (3, 0.40, 0.046, 44, -38))):
        across_seam(f"x-chest-{k}a", chest, seg, t, length, 0.0080, 0.0055, a1)
        across_seam(f"x-chest-{k}b", chest, seg, t, length * 0.96, 0.0080, 0.0050, a2)
    for k, (seg, t, length, a1, a2) in enumerate(((1, 0.60, 0.052, 40, -45), (2, 0.95, 0.054, 36, -40),
                                                 (4, 0.30, 0.054, 42, -39), (5, 0.85, 0.050, 39, -44))):
        across_seam(f"x-front-{k}a", front, seg, t, length, 0.0082, 0.0055, a1)
        across_seam(f"x-front-{k}b", front, seg, t, length * 0.96, 0.0082, 0.0050, a2)
    across_seam("fray-chest-0", chest, 2, 0.80, 0.030, 0.0022, 0.0022, 12, THREAD_TAN)
    across_seam("fray-front-0", front, 3, 0.35, 0.032, 0.0022, 0.0022, -18, THREAD_TAN)
    across_seam("fray-front-1", front, 0, 0.60, 0.028, 0.0022, 0.0022, 25, THREAD_TAN)

    # --- THE BACK: split down the capelet and the back panel, ladder-stitched in dark bars.
    mantle_back = seam("seam-mantle-back", "torso", "back",
        [(0.000, 0.452), (0.008, 0.430), (-0.004, 0.408), (0.006, 0.386), (-0.002, 0.368)], [0.007, 0.009, 0.009, 0.008], 0.034,
        ["mantle", "mantle-top", "back"], 0.030)
    back = seam("seam-back", "torso", "back",
        [(0.004, 0.350), (-0.006, 0.326), (0.006, 0.300), (-0.004, 0.274), (0.006, 0.248), (-0.002, 0.224), (0.004, 0.204)],
        [0.009, 0.010, 0.011, 0.010, 0.010, 0.008], 0.042, ["back", "core"], 0.032)
    for k, (seg, t, angle, length) in enumerate(((0, 0.30, 5, 0.042), (1, 0.70, -4, 0.044), (2, 0.90, 6, 0.042),
                                                (3, 0.50, -6, 0.040))):
        across_seam(f"st-mantle-back-{k}", mantle_back, seg, t, length, 0.0072, 0.0050, angle)
    for k, (seg, t, angle, length) in enumerate(((0, 0.40, -5, 0.046), (1, 0.60, 4, 0.048), (2, 0.50, -7, 0.050),
                                                (3, 0.30, 5, 0.048), (4, 0.70, -3, 0.046), (5, 0.50, 6, 0.042))):
        across_seam(f"st-back-{k}", back, seg, t, length, 0.0074, 0.0052, angle)
    across_seam("fray-back-0", back, 2, 0.20, 0.032, 0.0022, 0.0022, -22, THREAD_TAN)
    patch("patch-back", "torso", "back", 0.100, 0.300, (0.062, 0.068), 6, "patch-back", ["back"])

    # --- THE LEGS: each split down its outside, the light in it.
    leg_l = seam("seam-leg-l", "leg-left", "left", [(0.004, 0.138), (-0.006, 0.124), (0.004, 0.110), (-0.003, 0.096)],
        [0.006, 0.008, 0.007], 0.030, ["leg-l"], 0.026)
    leg_r = seam("seam-leg-r", "leg-right", "right", [(-0.002, 0.138), (0.006, 0.122), (-0.004, 0.106), (0.004, 0.092)],
        [0.007, 0.008, 0.006], 0.030, ["leg-r"], 0.026)
    across_seam("st-leg-l-0", leg_l, 1, 0.5, 0.036, 0.0070, 0.0050, 5)
    across_seam("st-leg-r-0", leg_r, 1, 0.4, 0.036, 0.0070, 0.0050, -6)
    patch("patch-thigh", "leg-left", "front", 0.100, 0.112, (0.052, 0.046), 7, "patch-thigh", ["leg-l"])
    patch("patch-calf", "leg-right", "back", -0.098, 0.108, (0.050, 0.048), -7, "patch-calf", ["leg-r"])

    # --- THE BELLY'S PATCHES: her charcoal cloth as a diamond with lilac crosshatch (its left), her magenta with a rune (its right).
    patch("patch-chest", "torso", "front", 0.106, 0.408, (0.044, 0.044), 45 - 5, "patch-chest", ["mantle"])
    patch("patch-belly", "torso", "front", -0.090, 0.269, (0.050, 0.048), -8, "patch-belly", ["belly"])

    # --- THE CAPELET'S TORN FRINGE: in front it lies on the belly's top, at the sides on the flanks, behind on the back panel.
    fringe_front = ["belly"]
    for name, a, b, width, length, tilt, twist, tongue in (
            ("fringe-f1", 0.150, 0.364, 0.050, 0.036, 6, 4, (1, 0.40, 0.45)),
            ("fringe-f2", 0.092, 0.366, 0.056, 0.030, 5, -6, (-1, 0.38, 0.55)),
            ("fringe-f3", 0.042, 0.366, 0.034, 0.024, 4, 8, (1, 0.50, 0.40)),
            ("fringe-f4", -0.072, 0.366, 0.044, 0.034, 6, -3, (-1, 0.42, 0.50)),
            ("fringe-f5", -0.128, 0.364, 0.058, 0.028, 5, 7, (1, 0.36, 0.60)),
            ("fringe-f6", -0.176, 0.360, 0.040, 0.036, 7, -9, (-1, 0.45, 0.40))):
        flap(name, "torso", "front", a, b, width, length, tilt, twist, "mantle", fringe_front, tongue)
    for name, face, a, b, width, length, tilt, twist, tongue, only in (
            ("fringe-l1", "left", -0.110, 0.360, 0.046, 0.032, 10, 5, (1, 0.40, 0.50), ["flank-l", "mantle"]),
            ("fringe-l2", "left", 0.090, 0.362, 0.050, 0.030, 9, -4, None, ["flank-l", "mantle"]),
            ("fringe-r1", "right", -0.112, 0.358, 0.048, 0.034, 10, -6, (-1, 0.44, 0.45), ["flank-r", "mantle"]),
            ("fringe-r2", "right", 0.094, 0.362, 0.042, 0.028, 8, 5, None, ["flank-r", "mantle"]),
            ("fringe-b1", "back", 0.150, 0.362, 0.052, 0.040, 8, -5, (1, 0.40, 0.50), ["back", "mantle"]),
            ("fringe-b2", "back", 0.086, 0.364, 0.044, 0.030, 6, 6, None, ["back", "mantle"]),
            ("fringe-b3", "back", 0.040, 0.362, 0.030, 0.034, 7, -8, (-1, 0.50, 0.40), ["back", "mantle"]),
            ("fringe-b4", "back", -0.058, 0.364, 0.050, 0.036, 7, 4, (1, 0.42, 0.55), ["back", "mantle"]),
            ("fringe-b5", "back", -0.124, 0.362, 0.046, 0.030, 6, -7, None, ["back", "mantle"]),
            ("fringe-b6", "back", -0.176, 0.360, 0.040, 0.038, 9, 8, (-1, 0.46, 0.42), ["back", "mantle"])):
        flap(name, "torso", face, a, b, width, length, tilt, twist, "mantle", only, tongue)

    # --- THE SACK'S TORN HEM, over the tops of its legs.
    for name, face, a, b, width, length, tilt, twist, tongue, only in (
            ("hem-f1", "front", 0.138, 0.178, 0.040, 0.030, 8, 5, (1, 0.40, 0.50), ["belly-low"]),
            ("hem-f2", "front", -0.150, 0.176, 0.044, 0.026, 7, -6, (-1, 0.45, 0.45), ["belly-low"]),
            ("hem-l1", "left", -0.060, 0.190, 0.040, 0.030, 10, 4, None, ["flank-l", "core"]),
            ("hem-l2", "left", 0.060, 0.186, 0.046, 0.028, 9, -5, (1, 0.40, 0.50), ["flank-l", "core"]),
            ("hem-r1", "right", -0.050, 0.188, 0.044, 0.032, 10, -4, (-1, 0.42, 0.45), ["flank-r", "core"]),
            ("hem-r2", "right", 0.066, 0.190, 0.038, 0.026, 8, 6, None, ["flank-r", "core"]),
            ("hem-b1", "back", 0.110, 0.206, 0.048, 0.034, 8, -5, (1, 0.40, 0.50), ["back", "core"]),
            ("hem-b2", "back", -0.060, 0.204, 0.044, 0.030, 7, 6, None, ["back", "core"]),
            ("hem-b3", "back", -0.150, 0.202, 0.040, 0.036, 9, -7, (-1, 0.45, 0.45), ["back", "core"])):
        flap(name, "torso", face, a, b, width, length, tilt, twist, "hem", only, tongue)

    # --- THE ROPE BELT: gold, sagging under the belly at the front, higher at the back, knotted on its left front.
    stations = ((0, 0.196), (28, 0.197), (56, 0.200), (84, 0.204), (112, 0.208), (140, 0.211), (168, 0.213), (196, 0.213),
                (224, 0.211), (252, 0.207), (280, 0.203), (308, 0.199), (336, 0.197))
    hits = rope_belt("rope", "torso", stations, 0.011, ["core", "belly", "belly-low", "flank-l", "flank-r", "back"])
    kp, kn = hits[1]
    knot = _add(kp, _scale(kn, 0.020))
    obox("rope-knot-a", "torso", knot, (0.030, 0.026, 0.024), _mul(frame_on(kn), _rot(0, 0, 18)), "rope")
    obox("rope-knot-b", "torso", _add(knot, (0.006, -0.010, -0.004)),
     (0.022, 0.028, 0.022), _mul(frame_on(kn), _rot(8, 0, -30)),
         "rope")
    bar("rope-end-1", "torso", _add(knot, (0.004, -0.012, 0.0)), _add(knot, (0.016, -0.066, -0.010)), 0.016, 0.016, kn, "rope")
    bar("rope-end-2", "torso", _add(knot, (-0.006, -0.012, 0.0)), _add(knot, (-0.010, -0.056, -0.012)), 0.015, 0.015, kn, "rope")
    obox("rope-end-1-fray", "torso", _add(knot, (0.017, -0.072, -0.011)), (0.020, 0.014, 0.018), _rot(0, 10, 12), "straw")
    obox("rope-end-2-fray", "torso", _add(knot, (-0.011, -0.061, -0.013)), (0.018, 0.013, 0.017), _rot(0, -8, -10), "straw")

    # --- CHARMS on the belt: a little purple bundle doll, a bone tag with a crimson rune, a charcoal pouch on a gold string, and a
    # second bundle at its right hip. Each hangs from the rope where it lands (a station's angle and height), in front of it.
    rope_on = ["core", "belly", "belly-low", "flank-l", "flank-r", "back"]

    def hang(angle, y):
        p, n = around(angle, y, rope_on)
        return _add(p, _scale(n, 0.024)), n

    def off(anchor, n, dx, dy, out):
        return _add(_add(anchor, (dx, dy, 0.0)), _scale(n, out))

    a, n = hang(-14, 0.1965)
    face = frame_on(n)
    bar("charm-doll-loop", "torso", off(a, n, 0.0, 0.006, 0.0), off(a, n, 0.001, -0.012, 0.004), 0.005, 0.005, n, GOLD_THREAD)
    obox("charm-doll-head", "torso", off(a, n, 0.001, -0.022, 0.006),
     (0.026, 0.024, 0.024), _mul(face, _rot(0, 0, 4)), "bundle")
    obox("charm-doll-neck", "torso", off(a, n, 0.001, -0.0355, 0.006), (0.029, 0.006, 0.027), _mul(face, _rot(0, 0, 4)),
         GOLD_THREAD, smooth=False)
    obox("charm-doll-body", "torso", off(a, n, 0.0015, -0.058, 0.006),
     (0.036, 0.042, 0.030), _mul(face, _rot(0, 0, 3)), "bundle")
    obox("charm-doll-arm-l", "torso", off(a, n, 0.022, -0.048, 0.006), (0.014, 0.008, 0.010), _mul(face, _rot(0, 0, 30)),
         "bundle")
    obox("charm-doll-arm-r", "torso", off(a, n, -0.020, -0.046, 0.006), (0.014, 0.008, 0.010), _mul(face, _rot(0, 0, -26)),
         "bundle")
    bar("charm-doll-straw-1", "torso", off(a, n, -0.005, -0.077, 0.006), off(a, n, -0.011, -0.092, 0.008), 0.005, 0.004, n,
        "straw")
    bar("charm-doll-straw-2", "torso", off(a, n, 0.002, -0.077, 0.006), off(a, n, 0.004, -0.094, 0.009), 0.005, 0.004, n, "straw")
    bar("charm-doll-straw-3", "torso", off(a, n, 0.008, -0.077, 0.006), off(a, n, 0.015, -0.090, 0.007), 0.005, 0.004, n,
        "straw")

    a, n = hang(12, 0.1962)
    bar("charm-tag-loop", "torso", off(a, n, 0.0, 0.006, 0.0), off(a, n, -0.001, -0.018, 0.003), 0.004, 0.004, n, GOLD_THREAD)
    obox("charm-tag", "torso", off(a, n, -0.001, -0.048, 0.004),
     (0.030, 0.056, 0.005), _mul(frame_on(n), _rot(0, 0, -6)), "tag",
         bevel=0.0012, fit=True, smooth=False)

    a, n = hang(46, 0.2005)
    face = frame_on(n)
    bar("charm-pouch-cord", "torso", off(a, n, 0.0, 0.006, 0.0), off(a, n, 0.0, -0.012, 0.004), 0.0045, 0.0045, n, GOLD_THREAD)
    obox("charm-pouch-top", "torso", off(a, n, 0.0, -0.011, 0.006), (0.036, 0.010, 0.030), _mul(face, _rot(0, 8, 0)), "pouch")
    obox("charm-pouch-neck", "torso", off(a, n, 0.0, -0.022, 0.006), (0.026, 0.014, 0.022), _mul(face, _rot(0, 8, 0)), "pouch")
    obox("charm-pouch-string", "torso", off(a, n, 0.0, -0.0235, 0.006), (0.030, 0.0055, 0.026), _mul(face, _rot(0, 8, 0)),
         GOLD_THREAD, smooth=False)
    obox("charm-pouch", "torso", off(a, n, 0.0005, -0.052, 0.010), (0.054, 0.048, 0.040), _mul(face, _rot(0, 8, 4)), "pouch")
    bar("charm-pouch-tail-1", "torso", off(a, n, 0.010, -0.024, 0.018), off(a, n, 0.020, -0.044, 0.022), 0.004, 0.004, n,
        GOLD_THREAD)
    bar("charm-pouch-tail-2", "torso", off(a, n, 0.006, -0.024, 0.019), off(a, n, 0.008, -0.048, 0.024), 0.004, 0.004, n,
        GOLD_THREAD)

    a, n = hang(-84, 0.2045)
    face = frame_on(n)
    bar("charm-hip-loop", "torso", off(a, n, 0.0, 0.006, 0.0), off(a, n, 0.0, -0.012, 0.002), 0.004, 0.004, n, GOLD_THREAD)
    obox("charm-hip-bundle", "torso", off(a, n, 0.0, -0.034, 0.006),
     (0.024, 0.036, 0.026), _mul(face, _rot(0, 0, -6)), "bundle")
    obox("charm-hip-tie", "torso", off(a, n, 0.0, -0.020, 0.006), (0.027, 0.005, 0.029), _mul(face, _rot(0, 0, -6)),
         GOLD_THREAD, smooth=False)
    bar("charm-hip-straw-1", "torso", off(a, n, -0.003, -0.016, 0.006), off(a, n, -0.010, -0.002, 0.008), 0.004, 0.004, n,
        "straw")
    bar("charm-hip-straw-2", "torso", off(a, n, 0.004, -0.016, 0.006), off(a, n, 0.008, -0.001, 0.009), 0.004, 0.004, n,
        "straw")

    # --- HER PURPLE WRAPS: turns wound round each forearm (each turn its own lean), one round the left upper arm, two at each
    # ankle, a loose end on the left forearm.
    for name, bone, centre, size, rot in (
            ("wrap-l1", "arm-left", (0.364, 0.401, 0.000), (0.024, 0.140, 0.154), (0, 4, -8)),
            ("wrap-l2", "arm-left", (0.388, 0.402, 0.001), (0.026, 0.141, 0.155), (0, -3, 6)),
            ("wrap-l3", "arm-left", (0.414, 0.400, -0.001), (0.024, 0.140, 0.154), (0, 5, -5)),
            ("wrap-l4", "arm-left", (0.300, 0.401, 0.000), (0.022, 0.124, 0.140), (0, -6, 10)),
            ("wrap-r1", "arm-right", (-0.365, 0.401, 0.002), (0.026, 0.142, 0.152), (0, -5, 7)),
            ("wrap-r2", "arm-right", (-0.392, 0.401, 0.002), (0.024, 0.142, 0.153), (0, 3, -9)),
            ("wrap-r3", "arm-right", (-0.432, 0.402, 0.003), (0.022, 0.141, 0.152), (0, -2, 4)),
            ("wrap-ankle-l1", "leg-left", (0.101, 0.064, -0.001), (0.154, 0.022, 0.174), (4, 0, -3)),
            ("wrap-ankle-r1", "leg-right", (-0.101, 0.068, 0.001), (0.152, 0.024, 0.172), (-4, 0, 4)),
            ):
        obox(name, bone, centre, size, _rot(*rot), "wrap")
    obox("wrap-l-end", "arm-left", (0.434, 0.332, -0.052), (0.030, 0.012, 0.022), _rot(0, 20, -25), "wrap")
    obox("wrap-ankle-l-knot", "leg-left", (0.180, 0.074, 0.012), (0.014, 0.024, 0.022), _rot(0, 0, 14), "wrap")
    bar("wrap-ankle-l-end-1", "leg-left", (0.184, 0.068, 0.010), (0.196, 0.040, 0.022), 0.012, 0.005, (1.0, 0.0, 0.0), "wrap")
    bar("wrap-ankle-l-end-2", "leg-left", (0.184, 0.068, 0.016), (0.190, 0.044, -0.004), 0.011, 0.005, (1.0, 0.0, 0.0), "wrap")
    obox("wrap-ankle-r-knot", "leg-right", (-0.182, 0.078, -0.008), (0.014, 0.022, 0.020), _rot(0, 0, -12), "wrap")
    bar("wrap-ankle-r-end-1", "leg-right", (-0.186, 0.072, -0.010), (-0.198, 0.046, -0.024), 0.012, 0.005, (-1.0, 0.0, 0.0),
        "wrap")

    # --- WHIP STITCHES where each arm was sewn on and round each mitten's end. (Mittens: NO fingers, owner, twice.)
    for name, face, a, b, angle, only in (
            ("whip-sh-l-0", "front", 0.232, 0.440, 70, ["upper-l"]), ("whip-sh-l-1", "front", 0.233, 0.408, 66, ["upper-l"]),
            ("whip-sh-l-2", "front", 0.231, 0.376, 72, ["upper-l"]), ("whip-sh-l-3", "top", 0.232, -0.040, 68, ["upper-l"]),
            ("whip-sh-l-4", "top", 0.233, 0.010, 64, ["upper-l"]), ("whip-sh-l-5", "top", 0.231, 0.050, 70, ["upper-l"]),
            ("whip-sh-r-0", "front", -0.232, 0.438, -68, ["upper-r"]),
             ("whip-sh-r-1", "front", -0.233, 0.406, -72, ["upper-r"]),
            ("whip-sh-r-2", "front", -0.231, 0.374, -66, ["upper-r"]), ("whip-sh-r-3", "top", -0.232, -0.036, -70, ["upper-r"]),
            ("whip-sh-r-4", "top", -0.233, 0.012, -64, ["upper-r"]), ("whip-sh-r-5", "top", -0.231, 0.052, -68, ["upper-r"]),
            ("whip-mit-l-0", "front", 0.534, 0.440, 70, ["mitten-l"]),
             ("whip-mit-l-1", "front", 0.533, 0.410, 66, ["mitten-l"]),
            ("whip-mit-l-2", "front", 0.535, 0.380, 72, ["mitten-l"]),
             ("whip-mit-l-3", "front", 0.533, 0.352, 68, ["mitten-l"]),
            ("whip-mit-l-4", "top", 0.534, -0.040, 64, ["mitten-l"]), ("whip-mit-l-5", "top", 0.533, 0.004, 70, ["mitten-l"]),
            ("whip-mit-l-6", "top", 0.535, 0.044, 66, ["mitten-l"]),
            ("whip-mit-r-0", "front", -0.537, 0.442, -68, ["mitten-r"]),
             ("whip-mit-r-1", "front", -0.536, 0.412, -72, ["mitten-r"]),
            ("whip-mit-r-2", "front", -0.538, 0.380, -66, ["mitten-r"]),
             ("whip-mit-r-3", "front", -0.536, 0.350, -70, ["mitten-r"]),
            ("whip-mit-r-4", "top", -0.537, -0.036, -64, ["mitten-r"]),
             ("whip-mit-r-5", "top", -0.536, 0.008, -70, ["mitten-r"]),
            ("whip-mit-r-6", "top", -0.538, 0.048, -66, ["mitten-r"])):
        bone = "arm-left" if name.split("-")[2] == "l" else "arm-right"
        whip(name, bone, face, a, b, angle, only)

    # --- PINS with her gems: through its head (three), its right shoulder, its back, its right hip, its left arm.
    pin("pin-head-r", "head", "right", -0.030, 0.672, (-0.72, 0.62, -0.30), 0.110, GEM_LILAC, 0.034, ["head"], 10)
    pin("pin-head-l", "head", "left", 0.020, 0.660, (0.66, 0.70, 0.26), 0.098, GEM_GOLD, 0.030, ["head"], 25)
    pin("pin-head-back", "head", "back", 0.112, 0.640, (0.35, 0.50, 0.79), 0.100, GEM_CRIMSON, 0.030, ["head-back"], 40)
    pin("pin-shoulder-r", "torso", "right", -0.060, 0.430, (-0.92, 0.28, -0.26), 0.080, GEM_MAGENTA, 0.028, ["mantle"], 5)
    pin("pin-back", "torso", "back", -0.110, 0.320, (-0.30, 0.55, 0.78), 0.096, GEM_MAGENTA, 0.028, ["back"], 50)
    pin("pin-hip", "torso", "front", -0.150, 0.215, (-0.62, -0.30, -0.72), 0.086, GEM_GOLD, 0.028, ["belly", "belly-low"], 15)
    pin("pin-arm-l", "arm-left", "top", 0.290, 0.030, (0.20, 0.90, -0.38), 0.080, GEM_LILAC, 0.026, ["upper-l"], 35)


# Where its light's motes rise from (the runtime reads these as empty nodes under the bones): (name, bone, authored point).
SOUL_ANCHORS = [
    ("soul-crown", "head", (0.0, 0.742, -0.006)),
    ("soul-chest", "torso", (-0.014, 0.330, -0.256)),
]

# =====================================================================================================================
# THE CHECK AND THE GLB
# =====================================================================================================================
def retarget(gltf):
    """The rig's bones moved onto the doll's own skeleton; returns each moved node's translation delta (for the clips)."""
    by_name = {node.get("name"): i for i, node in enumerate(gltf["nodes"])}
    deltas = {}
    for bone, world in SKELETON.items():
        index = by_name[bone]
        parent = SKELETON[PARENT[bone]] if bone in PARENT else (0.0, 0.0, 0.0)
        local = tuple(world[a] - parent[a] for a in range(3))
        old = tuple(gltf["nodes"][index].get("translation", [0.0, 0.0, 0.0]))
        gltf["nodes"][index]["translation"] = list(local)
        deltas[index] = tuple(local[a] - old[a] for a in range(3))
    return deltas


def bind_matrices(gltf):
    for skin in gltf["skins"]:
        rows = []
        for joint in skin["joints"]:
            world = SKELETON[gltf["nodes"][joint].get("name")]
            rows.append((1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, -world[0], -world[1], -world[2], 1.0))
        skin["_rows"] = rows


def verify():
    every = [p for key in MESH for p in MESH[key].pos]
    cloth = MESH["body"].pos + MESH["head"].pos
    lo = [min(v[a] for v in every) for a in range(3)]
    hi = [max(v[a] for v in every) for a in range(3)]
    height = max(v[1] for v in cloth) - min(v[1] for v in cloth)     # the doll, not the light hanging over its crown
    print(f"parts={len(PLACED)}  tris: body={len(MESH['body'].idx) // 3} head={len(MESH['head'].idx) // 3} "
          f"glow={len(MESH['glow'].idx) // 3} spill={len(MESH['spill'].idx) // 3}")
    print(f"bounds min={[round(v, 4) for v in lo]} max={[round(v, 4) for v in hi]}  height={height:.4f}")
    if not (MIN_HEIGHT <= height <= MAX_HEIGHT):
        raise SystemExit(f"HEIGHT {height:.4f} outside {MIN_HEIGHT}..{MAX_HEIGHT} (Paete's size); nothing written.")
    floor = min(v[1] for v in cloth)
    if abs(floor) > 0.001:
        raise SystemExit(f"feet are at y={floor:.4f}, not 0.")
    for name, bone, centre in PLACED:
        if "finger" in name or "thumb" in name:
            raise SystemExit(f"'{name}': nobody in this game has fingers (owner, twice).")
        if math.dist(centre, SKELETON[bone]) > 0.75:
            raise SystemExit(f"part '{name}' is far from the {bone} bone: almost certainly on the wrong bone.")
    r, g, b = (int(PALETTE[INK][i:i + 2], 16) / 255.0 for i in (0, 2, 4))
    if 0.2126 * r + 0.7152 * g + 0.0722 * b > V.MAX_FACE_LUMINANCE:
        raise SystemExit("slot 8 must stay ink.")


def main():
    for row in SACK:
        name, bone, lo, hi, cloth = row[:5]
        abox(name, bone, lo, hi, cloth, bottom=row[5] if len(row) > 5 else None, bevel=SACK_BEVEL.get(name))
    build_details()
    verify()

    if not os.path.exists(V.BASE):
        raise SystemExit(f"base rig not found: {V.BASE}")
    gltf, buffer = V.read_glb(V.BASE)
    deltas = retarget(gltf)
    bind_matrices(gltf)

    blob, new_views, new_accessors, remap = bytearray(), [], [], {}

    def align():
        while len(blob) % 4:
            blob.append(0)

    def keep(old_index):
        if old_index in remap:
            return remap[old_index]
        acc = dict(gltf["accessors"][old_index])
        data = V.accessor_bytes(gltf, buffer, old_index)
        align()
        acc["bufferView"] = len(new_views)
        acc.pop("byteOffset", None)
        new_views.append({"buffer": 0, "byteOffset": len(blob), "byteLength": len(data)})
        blob.extend(data)
        remap[old_index] = len(new_accessors)
        new_accessors.append(acc)
        return remap[old_index]

    def add(values, fmt, kind, component, minmax=False):
        align()
        start = len(blob)
        for v in values:
            blob.extend(struct.pack("<" + fmt * len(v), *v))
        acc = {"bufferView": len(new_views), "componentType": component, "count": len(values), "type": kind}
        if minmax:
            n = len(values[0])
            acc["min"] = [min(v[a] for v in values) for a in range(n)]
            acc["max"] = [max(v[a] for v in values) for a in range(n)]
        new_views.append({"buffer": 0, "byteOffset": start, "byteLength": len(blob) - start})
        new_accessors.append(acc)
        return len(new_accessors) - 1

    for skin in gltf["skins"]:
        skin["inverseBindMatrices"] = add(skin.pop("_rows"), "f", "MAT4", 5126)

    for anim in gltf["animations"]:
        for channel in anim["channels"]:
            sampler = anim["samplers"][channel["sampler"]]
            sampler["input"] = keep(sampler["input"])
            delta = deltas.get(channel["target"]["node"])
            if channel["target"]["path"] != "translation" or delta is None or delta == (0.0, 0.0, 0.0):
                sampler["output"] = keep(sampler["output"])
                continue
            values = V.read_accessor(gltf, buffer, sampler["output"])
            sampler["output"] = add([tuple(v[a] + delta[a] for a in range(3)) for v in values], "f", "VEC3", 5126)

    # ⚠️ THE LIGHT AND ITS SPILL ARE TWO MORE SKINNED MESHES ON THE BODY'S SKIN, so the game paints them unlit and additive
    # (`PhaisterDollArt.ApplyGlow`) while the toon paint keeps the cloth. Their joints index the same seven bones.
    for mesh_name in ("glow-mesh", "spill-mesh"):
        gltf["meshes"].append({"name": mesh_name, "primitives": []})
        gltf["nodes"].append({"name": mesh_name, "mesh": len(gltf["meshes"]) - 1, "skin": 0})
        gltf["nodes"][0].setdefault("children", []).append(len(gltf["nodes"]) - 1)

    # Empty nodes under the bones where its light's motes rise from.
    by_name = {node.get("name"): i for i, node in enumerate(gltf["nodes"])}
    for name, bone, point in SOUL_ANCHORS:
        at = (point[0], point[1], -point[2])
        local = [at[a] - SKELETON[bone][a] for a in range(3)]
        gltf["nodes"].append({"name": name, "translation": local})
        gltf["nodes"][by_name[bone]].setdefault("children", []).append(len(gltf["nodes"]) - 1)

    for mesh, key in ((gltf["meshes"][0], "body"), (gltf["meshes"][1], "head"), (gltf["meshes"][-2], "glow"),
                      (gltf["meshes"][-1], "spill")):
        built = MESH[key]
        normals = built.nrm if key in ("glow", "spill") else V.smooth_normals(built.pos, built.nrm, sorted(built.flat))
        attributes = {
            "POSITION": add(built.pos, "f", "VEC3", 5126, minmax=True),
            "NORMAL": add(normals, "f", "VEC3", 5126),
            "TEXCOORD_0": add(built.uv, "f", "VEC2", 5126),
            "JOINTS_0": add(built.joints, "H", "VEC4", 5123),
            "WEIGHTS_0": add(built.weights, "f", "VEC4", 5126),
        }
        if key in ("glow", "spill"):
            attributes["COLOR_0"] = add(built.col, "f", "VEC4", 5126)
        mesh["primitives"] = [{"attributes": attributes, "indices": add([(i,) for i in built.idx], "I", "SCALAR", 5125),
                               "material": 0, "mode": 4}]

    # ⚠️ THE CLOTH IS EMBEDDED, not the shared `Textures/colormap.png`: its top half is the painted cloth, its bottom half the
    # palette cells the toon shader remaps anyway (`paint_phaister_doll_cloth.py` says why the halves are where they are).
    image = CLOTH.build([PALETTE[s] for s in range(16)], CLOTH_STYLE)
    png = io.BytesIO()
    image.save(png, format="PNG", optimize=True)
    align()
    start = len(blob)
    blob.extend(png.getvalue())
    new_views.append({"buffer": 0, "byteOffset": start, "byteLength": len(blob) - start})
    gltf["images"] = [{"bufferView": len(new_views) - 1, "mimeType": "image/png", "name": "phaister-doll-cloth"}]
    # Hard-edged pixels up close (the cast's own atlas is nearest-sampled), mip-blended far off so the weave never shimmers.
    gltf["samplers"] = [{"magFilter": 9728, "minFilter": 9987, "wrapS": 33071, "wrapT": 33071}]
    gltf["textures"] = [{"sampler": 0, "source": 0, "name": "phaister-doll-cloth"}]
    gltf["materials"] = [{"name": "phaister-doll-cloth", "doubleSided": True,
                          "pbrMetallicRoughness": {"baseColorTexture": {"index": 0}, "metallicFactor": 0.0,
                                                   "roughnessFactor": 1.0}}]
    for key in ("extensionsUsed", "extensionsRequired"):
        if key in gltf:
            gltf[key] = [e for e in gltf[key] if e != "KHR_texture_transform"]
            if not gltf[key]:
                del gltf[key]

    gltf["accessors"] = new_accessors
    gltf["bufferViews"] = new_views
    gltf["buffers"] = [{"byteLength": len(blob)}]
    gltf["asset"] = {"version": "2.0", "generator": "Tumbang Preso voodoo doll builder"}
    stem = os.path.splitext(os.path.basename(OUT))[0]
    gltf["nodes"][0]["name"] = stem
    gltf["scenes"][0]["name"] = stem

    V.write_glb(OUT, gltf, bytes(blob))
    os.makedirs(os.path.dirname(PALETTE_OUT), exist_ok=True)
    with open(PALETTE_OUT, "w", encoding="utf-8", newline="\n") as handle:
        json.dump({"slots": [PALETTE[s] for s in range(16)]}, handle, indent=2)
        handle.write("\n")
    image.save(CLOTH_OUT, optimize=True)
    print(f"wrote {PALETTE_OUT} and {CLOTH_OUT}")


if __name__ == "__main__":
    sys.exit(main())
