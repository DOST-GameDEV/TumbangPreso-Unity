"""PAYO night fires (2026-10-19): paints the fire kit's atlas, its height map and its normal map. System Python.

  python -I tools/payo_pieces/fire_paint.py <ABS out folder>
      writes fire_kit-atlas.png, fire_kit_height.png, fire_kit_normal.png

The calm rules (Logs/payo/pieces/CALM_STYLE.md): one base colour per surface with one soft gradient over the whole
face, contrast between surfaces and not inside them, a few BIG bold motifs (the fret and the diamond, the woven band),
no speckle; shape goes into the normal map (carving as relief, joints as soft grooves, coals as round lumps), low
frequency only. The stone matches the shared calm value (about 165, 156, 140). Every region tiles across its width
(a lathe's way round runs across it).
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import fire_layout as L  # noqa: E402
from calm_glb import height_to_normal, save_height  # noqa: E402

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "..", "..", "Logs", "payo", "pieces", "fire")
os.makedirs(OUT, exist_ok=True)
S = L.SIZE
A = np.zeros((S, S, 3), np.float32)
H = np.zeros((S, S), np.float32)


def grid(name):
    """The region's pixel grid: fu 0..1 across (tiling), fv 0..1 from its bottom to its top, and its slices."""
    x0, y0, x1, y1 = L.R[name]
    w, h = x1 - x0, y1 - y0
    fu = (np.arange(w) + 0.5)[None, :] / w * np.ones((h, 1))
    fv = 1.0 - (np.arange(h) + 0.5)[:, None] / h * np.ones((1, w))
    return fu, fv, (slice(y0, y1), slice(x0, x1))


def c(rgb):
    return np.array(rgb, np.float32) / 255.0


def mix(a, b, t):
    t = np.clip(t, 0, 1)[..., None]
    return a * (1 - t) + b * t


def soft(d, w):
    """1 inside (d < 0) to 0 outside, over a soft edge of width w (in the same units as d)."""
    return np.clip(0.5 - d / w, 0, 1)


def wave(fu, n):
    """Distance-ish coordinate repeating n times across: -0.5..0.5 inside each repeat."""
    return (fu * n) % 1.0 - 0.5


# ---------------------------------------------------------------- the stone (the pedestal's sides)
fu, fv, s = grid("stone")
top, foot = c((176, 166, 148)), c((146, 139, 129))
A[s] = mix(foot, top, fv ** 0.8)
# two soft course joints (big blocks), contrast under 10 per cent: a gentle groove in the height, a faint line in colour
for j in (0.34, 0.68):
    g = np.exp(-((fv - j) / 0.018) ** 2)
    H[s] -= 0.006 * g
    A[s] *= (1 - 0.06 * g)[..., None]
# a rounded block face: each course swells a little between its joints
H[s] += 0.004 * np.sin(np.clip(fv / 0.34 % 1.0, 0, 1) * np.pi)

# ---------------------------------------------------------------- the carved band (fret and diamond, 4 repeats)
fu, fv, s = grid("band")
ground = c((182, 171, 152))
A[s] = mix(c((160, 151, 137)), ground, 0.5 + 0.5 * fv)
n = 4
x = wave(fu, n) * 2.0          # -1..1 across one repeat (0.5 m)
y = fv * 2.0 - 1.0             # -1..1 up the band
# the frame lines top and bottom
frame = (np.abs(y) > 0.78) & (np.abs(y) < 0.92)
# a bold diamond in the middle of each repeat, terracotta
dia = np.abs(x) * 1.0 + np.abs(y) * 0.62
diamond = soft(dia - 0.42, 0.05) - soft(dia - 0.18, 0.05)          # a diamond ring
dot = soft(dia - 0.1, 0.05)
# a stack of three bold chevrons either side (ochre), pointing out from the diamond (the Cordillera zigzag)
xx = np.abs(x)
fret = np.zeros_like(x)
for k in range(3):
    cx = 0.58 + 0.17 * k
    d2 = np.abs((xx - cx) - np.abs(y) * 0.32)
    fret = np.maximum(fret, soft(d2 - 0.045, 0.04) * (np.abs(y) < 0.62))
terracotta, ochre = c((198, 104, 72)), c((214, 160, 84))
A[s] = mix(A[s], ochre, fret)
A[s] = mix(A[s], terracotta, np.maximum(diamond, dot))
A[s] = mix(A[s], c((150, 140, 126)), frame.astype(np.float32))
H[s] += 0.012 * np.clip(np.maximum(fret, np.maximum(diamond, dot)), 0, 1)
H[s] -= 0.006 * frame

# ---------------------------------------------------------------- the woven band (the copings' red-black-white diamonds)
fu, fv, s = grid("weave")
red = c((168, 52, 40))
A[s] = red
x = wave(fu, 8) * 2.0
y = fv * 2.0 - 1.0
d = np.abs(x) + np.abs(y) * 0.9
A[s] = mix(A[s], c((30, 24, 24)), soft(d - 0.72, 0.08))
A[s] = mix(A[s], c((236, 226, 206)), soft(d - 0.42, 0.08))
A[s] = mix(A[s], c((30, 24, 24)), soft(d - 0.14, 0.08))
H[s] += 0.004 * soft(d - 0.72, 0.2)

# ---------------------------------------------------------------- the flat stone (plinth tops, the bowl's inside)
fu, fv, s = grid("flat")
A[s] = mix(c((150, 142, 130)), c((172, 163, 146)), fv)
H[s] += 0.003 * np.sin(fu * np.pi) * np.sin(fv * np.pi)

# ---------------------------------------------------------------- the coals: round dark lumps, a few warm seams
fu, fv, s = grid("coal")
rng = np.random.default_rng(7)
lumps = np.zeros_like(fu)
for _ in range(40):
    cx, cy, r = rng.uniform(0, 1), rng.uniform(0, 1), rng.uniform(0.09, 0.17)
    for ox in (-1, 0, 1):                       # tiles across
        dd = np.sqrt((fu - cx - ox) ** 2 + (fv - cy) ** 2) / r
        lumps = np.maximum(lumps, np.clip(1 - dd * dd, 0, 1))
A[s] = mix(c((92, 34, 22)), c((40, 33, 31)), np.clip(lumps * 5.0, 0, 1))        # seams glow dull red between lumps
A[s] = mix(A[s], c((64, 54, 50)), np.clip((lumps - 0.6) * 2.5, 0, 1))          # the lumps' tops catch a little light
H[s] += 0.02 * np.sqrt(lumps)

# ---------------------------------------------------------------- bamboo: yellow-green to ochre, 5 nodes up the pole
fu, fv, s = grid("bamboo")
A[s] = mix(c((150, 146, 82)), c((196, 168, 104)), fv ** 0.7)
for k in range(1, 6):
    j = k / 6.0
    g = np.exp(-((fv - j) / 0.012) ** 2)
    A[s] = mix(A[s], c((132, 116, 60)), g * 0.8)
    H[s] += 0.004 * g
# a soft lighter stripe down one side of each cane (its sheen), wide
A[s] *= (1 + 0.05 * np.cos(fu * 2 * np.pi))[..., None]

# ---------------------------------------------------------------- the basket head: split cane, big diamonds
fu, fv, s = grid("basket")
A[s] = mix(c((168, 128, 74)), c((212, 176, 110)), fv)
x = wave(fu, 6) * 2.0
y = wave(fv, 2) * 2.0
d = np.abs(x) + np.abs(y)
band = soft(np.abs(d - 0.7) - 0.16, 0.08)
A[s] = mix(A[s], c((140, 96, 54)), band * 0.85)
H[s] += 0.006 * band

# ---------------------------------------------------------------- the bindings: red-dyed rattan, two strand grooves
fu, fv, s = grid("binding")
A[s] = mix(c((140, 44, 34)), c((184, 66, 46)), np.sin(fv * np.pi))
H[s] += 0.004 * np.abs(np.sin(fv * np.pi * 3))

# ---------------------------------------------------------------- river stones at the torch's foot
fu, fv, s = grid("river")
A[s] = mix(c((112, 116, 122)), c((150, 152, 150)), fv)
H[s] += 0.006 * np.sin(fu * np.pi) * np.sin(fv * np.pi)

# ---------------------------------------------------------------- write
img = (np.clip(A, 0, 1) * 255 + 0.5).astype(np.uint8)
Image.fromarray(img, "RGB").save(os.path.join(OUT, "fire_kit-atlas.png"))
save_height(H, os.path.join(OUT, "fire_kit_height.png"), -0.03, 0.03)
rects = [(*L.R[k], L.PPM) for k in L.R]
N = height_to_normal(H, rects, strength=3.0, blur=1.2)
Image.fromarray(N, "RGB").save(os.path.join(OUT, "fire_kit_normal.png"))
print("FIRE PAINT", OUT)
