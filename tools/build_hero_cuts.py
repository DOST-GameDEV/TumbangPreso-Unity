"""The avatars' figures with no sunburst, for the profile and character select (owner, 2026-11-03).

Same placement and ink rim as build_avatars.py, on a transparent ground. Writes Resources/UI/hero-cuts/<id>.png.
The avatars themselves are only read.
"""
import os
import build_avatars as avatars
from PIL import Image, ImageFilter

OUT = os.path.join(avatars.ROOT, "Assets", "TumbangPreso", "Resources", "UI", "hero-cuts")


def cut(pid):
    figure = avatars.place(pid, None)
    rim = Image.new("RGBA", figure.size, (28, 15, 6, 255))
    rim.putalpha(figure.split()[3].filter(ImageFilter.MaxFilter(9)))
    rim.alpha_composite(figure)
    return rim.resize((avatars.SIZE, avatars.SIZE), Image.LANCZOS)


def main():
    os.makedirs(OUT, exist_ok=True)
    for pid, _ in avatars.SET:
        if os.path.exists(os.path.join(avatars.PORTRAITS, pid + ".png")):
            cut(pid).save(os.path.join(OUT, pid + ".png"))
            print(pid)


if __name__ == "__main__":
    main()
