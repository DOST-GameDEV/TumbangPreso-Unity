"""Join the frames of tools/render_character_redesign_motion.py into ONE gif and one still sheet.

    py -3 tools/sheet_character_redesign_motion.py dante v22

Writes, beside the version folder in Logs/character-redesign-<id>/:
    <version>_<id>_motion.gif          a GRID: every animation in its own cell, all playing at once
    <version>_<id>_motion_sheet.png    four frames of every segment from three views, to LOOK at
                                       (a gif cannot be judged frame by frame; read this, fix, show)

⚠️ A GRID, NOT A SEQUENCE. The first version played the segments one after another. Owner,
2026-10-05: *"the gif i was expecting was more like a grid where all animations play at the same
time instead of one long contiguous animation sequence"*.

The gif is 96 frames at 24 a second, the rate the frames were rendered at, so every clip plays
at its real speed. Each cell loops inside those 96: `sprint` (12) eight times, `fall` (8) twelve,
`idle` (32) three, the spin once with each of its 24 steps held four frames, `walk` (17) six
times with one frame dropped, `jump` (12) three times with a hold after each, and the head-turn
test (43) once at half speed.

ONE palette for the whole gif and no dithering. A palette per frame makes low-contrast paint
shimmer from frame to frame, and the owner read exactly that as z-fighting (2026-10-05).
"""
import glob
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
WHO, VERSION = sys.argv[1], sys.argv[2]
BASE = "%s/Logs/character-redesign-%s" % (ROOT, WHO)
SRC = "%s/%s/motion/" % (BASE, VERSION)
VIEWS = ("front", "side", "back")
FRAMES = 96
FRAME_MS = 42
CELL = (200, 250)   # 1000 wide: the gif is every frame whole, so its size is its area
COLUMNS = 5
#   (segment, view, label)
CELLS = (
    ("spin", "front", "360"),
    ("idle", "front", "idle"),
    ("walk", "front", "walk"),
    ("sprint", "front", "sprint"),
    ("jump", "front", "jump"),
    ("fall", "front", "fall"),
    ("look", "front", "look around (rig test)"),
    ("walk", "side", "walk, side"),
    ("sprint", "side", "sprint, side"),
    ("look", "back", "look around, back"),
)
STILL_ROWS = ("spin", "idle", "walk", "sprint", "jump", "fall", "look")


def font(size):
    try:
        return ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", size)
    except OSError:
        return ImageFont.load_default()


def label(im, text, size):
    ImageDraw.Draw(im).text((8, 6), text, font=font(size), fill=(255, 255, 255), stroke_width=3, stroke_fill=(20, 20, 26))


def count(tag):
    return len(glob.glob(SRC + "%s_%s_*.png" % (tag, VIEWS[0])))


def frame(tag, view, i):
    return Image.open(SRC + "%s_%s_%03d.png" % (tag, view, i)).convert("RGB")


def which(tag, n, i):
    """The frame of a segment `n` long that its cell shows at gif frame `i`."""
    if tag == "spin":
        return (i // 4) % n
    if tag == "walk":
        return min(n - 1, int(round((i % 16) * n / 16.0)))
    if tag == "jump":
        return min(i % 32, n - 1)
    if tag == "look":
        return min(n - 1, int(round(i * n / float(FRAMES))))
    return i % n


cells = [(tag, view, text, count(tag)) for tag, view, text in CELLS if count(tag)]
# an ACTED idle is longer than 96 frames (Dante's is 8 s): the gif then runs 192 so it is seen whole
if count("idle") > FRAMES:
    FRAMES = 192
rows = (len(cells) + COLUMNS - 1) // COLUMNS
cache = {}


def cell(tag, view, text, k):
    key = (tag, view, k)
    if key not in cache:
        im = frame(tag, view, k).resize(CELL, Image.LANCZOS)
        label(im, text, 15)
        cache[key] = im
    return cache[key]


sequence = []
for i in range(FRAMES):
    im = Image.new("RGB", (CELL[0] * COLUMNS, CELL[1] * rows), (34, 36, 44))
    for c, (tag, view, text, n) in enumerate(cells):
        im.paste(cell(tag, view, text, which(tag, n, i)), ((c % COLUMNS) * CELL[0], (c // COLUMNS) * CELL[1]))
    ImageDraw.Draw(im).text((im.width - 150, im.height - 22), WHO + "  " + VERSION, font=font(14), fill=(200, 200, 210))
    sequence.append(im)

palette = sequence[FRAMES // 3].quantize(256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
quantized = [f.quantize(palette=palette, dither=Image.Dither.NONE) for f in sequence]
gif = "%s/%s_%s_motion.gif" % (BASE, VERSION, WHO)
quantized[0].save(gif, save_all=True, append_images=quantized[1:], duration=FRAME_MS, loop=0, optimize=False)
print("wrote", gif, len(sequence), "frames", round(os.path.getsize(gif) / 1e6, 1), "MB")

# the still sheet: four frames of each segment, all three views of each
stills = []
for tag in STILL_ROWS:
    n = count(tag)
    if not n:
        continue
    row = []
    for k in range(4):
        parts = [frame(tag, v, (n * k) // 4) for v in VIEWS]
        w, h = parts[0].size
        im = Image.new("RGB", (w * len(parts), h))
        for j, p in enumerate(parts):
            im.paste(p, (w * j, 0))
        label(im, "%s:  %s  %d/%d" % (WHO, tag, (n * k) // 4, n), 20)
        row.append(im)
    stills.append(row)
w, h = stills[0][0].size
sheet = Image.new("RGB", (w * 4, h * len(stills)), (34, 36, 44))
for i, row in enumerate(stills):
    for k, im in enumerate(row):
        sheet.paste(im, (w * k, h * i))
sheet = sheet.resize((2400, int(sheet.height * 2400 / sheet.width)), Image.LANCZOS)
png = "%s/%s_%s_motion_sheet.png" % (BASE, VERSION, WHO)
sheet.save(png)
print("wrote", png, sheet.size)
