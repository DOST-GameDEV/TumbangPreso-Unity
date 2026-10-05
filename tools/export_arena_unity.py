"""Export the Arena (ArtSource/arena/arena_assembly.blend) for Unity (ARENA-1.5).

  blender -b ArtSource/arena/arena_assembly.blend --python tools/export_arena_unity.py

Writes, and touches nothing else (the .blend is NEVER saved; the process exits without saving):

  Assets/TumbangPreso/Art/Arena/Models/<model>.glb    the bowl, the roof, the hull, the city, the shaft rim, the three craft
  Assets/TumbangPreso/Art/Arena/Stage/stage_<layout>_<id>.glb    one per stage piece per layout (ArenaSceneBuilder finds them by name)
  Assets/TumbangPreso/Art/Arena/Props/arena_<prop>.glb           the jump pad, the speed pad, the pickup, the drone (several named parts each)
  Assets/TumbangPreso/Art/Arena/arena_layout.json                materials, placements, counts (the Ilalim format, read by ArenaArtPlacer)

The textures are already where Unity reads them (Assets/TumbangPreso/Art/Arena/Textures, written by
tools/author_arena_textures_*.py): this script only checks that every file a material names is there.

THE FRAME, AND WHY EVERY .glb HERE IS IN THE GAME'S OWN SPACE.
  Blender: metres, z up, y north, the can at the origin, a bearing b at radius r is (r sin b, r cos b).
  The game (tools/arena_layouts.json): y up, north is +z, a bearing b at radius r is x = r sin b, z = r cos b.
  So a Blender point (x, y, z) must land at Unity (x, z, y): the matrix D below. That is the Ilalim
  frame (tools/export_ilalim_unity.py), and it is what the stage author wrote. The city author's
  (-x, z, -y) is what glTFast gives a RAW export (Blender's exporter writes (x, z, -y), glTFast negates
  X): the matrix C. C and D differ by half a turn about the vertical, so a raw export dropped at the
  origin would stand the north tower in the south and every layout 180 degrees out of register with
  its art. Neither is a mirror: D swaps two axes AND the handedness changes (Blender is right-handed,
  Unity left-handed), so signs and the logo read the right way round.
  Ilalim answers this with a placement matrix D . M . C^T on every prototype. Here the stage pieces
  are instantiated by name with NO matrix (ArenaSceneBuilder.Visual), and the craft are steered by
  LookRotation, so every mesh is turned half a turn about Blender's z BEFORE export (R below; a proper
  rotation, normals and winding untouched). Then C . R = D: a vertex p of any .glb is at D . p in the
  model's own Unity space, and a placement's matrix is simply D . M . D (the identity for everything
  the kits modelled in place). A part with its own pivot inside a prop (the drone's prongs) is
  exported at R . M . R for the same reason. The read-back at the end proves it with numbers.

WHAT IS AN INSTANCE. Only the 36 craft share mesh data (3 meshes); they are exported once each and
Unity places and moves them (tools/arena_traffic.json). The kits baked every other repeat in place
with UVs that run on round the stadium (the four upper stands are the same geometry a quarter turn
apart, 0.0 m off, but their u differs by 35.3 tiles), so they are not instances and each is its own
file. `--dupes` prints that measurement.

THE THREE BIG RINGS ARE CUT INTO SECTORS (SECTORS below): the lower bowl (64,788 triangles), the
hull's body (15,872) and the aisle rails (8,064). Each was ONE mesh running all the way round the
can, and a camera standing in the middle of a ring can never frustum-cull it: all of it was drawn in
every frame, the two thirds behind the player included. A sector is culled like any other object.
The faces are the same faces (a face goes with its centre's bearing, a loose rail with its own
centre), so nothing changes in the picture: only meshes with no smooth face are cut, because a
smooth normal across a cut would change.

NORMALS are exported as authored (the bowl, the hull and the city are flat shaded; the roof and the
stage carry smooth faces and sharp edges). Nothing is decimated. No LOD meshes are made: see
ArenaArtPlacer for why (distances from the stage are fixed).
"""
import json
import math
import sys
import time
from collections import Counter, defaultdict
from pathlib import Path

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

TOOLS = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS))
import export_ilalim_unity as ILA      # noqa: E402  read_glb, accessor (the .glb reader the Ilalim export proves itself with)

ROOT = TOOLS.parent
OUT = ROOT / "Assets" / "TumbangPreso" / "Art" / "Arena"
MODELS, STAGE, PROPS, TEXTURES = OUT / "Models", OUT / "Stage", OUT / "Props", OUT / "Textures"
LAYOUT = OUT / "arena_layout.json"
TAG = "[arena-unity]"

# Blender (x, y, z) -> Unity (x, z, y).
D3 = Matrix(((1, 0, 0), (0, 0, 1), (0, 1, 0)))
D4 = D3.to_4x4()
# Half a turn about Blender's z, baked into every mesh before export (see the docstring).
R4 = Matrix.Rotation(math.pi, 4, "Z")

# The Holo kit's objects carry their own pivots (the middle of the globe, the balloon's tether
# point): each is exported in its own space and its placement's matrix puts it there, so Unity can
# turn it about that pivot (tools/arena_holo_motion.json, Runtime/Map/ArenaHoloMotion.cs).
KITS = (("arena_bowl", "Bowl"), ("arena_roof", "Roof"), ("arena_hull", "Hull"), ("arena_city", "City"), ("arena_holo", "Holo"))
# object -> (degrees a sector, how it is cut: whole loose "parts", or single "faces").
SECTORS = {
    "arena_bowl_rails": (30.0, "parts"),
    "arena_bowl_lower": (45.0, "faces"),
    "hull body (deck, rim, flank, tiers, keel)": (45.0, "faces"),
}
CRAFT_PREFIX, TRAIN = "city_craft_", "city_train"
# The props: file -> (node name in the file, object in the kit). The node names are what the
# runtime components look for.
PROP_SETS = {
    "arena_jump_pad": ("jump_base", "jump_cushion", "jump_chevron", "jump_ring"),
    "arena_speed_pad": ("speed_base", "speed_chevrons"),
    "arena_pickup": ("pickup_base", "pickup_cell", "pickup_halo"),
    # SAGIP (v6). Every moving part is its own node with its pivot where it turns (Runtime/Map/ArenaDrone.cs).
    "arena_drone": ("drone_body", "drone_fan", "drone_antenna", "drone_claw_0", "drone_claw_1", "drone_claw_2",
                    "drone_face_search", "drone_face_lock", "drone_face_carry", "drone_face_proud",
                    "drone_beam", "drone_beam_core", "drone_spot"),
}
SHAFT_RIM = "stage_shaft_rim"


def log(*a):
    print(TAG, *a, flush=True)


def safe(name):
    out, last = [], "_"
    for ch in name.lower():
        ch = ch if ch.isalnum() else "_"
        if ch == "_" and last == "_":
            continue
        out.append(ch)
        last = ch
    return "".join(out).strip("_")


def unity_matrix(m):
    r = D4 @ m @ D4
    return [round(r[i][j], 6) for i in range(4) for j in range(4)]


def world_of(ob):
    """The object's world matrix from its own location, rotation and scale. NOT `matrix_world`: the
    assembly hides the stage's collections, and a hidden object's matrix_world is never evaluated
    (the drone's prongs read as standing at the origin)."""
    if ob.parent is not None:
        raise RuntimeError(f"{ob.name} has a parent; the kits were expected to place every object on its own")
    return ob.matrix_basis.copy()


def tri_count(me):
    return sum(len(p.vertices) - 2 for p in me.polygons)


def coords(me):
    co = np.empty(len(me.vertices) * 3, dtype=np.float64)
    me.vertices.foreach_get("co", co)
    return co.reshape(-1, 3)


def to_unity(points):
    """Blender points (n x 3) as Unity points."""
    return np.stack([points[:, 0], points[:, 2], points[:, 1]], axis=1)


# ====================================================================== meshes

def game_mesh(ob, dg, name):
    """A new local mesh of the object as it renders (modifiers applied), turned half a turn about z
    (the docstring), with one UV map named UVMap."""
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
    me.transform(R4)
    me.name = name
    while len(me.uv_layers) > 1:
        me.uv_layers.remove(me.uv_layers[me.uv_layers[1].name])
    if len(me.uv_layers):
        me.uv_layers[0].name = "UVMap"
    me.update()
    return me


def loose_parts(me):
    """A label per vertex: which connected part it belongs to."""
    n = len(me.vertices)
    parent = list(range(n))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    ev = np.empty(len(me.edges) * 2, dtype=np.int64)
    me.edges.foreach_get("vertices", ev)
    for a, b in ev.reshape(-1, 2).tolist():
        ra, rb = find(a), find(b)
        if ra != rb:
            parent[max(ra, rb)] = min(ra, rb)
    return np.array([find(i) for i in range(n)])


def split_by_sector(me, degrees, name, how):
    """The mesh cut into one mesh per sector of bearing: whole loose parts ("parts", a part goes
    with its centroid) or single faces ("faces", a face goes with its centre). The mesh is already
    turned by R, so a bearing is read back through R."""
    def sector_at(c):
        return int((math.degrees(math.atan2(-c[0], -c[1])) % 360.0) // degrees)      # R undone: (x, y) was (-x, -y)
    co = coords(me)
    if how == "parts":
        label = loose_parts(me)
        sector_of_vertex = np.zeros(len(co), dtype=np.int64)
        for part in np.unique(label):
            sel = label == part
            sector_of_vertex[sel] = sector_at(co[sel].mean(axis=0))
        sectors = np.unique(sector_of_vertex).tolist()
    else:
        sector_of_face = np.array([sector_at(p.center) for p in me.polygons], dtype=np.int64)
        sectors = np.unique(sector_of_face).tolist()
    out = {}
    for sector in sectors:
        bm = bmesh.new()
        bm.from_mesh(me)
        if how == "parts":
            bm.verts.ensure_lookup_table()
            bmesh.ops.delete(bm, geom=[v for v in bm.verts if sector_of_vertex[v.index] != sector], context="VERTS")
        else:
            bm.faces.ensure_lookup_table()
            bmesh.ops.delete(bm, geom=[f for f in bm.faces if sector_of_face[f.index] != sector], context="FACES")
        piece = bpy.data.meshes.new(f"{name}_s{sector:02d}")
        bm.to_mesh(piece)
        bm.free()
        for m in me.materials:
            piece.materials.append(m)
        piece.update()
        out[sector] = piece
    return out


def open_edge_share(me):
    return ILA.open_edge_share(me)


def export_glb(objects, path):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.gltf(
        filepath=str(path), export_format="GLB", use_selection=True, export_apply=False, export_yup=True,
        export_materials="EXPORT", export_image_format="NONE", export_texcoords=True, export_normals=True,
        export_tangents=False, export_vertex_color="NONE", export_attributes=False, export_cameras=False,
        export_lights=False, export_animations=False, export_skins=False, export_morph=False, export_extras=False)


# ====================================================================== materials

def describe(m):
    """What the Unity builder needs to know of a kit material: its files, its Blender strength, how
    it blends. The per-material SETTINGS are the table in ArenaArtPlacer, by name."""
    spec = {"name": m.name, "shader": "painted", "albedo": "", "emissionMap": "", "emissionStrength": 0.0,
            "alphaFromAlbedo": False, "blend": "opaque", "twoSided": False, "size": [0, 0], "notes": []}
    nt = m.node_tree
    bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None) if nt else None
    if bsdf is None:
        spec["notes"].append("no Principled BSDF")
        return spec

    def image_of(sock):
        if not sock.is_linked:
            return None
        n = sock.links[0].from_node
        return n.image if n.type == "TEX_IMAGE" else None
    base = image_of(bsdf.inputs["Base Color"])
    emit = image_of(bsdf.inputs["Emission Color"])
    if base is not None:
        spec["albedo"] = Path(bpy.path.abspath(base.filepath)).name
        spec["size"] = [int(base.size[0]), int(base.size[1])]
    else:
        spec["notes"].append("no albedo image")
    strength = float(bsdf.inputs["Emission Strength"].default_value)
    if strength > 0:
        spec["emissionStrength"] = round(strength, 4)
        if emit is not None:
            spec["emissionMap"] = Path(bpy.path.abspath(emit.filepath)).name
    spec["alphaFromAlbedo"] = bool(bsdf.inputs["Alpha"].is_linked)
    method = getattr(m, "surface_render_method", "") or getattr(m, "blend_method", "")
    if spec["alphaFromAlbedo"]:
        spec["blend"] = "blend" if method in ("BLENDED", "BLEND") else "cutout"
    return spec


# ====================================================================== reading back

def node_points(gltf, blob):
    """Every mesh node of a .glb: (name, Unity points n x 3, Unity normals, triangles, materials),
    the node's own translation, rotation and scale applied, then X negated as glTFast does."""
    out = []
    for node in gltf.get("nodes", []):
        if node.get("mesh") is None:
            continue
        t = np.array(node.get("translation", (0, 0, 0)), dtype=np.float64)
        q = node.get("rotation", (0, 0, 0, 1))
        s = np.array(node.get("scale", (1, 1, 1)), dtype=np.float64)
        x, y, z, w = q
        rot = np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                        [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                        [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])
        pts, nrm, tris, mats = [], [], 0, []
        for prim in gltf["meshes"][node["mesh"]]["primitives"]:
            p = ILA.accessor(gltf, blob, prim["attributes"]["POSITION"])
            pts.append((rot @ (p * s).T).T + t)
            if "NORMAL" in prim["attributes"]:
                nrm.append((rot @ ILA.accessor(gltf, blob, prim["attributes"]["NORMAL"]).T).T)
            tris += gltf["accessors"][prim["indices"]]["count"] // 3
            mats.append(gltf["materials"][prim["material"]]["name"] if "material" in prim else None)
        pts = np.concatenate(pts)
        pts[:, 0] = -pts[:, 0]
        if nrm:
            nrm = np.concatenate(nrm)
            nrm[:, 0] = -nrm[:, 0]
        out.append((node.get("name", ""), pts, nrm, tris, mats))
    return out


def worst_distance(sample, cloud):
    """The furthest any sample point is from its nearest point of the cloud."""
    worst = 0.0
    for q in sample:
        worst = max(worst, float(np.sqrt(((cloud - q) ** 2).sum(axis=1).min())))
    return worst


# ====================================================================== main

def main():
    started = time.time()
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    dg = bpy.context.evaluated_depsgraph_get()
    for d in (MODELS, STAGE, PROPS):
        d.mkdir(parents=True, exist_ok=True)

    # The kit objects by their own names, then every existing object and mesh is renamed out of
    # the way: an exported node must carry exactly the name Unity looks for, and Blender would
    # hand a second "stage_tore_drum" the name "stage_tore_drum.001". Nothing is saved.
    by_name = {o.name: o for o in bpy.data.objects}
    collection_of = {}
    for c in bpy.data.collections:
        for o in c.objects:
            collection_of[o.name] = c.name
    for o in list(bpy.data.objects):
        o.name = "~" + o.name
    for me in list(bpy.data.meshes):
        me.name = "~" + me.name
    scene_col = bpy.context.scene.collection

    if "--dupes" in argv:
        dupes(by_name, collection_of)
        return

    # ------------------------------------------------------------ what goes out
    written = {}            # path -> expected: [(node name, Unity points, triangles, material names)]
    placements = []
    budget = defaultdict(lambda: {"placements": 0, "prototypes": 0, "placedTris": 0})
    used_materials = {}
    open_share = defaultdict(float)
    farthest = 0.0
    proof = {}

    def note_materials(me):
        for i, share in open_edge_share(me).items():
            if i < len(me.materials) and me.materials[i] is not None:
                open_share[me.materials[i].name] = max(open_share[me.materials[i].name], share)
        mi = np.empty(len(me.polygons), dtype=np.int64)
        me.polygons.foreach_get("material_index", mi)
        for i in np.unique(mi).tolist():
            if i < len(me.materials) and me.materials[i] is not None:
                used_materials[me.materials[i].name] = me.materials[i]

    def write(folder, file_name, parts):
        """parts: [(node name, mesh already turned by R, Blender matrix of the node)]."""
        objs, expect = [], []
        for node, me, m in parts:
            o = bpy.data.objects.new(node, me)
            if o.name != node:
                raise RuntimeError(f"Blender renamed the node {node} to {o.name}")
            scene_col.objects.link(o)
            o.matrix_world = R4 @ m @ R4
            objs.append(o)
            note_materials(me)
            # Where Unity must find it: D . M . p for every vertex p of the object as modelled.
            local = (np.array(R4.to_3x3()) @ coords(me).T).T              # R undone
            world = (np.array(m.to_3x3()) @ local.T).T + np.array(m.translation)
            mi = np.empty(len(me.polygons), dtype=np.int64)
            me.polygons.foreach_get("material_index", mi)
            expect.append((node, to_unity(world), tri_count(me), {me.materials[i].name for i in np.unique(mi).tolist()}))
        bpy.context.view_layer.update()
        path = folder / f"{file_name}.glb"
        export_glb(objs, path)
        for o in objs:
            bpy.data.objects.remove(o)
        written[path] = expect
        return path

    def place(model, group, obj_name, m, tris):
        placements.append({"model": model, "group": group, "object": obj_name, "matrix": unity_matrix(m)})
        b = budget[group]
        b["placements"] += 1
        b["placedTris"] += tris

    # ------------------------------------------------------------ the four kits
    craft = {}              # model -> mesh
    craft_places = []
    for coll_name, group in KITS:
        coll = bpy.data.collections.get(coll_name)
        if coll is None:
            log(f"ERROR: no collection {coll_name} in the file")
            continue
        for ob in sorted(coll.objects, key=lambda o: o.name):
            if ob.type != "MESH":
                continue
            src = ob.name[1:]
            m = world_of(ob)
            w = (np.array(m.to_3x3()) @ np.array([v.co[:] for v in ob.data.vertices]).T).T + np.array(m.translation)
            if src.startswith(CRAFT_PREFIX):
                # city_craft_<model>_<lane>_<nn>: one mesh per model, Unity places them.
                model = CRAFT_PREFIX + src[len(CRAFT_PREFIX):].split("_")[0]
                if model not in craft:
                    craft[model] = game_mesh(ob, dg, model)
                craft_places.append({"object": src, "model": model, "position": [round(float(v), 3) for v in to_unity(np.array([m.translation]))[0]]})
                continue
            farthest = max(farthest, float(np.linalg.norm(w, axis=1).max()))
            name = safe(src) if src.startswith(("arena_", "city_", "roof_")) else safe(f"{coll_name[len('arena_'):]}_{src}")
            me = game_mesh(ob, dg, name)
            if src in SECTORS and not any(p.use_smooth for p in me.polygons):
                degrees, how = SECTORS[src]
                pieces = split_by_sector(me, degrees, name, how)
                if sum(tri_count(piece) for piece in pieces.values()) != tri_count(me):
                    raise RuntimeError(f"cutting {src} into sectors lost or doubled faces")
                for sector, piece in sorted(pieces.items()):
                    write(MODELS, piece.name, [(piece.name, piece, Matrix.Identity(4))])
                    place(piece.name, group, piece.name, m, tri_count(piece))
                    budget[group]["prototypes"] += 1
                log(f"{src}: cut into {len(pieces)} sectors of {degrees:g} degrees ({how})")
                continue
            if src in SECTORS:
                log(f"WARNING: {src} has smooth faces and is not cut into sectors")
            write(MODELS, name, [(name, me, Matrix.Identity(4))])
            g = "Train" if src == TRAIN else group
            place(name, g, src, m, tri_count(me))
            budget[g]["prototypes"] += 1
            if src.startswith("city_T01") and src.endswith("_body"):
                proof["T01"] = (name, w)
    for model, me in sorted(craft.items()):
        write(MODELS, model, [(model, me, Matrix.Identity(4))])
    for c in craft_places:
        budget["Craft"]["placements"] += 1
        budget["Craft"]["placedTris"] += tri_count(craft[c["model"]])
    budget["Craft"]["prototypes"] = len(craft)

    # ------------------------------------------------------------ the stage
    layouts = json.loads((TOOLS / "arena_layouts.json").read_text(encoding="utf-8"))
    stage_models, stage_missing = [], []
    for lay in layouts["layouts"]:
        coll = bpy.data.collections.get("stage_" + lay["name"])
        have = {o.name[1:]: o for o in coll.objects if o.type == "MESH"} if coll else {}
        for piece in lay["pieces"]:
            name = f"stage_{lay['name']}_{piece['id']}"
            ob = have.get(name)
            if ob is None:
                stage_missing.append(name)
                continue
            if float(np.abs(np.array(world_of(ob)) - np.eye(4)).max()) > 1e-6:
                log(f"WARNING: {name} is not modelled in place (its matrix is not the identity); it is baked in")
            me = game_mesh(ob, dg, name)
            me.transform(R4 @ world_of(ob) @ R4)
            write(STAGE, name, [(name, me, Matrix.Identity(4))])
            stage_models.append({"model": name, "layout": lay["name"], "id": piece["id"], "tris": tri_count(me)})
            budget["Stage (all five layouts)"]["placements"] += 1
            budget["Stage (all five layouts)"]["prototypes"] += 1
            budget["Stage (all five layouts)"]["placedTris"] += tri_count(me)
        extra = sorted(set(have) - {f"stage_{lay['name']}_{p['id']}" for p in lay["pieces"]})
        if extra:
            log(f"WARNING: stage_{lay['name']} has meshes the layout data does not name: {extra}")
    if stage_missing:
        log("ERROR: stage pieces named by tools/arena_layouts.json with no mesh in the kit:", stage_missing)

    rim = by_name.get(SHAFT_RIM)
    if rim is not None:
        me = game_mesh(rim, dg, SHAFT_RIM)
        write(MODELS, SHAFT_RIM, [(SHAFT_RIM, me, Matrix.Identity(4))])
        place(SHAFT_RIM, "StageRim", SHAFT_RIM, world_of(rim), tri_count(me))
        budget["StageRim"]["prototypes"] += 1
    else:
        log("ERROR: no", SHAFT_RIM)

    prop_files = {}
    for file_name, nodes in PROP_SETS.items():
        parts = []
        for node in nodes:
            ob = by_name.get(node)
            if ob is None:
                log(f"ERROR: prop part {node} is not in the kit")
                continue
            parts.append((node, game_mesh(ob, dg, node), world_of(ob)))
        if parts:
            write(PROPS, file_name, parts)
            prop_files[file_name] = [{"node": n, "tris": tri_count(me), "position": [round(float(v), 4) for v in to_unity(np.array([m.translation]))[0]]}
                                     for n, me, m in parts]

    # ------------------------------------------------------------ stale files
    keep = {p.resolve() for p in written}
    for folder in (MODELS, STAGE, PROPS):
        for f in folder.glob("*.glb"):
            if f.resolve() not in keep:
                f.unlink()
                meta = Path(str(f) + ".meta")
                if meta.exists():
                    meta.unlink()
                log("removed stale", f.name)

    # ------------------------------------------------------------ materials
    specs, missing_textures = [], []
    for name in sorted(used_materials):
        spec = describe(used_materials[name])
        if open_share.get(name, 0.0) > 0.25:
            spec["twoSided"] = True
            spec["notes"].append(f"two-sided: {round(open_share[name] * 100)} per cent open edges")
        for key in ("albedo", "emissionMap"):
            if spec[key] and not (TEXTURES / spec[key]).exists():
                missing_textures.append(spec[key])
        specs.append(spec)
    if missing_textures:
        log("ERROR: textures named by materials but not in", TEXTURES, ":", sorted(set(missing_textures)))

    # ------------------------------------------------------------ read back and prove
    problems, worst_all, total_tris, total_bytes = [], 0.0, 0, 0
    read = {}
    for path, expect in sorted(written.items()):
        gltf, blob = ILA.read_glb(path)
        nodes = node_points(gltf, blob)
        read[path.stem] = nodes
        total_bytes += path.stat().st_size
        if len(nodes) != len(expect):
            problems.append(f"{path.name}: {len(nodes)} mesh nodes, expected {len(expect)}")
            continue
        got = {n[0]: n for n in nodes}
        for node, want_pts, want_tris, want_mats in expect:
            if node not in got:
                problems.append(f"{path.name}: no node named {node} (has {sorted(got)})")
                continue
            _, pts, nrm, tris, mats = got[node]
            total_tris += tris
            if tris != want_tris:
                problems.append(f"{path.name}/{node}: {tris} triangles, Blender has {want_tris}")
            if set(mats) != want_mats:
                problems.append(f"{path.name}/{node}: materials {sorted(str(m) for m in mats)}, expected {sorted(want_mats)}")
            off = max(float(np.abs(pts.min(axis=0) - want_pts.min(axis=0)).max()), float(np.abs(pts.max(axis=0) - want_pts.max(axis=0)).max()))
            sample = pts[np.linspace(0, len(pts) - 1, min(24, len(pts))).astype(int)]
            off = max(off, worst_distance(sample, want_pts))
            worst_all = max(worst_all, off)
            if off > 2e-3:
                problems.append(f"{path.name}/{node}: {off:.4f} m from where Blender has it")
            if len(nrm) == 0:
                problems.append(f"{path.name}/{node}: no normals")
    for p in problems[:40]:
        log("ERROR:", p)
    log(f"read-back: {len(written)} files, {total_tris} triangles, {len(problems)} problems, worst vertex {worst_all * 1000:.3f} mm from Blender's (x, z, y)")

    # The frame, in numbers a person can check against the kit and the gameplay data.
    proofs = []
    if "T01" in proof:
        name, w = proof["T01"]
        _, pts, _, _, _ = read[name][0]
        u = (pts.min(axis=0) + pts.max(axis=0)) / 2
        bx, by = (w[:, 0].min() + w[:, 0].max()) / 2, (w[:, 1].min() + w[:, 1].max()) / 2
        proofs.append(f"T01 (the NE landmark): Blender plan centre ({bx:.1f}, {by:.1f}), bearing {math.degrees(math.atan2(bx, by)) % 360:.1f}; "
                      f"read back from {name}.glb as glTFast imports it: Unity x {u[0]:.1f}, z {u[2]:.1f}, bearing {math.degrees(math.atan2(u[0], u[2])) % 360:.1f}")
    tore = next((l for l in layouts["layouts"] if l["name"] == "tore"), None)
    if tore is not None:
        for piece in tore["pieces"]:
            if piece["kind"] != "arc" or f"stage_tore_{piece['id']}" not in read:
                continue
            _, pts, _, _, _ = read[f"stage_tore_{piece['id']}"][0]
            top = pts[pts[:, 1] > pts[:, 1].max() - 0.02]
            bearings = np.degrees(np.arctan2(top[:, 0], top[:, 2]))
            mid = (piece["a0"] + piece["a1"]) / 2
            rel = (bearings - mid + 180) % 360 - 180
            radii = np.hypot(top[:, 0], top[:, 2])
            proofs.append(f"layout tore, {piece['id']}: the data says bearings {piece['a0']}..{piece['a1']}, r {piece['r0']}..{piece['r1']}, top {piece['top']}; "
                          f"stage_tore_{piece['id']}.glb's top face spans bearings {mid + rel.min():.1f}..{mid + rel.max():.1f}, r {radii.min():.2f}..{radii.max():.2f}, y {top[:, 1].max():.2f}")
    drone = read.get("arena_drone")
    if drone:
        for node, pts, _, _, _ in drone:
            if node.startswith("drone_claw_") or node in ("drone_antenna", "drone_face_search", "drone_beam"):
                c = (pts.min(axis=0) + pts.max(axis=0)) / 2
                proofs.append(f"{node}: centre Unity ({c[0]:.3f}, {c[1]:.3f}, {c[2]:.3f}), bearing {math.degrees(math.atan2(c[0], c[2])) % 360:.0f}")
    # The floodlight banks the roof author measured, against the lamp faces that went out.
    lights = json.loads((TOOLS / "arena_lights.json").read_text(encoding="utf-8"))
    lamp = [pts for stem, nodes in read.items() if stem.startswith("roof_") for (_, pts, _, _, mats) in nodes if "arena_roof_lamp" in mats]
    if lamp:
        cloud = np.concatenate(lamp)
        banks = lights["roof"]["floodlight_banks"]
        far = max(worst_distance(to_unity(np.array([b["position"]])), cloud) for b in banks)
        proofs.append(f"the {len(banks)} floodlight banks of tools/arena_lights.json, taken as (x, z, y), are each within {far:.2f} m of an exported roof vertex "
                      "(a bank's position is the middle of its lamp face)")
    for p in proofs:
        log("FRAME:", p)

    # ------------------------------------------------------------ the layout
    kit_tris = sum(v["placedTris"] for g, v in budget.items() if g != "Stage (all five layouts)")
    per_layout = defaultdict(int)
    for s in stage_models:
        per_layout[s["layout"]] += s["tris"]
    data = {
        "note": ("Written by tools/export_arena_unity.py from ArtSource/arena/arena_assembly.blend. Every .glb is in the GAME's "
                 "own space once glTFast has imported it: a Blender point (x, y, z) is Unity (x, z, y), north is +z, a bearing b "
                 "at radius r is x = r sin b, z = r cos b, the can at the origin. Matrices are Unity axes, row-major, D . M . D "
                 "with D = [[1,0,0],[0,0,1],[0,1,0]] (the identity for everything modelled in place). tools/arena_lights.json and "
                 "tools/arena_traffic.json are in the BLENDER frame (use points_blender, as (x, z, y)); the traffic file's "
                 "points_unity are (-x, z, -y), half a turn out, and are not used."),
        "extent": {"farthest": round(float(farthest), 1),
                   "note": "the farthest vertex of the bowl, roof, hull and city from the can, metres (the city floor's corners)"},
        "budget": [dict(group=g, **v) for g, v in sorted(budget.items())],
        "stageTrisPerLayout": dict(per_layout),
        "stage": stage_models,
        "props": [{"file": f, "parts": parts} for f, parts in sorted(prop_files.items())],
        "craft": craft_places,
        "lods": [],
        "materials": specs,
        "placements": placements,
    }
    LAYOUT.write_text(json.dumps(data, indent=1), encoding="utf-8")

    # ------------------------------------------------------------ report
    def size_of(folder):
        return sum(p.stat().st_size for p in written if p.parent == folder)
    log(f"{len(written)} .glb files, {total_bytes / 1e6:.1f} MB: Models {sum(1 for p in written if p.parent == MODELS)} files {size_of(MODELS) / 1e6:.1f} MB, "
        f"Stage {sum(1 for p in written if p.parent == STAGE)} files {size_of(STAGE) / 1e6:.1f} MB, Props {sum(1 for p in written if p.parent == PROPS)} files {size_of(PROPS) / 1e6:.2f} MB")
    for g, v in sorted(budget.items()):
        log(f"  {g:26s} placements {v['placements']:4d} prototypes {v['prototypes']:4d} triangles {v['placedTris']:8d}")
    log(f"  drawn at once: the kits {kit_tris} triangles, plus one stage layout ({min(per_layout.values())} to {max(per_layout.values())}), plus the props")
    log(f"{len(specs)} materials; with an emission map: {sum(1 for s in specs if s['emissionMap'])}; alpha: "
        f"{[(s['name'], s['blend']) for s in specs if s['blend'] != 'opaque']}; two-sided by open edges: {[s['name'] for s in specs if s['twoSided']]}")
    log(f"farthest vertex from the can: {farthest:.0f} m")
    log("layout ->", LAYOUT)
    log(f"export took {time.time() - started:.0f} s")
    if problems or stage_missing or missing_textures:
        log("EXPORT_FAILED")
        sys.exit(1)
    log("EXPORT_OK")


def dupes(by_name, collection_of):
    """Print which kit objects are the same geometry turned about the can, and how far their UVs
    are from being the same too (the docstring's "what is an instance")."""
    groups = defaultdict(list)
    for name, o in by_name.items():
        if o.type != "MESH" or collection_of.get(name) not in dict(KITS):
            continue
        groups[(len(o.data.vertices), len(o.data.polygons), tuple(s.material.name for s in o.material_slots))].append((name, o))
    for key, obs in sorted(groups.items(), key=lambda kv: kv[1][0][0]):
        if len(obs) < 2 or len({o.data.name for _, o in obs}) < 2:
            continue
        an, a = obs[0]
        A = np.array([world_of(a) @ v.co for v in a.data.vertices])
        ua = np.array([d.uv[:] for d in a.data.uv_layers[0].data])
        for bn, b in obs[1:]:
            B = np.array([world_of(b) @ v.co for v in b.data.vertices])
            best = (9e9, 0)
            for quarter in range(1, 8):
                th = math.radians(45.0 * quarter)
                c, s = math.cos(th), math.sin(th)
                err = float(np.abs(A @ np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]]).T - B).max())
                best = min(best, (err, 45 * quarter))
            ub = np.array([d.uv[:] for d in b.data.uv_layers[0].data])
            uerr = float(np.abs(ua - ub).max()) if ua.shape == ub.shape else float("nan")
            log(f"DUPES {an} -> {bn}: turned {best[1]} degrees the vertices are {best[0]:.4f} m apart; the UVs differ by up to {uerr:.3f}")


if __name__ == "__main__":
    main()
