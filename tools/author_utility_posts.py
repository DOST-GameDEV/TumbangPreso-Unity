"""Separate retained pole hardware from its baked-in longitudinal wire spans.

The original OBJ/MTL stays untouched. Connected wire components longer than3m
are omitted from this derived hardware model; actual scene spans are authored
between placed posts. Work in Logs until Unity review is complete.
"""
import argparse
import json
import shutil
import sys
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--out',default='Logs/utility-posts-v1')
a=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
out=ROOT/a.out;out.mkdir(parents=True,exist_ok=True)
source=ROOT/'Assets/TumbangPreso/Art/models/env_post_electric.obj'
lines=source.read_text().splitlines();vertices=[];wire_faces=[];material=''
for number,line in enumerate(lines):
    parts=line.split()
    if not parts:continue
    if parts[0]=='v':vertices.append(tuple(map(float,parts[1:4])))
    elif parts[0]=='usemtl':material=parts[1]
    elif parts[0]=='f' and material=='wire':wire_faces.append((number,[int(v.split('/')[0])-1 for v in parts[1:]]))
keys={};canonical=[]
for point in vertices:
    key=tuple(round(v,5) for v in point)
    if key not in keys:keys[key]=len(keys)
    canonical.append(keys[key])
links={}
for _,face in wire_faces:
    ids=[canonical[i] for i in face]
    for i in ids:links.setdefault(i,set()).update(ids)
seen=set();long_vertices=set();removed_groups=0
for i in links:
    if i in seen:continue
    todo=[i];group=set()
    while todo:
        n=todo.pop()
        if n in seen:continue
        seen.add(n);group.add(n);todo.extend(links[n]-seen)
    points=[point for point,index in zip(vertices,canonical) if index in group]
    if max(v[0] for v in points)-min(v[0] for v in points)>3:
        long_vertices.update(group);removed_groups+=1
omit={line for line,face in wire_faces if any(canonical[i] in long_vertices for i in face)}
if removed_groups!=8:raise ValueError('Unexpected source wire topology: '+str(removed_groups))
filtered=out/'retained-pole-hardware.obj'
filtered.write_text('\n'.join(line for i,line in enumerate(lines) if i not in omit)+'\n')
shutil.copy2(source.with_suffix('.mtl'),out/source.with_suffix('.mtl').name)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.wm.obj_import(filepath=str(filtered),forward_axis='NEGATIVE_Z',up_axis='Y')
objects=[o for o in bpy.context.selected_objects if o.type=='MESH']
for o in objects:
    # Unused source vertices otherwise keep the old span in the imported bounds.
    import bmesh
    bm=bmesh.new();bm.from_mesh(o.data)
    unused=[v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm,geom=unused,context='VERTS');bm.to_mesh(o.data);bm.free()
    for m in o.data.materials:
        if m.name=='wire':
            m.diffuse_color=(.05,.055,.05,1)
            if m.use_nodes:
                bsdf=m.node_tree.nodes.get('Principled BSDF')
                if bsdf:bsdf.inputs['Base Color'].default_value=m.diffuse_color
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join()
model=bpy.context.object;model.name='retained-utility-pole';bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
for name,x,height in [('high0',-.6,6.38),('high1',0,6.38),('high2',.6,6.38),('mid0',-.48,5.74),('mid1',.24,5.74)]:
    anchor=bpy.data.objects.new('WireAnchor_'+name,None);bpy.context.collection.objects.link(anchor)
    anchor.parent=model;anchor.location=(x,0,height);anchor.select_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'retained-utility-pole.blend'))
bpy.ops.export_scene.gltf(filepath=str(out/'retained-utility-pole.glb'),export_format='GLB',use_selection=True,export_animations=False)
(out/'source-record.json').write_text(json.dumps({'source':str(source.relative_to(ROOT)),'removedSpanComponents':removed_groups,'removedFaces':len(omit),'retained':'pole,timber crossarms,insulators,drum,coils and source materials with neutral wire hardware'},indent=2)+'\n')
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.view_settings.view_transform='Standard'
shade=scene.display.shading;shade.color_type='MATERIAL';shade.light='STUDIO';shade.show_shadows=True
camera=bpy.data.objects.new('Hardware review',bpy.data.cameras.new('Hardware review'));bpy.context.collection.objects.link(camera);scene.camera=camera
camera.data.type='ORTHO';camera.data.ortho_scale=8.5;camera.location=(8,-12,7)
camera.rotation_euler=(Vector((0,0,3.5))-camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=850;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.filepath=str(out/'retained-utility-pole.png');bpy.ops.render.render(write_still=True)
print('Removed',removed_groups,'embedded spans;',len(model.data.vertices),'retained vertices')
