"""
Pose previews of a hero's ULTIMATE INTRODUCTION, drawn from the real glb, in Unity's space.

WHY THIS EXISTS: the cloud machine that authored REFINE-2.11 has no Unity licence, so the
in-engine render pipeline (`docs/CANONICAL_RENDERING_PIPELINE.md`) could not run there. A pose
still has to be LOOKED AT before it ships, so this skins the actual team-*.glb with the actual
key tables `tools/author_ultimate_intros.py` writes, from the actual shot the game will use.

WARNING: IT IS A SILHOUETTE AND POSE CHECK, NOT THE LOOK. No toon shader, no ink outline, no
accessories Unity adds at runtime, no stage effects. Every sheet says so in its corner. Final
art approval stays in the engine.

WARNING: IT WORKS IN UNITY'S SPACE, NOT GLTF'S. glTFast mirrors X on import (the gameplay
animation report found this the hard way), and the key tables are Unity local euler angles
(`localEulerAnglesRaw`, applied as Quaternion.Euler: Z, then X, then Y). So every glTF position
and matrix is conjugated by diag(-1, 1, 1) before anything else happens, and the camera looks
along +Z with +X to the right, exactly as a Unity camera does.
"""

import math
import numpy as np
import pygltflib
from PIL import Image, ImageDraw, ImageFont
import io

S = np.diag([-1.0, 1.0, 1.0, 1.0])


def quat_euler(x, y, z):
    """Unity Quaternion.Euler(x, y, z) as a 3x3 matrix: Ry * Rx * Rz."""
    x, y, z = map(math.radians, (x, y, z))
    cx, sx, cy, sy, cz, sz = math.cos(x), math.sin(x), math.cos(y), math.sin(y), math.cos(z), math.sin(z)
    rx = np.array([[1, 0, 0], [0, cx, -sx], [0, sx, cx]])
    ry = np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]])
    rz = np.array([[cz, -sz, 0], [sz, cz, 0], [0, 0, 1]])
    return ry @ rx @ rz


def _accessor(g, blob, index):
    acc = g.accessors[index]
    view = g.bufferViews[acc.bufferView]
    comps = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}[acc.type]
    dtype = {5126: np.float32, 5123: np.uint16, 5125: np.uint32, 5121: np.uint8}[acc.componentType]
    start = (view.byteOffset or 0) + (acc.byteOffset or 0)
    count = acc.count * comps
    arr = np.frombuffer(blob, dtype=dtype, count=count, offset=start)
    return arr.reshape(acc.count, comps) if comps > 1 else arr


class Model:
    def __init__(self, path):
        g = pygltflib.GLTF2().load(path)
        blob = g.binary_blob()
        self._g, self._blob = g, blob
        self.nodes = g.nodes
        self.names = {i: n.name for i, n in enumerate(g.nodes)}
        self.parent = {}
        for i, n in enumerate(g.nodes):
            for c in n.children or []:
                self.parent[c] = i
        img = g.images[0]
        if img.bufferView is not None:
            view = g.bufferViews[img.bufferView]
            data = blob[view.byteOffset or 0:(view.byteOffset or 0) + view.byteLength]
            self.texture = np.asarray(Image.open(io.BytesIO(data)).convert("RGB"))
        else:
            import os
            self.texture = np.asarray(Image.open(os.path.join(os.path.dirname(path), img.uri)).convert("RGB"))
        self.parts = []
        for ni, n in enumerate(g.nodes):
            if n.mesh is None:
                continue
            skin = g.skins[n.skin]
            ibm = _accessor(g, blob, skin.inverseBindMatrices).reshape(-1, 4, 4).transpose(0, 2, 1)
            for p in g.meshes[n.mesh].primitives:
                pos = _accessor(g, blob, p.attributes.POSITION).astype(float)
                uv = _accessor(g, blob, p.attributes.TEXCOORD_0).astype(float)
                joints = _accessor(g, blob, p.attributes.JOINTS_0).astype(int)
                weights = _accessor(g, blob, p.attributes.WEIGHTS_0).astype(float)
                idx = _accessor(g, blob, p.indices).astype(int).reshape(-1, 3)
                h, w, _ = self.texture.shape
                cuv = uv[idx].mean(axis=1)
                px = np.clip((cuv[:, 0] % 1.0) * (w - 1), 0, w - 1).astype(int)
                py = np.clip((cuv[:, 1] % 1.0) * (h - 1), 0, h - 1).astype(int)
                colours = self.texture[py, px].astype(float) / 255.0
                self.parts.append(dict(pos=pos, joints=joints, weights=weights, idx=idx,
                                       colour=colours, skin=skin.joints, ibm=ibm))

    def world(self, pose):
        """Node world matrices in Unity space. `pose` maps bone -> (rot3x3 or None, pos or None)."""
        mats = {}

        def local(i):
            n = self.nodes[i]
            t = np.array(n.translation or [0, 0, 0], float)
            m = np.eye(4)
            m[:3, 3] = S[:3, :3] @ t
            name = n.name
            if name in pose:
                rot, at = pose[name]
                if rot is not None:
                    m[:3, :3] = rot
                if at is not None:
                    m[:3, 3] = at
            return m

        def get(i):
            if i in mats:
                return mats[i]
            m = local(i)
            if i in self.parent:
                m = get(self.parent[i]) @ m
            mats[i] = m
            return m

        for i in range(len(self.nodes)):
            get(i)
        return mats

    def action(self, name, t):
        """
        A glb animation sampled at t (linear, as authored) as a pose in Unity space.
        glTF quaternions convert to Unity's mirrored-X space as (x, -y, -z, w); translations (-x, y, z).
        Returns (pose, duration).
        """
        g, blob = self._g, self._blob
        anim = next(a for a in g.animations if a.name == name)
        pose, duration = {}, 0.0
        for ch in anim.channels:
            smp = anim.samplers[ch.sampler]
            times = _accessor(g, blob, smp.input).astype(float).reshape(-1)
            vals = _accessor(g, blob, smp.output).astype(float)
            duration = max(duration, times[-1])
            k = int(np.searchsorted(times, t, side="right") - 1)
            k = max(0, min(k, len(times) - 2))
            u = 0.0 if times[k + 1] == times[k] else min(1.0, max(0.0, (t - times[k]) / (times[k + 1] - times[k])))
            v = vals[k] * (1 - u) + vals[k + 1] * u
            node = g.nodes[ch.target.node].name
            rot, pos = pose.get(node, (None, None))
            if ch.target.path == "rotation":
                x, y, z, w = v / np.linalg.norm(v)
                x, y, z = x, -y, -z
                rot = np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                                [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                                [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])
            elif ch.target.path == "translation":
                pos = np.array([-v[0], v[1], v[2]])
            pose[node] = (rot, pos)
        return pose, duration

    def hands(self, pose):
        """Farthest skinned vertex of each arm from its shoulder: a palm, in Unity space (glb units)."""
        mats = self.world(pose)
        out = {}
        part = self.parts[0]
        names = [self.names[j] for j in part["skin"]]
        for side in ("left", "right"):
            bone = "arm-" + side
            li = names.index(bone)
            mask = (part["joints"][:, 0] == li) & (part["weights"][:, 0] > .9)
            node = [i for i in self.names if self.names[i] == bone][0]
            jm = mats[node] @ (S @ part["ibm"][li] @ S)
            v = np.c_[part["pos"][mask] * np.array([-1, 1, 1]), np.ones(mask.sum())]
            pts = (v @ jm.T)[:, :3]
            shoulder = mats[node][:3, 3]
            out[side] = pts[np.argmax(np.linalg.norm(pts - shoulder, axis=1))]
        return out

    def skinned(self, pose):
        mats = self.world(pose)
        tris, cols = [], []
        for part in self.parts:
            jm = np.stack([mats[j] @ (S @ part["ibm"][k] @ S) for k, j in enumerate(part["skin"])])
            v = np.c_[part["pos"] * np.array([-1, 1, 1]), np.ones(len(part["pos"]))]
            out = np.zeros((len(v), 4))
            for k in range(4):
                out += part["weights"][:, k:k + 1] * np.einsum("nij,nj->ni", jm[part["joints"][:, k]], v)
            tris.append(out[:, :3][part["idx"]])
            cols.append(part["colour"])
        return np.concatenate(tris), np.concatenate(cols)


def look_matrix(eye, target):
    f = np.array(target, float) - np.array(eye, float)
    f /= np.linalg.norm(f)
    r = np.cross([0, 1, 0], f)
    r /= np.linalg.norm(r)
    u = np.cross(f, r)
    return np.stack([r, u, f])


def render(tris, cols, eye, target, fov, size=(640, 360), extra=None, ground=0.0, label=None, sky=(236, 226, 206)):
    """Painter's algorithm with flat Lambert shading and a thin dark edge (a nod to the ink line)."""
    w, h = size
    img = Image.new("RGB", size, sky)
    d = ImageDraw.Draw(img)
    rot = look_matrix(eye, target)
    focal = (h / 2) / math.tan(math.radians(fov) / 2)

    def project(p):
        c = rot @ (np.asarray(p) - eye)
        return c

    # Floor grid, so height off the ground (a levitation) reads.
    for gx in np.arange(-3, 3.01, 0.5):
        pts = [project([gx + target[0], ground, gz + target[2]]) for gz in np.arange(-3, 3.01, 0.25)]
        pts = [(w / 2 + focal * c[0] / c[2], h / 2 - focal * c[1] / c[2]) for c in pts if c[2] > 0.1]
        if len(pts) > 1:
            d.line(pts, fill=(205, 192, 168), width=1)
    for gz in np.arange(-3, 3.01, 0.5):
        pts = [project([gx + target[0], ground, gz + target[2]]) for gx in np.arange(-3, 3.01, 0.25)]
        pts = [(w / 2 + focal * c[0] / c[2], h / 2 - focal * c[1] / c[2]) for c in pts if c[2] > 0.1]
        if len(pts) > 1:
            d.line(pts, fill=(205, 192, 168), width=1)

    cam = np.einsum("ij,ntj->nti", rot, tris - eye)
    depth = cam[:, :, 2].mean(axis=1)
    normals = np.cross(tris[:, 1] - tris[:, 0], tris[:, 2] - tris[:, 0])
    ln = np.linalg.norm(normals, axis=1, keepdims=True)
    normals = normals / np.maximum(ln, 1e-9)
    # Unity's mirrored winding: flip if most normals face inward. Use abs for two-sided light.
    light = np.array([0.35, 0.8, -0.45])
    light /= np.linalg.norm(light)
    shade = 0.55 + 0.45 * np.abs(normals @ light)
    order = np.argsort(-depth)
    for i in order:
        c = cam[i]
        if c[:, 2].min() <= 0.1:
            continue
        pts = [(w / 2 + focal * p[0] / p[2], h / 2 - focal * p[1] / p[2]) for p in c]
        col = tuple(int(255 * min(1, v * shade[i])) for v in cols[i])
        d.polygon(pts, fill=col, outline=tuple(int(v * 0.55) for v in col))
    if extra:
        extra(d, project, focal, w, h)
    if label:
        d.text((6, 4), label, fill=(40, 20, 10))
    d.text((6, h - 14), "pose preview, not engine look", fill=(120, 90, 60))
    return img
