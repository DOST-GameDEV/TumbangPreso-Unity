"""Lift a decal material clear of the surface it z-fights with.

  python tools/lift_obj_decal.py <file.obj> <material> <metres>
  python tools/lift_obj_decal.py Assets/.../tsinelas_sike.obj m3 0.0008

⚠️⚠️ THIS EXISTS FOR THE IKE SWOOSH. 🧑 2026-08-28, off the in-engine sheet: *"wtf is
this can u fix the shaders"*, over a band of black-and-white speckle down the side of the
shoe. It was not a shader fault and the toon pass was innocent.

`sike_sandals.glb` builds its swoosh from SVG-derived flat geometry laid ON the upper:
measured, m3 is a 925-vertex island with **zero vertices shared** with the 33,108-vertex
body, and it sits flush against it. Two coplanar surfaces at the same depth is textbook
z-fighting, and it resolves per pixel per frame, which is why it reads as noise rather
than as a misplaced logo.

⚠️ THE FIX IS GEOMETRY, NOT DEPTH BIAS OR RENDER QUEUE. A bias applies to a whole
material and the swoosh shares its shader with everything else the toon pass dresses;
pushing the decal off the surface is local to this one mesh and cannot affect a single
other prop.

⚠️ AND IT IS DELIBERATELY TINY. 0.8 mm on a 432 mm shoe is under a fifth of a percent of
its length. It is enough to beat depth-buffer precision at arena distance and small
enough that the logo still reads as printed on the shoe rather than floating over it,
which is what a larger offset looks like from a low camera.

Rewrites only the `v` lines belonging to that material's faces. Faces, UVs, normals,
material groups and every comment survive.

⚠️ NOT IDEMPOTENT, unlike `normalise_obj_prop.py`. Each run lifts by the amount given, so
running it twice lifts twice. The amount actually applied is printed.
"""

import math
import sys
from pathlib import Path


def main():
    path, material, amount = Path(sys.argv[1]), sys.argv[2], float(sys.argv[3])

    lines = path.read_text().splitlines()
    verts, normals = [], []
    for line in lines:
        if line.startswith("v "):
            verts.append([float(x) for x in line[2:].split()])
        elif line.startswith("vn "):
            normals.append([float(x) for x in line[3:].split()])

    # Which vertices the target material's faces use, and the normal each one carries
    # there. A vertex touched by several faces gets the average, so the lift follows the
    # decal's own curvature rather than one arbitrary face.
    pushed = {}
    current = None
    for line in lines:
        if line.startswith("usemtl"):
            current = line.split()[1]
        elif line.startswith("f ") and current == material:
            for token in line[2:].split():
                parts = token.split("/")
                vi = int(parts[0]) - 1
                if len(parts) < 3 or not parts[2]:
                    continue
                n = normals[int(parts[2]) - 1]
                acc = pushed.setdefault(vi, [0.0, 0.0, 0.0])
                for k in range(3):
                    acc[k] += n[k]

    if not pushed:
        raise SystemExit(f"{path.name}: no faces use material '{material}'")

    for vi, acc in pushed.items():
        length = math.sqrt(sum(c * c for c in acc)) or 1.0
        for k in range(3):
            verts[vi][k] += acc[k] / length * amount

    out, i = [], 0
    for line in lines:
        if line.startswith("v "):
            out.append("v %.5f %.5f %.5f" % tuple(verts[i])); i += 1
        else:
            out.append(line)
    path.write_text("\n".join(out) + "\n", newline="\n")

    print(f"{path.name}: lifted {len(pushed)} vertices of '{material}' by {amount * 1000:.2f} mm")


if __name__ == "__main__":
    main()
