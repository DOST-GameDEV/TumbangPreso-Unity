"""Original small rooftop birds for TUMP. Native rig and mesh, no sourced textures.
Models use six simple bones; runtime visits and poses use a private cosmetic stream.
"""
import argparse,math,sys,json
from pathlib import Path
import bpy,bmesh
from mathutils import Vector,Quaternion
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--out',default='Logs/roof-birds-v1')
a=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
out=ROOT/a.out;out.mkdir(parents=True,exist_ok=True)

def mat(name,c):
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);return m

def weighted(obj,bone,material):
 obj.data.materials.append(material);g=obj.vertex_groups.new(name=bone);g.add(list(range(len(obj.data.vertices))),1,'REPLACE');return obj

def box(name,at,size,bone,material,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.name=name;o.scale=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Broad corners','BEVEL');mod.width=bevel;mod.segments=1
  bpy.ops.object.modifier_apply(modifier=mod.name)
 return weighted(o,bone,material)

def prism(name,points,bone,material,thickness=.016):
 verts=[(x,y,z+dz) for dz in [-thickness*.5,thickness*.5] for x,y,z in points];n=len(points)
 faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();bm=bmesh.new();bm.from_mesh(mesh)
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
 o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);return weighted(o,bone,material)

for species,scale in [('maya',.68),('kalapati',1),('fantail',.78)]:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 brown=(.34,.23,.14);grey=(.43,.46,.44);black=(.12,.16,.14);cream=(.76,.70,.56)
 base=mat('Body',brown if species=='maya' else grey if species=='kalapati' else black)
 dark=mat('Wing',(.20,.15,.10) if species=='maya' else (.25,.29,.29) if species=='kalapati' else (.10,.13,.12))
 light=mat('Breast',(.67,.55,.38) if species=='maya' else (.62,.64,.61) if species=='kalapati' else (.79,.77,.66))
 eye=mat('Graphic eye',(.035,.036,.026));feet=mat('Feet',(.38,.28,.20))
 beak=mat('Small bill',(.19,.15,.10) if species=='maya' else (.22,.24,.21))
 parts=[]
 parts.append(box('Body',(0,.02,.16),(.18,.25,.18),'Body',base,.035))
 parts.append(box('Breast',(0,-.075,.175),(.145,.067,.125),'Body',light,.019))
 parts.append(box('Head',(0,-.09,.28),(.13,.13,.125),'Head',base,.020))
 if species=='maya':
  for side in [-1,1]:
   parts.append(box('Pale cheek',(.064*side,-.085,.267),(.007,.067,.045),'Head',light,.004))
   parts.append(box('Cheek spot',(.069*side,-.074,.268),(.008,.015,.017),'Head',dark,.003))
 if species=='kalapati':parts.append(box('Quiet green neck',(0,-.065,.225),(.11,.075,.075),'Head',mat('Neck',(.27,.36,.29)),.012))
 for side in [-1,1]:
  parts.append(box('Graphic eye',(.069*side,-.112,.294),(.007,.019,.019),'Head',eye,.002))
  parts.append(box('Leg',(.047*side,.025,.040),(.012,.016,.064),'Root',feet))
  parts.append(prism('Small forked foot',[(.047*side-.012,.024,.008),(.047*side-.013,-.030,.008),
   (.047*side,-.016,.008),(.047*side+.013,-.030,.008),(.047*side+.012,.024,.008)],'Root',feet,.012))
  pts=[(.092*side,-.057,.19),(.245*side,-.08,.185),(.35*side,.02,.17),(.21*side,.15,.17),(.10*side,.12,.19)]
  parts.append(prism('Wing_'+str(side),pts,'WingL' if side<0 else 'WingR',dark,.024))
  if species=='kalapati':
   parts.append(prism('Wing band_'+str(side),[(.20*side,-.049,.204),(.22*side,-.038,.204),(.22*side,.116,.194),(.20*side,.132,.194)],'WingL' if side<0 else 'WingR',eye,.004))
 bill_length=.043 if species=='kalapati' else .035
 parts.append(prism('Small tapered bill',[(-.016,-.151,.276),(.016,-.151,.276),(0,-.151-bill_length,.274)],'Head',beak,.020))
 if species=='fantail':
  for i in range(5):
   x=(i-2)*.04;parts.append(prism('Fan feather_'+str(i),[(x*.25,.12,.17),(x-.021,.33,.225),(x+.021,.33,.225)],'Tail',dark,.012))
   parts.append(prism('White tail tip_'+str(i),[(x-.021,.31,.22),(x-.021,.34,.227),(x+.021,.34,.227),(x+.021,.31,.22)],'Tail',light,.013))
 else:parts.append(prism('Tail',[(-.045,.12,.15),(-.052,.30,.18),(.052,.30,.18),(.045,.12,.15)],'Tail',dark,.026))
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();bird=bpy.context.object;bird.name=species
 # Apply coordinates to the world origin before attaching to the rig.
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 rigdata=bpy.data.armatures.new('BirdRig');rig=bpy.data.objects.new('BirdRig',rigdata);bpy.context.collection.objects.link(rig)
 bpy.context.view_layer.objects.active=rig;bird.select_set(False);rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 definitions={'Root':((0,0,0),(0,0,.1),None),'Body':((0,0,.1),(0,0,.22),'Root'),'Head':((0,-.065,.22),(0,-.065,.33),'Body'),'WingL':((-.09,0,.19),(-.30,0,.19),'Body'),'WingR':((.09,0,.19),(.30,0,.19),'Body'),'Tail':((0,.11,.15),(0,.28,.19),'Body')}
 for name,(head,tail,parent) in definitions.items():
  bone=rigdata.edit_bones.new(name);bone.head=head;bone.tail=tail
  if parent:bone.parent=rigdata.edit_bones[parent]
 bpy.ops.object.mode_set(mode='OBJECT');bird.parent=rig;mod=bird.modifiers.new('Bird motion','ARMATURE');mod.object=rig
 marker=bpy.data.objects.new('LookForward',None);bpy.context.collection.objects.link(marker);marker.parent=rig;marker.location=(0,-1,0)
 rig.scale=(scale,scale,scale)
 bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=rig
 bpy.ops.wm.save_as_mainfile(filepath=str(out/(species+'.blend')))
 bpy.ops.export_scene.gltf(filepath=str(out/(species+'.glb')),export_format='GLB',use_selection=True,export_animations=False)
 scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.view_settings.view_transform='Standard'
 scene.world.color=(.13,.14,.15);shade=scene.display.shading;shade.light='STUDIO';shade.color_type='MATERIAL';shade.show_shadows=True;shade.show_specular_highlight=False;shade.background_type='WORLD'
 camdata=bpy.data.cameras.new('Review');cam=bpy.data.objects.new('Review',camdata);bpy.context.collection.objects.link(cam);scene.camera=cam
 cam.location=(.8,-1.2,.65);cam.rotation_euler=(Vector((0,.02,.17*scale))-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=.75
 scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.resolution_percentage=100
 for state in ['perched','flight']:
  for name,side in [('WingL',-1),('WingR',1)]:
   bone=rig.pose.bones[name];q=bone.bone.matrix_local.to_quaternion();rotation=Quaternion((0,0,1),math.radians(side*70)) if state=='perched' else Quaternion((0,-1,0),math.radians(side*32))
   bone.rotation_mode='QUATERNION';bone.rotation_quaternion=q.inverted()@rotation@q
  scene.render.filepath=str(out/(species+'-'+state+'.png'));bpy.ops.render.render(write_still=True)
 (out/(species+'.json')).write_text(json.dumps({'species':species,'scale':scale,'bones':list(definitions),'vertices':len(bird.data.vertices),'author':'tools/author_roof_birds.py','reference':'https://birdwatch.ph/2013/07/03/10-most-common-urban-birds/'},indent=2))
 print(species,len(bird.data.vertices),'vertices,',len(bird.data.polygons),'faces')
