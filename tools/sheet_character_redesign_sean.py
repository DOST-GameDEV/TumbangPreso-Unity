"""Stack and label the frames of tools/render_character_redesign_sean.py into review sheets.

    py -3 tools/sheet_character_redesign_sean.py v03

Reads Logs/character-redesign-sean/<version>/ and writes <version>_*.png beside that folder.
A sheet whose frames were not rendered is skipped, so a partial render still sheets.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
S = ROOT + "/Logs/character-redesign-sean"
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


grid("original", [("orig_turn.png", "ORIGINAL team-sean.glb:  front, three-quarter, side, back")], 1)
grid("turnaround", [("turn.png", "front, three-quarter, his left side, back"),
                    ("turn2.png", "back three-quarter, his right side, front three-quarter, back three-quarter")], 1,
     "Rago (roster id sean) redesign prototype " + V)
grid("face", [("face.png", "front"), ("face34.png", "three-quarter"), ("face34b.png", "three-quarter, other side"),
              ("face_side.png", "his left side")], 2)
grid("face_beside_original", [("facecmp_front.png", "ORIGINAL (left) and redesign (right), front"),
                              ("facecmp_34.png", "ORIGINAL (left) and redesign (right), three-quarter")], 1,
     "Rule 12: a cute face, not a portrait. Is it as cute as the original's, or cuter?")
grid("head_closeups", [("close_hair_back.png", "mohawk from behind"), ("close_hair_above.png", "from above-front"),
                       ("close_hair_side.png", "from his left"), ("close_ear_left.png", "his left ear and the razor slits"),
                       ("close_ear_right.png", "his right ear"), ("close_above.png", "from above")], 3,
     "The head: no painted hair shine, flat tones by which way a face points", cell_w=620)
grid("hands_and_hem", [("close_hand_left_front.png", "left fist, front"), ("close_hand_left_back.png", "left fist, back"),
                       ("close_hem_left.png", "vest hem and belt at his left arm"),
                       ("close_hand_right_front.png", "right fist, front"), ("close_hand_right_back.png", "right fist, back"),
                       ("close_hem_right.png", "vest hem and belt at his right arm")], 3,
     "Fists, bracers and the vest's hem", cell_w=620)
grid("body_closeups", [("close_chest.png", "chest"), ("close_back.png", "back"), ("close_feet.png", "feet"),
                       ("close_side_left.png", "his left side"), ("close_side_right.png", "his right side"),
                       ("close_feet_back.png", "feet from behind")], 3, "Close pass, part by part", cell_w=620)

grid("fists_and_bracers", [(f"hand_{tag}_{name}_{k}.png", f"{tag}, {name}, {45 * k} deg")
                           for tag in ("stand", "walk", "guard") for name in ("left", "right") for k in range(8)], 8,
     "Mapping check: each fist and bracer from eight angles, hanging (stand) and with the elbow bent (walk, guard)", cell_w=300)

grid("versus_previous", [("versus_a.png", "PREVIOUS then NEW: front, and three-quarter"),
                         ("versus_b.png", "PREVIOUS then NEW: side, and back")], 1, "The head at 0.84 beside the build before it")
grid("jump_first_six", [(f"jump6_{f}.png", f"jump frame {f}") for f in range(6)], 6,
     "`jump`, its first six frames (24 a second) from the front: the fists must go UP", cell_w=400)

LOOKS = (("yaw55", "head turned 55"), ("yawm55", "turned 55 the other way"), ("nod22", "nodded down 22"),
         ("back22", "tipped back 20"), ("yaw55nod22", "turned 55 and nodded 22"))
grid("neck_test", [(f"look_{tag}_{view}.png", f"{label}, {view}") for tag, label in LOOKS for view in ("front", "back", "side")], 3,
     "Rule 6: the head bone alone, from the front, the back and the side", cell_w=520)

IDLE = {48: "the flex", 98: "the bull", 137: "the guard", 145: "left jab", 157: "right jab"}
for clip, frames in (("idle", (48, 98, 137, 145, 157)), ("walk", (0, 4, 9, 13)), ("sprint", (0, 3, 6, 9)), ("jump", (0, 1, 3, 11)),
                     ("fall", (0, 2, 4, 6))):
    grid("clip_" + clip, [(f"clip_{clip}_{f:02d}_{view}.png", f"{IDLE.get(f, clip) if clip == 'idle' else clip} frame {f}, {view}")
                          for f in frames for view in ("front", "side", "back")],
         3, f"`{clip}` held at its telling frames (24 a second), from the front, the side and the back", cell_w=460)

if os.path.exists(R + "trio.png"):
    for src, out in (("trio.png", "with_original_and_dante"), ("trio_back.png", "with_original_and_dante_back")):
        if not os.path.exists(R + src):
            continue
        im = Image.open(R + src).convert("RGB")
        d = ImageDraw.Draw(im)
        for x, n in ((0.21, "ORIGINAL (in game today)"), (0.50, "REDESIGN"), (0.77, "Dante redesign")):
            text(d, (im.width * x, im.height - 60), n, 28)
        im.save(f"{S}/{V}_{out}.png"); print("wrote", f"{S}/{V}_{out}.png")

if os.path.exists(R + "far.png"):
    a, b = Image.open(R + "far.png").convert("RGB"), Image.open(R + "far_sil.png").convert("RGB")
    w, h = a.size
    sheet = Image.new("RGB", (w * 2 + 60, h + 100), BG)
    sheet.paste(a, (20, 60)); sheet.paste(b, (w + 40, 60))
    d = ImageDraw.Draw(sheet)
    text(d, (20 + w / 2, 14), "at game distance (actual pixels)", 20)
    text(d, (w + 40 + w / 2, 14), "the same shot as silhouettes", 20)
    sheet.save(f"{S}/{V}_at_distance.png"); print("wrote", f"{S}/{V}_at_distance.png")
