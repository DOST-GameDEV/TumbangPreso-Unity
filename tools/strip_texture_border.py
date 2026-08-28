"""Remove the white matte border baked into a label texture.

  python tools/strip_texture_border.py Assets/.../lata_metal.png [more.png ...]

⚠️⚠️ THIS IS WHY THE RUSTY CAN HAD A WHITE LINE DOWN ITS SIDE. 🧑 2026-08-28:
*"theres like a white line in between the rusty can and i think its bcz theres a white
line in the picture for it"*. He was right. `lata_metal.png` and `lata_pasip.png` were
exported with a pure-white matte one pixel wide on the left and right and several rows
deep on the top and bottom. The label wraps the can cylindrically, so column 0 and
column 1023 land next to each other on the mesh and the two white columns read as one
bright seam running the full height of the can. It is not a shader or a UV fault and
looking for one costs an afternoon.

⚠️ THE FIX IS A CLAMP, NOT A CROP. Rescaling the image to drop the border would shift
every UV on the can by a fraction of a texel and smear the label; copying the first
real line of pixels outward leaves every interior texel exactly where the UVs expect
it and only overwrites the matte.

⚠️ IT ONLY TOUCHES A RUN THAT STARTS AT AN EDGE AND IS BOTH COLOURLESS AND WELL ABOVE
THE IMAGE MEDIAN, so a texture that is legitimately pale overall (`lata_decades`, median
163) is left alone. Running it on a clean texture is a no-op and it says so.
"""

import statistics
import sys
from pathlib import Path

from PIL import Image


# ⚠️ A MATTE IS RECOGNISED BY BEING COLOURLESS, NOT BY BEING PURE WHITE. The first
# version of this tested luma >= 240 and left `lata_metal` with a cream band still on
# its bottom edge, because the export mattes to a slightly-off white (luma 219 to 237)
# and only the extreme rows reach 240. Every matte line measured is also flat grey
# (saturation 0.00 to 0.09) while the rust label itself runs 0.25 to 0.56, so
# saturation separates the two cleanly where brightness alone does not.
FLAT = 0.15
OVER_MEDIAN = 60.0

# A matte wider than this is not a matte, it is artwork. Refuse rather than eat it.
MAX_BORDER = 24


def luma(pixel):
    return 0.2126 * pixel[0] + 0.7152 * pixel[1] + 0.0722 * pixel[2]


def saturation(pixel):
    return (max(pixel) - min(pixel)) / max(max(pixel), 1)


def line_stats(pixels, size, index, vertical):
    """Mean luma and mean saturation of one row or column."""
    width, height = size
    span = range(height) if vertical else range(width)
    if vertical:
        samples = [pixels[index, i] for i in span]
    else:
        samples = [pixels[i, index] for i in span]
    n = len(samples)
    return (sum(luma(p) for p in samples) / n,
            sum(saturation(p) for p in samples) / n)


def line_luma(pixels, size, index, vertical):
    return line_stats(pixels, size, index, vertical)[0]


def border_run(pixels, size, indices, vertical, floor):
    """How many lines from this edge inward are matte."""
    run = 0
    for i in indices:
        value, colour = line_stats(pixels, size, i, vertical)
        if value >= floor and colour <= FLAT:
            run += 1
        else:
            break
    return run


def strip(path):
    image = Image.open(path)
    mode = image.mode
    rgb = image.convert("RGB")
    width, height = rgb.size
    pixels = rgb.load()

    median = statistics.median(
        [line_luma(pixels, rgb.size, x, True) for x in range(width)]
    )
    floor = median + OVER_MEDIAN

    left = border_run(pixels, rgb.size, range(width), True, floor)
    right = border_run(pixels, rgb.size, range(width - 1, -1, -1), True, floor)
    top = border_run(pixels, rgb.size, range(height), False, floor)
    bottom = border_run(pixels, rgb.size, range(height - 1, -1, -1), False, floor)

    if not (left or right or top or bottom):
        print(f"{path.name}: no matte border, left alone")
        return False

    for name, run in (("left", left), ("right", right), ("top", top), ("bottom", bottom)):
        if run > MAX_BORDER:
            raise SystemExit(
                f"{path.name}: {name} border measures {run} px, over the {MAX_BORDER} px "
                f"cap. That is artwork, not a matte. Check the image before rerunning."
            )

    work = image.convert("RGBA") if mode in ("RGBA", "LA", "P") else rgb
    out = work.load()

    for x in range(left):
        for y in range(height):
            out[x, y] = out[left, y]
    for x in range(width - right, width):
        for y in range(height):
            out[x, y] = out[width - right - 1, y]
    for y in range(top):
        for x in range(width):
            out[x, y] = out[x, top]
    for y in range(height - bottom, height):
        for x in range(width):
            out[x, y] = out[x, height - bottom - 1]

    work.convert(mode if mode != "P" else "RGB").save(path)
    print(f"{path.name}: clamped left {left}, right {right}, top {top}, bottom {bottom}")
    return True


def main():
    targets = [Path(p) for p in sys.argv[1:]]
    if not targets:
        raise SystemExit(__doc__)
    changed = sum(1 for p in targets if strip(p))
    print(f"[border] rewrote {changed} of {len(targets)}")


if __name__ == "__main__":
    main()
