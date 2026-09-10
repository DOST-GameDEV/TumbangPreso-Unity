"""Native Blender comparison of installed source trees; writes only Logs."""
from pathlib import Path
import bpy,json
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Logs/map-tree-inventory';OUT.mkdir(parents=True,exist_ok=True)
paths=['city/tree-large','city/tree-small','forest/tree','forest/tree-high',
       'town/tree','town/tree-high','town/tree-high-round','town/tree-crooked']
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
records=[]
for i,path in enumerate(paths):
 before=set(bpy.context.scene.objects)
 bpy.ops.import_scene.gltf(filepath=str(ROOT/('Assets/TumbangPreso/Art/models/kits/'+path+'.glb')))
 objects=[o for o in bpy.context.scene.objects if o not in before]
 bpy.context.view_layer.update()
 vertices=[o.matrix_world@v.co for o in objects if o.type=='MESH' for v in o.data.vertices]
 lo=Vector(tuple(min(p[j] for p in vertices) for j in range(3)))
 hi=Vector(tuple(max(p[j] for p in vertices) for j in range(3)))
 holder=bpy.data.objects.new(path,None);bpy.context.collection.objects.link(holder)
 for obj in objects:
  if obj.parent not in objects:
   matrix=obj.matrix_world.copy();obj.parent=holder;obj.matrix_world=matrix
 scale=3.2/(hi.z-lo.z);holder.scale=(scale,)*3
 x=(i%3-1)*4.4;z=(2-i//3)*4.6
 holder.location=(x-(lo.x+hi.x)*.5*scale,-(lo.y+hi.y)*.5*scale,z-lo.z*scale)
 label=bpy.data.curves.new(path,'FONT');label.body=path;label.align_x='CENTER';label.size=.24
 text=bpy.data.objects.new('Label '+path,label);bpy.context.collection.objects.link(text)
 text.location=(x,-1,z-.35);text.rotation_euler=(1.570796,0,0)
 records.append(dict(path=path,bounds=list(hi-lo),vertices=len(vertices)))
for material in bpy.data.materials:
 if material.use_nodes:
  shader=material.node_tree.nodes.get('Principled BSDF')
  if shader:material.diffuse_color=shader.inputs['Base Color'].default_value
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.view_settings.view_transform='Standard'
scene.world.color=(.10,.11,.13);s=scene.display.shading;s.light='STUDIO';s.color_type='MATERIAL'
s.show_shadows=True;s.show_specular_highlight=False;s.show_object_outline=False;s.background_type='WORLD'
data=bpy.data.cameras.new('Inventory camera');camera=bpy.data.objects.new('Inventory camera',data);bpy.context.collection.objects.link(camera)
camera.location=(0,-35,5.9);camera.rotation_euler=(Vector((0,0,5.9))-camera.location).to_track_quat('-Z','Y').to_euler()
data.type='ORTHO';data.ortho_scale=14.3;scene.camera=camera
scene.render.resolution_x=1300;scene.render.resolution_y=1300;scene.render.resolution_percentage=100
scene.render.filepath=str(OUT/'installed-trees.png');bpy.ops.render.render(write_still=True)
(OUT/'inventory.json').write_text(json.dumps(records,indent=2))
