"""Put an already-correct .obj prop into the game's carry frame, in place.

  python tools/normalise_obj_prop.py Assets/.../tsinelas_sike.obj

⚠️⚠️ THIS EXISTS FOR IKE AND FOR THE REASON IKE COULD NOT GO THROUGH THE NORMAL PIPELINE.
🧑 2026-08-28: *"i didnt want u o replace ike br"*, *"js fix it"*. So the mesh, the
material and the .mtl stay exactly as they are; `build_slipper_models.py` would have
rebuilt it as a .glb and thrown away the hand-maintained material note along with it.

⚠️ THE FAULT IT FIXES IS REAL AND WAS FOUND BY MEASURING ALL TEN PROPS TOGETHER, not by
looking at IKE. Every other slipper in the roster is 0.432 m along X and seated on y = 0.
IKE measured 0.184 x 0.104 x 0.432 with its long axis on Z and its lowest vertex at
y = -0.0269. `Slipper.CarryRotation` is a quarter turn about Y that takes +X to forward,
so a prop whose length runs down Z is carried across the palm rather than along it, and
one whose origin sits inside the mesh hangs 27 mm through the hand. Nine props obeyed
the convention and the tenth did not.

⚠️ IT REWRITES `v` AND `vn` AND NOTHING ELSE. Faces, UVs, material groups, the object
name and every comment survive byte for byte, so the diff is only the coordinate frame.
Idempotent: running it on an already-normalised prop moves nothing.
"""

import math
import sys
from pathlib import Path

TARGET_LENGTH = 0.432


def read(path):
    verts, normals, lines = [], [], []
    for line in path.read_text().splitlines():
        if line.startswith("v "):
            verts.append([float(x) for x in line[2:].split()])
            lines.append(("v", len(verts) - 1))
        elif line.startswith("vn "):
            normals.append([float(x) for x in line[3:].split()])
            lines.append(("vn", len(normals) - 1))
        else:
            lines.append(("raw", line))
    return verts, normals, lines


def covariance_axes(points):
    """The three principal axes, longest first. See build_slipper_models.level."""
    n = len(points)
    mean = [sum(p[i] for p in points) / n for i in range(3)]
    cov = [[0.0] * 3 for _ in range(3)]
    for p in points:
        d = [p[i] - mean[i] for i in range(3)]
        for i in range(3):
            for j in range(3):
                cov[i][j] += d[i] * d[j]

    import numpy

    values, vectors = numpy.linalg.eigh(numpy.array(cov))
    order = list(numpy.argsort(values))[::-1]
    axes = [[float(v) for v in vectors[:, k]] for k in order]

    def cross(a, b):
        return [a[1] * b[2] - a[2] * b[1],
                a[2] * b[0] - a[0] * b[2],
                a[0] * b[1] - a[1] * b[0]]

    if sum(c * d for c, d in zip(cross(axes[0], axes[1]), axes[2])) < 0.0:
        axes[2] = [-v for v in axes[2]]
    return axes


def apply(rows, axes):
    return [[sum(a[i] * r[i] for i in range(3)) for a in axes] for r in rows]


def main():
    path = Path(sys.argv[1])
    verts, normals, lines = read(path)

    # ⚠️ THE OBJ IS Y-UP, NOT Z-UP. Unity imports it unrotated, so "up" here is index 1
    # and the ground plane is XZ. The Blender pipeline works Z-up and converts on export;
    # this one must not, or the prop lands on its side in the hand.
    axes = covariance_axes(verts)
    length, width, up = axes[0], axes[2], axes[1]
    # Rebuild as (length -> X, up -> Y, width -> Z).
    frame = [length, up, width]

    verts = apply(verts, frame)
    normals = apply(normals, frame)

    lo = [min(v[i] for v in verts) for i in range(3)]
    hi = [max(v[i] for v in verts) for i in range(3)]

    # Sole down. The half with the larger ground footprint is the bottom.
    mid = (lo[1] + hi[1]) * 0.5
    halves = [[v for v in verts if v[1] < mid], [v for v in verts if v[1] >= mid]]

    def footprint(g):
        if not g:
            return 0.0
        return ((max(p[0] for p in g) - min(p[0] for p in g))
                * (max(p[2] for p in g) - min(p[2] for p in g)))

    if footprint(halves[1]) > footprint(halves[0]):
        verts = [[v[0], -v[1], -v[2]] for v in verts]
        normals = [[n[0], -n[1], -n[2]] for n in normals]
        lo = [min(v[i] for v in verts) for i in range(3)]
        hi = [max(v[i] for v in verts) for i in range(3)]

    # Toe forward. A shoe carries its mass ahead of centre, so a centroid behind the box
    # centre means it is facing backwards.
    if sum(v[0] for v in verts) / len(verts) < (lo[0] + hi[0]) * 0.5:
        verts = [[-v[0], v[1], -v[2]] for v in verts]
        normals = [[-n[0], n[1], -n[2]] for n in normals]
        lo = [min(v[i] for v in verts) for i in range(3)]
        hi = [max(v[i] for v in verts) for i in range(3)]

    scale = TARGET_LENGTH / (hi[0] - lo[0])
    cx, cz = (lo[0] + hi[0]) * 0.5, (lo[2] + hi[2]) * 0.5
    verts = [[(v[0] - cx) * scale, (v[1] - lo[1]) * scale, (v[2] - cz) * scale]
             for v in verts]

    def unit(n):
        m = math.sqrt(sum(c * c for c in n)) or 1.0
        return [c / m for c in n]

    normals = [unit(n) for n in normals]

    out, vi, ni = [], 0, 0
    for kind, payload in lines:
        if kind == "v":
            out.append("v %.5f %.5f %.5f" % tuple(verts[vi])); vi += 1
        elif kind == "vn":
            out.append("vn %.5f %.5f %.5f" % tuple(normals[ni])); ni += 1
        else:
            out.append(payload)
    path.write_text("\n".join(out) + "\n", newline="\n")

    size = [max(v[i] for v in verts) - min(v[i] for v in verts) for i in range(3)]
    print(f"{path.name}: {size[0]:.3f} x {size[1]:.3f} x {size[2]:.3f} m, "
          f"seated on y={min(v[1] for v in verts):.4f}")


if __name__ == "__main__":
    main()
