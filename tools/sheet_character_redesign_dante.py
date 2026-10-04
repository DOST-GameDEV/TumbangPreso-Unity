"""Stack and label the frames of tools/render_character_redesign_dante.py into review sheets.

    py -3 tools/sheet_character_redesign_dante.py v12

Reads Logs/character-redesign-dante/<version>/ and writes <version>_*.png beside that folder.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
S = ROOT + "/Logs/character-redesign-dante"
V = sys.argv[1]
R = f"{S}/{V}/"
BG = (34, 36, 44)
A = "A  block hair"
B = "B  lock hair"


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


grid("AB_turnaround", [("turn_A.png", A + ":  front, three-quarter, side, back"), ("turn_B.png", B + ":  front, three-quarter, side, back")], 1)
grid("AB_face", [("face_A.png", A), ("face_B.png", B), ("face34_A.png", A), ("face34_B.png", B)], 2)
grid("AB_hair_closeups", [("close_hair_back_A.png", A + ", from behind"), ("close_hair_back_B.png", B + ", from behind"),
                          ("close_hair_above_A.png", A + ", from above-front"), ("close_hair_above_B.png", B + ", from above-front")], 2,
     "The hair: no painted shine, a lighter top plane and darker underside per clump")
grid("AB_ear_closeups", [("close_ear_left_A.png", A + ", his left ear (horn side)"), ("close_ear_left_B.png", B + ", his left ear"),
                         ("close_ear_right_A.png", A + ", his right ear"), ("close_ear_right_B.png", B + ", his right ear"),
                         ("close_ear_left_front_A.png", A + ", left ear from the front"), ("close_ear_left_front_B.png", B + ", left ear from the front")], 2,
     "The ears: skin only, no hair or stubble paint on them")
grid("hands_and_hem_closeups", [("close_hand_left_front_A.png", "left hand, front"), ("close_hand_left_back_A.png", "left hand, back"),
                                ("close_hem_left_A.png", "coat hem at the left hand"),
                                ("close_hand_right_front_A.png", "right hand, front"), ("close_hand_right_back_A.png", "right hand, back"),
                                ("close_hem_right_A.png", "coat hem at the right hand")], 3,
     "The hands (same in A and B): skin, finger lines, shade; the wrap stays on the forearm", cell_w=620)
grid("body_closeups", [("close_chest_A.png", "chest"), ("close_back_A.png", "back"), ("close_feet_A.png", "feet"),
                       ("close_side_left_A.png", "A, his left side"), ("close_side_right_A.png", "A, his right side"), ("close_above_A.png", "A, from above"),
                       ("close_side_left_B.png", "B, his left side"), ("close_side_right_B.png", "B, his right side"), ("close_above_B.png", "B, from above")], 3,
     "Close pass, part by part", cell_w=620)

if os.path.exists(R + "far.png"):
    a, b = Image.open(R + "far.png").convert("RGB"), Image.open(R + "far_sil.png").convert("RGB")
    w, h = a.size
    sheet = Image.new("RGB", (w * 2 + 60, h + 100), BG)
    sheet.paste(a, (20, 60)); sheet.paste(b, (w + 40, 60))
    d = ImageDraw.Draw(sheet)
    text(d, (20 + w / 2, 14), "at 10 m: about 150 px tall (actual pixels)", 20)
    text(d, (w + 40 + w / 2, 14), "the same shot as silhouettes", 20)
    for i, n in enumerate(("original", "A block hair", "B lock hair")):
        text(d, (20 + w * (0.21 + 0.29 * i), h + 66), n, 16)
        text(d, (w + 40 + w * (0.21 + 0.29 * i), h + 66), n, 16)
    sheet.save(f"{S}/{V}_at_10m.png"); print("wrote", f"{S}/{V}_at_10m.png")

if os.path.exists(R + "trio.png"):
    im = Image.open(R + "trio.png").convert("RGB")
    d = ImageDraw.Draw(im)
    for i, n in enumerate(("ORIGINAL (in game today)", "REDESIGN A  block hair", "REDESIGN B  lock hair")):
        text(d, (im.width * (0.20 + 0.30 * i), im.height - 60), n, 28)
    im.save(f"{S}/{V}_original_A_B.png"); print("wrote", f"{S}/{V}_original_A_B.png")


if os.path.exists(R + "heads_front.png"):
    rows = [Image.open(R + f"heads_{t}.png").convert("RGB") for t in ("front", "threequarter", "side")]
    w, h = rows[0].size
    sheet = Image.new("RGB", (w, h * 3 + 70), BG)
    for i, r in enumerate(rows):
        sheet.paste(r, (0, 70 + i * h))
    d = ImageDraw.Draw(sheet)
    names = ["ORIGINAL (in game today)", "1  BLOCK: nearly the cube", "2  CARVED: softened block (built)", "3  SHAPED: cheeks, brow, jaw"]
    for i, n in enumerate(names):
        text(d, (w * (0.5 + (i - 1.5) * 0.2365), 18), n, 22)
    sheet.save(f"{S}/{V}_head_variants.png"); print("wrote", f"{S}/{V}_head_variants.png")

if os.path.exists(R + "golem.png"):
    im = Image.open(R + "golem.png").convert("RGB")
    d = ImageDraw.Draw(im)
    for x, n in ((0.22, "original"), (0.44, "redesign A"), (0.74, "Paete (the golem)")):
        text(d, (im.width * x, im.height - 60), n, 26)
    im.save(f"{S}/{V}_with_golem.png"); print("wrote", f"{S}/{V}_with_golem.png")
