"""Measure this build's frame against the Godot reference frame it is copying.

⚠️⚠️ THE COLOUR PASSES IN THIS PORT KEPT BEING JUDGED BY EYE AND KEPT BEING WRONG. Four
separate "everything is too light" reports, three attempted fixes, and the last one
overshot into "everything is too dark" because there was no number anywhere in the loop.
🧑 2026-08-18: *"u keep comparing it urself until it matches"*.

`Logs/shots-godot/` holds frames captured out of the running Godot build. `g04-ready` is
the free-roam window from the player's own eyes and `Logs/shots-play/ready-eyes.png` is
the same phase from the same camera in this one, which the shot harness says in its own
comment. So the two are directly comparable and the difference is a number.

⚠️ IT SAMPLES BANDS, NOT THE WHOLE FRAME. A full-frame mean is dominated by the HUD, which
is identical in both builds and hides everything underneath it: the two frames agreed to
within 13% on the full-frame mean while the ROAD was out by a factor of 2.6.

    python tools/compare_tone.py
"""
import sys

from PIL import Image

# Fractions of width and height. Chosen to sit on one material each, clear of the HUD
# panels in both builds.
BANDS = {
    # Clear sky, above the rooftops and to the right of the scoreboard card.
    "sky": (0.55, 0.06, 0.95, 0.16),
    # The far end of the street: rooftops and facades, no road and no HUD.
    "buildings": (0.30, 0.38, 0.70, 0.46),
    # Open asphalt in front of the camera, below the crowd and above the objective line.
    "road": (0.30, 0.60, 0.70, 0.72),
}

PAIRS = [
    ("Logs/shots-godot/g04-ready.png", "Logs/shots-play/ready-eyes.png", "ready / free-roam"),
    ("Logs/shots-godot/g06-round-live.png", "Logs/shots-play/round-eyes.png", "round, live"),
]


def band(image, rect):
    w, h = image.size
    x0, y0, x1, y1 = rect
    crop = image.crop((int(w * x0), int(h * y0), int(w * x1), int(h * y1)))

    px = list(crop.convert("RGB").getdata())
    n = len(px)

    return (sum(p[0] for p in px) / n,
            sum(p[1] for p in px) / n,
            sum(p[2] for p in px) / n)


def main():
    worst = 0.0

    for reference, ours, label in PAIRS:
        try:
            a = Image.open(reference)
            b = Image.open(ours)
        except FileNotFoundError as missing:
            print(f"-- {label}: {missing.filename} not captured yet")
            continue

        print(f"\n== {label}")
        print(f"{'band':11s} {'godot':>18s} {'unity':>18s} {'delta':>18s}   err")

        for name, rect in BANDS.items():
            g = band(a, rect)
            u = band(b, rect)
            d = tuple(u[i] - g[i] for i in range(3))

            # Relative to the reference channel, so a 20-count miss on a dark road counts
            # for more than a 20-count miss on a bright sky. That is also how it reads.
            err = max(abs(d[i]) / max(8.0, g[i]) for i in range(3))
            worst = max(worst, err)

            print(f"{name:11s} "
                  f"{g[0]:5.1f} {g[1]:5.1f} {g[2]:5.1f}   "
                  f"{u[0]:5.1f} {u[1]:5.1f} {u[2]:5.1f}   "
                  f"{d[0]:+5.1f} {d[1]:+5.1f} {d[2]:+5.1f}   {err * 100:4.1f}%")

    print(f"\nworst band error {worst * 100:.1f}%")

    # 12% per channel on a band is about where a side by side stops reading as two
    # different grades and starts reading as two screenshots of the same game.
    return 0 if worst <= 0.12 else 1


if __name__ == "__main__":
    sys.exit(main())
