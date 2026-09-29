"""Paint the Ilalim ng Tulay rebuild's tileable textures, in the house illustrated style (ILALIM-1.3).

  py -3 tools/author_ilalim_textures.py              # paint every texture and the swatch sheet
  py -3 tools/author_ilalim_textures.py --normals-only  # rebuild normals from painted heights

Writes ArtSource/ilalim/textures/<name>_albedo.png, _height.png and _normal.png (OpenGL
convention, green up), and Logs/ilalim-blender/<sheet>_swatches_vN.png for review. Every
texture tiles and covers TILE_M metres, and the models use world-scale UVs.

THE STYLE is Kanto's and the Lagoon's (KANTO_DESIGN_GUIDE.md section 3, LAGOON_REWORK_GUIDE.md
section 2): FLAT fills, a few LARGE patches with FEATHERED organic edges, low contrast between
a thing and its joints, no grain, no streaks, no cracks. Every surface gets ITS OWN drawing: the
owner rejected textures that were another surface's generator in disguise. If a change adds
fine noise, streaks or more than two or three value steps, it is going the wrong way.

THE LRT-1 GUIDEWAY SET (first kit, owner 2026-09-29: "give me models for the LRT way"). The
references are in docs/reports/ilalim-rework-2026-09-29/research.md section 4: a narrow grey
concrete box deck with a panelled parapet, a dark flat underside, square piers.
  * lrt_concrete: the cast concrete of the deck sides, parapet panels and piers. A warm light
    grey with two soft coats, and faint wobbly POUR LINES every 2 m, where one lift of
    concrete met the next. They are the one drawn mark, very low contrast.
  * lrt_soffit: the underside the players stand under. Cooler and darker, with big soft damp
    patches and faint longitudinal formwork joints.
  * lrt_track_bed: the slab track on the deck. Darker warm grey, with broad rust-brown patches
    of brake dust.
  * lrt_steel: painted steel for the masts, arms and pipe clamps. NEUTRAL: each material tints
    it. A flat coat with one broad soft sheen and one broad soft shade, nothing else.

DIRT AND GRIME (owner, review of kit v3: "you should take a look at how the lrt way actually
looks. theres no dirt or grime on what you have"). The references (research.md section 8) show
where it sits:
  * dark rain tongues running down from every edge and parapet joint;
  * yellow-brown rust bleeding from bearings and fixings;
  * green-black damp patches where water sits;
  * a sooty band just inside the deck edges on the underside;
  * a splash zone at the foot of every pier.
It is POSITIONAL, so it is not painted into the tiling textures above. It is two MULTIPLIER
overlays (white means no change), mapped by the models' second and third UV maps:
  * grime_drips: 8 m wide, 4 m down from an edge. A soft darker band under the edge, and
    tongues of stain hanging from it, each a soft rounded shape with a feathered edge.
  * grime_splash: 8 m wide, 2 m up from the ground. A dark band with a ragged soft top, and
    short mud tongues reaching up.
Still the house style: big soft drawn shapes at low contrast, never photographic streak noise.
  * lrt_ballast: the gravel bed of the ballasted track LRT-1 actually runs on. Pebbles drawn as
    soft rounded lozenges with one flat highlight, in warm greys with a little rust dust.
"""
import os
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = Path(os.environ.get("ILALIM_TEX_OUT", ROOT / "ArtSource" / "ilalim" / "textures"))
SHEETS = ROOT / "Logs" / "ilalim-blender"
SIZE = 1024
TILE_M = 4.0
PX = SIZE / TILE_M
Y, X = np.mgrid[0:SIZE, 0:SIZE] / PX   # metres
STRENGTH = {"lrt_concrete": 1.2, "lrt_soffit": 0.9, "lrt_track_bed": 0.8, "lrt_steel": 0.5}
NEUTRAL = {"lrt_steel"}


def hexcol(h):
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])


def smooth(scale_m, seed, stretch=(1.0, 1.0)):
    """Periodic smooth noise with features about `scale_m` across. Tiles exactly."""
    white = np.random.default_rng(seed).standard_normal((SIZE, SIZE))
    fy = np.fft.fftfreq(SIZE)[:, None] * stretch[1]
    fx = np.fft.fftfreq(SIZE)[None, :] * stretch[0]
    s = scale_m * PX
    n = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-(fx ** 2 + fy ** 2) * s * s * 2)))
    return (n - n.mean()) / (n.std() + 1e-9)


def coat(img, shift, scale_m, coverage, seed, feather=0.5, stretch=(1.0, 1.0)):
    """A second coat of paint over part of the surface: organic patches with a FEATHERED edge.
    `coverage` 0..1 is how much of the surface they take; `feather` is the edge softness."""
    n = smooth(scale_m, seed, stretch) + 0.12 * smooth(scale_m / 2.5, seed + 1, stretch)
    n = (n - n.mean()) / n.std()
    edge = np.quantile(n, 1 - coverage)
    mask = np.clip((n - edge) / feather + 0.5, 0, 1)
    mask = mask * mask * (3 - 2 * mask)
    return img * (1 + (np.asarray(shift) - 1) * mask[..., None])


def flat(h):
    return np.ones((SIZE, SIZE, 3)) * hexcol(h)


def wobbly_lines(spacing_m, axis, width_m, wobble_m, seed):
    """Soft hand-drawn lines every `spacing_m` along `axis` ('y' draws horizontal lines), each
    drifting by up to `wobble_m` and thickening and thinning along its length. Tiles exactly."""
    along, across = (X, Y) if axis == "y" else (Y, X)
    drift = smooth(0.8, seed)[0, :] if axis == "y" else smooth(0.8, seed)[:, 0]
    thick = smooth(0.5, seed + 7)[0, :] if axis == "y" else smooth(0.5, seed + 7)[:, 0]
    idx = (along * PX).astype(int) % SIZE
    off = drift[idx] * wobble_m
    w = width_m * (1 + 0.35 * thick[idx])
    d = (across - off) % spacing_m
    d = np.minimum(d, spacing_m - d)
    return np.exp(-(d / w) ** 2)


def normal_from_height(h, strength):
    gy, gx = np.gradient(h * strength * 8)
    n = np.dstack((-gx, gy, np.ones_like(h)))
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n * 0.5 + 0.5


def save(name, albedo, height):
    OUT.mkdir(parents=True, exist_ok=True)
    albedo = np.clip(albedo, 0, 1)
    span = np.ptp(height)
    h = (height - height.min()) / span if span > 1e-9 else np.full_like(height, 0.5)
    Image.fromarray((albedo * 255).astype(np.uint8)).save(OUT / f"{name}_albedo.png")
    Image.fromarray((h * 255).astype(np.uint8)).save(OUT / f"{name}_height.png")
    Image.fromarray((normal_from_height(h, STRENGTH[name]) * 255).astype(np.uint8)).save(OUT / f"{name}_normal.png")
    print("[ilalim-tex]", name)


def lrt_concrete():
    img = flat("cfcbc2")
    img = coat(img, (1.022, 1.022, 1.02), 1.6, 0.30, seed=301, feather=1.2)
    img = coat(img, (0.975, 0.972, 0.968), 1.1, 0.22, seed=302, feather=1.1)
    # One lift every 2 m, faint: at 1 m and 7 per cent the piers read as stacked stone blocks.
    lines = wobbly_lines(2.0, "y", 0.010, 0.02, seed=303)
    img = img * (1 - 0.045 * lines[..., None])
    save("lrt_concrete", img, -lines)


def lrt_soffit():
    # The slab's underside between the girders: DARK, as on Taft (owner's street view, review
    # v6: "the underside also looks different from what you currently have"). Warm dark grey
    # with big soft damp patches; kept a little lighter than the photograph so it still reads.
    img = flat("7f7b76")
    img = coat(img, (0.93, 0.93, 0.935), 1.5, 0.30, seed=311, feather=1.2)
    img = coat(img, (1.03, 1.03, 1.025), 0.9, 0.15, seed=312, feather=1.0)
    joints = wobbly_lines(2.0, "x", 0.012, 0.015, seed=313)
    img = img * (1 - 0.05 * joints[..., None])
    save("lrt_soffit", img, -joints)


def lrt_girder():
    # The precast girders, fascia beams and pier caps: a mid, slightly cool grey, darker than
    # the parapet and far cooler than the piers. Soft coats stretched ALONG the beam (broad
    # weathering bands where water runs along the bottom flange), no drawn lines.
    img = flat("8f8b85")
    img = coat(img, (0.95, 0.95, 0.95), 1.2, 0.28, seed=361, feather=1.5, stretch=(1.0, 0.6))
    img = coat(img, (1.035, 1.03, 1.025), 0.9, 0.18, seed=362, feather=1.1)
    save("lrt_girder", img, np.zeros((SIZE, SIZE)))


def lrt_pier():
    # THE COLUMNS HAVE THEIR OWN DRAWING (owner, review v6: "rework the supporting columns
    # texture in a way that it doesnt look blended in to the main duct/railway"). On Taft they
    # are a warm, pale, sandy concrete that stands clear of the dark deck above. Broad soft
    # VERTICAL washes, as rain and render leave on a column, and no pour lines: those belong
    # to the deck.
    img = flat("cbc2b1")
    img = coat(img, (0.975, 0.968, 0.958), 1.0, 0.26, seed=371, feather=1.6, stretch=(0.4, 1.0))
    img = coat(img, (1.03, 1.028, 1.02), 1.2, 0.20, seed=372, feather=1.2)
    save("lrt_pier", img, np.zeros((SIZE, SIZE)))


def lrt_track_bed():
    img = flat("8f8b84")
    img = coat(img, (1.0, 0.95, 0.90), 1.4, 0.28, seed=321, feather=1.2)
    img = coat(img, (1.03, 1.03, 1.025), 0.8, 0.15, seed=322, feather=1.0)
    save("lrt_track_bed", img, np.zeros((SIZE, SIZE)))


def lrt_steel():
    img = flat("e2e2e2")
    img = coat(img, (1.03, 1.03, 1.03), 1.0, 0.25, seed=331, feather=1.2)
    img = coat(img, (0.975, 0.975, 0.975), 1.4, 0.18, seed=332, feather=1.2)
    save("lrt_steel", img, np.zeros((SIZE, SIZE)))


def lrt_ballast():
    """Pebbles: jittered points, each pixel belongs to its nearest one. A pixel is stone when it
    is well inside its cell, with a rounded edge and a flat highlight toward the top-left."""
    rng = np.random.default_rng(351)
    cell_m = 0.11
    n = int(TILE_M / cell_m)
    gy, gx = np.mgrid[0:n, 0:n]
    px = (gx + 0.5 + rng.uniform(-0.35, 0.35, (n, n))) * cell_m
    py = (gy + 0.5 + rng.uniform(-0.35, 0.35, (n, n))) * cell_m
    tone = rng.choice([0.93, 0.98, 1.03, 1.07], size=(n, n))
    best = np.full((SIZE, SIZE), 9.0)
    second = np.full((SIZE, SIZE), 9.0)
    val = np.ones((SIZE, SIZE))
    hl = np.zeros((SIZE, SIZE))
    ci, cj = (Y / cell_m).astype(int), (X / cell_m).astype(int)
    for di in (-1, 0, 1):
        for dj in (-1, 0, 1):
            i, j = (ci + di) % n, (cj + dj) % n
            cx = px[i, j] + (cj + dj - j) * cell_m
            cy = py[i, j] + (ci + di - i) * cell_m
            d = np.hypot(X - cx, Y - cy)
            closer = d < best
            second = np.where(closer, best, np.minimum(second, d))
            val = np.where(closer, tone[i, j], val)
            hl = np.where(closer, ((X - cx) + (Y - cy) < -0.012) & (d < cell_m * 0.28), hl)
            best = np.where(closer, d, best)
    gap = second - best
    # Loose gravel, not paving: the gaps are barely darker than the stones, so no stone gets a
    # mortar outline (review v4 read the first version as cobbles).
    stone = np.clip((gap - 0.004) / 0.02, 0, 1)
    img = flat("928c83") * (1 - stone[..., None]) + (hexcol("a19b92") * val[..., None]) * stone[..., None]
    img = img * (1 + 0.06 * hl[..., None] * stone[..., None])
    img = coat(img, (1.0, 0.94, 0.88), 1.2, 0.25, seed=352, feather=1.2)
    save("lrt_ballast", img, stone)


def _mult(name, a):
    """Save a multiplier overlay. Row 0 of `a` is the EDGE; it is flipped so that UV v = 0 (the
    image's bottom row in Blender) is the edge."""
    OUT.mkdir(parents=True, exist_ok=True)
    img = np.flipud(np.clip(a, 0, 1))
    Image.fromarray((img * 255).astype(np.uint8)).save(OUT / f"{name}.png")
    print("[ilalim-tex]", name)


def _noise1d(n, scale_px, seed):
    white = np.random.default_rng(seed).standard_normal(n)
    f = np.fft.fftfreq(n)
    s = np.real(np.fft.ifft(np.fft.fft(white) * np.exp(-(f * scale_px) ** 2 * 2)))
    return (s - s.mean()) / (s.std() + 1e-9)


def grime_drips():
    W_M, H_M, ppm = 8.0, 4.0, 200
    w, h = int(W_M * ppm), int(H_M * ppm)
    v, u = np.mgrid[0:h, 0:w] / ppm           # v: metres down from the edge
    a = np.ones((h, w, 3))
    # The soft band right under the edge, its lower boundary wobbling.
    edge = 0.22 + 0.08 * _noise1d(w, 0.8 * ppm, 401)[None, :]
    band = np.clip((edge - v) / 0.12 + 0.5, 0, 1)
    a *= 1 - 0.13 * band[..., None]
    rng = np.random.default_rng(402)
    colours = [np.array([0.80, 0.79, 0.77])] * 5 + [np.array([0.88, 0.82, 0.74])] * 2 + [np.array([0.78, 0.81, 0.75])]
    for k in range(16):
        u0 = rng.uniform(0, W_M)
        width = rng.uniform(0.10, 0.45)
        length = rng.choice([rng.uniform(0.4, 1.2), rng.uniform(1.2, 3.4)], p=[0.6, 0.4])
        col = colours[rng.integers(len(colours))]
        phase = rng.uniform(0, 6.28)
        centre = u0 + 0.06 * np.sin(v * 1.4 + phase)
        du = np.abs(((u - centre + W_M / 2) % W_M) - W_M / 2)
        # A DRAWN shape, not an airbrushed blur (review v4): a flat fill whose width wobbles
        # and narrows toward a rounded end, with only a narrow soft edge.
        taper = np.clip(1 - v / length, 0, 1) ** 0.5
        wob = 1 + 0.08 * np.sin(v * 3.5 + phase * 2) + 0.04 * np.sin(v * 8.0 + phase)
        half = width * (0.45 + 0.55 * taper) * wob
        end = np.sqrt(np.clip(1 - ((v - (length - width)) / width) ** 2, 0, 1))
        half = np.where(v > length - width, half * end, half)
        feather = 0.025
        mask = np.clip((half - du) / feather + 0.5, 0, 1) * (v < length)
        mask *= rng.uniform(0.35, 0.75)
        a *= 1 - mask[..., None] * (1 - col)
    _mult("grime_drips", a)


def grime_splash():
    W_M, H_M, ppm = 8.0, 2.0, 200
    w, h = int(W_M * ppm), int(H_M * ppm)
    v, u = np.mgrid[0:h, 0:w] / ppm           # v: metres UP from the ground
    a = np.ones((h, w, 3))
    top = 0.55 + 0.18 * _noise1d(w, 0.5 * ppm, 411)[None, :]
    band = np.clip((top - v) / 0.15 + 0.5, 0, 1)
    a *= 1 - band[..., None] * (1 - np.array([0.80, 0.78, 0.74]))
    rng = np.random.default_rng(412)
    for k in range(14):
        u0 = rng.uniform(0, W_M)
        width = rng.uniform(0.08, 0.3)
        length = rng.uniform(0.6, 1.3)
        du = np.abs(((u - u0 + W_M / 2) % W_M) - W_M / 2)
        mask = np.exp(-(du / width) ** 4) * np.clip((length - v) / 0.2, 0, 1) * rng.uniform(0.4, 0.8)
        a *= 1 - mask[..., None] * (1 - np.array([0.84, 0.81, 0.76]))
    _mult("grime_splash", a)


STRENGTH["lrt_ballast"] = 0.6
STRENGTH["lrt_girder"] = 0.6
STRENGTH["lrt_pier"] = 0.6
PAINTERS = [lrt_concrete, lrt_girder, lrt_pier, lrt_soffit, lrt_track_bed, lrt_steel, lrt_ballast]
OVERLAYS = [grime_drips, grime_splash]


def swatch_sheet(names, version, tint=None):
    """Each texture as a 3 x 3 repeat (12 m square), labelled, side by side."""
    SHEETS.mkdir(parents=True, exist_ok=True)
    cell = 480
    sheet = Image.new("RGB", (cell * len(names) + 20 * (len(names) + 1), cell + 70), (245, 242, 235))
    draw = ImageDraw.Draw(sheet)
    for k, name in enumerate(names):
        tile = Image.open(OUT / f"{name}_albedo.png").convert("RGB")
        if name in NEUTRAL:
            a = np.asarray(tile) / 255 * hexcol("6f7c76") * 1.2
            tile = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
        rep = Image.new("RGB", (SIZE * 3, SIZE * 3))
        for i in range(3):
            for j in range(3):
                rep.paste(tile, (i * SIZE, j * SIZE))
        rep = rep.resize((cell, cell), Image.LANCZOS)
        x = 20 + k * (cell + 20)
        sheet.paste(rep, (x, 20))
        draw.text((x, cell + 32), f"{name}  (3 x 3 repeat, {3 * TILE_M:.0f} m square)", fill=(40, 40, 40))
    path = SHEETS / f"lrt_swatches_v{version}.png"
    sheet.save(path)
    print("[ilalim-tex] sheet", path)


def normals_only():
    for name in STRENGTH:
        h = np.asarray(Image.open(OUT / f"{name}_height.png"), dtype=float) / 255
        Image.fromarray((normal_from_height(h, STRENGTH[name]) * 255).astype(np.uint8)).save(OUT / f"{name}_normal.png")
        print("[ilalim-tex] normals", name)


def main():
    if "--normals-only" in sys.argv:
        normals_only()
        return
    for paint in PAINTERS + OVERLAYS:
        paint()
    version = int(sys.argv[sys.argv.index("--sheet") + 1]) if "--sheet" in sys.argv else 1
    swatch_sheet([p.__name__ for p in PAINTERS], version)
    # The overlays, shown over plain concrete so the stains read as they will on the models.
    base = np.asarray(Image.open(OUT / "lrt_concrete_albedo.png").convert("RGB"), dtype=float) / 255
    for name in ("grime_drips", "grime_splash"):
        over = np.asarray(Image.open(OUT / f"{name}.png").convert("RGB"), dtype=float) / 255
        h, w = over.shape[:2]
        reps = np.tile(base, (h // SIZE + 1, w // SIZE + 1, 1))[:h, :w]
        shown = np.clip(reps * over, 0, 1)
        if name == "grime_drips":
            shown = np.flipud(shown)            # edge at the top, as it hangs on the models
        Image.fromarray((shown * 255).astype(np.uint8)).save(SHEETS / f"{name}_on_concrete_v{version}.png")


if __name__ == "__main__":
    main()
