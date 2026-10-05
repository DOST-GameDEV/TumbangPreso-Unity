"""Paint the arena's HOLO kit textures: the giant hologram ads and the slipper balloon.

  py -3 tools/author_arena_textures_holo.py [--sheet N]

Writes Assets/TumbangPreso/Art/Arena/Textures/arena_holo_<surface>.png (and _emit.png) and a sheet
in Logs/arena/holo/. The models are built by tools/author_arena_holo.py, which imports the constants
at the top of this file (the atlas boxes, the ad list, the balloon's UV islands and its outline) and
nothing else, so everything above `_lazy()` stays free of PIL and scipy.

The owner, 2026-10-05, having played the map: "we're missing some gigantic holograms throughout the
map, i was thinking of like the screenshots attached could be used like ads (should include pc
express there)", and "i want a massive slipper thing, idk if i want it as a hologram or something
like a balloon, like the balloon cow in overwatch".

  ads      1024 x 2048, RGBA. TWO STRIPS of eight advertisements, one above the next, for the ad
           columns. v wraps, so Unity scrolls a strip by moving v; u 0..0.5 is strip A, 0.5..1 is B.
           Each ad stands clear of its cell's sides (AD_GUTTER), so the strips do not bleed
           into each other in the far mips.
           An ad is light on nothing: a faint wash, a hand-drawn frame, lettering and one emblem.
  fx       2048 x 2048, RGBA. The free-standing holograms' atlas (FX): PC EXPRESS, the TUMP stamp,
           the globe's ring of text, five LINE-ART FIGURES of the game's world (a jeepney, a
           carabao, a rooster, the can being knocked over, sampaguita), and the small parts every
           hologram is built from (a line, a dotted sheet, a scan band, a beam, a glow, a cube face).
  metal    512, tiling. The emitters' and buoys' painted steel.
  led      256 x 256, eight flat rows (LED): UVs pick a row. Emitter rings, thrusters, ropes.
  balloon  2048 x 2048. The slipper balloon, painted by island (ISLAND): its front with the face, its
           back with the game's stamp, the rim band with its stitching, and three rows of tube: the
           strap, the limbs, the scarf.

THE BRANDS. PC EXPRESS is a real sponsor already in this game and the owner asked for it by name: it
is the project's own artwork (Assets/TumbangPreso/Art/models/textures/pc_express_horizontal_rgb.png),
resized and nothing else: not redrawn, not recoloured, not cropped, and it is only ever shown on a
ONE-SIDED sheet so it is never seen mirrored. TUMP is the game's logo and the owner's one-colour
stamp. EVERY OTHER NAME IS INVENTED, hand-named the way the city kit names its nine signs
(KANTO_DESIGN_GUIDE 11.1): five of them ARE the city kit's (Sinag, Bahaghari, Kape ni Ka Inggo,
Halo-Halo Holo, Dyip-Lipad, Pansitan sa Ulap), so the columns advertise the same city the towers
carry, and six are new. No other real brand.

ROLE HUES: holograms are cyan, magenta, white and gold; nothing large is near offence orange #f87020
or defence blue #0080e8 (a logo's own colours excepted). The balloon wears the game's own slipper's
colours (BAL_HEX): the logo's yellow sole and maroon line. The logo's strap is orange, which is the
offence colour, so the balloon's strap is that maroon lifted to a red.
"""
import math
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = Path(os.environ.get("ARENA_TEX_OUT", ROOT / "Assets" / "TumbangPreso" / "Art" / "Arena" / "Textures"))
LOGS = ROOT / "Logs" / "arena" / "holo"
TUMP_LOGO = ROOT / "Assets" / "TumbangPreso" / "Art" / "ui" / "brand" / "tump_logo.png"
TUMP_STAMP = ROOT / "ArtSource" / "arena" / "brand" / "tump_stamp_owner.png"
PCX = ROOT / "Assets" / "TumbangPreso" / "Art" / "models" / "textures" / "pc_express_horizontal_rgb.png"
PCX_ASPECT = 14107.0 / 3729.0

# ------------------------------------------------------------------ the ad strips
ADS_W, ADS_H = 1024, 2048
AD_PX = (512, 256)                         # one advertisement
ADS_PER_STRIP = 8
AD_ASPECT = 2.0
# THE GUTTER. The two strips share one image, side by side, and a column is 400 to 700 m from the
# stage: Unity draws it from the third or fourth mip, where one texel is 8 or 16 of these pixels and
# a strip's edge takes in its neighbour (and, the image wrapping in u, strip B's far edge takes in
# strip A's near one). So an ad is laid into its 512 x 256 cell at 480 x 240 (the same shape: 2 : 1),
# 16 px clear of each side and 8 px of the top and the bottom. With the ad's own clear margin that
# is 23 px of nothing between a strip's edge and its first line: clean to the fourth mip. v needs no
# gutter: above and below an ad is the next ad of its own strip, and the image wraps in v on purpose
# (ArenaHoloMotion scrolls it; ArenaArtPlacer imports it Repeat).
AD_GUTTER = (16, 8)
# (key, ground, ink, second ink, lines). The order is top to bottom.
STRIPS = (
    ("pcx", "sinag", "tsinelas", "kape", "liga", "pisonet", "halo", "lata"),
    ("bahaghari", "pcx", "lugaw", "dyip", "tump", "turon", "taxi", "pansitan"),
)

# ------------------------------------------------------------------ the hologram atlas (image pixels, top-left origin)
FX_ATLAS = 2048
FX = {
    "pcx":        (8, 8, 2040, 545),        # 2032 x 537: the PC Express artwork at its own 3.783 : 1
    "ring":       (0, 560, 2048, 688),      # the globe's ring of text: wraps in u
    "tump":       (8, 704, 648, 1344),
    "jeepney":    (664, 704, 1624, 1184),
    "can":        (1640, 704, 2040, 1184),
    "carabao":    (664, 1200, 1384, 1720),
    "rooster":    (1400, 1200, 1848, 1840),
    "sampaguita": (8, 1360, 648, 2000),
    "dots":       (664, 1736, 920, 1992),   # a dotted sheet: the layer behind a hologram
    "scan":       (936, 1736, 1384, 1800),  # a scan band: hoops
    "line":       (936, 1816, 1384, 1848),  # a line of light: wireframes, brackets
    "beam":       (936, 1864, 1128, 2040),  # a soft column of light: across u, fading out at both ends of v
    "glow":       (1144, 1864, 1320, 2040),
    "cube":       (1864, 1200, 2040, 1376),  # one face of a floating cube
    "fan":        (1864, 1392, 2040, 1840),  # an emitter's fan of light: narrow at the bottom
    "slice":      (1400, 1856, 1848, 2040),  # a faint striped fill: the volume inside a wireframe
}

LED = ("cyan", "magenta", "gold", "white", "thrust", "rope", "red", "dark")
LED_HEX = {"cyan": "62dcf2", "magenta": "e45cb4", "gold": "f2c060", "white": "e4ecff", "thrust": "8ff0ff",
           "rope": "c8c2b0", "red": "e8483a", "dark": "10142a"}
METAL_TILE_M = 16.0

# ------------------------------------------------------------------ the balloon
# The slipper stands on its heel: local x across, y from heel to toe, z out of the footbed.
# The balloon is a CHARACTER whose body is the slipper's sole: fat, short and round (design A of the
# three in Logs/arena/holo/balloon_options_v5.png). Local x across, y from heel to toe, z out of the front.
BALLOON = dict(length=60.0, width=36.0, thick=22.0, waist=0.90)
SEAMS = ((0.60, 0.02), (0.21, 0.0))        # the two seams across the sole: (t at the middle, how far it bows); the model pinches along them
# UV islands, (u0, v0, u1, v1), v up.
ISLAND = {
    "foot":  (0.010, 0.010, 0.430, 0.990),   # the front, seen square on: u across, v heel to toe
    "tread": (0.445, 0.010, 0.765, 0.755),   # the back, the same way, mirrored
    "rim":   (0.780, 0.000, 0.990, 1.000),   # the band round the edge: v runs once round, u across the band
    "strap": (0.445, 0.905, 0.765, 0.990),   # three rows of tube: u along it, v round it
    "limb":  (0.445, 0.840, 0.765, 0.895),
    "scarf": (0.445, 0.775, 0.765, 0.830),
}
# Where things are on the balloon: t along the sole, x in widths. The face is small, low and wide apart.
BAL = dict(post=0.86, anchor=0.60, anchor_x=0.80, scarf_t=0.21, eye_t=0.405, eye_x=0.215, eye_r=0.066, line=0.021,
           mouth_t=0.335, mouth_r=0.050, blush_x=0.335, blush_t=0.350)
# The game's own slipper colours (Art/ui/brand/tsinelas_hit.png, avatars/avatar_tsinelas.png): the
# logo's yellow sole d8c808 and maroon line 980818, the avatar strap's amber f8b828.
# The balloon's faces: arena_holo_balloon.png is the first, arena_holo_balloon_<mood>.png the others
# (each with its _emit). Runtime/Map/ArenaBalloon.cs holds the same four, in this order.
BALLOON_FACES = ("happy", "worried", "ouch", "dizzy")
BAL_HEX = dict(yellow="dccb0c", yellow_hi="ece04c", yellow_lo="c2b006", yellow_back="cdbb08", yellow_line="a08c04",
               maroon="980818", red="b8242e", amber="f8b828", cream="f6ecc8", stitch="fff4b4", blush="f08a80", gloss="fffbd8")
# The hologram slipper's strap (tools/author_arena_holo.py, `slipper_hologram`).
FACE = dict(post_v=0.79, anchor_v=0.50)


def half_width(t, waist=0.80):
    """The slipper's half width (in widths) at t, 0 at the heel's end, 1 at the toe's: a round heel,
    a waist (`waist` of the ball's width), a broad ball, a round toe."""
    t = min(max(t, 0.0), 1.0)
    ends = max(0.0, 1.0 - abs(2.0 * t - 1.0) ** 2.7) ** 0.47
    s = min(max((t - 0.30) / 0.42, 0.0), 1.0)
    broad = waist + (1.0 - waist) * (s * s * (3 - 2 * s))
    return 0.5 * ends * broad


def lean(t):
    """The centre line drifts toward the big toe (in widths)."""
    s = min(max((t - 0.55) / 0.45, 0.0), 1.0)
    return 0.035 * s * s * (3 - 2 * s)


def outline(n=56, waist=0.80):
    """The footbed's outline, counter-clockwise seen from the footbed's side, in (widths, lengths):
    x in about -0.5..0.5, y in 0..1. n is even; the two ends are single points."""
    half = n // 2
    pts = []
    for k in range(half + 1):                                     # up the +x side, heel to toe
        t = (1 - math.cos(math.pi * k / half)) / 2
        pts.append((lean(t) + half_width(t, waist), t))
    for k in range(1, half):                                      # back down the -x side
        t = (1 + math.cos(math.pi * k / half)) / 2
        pts.append((lean(t) - half_width(t, waist), t))
    return pts


def atlas_uv(box, size=FX_ATLAS):
    """(u0, v0, u1, v1) for an image-pixel box, v up, pulled in half a texel."""
    x0, y0, x1, y1 = box
    return (x0 + 0.5) / size, 1.0 - (y1 - 0.5) / size, (x1 - 0.5) / size, 1.0 - (y0 + 0.5) / size


def ad_uv(strip, slot=0):
    """Where strip 0 or 1 lies in u, and the v at the TOP of ad `slot` (v up)."""
    return strip * 0.5, strip * 0.5 + 0.5, 1.0 - slot / ADS_PER_STRIP


# ------------------------------------------------------------------ painting (lazy imports from here down)

def _lazy():
    global np, Image, ImageDraw, ImageFont, ndimage, T
    import numpy as np
    from PIL import Image, ImageDraw, ImageFont
    from scipy import ndimage
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import author_arena_textures_city as T                       # its painting helpers only: smooth, patch, coat, lettering, font
    T._lazy()
    Image.MAX_IMAGE_PIXELS = None


def hexcol(h):
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])


def save(name, rgb, alpha=None):
    OUT.mkdir(parents=True, exist_ok=True)
    a = (np.clip(rgb, 0, 1) * 255).astype(np.uint8)
    if alpha is not None:
        a = np.dstack([a, (np.clip(alpha, 0, 1) * 255).astype(np.uint8)])
    Image.fromarray(a).save(OUT / ("arena_holo_%s.png" % name))
    print("[arena-holo-tex]", name, a.shape)


def scanlines(h, w, period=6.0, duty=0.62, seed=1, wobble=0.0):
    yy = np.mgrid[0:h, 0:w][0].astype(float)
    if wobble:
        yy = yy + wobble * T.smooth(h, w, 60, seed)
    return np.clip((duty - np.abs((yy % period) / period - 0.5) * 2) * 3.0 + 0.5, 0, 1)


def dots(h, w, pitch=9.0, r=2.4):
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    row = np.floor(yy / pitch)
    cx = (xx + (row % 2) * pitch / 2) % pitch - pitch / 2
    cy = yy % pitch - pitch / 2
    return np.clip((r - np.hypot(cx, cy)) / 1.2 + 0.5, 0, 1)


# ---- a small vector pen: every figure is drawn by hand as splines, three times oversize, then
# brought down, so a line is clean and even and its joints are round.

def spline(pts, closed=False, per=10):
    """Catmull-Rom through pts."""
    p = [np.asarray(q, dtype=float) for q in pts]
    n = len(p)
    if n < 3:
        return p
    out = []
    last = n if closed else n - 1
    for i in range(last):
        p0 = p[(i - 1) % n] if (closed or i > 0) else p[0] * 2 - p[1]
        p1, p2 = p[i], p[(i + 1) % n]
        p3 = p[(i + 2) % n] if (closed or i + 2 < n) else p[-1] * 2 - p[-2]
        for k in range(per):
            t = k / per
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    if not closed:
        out.append(p[-1])
    return out


class Pen:
    """Draws in figure units (x right, y UP, the figure `w` units wide and `h` tall) onto a box of
    the atlas. `line` and `fill` are separate masks: a hologram is a bright line and a faint body."""
    K = 3

    def __init__(self, box, w, h, margin=0.06):
        self.pw, self.ph = box[2] - box[0], box[3] - box[1]
        s = min(self.pw * (1 - 2 * margin) / w, self.ph * (1 - 2 * margin) / h) * self.K
        self.s = s
        self.ox = (self.pw * self.K - w * s) / 2
        self.oy = (self.ph * self.K + h * s) / 2
        self.line = Image.new("L", (self.pw * self.K, self.ph * self.K), 0)
        self.body = Image.new("L", (self.pw * self.K, self.ph * self.K), 0)
        self.dl, self.db = ImageDraw.Draw(self.line), ImageDraw.Draw(self.body)
        self.base = 0.011 * max(w, h) * s                         # the pen's ordinary width, in oversize pixels

    def xy(self, p):
        return (self.ox + p[0] * self.s, self.oy - p[1] * self.s)

    def stroke(self, pts, width=1.0, closed=False, smooth=True, fill=False, per=10):
        q = spline(pts, closed, per) if smooth else [np.asarray(p, dtype=float) for p in pts]
        px = [self.xy(p) for p in q]
        if fill:
            self.db.polygon(px, fill=255)
        wd = max(1, int(round(self.base * width)))
        self.dl.line(px + ([px[0]] if closed else []), fill=255, width=wd, joint="curve")
        r = wd / 2
        for x, y in (px if not closed else px[:1]):
            self.dl.ellipse((x - r, y - r, x + r, y + r), fill=255)
        return q

    def poly(self, pts, width=1.0, fill=False):
        return self.stroke(pts, width, closed=True, smooth=False, fill=fill)

    def circle(self, c, r, width=1.0, fill=False, n=40):
        return self.stroke([(c[0] + r * math.cos(math.tau * i / n), c[1] + r * math.sin(math.tau * i / n)) for i in range(n)],
                           width, closed=True, smooth=False, fill=fill)

    def ellipse(self, c, rx, ry, turn=0.0, width=1.0, fill=False, n=36):
        ct, st = math.cos(turn), math.sin(turn)
        pts = []
        for i in range(n):
            x, y = rx * math.cos(math.tau * i / n), ry * math.sin(math.tau * i / n)
            pts.append((c[0] + x * ct - y * st, c[1] + x * st + y * ct))
        return self.stroke(pts, width, closed=True, smooth=False, fill=fill)

    def dot(self, c, r):
        x, y = self.xy(c)
        rr = r * self.s
        self.dl.ellipse((x - rr, y - rr, x + rr, y + rr), fill=255)

    def masks(self):
        size = (self.pw, self.ph)
        line = np.asarray(self.line.resize(size, Image.LANCZOS), dtype=float) / 255
        body = np.asarray(self.body.resize(size, Image.LANCZOS), dtype=float) / 255
        return np.clip(line, 0, 1), np.clip(body, 0, 1)


def hologram(line, body, colour, second=None, second_mask=None, fill="scan", seed=0):
    """A line drawing as light: the line whole, a soft glow round it, and the body filled with a
    faint scan or dot pattern, so the figure has a volume and not only an edge."""
    h, w = line.shape
    pattern = scanlines(h, w, 7.0, 0.5, seed) if fill == "scan" else dots(h, w, 8.0, 2.2)
    glow = ndimage.gaussian_filter(line, 5.0) * 0.55 + ndimage.gaussian_filter(line, 14.0) * 0.35
    inside = body * (0.10 + 0.20 * pattern)
    alpha = np.clip(line * 0.96 + glow * (1 - line) * 0.5 + inside * (1 - line), 0, 1)
    c = hexcol(colour)
    rgb = np.ones((h, w, 3)) * c
    if second is not None and second_mask is not None:
        rgb = rgb * (1 - second_mask[..., None]) + hexcol(second) * second_mask[..., None]
    rgb = rgb * (0.82 + 0.18 * line[..., None]) + line[..., None] * 0.12     # the line's core runs a little toward white
    return np.clip(rgb, 0, 1), alpha


# ------------------------------------------------------------------ the five figures

def fig_jeepney(box):
    """DYIP: a jeepney in side view, nose left: the long cab, the open side windows, the rack and
    its bundles, the horses on the bonnet, the route board. It flies: no road, two hover skids."""
    p = Pen(box, 2.0, 1.0)
    body = [(0.10, 0.30), (0.09, 0.44), (0.13, 0.52), (0.44, 0.565), (0.56, 0.80), (1.86, 0.80), (1.90, 0.76),
            (1.90, 0.30), (1.66, 0.30), (1.62, 0.39), (1.50, 0.44), (1.38, 0.39), (1.34, 0.30), (0.62, 0.30),
            (0.58, 0.39), (0.46, 0.44), (0.34, 0.39), (0.30, 0.30)]
    p.poly(body, 1.25, fill=True)
    for cx in (0.46, 1.50):                                       # the wheels, tucked up: a ring, a hub, a hover glow under each
        p.circle((cx, 0.27), 0.105, 1.1)
        p.circle((cx, 0.27), 0.04, 0.8)
        for k in (-1, 0, 1):
            p.stroke([(cx + k * 0.07 - 0.02, 0.12 - abs(k) * 0.01), (cx + k * 0.07 + 0.02, 0.12 - abs(k) * 0.01)], 0.8, smooth=False)
    p.poly([(0.52, 0.565), (0.60, 0.74), (0.72, 0.74), (0.72, 0.565)], 0.8)          # the driver's window
    for k in range(5):                                            # the long open side: five bays between posts
        x0 = 0.80 + k * 0.205
        p.poly([(x0, 0.56), (x0, 0.74), (x0 + 0.165, 0.74), (x0 + 0.165, 0.56)], 0.8)
    p.stroke([(0.76, 0.50), (1.84, 0.50)], 0.8, smooth=False)                       # the rail under the windows
    p.stroke([(0.74, 0.40), (0.90, 0.44), (1.06, 0.38), (1.22, 0.44), (1.30, 0.40)], 0.8)   # the painted swash on the flank
    p.stroke([(0.60, 0.86), (1.84, 0.86)], 0.9, smooth=False)                       # the roof rack and its posts
    for x in (0.64, 0.94, 1.24, 1.54, 1.80):
        p.stroke([(x, 0.80), (x, 0.86)], 0.8, smooth=False)
    p.stroke([(1.02, 0.86), (1.04, 0.95), (1.20, 0.97), (1.30, 0.93), (1.32, 0.86)], 0.8)   # two bundles tied on the rack
    p.stroke([(1.40, 0.86), (1.42, 0.93), (1.60, 0.93), (1.62, 0.86)], 0.8, smooth=False)
    p.poly([(0.60, 0.88), (0.60, 0.97), (0.90, 0.97), (0.90, 0.88)], 0.8)            # the route board over the cab
    p.stroke([(0.65, 0.925), (0.85, 0.925)], 0.7, smooth=False)
    p.stroke([(0.20, 0.535), (0.19, 0.62), (0.23, 0.66), (0.26, 0.61), (0.25, 0.545)], 0.8)   # a horse on the bonnet: neck and head
    p.stroke([(0.23, 0.66), (0.27, 0.69)], 0.7, smooth=False)
    p.stroke([(0.34, 0.55), (0.30, 0.74)], 0.7, smooth=False)                       # two aerials raked back
    p.stroke([(0.40, 0.56), (0.37, 0.70)], 0.7, smooth=False)
    p.circle((0.135, 0.455), 0.03, 0.8)                                            # the headlamp, the grille's bars, the bumper
    for k in range(3):
        p.stroke([(0.105, 0.33 + k * 0.03), (0.17, 0.335 + k * 0.03)], 0.6, smooth=False)
    p.stroke([(0.06, 0.29), (0.26, 0.29)], 1.2, smooth=False)
    p.stroke([(1.90, 0.42), (1.96, 0.42), (1.96, 0.30), (1.90, 0.30)], 0.9, smooth=False)   # the step at the back
    for k in range(3):                                            # speed lines off the tail
        p.stroke([(1.97, 0.72 - k * 0.09), (1.995, 0.72 - k * 0.09)], 0.7, smooth=False)
    return p.masks()


def fig_carabao(box):
    """KALABAW: a carabao in side view, head left and low, the two great horns swept back, an egret
    standing on its back."""
    p = Pen(box, 1.40, 1.0)
    body = [(0.115, 0.43), (0.10, 0.50), (0.16, 0.60), (0.27, 0.665), (0.36, 0.69), (0.46, 0.745), (0.60, 0.775),
            (0.80, 0.755), (1.00, 0.76), (1.16, 0.73), (1.265, 0.64), (1.285, 0.50), (1.265, 0.36),
            (1.255, 0.20), (1.265, 0.075), (1.15, 0.075), (1.15, 0.20), (1.12, 0.335),
            (1.00, 0.305), (0.84, 0.29), (0.70, 0.31),
            (0.685, 0.20), (0.695, 0.075), (0.58, 0.075), (0.575, 0.20), (0.54, 0.34),
            (0.47, 0.40), (0.40, 0.42), (0.33, 0.395), (0.25, 0.365), (0.17, 0.37)]
    p.stroke(body, 1.25, closed=True, fill=True, per=8)
    p.stroke([(0.985, 0.30), (0.975, 0.20), (0.985, 0.075), (1.085, 0.075), (1.075, 0.20), (1.10, 0.33)], 1.0, per=6)   # the far hind leg
    p.stroke([(0.455, 0.395), (0.445, 0.20), (0.455, 0.075), (0.545, 0.075), (0.54, 0.20), (0.545, 0.33)], 1.0, per=6)   # the far fore leg
    # The horns: two crescents from the poll, swept back and up, the near one drawn whole.
    p.stroke([(0.33, 0.685), (0.37, 0.80), (0.47, 0.895), (0.62, 0.925), (0.50, 0.855), (0.435, 0.775), (0.405, 0.705)], 1.15, fill=True, per=8)
    p.stroke([(0.285, 0.675), (0.265, 0.78), (0.31, 0.875), (0.40, 0.93), (0.345, 0.855), (0.32, 0.775)], 0.95, per=8)
    p.stroke([(0.375, 0.655), (0.445, 0.635), (0.475, 0.59), (0.425, 0.595), (0.385, 0.625)], 0.85, closed=True)        # the ear
    p.dot((0.235, 0.565), 0.015)                                                    # the eye, the nostril, the mouth's line
    p.dot((0.125, 0.475), 0.010)
    p.stroke([(0.125, 0.41), (0.19, 0.415), (0.245, 0.44)], 0.7)
    p.stroke([(0.45, 0.70), (0.47, 0.58), (0.52, 0.46)], 0.75)                       # the shoulder
    p.stroke([(1.05, 0.72), (1.11, 0.60), (1.10, 0.45)], 0.75)                       # the haunch
    p.stroke([(1.27, 0.62), (1.325, 0.52), (1.335, 0.38), (1.32, 0.30)], 0.9)        # the tail and its tuft
    p.stroke([(1.30, 0.31), (1.32, 0.22), (1.345, 0.31)], 0.8, smooth=False)
    # The egret: a small S of a bird on the withers.
    p.stroke([(0.77, 0.765), (0.775, 0.83)], 0.6, smooth=False)
    p.stroke([(0.80, 0.762), (0.80, 0.83)], 0.6, smooth=False)
    p.stroke([(0.86, 0.845), (0.80, 0.83), (0.745, 0.85), (0.725, 0.90), (0.745, 0.945), (0.72, 0.965)], 0.8)
    p.stroke([(0.72, 0.965), (0.665, 0.955)], 0.7, smooth=False)
    p.stroke([(0.86, 0.845), (0.80, 0.875), (0.75, 0.87)], 0.7)
    for k in range(4):                                            # the ground: four short dashes
        p.stroke([(0.36 + k * 0.27, 0.035), (0.50 + k * 0.27, 0.035)], 0.7, smooth=False)
    return p.masks()


def fig_rooster(box):
    """TANDANG: a rooster standing, head left and high, chest out, the tail a fan of five sickles."""
    p = Pen(box, 0.74, 1.0)
    body = [(0.215, 0.865), (0.165, 0.825), (0.150, 0.74), (0.135, 0.62), (0.150, 0.50), (0.215, 0.405), (0.32, 0.37),
            (0.43, 0.395), (0.50, 0.46), (0.505, 0.545), (0.44, 0.60), (0.345, 0.635), (0.29, 0.70), (0.275, 0.79), (0.265, 0.85)]
    p.stroke(body, 1.25, closed=True, fill=True, per=8)
    p.stroke([(0.165, 0.83), (0.075, 0.805), (0.165, 0.785)], 1.0, smooth=False, fill=True)          # the beak
    p.stroke([(0.175, 0.875), (0.165, 0.935), (0.20, 0.905), (0.215, 0.965), (0.245, 0.915), (0.275, 0.955), (0.275, 0.865)], 1.0, fill=True, per=5)   # the comb
    p.stroke([(0.165, 0.775), (0.145, 0.715), (0.175, 0.68), (0.20, 0.725), (0.19, 0.775)], 0.9, fill=True)   # the wattle
    p.dot((0.215, 0.835), 0.012)
    for k, (tip, mid) in enumerate((((0.585, 0.975), (0.455, 0.84)), ((0.685, 0.90), (0.565, 0.80)), ((0.725, 0.77), (0.63, 0.715)),
                                    ((0.715, 0.62), (0.635, 0.625)), ((0.665, 0.48), (0.60, 0.535)))):   # the tail's sickles
        root = (0.455 + k * 0.006, 0.575 - k * 0.022)
        p.stroke([root, mid, tip], 1.0)
        p.stroke([tip, (mid[0] + 0.045, mid[1] - 0.045), (root[0] + 0.03, root[1] - 0.03)], 0.8)
    p.stroke([(0.235, 0.585), (0.31, 0.60), (0.395, 0.565), (0.455, 0.50)], 0.9)                       # the wing and three flight feathers
    for k in range(3):
        p.stroke([(0.27 + k * 0.055, 0.585 - k * 0.012), (0.305 + k * 0.055, 0.50 - k * 0.012)], 0.7, smooth=False)
    p.stroke([(0.215, 0.405), (0.26, 0.47), (0.33, 0.49), (0.395, 0.46), (0.43, 0.395)], 0.8)           # the belly's fluff
    for k in range(4):                                            # hackle feathers down the neck
        p.stroke([(0.185 + k * 0.022, 0.72 - k * 0.035), (0.20 + k * 0.03, 0.655 - k * 0.035)], 0.7, smooth=False)
    for x in (0.285, 0.385):                                      # the legs (short and thick: a fighting cock, not a wader), a spur, three toes
        p.stroke([(x - 0.012, 0.385), (x - 0.02, 0.30), (x - 0.008, 0.215)], 1.1)
        p.stroke([(x + 0.022, 0.385), (x + 0.012, 0.30), (x + 0.018, 0.215)], 1.1)
        p.stroke([(x + 0.015, 0.275), (x + 0.06, 0.29)], 0.8, smooth=False)
        for dx in (-0.085, -0.02, 0.07):
            p.stroke([(x + 0.005, 0.215), (x + dx, 0.165)], 1.0, smooth=False)
    for k in range(3):
        p.stroke([(0.12 + k * 0.19, 0.125), (0.24 + k * 0.19, 0.125)], 0.7, smooth=False)
    return p.masks()


def fig_can(box):
    """ANG LATA: the game itself. The can struck and tipping, the slipper that struck it, the burst."""
    p = Pen(box, 0.84, 1.0)
    a = math.radians(-17.0)

    def R(x, y, c=(0.36, 0.40)):
        return (c[0] + x * math.cos(a) - y * math.sin(a), c[1] + x * math.sin(a) + y * math.cos(a))

    w, h, e = 0.17, 0.25, 0.045
    side = [R(-w, h)] + [R(-w, -h)] + [R(w * math.cos(t), -h - e * math.sin(t)) for t in (math.pi * (1 - k / 14) for k in range(15))] + [R(w, h)]
    top = [R(w * math.cos(t), h + e * math.sin(t)) for t in (math.tau * k / 30 for k in range(30))]
    p.stroke(side + top[16:30], 1.25, closed=True, smooth=False, fill=True)
    p.stroke(top, 1.1, closed=True, smooth=False, fill=True)
    p.stroke([R(w * 0.80 * math.cos(t), h + e * 0.72 * math.sin(t)) for t in (math.tau * k / 30 for k in range(30))], 0.7, closed=True, smooth=False)
    p.stroke([R(0.02, h + 0.004), R(0.085, h + 0.012)], 0.9, smooth=False)            # the pull tab
    for yy in (0.13, -0.13):                                      # the label's two bands and a star between them
        p.stroke([R(w * math.cos(t), yy - e * math.sin(t)) for t in (math.pi * (1 - k / 14) for k in range(15))], 0.8, smooth=False)
    star = []
    for k in range(10):
        r = 0.085 if k % 2 == 0 else 0.036
        t = math.pi / 2 + k * math.tau / 10
        star.append(R(r * math.cos(t), -0.01 + r * math.sin(t)))
    p.poly(star, 0.85)
    for k, (t, r0, r1) in enumerate(((2.25, 0.06, 0.15), (2.65, 0.05, 0.12), (1.85, 0.05, 0.13), (3.05, 0.06, 0.13))):   # the burst where it was struck
        c = R(w, 0.16)
        p.stroke([(c[0] + r0 * math.cos(t - 1.9), c[1] + r0 * math.sin(t - 1.9)), (c[0] + r1 * math.cos(t - 1.9), c[1] + r1 * math.sin(t - 1.9))], 0.9, smooth=False)
    # The slipper, flying in from the upper right: the sole, the strap's V, three speed lines.
    b = math.radians(28.0)

    def S(x, y, c=(0.665, 0.80)):
        return (c[0] + x * math.cos(b) - y * math.sin(b), c[1] + x * math.sin(b) + y * math.cos(b))

    sole = [S((lean(t) + sx * half_width(t)) * 0.17, (t - 0.5) * 0.30) for sx, ts in ((1, [k / 14 for k in range(15)]), (-1, [1 - k / 14 for k in range(1, 14)])) for t in ts]
    p.stroke(sole, 1.1, closed=True, smooth=False, fill=True)
    p.stroke([S(-0.062, -0.01), S(0.0, 0.085), S(0.068, -0.01)], 1.0)
    for k in range(3):
        o = (k - 1) * 0.045
        p.stroke([S(o, -0.19), S(o, -0.26 - (0.03 if k == 1 else 0.0))], 0.8, smooth=False)
    for k in range(3):
        p.stroke([(0.10 + k * 0.20, 0.04), (0.22 + k * 0.20, 0.04)], 0.7, smooth=False)
    p.stroke([(0.07, 0.62), (0.03, 0.70)], 0.8, smooth=False)                         # it tips: two motion ticks at its shoulder
    p.stroke([(0.11, 0.67), (0.08, 0.74)], 0.8, smooth=False)
    return p.masks()


def fig_sampaguita(box):
    """SAMPAGUITA: a sprig. Three open flowers of eight round petals, two buds, five leaves."""
    p = Pen(box, 1.0, 1.0)
    stem = p.stroke([(0.50, 0.04), (0.47, 0.22), (0.50, 0.40), (0.46, 0.56), (0.50, 0.70)], 1.0)
    p.stroke([(0.49, 0.33), (0.36, 0.42), (0.27, 0.52)], 0.9)
    p.stroke([(0.49, 0.42), (0.62, 0.50), (0.72, 0.60)], 0.9)

    def leaf(root, tip, bulge=0.085):
        r, t = np.asarray(root), np.asarray(tip)
        d = t - r
        n = np.array([-d[1], d[0]]) / (np.hypot(*d) + 1e-9)
        p.stroke([r, r + d * 0.45 + n * bulge, t, r + d * 0.45 - n * bulge], 1.0, closed=True, fill=True, per=8)
        p.stroke([r, r + d * 0.5 + n * 0.008, t], 0.6)

    leaf((0.47, 0.20), (0.22, 0.13))
    leaf((0.48, 0.24), (0.76, 0.19))
    leaf((0.48, 0.12), (0.69, 0.035), 0.06)
    leaf((0.40, 0.385), (0.14, 0.335), 0.07)
    leaf((0.60, 0.485), (0.865, 0.43), 0.07)

    def flower(c, r, turn=0.0):
        for k in range(8):                                        # eight round petals that touch and do not cross, each a little unlike the next
            t = turn + k * math.tau / 8
            rr = r * (0.94 + 0.10 * math.sin(k * 2.3))
            p.ellipse((c[0] + 0.62 * rr * math.cos(t), c[1] + 0.62 * rr * math.sin(t)), 0.38 * rr, 0.20 * rr, t, 0.9, fill=True, n=22)
        p.circle(c, r * 0.20, 0.8, n=20)
        p.dot(c, r * 0.07)

    flower((0.50, 0.775), 0.185, 0.2)
    flower((0.235, 0.60), 0.15, 0.5)
    flower((0.765, 0.685), 0.14, 0.0)
    for c, tip in (((0.335, 0.80), (0.30, 0.90)), ((0.665, 0.875), (0.70, 0.955))):   # two buds on their stalks
        r, t = np.asarray(c), np.asarray(tip)
        d = t - r
        n = np.array([-d[1], d[0]]) / np.hypot(*d)
        p.stroke([r, r + d * 0.5 + n * 0.035, t, r + d * 0.5 - n * 0.035], 0.9, closed=True, fill=True, per=8)
    p.stroke([(0.46, 0.60), (0.39, 0.70), (0.34, 0.80)], 0.8)
    p.stroke([(0.52, 0.72), (0.60, 0.80), (0.66, 0.875)], 0.8)
    return p.masks()


# ------------------------------------------------------------------ the atlas

def put(rgb, alpha, box, c, a):
    x0, y0, x1, y1 = box
    rgb[y0:y1, x0:x1] = c
    alpha[y0:y1, x0:x1] = a


def fit(img, w, h):
    """An RGBA image resized whole into w x h, keeping its shape, centred on clear."""
    s = min(w / img.size[0], h / img.size[1])
    sz = (max(1, int(round(img.size[0] * s))), max(1, int(round(img.size[1] * s))))
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    out.paste(img.resize(sz, Image.LANCZOS), ((w - sz[0]) // 2, (h - sz[1]) // 2))
    return np.asarray(out, dtype=float) / 255


def bleed(rgb, a):
    """Colour pushed out under the clear texels, so a mip or a filter never drags black in."""
    known = a > 0.02
    if not known.any():
        return rgb
    idx = ndimage.distance_transform_edt(~known, return_distances=False, return_indices=True)
    return rgb[idx[0], idx[1]]


def pcx_light(w, h):
    """THE PC EXPRESS ARTWORK, WHOLE AND UNCHANGED, as light. The file is resized and nothing else.
    A hologram is added to the sky, and added light cannot draw the mark's deep blue as a dark
    colour, so the sheet carries the artwork's own colours at full strength and its own alpha, and
    the kit shows it brighter than its neighbours rather than tinting or outlining it."""
    art = fit(Image.open(PCX).convert("RGBA"), w, h)
    return art[..., :3], art[..., 3]


def fx():
    n = FX_ATLAS
    rgb = np.zeros((n, n, 3))
    alpha = np.zeros((n, n))
    box = FX["pcx"]
    c, a = pcx_light(box[2] - box[0], box[3] - box[1])
    put(rgb, alpha, box, c, a * 0.96)

    # The globe's ring of text: gold capitals with a small slipper between the phrases. It wraps.
    x0, y0, x1, y1 = FX["ring"]
    w, h = x1 - x0, y1 - y0
    phrases = ["TUMBANG PRESO", "LIGA NG LANGIT", "FINALS NGAYONG GABI", "TUMP"]
    cells = [0.24, 0.24, 0.34, 0.18]
    m = np.zeros((h, w))
    x = 0.0
    for text, share in zip(phrases, cells):
        cw = int(w * share)
        cell = T.lettering(h, cw, [text], "impact", (cw * 0.10, h * 0.20, cw * 0.90, h * 0.80), 31 + len(text), wobble=0.5)
        m[:, int(x):int(x) + cw] = np.maximum(m[:, int(x):int(x) + cw], cell[:, :w - int(x)])
        yy, xx = np.mgrid[0:h, 0:w].astype(float)
        d = np.abs(xx - (x + 2.0)) / 9.0 + np.abs(yy - h / 2) / 18.0     # a diamond on each seam
        m = np.maximum(m, np.clip((1 - d) * 4, 0, 1))
        x += w * share
    rail = np.clip((3.0 - np.abs(np.mgrid[0:h, 0:w][0] - h * 0.07)) / 1.5, 0, 1) + np.clip((3.0 - np.abs(np.mgrid[0:h, 0:w][0] - h * 0.93)) / 1.5, 0, 1)
    wash = 0.10 + 0.08 * scanlines(h, w, 6.0, 0.5, 5)
    put(rgb, alpha, FX["ring"], hexcol("f2c060") * (1 - rail[..., None] * 0.0) , np.clip(m * 0.96 + rail * 0.8 + wash * (1 - m), 0, 1))

    # The TUMP stamp, the owner's one-colour drawing: its black line becomes the light.
    x0, y0, x1, y1 = FX["tump"]
    st = fit(Image.open(TUMP_STAMP).convert("RGBA").crop((30, 145, 800, 665)), x1 - x0, y1 - y0)
    luma = st[..., :3] @ np.array([0.3, 0.6, 0.1])
    line = st[..., 3] * np.clip((0.45 - luma) / 0.3, 0, 1)
    hatch = st[..., 3] * np.clip((0.97 - luma) / 0.2, 0, 1) * (1 - line)
    body = ndimage.binary_fill_holes(line > 0.4).astype(float)
    body = ndimage.gaussian_filter(body, 1.0)
    glow = ndimage.gaussian_filter(line, 5.0) * 0.5 + ndimage.gaussian_filter(line, 14.0) * 0.35
    a = np.clip(line * 0.96 + hatch * 0.45 + glow * (1 - line) * 0.5 + body * (0.10 + 0.16 * scanlines(y1 - y0, x1 - x0, 7.0, 0.5, 3)) * (1 - line), 0, 1)
    put(rgb, alpha, FX["tump"], hexcol("dff6ff") * (0.8 + 0.2 * line[..., None]) * (1 - hatch[..., None]) + hexcol("f2c060") * hatch[..., None], a)

    for name, fn, colour, fill in (("jeepney", fig_jeepney, "f2c060", "scan"), ("carabao", fig_carabao, "62dcf2", "dots"),
                                   ("rooster", fig_rooster, "f08ac0", "scan"), ("can", fig_can, "e4ecff", "dots"),
                                   ("sampaguita", fig_sampaguita, "c8ffe6", "scan")):
        line, body = fn(FX[name])
        c, a = hologram(line, body, colour, fill=fill, seed=len(name))
        put(rgb, alpha, FX[name], c, a)

    x0, y0, x1, y1 = FX["dots"]
    h, w = y1 - y0, x1 - x0
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    edge = np.minimum(np.minimum(xx, w - 1 - xx), np.minimum(yy, h - 1 - yy))
    fade = np.clip(edge / 26.0, 0, 1)
    put(rgb, alpha, FX["dots"], hexcol("62dcf2"), dots(h, w, 10.0, 2.3) * 0.28 * fade)

    x0, y0, x1, y1 = FX["scan"]
    h, w = y1 - y0, x1 - x0
    v = np.mgrid[0:h, 0:w][0] / (h - 1.0)
    band = np.clip(1 - np.abs(v - 0.5) * 2, 0, 1) ** 0.7 * (0.55 + 0.45 * scanlines(h, w, 5.0, 0.5, 2))
    put(rgb, alpha, FX["scan"], hexcol("62dcf2"), band * 0.7)

    x0, y0, x1, y1 = FX["line"]
    h, w = y1 - y0, x1 - x0
    v = np.abs(np.mgrid[0:h, 0:w][0] / (h - 1.0) - 0.5) * 2
    put(rgb, alpha, FX["line"], hexcol("bff2ff"), np.clip(1.25 - v * 1.6, 0, 1) ** 1.3 * 0.95)

    x0, y0, x1, y1 = FX["beam"]
    h, w = y1 - y0, x1 - x0
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    across = np.clip(1 - np.abs(xx / (w - 1.0) - 0.5) * 2, 0, 1) ** 1.6
    along = np.clip(np.minimum(yy, h - 1 - yy) / (h * 0.16), 0, 1)
    put(rgb, alpha, FX["beam"], hexcol("9fe6f4"), across * along * 0.34)

    x0, y0, x1, y1 = FX["glow"]
    h, w = y1 - y0, x1 - x0
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    d = np.hypot(xx - (w - 1) / 2, yy - (h - 1) / 2) / (w / 2)
    put(rgb, alpha, FX["glow"], hexcol("eaf6ff"), np.clip(1 - d, 0, 1) ** 1.8 * 0.9)

    x0, y0, x1, y1 = FX["cube"]
    h, w = y1 - y0, x1 - x0
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    edge = np.minimum(np.minimum(xx, w - 1 - xx), np.minimum(yy, h - 1 - yy))
    frame = np.clip((14.0 - edge) / 3.0, 0, 1) * np.clip(edge / 3.0, 0, 1)
    put(rgb, alpha, FX["cube"], hexcol("bff2ff"), np.clip(frame * 0.95 + 0.10 + 0.08 * scanlines(h, w, 8.0, 0.5, 9), 0, 1) * np.clip(edge / 3.0, 0, 1))

    x0, y0, x1, y1 = FX["fan"]
    h, w = y1 - y0, x1 - x0
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    up = 1 - yy / (h - 1.0)                                        # 0 at the emitter (the bottom), 1 at the top
    half = 0.10 + 0.40 * up
    across = np.clip(1 - np.abs(xx / (w - 1.0) - 0.5) / half, 0, 1) ** 1.3
    put(rgb, alpha, FX["fan"], hexcol("9fe6f4"), across * (1 - up) ** 0.8 * np.clip(up / 0.04, 0, 1) * 0.5 * (0.75 + 0.25 * scanlines(h, w, 9.0, 0.5, 4)))

    x0, y0, x1, y1 = FX["slice"]
    h, w = y1 - y0, x1 - x0
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    edge = np.minimum(np.minimum(xx, w - 1 - xx), np.minimum(yy, h - 1 - yy))
    put(rgb, alpha, FX["slice"], hexcol("62dcf2"), (0.07 + 0.13 * scanlines(h, w, 10.0, 0.4, 6)) * np.clip(edge / 6.0, 0, 1))

    rgb = bleed(rgb, alpha)
    save("fx", rgb, alpha)
    save("fx_emit", rgb * alpha[..., None])


# ------------------------------------------------------------------ the ads

def ad_ground(h, w, colour, seed):
    """An ad's sheet: a faint wash of its colour with scan lines, and a hand-drawn frame."""
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    xx2, yy2 = xx + 1.2 * T.smooth(h, w, 40, seed), yy + 1.2 * T.smooth(h, w, 40, seed + 1)
    d = np.minimum(np.minimum(xx2, w - 1 - xx2), np.minimum(yy2, h - 1 - yy2))
    frame = np.clip((d - 7.0) / 1.5 + 0.5, 0, 1) * np.clip((12.0 - d) / 1.5 + 0.5, 0, 1)
    wash = (0.10 + 0.07 * scanlines(h, w, 6.0, 0.5, seed)) * np.clip((d - 4.0) / 3.0, 0, 1)
    tick = frame * 0.0
    return np.ones((h, w, 3)) * hexcol(colour), wash, frame


def ad(key, seed):
    w, h = AD_PX
    def L(lines, font, box, s, **kw):
        kw.setdefault("wobble", 0.9)                              # a signwriter's hand, but small: these are read from 400 m
        return T.lettering(h, w, lines, font, (w * box[0], h * box[1], w * box[2], h * box[3]), seed + s, **kw)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    cyan, mag, gold, white, mint = "62dcf2", "f08ac0", "f2c060", "eef4ff", "86efc4"

    def sheet(ground, parts):
        rgb, wash, frame = ad_ground(h, w, ground, seed)
        a = np.maximum(wash, frame * 0.85)
        for mask, colour in parts:
            rgb = rgb * (1 - mask[..., None]) + hexcol(colour) * mask[..., None]
            a = np.maximum(a, mask * 0.96)
        return rgb, a

    if key == "pcx":                                              # the artwork alone on a clear sheet with a plain white frame
        rgb, wash, frame = ad_ground(h, w, white, seed)
        a = np.maximum(wash * 0.6, frame * 0.85)
        lw, lh = int(w * 0.90), int(w * 0.90 / PCX_ASPECT)
        c, la = pcx_light(lw, lh)
        ox, oy = (w - lw) // 2, (h - lh) // 2
        rgb[oy:oy + lh, ox:ox + lw] = rgb[oy:oy + lh, ox:ox + lw] * (1 - la[..., None]) + c * la[..., None]
        a[oy:oy + lh, ox:ox + lw] = np.maximum(a[oy:oy + lh, ox:ox + lw] * (1 - la), la * 0.97)
        return rgb, a
    if key in ("liga", "tump"):                                   # the game's own logo in its own colours
        logo = Image.open(TUMP_LOGO).convert("RGBA")
        lw = int(w * (0.46 if key == "liga" else 0.52))
        lh = int(lw * logo.size[1] / logo.size[0])
        lg = np.asarray(logo.resize((lw, lh), Image.LANCZOS), dtype=float) / 255
        ox, oy = (int(w * 0.04) if key == "liga" else (w - lw) // 2), (h - lh) // 2 - (0 if key == "liga" else 24)
        parts = [(L(["LIGA NG", "TUMBANG", "PRESO"], "impact", (0.54, 0.12, 0.95, 0.70), 2), white),
                 (L(["FINALS NGAYONG GABI"], "bahn", (0.54, 0.74, 0.95, 0.88), 3), cyan)] if key == "liga" else \
                [(L(["LARO TAYO  ·  TUMBANG PRESO"], "bahn", (0.12, 0.83, 0.88, 0.93), 4), gold)]
        rgb, a = sheet(cyan if key == "liga" else gold, parts)
        la = lg[..., 3]
        rgb[oy:oy + lh, ox:ox + lw] = rgb[oy:oy + lh, ox:ox + lw] * (1 - la[..., None]) + lg[..., :3] * la[..., None]
        a[oy:oy + lh, ox:ox + lw] = np.maximum(a[oy:oy + lh, ox:ox + lw], la * 0.95)
        return rgb, a
    if key == "sinag":                                            # the city kit's SINAG ENERHIYA: a rising sun
        cx, cy = w * 0.19, h * 0.66
        rr, aa = np.hypot(xx - cx, yy - cy), np.degrees(np.arctan2(cy - yy, xx - cx))
        sun = np.clip((h * 0.15 - rr) / 1.5 + 0.5, 0, 1) * (yy < cy)
        rays = ndimage.gaussian_filter(((np.abs(((aa + 15) % 30) - 15) < 6.5) * (aa > 2) * (aa < 178) * np.clip((rr - h * 0.20) / 2, 0, 1) * np.clip((h * 0.32 - rr) / 2, 0, 1)).astype(float), 0.8)
        return sheet(gold, [(np.maximum(sun, rays), gold), (L(["SINAG"], "impact", (0.36, 0.14, 0.94, 0.62), 1), white),
                            (L(["ENERHIYA  ·  ILAW NG LUNGSOD"], "bahn", (0.36, 0.68, 0.94, 0.84), 2), gold)])
    if key == "bahaghari":                                        # BAHAGHARI TELEKOM: four arcs
        cx, cy = w * 0.16, h * 0.76
        rr = np.hypot(xx - cx, yy - cy)
        parts = [(np.clip((5.0 - np.abs(rr - (h * 0.46 - k * 15.0))) / 1.5 + 0.5, 0, 1) * (yy < cy), c) for k, c in enumerate((mag, gold, mint, cyan))]
        return sheet(mag, parts + [(L(["BAHAGHARI"], "black", (0.33, 0.16, 0.95, 0.58), 1), white),
                                   (L(["TELEKOM  ·  SIGNAL HANGGANG ULAP"], "bahn", (0.44, 0.64, 0.95, 0.78), 2), "c9b8f0")])
    if key == "kape":                                             # KAPE NI KA INGGO: a cup and three curls of steam
        cup = T.box_mask(xx, yy, w * 0.18, h * 0.60, w * 0.09, h * 0.14, 12.0)
        cup = np.maximum(cup, np.clip((5.0 - np.abs(np.hypot(xx - w * 0.29, yy - h * 0.58) - h * 0.08)) / 1.5 + 0.5, 0, 1))
        steam = np.zeros((h, w))
        for k in (-1, 0, 1):
            sx = w * 0.18 + k * w * 0.045 + np.sin((yy - h * 0.1) / 9.0 + k) * 5.0
            steam = np.maximum(steam, np.clip((3.0 - np.abs(xx - sx)) / 1.5 + 0.5, 0, 1) * (yy > h * 0.14) * (yy < h * 0.40))
        return sheet(gold, [(cup, "f0dcb0"), (steam, "c9b8f0"), (L(["KAPE"], "impact", (0.38, 0.14, 0.94, 0.64), 1), gold),
                            (L(["ni Ka Inggo"], "print", (0.38, 0.64, 0.94, 0.88), 2, wobble=1.6), "f0dcb0")])
    if key == "halo":                                             # HALO-HALO HOLO: a tall glass in three layers
        half = w * 0.055 + (h * 0.78 - yy) * 0.11
        glass = np.clip((half - np.abs(xx - w * 0.15)) / 1.5 + 0.5, 0, 1) * (yy > h * 0.20) * (yy < h * 0.80)
        parts = [(glass * (yy >= h * lo) * (yy < h * hi), c) for lo, hi, c in ((0.20, 0.40, white), (0.40, 0.60, mag), (0.60, 0.80, "c9b8f0"))]
        return sheet(mint, parts + [(L(["HALO-HALO"], "impact", (0.30, 0.14, 0.95, 0.56), 1), mint), (L(["HOLO"], "impact", (0.30, 0.58, 0.62, 0.88), 2), mag),
                                    (L(["MALAMIG", "NA ILAW"], "bahn", (0.66, 0.60, 0.95, 0.86), 3), white)])
    if key == "dyip":                                             # DYIP-LIPAD TERMINAL: three chevrons
        parts = []
        for k in range(3):
            d = np.abs(yy - h * 0.36) * 0.7 + (xx - (w * 0.80 + k * w * 0.05))
            parts.append((np.clip((5.0 - np.abs(d)) / 1.5 + 0.5, 0, 1) * (np.abs(yy - h * 0.36) < h * 0.20), gold))
        return sheet(cyan, parts + [(L(["DYIP-LIPAD"], "impact", (0.06, 0.14, 0.76, 0.60), 1), white), (L(["TERMINAL"], "impact", (0.06, 0.62, 0.50, 0.88), 2), cyan),
                                    (L(["CUBAO  ·  QUIAPO", "BACLARAN"], "bahn", (0.54, 0.64, 0.94, 0.88), 3), cyan)])
    if key == "pansitan":                                         # PANSITAN SA ULAP: a bowl and chopsticks
        bowl = np.clip((w * 0.11 - np.hypot(xx - w * 0.16, (yy - h * 0.50) * 1.5)) / 1.5 + 0.5, 0, 1) * (yy > h * 0.50)
        sticks = np.zeros((h, w))
        for k in (-1, 1):
            sticks = np.maximum(sticks, np.clip((3.0 - np.abs((xx - w * 0.16) - k * 16 - (h * 0.50 - yy) * 0.5 * k)) / 1.5 + 0.5, 0, 1) * (yy < h * 0.50) * (yy > h * 0.22))
        return sheet(mag, [(bowl, white), (sticks, gold), (L(["PANSITAN"], "impact", (0.32, 0.14, 0.95, 0.62), 1), mag),
                           (L(["SA ULAP  ·  BUKAS HANGGANG UMAGA"], "bahn", (0.32, 0.68, 0.95, 0.84), 2), white)])
    if key == "tsinelas":                                         # TSINELAS REPUBLIK (new): a slipper, sole and strap
        pts = [((lean(t) + sx * half_width(t)) * 0.36 * h, (0.5 - t) * 0.74 * h) for sx, ts in ((1, [k / 20 for k in range(21)]), (-1, [1 - k / 20 for k in range(1, 20)])) for t in ts]
        im = Image.new("L", (w * 2, h * 2), 0)
        d = ImageDraw.Draw(im)
        ang = math.radians(-24)
        P2 = [((w * 0.16 + x * math.cos(ang) - y * math.sin(ang)) * 2, (h * 0.50 + x * math.sin(ang) + y * math.cos(ang)) * 2) for x, y in pts]
        d.line(P2 + [P2[0]], fill=255, width=9, joint="curve")
        strap = [(-0.13 * h, 0.02 * h), (0.0, -0.20 * h), (0.14 * h, 0.02 * h)]
        d.line([((w * 0.16 + x * math.cos(ang) - y * math.sin(ang)) * 2, (h * 0.50 + x * math.sin(ang) + y * math.cos(ang)) * 2) for x, y in strap], fill=255, width=11, joint="curve")
        slip = np.asarray(im.resize((w, h), Image.LANCZOS), dtype=float) / 255
        return sheet(gold, [(slip, gold), (L(["TSINELAS"], "impact", (0.32, 0.12, 0.95, 0.54), 1), white), (L(["REPUBLIK"], "black", (0.32, 0.56, 0.95, 0.80), 2), gold),
                            (L(["PANG-TAYA  ·  PANG-ARAW-ARAW"], "bahn", (0.32, 0.82, 0.95, 0.92), 3), white)])
    if key == "pisonet":                                          # PISO-NET SA ULAP (new): a coin and three signal arcs
        coin = np.clip((5.0 - np.abs(np.hypot(xx - w * 0.15, yy - h * 0.60) - h * 0.14)) / 1.5 + 0.5, 0, 1)
        one = L(["1"], "impact", (0.11, 0.48, 0.19, 0.72), 5)
        arcs = np.zeros((h, w))
        rr, aa = np.hypot(xx - w * 0.15, yy - h * 0.40), np.degrees(np.arctan2(h * 0.40 - yy, xx - w * 0.15))
        for k in range(3):
            arcs = np.maximum(arcs, np.clip((3.5 - np.abs(rr - (16.0 + k * 13.0))) / 1.5 + 0.5, 0, 1) * (np.abs(aa - 90) < 42))
        return sheet(cyan, [(coin, gold), (one, gold), (arcs, cyan), (L(["PISO-NET"], "impact", (0.30, 0.14, 0.95, 0.60), 1), cyan),
                            (L(["SA ULAP  ·  PISO, LIMANG MINUTO"], "bahn", (0.30, 0.66, 0.95, 0.82), 2), white)])
    if key == "lata":                                             # LATA KOLA (new): a can and a star
        can = T.box_mask(xx, yy, w * 0.15, h * 0.52, w * 0.065, h * 0.30, 9.0)
        inner = T.box_mask(xx, yy, w * 0.15, h * 0.52, w * 0.065 - 6, h * 0.30 - 6, 6.0)
        band = can * (np.abs(yy - h * 0.52) < h * 0.07)
        return sheet(mag, [(can - inner, white), (band, mag), (L(["LATA"], "impact", (0.28, 0.14, 0.61, 0.66), 1), white), (L(["KOLA"], "impact", (0.63, 0.14, 0.95, 0.66), 2), mag),
                           (L(["TUMBA SA UHAW  ·  MALAMIG"], "bahn", (0.28, 0.72, 0.95, 0.86), 3), gold)])
    if key == "lugaw":                                            # MANG KANOR LUGAWAN (new): a bowl, a spoon, steam
        bowl = np.clip((w * 0.12 - np.hypot(xx - w * 0.16, (yy - h * 0.56) * 1.4)) / 1.5 + 0.5, 0, 1) * (yy > h * 0.56)
        steam = np.zeros((h, w))
        for k in (-1, 1):
            sx = w * 0.16 + k * w * 0.04 + np.sin((yy - h * 0.1) / 8.0 + k) * 4.0
            steam = np.maximum(steam, np.clip((3.0 - np.abs(xx - sx)) / 1.5 + 0.5, 0, 1) * (yy > h * 0.22) * (yy < h * 0.48))
        return sheet(gold, [(bowl, "f0dcb0"), (steam, white), (L(["Mang Kanor"], "print", (0.34, 0.10, 0.95, 0.42), 1, wobble=1.6, lean=-0.05), "f0dcb0"),
                            (L(["LUGAWAN"], "impact", (0.34, 0.42, 0.95, 0.80), 2), gold), (L(["MAY ITLOG  ·  24 ORAS"], "bahn", (0.34, 0.82, 0.95, 0.92), 3), white)])
    if key == "turon":                                            # ALING NENA TURON (new): three rolls on a stick
        rolls = np.zeros((h, w))
        for k in range(3):
            rolls = np.maximum(rolls, T.box_mask(xx, yy, w * 0.16, h * (0.34 + k * 0.17), w * 0.085, h * 0.06, 10.0))
        stick = np.clip((2.5 - np.abs(xx - w * 0.16)) / 1.5 + 0.5, 0, 1) * (yy > h * 0.20) * (yy < h * 0.90)
        return sheet(mint, [(stick, white), (rolls, gold), (L(["Aling Nena"], "print", (0.32, 0.12, 0.95, 0.44), 1, wobble=1.6, lean=-0.05), mint),
                            (L(["TURON"], "impact", (0.32, 0.44, 0.74, 0.86), 2), gold), (L(["MAINIT", "PA"], "bahn", (0.78, 0.50, 0.95, 0.82), 3), white)])
    if key == "taxi":                                             # KUYA JUN HOVER TAXI (new): a checker band
        chk = ((np.floor(xx / 16.0) + np.floor(yy / 16.0)) % 2 == 0) * (yy > h * 0.74) * (yy < h * 0.86) * (xx > w * 0.06) * (xx < w * 0.94)
        return sheet(gold, [(ndimage.gaussian_filter(chk.astype(float), 0.7), gold), (L(["Kuya Jun"], "print", (0.06, 0.10, 0.50, 0.44), 1, wobble=1.6, lean=-0.05), white),
                            (L(["HOVER TAXI"], "impact", (0.06, 0.40, 0.94, 0.72), 2), gold), (L(["SAKAY NA  ·  KAHIT SAAN"], "bahn", (0.54, 0.16, 0.94, 0.32), 3), cyan)])
    raise KeyError(key)


def ads():
    """The two strips. Each ad is painted at AD_PX and laid into its cell AD_GUTTER px clear of the
    cell's sides (see AD_GUTTER): brought down whole, the same in both directions, so it is not
    stretched. Its colour is brought down under its own alpha, so no dark fringe is made."""
    rgb = np.zeros((ADS_H, ADS_W, 3))
    alpha = np.zeros((ADS_H, ADS_W))
    w, h = AD_PX
    gx, gy = AD_GUTTER
    iw, ih = w - 2 * gx, h - 2 * gy
    for s, strip in enumerate(STRIPS):
        for k, key in enumerate(strip):
            c, a = ad(key, 9000 + s * 100 + k * 10)
            lit = np.dstack([c * a[..., None], a]).astype(np.float32)
            small = np.dstack([np.asarray(Image.fromarray(lit[..., i], mode="F").resize((iw, ih), Image.LANCZOS)) for i in range(4)])
            sa = np.clip(small[..., 3], 0, 1)
            sc = np.clip(small[..., :3] / np.maximum(sa, 1e-4)[..., None], 0, 1)
            y0, x0 = k * h + gy, s * w + gx
            rgb[y0:y0 + ih, x0:x0 + iw] = sc
            alpha[y0:y0 + ih, x0:x0 + iw] = sa
    rgb = bleed(rgb, alpha)
    save("ads", rgb, alpha)
    save("ads_emit", rgb * alpha[..., None])


# ------------------------------------------------------------------ the hardware

def metal():
    """The emitters' steel: dark blue-violet plates 4 m square with soft joints, one in six repainted."""
    S = 512
    img = T.fill(S, S, "3c4468")
    img = T.coat(img, "4a5380", 150, 0.4, 2101)
    img = T.coat(img, "303858", 110, 0.22, 2103)
    ix, iy, lx, ly = T.grid(4, 4, 1.6, 2110, S, S)
    plate = T.box_mask(lx, ly, 0.0, 0.0, 61.0, 61.0, 6.0, feather=1.8)
    shade = T.table(4, 4, 2111, 0.93, 1.08)[iy, ix]
    img = img * (shade * plate + (1 - plate) * 0.80)[..., None]
    bolt = T.box_mask(np.abs(lx), np.abs(ly), 50.0, 50.0, 3.0, 3.0, 2.5)
    img = T.lay(img, bolt * 0.6, "232842")
    save("metal", img)
    save("metal_emit", img * 0.30)


def led():
    h = w = 256
    img = np.zeros((h, w, 3))
    yy = np.mgrid[0:h, 0:w][0].astype(float)
    for k, name in enumerate(LED):
        row = (h - 1 - yy) // 32 == k
        centre = 1.0 - np.abs(((h - 1 - yy) % 32) - 15.5) / 16.0
        tone = hexcol(LED_HEX[name])[None, None, :] * (0.78 + 0.22 * np.clip(centre * 1.6, 0, 1))[..., None]
        img = np.where(row[..., None], tone, img)
    save("led", img)
    glow = img.copy()
    for k, name in enumerate(LED):
        if name in ("rope", "dark"):                              # a rope and a dark trim are not lamps
            glow[((h - 1 - yy) // 32 == k)] *= 0.22 if name == "rope" else 0.0
    save("led_emit", glow)


# ------------------------------------------------------------------ the balloon

def island_px(name, size=2048):
    u0, v0, u1, v1 = ISLAND[name]
    return int(round(u0 * size)), int(round((1 - v1) * size)), int(round(u1 * size)), int(round((1 - v0) * size))


def balloon():
    """THE SLIPPER BALLOON, second design (2026-10-05). The owner, of the first: "the balloon slipper
    design looks so off and unsettling". What was wrong with it is written over `balloon` in
    tools/author_arena_holo.py; what the PAINT did wrong was: realistic eyes (whites, pupils,
    highlights) that stare, raised brows, an open mouth with a tongue, and a stitched seam running
    through the cheeks like a mask's edge.

    So: a simple face in the spirit of the reference (the Overwatch balloon cow): two closed happy
    arcs set low and wide apart, a small content smile, two soft cheeks. Nothing else on the face,
    and no seam near it. The colours are the GAME'S OWN slipper's, taken from its artwork
    (Art/ui/brand/tsinelas_hit.png and avatars/avatar_tsinelas.png): the logo's yellow sole #d8c808,
    its maroon line #980818, the avatar strap's amber #f8b828 as the accent. The logo's strap is
    orange, which is the offence colour: the strap here is the maroon lifted to a red, with the
    logo strap's diagonal stripes. The two short ticks by the logo slipper's toe are there too.

    Islands (ISLAND): foot (the front), tread (the back, mirrored: the stamp), rim (v once round),
    and three rows of tube: strap, limb (arms and feet, a pale pad at the tip), scarf."""
    n = 2048
    C = BAL_HEX
    img = T.fill(n, n, C["maroon"])
    img = T.coat(img, "ac1a22", 500, 0.4, 3001, feather=1.0)
    glow = np.zeros((n, n))
    L, W = BALLOON["length"], BALLOON["width"]
    asp = L / W
    hwf = np.vectorize(lambda t: half_width(t, BALLOON["waist"]))
    lnf = np.vectorize(lean)
    hw1 = lambda t: half_width(t, BALLOON["waist"])

    def field(name, mirror):
        x0, y0, x1, y1 = island_px(name)
        w, h = x1 - x0, y1 - y0
        yy, xx = np.mgrid[0:h, 0:w].astype(float)
        fx_ = (xx / (w - 1.0) - 0.5) * FOOT_SPAN[0]
        if mirror:
            fx_ = -fx_
        ft = (1 - yy / (h - 1.0)) * FOOT_SPAN[1] + FOOT_T0
        hw = hwf(ft[:, 0])[:, None]
        ln = lnf(ft[:, 0])[:, None]
        d = (hw - np.abs(fx_ - ln)) + 0.004 * T.smooth(h, w, 90, 3011 + mirror)
        px = FOOT_SPAN[0] / w
        inside = np.clip(d / (px * 1.6) + 0.5, 0, 1) * ((ft > 0.0) & (ft < 1.0))
        return (x0, y0, x1, y1), w, h, fx_, ft, d, px, inside

    def disc(fx_, ft, cx, ct, rx, rt=None):
        """Distance from (cx, ct) in widths, an ellipse rx across and rt along (both in widths)."""
        return np.hypot((fx_ - cx) / rx, (ft - ct) * asp / (rt or rx))

    def stitched_seams(base, fx_, ft, h, inside, seed):
        tpx = FOOT_SPAN[1] / h
        for t0, bow in SEAMS:
            line = ft - (t0 + bow * np.cos(fx_ * math.pi / 0.9)) + 0.003 * T.smooth(base.shape[0], base.shape[1], 70, seed)
            seam = np.clip((tpx * 3.0 - np.abs(line)) / (tpx * 1.2) + 0.5, 0, 1)
            acr = (fx_ * W / 1.6) % 1.0
            st = np.clip((tpx * 2.2 - np.abs(np.abs(line) - tpx * 10.0)) / (tpx * 1.0) + 0.5, 0, 1) * (np.abs(acr - 0.5) < 0.30)
            base = T.lay(base, st * inside, C["stitch"])
            base = T.lay(base, seam * inside * 0.7, C["yellow_line"])
        return base

    # ---- the front
    (x0, y0, x1, y1), w, h, fx_, ft, d, px, inside = field("foot", False)
    foot = T.fill(h, w, C["yellow"])
    foot = T.coat(foot, C["yellow_hi"], 330, 0.36, 3013, feather=1.0)
    foot = T.coat(foot, C["yellow_lo"], 300, 0.24, 3015, feather=1.0)
    foot = stitched_seams(foot, fx_, ft, h, inside, 3021)
    along = ft * L / 1.6
    stitch = np.clip((px * 2.6 - np.abs(d - 0.105)) / (px * 1.2) + 0.5, 0, 1) * (np.abs((along % 1.0) - 0.5) < 0.30) * inside
    foot = T.lay(foot, stitch, C["stitch"])
    # The two ticks by the toe, as on the logo's slipper.
    for k in range(2):
        cx, ct = -0.185 + k * 0.05, 0.905 + k * 0.012
        ang = math.radians(52.0)
        u = (fx_ - cx) * math.cos(ang) + (ft - ct) * asp * math.sin(ang)
        v = -(fx_ - cx) * math.sin(ang) + (ft - ct) * asp * math.cos(ang)
        tick = T.box_mask(u, v, 0.0, 0.0, 0.050, 0.011, 0.010, feather=px * 1.5)
        foot = T.lay(foot, tick * inside * 0.85, C["yellow_line"])
    # A reinforcing patch where the strap goes in: at the toe post and at each side.
    marks = [(lean(BAL["post"]), BAL["post"], 0.085)]
    for sx in (-1, 1):
        marks.append((lean(BAL["anchor"]) + sx * hw1(BAL["anchor"]) * BAL["anchor_x"], BAL["anchor"], 0.080))
    for cx, ct, r in marks:
        rr = disc(fx_, ft, cx, ct, 1.0)
        foot = T.lay(foot, np.clip((r - rr) / (px * 1.6) + 0.5, 0, 1) * inside, C["red"])
        foot = T.lay(foot, np.clip((px * 3.0 - np.abs(rr - r)) / (px * 1.2) + 0.5, 0, 1) * inside, C["maroon"])
        an = np.arctan2((ft - ct) * asp, fx_ - cx)
        st = np.clip((px * 2.0 - np.abs(rr - r * 0.74)) / (px * 1.0) + 0.5, 0, 1) * (np.abs(((an * 8 / math.pi) % 1.0) - 0.5) < 0.3)
        foot = T.lay(foot, st * inside, C["stitch"])
    # THE FACE: two closed happy arcs, a small smile, two cheeks. That is all of it.
    # It has FOUR MOODS, one texture each (BALLOON_FACES): Unity swaps the whole texture on the body
    # for a moment when a slipper hits it (Runtime/Map/ArenaBalloon.cs). Every mood is drawn with the
    # same maroon line, the same cheeks and in the same place; none has whites, pupils or highlights.
    et, ex, er = BAL["eye_t"], BAL["eye_x"], BAL["eye_r"]
    thick = BAL["line"]
    mt, mr = BAL["mouth_t"], BAL["mouth_r"]
    edge = lambda dist, half: np.clip((half - dist) / (px * 1.4) + 0.5, 0, 1)

    def stroke(pts, half=None):
        """A round-ended line through pts, (x in widths, t along)."""
        out = np.zeros((h, w))
        for (ax, at_), (bx, bt) in zip(pts, pts[1:]):
            ux, uy = bx - ax, (bt - at_) * asp
            qx, qy = fx_ - ax, (ft - at_) * asp
            k = np.clip((qx * ux + qy * uy) / max(ux * ux + uy * uy, 1e-9), 0, 1)
            out = np.maximum(out, edge(np.hypot(qx - ux * k, qy - uy * k), half or thick / 2))
        return out

    def draw_face(base, mood):
        face = np.zeros((h, w))
        for sx in (-1, 1):
            cx = sx * ex + lean(et)
            blush = np.clip((1.0 - disc(fx_, ft, sx * BAL["blush_x"] + lean(et), BAL["blush_t"], 0.070, 0.042)) / 0.45, 0, 1)
            base = T.lay(base, blush * inside * (0.95 if mood == "ouch" else 0.78), C["blush"])
            if mood == "happy":                                   # a closed arc, a round end on each foot
                rr = disc(fx_, ft, cx, et, 1.0)
                arc = edge(np.abs(rr - er), thick / 2) * np.clip(((ft - et) * asp + er * 0.18) / (px * 2.0), 0, 1)
                for ex_ in (-1, 1):
                    arc = np.maximum(arc, edge(disc(fx_, ft, cx + ex_ * er * 0.984, et - er * 0.18 / asp, 1.0), thick / 2))
                face = np.maximum(face, arc)
            elif mood == "worried":                               # a small round dot, and a short brow tipped up toward the middle
                face = np.maximum(face, edge(disc(fx_, ft, cx, et - er * 0.1 / asp, 1.0), er * 0.40))
                face = np.maximum(face, stroke([(cx - sx * er * 0.2, et + er * 1.55 / asp), (cx + sx * er * 1.0, et + er * 1.05 / asp)]))
            elif mood == "ouch":                                  # squeezed shut: > and <
                tip = (cx - sx * er * 0.55, et)
                face = np.maximum(face, stroke([(cx + sx * er * 0.95, et + er * 0.80 / asp), tip, (cx + sx * er * 0.95, et - er * 0.80 / asp)]))
            else:                                                 # dizzy: a spiral
                spiral = [(cx + sx * er * 1.12 * (k / 40.0) * math.cos(math.tau * 1.75 * k / 40.0),
                           et + er * 1.12 * (k / 40.0) * math.sin(math.tau * 1.75 * k / 40.0) / asp) for k in range(41)]
                face = np.maximum(face, stroke(spiral, thick * 0.42))
        mx = lean(mt)
        if mood == "happy":
            rr = disc(fx_, ft, mx, mt + mr * 0.55 / asp, 1.0)
            smile = edge(np.abs(rr - mr), thick / 2) * np.clip((-(ft - mt) * asp - mr * 0.05) / (px * 2.0), 0, 1)
            for ex_ in (-1, 1):
                smile = np.maximum(smile, edge(disc(fx_, ft, mx + ex_ * mr * 0.835, mt - mr * 0.05 / asp, 1.0), thick / 2))
            face = np.maximum(face, smile)
        elif mood == "worried":                                   # a small wavy line, and one drop of sweat by its temple
            face = np.maximum(face, stroke([(mx + mr * (k / 6.0 - 1.0) * 1.1, mt - mr * 0.25 / asp + mr * 0.22 * math.sin(k * math.pi / 2.0) / asp) for k in range(13)]))
            dx_, dt_ = ex + er * 2.1 + lean(et), et + er * 1.5 / asp
            drop = np.maximum(edge(disc(fx_, ft, dx_, dt_, 1.0), er * 0.42),
                              stroke([(dx_, dt_), (dx_ - er * 0.18, dt_ + er * 0.95 / asp)], er * 0.16))
            base = T.lay(base, drop * inside, "dff6ff")
            face = np.maximum(face, np.clip(drop - edge(disc(fx_, ft, dx_, dt_, 1.0), er * 0.42 - thick * 0.45), 0, 1) * 0.0)
        elif mood == "ouch":                                      # a small round "o"
            rr = disc(fx_, ft, mx, mt - mr * 0.15 / asp, 0.80, 1.0)
            face = np.maximum(face, edge(np.abs(rr - mr * 0.62), thick / 2))
        else:                                                     # dizzy: a wobbly line, wider
            face = np.maximum(face, stroke([(mx + mr * (k / 8.0 - 1.0) * 1.45, mt - mr * 0.2 / asp + mr * 0.30 * math.sin(k * math.pi / 2.0 + 0.6) / asp) for k in range(17)]))
        return T.lay(base, face * inside, C["maroon"])

    bare = foot.copy()
    foot = draw_face(foot, "happy")
    # Painted gloss: two big soft shapes high on the left. Vinyl, not chrome.
    gl = np.clip(1 - disc(fx_, ft, -0.25, 0.745, 0.050, 0.150), 0, 1) + np.clip(1 - disc(fx_, ft, -0.335, 0.52, 0.030, 0.060), 0, 1)
    gl = ndimage.gaussian_filter(np.clip(gl, 0, 1), 7.0) * inside
    foot = T.lay(foot, gl * 0.55, C["gloss"])
    glow[y0:y1, x0:x1] = gl * 0.3
    img[y0:y1, x0:x1] = img[y0:y1, x0:x1] * (1 - inside[..., None]) + foot * inside[..., None]
    front_box, front_inside = (x0, y0, x1, y1), inside
    moods = {}
    for mood in BALLOON_FACES[1:]:
        moods[mood] = T.lay(draw_face(bare.copy(), mood), gl * 0.55, C["gloss"])
    # Where the two seams cross the front, soft: the strained balloon's seams glow (the worried face's emission).
    tpx = FOOT_SPAN[1] / h
    strain = np.zeros((h, w))
    for t0, bow in SEAMS:
        strain = np.maximum(strain, np.clip(1.0 - np.abs(ft - (t0 + bow * np.cos(fx_ * math.pi / 0.9))) / (tpx * 9.0), 0, 1))
    strain = ndimage.gaussian_filter(strain, 2.0) * inside

    # ---- the back: a designed back. The same yellow a shade deeper, the game's stamp across the
    # middle, a chevron tread at the heel and at the toe, the same stitching.
    (x0, y0, x1, y1), w, h, fx_, ft, d, px, inside = field("tread", True)
    tr = T.fill(h, w, C["yellow_back"])
    tr = T.coat(tr, C["yellow"], 240, 0.36, 3033, feather=1.0)
    tr = T.coat(tr, C["yellow_lo"], 220, 0.26, 3035, feather=1.0)
    tr = stitched_seams(tr, fx_, ft, h, inside, 3037)
    chev = ((ft * L / 5.0 + np.abs(fx_) * 1.8) % 1.0)
    groove = np.clip((0.14 - np.abs(chev - 0.5)) / 0.04 + 0.5, 0, 1) * np.clip((d - 0.13) / (px * 2.0) + 0.5, 0, 1)
    groove = groove * (((ft > 0.03) & (ft < 0.17)) | (ft > 0.84))
    tr = T.lay(tr, groove * 0.7, C["yellow_line"])
    sw_, sh_ = int(w * 0.66), int(h * 0.26)
    st = fit(Image.open(TUMP_STAMP).convert("RGBA").crop((30, 145, 800, 665)), sw_, sh_)
    ink = st[..., 3] * np.clip((0.45 - (st[..., :3] @ np.array([0.3, 0.6, 0.1]))) / 0.3, 0, 1)
    paper = ndimage.gaussian_filter(ndimage.binary_fill_holes(ink > 0.4).astype(float), 1.2)
    ox_, oy_ = (w - sw_) // 2, int(h * (1 - (0.41 - FOOT_T0) / FOOT_SPAN[1])) - sh_ // 2
    part = tr[oy_:oy_ + sh_, ox_:ox_ + sw_]
    part = T.lay(part, paper * 0.92, C["cream"])
    part = T.lay(part, ink, C["maroon"])
    tr[oy_:oy_ + sh_, ox_:ox_ + sw_] = part
    stitch = np.clip((px * 2.6 - np.abs(d - 0.105)) / (px * 1.2) + 0.5, 0, 1) * (np.abs(((ft * L / 1.6) % 1.0) - 0.5) < 0.30) * inside
    tr = T.lay(tr, stitch, C["stitch"])
    img[y0:y1, x0:x1] = img[y0:y1, x0:x1] * (1 - inside[..., None]) + tr * inside[..., None]

    # ---- the rim band: v runs once round the slipper, u across the band.
    x0, y0, x1, y1 = island_px("rim")
    w, h = x1 - x0, y1 - y0
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    u = xx / (w - 1.0)
    rim = T.fill(h, w, C["maroon"])
    rim = T.coat(rim, "b01c26", 300, 0.4, 3041, feather=1.0, stretch=(1.0, 0.5))
    rim = T.coat(rim, "84101a", 260, 0.25, 3043, feather=1.0, stretch=(1.0, 0.5))
    for uu in (0.10, 0.90):
        st = np.clip((3.0 - np.abs(xx - uu * w)) / 1.4 + 0.5, 0, 1) * (np.abs((yy / 26.0) % 1.0 - 0.5) < 0.30)
        rim = T.lay(rim, st, C["stitch"])
    seam = np.clip((3.0 - np.abs((yy % (h / 8.0)) - h / 16.0)) / 1.4 + 0.5, 0, 1) * (u > 0.10) * (u < 0.90)
    rim = T.lay(rim, seam * 0.8, "6c0c14")
    bead = ndimage.gaussian_filter(np.clip(1 - np.abs(u - 0.62) / 0.07, 0, 1), 3.0)
    rim = T.lay(rim, bead * 0.4, "e06a70")
    img[y0:y1, x0:x1] = rim

    # ---- the three rows of tube: u along it, v round it.
    def row(name):
        x0, y0, x1, y1 = island_px(name)
        w, h = x1 - x0, y1 - y0
        yy, xx = np.mgrid[0:h, 0:w].astype(float)
        return (x0, y0, x1, y1), w, h, xx / (w - 1.0), yy / (h - 1.0), xx, yy

    box, w, h, uu, vv, xx, yy = row("strap")
    st_ = T.fill(h, w, C["red"])
    st_ = T.coat(st_, "cc3a40", 200, 0.4, 3051, feather=1.0, stretch=(0.5, 1.0))
    st_ = T.coat(st_, "a01a24", 180, 0.25, 3053, feather=1.0, stretch=(0.5, 1.0))
    # The logo strap's diagonal stripes: WIDE and FAINT. (At 46 px and 0.32 they read as a segmented,
    # busy limb from the stage; the strap is one smooth red band with a hint of the logo's stripe.)
    stripe = np.clip((0.20 - np.abs(((xx + yy * 1.4) / 92.0) % 1.0 - 0.5)) / 0.08 + 0.5, 0, 1)
    st_ = T.lay(st_, stripe * 0.15, "d8565c")
    gloss = ndimage.gaussian_filter(np.clip(1 - np.abs(vv - 0.30) / 0.07, 0, 1) * ((uu > 0.25) & (uu < 0.9)), 4.0)
    st_ = T.lay(st_, gloss * 0.45, "ffd0cc")
    img[box[1]:box[3], box[0]:box[2]] = st_

    box, w, h, uu, vv, xx, yy = row("limb")
    lb = T.fill(h, w, C["yellow"])
    lb = T.coat(lb, C["yellow_hi"], 160, 0.36, 3061, feather=1.0)
    lb = T.coat(lb, C["yellow_lo"], 150, 0.22, 3063, feather=1.0)
    lb = T.lay(lb, np.clip((3.0 - np.abs(xx - 0.20 * w)) / 1.4 + 0.5, 0, 1) * 0.8, C["yellow_line"])          # the seam where it joins the body
    lb = T.lay(lb, np.clip((2.4 - np.abs(xx - 0.235 * w)) / 1.2 + 0.5, 0, 1) * (np.abs((yy / 15.0) % 1.0 - 0.5) < 0.30), C["stitch"])
    pad = np.clip((uu - 0.80 + 0.012 * np.sin(vv * math.tau * 3)) / 0.012 + 0.5, 0, 1)                        # a pale pad on the tip
    lb = T.lay(lb, pad, C["cream"])
    lb = T.lay(lb, np.clip((2.6 - np.abs(xx - 0.80 * w)) / 1.2 + 0.5, 0, 1) * 0.6, C["yellow_line"])
    img[box[1]:box[3], box[0]:box[2]] = lb

    box, w, h, uu, vv, xx, yy = row("scarf")
    sc = T.fill(h, w, C["cream"])
    sc = T.coat(sc, "fff6e0", 160, 0.36, 3071, feather=1.0)
    sc = T.coat(sc, "e4d6b4", 150, 0.22, 3073, feather=1.0)
    for v0, colr, half in ((0.24, C["red"], 0.060), (0.40, C["amber"], 0.035), (0.74, C["red"], 0.060), (0.90, C["amber"], 0.035)):
        sc = T.lay(sc, np.clip((half - np.abs(vv - v0 + 0.006 * np.sin(uu * 40.0))) / 0.012 + 0.5, 0, 1), colr)
    img[box[1]:box[3], box[0]:box[2]] = sc

    save("balloon", img)
    # Lit for night from inside and by the stadium's spill: it gives off a good part of its own colour.
    save("balloon_emit", img * 0.70 + glow[..., None] * 0.3)
    # The other moods: the same sheet with the front's face redrawn. The worried one is what a
    # STRAINED balloon wears (three hits and more), and its emission has the two seams glowing.
    x0, y0, x1, y1 = front_box
    for mood, front in moods.items():
        sheet_ = img.copy()
        sheet_[y0:y1, x0:x1] = sheet_[y0:y1, x0:x1] * (1 - front_inside[..., None]) + front * front_inside[..., None]
        save("balloon_" + mood, sheet_)
        emit = sheet_ * 0.70 + glow[..., None] * 0.3
        if mood == "worried":
            hot = np.zeros((n, n))
            hot[y0:y1, x0:x1] = strain
            emit = np.clip(emit + hot[..., None] * (np.array(hexcol("ffd0b0")) - emit) * 0.9, 0, 1)
        save("balloon_%s_emit" % mood, emit)


def options_sheet(version):
    """The three balloon designs side by side for the owner: the blockouts' two pictures (rendered by
    tools/author_arena_holo.py --options) stacked, with their names."""
    names = ["A  the seated mascot: arms, feet, a scarf", "B  the parade balloon: level, a face on the toe", "C  the pair, tied together"]
    parts = [Image.open(LOGS / ("balloon_options_%s_%s.png" % (k, version))).convert("RGB") for k in ("front", "quarter")]
    w, h = parts[0].size
    im = Image.new("RGB", (w, h * 2 + 70), (8, 10, 26))
    d = ImageDraw.Draw(im)
    f = T.font("bahn", 34)
    for k, p in enumerate(parts):
        im.paste(p, (0, 70 + k * h))
    for k, name in enumerate(names):
        d.text((w * (k + 0.5) / 3 - d.textlength(name, font=f) / 2, 18), name, font=f, fill=(236, 236, 244))
    d.text((16, 76), "from the stage's side, below", font=T.font("bahn", 22), fill=(170, 176, 200))
    d.text((16, 76 + h), "three-quarter", font=T.font("bahn", 22), fill=(170, 176, 200))
    im.save(LOGS / ("balloon_options_%s.png" % version))
    print("[arena-holo-tex] options sheet", version)


# The footbed island's span: (widths across, lengths along), and the t at its bottom edge.
FOOT_SPAN = (1.16, 1.04)
FOOT_T0 = -0.02


def foot_uv(x, t, name="foot"):
    """(u, v) on the `foot` or `tread` island for a point x widths across and t along. The tread is
    seen from the other side, so it is mirrored across."""
    u0, v0, u1, v1 = ISLAND[name]
    fx = (x if name == "foot" else -x) / FOOT_SPAN[0] + 0.5
    ft = (t - FOOT_T0) / FOOT_SPAN[1]
    return u0 + (u1 - u0) * fx, v0 + (v1 - v0) * ft


PAINTERS = {"fx": fx, "ads": ads, "metal": metal, "led": led, "balloon": balloon}


def sheet(version):
    LOGS.mkdir(parents=True, exist_ok=True)
    names = ["fx", "ads", "balloon", "metal", "led"]
    cell = 640
    im = Image.new("RGB", (cell * len(names), cell * 2 + 40), (8, 10, 26))
    d = ImageDraw.Draw(im)
    f = T.font("bahn", 20)
    for k, name in enumerate(names):
        for part, suffix in enumerate(("", "_emit")):
            p = OUT / ("arena_holo_%s%s.png" % (name, suffix))
            if not p.exists():
                continue
            t = Image.open(p).convert("RGBA")
            t.thumbnail((cell - 8, cell - 8))
            bg = Image.new("RGBA", t.size, (8, 10, 26, 255))
            bg.alpha_composite(t)
            im.paste(bg.convert("RGB"), (k * cell + 4, part * cell + 4 + 40))
        d.text((k * cell + 8, 10), name, font=f, fill=(230, 230, 240))
    im.save(LOGS / ("holo_textures_%s.png" % version))
    for name in ("jeepney", "carabao", "rooster", "can", "sampaguita", "tump"):   # each figure on the night, at its own size
        x0, y0, x1, y1 = FX[name]
        t = Image.open(OUT / "arena_holo_fx.png").convert("RGBA").crop((x0, y0, x1, y1))
        bg = Image.new("RGBA", t.size, (8, 10, 30, 255))
        bg.alpha_composite(t)
        bg.convert("RGB").save(LOGS / ("holo_figure_%s_%s.png" % (name, version)))
    print("[arena-holo-tex] sheet", version)


def main():
    _lazy()
    only = [a for a in sys.argv[1:] if a in PAINTERS]
    for name, fn in PAINTERS.items():
        if (not only and "--options-sheet" not in sys.argv) or name in only:
            fn()
    for i, a in enumerate(sys.argv):
        if a == "--sheet" and i + 1 < len(sys.argv):
            sheet(sys.argv[i + 1])
        if a == "--options-sheet" and i + 1 < len(sys.argv):
            options_sheet(sys.argv[i + 1])


if __name__ == "__main__":
    main()
