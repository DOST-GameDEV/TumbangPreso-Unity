"""Contact sheets of rendered trailer shots, for reviewing the acting and framing frame by frame.

  py -3 tools/trailer_sheet.py street            # every shot of the street film, 8 frames each
  py -3 tools/trailer_sheet.py street throw kid  # only these shots
  py -3 tools/trailer_sheet.py arena --frames 10

Writes Logs/trailer/review/<film>_<shot>.png (each row is one shot, left to right in time, frame numbers stamped).
"""
import argparse
import os

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAMES = os.path.join(ROOT, "Logs", "trailer", "frames")
REVIEW = os.path.join(ROOT, "Logs", "trailer", "review")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("film")
    ap.add_argument("shots", nargs="*")
    ap.add_argument("--frames", type=int, default=8)
    ap.add_argument("--width", type=int, default=400)
    args = ap.parse_args()
    folder = os.path.join(FRAMES, args.film)
    table = []
    with open(os.path.join(folder, "shots.txt"), encoding="utf-8") as f:
        for line in f:
            name, a, b = line.strip().split("\t")
            table.append((name, int(a), int(b)))
    os.makedirs(REVIEW, exist_ok=True)
    w, h = args.width, args.width * 9 // 16
    for name, a, b in table:
        if args.shots and name not in args.shots:
            continue
        picks = [a + round(i * (b - a) / max(1, args.frames - 1)) for i in range(args.frames)]
        sheet = Image.new("RGB", (w * args.frames, h), (0, 0, 0))
        for k, i in enumerate(picks):
            path = os.path.join(folder, f"frame_{i:04d}.png")
            if not os.path.exists(path):
                continue
            im = Image.open(path).convert("RGB").resize((w, h))
            ImageDraw.Draw(im).text((6, 4), f"{name} {i} ({(i - a) / 24:.2f}s)", fill=(255, 255, 0))
            sheet.paste(im, (k * w, 0))
        out = os.path.join(REVIEW, f"{args.film}_{name}.png")
        sheet.save(out)
        print(out)


if __name__ == "__main__":
    main()
