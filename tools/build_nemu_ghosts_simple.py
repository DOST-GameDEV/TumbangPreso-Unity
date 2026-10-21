"""Nemu's three ghosts, SIMPLE (v6). Replaces the look of tools/build_nemu_ghosts.py (v1 to v5), which the owner refused:
"no, they look ugly as hell. be more simplistic".

The rules this file keeps (owner via the coordinator, 2026-10-19):
  * ONE main form per ghost, plus at most two small parts (nub arms, or the cloth / lantern / slipper its job needs).
  * No hair, fangs, tongue, claws, mitts, multi-lobe hems or bead tails. No blush and glint pile-up.
  * Face: two ink dots or bars and at most a tiny mouth, like the cast's flat faces.
  * One flat body colour, one shade band, a lighter crown (soft rim) and a tail that pales out (see-through feel).
  * The three differ by silhouette and one prop: DON'T LOOK a small round ghost with one cloth band over its eyes;
    SKITTER low and wide, carrying the slipper; LOOKOUT taller, holding a lantern.

Two readings of "simple", both built so the owner can pick:
  A  BLOCK  a soft rounded block like Kuro's head, with one short wisp of tail under it.
  B  DROP   the classic sheet ghost: one teardrop, round on top, its tail tip swept back.

  py -3 tools/build_nemu_ghosts_simple.py
  blender -b --python tools/review_nemu_ghosts_simple.py -- --version=v6
  py -3 tools/review_nemu_ghosts_simple.py --join --version=v6

Writes ArtSource/nemu-ghosts/simple/nemu_ghost_<job>_<a|b>.glb (+ -atlas.png) and nemu_ghost_lantern_simple.glb,
outside Assets, so the game is unchanged. Metres, y up, +z the face, centred on x and z, standing on y 0.

Sizes are chosen to sit with Kuro (about 0.45 m in the game) rather than ASTRA's 0.85 m child; so ASTRA's attach
constants must be re-read off these (each model's `ATTACH` line is printed when it is built).
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import Model, ellipsoid, lathe, moved, rounded, tube  # noqa: E402

OUT = kit.ROOT / "ArtSource/nemu-ghosts/simple"
INK, WHITE, GLOW = 0, 1, 2
ATLAS, PAD = 512, 0.006
R_BODY, R_TAIL, R_PROP, R_PROP2 = (0, 0, .5, .25), (.5, 0, .75, .25), (.75, 0, 1, .25), (0, .25, .25, .5)

# One body colour each, from Nemu's own violets (person_team-nemu.tres); the cloth is her violet, the lantern warm.
JOBS = {
    "dontlook": dict(body="#E9E1FB", shade="#C3B2EE", prop="#8A3CD0", prop_shade="#6A2CA6"),
    "skitter": dict(body="#B98AF0", shade="#9264D6", prop="#B98AF0", prop_shade="#9264D6"),
    "lookout": dict(body="#5B4890", shade="#433570", prop="#4A3020", prop_shade="#FFE7A3"),
}


def hexc(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32)


def smooth01(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def paint(path, c, band):
    """Body: flat colour, ONE shade band low down, a lighter crown. Tail: the body colour paling to near white."""
    from PIL import Image
    img = np.zeros((ATLAS, ATLAS, 3), np.float32) + hexc(c["body"])

    def rect(r):
        x0, y0, x1, y1 = (int(v * ATLAS) for v in r)
        t = 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0)
        return np.repeat(t[:, None], x1 - x0, axis=1), (x0, y0, x1, y1)

    def put(r, fn):
        t, (x0, y0, x1, y1) = rect(r)
        img[y0:y1, x0:x1] = fn(t)

    body, shade, light = hexc(c["body"]), hexc(c["shade"]), np.minimum(hexc(c["body"]) * 1.12 + 18, 255)
    lo, hi = band

    def body_fn(t):
        inside = (smooth01(lo - .06, lo + .04, t) * (1 - smooth01(hi - .04, hi + .06, t))) * .7
        crown = smooth01(.78, 1.0, t) * .55
        fade = (1 - smooth01(0.0, lo, t)) ** 2                # below the band: the tail paling out, only at its tip
        out = body[None, None] * np.ones(t.shape + (3,))
        out = out * (1 - inside[..., None]) + shade * inside[..., None]
        out = out * (1 - crown[..., None]) + light * crown[..., None]
        pale = np.array([250, 248, 255], np.float32)
        return out * (1 - fade[..., None] * .7) + pale * fade[..., None] * .7

    put(R_BODY, body_fn)
    put(R_TAIL, lambda t: body[None, None] * (1 - ((1 - t) ** 2 * .6)[..., None]) + np.array([250, 248, 255], np.float32) * ((1 - t) ** 2 * .6)[..., None])
    put(R_PROP, lambda t: hexc(c["prop"]) * smooth01(.0, .35, t)[..., None] + hexc(c["prop_shade"]) * (1 - smooth01(.0, .35, t)[..., None]))
    put(R_PROP2, lambda t: hexc(c["prop_shade"]) * np.ones(t.shape + (3,)))
    Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB").save(path)


def uv(mesh, r, cols, along="axis"):
    inset = (r[0] + PAD, r[1] + PAD, r[2] - PAD, r[3] - PAD)
    return (mesh, kit.grid_uv(mesh, inset, cols, along, 1))


# ------------------------------------------------------------------ the two forms

def block_form(w, h, d, lift, tail_len):
    """Option A: a soft rounded block (Kuro's head) on one short wisp. Returns pieces, the front surface function and
    the block's (centre y, half) for placing the face and props."""
    half = (w / 2, h / 2, d / 2)
    radius = min(half) * 0.62
    cy = lift + half[1]
    blk = rounded(half, radius, seg=24, rings=16)
    # The block's UVs run its own height, so the band sits at the block's foot.
    pieces = [(moved(blk, at=(0, cy, 0)), kit.grid_uv(blk, (R_BODY[0] + PAD, R_BODY[1] + PAD, R_BODY[2] - PAD, R_BODY[3] - PAD), 24, "axis", 1))]
    top = lift + radius * .9
    # v6b: the first wisp hung straight down, pale, and read as an ice-cream cone. Short, fat, curled back and to one side.
    wisp = tube([(0, top, 0), (0, lift - tail_len * .10, -d * .10), (w * .08, lift - tail_len * .45, -d * .32), (w * .16, lift - tail_len * .62, -d * .55)],
                [w * .34, w * .25, w * .13, w * .04], seg=16)
    pieces.append(uv(wisp, R_TAIL, 16, "rows"))

    def front(x, y):
        core = np.maximum(np.array(half) - radius, 0)
        dx = max(abs(x) - core[0], 0); dy = max(abs(y - cy) - core[1], 0)
        dz = math.sqrt(max(radius ** 2 - dx * dx - dy * dy, 1e-9))
        n = np.array([math.copysign(dx, x), math.copysign(dy, y - cy), dz]) / radius
        return (x, y, core[2] + dz), (-math.degrees(math.asin(n[1])), math.degrees(math.atan2(n[0], n[2])), 0)
    return pieces, front, (cy, half)


def drop_form(w, h, d, lift):
    """Option B: one teardrop sheet, round on top, tapering to a tip swept back and a little to one side."""
    R = w / 2
    # v6b: a round dome (the first profile had nine points and came out a pointed onion), straight-ish sides, a short tip.
    prof = []
    for i in range(25):
        y = i / 24
        if y >= 0.52:
            r = math.sqrt(max(0.0, 1 - ((y - 0.52) / 0.48) ** 2))
        else:
            r = math.sin(math.pi / 2 * (y / 0.52)) ** 0.8
        prof.append((max(r, 0.0), y))
    prof[0] = (0.0, 0.0); prof[-1] = (0.0, 1.0)
    P = [(r * R, lift + y * h) for r, y in prof]
    mesh = lathe(P, seg=28, squash_z=d / w)
    pos = mesh[0].copy()
    k = np.clip((lift + 0.40 * h - pos[:, 1]) / (0.40 * h), 0, 1) ** 2   # 0 above the waist, 1 at the tip
    pos[:, 2] -= k * R * 0.55
    pos[:, 0] += k * R * 0.22
    bent = (pos, mesh[1], mesh[2])
    pieces = [uv(bent, R_BODY, 28)]
    ys = np.array([p[1] for p in P]); rs = np.array([p[0] for p in P])

    def front(x, y):
        r = float(np.interp(y, ys, rs))
        z = math.sqrt(max(r * r - x * x, 1e-9)) * d / w
        # A lathe's normal, roughly: out from the axis, tipped by the profile's slope.
        dr = (float(np.interp(y + .01, ys, rs)) - float(np.interp(y - .01, ys, rs))) / .02
        return (x, y, z), (math.degrees(math.atan(dr)) * .6, math.degrees(math.atan2(x, z)), 0)
    return pieces, front, (lift + 0.62 * h, (R, h / 2, R * d / w))


def eyes(front, y, gap, kind):
    """Two ink eyes: 'dot' (round) or 'bar' (a flat, watchful lid line)."""
    out = []
    for side in (-1, 1):
        at, turn = front(side * gap, y)
        half = (0.024, 0.030, 0.008) if kind == "dot" else (0.030, 0.011, 0.008)
        out.append((moved(ellipsoid(half, seg=16, rings=10), turn=turn, at=at), INK))
    return out


def mouth(front, y):
    at, turn = front(0.0, y)
    return [(moved(ellipsoid((0.010, 0.007, 0.006)), turn=turn, at=at), INK)]


def nub(side, length=0.06, r=0.032):
    """A tiny arm: shoulder at the origin, a short round stub along +z."""
    return tube([(0, 0, 0), (0, 0, length)], [r, r * .9], seg=12)


# ------------------------------------------------------------------ the ghosts

def build(job, option):
    c = JOBS[job]
    name = "nemu_ghost_%s_%s" % (job, option)
    m = Model(name)
    m.folder = OUT
    m.texture = name + "-atlas.png"
    OUT.mkdir(parents=True, exist_ok=True)
    w, h, d, lift = {"dontlook": (0.30, 0.27, 0.27, 0.16), "skitter": (0.36, 0.20, 0.27, 0.13), "lookout": (0.28, 0.36, 0.25, 0.18)}[job]
    if option == "a":
        pieces, front, (cy, half) = block_form(w, h, d, lift, lift)
        H = h + lift
        band = (0.12, 0.26)
        face_y = cy + half[1] * .02
    else:
        H = h + lift + (0.04 if job != "skitter" else 0.0)
        pieces, front, (cy, half) = drop_form(w, H, d, 0.0)
        band = (0.30, 0.40)
        face_y = H * .64
        lift = 0.0
    paint(m.folder / m.texture, c, band)
    m.add("body", pieces)
    face = []
    attach = {}
    if job == "dontlook":
        # The cloth: ONE band round the head at eye level. No knot, no tails. A tiny mouth under it.
        if option == "a":
            hb = (half[0] + .010, .042, half[2] + .010)
            band_mesh = rounded(hb, min(hb) * .9, seg=24, rings=10)
            m.add("cloth", [(moved(band_mesh, at=(0, face_y, 0)), kit.grid_uv(band_mesh, (R_PROP[0] + PAD, R_PROP[1] + PAD, R_PROP[2] - PAD, R_PROP[3] - PAD), 24, "axis", 1))])
        else:
            r_at = front(0.0, face_y)[0][2] / (d / w)
            ring = lathe([(r_at + .010, face_y - .042), (r_at + .016, face_y - .02), (r_at + .016, face_y + .02), (r_at + .008, face_y + .042)], seg=28, squash_z=d / w)
            m.add("cloth", [uv(ring, R_PROP, 28)])
        face += mouth(front, face_y - 0.062)
        attach = {"HeadCentre": (0, round(face_y, 3), 0), "face plane z": round(front(0, face_y)[0][2], 3)}
    elif job == "skitter":
        face += eyes(front, face_y + .005, 0.070, "dot") + mouth(front, face_y - 0.050)
        sy = (cy - half[1] * .35) if option == "a" else H * .42
        for side, nm in ((-1, "l"), (1, "r")):
            sx = side * (w / 2 - .02)
            m.add("arm-" + nm, [uv(nub(side), R_BODY, 12, "rows")], at=(sx, sy, front(0, sy)[0][2] * .55))
        attach = {"shoulders": (round(w / 2 - .02, 3), round(sy, 3)), "SkitterHands": (0, round(sy - .04, 3), round(front(0, sy)[0][2] + .07, 3))}
    else:
        face += eyes(front, face_y, 0.062, "bar") + mouth(front, face_y - 0.052)
        sy = (cy - half[1] * .2) if option == "a" else H * .50
        sx = w / 2 - .015
        m.add("arm-r", [uv(nub(1, .07, .034), R_BODY, 12, "rows")], at=(sx, sy, 0.02))
        attach = {"shoulder-r": (round(sx, 3), round(sy, 3), 0.02), "hand along arm z": 0.07}
    m.add("face", face)
    m.write(["#120E1C", "#FFFFFF", "#FFE39A"] + ["#FF00FF"] * 13)
    top = max(float(p[0][:, 1].max()) for p, _ in pieces)
    print("ATTACH", name, "height %.2f m" % top, attach)


def lantern():
    c = JOBS["lookout"]
    name = "nemu_ghost_lantern_simple"
    m = Model(name)
    m.folder = OUT
    m.texture = name + "-atlas.png"
    paint(m.folder / m.texture, dict(body=c["prop"], shade=c["prop"], prop=c["prop"], prop_shade="#2E1E12"), (0.1, 0.2))
    # Pivot at the top of the handle; a dark cap, one warm rounded glass, nothing else.
    handle = tube([(-.026, -.040, 0), (0, -.004, 0), (.026, -.040, 0)], .007, seg=8)
    cap = rounded((.050, .016, .050), .014, seg=16, rings=8)
    glass = rounded((.044, .052, .044), .034, seg=18, rings=12)
    m.add("lantern", [(handle, INK), uv(moved(cap, at=(0, -.050, 0)), R_PROP, 16),
                      (moved(glass, at=(0, -.110, 0)), GLOW)])
    m.write(["#120E1C", "#FFFFFF", "#FFE39A"] + ["#FF00FF"] * 13)
    print("ATTACH", name, "glass centre y -0.110")


# ------------------------------------------------------------------ v7: pet-animal ghosts in Kuro's family
#
# Owner, 2026-10-19: "since kuro is a cat ghost are you able to design other pet animals". Kuro is a rounded block head
# with cat ears, an ink face and a tail of three beads paling to white. Each ghost here is the same recipe with another
# animal's ears or snout: one rounded block, Kuro's bead tail, two ink eyes, at most a tiny mouth or nose, one colour with
# one shade band. The animal reads from the silhouette only. Two animals per job so he can point:
#   DON'T LOOK  bat (tall pointed ears, two small wing nubs) | mouse (two big round ears)       + the cloth band
#   SKITTER     puppy (floppy ears, a snout; carries the slipper in its mouth, the fetch)  | bunny (long ears, nub arms)
#   LOOKOUT     owl (ear tufts, a small beak, round eyes)      | frog (eyes up on two bumps)   + the lantern on a nub arm

BODY, SHADE_CELL, BEAK = 3, 4, 5


def kuro_tail(w, lift, d):
    """Kuro's tail: three beads under the body, shrinking and paling to white, leaning back."""
    out = []
    spec = [((0.0, lift - 0.010, -0.010), (w * .27, w * .17, w * .25)), ((0.010, lift - 0.075, -0.040), (w * .19, w * .13, w * .18)),
            ((0.022, lift - 0.125, -0.070), (w * .12, w * .09, w * .12))]
    u0, v0, u1, v1 = R_TAIL
    for i, (at, half) in enumerate(spec):
        top, bottom = v1 - (v1 - v0) * (i + 1) / 3, v1 - (v1 - v0) * i / 3
        bead = ellipsoid(half, seg=16, rings=10)
        out.append((moved(bead, at=at), kit.grid_uv(bead, (u0 + PAD, top + PAD, u1 - PAD, bottom - PAD), 16, "axis", 1)))
    return out


def paint_v7(path, c):
    """Body as v6 (one shade band low, a lighter crown); the tail rect runs from the body colour (its bottom is the
    first bead) to near white."""
    from PIL import Image
    paint(path, c, (0.10, 0.24))
    img = np.array(Image.open(path)).astype(np.float32)
    x0, y0, x1, y1 = (int(v * ATLAS) for v in R_TAIL)
    t = 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0)
    col = hexc(c["body"])[None] * (1 - t[:, None] * .75) + np.array([250, 248, 255], np.float32)[None] * (t[:, None] * .75)
    img[y0:y1, x0:x1] = col[:, None, :]
    Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB").save(path)


ANIMALS = {"dontlook": ("bat", "mouse"), "skitter": ("puppy", "bunny", "croc"), "lookout": ("owl", "frog")}


LAST = {}
# Arm nub lengths (board metres). The game's copies are longer (`export_final`), so the arms reach without stretching to sticks.
ARM = {"skitter": 0.06, "lookout": 0.07}


def build_animal(job, animal, suffix="", sprung=False):
    c = JOBS[job]
    name = "nemu_ghost_%s_%s%s" % (job, animal, suffix)
    m = Model(name)
    m.folder = OUT
    m.texture = name + "-atlas.png"
    OUT.mkdir(parents=True, exist_ok=True)
    w, h, d, lift = {"dontlook": (0.29, 0.25, 0.25, 0.17), "skitter": (0.32, 0.23, 0.26, 0.15), "lookout": (0.29, 0.30, 0.26, 0.17)}[job]
    if animal == "frog":
        w, h = 0.33, 0.24
    paint_v7(m.folder / m.texture, c)
    pieces, front, (cy, half) = block_form(w, h, d, lift, lift)
    pieces = pieces[:1] + kuro_tail(w, lift, d)               # Kuro's beads instead of v6's wisp
    top = cy + half[1]
    parts = []                                                   # the animal's silhouette, in the body's own colour
    face_y = cy + half[1] * .05
    eye_gap, eye_y = 0.062, face_y
    mouth_y = face_y - 0.055
    if animal == "bat":
        for s in (-1, 1):
            # v7c: pointed ears (v7, v7b) read as Kuro's cat. A bat now reads from its WINGS: small round ears, and a
            # wide scalloped wing each side, three soft fingers fanned up and out.
            ear = ellipsoid((0.040, 0.044, 0.016), seg=16, rings=10)
            parts.append((moved(ear, turn=(0, 0, -s * 30), at=(s * 0.100, top + 0.006, -0.01)), BODY))
            for k, (ang, ln) in enumerate(((55, 0.085), (25, 0.095), (-5, 0.075))):
                finger = ellipsoid((ln, 0.034, 0.012), seg=16, rings=10)
                a = math.radians(ang)
                parts.append((moved(finger, turn=(0, s * 14, s * ang), at=(s * (half[0] + ln * .75 * math.cos(a)), cy + 0.01 + ln * .75 * math.sin(a), -0.03)), SHADE_CELL))
    elif animal == "mouse":
        for s in (-1, 1):
            ear = ellipsoid((0.066, 0.066, 0.016), seg=20, rings=12)
            parts.append((moved(ear, turn=(0, -s * 12, -s * 14), at=(s * 0.105, top + 0.015, -0.02)), EAR))
    elif animal == "puppy":
        for s in (-1, 1):
            ear = ellipsoid((0.040, 0.085, 0.020), seg=16, rings=12)
            parts.append((moved(ear, turn=(0, 0, s * 14), at=(s * (half[0] + 0.012), top - 0.065, 0.015)), SHADE_CELL))
        at, turn = front(0.0, face_y - 0.040)
        parts.append((moved(ellipsoid((0.058, 0.040, 0.034), seg=18, rings=10), turn=turn, at=(at[0], at[1], at[2] - 0.010)), BODY))
        mouth_y = None
        eye_y = face_y + 0.025
    elif animal == "bunny":
        for s in (-1, 1):
            ear = ellipsoid((0.034, 0.110, 0.020), seg=16, rings=12)
            parts.append((moved(ear, turn=(0, 0, -s * 10), at=(s * 0.065, top + 0.085, -0.01)), BODY))
            inner = ellipsoid((0.018, 0.080, 0.008), seg=14, rings=10)      # v12: the inner ear carries the ear colour
            parts.append((moved(moved(inner, at=(0, -0.006, 0.016)), turn=(0, 0, -s * 10), at=(s * 0.065, top + 0.085, -0.01)), EAR))
    elif animal == "owl":
        for s in (-1, 1):
            # v7b: v7's tufts stood up like cat ears. Small and flicked out sideways off the corners, an owl's.
            tuft = lathe([(0.030, 0.0), (0.020, 0.025), (0.0, 0.050)], seg=14, squash_z=0.5)
            parts.append((moved(tuft, turn=(0, 0, -s * 62), at=(s * (half[0] - 0.020), top - 0.022, -0.01)), BODY))
        at, turn = front(0.0, face_y - 0.020)
        beak = lathe([(0.020, 0.0), (0.0, 0.034)], seg=12, squash_z=0.7)
        parts.append((moved(moved(beak, turn=(180, 0, 0)), turn=turn, at=(at[0], at[1] + 0.012, at[2] + 0.004)), BEAK))
        mouth_y = None
        eye_gap = 0.070
    elif animal == "croc":
        # Owner: "i wanna take inspo from fuecoco for one design". INSPIRATION ONLY, made our own: what is kept is the
        # charm (a chubby body, a long flat rounded snout with two nostril dots, eyes set high above it, one little thing on
        # top). What is NOT taken: its red and yellow, its cream face patch, its flame tuft, its legs. Ours is Skitter's
        # lavender all over, Kuro's bead tail, a curled ghost wisp on top, and it carries the slipper in its snout.
        # v7d: a longer, flatter snout than v7c's (that one read as a hippo), wider at its rounded tip.
        snout = rounded((0.092, 0.038, 0.165), 0.034, seg=20, rings=12)
        sp = snout[0].copy()
        sp[:, 0] *= 0.82 + 0.18 * np.clip((sp[:, 2] + 0.165) / 0.33, 0, 1)
        parts.append(((sp + np.array([0.0, cy - half[1] * .45, half[2] + 0.095], np.float32), snout[1], snout[2]), BODY))
        wisp = tube([(0.0, top - 0.010, -0.020), (0.004, top + 0.040, -0.024), (0.030, top + 0.066, -0.036), (0.052, top + 0.052, -0.046)],
                    [0.030, 0.024, 0.016, 0.008], seg=10)
        parts.append((wisp, SHADE_CELL))
        mouth_y = None
        eye_y = cy + half[1] * .40
        eye_gap = 0.072
    elif animal == "bear":
        # v11, owner: "i want a bear design for that, instead of ac croc". Round ears, and the two side-by-side ovals
        # from the croc's v10 muzzle, small and pale low on the face, a nose on top where they meet.
        for s in (-1, 1):
            ear = ellipsoid((0.046, 0.044, 0.024), seg=18, rings=12)
            parts.append((moved(ear, turn=(0, 0, -s * 18), at=(s * (half[0] - 0.030), top + 0.004, -0.01)), BODY))
        my = face_y - 0.048
        at, turn = front(0.0, my)
        for s in (-1, 1):
            lobe = ellipsoid((0.036, 0.027, 0.026), seg=20, rings=12)
            parts.append((moved(lobe, turn=turn, at=(s * 0.029, my, at[2] + 0.004)), PATCH))
        mouth_y = None
        eye_y = face_y + 0.022
        eye_gap = 0.064
    elif animal == "frog":
        for s in (-1, 1):
            bump = ellipsoid((0.058, 0.050, 0.052), seg=18, rings=12)
            parts.append((moved(bump, at=(s * 0.085, top - 0.004, 0.035)), BODY))
        eye_y = None
        mouth_y = face_y - 0.020
    m.add("body", pieces + parts)
    face = []
    if animal == "owl":
        # v10, owner: "the owl could use some larger eyes". About 1.65x, close to the beak, each on a thin pale ring.
        for s in (-1, 1):
            at, turn = front(s * 0.066, eye_y + 0.004)
            face.append((moved(ellipsoid((0.050, 0.054, 0.006), seg=20, rings=12), turn=turn, at=at), WHITE))
            face.append((moved(ellipsoid((0.040, 0.044, 0.010), seg=20, rings=12), turn=turn, at=(at[0], at[1], at[2] + 0.003)), INK))
    elif eye_y is not None and job != "dontlook":
        face += eyes(front, eye_y, eye_gap, "dot")
    if animal == "frog":
        for s in (-1, 1):
            face.append((moved(ellipsoid((0.030, 0.034, 0.012), seg=16, rings=10), turn=(-12, 0, 0), at=(s * 0.085, top + 0.008, 0.035 + 0.052)), INK))
    if animal == "croc":
        sy, sz = cy - half[1] * .45 + 0.038, half[2] + 0.095 + 0.140
        for s in (-1, 1):
            face.append((moved(ellipsoid((0.011, 0.007, 0.009)), at=(s * 0.030, sy + 0.002, sz)), INK))
    if animal == "bear":
        at, turn = front(0.0, face_y - 0.048)
        face.append((moved(ellipsoid((0.020, 0.013, 0.012), seg=16, rings=10), at=(0.0, face_y - 0.048 + 0.022, at[2] + 0.024)), INK))
    if animal == "puppy":
        at, turn = front(0.0, face_y - 0.040)
        face.append((moved(ellipsoid((0.020, 0.014, 0.010)), turn=turn, at=(at[0], at[1] + 0.022, at[2] + 0.024)), INK))
    if mouth_y is not None and job != "dontlook":
        face += mouth(front, mouth_y)
    if job == "dontlook" and sprung:
        # SPRUNG (the game swaps to this when it jumps a victim): the cloth has slipped down round its neck, its eyes show.
        hb = (half[0] + .016, .036, half[2] + .016)
        band_mesh = rounded(hb, min(hb) * .9, seg=24, rings=10)
        m.add("cloth", [(moved(band_mesh, at=(0, cy - half[1] + 0.030, 0)), kit.grid_uv(band_mesh, (R_PROP[0] + PAD, R_PROP[1] + PAD, R_PROP[2] - PAD, R_PROP[3] - PAD), 24, "axis", 1))])
        face += eyes(front, face_y + 0.004, 0.062, "dot")
        face += mouth(front, face_y - 0.062)
    elif job == "dontlook":
        hb = (half[0] + .010, .042, half[2] + .010)
        band_mesh = rounded(hb, min(hb) * .9, seg=24, rings=10)
        m.add("cloth", [(moved(band_mesh, at=(0, face_y, 0)), kit.grid_uv(band_mesh, (R_PROP[0] + PAD, R_PROP[1] + PAD, R_PROP[2] - PAD, R_PROP[3] - PAD), 24, "axis", 1))])
        face += mouth(front, face_y - 0.062)
    if job == "skitter" and animal in ("bunny", "bear"):
        sy = cy - half[1] * .35
        for side, nm in ((-1, "l"), (1, "r")):
            m.add("arm-" + nm, [uv(nub(side, ARM["skitter"]), R_BODY, 12, "rows")], at=(side * (w / 2 - .02), sy, front(0, sy)[0][2] * .55))
    if job == "lookout":
        sy = cy - half[1] * .2
        m.add("arm-r", [uv(nub(1, ARM["lookout"], .034), R_BODY, 12, "rows")], at=(w / 2 - .015, sy, 0.02))
        if animal == "owl":   # the game lifts the can with BOTH hands, so the owl has a left nub too
            m.add("arm-l", [uv(nub(-1, ARM["lookout"], .034), R_BODY, 12, "rows")], at=(-(w / 2 - .015), sy, 0.02))
    if face:
        m.add("face", face)
    m.write(cells(c))
    print("ATTACH", name, "top %.2f" % max(float(p[0][:, 1].max()) for p, _ in pieces + parts), "face y %.3f" % face_y)
    eye_at = front(eye_gap if animal != "owl" else 0.066, (eye_y if eye_y is not None else face_y) + (0.004 if animal == "owl" else 0))[0]
    LAST.update(model=m, cells=cells(c), cy=cy, half=half, face_y=face_y, lift=lift, w=w, front_z=front(0, face_y)[0][2], eye=eye_at,
                muzzle_z=front(0, face_y - 0.048)[0][2])
    return name


def build_v7():
    for job, pair in ANIMALS.items():
        for animal in pair:
            build_animal(job, animal)
    lantern()


# ------------------------------------------------------------------ v8: the owner's picks, colour sets, and the croc
#
# Owner on v7b: picks DON'T LOOK = mouse, SKITTER = bunny, LOOKOUT = owl (silhouettes now LOCKED: v8 changes only
# colour). "you can try different colors btw, the new ghost set isnt necessarily nemu colors too". Of the croc: "fuecoco
# style doesnt look refined yet.. fix it to be more cutesy". Each set keeps ASTRA's rule that each ghost is readable by
# its own colour in play.

SETS = {
    # Soft pastel, one per ghost: mint mouse, peach bunny, butter owl (the croc is lilac so it never reads as a bunny).
    "pastel": {"dontlook": dict(body="#BDE8D3", shade="#8FCDB3", prop="#7A5CC8", prop_shade="#5E44A6"),
               # Owner: "pink rabit, yellow owl".
               # Owner, then: "white rabbit". A soft warm white with a faint pink inner ear, so it never vanishes.
               "skitter": dict(body="#FBF6F0", shade="#E3D6CA", prop="#FBF6F0", prop_shade="#E3D6CA", ear="#F6C3D3", patch="#FFFFFF"),
               "croc": dict(body="#C9B6F2", shade="#A68FDC", patch="#F4EEFF"),
               "lookout": dict(body="#FFE6A0", shade="#E8C46A", prop="#4A3020", prop_shade="#FFE7A3", beak="#F08A4B")},
    # Ghostly cool white, ONE accent each: a violet cloth, pink ears, an amber beak and lantern.
    "ghost": {"dontlook": dict(body="#F3F4FA", shade="#CBD0E4", prop="#8A3CD0", prop_shade="#6A2CA6"),
              "skitter": dict(body="#F3F4FA", shade="#CBD0E4", prop="#F3F4FA", prop_shade="#CBD0E4", ear="#F6A9C6", patch="#FFFFFF"),
              "croc": dict(body="#F3F4FA", shade="#CBD0E4", patch="#FFFFFF", ear="#7FD8C8"),
              "lookout": dict(body="#F3F4FA", shade="#CBD0E4", prop="#4A3020", prop_shade="#FFE7A3", beak="#F2A030")},
    # Warm: dusty rose mouse, caramel bunny, cocoa owl; a plum cloth.
    "warm": {"dontlook": dict(body="#EDB9BC", shade="#CC9398", prop="#5B3A6E", prop_shade="#432A52"),
             "skitter": dict(body="#EDBE8F", shade="#CC9663", prop="#EDBE8F", prop_shade="#CC9663", patch="#FFF0DC"),
             "croc": dict(body="#A9C98A", shade="#86A866", patch="#F3F6DC"),
             "lookout": dict(body="#A9785A", shade="#87593F", prop="#3A2416", prop_shade="#FFE7A3", beak="#F2B04A")},
}
PICKS = {"dontlook": "mouse", "skitter": "bunny", "lookout": "owl"}
EAR, PATCH = 6, 7


def cells(c):
    return ["#120E1C", "#FFFFFF", "#FFE39A", c["body"], c["shade"], c.get("beak", "#F2A65A"), c.get("ear", c["body"]),
            c.get("patch", c["body"])] + ["#FF00FF"] * 8


def build_v8_pick(job, cset):
    """The locked pick in another colour set: v7's build with the set's colours and its ear accent."""
    global JOBS
    keep = JOBS
    JOBS = dict(keep); JOBS[job] = SETS[cset][job]
    try:
        name = build_animal(job, PICKS[job], suffix="_" + cset)
    finally:
        JOBS = keep
    return name


def build_croc(cset, snout=None):
    """SKITTER's alternative, rebuilt cutesy (v8): a chubby PEAR body rounder than the block heads, a long soft spoon
    snout with a rounded, slightly upturned tip and two nostril dots, big round eyes high where the snout meets the
    head, a pale belly and jaw patch in its own colour, a little wisp on top, two tiny stubby arms, Kuro's tail.
    Inspiration only (the owner's Fuecoco reference): none of its colours, no flame, no cream face mask, no legs."""
    c = SETS[cset]["croc"]
    name = "nemu_ghost_skitter_croc_" + (snout + "_" if snout else "") + cset
    m = Model(name)
    m.folder = OUT
    m.texture = name + "-atlas.png"
    OUT.mkdir(parents=True, exist_ok=True)
    paint_v7(m.folder / m.texture, dict(c, prop=c["body"], prop_shade=c["shade"]))
    w, lift = 0.32, 0.15
    # The pear: fullest low (its belly), narrowing to a round crown. Smooth lathe, squashed a little front to back.
    H = 0.27
    prof = [(0.0, 0.0), (0.55, 0.03), (0.86, 0.12), (1.0, 0.28), (0.98, 0.45), (0.88, 0.62), (0.74, 0.78), (0.52, 0.92), (0.0, 1.0)]
    P = [(r * w / 2, lift + y * H) for r, y in prof]
    sq = 0.88
    pear = lathe(P, seg=28, squash_z=sq)
    pieces = [uv(pear, R_BODY, 28)] + kuro_tail(w * .9, lift + 0.03, w)
    ys = np.array([p[1] for p in P]); rs = np.array([p[0] for p in P])

    def zfront(x, y):
        r = float(np.interp(y, ys, rs))
        return math.sqrt(max(r * r - x * x, 1e-9)) * sq

    parts = []
    # The pale belly, low on the front (its own colour, a tint of the body).
    by = lift + 0.30 * H
    if not snout:   # v9: the pale lower jaw is the one pale patch; a belly as well merged with it into a big grin
        parts.append((moved(ellipsoid((0.100, 0.078, 0.070), seg=20, rings=12), at=(0.0, by, zfront(0, by) - 0.058)), PATCH))
    # The snout: a soft wide spoon, its tip rounder and a touch higher than its root. Upper jaw body colour, lower jaw pale.
    sy = lift + 0.56 * H
    root = zfront(0, sy) - 0.040
    # (Built about the origin, squashed, THEN moved: v8's first pass squashed it in place and the snout sank off the face.)
    LOBES = {"small": (0.056, 0.040, 0.050), "chunky": (0.070, 0.052, 0.062)}
    if snout in LOBES:
        # v10, owner: "no bruh like / oo / not / o / o": the two horizontal ellipsoids sit SIDE BY SIDE across the front
        # of the face, touching in the middle, a wide two-lobed puffy muzzle. Each lobe has its nostril near its front.
        rx, ry, rz = LOBES[snout]
        lz = zfront(0, sy) + rz * 0.30
        for side in (-1, 1):
            lobe = ellipsoid((rx, ry, rz), seg=24, rings=14)
            parts.append((moved(lobe, at=(side * rx * 0.86, sy, lz)), BODY))
        L = None
    elif snout:
        # v9, owner: "for the snout try 2 horizontal ellipsoids". Two long rounded jaws lying forward from the face, the
        # upper one a touch longer and wider resting on the pale lower one, a thin gap between them for the mouth (the
        # slipper is held in it). `snout` is "short" (chubby) or "long".
        L = 0.078 if snout == "short" else 0.112
        upper = ellipsoid((0.088, 0.040, L), seg=24, rings=14)
        upper = moved(upper, at=(0, sy, root + L * 0.80))
        lower = ellipsoid((0.076, 0.030, L * 0.88), seg=24, rings=14)
        lower = moved(lower, at=(0, sy - 0.077, root + L * 0.74))
    else:
        upper = tube([(0, 0, 0), (0, 0.004, 0.070), (0, 0.014, 0.125), (0, 0.022, 0.150)], [0.060, 0.056, 0.060, 0.050], seg=20)
        upper = moved(moved(upper, scale=(1.30, 0.70, 1.0)), at=(0, sy, root))
        lower = tube([(0, 0, 0.010), (0, 0, 0.070), (0, 0.006, 0.118)], [0.050, 0.046, 0.040], seg=18)
        lower = moved(moved(lower, scale=(1.20, 0.55, 1.0)), at=(0, sy - 0.030, root))
    if snout not in LOBES:
        parts.append((upper, BODY))
        parts.append((lower, PATCH))
    # A little curled wisp on the crown (not a flame: a ghost's curl, leaning back).
    top = lift + H
    wisp = tube([(0.0, top - 0.012, -0.010), (0.004, top + 0.026, -0.016), (0.026, top + 0.046, -0.030), (0.044, top + 0.036, -0.042)],
                [0.026, 0.020, 0.013, 0.006], seg=10)
    parts.append((wisp, SHADE_CELL))
    m.add("body", pieces + parts)
    face = []
    # Big round eyes, high, sitting on the head just behind where the snout starts.
    ey = sy + (0.064 if snout in LOBES else 0.050 if snout else 0.058)        # v9: lower, where the snout meets the head
    for s in (-1, 1):
        ex = s * 0.062
        ez = zfront(ex, ey)
        face.append((moved(ellipsoid((0.030, 0.036, 0.012), seg=18, rings=12), turn=(0, math.degrees(math.atan2(ex, ez)), 0), at=(ex, ey, ez)), INK))
        # Two nostril dots on top of the snout's tip.
        if snout in LOBES:
            face.append((moved(ellipsoid((0.007, 0.005, 0.006)), turn=(-35, 0, 0), at=(s * rx * 0.55, sy + ry * 0.45, lz + rz * 0.86)), INK))   # v10b: small, inner and high, so they never read as a second pair of eyes
        elif snout:
            face.append((moved(ellipsoid((0.010, 0.007, 0.008)), turn=(-70, 0, 0), at=(s * 0.026, sy + 0.032, root + L * 1.42)), INK))
        else:
            face.append((moved(ellipsoid((0.010, 0.007, 0.008)), turn=(-60, 0, 0), at=(s * 0.026, sy + 0.050, root + 0.150)), INK))
    m.add("face", face)
    # Tiny stubby arms, low at its sides, shoulder at the node origin pointing forward.
    for side, nm in ((-1, "l"), (1, "r")):
        ay = lift + 0.30 * H
        if snout:   # v9: small soft nubs, sunk into the body so they do not bulge
            m.add("arm-" + nm, [uv(nub(side, 0.018, 0.026), R_BODY, 12, "rows")], at=(side * (w / 2 - 0.050), ay, zfront(side * (w / 2 - .050), ay) * .82))
        else:
            m.add("arm-" + nm, [uv(nub(side, 0.035, 0.030), R_BODY, 12, "rows")], at=(side * (w / 2 - 0.035), ay, zfront(side * (w / 2 - .035), ay) * .6))
    m.write(cells(c))
    if snout in LOBES:
        print("ATTACH", name, "slipper under the muzzle at y %.3f z %.3f" % (sy - ry - 0.012, lz + rz * 0.55))
    else:
        print("ATTACH", name, "mouth y %.3f, slipper z %.3f" % (sy - 0.040, root + (L * 1.05 if snout else 0.12)))
    return name


BEARS = {"pastel": dict(body="#F2D2A4", shade="#DCAE78", prop="#F2D2A4", prop_shade="#DCAE78", patch="#FFF4E4"),
         "warm": dict(body="#B7835F", shade="#946143", prop="#B7835F", prop_shade="#946143", patch="#F0D9C0"),
         # v11b: two options beside the mint mouse, pink bunny and butter owl (honey sat too close to the owl's yellow).
         "cocoa": dict(body="#D8AE92", shade="#BC8C6E", prop="#D8AE92", prop_shade="#BC8C6E", patch="#FFF1E6"),
         # The game's cocoa: the board's read pale tan on the daylight court (court film v1), so it is darker in the game.
         "cocoa_game": dict(body="#9A6646", shade="#764A30", prop="#9A6646", prop_shade="#764A30", patch="#EDD3B6"),
         "lilac": dict(body="#CFC0F2", shade="#A996DC", prop="#CFC0F2", prop_shade="#A996DC", patch="#F7F2FF")}


def build_bear(cset):
    global JOBS
    keep = JOBS
    JOBS = dict(keep); JOBS["skitter"] = BEARS[cset]
    try:
        return build_animal("skitter", "bear", suffix="_" + cset)
    finally:
        JOBS = keep


def build_v11b():
    for job in PICKS:
        build_v8_pick(job, "pastel")
    for c in ("cocoa", "lilac"):
        build_bear(c)
    lantern()


def build_v11():
    for cset in ("pastel", "warm"):
        build_bear(cset)
        for job in PICKS:
            build_v8_pick(job, cset)
    lantern()


def build_v10():
    for cset in ("pastel", "warm"):
        for snout in ("small", "chunky"):
            build_croc(cset, snout)
        build_v8_pick("skitter", cset)
        build_v8_pick("lookout", cset)


def build_v9():
    for cset in ("pastel", "warm"):
        for snout in ("short", "long"):
            build_croc(cset, snout)
        build_v8_pick("skitter", cset)


def build_v8():
    for cset in SETS:
        for job in PICKS:
            build_v8_pick(job, cset)
        build_croc(cset)
    lantern()


# ------------------------------------------------------------------ FINAL: export into the game (behind PetGhostModels.PetGhosts)
#
# Owner, 2026-10-19: "cocoa bear / scrap the rabbit". The set: DON'T LOOK the mint mouse, SKITTER the cocoa bear,
# LOOKOUT the butter owl with the bigger eyes, and the lantern. Exported as the game reads them: ONE mesh per node, so the
# C# side takes each node's mesh and hands it to ASTRA's `NemuGhostBody` unchanged (no combining at run time):
#   nemu_petghost_mouse.glb   `waiting` (the cloth over its eyes) and `sprung` (cloth at its neck, eyes out), body origin
#   nemu_petghost_bear.glb    `body`, `arm-0`, `arm-1`: each arm's shoulder at its node origin, reaching along +z
#   nemu_petghost_owl.glb     `body`, `arm-0`, `arm-1`
#   nemu_petghost_lantern.glb `lantern`, its pivot at the top of the handle, hanging down
# Each atlas carries the paint in its upper half AND the flat cells in its lower half (one texture is the whole look),
# and its ALPHA is the glow mask: 0 on the glow cell (the lantern's glass), 1 everywhere else. `Shaders/PetGhost` reads it.
# The attach numbers the C# side needs are printed (and are typed into `Visual/PetGhostModels.cs`).
#
#   py -3 tools/build_nemu_ghosts_simple.py --final [--final-out=<folder>]

FINAL = Path(next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--final-out=")), str(kit.ROOT / "Assets/TumbangPreso/Resources/Models/NemuGhosts")))


def final_atlas(src, dst, cell_hex):
    from PIL import Image
    img = np.array(Image.open(src).convert("RGB")).astype(np.uint8)
    rgba = np.concatenate([img, np.full(img.shape[:2] + (1,), 255, np.uint8)], axis=2)
    n = img.shape[0]
    for k, hx in enumerate(cell_hex[:16]):
        u0, u1 = (2 * (k % 8)) / 16.0, (2 * (k % 8) + 2) / 16.0
        v0, v1 = (0.5, 0.75) if k < 8 else (0.75, 1.0)
        x0, x1, y0, y1 = int(u0 * n), int(u1 * n), int(v0 * n), int(v1 * n)
        rgba[y0:y1, x0:x1, :3] = hexc(hx).astype(np.uint8)
        if k == GLOW:
            rgba[y0:y1, x0:x1, 3] = 0
    Image.fromarray(rgba, "RGBA").save(dst)


def merged(m, names):
    """The named parts of a built model as one list of pieces in the model's space (node offsets applied)."""
    by = {name: (pieces, at, parent) for name, pieces, at, parent in m.parts}
    out = []
    for name in names:
        if name not in by:
            continue
        pieces, at, parent = by[name]
        off = np.array(at, np.float32)
        while parent:
            _, pat, parent = by[parent]
            off = off + np.array(pat, np.float32)
        out += [((mesh[0] + off, mesh[1], mesh[2]), slot) for mesh, slot in pieces]
    return out


def part_local(m, name):
    for n, pieces, at, parent in m.parts:
        if n == name:
            return pieces, at
    raise KeyError(name)


# The in-game size. The boards were drawn at Kuro's size (about 0.45 m); on the court beside a person at PersonScale that
# was too small to read and the mouse could not cover a face (first court film, 2026-10-19), so the game's copies are
# 1.35 times that, ASTRA's ghosts' own height range (0.6 to 0.7 m). The printed attach numbers are at board size;
# `PetGhostModels.Scale` multiplies them by this same factor.
EXPORT_SCALE = 1.35


def export(final_name, nodes, src_model, cell_hex):
    FINAL.mkdir(parents=True, exist_ok=True)
    k = EXPORT_SCALE
    nodes = [(n, [((m[0] * k, m[1], m[2]), slot) for m, slot in pieces], tuple(v * k for v in at)) for n, pieces, at in nodes]
    out = Model(final_name)
    out.folder = FINAL
    out.texture = final_name + "-atlas.png"
    final_atlas(src_model.folder / src_model.texture, FINAL / out.texture, cell_hex)
    for node, pieces, at in nodes:
        out.add(node, pieces, at=at)
    out.write(cell_hex)


def export_final():
    global JOBS
    keep = JOBS
    ARM["skitter"] = ARM["lookout"] = 0.085
    try:
        # DON'T LOOK: the mint mouse, waiting and sprung.
        JOBS = dict(keep); JOBS["dontlook"] = SETS["pastel"]["dontlook"]
        build_animal("dontlook", "mouse", suffix="_final")
        wait = LAST["model"]; info = dict(LAST)
        build_animal("dontlook", "mouse", suffix="_final_sprung", sprung=True)
        spr = LAST["model"]
        export("nemu_petghost_mouse", [("waiting", merged(wait, ["body", "cloth", "face"]), (0, 0, 0)),
                                       ("sprung", merged(spr, ["body", "cloth", "face"]), (0, 0, 0))], wait, info["cells"])
        print("PETGHOST mouse HeadCentre (0, %.3f, 0) HeadHalfDepth %.3f Eyes (+-%.3f, %.3f, %.3f) TailTop %.3f"
              % (info["face_y"], info["front_z"], info["eye"][0], info["eye"][1], info["eye"][2] + 0.01, info["lift"]))
        # SKITTER: the cocoa bear.
        JOBS = dict(keep); JOBS["skitter"] = BEARS["cocoa_game"]
        build_animal("skitter", "bear", suffix="_final")
        bear = LAST["model"]; info = dict(LAST)
        pl, lat = part_local(bear, "arm-l"); pr, rat = part_local(bear, "arm-r")
        export("nemu_petghost_bear", [("body", merged(bear, ["body", "face"]), (0, 0, 0)), ("arm-0", pl, lat), ("arm-1", pr, rat)], bear, info["cells"])
        hands = (0.0, info["face_y"] - 0.048 - 0.050, info["muzzle_z"] + 0.035)
        print("PETGHOST bear shoulders (+-%.3f, %.3f, %.3f) hand-on-arm z %.3f SkitterHands (0, %.3f, %.3f) Eyes (+-%.3f, %.3f, %.3f) TailTop %.3f"
              % (rat[0], rat[1], rat[2], ARM["skitter"], hands[1], hands[2], info["eye"][0], info["eye"][1], info["eye"][2] + 0.01, info["lift"]))
        # LOOKOUT: the butter owl, and its lantern.
        JOBS = dict(keep); JOBS["lookout"] = SETS["pastel"]["lookout"]
        build_animal("lookout", "owl", suffix="_final")
        owl = LAST["model"]; info = dict(LAST)
        pl, lat = part_local(owl, "arm-l"); pr, rat = part_local(owl, "arm-r")
        export("nemu_petghost_owl", [("body", merged(owl, ["body", "face"]), (0, 0, 0)), ("arm-0", pl, lat), ("arm-1", pr, rat)], owl, info["cells"])
        print("PETGHOST owl shoulders (+-%.3f, %.3f, %.3f) hand-on-arm z %.3f Eyes (+-%.3f, %.3f, %.3f) TailTop %.3f"
              % (rat[0], rat[1], rat[2], ARM["lookout"], info["eye"][0], info["eye"][1], info["eye"][2] + 0.01, info["lift"]))
        lantern()
        lm = Model("nemu_ghost_lantern_simple"); lm.folder = OUT; lm.texture = "nemu_ghost_lantern_simple-atlas.png"
        handle = tube([(-.026, -.040, 0), (0, -.004, 0), (.026, -.040, 0)], .007, seg=8)
        cap = rounded((.050, .016, .050), .014, seg=16, rings=8)
        glass = rounded((.044, .052, .044), .034, seg=18, rings=12)
        pieces = [(handle, INK), uv(moved(cap, at=(0, -.050, 0)), R_PROP, 16), (moved(glass, at=(0, -.110, 0)), GLOW)]
        # The glass: warm amber (v1's pale yellow, lifted by the glow, read as white).
        export("nemu_petghost_lantern", [("lantern", pieces, (0, 0, 0))], lm, ["#120E1C", "#FFFFFF", "#FF9A2E"] + ["#FF00FF"] * 13)
        print("PETGHOST lantern glass (0, -0.110, 0)")
    finally:
        JOBS = keep


if __name__ == "__main__" and "--final" in sys.argv:
    export_final()
    raise SystemExit


if __name__ == "__main__" and "--v11b" in sys.argv:
    build_v11b()
    raise SystemExit


if __name__ == "__main__" and "--v11" in sys.argv:
    build_v11()
    raise SystemExit


if __name__ == "__main__" and "--v10" in sys.argv:
    build_v10()
    raise SystemExit


if __name__ == "__main__" and "--v9" in sys.argv:
    build_v9()
    raise SystemExit


if __name__ == "__main__" and "--v8" in sys.argv:
    build_v8()
    raise SystemExit


if __name__ == "__main__" and "--v7" in sys.argv:
    build_v7()
    raise SystemExit


if __name__ == "__main__":
    for job in JOBS:
        for option in "ab":
            build(job, option)
    lantern()
