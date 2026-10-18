"""Write Dante's MOUNTAIN FORM clips into his redesign .glb files, in place.

    py -3 tools/author_dante_mountain_clips.py

The clips are `MOUNTAIN_CLIPS` in tools/author_character_redesign_dante_clips.py (read its note). This appends them to
dante-redesign.glb and dante-redesign-spiky.glb, replacing any `mountain-*` clip already there, and leaves every mesh,
bone, bind matrix and other clip byte for byte as it was. It does not need Blender: rebuilding the whole model with
tools/author_character_redesign_dante.py writes the same clips through the same function (`append_clips`).
"""
import os
import struct
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import build_person_voxel as bpv  # noqa: E402
import author_character_redesign_dante_clips as clips  # noqa: E402

ROOT = os.path.dirname(HERE)
FOLDER = os.path.join(ROOT, "Assets/TumbangPreso/Art/CharacterRedesign/dante")
FILES = [os.path.join(FOLDER, "dante-redesign.glb"), os.path.join(FOLDER, "dante-redesign-spiky.glb")]


def append_clips(gltf, blob, table):
    """Add every clip of `table` (name -> function returning {(bone, path): [(t, value)]}) to `gltf`, its data appended
    to `blob` (a bytearray holding the file's binary chunk), replacing any clip of the same name."""
    gltf["animations"] = [a for a in gltf["animations"] if a["name"] not in table]
    node_of = {n.get("name"): i for i, n in enumerate(gltf["nodes"])}

    def add(values, fmt, kind, minmax=False):
        while len(blob) % 4:
            blob.append(0)
        start = len(blob)
        for v in values:
            blob.extend(struct.pack("<" + fmt * len(v), *v))
        acc = {"bufferView": len(gltf["bufferViews"]), "componentType": 5126, "count": len(values), "type": kind}
        if minmax:
            acc["min"] = [min(v[a] for v in values) for a in range(len(values[0]))]
            acc["max"] = [max(v[a] for v in values) for a in range(len(values[0]))]
        gltf["bufferViews"].append({"buffer": 0, "byteOffset": start, "byteLength": len(blob) - start})
        gltf["accessors"].append(acc)
        return len(gltf["accessors"]) - 1

    for name, build in table.items():
        anim = {"name": name, "samplers": [], "channels": []}
        for (bone, path), keys in build().items():
            times = add([(t,) for t, _ in keys], "f", "SCALAR", minmax=True)
            values = add([tuple(v) for _, v in keys], "f", "VEC4" if path == "rotation" else "VEC3")
            anim["channels"].append({"sampler": len(anim["samplers"]), "target": {"node": node_of[bone], "path": path}})
            anim["samplers"].append({"input": times, "output": values, "interpolation": "LINEAR"})
        gltf["animations"].append(anim)
    gltf["buffers"] = [{"byteLength": len(blob)}]


def main():
    for path in FILES:
        gltf, buffer = bpv.read_glb(path)
        blob = bytearray(buffer)
        before = [a["name"] for a in gltf["animations"]]
        append_clips(gltf, blob, clips.MOUNTAIN_CLIPS)
        bpv.write_glb(path, gltf, blob)
        after = [a["name"] for a in gltf["animations"]]
        print("  clips %d -> %d: %s" % (len(before), len(after), ", ".join(n for n in after if n.startswith("mountain-"))))


if __name__ == "__main__":
    main()
