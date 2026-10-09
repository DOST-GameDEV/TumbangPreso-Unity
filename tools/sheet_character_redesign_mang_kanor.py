"""Stack and label the frames of tools/render_character_redesign_mang_kanor.py into review sheets.

    py -3 tools/sheet_character_redesign_mang_kanor.py v01

Reads Logs/character-redesign-mang_kanor/<version>/ and writes <version>_*.png beside that
folder. A sheet is skipped when none of its frames were rendered. His own copy of Bayan's sheet
script (docs/CHARACTER_REDESIGN_DANTE.md section 13), with the sheets the Classic brief asks for:
THE TEST (him in a row with three redesigned heroes and the three approved Classics), his face
beside two heroes' and the original's, a turnaround over the original's, the head turned and
nodded, both fists close, the idle's stances and the far frames of the locomotion clips.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
S = ROOT + "/Logs/character-redesign-mang_kanor"
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


# (a) THE TEST
ORDER = "DANTE, BAYAN, SEAN, MANG KANOR, LOLA PACING, AMIHAN, BEBANG"
grid("row", [("row_front.png", "front: " + ORDER), ("row_34.png", "three-quarter: " + ORDER),
             ("row_back.png", "back: BEBANG, AMIHAN, LOLA PACING, MANG KANOR, SEAN, BAYAN, DANTE")], 1,
     "THE TEST: Mang Kanor " + V + " in a row with three redesigned heroes and the three approved Classics", cell_w=2400)
grid("row_near", [("near_front.png", "front: BAYAN, MANG KANOR, LOLA PACING, DANTE"), ("near_34.png", "three-quarter: the same four")], 1,
     "Mang Kanor " + V + " beside the two approved Classics nearest him, and Dante")

# (b) the face beside two heroes' and the original's
cells = []
for view, vlab in (("front", "front"), ("right34", "three-quarter, his right"), ("left34", "three-quarter, his left")):
    cells += [(f"faces_dante_{view}.png", "DANTE REDESIGN: " + vlab), (f"faces_new_{view}.png", "MANG KANOR REDESIGN: " + vlab),
              (f"faces_sean_{view}.png", "SEAN REDESIGN: " + vlab), (f"faces_orig_{view}.png", "ORIGINAL: " + vlab)]
grid("face", cells, 4, "His face beside Dante's and Sean's redesigns, and the original's", cell_w=560, label=17)

# (c) the turnaround over the original's
grid("turn", [("turn.png", "REDESIGN: front, three-quarter, side, back"),
              ("orig_turn.png", "ORIGINAL (character-male-e.glb in his palette): the same four"),
              ("turn2.png", "REDESIGN: the other three-quarter, the other side, both rear three-quarters"),
              ("orig_turn2.png", "ORIGINAL: the same four")], 1, "Mang Kanor redesign " + V + " over the original", cell_w=1500)

cells = []
for view, vlab in (("front", "front"), ("right34", "three-quarter"), ("left", "his left side"), ("back", "back"), ("above", "from above")):
    cells += [(f"head_new_{view}.png", "REDESIGN: " + vlab), (f"head_orig_{view}.png", "ORIGINAL: " + vlab)]
grid("head", cells, 4, "His head beside the original's", cell_w=520, label=18)

# both fists
cells = []
for side in ("left", "right"):
    for view, vlab in (("front", "front"), ("back", "back"), ("above", "from above"), ("below", "from below"), ("end", "end on")):
        cells.append((f"hand_{side}_{view}.png", f"{side} fist, rest pose: {vlab}"))
for side in ("left", "right"):
    for view, vlab in (("front", "front"), ("back", "back"), ("out", "from his side")):
        cells.append((f"handidle_{side}_{view}.png", f"{side} fist, idle frame 0: {vlab}"))
cells += [("rest_front.png", "REDESIGN, rest pose: front"), ("rest_top.png", "REDESIGN, rest pose: above"),
          ("orig_rest_front.png", "ORIGINAL, rest pose: front"), ("orig_rest_top.png", "ORIGINAL, rest pose: above")]
grid("hands", cells, 5, "Both fists: the far end of the forearm, bare skin, nothing wider past the wrist", cell_w=480, label=15)

grid("close", [("close_hair_back.png", "hair from behind"), ("close_hair_left.png", "hair, his left"), ("close_hair_right.png", "hair, his right"),
               ("close_hair_above.png", "hair from above"), ("close_chest.png", "chest"), ("close_neck.png", "neck"),
               ("close_belt.png", "waistband and belt bag"), ("close_back.png", "back"), ("close_feet.png", "feet"),
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
grid("look", cells, 5, "The head bone alone, turned 55 and nodded 22 (a rig test): front, back, side, and the neck close", cell_w=420, label=14)

IDLE = (("00", "the stand"), ("15", "the glasses, nudged"), ("25", "looking down the road"), ("42", "the fare call, beckoning"),
        ("46", "the fare call, arm up"), ("76", "the ride, a left corner"), ("84", "the ride, a right corner"), ("89", "the brake"))
cells = []
for fr, lab in IDLE:
    for view in ("front", "side", "back"):
        cells.append((f"ext_idle_{fr}_{view}.png", f"idle, {lab}: {view}"))
grid("idle", cells, 6, "The idle's stances, one frame inside each", cell_w=360, label=13)
cells = []
for clip, fracs in (("walk", (25, 75, 50)), ("sprint", (25, 75, 50)), ("jump", (6, 30, 95)), ("fall", (25, 75))):
    for fr in fracs:
        for view in ("front", "side", "back"):
            cells.append((f"ext_{clip}_{fr:02d}_{view}.png", f"{clip} at {fr}%: {view}"))
grid("motion", cells, 9, "walk, sprint, jump and fall: their far frames", cell_w=300, label=12)

# him between the original and Dante's redesign, front and back
ims = [Image.open(R + f).convert("RGB") for f in ("pair.png", "pair_back.png") if os.path.exists(R + f)]
if ims:
    w, h = ims[0].size
    sheet = Image.new("RGB", (w + 16, len(ims) * (h + 8) + 8), BG)
    for i, im in enumerate(ims):
        d = ImageDraw.Draw(im)
        names = ("ORIGINAL (in game today)", "MANG KANOR REDESIGN " + V, "DANTE REDESIGN")
        for x, n in zip((0.19, 0.50, 0.81), names if i == 0 else names[::-1]):
            text(d, (im.width * x, im.height - 60), n, 26)
        text(d, (16, 12), "front" if i == 0 else "back", 26, anchor="left")
        sheet.paste(im, (8, 8 + i * (h + 8)))
    sheet.save(f"{S}/{V}_pair.png"); print("wrote", f"{S}/{V}_pair.png")

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
