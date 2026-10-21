"""Paint the landmark buildings' textures, in the house illustrated style (ILALIM-1.3, landmarks kit).

  py -3 tools/author_ilalim_textures_landmarks.py [--sheet N]

Writes ArtSource/ilalim/textures/land_*.png and a swatch sheet
Logs/ilalim-blender/land_swatches_vN.png (never overwritten). The buildings are modelled by
tools/author_ilalim_landmarks.py, which documents the sites and what each replaces.

Owner, 2026-09-30: "can you think of a way to make the place look more lively, more unique
building shapes etc?", then "proceed". Three landmarks give the district an identity: a condo tower
under construction with its tower crane (north of Padre Faura), an art deco corner block and a
1960s brise-soleil office block (the G. Apacible junction, down Taft to the south).

The drawing helpers (coats, patches, hand-warped lettering) are the prop kit's, imported from
tools/author_ilalim_textures_props.py, so the landmarks sit in the same hand as the rest of the map.

THE STYLE (KANTO_DESIGN_GUIDE.md section 3, LAGOON_REWORK_GUIDE.md section 2): FLAT fills, a few
LARGE patches with FEATHERED organic edges, low contrast, no grain, no noise, no streaks, no
airbrushed blur. Every surface is its own drawing, never another surface's generator.

  TILING (world or storey-aligned UVs; the model script repeats the sizes in metres):
    land_concrete_raw   4 x 4 m. Fresh grey cast concrete on the construction floors: the
                        shapes of plywood formwork panels (1.2 x 2.4 m) as faint broad fields.
    land_concrete_paint 4 x 4 m. NEUTRAL painted concrete (tinted per building): two broad
                        coats and one damp field. The deco render and the sixties fins.
    land_blockwork      2 x 2 m. Grey hollow block infill, not yet plastered: rows of blocks as
                        soft lozenges, mortar smears as broad pale fields.
    land_formwork       2.4 x 2.4 m. Plywood edge forms on the top slab: tan sheets, one soft
                        seam per sheet, a few broad concrete splashes.
    land_roof           8 x 8 m. A flat roof membrane: pale grey with broad patched fields
                        (anti-tiled in Blender, tools/ilalim_antitile.py).
    land_condo_storey   6 m x 3.1 m, one storey. The condo's finished floors: a cream wall
                        with one wide sliding window and one narrow window per 3 m bay, a
                        curtain here and there, a painted railing band on the sliding door.
    land_netting        4 m x 3.1 m, one storey and one column bay. GREEN SAFETY NETTING drawn
                        to look see-through: flat green with the slab edge and the column behind
                        it drawn as darker green shapes, and the net's seams as soft pale bands.
    land_crane          2 m x 2 m, RGBA. The crane's lattice PAINTED on chunky members: a
                        yellow chord border and one diagonal per panel; the gaps are
                        transparent (a cutout in Unity), so the jib reads against the sky.
    land_deco_ribbon    4 m x 1.6 m. The deco block's steel ribbon windows: bottle green frames,
                        horizontal glazing bars (the streamline "speed lines"), pale panes.
    land_sixties_cell   1.6 m x 3.4 m, one grid cell. What sits behind the brise-soleil: a
                        jalousie window over a mustard spandrel panel on a deep green wall.
    land_shop_bay       4 m x 3.4 m. A ground-floor shop bay: a roll shutter half up over a
                        glazed front and a counter, drawn flat.
    land_grime_drips    2 m x 3 m multiplier, v = metres below the band above (UVGrime).
    land_grime_splash   2 m x 1.5 m multiplier, v = metres above the ground (UVSplash).
  ONE-OFF artwork (UV 0..1 on one face):
    land_sign_hoarding  16 m x 2.4 m site hoarding: TANAW RESIDENCES by DALISAY LAND (both
                        invented), a painted picture of the finished tower, PRE-SELLING / STUDIO
                        1BR 2BR, a safety strip along the foot.
    land_sign_net       8 m x 4 m tarpaulin banner on the netting: DALISAY LAND / TANAW.
    land_sign_safety    1.2 m x 0.9 m gate sign: SAFETY FIRST / HARD HAT AREA / BAWAL ANG
                        HINDI TAGA-SITE.
    land_site_office    6 m x 2.6 m side of the site office container: ribbed green steel with a
                        door, a window and a stencilled SITE OFFICE.
    land_sign_amihan    1.2 m x 7 m vertical fin lettering: AMIHAN (invented), cream on rose.
    land_sign_deco      3.6 m x 0.6 m plaque: EDIFICIO AMIHAN 1939 in a deco hand.
    land_sign_sixties   9 m x 0.9 m band: MAKABAYAN BUILDING (invented) as metal letters.

ROLE HUES (Art_Direction.md section 1): nothing near offence orange #f87020 or defence blue
#0080e8. The crane is YELLOW, the netting GREEN, the plywood a dull tan, the deco accents rose and
sage, the sixties back wall deep green with mustard; no mid blue anywhere, glass is teal-grey.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
from author_ilalim_textures_props import (  # noqa: E402
    OUT, SHEETS, blobs, circle_mask, coat, drips, fill, frame_mask, hexcol, paint, patches, rect_mask,
    smooth, text_mask, weather, wobbly_lines, _noise1d,
)


def save(name, img, alpha=None):
    OUT.mkdir(parents=True, exist_ok=True)
    rgb = (np.clip(img, 0, 1) * 255).astype(np.uint8)
    if alpha is not None:
        a = (np.clip(alpha, 0, 1) * 255).astype(np.uint8)[..., None]
        Image.fromarray(np.concatenate([rgb, a], axis=2), "RGBA").save(OUT / f"{name}.png")
    else:
        Image.fromarray(rgb).save(OUT / f"{name}.png")
    print("[ilalim-land-tex]", name)


def hband(h, w, y0, y1, feather=1.5, wob=0.0, seed=0):
    """A horizontal band y0..y1 across the whole width, optionally with a gently wobbling edge."""
    yy = np.mgrid[0:h, 0:w][0].astype(float)
    if wob:
        off = wob * _noise1d(w, w / 5, seed)[None, :]
        yy = yy + off
    return np.clip((yy - y0) / feather + 0.5, 0, 1) * np.clip((y1 - yy) / feather + 0.5, 0, 1)


def vband(h, w, x0, x1, feather=1.5):
    xx = np.mgrid[0:h, 0:w][1].astype(float)
    return np.clip((xx - x0) / feather + 0.5, 0, 1) * np.clip((x1 - xx) / feather + 0.5, 0, 1)


def mix(img, mask, colour, strength=1.0):
    a = mask[..., None] * strength
    return img * (1 - a) + hexcol(colour) * a


# ------------------------------------------------------------------ tiling surfaces

def land_concrete_raw():
    n = 512                                   # 4 m
    img = fill(n, n, "b3b0a8")
    rng = np.random.default_rng(3101)
    # Formwork panels, 1.2 x 2.4 m (154 x 307 px), each cast a touch lighter or darker: broad
    # fields with soft edges, never lines.
    pw, ph = 154, 307
    for r in range(3):
        for c in range(4):
            x0 = c * pw + (r % 2) * pw / 2 - pw / 2
            y0 = r * ph - ph / 3
            m = rect_mask(n, n, (x0 + 3, y0 + 3, x0 + pw - 3, y0 + ph - 3), 10, wobble=3, seed=3102 + r * 5 + c,
                          feather=6)
            img = img * (1 + rng.uniform(-0.035, 0.035) * m[..., None])
    img = coat(img, (1.04, 1.04, 1.03), 140, 0.25, seed=3103, feather=1.2)
    img = coat(img, (0.94, 0.94, 0.935), 110, 0.14, seed=3104, feather=1.1)
    save("land_concrete_raw", img)


def land_concrete_paint():
    n = 512
    img = fill(n, n, "efece6")
    img = coat(img, (0.965, 0.96, 0.955), 150, 0.3, seed=3201, feather=1.2)
    img = coat(img, (1.02, 1.02, 1.02), 90, 0.2, seed=3202, feather=1.1)
    damp = patches(n, n, 170, 0.15, 3203, feather=1.1, stretch=(1.0, 0.45))
    img = img * (1 - 0.05 * damp[..., None])
    save("land_concrete_paint", img)


def land_blockwork():
    n = 512                                   # 2 m: blocks 0.4 x 0.2 m = 102 x 51 px
    img = fill(n, n, "9d9a93")
    bw, bh = n / 5, n / 10
    rng = np.random.default_rng(3301)
    yy, xx = np.mgrid[0:n, 0:n].astype(float)
    for r in range(10):
        for c in range(-1, 6):
            x0 = c * bw + (bw / 2 if r % 2 else 0)
            m = rect_mask(n, n, (x0 + 4, r * bh + 4, x0 + bw - 4, (r + 1) * bh - 4), 7, wobble=2.0,
                          seed=3302 + r * 7 + c, feather=2.2)
            tone = rng.uniform(1.03, 1.1)
            img = img * (1 + (tone - 1) * m[..., None])
    img = coat(img, (1.06, 1.06, 1.05), 120, 0.2, seed=3303, feather=1.2)      # mortar smears
    save("land_blockwork", img)


def land_formwork():
    n = 512                                   # 2.4 m: 1.2 m sheets
    img = fill(n, n, "b39a74")
    xx = np.mgrid[0:n, 0:n][1].astype(float)
    for x in (0, n / 2):
        a = np.clip((3 - np.abs(((xx - x + n / 2) % n) - n / 2)) / 1.5, 0, 1)
        img = img * (1 - 0.18 * a[..., None])
    img = coat(img, (1.05, 1.04, 1.03), 110, 0.3, seed=3401, feather=1.2, stretch=(0.4, 1.0))
    img = coat(img, (0.93, 0.92, 0.9), 70, 0.12, seed=3402, feather=1.0)
    splash = patches(n, n, 80, 0.08, 3403, feather=0.8)
    img = mix(img, splash, "a9a69e", 0.7)
    save("land_formwork", img)


def land_roof():
    n = 512                                   # 8 m
    img = fill(n, n, "a9a8a2")
    img = coat(img, (1.05, 1.05, 1.045), 180, 0.3, seed=3501, feather=1.3)
    img = coat(img, (0.95, 0.95, 0.945), 150, 0.2, seed=3502, feather=1.2)
    # Two squarish re-laid membrane patches.
    for k, box in enumerate(((60, 300, 220, 420), (330, 80, 470, 190))):
        img = img * (1 - 0.05 * rect_mask(n, n, box, 12, wobble=4, seed=3503 + k, feather=3)[..., None])
    save("land_roof", img)


def land_condo_storey():
    h, w = 512, 992                           # 3.1 m x 6 m at 165 px/m
    img = fill(h, w, "ebe2cc")
    img = coat(img, (1.025, 1.02, 1.01), 160, 0.3, seed=3601, feather=1.2)
    px = w / 6.0
    for bay in range(2):
        x0 = bay * 3.0 * px
        # A wide sliding door with a painted railing band, then a narrow window.
        dx0, dx1 = x0 + 0.25 * px, x0 + 1.95 * px
        dy0, dy1 = h - 2.35 * px, h - 0.2 * px
        img = paint(img, rect_mask(h, w, (dx0 - 7, dy0 - 7, dx1 + 7, dy1 + 2), 4), "d7cdb4", 3602 + bay, 0)
        img = paint(img, rect_mask(h, w, (dx0, dy0, dx1, dy1), 3), "3f5754", 3604 + bay, 0.03)
        img = paint(img, rect_mask(h, w, (dx0 + 8, dy0 + 8, (dx0 + dx1) / 2 - 3, dy1 - 6), 2), "9fb8ae", 3606 + bay, 0.04)
        img = paint(img, rect_mask(h, w, ((dx0 + dx1) / 2 + 3, dy0 + 8, dx1 - 8, dy1 - 6), 2), "7f9c92", 3608 + bay, 0.04)
        if bay == 0:   # a curtain drawn half across
            img = paint(img, rect_mask(h, w, (dx0 + 10, dy0 + 10, dx0 + 0.6 * px, dy1 - 8), 6), "d9c98f", 3610, 0.05)
        # The railing: a pale band with a dark top rail, drawn across the lower door.
        ry = h - 1.0 * px
        img = paint(img, rect_mask(h, w, (dx0 - 10, ry, dx1 + 10, dy1 + 2), 2), "cfc6ae", 3611 + bay, 0.02)
        img = paint(img, rect_mask(h, w, (dx0 - 14, ry - 8, dx1 + 14, ry + 4), 3), "5e5a52", 3613 + bay, 0)
        for k in range(1, 9):
            x = dx0 - 10 + (dx1 - dx0 + 20) * k / 9
            img = paint(img, rect_mask(h, w, (x - 3, ry, x + 3, dy1), 1), "8c877b", 3615, 0)
        # The narrow window, with an aircon box under it.
        wx0, wx1 = x0 + 2.25 * px, x0 + 2.8 * px
        wy0, wy1 = h - 2.3 * px, h - 1.1 * px
        img = paint(img, rect_mask(h, w, (wx0 - 6, wy0 - 6, wx1 + 6, wy1 + 10), 3), "d7cdb4", 3620 + bay, 0)
        img = paint(img, rect_mask(h, w, (wx0, wy0, wx1, wy1), 2), "3f5754", 3622 + bay, 0.03)
        img = paint(img, rect_mask(h, w, (wx0 + 6, wy0 + 6, wx1 - 6, wy1 - 6), 2), "a6bdb2", 3624 + bay, 0.04)
        img = paint(img, rect_mask(h, w, (wx0 - 2, wy1 + 22, wx1 + 2, wy1 + 70), 5), "ddd8ca", 3626 + bay, 0)
        img = paint(img, rect_mask(h, w, (wx0 + 8, wy1 + 32, wx1 - 8, wy1 + 60), 2), "8e8a80", 3628 + bay, 0)
    save("land_condo_storey", img)


def land_netting():
    h, w = 400, 516                           # 3.1 m x 4 m at 129 px/m
    img = fill(h, w, "4f7d4a")
    px = w / 4.0
    # What shows through the net: the slab edge (a band 0.25 m tall at the storey's foot) and
    # the column at the bay's edge, drawn as darker green shapes.
    slab = hband(h, w, h - 0.34 * px, h - 0.02 * px, feather=4, wob=3, seed=3701)
    img = mix(img, slab, "34563a", 0.8)
    col = vband(h, w, -0.25 * px, 0.25 * px, feather=5) + vband(h, w, w - 0.25 * px, w + 0.25 * px, feather=5)
    img = mix(img, np.clip(col, 0, 1), "3a5f40", 0.7)
    # A dim interior: a broad darker field in the middle of the bay.
    img = mix(img, patches(h, w, 90, 0.25, 3702, feather=1.2), "446f42", 0.5)
    # The net's seams: soft pale bands every 1.03 m (three widths of net per storey) and the
    # lighter sun-faced folds, broad and vertical.
    for k in range(1, 3):
        y = k * h / 3
        img = mix(img, hband(h, w, y - 3, y + 3, feather=2.5, wob=1.2, seed=3703 + k), "6f9764", 0.5)
    img = coat(img, (1.08, 1.08, 1.05), 150, 0.3, seed=3706, feather=1.3, stretch=(0.35, 1.0))
    save("land_netting", img)


def land_crane():
    n = 256                                   # 2 m x 2 m panel
    img = fill(n, n, "d9b43a")
    img = coat(img, (1.05, 1.04, 1.0), 60, 0.3, seed=3801, feather=1.2)
    yy, xx = np.mgrid[0:n, 0:n].astype(float)
    chord = 30
    edge = np.maximum(np.clip((chord - np.minimum(xx, n - 1 - xx)) / 1.5, 0, 1),
                      np.clip((chord * 0.7 - np.minimum(yy, n - 1 - yy)) / 1.5, 0, 1))
    d = np.abs(xx - yy) / np.sqrt(2)
    diag = np.clip((15 - d) / 1.5, 0, 1)
    solid = np.clip(edge + diag, 0, 1)
    # Shading: the chords get a darker inner edge so the members read as square tubes.
    inner = np.clip((chord - np.minimum(xx, n - 1 - xx)) / 1.5, 0, 1) - np.clip((chord - 9 - np.minimum(xx, n - 1 - xx)) / 1.5, 0, 1)
    img = img * (1 - 0.18 * np.clip(inner, 0, 1)[..., None])
    img = mix(img, blobs(np.zeros((n, n, 3)), "ffffff", 3, 7, 3802)[..., 0], "8d6a3c", 0.5)   # rust spots
    save("land_crane", img, alpha=solid)


def land_deco_ribbon():
    h, w = 256, 640                           # 1.6 m x 4 m at 160 px/m
    img = fill(h, w, "375248")
    # Pale panes between horizontal glazing bars (three per ribbon) and vertical mullions every 1 m.
    bars = [0.0, 0.42, 0.84, 1.26, 1.6]
    for a, b in zip(bars, bars[1:]):
        for k in range(4):
            x0, x1 = k * 160 + 12, (k + 1) * 160 - 12
            y0, y1 = h - b * 160 + 9, h - a * 160 - 9
            tone = ("a8c0b4", "9ab4a8", "b3c8bc", "a0b9ac")[(k + int(a * 10)) % 4]
            img = paint(img, rect_mask(h, w, (x0, y0, x1, y1), 2), tone, 3901 + k, 0.03)
    # A reflection: one broad pale field across the panes, and curtains in two windows.
    img = coat(img, (1.08, 1.08, 1.06), 110, 0.25, seed=3905, feather=1.2)
    for x0 in (172, 494):
        m = rect_mask(h, w, (x0, 18, x0 + 70, h - 18), 6) * 0.6
        img = mix(img, m, "cdb98f")
    save("land_deco_ribbon", img)


def land_sixties_cell():
    h, w = 512, 240                           # 3.4 m x 1.6 m at 150 px/m
    img = fill(h, w, "3e5f52")
    img = coat(img, (1.05, 1.05, 1.04), 70, 0.25, seed=4001, feather=1.1)
    px = 150
    # Mustard spandrel panel at the foot (0..1.0 m), a jalousie window above (1.0..2.9 m).
    img = paint(img, rect_mask(h, w, (10, h - 1.0 * px + 6, w - 10, h - 6), 4), "c9a64a", 4002, 0.04)
    wy0, wy1 = h - 2.95 * px, h - 1.1 * px
    img = paint(img, rect_mask(h, w, (18, wy0, w - 18, wy1), 3), "5d615c", 4003, 0)
    n = 11
    yy = np.mgrid[0:h, 0:w][0].astype(float)
    for k in range(n):
        y0 = wy0 + 6 + k * (wy1 - wy0 - 12) / n
        y1 = y0 + (wy1 - wy0 - 12) / n - 5
        img = paint(img, rect_mask(h, w, (24, y0, w - 24, y1), 2), ("bcc5bb", "aeb9af")[k % 2], 4004 + k, 0)
    img = paint(img, rect_mask(h, w, (w / 2 - 3, wy0, w / 2 + 3, wy1), 0), "5d615c", 4020, 0)
    save("land_sixties_cell", img)


def land_shop_bay():
    h, w = 512, 600                           # 3.4 m x 4 m at 150 px/m
    img = fill(h, w, "3d4744")
    img = coat(img, (1.08, 1.08, 1.06), 90, 0.3, seed=4101, feather=1.2)
    # The shutter rolled half down over the top of the opening: a pale ribbed band.
    sh = h * 0.42
    img = paint(img, rect_mask(h, w, (0, 0, w, sh), 0), "c3c1b8", 4102, 0.03)
    yy = np.mgrid[0:h, 0:w][0].astype(float)
    ribs = (np.abs((yy % 16) - 8) < 1.5) * (yy < sh)
    img = img * (1 - 0.1 * ribs[..., None])
    img = paint(img, rect_mask(h, w, (0, sh - 10, w, sh + 6), 2), "8f8c83", 4103, 0)
    # Shelves and goods inside, drawn as flat blocks, and a counter.
    rng = np.random.default_rng(4104)
    for row, y in enumerate((sh + 70, sh + 150)):
        x = 30.0
        while x < w - 60:
            bw = rng.uniform(30, 70)
            col = ("c9a64a", "a8c0b4", "b24a40", "e0d7c0", "5d7f55", "7a3f6e")[rng.integers(6)]
            img = paint(img, rect_mask(h, w, (x, y - rng.uniform(30, 55), x + bw, y), 4), col, int(x), 0.04)
            x += bw + rng.uniform(8, 20)
        img = paint(img, rect_mask(h, w, (20, y, w - 20, y + 8), 1), "6e5a44", 4105 + row, 0)
    img = paint(img, rect_mask(h, w, (40, h - 110, w - 40, h), 4), "b7a88a", 4108, 0.04)
    save("land_shop_bay", img)


def land_grime_drips():
    W, H = 256, 384                           # 2 m x 3 m, row 0 at the band above
    img = np.ones((H, W, 3))
    v = np.mgrid[0:H, 0:W][0].astype(float)
    edge = 14 + 6 * _noise1d(W, 40, 4201)[None, :]
    img *= 1 - 0.12 * np.clip((edge - v) / 6 + 0.5, 0, 1)[..., None]
    img = drips(img, list(np.random.default_rng(4202).uniform(0, W, 5)), 6, 150, 9, "8f8a80", seed=4203,
                strength=0.3)
    save("land_grime_drips", np.flipud(img))


def land_grime_splash():
    W, H = 256, 192                           # 2 m x 1.5 m, v up from the ground
    img = np.ones((H, W, 3))
    v, u = np.mgrid[0:H, 0:W].astype(float)
    top = 50 + 14 * _noise1d(W, 30, 4301)[None, :]
    band = np.clip((top - v) / 10 + 0.5, 0, 1)
    img = img * (1 - band[..., None] * (1 - hexcol("bdb4a5")))
    save("land_grime_splash", np.flipud(img))


# ------------------------------------------------------------------ one-off artwork

def land_sign_hoarding():
    h, w = 300, 2000                          # 16 m x 2.4 m at 125 px/m
    img = fill(h, w, "e9e3d2")
    img = coat(img, (1.02, 1.02, 1.01), 200, 0.3, seed=4401, feather=1.2)
    # A deep green top band and a safety strip at the foot (yellow and charcoal, broad).
    img = paint(img, rect_mask(h, w, (-5, -5, w + 5, 40), 0), "2e5b43", 4402, 0.03)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    strip = (yy > h - 34)
    diag = ((xx + yy) % 80) < 40
    img = mix(img, (strip & diag).astype(float), "d6b33c")
    img = mix(img, (strip & ~diag).astype(float), "3b3a37")
    # The painted tower: a pale rounded slab rising behind palm shapes, teal-grey glass bands.
    tx0, tx1 = 1560, 1760
    img = paint(img, rect_mask(h, w, (tx0, 58, tx1, h - 40), 14), "d9d2bf", 4403, 0)
    for k in range(9):
        y = 72 + k * 21
        img = paint(img, rect_mask(h, w, (tx0 + 14, y, tx1 - 14, y + 11), 3), "6f8f86", 4404 + k, 0)
    img = paint(img, rect_mask(h, w, (tx0 - 60, h - 70, tx1 + 90, h - 40), 8), "b9c79a", 4420, 0)
    for k, cx in enumerate((1500, 1810, 1860)):
        img = paint(img, circle_mask(h, w, cx, h - 92, 34), "5f8a4d", 4421 + k, 0.05)
        img = paint(img, rect_mask(h, w, (cx - 4, h - 90, cx + 4, h - 40), 1), "6e5a44", 4425 + k, 0)
    img = paint(img, text_mask(h, w, ["TANAW"], "black", (90, 54, 900, 170), seed=4430, wobble=0.8),
                "2e5b43", 4431, 0.02)
    img = paint(img, text_mask(h, w, ["RESIDENCES"], "bahn", (96, 170, 900, 214), seed=4432, wobble=0.6),
                "2e5b43", 4433, 0.02)
    img = paint(img, text_mask(h, w, ["a DALISAY LAND community"], "bahn", (98, 222, 700, 256), seed=4434,
                               wobble=0.6, align="left"), "6b6457", 4435, 0)
    img = paint(img, text_mask(h, w, ["PRE-SELLING", "STUDIO · 1BR · 2BR"], "black", (980, 70, 1420, 230),
                               seed=4436, wobble=0.8, spacing=0.3), "a63a33", 4437, 0.02)
    img = paint(img, text_mask(h, w, ["DALISAY LAND"], "bahn", (1100, 6, 1500, 34), seed=4438, wobble=0.4),
                "e9e3d2", 4439, 0)
    img = weather(img, 4440, fade=0.05)
    save("land_sign_hoarding", img)


def land_sign_net():
    h, w = 400, 800                           # 8 m x 4 m
    img = fill(h, w, "f0ece0")
    img = coat(img, (0.97, 0.97, 0.96), 120, 0.3, seed=4501, feather=1.2)
    img = paint(img, rect_mask(h, w, (0, 0, w, 90), 0), "2e5b43", 4502, 0.03)
    img = paint(img, text_mask(h, w, ["DALISAY LAND"], "bahn", (60, 14, w - 60, 78), seed=4503, wobble=0.6),
                "f0ece0", 4504, 0)
    img = paint(img, text_mask(h, w, ["TANAW"], "black", (60, 110, w - 60, 290), seed=4505, wobble=0.8),
                "2e5b43", 4506, 0.02)
    img = paint(img, text_mask(h, w, ["RESIDENCES · TAFT"], "bahn", (100, 300, w - 100, 360), seed=4507,
                               wobble=0.6), "a63a33", 4508, 0)
    # Eyelets along the edges and the tarp's sag shading.
    for x in np.linspace(24, w - 24, 9):
        img = paint(img, circle_mask(h, w, x, 10, 6), "9a9a92", int(x), 0)
        img = paint(img, circle_mask(h, w, x, h - 10, 6), "9a9a92", int(x) + 1, 0)
    img = weather(img, 4509, fade=0.06)
    save("land_sign_net", img)


def land_sign_safety():
    h, w = 270, 360                           # 1.2 m x 0.9 m
    img = fill(h, w, "f1eee4")
    img = paint(img, rect_mask(h, w, (0, 0, w, 70), 0), "2e6a45", 4601, 0.02)
    img = paint(img, text_mask(h, w, ["SAFETY FIRST"], "black", (20, 12, w - 20, 60), seed=4602, wobble=0.6),
                "f1eee4", 4603, 0)
    img = paint(img, text_mask(h, w, ["HARD HAT AREA"], "black", (20, 86, w - 20, 140), seed=4604, wobble=0.6),
                "2b2626", 4605, 0)
    img = paint(img, text_mask(h, w, ["BAWAL ANG", "HINDI TAGA-SITE"], "bahn", (30, 160, w - 30, 250),
                               seed=4606, wobble=0.6), "a63a33", 4607, 0)
    img = weather(img, 4608)
    save("land_sign_safety", img)


def land_site_office():
    h, w = 260, 600                           # 2.6 m x 6 m at 100 px/m
    img = fill(h, w, "6f8f5c")
    img = coat(img, (1.05, 1.05, 1.03), 120, 0.3, seed=4701, feather=1.2)
    xx = np.mgrid[0:h, 0:w][1].astype(float)
    ribs = np.abs(((xx) % 30) - 15) < 4
    img = img * (1 - 0.1 * ribs[..., None])
    img = paint(img, rect_mask(h, w, (60, 50, 150, h - 8), 3), "5a744a", 4702, 0.03)            # door
    img = paint(img, circle_mask(h, w, 138, 150, 5), "d9d2bf", 4703, 0)
    img = paint(img, rect_mask(h, w, (230, 60, 400, 150), 3), "e7e2d4", 4704, 0)                 # window
    img = paint(img, rect_mask(h, w, (240, 70, 390, 140), 2), "5d6b66", 4705, 0.04)
    img = paint(img, rect_mask(h, w, (250, 76, 300, 134), 2), "cdb98f", 4706, 0.04)              # curtain
    img = paint(img, text_mask(h, w, ["SITE OFFICE"], "black", (430, 70, 580, 120), seed=4707, wobble=1.2),
                "ece6d8", 4708, 0.02)
    img = weather(img, 4709, fade=0.06)
    img = drips(img, [120, 330, 520], 0, 60, 8, "5f5a4a", seed=4710, strength=0.3)
    save("land_site_office", img)


def land_sign_amihan():
    h, w = 1400, 240                          # 7 m x 1.2 m
    img = fill(h, w, "c79d92")
    img = coat(img, (1.03, 1.02, 1.02), 160, 0.3, seed=4801, feather=1.2)
    img = paint(img, frame_mask(h, w, (14, 14, w - 14, h - 14), 12, 20), "efe6d2", 4802, 0)
    letters = "AMIHAN"
    step = (h - 120) / len(letters)
    for k, ch in enumerate(letters):
        y0 = 60 + k * step
        img = paint(img, text_mask(h, w, [ch], "bahn", (40, y0 + 12, w - 40, y0 + step - 12), seed=4803 + k,
                                   wobble=0.5), "f3ecd9", 4810 + k, 0)
    img = weather(img, 4820, fade=0.05)
    save("land_sign_amihan", img)


def land_sign_deco():
    h, w = 150, 900                           # 3.6 m x 0.6 m
    img = fill(h, w, "e9e0c9")
    img = paint(img, frame_mask(h, w, (8, 8, w - 8, h - 8), 8, 14), "8a5b56", 4901, 0)
    img = paint(img, text_mask(h, w, ["EDIFICIO AMIHAN"], "bahn", (60, 26, w - 60, 100), seed=4902, wobble=0.4),
                "3d5a4a", 4903, 0)
    img = paint(img, text_mask(h, w, ["1939"], "bahn", (380, 104, 520, 134), seed=4904, wobble=0.4),
                "8a5b56", 4905, 0)
    img = weather(img, 4906)
    save("land_sign_deco", img)


def land_sign_sixties():
    h, w = 90, 900                            # 9 m x 0.9 m
    img = fill(h, w, "e3e0d7")
    img = coat(img, (0.97, 0.97, 0.96), 120, 0.3, seed=5001, feather=1.2)
    m = text_mask(h, w, ["MAKABAYAN BUILDING"], "bahn", (40, 14, w - 40, 76), seed=5002, wobble=0.3)
    # Metal letters: a dark bronze face with a soft cast shadow down-right.
    sh = np.roll(np.roll(m, 4, axis=0), 4, axis=1)
    img = mix(img, sh, "a9a497", 0.8)
    img = paint(img, m, "5b4a38", 5003, 0.04)
    img = weather(img, 5004)
    save("land_sign_sixties", img)


TILING = [land_concrete_raw, land_concrete_paint, land_blockwork, land_formwork, land_roof, land_condo_storey,
          land_netting, land_crane, land_deco_ribbon, land_sixties_cell, land_shop_bay, land_grime_drips,
          land_grime_splash]
ARTWORK = [land_sign_hoarding, land_sign_net, land_sign_safety, land_site_office, land_sign_amihan, land_sign_deco,
           land_sign_sixties]


def swatch_sheet(version):
    SHEETS.mkdir(parents=True, exist_ok=True)
    path = SHEETS / f"land_swatches_v{version}.png"
    if path.exists():
        print("[ilalim-land-tex] sheet exists, not overwriting:", path)
        return
    tints = {"land_concrete_paint": "e9dfc6"}
    names = [p.__name__ for p in TILING + ARTWORK]
    cell, pad, cols = 300, 16, 5
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (cell + pad) + pad, rows * (cell + pad + 26) + pad), (245, 242, 235))
    draw = ImageDraw.Draw(sheet)
    for k, name in enumerate(names):
        src = Image.open(OUT / f"{name}.png")
        tile = Image.new("RGB", src.size, (150, 185, 220))            # sky behind cutouts
        tile.paste(src.convert("RGB"), mask=src.split()[3] if src.mode == "RGBA" else None)
        if name in tints:
            a = np.asarray(tile) / 255 * hexcol(tints[name]) * 1.05
            tile = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
        if k < len(TILING):
            rep = Image.new("RGB", (tile.width * 2, tile.height * 2))
            for i in range(2):
                for j in range(2):
                    rep.paste(tile, (i * tile.width, j * tile.height))
            tile = rep
        tile.thumbnail((cell, cell), Image.LANCZOS)
        r, c = divmod(k, cols)
        x, y = pad + c * (cell + pad), pad + r * (cell + pad + 26)
        sheet.paste(tile, (x, y))
        draw.text((x, y + tile.height + 4), name, fill=(40, 40, 40))
    sheet.save(path)
    print("[ilalim-land-tex] sheet", path)


def main():
    for p in TILING + ARTWORK:
        p()
    version = int(sys.argv[sys.argv.index("--sheet") + 1]) if "--sheet" in sys.argv else 1
    swatch_sheet(version)


if __name__ == "__main__":
    main()
