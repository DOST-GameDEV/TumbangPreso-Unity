"""Paint the Ilalim ng Tulay street-prop textures, in the house illustrated style (ILALIM-1.3, prop kit).

  py -3 tools/author_ilalim_textures_props.py [--sheet N]

Writes ArtSource/ilalim/textures/prop_*.png and a swatch sheet
Logs/ilalim-blender/prop_swatches_vN.png. The props themselves are modelled by
tools/author_ilalim_props.py, which reads these files.

THE STYLE is Kanto's and the Lagoon's (KANTO_DESIGN_GUIDE.md section 3, LAGOON_REWORK_GUIDE.md
section 2): FLAT fills, a few LARGE patches with FEATHERED organic edges, low contrast, no grain,
no noise, no streaks, no airbrushed blur. Every surface has ITS OWN drawing, written here as its
own function; nothing is another surface's generator with a new colour.

TWO KINDS OF TEXTURE:
  * TILING surfaces (world-scale UVs, TILE_M = 2 m per tile, 512 px): the materials the props are
    made of. Some are NEUTRAL (near white) and are tinted per material in Blender and Unity, so
    one drawing of painted steel serves the jade pares cart and the yellow hoop post.
      prop_laminate   the pisonet cabinets' black laminate: soft grey scuffs where knees and
                      hands rub, nothing else.
      prop_paint      NEUTRAL painted steel: two broad soft coats and a few round rust blooms.
      prop_stainless  the cart counters: cool grey with wide soft horizontal sheen bands.
      prop_tarp       NEUTRAL tarpaulin: sun-bleached patches and a few long soft creases.
      prop_canvas     NEUTRAL umbrella canvas: faded crown patches and a few soft mildew spots.
      prop_plastic    NEUTRAL moulded plastic (monobloc chairs, stools, crates, drum, bin):
                      one broad sheen and grey scuff patches.
      prop_wood       weathered plank wood: pale grey-brown with a few broad wobbly grain rings.
      prop_rubber     tyres and cables: near black with dusty grey patches.
      prop_concrete   the hoop's concrete-filled tyre: grey with broad trowel arcs.
  * ONE-OFF artwork (UV 0..1 on one face): every sign, the screens, the pad. Each sign is its own
    SIGN SYSTEM, so no two read alike (Ilalim_Ng_Tulay.md section 10.4):
      prop_sign_pisonet    hand-painted yellow rate board, PISONET / P1 = 5 MIN (brush Impact)
      prop_pisonet_atlas   the three screens, the keyboard and the terminal number stickers
      prop_sign_cart       the pares cart's painted side panel, PARES NI MANG BOYET
      prop_sign_fishball   the fishball cart's painted panel (a different hand, Arial Black)
      prop_sign_aboard     the chalk A-board, PARES / MAMI with prices (chalk hand)
      prop_sign_backboard  the barangay hoop's backboard, BRGY. 671, with ball smudges
      prop_sign_bawal_a    BAWAL UMIHI DITO as a chipped red ENAMEL plate
      prop_sign_bawal_b    BAWAL UMIHI DITO brush-painted on a red plank, MULTA P500
      prop_sign_tarp       the deep-navy barangay PAALALA tarpaulin (digital print, faded)
      prop_sign_sarisari   a cardboard MAY LOAD / YELO sign in marker
      prop_sachet_strip    a hanging strip of sachets, invented, no real brands
      prop_pad_plate       the overclock pad's charcoal plate with gold chevrons
  * TWO GRIME OVERLAYS, multipliers (white = clean), mapped by the models' UVGrime and UVSplash:
      prop_grime_top    2 m x 1 m, v = metres below a prop's top edge: a soft band and short
                        drawn tongues, where rain runs off a counter or a board.
      prop_grime_foot   2 m x 1 m, v = metres above the pavement: the splash band every prop on a
                        Manila pavement carries, with a ragged top.

LETTERING is set in a real font, then WARPED by a smooth displacement field and feathered by a
pixel, so it reads as brushwork, chalk or marker rather than type. The fonts come from
C:/Windows/Fonts (Impact, Arial Black, Segoe Print, Ink Free, Bahnschrift); any missing font
falls back to the next one.

ROLE HUES (Art_Direction.md section 1): no colour here sits near offence orange #f87020 or
defence blue #0080e8. The barangay tarpaulin is DEEP NAVY, never a mid blue; the pad's bars are
plum, mint and gold; the cart roof is maroon, not orange.
"""
import os
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[1]
OUT = Path(os.environ.get("ILALIM_TEX_OUT", ROOT / "ArtSource" / "ilalim" / "textures"))
SHEETS = ROOT / "Logs" / "ilalim-blender"
TILE = 512
TILE_M = 2.0
FONTS = Path("C:/Windows/Fonts")
FONT_FILES = {
    "impact": ["impact.ttf", "ariblk.ttf", "arialbd.ttf"],
    "black": ["ariblk.ttf", "impact.ttf", "arialbd.ttf"],
    "print": ["segoeprb.ttf", "segoepr.ttf", "comicbd.ttf", "arialbd.ttf"],
    "ink": ["Inkfree.ttf", "segoepr.ttf", "comicbd.ttf", "arialbd.ttf"],
    "bahn": ["bahnschrift.ttf", "arialbd.ttf"],
}

# Atlas regions (u0, v0, u1, v1) in IMAGE pixels of the 1024 x 1024 pisonet atlas, top-left origin.
# tools/author_ilalim_props.py repeats these numbers (Blender's Python has no scipy to import
# this module).
ATLAS = {
    "screen_1": (0, 0, 512, 384),
    "screen_2": (512, 0, 1024, 384),
    "screen_3": (0, 384, 512, 768),
    "keyboard": (512, 384, 1024, 576),
    "num_1": (512, 576, 682, 746),
    "num_2": (682, 576, 852, 746),
    "num_3": (852, 576, 1022, 746),
}


def hexcol(h):
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])


def smooth(h, w, scale_px, seed, stretch=(1.0, 1.0)):
    """Periodic smooth noise of any size, features about `scale_px` across. Tiles exactly."""
    white = np.random.default_rng(seed).standard_normal((h, w))
    fy = np.fft.fftfreq(h)[:, None] * stretch[1]
    fx = np.fft.fftfreq(w)[None, :] * stretch[0]
    n = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-(fx ** 2 + fy ** 2) * scale_px * scale_px * 2)))
    return (n - n.mean()) / (n.std() + 1e-9)


def patches(h, w, scale_px, coverage, seed, feather=0.5, stretch=(1.0, 1.0)):
    """A mask of organic patches covering `coverage` of the image, with a feathered edge."""
    n = smooth(h, w, scale_px, seed, stretch) + 0.12 * smooth(h, w, scale_px / 2.5, seed + 1, stretch)
    n = (n - n.mean()) / n.std()
    edge = np.quantile(n, 1 - coverage)
    m = np.clip((n - edge) / feather + 0.5, 0, 1)
    return m * m * (3 - 2 * m)


def coat(img, shift, scale_px, coverage, seed, feather=0.5, stretch=(1.0, 1.0)):
    m = patches(img.shape[0], img.shape[1], scale_px, coverage, seed, feather, stretch)
    return img * (1 + (np.asarray(shift) - 1) * m[..., None])


def fill(h, w, colour):
    return np.ones((h, w, 3)) * hexcol(colour)


def blobs(img, colour, count, r_px, seed, strength=1.0, halo=None, wrap=True, region=None):
    """Round soft-edged drawn spots (rust blooms, chips, mildew, ball marks). Each is a flat disc
    with a narrow feather and a slightly wobbly outline."""
    h, w = img.shape[:2]
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:h, 0:w]
    col = hexcol(colour)
    for _ in range(count):
        if region:
            x0, y0, x1, y1 = region
            cx, cy = rng.uniform(x0, x1), rng.uniform(y0, y1)
        else:
            cx, cy = rng.uniform(0, w), rng.uniform(0, h)
        r = r_px * rng.uniform(0.6, 1.3)
        dx, dy = xx - cx, yy - cy
        if wrap:
            dx = (dx + w / 2) % w - w / 2
            dy = (dy + h / 2) % h - h / 2
        ang = np.arctan2(dy, dx)
        ph = rng.uniform(0, 6.28)
        rr = r * (1 + 0.12 * np.sin(3 * ang + ph) + 0.07 * np.sin(5 * ang + 2 * ph))
        d = np.hypot(dx, dy)
        a = np.clip((rr - d) / max(1.5, r * 0.18) + 0.5, 0, 1) * strength * rng.uniform(0.6, 1.0)
        if halo:
            ha = np.clip((rr * 1.7 - d) / max(2.0, r * 0.5) + 0.5, 0, 1) * 0.5 * strength
            img = img * (1 - ha[..., None]) + hexcol(halo) * ha[..., None]
        img = img * (1 - a[..., None]) + col * a[..., None]
    return img


def wobbly_lines(h, w, spacing_px, width_px, wobble_px, seed, axis="h"):
    """Soft hand-drawn lines every `spacing_px`, drifting and thickening along their length."""
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    along, across, n_along = (xx, yy, w) if axis == "h" else (yy, xx, h)
    drift = smooth(1, n_along, n_along / 6, seed)[0] if axis == "h" else smooth(n_along, 1, n_along / 6, seed)[:, 0]
    thick = smooth(1, n_along, n_along / 10, seed + 3)[0] if axis == "h" else smooth(n_along, 1, n_along / 10, seed + 3)[:, 0]
    idx = along.astype(int) % n_along
    d = (across - drift[idx] * wobble_px) % spacing_px
    d = np.minimum(d, spacing_px - d)
    return np.exp(-(d / (width_px * (1 + 0.35 * thick[idx]))) ** 2)


# ------------------------------------------------------------------ lettering

def font(key, size):
    for f in FONT_FILES[key]:
        p = FONTS / f
        if p.exists():
            return ImageFont.truetype(str(p), size)
    return ImageFont.load_default()


def text_mask(h, w, lines, key, box, align="center", stroke=0, spacing=0.12, seed=1, wobble=3.0,
              lean=0.0):
    """Render `lines` fitted inside `box` (x0, y0, x1, y1), then warp it by a smooth field so the
    strokes wobble like a hand's. Returns a float mask 0..1 at h x w."""
    S = 2                                   # supersample, then down
    H, W = h * S, w * S
    x0, y0, x1, y1 = (v * S for v in box)
    bw, bh = x1 - x0, y1 - y0
    size = 400
    while size > 6:
        f = font(key, size)
        dims = [f.getbbox(t, stroke_width=stroke * S) for t in lines]
        widths = [d[2] - d[0] for d in dims]
        heights = [d[3] - d[1] for d in dims]
        total = sum(heights) + spacing * size * (len(lines) - 1)
        if max(widths) <= bw and total <= bh:
            break
        size = int(size * 0.94)
    img = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(img)
    y = y0 + (bh - total) / 2
    for t, dm, tw, th in zip(lines, dims, widths, heights):
        if align == "center":
            x = x0 + (bw - tw) / 2
        elif align == "left":
            x = x0
        else:
            x = x1 - tw
        d.text((x - dm[0], y - dm[1]), t, font=f, fill=255, stroke_width=stroke * S, stroke_fill=255)
        y += th + spacing * size
    m = np.asarray(img, dtype=float) / 255
    if lean:
        yy, xx = np.mgrid[0:H, 0:W].astype(float)
        m = ndimage.map_coordinates(m, [yy, xx + lean * (yy - H / 2)], order=1, mode="constant")
    if wobble:
        yy, xx = np.mgrid[0:H, 0:W].astype(float)
        dx = smooth(H, W, 22 * S, seed) * wobble * S
        dy = smooth(H, W, 22 * S, seed + 1) * wobble * S
        m = ndimage.map_coordinates(m, [yy + dy, xx + dx], order=1, mode="constant")
    m = np.asarray(Image.fromarray((np.clip(m, 0, 1) * 255).astype(np.uint8)).resize((w, h), Image.LANCZOS),
                   dtype=float) / 255
    # A one-pixel feather and a firm core: a painted edge, not a blur.
    m = ndimage.gaussian_filter(m, 0.6)
    return np.clip((m - 0.5) * 1.8 + 0.5, 0, 1)


def paint(img, mask, colour, seed=0, variation=0.05):
    """Lay paint through `mask`: a flat colour with one soft coat inside it, so a brushed letter
    is a touch uneven, never gradient-filled."""
    h, w = img.shape[:2]
    col = np.ones((h, w, 3)) * hexcol(colour)
    if variation:
        col = coat(col, (1 + variation,) * 3, max(h, w) / 8, 0.3, seed + 900, feather=1.0)
    return img * (1 - mask[..., None]) + col * mask[..., None]


def rect_mask(h, w, box, radius=0, wobble=0.0, seed=0, feather=1.2):
    """A painted rectangle (a border band, a target square), optionally hand-wobbled."""
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    if wobble:
        xx = xx + smooth(h, w, 40, seed) * wobble
        yy = yy + smooth(h, w, 40, seed + 1) * wobble
    x0, y0, x1, y1 = box
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    hx, hy = (x1 - x0) / 2 - radius, (y1 - y0) / 2 - radius
    qx, qy = np.abs(xx - cx) - hx, np.abs(yy - cy) - hy
    d = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - radius
    return np.clip(-d / feather + 0.5, 0, 1)


def frame_mask(h, w, box, band, radius=0, wobble=0.0, seed=0):
    x0, y0, x1, y1 = box
    outer = rect_mask(h, w, box, radius, wobble, seed)
    inner = rect_mask(h, w, (x0 + band, y0 + band, x1 - band, y1 - band), max(0, radius - band), wobble, seed)
    return np.clip(outer - inner, 0, 1)


def circle_mask(h, w, cx, cy, r, feather=1.2):
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    return np.clip((r - np.hypot(xx - cx, yy - cy)) / feather + 0.5, 0, 1)


def drips(img, x_list, top, length, width, colour, seed, strength=0.5):
    """Short drawn rust or grime tongues hanging from points (bolts, a top edge)."""
    h, w = img.shape[:2]
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    col = hexcol(colour)
    for x in x_list:
        L = length * rng.uniform(0.6, 1.2)
        wd = width * rng.uniform(0.7, 1.2)
        v = (yy - top) / L
        half = wd * (1 - 0.6 * np.clip(v, 0, 1)) * (1 + 0.08 * np.sin(yy * 0.05 + x))
        a = np.clip((half - np.abs(xx - x)) / 1.5 + 0.5, 0, 1) * (v >= 0) * np.clip((1 - v) * 4, 0, 1)
        a *= strength * rng.uniform(0.6, 1.0)
        img = img * (1 - a[..., None]) + col * a[..., None]
    return img


# ------------------------------------------------------------------ saving

def save(name, img):
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(OUT / f"{name}.png")
    print("[ilalim-prop-tex]", name)


# ------------------------------------------------------------------ tiling surfaces (2 m tiles)

def prop_laminate():
    img = fill(TILE, TILE, "2e2c2f")
    img = coat(img, (1.16, 1.15, 1.13), 130, 0.2, seed=501, feather=1.3)
    img = coat(img, (0.92, 0.92, 0.92), 170, 0.18, seed=502, feather=1.3)
    save("prop_laminate", img)


def prop_paint():
    img = fill(TILE, TILE, "ebe9e4")
    img = coat(img, (0.965, 0.96, 0.955), 110, 0.3, seed=511, feather=1.2)
    img = coat(img, (1.025, 1.025, 1.02), 70, 0.18, seed=512, feather=1.0)
    # Rust blooms: a few round warm-brown spots with a paler halo, 2 to 5 cm across.
    img = blobs(img, "a9876a", 4, 12, seed=513, strength=0.45, halo="d8c9b4")
    save("prop_paint", img)


def prop_stainless():
    img = fill(TILE, TILE, "b9bcbe")
    img = coat(img, (1.07, 1.07, 1.07), 150, 0.3, seed=521, feather=1.5, stretch=(1.0, 0.3))
    img = coat(img, (0.94, 0.94, 0.945), 170, 0.22, seed=522, feather=1.5, stretch=(1.0, 0.3))
    img = blobs(img, "a4a39c", 5, 12, seed=523, strength=0.35)   # dull water marks
    save("prop_stainless", img)


def prop_tarp():
    img = fill(TILE, TILE, "e9e8e3")
    img = coat(img, (1.05, 1.05, 1.04), 120, 0.3, seed=531, feather=1.3)     # sun-bleached
    img = coat(img, (0.95, 0.945, 0.94), 80, 0.2, seed=532, feather=1.1)
    creases = wobbly_lines(TILE, TILE, 256, 3.0, 12, seed=533, axis="v")
    img = img * (1 - 0.06 * creases[..., None])
    save("prop_tarp", img)


def prop_canvas():
    img = fill(TILE, TILE, "ecebe7")
    img = coat(img, (1.045, 1.045, 1.04), 100, 0.28, seed=541, feather=1.2)
    img = blobs(img, "c9cbc0", 6, 7, seed=542, strength=0.35)                 # mildew spots
    save("prop_canvas", img)


def prop_plastic():
    img = fill(TILE, TILE, "ecebe8")
    img = coat(img, (1.04, 1.04, 1.04), 160, 0.22, seed=551, feather=1.3)
    img = coat(img, (0.93, 0.925, 0.915), 110, 0.12, seed=552, feather=1.1)     # scuffs
    save("prop_plastic", img)


def prop_wood():
    img = fill(TILE, TILE, "a38d74")
    img = coat(img, (1.06, 1.05, 1.04), 90, 0.3, seed=561, feather=1.2, stretch=(0.3, 1.0))
    rings = wobbly_lines(TILE, TILE, 46, 2.2, 10, seed=562, axis="h")
    img = img * (1 - 0.08 * rings[..., None])
    img = coat(img, (0.9, 0.89, 0.88), 60, 0.12, seed=563, feather=1.0)        # damp darkening
    save("prop_wood", img)


def prop_rubber():
    img = fill(TILE, TILE, "2a2828")
    img = coat(img, (1.3, 1.28, 1.25), 120, 0.2, seed=571, feather=1.3)        # dust
    save("prop_rubber", img)


def prop_concrete():
    img = fill(TILE, TILE, "a8a49c")
    img = coat(img, (1.05, 1.05, 1.04), 90, 0.3, seed=581, feather=1.2)
    yy, xx = np.mgrid[0:TILE, 0:TILE].astype(float)
    arcs = np.zeros((TILE, TILE))
    rng = np.random.default_rng(582)
    for _ in range(6):                    # broad trowel arcs
        cx, cy, r = rng.uniform(0, TILE), rng.uniform(0, TILE), rng.uniform(60, 160)
        dx = (xx - cx + TILE / 2) % TILE - TILE / 2
        dy = (yy - cy + TILE / 2) % TILE - TILE / 2
        d = np.abs(np.hypot(dx, dy) - r)
        arcs = np.maximum(arcs, np.exp(-(d / 3.0) ** 2) * (dy < 0))
    img = img * (1 - 0.06 * arcs[..., None])
    save("prop_concrete", img)


# ------------------------------------------------------------------ grime overlays

def _noise1d(n, scale_px, seed):
    return smooth(1, n, scale_px, seed)[0]


def prop_grime_top():
    W, H = 512, 256                        # 2 m x 1 m
    img = np.ones((H, W, 3))
    v, u = np.mgrid[0:H, 0:W].astype(float)
    edge = 16 + 6 * _noise1d(W, 40, 601)[None, :]
    band = np.clip((edge - v) / 6 + 0.5, 0, 1)
    img *= 1 - 0.12 * band[..., None]
    img = drips(img, list(np.random.default_rng(602).uniform(0, W, 9)), 8, 70, 7, "9d968b", seed=603, strength=0.35)
    # Row 0 is the top edge; Blender's v = 0 is the image's bottom row, so flip.
    save("prop_grime_top", np.flipud(img))


def prop_grime_foot():
    W, H = 512, 256                        # 2 m x 1 m, v up from the pavement
    img = np.ones((H, W, 3))
    v, u = np.mgrid[0:H, 0:W].astype(float)
    top = 30 + 10 * _noise1d(W, 30, 611)[None, :]
    band = np.clip((top - v) / 8 + 0.5, 0, 1)
    img = img * (1 - band[..., None] * (1 - hexcol("c7bfb1")))
    rng = np.random.default_rng(612)
    for _ in range(10):
        u0, wd, L = rng.uniform(0, W), rng.uniform(4, 12), rng.uniform(40, 80)
        du = np.abs((u - u0 + W / 2) % W - W / 2)
        half = wd * np.clip(1 - v / L, 0, 1) ** 0.6
        a = np.clip((half - du) / 1.5 + 0.5, 0, 1) * (v < L) * rng.uniform(0.35, 0.6)
        img = img * (1 - a[..., None] * (1 - hexcol("cfc8bb")))
    save("prop_grime_foot", np.flipud(img))


# ------------------------------------------------------------------ one-off artwork

def weather(img, seed, fade=0.05, dirt_bottom=True):
    """The common fate of a sign on Taft: a sun-faded patch or two and a soft dirt edge low down.
    Drawn as big feathered patches, never speckle."""
    h, w = img.shape[:2]
    img = coat(img, (1 + fade, 1 + fade, 1 + fade * 0.6), max(h, w) / 5, 0.25, seed, feather=1.2)
    if dirt_bottom:
        yy = np.mgrid[0:h, 0:w][0].astype(float)
        edge = h * (0.86 + 0.05 * _noise1d(w, w / 6, seed + 5)[None, :])
        a = np.clip((yy - edge) / (h * 0.05) + 0.5, 0, 1) * 0.16
        img = img * (1 - a[..., None])
    return img


def prop_sign_pisonet():
    h, w = 400, 1040                       # 1.3 m x 0.5 m
    img = fill(h, w, "e7c24a")
    img = coat(img, (1.04, 1.03, 1.0), 120, 0.3, seed=701, feather=1.2)
    img = paint(img, frame_mask(h, w, (10, 10, w - 10, h - 10), 26, 18, 2.5, 702), "8e2626", 703)
    img = paint(img, text_mask(h, w, ["PISONET"], "impact", (150, 42, w - 150, 250), seed=704, wobble=1.8,
                               lean=-0.06), "982a26", 705)
    img = paint(img, text_mask(h, w, ["P1 = 5 MIN"], "impact", (230, 268, w - 230, 356), seed=706,
                               wobble=1.5), "231f1d", 707)
    for cx in (84, w - 84):                # two painted one-peso coins
        img = paint(img, circle_mask(h, w, cx, 200, 58), "b8a257", 708)
        img = paint(img, circle_mask(h, w, cx, 200, 46), "d4bf6c", 709)
        img = paint(img, text_mask(h, w, ["1"], "impact", (cx - 22, 168, cx + 22, 232), seed=710, wobble=1.5),
                    "7d6a2c", 711)
    img = drips(img, [36, w - 36], 36, 110, 7, "9b6b3c", seed=712, strength=0.45)   # rust from the bolts
    img = weather(img, 713)
    save("prop_sign_pisonet", img)


def prop_pisonet_atlas():
    S = 1024
    img = fill(S, S, "1c1b1e")

    def region(key):
        return ATLAS[key]

    # Screen 1: a game, drawn as flat bands and a chunky hero block (the screens glow in Unity).
    x0, y0, x1, y1 = region("screen_1")
    sub = fill(y1 - y0, x1 - x0, "2f6f64")
    sh, sw = sub.shape[:2]
    sub[int(sh * 0.62):] = hexcol("3f7f3c")
    sub = paint(sub, rect_mask(sh, sw, (60, int(sh * 0.62) - 90, 120, int(sh * 0.62)), 8), "e8c74c", 1, 0)
    sub = paint(sub, rect_mask(sh, sw, (300, int(sh * 0.62) - 60, 420, int(sh * 0.62)), 10), "7c3f6a", 2, 0)
    sub = paint(sub, circle_mask(sh, sw, 420, 80, 40), "e9e2b8", 3)
    sub = paint(sub, text_mask(sh, sw, ["P1  04:32"], "bahn", (20, 16, 250, 60), wobble=0), "f1efe4", 4, 0)
    img[y0:y1, x0:x1] = sub
    # Screen 2: INSERT COIN on a dark screen.
    x0, y0, x1, y1 = region("screen_2")
    sub = fill(y1 - y0, x1 - x0, "173a36")
    sh, sw = sub.shape[:2]
    sub = paint(sub, text_mask(sh, sw, ["INSERT", "COIN"], "impact", (90, 70, sw - 90, 260), wobble=0),
                "e6d56a", 5, 0)
    sub = paint(sub, text_mask(sh, sw, ["P1 = 5 MIN"], "bahn", (150, 290, sw - 150, 340), wobble=0), "9fd8c6", 6, 0)
    img[y0:y1, x0:x1] = sub
    # Screen 3: a desktop, a green hill under a teal-green sky, and a taskbar.
    x0, y0, x1, y1 = region("screen_3")
    sub = fill(y1 - y0, x1 - x0, "74b4a6")
    sh, sw = sub.shape[:2]
    yy, xx = np.mgrid[0:sh, 0:sw].astype(float)
    hill = yy > sh * 0.55 + 50 * np.cos((xx / sw) * 3.0)
    sub[hill] = hexcol("5b9a45")
    sub[int(sh * 0.9):] = hexcol("24252a")
    sub = paint(sub, rect_mask(sh, sw, (120, 70, 380, 250), 6), "e9e6dc", 7, 0)
    sub = paint(sub, rect_mask(sh, sw, (120, 70, 380, 100), 6), "3f5f73", 8, 0)
    img[y0:y1, x0:x1] = sub
    # The keyboard: rows of soft, slightly lighter key lozenges on a dark case.
    x0, y0, x1, y1 = region("keyboard")
    sub = fill(y1 - y0, x1 - x0, "232226")
    sh, sw = sub.shape[:2]
    for r in range(5):
        n = 14 - (r == 4) * 6
        kw = (sw - 30) / 14
        for k in range(n):
            ww = kw * (6 if (r == 4 and k == 3) else 1)
            kx = 15 + k * kw + (r * 6) + (0 if not (r == 4 and k > 3) else kw * 5)
            if kx + kw > sw - 10:
                continue
            sub = paint(sub, rect_mask(sh, sw, (kx + 3, 12 + r * 34, kx + min(ww, sw - kx - 12) - 3, 12 + r * 34 + 28),
                                       6), "3a393e", 20 + r * 20 + k, 0)
    img[y0:y1, x0:x1] = sub
    # Number stickers 1, 2 and 3: white circles with a black hand-drawn number.
    for i in (1, 2, 3):
        x0, y0, x1, y1 = region(f"num_{i}")
        sub = fill(y1 - y0, x1 - x0, "e8e3d6")
        sh, sw = sub.shape[:2]
        sub = paint(sub, text_mask(sh, sw, [str(i)], "impact", (40, 25, sw - 40, sh - 25), wobble=2.0, seed=30 + i),
                    "1d1b1a", 31 + i)
        img[y0:y1, x0:x1] = sub
    save("prop_pisonet_atlas", img)


def prop_sign_cart():
    h, w = 400, 1360                       # 1.7 m x 0.5 m
    img = fill(h, w, "ece0c0")
    img = coat(img, (1.03, 1.02, 1.0), 120, 0.3, seed=721, feather=1.2)
    img = paint(img, frame_mask(h, w, (14, 14, w - 14, h - 14), 20, 30, 2.0, 722), "6f2431", 723)
    img = paint(img, text_mask(h, w, ["PARES NI", "MANG BOYET"], "impact", (300, 44, w - 60, 280), seed=724,
                               wobble=1.6, spacing=0.05), "7a2330", 725)
    img = paint(img, text_mask(h, w, ["MAMI  ·  LUGAW  ·  GOTO"], "black", (320, 298, w - 80, 352), seed=726,
                               wobble=2.0), "35573c", 727)
    # A painted steaming bowl: a deep bowl with a green band, a broth-dark rim, three wavy steam
    # strokes rising from it. Flat shapes, drawn, no shading.
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    bowl = np.clip((1 - ((xx - 160) / 118) ** 2 - ((yy - 196) / 100) ** 2) * 40, 0, 1) * (yy > 196)
    img = paint(img, bowl, "e4ddcc", 728, 0)
    img = paint(img, bowl * np.clip((9 - np.abs(yy - 250)) / 1.5, 0, 1), "35573c", 729, 0)
    rim = np.clip((1 - ((xx - 160) / 118) ** 2 - ((yy - 196) / 20) ** 2) * 30, 0, 1)
    img = paint(img, rim, "5a3423", 730, 0)
    for sx in (115, 160, 205):
        wave = sx + 12 * np.sin((yy - 40) / 22 + sx)
        steam = np.clip((7 - np.abs(xx - wave)) / 1.5, 0, 1) * (yy > 60) * (yy < 165)
        img = paint(img, steam, "b3aa9c", 731, 0)
    img = weather(img, 732)
    save("prop_sign_cart", img)


def prop_sign_fishball():
    h, w = 280, 1000                       # 1.1 m x 0.3 m
    img = fill(h, w, "e4d78c")
    img = coat(img, (1.03, 1.03, 1.0), 100, 0.3, seed=741, feather=1.2)
    img = paint(img, text_mask(h, w, ["FISHBALL"], "black", (30, 30, 640, 150), seed=742, wobble=2.5, lean=0.05),
                "2f5a37", 743)
    img = paint(img, text_mask(h, w, ["KIKIAM · SQUIDBALL"], "black", (30, 170, 640, 240), seed=744, wobble=2.0),
                "8a2a2a", 745)
    img = paint(img, circle_mask(h, w, 820, 140, 100), "8a2a2a", 746)
    img = paint(img, text_mask(h, w, ["P2", "ISA"], "impact", (755, 60, 885, 220), seed=747, wobble=2.0, spacing=0.02),
                "efe6c8", 748)
    img = weather(img, 749)
    save("prop_sign_fishball", img)


def prop_sign_aboard():
    h, w = 800, 600                        # 0.6 m x 0.8 m
    img = fill(h, w, "27302b")
    img = coat(img, (1.18, 1.18, 1.15), 120, 0.28, seed=761, feather=1.4)   # old chalk rubbed in
    chalk = "e8e6dc"
    img = paint(img, text_mask(h, w, ["PARES"], "print", (40, 50, w - 40, 230), seed=762, wobble=4.0, stroke=2),
                chalk, 763, 0.08)
    img = paint(img, text_mask(h, w, ["MAMI"], "print", (90, 250, w - 90, 390), seed=764, wobble=4.0, stroke=2),
                "e5d98c", 765, 0.08)
    img = paint(img, text_mask(h, w, ["pares  P70", "mami  P55", "+rice  P15"], "ink", (70, 430, w - 60, 700),
                               align="left", seed=766, wobble=3.0, stroke=1, spacing=0.3), chalk, 767, 0.08)
    img = paint(img, frame_mask(h, w, (24, 24, w - 24, h - 24), 5, 10, 4.0, 768), "c9c7bd", 769, 0)
    save("prop_sign_aboard", img)


def prop_sign_backboard():
    h, w = 680, 1020                       # 1.2 m x 0.8 m
    img = fill(h, w, "e6e1d3")
    img = coat(img, (1.03, 1.03, 1.02), 150, 0.3, seed=781, feather=1.3)
    # Ball marks: round soft grey smudges clustered round the target square, where shots land.
    img = blobs(img, "c4bfb2", 14, 22, seed=782, strength=0.5, wrap=False, region=(260, 220, 760, 560))
    img = paint(img, frame_mask(h, w, (8, 8, w - 8, h - 8), 22, 14, 1.5, 783), "7a2530", 784)
    # The shooter's square: 0.59 m x 0.45 m, its base just above the rim.
    img = paint(img, frame_mask(h, w, (260, 300, 760, 600), 22, 0, 1.5, 785), "7a2530", 786)
    img = paint(img, text_mask(h, w, ["BRGY. 671"], "impact", (300, 60, w - 300, 200), seed=787, wobble=2.5),
                "7a2530", 788)
    img = paint(img, text_mask(h, w, ["Handog ni Kag. Ruben Dela Paz"], "print", (40, 616, 520, 660), align="left",
                               seed=789, wobble=1.5), "4a4440", 790)
    img = drips(img, [120, 900], 30, 140, 6, "9b6b3c", seed=791, strength=0.4)
    img = weather(img, 792, fade=0.04)
    save("prop_sign_backboard", img)


def prop_sign_bawal_a():
    h, w = 520, 800                        # 0.8 m x 0.52 m, enamel
    img = fill(h, w, "b2302b")
    img = coat(img, (1.05, 1.03, 1.03), 120, 0.25, seed=801, feather=1.2)
    img = paint(img, frame_mask(h, w, (26, 26, w - 26, h - 26), 12, 30, 0.0, 802), "eee8da", 803, 0)
    img = paint(img, text_mask(h, w, ["BAWAL", "UMIHI", "DITO"], "black", (80, 60, w - 80, h - 60), seed=804,
                               wobble=0.6, spacing=0.14), "eee8da", 805, 0.02)
    # Enamel chips: dark iron with a rust halo, at the corners and edges where it was knocked.
    img = blobs(img, "2d2624", 4, 9, seed=806, strength=0.9, halo="7a4a33", wrap=False, region=(0, 0, w, 40))
    img = blobs(img, "2d2624", 3, 8, seed=807, strength=0.9, halo="7a4a33", wrap=False, region=(0, h - 40, w, h))
    img = drips(img, [44, w - 44], 44, 150, 7, "6e3a2a", seed=808, strength=0.5)
    img = weather(img, 809, fade=0.05)
    save("prop_sign_bawal_a", img)


def prop_sign_bawal_b():
    h, w = 450, 900                        # 0.9 m x 0.45 m, brush-painted plank
    img = fill(h, w, "9a2c27")
    img = coat(img, (1.06, 1.04, 1.04), 110, 0.3, seed=821, feather=1.2, stretch=(0.3, 1.0))
    strokes = wobbly_lines(h, w, 64, 4, 6, seed=822, axis="h")      # broad brush drags
    img = img * (1 - 0.05 * strokes[..., None])
    img = paint(img, text_mask(h, w, ["BAWAL UMIHI", "DITO!"], "print", (40, 26, w - 40, 300), seed=823, wobble=1.8,
                               stroke=2, spacing=0.16), "efe3c6", 824, 0.06)
    img = paint(img, text_mask(h, w, ["MULTA P500"], "ink", (300, 318, w - 60, 410), seed=825, wobble=1.5, stroke=2),
                "e8c64d", 826, 0.05)
    img = blobs(img, "4b3a30", 4, 5, seed=827, strength=0.8, wrap=False, region=(20, 20, w - 20, h - 20))   # nail heads
    img = weather(img, 828, fade=0.06)
    save("prop_sign_bawal_b", img)


def prop_sign_tarp():
    h, w = 600, 1020                       # 1.7 m x 1.0 m, deep navy digital print
    img = fill(h, w, "1f2a4c")
    img = coat(img, (1.1, 1.1, 1.06), 150, 0.3, seed=841, feather=1.4)      # sun-faded
    img = paint(img, rect_mask(h, w, (0, 0, w, 96)), "ece7da", 842, 0.02)
    img = paint(img, text_mask(h, w, ["BARANGAY 671  ·  ZONE 72  ·  ERMITA, MAYNILA"], "bahn", (30, 20, w - 30, 76),
                               wobble=0), "6e2330", 843, 0)
    img = paint(img, text_mask(h, w, ["PAALALA!"], "black", (40, 120, 700, 250), wobble=0), "e6c64f", 844, 0)
    img = paint(img, text_mask(h, w, ["BAWAL MAGTAPON NG BASURA", "AT UMIHI SA ILALIM NG TULAY"], "black",
                               (40, 272, 740, 400), align="left", wobble=0, spacing=0.3), "f0ece2", 845, 0)
    img = paint(img, text_mask(h, w, ["MULTA: P500  /  COMMUNITY SERVICE"], "bahn", (40, 420, 740, 470), align="left",
                               wobble=0), "e6c64f", 846, 0)
    img = paint(img, text_mask(h, w, ["Kap. Nestor B. Lualhati at mga Kagawad"], "bahn", (40, 520, 740, 566),
                               align="left", wobble=0), "c9c4b6", 847, 0)
    # An invented barangay seal: rings, a band of lettering, a sun and a star.
    cx, cy = 870, 340
    img = paint(img, circle_mask(h, w, cx, cy, 118), "ece7da", 848, 0)
    img = paint(img, circle_mask(h, w, cx, cy, 104), "2f6a4a", 849, 0)
    img = paint(img, circle_mask(h, w, cx, cy, 70), "ece7da", 850, 0)
    img = paint(img, circle_mask(h, w, cx, cy, 34), "e6c64f", 851, 0)
    img = paint(img, text_mask(h, w, ["671"], "black", (cx - 26, cy - 16, cx + 26, cy + 16), wobble=0), "6e2330", 852, 0)
    for i in range(8):                     # the sun's rays
        a = i / 8 * np.pi * 2
        img = paint(img, circle_mask(h, w, cx + 52 * np.cos(a), cy + 52 * np.sin(a), 9), "e6c64f", 853 + i, 0)
    # Grommets at the four corners, printed white rings.
    for gx, gy in ((22, 22), (w - 22, 22), (22, h - 22), (w - 22, h - 22)):
        img = paint(img, circle_mask(h, w, gx, gy, 12), "cfcac0", 870, 0)
        img = paint(img, circle_mask(h, w, gx, gy, 6), "1b1b1d", 871, 0)
    img = weather(img, 872, fade=0.08)
    save("prop_sign_tarp", img)


def prop_sign_sarisari():
    h, w = 400, 600                        # 0.6 m x 0.4 m cardboard
    img = fill(h, w, "b8956a")
    img = coat(img, (1.06, 1.05, 1.03), 80, 0.3, seed=881, feather=1.2)
    img = coat(img, (0.9, 0.88, 0.86), 60, 0.12, seed=882, feather=1.0)     # damp patches
    img = paint(img, text_mask(h, w, ["MAY LOAD"], "ink", (40, 40, w - 40, 190), seed=883, wobble=3.0, stroke=3),
                "1d1b1c", 884, 0.03)
    img = paint(img, text_mask(h, w, ["YELO  P5"], "ink", (80, 220, w - 80, 350), seed=885, wobble=3.0, stroke=3),
                "8e2222", 886, 0.03)
    save("prop_sign_sarisari", img)


def prop_sachet_strip():
    h, w = 1024, 200                       # 0.12 m x 0.6 m strip of eight sachets
    colours = ["6b3f7a", "3f7a4f", "d8b440", "9a2d2d", "5e7f7a", "e3ddd0", "6b3f7a", "3f7a4f"]
    img = np.zeros((h, w, 3))
    ph = h / 8
    for k, c in enumerate(colours):
        y0 = int(k * ph)
        img[y0:int((k + 1) * ph)] = hexcol(c)
        img = paint(img, rect_mask(h, w, (40, y0 + 34, w - 40, y0 + ph - 34), 30), "f1eee6", 890 + k, 0)
        img = paint(img, rect_mask(h, w, (60, y0 + 54, w - 60, y0 + 74), 6), c, 900 + k, 0)
        img[y0:y0 + 4] *= 0.8                 # the crimp between sachets
    img = weather(img, 910, fade=0.05, dirt_bottom=False)
    save("prop_sachet_strip", img)


def prop_pad_plate():
    S = 1024                               # 1.8 m square
    img = fill(S, S, "2d2c32")
    img = coat(img, (1.12, 1.12, 1.12), 180, 0.25, seed=921, feather=1.3)   # worn where feet land
    img = paint(img, frame_mask(S, S, (20, 20, S - 20, S - 20), 46, 60, 0), "3c3a42", 922, 0)
    yy, xx = np.mgrid[0:S, 0:S].astype(float)
    for end in (-1, 1):                    # gold chevrons at both ends, pointing along the bars
        for k in range(3):
            cy = S / 2 + end * (400 - k * 30)
            d = np.abs((yy - cy) * end + 0.45 * np.abs(xx - S / 2))
            a = np.clip((12 - np.abs(d - 0)) / 2 + 0.5, 0, 1) * (np.abs(xx - S / 2) < 170)
            img = paint(img, a, "b9923e", 923 + k, 0.03)
    img = paint(img, text_mask(S, S, ["OVERCLOCK"], "bahn", (330, 944, 694, 996), wobble=0), "b9923e", 930, 0)
    img = blobs(img, "25242a", 6, 20, seed=931, strength=0.4, wrap=False, region=(100, 100, S - 100, S - 100))
    save("prop_pad_plate", img)


TILING = [prop_laminate, prop_paint, prop_stainless, prop_tarp, prop_canvas, prop_plastic, prop_wood,
          prop_rubber, prop_concrete]
OVERLAYS = [prop_grime_top, prop_grime_foot]
ARTWORK = [prop_sign_pisonet, prop_pisonet_atlas, prop_sign_cart, prop_sign_fishball, prop_sign_aboard,
           prop_sign_backboard, prop_sign_bawal_a, prop_sign_bawal_b, prop_sign_tarp, prop_sign_sarisari,
           prop_sachet_strip, prop_pad_plate]


def swatch_sheet(version):
    """Tiling surfaces as 2 x 2 repeats (4 m), tinted as the kit uses them, then every artwork."""
    SHEETS.mkdir(parents=True, exist_ok=True)
    tints = {"prop_paint": "4f7a63", "prop_tarp": "7d7f55", "prop_canvas": "a33a3a", "prop_plastic": "e0dcd2"}
    cell, pad = 300, 16
    names = [p.__name__ for p in TILING + OVERLAYS]
    arts = [p.__name__ for p in ARTWORK]
    cols = 6
    rows_t = (len(names) + cols - 1) // cols
    rows_a = (len(arts) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (cell + pad) + pad, (rows_t + rows_a) * (cell + pad + 26) + pad), (245, 242, 235))
    draw = ImageDraw.Draw(sheet)
    for k, name in enumerate(names + arts):
        tile = Image.open(OUT / f"{name}.png").convert("RGB")
        if name in tints:
            a = np.asarray(tile) / 255 * hexcol(tints[name]) * 1.05
            tile = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
        if k < len(names) and not name.startswith("prop_grime"):
            rep = Image.new("RGB", (tile.width * 2, tile.height * 2))
            for i in range(2):
                for j in range(2):
                    rep.paste(tile, (i * tile.width, j * tile.height))
            tile = rep
        tile.thumbnail((cell, cell), Image.LANCZOS)
        r, c = divmod(k, cols) if k < len(names) else (rows_t + (k - len(names)) // cols, (k - len(names)) % cols)
        x, y = pad + c * (cell + pad), pad + r * (cell + pad + 26)
        sheet.paste(tile, (x, y))
        draw.text((x, y + tile.height + 4), name, fill=(40, 40, 40))
    path = SHEETS / f"prop_swatches_v{version}.png"
    if path.exists():
        print("[ilalim-prop-tex] sheet exists, not overwriting:", path)
        return
    sheet.save(path)
    print("[ilalim-prop-tex] sheet", path)


def main():
    for p in TILING + OVERLAYS + ARTWORK:
        p()
    version = int(sys.argv[sys.argv.index("--sheet") + 1]) if "--sheet" in sys.argv else 1
    swatch_sheet(version)


if __name__ == "__main__":
    main()
