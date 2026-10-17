"""The calm guardians and props with the pilot's three calm pieces, one group: writes the layout for
tools/payo_pieces/prop_calm_review.py (group mode) and, after the renders, the sheet.

  python tools/payo_pieces/prop_calm_group.py layout <out.json>
  python tools/payo_pieces/prop_calm_group.py sheet <shots dir> <out.png> [note]

The pilot's three stand at the back as in pilot_together.png, the seven guardians in front of them, the 24 props in
three rows nearest the camera. OLD is the approved glb of each, CALM the calm one, side by side at 40 m and 150 m.
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / "ArtSource/payo/pieces-approved-2026-10-12"
CALM = ROOT / "Logs/payo/pieces/calm"


def item(old, calm, x, y, yaw=0.0, lift=0.0):
    return {"old": str(old), "calm": str(calm), "x": x, "y": y, "yaw": yaw, "lift": lift}


def layout():
    it = [item(APP / "house/house_tower.glb", CALM / "house/house_tower.glb", -24, 8),
          item(APP / "gate/gate_portal.glb", CALM / "gate/gate_portal.glb", 0, 10),
          item(APP / "stone/monolith.glb", CALM / "stone/monolith.glb", 20, 8, lift=13.7)]
    for name, x, y in (("bulul_post", -31, -2), ("bulul_seated", -13, -1), ("bulul_lizard", -6, -5), ("bulul_small_m", 3, -6),
                       ("bulul_small_f", 5, -6), ("bulul_standing", 11, -1), ("bulul_spout", 30, -2)):
        it.append(item(APP / "bulul" / (name + ".glb"), CALM / "bulul" / (name + ".glb"), x, y))
    props = []
    for L in "abcd":
        props += [p for p in json.load(open(APP / ("prop/parts/set%s.json" % L)))["props"]]
    props.sort(key=lambda p: -max(p["size"]))
    rows = (props[:8], props[8:16], props[16:])
    for r, row in enumerate(rows):
        for k, p in enumerate(row):
            x = -20 + k * (40.0 / (len(row) - 1)) + 2.0 * r
            y = -10 - r * 4.5
            it.append(item(APP / "prop" / (p["name"] + ".glb"), CALM / "prop" / (p["name"] + ".glb"), x, y, p.get("yaw", 0.0), p.get("lift", 0.0)))
    return {"prefix": "family", "res": [1400, 700], "look": [0, -4, 4.0], "dists": [[40, 9, 58], [150, 24, 30]], "items": it}


if __name__ == "__main__":
    a = sys.argv[1:]
    if a[0] == "layout":
        json.dump(layout(), open(a[1], "w"), indent=1)
        print("LAYOUT", a[1])
    else:
        from calm_sheet import grid
        shots, out = a[1], a[2]
        note = a[3] if len(a) > 3 else ""
        rows = [(d, [("OLD (approved)", "%s/family_old_%s.png" % (shots, d)), ("CALM", "%s/family_calm_%s.png" % (shots, d))]) for d in ("40m", "150m")]
        grid(rows, "CALM: guardians and props with the pilot", out, note)
