"""Measure one rig against the shared retrieval slide BEFORE writing anything to it.

    blender --background --python tools/inspect_slide_rig.py -- FILE [FILE ...]

WHY THIS EXISTS. `ASTRA.md` task 3 says, in its own words, *"inspect its proportions
and existing rest transforms; do not blindly assume the Classic poses fit"*. The
twelve Classic `character-*.glb` rigs were authored first, and the ten roster rigs
left over are NOT all the same size: `team-sean` stands its legs at **0.245** where
`character-male-a` stands them at 0.176, and `team-nemu` hangs its arms at **0.080**
against 0.112. One set of joint angles applied to three different skeletons produces
three different silhouettes, and the two ways that goes wrong are a limb through the
floor and a reach that never gets near the ground.

⚠️⚠️ THE ANGLES ARE NOT THE MEASUREMENT. `author_retrieval_slide.BEATS` holds
rotations, and a rotation says nothing about where a hand ends up: that is the joint
angle TIMES the limb length, and the limb length is what differs between these rigs.
So this reads the real skinned vertices of the file in front of it, poses them through
the same `pose()` the author uses, and reports where the body actually goes. It is the
same technique `author` already uses to solve pelvis height, asking the question one
step earlier and without writing a byte.

It reports, per rig and per beat:

  * `pelvis_offset`  the vertical correction the author will key onto `root`, which is
    how far the body would otherwise sink through the ground. It is the SIZE of the
    skid, so a rig whose number is near zero is not sliding.
  * `top`            the highest skinned vertex, so `body_drop` is how far the whole
    silhouette lowers. `verify_retrieval_slide.py` asserts this exceeds 0.1 on the
    imported result and a small rig is the one that could fail it.
  * `reach`          the lowest vertex weighted mainly to `arm-right`, above the floor.
    ⚠️ THIS IS THE ONE THE ANGLES CANNOT PREDICT. The retrieval reach is the point of
    the whole clip: a hand that stops 0.2 m up is a character miming a pickup.
  * `forward`        how far the body extends along -Z, which is the committed part.

⚠️ IT WRITES NOTHING AND ASSERTS NOTHING ABOUT TASTE. A number out of family with the
Classic rigs is a prompt to look, not a failure: the reference row is printed first for
exactly that comparison, which is `PersonSwapProbe`'s *"the old row is the point of the
sheet"* argument applied to arithmetic instead of to a picture.
"""
import argparse
import json
import math
from pathlib import Path
import sys

from mathutils import Euler, Matrix, Vector, Quaternion

sys.path.insert(0, str(Path(__file__).resolve().parent))
from glb_mesh_dump import read_glb, read_accessor
from author_retrieval_slide import BEATS, pose

# The seven the pose addresses. A rig missing one of these cannot take this clip at
# all, which is a louder finding than any number below it.
BONES = ("root", "torso", "head", "leg-left", "leg-right", "arm-left", "arm-right")


def load(path):
    """Rest world matrices, and every skinned vertex as (joint, weight, local) terms."""
    gltf, blob = read_glb(str(path))
    nodes = gltf["nodes"]
    ids = {n.get("name"): i for i, n in enumerate(nodes)}
    parents = {c: i for i, n in enumerate(nodes) for c in n.get("children", [])}

    missing = [b for b in BONES if b not in ids]
    posed = [n for n in nodes
             if any(k in n for k in ("matrix", "rotation", "scale"))]

    def world(i, rotations=None):
        n = nodes[i]
        local = Matrix.Translation(Vector(n.get("translation", (0, 0, 0))))
        if rotations and n.get("name") in rotations:
            local = local @ rotations[n["name"]].to_matrix().to_4x4()
        return (world(parents[i], rotations) @ local) if i in parents else local

    rest = {i: world(i) for i in range(len(nodes))}

    vertices = []
    for n in nodes:
        if "skin" not in n:
            continue
        joints = gltf["skins"][n["skin"]]["joints"]
        for prim in gltf["meshes"][n["mesh"]]["primitives"]:
            at = prim["attributes"]
            positions = read_accessor(gltf, blob, at["POSITION"])
            weights = read_accessor(gltf, blob, at["WEIGHTS_0"])
            indices = read_accessor(gltf, blob, at["JOINTS_0"])
            for p, ws, js in zip(positions, weights, indices):
                vertices.append([(joints[j], w, rest[joints[j]].inverted() @ Vector(p))
                                 for j, w in zip(js, ws) if w > 0])

    return gltf, nodes, ids, world, rest, vertices, missing, posed


def rotations_at(t):
    """The same rotation set `author` keys, including its T-pose arm correction."""
    rots = {n: Euler(tuple(math.radians(v) for v in angles), "XYZ").to_quaternion()
            for n, angles in pose(t).items()}
    for name, sign in [("arm-left", -1), ("arm-right", 1)]:
        pitch, _, spread = pose(t)[name]
        rots[name] = (Quaternion((1, 0, 0), math.radians(pitch)) @
                      Quaternion((0, 0, 1), math.radians(sign * 80 + spread)))
    return rots


def inspect(path):
    gltf, nodes, ids, world, rest, vertices, missing, posed = load(path)

    clips = [a.get("name") for a in gltf.get("animations", [])]

    # ⚠️ A vertex belongs to the hand when the RIGHT ARM owns most of it. Weighting
    # rather than a name, because this rig has no hand bone: the arm is one rigid
    # limb and its far end is the hand by geometry alone.
    hand = [v for v in vertices
            if sum(w for j, w, _ in v if nodes[j].get("name") == "arm-right") > 0.5]

    height = max(sum((rest[j] @ p).y * w for j, w, p in v) for v in vertices)
    floor = min(sum((rest[j] @ p).y * w for j, w, p in v) for v in vertices)

    row = {
        "file": path.name,
        "clips": len(clips),
        "has_slide": "slide" in clips,
        "nontranslation_rest": [n.get("name") for n in posed],
        "missing_bones": missing,
        "rest": {b: [round(x, 4) for x in (nodes[ids[b]].get("translation") or (0, 0, 0))]
                 for b in BONES if b in ids},
        "authored_height": round(height - floor, 5),
        "hand_vertices": len(hand),
        "beats": [],
    }

    if missing or posed:
        return row

    tops, reaches = [], []

    for t in [b[0] for b in BEATS]:
        rots = rotations_at(t)
        matrices = {j: world(j, rots) for j in rest}

        def y(v):
            return sum((matrices[j] @ p).y * w for j, w, p in v)

        def z(v):
            return sum((matrices[j] @ p).z * w for j, w, p in v)

        low = min(y(v) for v in vertices)
        lift = floor - low                      # what `author` keys onto `root`
        top = max(y(v) for v in vertices) + lift
        reach = (min(y(v) for v in hand) + lift - floor) if hand else None
        forward = -min(z(v) for v in vertices)

        tops.append(top)
        if reach is not None:
            reaches.append(reach)

        row["beats"].append({
            "t": t,
            "pelvis_offset": round(lift, 5),
            "top": round(top, 5),
            "reach": None if reach is None else round(reach, 5),
            "forward": round(forward, 5),
        })

    row["body_drop"] = round(max(tops) - min(tops), 5)
    row["standing_top"] = round(tops[0], 5)
    row["lowest_top"] = round(min(tops), 5)
    row["deepest_skid"] = round(max(b["pelvis_offset"] for b in row["beats"]), 5)
    if reaches:
        row["closest_reach"] = round(min(reaches), 5)
        row["reach_as_height"] = round(min(reaches) / (height - floor), 4)

    return row


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("files", type=Path, nargs="+")
    ap.add_argument("--out", type=Path)
    args = ap.parse_args(sys.argv[sys.argv.index("--") + 1:]
                         if "--" in sys.argv else sys.argv[1:])

    rows = [inspect(f) for f in args.files]

    for r in rows:
        head = (f"{r['file']:<24} clips {r['clips']:>2} slide {str(r['has_slide']):<5} "
                f"height {r['authored_height']:.4f}")
        if r["missing_bones"] or r["nontranslation_rest"]:
            print(head + "  REFUSED: "
                  + json.dumps({"missing": r["missing_bones"],
                                "nontranslation_rest": r["nontranslation_rest"]}))
            continue
        print(head + f" drop {r['body_drop']:.4f} skid {r['deepest_skid']:.4f} "
                     f"reach {r.get('closest_reach', float('nan')):.4f} "
                     f"({r.get('reach_as_height', float('nan')):.1%} of height)")

    if args.out:
        args.out.write_text(json.dumps({"rigs": rows}, indent=2) + "\n", encoding="utf-8")

    print("INSPECT_SLIDE_RIG " + json.dumps({"count": len(rows)}))
