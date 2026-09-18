"""Author the September 18 main-menu layers from the owner's supplied plates.

The owner supplied three versions of one painting and the difference between
them IS the animation data, which is why nothing here invents a shape:

    47.png  the clean street, flat sky, no graffiti, no shadow
    48.png  47 plus the TUMP graffiti. THIS IS THE RUNTIME PLATE.
    46.png  48 plus painted sky clouds, their cast shadow, and a caption

So 46 minus 48 is exactly her own cloud art and her own shadow pattern, lifted
off a plate that already has neither. That is what "i gave u a pic without
clouds already so that u can just put our animated subtle clouds in" asks for.

⚠️ THE SKY IS ONE FLAT COLOUR NOW AND THAT IS WHY THIS FILE IS SHORT. The
September 15 pipeline needed PyMatting closed-form matting and a background
estimate because the old sky had painted clouds baked into it, so the runtime
had to subtract an unknown per pixel. Here the thing behind the clouds is a
constant, so the whole composite collapses to

    result = plate + opening * cloudAlpha * (cloudRGB - SKY)

and there is nothing left to estimate. Do not reintroduce the matting stack.

Outputs, all into Resources/UI/owner-menu-edits:

    main2-background.png    48.png unchanged, the plate the menu draws
    main2-sky-mask.png      the sky opening, 8 bit, linear data
    main2-cloud.png         her painted cloud mass, RGBA, holes healed
    main2-shadow.png        her cast shadow as a strength mask, 8 bit
    main2-leaf.png          one fallen leaf lifted from the road litter

Run with Python/Pillow/NumPy/SciPy, then
TumbangPreso.EditorTools.OwnerMenuEditsAuthor.Prepare for the import settings.
"""
from pathlib import Path
import json
import numpy as np
from scipy import ndimage
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/ui/owner-ui-edits-2026-09-18'
TARGET = ROOT / 'Assets/TumbangPreso/Resources/UI/owner-menu-edits'

# The flat sky behind the whole street. Measured, not chosen: 47.png and 48.png
# agree on it to the byte across the entire opening.
SKY = np.array([136.0, 200.0, 119.0])

# One leaf out of the road litter, so the falling leaves are the same object
# the player can already see lying on the ground. Generous margin on purpose:
# a box cropped tight to the leaf makes the leaf its own median road colour.
LEAF = (1022, 928, 140, 62)


def load(name):
    return np.array(Image.open(SOURCE / name).convert('RGB')).astype(float)


def sky_opening(plate):
    """Soft alpha for the sky, solved rather than thresholded.

    An edge pixel is a blend of the flat sky and whatever leaf or wire is in
    front of it. That foreground colour is unknown per pixel, so it is taken
    from the nearest pixel that is definitely foreground, the same way
    tools/extract_login_v2.py reads the login plates. A hard threshold would
    put a green fringe around every leaf the moment a cloud passed behind it.
    """
    distance = np.abs(plate - SKY).max(axis=2)
    definite = ndimage.binary_opening(distance < 10, structure=np.ones((3, 3)))
    solid = distance > 60
    nearest = ndimage.distance_transform_edt(
        ~solid, return_distances=False, return_indices=True)
    front = plate[nearest[0], nearest[1]]
    direction = SKY - front
    denominator = np.sum(direction * direction, axis=2)
    alpha = np.divide(np.sum((plate - front) * direction, axis=2), denominator,
                      out=np.zeros_like(denominator), where=denominator > 1)
    alpha = np.clip(alpha, 0, 1)
    alpha[definite] = 1
    alpha[solid] = 0
    # ⚠️ The opening is only where the sky can actually be seen. A stray solved
    # pixel down in the road would let a cloud draw on the tarmac.
    alpha[~ndimage.binary_dilation(definite, iterations=3)] = 0
    return alpha


def cut_cloud(lit, plate, opening):
    """Her painted cloud mass, as one drifting sprite with its holes healed.

    ⚠️ THE POLE AND THE PALM MUST NOT TRAVEL WITH THE CLOUD. They are cut out
    of the raw difference because they sit in front of it, so lifting the
    difference as-is gives a sprite with a pole-shaped hole that then slides
    across the sky. The holes are closed and repainted from the surrounding
    cloud, and the plate's own sky mask puts the pole back in front at runtime.
    """
    # ⚠️ ONLY FULLY OPEN SKY COUNTS AS CLOUD. At `opening > .5` the pole's own
    # half-covered rim pixels qualified, so the fill was pinned to a dark blend
    # along the pole's silhouette and the healed hole came back with a crisp
    # outline of the thing it was supposed to erase.
    inside = opening > .97
    body = inside & (np.abs(lit - plate).max(axis=2) > 14)
    labels, _ = ndimage.label(body, structure=np.ones((3, 3)))
    sizes = np.bincount(labels.ravel())
    sizes[0] = 0
    body = labels == sizes.argmax()

    # Bridge the pole and the palm fronds, then close what they enclosed.
    healed = ndimage.binary_closing(body, structure=np.ones((45, 45)))
    healed = ndimage.binary_fill_holes(healed)
    healed &= ndimage.binary_dilation(body, iterations=24)

    ys, xs = np.nonzero(healed)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    crop_lit = lit[y0:y1, x0:x1]
    crop_body = body[y0:y1, x0:x1]
    crop_healed = healed[y0:y1, x0:x1]

    # Repaint the healed holes by diffusion from the surrounding cloud: seed
    # every non-cloud pixel with its nearest cloud colour, then blur and
    # restore the real pixels until the hole relaxes into its neighbours.
    # ⚠️ SEEDING MATTERS AS MUCH AS BLURRING. Starting from the source left a
    # tan ghost of the pole and a green one of the palm that then travelled
    # with the cloud, and it pulled sky green in through the sprite's border.
    nearest = ndimage.distance_transform_edt(
        ~crop_body, return_distances=False, return_indices=True)
    rgb = crop_lit[nearest[0], nearest[1]]
    for _ in range(40):
        rgb = np.dstack([ndimage.gaussian_filter(rgb[..., c], 3) for c in range(3)])
        rgb[crop_body] = crop_lit[crop_body]

    # Alpha from how far the paint is from the flat sky, normalised by the
    # cloud's own body, so her brushed rim survives instead of a cut-out.
    reach = np.abs(crop_lit - SKY).max(axis=2)
    scale = max(np.percentile(reach[crop_body], 70), 1)
    alpha = np.clip(reach / scale, 0, 1)
    alpha = np.where(crop_healed, np.maximum(alpha, .9), 0)

    # ⚠️ The mass is cut off flat by the rooflines, which is invisible while it
    # sits where she painted it and a hard vertical edge the moment it drifts.
    # Fade the sprite's own borders so a travelling end never shows a cut.
    height, width = alpha.shape
    fx = np.clip(np.minimum(np.arange(width), width - 1 - np.arange(width)) / 46.0, 0, 1)
    fy = np.clip(np.minimum(np.arange(height), height - 1 - np.arange(height)) / 26.0, 0, 1)
    alpha *= fx[None, :] * fy[:, None]

    rgba = np.dstack((np.uint8(np.clip(rgb, 0, 255)),
                      np.uint8(np.clip(alpha * 255, 0, 255))))
    Image.fromarray(rgba).save(TARGET / 'main2-cloud.png')
    print(f'main2-cloud.png  {width}x{height} lifted from x{x0} y{y0}')
    return dict(x=int(x0), y=int(y0), width=int(width), height=int(height))


def cut_shadow(lit, plate, opening):
    """Her cast shadow as one strength mask plus the tint it multiplies by.

    ⚠️ THE TINT IS THE PRINCIPAL DIRECTION OF THE DIFFERENCE, NOT THE DARKEST
    PIXEL. Taking the darkest pixels returned (0.93, 0.36, 0.45), which is not
    a shadow at all: the deepest samples all sat on the graffiti outline, where
    the two exports disagree about a hard edge rather than about light. Fitting
    the direction over every shadowed ground pixel gives a cool blue-shifted
    shadow that matches what she painted and what a sky-lit shadow does.
    """
    ground = (opening < .05) & (plate.min(axis=2) > 40)
    difference = 1 - (lit + 1) / (plate + 1)
    magnitude = np.linalg.norm(difference, axis=2)
    sample = ground & (magnitude > .06) & (magnitude < .45)
    _, _, vectors = np.linalg.svd(difference[sample], full_matrices=False)
    axis = vectors[0] / np.linalg.norm(vectors[0])
    if axis.sum() < 0:
        axis = -axis
    projection = difference @ axis
    full = float(np.percentile(projection[sample], 99.5))
    tint = 1 - axis * full
    strength = np.clip(projection / full, 0, 1)

    # ⚠️⚠️ THREE THINGS IN THAT DIFFERENCE ARE NOT SHADOW AND ALL THREE TRAVEL
    # WITH IT IF THEY ARE LEFT IN. The first render of this mask showed the
    # caption as a moving hole in the road and every roofline, the can, the
    # slipper and the whole graffiti outline as bright moving wires.
    #   1  the caption she typed onto 46.png, which is brighter, not darker;
    #   2  every hard edge in the painting, where two exports of one drawing
    #      disagree by a pixel and the ratio is meaningless;
    #   3  the graffiti's own outline, which is the biggest edge in the frame.
    # So the mask is only read where the plate is locally flat, and the rest is
    # filled from the nearest place where it could be.
    luma = plate @ np.array([.299, .587, .114])
    edges = ndimage.gaussian_gradient_magnitude(luma, 1.0) > 6
    unreliable = ndimage.binary_dilation(edges | (projection < -.02), iterations=3)
    reliable = (opening < .05) & ~unreliable
    nearest = ndimage.distance_transform_edt(
        ~reliable, return_distances=False, return_indices=True)
    strength = strength[nearest[0], nearest[1]]
    strength[~(opening < .05)] = 0
    # ⚠️ Re-encoding noise between two exports of one painting sits under 0.06.
    # Without this floor the whole street carries a faint travelling veil.
    strength[strength < .06] = 0
    strength = ndimage.gaussian_filter(strength, 2.0)
    Image.fromarray(np.uint8(np.clip(strength * 255, 0, 255))).save(TARGET / 'main2-shadow.png')
    print(f'main2-shadow.png  tint {tint.round(4)}  covers '
          f'{(strength > .15).mean() * 100:.1f}% of the frame')
    return dict(tint=[round(float(v), 4) for v in tint],
                covered=round(float((strength > .15).mean()), 4))


def cut_leaf(plate):
    x, y, w, h = LEAF
    crop = plate[y:y + h, x:x + w]
    road = np.median(crop.reshape(-1, 3), axis=0)
    alpha = np.clip((np.abs(crop - road).max(axis=2) - 12) / 26, 0, 1)
    labels, _ = ndimage.label(alpha > .4, structure=np.ones((3, 3)))
    sizes = np.bincount(labels.ravel())
    sizes[0] = 0
    keep = ndimage.binary_fill_holes(labels == sizes.argmax())
    alpha = np.where(ndimage.binary_dilation(keep, iterations=1), alpha, 0)
    ys, xs = np.nonzero(alpha > .05)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    rgba = np.dstack((np.uint8(np.clip(crop[y0:y1, x0:x1], 0, 255)),
                      np.uint8(np.clip(alpha[y0:y1, x0:x1] * 255, 0, 255))))
    Image.fromarray(rgba).save(TARGET / 'main2-leaf.png')
    print(f'main2-leaf.png  {rgba.shape[1]}x{rgba.shape[0]}')
    return dict(width=int(rgba.shape[1]), height=int(rgba.shape[0]))


def main():
    TARGET.mkdir(parents=True, exist_ok=True)
    plate = load('48.png')
    lit = load('46.png')
    Image.open(SOURCE / '48.png').convert('RGB').save(TARGET / 'main2-background.png')
    opening = sky_opening(plate)
    Image.fromarray(np.uint8(np.clip(opening * 255, 0, 255))).save(TARGET / 'main2-sky-mask.png')
    print(f'main2-sky-mask.png  opening is {opening.mean() * 100:.1f}% of the frame')
    manifest = dict(
        source='ArtSource/ui/owner-ui-edits-2026-09-18',
        plate='48.png', lit='46.png', sky=[float(v) for v in SKY],
        cloud=cut_cloud(lit, plate, opening),
        shadow=cut_shadow(lit, plate, opening),
        leaf=cut_leaf(plate))
    (SOURCE / 'menu-v3-manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
