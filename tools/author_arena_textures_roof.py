"""Paint the arena's ROOF kit textures, in the house illustrated style (ARENA-1.4, roof kit).

  py -3 tools/author_arena_textures_roof.py [--sheet N]

Writes Assets/TumbangPreso/Art/Arena/Textures/arena_roof_<surface>.png (and _emit.png where the
surface glows), a checker for the UV check and a swatch sheet in Logs/arena/roof/. The models
are built by tools/author_arena_roof.py, whose materials are named arena_roof_<surface>.

THE STYLE (docs/ARENA_ART_BRIEF.md, KANTO_DESIGN_GUIDE.md section 3): FLAT fills, a few LARGE
patches with FEATHERED organic edges, low contrast between a thing and its joints, no grain, no
noise, no streaks. Every surface is its own drawing, its own function. Small things (panel
joints, mullions, LED modules, lamp lenses) are DRAWN here, never modelled. The drawing helpers
(periodic smooth fields, feathered patches, wobbly hand lines) are the Ilalim prop kit's, so the
hand is the same as on the other maps.

NIGHT: the albedo is dark navy and slate but not black; the glow is in the _emit textures.
ROLE HUES: nothing near offence orange #f87020 or defence blue #0080e8 except the logo itself.
LED blue is deep, #0a1a9a.

  TILING, 16 m per 1024 px tile (64 px per metre; these are seen from 40 to 190 m):
    skin         the roof sheet, top and soffit: dark navy panels 4 m wide with a hand-drawn seam
                 and one flat highlight beside it, a cross joint every 8 m, a few whole panels a
                 shade apart.
    glazing      the glazed roof bays: slate-teal panes 4 m square in dark frames, each pane one
                 flat shade. EMIT: the panes catch the floodlights, a dim cool glow, panes in
                 clusters brighter, frames dark.
    steel        painted structural steel (masts, trusses, purlins, catwalks, cables): blue-grey,
                 two broad coats, a faint joint band every 4 m along the member.
    housing      the dark casings (lamp banks, speakers, the scoreboard's shell, the screens'
                 cabinets, the canopy's nose): graphite navy, broad scuffed fields, a few faint
                 service-panel outlines.
    screen       an LED screen that is idle: near black, module joints every 1 m.
                 EMIT: one flat dim indigo, the same as the content panel's ground.
    booth_wall   the booth's composite panels: slate, a joint every 2 m, a darker plinth band.
  SPECIAL:
    lamp         one tile is 2 x 2 round floodlight lenses in their bezels (the model maps one
                 tile to a square the height of the lamp face). EMIT: the lenses, white.
    led          four bands, top to bottom of the image: WHITE, DEEP BLUE, CYAN, GOLD. u runs
                 along the strip, 4 m per tile, eight modules with dark gaps. EMIT the same.
                 LED_BANDS in the model holds the v range of each band.
    logo         the screens' CONTENT panel, UV 0..1: the TUMP logo on the idle screen's dark.
                 EMIT: the logo. Swap both for the score or a replay in Unity.
    booth_glass  8 m per tile along u; the bottom third of the tile (v 0..0.325) is the 2.6 m
                 window band: dark teal glass. EMIT: a warm room behind it, desk lamps, monitors,
                 the hosts' silhouettes.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
from author_ilalim_textures_props import (  # noqa: E402
    circle_mask, coat, fill, hexcol, patches, rect_mask, smooth, wobbly_lines,
)

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "TumbangPreso" / "Art" / "Arena" / "Textures"
LOGS = ROOT / "Logs" / "arena" / "roof"
LOGO = ROOT / "Assets" / "TumbangPreso" / "Art" / "ui" / "brand" / "tump_logo.png"
N = 1024
SCREEN_DARK = "0b0e1c"
IDLE = (0.030, 0.038, 0.095)                         # an idle screen's glow, the same on screen and content
DONE = []


def save(name, img, emit=None):
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).save(OUT / f"arena_roof_{name}.png")
    if emit is not None:
        Image.fromarray((np.clip(emit, 0, 1) * 255).astype(np.uint8)).save(OUT / f"arena_roof_{name}_emit.png")
    DONE.append((name, np.clip(img, 0, 1), None if emit is None else np.clip(emit, 0, 1)))
    print("[arena-roof-tex]", name, "+ emit" if emit is not None else "")


def lay(img, mask, colour):
    return img * (1 - mask[..., None]) + np.asarray(colour) * mask[..., None]


# ------------------------------------------------------------------ tiling, 16 m per tile

def skin():
    img = fill(N, N, "36405c")
    img = coat(img, (1.11, 1.10, 1.08), 300, 0.30, seed=5101, feather=1.2)
    img = coat(img, (0.89, 0.90, 0.92), 220, 0.22, seed=5102, feather=1.0)
    rng = np.random.default_rng(5103)
    for k in range(5):                              # whole sheets from another batch
        i, j = rng.integers(0, 4), rng.integers(0, 2)
        m = rect_mask(N, N, (i * 256 + 6, j * 512 + 6, i * 256 + 250, j * 512 + 506), 8, feather=3.0)
        img = img * (1 + (0.075 if k % 2 else -0.07) * m[..., None])
    seam = wobbly_lines(N, N, 256, 3.2, 2.0, seed=5104, axis="v")
    lit = np.roll(seam, 8, axis=1)                  # one flat highlight beside each seam
    joint = wobbly_lines(N, N, 512, 2.4, 2.0, seed=5105, axis="h")
    img = img * (1 + 0.10 * lit[..., None]) * (1 - 0.34 * seam[..., None]) * (1 - 0.22 * joint[..., None])
    save("skin", img)


def glazing():
    img = fill(N, N, "3c6074")
    glow = np.ones((N, N, 3)) * np.array([0.10, 0.17, 0.21])
    rng = np.random.default_rng(5201)
    shades = [0.93, 1.0, 1.0, 1.07, 1.12]
    bright = {(0, 1), (1, 1), (1, 2), (3, 0), (3, 3), (2, 3)}        # lit panes come in clusters
    for i in range(4):
        for j in range(4):
            m = rect_mask(N, N, (i * 256 + 4, j * 256 + 4, i * 256 + 252, j * 256 + 252), 10, feather=3.0)
            img = img * (1 + (shades[rng.integers(0, 5)] - 1) * m[..., None])
            glow = glow * (1 + ((1.7 if (i, j) in bright else 0.75) - 1) * m[..., None])
    img = img * (1 + 0.04 * smooth(N, N, 500, 5202)[..., None])     # one gentle gradient, no streaks
    frame = np.maximum(wobbly_lines(N, N, 256, 5.0, 1.5, seed=5203, axis="v"),
                       wobbly_lines(N, N, 256, 5.0, 1.5, seed=5204, axis="h"))
    img = lay(img, 0.88 * frame, hexcol("1f2740"))
    glow = glow * (1 - frame[..., None])
    save("glazing", img, glow)


def steel():
    img = fill(N, N, "5b6684")
    img = coat(img, (1.05, 1.05, 1.045), 420, 0.30, seed=5301, feather=1.6)
    img = coat(img, (0.955, 0.96, 0.965), 360, 0.24, seed=5302, feather=1.4)
    band = wobbly_lines(N, N, 256, 3.0, 1.5, seed=5303, axis="h")   # a joint every 4 m along a member
    img = img * (1 - 0.13 * band[..., None])
    save("steel", img)


def housing():
    img = fill(N, N, "272d40")
    img = coat(img, (1.12, 1.115, 1.10), 380, 0.26, seed=5401, feather=1.5)
    img = coat(img, (0.91, 0.915, 0.93), 420, 0.20, seed=5402, feather=1.4)
    seam = np.maximum(wobbly_lines(N, N, 512, 2.4, 1.5, seed=5403, axis="v"),
                      wobbly_lines(N, N, 256, 2.0, 1.5, seed=5404, axis="h"))
    img = img * (1 - 0.20 * seam[..., None])
    for k, (x, y, w, h) in enumerate(((70, 300, 300, 150), (600, 60, 220, 130), (640, 560, 300, 160))):
        outer = rect_mask(N, N, (x, y, x + w, y + h), 18, wobble=2.0, seed=5410 + k, feather=2.0)
        inner = rect_mask(N, N, (x + 7, y + 7, x + w - 7, y + h - 7), 12, wobble=2.0, seed=5410 + k, feather=2.0)
        img = img * (1 + 0.16 * np.clip(outer - inner, 0, 1)[..., None])   # a service panel's lip
    save("housing", img)


def screen():
    # FLAT, so the content cell cut into the screen (the logo material) has no visible edge.
    img = fill(N, N, SCREEN_DARK)
    grid = np.maximum(wobbly_lines(N, N, 64, 1.3, 0.6, seed=5502, axis="v"),
                      wobbly_lines(N, N, 64, 1.3, 0.6, seed=5503, axis="h"))
    img = img * (1 - 0.3 * grid[..., None])
    glow = np.ones((N, N, 3)) * np.array(IDLE)
    save("screen", img, glow)


def booth_wall():
    img = fill(N, N, "5f6a86")
    img = coat(img, (1.07, 1.07, 1.06), 260, 0.3, seed=5601, feather=1.2)
    img = coat(img, (0.93, 0.94, 0.95), 190, 0.2, seed=5602, feather=1.0)
    joint = wobbly_lines(N, N, 128, 2.0, 1.2, seed=5603, axis="v")
    img = img * (1 - 0.16 * joint[..., None])
    yy = np.mgrid[0:N, 0:N][0].astype(float)
    edge = N - 58 + 4 * smooth(1, N, 200, 5604)[0][None, :]          # the plinth band's hand-drawn top
    plinth = np.clip((yy - edge) / 3.0 + 0.5, 0, 1)
    img = img * (1 - 0.30 * plinth[..., None])
    save("booth_wall", img)


# ------------------------------------------------------------------ special

def lamp():
    img = fill(N, N, "1c2233")
    img = coat(img, (1.18, 1.18, 1.15), 300, 0.3, seed=5701, feather=1.2)
    glow = np.zeros((N, N, 3))
    yy, xx = np.mgrid[0:N, 0:N].astype(float)
    rng = np.random.default_rng(5702)
    for cx in (256, 768):
        for cy in (256, 768):
            ang = np.arctan2(yy - cy, xx - cx)
            ph = rng.uniform(0, 6.28)
            wob = 1 + 0.010 * np.sin(3 * ang + ph) + 0.006 * np.sin(5 * ang + 2 * ph)
            d = np.hypot(xx - cx, yy - cy) / wob
            bezel = np.clip((222 - d) / 3.0 + 0.5, 0, 1)
            lens = np.clip((190 - d) / 3.0 + 0.5, 0, 1)
            core = np.clip((120 - d) / 8.0 + 0.5, 0, 1)
            img = lay(img, bezel, hexcol("3a4460"))
            img = lay(img, lens, hexcol("d3e2f0"))
            img = lay(img, core, hexcol("f6fbff"))
            halo = np.clip((240 - d) / 40.0, 0, 1) * 0.30
            glow = np.maximum(glow, np.array([0.86, 0.93, 1.0]) * np.maximum(lens, halo)[..., None])
            glow = np.maximum(glow, np.array([1.0, 1.0, 1.0]) * core[..., None])
    save("lamp", img, glow)


LED = (("white", "f2f6ff"), ("blue", "0a1a9a"), ("cyan", "3fe0f0"), ("gold", "ffc860"))


def led():
    img = fill(N, N, "0c0f1c")
    glow = np.zeros((N, N, 3))
    xx = np.mgrid[0:N, 0:N][1].astype(float)
    cell = xx % 128
    module = np.clip((np.minimum(cell, 128 - cell) - 5) / 2.5, 0, 1)   # eight modules, dark gaps
    for k, (_, col) in enumerate(LED):
        rows = slice(k * 256, (k + 1) * 256)
        c = hexcol(col)
        m = module[rows]
        glow[rows] = c * m[..., None]
        img[rows] = img[rows] * (1 - m[..., None]) + (c * 0.6 + 0.04) * m[..., None]
    save("led", img, glow)


def logo():
    src = np.asarray(Image.open(LOGO).convert("RGBA")).astype(float) / 255
    # The logo's cream letters are half transparent (it is drawn for a white page). Inside its
    # own outline it is laid over white, so on the screen the letters are the cream of the page.
    inside = ndimage.binary_fill_holes(src[..., 3] > 0.9).astype(float)
    a0 = src[..., 3:4]
    solid = np.concatenate([src[..., :3] * a0 + (1 - a0) * inside[..., None], np.maximum(a0, inside[..., None])], axis=2)
    art = Image.fromarray((solid * 255).astype(np.uint8), "RGBA").resize((int(N * 0.90), int(N * 0.86)), Image.LANCZOS)
    full = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    full.paste(art, ((N - art.width) // 2, (N - art.height) // 2))
    a = np.asarray(full).astype(float) / 255
    rgb, alpha = a[..., :3], a[..., 3]
    back = fill(N, N, SCREEN_DARK)
    img = back * (1 - alpha[..., None]) + rgb * 0.62 * alpha[..., None]
    halo = np.clip(ndimage.gaussian_filter(alpha, 26) * 1.2, 0, 1) * (1 - alpha)
    glow = rgb * alpha[..., None] + np.array([0.16, 0.06, 0.03]) * halo[..., None]
    yy, xx = np.mgrid[0:N, 0:N]
    edge = np.clip(np.minimum(np.minimum(xx, yy), np.minimum(N - 1 - xx, N - 1 - yy)) / 40.0, 0, 1)
    glow = glow * edge[..., None]                    # the halo dies before the cell's edge
    glow = glow + np.array(IDLE) * (1 - alpha)[..., None]
    save("logo", img, glow)


def booth_glass():
    img = fill(N, N, "2c4a5b")
    img = coat(img, (1.10, 1.10, 1.08), 320, 0.3, seed=5901, feather=1.4)
    glow = np.zeros((N, N, 3))
    top = N - 333                                    # the window band is the bottom 333 rows (2.6 m)
    yy, xx = np.mgrid[0:N, 0:N].astype(float)
    v = np.clip((yy - top) / 333.0, 0, 1)            # 0 at the head, 1 at the sill
    room = np.clip((v - 0.18) / 0.5, 0, 1) * (yy >= top)
    lampfield = 0.55 + 0.45 * patches(N, N, 260, 0.45, 5902, feather=1.6, stretch=(1.0, 0.25))
    glow += np.array([0.62, 0.42, 0.20]) * (room * lampfield)[..., None]
    for k, (x, w, h) in enumerate(((90, 110, 70), (255, 80, 56), (430, 140, 84), (640, 90, 60), (820, 120, 76))):
        y0 = top + 120 + (k % 2) * 16
        m = rect_mask(N, N, (x, y0, x + w, y0 + h), 8, wobble=1.5, seed=5910 + k, feather=2.0)
        glow = lay(glow, m, (0.50, 0.80, 0.88))      # a monitor
        img = lay(img, 0.5 * m, hexcol("6f98a6"))
    for k, x in enumerate((200, 560, 760)):          # the hosts, dark against the room
        head = circle_mask(N, N, x, top + 196, 22, feather=2.5)
        body = rect_mask(N, N, (x - 44, top + 214, x + 44, N + 40), 26, wobble=2.0, seed=5920 + k, feather=2.5)
        who = np.clip(head + body, 0, 1)
        glow = glow * (1 - 0.86 * who[..., None])
        img = img * (1 - 0.25 * who[..., None])
    shine = np.clip(1 - v / 0.22, 0, 1) * (yy >= top)   # the one soft reflection band under the head
    img = img * (1 + 0.16 * shine[..., None])
    save("booth_glass", img, glow)


def checker():
    yy, xx = np.mgrid[0:N, 0:N]
    cx, cy = xx // 128, yy // 128
    base = np.where(((cx + cy) % 2 == 0)[..., None], np.array([0.86, 0.86, 0.86]), np.array([0.22, 0.24, 0.30]))
    base = np.where(((cx % 4 == 0) & (cy % 4 == 0))[..., None], np.array([0.90, 0.30, 0.25]), base)
    line = ((xx % 128 < 3) | (yy % 128 < 3))[..., None]
    base = np.where(line, np.array([0.05, 0.05, 0.08]), base)
    LOGS.mkdir(parents=True, exist_ok=True)
    Image.fromarray((base * 255).astype(np.uint8)).save(LOGS / "checker.png")


def sheet(version):
    cell = 300
    cols = 5
    rows = (len(DONE) * 2 + cols - 1) // cols
    out = Image.new("RGB", (cols * cell, rows * cell), (10, 12, 20))
    k = 0
    for name, img, emit in DONE:
        for layer in (img, emit):
            if layer is None:
                layer = np.zeros_like(img)
            tile = Image.fromarray((layer * 255).astype(np.uint8)).resize((cell - 8, cell - 8), Image.LANCZOS)
            out.paste(tile, ((k % cols) * cell + 4, (k // cols) * cell + 4))
            k += 1
    p = LOGS / f"roof_swatches_v{version}.png"
    out.save(p)
    print("[arena-roof-tex] sheet", p)


def main():
    version = 1
    if "--sheet" in sys.argv:
        version = int(sys.argv[sys.argv.index("--sheet") + 1])
    for f in (skin, glazing, steel, housing, screen, booth_wall, lamp, led, logo, booth_glass):
        f()
    checker()
    sheet(version)


if __name__ == "__main__":
    main()
