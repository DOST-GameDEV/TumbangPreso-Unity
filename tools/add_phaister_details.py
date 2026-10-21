"""Adds Phaister's witch details (v26: the hip manika, three hat pins, two moths) INTO the shipped team-phaister.glb.

    python tools/add_phaister_details.py            # apply
    python tools/add_phaister_details.py --check    # report only

WHY (owner, 2026-09-27, on her overhaul: *"u can refine a bit i dont mind"*; `docs/reports/phaister-kit-2026-09-27/plan.md`
section 5). The parts are typed by hand in `tools/build_phaister_voxel.py` (rows named `manika-`, `hatpin-`, `moth-`), which
stays the one source of her geometry.

⚠️⚠️ SURGERY, NOT A REBUILD. The shipped glb carries work appended after the builder last ran: her three hero cast clips
(`tools/author_hero_action.py`), the grounded gaits and slide, and the shortened arms (`tools/reshape_hero_arms.py`, which
refuses a second run). Re-running the builder drops all of it: the first try on 2026-09-27 came back with 33 clips instead
of 36 and her long arms back. So this builds ONLY the new rows with the builder's own `build_mesh` and appends them to the
end of the body and head primitives (same joints, same material, same atlas). Every other accessor is copied byte for byte.

⚠️ IDEMPOTENT BY REFUSAL: the glb's `asset.extras.phaisterDetails` records the version applied; a second run refuses.
"""
import importlib.util
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
GLB = ROOT / "Assets/TumbangPreso/Art/characters/persons/team-phaister.glb"
VERSION = "v26"
PREFIXES = ("manika-", "hatpin-", "moth-")


def load_builder():
    spec = importlib.util.spec_from_file_location("phaister_builder", ROOT / "tools/build_phaister_voxel.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    check = "--check" in sys.argv[1:]
    b = load_builder()
    gltf, buffer = b.read_glb(str(GLB))

    extras = gltf.setdefault("asset", {}).setdefault("extras", {})
    if extras.get("phaisterDetails"):
        raise SystemExit(f"already applied ({extras['phaisterDetails']}); refusing to add the details twice")

    body_rows = [r for r in b.BODY_BOXES if r[0].startswith(PREFIXES)]
    head_rows = [r for r in b.HEAD_BOXES if r[0].startswith(PREFIXES)]
    if not body_rows or not head_rows:
        raise SystemExit("no detail rows found in the builder")
    joints = [gltf["nodes"][i]["name"] for i in gltf["skins"][0]["joints"]]
    if joints != ["root", "leg-left", "leg-right", "torso", "arm-left", "arm-right", "head"]:
        raise SystemExit(f"unexpected joint order {joints}")

    added = {"body-mesh": b.build_mesh(body_rows), "head-mesh": b.build_mesh(head_rows)}
    print(f"body rows {len(body_rows)}, head rows {len(head_rows)}")
    for name, built in added.items():
        print(f"  {name}: +{len(built[0])} vertices, +{len(built[5]) // 3} triangles")
    if check:
        return 0

    blob = bytearray()
    views, accessors, remap = [], [], {}

    def align():
        while len(blob) % 4:
            blob.append(0)

    def keep(old):
        if old in remap:
            return remap[old]
        acc = dict(gltf["accessors"][old])
        data = b.accessor_bytes(gltf, buffer, old)
        align()
        acc["bufferView"] = len(views)
        acc.pop("byteOffset", None)
        views.append({"buffer": 0, "byteOffset": len(blob), "byteLength": len(data)})
        blob.extend(data)
        remap[old] = len(accessors)
        accessors.append(acc)
        return remap[old]

    def add(values, fmt, kind, component, minmax=False):
        align()
        start = len(blob)
        for v in values:
            blob.extend(struct.pack("<" + fmt * len(v), *v))
        acc = {"bufferView": len(views), "componentType": component, "count": len(values), "type": kind}
        if minmax:
            n = len(values[0])
            acc["min"] = [min(v[a] for v in values) for a in range(n)]
            acc["max"] = [max(v[a] for v in values) for a in range(n)]
        views.append({"buffer": 0, "byteOffset": start, "byteLength": len(blob) - start})
        accessors.append(acc)
        return len(accessors) - 1

    # Everything that is not a mesh attribute keeps its bytes: skins, every animation sampler.
    for skin in gltf["skins"]:
        skin["inverseBindMatrices"] = keep(skin["inverseBindMatrices"])
    for anim in gltf["animations"]:
        for sampler in anim["samplers"]:
            sampler["input"] = keep(sampler["input"])
            sampler["output"] = keep(sampler["output"])

    for mesh in gltf["meshes"]:
        if len(mesh["primitives"]) != 1:
            raise SystemExit(f"{mesh['name']} has {len(mesh['primitives'])} primitives; expected one")
        prim = mesh["primitives"][0]
        attrs = prim["attributes"]
        old = {k: b.read_accessor(gltf, buffer, v) for k, v in attrs.items()}
        old_idx = b.read_accessor(gltf, buffer, prim["indices"])
        base = len(old["POSITION"])
        pos, nrm, uv, jts, wts, idx = added[mesh["name"]]
        prim["attributes"] = {
            "POSITION": add(old["POSITION"] + [tuple(p) for p in pos], "f", "VEC3", 5126, minmax=True),
            "NORMAL": add(old["NORMAL"] + [tuple(n) for n in nrm], "f", "VEC3", 5126),
            "TEXCOORD_0": add(old["TEXCOORD_0"] + [tuple(t) for t in uv], "f", "VEC2", 5126),
            "JOINTS_0": add(old["JOINTS_0"] + [tuple(j) for j in jts], "H", "VEC4", 5123),
            "WEIGHTS_0": add(old["WEIGHTS_0"] + [tuple(w) for w in wts], "f", "VEC4", 5126),
        }
        prim["indices"] = add([(i,) for i in old_idx] + [(base + i,) for i in idx], "I", "SCALAR", 5125)

    gltf["accessors"] = accessors
    gltf["bufferViews"] = views
    gltf["buffers"] = [{"byteLength": len(blob)}]
    extras["phaisterDetails"] = VERSION
    b.write_glb(str(GLB), gltf, blob)
    return 0


if __name__ == "__main__":
    sys.exit(main())
