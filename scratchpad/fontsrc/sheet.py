# Each recolour on the ground it is actually meant to be seen on. CLAUDE.md 6.2b row 2:
# a shot over a blank scene is a shot of a different screen.
from PIL import Image

B = "Assets/TumbangPreso/Art/ui/brand"
HONEY=(0xFC,0xD3,0x9F); ARMY=(0xB3,0xA8,0x28); CHART=(0xD6,0xCE,0x01)
KHAKI=(0xE6,0xC9,0x92); STAGE=(0x3A,0x36,0x14)

rows = [
    ("tump_logo.png",           HONEY, "the colour master, keyed"),
    ("tump_wordmark_login.png", HONEY, "LOGIN: red line, honey fill"),
    ("tump_wordmark_lobby.png", HONEY, "LOBBY: red line, chartreuse fill"),
    ("tump_wordmark_stage.png", STAGE, "CHAR SELECT: honey line, army fill, dark ground"),
    ("tump_wordmark_ink.png",   KHAKI, "QUIET: no texture tint"),
    ("tsinelas_hit.png",        HONEY, "the tsinelas mark"),
]

W, H = 900, 260
sheet = Image.new("RGB", (W, H*len(rows)), (30,26,20))
y = 0
for name, ground, _ in rows:
    band = Image.new("RGB", (W, H), ground)
    art = Image.open(f"{B}/{name}").convert("RGBA")
    s = min((W-80)/art.width, (H-50)/art.height)
    art = art.resize((int(art.width*s), int(art.height*s)), Image.LANCZOS)
    band.paste(art, ((W-art.width)//2, (H-art.height)//2), art)
    sheet.paste(band, (0, y)); y += H
sheet.save("scratchpad/brandsheet_v1.png")
print("scratchpad/brandsheet_v1.png", sheet.size)
