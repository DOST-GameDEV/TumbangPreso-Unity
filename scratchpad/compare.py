import sys
from PIL import Image, ImageDraw, ImageFont

DAR = "Assets/TumbangPreso/Art/ui/fonts/DarumadropOne-Regular.ttf"

def build(before, after, out, label_a="BEFORE  (rejected)", label_b="AFTER"):
    a = Image.open(before).convert("RGB")
    b = Image.open(after).convert("RGB")
    w = 1280
    a = a.resize((w, round(a.height * w / a.width)), Image.LANCZOS)
    b = b.resize((w, round(b.height * w / b.width)), Image.LANCZOS)
    bar = 44
    img = Image.new("RGB", (w, bar + a.height + bar + b.height), (29, 14, 6))
    d = ImageDraw.Draw(img)
    f = ImageFont.truetype(DAR, 26)
    d.text((16, 9), label_a, font=f, fill=(252, 211, 159))
    img.paste(a, (0, bar))
    d.text((16, bar + a.height + 9), label_b, font=f, fill=(214, 206, 1))
    img.paste(b, (0, bar + a.height + bar))
    img.save(out)
    print(out, img.size)

build(sys.argv[1], sys.argv[2], sys.argv[3])
