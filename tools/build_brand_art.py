"""Turns the brand JPEGs into UI art, and recolours the wordmark per screen.

    python tools/build_brand_art.py

WHY THIS EXISTS RATHER THAN THE JPEGS BEING USED DIRECTLY. They arrive as JPEG on a WHITE PAGE,
and both halves of that are fatal in a UI:

  * A logo with an opaque white rectangle behind it is a white rectangle. Every screen this art
    goes on is Honey Quartz or Army, so the page has to become alpha.
  * JPEG puts ringing around every hard edge, and the wordmark is nothing but hard edges. Keying
    on a single white threshold would leave a grey halo one to three pixels wide, which reads as
    a bad cut-out at any size.

WHY THE MONO MASTER IS THE INTERESTING ONE. 'new tump text.jpg' is the wordmark as flat black
line on white, which makes it a RECOLOURABLE master rather than a picture. That is the mechanism
CLAUDE.md 6.5 already names in his own art: "JOIN BUTTON.png is BUTTON LONG.png with one colour
swapped, keyline to floor, so one base colour generates a whole control". Same idea, one level
up: one wordmark drawing, tinted per screen out of the palette, so the login screen's is not the
lobby's and neither of them is a second drawing anybody has to maintain.

He asked for exactly this on 2026-09-03: "u can edit those assets and change the colors or smth,
depending on which screen u will use".

THE COLOURS ARE READ, NOT TYPED. Every hex below came out of tools/read_brand_palette.py run
against the two colour files, and it agreed with itself across both: the wordmark and the
tsinelas mark are drawn from one palette. docs/TODO.md 133.1 forbids doing this by eye.
"""

import os
import sys

try:
    from PIL import Image
except ImportError:
    sys.exit("Pillow is needed: python -m pip install pillow")

SRC = "Assets/TumbangPreso/Art/ui/brand/source"
OUT = "Assets/TumbangPreso/Art/ui/brand"
MIRROR = "Assets/TumbangPreso/Resources/UI/brand"

# ---------------------------------------------------------------------------------------------
# The palette, measured. See the module note.
# ---------------------------------------------------------------------------------------------
DEEP_RED   = (0x98, 0x07, 0x15)   # 34.3% of the logo: the outline every shape is held in
HONEY      = (0xFC, 0xD3, 0x9F)   # 23.1%: the letter fill
CHARTREUSE = (0xD6, 0xCE, 0x01)   # 17.0%: the blob behind the wordmark
PERSIMMON  = (0xFD, 0x80, 0x41)   #  5.7%: the diagonal fill on the 1
GOLDEN     = (0xF5, 0xB5, 0x21)   #  4.2%: the drip's swirl
RIM_RED    = (0xC3, 0x2E, 0x0D)   #  3.8%: the brighter rim marks under the letters
ARMY       = (0xB3, 0xA8, 0x28)   #  1.4%: the shading strokes on the blob

# White page, and the grey texture hatching inside the mono master.
PAGE = 236          # at or above this on all three channels is page
INK = 110           # at or below this on all three channels is line


def key_page(img, feather=True):
    """White page to alpha, with the JPEG halo taken with it.

    The alpha is derived from LUMINANCE rather than from a hard threshold, so a pixel that is
    80 per cent page becomes 20 per cent opaque instead of being kept whole or thrown away
    whole. That is what removes the ringing without eating the line: a hard key at any single
    value leaves either a grey fringe or a chewed edge, and this art is all edge.
    """
    img = img.convert("RGB")
    w, h = img.size
    out = Image.new("RGBA", (w, h))
    px = img.load()
    op = out.load()

    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            lum = (r * 299 + g * 587 + b * 114) // 1000

            if lum >= PAGE:
                op[x, y] = (r, g, b, 0)
                continue

            if not feather:
                op[x, y] = (r, g, b, 255)
                continue

            # Fully opaque well below the page, ramping over the last stretch.
            a = 255 if lum < PAGE - 40 else int(255 * (PAGE - lum) / 40.0)
            op[x, y] = (r, g, b, max(0, min(255, a)))

    return out


def recolour_mono(img, line, fill, texture=None):
    """Paints the mono wordmark: black line becomes `line`, white counter becomes `fill`.

    The master is three tones and not two, which is why this takes a third argument: the
    hatching inside the 1 and the swirl in the drip are drawn in a light GREY, deliberately, so
    they can be tinted separately from the counter they sit in. Passing texture=None leaves them
    as a tint of the fill, which is the quiet option.
    """
    img = img.convert("RGB")
    w, h = img.size
    out = Image.new("RGBA", (w, h))
    px = img.load()
    op = out.load()

    tex = texture if texture is not None else fill

    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            lum = (r * 299 + g * 587 + b * 114) // 1000

            if lum <= INK:
                # The line. Solid, and it is the thing that makes this drawing his.
                op[x, y] = (*line, 255)
            elif lum >= PAGE:
                # Outside the drawing.
                op[x, y] = (*fill, 0)
            elif lum >= 190:
                # The grey texture hatching.
                op[x, y] = (*tex, 255)
            else:
                # The anti-aliased band between line and counter. Blend so the edge stays soft.
                t = (lum - INK) / float(PAGE - INK)
                op[x, y] = (
                    int(line[0] + (fill[0] - line[0]) * t),
                    int(line[1] + (fill[1] - line[1]) * t),
                    int(line[2] + (fill[2] - line[2]) * t),
                    255,
                )

    return out


def trim(img):
    """Crops to the drawing.

    CLAUDE.md 6.2c: "Is this image fitted to the region it is SEEN in, or to the whole screen?"
    A 2048 square that is two thirds empty page makes every caller's fit arithmetic wrong, and
    100 records what that cost the last time: the key art was enveloped to the full canvas, a
    column then covered a third of it, and the cast came out off-centre with its heads cut off.
    Trimming here means a caller sizing against the RECT is sizing against the DRAWING.
    """
    box = img.getbbox()
    return img.crop(box) if box else img


def save(img, name):
    for folder in (OUT, MIRROR):
        os.makedirs(folder, exist_ok=True)
        path = os.path.join(folder, name)
        img.save(path)
    print(f"   {name:34s} {img.width:5d} x {img.height:<5d}")


def main():
    if not os.path.isdir(SRC):
        sys.exit(f"no {SRC}. The artist's files go there and are committed unchanged.")

    print("brand art, from", SRC)

    colour = trim(key_page(Image.open(f"{SRC}/tump_logo_colour.jpg")))
    save(colour, "tump_logo.png")

    mark = trim(key_page(Image.open(f"{SRC}/tsinelas_hit.jpg")))
    save(mark, "tsinelas_hit.png")

    mono = Image.open(f"{SRC}/tump_wordmark_line_textured.jpg")

    # ⚠️ ONE DRAWING, FOUR SCREENS, AND THE PAIRINGS ARE THE PALETTE'S ROLES RATHER THAN TASTE.
    # docs/Front_End_Design.md section 4: deep red is the OUTLINE everywhere, and what changes
    # between screens is the ground it is holding. Each of these is the wordmark as that screen
    # would draw it, so the login screen's hero and the lobby's small mark are the same object
    # in two costumes rather than two files that can drift apart.
    # !! A RECOLOUR ONLY WORKS ON A GROUND THAT IS FAR FROM ITS FILL, and the first render of
    # the login screen is the receipt. The master is a SINGLE-FILL drawing: the letters, the blob
    # behind them and the drip are all one white counter, so every recolour paints the three the
    # same colour and they collapse into one silhouette. tump_wordmark_login.png is deep red on
    # Honey Quartz and the login column IS Honey Quartz, so on SignInBoot-v77.png only the
    # outline read and the game's name arrived as an empty wire frame.
    #
    # So: use a variant where the ground contrasts (the stage one is honey on Army and reads),
    # and use tump_logo.png, the colour master, anywhere the logo is the hero on a light ground.
    # That is also the more faithful answer per VISION.md section 6.
    #
    # A per-region recolour would need the master separated into layers the way SkinLayers
    # separates a control. That is a request to the artist, not something this script can infer
    # from one flat fill.
    variants = [
        ("tump_wordmark_login.png",  DEEP_RED, HONEY,      PERSIMMON),
        ("tump_wordmark_lobby.png",  DEEP_RED, CHARTREUSE, GOLDEN),
        ("tump_wordmark_stage.png",  HONEY,    ARMY,       PERSIMMON),
        ("tump_wordmark_ink.png",    DEEP_RED, HONEY,      None),
    ]

    for name, line, fill, tex in variants:
        save(trim(recolour_mono(mono, line, fill, tex)), name)

    print("\nEvery file above is generated. The artist's originals are in source/ and are")
    print("committed unchanged; nothing here edits them. Re-run this after he sends a new")
    print("version of any master.")


if __name__ == "__main__":
    main()
