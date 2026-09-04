"""Draw the controller line art the CONTROLLER MAP screen labels, and its anchor table.

WHY THIS EXISTS
---------------
`docs/TODO.md` section 138 is the write-up of how a pad reaches this game, and section 139
records 🧑 asking for the controller half of settings to be *"prettier"* and rebindable. Both
were answered with LISTS: a column of action names against a column of control names. A list
is the wrong shape for this one question, because the question a player actually asks is
never "what is LUNGE bound to", it is **"what does this button under my thumb do"**, and a
list cannot be read that way round without knowing the answer first.

So the map is a PICTURE of the pad with the jobs written around it, which is the shape 🧑
handed over as the reference: a line drawing in the middle, callouts ringing it, a leader
line from each callout to the control it names.

WHAT THIS FILE OWNS, AND WHAT IT DELIBERATELY DOES NOT
------------------------------------------------------
It owns the DRAWING and the ANCHORS, and it emits both from one pass so they cannot drift.

    Assets/TumbangPreso/Resources/UI/input/pad_diagram_v1.png
    Assets/TumbangPreso/Resources/UI/input/pad_diagram_v1.txt

WARNING  THE ANCHORS ARE EMITTED RATHER THAN COPIED, AND THAT IS THE WHOLE REASON THE TABLE
IS A FILE INSTEAD OF A C# CONSTANT BLOCK. A leader line has to end ON the control it names.
If the picture is drawn here and the arrow-heads are typed into `ControllerMapScreen.cs`, then
moving the d-pad two hundred pixels left is a change to one file that silently makes four
leader lines in another file point at bare plastic, and nothing in the repository can notice.
This is the same fault `Settings.Rebinding`'s class note records for its own two tables:
*"a stale row in either table is not cosmetic ... a missing action silently produces a dead
row instead, which is worse, because nobody notices."* One generator, one pass, two outputs.

WARNING  IT DOES NOT DECIDE WHAT ANY BUTTON DOES. The bindings are `InputCatalogue` and
`ScreenInputCatalogue`, they are read live through `Settings.Rebinding`, and this script has
never heard of a verb. A picture that hard-coded "THROW" beside the right trigger would be a
screen that teaches the wrong control the moment somebody rebinds one, which
`docs/VISION.md` section 3 calls worse than teaching none.

THE PALETTE
-----------
WARNING  INK AND HONEY QUARTZ, AND NOT ONE COLD PIXEL. `CLAUDE.md` section 6.4, stated as
wide as it goes: *"no blue, no navy, no cold grey, in any UI colour, in any layer"*, and the
reference drawing 🧑 handed over is a black-on-white photocopy whose face buttons are the
PlayStation cyan, pink and blue. Those are three separate bans in one picture. The line art
is `#55290F`, which `scratchpad/fontsrc/ramp.py` measured at 10.5:1 on the paper ground, and
the face buttons are told apart by their SHAPE, which is how the real pad tells them apart
too and is `docs/FUTURE.md` section 16.1's rule: a distinction carried by hue alone is a
distinction some players do not have.

WARNING  THE SILHOUETTE IS ONE OUTLINE, NOT A PILE OF STROKED SHAPES, AND THAT IS WHY THE
BODY IS BUILT AS A MASK. Drawing the grips and the body as three stroked rounded rectangles
leaves the seams where they overlap visible as lines THROUGH the pad, which reads as three
objects rather than one. The union is rasterised first and the outline is taken as the
difference between the mask and its own erosion, so the body has exactly one edge.

USAGE
-----
    python tools/build_controller_diagram.py [--out DIR] [--preview]

`--preview` also writes a versioned review render under `Logs/renders/`, which is
`CLAUDE.md` section 6.1's rule: every model iteration gets a picture and every picture gets a
new filename.
"""

import argparse
import os
import sys

try:
    import numpy as np
    from PIL import Image, ImageDraw, ImageFilter
except ImportError:  # pragma: no cover - report rather than crash, like the audits
    print("build_controller_diagram: Pillow and numpy are required")
    sys.exit(2)


# ---------------------------------------------------------------------------------------
# The canvas.
#
# WARNING  4x SUPERSAMPLED, BECAUSE EVERY LINE IN THIS DRAWING IS A CURVE. Pillow has no
# antialiased stroking: an ellipse outline at final size comes out as a staircase, and a
# staircase beside Darumadrop's smooth lettering reads as a placeholder rather than as art.
# Drawing at 4x and reducing with LANCZOS is the cheapest fix and costs one second.
# ---------------------------------------------------------------------------------------
WIDTH = 1024
HEIGHT = 620
SS = 4

INK = (0x55, 0x29, 0x0F, 255)
FILL = (0xFE, 0xEB, 0xD4, 255)        # UiTheme.Paper, so the body reads as a solid object
SUNK = (0xDE, 0xBA, 0x8C, 255)        # UiTheme.PaperSunk, for the recessed touchpad

STROKE = 5                            # final pixels; multiplied by SS while drawing


def px(v):
    """A final-resolution number in supersampled space."""
    return int(round(v * SS))


class Pad:
    """The drawing, and the anchor each control's leader line ends on."""

    def __init__(self):
        self.image = Image.new("RGBA", (WIDTH * SS, HEIGHT * SS), (0, 0, 0, 0))
        self.draw = ImageDraw.Draw(self.image)
        self.anchors = {}

    def anchor(self, name, x, y):
        """Record a control's centre in NORMALISED coordinates.

        WARNING  NORMALISED, NOT PIXELS, AND FOR `TouchMetrics`' OWN REASON. The screen
        draws this picture into whatever rectangle the aspect-safe canvas leaves it, which is
        never 1024 units wide; a pixel anchor would be correct at exactly one window size,
        which is `CLAUDE.md` section 6.2c's first question about every rect in this game.
        Y is measured DOWN from the top, which is the picture's own direction; the C# flips
        it once, at the one place it turns an anchor into a `RectTransform` position.
        """
        self.anchors[name] = (x / float(WIDTH), y / float(HEIGHT))


def rounded(mask_draw, box, radius):
    mask_draw.rounded_rectangle(box, radius=radius, fill=255)


def build_body_mask():
    """The union of body and grips, as a single-channel mask at supersampled size.

    WARNING  THE GRIPS ARE ROTATED RECTANGLES PASTED IN, NOT POLYGONS TYPED OUT BY HAND. A
    hand-written eight-point polygon per grip is eight numbers that have to stay mirror images
    of eight others, and the first version of this drawing had a left grip four pixels longer
    than the right one. Rotating one shape twice cannot be asymmetric.

    WARNING  AND THE RESULT IS THRESHOLDED BACK TO BLACK AND WHITE, WHICH IS NOT TIDINESS.
    `Image.rotate` with a bicubic filter returns a mask with a soft edge, and `outline_of`
    takes its outline with `MinFilter`, which reads any non-zero pixel as inside. A soft edge
    therefore produces a SECOND faint outline a few pixels out from the real one, and the
    first render of this file had exactly that: two grey arcs across the top of the pad that
    looked like seams where three shapes had been welded together.
    """
    mask = Image.new("L", (WIDTH * SS, HEIGHT * SS), 0)
    md = ImageDraw.Draw(mask)

    # The central slab the sticks, the d-pad and the face buttons all sit on.
    rounded(md, (px(250), px(150), px(774), px(410)), px(62))

    # The shoulder shelf: a shallower block behind the top edge, so L1 and R1 have somewhere
    # to sit that is part of the same object rather than floating beside it.
    rounded(md, (px(268), px(132), px(756), px(240)), px(48))

    # WARNING  THE CHIN IS AN ELLIPSE AND IT IS WHAT STOPS A STRAIGHT LINE RUNNING BETWEEN THE
    # TWO STICKS. The slab's bottom edge is horizontal, the sticks sit on it, and the first
    # render of this geometry drew that edge straight through the gap between them: a ruled
    # line across the belly of a shape whose every other edge is a curve, which reads as a
    # mistake rather than as a pad. The ellipse pushes the silhouette below the sticks and
    # meets both grips, so the whole lower edge is one continuous curve.
    md.ellipse((px(252), px(170), px(772), px(430)), fill=255)

    for sign in (-1, 1):
        grip = Image.new("L", (px(158), px(272)), 0)
        gd = ImageDraw.Draw(grip)
        gd.rounded_rectangle((0, 0, px(158) - 1, px(272) - 1), radius=px(74), fill=255)
        grip = grip.rotate(sign * 17.0, resample=Image.BICUBIC, expand=True)

        cx = px(512) + sign * px(188)
        mask.paste(grip, (cx - grip.width // 2, px(248)), grip)

    # See the second warning above: anything the rotation left grey becomes solid or nothing.
    return mask.point(lambda v: 255 if v > 127 else 0)


def outline_of(mask, weight):
    """The edge of a mask, as a mask: the shape minus its own erosion.

    WARNING  `MinFilter` IS THE EROSION AND ITS SIZE MUST BE ODD. An even size silently
    shifts the result half a pixel in each axis, which puts the outline off-centre on two
    sides of the pad and nowhere else, and it looks exactly like a badly drawn shape.
    """
    size = weight if weight % 2 == 1 else weight + 1
    inner = mask.filter(ImageFilter.MinFilter(size))
    return Image.fromarray(np.maximum(
        np.asarray(mask, dtype=np.int16) - np.asarray(inner, dtype=np.int16), 0
    ).astype(np.uint8))


def ring(pad, box, weight=STROKE):
    pad.draw.ellipse(box, outline=INK, width=px(weight))


def build():
    pad = Pad()
    d = pad.draw

    body = build_body_mask()

    # The face first, then its edge on top, so the stroke is never half-covered by the fill.
    pad.image.paste(Image.new("RGBA", pad.image.size, FILL), (0, 0), body)
    pad.image.paste(Image.new("RGBA", pad.image.size, INK), (0, 0),
                    outline_of(body, px(STROKE)))

    # ---- the shoulders and triggers ---------------------------------------------------
    #
    # WARNING  THE TRIGGERS ARE DRAWN ABOVE THE BUMPERS AND BOTH OVERLAP THE SHELF, WHICH IS
    # THE ONE CUE THAT SAYS WHICH IS WHICH. On every pad ever made L2 is the far one and L1 is
    # the near one, and a diagram that draws them side by side makes the player guess. The
    # overlap matters too: the first render had all four floating clear of the body with a gap
    # of bare paper between, and four detached tabs read as four separate objects.
    for sign in (-1, 1):
        cx = 512 + sign * 210

        # L2 / R2: a tab peeking over the top edge.
        d.rounded_rectangle((px(cx - 58), px(74), px(cx + 58), px(146)),
                            radius=px(24), fill=FILL, outline=INK, width=px(STROKE))
        pad.anchor("leftTrigger" if sign < 0 else "rightTrigger", cx, 100)

        # L1 / R1: a slimmer bar on the shelf itself.
        d.rounded_rectangle((px(cx - 68), px(136), px(cx + 68), px(182)),
                            radius=px(20), fill=FILL, outline=INK, width=px(STROKE))
        pad.anchor("leftShoulder" if sign < 0 else "rightShoulder", cx, 159)

    # ---- the touchpad ------------------------------------------------------------------
    #
    # The reference pad has one and it is what makes the silhouette readable as a controller
    # rather than as a bone. It carries no binding, so it gets no anchor.
    d.rounded_rectangle((px(446), px(196), px(578), px(300)),
                        radius=px(14), fill=SUNK, outline=INK, width=px(STROKE))

    # ---- start and select ---------------------------------------------------------------
    #
    # Flanking the touchpad, which is where the reference drawing puts them, and clear of both
    # thumb clusters: SELECT sits between the d-pad and the pad, START between the pad and the
    # face buttons.
    for sign, name in ((-1, "select"), (1, "start")):
        cx = 512 + sign * 96
        d.rounded_rectangle((px(cx - 13), px(196), px(cx + 13), px(240)),
                            radius=px(11), fill=FILL, outline=INK, width=px(STROKE))
        pad.anchor(name, cx, 218)

    # ---- the d-pad -----------------------------------------------------------------------
    #
    # WARNING  FOUR SEPARATE ANCHORS, NOT ONE. Four different actions bind to the four
    # directions (`InputCatalogue`: EMOTE up, and `ScreenInputCatalogue`: HIDE HUD down, the
    # two pektus curves left and right), and one anchor on the middle of the cross would draw
    # four leader lines converging on a point that means "the d-pad generally", which teaches
    # a player that all four are the same button. `UI.InputGlyphs` records the same decision
    # for the same reason on the glyph sheet.
    dx, dy, arm, half = 338, 282, 46, 22
    d.polygon([
        (px(dx - half), px(dy - arm)), (px(dx + half), px(dy - arm)),
        (px(dx + half), px(dy - half)), (px(dx + arm), px(dy - half)),
        (px(dx + arm), px(dy + half)), (px(dx + half), px(dy + half)),
        (px(dx + half), px(dy + arm)), (px(dx - half), px(dy + arm)),
        (px(dx - half), px(dy + half)), (px(dx - arm), px(dy + half)),
        (px(dx - arm), px(dy - half)), (px(dx - half), px(dy - half)),
    ], fill=FILL, outline=INK, width=px(STROKE))

    pad.anchor("dpad/up", dx, dy - arm + 13)
    pad.anchor("dpad/down", dx, dy + arm - 13)
    pad.anchor("dpad/left", dx - arm + 13, dy)
    pad.anchor("dpad/right", dx + arm - 13, dy)

    # ---- the face buttons -----------------------------------------------------------------
    #
    # WARNING  THE SHAPE IS THE NAME, WHICH IS WHY THEY ARE NOT FOUR IDENTICAL CIRCLES WITH
    # LETTERS IN. The reference drawing tells them apart by hue and two of those hues are
    # banned here (section 6.4); a triangle, a square, a circle and a cross survive both the
    # ban and a colourblind player. The four positions are the compass names the Input System
    # uses, so `buttonNorth` really is the top one on every pad it matches.
    fx, fy, spread, r = 676, 282, 50, 23

    ring(pad, (px(fx - r), px(fy - spread - r), px(fx + r), px(fy - spread + r)))
    d.polygon([(px(fx), px(fy - spread - 12)),
               (px(fx + 12), px(fy - spread + 9)),
               (px(fx - 12), px(fy - spread + 9))], outline=INK, width=px(3))
    pad.anchor("buttonNorth", fx, fy - spread)

    ring(pad, (px(fx - spread - r), px(fy - r), px(fx - spread + r), px(fy + r)))
    d.rectangle((px(fx - spread - 10), px(fy - 10), px(fx - spread + 10), px(fy + 10)),
                outline=INK, width=px(3))
    pad.anchor("buttonWest", fx - spread, fy)

    ring(pad, (px(fx + spread - r), px(fy - r), px(fx + spread + r), px(fy + r)))
    ring(pad, (px(fx + spread - 10), px(fy - 10), px(fx + spread + 10), px(fy + 10)), weight=3)
    pad.anchor("buttonEast", fx + spread, fy)

    ring(pad, (px(fx - r), px(fy + spread - r), px(fx + r), px(fy + spread + r)))
    d.line([(px(fx - 10), px(fy + spread - 10)), (px(fx + 10), px(fy + spread + 10))],
           fill=INK, width=px(3))
    d.line([(px(fx + 10), px(fy + spread - 10)), (px(fx - 10), px(fy + spread + 10))],
           fill=INK, width=px(3))
    pad.anchor("buttonSouth", fx, fy + spread)

    # ---- the sticks ------------------------------------------------------------------------
    #
    # WARNING  ONE STICK CARRIES TWO ANCHORS AND THEY ARE THE SAME POINT ON PURPOSE. Pushing
    # the stick and CLICKING it are two controls (`<Gamepad>/leftStick` moves, and
    # `<Gamepad>/leftStickPress` is SPRINT), and they are the same lump of plastic. Two
    # anchors a centimetre apart would be a drawing that claims there are two objects there.
    for sign, stick, press in ((-1, "leftStick", "leftStickPress"),
                               (1, "rightStick", "rightStickPress")):
        sx, sy = 512 + sign * 104, 378

        ring(pad, (px(sx - 48), px(sy - 48), px(sx + 48), px(sy + 48)))
        d.ellipse((px(sx - 33), px(sy - 33), px(sx + 33), px(sy + 33)),
                  fill=FILL, outline=INK, width=px(STROKE))

        pad.anchor(stick, sx, sy)
        pad.anchor(press, sx, sy)

    return pad


def crop_to_ink(flat, anchors):
    """Trim the transparent margin, and carry the anchors through the same transform.

    WARNING  THE UNCROPPED PNG SHRANK INSIDE ITS OWN BOX ON THE SCREEN, AND THE FIRST RENDER OF
    `ControllerMapScreen` IS THE RECEIPT. The drawing occupies about 65 per cent of the 1024 px
    canvas and the rest is transparent margin, so a `UnityEngine.UI.Image` with `preserveAspect`
    fitted the WHOLE canvas into the 980-unit rect and the pad came out about 640 units wide
    with 170 units of nothing either side. That is `CLAUDE.md` section 6.2c's second question
    exactly: *"Is this image fitted to the region it is SEEN in, or to the whole screen?"* The
    answer has to be baked into the file, because the C# cannot know where the ink is.

    WARNING  THE ANCHORS ARE REMAPPED IN THE SAME PASS OR EVERY LEADER LINE MOVES. They are
    normalised against the canvas, and cropping changes what "normalised" means. Doing the crop
    in an image editor later and leaving the manifest alone would put all eighteen arrow-heads
    somewhere off the pad, which is the drift this whole one-generator arrangement exists to
    prevent.
    """
    box = flat.getbbox()
    if box is None:
        return flat, anchors

    # A few pixels of air, so the outline's own antialiasing is not clipped.
    margin = 6
    x0 = max(0, box[0] - margin)
    y0 = max(0, box[1] - margin)
    x1 = min(flat.width, box[2] + margin)
    y1 = min(flat.height, box[3] + margin)

    cropped = flat.crop((x0, y0, x1, y1))
    width = float(x1 - x0)
    height = float(y1 - y0)

    moved = {}

    for name, (nx, ny) in anchors.items():
        moved[name] = ((nx * flat.width - x0) / width, (ny * flat.height - y0) / height)

    return cropped, moved


def write(pad, out_dir, preview):
    os.makedirs(out_dir, exist_ok=True)

    flat = pad.image.resize((WIDTH, HEIGHT), Image.LANCZOS)
    flat, anchors = crop_to_ink(flat, pad.anchors)

    png = os.path.join(out_dir, "pad_diagram_v1.png")
    flat.save(png)

    # WARNING  THE MANIFEST IS PLAIN `name x y` LINES AND NOT JSON, because Unity loads it as a
    # `TextAsset` from `Resources` and `JsonUtility` cannot deserialise a dictionary. A
    # three-token line needs no parser at all and no package reference.
    #
    # WARNING  AND ITS BASENAME MUST DIFFER FROM THE PNG'S, WHICH COST THE FIRST RENDER ALL
    # EIGHTEEN OF ITS LEADER LINES. It was `pad_diagram_v1.txt` beside `pad_diagram_v1.png`, and
    # `Resources.Load<TextAsset>("UI/input/pad_diagram_v1")` resolved the PNG for that name and
    # answered **null** for the text asset: no error, no log, and a screen that drew the pad and
    # the callouts perfectly with nothing joining them. `Resources.Load` matches on the PATH
    # first and the type second.
    manifest = os.path.join(out_dir, "pad_diagram_v1_anchors.txt")

    with open(manifest, "w", encoding="utf-8") as handle:
        handle.write("# generated by tools/build_controller_diagram.py - do not hand-edit\n")
        handle.write("# control  x  y   (normalised, y measured DOWN from the top)\n")

        for name in sorted(anchors):
            x, y = anchors[name]
            handle.write(f"{name} {x:.5f} {y:.5f}\n")

    print(f"wrote {png} ({flat.width}x{flat.height})")
    print(f"wrote {manifest} ({len(anchors)} anchors)")

    if preview:
        # A review render gets a light ground under it, because the screen it ships on is cream
        # paper and a transparent PNG viewed on white is a picture of a different thing.
        # `CLAUDE.md` section 6.2b: over the real background, never an empty scene.
        renders = os.path.join("Logs", "renders")
        os.makedirs(renders, exist_ok=True)

        ground = Image.new("RGBA", flat.size, (0xFC, 0xD3, 0x9F, 255))
        ground.alpha_composite(flat)

        shot = os.path.join(renders, "pad_diagram_v3.png")
        ground.convert("RGB").save(shot)
        print(f"wrote {shot}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", default=os.path.join(
        "Assets", "TumbangPreso", "Resources", "UI", "input"))
    parser.add_argument("--preview", action="store_true")
    args = parser.parse_args()

    write(build(), args.out, args.preview)
    return 0


if __name__ == "__main__":
    sys.exit(main())
