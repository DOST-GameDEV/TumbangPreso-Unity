import json
import os
import struct

OUT_DIR = "Assets/TumbangPreso/Art/characters/pets"
OUT_GLB = os.path.join(OUT_DIR, "pet-nemu-ghost.glb")

# Palette slot definitions (matching Nemu's palette):
HOODIE_DARK    = 0
HOODIE_SHADOW  = 1
LAVENDER_GLOW  = 2
LAVENDER_PALE  = 3
SHOE_PURPLE    = 4
OFUDA_PURPLE   = 5
HAIR_DARK      = 6
HAIR_HIGHLIGHT = 7
INK            = 8
OFUDA_WHITE    = 9
GRAPHIC_ACCENT = 10
SILVER         = 11
WHITE          = 12
SKIN           = 13
SKIN_DARK      = 14
SKIN_LIT       = 15

# Box tuples: (name, (min_x, min_y, min_z), (max_x, max_y, max_z), slot)
# Authored facing -Z (matching character model rig convention)
GHOST_BOXES = [
    # 1. Cute Chamfered Cube Body (Centered around (0,0,0))
    ("ghost-body-core",        (-0.036, -0.036, -0.036), (0.036, 0.036, 0.036), HOODIE_DARK),
    ("ghost-body-top-rim",     (-0.032, 0.034, -0.032),  (0.032, 0.042, 0.032), HOODIE_SHADOW),
    ("ghost-body-bot-bevel",   (-0.032, -0.042, -0.032), (0.032, -0.034, 0.032), HOODIE_SHADOW),

    # 2. Ultra-Cute Glowing Face (Front -Z, so GLB faces forward with Nemu)
    # Left eye (-X in rig space = character's left / viewer's right in front view)
    ("ghost-eye-l",            (-0.026, -0.006, -0.044), (-0.008, 0.016, -0.035), LAVENDER_GLOW),
    ("ghost-eye-glint-l",      (-0.022, 0.000, -0.046),  (-0.012, 0.012, -0.038), LAVENDER_PALE),
    ("ghost-eye-pupil-l",      (-0.020, 0.002, -0.047),  (-0.014, 0.008, -0.040), INK),

    # Right eye (+X in rig space = character's right / viewer's left in front view)
    ("ghost-eye-r",            (0.008, -0.006, -0.044),  (0.026, 0.016, -0.035), LAVENDER_GLOW),
    ("ghost-eye-glint-r",      (0.012, 0.000, -0.046),   (0.022, 0.012, -0.038), LAVENDER_PALE),
    ("ghost-eye-pupil-r",      (0.014, 0.002, -0.047),   (0.020, 0.008, -0.040), INK),

    # Cute mouth dot
    ("ghost-mouth-dot",        (-0.005, -0.018, -0.044), (0.005, -0.010, -0.035), LAVENDER_GLOW),

    # Glowing cheek blush
    ("ghost-blush-l",          (-0.024, -0.018, -0.042), (-0.012, -0.008, -0.035), GRAPHIC_ACCENT),
    ("ghost-blush-r",          (0.012, -0.018, -0.042),  (0.024, -0.008, -0.035),  GRAPHIC_ACCENT),

    # 3. Stepped Smoky Wispy Ghost Tail (Curving back towards +Z beneath the cube -Y)
    ("ghost-tail-tier1",       (-0.020, -0.062, -0.018), (0.018, -0.036, 0.022), LAVENDER_GLOW),
    ("ghost-tail-tier2",       (-0.028, -0.088, -0.014), (0.008, -0.058, 0.018), LAVENDER_GLOW),
    ("ghost-tail-tier3",       (-0.034, -0.110, -0.010), (-0.004, -0.082, 0.014), LAVENDER_PALE),
    ("ghost-tail-tip-wisp",    (-0.038, -0.126, -0.006), (-0.016, -0.104, 0.010), LAVENDER_PALE),
    ("ghost-tail-tip-glint",   (-0.036, -0.120, -0.002), (-0.024, -0.108, 0.008), OFUDA_WHITE),
]


def cell_uv(slot):
    col = 2 * (slot % 8) + 1
    row = 9 if slot < 8 else 13
    return ((col + 0.5) / 16.0, (row + 0.5) / 16.0)


def build_box(box_min, box_max, slot):
    x0, y0, z0 = box_min
    x1, y1, z1 = box_max
    uv = cell_uv(slot)

    faces = [
        ([(x1, y0, z0), (x1, y1, z0), (x1, y1, z1), (x1, y0, z1)], (1.0, 0.0, 0.0)),
        ([(x0, y0, z1), (x0, y1, z1), (x0, y1, z0), (x0, y0, z0)], (-1.0, 0.0, 0.0)),
        ([(x0, y1, z0), (x0, y1, z1), (x1, y1, z1), (x1, y1, z0)], (0.0, 1.0, 0.0)),
        ([(x0, y0, z1), (x0, y0, z0), (x1, y0, z0), (x1, y0, z1)], (0.0, -1.0, 0.0)),
        ([(x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], (0.0, 0.0, 1.0)),
        ([(x1, y0, z0), (x0, y0, z0), (x0, y1, z0), (x1, y1, z0)], (0.0, 0.0, -1.0)),
    ]

    positions = []
    normals = []
    uvs = []
    indices = []

    for quad, norm in faces:
        base = len(positions)
        for p in quad:
            positions.append(p)
            normals.append(norm)
            uvs.append(uv)
        indices.extend([base, base + 1, base + 2, base, base + 2, base + 3])

    return positions, normals, uvs, indices


def build_mesh():
    all_pos = []
    all_nrm = []
    all_uv = []
    all_idx = []

    for name, bmin, bmax, slot in GHOST_BOXES:
        # Convert -Z authored front to +Z GLB front matching Unity character forward convention
        bmin_f = (bmin[0], bmin[1], -bmax[2])
        bmax_f = (bmax[0], bmax[1], -bmin[2])
        pos, nrm, uv, idx = build_box(bmin_f, bmax_f, slot)
        base = len(all_pos)
        all_pos.extend(pos)
        all_nrm.extend(nrm)
        all_uv.extend(uv)
        all_idx.extend([base + i for i in idx])

    return all_pos, all_nrm, all_uv, all_idx


def export_glb(path, pos, nrm, uv, idx):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    blob = bytearray()
    views = []
    accessors = []

    def align():
        while len(blob) % 4:
            blob.append(0)

    def add(values, fmt, kind, component, minmax=False):
        align()
        start = len(blob)
        for v in values:
            blob.extend(struct.pack("<" + fmt * len(v), *v))

        acc = {
            "bufferView": len(views),
            "componentType": component,
            "count": len(values),
            "type": kind,
        }
        if minmax:
            n = len(values[0])
            acc["min"] = [min(v[a] for v in values) for a in range(n)]
            acc["max"] = [max(v[a] for v in values) for a in range(n)]

        views.append({"buffer": 0, "byteOffset": start, "byteLength": len(blob) - start})
        accessors.append(acc)
        return len(accessors) - 1

    pos_acc = add(pos, "f", "VEC3", 5126, minmax=True)
    nrm_acc = add(nrm, "f", "VEC3", 5126)
    uv_acc = add(uv, "f", "VEC2", 5126)
    idx_acc = add([(i,) for i in idx], "I", "SCALAR", 5125)

    gltf = {
        "asset": {"version": "2.0", "generator": "Tumbang Preso Ghost Pet Builder"},
        "scenes": [{"name": "GhostPet", "nodes": [0]}],
        "scene": 0,
        "nodes": [{"name": "GhostPetRoot", "mesh": 0}],
        "meshes": [{
            "name": "GhostPetMesh",
            "primitives": [{
                "attributes": {
                    "POSITION": pos_acc,
                    "NORMAL": nrm_acc,
                    "TEXCOORD_0": uv_acc,
                },
                "indices": idx_acc,
                "mode": 4,
            }]
        }],
        "accessors": accessors,
        "bufferViews": views,
        "buffers": [{"byteLength": len(blob)}],
    }

    json_bytes = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    while len(json_bytes) % 4:
        json_bytes += b" "

    while len(blob) % 4:
        blob.append(0)

    total_len = 12 + 8 + len(json_bytes) + 8 + len(blob)
    header = struct.pack("<4sII", b"glTF", 2, total_len)
    chunk0 = struct.pack("<I4s", len(json_bytes), b"JSON") + json_bytes
    chunk1 = struct.pack("<I4s", len(blob), b"BIN\x00") + bytes(blob)

    with open(path, "wb") as f:
        f.write(header)
        f.write(chunk0)
        f.write(chunk1)

    print(f"wrote {path} ({len(pos)} verts, {len(idx)//3} tris, {len(GHOST_BOXES)} boxes)")


if __name__ == "__main__":
    pos, nrm, uv, idx = build_mesh()
    export_glb(OUT_GLB, pos, nrm, uv, idx)
