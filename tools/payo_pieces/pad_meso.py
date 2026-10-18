"""PAYO's pads remodelled as carved temple stone (owner, 2026-10-18: "speedpad jump pad and geyser need a rework. you can
take the ones we have from arena and just remodel them to be more meso instead of futuristic").

  py -I tools/payo_pieces/pad_meso.py [pad_geyser pad_speed pickup_plinth]   parts + atlas + height + normal
  blender -b --python tools/payo_pieces/stone_weld.py -- <names> --out=<ABS Logs/payo/pieces/pads/build>
  py -I tools/payo_pieces/calm_glb.py <build/n.glb> <build/n-atlas.png> <pads/n.glb> <build/n_normal.png>
  (all three: bash tools/payo_pieces/pad_run.sh build; the review: bash tools/payo_pieces/pad_run.sh review)

Writes ONLY under Logs/payo/pieces/pads/. The game's files are never touched here.

THE CONTRACT (measured on the Payo glbs in Art/Payo/Models, kept exactly):
  pad_geyser     2.4 m across (radius 1.2), y 0 to 0.17, origin on the floor at its centre. `PayoGeyser` draws its
                 water at BowlRadius 0.62 m, PoolDepth -0.02 m: the basin's inner wall is at 0.62 m, its floor 6 mm up.
  pad_speed      1.5 x 3.0 m, y 0 to 0.05, travel +z, origin on the floor at its centre (ArenaSpeedPad HalfSize 0.75, 1.5).
  pickup_plinth  0.92 m across (radius 0.46), y 0 to 0.40, origin on the floor at its centre (the orb is the game's).

THE METHOD: the stone family's kit (stone_kit.Piece, its CALM height map baked to a normal map, the CALM palette),
rounded parts (lathes, rounded boxes), ONE painted canvas per prop. Every part takes its UVs from the same canvas:
  * a turned piece is painted as an UNFOLDED top view: a point's distance from the canvas centre is its distance along
    the profile from the axis (over the rim, down the risers), its angle its angle round the axis. Treads, risers and
    the basin wall each get their own ring of the painting at true scale across, so a band on a 4 cm riser is 4 cm.
  * a flat piece (the speed pad) is painted from straight above; parts lying on it take their UVs from where they lie.
Mesoamerican-flavoured: stepped rims, a sun of rays round the spring, carved serpent heads spouting into it, frets,
ancestor faces, feathered-serpent scales; inlaid turquoise and ochre as on the court. No glow, no chrome.
"""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
PADS = ROOT / "Logs/payo/pieces/pads"
BUILD = PADS / "build"
if "--calm" not in sys.argv:
    sys.argv.append("--calm")
sys.argv += ["--out", str(BUILD)]
sys.path.insert(0, str(HERE))
import stone_kit as K                                   # noqa: E402
import stone_build as S                                 # noqa: E402
import hand_companion_kit as hk                         # noqa: E402

# ------------------------------------------------------------------ the palette (CALM: the court's and the stage's)
STONE = K.calm_tone((150, 148, 144))                    # the stage's soft warm grey
STONE_D = K.shade(STONE, 0.80)
PALE = K.PALE
CREAM = (236, 224, 198)
TURQ = (70, 196, 182)                                   # the court's inlay, a touch softened
TURQ_D = (44, 130, 122)
OCHRE = K._soft((222, 170, 76))
TERRA = K._soft((198, 112, 82))
CORAL = K.CORAL
DRED = (150, 66, 56)
WATER_D = (40, 104, 108)
WATER_L = (92, 170, 168)


# ------------------------------------------------------------------ a canvas: colour and height as numpy
class Canvas:
    def __init__(self, w, h, base):
        self.w, self.h = w, h
        self.rgb = np.ones((h, w, 3), np.float32) * np.array(base, np.float32)
        self.H = np.zeros((h, w), np.float32)           # metres

    def put(self, mask, colour, alpha=1.0):
        m = (mask * alpha)[..., None]
        c = np.array(colour, np.float32)
        self.rgb = self.rgb * (1 - m) + (c if c.ndim == 1 else c) * m

    def shade(self, f):
        self.rgb *= f[..., None] if np.ndim(f) == 2 else f

    def lift(self, mask, metres, blur=1.2):
        self.H += gblur(mask, blur) * metres

    def to(self, img):
        img.paste(Image.fromarray(np.clip(self.rgb, 0, 255).astype(np.uint8)))
        Hm = K.HEIGHT.get(id(img))
        if Hm is not None:
            Hm.paste(Image.fromarray(np.clip(128 + self.H / K.HU, 0, 255).astype(np.uint8)))
        return img


def gblur(a, sigma):
    import calm_glb
    return calm_glb.gblur(np.asarray(a, np.float32), sigma)


def drawn(w, h, fn, blur=0.8):
    """A mask drawn with PIL on an L image, softened a little: a float array 0..1."""
    m = Image.new("L", (w, h), 0)
    fn(ImageDraw.Draw(m))
    if blur:
        m = m.filter(ImageFilter.GaussianBlur(blur))
    return np.asarray(m, np.float32) / 255.0


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


# ------------------------------------------------------------------ the unfolded lathe
class Turned:
    """A lathe and its unfolded painting. profile: (radius, y) from the outer FOOT up and over, ending at the axis on
    top (or in the basin). s: each profile point's distance along the profile from the axis end."""

    def __init__(self, profile, ppm, margin=10):
        prof = list(profile)
        if prof[0][0] > 1e-6:
            prof.insert(0, (0.0, prof[0][1]))           # the foot's closing disc (hk.lathe does the same)
            self.closed_foot = True
        if prof[-1][0] > 1e-6:
            prof.append((0.0, prof[-1][1]))
        self.prof = prof
        seglen = [math.hypot(b[0] - a[0], b[1] - a[1]) for a, b in zip(prof, prof[1:])]
        s = [0.0]
        for L in reversed(seglen):
            s.append(s[-1] + L)
        s = list(reversed(s))                           # s[i]: along the profile from point i to the axis end
        s[0] = s[1]                                     # the foot disc is never seen: it takes its rim's ring
        self.s = s
        self.smax = s[1]
        self.ppm = ppm
        self.margin = margin
        self.size = int(2 * self.smax * ppm + 2 * margin)

    def s_at(self, i):
        return self.s[i]

    def s_of_r(self, r, i0, i1):
        """On a flat ring of the profile between points i0 and i1: the s of radius r."""
        (r0, _), (r1, _) = self.prof[i0], self.prof[i1]
        t = (r - r0) / (r1 - r0)
        return self.s[i0] + (self.s[i1] - self.s[i0]) * t

    def polar(self):
        n = self.size
        c = n / 2.0
        y, x = np.mgrid[0:n, 0:n].astype(np.float32)
        dx, dy = (x + 0.5 - c) / self.ppm, (y + 0.5 - c) / self.ppm
        return np.hypot(dx, dy), np.mod(np.arctan2(dy, dx), 2 * math.pi)

    def mesh(self, P, rect, seg=48):
        m = hk.lathe(self.prof[1:-1] if False else self.prof, seg=seg)
        pos = m[0]
        uv = np.zeros((len(pos), 2), np.float32)
        c = self.size / 2.0
        for r in range(len(self.prof)):
            for k in range(seg + 1):
                a = 2 * math.pi * (k % seg) / seg
                i = r * (seg + 1) + k
                rr = self.s[r] * self.ppm
                uv[i] = ((rect[0] + c + rr * math.cos(a)) / P.S, (rect[1] + c + rr * math.sin(a)) / P.S)
        return m, uv

    def uv_point(self, P, rect, x, z, i0, i1):
        """UV for a point lying on the flat ring i0..i1 of the profile (heads, studs on a tread)."""
        r = math.hypot(x, z)
        a = math.atan2(z, x)
        s = self.s_of_r(r, i0, i1)
        c = self.size / 2.0
        return ((rect[0] + c + s * self.ppm * math.cos(a)) / P.S, (rect[1] + c + s * self.ppm * math.sin(a)) / P.S)


def ring(Sg, s0, s1, soft=0.6, ppm=1.0):
    """A ring of the painting between s0 and s1 (metres), edges softened by `soft` pixels."""
    e = soft / ppm
    return smooth(s0 - e, s0 + e, Sg) * (1 - smooth(s1 - e, s1 + e, Sg))


def sector_tiles(Sg, Ag, s0, s1, n, tile, phase=0.0, width=1.0, outward=True):
    """Lays the float tile (h, w) round the ring s0..s1, n times, each copy across `width` of its 1/n of the turn.
    The tile's top row is at s0 (the inner edge) unless outward is False."""
    th, tw = tile.shape
    step = 2 * math.pi / n
    a = np.mod(Ag - phase + step * width / 2, step)
    u = a / (step * width)
    v = (Sg - s0) / (s1 - s0)
    if not outward:
        v = 1 - v
    inside = (u >= 0) & (u < 1) & (v >= 0) & (v < 1)
    ui = np.clip((u * tw).astype(np.int32), 0, tw - 1)
    vi = np.clip((v * th).astype(np.int32), 0, th - 1)
    out = np.where(inside, tile[vi, ui], 0.0)
    return out.astype(np.float32)


def motif_tile(kind, w=160, h=160, scale=0.42, blur=1.0, thick=0.13):
    def fn(d):
        if kind == "serpent":
            serpent_motif(d, w / 2, h / 2, h * scale)
        elif kind == "feather":
            feather_motif(d, w, h)
        elif kind == "stepfret":
            stepfret_motif(d, w, h, thick)
        else:
            S.calm_motif(d, kind, w / 2, h / 2, h * scale)
    return drawn(w, h, fn, blur)


def serpent_motif(d, cx, cy, s):
    """A coiled serpent's head seen from the side, bold and few-parted: a jaw, an eye, a plume."""
    d.ellipse([cx - 0.7 * s, cy - 0.45 * s, cx + 0.5 * s, cy + 0.45 * s], fill=255)
    d.polygon([(cx + 0.3 * s, cy - 0.3 * s), (cx + 0.95 * s, cy - 0.05 * s), (cx + 0.3 * s, cy + 0.1 * s)], fill=255)
    d.ellipse([cx - 0.2 * s, cy - 0.28 * s, cx + 0.08 * s, cy], fill=0)
    for k in range(3):
        a = math.radians(200 + k * 28)
        d.line([(cx - 0.5 * s, cy - 0.2 * s), (cx - 0.5 * s + 0.75 * s * math.cos(a), cy - 0.2 * s + 0.75 * s * math.sin(a))], fill=255, width=max(3, int(0.16 * s)))


def feather_motif(d, w, h):
    """Feathered-serpent scales: overlapping round-ended feathers pointing down the tile (the flow direction)."""
    for row in range(3):
        for col in range(2):
            cx = w * (0.25 + 0.5 * col + (0.25 if row % 2 else 0))
            cy = h * (0.12 + row * 0.33)
            d.polygon([(cx - w * 0.2, cy), (cx + w * 0.2, cy), (cx + w * 0.12, cy + h * 0.24), (cx, cy + h * 0.32), (cx - w * 0.12, cy + h * 0.24)], fill=255)


def stepfret_motif(d, w, h, thick=0.13):
    """The step fret (xicalcoliuhqui): a stepped rise and a hooked spiral, one unit to the tile."""
    t = max(3, int(h * thick))
    pts = [(0, h * 0.85), (w * 0.2, h * 0.85), (w * 0.2, h * 0.62), (w * 0.4, h * 0.62), (w * 0.4, h * 0.38), (w * 0.6, h * 0.38),
           (w * 0.6, h * 0.15), (w * 0.88, h * 0.15), (w * 0.88, h * 0.62), (w * 0.72, h * 0.62), (w * 0.72, h * 0.38)]
    d.line(pts, fill=255, width=t, joint="curve")


# ------------------------------------------------------------------ the geyser bowl
# Profile from the outer foot, up two steps, over the rim, down the basin wall to the floor, to the axis.
GEYSER = [
    (1.20, 0.000), (1.20, 0.030), (1.188, 0.044),               # 0-2   the foot step's riser, rounded over
    (1.06, 0.046), (1.046, 0.056),                               # 3-4   its tread: the sun's rays
    (1.036, 0.098), (1.018, 0.114), (0.99, 0.120),               # 5-7   the second riser (terracotta band), rounded
    (0.70, 0.120), (0.672, 0.112), (0.640, 0.090),               # 8-10  the rim top (glyph ring), the inner lip
    (0.620, 0.060), (0.620, 0.018), (0.600, 0.006),              # 11-13 the basin wall, wet, down to the floor
    (0.30, 0.006), (0.0, 0.006),                                 # 14-15 the floor under the water
]
RIM_TOP = (8, 9)                                                 # indices in Turned.prof of GEYSER 7, 8 (a foot point is inserted)
HEADS = 4
HEAD_R = 0.835


def geyser(P):
    T = Turned(GEYSER, ppm=330)
    rect = P.alloc(T.size, T.size)
    c = T.size / 2.0
    s = T.s
    # (prof index i -> GEYSER index i - 1)

    def paint(img, info):
        cv = Canvas(T.size, T.size, STONE)
        Sg, Ag = T.polar()
        ppm = T.ppm
        sp = lambda i: s[i + 1]                         # noqa: E731  s of GEYSER point i
        # ---- the floor under the water: a deep spring, lighter toward its wall; a sun carved on the bottom
        floor = 1 - smooth(sp(13) - 0.004, sp(13) + 0.004, Sg)
        t = np.clip(Sg / sp(13), 0, 1)
        deep = np.array(WATER_D, np.float32) * (1 - t[..., None] * 0.6) + np.array(WATER_L, np.float32) * (t[..., None] * 0.6)
        cv.rgb = cv.rgb * (1 - floor[..., None]) + deep * floor[..., None]
        sun = drawn(T.size, T.size, lambda d: S.calm_motif(d, "sun", c, c, 0.40 * ppm), blur=2.0)
        cv.put(sun * floor, K.mix3(WATER_D, TURQ, 0.35), 0.8)
        cv.lift(sun * floor, -0.006, 2.0)
        # ---- the basin wall: dark wet stone, a turquoise lip where the water stands
        wall = ring(Sg, sp(13), sp(10), 0.8, ppm)
        cv.put(wall, K.mix3(STONE_D, WATER_D, 0.35))
        cv.put(ring(Sg, sp(12) - 0.012, sp(12) + 0.008, 0.6, ppm), TURQ_D, 0.9)
        # ---- the rim top: pale carved stone, a turquoise inlay at its inner edge, eight glyph cartouches
        rim = ring(Sg, sp(10), sp(7), 0.6, ppm)
        cv.put(rim, K.mix3(STONE, PALE, 0.45))
        inlay = ring(Sg, sp(9) + 0.004, sp(9) + 0.030, 0.8, ppm)
        cv.put(inlay, TURQ)
        cv.lift(inlay, -0.004)
        outer_line = ring(Sg, sp(7) - 0.03, sp(7) - 0.012, 0.8, ppm)
        cv.put(outer_line, OCHRE)
        cv.lift(outer_line, -0.003)
        # cartouches between the heads: a coral field, a raised pale frame, a bold deep-red glyph
        a_mid, a_out = sp(9) + 0.045, sp(7) - 0.045
        kinds = ["sun", "stepfret", "ancestor", "stepfret"]
        RC = 0.845                                       # the rim's middle radius: angular widths from true metres
        wd = lambda m: m / RC / (2 * math.pi)            # noqa: E731
        for j in range(2 * HEADS):
            k = j // 2
            phase = (k + 0.5) * 2 * math.pi / HEADS + (-1 if j % 2 == 0 else 1) * 0.19
            frame = sector_tiles(Sg, Ag, a_mid, a_out, 1, np.ones((8, 8), np.float32), phase=phase, width=wd(0.26))
            frame = gblur(frame, 1.2)
            field = sector_tiles(Sg, Ag, a_mid + 0.018, a_out - 0.018, 1, np.ones((8, 8), np.float32), phase=phase, width=wd(0.22))
            field = gblur(field, 1.0)
            cv.put(frame, K.mix3(PALE, CREAM, 0.5))
            cv.lift(frame, 0.008, 1.5)
            cv.put(field, CORAL if j % 2 == 0 else OCHRE)
            g = sector_tiles(Sg, Ag, a_mid + 0.026, a_out - 0.026, 1, motif_tile(kinds[j % 4]), phase=phase, width=wd(0.16), outward=False)
            cv.put(g * field, DRED)
            cv.lift(g * field, 0.006, 1.2)
        # ---- the second riser: one terracotta band with a step fret, the band you see from the side
        riser2 = ring(Sg, sp(7), sp(4), 0.6, ppm)
        cv.put(riser2, TERRA)
        fret = sector_tiles(Sg, Ag, sp(6) + 0.004, sp(4) - 0.008, 22, motif_tile("stepfret", 120, 60, thick=0.22), outward=True)
        cv.put(fret * riser2, CREAM, 0.85)
        cv.lift(fret * riser2, 0.004, 1.0)
        # ---- the foot tread: the sun's rays, sixteen ochre wedges radiating from the spring, turquoise between
        tread = ring(Sg, sp(4), sp(2), 0.6, ppm)
        cv.put(tread, K.mix3(STONE, PALE, 0.25))
        n = 16
        a = np.mod(Ag + math.pi / n, 2 * math.pi / n) - math.pi / n          # -half..half of one ray's slot
        u = (Sg - sp(4)) / (sp(2) - sp(4))                                    # 0 at the riser, 1 at the edge
        half = (math.pi / n) * (0.30 + 0.45 * np.clip(u, 0, 1))
        ray = np.clip((half - np.abs(a)) * Sg * ppm / 1.2, 0, 1) * tread
        cv.put(ray, OCHRE)
        cv.lift(ray, 0.004, 1.0)
        dots = np.clip((0.022 - np.hypot(np.abs(a) * Sg - (math.pi / n) * Sg, Sg - (sp(4) + sp(2)) / 2)) * ppm / 1.5, 0, 1) * tread
        cv.put(dots, TURQ)
        # ---- the foot riser: grey stone, a darker foot
        foot = ring(Sg, sp(2), sp(0) + 0.01, 0.6, ppm)
        cv.put(foot, STONE)
        cv.put(foot * smooth(sp(1), sp(0), Sg), STONE_D, 0.5)
        # ---- the soft light: lighter toward the rim, a little cooler and darker at the foot (CALM rule 1)
        cv.shade(1.0 + 0.05 * np.cos(np.clip((Sg - sp(8)) / 0.6, -1, 1) * math.pi / 2) - 0.04 * smooth(sp(4), sp(0), Sg))
        # the carved heads lie on the rim top: paint them where they lie (their UVs come from here)
        for k in range(HEADS):
            ang = k * 2 * math.pi / HEADS
            head_paint(cv, T, Sg, Ag, ang)
        cv.to(img)
        return img

    P.paint(rect, paint, {"ppm": T.ppm, "w": T.size, "h": T.size})
    m, uv = T.mesh(P, rect, seg=48)
    P.add(m, uv)
    # four serpent heads lying on the rim top, snouts to the spring: rounded, low (the pad's top stays at 0.17)
    for k in range(HEADS):
        ang = k * 2 * math.pi / HEADS
        head = hk.rounded((0.10, 0.045, 0.14), 0.04, seg=14, rings=8)
        p = head[0].copy()
        # local +z is the snout: narrow it and bring it down toward the spring; the brow stands at the back
        k = np.clip((p[:, 2] + 0.02) / 0.16, 0, 1)
        p[:, 0] *= 1.0 - 0.32 * k
        p[:, 1] = np.where(p[:, 1] > 0, p[:, 1] * (1.0 - 0.45 * k), p[:, 1])
        hm = (p, head[1], head[2])
        yaw = 90 - math.degrees(ang) + 180                # snout (+z) toward the axis
        at = (HEAD_R * math.cos(ang), 0.125, HEAD_R * math.sin(ang))
        mm = hk.moved(hm, at, (0, yaw, 0))
        uv = np.array([T.uv_point(P, rect, q[0], q[2], *RIM_TOP) for q in mm[0]], np.float32)
        P.add(mm, uv, cut="join")
    P.meta.update({"size": "2.4 m across, 0.17 m high; origin on the floor at its centre; basin wall at 0.62 m, floor 6 mm up",
                   "note": "the water, jet and steam are PayoGeyser's"})


def head_paint(cv, T, Sg, Ag, ang):
    """A serpent head seen from above, painted at its place on the rim: stone with an ochre feathered brow, turquoise
    eyes with deep pupils, a red open mouth at the snout (toward the spring)."""
    ppm = T.ppm
    i0, i1 = RIM_TOP
    # local frame: along = toward the axis (snout), across = round the ring
    s_c = T.s_of_r(HEAD_R, i0, i1)
    da = np.angle(np.exp(1j * (Ag - ang)))
    across = da * HEAD_R                                  # metres round the ring (true scale on the rim)
    # on the rim top s grows toward the axis? s is distance to the axis end along the profile: toward the axis s falls
    along = (s_c - Sg)                                    # + toward the spring
    body = np.clip((1.0 - np.hypot(across / 0.115, (along - 0.0) / 0.16)) * 6, 0, 1)
    cv.put(body, K.mix3(STONE, PALE, 0.35))
    brow = np.clip((1.0 - np.hypot(across / 0.11, (along + 0.07) / 0.07)) * 5, 0, 1)
    cv.put(brow, OCHRE)
    cv.lift(brow, 0.006, 1.5)
    for side in (-1, 1):
        eye = np.clip((0.03 - np.hypot(across - side * 0.05, along - 0.0)) * ppm / 1.5, 0, 1)
        cv.put(eye, TURQ)
        pupil = np.clip((0.013 - np.hypot(across - side * 0.05, along - 0.004)) * ppm / 1.2, 0, 1)
        cv.put(pupil, (34, 30, 32))
    mouth = np.clip((1.0 - np.hypot(across / 0.05, (along - 0.12) / 0.04)) * 5, 0, 1)
    cv.put(mouth, DRED)


# ------------------------------------------------------------------ the speed pad
def speed(P):
    """v2 (coordinator, 2026-10-18): three BOLD FILLED chevrons in cream stone, a thick turquoise band along each one's leading edge,
    raised to the border's height; a plain deep-red channel and a plain raised border; an ochre band where the run
    starts (-z) and a turquoise band where it sends you (+z). Nothing small or repeated."""
    hx, hz = 0.75, 1.5
    ppm = 300
    mg = 10
    W, D = int(2 * hx * ppm) + 2 * mg, int(2 * hz * ppm) + 2 * mg
    rect = P.alloc(W, D)

    def px(x, z):                                        # geometry (x, z) -> canvas pixel; +z (the travel) is DOWN
        return mg + (x + hx) * ppm, mg + (z + hz) * ppm

    def uv(x, z):
        a, b = px(x, z)
        return ((rect[0] + a) / P.S, (rect[1] + b) / P.S)

    FR = 0.15                                            # the raised border's width
    TIPS = (-0.62, 0.11, 0.84)                           # each chevron's tip z: they point +z
    CW, CL, CT = 0.52, 0.30, 0.30                        # half span across, how far the arms sweep back, thickness along z

    def arm_quads(tz):
        """A chevron as two convex quads (x, z), right arm then left: tip edge at x 0, outer end at |x| CW."""
        r = [(0.0, tz), (CW, tz - CL), (CW, tz - CL - CT), (0.0, tz - CT)]
        return [r, [(-x, z) for (x, z) in r]]

    def paint(img, info):
        cv = Canvas(W, D, STONE)
        X, Z = np.meshgrid((np.arange(W) + 0.5 - mg) / ppm - hx, (np.arange(D) + 0.5 - mg) / ppm - hz)
        ax, az = np.abs(X), np.abs(Z)
        inner_s = gblur(((ax < hx - FR) & (az < hz - FR)).astype(np.float32), 1.5)
        # the channel: one deep red, a soft gradient toward terracotta along the travel
        t = np.clip((Z + hz) / (2 * hz), 0, 1)
        chan = np.array(DRED, np.float32) * (1 - t[..., None] * 0.35) + np.array(TERRA, np.float32) * (t[..., None] * 0.35)
        cv.rgb = cv.rgb * (1 - inner_s[..., None]) + chan * inner_s[..., None]
        cv.lift(inner_s, -0.004, 2.0)
        # the border: plain pale stone
        cv.put(1 - inner_s, K.mix3(STONE, PALE, 0.35))
        # the start (-z) ochre band, the far (+z) turquoise band
        far = ((Z > hz - FR + 0.03) & (Z < hz - 0.03) & (ax < hx - FR)).astype(np.float32)
        cv.put(gblur(far, 1.2), TURQ)
        near = ((Z < -hz + FR - 0.03) & (Z > -hz + 0.03) & (ax < hx - FR)).astype(np.float32)
        cv.put(gblur(near, 1.2), OCHRE)
        # the chevrons: filled cream stone, a soft rounded edge in the height, a thick turquoise line down each arm
        for tz in TIPS:
            m = drawn(W, D, lambda d, tz=tz: [d.polygon([px(x, z) for (x, z) in q], fill=255) for q in arm_quads(tz)], blur=0.8)
            cv.put(m, CREAM)
            cv.lift(gblur(m, 4.0), 0.008, 0.0)
            e = tz - 0.02 - 0.045                         # a thick turquoise band just inside the LEADING edge
            line = drawn(W, D, lambda d, e=e: d.line([px(-CW + 0.04, e - CL * (CW - 0.04) / CW), px(0, e), px(CW - 0.04, e - CL * (CW - 0.04) / CW)],
                                                     fill=255, width=int(0.075 * ppm), joint="curve"), blur=1.0)
            line = line * m
            cv.put(line, TURQ)
            cv.lift(line, -0.003, 1.0)
        cv.shade(1.02 - 0.04 * np.clip((Z + hz) / (2 * hz), 0, 1))
        cv.to(img)
        return img

    P.paint(rect, paint, {"ppm": ppm, "w": W, "h": D})

    def put(mesh, cut="join"):
        P.add(mesh, np.array([uv(q[0], q[2]) for q in mesh[0]], np.float32), cut=cut)

    # the slab, rounded at its edges, its top at 0.034 (the channel floor)
    put(hk.rounded((hx, 0.017, hz), 0.012, at=(0, 0.017, 0), seg=16, rings=6), cut=False)
    # the border: four rounded bars up to 0.05
    put(hk.rounded((FR / 2, 0.011, hz), 0.010, at=(hx - FR / 2, 0.039, 0), seg=12, rings=6))
    put(hk.rounded((FR / 2, 0.011, hz), 0.010, at=(-hx + FR / 2, 0.039, 0), seg=12, rings=6))
    put(hk.rounded((hx - FR + 0.01, 0.011, FR / 2), 0.010, at=(0, 0.039, hz - FR / 2), seg=12, rings=6))
    put(hk.rounded((hx - FR + 0.01, 0.011, FR / 2), 0.010, at=(0, 0.039, -hz + FR / 2), seg=12, rings=6))
    # the chevrons: each arm a closed prism from 0.028 (in the slab) to 0.05; the edge is rounded in the normal map
    for tz in TIPS:
        r = arm_quads(tz)[0]                             # one closed solid: tip, right end, its foot, notch, left foot, left end
        put(prism([r[0], r[1], r[2], r[3], (-CW, tz - CL - CT), (-CW, tz - CL)], 0.028, 0.050))
    P.meta.update({"size": "1.5 x 3.0 m, 5 cm high; origin on the floor at its centre; travel is +z (the chevrons point that way)"})


def prism(outline, y0, y1):
    """A closed flat-sided prism over an outline (x, z) that is star-shaped from its FIRST point (the top and bottom
    are fanned from it), each face its own corners (sharp edges)."""
    q = list(outline)
    area = sum(q[i][0] * q[(i + 1) % len(q)][1] - q[i][1] * q[(i + 1) % len(q)][0] for i in range(len(q)))
    if area > 0:
        q = [q[0]] + q[1:][::-1]                         # one winding throughout, still fanned from the first point
    pos, nrm, tri = [], [], []

    def face(pts, n):
        b = len(pos)
        pos.extend(pts); nrm.extend([n] * len(pts))
        for i in range(1, len(pts) - 1):
            tri.append((b, b + i, b + i + 1))
    face([(x, y1, z) for (x, z) in q], (0, 1, 0))
    face([(x, y0, z) for (x, z) in reversed(q)], (0, -1, 0))
    for i in range(len(q)):
        (ax_, az_), (bx_, bz_) = q[i], q[(i + 1) % len(q)]
        n = -np.array([bz_ - az_, 0, -(bx_ - ax_)], np.float32)     # outward for this winding
        n = tuple(n / max(1e-9, np.linalg.norm(n)))
        face([(ax_, y1, az_), (ax_, y0, az_), (bx_, y0, bz_), (bx_, y1, bz_)], n)
    return np.array(pos, np.float32), np.array(nrm, np.float32), np.array(tri, np.uint32)

# ------------------------------------------------------------------ the pickup plinth
# A small stepped altar, talud and tablero: a foot step, a sloped skirt, a panelled drum with four carved ancestor
# faces, a projecting cap, a turquoise sun inlaid in its top (the orb floats over it).
PLINTH = [
    (0.46, 0.000), (0.46, 0.040), (0.448, 0.054),                 # 0-2   the foot step
    (0.415, 0.058),                                               # 3     its tread
    (0.405, 0.070), (0.365, 0.165),                               # 4-5   the talud (sloped skirt)
    (0.385, 0.172), (0.392, 0.184), (0.392, 0.292),               # 6-8   the tablero (the panelled drum)
    (0.382, 0.302), (0.352, 0.308), (0.352, 0.326),               # 9-11  the neck
    (0.430, 0.338), (0.440, 0.352), (0.440, 0.382),               # 12-14 the cap's edge
    (0.425, 0.398), (0.400, 0.400), (0.0, 0.400),                 # 15-17 the top
]


def plinth(P):
    T = Turned(PLINTH, ppm=420)
    rect = P.alloc(T.size, T.size)
    c = T.size / 2.0
    s = T.s

    def paint(img, info):
        cv = Canvas(T.size, T.size, STONE)
        Sg, Ag = T.polar()
        ppm = T.ppm
        sp = lambda i: s[i + 1]                         # noqa: E731
        # the top: pale stone, an inlaid turquoise sun under the orb
        top = 1 - smooth(sp(15) - 0.004, sp(15) + 0.004, Sg)
        cv.put(top, K.mix3(STONE, PALE, 0.45))
        sun = drawn(T.size, T.size, lambda d: S.calm_motif(d, "sun", c, c, 0.33 * ppm), blur=1.6)
        cv.put(sun, TURQ)
        cv.lift(sun, -0.004, 1.5)
        disc = np.clip((0.12 - Sg) * ppm / 2.0, 0, 1)
        cv.put(disc, OCHRE)
        # the cap's edge: an ochre band
        cap = ring(Sg, sp(15), sp(11), 0.6, ppm)
        cv.put(cap, OCHRE)
        cv.put(ring(Sg, sp(13) - 0.006, sp(13) + 0.006, 0.6, ppm), K.shade(OCHRE, 0.85))
        # the neck: shadowed stone
        cv.put(ring(Sg, sp(11), sp(8), 0.6, ppm), STONE_D)
        # the tablero: four cream panels with deep-red ancestor faces on coral, turquoise studs between
        tab = ring(Sg, sp(8), sp(6), 0.6, ppm)
        cv.put(tab, K.mix3(STONE, PALE, 0.3))
        for k in range(4):
            phase = k * math.pi / 2 + math.pi / 4
            frame = gblur(sector_tiles(Sg, Ag, sp(8) + 0.006, sp(6) - 0.006, 1, np.ones((4, 4), np.float32), phase=phase, width=0.24 / (2 * math.pi * 0.392)), 1.2)
            field = gblur(sector_tiles(Sg, Ag, sp(8) + 0.018, sp(6) - 0.018, 1, np.ones((4, 4), np.float32), phase=phase, width=0.21 / (2 * math.pi * 0.392)), 1.0)
            cv.put(frame, CREAM)
            cv.lift(frame, 0.006, 1.5)
            cv.put(field, CORAL)
            face = sector_tiles(Sg, Ag, sp(8) + 0.022, sp(6) - 0.022, 1, motif_tile("ancestor", 120, 140, 0.46), phase=phase, width=0.085 / (2 * math.pi * 0.392))
            cv.put(face * field, DRED)
            cv.lift(face * field, 0.005, 1.0)
            stud_a = np.angle(np.exp(1j * (Ag - k * math.pi / 2)))
            stud = np.clip((0.022 - np.hypot(stud_a * 0.392, Sg - (sp(8) + sp(6)) / 2)) * ppm / 1.5, 0, 1)
            cv.put(stud, TURQ)
            cv.lift(stud, 0.004, 1.0)
        # the talud: grey stone sloping out, a step fret in terracotta
        tal = ring(Sg, sp(6), sp(3), 0.6, ppm)
        cv.put(tal, STONE)
        fret = sector_tiles(Sg, Ag, sp(5) + 0.008, sp(4) - 0.008, 12, motif_tile("stepfret", 120, 60, thick=0.22))
        cv.put(fret * tal, TERRA)
        cv.lift(fret * tal, 0.003, 1.0)
        # the foot step: darker stone
        foot = ring(Sg, sp(3), sp(0) + 0.01, 0.6, ppm)
        cv.put(foot, K.shade(STONE, 0.92))
        cv.shade(1.03 - 0.07 * smooth(sp(15), sp(0), Sg))
        cv.to(img)
        return img

    P.paint(rect, paint, {"ppm": T.ppm, "w": T.size, "h": T.size})
    m, uv = T.mesh(P, rect, seg=40)
    P.add(m, uv)
    P.meta.update({"size": "0.92 m across, 0.40 m high; origin on the floor at its centre"})


# ------------------------------------------------------------------ main
BUILDERS = {"pad_geyser": (geyser, 1024), "pad_speed": (speed, 1024), "pickup_plinth": (plinth, 1024)}

if __name__ == "__main__":
    names = [a for a in sys.argv[1:] if a in BUILDERS] or list(BUILDERS)
    BUILD.mkdir(parents=True, exist_ok=True)
    for n in names:
        fn, size = BUILDERS[n]
        P = K.Piece(n, size=size, ppm=200, seed=7)
        fn(P)
        P.write(BUILD)
        print("built", n, "parts", len(P.parts))
