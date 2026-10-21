"""PAYO market pieces: the review line-up (2026-10-19). Blender 5.0, headless.

  blender -b --python tools/payo_pieces/market_review.py -- <out dir> <label> <a.glb> [<b.glb> ...]

Every glb is stood in a row along x (gaps by its own width), then shot with calm_review.py's stage, light and ink
(its functions are run from its source, not copied): a front three-quarter close view of the whole row, a high view
like the game's look from the court, and one close view per piece. Paths are absolute.
"""
import math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
src = open(os.path.join(HERE, "calm_review.py"), encoding="utf-8").read()
src = src[:src.index('\nif args[0] == "piece"')]
argv = sys.argv
sys.argv = [argv[0], "--", "none"]
ns = {"__name__": "calm_review_lib"}
exec(compile(src, os.path.join(HERE, "calm_review.py"), "exec"), ns)
sys.argv = argv
from mathutils import Vector  # noqa: E402

args = argv[argv.index("--") + 1:]
out, label, glbs = args[0], args[1], args[2:]
os.makedirs(out, exist_ok=True)
sc, cam = ns["stage"]((1400, 700))
x = 0.0
items = []
for g in glbs:
    ms, lo, hi = ns["load"](g)
    w = hi.x - lo.x
    shift = x - lo.x
    for o in ms:
        if o.parent is None:
            o.location.x += shift
    items.append((os.path.basename(g)[:-4], ms, Vector((x + w / 2, (lo.y + hi.y) / 2, 0)), max(w, hi.y - lo.y), hi.z))
    x += w + 0.8
mid = Vector((x / 2, 0, 0))
H = max(i[4] for i in items)
d = Vector((0.35, -1.0, 0)).normalized()
span = x
ns["aim"](cam, mid + d * (span * 0.9 + 3) + Vector((0, 0, H * 0.9 + 1.0)), mid + Vector((0, 0, H * 0.35)), 34)
ns["shoot"](sc, os.path.join(out, "%s_row.png" % label))
ns["aim"](cam, mid + d * (span * 0.8 + 4) + Vector((0, 0, span * 0.6 + 4)), mid, 34)
ns["shoot"](sc, os.path.join(out, "%s_high.png" % label))
if len(items) > 1:
    for name, ms, c, rad, h in items:
        ns["aim"](cam, c + d * (rad * 1.3 + 1.6) + Vector((0, 0, h * 0.8 + 0.6)), c + Vector((0, 0, h * 0.4)), 40)
        ns["shoot"](sc, os.path.join(out, "%s_%s.png" % (label, name)))
print("SHOT", out, label)
