"""MARIANG MAKILING, the spirit in Paete's ultimate, remodelled in the new kit (`Visual.MakilingSpirit`).

Owner, 2026-10-08, after the guardian tree was remodelled: "remodel makiling and the meadow too". Her first model
(`tools/build_paete_props.py` `makiling`) was built in September from lofts and flat ribbons; next to the new tree she
was the most dated thing left in the cutscene.

⚠️ WHAT IS KEPT, because he asked for each in September and has not taken it back: a tall calm woman, NOT in the cast's
chibi proportions ("its fine if u dont make maria makiling like other characters"), in a BARO'T SAYA: a camisa with
long open sleeves, a folded pañuelo over her shoulders, a long saya flaring into the mist, a darker tapis wrapped over
it, long black hair parted in the middle and falling to her knees, a wreath of sampaguita, closed eyes, soft brows, a
small smile, no nose. She holds a light in her cupped hands. 3.0 m tall as typed.

⚠️ SHE IS PAINTED NOW (owner, 2026-10-08, shown a first pass that was palette slots only because her shader could
not sample a texture: "change the maikling shader"). `Resources/Shaders/SpiritGhost.shader` takes a UV in the atlas's
upper half as PAINT and the lower half as a palette slot, as `TumbangPreso/Toon` does; `makiling-atlas.png` is written
beside the model. Her cloth and hair are painted (pleats, the tapis's weave, embroidery, strands); her skin, her face's
ink, the flowers and the light are still slots, because the shader treats the ink and the light specially by slot.

  py -3 tools/build_paete_makiling.py [--out=folder]
  blender -b --python tools/review_hand_companion.py -- --hero=makiling --file=<abs>/makiling.glb --version=k1 --ink=0.01 --size=3.6 --centre=0,1.5,0

Writes Assets/TumbangPreso/Resources/Models/PaeteProps/makiling.glb (the first is kept at
ArtSource/paete/props-pre-rework/makiling.glb). The node NAMES and PLACES are the class's contract, unchanged: `body`;
under it `arm-left` and `arm-right` at (+-0.215, 2.100, -0.010) (the arm hangs to the elbow and the forearm comes
forward, hands cupped: pitched about x to reach, turned about y to part), `seed` at (0, 2.020, 0.345), `head` at
(0, 2.272, 0.010); under the head `hair` at (0, 0.320, -0.140) (the long hair, swayed) and `flower-crown`.

Metres, y up, +z her front. The sixteen colours are `MakilingSpirit.Palette`, same order.
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import Model, ellipsoid, lathe, moved, tube  # noqa: E402
from build_paete_bloom import arg, hexc, inset, smooth01  # noqa: E402

PALETTE = ["#231A17", "#4A382E", "#C98E68", "#A8704F", "#E6DDCC", "#C9BEA9", "#FBF8EF", "#E8D8A0",
           "#1E140C", "#6A962E", "#D8FF6A", "#FFFDF6", "#EDE6D8", "#CFC4AE", "#3F5F2C", "#8FB06A"]
HAIR, HAIR_LIT, SKIN, SKIN_SH, CAMISA, CAMISA_SH, PETAL, HEART, INK, LEAF, LIGHT, PANUELO, SAYA, SAYA_FOLD, TAPIS, TRIM = range(16)

ATLAS = 2048
R_SAYA, R_TAPIS, R_CAMISA = (0.0, 0.0, 0.375, 0.25), (0.375, 0.0, 0.75, 0.25), (0.75, 0.0, 1.0, 0.25)
R_PANUELO, R_SLEEVE, R_HAIR = (0.0, 0.25, 0.25, 0.5), (0.25, 0.25, 0.5, 0.5), (0.5, 0.25, 0.75, 0.5)


def painted(mesh, rect, cols, along="rows", axis=1):
    return (mesh, kit.grid_uv(mesh, inset(rect), cols, along, axis))


def keep(piece, mesh):
    """The same paint on a piece whose points were moved after its UVs were laid."""
    return (mesh, piece[1])


def paint_atlas(path):
    from PIL import Image, ImageDraw, ImageFilter
    img = np.zeros((ATLAS, ATLAS, 3), np.float32)
    img[:] = hexc("#EDE6D8")

    def grid(rect):
        x0, y0, x1, y1 = (int(v * ATLAS) for v in rect)
        u = (np.arange(x0, x1) + .5 - x0) / (x1 - x0)
        t = 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0)
        return np.meshgrid(u, t), (x0, y0, x1, y1)

    def mix(base, colour, weight):
        return base * (1 - weight[..., None]) + hexc(colour) * weight[..., None]

    # ---- the saya: warm white, pleated (a shade in each fold, deeper toward the hem), a cut-work band and a rolled hem.
    (u, t), (x0, y0, x1, y1) = grid(R_SAYA)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#F4EFE4")
    fold = (0.5 + 0.5 * np.cos(u * 2 * np.pi * 14 + 0.6 * np.sin(u * 2 * np.pi * 3))) ** 2
    c = mix(c, "#CFC6B2", fold * (0.55 - 0.40 * t))
    c = mix(c, "#FFFFFF", (1 - fold) * 0.25 * (1 - t))
    c = mix(c, "#BFD1B4", smooth01(0.16, 0.0, t) * 0.55)                                  # the mist's green taking her hem
    for at, wide in ((0.115, 0.010), (0.185, 0.006)):
        c = mix(c, "#B8AE96", np.exp(-((t - at) / wide) ** 2) * 0.85)
    holes = (0.5 + 0.5 * np.cos(u * 2 * np.pi * 42)) ** 6 * np.exp(-((t - 0.150) / 0.014) ** 2)
    c = mix(c, "#9E9580", holes * 0.9)
    img[y0:y1, x0:x1] = c
    # ---- the tapis: deep green hand-woven cloth, striped down its length, banded at its hem and its waist.
    (u, t), (x0, y0, x1, y1) = grid(R_TAPIS)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#3F5F2C")
    c = mix(c, "#2E4821", (0.5 + 0.5 * np.cos(u * 2 * np.pi * 9 + 1.0)) ** 2 * 0.55)                    # soft folds
    for at, wide, colour, much in [(0.05, .006, "#8FB06A", .9), (0.12, .003, "#C9D78A", .8), (0.20, .008, "#27401C", .8), (0.29, .004, "#8FB06A", .9),
                                   (0.36, .003, "#C9D78A", .7), (0.45, .008, "#27401C", .8), (0.55, .005, "#8FB06A", .9), (0.62, .003, "#C9D78A", .8),
                                   (0.70, .008, "#27401C", .8), (0.79, .004, "#8FB06A", .9), (0.87, .003, "#C9D78A", .7), (0.95, .007, "#27401C", .8)]:
        c = mix(c, colour, np.exp(-((u - at) / wide) ** 2) * much)
    for at, wide, colour in [(0.06, .020, "#C9D78A"), (0.115, .008, "#8FB06A"), (0.955, .018, "#C9D78A")]:
        c = mix(c, colour, np.exp(-((t - at) / wide) ** 2) * 0.9)
    weave = (0.5 + 0.5 * np.cos(t * 2 * np.pi * 120)) * (0.5 + 0.5 * np.cos(u * 2 * np.pi * 150))
    c = mix(c, "#4E7438", weave * 0.14)
    img[y0:y1, x0:x1] = c
    # ---- the camisa: fine white cloth, soft folds, a line of embroidery down its front (u = 0.25).
    (u, t), (x0, y0, x1, y1) = grid(R_CAMISA)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#F1ECE0")
    c = mix(c, "#D2CAB8", (0.5 + 0.5 * np.cos(u * 2 * np.pi * 8)) ** 3 * 0.35)
    c = mix(c, "#C2B99F", np.exp(-((u - 0.25) / 0.012) ** 2) * (0.5 + 0.5 * np.cos(t * 2 * np.pi * 18)) * 0.8)
    img[y0:y1, x0:x1] = c
    # ---- the pañuelo: starched, a touch warmer, folded (lines running round it), an embroidered edge.
    (u, t), (x0, y0, x1, y1) = grid(R_PANUELO)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#FBF6EA")
    for at in (0.30, 0.55, 0.78):
        c = mix(c, "#D8CFBA", np.exp(-((t - at) / 0.012) ** 2) * 0.6)
    c = mix(c, "#C8BD9E", np.exp(-((t - 0.06) / 0.03) ** 2) * (0.35 + 0.65 * (0.5 + 0.5 * np.cos(u * 2 * np.pi * 36)) ** 3))
    img[y0:y1, x0:x1] = c
    # ---- a sleeve: sheer, cooler than the camisa, falling in folds, its hem worked with a scalloped band.
    (u, t), (x0, y0, x1, y1) = grid(R_SLEEVE)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#E9EEE8")
    c = mix(c, "#C4CEC6", (0.5 + 0.5 * np.cos(u * 2 * np.pi * 6 + 0.8)) ** 2 * (0.25 + 0.35 * (1 - t)))
    c = mix(c, "#F8FBF6", smooth01(0.6, 1.0, t) * 0.5)
    c = mix(c, "#AEB9A8", np.exp(-((t - 0.10) / 0.022) ** 2) * (0.4 + 0.6 * (0.5 + 0.5 * np.cos(u * 2 * np.pi * 22)) ** 2))
    c = mix(c, "#AEB9A8", np.exp(-((t - 0.03) / 0.012) ** 2) * 0.9)
    img[y0:y1, x0:x1] = c
    # ---- her hair: black, with strands that catch the light running its length.
    (u, t), (x0, y0, x1, y1) = grid(R_HAIR)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#231A17")
    for at, wide, much in [(0.08, .010, .7), (0.19, .006, .5), (0.27, .014, .8), (0.40, .007, .5), (0.52, .012, .75), (0.63, .006, .45),
                           (0.74, .013, .8), (0.86, .007, .5), (0.95, .010, .65)]:
        c = mix(c, "#5A463A", np.exp(-((u - at - 0.012 * np.sin(t * 9)) / wide) ** 2) * much)
    c = mix(c, "#140E0C", smooth01(0.25, 0.0, t) * 0.5)
    img[y0:y1, x0:x1] = c

    picture = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB")
    d = ImageDraw.Draw(picture, "RGBA")
    # Sampaguita worked into the saya's lower half, each typed: where round her, how high, how big.
    x0, y0, x1, y1 = (int(v * ATLAS) for v in R_SAYA)
    w, h = x1 - x0, y1 - y0
    for u_at, t_at, r in [(0.06, 0.30, 9), (0.17, 0.42, 7), (0.25, 0.27, 10), (0.34, 0.38, 7), (0.45, 0.31, 9), (0.55, 0.44, 7), (0.63, 0.28, 9),
                          (0.74, 0.40, 8), (0.84, 0.30, 10), (0.94, 0.41, 7)]:
        cx, cy = x0 + u_at * w, y0 + (1 - t_at) * h
        for k in range(5):
            a = math.radians(k * 72 + 20)
            d.ellipse([cx + math.cos(a) * r - r * .6, cy + math.sin(a) * r - r * .6, cx + math.cos(a) * r + r * .6, cy + math.sin(a) * r + r * .6], fill=(196, 188, 164, 230))
        d.ellipse([cx - r * .35, cy - r * .35, cx + r * .35, cy + r * .35], fill=(222, 204, 140, 240))
    picture = picture.filter(ImageFilter.GaussianBlur(0.6))
    picture.save(path)


# The saya, turned about her own up: (radius, height). It flares into the mist at her feet.
SAYA_ROWS = [(0.560, 0.00), (0.545, 0.05), (0.450, 0.30), (0.375, 0.65), (0.325, 1.00), (0.285, 1.30), (0.250, 1.55), (0.228, 1.70), (0.220, 1.76)]
SAYA_FLAT = 0.86          # she is not round: front to back is this much of side to side
HEAD_AT, HEAD = (0.0, 0.205, 0.0), (0.150, 0.205, 0.158)


def saya_radius(y):
    for (r0, y0), (r1, y1) in zip(SAYA_ROWS, SAYA_ROWS[1:]):
        if y0 <= y <= y1:
            return r0 + (r1 - r0) * (y - y0) / (y1 - y0)
    return SAYA_ROWS[-1][0]


def trail(y):
    """Her hem trails a little behind her: how far back the saya is drawn at height y."""
    return -0.30 * max(0.0, 1.0 - y / 1.2) ** 2


def on_saya(angle, y, out=0.0):
    a = math.radians(angle)
    r = saya_radius(y) + out
    return (r * math.sin(a), y, r * math.cos(a) * SAYA_FLAT + trail(y))


def trailed(mesh):
    pos = mesh[0].copy()
    for i in range(len(pos)):
        pos[i, 2] += trail(float(pos[i, 1]))
    return (pos, mesh[1], mesh[2])


def on_head(angle, elevation, out=0.0):
    a, e = math.radians(angle), math.radians(elevation)
    d = (math.sin(a) * math.cos(e), math.sin(e), math.cos(a) * math.cos(e))
    return tuple(HEAD_AT[k] + d[k] * (HEAD[k] + out) for k in range(3))


def stroke(points, girth, slot, seg=6):
    """A drawn line on her face: a thin tube through points on the head's surface."""
    return (tube([on_head(a, e, 0.004) for a, e in points], girth, seg=seg), slot)


def lock(points, radii, flat, seg=10, per=5):
    """A lock of hair through `points`, smooth, flattened front to back."""
    P = [np.array(q, np.float32) for q in points]
    pts, rad = [], []
    for i in range(len(P) - 1):
        p0, p1, p2, p3 = P[max(i - 1, 0)], P[i], P[i + 1], P[min(i + 2, len(P) - 1)]
        for k in range(per):
            t = k / per
            pts.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
            rad.append(radii[i] + (radii[i + 1] - radii[i]) * t)
    pts.append(P[-1]); rad.append(radii[-1])
    mesh = tube([tuple(float(v) for v in q) for q in pts], [max(0.012, r) for r in rad], seg=seg)
    # Flatten about the lock's own middle line, in z.
    pos = mesh[0].copy()
    mid = np.array([q[2] for q in pts], np.float32)
    ys = np.array([q[1] for q in pts], np.float32)
    for i in range(len(pos)):
        j = int(np.argmin(np.abs(ys - pos[i, 1])))
        pos[i, 2] = mid[j] + (pos[i, 2] - mid[j]) * flat
    return (pos, mesh[1], mesh[2])


def blossom(at, turn, size):
    """A sampaguita: one five-lobed cup (so its ink never crosses itself) and a heart. It faces up before `turn`."""
    cup = lathe([(0.03, 0.0), (0.46, 0.08), (0.86, 0.24), (1.0, 0.38), (0.80, 0.43), (0.32, 0.30), (0.0, 0.24)], seg=20)
    pos = cup[0].copy()
    for i in range(len(pos)):
        lobe = 1.0 + 0.24 * math.cos(5 * math.atan2(float(pos[i, 2]), float(pos[i, 0])))
        pos[i, 0] *= lobe; pos[i, 2] *= lobe
    pos *= size * 0.62
    return [(moved((pos, cup[1], cup[2]), turn=turn, at=at), PETAL),
            (moved(ellipsoid((size * .16, size * .11, size * .16), at=(0, size * .19, 0)), turn=turn, at=at), HEART)]


def main():
    m = Model("makiling")
    m.folder = Path(arg("out", kit.ROOT / "Assets/TumbangPreso/Resources/Models/PaeteProps"))
    m.folder.mkdir(parents=True, exist_ok=True)

    # ================================================================ her body
    paint_atlas(m.folder / "makiling-atlas.png")
    m.texture = "makiling-atlas.png"
    body = []

    def flow(angle, y):
        """How far the saya's cloth stands out in its folds at this place: none at her waist, deep and slow at her hem."""
        a = math.radians(angle)
        depth = 0.075 * max(0.0, 1.0 - y / 1.55) ** 1.4
        return 1.0 + depth * (math.sin(a * 7 + 0.8) * 0.6 + math.sin(a * 3 - 0.4) * 0.4)

    def cloth(mesh, swing=1.0):
        """A piece turned on a lathe made into cloth: folds that deepen toward the hem, and the hem trailing behind her."""
        pos = mesh[0].copy()
        for i in range(len(pos)):
            x, y, z = (float(v) for v in pos[i])
            f = flow(math.degrees(math.atan2(x, z)), y) if swing else 1.0
            pos[i, 0] = x * f
            pos[i, 2] = z * f + trail(y)
        return (pos, mesh[1], mesh[2])

    # ⚠️ THE SAYA FLOWS (owner, of a first one that was a clean cone: "fix those criticisms"). The same long skirt, but its
    # cloth now stands out in seven folds that deepen toward the hem and it trails behind her; the pleats are painted.
    saya = lathe(SAYA_ROWS, seg=42, squash_z=SAYA_FLAT)
    body.append(keep(painted(saya, R_SAYA, 42, "axis", 1), cloth(saya)))
    body.append((tube([(lambda p, f: (p[0] * f, p[1], (p[2] - trail(0.03)) * f + trail(0.03)))(on_saya(a, 0.03, 0.006), flow(a, 0.03)) for a in range(0, 361, 6)], 0.018, seg=6), SAYA_FOLD))

    # ⚠️ THE TAPIS IS WRAPPED, NOT A BARREL. It was a striped drum with rings round it. Now it is one length of woven
    # cloth wrapped round her hips: its hem runs on a slant (long at her left hip, short at her right), its free edge
    # falls open down her left front with the cloth turned back, it shares the saya's folds, and its stripes are in its
    # weave (the paint), running down it as a tapis's do.
    def hem_of(angle):
        return 1.02 + 0.13 * math.cos(math.radians(angle + 60))
    tapis = lathe([(saya_radius(1.0 + 0.76 * k / 6) + 0.014, 1.0 + 0.76 * k / 6) for k in range(7)], seg=42, squash_z=SAYA_FLAT)
    pos = tapis[0].copy()
    for i in range(len(pos)):
        x, y, z = (float(v) for v in pos[i])
        angle = math.degrees(math.atan2(x, z))
        low = hem_of(angle)
        pos[i, 1] = low + (y - 1.0) * (1.76 - low) / 0.76
    tapis_shaped = (pos, tapis[1], tapis[2])
    body.append(keep(painted(tapis, R_TAPIS, 42, "axis", 1), cloth(tapis_shaped)))

    def on_tapis(angle, y, out=0.0):
        p = on_saya(angle, y, 0.014 + out)
        f = flow(angle, y)
        return (p[0] * f, y, (p[2] - trail(y)) * f + trail(y))
    body.append((tube([on_tapis(a, hem_of(a), 0.004) for a in range(0, 361, 6)], 0.013, seg=6), TRIM))                         # its hem's edge
    # Its free edge, down her left front, turned back on itself: a soft roll of the same cloth, and the tie at her hip.
    edge = [on_tapis(-30 + 5 * math.sin(k * 1.1), 1.74 - (1.74 - hem_of(-30) - 0.01) * k / 7, 0.020) for k in range(8)]
    body.append((tube(edge, [0.020, 0.026, 0.030, 0.032, 0.032, 0.030, 0.026, 0.016], seg=8), TAPIS))
    body.append((tube([(q[0] + 0.006, q[1], q[2] + 0.012) for q in edge], 0.007, seg=5), TRIM))
    knot = on_tapis(-52, 1.71, 0.030)
    body.append((ellipsoid((0.052, 0.042, 0.042), at=knot), TAPIS))
    for dx, length in ((-0.035, 0.30), (0.030, 0.22)):
        body.append((tube([knot, (knot[0] + dx, knot[1] - length * .5, knot[2] + 0.025), (knot[0] + dx * 1.7, knot[1] - length, knot[2] + 0.012)],
                          [0.024, 0.028, 0.012], seg=6), TRIM))

    # THE CAMISA: fitted from her waist, to a narrow neck under the pañuelo.
    camisa = lathe([(0.228, 1.74), (0.204, 1.84), (0.214, 1.96), (0.198, 2.06), (0.150, 2.14), (0.070, 2.18)], seg=24, squash_z=0.70)
    body.append(painted(camisa, R_CAMISA, 24, "axis", 1))
    body.append((tube([(0.228 * math.sin(math.radians(a)), 1.765, 0.228 * 0.70 * math.cos(math.radians(a))) for a in range(0, 361, 12)], 0.022, seg=6), TRIM))   # her sash
    # THE PAÑUELO: a folded kerchief over her shoulders, its ends crossing to a point on her chest, its fold lines.
    # Narrower than the first (0.33 out), so her arms and sleeves stand clear of it and are not one white mass with it.
    panuelo = lathe([(0.272, 2.020), (0.262, 2.060), (0.212, 2.125), (0.112, 2.190), (0.062, 2.205)], seg=24, squash_z=0.78)
    body.append(painted(panuelo, R_PANUELO, 24, "axis", 1))
    for sx in (-1, 1):
        body.append((tube([(0.17 * sx, 2.07, 0.160), (0.09 * sx, 1.99, 0.205), (0.0, 1.90, 0.214)], [0.044, 0.036, 0.014], seg=8), PANUELO))
    body.append((ellipsoid((0.022, 0.022, 0.014), at=(0.0, 1.922, 0.224)), HEART))                                             # its pin
    # Her neck.
    body.append((tube([(0.0, 2.15, 0.0), (0.0, 2.30, 0.008)], [0.058, 0.050], seg=10), SKIN))
    m.add("body", body)

    # ================================================================ her arms: the arm hangs to the elbow, the forearm comes forward
    # ⚠️ THE ARMS READ AS ARMS. In the first pass the arm, the forearm, the hanging sleeve and the pañuelo were all one
    # white and lay on each other. The upper arm now hangs a little OUT from her side with a slim fitted sleeve, the
    # forearm comes forward and in to her cupped hands, and the open sleeve falls from the forearm as a wide sheet that
    # hangs outside her hip, painted cooler and worked along its hem, so each is a shape of its own.
    for name, sx in (("arm-left", 1.0), ("arm-right", -1.0)):
        elbow, wrist = (0.110 * sx, -0.270, 0.060), (-0.128 * sx, -0.130, 0.290)
        hand = (-0.186 * sx, -0.110, 0.330)
        upper = tube([(0.006 * sx, 0.020, 0.0), (0.066 * sx, -0.120, 0.016), elbow], [0.074, 0.060, 0.050], seg=12)
        fore = tube([elbow, (0.000 * sx, -0.205, 0.180), wrist], [0.050, 0.046, 0.040], seg=12)
        arm = [painted(upper, R_SLEEVE, 12), painted(fore, R_SLEEVE, 12),
               (tube([wrist, hand], [0.034, 0.032], seg=10), SKIN),
               (moved(ellipsoid((0.046, 0.026, 0.060)), turn=(14, -38 * sx, 0), at=hand), SKIN)]
        arm.append((tube([(wrist[0], wrist[1] + 0.046 * math.sin(math.radians(a)), wrist[2] + 0.046 * 0.6 * math.cos(math.radians(a))) for a in range(0, 361, 30)], 0.010, seg=5), CAMISA_SH))
        # The hanging sleeve: from under her forearm, falling outside her hip.
        drape = lathe([(0.215, -0.74), (0.190, -0.52), (0.136, -0.29), (0.078, -0.10), (0.040, 0.0)], seg=22, squash_z=0.13)
        how = dict(turn=(-6, 62 * sx, 4 * sx), at=(0.040 * sx, -0.225, 0.150))
        arm.append((moved(drape, **how), kit.grid_uv(drape, inset(R_SLEEVE), 22, "axis", 1)))
        m.add(name, arm, at=(0.215 * sx, 2.100, -0.010), parent="body")

    # THE LIGHT she gives him, cupped in her hands.
    m.add("seed", [(ellipsoid((0.060, 0.060, 0.060)), LIGHT)], at=(0.0, 2.020, 0.345), parent="body")

    # ================================================================ her head
    head = [(ellipsoid(HEAD, at=HEAD_AT, seg=24, rings=16), SKIN)]
    for sx in (1, -1):
        # Closed eyes: a lowered lid curving down, two short lashes at its outer end.
        head.append(stroke([(13 * sx, -3), (24 * sx, -8), (36 * sx, -8), (46 * sx, -3)], [0.006, 0.008, 0.008, 0.005], INK))
        head.append(stroke([(40 * sx, -6), (47 * sx, -11)], [0.005, 0.003], INK))
        head.append(stroke([(33 * sx, -8), (37 * sx, -13)], [0.005, 0.003], INK))
        # A soft brow, high and gently arched.
        head.append(stroke([(14 * sx, 12), (27 * sx, 15), (42 * sx, 12)], [0.004, 0.006, 0.004], HAIR_LIT))
    # The smile: small, turned up at both corners.
    head.append(stroke([(-12, -35), (-5, -39), (5, -39), (12, -35)], [0.005, 0.007, 0.007, 0.005], INK))
    # HER HAIR: the cap over her crown and the back of her head, parted in the middle.
    cap = ellipsoid((0.170, 0.200, 0.178), at=(0.0, 0.232, -0.036), seg=24, rings=16)
    head.append(painted(cap, R_HAIR, 24))
    head.append((tube([(0.0, 0.425, 0.050), (0.0, 0.436, -0.040), (0.0, 0.400, -0.130)], 0.006, seg=5), HAIR_LIT))            # the parting
    # Two curtains from the parting, down past her cheeks to her chest: they frame her face.
    for sx in (1, -1):
        curtain = lock([(0.020 * sx, 0.405, 0.100), (0.122 * sx, 0.335, 0.112), (0.172 * sx, 0.150, 0.060), (0.172 * sx, -0.120, 0.040), (0.158 * sx, -0.400, 0.060)],
                       [0.034, 0.040, 0.036, 0.028, 0.012], 0.50)
        head.append(painted(curtain, R_HAIR, 10))
    m.add("head", head, at=(0.0, 2.272, 0.010), parent="body")

    # THE LONG HAIR down her back to her knees: three broad locks, each its own fall, with lit strands.
    long_hair = []
    for x0, sway, length, fat in [(-0.115, -0.05, 1.56, 0.105), (0.0, 0.02, 1.70, 0.120), (0.118, 0.06, 1.50, 0.102)]:
        fall = lock([(x0, 0.03, 0.02), (x0 * 1.15 + sway * .3, -0.40, -0.075), (x0 * 1.25 + sway * .7, -0.95, -0.120), (x0 * 1.15 + sway, -1.35, -0.105),
                     (x0 * 0.95 + sway * 1.3, -length, -0.060)], [fat, fat * 1.05, fat * .92, fat * .66, 0.014], 0.42)
        long_hair.append(painted(fall, R_HAIR, 10))
    m.add("hair", long_hair, at=(0.0, 0.320, -0.140), parent="head")

    # THE WREATH: sampaguita round her head on a thin green vine, lower at her brow and higher behind. Each flower
    # typed: where round her head, how big, how it tips.
    crown = []
    def wreath_at(angle, out=0.0):
        a = math.radians(angle)
        r = 0.176 + out
        lift = 0.330 + 0.050 * (1 - math.cos(a)) * .5
        return (r * math.sin(a), lift, r * math.cos(a) * 1.02 - 0.030)
    crown.append((tube([wreath_at(a) for a in range(0, 361, 15)], 0.010, seg=6), LEAF))
    for angle, size, tip in [(0, .070, 62), (34, .058, 70), (68, .066, 58), (104, .054, 66), (142, .062, 60), (180, .056, 68),
                             (216, .064, 58), (252, .052, 66), (290, .066, 60), (326, .058, 70)]:
        crown += blossom(wreath_at(angle, 0.012), (tip, angle, 0), size)
    for angle in (18, 86, 160, 234, 308):
        leafy = lathe([(0.0, 0.0), (0.018, 0.016), (0.022, 0.034), (0.0, 0.062)], seg=8, squash_z=0.35)
        crown.append((moved(leafy, turn=(70, angle, 0), at=wreath_at(angle, 0.006)), LEAF))
    m.add("flower-crown", crown, parent="head")
    m.write(PALETTE)


if __name__ == "__main__":
    main()
