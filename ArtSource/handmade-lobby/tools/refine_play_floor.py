"""Repaints PLAY's floor (play_slab.png) as one clean gradient (owner, 2026-11-02: no texture in its shadow). Her shape and size stay; only the slab's colours change.

It starts just under the face in a green from the knit's own stripes, deepens evenly over the same height all along the button, and its last pixels turn under. The gradient follows the face's lower edge, chamfer included. Run from the repo root, after assemble_board.py, which restores her original floor.
"""
import numpy as np
from PIL import Image

D = 'Assets/TumbangPreso/Resources/UI/handmade-lobby/'
STOPS = [(0.0, (116, 168, 60)), (0.4, (84, 146, 54)), (0.8, (48, 112, 48)), (1.0, (30, 86, 42))]

face = np.asarray(Image.open(D + 'play_face.png').convert('RGBA'))[..., 3] > 127
slab = np.asarray(Image.open(D + 'play_slab.png').convert('RGBA')).copy()
h, w = face.shape
bottom = np.nonzero((slab[..., 3] > 127).any(axis=1))[0].max()
floor = np.nonzero(face.any(axis=1))[0].max() + 1
ys = np.arange(h)[:, None]
tops = np.array([np.nonzero(face[:, x])[0].max() + 1 if face[:, x].any() else floor for x in range(w)])
# Under the flat run of the face this is one even band. In the chamfer it starts higher, on the same colours.
t = np.clip((ys - tops[None, :]) / np.maximum(1, bottom - tops[None, :]), 0, 1)
at = [s[0] for s in STOPS]
for c in range(3):
    slab[..., c] = np.round(np.interp(t, at, [s[1][c] for s in STOPS])).astype(np.uint8)
Image.fromarray(slab, 'RGBA').save(D + 'play_slab.png')
print('floor rows', floor, 'to', bottom)
