"""BAKYA BLOOM's pitcher plant, remodelled as a little character (`Visual.PaetePlantBody`).

Owner, 2026-10-07: "by 'rework' im thinking of remodelling, textures, and vfx.. not just vfx", and, asked whether the
plants should have character like the Arena's cutesy drone: "try the cutesy character for the plants". The first
pitcher (`tools/build_paete_props.py` `seedling`) was faceted, its lid a flat card that cut through the lip, its
colours flat fills. This one is built with the hand companions' kit, so it wears the redesigned cast's dress: round
chunky forms, smooth shading, one palette cell per piece, an ink line.

The character: a fat round jug with two big eyes and blushing cheeks under a thick rolled lip. Its mouth IS the
pitcher's mouth, so the clog it grows it holds in its mouth like a pup with a slipper. A leaf cap for a lid, with a
curl of sprout on top. A chubby S of neck, a rosette of plump leaves (two of them its arms), four root toes.

  py -3 tools/build_paete_bloom.py
  blender -b --python tools/review_hand_companion.py -- --hero=seedling --folder=PaeteProps --version=v1
  py -3 tools/hand_companion_kit.py sheet seedling v1

Writes Assets/TumbangPreso/Resources/Models/PaeteProps/seedling.glb (the first one is kept at
ArtSource/paete/props-pre-rework/seedling.glb). The node NAMES are the body's contract: `stem` (origin on the
ground: the body sinks and squashes it), `pod` under it (pitched about x to rear and spit), `lid` under the pod (its
hinge at the back of the rim, lying forward over the mouth: a negative pitch opens it), `slipper` under the pod (a
stand-in the body swaps for the carved clog), `root-0..3` and `arm-0..1` (each turned to point along its own +z, so
the body pitches it about x), and new for the character: `rosette` (the three still leaves, popped open by the
body), `eye-l`, `eye-r` (scaled in y to blink) and `sprout` on the lid.

Metres, y up, +z its front. The sixteen colours are `PaetePlantBody.Palette`, same order.
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import Model, ellipsoid, join, lathe, moved, tube  # noqa: E402

PALETTE = ["#9CCB3B", "#5F8F2A", "#4F9A2F", "#37752A", "#79AE38", "#C29563", "#B59A6C", "#5E7F24",
           "#1E140C", "#3A1420", "#B02A45", "#D6EE86", "#D9596B", "#8A6240", "#F29AA8", "#FFFDF6"]
BODY, SHADE, LEAF, LEAF_DK, NECK, WOOD, ROOT, MOSS, INK, INSIDE, LIP, BELLY, LID_UNDER, WOOD_DK, BLUSH, WHITE = range(16)

def arg(name, default):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


# Two faces to choose between (owner, 2026-10-07, of the first: "the mouth piece is weird.."):
#   a  the first one tidied: eyes on the jug, the lid a cap. No pale patch (it read as a second, wrong mouth).
#   b  the JAW: the mouth leans well forward and the eyes sit on the lid, so the lid is its upper head and the jug its
#      lower jaw. Opening the lid opens its mouth, and the clog is held between the two like a pup's slipper.
VARIANT = arg("variant", "d")
OPEN = float(arg("open", 0))       # review only: the lid baked open this many degrees. The game's model is 0.
TILT = {"a": 20.0, "d": 24.0}.get(VARIANT, 38.0)     # the mouth leans this far forward, degrees
POD_AT = (0.0, 0.53, 0.03)

# The jug, turned about its own up: (radius, height above the pod's origin). A big round head on a small base.
JUG = [(0.070, -0.03), (0.150, 0.01), (0.205, 0.08), (0.222, 0.16), (0.205, 0.24), (0.165, 0.30), (0.140, 0.345), (0.150, 0.385)]
MOUTH_Y, MOUTH_R = 0.385, 0.150
if VARIANT == "d":
    # A PITCHER, unmistakably: taller than it is wide, its belly low, a narrow neck, a mouth that flares.
    JUG = [(0.060, -0.03), (0.130, 0.01), (0.178, 0.09), (0.182, 0.17), (0.150, 0.27), (0.120, 0.35), (0.118, 0.40), (0.150, 0.455)]
    MOUTH_Y, MOUTH_R = 0.455, 0.150


def jug_radius(y):
    for (r0, y0), (r1, y1) in zip(JUG, JUG[1:]):
        if y0 <= y <= y1:
            return r0 + (r1 - r0) * (y - y0) / (y1 - y0)
    return JUG[-1][0]


def on_jug(angle, y, out=0.0):
    """A point on the jug's skin at `angle` degrees round it (0 = front) and height y, `out` proud of it."""
    a = math.radians(angle)
    r = jug_radius(y) + out
    return (r * math.sin(a), y, r * math.cos(a))


def tilt(mesh):
    return moved(mesh, turn=(TILT, 0, 0))


def lid_mesh(mesh):
    """A piece typed in the lid's own space (from its hinge), opened by OPEN and leant with the jug."""
    return tilt(moved(mesh, turn=(-OPEN, 0, 0)))


def lid_point(p):
    c, s = math.cos(math.radians(-OPEN)), math.sin(math.radians(-OPEN))
    return tilted((p[0], p[1] * c - p[2] * s, p[1] * s + p[2] * c))


def tilted(p):
    c, s = math.cos(math.radians(TILT)), math.sin(math.radians(TILT))
    return (p[0], p[1] * c - p[2] * s, p[1] * s + p[2] * c)


def loop(radius, y, thick, count=28):
    pts = [(radius * math.cos(2 * math.pi * k / count), y, radius * math.sin(2 * math.pi * k / count)) for k in range(count + 1)]
    return tube(pts, thick, seg=10)


def leaf(length, width, thick):
    """A plump leaf along +z from its stem at the origin, with a point."""
    prof = [(0.0, 0.0), (0.42, 0.10), (0.92, 0.36), (1.0, 0.55), (0.78, 0.78), (0.30, 0.94), (0.0, 1.0)]
    blade = lathe([(r * width * .5, y * length) for r, y in prof], seg=14, squash_z=thick / width)
    return moved(blade, turn=(90, 0, 0))


def main():
    m = Model("seedling")
    m.folder = Path(next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--out=")), kit.ROOT / "Assets/TumbangPreso/Resources/Models/PaeteProps"))

    # A low mound of moss it stands in.
    m.add("moss", [(ellipsoid((0.21, 0.045, 0.19), at=(0.0, 0.012, 0.0)), MOSS),
                   (ellipsoid((0.10, 0.04, 0.09), at=(0.13, 0.02, 0.07)), LEAF_DK),
                   (ellipsoid((0.09, 0.035, 0.10), at=(-0.12, 0.018, -0.06)), LEAF_DK)])

    # Four root toes, each pointing along its own +z.
    for i, (yaw, reach, fat) in enumerate([(40, 0.33, 0.050), (128, 0.27, 0.044), (214, 0.36, 0.052), (301, 0.25, 0.042)]):
        toe = tube([(0, 0.05, 0.08), (0, 0.045, reach * .55), (0, 0.01, reach * .88), (0, -0.03, reach)], [fat, fat * .9, fat * .6, fat * .25], seg=10)
        m.add("root-%d" % i, toe, ROOT, yaw=yaw)

    # The rosette: three still leaves under one node the body pops open, and the two arms.
    still = []
    for yaw, pitch, length, width in [(118, 10, 0.48, 0.20), (186, 15, 0.56, 0.22), (250, 8, 0.44, 0.19)]:
        blade = moved(leaf(length, width, 0.05), turn=(-pitch, 0, 0))
        rib = moved(tube([(0, 0.022, 0.04), (0, 0.026, length * .5), (0, 0.016, length * .86)], [0.011, 0.009, 0.004], seg=6), turn=(-pitch, 0, 0))
        still.append((moved(blade, turn=(0, yaw, 0)), LEAF_DK))
        still.append((moved(rib, turn=(0, yaw, 0)), LEAF))
    m.add("rosette", still, at=(0.0, 0.07, 0.0))
    for i, (yaw, pitch, length, width) in enumerate([(42, 18, 0.54, 0.22), (318, 21, 0.52, 0.22)]):
        blade = moved(leaf(length, width, 0.055), turn=(-pitch, 0, 0))
        rib = moved(tube([(0, 0.024, 0.04), (0, 0.028, length * .5), (0, 0.018, length * .86)], [0.011, 0.009, 0.004], seg=6), turn=(-pitch, 0, 0))
        m.add("arm-%d" % i, [(blade, LEAF), (rib, LEAF_DK)], at=(0.0, 0.07, 0.0), yaw=yaw)

    # The neck: a chubby S, with a collar where it leaves the rosette.
    neck = tube([(0.0, 0.03, -0.02), (0.03, 0.20, -0.075), (0.01, 0.36, -0.085), (-0.02, 0.47, -0.035), (0.0, 0.54, 0.03)],
                [0.066, 0.058, 0.052, 0.050, 0.052], seg=12)
    m.add("stem", [(neck, NECK), (ellipsoid((0.10, 0.035, 0.10), at=(0.0, 0.085, -0.03)), LEAF_DK)])

    # The jug, leaning forward. Everything on it is typed upright and leant with it.
    pod = [(tilt(lathe(JUG, seg=24)), BODY)]
    # Veins down the flanks and the back, each a little different. The front is the face's.
    for angle, top, bottom in [(98, 0.29, 0.02), (146, 0.30, 0.01), (180, 0.30, 0.00), (214, 0.30, 0.01), (262, 0.29, 0.02)]:
        ys = [top + (bottom - top) * k / 5 for k in range(6)]
        pod.append((tilt(tube([on_jug(angle, y, 0.004) for y in ys], [0.008, 0.012, 0.013, 0.012, 0.010, 0.005], seg=6)), SHADE))
    # The throat, and the thick rolled lip round it.
    pod.append((tilt(ellipsoid((MOUTH_R - 0.012, 0.030, MOUTH_R - 0.012), at=(0.0, MOUTH_Y - 0.004, 0.0))), INSIDE))
    pod.append((tilt(loop(MOUTH_R, MOUTH_Y, 0.046)), LIP))
    # Cheeks.
    for side in (-1, 1):
        at = on_jug(side * (44 if VARIANT == "a" else 38), 0.165 if VARIANT == "a" else 0.255, 0.002)
        pod.append((tilt(moved(ellipsoid((0.034, 0.020, 0.012)), turn=(0, side * (44 if VARIANT == "a" else 38), 0), at=at)), BLUSH))
    m.add("pod", pod, at=POD_AT, parent="stem")

    hinge = (0.0, MOUTH_Y + 0.030, -MOUTH_R + 0.005)
    jaw = VARIANT == "b"
    # The lid: a domed leaf cap hinged at the back of the rim, lying forward over the mouth. For the jaw it is a
    # taller dome, because it is the top of its head.
    dome = 0.095 if jaw else 0.050
    cap = ellipsoid((0.190, dome, 0.190), at=(0.0, 0.034, 0.170))
    under = ellipsoid((0.165, 0.022, 0.165), at=(0.0, 0.010, 0.170))
    stalk = tube([(0.0, -0.035, -0.010), (0.0, 0.010, 0.010), (0.0, 0.030, 0.050)], [0.030, 0.028, 0.024], seg=8)
    top = 0.034 + dome
    midrib = tube([(0.0, top * .80, 0.02), (0.0, top + 0.004, 0.17), (0.0, top * .80, 0.33)], [0.008, 0.011, 0.005], seg=6)
    lid = [(lid_mesh(cap), BODY), (lid_mesh(under), LID_UNDER), (lid_mesh(stalk), SHADE)]
    if not jaw:
        lid.append((lid_mesh(midrib), SHADE))
    m.add("lid", lid, at=tilted(hinge), parent="pod")

    # The eyes: tall ink ovals with a white catchlight, each its own node at its middle so it blinks about it.
    for name, side in (("eye-l", -1), ("eye-r", 1)):
        if jaw:
            # On the front of the dome, looking forward and a little up.
            a = math.radians(side * 26)
            lift = 0.30                                   # how far up the dome, 0 its rim, 1 its top
            ring = 0.190 * math.cos(lift * math.pi / 2)
            at = (ring * math.sin(a), 0.034 + dome * math.sin(lift * math.pi / 2) + 0.002, 0.170 + ring * math.cos(a))
            lean = -22.0
        else:
            at = on_jug(side * 21, 0.215, 0.004)
            lean = 0.0
        eye = moved(ellipsoid((0.036, 0.048, 0.016)), turn=(lean, side * (26 if jaw else 21), 0))
        glint = moved(ellipsoid((0.012, 0.015, 0.006), at=(-0.010, 0.018, 0.014)), turn=(lean, side * (26 if jaw else 21), 0))
        small = moved(ellipsoid((0.006, 0.007, 0.004), at=(0.012, -0.014, 0.015)), turn=(lean, side * (26 if jaw else 21), 0))
        if jaw:
            m.add(name, [(lid_mesh(eye), INK), (lid_mesh(glint), WHITE), (lid_mesh(small), WHITE)], at=lid_point(at), parent="lid")
        else:
            m.add(name, [(tilt(eye), INK), (tilt(glint), WHITE), (tilt(small), WHITE)], at=tilted(at), parent="pod")

    # A curl of sprout on top of the cap, with one small leaf: the body wags it.
    curl_at = (0.0, top - 0.004, 0.150 if not jaw else 0.110)
    curl = tube([(0, 0, 0), (0.0, 0.07, -0.01), (0.015, 0.12, 0.02), (0.02, 0.13, 0.06), (0.01, 0.105, 0.085), (0.0, 0.085, 0.07)],
                [0.015, 0.013, 0.012, 0.011, 0.010, 0.008], seg=8)
    leaflet = moved(leaf(0.11, 0.07, 0.02), turn=(-35, 60, 0), at=(0.005, 0.075, -0.01))
    m.add("sprout", [(lid_mesh(curl), NECK), (lid_mesh(leaflet), LEAF)], at=lid_point(curl_at), parent="lid")

    # The clog's stand-in: the body hides it and hangs the carved clog in its place (`PaetePlantBody.CarveShoe`).
    # OPEN_CLOG: for the review with the lid open the stand-in is shown risen, as the body raises it when loaded.
    risen = (0.0, 0.11, 0.10) if OPEN > 0 else (0.0, 0.0, 0.0)
    clog = moved(kit.rounded((0.062, 0.050, 0.130), 0.03), turn=(-28 if OPEN > 0 else 0, 0, 0))
    m.add("slipper", [(tilt(clog), WOOD)], at=tilted((0.0 + risen[0], 0.300 + risen[1], 0.0 + risen[2])), parent="pod")

    m.write(PALETTE)


# ====================================================================== variant c: the shooter
#
# Owner, 2026-10-07, of a and b: "idk about the design overall.. u can take inspo from bellsprout or peeshooter".
# So: a round head on a thin springy stalk, and the pitcher's mouth is a SNOUT out of the front of it, a barrel with
# a rolled wine lip, which is where the clog comes from. Big eyes above the snout, cheeks beside it, a leaf crest
# lying back over its head (the `lid` node: the body raises it as a clog loads, like ears going up, and flicks it
# down on the spit), a curl of sprout, dark freckles over its crown. Leaves and root toes at its foot.

HEAD_AT, HEAD = (0.0, 0.165, 0.0), (0.200, 0.188, 0.200)
SNOUT_Y, SNOUT_UP = 0.085, 12.0


def on_head(angle, elevation, out=0.0):
    a, e = math.radians(angle), math.radians(elevation)
    d = (math.sin(a) * math.cos(e), math.sin(e), math.cos(a) * math.cos(e))
    return tuple(HEAD_AT[k] + d[k] * (HEAD[k] + out) for k in range(3))


def nod(mesh):
    """Typed level, then the whole head lifts its snout a little."""
    return moved(mesh, turn=(-SNOUT_UP, 0, 0))


def nodded(p):
    c, s = math.cos(math.radians(-SNOUT_UP)), math.sin(math.radians(-SNOUT_UP))
    return (p[0], p[1] * c - p[2] * s, p[1] * s + p[2] * c)


def clog_standin():
    """A small carved clog for the review. The game hides this node's mesh and hangs `PaeteBloomFx.BuildBakya` on it."""
    sole = kit.rounded((0.058, 0.020, 0.150), 0.02, at=(0, 0.012, 0))
    heel = kit.rounded((0.048, 0.024, 0.045), 0.012, at=(0, -0.026, -0.085))
    ball = kit.rounded((0.050, 0.024, 0.050), 0.012, at=(0, -0.026, 0.050))
    strap = tube([(-0.060, 0.020, 0.060), (-0.045, 0.062, 0.062), (0.0, 0.078, 0.064), (0.045, 0.062, 0.062), (0.060, 0.020, 0.060)], 0.017, seg=8)
    return [(sole, WOOD), (heel, WOOD_DK), (ball, WOOD_DK), (strap, LEAF_DK)]


def main_c():
    m = Model("seedling")
    m.folder = Path(arg("out", kit.ROOT / "Assets/TumbangPreso/Resources/Models/PaeteProps"))
    m.add("moss", [(ellipsoid((0.21, 0.045, 0.19), at=(0.0, 0.012, 0.0)), MOSS),
                   (ellipsoid((0.10, 0.04, 0.09), at=(0.13, 0.02, 0.07)), LEAF_DK),
                   (ellipsoid((0.09, 0.035, 0.10), at=(-0.12, 0.018, -0.06)), LEAF_DK)])
    for i, (yaw, reach, fat) in enumerate([(40, 0.31, 0.048), (128, 0.26, 0.042), (214, 0.34, 0.050), (301, 0.24, 0.040)]):
        toe = tube([(0, 0.05, 0.08), (0, 0.045, reach * .55), (0, 0.01, reach * .88), (0, -0.03, reach)], [fat, fat * .9, fat * .6, fat * .25], seg=10)
        m.add("root-%d" % i, toe, ROOT, yaw=yaw)
    still = []
    for yaw, pitch, length, width in [(118, 12, 0.44, 0.20), (186, 17, 0.50, 0.22), (250, 10, 0.40, 0.19)]:
        blade = moved(leaf(length, width, 0.05), turn=(-pitch, 0, 0))
        rib = moved(tube([(0, 0.022, 0.04), (0, 0.026, length * .5), (0, 0.016, length * .86)], [0.011, 0.009, 0.004], seg=6), turn=(-pitch, 0, 0))
        still.append((moved(blade, turn=(0, yaw, 0)), LEAF_DK))
        still.append((moved(rib, turn=(0, yaw, 0)), LEAF))
    m.add("rosette", still, at=(0.0, 0.07, 0.0))
    for i, (yaw, pitch, length, width) in enumerate([(52, 24, 0.50, 0.23), (308, 27, 0.48, 0.23)]):
        blade = moved(leaf(length, width, 0.055), turn=(-pitch, 0, 0))
        rib = moved(tube([(0, 0.024, 0.04), (0, 0.028, length * .5), (0, 0.018, length * .86)], [0.011, 0.009, 0.004], seg=6), turn=(-pitch, 0, 0))
        m.add("arm-%d" % i, [(blade, LEAF), (rib, LEAF_DK)], at=(0.0, 0.07, 0.0), yaw=yaw)

    # The stalk: thin and springy, an S, with a little collar of two leaves under the head.
    neck = tube([(0.0, 0.03, -0.02), (0.035, 0.18, -0.07), (0.015, 0.34, -0.09), (-0.02, 0.46, -0.05), (0.0, 0.545, 0.0)],
                [0.050, 0.040, 0.036, 0.036, 0.042], seg=12)
    collar = [(moved(leaf(0.16, 0.10, 0.03), turn=(-35, yaw, 0), at=(0.0, 0.50, -0.02)), LEAF) for yaw in (115, 245)]
    m.add("stem", [(neck, NECK), (ellipsoid((0.085, 0.03, 0.085), at=(0.0, 0.085, -0.03)), LEAF_DK)] + collar)

    # The head and its snout.
    pod = [(nod(ellipsoid(HEAD, at=HEAD_AT, seg=24, rings=16)), BODY)]
    barrel = moved(lathe([(0.100, 0.06), (0.100, 0.20), (0.106, 0.285), (0.122, 0.330)], seg=22), turn=(90, 0, 0), at=(0.0, SNOUT_Y, 0.0))
    throat = moved(ellipsoid((0.102, 0.102, 0.020), at=(0.0, SNOUT_Y, 0.318)))
    lip = moved(loop(0.122, 0.0, 0.030), turn=(90, 0, 0), at=(0.0, SNOUT_Y, 0.332))
    pod += [(nod(barrel), BODY), (nod(throat), INSIDE), (nod(lip), LIP)]
    # A paler chin under the snout, flush with the head.
    pod.append((nod(ellipsoid((0.110, 0.050, 0.080), at=(0.0, 0.008, 0.070))), BELLY))
    for side in (-1, 1):
        pod.append((nod(moved(ellipsoid((0.036, 0.022, 0.012)), turn=(8, side * 62, 0), at=on_head(side * 58, 2, 0.002))), BLUSH))
    # Freckles over the crown and the back of its head, each typed: angle round, elevation, size.
    for angle, elev, size in [(150, 38, 0.030), (205, 44, 0.024), (178, 16, 0.034), (118, 14, 0.022), (238, 10, 0.026), (95, 46, 0.018), (262, 40, 0.020)]:
        pod.append((nod(moved(ellipsoid((size, size * .8, 0.008)), turn=(-elev, angle, 0), at=on_head(angle, elev, 0.001))), SHADE))
    m.add("pod", pod, at=POD_AT, parent="stem")

    for name, side in (("eye-l", -1), ("eye-r", 1)):
        angle, elev = side * 33, 36
        at = on_head(angle, elev, 0.004)
        turn = (-elev, angle, 0)
        eye = moved(ellipsoid((0.048, 0.064, 0.016)), turn=turn)
        glint = moved(ellipsoid((0.016, 0.021, 0.006), at=(-0.013, 0.023, 0.014)), turn=turn)
        small = moved(ellipsoid((0.007, 0.008, 0.004), at=(0.014, -0.017, 0.015)), turn=turn)
        m.add(name, [(nod(eye), INK), (nod(glint), WHITE), (nod(small), WHITE)], at=nodded(at), parent="pod")

    # The crest: a broad leaf hinged at the top of the back of its head, lying back along it. A POSITIVE pitch raises it.
    hinge = on_head(180, 62, -0.01)
    raise_ = OPEN
    crest = moved(leaf(0.34, 0.24, 0.05), turn=(38 - raise_, 180, 0))
    rib = moved(tube([(0, 0.022, 0.03), (0, 0.027, 0.17), (0, 0.018, 0.30)], [0.011, 0.010, 0.004], seg=6), turn=(38 - raise_, 180, 0))
    m.add("lid", [(nod(crest), LEAF), (nod(rib), LEAF_DK)], at=nodded(hinge), parent="pod")

    curl_at = on_head(0, 84, -0.006)
    curl = tube([(0, 0, 0), (0.0, 0.07, -0.01), (0.015, 0.12, 0.02), (0.02, 0.13, 0.06), (0.01, 0.105, 0.085), (0.0, 0.085, 0.07)],
                [0.016, 0.014, 0.013, 0.012, 0.010, 0.008], seg=8)
    leaflet = moved(leaf(0.11, 0.07, 0.02), turn=(-35, 60, 0), at=(0.005, 0.075, -0.01))
    m.add("sprout", [(nod(curl), NECK), (nod(leaflet), LEAF)], at=nodded(curl_at), parent="pod")

    # The clog, down the barrel, toe out. For the review with --open it is shown pushed out to the lip, as loaded.
    out = 0.20 if OPEN > 0 else 0.0
    m.add("slipper", [(nod(mesh), slot) for mesh, slot in clog_standin()], at=nodded((0.0, SNOUT_Y, 0.10 + out)), parent="pod")
    m.write(PALETTE)


# ====================================================================== variant d: the pitcher, kept a pitcher
#
# Owner, 2026-10-07, of the shooter (c): "oh no.. i mean keep it as a pitcher plant design..". So the reference
# (Bellsprout, a Peashooter) is for how SIMPLE and friendly it is, not for its shape. This is a Nepenthes pitcher
# anyone would name: a tall jug, belly low, narrow neck, a flared mouth with a thick rolled lip, OPEN, and its lid
# standing up behind the mouth like a hood, as a real one's does. Wine speckles up its neck, veins down its back.
# On it, a small plain face: two dot eyes and cheeks on the belly. On a thin springy stalk, leaves and root toes.

# ---------------------------------------------------------------- the paint (owner: "do you plan on texturing this?")
#
# The redesigned cast wear painted atlases, so this does too: `seedling-atlas.png` beside the model, 2048 square. Its
# UPPER half is paint; the lower half is left for the flat palette cells (eyes, cheeks, throat, moss). Every mark below
# is typed: where it is and how big, none stamped by a loop over random numbers.

ATLAS = 2048
PAD = 0.004
R_JUG, R_LIP, R_LEAF, R_HOOD = (0.0, 0.0, 0.5, 0.25), (0.5, 0.0, 0.625, 0.25), (0.625, 0.0, 0.8125, 0.25), (0.8125, 0.0, 1.0, 0.25)
R_STALK, R_ROOT, R_UNDER = (0.0, 0.25, 0.125, 0.5), (0.125, 0.25, 0.25, 0.5), (0.25, 0.25, 0.5, 0.5)


def inset(rect):
    return (rect[0] + PAD, rect[1] + PAD, rect[2] - PAD, rect[3] - PAD)


def hexc(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32)


def smooth01(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def paint_atlas(path):
    from PIL import Image, ImageDraw, ImageFilter
    img = np.zeros((ATLAS, ATLAS, 3), np.float32)
    img[:] = hexc(PALETTE[BODY])

    def box(rect):
        return int(rect[0] * ATLAS), int(rect[1] * ATLAS), int(rect[2] * ATLAS), int(rect[3] * ATLAS)

    def grid(rect):
        """u across (0 to 1) and t up (0 at the bottom of the piece, 1 at its top) for every pixel of `rect`."""
        x0, y0, x1, y1 = box(rect)
        u = (np.arange(x0, x1) + .5 - x0) / (x1 - x0)
        t = 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0)
        return np.meshgrid(u, t), (x0, y0, x1, y1)

    def mix(base, colour, weight):
        return base * (1 - weight[..., None]) + hexc(colour) * weight[..., None]

    # ---- the jug: deep green at its foot, lime up the belly, flushed wine at the neck. Its front is u = 0.25.
    (u, t), (x0, y0, x1, y1) = grid(R_JUG)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#7DB434")
    c = mix(c, "#A8D843", smooth01(0.02, 0.45, t))
    c = mix(c, "#B9E052", smooth01(0.40, 0.70, t) * 0.6)
    c = mix(c, "#C8425E", smooth01(0.70, 1.00, t) * 0.78)
    front = np.cos((u - 0.25) * 2 * np.pi)                       # 1 at the front, -1 at the back
    c = mix(c, "#D6F283", np.exp(-(((u - 0.25) / 0.10) ** 2 + ((t - 0.30) / 0.20) ** 2)) * 0.55)   # the pale belly
    c = mix(c, "#4E8A2C", np.clip(-front, 0, 1) * 0.28 * (1 - smooth01(0.6, 0.9, t)))             # its back is deeper
    c = mix(c, "#5C1F33", smooth01(0.93, 1.0, t) * 0.45)                                           # shade under the lip
    img[y0:y1, x0:x1] = c
    # ---- the lip: rolled, wine, ribbed across (a pitcher's peristome). t runs round the mouth.
    (u, t), (x0, y0, x1, y1) = grid(R_LIP)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#B02A45")
    c = mix(c, "#E0566E", (0.5 + 0.5 * np.cos((u - 0.30) * 2 * np.pi)) ** 3 * 0.75)               # a glossy band along the roll
    c = mix(c, "#7A1830", (0.5 + 0.5 * np.cos(t * 2 * np.pi * 33)) ** 6 * 0.55)                    # the ribs
    img[y0:y1, x0:x1] = c
    # ---- a leaf: both faces. Stem at the bottom, tip at the top; a face's midrib is at u = 0.25 and 0.75.
    for rect, lo, hi, vein, edge in ((R_LEAF, "#2F7A2A", "#5DB03A", "#A5DC55", "#235E22"), (R_HOOD, "#8FC63B", "#B4DE4C", "#5F9330", "#C8425E")):
        (u, t), (x0, y0, x1, y1) = grid(rect)
        half = np.abs(((u * 2) % 1.0) - 0.5) * 2                 # 0 on a midrib, 1 at the leaf's edge
        c = np.zeros(u.shape + (3,), np.float32) + hexc(lo)
        c = mix(c, hi, smooth01(0.0, 0.9, t) * 0.85)
        c = mix(c, edge, smooth01(0.72, 1.0, half) * (0.75 if rect is R_HOOD else 0.55))
        side = np.abs(((t * 7.0 - half * 1.6) % 1.0) - 0.5) * 2  # side veins, swept up and out from the midrib
        c = mix(c, vein, (1 - smooth01(0.0, 0.16, side)) * (1 - smooth01(0.55, 0.9, half)) * 0.5)
        c = mix(c, vein, (1 - smooth01(0.0, 0.07, half)) * 0.9)  # the midrib
        img[y0:y1, x0:x1] = c
    # ---- the lid's underside: wine, darker veins, paler at its middle.
    (u, t), (x0, y0, x1, y1) = grid(R_UNDER)
    half = np.abs(((u * 2) % 1.0) - 0.5) * 2
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#D9596B")
    c = mix(c, "#F08A93", (1 - smooth01(0.0, 0.6, half)) * 0.5)
    side = np.abs(((t * 6.0 - half * 1.4) % 1.0) - 0.5) * 2
    c = mix(c, "#A12C48", (1 - smooth01(0.0, 0.18, side)) * 0.45)
    c = mix(c, "#A12C48", (1 - smooth01(0.0, 0.08, half)) * 0.8)
    img[y0:y1, x0:x1] = c
    # ---- the stalk: deeper at the foot, a pale stripe up it, knuckles.
    (u, t), (x0, y0, x1, y1) = grid(R_STALK)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#5E9A2E")
    c = mix(c, "#93C940", smooth01(0.0, 1.0, t) * 0.8)
    c = mix(c, "#B5E05A", (0.5 + 0.5 * np.cos((u - 0.25) * 2 * np.pi)) ** 4 * 0.45)
    for at in (0.20, 0.42, 0.63, 0.83):
        c = mix(c, "#3F7524", np.exp(-((t - at) / 0.012) ** 2) * 0.8)
        c = mix(c, "#B5E05A", np.exp(-((t - at - 0.03) / 0.014) ** 2) * 0.45)
    img[y0:y1, x0:x1] = c
    # ---- a root toe: pale where it leaves the plant, earthy at its tip, ringed.
    (u, t), (x0, y0, x1, y1) = grid(R_ROOT)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#D2B988")
    c = mix(c, "#8B6B42", smooth01(0.35, 1.0, t) * 0.85)
    for at in (0.28, 0.44, 0.58, 0.70, 0.80):
        c = mix(c, "#6E5230", np.exp(-((t - at) / 0.010) ** 2) * 0.55)
    img[y0:y1, x0:x1] = c

    picture = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB")
    d = ImageDraw.Draw(picture, "RGBA")
    x0, y0, x1, y1 = box(R_JUG)
    w, h = x1 - x0, y1 - y0

    def at(u, t):
        return x0 + u * w, y0 + (1 - t) * h

    # Veins down the flanks and the back, each typed: where round, from how high to how low, how it wanders, how wide.
    for u_at, top, bottom, wander, width in [(0.455, 0.66, 0.05, 0.012, 7), (0.560, 0.70, 0.03, -0.010, 8), (0.655, 0.72, 0.02, 0.008, 9),
                                             (0.750, 0.73, 0.02, -0.006, 9), (0.845, 0.72, 0.02, 0.010, 9), (0.940, 0.70, 0.03, -0.012, 8),
                                             (0.045, 0.66, 0.05, 0.010, 7)]:
        pts = [at(u_at + wander * math.sin(k / 12 * math.pi * 1.5), top + (bottom - top) * k / 12) for k in range(13)]
        for k in range(12):
            taper = math.sin((k + .5) / 12 * math.pi) ** 0.5
            d.line([pts[k], pts[k + 1]], fill=(74, 132, 44, 205), width=max(2, int(width * taper)))
    # Wine speckles: thick and big at the neck, thinning and paling down onto the shoulders. None on the face.
    for u_at, t_at, r, deep in [(0.03, 0.90, 13, 1), (0.09, 0.84, 9, 1), (0.14, 0.92, 11, 1), (0.20, 0.86, 8, 1), (0.25, 0.93, 10, 1), (0.31, 0.85, 8, 1),
                                (0.36, 0.91, 12, 1), (0.42, 0.83, 9, 1), (0.47, 0.90, 13, 1), (0.53, 0.82, 10, 1), (0.59, 0.91, 14, 1), (0.65, 0.84, 9, 1),
                                (0.70, 0.92, 12, 1), (0.76, 0.83, 11, 1), (0.82, 0.90, 13, 1), (0.88, 0.84, 9, 1), (0.94, 0.92, 11, 1), (0.98, 0.80, 8, 1),
                                (0.06, 0.74, 7, 0), (0.44, 0.73, 8, 0), (0.50, 0.66, 6, 0), (0.57, 0.74, 8, 0), (0.63, 0.63, 6, 0), (0.68, 0.75, 9, 0),
                                (0.74, 0.66, 7, 0), (0.80, 0.74, 8, 0), (0.86, 0.64, 6, 0), (0.92, 0.75, 8, 0), (0.99, 0.68, 6, 0), (0.71, 0.55, 5, 0),
                                (0.79, 0.52, 5, 0), (0.60, 0.50, 4, 0)]:
        cx, cy = at(u_at, t_at)
        d.ellipse([cx - r, cy - r * .85, cx + r, cy + r * .85], fill=(132, 22, 52, 235) if deep else (190, 60, 84, 215))
    # Speckles on the lid's upper face, toward its tip.
    hx0, hy0, hx1, hy1 = box(R_HOOD)
    for u_at, t_at, r in [(0.16, 0.80, 8), (0.34, 0.74, 7), (0.20, 0.62, 6), (0.31, 0.56, 5), (0.25, 0.90, 7),
                          (0.66, 0.80, 8), (0.84, 0.74, 7), (0.70, 0.62, 6), (0.81, 0.56, 5), (0.75, 0.90, 7)]:
        cx, cy = hx0 + u_at * (hx1 - hx0), hy0 + (1 - t_at) * (hy1 - hy0)
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(176, 42, 69, 225))
    picture = picture.filter(ImageFilter.GaussianBlur(0.6))
    picture.save(path)
    return path


def painted(mesh, rect, cols, along="rows", axis=1):
    return (mesh, kit.grid_uv(mesh, inset(rect), cols, along, axis))


def turned(piece, **how):
    return (moved(piece[0], **how), piece[1])


def leaf_painted(length, width, thick, rect):
    """`leaf`, painted: the same blade along +z, its UVs laid before it is turned."""
    prof = [(0.0, 0.0), (0.42, 0.10), (0.92, 0.36), (1.0, 0.55), (0.78, 0.78), (0.30, 0.94), (0.0, 1.0)]
    blade = lathe([(r * width * .5, y * length) for r, y in prof], seg=14, squash_z=thick / width)
    return (moved(blade, turn=(90, 0, 0)), kit.grid_uv(blade, inset(rect), 14, "axis", 1))


def main_d():
    m = Model("seedling")
    m.folder = Path(arg("out", kit.ROOT / "Assets/TumbangPreso/Resources/Models/PaeteProps"))
    m.texture = "seedling-atlas.png"
    m.folder.mkdir(parents=True, exist_ok=True)
    paint_atlas(m.folder / m.texture)

    m.add("moss", [(ellipsoid((0.21, 0.045, 0.19), at=(0.0, 0.012, 0.0)), MOSS),
                   (ellipsoid((0.10, 0.04, 0.09), at=(0.13, 0.02, 0.07)), LEAF_DK),
                   (ellipsoid((0.09, 0.035, 0.10), at=(-0.12, 0.018, -0.06)), LEAF_DK)])
    for i, (yaw, reach, fat) in enumerate([(40, 0.31, 0.048), (128, 0.26, 0.042), (214, 0.34, 0.050), (301, 0.24, 0.040)]):
        toe = tube([(0, 0.05, 0.08), (0, 0.045, reach * .55), (0, 0.01, reach * .88), (0, -0.03, reach)], [fat, fat * .9, fat * .6, fat * .25], seg=10)
        m.add("root-%d" % i, [painted(toe, R_ROOT, 10)], yaw=yaw)
    still = []
    for yaw, pitch, length, width in [(118, 12, 0.44, 0.20), (186, 17, 0.50, 0.22), (250, 10, 0.40, 0.19)]:
        still.append(turned(turned(leaf_painted(length, width, 0.05, R_LEAF), turn=(-pitch, 0, 0)), turn=(0, yaw, 0)))
    m.add("rosette", still, at=(0.0, 0.07, 0.0))
    for i, (yaw, pitch, length, width) in enumerate([(52, 24, 0.50, 0.23), (308, 27, 0.48, 0.23)]):
        m.add("arm-%d" % i, [turned(leaf_painted(length, width, 0.055, R_LEAF), turn=(-pitch, 0, 0))], at=(0.0, 0.07, 0.0), yaw=yaw)

    neck = tube([(0.0, 0.03, -0.02), (0.035, 0.18, -0.07), (0.015, 0.34, -0.09), (-0.02, 0.46, -0.05), (0.0, 0.545, 0.02)],
                [0.050, 0.040, 0.036, 0.036, 0.044], seg=12)
    m.add("stem", [painted(neck, R_STALK, 12), (ellipsoid((0.085, 0.03, 0.085), at=(0.0, 0.085, -0.03)), LEAF_DK)])

    # The jug. `lathe` starts its round at +x, so it is turned a quarter for the paint's front (u = 0.25) to face +z.
    jug = lathe(JUG, seg=28)
    pod = [(tilt(jug), kit.grid_uv(jug, inset(R_JUG), 28, "axis", 1))]
    # The throat, dark and deep, and the thick rolled lip round it.
    pod.append((tilt(ellipsoid((MOUTH_R - 0.014, 0.026, MOUTH_R - 0.014), at=(0.0, MOUTH_Y - 0.006, 0.0))), INSIDE))
    lip = loop(MOUTH_R, MOUTH_Y, 0.040, count=40)
    pod.append((tilt(lip), kit.grid_uv(lip, inset(R_LIP), 10)))
    for side in (-1, 1):
        pod.append((tilt(moved(ellipsoid((0.030, 0.018, 0.010)), turn=(0, side * 46, 0), at=on_jug(side * 46, 0.115, 0.002))), BLUSH))
    m.add("pod", pod, at=POD_AT, parent="stem")

    for name, side in (("eye-l", -1), ("eye-r", 1)):
        at = on_jug(side * 22, 0.170, 0.003)
        turn = (0, side * 22, 0)
        eye = moved(ellipsoid((0.030, 0.038, 0.014)), turn=turn)
        glint = moved(ellipsoid((0.010, 0.012, 0.005), at=(-0.008, 0.014, 0.012)), turn=turn)
        m.add(name, [(tilt(eye), INK), (tilt(glint), WHITE)], at=tilted(at), parent="pod")

    # The lid: a leaf hood on a short stalk at the back of the rim. It rests standing half open over the mouth
    # (REST degrees are baked in, so the body's own opening adds to that), with a spur behind the hinge.
    REST = 28.0
    hinge = (0.0, MOUTH_Y + 0.020, -MOUTH_R - 0.005)

    def hood(mesh):
        return tilt(moved(mesh, turn=(-(REST + OPEN), 0, 0)))

    def hood_point(p):
        c, s2 = math.cos(math.radians(-(REST + OPEN))), math.sin(math.radians(-(REST + OPEN)))
        return tilted((p[0], p[1] * c - p[2] * s2, p[1] * s2 + p[2] * c))

    blade = turned(leaf_painted(0.38, 0.32, 0.055, R_HOOD), at=(0.0, 0.020, 0.0))
    under = turned(leaf_painted(0.34, 0.27, 0.030, R_UNDER), at=(0.0, 0.002, 0.02))
    stalk = tube([(0.0, -0.040, 0.000), (0.0, 0.000, 0.000), (0.0, 0.022, 0.040)], [0.028, 0.026, 0.022], seg=8)
    spur = tube([(0.0, 0.010, -0.010), (0.0, 0.030, -0.050), (0.0, 0.020, -0.085)], [0.014, 0.010, 0.003], seg=6)
    m.add("lid", [(hood(blade[0]), blade[1]), (hood(under[0]), under[1]), (hood(stalk), SHADE), (hood(spur), SHADE)],
          at=tilted(hinge), parent="pod")
    # No sprout on this one: a speck so the body still finds the node.
    m.add("sprout", [(hood(ellipsoid((0.004, 0.004, 0.004))), SHADE)], at=hood_point((0.0, 0.05, 0.10)), parent="lid")

    # The clog grows down in the belly, out of sight, and the body raises it to the mouth, toe up.
    risen = (0.0, 0.34, 0.05) if OPEN > 0 else (0.0, 0.0, 0.0)
    clog = [(tilt(moved(mesh, turn=(-62 if OPEN > 0 else -90, 0, 0))), slot) for mesh, slot in clog_standin()]
    m.add("slipper", clog, at=tilted((0.0, 0.125 + risen[1], 0.0 + risen[2])), parent="pod")
    m.write(PALETTE)


if __name__ == "__main__":
    if VARIANT == "d":
        main_d()
        raise SystemExit
    if VARIANT == "c":
        main_c()
        raise SystemExit
    main()
