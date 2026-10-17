"""PAYO sacred pool: finish the glb (system Python). The stone gets the court's atlas, normal map and tangents
(calm_glb.swap), then the water (py_water) has the normal map taken off again: it is the game's water shader's.

  py tools/payo_pieces/pool_finish.py
"""
import json
import os
import struct
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import calm_glb  # noqa: E402

D = os.path.join(os.path.dirname(os.path.dirname(HERE)), "Logs", "payo", "pieces", "calm", "court")
RAW, OUT = os.path.join(D, "pool_basin_raw.glb"), os.path.join(D, "pool_basin.glb")
calm_glb.swap(RAW, os.path.join(D, "court_platform-atlas.png"), OUT, os.path.join(D, "court_platform_normal.png"))
j, b = calm_glb.read(OUT)
for m in j["materials"]:
    if m["name"] == "py_water":
        m.pop("normalTexture", None)
        m["alphaMode"] = "BLEND"
js = calm_glb.pad4(json.dumps(j, separators=(",", ":")).encode(), b" ")
b = calm_glb.pad4(b)
with open(OUT, "wb") as f:
    f.write(struct.pack("<4sII", b"glTF", 2, 12 + 8 + len(js) + 8 + len(b)))
    f.write(struct.pack("<II", len(js), 0x4E4F534A)); f.write(js)
    f.write(struct.pack("<II", len(b), 0x004E4942)); f.write(b)
os.remove(RAW)
print("POOL GLB", OUT, [m["name"] for m in j["materials"]])
