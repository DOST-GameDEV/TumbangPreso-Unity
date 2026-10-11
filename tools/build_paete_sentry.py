"""MAKILING'S EMBRACE's guardian tree, remodelled in the redesigned cast's dress (`Visual.PaeteSentryBody`).

Owner, 2026-10-07: "by 'rework' im thinking of remodelling, textures, and vfx.. not just vfx", "try the cutesy
character for the plants", then "do makiling's embrace next". The pitcher and the rattan went first
(`tools/build_paete_bloom.py`, `tools/build_paete_rattan.py`); this is the same treatment for the ultimate's tree: the
hand companions' kit, round chunky forms, a painted atlas, an ink line.

⚠️ WHAT IS KEPT FROM THE FIRST TREE, because the owner asked for each by name in September and has not taken it back:
  * a WRUNG ROPE of a trunk ("woven tree branches"): now one smooth trunk with three fat cords wound up it and the
    twist painted into its bark;
  * a POINTED crown with a glow coming from within, a few leaves at the edge and NO green blob ("it makes it look
    goofy"): a leader spire and five rising branches, bare wood but for a few leaves and three sampaguita (her flower);
  * its eyes are two HOLLOWS in the wood with a light in each, set INTO the trunk, not floating ("Js make 2 holes",
    "isnt embedded anywhere");
  * six claw roots that go INTO the ground.
What is new is its dress and its face: an old, STERN guardian (the owner: "should be sterner cuz this is an ultimate
ability of essentialy a tree guardian"), heavy-browed, slit-eyed, with mossy shoulders and a mossy foot.

  py -3 tools/build_paete_sentry.py [--out=folder]
  blender -b --python tools/review_hand_companion.py -- --hero=sentry --file=<folder>/sentry.glb --atlas=<folder>/sentry-atlas.png --version=v1 --size=7 --centre=0,2.7,0

Writes Assets/TumbangPreso/Resources/Models/PaeteProps/sentry.glb and sentry-atlas.png (the first model is kept at
ArtSource/paete/props-pre-rework/sentry.glb). The node NAMES and PLACES are the body's contract, unchanged: `trunk`
(origin on the ground), `face` under it at 2.40 and `eyes` under that (scaled in y to blink), `crown` under the trunk
at 3.44 with `claw-0..4` (the branches, each turned so +z is outward; pitched about x to fold and droop), and
`buttress-0..5` under the root at 0.34 out (the claw roots, each turned so +z is outward). 5.39 m to the tip.

Metres, y up, +z its face. The sixteen colours are `PaeteSentryBody.Palette`, same order.
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import Model, ellipsoid, lathe, moved, tube  # noqa: E402
from build_paete_bloom import arg, hexc, inset, leaf_painted, painted, smooth01, turned  # noqa: E402

PALETTE = ["#4E6E1E", "#34501A", "#557A2E", "#3A5E26", "#4C6E20", "#6E4A2C", "#4A3320", "#5C7A2E",
           "#1E140C", "#140C06", "#D8FF6A", "#86C83A", "#E8C24A", "#5F4128", "#3A2616", "#7C5836"]
MOSS, MOSS_DK, LEAF, LEAF_DK, VINE, HEART, ROOT, MOSS_LIT, INK, SOCKET, EYE, EYE_GLOW, GOLD, BARK, BARK_DK, BARK_LIT = range(16)

ATLAS = 2048
R_TRUNK, R_ROOT, R_BRANCH, R_LEAF, R_PETAL = (0.0, 0.0, 0.5, 0.25), (0.5, 0.0, 0.625, 0.25), (0.625, 0.0, 0.75, 0.25), (0.75, 0.0, 0.875, 0.25), (0.875, 0.0, 1.0, 0.25)
R_MOSS = (0.0, 0.25, 0.25, 0.5)

# The trunk, turned about its own up: (radius, height). A flared foot, a waist, a swell where its face is, a shoulder.
# ⚠️ THE SHAPE (owner, 2026-10-07, of a first trunk that was a straight post with a tuft of sticks on it: "can you fix
# the shape of the actual tree"). A tree has a stance: a wide gripping foot, a narrow waist, a heavy head where its
# face is, a lean (`bend`), boughs that spread like antlers, and leaves at their ends.
# ⚠️ NO SWOLLEN HEAD (owner, of a trunk that pinched to 0.40 at its waist and swelled to 0.58 where its face is: "the
# swelling trunk looks kinda weird"). It tapers from its foot now and only fills out a little for the face.
TRUNK = [(0.86, -0.10), (0.78, 0.08), (0.58, 0.42), (0.48, 0.95), (0.445, 1.50), (0.45, 2.00), (0.475, 2.40), (0.475, 2.70), (0.45, 3.00), (0.41, 3.28), (0.36, 3.50)]


def bend(y):
    """How far the trunk leans off its axis at height y (x, z): a slow S, upright again where the crown sits."""
    f = max(0.0, min(1.0, y / CROWN_Y))
    return (0.16 * math.sin(f * 2 * math.pi), -0.10 * math.sin(f * math.pi))


def bent(mesh):
    """A piece typed on the straight trunk, leant with it."""
    pos = mesh[0].copy()
    for i in range(len(pos)):
        dx, dz = bend(float(pos[i, 1]))
        pos[i, 0] += dx; pos[i, 2] += dz
    return (pos, mesh[1], mesh[2])
FACE_Y, CROWN_Y = 2.40, 3.44


def trunk_radius(y):
    for (r0, y0), (r1, y1) in zip(TRUNK, TRUNK[1:]):
        if y0 <= y <= y1:
            return r0 + (r1 - r0) * (y - y0) / (y1 - y0)
    return TRUNK[-1][0] if y > TRUNK[-1][1] else TRUNK[0][0]


def on_trunk(angle, y, out=0.0):
    a = math.radians(angle)
    r = trunk_radius(y) + out
    dx, dz = bend(y)
    return (r * math.sin(a) + dx, y, r * math.cos(a) + dz)


def paint_atlas(path):
    from PIL import Image, ImageDraw, ImageFilter
    img = np.zeros((ATLAS, ATLAS, 3), np.float32)
    img[:] = hexc(PALETTE[BARK])

    def grid(rect):
        x0, y0, x1, y1 = (int(v * ATLAS) for v in rect)
        u = (np.arange(x0, x1) + .5 - x0) / (x1 - x0)
        t = 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0)
        return np.meshgrid(u, t), (x0, y0, x1, y1)

    def mix(base, colour, weight):
        return base * (1 - weight[..., None]) + hexc(colour) * weight[..., None]

    def bark(u, t, twist, lanes):
        """Old bark: dark at the foot, warmer above, wrung: grooves that climb as they go round, a pale ridge between them."""
        c = np.zeros(u.shape + (3,), np.float32) + hexc("#3E2A18")
        c = mix(c, "#6B4A2C", smooth01(0.02, 0.5, t))
        c = mix(c, "#80603A", smooth01(0.5, 1.0, t) * 0.55)
        lane = (u * lanes + t * twist) % 1.0
        c = mix(c, "#A07A48", (0.5 + 0.5 * np.cos((lane - 0.5) * 2 * np.pi)) ** 3 * 0.38)       # the ridge
        c = mix(c, "#24160C", np.exp(-((np.minimum(lane, 1 - lane)) / 0.055) ** 2) * 0.85)       # the groove
        fine = (u * lanes * 6 + t * twist * 6) % 1.0
        c = mix(c, "#2E1C10", np.exp(-((np.minimum(fine, 1 - fine)) / 0.10) ** 2) * 0.16)        # the grain
        return c

    # ---- the trunk. Its face is u = 0.25.
    (u, t), (x0, y0, x1, y1) = grid(R_TRUNK)
    c = bark(u, t, 1.9, 5)
    # Moss climbing its foot in tongues, and a cap of it on its shoulders.
    foot = smooth01(0.16 + 0.05 * np.sin(u * 2 * np.pi * 7) + 0.03 * np.sin(u * 2 * np.pi * 13 + 1), 0.0, t)
    c = mix(c, "#4E6E1E", foot * 0.9)
    c = mix(c, "#7C9A34", foot * (0.5 + 0.5 * np.sin(u * 2 * np.pi * 23)) * 0.35)
    top = smooth01(0.90 + 0.03 * np.sin(u * 2 * np.pi * 9), 1.0, t)
    c = mix(c, "#557A2E", top * 0.85)
    # Its face is a smooth pale swelling in the bark, so the eyes sit on something.
    c = mix(c, "#8A6A40", np.exp(-(((u - 0.25) / 0.085) ** 2 + ((t - 0.70) / 0.085) ** 2)) * 0.75)
    img[y0:y1, x0:x1] = c
    # ---- a root: the same bark, straighter, earth-dark where it goes in.
    (u, t), (x0, y0, x1, y1) = grid(R_ROOT)
    c = bark(u, t, 0.6, 3)
    c = mix(c, "#2A1A0E", smooth01(0.70, 1.0, t) * 0.8)
    img[y0:y1, x0:x1] = c
    # ---- a branch: paler and smoother toward its tip.
    (u, t), (x0, y0, x1, y1) = grid(R_BRANCH)
    c = bark(u, t, 1.2, 3)
    c = mix(c, "#9A7A4C", smooth01(0.4, 1.0, t) * 0.55)
    img[y0:y1, x0:x1] = c
    # ---- a leaf, both faces.
    (u, t), (x0, y0, x1, y1) = grid(R_LEAF)
    half = np.abs(((u * 2) % 1.0) - 0.5) * 2
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#3A5E26")
    c = mix(c, "#6F9A38", smooth01(0.0, 0.9, t) * 0.85)
    c = mix(c, "#24401A", smooth01(0.72, 1.0, half) * 0.55)
    side = np.abs(((t * 7.0 - half * 1.6) % 1.0) - 0.5) * 2
    c = mix(c, "#A5C85A", (1 - smooth01(0.0, 0.16, side)) * (1 - smooth01(0.55, 0.9, half)) * 0.45)
    c = mix(c, "#C2DC74", (1 - smooth01(0.0, 0.07, half)) * 0.9)
    img[y0:y1, x0:x1] = c
    # ---- a sampaguita petal: white, cream toward the heart, a faint line down it.
    (u, t), (x0, y0, x1, y1) = grid(R_PETAL)
    half = np.abs(((u * 2) % 1.0) - 0.5) * 2
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#FFFDF6")
    c = mix(c, "#F4E7B4", (1 - smooth01(0.0, 0.45, t)) * 0.8)
    c = mix(c, "#E3D9BE", (1 - smooth01(0.0, 0.06, half)) * 0.5)
    img[y0:y1, x0:x1] = c
    # ---- moss: deep, with paler tufts.
    (u, t), (x0, y0, x1, y1) = grid(R_MOSS)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#3E5C1C")
    c = mix(c, "#6E922E", (0.5 + 0.5 * np.sin(u * 2 * np.pi * 9 + np.sin(t * 17) * 2)) * (0.5 + 0.5 * np.sin(t * 2 * np.pi * 6)) * 0.7)
    c = mix(c, "#8FB23E", smooth01(0.6, 1.0, t) * 0.4)
    img[y0:y1, x0:x1] = c

    picture = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB")
    d = ImageDraw.Draw(picture, "RGBA")
    x0, y0, x1, y1 = (int(v * ATLAS) for v in R_TRUNK)
    w, h = x1 - x0, y1 - y0
    # Knots in the bark, each typed: where round, how high, how big. None on its face.
    for u_at, t_at, r in [(0.52, 0.40, 16), (0.70, 0.62, 13), (0.88, 0.30, 15), (0.60, 0.80, 11), (0.05, 0.50, 14), (0.44, 0.22, 12), (0.80, 0.50, 10)]:
        cx, cy = x0 + u_at * w, y0 + (1 - t_at) * h
        d.ellipse([cx - r, cy - r * 1.4, cx + r, cy + r * 1.4], fill=(34, 20, 10, 235))
        d.ellipse([cx - r * .55, cy - r * .8, cx + r * .55, cy + r * .8], fill=(96, 68, 40, 235))
        d.ellipse([cx - r * .25, cy - r * .4, cx + r * .25, cy + r * .4], fill=(40, 24, 12, 240))
    # Pale lichen, each typed.
    for u_at, t_at, r in [(0.57, 0.52, 9), (0.60, 0.55, 6), (0.92, 0.66, 8), (0.76, 0.36, 7), (0.02, 0.72, 8), (0.47, 0.60, 6), (0.66, 0.26, 7), (0.85, 0.78, 6)]:
        cx, cy = x0 + u_at * w, y0 + (1 - t_at) * h
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(176, 190, 132, 210))
    picture = picture.filter(ImageFilter.GaussianBlur(0.6))
    picture.save(path)


def blossom(at, turn, size):
    """A sampaguita: five rounded white petals round a gold heart, facing up before it is tipped.

    ⚠️ ONE SURFACE, NOT FIVE PETALS STUCK TOGETHER. The first was five leaf shapes crossing at the heart, and the ink
    round each lay over its neighbours: from behind the flower was a black speck. This is one shallow cup whose rim is
    pushed in and out five times, so there is nothing for the ink to cross.
    """
    cup = lathe([(0.03, 0.0), (0.46, 0.08), (0.86, 0.24), (1.0, 0.38), (0.80, 0.43), (0.32, 0.30), (0.0, 0.24)], seg=30)
    uv = kit.grid_uv(cup, inset(R_PETAL), 30, "axis", 1)
    pos = cup[0].copy()
    for i in range(len(pos)):
        lobe = 1.0 + 0.24 * math.cos(5 * math.atan2(float(pos[i, 2]), float(pos[i, 0])))
        pos[i, 0] *= lobe; pos[i, 2] *= lobe
    pos *= size * 0.62
    tip = (turn[0] + 90, turn[1], turn[2])
    return [((moved((pos, cup[1], cup[2]), turn=tip, at=at)), uv),
            (moved(ellipsoid((size * .17, size * .12, size * .17), at=(0, size * .20, 0)), turn=tip, at=at), GOLD)]


def main():
    m = Model("sentry")
    m.folder = Path(arg("out", kit.ROOT / "Assets/TumbangPreso/Resources/Models/PaeteProps"))
    m.texture = "sentry-atlas.png"
    m.folder.mkdir(parents=True, exist_ok=True)
    paint_atlas(m.folder / m.texture)

    # ---- the trunk, and the three fat cords wound up it (the wrung rope).
    post = lathe(TRUNK, seg=32)
    trunk = [(bent(post), kit.grid_uv(post, inset(R_TRUNK), 32, "axis", 1))]
    for start, turns, girth, top in [(40, 0.62, 0.150, 3.30), (165, 0.58, 0.135, 3.20), (285, 0.66, 0.145, 3.35)]:
        pts, radii = [], []
        for k in range(15):
            f = k / 14
            y = 0.05 + (top - 0.05) * f
            angle = start + 360 * turns * f
            # A cord dives under the bark where its face is, so nothing crosses its eyes.
            near = math.cos(math.radians(angle))
            sink = 0.10 * max(0.0, near) * math.exp(-((y - FACE_Y) / 0.55) ** 2)
            pts.append(on_trunk(angle, y, 0.02 - sink))
            radii.append(girth * (1.0 - 0.45 * f) * (0.55 + 0.45 * math.sin(f * math.pi) ** 0.5))
        cord = tube(pts, radii, seg=10)
        trunk.append(painted(cord, R_ROOT, 10))
    # Moss on its shoulders, two caps, each its own size.
    for angle, y, size in [(130, 3.34, (0.26, 0.11, 0.22)), (300, 3.26, (0.22, 0.09, 0.20)), (35, 3.40, (0.16, 0.07, 0.15))]:
        cap = ellipsoid(size, seg=14, rings=8)
        trunk.append(turned(painted(cap, R_MOSS, 14), turn=(0, angle, 0), at=on_trunk(angle, y, -0.06)))
    m.add("trunk", trunk)

    # ---- its face: two hollows set into the bark under one heavy brow, and a knot of a nose. No mouth: it does not speak.
    face = []

    def on_face(mesh, rect, cols):
        """A piece typed on the trunk, moved into the face node's own space (the node sits at FACE_Y)."""
        return ((mesh[0] - np.array([0, FACE_Y, 0], np.float32), mesh[1], mesh[2]), kit.grid_uv(mesh, inset(rect), cols))

    # ⚠️ STERN (owner, 2026-10-07, of a first face with big round eyes: "should be sterner cuz this is an ultimate ability of
    # essentialy a tree guardian"). So the hollows are narrow and slanted DOWN to the middle, the light in each is a
    # slit, the brow is one heavy ledge hanging over them with a furrow cut between, and under the nose a hard
    # down-turned line is cut for a mouth. Nothing round, nothing soft.
    for side in (-1, 1):
        at = on_trunk(side * 30, FACE_Y + 0.00, -0.030)
        socket = moved(ellipsoid((0.215, 0.105, 0.050)), turn=(0, 0, side * 17))
        face.append((moved(socket, turn=(0, side * 30, 0), at=(at[0], at[1] - FACE_Y, at[2])), SOCKET))
        # The brow: a heavy ledge, lowest and thickest toward the middle, hanging over the top of the hollow.
        brow = tube([on_trunk(side * 3, FACE_Y + 0.045, 0.050), on_trunk(side * 22, FACE_Y + 0.165, 0.095), on_trunk(side * 44, FACE_Y + 0.235, 0.070),
                     on_trunk(side * 62, FACE_Y + 0.215, 0.015)], [0.085, 0.105, 0.075, 0.034], seg=10)
        face.append(on_face(brow, R_ROOT, 10))
        # A hard cheek line under it.
        cheek = tube([on_trunk(side * 9, FACE_Y - 0.150, 0.006), on_trunk(side * 30, FACE_Y - 0.135, 0.030), on_trunk(side * 54, FACE_Y - 0.030, 0.008)],
                     [0.022, 0.036, 0.020], seg=8)
        face.append(on_face(cheek, R_ROOT, 8))
    # The furrow between the brows, and the nose: a long ridge.
    furrow = tube([on_trunk(0, FACE_Y + 0.26, 0.010), on_trunk(0, FACE_Y + 0.12, 0.040)], [0.016, 0.030], seg=8)
    face.append(((furrow[0] - np.array([0, FACE_Y, 0], np.float32), furrow[1], furrow[2]), BARK_DK))
    nose = tube([on_trunk(0, FACE_Y + 0.06, 0.020), on_trunk(0, FACE_Y - 0.10, 0.055), on_trunk(0, FACE_Y - 0.20, 0.030)], [0.040, 0.062, 0.040], seg=10)
    face.append(on_face(nose, R_ROOT, 10))
    # The mouth: a cut in the bark, its ends turned hard down.
    mouth = tube([on_trunk(-30, FACE_Y - 0.47, 0.004), on_trunk(-17, FACE_Y - 0.385, 0.012), on_trunk(0, FACE_Y - 0.360, 0.014),
                  on_trunk(17, FACE_Y - 0.385, 0.012), on_trunk(30, FACE_Y - 0.47, 0.004)], [0.012, 0.024, 0.027, 0.024, 0.012], seg=8)
    face.append(((mouth[0] - np.array([0, FACE_Y, 0], np.float32), mouth[1], mouth[2]), SOCKET))
    m.add("face", face, at=(0.0, FACE_Y, 0.0), parent="trunk")
    # The lights in the hollows: a slanted slit in each with a hot heart. On their own node, so the body blinks and wakes them.
    eyes = []
    for side in (-1, 1):
        at = on_trunk(side * 30, FACE_Y - 0.005, 0.010)
        rel = (at[0], at[1] - FACE_Y - 0.004, at[2])
        for size, slot, out in (((0.150, 0.052, 0.020), EYE_GLOW, 0.0), ((0.090, 0.028, 0.020), EYE, 0.010)):
            slit = moved(ellipsoid(size, at=(side * -0.012, 0.0, out)), turn=(0, 0, side * 17))
            eyes.append((moved(slit, turn=(0, side * 30, 0), at=rel), slot))
    m.add("eyes", eyes, at=(0.0, 0.004, 0.0), parent="face")

    # ⚠️ GROWN, NOT TURNED ON A LATHE (owner, 2026-10-07, of roots that were six copies of one arch set evenly round the
    # foot and boughs that were five copies of one fork: "the crown and roots dont looke natural and organic"). Nothing
    # here repeats now. Every root and every bough is typed on its own: where it leaves, how it wanders SIDEWAYS as
    # well as up and out, how thick, where it forks, whether it forks at all. They are unevenly spaced, one root is a
    # stub, one bough is broken off short, the leader leans. And every one is drawn as a smooth curve through its
    # points (`limb`), where the first cut joined them with straight pieces and showed an elbow at each.
    # ⚠️ NO LIMB ENDS THINNER THAN THIS. A tip finer than the ink line is all ink: the first render had black specks at the
    # end of every twig and root.
    TIP = 0.022

    def limb(points, radii, rect, seg=10, per=5):
        """A smooth limb through `points` (a Catmull-Rom curve), its girth eased between `radii`."""
        pts, rad = [], []
        n = len(points)
        P = [np.array(q, np.float32) for q in points]
        for i in range(n - 1):
            p0, p1, p2, p3 = P[max(i - 1, 0)], P[i], P[i + 1], P[min(i + 2, n - 1)]
            for k in range(per):
                t = k / per
                pts.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
                rad.append(max(TIP, radii[i] + (radii[i + 1] - radii[i]) * (t * t * (3 - 2 * t))))
        pts.append(P[-1]); rad.append(max(TIP, radii[-1]))
        mesh = tube([tuple(float(v) for v in q) for q in pts], rad, seg=seg)
        return painted(mesh, rect, seg)

    def tuft(at, leaves):
        """A handful of leaves from one twig end, each typed: length, sweep, droop."""
        return [turned(leaf_painted(length, length * .50, 0.035, R_LEAF), turn=(droop, sweep, 0), at=at) for length, sweep, droop in leaves]

    # ---- the roots. Each: yaw round the foot; the root (x sideways, y up, z out) and its girth; side roots; moss (at, size) or None.
    ROOTS = [
        (8, [(0, .34, 0), (.06, .58, .40), (.16, .50, .82), (.10, .20, 1.22), (-.04, -.02, 1.52), (-.10, -.30, 1.70)], [.25, .24, .19, .14, .08, .03],
         [([(.16, .50, .82), (.42, .30, 1.10), (.60, .02, 1.28), (.66, -.28, 1.36)], [.12, .10, .06, .02])], ((.08, .70, .50), (.20, .09, .26))),
        (58, [(0, .22, 0), (-.05, .30, .36), (-.14, .16, .72), (-.10, -.04, .98), (-.06, -.26, 1.10)], [.17, .16, .12, .07, .025], [], None),
        (118, [(0, .30, 0), (.08, .44, .42), (-.06, .34, .86), (-.20, .14, 1.26), (-.10, .02, 1.62), (.04, -.22, 1.90)], [.22, .21, .17, .12, .07, .025],
         [([(-.20, .14, 1.26), (-.46, .04, 1.46), (-.60, -.24, 1.56)], [.08, .05, .02])], ((-.02, .50, .70), (.17, .07, .22))),
        (172, [(0, .36, 0), (.04, .66, .34), (.02, .60, .70), (-.06, .24, 1.02), (-.04, -.28, 1.20)], [.21, .20, .16, .10, .03], [], None),
        (236, [(0, .28, 0), (-.10, .46, .44), (-.02, .40, .90), (.14, .16, 1.30), (.12, -.26, 1.56)], [.23, .22, .17, .10, .03],
         [([(-.02, .40, .90), (-.30, .22, 1.14), (-.44, -.24, 1.30)], [.10, .07, .02])], ((-.08, .60, .52), (.18, .08, .20))),
        (296, [(0, .24, 0), (.08, .36, .34), (.06, .18, .66), (.00, -.24, .86)], [.18, .17, .11, .03], [], None),
    ]
    # ⚠️ THE ROOTS STAY INSIDE THE FIRST TREE'S REACH (1.65 m from its middle as typed, 2.14 m as it stands):
    # `PaeteRules.SentryCanClearance` is sized against that, so a root must not lie over the can. They are typed long and
    # brought in by `REACH`.
    REACH = 0.68

    def near(q):
        return (q[0] * 0.85, q[1], q[2] * REACH)

    for i, (yaw, pts, radii, sides, moss) in enumerate(ROOTS):
        pieces = [limb([near(q) for q in pts], radii, R_ROOT, seg=12)]
        for side_pts, side_r in sides:
            pieces.append(limb([near(q) for q in side_pts], side_r, R_ROOT, seg=10))
        if moss is not None:
            at, size = moss
            at = near(at)
            pieces.append(turned(painted(ellipsoid(size, seg=12, rings=8), R_MOSS, 12), turn=(0, 20 * i, 0), at=at))
        m.add("buttress-%d" % i, pieces, at=(0.34 * math.sin(math.radians(yaw)), 0.0, 0.34 * math.cos(math.radians(yaw))), yaw=yaw)

    # ---- the crown. Still pointed, still mostly wood, still no green mass: the light inside it shows between the boughs.
    leader = limb([(0, -.12, 0), (.07, .40, .04), (.02, .88, -.06), (-.08, 1.36, -.02), (-.04, 1.92, .04)], [.27, .20, .135, .075, .012], R_BRANCH)
    twig = limb([(.02, .88, -.06), (.22, 1.06, -.20), (.30, 1.30, -.26)], [.05, .032, .008], R_BRANCH, seg=8)
    m.add("crown", [leader, twig] + tuft((.30, 1.30, -.26), [(.24, 130, 10), (.20, 170, 30)]), at=(0.0, CROWN_Y, 0.0), parent="trunk")
    # Each: yaw; the bough and its girth; forks; tufts (at, leaves); a blossom (at) or None.
    BOUGHS = [
        (20, [(0, -.05, 0), (.05, .34, .30), (.16, .74, .52), (.10, 1.16, .80), (-.06, 1.50, .96), (-.10, 1.78, .98)], [.19, .16, .125, .09, .055, .014],
         [([(.16, .74, .52), (.42, .92, .66), (.60, 1.20, .72), (.66, 1.42, .86)], [.08, .06, .04, .01]),
          ([(.10, 1.16, .80), (.04, 1.30, 1.10), (.14, 1.50, 1.34)], [.055, .035, .01])],
         [((.66, 1.42, .86), [(.34, 40, 6), (.30, 90, 22), (.26, -10, 30), (.22, 140, 40)]), ((.14, 1.50, 1.34), [(.30, 10, 14), (.26, -50, 30)])], (-.10, 1.78, .98)),
        (86, [(0, -.10, 0), (-.04, .10, .42), (-.14, .30, .86), (-.06, .60, 1.22), (.10, .84, 1.46), (.14, 1.10, 1.56)], [.18, .16, .12, .085, .05, .013],
         [([(-.14, .30, .86), (-.44, .46, 1.04), (-.62, .70, 1.08)], [.07, .045, .01]),
          ([(.10, .84, 1.46), (.30, .86, 1.72), (.36, 1.02, 1.90)], [.04, .028, .008])],
         [((-.62, .70, 1.08), [(.32, -60, 10), (.28, -110, 26), (.24, -20, 34)]), ((.36, 1.02, 1.90), [(.30, 30, 20), (.26, 80, 36), (.22, -20, 44)]),
          ((.14, 1.10, 1.56), [(.26, 0, 8), (.22, -40, 24)])], None),
        # Broken off short, long ago: a blunt end, and one young shoot from its side that has leafed.
        (150, [(0, 0, 0), (.06, .30, .26), (.10, .52, .50), (.04, .66, .62)], [.15, .12, .09, .07],
         [([(.06, .30, .26), (-.16, .56, .40), (-.24, .84, .42)], [.04, .028, .008])],
         [((-.24, .84, .42), [(.28, -30, 4), (.24, -80, 22), (.22, 20, 26)])], None),
        (205, [(0, -.05, 0), (-.06, .40, .24), (-.02, .90, .44), (.12, 1.36, .56), (.08, 1.74, .66)], [.20, .165, .12, .075, .014],
         [([(-.02, .90, .44), (-.30, 1.16, .60), (-.40, 1.50, .62)], [.07, .045, .01]),
          ([(.12, 1.36, .56), (.34, 1.50, .80), (.40, 1.76, .88)], [.05, .03, .008])],
         [((-.40, 1.50, .62), [(.30, -40, 8), (.26, -100, 26), (.22, 10, 34)]), ((.40, 1.76, .88), [(.28, 40, 6), (.24, 100, 26)])], (.08, 1.74, .66)),
        (288, [(0, -.08, 0), (.08, .22, .38), (.02, .52, .76), (-.14, .86, 1.04), (-.12, 1.20, 1.20)], [.17, .145, .105, .065, .013],
         [([(.02, .52, .76), (.28, .62, 1.00), (.44, .86, 1.12)], [.065, .04, .009]),
          ([(-.14, .86, 1.04), (-.38, .96, 1.22), (-.46, 1.20, 1.24)], [.045, .028, .008])],
         [((.44, .86, 1.12), [(.32, 50, 12), (.28, 110, 28), (.24, 0, 36), (.20, 160, 44)]), ((-.12, 1.20, 1.20), [(.28, -10, 10), (.24, -70, 28)])], None),
    ]
    for i, (yaw, pts, radii, forks, tufts, flower) in enumerate(BOUGHS):
        pieces = [limb(pts, radii, R_BRANCH)]
        for fork_pts, fork_r in forks:
            pieces.append(limb(fork_pts, fork_r, R_BRANCH, seg=8))
        for at, leaves in tufts:
            pieces += tuft(at, leaves)
        if flower is not None:
            pieces += blossom(flower, (-40, 0, 0), 0.21)
        m.add("claw-%d" % i, pieces, at=(0.24 * math.sin(math.radians(yaw)), 0.0, 0.24 * math.cos(math.radians(yaw))), yaw=yaw, parent="crown")
    m.write(PALETTE)


if __name__ == "__main__":
    main()
