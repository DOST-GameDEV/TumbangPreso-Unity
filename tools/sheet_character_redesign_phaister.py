"""Stack and label the frames of tools/render_character_redesign_phaister.py into review sheets.

    py -3 tools/sheet_character_redesign_phaister.py v01

Reads Logs/character-redesign-phaister/<version>/ and writes <version>_*.png beside that folder.
A sheet is skipped when none of its frames were rendered. Her own copy of the cast's sheet script
(docs/CHARACTER_REDESIGN_DANTE.md section 13), with the sheets her review asks for: her face
beside the ORIGINAL's and beside the redesigned Amihan's, both hands from every side (her kit
hangs props on them), and the head turned and nodded over the collar, front and back.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
S = ROOT + "/Logs/character-redesign-phaister"
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
     "Phaister redesign " + V)
grid("original_turnaround", [("orig_turn.png", "ORIGINAL: front, three-quarter, side, back"),
                             ("orig_turn2.png", "ORIGINAL: the other three-quarter, the other side, both rear three-quarters")], 1,
     "Phaister as she is in the game today (team-phaister.glb)")

cells = []
for view, vlab in (("front", "front"), ("right34", "three-quarter, her right"), ("left34", "three-quarter, her left")):
    cells += [(f"faces_new_{view}.png", "REDESIGN: " + vlab), (f"faces_orig_{view}.png", "ORIGINAL: " + vlab),
              (f"faces_amihan_{view}.png", "AMIHAN REDESIGN: " + vlab)]
grid("face_vs_original", cells, 3, "Her face beside the original's and beside Amihan's redesign: head at 0.84, measured eyes at 1.15", label=20)
grid("face_front", [("faces_new_front.png", "REDESIGN"), ("faces_orig_front.png", "ORIGINAL (in the game today)")], 2,
     "Phaister's face beside the original's")

cells = []
for view, vlab in (("front", "front"), ("right34", "three-quarter"), ("left", "her left side"), ("back", "back"), ("above", "from above")):
    cells += [(f"head_new_{view}.png", "REDESIGN: " + vlab), (f"head_orig_{view}.png", "ORIGINAL: " + vlab)]
hat = []
for view, vlab in (("front", "front"), ("right34", "three-quarter"), ("above", "from above")):
    hat += [(f"head_new_{view}.png", "REDESIGN: " + vlab), (f"head_orig_{view}.png", "ORIGINAL: " + vlab)]
grid("hat_vs_original", hat, 2, "Her hat beside the original's: full size, not scaled with the head", label=22)
grid("head_and_hat", cells, 4, "Her head and hat beside the original's", cell_w=560, label=18)

cells = []
for side in ("left", "right"):
    for view, vlab in (("front", "front"), ("back", "back"), ("above", "from above"), ("below", "from below"), ("end", "end on")):
        cells.append((f"hand_{side}_{view}.png", f"{side} hand, rest pose: {vlab}"))
grid("hands_closeups", cells, 5, "Both hands, arms straight out as the rig binds them: skin and finger lines on flat faces only", cell_w=520, label=16)
cells = []
for side in ("left", "right"):
    for view, vlab in (("front", "front"), ("back", "back"), ("out", "from her side")):
        cells.append((f"handidle_{side}_{view}.png", f"{side} hand, idle frame 0: {vlab}"))
grid("hands_idle_closeups", cells, 3, "Both hands as they hang in the idle", cell_w=560, label=16)

grid("hat_and_hair_closeups", [("close_hat_front.png", "hat, front"), ("close_hat_left.png", "hat, her left (the wands)"),
                               ("close_hat_right.png", "hat, her right (the pins)"), ("close_hat_above.png", "hat from above"),
                               ("close_hair_back.png", "hair from behind"), ("close_hair_left.png", "hair, her left"),
                               ("close_hair_right.png", "hair, her right")], 4,
     "The hat and the hair: flat tones, no painted shine", cell_w=560, label=18)
grid("body_closeups", [("close_chest.png", "chest"), ("close_neck.png", "collar and neck"), ("close_belt.png", "belt and skirt"),
                       ("close_manika.png", "the manika"), ("close_back.png", "the cape: moon and stars"), ("close_feet.png", "feet"),
                       ("close_feet_back.png", "feet, from behind"), ("close_below.png", "from below"),
                       ("close_side_left.png", "her left side"), ("close_side_right.png", "her right side"), ("close_above.png", "from above")], 4,
     "Close pass, part by part", cell_w=560, label=18)

grid("turnaround_vs_previous", [("prev_new.png", "this version: front, three-quarter, side, back"),
                                ("prev_prev.png", "the version before it, the same camera")], 1, "Phaister redesign " + V + " beside the version before")
grid("jump_first_six", [(f"jump6_{i}_front.png", f"jump, frame {i} of 30: front") for i in range(6)]
     + [(f"jump6_{i}_rear.png", f"frame {i}: from behind") for i in range(6)], 6, "The jump's first six frames", label=15)

LOOKS = (("yaw55", "head turned 55 to her left"), ("yawm55", "turned 55 to her right"), ("nod22", "nodded 22"),
         ("up20", "tipped back 20"), ("yaw55_nod22", "turned 55 left and nodded 22"), ("yawm55_nod22", "turned 55 right and nodded 22"))
cells = []
for tag, lab in LOOKS:
    for view in ("front", "back", "side", "top"):
        cells.append((f"look_{tag}_{view}.png", f"{lab}: {view}"))
grid("head_turn", cells, 4, "The head bone alone, over the collar and the cape (a rig test): front, back, side, above", cell_w=440, label=16)
cells = []
for tag, lab in LOOKS:
    for view in ("front", "back"):
        cells.append((f"neck_{tag}_{view}.png", f"{lab}: neck, {view}"))
grid("head_turn_neck", cells, 4, "The same test, close on the neck: front and back", cell_w=520, label=16)

IDLE = (("00", "the stand"), ("21", "the doll and the pin"), ("50", "the moth, held"), ("53", "the moth, flicked off (4.25 s)"),
        ("80", "the moon"))
cells = []
for fr, lab in IDLE:
    for view in ("front", "side", "back", "rear"):
        cells.append((f"ext_idle_{fr}_{view}.png", f"idle, {lab}: {view}"))
grid("extremes_idle", cells, 4, None, cell_w=400, label=15)
for clip, fracs in (("walk", (25, 75, 50)), ("sprint", (25, 75, 50)), ("jump", (6, 30, 95)), ("fall", (25, 75))):
    cells = []
    for fr in fracs:
        for view in ("front", "side", "back", "rear"):
            cells.append((f"ext_{clip}_{fr:02d}_{view}.png", f"{clip} at {fr}%: {view}"))
    grid("extremes_" + clip, cells, 4, None, cell_w=400, label=15)

if os.path.exists(R + "pair.png"):
    for f, out in (("pair.png", "original_redesign_cast"), ("pair_back.png", "original_redesign_cast_back")):
        if not os.path.exists(R + f):
            continue
        im = Image.open(R + f).convert("RGB")
        d = ImageDraw.Draw(im)
        for x, n in ((0.17, "ORIGINAL (in game today)"), (0.42, "PHAISTER REDESIGN"), (0.66, "AMIHAN REDESIGN"), (0.85, "DANTE REDESIGN")):
            text(d, (im.width * x, im.height - 60), n, 26)
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
        text(d, (20 + w * (0.17 + 0.31 * i), h + 66), n, 16)
        text(d, (w + 40 + w * (0.17 + 0.31 * i), h + 66), n, 16)
    sheet.save(f"{S}/{V}_at_10m.png"); print("wrote", f"{S}/{V}_at_10m.png")
