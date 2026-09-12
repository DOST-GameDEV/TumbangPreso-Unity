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
parser=argparse.ArgumentParser();parser.add_argument('--out',default='Logs/retained-house-details-v1')
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

for kind in ['a','c']:
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
    for low,high,normal in windows(original):
        center=(low+high)*.5;side=abs(normal.x)>.8;width=(high.y-low.y) if side else (high.x-low.x)
        count=max(3,round((high.z-low.z)/.16))
        for row in range(count):
            at=center+normal*.055;at.z=low.z+(row+.5)*(high.z-low.z)/count
            size=(.13,width-.035,.075) if side else (width-.035,.13,.075)
            o=box('Jalousie blade within retained deep frame',at,size,steel,.008)
            if side:o.rotation_euler.y=math.radians(-16)*normal.x
            else:o.rotation_euler.x=math.radians(16)*normal.y
    # A solid threshold and paired substantial piers carry the entrance shade.
    # This projects into a private setback, which must be reserved during layout.
    door_x=0 if kind=='a' else -.03
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
