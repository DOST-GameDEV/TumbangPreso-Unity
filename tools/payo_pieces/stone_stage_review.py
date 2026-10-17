"""The whole stage assembled from the stone pieces for one layout, to look at together. Blender, not the game.

  blender -b --python tools/payo_pieces/stone_stage_review.py -- [--layout=0] [--v=1]
  blender -b --python tools/payo_pieces/stone_stage_review.py -- --calm --shots=<dir>      the CALM pass: renders the
      stage twice, the approved glbs (Logs/payo/pieces/build/stone) and the calm ones (Logs/payo/pieces/calm/stone, normal
      maps on, the ink an inverted hull so the tangents survive), as <dir>/stage_<old|calm>_<view>.png

Reads Logs/payo/pieces/build/stone/blocks.json and Assets/TumbangPreso/Art/Payo/payo_layout.json (read only), stands
every block's glb at (x, top, z) with its extension under it, the movers at their start, the pads on their floors, and
writes Logs/payo/pieces/stone/stage_<layout>_<view>_vN.png: from 1.7 m on the core looking four ways, and from above.
A cool sky fill over a warm-white sun, an ink line, no fog: only the stone pieces, nothing of the rest of the map.
"""
import bpy, json, math, os, sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Logs", "payo", "pieces", "build", "stone")
SHOTS = os.path.join(ROOT, "Logs", "payo", "pieces", "stone")


def arg(name, default=None):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--" + name + "=")), default)


LAY, VER = int(arg("layout", "0")), arg("v", "1")
CALM = "--calm" in sys.argv
CALM_DIR = os.path.join(ROOT, "Logs", "payo", "pieces", "calm", "stone")
if CALM:
    SHOTS = arg("shots", os.path.join(CALM_DIR, "shots"))
table = json.load(open(os.path.join(OUT, "blocks.json")))
data = json.load(open(os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "Payo", "payo_layout.json"), encoding="utf-8"))
lay = data["layouts"][LAY]

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
sc.view_settings.view_transform = "Standard"
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.62, 0.90, 1)
w.node_tree.nodes["Background"].inputs[1].default_value = 0.9
for rot, energy in (((52, 0, -34), 2.7), ((118, 0, 150), 1.0)):
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
    sun.data.energy = energy; sun.rotation_euler = tuple(math.radians(a) for a in rot)
    sun.data.color = (1.0, 0.95, 0.86) if energy > 2 else (0.7, 0.82, 1.0)
cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam)
cam.data.clip_end = 3000
sc.camera = cam; sc.render.resolution_x, sc.render.resolution_y = 1280, 720

line = bpy.data.materials.new("ink"); line.use_nodes = True; line.use_backface_culling = True
b = line.node_tree.nodes["Principled BSDF"]
b.inputs["Base Color"].default_value = (0.01, 0.03, 0.05, 1); b.inputs["Roughness"].default_value = 1
cache = {}
tris = 0


def hull(o, thick):
    """The ink as a separate inverted hull (as tools/payo_pieces/calm_review.py): a solidify on the piece would drop its
    tangent space and the normal map with it."""
    import bmesh
    h = o.copy(); h.data = o.data.copy(); h.name = o.name + "_ink"
    sc.collection.objects.link(h)
    if "custom_normal" in h.data.attributes:
        h.data.attributes.remove(h.data.attributes["custom_normal"])
    h.data.materials.clear(); h.data.materials.append(line)
    bm = bmesh.new(); bm.from_mesh(h.data)
    for f in bm.faces:
        f.material_index = 0
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
    bmesh.ops.reverse_faces(bm, faces=bm.faces)
    bm.normal_update()
    for v in bm.verts:
        v.co = v.co - v.normal * thick
    bm.to_mesh(h.data); bm.free()
    h.visible_shadow = False
    h.parent = o
    h.matrix_parent_inverse = o.matrix_world.inverted()
    return h


def stand(glb, x, z, y, yaw=0.0, ink=0.03, src=None):
    """A piece at the layout's (x, y up, z). glTF (x, y, z) arrives in Blender as (x, -z, y)."""
    global tris
    path = os.path.join(src or OUT, glb)
    key = (src, glb)
    if CALM:
        if key not in cache:
            bpy.ops.import_scene.gltf(filepath=path)
            o = [m for m in bpy.context.selected_objects if m.type == "MESH"][0]
            o.parent = None
            o.location = (0, 0, 0); o.rotation_euler = (0, 0, 0)
            for m in o.data.materials:
                if m and m.use_nodes:
                    m.use_backface_culling = True
                    bs = m.node_tree.nodes.get("Principled BSDF")
                    tex = next((n for n in m.node_tree.nodes if n.type == "TEX_IMAGE" and n.image and "normal" not in n.image.name.lower()), None)
                    if bs and tex:
                        m.node_tree.links.new(tex.outputs["Color"], bs.inputs["Emission Color"])
                        bs.inputs["Emission Strength"].default_value = 0.9 if "glow" in m.name else 0.22
                        bs.inputs["Roughness"].default_value = 1.0
            bpy.context.view_layer.update()
            hull(o, ink)
            cache[key] = o
            group[src].append(o)
            inst = o
        else:
            base = cache[key]
            inst = base.copy(); sc.collection.objects.link(inst)
            h = [c for c in base.children][0].copy(); sc.collection.objects.link(h); h.parent = inst
            group[src].append(inst)
        inst.location = (x, -z, y)
        inst.rotation_euler = (0, 0, math.radians(yaw))
        tris += sum(len(p.vertices) - 2 for p in inst.data.polygons)
        return inst
    if glb not in cache:
        bpy.ops.import_scene.gltf(filepath=path)
        o = [m for m in bpy.context.selected_objects if m.type == "MESH"][0]
        for m in o.data.materials:
            if m:
                m.use_backface_culling = True
                bs = m.node_tree.nodes.get("Principled BSDF")
                tex = next((n for n in m.node_tree.nodes if n.type == "TEX_IMAGE"), None)
                if bs and tex:
                    m.node_tree.links.new(tex.outputs["Color"], bs.inputs["Emission Color"]); bs.inputs["Emission Strength"].default_value = 0.22
        o.data.materials.append(line)
        s = o.modifiers.new("ink", "SOLIDIFY")
        s.thickness = ink; s.offset = 1; s.use_flip_normals = True; s.material_offset = len(o.data.materials) - 1; s.use_rim = False
        cache[glb] = o
        inst = o
    else:
        inst = cache[glb].copy()
        sc.collection.objects.link(inst)
    inst.location = (x, -z, y)
    inst.rotation_euler = (0, 0, math.radians(yaw))
    tris += sum(len(p.vertices) - 2 for p in inst.data.polygons)
    return inst


rows = {r["id"]: r for r in table["blocks"]}
tops = {}
group = {None: []}


def place(src=None):
    for p in lay["pieces"]:
        r = rows[p["id"]]
        glb = r["glb"]
        if "stairDown" in p and p["id"] in table["stairs"]:
            glb = table["stairs"][p["id"]]["glb"]
        stand(glb, p["x"], p["z"], p["top"] - 0.01, src=src)
        stand(r["extension"], p["x"], p["z"], p["top"] - 0.01, src=src)
        tops[p["id"]] = p["top"]
    for m in lay.get("movers", []):
        kind = m["id"].rstrip("NSEWab")
        stand(table["movers"][kind], m["from"][0], m["from"][2], m["from"][1] - 0.01, src=src)
    for j in lay["jumpPads"]:
        stand(table["pads"]["jumpPads"], j["x"], j["z"], j["y"], ink=0.012, src=src)
    for sp in lay["speedPads"]:
        stand(table["pads"]["speedPads"], sp["x"], sp["z"], sp["y"], yaw={"+z": 0, "-z": 180, "+x": -90, "-x": 90}[sp["along"]], ink=0.012, src=src)
    for pk in lay["pickups"]:
        stand(table["pads"]["pickups"], pk["x"], pk["z"], pk["y"], ink=0.012, src=src)


if CALM:
    for src in (OUT, CALM_DIR):
        group[src] = []
        place(src)
else:
    place()
bpy.ops.mesh.primitive_plane_add(size=600, location=(0, 0, -40))
g = bpy.data.materials.new("mist"); g.use_nodes = True
g.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.62, 0.70, 0.78, 1)
g.node_tree.nodes["Principled BSDF"].inputs["Emission Color"].default_value = (0.62, 0.70, 0.78, 1)
g.node_tree.nodes["Principled BSDF"].inputs["Emission Strength"].default_value = 0.6
bpy.context.object.data.materials.append(g)
print("STAGE layout %d (%s): %d triangles of stone as placed" % (LAY, lay["name"], tris))

ch = lay["canHeight"]


def shoot(name, eye, look, fov):
    cam.data.sensor_fit = "VERTICAL"; cam.data.angle_y = math.radians(fov)
    cam.location = eye
    cam.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()
    if CALM:
        for tag, src in (("old", OUT), ("calm", CALM_DIR)):
            for k, objs in group.items():
                for o in objs:
                    o.hide_render = k != src
                    for c in o.children:
                        c.hide_render = k != src
            sc.render.filepath = os.path.join(SHOTS, "stage_%s_%s.png" % (tag, name))
            bpy.ops.render.render(write_still=True)
        return
    sc.render.filepath = os.path.join(SHOTS, "stage_%s_%s_v%s.png" % (lay["name"], name, VER))
    bpy.ops.render.render(write_still=True)


# Blender y is minus the layout's z: north (the layout's +z) is -y here.
shoot("eye_north", (0.0, 1.4, ch + 1.7), (6.0, -60.0, -1.0), 66)
shoot("eye_east", (-1.5, 0.0, ch + 1.7), (60.0, 8.0, -1.0), 66)
if not CALM:
    shoot("eye_south", (0.0, -1.4, ch + 1.7), (-6.0, 60.0, -1.0), 66)
    shoot("eye_west", (1.5, 0.0, ch + 1.7), (-60.0, -8.0, -1.0), 66)
shoot("outer", (20.9, -20.9, tops.get("towerNE", 0.6) + 1.7), (-8.0, 14.0, -2.0), 66)
shoot("above", (34.0, 44.0, 40.0), (0.0, 2.0, -6.0), 46)
print("done")
