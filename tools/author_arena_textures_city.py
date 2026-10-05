"""Paint the arena's CITY kit textures, in the house illustrated style (ARENA-1.4, city kit).

  py -3 tools/author_arena_textures_city.py [--sheet N]

Writes Assets/TumbangPreso/Art/Arena/Textures/arena_city_<surface>.png (and _emit.png where the
surface glows), a UV checker and a swatch sheet in Logs/arena/city/. The models are built by
tools/author_arena_city.py, which imports the constants at the top of this file (TILE_M, SIGNS,
FX, TRIM) and nothing else, so everything above `_lazy()` must stay free of PIL and scipy.

Owner, 2026-10-05, of the blockout's procedural window grids: "i dont want any buildings to look
like that in the final design", and "theres a few stretched out textures on the buildings".

THE STYLE (docs/KANTO_DESIGN_GUIDE.md section 3, docs/ARENA_ART_BRIEF.md): FLAT fills, a few LARGE
patches with FEATHERED organic edges, no grain, no streaks, no cracks. Windows are PAINTED shapes
with soft irregular edges and a few chosen colours; lit ones come in clusters and rows that
suggest floors and rooms. Night is carried by the emission image, not by dark albedo alone.
Every surface is its own drawing, written here as its own function:

  TILING facades (world-scale UVs; a tile is TILE_M[surface] metres, a floor 5.33 m, a bay 8 m):
    glass_a   OPISINA. An indigo curtain wall: a spandrel band and a row of sixteen panes per
              floor. Whole office floors are lit in long runs, cool white and pale cyan.
    glass_b   SALAMIN. A teal megaframe: painted pilasters every bay, tall glass slots three
              floors high between them. Lift lobbies and stairs are lit in VERTICAL runs, mint
              and amber.
    resi      TIRAHAN. A condominium in dusk-violet plaster: per bay a balcony door with its
              slab and rail, and a small window with an aircon box. Lit by the flat (two bays),
              warm yellow, a television's cyan, the odd pink room; some curtains half drawn.
    bands     PAHALANG. Rows only: a glowing glazing strip and a dark spandrel per floor, the
              strip lit in long soft-ended runs. For round and tapered bodies, where columns
              would be cut by every facet.
    far       MALAYO. The far towers and the low city: hazed indigo, soft window lights in loose
              rows, low contrast.
    base      Every tower's lower shaft: rows of light thinning out and the wall dissolving
              into the haze colour toward the foot (v 0 at the city floor, 1 at the sky lobby).
    metal     BAKAL. Dark blue-grey painted panels: piers, fins, ledges, crowns, plant, craft.
    roofs     BUBONG. Rooftops from above: plant rooms, tanks, lit skylights, a landing pad.
  ONE-OFF artwork:
    floor     the city floor, 4096 m across: the rotunda under the shaft (drawn as the game's
              chalk ring), radial avenues, ring roads, a hand-wobbled street grid, lit
              districts, all fading to the haze colour at the rim.
    trim      eight LED strips (TRIM): UVs pick one row.
    signs     the sign atlas (SIGNS): nine hand-named signs.
    fx        the hologram atlas (FX): the TUMP logo as a hologram, a scanline band, a soft glow.
    haze      a tileable layer of haze wisps (alpha).
    sky       the night sky panorama, 4096 x 2048, u = compass bearing / 360 (0 north, clockwise),
              v from nadir to zenith, and sky_cloudform, the same clouds encoded for
              TumbangPreso/NeighbourhoodSky's _CloudMap.

ROLE HUES: nothing on a large surface near offence orange #f87020 or defence blue #0080e8. Blues
here are deep navy and indigo or go to white and cyan; LED blue is #0a1a9a.
"""
import os
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = Path(os.environ.get("ARENA_TEX_OUT", ROOT / "Assets" / "TumbangPreso" / "Art" / "Arena" / "Textures"))
LOGS = ROOT / "Logs" / "arena" / "city"
LOGO = ROOT / "Assets" / "TumbangPreso" / "Art" / "ui" / "brand" / "tump_logo.png"

# ------------------------------------------------------------------ shared with the model author
FLOOR_M = 16.0 / 3.0                      # one painted floor
BAY_M = 8.0                               # one painted bay
TILE_M = {"glass_a": 64.0, "glass_b": 64.0, "resi": 64.0, "bands": 64.0, "far": 96.0, "metal": 32.0,
          "roofs": 128.0, "haze": 1400.0}
FLOOR_SPAN = 4096.0                       # the floor painting covers this many metres, centred on the can
HAZE = "2f2c74"                           # what the depths dissolve into; the sky below the horizon is this too
HAZE_EMIT = "211f58"

# Eight LED strips in the trim texture, bottom row first: UV v picks one.
TRIM = ("led_blue", "white", "cyan", "magenta", "amber", "red", "mint", "dark")
TRIM_HEX = {"led_blue": "0a1a9a", "white": "c4d0ea", "cyan": "58c4da", "magenta": "b8478e", "amber": "e0b25c",
            "red": "d8404c", "mint": "7ad8b4", "dark": "10142a"}

# The sign atlas, 2048 x 2048. Every sign is HAND-NAMED (KANTO_DESIGN_GUIDE 11.1): an invented
# business, named the way Manila names them, Tagalog and Taglish, no real brand. `box` is its
# rectangle in image pixels (x0, y0, x1, y1), top-left origin; `size` its face in metres.
SIGN_ATLAS = 2048
SIGNS = {
    "liga":      dict(size=(36.0, 20.0), box=(0, 0, 936, 520)),        # LIGA NG TUMBANG PRESO, the logo
    "sinag":     dict(size=(34.0, 14.0), box=(952, 0, 1836, 364)),     # SINAG ENERHIYA
    "bahaghari": dict(size=(30.0, 12.0), box=(952, 380, 1732, 692)),   # BAHAGHARI TELEKOM
    "isko":      dict(size=(28.0, 16.0), box=(0, 536, 728, 952)),      # MANG ISKO HOVER VULCANIZING
    "dyip":      dict(size=(40.0, 10.0), box=(744, 708, 1784, 968)),   # DYIP-LIPAD TERMINAL
    "kape":      dict(size=(22.0, 22.0), box=(0, 968, 572, 1540)),     # KAPE NI KA INGGO
    "dely":      dict(size=(26.0, 10.0), box=(588, 984, 1264, 1244)),  # ALING DELY SARI-SARI
    "pansitan":  dict(size=(12.0, 44.0), box=(1280, 984, 1568, 2040)),  # PANSITAN SA ULAP
    "halo":      dict(size=(14.0, 36.0), box=(1584, 984, 1920, 1848)),  # HALO-HALO HOLO
}
# The hologram atlas, 1024 x 1024, image pixels.
FX_ATLAS = 1024
FX = {"logo": (0, 0, 1024, 674), "scan": (0, 700, 1024, 828), "glow": (0, 832, 192, 1024),
      "beam": (208, 832, 800, 1024), "halo": (816, 832, 1008, 1024)}


def atlas_uv(box, size):
    """(u0, v0, u1, v1) for an image-pixel box, v up."""
    x0, y0, x1, y1 = box
    return x0 / size, 1.0 - y1 / size, x1 / size, 1.0 - y0 / size


# ------------------------------------------------------------------ painting helpers (lazy imports)
S = 1024


def _lazy():
    global Image, ImageDraw, ImageFont, ndimage
    from PIL import Image, ImageDraw, ImageFont
    from scipy import ndimage


def hexcol(h):
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])


def smooth(h, w, scale_px, seed, stretch=(1.0, 1.0)):
    """Periodic smooth noise, features about `scale_px` across, unit variance. Tiles exactly."""
    white = np.random.default_rng(seed).standard_normal((h, w))
    fy = np.fft.fftfreq(h)[:, None] * stretch[1]
    fx = np.fft.fftfreq(w)[None, :] * stretch[0]
    n = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-(fx ** 2 + fy ** 2) * scale_px * scale_px * 2)))
    return (n - n.mean()) / (n.std() + 1e-9)


def patch(h, w, scale_px, coverage, seed, feather=0.6, stretch=(1.0, 1.0)):
    """A mask of large organic patches covering `coverage` of the image, feathered."""
    n = smooth(h, w, scale_px, seed, stretch) + 0.12 * smooth(h, w, scale_px / 2.5, seed + 1, stretch)
    n = (n - n.mean()) / n.std()
    m = np.clip((n - np.quantile(n, 1 - coverage)) / feather + 0.5, 0, 1)
    return m * m * (3 - 2 * m)


def fill(h, w, colour):
    return np.ones((h, w, 3)) * hexcol(colour)


def lay(img, mask, colour):
    c = hexcol(colour) if isinstance(colour, str) else np.asarray(colour)
    return img * (1 - mask[..., None]) + c * mask[..., None]


def coat(img, colour, scale_px, coverage, seed, strength=1.0, feather=0.6, stretch=(1.0, 1.0)):
    """A second coat of another colour over part of the surface: the style's big soft patch."""
    return lay(img, patch(img.shape[0], img.shape[1], scale_px, coverage, seed, feather, stretch) * strength, colour)


def grid(nx, ny, wobble_px, seed, h=S, w=S):
    """Cells of a hand-drawn grid: for every pixel its cell (ix, iy) and its position inside the
    cell in pixels from the cell's centre (lx right, ly down), the whole field warped by a smooth
    periodic noise so no edge is ruled. iy counts floors from the TOP of the image."""
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    xx = xx + wobble_px * smooth(h, w, 46, seed)
    yy = yy + wobble_px * smooth(h, w, 46, seed + 1)
    cw, ch = w / nx, h / ny
    ix = np.floor(xx / cw).astype(int) % nx
    iy = np.floor(yy / ch).astype(int) % ny
    return ix, iy, (xx % cw) - cw / 2, (yy % ch) - ch / 2


def box_mask(lx, ly, cx, cy, hx, hy, r, feather=1.6):
    """A rounded rectangle drawn in cell coordinates. cx, cy, hx, hy may be per-pixel arrays."""
    qx, qy = np.abs(lx - cx) - (hx - r), np.abs(ly - cy) - (hy - r)
    d = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r
    return np.clip(-d / feather + 0.5, 0, 1)


def table(ny, nx, seed, lo=0.0, hi=1.0):
    return np.random.default_rng(seed).uniform(lo, hi, (ny, nx))


def clusters(ny, nx, seed, scale=(2.2, 1.2)):
    """A smooth periodic field sampled per cell, so neighbours agree: lights come in groups.
    `scale` is the group size in cells (across, down)."""
    white = np.random.default_rng(seed).standard_normal((ny, nx))
    fy = np.fft.fftfreq(ny)[:, None] * scale[1]
    fx = np.fft.fftfreq(nx)[None, :] * scale[0]
    n = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-(fx ** 2 + fy ** 2) * 18)))
    return (n - n.mean()) / (n.std() + 1e-9)


def halo(emit, sigma, gain):
    """The painted glow round a lit shape: a soft wash, drawn once, tiling."""
    return emit + ndimage.gaussian_filter(emit, (sigma, sigma, 0), mode="wrap") * gain


# How much of its own colour a facade gives off, so night never turns it black. 0.22 until the owner
# played the map (2026-10-05, "buildings should also be more visible"): the towers read as near-black
# slabs with sparse windows. 0.55 lifts every wall off the sky; `band()` adds a lit floor per tile.
NIGHT = 0.55


def night(img, emit):
    return img * NIGHT + emit


def band(emit, ly, colour, centre, half, strength=0.85, rows=None, iy=None):
    """A LIT FLOOR BAND: one soft strip of light across the whole tile at `centre` (cell pixels from a
    floor's middle), on the floors `rows` (None: every floor). A tile is twelve floors, so a tower
    carries one every 64 m: the edge lighting that lets a silhouette be read against the night."""
    m = np.clip((half - np.abs(ly - centre)) / 1.6 + 0.5, 0, 1)
    if rows is not None:
        m = m * np.isin(iy, rows)
    return np.maximum(emit, m[..., None] * hexcol(colour) * strength)


def save(name, img, alpha=None):
    OUT.mkdir(parents=True, exist_ok=True)
    a = (np.clip(img, 0, 1) * 255).astype(np.uint8)
    if alpha is not None:
        a = np.dstack([a, (np.clip(alpha, 0, 1) * 255).astype(np.uint8)])
    Image.fromarray(a).save(OUT / ("arena_city_%s.png" % name))
    print("[arena-city-tex]", name, a.shape)


FONTS = Path("C:/Windows/Fonts")
FONT_FILES = {"impact": ["impact.ttf", "ariblk.ttf"], "black": ["ariblk.ttf", "impact.ttf"],
              "print": ["segoeprb.ttf", "segoepr.ttf", "arialbd.ttf"], "ink": ["Inkfree.ttf", "segoepr.ttf"],
              "bahn": ["bahnschrift.ttf", "arialbd.ttf"]}


def font(key, size):
    for f in FONT_FILES[key]:
        if (FONTS / f).exists():
            return ImageFont.truetype(str(FONTS / f), size)
    return ImageFont.load_default()


def lettering(h, w, lines, key, box, seed, wobble=1.6, lean=0.0, vertical=False, spacing=0.14):
    """`lines` fitted inside `box`, then warped by a smooth field so the strokes wobble like a
    signwriter's hand. `vertical` stacks one letter per line (a blade sign)."""
    if vertical:
        lines = [c for c in " ".join(lines) if c != " "] if len(lines) == 1 else lines
    K = 2
    H, W = h * K, w * K
    x0, y0, x1, y1 = (v * K for v in box)
    size = 600
    while size > 6:
        f = font(key, size)
        dims = [f.getbbox(t) for t in lines]
        widths = [d[2] - d[0] for d in dims]
        heights = [d[3] - d[1] for d in dims]
        total = sum(heights) + spacing * size * (len(lines) - 1)
        if max(widths) <= x1 - x0 and total <= y1 - y0:
            break
        size = int(size * 0.95)
    im = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(im)
    y = y0 + (y1 - y0 - total) / 2
    for t, dm, tw, th in zip(lines, dims, widths, heights):
        d.text((x0 + (x1 - x0 - tw) / 2 - dm[0], y - dm[1]), t, font=f, fill=255)
        y += th + spacing * size
    m = np.asarray(im, dtype=float) / 255
    yy, xx = np.mgrid[0:H, 0:W].astype(float)
    dx = smooth(H, W, 26 * K, seed) * wobble * K + lean * (yy - H / 2)
    dy = smooth(H, W, 26 * K, seed + 1) * wobble * K
    m = ndimage.map_coordinates(m, [yy + dy, xx + dx], order=1, mode="constant")
    m = np.asarray(Image.fromarray((np.clip(m, 0, 1) * 255).astype(np.uint8)).resize((w, h), Image.LANCZOS), dtype=float) / 255
    m = ndimage.gaussian_filter(m, 0.7)
    return np.clip((m - 0.5) * 1.7 + 0.5, 0, 1)


# ------------------------------------------------------------------ facades

def glass_a():
    """OPISINA: sixteen panes and a spandrel per floor; office floors lit in long runs."""
    nx, ny = 16, 12
    img = fill(S, S, "39447a")                                    # the frame and spandrel colour
    img = coat(img, "444f8a", 260, 0.35, 101)
    img = coat(img, "313a6c", 200, 0.22, 103)
    ix, iy, lx, ly = grid(nx, ny, 2.2, 110)
    jw = table(ny, nx, 111, -3.0, 3.0)[iy, ix]                    # every pane a little different
    jh = table(ny, nx, 112, -2.5, 2.5)[iy, ix]
    pane = box_mask(lx, ly, 0.0, -11.0, 26.0 + jw, 28.0 + jh, 6.0)   # the spandrel is the dark band left below
    glass = fill(S, S, "222a58")
    glass = coat(glass, "2c3670", 180, 0.4, 113, feather=0.9)     # the sky's glow lying on the glass, in big fields
    glass = coat(glass, "1b224a", 150, 0.25, 114)
    # A flat reflection wedge on one pane in five: a drawn highlight, never a streak.
    wedge = np.clip((lx * 0.8 - ly - 6.0) / 3.0, 0, 1) * (table(ny, nx, 115)[iy, ix] > 0.8)
    glass = lay(glass, wedge * 0.5, "3d4888")
    img = lay(img, pane, glass)
    # Lit floors: a floor is lit in runs, and neighbouring floors tend to agree.
    run = clusters(ny, nx, 120, scale=(4.0, 0.35)) + 0.9 * np.random.default_rng(127).standard_normal((ny, 1))
    lit = (run > 0.45) & (table(ny, nx, 122) > 0.15)             # the odd dark pane in a lit floor
    lit |= table(ny, nx, 123) > 0.975                             # a cleaner's light, alone
    tone = clusters(ny, nx, 124, scale=(6.0, 1.5))
    cols = np.where(tone[..., None] > 0.5, hexcol("f0cf9c"), np.where(tone[..., None] > -0.5, hexcol("bcd0ea"), hexcol("98cfe0")))
    room = cols[iy, ix] * (0.82 + 0.18 * table(ny, nx, 125)[iy, ix])[..., None]
    ceiling = box_mask(lx, ly, 0.0, -30.0, 20.0 + jw, 3.5, 2.0)    # one flat band of ceiling light per pane
    room = room * (0.86 + 0.14 * ceiling[..., None])
    on = pane * lit[iy, ix]
    img = lay(img, on, room * 0.6)
    emit = halo(room * on[..., None], 7, 0.5)
    emit = band(emit, ly, "9fe6f4", 29.0, 4.2, rows=[5, 11], iy=iy)   # the spandrel of every sixth floor is a light line
    save("glass_a", img)
    save("glass_a_emit", night(img, emit))


def glass_b():
    """SALAMIN: painted pilasters every bay, tall glass slots three floors high, lit in vertical runs."""
    nx, ny = 16, 4                                                # sixteen half-bays, four three-floor groups
    img = fill(S, S, "3e5a6a")                                    # the megaframe: a blue-green steel
    img = coat(img, "4a6878", 240, 0.35, 201)
    img = coat(img, "344e5c", 190, 0.25, 203)
    ix, iy, lx, ly = grid(nx, ny, 2.4, 210)
    jw = table(ny, nx, 211, -2.0, 2.0)[iy, ix]
    # A pilaster stands on every second boundary (every 8 m), so the slot is pushed off it.
    side = np.where(ix % 2 == 0, 5.0, -5.0)
    slot = box_mask(lx, ly, side, 0.0, 21.0 + jw, 118.0, 7.0)
    glass = fill(S, S, "1a3e48")
    glass = coat(glass, "24525c", 200, 0.4, 213, feather=0.9)
    glass = coat(glass, "15323c", 160, 0.25, 214)
    img = lay(img, slot, glass)
    # Transoms: two soft frame lines cross every slot, so the three floors read.
    for off in (-42.7, 42.7):
        bar = box_mask(lx, ly, side, off, 24.0, 2.6, 1.2)
        img = lay(img, bar * slot, "3e5a6a")
    # Lit in VERTICAL runs (lift lobbies, stairs), three panes to a slot.
    fx, fy = 16, 12
    fyi = np.floor((np.mgrid[0:S, 0:S][0] + 2.4 * smooth(S, S, 46, 211)) / (S / fy)).astype(int) % fy
    run = clusters(fy, fx, 220, scale=(0.8, 4.5)) + 0.8 * np.random.default_rng(221).standard_normal((1, fx))
    lit = (run > 0.7) & (table(fy, fx, 222) > 0.2)
    lit |= table(fy, fx, 223) > 0.975
    tone = clusters(fy, fx, 224, scale=(1.5, 6.0))
    cols = np.where(tone[..., None] > 0.8, hexcol("e8c890"), np.where(tone[..., None] > -0.6, hexcol("a0d4c4"), hexcol("c4e4d8")))
    room = cols[fyi, ix] * (0.8 + 0.2 * table(fy, fx, 225)[fyi, ix])[..., None]
    pane_y = (np.mgrid[0:S, 0:S][0] + 2.4 * smooth(S, S, 46, 211)) % (S / fy) - S / fy / 2
    pane = slot * np.clip((36.0 - np.abs(pane_y)) / 2.0, 0, 1)
    on = pane * lit[fyi, ix]
    img = lay(img, on, room * 0.6)
    emit = halo(room * on[..., None], 7, 0.45)
    emit = band(emit, ly, "8af0c8", 124.0, 3.6, strength=0.8)       # a mint line on the megaframe between slot groups
    save("glass_b", img)
    save("glass_b_emit", night(img, emit))


def resi():
    """TIRAHAN: a condominium. Per bay a balcony door, its slab and rail, a small window, an aircon."""
    nx, ny = 8, 12
    img = fill(S, S, "5a4f7c")                                    # dusk-violet plaster
    img = coat(img, "68598c", 280, 0.38, 301)
    img = coat(img, "4e446e", 220, 0.25, 303)
    img = coat(img, "544a78", 120, 0.12, 305, stretch=(1.0, 0.4))  # a long damp field under a ledge
    ix, iy, lx, ly = grid(nx, ny, 2.0, 310)
    jx = table(ny, nx, 311, -4.0, 4.0)[iy, ix]
    jw = table(ny, nx, 312, -3.0, 4.0)[iy, ix]
    door = box_mask(lx, ly, -24.0 + jx, 2.0, 25.0 + jw, 27.0, 5.0)
    small = box_mask(lx, ly, 34.0 + jx * 0.5, -6.0, 12.0 + jw * 0.5, 15.0, 4.0)
    # The balcony: one slab line under the door and a rail of three soft bars. Drawn, not ruled.
    slab = box_mask(lx, ly, -24.0 + jx, 34.0, 36.0, 3.6, 1.5)
    rail = box_mask(lx, ly, -24.0 + jx, 22.0, 34.0, 1.4, 0.7)
    aircon = box_mask(lx, ly, 34.0 + jx * 0.5, 19.0, 9.0, 5.5, 1.5) * (table(ny, nx, 313)[iy, ix] > 0.35)
    dark = fill(S, S, "1d1d3a")
    dark = coat(dark, "262650", 160, 0.4, 314, feather=0.9)
    img = lay(img, np.maximum(door, small), dark)
    img = lay(img, slab, "3a3256")
    img = lay(img, aircon, "8078a0")
    # Lit by the FLAT: two bays share a home, so their windows agree.
    flat = clusters(ny, nx // 2, 320, scale=(1.3, 1.6))
    home = np.repeat(flat, 2, axis=1)
    lit_d = (home > 0.15) & (table(ny, nx, 322) > 0.2)
    lit_s = (home > 0.15) & (table(ny, nx, 323) > 0.45)
    tone = np.repeat(table(ny, nx // 2, 324), 2, axis=1)
    cols = np.where(tone[..., None] > 0.9, hexcol("e088b8"), np.where(tone[..., None] > 0.74, hexcol("9cd8e6"),
                    np.where(tone[..., None] > 0.38, hexcol("f0c880"), hexcol("f0dcb8"))))
    room = cols[iy, ix] * (0.8 + 0.2 * table(ny, nx, 325)[iy, ix])[..., None]
    # A curtain half drawn across some lit doors: one flat darker shape.
    curtain = box_mask(lx, ly, -36.0 + jx, 2.0, 11.0, 27.0, 4.0) * (table(ny, nx, 326)[iy, ix] > 0.6)
    room = room * (1 - 0.45 * curtain[..., None])
    on = door * lit_d[iy, ix] + small * lit_s[iy, ix]
    img = lay(img, np.clip(on, 0, 1), room * 0.6)
    img = lay(img, rail * 0.85, "3a3256")
    emit = room * (np.clip(on, 0, 1) * (1 - 0.8 * rail))[..., None]
    emit = halo(emit, 6, 0.4)
    emit = band(emit, ly, "f2a8d0", 39.0, 2.6, strength=0.7, rows=[3, 7, 11], iy=iy)   # a pink slab light every fourth floor
    save("resi", img)
    save("resi_emit", night(img, emit))


def bands():
    """PAHALANG: rows only. A glazing strip per floor, lit in long runs with soft ends."""
    ny = 12
    img = fill(S, S, "3c4480")
    img = coat(img, "48508e", 260, 0.35, 401)
    img = coat(img, "343c72", 200, 0.22, 403)
    yy, xx = np.mgrid[0:S, 0:S].astype(float)
    yw = yy + 2.2 * smooth(S, S, 60, 410)
    ch = S / ny
    iy = np.floor(yw / ch).astype(int) % ny
    ly = (yw % ch) - ch / 2
    strip = np.clip((17.0 - np.abs(ly + 12.0)) / 1.8 + 0.5, 0, 1)
    glass = fill(S, S, "232a5c")
    glass = coat(glass, "2d3672", 200, 0.4, 413, feather=0.9)
    img = lay(img, strip, glass)
    # Long runs: a smooth periodic field along the strip, one per floor, thresholded softly.
    emit = np.zeros((S, S, 3))
    rng = np.random.default_rng(420)
    run = np.zeros((S, S))
    tone = np.zeros((S, S, 3))
    palette = [hexcol("bcd0ea"), hexcol("c8bce8"), hexcol("98cfe0"), hexcol("f0cf9c")]
    for k in range(ny):
        n = smooth(1, S, 70, 421 + k)[0]
        level = rng.uniform(0.1, 1.5)
        r = np.clip((n - level) / 0.25 + 0.5, 0, 1)
        run[iy == k] = np.broadcast_to(r, (S, S))[iy == k]
        tone[iy == k] = palette[rng.integers(0, len(palette))] * rng.uniform(0.8, 1.0)
    on = strip * run
    img = lay(img, on, tone * 0.6)
    emit = halo(tone * on[..., None], 7, 0.5)
    emit = band(emit, ly, "b9c4ff", 26.0, 3.0, strength=0.8, rows=[2, 6, 10], iy=iy)
    save("bands", img)
    save("bands_emit", night(img, emit))


def far():
    """MALAYO: distance. Hazed indigo, soft window lights in loose rows, low contrast."""
    nx, ny = 14, 16
    img = fill(S, S, "353a7c")
    img = coat(img, "3e448c", 300, 0.4, 501)
    img = coat(img, "2e3270", 220, 0.25, 503)
    ix, iy, lx, ly = grid(nx, ny, 3.0, 510)
    jx = table(ny, nx, 511, -8.0, 8.0)[iy, ix]
    jw = table(ny, nx, 512, -6.0, 8.0)[iy, ix]
    light = box_mask(lx, ly, jx, 0.0, 20.0 + jw, 12.0, 8.0, feather=4.0)
    group = clusters(ny, nx, 520, scale=(1.1, 0.8))
    lit = (group > 0.4) & (table(ny, nx, 522) > 0.25)
    tone = clusters(ny, nx, 524, scale=(3.0, 3.0))
    cols = np.where(tone[..., None] > 0.6, hexcol("e8c898"), np.where(tone[..., None] > -0.7, hexcol("a8b8e8"), hexcol("c898d0")))
    room = cols[iy, ix] * (0.7 + 0.3 * table(ny, nx, 525)[iy, ix])[..., None]
    on = light * lit[iy, ix]
    img = lay(img, on * 0.7, room)
    emit = halo(room * on[..., None] * 0.7, 9, 0.5)
    save("far", img)
    save("far_emit", img * 0.42 + emit)


def base():
    """A tower's lower shaft, FOOT to the sky lobby: v 0 is the city floor. Rows of light that
    thin out, and the wall dissolving into the haze colour. Rows only, so its long v cannot stretch
    anything."""
    h, w = 1024, 256
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    v = 1.0 - yy / h                                              # 1 at the sky lobby, 0 at the floor
    wall = fill(h, w, "3c4480")
    fade = np.clip((1.0 - v) / 0.62, 0, 1) ** 1.1                 # the haze takes over going down
    fade = np.clip(fade + 0.06 * smooth(h, w, 90, 601, stretch=(0.25, 1.0)), 0, 1)
    img = lay(wall, fade, HAZE)
    rows = 72                                                     # about one row per 8 m of a 590 m shaft
    ly = (yy % (h / rows)) - h / rows / 2
    strip = np.clip((2.6 - np.abs(ly)) / 1.4 + 0.5, 0, 1)
    k = np.floor(yy / (h / rows)).astype(int)
    rng = np.random.default_rng(602)
    level = rng.uniform(0.5, 2.0, rows + 1)[k] + (1 - v) * 2.0   # fewer lit rows deeper down
    n = np.vstack([smooth(1, w, 40, 610 + i)[0] for i in range(rows + 1)])[k, xx.astype(int)]
    on = strip * np.clip((n - level) / 0.3 + 0.5, 0, 1) * (1 - fade)
    tone = hexcol("bcd0ea")
    img = lay(img, on, tone)
    emit = on[..., None] * tone + hexcol(HAZE_EMIT) * fade[..., None] + img * NIGHT * (1 - fade[..., None])
    save("base", img)
    save("base_emit", emit)


def metal():
    """BAKAL: dark blue-grey painted panels, 8 m square, with soft joints and the odd repainted one."""
    img = fill(S, S, "4a547c")
    img = coat(img, "56608a", 300, 0.4, 701)
    img = coat(img, "3e476c", 220, 0.22, 703)
    ix, iy, lx, ly = grid(4, 4, 3.0, 710)
    panel = box_mask(lx, ly, 0.0, 0.0, 124.0, 124.0, 10.0, feather=2.5)
    shade = table(4, 4, 711, 0.94, 1.07)[iy, ix]
    img = img * (shade * panel + (1 - panel) * 0.84)[..., None]
    save("metal", img)
    save("metal_emit", img * NIGHT)


def roofs():
    """BUBONG: rooftops from above, a 128 m tile of sixteen roofs. Plant rooms with a drawn shadow,
    round tanks, lit skylights, one landing pad."""
    nx = ny = 4
    img = fill(S, S, "343c66")
    img = coat(img, "3e4674", 260, 0.4, 801)
    img = coat(img, "2a3258", 200, 0.22, 803)
    ix, iy, lx, ly = grid(nx, ny, 2.5, 810)
    emit = np.zeros((S, S, 3))
    kind = (table(ny, nx, 811) * 5).astype(int)[iy, ix]
    jx = table(ny, nx, 812, -30.0, 30.0)[iy, ix]
    jy = table(ny, nx, 813, -30.0, 30.0)[iy, ix]
    edge = box_mask(lx, ly, 0.0, 0.0, 122.0, 122.0, 6.0) - box_mask(lx, ly, 0.0, 0.0, 114.0, 114.0, 4.0)
    img = lay(img, np.clip(edge, 0, 1) * 0.8, "4a5484")           # the parapet, drawn
    # Plant rooms: a box and its flat shadow down and to the right.
    for dx, dy, hw, hh, which in ((-30, -34, 34, 22, (0, 1, 3)), (40, 30, 22, 30, (0, 2)), (-44, 44, 20, 14, (1, 2, 4))):
        on = np.isin(kind, which)
        img = lay(img, box_mask(lx, ly, dx + jx * 0.3 + 7, dy + jy * 0.3 + 7, hw, hh, 3.0) * on * 0.7, "1c2244")
        img = lay(img, box_mask(lx, ly, dx + jx * 0.3, dy + jy * 0.3, hw, hh, 3.0) * on, "56608e")
    # Tanks: three circles in a row on some roofs.
    for k in range(3):
        d = np.hypot(lx - (20 + k * 24 + jx * 0.2), ly - (-56 + jy * 0.2))
        on = np.isin(kind, (1, 4))
        img = lay(img, np.clip((11.0 - np.hypot(lx - (24 + k * 24 + jx * 0.2), ly - (-52 + jy * 0.2))) / 2 + 0.5, 0, 1) * on * 0.7, "1c2244")
        img = lay(img, np.clip((10.0 - d) / 1.6 + 0.5, 0, 1) * on, "6a74a0")
    # Skylights: lit rounded panes in a row, the roof's own windows.
    for k in range(3):
        sky = box_mask(lx, ly, -40 + k * 30 + jx * 0.4, 6 + jy * 0.4, 10.0, 16.0, 4.0) * np.isin(kind, (2, 3))
        tone = np.where(table(ny, nx, 815)[iy, ix][..., None] > 0.5, hexcol("f0cf9c"), hexcol("a8d8e8"))
        img = lay(img, sky, tone)
        emit += tone * sky[..., None]
    # One landing pad on kind 4: a cream ring and a bar, not lit.
    d = np.hypot(lx + 30, ly - 10)
    pad = (np.clip((34.0 - d) / 1.6 + 0.5, 0, 1) - np.clip((29.0 - d) / 1.6 + 0.5, 0, 1)) * (kind == 4)
    img = lay(img, np.clip(pad, 0, 1) * 0.8, "b8b8a8")
    img = lay(img, box_mask(lx, ly, -30.0, 10.0, 14.0, 4.0, 1.5) * (kind == 4) * 0.8, "b8b8a8")
    # An obstruction light at one corner of every roof.
    d = np.hypot(lx - 108, ly + 108)
    lamp = np.clip((5.0 - d) / 1.6 + 0.5, 0, 1)
    img = lay(img, lamp, "e8404c")
    emit += hexcol("e8404c") * lamp[..., None]
    save("roofs", img)
    save("roofs_emit", img * NIGHT + halo(emit, 6, 0.5))


# ------------------------------------------------------------------ one-off artwork

def floor():
    """The city floor, 4096 m across at 2 m a pixel: the rotunda under the shaft drawn as the
    game's own chalk ring, eight avenues, ring roads, a wobbled grid of streets, lit districts."""
    n = 2048
    yy, xx = np.mgrid[0:n, 0:n].astype(float)
    wx = xx + 9.0 * smooth(n, n, 120, 901)
    wy = yy + 9.0 * smooth(n, n, 120, 902)
    x, y = (wx - n / 2) * 2.0, (wy - n / 2) * 2.0                 # metres
    r = np.hypot(x, y)
    ang = np.degrees(np.arctan2(x, -y)) % 360.0

    def line(d, width):
        return np.clip((width - np.abs(d)) / 1.5 + 0.5, 0, 1)

    ground = fill(n, n, "141838")
    ground = coat(ground, "1c2048", 300, 0.4, 903)
    ground = coat(ground, "10132c", 240, 0.25, 905)
    emit = np.zeros((n, n, 3))
    # The street grid: blocks 150 m, in two districts turned against each other.
    turn = patch(n, n, 420, 0.5, 906, feather=0.4)
    c, s = np.cos(np.radians(24)), np.sin(np.radians(24))
    xr, yr = x * c + y * s, -x * s + y * c
    g1 = np.maximum(line((x % 150) - 75, 3.0), line((y % 150) - 75, 3.0))
    g2 = np.maximum(line((xr % 170) - 85, 3.0), line((yr % 170) - 85, 3.0))
    streets = (g1 * (1 - turn) + g2 * turn) * np.clip((r - 150) / 60, 0, 1)
    busy = patch(n, n, 260, 0.55, 907, feather=1.2)               # some districts are lit, some asleep
    emit += hexcol("7aa8e8") * (streets * (0.25 + 0.75 * busy))[..., None]
    warm = patch(n, n, 300, 0.3, 908, feather=1.0)
    emit += hexcol("e8a070") * (streets * warm * 0.5)[..., None]
    pink = patch(n, n, 240, 0.18, 909, feather=1.0)
    emit += hexcol("d060b0") * (streets * pink * 0.55)[..., None]
    # Lights inside the blocks: soft clusters, big and few.
    lots = patch(n, n, 9, 0.12, 910, feather=0.5) * busy * np.clip((r - 170) / 60, 0, 1)
    emit += hexcol("c8d4ff") * (lots * 0.3)[..., None]
    # Ring roads and eight avenues, wide and warm.
    for rr, wdt in ((190.0, 7.0), (430.0, 8.0), (780.0, 9.0), (1180.0, 9.0), (1640.0, 9.0)):
        emit += hexcol("ffcf8a") * (line(r - rr, wdt) * 0.9)[..., None]
    for k in range(8):
        d = np.sin(np.radians(ang - k * 45.0)) * r
        ahead = np.cos(np.radians(ang - k * 45.0)) > 0
        emit += hexcol("ffcf8a") * (line(d, 8.0) * ahead * np.clip((r - 70) / 30, 0, 1) * 0.9)[..., None]
    # The rotunda: the game's chalk ring, drawn in light. A ring, a small ring, a square for the can.
    emit += hexcol("e8f0ff") * (line(r - 70.0, 5.0))[..., None]
    emit += hexcol("e8f0ff") * (line(r - 26.0, 2.5) * 0.9)[..., None]
    park = np.clip((64.0 - r) / 6 + 0.5, 0, 1)
    ground = lay(ground, park * 0.8, "163a34")                    # a dark green park inside the rotunda
    emit *= (1 - 0.85 * park * (r > 30))[..., None]
    emit += hexcol("62dcf2") * np.clip((9.0 - np.maximum(np.abs(x), np.abs(y))) / 2 + 0.5, 0, 1)[..., None]
    emit = np.clip(emit, 0, 1.0)
    emit = emit + ndimage.gaussian_filter(emit, (5, 5, 0)) * 0.7
    # Everything dissolves into the haze toward the rim.
    out = np.clip((r - 1150.0) / 800.0, 0, 1) ** 1.2
    img = lay(ground + emit * 0.6, out, HAZE)
    emit = emit * (1 - out[..., None]) * 0.9 + hexcol(HAZE_EMIT) * (0.35 + 0.65 * out[..., None])
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(OUT / "arena_city_floor.png")
    Image.fromarray((np.clip(emit, 0, 1) * 255).astype(np.uint8)).save(OUT / "arena_city_floor_emit.png")
    print("[arena-city-tex] floor", img.shape)


def trim():
    """Eight LED strips, bottom row first (TRIM). Flat light with a soft brighter core."""
    h = w = 256
    img = np.zeros((h, w, 3))
    yy = np.mgrid[0:h, 0:w][0].astype(float)
    for k, name in enumerate(TRIM):
        row = (h - 1 - yy) // 32 == k
        centre = 1.0 - np.abs(((h - 1 - yy) % 32) - 15.5) / 16.0
        c = hexcol(TRIM_HEX[name])
        tone = c[None, None, :] * (0.78 + 0.22 * np.clip(centre * 1.6, 0, 1))[..., None]
        img = np.where(row[..., None], tone, img)
    save("trim", img)
    dark = np.mgrid[0:h, 0:w][0] < 32                             # the top image row is "dark": no glow
    save("trim_emit", np.where(dark[..., None], 0.0, img))


def _board(h, w, ground, border, seed, band=10):
    img = fill(h, w, ground)
    img = coat(img, np.clip(hexcol(ground) * 1.25, 0, 1), max(h, w) / 3, 0.35, seed, feather=1.0)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    xx2, yy2 = xx + 1.5 * smooth(h, w, 40, seed + 1), yy + 1.5 * smooth(h, w, 40, seed + 2)
    d = np.minimum(np.minimum(xx2, w - 1 - xx2), np.minimum(yy2, h - 1 - yy2))
    frame = np.clip((d - band) / 1.5 + 0.5, 0, 1) * np.clip((band * 1.9 - d) / 1.5 + 0.5, 0, 1)
    return lay(img, frame, border), frame


def signs():
    """The sign atlas. Dark grounds, letters in a few soft colours, one flat emblem each. Emission
    is the letters and emblem plus a little of the ground, so no sign is a glowing slab."""
    n = SIGN_ATLAS
    atlas = fill(n, n, "141830")
    glow = np.zeros((n, n, 3))

    def put(name, img, lit, ground_glow=0.16):
        x0, y0, x1, y1 = SIGNS[name]["box"]
        atlas[y0:y1, x0:x1] = img
        glow[y0:y1, x0:x1] = img * np.clip(lit[..., None] * 0.92 + ground_glow, 0, 1)

    def dims(name):
        x0, y0, x1, y1 = SIGNS[name]["box"]
        return y1 - y0, x1 - x0

    # LIGA NG TUMBANG PRESO: the league's board, the logo on the left.
    h, w = dims("liga")
    img, lit = _board(h, w, "171b3c", "dfeaff", 1001)
    logo = Image.open(LOGO).convert("RGBA")
    lw = int(w * 0.44)
    lh = int(lw * logo.size[1] / logo.size[0])
    lg = np.asarray(logo.resize((lw, lh), Image.LANCZOS), dtype=float) / 255
    ox, oy = int(w * 0.04), (h - lh) // 2
    a = lg[..., 3:4]
    img[oy:oy + lh, ox:ox + lw] = img[oy:oy + lh, ox:ox + lw] * (1 - a) + lg[..., :3] * 0.92 * a
    lit[oy:oy + lh, ox:ox + lw] = np.maximum(lit[oy:oy + lh, ox:ox + lw], a[..., 0] * 0.8)
    m = lettering(h, w, ["LIGA NG", "TUMBANG", "PRESO"], "impact", (w * 0.52, h * 0.12, w * 0.95, h * 0.68), 1002)
    img = lay(img, m, "f4ecd8"); lit = np.maximum(lit, m)
    m = lettering(h, w, ["FINALS NGAYONG GABI"], "bahn", (w * 0.52, h * 0.72, w * 0.95, h * 0.86), 1003)
    img = lay(img, m, "62dcf2"); lit = np.maximum(lit, m)
    put("liga", img, lit)

    # SINAG ENERHIYA: a rising sun of seven flat rays.
    h, w = dims("sinag")
    img, lit = _board(h, w, "12313a", "f2c060", 1011)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    cx, cy = w * 0.16, h * 0.70
    rr, aa = np.hypot(xx - cx, yy - cy), np.degrees(np.arctan2(cy - yy, xx - cx))
    sun = np.clip((h * 0.2 - rr) / 1.5 + 0.5, 0, 1) * (yy < cy)
    rays = (np.abs(((aa + 15) % 30) - 15) < 6.5) * (aa > 2) * (aa < 178) * np.clip((rr - h * 0.26) / 2, 0, 1) * np.clip((h * 0.46 - rr) / 2, 0, 1)
    rays = ndimage.gaussian_filter(rays.astype(float), 1.0)
    img = lay(img, np.maximum(sun, rays), "f2c060"); lit = np.maximum(lit, np.maximum(sun, rays))
    m = lettering(h, w, ["SINAG"], "impact", (w * 0.33, h * 0.14, w * 0.95, h * 0.62), 1012)
    img = lay(img, m, "f6efdc"); lit = np.maximum(lit, m)
    m = lettering(h, w, ["ENERHIYA  ·  ILAW NG LUNGSOD"], "bahn", (w * 0.33, h * 0.68, w * 0.95, h * 0.84), 1013)
    img = lay(img, m, "f2c060"); lit = np.maximum(lit, m)
    put("sinag", img, lit)

    # BAHAGHARI TELEKOM: four muted arcs.
    h, w = dims("bahaghari")
    img, lit = _board(h, w, "1a1d44", "c9b8f0", 1021)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    cx, cy = w * 0.14, h * 0.78
    rr = np.hypot(xx - cx, yy - cy)
    for k, colr in enumerate(("c8479a", "f2c060", "86efc4", "62dcf2")):
        arc = np.clip((7.0 - np.abs(rr - (h * 0.50 - k * 19.0))) / 1.5 + 0.5, 0, 1) * (yy < cy)
        img = lay(img, arc, colr); lit = np.maximum(lit, arc)
    m = lettering(h, w, ["BAHAGHARI"], "black", (w * 0.30, h * 0.16, w * 0.95, h * 0.60), 1022)
    img = lay(img, m, "f4ecd8"); lit = np.maximum(lit, m)
    m = lettering(h, w, ["TELEKOM  ·  SIGNAL HANGGANG ULAP"], "bahn", (w * 0.30, h * 0.66, w * 0.95, h * 0.82), 1023)
    img = lay(img, m, "c9b8f0"); lit = np.maximum(lit, m)
    put("bahaghari", img, lit)

    # MANG ISKO HOVER VULCANIZING: a hand-painted board, brush letters, a hover pad for a tyre.
    h, w = dims("isko")
    img, lit = _board(h, w, "4a1c28", "f0dcb0", 1031)
    m = lettering(h, w, ["Mang Isko"], "print", (w * 0.08, h * 0.08, w * 0.70, h * 0.36), 1032, wobble=2.2, lean=-0.06)
    img = lay(img, m, "f0dcb0"); lit = np.maximum(lit, m)
    m = lettering(h, w, ["HOVER", "VULCANIZING"], "impact", (w * 0.08, h * 0.36, w * 0.92, h * 0.80), 1033, wobble=2.0)
    img = lay(img, m, "f2c060"); lit = np.maximum(lit, m)
    m = lettering(h, w, ["BUKAS 24 ORAS  ·  IKA-88 PALAPAG"], "bahn", (w * 0.08, h * 0.82, w * 0.92, h * 0.92), 1034)
    img = lay(img, m, "f0dcb0"); lit = np.maximum(lit, m * 0.8)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    rr = np.hypot((xx - w * 0.84) / 1.5, yy - h * 0.22)
    ring = np.clip((6.0 - np.abs(rr - h * 0.11)) / 1.5 + 0.5, 0, 1)
    img = lay(img, ring, "62dcf2"); lit = np.maximum(lit, ring)
    put("isko", img, lit, 0.2)

    # DYIP-LIPAD TERMINAL: the flying jeepney's stop, its routes and three chevrons.
    h, w = dims("dyip")
    img, lit = _board(h, w, "141c40", "62dcf2", 1041, band=8)
    m = lettering(h, w, ["DYIP-LIPAD TERMINAL"], "impact", (w * 0.05, h * 0.14, w * 0.80, h * 0.62), 1042)
    img = lay(img, m, "f4ecd8"); lit = np.maximum(lit, m)
    m = lettering(h, w, ["MONUMENTO  ·  CUBAO  ·  QUIAPO  ·  BACLARAN"], "bahn", (w * 0.05, h * 0.66, w * 0.80, h * 0.84), 1043)
    img = lay(img, m, "62dcf2"); lit = np.maximum(lit, m)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    for k in range(3):
        d = np.abs(yy - h / 2) * 0.7 + (xx - (w * 0.84 + k * w * 0.045))
        ch = np.clip((7.0 - np.abs(d)) / 1.5 + 0.5, 0, 1) * (np.abs(yy - h / 2) < h * 0.26)
        img = lay(img, ch, "f2c060"); lit = np.maximum(lit, ch)
    put("dyip", img, lit)

    # KAPE NI KA INGGO: a cup, three flat curls of steam.
    h, w = dims("kape")
    img, lit = _board(h, w, "2a1c30", "f2c060", 1051)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    cup = box_mask(xx, yy, w * 0.5, h * 0.36, w * 0.17, h * 0.10, 22.0)
    cup = np.maximum(cup, np.clip((8.0 - np.abs(np.hypot(xx - w * 0.70, yy - h * 0.35) - h * 0.06)) / 1.5 + 0.5, 0, 1))
    img = lay(img, cup, "f0dcb0"); lit = np.maximum(lit, cup)
    for k in (-1, 0, 1):
        sx = w * 0.5 + k * w * 0.09 + np.sin((yy - h * 0.1) / 16.0 + k) * 9.0
        st = np.clip((5.0 - np.abs(xx - sx)) / 1.5 + 0.5, 0, 1) * (yy > h * 0.08) * (yy < h * 0.23)
        img = lay(img, st, "c9b8f0"); lit = np.maximum(lit, st * 0.8)
    m = lettering(h, w, ["KAPE"], "impact", (w * 0.12, h * 0.50, w * 0.88, h * 0.76), 1052)
    img = lay(img, m, "f2c060"); lit = np.maximum(lit, m)
    m = lettering(h, w, ["ni Ka Inggo"], "print", (w * 0.12, h * 0.76, w * 0.88, h * 0.92), 1053, wobble=2.0)
    img = lay(img, m, "f0dcb0"); lit = np.maximum(lit, m)
    put("kape", img, lit)

    # ALING DELY SARI-SARI: the corner store, 212 floors up.
    h, w = dims("dely")
    img, lit = _board(h, w, "1c3a30", "f0dcb0", 1061, band=8)
    m = lettering(h, w, ["Aling Dely"], "print", (w * 0.05, h * 0.12, w * 0.42, h * 0.60), 1062, wobble=2.0, lean=-0.05)
    img = lay(img, m, "f2c060"); lit = np.maximum(lit, m)
    m = lettering(h, w, ["SARI-SARI"], "impact", (w * 0.45, h * 0.12, w * 0.95, h * 0.62), 1063)
    img = lay(img, m, "f4ecd8"); lit = np.maximum(lit, m)
    m = lettering(h, w, ["YELO  ·  LOAD  ·  KARGA NG HOVER  ·  IKA-212 PALAPAG"], "bahn", (w * 0.05, h * 0.68, w * 0.95, h * 0.84), 1064)
    img = lay(img, m, "f0dcb0"); lit = np.maximum(lit, m * 0.8)
    put("dely", img, lit, 0.2)

    # PANSITAN SA ULAP: a blade sign, one letter to a line, a bowl and chopsticks on top.
    h, w = dims("pansitan")
    img, lit = _board(h, w, "361a3c", "f08ac0", 1071)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    bowl = np.clip((w * 0.30 - np.hypot(xx - w / 2, (yy - h * 0.075) * 1.5)) / 1.5 + 0.5, 0, 1) * (yy > h * 0.075)
    img = lay(img, bowl, "f4ecd8"); lit = np.maximum(lit, bowl)
    for k in (-1, 1):
        st = np.clip((4.0 - np.abs((xx - w / 2) - k * 26 - (h * 0.075 - yy) * 0.5 * k)) / 1.5 + 0.5, 0, 1) * (yy < h * 0.075) * (yy > h * 0.03)
        img = lay(img, st, "f2c060"); lit = np.maximum(lit, st)
    m = lettering(h, w, ["PANSITAN"], "impact", (w * 0.2, h * 0.15, w * 0.8, h * 0.74), 1072, vertical=True, spacing=0.10)
    img = lay(img, m, "f08ac0"); lit = np.maximum(lit, m)
    m = lettering(h, w, ["SA", "ULAP"], "black", (w * 0.14, h * 0.78, w * 0.86, h * 0.94), 1073)
    img = lay(img, m, "f4ecd8"); lit = np.maximum(lit, m)
    put("pansitan", img, lit)

    # HALO-HALO HOLO: a tall glass in three flat layers.
    h, w = dims("halo")
    img, lit = _board(h, w, "14303c", "86efc4", 1081)
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    half = w * 0.13 + (h * 0.24 - yy) * 0.22
    glass = np.clip((half - np.abs(xx - w / 2)) / 1.5 + 0.5, 0, 1) * (yy > h * 0.06) * (yy < h * 0.24)
    for lo, hi, colr in ((0.06, 0.12, "f4ecd8"), (0.12, 0.18, "f08ac0"), (0.18, 0.24, "c9b8f0")):
        part = glass * (yy >= h * lo) * (yy < h * hi)
        img = lay(img, part, colr); lit = np.maximum(lit, part)
    m = lettering(h, w, ["HALO", "HALO"], "impact", (w * 0.16, h * 0.30, w * 0.84, h * 0.62), 1082)
    img = lay(img, m, "86efc4"); lit = np.maximum(lit, m)
    m = lettering(h, w, ["HOLO"], "impact", (w * 0.2, h * 0.64, w * 0.8, h * 0.95), 1083, vertical=True, spacing=0.10)
    img = lay(img, m, "f08ac0"); lit = np.maximum(lit, m)
    put("halo", img, lit)

    Image.fromarray((np.clip(atlas, 0, 1) * 255).astype(np.uint8)).save(OUT / "arena_city_signs.png")
    Image.fromarray((np.clip(glow, 0, 1) * 255).astype(np.uint8)).save(OUT / "arena_city_signs_emit.png")
    print("[arena-city-tex] signs", atlas.shape)


def fx():
    """The hologram atlas (RGBA): the TUMP logo as a hologram in one cyan, with scanlines; a
    scanline band for the hologram's rings; a soft round glow; a long soft beam."""
    n = FX_ATLAS
    rgb = np.zeros((n, n, 3))
    alpha = np.zeros((n, n))
    yy, xx = np.mgrid[0:n, 0:n].astype(float)
    x0, y0, x1, y1 = FX["logo"]
    logo = np.asarray(Image.open(LOGO).convert("RGBA").resize((x1 - x0, y1 - y0), Image.LANCZOS), dtype=float) / 255
    luma = logo[..., :3] @ np.array([0.3, 0.6, 0.1])
    scan = 0.72 + 0.28 * (np.sin(yy[y0:y1, x0:x1] * 0.9) > -0.2)
    tone = hexcol("62dcf2")[None, None, :] * (0.3 * luma[..., None]) + logo[..., :3] * 0.72   # its own colours, washed with the hologram's cyan
    rgb[y0:y1, x0:x1] = tone * scan[..., None]
    alpha[y0:y1, x0:x1] = logo[..., 3] * 0.9 * scan
    x0, y0, x1, y1 = FX["scan"]
    v = (yy[y0:y1, x0:x1] - y0) / (y1 - y0)
    band = np.clip(1 - np.abs(v - 0.5) * 2, 0, 1) ** 0.6 * (0.6 + 0.4 * (np.sin(yy[y0:y1, x0:x1] * 1.3) > 0))
    rgb[y0:y1, x0:x1] = hexcol("62dcf2")
    alpha[y0:y1, x0:x1] = band * 0.6
    x0, y0, x1, y1 = FX["glow"]
    d = np.hypot(xx[y0:y1, x0:x1] - (x0 + x1) / 2, yy[y0:y1, x0:x1] - (y0 + y1) / 2) / ((x1 - x0) / 2)
    rgb[y0:y1, x0:x1] = hexcol("f4f6ff")
    alpha[y0:y1, x0:x1] = np.clip(1 - d, 0, 1) ** 1.8
    x0, y0, x1, y1 = FX["beam"]
    u = (xx[y0:y1, x0:x1] - x0) / (x1 - x0)
    v = np.abs((yy[y0:y1, x0:x1] - y0) / (y1 - y0) - 0.5) * 2
    rgb[y0:y1, x0:x1] = hexcol("dfeaff")
    alpha[y0:y1, x0:x1] = np.clip(1 - v / (0.15 + 0.85 * u), 0, 1) ** 1.5 * (1 - u) ** 1.2 * 0.5
    # THE CROWN HALO: a faint violet-blue wash that stands BEHIND a tower's upper third (the tower
    # hides the middle of it), so the silhouette is cut out of light instead of lost in the sky.
    # Faint on purpose: drawn at the fx material's strength it adds about a fifth of a window's light.
    x0, y0, x1, y1 = FX["halo"]
    d = np.hypot(xx[y0:y1, x0:x1] - (x0 + x1) / 2, yy[y0:y1, x0:x1] - (y0 + y1) / 2) / ((x1 - x0) / 2)
    rgb[y0:y1, x0:x1] = hexcol("8f8cff")
    alpha[y0:y1, x0:x1] = np.clip(1 - d, 0, 1) ** 1.5 * 0.30
    save("fx", rgb, alpha)
    save("fx_emit", rgb * alpha[..., None])


def haze():
    """A tileable layer of haze: broad soft wisps in the haze colour (alpha)."""
    a = patch(S, S, 150, 0.55, 1201, feather=1.6, stretch=(1.0, 0.55))
    a = 0.16 + a * 0.5 + 0.14 * patch(S, S, 70, 0.4, 1203, feather=1.4)
    rgb = fill(S, S, HAZE)
    rgb = coat(rgb, "4a3a8c", 200, 0.3, 1205, feather=1.2)
    save("haze", rgb, a)
    save("haze_emit", rgb * 0.55 * a[..., None])


def sky():
    """The night sky, u = bearing / 360, row 0 the zenith. Deep navy overhead, the city's glow
    rising from the horizon in broad uneven fields, a few long clouds LIT FROM BELOW, a handful
    of stars high up. Below the horizon it is the haze colour, so the depths have no edge."""
    h, w = 2048, 4096
    yy, xx = np.mgrid[0:h, 0:w].astype(float)
    elev = 90.0 - yy / h * 180.0
    up = np.clip(elev / 90.0, 0, 1)
    zen, mid, hor = hexcol("060a1e"), hexcol("101844"), hexcol("3a2c80")
    t = up ** 0.5
    img = hor * (1 - t[..., None]) + mid * t[..., None]
    t2 = np.clip((up - 0.35) / 0.65, 0, 1)
    img = img * (1 - t2[..., None]) + zen * t2[..., None]
    # The glow is uneven: two broad warm-pink fields and one teal one low on the horizon.
    low = np.clip(1 - elev / 26.0, 0, 1) ** 1.6 * (elev > -2)
    for seed, colr, cov in ((1301, "7a3c8c", 0.4), (1303, "2a6a8a", 0.25)):
        field = patch(h, w, 480, cov, seed, feather=1.6, stretch=(1.0, 0.35))
        img = lay(img, field * low * 0.75, colr)
    # Clouds: a few long flat shapes between 6 and 34 degrees up.
    n = smooth(h, w, 460, 1310, stretch=(1.0, 0.36)) + 0.2 * smooth(h, w, 140, 1311, stretch=(1.0, 0.4))
    band = np.exp(-((elev - 22.0) / 12.0) ** 2)
    field = n * 0.5 + band * 1.2 - 1.42
    cloud = np.clip(field / 0.05 + 0.5, 0, 1)
    cloud = cloud * cloud * (3 - 2 * cloud)
    below_it = np.roll(cloud, -26, axis=0)                        # is there still cloud 26 px lower down?
    under = cloud * (1 - below_it)                                # no: so this is the underside
    under = np.clip(ndimage.gaussian_filter(under, 2.0) * 1.2, 0, 1) * cloud
    body = hexcol("1c2254")
    lit_a, lit_b = hexcol("7c5690"), hexcol("46768e")
    which = patch(h, w, 600, 0.35, 1320, feather=1.5)
    lit = lit_a * (1 - which[..., None]) + lit_b * which[..., None]
    ccol = body * (1 - under[..., None]) + lit * under[..., None]
    img = img * (1 - cloud[..., None] * 0.92) + ccol * cloud[..., None] * 0.92
    # Stars: about ninety, above 32 degrees, soft and small, three sizes.
    rng = np.random.default_rng(1330)
    stars = np.zeros((h, w))
    for _ in range(160):
        e = rng.uniform(32, 88)
        sy, sx = (90 - e) / 180 * h, rng.uniform(0, w)
        rad = rng.choice([1.3, 1.8, 2.5], p=[0.6, 0.3, 0.1])
        x0, x1, y0, y1 = int(max(0, sx - 60)), int(min(w, sx + 60)), int(max(0, sy - 8)), int(min(h, sy + 8))
        d = np.hypot((xx[y0:y1, x0:x1] - sx) * np.cos(np.radians(e)), yy[y0:y1, x0:x1] - sy)
        stars[y0:y1, x0:x1] = np.maximum(stars[y0:y1, x0:x1], np.clip((rad - d) / 1.0 + 0.5, 0, 1) * rng.uniform(0.45, 0.9))
    img = lay(img, stars * (1 - cloud), "e8ecff")
    # Below the horizon: the haze.
    below = np.clip(-elev / 5.0 + 0.3, 0, 1)
    img = lay(img, below, HAZE_EMIT)
    save("sky", img)
    # The same clouds for TumbangPreso/NeighbourhoodSky's _CloudMap: pure blue is clear sky, grey is
    # cloud, and the grey's value is how much of the glow it catches (0.25 body, 0.9 underside).
    form = np.zeros((h, w, 3))
    form[..., 2] = 1.0
    grey = 0.25 + 0.65 * under
    form = form * (1 - cloud[..., None]) + grey[..., None] * cloud[..., None]
    save("sky_cloudform", form)


def checker():
    """The UV checker: eight squares a side, every square numbered by column and row, so a
    stretched or mirrored face is plain to see."""
    LOGS.mkdir(parents=True, exist_ok=True)
    im = Image.new("RGB", (S, S))
    d = ImageDraw.Draw(im)
    f = font("bahn", 44)
    cells = 8
    c = S // cells
    for j in range(cells):
        for i in range(cells):
            dark = (i + j) % 2 == 0
            hue = (40 + i * 26, 60 + j * 22, 150) if dark else (232, 232, 222)
            d.rectangle((i * c, j * c, (i + 1) * c - 1, (j + 1) * c - 1), fill=hue)
            d.ellipse((i * c + 34, j * c + 34, (i + 1) * c - 35, (j + 1) * c - 35), outline=(250, 200, 60) if dark else (60, 60, 90), width=5)
            d.text((i * c + 46, j * c + 38), "%d%d" % (i, cells - 1 - j), font=f, fill=(255, 255, 255) if dark else (30, 30, 50))
    im.save(LOGS / "checker.png")
    print("[arena-city-tex] checker")


PAINTERS = {"glass_a": glass_a, "glass_b": glass_b, "resi": resi, "bands": bands, "far": far, "base": base,
            "metal": metal, "roofs": roofs, "floor": floor, "trim": trim, "signs": signs, "fx": fx, "haze": haze,
            "sky": sky}


def sheet(version):
    """One picture of every texture, albedo over emission, for the review."""
    LOGS.mkdir(parents=True, exist_ok=True)
    names = ["glass_a", "glass_b", "resi", "bands", "far", "metal", "roofs", "base", "floor", "signs", "fx", "sky"]
    cell = 384
    im = Image.new("RGB", (cell * 6, cell * 4 + 40), (10, 12, 24))
    d = ImageDraw.Draw(im)
    f = font("bahn", 20)
    for k, name in enumerate(names):
        col, row = k % 6, k // 6
        for part, suffix in enumerate(("", "_emit")):
            p = OUT / ("arena_city_%s%s.png" % (name, suffix))
            if not p.exists():
                continue
            t = Image.open(p).convert("RGB")
            t.thumbnail((cell - 8, cell - 8))
            im.paste(t, (col * cell + 4, (row * 2 + part) * cell + 4 + 40))
        d.text((col * cell + 8, row * 2 * cell + 10), name, font=f, fill=(230, 230, 240))
    im.save(LOGS / ("city_textures_%s.png" % version))
    print("[arena-city-tex] sheet", version)


def main():
    _lazy()
    only = [a for a in sys.argv[1:] if a in PAINTERS]
    for name, fn in PAINTERS.items():
        if not only or name in only:
            fn()
    checker()
    for i, a in enumerate(sys.argv):
        if a == "--sheet" and i + 1 < len(sys.argv):
            sheet(sys.argv[i + 1])


if __name__ == "__main__":
    main()
