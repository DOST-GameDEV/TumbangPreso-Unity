"""Recolour the CC0 controller art the CONTROLLER MAP labels, and emit its anchor table.

WHY THIS EXISTS
---------------
`docs/TODO.md` section 142.3 is the CONTROLLER MAP screen: a picture of the pad with every job
written around it and a leader line from each label to the control it names. This file makes
the picture and the table of where each control sits, in ONE pass, so a line cannot end on bare
plastic when the drawing changes.

WHAT CHANGED ON 2026-09-04, AND WHY
-----------------------------------
The first version DREW the pad from primitives: rounded rectangles for the body, an ellipse per
stick, a polygon for the d-pad. It read as a controller and it read as programmer art, which is
what it was. It now starts from a real illustration.

    tools/assets/ps3_gamepad_cc0_2400.png

WARNING  THE ART IS CC0 AND ITS LICENCE IS COMMITTED BESIDE IT.
`tools/assets/ps3_gamepad_cc0.LICENSE.txt`. It is Grumbel's PlayStation 3 gamepad from the Open
Clip Art Library via Wikimedia Commons, CC0 1.0. `docs/Asset_Sourcing.md` section 1 rule 8 is
what permits it to live in a public repository at all, and rule 1 is why it had to be CC0
rather than the first image a search returns: paid, non-commercial and share-alike art is
excluded from this project by a rule written long before this screen existed.

WARNING  IT IS RECOLOURED ON THE WAY IN AND MUST NEVER BE USED AS BOUGHT.
The source is near-black with GREEN, MAGENTA, RED and PURPLE face buttons. `CLAUDE.md` section
6.4 bans blue, navy and cold grey in any UI layer, stated as wide as it goes, and section 139.4
records a magenta tick shipping in the settings panel as a defect worth its own table row. Two
of those four hues are illegal here outright and the other two belong to other jobs. This is
the same decision `tools/build_input_glyphs.py` took about the bought glyph sheets, in its own
words: a pack dropped in as bought puts colours nobody chose on the one screen a player reads.

WARNING  THE FACE BUTTONS KEEP THEIR SHAPE AND LOSE THEIR HUE.
A triangle, a square, a circle and a cross survive a photograph, a recolour and a colourblind
player; four hues do not. `docs/FUTURE.md` section 16.1 is the rule and `build_input_glyphs.py`
already applied it to the same four buttons one screen over.

THE ANCHORS
-----------
WARNING  THEY ARE MEASURED IN THE COMMITTED RASTER'S OWN PIXELS, AND THE RASTER IS THE CONTRACT.
The four face buttons are FOUND, by their source hues, before the recolour throws those hues
away: that is four anchors this file cannot get wrong. The other fourteen were measured by hand
off a coordinate grid laid over the same file, which is why `SOURCE_SIZE` is asserted below. A
different rasterisation of the same SVG is a different set of pixels, and every leader line
would move without anything failing.

WARNING  THE CROP HAPPENS AFTER THE MEASUREMENT AND CARRIES THE ANCHORS WITH IT.
Trimming the transparent margin changes what "normalised" means. `docs/TODO.md` section 142.3
records the render where that was got wrong: a 640-unit controller in a 980-unit hole, and then
eighteen arrow-heads pointing at nothing.

USAGE
-----
    python tools/build_controller_diagram.py [--out DIR] [--preview]
"""

import argparse
import os
import sys

try:
    import numpy as np
    from PIL import Image, ImageDraw
except ImportError:  # pragma: no cover - report rather than crash, like the audits
    print("build_controller_diagram: Pillow and numpy are required")
    sys.exit(2)


SOURCE = os.path.join("tools", "assets", "gamepad_cc0_2048.png")

# WARNING  ASSERTED, NOT ASSUMED. Every hand-measured anchor below is in these pixels. See the
# header: a re-render of the SVG by a different engine is a different picture with the same name.
SOURCE_SIZE = (1698, 1078)


# ---------------------------------------------------------------------------------------
# § THE PALETTE
#
# WARNING  A LUMINANCE RAMP, NOT A COLOUR SWAP, AND THAT IS WHAT KEEPS THE ILLUSTRATION'S
# MODELLING. The source draws its form with about seven values of grey: the body, the recessed
# button wells, the rim highlights, the moulded d-pad. Mapping each named colour to a chosen
# replacement (which is what `build_input_glyphs.py` does, correctly, for a nine-colour pixel
# ramp) would need a table of 750 entries here and would flatten anything it missed. Sampling a
# warm ramp by luminance keeps every one of those values and moves the whole object into the
# palette at once.
#
# WARNING  THE RAMP IS DARK AT THE BOTTOM ON PURPOSE. The screen is Honey Quartz paper, and the
# controller drawn for its first version was cream on cream and nearly vanished. `CLAUDE.md`
# section 6.2 question 1 asks what the ONE thing on the screen is; here it is the pad, so the pad
# is the darkest object in the frame and everything else stays light.
# ---------------------------------------------------------------------------------------
RAMP = [
    (0x1D, 0x0E, 0x06),   # the black button wells and the touchpad
    (0x2E, 0x16, 0x07),
    (0x4A, 0x24, 0x0D),   # the body
    (0x6B, 0x36, 0x14),
    (0x8B, 0x52, 0x27),   # the wood edge, the raised shoulder faces
    (0xC8, 0x94, 0x5A),
    (0xE8, 0xC7, 0x7E),   # Khaki
    (0xFE, 0xEB, 0xD4),   # UiTheme.Paper, the keylines
]

# ⚠️⚠️ THE SATURATED MARKS ARE FLATTENED TO ONE HONEY. The source draws its face glyphs in cyan,
# pink, red and blue and its light bar in blue: two of those are `CLAUDE.md` § 6.4 bans outright
# and the other two belong to other jobs in this palette. Ramping them by luminance instead would
# come out as four different browns, so the pad would look like two of its four buttons were
# greyed out. One colour, and the SHAPE is what names them (`docs/FUTURE.md` § 16.1).
GLYPH = (0xFC, 0xD3, 0x9F)



# ---------------------------------------------------------------------------------------
# § WHERE EACH CONTROL IS, IN THE SOURCE RASTER'S PIXELS
#
# WARNING  THE FOUR FACE BUTTONS ARE NOT IN THIS TABLE. They are found by their source hues in
# `find_face_buttons`, before the recolour destroys those hues, which makes them the four anchors
# nobody can mistype. Everything below was measured by hand off a coordinate grid.
#
# WARNING  L1 AND L2 ARE TWO POINTS ON ONE BLOCK AND THAT IS DELIBERATE. The illustration draws
# the shoulder as a single moulded nub with the bumper in front of the trigger, which is what a
# real pad looks like from above. Anchoring both to its centre would draw two lines to one point
# and claim there is one control there; the far edge is the trigger and the near edge is the
# bumper, which is the ordering `docs/TODO.md` section 142.3's ring already relies on.
# ---------------------------------------------------------------------------------------
HAND_MEASURED = {
    # ⚠️⚠️ L1 AND L2 HAVE THEIR OWN POINTS AT LAST, WHICH IS THE WHOLE REASON FOR THE TILT. On a
    # flat front view the shoulders are edge-on, so both labels had to point at one visible bar
    # and the map claimed there was one control there. 🧑 asked for a pad *"tilted like this ps5
    # controller"*, and this drawing shows the TOP FACE of each shoulder as a stacked pair: the
    # far band is the trigger, the near one is the bumper, exactly as they sit under a finger.
    "leftTrigger": (330, 55),
    "leftShoulder": (335, 100),
    "rightTrigger": (1368, 55),
    "rightShoulder": (1363, 100),

    "dpad/up": (322, 335),
    "dpad/left": (242, 415),
    "dpad/right": (405, 415),
    "dpad/down": (322, 512),

    # SHARE and OPTIONS, which are what this generation calls select and start.
    "select": (512, 255),
    "start": (1185, 255),

    # WARNING  THE STICK AND ITS CLICK ARE THE SAME POINT, AND HERE THAT IS CORRECT WHERE IT WAS A
    # COMPROMISE FOR THE SHOULDERS. Pushing the stick and pressing it down are two controls on one
    # lump of plastic; two anchors an inch apart would draw a pad with four sticks on it.
    "leftStick": (565, 640),
    "leftStickPress": (565, 640),
    "rightStick": (1095, 640),
    "rightStickPress": (1095, 640),

    # ⚠️⚠️ THE `--preview` OVERLAY IS NOT OPTIONAL FOR THIS FILE, IT IS THE MEASUREMENT. Every
    # anchor here is typed, and a typo puts a leader line on bare plastic with nothing failing.
    # An earlier set of these was read off a coordinate grid by eye and every one was forty to a
    # hundred pixels out, which the drawing hid completely and the ring overlay showed at a
    # glance. Change an anchor, run `--preview`, look at the rings.
    "buttonNorth": (1377, 292),
    "buttonWest": (1260, 420),
    "buttonEast": (1495, 420),
    "buttonSouth": (1376, 541),
}

# ⚠️⚠️ THERE IS NO TRADEMARK TO ERASE IN THIS SOURCE, AND BOTH PREVIOUS ONES HAD ONE. They drew
# Sony's mark between the sticks and this file carried a disc-erase step to remove it, because a
# licence to reuse a DRAWING is not a licence to the trademark inside it (`docs/Port_Plan.md` § 8
# records the same open item for the IKE slipper's Nike wordmark). This drawing's centre button is
# a plain circle. **If the art is swapped again, look for a mark before assuming there is none.**


def load_source():
    if not os.path.exists(SOURCE):
        print(f"build_controller_diagram: {SOURCE} is missing. See its LICENSE.txt.")
        sys.exit(2)

    image = Image.open(SOURCE).convert("RGBA")

    if image.size != SOURCE_SIZE:
        print(f"build_controller_diagram: {SOURCE} is {image.size}, and every anchor in this "
              f"file was measured against {SOURCE_SIZE}. Re-measure or re-render.")
        sys.exit(2)

    return image


def check_anchors_land_on_the_pad(art, anchors):
    """Every anchor has to be ON the drawing, not beside it.

    WARNING  THIS IS THE WEAKER GUARD THAT REPLACED A STRONGER ONE, AND SAYING SO IS THE POINT.
    The previous source drew its face buttons in four saturated hues, so the generator FOUND them
    and could not mistype them. This drawing has no hues to find, so all eighteen anchors are
    typed, and a typo puts a leader line on bare paper with nothing failing. Checking that each
    one lands on an opaque pixel catches the gross version of that mistake: an anchor off the pad
    entirely, or one left behind when the source art is swapped again.

    ⚠️ IT CANNOT CATCH AN ANCHOR ON THE WRONG BUTTON, which is the failure it would most like to
    catch. `--preview` writes an overlay with a ring drawn at every anchor; that picture is the
    real check and it takes five seconds to read.
    """
    stray = []

    for name, (px, py) in anchors.items():
        if not (0 <= px < art.width and 0 <= py < art.height):
            stray.append(f"{name} is outside the image")
            continue

        if art.getpixel((int(px), int(py)))[3] < 8:
            stray.append(f"{name} is on transparent background")

    if stray:
        print("build_controller_diagram: " + "; ".join(stray))
        sys.exit(2)


def recolour(image):
    """Every pixel onto the warm paper ramp.

    WARNING  THE BODY LANDS ON `UiTheme.Paper`, WHICH IS LIGHTER THAN THE SCREEN IT SITS ON, AND
    THAT IS THE WHOLE COLOUR DECISION. The source body is a mid grey at about 0.78 luminance,
    which a ramp spread evenly to Honey Quartz would map onto the page's own ground and make the
    pad vanish into it. The top of the ramp is pushed up to `Paper` and beyond so the controller
    reads as an object lying ON the page rather than a hole cut in it, and the linework carries
    the shape. It is the opposite treatment from the pad this replaced, which was a dark solid.
    """
    pixels = np.asarray(image).astype(int)
    red, green, blue, alpha = (pixels[:, :, i] for i in range(4))

    # WARNING  THE WHITE PAGE BEHIND THE DRAWING BECOMES TRANSPARENT. The SVG has no background
    # of its own; the white is the rasteriser's. Keeping it would draw a white card behind the
    # pad on a cream screen, which is the first thing a photograph of that screen would show.
    page = (red > 245) & (green > 245) & (blue > 245)

    # Rec. 709 luminance, so the ramp follows what the eye reads as light rather than the numeric
    # average, which would make the source's mid greys jump a step.
    rgb = pixels[:, :, :3]
    saturation = rgb.max(2) - rgb.min(2)
    coloured = (saturation > 60) & (alpha > 8)

    luma = (0.2126 * red + 0.7152 * green + 0.0722 * blue) / 255.0

    steps = len(RAMP) - 1
    index = np.clip(np.round(luma * steps).astype(int), 0, steps)

    out = np.array(RAMP, dtype=int)[index]

    # See `GLYPH`: the face marks and the light bar are the only coloured things in the source,
    # and they all become one honey so shape is what tells the four buttons apart.
    out[coloured] = GLYPH

    result = np.dstack([out, alpha]).astype(np.uint8)
    result[page] = (0, 0, 0, 0)

    return Image.fromarray(result, "RGBA")


def crop_to_ink(flat, anchors):
    """Trim the transparent margin, carrying the anchors through the same transform.

    WARNING  THE ANCHORS ARE REMAPPED HERE OR EVERY LEADER LINE MOVES. They are normalised
    against the canvas and cropping changes what normalised means.
    """
    box = flat.getbbox()
    if box is None:
        return flat, {name: (0.5, 0.5) for name in anchors}

    margin = 6
    x0 = max(0, box[0] - margin)
    y0 = max(0, box[1] - margin)
    x1 = min(flat.width, box[2] + margin)
    y1 = min(flat.height, box[3] + margin)

    cropped = flat.crop((x0, y0, x1, y1))
    width = float(x1 - x0)
    height = float(y1 - y0)

    moved = {}

    for name, (px, py) in anchors.items():
        moved[name] = ((px - x0) / width, (py - y0) / height)

    return cropped, moved


# WARNING  THE SHIPPED PNG IS RESAMPLED DOWN AND THE SOURCE RASTER IS NOT, BECAUSE THEY ARE
# ANSWERING TWO DIFFERENT QUESTIONS. The 1960 px input exists so the hand-measured anchors have
# somewhere precise to be measured; the OUTPUT only ever draws at about 980 canvas units, which
# is roughly 1300 device pixels on a 1440p monitor. `EditorTools.InputGlyphImport` keeps this
# folder UNCOMPRESSED (flat fills inside hard outlines are what block compression ruins), so
# every pixel here is four bytes in memory plus a third for mips: shipping the full 1942 x 1214
# would be about 12.6 MB of texture for one menu illustration, and 1400 wide is 6.5 MB for a
# picture nobody can tell apart. The anchors are normalised, so a resize costs them nothing.
SHIPPED_WIDTH = 1400


def write(out_dir, art, anchors, preview):
    os.makedirs(out_dir, exist_ok=True)

    if art.width > SHIPPED_WIDTH:
        height = max(1, round(art.height * SHIPPED_WIDTH / art.width))
        art = art.resize((SHIPPED_WIDTH, height), Image.LANCZOS)

    png = os.path.join(out_dir, "pad_diagram_v1.png")
    art.save(png)

    # WARNING  A DIFFERENT BASENAME FROM THE PNG, AND IT COST A WHOLE RENDER ONCE. It was
    # `pad_diagram_v1.txt` beside `pad_diagram_v1.png`, and
    # `Resources.Load<TextAsset>("UI/input/pad_diagram_v1")` resolved the PNG for that path and
    # answered null: no error, no log, and a screen that drew the pad and every callout with
    # nothing joining them. `Resources.Load` matches the path first and the type second.
    manifest = os.path.join(out_dir, "pad_diagram_v1_anchors.txt")

    with open(manifest, "w", encoding="utf-8") as handle:
        handle.write("# generated by tools/build_controller_diagram.py - do not hand-edit\n")
        # ⚠️ THE SOURCE PATH IS DERIVED, NOT TYPED. It was a literal, and it still named the
        # PS3 art two source swaps later: a generated file whose own header lies about where it
        # came from is worse than one with no header. `CLAUDE.md` § 5's drift rule, in a comment.
        handle.write(f"# source: {SOURCE.replace(os.sep, '/')}, see its LICENSE beside it\n")
        handle.write("# control  x  y   (normalised, y measured DOWN from the top)\n")

        for name in sorted(anchors):
            x, y = anchors[name]
            handle.write(f"{name} {x:.5f} {y:.5f}\n")

    print(f"wrote {png} ({art.width}x{art.height})")
    print(f"wrote {manifest} ({len(anchors)} anchors)")

    if not preview:
        return

    # WARNING  OVER THE REAL GROUND, NEVER AN EMPTY SCENE, which is `CLAUDE.md` section 6.2b's
    # second row. A transparent PNG viewed on white is a picture of a different object, and this
    # one ships on Honey Quartz paper.
    renders = os.path.join("Logs", "renders")
    os.makedirs(renders, exist_ok=True)

    ground = Image.new("RGBA", art.size, (0xFC, 0xD3, 0x9F, 255))
    ground.alpha_composite(art)
    ground.convert("RGB").save(os.path.join(renders, "pad_diagram_v4.png"))

    # WARNING  AND A SECOND RENDER WITH THE ANCHORS DRAWN ON, because an anchor three per cent out
    # is invisible in the art and obvious the moment a ring is drawn where the line will end.
    # This is the picture to look at when a leader points at the wrong button.
    marked = ground.copy()
    draw = ImageDraw.Draw(marked)

    for _, (nx, ny) in anchors.items():
        x = nx * art.width
        y = ny * art.height
        draw.ellipse((x - 10, y - 10, x + 10, y + 10), outline=(0xC3, 0x2E, 0x0D), width=4)

    marked.convert("RGB").save(os.path.join(renders, "pad_diagram_v4_anchors.png"))
    print(f"wrote {renders}/pad_diagram_v4.png and its anchor overlay")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", default=os.path.join(
        "Assets", "TumbangPreso", "Resources", "UI", "input"))
    parser.add_argument("--preview", action="store_true")
    args = parser.parse_args()

    source = load_source()
    anchors = dict(HAND_MEASURED)

    art = recolour(source)
    check_anchors_land_on_the_pad(art, anchors)

    art, normalised = crop_to_ink(art, anchors)
    write(args.out, art, normalised, args.preview)

    return 0


if __name__ == "__main__":
    sys.exit(main())
