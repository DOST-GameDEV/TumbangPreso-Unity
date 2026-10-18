"""PAYO land kit, the CALM pass: the kit-together sheet (system Python, Pillow). Per-piece sheets use calm_sheet.py.

  py -3 tools/payo_pieces/terrace_calm_sheet.py [note]

Lays out Logs/payo/pieces/calm/terrace/shots/kit_{old,calm}_{below,40m,150m}.png as
Logs/payo/pieces/calm/terrace/kit_together.png, OLD left, CALM right.
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import calm_sheet  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
C = ROOT / "Logs/payo/pieces/calm/terrace"
S = C / "shots"
rows = [(label, [("OLD (approved)", str(S / ("kit_old_%s.png" % k))), ("CALM", str(S / ("kit_calm_%s.png" % k)))])
        for k, label in (("below", "eye 1.7 m, from below"), ("40m", "40 m"), ("150m", "150 m"))]
calm_sheet.grid(rows, "TERRACE AND LAND KIT together", str(C / "kit_together.png"),
                sys.argv[1] if len(sys.argv) > 1 else "six terrace rows, pit wall, cliff and fall, ground tiles; normal maps on")
