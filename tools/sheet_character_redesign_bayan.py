"""Stack and label the frames of tools/render_character_redesign_bayan.py into review sheets.

    py -3 tools/sheet_character_redesign_bayan.py v01

Reads Logs/character-redesign-bayan/<version>/ and writes <version>_*.png beside that folder.
A sheet is skipped when none of its frames were rendered. His own copy of the cast's sheet
script (docs/CHARACTER_REDESIGN_DANTE.md section 13), with the sheets the owner asked for on the
Classic characters: his face beside the ORIGINAL's (the stock rig in HIS palette) and Dante's
redesign, a turnaround over the original's, him between the original and Dante's redesign from
the front and the back, the head turned and nodded, and both fists close.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
S = ROOT + "/Logs/character-redesign-bayan"
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


def grid(name, cells, cols, title=None, cell_w=None, label=20):
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
        text(d, (x + 12, y + 8), lab, label, anchor="left")
    sheet.save(f"{S}/{V}_{name}.png")
    print("wrote", f"{S}/{V}_{name}.png", sheet.size)


# (b) the turnaround over the original's
grid("turnaround_over_original", [("turn.png", "REDESIGN: front, three-quarter, side, back"),
                                  ("orig_turn.png", "ORIGINAL (character-male-f.glb in his palette): the same four"),
                                  ("turn2.png", "REDESIGN: the other three-quarter, the other side, both rear three-quarters"),
                                  ("orig_turn2.png", "ORIGINAL: the same four")], 1, "Bayan (BERTO) redesign " + V + " over the original", cell_w=1500)

# (a) the face beside the original's and Dante's redesign
cells = []
for view, vlab in (("front", "front"), ("right34", "three-quarter, his right"), ("left34", "three-quarter, his left")):
    cells += [(f"faces_dante_{view}.png", "DANTE REDESIGN: " + vlab), (f"faces_new_{view}.png", "BAYAN REDESIGN: " + vlab),
              (f"faces_sean_{view}.png", "SEAN REDESIGN: " + vlab), (f"faces_orig_{view}.png", "ORIGINAL: " + vlab)]
grid("face_vs_heroes", cells, 4, "His face beside Dante's and Sean's redesigns, and the original's", cell_w=560, label=17)
grid("row_with_heroes", [("row_front.png", "front: DANTE, SEAN, BAYAN, AMIHAN"), ("row_34.png", "three-quarter: DANTE, SEAN, BAYAN, AMIHAN"),
                         ("row_back.png", "back: AMIHAN, BAYAN, SEAN, DANTE")], 1, "THE TEST: Bayan " + V + " in a row with the redesigned heroes")

cells = []
for view, vlab in (("front", "front"), ("right34", "three-quarter"), ("left", "his left side"), ("back", "back"), ("above", "from above")):
    cells += [(f"head_new_{view}.png", "REDESIGN: " + vlab), (f"head_orig_{view}.png", "ORIGINAL: " + vlab)]
grid("head_vs_original", cells, 4, "His head beside the original's", cell_w=520, label=18)

# (e) both fists
cells = []
for side in ("left", "right"):
    for view, vlab in (("front", "front"), ("back", "back"), ("above", "from above"), ("below", "from below"), ("end", "end on")):
        cells.append((f"hand_{side}_{view}.png", f"{side} fist, rest pose: {vlab}"))
for side in ("left", "right"):
    for view, vlab in (("front", "front"), ("back", "back"), ("out", "from his side")):
        cells.append((f"handidle_{side}_{view}.png", f"{side} fist, idle frame 0: {vlab}"))
    for view, vlab in (("front", "ORIGINAL, rest: front"), ("above", "ORIGINAL, rest: above")):
        cells.append((f"orig_hand_{side}_{view}.png", f"{side} fist, {vlab}"))
grid("hands_closeups", cells, 5, "Both fists: the far end of the forearm, bare skin, nothing wider past the wrist", cell_w=480, label=15)
grid("rest_pose", [("rest_front.png", "REDESIGN, rest pose: front"), ("rest_top.png", "REDESIGN, rest pose: above"),
                   ("orig_rest_front.png", "ORIGINAL, rest pose: front"), ("orig_rest_top.png", "ORIGINAL, rest pose: above")], 4,
     "The rest pose: the redesign's arm is straight (the elbow is a bone); the original bakes the bend into the mesh", cell_w=560, label=16)

grid("closeups", [("close_hair_back.png", "hair from behind"), ("close_hair_left.png", "hair, his left"), ("close_hair_right.png", "hair, his right"),
                  ("close_hair_above.png", "hair from above"), ("close_chest.png", "chest"), ("close_neck.png", "neck"),
                  ("close_belt.png", "belt and buckle"), ("close_back.png", "back"), ("close_feet.png", "feet"),
                  ("close_feet_back.png", "feet, from behind"), ("close_side_left.png", "his left side"), ("close_side_right.png", "his right side"),
                  ("close_above.png", "from above"), ("close_below.png", "from below")], 4, "Close pass, part by part", cell_w=560, label=18)

# (d) the head turned and nodded
LOOKS = (("yaw55", "head turned 55 to his left"), ("yawm55", "turned 55 to his right"), ("nod22", "nodded 22"),
         ("up20", "tipped back 20"), ("yaw55_nod22", "turned 55 left and nodded 22"), ("yawm55_nod22", "turned 55 right and nodded 22"))
cells = []
for tag, lab in LOOKS:
    for view in ("front", "back", "side"):
        cells.append((f"look_{tag}_{view}.png", f"{lab}: {view}"))
    cells.append((f"neck_{tag}_front.png", f"{lab}: neck, front"))
    cells.append((f"neck_{tag}_back.png", f"{lab}: neck, back"))
grid("head_turn", cells, 5, "The head bone alone, turned 55 and nodded 22 (a rig test): front, back, side, and the neck close", cell_w=420, label=14)

IDLE = (("00", "the stand"), ("18", "the hitch, looking right"), ("30", "the hitch, looking left"), ("44", "the neck, cracked to his left"),
        ("72", "the wind-up"), ("75", "the throw"), ("80", "the follow-through"))
# (v05 on: the brief is the heroes' style, so the face sheet leads with Dante and Sean and the
# row sheet is the test; the original comes second)
cells = []
for fr, lab in IDLE:
    for view in ("front", "side", "back"):
        cells.append((f"ext_idle_{fr}_{view}.png", f"idle, {lab}: {view}"))
grid("extremes_idle", cells, 6, "The idle's stances, one frame inside each", cell_w=360, label=13)
cells = []
for clip, fracs in (("walk", (25, 75, 50)), ("sprint", (25, 75, 50)), ("jump", (6, 30, 95)), ("fall", (25, 75))):
    for fr in fracs:
        for view in ("front", "side", "back"):
            cells.append((f"ext_{clip}_{fr:02d}_{view}.png", f"{clip} at {fr}%: {view}"))
grid("extremes_locomotion", cells, 9, "walk, sprint, jump and fall: their far frames", cell_w=300, label=12)

# (c) him between the original and Dante's redesign, front and back
ims = [Image.open(R + f).convert("RGB") for f in ("pair.png", "pair_back.png") if os.path.exists(R + f)]
if ims:
    w, h = ims[0].size
    sheet = Image.new("RGB", (w + 16, len(ims) * (h + 8) + 8), BG)
    for i, im in enumerate(ims):
        d = ImageDraw.Draw(im)
        names = ("ORIGINAL (in game today)", "BAYAN REDESIGN " + V, "DANTE REDESIGN")
        for x, n in zip((0.19, 0.50, 0.81), names if i == 0 else names[::-1]):
            text(d, (im.width * x, im.height - 60), n, 26)
        text(d, (16, 12), "front" if i == 0 else "back", 26, anchor="left")
        sheet.paste(im, (8, 8 + i * (h + 8)))
    sheet.save(f"{S}/{V}_original_redesign_dante.png"); print("wrote", f"{S}/{V}_original_redesign_dante.png")

# the head at 0.84 beside the head at full size (rendered by `headscale`)
grid("head_scale", [("scale_pair.png", "ORIGINAL, REDESIGN with the head at 0.84 (the rule), REDESIGN with the head at 1.00, DANTE REDESIGN"),
                    ("scale_pair_back.png", "the same four from behind")], 1, "Does 0.84 suit him? Both, beside the original and Dante")

if os.path.exists(R + "far.png"):
    a, b = Image.open(R + "far.png").convert("RGB"), Image.open(R + "far_sil.png").convert("RGB")
    w, h = a.size
    sheet = Image.new("RGB", (w * 2 + 60, h + 100), BG)
    sheet.paste(a, (20, 60)); sheet.paste(b, (w + 40, 60))
    d = ImageDraw.Draw(sheet)
    text(d, (20 + w / 2, 14), "at 10 m (actual pixels)", 20)
    text(d, (w + 40 + w / 2, 14), "the same shot as silhouettes", 20)
    for i, n in enumerate(("original", "redesign", "Dante redesign")):
        text(d, (20 + w * (0.17 + 0.31 * i), h + 66), n, 16)
        text(d, (w + 40 + w * (0.17 + 0.31 * i), h + 66), n, 16)
    sheet.save(f"{S}/{V}_at_10m.png"); print("wrote", f"{S}/{V}_at_10m.png")
