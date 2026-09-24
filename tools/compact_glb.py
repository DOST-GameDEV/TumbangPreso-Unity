"""Drop the accessors and buffer views nothing references any more, and repack the binary.

    python tools/compact_glb.py FILE.glb [FILE.glb ...]

WHY THIS EXISTS: `glb_action.append_action(replace=True)` is append-only ON PURPOSE. It keeps
every original byte at its original offset so that the rest of the rig is untouched by
construction rather than by care (its own header). The price is that a replaced clip's old
samples stay in the file with nothing pointing at them. By 2026-09-24 `team-custom.glb` carried
533 unreferenced accessors out of 1116, about a third of its binary, from every re-author of a
hero cast since the clips were first baked.

WHAT IT GUARANTEES, ASSERTED RATHER THAN INTENDED:

* Every accessor that is still referenced (animation samplers, mesh attributes, indices and
  morph targets, skin matrices) reads back BYTE FOR BYTE the same data after the repack, and
  every image's bytes are the same.
* Nodes, skins (apart from the renumbered matrix index), meshes (apart from renumbered
  accessor indices), materials, textures and the animation list keep their order and names.
* A file with nothing to drop is not rewritten at all.

Run `tools/verify_hero_action.py` on the file afterwards, as after any re-author.
"""
import json
import struct
import sys


def read(path):
    data = open(path, "rb").read()
    magic, version, _ = struct.unpack("<4sII", data[:12])
    assert magic == b"glTF" and version == 2, path
    json_len, json_type = struct.unpack("<II", data[12:20])
    assert json_type == 0x4E4F534A
    gltf = json.loads(data[20:20 + json_len])
    rest = data[20 + json_len:]
    blob = b""
    if rest:
        bin_len, bin_type = struct.unpack("<II", rest[:8])
        assert bin_type == 0x004E4942
        blob = rest[8:8 + bin_len]
    return gltf, blob


def write(path, gltf, blob):
    text = json.dumps(gltf, separators=(",", ":")).encode()
    text += b" " * ((-len(text)) % 4)
    blob = bytes(blob) + b"\0" * ((-len(blob)) % 4)
    total = 12 + 8 + len(text) + 8 + len(blob)
    with open(path, "wb") as f:
        f.write(struct.pack("<4sII", b"glTF", 2, total))
        f.write(struct.pack("<II", len(text), 0x4E4F534A)); f.write(text)
        f.write(struct.pack("<II", len(blob), 0x004E4942)); f.write(blob)


def accessor_refs(gltf):
    """Every place an accessor index lives, as (container, key) pairs to rewrite."""
    for a in gltf.get("animations", []):
        for s in a["samplers"]:
            yield s, "input"
            yield s, "output"
    for m in gltf.get("meshes", []):
        for p in m["primitives"]:
            for k in p["attributes"]:
                yield p["attributes"], k
            if "indices" in p:
                yield p, "indices"
            for t in p.get("targets", []):
                for k in t:
                    yield t, k
    for s in gltf.get("skins", []):
        if "inverseBindMatrices" in s:
            yield s, "inverseBindMatrices"


def view_bytes(gltf, blob, v):
    view = gltf["bufferViews"][v]
    start = view.get("byteOffset", 0)
    return blob[start:start + view["byteLength"]]


def compact(path):
    gltf, blob = read(path)
    for a in gltf["accessors"]:
        assert "sparse" not in a, "sparse accessors are not handled; inspect by hand"
    live = sorted({c[k] for c, k in accessor_refs(gltf)})
    if len(live) == len(gltf["accessors"]):
        return {"file": path, "dropped_accessors": 0}

    before = {i: view_bytes(gltf, blob, gltf["accessors"][i]["bufferView"]) for i in live}
    images_before = [view_bytes(gltf, blob, im["bufferView"]) for im in gltf.get("images", []) if "bufferView" in im]

    views = sorted({gltf["accessors"][i]["bufferView"] for i in live}
                   | {im["bufferView"] for im in gltf.get("images", []) if "bufferView" in im})
    new_blob = bytearray()
    view_map, new_views = {}, []
    for v in views:
        new_blob.extend(b"\0" * ((-len(new_blob)) % 4))
        chunk = view_bytes(gltf, blob, v)
        view = dict(gltf["bufferViews"][v]); view["byteOffset"] = len(new_blob)
        new_blob.extend(chunk)
        view_map[v] = len(new_views); new_views.append(view)

    acc_map = {old: new for new, old in enumerate(live)}
    new_accessors = []
    for old in live:
        a = dict(gltf["accessors"][old]); a["bufferView"] = view_map[a["bufferView"]]
        new_accessors.append(a)
    for c, k in list(accessor_refs(gltf)):
        c[k] = acc_map[c[k]]
    for im in gltf.get("images", []):
        if "bufferView" in im:
            im["bufferView"] = view_map[im["bufferView"]]
    dropped = len(gltf["accessors"]) - len(new_accessors)
    gltf["accessors"], gltf["bufferViews"] = new_accessors, new_views
    gltf["buffers"][0]["byteLength"] = len(new_blob)

    for old, new in acc_map.items():
        assert view_bytes(gltf, new_blob, gltf["accessors"][new]["bufferView"]) == before[old], old
    images_after = [view_bytes(gltf, new_blob, im["bufferView"]) for im in gltf.get("images", []) if "bufferView" in im]
    assert images_after == images_before

    write(path, gltf, new_blob)
    return {"file": path, "dropped_accessors": dropped, "binary_before": len(blob), "binary_after": len(new_blob)}


if __name__ == "__main__":
    for p in sys.argv[1:]:
        print(json.dumps(compact(p)))
