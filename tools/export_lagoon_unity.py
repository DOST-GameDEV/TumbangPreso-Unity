"""Export the Lagoon Cove (ArtSource/lagoon/lagoon_cove.blend) for Unity.

  blender -b ArtSource/lagoon/lagoon_cove.blend --python tools/export_lagoon_unity.py

Writes, and touches nothing else:

  Assets/TumbangPreso/Art/LagoonCove/Models/<model>.glb     one file per PROTOTYPE
  Assets/TumbangPreso/Art/LagoonCove/Textures/<name>.png    every image an exported material reads
  Assets/TumbangPreso/Art/LagoonCove/lagoon_cove_layout.json    materials, placements, sun, gameplay

⚠️ THE .blend IS NEVER SAVED. Everything below (un-excluding the kit sources, stripping material
node trees to names, resetting object-level material slots, adding the ground's field colours)
happens to the copy loaded in memory and dies with the process. tools/author_lagoon_cove.py owns
the file; this script only reads it.

PROTOTYPES AND PLACEMENTS (the Kanto precedent, tools/author_kanto_city.py, extended):
  * Every set of objects sharing mesh data goes out ONCE. A house, boat or prop is a hierarchy
    built once into a hidden "(source, not placed)" collection and placed as linked duplicates,
    so its source collection is one .glb (root empty plus its meshes) and each placed copy is a
    placement. A plant is one shared mesh, a stone one of the sixteen rock kit meshes.
  * A placement carries the copy's FULL matrix, not a yaw: props are tilted onto slopes and rocks
    carry non-uniform scale, which a position plus a yaw would lose.
  * Unique objects go out as their own .glb, placed once: the ground, the structures (grouped by
    kind into a few files, in world coordinates, placed at identity), the landmark rock and its
    emblem decal (in the rock's own frame, placed with the rock's matrix so the rock shader can
    read its scale), and the backdrop (world coordinates, identity).
  * A placed hierarchy that no longer matches its source (the owner deleted or moved a piece of
    it by hand) is exported as its own variant rather than silently restored.

OBJECT-LEVEL MATERIALS (house thatch and sawali variety, boat hull and trim colours, flower
colours) are how linked duplicates differ. The prototype .glb carries the MESH's material names;
each placement lists what its copy wears instead as "overrides" (from the base name to the
override name). Every override material is in "materials".

VERTEX COLOURS: a material that multiplies its albedo by a colour attribute (trim_tint,
prop_tint, buri_tint, float_tint, cloth_tint) is marked vertexTint, and that attribute is made
the mesh's render colour so the glTF exporter writes it as COLOR_0 ("ACTIVE" mode). Blender's
exporter writes the LINEAR colour as normalized unsigned shorts; glTFast hands it to the mesh
unchanged.

THE GROUND'S SIX FIELDS (author_lagoon_cove.GROUND_FIELDS, signed metres) travel as
COLOR_0 = (court_in, grass_in, sand_in, wet_depth) and TEXCOORD_1 = (steep, ring), each remapped
to 0..1 by field = FIELD_LO + v * (FIELD_HI - FIELD_LO); TEXCOORD_0 is world XY / 4 m, the
Blender material's own tile. The remap is written into the ground material's "extra".

COLOURS IN THE JSON are sRGB-ENCODED, as Kanto's: `material.color = new Color(tint)` in a
linear-space project gives back exactly the linear value Blender's nodes multiply by. Values
above 1 (a few multipliers brighten) are encoded on the same curve.

COORDINATES (as Kanto): a Blender point (x, y, z) lands in Unity at (-x, z, -y). The glTF
exporter writes (x, z, -y) and glTFast negates X. A placement matrix is C . M . C^T with
C = [[-1, 0, 0], [0, 0, 1], [0, -1, 0]] (translation mapped by C), written row-major. The script
proves it at the end: vertices read back from the exported .glb files, pushed through a
placement's matrix, are compared with the same placed object's Blender vertices converted by
(-x, z, -y).

Nothing is decimated or simplified (CLAUDE.md section 6.0): modifiers are applied, every UV map
and vertex colour is kept.
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
OUT = ROOT / "Assets" / "TumbangPreso" / "Art" / "LagoonCove"
MODELS = OUT / "Models"
TEXTURES = OUT / "Textures"
LAYOUT = OUT / "lagoon_cove_layout.json"
TEX_SRC = ROOT / "ArtSource" / "lagoon" / "textures"
TAG = "[lagoon-unity]"

# Blender (x, y, z) -> Unity (-x, z, -y). Orthogonal, so its inverse is its transpose.
C3 = Matrix(((-1, 0, 0), (0, 0, 1), (0, -1, 0)))
C4 = C3.to_4x4()
C4T = C4.transposed()

SOURCE_SUFFIX = "(source, not placed)"
SKIP_COLLECTIONS = ("Gameplay markers (not in game art)",)
SKIP_OBJECTS = ("sea",)      # replaced by the Unity water shader (brief: "What is NOT exported")

# ⚠️ THE GROUND FIELD REMAP. glTF COLOR_0 is normalized (the exporter writes unsigned shorts, so
# anything outside 0..1 is clipped), so the signed metre fields are mapped into 0..1. The material
# only ever cuts a field at zero with a smoothstep of half-width 0.05 to 0.3 m plus a 0.6 m noise
# wobble, and reads wet_depth up to 1.7 m, so +-16 m keeps every boundary exact: a zero crossing
# moves only where a vertex beyond 16 m sits next to one on the other side, which the export
# counts and logs. Precision at 16 bits is 32 m / 65535 = 0.5 mm.
FIELD_LO, FIELD_HI = -16.0, 16.0

GROUPS_COLLIDER = {"Ground": "mesh", "Rocks": "mesh", "Houses": "mesh", "Structures": "mesh",
                   "Landmark": "mesh", "Boats": "mesh", "Props": "none", "Plants": "none",
                   "Backdrop": "none"}


def log(*a):
    print(TAG, *a)


def unity_matrix(m):
    r = C4 @ m @ C4T
    return [round(r[i][j], 6) for i in range(4) for j in range(4)]


def srgb(c):
    """Linear -> sRGB-encoded, extended past 1 on the same curve (Unity decodes it the same way)."""
    if c <= 0.0031308:
        return c * 12.92
    return 1.055 * c ** (1 / 2.4) - 0.055


def enc(rgb):
    return [round(srgb(max(0.0, c)), 5) for c in rgb]


def safe(name):
    return "".join(ch if ch.isalnum() or ch in "_-" else "_" for ch in name).strip("_")


def in_source(o):
    return any(c.name.endswith(SOURCE_SUFFIX) or _holder_of(c) is not None for c in o.users_collection)


_HOLDER_CACHE = {}


def _holder_of(col):
    """The '(source, not placed)' holder a kit collection sits in, if any."""
    if col.name not in _HOLDER_CACHE:
        found = None
        for h in bpy.data.collections:
            if h.name.endswith(SOURCE_SUFFIX) and col.name in h.children:
                found = h
        _HOLDER_CACHE[col.name] = found
    return _HOLDER_CACHE[col.name]


def skipped(o):
    if o.name in SKIP_OBJECTS or o.type not in ("MESH", "EMPTY"):
        return True
    if any(c.name in SKIP_COLLECTIONS for c in o.users_collection):
        return True
    return o.hide_render or o.hide_get()


# ====================================================================== materials

def _input(node, identifier):
    return next(i for i in node.inputs if i.identifier == identifier)


def _src(sock):
    return sock.links[0].from_node if sock.is_linked else None


def _upstream(sock):
    seen, todo = set(), [sock]
    while todo:
        s = todo.pop()
        for l in s.links:
            n = l.from_node
            if n.name in seen:
                continue
            seen.add(n.name)
            todo.extend(i for i in n.inputs if i.is_linked)
    return seen


def _file_of(img):
    if img is None:
        return ""
    if img.filepath:
        return Path(bpy.path.abspath(img.filepath)).name
    return img.name if img.name.endswith(".png") else img.name + ".png"


def _surface(m):
    nt = m.node_tree
    out = next((n for n in nt.nodes if n.type == "OUTPUT_MATERIAL" and n.is_active_output), None) or \
        next((n for n in nt.nodes if n.type == "OUTPUT_MATERIAL"), None)
    return nt, out


def trace_base(sock):
    """Walk the Base Color input back to its albedo image through the chains the kits build
    (lagoon_paint_materials, lagoon_cove_planting, render_lagoon_texture_preview, the boat
    colours): MULTIPLY mixes with the image on A and a constant, an RGB node, a colour attribute or
    a computed factor on B; the flower's pale-key MIX; the per-plant HUE_SAT nudge."""
    info = {"albedo": None, "mult": [1.0, 1.0, 1.0], "attr": None, "nudge": False, "key": None,
            "computed": [], "unknown": [], "const": None}
    s = sock
    if not s.is_linked:
        info["const"] = list(s.default_value)[:3]
        return info
    while s is not None and s.is_linked:
        n = s.links[0].from_node
        if n.type == "TEX_IMAGE":
            info["albedo"] = n
            return info
        if n.type == "MIX" and n.data_type == "RGBA":
            fac, a, b = _input(n, "Factor_Float"), _input(n, "A_Color"), _input(n, "B_Color")
            if n.blend_type == "MULTIPLY" and not fac.is_linked and abs(fac.default_value - 1) < 1e-6:
                bn = _src(b)
                if bn is None:
                    info["mult"] = [x * y for x, y in zip(info["mult"], b.default_value[:3])]
                elif bn.type == "RGB":
                    info["mult"] = [x * y for x, y in zip(info["mult"], bn.outputs[0].default_value[:3])]
                elif bn.type == "ATTRIBUTE" and bn.attribute_type == "GEOMETRY":
                    info["attr"] = bn.attribute_name
                else:
                    info["computed"].append(bn)
                s = a
                continue
            if n.blend_type == "MIX" and fac.is_linked and _src(fac).type == "MAP_RANGE" and not b.is_linked:
                mr = _src(fac)
                info["key"] = (mr.inputs["From Min"].default_value, mr.inputs["From Max"].default_value,
                               list(b.default_value[:3]))
                s = a
                continue
        if n.type == "HUE_SAT":
            info["nudge"] = True
            s = n.inputs["Color"]
            continue
        info["unknown"].append(n.type)
        return info
    return info


def _normal(nt, bsdf):
    nm = _src(bsdf.inputs["Normal"])
    if nm is None or nm.type != "NORMAL_MAP":
        return "", 0.0
    img = _src(nm.inputs["Color"])
    return (_file_of(img.image) if img is not None and img.type == "TEX_IMAGE" else ""), nm.inputs["Strength"].default_value


def _height(out):
    d = _src(out.inputs["Displacement"])
    if d is None or d.type != "DISPLACEMENT":
        return "", 0.0
    img = _src(d.inputs["Height"])
    return (_file_of(img.image) if img is not None and img.type == "TEX_IMAGE" else ""), d.inputs["Scale"].default_value


def _x(extra, key, value):
    extra.append({"key": key, "value": str(value)})


def rock_extra(m, extra, files):
    """⚠️ The rock look, for the Unity shader to match (render_lagoon_texture_preview.rock_material
    with wear "brush": box projection, anti-tiling, normal tone, crevice AO, brushed_edge). The
    numbers read from the node tree are read; the brushed edge's own literals mirror that function
    and must change with it."""
    import render_lagoon_texture_preview as T
    nt = m.node_tree
    scale = next((n for n in nt.nodes if n.type == "VECT_MATH" and n.operation == "SCALE"), None)
    tile = 1.0 / scale.inputs["Scale"].default_value if scale else T.TILE_M
    imgs = [n for n in nt.nodes if n.type == "TEX_IMAGE"]
    box = next((n for n in imgs if n.projection == "BOX"), None)
    mapping = next((n for n in nt.nodes if n.type == "MAPPING"), None)
    atlas = next((n for n in imgs if "rock_edges" in (n.image.name if n.image else "")), None)
    nmap = next((n for n in nt.nodes if n.type == "NORMAL_MAP"), None)
    files.add(_file_of(atlas.image) if atlas else "rock_edges_atlas.png")
    _x(extra, "projection", "world-space box (Unity: triplanar), albedo and normal")
    _x(extra, "tileMetres", round(tile, 4))
    _x(extra, "boxBlend", round(box.projection_blend, 3) if box else 0.45)
    _x(extra, "normalStrength", round(nmap.inputs["Strength"].default_value, 3) if nmap else 0.6)
    if mapping:
        rot = [round(math.degrees(a), 3) for a in mapping.inputs["Rotation"].default_value]
        sc = round(mapping.inputs["Scale"].default_value[0] * tile, 4)
        loc = [round(v, 3) for v in mapping.inputs["Location"].default_value]
        _x(extra, "antiTiling", f"second sample of the same albedo: position rotated {rot} degrees (XYZ euler), "
                                f"scaled {sc} per tile, offset {loc}; blended in by noise(position * 0.09, detail 1.5) "
                                f"smoothstep 0.42..0.58")
    _x(extra, "normalTone", "albedo x mapRange(worldNormal.z, -0.6..0.9 -> 0.78..1.14)")
    _x(extra, "edgeAtlas", _file_of(atlas.image) if atlas else "rock_edges_atlas.png")
    _x(extra, "edgeAtlasUV", "TEXCOORD_1 (Blender UV map UVBake), linear (non-colour) texture")
    _x(extra, "edgeChannels", "R = 1 - distanceToPlaneBreak / EDGE_BAKE_R (linear), G = broad shoulder, B = ambient occlusion")
    _x(extra, "EDGE_BAKE_R", T.EDGE_BAKE_R)
    _x(extra, "rockScale", "(|col0| + |col1| + |col2|) / 3 of the object-to-world matrix, max(rockScale, 0.3); "
                           "Blender stored it per stone as the object attribute rock_scale = mean(scale.xyz)")
    _x(extra, "reach", "reach = max(rockScale, 0.3) * EDGE_BAKE_R; threshold(widthMetres) = 1 - widthMetres / reach")
    _x(extra, "crevice", "colour x smoothstep-mapRange(B, 0.35..0.95 -> 0.72..1.0)")
    _x(extra, "noises", "all on world position: slow = noise(0.45, detail 1), fine = noise(7, detail 8, rough 0.7), "
                        "grain = noise(28, detail 10, rough 0.85), quick = noise(1.6, detail 2), cuts = noise(3.2, detail 3, rough 0.6)")
    _x(extra, "band", "width = smoothstep-mapRange(slow, 0.32..0.68 -> 0.12..0.55) x mapRange(fine, 0.3..0.7 -> 0.55..1.35); "
                      "band = smoothstep-mapRange(R - threshold(width), -0.015..0.04) x mapRange(grain, 0.25..0.5 -> 0.55..1.0); "
                      "colour x mapRange(band, 0..1 -> 1.0..1.38)")
    _x(extra, "line", "lw = smoothstep-mapRange(quick, 0.3..0.7 -> 0.015..0.085) x mapRange(fine, 0.3..0.7 -> 0.6..1.3); "
                      "line = smoothstep-mapRange(R - threshold(lw), -0.006..0.01) x smoothstep-mapRange(cuts, 0.42..0.5) "
                      "x clamp(mapRange(grain, 0.3..0.5)); colour x mapRange(line, 0..1 -> 1.0..1.3), "
                      "then lerp toward EDGE_WHITE by line x 0.05")
    _x(extra, "EDGE_WHITE", [round(c, 4) for c in T.EDGE_WHITE[:3]] + ["linear"])
    _x(extra, "mapRangeNote", "mapRange clamps (Blender's default); smoothstep-mapRange is Blender's SMOOTHSTEP interpolation")


def ground_extra(m, extra, files):
    """⚠️ The ground's recipe (author_lagoon_cove.ground_material), for the Unity shader. The mask
    widths are read from the node tree; the layer order and tints mirror that function."""
    import author_lagoon_cove as COVE
    nt = m.node_tree
    widths, wobbled = {}, {}
    for a in (n for n in nt.nodes if n.type == "ATTRIBUTE"):
        for l in a.outputs["Fac"].links:
            n = l.to_node
            wob = n.type == "MATH"
            if wob:
                n = n.outputs[0].links[0].to_node
            if n.type == "MAP_RANGE" and n.interpolation_type == "SMOOTHSTEP" and n.inputs["From Min"].default_value < 0:
                widths[a.attribute_name] = round(n.inputs["From Max"].default_value, 4)
                wobbled[a.attribute_name] = wob
    for n in nt.nodes:
        if n.type == "TEX_IMAGE" and n.image:
            files.add(_file_of(n.image))
    _x(extra, "fields", "COLOR_0 = (court_in, grass_in, sand_in, wet_depth); TEXCOORD_1 = (steep, ring)")
    _x(extra, "fieldDecode", f"field_metres = {FIELD_LO} + v * {FIELD_HI - FIELD_LO} (every field, colour and UV alike)")
    _x(extra, "fieldMeaning", "signed metres, positive INSIDE the region the field names (author_lagoon_cove.ground_fields)")
    _x(extra, "uv0", "TEXCOORD_0 = Blender world (x, y) / 4 m = Unity (-X, -Z) / 4: one tile of every ground texture")
    _x(extra, "maskWidths", ", ".join(f"{k}:{v}{' +wobble' if wobbled.get(k) else ''}" for k, v in sorted(widths.items())))
    _x(extra, "mask", "mask(field) = smoothstep(-width, width, field + wobble); wobble = mapRange(noise(objectPosition * 0.35), 0..1 -> -0.6..0.6)")
    _x(extra, "antiTiling", "each texture sampled twice: uv0, and rotate(uv0 * 0.61, 37 degrees) + (0.37, 0.71); "
                            "lerp by smoothstep(0.42, 0.58, noise(xy * 0.06, detail 1))")
    _x(extra, "textures", "sand_a_albedo.png, grass_a_albedo.png, earth_a_albedo.png, rock_a_albedo.png")
    _x(extra, "rockFill", "rock_a x (0.62, 0.6, 0.58) linear")
    _x(extra, "layers", "out = rockFill; out = lerp(out, grass, mask(ring)); out = lerp(out, rockFill, mask(steep)); "
                        "out = lerp(out, sand, mask(sand_in)); out = lerp(out, seabed, mask(wet_depth)); "
                        "out = lerp(out, grass, mask(grass_in)); out = lerp(out, earth, mask(court_in))")
    _x(extra, "seabed", f"sand x lerp({list(COVE.SEABED_SHALLOW)}, {list(COVE.SEABED_DEEP)}, smoothstep(0, 1.7, wet_depth)) "
                        f"x (1.2, 1.2, 1.3), all linear")
    _x(extra, "roughness", 0.9)


def classify(m):
    """One material's spec for the layout JSON, the texture files it needs, and a log note."""
    nt, out = _surface(m)
    files, extra = set(), []
    spec = {"name": m.name, "kind": "plain", "albedo": "", "normal": "", "tint": [1.0, 1.0, 1.0, 1.0],
            "tiling": [1.0, 1.0], "cutoff": 0.0, "vertexTint": False, "gain": 1.0, "smoothness": 0.1,
            "emission": [0.0, 0.0, 0.0], "twoSided": False, "extra": extra}
    if nt is None or out is None or not out.inputs["Surface"].is_linked:
        rgb = list(m.diffuse_color[:3])
        spec["tint"] = enc(rgb) + [1.0]
        return spec, files, "plain (FALLBACK: no node tree), viewport colour"
    first = _src(out.inputs["Surface"])
    haze = None
    if first.type == "MIX_SHADER":
        haze = first
        bsdf = next((l.from_node for i in first.inputs if i.is_linked for l in i.links
                     if l.from_node.type == "BSDF_PRINCIPLED"), None)
    else:
        bsdf = first if first.type == "BSDF_PRINCIPLED" else None
    if bsdf is None:
        spec["tint"] = enc(list(m.diffuse_color[:3])) + [1.0]
        return spec, files, f"plain (FALLBACK: surface is {first.type}), viewport colour"
    rough = bsdf.inputs["Roughness"]
    spec["smoothness"] = round(1.0 - (rough.default_value if not rough.is_linked else 0.9), 3)
    names = {n.attribute_name for n in nt.nodes if n.type == "ATTRIBUTE"}
    import author_lagoon_cove as COVE
    if names & set(COVE.GROUND_FIELDS):
        spec["kind"] = "ground"
        ground_extra(m, extra, files)
        return spec, files, "ground"
    if any(n.type == "UVMAP" and n.uv_map == "UVBake" for n in nt.nodes):
        spec["kind"] = "rock"
        alb = next((n for n in nt.nodes if n.type == "TEX_IMAGE" and n.projection == "BOX"
                    and n.image and n.image.colorspace_settings.name != "Non-Color"), None)
        spec["albedo"] = _file_of(alb.image) if alb else ""
        spec["normal"], _s = _normal(nt, bsdf)
        rock_extra(m, extra, files)
        tile = float(next(e["value"] for e in extra if e["key"] == "tileMetres"))
        spec["tiling"] = [round(1.0 / tile, 5)] * 2
        files.update(f for f in (spec["albedo"], spec["normal"]) if f)
        return spec, files, "rock"
    base = trace_base(bsdf.inputs["Base Color"])
    if base["albedo"] is not None:
        spec["albedo"] = _file_of(base["albedo"].image)
    spec["normal"], nstrength = _normal(nt, bsdf)
    hfile, hscale = _height(out)
    if base["const"] is not None:
        spec["tint"] = enc(base["const"]) + [1.0]
    else:
        spec["tint"] = enc(base["mult"]) + [1.0]
        _x(extra, "tintLinear", [round(c, 4) for c in base["mult"]])
    if base["attr"]:
        spec["vertexTint"] = True
        _x(extra, "vertexColour", f"{base['attr']} as COLOR_0, LINEAR, multiplies albedo x tint")
    if nstrength:
        _x(extra, "normalStrength", round(nstrength, 3))
    if hfile:
        _x(extra, "height", hfile)
        _x(extra, "heightScale", round(hscale, 4))
        _x(extra, "heightUse", "Blender bump (Displacement node, BUMP method); Unity: height/parallax map")
    if spec["albedo"] or spec["normal"]:
        if any(n.type == "UVMAP" and n.uv_map == "UVMap" for n in nt.nodes):
            _x(extra, "uv", "TEXCOORD_0 (the kit's world-scale UVMap: 1 UV = 2 m, textures are 2 m tiles)")
        else:
            _x(extra, "uv", "TEXCOORD_0 (the mesh's own UVs, the drawing spans 0..1)")
    alpha = bsdf.inputs["Alpha"]
    if alpha.is_linked:
        spec["cutoff"] = 0.5
        a_src = _src(alpha)
        if a_src.type == "TEX_IMAGE":
            _x(extra, "alpha", "albedo alpha, cut at cutoff (Blender DITHERED)")
        else:
            attrs = [n for n in nt.nodes if n.type == "ATTRIBUTE"]
            _x(extra, "alpha", "albedo alpha x vertex colour alpha (" + ", ".join(a.attribute_name for a in attrs)
               + " written to COLOR_0.a), cut at cutoff")
            spec["vertexTint"] = True
            _x(extra, "vertexColour", "COLOR_0 = (1, 1, 1, paint): rgb multiplies nothing, alpha masks the decal")
    for c in base["computed"]:
        if c.type == "COMBINE_COLOR":
            img = next((n for n in nt.nodes if n.type == "TEX_IMAGE" and n.projection == "BOX"), None)
            mr = next((n for n in nt.nodes if n.type == "MAP_RANGE"), None)
            if img is not None and mr is not None:
                files.add(_file_of(img.image))
                _x(extra, "grain", f"albedo x grey(mapRange(luminance({_file_of(img.image)} box-projected at world "
                                   f"position / 4 m), {round(mr.inputs['From Min'].default_value, 4)}..{round(mr.inputs['From Max'].default_value, 4)}"
                                   f" -> {round(mr.inputs['To Min'].default_value, 3)}..{round(mr.inputs['To Max'].default_value, 3)}))")
    if base["key"]:
        lo, hi, pale = base["key"]
        _x(extra, "paleKey", f"above albedo value {round(lo, 4)}..{round(hi, 4)} (linear, mapRange) the tinted colour gives "
                             f"way to {enc(pale)} (sRGB): the flower's cream stamen")
    if base["nudge"]:
        _x(extra, "objectNudge", "per object: value x mapRange(random, 0..1 -> 0.9..1.08), hue shift mapRange(random -> 0.485..0.515) (0.5 = none)")
    em = bsdf.inputs["Emission Strength"].default_value
    if em > 0:
        ecol = bsdf.inputs["Emission Color"]
        if ecol.is_linked:
            spec["emission"] = enc([em] * 3)
            _x(extra, "emissionSource", "emission = albedo x tint x emission (Blender feeds the albedo chain into Emission Color)")
        else:
            spec["emission"] = enc([em * c for c in ecol.default_value[:3]])
    files.update(f for f in (spec["albedo"], spec["normal"], hfile) if f)
    if haze is not None:
        emit = next((l.from_node for i in haze.inputs if i.is_linked for l in i.links if l.from_node.type == "EMISSION"), None)
        mrs = sorted((n for n in nt.nodes if n.type == "MAP_RANGE"), key=lambda n: n.name)
        _x(extra, "haze", "Blender mixes the lit colour toward a flat haze colour (Emission) by "
                          "mapRange(distance from the world origin to the object, 150..420 m -> 0.3..0.55) "
                          "+ mapRange(world z, 0..18 m -> 0.14..0); Unity: fog or the same lerp")
        if emit is not None:
            _x(extra, "hazeColour", enc(emit.inputs["Color"].default_value[:3]))
        def reads_normal_z(n):
            sep = _src(n.inputs["Value"])
            if sep is None or sep.type != "SEPXYZ" or n.inputs["Value"].links[0].from_socket.name != "Z":
                return False
            geo = _src(sep.inputs[0])
            return geo is not None and geo.type == "NEW_GEOMETRY" and sep.inputs[0].links[0].from_socket.name == "Normal"
        tone = next((n for n in mrs if reads_normal_z(n)), None)
        if tone is not None:
            _x(extra, "normalTone", f"colour x mapRange(worldNormal.z, {round(tone.inputs['From Min'].default_value, 3)}.."
                                    f"{round(tone.inputs['From Max'].default_value, 3)} -> {round(tone.inputs['To Min'].default_value, 3)}.."
                                    f"{round(tone.inputs['To Max'].default_value, 3)})")
        # A haze material's base colour is a constant on the MULTIPLY's A side.
        mix = _src(bsdf.inputs["Base Color"])
        if mix is not None and mix.type == "MIX" and not _input(mix, "A_Color").is_linked:
            flat = list(_input(mix, "A_Color").default_value[:3])
            spec["tint"] = enc(flat) + [1.0]
            extra[:] = [e for e in extra if e["key"] != "tintLinear"]
            _x(extra, "tintLinear", [round(c, 4) for c in flat])
        spec["kind"] = "plain"
        return spec, files, "plain + haze extras (distant backdrop)"
    if base["unknown"]:
        spec["kind"] = "plain"
        spec["tint"] = enc(list(m.diffuse_color[:3])) + [1.0]
        return spec, files, f"plain (FALLBACK: base colour from {base['unknown']}), viewport colour"
    if spec["cutoff"] > 0 and base["albedo"] is not None and (base["nudge"] or m.name.startswith(("lagoon_leaf", "lagoon_flower"))):
        spec["kind"] = "foliage"
        spec["twoSided"] = True
        _x(extra, "normals", "custom normals point out of each clump: never flip them for back faces")
        _x(extra, "gainNote", "tint already includes the kit's gain and its clamp to 1 (min(1, tint x gain)), so gain is 1")
        return spec, files, "foliage"
    if em > 0:
        spec["kind"] = "emissive"
        return spec, files, "emissive"
    if base["albedo"] is not None:
        spec["kind"] = "painted"
        return spec, files, "painted"
    if base["const"] is not None:
        spec["kind"] = "plain"
        return spec, files, "plain (flat BSDF colour)"
    spec["tint"] = enc(list(m.diffuse_color[:3])) + [1.0]
    return spec, files, "plain (FALLBACK: nothing recognised), viewport colour"


def open_edge_share(meshes_by_material):
    """Share of the edges around each material's faces that border only ONE face. Cards, cloth and
    open nets are all edge; closed solids are none. A material whose faces are mostly open is
    drawn two-sided in Unity, as Blender draws every face (no material here culls back faces)."""
    share = {}
    for name, meshes in meshes_by_material.items():
        total = open_ = 0
        for me in meshes:
            idx = [i for i, mm in enumerate(me.materials) if mm and mm.name == name]
            if not idx:
                continue
            count = Counter()
            for p in me.polygons:
                for ek in p.edge_keys:
                    count[ek] += 1
            for p in me.polygons:
                if p.material_index in idx:
                    for ek in p.edge_keys:
                        total += 1
                        open_ += count[ek] == 1
        share[name] = open_ / total if total else 0.0
    return share


# ====================================================================== glb reading (verification)

def read_glb(path):
    data = path.read_bytes()
    _magic, _ver, _len = struct.unpack_from("<III", data, 0)
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


def node_matrix(n):
    if "matrix" in n:
        return np.array(n["matrix"], dtype=np.float64).reshape(4, 4).T
    t = n.get("translation", [0, 0, 0])
    x, y, z, w = n.get("rotation", [0, 0, 0, 1])
    s = n.get("scale", [1, 1, 1])
    r = np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                  [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                  [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])
    m = np.eye(4)
    m[:3, :3] = r * np.array(s)
    m[:3, 3] = t
    return m


def node_worlds(gltf):
    worlds = {}

    def walk(i, parent):
        w = parent @ node_matrix(gltf["nodes"][i])
        worlds[i] = w
        for c in gltf["nodes"][i].get("children", []):
            walk(c, w)
    for root in gltf["scenes"][gltf.get("scene", 0)]["nodes"]:
        walk(root, np.eye(4))
    return worlds


def glb_stats(path):
    gltf, blob = read_glb(path)
    tris, attrs = 0, set()
    worlds = node_worlds(gltf)
    for i in worlds:
        mi = gltf["nodes"][i].get("mesh")
        if mi is None:
            continue
        for p in gltf["meshes"][mi]["primitives"]:
            attrs.update(p["attributes"])
            if "indices" in p:
                tris += gltf["accessors"][p["indices"]]["count"] // 3
            else:
                tris += gltf["accessors"][p["attributes"]["POSITION"]]["count"] // 3
    mats = sorted({m.get("name", "") for m in gltf.get("materials", [])})
    return tris, attrs, mats, gltf, blob


# ====================================================================== export

def export(objs, name):
    """One .glb of exactly these objects (selection), modifiers applied, material names only."""
    bpy.ops.object.select_all(action="DESELECT")
    objs = [o for o in objs]
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = next((o for o in objs if o.type == "MESH"), objs[0])
    path = MODELS / f"{name}.glb"
    bpy.ops.export_scene.gltf(
        filepath=str(path), export_format="GLB", use_selection=True, export_apply=True, export_yup=True,
        export_materials="EXPORT", export_image_format="NONE", export_texcoords=True, export_normals=True,
        export_tangents=False, export_vertex_color="ACTIVE", export_all_vertex_colors=False,
        export_active_vertex_color_when_no_material=True, export_attributes=False, export_cameras=False,
        export_lights=False, export_animations=False, export_skins=False, export_morph=False, export_extras=False)
    return path


def temp_object(mesh, name):
    o = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(o)
    return o


def descendants(root):
    return [root] + list(root.children_recursive)


def rel(root, o):
    return root.matrix_world.inverted() @ o.matrix_world


def mats_close(a, b, tol=1e-4):
    return all(abs(a[i][j] - b[i][j]) < tol for i in range(4) for j in range(4))


def main():
    import author_lagoon_cove as COVE
    MODELS.mkdir(parents=True, exist_ok=True)
    TEXTURES.mkdir(parents=True, exist_ok=True)
    vl = bpy.context.view_layer

    # ⚠️ The kit sources live in EXCLUDED collections, which the depsgraph does not evaluate, so
    # their modifiers (the bevels) would not apply and they could not be selected. In memory only.
    def unexclude(lc):
        if lc.collection.name.endswith(SOURCE_SUFFIX) or _holder_of(lc.collection) is not None:
            lc.exclude = False
        for ch in lc.children:
            unexclude(ch)
    unexclude(vl.layer_collection)
    vl.update()

    # ------------------------------------------------------------ the kit sources
    source_roots = {}         # source collection name -> root object
    mesh_to_source = {}       # mesh data name -> source collection name
    source_group = {}
    for holder_name, group in (("House kit " + SOURCE_SUFFIX, "Houses"), ("Boat kit " + SOURCE_SUFFIX, "Boats"),
                               ("Props kit " + SOURCE_SUFFIX, "Props")):
        holder = bpy.data.collections.get(holder_name)
        if holder is None:
            log("WARNING: no", holder_name)
            continue
        for col in holder.children:
            roots = [o for o in col.all_objects if o.parent is None]
            if len(roots) != 1:
                log("WARNING: source", col.name, "has", len(roots), "roots")
                continue
            source_roots[col.name] = roots[0]
            source_group[col.name] = group
            for o in col.all_objects:
                if o.type == "MESH":
                    mesh_to_source[o.data.name] = col.name

    def signature(root):
        out = []
        for o in root.children_recursive:
            if o.type == "MESH":
                out.append((o.data.name, rel(root, o)))
        return out

    src_sig = {k: signature(r) for k, r in source_roots.items()}

    placements = []           # dicts, filled in order
    exports = []              # (name, kind, payload) run after the read-only pass
    unique_hierarchies = {}   # variant name -> placed root
    base_mats_by_model = defaultdict(set)

    def overrides_of(objs):
        ovr, conflicts = {}, []
        for o in objs:
            if o.type != "MESH":
                continue
            for i, slot in enumerate(o.material_slots):
                base = o.data.materials[i] if i < len(o.data.materials) else None
                if base is None or slot.material is None or slot.material == base:
                    continue
                if base.name in ovr and ovr[base.name] != slot.material.name:
                    conflicts.append((base.name, ovr[base.name], slot.material.name))
                ovr[base.name] = slot.material.name
        if conflicts:
            log("WARNING: conflicting overrides in one placement", conflicts)
        return [{"from": k, "to": v} for k, v in sorted(ovr.items())]

    def add(model, matrix, group, overrides=()):
        placements.append({"model": model, "matrix": unity_matrix(matrix), "overrides": list(overrides),
                           "group": group, "collider": GROUPS_COLLIDER[group], "_m": matrix.copy()})

    # ------------------------------------------------------------ Village and Props hierarchies
    root_col = bpy.data.collections["lagoon_cove"]
    village = bpy.data.collections.get("Village")
    props = bpy.data.collections.get("Props")
    structures = defaultdict(list)
    landmark = bpy.data.objects.get("landmark rock")
    unassigned = []
    for col in (village, props):
        for o in col.objects:
            if o.parent is not None or skipped(o):
                continue
            if o is landmark:
                continue
            if o.get("lagoon_structure") is not None:
                s = o["lagoon_structure"]
                kind = ("walks" if s in ("spine", "spur", "east walk") else
                        "cliff_walks" if s.startswith("cliff walk") else
                        "piers" if s.endswith("pier") else
                        "railings" if s.startswith("ledge railing") else
                        "stairs" if " to " in s else "other")
                structures[kind].append(o)
                continue
            meshes = [d for d in o.children_recursive if d.type == "MESH"]
            srcs = Counter(mesh_to_source.get(d.data.name) for d in meshes)
            src = srcs.most_common(1)[0][0] if srcs else None
            if src is None:
                unassigned.append(o)
                continue
            group = source_group[src]
            # Does this copy still match its source piece for piece?
            placed = signature(o)
            want = list(src_sig[src])
            ok = len(placed) == len(want)
            if ok:
                for mesh_name, m in placed:
                    hit = next((k for k, (wn, wm) in enumerate(want) if wn == mesh_name and mats_close(wm, m)), None)
                    if hit is None:
                        ok = False
                        break
                    want.pop(hit)
            model = src
            matrix = o.matrix_world @ source_roots[src].matrix_world.inverted()
            if not ok:
                model = f"{src}__{safe(o.name)}"
                unique_hierarchies[model] = o
                matrix = o.matrix_world.copy()
                log(f"variant: {o.name} no longer matches {src} piece for piece; exported as {model}")
            add(model, matrix, group, overrides_of(descendants(o)))
    for o in unassigned:
        log("WARNING: unassigned hierarchy, exported with the structures:", o.name)
        structures["other"].append(o)

    # ------------------------------------------------------------ rocks, plants
    rock_meshes, plant_meshes = {}, {}
    for o in bpy.data.collections["Boulders"].objects:
        if skipped(o) or o.type != "MESH":
            continue
        rock_meshes[o.data.name] = o.data
        add(o.data.name, o.matrix_world, "Rocks", overrides_of([o]))
    for o in bpy.data.collections["Planting"].objects:
        if skipped(o) or o.type != "MESH":
            continue
        plant_meshes[o.data.name] = o.data
        add(o.data.name, o.matrix_world, "Plants", overrides_of([o]))

    # ------------------------------------------------------------ unique pieces
    ground = bpy.data.objects["ground"]
    add("ground", ground.matrix_world, "Ground")
    for kind, roots in sorted(structures.items()):
        add(f"structures_{kind}", Matrix.Identity(4), "Structures")
    emblem = None
    if landmark is not None:
        add("landmark_rock", landmark.matrix_world, "Landmark", overrides_of([landmark]))
        emblem = next((c for c in landmark.children if c.type == "MESH"), None)
        if emblem is not None:
            add("landmark_emblem", landmark.matrix_world, "Landmark")
            placements[-1]["collider"] = "none"      # a painted decal over the rock: the rock collides
    backdrop = [o for o in bpy.data.collections["Backdrop"].all_objects if not skipped(o)]
    if backdrop:
        add("backdrop", Matrix.Identity(4), "Backdrop")

    # Anything under the cove the passes above did not claim is reported, never dropped silently.
    handled = ("Village", "Props", "Boulders", "Planting", "Backdrop") + SKIP_COLLECTIONS
    for col in root_col.children:
        if col.name in handled:
            continue
        for o in col.all_objects:
            if o.name != "ground" and not skipped(o):
                log("WARNING: not exported:", o.name, "in", col.name)
    for o in bpy.context.scene.objects:
        if o.type not in ("MESH", "EMPTY") and o.type != "LIGHT":
            log("skipped (not geometry):", o.name, o.type)

    # ------------------------------------------------------------ materials (read before stripping)
    used = {}                 # material name -> Material
    meshes_by_material = defaultdict(set)

    def note_mesh(me, obj=None):
        for i, mm in enumerate(me.materials):
            if mm is not None:
                used[mm.name] = mm
                meshes_by_material[mm.name].add(me)
        if obj is not None:
            for slot in obj.material_slots:
                if slot.material is not None:
                    used[slot.material.name] = slot.material
                    meshes_by_material[slot.material.name].add(obj.data)

    for r in source_roots.values():
        for o in descendants(r):
            if o.type == "MESH":
                note_mesh(o.data)
    for r in unique_hierarchies.values():
        for o in descendants(r):
            if o.type == "MESH":
                note_mesh(o.data)
    for me in list(rock_meshes.values()) + list(plant_meshes.values()):
        note_mesh(me)
    note_mesh(ground.data)
    for roots in structures.values():
        for r in roots:
            for o in descendants(r):
                if o.type == "MESH":
                    note_mesh(o.data, o)
    if landmark is not None:
        note_mesh(landmark.data, landmark)
    if emblem is not None:
        note_mesh(emblem.data, emblem)
    for o in backdrop:
        if o.type == "MESH":
            note_mesh(o.data, o)
    for p in placements:
        for ov in p["overrides"]:
            m = bpy.data.materials[ov["to"]]
            used[m.name] = m
            meshes_by_material[m.name] |= meshes_by_material.get(ov["from"], set())

    specs, all_files, kinds, fallbacks = [], set(), Counter(), []
    tint_attr = {}
    open_share = open_edge_share(meshes_by_material)
    for p in placements:                  # an override material covers its base's faces
        for ov in p["overrides"]:
            open_share[ov["to"]] = max(open_share.get(ov["to"], 0.0), open_share.get(ov["from"], 0.0))
    for name in sorted(used):
        spec, files, note = classify(used[name])
        if spec["kind"] in ("painted", "emissive", "plain") and not spec["twoSided"]:
            # An alpha-cut surface shows its far side through its holes (the open net); a thin
            # sheet is seen from both sides. Blender draws both sides of everything here.
            if spec["cutoff"] > 0:
                spec["twoSided"] = True
                _x(spec["extra"], "twoSidedWhy", "alpha cut: the far side shows through the holes")
            elif open_share.get(name, 0) > 0.25:
                spec["twoSided"] = True
                _x(spec["extra"], "twoSidedWhy", f"{round(open_share[name] * 100)} per cent of its faces' edges are open (thin sheet)")
        specs.append(spec)
        all_files |= files
        kinds[spec["kind"]] += 1
        if "FALLBACK" in note:
            fallbacks.append((name, note))
        for e in spec["extra"]:
            if e["key"] == "vertexColour" and not e["value"].startswith("COLOR_0 ="):
                tint_attr[name] = e["value"].split(" ")[0]
        log(f"material {name:40s} {spec['kind']:9s} {note}"
            + (f"  vertexTint({tint_attr.get(name, 'paint')})" if spec["vertexTint"] else "")
            + ("  twoSided" if spec["twoSided"] else ""))

    # ------------------------------------------------------------ prepare meshes (in memory)
    def set_render_colour(me):
        want = [tint_attr[m.name] for m in me.materials if m is not None and m.name in tint_attr]
        for w in want:
            k = me.color_attributes.find(w)
            if k >= 0:
                me.color_attributes.active_color_index = k
                me.color_attributes.render_color_index = k
                return w
        if want:
            log("WARNING: mesh", me.name, "uses", want, "but has no such colour attribute")
        return None

    tinted = Counter()
    for me in bpy.data.meshes:
        w = set_render_colour(me)
        if w:
            tinted[w] += 1
    log("vertex tints made the render colour (meshes):", dict(tinted))

    # The ground: fields into a colour attribute and a second UV map, a first UV map in metres / 4.
    gme = ground.data.copy()
    gme.name = "ground"
    nv = len(gme.vertices)
    vals = {}
    for f in COVE.GROUND_FIELDS:
        arr = np.empty(nv, dtype=np.float32)
        gme.attributes[f].data.foreach_get("value", arr)
        vals[f] = arr
    rng = {f: (float(v.min()), float(v.max())) for f, v in vals.items()}
    log("ground field ranges (metres):", {f: (round(a, 3), round(b, 3)) for f, (a, b) in rng.items()})

    def remap(a):
        return np.clip((a - FIELD_LO) / (FIELD_HI - FIELD_LO), 0.0, 1.0)
    # Boundary shifts caused by the clamp: an edge whose endpoints straddle zero with one beyond range.
    ev = np.empty(len(gme.edges) * 2, dtype=np.int64)
    gme.edges.foreach_get("vertices", ev)
    ev = ev.reshape(-1, 2)
    for f in COVE.GROUND_FIELDS:
        a, b = vals[f][ev[:, 0]], vals[f][ev[:, 1]]
        cross = (a > 0) != (b > 0)
        clipped = cross & ((np.abs(a) > FIELD_HI) | (np.abs(b) > FIELD_HI))
        log(f"ground field {f}: {int(cross.sum())} boundary edges, {int(clipped.sum())} with an endpoint beyond +-{FIELD_HI} m")
    colour = np.stack([remap(vals["court_in"]), remap(vals["grass_in"]), remap(vals["sand_in"]),
                       remap(vals["wet_depth"])], axis=1).astype(np.float32)
    ca = gme.color_attributes.new("ground_fields", "FLOAT_COLOR", "POINT")
    ca.data.foreach_set("color", colour.ravel())
    gme.color_attributes.active_color_index = gme.color_attributes.find("ground_fields")
    gme.color_attributes.render_color_index = gme.color_attributes.find("ground_fields")
    loop_v = np.empty(len(gme.loops), dtype=np.int64)
    gme.loops.foreach_get("vertex_index", loop_v)
    co = np.empty(nv * 3, dtype=np.float32)
    gme.vertices.foreach_get("co", co)
    co = co.reshape(-1, 3)
    for uvname, data in (("UVMap", np.stack([co[:, 0] / 4.0, co[:, 1] / 4.0], axis=1)),
                         ("UVFields", np.stack([remap(vals["steep"]), remap(vals["ring"])], axis=1))):
        layer = gme.uv_layers.new(name=uvname)
        layer.data.foreach_set("uv", data[loop_v].astype(np.float32).ravel())
    gme.uv_layers.active_index = 0

    # The emblem: in the rock's own frame, its "paint" mask as COLOR_0 alpha.
    eme = None
    if emblem is not None:
        eme = emblem.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh().copy()
        eme.name = "landmark_emblem"
        eme.transform(landmark.matrix_world.inverted() @ emblem.matrix_world)
        paint = np.empty(len(eme.vertices), dtype=np.float32)
        eme.attributes["paint"].data.foreach_get("value", paint)
        log("emblem paint range:", round(float(paint.min()), 4), round(float(paint.max()), 4),
            "custom normals:", eme.has_custom_normals)
        rgba = np.ones((len(eme.vertices), 4), dtype=np.float32)
        rgba[:, 3] = np.clip(paint, 0, 1)
        pa = eme.color_attributes.new("emblem_paint", "FLOAT_COLOR", "POINT")
        pa.data.foreach_set("color", rgba.ravel())
        eme.color_attributes.render_color_index = eme.color_attributes.find("emblem_paint")
        eme.color_attributes.active_color_index = eme.color_attributes.find("emblem_paint")

    # ⚠️ Material node trees are STRIPPED to a bare BSDF before export, in memory: the .glb must
    # carry names only (Unity builds the real materials from the JSON), the exporter must not try
    # to pack images, and its node-tree vertex colour detection must not mistake the ground's field
    # attributes for colours. The object slots of the prototypes' own objects go back to the mesh's
    # material, so a prototype carries BASE names and each placement lists its overrides.
    for m in bpy.data.materials:
        if m.node_tree is None:
            continue
        nt = m.node_tree
        nt.nodes.clear()
        o = nt.nodes.new("ShaderNodeOutputMaterial")
        b = nt.nodes.new("ShaderNodeBsdfPrincipled")
        b.inputs["Base Color"].default_value = m.diffuse_color
        nt.links.new(b.outputs["BSDF"], o.inputs["Surface"])
    for r in list(source_roots.values()) + list(unique_hierarchies.values()):
        for o in descendants(r):
            for slot in o.material_slots:
                slot.link = "DATA"

    # ------------------------------------------------------------ write the .glb files
    written = {}
    for name, root in sorted(source_roots.items()):
        if any(p["model"] == name for p in placements):
            written[name] = export(descendants(root), name)
    for name, root in sorted(unique_hierarchies.items()):
        keep = root.matrix_world.copy()
        root.matrix_world = Matrix.Identity(4)
        vl.update()
        written[name] = export(descendants(root), name)
        root.matrix_world = keep
        vl.update()
    for name, me in sorted(rock_meshes.items()) + sorted(plant_meshes.items()):
        t = temp_object(me, name)
        written[name] = export([t], name)
        bpy.data.objects.remove(t)
    t = temp_object(gme, "ground")
    written["ground"] = export([t], "ground")
    bpy.data.objects.remove(t)
    for kind, roots in sorted(structures.items()):
        objs = [d for r in roots for d in descendants(r)]
        written[f"structures_{kind}"] = export(objs, f"structures_{kind}")
    if landmark is not None:
        t = temp_object(landmark.data, "landmark_rock")
        for i, slot in enumerate(landmark.material_slots):
            if slot.link == "OBJECT":
                t.material_slots[i].link = "OBJECT"
                t.material_slots[i].material = slot.material
        written["landmark_rock"] = export([t], "landmark_rock")
        bpy.data.objects.remove(t)
    if eme is not None:
        t = temp_object(eme, "landmark_emblem")
        written["landmark_emblem"] = export([t], "landmark_emblem")
        bpy.data.objects.remove(t)
    if backdrop:
        written["backdrop"] = export(backdrop, "backdrop")
    missing = sorted(set(p["model"] for p in placements) - set(written))
    if missing:
        log("ERROR: placements name models that were not written:", missing)
    # Old files from an earlier run that no longer correspond to a prototype.
    for f in MODELS.glob("*.glb"):
        if f.stem not in written:
            f.unlink()
            meta = f.with_suffix(".glb.meta")
            if meta.exists():
                meta.unlink()
            log("removed stale", f.name)

    # ------------------------------------------------------------ textures
    copied, lost = [], []
    for f in sorted(x for x in all_files if x):
        src = TEX_SRC / f
        if src.exists():
            shutil.copyfile(src, TEXTURES / f)
            copied.append(f)
        else:
            lost.append(f)
    if lost:
        log("ERROR: textures named by materials but not in ArtSource/lagoon/textures:", lost)

    # ------------------------------------------------------------ read the .glb files back
    stats, group_tris, group_inst_tris = {}, Counter(), Counter()
    model_group = {}
    for p in placements:
        model_group.setdefault(p["model"], p["group"])
    problems = []
    for name, path in sorted(written.items()):
        tris, attrs, mats, gltf, blob = glb_stats(path)
        stats[name] = (tris, attrs, mats, gltf, blob)
        group_tris[model_group.get(name, "?")] += tris
    for p in placements:
        group_inst_tris[p["group"]] += stats[p["model"]][0]
    # Every vertex-tinted material's meshes must carry COLOR_0, rocks two UV sets, the ground both.
    for name, (tris, attrs, mats, gltf, blob) in stats.items():
        for mi, m in enumerate(gltf.get("meshes", [])):
            for prim in m["primitives"]:
                mname = gltf["materials"][prim["material"]]["name"] if "material" in prim else ""
                if mname in tint_attr and "COLOR_0" not in prim["attributes"]:
                    problems.append(f"{name}: {mname} has no COLOR_0")
                if mname == "rock" and "TEXCOORD_1" not in prim["attributes"]:
                    problems.append(f"{name}: rock has no TEXCOORD_1")
    g_gltf, g_blob = stats["ground"][3], stats["ground"][4]
    gprim = g_gltf["meshes"][0]["primitives"][0]
    gc = accessor(g_gltf, g_blob, gprim["attributes"]["COLOR_0"])
    gu = accessor(g_gltf, g_blob, gprim["attributes"]["TEXCOORD_1"])
    gp = accessor(g_gltf, g_blob, gprim["attributes"]["POSITION"])
    # Read-back check: decode court_in at the vertex nearest the court's centre, and the extremes.
    k = int(np.argmin(np.linalg.norm(gp[:, [0, 2]], axis=1)))
    log("ground read back: COLOR_0 ranges", [(round(float(gc[:, i].min()), 4), round(float(gc[:, i].max()), 4)) for i in range(4)],
        "TEXCOORD_1 (glTF, V flipped) ranges", [(round(float(gu[:, i].min()), 4), round(float(gu[:, i].max()), 4)) for i in range(2)])
    log("ground read back at the court centre: court_in =", round(FIELD_LO + gc[k, 0] * (FIELD_HI - FIELD_LO), 3),
        "m (expected", round(float(vals["court_in"][np.argmin(np.linalg.norm(co[:, :2] - np.array([0, 0]), axis=1))]), 3), "m)")
    rk = stats["rock_boulder_01"][3]
    rp = rk["meshes"][0]["primitives"][0]["attributes"]
    log("rock_boulder_01 attributes:", sorted(rp))
    if problems:
        for p_ in problems:
            log("ERROR:", p_)

    # ------------------------------------------------------------ coordinate proof
    dg = bpy.context.evaluated_depsgraph_get()

    def blender_world_verts(o):
        ev_ = o.evaluated_get(dg)
        me = ev_.to_mesh()
        a = np.empty(len(me.vertices) * 3, dtype=np.float64)
        me.vertices.foreach_get("co", a)
        ev_.to_mesh_clear()
        a = a.reshape(-1, 3)
        mw = np.array(o.matrix_world)
        w = (mw[:3, :3] @ a.T).T + mw[:3, 3]
        return np.stack([-w[:, 0], w[:, 2], -w[:, 1]], axis=1)      # (-x, z, -y)

    def prove(p, placed_obj_for_node):
        gltf, blob = stats[p["model"]][3], stats[p["model"]][4]
        worlds = node_worlds(gltf)
        mu = np.array(p["matrix"], dtype=np.float64).reshape(4, 4)
        worst, checked = 0.0, 0
        for i, w in worlds.items():
            node = gltf["nodes"][i]
            if node.get("mesh") is None:
                continue
            obj = placed_obj_for_node(node["name"])
            if obj is None:
                continue
            pos = accessor(gltf, blob, gltf["meshes"][node["mesh"]]["primitives"][0]["attributes"]["POSITION"])
            picks = pos[np.linspace(0, len(pos) - 1, 6).astype(int)]
            g = (w[:3, :3] @ picks.T).T + w[:3, 3]            # prototype-root space, glTF axes
            u_local = np.stack([-g[:, 0], g[:, 1], g[:, 2]], axis=1)   # glTFast negates X
            U = (mu[:3, :3] @ u_local.T).T + mu[:3, 3]
            B_ = blender_world_verts(obj)
            for q in U:
                worst = max(worst, float(np.min(np.linalg.norm(B_ - q, axis=1))))
                checked += 1
            break
        return worst, checked

    proofs = []
    house = next(p for p in placements if p["group"] == "Houses" and p["model"] in source_roots)
    tilted = next((p for p in placements if p["group"] == "Props" and p["model"] in source_roots
                   and (p["_m"].to_3x3().normalized() @ Vector((0, 0, 1))).z < 0.995), None)
    scaled = max((p for p in placements if p["group"] == "Rocks"),
                 key=lambda p: max(p["_m"].to_scale()) / min(p["_m"].to_scale()))
    by_matrix = {}
    for col in (village, props):
        for o in col.objects:
            if o.parent is None:
                by_matrix[tuple(round(v, 5) for row in o.matrix_world for v in row)] = o

    def hierarchy_finder(p):
        root = by_matrix[tuple(round(v, 5) for row in p["_m"] for v in row)]
        src = source_roots[p["model"]]
        name_to_mesh = {o.name: o.data.name for o in src.children_recursive if o.type == "MESH"}

        def find(node_name):
            mesh = name_to_mesh.get(node_name)
            return next((d for d in root.children_recursive if d.type == "MESH" and d.data.name == mesh), None)
        return find

    for label, p in (("house", house), ("tilted prop", tilted)):
        if p is None:
            log("coordinate proof: no", label, "placement found")
            continue
        worst, n = prove(p, hierarchy_finder(p))
        proofs.append((label, p["model"], worst, n))
    rock_obj = next(o for o in bpy.data.collections["Boulders"].objects
                    if mats_close(o.matrix_world, scaled["_m"], 1e-6))
    worst, n = prove(scaled, lambda _n: rock_obj)
    proofs.append(("scaled rock", f"{scaled['model']} scale {tuple(round(s, 3) for s in scaled['_m'].to_scale())}", worst, n))
    if tilted is not None:
        up = tilted["_m"].to_3x3().normalized() @ Vector((0, 0, 1))
        log(f"tilted prop: {tilted['model']} up axis {tuple(round(c, 3) for c in up)} "
            f"({round(math.degrees(math.acos(max(-1, min(1, up.z)))), 2)} degrees off vertical)")
    for label, model, worst, n in proofs:
        log(f"coordinate proof, {label} ({model}): {n} vertices, worst error {worst:.2e} m")

    # ------------------------------------------------------------ the sun
    sun = next((o for o in bpy.data.objects if o.type == "LIGHT" and o.data.type == "SUN"), None)
    sun_json = {"euler": [50.0, 0.0, 0.0], "color": [1.0, 1.0, 1.0], "intensity": 1.0}
    if sun is not None:
        rb = sun.matrix_world.to_3x3().normalized()
        fwd = C3 @ (rb @ Vector((0, 0, -1)))      # a Blender sun shines down its local -Z
        upv = C3 @ (rb @ Vector((0, 1, 0)))
        right = upv.cross(fwd).normalized()       # Unity: right = Cross(up, forward), same formula
        upv = fwd.cross(right).normalized()
        ex = math.degrees(math.asin(max(-1.0, min(1.0, -fwd.y))))
        ey = math.degrees(math.atan2(fwd.x, fwd.z))
        ez = math.degrees(math.atan2(right.y, upv.y))
        sun_json = {"euler": [round(ex, 4) % 360, round(ey, 4) % 360, round(ez, 4) % 360],
                    "color": enc(sun.data.color[:3]), "intensity": round(sun.data.energy, 4),
                    "forward": [round(c, 5) for c in fwd], "colorLinear": [round(c, 5) for c in sun.data.color[:3]],
                    "blenderEnergy": round(sun.data.energy, 4)}
        log("sun:", sun_json)

    # ------------------------------------------------------------ the layout JSON
    data = {
        "note": ("Written by tools/export_lagoon_unity.py from ArtSource/lagoon/lagoon_cove.blend. Matrices are UNITY axes, "
                 "row-major (m00 m01 m02 m03 m10 ...): C . M_blender . C^T with C = [[-1,0,0],[0,0,1],[0,-1,0]], so a "
                 "Blender point (x, y, z) is Unity (-x, z, -y), glTFast's conversion. Colours (tint, emission, sun color) "
                 "are sRGB-encoded like Kanto's: assign with new Color(r, g, b) in the linear-space project. Vertex colours "
                 "in the .glb files are LINEAR. cutoff 0 means no alpha cut. Sun intensity is Blender's sun strength "
                 "(W/m2), not a Unity intensity."),
        "gameplay": {"box": 7, "throw": 8, "spawn": 9, "half": 13, "walk": 0.0, "water": COVE.WATER, "seabed": COVE.SEABED},
        "sun": sun_json,
        "materials": specs,
        "placements": [{k: v for k, v in p.items() if not k.startswith("_")} for p in placements],
    }
    LAYOUT.write_text(json.dumps(data, indent=1))

    # ------------------------------------------------------------ report
    per_group = Counter(p["group"] for p in placements)
    protos_by_group = defaultdict(set)
    for p in placements:
        protos_by_group[p["group"]].add(p["model"])
    size = sum(f.stat().st_size for f in written.values())
    log(f"{len(written)} .glb prototypes, {len(placements)} placements, {len(specs)} materials, {len(copied)} textures copied")
    for g in sorted(per_group):
        log(f"  {g:10s} prototypes {len(protos_by_group[g]):3d}  placements {per_group[g]:4d}  "
            f"prototype triangles {group_tris[g]:8d}  placed triangles {group_inst_tris[g]:9d}")
    log(f"  total .glb size {size / 1e6:.2f} MB; placed triangles {sum(group_inst_tris.values())}")
    log("materials per kind:", dict(kinds))
    log("fell back to plain:", fallbacks if fallbacks else "none")
    log("overrides used:", dict(Counter((o["from"], o["to"]) for p in placements for o in p["overrides"])))
    log("variants (placed copies that differ from their source):", sorted(unique_hierarchies) or "none")
    log("layout ->", LAYOUT)


if __name__ == "__main__":
    main()
