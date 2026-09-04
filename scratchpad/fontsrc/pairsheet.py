import os
from PIL import Image, ImageDraw, ImageFont

SRC = "scratchpad/fontsrc"
DARUMA = "Assets/TumbangPreso/Art/ui/fonts/DarumadropOne-Regular.ttf"
OUT = "Logs/ui/font-pairing-v1.png"
os.makedirs("Logs/ui", exist_ok=True)

PAPER=(254,235,212); INK=(85,41,15); SOFT=(151,73,27); RED=(152,7,21); CHART=(214,206,1)

CANDS = [
    ("Nunito  (current)",   f"{SRC}/Nunito-Regular.ttf",          f"{SRC}/Nunito-Bold.ttf"),
    ("Familjen Grotesk",    f"{SRC}/FamiljenGrotesk-Regular.ttf", f"{SRC}/FamiljenGrotesk-Bold.ttf"),
    ("Archivo",             f"{SRC}/Archivo-Regular.ttf",         f"{SRC}/Archivo-Bold.ttf"),
    ("Work Sans",           f"{SRC}/WorkSans-Regular.ttf",        f"{SRC}/WorkSans-Bold.ttf"),
    ("Hanken Grotesk",      f"{SRC}/HankenGrotesk-Regular.ttf",   f"{SRC}/HankenGrotesk-Bold.ttf"),
]

W = 1500
ROW = 250
H = 90 + ROW*len(CANDS)
img = Image.new("RGB", (W, H), PAPER)
d = ImageDraw.Draw(img)

# Darumadrop at the sizes the game draws it: Title 26 and Body 20 -> at 2x for the sheet
dar_title = ImageFont.truetype(DARUMA, 52)
dar_body  = ImageFont.truetype(DARUMA, 40)
head = ImageFont.truetype(DARUMA, 34)

d.text((40, 28), "THE SUB FONT, BESIDE DARUMADROP, AT THE SIZES THE GAME DRAWS", font=head, fill=RED)

y = 96
for name, reg, bold in CANDS:
    if not os.path.exists(reg): continue
    r32 = ImageFont.truetype(reg, 32)   # Caption 16 at 2x
    b32 = ImageFont.truetype(bold, 32)
    r26 = ImageFont.truetype(reg, 26)

    d.line([(40,y-14),(W-40,y-14)], fill=(220,186,140), width=2)
    d.text((40, y), name.upper(), font=ImageFont.truetype(bold, 22), fill=SOFT)

    # the real pairing: a Darumadrop title with a sub line under it
    d.text((40, y+30), "SEISMIC STOMP", font=dar_title, fill=INK)
    d.text((44, y+92), "A 2.2 m shock at your feet that launches whoever is standing in it.",
           font=r32, fill=SOFT)
    d.text((44, y+130), "COOLDOWN  ·  2 USES", font=b32, fill=RED)

    # right column: the caps/lowercase proportion beside Darumadrop's
    d.text((900, y+30), "Master volume", font=dar_body, fill=INK)
    d.text((900, y+74), "Master volume", font=r32, fill=INK)
    d.text((900, y+112), "Hxpq  HANDLE  0123", font=r26, fill=SOFT)
    d.text((900, y+150), "◀ ▶ ✓ ← ñ á ×", font=r26, fill=RED)
    y += ROW

img.save(OUT)
print("wrote", OUT, img.size)
