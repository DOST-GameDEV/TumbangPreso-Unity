"""Repaints PLAY's floor (play_slab.png) as one clean gradient (owner, 2026-11-02: no texture in its shadow). Her shape and size stay; only the slab's colours change.

It starts just under the face in a green from the knit's own stripes, deepens evenly over the same height all along the button, and its last pixels turn under. The gradient follows the face's lower edge, chamfer included. Run from the repo root, after assemble_board.py, which restores her original floor.
"""
import numpy as np
from PIL import Image

D = 'Assets/TumbangPreso/Resources/UI/handmade-lobby/'
STOPS = [(0.0, (104, 160, 58)), (0.5, (74, 136, 52)), (1.0, (40, 100, 46))]
# How tall the floor stands under the face, in this file's pixels (owner, 2026-11-02: "cut a bit of the bottom"). Rerunning changes nothing.
FLOOR = 38

face = np.asarray(Image.open(D + 'play_face.png').convert('RGBA'))[..., 3] > 127
slab = np.asarray(Image.open(D + 'play_slab.png').convert('RGBA')).copy()
h, w = face.shape
bottom = np.nonzero((slab[..., 3] > 127).any(axis=1))[0].max()
floor = np.nonzero(face.any(axis=1))[0].max() + 1
# Take the height out of the straight middle of the floor, so its rounded corners keep their shape.
cut = bottom - (floor + FLOOR)
if cut > 0:
    keep = floor + 8
    slab = np.concatenate([slab[:keep], slab[keep + cut:], np.zeros((cut, w, 4), np.uint8)])
    bottom -= cut
# Cutting takes a slice out of the corners too, which leaves a notch. So the bottom corners are drawn again as one true curve (owner: "make sure it's still round").
R = 44.0
solid = np.nonzero((slab[..., 3] > 127).any(axis=0))[0]
left, right = solid.min(), solid.max() + 1
for y in range(int(bottom + 1 - R), h):
    for x in range(w):
        cover = 0
        for sy in (0.25, 0.75):
            for sx in (0.25, 0.75):
                px, py = x + sx, y + sy
                dy = max(0.0, py - (bottom + 1 - R))
                dx = max(0.0, (left + R) - px, px - (right - R))
                cover += px >= left and px <= right and py <= bottom + 1 and dx * dx + dy * dy <= R * R
        slab[y, x, 3] = int(cover * 255 / 4)
ys = np.arange(h)[:, None]
tops = np.array([np.nonzero(face[:, x])[0].max() + 1 if face[:, x].any() else floor for x in range(w)])
# Under the flat run of the face this is one even band. In the chamfer it starts higher, on the same colours.
t = np.clip((ys - tops[None, :]) / np.maximum(1, bottom - tops[None, :]), 0, 1)
at = [s[0] for s in STOPS]
for c in range(3):
    slab[..., c] = np.round(np.interp(t, at, [s[1][c] for s in STOPS])).astype(np.uint8)
Image.fromarray(slab, 'RGBA').save(D + 'play_slab.png')
print('floor rows', floor, 'to', bottom)
