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

# The skyline. Everything above this is wall, houses and canopy; the street is
# below it. Read off her own plate: the kerb on the right meets the road at 545
# and the wall's foot on the left at 612, so 560 clears both without eating the
# top of the road.
HORIZON = 560

# Her caption on 46.png, which is the box `HomeCourtView.Caption` draws the live
# line into. x, y, width, height in the plate's own 1920x1080 pixels.
CAPTION = (580, 960, 760, 110)

# Her tree, top right, where the leaves come from. Read for its colour ramp
# only; the leaf's shape stays the one she painted lying in the road.
CANOPY = (1470, 20, 440, 220)


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
    # ⚠⚠ COARSE TO FINE, BECAUSE FORTY PASSES AT ONE SIGMA CANNOT CROSS A PALM
    # FROND. Diffusion at sigma 3 carries colour about three pixels a pass, so
    # the hole left by the pole closed and the ones left by the fronds did not:
    # the finished sprite kept a tan ghost of the post and a green one of the
    # palm, and both TRAVELLED with the cloud, which is the one thing a cloud
    # may not do. Relaxing the wide holes first and refining after closes them
    # for the same total work, and her own pixels are still pinned every pass.
    nearest = ndimage.distance_transform_edt(
        ~crop_body, return_distances=False, return_indices=True)
    rgb = crop_lit[nearest[0], nearest[1]]
    for sigma, passes in ((14, 18), (8, 14), (4, 12), (2, 10)):
        for _ in range(passes):
            rgb = np.dstack([ndimage.gaussian_filter(rgb[..., c], sigma) for c in range(3)])
            rgb[crop_body] = crop_lit[crop_body]

    # Alpha from how far the paint is from the flat sky, normalised by the
    # cloud's own body, so her brushed rim survives instead of a cut-out.
    reach = np.abs(crop_lit - SKY).max(axis=2)
    scale = max(np.percentile(reach[crop_body], 70), 1)
    alpha = np.clip(reach / scale, 0, 1)
    # ⚠⚠ THE HOLES ARE FILLED IN ALPHA TOO, AND FORGETTING THAT IS WHAT MADE THE
    # POLE VISIBLE. The colour under the pole and the fronds was repainted
    # above, but the alpha floor here was `max(alpha, .9)`: a flat ten per cent
    # dip in exactly their shape. At runtime that lets her green sky through in
    # the outline of a telephone pole and a palm frond, and the outline TRAVELS
    # with the cloud. 🧑 on that build: *"the clouds are absolute dog water"*.
    # A normalised convolution carries the surrounding cloud's own alpha across
    # each hole instead, so the mass is solid where she painted it solid and
    # still fades at the brushed rim she drew.
    hole = crop_healed & ~crop_body
    known = (crop_healed & ~hole).astype(np.float64)
    carried = (ndimage.gaussian_filter(alpha * known, 9.0)
               / np.maximum(ndimage.gaussian_filter(known, 9.0), 1e-6))
    alpha = np.where(hole, np.maximum(alpha, carried), alpha)
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
    # Her caption is type she added to 46 and it lies flat on the road, so it
    # passes every reliability test above and comes out as a dark stencil of the
    # words. Calling it unreliable lets the diffusion below carry the road's own
    # dapple straight through it; zeroing it afterwards left a visible rectangle.
    unreliable[CAPTION[1]:CAPTION[1] + CAPTION[3], CAPTION[0]:CAPTION[0] + CAPTION[2]] = True
    reliable = (opening < .05) & ~unreliable

    # ⚠⚠ THE HOLES ARE FILLED BY DIFFUSION, NOT BY THE NEAREST PIXEL, AND THAT
    # DIFFERENCE IS THE WHOLE LOOK OF THE SCREEN. This used to be a
    # `distance_transform_edt`, which gives every unreliable pixel the value of
    # the single CLOSEST reliable one: a Voronoi stamp. `edges` is every
    # gradient over 6 in a PAINTING dilated three pixels, so most of the frame
    # is unreliable, and her soft leaf dapple came back as flat hard-edged slabs
    # tiled across the road. Measured against her own darkening, 46 minus 48:
    # her dapple carries 0.0121 of edge energy on the road and the stamped mask
    # carried 0.0167, which is 38 per cent MORE edge than she painted, while
    # over the whole frame it came back 1.4x SOFTER than hers because of the two
    # pixel blur underneath. Both at once is why it read as mush.
    #
    # A coarse normalised convolution seeds the holes with the weighted AVERAGE
    # of the reliable pixels around them, then a few finer relaxation passes
    # settle the seam. Her own pixels are pinned back after every pass, so
    # nothing she painted is ever averaged with anything.
    weight = reliable.astype(np.float64)
    seed = (ndimage.gaussian_filter(strength * weight, 12.0)
            / np.maximum(ndimage.gaussian_filter(weight, 12.0), 1e-6))
    field = np.where(reliable, strength, seed)
    for sigma in (6.0, 3.0, 1.5):
        for _ in range(8):
            field = ndimage.gaussian_filter(field, sigma)
            field[reliable] = strength[reliable]
    strength = field

    # ⚠⚠ THE DAPPLE IS RESTRICTED TO THE GROUND SHE PAINTED IT ON, AND WITHOUT
    # THIS THE MASK IS A LIGHTING DIFFERENCE RATHER THAN A SHADOW. 46 is not 48
    # plus a cast shadow: it is a separate export, lit slightly differently over
    # the WHOLE frame, with her caption typed into it. So the raw difference
    # reported 48 per cent of the wall and the entire sari-sari block as
    # shadowed, the words "to continue" came out as a dark stencil across the
    # road, and all of it was then multiplied over her painting by a blue tint.
    # 🧑, opening that build: *"WHY IS THIS SHIT SO BLURRY WHAT DID U DOOOO"*.
    # He also said it plainly: **46 was just reference.**
    #
    # ⚠️ SO THE ONE PART OF THAT DIFFERENCE THAT IS REAL IS KEPT AND THE REST IS
    # DROPPED. A cast shadow on the street is on the STREET; the road is one
    # connected warm plane under the horizon and it is found rather than typed,
    # so a re-export that shifts the composition does not silently re-cut it.
    road_colour = np.median(plate[880:1040, 600:1400].reshape(-1, 3), axis=0)
    near_road = np.abs(plate - road_colour).max(axis=2) < 46
    near_road[:HORIZON] = False
    labels, count = ndimage.label(ndimage.binary_closing(near_road, np.ones((9, 9))))
    if count:
        sizes = np.bincount(labels.ravel())
        sizes[0] = 0
        ground_plane = ndimage.binary_fill_holes(labels == sizes.argmax())
    else:
        ground_plane = near_road
    # A hair of feather so the dapple does not stop at a hard line on the kerb.
    ground_plane = ndimage.gaussian_filter(ground_plane.astype(np.float64), 3.0)
    strength *= np.clip(ground_plane * 1.15, 0, 1)

    strength[~(opening < .05)] = 0
    # ⚠️ Re-encoding noise between two exports of one painting sits under 0.06.
    # Without this floor the whole street carries a faint travelling veil.
    strength[strength < .06] = 0
    # ⚠️ HALF A PIXEL, NOT TWO. The 2.0 was covering for the slab edges the stamp
    # produced; with the fill continuous there is nothing left to hide, and two
    # pixels of blur across a 1920 wide mask is what turned her dapple into
    # weather. This only takes the aliasing off the reliable-to-filled boundary.
    strength = ndimage.gaussian_filter(strength, 0.5)
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
    blade = crop[y0:y1, x0:x1]
    mask = alpha[y0:y1, x0:x1] > .5

    # ⚠⚠ THE SHAPE IS HER FALLEN LEAF AND THE COLOUR IS HER TREE, BECAUSE A LEAF
    # FALLS OUT OF THE TREE IT CAME FROM. Cut raw, this blade is her road
    # litter: measured (174, 84, 39) against the litter beside it at
    # (162, 75, 38), so the cut is exact. But the canopy it falls out of is
    # (126, 132, 77), and five burnt-orange leaves tumbling out of a green tree
    # is the first thing anybody notices. 🧑: *"the leaves arent even the same
    # color"*.
    #
    # ⚠️ BOTH ENDS OF THIS ARE HERS AND NOTHING IS INVENTED. The silhouette and
    # its shading are the leaf she painted; the palette is the ramp measured off
    # HER canopy, dark to light. Her own light-to-dark modelling on the blade is
    # what indexes into it, so the leaf keeps the form she drew and wears the
    # colour of the tree above it. This is not `CLAUDE.md` § 6.0's ban on
    # repainting sourced art: that is about an authored asset shipping as
    # delivered, and her plate does, byte for byte. This is one derived sprite
    # being cut from the right part of the same painting.
    canopy = plate[CANOPY[1]:CANOPY[1] + CANOPY[3], CANOPY[0]:CANOPY[0] + CANOPY[2]]
    foliage = canopy.reshape(-1, 3)
    foliage = foliage[np.abs(foliage - SKY).max(axis=1) > 30]
    # ⚠️ A RAMP, NOT A SORTED PILE OF PIXELS. Sorting her canopy by luma and
    # indexing straight into it picks a different hue for every neighbouring
    # value, because the tree carries red-brown branch strokes at the same
    # brightness as its leaves: the first leaf out of that came back speckled
    # with orange. Binning by luma and taking the MEDIAN hue in each bin throws
    # the branch strokes out, and smoothing across the bins leaves one
    # continuous dark-to-light green.
    foliage_luma = foliage @ np.array([.299, .587, .114])
    bins = np.linspace(foliage_luma.min(), foliage_luma.max(), 24)
    ramp = []
    for lo, hi in zip(bins[:-1], bins[1:]):
        band = foliage[(foliage_luma >= lo) & (foliage_luma <= hi)]
        if len(band):
            ramp.append(np.median(band, axis=0))
    ramp = np.array(ramp) if ramp else foliage[:1]
    if len(ramp) > 4:
        ramp = np.stack([ndimage.uniform_filter1d(ramp[:, c], 5, mode='nearest')
                         for c in range(3)], axis=1)

    luma = blade @ np.array([.299, .587, .114])
    if mask.any():
        low, high = np.percentile(luma[mask], (4, 96))
    else:
        low, high = luma.min(), luma.max()
    index = np.clip((luma - low) / max(high - low, 1e-6), 0, 1)
    picked = ramp[np.clip((index * (len(ramp) - 1)).astype(int), 0, len(ramp) - 1)]

    rgba = np.dstack((np.uint8(np.clip(picked, 0, 255)),
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
