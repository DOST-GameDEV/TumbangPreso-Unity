"""Stack and label the frames of tools/render_character_redesign_rafi.py into review sheets.

    py -3 tools/sheet_character_redesign_rafi.py v03

Reads Logs/character-redesign-rafi/<version>/ and writes <version>_*.png beside that folder.
Rafi's own copy of Dante's sheet script (docs/CHARACTER_REDESIGN_DANTE.md section 13). A sheet is
only written when its frames exist, so a partial render gives a partial set.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
S = ROOT + "/Logs/character-redesign-rafi"
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


grid("turnaround", [("turn.png", "Rafi redesign:  front, three-quarter, side, back, back three-quarter")], 1)
grid("original", [("orig_turn.png", "ORIGINAL team-rafi.glb:  front, three-quarter, side, back, back three-quarter")], 1)
grid("face", [("face.png", "front"), ("face34.png", "three-quarter, his right"), ("face34b.png", "three-quarter, his left")], 3,
     "The face: the original's ink eyes and level mouth on flat skin", cell_w=640)
grid("face_beside_original", [("facecmp_front.png", "ORIGINAL (left)   REDESIGN (right), front"),
                              ("facecmp_34.png", "ORIGINAL (left)   REDESIGN (right), three-quarter")], 1,
     "Rule 12: a cute face, not a portrait. No nose, no sockets, no creases; the original's ink, a third bigger")
grid("head_family", [("headfam_front.png", "ORIGINAL        RAFI REDESIGN        DANTE REDESIGN,  front"),
                     ("headfam_34r.png", "three-quarter, his right"), ("headfam_34l.png", "three-quarter, his left")], 1,
     "Dante's head shape on Rafi's box: soft corners, fullest at the cheeks, lower corners rounded in, no nose, no brow")
grid("head_084_beside_previous", [("prev_turn.png", "each pair: PREVIOUS (full head) then NOW (head at 0.84): front, three-quarter, back")], 1)
grid("jump_fall_tooth", [(f"jumpfront_{f}.png", f"jump frame {f}") for f in range(6)]
     + [("fall_2_front.png", "fall 2, front"), ("fall_6_front.png", "fall 6, front"), ("fall_2_back.png", "fall 2, back"),
        ("fall_6_back.png", "fall 6, back"), ("tooth_front.png", "idle: hand on the tooth"), ("tooth_side.png", "the same, his right side")], 6,
     "Arms with the 0.84 head: the jump's first six frames, fall, and the shark tooth stance")
grid("hair_closeups", [("close_hair_back.png", "from behind"), ("close_hair_above.png", "from above-front"),
                       ("close_hair_above_back.png", "from above-behind"),
                       ("close_ear_left.png", "his left ear and drop"), ("close_ear_right.png", "his right ear and drop"),
                       ("close_above.png", "from above")], 3,
     "Hair, putong, ears: flat tones only, blocks on the back, skin-only ears", cell_w=620)
grid("hands_and_hem_closeups", [("close_hand_left_front.png", "left hand, front"), ("close_hand_left_back.png", "left hand, back"),
                                ("close_hem_front.png", "front flap and hem"),
                                ("close_hand_right_front.png", "right hand, front"), ("close_hand_right_back.png", "right hand, back"),
                                ("close_hem_back.png", "back flap and hem")], 3,
     "Hands, cuffs, flaps: paint stops at its own piece", cell_w=620)
grid("body_closeups", [("close_chest.png", "chest"), ("close_back.png", "back"), ("close_legs_front.png", "legs"),
                       ("close_side_left.png", "his left side"), ("close_side_right.png", "his right side"), ("close_feet.png", "feet"),
                       ("close_arm_left.png", "left arm"), ("close_arm_right.png", "right arm"),
                       ("close_elbow_left.png", "elbows bent (sprint)"), ("close_elbow_right.png", "elbows bent (sprint)")], 3,
     "The tattoos, part by part (the builder's own routes, painted)", cell_w=620)

looks = [(f"look_{tag}_{view}.png", f"{lab}, {view}")
         for tag, lab in (("yaw+55", "turned 55"), ("yaw-55", "turned -55"), ("nod-22", "nodded 22 down"),
                          ("nod+20", "tipped 20 back"), ("yaw55_nod22", "turned 55 and nodded 22"))
         for view in ("front", "back", "side")]
grid("head_turn", looks, 3, "Rule 6: the head bone alone, over idle. Tail, putong tails, side hair, necklace", cell_w=520)

ext = []
for clip, frames in (("idle", (60, 106, 144, 163)), ("walk", (4, 13)), ("sprint", (3, 9)), ("jump", (2, 11)), ("fall", (2, 6))):
    for f in frames:
        for view in ("front", "side", "back"):
            ext.append((f"ext_{clip}_{f:02d}_{view}.png", f"{clip} frame {f}, {view}"))
grid("extremes", ext, 6, "Clip extremes: limbs and hair against cloth", cell_w=400)

if os.path.exists(R + "trio.png"):
    for name, out in (("trio.png", "original_redesign_dante"), ("trio_back.png", "original_redesign_dante_back")):
        if not os.path.exists(R + name):
            continue
        im = Image.open(R + name).convert("RGB")
        d = ImageDraw.Draw(im)
        for i, n in enumerate(("ORIGINAL (in game today)", "RAFI REDESIGN", "DANTE REDESIGN")):
            text(d, (im.width * (0.22 + 0.29 * i), im.height - 60), n, 28)
        im.save(f"{S}/{V}_{out}.png"); print("wrote", f"{S}/{V}_{out}.png")
