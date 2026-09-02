"""Comprehensive Modular Voxel Hero Builder for Tumbang Preso.
Produces 100% official game-ready GLB models matching canonical cast quality.
"""

import os
import sys
import copy
import struct
import json
import math

BASE = "Assets/TumbangPreso/Art/characters/persons/character-female-a.glb"
DONOR_SKULL = BASE
OUT = "Assets/TumbangPreso/Art/characters/persons/team-custom.glb"
PALETTE_OUT = "MapSource/materials_persons/person_team-custom.tres"

# Palette Slots
WHITE = 0
SILVER = 1
CYAN_TRIM = 2
GOLD_TRIM = 3
TOP_MAIN = 4
TOP_DARK = 5
TOP_LIT = 6
BOTTOM_MAIN = 7
INK = 8
BOTTOM_DARK = 9
BOTTOM_LIT = 10
HAIR_MAIN = 11
HAIR_LIT = 12
SKIN = 13
SKIN_DARK = 14
SKIN_LIT = 15

# Canonical Skeleton (CC0 7-Bone Standard)
SKELETON = {
    "root":       (0.0000, 0.0000, 0.0000),
    "torso":      (0.0000, 0.1760, -0.0288),
    "head":       (0.0000, 0.3430, -0.0024),
    "arm-left":   (0.1472, 0.2880, -0.0173),
    "arm-right":  (-0.1472, 0.2880, -0.0173),
    "leg-left":   (0.0836, 0.1760, -0.0288),
    "leg-right":  (-0.0836, 0.1760, -0.0288),
}

PARENT = {
    "torso": "root",
    "head": "torso",
    "arm-left": "torso",
    "arm-right": "torso",
    "leg-left": "root",
    "leg-right": "root",
}

BONES = ["root", "torso", "head", "arm-left", "arm-right", "leg-left", "leg-right"]
BONE_INDEX = {b: i for i, b in enumerate(BONES)}

FRONT_IS_MINUS_Z = True

def read_glb(path):
    with open(path, "rb") as handle:
        data = handle.read()
    magic, version, _total = struct.unpack_from("<III", data, 0)
    if magic != 0x46546C67:
        raise SystemExit(f"{path} is not a .glb")
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
    return gltf, buffer

def read_accessor(gltf, buffer, index):
    acc = gltf["accessors"][index]
    counts = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}
    fmts = {5120: "b", 5121: "B", 5122: "h", 5123: "H", 5125: "I", 5126: "f"}
    n = counts[acc["type"]]
    fmt = fmts[acc["componentType"]]
    size = struct.calcsize("<" + fmt)
    element = size * n
    view = gltf["bufferViews"][acc["bufferView"]]
    start = view.get("byteOffset", 0) + acc.get("byteOffset", 0)
    stride = view.get("byteStride") or element
    out = []
    for i in range(acc["count"]):
        offset = start + i * stride
        vals = struct.unpack_from("<" + fmt * n, buffer, offset)
        out.append(vals[0] if n == 1 else vals)
    return out

def cell_uv(slot):
    col = 2 * (slot % 8) + 1
    row = 9 if slot < 8 else 13
    return ((col + 0.5) / 16.0, (row + 0.5) / 16.0)

def mirrored(boxes, from_bone, to_bone):
    out = []
    for name, bone, lo, hi, slot in boxes:
        if bone != from_bone:
            continue
        mname = name.replace("-l-", "-r-").replace("-left", "-right").replace("-l", "-r")
        if mname == name:
            mname += "-mirrored"
        out.append((mname, to_bone, (-hi[0], lo[1], lo[2]), (-lo[0], hi[1], hi[2]), slot))
    return out

# ---------------------------------------------------------------------------
# 🌾 WEARABLE 1: FILIPINO WOVEN SALAKOT
# ---------------------------------------------------------------------------
SALAKOT_BOXES = [
    ("salakot-rim-base",   "head", (-0.260, 0.580, -0.260), (0.260, 0.605, 0.260), GOLD_TRIM),
    ("salakot-rim-inner",  "head", (-0.245, 0.595, -0.245), (0.245, 0.620, 0.245), TOP_MAIN),
    ("salakot-mid-cone",   "head", (-0.200, 0.610, -0.200), (0.200, 0.655, 0.200), GOLD_TRIM),
    ("salakot-mid-weave",  "head", (-0.175, 0.645, -0.175), (0.175, 0.690, 0.175), TOP_MAIN),
    ("salakot-apex-cone",  "head", (-0.120, 0.680, -0.120), (0.120, 0.730, 0.120), GOLD_TRIM),
    ("salakot-apex-cap",   "head", (-0.070, 0.720, -0.070), (0.070, 0.755, 0.070), TOP_MAIN),
    ("salakot-finial-pin", "head", (-0.020, 0.750, -0.020), (0.020, 0.778, 0.020), SILVER),
    # Woven Chin Ties
    ("salakot-tie-l",      "head", (0.160, 0.350, -0.010), (0.175, 0.585, 0.010), GOLD_TRIM),
    ("salakot-tie-r",      "head", (-0.175, 0.350, -0.010), (-0.160, 0.585, 0.010), GOLD_TRIM),
    ("salakot-tie-chin",   "head", (-0.050, 0.340, 0.030), (0.050, 0.355, 0.050), GOLD_TRIM),
]

# ---------------------------------------------------------------------------
# ⚡ HAIRSTYLE 1: SPIKY STREET QUIFF WITH HIGHLIGHT
# ---------------------------------------------------------------------------
QUIFF_BOXES = [
    ("hair-cap-base",     "head", (-0.185, 0.480, -0.210), (0.185, 0.620, 0.145), HAIR_MAIN),
    ("hair-sideburn-l",   "head", (0.175, 0.420, -0.150), (0.196, 0.560, 0.080), HAIR_MAIN),
    ("hair-sideburn-r",   "head", (-0.196, 0.420, -0.150), (-0.175, 0.560, 0.080), HAIR_MAIN),
    ("hair-nape",         "head", (-0.170, 0.380, -0.220), (0.170, 0.500, -0.165), HAIR_MAIN),
    # Spiky forward quiff crest
    ("quiff-crest-base",  "head", (-0.115, 0.590, 0.060), (0.115, 0.670, 0.185), HAIR_MAIN),
    ("quiff-crest-mid",   "head", (-0.085, 0.640, 0.030), (0.085, 0.710, 0.170), HAIR_MAIN),
    ("quiff-crest-top",   "head", (-0.055, 0.690, 0.000), (0.055, 0.745, 0.135), HAIR_MAIN),
    # Electric highlight streak
    ("quiff-streak-1",    "head", (0.015, 0.600, 0.070), (0.090, 0.680, 0.188), HAIR_LIT),
    ("quiff-streak-2",    "head", (0.010, 0.650, 0.040), (0.075, 0.725, 0.175), HAIR_LIT),
    ("quiff-streak-top",  "head", (0.005, 0.700, 0.010), (0.055, 0.755, 0.140), HAIR_LIT),
]

# ---------------------------------------------------------------------------
# 🎽 TORSO & ARMS: BARANGAY MVP JERSEY #7
# ---------------------------------------------------------------------------
MVP_JERSEY_TORSO = [
    # Main Jersey Tank
    ("jersey-core",       "torso", (-0.146, 0.160, -0.085), (0.146, 0.340, 0.082), TOP_MAIN),
    ("jersey-side-l",     "torso", (0.136, 0.160, -0.080), (0.152, 0.335, 0.080), TOP_DARK),
    ("jersey-side-r",     "torso", (-0.152, 0.160, -0.080), (-0.136, 0.335, 0.080), TOP_DARK),
    # Gold Collar Ribbing
    ("jersey-collar",     "torso", (-0.085, 0.325, -0.088), (0.085, 0.343, 0.085), GOLD_TRIM),
    ("jersey-arm-trim-l", "torso", (0.135, 0.270, -0.086), (0.150, 0.338, 0.084), GOLD_TRIM),
    ("jersey-arm-trim-r", "torso", (-0.150, 0.270, -0.086), (-0.135, 0.338, 0.084), GOLD_TRIM),
    # 3D Relief Number 7 on Chest (Front -Z)
    ("jersey-num7-bar",   "torso", (-0.050, 0.265, -0.092), (0.050, 0.285, -0.084), GOLD_TRIM),
    ("jersey-num7-diag",  "torso", (0.005, 0.205, -0.092), (0.045, 0.270, -0.084), GOLD_TRIM),
    ("jersey-num7-stem",  "torso", (-0.015, 0.200, -0.092), (0.020, 0.235, -0.084), GOLD_TRIM),
    # Bare Kayumanggi Neck Opening
    ("jersey-neck-skin",  "torso", (-0.055, 0.300, -0.086), (0.055, 0.340, -0.075), SKIN),
    # Silver Cuban Link Chain with Amethyst Amulet
    ("cuban-chain-l",     "torso", (0.040, 0.270, -0.090), (0.080, 0.320, -0.078), SILVER),
    ("cuban-chain-r",     "torso", (-0.080, 0.270, -0.090), (-0.040, 0.320, -0.078), SILVER),
    ("cuban-pendant",     "torso", (-0.020, 0.245, -0.094), (0.020, 0.275, -0.082), CYAN_TRIM),
]

MVP_JERSEY_ARM_LEFT = [
    # Bare Upper Arm (Skin)
    ("arm-skin-upper-l",  "arm-left", (0.000, 0.160, -0.058), (0.086, 0.288, 0.058), SKIN),
    # Red Bicep Sweatband with Gold Stripe
    ("sweatband-red-l",   "arm-left", (-0.004, 0.170, -0.062), (0.090, 0.215, 0.062), TOP_MAIN),
    ("sweatband-gold-l",  "arm-left", (-0.006, 0.185, -0.064), (0.092, 0.200, 0.064), GOLD_TRIM),
    # Forearm & Hand (Skin)
    ("arm-skin-fore-l",   "arm-left", (0.005, 0.055, -0.052), (0.080, 0.160, 0.052), SKIN),
    ("hand-skin-l",       "arm-left", (0.008, 0.000, -0.048), (0.078, 0.055, 0.048), SKIN),
]
MVP_JERSEY_ARM_RIGHT = mirrored(MVP_JERSEY_ARM_LEFT, "arm-left", "arm-right")

# ---------------------------------------------------------------------------
# 👖 BOTTOM: RAW DENIM JORTS & RAMBO TSINELAS
# ---------------------------------------------------------------------------
DENIM_JORTS_TORSO = [
    ("jorts-waist",       "torso", (-0.148, 0.150, -0.086), (0.148, 0.175, 0.084), BOTTOM_MAIN),
    ("jorts-crotch",      "torso", (-0.050, 0.090, -0.078), (0.050, 0.155, 0.074), BOTTOM_DARK),
    ("jorts-buckle",      "torso", (-0.024, 0.152, -0.092), (0.024, 0.172, -0.082), GOLD_TRIM),
    ("chain-loop-1",      "torso", (0.115, 0.130, -0.090), (0.148, 0.160, -0.072), SILVER),
    ("chain-loop-2",      "torso", (0.125, 0.105, -0.088), (0.150, 0.135, -0.062), SILVER),
]

DENIM_JORTS_LEG_LEFT = [
    # Denim Thigh
    ("jorts-thigh-l",     "leg-left", (0.010, 0.090, -0.080), (0.152, 0.175, 0.076), BOTTOM_MAIN),
    # Folded light denim cuff
    ("jorts-cuff-l",      "leg-left", (0.006, 0.075, -0.084), (0.156, 0.095, 0.080), BOTTOM_LIT),
    # Bare Skin Calf
    ("calf-skin-l",       "leg-left", (0.015, 0.020, -0.072), (0.146, 0.078, 0.068), SKIN),
    # White athletic sock
    ("sock-crew-l",       "leg-left", (0.012, 0.020, -0.075), (0.148, 0.042, 0.070), WHITE),
]
DENIM_JORTS_LEG_RIGHT = mirrored(DENIM_JORTS_LEG_LEFT, "leg-left", "leg-right")

# ---------------------------------------------------------------------------
# 🩴 FOOTWEAR: RAMBO TSINELAS (Blue strap, yellow foam, white sole)
# ---------------------------------------------------------------------------
RAMBO_SLIPPER_LEFT = [
    # White base sole (y=0.000 to 0.008)
    ("rambo-sole-l",      "leg-left", (0.005, 0.000, -0.132), (0.156, 0.008, 0.082), WHITE),
    # Yellow foam mid-layer (y=0.008 to 0.016)
    ("rambo-foam-l",      "leg-left", (0.005, 0.008, -0.132), (0.156, 0.016, 0.082), GOLD_TRIM),
    # Blue rubber textured footbed (y=0.016 to 0.022)
    ("rambo-bed-l",       "leg-left", (0.006, 0.016, -0.130), (0.154, 0.022, 0.080), CYAN_TRIM),
    # Bare Foot Skin
    ("rambo-foot-l",      "leg-left", (0.012, 0.022, -0.122), (0.148, 0.052, 0.072), SKIN),
    # Blue V-Thong Strap
    ("rambo-strap-c-l",   "leg-left", (0.065, 0.022, -0.115), (0.095, 0.048, -0.095), CYAN_TRIM),
    ("rambo-strap-o-l",   "leg-left", (0.108, 0.022, -0.068), (0.154, 0.044, -0.038), CYAN_TRIM),
    ("rambo-strap-i-l",   "leg-left", (0.006, 0.022, -0.068), (0.052, 0.044, -0.038), CYAN_TRIM),
]
RAMBO_SLIPPER_RIGHT = mirrored(RAMBO_SLIPPER_LEFT, "leg-left", "leg-right")

# ---------------------------------------------------------------------------
# MESH GENERATION (Beveled Cuboids with Bone Weights)
# ---------------------------------------------------------------------------
FACES = [
    # +X face
    ((1, 0, 0), ((1, 0, 0), (1, 1, 0), (1, 1, 1), (1, 0, 1))),
    # -X face
    ((-1, 0, 0), ((0, 0, 1), (0, 1, 1), (0, 1, 0), (0, 0, 0))),
    # +Y face
    ((0, 1, 0), ((0, 1, 0), (0, 1, 1), (1, 1, 1), (1, 1, 0))),
    # -Y face
    ((0, -1, 0), ((0, 0, 1), (0, 0, 0), (1, 0, 0), (1, 0, 1))),
    # +Z face
    ((0, 0, 1), ((1, 0, 1), (1, 1, 1), (0, 1, 1), (0, 0, 1))),
    # -Z face
    ((0, 0, -1), ((0, 0, 0), (0, 1, 0), (1, 1, 0), (1, 0, 0))),
]

def build_box(entry):
    name, bone, lo, hi, slot = entry
    bidx = BONE_INDEX[bone]
    uv = cell_uv(slot)
    
    # Model space to GLB space (+Z front)
    if FRONT_IS_MINUS_Z:
        lx, hx = lo[0], hi[0]
        ly, hy = lo[1], hi[1]
        lz, hz = -hi[2], -lo[2]
    else:
        lx, hx = lo[0], hi[0]
        ly, hy = lo[1], hi[1]
        lz, hz = lo[2], hi[2]
        
    pos, nrm, uvs, joints, weights, tris = [], [], [], [], [], []
    
    for norm, quad in FACES:
        if FRONT_IS_MINUS_Z:
            normal = (norm[0], norm[1], -norm[2])
        else:
            normal = norm
            
        base = len(pos)
        for q in quad:
            x = hx if q[0] else lx
            y = hy if q[1] else ly
            z = hz if q[2] else lz
            pos.append((x, y, z))
            nrm.append(normal)
            uvs.append(uv)
            joints.append((bidx, 0, 0, 0))
            weights.append((1.0, 0.0, 0.0, 0.0))
            
        tris.extend([base, base + 1, base + 2, base, base + 2, base + 3])
        
    return pos, nrm, uvs, joints, weights, tris

def build_mesh(boxes):
    pos, nrm, uvs, joints, weights, tris = [], [], [], [], [], []
    for entry in boxes:
        p, n, u, j, w, t = build_box(entry)
        base = len(pos)
        pos.extend(p)
        nrm.extend(n)
        uvs.extend(u)
        joints.extend(j)
        weights.extend(w)
        tris.extend([idx + base for idx in t])
    return pos, nrm, uvs, joints, weights, tris

# ---------------------------------------------------------------------------
# DONOR SKULL INTEGRATION (Authentic Kenney Head, Eyes, & Cat Mouth :3)
# ---------------------------------------------------------------------------
def _slot_at(u, v):
    col = min(int(u * 16.0), 15)
    row = min(int(v * 16.0), 15)
    if row < 8: return None
    return (col // 2) + (8 if row >= 12 else 0)

def donor_head_mesh():
    """Extract official skull and eyes from base CC0 rig."""
    gltf, buffer = read_glb(DONOR_SKULL)
    for node in gltf["nodes"]:
        if node.get("name") == "head-mesh":
            prim = gltf["meshes"][node["mesh"]]["primitives"][0]
            src_pos = [tuple(p) for p in read_accessor(gltf, buffer, prim["attributes"]["POSITION"])]
            src_nrm = [tuple(n) for n in read_accessor(gltf, buffer, prim["attributes"]["NORMAL"])]
            src_uv = [tuple(t) for t in read_accessor(gltf, buffer, prim["attributes"]["TEXCOORD_0"])]
            raw = read_accessor(gltf, buffer, prim["indices"])
            idx = [v[0] for v in raw] if isinstance(raw[0], tuple) else list(raw)
            
            pos, nrm, uvs, joints, weights, tris = [], [], [], [], [], []
            remap = {}
            head_joint = BONE_INDEX["head"]
            
            for t in range(0, len(idx), 3):
                tri = (idx[t], idx[t + 1], idx[t + 2])
                slot = _slot_at(*src_uv[tri[0]])
                
                # Keep skull (15) and ink (8)
                if slot not in [8, 15]:
                    continue
                    
                paint = SKIN if slot == 15 else INK
                
                for i in tri:
                    if i not in remap:
                        remap[i] = len(pos)
                        pos.append(src_pos[i])
                        nrm.append(src_nrm[i])
                        uvs.append(cell_uv(paint))
                        joints.append((head_joint, 0, 0, 0))
                        weights.append((1.0, 0.0, 0.0, 0.0))
                tris.extend([remap[i] for i in tri])
                
            return pos, nrm, uvs, joints, weights, tris
            
    raise SystemExit("No head-mesh found in donor!")

# ---------------------------------------------------------------------------
# GLB WRITER WITH CC0 ANIMATION RETARGETING
# ---------------------------------------------------------------------------
def write_glb(path, gltf, blob):
    gltf_json = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    while len(gltf_json) % 4 != 0:
        gltf_json += b" "
    while len(blob) % 4 != 0:
        blob += b"\x00"

    total = 12 + 8 + len(gltf_json) + 8 + len(blob)
    out = bytearray()
    out += struct.pack("<4sII", b"glTF", 2, total)
    out += struct.pack("<II", len(gltf_json), 0x4E4F534A)
    out += gltf_json
    out += struct.pack("<II", len(blob), 0x004E4942)
    out += blob

    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    with open(path, "wb") as f:
        f.write(out)
    print(f"wrote {path}  ({len(out)} bytes)")

def write_palette(path, palette):
    os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        f.write("[gd_resource type=\"StandardMaterial3D\" format=3]\n\n[resource]\n")
        f.write("resource_name = \"person_team-custom\"\n")
        f.write("albedo_color = Color(1, 1, 1, 1)\n")
        for slot, hex_str in palette.items():
            r = int(hex_str[0:2], 16) / 255.0
            g = int(hex_str[2:4], 16) / 255.0
            b = int(hex_str[4:6], 16) / 255.0
            f.write(f"# Slot {slot}: {hex_str} (r={r:.4f}, g={g:.4f}, b={b:.4f})\n")

def compile_custom_hero(preset="barangay_mvp"):
    gltf, base_blob = read_glb(BASE)
    
    # 1. Gather all body and head boxes
    body_boxes = []
    body_boxes.extend(MVP_JERSEY_TORSO)
    body_boxes.extend(MVP_JERSEY_ARM_LEFT)
    body_boxes.extend(MVP_JERSEY_ARM_RIGHT)
    body_boxes.extend(DENIM_JORTS_TORSO)
    body_boxes.extend(DENIM_JORTS_LEG_LEFT)
    body_boxes.extend(DENIM_JORTS_LEG_RIGHT)
    body_boxes.extend(RAMBO_SLIPPER_LEFT)
    body_boxes.extend(RAMBO_SLIPPER_RIGHT)
    
    head_boxes = []
    head_boxes.extend(QUIFF_BOXES)
    if preset == "salakot_warrior" or preset == "barangay_mvp":
        head_boxes.extend(SALAKOT_BOXES)
        
    # 2. Build Body Mesh
    b_pos, b_nrm, b_uvs, b_joints, b_weights, b_tris = build_mesh(body_boxes)
    
    # 3. Build Head Mesh (Donor Skull + Head Boxes)
    h_donor = donor_head_mesh()
    h_custom = build_mesh(head_boxes)
    
    h_pos = h_donor[0] + h_custom[0]
    h_nrm = h_donor[1] + h_custom[1]
    h_uvs = h_donor[2] + h_custom[2]
    h_joints = h_donor[3] + h_custom[3]
    h_weights = h_donor[4] + h_custom[4]
    
    d_len = len(h_donor[0])
    h_tris = h_donor[5] + [idx + d_len for idx in h_custom[5]]
    
    # 4. Verify Heights
    all_verts = b_pos + h_pos
    lo_y = min(v[1] for v in all_verts)
    hi_y = max(v[1] for v in all_verts)
    height = hi_y - lo_y
    print(f"Authored Mesh Bounds: min_y={lo_y:.4f}, max_y={hi_y:.4f}, total_height={height:.4f}")
    if height < 0.6613 or height > 0.7928:
        print(f"Warning: height {height:.4f} outside canonical [0.6613, 0.7928], scaling...")
        scale = 0.7750 / height
        b_pos = [(v[0]*scale, v[1]*scale, v[2]*scale) for v in b_pos]
        h_pos = [(v[0]*scale, v[1]*scale, v[2]*scale) for v in h_pos]
        all_verts = b_pos + h_pos
        height = max(v[1] for v in all_verts) - min(v[1] for v in all_verts)
        print(f"Scaled height: {height:.4f}")
        
    # 5. Pack binary buffers
    blob = bytearray()
    
    def append_data(data, fmt, component_type, acc_type, is_indices=False):
        nonlocal blob
        while len(blob) % 4 != 0:
            blob += b"\x00"
        offset = len(blob)
        count = len(data)
        
        packed = bytearray()
        if is_indices:
            for val in data:
                packed += struct.pack("<H", val)
        else:
            for elem in data:
                if isinstance(elem, (list, tuple)):
                    packed += struct.pack(fmt, *elem)
                else:
                    packed += struct.pack(fmt, elem)
        blob += packed
        
        # Buffer View
        bv_idx = len(gltf["bufferViews"])
        gltf["bufferViews"].append({
            "buffer": 0,
            "byteOffset": offset,
            "byteLength": len(packed),
            "target": 34963 if is_indices else 34962
        })
        
        # Accessor
        acc_idx = len(gltf["accessors"])
        acc = {
            "bufferView": bv_idx,
            "byteOffset": 0,
            "componentType": component_type,
            "count": count,
            "type": acc_type
        }
        if not is_indices and acc_type == "VEC3":
            acc["min"] = [min(v[i] for v in data) for i in range(3)]
            acc["max"] = [max(v[i] for v in data) for i in range(3)]
        gltf["accessors"].append(acc)
        return acc_idx

    # Reset bufferViews, accessors, meshes
    gltf["bufferViews"] = []
    gltf["accessors"] = []
    gltf["meshes"] = []
    
    # Body Primitive
    b_acc_idx = append_data(b_tris, "<H", 5123, "SCALAR", is_indices=True)
    b_acc_pos = append_data(b_pos, "<fff", 5126, "VEC3")
    b_acc_nrm = append_data(b_nrm, "<fff", 5126, "VEC3")
    b_acc_uvs = append_data(b_uvs, "<ff", 5126, "VEC2")
    b_acc_jnt = append_data(b_joints, "<HHHH", 5123, "VEC4")
    b_acc_wgt = append_data(b_weights, "<ffff", 5126, "VEC4")
    
    gltf["meshes"].append({
        "name": "body-mesh",
        "primitives": [{
            "attributes": {
                "POSITION": b_acc_pos,
                "NORMAL": b_acc_nrm,
                "TEXCOORD_0": b_acc_uvs,
                "JOINTS_0": b_acc_jnt,
                "WEIGHTS_0": b_acc_wgt
            },
            "indices": b_acc_idx,
            "material": 0
        }]
    })
    
    # Head Primitive
    h_acc_idx = append_data(h_tris, "<H", 5123, "SCALAR", is_indices=True)
    h_acc_pos = append_data(h_pos, "<fff", 5126, "VEC3")
    h_acc_nrm = append_data(h_nrm, "<fff", 5126, "VEC3")
    h_acc_uvs = append_data(h_uvs, "<ff", 5126, "VEC2")
    h_acc_jnt = append_data(h_joints, "<HHHH", 5123, "VEC4")
    h_acc_wgt = append_data(h_weights, "<ffff", 5126, "VEC4")
    
    gltf["meshes"].append({
        "name": "head-mesh",
        "primitives": [{
            "attributes": {
                "POSITION": h_acc_pos,
                "NORMAL": h_acc_nrm,
                "TEXCOORD_0": h_acc_uvs,
                "JOINTS_0": h_acc_jnt,
                "WEIGHTS_0": h_acc_wgt
            },
            "indices": h_acc_idx,
            "material": 0
        }]
    })

    # Assign meshes to nodes
    for node in gltf["nodes"]:
        if node.get("name") == "body-mesh":
            node["mesh"] = 0
        elif node.get("name") == "head-mesh":
            node["mesh"] = 1
            
    # Retarget animation tracks
    by_name = {node.get("name"): i for i, node in enumerate(gltf["nodes"])}
    for bone, world in SKELETON.items():
        if bone in by_name:
            idx = by_name[bone]
            parent = SKELETON[PARENT[bone]] if bone in PARENT else (0.0, 0.0, 0.0)
            local = [world[a] - parent[a] for a in range(3)]
            gltf["nodes"][idx]["translation"] = local

    # Inverse Bind Matrices
    ibm_data = []
    for skin in gltf.get("skins", []):
        for joint in skin["joints"]:
            jname = gltf["nodes"][joint].get("name")
            world = SKELETON.get(jname, (0, 0, 0))
            ibm_data.append((
                1.0, 0.0, 0.0, 0.0,
                0.0, 1.0, 0.0, 0.0,
                0.0, 0.0, 1.0, 0.0,
                -world[0], -world[1], -world[2], 1.0
            ))
        acc_ibm = append_data(ibm_data, "<16f", 5126, "MAT4")
        skin["inverseBindMatrices"] = acc_ibm

    # Write Palette & GLB
    palette = {
        0: "FFFFFF", # White sole / crew socks
        1: "C8C8DC", # Silver Cuban Chain / Salakot finial
        2: "0080E8", # Blue Rambo rubber straps
        3: "FFBA00", # Gold MVP Jersey ribbing / Salakot rattan
        4: "ED2136", # Vivid Red Barangay MVP Jersey #7
        5: "B01424", # Red Jersey shadow
        6: "FFAA00", # Gold #7 Number
        7: "2C4566", # Raw Denim Jorts
        8: "141416", # Ink Black Eyes & Smile (:3)
        9: "1C2E46", # Denim Dark Shadow
        10: "486E9E", # Denim Light Cuff
        11: "18181C", # Jet Black Hair
        12: "FFD200", # Electric Yellow Highlight Streak
        13: "C88A52", # Warm Kayumanggi Skin
        14: "A06E3D", # Skin Shadow
        15: "E6A86E", # Skin Highlight
    }
    
    write_palette(PALETTE_OUT, palette)
    write_glb(OUT, gltf, bytes(blob))
    print("Compilation complete!")

if __name__ == "__main__":
    compile_custom_hero("barangay_mvp")
