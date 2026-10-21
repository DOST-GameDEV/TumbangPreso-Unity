"""Paint the Eskinita Alley map's surface textures in TUMP's flat illustrated house style.

  py -3 tools/author_eskinita_textures.py                 # everything, swatch sheet v1
  py -3 tools/author_eskinita_textures.py --sheet 3       # everything, swatch sheet v3
  py -3 tools/author_eskinita_textures.py --only tin,floor --sheet 4

Writes Assets/TumbangPreso/Art/EskinitaAlley/Textures/<name>_albedo.png, the manifest
Assets/TumbangPreso/Art/EskinitaAlley/textures_manifest.json and a labelled swatch sheet
Logs/eskinita/textures_swatches_vN.png (each texture flat, and as a 3 x 3 repeat when it tiles,
beside two approved Kanto swatches).

THE STYLE is Kanto's (tools/author_kanto_textures.py docstring, docs/KANTO_DESIGN_GUIDE.md
section 3, docs/LAGOON_REWORK_GUIDE.md section 2):
  * FLAT fills. Value changes come from a FEW LARGE patches with feathered organic edges.
  * Shapes are hand-drawn and wobbly, never ruled.
  * Low contrast between a thing and its joints.
  * No grain, no noise, no streaks, no cracks, no airbrushed smudges.
  * Every surface is its OWN drawing: no generator here is shared between two surfaces. The
    only shared code is the brush kit (a periodic wobble field, one organic patch, one line).
  * Nothing near offence orange f87020 or defence blue 0080e8.

WHERE AN APPROVED TEXTURE OF THE SAME SURFACE EXISTS IT IS COPIED, not repainted (COPIES below).
"Neutral" textures are light and near grey or cream, because the scene builder multiplies the
texture by a material tint.
"""
import json
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Assets" / "TumbangPreso" / "Art"
OUT = ART / "EskinitaAlley" / "Textures"
MANIFEST = ART / "EskinitaAlley" / "textures_manifest.json"
LOGS = ROOT / "Logs" / "eskinita"

# name: (source albedo relative to Art/, tile metres or None for a card)
COPIES = {
    "plaster": ("Kanto/Textures/plaster_albedo.png", 3.0),
    "tiles_clay": ("Kanto/Textures/tiles_clay_albedo.png", 2.0),
    # Kanto's "timber" is the plain neutral pole and beam wood; its "wood" is a plank wall.
    "wood": ("Kanto/Textures/timber_albedo.png", 2.0),
    "paint": ("Kanto/Textures/paint_albedo.png", 2.0),
    # Lagoon's approved twill sawali is authored at 20 strips per 2 m (10 cm strips).
    "sawali": ("LagoonCove/Textures/sawali_a_albedo.png", 2.0),
    "jalousie": ("IlalimRebuild/Textures/heritage_window_jal_albedo.png", None),
    "leaf": ("Kanto/Textures/leaf_albedo.png", None),
}


# ------------------------------------------------------------------ brush kit
def hexcol(h):
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])


def grid(h, w):
    y, x = np.mgrid[0:h, 0:w].astype(float)
    return y, x


def wobble(shape, scale_px, seed, stretch=(1.0, 1.0)):
    """A periodic smooth field, about one unit strong, features about scale_px across. It MOVES
    drawn lines (a hand tremor) and, cut at a level by `coats`, gives the outline of the big
    patches. It is never added to a colour: that would be noise."""
    h, w = shape
    white = np.random.default_rng(seed).standard_normal((h, w))
    fy = np.fft.fftfreq(h)[:, None] * stretch[1]
    fx = np.fft.fftfreq(w)[None, :] * stretch[0]
    n = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-(fx ** 2 + fy ** 2) * scale_px ** 2 * 2)))
    return (n - n.mean()) / (n.std() + 1e-9)


def soft(a):
    a = np.clip(a, 0, 1)
    return a * a * (3 - 2 * a)


def patch(shape, cx, cy, r, seed, feather=26.0, lobes=0.2, aspect=1.0, wrap=True):
    """ONE large organic patch (a second coat that did not quite cover): a hand-drawn blob
    about r pixels in radius, its outline pushed in and out by a few slow lobes, with a
    feathered edge. Wraps across the tile so a patch never makes a seam."""
    h, w = shape
    y, x = grid(h, w)
    rng = np.random.default_rng(seed)
    dx, dy = x - cx, y - cy
    if wrap:
        dx = (dx + w / 2) % w - w / 2
        dy = (dy + h / 2) % h - h / 2
    dx = dx / aspect
    th = np.arctan2(dy, dx)
    rr = np.ones_like(th)
    for k in (2, 3, 4, 5):
        rr += lobes * rng.uniform(0.35, 1.0) * np.cos(k * th + rng.uniform(0, 6.283)) / (k - 1) ** 0.6
    d = np.hypot(dx, dy)
    return soft((r * rr - d) / feather + 0.5)[..., None]


def coats(shape, scale_px, coverage, seed, feather=0.4, rough=0.22):
    """The approved Kanto patch: a second coat over `coverage` of the tile, as a few LARGE
    sprawling shapes (about scale_px across) with a feathered edge. Tiles exactly. Sprawling
    shapes repeat far less obviously than a round blob does (swatch sheet v1 read as polka
    dots in the 3 x 3). `rough` is how much the outline crinkles; sheet v2's floor and
    concrete crumbled into too many pieces and too soft an edge, which read as mottling."""
    n = wobble(shape, scale_px, seed) + rough * wobble(shape, scale_px / 3, seed + 1)
    t = np.quantile(n, 1 - coverage)
    return soft((n - t) / feather + 0.5)[..., None]


def coat(img, a, mult):
    return img * (1 - a) + img * np.asarray(mult) * a


def line(dist_px, width_px, edge=1.4):
    """Coverage of a drawn line of the given width, from the distance to its centre."""
    return np.clip((width_px / 2 - dist_px) / edge + 0.5, 0, 1)


def wrapdist(v, pos, period):
    return np.abs((v - pos + period / 2) % period - period / 2)


def flat(shape, col):
    return np.broadcast_to(hexcol(col), (*shape, 3)).copy()


def save(name, img):
    OUT.mkdir(parents=True, exist_ok=True)
    img = np.clip(img, 0, 1)
    mode = "RGBA" if img.shape[2] == 4 else "RGB"
    Image.fromarray((img * 255 + 0.5).astype(np.uint8), mode).save(OUT / f"{name}_albedo.png")
    print("[eskinita-tex] painted", name)


# ------------------------------------------------------------------ tiling surfaces
def chb():
    """Unpainted concrete hollow block, 2 m tile: 0.4 x 0.2 m blocks in running bond.
    Its own drawing: the JOINTS are drawn, as freehand lines. Course lines sag and drift;
    every vertical joint sits a little off where a ruler would put it, so no two blocks are
    the same length; where lines meet the corner fills in, as a brush does. No highlight band
    (that is the brick's mark): a block is one flat grey."""
    S, rows, cols = 1024, 10, 5
    rh, cw = S / rows, S / cols
    y, x = grid(S, S)
    wx = x + 4.0 * wobble((S, S), 80, 11)
    wy = y + 3.5 * wobble((S, S), 120, 12, stretch=(3.0, 1.0)) + 1.5 * wobble((S, S), 40, 13)
    rng = np.random.default_rng(14)
    row = np.floor((wy % S) / rh).astype(int) % rows
    ly = (wy % S) - row * rh
    dyj = np.minimum(ly, rh - ly)
    jit = rng.uniform(-0.085, 0.085, (rows, cols))
    dxj = np.full((S, S), 1e9)
    left = np.full((S, S), 1e9)
    col = np.zeros((S, S), int)
    for k in range(cols):
        xk = (k + 0.5 * (row % 2) + jit[row, k]) * cw
        dl = (wx - xk) % S
        dxj = np.minimum(dxj, np.minimum(dl, S - dl))
        upd = dl < left
        col = np.where(upd, k, col)
        left = np.where(upd, dl, left)
    kk = 5.0                                                    # corner fill where lines meet
    dj = -kk * np.log(np.exp(-dyj / kk) + np.exp(-dxj / kk))
    joint = line(dj, 8.0, edge=1.6)[..., None]
    shade = rng.choice([0.955, 0.985, 1.0, 1.0, 1.0, 1.03], (rows, cols))[row, col][..., None]
    face = hexcol("aeaba4") * shade
    img = face * (1 - joint) + hexcol("9a978f") * joint
    img = coat(img, coats((S, S), 330, 0.24, 15, rough=0.12), (1.045, 1.045, 1.035))
    img = coat(img, coats((S, S), 300, 0.15, 17, rough=0.12), (0.95, 0.95, 0.955))
    save("chb", img)


def _rib_phase(S, n, seed, amp):
    """Corrugation phase 0..1 across each of n vertical ribs, drawn freehand: the rib lines
    lean and drift slowly down the sheet."""
    y, x = grid(S, S)
    wx = x + amp * wobble((S, S), 150, seed, stretch=(1.0, 3.0))
    return (wx / S * n) % 1.0


def tin():
    """Corrugated GI sheet, 2 m tile, neutral light grey. Twelve chunky ribs drawn as three
    FLAT bands each: the sheet colour, one soft highlight band on the crest, one narrower
    shaded band in the valley. One faint lap line where two sheets overlap. Three large
    slightly warmer patches. No rust speckle, no streaks."""
    S = 1024
    p = _rib_phase(S, 12, 21, 2.0)
    hi = soft((0.13 - np.abs(p - 0.32)) / 0.05 + 0.5)
    lo = soft((0.085 - np.abs(p - 0.82)) / 0.045 + 0.5)
    val = (1 + 0.055 * hi - 0.085 * lo)[..., None]
    img = flat((S, S), "dcdddc") * val
    y, x = grid(S, S)
    lap = wrapdist(y + 2.5 * wobble((S, S), 200, 22, stretch=(3.0, 1.0)), 300, S)
    below = (y + 2.5 * wobble((S, S), 200, 22, stretch=(3.0, 1.0)) - 300) % S
    img = img * (1 - 0.05 * soft(1 - below / 16.0) * (below < 16))[..., None]
    img = img * (1 - 0.1 * line(lap, 3.5))[..., None]
    img = coat(img, coats((S, S), 330, 0.26, 23, feather=0.5), (1.012, 0.99, 0.955))
    save("tin", img)


def tin_rust():
    """A rusted tin roof, 2 m tile. Not the grey sheet recoloured: painted in oxide red-brown
    from the start, the ribs quieter (rust is matt), and the life comes from a few big
    feathered coats: two darker, burnt ones and two lighter, dustier ones."""
    S = 1024
    p = _rib_phase(S, 12, 31, 2.4)
    hi = soft((0.12 - np.abs(p - 0.3)) / 0.06 + 0.5)
    lo = soft((0.1 - np.abs(p - 0.8)) / 0.05 + 0.5)
    val = (1 + 0.07 * hi - 0.1 * lo)[..., None]
    img = flat((S, S), "8f4b35") * val
    img = coat(img, coats((S, S), 340, 0.26, 32, feather=0.5), (0.9, 0.885, 0.89))
    img = coat(img, coats((S, S), 280, 0.16, 36, feather=0.5), (1.075, 1.065, 1.03))
    save("tin_rust", img)


def planks():
    """Horizontal wooden siding, 2 m tile, neutral warm light. Ten boards about 0.2 m tall.
    Its own drawing: clapboard. Each board line is one freehand stroke that drifts along the
    wall, no two boards the same height; the board above casts ONE flat shadow band on the
    board below; a few boards are a shade off; a handful of butt joints. No grain."""
    S, n = 1024, 10
    bh = S / n
    y, x = grid(S, S)
    rng = np.random.default_rng(41)
    wy = y + 3.0 * wobble((S, S), 170, 42, stretch=(3.0, 1.0)) + 0.5 * wobble((S, S), 70, 43)
    wxx = x + 2.0 * wobble((S, S), 90, 44)
    edges = (np.arange(n) + rng.uniform(-0.09, 0.09, n)) * bh
    above = np.full((S, S), 1e9)
    board = np.zeros((S, S), int)
    for k in range(n):
        d = (wy - edges[k]) % S                      # distance below board line k
        upd = d < above
        board = np.where(upd, k, board)
        above = np.where(upd, d, above)
    shade = rng.choice([0.955, 0.985, 1.0, 1.0, 1.0, 1.035], n)[board]
    img = hexcol("e8e2d6") * shade[..., None]
    img = img * (1 - 0.055 * np.clip((13.0 - above) / 2.0, 0, 1))[..., None]     # flat lap shadow
    ln = line(np.minimum(above, bh * 1.2 - above).clip(0) * 0 + above, 5.0)
    butt = np.zeros((S, S))
    for k in rng.choice(n, 5, replace=False):
        bx = rng.uniform(0, S)
        butt = np.maximum(butt, line(wrapdist(wxx, bx, S), 4.0) * (board == k))
    mark = np.maximum(ln, butt * 0.85)[..., None]
    img = img * (1 - mark) + hexcol("bdb4a4") * mark
    img = coat(img, coats((S, S), 320, 0.24, 45, feather=0.5), (0.96, 0.96, 0.955))
    save("planks", img)


def floor():
    """The alley's poured concrete floor, 4 m tile. Warm grey. Its own drawing: three bands of
    slabs, each cut into three slabs of its own widths (1.2 to 1.5 m), the joints freehand
    grooves a little darker than the slab. Three big feathered patches: one darker and cooler
    (damp), one lighter (a repair), one faintly warm. No speckle."""
    S = 1024
    m = S / 4.0
    y, x = grid(S, S)
    wy = y + 5.0 * wobble((S, S), 190, 51, stretch=(2.5, 1.0)) + 1.5 * wobble((S, S), 60, 52)
    wx = x + 5.0 * wobble((S, S), 190, 53, stretch=(1.0, 2.5)) + 1.5 * wobble((S, S), 60, 54)
    ybands = np.array([0.25, 1.6, 2.85]) * m
    xcuts = [np.array([0.2, 1.6, 2.9]) * m, np.array([0.9, 2.2, 3.55]) * m, np.array([0.5, 1.8, 3.3]) * m]
    above = np.full((S, S), 1e9)
    band = np.zeros((S, S), int)
    dj = np.full((S, S), 1e9)
    for k, yb in enumerate(ybands):
        d = (wy - yb) % S
        upd = d < above
        band = np.where(upd, k, band)
        above = np.where(upd, d, above)
        dj = np.minimum(dj, wrapdist(wy, yb, S))
    slab = np.zeros((S, S), int)
    for k in range(3):
        left = np.full((S, S), 1e9)
        for j, xc in enumerate(xcuts[k]):
            dl = (wx - xc) % S
            upd = (dl < left) & (band == k)
            slab = np.where(upd, k * 3 + j, slab)
            left = np.where(dl < left, dl, left)
            dj = np.where(band == k, np.minimum(dj, wrapdist(wx, xc, S)), dj)
    rng = np.random.default_rng(55)
    shade = rng.permutation([0.97, 0.985, 1.0, 1.0, 1.0, 1.0, 1.015, 1.03, 0.975])[slab]
    img = hexcol("bdb7ad") * shade[..., None]
    j = line(dj, 6.0, edge=1.8)[..., None]
    img = img * (1 - j) + hexcol("a39d92") * j
    img = coat(img, coats((S, S), 430, 0.2, 56, feather=0.4, rough=0.08), (0.945, 0.952, 0.96))   # damp
    img = coat(img, coats((S, S), 380, 0.13, 60, feather=0.4, rough=0.08), (1.028, 1.022, 1.004))  # sun-warmed
    # the repair: a hand-trowelled rounded rectangle of newer, lighter concrete
    rx = x + 9.0 * wobble((S, S), 110, 57)
    ry = y + 9.0 * wobble((S, S), 110, 58)
    rep = soft((_sdf_box(rx, ry, 640, 150, 900, 330, 40) * -1) / 18.0 + 0.5)[..., None]
    img = coat(img, rep, (1.06, 1.057, 1.05))
    save("floor", img)


def _sdf_box(x, y, x0, y0, x1, y1, r):
    cx, cy, hx, hy = (x0 + x1) / 2, (y0 + y1) / 2, (x1 - x0) / 2, (y1 - y0) / 2
    dx, dy = np.abs(x - cx) - (hx - r), np.abs(y - cy) - (hy - r)
    return np.hypot(np.maximum(dx, 0), np.maximum(dy, 0)) + np.minimum(np.maximum(dx, dy), 0) - r


def concrete():
    """Plain smooth concrete for stairs, ledges and beams, 2 m tile, neutral. One flat coat
    and exactly two big soft patches, one a shade darker, one a shade lighter."""
    S = 1024
    img = flat((S, S), "e3e2df")
    img = coat(img, coats((S, S), 560, 0.3, 61, feather=0.45, rough=0.06), (0.95, 0.95, 0.95))
    img = coat(img, patch((S, S), 800, 300, 190, 62, feather=50, lobes=0.3, aspect=1.4), (1.03, 1.03, 1.03))
    save("concrete", img)


def trapal(name, colour, seed):
    """Striped tarpaulin (trapal), 1 m tile: eight stripes of 12.5 cm, colour and off-white.
    Its own drawing: the stripe edges are freehand, each edge leaning its own way, with a soft
    edge like printed cloth; one big sun-faded patch where the colour has gone chalky."""
    S = 1024
    y, x = grid(S, S)
    wx = x + 4.0 * wobble((S, S), 200, seed, stretch=(1.0, 2.5))
    t = wx % 256.0
    a = soft((64.0 - np.abs(t - 64.0)) / 5.0 + 0.5)[..., None]
    white = hexcol("ece7d9")
    img = white * (1 - a) + hexcol(colour) * a
    f = coats((S, S), 470, 0.26, seed + 2, feather=0.4, rough=0.08) * 0.2
    img = img * (1 - f) + (img * 0.35 + white * 0.65) * f
    save(name, img)


# ------------------------------------------------------------------ cards
def _rect(x, y, x0, y0, x1, y1, r=6.0):
    """Coverage of a rounded rectangle (soft one pixel edge)."""
    cx, cy, hx, hy = (x0 + x1) / 2, (y0 + y1) / 2, (x1 - x0) / 2, (y1 - y0) / 2
    r = min(r, hx, hy)
    dx, dy = np.abs(x - cx) - (hx - r), np.abs(y - cy) - (hy - r)
    sdf = np.hypot(np.maximum(dx, 0), np.maximum(dy, 0)) + np.minimum(np.maximum(dx, dy), 0) - r
    return np.clip(-sdf / 1.3 + 0.5, 0, 1)


def _ring(x, y, x0, y0, x1, y1, w, r=6.0):
    return np.clip(_rect(x, y, x0, y0, x1, y1, r) - _rect(x, y, x0 + w, y0 + w, x1 - w, y1 - w, max(r - w, 1.5)), 0, 1)


def _put(img, a, col):
    a = a[..., None]
    return img * (1 - a) + np.asarray(col) * a


def window_slide():
    """A wooden sliding window card, 512 px: a cream painted frame and sill, two timber sashes
    that overlap in the middle, each a 3 x 4 grid of small panes. Some panes are warm (a lamp
    is on inside), the rest dusky. Flat fills, freehand edges."""
    S = 512
    y, x = grid(S, S)
    x = x + 1.0 * wobble((S, S), 120, 71) * np.clip(np.minimum(x, S - x) / 30, 0, 1)
    y = y + 1.0 * wobble((S, S), 120, 72) * np.clip(np.minimum(y, S - y) / 30, 0, 1)
    frame, frame_line = hexcol("e7dec9"), hexcol("c5baa2")
    img = flat((S, S), "e7dec9")
    img = coat(img, patch((S, S), 120, 60, 150, 73, feather=30, wrap=False), (0.955, 0.955, 0.95))
    img = _put(img, _rect(x, y, 36, 36, 476, 440, 5), hexcol("3f4a48"))            # the opening
    img = _put(img, _ring(x, y, 31, 31, 481, 445, 5, 7), frame_line)
    rng = np.random.default_rng(74)
    timber, timber_dk = hexcol("9b6b44"), hexcol("7c5234")
    lit, lit2, dark, dark2 = hexcol("ecc98a"), hexcol("e3bb78"), hexcol("5c6f6d"), hexcol("506260")
    for s, (x0, x1) in enumerate(((40, 264), (248, 472))):
        y0, y1 = 40, 436
        img = _put(img, _rect(x, y, x0, y0, x1, y1, 5), timber_dk)
        img = _put(img, _rect(x, y, x0 + 3, y0 + 3, x1 - 3, y1 - 3, 4), timber)
        stile, mun = 20, 9
        pw = (x1 - x0 - 2 * stile - 2 * mun) / 3
        ph = (y1 - y0 - 2 * stile - 3 * mun) / 4
        for i in range(3):
            for j in range(4):
                px0 = x0 + stile + i * (pw + mun)
                py0 = y0 + stile + j * (ph + mun)
                on = rng.random() < 0.42
                c = (lit if rng.random() < 0.6 else lit2) if on else (dark if rng.random() < 0.6 else dark2)
                img = _put(img, _rect(x, y, px0, py0, px0 + pw, py0 + ph, 5), c)
    # the sill: a thicker board under the opening with one drawn shadow line
    img = _put(img, _rect(x, y, 14, 448, 498, 500, 7), frame * 1.03)
    img = _put(img, _ring(x, y, 14, 448, 498, 500, 4, 7), frame_line)
    save("window_slide", img)


def door_wood():
    """A painted panelled wooden door card, 512 x 1024, neutral light for tinting. Six
    recessed panels, each drawn with three flat values (a shaded top edge, the panel, a lit
    lower edge), and a small round knob."""
    W, H = 512, 1024
    y, x = grid(H, W)
    x = x + 1.6 * wobble((H, W), 110, 81) * np.clip(np.minimum(x, W - x) / 30, 0, 1)
    y = y + 1.6 * wobble((H, W), 110, 82) * np.clip(np.minimum(y, H - y) / 30, 0, 1)
    base = hexcol("e5e1d8")
    img = flat((H, W), "e5e1d8")
    img = coat(img, patch((H, W), 330, 760, 230, 83, feather=40, wrap=False), (0.955, 0.955, 0.95))
    img = _put(img, _ring(x, y, 5, 5, W - 5, H - 5, 6, 8), base * 0.86)
    for (x0, x1) in ((62, 232), (280, 450)):
        for (y0, y1) in ((64, 300), (352, 640), (692, 958)):
            img = _put(img, _rect(x, y, x0, y0, x1, y1, 9), base * 0.84)             # groove
            img = _put(img, _rect(x, y, x0 + 7, y0 + 7, x1 - 7, y1 - 7, 7), base * 1.035)   # lit lower lip
            img = _put(img, _rect(x, y, x0 + 7, y0 + 7, x1 - 7, y1 - 19, 7), base * 0.9)    # shaded top
            img = _put(img, _rect(x, y, x0 + 7, y0 + 21, x1 - 7, y1 - 19, 7), base * 0.955)  # the panel
    kx, ky = 481, 530
    d = np.hypot(x - kx, y - ky)
    img = _put(img, np.clip((19 - d) / 1.3 + 0.5, 0, 1), base * 0.8)                 # rose plate
    img = _put(img, np.clip((13 - d) / 1.3 + 0.5, 0, 1), hexcol("7d7868"))
    img = _put(img, np.clip((5 - np.hypot(x - kx + 4, y - ky + 4)) / 1.3 + 0.5, 0, 1), hexcol("b5b0a0"))
    save("door_wood", img)


def gate():
    """A painted steel sheet gate card, 512 x 1024, neutral light for tinting. An angle-bar
    frame, a welded diamond grille over the open top third, a plain sheet below with two
    pressed ribs, a kick rail and a small latch plate."""
    W, H = 512, 1024
    y, x = grid(H, W)
    ex = np.clip(np.minimum(x, W - x) / 30, 0, 1)
    ey = np.clip(np.minimum(y, H - y) / 30, 0, 1)
    x = x + 1.6 * wobble((H, W), 120, 91) * ex
    y = y + 1.6 * wobble((H, W), 120, 92) * ey
    base = hexcol("dedede")
    img = flat((H, W), "dedede")
    img = coat(img, patch((H, W), 250, 760, 300, 93, feather=40, lobes=0.3, aspect=1.3, wrap=False), (0.962, 0.962, 0.962))
    top0, top1 = 34, 338
    hole = _rect(x, y, 34, top0, W - 34, top1, 6)
    u, v = (x + y) / np.sqrt(2), (x - y) / np.sqrt(2)
    pitch = 78.0
    bars = np.maximum(line(wrapdist(u, 20, pitch), 13.0), line(wrapdist(v, 20, pitch), 13.0))
    img = _put(img, hole * (1 - bars), hexcol("4b4b4d"))
    # a flat drawn shade on the bars' lower right, so the grille reads as bars, not print
    edge = np.maximum(line(wrapdist(u - 5, 20, pitch), 4.0), line(wrapdist(v - 5, 20, pitch), 4.0))
    img = _put(img, hole * bars * edge * 0.0, base * 0.9)
    img = _put(img, _ring(x, y, 28, top0 - 6, W - 28, top1 + 6, 6, 7), base * 0.83)
    for xr in (182, 330):
        img = _put(img, line(np.abs(x - xr), 5.0) * (y > 372) * (y < 912), base * 0.87)
        img = _put(img, line(np.abs(x - xr - 5), 4.0) * (y > 372) * (y < 912), base * 1.04)
    img = _put(img, line(np.abs(y - 928), 5.0) * (x > 30) * (x < W - 30), base * 0.86)
    img = _put(img, _ring(x, y, 5, 5, W - 5, H - 5, 6, 8), base * 0.83)
    img = _put(img, _rect(x, y, 420, 600, 478, 664, 7), base * 0.8)
    img = _put(img, _rect(x, y, 436, 622, 462, 642, 5), hexcol("6c6c6c"))
    save("gate", img)


def grille():
    """A window security grille card, 512 px, RGBA: white bars (tinted by the material),
    transparent between them. Its own drawing: a flat-bar frame, two cross rails, and seven
    vertical S-wave bars, neighbours mirrored, all drawn freehand."""
    S = 512
    y, x = grid(S, S)
    e = np.clip(np.minimum(np.minimum(x, S - x), np.minimum(y, S - y)) / 30, 0, 1)
    x = x + 1.8 * wobble((S, S), 70, 101) * e
    y = y + 1.8 * wobble((S, S), 70, 102) * e
    a = _ring(x, y, 3, 3, S - 3, S - 3, 15, 5)
    for yr in (172, 340):
        a = np.maximum(a, line(np.abs(y - yr), 10.0))
    n, amp, period = 7, 15.0, 168.0
    for k in range(n):
        xk = 3 + (k + 0.5) * (S - 6) / n
        sgn = 1 if k % 2 == 0 else -1
        ph = 2 * np.pi * (y - 4) / period
        cx = xk + sgn * amp * np.sin(ph)
        slope = sgn * amp * 2 * np.pi / period * np.cos(ph)
        a = np.maximum(a, line(np.abs(x - cx) / np.sqrt(1 + slope ** 2), 10.0))
    rgb = np.full((S, S, 3), 0.97)
    save("grille", np.dstack([rgb, a]))


def chalk():
    """Chalk line card, 512 px, RGBA: plain off-white, a band down the middle whose two long
    edges are soft and uneven. Repeats along its length (texture Y)."""
    S = 512
    y, x = grid(S, S)
    l = 60 + 7 * wobble((S, S), 60, 111, stretch=(1e3, 1.0))
    r = S - 60 + 7 * wobble((S, S), 60, 112, stretch=(1e3, 1.0))
    a = soft((x - l) / 26 + 0.5) * soft((r - x) / 26 + 0.5)
    img = flat((S, S), "f2efe7")
    save("chalk", np.dstack([img, a]))


PAINTERS = {
    # name: (painter, tile metres or None for a card)
    "chb": (chb, 2.0),
    "tin": (tin, 2.0),
    "tin_rust": (tin_rust, 2.0),
    "planks": (planks, 2.0),
    "floor": (floor, 4.0),
    "concrete": (concrete, 2.0),
    "trapal_teal": (lambda: trapal("trapal_teal", "2f8f76", 121), 1.0),
    "trapal_red": (lambda: trapal("trapal_red", "c0463c", 131), 1.0),
    "trapal_yellow": (lambda: trapal("trapal_yellow", "e6c243", 141), 1.0),
    "window_slide": (window_slide, None),
    "door_wood": (door_wood, None),
    "gate": (gate, None),
    "grille": (grille, None),
    "chalk": (chalk, None),
}

ORDER = ["chb", "plaster", "tin", "tin_rust", "planks", "floor", "concrete", "tiles_clay", "wood", "paint",
         "trapal_teal", "trapal_red", "trapal_yellow", "sawali", "jalousie", "window_slide", "door_wood", "gate",
         "grille", "leaf", "chalk"]
APPROVED_FOR_COMPARISON = ["brick", "paving"]     # Kanto swatches shown on the sheet, not written


def copy_all():
    OUT.mkdir(parents=True, exist_ok=True)
    for name, (src, _) in COPIES.items():
        shutil.copyfile(ART / src, OUT / f"{name}_albedo.png")
        normal = ART / src.replace("_albedo.png", "_normal.png")
        if normal.exists():
            shutil.copyfile(normal, OUT / f"{name}_normal.png")
        print("[eskinita-tex] copied", name, "from", src)


def write_manifest():
    entries = []
    for name in ORDER:
        if name in COPIES:
            src, tile = COPIES[name]
            source = f"copied from Assets/TumbangPreso/Art/{src}"
        else:
            tile = PAINTERS[name][1]
            source = "painted"
        w, h = Image.open(OUT / f"{name}_albedo.png").size
        entries.append({"name": name, "tile_m": tile, "kind": "tile" if tile else "card", "source": source,
                        "file": f"{name}_albedo.png", "size": [w, h],
                        "normal": f"{name}_normal.png" if (OUT / f"{name}_normal.png").exists() else None,
                        "alpha": Image.open(OUT / f"{name}_albedo.png").mode == "RGBA"})
    MANIFEST.write_text(json.dumps({"textures": entries}, indent=2) + "\n", encoding="utf-8")
    print("[eskinita-tex] manifest", MANIFEST)


def _checker(size, cell=16):
    yy, xx = np.mgrid[0:size[1], 0:size[0]]
    c = np.where(((xx // cell) + (yy // cell)) % 2 == 0, 150, 110).astype(np.uint8)
    return Image.fromarray(np.dstack([c, c, c]))


def swatch_sheet(version):
    """Every texture flat, and as a 3 x 3 repeat when it tiles, plus two approved Kanto
    swatches for a side by side read."""
    cell, pad, label = 300, 10, 20
    items = [(n, OUT / f"{n}_albedo.png", (COPIES[n][1] if n in COPIES else PAINTERS[n][1]),
              "copied" if n in COPIES else "painted") for n in ORDER]
    items += [(f"KANTO {n} (approved)", ART / "Kanto" / "Textures" / f"{n}_albedo.png", 2.0, "reference")
              for n in APPROVED_FOR_COMPARISON]
    per_row = 3
    rows = (len(items) + per_row - 1) // per_row
    cw = 2 * cell + 3 * pad
    sheet = Image.new("RGB", (per_row * cw, rows * (cell + label + 2 * pad)), (46, 46, 46))
    d = ImageDraw.Draw(sheet)
    for i, (name, path, tile, how) in enumerate(items):
        ox, oy = (i % per_row) * cw + pad, (i // per_row) * (cell + label + 2 * pad) + pad
        im = Image.open(path)
        text = f"{name}  [{how}]  " + (f"tile {tile:g} m, flat | 3 x 3" if tile else f"card {im.size[0]} x {im.size[1]}")
        d.text((ox, oy), text, fill=(240, 240, 240))

        def fit(img):
            img = img.copy()
            img.thumbnail((cell, cell), Image.LANCZOS)
            bg = _checker(img.size)
            if img.mode == "RGBA":
                bg.paste(img, (0, 0), img)
                return bg
            return img.convert("RGB")
        sheet.paste(fit(im), (ox, oy + label))
        if tile:
            rep = Image.new(im.mode, (im.size[0] * 3, im.size[1] * 3))
            for a in range(3):
                for b in range(3):
                    rep.paste(im, (a * im.size[0], b * im.size[1]))
            sheet.paste(fit(rep), (ox + cell + pad, oy + label))
    LOGS.mkdir(parents=True, exist_ok=True)
    out = LOGS / f"textures_swatches_v{version}.png"
    sheet.save(out)
    print("[eskinita-tex] swatches", out)


if __name__ == "__main__":
    version = int(sys.argv[sys.argv.index("--sheet") + 1]) if "--sheet" in sys.argv else 1
    wanted = sys.argv[sys.argv.index("--only") + 1].split(",") if "--only" in sys.argv else None
    if wanted is None:
        copy_all()
    for name, (painter, _) in PAINTERS.items():
        if wanted is None or name in wanted:
            painter()
    write_manifest()
    swatch_sheet(version)
