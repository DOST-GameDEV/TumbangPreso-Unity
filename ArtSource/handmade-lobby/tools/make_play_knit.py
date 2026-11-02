"""Bakes the yarn out of play_face.png for the PLAY barber stripes (Shaders/PlayBarber).

Colour is removed by dividing each pixel by its own yellow-to-green mix, found from chroma, so
only the knit's light and shadow remain. Red channel: that shading, scaled by 1/SHADE_MAX. Green
channel: the yarn's height, which bends the moving stripe edges around the stitches.
Prints the stripe colours the shader uses. play_face.png is only read.
"""
from pathlib import Path
import numpy as np
import scipy.ndimage as nd
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
FACE = ROOT / 'Assets/TumbangPreso/Resources/UI/handmade-lobby/play_face.png'
OUT = ROOT / 'Assets/TumbangPreso/Resources/UI/play-stripes/play_knit.png'
TILE = ROOT / 'Assets/TumbangPreso/Resources/UI/play-stripes/play_knit_tile.png'
TILE_WIDTH = 750
SHADE_MAX = 1.25
HEIGHT_GAIN = 2.0

art = np.array(Image.open(FACE).convert('RGBA')).astype(float) / 255
rgb, solid = art[..., :3], art[..., 3] > .98
chroma = (rgb[..., 0] - rgb[..., 1]) / (rgb[..., 0] + rgb[..., 1] + 1e-4)
green_c, yellow_c = np.percentile(chroma[solid], [8, 92])
mix = np.clip((chroma - green_c) / (yellow_c - green_c), 0, 1)
yellow = np.median(rgb[solid & (mix > .9)], 0)
green = np.median(rgb[solid & (mix < .1)], 0)
shade = np.where(solid[..., None], rgb / (green + (yellow - green) * mix[..., None]), 1).mean(2)
for band in (mix > .5, mix <= .5):
    shade[solid & band] /= shade[solid & band].mean()

# The old stripe edges and the art's rim are mixed pixels; take their yarn from clean neighbours.
inside = nd.binary_erosion(solid, iterations=3)
clean = inside & ~nd.binary_dilation((mix > .08) & (mix < .92), iterations=1)
nearest = nd.distance_transform_edt(~clean, return_distances=False, return_indices=True)
filled = shade[nearest[0], nearest[1]]
blend = nd.gaussian_filter((~clean).astype(float), 1.2)
shade = shade * (1 - blend) + nd.gaussian_filter(filled, 1.0) * blend
height = nd.gaussian_filter(shade, 2.0) - nd.gaussian_filter(shade, 14)

out = np.zeros(art.shape[:2] + (3,), np.uint8)
out[..., 0] = np.uint8(np.clip(shade / SHADE_MAX, 0, 1) * 255 + .5)
out[..., 1] = np.uint8(np.clip(.5 + height * HEIGHT_GAIN, 0, 1) * 255 + .5)
OUT.parent.mkdir(parents=True, exist_ok=True)
Image.fromarray(out).save(OUT)
# The travelling yarn: the art's inside wrapped to TILE_WIDTH. Each row joins along its least visible cut through the overlap.
band = out[:, 18:873].astype(float)
tile = band[:, :TILE_WIDTH].copy()
overlap = band.shape[1] - TILE_WIDTH
error = ((band[:, :overlap, 0] - band[:, TILE_WIDTH:, 0]) ** 2)
cost = error.copy()
for y in range(1, cost.shape[0]):
    best = nd.minimum_filter1d(cost[y - 1], 3, mode='nearest')
    cost[y] += best
cut = np.zeros(cost.shape[0], int)
cut[-1] = cost[-1, 8:overlap - 8].argmin() + 8
for y in range(cost.shape[0] - 2, -1, -1):
    low = max(8, cut[y + 1] - 1)
    cut[y] = low + cost[y, low:min(overlap - 8, cut[y + 1] + 2)].argmin()
xs = np.arange(overlap)
keep = np.clip((xs[None] - cut[:, None] + 4) / 8, 0, 1)[..., None]
tile[:, :overlap] = band[:, TILE_WIDTH:] * (1 - keep) + band[:, :overlap] * keep
Image.fromarray(np.uint8(tile + .5)).save(TILE)
print('yellow', np.round(yellow * 255), 'green', np.round(green * 255))
print('band means', shade[solid & (mix > .9)].mean(), shade[solid & (mix < .1)].mean())
