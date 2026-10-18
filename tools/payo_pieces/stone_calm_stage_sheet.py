"""The stone family's CALM stage sheet: the approved stage next to the calm one, from the shots of
tools/payo_pieces/stone_stage_review.py --calm.

  python tools/payo_pieces/stone_calm_stage_sheet.py <shots dir> <out.png> [note]
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import calm_sheet                                    # noqa: E402

shots, out = sys.argv[1], sys.argv[2]
note = sys.argv[3] if len(sys.argv) > 3 else "layout 0 (Liwasan), stone pieces only, same light and cameras; calm has its normal maps on"
views = [("eye_north", "1.7 m on the core, north"), ("eye_east", "1.7 m on the core, east"), ("outer", "1.7 m on the NE tower, looking back"), ("above", "from above")]
rows = [(cap, [("OLD (approved)", "%s/stage_old_%s.png" % (shots, v)), ("CALM", "%s/stage_calm_%s.png" % (shots, v))]) for v, cap in views]
calm_sheet.grid(rows, "STONE STAGE  old vs calm", out, note)
