"""Owner-directed plain brown arms; preserve the rest of both Inday models.

The 2026-09-22 instruction supersedes the old coral-guard restoration. Retain
each model's actual simple hands, remove arm-mounted props/guards/sleeves and
join the hands to the existing shoulder joints with plain brown forearms.
Animation accessor bytes, skeleton, non-arm attributes and material identities
are checked before writing. Raw inputs are kept under ArtSource.
"""
from copy import deepcopy
import hashlib
import json
from pathlib import Path

from author_cast_finish import Geometry, animation_digest, cell, slot, store_geometry, write
from glb_mesh_dump import read_accessor, read_glb

ROOT = Path(__file__).resolve().parents[1]
VERSION = "owner-plain-brown-arms-20260922-v2"


def author(filename, hand_slot):
    path = ROOT / "Assets/TumbangPreso/Art/characters/persons" / filename
    g, original = read_glb(path)
    if g.get("extras", {}).get("indayPlainArms") == VERSION:
        return {"model": filename, "state": "already authored"}
    backup = ROOT / "ArtSource/inday/before-plain-arms-20260922" / filename
    backup.parent.mkdir(parents=True, exist_ok=True)
    if not backup.exists():
        backup.write_bytes(path.read_bytes())
    elif backup.read_bytes() != path.read_bytes():
        if not g.get("extras", {}).get("indayPlainArms", "").startswith("owner-plain-brown-arms-20260922-"):
            raise RuntimeError("Existing source backup differs; refuse to replace it")
        g, original = read_glb(backup)
    rig = deepcopy((g["nodes"], g["skins"], g.get("animations"), g.get("materials")))
    before_animation = animation_digest(g, original)
    node = next(n for n in g["nodes"] if n.get("name") == "body-mesh")
    primitive = g["meshes"][node["mesh"]]["primitives"][0]
    data = {key: list(read_accessor(g, original, accessor)) for key, accessor in primitive["attributes"].items()}
    indices = [v[0] for v in read_accessor(g, original, primitive["indices"])]
    names = [g["nodes"][i]["name"] for i in g["skins"][node["skin"]]["joints"]]
    bones = [names.index("arm-left"), names.index("arm-right")]
    owned = {i for i, joints in enumerate(data["JOINTS_0"]) if joints[0] in bones and data["WEIGHTS_0"][i][0] == 1}
    hands = {i for i in owned if slot(data["TEXCOORD_0"][i]) == hand_slot}
    removed = owned - hands
    triangles = [indices[i:i+3] for i in range(0, len(indices), 3)]
    assert all(not any(i in removed for i in t) or all(i in removed for i in t) for t in triangles), "Mixed arm/body triangle"
    kept = [i for i in range(len(data["POSITION"])) if i not in removed]
    remap = {old: new for new, old in enumerate(kept)}
    new_data = {key: [values[i] for i in kept] for key, values in data.items()}
    new_indices = [remap[i] for t in triangles if t[0] not in removed for i in t]
    for old in hands:
        new_data["TEXCOORD_0"][remap[old]] = cell(14)
        if "TEXCOORD_1" in new_data:
            new_data["TEXCOORD_1"][remap[old]] = cell(14)
    geometry = Geometry(new_data, new_indices)
    for bone, sign in zip(bones, (1, -1)):
        points = [data["POSITION"][i] for i in hands if data["JOINTS_0"][i][0] == bone]
        assert len(points) >= 24, "The retained hand is missing"
        # Extend beneath the hand's centre, rather than ending at its angled
        # corner; a tiny bounding-box overlap left a pinched wrist in real FPP.
        wrist = (min(p[0] * sign for p in points) + max(p[0] * sign for p in points)) / 2
        hand_z = (min(p[2] for p in points) + max(p[2] for p in points)) / 2
        shoulder = .0999
        geometry.bevel((sign * (shoulder + wrist) / 2, .288, (hand_z-.01725)/2),
                       (wrist - shoulder, .098, .104), bone, 14, radius=.004)
    # Every old non-arm attribute stays byte-equivalent at the decoded level.
    for old in kept:
        if old in owned:
            continue
        assert all(new_data[key][remap[old]] == values[old] for key, values in data.items())
    blob = bytearray(original)
    store_geometry(g, blob, primitive, new_data, new_indices)
    assert (g["nodes"], g["skins"], g.get("animations"), g.get("materials")) == rig
    assert animation_digest(g, blob) == before_animation
    g.setdefault("extras", {})["indayPlainArms"] = VERSION
    # Keep the generic cast author from rewriting this explicitly selected art.
    g["extras"]["preserveOwnerBackupGeometry"] = True
    write(path, g, blob)
    result, result_bytes = read_glb(path)
    assert animation_digest(result, result_bytes) == before_animation
    return {"model": filename, "removed_vertices": len(removed), "retained_hand_vertices": len(hands),
            "animation_sha256": before_animation, "animations": len(g.get("animations", [])),
            "before_sha256": hashlib.sha256(backup.read_bytes()).hexdigest(),
            "after_sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


if __name__ == "__main__":
    report = [author("character-female-a.glb", 14), author("team-inday.glb", 15)]
    destination = ROOT / "docs/reports/full-backlog-2026-09-21/inday-plain-arms.json"
    if any(row.get("state") != "already authored" for row in report):
        destination.write_text(json.dumps(report, indent=2) + "\n", encoding="utf8")
    print(json.dumps(report, indent=2))
