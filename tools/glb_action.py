"""Append one named action to an existing rig, without touching anything else in it.

This is the surgical half of `author_retrieval_slide.py`, lifted out so the hero casts
can use the same one. `CLAUDE.md` § 4's rule about the core package applies to tools as
well: **one copy, two callers.** A second transcription of glTF buffer surgery is a
second thing to get wrong, and the copy that drifts is the one nobody re-verifies.

WHAT IT GUARANTEES, AND WHY EACH GUARANTEE IS ASSERTED RATHER THAN INTENDED:

* ⚠️⚠️ **EVERY ORIGINAL BYTE OF THE BINARY CHUNK IS STILL THERE, AT THE SAME OFFSET.**
  New samples are appended past the end, so every existing accessor still addresses the
  same bytes. Mesh, skin, materials, textures and all 32 shipped clips are untouched by
  construction rather than by care, and `blob[:len(original)] == original` says so.
* ⚠️ **THE JSON's nodes, skins, meshes, materials, textures and images are compared
  before and after**, and the existing animation list is compared element by element. A
  tool that rewrites a rig it was asked to add one clip to is the worst failure here,
  because it looks like a working export.
* ⚠️ **A rig with a rotated or scaled rest transform is REFUSED, not guessed at.** Every
  pose in this project is authored against a translation-only rest hierarchy; applying
  one to a rig with a baked rotation silently produces a different pose.

⚠️ THE FLOOR IS SOLVED FROM REAL SKINNED VERTICES, NOT FROM THE SKELETON. Bone angles
say nothing about where a body actually is: `tools/inspect_slide_rig.py`'s header carries
the measurement that made that point (one cast, two arm-to-shoulder ratios, 1.008 and
0.712). So `Rig.lowest` deforms the mesh and reports its lowest point, and the caller
keys whatever root height puts that back on the ground.
"""
import copy
import math
from pathlib import Path
import struct
import sys

from mathutils import Euler, Matrix, Quaternion, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from glb_mesh_dump import read_glb, read_accessor
from build_person_voxel import write_glb

# The seven this project's rigs have. No knees, no elbows: a limb is one rigid segment,
# which is why every pose here is expressed as a shoulder or hip angle and never as a
# reach target.
BONES = ("root", "torso", "head", "leg-left", "leg-right", "arm-left", "arm-right")

# ⚠️⚠️ THE SOURCE IS A T-POSE, SO AN ARM IS LOWERED BEFORE IT IS SWUNG, AND THE ORDER OF
# THAT MULTIPLICATION IS NOT INTERCHANGEABLE. `Quaternion(X, pitch) @ Quaternion(Z, drop)`
# drops the arm to the side first and then swings it about the shoulder's lateral axis.
# Reversed, the pitch is applied to an arm still pointing straight out sideways, where it
# spins the limb about its own length and the character stays spread-eagled. This cost a
# whole authoring pass before it was written down.
ARM_DROP = 80.0

# Negative pitch swings a limb FORWARD, on arms and on legs alike, which matches the
# convention `HeroAbilityClips`' header states for the procedural clips it is replacing.


class Rig:
    """One `.glb` on disk, loaded far enough to pose and to measure."""

    def __init__(self, path):
        self.path = Path(path)
        self.gltf, self.original = read_glb(str(self.path))
        self.nodes = self.gltf["nodes"]
        self.ids = {n.get("name"): i for i, n in enumerate(self.nodes)}
        self.parents = {c: i for i, n in enumerate(self.nodes)
                        for c in n.get("children", [])}

        missing = [b for b in BONES if b not in self.ids]
        assert not missing, f"{self.path.name} has no {missing}"

        for n in self.nodes:
            assert not any(k in n for k in ("matrix", "rotation", "scale")), (
                self.path.name, n.get("name"),
                "inspect a nontranslation rest transform before posing it")

        self.rest = {i: self.world(i) for i in range(len(self.nodes))}
        self.vertices = self._skinned()
        self.floor = min(self.height_of(v, self.rest) for v in self.vertices)
        self.top = max(self.height_of(v, self.rest) for v in self.vertices)
        self.height = self.top - self.floor

    def world(self, i, rotations=None):
        n = self.nodes[i]
        local = Matrix.Translation(Vector(n.get("translation", (0, 0, 0))))
        if rotations and n.get("name") in rotations:
            local = local @ rotations[n["name"]].to_matrix().to_4x4()
        return (self.world(self.parents[i], rotations) @ local
                if i in self.parents else local)

    def _skinned(self):
        out = []
        for n in self.nodes:
            if "skin" not in n:
                continue
            joints = self.gltf["skins"][n["skin"]]["joints"]
            for prim in self.gltf["meshes"][n["mesh"]]["primitives"]:
                at = prim["attributes"]
                positions = read_accessor(self.gltf, self.original, at["POSITION"])
                weights = read_accessor(self.gltf, self.original, at["WEIGHTS_0"])
                indices = read_accessor(self.gltf, self.original, at["JOINTS_0"])
                for p, ws, js in zip(positions, weights, indices):
                    out.append([(joints[j], w,
                                 self.rest[joints[j]].inverted() @ Vector(p))
                                for j, w in zip(js, ws) if w > 0])
        assert out, f"{self.path.name} has no skinned vertices"
        return out

    @staticmethod
    def height_of(vertex, matrices):
        return sum((matrices[j] @ p).y * w for j, w, p in vertex)

    def owned_by(self, bone, share=0.5):
        """Vertices this bone owns most of. The rig has no hand, so the far end of the
        right arm IS the hand, by weight rather than by name."""
        return [v for v in self.vertices
                if sum(w for j, w, _ in v if self.nodes[j].get("name") == bone) > share]

    def posed(self, rotations):
        """World matrices for every node under one set of bone rotations."""
        return {i: self.world(i, rotations) for i in self.rest}

    def lowest(self, matrices):
        return min(self.height_of(v, matrices) for v in self.vertices)

    def highest(self, matrices):
        return max(self.height_of(v, matrices) for v in self.vertices)


def rotations(angles):
    """Bone name to quaternion, given (pitch, yaw, roll) degrees per bone.

    The arms take the T-pose correction; everything else is a plain XYZ Euler.
    """
    out = {n: Euler(tuple(math.radians(v) for v in a), "XYZ").to_quaternion()
           for n, a in angles.items()}
    for name, sign in (("arm-left", -1), ("arm-right", 1)):
        if name not in angles:
            continue
        pitch, _, spread = angles[name]
        out[name] = (Quaternion((1, 0, 0), math.radians(pitch)) @
                     Quaternion((0, 0, 1), math.radians(sign * ARM_DROP + spread)))
    return out


def append_action(rig, name, times, tracks, root_translation, order=None):
    """Write `name` into the rig's `.glb`, in place, keeping every original byte.

    `tracks` maps bone name to a list of (x, y, z, w) rotations, one per time.
    `root_translation` is a list of (x, y, z) for the root node, same length.
    `order` fixes the channel order when byte-for-byte reproducibility matters.
    """
    gltf, blob = rig.gltf, bytearray(rig.original)
    assert not any(a.get("name") == name for a in gltf.get("animations", [])), (
        f"'{name}' already exists in {rig.path.name}; restore the source before "
        "reauthoring, so the clip in the file is always the one this tool produced")

    before = copy.deepcopy(gltf)
    assert len(times) == len(root_translation)
    for bone, values in tracks.items():
        assert len(values) == len(times), bone

    def add(values, kind):
        blob.extend(b"\0" * ((-len(blob)) % 4))
        start = len(blob)
        for value in values:
            blob.extend(struct.pack("<" + "f" * len(value), *value))
        view = len(gltf["bufferViews"])
        gltf["bufferViews"].append(
            {"buffer": 0, "byteOffset": start, "byteLength": len(blob) - start})
        index = len(gltf["accessors"])
        accessor = {"bufferView": view, "componentType": 5126,
                    "count": len(values), "type": kind}
        if kind == "SCALAR":
            accessor.update(min=[values[0][0]], max=[values[-1][0]])
        gltf["accessors"].append(accessor)
        return index

    clock = add([(t,) for t in times], "SCALAR")
    clip = {"name": name, "samplers": [], "channels": []}

    channels = [(b, tracks[b], "VEC4", "rotation") for b in (order or tracks)]
    channels.append(("root", root_translation, "VEC3", "translation"))

    for bone, values, kind, prop in channels:
        output = add(values, kind)
        clip["channels"].append({"sampler": len(clip["samplers"]),
                                 "target": {"node": rig.ids[bone], "path": prop}})
        clip["samplers"].append({"input": clock, "output": output,
                                 "interpolation": "LINEAR"})

    gltf.setdefault("animations", []).append(clip)
    gltf["buffers"][0]["byteLength"] = len(blob)

    for key in ("nodes", "skins", "meshes", "materials", "textures", "images"):
        assert gltf.get(key) == before.get(key), key
    assert gltf["animations"][:-1] == before.get("animations", [])
    assert blob[:len(rig.original)] == rig.original

    write_glb(str(rig.path), gltf, blob)

    return {"file": rig.path.name, "clip": name, "duration": times[-1],
            "samples": len(times), "authored_height": round(rig.height, 5),
            "original_clips_preserved": len(before.get("animations", [])),
            "original_binary_preserved": True}
