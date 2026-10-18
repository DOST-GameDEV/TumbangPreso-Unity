"""Turn a slipper glb half round about its up axis, in its own vertex data (no node transform).

Owner, 2026-10-18, of the remodelled slippers in hand: "the slippers are backwards". `tools/build_slippers.py` laid the
toe along +X, but the game's shoes (every one it replaced: tsinelas_tsinelas.glb's wide forefoot was at -X) carry the toe
along -X, and the hold, the throw and the landed rest are all seated for that. So each built shoe is turned 180 degrees
about Y: x and z of every POSITION, NORMAL and TANGENT are negated (a proper rotation, so the winding and the tangent's
handedness stay), and POSITION's min/max are rewritten. The file is otherwise byte for byte the same.

  py -I tools/slipper_turn.py <in.glb> [<out.glb>]      in place when no out is given
"""
import json
import struct
import sys

import numpy as np


def turn(path_in, path_out):
    b = bytearray(open(path_in, "rb").read())
    n = struct.unpack("<I", b[12:16])[0]
    j = json.loads(bytes(b[20:20 + n]))
    bin_at = 20 + n + 8
    done = set()
    for mesh in j["meshes"]:
        for prim in mesh["primitives"]:
            for attr, width in (("POSITION", 3), ("NORMAL", 3), ("TANGENT", 4)):
                if attr not in prim["attributes"]:
                    continue
                index = prim["attributes"][attr]
                if index in done:
                    continue
                done.add(index)
                acc = j["accessors"][index]
                assert acc["componentType"] == 5126, "%s %s is not float" % (path_in, attr)
                view = j["bufferViews"][acc["bufferView"]]
                stride = view.get("byteStride", 4 * width)
                start = bin_at + view.get("byteOffset", 0) + acc.get("byteOffset", 0)
                for i in range(acc["count"]):
                    at = start + i * stride
                    v = list(struct.unpack_from("<%df" % width, b, at))
                    v[0], v[2] = -v[0], -v[2]
                    struct.pack_into("<%df" % width, b, at, *v)
                if attr == "POSITION" and "min" in acc:
                    lo, hi = acc["min"], acc["max"]
                    acc["min"] = [-hi[0], lo[1], -hi[2]]
                    acc["max"] = [-lo[0], hi[1], -lo[2]]
    text = json.dumps(j, separators=(",", ":")).encode()
    text += b" " * ((4 - len(text) % 4) % 4)
    head = struct.pack("<III", 0x46546C67, 2, 0)
    chunk = struct.pack("<II", len(text), 0x4E4F534A) + text
    rest = bytes(b[20 + n:])
    out = head + chunk + rest
    out = out[:8] + struct.pack("<I", len(out)) + out[12:]
    open(path_out, "wb").write(out)
    print("turned", path_out)


if __name__ == "__main__":
    turn(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else sys.argv[1])
