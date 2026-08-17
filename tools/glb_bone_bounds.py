"""Per-bone vertex bounds and bind matrices for a person rig.

    python tools/glb_bone_bounds.py <file.glb>

WHY THIS EXISTS. A replacement Person has to hang new geometry off the SAME seven
bones the 32 shipped clips key, and a box authored in the wrong place is a limb
that detaches the moment the rig moves. The clips are not editable here, so the
geometry has to be authored to the skeleton rather than the other way round, and
that means knowing where each bone's mass currently sits.
"""
import json
import struct
import sys
from collections import defaultdict

sys.path.insert(0, "tools")
from glb_mesh_dump import read_glb, read_accessor  # noqa: E402


def world_positions(gltf):
    """Bind-pose world translation of every node, walking the scene tree."""
    parent = {}
    for i, node in enumerate(gltf["nodes"]):
        for child in node.get("children", []):
            parent[child] = i

    def walk(i):
        t = gltf["nodes"][i].get("translation", [0.0, 0.0, 0.0])
        if i in parent:
            p = walk(parent[i])
            return [p[a] + t[a] for a in range(3)]
        return list(t)

    return {i: walk(i) for i in range(len(gltf["nodes"]))}


def main():
    path = sys.argv[1]
    gltf, buffer = read_glb(path)
    world = world_positions(gltf)

    names = {i: n.get("name") for i, n in enumerate(gltf["nodes"])}

    print("== bone bind-pose world positions")
    for skin in gltf.get("skins", [])[:1]:
        for j in skin["joints"]:
            print(f"   {names[j]:10s} {[round(v, 5) for v in world[j]]}")

    per_bone = defaultdict(lambda: [[1e9] * 3, [-1e9] * 3, 0])

    for node in gltf["nodes"]:
        if "mesh" not in node:
            continue

        skin = gltf["skins"][node["skin"]]
        joint_names = [names[j] for j in skin["joints"]]

        for prim in gltf["meshes"][node["mesh"]]["primitives"]:
            pos = read_accessor(gltf, buffer, prim["attributes"]["POSITION"])
            joints = read_accessor(gltf, buffer, prim["attributes"]["JOINTS_0"])
            weights = read_accessor(gltf, buffer, prim["attributes"]["WEIGHTS_0"])

            for i, p in enumerate(pos):
                best = max(range(4), key=lambda k: weights[i][k])
                bone = joint_names[joints[i][best]]
                slot = per_bone[bone]
                for a in range(3):
                    slot[0][a] = min(slot[0][a], p[a])
                    slot[1][a] = max(slot[1][a], p[a])
                slot[2] += 1

    print("\n== vertex bounds by dominant bone (model space)")
    for bone, (lo, hi, n) in per_bone.items():
        size = [round(hi[a] - lo[a], 4) for a in range(3)]
        print(f"   {bone:10s} n={n:4d}  min={[round(v, 4) for v in lo]}  "
              f"max={[round(v, 4) for v in hi]}  size={size}")

    print("\n== blend weights: how many verts are rigidly bound to one bone")
    rigid = soft = 0
    for node in gltf["nodes"]:
        if "mesh" not in node:
            continue
        for prim in gltf["meshes"][node["mesh"]]["primitives"]:
            weights = read_accessor(gltf, buffer, prim["attributes"]["WEIGHTS_0"])
            for w in weights:
                if max(w) > 0.999:
                    rigid += 1
                else:
                    soft += 1
    print(f"   rigid={rigid}  blended={soft}")


if __name__ == "__main__":
    main()
