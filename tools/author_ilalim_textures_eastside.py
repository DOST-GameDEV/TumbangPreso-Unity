"""Paint the east side of Taft: the shop row facing the court and the district behind it (ILALIM-1.3).

  py -3 tools/author_ilalim_textures_eastside.py [--sheet N] [--only name,name]

Writes ArtSource/ilalim/textures/east_*.png (albedo, and height and normal where a surface has
relief) plus one face per sign in tools/author_ilalim_signs.py SIGNS (east_sign_<key>.png), and
the swatch sheets Logs/ilalim-blender/east_swatches_vN.png and east_signs_vN.png.

THE STYLE is the house style (KANTO_DESIGN_GUIDE.md section 3, LAGOON_REWORK_GUIDE.md section 2,
tools/author_ilalim_textures.py): FLAT fills, a few LARGE patches with FEATHERED organic edges,
low contrast between a thing and its joints, no grain, no noise, no streaks, no airbrushed blur.
Every surface gets ITS OWN drawing; none is another surface's generator in disguise. Shapes that
are drawn (windows, tiles, blocks, letters) are drawn with a slightly wobbly hand: their outlines
are pushed about by a broad smooth field, never jittered per pixel.

WHAT THE REFERENCES SHOW (Wikimedia Commons, Judgefloro's CC0 series at Taft and Padre Faura,
listed in the kit's report): the West East Center podium is SALMON-PINK painted render, with
ribbon balconies, planters and panels of BREEZE BLOCKS; the Astral Tower is cream balcony bands
over a salmon wall, one band per floor; the shops below have green and maroon fascias, glass
fronts, roll-up shutters and striped canvas. The district behind is painted concrete mid-rises
with ribbon windows, grilles, aircon boxes and water tanks.

ROLE HUES (Art_Direction.md section 1): nothing near offence orange #f87020 or defence blue
#0080e8. The salmon is pushed PINK (hue about 8 degrees, low saturation), never orange; glass is
teal-grey, never blue; the only red and blue that approach the role hues are PC Express's
official mark, the recorded brand exception, and it is used unchanged.

THE SURFACES (all world-scale; the tile sizes are in the eastside script's MATERIALS):
  east_wec_render    salmon-pink render of the podium parapets: two soft coats and a few squarish
                     repainted patches, the one mark a Manila owner leaves on a wall.
  east_wec_breeze    breeze-block screen: 40 cm blocks, each a quatrefoil hole, soft edges.
  east_wec_wall      the paler recessed wall behind the balconies, broad damp washes.
  east_astral_wall   one storey of the tower's salmon wall: sliding windows with cream frames
                     and curtains behind the glass, a soft shadow under the slab above.
  east_astral_band   the cream balcony band, with a feathered dirt wash along its lower edge.
  east_glass_ribbon  a ribbon window band: teal-grey glass, cream mullions, a transom, curtains.
  east_render        neutral painted render, tinted per building (the district palette).
  east_concrete      bare concrete: soft coats and faint formwork board joints every 60 cm.
  east_lod_ribbon / east_lod_punched / east_lod_balcony
                     one storey of a far building's facade each (ribbons; punched windows with
                     grilles and rust under the sills; balconies with laundry). Neutral, tinted.
  east_tile          10 cm ceramic tiles of the shopfront pilasters, drawn lozenges.
  east_shutter       a roll-up shutter's ribs, galvanized, with a big soft scuffed patch.
  east_shop_glass    shop glass: dark teal-grey, one gentle gradient, one broad pale patch.
  east_tin           corrugated GI: soft ribs, a few rust patches. Neutral, tinted per awning.
  east_canvas_green / east_canvas_maroon
                     striped awning canvas, two different stripe drawings, sun-faded patches.
  east_timber        painted timber of sign frames and posts: long soft grain washes.
  east_interior_shelves / east_interior_eatery / east_interior_pc
                     the back walls seen through open fronts: goods on shelves; tiles and a
                     menu board; a row of glowing screens.
  east_ac            a window aircon's front: soft louvres and a badge.
  east_tank          a stainless water tank's ribs.
  east_brick         the school's red brick panels: soft hand-drawn lozenges in stack bond.
  east_curtain       a glass curtain wall's grid, one storey tall.
  east_roof          flat roof slab: grey with big dark bitumen patches.
  east_grille        a Filipino window grille (bars and a sunburst), RGBA, dark paint.
  east_grime_drips   the positional rain grime under every slab and sill (multiplier overlay).
  east_grime_splash  the street splash band at the foot of every wall (multiplier overlay).
  east_pcx_mark      PC Express's official artwork, downsized, the registered badge removed.
"""
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_ilalim_signs as S          # noqa: E402  (the SIGNS table only)

OUT = ROOT / "ArtSource" / "ilalim" / "textures"
SHEETS = ROOT / "Logs" / "ilalim-blender"
LOGO = ROOT / "Assets" / "TumbangPreso" / "Art" / "models" / "textures" / "pc_express_horizontal_rgb.png"
FONTS = Path("C:/Windows/Fonts")


# ------------------------------------------------------------------ helpers

def hexcol(h):
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])


class Canvas:
    """A tile w_m x h_m metres at `ppm` pixels per metre, with X, Y in metres (Y down the image)."""

    def __init__(self, w_m, h_m, ppm):
        self.w_m, self.h_m, self.ppm = w_m, h_m, ppm
        self.w, self.h = int(round(w_m * ppm)), int(round(h_m * ppm))
        self.Y, self.X = np.mgrid[0:self.h, 0:self.w] / ppm

    def field(self, scale_m, seed, stretch=(1.0, 1.0)):
        """Periodic smooth noise with features about `scale_m` across. Tiles exactly."""
        white = np.random.default_rng(seed).standard_normal((self.h, self.w))
        fy = np.fft.fftfreq(self.h)[:, None] * stretch[1]
        fx = np.fft.fftfreq(self.w)[None, :] * stretch[0]
        s = scale_m * self.ppm
        n = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-(fx ** 2 + fy ** 2) * s * s * 2)))
        return (n - n.mean()) / (n.std() + 1e-9)

    def flat(self, h):
        return np.ones((self.h, self.w, 3)) * hexcol(h)

    def coat(self, img, shift, scale_m, coverage, seed, feather=0.5, stretch=(1.0, 1.0), colour=None):
        """Organic patches with FEATHERED edges over `coverage` of the surface. `shift` multiplies,
        or `colour` (hex) replaces, inside them."""
        n = self.field(scale_m, seed, stretch) + 0.12 * self.field(scale_m / 2.5, seed + 1, stretch)
        n = (n - n.mean()) / n.std()
        edge = np.quantile(n, 1 - coverage)
        m = np.clip((n - edge) / feather + 0.5, 0, 1)
        m = (m * m * (3 - 2 * m))[..., None]
        if colour is not None:
            return img * (1 - m) + hexcol(colour) * m
        return img * (1 + (np.asarray(shift) - 1) * m)

    def wob(self, amp_m, scale_m, seed):
        """A broad smooth displacement (dx, dy), in metres, for a hand-drawn wobble."""
        return self.field(scale_m, seed) * amp_m, self.field(scale_m, seed + 50) * amp_m


def soft(d, feather_m):
    """A mask from a signed distance (negative inside), with a feathered edge."""
    m = np.clip(0.5 - d / max(feather_m, 1e-6), 0, 1)
    return m * m * (3 - 2 * m)


def sd_rrect(X, Y, cx, cy, hw, hh, r):
    qx, qy = np.abs(X - cx) - (hw - r), np.abs(Y - cy) - (hh - r)
    return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r


def paint(img, mask, colour, alpha=1.0):
    m = (np.clip(mask, 0, 1) * alpha)[..., None]
    return img * (1 - m) + hexcol(colour) * m


def normal_from_height(h, strength):
    gy, gx = np.gradient(h * strength * 8)
    n = np.dstack((-gx, gy, np.ones_like(h)))
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n * 0.5 + 0.5


def save(name, albedo, height=None, strength=0.6, alpha=None):
    OUT.mkdir(parents=True, exist_ok=True)
    a = np.clip(albedo, 0, 1)
    if alpha is not None:
        rgba = np.dstack((a, np.clip(alpha, 0, 1)))
        Image.fromarray((rgba * 255).astype(np.uint8), "RGBA").save(OUT / f"{name}_albedo.png")
    else:
        Image.fromarray((a * 255).astype(np.uint8)).save(OUT / f"{name}_albedo.png")
    if height is not None:
        span = np.ptp(height)
        h = (height - height.min()) / span if span > 1e-9 else np.full_like(height, 0.5)
        Image.fromarray((h * 255).astype(np.uint8)).save(OUT / f"{name}_height.png")
        Image.fromarray((normal_from_height(h, strength) * 255).astype(np.uint8)).save(OUT / f"{name}_normal.png")
    print("[east-tex]", name)


def periodic(u, period):
    """Distance to the nearest multiple of `period`."""
    d = np.mod(u, period)
    return np.minimum(d, period - d)


# ------------------------------------------------------------------ West East Center podium

def east_wec_render():
    c = Canvas(4, 4, 256)
    img = c.flat("d09a90")
    img = c.coat(img, (1.03, 1.025, 1.02), 1.5, 0.30, 501, feather=1.3)
    img = c.coat(img, (0.965, 0.96, 0.96), 1.0, 0.22, 502, feather=1.1)
    # Repainted patches: squarish, a shade pinker, their edges feathered but straight-ish. The
    # owner painted over a crack or a sign that came down.
    dx, dy = c.wob(0.05, 0.6, 503)
    for cx, cy, hw, hh in ((1.0, 1.2, 0.7, 0.45), (3.1, 3.0, 0.5, 0.6)):
        m = soft(sd_rrect(c.X + dx, c.Y + dy, cx, cy, hw, hh, 0.12), 0.08)
        img = paint(img, m, "d6a39a", 0.8)
    save("east_wec_render", img)


def east_wec_breeze():
    c = Canvas(1.6, 1.6, 400)
    blk = 0.4
    lx, ly = np.mod(c.X, blk) - blk / 2, np.mod(c.Y, blk) - blk / 2
    dx, dy = c.wob(0.006, 0.15, 511)
    lx, ly = lx + dx, ly + dy
    # The block face: a square with soft rounded corners, a hair inside its cell (the mortar).
    face = soft(sd_rrect(lx, ly, 0, 0, 0.19, 0.19, 0.03), 0.01)
    # The hole: a quatrefoil, four round lobes around the centre.
    r = np.hypot(lx, ly)
    ang = np.arctan2(ly, lx)
    lobe = 0.085 + 0.04 * np.cos(4 * ang)
    hole = soft(r - lobe, 0.012)
    img = c.flat("b88478")                       # the mortar, just darker than the paint
    img = paint(img, face, "d4a196")
    img = paint(img, hole, "4a3432")             # the shadowed inside of the screen
    # The lit lower edge of each hole, where the block's depth catches the light.
    rim = soft(np.hypot(lx, ly - 0.02) - lobe, 0.012) * (1 - hole)
    img = paint(img, rim * (ly > 0), "e2b6ab", 0.6)
    img = c.coat(img, (0.96, 0.955, 0.95), 0.5, 0.25, 512, feather=1.0)
    height = face * 0.6 - hole
    save("east_wec_breeze", img, height, 0.8)


def east_wec_wall():
    c = Canvas(4, 4, 256)
    img = c.flat("e2c4ba")
    # Broad vertical damp washes, where rain blown under the slab runs down.
    img = c.coat(img, (0.955, 0.95, 0.95), 1.2, 0.26, 521, feather=1.6, stretch=(0.45, 1.0))
    img = c.coat(img, (1.025, 1.02, 1.02), 1.4, 0.2, 522, feather=1.2)
    save("east_wec_wall", img)


# ------------------------------------------------------------------ the Astral Tower

def _curtain(c, img, x0, x1, z0, z1, colour, seed, open_frac):
    """A curtain hung behind a pane: a soft-edged panel from the top, gathered to one side, with
    two or three broad soft folds."""
    dx, dy = c.wob(0.015, 0.4, seed)
    width = (x1 - x0) * open_frac
    right = (seed % 2) == 0
    cx0 = x1 - width if right else x0
    m = soft(sd_rrect(c.X + dx, c.Y + dy, cx0 + width / 2, (z0 + z1) / 2, width / 2, (z1 - z0) / 2, 0.03), 0.02)
    folds = 0.5 + 0.5 * np.cos((c.X - cx0) / max(width, 0.1) * math.tau * 2.0)
    col = hexcol(colour)
    shade = col * (0.94 + 0.06 * folds[..., None])
    return img * (1 - m[..., None]) + shade * m[..., None]


def east_astral_wall():
    # One storey, 3.0 m, drawn with z = 0 at the FLOOR (image bottom). The cream balcony band
    # covers the lowest 1.1 m, so the windows sit from 1.15 to 2.65.
    c = Canvas(6, 3, 200)
    Z = c.h_m - c.Y                                  # metres above the floor
    img = c.flat("c98f86")
    img = c.coat(img, (1.03, 1.03, 1.025), 1.2, 0.25, 531, feather=1.3)
    img = c.coat(img, (0.965, 0.96, 0.96), 0.9, 0.2, 532, feather=1.0)
    dx, dy = c.wob(0.01, 0.5, 533)
    rng = np.random.default_rng(534)
    for x0 in (0.55, 3.55):
        x1 = x0 + 1.9
        frame = soft(sd_rrect(c.X + dx, Z + dy, (x0 + x1) / 2, 1.9, (x1 - x0) / 2, 0.78, 0.04), 0.012)
        img = paint(img, frame, "eee5d3")
        glass = np.zeros_like(c.X)
        for a, b in ((x0 + 0.06, (x0 + x1) / 2 - 0.02), ((x0 + x1) / 2 + 0.02, x1 - 0.06)):
            g = soft(sd_rrect(c.X + dx, Z + dy, (a + b) / 2, 1.9, (b - a) / 2, 0.71, 0.02), 0.01)
            glass = np.maximum(glass, g)
        pane = c.flat("4d6563") * (1 + 0.12 * np.clip((Z[..., None] - 1.2) / 1.4, 0, 1))
        img = img * (1 - glass[..., None]) + pane * glass[..., None]
        colour = ["efe4cc", "d9c1c8", "cfe0d2", "ecdca0"][rng.integers(4)]
        before = img.copy()
        img = _curtain(c, img, x0 + 0.06, x1 - 0.06, c.h_m - 2.61, c.h_m - 1.19, colour, int(x0 * 10) + 535,
                       rng.uniform(0.35, 0.8))
        img = before * (1 - glass[..., None]) + img * glass[..., None]
    # The slab above throws a soft shadow and damp band on the top 25 cm.
    top = soft(2.72 - Z, 0.1)
    img = img * (1 - 0.12 * (1 - top)[..., None])
    save("east_astral_wall", img)


def east_astral_band():
    # The balcony band, 1.2 m tall with v = 0 at its FOOT: cream, and a dirt wash along the lower
    # edge where rain runs off the slab nose, its top edge wobbling.
    c = Canvas(4, 1.2, 256)
    Z = c.h_m - c.Y
    img = c.flat("ece3cf")
    img = c.coat(img, (1.02, 1.02, 1.015), 1.2, 0.25, 541, feather=1.3, stretch=(1.0, 3.0))
    img = c.coat(img, (0.97, 0.965, 0.955), 0.9, 0.2, 542, feather=1.1)
    edge = 0.26 + 0.07 * c.field(0.7, 543)[0:1, :]
    img = img * (1 - 0.085 * soft(Z - edge, 0.06)[..., None])
    save("east_astral_band", img)


# ------------------------------------------------------------------ glass and generic walls

def east_glass_ribbon():
    # A ribbon window band 1.6 m tall, v = 0 at its sill: teal-grey glass, cream mullions every
    # 1.2 m, a transom 45 cm under the head, and curtains in some panes.
    c = Canvas(4.8, 1.6, 200)
    Z = c.h_m - c.Y
    img = c.flat("4f6866") * (1 + 0.14 * (Z[..., None] / 1.6))
    rng = np.random.default_rng(551)
    for k in range(4):
        x0, x1 = k * 1.2 + 0.04, (k + 1) * 1.2 - 0.04
        if rng.uniform() < 0.7:
            colour = ["ede3cb", "dcc6ca", "cde0d3", "e9dba2", "c9d3dc"][rng.integers(5)]
            img = _curtain(c, img, x0, x1, c.h_m - 1.5, c.h_m - 0.05, colour, 552 + k, rng.uniform(0.3, 0.9))
    dx, dy = c.wob(0.006, 0.4, 553)
    mull = soft(periodic(c.X + dx, 1.2) - 0.035, 0.008)
    transom = soft(np.abs(Z + dy - 1.15) - 0.03, 0.008)
    edge = soft(np.minimum(Z, c.h_m - Z) - 0.045, 0.008)
    img = paint(img, np.maximum(np.maximum(mull, transom), edge), "e9e2d0")
    save("east_glass_ribbon", img)


def east_render():
    # NEUTRAL: each material tints it.
    c = Canvas(4, 4, 256)
    img = c.flat("ecebe7")
    img = c.coat(img, (0.965, 0.962, 0.958), 1.4, 0.28, 561, feather=1.3)
    img = c.coat(img, (1.025, 1.025, 1.02), 1.0, 0.18, 562, feather=1.1)
    img = c.coat(img, (0.975, 0.975, 0.97), 0.7, 0.1, 563, feather=0.9, stretch=(0.5, 1.0))
    save("east_render", img)


def east_concrete():
    c = Canvas(4, 4, 256)
    img = c.flat("aba69d")
    img = c.coat(img, (1.03, 1.03, 1.025), 1.3, 0.28, 571, feather=1.3)
    img = c.coat(img, (0.96, 0.958, 0.955), 0.9, 0.2, 572, feather=1.0)
    dx, dy = c.wob(0.012, 0.8, 573)
    boards = soft(periodic(c.Y + dy, 0.6) - 0.006, 0.006)
    img = img * (1 - 0.035 * boards[..., None])
    save("east_concrete", img, -boards, 0.4)


def _storey(c):
    return c.h_m - c.Y


def east_lod_ribbon():
    c = Canvas(6, 3.2, 128)
    Z = _storey(c)
    img = c.flat("ebe8e1")
    img = c.coat(img, (0.965, 0.96, 0.955), 1.2, 0.25, 581, feather=1.2)
    dx, dy = c.wob(0.01, 0.6, 582)
    band = soft(np.abs(Z + dy - 1.75) - 0.72, 0.015)
    img = paint(img, band, "5a6b6c")
    rng = np.random.default_rng(583)
    for k in range(5):
        if rng.uniform() < 0.6:
            x0 = k * 1.2 + 0.05
            m = soft(sd_rrect(c.X + dx, Z + dy, x0 + 0.4, 1.85, rng.uniform(0.2, 0.5), 0.55, 0.03), 0.02) * band
            img = paint(img, m, ["e6dcc6", "d6c3c4", "cad9cd"][rng.integers(3)])
    mull = soft(periodic(c.X + dx, 1.2) - 0.03, 0.01) * band
    img = paint(img, mull, "e2ddd0")
    img = paint(img, soft(Z - 0.22, 0.02), "d3cfc6", 0.7)          # the slab edge
    save("east_lod_ribbon", img)


def east_lod_punched():
    c = Canvas(6, 3.2, 128)
    Z = _storey(c)
    img = c.flat("e9e5dd")
    img = c.coat(img, (0.96, 0.955, 0.95), 1.2, 0.25, 591, feather=1.2)
    dx, dy = c.wob(0.012, 0.6, 592)
    rng = np.random.default_rng(593)
    for k, x in enumerate((1.0, 3.0, 5.0)):
        # Rust bleeding from the grille down the wall: a soft tongue under the sill.
        tongue = soft(sd_rrect(c.X + dx, Z + dy, x + rng.uniform(-0.3, 0.3), 0.55, 0.12, 0.45, 0.1), 0.06)
        img = paint(img, tongue, "9a7a62", 0.35)
        frame = soft(sd_rrect(c.X + dx, Z + dy, x, 1.75, 0.66, 0.76, 0.04), 0.012)
        img = paint(img, frame, "efe8d6")
        glass = soft(sd_rrect(c.X + dx, Z + dy, x, 1.75, 0.58, 0.68, 0.03), 0.01)
        img = paint(img, glass, "56696a")
        if k == 1:        # an aircon box in the lower pane
            ac = soft(sd_rrect(c.X + dx, Z + dy, x + 0.2, 1.3, 0.34, 0.22, 0.03), 0.01)
            img = paint(img, ac, "e3e0d6")
        bars = soft(periodic(c.X + dx - x, 0.14) - 0.012, 0.006) * glass
        img = paint(img, bars, "35302c", 0.85)
    img = paint(img, soft(Z - 0.2, 0.02), "d2cdc4", 0.6)
    save("east_lod_punched", img)


def east_lod_balcony():
    c = Canvas(6, 3.2, 128)
    Z = _storey(c)
    img = c.flat("cfcac1")                                   # the recessed wall, in shadow
    dx, dy = c.wob(0.012, 0.6, 601)
    for x in (1.5, 4.5):
        door = soft(sd_rrect(c.X + dx, Z + dy, x, 1.65, 0.8, 0.95, 0.03), 0.012)
        img = paint(img, door, "56686a")
        img = paint(img, soft(np.abs(c.X + dx - x) - 0.03, 0.01) * door, "e5e0d4")
    # Laundry on a line across the balcony: little flat shapes in faded colours.
    rng = np.random.default_rng(602)
    line = 2.3
    for k in range(9):
        x = rng.uniform(0.2, 5.8)
        w, h = rng.uniform(0.18, 0.4), rng.uniform(0.3, 0.6)
        m = soft(sd_rrect(c.X + dx, Z + dy, x, line - h / 2, w / 2, h / 2, 0.04), 0.012)
        img = paint(img, m, ["e9e2cc", "c9d8cc", "d8b7bd", "e6d38e", "b9c7cf", "d4d0c6"][rng.integers(6)])
    # The parapet band across the lower 1.1 m, lighter and lit.
    band = soft(Z + dy - 1.1, 0.012)
    img = paint(img, band, "ece8e0")
    img = paint(img, soft(np.abs(Z + dy - 1.08) - 0.04, 0.01), "dcd7cc")
    save("east_lod_balcony", img)


# ------------------------------------------------------------------ shopfronts

def east_tile():
    c = Canvas(1, 1, 512)
    t = 0.1
    dx, dy = c.wob(0.004, 0.3, 611)
    lx, ly = np.mod(c.X + dx, t) - t / 2, np.mod(c.Y + dy, t) - t / 2
    tile = soft(sd_rrect(lx, ly, 0, 0, 0.046, 0.046, 0.012), 0.004)
    img = c.flat("d8d2c4")                                   # the grout, close to the tile
    ix, iy = np.floor((c.X + dx) / t).astype(int) % 10, np.floor((c.Y + dy) / t).astype(int) % 10
    tone = np.random.default_rng(612).choice([0.975, 1.0, 1.0, 1.02], size=(10, 10))[iy, ix]
    img = img * (1 - tile[..., None]) + (hexcol("f1ece0") * tone[..., None]) * tile[..., None]
    hl = soft(sd_rrect(lx + 0.012, ly + 0.012, 0, 0, 0.014, 0.014, 0.008), 0.006) * tile
    img = paint(img, hl, "fbf8f0", 0.5)
    img = c.coat(img, (0.97, 0.965, 0.96), 0.35, 0.2, 613, feather=1.0)
    save("east_tile", img, tile, 0.5)


def east_shutter():
    c = Canvas(2, 1, 256)
    rib = 0.075
    dx, dy = c.wob(0.004, 0.6, 621)
    ph = np.mod(c.Y + dy, rib) / rib
    prof = 0.5 + 0.5 * np.cos(ph * math.tau)                 # the rounded rib
    img = c.flat("c7c7c1") * (0.93 + 0.1 * prof[..., None])
    img = c.coat(img, (0.93, 0.92, 0.9), 0.7, 0.22, 622, feather=1.0)
    img = c.coat(img, (1.03, 1.03, 1.03), 0.5, 0.15, 623, feather=0.8)
    save("east_shutter", img, prof, 0.6)


def east_shop_glass():
    c = Canvas(4, 4, 128)
    img = c.flat("3e5351") * (1 + 0.1 * (1 - c.Y / 4)[..., None])
    img = c.coat(img, (1.12, 1.12, 1.1), 1.4, 0.18, 631, feather=1.4, stretch=(0.6, 1.0))
    save("east_shop_glass", img)


def east_shop_display():
    # What a shop window shows: goods on shelves and a counter, dim behind teal-grey glass, with
    # one broad soft pale reflection. 4 x 3 m, v = 0 at the sill.
    c = Canvas(4, 3, 128)
    Z = _storey(c)
    img = c.flat("4d4a45")
    rng = np.random.default_rng(761)
    dx, dy = c.wob(0.01, 0.5, 762)
    for z in (0.55, 1.2, 1.85):
        img = paint(img, soft(np.abs(Z + dy - z) - 0.03, 0.01), "7b6d5e")
        x = 0.1
        while x < 3.9:
            w, h = rng.uniform(0.15, 0.4), rng.uniform(0.2, 0.45)
            col = ["dcd6c8", "c2cfbd", "cfb2b3", "d4c486", "b3c1c4", "e4e0d6"][rng.integers(6)]
            img = paint(img, soft(sd_rrect(c.X + dx, Z + dy, x + w / 2, z + 0.03 + h / 2, w / 2, h / 2, 0.03), 0.012), col, 0.8)
            x += w + rng.uniform(0.03, 0.15)
    img = paint(img, soft(sd_rrect(c.X + dx, Z + dy, 2.6, 0.35, 1.0, 0.4, 0.05), 0.012), "8c8578")      # the counter
    glass = hexcol("3f5654")
    img = img * 0.55 + glass * 0.45
    img = c.coat(img, (1.16, 1.16, 1.14), 1.3, 0.2, 763, feather=1.5, stretch=(0.6, 1.0))
    save("east_shop_display", img)


def east_tin():
    # NEUTRAL: tinted per awning or roof.
    c = Canvas(2, 2, 256)
    dx, dy = c.wob(0.004, 0.8, 641)
    ph = np.mod(c.X + dx, 0.076) / 0.076
    prof = 0.5 + 0.5 * np.cos(ph * math.tau)
    img = c.flat("dcdcd8") * (0.92 + 0.1 * prof[..., None])
    img = c.coat(img, (0.96, 0.93, 0.9), 0.8, 0.1, 642, feather=1.3)       # rust patches, soft brown
    img = c.coat(img, (0.95, 0.95, 0.95), 0.9, 0.2, 643, feather=1.1)
    save("east_tin", img, prof, 0.7)


def _stripes(c, widths, colours, seed):
    """Stripes across u from a repeating list of widths and colours, their edges wobbling a little."""
    dx, dy = c.wob(0.008, 0.5, seed)
    period = sum(widths)
    u = np.mod(c.X + dx, period)
    img = c.flat(colours[0])
    at = 0.0
    for w, col in zip(widths, colours):
        m = soft(np.maximum(at - u, u - (at + w)), 0.006)
        img = paint(img, m, col)
        at += w
    return img


def east_canvas_green():
    c = Canvas(2, 2, 256)
    img = _stripes(c, (0.2, 0.2), ("3f7453", "ede4cd"), 651)
    img = c.coat(img, (1.08, 1.08, 1.06), 1.0, 0.3, 652, feather=1.3)          # sun-faded
    img = c.coat(img, (0.94, 0.93, 0.9), 0.6, 0.12, 653, feather=0.9)
    save("east_canvas_green", img)


def east_canvas_maroon():
    # A different drawing from the green: broad cream and maroon bands with a thin cream pinstripe
    # down the middle of each maroon one.
    c = Canvas(2, 2, 256)
    img = _stripes(c, (0.3, 0.13, 0.04, 0.13), ("ebdfc6", "872d36", "ebdfc6", "872d36"), 661)
    img = c.coat(img, (1.07, 1.06, 1.05), 1.1, 0.28, 663, feather=1.3)
    img = c.coat(img, (0.95, 0.94, 0.92), 0.7, 0.12, 664, feather=0.9)
    save("east_canvas_maroon", img)


def east_timber():
    c = Canvas(1.5, 1.5, 256)
    img = c.flat("a88c6c")
    img = c.coat(img, (0.94, 0.93, 0.91), 0.5, 0.3, 671, feather=1.4, stretch=(1.0, 0.18))
    img = c.coat(img, (1.04, 1.035, 1.03), 0.4, 0.2, 672, feather=1.2, stretch=(1.0, 0.25))
    save("east_timber", img)


def east_interior_shelves():
    c = Canvas(4, 3, 128)
    Z = _storey(c)
    img = c.flat("5b4d44")
    img = c.coat(img, (1.08, 1.07, 1.05), 1.0, 0.3, 681, feather=1.4)
    rng = np.random.default_rng(682)
    dx, dy = c.wob(0.01, 0.5, 683)
    for z in (0.45, 1.05, 1.65, 2.25):
        img = paint(img, soft(np.abs(Z + dy - z) - 0.03, 0.01), "8a7661")
        x = 0.1
        while x < 3.9:
            w, h = rng.uniform(0.12, 0.35), rng.uniform(0.18, 0.42)
            col = ["e8e2d2", "c9d8c4", "d6b4b8", "e0cf8e", "b9c7c9", "cfc4b0"][rng.integers(6)]
            m = soft(sd_rrect(c.X + dx, Z + dy, x + w / 2, z + 0.03 + h / 2, w / 2, h / 2, 0.03), 0.012)
            img = paint(img, m, col, 0.85)
            x += w + rng.uniform(0.02, 0.12)
    img = paint(img, soft(np.abs(Z - 2.85) - 0.04, 0.02), "efeadc", 0.8)    # a tube light
    save("east_interior_shelves", img)


def east_interior_eatery():
    c = Canvas(4, 3, 128)
    Z = _storey(c)
    img = c.flat("e7dfcc")
    tiles = soft(1.2 - Z, 0.01)
    grid = np.maximum(soft(periodic(c.X, 0.2) - 0.006, 0.004), soft(periodic(Z, 0.2) - 0.006, 0.004))
    img = paint(img, tiles, "dde6df")
    img = paint(img, grid * tiles, "b9c6be", 0.7)
    dx, dy = c.wob(0.01, 0.5, 691)
    board = soft(sd_rrect(c.X + dx, Z + dy, 2.2, 2.05, 0.9, 0.5, 0.05), 0.012)
    img = paint(img, board, "2f3d33")
    rng = np.random.default_rng(692)
    for k in range(5):
        y = 2.38 - k * 0.17
        w = rng.uniform(0.4, 1.1)
        img = paint(img, soft(sd_rrect(c.X + dx, Z + dy, 1.45 + w / 2, y, w / 2, 0.03, 0.02), 0.01), "e9e4d4", 0.9)
        img = paint(img, soft(sd_rrect(c.X + dx, Z + dy, 2.85, y, 0.1, 0.03, 0.02), 0.01), "e8d27a", 0.9)
    img = c.coat(img, (0.93, 0.92, 0.9), 1.0, 0.25, 693, feather=1.3)
    save("east_interior_eatery", img)


def east_interior_pc():
    c = Canvas(4, 3, 128)
    Z = _storey(c)
    img = c.flat("2e3533")
    img = c.coat(img, (1.1, 1.1, 1.08), 1.2, 0.25, 701, feather=1.4)
    dx, dy = c.wob(0.01, 0.5, 702)
    for row, z in enumerate((1.15, 1.75)):
        for k in range(6):
            x = 0.35 + k * 0.65 + row * 0.3
            m = soft(sd_rrect(c.X + dx, Z + dy, x, z, 0.24, 0.16, 0.03), 0.01)
            img = paint(img, m, ["a9d3c4", "c6dcae", "b5c9d6"][(k + row) % 3], 0.9)
    poster = soft(sd_rrect(c.X + dx, Z + dy, 3.4, 2.5, 0.3, 0.35, 0.03), 0.01)
    img = paint(img, poster, "c9b24e", 0.8)
    save("east_interior_pc", img)


def east_ac():
    # The front of a window aircon, 0.7 x 0.45 m, v = 0 at its foot.
    c = Canvas(0.7, 0.45, 800)
    Z = c.h_m - c.Y
    img = c.flat("e6e2d7")
    louvre = soft(sd_rrect(c.X, Z, 0.44, 0.24, 0.22, 0.16, 0.02), 0.004)
    lines = soft(periodic(Z, 0.035) - 0.006, 0.003) * louvre
    img = paint(img, louvre, "cfcabd")
    img = paint(img, lines, "9d988d", 0.8)
    img = paint(img, soft(sd_rrect(c.X, Z, 0.12, 0.3, 0.06, 0.03, 0.01), 0.003), "7d7a72")
    img = img * (1 - 0.08 * soft(Z - 0.1, 0.05)[..., None])
    save("east_ac", img, louvre - lines, 0.5)


def east_tank():
    c = Canvas(2, 1.2, 256)
    Z = c.h_m - c.Y
    ribs = 0.5 + 0.5 * np.cos(Z / 0.3 * math.tau)
    img = c.flat("c3c4bf") * (0.94 + 0.08 * ribs[..., None])
    img = c.coat(img, (0.93, 0.92, 0.9), 0.6, 0.25, 711, feather=1.2)
    save("east_tank", img, ribs, 0.4)


def east_brick():
    c = Canvas(2, 2, 256)
    bw, bh = 0.22, 0.075
    dx, dy = c.wob(0.004, 0.4, 721)
    row = np.floor((c.Y + dy) / bh)
    lx = np.mod(c.X + dx + (row % 2) * bw / 2, bw) - bw / 2
    ly = np.mod(c.Y + dy, bh) - bh / 2
    brick = soft(sd_rrect(lx, ly, 0, 0, bw / 2 - 0.009, bh / 2 - 0.008, 0.012), 0.004)
    img = c.flat("b28978")
    ix = np.floor((c.X + dx + (row % 2) * bw / 2) / bw).astype(int) % 9
    tone = np.random.default_rng(722).choice([0.95, 1.0, 1.0, 1.05], size=(27, 9))[row.astype(int) % 27, ix]
    img = img * (1 - brick[..., None]) + (hexcol("9e4a3e") * tone[..., None]) * brick[..., None]
    band = soft(np.abs(ly + 0.018) - 0.012, 0.006) * brick
    img = paint(img, band, "b3604f", 0.5)                    # one flat highlight band per brick
    img = c.coat(img, (0.95, 0.94, 0.93), 0.8, 0.22, 723, feather=1.2)
    save("east_brick", img, brick, 0.6)


def east_curtain():
    c = Canvas(3, 3.4, 128)
    Z = c.h_m - c.Y
    img = c.flat("5f7876")
    img = np.where((np.mod(np.floor(c.X / 1.5), 2) == 1)[..., None], img * 1.07, img)
    img = img * (1 + 0.12 * (Z / 3.4)[..., None])
    spandrel = soft(0.9 - Z, 0.01)
    img = paint(img, spandrel, "9eaeac")
    mull = np.maximum(soft(periodic(c.X, 1.5) - 0.03, 0.006), soft(np.abs(Z - 0.9) - 0.03, 0.006))
    mull = np.maximum(mull, soft(np.minimum(Z, 3.4 - Z) - 0.04, 0.006))
    img = paint(img, mull, "d3d6d2")
    img = c.coat(img, (1.06, 1.06, 1.05), 1.0, 0.18, 731, feather=1.3)
    save("east_curtain", img)


def east_roof():
    c = Canvas(6, 6, 170)
    img = c.flat("a09c93")
    img = c.coat(img, (1.03, 1.03, 1.025), 2.0, 0.3, 741, feather=1.3)
    # Two or three big bitumen patches over repaired cracks and a greenish damp corner: large,
    # soft and only a few steps darker (v1 read as leopard spots).
    img = c.coat(img, None, 2.6, 0.12, 742, feather=1.4, colour="8a857c")
    img = c.coat(img, None, 3.0, 0.07, 743, feather=1.6, colour="959985")
    save("east_roof", img)


def east_grille():
    # 1.2 x 1.4 m, RGBA: a frame, vertical bars every 14 cm, and a half-sunburst in the middle.
    c = Canvas(1.2, 1.4, 400)
    Z = c.h_m - c.Y
    dx, dy = c.wob(0.003, 0.3, 751)
    X = c.X + dx
    frame = soft(np.minimum(np.minimum(X, 1.2 - X), np.minimum(Z, 1.4 - Z)) - 0.03, 0.004)
    bars = soft(periodic(X - 0.6, 0.14) - 0.011, 0.004)
    r = np.hypot(X - 0.6, Z + dy - 0.7)
    ang = np.arctan2(Z + dy - 0.7, X - 0.6)
    rays = soft(np.abs(np.sin(ang * 6)) * r - 0.011, 0.004) * (r < 0.42)
    ring = soft(np.abs(r - 0.42) - 0.012, 0.004)
    hub = soft(r - 0.07, 0.004)
    a = np.maximum.reduce([frame, bars * (r > 0.42), rays, ring, hub])
    img = c.flat("2f2b28")
    img = c.coat(img, None, 0.3, 0.2, 752, feather=0.8, colour="6b4a36")           # rust
    save("east_grille", img, alpha=a)


# ------------------------------------------------------------------ grime overlays

def _mult(name, a):
    """Save a multiplier overlay; row 0 of `a` is the EDGE. Flipped so UV v = 0 is the edge."""
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.flipud(np.clip(a, 0, 1)) * 255).astype(np.uint8)).save(OUT / f"{name}.png")
    print("[east-tex]", name)


def east_grime_drips():
    # 8 m wide, 3 m down from a slab or sill edge. A soft band hugging the edge, then drawn
    # tongues: flat shapes that narrow to a round end. Shorter and warmer than the viaduct's.
    W, H, ppm = 8.0, 3.0, 160
    v, u = np.mgrid[0:int(H * ppm), 0:int(W * ppm)] / ppm
    c = Canvas(W, H, ppm)
    a = np.ones(v.shape + (3,))
    edge = 0.16 + 0.05 * c.field(0.6, 801)[0:1, :]
    a *= 1 - 0.1 * soft(v - edge, 0.05)[..., None]
    rng = np.random.default_rng(802)
    for k in range(22):
        u0 = rng.uniform(0, W)
        width = rng.uniform(0.08, 0.3)
        length = rng.choice([rng.uniform(0.3, 0.9), rng.uniform(0.9, 2.4)], p=[0.65, 0.35])
        col = np.array([[0.82, 0.79, 0.76], [0.84, 0.8, 0.74], [0.8, 0.8, 0.77]][rng.integers(3)])
        du = np.abs(((u - u0 - 0.04 * np.sin(v * 2 + k) + W / 2) % W) - W / 2)
        taper = np.clip(1 - v / length, 0, 1) ** 0.5
        half = width * (0.4 + 0.6 * taper)
        end = np.sqrt(np.clip(1 - ((v - (length - width)) / width) ** 2, 0, 1))
        half = np.where(v > length - width, half * end, half)
        m = np.clip((half - du) / 0.02 + 0.5, 0, 1) * (v < length) * rng.uniform(0.35, 0.7)
        a *= 1 - m[..., None] * (1 - col)
    _mult("east_grime_drips", a)


def east_grime_splash():
    # 8 m wide, 1.5 m up from the ground: brown-grey mud, a ragged soft top, short tongues up.
    W, H, ppm = 8.0, 1.5, 160
    v, u = np.mgrid[0:int(H * ppm), 0:int(W * ppm)] / ppm
    c = Canvas(W, H, ppm)
    a = np.ones(v.shape + (3,))
    top = 0.42 + 0.12 * c.field(0.45, 811)[0:1, :]
    a *= 1 - soft(v - top, 0.08)[..., None] * (1 - np.array([0.8, 0.77, 0.72]))
    rng = np.random.default_rng(812)
    for k in range(12):
        u0 = rng.uniform(0, W)
        du = np.abs(((u - u0 + W / 2) % W) - W / 2)
        length = rng.uniform(0.5, 0.95)
        m = soft(du - rng.uniform(0.06, 0.22) * np.clip(1 - v / length, 0, 1) ** 0.5, 0.03) * (v < length)
        a *= 1 - (m * rng.uniform(0.3, 0.6))[..., None] * (1 - np.array([0.84, 0.8, 0.75]))
    _mult("east_grime_splash", a)


# ------------------------------------------------------------------ PC Express, the brand exception

def east_pcx_mark():
    """The supplied official artwork on the lightbox's white acrylic, unchanged in colour. The
    registered-mark badge is painted out, as the real storefront has none."""
    im = Image.open(LOGO).convert("RGBA")
    W, H = im.size
    d = ImageDraw.Draw(im)
    # The (R) badge sits in the top-right corner of the red field, about 94..97 % across.
    d.ellipse((int(W * 0.94), int(H * 0.09), int(W * 0.975), int(H * 0.2)), fill=(213, 39, 51, 255))
    im = im.resize((2048, int(2048 * H / W)), Image.LANCZOS)
    face = Image.new("RGB", (2400, int(2400 * S.SIGNS["pcx"]["h"] / S.SIGNS["pcx"]["w"])), (244, 241, 234))
    lw = int(min(face.width * 0.94, face.height * 0.9 * im.width / im.height))
    lh = int(lw * im.height / im.width)
    logo = im.resize((lw, lh), Image.LANCZOS)
    face.paste(logo, ((face.width - lw) // 2, (face.height - lh) // 2), logo)
    OUT.mkdir(parents=True, exist_ok=True)
    face.save(OUT / "east_sign_pcx_albedo.png")
    print("[east-tex] east_sign_pcx (official mark)")


# ------------------------------------------------------------------ sign faces

PPM_SIGN = 220
FONT_OF = {
    "lightbox": "segoeuib.ttf", "fascia": "ariblk.ttf", "blade": "impact.ttf", "aboard": "segoeprb.ttf",
    "placard": "impact.ttf", "tarp": "bahnschrift.ttf", "pylon": "ariblk.ttf", "hung": "impact.ttf",
    "painted": "ariblk.ttf", "banner": "impact.ttf", "tin": "segoeprb.ttf",
}
# (horizontal stretch of the letters, slant, stroke in px of the drawn outline).
LETTER = {
    "lightbox": (1.0, 0.0, 0), "fascia": (1.18, 0.0, 0), "blade": (0.9, 0.2, 0), "aboard": (1.0, 0.0, 0),
    "placard": (0.8, 0.0, 0), "tarp": (1.25, 0.0, 0), "pylon": (1.15, 0.0, 0), "hung": (0.85, 0.0, 0),
    "painted": (1.4, 0.0, 0), "banner": (0.8, 0.0, 0), "tin": (1.05, 0.0, 0),
}


def _text_mask(text, font_file, px_h, stretch, slant, stencil=False):
    """A line of text as a float mask, `px_h` tall (cap height), stretched and slanted."""
    font = ImageFont.truetype(str(FONTS / font_file), int(px_h * 1.45))
    box = font.getbbox(text)
    w, h = box[2] - box[0] + 8, box[3] - box[1] + 8
    im = Image.new("L", (w, h), 0)
    ImageDraw.Draw(im).text((4 - box[0], 4 - box[1]), text, font=font, fill=255)
    if stencil:
        # Stencil bridges: a narrow vertical gap through the middle of each letter's bowl, the
        # mark of a cut stencil.
        d = ImageDraw.Draw(im)
        x = 4
        for ch in text:
            cw = font.getlength(ch)
            if ch in "ABDOPQRG0689":
                d.rectangle((x + cw * 0.46, 0, x + cw * 0.54, h), fill=0)
            x += cw
    a = np.asarray(im, dtype=float) / 255
    new_w = max(1, int(a.shape[1] * stretch))
    a = np.asarray(Image.fromarray((a * 255).astype(np.uint8)).resize((new_w, a.shape[0]), Image.LANCZOS),
                   dtype=float) / 255
    if slant:
        out = np.zeros((a.shape[0], a.shape[1] + int(a.shape[0] * slant) + 2))
        for r in range(a.shape[0]):
            off = int((a.shape[0] - r) * slant)
            out[r, off:off + a.shape[1]] = a[r]
        a = out
    return a


def _hand(mask, amp_px, scale_px, seed):
    """Push a crisp mask about by a broad smooth field, then soften its edge by a pixel: a painted
    letter, not a printed one."""
    h, w = mask.shape
    rng = np.random.default_rng(seed)
    fx = ndimage.gaussian_filter(rng.standard_normal((h, w)), scale_px, mode="wrap")
    fy = ndimage.gaussian_filter(rng.standard_normal((h, w)), scale_px, mode="wrap")
    fx *= amp_px / (fx.std() + 1e-9)
    fy *= amp_px / (fy.std() + 1e-9)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    out = ndimage.map_coordinates(mask, [yy + fy, xx + fx], order=1, mode="constant")
    return ndimage.gaussian_filter(out, 0.7)


def _fit(block, max_w, max_h):
    h, w = block.shape
    s = min(max_w / w, max_h / h)
    return np.asarray(Image.fromarray((np.clip(block, 0, 1) * 255).astype(np.uint8)).resize(
        (max(1, int(w * s)), max(1, int(h * s))), Image.LANCZOS), dtype=float) / 255


def _blit(canvas, mask, cx, cy):
    h, w = mask.shape
    x0, y0 = int(cx - w / 2), int(cy - h / 2)
    H, W = canvas.shape
    xs, ys = max(0, x0), max(0, y0)
    xe, ye = min(W, x0 + w), min(H, y0 + h)
    if xe > xs and ye > ys:
        canvas[ys:ye, xs:xe] = np.maximum(canvas[ys:ye, xs:xe], mask[ys - y0:ye - y0, xs - x0:xe - x0])


def _icon(kind, size):
    """A tiny flat pictogram (bowl, cap, cross, chicken, steam) as a mask `size` px square."""
    c = Canvas(1, 1, size)
    X, Y = c.X - 0.5, c.Y - 0.5
    if kind == "bowl":
        bowl = soft(np.maximum(np.hypot(X, Y + 0.1) - 0.42, -(Y - 0.02)), 0.01)
        steam = sum(soft(np.abs(X - dx - 0.05 * np.sin(Y * 18)) - 0.035, 0.01) * (Y < -0.05) * (Y > -0.45)
                    for dx in (-0.15, 0.0, 0.15))
        return np.clip(bowl + steam, 0, 1)
    if kind == "cap":
        top = soft(np.abs(X) + np.abs(Y + 0.08) * 2.2 - 0.46, 0.01)
        base = soft(sd_rrect(X, Y, 0, 0.12, 0.22, 0.1, 0.03), 0.01)
        return np.clip(top + base, 0, 1)
    if kind == "cross":
        return np.clip(soft(sd_rrect(X, Y, 0, 0, 0.14, 0.42, 0.04), 0.01) + soft(sd_rrect(X, Y, 0, 0, 0.42, 0.14, 0.04), 0.01), 0, 1)
    if kind == "chicken":
        body = soft(np.hypot(X * 0.9, Y - 0.08) - 0.32, 0.01)
        head = soft(np.hypot(X - 0.22, Y + 0.25) - 0.14, 0.01)
        comb = soft(np.hypot(X - 0.2, Y + 0.42) - 0.07, 0.01)
        return np.clip(body + head + comb, 0, 1)
    if kind == "steam":
        bun = soft(np.maximum(np.hypot(X, Y - 0.1) - 0.36, -(Y - 0.1) - 0.18), 0.01)
        return bun
    return np.zeros((size, size))


def _weather(c, img, seed, amount=1.0):
    """Sun-fade and dirt on a sign face: a few big soft patches, a darker lower edge."""
    img = c.coat(img, (1.05, 1.05, 1.04), c.w_m * 0.35, 0.25 * amount, seed, feather=1.3)
    img = c.coat(img, (0.94, 0.93, 0.91), c.w_m * 0.25, 0.18 * amount, seed + 1, feather=1.1)
    Z = c.h_m - c.Y
    img = img * (1 - 0.08 * amount * soft(Z - 0.12 * c.h_m, 0.1 * c.h_m)[..., None])
    return img


def sign_face(key, spec):
    system = spec["system"]
    if spec.get("mark") == "pcx":
        east_pcx_mark()
        return
    w_m, h_m = spec["w"], spec["h"]
    vertical = system == "blade"          # words run UP the blade: paint it turned
    cw, ch = (h_m, w_m) if vertical else (w_m, h_m)
    c = Canvas(cw, ch, PPM_SIGN)
    bg, fg, acc = spec["bg"], spec["fg"], spec["accent"]
    img = c.flat(bg)
    alpha = None
    stretch, slant, _ = LETTER[system]
    font = FONT_OF[system]
    margin = 0.08 * min(c.w, c.h) + 6
    if system == "fascia":
        margin += 0.05 * PPM_SIGN          # clear of the painted rules top and bottom
    ink = np.zeros((c.h, c.w))
    seed = sum(map(ord, key))
    lines = spec["lines"]
    extra = spec.get("extra", {})

    if system == "banner":
        # Letters stacked one above the other, condensed and tight, then a small footer line.
        word, foot = lines[0][0], lines[1][0] if len(lines) > 1 else ""
        n = len(word)
        cell = (c.h - 2 * margin) * 0.82 / n
        for i, chx in enumerate(word):
            m = _text_mask(chx, font, cell * 0.8, stretch, 0.0)
            m = _fit(m, c.w - 2 * margin, cell * 0.92)
            _blit(ink, m, c.w / 2, margin + cell * (i + 0.5))
        if foot:
            parts = foot.split(" • ")
            fh = (c.h - 2 * margin) * 0.16 / max(1, len(parts))
            for j, p in enumerate(parts):
                m = _fit(_text_mask(p, "bahnschrift.ttf", 60, 1.0, 0.0), c.w - 2 * margin, fh * 0.8)
                _blit(ink, m, c.w / 2, c.h - margin - (len(parts) - j - 0.5) * fh)
        img = paint(img, soft(np.abs(c.X - c.w_m / 2) - (c.w_m / 2 - 0.05), 0.01), acc)
    else:
        total = sum(r for _, r in lines)
        avail_h = c.h - 2 * margin
        gap = 0.18
        unit = avail_h / (total + gap * (len(lines) - 1))
        y = margin
        icon = extra.get("icon")
        text_left, text_w = margin, c.w - 2 * margin
        if icon and system not in ("blade",):
            isz = int(min(avail_h, c.w * 0.18))
            _blit(ink, _icon(icon, isz) * 1.0, margin + isz / 2, c.h / 2)
            text_left, text_w = margin + isz + margin * 0.6, c.w - 2 * margin - isz - margin * 0.6
        if icon and system == "blade":
            isz = int(min(c.h - 2 * margin, c.w * 0.14))
            _blit(ink, _icon(icon, isz), c.w - margin - isz / 2, c.h / 2)
            text_w = c.w - 2 * margin - isz - margin * 0.5
        for text, rel in lines:
            lh = unit * rel
            f = font if rel >= 0.7 or system in ("aboard", "tin") else ("bahnschrift.ttf" if system != "placard" else font)
            m = _text_mask(text, f, 80, stretch if rel >= 0.7 else 1.0, slant, stencil=(system == "placard"))
            m = _fit(m, text_w, lh)
            _blit(ink, m, text_left + text_w / 2, y + lh / 2)
            y += lh + unit * gap
    amp = {"aboard": 2.2, "tin": 2.6, "painted": 2.0, "fascia": 1.4, "banner": 1.0}.get(system, 0.6)
    ink = _hand(ink, amp, 18, seed)

    # The system's own field and trim.
    if system == "fascia":
        img = paint(img, soft(np.abs(c.Y - c.h_m + 0.08) - 0.025, 0.01), acc)
        img = paint(img, soft(np.abs(c.Y - 0.08) - 0.025, 0.01), acc)
    if system == "tarp":
        img = paint(img, soft(c.h_m - c.Y - 0.14, 0.01), acc)                  # the printed footer bar
        img = paint(img, soft(np.minimum(c.X, c.w_m - c.X) - 0.04, 0.01), acc, 0.9)
    if system == "pylon":
        ring = np.abs(sd_rrect(c.X, c.Y, c.w_m / 2, c.h_m / 2, c.w_m / 2 - 0.08, c.h_m / 2 - 0.08, 0.1))
        img = paint(img, soft(ring - 0.025, 0.01), acc)
    if system == "hung":
        img = paint(img, soft(c.Y - 0.12, 0.01), acc)                           # a coloured top band
    if system == "aboard":
        img = c.coat(img, (1.12, 1.12, 1.1), 0.25, 0.25, seed + 3, feather=0.9)  # chalk dust
    if system == "tin":
        # Galvanized sheet: soft ribs across, rust patches, a white painted field behind the words.
        ph = np.mod(c.X, 0.076) / 0.076
        img = img * (0.93 + 0.08 * (0.5 + 0.5 * np.cos(ph * math.tau)))[..., None]
        img = c.coat(img, None, 0.5, 0.14, seed + 4, feather=1.0, colour="95705a")
        field = soft(sd_rrect(c.X, c.Y, c.w_m / 2, c.h_m / 2, c.w_m / 2 - 0.1, c.h_m / 2 - 0.1, 0.05), 0.04)
        field = field * (0.5 + 0.5 * np.clip(c.field(0.2, seed + 5) + 0.8, 0, 1))
        img = paint(img, field, "ece8dc", 0.9)
    if system == "painted":
        # Paint on render: the field's edge is ragged and the whole thing is faded.
        rag = sd_rrect(c.X, c.Y, c.w_m / 2, c.h_m / 2, c.w_m / 2 - 0.08, c.h_m / 2 - 0.06, 0.05) + 0.03 * c.field(0.4, seed + 6)
        alpha = soft(rag, 0.03)
    if system == "placard":
        img = paint(img, soft(np.abs(sd_rrect(c.X, c.Y, c.w_m / 2, c.h_m / 2, c.w_m / 2 - 0.03, c.h_m / 2 - 0.03, 0.04)) - 0.008, 0.004), acc)

    img = paint(img, ink, fg)
    img = _weather(c, img, seed + 10, amount={"painted": 0.7, "tin": 0.8, "tarp": 0.8}.get(system, 1.0))
    if system == "painted":
        fade = np.clip(0.85 + 0.12 * c.field(1.2, seed + 7), 0.65, 1.0)
        alpha = alpha * fade
    if vertical:
        img = np.rot90(img, 1)
        alpha = np.rot90(alpha, 1) if alpha is not None else None
    save(f"east_sign_{key}", img, alpha=alpha)


# ------------------------------------------------------------------ sheets

PAINTERS = [east_wec_render, east_wec_breeze, east_wec_wall, east_astral_wall, east_astral_band,
            east_glass_ribbon, east_render, east_concrete, east_lod_ribbon, east_lod_punched,
            east_lod_balcony, east_tile, east_shutter, east_shop_glass, east_shop_display, east_tin, east_canvas_green,
            east_canvas_maroon, east_timber, east_interior_shelves, east_interior_eatery, east_interior_pc,
            east_ac, east_tank, east_brick, east_curtain, east_roof, east_grille]
OVERLAYS = [east_grime_drips, east_grime_splash]


def swatch_sheet(version):
    SHEETS.mkdir(parents=True, exist_ok=True)
    names = [p.__name__ for p in PAINTERS]
    cell, cols = 300, 7
    rows = math.ceil(len(names) / cols)
    sheet = Image.new("RGB", (cols * (cell + 16) + 16, rows * (cell + 40) + 16), (245, 242, 235))
    d = ImageDraw.Draw(sheet)
    for k, n in enumerate(names):
        im = Image.open(OUT / f"{n}_albedo.png").convert("RGBA")
        bg = Image.new("RGBA", im.size, (200, 196, 188, 255))
        bg.alpha_composite(im)
        tile = Image.new("RGB", (im.width * 2, im.height * 2))
        for i in range(2):
            for j in range(2):
                tile.paste(bg.convert("RGB"), (i * im.width, j * im.height))
        tile.thumbnail((cell, cell), Image.LANCZOS)
        x, y = 16 + (k % cols) * (cell + 16), 16 + (k // cols) * (cell + 40)
        sheet.paste(tile, (x, y))
        d.text((x, y + cell + 6), f"{n} (2x2)", fill=(40, 40, 40))
    sheet.save(SHEETS / f"east_swatches_v{version}.png")
    # The signs, each at its true proportions on one sheet.
    keys = list(S.SIGNS)
    W = 1800
    x = y = 16
    rowh = 0
    placed = []
    for k in keys:
        im = Image.open(OUT / f"east_sign_{k}_albedo.png").convert("RGBA")
        spec = S.SIGNS[k]
        scale = 80 if spec["system"] != "blade" else 80
        tw = int(im.width * (scale * spec["w"] if spec["system"] != "blade" else scale * spec["h"]) / im.width) \
            if spec["system"] != "blade" else int(scale * spec["w"])
        th = int(tw * im.height / im.width)
        if x + tw > W - 16:
            x, y = 16, y + rowh + 30
            rowh = 0
        placed.append((k, im, x, y, tw, th))
        x += tw + 20
        rowh = max(rowh, th)
    sheet = Image.new("RGB", (W, y + rowh + 50), (120, 118, 112))
    d = ImageDraw.Draw(sheet)
    for k, im, x, y, tw, th in placed:
        bg = Image.new("RGBA", im.size, (170, 150, 140, 255))
        bg.alpha_composite(im)
        sheet.paste(bg.convert("RGB").resize((tw, th), Image.LANCZOS), (x, y))
        d.text((x, y + th + 4), f"{k} [{S.SIGNS[k]['system']}]", fill=(250, 250, 240))
    sheet.save(SHEETS / f"east_signs_v{version}.png")
    print("[east-tex] sheets v", version)


def main():
    argv = sys.argv[1:]
    version = int(argv[argv.index("--sheet") + 1]) if "--sheet" in argv else 1
    only = set(argv[argv.index("--only") + 1].split(",")) if "--only" in argv else None
    for p in PAINTERS + OVERLAYS:
        if only is None or p.__name__ in only:
            p()
    for key, spec in S.SIGNS.items():
        if only is None or f"east_sign_{key}" in only or "signs" in only:
            sign_face(key, spec)
    swatch_sheet(version)


if __name__ == "__main__":
    main()
