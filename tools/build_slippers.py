"""The ten slippers, remodelled in the game's own dress: chunky rounded toys with a hand-painted atlas.

Owner, 2026-10-11: "need you to create new models for the slippers that matches our fun cutesy artstyle"; of the first
one: "do the others and give me a comparison sheet ... in each cell give me a top isometric view, and a bottom isometric
view from the opposite end", "i'd prefer if you optimized the slipper models too, and ensure it looks seamless, not
multiple parts intersected together".

The roster's shoes are downloaded models (tools/build_slipper_models.py: Sketchfab sources put in "as is"), 0.1 to 54 MB
each, with their own realistic proportions and photo-ish textures. These are built the way the redesigned cast and
Paete's plants are (docs/CHARACTER_REDESIGN_DANTE.md 15.11): round forms, smooth normals, a painted atlas.

THREE STEPS:
  1. py -3 tools/build_slippers.py [id ...] [--out=folder]
       types each shoe as a list of closed PIECES (add or cut) and paints its atlas: <out>/parts/<id>.glb, <id>-atlas.png
  2. blender -b --python tools/weld_slippers.py -- [id ...] [--out=folder]
       ⚠️ SEAMLESS: joins the pieces into ONE closed skin (exact boolean union, the cuts taken out), so no strap pokes
       through a sole and nothing overlaps inside; writes <out>/<id>.glb with the atlas inside it.
  3. blender -b --python tools/review_slippers.py -- [--out=folder], then py -3 tools/build_slippers.py sheet
       the comparison sheet: Logs/slippers/slippers_<tag>.png

THE CONTRACT WITH THE GAME (read from build_slipper_models.py and MatchInstaller.BuildSlipper, not chosen here):
  * ONE shoe, toe along +X, y up, sole seated on y = 0, centred on x and z.
    ⚠️ WRONG WAY ROUND FOR THE GAME (owner, 2026-10-18, in hand: "the slippers are backwards"): the game's shoes carry
    the toe along -X. The shoes here are still built toe +X, and `tools/slipper_turn.py <id>.glb <Art/models/tsinelas_id.glb>`
    turns each one half round as it is installed. Never install a built glb without it.
  * 0.432 m toe to heel: the mesh's own units are the gameplay size (the prop is instantiated with no rescale), and the
    hit radius is a fixed 0.23 m.
  * It is dressed by ToonSkin.ApplySlipper with NO palette, so every colour is PAINT (the atlas's upper half).

Until the owner says yes these are written to Logs/slippers/build (the default --out), not into Assets.
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import hand_companion_kit as kit  # noqa: E402
from hand_companion_kit import Model, ellipsoid, grid_uv, moved, rounded, tube  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
LENGTH = 0.432
HALF = LENGTH * 0.5
ATLAS = 1024

# Rects of the atlas (u0, v0, u1, v1), v down. Everything sits in the upper half.
R_TOP, R_BOTTOM = (0.0, 0.0, 0.5, 0.25), (0.5, 0.0, 1.0, 0.25)
R_SIDE, R_UPPER = (0.0, 0.25, 0.5, 0.375), (0.5, 0.25, 1.0, 0.5)
R_STRAP, R_KNOB, R_LINING = (0.0, 0.375, 0.25, 0.5), (0.25, 0.375, 0.375, 0.5), (0.375, 0.375, 0.5, 0.5)


def arg(name, default=None):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


def hexc(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)], np.float32)


def smooth01(a, b, x):
    t = np.clip((np.asarray(x, np.float32) - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


# ------------------------------------------------------------------ meshes

def as_mesh(pos, tris):
    pos = np.asarray(pos, np.float32)
    return pos, np.tile(np.array([0, 1, 0], np.float32), (len(pos), 1)), np.asarray(tris, np.uint32)


class Sole:
    """A thick rounded sole as ONE closed piece: a plan longer than wide and fatter at the forefoot, a rounded edge top
    and bottom, a toe that springs up, a heel a little thicker. `rise` (x -> metres) replaces the toe spring for a shoe
    whose sole climbs (a high heel)."""

    def __init__(self, thick=0.052, round_=0.018, heel=0.074, ball=0.098, spring=0.026, wedge=0.010, power=0.72, rise=None):
        self.thick, self.round, self.heel, self.ball, self.spring, self.wedge, self.power, self.rise = thick, round_, heel, ball, spring, wedge, power, rise

    def width(self, x):
        u = (x / HALF + 1) * 0.5
        return float(self.heel + (self.ball - self.heel) * smooth01(0.15, 0.75, u))

    def lift(self, x):
        if self.rise:
            return float(self.rise(x))
        return self.spring * float(smooth01(0.45, 1.0, x / HALF)) ** 1.6

    def top(self, x):
        return self.thick + self.wedge * float(smooth01(0.2, -0.9, x / HALF)) + self.lift(x)

    def plan(self, angle, inset=0.0):
        c, s = math.cos(angle), math.sin(angle)
        x = (HALF - inset) * math.copysign(abs(c) ** self.power, c)
        return x, max(self.width(x) - inset, 0.004) * math.copysign(abs(s) ** self.power, s)

    def span(self, x, inset=0.0):
        """Half the plan's width at x, `inset` in from its edge: 0 past its ends."""
        c = min(1.0, abs(x) / max(HALF - inset, 1e-6)) ** (1.0 / self.power)
        return max(self.width(x) - inset, 0.0) * max(0.0, 1.0 - c * c) ** (self.power * 0.5)

    def piece(self, n=48):
        r = self.round
        # (inset, offset, from the top?, which rect, t in the side rect)
        rings = [(r, 0.0, 0), (r * 0.30, r * 0.30, 0), (0.0, r, 0), (0.0, None, 0), (0.0, -r, 1), (r * 0.30, -r * 0.30, 1), (r, 0.0, 1)]
        pos, uv, tris = [], [], []

        def point(i, inset, off, from_top, shrink=1.0):
            x, z = self.plan(2 * math.pi * (i % n) / n, inset)
            x, z = x * shrink, z * shrink
            if off is None:
                return x, (self.lift(x) + self.top(x)) * 0.5, z
            return x, (self.top(x) if from_top else self.lift(x)) + off, z

        def flat(p, rect):
            return rect[0] + (rect[2] - rect[0]) * (p[0] / LENGTH + 0.5), rect[1] + (rect[3] - rect[1]) * (p[2] / 0.24 + 0.5)

        rows = []
        # the bottom cap, centre out
        for shrink in (0.0, 0.45, 0.75):
            rows.append([(point(i, r, 0.0, 0, shrink), R_BOTTOM) for i in range(n + 1)])
        rows.append([(point(i, r, 0.0, 0), R_BOTTOM) for i in range(n + 1)])
        for k, (inset, off, from_top) in enumerate(rings):
            rows.append([(point(i, inset, off, from_top), ("side", k / (len(rings) - 1), i / n)) for i in range(n + 1)])
        rows.append([(point(i, r, 0.0, 1), R_TOP) for i in range(n + 1)])
        for shrink in (0.75, 0.45, 0.0):
            rows.append([(point(i, r, 0.0, 1, shrink), R_TOP) for i in range(n + 1)])
        for row in rows:
            for p, where in row:
                pos.append(p)
                if isinstance(where[0], str):
                    uv.append((R_SIDE[0] + (R_SIDE[2] - R_SIDE[0]) * where[2], R_SIDE[3] - (R_SIDE[3] - R_SIDE[1]) * where[1]))
                else:
                    uv.append(flat(p, where))
        for k in range(len(rows) - 1):
            # The rows where the paint changes rect are doubled (same place, other UV): no faces between those.
            if rows[k][0][0] == rows[k + 1][0][0] and rows[k][1][0] == rows[k + 1][1][0]:
                continue
            for i in range(n):
                a = k * (n + 1) + i
                b = a + n + 1
                tris.append((a, b + 1, b)); tris.append((a, a + 1, b + 1))
        return as_mesh(pos, tris), np.array(uv, np.float32)


def dome(sole, x0, x1, height, rect, inset=0.002, sink=0.014, slope=0.0, seg=24, rings=14, square=2.0):
    """An upper: a shell standing on the footbed from x0 to x1, `height` tall (`slope` > 0 makes the heel end taller).
    ⚠️ IT FOLLOWS THE SOLE'S OWN PLAN, edge to edge (owner, 2026-10-11, of uppers that were plain eggs on a boxier
    sole: "dont cover the extent of the sole, so theres parts on the sole visible at the front tip of the shoe"):
    at every x it is as wide as the sole is there, less `inset`, so a toe cap reaches the tip and its corners.
    Its lower half is sunk into the sole so the join is clean."""
    e = ellipsoid((1, 1, 1), seg=seg, rings=rings)
    uv = grid_uv(e, rect, seg)
    n = e[0]
    cx, rx = (x0 + x1) * 0.5, (x1 - x0) * 0.5
    pos = np.zeros_like(n)
    for i, (nx, ny, nz) in enumerate(n):
        nx, ny, nz = float(nx), float(ny), float(nz)
        x = cx + rx * nx
        across = math.sqrt(max(1e-9, 1.0 - nx * nx))
        f = max(-1.0, min(1.0, nz / across))                     # where across the shoe: -1 one edge, 1 the other
        # `square` 2 is a round arch across the shoe; more is steeper sides and a flatter top (a trainer's wall).
        up = max(0.0, 1.0 - abs(f) ** square) ** (1.0 / square) * across
        y = sole.top(x) + (height * (1 - slope * nx) * up if ny >= 0 else -sink * up)
        pos[i] = (x, y, sole.span(x, inset) * f)
    return (pos, e[1], e[2]), uv


def panelled(mesh, sole, rect_side, rect_far, rect_top, tall):
    """The same shell with its paint laid out as a shoemaker cuts it: each SIDE is its own flat panel seen square-on from
    outside (the near side, +z, heel on the left; the far side toe on the left, so a WORD reads forward on both; v welt
    to `tall` above the footbed), and the TOP is one panel seen from above.
    ⚠️ WHY (owner, 2026-10-11, of a tick painted on a shell that was unwrapped round its crown like a globe, so it bent
    and smeared: "fix the topology for sike so u can actually put on a comprehensible logo"). A drawing put on the
    side panel lands on the shoe as drawn. Every triangle gets its own corners (the welder joins the points again and
    keeps each face's paint), so the panels meet at a seam instead of stretching into each other."""
    pos, _, tris = mesh
    out, uv = [], []
    for tri in tris:
        a, b, c = (pos[int(i)] for i in tri)
        n = np.cross(b - a, c - a)
        mid = (a + b + c) / 3.0
        level = abs(float(n[1])) / max(float(np.linalg.norm(n)), 1e-12)
        top = level > 0.80 and float(mid[1]) > sole.top(float(mid[0])) + tall * 0.25
        for q in (a, b, c):
            x, y, z = (float(v) for v in q)
            out.append(q)
            if top:
                uv.append((rect_top[0] + (rect_top[2] - rect_top[0]) * (x / LENGTH + 0.5), rect_top[1] + (rect_top[3] - rect_top[1]) * (z / 0.24 + 0.5)))
            else:
                h = min(1.0, max(0.0, (y - sole.top(x)) / tall))
                r, along = (rect_side, x / LENGTH + 0.5) if float(mid[2]) >= 0 else (rect_far, 0.5 - x / LENGTH)
                uv.append((r[0] + (r[2] - r[0]) * along, r[3] - (r[3] - r[1]) * h))
    count = len(out)
    return as_mesh(out, np.arange(count, dtype=np.uint32).reshape(-1, 3)), np.array(uv, np.float32)


def hollow(sole, x0, x1, height, mouth_x, mouth_r, wall=0.011, toward=-1, inset=0.002, rect=R_LINING):
    """The two cutters that turn a `dome` of the same numbers into a shoe's upper: its INSIDE (the same shell, `wall`
    smaller, so the skin is one even thickness), and its MOUTH: a big egg whose nose is at `mouth_x` on the centre
    line and which takes away everything of the upper behind it (`toward` -1, the heel side) or ahead of it (+1).
    ⚠️ THE MOUTH IS ITS OWN CUT, NOT THE INSIDE RUN OUT THROUGH THE SKIN (the first try): two near-parallel shells
    cross at a grazing angle and the edge comes out in stairs. The egg meets the skin steeply, so the edge is clean.
    A `mouth_r` narrower than the shoe leaves the side walls standing (a clog); wider, it cuts straight across (a mule)."""
    inside = dome(sole, x0 + wall * 1.2, x1 - wall * 1.5, height - wall, rect, inset=inset + wall, sink=0.004, seg=20, rings=12)
    mouth = ball(mouth_r, (mouth_x + toward * mouth_r[0], sole.top(mouth_x) + 0.004, 0.0), rect, seg=28, rings=16)
    return [("cut", inside), ("cut", mouth)]


def ball(half, at, rect, seg=14, rings=8, turn=(0, 0, 0)):
    e = ellipsoid(half, seg=seg, rings=rings)
    return moved(e, at=at, turn=turn), grid_uv(e, rect, seg)


def block(half, radius, at, rect, turn=(0, 0, 0)):
    e = rounded(half, radius, seg=12, rings=8)
    return moved(e, at=at, turn=turn), grid_uv(e, rect, 12)


def bezier(p0, p1, p2, p3, count=12):
    p0, p1, p2, p3 = (np.array(p, np.float32) for p in (p0, p1, p2, p3))
    return [((1 - t) ** 3) * p0 + 3 * ((1 - t) ** 2) * t * p1 + 3 * (1 - t) * t * t * p2 + (t ** 3) * p3 for t in np.linspace(0, 1, count)]


def cord(points, radius, rect=R_STRAP, seg=10):
    m = tube(points, radius, seg=seg)
    return m, grid_uv(m, rect, seg)


def band(sole, xc, half_x, height, rect=R_STRAP, thick=0.011, inset=0.010, count=13, seg=10, lean=0.0):
    """A wide strap arching over the foot from one edge of the sole to the other at xc: `half_x` front to back,
    `height` over the footbed. Its ends go down into the sole. `lean` tips its crown toward the heel."""
    w = sole.width(xc) - inset
    base = sole.top(xc) - 0.014
    path = [(-lean * math.cos(a), base + (height + 0.014) * math.cos(a), w * math.sin(a)) for a in np.linspace(-math.pi / 2, math.pi / 2, count)]
    m = tube(path, thick, seg=seg)
    return moved(m, at=(xc, 0, 0), scale=(half_x / thick, 1, 1)), grid_uv(m, rect, seg)


# ------------------------------------------------------------------ paint

class Paint:
    def __init__(self, base="#808080"):
        self.img = np.zeros((ATLAS, ATLAS, 3), np.float32)
        self.img[:] = hexc(base)
        self.rng = np.random.default_rng(7)

    def grid(self, rect):
        """(u, t) over a rect: u left to right, t 0 at the rect's BOTTOM edge to 1 at its top; and its pixel box."""
        x0, y0, x1, y1 = (int(round(v * ATLAS)) for v in rect)
        u = (np.arange(x0, x1) + .5 - x0) / (x1 - x0)
        t = 1.0 - (np.arange(y0, y1) + .5 - y0) / (y1 - y0)
        u, t = np.meshgrid(u, t)
        return u, t, (x0, y0, x1, y1)

    def fill(self, rect, base, paint=None):
        """Fills a rect with `base`, then lets `paint(u, t, c) -> c` work on it."""
        u, t, (x0, y0, x1, y1) = self.grid(rect)
        c = np.zeros(u.shape + (3,), np.float32) + hexc(base)
        if paint:
            c = paint(u, t, c)
        self.img[y0:y1, x0:x1] = np.clip(c, 0, 1)

    def draw(self, rect, pen):
        """`pen(d, w, h)` draws with PIL on the rect as it is (x right, y DOWN, w by h pixels at four times the atlas's
        size, so its edges come out smooth): for a shape that must be read, which soft blobs cannot do."""
        from PIL import Image, ImageDraw
        x0, y0, x1, y1 = (int(round(v * ATLAS)) for v in rect)
        big = Image.fromarray((np.clip(self.img[y0:y1, x0:x1], 0, 1) * 255 + .5).astype(np.uint8)).resize(((x1 - x0) * 4, (y1 - y0) * 4), Image.BICUBIC)
        pen(ImageDraw.Draw(big), big.size[0], big.size[1])
        self.img[y0:y1, x0:x1] = np.asarray(big.resize((x1 - x0, y1 - y0), Image.LANCZOS), np.float32) / 255.0

    def save(self, path):
        from PIL import Image
        Image.fromarray((np.clip(self.img, 0, 1) * 255 + .5).astype(np.uint8)).save(path)


def mix(c, colour, weight):
    weight = np.clip(weight, 0, 1)
    return c * (1 - weight[..., None]) + hexc(colour) * weight[..., None]


def blob(u, t, cu, ct, ru, rt, soft=0.35):
    d = np.sqrt(((u - cu) / ru) ** 2 + ((t - ct) / rt) ** 2)
    return 1.0 - smooth01(1.0 - soft, 1.0, d)


def footprint(u, t):
    """The print a bare foot wears into a footbed: heel, outer edge, ball, five toes. u heel to toe, t across."""
    w = blob(u, t, 0.21, 0.50, 0.125, 0.215)
    w = np.maximum(w, blob(u, t, 0.42, 0.58, 0.14, 0.11))
    w = np.maximum(w, blob(u, t, 0.63, 0.50, 0.105, 0.265))
    for cu, ct, r in ((0.815, 0.27, 0.050), (0.835, 0.41, 0.040), (0.835, 0.53, 0.036), (0.822, 0.64, 0.033), (0.798, 0.74, 0.030)):
        w = np.maximum(w, blob(u, t, cu, ct, r, r * 2.1, 0.45))
    return w


def footbed(base, worn, deep=None, extra=None):
    def paint(u, t, c):
        f = footprint(u, t)
        c = mix(c, worn, f * 0.85)
        if deep:
            c = mix(c, deep, f * blob(u, t, 0.20, 0.50, 0.09, 0.15) * 0.6)
        return extra(u, t, c) if extra else c
    return paint


def tread(line, kind="wave", count=13, worn=None):
    def paint(u, t, c):
        if kind == "wave":
            w = np.abs(((u * count + 0.35 * np.sin(t * 2 * np.pi * 3)) % 1.0) - 0.5) * 2
            m = smooth01(0.55, 0.85, w)
        elif kind == "lug":
            m = smooth01(0.30, 0.20, np.abs((u * count) % 1.0 - 0.5)) * smooth01(0.34, 0.24, np.abs((t * 5 + np.floor(u * count) % 2 * 0.5) % 1.0 - 0.5))
        elif kind == "dots":
            m = 1.0 - smooth01(0.22, 0.30, np.sqrt(((u * count) % 1.0 - 0.5) ** 2 + ((t * 6 + np.floor(u * count) % 2 * 0.5) % 1.0 - 0.5) ** 2))
        else:                                                    # herringbone
            m = smooth01(0.55, 0.8, np.abs(((u * count + np.abs(t - 0.5) * 4) % 1.0) - 0.5) * 2)
        c = mix(c, line, m * 0.85)
        if worn:
            c = mix(c, worn, np.maximum(blob(u, t, 0.2, 0.5, 0.13, 0.26), blob(u, t, 0.65, 0.5, 0.13, 0.3)) * 0.7)
        return c
    return paint


def layers(*bands):
    """The sole's edge, bottom (t 0) to top (t 1): (from, to, colour) bands with soft joins."""
    def paint(u, t, c):
        for a, b, colour in bands:
            c = mix(c, colour, smooth01(a - 0.015, a + 0.015, t) * smooth01(b + 0.015, b - 0.015, t))
        return c
    return paint


def lit(dark, light, top=0.0, pattern=None):
    """A round piece: paler toward its top (t = `top`), darker away from it; then `pattern(u, t, c)`."""
    def paint(u, t, c):
        up = 1.0 - np.abs(t - top)
        c = mix(c, dark, smooth01(0.62, 0.25, up) * 0.75)
        c = mix(c, light, smooth01(0.72, 1.0, up) * 0.7)
        return pattern(u, t, c) if pattern else c
    return paint


def cord_paint(dark, light, pattern=None):
    """A strap or a cord: a shine along one side of its round, a shade along the other."""
    def paint(u, t, c):
        r = 0.5 + 0.5 * np.cos((u - 0.25) * 2 * np.pi)
        c = mix(c, dark, (1 - r) ** 2 * 0.75)
        c = mix(c, light, r ** 6 * 0.65)
        return pattern(u, t, c) if pattern else c
    return paint


def dots(colour, nu=4, nt=7, size=0.16):
    def paint(u, t, c):
        du, dt = (u * nu) % 1.0 - 0.5, (t * nt + (np.floor(u * nu) % 2) * 0.5) % 1.0 - 0.5
        return mix(c, colour, 1.0 - smooth01(size, size + 0.06, np.sqrt((du * 0.9) ** 2 + (dt * 0.55) ** 2)))
    return paint


# ------------------------------------------------------------------ the shoes
# Each returns (pieces, paint). A piece is (op, (mesh, uv)): "add" pieces are joined into one skin, a "cut" is taken out
# of everything added BEFORE it, so an upper is added and hollowed first and the sole and straps come after.

def flip_flop(sole, strap_r=0.0135, knob=True):
    post_x = 0.118
    foot = (post_x, sole.top(post_x) - 0.014, 0.0)
    knot = (post_x - 0.012, sole.top(post_x) + 0.058, 0.0)
    pieces = [("add", sole.piece())]
    pieces.append(("add", cord([foot, ((foot[0] + knot[0]) / 2, (foot[1] + knot[1]) / 2, 0.0), knot], strap_r)))
    for sgn in (1, -1):
        end_x = -0.050
        end = (end_x, sole.top(end_x) - 0.014, sgn * (sole.width(end_x) - 0.016))
        path = bezier(knot, (knot[0] - 0.035, knot[1] + 0.022, sgn * 0.030), (end_x + 0.050, end[1] + 0.078, sgn * (abs(end[2]) + 0.012)), end)
        pieces.append(("add", cord(path, [strap_r + 0.0035 * math.sin(math.pi * i / (len(path) - 1)) for i in range(len(path))])))
    if knob:
        pieces.append(("add", ball((0.021, 0.016, 0.021), (knot[0], knot[1] + 0.004, 0.0), R_KNOB)))
    return pieces


def tsinelas():
    """"The street-game original. Thick layered sole, printed Y-strap, worn down at the heel."""
    p = Paint("#F8ECD0")
    p.fill(R_TOP, "#FFF4DC", footbed("#FFF4DC", "#EFC98C", "#D9A765", lambda u, t, c: mix(c, "#6FB2F2", blob(u, t, 0.115, 0.50, 0.035, 0.085, 0.5) * 0.8)))
    p.fill(R_BOTTOM, "#1F56B8", tread("#4C8AE6", "wave", worn="#2C66C8"))
    p.fill(R_SIDE, "#2A66D0", layers((0.33, 0.50, "#FFFDF4"), (0.50, 0.74, "#4E8FEA"), (0.74, 1.01, "#FFF4DC")))
    p.fill(R_STRAP, "#2F7DF0", cord_paint("#1B50B4", "#8CC0FF", dots("#FFFFFF")))
    p.fill(R_KNOB, "#FFC83A", lit("#E09A1E", "#FFE88A"))
    return flip_flop(Sole()), p


def spartan():
    """"Black rubber and a red Y-strap, straight from the corner shop." Flatter, squarer, plainer than the tsinelas."""
    p = Paint("#2A2A30")
    p.fill(R_TOP, "#34343C", footbed("#34343C", "#4A4A56", "#5A5A68"))
    p.fill(R_BOTTOM, "#1C1C22", tread("#3C3C48", "lug", 11, worn="#2A2A32"))
    p.fill(R_SIDE, "#24242A", layers((0.40, 0.56, "#E8352E"), (0.80, 1.01, "#3A3A44")))
    p.fill(R_STRAP, "#EE3A30", cord_paint("#A81E1C", "#FF8A78"))
    p.fill(R_KNOB, "#EE3A30", lit("#A81E1C", "#FF8A78"))
    return flip_flop(Sole(thick=0.040, round_=0.013, heel=0.078, ball=0.096, spring=0.014, wedge=0.004, power=0.62), strap_r=0.0125), p


def slide():
    """PAMBAHAY: "The scuffed slide beside the shower." One fat band, three stripes that are definitely not the brand's."""
    sole = Sole(thick=0.050, round_=0.021, heel=0.082, ball=0.102, spring=0.020, wedge=0.008, power=0.66)
    pieces = [("add", sole.piece()), ("add", band(sole, 0.055, 0.078, 0.074, thick=0.013))]
    p = Paint("#1FA39A")

    def stripes(u, t, c):
        # across the band (t along its arch): four slanted white stripes on its crown
        s = np.abs(((t - 0.5) * 9 + (u - 0.5) * 1.2) % 1.0 - 0.5)
        return mix(c, "#FFFFFF", smooth01(0.30, 0.22, s) * smooth01(0.30, 0.36, t) * smooth01(0.70, 0.64, t) * (np.abs(u - 0.25) < 0.2))

    p.fill(R_TOP, "#35C2B6", footbed("#35C2B6", "#249C92", "#1B7F78", dots("#249C92", 16, 9, 0.12)))
    p.fill(R_BOTTOM, "#158178", tread("#35C2B6", "dots", 12, worn="#1B8F86"))
    p.fill(R_SIDE, "#1FA39A", layers((0.0, 0.20, "#158178"), (0.80, 1.01, "#35C2B6")))
    p.fill(R_STRAP, "#1FA39A", cord_paint("#12766E", "#6FE0D4", stripes))
    return pieces, p


def crocs():
    """"Holes in the top, strap round the back." A fat clog: a bulbous toe box, a heel strap on two rivets."""
    sole = Sole(thick=0.054, round_=0.023, heel=0.088, ball=0.106, spring=0.022, wedge=0.006, power=0.66)
    x0, x1, high = -0.105, HALF - 0.002, 0.098
    pieces = [("add", dome(sole, x0, x1, high, R_UPPER, slope=-0.12, seg=24, rings=14)),
              *hollow(sole, x0, x1, high, 0.040, (0.190, 0.150, 0.070), wall=0.013),
              ("add", sole.piece())]
    # The rivets sit ON the toe box's side wall, a third of the way up it, and the strap swings back from them.
    hinge_x = -0.030
    hy = sole.top(hinge_x) + 0.036
    w = sole.span(hinge_x, 0.002) * 0.93
    back = (-0.198, sole.top(-0.2) + 0.044, 0.0)
    for sgn in (1, -1):
        pieces.append(("add", cord(bezier((hinge_x, hy, sgn * w), (-0.11, hy + 0.012, sgn * (w + 0.004)), (back[0] + 0.004, back[1], sgn * 0.086), back, 10), 0.013)))
        pieces.append(("add", ball((0.020, 0.020, 0.013), (hinge_x, hy, sgn * (w + 0.003)), R_KNOB)))
    p = Paint("#8FD63A")

    def holes(u, t, c):
        # the toe box: t 0 is its crown. Two rings of round holes, and a row of vents low on each side.
        c = mix(c, "#2C5A12", 1.0 - smooth01(0.030, 0.040, t))                                       # one at the crown
        c = mix(c, "#6DB828", smooth01(0.014, 0.006, np.abs(t - 0.455)))                            # the ridge round its foot
        for ring_t, n, size in ((0.13, 6, 0.024), (0.24, 10, 0.022)):
            du = ((u * n) % 1.0 - 0.5) / n
            hole = 1.0 - smooth01(size * 0.8, size, np.sqrt((du * 1.9) ** 2 + (t - ring_t) ** 2))
            c = mix(c, "#2C5A12", hole)
        vent = smooth01(0.035, 0.022, np.abs(t - 0.415)) * smooth01(0.34, 0.22, np.abs((u * 18) % 1.0 - 0.5)) * ((np.abs(u - 0.25) < 0.16) | (np.abs(u - 0.75) < 0.16))
        return mix(c, "#2C5A12", vent)

    p.fill(R_UPPER, "#8FD63A", lit("#5FA81E", "#C6F27A", 0.0, holes))
    p.fill(R_LINING, "#4E8E18", lit("#2C5A12", "#6DB828", 0.5))
    p.fill(R_TOP, "#A6E452", footbed("#A6E452", "#7FC234", "#63A222", dots("#7FC234", 22, 12, 0.10)))
    p.fill(R_BOTTOM, "#5FA81E", tread("#8FD63A", "lug", 10, worn="#6DB828"))
    p.fill(R_SIDE, "#8FD63A", layers((0.0, 0.22, "#5FA81E"), (0.82, 1.01, "#A6E452")))
    p.fill(R_STRAP, "#8FD63A", cord_paint("#5FA81E", "#C6F27A"))
    p.fill(R_KNOB, "#2A2A30", lit("#141418", "#6A6A78", 0.0, lambda u, t, c: mix(c, "#FFD23A", blob(u, t, 0.5, 0.0, 9, 0.22, 0.4))))
    return pieces, p


def pantulog():
    """"Grandma's house slipper, fur worn flat." A puffy pink scuff with a fur roll round its mouth and a pompom."""
    sole = Sole(thick=0.040, round_=0.019, heel=0.086, ball=0.104, spring=0.016, wedge=0.004, power=0.70)
    pieces = [("add", dome(sole, -0.045, HALF - 0.002, 0.092, R_UPPER)),
              *hollow(sole, -0.045, HALF - 0.002, 0.092, 0.050, (0.170, 0.160, 0.210), wall=0.014),
              ("add", sole.piece())]
    roll_x = 0.054
    w = sole.width(roll_x) - 0.012
    base = sole.top(roll_x)
    pieces.append(("add", cord([(roll_x + 0.030 * math.cos(a) ** 2, base + 0.004 + 0.086 * math.cos(a), w * math.sin(a)) for a in np.linspace(-math.pi / 2, math.pi / 2, 13)], 0.019, R_STRAP, 10)))
    pieces.append(("add", ball((0.032, 0.030, 0.032), (0.125, sole.top(0.125) + 0.084, 0.0), R_KNOB)))
    p = Paint("#F7A8C4")

    def fur(light, dark, amount=0.5):
        def paint(u, t, c):
            n = p.rng.random(u.shape).astype(np.float32)
            n = (n + np.roll(n, 1, 0) + np.roll(n, 2, 0) + np.roll(n, 3, 0)) / 4      # short strokes, not salt
            c = mix(c, light, smooth01(0.58, 0.80, n) * amount)
            return mix(c, dark, smooth01(0.42, 0.20, n) * amount)
        return paint

    p.fill(R_UPPER, "#F7A8C4", lit("#E0789E", "#FFD0E0", 0.0, fur("#FFE0EC", "#E88AAE")))
    p.fill(R_LINING, "#FFF2DA", lit("#E8D2A8", "#FFFBF0", 0.5))
    p.fill(R_TOP, "#FFF2DA", footbed("#FFF2DA", "#EED8AE", "#D8BC8A", fur("#FFFFFF", "#EAD2A6", 0.3)))
    p.fill(R_BOTTOM, "#C9A27A", tread("#E0BE98", "dots", 15, worn="#D2AE88"))
    p.fill(R_SIDE, "#F7A8C4", layers((0.0, 0.30, "#C9A27A"), (0.30, 1.01, "#F7A8C4")))
    p.fill(R_STRAP, "#FFFFFF", cord_paint("#E6DCE4", "#FFFFFF", fur("#FFFFFF", "#D8CCD6", 0.6)))
    p.fill(R_KNOB, "#FFFFFF", lit("#DCCFDA", "#FFFFFF", 0.0, fur("#FFFFFF", "#D8CCD6", 0.6)))
    return pieces, p


def alpombra():
    """"The good pair borrowed from the doorway." The velvet beaded mule: a slim wedge, a pointed toe, a gold flower."""
    sole = Sole(thick=0.030, round_=0.012, heel=0.066, ball=0.090, spring=0.012, wedge=0.030, power=0.92)
    pieces = [("add", dome(sole, -0.030, HALF - 0.002, 0.066, R_UPPER, slope=0.15)),
              *hollow(sole, -0.030, HALF - 0.002, 0.066, 0.072, (0.170, 0.130, 0.200), wall=0.010),
              ("add", sole.piece())]
    p = Paint("#8E1B3A")

    def beads(u, t, c):
        # t 0 is the crown of the vamp. A flower of gold beads on it, leaves either side, a gold rope round the mouth.
        ang, rad = u * 2 * np.pi, t / 0.30
        petal = smooth01(1.0, 0.75, rad / (0.55 + 0.45 * np.abs(np.cos(ang * 3)))) * (t < 0.30)
        c = mix(c, "#FFD24A", petal)
        c = mix(c, "#FF8A3A", (rad < 0.22).astype(np.float32))
        sparkle = 1.0 - smooth01(0.18, 0.30, np.sqrt(((u * 26) % 1.0 - 0.5) ** 2 + ((t * 30 + np.floor(u * 26) % 2 * 0.5) % 1.0 - 0.5) ** 2))
        c = mix(c, "#FFE9A0", sparkle * smooth01(0.30, 0.34, t) * smooth01(0.44, 0.40, t) * 0.9)
        return c

    p.fill(R_UPPER, "#8E1B3A", lit("#5E0E24", "#C2385E", 0.0, beads))
    p.fill(R_LINING, "#4A0C1E", lit("#2A0612", "#6E1430", 0.5))
    p.fill(R_TOP, "#F2CE6A", footbed("#F2CE6A", "#D8AC44", "#BC8E2E"))
    p.fill(R_BOTTOM, "#5A3A22", tread("#7A5636", "herring", 14, worn="#6A482C"))
    p.fill(R_SIDE, "#8E1B3A", layers((0.0, 0.18, "#5A3A22"), (0.78, 0.90, "#FFD24A"), (0.90, 1.01, "#F2CE6A")))
    return pieces, p


R_PANEL, R_FAR, R_CROWN = (0.5, 0.25, 1.0, 0.375), (0.5, 0.375, 0.875, 0.5), (0.875, 0.375, 1.0, 0.5)      # the trainer's sides and top


def sike():
    """"Definitely not the real brand." A fat little trainer: straight walls, a high heel collar, and on each side a big
    tick drawn the WRONG WAY ROUND with the word under it."""
    sole = Sole(thick=0.046, round_=0.019, heel=0.084, ball=0.100, spring=0.024, wedge=0.010, power=0.68)
    tall = 0.088
    shell, _ = dome(sole, -HALF + 0.002, HALF - 0.002, 0.084, R_UPPER, slope=0.46, seg=32, rings=18, square=3.4)
    pieces = [("add", panelled(shell, sole, R_PANEL, R_FAR, R_CROWN, tall)),
              ("cut", ball((0.086, 0.075, 0.056), (-0.104, sole.top(-0.104) + 0.110, 0.0), R_LINING, seg=20, rings=12)),
              ("add", ball((0.048, 0.012, 0.040), (-0.012, sole.top(0.0) + 0.092, 0.0), R_KNOB, turn=(0, 0, -32))),
              ("add", sole.piece())]
    p = Paint("#FFFFFF")

    def side(flip):
        def paint(u, t, c):
            # h 0 the heel, 1 the toe (the far panel is laid the other way); t 0 the welt, 1 the top of the wall.
            h = 1.0 - u if flip else u
            c = mix(c, "#DCE4F0", smooth01(0.30, 0.0, t) * 0.5)
            c = mix(c, "#2D6BE0", smooth01(0.125, 0.105, h))                                          # the heel counter
            c = mix(c, "#E3E9F3", smooth01(0.80, 0.83, h))                                            # the toe cap
            return mix(c, "#B9C4D6", smooth01(0.020, 0.010, np.abs(h - 0.815)) * 0.9)                # and its seam
        return paint

    def logo(flip):
        def pen(d, w, h):
            red, ink = (242, 58, 52), (30, 34, 48)
            X = (lambda v: (1.0 - v) * w) if flip else (lambda v: v * w)
            # The tick, the wrong way round: the long stroke climbs toward the HEEL, the short hook is at the toe end.
            pts = [(X(0.31), 0.36 * h), (X(0.565), 0.74 * h), (X(0.655), 0.50 * h)]
            for colour, wide in ((ink, 0.215), (red, 0.145)):
                d.line(pts, fill=colour, width=int(h * wide), joint="curve")
                for x, y in pts:
                    d.ellipse((x - h * wide / 2, y - h * wide / 2, x + h * wide / 2, y + h * wide / 2), fill=colour)
            from PIL import ImageFont
            try:
                font = ImageFont.truetype("arialbd.ttf", int(h * 0.19))
            except OSError:
                font = ImageFont.load_default(int(h * 0.19))
            word = d.textlength("SIKE", font=font)
            d.text(((X(0.375) - word / 2), 0.735 * h), "SIKE", fill=ink, font=font)
            for k in range(40):                                                    # stitching along the welt
                x = X(0.14 + 0.66 * k / 39)
                d.line([(x - 0.004 * w, 0.962 * h), (x + 0.004 * w, 0.962 * h)], fill=(150, 162, 184), width=max(2, int(h * 0.014)))
        return pen

    def crown(u, t, c):
        # seen from above: u 0 the heel, 1 the toe; t across. The lace bars on the instep, and the toe cap.
        on = (u > 0.50) * (u < 0.76)
        bars = smooth01(0.30, 0.20, np.abs(((u - 0.50) * 16) % 1.0 - 0.5)) * on * (np.abs(t - 0.5) < 0.17)
        c = mix(c, "#F23A34", bars)
        c = mix(c, "#E3E9F3", smooth01(0.80, 0.83, u))
        return mix(c, "#2D6BE0", smooth01(0.125, 0.105, u))

    for rect, flip in ((R_PANEL, False), (R_FAR, True)):
        p.fill(rect, "#FFFFFF", side(flip))
        p.draw(rect, logo(flip))
    p.fill(R_CROWN, "#FFFFFF", crown)
    p.fill(R_LINING, "#F23A34", lit("#B8201E", "#FF7A6A", 0.5))
    p.fill(R_KNOB, "#FFFFFF", lit("#CCD6E6", "#FFFFFF", 0.0))
    p.fill(R_TOP, "#F23A34", footbed("#F23A34", "#C82A26"))
    p.fill(R_BOTTOM, "#F23A34", tread("#FFFFFF", "herring", 12, worn="#F86A5E"))
    p.fill(R_SIDE, "#FFFFFF", layers((0.0, 0.16, "#F23A34"), (0.46, 0.56, "#2D6BE0")))
    return pieces, p


def heels():
    """"Completely impractical and brutally effective." A red stiletto: a pointed toe, a climbing sole, a spike, a bow."""
    high = 0.118
    sole = Sole(thick=0.020, round_=0.008, heel=0.046, ball=0.074, wedge=0.0, power=0.95,
                rise=lambda x: high * float(smooth01(0.25, -0.80, x / HALF)) ** 1.25)
    pieces = [("add", dome(sole, 0.020, HALF - 0.002, 0.054, R_UPPER, slope=0.2)),
              *hollow(sole, 0.020, HALF - 0.002, 0.054, 0.112, (0.130, 0.110, 0.160), wall=0.009),
              ("add", dome(sole, -HALF + 0.002, -0.085, 0.056, R_UPPER, slope=-0.3)),
              *hollow(sole, -HALF + 0.002, -0.085, 0.056, -0.172, (0.110, 0.110, 0.032), wall=0.009, toward=1),
              ("add", sole.piece())]
    hx = -0.168
    pieces.append(("add", cord([(hx, sole.lift(hx) + 0.012, 0), (hx - 0.004, high * 0.55, 0), (hx - 0.008, 0.008, 0)], [0.030, 0.015, 0.0085], R_STRAP)))
    bx = 0.140
    by = sole.top(bx) + 0.050
    for sgn in (1, -1):
        pieces.append(("add", ball((0.016, 0.014, 0.024), (bx, by, sgn * 0.024), R_KNOB, turn=(sgn * 18, 0, 0))))
    pieces.append(("add", ball((0.012, 0.012, 0.012), (bx, by, 0.0), R_KNOB)))
    p = Paint("#E01E3C")
    shine = lambda u, t, c: mix(c, "#FFFFFF", blob(u, t, 0.20, 0.16, 0.05, 0.07, 0.6) * 0.75)
    p.fill(R_UPPER, "#E01E3C", lit("#A00E28", "#FF6A7E", 0.0, shine))
    p.fill(R_LINING, "#8A1024", lit("#5A0816", "#B81430", 0.5))
    p.fill(R_TOP, "#F6D8A8", footbed("#F6D8A8", "#E2BC84"))
    p.fill(R_BOTTOM, "#FFC83A", lit("#E09A1E", "#FFE88A", 0.5))
    p.fill(R_SIDE, "#E01E3C", layers((0.0, 0.25, "#B81430"), (0.80, 1.01, "#F6D8A8")))
    p.fill(R_STRAP, "#E01E3C", cord_paint("#A00E28", "#FF8A98"))
    p.fill(R_KNOB, "#FFFFFF", lit("#E6D2DA", "#FFFFFF", 0.0, dots("#E01E3C", 3, 3, 0.16)))
    return pieces, p


def sandals():
    """"Strapped down and built for walking." A trail sandal: a thick lugged sole, two wide straps, a heel loop."""
    sole = Sole(thick=0.050, round_=0.015, heel=0.080, ball=0.100, spring=0.024, wedge=0.010, power=0.62)
    pieces = [("add", sole.piece()),
              ("add", band(sole, 0.118, 0.030, 0.050, thick=0.010)),
              ("add", band(sole, 0.010, 0.036, 0.074, thick=0.010, lean=0.012))]
    hx = -0.060
    w = sole.width(hx) - 0.014
    base = sole.top(hx) - 0.014
    back = (-0.200, sole.top(-0.2) + 0.066, 0.0)
    for sgn in (1, -1):
        pieces.append(("add", cord(bezier((hx, base, sgn * w), (hx - 0.01, base + 0.085, sgn * (w + 0.010)), (back[0], back[1], sgn * 0.070), back, 10), 0.0125)))
        pieces.append(("add", cord([(0.010, sole.top(0.01) + 0.030, sgn * (sole.width(0.01) - 0.012)), (-0.060, sole.top(-0.06) + 0.050, sgn * (w + 0.004))], 0.010)))
    pieces.append(("add", block((0.020, 0.008, 0.026), 0.006, (0.010, sole.top(0.01) + 0.086, 0.0), R_KNOB)))
    p = Paint("#6E7A3A")

    def webbing(u, t, c):
        stitch = (np.abs((t * 34) % 1.0 - 0.5) < 0.30) * (smooth01(0.035, 0.020, np.abs(u - 0.06)) + smooth01(0.035, 0.020, np.abs(u - 0.44)))
        return mix(c, "#FFB03A", stitch)

    p.fill(R_TOP, "#E4C48A", footbed("#E4C48A", "#C49C5E", "#A67E44", dots("#CFAA70", 24, 13, 0.10)))
    p.fill(R_BOTTOM, "#3A3226", tread("#5C5040", "lug", 9, worn="#4A4032"))
    p.fill(R_SIDE, "#4A4032", layers((0.34, 0.50, "#FF8A2A"), (0.50, 0.80, "#5C5040"), (0.80, 1.01, "#E4C48A")))
    p.fill(R_STRAP, "#6E7A3A", cord_paint("#4A5424", "#A0AE5E", webbing))
    p.fill(R_KNOB, "#FF8A2A", lit("#C85E12", "#FFC27A"))
    return pieces, p


def loafers():
    """"The school shoe, buckle and all." A fat black Mary Jane: a round toe, a strap, a gold buckle, a heel block."""
    sole = Sole(thick=0.034, round_=0.012, heel=0.080, ball=0.100, spring=0.018, wedge=0.016, power=0.66)
    cx = -0.070
    pieces = [("add", dome(sole, -HALF + 0.002, HALF - 0.002, 0.074, R_UPPER, slope=0.10, seg=24, rings=14)),
              ("cut", ball((0.100, 0.062, 0.056), (cx, sole.top(cx) + 0.074, 0.0), R_LINING, seg=18, rings=10)),
              ("add", sole.piece()),
              ("add", band(sole, -0.020, 0.019, 0.082, thick=0.010, inset=0.006)),
              ("add", block((0.019, 0.017, 0.008), 0.006, (-0.020, sole.top(-0.02) + 0.050, sole.width(-0.02) - 0.004), R_KNOB, turn=(-28, 0, 0)))]
    p = Paint("#1E1E26")
    shine = lambda u, t, c: mix(c, "#FFFFFF", blob(u, t, 0.08, 0.24, 0.05, 0.045, 0.6) * 0.6)
    p.fill(R_UPPER, "#22222C", lit("#0E0E14", "#5A5A70", 0.0, lambda u, t, c: mix(shine(u, t, c), "#F2EFE6", (np.abs((u * 90) % 1.0 - 0.5) < 0.28) * smooth01(0.014, 0.007, np.abs(t - 0.455)) * 0.9)))
    p.fill(R_LINING, "#C89A6A", lit("#9A6E42", "#E8C49A", 0.5))
    p.fill(R_TOP, "#C89A6A", footbed("#C89A6A", "#A87A4A"))
    p.fill(R_BOTTOM, "#3A2A1E", tread("#5A4230", "herring", 10, worn="#4A3626"))
    p.fill(R_SIDE, "#2A2018", layers((0.55, 0.70, "#F2EFE6"), (0.70, 1.01, "#22222C")))
    p.fill(R_STRAP, "#22222C", cord_paint("#0E0E14", "#5A5A70"))
    p.fill(R_KNOB, "#FFD23A", lit("#D89A1A", "#FFF0A0", 0.0))
    return pieces, p


BUILD = {"tsinelas": tsinelas, "crocs": crocs, "pantulog": pantulog, "sike": sike, "spartan": spartan, "alpombra": alpombra,
         "pambahay": slide, "heels": heels, "sandals": sandals, "loafers": loafers}


def build(name, out):
    pieces, paint = BUILD[name]()
    parts = out / "parts"
    parts.mkdir(parents=True, exist_ok=True)
    model = Model(name)
    model.folder = parts
    model.texture = name + "-atlas.png"
    for k, (op, (mesh, uv)) in enumerate(pieces):
        model.add("%02d_%s" % (k, op), [(mesh, uv)])
    paint.save(parts / model.texture)
    model.write()
    pos = np.concatenate([m[0] for op, (m, _) in pieces if op == "add"])
    print("%s: %.3f long, %.3f wide, %.3f tall" % (name, float(np.ptp(pos[:, 0])), float(np.ptp(pos[:, 2])), float(pos[:, 1].max())))


def sheet(out, tag):
    """The comparison sheet: per shoe a top isometric view and a bottom one from the other end, and the shoe it replaces."""
    import json
    from PIL import Image, ImageDraw
    shots = ROOT / "Logs/slippers/shots"
    stats = json.loads((out / "stats.json").read_text()) if (out / "stats.json").exists() else {}
    names = [n for n in BUILD if (shots / (n + "_top.png")).exists()]
    cw, ch, cols = 400, 300, 2
    cell_w, cell_h = cw * 2 + 210, ch + 34
    rows = (len(names) + cols - 1) // cols
    img = Image.new("RGB", (cell_w * cols, cell_h * rows), (24, 26, 34))
    d = ImageDraw.Draw(img)
    for k, n in enumerate(names):
        x, y = (k % cols) * cell_w, (k // cols) * cell_h
        s = stats.get(n, {})
        d.text((x + 8, y + 10), "%s    new: %s triangles, %s KB    old: %s triangles, %s KB" % (
            n.upper(), s.get("triangles", "?"), s.get("kb", "?"), s.get("old_triangles", "?"), s.get("old_kb", "?")), fill=(255, 255, 255))
        img.paste(Image.open(shots / (n + "_top.png")).convert("RGB").resize((cw, ch)), (x, y + 34))
        img.paste(Image.open(shots / (n + "_bottom.png")).convert("RGB").resize((cw, ch)), (x + cw, y + 34))
        old = shots / (n + "_old.png")
        if old.exists():
            img.paste(Image.open(old).convert("RGB").resize((200, 150)), (x + cw * 2 + 5, y + 34 + 75))
            d.text((x + cw * 2 + 10, y + 34 + 56), "the one it replaces", fill=(190, 196, 210))
    path = ROOT / "Logs/slippers" / ("slippers_%s.png" % tag)
    img.save(path)
    print("wrote", path)


if __name__ == "__main__":
    out = Path(arg("out", str(ROOT / "Logs/slippers/build")))
    words = [a for a in sys.argv[1:] if not a.startswith("--")]
    if words and words[0] == "sheet":
        sheet(out, words[1] if len(words) > 1 else "v1")
    else:
        for i in words or list(BUILD):
            build(i, out)
