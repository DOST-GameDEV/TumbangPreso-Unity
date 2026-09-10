"""Rig the chunky Kuro giant and author its slow reaching/inhalation cycle."""
from pathlib import Path
import bpy,math
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'MapSource/characters/kuro/rage';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Logs/kuro-clean-block-study-v2/kuro-clean-block.blend'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
bpy.context.view_layer.update()
for obj in meshes:
 points=[obj.matrix_world@v.co for v in obj.data.vertices]
 obj.parent=None;obj.matrix_world=Matrix.Identity(4)
 for v,p in zip(obj.data.vertices,points):v.co=p
for o in list(bpy.context.scene.objects):
 if o.type!='MESH':bpy.data.objects.remove(o,do_unlink=True)
body=bpy.data.objects['RageLowerSpirit']
data=bpy.data.armatures.new('Kuro rage skeleton');rig=bpy.data.objects.new('RageRig',data);bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
 b=data.edit_bones.new(name);b.head=head;b.tail=tail
 if parent:b.parent=data.edit_bones[parent]
 return b
bone('RageChest',(0,.10,1.15),(0,.10,2.35))
bone('RageHead',(0,-.25,2.70),(0,-.50,3.25),'RageChest')
bone('RageJaw',(0,-.45,2.13),(0,-.70,1.65),'RageHead')
bone('RageWispA',(0,.10,1.15),(0,.16,.65),'RageChest');bone('RageWispB',(0,.16,.65),(.18,.12,.30),'RageWispA');bone('RageWispC',(.18,.12,.30),(.52,.08,.06),'RageWispB')
bone('RageArmLeft',(-.55,.08,2.55),(-1.06,-.12,2.35),'RageChest');bone('RageForearmLeft',(-1.06,-.12,2.35),(-1.40,-.65,2.12),'RageArmLeft');bone('RageHandLeft',(-1.40,-.65,2.12),(-1.47,-.95,2.08),'RageForearmLeft')
bone('RageArmRight',(.72,.12,2.52),(1.12,-.10,2.20),'RageChest');bone('RageForearmRight',(1.12,-.10,2.20),(1.40,-.58,1.82),'RageArmRight');bone('RageHandRight',(1.40,-.58,1.82),(1.46,-.89,1.78),'RageForearmRight')
for obj in meshes:
 if obj.name.startswith('RageFinger'):
  points=[v.co.copy() for v in obj.data.vertices]
  first=sum(points[:8],Vector())/8;last=sum(points[-8:],Vector())/8
  hand='RageHandLeft' if 'Left' in obj.name else 'RageHandRight'
  bone(obj.name+'Joint',first,last,hand)
bpy.ops.object.mode_set(mode='OBJECT')
def smooth(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def distance_segment(p,a,b):
 ab=b-a;t=max(0,min(1,(p-a).dot(ab)/ab.length_squared));return (p-(a+ab*t)).length
def spread(p,names,total):
 weights=[1/(distance_segment(p,data.bones[n].head_local,data.bones[n].tail_local)+.075)**3 for n in names];s=sum(weights)
 return {n:total*w/s for n,w in zip(names,weights)}
for o in [o for o in bpy.context.scene.objects if o.type=='MESH']:
 groups={n:o.vertex_groups.new(name=n) for n in data.bones.keys()}
 for v in o.data.vertices:
  if o.name.startswith('RageFinger'):weights={o.name+'Joint':1}
  elif o!=body:weights={o.get('rig_group','RageChest'):1}
  else:
   p=v.co;lower=1-smooth(.70,1.35,p.z)
   weights={'RageChest':1-lower}
   weights.update(spread(p,['RageWispA','RageWispB','RageWispC'],lower))
  weights={n:w for n,w in weights.items() if w>.0001};total=sum(weights.values())
  for n,w in weights.items():groups[n].add([v.index],w/total,'REPLACE')
 o.parent=rig;mod=o.modifiers.new('Authored giant rig','ARMATURE');mod.object=rig
# A real mouth anchor, independent of the graphic eyes.
anchor=bpy.data.objects.new('RageMaw',None);bpy.context.collection.objects.link(anchor);anchor.parent=rig;anchor.location=(0,-.82,2.10)
rig.location.y=.82
scene=bpy.context.scene;scene.render.fps=30;scene.frame_start=0;scene.frame_end=54
for b in rig.pose.bones:b.rotation_mode='XYZ'
for frame in range(55):
 phase=frame/54*math.tau;inhale=(1-math.cos(phase))*.5
 poses={
 'RageChest':(.035*inhale,.015*math.sin(phase),-.025*math.sin(phase)),
 'RageHead':(.095*inhale,-.025*math.sin(phase),.02*math.sin(phase)),
 'RageJaw':(-.14*inhale,0,0),
 'RageArmLeft':(-.08*inhale,.09*inhale,.10*inhale),
 'RageForearmLeft':(.12*inhale,0,.09*inhale),
 'RageHandLeft':(.08*inhale,.12*inhale,0),
 'RageArmRight':(.06*inhale,-.075*inhale,-.085*inhale),
 'RageForearmRight':(-.10*inhale,0,-.075*inhale),
 'RageHandRight':(-.06*inhale,-.10*inhale,0),
 'RageWispA':(.045*math.sin(phase-.3),0,.04*math.sin(phase)),
 'RageWispB':(.055*math.sin(phase-.7),0,.06*math.sin(phase-.5)),
 'RageWispC':(.07*math.sin(phase-1.0),0,.075*math.sin(phase-.8))}
 for name in data.bones.keys():
  if name.startswith('RageFinger'):
   poses[name]=(.13*inhale,0,(.035 if 'Left' in name else -.035)*inhale)
 for name,angles in poses.items():
  b=rig.pose.bones[name];b.rotation_euler=angles;b.keyframe_insert(data_path='rotation_euler',frame=frame,group=name)
rig.animation_data.action.name='KuroRageInhale'
# Measure the whole authored loop, then put its lowest visible point just
# above the floor. Render bounds can be conservative; evaluated vertices are
# the geometry that matters for contact.
minimum=1e9
for frame in range(55):
 scene.frame_set(frame);depsgraph=bpy.context.evaluated_depsgraph_get()
 for obj in meshes:
  evaluated=obj.evaluated_get(depsgraph);evaluated_mesh=evaluated.to_mesh()
  minimum=min(minimum,min((evaluated.matrix_world@v.co).z for v in evaluated_mesh.vertices))
  evaluated.to_mesh_clear()
rig.location.z+=.025-minimum
scene.frame_set(0)
print('Authored floor clearance adjustment',.025-minimum)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'kuro-rage.blend'))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=str(OUT/'kuro-rage.glb'),export_format='GLB',use_selection=True,export_animations=True,export_frame_range=True,export_force_sampling=True)
print('Rage rig',len(data.bones),'bones, 1.8s authored inhale cycle',OUT)
