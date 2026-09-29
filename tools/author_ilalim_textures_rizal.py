"""Paint Rizal Hall's textures for the Ilalim ng Tulay rebuild, in the house illustrated style (ILALIM-1.3).

  py -3 tools/author_ilalim_textures_rizal.py [--sheet N]

Writes ArtSource/ilalim/textures/rizal_*.png and the swatch sheet
Logs/ilalim-blender/rizal_swatches_vN.png. The tiling textures cover TILE_M (4 m) and the model
(tools/author_ilalim_rizal_hall.py) maps them with world-scale UVs.

THE STYLE is the LRT kit's (tools/author_ilalim_textures.py) and Kanto's: FLAT fills, a few
LARGE patches with FEATHERED organic edges, low contrast, no grain, no noise, no streaks. Every
surface has ITS OWN drawing; none of these reuses another surface's generator, and none reuses
the LRT kit's drawings. Only the small numeric helpers (smooth, coat, flat, hexcol) are imported.

THE SURFACES, from the Commons photographs of Rizal Hall (research.md section 3) and the
Oblation plaza photograph (UPM Oblation and PGH, July 2023):
  * rizal_stucco: the ivory walls. Warm cream with two broad, soft coats: a yellowed coat where
    old paint shows and a paler touch-up coat. No lines at all: the walls are plain render.
  * rizal_trim: the mouldings, bands, entablature and entrance frame. Paler and cooler than
    the walls, as the photographs show, with soft HORIZONTAL washes, as water runs along a
    ledge.
  * rizal_column: the Ionic shafts. The palest cream, with soft VERTICAL washes, so a column
    reads as one painted round and never as the wall.
  * rizal_plinth: the base course. A darker warm grey-ochre with damp patches.
  * rizal_steps: the front steps and the portico floor. Warm grey concrete, paler where feet
    have worn it, with faint tread joints.
  * rizal_roof: the red-brown long-span metal roof seen from the air in the campus aerials.
    Soft corrugation ribs every 0.25 m down the slope (normal map mostly), broad sun-bleached
    and rain-darkened washes; the model blends a second, rescaled sample through a big noise
    mask so the 4 m repeat never lines up. Hue about 10 degrees, dark and muted: nowhere near offence
    orange #f87020.
  * rizal_eave: the eave soffit boards over the exposed rafters. Pale warm grey, faint boards.
  * rizal_paint: NEUTRAL painted metal for the iron railings, the gutters and the aircon
    casings; each material tints it. One soft sheen, a few small soft chips.
  * rizal_bronze: the Oblation. Dark bronze with big soft verdigris patches.
  * rizal_stone: the Oblation's drum. Warm grey granite as a flat fill with soft patches.
  * rizal_sash (not tiling): one steel-sash window per quarter of the image, four variants:
    thick dark mullions, dark glass panes, a few paler panes (opened, a curtain, the sky).
    Mapped 0..1 per window.
  * rizal_door (not tiling): a dark varnished double door with chunky panels.
DIRT AND GRIME, positional multiplier overlays (white means no change), like the LRT kit's but
drawn for a stucco building:
  * rizal_grime_sill: a strip 4 m wide per window BAY, four variants side by side, and two rows:
    row 0 hangs under a SILL (a soft band the width of the sill and one to three stain tongues,
    as rain and aircon drip leave under Manila windows), row 1 hangs under the CORNICE (a
    continuous band with wider tongues). v runs 4.5 m down from the edge.
  * rizal_grime_splash: 8 m wide, 1.5 m up from the ground: a soil splash band with a ragged
    soft top, warmer and browner than the LRT's road splash, since this sits over garden beds.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
from author_ilalim_textures import OUT, SIZE, TILE_M, PX, X, Y, coat, flat, hexcol, smooth  # noqa: E402

SHEETS = Path(__file__).resolve().parents[1] / "Logs" / "ilalim-blender"
NEUTRAL = {"rizal_paint"}


def normal_from_height(h, strength):
    gy, gx = np.gradient(h * strength * 8)
    n = np.dstack((-gx, gy, np.ones_like(h)))
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n * 0.5 + 0.5


def save(name, albedo, height=None, strength=0.6):
    OUT.mkdir(parents=True, exist_ok=True)
    albedo = np.clip(albedo, 0, 1)
    if height is None:
        height = np.zeros(albedo.shape[:2])
    span = np.ptp(height)
    h = (height - height.min()) / span if span > 1e-9 else np.full_like(height, 0.5)
    Image.fromarray((albedo * 255).astype(np.uint8)).save(OUT / f"{name}_albedo.png")
    Image.fromarray((normal_from_height(h, strength) * 255).astype(np.uint8)).save(OUT / f"{name}_normal.png")
    print("[rizal-tex]", name)


def soft_lines(spacing_m, coord, width_m, seed, wobble_m=0.01):
    """Soft, slightly wandering lines every `spacing_m` along `coord` (an array of metres)."""
    drift = smooth(1.2, seed)
    d = (coord + drift * wobble_m) % spacing_m
    d = np.minimum(d, spacing_m - d)
    return np.exp(-(d / width_m) ** 2)


# ------------------------------------------------------------------ tiling surfaces

def rizal_stucco():
    # A little warmer and deeper than the columns and trim, so the white colonnade stands off
    # the wall from the court (review v1: they merged at 80 m).
    img = flat("e6d6b2")
    # A yellowed old coat, broad; a paler touch-up coat; a faint greyed coat low down is left to
    # the positional grime, not the tile.
    img = coat(img, (0.965, 0.955, 0.925), 1.5, 0.30, seed=501, feather=1.3)
    img = coat(img, (1.022, 1.02, 1.015), 1.0, 0.18, seed=502, feather=1.0)
    save("rizal_stucco", img, None)


def rizal_trim():
    img = flat("efe8d6")
    img = coat(img, (0.965, 0.962, 0.95), 1.2, 0.26, seed=511, feather=1.4, stretch=(1.0, 0.35))
    img = coat(img, (1.015, 1.015, 1.012), 0.8, 0.16, seed=512, feather=1.0)
    save("rizal_trim", img, None)


def rizal_column():
    img = flat("f2ecdc")
    img = coat(img, (0.975, 0.97, 0.96), 1.1, 0.22, seed=521, feather=1.7, stretch=(0.45, 1.0))
    img = coat(img, (1.012, 1.012, 1.01), 1.1, 0.16, seed=522, feather=1.1)
    save("rizal_column", img, None)


def rizal_plinth():
    img = flat("a39a86")
    img = coat(img, (0.93, 0.93, 0.92), 1.1, 0.26, seed=531, feather=1.2)
    img = coat(img, (1.04, 1.035, 1.02), 0.8, 0.14, seed=532, feather=1.0)
    save("rizal_plinth", img, None)


def rizal_steps():
    img = flat("aca69a")
    # Worn paler centres, broad and soft.
    img = coat(img, (1.05, 1.05, 1.045), 1.3, 0.24, seed=541, feather=1.4)
    img = coat(img, (0.95, 0.945, 0.935), 0.9, 0.16, seed=542, feather=1.0)
    joints = soft_lines(1.0, X, 0.008, seed=543)
    img = img * (1 - 0.05 * joints[..., None])
    save("rizal_steps", img, -joints, strength=0.5)


def rizal_roof():
    img = flat("8a4434")
    # Sun-bleached patches, broad and pinkish-pale; darker rain-soaked patches; few rust patches.
    img = coat(img, (1.07, 1.05, 1.045), 1.8, 0.40, seed=551, feather=1.8, stretch=(0.6, 1.0))
    img = coat(img, (0.96, 0.955, 0.955), 1.5, 0.22, seed=552, feather=1.6, stretch=(0.6, 1.0))
    # No rust patches in the tile: at 0.6 m and even at 1.5 m they read as polka dots from the
    # air once repeated (reviews v1 and v4).
    # Corrugation ribs run DOWN the slope (texture v); they vary with u (X).
    ribs = soft_lines(0.25, X, 0.028, seed=554, wobble_m=0.004)
    img = img * (1 + 0.035 * ribs[..., None])
    save("rizal_roof", img, ribs, strength=0.9)


def rizal_eave():
    img = flat("d6cfbf")
    img = coat(img, (0.95, 0.945, 0.935), 1.2, 0.26, seed=561, feather=1.3)
    boards = soft_lines(0.3, Y, 0.01, seed=562)
    img = img * (1 - 0.04 * boards[..., None])
    save("rizal_eave", img, -boards, strength=0.4)


def rizal_paint():
    img = flat("e0e0e0")
    img = coat(img, (1.03, 1.03, 1.03), 0.9, 0.24, seed=571, feather=1.2)
    img = coat(img, (0.97, 0.97, 0.97), 1.3, 0.18, seed=572, feather=1.2)
    img = coat(img, (0.93, 0.89, 0.84), 0.06, 0.02, seed=573, feather=0.45)
    save("rizal_paint", img, None)


def rizal_bronze():
    img = flat("4e4536")
    # Verdigris in a few big soft patches, where rain runs; low contrast, never camouflage.
    img = coat(img, (1.08, 1.20, 1.12), 1.1, 0.20, seed=581, feather=1.4)
    img = coat(img, (1.06, 1.05, 1.02), 0.8, 0.10, seed=582, feather=1.0)   # worn highlights
    save("rizal_bronze", img, None)


def rizal_stone():
    img = flat("aaa598")
    img = coat(img, (0.965, 0.965, 0.955), 1.4, 0.24, seed=591, feather=1.5)
    img = coat(img, (1.025, 1.025, 1.02), 0.9, 0.14, seed=592, feather=1.1)
    save("rizal_stone", img, None)


# ------------------------------------------------------------------ per-window drawings

def _rect(a, x0, y0, x1, y1, colour, feather=1.5):
    """Draw a soft-edged flat rectangle (pixel coordinates) into image array `a`."""
    h, w = a.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    m = np.clip(np.minimum.reduce([xx - x0, x1 - xx, yy - y0, y1 - yy]) / feather + 0.5, 0, 1)
    a[:] = a * (1 - m[..., None]) + np.asarray(colour) * m[..., None]


def rizal_sash():
    """Four steel-sash windows side by side, each 1.7 m by 2.5 m at 150 px per metre."""
    ppm = 150
    w, h = int(1.7 * ppm), int(2.5 * ppm)
    frame = hexcol("2a2e2b")
    glass = hexcol("3b464c")
    img = np.zeros((h, 4 * w, 3))
    rng = np.random.default_rng(601)
    pale = [hexcol("6f7b80"), hexcol("8a8676"), hexcol("5b676d")]
    for v in range(4):
        a = np.ones((h, w, 3)) * frame
        cols, rows = 4, 6
        mull = 0.075 * ppm
        cw = (w - mull * (cols + 1)) / cols
        rh = (h - mull * (rows + 1)) / rows
        for i in range(cols):
            for j in range(rows):
                x0 = mull + i * (cw + mull)
                y0 = mull + j * (rh + mull)
                c = glass * rng.uniform(0.94, 1.06)
                # A few paler panes per variant: an opened pane, a curtain, the sky.
                if (v == 1 and j >= 4) or (v == 2 and i in (1, 2) and 1 <= j <= 3) or rng.random() < 0.06:
                    c = pale[(v + i + j) % 3]
                _rect(a, x0, y0, x0 + cw, y0 + rh, c)
        # One broad soft reflection over the upper panes: a flat lighter band, not a gradient.
        yy, xx = np.mgrid[0:h, 0:w]
        band = np.clip(1 - np.abs((xx * 0.6 + yy) - (0.55 + 0.1 * v) * h) / (0.16 * h), 0, 1)
        band = np.clip(band * 3 - 1.2, 0, 1) * 0.10 * (yy < 0.6 * h)
        a = a * (1 + band[..., None])
        img[:, v * w:(v + 1) * w] = a
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(OUT / "rizal_sash_albedo.png")
    print("[rizal-tex] rizal_sash")


def rizal_door():
    ppm = 150
    w, h = int(2.4 * ppm), int(3.4 * ppm)
    a = np.ones((h, w, 3)) * hexcol("4a3226")
    wood = hexcol("5d3f2d")
    # Two leaves, each with three chunky raised panels, and a fanlight grille at the top.
    for leaf in range(2):
        x0 = leaf * w / 2
        for k, (py0, py1) in enumerate(((0.06, 0.24), (0.30, 0.62), (0.68, 0.94))):
            _rect(a, x0 + 0.12 * w / 2, py0 * h, x0 + 0.88 * w / 2, py1 * h, wood * (1.0 + 0.04 * k), 2.0)
    _rect(a, w / 2 - 3, 0, w / 2 + 3, h, hexcol("2e1f17"), 1.0)
    a = coat_small(a, 611)
    Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8)).save(OUT / "rizal_door_albedo.png")
    print("[rizal-tex] rizal_door")


def coat_small(a, seed):
    """One broad soft darker patch over a small non-tiling image."""
    h, w = a.shape[:2]
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:h, 0:w]
    cx, cy = rng.uniform(0.3, 0.7) * w, rng.uniform(0.6, 0.9) * h
    d = np.hypot((xx - cx) / w, (yy - cy) / h)
    m = np.clip((0.35 - d) / 0.12, 0, 1)
    return a * (1 - 0.08 * m[..., None])


# ------------------------------------------------------------------ positional grime

def _noise1d(n, scale_px, seed):
    white = np.random.default_rng(seed).standard_normal(n)
    f = np.fft.fftfreq(n)
    s = np.real(np.fft.ifft(np.fft.fft(white) * np.exp(-(f * scale_px) ** 2 * 2)))
    return (s - s.mean()) / (s.std() + 1e-9)


def _tongue(u, v, u0, width, length, phase):
    """A DRAWN stain tongue: a flat fill whose width wobbles and narrows to a rounded end, with a
    narrow soft edge. u, v in metres; v runs down from the edge."""
    centre = u0 + 0.04 * np.sin(v * 1.6 + phase)
    du = np.abs(u - centre)
    taper = np.clip(1 - v / length, 0, 1) ** 0.6
    wob = 1 + 0.07 * np.sin(v * 3.1 + phase * 2)
    half = width * (0.5 + 0.5 * taper) * wob
    end = np.sqrt(np.clip(1 - ((v - (length - width)) / width) ** 2, 0, 1))
    half = np.where(v > length - width, half * end, half)
    return np.clip((half - du) / 0.03 + 0.5, 0, 1) * (v < length)


def rizal_grime_sill():
    BAY, H, ppm = 4.0, 4.5, 100
    w, h = int(BAY * ppm), int(H * ppm)
    v, u = np.mgrid[0:h, 0:w] / ppm            # v: metres down from the edge; u: 0..4 m, window at 2
    out = np.ones((2 * h, 4 * w, 3))
    rng = np.random.default_rng(621)
    stain = np.array([0.76, 0.74, 0.68])
    rust = np.array([0.86, 0.78, 0.68])
    for row in range(2):
        for var in range(4):
            if row == 0:
                side_w = rng.uniform(0.95, 1.1)
            a = np.ones((h, w, 3))
            if row == 0:
                # The band under the sill: as wide as the sill, its lower edge wobbling.
                edge = 0.16 + 0.06 * _noise1d(w, 0.4 * ppm, 630 + var)[None, :]
                side = np.clip((side_w - np.abs(u - 2.0)) / 0.08 + 0.5, 0, 1)
                band = np.clip((edge - v) / 0.06 + 0.5, 0, 1) * side
                a *= 1 - 0.2 * band[..., None]
                # Some bays stay clean; the rest carry one to three tongues of different lengths,
                # so the facade never reads stamped (review v3).
                count = rng.choice([0, 1, 2, 3], p=[0.2, 0.35, 0.3, 0.15])
                spots = rng.uniform(1.0, 3.0, count)
            else:
                # Under the cornice: a continuous band, and broader tongues.
                edge = 0.35 + 0.1 * _noise1d(w, 0.6 * ppm, 640 + var)[None, :]
                band = np.clip((edge - v) / 0.08 + 0.5, 0, 1)
                a *= 1 - 0.14 * band[..., None]
                count = rng.integers(2, 4)
                spots = rng.uniform(0.5, 3.5, count)
            for u0 in spots:
                width = rng.uniform(0.10, 0.26) * (1.4 if row else 1.0)
                length = rng.choice([rng.uniform(0.5, 1.1), rng.uniform(1.1, 2.4)], p=[0.55, 0.45])
                col = rust if rng.random() < 0.3 else stain
                m = _tongue(u, v, u0, width, length, rng.uniform(0, 6.28)) * rng.uniform(0.6, 0.9)
                a *= 1 - m[..., None] * (1 - col)
            # Keep the last rows and the side columns clean, so clamped UVs read white.
            a[-3:] = 1
            a[:, :2] = 1
            a[:, -2:] = 1
            out[row * h:(row + 1) * h, var * w:(var + 1) * w] = a
    # Row index down the image is v; flip so that UV v = 0 (the image bottom in Blender) is the
    # sill edge of row 0.
    img = np.flipud(np.clip(out, 0, 1))
    Image.fromarray((img * 255).astype(np.uint8)).save(OUT / "rizal_grime_sill.png")
    print("[rizal-tex] rizal_grime_sill")


def rizal_grime_splash():
    W_M, H_M, ppm = 8.0, 1.5, 150
    w, h = int(W_M * ppm), int(H_M * ppm)
    v, u = np.mgrid[0:h, 0:w] / ppm           # v: metres UP from the ground
    a = np.ones((h, w, 3))
    top = 0.42 + 0.14 * _noise1d(w, 0.6 * ppm, 651)[None, :]
    band = np.clip((top - v) / 0.1 + 0.5, 0, 1)
    a *= 1 - band[..., None] * (1 - np.array([0.80, 0.76, 0.70]))
    rng = np.random.default_rng(652)
    for k in range(10):
        u0 = rng.uniform(0, W_M)
        width = rng.uniform(0.1, 0.35)
        length = rng.uniform(0.55, 0.95)
        du = np.abs(((u - u0 + W_M / 2) % W_M) - W_M / 2)
        m = np.clip((width * np.clip(1 - v / length, 0, 1) ** 0.5 - du) / 0.03 + 0.5, 0, 1) * (v < length - 0.02)
        m = m * rng.uniform(0.3, 0.6)
        a *= 1 - m[..., None] * (1 - np.array([0.86, 0.82, 0.76]))
    a[-3:] = 1
    img = np.flipud(np.clip(a, 0, 1))          # UV v = 0 is the ground
    Image.fromarray((img * 255).astype(np.uint8)).save(OUT / "rizal_grime_splash.png")
    print("[rizal-tex] rizal_grime_splash")


PAINTERS = [rizal_stucco, rizal_trim, rizal_column, rizal_plinth, rizal_steps, rizal_roof, rizal_eave,
            rizal_paint, rizal_bronze, rizal_stone]
OTHERS = [rizal_sash, rizal_door, rizal_grime_sill, rizal_grime_splash]


def swatch_sheet(version):
    SHEETS.mkdir(parents=True, exist_ok=True)
    names = [p.__name__ for p in PAINTERS]
    cell, pad = 300, 16
    cols = 5
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (cell + pad) + pad, rows * (cell + 40) + pad + 560), (245, 242, 235))
    draw = ImageDraw.Draw(sheet)
    for k, name in enumerate(names):
        tile = Image.open(OUT / f"{name}_albedo.png").convert("RGB")
        if name in NEUTRAL:
            a = np.asarray(tile) / 255 * hexcol("2c3230") * 1.3
            tile = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
        rep = Image.new("RGB", (SIZE * 2, SIZE * 2))
        for i in range(2):
            for j in range(2):
                rep.paste(tile, (i * SIZE, j * SIZE))
        rep = rep.resize((cell, cell), Image.LANCZOS)
        x, y = pad + (k % cols) * (cell + pad), pad + (k // cols) * (cell + 40)
        sheet.paste(rep, (x, y))
        draw.text((x, y + cell + 6), f"{name} (8 m square)", fill=(40, 40, 40))
    y0 = pad + rows * (cell + 40)
    sash = Image.open(OUT / "rizal_sash_albedo.png").convert("RGB")
    sash.thumbnail((700, 260))
    sheet.paste(sash, (pad, y0))
    door = Image.open(OUT / "rizal_door_albedo.png").convert("RGB")
    door.thumbnail((200, 260))
    sheet.paste(door, (pad + 720, y0))
    # The grime overlays shown over the stucco, edge at the top.
    base = np.asarray(Image.open(OUT / "rizal_stucco_albedo.png").convert("RGB"), dtype=float) / 255
    over = np.flipud(np.asarray(Image.open(OUT / "rizal_grime_sill.png").convert("RGB"), dtype=float) / 255)
    reps = np.tile(base, (over.shape[0] // SIZE + 1, over.shape[1] // SIZE + 1, 1))[:over.shape[0], :over.shape[1]]
    shown = Image.fromarray((np.clip(reps * over, 0, 1) * 255).astype(np.uint8))
    shown.thumbnail((900, 280))
    sheet.paste(shown, (pad + 940, y0))
    sp = np.asarray(Image.open(OUT / "rizal_grime_splash.png").convert("RGB"), dtype=float) / 255
    reps = np.tile(base, (1, sp.shape[1] // SIZE + 1, 1))[:sp.shape[0], :sp.shape[1]]
    shown = Image.fromarray((np.clip(reps * sp, 0, 1) * 255).astype(np.uint8))
    shown.thumbnail((900, 200))
    sheet.paste(shown, (pad, y0 + 290))
    path = SHEETS / f"rizal_swatches_v{version}.png"
    sheet.save(path)
    print("[rizal-tex] sheet", path)


def main():
    for paint in PAINTERS + OTHERS:
        paint()
    version = int(sys.argv[sys.argv.index("--sheet") + 1]) if "--sheet" in sys.argv else 1
    swatch_sheet(version)


if __name__ == "__main__":
    main()
