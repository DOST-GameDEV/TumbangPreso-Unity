"""Stack and label the frames of tools/render_character_redesign_paete.py into review sheets.

    py -3 tools/sheet_character_redesign_paete.py v03

Reads Logs/character-redesign-paete/<version>/ and writes <version>_*.png beside that folder.
Paete's own copy of the sheet script (docs/CHARACTER_REDESIGN_DANTE.md section 13). A sheet is
only written when its frames exist, so a partial render gives a partial set.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
S = ROOT + "/Logs/character-redesign-paete"
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
    for attempt in range(5):
        try:
            sheet.save(f"{S}/{V}_{name}.png")
            break
        except OSError:
            import time
            time.sleep(1.0)
    print("wrote", f"{S}/{V}_{name}.png", sheet.size)


ANGLES = ((0, "front"), (35, "three-quarter"), (90, "side"), (180, "back"), (215, "back three-quarter"))
grid("turnaround", [(f"turn_{rot:03d}.png", "REDESIGN, " + lab) for rot, lab in ANGLES]
     + [(f"origturn_{rot:03d}.png", "ORIGINAL, " + lab) for rot, lab in ANGLES], 5,
     "Paete: the redesign (top) over the original team-paete.glb (bottom), idle frame 0", cell_w=620)
grid("original", [(f"orig_turn_{rot:03d}.png", "ORIGINAL, " + lab) for rot, lab in ANGLES], 5, cell_w=620)
grid("face", [("face.png", "front"), ("face34.png", "three-quarter, his right"), ("face34b.png", "three-quarter, his left")], 3,
     "The face: the mask's planks as real blocks over a dark core, the eyes measured and sunk under the brow, the mouth one stroke", cell_w=640)
grid("face_beside_original", [("facecmp_front.png", "ORIGINAL (left)   REDESIGN (right), front"),
                              ("facecmp_34.png", "ORIGINAL (left)   REDESIGN (right), three-quarter"),
                              ("facecmp_34b.png", "the other three-quarter")], 1,
     "Beside the original: brow blocks back, eyes measured at their own size, mouth one stroke")
grid("head_family", [("headfam_front.png", "ORIGINAL            PAETE REDESIGN            DANTE REDESIGN")], 1,
     "Beside redesigned Dante: his head keeps its own narrow plank mask, at full size (the owner's exception)")

braid = []
for tag, lab in (("left", "his LEFT braid"), ("right", "his RIGHT braid")):
    for view in ("front", "above", "back"):
        braid.append((f"braid_orig_{tag}_{view}.png", f"ORIGINAL, {lab}, {view}"))
        braid.append((f"braid_new_{tag}_{view}.png", f"REDESIGN, {lab}, {view}"))
grid("forearm_braids", braid, 2,
     "The vine-braid forearms, rest pose: the original's own typed paths, drawn through twice the rings, to true points", cell_w=620)
grid("forearm_braids_idle", [("braid_idle_left.png", "his left braid, hanging in idle"), ("braid_idle_right.png", "his right braid, hanging in idle")], 2)

grid("body_closeups", [("close_chest.png", "chest"), ("origclose_chest.png", "ORIGINAL chest"), ("close_back.png", "back"),
                       ("origclose_back.png", "ORIGINAL back"),
                       ("close_legs_front.png", "legs"), ("origclose_legs_front.png", "ORIGINAL legs"), ("close_legs_back.png", "legs, back"),
                       ("close_feet.png", "root feet"),
                       ("close_shoulder_left.png", "left shoulder"), ("origclose_shoulder_left.png", "ORIGINAL left shoulder"),
                       ("close_shoulder_right.png", "right shoulder"), ("close_above.png", "from above"),
                       ("close_head_back.png", "back of the head"), ("origclose_head_back.png", "ORIGINAL back of the head"),
                       ("close_head_above.png", "crown"), ("close_side_left.png", "his left side")], 4,
     "Part by part: every plank its own cut and its own grain", cell_w=520)

looks = [(f"look_{tag}_{view}.png", f"{lab}, {view}")
         for tag, lab in (("yaw+55", "turned 55"), ("yaw-55", "turned -55"), ("nod-22", "nodded 22 down"),
                          ("nod+20", "tipped 20 back"), ("yaw55_nod22", "turned 55 and nodded 22"))
         for view in ("front", "back", "side")]
grid("head_turn", looks, 3, "The head bone alone, over idle: collar leaves, collar moss, nape moss, antlers against the shoulders", cell_w=520)

ext = []
for clip, frames in (("idle", (0, 50, 106, 152)), ("walk", (4, 13)), ("sprint", (3, 9)), ("jump", (0, 2, 5, 11)), ("fall", (2, 6))):
    for f in frames:
        for view in ("front", "side", "back"):
            ext.append((f"ext_{clip}_{f:03d}_{view}.png", f"{clip} frame {f}, {view}"))
grid("clips", ext, 6, "Clips: idle (the stand, the seed, rooting, the reach), walk, sprint, jump, fall", cell_w=420)

for name, out in (("trio.png", "original_redesign_dante"), ("trio_back.png", "original_redesign_dante_back")):
    if not os.path.exists(R + name):
        continue
    im = Image.open(R + name).convert("RGB")
    d = ImageDraw.Draw(im)
    for i, n in enumerate(("ORIGINAL (in game today)", "PAETE REDESIGN", "DANTE REDESIGN")):
        text(d, (im.width * (0.20 + 0.31 * i), im.height - 60), n, 28)
    im.save(f"{S}/{V}_{out}.png"); print("wrote", f"{S}/{V}_{out}.png")
