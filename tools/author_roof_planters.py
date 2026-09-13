"""Original resident pots: simple volumes and thick leaves, in TUMP's blocky style."""
import math
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'MapSource/environment/roof-residents/study-v1'
OUT.mkdir(parents=True,exist_ok=True)

def mat(name,colour):
    m=bpy.data.materials.new(name);m.diffuse_color=(*colour,1);m.use_nodes=True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*colour,1)
    m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.8
    return m

def mesh(name,vertices,faces,material):
    m=bpy.data.meshes.new(name);m.from_pydata(vertices,[],faces);m.update()
    o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);o.data.materials.append(material)
    return o

def pot(name,material,soil,r=.19,h=.31):
    # Hollow lip and a visible soil inset rather than a solid coloured cylinder.
    rings=[(r*.74,0),(r,h-.025),(r*1.035,h-.025),(r*1.035,h+.015),(r*.86,h+.015),(r*.83,h-.04)]
    vertices=[(math.cos(i*math.tau/12)*radius,math.sin(i*math.tau/12)*radius,z) for radius,z in rings for i in range(12)]
    faces=[]
    for j in range(len(rings)-1):
        for i in range(12):a=j*12+i;b=j*12+(i+1)%12;faces.append((a,b,b+12,a+12))
    faces.append(tuple(range(11,-1,-1)))
    mesh(name,vertices,faces,material)
    bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r*.84,depth=.018,location=(0,0,h-.045))
    bpy.context.object.name='Dark inset soil';bpy.context.object.data.materials.append(soil)

def leaf(name,start,end,width,material,curve=.045):
    start=Vector(start);end=Vector(end);direction=end-start
    lateral=Vector((-direction.y,direction.x,0))
    if lateral.length<.001:lateral=Vector((1,0,0))
    lateral.normalize();points=[];faces=[]
    for t,w in [(0,.04),(.22,.62),(.46,1),(.7,.83),(.9,.4),(1,.015)]:
        center=start+direction*t+Vector((0,0,math.sin(t*math.pi)*curve))
        points.extend([tuple(center-lateral*width*w/2),tuple(center+Vector((0,0,.01))),tuple(center+lateral*width*w/2),tuple(center-Vector((0,0,.009)))])
    for i in range(5):
        for j in range(4):a=i*4+j;b=i*4+(j+1)%4;faces.append((a,b,b+4,a+4))
    faces.extend([(3,2,1,0),(20,21,22,23)])
    mesh(name,points,faces,material)

def rod(name,a,b,radius,material):
    a=Vector(a);b=Vector(b);delta=b-a
    bpy.ops.mesh.primitive_cylinder_add(vertices=6,radius=radius,depth=delta.length,location=(a+b)/2)
    o=bpy.context.object;o.name=name;o.rotation_euler=delta.to_track_quat('Z','Y').to_euler();o.data.materials.append(material)

for kind in ['pail-sansevieria','terracotta-broadleaf','small-aloe']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    clay=mat('Old terracotta',(.49,.28,.18));chalk=mat('Reused cream pail',(.76,.72,.59))
    soil=mat('Potting soil',(.18,.15,.10));dark=mat('Leaf shade',(.17,.29,.105));green=mat('Leaf green',(.30,.43,.17))
    stripe=mat('Quiet worn pail band',(.35,.44,.40));stem=mat('Stems',(.24,.32,.12))
    height=.33 if kind.startswith('pail') else .28 if kind.startswith('terracotta') else .18
    radius=.20 if kind.startswith('pail') else .22 if kind.startswith('terracotta') else .14
    pot('Resident plant pot',chalk if kind.startswith('pail') else clay,soil,radius,height)
    if kind.startswith('pail'):
        for i in range(12):
            a=i*math.tau/12;b=(i+1)*math.tau/12
            vertices=[]
            for z in [.195,.228]:
                r=radius*.74+radius*.26*z/(height-.025)+.001
                vertices.extend([(math.cos(t)*r,math.sin(t)*r,z) for t in [a,b]])
            mesh('Faded pail stripe',vertices,[(0,1,3,2)],stripe)
        for i in range(8):
            a=i*2.399;spread=.10+(i%3)*.023;tip=.44+(i%4)*.07
            x=math.cos(a);y=math.sin(a)
            leaf('Upright sword leaf',(x*.025,y*.025,height-.04),(x*spread,y*spread,height+tip),.075,dark if i%3==0 else green)
        for side in [-1,1]:
            rod('Pail handle support',(side*.192,0,.25),(side*.20,0,.42),.009,stripe)
        rod('Raised carry handle',(-.20,0,.42),(.20,0,.42),.012,stripe)
    elif kind.startswith('terracotta'):
        for i in range(7):
            a=i*2.399;tip=Vector((math.cos(a)*(.25+i%2*.05),math.sin(a)*(.25+i%2*.05),height+.30+i%3*.09))
            join=Vector((0,0,height-.03));mid=join.lerp(tip,.35)
            rod('Branch',join,mid,.009,stem)
            leaf('Broad pointed leaf',mid,tip,.17,green if i%2 else dark,.035)
    else:
        for i in range(9):
            a=i*2.399;reach=.22 if i<6 else .10
            leaf('Aloe blade',(0,0,height-.025),(math.cos(a)*reach,math.sin(a)*reach,height+.18+i%3*.055),.065,green)
    # One assembled plant keeps its material slots/vertices intact. Separate
    # leaves/handle pieces are not independent unsupported scenery props.
    bpy.ops.object.select_all(action='DESELECT')
    parts=[o for o in bpy.context.scene.objects if o.type=='MESH']
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join()
    bpy.context.object.name=kind
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(kind+'.blend')))
    bpy.ops.export_scene.gltf(filepath=str(OUT/(kind+'.glb')),export_format='GLB',export_yup=True,export_animations=False)
    # Native preview; scenery import and placement are reviewed separately in Unity.
    bpy.ops.object.camera_add(location=(1.2,-1.7,1.05));camera=bpy.context.object
    camera.rotation_euler=(Vector((0,0,.43))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=1.32
    scene=bpy.context.scene;scene.camera=camera;scene.world=bpy.data.worlds.new('Preview world')
    scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL'
    scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
    scene.render.resolution_x=700;scene.render.resolution_y=700;scene.render.resolution_percentage=100
    scene.render.filepath=str(OUT/(kind+'.png'));bpy.ops.render.render(write_still=True)
print('Wrote three original roof-resident plant sources to',OUT)
