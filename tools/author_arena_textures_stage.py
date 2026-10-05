"""Paint the arena STAGE kit's textures, in the house illustrated style (docs/ARENA_ART_BRIEF.md).

  py -3 tools/author_arena_textures_stage.py [--sheet vN]

Writes Assets/TumbangPreso/Art/Arena/Textures/arena_stage_<surface>.png (and _emit.png where the
surface glows) and, with --sheet, a swatch sheet Logs/arena/stage/textures_<vN>.png. The models are
tools/author_arena_stage.py, which reads these files; material names are arena_stage_<surface>.

THE STYLE (KANTO_DESIGN_GUIDE.md section 3): FLAT fills, a few LARGE patches with FEATHERED organic
edges, low contrast between a thing and its joints, no grain, no streaks, no cracks. Lines are
warped by a smooth field so they read as a hand's. Every surface has its own drawing below.

TILING SURFACES (world-scale UVs written by the model; the tile's size in metres is TILE_M):
  deck    8 m   the walking surface: PALE hexagonal panels, three near-white tones, soft seams.
                The stage is the brightest, cleanest thing in the stadium, so this stays quiet:
                nothing on it is darker than a light grey, nothing is saturated.
  line    4 m   the painted white edge line.
  rim     4 m   the lit edge strip (the chamfer): ice white. Its emission map is what glows.
  hull    4 m   the dark metal of the sides and the skirt: deep navy charcoal, two soft coats and
                a few wide panel joints.
  under   4 m   the underside's structure: dark ribs on a one metre grid with a glowing hover cell
                in every bay. The emission map carries the cells only.
  holo    8 m   the HOLOGRAM of the next layout: the deck's own hexagons as bright cyan lines on
                a nearly clear fill (RGBA). It shares the deck's UVs, so a line lands on a seam.
ONE-OFF ARTWORK (UV 0..1):
  mark    the can's mark, 3 m across, cut into the centre of the drum: a gold spot ring where the
          can stands, a wider ring, four ticks.
  props   one 1024 atlas for the jump pad, the speed pad, the stamina pickup and the drone.
          REGIONS below; tools/author_arena_stage.py repeats the numbers.
  beam, beamcore   the drone's tractor beam, an outer and an inner cone: white drawings in the alpha
          (zigzag bands and halftone; a barber's pole), scrolled and coloured by the game. See beam().
  spot    the drone's landing mark, 2 m across, painted on the deck (RGBA, blended, not added).

ROLE HUES: nothing here is near offence orange #f87020 or defence blue #0080e8. The deck is a
cool near-white; the dark metal is navy charcoal; the lights are ice white and pale cyan; the
three gameplay accents are TEAL (jump), LIME (speed) and VIOLET (stamina), one each; the mark and
the drone's lamps are gold. The drone (v6, SAGIP) adds a warm shell white and a rescue crimson.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
from author_ilalim_textures_props import hexcol, smooth, patches, coat, fill      # noqa: E402  (read-only: the brushes)

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "TumbangPreso" / "Art" / "Arena" / "Textures"
SHEETS = ROOT / "Logs" / "arena" / "stage"
S = 1024
TILE_M = {"deck": 8.0, "line": 4.0, "rim": 4.0, "hull": 4.0, "under": 4.0, "holo": 8.0}

TEAL, LIME, VIOLET = "3df2c4", "7df032", "a678ff"
ICE, GOLD, NAVY = "c4f3ff", "ffcf4a", "141a2e"
# The drone's own three: a warm shell (the deck is a COOL white, so it stands off it), the kit's gold,
# and a rescue crimson that is well on the pink side of offence orange #f87020.
SHELL, CRIMSON, SCREEN, EYE = "f6f0e2", "e3264f", "0d1530", "7ff3ff"
FONTS = Path("C:/Windows/Fonts")

# The props atlas, image pixels, top-left origin: (x0, y0, x1, y1).
REGIONS = {
    "jump_cushion": (0, 0, 256, 256),          # top down, 0.66 m from the centre to the region's edge
    "jump_base": (256, 0, 512, 256),           # top down, 0.94 m
    "speed_top": (512, 0, 768, 512),           # top down, 0.80 m across by 1.60 m along (half sizes)
    "metal_dark": (768, 0, 1024, 256),         # painted dark metal, 1 m across
    "metal_light": (768, 256, 1024, 512),      # painted pale shell, 1 m across
    "pickup_cell": (0, 256, 256, 512),         # the cell's skin: u round it, v up it
    "drone_top": (256, 256, 512, 512),         # the drone's fan from above, 0.84 m
    # the drone (v6): its skin (u round it from the back, v up it), the lifebuoy (u round it, v round
    # the tube), its underside from below (0.46 m), the nameplate, and its four faces
    "drone_skin": (0, 640, 512, 832), "drone_buoy": (0, 832, 512, 896), "drone_under": (512, 640, 768, 896),
    "drone_plate": (768, 640, 1024, 704), "sw_crimson": (768, 704, 896, 832), "sw_shell": (896, 704, 1024, 832), "sw_brass": (768, 832, 896, 896),
    "drone_face_search": (0, 896, 256, 1024), "drone_face_lock": (256, 896, 512, 1024),
    "drone_face_carry": (512, 896, 768, 1024), "drone_face_proud": (768, 896, 1024, 1024),
    "sw_teal": (0, 512, 128, 640), "sw_lime": (128, 512, 256, 640), "sw_violet": (256, 512, 384, 640),
    "sw_ice": (384, 512, 512, 640), "sw_gold": (512, 512, 640, 640), "sw_glass": (640, 512, 768, 640),
    "sw_cream": (768, 512, 896, 640), "sw_black": (896, 512, 1024, 640),
}


def save(name, img, alpha=None):
    OUT.mkdir(parents=True, exist_ok=True)
    a = np.clip(img, 0, 1)
    if alpha is not None:
        a = np.dstack([a, np.clip(alpha, 0, 1)])
    Image.fromarray((a * 255 + 0.5).astype(np.uint8)).save(OUT / ("arena_stage_%s.png" % name))
    return a


def warp(h, w, px, seed):
    """Coordinates pushed about by a smooth periodic field, so a drawn line wobbles like a hand's."""
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    return xx + smooth(h, w, 90, seed) * px, yy + smooth(h, w, 90, seed + 1) * px


# ---------------------------------------------------------------- the hexagons the deck and the hologram share
def hexes(seed, wobble=1.6):
    """(edge distance in metres, cell id) for flat-top hexagons of circumradius 2/3 m on the 8 m tile:
    8 columns 1 m apart and 7 rows (7 x 1.1547 = 8.08 m, squeezed 1 percent to tile)."""
    xx, yy = warp(S, S, wobble, seed)
    a = 2.0 / 3.0
    x = xx / S * 8.0
    y = yy / S * 7.0 * (3 ** 0.5) * a
    best_d, best_id = np.full((S, S), 1e9), np.zeros((S, S), int)
    for off, (ox, oy) in enumerate(((0.0, 0.0), (1.5 * a, 3 ** 0.5 / 2 * a))):
        px_, py_ = 3 * a, 3 ** 0.5 * a
        cx = np.round((x - ox) / px_) * px_ + ox
        cy = np.round((y - oy) / py_) * py_ + oy
        dx, dy = np.abs(x - cx), np.abs(y - cy)
        d = np.maximum(dy, dx * (3 ** 0.5) / 2 + dy / 2)            # the hex metric: 0 at the centre, a * 0.866 at an edge
        ident = (np.round((x - ox) / px_).astype(int) % 4) * 14 + (np.round((y - oy) / py_).astype(int) % 7) * 2 + off
        take = d < best_d
        best_id = np.where(take, ident, best_id)
        best_d = np.where(take, d, best_d)
    return a * (3 ** 0.5) / 2 - best_d, best_id


def deck():
    edge, ident = hexes(11)
    tones = [hexcol(c) for c in ("e3e8ef", "d8dfe9", "eaeef3", "dde4ed")]
    rng = np.random.default_rng(5)
    pick = rng.integers(0, len(tones), 64)
    img = np.zeros((S, S, 3))
    for k in range(56):
        img[ident == k] = tones[pick[k]]
    img = coat(img, (0.975, 0.98, 0.99), 190, 0.28, 21, feather=0.9)            # a few large cool patches
    img = coat(img, (1.015, 1.012, 1.005), 170, 0.20, 22, feather=0.9)          # and a few lighter ones
    seam = np.clip(1 - edge / 0.022, 0, 1)                                      # a 2 cm joint, feathered
    seam = ndimage.gaussian_filter(seam, 0.8, mode="wrap")
    img = img * (1 - seam[..., None] * 0.62) + hexcol("a9b6c9") * seam[..., None] * 0.62
    lip = np.clip(1 - np.abs(edge - 0.07) / 0.03, 0, 1) * 0.10                  # one flat highlight band inside each panel
    img = img + lip[..., None] * (hexcol("ffffff") - img)
    a = save("deck", img)
    save("deck_emit", img * 0.30)                                               # a low self light: night does not drown the deck
    return a


def holo():
    edge, _ = hexes(11)
    line = np.clip(1 - edge / 0.05, 0, 1)
    line = ndimage.gaussian_filter(line, 1.0, mode="wrap")
    glow = np.clip(1 - edge / 0.16, 0, 1) ** 2 * 0.35
    alpha = np.clip(0.035 + line * 0.95 + glow * 0.45, 0, 1)
    col = np.ones((S, S, 3)) * hexcol("54e6ff")
    col = col + line[..., None] * (hexcol("e8fdff") - col)
    return save("holo", col, alpha)


def line():
    img = fill(S, S, "f2f5f9")
    img = coat(img, (0.975, 0.98, 0.99), 160, 0.3, 31, feather=0.9)
    a = save("line", img)
    save("line_emit", img * 0.35)
    return a


def rim():
    img = fill(S, S, ICE)
    img = coat(img, (0.93, 0.98, 1.0), 160, 0.35, 41, feather=0.8)
    a = save("rim", img)
    save("rim_emit", img * np.array([0.55, 0.93, 1.0]))
    return a


def hull():
    img = fill(S, S, NAVY)
    img = coat(img, (1.35, 1.32, 1.28), 170, 0.34, 51, feather=0.6)             # a lighter worn coat
    img = coat(img, (0.78, 0.80, 0.86), 130, 0.22, 52, feather=0.6)             # a deeper one
    xx, yy = warp(S, S, 3.0, 53)
    for coord, period in ((xx, S / 2.0), (yy, S / 4.0)):                        # joints: 2 m one way, 1 m the other
        d = np.abs((coord % period) - period / 2)
        j = np.clip(1 - np.abs(d - period / 2) / 2.2, 0, 1)
        img = img * (1 - j[..., None] * 0.45) + hexcol("262f4d") * j[..., None] * 0.45
    return save("hull", img)


def under():
    """The model lays this out ALONG the plate (u round a ring or across a ramp, v outward), so the
    ribs run round and across the plate, not on a square grid under a round thing."""
    xx, yy = warp(S, S, 1.5, 61)
    cell = S / 4.0                                                              # one bay a metre
    dx = np.abs((xx % cell) - cell / 2) / cell
    dy = np.abs((yy % cell) - cell / 2) / cell
    box = np.maximum(dx, dy)                                                    # 0 at a bay's centre, 0.5 on a rib
    img = fill(S, S, "0f1426")
    img = coat(img, (1.35, 1.3, 1.25), 150, 0.3, 62, feather=0.6)
    plate = np.clip((0.33 - np.hypot(np.maximum(dx - 0.10, 0), np.maximum(dy - 0.10, 0)) - 0.10) / 0.02, 0, 1)   # a rounded access plate in each bay
    img = img * (1 - plate[..., None] * 0.7) + hexcol("1b2340") * plate[..., None] * 0.7
    rib = np.clip((box - 0.42) / 0.02, 0, 1)                                    # the ribs: a lighter steel, 16 cm wide
    img = img * (1 - rib[..., None]) + hexcol("2b3557") * rib[..., None]
    seam = np.clip((box - 0.487) / 0.006, 0, 1)                                 # a thin light channel down each rib
    img = img * (1 - seam[..., None]) + hexcol("7fe3f5") * seam[..., None]
    a = save("under", img)
    save("under_emit", hexcol("3fc4e6") * seam[..., None])
    return a


def ring_mask(r, r0, r1, feather):
    return np.clip((r - r0) / feather + 0.5, 0, 1) * np.clip((r1 - r) / feather + 0.5, 0, 1)


def mark():
    """3 m across. The disc's own edge is at 1.5 m; the drawing keeps inside 1.44 m."""
    xx, yy = warp(S, S, 0.9, 71)
    m = 3.0 / S
    x, y = (xx - S / 2) * m, (yy - S / 2) * m
    r = np.hypot(x, y)
    ang = np.degrees(np.arctan2(x, y)) % 360
    img = fill(S, S, "e3e8ef")
    img = coat(img, (0.955, 0.965, 0.985), 150, 0.30, 72, feather=0.7)
    f = 0.008
    navy = ring_mask(r, 1.30, 1.44, f)                                          # the dark band that frames the cut
    img = img * (1 - navy[..., None]) + hexcol("1a2138") * navy[..., None]
    plate = ring_mask(r, -1, 0.62, f)                                           # the can's plate
    img = img * (1 - plate[..., None]) + hexcol("20283f") * plate[..., None]
    gold = np.maximum(ring_mask(r, 0.50, 0.57, f), ring_mask(r, 1.345, 1.395, f))
    gold = np.maximum(gold, ring_mask(r, 0.19, 0.215, f))                       # the can's own footprint
    tick = (np.abs(((ang + 45) % 90) - 45) < np.degrees(0.035 / np.maximum(r, 0.05))) * ring_mask(r, 0.70, 1.22, f)
    gold = np.maximum(gold, tick)
    dots = np.zeros_like(r)
    for k in range(12):                                                         # twelve studs round the wide ring
        a = np.radians(k * 30 + 15)
        dots = np.maximum(dots, np.clip((0.045 - np.hypot(x - 0.96 * np.sin(a), y - 0.96 * np.cos(a))) / f + 0.5, 0, 1))
    img = img * (1 - dots[..., None]) + hexcol("a9b6c9") * dots[..., None]
    img = img * (1 - gold[..., None]) + hexcol(GOLD) * gold[..., None]
    a = save("mark", img)
    emit = img * 0.16 * (1 - np.clip(navy + plate, 0, 1))[..., None] + hexcol(GOLD) * gold[..., None] * 0.9
    save("mark_emit", emit)
    return a


# ---------------------------------------------------------------- the props atlas
def region_xy(name, half_x, half_y=None, wobble=1.2, seed=1):
    """Metre coordinates over a region, its centre at (0, 0), +y up the image."""
    x0, y0, x1, y1 = REGIONS[name]
    h, w = y1 - y0, x1 - x0
    xx, yy = warp(h, w, wobble, seed)
    half_y = half_x * h / w if half_y is None else half_y
    return (xx - w / 2) / (w / 2) * half_x, -(yy - h / 2) / (h / 2) * half_y, h, w


def chevron(x, y, tip_y, half_w, arm, thick):
    """A '^' pointing +y: distance field of two arms falling `arm` metres from the tip at +-half_w."""
    slope = arm / half_w
    d = np.abs((y - (tip_y - np.abs(x) * slope))) / np.sqrt(1 + slope * slope)
    return np.clip((thick / 2 - d) / 0.012 + 0.5, 0, 1) * (np.abs(x) < half_w)


# ---------------------------------------------------------------- the rescue drone, SAGIP (v6)
def inked(size, draw, scale=4):
    """A drawing made with PIL at `scale` times the size and brought down: clean hard edges with
    one pixel of feather, as the rest of the atlas has. `draw(d, w, h)` paints on an RGB canvas."""
    from PIL import ImageDraw
    w, h = size
    im = Image.new("RGB", (w * scale, h * scale))
    draw(ImageDraw.Draw(im), w * scale, h * scale)
    return np.asarray(im.resize((w, h), Image.LANCZOS)).astype(float) / 255.0


def rgb(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def font(names, size):
    from PIL import ImageFont
    for n in names:
        if (FONTS / n).exists():
            return ImageFont.truetype(str(FONTS / n), size)
    return ImageFont.load_default()


def drone_art(put):
    """SAGIP ("rescue"): a chubby rescue bot in a lifebuoy with a ceiling fan on its head.
    Every region here is drawn for ONE place on the model (tools/author_arena_stage.py, props)."""
    # ---- the skin. x is the way round from the NOSE (-1 and +1 meet at the back). y is the way DOWN
    # the shell's outline by its own length (even texels on the dome): +1 at the top's middle, 0.65
    # at 16 cm from the axis, 0.39..-0.02 behind the brow plate, -0.07..-0.62 behind the screen,
    # the ear lamps at -0.12, and the buoy hides everything under -0.64. The flank is -0.6..0.1.
    x, y, h, w = region_xy("drone_skin", 1.0, 1.0, wobble=1.0, seed=131)
    img = np.ones((h, w, 3)) * hexcol(SHELL)
    img = coat(img, (0.94, 0.94, 0.955), 70, 0.30, 132, feather=0.7)                 # two large cool patches
    img = coat(img, (1.03, 1.02, 1.0), 60, 0.22, 133, feather=0.7)
    ax = np.abs(x)
    MX, MY = 1.38, 0.452                                                               # metres to a unit of x (at the waist) and of y

    def lay(mask, colour, strength=1.0):
        nonlocal img
        img = img * (1 - mask[..., None] * strength) + hexcol(colour) * mask[..., None] * strength

    def within(v, lo, hi, f=0.02):
        return np.clip((v - lo) / f + 0.5, 0, 1) * np.clip((hi - v) / f + 0.5, 0, 1)
    lay(np.clip((y - 0.44) / 0.03 + 0.5, 0, 1), GOLD)                                  # the gold crown the fan stands on
    lay(within(y, 0.33, 0.385, 0.012), CRIMSON)                                        # and its pinstripe
    # the screen behind the face: a rounded box across the front (the face meshes lie over it)
    bx, by = ax / 0.205, np.abs(y + 0.345) / 0.31
    box = np.maximum(bx, by) + 0.35 * np.minimum(bx, by) ** 3
    lay(np.clip((1.10 - box) / 0.04 + 0.5, 0, 1), GOLD)
    lay(np.clip((1.02 - box) / 0.04 + 0.5, 0, 1), SCREEN)
    # jeepney paint down each side, from the face round to the back quarter: a fat crimson sweep
    # that swells and tapers, a gold line riding under it, and three gold stars over it
    side = within(ax, 0.25, 0.64)
    t = np.clip((ax - 0.25) / 0.39, 0, 1)
    mid = -0.13 - 0.07 * np.sin(t * np.pi)
    fat = 0.035 + 0.075 * np.sin(np.clip(t * 1.25, 0, 1) * np.pi) ** 0.8
    lay(np.clip((fat - np.abs(y - mid)) / 0.02 + 0.5, 0, 1) * side, CRIMSON)
    lay(np.clip((0.022 - np.abs(y - mid + fat + 0.065)) / 0.014 + 0.5, 0, 1) * side, GOLD)
    for sx in (-1, 1):
        for k, at in enumerate((0.33, 0.45, 0.57)):
            dx, dy = (x - sx * at) * MX / 0.062, (y - (0.17 - 0.02 * k)) * MY / 0.062
            ang = np.arctan2(dx, dy)
            star = np.hypot(dx, dy) * (1.0 + 0.40 * np.cos(5 * ang))                   # a soft five-pointed star
            lay(np.clip((0.86 - star) / 0.10 + 0.5, 0, 1), GOLD)
    # the rescue roundel on each back quarter: a crimson cross on a white disc in a gold ring
    for sx in (-1, 1):
        dx, dy = (x - sx * 0.80) * MX / 0.125, (y + 0.17) * MY / 0.125
        r = np.hypot(dx, dy)
        lay(np.clip((1.0 - r) / 0.07 + 0.5, 0, 1), GOLD)
        lay(np.clip((0.84 - r) / 0.07 + 0.5, 0, 1), "fffaf0")
        cross = np.maximum(within(np.abs(dx), -1, 0.20, 0.05) * within(np.abs(dy), -1, 0.60, 0.05),
                           within(np.abs(dy), -1, 0.20, 0.05) * within(np.abs(dx), -1, 0.60, 0.05))
        lay(cross, CRIMSON)
    seam = within(y, -0.565, -0.535, 0.012) * np.clip((ax - 0.25) / 0.02, 0, 1)
    lay(seam, "b9b3a6", 0.55)                                                          # the shell's one joint, just over the buoy
    glow = hexcol(GOLD) * np.clip((y - 0.44) / 0.03 + 0.5, 0, 1)[..., None] * 0.35
    put("drone_skin", img, img * 0.16 + glow)

    # ---- the lifebuoy (salbabida): eight parts, crimson and white, a gold band at each join
    x, y, h, w = region_xy("drone_buoy", 1.0, 1.0, wobble=0.8, seed=135)
    part = (x + 1.0) * 4.0                                                             # 0..8 round it
    red = np.clip((0.5 - np.abs((part % 2.0) - 0.5)) / 0.012 + 0.5, 0, 1)              # every other part
    img = np.ones((h, w, 3)) * hexcol("fbf6ea")
    img = coat(img, (0.95, 0.95, 0.96), 40, 0.3, 136, feather=0.7)
    img = img * (1 - red[..., None]) + hexcol(CRIMSON) * red[..., None]
    img = coat(img, (1.10, 1.06, 1.04), 50, 0.25, 137, feather=0.7)
    join = np.abs(((part + 0.5) % 1.0) - 0.5)                                          # 0 at each join
    band = np.clip((0.035 - join) / 0.008 + 0.5, 0, 1)
    img = img * (1 - band[..., None]) + hexcol(GOLD) * band[..., None]
    put("drone_buoy", img, img * 0.42)

    # ---- the underside, from below, 0.46 m to the region's edge: the lens, eight bulbs, a sunburst
    x, y, h, w = region_xy("drone_under", 0.46, seed=139)
    r = np.hypot(x, y)
    ang = np.degrees(np.arctan2(x, y)) % 360
    f = 0.006
    img = np.ones((h, w, 3)) * hexcol("fbf6ea")
    ray = np.clip((np.abs(((ang + 7.5) % 30.0) - 15.0) - 7.5) / 0.9 + 0.5, 0, 1)        # twelve crimson rays
    img = img * (1 - ray[..., None]) + hexcol(CRIMSON) * ray[..., None]
    plate = ring_mask(r, -1, 0.315, f)
    img = img * (1 - plate[..., None]) + hexcol("1b2238") * plate[..., None]
    gold = np.maximum(ring_mask(r, 0.295, 0.325, f), ring_mask(r, 0.185, 0.21, f))
    bulbs = np.zeros_like(r)
    for k in range(8):
        a = np.radians(k * 45 + 22.5)
        bulbs = np.maximum(bulbs, np.clip((0.034 - np.hypot(x - 0.252 * np.sin(a), y - 0.252 * np.cos(a))) / f + 0.5, 0, 1))
    img = img * (1 - gold[..., None]) + hexcol(GOLD) * gold[..., None]
    img = img * (1 - bulbs[..., None]) + hexcol("fff1b0") * bulbs[..., None]
    lens = ring_mask(r, -1, 0.185, f)
    lens_col = hexcol("8feeff") + np.clip(1 - r / 0.17, 0, 1)[..., None] ** 1.5 * (hexcol("f4feff") - hexcol("8feeff"))
    spoke = np.clip((2.2 - np.abs(((ang + 30) % 60.0) - 30.0) * np.maximum(r, 0.02) / 0.10) / 1.0, 0, 1) * ring_mask(r, 0.06, 0.17, f)
    lens_col = lens_col * (1 - spoke[..., None] * 0.45) + hexcol("2aa7c9") * spoke[..., None] * 0.45
    img = img * (1 - lens[..., None]) + lens_col * lens[..., None]
    put("drone_under", img, img * 0.30 * (1 - np.clip(lens + bulbs, 0, 1))[..., None] + lens_col * lens[..., None] * 0.95 + hexcol("ffe27a") * bulbs[..., None] * 0.95)

    # ---- the fan from above: what a spinning toy propeller shows, rings. 0.84 m to the region's edge
    x, y, h, w = region_xy("drone_top", 0.84, seed=141)
    r = np.hypot(x, y)
    img = np.ones((h, w, 3)) * hexcol("fbf6ea")
    img = coat(img, (0.95, 0.95, 0.96), 60, 0.3, 142, feather=0.7)
    for r0, r1, colour in ((-1, 0.17, GOLD), (0.58, 0.70, CRIMSON), (0.70, 0.745, GOLD)):
        m = ring_mask(r, r0, r1, 0.008)
        img = img * (1 - m[..., None]) + hexcol(colour) * m[..., None]
    cap = ring_mask(r, -1, 0.07, 0.008)
    img = img * (1 - cap[..., None]) + hexcol(CRIMSON) * cap[..., None]
    put("drone_top", img, img * 0.36)

    # ---- the nameplate: SAGIP in jeepney sign paint, crimson on gold, a star at each end
    x0, y0, x1, y1 = REGIONS["drone_plate"]

    def plate_art(d, W, H):
        d.rectangle((0, 0, W, H), fill=rgb(CRIMSON))
        d.rounded_rectangle((W * 0.02, H * 0.10, W * 0.98, H * 0.90), radius=H * 0.22, fill=rgb(GOLD))
        fnt = font(("impact.ttf", "ariblk.ttf", "arialbd.ttf"), int(H * 0.74))
        box = d.textbbox((0, 0), "SAGIP", font=fnt)
        tx, ty = (W - (box[2] - box[0])) / 2 - box[0], (H - (box[3] - box[1])) / 2 - box[1]
        d.text((tx + H * 0.045, ty + H * 0.05), "SAGIP", font=fnt, fill=rgb("8a1230"))        # its drop shadow
        d.text((tx, ty), "SAGIP", font=fnt, fill=rgb(CRIMSON), stroke_width=int(H * 0.035), stroke_fill=rgb("fffaf0"))
        for cx in (W * 0.115, W * 0.885):
            pts = []
            for k in range(10):
                rr = H * (0.26 if k % 2 == 0 else 0.11)
                pts.append((cx + rr * np.sin(k * np.pi / 5), H * 0.5 - rr * np.cos(k * np.pi / 5)))
            d.polygon(pts, fill=rgb(CRIMSON))
    img = inked((x1 - x0, y1 - y0), plate_art)
    put("drone_plate", img, img * 0.5)

    # ---- the four faces, each the whole screen: 256 by 128, drawn in light on the dark glass
    def face(name, art):
        x0, y0, x1, y1 = REGIONS[name]

        def whole(d, W, H):
            d.rectangle((0, 0, W, H), fill=rgb(SCREEN))
            d.rounded_rectangle((W * 0.03, H * 0.06, W * 0.97, H * 0.94), radius=H * 0.2, fill=rgb("111c3f"))
            art(d, W, H)
        img = inked((x1 - x0, y1 - y0), whole)
        lit = np.clip((img.max(axis=2) - 0.36) / 0.2, 0, 1)[..., None]                 # what is drawn in light glows, the glass does not
        put(name, img, img * lit * 0.95 + img * 0.10)

    eye, gold, blush, white = rgb(EYE), rgb("ffe27a"), rgb("ff7fa3"), rgb("f4feff")

    def search(d, W, H):
        for cx in (0.30, 0.70):                                                        # two tall eyes looking DOWN for somebody
            d.rounded_rectangle((W * (cx - 0.095), H * 0.20, W * (cx + 0.095), H * 0.74), radius=W * 0.09, fill=eye)
            d.ellipse((W * (cx - 0.055), H * 0.46, W * (cx + 0.055), H * 0.70), fill=rgb(SCREEN))
            d.ellipse((W * (cx + 0.005), H * 0.50, W * (cx + 0.04), H * 0.59), fill=white)
        d.line((W * 0.455, H * 0.84, W * 0.545, H * 0.84), fill=eye, width=int(H * 0.05))

    def lock(d, W, H):
        for cx in (0.29, 0.71):                                                        # wide gold target eyes
            d.ellipse((W * (cx - 0.15), H * 0.12, W * (cx + 0.15), H * 0.72), fill=gold)
            d.ellipse((W * (cx - 0.105), H * 0.21, W * (cx + 0.105), H * 0.63), fill=rgb(SCREEN))
            d.ellipse((W * (cx - 0.045), H * 0.33, W * (cx + 0.045), H * 0.51), fill=gold)
        d.rounded_rectangle((W * 0.478, H * 0.16, W * 0.522, H * 0.56), radius=W * 0.02, fill=white)   # the "!"
        d.ellipse((W * 0.474, H * 0.64, W * 0.526, H * 0.745), fill=white)
        d.ellipse((W * 0.455, H * 0.80, W * 0.545, H * 0.93), outline=gold, width=int(H * 0.045))

    def carry(d, W, H):
        wd = int(H * 0.095)
        for cx, s in ((0.29, 1), (0.71, -1)):                                          # > < : it is heavy
            d.line((W * (cx - 0.10 * s), H * 0.20, W * (cx + 0.09 * s), H * 0.44, W * (cx - 0.10 * s), H * 0.68), fill=eye, width=wd, joint="curve")
        pts = [(W * (0.40 + 0.04 * k), H * (0.80 if k % 2 == 0 else 0.90)) for k in range(6)]
        d.line(pts, fill=eye, width=int(H * 0.05), joint="curve")                      # gritted teeth
        d.polygon(((W * 0.90, H * 0.14), (W * 0.865, H * 0.34), (W * 0.935, H * 0.34)), fill=white)   # a bead of sweat
        d.ellipse((W * 0.862, H * 0.26, W * 0.938, H * 0.44), fill=white)

    def proud(d, W, H):
        wd = int(H * 0.10)
        for cx in (0.29, 0.71):                                                        # ^ ^ : the balloon's own smile
            d.arc((W * (cx - 0.12), H * 0.20, W * (cx + 0.12), H * 0.82), 200, 340, fill=eye, width=wd)
        for cx in (0.13, 0.87):
            d.ellipse((W * (cx - 0.065), H * 0.52, W * (cx + 0.065), H * 0.70), fill=blush)
        d.arc((W * 0.42, H * 0.46, W * 0.58, H * 0.86), 20, 160, fill=eye, width=int(H * 0.07))
    for name, art in (("drone_face_search", search), ("drone_face_lock", lock), ("drone_face_carry", carry), ("drone_face_proud", proud)):
        face(name, art)


def props():
    alb = np.zeros((S, S, 3)); alb[:] = hexcol(NAVY)
    emi = np.zeros((S, S, 3))

    def put(name, img, emit=None):
        x0, y0, x1, y1 = REGIONS[name]
        alb[y0:y1, x0:x1] = img
        if emit is not None:
            emi[y0:y1, x0:x1] = emit

    # the jump pad's cushion: a teal dome, lighter at the crown, with a cream double chevron
    x, y, h, w = region_xy("jump_cushion", 0.66, seed=3)
    r = np.hypot(x, y)
    img = np.ones((h, w, 3)) * hexcol("27c9a4")
    crown = np.clip(1 - r / 0.5, 0, 1) ** 1.5
    img = img + crown[..., None] * (hexcol(TEAL) - img)
    img = coat(img, (1.10, 1.06, 1.04), 60, 0.3, 81, feather=0.7)
    edge = ring_mask(r, 0.50, 0.60, 0.01)
    img = img * (1 - edge[..., None] * 0.5) + hexcol("0f6f5c") * edge[..., None] * 0.5
    ch = np.maximum(chevron(x, y, 0.26, 0.30, 0.20, 0.085), chevron(x, y, 0.04, 0.30, 0.20, 0.085))
    img = img * (1 - ch[..., None]) + hexcol("f4fff2") * ch[..., None]
    put("jump_cushion", img, img * 0.75)

    # the jump pad's base: a dark round tray with a white ring where the rising rings start
    x, y, h, w = region_xy("jump_base", 0.94, seed=5)
    r = np.hypot(x, y)
    img = np.ones((h, w, 3)) * hexcol("1b2238")
    img = coat(img, (1.3, 1.28, 1.25), 60, 0.35, 82, feather=0.6)
    band = ring_mask(r, 0.605, 0.695, 0.008)
    img = img * (1 - band[..., None]) + hexcol("f2f5f9") * band[..., None]
    lip = ring_mask(r, 0.78, 0.86, 0.01)
    img = img * (1 - lip[..., None] * 0.7) + hexcol("3a4670") * lip[..., None] * 0.7
    notch = np.zeros_like(r)
    ang = np.degrees(np.arctan2(x, y)) % 360
    for k in range(8):
        notch = np.maximum(notch, (np.abs(((ang - k * 45 + 180) % 360) - 180) < 5) * ring_mask(r, 0.72, 0.77, 0.008))
    img = img * (1 - notch[..., None]) + hexcol(TEAL) * notch[..., None]
    put("jump_base", img, hexcol("f2f5f9") * band[..., None] * 0.35 + hexcol(TEAL) * notch[..., None] * 0.9)

    # the speed pad's top: a dark plate, a lime frame line, a faint arrow field under the raised chevrons
    x, y, h, w = region_xy("speed_top", 0.80, 1.60, seed=7)
    img = np.ones((h, w, 3)) * hexcol("161d31")
    img = coat(img, (1.3, 1.3, 1.25), 70, 0.35, 83, feather=0.6)
    box = np.maximum(np.abs(x) / 0.66, np.abs(y) / 1.41)
    frame = np.clip(1 - np.abs(box - 1.0) / 0.045, 0, 1)
    img = img * (1 - frame[..., None]) + hexcol(LIME) * frame[..., None]
    field = np.zeros_like(x)
    for k in range(3):
        field = np.maximum(field, chevron(x, y, -0.62 + k * 0.80, 0.50, 0.42, 0.26))
    img = img * (1 - field[..., None] * 0.55) + hexcol("4d6a12") * field[..., None] * 0.55
    put("speed_top", img, hexcol(LIME) * (frame * 0.8 + field * 0.22)[..., None])

    # painted metal, dark and pale
    for name, base, joint, seed in (("metal_dark", "1b2238", "2c3656", 91), ("metal_light", "d7dde8", "aab4c6", 95)):
        x, y, h, w = region_xy(name, 0.5, seed=seed)
        img = np.ones((h, w, 3)) * hexcol(base)
        img = coat(img, (1.22, 1.2, 1.18) if name == "metal_dark" else (0.93, 0.94, 0.97), 60, 0.35, seed + 1, feather=0.6)
        j = np.clip(1 - np.abs(np.abs(y) - 0.25) / 0.012, 0, 1)
        img = img * (1 - j[..., None] * 0.5) + hexcol(joint) * j[..., None] * 0.5
        put(name, img, img * 0.10 if name == "metal_light" else None)

    # the stamina cell's skin: violet, a pale band round its waist, lighter toward the top
    x, y, h, w = region_xy("pickup_cell", 0.5, seed=9)
    img = np.ones((h, w, 3)) * hexcol("7a48ee")
    up = np.clip((y + 0.5), 0, 1)[..., None]
    img = img + up * (hexcol("c9a8ff") - img) * 0.7
    img = coat(img, (1.12, 1.05, 1.0), 50, 0.3, 97, feather=0.7)
    put("pickup_cell", img, img * 0.7)

    drone_art(put)

    for name, colour, glow in (("sw_teal", TEAL, 1.0), ("sw_lime", LIME, 1.0), ("sw_violet", VIOLET, 1.0), ("sw_ice", ICE, 1.0),
                               ("sw_gold", GOLD, 1.0), ("sw_glass", "0b1020", 0.0), ("sw_cream", "f4fff2", 0.8), ("sw_black", "0a0d18", 0.0),
                               ("sw_crimson", CRIMSON, 0.55), ("sw_shell", SHELL, 0.16), ("sw_brass", "f2b62e", 0.22)):
        x0, y0, x1, y1 = REGIONS[name]
        img = np.ones((y1 - y0, x1 - x0, 3)) * hexcol(colour)
        img = coat(img, (0.94, 0.96, 0.98), 40, 0.3, 120 + x0, feather=0.8)
        put(name, img, img * glow if glow else None)
    a = save("props", alb)
    save("props_emit", emi)
    return a


def beam():
    """The tractor beam, two sheets of light (owner, 2026-10-05: "ufo effect needs to be more
    stylized"; before this it was one soft fade). Both are WHITE with the drawing in the alpha, tile
    both ways and are scrolled UP the cone by the game (Runtime/Map/ArenaDrone.cs), which also gives
    them their colour, so nothing here fades along the beam.
      beam      256 x 512, the OUTER cone, six times round it: a faint flat fill, two bold zigzag
                bands with a thin echo over each (a crown of points travelling up), and a halftone
                of dots that swell toward each band.
      beamcore  256 x 256, the INNER cone, four times round it: hard diagonal stripes (a barber's
                pole once it scrolls) with a row of chunky sparkles."""
    h, w = 512, 256
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    u, v = xx / w, 1.0 - yy / h                                                 # v up the beam, as the model's UVs
    tooth = np.abs(u - 0.5) * 2.0                                               # 1 at the tile's sides, 0 at its middle: a "^"
    alpha = np.full((h, w), 0.13)
    for k in range(2):
        d = ((v - (0.5 * k + 0.20) + tooth * 0.16 + 0.5) % 1.0) - 0.5           # the band's centre line, a zigzag
        alpha = np.maximum(alpha, np.clip((0.050 - np.abs(d)) / 0.004 + 0.5, 0, 1) * 0.92)
        alpha = np.maximum(alpha, np.clip((0.011 - np.abs(d - 0.085)) / 0.004 + 0.5, 0, 1) * 0.55)
    # the halftone: a staggered grid, 8 dots across the tile, each bigger the nearer the band above it
    cell = w / 8.0
    row = np.floor(yy / cell)
    gx = ((xx + (row % 2) * cell / 2) % cell) - cell / 2
    gy = (yy % cell) - cell / 2
    cv = 1.0 - (row + 0.5) * cell / h
    near = ((0.12 - cv) % 0.5) / 0.5                                            # 0 just under a band, 1 just over the one below
    size = np.clip(1.0 - near * 1.5, 0, 1) * 0.36 * cell
    dots = np.clip((size - np.hypot(gx, gy)) / 1.2 + 0.5, 0, 1) * (size > 1.0)
    alpha = np.maximum(alpha, dots * 0.50)
    col = np.ones((h, w, 3)) * hexcol("f2fdff")
    OUT.mkdir(parents=True, exist_ok=True)
    out = np.dstack([col, np.clip(alpha, 0, 1)])
    Image.fromarray((out * 255 + 0.5).astype(np.uint8)).save(OUT / "arena_stage_beam.png")

    n = 256
    yy, xx = np.mgrid[0:n, 0:n].astype(float)
    u, v = xx / n, 1.0 - yy / n
    stripe = ((u + v) * 2.0) % 1.0                                              # two diagonal stripes a tile
    a2 = 0.20 + 0.62 * np.clip((0.26 - np.abs(stripe - 0.5)) / 0.012 + 0.5, 0, 1)
    for cx, cy, r in ((0.25, 0.25, 0.085), (0.75, 0.75, 0.065)):                # a sparkle in each dark stripe
        dx, dy = np.abs(((u - cx + 0.5) % 1.0) - 0.5), np.abs(((v - cy + 0.5) % 1.0) - 0.5)
        spark = np.clip((r - (dx + dy + 2.2 * np.sqrt(dx * dy + 1e-9))) / 0.006 + 0.5, 0, 1)
        a2 = np.maximum(a2, spark)
    core = np.dstack([np.ones((n, n, 3)) * hexcol("ffffff"), np.clip(a2, 0, 1)])
    Image.fromarray((core * 255 + 0.5).astype(np.uint8)).save(OUT / "arena_stage_beamcore.png")
    return out


def spot():
    """The drone's LANDING MARK, 2 m across, laid on the deck where it will set a body down and
    turned by the game: PAINT, not light (the deck is near white, and light added to white is
    only more white). The drone's own lifebuoy as a ring, a dashed gold ring inside it, four
    crimson darts pointing at the spot, a gold cross where the feet go. RGBA; nothing outside
    the drawing."""
    n = 512
    xx, yy = warp(n, n, 1.6, 171)
    x, y = (xx - n / 2) / (n / 2), -(yy - n / 2) / (n / 2)
    r = np.hypot(x, y)
    ang = np.degrees(np.arctan2(x, y)) % 360
    f = 0.008
    img = np.ones((n, n, 3)) * hexcol("fbf6ea")
    alpha = np.zeros((n, n))

    def lay(mask, colour, a=1.0):
        nonlocal img, alpha
        img = img * (1 - mask[..., None]) + hexcol(colour) * mask[..., None]
        alpha = np.maximum(alpha, mask * a)
    buoy = ring_mask(r, 0.76, 0.96, f)
    lay(buoy, "fbf6ea")
    part = (ang / 45.0) % 2.0
    lay(buoy * np.clip((0.5 - np.abs(part - 0.5)) / 0.02 + 0.5, 0, 1), CRIMSON)
    lay(buoy * np.clip((0.045 - np.abs(((ang / 45.0 + 0.5) % 1.0) - 0.5)) / 0.012 + 0.5, 0, 1), GOLD)
    lay(np.maximum(ring_mask(r, 0.745, 0.765, f), ring_mask(r, 0.955, 0.975, f)), "8a1230")
    dash = ring_mask(r, 0.60, 0.655, f) * np.clip((np.abs(((ang / 22.5) % 1.0) - 0.5) - 0.17) / 0.03 + 0.5, 0, 1)
    lay(dash, GOLD)
    q = np.radians(((ang + 45.0) % 90.0) - 45.0)                                   # from the nearest of four axes
    dart = np.clip((0.34 * (r - 0.20) / 0.32 - np.abs(q) * r) / f + 0.5, 0, 1) * ring_mask(r, 0.20, 0.52, f)
    lay(dart, CRIMSON)
    cross = np.maximum((np.abs(x) < 0.025) * (np.abs(y) < 0.11), (np.abs(y) < 0.025) * (np.abs(x) < 0.11)).astype(float)
    lay(ndimage.gaussian_filter(cross, 1.0), GOLD)
    alpha = np.maximum(alpha * 0.96, ring_mask(r, -1, 0.75, f) * 0.10)                # a breath of white inside the ring
    OUT.mkdir(parents=True, exist_ok=True)
    out = np.dstack([np.clip(img, 0, 1), np.clip(alpha, 0, 1)])
    Image.fromarray((out * 255 + 0.5).astype(np.uint8)).save(OUT / "arena_stage_spot.png")
    return out


def main():
    made = {"deck": deck(), "line": line(), "rim": rim(), "hull": hull(), "under": under(), "mark": mark(),
            "props": props(), "holo": holo()}
    beam()
    spot()
    for a in sys.argv:
        if a.startswith("--sheet"):
            version = sys.argv[sys.argv.index(a) + 1] if a == "--sheet" else a.split("=", 1)[1]
            cells = []
            for k in ("deck", "hull", "under", "mark", "props", "holo", "line", "rim"):
                im = made[k]
                if im.shape[2] == 4:
                    im = im[..., :3] * im[..., 3:] + hexcol("05070f") * (1 - im[..., 3:])
                cells.append((im[::2, ::2, :3] * 255).astype(np.uint8))
            emits = []
            for k in ("deck", "under", "mark", "props"):
                emits.append(np.asarray(Image.open(OUT / ("arena_stage_%s_emit.png" % k)).convert("RGB"))[::2, ::2])
            sheet = np.vstack([np.hstack(cells[:4]), np.hstack(cells[4:]), np.hstack(emits)])
            SHEETS.mkdir(parents=True, exist_ok=True)
            Image.fromarray(sheet).save(SHEETS / ("textures_%s.png" % version))
            print("sheet", SHEETS / ("textures_%s.png" % version))
    print("STAGE_TEXTURES_OK", OUT)


if __name__ == "__main__":
    main()
