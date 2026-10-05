"""Stack and label the frames of tools/render_character_redesign_nemu.py into review sheets.

    py -3 tools/sheet_character_redesign_nemu.py v02
    py -3 tools/sheet_character_redesign_nemu.py v10 v09      (also: v09's turnaround over v10's)

Reads Logs/character-redesign-nemu/<version>/ and writes <version>_*.png beside that folder.
A sheet whose frames were not rendered this version is skipped.

THE SILHOUETTE SHEET is the one Dante's script does not have. docs/CAST_CLOTHING_STYLE.md: a
rework once moved Nemu's cowl, fringe and head size and the owner said "u ruined nemuu". So the
original and the redesign are rendered as flat silhouettes through one orthographic camera and
laid over each other here: grey where both are, RED where only the original is (the redesign
lost it), GREEN where only the redesign is (it added it). The count of each is printed, so "the
silhouette is kept" is a number and not a claim.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
S = ROOT + "/Logs/character-redesign-nemu"
V = sys.argv[1]
R = f"{S}/{V}/"
BG = (34, 36, 44)


def font(size):
    try:
        return ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", size)
    except OSError:
        return ImageFont.load_default()


def text(d, xy, s, size=24, anchor="mid"):
    f = font(size)
    w = d.textlength(s, font=f)
    x = xy[0] - w / 2 if anchor == "mid" else xy[0]
    d.text((x, xy[1]), s, font=f, fill=(255, 255, 255), stroke_width=3, stroke_fill=(20, 20, 26))


def grid(name, cells, cols, title=None, cell_w=None):
    """cells: [(file, label)]"""
    ims = [(Image.open(R + f).convert("RGB"), lab) for f, lab in cells if os.path.exists(R + f)]
    if not ims:
        return
    if cell_w:
        ims = [(im.resize((cell_w, int(im.height * cell_w / im.width)), Image.LANCZOS), lab) for im, lab in ims]
    w, h = ims[0][0].size
    rows = (len(ims) + cols - 1) // cols
    top = 56 if title else 8
    sheet = Image.new("RGB", (cols * w + (cols + 1) * 8, top + rows * (h + 8)), BG)
    d = ImageDraw.Draw(sheet)
    if title:
        text(d, (16, 12), title, 26, anchor="left")
    for i, (im, lab) in enumerate(ims):
        x = 8 + (i % cols) * (w + 8); y = top + (i // cols) * (h + 8)
        sheet.paste(im, (x, y))
        text(d, (x + 12, y + 8), lab, 20, anchor="left")
    sheet.save(f"{S}/{V}_{name}.png")
    print("wrote", f"{S}/{V}_{name}.png", sheet.size)


grid("turnaround", [("orig_turn.png", "ORIGINAL (in game today):  front, three-quarter, side, back"),
                    ("turn.png", "REDESIGN:  front, three-quarter, side, back"),
                    ("turn2.png", "REDESIGN:  the other three-quarter, the other side, both back three-quarters")], 1)
grid("face", [("face_pair_same_size.png", "front, ORIGINAL SHRUNK TO 0.84 for the comparison: original, redesign"),
              ("face34_pair_same_size.png", "three-quarter, original shrunk to 0.84: original, redesign"),
              ("face_pair.png", "front, true sizes: original, redesign"), ("face34_pair.png", "three-quarter: original, redesign"),
              ("faceside_pair.png", "side: original, redesign")], 1,
     "Her face: the cowl's rim, the fringe's foot and the eye bars are the original's numbers", cell_w=1200)
grid("hair_closeups", [("close_hair_back.png", "from behind"), ("close_hair_above.png", "from above, front"),
                       ("close_hair_above_back.png", "from above, behind"), ("close_face34.png", "her right"),
                       ("close_face34_l.png", "her left"), ("close_cowl_side.png", "side: lock, cheek, cowl")], 3,
     "The hair: no drawing on it, blocks only", cell_w=620)
grid("body_closeups", [("close_chest.png", "chest"), ("close_back.png", "back"), ("close_hem.png", "hem and hands"),
                       ("close_hand_left.png", "left cuff and hand"), ("close_hand_right.png", "right cuff and hand"),
                       ("close_hand_left_back.png", "left cuff from behind"), ("close_feet.png", "feet"),
                       ("close_hem_back.png", "hem from behind"), ("close_cowl_under.png", "under the cowl"),
                       ("close_side_left.png", "her left side"), ("close_side_right.png", "her right side"), ("close_above.png", "from above")], 3,
     "Close pass, part by part", cell_w=620)

if len(sys.argv) > 2:
    B = sys.argv[2]
    rows = [(f"{S}/{B}/turn.png", f"BEFORE ({B}):  front, three-quarter, side, back"), (R + "turn.png", f"AFTER ({V})"),
            (f"{S}/{B}/turn2.png", f"BEFORE ({B}):  the other views"), (R + "turn2.png", f"AFTER ({V})")]
    ims = [(Image.open(f).convert("RGB"), lab) for f, lab in rows if os.path.exists(f)]
    if len(ims) == 4:
        w, h = ims[0][0].size
        sheet = Image.new("RGB", (w + 16, 8 + 4 * (h + 8)), BG)
        d = ImageDraw.Draw(sheet)
        for i, (im, lab) in enumerate(ims):
            sheet.paste(im, (8, 8 + i * (h + 8)))
            text(d, (20, 16 + i * (h + 8)), lab, 22, anchor="left")
        sheet.save(f"{S}/{V}_before_after.png"); print("wrote", f"{S}/{V}_before_after.png")

LOOK = [("yaw55", "head turned 55 to her left"), ("yawm55", "head turned 55 to her right"), ("nod22", "nodded 22 down"),
        ("up20", "tipped 20 up"), ("yaw55nod", "turned 55 and nodded 22")]
grid("head_turn", [(f"look_{tag}_{view}.png", f"{lab}, {view}") for tag, lab in LOOK for view in ("front", "side", "back", "above")], 4,
     "RIG TEST: the head bone alone. The cowl is the head's, the lock tails the torso's.", cell_w=470)

CLIPS = {"idle": (0, 60, 70, 79, 118, 150, 161), "walk": (0, 4, 9, 13), "sprint": (0, 3, 6, 9), "jump": (0, 1, 3, 6, 11),
         "fall": (0, 2, 4, 6)}
NAMES = {("idle", 0): "stand", ("idle", 60): "NODS OFF", ("idle", 70): "jolts awake", ("idle", 79): "looks round",
         ("idle", 118): "YAWN AND STRETCH", ("idle", 150): "SLEEVE SWISH", ("idle", 161): "swish, other way"}
cells = [(f"ext_{clip}_{f:03d}_{view}.png", f"{clip} {NAMES.get((clip, f), 'f%d' % f)} {view}") for clip, frames in CLIPS.items() for f in frames
         for view in ("front", "side", "back")]
grid("extremes", cells, 6, "The clips at their extremes: front, side, back three-quarter", cell_w=380)

for name, labels in (("trio.png", ((0.22, "ORIGINAL (in game today)"), (0.47, "REDESIGN"), (0.76, "redesigned Dante"))),
                     ("trio_back.png", ((0.22, "redesigned Dante"), (0.50, "REDESIGN"), (0.76, "ORIGINAL")))):
    if os.path.exists(R + name):
        im = Image.open(R + name).convert("RGB")
        d = ImageDraw.Draw(im)
        for x, n in labels:
            text(d, (im.width * x, im.height - 70), n, 28)
        out = f"{S}/{V}_{name.replace('.png', '')}_original_redesign_dante.png"
        im.save(out); print("wrote", out)

# the silhouettes, laid over each other
rows = []
for pose in ("rest", "idle"):
    for view in ("front", "side", "back"):
        a, b = R + f"sil_old_{pose}_{view}.png", R + f"sil_new_{pose}_{view}.png"
        if not (os.path.exists(a) and os.path.exists(b)):
            continue
        old = Image.open(a).convert("L").point(lambda v: 255 if v < 128 else 0)
        new = Image.open(b).convert("L").point(lambda v: 255 if v < 128 else 0)
        w, h = old.size
        out = Image.new("RGB", (w, h), (255, 255, 255))
        po, pn, px = old.load(), new.load(), out.load()
        both = lost = added = 0
        for y in range(h):
            for x in range(w):
                o, n = po[x, y], pn[x, y]
                if o and n:
                    px[x, y] = (150, 150, 158); both += 1
                elif o:
                    px[x, y] = (230, 40, 40); lost += 1
                elif n:
                    px[x, y] = (30, 170, 60); added += 1
        d = ImageDraw.Draw(out)
        label = "%s, %s: lost %.1f%%, added %.1f%% of the original's area" % (pose, view, 100.0 * lost / (both + lost), 100.0 * added / (both + lost))
        text(d, (12, 10), label, 20, anchor="left")
        print("silhouette", label)
        rows.append(out)
if rows:
    w, h = rows[0].size
    sheet = Image.new("RGB", (3 * w, 60 + (len(rows) // 3) * h), BG)
    for i, im in enumerate(rows):
        sheet.paste(im, ((i % 3) * w, 60 + (i // 3) * h))
    text(ImageDraw.Draw(sheet), (16, 14), "Silhouette, original against redesign: grey both, RED only the original, GREEN only the redesign", 26, anchor="left")
    sheet.save(f"{S}/{V}_silhouette.png"); print("wrote", f"{S}/{V}_silhouette.png")
