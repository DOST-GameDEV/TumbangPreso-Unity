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
    img = flat("9fa2a4")
    img = coat(img, (0.95, 0.952, 0.958), 1.5, 0.30, seed=311, feather=1.2)
    img = coat(img, (1.025, 1.025, 1.025), 0.9, 0.15, seed=312, feather=1.0)
    joints = wobbly_lines(2.0, "x", 0.012, 0.015, seed=313)
    img = img * (1 - 0.06 * joints[..., None])
    save("lrt_soffit", img, -joints)


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


PAINTERS = [lrt_concrete, lrt_soffit, lrt_track_bed, lrt_steel]


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
    for paint in PAINTERS:
        paint()
    version = int(sys.argv[sys.argv.index("--sheet") + 1]) if "--sheet" in sys.argv else 1
    swatch_sheet([p.__name__ for p in PAINTERS], version)


if __name__ == "__main__":
    main()
