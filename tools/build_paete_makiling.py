"""MARIANG MAKILING, the spirit in Paete's ultimate (`Visual.MakilingSpirit`): THE DIWATA, a second remodel.

Owner, 2026-10-08, of the first remodel (kept as `ArtSource/paete/props-pre-rework/build_paete_makiling_k3.py`, a
standing woman in a pleated saya with straight hair, the September figure rebuilt and painted): "makiling's model
looks exactly the same. it could use a remodel and i dont like the outline stuff on her". So this is a new FIGURE, not
the old one with better cloth:

  * she FLOATS: no feet, her skirt ends in a wisp that trails behind her;
  * her skirt is a SAMPAGUITA hung upside down: two rings of big white petals, their tips curling out;
  * her tapis is the flower's CALYX: green woven sepals over the petals, gold at their edges, a gold sash;
  * her hair is the big shape: it floats out round her as if under water, each lock ending in a curl, flowers in it;
  * lily sleeves at her elbows, a sampaguita garland round her neck, a fuller wreath with a cluster at one temple;
  * a larger head and larger features, so her face reads from where the cutscene's cameras stand.

⚠️ WHAT IS KEPT from September (his words: "its fine if u dont make maria makiling like other characters", "as long as it
still has her style", "like a spirit thats js watching over"): tall and not chibi, a woman of the mountain in white and
green, long dark hair parted in the middle, the sampaguita wreath, closed eyes and a small smile, the light in her
cupped hands. ⚠️ No deer, no antlers ("why is there a deer even did i ask for that").

  py -3 tools/build_paete_makiling.py [--out=folder]
  py -3 tools/build_paete_makiling.py [--out=folder] [--eyes=open]
  blender -b --python tools/review_hand_companion.py -- --hero=makiling --file=<abs>/makiling.glb --atlas=<abs>/makiling-atlas.png --version=d1 --ink=0.0002 --size=4.0 --centre=0,1.55,0

The node NAMES are the class's contract, unchanged: `body`; under it `arm-left`, `arm-right` (the arm hangs to the
elbow, the forearm comes forward, hands cupped), `seed`, `head`; under the head `hair` and `flower-crown`.
Metres, y up, +z her front. The sixteen colours are `MakilingSpirit.Palette`, same order (5 is her blush and 13 gold now).
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import Model, ellipsoid, lathe, moved, tube  # noqa: E402
from build_paete_bloom import arg, hexc, inset, smooth01  # noqa: E402

PALETTE = ["#1B1A16", "#4A382E", "#C98E68", "#A8704F", "#F1ECE0", "#E7968A", "#FBF8EF", "#E8D8A0",
           "#1E140C", "#6A962E", "#D8FF6A", "#FFFDF6", "#EDE6D8", "#E2B84A", "#3F5F2C", "#8FB06A"]
HAIR, HAIR_LIT, SKIN, SKIN_SH, CAMISA, BLUSH, PETAL, HEART, INK, LEAF, LIGHT, PANUELO, SAYA, GOLD, TAPIS, TRIM = range(16)

ATLAS = 2048
R_PETAL, R_TAIL, R_SEPAL, R_CAMISA = (0.0, 0.0, 0.25, 0.25), (0.25, 0.0, 0.5, 0.125), (0.5, 0.0, 0.75, 0.25), (0.75, 0.0, 1.0, 0.25)
# ⚠️ HER FACE IS PAINTED, as the cast's are (owner, 2026-10-08, of eyes, blush and a mouth that were modelled pieces laid on
# her head: "why is the face a model instead of a face texture?"). This rect is her face seen from the front, x from
# -FACE_HALF to FACE_HALF, y from FACE_LOW to FACE_HIGH above her neck; `SpiritGhost.shader` is told where it is
# (`_FaceRect`, set by `MakilingSpirit`) so the dark of her eyes and mouth stays dark when she is a ghost.
R_FACE = (0.25, 0.125, 0.5, 0.25)
FACE_HALF, FACE_LOW, FACE_HIGH = 0.170, 0.030, 0.300
R_PANUELO, R_SLEEVE, R_HAIR, R_LEAF = (0.0, 0.25, 0.25, 0.5), (0.25, 0.25, 0.5, 0.5), (0.5, 0.25, 0.75, 0.5), (0.75, 0.25, 1.0, 0.5)


# ------------------------------------------------------------------ shapes this figure needs

def catmull(points, values, per):
    """A smooth line through `points`, `per` steps a span, with `values` carried along it."""
    P = [np.array(q, np.float32) for q in points]
    pts, val = [], []
    for i in range(len(P) - 1):
        p0, p1, p2, p3 = P[max(i - 1, 0)], P[i], P[i + 1], P[min(i + 2, len(P) - 1)]
        for k in range(per):
            t = k / per
            pts.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
            val.append(values[i] + (values[i + 1] - values[i]) * t)
    pts.append(P[-1]); val.append(values[-1])
    return np.array(pts, np.float32), np.array(val, np.float32)


def smooth_normals(pos, tris, cols):
    """Normals from the shape itself, for a piece made of rings of `cols` + 1 points that was bent after it was built."""
    n = np.zeros_like(pos)
    a, b, c = pos[tris[:, 0]], pos[tris[:, 1]], pos[tris[:, 2]]
    f = np.cross(b - a, c - a)
    for k in range(3):
        np.add.at(n, tris[:, k], f)
    rings = n.reshape(-1, cols + 1, 3)
    seam = rings[:, 0] + rings[:, -1]
    rings[:, 0] = seam; rings[:, -1] = seam
    grid = pos.reshape(-1, cols + 1, 3)
    for r in range(len(rings)):
        if float(np.abs(grid[r] - grid[r, 0]).max()) < 1e-6:           # a ring drawn to a point
            rings[r, :] = rings[r].sum(axis=0)
    n = rings.reshape(-1, 3)
    n /= np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-6)
    return n.astype(np.float32)


def reshaped(mesh, fn, cols):
    """`mesh` with every point passed through `fn`, and its normals made again."""
    pos = np.array([fn(tuple(float(v) for v in p)) for p in mesh[0]], np.float32)
    return (pos, smooth_normals(pos, mesh[2], cols), mesh[2])


def ribbon(points, widths, thick=0.12, face=(0, 0, 1), cup=0.0, seg=12, per=4):
    """A petal, a sepal, a leaf: a smooth blade through `points`, `widths` across, its flat side looking along `face`
    at its start, `thick` of its width deep, its edges cupped toward `face` by `cup` of its half width (negative: away).
    Rings of `seg` + 1 points from its first point to its last, closed to a point at both ends."""
    pts, w = catmull(points, widths, per)
    tan = np.gradient(pts, axis=0)
    tan /= np.maximum(np.linalg.norm(tan, axis=1, keepdims=True), 1e-6)
    side = np.cross(np.array(face, np.float32), tan[0])
    side /= max(float(np.linalg.norm(side)), 1e-6)
    pos = [np.tile(pts[0], (seg + 1, 1))]
    for i in range(len(pts)):
        side = side - tan[i] * float(np.dot(side, tan[i]))
        side /= max(float(np.linalg.norm(side)), 1e-6)
        nrm = np.cross(tan[i], side)
        ring = []
        for c in range(seg + 1):
            a = 2 * math.pi * (c % seg) / seg
            half = float(w[i]) * .5
            ring.append(pts[i] + side * (half * math.cos(a)) + nrm * (half * thick * math.sin(a) + cup * half * math.cos(a) ** 2))
        pos.append(np.array(ring, np.float32))
    pos.append(np.tile(pts[-1], (seg + 1, 1)))
    pos = np.concatenate(pos).astype(np.float32)
    rows = len(pts) + 2
    tris = []
    for r in range(rows - 1):
        for s in range(seg):
            a = r * (seg + 1) + s
            b = a + seg + 1
            tris.append((a, a + 1, b)); tris.append((a + 1, b + 1, b))
    tris = np.array(tris, np.uint32)
    a, b, c = pos[tris[:, 0]], pos[tris[:, 1]], pos[tris[:, 2]]
    if float(np.einsum("ij,ij->i", a, np.cross(b, c)).sum()) < 0:           # inside out: turn it
        tris = tris[:, ::-1].copy()
    return (pos, smooth_normals(pos, tris, seg), tris)


def strand(points, radii, seg=10, per=5):
    """A lock of hair: a round tapering tube, smooth through `points`."""
    pts, rad = catmull(points, radii, per)
    return tube([tuple(float(v) for v in q) for q in pts], [max(0.008, float(r)) for r in rad], seg=seg)


def aimed(mesh, a, b):
    """A piece built along +y from the origin, stood at `a` pointing at `b`."""
    d = np.array(b, np.float32) - np.array(a, np.float32)
    d /= max(float(np.linalg.norm(d)), 1e-6)
    return moved(mesh, turn=(math.degrees(math.acos(max(-1.0, min(1.0, float(d[1]))))), math.degrees(math.atan2(float(d[0]), float(d[2]))), 0), at=a)


def painted(mesh, rect, cols, along="rows", axis=1):
    return (mesh, kit.grid_uv(mesh, inset(rect), cols, along, axis))


def keep(piece, mesh):
    """The same paint on a piece whose points were moved after its UVs were laid."""
    return (mesh, piece[1])


def blossom(at, turn, size):
    """A sampaguita: one five-lobed cup and a heart. It faces up before `turn`."""
    cup = lathe([(0.03, 0.0), (0.46, 0.08), (0.86, 0.24), (1.0, 0.38), (0.80, 0.43), (0.32, 0.30), (0.0, 0.24)], seg=20)
    pos = cup[0].copy()
    for i in range(len(pos)):
        lobe = 1.0 + 0.24 * math.cos(5 * math.atan2(float(pos[i, 2]), float(pos[i, 0])))
        pos[i, 0] *= lobe; pos[i, 2] *= lobe
    pos *= size * 0.62
    return [(moved((pos, cup[1], cup[2]), turn=turn, at=at), PETAL),
            (moved(ellipsoid((size * .16, size * .11, size * .16), at=(0, size * .19, 0)), turn=turn, at=at), HEART)]


def leaf_blade(a, b, wide, face=(0, 1, 0), bow=0.0):
    """A leaf from `a` to `b`, bowed toward `face` in its middle, painted with veins."""
    a, b = np.array(a, np.float32), np.array(b, np.float32)
    mid = (a + b) * .5 + np.array(face, np.float32) * bow
    blade = ribbon([tuple(a), tuple(a + (mid - a) * .9), tuple(mid + (b - mid) * .35), tuple(b)], [wide * .25, wide, wide * .8, wide * .06],
                   thick=0.22, face=face, cup=0.25, seg=8, per=3)
    return painted(blade, R_LEAF, 8)


# ------------------------------------------------------------------ her paint

def paint_atlas(path):
    from PIL import Image, ImageFilter
    img = np.zeros((ATLAS, ATLAS, 3), np.float32)
    img[:] = hexc("#EDE6D8")

    def grid(rect):
        x0, y0, x1, y1 = (int(v * ATLAS) for v in rect)
        u = (np.arange(x0, x1) + .5 - x0) / (x1 - x0)
        t = 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0)
        return np.meshgrid(u, t), (x0, y0, x1, y1)

    def mix(base, colour, weight):
        weight = np.clip(weight, 0, 1)
        return base * (1 - weight[..., None]) + hexc(colour) * weight[..., None]

    # A blade (petal, sepal, leaf) is wrapped round: `s` is across it, -1 at one edge, 0 on its midrib, 1 at the other.
    # ---- a petal of her skirt: warm white, green where it leaves the calyx, fine veins fanning to a jade tip.
    (u, t), (x0, y0, x1, y1) = grid(R_PETAL)
    s = np.cos(u * 2 * np.pi)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#FBF7EC")
    c = mix(c, "#BFD6A0", smooth01(0.30, 0.0, t) * 0.85)
    for k in (-3, -2, -1, 0, 1, 2, 3):
        fan = k * 0.27 * (0.25 + 0.75 * t)
        c = mix(c, "#D6CDB4", np.exp(-((s - fan) / 0.035) ** 2) * (0.50 if k else 0.75) * smooth01(0.02, 0.2, t) * smooth01(1.0, 0.75, t))
    c = mix(c, "#DCEBC6", smooth01(0.72, 1.0, t) * 0.75)
    c = mix(c, "#FFFFFF", smooth01(0.80, 1.0, np.abs(s)) * 0.55)
    c = mix(c, "#D9D0BA", (1 - np.abs(np.sin(u * 2 * np.pi))) * 0.0)
    img[y0:y1, x0:x1] = c
    # ---- the wisp under her skirt: white at her waist, jade, then the mist's green at its tip; slow lines running down it.
    (u, t), (x0, y0, x1, y1) = grid(R_TAIL)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#F3F1E4")
    c = mix(c, "#CFE2BE", smooth01(0.75, 0.35, t))
    c = mix(c, "#8DBE8A", smooth01(0.40, 0.0, t) * 0.9)
    c = mix(c, "#EAF4DC", (0.5 + 0.5 * np.cos((u * 5 + t * 1.6) * 2 * np.pi)) ** 4 * 0.5)
    img[y0:y1, x0:x1] = c
    # ---- a sepal of her tapis: deep green hand-woven cloth, striped down its length, a line of diamonds on its midrib,
    # gold at its edges.
    (u, t), (x0, y0, x1, y1) = grid(R_SEPAL)
    s = np.cos(u * 2 * np.pi)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#3F5F2C")
    for at, wide, colour, much in [(-0.72, .05, "#8FB06A", .9), (-0.56, .025, "#C9D78A", .8), (-0.38, .06, "#27401C", .8), (0.38, .06, "#27401C", .8),
                                   (0.56, .025, "#C9D78A", .8), (0.72, .05, "#8FB06A", .9)]:
        c = mix(c, colour, np.exp(-((s - at) / wide) ** 2) * much)
    diamond = np.abs(((t * 7.0) % 1.0) - 0.5) * 2 + np.abs(s) * 4.2
    c = mix(c, "#C9D78A", (diamond < 0.82) * 0.9)
    c = mix(c, "#3F5F2C", (diamond < 0.42) * 1.0)
    weave = (0.5 + 0.5 * np.cos(t * 2 * np.pi * 90)) * (0.5 + 0.5 * np.cos(s * 2 * np.pi * 26))
    c = mix(c, "#4E7438", weave * 0.16)
    c = mix(c, "#E2B84A", smooth01(0.84, 0.93, np.abs(s)))
    c = mix(c, "#E2B84A", smooth01(0.93, 1.0, t))
    img[y0:y1, x0:x1] = c
    # ---- the camisa: fine white cloth, soft folds, a line of embroidery down its front (u = 0.25).
    (u, t), (x0, y0, x1, y1) = grid(R_CAMISA)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#F1ECE0")
    c = mix(c, "#D2CAB8", (0.5 + 0.5 * np.cos(u * 2 * np.pi * 8)) ** 3 * 0.35)
    c = mix(c, "#C2B99F", np.exp(-((u - 0.25) / 0.012) ** 2) * (0.5 + 0.5 * np.cos(t * 2 * np.pi * 18)) * 0.8)
    img[y0:y1, x0:x1] = c
    # ---- the pañuelo: starched, a touch warmer, its fold lines running round it, a gold worked edge.
    (u, t), (x0, y0, x1, y1) = grid(R_PANUELO)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#FBF6EA")
    for at in (0.34, 0.60, 0.80):
        c = mix(c, "#D8CFBA", np.exp(-((t - at) / 0.012) ** 2) * 0.6)
    c = mix(c, "#E2B84A", np.exp(-((t - 0.07) / 0.035) ** 2) * (0.35 + 0.65 * (0.5 + 0.5 * np.cos(u * 2 * np.pi * 30)) ** 3))
    img[y0:y1, x0:x1] = c
    # ---- a lily sleeve: sheer and cool, six soft ribs running to its mouth, a jade blush and a gold line at its lip.
    (u, t), (x0, y0, x1, y1) = grid(R_SLEEVE)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#EEF2EA")
    c = mix(c, "#C9D3C8", (0.5 + 0.5 * np.cos(u * 2 * np.pi * 6)) ** 3 * (0.20 + 0.45 * t))
    c = mix(c, "#D5E8C4", smooth01(0.62, 0.95, t) * 0.7)
    c = mix(c, "#E2B84A", np.exp(-((t - 0.955) / 0.022) ** 2) * 0.95)
    img[y0:y1, x0:x1] = c
    # ---- her hair: near black, with strands of deep moss that catch the light running its length.
    (u, t), (x0, y0, x1, y1) = grid(R_HAIR)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#1B1A16")
    for at, wide, much in [(0.06, .012, .75), (0.17, .007, .5), (0.26, .016, .85), (0.39, .008, .5), (0.50, .014, .8), (0.62, .007, .45),
                           (0.73, .015, .85), (0.85, .008, .5), (0.94, .012, .7)]:
        c = mix(c, "#3F5A46", np.exp(-((u - at - 0.014 * np.sin(t * 11)) / wide) ** 2) * much)
    c = mix(c, "#5E7F62", np.exp(-((t - 0.80) / 0.05) ** 2) * (0.5 + 0.5 * np.cos(u * 2 * np.pi * 9)) ** 2 * 0.45)      # a soft sheen band
    c = mix(c, "#0F0E0C", smooth01(0.22, 0.0, t) * 0.45)
    img[y0:y1, x0:x1] = c
    # ---- a leaf: fresh green, a pale midrib, side veins, a darker edge.
    (u, t), (x0, y0, x1, y1) = grid(R_LEAF)
    s = np.cos(u * 2 * np.pi)
    c = np.zeros(u.shape + (3,), np.float32) + hexc("#6A962E")
    c = mix(c, "#4C7422", smooth01(0.55, 1.0, np.abs(s)) * 0.7)
    c = mix(c, "#B8D86A", np.exp(-(((np.abs(s) * 2.2 + t * 6.0) % 1.0 - 0.5) / 0.10) ** 2) * 0.35 * (np.abs(s) > 0.06))
    c = mix(c, "#C9E08A", np.exp(-(s / 0.07) ** 2) * 0.85)
    img[y0:y1, x0:x1] = c

    picture = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB").filter(ImageFilter.GaussianBlur(0.6))

    # ---- HER FACE, the cast's: flat skin, two plain dark eyes (closed, or the cast's blocks with `--eyes=open`), a soft
    # round blush under each, a one-stroke smile. No nose, no brows, no shading. Drawn four times too large and brought
    # down, so its edges are clean. Positions are metres on her head, the same the modelled face had.
    from PIL import ImageDraw
    x0, y0, x1, y1 = (int(v * ATLAS) for v in R_FACE)
    big = 4
    wide, high = (x1 - x0) * big, (y1 - y0) * big
    skin, ink, blush = tuple(int(v) for v in hexc(PALETTE[SKIN])), tuple(int(v) for v in hexc(PALETTE[INK])), tuple(int(v) for v in hexc(PALETTE[BLUSH]))

    def px(x, y):
        return ((x + FACE_HALF) / (2 * FACE_HALF) * wide, (FACE_HIGH - y) / (FACE_HIGH - FACE_LOW) * high)

    sx_, sy_ = wide / (2 * FACE_HALF), high / (FACE_HIGH - FACE_LOW)
    face = Image.new("RGB", (wide, high), skin)
    soft = Image.new("L", (wide, high), 0)
    d = ImageDraw.Draw(soft)
    for side in (1, -1):
        cx, cy = px(0.112 * side, 0.122)
        d.ellipse([cx - 0.032 * sx_, cy - 0.026 * sy_, cx + 0.032 * sx_, cy + 0.026 * sy_], fill=235)
    face.paste(Image.new("RGB", (wide, high), blush), mask=soft.filter(ImageFilter.GaussianBlur(big * 2.2)))
    d = ImageDraw.Draw(face)

    def stroke(points, widths):
        pts, wid = catmull([(a, b, 0.0) for a, b in points], widths, 24)
        for q, w in zip(pts, wid):
            cx, cy = px(float(q[0]), float(q[1]))
            d.ellipse([cx - w * .5 * sx_, cy - w * .5 * sy_, cx + w * .5 * sx_, cy + w * .5 * sy_], fill=ink)

    for side in (1, -1):
        cx = 0.080 * side
        if EYES == "open":
            a, b = px(cx - 0.025, 0.185 + 0.032), px(cx + 0.025, 0.185 - 0.032)
            d.rounded_rectangle([a[0], a[1], b[0], b[1]], radius=0.012 * sx_, fill=ink)
            stroke([(cx + 0.018 * side, 0.211), (cx + 0.042 * side, 0.221)], [0.012, 0.003])
        else:
            stroke([(cx - 0.036, 0.193), (cx - 0.019, 0.176), (cx, 0.170), (cx + 0.019, 0.176), (cx + 0.036, 0.193)], [0.007, 0.016, 0.019, 0.016, 0.007])
            stroke([(cx + 0.032 * side, 0.187), (cx + 0.055 * side, 0.181)], [0.011, 0.003])
    stroke([(-0.028, 0.124), (-0.012, 0.111), (0.012, 0.111), (0.028, 0.124)], [0.004, 0.011, 0.011, 0.004])
    picture.paste(face.resize((x1 - x0, y1 - y0), Image.LANCZOS), (x0, y0))
    picture.save(path)


# ------------------------------------------------------------------ where things are on her

WAIST = 1.70
# ⚠️⚠️ HER HEAD IS THE CAST'S HEAD (owner, 2026-10-08, shown an egg ("too basic"), a sculpted face ("weird head
# shape..") and two smooth ovals: "neither. the it just doesnt fit our current style"). The current style is the
# redesigned cast's (`tools/author_character_redesign_cheska.py`, `docs/CHARACTER_REDESIGN_DANTE.md`): a SOFT BOX, fullest
# at the cheeks, its corners rounded in plan, its face ONE FLAT PLANE; on it two plain dark eyes, a round blush under
# each, a one-stroke smile; NO nose, no brows, no modelling ("too realistic", "they need to be more cutesy"); the hair
# cut from blocks and wedges. A fully rounded head was refused for the cast on 2026-10-05. So hers is that box, 1.22
# times a heroine's, on her own tall body ("its fine if u dont make maria makiling like other characters" was said of
# her body's proportions and still holds).
# ⚠️ AND SOFTER THAN THE CAST'S (owner, of that box on her, the same day: "maybe slightly more rounded, the boxy look
# doesnt work when her body is slim and rounded"). The cast's rows have corners of exponent 4.2, a flat top and a flat
# face; hers are 2.6 to 3.0 (2 is an egg), her crown is domed and her jaw rounds in, so the head is a rounded block that
# sits on a rounded body. Her face is no longer one plane, so what is drawn on it is laid on its surface (`face_z`).
# ⚠️ AND THEN AN OVAL (owner, of that rounded block, with a sketch of a round U drawn under her cheeks and a picture
# of Princess Bubblegum: "more.. make her look like princess bubblegum's head shape"). So her head is one smooth TALL
# OVAL: no corners at all, widest a little above its middle, a round chin (the U he drew), a domed crown. What stays
# of the cast is the FACE: flat plain features (two eyes, a round blush, a one-stroke smile, no nose, no brows).
#     rows are (height above her neck, half width, depth to the front, depth to the back, exponent)
HEAD_HIGH = 0.470


def _oval_rows(high, half, front, back, middle=0.54, power=2.25, plan=2.25, chin=0.10, count=16):
    rows = []
    for k in range(count + 1):
        v = 0.5 - 0.5 * math.cos(math.pi * k / count)
        away = (middle - v) / middle if v < middle else (v - middle) / (1.0 - middle)
        r = max(0.0, 1.0 - away ** power) ** (1.0 / power)
        if v < middle:
            r *= 1.0 - chin * away
        rows.append((v * high, max(0.001, half * r), max(0.001, front * r), max(0.001, back * r), plan))
    return rows


# ⚠️ HER NECK IS PART OF HER HEAD (owner, 2026-10-08, of her ghost in the game: "you might have to merge the neck and head
# mesh into one, look at the neck in her ghost model"). As a ghost she is drawn by her edges, so a neck that was its own
# tube pushed up into the oval showed as a lit rod inside her head. Now the oval's underside runs down into the neck as
# ONE surface (a smooth join, `_with_neck`), and the neck goes on down inside her collar.
# ⚠️ AND IT IS A STRAIGHT COLUMN, CUT IN UNDER HER JAW (owner, of a first join that flared out of her collar into her chin
# like a funnel, with a sketch of her jaw coming in and then two straight lines down: "neck needs to be straight and more
# cut"). So there is no blend: the neck is one width all the way up, and her jaw's curve begins from it at a corner.
NECK, NECK_LOW = 0.052, -0.130


def _with_neck(rows):
    out = [(NECK_LOW, NECK, NECK, NECK, 2.0), (-0.060, NECK, NECK, NECK, 2.0), (0.000, NECK, NECK, NECK, 2.0), (0.007, NECK, NECK, NECK, 2.0)]
    return out + [row for row in rows if row[0] > 0.007 and row[1] > NECK * 1.25]


HEAD_ROWS = _with_neck(_oval_rows(HEAD_HIGH, 0.198, 0.188, 0.198))
# Her hair over her skull: the same shape, larger, set back so her face stands through its front, ending at her cheek.
# It comes down the sides and back of her head to her neck (ending at her cheek left the side of her head bare under
# it); below her cheek its front draws back, so it frames her face and does not cover it.
HAIR_ROWS = [(y * 1.04 + 0.034, w + 0.018, f - 0.010 - 0.075 * float(smooth01(0.200, 0.060, y)), back + 0.032, e) for y, w, f, back, e in HEAD_ROWS if y >= 0.035]


def _row_at(rows, y):
    for lo, hi in zip(rows, rows[1:]):
        if lo[0] <= y <= hi[0]:
            k = (y - lo[0]) / max(1e-6, hi[0] - lo[0])
            return tuple(lo[i] + (hi[i] - lo[i]) * k for i in range(5))
    return rows[0] if y < rows[0][0] else rows[-1]


def _front_of(rows, x, y):
    """How far forward the front of a `soft_box` of `rows` is at (x, y); 0 outside it."""
    if y <= rows[0][0] or y >= rows[-1][0]:
        return 0.0
    for lo, hi in zip(rows, rows[1:]):
        if lo[0] <= y <= hi[0]:
            k = (y - lo[0]) / max(1e-6, hi[0] - lo[0])
            half, front, e = (lo[i] + (hi[i] - lo[i]) * k for i in (1, 2, 4))
            return front * max(0.0, 1.0 - min(1.0, abs(x) / half) ** e) ** (1.0 / e)
    return 0.0


def face_z(x, y):
    return _front_of(HEAD_ROWS, x, y)


def brow_z(x, y):
    """The front of her head with her hair on it: whichever stands further forward."""
    return max(face_z(x, y), _front_of(HAIR_ROWS, x, y))


EYES = arg("eyes", "closed")
SKIRT_FLAT = 0.90         # she is not round: front to back is this much of side to side
# A petal of her skirt in the plane through her middle: (how far out, how high), from the calyx to its curled tip.
PETAL_LINE = [(0.165, 1.69), (0.270, 1.46), (0.440, 1.10), (0.570, 0.78), (0.670, 0.56), (0.770, 0.47), (0.845, 0.52)]
PETAL_WIDE = [0.12, 0.36, 0.58, 0.66, 0.54, 0.30, 0.04]
SEPAL_LINE = [(0.172, 1.72), (0.265, 1.56), (0.385, 1.34), (0.475, 1.17), (0.540, 1.12)]
SEPAL_WIDE = [0.15, 0.31, 0.31, 0.17, 0.03]


def drift(p, much=1.0):
    """She floats, and what hangs from her trails behind her and a little to her right: more the lower it hangs."""
    k = max(0.0, min(1.0, 1.0 - p[1] / WAIST)) ** 2
    return (p[0] - 0.14 * k * much, p[1], p[2] - 0.32 * k * much)


def blade_on_skirt(line, wide, angle, flare=1.0, drop=1.0, out=0.0, cup=-0.22, thick=0.12, rect=R_PETAL):
    a = math.radians(angle)
    away = (math.sin(a), 0.0, math.cos(a))
    pts = [drift(((r * flare + out) * away[0], WAIST - (WAIST - y) * drop, (r * flare + out) * away[2] * SKIRT_FLAT)) for r, y in line]
    return painted(ribbon(pts, wide, thick=thick, face=away, cup=cup, seg=12, per=4), rect, 12)


def _unit(v):
    v = np.array(v, np.float32)
    return v / max(float(np.linalg.norm(v)), 1e-6)


def _near(d, c, r):
    return math.exp(-float(((d - _unit(c)) ** 2).sum()) / (r * r))


def soft_box(rows, n=32):
    """The cast's head: rings that are rounded squares (`rows`, see `HEAD_ROWS`), closed above and below."""
    rings = [[(0.0, rows[0][0], 0.0)] * (n + 1)]
    for y, half, front, back, e in rows:
        ring = []
        for j in range(n + 1):
            t = 2 * math.pi * (j % n) / n
            c, q = math.cos(t), math.sin(t)
            ring.append((math.copysign(abs(c) ** (2.0 / e), c) * half, y, math.copysign(abs(q) ** (2.0 / e), q) * (front if q >= 0 else back)))
        rings.append(ring)
    rings.append([(0.0, rows[-1][0], 0.0)] * (n + 1))
    pos = np.array([q for ring in rings for q in ring], np.float32)
    tris = []
    for r in range(len(rings) - 1):
        for j in range(n):
            i = r * (n + 1) + j
            k = i + n + 1
            tris.append((i, i + 1, k)); tris.append((i + 1, k + 1, k))
    tris = np.array(tris, np.uint32)
    i, j, k = pos[tris[:, 0]], pos[tris[:, 1]], pos[tris[:, 2]]
    if float(np.einsum("ij,ij->i", i, np.cross(j, k)).sum()) < 0:
        tris = tris[:, ::-1].copy()
    return (pos, smooth_normals(pos, tris, n), tris)


def wedge(base, tip, base_half, tip_half, across=(1, 0, 0)):
    """A lock cut from a block, as the cast's hair is: a four-sided piece from a broad `base` to a narrower `tip`, each
    end a rectangle (half its size `across`, half its depth). Flat faces, hard edges."""
    base, tip = np.array(base, np.float32), np.array(tip, np.float32)
    x = _unit(across)
    z = _unit(np.cross(x, tip - base))
    corners = [c + x * (sx * h[0]) + z * (sz * h[1]) for c, h in ((base, base_half), (tip, tip_half)) for sx, sz in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    middle = sum(corners) / 8.0
    pos, nrm, tris = [], [], []
    for quad in ((0, 1, 2, 3), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)):
        q = [corners[i] for i in quad]
        n = _unit(np.cross(q[1] - q[0], q[2] - q[0]))
        if float(np.dot(n, sum(q) / 4.0 - middle)) < 0:
            q = q[::-1]
            n = -n
        i = len(pos)
        pos += q
        nrm += [n] * 4
        tris += [(i, i + 1, i + 2), (i, i + 2, i + 3)]
    return (np.array(pos, np.float32), np.array(nrm, np.float32), np.array(tris, np.uint32))


def face_uv(mesh):
    """Her head's UVs: its front laid flat into `R_FACE` (everything outside the face's window takes the window's edge,
    which is plain skin)."""
    u0, v0, u1, v1 = inset(R_FACE)
    uv = np.zeros((len(mesh[0]), 2), np.float32)
    for i, (x, y, z) in enumerate(mesh[0]):
        a = min(1.0, max(0.0, (float(x) + FACE_HALF) / (2 * FACE_HALF)))
        if z < 0.0:
            a = 0.0 if x < 0 else 1.0                # the back of her head is skin, not her face again
        b = min(1.0, max(0.0, (float(y) - FACE_LOW) / (FACE_HIGH - FACE_LOW)))
        uv[i] = (u0 + (u1 - u0) * a, v1 - (v1 - v0) * b)
    return uv


# ⚠️ HER LONG HAIR IS ONE STREAM (owner, 2026-10-08, of seven locks that fanned out round her, each turning up at its
# end: "hair flow i like the idea but its poorly executed that it looks like octopus tentacles instead of flowwy and
# floaty hair"). What made them tentacles: separate arms, alike, radiating from one middle. Hair under water is carried
# ONE way by one current. So: it leaves her head as one mass, is carried off behind her right shoulder (the way her
# skirt drifts), and rides slow waves; it only comes apart into strands toward its end, and thin strands ride loose
# round it. `STREAM` is the line the current takes, in the hair node's space.
STREAM = [(0.00, 0.02, -0.02), (-0.10, -0.36, -0.17), (-0.32, -0.68, -0.24), (-0.62, -0.84, -0.32), (-0.95, -0.66, -0.42), (-1.26, -0.74, -0.52),
          (-1.54, -0.44, -0.62), (-1.72, -0.06, -0.70)]
STREAM_ACROSS = _unit((0.41, 0.0, -0.91))     # square to the sheet the stream lies in (it looks behind her)


def stream_line(place, depth, length, sway, beats, phase, steps=11):
    """One lock's line in the stream: `place` -1 to 1 across it (its top edge is 1 once it runs sideways), `depth` its
    layer, `length` how much of the stream it runs, and its own wave: how far, how many, where it starts."""
    spine, _ = catmull(STREAM, [0.0] * len(STREAM), 8)
    tan = np.gradient(spine, axis=0)
    tan /= np.maximum(np.linalg.norm(tan, axis=1, keepdims=True), 1e-6)
    line = []
    for j in range(steps):
        t = length * j / (steps - 1)
        at = min(len(spine) - 1.001, t * (len(spine) - 1))
        i = int(at)
        f = at - i
        q = spine[i] * (1 - f) + spine[i + 1] * f
        along = tan[i] * (1 - f) + tan[i + 1] * f
        across = np.cross(STREAM_ACROSS, along)
        across /= max(float(np.linalg.norm(across)), 1e-6)
        spread = 0.125 + 0.26 * t ** 1.3
        q = q + across * (place * spread + sway * t * math.sin(2 * math.pi * (beats * t + phase))) + STREAM_ACROSS * depth * (0.025 + 0.10 * t)
        line.append(tuple(float(v) for v in q))
    return line


LOCK_SHAPE = [0.55, 0.90, 1.00, 1.00, 0.98, 0.94, 0.86, 0.72, 0.50, 0.24, 0.015]


def main():
    m = Model("makiling")
    m.folder = Path(arg("out", kit.ROOT / "Assets/TumbangPreso/Resources/Models/PaeteProps"))
    m.folder.mkdir(parents=True, exist_ok=True)
    paint_atlas(m.folder / "makiling-atlas.png")
    m.texture = "makiling-atlas.png"

    # ================================================================ her body
    body = []
    # THE WISP she ends in instead of feet: it trails behind her to a point.
    wisp = lathe([(0.0, 0.10), (0.045, 0.20), (0.105, 0.45), (0.175, 0.82), (0.230, 1.20), (0.215, 1.50), (0.168, 1.70)], seg=24, squash_z=SKIRT_FLAT)
    body.append(keep(painted(wisp, R_TAIL, 24, "axis", 1), reshaped(wisp, lambda p: drift(p, 2.1), 24)))

    # HER SKIRT, a sampaguita hung upside down. Each petal typed: where round her, how wide it stands, how long it
    # hangs, how broad it is. The inner ring hangs longer and shows in the gaps of the outer.
    for angle, flare, drop, wide in [(28, 0.80, 1.14, 0.92), (88, 0.84, 1.20, 1.00), (150, 0.78, 1.10, 0.90), (212, 0.86, 1.22, 1.04), (268, 0.80, 1.12, 0.92),
                                     (330, 0.82, 1.18, 0.96)]:
        body.append(blade_on_skirt(PETAL_LINE, [w * wide for w in PETAL_WIDE], angle, flare, drop, out=-0.02))
    for angle, flare, drop, wide in [(-4, 1.00, 1.00, 1.04), (58, 0.96, 0.93, 0.94), (121, 1.04, 1.03, 1.06), (181, 1.06, 1.06, 1.10), (243, 0.97, 0.95, 0.96),
                                     (301, 1.02, 0.99, 1.00)]:
        body.append(blade_on_skirt(PETAL_LINE, [w * wide for w in PETAL_WIDE], angle, flare, drop))

    # HER TAPIS, the flower's calyx: woven green sepals over the petals. The one at her left hip is the cloth's free
    # end and hangs long.
    for angle, drop, wide in [(20, 1.00, 1.00), (93, 1.62, 0.92), (166, 1.06, 1.04), (236, 0.94, 0.96), (311, 1.10, 1.00)]:
        body.append(blade_on_skirt(SEPAL_LINE, [w * wide for w in SEPAL_WIDE], angle, 1.0, drop, out=0.045, cup=-0.16, thick=0.14, rect=R_SEPAL))
    # Her sash, gold, knotted at her left hip with two ends.
    def on_waist(angle, y, out=0.0):
        a = math.radians(angle)
        return ((0.176 + out) * math.sin(a), y, (0.176 + out) * math.cos(a) * 0.76)
    body.append((tube([on_waist(a, 1.715) for a in range(0, 361, 12)], 0.026, seg=8), GOLD))
    knot = on_waist(72, 1.715, 0.030)
    body.append((ellipsoid((0.050, 0.040, 0.042), at=knot), GOLD))
    for dx, dz, length in ((0.050, 0.030, 0.42), (0.012, 0.055, 0.30)):
        body.append((ribbon([knot, (knot[0] + dx, knot[1] - length * .5, knot[2] + dz), (knot[0] + dx * 1.9, knot[1] - length, knot[2] + dz * .6)],
                            [0.05, 0.062, 0.02], thick=0.3, face=(0.6, 0, 0.8), seg=8, per=3), GOLD))

    # THE CAMISA: fitted, a small waist, a narrow neck under the pañuelo.
    camisa = lathe([(0.168, 1.66), (0.152, 1.74), (0.174, 1.86), (0.198, 1.96), (0.182, 2.05), (0.122, 2.12), (0.060, 2.16)], seg=24, squash_z=0.72)
    body.append(painted(camisa, R_CAMISA, 24, "axis", 1))
    # THE PAÑUELO: a small folded kerchief over her shoulders.
    panuelo = lathe([(0.262, 2.005), (0.252, 2.045), (0.200, 2.105), (0.106, 2.165), (0.060, 2.180)], seg=24, squash_z=0.76)
    body.append(painted(panuelo, R_PANUELO, 24, "axis", 1))
    # A GARLAND of sampaguita round her neck, falling to her breast: each flower typed (how far round, how big).
    def on_garland(k):
        a = math.radians(-78 + 156 * k)
        return (0.150 * math.sin(a), 2.100 - 0.250 * math.cos(a) ** 1.5, 0.060 + 0.150 * math.cos(a))
    body.append((tube([on_garland(k / 16) for k in range(17)], 0.008, seg=5), LEAF))
    for k, size in [(0.03, .044), (0.13, .050), (0.22, .046), (0.32, .054), (0.42, .050), (0.50, .066), (0.59, .050), (0.69, .054), (0.78, .046), (0.88, .050), (0.97, .044)]:
        at = on_garland(k)
        body += blossom(at, (72, math.degrees(math.atan2(at[0], at[2] - 0.02)), 0), size)
    # (Her neck is part of her head now: see `_with_neck`.)
    m.add("body", body)

    # ================================================================ her arms: the arm hangs to the elbow, the forearm comes forward
    for name, sx in (("arm-left", 1.0), ("arm-right", -1.0)):
        elbow, wrist = (0.100 * sx, -0.270, 0.050), (-0.092 * sx, -0.135, 0.285)
        hand = (-0.142 * sx, -0.112, 0.322)
        upper = tube([(0.004 * sx, 0.020, 0.0), (0.062 * sx, -0.120, 0.012), elbow], [0.068, 0.056, 0.048], seg=12)
        arm = [painted(upper, R_CAMISA, 12),
               (tube([elbow, (0.000 * sx, -0.200, 0.170), wrist], [0.042, 0.038, 0.033], seg=10), SKIN),
               (tube([wrist, hand], [0.032, 0.030], seg=10), SKIN),
               (moved(ellipsoid((0.044, 0.025, 0.058)), turn=(14, -38 * sx, 0), at=hand), SKIN)]
        # THE LILY SLEEVE: a trumpet from above her elbow, opening before her wrist, six soft lobes at its lip, hollow.
        bell = lathe([(0.058, -0.040), (0.052, 0.040), (0.062, 0.120), (0.082, 0.190), (0.104, 0.236), (0.114, 0.258), (0.100, 0.244), (0.062, 0.180), (0.0, 0.150)],
                     seg=36)
        pos = bell[0].copy()
        for i in range(len(pos)):
            lobe = 1.0 + 0.07 * float(smooth01(0.10, 0.26, float(pos[i, 1]))) * math.cos(6 * math.atan2(float(pos[i, 2]), float(pos[i, 0])))
            pos[i, 0] *= lobe; pos[i, 2] *= lobe
        lobed = (pos, smooth_normals(pos, bell[2], 36), bell[2])
        arm.append((aimed(lobed, elbow, wrist), kit.grid_uv(bell, inset(R_SLEEVE), 36, "axis", 1)))
        m.add(name, arm, at=(0.200 * sx, 2.085, -0.010), parent="body")

    # THE LIGHT she gives him, a bud of it over her cupped hands.
    m.add("seed", [(lathe([(0.0, -0.066), (0.050, -0.030), (0.062, 0.004), (0.044, 0.052), (0.0, 0.108)], seg=14), LIGHT)], at=(0.0, 2.035, 0.335), parent="body")

    # ================================================================ her head
    skull = soft_box(HEAD_ROWS, n=40)
    head = [(skull, face_uv(skull))]                 # her face is painted on it (`paint_atlas`)
    for sx in (1, -1):
        # A gold drop at each ear, under her hair.
        ear = _row_at(HEAD_ROWS, 0.130)[1] + 0.018
        head.append((ellipsoid((0.013, 0.013, 0.013), at=(ear * sx, 0.112, 0.020)), GOLD))
        head.append((lathe([(0.0, -0.066), (0.019, -0.048), (0.022, -0.030), (0.011, -0.008), (0.0, 0.0)], seg=10, at=(ear * sx, 0.104, 0.020)), GOLD))
    # HER HAIR over her skull, and a fringe parted in the middle: thick rounded locks, each its own length (the outer
    # ones longest, framing her face). Each typed: where it leaves her hair, where its tip lies, how low, how broad.
    head.append((soft_box(HAIR_ROWS, n=40), HAIR))
    # ⚠️ THE FRINGE COMES DOWN TO HER EYES (owner, of an oval head whose face sat high and whose fringe was six short
    # tips at her hairline: "her face is too high making her chin look bloated, and the fringe got ruined"). Her face
    # is 47 mm lower on her head now, and the fringe hangs over her whole brow: short at the parting, long at her temples.
    top = 0.452
    for x0, x1, low, wide in [(0.026, 0.058, 0.300, 0.096), (0.082, 0.120, 0.250, 0.104), (0.134, 0.166, 0.204, 0.094),
                              (-0.032, -0.066, 0.288, 0.096), (-0.086, -0.126, 0.236, 0.104), (-0.136, -0.168, 0.216, 0.094)]:
        line = []
        for k in range(5):
            f = k / 4.0
            x, y = x0 + (x1 - x0) * f, top + (low - top) * f
            line.append((x, y, brow_z(x, y) + 0.006 + 0.014 * math.sin(f * math.pi)))
        fringe = ribbon(line, [wide * .80, wide, wide, wide * .78, wide * .26], thick=0.42, face=(0, 0, 1), cup=-0.10, seg=10, per=4)
        head.append(painted(fringe, R_HAIR, 10))
    # A long lock by each cheek. Her left one falls to her breast; her right one is lifted off her shoulder by the
    # current that carries the rest.
    # ⚠️ They leave from UNDER her hair (owner, circling a square shoulder each side of her head where a lock began on
    # her hair's surface with a flat top): each starts thin and inside it, and only stands out below where her hair ends.
    side = _row_at(HAIR_ROWS, 0.300)[1]
    left = [(side - 0.060, 0.420, 0.040), (side - 0.036, 0.300, 0.070), (side - 0.014, 0.150, 0.088), (side + 0.004, -0.080, 0.085), (side - 0.010, -0.290, 0.092),
            (side + 0.016, -0.430, 0.100)]
    head.append(painted(ribbon(left, [0.04, 0.09, 0.13, 0.12, 0.09, 0.012], thick=0.40, face=(0.3, 0, 1), seg=12, per=4), R_HAIR, 12))
    right = [(-side + 0.060, 0.420, 0.040), (-side + 0.036, 0.300, 0.070), (-side + 0.012, 0.150, 0.085), (-side - 0.030, -0.020, 0.065), (-side - 0.132, -0.150, 0.045),
             (-side - 0.270, -0.195, 0.005), (-side - 0.402, -0.115, -0.040), (-side - 0.502, 0.010, -0.070)]
    head.append(painted(ribbon(right, [0.04, 0.09, 0.13, 0.12, 0.10, 0.085, 0.055, 0.010], thick=0.40, face=(-0.3, 0, 1), seg=12, per=4), R_HAIR, 12))
    m.add("head", head, at=(0.0, 2.210, 0.012), parent="body")

    # HER LONG HAIR: the stream (see `STREAM`). Each lock typed, none like another: where across the stream, its layer,
    # how far it runs, how broad, and its own wave (how far, how many, where it starts).
    long_hair = []
    lines = []
    for place, depth, length, wide, sway, beats, phase in [
            (-0.95, 0.2, 0.78, 0.30, 0.07, 1.3, 0.10), (-0.70, -0.5, 0.95, 0.34, 0.09, 1.1, 0.42), (-0.48, 0.6, 0.66, 0.28, 0.06, 1.6, 0.80),
            (-0.25, -0.2, 1.00, 0.36, 0.10, 1.0, 0.25), (-0.05, 0.8, 0.84, 0.30, 0.08, 1.4, 0.63), (0.14, -0.7, 0.92, 0.34, 0.11, 1.2, 0.05),
            (0.33, 0.3, 0.72, 0.28, 0.07, 1.7, 0.50), (0.52, -0.3, 0.98, 0.34, 0.12, 0.9, 0.88), (0.72, 0.5, 0.80, 0.30, 0.09, 1.3, 0.33),
            (0.93, -0.6, 0.62, 0.26, 0.08, 1.5, 0.71),
            # thin strands riding loose round it
            (-1.15, -0.9, 0.70, 0.07, 0.16, 1.9, 0.20), (-0.40, -1.0, 1.04, 0.06, 0.18, 1.5, 0.66), (0.25, -1.0, 0.88, 0.07, 0.15, 2.1, 0.37),
            (0.80, -0.9, 1.02, 0.06, 0.20, 1.4, 0.92), (1.18, 0.4, 0.74, 0.07, 0.17, 1.8, 0.55)]:
        line = stream_line(place, depth, length, sway, beats, phase)
        lines.append(line)
        long_hair.append(painted(ribbon(line, [wide * k for k in LOCK_SHAPE], thick=0.40, face=tuple(-STREAM_ACROSS), cup=0.10, seg=10, per=3), R_HAIR, 10))
    # What does not go with the stream at once: the hair down her back and her left side, which hangs, and is only
    # turned toward the current at its ends.
    for line, wide in [([(0.10, 0.02, -0.03), (0.17, -0.45, -0.16), (0.20, -0.95, -0.24), (0.12, -1.40, -0.30), (-0.06, -1.72, -0.38), (-0.30, -1.86, -0.48)],
                        [0.20, 0.32, 0.34, 0.30, 0.20, 0.03]),
                       ([(0.02, 0.02, -0.04), (0.04, -0.55, -0.19), (0.00, -1.10, -0.28), (-0.14, -1.55, -0.34), (-0.38, -1.80, -0.42), (-0.62, -1.80, -0.52)],
                        [0.22, 0.36, 0.38, 0.32, 0.20, 0.03]),
                       ([(0.15, 0.00, -0.01), (0.25, -0.40, -0.10), (0.30, -0.85, -0.14), (0.24, -1.20, -0.18), (0.10, -1.42, -0.24)],
                        [0.10, 0.16, 0.16, 0.10, 0.02])]:
        long_hair.append(painted(ribbon(line, wide, thick=0.40, face=(0, 0, -1), cup=0.10, seg=10, per=5), R_HAIR, 10))
    # Sampaguita caught in the stream: each typed (which lock, which point along it, how big).
    toward = -STREAM_ACROSS
    for which, point, size in [(1, 4, .086), (3, 6, .078), (5, 3, .074), (7, 5, .092), (8, 7, .070), (3, 9, .062), (0, 6, .070)]:
        at = lines[which][point]
        long_hair += blossom((at[0] + float(toward[0]) * 0.06, at[1] + 0.01, at[2] + float(toward[2]) * 0.06), (64, -24, 0), size)
    m.add("hair", long_hair, at=(0.0, 0.345, -0.150), parent="head")

    # THE WREATH: sampaguita round her head on a green vine, lower at her brow and higher behind, and a cluster of three
    # large flowers and two leaves at her left temple.
    crown = []
    def wreath_at(angle, out=0.0):
        # Round her hair where it sits: lower at her brow, higher behind.
        a = math.radians(angle)
        c, q = math.sin(a), math.cos(a)
        lift = 0.418 + 0.034 * (1 - q) * .5
        _, half, front, back, e = _row_at(HAIR_ROWS, lift)
        # ⚠️ Over her brow it rides OUTSIDE her fringe (owner, circling three flowers that sat half sunk in it).
        out += 0.036 * max(0.0, q) ** 0.5
        x = math.copysign(abs(c) ** (2.0 / e), c) * (half + 0.004 + out)
        z = math.copysign(abs(q) ** (2.0 / e), q) * ((front if q >= 0 else back) + 0.004 + out)
        return (x, lift, z)
    crown.append((tube([wreath_at(a) for a in range(0, 361, 6)], 0.011, seg=6), LEAF))
    for angle, size, tip in [(-6, .074, 60), (-40, .062, 68), (-78, .070, 58), (-118, .058, 66), (-160, .066, 60), (158, .060, 68), (122, .066, 58),
                             (88, .104, 52), (58, .118, 46), (34, .092, 56), (12, .060, 66)]:
        crown += blossom(wreath_at(angle, 0.014), (tip, angle, 0), size)
    for angle, to, wide in [(74, (0.13, 0.10, 0.02), 0.085), (46, (0.10, 0.15, 0.06), 0.075), (-58, (-0.08, 0.07, 0.03), 0.055), (-140, (-0.07, 0.06, -0.05), 0.055)]:
        at = wreath_at(angle, 0.004)
        crown.append(leaf_blade(at, (at[0] + to[0], at[1] + to[1], at[2] + to[2]), wide, face=(0, 0, 1) if abs(angle) < 100 else (0, 0, -1), bow=0.015))
    m.add("flower-crown", crown, parent="head")
    m.write(PALETTE)


if __name__ == "__main__":
    main()
