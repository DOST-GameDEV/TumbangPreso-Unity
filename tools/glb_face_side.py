"""Which way does a person rig FACE? Measured off the eyes, not inferred from the toes.

    python tools/glb_face_side.py <file.glb>

WHY THIS EXISTS. `CharacterVisual.PersonModelYaw` is 180 degrees because the rig's
face and Unity's forward disagree, and the Godot header records that more than ten
sessions went looking in the yaw maths before anyone looked at the mesh. Both
engines' importers change the handedness on the way in, so "the face is on -Z" is
a claim about a specific file in a specific space and is worth an actual
measurement rather than a guess off which end of the foot is longer.

The eyes are the only geometry that can answer it: they are the vertices whose UVs
land in the atlas cells the palette's slot 8 owns, on the HEAD mesh.
"""
import sys

sys.path.insert(0, "tools")
from glb_mesh_dump import read_glb, read_accessor  # noqa: E402


def main():
    path = sys.argv[1]
    gltf, buffer = read_glb(path)

    for node in gltf["nodes"]:
        if "mesh" not in node:
            continue

        mesh = gltf["meshes"][node["mesh"]]

        for prim in mesh["primitives"]:
            pos = read_accessor(gltf, buffer, prim["attributes"]["POSITION"])
            uv = read_accessor(gltf, buffer, prim["attributes"]["TEXCOORD_0"])

            slot8 = []
            allz = []

            for i, (u, v) in enumerate(uv):
                col = min(int(u * 16.0), 15)
                row = min(int(v * 16.0), 15)
                allz.append(pos[i][2])

                if row < 8:
                    continue
                if (col // 2) + (8 if row >= 12 else 0) == 8:
                    slot8.append(pos[i])

            print(f"== {node.get('name')} / {mesh.get('name')}")
            print(f"   mesh z spans {min(allz):.4f} to {max(allz):.4f}")

            if not slot8:
                print("   no slot-8 vertices here")
                continue

            zs = [p[2] for p in slot8]
            ys = [p[1] for p in slot8]
            print(f"   slot-8 (face/ink) vertices: {len(slot8)}")
            print(f"      z {min(zs):.4f} to {max(zs):.4f}, mean {sum(zs) / len(zs):.4f}")
            print(f"      y {min(ys):.4f} to {max(ys):.4f}")

            mid = (min(allz) + max(allz)) / 2.0
            side = "-Z (glTF forward)" if sum(zs) / len(zs) < mid else "+Z"
            print(f"      -> this mesh's ink sits on {side}")


if __name__ == "__main__":
    main()
