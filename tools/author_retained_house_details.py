"""Construction additions fitted to the retained Kenney city houses.

The old building mesh, deep frames, chamfered roof silhouette and proportions are
the baseline, not the rejected thin replacement studies. Blender sources contain
that existing licensed reference; exported detail sets contain only new members.
Outputs stay in Logs until their Unity comparison has been reviewed.
"""
import argparse
import json
import math
import sys
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('--out',default='Logs/retained-house-details-v3')
parser.add_argument('--kinds',nargs='+',default=['a','c','e','o'])
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
out=ROOT/args.out;out.mkdir(parents=True,exist_ok=True)

def material(name,color):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.84
    return m

def box(name,at,size,mat,bevel=.015):
    bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.name=name;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat)
    if bevel:
        b=o.modifiers.new('Chamfered construction edge','BEVEL');b.width=bevel;b.segments=1
        bpy.ops.object.modifier_apply(modifier=b.name)
    return o

def windows(model):
    mesh=model.data;uv=mesh.uv_layers.active
    image=next(n.image for n in model.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE')
    pixels=list(image.pixels);w,h=image.size;unique={}
    for face in mesh.polygons:
        normal=model.matrix_world.to_3x3()@face.normal
        if abs(normal.z)>.1:continue
        coord=uv.data[face.loop_indices[0]].uv
        x=min(w-1,int(coord.x*w));y=min(h-1,int(coord.y*h));color=pixels[(y*w+x)*4:(y*w+x)*4+3]
        if not(color[2]>color[0]*1.6 and color[2]>color[1]*1.2):continue
        vertices=[model.matrix_world@mesh.vertices[i].co for i in face.vertices]
        low=Vector([min(v[a] for v in vertices) for a in range(3)])
        high=Vector([max(v[a] for v in vertices) for a in range(3)])
        if high.z-low.z<.35:continue
        key=tuple(round(v,3) for v in (*low,*high))
        unique[key]=(low,high,normal)
    return unique.values()

def clip_height(points,height,above):
    result=[]
    for a,b in zip(points,points[1:]+points[:1]):
        a_inside=a.z>=height if above else a.z<=height
        b_inside=b.z>=height if above else b.z<=height
        if a_inside:result.append(a)
        if a_inside!=b_inside:result.append(a+(b-a)*((height-a.z)/(b.z-a.z)))
    return result

def upper_cladding(model,backing,planks):
    mesh=model.data;uv=mesh.uv_layers.active
    image=next(n.image for n in mesh.materials[0].node_tree.nodes if n.type=='TEX_IMAGE')
    pixels=list(image.pixels);w,h=image.size
    vertices=[];faces=[];slots=[]
    def polygon(points,normal,depth,slot):
        start=len(vertices);vertices.extend([p+normal*depth for p in points]);faces.append(tuple(range(start,len(vertices))));slots.append(slot)
    for face in mesh.polygons:
        normal=(model.matrix_world.to_3x3()@face.normal).normalized()
        if abs(normal.z)>.08:continue
        coord=uv.data[face.loop_indices[0]].uv;x=min(w-1,int(coord.x*w));y=min(h-1,int(coord.y*h))
        color=pixels[(y*w+x)*4:(y*w+x)*4+3]
        if sum(color)/3<.63 or max(color)-min(color)>.12:continue
        points=[model.matrix_world@mesh.vertices[i].co for i in face.vertices]
        points=clip_height(points,2.17,True)
        if len(points)<3:continue
        # The actual solid wall remains. This is a fitted finish, set behind the
        # retained projecting window frames/glazing, not a new thin house shell.
        polygon(points,normal,.035,0)
        for band in range(18):
            low=2.17+band*.25;high=low+.238
            strip=clip_height(clip_height(points,low,True),high,False)
            if len(strip)>=3:polygon(strip,normal,.052,1+band%2)
    m=bpy.data.meshes.new('Fitted timber upper courses');m.from_pydata(vertices,[],faces)
    for mat in [backing,*planks]:m.materials.append(mat)
    for polygon,slot in zip(m.polygons,slots):polygon.material_index=slot
    o=bpy.data.objects.new('Fitted timber upper courses',m);bpy.context.collection.objects.link(o)

for kind in args.kinds:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    source=ROOT/f'Assets/TumbangPreso/Art/models/kits/city/building-type-{kind}.glb'
    bpy.ops.import_scene.gltf(filepath=str(source))
    original=bpy.context.selected_objects[0];original.name='Retained house '+kind
    original.scale*=5;bpy.context.view_layer.objects.active=original
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    retained=set(bpy.context.scene.objects)
    wood=material('Warm aged timber',(.34,.23,.14));steel=material('Dull painted steel',(.25,.33,.28))
    concrete=material('Warm concrete',(.52,.50,.43));roof=material('Galvanized shade roof',(.40,.44,.40))
    cream=material('Cream goods',(.72,.65,.47));red=material('Oxblood goods',(.48,.22,.23))
    if kind in ['e','o']:
        shadow=material('Timber course joints',(.24,.19,.13))
        plank_a=material('Timber upper warm',(.43,.32,.21));plank_b=material('Timber upper quiet',(.40,.29,.19))
        upper_cladding(original,shadow,[plank_a,plank_b])
    for low,high,normal in windows(original):
        center=(low+high)*.5;side=abs(normal.x)>.8;width=(high.y-low.y) if side else (high.x-low.x)
        count=max(3,round((high.z-low.z)/.16))
        for row in range(count):
            at=center+normal*.055;at.z=low.z+(row+.5)*(high.z-low.z)/count
            size=(.13,width-.035,.075) if side else (width-.035,.13,.075)
            o=box('Jalousie blade within retained deep frame',at,size,steel,.008)
            if side:o.rotation_euler.y=math.radians(-16)*normal.x
            else:o.rotation_euler.x=math.radians(16)*normal.y
    if kind=='o':
        # Measured retained terrace: X[-3.075,-1.075],Y[-2.28,1.72],Z2.
        # The upper room previously had no visible access to that terrace.
        trim=material('Substantial dark window trim',(.24,.26,.31))
        box('Closed terrace door',(-1.16,-.28,2.88),(.08,1.0,1.72),wood,.025)
        for y in [-.90,.34]:box('Terrace door jamb',(-1.18,y,2.94),(.20,.18,1.88),trim,.025)
        box('Terrace door head',(-1.18,-.28,3.85),(.20,1.42,.18),trim,.025)
        box('Terrace threshold',(-1.23,-.28,2.04),(.36,1.42,.08),trim,.015)
        box('Terrace door handle',(-1.235,.09,2.9),(.045,.09,.09),cream,.009)
        box('Upper front window backing',(.925,-2.36,3.10),(1.25,.10,.94),trim,.025)
        for x in [.19,1.66]:box('Upper front jamb',(x,-2.40,3.10),(.20,.19,1.30),trim,.025)
        for z in [2.51,3.69]:box('Upper front head or sill',(.925,-2.40,z),(1.67,.19,.17),trim,.025)
        for i in range(5):box('Upper front jalousie',(.925,-2.425,2.73+i*.185),(1.25,.11,.08),steel,.008)
        for y in [-1.7,1.10]:box('Terrace laundry post',(-2.70,y,2.67),(.075,.075,1.34),steel,.008)
        box('Terrace clothesline',(-2.70,-.30,3.34),(.018,2.80,.018),steel,.003)
        for i,y in enumerate([-.95,-.20,.55]):
            box('Hanging household towel',(-2.70,y,2.99),(.026,.45,.57),cream if i!=1 else red,.003)
            for peg in [-.16,.16]:box('Laundry peg',(-2.70,y+peg,3.30),(.045,.035,.12),wood,.004)
    # A solid threshold and paired substantial piers carry the entrance shade.
    # This projects into a private setback, which must be reserved during layout.
    door_x=0 if kind=='a' else -.03
    if kind in ['a','c']:
        box('Entry threshold',(door_x,-2.70,.075),(2.03,1.24,.15),concrete,.04)
        box('Lower entry step',(door_x,-3.36,.0375),(2.11,.38,.075),concrete,.02)
    # The low house already has a deep eave. An extra entrance roof would cover
    # its transom, so reserve this portico for the taller retained body only.
    if kind=='c':
        for x in [door_x-.84,door_x+.84]:
            box('Entry pier',(x,-3.10,1.15),(.24,.24,2.15),concrete,.025)
            box('Pier base',(x,-3.10,.25),(.33,.33,.25),concrete,.025)
        box('Entry beam',(door_x,-3.10,2.20),(2.08,.25,.25),wood,.02)
        canopy=box('Supported entrance shade',(door_x,-2.70,2.30),(2.28,1.23,.17),roof,.025)
        canopy.rotation_euler.x=math.radians(7)
        for i in range(8):
            rib=box('Roof sheet seam',(door_x-.99+i*.28,-2.70,2.395),(.025,1.19,.022),roof,.004)
            rib.rotation_euler.x=canopy.rotation_euler.x
    if kind=='a':
        # One small service window; the rest of the house stays residential.
        box('Service window ledge',(-2,-2.49,.67),(1.82,.57,.15),wood,.025)
        for x in [-2.65,-1.35]:box('Ledge wall bracket',(x,-2.37,.51),(.14,.27,.27),wood,.015)
        box('Sachet hanging rail',(-2,-2.43,1.55),(1.58,.09,.08),wood,.012)
        for col in range(6):
            for row in range(2):
                box('Hanging sachet',(-2.62+col*.25,-2.45,1.37-row*.24),(.18,.055,.215),red if col%3==0 else cream,.008)
    details=[o for o in bpy.context.scene.objects if o not in retained]
    bpy.ops.object.select_all(action='DESELECT')
    for o in details:o.select_set(True)
    bpy.context.view_layer.objects.active=details[0];bpy.ops.object.join();detail=bpy.context.object
    detail.name='retained-house-'+kind+'-details';bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    # Keep reference textures portable in the native study without changing sources.
    bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(out/(detail.name+'.blend')))
    bpy.ops.export_scene.gltf(filepath=str(out/(detail.name+'.glb')),export_format='GLB',use_selection=True,export_animations=False)
    scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.view_settings.view_transform='Standard'
    shade=scene.display.shading;shade.light='STUDIO';shade.color_type='TEXTURE';shade.show_shadows=True;shade.show_cavity=True;shade.cavity_type='BOTH'
    shade.background_type='WORLD';scene.world.color=(.15,.16,.17)
    camera=bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'));bpy.context.collection.objects.link(camera);scene.camera=camera
    camera.data.type='ORTHO';camera.data.ortho_scale=11
    scene.render.resolution_x=1100;scene.render.resolution_y=900;scene.render.resolution_percentage=100
    for tag,pos in [('front',(10,-14,9)),('back',(-10,14,9)),('straight',(0,-15,3))]:
        camera.location=pos;camera.rotation_euler=(Vector((0,0,2.4))-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(out/(kind+'-'+tag+'.png'));bpy.ops.render.render(write_still=True)
    print(kind,'new vertices',len(detail.data.vertices),'new faces',len(detail.data.polygons))
