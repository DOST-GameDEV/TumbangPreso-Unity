"""Retain Phaister's existing cast poses and retime only their named GLB action.

All mesh/skin/material data and other animations stay byte-addressable unchanged.
Only new time accessors are appended; output pose values are never regenerated.
"""
import copy
import hashlib
import json
from pathlib import Path
import struct

from glb_mesh_dump import read_glb, read_accessor
from build_person_voxel import write_glb

ROOT = Path(__file__).resolve().parents[1]
ACTION = "hero-phaister-eclipse"
OLD = (0, .04, .08, .115, .14, .18, .38, .85)
NEW = (0, .20, .50, .82, 1.22, 1.55, 1.74, 2.12)
FILES = ("team-phaister.glb", "team-custom-base.glb", "team-custom.glb")


def remap(value):
    for index in range(len(OLD) - 1):
        if value <= OLD[index + 1] + .000001:
            fraction = (value - OLD[index]) / (OLD[index + 1] - OLD[index])
            return NEW[index] + fraction * (NEW[index + 1] - NEW[index])
    raise ValueError("Unexpected input time " + str(value))


def retime(path):
    original, original_blob = read_glb(path)
    doc = copy.deepcopy(original); blob = bytearray(original_blob)
    indices = [i for i, animation in enumerate(doc["animations"]) if animation.get("name") == ACTION]
    assert len(indices) == 1, path
    index = indices[0]; action = doc["animations"][index]
    inputs = {sample["input"] for sample in action["samplers"]}
    end = max(read_accessor(doc, blob, accessor)[-1][0] for accessor in inputs)
    if abs(end - NEW[-1]) < .0001:
        return {"file": path.name, "already_retimed": True, "seconds": end}
    assert abs(end - OLD[-1]) < .0001, (path, end)
    assert all(sample.get("interpolation", "LINEAR") == "LINEAR" for sample in action["samplers"])
    replacement = {}
    for source in sorted(inputs):
        values = [row[0] for row in read_accessor(doc, blob, source)]
        times = [remap(value) for value in values]
        assert all(b > a for a, b in zip(times, times[1:]))
        blob.extend(b"\0" * (-len(blob) % 4)); start = len(blob)
        blob.extend(struct.pack("<" + str(len(times)) + "f", *times))
        view = len(doc["bufferViews"])
        doc["bufferViews"].append({"buffer": 0, "byteOffset": start, "byteLength": len(times) * 4})
        replacement[source] = len(doc["accessors"])
        doc["accessors"].append({"bufferView": view, "componentType": 5126, "count": len(times),
                                 "type": "SCALAR", "min": [min(times)], "max": [max(times)]})
    for sample in action["samplers"]:
        sample["input"] = replacement[sample["input"]]
    doc["buffers"][0]["byteLength"] = len(blob)
    for key in ("nodes", "meshes", "skins", "materials", "textures", "images", "samplers"):
        assert doc.get(key) == original.get(key), key
    for i, animation in enumerate(doc["animations"]):
        if i != index:
            assert animation == original["animations"][i]
    assert bytes(blob[:len(original_blob)]) == original_blob
    write_glb(str(path), doc, blob)
    check, check_blob = read_glb(path)
    assert check_blob[:len(original_blob)] == original_blob
    assert check["animations"][index]["channels"] == original["animations"][index]["channels"]
    return {"file": path.name, "seconds_before": end, "seconds_after": NEW[-1],
            "original_binary_sha256": hashlib.sha256(original_blob).hexdigest(),
            "preserved_original_bytes": len(original_blob), "changed_action": ACTION,
            "unchanged_other_animations": len(original["animations"]) - 1}


if __name__ == "__main__":
    report = [retime(ROOT / "Assets/TumbangPreso/Art/characters/persons" / name) for name in FILES]
    target = ROOT / "Logs/phaister-ritual-retiming.json"
    target.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))
