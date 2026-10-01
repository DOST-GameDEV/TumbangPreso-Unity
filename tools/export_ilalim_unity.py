"""Export the Ilalim ng Tulay rebuild (ArtSource/ilalim/ilalim_city.blend) for Unity (ILALIM-1.4).

  blender -b ArtSource/ilalim/ilalim_city.blend --python tools/export_ilalim_unity.py [-- --survey]

Writes, and touches nothing else:

  Assets/TumbangPreso/Art/IlalimRebuild/Models/<model>.glb    one file per PROTOTYPE mesh
  Assets/TumbangPreso/Art/IlalimRebuild/Textures/<name>.png   every image an exported material reads
  Assets/TumbangPreso/Art/IlalimRebuild/ilalim_layout.json    materials, placements, sun, haze, anchors

With `-- --survey` it only prints every material's traced colour chain and writes nothing.

⚠️ THE .blend IS NEVER SAVED, and neither is any kit file. The city LIBRARY-LINKS every kit, so
nothing in it can be edited in place anyway: every mesh that goes out is first copied into a new
LOCAL mesh in memory (bpy.data.meshes.new_from_object on the evaluated object, so the Bevel and
Solidify modifiers are applied and every UV map is kept), and every material the .glb names is a
new local stub carrying only the name. The real material is described in the layout JSON and
built in Unity (Editor/MapKit/IlalimSceneBuilder.cs). The process exits without saving.

PROTOTYPES AND PLACEMENTS. The walk is the evaluated depsgraph's object instances, so linked
duplicates (the LRT kit's spans and piers, the street kit's furniture, 1174 trees and lilies),
collection instances (the lilies, the train, the traffic, the streetlife vehicles) and plain
objects are one case: every rendered mesh instance is a placement with its FULL world matrix,
and every distinct mesh (per library, per modifier stack) is written once as a prototype .glb.
A mesh used once is still a prototype placed once: that keeps object names (the Unity builder
finds the pisonet, the pares cart and the hoop by them) and costs nothing at run time.
Skipped: the street kit's "median plant placeholders" (the city hides them and places lilies),
the buildings landmarks.json lists under "hide" (the city hides them only for its session), and
any object with hide_render.

⚠️ COORDINATES. THIS MAP IS NOT KANTO'S OR THE LAGOON'S FRAME. The Ilalim kits were modelled in
the GAME's own frame: Blender X is the game's x (east), Blender Y is the game's z (north), origin
the court centre on Taft (author_ilalim_lrt.py and every kit docstring). So a Blender point
(x, y, z) must land at Unity (x, z, y), not at Kanto's (-x, z, -y). The two differ by a 180
degree turn about the vertical (diag(-1, 1, -1)), a proper rotation, so signs still read the
right way round. Proof, read off the current IlalimNgTulay scene by the Unity builder's
ProveFrame step: the bridge hoop (Blender (-8.9, -10)) and the pares cart (8.8, -5) sit at the
same x and z in the current Unity map, and the piers at (+/-4.45, +/-10). The pad and the
pisonet row moved on purpose (guide section 0.6, decision 1: the shops went to the east
pavement).
  glTF (+Y up) plus glTFast's X negation puts a prototype's local Blender point p at C.p with
  C = [[-1,0,0],[0,0,1],[0,-1,0]] (Kanto's matrix). The placement is therefore
      U = D . M_blender . C^T,   D = [[1,0,0],[0,0,1],[0,1,0]]
  (translation mapped by D), written row-major, so U . (C.p) = D . (M.p). The script proves it
  at the end: vertices read back from the written .glb files, X-negated as glTFast does, pushed
  through each proof placement's U, are compared with the same Blender instance's world
  vertices mapped by D (a pier, a mirrored span, a tree, a lily inside a collection instance, a
  parked vehicle, the train).

UV CHANNELS AND THE GRIME. The kits paint positional grime as MULTIPLY overlays on their own UV
maps (UVGrime: rain tongues below an edge; UVSplash: road splash above the ground; UVSill: the
heritage sill stains). They are kept as extra TEXCOORDs, not baked into the albedo: the albedo is
a tiling world-scale texture shared by hundreds of faces, so a bake would need a unique unwrap of
every building (millions of texels) and would lose the tiling's resolution. Every exported mesh
has its UV maps rewritten in ONE canonical order so the shader can hard-wire the channels:
  TEXCOORD_0 the painted UV (the mesh's first map, UVMap or Float2, the one every '' UV node reads)
  TEXCOORD_1 UVGrime, TEXCOORD_2 UVSplash, TEXCOORD_3 UVSill
Only up to the highest channel the mesh's materials sample; a missing map is written as zeros,
exactly what Blender feeds a UV node naming a map the mesh lacks.

⚠️ THE ONE BAKE: THE LRT PIERS. Every pier must render on TumbangPreso/NearFade (the AO NearGuard
finds near-fade renderers by that shader name: ILALIM_REWORK_GUIDE.md section 1.7), and that
shader has one albedo and one normal map on one UV. So the pier prototype alone gets a unique
UV (Smart UV Project) and its full colour chain (lrt_pier sand, lrt_pier_cap grey, both grime
overlays) and its normal maps are baked with Cycles into lrt_pier_nearfade_albedo.png and
lrt_pier_nearfade_normal.png (tangent space, MikkTSpace; the pier .glb carries TANGENTs made
from the same UV, so the baked normals are read in the frame they were baked in).

MATERIALS. Each used material is traced from its Base Color socket back to its albedo image
through the chains the kits build, into an ordered recipe the Unity shader
TumbangPreso/IlalimPainted reproduces:
  albedo on TEXCOORD_0 (a Mapping node's scale, rotation and offset kept), up to two anti-tiling
  resamples (tools/ilalim_antitile.py and the LRT kit's one-sample version: rotated, scaled,
  offset copies of the same image through a feathered Blender noise mask), a saturation
  (Hue/Saturation), constant tints, then overlays in chain order: multiply (grime) or mix by the
  overlay's own alpha (the tree kit's limewash), each on its TEXCOORD.
  Leaves and flowers (the Kanto and Lagoon card recipe with the Object Info nudge) go to the
  existing TumbangPreso/LagoonFoliage. The one blended glass (the pares cart case) goes to a
  transparent Standard. Anything the tracer does not recognise falls back to the material's
  viewport colour (each kit sets it to the texture's average) and is LISTED in the log and in
  the layout ("fallback" notes), never silently.
  Emission: the Emission Strength times its colour, or times the albedo chain when the kit feeds
  it the albedo (screens, lightboxes, destination blinds). Cutout: an albedo alpha feeding the
  BSDF Alpha (grilles, the roof lattice, the crane, leaves). Every material draws both sides
  where it is cut out or where more than a quarter of its faces' edges are open (thin sheets):
  Blender draws both sides of everything here (no kit culls back faces).

COLOURS IN THE JSON are sRGB-ENCODED, as Kanto's and the Lagoon's: `new Color(r, g, b)` in the
linear-space project lands on Blender's linear value. Values above 1 are encoded on the same
curve. Overlay and normal images are marked "linear" when Blender reads them Non-Color.

Nothing is decimated or simplified (WORKING_RULES: do not decimate as a speculative optimization):
modifiers are applied, every used UV map is kept. Triangle counts per kit are logged and written
into the layout ("budget") for the Unity builder's measures (LOD culling of small far things).
"""
import json
import math
import shutil
import struct
import sys
from collections import Counter, defaultdict
from pathlib import Path

import bpy
import numpy as np
from mathutils import Matrix, Vector

TOOLS = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS))
ROOT = TOOLS.parent
OUT = ROOT / "Assets" / "TumbangPreso" / "Art" / "IlalimRebuild"
MODELS = OUT / "Models"
TEXTURES = OUT / "Textures"
LAYOUT = OUT / "ilalim_layout.json"
SOURCE = ROOT / "ArtSource" / "ilalim"
TAG = "[ilalim-unity]"

# Blender (x, y, z) -> Unity (x, z, y): the game frame (see the docstring).
D3 = Matrix(((1, 0, 0), (0, 0, 1), (0, 1, 0)))
D4 = D3.to_4x4()
# glTF + glTFast: a prototype's local Blender point p lands at C.p in Unity.
C3 = Matrix(((-1, 0, 0), (0, 0, 1), (0, -1, 0)))
C4T = C3.to_4x4().transposed()

CHANNEL = {"UVGrime": 1, "UVSplash": 2, "UVSill": 3}
OVERLAY_LAYERS = ("UVGrime", "UVSplash", "UVSill")

# The city root's placed collections, by kit (Unity groups; colliders and statics follow them).
GROUP_OF = {
    "guideway over the court": "Guideway",
    "rizal_oblation": "RizalHall", "rizal_hall": "RizalHall",
    "heritage west of Taft": "Heritage",
    "eastside": "Eastside",
    "street median planter": "Street", "street markings and chalk": "StreetMarkings",
    "street ground": "StreetGround", "street furniture (placed)": "StreetFurniture",
    "street fences and walls": "StreetFences",
    "trees over Ilalim": "Trees", "median lilies (from placeholders)": "Lilies",
    "review placement (column signs)": "ColumnSigns", "props (placed)": "Props",
    "sarisari (placed)": "SariSari", "rooftops (placed)": "Rooftops",
    "streetlife (placed)": "StreetLife", "landmarks (placed)": "Landmarks",
    "stations (placed)": "Stations", "lrt train (instance)": "Train", "traffic (instances)": "Traffic",
}
# Taken from tools/author_ilalim_city.py (HAZE_START, HAZE_DEPTH, HAZE_CAP, HAZE): imported, not copied.
PIER_MESH = "lrt_pier"
BAKE_SIZE = 2048
# Footprint colliders (see footprint()): which groups, how low counts as the base, the cap.
FOOTPRINT_GROUPS = {"Props", "StreetFurniture", "StreetFences", "Trees", "StreetLife", "SariSari", "Eastside",
                    "Heritage"}
FOOT_REACH, FOOT_CAP, GROUNDED, COMPACT = 1.2, 6.0, 1.0, 3.0
REACH_X, REACH_Z = 11.0, 16.5
PROOF_MESHES = ("lrt_pier", "lrt_span_25_parapet", "tree_mango_0_leaves", "tree_lily_0", "train_body")


def log(*a):
    print(TAG, *a, flush=True)


def unity_matrix(m):
    r = D4 @ m @ C4T
    return [round(r[i][j], 6) for i in range(4) for j in range(4)]


def game_point(v):
    return [round(v.x, 5), round(v.z, 5), round(v.y, 5)]


def srgb(c):
    if c <= 0.0031308:
        return c * 12.92
    return 1.055 * c ** (1 / 2.4) - 0.055


def enc(rgb):
    return [round(srgb(max(0.0, c)), 5) for c in rgb]


def safe(name):
    return "".join(ch if ch.isalnum() or ch in "_-" else "_" for ch in name).strip("_")


def lib_of(idb):
    return idb.library.name if idb is not None and idb.library is not None else "local"


# ====================================================================== the colour chain tracer

def _in(node, identifier):
    return next(i for i in node.inputs if i.identifier == identifier)


def _src(sock):
    return sock.links[0].from_node if sock.is_linked else None


def _src_socket(sock):
    return sock.links[0].from_socket if sock.is_linked else None


def image_file(img):
    """The PNG an image node reads, as an absolute path (library-relative paths resolved)."""
    if img is None:
        return None
    p = bpy.path.abspath(img.filepath, library=img.library) if img.filepath else ""
    return Path(p) if p else None


def uv_of(tex):
    """Which UV a texture node reads, and the Mapping between: (layer name or '', mapping or None)."""
    vec = tex.inputs["Vector"]
    n = _src(vec)
    mapping = None
    if n is not None and n.type == "MAPPING":
        mapping = {"scale": list(n.inputs["Scale"].default_value[:2]),
                   "rot": math.degrees(n.inputs["Rotation"].default_value[2]),
                   "loc": list(n.inputs["Location"].default_value[:2]), "type": n.vector_type}
        if any(abs(a) > 1e-6 for a in n.inputs["Rotation"].default_value[:2]):
            mapping["unsupported"] = "x/y rotation"
        n = _src(n.inputs["Vector"])
    if n is None:
        return "", mapping
    if n.type == "UVMAP":
        return n.uv_map, mapping
    if n.type == "TEX_COORD":
        return "", mapping
    return f"?{n.type}", mapping


def channel_of(layer, first_names):
    if layer == "" or layer in first_names:
        return 0
    return CHANNEL.get(layer, -1)


def trace(sock, first_names):
    """The Base Color chain as a recipe. Walks from the BSDF inward; ops are returned outward
    (albedo first). Unknown nodes end the walk and are reported."""
    out = {"albedo": None, "albedoUV": 0, "mapping": None, "const": None, "antiTile": [],
           "ops": [], "nudge": None, "unknown": []}
    ops = []
    s = sock
    if not s.is_linked:
        out["const"] = list(s.default_value[:3])
        return out
    while s is not None and s.is_linked:
        n = _src(s)
        if n.type == "TEX_IMAGE":
            out["albedo"] = n.image
            out["albedoClamp"] = n.extension in ("EXTEND", "CLIP")
            layer, mapping = uv_of(n)
            out["albedoUV"] = channel_of(layer, first_names)
            out["mapping"] = mapping
            if out["albedoUV"] != 0:
                out["unknown"].append(f"albedo on UV '{layer}'")
            break
        if n.type == "RGB":
            out["const"] = list(n.outputs[0].default_value[:3])
            break
        if n.type == "MIX" and n.data_type == "RGBA":
            fac, a, b = _in(n, "Factor_Float"), _in(n, "A_Color"), _in(n, "B_Color")
            bn = _src(b)
            if n.blend_type == "MULTIPLY" and not fac.is_linked and abs(fac.default_value - 1) < 1e-6:
                if bn is None:
                    ops.append({"op": "mul", "rgb": list(b.default_value[:3])})
                elif bn.type == "RGB":
                    ops.append({"op": "mul", "rgb": list(bn.outputs[0].default_value[:3])})
                elif bn.type == "TEX_IMAGE":
                    layer, mapping = uv_of(bn)
                    ops.append({"op": "mulTex", "image": bn.image, "uv": channel_of(layer, first_names),
                                "layer": layer, "mapping": mapping, "clamp": bn.extension in ("EXTEND", "CLIP")})
                else:
                    out["unknown"].append(f"multiply by {bn.type}")
                    break
                s = a
                continue
            if n.blend_type == "MIX" and fac.is_linked:
                fn = _src(fac)
                if fn.type == "MAP_RANGE" and _src(fn.inputs["Value"]) is not None \
                        and _src(fn.inputs["Value"]).type == "TEX_NOISE" and bn is not None and bn.type == "TEX_IMAGE":
                    noise = _src(fn.inputs["Value"])
                    layer, mapping = uv_of(bn)
                    seed = [0.0, 0.0, 0.0]
                    nv = _src(noise.inputs["Vector"])
                    if nv is not None and nv.type == "VECT_MATH" and nv.operation == "ADD":
                        seed = list(nv.inputs[1].default_value)
                    out["antiTile"].append({
                        "rot": round(mapping["rot"], 4) if mapping else 0.0,
                        "scale": round(mapping["scale"][0], 5) if mapping else 1.0,
                        "offset": [round(v, 5) for v in (mapping["loc"] if mapping else (0, 0))],
                        "maskScale": round(noise.inputs["Scale"].default_value, 5),
                        "maskDetail": round(noise.inputs["Detail"].default_value, 3),
                        "seed": [round(v, 4) for v in seed],
                        "lo": round(fn.inputs["From Min"].default_value, 4),
                        "hi": round(fn.inputs["From Max"].default_value, 4),
                        "smooth": fn.interpolation_type == "SMOOTHSTEP",
                        "image": bn.image})
                    s = a
                    continue
                if fn.type == "TEX_IMAGE" and _src_socket(fac).name == "Alpha" and bn is not None and bn.name == fn.name:
                    layer, mapping = uv_of(fn)
                    ops.append({"op": "mixTex", "image": fn.image, "uv": channel_of(layer, first_names),
                                "layer": layer, "mapping": mapping, "clamp": fn.extension in ("EXTEND", "CLIP")})
                    s = a
                    continue
                out["unknown"].append(f"mix by {fn.type}")
                break
            if n.blend_type == "MIX" and not fac.is_linked and bn is None:
                ops.append({"op": "mixConst", "rgb": list(b.default_value[:3]), "f": fac.default_value})
                s = a
                continue
            out["unknown"].append(f"MIX {n.blend_type}")
            break
        if n.type == "HUE_SAT":
            hue, sat, val, fac = (n.inputs[k] for k in ("Hue", "Saturation", "Value", "Fac"))
            if hue.is_linked or val.is_linked:
                def rng(sock_):
                    mr = _src(sock_)
                    if mr is None:
                        return (sock_.default_value, sock_.default_value)
                    return (mr.inputs["To Min"].default_value, mr.inputs["To Max"].default_value)
                out["nudge"] = [*rng(val), *rng(hue)]
            else:
                ops.append({"op": "hsv", "hue": hue.default_value, "sat": sat.default_value,
                            "val": val.default_value, "fac": fac.default_value})
            s = n.inputs["Color"]
            continue
        out["unknown"].append(n.type)
        break
    out["ops"] = list(reversed(ops))
    return out


def surface(m):
    nt = m.node_tree
    if nt is None:
        return None, None, None
    outn = next((n for n in nt.nodes if n.type == "OUTPUT_MATERIAL" and n.is_active_output), None) or \
        next((n for n in nt.nodes if n.type == "OUTPUT_MATERIAL"), None)
    bsdf = _src(outn.inputs["Surface"]) if outn is not None else None
    return nt, outn, bsdf


class Textures:
    """Every image a spec names, copied once under a unique file name."""
    def __init__(self):
        self.by_path = {}
        self.names = set()
        self.linear = {}
        self.missing = []

    def add(self, img, linear=None):
        if img is None:
            return ""
        p = image_file(img)
        if p is None or not p.exists():
            self.missing.append(img.name)
            return ""
        key = str(p.resolve()).lower()
        if key not in self.by_path:
            name = p.name
            if name in self.names:
                name = f"{p.parent.parent.name}_{p.name}"
            self.names.add(name)
            self.by_path[key] = (p, name)
        name = self.by_path[key][1]
        if linear is None:
            linear = img.colorspace_settings.name in ("Non-Color", "Linear", "Linear Rec.709")
        self.linear[name] = self.linear.get(name, False) or linear
        return name


def classify(m, name, first_names, tex):
    """One material's spec for the layout JSON and a log note."""
    nt, outn, bsdf = surface(m)
    spec = {"name": name, "shader": "painted", "albedo": "", "tint": [1.0, 1.0, 1.0], "tiling": [1.0, 1.0],
            "offset": [0.0, 0.0], "rotation": 0.0, "saturation": 1.0, "antiTile": [], "overlays": [],
            "postTint": [1.0, 1.0, 1.0], "normal": "", "normalStrength": 0.0, "smoothness": 0.1,
            "cutoff": 0.0, "twoSided": False, "emission": [0.0, 0.0, 0.0], "emissionFromAlbedo": False,
            "alpha": 1.0, "nudge": [], "notes": []}
    notes = spec["notes"]
    if bsdf is None or bsdf.type != "BSDF_PRINCIPLED":
        spec["tint"] = enc(m.diffuse_color[:3])
        notes.append("FALLBACK: no Principled BSDF, viewport colour")
        return spec
    rough = bsdf.inputs["Roughness"]
    spec["smoothness"] = round(1.0 - (rough.default_value if not rough.is_linked else 0.9), 3)
    t = trace(bsdf.inputs["Base Color"], first_names)
    if t["unknown"]:
        spec["tint"] = enc(m.diffuse_color[:3])
        notes.append("FALLBACK: " + ", ".join(t["unknown"]) + "; viewport colour")
        spec["_trace"] = t
        return spec
    if t["const"] is not None:
        base = t["const"]
    else:
        base = [1.0, 1.0, 1.0]
        spec["albedo"] = tex.add(t["albedo"], linear=False)
        spec["albedoClamp"] = bool(t.get("albedoClamp"))
        mp = t["mapping"]
        if mp:
            spec["tiling"] = [round(v, 6) for v in mp["scale"]]
            spec["offset"] = [round(v, 6) for v in mp["loc"]]
            spec["rotation"] = round(mp["rot"], 4)
            if mp.get("unsupported"):
                notes.append("mapping " + mp["unsupported"] + " ignored")
            if mp["type"] != "POINT":
                notes.append(f"mapping type {mp['type']} read as POINT")
    for at in t["antiTile"]:
        if at["image"] != t["albedo"]:
            notes.append("anti-tile sample reads a different image; used the albedo")
        spec["antiTile"].append({k: v for k, v in at.items() if k != "image"})
    if len(spec["antiTile"]) > 2:
        notes.append(f"{len(spec['antiTile'])} anti-tile samples, only 2 drawn")
    # Constant multipliers and saturation before the first mix commute into the tint; after a
    # mix they are kept apart as the post tint.
    tint, post, mixed = list(base), [1.0, 1.0, 1.0], False
    for op in t["ops"]:
        if op["op"] == "mul":
            if mixed:
                post = [a * b for a, b in zip(post, op["rgb"])]
            else:
                tint = [a * b for a, b in zip(tint, op["rgb"])]
        elif op["op"] == "hsv":
            if abs(op["hue"] - 0.5) > 1e-4 or abs(op["val"] - 1) > 1e-4 or abs(op["fac"] - 1) > 1e-4:
                notes.append(f"hsv hue {op['hue']:.3f} val {op['val']:.3f} fac {op['fac']:.3f}: only saturation drawn")
            if mixed or any(abs(v - 1) > 1e-6 for v in tint[:3]) and t["const"] is None and spec["saturation"] != 1.0:
                notes.append("saturation after a tint: drawn before it")
            spec["saturation"] = round(op["sat"], 4)
        elif op["op"] in ("mulTex", "mixTex"):
            if op["uv"] < 0:
                notes.append(f"overlay on unknown UV '{op['layer']}' dropped")
                continue
            ov = {"mode": "multiply" if op["op"] == "mulTex" else "mix", "texture": tex.add(op["image"]),
                  "uv": op["uv"], "tiling": [1.0, 1.0], "offset": [0.0, 0.0], "clamp": op["clamp"]}
            if op["mapping"]:
                ov["tiling"] = [round(v, 6) for v in op["mapping"]["scale"]]
                ov["offset"] = [round(v, 6) for v in op["mapping"]["loc"]]
            if op["op"] == "mixTex":
                mixed = True
            spec["overlays"].append(ov)
        elif op["op"] == "mixConst":
            notes.append("constant mix dropped")
    if len(spec["overlays"]) > 3:
        notes.append(f"{len(spec['overlays'])} overlays, only 3 drawn")
    spec["tint"] = enc(tint)
    spec["postTint"] = enc(post)
    spec["tintLinear"] = [round(c, 5) for c in tint]
    # Normal map.
    nm = _src(bsdf.inputs["Normal"])
    if nm is not None and nm.type == "NORMAL_MAP":
        img = _src(nm.inputs["Color"])
        if img is not None and img.type == "TEX_IMAGE":
            spec["normal"] = tex.add(img.image, linear=True)
            spec["normalStrength"] = round(nm.inputs["Strength"].default_value, 4)
    # Alpha: an albedo alpha is a cut-out; a constant below 1 with a BLEND method is glass.
    alpha = bsdf.inputs["Alpha"]
    if alpha.is_linked:
        a_src = _src(alpha)
        spec["cutoff"] = 0.5
        if a_src.type != "TEX_IMAGE":
            notes.append(f"alpha from {a_src.type}, read as the albedo alpha")
        elif a_src.image != t["albedo"]:
            notes.append("alpha from a different image than the albedo; read as the albedo alpha")
    elif alpha.default_value < 0.999:
        spec["alpha"] = round(alpha.default_value, 4)
        spec["shader"] = "glass"
    # Emission.
    em = bsdf.inputs["Emission Strength"].default_value
    if em > 0:
        ecol = bsdf.inputs["Emission Color"]
        if ecol.is_linked:
            spec["emission"] = enc([em] * 3)
            spec["emissionFromAlbedo"] = True
        else:
            spec["emission"] = enc([em * c for c in ecol.default_value[:3]])
    if t["nudge"] is not None:
        # The Kanto/Lagoon leaf card: LagoonFoliage draws it (tint already includes gain and clamp).
        spec["shader"] = "foliage"
        spec["nudge"] = [round(v, 4) for v in t["nudge"]]
        spec["twoSided"] = True
        if spec["cutoff"] == 0:
            spec["cutoff"] = 0.5
            notes.append("nudged card without an alpha: cut at 0.5 anyway")
    return spec


# ====================================================================== glb reading (verification)

def read_glb(path):
    data = path.read_bytes()
    off, gltf, blob = 12, None, b""
    while off < len(data):
        n, kind = struct.unpack_from("<II", data, off)
        chunk = data[off + 8: off + 8 + n]
        if kind == 0x4E4F534A:
            gltf = json.loads(chunk)
        elif kind == 0x004E4942:
            blob = chunk
        off += 8 + n
    return gltf, blob


COMP = {5126: (np.float32, 4), 5123: (np.uint16, 2), 5125: (np.uint32, 4), 5121: (np.uint8, 1)}
NCOMP = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}


def accessor(gltf, blob, i):
    a = gltf["accessors"][i]
    bv = gltf["bufferViews"][a["bufferView"]]
    dt, size = COMP[a["componentType"]]
    k = NCOMP[a["type"]]
    start = bv.get("byteOffset", 0) + a.get("byteOffset", 0)
    stride = bv.get("byteStride", 0) or size * k
    raw = np.frombuffer(blob, dtype=np.uint8, count=stride * (a["count"] - 1) + size * k, offset=start)
    rows = np.lib.stride_tricks.as_strided(raw, shape=(a["count"], size * k), strides=(stride, 1))
    arr = np.frombuffer(rows.copy().tobytes(), dtype=dt).reshape(a["count"], k).astype(np.float64)
    if a.get("normalized"):
        arr /= float(np.iinfo(dt).max)
    return arr


def glb_summary(path):
    gltf, blob = read_glb(path)
    tris, texcoords = 0, set()
    for m in gltf.get("meshes", []):
        for p in m["primitives"]:
            texcoords.add(sum(1 for k in p["attributes"] if k.startswith("TEXCOORD_")))
            tris += gltf["accessors"][p["indices"]]["count"] // 3 if "indices" in p else \
                gltf["accessors"][p["attributes"]["POSITION"]]["count"] // 3
    return tris, texcoords, gltf, blob


# ====================================================================== meshes

def canonical_uvs(me, want):
    """Rewrite the UV maps as [first, UVGrime, UVSplash, UVSill][:want + 1], zeros for a missing map."""
    n = len(me.loops)
    layers = list(me.uv_layers)
    data = {}
    for layer in layers:
        a = np.empty(n * 2, dtype=np.float32)
        layer.data.foreach_get("uv", a)
        data[layer.name] = a
    first = layers[0].name if layers else None
    order = [first] + list(OVERLAY_LAYERS)
    order = order[:want + 1]
    # By name, one at a time: a held reference to a later layer goes stale after a removal.
    while len(me.uv_layers):
        me.uv_layers.remove(me.uv_layers[me.uv_layers[0].name])
    names = ["UVMap", "UVGrime", "UVSplash", "UVSill"]
    for i, src in enumerate(order):
        new = me.uv_layers.new(name=names[i])
        arr = data.get(src) if src else None
        new.data.foreach_set("uv", arr if arr is not None else np.zeros(n * 2, dtype=np.float32))
    if len(me.uv_layers):
        me.uv_layers.active_index = 0
        me.uv_layers[0].active_render = True
    return [src if (src in data) else None for src in order]


def open_edge_share(me):
    """Per material index: share of its faces' edges that border only one face."""
    npoly = len(me.polygons)
    if npoly == 0:
        return {}
    loop_edge = np.empty(len(me.loops), dtype=np.int64)
    me.loops.foreach_get("edge_index", loop_edge)
    count = np.bincount(loop_edge, minlength=len(me.edges))
    tot = np.empty(npoly, dtype=np.int64)
    me.polygons.foreach_get("loop_total", tot)
    mi = np.empty(npoly, dtype=np.int64)
    me.polygons.foreach_get("material_index", mi)
    loop_mi = np.repeat(mi, tot)
    open_ = count[loop_edge] == 1
    share = {}
    for k in np.unique(mi):
        sel = loop_mi == k
        share[int(k)] = float(open_[sel].mean()) if sel.any() else 0.0
    return share


def export_glb(obj, path, tangents=False):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.gltf(
        filepath=str(path), export_format="GLB", use_selection=True, export_apply=False, export_yup=True,
        export_materials="EXPORT", export_image_format="NONE", export_texcoords=True, export_normals=True,
        export_tangents=tangents, export_vertex_color="NONE", export_attributes=False, export_cameras=False,
        export_lights=False, export_animations=False, export_skins=False, export_morph=False, export_extras=False)


# ====================================================================== footprint colliders

def footprint(ob, mw):
    """Walkable-area colliders for the solid things a body can run into, one box per LOOSE PART
    (a pole's shaft, a chair, a stall's table legs, a tree's trunk; one mesh often holds many,
    like the street kit's railings or the PGH fence, whose single bounding box would span the
    court). Per part: the plan extent of its vertices lower than FOOT_REACH above its own base,
    up to the part's highest vertex over that extent, capped at FOOT_CAP. A part counts only if
    it stands on the ground (base under GROUNDED; never an awning, a wall sign, a crossarm or a
    canopy), is taller than the 0.30 m step, and reaches into the street a body can reach
    (|x| < 11, |z| < 16.5), where the Bounds walls do not already stop everything.
    A COMPACT object (its low plan extent at most COMPACT metres: a chair, a kiosk, a cart, a
    barrier) is one box round the whole object instead, so a chair is a chair, not four legs.
    World axis-aligned boxes in the game frame: [[cx, cy, cz, sx, sy, sz], ...]."""
    corners = np.array([mw @ Vector(c) for c in ob.bound_box])
    if corners[:, 0].min() > REACH_X or corners[:, 0].max() < -REACH_X or \
            corners[:, 1].min() > REACH_Z or corners[:, 1].max() < -REACH_Z or corners[:, 2].min() > GROUNDED:
        return None
    me = ob.data
    co = np.empty(len(me.vertices) * 3, dtype=np.float64)
    me.vertices.foreach_get("co", co)
    co = co.reshape(-1, 3)
    w = (np.array(mw)[:3, :3] @ co.T).T + np.array(mw)[:3, 3]
    whole = _part_box(w)
    if whole is not None and max(whole[3], whole[5]) <= COMPACT:
        return [whole]
    ev = np.empty(len(me.edges) * 2, dtype=np.int64)
    me.edges.foreach_get("vertices", ev)
    ev = ev.reshape(-1, 2)
    # Loose parts by union-find over the edges (path halving).
    parent = list(range(len(w)))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    for a, b in ev.tolist():
        ra, rb = find(a), find(b)
        if ra != rb:
            parent[max(ra, rb)] = min(ra, rb)
    label = np.array([find(i) for i in range(len(w))])
    boxes = [b for b in (_part_box(w[label == part]) for part in np.unique(label)) if b is not None]
    return boxes or None


def _part_box(pw):
    base = pw[:, 2].min()
    if base > GROUNDED:
        return None
    low = pw[pw[:, 2] < base + FOOT_REACH]
    x0, y0 = low[:, 0].min(), low[:, 1].min()
    x1, y1 = low[:, 0].max(), low[:, 1].max()
    if x0 >= REACH_X or x1 <= -REACH_X or y0 >= REACH_Z or y1 <= -REACH_Z:
        return None
    over = pw[(pw[:, 0] >= x0 - 0.05) & (pw[:, 0] <= x1 + 0.05) & (pw[:, 1] >= y0 - 0.05) & (pw[:, 1] <= y1 + 0.05)]
    top = min(over[:, 2].max(), base + FOOT_CAP)
    if top - base < 0.30:
        return None
    return [round(float(v), 4) for v in ((x0 + x1) / 2, (base + top) / 2, (y0 + y1) / 2, x1 - x0, top - base, y1 - y0)]


# ====================================================================== the pier bake

def bake_pier(me, mats, tex_dir):
    """Bake the pier's colour chain and normals onto a unique UV. Returns the baked mesh copy."""
    bm = me.copy()
    bm.name = "lrt_pier_nearfade"
    o = bpy.data.objects.new("lrt_pier_nearfade", bm)
    bpy.context.scene.collection.objects.link(o)
    first = bm.uv_layers[0].name
    # Local copies of the materials, so the linked ones are never touched; every image and '' UV
    # node pinned to the first map, since the bake UV becomes the active one.
    locals_ = []
    for i, m in enumerate(bm.materials):
        lm = m.copy()
        lm.name = f"bake_{m.name}"
        for n in lm.node_tree.nodes:
            if n.type in ("UVMAP", "NORMAL_MAP") and n.uv_map == "":
                n.uv_map = first
            if n.type == "TEX_IMAGE" and not n.inputs["Vector"].is_linked:
                uvn = lm.node_tree.nodes.new("ShaderNodeUVMap")
                uvn.uv_map = first
                lm.node_tree.links.new(uvn.outputs["UV"], n.inputs["Vector"])
        bm.materials[i] = lm
        locals_.append(lm)
    bake_uv = bm.uv_layers.new(name="UVBake")
    bm.uv_layers.active = bake_uv
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.004, area_weight=0.0,
                             correct_aspect=True, scale_to_bounds=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 8
    scene.cycles.device = "CPU"
    results = {}
    for kind, bake_type, colour in (("albedo", "DIFFUSE", True), ("normal", "NORMAL", False)):
        img = bpy.data.images.new(f"lrt_pier_nearfade_{kind}", BAKE_SIZE, BAKE_SIZE, alpha=False,
                                  float_buffer=False, is_data=not colour)
        img.colorspace_settings.name = "sRGB" if colour else "Non-Color"
        for lm in locals_:
            nodes = lm.node_tree.nodes
            for n in nodes:
                n.select = False
            t = nodes.new("ShaderNodeTexImage")
            t.image = img
            t.select = True
            nodes.active = t
        if bake_type == "DIFFUSE":
            bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"}, margin=16, use_clear=True)
        else:
            bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT", margin=16, use_clear=True)
        path = tex_dir / f"lrt_pier_nearfade_{kind}.png"
        img.filepath_raw = str(path)
        img.file_format = "PNG"
        img.save()
        results[kind] = path.name
        for lm in locals_:
            nodes = lm.node_tree.nodes
            nodes.remove(nodes.active)
    # The exported copy keeps only the bake UV as TEXCOORD_0.
    keep = np.empty(len(bm.loops) * 2, dtype=np.float32)
    bm.uv_layers["UVBake"].data.foreach_get("uv", keep)      # re-fetched: edit mode rebuilt the layers
    while len(bm.uv_layers):
        bm.uv_layers.remove(bm.uv_layers[bm.uv_layers[0].name])
    nl = bm.uv_layers.new(name="UVMap")
    nl.data.foreach_set("uv", keep)
    bpy.data.objects.remove(o)
    return bm, results


# ====================================================================== main

def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    survey = "--survey" in argv
    sys.path.insert(0, str(TOOLS))
    import author_ilalim_city as CITY
    dg = bpy.context.evaluated_depsgraph_get()
    root = bpy.data.collections["ilalim_city"]
    top_of = {}
    for top in root.children:
        for o in top.all_objects:
            top_of.setdefault(o.name_full, top.name)
    hide = set()
    hide_path = SOURCE / "landmarks.json"
    if hide_path.exists():
        hide = set(json.loads(hide_path.read_text(encoding="utf-8")).get("hide", []))

    # ------------------------------------------------------------ materials (read-only trace)
    first_names = {"UVMap", "Float2"}
    used = {}
    for inst in dg.object_instances:
        if inst.object.type == "MESH":
            for s in inst.object.original.material_slots:
                if s.material is not None:
                    used[(lib_of(s.material), s.material.name)] = s.material
    names = Counter(n for _, n in used)
    mat_name = {k: (k[1] if names[k[1]] == 1 else f"{k[1]}__{Path(k[0]).stem}") for k in used}
    tex = Textures()
    specs = {}
    for key in sorted(used, key=lambda k: mat_name[k]):
        specs[key] = classify(used[key], mat_name[key], first_names, tex)
    if survey:
        for key, s in sorted(specs.items(), key=lambda kv: kv[1]["name"]):
            ov = ",".join(f"{o['mode'][0]}{o['uv']}:{o['texture']}" for o in s["overlays"])
            log(f"SURVEY {s['name']:34s} {s['shader']:8s} alb={s['albedo'] or '-'} at={len(s['antiTile'])} "
                f"sat={s['saturation']} ov=[{ov}] cut={s['cutoff']} em={s['emission']} {s['notes']}")
        log("SURVEY missing images:", tex.missing)
        return

    MODELS.mkdir(parents=True, exist_ok=True)
    TEXTURES.mkdir(parents=True, exist_ok=True)

    # ------------------------------------------------------------ walk the instances
    protos = {}            # key -> {"mesh": local mesh, "name": str, "mats": [keys], "users": n}
    placements = []
    proof_objs = {}        # mesh name -> (key, world matrix, evaluated world vertices mapped by D)
    budget = Counter()
    skipped = Counter()
    anchors = {}
    footprints = Counter()
    train_parent = None
    pier_raw = None

    def modsig(o):
        return tuple((m.type, getattr(m, "width", 0), getattr(m, "segments", 0), getattr(m, "thickness", 0))
                     for m in o.modifiers if m.show_render)

    for inst in dg.object_instances:
        ob = inst.object
        if ob.type != "MESH":
            continue
        orig = ob.original
        if orig.hide_render or orig.name.startswith("median plant placeholders") or orig.name in hide:
            skipped[orig.name.split(".")[0] if orig.name.startswith("median") else "hidden"] += 1
            continue
        instancer = inst.parent.original if inst.is_instance and inst.parent is not None else None
        top = top_of.get((instancer or orig).name_full)
        if top is None:
            skipped["no top collection"] += 1
            log("WARNING: instance outside the city root:", orig.name)
            continue
        group = GROUP_OF.get(top, safe(top))
        key = (lib_of(orig.data), orig.data.name, modsig(orig))
        if orig.data.name == PIER_MESH and pier_raw is None:
            pier_raw = bpy.data.meshes.new_from_object(ob, preserve_all_data_layers=True, depsgraph=dg)
        if key not in protos:
            me = bpy.data.meshes.new_from_object(ob, preserve_all_data_layers=True, depsgraph=dg)
            protos[key] = {"mesh": me, "mats": [(lib_of(s.material), s.material.name) if s.material else None
                                                 for s in orig.material_slots],
                           "users": 0, "tris": sum(len(p.vertices) - 2 for p in me.polygons), "group": group,
                           "object": orig.name}
        p = protos[key]
        p["users"] += 1
        mw = inst.matrix_world.copy()
        rec = {"key": key, "group": group, "object": orig.name, "_m": mw}
        if group == "Train":
            train_parent = inst.parent.matrix_world.copy() if inst.parent is not None else Matrix.Identity(4)
            rec["_local"] = train_parent.inverted() @ mw
        placements.append(rec)
        budget[group] += p["tris"]
        if orig.data.name in PROOF_MESHES and orig.data.name not in proof_objs:
            me_ev = ob.data
            co = np.empty(len(me_ev.vertices) * 3, dtype=np.float64)
            me_ev.vertices.foreach_get("co", co)
            co = co.reshape(-1, 3)
            w = (np.array(mw)[:3, :3] @ co.T).T + np.array(mw)[:3, 3]
            proof_objs[orig.data.name] = (key, len(placements) - 1, np.stack([w[:, 0], w[:, 2], w[:, 1]], axis=1))
        if group in FOOTPRINT_GROUPS:
            fp = footprint(ob, mw)
            if fp is not None:
                rec["collider"] = fp
                footprints[group] += len(fp)
        if group in ("Props", "ColumnSigns") or orig.data.name == PIER_MESH:
            me_ev = ob.data
            co = np.empty(len(me_ev.vertices) * 3, dtype=np.float64)
            me_ev.vertices.foreach_get("co", co)
            co = co.reshape(-1, 3)
            w = (np.array(mw)[:3, :3] @ co.T).T + np.array(mw)[:3, 3]
            entry = {"origin": game_point(mw.translation), "min": game_point(Vector(w.min(axis=0))),
                     "max": game_point(Vector(w.max(axis=0)))}
            if orig.data.name == PIER_MESH:
                anchors.setdefault("piers", []).append(entry)
            else:
                anchors[orig.name.split(".")[0]] = entry
    log("instances:", len(placements), "prototypes:", len(protos), "skipped:", dict(skipped))
    log("footprint colliders:", dict(footprints))

    # ------------------------------------------------------------ prototype names
    taken = set()
    for key, p in sorted(protos.items(), key=lambda kv: (kv[0][1], kv[0][0])):
        base = safe(key[1])
        name = base
        if name in taken:
            name = f"{base}__{safe(Path(key[0]).stem)}"
        k = 2
        while name in taken:
            name = f"{base}_{k}"
            k += 1
        taken.add(name)
        p["name"] = name

    # ------------------------------------------------------------ stub materials, UV order, two-sided
    stub = {}
    for key, spec in specs.items():
        s = bpy.data.materials.new(spec["name"])
        s.name = spec["name"]
        if s.name != spec["name"]:
            log("WARNING: stub material renamed by Blender:", spec["name"], "->", s.name)
        stub[key] = s
    open_share = defaultdict(float)
    uv_report = Counter()
    for key, p in protos.items():
        me = p["mesh"]
        want = 0
        for mk in p["mats"]:
            if mk is None:
                continue
            for ov in specs[mk]["overlays"]:
                want = max(want, ov["uv"])
        sources = canonical_uvs(me, want)
        for i, src in enumerate(sources):
            if i > 0 and src is None:
                uv_report[f"TEXCOORD_{i} zero-filled"] += 1
        uv_report[f"{len(sources)} UV sets"] += 1
        for i, share in open_edge_share(me).items():
            if i < len(p["mats"]) and p["mats"][i] is not None:
                open_share[p["mats"][i]] = max(open_share[p["mats"][i]], share)
        for i, mk in enumerate(p["mats"]):
            if i < len(me.materials):
                me.materials[i] = stub[mk] if mk is not None else None
    log("UV layouts:", dict(uv_report))
    for key, spec in specs.items():
        if spec["shader"] in ("painted", "glass") and not spec["twoSided"]:
            if spec["cutoff"] > 0:
                spec["twoSided"] = True
                spec["notes"].append("two-sided: alpha cut")
            elif open_share.get(key, 0) > 0.25:
                spec["twoSided"] = True
                spec["notes"].append(f"two-sided: {round(open_share[key] * 100)} per cent open edges")

    # ------------------------------------------------------------ the pier bake
    pier_key = next((k for k in protos if k[1] == PIER_MESH), None)
    pier_spec = None
    if pier_key is not None:
        pier = protos[pier_key]
        # The bake reads the ORIGINAL (linked) materials through local copies, so it needs the
        # un-stubbed mesh: a fresh evaluated copy of one pier.
        baked, files = bake_pier(pier_raw, None, TEXTURES)
        baked.materials.clear()
        nf = bpy.data.materials.new("lrt_pier_nearfade")
        baked.materials.append(nf)
        pier["mesh"] = baked
        pier["mats"] = ["NEARFADE"]
        pier["tangents"] = True
        pier_spec = {"name": nf.name, "shader": "nearfade", "albedo": files["albedo"], "normal": files["normal"],
                     "normalStrength": 1.0, "tint": [1.0, 1.0, 1.0], "smoothness": 0.15,
                     "notes": ["baked in Cycles from lrt_pier and lrt_pier_cap with both grime overlays "
                               "(DIFFUSE colour and tangent-space NORMAL) onto a Smart UV Project unwrap"]}
        tex.linear[files["normal"]] = True
        tex.linear.setdefault(files["albedo"], False)
        log("pier baked:", files)

    # ------------------------------------------------------------ write the .glb files
    scene_col = bpy.context.scene.collection
    written = {}
    for key, p in sorted(protos.items(), key=lambda kv: kv[1]["name"]):
        o = bpy.data.objects.new(p["name"], p["mesh"])
        scene_col.objects.link(o)
        path = MODELS / f"{p['name']}.glb"
        export_glb(o, path, tangents=p.get("tangents", False))
        written[p["name"]] = path
        bpy.data.objects.remove(o)
    for f in MODELS.glob("*.glb"):
        if f.stem not in written:
            f.unlink()
            meta = f.with_suffix(".glb.meta")
            if meta.exists():
                meta.unlink()
            log("removed stale", f.name)

    # ------------------------------------------------------------ textures
    copied = []
    for key_, (src, name) in sorted(tex.by_path.items(), key=lambda kv: kv[1][1]):
        shutil.copyfile(src, TEXTURES / name)
        copied.append(name)
    keep = set(copied) | ({pier_spec["albedo"], pier_spec["normal"]} if pier_spec else set())
    for f in TEXTURES.glob("*.png"):
        if f.name not in keep:
            f.unlink()
            meta = f.with_suffix(".png.meta")
            if meta.exists():
                meta.unlink()
            log("removed stale texture", f.name)
    if tex.missing:
        log("ERROR: images named by materials but not found on disk:", sorted(set(tex.missing)))

    # ------------------------------------------------------------ read back and prove
    stats = {}
    problems = []
    for key, p in protos.items():
        tris, texcoords, gltf, blob = glb_summary(written[p["name"]])
        stats[p["name"]] = (tris, gltf, blob)
        want = len(p["mesh"].uv_layers)
        if texcoords and max(texcoords) != want:
            problems.append(f"{p['name']}: {sorted(texcoords)} TEXCOORD sets, expected {want}")
    for pr in problems[:30]:
        log("ERROR:", pr)
    log("read-back problems:", len(problems))

    def prove(mesh_name):
        key, index, B_ = proof_objs[mesh_name]
        p = protos[key]
        _t, gltf, blob = stats[p["name"]]
        mu = np.array(unity_matrix(placements[index]["_m"]), dtype=np.float64).reshape(4, 4)
        worst, n = 0.0, 0
        for node in gltf["nodes"]:
            if node.get("mesh") is None:
                continue
            pos = accessor(gltf, blob, gltf["meshes"][node["mesh"]]["primitives"][0]["attributes"]["POSITION"])
            picks = pos[np.linspace(0, len(pos) - 1, 12).astype(int)]
            local = np.stack([-picks[:, 0], picks[:, 1], picks[:, 2]], axis=1)   # glTFast negates X
            U = (mu[:3, :3] @ local.T).T + mu[:3, 3]
            for q in U:
                worst = max(worst, float(np.min(np.linalg.norm(B_ - q, axis=1))))
                n += 1
        det = float(np.linalg.det(mu[:3, :3]))
        return worst, n, det

    proofs = []
    for name in PROOF_MESHES:
        if name in proof_objs:
            if name == PIER_MESH:
                continue      # the pier mesh was rebuilt by the bake; its vertices are unchanged
            w, n, det = prove(name)
            proofs.append((name, w, n, det))
            log(f"coordinate proof {name}: {n} vertices, worst {w:.2e} m, det {det:+.3f}")
    mirrored = sum(1 for r in placements if np.linalg.det(np.array(r["_m"])[:3, :3]) < 0)
    log("mirrored placements (negative determinant):", mirrored)

    # ------------------------------------------------------------ sun and haze
    sun = next((o for o in bpy.data.objects if o.type == "LIGHT" and o.data.type == "SUN"), None)
    sun_json = None
    if sun is not None:
        fwd = D3 @ (sun.matrix_world.to_3x3().normalized() @ Vector((0, 0, -1)))
        sun_json = {"forward": [round(c, 5) for c in fwd], "color": enc(sun.data.color[:3]),
                    "colorLinear": [round(c, 5) for c in sun.data.color[:3]],
                    "blenderEnergy": round(sun.data.energy, 4), "angleDeg": round(math.degrees(sun.data.angle), 3)}
    world = bpy.context.scene.world
    sky = None
    if world is not None and world.node_tree is not None and "Background" in world.node_tree.nodes:
        bg = world.node_tree.nodes["Background"]
        sky = {"color": enc(bg.inputs["Color"].default_value[:3]), "strength": round(bg.inputs["Strength"].default_value, 4)}
    haze = {"color": enc(CITY.HAZE), "colorLinear": list(CITY.HAZE), "start": CITY.HAZE_START,
            "depth": CITY.HAZE_DEPTH, "cap": CITY.HAZE_CAP,
            "note": "compositor: lerp(frame, color, min(cap, (distance - start) / depth)), linear light"}

    # ------------------------------------------------------------ the layout JSON
    spec_list = [{k: v for k, v in s.items() if not k.startswith("_")} for s in specs.values()]
    if pier_spec:
        spec_list.append(pier_spec)
    for s in spec_list:
        s.setdefault("linear", {})
        for f in [s.get("normal", "")] + [o["texture"] for o in s.get("overlays", [])]:
            if f:
                s["linear"][f] = bool(tex.linear.get(f, False))
        s["linear"] = [{"key": k, "value": str(v)} for k, v in s["linear"].items()]
    out_placements = []
    for r in placements:
        p = protos[r["key"]]
        m = r["_local"] if "_local" in r else r["_m"]
        out_placements.append({"model": p["name"], "group": r["group"], "object": r["object"],
                               "matrix": unity_matrix(m), "local": "_local" in r, "collider": [v for box in r.get("collider", []) for v in box]})
    kits = defaultdict(lambda: {"placements": 0, "prototypes": set(), "placedTris": 0})
    for r in placements:
        k = kits[r["group"]]
        k["placements"] += 1
        k["prototypes"].add(protos[r["key"]]["name"])
        k["placedTris"] += protos[r["key"]]["tris"]
    budget_json = [{"group": g, "placements": v["placements"], "prototypes": len(v["prototypes"]),
                    "placedTris": v["placedTris"]} for g, v in sorted(kits.items())]
    train_json = None
    if train_parent is not None:
        train_json = {"root": unity_matrix(train_parent), "halfLength": 7.8,
                      "note": "author_ilalim_train.py: origin on the rail head at the consist centre, runs along the game's z"}
    data = {
        "note": ("Written by tools/export_ilalim_unity.py from ArtSource/ilalim/ilalim_city.blend. Matrices are UNITY "
                 "axes, row-major: D . M_blender . C^T, D = [[1,0,0],[0,0,1],[0,1,0]], C = [[-1,0,0],[0,0,1],[0,-1,0]]. "
                 "A Blender point (x, y, z) is Unity (x, z, y): the Ilalim kits are modelled in the game's frame. "
                 "Colours are sRGB-encoded (assign with new Color in the linear project). UV channels: TEXCOORD_0 painted, "
                 "1 UVGrime, 2 UVSplash, 3 UVSill. Placements with local=true are relative to the train root. "
                 "collider: world axis-aligned boxes in the game frame, six floats each (centre, size)."),
        "gameplay": {"box": 7.0, "kerbInner": 6.65, "kerbTop": 0.150, "pavementOuter": 11.0, "pavementTop": 0.212,
                     "wallZ": 16.5, "soffit": 8.0, "deckTop": 9.04, "deckWidth": 10.5, "railHead": 9.19,
                     "trackX": 2.35, "pierX": 4.45, "pierHalf": 0.70},
        "sun": sun_json, "sky": sky, "haze": haze, "train": train_json,
        "anchors": [dict(name=k, **v) for k, v in sorted(anchors.items()) if k != "piers"],
        "piers": anchors.get("piers", []),
        "budget": budget_json,
        "materials": spec_list,
        "placements": out_placements,
    }
    LAYOUT.write_text(json.dumps(data, indent=1), encoding="utf-8")

    # ------------------------------------------------------------ report
    size = sum(f.stat().st_size for f in written.values())
    kinds = Counter(s["shader"] for s in spec_list)
    fallbacks = [(s["name"], s["notes"]) for s in spec_list if any("FALLBACK" in n for n in s["notes"])]
    others = [(s["name"], s["notes"]) for s in spec_list if s["notes"] and not any("FALLBACK" in n for n in s["notes"])
              and not all(n.startswith("two-sided") for n in s["notes"])]
    log(f"{len(written)} prototypes, {len(placements)} placements, {len(spec_list)} materials, {len(copied)} textures, "
        f".glb total {size / 1e6:.1f} MB")
    for b in budget_json:
        log(f"  {b['group']:16s} placements {b['placements']:5d} prototypes {b['prototypes']:4d} placed tris {b['placedTris']:9d}")
    log("  placed triangles total", sum(b["placedTris"] for b in budget_json))
    log("materials per shader:", dict(kinds))
    log("fallbacks:", fallbacks if fallbacks else "none")
    for n, notes in others:
        log("note:", n, notes)
    log("anchors:", [a["name"] for a in data["anchors"]], "piers", len(data["piers"]))
    log("layout ->", LAYOUT)


if __name__ == "__main__":
    main()
