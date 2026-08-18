"""Read `Logs/shots-tone/*.png` against the Godot reference frame, band by band.

Companion to `Assets/TumbangPreso/Tests/PlayMode/ToneSweep.cs`. See that file for why the
grade is solved from a sweep rather than adjusted by eye.
"""
import glob
import os

from PIL import Image

BANDS = {
    "sky": (0.55, 0.06, 0.95, 0.16),
    "buildings": (0.30, 0.38, 0.70, 0.46),
    "road": (0.30, 0.60, 0.70, 0.72),
}

REFERENCE = "Logs/shots-godot/g04-ready.png"


def band(image, rect):
    """The MEDIAN colour of a band, per channel.

    ⚠️ A MEAN IS THE WRONG STATISTIC HERE AND IT COST A ROUND OF TUNING. The road band
    contains painted lane markings at near-white and, in some frames, a chair or a bollard;
    the two builds do not put the camera in exactly the same spot, so the FRACTION of the
    band those cover differs and the mean moves with it. The median reports the surface the
    band is mostly looking at, which is the thing being compared.
    """
    w, h = image.size
    x0, y0, x1, y1 = rect
    px = list(image.crop((int(w * x0), int(h * y0),
                          int(w * x1), int(h * y1))).convert("RGB").getdata())

    out = []
    for i in range(3):
        channel = sorted(p[i] for p in px)
        out.append(channel[len(channel) // 2])

    return tuple(out)


def main():
    reference = Image.open(REFERENCE)
    target = {name: band(reference, rect) for name, rect in BANDS.items()}

    print("target   " + "   ".join(
        f"{k}={target[k][0]:.0f}/{target[k][1]:.0f}/{target[k][2]:.0f}" for k in BANDS))
    print()

    for path in sorted(glob.glob("Logs/shots-tone/*.png")):
        image = Image.open(path)
        cells, worst = [], 0.0

        for name, rect in BANDS.items():
            got = band(image, rect)
            err = max(abs(got[i] - target[name][i]) / max(8.0, target[name][i])
                      for i in range(3))
            worst = max(worst, err)
            cells.append(f"{name}={got[0]:.0f}/{got[1]:.0f}/{got[2]:.0f} ({err * 100:.0f}%)")

        print(f"{os.path.basename(path):24s} " + "  ".join(cells) +
              f"   worst {worst * 100:.1f}%")


if __name__ == "__main__":
    main()
