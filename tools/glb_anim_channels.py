"""What every clip actually keys, per bone and per path.

    python tools/glb_anim_channels.py <file.glb>

WHY THIS EXISTS. The question it answers is whether the SKELETON is editable. A
replacement Person is authored to the base rig's bone positions because the 32
shipped clips key those bones, but that constraint is only as wide as what the
clips actually write: a clip that keys rotation alone leaves a bone's rest
translation intact, and rest translations are the proportions. So before deciding
that the chibi head is unavoidable, ask which channels exist and whether their
translation tracks are anything other than the rest pose repeated.
"""
import sys
from collections import defaultdict

sys.path.insert(0, "tools")
from glb_mesh_dump import read_glb, read_accessor  # noqa: E402


def main():
    path = sys.argv[1]
    gltf, buffer = read_glb(path)

    names = {i: n.get("name") for i, n in enumerate(gltf["nodes"])}
    rest = {i: n.get("translation", [0.0, 0.0, 0.0]) for i, n in enumerate(gltf["nodes"])}

    per_path = defaultdict(int)
    moving = defaultdict(list)

    for anim in gltf["animations"]:
        for channel in anim["channels"]:
            target = channel["target"]
            node = target["node"]
            kind = target["path"]

            per_path[kind] += 1

            if kind != "translation":
                continue

            sampler = anim["samplers"][channel["sampler"]]
            values = read_accessor(gltf, buffer, sampler["output"])

            drift = max(
                max(abs(v[a] - rest[node][a]) for a in range(3)) for v in values)

            if drift > 0.0005:
                moving[names[node]].append((anim.get("name"), round(drift, 5)))

    print("channels by path:", dict(per_path))
    print()
    print("translation tracks that differ from the node's rest translation:")

    if not moving:
        print("   NONE. Every translation track in every clip is the rest pose held.")
        print("   The bone REST TRANSLATIONS are therefore free: move one and rewrite")
        print("   the matching constant tracks and no clip can contradict it.")
        return

    for bone, hits in moving.items():
        print(f"   {bone}: {hits}")


if __name__ == "__main__":
    main()
