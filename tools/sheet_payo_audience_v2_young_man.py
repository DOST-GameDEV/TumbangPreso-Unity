"""Stack and label the frames of tools/render_payo_audience_v2_young_man.py into review sheets.

    py -3 tools/sheet_payo_audience_v2_young_man.py v01

Reads ArtSource/payo/audience/v2/young_man/renders/<version>/ and writes renders/<version>_*.png.
A sheet is skipped when none of its frames were rendered. A copy of the Kuya Boy redesign's sheet
script rewritten for him: the row with the heroes and Classics (Kuya Boy, his base, beside him),
the face beside two heroes' and Kuya Boy's, the turnaround, the clip stills, the head turn.
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace("\\", "/")
S = ROOT + "/ArtSource/payo/audience/v2/young_man/renders"
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


ROW = "DANTE, BAYAN (Classic), RAFI, KUYA BOY (his base Classic), YOUNG MAN, BEBANG (Classic), CHESKA, LOLA PACING (Classic)"
grid("row", [("row_front.png", "front: " + ROW), ("row_34.png", "three-quarter: " + ROW),
             ("row_back.png", "back, the same eight in the other order")], 1,
     "THE TEST: young_man " + V + " with three redesigned heroes and the approved Classics, Kuya Boy beside him", cell_w=2200)
grid("crowd_distance", [("far.png", "the same row at crowd distance"), ("far_sil.png", "as silhouettes")], 2,
     "At crowd distance", cell_w=900)

cells = []
for view, vlab in (("front", "front"), ("right34", "three-quarter, his right"), ("left34", "three-quarter, his left")):
    cells += [(f"faces_rafi_{view}.png", "RAFI: " + vlab), (f"faces_new_{view}.png", "YOUNG MAN: " + vlab),
              (f"faces_dante_{view}.png", "DANTE: " + vlab), (f"faces_kuya_{view}.png", "KUYA BOY: " + vlab)]
grid("face", cells, 4, "His face beside Rafi's and Dante's (heroes) and Kuya Boy's (his base Classic)", cell_w=520, label=17)
grid("head", [(f"head_{v}.png", v) for v in ("front", "right34", "left", "back", "above")], 5, "His head", cell_w=440)

grid("turnaround", [("turn.png", "front, three-quarter, his left side, back"),
                    ("turn2.png", "the other three-quarter, his right side, both rear three-quarters")], 1,
     "young_man " + V + " turnaround", cell_w=1700)
grid("closeups", [(f"close_{n}.png", n) for n in ("chest", "back", "waist", "feet", "side", "above")], 3, "Close pass", cell_w=520)

CLIPS = (("idle_fold_right", "idle 0 s: folded, on his right leg"), ("idle_lean_in", "idle 1.9 s: leaning in"),
         ("idle_pump", "idle 3.45 s: the fist pump"), ("idle_pull", "idle 3.6 s: the pull down"),
         ("idle_lean_left", "idle 6.2 s: on his left leg, leaning in"), ("idle_low_pump", "idle 7.05 s: the small pump"),
         ("cheer_top", "cheer, top of the hop"), ("cheer_land", "cheer, landing"), ("clap_shut", "clap, hands meeting"),
         ("clap_open", "clap, wide open"), ("sit", "sit on a ledge (seat at z 0)"), ("sit_half", "sit, mid-breath"))
cells = []
for tag, lab in CLIPS:
    for view in ("front", "side", "back"):
        cells.append((f"clip_{tag}_{view}.png", f"{lab}: {view}"))
grid("clips", cells, 6, "Clip stills", cell_w=380, label=13)
cells = []
for tag, lab in (("yaw55", "head turned 55 left"), ("yawm55", "turned 55 right"), ("nod22", "nodded 22"), ("yaw55_nod22", "turned 55 and nodded 22")):
    for view in ("front", "back"):
        cells.append((f"look_{tag}_{view}.png", f"{lab}: {view}"))
grid("head_turn", cells, 4, "The head bone alone, turned and nodded", cell_w=420, label=14)
