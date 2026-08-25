"""Blender-side helper for build_pc_express_logo_mesh.py. Do not run directly."""

from __future__ import annotations

import json
from pathlib import Path
import sys

import bpy


def make_curve(layer: dict) -> bpy.types.Object:
    curve = bpy.data.curves.new(layer["name"], "CURVE")
    curve.dimensions = "2D"
    curve.resolution_u = 1
    curve.render_resolution_u = 1
    curve.fill_mode = "BOTH"
    curve.extrude = 0.5
    curve.resolution_v = 1

    for loop in layer["loops"]:
        spline = curve.splines.new("POLY")
        spline.points.add(len(loop) - 1)
        for point, (x, y) in zip(spline.points, loop):
            point.co = (x, y, 0.0, 1.0)
        spline.use_cyclic_u = True

    obj = bpy.data.objects.new(layer["name"], curve)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")

    minimum = min(vertex.co.z for vertex in obj.data.vertices)
    maximum = max(vertex.co.z for vertex in obj.data.vertices)
    depth = maximum - minimum
    for vertex in obj.data.vertices:
        ratio = (vertex.co.z - minimum) / depth if depth > 0 else 0.0
        vertex.co.z = layer["front"] + ratio * (layer["back"] - layer["front"])
    obj.select_set(False)
    return obj


def write_obj(output: Path, objects: list[bpy.types.Object], layers: list[dict]) -> None:
    mtl = output.with_suffix(".mtl")
    vertices = []
    sections = []
    offset = 0
    for obj, layer in zip(objects, layers):
        vertices.extend(obj.matrix_world @ vertex.co for vertex in obj.data.vertices)
        faces = ["f " + " ".join(str(offset + index + 1) for index in polygon.vertices)
                 for polygon in obj.data.polygons]
        sections.append((layer["name"], faces))
        offset += len(obj.data.vertices)

    with output.open("w", encoding="utf-8", newline="\n") as handle:
        handle.write("# Generated from the supplied official PC Express logo. Registered mark omitted.\n")
        handle.write(f"mtllib {mtl.name}\no PCExpressOfficialRaisedLogo\n")
        for vertex in vertices:
            handle.write(f"v {vertex.x:.6f} {vertex.y:.6f} {vertex.z:.6f}\n")
        for name, faces in sections:
            handle.write(f"\nusemtl {name}\ns 1\n")
            handle.write("\n".join(faces) + "\n")

    with mtl.open("w", encoding="utf-8", newline="\n") as handle:
        handle.write("# Generated from the supplied official PC Express logo.\n\n")
        for layer in layers:
            colour = layer["colour"]
            emission = layer["emission"]
            handle.write(f"newmtl {layer['name']}\n")
            handle.write(f"Kd {colour[0]:.5f} {colour[1]:.5f} {colour[2]:.5f}\n")
            handle.write(f"Ke {emission[0]:.5f} {emission[1]:.5f} {emission[2]:.5f}\n")
            handle.write("Ks 0 0 0\nNs 10\nd 1\nillum 2\n\n")


def main() -> None:
    payload = json.loads(Path(sys.argv[sys.argv.index("--") + 1]).read_text(encoding="utf-8"))
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    objects = [make_curve(layer) for layer in payload["layers"]]
    write_obj(Path(payload["output"]), objects, payload["layers"])


if __name__ == "__main__":
    main()
