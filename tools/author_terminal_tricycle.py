"""Original blocky passenger tricycle for Bayan's roadside waiting bay.

Blender Z is up, -Y is forward. A motorcycle and an open right-hand sidecar
remain legible at court distance. No logos, decals or tiny mechanical fittings.
"""
import argparse
import math
import shutil
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--out', default='Logs/terminal-tricycle-v1')
parser.add_argument('--publish', action='store_true')
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
out = ROOT / args.out
out.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def material(name, color, roughness=.8, metallic=0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    shader = m.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Roughness'].default_value = roughness
    shader.inputs['Metallic'].default_value = metallic
    return m


paint = material('Terminal oxblood paint', (.31, .10, .12))
cream = material('Warm canopy', (.63, .59, .47))
metal = material('Brushed sheet metal', (.40, .43, .42), .58, .25)
rubber = material('Tyres and grips', (.055, .058, .052))
seat = material('Dark vinyl seats', (.12, .15, .12))
glass = material('Smoked windscreen', (.19, .26, .25), .38)
lamp = material('Headlamp lens', (.78, .76, .62), .45)


def box(name, at, size, mat, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=at)
    o = bpy.context.object
    o.name = name
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(mat)
    if bevel:
        mod = o.modifiers.new('Broad corner cuts', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return o


def beam(name, a, b, width, mat):
    a, b = Vector(a), Vector(b)
    o = box(name, (a + b) / 2, (width, width, (b - a).length), mat)
    o.rotation_euler = (b - a).to_track_quat('Z', 'Y').to_euler()
    return o


def wheel(name, x, y, radius=.28, width=.14):
    for suffix, r, w, mat in [(' tyre', radius, width, rubber),
                             (' hub', radius * .53, width + .012, metal)]:
        bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=r, depth=w,
                                           location=(x, y, radius), rotation=(0, math.pi / 2, 0))
        o = bpy.context.object
        o.name = name + suffix
        o.data.materials.append(mat)


# Motorcycle: two distinct wheels, high front fork and an exposed rider's seat.
wheel('Front wheel', -.53, -.77)
wheel('Rear wheel', -.53, .66)
box('Motorcycle frame', (-.53, .16, .44), (.25, 1.15, .16), metal)
box('Engine', (-.53, -.03, .48), (.32, .39, .30), metal, .045)
box('Fuel tank', (-.53, -.25, .82), (.40, .46, .25), paint, .065)
box('Rider saddle', (-.53, .27, .83), (.39, .66, .12), seat, .04)
box('Rear fender', (-.53, .66, .62), (.28, .52, .13), paint, .035)
box('Front mudguard', (-.53, -.77, .61), (.24, .50, .085), paint, .03)
for x in [-.64, -.42]:
    beam('Front fork', (x, -.77, .28), (x, -.58, 1.10), .045, metal)
beam('Handlebar', (-.89, -.60, 1.12), (-.18, -.60, 1.12), .04, metal)
for x in [-.85, -.22]:
    box('Handle grip', (x, -.60, 1.12), (.16, .065, .065), rubber)
box('Headlight case', (-.53, -.73, .99), (.25, .18, .22), metal, .03)
box('Headlight', (-.53, -.827, .99), (.19, .02, .16), lamp, .02)
beam('Footrest', (-.87, .03, .39), (.23, .03, .39), .06, metal)

# Passenger compartment: a low nose, sloping front screen and open right entry.
wheel('Sidecar wheel', .99, .48, .25, .14)
box('Sidecar floor', (.40, .03, .32), (.95, 1.43, .12), metal, .035)
box('Sidecar lower nose', (.40, -.59, .58), (.95, .20, .47), paint, .045)
box('Sidecar rear body', (.40, .64, .67), (.95, .16, .67), paint, .04)
box('Passenger bench', (.40, .35, .58), (.79, .43, .13), seat, .025)
box('Passenger backrest', (.40, .57, .83), (.79, .12, .43), seat, .025)
box('Inner side panel', (-.05, .16, .56), (.09, .95, .38), paint)
box('Wheel cover', (.96, .47, .59), (.28, .65, .12), paint, .045)
box('Entry step', (.94, -.12, .28), (.25, .47, .09), metal)
for x in [-.06, .86]:
    beam('Windscreen pillar', (x, -.59, .79), (x, -.40, 1.50), .055, metal)
    beam('Rear canopy post', (x, .66, .95), (x, .66, 1.57), .055, metal)
screen = box('Broad windscreen', (.40, -.49, 1.145), (.85, .025, .70), glass)
screen.rotation_euler.x = -math.atan2(.19, .71)
beam('Windscreen sill', (-.06, -.59, .80), (.86, -.59, .80), .06, metal)
box('Passenger canopy', (.40, .05, 1.59), (1.12, 1.51, .12), cream, .055)
box('Rear lamp', (.72, .73, .54), (.12, .025, .08), paint)

# One mesh / seven materials. Preserve broad planes and flat wheel facets.
bpy.ops.object.select_all(action='SELECT')
bpy.context.view_layer.objects.active = bpy.context.selected_objects[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'bayan-passenger-tricycle'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bpy.ops.wm.save_as_mainfile(filepath=str(out / 'bayan-passenger-tricycle.blend'))
bpy.ops.export_scene.gltf(filepath=str(out / 'bayan-passenger-tricycle.glb'),
                          export_format='GLB', use_selection=True, export_animations=False)
if args.publish:
    for suffix, folder in [('.blend', ROOT / 'MapSource/environment/terminal'),
                           ('.glb', ROOT / 'Assets/TumbangPreso/Art/models/terminal')]:
        folder.mkdir(parents=True, exist_ok=True)
        shutil.copy2(out / (model.name + suffix), folder / (model.name + suffix))

scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.view_settings.view_transform = 'Standard'
scene.world.color = (.11, .13, .15)
shading = scene.display.shading
shading.light = 'STUDIO'
shading.color_type = 'MATERIAL'
shading.show_shadows = True
shading.show_specular_highlight = False
shading.background_type = 'WORLD'
camera = bpy.data.objects.new('Review camera', bpy.data.cameras.new('Review camera'))
bpy.context.collection.objects.link(camera)
scene.camera = camera
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 3.0
scene.render.resolution_x = 950
scene.render.resolution_y = 800
scene.render.resolution_percentage = 100
for name, at in [('front', (3, -5, 2.8)), ('back', (-3, 5, 2.7))]:
    camera.location = at
    camera.rotation_euler = (Vector((.05, 0, .78)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.filepath = str(out / (name + '.png'))
    bpy.ops.render.render(write_still=True)
print('Tricycle vertices:', len(model.data.vertices), 'faces:', len(model.data.polygons))
