"""Paint the arena hull kit's textures, in the house illustrated style (ARENA-1.4, hull kit).

  py -3 tools/author_arena_textures_hull.py [--sheet vN]

Writes Assets/TumbangPreso/Art/Arena/Textures/arena_hull_<surface>.png (and _emit.png where the
surface glows) and a swatch sheet Logs/arena/hull/swatches_<vN>.png. The model is
tools/author_arena_hull.py, which reads these files by name; material names are
arena_hull_<surface>.

THE STYLE (docs/ARENA_ART_BRIEF.md, KANTO_DESIGN_GUIDE.md section 3): FLAT fills, a few LARGE
patches with FEATHERED organic edges, hand-wobbled joints at low contrast with one flat highlight
band, no grain, no noise, no streaks, no cracks. Every surface is its own drawing, written here as
its own function. Night is carried by the emission maps and Unity's look profile: the albedos are
mid-dark slate navies, not black.

SCALE: every tiling texture is 1024 px for 16 m (64 px per metre), and the model's UVs are laid at
that density. Which way a tile runs on the model:
    plate_a   dark hull plate. u runs round the hull, v down its slope. Plates 8 m x 4 m in
              running bond, an access hatch, three ports.
    plate_b   the lighter plate. Long 2 m strakes, one butt joint each, one darker painted strake.
    paving    the plaza. u runs round the stadium, v outward: slab courses 3 m deep, a band of
              small setts, a dark drain line. Laid in rings, so the plaza reads as a round floor.
    steel     ribs, frames, posts, rails. Nearly flat: two broad coats and a soft seam every 4 m.
    engine    gunmetal casing: hoop seams every 2 m, panel lines, a row of bolts, two heat fields.
    shaft     ONE BAY of the shaft's wall (the wall between two ribs is one tile wide, 16.1 m):
              three panel columns, the middle one a dark cable tray with rungs, a louvre, an
              access door, a control-room slit window (emission), a hazard band, indicator lamps.
ATLASES (the model picks a region; the numbers are repeated in author_arena_hull.py):
    glass     rows that tile in u: the gate halls' lit curtain wall (10 m tall, mullions every
              2 m, the hall inside drawn as flat shapes: floor, turnstiles, people, a balcony),
              the flank's row of windows (lit rooms in runs), a dark glass strip (shuttle
              canopies), the rim balustrade (posts every 2 m).
    light     eight rows: deep blue, magenta, cyan, white, and the same four as dashes.
    glow      the engines' throat: a ramp in v from the dark lip to the white core, flat bands.
    pad       one landing pad, 20 m across: ring, cross, approach chevrons, hazard arc, lamps.
    trim      one-off drawings: four gate signs, a door, a cabinet, a vent, a hazard band, a
              hedge (seen from above and the side), a shuttle's side, a coil emitter, flat livery.

ROLE HUES: nothing near offence orange #f87020 or defence blue #0080e8. The blues are slate navy
and indigo; the light bands are deep blue #0a1a9a, magenta and cyan; the hazard paint is mustard.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
from author_ilalim_textures_props import coat, hexcol, patches, smooth, text_mask  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "TumbangPreso" / "Art" / "Arena" / "Textures"
SHEETS = ROOT / "Logs" / "arena" / "hull"
S = 1024
M = 64.0            # pixels per metre

# Atlas regions in IMAGE pixels (x0, y0, x1, y1), top-left origin. author_arena_hull.py repeats
# these numbers (Blender's Python has no scipy to import this module).
GLASS = {"curtain": (0, 0, 1024, 640), "ports": (0, 656, 1024, 768), "dark": (0, 784, 1024, 912), "rail": (0, 928, 1024, 1024)}
TRIM = {
    "sign_n": (0, 0, 384, 96), "sign_e": (0, 96, 384, 192), "sign_s": (0, 192, 384, 288), "sign_w": (0, 288, 384, 384),
    "door": (400, 0, 560, 208), "cabinet": (576, 0, 832, 208), "livery": (848, 0, 1024, 208),
    "vent": (400, 224, 1024, 384), "hazard": (0, 400, 1024, 464), "hedge": (0, 480, 1024, 736),
    "shuttle": (0, 752, 640, 944), "coil": (656, 752, 1024, 1008),
}
LIGHT_ROWS = ("blue", "magenta", "cyan", "white", "blue_dash", "magenta_dash", "cyan_dash", "white_dash")
LIGHT_HEX = {"blue": "0a1a9a", "magenta": "d8168c", "cyan": "38dcf4", "white": "eaf4ff"}


class Canvas:
    """A drawing with one hand-wobble field shared by everything on it, so joints, panels and
    marks drift together like one hand drew them. The wobble is periodic: tiles stay seamless."""

    def __init__(self, base, seed, wobble=2.2, size=(S, S)):
        self.h, self.w = size
        self.img = np.ones((self.h, self.w, 3)) * hexcol(base)
        self.yy, self.xx = np.mgrid[0:self.h, 0:self.w].astype(float)
        self.y = self.yy + smooth(self.h, self.w, 70, seed) * wobble
        self.x = self.xx + smooth(self.h, self.w, 70, seed + 1) * wobble

    def rect(self, box, radius=0.0, feather=1.2):
        x0, y0, x1, y1 = box
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        hx, hy = (x1 - x0) / 2 - radius, (y1 - y0) / 2 - radius
        qx, qy = np.abs(self.x - cx) - hx, np.abs(self.y - cy) - hy
        d = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - radius
        return np.clip(-d / feather + 0.5, 0, 1)

    def frame(self, box, band, radius=0.0):
        x0, y0, x1, y1 = box
        return np.clip(self.rect(box, radius) - self.rect((x0 + band, y0 + band, x1 - band, y1 - band),
                                                          max(0.0, radius - band)), 0, 1)

    def disc(self, cx, cy, r, feather=1.2):
        return np.clip((r - np.hypot(self.x - cx, self.y - cy)) / feather + 0.5, 0, 1)

    def ring(self, cx, cy, r, width, feather=1.2):
        return np.clip((width / 2 - np.abs(np.hypot(self.x - cx, self.y - cy) - r)) / feather + 0.5, 0, 1)

    def lay(self, mask, colour, k=1.0):
        m = (np.clip(mask, 0, 1) * k)[..., None]
        self.img = self.img * (1 - m) + hexcol(colour) * m

    def coat(self, shift, scale, coverage, seed, feather=1.2, stretch=(1.0, 1.0)):
        self.img = coat(self.img, (shift,) * 3 if np.isscalar(shift) else shift, scale, coverage, seed, feather, stretch)


def pdist(coord, period, at=0.0):
    """Distance to the nearest of the lines at + k * period, signed."""
    return (coord - at + period / 2) % period - period / 2


def line(d, width, feather=1.0):
    return np.clip((width / 2 - np.abs(d)) / feather + 0.5, 0, 1)


def save(name, img):
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(OUT / f"arena_hull_{name}.png")
    print("[arena-hull-tex]", name)


# ------------------------------------------------------------------ the tiling surfaces (16 m tiles)

def plate_a():
    c = Canvas("3a4460", 101)
    ph, pw = S / 4, S / 2                      # plates 4 m deep, 8 m long
    tones = (("3a4460", "35405a"), ("3f4a67", "3a4460"), ("36405b", "3c4763"), ("3a4460", "333d57"))
    row = np.floor(c.y / ph).astype(int) % 4
    xs = c.x + (row % 2) * pw / 2
    col = np.floor(xs / pw).astype(int) % 2
    for r in range(4):
        for k in range(2):
            c.img[(row == r) & (col == k)] = hexcol(tones[r][k])
    c.coat(1.07, 260, 0.22, 111, feather=1.3)                      # two LARGE feathered fields
    c.coat(0.93, 210, 0.16, 112, feather=1.3)
    dy, dx = pdist(c.y, ph), pdist(xs, pw)
    c.lay(np.maximum(line(dy - 4.5, 5), line(dx - 4.5, 5)), "4c5777", 0.55)   # one flat highlight band
    c.lay(np.maximum(line(dy, 3.2), line(dx, 3.2)), "262d44", 0.9)            # the joint
    hatch = (150, 300, 320, 384)                                   # an access hatch and three ports
    c.lay(c.rect(hatch, 14), "465272")
    c.lay(c.frame(hatch, 4, 14), "272e45", 0.9)
    for k in range(3):
        c.lay(c.disc(700 + k * 58, 852, 13), "272e45", 0.9)
        c.lay(c.disc(700 + k * 58, 852, 7), "4c5777", 0.8)
    save("plate_a", c.img)


def plate_b():
    c = Canvas("566184", 201)
    sh = S / 8                                   # strakes 2 m deep, 16 m long, one butt joint each
    joints = (0.15, 0.60, 0.35, 0.85, 0.10, 0.50, 0.75, 0.30)
    tones = ("566184", "5b678b", "566184", "515c7e", "566184", "3f4a6b", "5b678b", "566184")
    row = np.floor(c.y / sh).astype(int) % 8
    for r in range(8):
        c.img[row == r] = hexcol(tones[r])
    c.coat(1.06, 280, 0.2, 211, feather=1.3)
    c.coat(0.94, 190, 0.15, 212, feather=1.3)
    dy = pdist(c.y, sh)
    c.lay(line(dy - 4.0, 4), "6f7ba3", 0.5)
    c.lay(line(pdist(c.y, sh, sh / 2), 2.2), "4a5576", 0.45)      # a stiffener line down each strake
    seam = line(dy, 3.0)
    for r, j in enumerate(joints):
        seam = np.maximum(seam, line(pdist(c.x, S, j * S), 3.0) * (row == r))
    c.lay(seam, "3b4462", 0.9)
    vent = (610, 150, 800, 232)                                   # a slatted relief vent in one strake
    c.lay(c.rect(vent, 8), "3f4868")
    for k in range(4):
        c.lay(c.rect((624, 162 + k * 16, 786, 170 + k * 16), 3), "2a3149", 0.9)
    save("plate_b", c.img)


def paving():
    c = Canvas("71768c", 301, wobble=1.8)
    # v runs outward. Courses, in pixels down the tile: (top, bottom, slab width, stagger, kind)
    courses = ((32, 224, 128, 0, 0), (224, 416, 128, 64, 1), (416, 464, 32, 0, 2), (464, 512, 32, 16, 2),
               (512, 704, 256, 0, 3), (704, 896, 128, 64, 4), (896, 1024, 128, 0, 5))
    slab_tones = ("71768c", "71768c", "777c93", "71768c", "6b7086")
    joint = np.zeros((S, S))
    for top, bottom, wdt, stag, kind in courses:
        inside = (c.y >= top) & (c.y < bottom)
        if kind == 2:
            c.img[inside] = hexcol("858aa0")
        else:
            idx = np.floor((c.x + stag) / wdt).astype(int)
            for t in range(5):
                c.img[inside & ((idx * 2 + kind * 3) % 5 == t)] = hexcol(slab_tones[t])
        joint = np.maximum(joint, line(pdist(c.x + stag, wdt), 2.4) * inside)
        joint = np.maximum(joint, line(c.y - top, 2.4))
    c.img[c.y < 32] = hexcol("4a4e62")                            # the drain line, 0.5 m
    c.lay(line(c.y - 16, 5), "353849", 0.8)
    c.coat(1.05, 300, 0.22, 311, feather=1.3)
    c.coat(0.95, 220, 0.15, 312, feather=1.3)
    c.lay(joint, "595e72", 0.7)
    save("paving", c.img)


def steel():
    c = Canvas("7f8899", 401)
    c.coat(1.06, 240, 0.25, 411, feather=1.3)
    c.coat(0.94, 180, 0.18, 412, feather=1.3)
    c.lay(line(pdist(c.y, S / 4), 3.0), "67707f", 0.55)
    c.lay(line(pdist(c.y, S / 4, 5.0), 3.0), "949cac", 0.4)
    save("steel", c.img)


def engine():
    c = Canvas("4a4c5c", 501)
    hoop = S / 8                                 # hoop seams every 2 m, panel lines every 4 m, staggered
    row = np.floor(c.y / hoop).astype(int) % 8
    c.img[(row == 2) | (row == 6)] = hexcol("424454")
    heat = patches(S, S, 250, 0.2, 511, feather=1.3)              # two LARGE heat-tinted fields
    c.lay(heat, "62506a", 0.55)
    c.coat(1.08, 200, 0.16, 512, feather=1.3)
    dy = pdist(c.y, hoop)
    dx = pdist(c.x + (row % 2) * S / 8, S / 4)
    c.lay(line(dy - 4.0, 4), "656880", 0.5)
    c.lay(np.maximum(line(dy, 3.0), line(dx, 2.4)), "2f3140", 0.9)
    bolts = np.zeros((S, S))                                      # a row of bolts along two of the hoops
    for by in (hoop * 2 + 14, hoop * 6 + 14):
        for k in range(32):
            bolts = np.maximum(bolts, c.disc(16 + k * 32, by, 4.5))
    c.lay(bolts, "74788f", 0.9)
    save("engine", c.img)


def shaft():
    """One bay of the shaft wall. The ribs cover u < 0.10 and u > 0.90; the drawing lives between."""
    c = Canvas("3b466c", 601)
    e = Canvas("000000", 601)
    x0, x1 = 104, 920
    pw = (x1 - x0) / 3                           # three panel columns, 4.25 m each; four rows of 4 m
    for k, tone in enumerate(("3b466c", "2a3252", "414d75")):
        c.img[(c.x >= x0 + k * pw) & (c.x < x0 + (k + 1) * pw)] = hexcol(tone)
    c.coat(1.09, 280, 0.22, 611, feather=1.3)
    c.coat(0.92, 200, 0.14, 612, feather=1.3)
    side = (c.x < x0 + pw) | (c.x >= x0 + 2 * pw)
    dy = pdist(c.y, S / 4)
    c.lay(line(dy - 4.5, 5) * side, "596690", 0.55)
    c.lay(line(dy, 3.2) * side, "1a2036", 0.9)
    for k in range(4):                           # the panel columns' edges
        c.lay(line(c.x - (x0 + k * pw), 4.0), "596690", 0.8)
        c.lay(line(c.x - (x0 + k * pw) - 4.0, 3.0), "1a2036", 0.8)
    tray = (c.x > x0 + pw + 40) & (c.x < x0 + 2 * pw - 40)        # the cable tray: rungs every metre
    c.lay(line(pdist(c.y, M, 20), 9) * tray, "475380", 0.9)
    for xr in (x0 + pw + 40, x0 + 2 * pw - 40):
        c.lay(line(c.x - xr, 7), "596690", 0.9)
    louvre = (x0 + 46, 560, x0 + pw - 46, 720)                    # a louvre in the left column
    c.lay(c.rect(louvre, 8), "596690")
    for k in range(6):
        c.lay(c.rect((louvre[0] + 12, louvre[1] + 14 + k * 24, louvre[2] - 12, louvre[1] + 26 + k * 24), 3), "141a2e")
    door = (x0 + 2 * pw + 70, 40, x0 + 2 * pw + 200, 232)         # an access door in the right column
    c.lay(c.rect(door, 10), "596690")
    c.lay(c.frame(door, 4, 10), "1a2036", 0.9)
    port = c.disc(door[0] + 65, door[1] + 52, 17)
    c.lay(port, "8fd0e0"); e.lay(port, "4f97aa")
    slit = c.rect((x0 + 40, 300, x0 + pw - 40, 338), 8)           # a control room's slit window, lit
    c.lay(c.rect((x0 + 30, 290, x0 + pw - 30, 348), 10), "1a2036")
    c.lay(slit, "a9d6e2"); e.lay(slit, "5fa3b6")
    for k in range(3):                                            # the room's mullions
        m = c.rect((x0 + 40 + (k + 1) * (pw - 80) / 4 - 3, 296, x0 + 40 + (k + 1) * (pw - 80) / 4 + 3, 342))
        c.lay(m, "1a2036"); e.lay(m, "000000")
    band = (c.y > 812) & (c.y < 852) & side                       # a hazard band low on the side columns
    stripes = (np.floor((c.x + c.y) / 28).astype(int) % 2 == 0)
    c.img[band & stripes] = hexcol("cfa93a")
    c.img[band & ~stripes] = hexcol("1b2138")
    for cx, cy, colour in ((door[2] + 34, 70, "ffcf5a"), (door[2] + 34, 110, "58e6ff"), (louvre[0] - 24, 590, "58e6ff"),
                           (x0 + 2 * pw + 60, 620, "ffcf5a")):    # indicator lamps
        lamp = c.disc(cx, cy, 7)
        c.lay(c.disc(cx, cy, 11), "141a2e"); c.lay(lamp, colour); e.lay(lamp, colour)
    save("shaft", c.img)
    save("shaft_emit", e.img)


# ------------------------------------------------------------------ the atlases

def glass():
    c = Canvas("2a3048", 701, wobble=0.8)
    e = Canvas("000000", 701, wobble=0.8)
    # The curtain wall, rows 0..768 (12 m). Ground is at the bottom of the region (y = 768).
    top, ground = GLASS["curtain"][1], GLASS["curtain"][3]
    hall = (c.yy >= top) & (c.yy < ground)
    c.img[hall] = hexcol("d9c391")
    for box, tone in (((0, top, S, ground - 400), "8d8068"),      # the upper level, dimmer
                      ((0, ground - 400, S, ground - 372), "5d5148"),     # the balcony's edge
                      ((0, ground - 70, S, ground), "b39a6a")):  # the floor
        c.lay(c.rect(box), tone)
    c.coat(1.07, 220, 0.25, 711, feather=1.3)
    for k, x in enumerate((90, 250, 420, 600, 770, 930)):         # turnstiles and people, flat silhouettes
        c.lay(c.rect((x - 26, ground - 132, x + 26, ground - 62), 8), "6b5a5c")
        if k % 2 == 0:
            c.lay(c.rect((x + 44, ground - 166, x + 70, ground - 62), 11), "5a4a55")
            c.lay(c.disc(x + 57, ground - 180, 12), "5a4a55")
    for x in (170, 510, 850):                                     # banners hung from the balcony
        c.lay(c.rect((x - 42, ground - 366, x + 42, ground - 232), 6), ("9b3f7c", "3d4a8c", "9b3f7c")[(x // 300) % 3])
    for x in (0, 340, 680):                                       # columns inside the hall
        c.lay(c.rect((x + 150, top, x + 186, ground)), "b89a66")
    e.img[hall] = c.img[hall] * 0.8
    mull = np.maximum(line(pdist(c.x, 128), 10), line(pdist(c.y, 192, ground), 10)) * hall
    mull = np.maximum(mull, line(c.y - (ground - 6), 14) * hall)
    c.lay(mull, "2a3048"); e.lay(mull, "000000")
    # The flank's windows, rows 656..768 (1.75 m): a dark band, lit rooms in runs, dark ones between.
    y0, y1 = GLASS["ports"][1], GLASS["ports"][3]
    band = (c.yy >= y0 - 8) & (c.yy < y1 + 8)
    c.img[band] = hexcol("262d44")
    e.img[band] = 0
    lit = (1, 1, 1, 0, 1, 1, 0, 0, 1, 1, 1, 1, 0, 1, 0, 0, 1, 1, 0, 1, 1, 1, 0, 0, 0, 1, 1, 0, 1, 1, 1, 0)
    for k, on in enumerate(lit):                 # 32 windows, 0.5 m apart: runs of lit rooms
        pane = c.rect((k * 32 + 6, y0 + 34, k * 32 + 26, y1 - 34), 5)
        c.lay(pane, "c9dfe6" if on else "39435f")
        if on:
            e.lay(pane, "8fb9c6" if k % 5 else "c9b98f")
    # The dark glass strip, rows 784..912: flat teal-navy with one gentle gradient.
    y0, y1 = GLASS["dark"][1], GLASS["dark"][3]
    strip = (c.yy >= y0 - 8) & (c.yy < y1 + 8)
    grad = np.clip((c.yy - y0) / (y1 - y0), 0, 1)[..., None]
    c.img[strip] = (hexcol("2f4f6c") * (1 - grad) + hexcol("1a2a40") * grad)[strip]
    # The balustrade, rows 928..1024 (1.5 m): glass, a top rail, a post every 2 m.
    y0 = GLASS["rail"][1]
    rail = c.yy >= y0 - 8
    c.img[rail] = hexcol("36566c")
    c.lay(line(pdist(c.x, 128), 8) * rail, "8a93a5")
    c.lay(c.rect((0, y0 - 8, S, y0 + 14)), "8a93a5")
    c.lay(c.rect((0, S - 12, S, S)), "67707f")
    e.img[c.yy >= GLASS["curtain"][3]] = 0
    save("glass", c.img)
    save("glass_emit", e.img)


def light():
    img = np.zeros((S, S, 3))
    emit = np.zeros((S, S, 3))
    yy, xx = np.mgrid[0:S, 0:S].astype(float)
    wob = smooth(S, S, 90, 801) * 3.0
    for k, name in enumerate(LIGHT_ROWS):
        colour = hexcol(LIGHT_HEX[name.split("_")[0]])
        mid = k * 128 + 64
        core = np.clip((46 - np.abs(yy + wob - mid)) / 6 + 0.5, 0, 1)   # a bright core, a dimmer edge
        row = (yy >= k * 128) & (yy < (k + 1) * 128)
        level = 0.55 + 0.45 * core
        if name.endswith("_dash"):               # four lamps per tile: 2.5 m lit, 1.5 m dark
            d = np.abs(pdist(xx + wob, S / 4, S / 8))
            level = level * np.clip((80 - d) / 5 + 0.5, 0, 1)
        img[row] = (colour * (0.25 + 0.75 * level[..., None]) + (1 - level[..., None]) * hexcol("1b2138") * 0.6)[row]
        emit[row] = (colour * level[..., None])[row]
    save("light", img)
    save("light_emit", emit)


def glow():
    c = Canvas("15183a", 901, wobble=4.0)
    e = Canvas("000000", 901, wobble=4.0)
    # v = 0 is the bell's lip (the bottom row), v = 1 the throat's core (the top row): flat bands.
    for edge, colour, lit in ((0.80, "0a1a9a", "0a1a9a"), (0.56, "2231c8", "1a2ad0"), (0.36, "38dcf4", "38dcf4"),
                              (0.20, "b8f4ff", "c8f8ff"), (0.08, "ffffff", "ffffff")):
        band = np.clip((edge * S - c.y) / 7 + 0.5, 0, 1)
        c.lay(band, colour); e.lay(band, lit)
    save("glow", c.img)
    save("glow_emit", e.img)


def pad():
    """One pad, 20 m across (51 px per metre). Up the image is toward the stadium."""
    c = Canvas("343c57", 1001, wobble=2.6)
    e = Canvas("000000", 1001, wobble=2.6)
    k = S / 20.0
    cx = cy = S / 2
    c.coat(1.08, 260, 0.24, 1011, feather=1.3)
    c.coat(0.93, 200, 0.15, 1012, feather=1.3)
    for a in range(8):                           # the deck's plate joints: eight wedges and a ring
        ang = np.arctan2(c.y - cy, c.x - cx)
        c.lay(line(np.sin(ang - a * np.pi / 4) * np.hypot(c.x - cx, c.y - cy), 3.0) * (np.cos(ang - a * np.pi / 4) > 0)
              * (np.hypot(c.x - cx, c.y - cy) > 3.2 * k), "232a41", 0.7)
    c.lay(c.ring(cx, cy, 6.0 * k, 3.0), "232a41", 0.7)
    c.lay(c.ring(cx, cy, 8.6 * k, 0.5 * k), "d5dbe6")              # the touchdown ring
    ang = np.degrees(np.arctan2(c.x - cx, cy - c.y))               # 0 up the image, clockwise
    hazard = c.ring(cx, cy, 9.55 * k, 0.6 * k) * (np.abs(ang) > 118)
    stripes = np.floor(ang / 5.0).astype(int) % 2 == 0
    c.lay(hazard * stripes, "cfa93a"); c.lay(hazard * ~stripes, "1b2138")
    c.lay(c.disc(cx, cy, 3.0 * k), "2a3149")                       # the centre: a disc and a cross
    c.lay(c.ring(cx, cy, 3.0 * k, 0.3 * k), "d5dbe6")
    c.lay(np.maximum(c.rect((cx - 0.18 * k, cy - 2.2 * k, cx + 0.18 * k, cy + 2.2 * k)),
                     c.rect((cx - 2.2 * k, cy - 0.18 * k, cx + 2.2 * k, cy + 0.18 * k))), "d5dbe6")
    for j in range(3):                           # three chevrons pointing at the stadium
        yb = cy - (4.2 + j * 1.25) * k
        d1 = np.abs((c.x - cx) * 0.55 + (c.y - yb)) / 1.14
        d2 = np.abs(-(c.x - cx) * 0.55 + (c.y - yb)) / 1.14
        arm = np.where(c.x < cx, line(d2 * 1.0, 0.36 * k), line(d1 * 1.0, 0.36 * k)) * (np.abs(c.x - cx) < (2.3 - j * 0.45) * k)
        c.lay(arm, "d5dbe6")
    c.lay(text_mask(S, S, ["TP"], "impact", (cx - 1.9 * k, cy + 4.0 * k, cx + 1.9 * k, cy + 6.2 * k), seed=1021, wobble=1.6),
          "d5dbe6")
    for j in range(16):                          # lamps let into the deck round the ring
        a = np.radians(j * 22.5 + 11.25)
        lamp = c.disc(cx + np.sin(a) * 7.6 * k, cy - np.cos(a) * 7.6 * k, 0.17 * k)
        c.lay(c.disc(cx + np.sin(a) * 7.6 * k, cy - np.cos(a) * 7.6 * k, 0.27 * k), "1b2138")
        c.lay(lamp, "58e6ff"); e.lay(lamp, "58e6ff")
    outside = np.hypot(c.xx - cx, c.yy - cy) > 10.3 * k
    c.img[outside] = hexcol("343c57")
    save("pad", c.img)
    save("pad_emit", e.img)


def trim():
    c = Canvas("3a4460", 1101, wobble=1.5)
    e = Canvas("000000", 1101, wobble=1.5)

    def inset(box, d):
        return (box[0] + d, box[1] + d, box[2] - d, box[3] - d)

    for key, word in (("sign_n", "GATE N"), ("sign_e", "GATE E"), ("sign_s", "GATE S"), ("sign_w", "GATE W")):
        box = TRIM[key]
        c.lay(c.rect(box), "1d2238")
        c.lay(c.frame(inset(box, 5), 4, 8), "5c688c")
        letters = text_mask(S, S, [word], "bahn", inset(box, 20), seed=1110 + box[1], wobble=1.2)
        c.lay(letters, "dff3fa"); e.lay(letters, "9fd8e8")
    box = TRIM["door"]                           # a double door: two leaves, a lit glass panel in each
    c.lay(c.rect(box), "67707f")
    for k in range(2):
        leaf = (box[0] + 8 + k * 74, box[1] + 10, box[0] + 78 + k * 74, box[3] - 4)
        c.lay(c.rect(leaf, 4), "2f3855")
        pane = c.rect((leaf[0] + 12, leaf[1] + 16, leaf[2] - 12, leaf[1] + 120), 5)
        c.lay(pane, "f0d596"); e.lay(pane, "d9bf85")
        c.lay(c.rect((leaf[0] + (48 if k == 0 else 12), leaf[1] + 132, leaf[0] + (58 if k == 0 else 22), leaf[1] + 164), 3), "aab2c2")
    box = TRIM["cabinet"]                        # a machinery cabinet: three bays, a louvre, lamps
    c.lay(c.rect(box), "4c5777")
    for k in range(3):
        bay = (box[0] + 8 + k * 82, box[1] + 10, box[0] + 82 + k * 82, box[3] - 10)
        c.lay(c.rect(bay, 5), ("3f4a67", "36405b", "3f4a67")[k])
        c.lay(c.frame(bay, 3, 5), "262d44", 0.9)
        if k == 1:
            for j in range(5):
                c.lay(c.rect((bay[0] + 12, bay[1] + 70 + j * 20, bay[2] - 12, bay[1] + 80 + j * 20), 3), "1a2036")
        for j in range(3):
            lamp = c.disc(bay[0] + 18 + j * 17, bay[1] + 26, 5)
            colour = ("58e6ff", "ffcf5a", "58e6ff")[(j + k) % 3]
            c.lay(lamp, colour); e.lay(lamp, colour)
    box = TRIM["livery"]
    c.lay(c.rect((box[0] - 8, box[1], box[2], box[3] + 8)), "cfd4e2")
    box = TRIM["vent"]                           # a big louvred vent, 9.75 m x 2.5 m
    c.lay(c.rect(box), "4c5777")
    c.lay(c.rect(inset(box, 12), 8), "1a2036")
    for j in range(5):
        c.lay(c.rect((box[0] + 22, box[1] + 24 + j * 26, box[2] - 22, box[1] + 38 + j * 26), 3), "3a4460")
    for k in (1, 2):
        xm = box[0] + k * (box[2] - box[0]) / 3
        c.lay(c.rect((xm - 5, box[1] + 12, xm + 5, box[3] - 12)), "4c5777")
    box = TRIM["hazard"]                         # a hazard band, tiles in u
    band = (c.yy >= box[1] - 6) & (c.yy < box[3] + 6)
    stripes = np.floor((c.x + c.y) / 32).astype(int) % 2 == 0
    c.img[band & stripes] = hexcol("cfa93a")
    c.img[band & ~stripes] = hexcol("1b2138")
    box = TRIM["hedge"]                          # a clipped hedge: flat greens, big soft leaf masses
    region = (c.yy >= box[1] - 8) & (c.yy < box[3] + 8)
    c.img[region] = hexcol("2f5a3e")
    for scale, cover, seed, colour in ((60, 0.3, 1151, "3c6e48"), (46, 0.2, 1152, "24493a"), (34, 0.1, 1153, "4f8254")):
        m = patches(S, S, scale, cover, seed, feather=0.9) * region
        c.lay(m, colour)
    box = TRIM["shuttle"]                        # a shuttle's side, 10 m x 3 m: nose to the right
    c.lay(c.rect((box[0] - 6, box[1] - 6, box[2] + 6, box[3] + 6)), "cfd4e2")
    c.lay(c.rect((box[0], box[1] + 104, box[2], box[1] + 134)), "8a2a78")       # a magenta cheat line
    c.lay(c.rect((box[0], box[1] + 140, box[2], box[3])), "3a4460")             # a dark belly
    for k in range(6):
        pane = c.rect((box[0] + 150 + k * 62, box[1] + 44, box[0] + 190 + k * 62, box[1] + 88), 9)
        c.lay(pane, "1a2a40")
        if k in (1, 2, 4):
            lit = c.rect((box[0] + 154 + k * 62, box[1] + 48, box[0] + 186 + k * 62, box[1] + 84), 7)
            c.lay(lit, "f0d596"); e.lay(lit, "c9ad70")
    hatch = (box[0] + 60, box[1] + 30, box[0] + 126, box[1] + 136)
    c.lay(c.frame(hatch, 3, 8), "67707f")
    c.lay(text_mask(S, S, ["TP-07"], "bahn", (box[0] + 530, box[1] + 50, box[0] + 630, box[1] + 90), seed=1171, wobble=0.8), "3a4460")
    box = TRIM["coil"]                           # a coil emitter: a frame, fins, a glowing slot
    c.lay(c.rect(box), "4c5777")
    c.lay(c.rect(inset(box, 14), 10), "1a2036")
    for j in range(7):
        c.lay(c.rect((box[0] + 30 + j * 46, box[1] + 30, box[0] + 48 + j * 46, box[3] - 30), 4), "3a4460")
    slot = c.rect((box[0] + 30, box[1] + 104, box[2] - 30, box[1] + 152), 14)
    c.lay(slot, "9ff0ff"); e.lay(slot, "58e6ff")
    save("trim", c.img)
    save("trim_emit", e.img)


TEXTURES = (plate_a, plate_b, paving, steel, engine, shaft, glass, light, glow, pad, trim)


def sheet(version):
    names = sorted(p.name for p in OUT.glob("arena_hull_*.png"))
    cell, gap, cols = 320, 14, 6
    rows = (len(names) + cols - 1) // cols
    out = Image.new("RGB", (gap + cols * (cell + gap), gap + rows * (cell + gap + 22)), (60, 62, 70))
    draw = ImageDraw.Draw(out)
    for k, name in enumerate(names):
        tile = Image.open(OUT / name).convert("RGB").resize((cell, cell), Image.LANCZOS)
        r, col = divmod(k, cols)
        x, y = gap + col * (cell + gap), gap + r * (cell + gap + 22)
        out.paste(tile, (x, y))
        draw.text((x, y + cell + 4), name, fill=(235, 235, 235))
    SHEETS.mkdir(parents=True, exist_ok=True)
    out.save(SHEETS / f"swatches_{version}.png")
    print("[arena-hull-tex] sheet", SHEETS / f"swatches_{version}.png")


def main():
    for paint in TEXTURES:
        paint()
    version = "v1"
    for a in sys.argv:
        if a.startswith("--sheet="):
            version = a.split("=", 1)[1]
    sheet(version)


if __name__ == "__main__":
    main()
