"""Nemu's three ASTRA ghosts, remodelled in Kuro's cute family (phase 1: models only, nothing in the game reads these yet).

Owner, 2026-10-19: "start remodelling nemu's new ghosts. they should follow the same cutesy style like you did for kuro".

The ghosts the game draws today are ASTRA's hand-typed voxels (`Visual/BlindfoldGhostModel.cs`, `Visual/NemuGhostModels.cs`)
and they stay untouched. These are their replacements, built the way Kuro ("Kubo", tools/kubo_kuro.py) and Paete's plants
(tools/build_paete_bloom.py) were: `tools/hand_companion_kit.py`, round chunky forms, smooth normals, a painted atlas
beside each .glb, drawn faces. What makes them Kuro's family: a ROUNDED BLOCK for the head or body (never a sphere and
never a crate), big glossy eyes with two white glints, peach cheeks, little mitten paws, and a ghost's tail of beads that
pales to white toward its tip.

  DON'T LOOK   `nemu_ghost_blindfold`  a ghost CHILD, the tallest (0.85 m, it has to cover a face): a big rounded head
               under a plum bob, a violet cloth tied over its eyes and knotted behind with two tails, a shy smile with one
               fang, a small bell of a body and a bead tail, little arms out in front like a sleepwalker. SPRUNG (the
               `sprung-*` parts): the cloth drops to its neck, big hollow eyes with a lavender pinpoint, a gasp with two
               fangs, arms up round the victim's head.
  SKITTER      `nemu_ghost_skitter`    the greedy fetcher: a low WIDE mochi block (wider than tall), swept-back ear wisps,
               huge eager eyes, a toothy grin with its tongue out, a tail streaming out BEHIND it (it runs), and long noodle
               arms ending in three-clawed mitts turned in toward each other to close round a slipper's two ends.
  LOOKOUT      `nemu_ghost_lookout`    the night watchman: a TALL sheet over a rounded block, a scalloped hem, a gathered
               curl on top, steady half-lidded watchful eyes, stubby sheet arms; its right hand holds a little lantern
               (`nemu_ghost_lantern`, its own model) whose warm light glints in its eyes.

THE CONTRACT WITH ASTRA'S CODE (so phase 2 can swap the meshes without moving a number): every attach point below is
ASTRA's own constant, in metres, y up, +z the ghost's face, origin centred on x and z and standing on y 0.
  Blindfold  HeadCentre (0, .625, 0), HeadHalfDepth .20 (the face plane is z .20), Eyes (+-.075, .625, .205).
  Skitter    shoulders (+-.20, .425, .05), arm reaches along +z, hand at z .325 along it; SkitterHands (0, .225, .30);
             SkitterEyePoints (+-.075, .475, .18).
  Lookout    shoulders (+-.225, .35, .025), hand at z .225 along the arm; LookoutEyePoints (+-.075, .475, .205).
  Lantern    pivot (the top of its handle) at the origin, hanging down; LookoutGlass (0, -.20, 0).
Arms are their own nodes with the shoulder at the node origin (`arm-l` is index 0, -x; `arm-r` index 1, +x), as ASTRA's
are, so the code can still aim and stretch them.

  py -3 tools/build_nemu_ghosts.py [--colours=nemu|astra] [--out=ArtSource/nemu-ghosts]
  blender -b --python tools/review_nemu_ghosts.py -- --version=v1 [--colours=nemu|astra]

Writes <out>/<name>.glb and <name>-atlas.png for each, and the palettes to Logs/hand-companions/<name>.palette.json
(the review reads them from there). Default out is ArtSource/nemu-ghosts, OUTSIDE Assets, so the open editor never
imports them and nothing in the game changes.

`--colours=nemu` (default) dresses all three in Nemu's own violets (person_team-nemu.tres), each in a different part of
it so they stay distinct: Blindfold moon-pale, Skitter lavender, Lookout Kuro's deep plum with a warm lantern.
`--colours=astra` dresses them in the colours ASTRA's owner approved for each job (icy pale blue, ectoplasm green, lantern
amber), for comparison. The shapes are the same.
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import Model, ellipsoid, lathe, moved, rounded, tube  # noqa: E402


def arg(name, default):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


COLOURS = arg("colours", "nemu")
OUT = Path(arg("out", kit.ROOT / "ArtSource/nemu-ghosts"))
if COLOURS != "nemu":
    OUT = OUT / COLOURS
SUFFIX = "" if COLOURS == "nemu" else "_" + COLOURS

# ------------------------------------------------------------------ colour
# Sixteen flat cells per ghost (the lower half of its atlas); the paint (upper half) is drawn from the same scheme.
INK, WHITE, BLUSH, TONGUE, FANG, GLOW, MOUTH, DEEP = range(8)
SCHEMES = {
    "nemu": {
        "blindfold": dict(body=("#C4B4EC", "#EEE8FF", "#FBF8FF"), hair=("#1E1830", "#3A2D5C"), cloth=("#6E2FB4", "#9A52DE", "#D3A6FA"),
                          tail=("#D9CCF6", "#FFFFFF"), glow="#D09AF8"),
        "skitter": dict(body=("#7440BE", "#A866EC", "#D2A8FF"), tail=("#B57AF0", "#FFFFFF"), glow="#F4FAFF", ear="#E6CCFF"),
        "lookout": dict(body=("#2A2046", "#45367A", "#6A58A0"), tail=("#4A3A78", "#C9B6F2"), glow="#FFE39A", frame="#4A3020",
                        glass=("#FFB347", "#FFE7A3", "#FFFBEA")),
    },
    "astra": {
        "blindfold": dict(body=("#9CC0E4", "#DCEEFF", "#F4FAFF"), hair=("#16141E", "#34303F"), cloth=("#BDBACB", "#F0EEF2", "#FFFFFF"),
                          tail=("#C4DEF5", "#FFFFFF"), glow="#7FF3FF"),
        "skitter": dict(body=("#3FA453", "#6FD86A", "#B6F7A4"), tail=("#6FD86A", "#E8FFE0"), glow="#FFE35A", ear="#C9FBB8"),
        "lookout": dict(body=("#D88A3E", "#F5B567", "#FFD9A3"), tail=("#E79A4A", "#FFF0D8"), glow="#FFE89A", frame="#3A2A1A",
                        glass=("#FFB347", "#FFE7A3", "#FFFBEA")),
    },
}[COLOURS]


def palette(glow):
    # INK is Nemu's own ink (person_team-nemu.tres slot 8), BLUSH Kuro's peach, the rest one each.
    return ["#120E1C", "#FFFFFF", "#F29C7C", "#FF7FA6", "#FBF6EE", glow, "#2A1440", "#0B0A0F"] + ["#FF00FF"] * 8


def hexc(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32)


# ------------------------------------------------------------------ paint

ATLAS = 1024
PAD = 0.004


def inset(rect):
    return (rect[0] + PAD, rect[1] + PAD, rect[2] - PAD, rect[3] - PAD)


def smooth01(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


class Atlas:
    """The upper half of a 1024 atlas, cut into named rects (u0, v0, u1, v1 with v DOWN, all inside v 0..0.5)."""

    def __init__(self, base):
        self.img = np.zeros((ATLAS, ATLAS, 3), np.float32) + hexc(base)

    def grid(self, rect):
        x0, y0, x1, y1 = (int(rect[0] * ATLAS), int(rect[1] * ATLAS), int(rect[2] * ATLAS), int(rect[3] * ATLAS))
        u = (np.arange(x0, x1) + .5 - x0) / (x1 - x0)
        t = 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0)
        return np.meshgrid(u, t), (x0, y0, x1, y1)

    def paint(self, rect, fn):
        (u, t), (x0, y0, x1, y1) = self.grid(rect)
        self.img[y0:y1, x0:x1] = fn(u, t)

    def save(self, path):
        from PIL import Image, ImageFilter
        Image.fromarray(np.clip(self.img, 0, 255).astype(np.uint8), "RGB").filter(ImageFilter.GaussianBlur(0.8)).save(path)


def flat(u, colour):
    return np.zeros(u.shape + (3,), np.float32) + hexc(colour)


def mix(base, colour, weight):
    return base * (1 - weight[..., None]) + hexc(colour) * weight[..., None]


def front_of(u):
    """1 at the piece's front (u = .25, where the kit's rounds face +z), -1 at its back."""
    return np.cos((u - 0.25) * 2 * np.pi)


def body_paint(deep, mid, light, belly=0.5, cap=0.35):
    """Kuro's body light, painted: deep at the hem, mid up the flanks, light on the crown, a pale belly at the front."""
    def fn(u, t):
        c = flat(u, deep)
        c = mix(c, mid, smooth01(0.0, 0.45, t))
        c = mix(c, light, smooth01(0.70, 1.0, t) * cap)
        c = mix(c, light, np.exp(-(((u - 0.25) / 0.11) ** 2 + ((t - 0.42) / 0.26) ** 2)) * belly)
        c = mix(c, deep, np.clip(-front_of(u), 0, 1) * 0.22 * (1 - smooth01(0.7, 1.0, t)))
        return c
    return fn


def gradient(lo, hi, a=0.0, b=1.0):
    return lambda u, t: mix(flat(u, lo), hi, smooth01(a, b, t))


def piece(mesh, rect, cols, along="axis", axis=1):
    """A painted piece: UVs laid into `rect` of the atlas as BUILT (before it is turned)."""
    return (mesh, kit.grid_uv(mesh, inset(rect), cols, along, axis))


def turned(p, **how):
    return (moved(p[0], **how), p[1])


# ------------------------------------------------------------------ shared face and limb pieces

def on_box(half, radius, centre, x, y):
    """The point on a `rounded` block's FRONT at (x, y), and the (pitch, yaw) that turns +z to its normal there."""
    half = np.array(half, float)
    core = np.maximum(half - radius, 0)
    dx = max(abs(x - centre[0]) - core[0], 0) * np.sign(x - centre[0])
    dy = max(abs(y - centre[1]) - core[1], 0) * np.sign(y - centre[1])
    dz = math.sqrt(max(radius * radius - dx * dx - dy * dy, 1e-9))
    p = (x, y, centre[2] + core[2] + dz)
    n = np.array([dx, dy, dz]) / radius
    return p, (-math.degrees(math.asin(n[1])), math.degrees(math.atan2(n[0], n[2])), 0)


def glossy_eye(at, turn, rx, ry, iris, look=(0.0, 0.0)):
    """Kuro's eye: a deep oval, a lighter crescent low in it, a big glint high on the left and a small one low right."""
    lx, ly = look
    parts = [(moved(ellipsoid((rx, ry, 0.012), seg=20, rings=12), turn=turn, at=at), INK)]
    parts.append((moved(ellipsoid((rx * 0.62, ry * 0.30, 0.008), at=(lx * rx, -ry * 0.50 + ly * ry, 0.007)), turn=turn, at=at), iris))
    parts.append((moved(ellipsoid((rx * 0.36, ry * 0.30, 0.006), at=(-rx * 0.30 + lx * rx, ry * 0.36 + ly * ry, 0.010)), turn=turn, at=at), WHITE))
    parts.append((moved(ellipsoid((rx * 0.15, ry * 0.13, 0.005), at=(rx * 0.36 + lx * rx, -ry * 0.10 + ly * ry, 0.010)), turn=turn, at=at), WHITE))
    return parts


def cheek(at, turn, r=0.032):
    return (moved(ellipsoid((r, r * 0.62, 0.006)), turn=turn, at=at), BLUSH)


def stroke(points, radius):
    """An ink line drawn on the face: a thin round tube along `points`."""
    return (tube(points, radius, seg=8), INK)


def fang(at, length=0.026, width=0.011, turn=(0, 0, 0)):
    tooth = lathe([(0.0, -length), (width * 0.55, -length * 0.45), (width, 0.0)], seg=10, squash_z=0.55)
    return (moved(tooth, turn=turn, at=at), FANG)


def beads(spec, rect):
    """A ghost's tail: beads (centre, half) shrinking toward the tip, each painted a slice further toward white."""
    out = []
    n = len(spec)
    u0, v0, u1, v1 = rect
    for i, (at, half) in enumerate(spec):
        # Bead i takes slice i of the rect, from the bottom of the rect (deep) to its top (white).
        top = v1 - (v1 - v0) * (i + 1) / n
        bottom = v1 - (v1 - v0) * i / n
        sub = (u0, top, u1, bottom)
        b = ellipsoid(half, seg=16, rings=10)
        out.append((moved(b, at=at), kit.grid_uv(b, inset(sub), 16, "axis", 1)))
    return out


def mitt(at, r, rect, claws=0, inward=1, claw_slot=FANG):
    """A little round paw; `claws` small cones on its front, curling in toward the other hand."""
    ball = ellipsoid((r, r * 0.88, r * 1.05), seg=14, rings=10)
    out = [(moved(ball, at=at), kit.grid_uv(ball, inset(rect), 14, "axis", 1))]
    for k in range(claws):
        y = (k - (claws - 1) / 2) * r * 0.62
        cone = lathe([(r * 0.30, 0.0), (r * 0.22, r * 0.30), (0.0, r * 0.62)], seg=10)
        cone = moved(cone, turn=(90, 0, 0))                              # point along +z
        cone = moved(cone, turn=(0, inward * 34, 0))                     # curl in toward the other hand
        out.append((moved(cone, at=(at[0] + inward * r * 0.28, at[1] + y, at[2] + r * 0.80)), claw_slot))
    return out


# ------------------------------------------------------------------ DON'T LOOK

def blindfold():
    s = SCHEMES["blindfold"]
    name = "nemu_ghost_blindfold" + SUFFIX
    m = Model(name)
    m.folder = OUT
    m.texture = name + "-atlas.png"
    R_HEAD, R_BODY, R_HAIR, R_CLOTH = (0, 0, .5, .25), (.5, 0, .75, .25), (.75, 0, 1, .25), (0, .25, .25, .5)
    R_TAIL, R_ARM, R_TIE = (.25, .25, .375, .5), (.375, .25, .5, .5), (.5, .25, .75, .5)
    a = Atlas(s["body"][1])
    a.paint(R_HEAD, body_paint(s["body"][0], s["body"][1], s["body"][2], belly=0.35, cap=0.5))
    a.paint(R_BODY, body_paint(s["body"][0], s["body"][1], s["body"][2], belly=0.55))
    hair_lo, hair_hi = s["hair"]
    # The bob: dark, with one soft shine band across its crown (the cute "anime" highlight), none at the back.
    a.paint(R_HAIR, lambda u, t: mix(flat(u, hair_lo), hair_hi, np.exp(-((t - 0.80) / 0.06) ** 2) * np.clip(front_of(u) + 0.3, 0, 1) * 0.9))
    c_lo, c_mid, c_hi = s["cloth"]
    # The cloth: a band, darker at its lower edge, with a pale hem line along each edge (rings run bottom to top).
    a.paint(R_CLOTH, lambda u, t: mix(mix(mix(flat(u, c_mid), c_lo, 1 - smooth01(0.0, 0.5, t)),
                                          c_hi, np.exp(-((t - 0.16) / 0.035) ** 2) * 0.8), c_hi, np.exp(-((t - 0.84) / 0.035) ** 2) * 0.8))
    a.paint(R_TIE, lambda u, t: mix(flat(u, c_mid), c_lo, smooth01(0.3, 1.0, 1 - t) * 0.6))
    a.paint(R_TAIL, gradient(s["tail"][0], s["tail"][1], 0.0, 1.0))
    a.paint(R_ARM, gradient(s["body"][0], s["body"][2], 0.0, 1.0))
    OUT.mkdir(parents=True, exist_ok=True)
    a.save(OUT / m.texture)

    HC = (0.0, 0.625, 0.0)                     # ASTRA's HeadCentre
    HH, HR = (0.215, 0.19, 0.20), 0.095        # the head: front plane at z .20 = HeadHalfDepth
    head = rounded(HH, HR, seg=24, rings=16)
    m.add("head", [piece(head, R_HEAD, 24)], at=HC)

    # The plum bob: a cap over the crown and down the back, a fringe of three chunky locks of different sizes.
    # v2: v1's cap stood on the head like a hat with a scalloped brim and left a white forehead above the cloth.
    cap = rounded((0.236, 0.082, 0.222), 0.082, seg=22, rings=14)
    back = rounded((0.230, 0.150, 0.090), 0.088, seg=20, rings=12)
    hair = [turned(piece(cap, R_HAIR, 22), at=(0.0, 0.118, -0.004)), turned(piece(back, R_HAIR, 20), at=(0.0, 0.040, -0.140))]
    # v3: v2's round fringe lumps read as a scalloped hat brim and its side blobs as headphones. Hair CLUMPS instead:
    # fat drops pointing down over the top of the cloth, each its own length and lean, and two long side locks to the cheeks.
    # v4: v3's drops sat inside the cap and showed only their tips (drips). Further forward and fatter, so they break the
    # cap's straight lower edge and lie over the top of the cloth.
    for x, length, lean, fat in ((-0.168, 0.110, 16, 0.060), (-0.080, 0.132, 7, 0.066), (0.012, 0.118, -3, 0.062),
                                 (0.100, 0.136, -9, 0.066), (0.180, 0.104, -18, 0.058)):
        drop = lathe([(0.0, -length), (fat * 0.55, -length * 0.62), (fat, -length * 0.22), (fat * 0.92, 0.0), (0.0, 0.020)], seg=14, squash_z=0.62)
        hair.append((moved(drop, turn=(0, 0, lean), at=(x, 0.140, 0.196)), kit.grid_uv(drop, inset(R_HAIR), 14, "axis", 1)))
    for side in (-1, 1):
        lock = tube([(side * 0.236, 0.120, 0.070), (side * 0.250, 0.040, 0.085), (side * 0.252, -0.040, 0.095), (side * 0.238, -0.090, 0.110)],
                    [0.050, 0.046, 0.036, 0.016], seg=12)
        hair.append((lock, kit.grid_uv(lock, inset(R_HAIR), 12, "rows")))
    # A sprig sticking up off the crown (a child's cowlick), which also breaks the box's flat top.
    sprig = tube([(0.020, 0.180, 0.020), (0.026, 0.228, 0.012), (0.058, 0.252, -0.004), (0.084, 0.236, -0.012)], [0.038, 0.032, 0.022, 0.012], seg=10)
    hair.append((sprig, kit.grid_uv(sprig, inset(R_HAIR), 10, "rows")))
    m.add("hair", hair, parent="head")

    # Waiting: the cloth over its eyes, knotted at the back with two tails, a shy smile with one fang.
    band = rounded((0.232, 0.064, 0.217), 0.064, seg=24, rings=10)
    knot = ellipsoid((0.050, 0.042, 0.036), seg=14, rings=10)
    tie_l = tube([(0, 0, 0), (-0.030, -0.050, -0.040), (-0.050, -0.120, -0.060), (-0.040, -0.180, -0.090)], [0.030, 0.028, 0.024, 0.016], seg=10)
    tie_r = tube([(0, 0, 0), (0.035, -0.040, -0.050), (0.070, -0.090, -0.080), (0.095, -0.130, -0.120)], [0.028, 0.026, 0.022, 0.014], seg=10)
    cloth = [turned(piece(band, R_CLOTH, 24), at=(0, -0.002, 0)),
             (moved(knot, at=(0, 0.004, -0.226)), kit.grid_uv(knot, inset(R_TIE), 14, "axis", 1))]
    for t_ in (tie_l, tie_r):
        flat_tie = moved(t_, scale=(1, 1, 0.45))
        cloth.append((moved(flat_tie, at=(0, 0.0, -0.236)), kit.grid_uv(t_, inset(R_TIE), 10, "rows")))
    m.add("waiting-cloth", cloth, parent="head")
    face = []
    z = HH[2]
    smile = [(-0.040, -0.106, z + 0.004), (-0.020, -0.118, z + 0.006), (0.0, -0.122, z + 0.006), (0.020, -0.118, z + 0.006), (0.040, -0.106, z + 0.004)]
    face.append(stroke(smile, 0.0075))
    face.append(fang((0.020, -0.118, z + 0.006), length=0.022, width=0.010))
    face.append(cheek((-0.128, -0.090, z + 0.004), (0, 0, 0)))
    face.append(cheek((0.128, -0.090, z + 0.004), (0, 0, 0)))
    m.add("waiting-face", face, parent="head")

    # Sprung: the cloth round its neck, big hollow eyes with a lavender pinpoint, a gasp with two fangs.
    sprung_face = []
    for side in (-1, 1):
        ex = side * 0.075
        sprung_face.append((moved(ellipsoid((0.052, 0.066, 0.012), seg=20, rings=12), at=(ex, 0.0, z)), DEEP))
        sprung_face.append((moved(ellipsoid((0.016, 0.016, 0.006)), at=(ex + side * 0.006, 0.010, z + 0.010)), GLOW))
        sprung_face.append(cheek((side * 0.140, -0.070, z + 0.004), (0, 0, 0), r=0.028))
    sprung_face.append((moved(ellipsoid((0.040, 0.028, 0.010), seg=16, rings=10), at=(0, -0.098, z)), DEEP))
    sprung_face.append(fang((-0.022, -0.074, z + 0.008), length=0.020, width=0.009))
    sprung_face.append(fang((0.022, -0.074, z + 0.008), length=0.020, width=0.009))
    m.add("sprung-face", sprung_face, parent="head")
    # (Sprung's cloth hangs loose round its neck; it belongs to the body, not the head.)
    neckband = rounded((0.175, 0.040, 0.165), 0.040, seg=22, rings=10)
    m.add("sprung-cloth", [turned(piece(neckband, R_CLOTH, 22), turn=(-6, 0, 0), at=(0, 0.425, 0.006))])

    # The body: a small bell under the head, a bead tail that pales to white, no legs.
    bell = lathe([(0.090, 0.235), (0.142, 0.262), (0.168, 0.320), (0.165, 0.385), (0.140, 0.435), (0.090, 0.458)], seg=24)
    m.add("body", [piece(bell, R_BODY, 24)] + beads([((0.030, 0.205, -0.030), (0.088, 0.058, 0.084)), ((0.085, 0.140, -0.075), (0.060, 0.046, 0.058)),
                                                    ((0.112, 0.085, -0.130), (0.040, 0.034, 0.040)), ((0.098, 0.050, -0.172), (0.024, 0.022, 0.024))], R_TAIL))

    # Arms: a little nub reaching forward (waiting, like a sleepwalker) or up round a head (sprung). Shoulder at origin.
    for side, nm in ((-1, "l"), (1, "r")):
        reach = tube([(0, 0, 0), (side * 0.020, 0.010, 0.060), (side * 0.030, 0.020, 0.110)], [0.036, 0.032, 0.030], seg=12)
        m.add("waiting-arm-" + nm, [piece(reach, R_ARM, 12, "rows")] + mitt((side * 0.030, 0.020, 0.135), 0.040, R_ARM),
              at=(side * 0.140, 0.395, 0.020))
        up = tube([(0, 0, 0), (side * 0.080, 0.070, 0.040), (side * 0.140, 0.180, 0.130), (side * 0.120, 0.230, 0.250)], [0.036, 0.032, 0.030, 0.028], seg=12)
        m.add("sprung-arm-" + nm, [piece(up, R_ARM, 12, "rows")] + mitt((side * 0.108, 0.234, 0.278), 0.040, R_ARM, claws=3, inward=-side),
              at=(side * 0.140, 0.395, 0.020))
    m.write(palette(s["glow"]))
    return m


# ------------------------------------------------------------------ SKITTER

def skitter():
    s = SCHEMES["skitter"]
    name = "nemu_ghost_skitter" + SUFFIX
    m = Model(name)
    m.folder = OUT
    m.texture = name + "-atlas.png"
    R_BODY, R_TAIL, R_ARM, R_EAR = (0, 0, .5, .25), (.5, 0, .625, .25), (.625, 0, .75, .25), (.75, 0, .875, .25)
    a = Atlas(s["body"][1])
    a.paint(R_BODY, body_paint(s["body"][0], s["body"][1], s["body"][2], belly=0.6, cap=0.45))
    a.paint(R_TAIL, gradient(s["tail"][0], s["tail"][1]))
    a.paint(R_ARM, gradient(s["body"][0], s["body"][1], 0.0, 0.8))
    a.paint(R_EAR, gradient(s["body"][1], s["ear"], 0.3, 1.0))
    OUT.mkdir(parents=True, exist_ok=True)
    a.save(OUT / m.texture)

    BC, BH, BR = (0.0, 0.425, 0.0), (0.215, 0.170, 0.180), 0.125      # the mochi: wider than tall
    blob = rounded(BH, BR, seg=24, rings=16)
    m.add("body", [turned(piece(blob, R_BODY, 24), at=BC)])

    # Ear wisps, swept back as if it is always running.
    ears = []
    for side in (-1, 1):
        wisp = lathe([(0.045, 0.0), (0.040, 0.040), (0.026, 0.080), (0.0, 0.118)], seg=14)
        uv = kit.grid_uv(wisp, inset(R_EAR), 14, "axis", 1)
        ears.append((moved(wisp, turn=(-38, 0, side * -24), at=(side * 0.105, BC[1] + BH[1] - 0.030, -0.020)), uv))
    m.add("ears", ears, parent="body")

    face = []
    for side in (-1, 1):
        at, turn = on_box(BH, BR, (0, 0, 0), side * 0.075, 0.050)
        face += glossy_eye((at[0], BC[1] + at[1], at[2]), turn, 0.048, 0.060, GLOW, look=(0.0, 0.08))
        at, turn = on_box(BH, BR, (0, 0, 0), side * 0.150, -0.020)
        face.append(cheek((at[0], BC[1] + at[1], at[2] + 0.002), turn, r=0.030))
    # The greedy grin: a wide open D, a tongue out at one corner, a fang each side.
    z = BH[2]
    grin = ellipsoid((0.062, 0.030, 0.010), seg=20, rings=12)
    pos = grin[0].copy()
    pos[:, 1] = np.where(pos[:, 1] > 0, pos[:, 1] * 0.25, pos[:, 1])       # flat top, round bottom: a D on its back
    face.append(((pos + np.array([0, BC[1] - 0.055, z], np.float32), grin[1], grin[2]), MOUTH))
    face.append((moved(ellipsoid((0.022, 0.016, 0.010)), turn=(18, 0, 0), at=(0.024, BC[1] - 0.080, z + 0.004)), TONGUE))
    face.append(fang((-0.040, BC[1] - 0.050, z + 0.006), length=0.020, width=0.010))
    face.append(fang((0.040, BC[1] - 0.050, z + 0.006), length=0.020, width=0.010))
    m.add("face", face, parent="body")

    # The tail streams out BEHIND, low: it runs.
    m.add("tail", beads([((0.0, 0.330, -0.190), (0.090, 0.070, 0.085)), ((0.022, 0.300, -0.300), (0.066, 0.054, 0.066)),
                         ((0.000, 0.275, -0.390), (0.046, 0.040, 0.046)), ((-0.020, 0.262, -0.458), (0.028, 0.026, 0.028))], R_TAIL), parent="body")

    # Long noodle arms (shoulder at the node origin, reaching along +z), three-clawed mitts turned in toward each other.
    for side, nm in ((-1, "l"), (1, "r")):
        noodle = tube([(0, 0, 0), (0, 0.006, 0.110), (0, 0.000, 0.220), (0, -0.004, 0.290)], [0.034, 0.028, 0.026, 0.028], seg=12)
        m.add("arm-" + nm, [piece(noodle, R_ARM, 12, "rows")] + mitt((0.0, 0.0, 0.325), 0.044, R_ARM, claws=3, inward=-side),
              at=(side * 0.200, 0.425, 0.050), parent="body")
    m.write(palette(s["glow"]))
    return m


# ------------------------------------------------------------------ LOOKOUT

def lookout():
    s = SCHEMES["lookout"]
    name = "nemu_ghost_lookout" + SUFFIX
    m = Model(name)
    m.folder = OUT
    m.texture = name + "-atlas.png"
    R_BODY, R_HEM, R_ARM, R_TIP = (0, 0, .5, .25), (.5, 0, .625, .25), (.625, 0, .75, .25), (.75, 0, .875, .25)
    a = Atlas(s["body"][1])
    a.paint(R_BODY, body_paint(s["body"][0], s["body"][1], s["body"][2], belly=0.35, cap=0.6))
    a.paint(R_HEM, gradient(s["tail"][0], s["body"][1], 0.0, 1.0))
    a.paint(R_ARM, gradient(s["body"][0], s["body"][2], 0.0, 1.0))
    a.paint(R_TIP, gradient(s["body"][1], s["body"][2], 0.0, 1.0))
    OUT.mkdir(parents=True, exist_ok=True)
    a.save(OUT / m.texture)

    # A tall sheet over a rounded block: the block's front plane is at z .20 (its eyes sit at ASTRA's .205).
    BC, BH, BR = (0.0, 0.470, 0.0), (0.205, 0.255, 0.200), 0.120
    sheet = rounded(BH, BR, seg=24, rings=18)
    parts = [turned(piece(sheet, R_BODY, 24), at=BC)]
    # The hem: soft scallops all round the bottom edge, some bigger than others so it does not read as a cog.
    hem_y = BC[1] - BH[1] + 0.020
    # v2: v1's eight big lumps hung below the sheet and read as an octopus's feet. Fourteen small flat ones on the
    # outline itself make a wavy hem, barely below the block.
    for k in range(14):
        rad = math.radians(k * 360 / 14 + 7)
        r = 0.046 + 0.008 * ((k * 5) % 3) / 2
        cx, cz = math.sin(rad), math.cos(rad)
        k_ = (abs(cx) ** 4 + abs(cz) ** 4) ** 0.25                      # on the rounded square's outline
        px, pz = cx / k_ * (BH[0] - 0.022), cz / k_ * (BH[2] - 0.022)
        lump = ellipsoid((r, r * 0.55, r), seg=14, rings=10)
        parts.append((moved(lump, at=(px, hem_y - 0.008 - 0.008 * (k % 2), pz)), kit.grid_uv(lump, inset(R_HEM), 14, "axis", 1)))
    # The sheet gathered into a curl on top, leaning back.
    # v2: v1's thin curl read as a horn. A fat gathered tuft that flops over to one side, like a nightcap's tip.
    curl = tube([(0.0, -0.010, 0.0), (0.0, 0.040, 0.0), (0.028, 0.078, -0.004), (0.070, 0.082, -0.008), (0.096, 0.058, -0.010)],
                [0.062, 0.052, 0.040, 0.030, 0.022], seg=12)
    parts.append(turned(piece(curl, R_TIP, 12, "rows"), at=(0.0, BC[1] + BH[1] - 0.012, -0.010)))
    m.add("body", parts)

    face = []
    z = BC[2] + BH[2]
    for side in (-1, 1):
        ex, ey = side * 0.075, 0.475
        # Steady watchful eyes: Kuro's glossy oval with its top hidden under a lid, the glint warm from the lantern.
        face += glossy_eye((ex, ey, z), (0, 0, 0), 0.042, 0.050, GLOW, look=(side * -0.10, -0.05))
        face.append((moved(ellipsoid((0.050, 0.020, 0.013), seg=16, rings=10), at=(ex, ey + 0.040, z + 0.001)), DEEP))
        face.append(stroke([(ex - side * 0.050, ey + 0.026, z + 0.010), (ex, ey + 0.030, z + 0.012), (ex + side * 0.052, ey + 0.034, z + 0.010)], 0.0065))
        face.append(cheek((side * 0.140, 0.420, z + 0.003), (0, 0, 0), r=0.028))
    # A small calm mouth, a fang peeking.
    face.append(stroke([(-0.022, 0.405, z + 0.004), (0.0, 0.398, z + 0.006), (0.022, 0.405, z + 0.004)], 0.0065))
    face.append(fang((0.014, 0.400, z + 0.006), length=0.016, width=0.008))
    m.add("face", face, parent="body")

    for side, nm in ((-1, "l"), (1, "r")):
        stub = tube([(0, 0, 0), (0, 0.004, 0.090), (0, 0.000, 0.170)], [0.044, 0.040, 0.040], seg=12)
        m.add("arm-" + nm, [piece(stub, R_ARM, 12, "rows")] + mitt((0.0, 0.0, 0.215), 0.046, R_ARM),
              at=(side * 0.225, 0.350, 0.025), parent="body")
    m.write(palette(s["glow"]))
    return m


def lantern():
    s = SCHEMES["lookout"]
    name = "nemu_ghost_lantern" + SUFFIX
    m = Model(name)
    m.folder = OUT
    m.texture = name + "-atlas.png"
    R_GLASS, R_WOOD = (0, 0, .5, .25), (.5, 0, .75, .25)
    g0, g1, g2 = s["glass"]
    a = Atlas(g1)
    # The glass: hottest in its middle, amber toward its top and bottom edges, a soft flame shape up the front.
    a.paint(R_GLASS, lambda u, t: mix(mix(mix(flat(u, g0), g1, np.clip(1 - np.abs(t - 0.5) * 1.6, 0, 1)), g2,
                                          np.exp(-((t - 0.45) / 0.18) ** 2) * 0.85),
                                      g2, np.exp(-(((u - 0.25) / 0.06) ** 2 + ((t - 0.5) / 0.22) ** 2)) * 0.6))
    a.paint(R_WOOD, lambda u, t: mix(flat(u, s["frame"]), "#7A5434", np.exp(-((t - 0.7) / 0.12) ** 2) * 0.55))
    OUT.mkdir(parents=True, exist_ok=True)
    a.save(OUT / m.texture)

    # Pivot (the top of its handle) at the origin, hanging down; the glass's middle at y -.20 (ASTRA's LookoutGlass).
    handle = tube([(-0.040, -0.075, 0.0), (-0.034, -0.030, 0.0), (0.0, -0.006, 0.0), (0.034, -0.030, 0.0), (0.040, -0.075, 0.0)], 0.009, seg=8)
    cap = lathe([(0.085, 0.0), (0.082, 0.020), (0.060, 0.040), (0.020, 0.052), (0.0, 0.056)], seg=20)
    glass = rounded((0.068, 0.072, 0.068), 0.040, seg=20, rings=14)
    base = lathe([(0.0, -0.022), (0.070, -0.022), (0.084, -0.008), (0.084, 0.0), (0.0, 0.0)], seg=20)
    knob = ellipsoid((0.020, 0.016, 0.020), seg=12, rings=8)
    parts = [(handle, INK),
             turned(piece(cap, R_WOOD, 20), at=(0, -0.130, 0)),
             turned(piece(glass, R_GLASS, 20), at=(0, -0.200, 0)),
             turned(piece(base, R_WOOD, 20), at=(0, -0.272, 0)),
             turned(piece(knob, R_WOOD, 12), at=(0, -0.300, 0))]
    # Four slim posts at the glass's corners, so it reads as a lantern and not a lit cube.
    for cx, cz in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
        post = tube([(cx * 0.060, -0.130, cz * 0.060), (cx * 0.064, -0.200, cz * 0.064), (cx * 0.060, -0.270, cz * 0.060)], 0.009, seg=8)
        parts.append(piece(post, R_WOOD, 8, "rows"))
    m.add("lantern", parts)
    m.write(palette(s["glow"]))
    return m


if __name__ == "__main__":
    blindfold()
    skitter()
    lookout()
    lantern()
