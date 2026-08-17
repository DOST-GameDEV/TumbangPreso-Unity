"""Dumps the structure of a .glb: nodes, skins, meshes, animations.

    python tools/glb_dump.py Assets/TumbangPreso/Art/characters/persons/character-female-b.glb

WHY THIS EXISTS. The person rigs are binary CC0 assets and every property the
game depends on lives inside them: the bone NAMES that `CharacterVisual` hunts
for a hand anchor, the clip NAMES that `CharacterAnimator` selects by, the
authored height that `PersonScale` multiplies, and the UV cells that the palette
shader remaps. All four are invisible in the file browser, and guessing any of
them fails silently rather than loudly.
"""
import json
import struct
import sys


def read_glb(path):
    with open(path, "rb") as handle:
        data = handle.read()

    magic, version, _total = struct.unpack_from("<III", data, 0)
    if magic != 0x46546C67:
        raise SystemExit(f"{path} is not a .glb (magic {magic:#x})")

    offset = 12
    gltf = None
    buffer = None

    while offset < len(data):
        length, kind = struct.unpack_from("<II", data, offset)
        offset += 8
        chunk = data[offset:offset + length]
        offset += length

        if kind == 0x4E4F534A:
            gltf = json.loads(chunk.decode("utf-8"))
        elif kind == 0x004E4942:
            buffer = chunk

    return gltf, buffer, version


def main():
    path = sys.argv[1]
    gltf, buffer, version = read_glb(path)

    print(f"== {path}")
    print(f"glb version {version}, bin chunk {len(buffer)} bytes")
    print(f"generator: {gltf.get('asset', {}).get('generator')}")

    for key in ("scenes", "nodes", "meshes", "skins", "animations",
                "materials", "textures", "images", "accessors", "bufferViews"):
        print(f"{key}: {len(gltf.get(key, []))}")

    print("\n-- nodes")
    for i, node in enumerate(gltf.get("nodes", [])):
        bits = [f"[{i}] {node.get('name')}"]
        for field in ("mesh", "skin", "camera"):
            if field in node:
                bits.append(f"{field}={node[field]}")
        if "children" in node:
            bits.append(f"children={node['children']}")
        for field in ("translation", "rotation", "scale"):
            if field in node:
                bits.append(f"{field}={[round(v, 5) for v in node[field]]}")
        print("   " + "  ".join(bits))

    print("\n-- skins")
    for i, skin in enumerate(gltf.get("skins", [])):
        print(f"   [{i}] {skin.get('name')}  joints={len(skin['joints'])}  "
              f"skeleton={skin.get('skeleton')}  ibm={skin.get('inverseBindMatrices')}")
        names = [gltf["nodes"][j].get("name") for j in skin["joints"]]
        print(f"       joints: {names}")

    print("\n-- meshes")
    for i, mesh in enumerate(gltf.get("meshes", [])):
        print(f"   [{i}] {mesh.get('name')}  primitives={len(mesh['primitives'])}")
        for j, prim in enumerate(mesh["primitives"]):
            attrs = {k: v for k, v in prim["attributes"].items()}
            counts = {k: gltf["accessors"][v]["count"] for k, v in attrs.items()}
            idx = gltf["accessors"][prim["indices"]]["count"] if "indices" in prim else 0
            print(f"       prim {j}: material={prim.get('material')} indices={idx} {counts}")

    print("\n-- materials")
    for i, mat in enumerate(gltf.get("materials", [])):
        pbr = mat.get("pbrMetallicRoughness", {})
        print(f"   [{i}] {mat.get('name')}  baseColorTexture="
              f"{pbr.get('baseColorTexture', {}).get('index')}  "
              f"baseColorFactor={pbr.get('baseColorFactor')}")

    print("\n-- animations")
    for i, anim in enumerate(gltf.get("animations", [])):
        print(f"   [{i}] {anim.get('name')}  channels={len(anim['channels'])} "
              f"samplers={len(anim['samplers'])}")


if __name__ == "__main__":
    main()
