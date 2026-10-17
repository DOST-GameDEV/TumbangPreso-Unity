"""Paints the masks of the water effects of Ilyas (`Runtime/Visual/FxStudy/FxStudyRafiWall.cs`): the ones the effects
bench did not have, because it had never built water. Run from the worktree root:

    py -3 tools/build_rafi_water_textures.py [out dir]

Every mask is plain grey, read by `FxStudyRibbon.shader` (dark grey is the deep colour, mid grey the body, white the
hot colour), drawn here from typed numbers and fixed seeds. Nothing is traced or taken from anywhere. In every sheet x
runs ALONG the piece that wears it and y across it; row 0 of the array is v 0, so the file is flipped as it is saved.

What was read off stylised game waterfalls before painting (looked at, not copied):
  * falling water is LONG STREAKS along the fall, a saturated body with darker gaps, and thin near-white lines over it;
  * the white is in those thin lines and in round foam lobes at the foot, never across the body;
  * the foot is lobes of foam and ripple rings; a few fat drops are thrown.

  water_flow   the body of a falling sheet: one coarse noise drawn out about six to one along the fall, a slope from
               each streak's edge to its spine, with real gaps (the far side shows through).
  water_lines  the thin bright lines: contour lines of a second such noise, broken into runs.
  water_foam   foam lobes hanging from one edge (v 0): a pale body with a bright rim on each lobe, and a few beads.
  water_ring   one broken ripple ring round a wall's foot, as a stadium (it is laid on a square 3.25 times as wide).
  water_crest  the same construction as the foam with five big lobes: the splash crown where the wall lands.
  water_drop   four fat drops, two a side: a teardrop, a bead, a long drop and a pair.
"""
import os
import sys

import cv2
import numpy as np
from scipy import ndimage

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join("Assets", "TumbangPreso", "Resources", "Textures", "UltimateFx")
N = 512


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a + 1e-9), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def drawn_noise(seed, along, across, n=N):
    """Soft noise that wraps both ways with about `along` lumps along x and `across` lumps along y: white noise with
    everything finer than that filtered away. 0 to 1."""
    rng = np.random.default_rng(seed)
    spectrum = np.fft.fft2(rng.standard_normal((n, n)))
    fy = np.fft.fftfreq(n)[:, None] * n / across
    fx = np.fft.fftfreq(n)[None, :] * n / along
    out = np.real(np.fft.ifft2(spectrum * np.exp(-(fx * fx + fy * fy) * 0.9)))
    out -= out.min()
    return out / out.max()


def save(name, image):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".png")
    cv2.imwrite(path, np.flipud(np.clip(image * 255.0 + 0.5, 0, 255).astype(np.uint8)))
    return path


def water_flow():
    # ⚠️ KEEP. This is the sheet of the first sample (film w2), which the owner chose over a second with five wide
    # streaks and big gaps ("rafi use the first"). Two lumps along the fall and twelve across it: every streak is about
    # six times as long as it is wide.
    raw = drawn_noise(701, 2, 12)
    # A slow sideways wander, so the streaks are not ruler lines.
    ys, xs = np.mgrid[0:N, 0:N].astype(np.float32)
    wander = (drawn_noise(702, 2, 2) - 0.5) * (N / 14.0)
    raw = cv2.remap(raw.astype(np.float32), xs, (ys + wander.astype(np.float32)) % N, cv2.INTER_LINEAR, borderMode=cv2.BORDER_WRAP)
    # Lifted until 68 percent of the sheet is not black: a full body with real gaps in it.
    lifted = raw - np.quantile(raw, 1.0 - 0.68)
    return np.clip(lifted / np.quantile(lifted, 0.995), 0, 1)


def water_lines():
    # Contour lines of a noise drawn out further still: thin, long, and each a little crooked.
    raw = drawn_noise(711, 1, 16)
    line = np.zeros_like(raw)
    for level, wide in ((0.36, 0.030), (0.52, 0.024), (0.66, 0.030)):
        line = np.maximum(line, 1.0 - smooth(0.0, wide, np.abs(raw - level)))
    # Only runs of them: a second coarse noise leaves most of the sheet plain.
    runs = smooth(0.40, 0.54, drawn_noise(712, 2, 5))
    return np.clip(line * runs, 0, 1)


def water_foam(n=N, seed=721, aspect=1.0, count=11, small=(0.07, 0.15), beads=7):
    """`aspect` is how many times longer than wide the strip that wears one sheet is: the lobes are drawn that much
    narrower along x, so they come out round on it."""
    rng = np.random.default_rng(seed)
    ys, xs = np.mgrid[0:n, 0:n].astype(np.float64)
    u, v = xs / n, ys / n
    rim = np.zeros((n, n))
    # A band along the edge the foam hangs from, then lobes of typed sizes hung along it. x wraps.
    body = 1.0 - smooth(0.10, 0.16, v)
    lobes = [(i / float(count) + rng.uniform(-0.02, 0.02), rng.uniform(0.12, 0.34), rng.uniform(*small)) for i in range(count)]
    lobes += [(rng.uniform(0, 1), rng.uniform(0.50, 0.80), rng.uniform(0.022, 0.05)) for _ in range(beads)]
    for cx, cy, r in lobes:
        for shift in (-1.0, 0.0, 1.0):
            d = np.hypot((u - cx - shift) * aspect, (v - cy)) / r
            body = np.maximum(body, 1.0 - smooth(0.90, 1.0, d))
            # The bright rim is on the outer side of each lobe only (away from the edge it hangs from).
            outer = smooth(-0.2, 0.7, (v - cy) / r)
            rim = np.maximum(rim, (1.0 - smooth(0.0, 0.16, np.abs(d - 0.84))) * outer * (d < 1.0))
    # A thin bright line along the hanging edge itself: the crest.
    crest = 1.0 - smooth(0.0, 0.035, np.abs(v - 0.045))
    image = body * 0.62 + np.maximum(rim, crest) * 0.38 * body
    return np.clip(ndimage.gaussian_filter(image, 1.2, mode=("nearest", "wrap")), 0, 1)


def water_ring(n=N, aspect=3.25):
    ys, xs = np.mgrid[0:n, 0:n].astype(np.float64)
    # The square is laid `aspect` times as wide as deep, so the ring is drawn in metres and comes out a stadium.
    x, y = (xs / n - 0.5) * 2.0 * aspect, (ys / n - 0.5) * 2.0
    half = aspect - 1.0
    d = np.hypot(np.maximum(np.abs(x) - half * 0.86, 0.0), y)
    ring = 1.0 - smooth(0.0, 0.06, np.abs(d - 0.74))
    inner = (1.0 - smooth(0.0, 0.03, np.abs(d - 0.46))) * 0.45
    # Broken into arcs by a coarse noise, so no two breaks match.
    gaps = smooth(0.50, 0.66, drawn_noise(731, 5, 5, n))
    gaps2 = smooth(0.50, 0.60, drawn_noise(732, 5, 5, n))
    # Soft: a spreading arc of wet, not a drawn line.
    return np.clip(ndimage.gaussian_filter(ring * gaps + inner * gaps2, 2.5) * 1.25, 0, 1)


def water_drop(n=256):
    sheet = np.zeros((n, n))
    half = n // 2
    ys, xs = np.mgrid[0:half, 0:half].astype(np.float64)
    x, y = xs / half, ys / half - 0.5

    def bead(cx, cy, r):
        d = np.hypot(x - cx, y - cy) / r
        fill = (1.0 - smooth(0.86, 1.0, d)) * 0.60
        glint = 1.0 - smooth(0.0, 0.30, np.hypot(x - cx - r * 0.25, y - cy - r * 0.30) / r)
        return np.maximum(fill, glint * (d < 1.0))

    def tear(head, r, tail):
        # A round head at `head` along x, drawn back to a point at `tail`.
        t = np.clip((x - tail) / (head - tail), 0.0, 1.0)
        wide = r * t ** 0.75
        body = (np.abs(y) < wide) & (x >= tail) & (x <= head)
        cap = np.hypot(x - head, y) < r
        fill = np.where(body | cap, 0.60, 0.0)
        glint = 1.0 - smooth(0.0, 0.30, np.hypot(x - head - r * 0.15, y - r * 0.30) / r)
        return ndimage.gaussian_filter(np.maximum(fill, glint * (fill > 0)), 1.0)

    cells = [tear(0.70, 0.19, 0.12), bead(0.52, 0.0, 0.24), tear(0.76, 0.13, 0.08),
             np.maximum(bead(0.66, 0.06, 0.17), bead(0.30, -0.10, 0.10))]
    for i, cell in enumerate(cells):
        r, c = (i // 2) * half, (i % 2) * half
        sheet[r:r + half, c:c + half] = ndimage.gaussian_filter(cell, 0.8)
    return np.clip(sheet, 0, 1)


def build():
    made = {"water_flow": water_flow(), "water_lines": water_lines(), "water_foam": water_foam(),
            "water_ring": water_ring(), "water_drop": water_drop(),
            # The splash crown: five big lobes a sheet, round on a strip 3.3 times as long as it is wide.
            "water_crest": water_foam(seed=741, aspect=3.3, count=5, small=(0.20, 0.38), beads=3)}
    for name, image in made.items():
        print("wrote", save(name, image), "%.0f percent not black, %.0f percent bright" % (100 * (image > 0.06).mean(), 100 * (image > 0.8).mean()))
    return made


if __name__ == "__main__":
    build()
