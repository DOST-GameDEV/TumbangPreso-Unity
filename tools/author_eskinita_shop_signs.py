"""Letter the two existing Eskinita shop boards with the project's own font."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Assets/TumbangPreso/Art/EskinitaShopSigns"
FONT = ROOT / "Assets/TumbangPreso/Resources/UI/fonts/DarumadropOne-Regular.ttf"

def main():
    OUTPUT.mkdir(exist_ok=True)
    font = ImageFont.truetype(str(FONT), 72)
    for name, label, background in [
        ("WestSign", "LITA'S STORE", (201, 163, 86)),
        ("EastSign", "SARI-SARI", (229, 207, 159)),
    ]:
        image = Image.new("RGB", (664, 80), background)
        draw = ImageDraw.Draw(image)
        left, top, right, bottom = draw.textbbox((0, 0), label, font=font)
        draw.text(((664-right+left)/2-left, (80-bottom+top)/2-top),
                  label, font=font, fill=(48, 31, 20))
        image.save(OUTPUT / (name + ".png"))

if __name__ == "__main__":
    main()
