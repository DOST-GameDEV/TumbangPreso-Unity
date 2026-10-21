"""Eskinita passenger tricycle derived from our original terminal author.

The Bayan source/model remain intact. Real-reference and concept decisions are in
docs/reports/map-by-map-refinement-2026-09-23/filipino-street-life.md.
Blender authors geometry only; native Unity owns every approval render.

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
parser.add_argument('--out', default='Logs/eskinita-tricycle-v1')
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


paint = material('Eskinita enamel metal', (.19, .31, .40))
cream = material('Warm canopy', (.63, .59, .47))
metal = material('Brushed sheet metal', (.40, .43, .42), .58, .25)
rubber = material('Tyres and grips', (.055, .058, .052))
seat = material('Dark vinyl seats', (.095, .12, .14))
glass = material('Smoked windscreen', (.32, .46, .55), .38)
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


def wheel(name, x, y, radius=.28, width=.16):
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
for name, radius, depth, y, mat in [('Octagonal headlight case', .142, .13, -.76, metal),
                                    ('Headlight lens', .113, .016, -.834, lamp)]:
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=radius, depth=depth,
        location=(-.53, y, .99), rotation=(math.pi / 2, 0, 0))
    o=bpy.context.object; o.name=name; o.data.materials.append(mat)
for x, end in [(-.85, -.96), (-.22, -.11)]:
    beam('Mirror stem', (x, -.60, 1.12), (end, -.61, 1.34), .026, metal)
    box('Mirror housing', (end, -.61, 1.36), (.15, .055, .13), rubber, .018)
    box('Mirror face', (end, -.64, 1.36), (.12, .009, .10), glass, .01)
beam('Footrest', (-.87, .03, .39), (.23, .03, .39), .06, metal)

# Passenger compartment: a low nose, sloping front screen and open right entry.
wheel('Sidecar wheel', .99, .48, .25, .14)
box('Sidecar floor', (.40, .03, .32), (.95, 1.43, .12), metal, .035)
nose_vertices=[(-.03,-.71,.36),(.83,-.71,.36),(.88,-.64,.80),(-.08,-.64,.80),
               (-.03,-.46,.36),(.83,-.46,.36),(.88,-.46,.80),(-.08,-.46,.80)]
nose_faces=[(0,1,2,3),(5,4,7,6),(4,0,3,7),(1,5,6,2),(3,2,6,7),(4,5,1,0)]
nose_mesh=bpy.data.meshes.new('Tapered sidecar nose');nose_mesh.from_pydata(nose_vertices,[],nose_faces)
nose=bpy.data.objects.new('Tapered sidecar nose',nose_mesh);bpy.context.collection.objects.link(nose);nose.data.materials.append(paint)
box('Sidecar rear body', (.40, .64, .67), (.95, .16, .67), paint, .04)
box('Passenger bench', (.40, .35, .58), (.79, .43, .13), seat, .025)
box('Passenger backrest', (.40, .57, .83), (.79, .12, .43), seat, .025)
box('Inner side panel', (-.05, .16, .56), (.09, .95, .38), paint)
# Broad faceted mudguard wraps the passenger wheel, with clear tyre space.
for a,b in zip([0,30,65,115,150],[30,65,115,150,180]):
    a,b=math.radians(a),math.radians(b)
    p=Vector((.99,.48+math.cos(a)*.335,.25+math.sin(a)*.335))
    q=Vector((.99,.48+math.cos(b)*.335,.25+math.sin(b)*.335))
    guard=box('Faceted passenger mudguard',(p+q)*.5,(.25,(q-p).length+.014,.055),metal,.006)
    guard.rotation_euler.x=math.atan2(q.z-p.z,q.y-p.y)
box('Entry step', (.94, -.12, .28), (.25, .47, .09), metal)
for x in [-.06, .86]:
    beam('Windscreen pillar', (x, -.59, .79), (x, -.40, 1.50), .055, metal)
    beam('Rear canopy post', (x, .66, .95), (x, .66, 1.57), .055, metal)
screen = box('Broad windscreen', (.40, -.49, 1.145), (.85, .025, .70), glass)
screen.rotation_euler.x = -math.atan2(.19, .71)
beam('Windscreen sill', (-.06, -.59, .80), (.86, -.59, .80), .06, metal)
box('Passenger canopy', (.40, .05, 1.59), (1.12, 1.51, .12), cream, .055)
box('Rear lamp', (.72, .73, .54), (.12, .025, .08), paint)


# Driver shade is supported from the rear chassis, never from the handlebar.
beam('Rear shade mount',(-.86,.66,.59),(-.20,.66,.59),.07,metal)
for x in [-.85,-.21]:
    beam('Driver shade rear support',(x,.66,.59),(x,.52,1.73),.047,metal)
    beam('Driver shade frame brace',(x,.22,.53),(x,.59,1.11),.036,metal)
box('Driver rain shade',(-.53,.02,1.78),(.86,1.34,.085),cream,.035)
box('Driver shade rear crossmember',(-.53,.52,1.72),(.77,.065,.065),metal,.012)

mark=material('Unit markings',(.80,.75,.62))
reflection=material('Glass sky reflection',(.53,.63,.66),.40)
def face(name,vertices,mat):
    m=bpy.data.meshes.new(name);m.from_pydata(vertices,[],[tuple(range(len(vertices)))])
    o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);o.data.materials.append(mat);return o

def nose_y(z):return -.71+(z-.36)/.44*.07-.003
face('Quiet unit stripe',[(-.025,nose_y(.53),.53),(.82,nose_y(.53),.53),(.827,nose_y(.59),.59),(-.032,nose_y(.59),.59)],mark)
# Small block-painted unit number, not a copied real operator logo or association.
for col,digit in enumerate(['111101101101111','111001001001001']):
    for row in range(5):
        for x in range(3):
            if digit[row*3+x]!='1':continue
            px=.58+col*.106+x*.026;z=.735-row*.027
            face('Painted07',[(px,nose_y(z),z),(px+.023,nose_y(z),z),(px+.023,nose_y(z+.024),z+.024),(px,nose_y(z+.024),z+.024)],mark)
# A low-contrast reflection shape sits on the actual sloped glass plane.
# Preserve the glass slope and leave a broad calm pane, without micro scratches.
local=[Vector((-.36,-.019,-.29)),Vector((-.13,-.019,-.29)),Vector((.34,-.019,.22)),Vector((.34,-.019,.31)),Vector((.16,-.019,.31))]
bpy.context.view_layer.update()
face('Broad reflected sky',[screen.matrix_world@p for p in local],reflection)
box('Windscreen header',(.40,-.40,1.485),(.98,.065,.095),mark,.008)
box('Rear registration plate',(.40,.731,.45),(.28,.012,.12),mark,.006)

# One mesh / construction-specific materials. Preserve broad planes and flat wheel facets.
bpy.ops.object.select_all(action='SELECT')
bpy.context.view_layer.objects.active = bpy.context.selected_objects[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'eskinita-passenger-tricycle'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bpy.ops.wm.save_as_mainfile(filepath=str(out / 'eskinita-passenger-tricycle.blend'))
bpy.ops.export_scene.gltf(filepath=str(out / 'eskinita-passenger-tricycle.glb'),
                          export_format='GLB', use_selection=True, export_animations=False)
if args.publish:
    for suffix, folder in [('.blend', ROOT / 'MapSource/environment/eskinita-street'),
                           ('.glb', ROOT / 'Assets/TumbangPreso/Art/models/eskinita-street')]:
        folder.mkdir(parents=True, exist_ok=True)
        shutil.copy2(out / (model.name + suffix), folder / (model.name + suffix))


print('Eskinita tricycle: vertices',len(model.data.vertices),'materials',len(model.data.materials))
