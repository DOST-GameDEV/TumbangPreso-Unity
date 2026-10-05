"""Stack and label the frames of tools/render_character_redesign_zack.py into review sheets.

    py -3 tools/sheet_character_redesign_zack.py v03

Reads Logs/character-redesign-zack/<version>/ and writes <version>_*.png beside that folder.
A sheet is skipped when its frames were not rendered. Zack's own copy of the Dante sheet script.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
S = ROOT + "/Logs/character-redesign-zack"
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
        text(d, (x + 12, y + 8), lab, 22, anchor="left")
    sheet.save(f"{S}/{V}_{name}.png")
    print("wrote", f"{S}/{V}_{name}.png", sheet.size)


grid("turnaround", [("turn.png", "Zack (Isagani) redesign:  front, three-quarter, his left side, back, his right side, three-quarter"),
                    ("turn_prev.png", "the build before this one (head at full size), the same six views"),
                    ("orig_turn.png", "the original, the same six views")], 1)
grid("face", [("face_head_front.png", "REDESIGN, front"), ("face_head_34_left.png", "REDESIGN, three-quarter, his left"), ("face_head_34_right.png", "REDESIGN, three-quarter, his right"),
              ("face_orig.png", "ORIGINAL, front"), ("face_orig_34_left.png", "ORIGINAL, three-quarter, his left"), ("face_orig_34_right.png", "ORIGINAL, three-quarter, his right")], 3,
     "The face beside the original's (rule 12): solid ink eyes, no nose, no lines, no blush, the smirk", cell_w=720)
grid("face_vs_original", [(f"facebig_{tag}_{view}.png", f"{label}, {name}")
                          for tag, label in (("new", "REDESIGN"), ("orig", "ORIGINAL"))
                          for view, name in (("front", "front"), ("34_left", "three-quarter, his left"), ("34_right", "three-quarter, his right"))], 3,
     "Zack (Isagani): the redesign's face beside the original's. Eyes and mouth are the original's measured outlines")
# the smug candidates. Made with, for each of measured, A, B, C:
#     py -3 tools/author_character_redesign_zack_textures.py --face A
#     copy zack-redesign.glb and zack-redesign-atlas.png into Logs/character-redesign-zack/variants/A/
#     ... render_character_redesign_zack.py -- vNN smug
if os.path.exists(R + "smug_A_front.png"):
    cols = [("orig", "ORIGINAL"), ("measured", "v13 (measured)"), ("A", "A  subtle"), ("B", "B  medium"), ("C", "C  strong, uneven eyes")]
    big = [Image.open(R + f"smug_{k}_{v}.png").convert("RGB") for v in ("front", "34") for k, _ in cols]
    far = [Image.open(R + f"smug_{k}_far.png").convert("RGB") for k, _ in cols]
    w, h = big[0].size
    fw, fh = far[0].size
    sheet = Image.new("RGB", (5 * w + 48, 60 + 2 * (h + 8) + fh + 60), BG)
    d = ImageDraw.Draw(sheet)
    text(d, (16, 14), "Zack (Isagani): three smug candidates beside the original and the measured face. Bottom row: actual pixels at lineup size", 26, anchor="left")
    for i, im in enumerate(big):
        x0, y0 = 8 + (i % 5) * (w + 8), 60 + (i // 5) * (h + 8)
        sheet.paste(im, (x0, y0))
        text(d, (x0 + 12, y0 + 8), cols[i % 5][1] + (", front" if i < 5 else ", three-quarter"), 22, anchor="left")
    for i, im in enumerate(far):
        x0 = 8 + i * (w + 8) + (w - fw) // 2
        sheet.paste(im, (x0, 60 + 2 * (h + 8) + 20))
    sheet.save(f"{S}/{V}_smug_candidates.png"); print("wrote", f"{S}/{V}_smug_candidates.png", sheet.size)

grid("head_family", [(f"family_{tag}_{view}.png", f"{label}: {name}")
                     for view, name in (("front", "front"), ("34_right", "three-quarter, his right"), ("34_left", "three-quarter, his left"))
                     for tag, label in (("new", "ZACK REDESIGN"), ("dante", "DANTE REDESIGN"), ("orig", "ZACK ORIGINAL"))], 3,
     "His head on Dante's head shape: the same family, beside Dante's and beside his original")
grid("hair_closeups", [("close_head_back.png", "from behind"), ("close_head_above.png", "from above, front"), ("close_head_above_back.png", "from above, behind"),
                       ("close_ear_left.png", "his left ear and the earring"), ("close_ear_right.png", "his right ear"), ("close_neck_back.png", "the nape and the collar")], 3,
     "The hair: flat tones only, blocks on the back and the crown; ears stay skin", cell_w=640)
grid("body_closeups", [("close_chest.png", "chest"), ("close_back.png", "back"), ("close_feet.png", "feet"),
                       ("close_hand_left_front.png", "left hand, front"), ("close_hand_right_front.png", "right hand, front"), ("close_hem_right.png", "hem and wallet chain"),
                       ("close_hand_left_back.png", "left hand, back"), ("close_hand_right_back.png", "right hand, back"), ("close_feet_back.png", "feet from behind"),
                       ("close_shoe_left.png", "his left shoe"), ("close_shoe_right_in.png", "his right shoe"), ("close_elbow_left_back.png", "left elbow from behind"),
                       ("close_side_left.png", "his left side"), ("close_side_right.png", "his right side"), ("close_above.png", "from above")], 3,
     "Close pass, part by part", cell_w=640)
grid("neck_test", [("neck_yaw_left_front.png", "head turned 55, front"), ("neck_yaw_left_back.png", "turned 55, back"), ("neck_yaw_left_side.png", "turned 55, side"),
                   ("neck_yaw_right_front.png", "turned -55, front"), ("neck_yaw_right_back.png", "turned -55, back"), ("neck_yaw_right_side.png", "turned -55, side"),
                   ("neck_nod_down_front.png", "nodded 22 down, front"), ("neck_nod_down_back.png", "nodded down, back"), ("neck_nod_down_side.png", "nodded down, side"),
                   ("neck_nod_up_front.png", "tipped 22 back, front"), ("neck_nod_up_back.png", "tipped back, back"), ("neck_nod_up_side.png", "tipped back, side")], 3,
     "Rule 6: the head bone alone, turned 55 degrees and nodded 22 (a rig test, not a game clip)", cell_w=560)
grid("idle_stances", [(f"idle_{name}_{view}.png", f"{label}, {view.replace('_', ' ')}")
                      for name, label in (("pockets", "hands in pockets"), ("quiff", "fixing the quiff"), ("behind", "hands behind his back"))
                      for view in ("front", "side_left", "side_right", "back")], 4,
     "idle: the three stances at the middle of their holds, checked for limbs inside the body or clothes")
grid("jump_start", [(f"jumpstart_{view}_{i}.png", f"jump frame {i}, {'front' if view == 'front' else 'three-quarter'}")
                    for view in ("front", "34") for i in range(6)], 6,
     "jump: its first six frames. The arms must read as UP, with air between fist and cheek")
for clip in ("walk", "sprint", "jump", "fall"):
    grid("extremes_" + clip, [(f"clip_{clip}_{k}_{view}.png", f"{clip}, extreme {k + 1}, {view}") for k in (0, 1) for view in ("front", "side", "back")], 3,
         f"{clip}: the two frames furthest from rest, checked for limbs or hair passing through cloth")

for tag, names in (("front", ("ORIGINAL (in game today)", "REDESIGN", "redesigned Dante")), ("back", ("redesigned Dante", "REDESIGN", "ORIGINAL (in game today)"))):
    if os.path.exists(R + f"trio_{tag}.png"):
        im = Image.open(R + f"trio_{tag}.png").convert("RGB")
        d = ImageDraw.Draw(im)
        for i, n in enumerate(names):
            text(d, (im.width * (0.22 + 0.29 * i), im.height - 60), n, 28)
        im.save(f"{S}/{V}_trio_{tag}.png"); print("wrote", f"{S}/{V}_trio_{tag}.png")

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
