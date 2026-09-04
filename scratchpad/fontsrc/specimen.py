# Renders the candidate body faces at the PHYSICAL pixel sizes this game draws at,
# beside Darumadrop doing the same job today. CLAUDE.md 6.1: show, do not describe.
#
# The scale factor is MenuKit.MinReadableUnits' own arithmetic: AspectSafeCanvas matches
# on the short axis, so a 4:3 1024x768 panel renders one canvas unit at 768/1440 = 0.533
# physical pixels. That is the worst case the game supports and the size 132.8's blur
# complaint was made at.
from PIL import Image, ImageDraw, ImageFont

SRC = "scratchpad/fontsrc"
DARUMA = "Assets/TumbangPreso/Art/ui/fonts/DarumadropOne-Regular.ttf"
WORST = 0.533           # 4:3 at 1024x768
NATIVE = 1.0            # 1920x1080

BG = (244, 236, 221)    # UiTheme.Paper
INK = (59, 36, 21)      # UiTheme.PaperInk
SOFT = (122, 92, 64)    # UiTheme.PaperInkSoft
RULE = (220, 193, 154)  # UiTheme.PaperEdge

# PaperKit's type scale, in canvas units.
DISPLAY, TITLE, BODY, CAPTION = 44, 26, 20, 16

CANDS = [
    ("CURRENT  Darumadrop everywhere",      DARUMA,                          DARUMA),
    ("Nunito",              f"{SRC}/Nunito-Regular.ttf",           f"{SRC}/Nunito-Bold.ttf"),
    ("M PLUS Rounded 1c",   f"{SRC}/MPLUSRounded1c-Regular.ttf",   f"{SRC}/MPLUSRounded1c-Bold.ttf"),
    ("Figtree",             f"{SRC}/Figtree-Regular.ttf",          f"{SRC}/Figtree-Bold.ttf"),
    ("Baloo 2",             f"{SRC}/Baloo2-Regular.ttf",           f"{SRC}/Baloo2-Bold.ttf"),
]

def px(units, scale): return max(6, int(round(units * scale)))

def panel(name, reg, bold, scale, w=920):
    """One candidate's column: display face stays Darumadrop, body face is the candidate."""
    h = 470
    img = Image.new("RGB", (w, h), BG)
    d = ImageDraw.Draw(img)

    disp   = ImageFont.truetype(DARUMA, px(DISPLAY, scale))
    ttl    = ImageFont.truetype(DARUMA, px(TITLE, scale))
    body   = ImageFont.truetype(reg,  px(BODY, scale))
    bodyb  = ImageFont.truetype(bold, px(BODY, scale))
    cap    = ImageFont.truetype(reg,  px(CAPTION, scale))
    capb   = ImageFont.truetype(bold, px(CAPTION, scale))
    floor  = ImageFont.truetype(reg,  px(18, scale))

    y = 14
    d.text((18, y), name, font=ImageFont.truetype(bold if bold != DARUMA else DARUMA,
                                                  px(18, scale)), fill=(160, 40, 30))
    y += px(30, scale) + 10
    d.line([(14, y), (w - 14, y)], fill=RULE, width=2); y += 14

    # Display step: this stays Darumadrop in every candidate. It is the control.
    d.text((18, y), "CHOOSE YOUR HERO", font=disp, fill=INK); y += px(DISPLAY, scale) + 12

    # Title step, Darumadrop: the name of a thing.
    d.text((18, y), "AUDIO", font=ttl, fill=INK); y += px(TITLE, scale) + 10

    # Body step, candidate face: a settings row, which is what a front end is mostly made of.
    d.text((18, y), "Master volume", font=body, fill=INK)
    d.text((w - 200, y), "80", font=bodyb, fill=INK)
    y += px(BODY, scale) + 6

    # Caption step, candidate face: 16 units, the size 121.8 is still open about.
    d.text((18, y), "Applies to every sound in the game, including the menus.",
           font=cap, fill=SOFT)
    y += px(CAPTION, scale) + 16

    # The bold problem, side by side at the floor. THIS is what 132.8 is about.
    d.text((18, y), "REAL BOLD:", font=capb, fill=SOFT); y += px(CAPTION, scale) + 6
    d.text((18, y), "PRESS START TO HOST A GAME", font=bodyb, fill=INK)
    y += px(BODY, scale) + 12

    # A four-line paragraph at the readable floor: the actual failing case in 133.
    para = ["Slams the ground and knocks every attacker",
            "back three metres. Anyone caught inside the",
            "ring drops the tsinelas they are carrying and",
            "cannot throw again for two seconds."]
    for line in para:
        d.text((18, y), line, font=floor, fill=INK)
        y += px(24, scale)

    return img

for scale, tag in ((WORST, "worst-4x3-1024x768"), (NATIVE, "native-1080p")):
    cols = [panel(n, r, b, scale) for n, r, b in CANDS]
    W = cols[0].width
    sheet = Image.new("RGB", (W, sum(c.height + 8 for c in cols) + 8), (150, 140, 125))
    y = 8
    for c in cols:
        sheet.paste(c, (0, y)); y += c.height + 8
    out = f"scratchpad/fontsrc/bodyface_{tag}_v1.png"
    sheet.save(out)
    print("wrote", out, sheet.size)
