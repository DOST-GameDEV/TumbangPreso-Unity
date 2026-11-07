"""Exports the round break and halftime pictures from the drawing functions in boot_record.py into
Assets/TumbangPreso/Resources/UI/break, so the game draws the pictures the boards were drawn with.

    py -3 tools/ui_artboards/export_break_art.py

The stopwatch is exported with its berry track and no lime: the game draws the draining arc over it.
"""
import hashlib
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import boot_record as br  # noqa: E402
from bootkit import sunburst_art, hazard_art  # noqa: E402

UI = br.UI

OUT = UI / "break"
META = (UI / "boot-kit" / "thin-go.png.meta").read_text(encoding="utf-8")


def save(name, im):
    path = OUT / (name + ".png")
    path.parent.mkdir(parents=True, exist_ok=True)
    im.save(path)
    meta = path.with_name(path.name + ".meta")
    if not meta.exists():
        guid = hashlib.md5(("break/" + name).encode()).hexdigest()
        lines = META.splitlines()
        lines[1] = "guid: " + guid
        meta.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(name, im.size)


def main():
    # The next taya's burst and the countdown's berry burst, at the card sizes `round_break` frames them.
    save("burst-taya", sunburst_art(880, 450, centre=(312, 262)))
    save("burst-clock", sunburst_art(880, 380, c1=(0xA8, 0x34, 0x48), c2=(0x6A, 0x1B, 0x2C), centre=(440, 190)))
    # The stopwatch, 300 wide: dial centre (150, 194), track outer radius 124, 20 thick.
    save("stopwatch", br.clock_art(300, 0))
    save("hazard-tape", hazard_art(240, 30))


if __name__ == "__main__":
    main()
