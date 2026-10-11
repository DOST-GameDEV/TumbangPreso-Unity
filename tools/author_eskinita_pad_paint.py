"""The bounce tarp's ONE texture: the Ilalim pavement pad's own paint on the left half (its rising frames and chevrons
keep their painted yellow and cream), a 2 m by 2 m piece of the striped trapal on the right half (the sheet). JumpPad
gives a pad one texture for all its parts, so the two are put side by side. Run: py -3 tools/author_eskinita_pad_paint.py
Owner, 2026-10-09, of chevrons taken from Ilalim but painted one flat colour: "too high, and untextured"."""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
TEX = ROOT / "Assets/TumbangPreso/Art/EskinitaAlley/Textures"
pad = Image.open(ROOT / "Assets/TumbangPreso/Resources/Map/JumpPad/jump_pad_paint.png").convert("RGB").resize((1024, 1024), Image.LANCZOS)
for colour in ("red", "teal", "yellow"):
    t = Image.open(TEX / ("trapal_%s_albedo.png" % colour)).convert("RGB").resize((512, 512), Image.LANCZOS)
    out = Image.new("RGB", (2048, 1024))
    out.paste(pad, (0, 0))
    for i in range(2):
        for j in range(2):
            out.paste(t, (1024 + i * 512, j * 512))
    out.save(TEX / ("pad_trapal_%s_albedo.png" % colour))
    print("wrote pad_trapal_%s_albedo.png" % colour)
