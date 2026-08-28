"""Build the Unity slipper roster from the licensed source GLBs.

Run with Blender, not the system Python:
  blender -b --factory-startup --python tools/build_slipper_roster.py

Every output is one shoe, 0.432 m toe to heel, centred over the origin and seated
on y=0 after Unity imports the glTF. Source files and their licences live together
under Art/models/kits/footwear so this pass is reproducible and auditable.
"""

from pathlib import Path

import bpy
import bmesh
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/TumbangPreso/Art/models/kits/footwear"
# The roster book resolves every model under Art/models (RosterBookBuilder.ArtRoot),
# and a mesh written anywhere else is a mesh the book cannot find.
OUTPUT = ROOT / "Assets/TumbangPreso/Art/models"
TARGET_LENGTH = 0.432
MAX_HEIGHT = 0.160


RECIPES = (
    # id, source, object-name fragments, optional (axis, keep-positive), colours
    ("tsinelas", "source_tsinelas_flip_flops.glb", ("group1977808981",), None,
     ((0.12, 0.10, 0.08, 1.0), (0.74, 0.59, 0.27, 1.0))),
    ("pambahay", "source_tsinelas_flip_flops.glb", ("group1162052169",), None,
     ((0.18, 0.10, 0.20, 1.0), (0.73, 0.38, 0.56, 1.0))),
    ("spartan", "source_spartan_flip_flops.glb", ("Box003", "Line005"), None,
     ((0.055, 0.048, 0.045, 1.0), (0.70, 0.055, 0.075, 1.0))),
    ("alpombra", "source_alpombra_slippers.glb", ("slippers",), (0, True),
     ((0.45, 0.19, 0.28, 1.0), (0.83, 0.67, 0.55, 1.0))),
    ("heels", "source_heels_stiletto.glb", ("Stillettos",), (1, False),
     ((0.085, 0.065, 0.075, 1.0), (0.62, 0.10, 0.22, 1.0))),
    ("sandals", "source_sandals.glb", ("Sandal",), None,
     ((0.22, 0.25, 0.10, 1.0), (0.18, 0.095, 0.055, 1.0))),
)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for block in list(datablocks):
            datablocks.remove(block)


def material_colour(material, colour):
    material.diffuse_color = colour
    material.metallic = 0.0
    material.roughness = 0.82
    if material.use_nodes:
        node = material.node_tree.nodes.get("Principled BSDF")
        if node is not None:
            node.inputs["Base Color"].default_value = colour
            node.inputs["Metallic"].default_value = 0.0
            node.inputs["Roughness"].default_value = 0.82


def apply_world_transform(obj):
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    if obj.parent is not None:
        bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.select_set(False)


def slice_half(obj, axis, keep_positive):
    mesh = obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    doomed = [v for v in bm.verts if (v.co[axis] < 0.0) == keep_positive]
    bmesh.ops.delete(bm, geom=doomed, context="VERTS")
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    bpy.context.view_layer.update()


def bounds(objects):
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    low = Vector((min(v.x for v in points), min(v.y for v in points), min(v.z for v in points)))
    high = Vector((max(v.x for v in points), max(v.y for v in points), max(v.z for v in points)))
    return low, high


def build(entry_id, source_name, fragments, half, colours):
    clear_scene()
    bpy.ops.import_scene.gltf(filepath=str(SOURCE / source_name))

    meshes = []
    for obj in list(bpy.context.scene.objects):
        wanted = obj.type == "MESH" and obj.name != "Cube" and any(f in obj.name for f in fragments)
        if not wanted:
            bpy.data.objects.remove(obj, do_unlink=True)
            continue
        apply_world_transform(obj)
        meshes.append(obj)

    if not meshes:
        raise RuntimeError(f"{entry_id}: source contains none of {fragments}")

    if half is not None:
        slice_half(meshes[0], *half)

    # The source assets distinguish the sole and upper through object/material slots. Preserve
    # that silhouette cue with two flat, role-safe colours before Unity swaps in the toon shader.
    for obj_index, obj in enumerate(meshes):
        for material_index, material in enumerate(obj.data.materials):
            if material is None:
                continue
            material_colour(material, colours[(obj_index + material_index) % len(colours)])

    low, high = bounds(meshes)
    horizontal = max(high.x - low.x, high.y - low.y)
    if horizontal <= 0.0:
        raise RuntimeError(f"{entry_id}: zero horizontal extent")

    scale = TARGET_LENGTH / horizontal
    centre = (low + high) * 0.5
    height_scale = min(1.0, MAX_HEIGHT / max((high.z - low.z) * scale, 0.0001))
    for obj in meshes:
        obj.scale = Vector((scale, scale, scale * height_scale))
        obj.location = Vector((-centre.x * scale, -centre.y * scale,
                               -low.z * scale * height_scale))
        apply_world_transform(obj)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()

    model = bpy.context.object
    model.name = f"tsinelas_{entry_id}"
    model.data.name = model.name

    # A narrow two-segment bevel catches the toon key light without rounding away the deliberately
    # low-poly silhouette. It is 0.6 percent of the fixed 0.432 m length on every shoe.
    bevel = model.modifiers.new("Edge highlight", "BEVEL")
    bevel.width = TARGET_LENGTH * 0.006
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bevel.angle_limit = 0.60

    OUTPUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    model.select_set(True)
    bpy.context.view_layer.objects.active = model
    bpy.ops.export_scene.gltf(
        filepath=str(OUTPUT / f"tsinelas_{entry_id}.glb"),
        export_format="GLB",
        use_selection=True,
        export_apply=True,
        export_yup=True,
        export_materials="EXPORT",
        export_cameras=False,
        export_lights=False,
    )

    final_low, final_high = bounds((model,))
    print(f"[footwear] {entry_id}: {(final_high-final_low)[:]} -> {OUTPUT / f'tsinelas_{entry_id}.glb'}")


def main():
    for recipe in RECIPES:
        build(*recipe)


if __name__ == "__main__":
    main()
