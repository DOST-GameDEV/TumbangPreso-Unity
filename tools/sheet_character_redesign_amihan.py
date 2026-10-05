"""Stack and label the frames of tools/render_character_redesign_amihan.py into review sheets.

    py -3 tools/sheet_character_redesign_amihan.py v01

Reads Logs/character-redesign-amihan/<version>/ and writes <version>_*.png beside that folder.
A sheet is skipped when none of its frames were rendered. Her own copy of Dante's sheet script
(docs/CHARACTER_REDESIGN_DANTE.md section 13), with two sheets his does not have: the head
turned and nodded over the capelet, and the far frames of every clip.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
S = ROOT + "/Logs/character-redesign-amihan"
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


grid("turnaround", [("turn.png", "front, three-quarter, side, back"),
                    ("turn2.png", "the other three-quarter, the other side, both rear three-quarters")], 1,
     "Amihan redesign " + V)
grid("face", [("face.png", "redesign"), ("face_orig.png", "original (in game today)"),
              ("face34.png", "redesign, three-quarter"), ("face34_orig.png", "original, three-quarter"),
              ("face34_left.png", "redesign, her left (the flowers)"), ("face34_left_orig.png", "original, her left")], 2,
     "Rule 12, a cute face and not a portrait: judged beside the original")
grid("hair_closeups", [("close_hair_back.png", "from behind"), ("close_hair_above.png", "from above-front"),
                       ("close_hair_above_back.png", "from above-behind"), ("close_ear_left.png", "her left ear"),
                       ("close_ear_right.png", "her right ear"), ("close_flowers.png", "the cotton-boll pin")], 3,
     "The hair: flat tones, no painted shine; ears skin only", cell_w=620)
grid("hands_and_hem_closeups", [("close_hand_left_front.png", "left hand, front"), ("close_hand_left_back.png", "left hand, back"),
                                ("close_hem_left.png", "coat hem at the left hand"),
                                ("close_hand_right_front.png", "right hand, front"), ("close_hand_right_back.png", "right hand, back"),
                                ("close_hem_right.png", "coat hem at the right hand")], 3,
     "The hands: skin, finger lines, shade; the cuff's paint stays on the cuff", cell_w=620)
grid("body_closeups", [("close_chest.png", "chest"), ("close_neck.png", "capelet and neck"), ("close_back.png", "back"),
                       ("close_cape_back.png", "the kasikus"), ("close_feet.png", "feet"), ("close_below.png", "from below"),
                       ("close_side_left.png", "her left side"), ("close_side_right.png", "her right side"), ("close_above.png", "from above")], 3,
     "Close pass, part by part", cell_w=620)

grid("turnaround_vs_previous", [("prev_new.png", "this version: front, three-quarter, side, back"),
                                ("prev_prev.png", "the version before it, the same camera")], 1, "Amihan redesign " + V + " beside the version before")
grid("jump_first_six", [(f"jump6_{i}_front.png", f"jump, frame {i} of 30: front") for i in range(6)]
     + [(f"jump6_{i}_rear.png", f"frame {i}: from behind") for i in range(6)], 6, "The jump's first six frames", label=15)

cells = []
for view, vlab in (("front", "front"), ("right34", "three-quarter, her right"), ("left34", "three-quarter, her left")):
    cells += [(f"family_new_{view}.png", "AMIHAN REDESIGN: " + vlab), (f"family_dante_{view}.png", "DANTE REDESIGN: " + vlab),
              (f"family_orig_{view}.png", "AMIHAN ORIGINAL: " + vlab)]
grid("head_family", cells, 3, "Her head on Dante's head shape: the same family, beside his and beside her original", label=20)

cells = []
for view, vlab in (("front", "front"), ("right34", "three-quarter, her right"), ("left34", "three-quarter, her left")):
    cells += [(f"faces_new_{view}.png", "REDESIGN: " + vlab), (f"faces_orig_{view}.png", "ORIGINAL: " + vlab)]
grid("faces_large", cells, 2, "Amihan's face beside the original's: the head at 0.84, her measured eyes at 1.15", label=22)

cells = []
for tag, lab in (("rest", "head straight"), ("nod10", "nodded 10"), ("yaw30_nod10", "turned 30 to her left, nodded 10"),
                 ("yawm30_nod10", "turned 30 to her right, nodded 10")):
    for view, vlab in (("right34", "three-quarter, her right"), ("front", "front"), ("left34", "three-quarter, her left")):
        cells.append((f"jaw_{tag}_{view}.png", f"{lab}: {vlab}"))
grid("jaw", cells, 3, "The lower face: one skin tone, no dark shapes along the jaw; the neck closed", label=16)

LOOKS = (("yaw55", "head turned 55 to her left"), ("yawm55", "turned 55 to her right"), ("nod22", "nodded 22"),
         ("up20", "tipped back 20"), ("yaw55_nod22", "turned 55 and nodded 22"), ("yawm55_up20", "turned 55 and tipped back"))
cells = []
for tag, lab in LOOKS:
    for view in ("front", "back", "side", "top"):
        cells.append((f"look_{tag}_{view}.png", f"{lab}: {view}"))
grid("head_turn", cells, 4, "The head bone alone over the capelet (a rig test): front, back, side, above", cell_w=440, label=16)

for clip, fracs in (("idle", (0, 22, 49, 79)), ("walk", (25, 75, 50)), ("sprint", (25, 75, 50)), ("jump", (6, 30, 95)), ("fall", (25, 75))):
    cells = []
    for fr in fracs:
        for view in ("front", "side", "back", "rear"):
            cells.append((f"ext_{clip}_{fr:02d}_{view}.png", f"{clip} at {fr}%: {view}"))
    grid("extremes_" + clip, cells, 4, None, cell_w=400, label=15)

if os.path.exists(R + "pair.png"):
    for f, out in (("pair.png", "original_redesign_dante"), ("pair_back.png", "original_redesign_dante_back")):
        if not os.path.exists(R + f):
            continue
        im = Image.open(R + f).convert("RGB")
        d = ImageDraw.Draw(im)
        for i, n in enumerate(("ORIGINAL (in game today)", "AMIHAN REDESIGN", "DANTE REDESIGN")):
            text(d, (im.width * (0.20 + 0.30 * i), im.height - 60), n, 28)
        im.save(f"{S}/{V}_{out}.png"); print("wrote", f"{S}/{V}_{out}.png")

if os.path.exists(R + "far.png"):
    a, b = Image.open(R + "far.png").convert("RGB"), Image.open(R + "far_sil.png").convert("RGB")
    w, h = a.size
    sheet = Image.new("RGB", (w * 2 + 60, h + 100), BG)
    sheet.paste(a, (20, 60)); sheet.paste(b, (w + 40, 60))
    d = ImageDraw.Draw(sheet)
    text(d, (20 + w / 2, 14), "at 10 m: about 150 px tall (actual pixels)", 20)
    text(d, (w + 40 + w / 2, 14), "the same shot as silhouettes", 20)
    for i, n in enumerate(("original", "redesign", "Dante redesign")):
        text(d, (20 + w * (0.21 + 0.29 * i), h + 66), n, 16)
        text(d, (w + 40 + w * (0.21 + 0.29 * i), h + 66), n, 16)
    sheet.save(f"{S}/{V}_at_10m.png"); print("wrote", f"{S}/{V}_at_10m.png")
