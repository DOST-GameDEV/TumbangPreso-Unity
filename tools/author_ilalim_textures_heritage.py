"""Paint the heritage campus kit's textures for the Ilalim ng Tulay rebuild (ILALIM-1.3, heritage kit).

  py -3 tools/author_ilalim_textures_heritage.py [--sheet N]

Writes ArtSource/ilalim/textures/heritage_*.png and a swatch sheet
Logs/ilalim-blender/heritage_swatches_vN.png. The models are tools/author_ilalim_heritage.py.

THE STYLE is the house style of Kanto, the Lagoon and the LRT kit (tools/author_ilalim_textures.py):
FLAT fills, a few LARGE patches with FEATHERED organic edges, low contrast, no grain, no noise, no
streaks. Every surface has ITS OWN drawing below; none is another surface's generator in
disguise. The shared helpers (smooth, coat, wobbly_lines) come read-only from the LRT script.

THE REFERENCES (Wikimedia Commons, studied only): the PGH front with the Oblation, the PGH
Nurses Home, the Supreme Court corner from the LRT, the campus aerial. What they show:
  * the walls are old lime stucco in cream, butter or dusty pink, repainted in big patches, the
    ground storey often RUSTICATED with deep horizontal channels;
  * the roofs are painted rib-profile metal, red-brown, bleached pink in the sun and darker where
    rust and moss sit; the Supreme Court roofs are pale sage-green metal with standing seams;
  * the windows are dark steel sash with small panes, or glass jalousies;
  * rain leaves dark tongues under the cornices and under every window sill, and the plinth
    carries a greenish rising-damp band.

TILING textures (4 m, world UVs, NEUTRAL ones are tinted per building by the material):
  * heritage_stucco   neutral wall stucco: two soft repaint coats and one patch coat.
  * heritage_rustic   neutral rusticated ground storey: soft horizontal channels every 0.5 m,
                      short staggered joints, each block a hair different in tone.
  * heritage_trim     neutral trim paint: nearly flat, one very soft coat.
  * heritage_plinth   warm grey plinth render with big damp patches.
  * heritage_roof_red red-brown rib metal: ribs down the slope every 0.25 m, sheet laps every
                      2 m, sun-bleached and rust patches.
  * heritage_roof_sage the court roofs: pale sage standing seams every 0.6 m, chalky bleaching.
  * heritage_soffit   the eave soffit: cream tongue-and-groove boards along the eave.
PER-OPENING textures (UV 0..1 over one opening):
  * heritage_window_sash  dark steel sash, 3 x 4 panes and a top awning row.
  * heritage_window_jal   glass jalousies: pale green-grey slats in a dark frame.
  * heritage_door         a panelled double door in dark varnished narra under a fanlight.
  * heritage_ac_front     the louvred front of a window aircon unit.
MULTIPLIER overlays (white = no change), each its own drawing:
  * heritage_grime_eave   8 m x 3 m below the cornice: a soft shadow band and rain tongues.
  * heritage_grime_sill   4 window bays x 2.5 m below the sills: tongues centred under each
                          window, one variant per bay, so the stains line up with the windows.
  * heritage_grime_plinth 8 m x 1.5 m up from the ground: a green-grey rising-damp band.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_ilalim_textures as T  # noqa: E402  (read-only helpers: smooth, coat, flat, wobbly_lines)

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "ArtSource" / "ilalim" / "textures"
SHEETS = ROOT / "Logs" / "ilalim-blender"
SIZE, PX = T.SIZE, T.PX
X, Y = T.X, T.Y
hexcol, coat, flat, smooth, wobbly_lines = T.hexcol, T.coat, T.flat, T.smooth, T.wobbly_lines
STRENGTH = {}


def save(name, albedo, height=None, strength=0.6):
    OUT.mkdir(parents=True, exist_ok=True)
    albedo = np.clip(albedo, 0, 1)
    Image.fromarray((albedo * 255).astype(np.uint8)).save(OUT / f"{name}_albedo.png")
    if height is None:
        height = np.zeros(albedo.shape[:2])
    span = np.ptp(height)
    h = (height - height.min()) / span if span > 1e-9 else np.full_like(height, 0.5)
    Image.fromarray((h * 255).astype(np.uint8)).save(OUT / f"{name}_height.png")
    Image.fromarray((T.normal_from_height(h, strength) * 255).astype(np.uint8)).save(OUT / f"{name}_normal.png")
    STRENGTH[name] = strength
    print("[heritage-tex]", name)


# ------------------------------------------------------------------ tiling surfaces

def heritage_stucco():
    # Old lime stucco, repainted in big patches over a century. Neutral near-white: every
    # building multiplies its own colour in. Three coats: a broad lighter one, a broad darker
    # one, and a smaller patch coat with a tighter edge where a repair was painted over.
    img = flat("f4f1ea")
    img = coat(img, (1.02, 1.018, 1.015), 2.2, 0.30, seed=501, feather=1.5)
    img = coat(img, (0.975, 0.97, 0.962), 1.8, 0.24, seed=502, feather=1.4)
    # One repair patch coat, big and faint (review v1: 0.5 m patches read as camouflage spots).
    img = coat(img, (0.988, 0.983, 0.975), 1.2, 0.10, seed=503, feather=0.9)
    save("heritage_stucco", img, None, 0.3)


def heritage_rustic():
    # The rusticated ground storey: deep horizontal channels every 0.5 m (the one strong drawn
    # mark), short vertical joints staggered course by course, and each block a hair lighter or
    # darker, as hand-applied render is. Not the brick generator: blocks are 1.2 m long, the
    # vertical joints are faint, and the channels carry the shadow.
    img = flat("f2eee6")
    course = 0.5
    rows = np.floor(Y / course).astype(int)
    lines = wobbly_lines(course, "y", 0.022, 0.012, seed=511)
    # Staggered vertical joints, 1.2 m apart, offset half a block on alternate courses.
    xoff = (X + (rows % 2) * 0.6) % 1.2
    dv = np.minimum(xoff, 1.2 - xoff)
    vj = np.exp(-(dv / 0.010) ** 2)
    block = ((np.floor((X + (rows % 2) * 0.6) / 1.2).astype(int) * 7 + rows * 13) % 5)
    tone = np.array([0.994, 1.0, 1.007, 0.997, 1.004])[block]
    img = img * tone[..., None]
    img = coat(img, (0.97, 0.965, 0.955), 1.4, 0.25, seed=512, feather=1.2)
    img = img * (1 - 0.16 * lines[..., None]) * (1 - 0.05 * vj[..., None])
    save("heritage_rustic", img, -(lines + 0.4 * vj), 0.9)


def heritage_trim():
    # Mouldings, sills, pilasters and quoins: a fresher coat than the walls, nearly flat.
    img = flat("f7f5ef")
    img = coat(img, (0.975, 0.972, 0.965), 1.5, 0.22, seed=521, feather=1.4)
    save("heritage_trim", img, None, 0.2)


def heritage_plinth():
    # The plinth: a warm grey cement render, darker than the wall, with big soft damp patches in
    # a greenish grey. The splash band is positional (heritage_grime_plinth), not in here.
    img = flat("a39b8c")
    img = coat(img, (0.93, 0.95, 0.92), 1.2, 0.28, seed=531, feather=1.2)
    img = coat(img, (1.04, 1.035, 1.025), 0.9, 0.18, seed=532, feather=1.0)
    save("heritage_plinth", img, None, 0.3)


def heritage_roof_red():
    # Painted rib-profile metal, red-brown as the PGH roofs are from the air. The ribs run DOWN
    # the slope (UV v is down-slope on the roof faces): soft, low-contrast lines every 0.25 m.
    # Sheet laps every 2 m across the slope. Big sun-bleached patches (pinker, lighter) and a
    # few darker rust-and-moss patches. Hue ~10 degrees and dark: well clear of offence orange.
    img = flat("823b2e")
    img = coat(img, (1.09, 1.055, 1.04), 1.8, 0.28, seed=541, feather=1.4)
    # Rust and moss: few, big and faint (review v1: darker, smaller ones read as leopard spots).
    img = coat(img, (0.93, 0.92, 0.91), 1.5, 0.15, seed=542, feather=1.3)
    ribs = wobbly_lines(0.25, "x", 0.018, 0.006, seed=544)
    laps = wobbly_lines(2.0, "y", 0.012, 0.03, seed=545)
    img = img * (1 + 0.10 * ribs[..., None]) * (1 - 0.08 * laps[..., None])
    save("heritage_roof_red", img, ribs - 0.5 * laps, 0.8)


def heritage_roof_sage():
    # The Supreme Court roofs: pale sage-green metal with STANDING SEAMS every 0.6 m (a lighter
    # line with a soft shadow on one side), chalky bleached patches, and a little rust bloom.
    img = flat("a8b29b")
    img = coat(img, (1.07, 1.06, 1.05), 1.8, 0.30, seed=551, feather=1.4)
    img = coat(img, (0.93, 0.94, 0.92), 1.2, 0.20, seed=552, feather=1.1)
    seam = wobbly_lines(0.6, "x", 0.016, 0.004, seed=554)
    shade = np.roll(seam, int(0.03 * PX), axis=1)
    img = img * (1 + 0.12 * seam[..., None]) * (1 - 0.08 * shade[..., None])
    save("heritage_roof_sage", img, seam, 0.8)


def heritage_soffit():
    # The underside of the deep eaves: painted boards, cream, running along the eave (UV u),
    # 0.15 m wide, the joints faint, one big soft damp patch.
    img = flat("ebe3cf")
    boards = wobbly_lines(0.15, "y", 0.008, 0.004, seed=561)
    img = coat(img, (0.965, 0.96, 0.95), 1.8, 0.22, seed=562, feather=1.5)
    img = img * (1 - 0.09 * boards[..., None])
    save("heritage_soffit", img, -boards, 0.5)


# ------------------------------------------------------------------ per-opening drawings

def _canvas(n=512):
    v, u = np.mgrid[0:n, 0:n] / n      # u across, v DOWN from the top
    return u, v


def _bars(coord, centres, half):
    d = np.min(np.abs(coord[..., None] - np.asarray(centres)[None, None, :]), axis=-1)
    return np.clip((half - d) / 0.006 + 0.5, 0, 1)


def _finish(name, img):
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(OUT / f"{name}_albedo.png")
    print("[heritage-tex]", name)


def heritage_window_sash():
    # Dark steel sash, as on every Parsons building: deep teal-grey glass, near-black bars in a
    # 3 x 4 grid under a top awning row, and ONE big soft lighter patch where the sky reflects.
    u, v = _canvas()
    glass = np.ones(u.shape + (3,)) * hexcol("45595a")
    sky = np.exp(-(((u - 0.3) / 0.35) ** 2 + ((v - 0.25) / 0.3) ** 2))
    glass = glass * (1 + 0.45 * sky[..., None])
    bars = np.maximum(_bars(u, [0.0, 1 / 3, 2 / 3, 1.0], 0.022), _bars(v, [0.0, 0.2, 0.4, 0.6, 0.8, 1.0], 0.018))
    awning = _bars(v, [0.2], 0.03)
    frame = hexcol("1d2321")
    img = glass * (1 - bars[..., None]) + frame * bars[..., None]
    img = img * (1 - 0.25 * awning[..., None])
    _finish("heritage_window_sash", img)


def heritage_window_jal():
    # Glass jalousies: horizontal slats of pale green-grey frosted glass, each slat lighter at
    # its top edge, in a dark frame with one centre mullion.
    u, v = _canvas()
    n = 14
    f = (v * n) % 1
    slat = hexcol("8e9f96") * (0.9 + 0.18 * (1 - f))[..., None]
    gap = np.clip((f - 0.86) / 0.04, 0, 1)
    img = slat * (1 - 0.45 * gap[..., None])
    bars = np.maximum(_bars(u, [0.0, 0.5, 1.0], 0.03), _bars(v, [0.0, 1.0], 0.03))
    img = img * (1 - bars[..., None]) + hexcol("262b29") * bars[..., None]
    _finish("heritage_window_jal", img)


def heritage_door():
    # A panelled double door in dark varnished narra under a glazed fanlight. Panels are drawn
    # as soft lighter fields with a dark inset line, not carved geometry.
    u, v = _canvas()
    wood = hexcol("5b3b29")
    img = np.ones(u.shape + (3,)) * wood
    img = coat_small(img, u, v)
    fan = v < 0.22
    img[fan] = hexcol("33423f") * (1 + 0.3 * np.exp(-((u[fan] - 0.4) / 0.3) ** 2))[..., None]
    for x0, x1 in ((0.08, 0.44), (0.56, 0.92)):
        for y0, y1 in ((0.28, 0.58), (0.64, 0.92)):
            inside = (u > x0) & (u < x1) & (v > y0) & (v < y1)
            edge = inside & ((u < x0 + 0.02) | (u > x1 - 0.02) | (v < y0 + 0.02) | (v > y1 - 0.02))
            img[inside] *= 1.12
            img[edge] *= 0.7
    bars = np.maximum(_bars(u, [0.0, 0.5, 1.0], 0.025), _bars(v, [0.0, 0.22, 1.0], 0.02))
    img = img * (1 - 0.5 * bars[..., None])
    _finish("heritage_door", img)


def coat_small(img, u, v):
    # One soft lighter wash across the door leaves, so the varnish is not a dead flat fill.
    wash = np.exp(-(((u - 0.35) / 0.4) ** 2 + ((v - 0.5) / 0.5) ** 2))
    return img * (1 + 0.12 * wash[..., None])


def heritage_ac_front():
    # The front grille of a window aircon: an off-white casing face with a recessed panel of
    # chunky horizontal louvres on the left two thirds and a plain control strip on the right.
    u, v = _canvas(256)
    img = np.ones(u.shape + (3,)) * hexcol("e4e1d8")
    grille = (u > 0.07) & (u < 0.66) & (v > 0.12) & (v < 0.88)
    f = (v * 10) % 1
    lou = 0.78 + 0.16 * f
    img[grille] = (hexcol("b9b6ad") * lou[grille][..., None])
    ctrl = (u > 0.74) & (u < 0.93) & (v > 0.2) & (v < 0.4)
    img[ctrl] = hexcol("8d8a84")
    _finish("heritage_ac_front", img)


# ------------------------------------------------------------------ positional grime overlays

def _mult(name, a):
    """Row 0 of `a` is the EDGE the stain starts from; it is flipped so UV v = 0 is that edge."""
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.flipud(np.clip(a, 0, 1)) * 255).astype(np.uint8)).save(OUT / f"{name}.png")
    print("[heritage-tex]", name)


def _tongue(u, v, centre, width, length, rng, feather=0.03):
    """One drawn stain tongue hanging from v = 0: flat inside, narrowing to a rounded end."""
    phase = rng.uniform(0, 6.28)
    c = centre + 0.04 * np.sin(v * 1.7 + phase)
    du = np.abs(u - c)
    taper = np.clip(1 - v / length, 0, 1) ** 0.6
    half = width * (0.4 + 0.6 * taper) * (1 + 0.07 * np.sin(v * 4.0 + phase))
    end = np.sqrt(np.clip(1 - ((v - (length - width)) / width) ** 2, 0, 1))
    half = np.where(v > length - width, half * end, half)
    return np.clip((half - du) / feather + 0.5, 0, 1) * (v < length)


def heritage_grime_eave():
    # Under the deep eaves: a soft darker band where the eave shadow and splash-back sit (it
    # wobbles, it is not ruled), and rain tongues from the cornice, grey-brown and a few green.
    W, H, ppm = 8.0, 3.0, 160
    v, u = np.mgrid[0:int(H * ppm), 0:int(W * ppm)] / ppm
    a = np.ones(v.shape + (3,))
    edge = 0.35 + 0.1 * T._noise1d(u.shape[1], 1.0 * ppm, 571)[None, :]
    band = np.clip((edge - v) / 0.15 + 0.5, 0, 1)
    a *= 1 - 0.12 * band[..., None]
    rng = np.random.default_rng(572)
    cols = [np.array([0.82, 0.80, 0.76])] * 4 + [np.array([0.82, 0.84, 0.76])] * 2
    # Few and faint: at twelve strong tongues the top storey read as vertical streaks (review v1).
    for _ in range(7):
        centre = rng.uniform(0.3, W - 0.3)
        m = _tongue(u, v, centre, rng.uniform(0.15, 0.4), rng.uniform(0.5, 1.8), rng) * rng.uniform(0.25, 0.5)
        col = cols[rng.integers(len(cols))]
        a *= 1 - m[..., None] * (1 - col)
    # Wrap the tongues so the 8 m tile repeats cleanly.
    _mult("heritage_grime_eave", a)


def heritage_grime_sill():
    # Under every window sill: one or two tongues per window, CENTRED on the window (the model
    # maps u = bay position, one bay per unit, window in the middle, and picks one of the four
    # variants per bay). Short and soft; the odd one runs down to the next string course.
    BAYS, H, ppm = 4, 2.5, 200
    v, u = np.mgrid[0:int(H * ppm), 0:int(BAYS * ppm)] / ppm
    a = np.ones(v.shape + (3,))
    rng = np.random.default_rng(581)
    for bay in range(BAYS):
        n = rng.choice([1, 2, 2, 3])
        for _ in range(n):
            centre = bay + 0.5 + rng.uniform(-0.2, 0.2)
            m = _tongue(u, v, centre, rng.uniform(0.05, 0.12), rng.choice([rng.uniform(0.4, 0.9), rng.uniform(1.0, 2.2)]),
                        rng, feather=0.02) * rng.uniform(0.35, 0.7)
            a *= 1 - m[..., None] * (1 - np.array([0.80, 0.78, 0.74]))
        # A soft shadow right under the sill, the width of the window.
        under = np.exp(-((u - bay - 0.5) / 0.28) ** 6) * np.clip((0.12 - v) / 0.08, 0, 1)
        a *= 1 - 0.1 * under[..., None]
    _mult("heritage_grime_sill", a)


def heritage_grime_plinth():
    # Rising damp: a green-grey band up from the ground with a ragged, soft top, and a few
    # taller blooms where a downpipe or a planter keeps the wall wet.
    W, H, ppm = 8.0, 1.5, 160
    v, u = np.mgrid[0:int(H * ppm), 0:int(W * ppm)] / ppm      # v: metres UP from the ground
    a = np.ones(v.shape + (3,))
    top = 0.45 + 0.14 * T._noise1d(u.shape[1], 0.6 * ppm, 591)[None, :]
    band = np.clip((top - v) / 0.12 + 0.5, 0, 1)
    a *= 1 - band[..., None] * (1 - np.array([0.84, 0.86, 0.80]))
    rng = np.random.default_rng(592)
    for _ in range(3):
        c = rng.uniform(0.5, W - 0.5)
        r = rng.uniform(0.3, 0.6)
        h = rng.uniform(0.7, 1.2)
        m = np.clip((1 - ((u - c) / r) ** 2 - (v / h) ** 2) / 0.15, 0, 1) * rng.uniform(0.3, 0.5)
        a *= 1 - m[..., None] * (1 - np.array([0.85, 0.87, 0.80]))
    # Row 0 is the ground, the edge this stain grows from, so _mult puts it at UV v = 0.
    _mult("heritage_grime_plinth", a)


TILING = [heritage_stucco, heritage_rustic, heritage_trim, heritage_plinth, heritage_roof_red, heritage_roof_sage,
          heritage_soffit]
OPENINGS = [heritage_window_sash, heritage_window_jal, heritage_door, heritage_ac_front]
OVERLAYS = [heritage_grime_eave, heritage_grime_sill, heritage_grime_plinth]
# How the swatch sheet shows the neutral textures: tinted like the buildings that use them.
PREVIEW_TINT = {"heritage_stucco": "e9dcb4", "heritage_rustic": "dcc3b0", "heritage_trim": "f4efe2"}


def swatch_sheet(version):
    SHEETS.mkdir(parents=True, exist_ok=True)
    cell, pad = 300, 16
    names = [p.__name__ for p in TILING] + [p.__name__ for p in OPENINGS] + [p.__name__ for p in OVERLAYS]
    cols = 7
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (cell + pad) + pad, rows * (cell + 40 + pad) + pad), (245, 242, 235))
    draw = ImageDraw.Draw(sheet)
    base = np.asarray(Image.open(OUT / "heritage_stucco_albedo.png").convert("RGB"), dtype=float) / 255 * hexcol("e9dcb4")
    for k, name in enumerate(names):
        path = OUT / f"{name}_albedo.png"
        if not path.exists():
            path = OUT / f"{name}.png"
        img = np.asarray(Image.open(path).convert("RGB"), dtype=float) / 255
        if name in PREVIEW_TINT:
            img = img * hexcol(PREVIEW_TINT[name])
        if name.startswith("heritage_grime"):
            h, w = img.shape[:2]
            reps = np.tile(base, (h // SIZE + 1, w // SIZE + 1, 1))[:h, :w]
            img = np.clip(reps * img, 0, 1)
            if name != "heritage_grime_plinth":
                img = np.flipud(img)
        tile = Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8))
        if name in [p.__name__ for p in TILING]:
            rep = Image.new("RGB", (tile.width * 2, tile.height * 2))
            for i in range(2):
                for j in range(2):
                    rep.paste(tile, (i * tile.width, j * tile.height))
            tile = rep
        tile.thumbnail((cell, cell), Image.LANCZOS)
        x, y = pad + (k % cols) * (cell + pad), pad + (k // cols) * (cell + 40 + pad)
        sheet.paste(tile, (x, y))
        draw.text((x, y + cell + 6), name, fill=(40, 40, 40))
    path = SHEETS / f"heritage_swatches_v{version}.png"
    sheet.save(path)
    print("[heritage-tex] sheet", path)


def main():
    for paint in TILING + OPENINGS + OVERLAYS:
        paint()
    version = int(sys.argv[sys.argv.index("--sheet") + 1]) if "--sheet" in sys.argv else 1
    swatch_sheet(version)


if __name__ == "__main__":
    main()
