"""Redraws PLAY's slab and rounds its face, the way HOME's tiles are made (owner, 2026-11-02: no texture in its shadow, like the other buttons but better, smaller, with more depth, in colours that follow the knit).

The slab is a clean rounded plate set out to the lower right of the face, in darker greens taken from the face's stripes, dark on the left and lit on the right. The face shades it where they meet, its bottom edge turns under, and a soft shadow sits on the ground.
board.json holds the button's size (8 per cent up, with room won from the shorter slab) and HubKnit its margins. Run from the repo root, after assemble_board.py, which restores her art. It reads her original face, so run it once.
"""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

D = 'Assets/TumbangPreso/Resources/UI/handmade-lobby/'
W, H = 914, 309
LEFT, TOP, RIGHT, BOTTOM, RADIUS = 18, 40, 901, 293, 52
DARK, LIT = np.array((30, 98, 44.0)), np.array((98, 158, 48.0))
SS = 4


def rounded(box, size, radius=RADIUS):
    mask = Image.new('L', (size[0] * SS, size[1] * SS), 0)
    ImageDraw.Draw(mask).rounded_rectangle([v * SS for v in box], radius * SS, fill=255)
    return mask.resize(size, Image.LANCZOS)


# The face first: lower corners rounded like its top ones. The chamfer's missing corner, a little wider than the hole because its edge carries a pale fringe, is filled from one stripe period to the left.
face = np.asarray(Image.open(D + 'play_face.png').convert('RGBA'), dtype=np.float32).copy()
solid = face[..., 3] > 127
rows, cols = np.nonzero(solid.any(axis=1))[0], np.nonzero(solid.any(axis=0))[0]
top, bottom, left, right = rows.min(), rows.max(), cols.min(), cols.max()
strip = face[top + 60, left + 8:right - 8, :3].mean(axis=1)
strip = strip - strip.mean()
period = 150 + int(np.argmax([float((strip[:-k] * strip[k:]).mean()) for k in range(150, 400)]))
hole = np.asarray(Image.fromarray((face[..., 3] > 250).astype(np.uint8) * 255).filter(ImageFilter.MinFilter(15))) < 128
for py in range(bottom - 70, bottom + 1):
    for px in range(right - 70, right + 1):
        if hole[py, px]:
            face[py, px, :3] = face[py, px - period, :3]
fh, fw = face.shape[:2]
shape = rounded((left, top, right + 1, bottom + 1), (fw, fh))
face[..., 3] = np.asarray(shape, dtype=np.float32)
# Thickness: a soft light along its top edge, a soft shade along its bottom. The knit stays.
ys = np.arange(fh)[:, None, None]
lift = np.clip(1 - (ys - top) / 16, 0, 1) ** 2 * 0.1
sink = np.clip(1 - (bottom - ys) / 26, 0, 1) ** 2 * 0.2
face[..., :3] = np.clip(face[..., :3] * (1 + lift - sink), 0, 255)
Image.fromarray(face.round().astype(np.uint8), 'RGBA').save(D + 'play_face.png')

# The slab.
plate = np.asarray(rounded((LEFT, TOP, RIGHT, BOTTOM), (W, H)), dtype=np.float32) / 255
ground = Image.new('L', (W, H), 0)
ImageDraw.Draw(ground).rounded_rectangle((LEFT + 4, TOP + 8, RIGHT - 2, BOTTOM + 5), RADIUS, fill=255)
ground = np.asarray(ground.filter(ImageFilter.GaussianBlur(5)), dtype=np.float32) / 255 * 0.42

x = np.clip((np.arange(W) - LEFT) / (RIGHT - LEFT), 0, 1)[None, :, None]
x = x * x * (3 - 2 * x)
rgb = DARK + (LIT - DARK) * x
y = np.clip((np.arange(H) - (BOTTOM - 26)) / 26, 0, 1)[:, None, None]
rgb = rgb * (1 - 0.24 * y * y)
# The face's shadow on the slab, thrown to the lower right, so the face stands off it.
cast = Image.new('L', (W, H), 0)
cast.paste(shape, (5, 8))
cast = np.asarray(cast.filter(ImageFilter.GaussianBlur(9)), dtype=np.float32)[..., None] / 255
rgb = rgb * (1 - 0.42 * cast)

shade = np.array((30, 10, 16.0))
alpha = plate + ground * (1 - plate)
colour = (rgb * plate[..., None] + shade * (ground * (1 - plate))[..., None]) / np.maximum(alpha, 1e-4)[..., None]
out = np.dstack((np.clip(colour, 0, 255), alpha * 255)).round().astype(np.uint8)
Image.fromarray(out, 'RGBA').save(D + 'play_slab.png')
print('face', left, top, right, bottom, 'stripe period', period)
