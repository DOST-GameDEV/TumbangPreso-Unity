"""Shows sprites on dark, mid and light-warm grounds, zoomed, for an eye check of edges."""
import sys
from PIL import Image
files, out = sys.argv[1:-1], sys.argv[-1]
grounds = [(12, 6, 8), (70, 90, 110), (240, 185, 71)]
ims = [Image.open(f).convert('RGBA') for f in files]
z = 1
W = sum(i.width for i in ims) * z + 10 * len(ims); H = max(i.height for i in ims) * z
sheet = Image.new('RGBA', (W, H * 3 + 20), (0, 0, 0, 255))
for r, g in enumerate(grounds):
    x = 0
    for im in ims:
        bg = Image.new('RGBA', (im.width * z, im.height * z), g + (255,))
        bg.alpha_composite(im.resize((im.width * z, im.height * z), Image.LANCZOS))
        sheet.paste(bg, (x, r * (H + 10))); x += im.width * z + 10
sheet.save(out)
