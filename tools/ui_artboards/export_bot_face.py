"""The seat card's bot: antennae and a big toothy grin, white, tinted in the game. Writes Resources/UI/boot-kit/bot-face.png."""
import math
from pathlib import Path
from PIL import Image, ImageDraw

SS, N = 4, 512
WHITE = (255, 255, 255)
OUT = Path(__file__).resolve().parents[2] / "Assets/TumbangPreso/Resources/UI/boot-kit/bot-face.png"


def main():
    m = Image.new("L", (N * SS, N * SS), 0)
    d = ImageDraw.Draw(m)

    def rr(box, r, fill=255):
        d.rounded_rectangle([v * SS for v in box], r * SS, fill=fill)

    def el(box, fill=255):
        d.ellipse([v * SS for v in box], fill=fill)

    def stalk(a, b, w=20):
        d.line([(a[0] * SS, a[1] * SS), (b[0] * SS, b[1] * SS)], fill=255, width=w * SS)

    stalk((190, 120), (150, 58))
    el((124, 30, 176, 82))
    stalk((322, 120), (362, 58))
    el((336, 30, 388, 82))
    rr((70, 110, 442, 420), 84)
    rr((30, 214, 78, 324), 20)
    rr((434, 214, 482, 324), 20)
    el((136, 170, 226, 260), 0)
    el((286, 170, 376, 260), 0)
    el((172, 190, 212, 230))
    el((322, 190, 362, 230))
    rr((136, 296, 376, 376), 30, 0)
    for x in (184, 232, 280, 328):
        rr((x - 5, 296, x + 5, 376), 0)
    rr((136, 332, 376, 340), 0)

    alpha = m.resize((N, N), Image.LANCZOS)
    art = Image.new("RGBA", (N, N), WHITE + (0,))
    art.putalpha(alpha)
    art.save(OUT)


if __name__ == "__main__":
    main()
