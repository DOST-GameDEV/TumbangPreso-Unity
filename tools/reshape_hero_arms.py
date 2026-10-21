"""Shortens a hero's arms, and can move the shoulders out, inside the shipped `.glb`, one hero at a time.

    python tools/reshape_hero_arms.py cheska            # apply cheska's row
    python tools/reshape_hero_arms.py cheska --check    # report only

WHY (owner, 2026-09-27): *"the arms are too long for cheska sean and majority of cast / tahts why tey look so fucked up
walking"*, *"can u lowk js edit thheir models? i thhink the models themselves are te problems"*, *"no need to edit some of
the characters that SHOULD have long arms like paete"*, *"make sean look more muscular by moving his arms out"*.

MEASURED on the bind poses: the shared hero body hangs a 0.284 arm from a shoulder 0.288 above the floor, so an arm let
down straight reaches the ground (two and a half times the torso's height), and half of it is the hand (0.15). Hung
beside the body the fists bob at the knees; spread to clear the hips it is the penguin. The idle and the lineup hide it
in an A, a walk cannot.

⚠️⚠️ ONE ROW PER HERO, CHOSEN BY HAND, NEVER A SWEEP (CLAUDE.md section 0). Each row says how long that hero's arm becomes
and why; run it for one hero, film that hero, look, then the next. Paete is deliberately absent: a tree's arms are long.

⚠️⚠️ SURGERY, NOT A REBUILD. These files carry clips appended long after their builders ran (the hero casts, the grounded
gaits, the slide), so re-running a builder would lose them. This touches only: the POSITION of vertices whose dominant
joint is an arm (the part past the shoulder pivot is compressed along the arm toward the pivot; the part tucked inside the
torso is left alone, so the join does not open), those accessors' min/max, and for a shoulder move, the arm nodes'
translation, every skin's inverse bind matrix for the arm joints, and any arm translation keys. Every other byte is
compared before and after. The shipped bind poses are translation-only (asserted), which is what makes the bind update a
translation.

⚠️ IDEMPOTENT BY REFUSAL: a row records the arm length it expects to find (measured 2026-09-27); an arm already reshaped
does not match and the tool refuses rather than shortening it twice.
"""
import argparse
import json
import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from glb_mesh_dump import read_glb

ROOT = Path(__file__).resolve().parents[1]
PERSONS = ROOT / "Assets/TumbangPreso/Art/characters/persons"

# hero: (glb, arm length found, arm length wanted, shoulder moved out, why). Mesh units; the body is scaled 2.38 in play.
ROWS = {
    # A slab of muscle 0.40 wide: shoulders out onto the sides of the chest so the arms hang off it like a
    # bodybuilder's, and short enough that the fists swing at the hips, not the knees.
    "sean": ("team-sean.glb", 0.293, 0.225, 0.055,
             "shoulders onto the chest's sides, fists at the hips"),
    # Neat and compact: fists at the top of the thigh.
    "cheska": ("team-cheska.glb", 0.284, 0.180, 0.0, "fists at the top of the thigh"),
    # Stone gauntlets: a touch longer than Cheska's so the heavy fists still hang low and weighty.
    "dante": ("team-dante.glb", 0.284, 0.190, 0.0, "heavy stone fists hanging just below the hip"),
    # Loose, long-limbed swagger, but no longer dragging: mid-hip.
    "zack": ("team-zack.glb", 0.284, 0.185, 0.0, "fists at mid-hip"),
    # The robe's wide sleeves: short enough that the hands swing in front of the robe, not inside it.
    "phaister": ("team-phaister.glb", 0.284, 0.185, 0.0, "hands swing in front of the robe"),
    # Loose and athletic.
    "rafi": ("team-rafi.glb", 0.284, 0.185, 0.0, "fists at mid-hip"),
    # The smallest and quickest: short, bright, busy arms.
    "amihan": ("team-amihan.glb", 0.284, 0.175, 0.0, "short busy arms for the quickest hero"),
    # A player's own hero: the cast's middle.
    "custom": ("team-custom.glb", 0.284, 0.182, 0.0, "the cast's middle"),
    "custom_base": ("team-custom-base.glb", 0.284, 0.182, 0.0, "the cast's middle"),
}

COMPONENT = {5120: ("b", 1), 5121: ("B", 1), 5122: ("h", 2), 5123: ("H", 2), 5125: ("I", 4), 5126: ("f", 4)}
COUNT = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}


def span(gltf, index):
    acc = gltf["accessors"][index]
    fmt, size = COMPONENT[acc["componentType"]]
    n = COUNT[acc["type"]]
    view = gltf["bufferViews"][acc["bufferView"]]
    start = view.get("byteOffset", 0) + acc.get("byteOffset", 0)
    stride = view.get("byteStride") or size * n
    return acc, fmt, n, start, stride, size


def read(gltf, blob, index):
    acc, fmt, n, start, stride, size = span(gltf, index)
    return [list(struct.unpack_from("<" + fmt * n, blob, start + i * stride)) for i in range(acc["count"])]


def write(gltf, blob, index, rows):
    acc, fmt, n, start, stride, size = span(gltf, index)
    assert fmt == "f"
    for i, row in enumerate(rows):
        struct.pack_into("<" + fmt * n, blob, start + i * stride, *row)


def write_glb(path, gltf, blob):
    js = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    js += b" " * ((4 - len(js) % 4) % 4)
    blob = bytes(blob) + b"\0" * ((4 - len(blob) % 4) % 4)
    total = 12 + 8 + len(js) + 8 + len(blob)
    with open(path, "wb") as handle:
        handle.write(struct.pack("<III", 0x46546C67, 2, total))
        handle.write(struct.pack("<II", len(js), 0x4E4F534A))
        handle.write(js)
        handle.write(struct.pack("<II", len(blob), 0x004E4942))
        handle.write(blob)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("hero", choices=sorted(ROWS))
    ap.add_argument("--check", action="store_true")
    args = ap.parse_args()
    name, found, wanted, shoulder, why = ROWS[args.hero]
    path = PERSONS / name
    gltf, buffer = read_glb(str(path))
    blob = bytearray(buffer)
    original = bytes(buffer)
    before = json.loads(json.dumps(gltf))
    nodes = gltf["nodes"]
    ids = {n.get("name"): i for i, n in enumerate(nodes)}
    for n in nodes:
        assert "rotation" not in n and "matrix" not in n and "scale" not in n, (name, n.get("name"), "non-translation rest")

    # Each arm: its pivot in model space (the parents' translations summed; translation-only rest) and its side.
    def model_position(i):
        parents = {c: p for p, node in enumerate(nodes) for c in node.get("children", [])}
        x = [0.0, 0.0, 0.0]
        while i is not None:
            t = nodes[i].get("translation", [0, 0, 0])
            x = [x[k] + t[k] for k in range(3)]
            i = parents.get(i)
        return x

    arms = {}
    for bone in ("arm-left", "arm-right"):
        pivot = model_position(ids[bone])
        arms[ids[bone]] = (pivot, 1.0 if pivot[0] > 0 else -1.0)

    # The vertices of every skinned primitive whose dominant joint is an arm.
    edits = []
    measured = []
    seen_positions = set()
    for node in nodes:
        if "mesh" not in node or "skin" not in node:
            continue
        joints = gltf["skins"][node["skin"]]["joints"]
        for prim in gltf["meshes"][node["mesh"]]["primitives"]:
            attrs = prim["attributes"]
            if attrs["POSITION"] in seen_positions:
                continue
            seen_positions.add(attrs["POSITION"])
            pos = read(gltf, blob, attrs["POSITION"])
            jnt = read(gltf, blob, attrs["JOINTS_0"]) if "JOINTS_0" in attrs else None
            wgt = read(gltf, blob, attrs["WEIGHTS_0"]) if "WEIGHTS_0" in attrs else None
            if jnt is None:
                continue
            changed = False
            for v in range(len(pos)):
                dominant = joints[int(jnt[v][max(range(4), key=lambda k: wgt[v][k])])]
                if dominant not in arms:
                    continue
                pivot, side = arms[dominant]
                along = (pos[v][0] - pivot[0]) * side
                measured.append(along)
                if along > 0:
                    pos[v][0] = pivot[0] + side * along * (wanted / found)
                pos[v][0] += side * shoulder
                changed = True
            if changed:
                edits.append((attrs["POSITION"], pos))

    length = max(measured)
    print(f"{args.hero}: arm length found {length:.4f} (row expects {found}), wanted {wanted}, shoulder +{shoulder}: {why}")
    if abs(length - found) > .004:
        raise SystemExit(f"REFUSED: {name}'s arm is {length:.4f}, not the {found} this row was measured against "
                         "(already reshaped, or the model changed; re-measure and update the row by hand).")
    if args.check:
        return

    for index, pos in edits:
        write(gltf, blob, index, pos)
        acc = gltf["accessors"][index]
        acc["min"] = [min(p[k] for p in pos) for k in range(3)]
        acc["max"] = [max(p[k] for p in pos) for k in range(3)]

    if shoulder:
        done = set()
        for i, (pivot, side) in arms.items():
            t = nodes[i].setdefault("translation", [0.0, 0.0, 0.0])
            t[0] += side * shoulder
            for skin in gltf["skins"]:
                if i not in skin["joints"]:
                    continue
                k = skin["joints"].index(i)
                if (skin["inverseBindMatrices"], k) in done:
                    continue
                done.add((skin["inverseBindMatrices"], k))
                mats = read(gltf, blob, skin["inverseBindMatrices"])
                # Column-major; translation-only bind, so the inverse bind translation is minus the joint's position.
                mats[k][12] -= side * shoulder
                write(gltf, blob, skin["inverseBindMatrices"], mats)
            for anim in gltf.get("animations", []):
                for ch in anim["channels"]:
                    if ch["target"]["node"] == i and ch["target"]["path"] == "translation":
                        out = anim["samplers"][ch["sampler"]]["output"]
                        if out in done:
                            continue
                        done.add(out)
                        keys = read(gltf, blob, out)
                        for key in keys:
                            key[0] += side * shoulder
                        write(gltf, blob, out, keys)
                        acc = gltf["accessors"][out]
                        if "min" in acc:
                            acc["min"] = [min(q[c] for q in keys) for c in range(3)]
                            acc["max"] = [max(q[c] for q in keys) for c in range(3)]

    # Nothing but what is listed above may differ.
    assert len(blob) == len(original)
    for key in ("meshes", "materials", "textures", "images", "skins", "animations", "bufferViews"):
        assert gltf.get(key) == before.get(key), key
    write_glb(path, gltf, blob)
    print(f"wrote {path.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
