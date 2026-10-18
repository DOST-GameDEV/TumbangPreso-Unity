"""The stone family's CALM pass: proof that a calm repaint kept the approved UV layout.

  python tools/payo_pieces/stone_calm_check.py <piece> [...]

Compares Logs/payo/pieces/calm/stone/parts/<piece>.glb (typed in calm mode) with the approved typed parts in
Logs/payo/pieces/build/stone/parts/<piece>.glb (read only): every part's positions and UVs must be identical, so the
calm atlas lands exactly where the approved welded glb's UVs look. Prints one line per piece; exits 1 on a mismatch.
"""
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import calm_glb as G                                 # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
OLD = ROOT / "Logs/payo/pieces/build/stone/parts"
NEW = ROOT / "Logs/payo/pieces/calm/stone/parts"


def arrays(path):
    j, b = G.read(path)
    out = []
    for m in j["meshes"]:
        for p in m["primitives"]:
            at = p["attributes"]
            out.append((G.accessor(j, b, at["POSITION"]), G.accessor(j, b, at["TEXCOORD_0"])))
    return out


bad = 0
for name in sys.argv[1:]:
    a, b = arrays(OLD / (name + ".glb")), arrays(NEW / (name + ".glb"))
    ok = len(a) == len(b) and all(p0.shape == p1.shape and np.array_equal(p0, p1) and np.allclose(u0, u1, atol=1e-6) for (p0, u0), (p1, u1) in zip(a, b))
    bad += not ok
    print("%-14s %d parts  %s" % (name, len(b), "SAME positions and UVs as approved" if ok else "MISMATCH"))
sys.exit(1 if bad else 0)
