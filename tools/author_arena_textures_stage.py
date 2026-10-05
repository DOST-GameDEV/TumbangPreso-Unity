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
  beam    the drone's tractor beam: a soft vertical fade with two slow rings (RGBA).

ROLE HUES: nothing here is near offence orange #f87020 or defence blue #0080e8. The deck is a
cool near-white; the dark metal is navy charcoal; the lights are ice white and pale cyan; the
three gameplay accents are TEAL (jump), LIME (speed) and VIOLET (stamina), one each; the mark and
the drone's lamps are gold.
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

# The props atlas, image pixels, top-left origin: (x0, y0, x1, y1).
REGIONS = {
    "jump_cushion": (0, 0, 256, 256),          # top down, 0.66 m from the centre to the region's edge
    "jump_base": (256, 0, 512, 256),           # top down, 0.94 m
    "speed_top": (512, 0, 768, 512),           # top down, 0.80 m across by 1.60 m along (half sizes)
    "metal_dark": (768, 0, 1024, 256),         # painted dark metal, 1 m across
    "metal_light": (768, 256, 1024, 512),      # painted pale shell, 1 m across
    "pickup_cell": (0, 256, 256, 512),         # the cell's skin: u round it, v up it
    "drone_top": (256, 256, 512, 512),         # top down, 0.50 m
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

    # the drone from above: a pale shell, a dark hatch, a gold ring lamp and a number
    x, y, h, w = region_xy("drone_top", 0.50, seed=13)
    r = np.hypot(x, y)
    img = np.ones((h, w, 3)) * hexcol("d7dde8")
    img = coat(img, (0.93, 0.94, 0.97), 60, 0.35, 98, feather=0.6)
    hatch = ring_mask(r, -1, 0.17, 0.008)
    img = img * (1 - hatch[..., None]) + hexcol("1b2238") * hatch[..., None]
    lamp = ring_mask(r, 0.20, 0.235, 0.008)
    img = img * (1 - lamp[..., None]) + hexcol(GOLD) * lamp[..., None]
    seamr = ring_mask(r, 0.335, 0.35, 0.006)
    img = img * (1 - seamr[..., None] * 0.6) + hexcol("8f9bb3") * seamr[..., None] * 0.6
    nose = np.clip(1 - np.abs(x) / 0.05, 0, 1) * ((y > 0.27) & (y < 0.44))
    img = img * (1 - nose[..., None]) + hexcol("1b2238") * nose[..., None]
    put("drone_top", img, hexcol(GOLD) * lamp[..., None] * 0.9 + img * 0.10 * (1 - lamp[..., None]))

    for name, colour, glow in (("sw_teal", TEAL, 1.0), ("sw_lime", LIME, 1.0), ("sw_violet", VIOLET, 1.0), ("sw_ice", ICE, 1.0),
                               ("sw_gold", GOLD, 1.0), ("sw_glass", "0b1020", 0.0), ("sw_cream", "f4fff2", 0.8), ("sw_black", "0a0d18", 0.0)):
        x0, y0, x1, y1 = REGIONS[name]
        img = np.ones((y1 - y0, x1 - x0, 3)) * hexcol(colour)
        img = coat(img, (0.94, 0.96, 0.98), 40, 0.3, 120 + x0, feather=0.8)
        put(name, img, img * glow if glow else None)
    a = save("props", alb)
    save("props_emit", emi)
    return a


def beam():
    h, w = 512, 256
    v = np.linspace(0, 1, h)[:, None] * np.ones((1, w))                         # 0 at the drone, 1 at the far end
    alpha = (1 - v) ** 1.4 * 0.30
    for c in (0.30, 0.62):
        alpha = alpha + np.exp(-((v - c) / 0.05) ** 2) * 0.10 * (1 - v)
    col = np.ones((h, w, 3)) * hexcol("bfe9ff")
    OUT.mkdir(parents=True, exist_ok=True)
    out = np.dstack([col, np.clip(alpha, 0, 1)])
    Image.fromarray((out * 255 + 0.5).astype(np.uint8)).save(OUT / "arena_stage_beam.png")
    return out


def main():
    made = {"deck": deck(), "line": line(), "rim": rim(), "hull": hull(), "under": under(), "mark": mark(),
            "props": props(), "holo": holo()}
    beam()
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
