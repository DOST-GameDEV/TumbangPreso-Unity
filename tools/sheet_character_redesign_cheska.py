"""Stack and label the frames of tools/render_character_redesign_cheska.py into review sheets.

    py -3 tools/sheet_character_redesign_cheska.py v01

Reads Logs/character-redesign-cheska/<version>/ and writes <version>_*.png beside that folder.
A sheet is skipped when its frames were not rendered.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
S = ROOT + "/Logs/character-redesign-cheska"
V = sys.argv[1]
R = f"{S}/{V}/"
BG = (38, 34, 40)


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


grid("original", [("orig_turn.png", "THE ORIGINAL (in the game today): front, three-quarter, side, back")], 1)
grid("original_closeups", [("orig_face.png", "face"), ("orig_face34.png", "three-quarter"), ("orig_hair_back.png", "back"),
                           ("orig_chest.png", "chest"), ("orig_back.png", "back, close"), ("orig_feet.png", "feet"),
                           ("orig_side_left.png", "her left side"), ("orig_above.png", "above"), ("orig_hem_left.png", "sleeve and cord")], 3,
     "The original, looked at before anything was designed", cell_w=560)
grid("turnaround", [("turn.png", "REDESIGN: front, three-quarter, side, back"),
                    ("turn2.png", "the other three-quarter, her right side, both back quarters")], 1)
grid("face", [("face.png", "front"), ("face34.png", "three-quarter"), ("face34_left.png", "other three-quarter"), ("face_side.png", "side")], 2)
grid("face_beside_original", [("facepair.png", "ORIGINAL (left) and REDESIGN (right)"), ("facepair34.png", "three-quarter"),
                              ("facepair_eyes.png", "eyes and mouth, close: the notch in each eye")], 1)
grid("heads_original_redesign_dante", [("headpair_front.png", "ORIGINAL, REDESIGN, DANTE REDESIGN: front"), ("headpair_34.png", "three-quarter"),
                                       ("headpair_34_left.png", "the other three-quarter")], 1)
grid("beside_previous", [("prev_a.png", "PREVIOUS then NEW: front pair, three-quarter pair"), ("prev_b.png", "side pair, back pair")], 1)
grid("jump_first_six", [(f"jump6_{i}.png", f"jump frame {i}") for i in range(6)], 6)
grid("idle_stances", [(f"idle_{n}_{v}.png", f"{lab}, {v}") for n, lab in (("warm", "warming her hands"), ("hop", "hop"),
      ("tug_hold", "hands on the hat strings"), ("tug_left", "tugging her left"), ("tug_right", "tugging her right"))
      for v in ("front", "side", "back", "close")], 4, "The acted idle at its held stances", cell_w=480)
grid("head_closeups", [("close_hair_back.png", "hair and mantle, from behind"), ("close_hair_above.png", "from above-front"),
                       ("close_hair_above_back.png", "from above-behind"), ("close_ear_left.png", "her left flap"),
                       ("close_ear_right.png", "her right flap"), ("close_above.png", "from above")], 3,
     "The hat and hair: flat tones on the hair, blocks down the back", cell_w=620)
grid("body_closeups", [("close_chest.png", "chest"), ("close_back.png", "back"), ("close_feet.png", "feet"),
                       ("close_feet_back.png", "heels"), ("close_side_left.png", "her left side"), ("close_side_right.png", "her right side"),
                       ("close_hand_left_front.png", "left hand, front"), ("close_hand_left_back.png", "left hand, back"),
                       ("close_hem_left.png", "left sleeve, cord and hem"),
                       ("close_hand_right_front.png", "right hand, front"), ("close_hand_right_back.png", "right hand, back"),
                       ("close_hem_right.png", "right sleeve, cord and hem")], 3,
     "Close pass, part by part", cell_w=620)

LOOK = (("yaw55", "head turned 55 to her left"), ("yaw-55", "turned 55 to her right"), ("nod22", "nodded 22"),
        ("up20", "tipped back 20"), ("yaw55_nod22", "turned 55 and nodded 22"))
grid("head_turn", [(f"look_{tag}_{view}.png", f"{lab}, {view}") for tag, lab in LOOK for view in ("front", "back", "side")], 3,
     "RIG TEST: the head bone alone, over idle. Flaps, fur, cords and hair must survive it", cell_w=520)

CLIPS = (("walk", (25, 50, 75)), ("sprint", (25, 50, 75)), ("jump", (6, 30, 95)), ("fall", (25, 75)))
grid("clip_extremes", [(f"clip_{clip}_{f:02d}_{view}.png", f"{clip} {f}%, {view}") for clip, fr in CLIPS for f in fr
                       for view in ("front", "side", "back")], 6,
     "The new clips at their extreme frames", cell_w=330)

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

for src, out in (("trio.png", "original_redesign_dante"), ("trio_back.png", "original_redesign_dante_back")):
    if os.path.exists(R + src):
        im = Image.open(R + src).convert("RGB")
        d = ImageDraw.Draw(im)
        names = ("ORIGINAL (in game today)", "REDESIGN", "DANTE REDESIGN")
        if "back" in src:
            names = names[::-1]
        for i, n in enumerate(names):
            text(d, (im.width * (0.20 + 0.30 * i), im.height - 60), n, 28)
        im.save(f"{S}/{V}_{out}.png"); print("wrote", f"{S}/{V}_{out}.png")
