"""Dumps a .glb's mesh vertices, UV atlas cells and bone weights.

    python tools/glb_mesh_dump.py <file.glb>

WHY THIS EXISTS. `Toon.shader` chooses a Person's colour from WHICH 32x32 CELL of
the 512x512 atlas a vertex's UV lands in, so "what colour is the shirt" is a
question about UV coordinates rather than about a texture. Nothing in the editor
shows that, and the Godot generator's own MEASURED_SLOTS table exists because the
first attempt assumed the mapping instead of reading it.

It also reports the authored height, which is the number `CharacterVisual.PersonScale`
multiplies by 2.38 to fill the 1.6 unit capsule. A replacement rig authored to a
different height silently walks around at the wrong size.
"""
import json
import struct
import sys
from collections import Counter, defaultdict

COMPONENT = {5120: ("b", 1), 5121: ("B", 1), 5122: ("h", 2),
             5123: ("H", 2), 5125: ("I", 4), 5126: ("f", 4)}
COUNT = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}


def read_glb(path):
    with open(path, "rb") as handle:
        data = handle.read()

    offset, gltf, buffer = 12, None, None

    while offset < len(data):
        length, kind = struct.unpack_from("<II", data, offset)
        offset += 8
        chunk = data[offset:offset + length]
        offset += length
        if kind == 0x4E4F534A:
            gltf = json.loads(chunk.decode("utf-8"))
        elif kind == 0x004E4942:
            buffer = chunk

    return gltf, buffer


def read_accessor(gltf, buffer, index):
    acc = gltf["accessors"][index]
    fmt, size = COMPONENT[acc["componentType"]]
    n = COUNT[acc["type"]]

    view = gltf["bufferViews"][acc["bufferView"]]
    start = view.get("byteOffset", 0) + acc.get("byteOffset", 0)
    stride = view.get("byteStride") or (size * n)

    out = []
    for i in range(acc["count"]):
        chunk = buffer[start + i * stride: start + i * stride + size * n]
        out.append(struct.unpack("<" + fmt * n, chunk))

    return out


def main():
    path = sys.argv[1]
    gltf, buffer = read_glb(path)

    joint_names = None
    for skin in gltf.get("skins", []):
        joint_names = [gltf["nodes"][j].get("name") for j in skin["joints"]]
        break

    lo = [1e9] * 3
    hi = [-1e9] * 3
    all_slots = Counter()

    for node in gltf["nodes"]:
        if "mesh" not in node:
            continue

        mesh = gltf["meshes"][node["mesh"]]
        print(f"\n== node {node.get('name')} -> mesh {mesh.get('name')}")

        for prim in mesh["primitives"]:
            pos = read_accessor(gltf, buffer, prim["attributes"]["POSITION"])
            uv = read_accessor(gltf, buffer, prim["attributes"]["TEXCOORD_0"])
            joints = read_accessor(gltf, buffer, prim["attributes"]["JOINTS_0"])
            weights = read_accessor(gltf, buffer, prim["attributes"]["WEIGHTS_0"])

            for p in pos:
                for a in range(3):
                    lo[a] = min(lo[a], p[a])
                    hi[a] = max(hi[a], p[a])

            per_bone = defaultdict(Counter)
            cells = Counter()

            for i, (u, v) in enumerate(uv):
                col = min(int(u * 16.0), 15)
                row = min(int(v * 16.0), 15)
                slot = (col // 2) + (8 if row >= 12 else 0) if row >= 8 else None
                cells[(col, row, slot)] += 1
                if slot is not None:
                    all_slots[slot] += 1

                best = max(range(4), key=lambda k: weights[i][k])
                per_bone[joint_names[joints[i][best]]][slot] += 1

            print(f"   verts={len(pos)}  tris={len(read_accessor(gltf, buffer, prim['indices'])) // 3}")
            print("   cells (col,row,slot): " +
                  ", ".join(f"{c}x{n}" for c, n in cells.most_common()))
            print("   slot use by dominant bone:")
            for bone, counts in per_bone.items():
                print(f"      {bone:10s} " +
                      ", ".join(f"slot{s}:{n}" for s, n in sorted(counts.items(),
                                                                 key=lambda kv: -kv[1])))

    print(f"\n== bounds  min={[round(v, 4) for v in lo]}  max={[round(v, 4) for v in hi]}")
    print(f"   height={hi[1] - lo[1]:.4f}  width={hi[0] - lo[0]:.4f}  depth={hi[2] - lo[2]:.4f}")
    print(f"   slots used overall: {sorted(all_slots)}")


if __name__ == "__main__":
    main()
