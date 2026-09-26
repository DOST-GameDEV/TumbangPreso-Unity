"""Paint Lagoon Court's tileable textures in TUMP's illustrated style.

  py -3 tools/author_lagoon_textures.py rock_a,rock_b,rock_c     # paint the named textures
  py -3 tools/author_lagoon_textures.py --sheet N                  # swatch sheet vN for review

Writes ArtSource/lagoon/textures/<name>_albedo.png, _height.png and _normal.png (OpenGL
convention, green up). LAGOON_TEX_OUT redirects output so a swatch is reviewed before it
replaces a texture. Every texture tiles and covers TILE_M metres.

THE HOUSE STYLE (docs/KANTO_DESIGN_GUIDE.md § 3, owner-approved): flat fills; value changes
from a few LARGE patches with FEATHERED organic edges; hand-drawn shapes, never ruled; light
drawn in cel-style as one flat band, not a gradient; low contrast; no grain, no noise, no
streaks. ⚠️ Every surface gets its OWN drawing (the owner rejected bark and roof tiles that were
the brick generator in disguise). This file shares no generator with author_kanto_textures.py;
only the approved Kanto swatches are read, to sit beside the new ones on the sheet.

ROCK (docs/LAGOON_REWORK_GUIDE.md § 8 step 2). Research: stylized painted rock reads from a few
clean BROAD PLANES with light drawn on their upper edges and very little surface detail; the
light-top, dark-base gradient belongs to the MATERIAL (it depends on which way a face points,
which a tiling texture cannot know), so the textures here are the quiet rock FACE only.
  rock_a: warm tan, two feathered patch coats. The minimum.
  rock_b: rock_a plus broad angular PLANES (wobbly Voronoi facets) in three close values, each
          with a thin light band along its upper edge and a soft shade along its lower edge.
  rock_c: rock_b with its patches shifted in colour TEMPERATURE (warm ochre and cool mauve-grey)
          instead of value only.
"""
import os
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = Path(os.environ.get("LAGOON_TEX_OUT", ROOT / "ArtSource" / "lagoon" / "textures"))
KANTO = ROOT / "ArtSource" / "kanto" / "textures"
SHEETS = ROOT / "Logs" / "lagoon-blender"
SIZE = 1024
TILE_M = 4.0            # a boulder is 3 to 6 m, so one tile spans most of a face
PX = SIZE / TILE_M
Y, X = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32) / PX   # metres, y down the image


def hexcol(h):
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], dtype=np.float32)


def flat(col):
    return np.broadcast_to(hexcol(col), (SIZE, SIZE, 3)).copy()


def field(scale_m, seed):
    """Periodic smooth random field, features about `scale_m` across, unit variance."""
    white = np.random.default_rng(seed).standard_normal((SIZE, SIZE))
    f = np.fft.fftfreq(SIZE)
    s = scale_m * PX
    g = np.exp(-(f[:, None] ** 2 + f[None, :] ** 2) * s * s * 2)
    n = np.real(np.fft.ifft2(np.fft.fft2(white) * g))
    return ((n - n.mean()) / (n.std() + 1e-9)).astype(np.float32)


def coat(img, tint, scale_m, coverage, seed, feather=0.4):
    """A second coat of paint over part of the surface, with a feathered organic edge."""
    n = field(scale_m, seed) + 0.25 * field(scale_m / 3, seed + 1)
    t = np.quantile(n, 1 - coverage)
    a = np.clip((n - t) / feather + 0.5, 0, 1)
    a = (a * a * (3 - 2 * a))[..., None]
    return img * (1 - a) + (img * tint) * a


def facets(cells, seed, warp_m=0.015, stretch=0.55):
    """Broad rock PLANES: a periodic Voronoi of `cells` sites over the tile, its edges bent by a
    smooth warp so no edge is ruled. Returns the cell id, the distance to the nearest edge (m),
    and +1/-1 for whether the neighbour across that edge lies ABOVE (+1: this pixel is at its
    plane's upper edge, where light catches) or BELOW (-1: the lower edge, in shade).
    ⚠️ Sheet v1 bent the edges 0.12 m and outlined every one: it read as crazy paving, a cousin
    of stone_blocks, not rock. Rock planes are ANGULAR: nearly straight edges (0.015 m warp),
    wider than tall (`stretch` shrinks x distance, so cells run horizontally like weathered
    sheets of granite), told apart by value, with light only on the upper edges."""
    rng = np.random.default_rng(seed)
    sites = rng.uniform(0, TILE_M, (cells, 2)).astype(np.float32)
    wx = X + warp_m * field(0.5, seed + 1)
    wy = Y + warp_m * field(0.5, seed + 2)
    d1 = np.full((SIZE, SIZE), 1e9, np.float32)
    d2 = np.full((SIZE, SIZE), 1e9, np.float32)
    id1 = np.zeros((SIZE, SIZE), np.int32)
    y1 = np.zeros((SIZE, SIZE), np.float32)
    y2 = np.zeros((SIZE, SIZE), np.float32)
    for k, (sx, sy) in enumerate(sites):
        for ox in (-TILE_M, 0, TILE_M):
            for oy in (-TILE_M, 0, TILE_M):
                d = np.hypot((wx - sx - ox) * stretch, wy - sy - oy)
                closer = d < d1
                second = (~closer) & (d < d2)
                d2 = np.where(closer, d1, np.where(second, d, d2))
                y2 = np.where(closer, y1, np.where(second, sy + oy, y2))
                d1 = np.where(closer, d, d1)
                y1 = np.where(closer, sy + oy, y1)
                id1 = np.where(closer, k, id1)
    edge = (d2 - d1) * 0.5
    side = np.where(y2 < y1, 1.0, -1.0).astype(np.float32)   # neighbour above = upper edge
    return id1, edge, side


def band(edge, width_m, soft_m=0.012):
    """1 inside a drawn band of `width_m` along a facet edge, crisp with a hand-soft falloff."""
    return np.clip((width_m - edge) / soft_m + 0.5, 0, 1)


def rock_face(temperature=False, planes=True):
    img = flat("c29f76")                                            # warm tan
    if temperature:
        img = coat(img, np.array([1.05, 1.0, 0.88]), 1.1, 0.35, seed=31)   # warm ochre
        img = coat(img, np.array([0.92, 0.92, 0.97]), 0.9, 0.25, seed=33)  # cool mauve-grey
    else:
        img = coat(img, np.array([0.93, 0.92, 0.90]), 1.1, 0.35, seed=31)
        img = coat(img, np.array([1.05, 1.04, 1.02]), 0.8, 0.22, seed=33)
    height = np.zeros((SIZE, SIZE), np.float32)
    if planes:
        ident, edge, side = facets(8, seed=41)
        values = np.random.default_rng(42).choice(np.array([0.94, 1.0, 1.06], np.float32), 8)
        img = img * values[ident][..., None]
        light = band(edge, 0.03) * (side > 0)                           # upper edges only
        img = img * (1 - light[..., None]) + np.minimum(img * 1.13, 1) * light[..., None]
        height = np.clip(edge / 0.12, 0, 1) ** 0.5                   # each plane a soft plateau
    return img, height


def normal_from_height(h, strength):
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * strength
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * strength
    n = np.dstack([-gx, gy, np.ones_like(h)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return n * 0.5 + 0.5


STRENGTH = {"rock_a": 0.0, "rock_b": 1.2, "rock_c": 1.2}


def save(name, albedo, height):
    OUT.mkdir(parents=True, exist_ok=True)
    albedo = np.clip(albedo, 0, 1)
    r = np.ptp(height)
    h = (height - height.min()) / r if r > 1e-9 else np.full_like(height, 0.5)
    Image.fromarray((albedo * 255).astype(np.uint8)).save(OUT / f"{name}_albedo.png")
    Image.fromarray((h * 255).astype(np.uint8)).save(OUT / f"{name}_height.png")
    Image.fromarray((normal_from_height(h, STRENGTH[name]) * 255).astype(np.uint8)).save(OUT / f"{name}_normal.png")
    print("[lagoon-tex]", name)


PAINTERS = {
    "rock_a": lambda: rock_face(planes=False),
    "rock_b": lambda: rock_face(),
    "rock_c": lambda: rock_face(temperature=True),
}


def sheet(version, names):
    """The review sheet: approved Kanto swatches first, then each new one, at one tile (4 m for
    rock, 2 m for the Kanto pair) and as a 3 x 3 repeat so any tiling shows."""
    cols = [("stone_blocks (approved, 2 m)", KANTO / "stone_blocks_albedo.png"),
            ("brick (approved, 2 m)", KANTO / "brick_albedo.png")]
    cols += [(f"{n} (4 m)", OUT / f"{n}_albedo.png") for n in names]
    cell, gap, head = 360, 16, 40
    W = gap + len(cols) * (cell + gap)
    H = 2 * (head + cell) + gap * 2
    page = Image.new("RGB", (W, H), (245, 241, 234))
    draw = ImageDraw.Draw(page)
    try:
        font = ImageFont.truetype("arial.ttf", 18)
    except OSError:
        font = ImageFont.load_default()
    for i, (label, path) in enumerate(cols):
        x = gap + i * (cell + gap)
        tile = Image.open(path).convert("RGB")
        draw.text((x, 12), label, fill=(40, 36, 32), font=font)
        page.paste(tile.resize((cell, cell), Image.LANCZOS), (x, head))
        draw.text((x, head + cell + gap + 12), "3 x 3 repeat", fill=(40, 36, 32), font=font)
        rep = Image.new("RGB", (tile.width * 3, tile.height * 3))
        for a in range(3):
            for b in range(3):
                rep.paste(tile, (a * tile.width, b * tile.height))
        page.paste(rep.resize((cell, cell), Image.LANCZOS), (x, 2 * head + cell + gap))
    SHEETS.mkdir(parents=True, exist_ok=True)
    out = SHEETS / f"rock_swatches_v{version}.png"
    page.save(out)
    print("[lagoon-tex] sheet", out)


def main():
    args = sys.argv[1:]
    if "--sheet" in args:
        names = [a for a in args if a in PAINTERS] or list(PAINTERS)
        sheet(int(args[args.index("--sheet") + 1]), names)
        return
    names = args[0].split(",") if args else list(PAINTERS)
    for n in names:
        save(n, *PAINTERS[n]())


if __name__ == "__main__":
    main()
