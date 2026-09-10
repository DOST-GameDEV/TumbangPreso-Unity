"""Hand-built clean block forms for Kuro, matched to TUMP's actual cast."""
from pathlib import Path
import bpy,bmesh,math
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Logs/kuro-clean-block-study-v2';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
root=bpy.data.objects.new('KuroRageRoot',None);bpy.context.collection.objects.link(root)
def mat(n,c):
 m=bpy.data.materials.new(n);m.diffuse_color=(*c,1);m.use_nodes=True
 shader=m.node_tree.nodes.get('Principled BSDF')
 shader.inputs['Base Color'].default_value=(*c,1)
 shader.inputs['Roughness'].default_value=.95
 shader.inputs['Metallic'].default_value=0
 return m
skin=mat('Kuro deep violet',(.08,.025,.14));side=mat('Kuro side planes',(.13,.045,.23));edge=mat('Lavender spectral edges',(.27,.105,.43));dark=mat('Hollow maw',(.012,.004,.022));tooth=mat('Block ivory',(.92,.85,.71));eyes=mat('Floating spirit eyes',(.80,.57,1.0))
def mesh(n,verts,faces,mats,group='RageChest'):
 d=bpy.data.meshes.new(n);d.from_pydata(verts,[],faces);d.update();bm=bmesh.new();bm.from_mesh(d);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(d);bm.free()
 o=bpy.data.objects.new(n,d);bpy.context.collection.objects.link(o);o.parent=root
 for m in mats:d.materials.append(m)
 o['rig_group']=group
 return o
def box(n,at,size,material=skin,rotation=(0,0,0),bevel=.07,group='RageChest',accent=False):
 bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.name=n;o.parent=root;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 o.data.materials.append(material);o.data.materials.append(edge if accent else side);mod=o.modifiers.new('Broad chamfer','BEVEL');mod.width=bevel;mod.segments=1;mod.material=1;bpy.ops.object.modifier_apply(modifier=mod.name)
 o.rotation_euler=[math.radians(v) for v in rotation];o['rig_group']=group
 return o
def rectangle(w,d):
 return [(-w*.72,-d),(w*.72,-d),(w,-d*.72),(w,d*.72),(w*.72,d),(-w*.72,d),(-w,d*.72),(-w,-d*.72)]
def sweep(n,points,widths,depths,material=skin,group='RageChest',accent=False):
 verts=[];last=None;axis=None
 for i,p in enumerate(points):
  p=Vector(p);tangent=(Vector(points[min(len(points)-1,i+1)])-Vector(points[max(0,i-1)])).normalized()
  if last is None:
   ref=Vector((0,1,0)) if abs(tangent.y)<.85 else Vector((0,0,1));axis=tangent.cross(ref).normalized()
  else:axis=last.rotation_difference(tangent)@axis
  other=tangent.cross(axis).normalized();last=tangent
  for x,y in rectangle(widths[i],depths[i]):verts.append(p+axis*x+other*y)
 faces=[tuple(range(7,-1,-1))]
 for r in range(len(points)-1):
  for i in range(8):faces.append((r*8+i,r*8+(i+1)%8,(r+1)*8+(i+1)%8,(r+1)*8+i))
 faces.append(tuple(range((len(points)-1)*8,len(points)*8)))
 o=mesh(n,verts,faces,[material,edge if accent else side],group)
 for p in o.data.polygons:
  if p.index>0 and (p.index-1)%8 in [1,3,5,7]:p.material_index=1
 return o
# Few coherent volumes: squared forward-leaning head, broad back, tapered spirit.
box('RageHood',(0,-.28,3.00),(1.20,1.10,.86),rotation=(24,0,0),bevel=.12,group='RageHead',accent=True)
box('RageBack',(0,.27,2.54),(1.20,.85,1.08),rotation=(12,0,0),bevel=.13)
sweep('RageLowerSpirit',[(0,.16,1.96),(0,.15,1.45),(-.12,.15,.96),(-.25,.12,.55),(-.14,.02,.22),(.20,-.06,.10),(.47,-.10,.22),(.56,-.11,.38)],[.49,.45,.35,.28,.20,.15,.065,.003],[.43,.40,.30,.22,.16,.11,.05,.003],accent=True,group='RageWispA')
# Trapezoid mouth rim and an actual dark recess; never a flat panel with teeth.
def mouth_loop(w,top,bottom,y,cut):
 return [(-w+cut,y,top),(w-cut,y,top),(w,y,top-cut),(w*.94,y,bottom+cut),(w*.94-cut,y,bottom),(-w*.94+cut,y,bottom),(-w*.94,y,bottom+cut),(-w,y,top-cut)]
outer=mouth_loop(.64,2.67,1.49,-.86,.10);inner=mouth_loop(.50,2.56,1.65,-.88,.075)
verts=outer+inner;faces=[]
for i in range(1,8):faces.append((i,(i+1)%8,(i+1)%8+8,i+8))
mesh('RageJawRim',verts,faces,[side],'RageJaw')
back=mouth_loop(.37,2.41,1.79,-.39,.06);verts=inner+back;faces=[]
for i in range(8):faces.append((i,(i+1)%8,(i+1)%8+8,i+8))
faces.append(tuple(range(8,16)));cavity=mesh('RageMawCavity',verts,faces,[dark],'RageJaw')
# Recalculated exterior normals face away from the viewer for this open bowl.
# Its visible surface is the INSIDE; Unity culls the wrong-facing back wall.
bm=bmesh.new();bm.from_mesh(cavity.data);bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.to_mesh(cavity.data);bm.free()
box('RageLowerJaw',(0,-.60,1.57),(1.21,.58,.22),material=side,rotation=(8,0,0),bevel=.055,group='RageJaw')
for i,(x,h) in enumerate([(-.32,.42),(.33,.38)]):
 sweep('RageUpperFang'+str(i),[(x,-.73,2.62),(x,-.87,2.40),(x*.88,-.90,2.62-h)],[.125,.10,.043],[.10,.085,.043],tooth,'RageHead')
for i,(x,h) in enumerate([(-.33,.33),(0,.15),(.34,.29)]):
 sweep('RageLowerFang'+str(i),[(x,-.76,1.64),(x,-.90,1.64+h*.55),(x*.87,-.91,1.64+h)],[.115 if x else .075,.09 if x else .06,.036],[.085,.07,.034],tooth,'RageJaw')
# Shoulders and short wisp connections keep the hands weighty without anatomy.
box('RageShoulderLeft',(-.75,.08,2.61),(.69,.71,.65),rotation=(10,0,20),bevel=.09,group='RageArmLeft')
box('RageShoulderRight',(.75,.08,2.51),(.68,.70,.62),rotation=(10,0,-19),bevel=.09,group='RageArmRight')
sweep('RageWristWispLeft',[(-.98,-.04,2.45),(-1.13,-.18,2.26),(-1.30,-.46,2.13)],[.145,.11,.065],[.13,.10,.06],edge,'RageForearmLeft')
sweep('RageWristWispRight',[(.98,-.02,2.36),(1.10,-.20,2.03),(1.30,-.42,1.82)],[.145,.10,.06],[.13,.10,.055],edge,'RageForearmRight')
fingers=[[(.02,-.05,.18),(.00,-.22,.30),(.12,-.42,.27),(.24,-.54,.055)], [(-.18,-.05,.035),(-.45,-.12,.12),(-.48,-.36,-.07),(-.23,-.53,-.18)], [(.18,-.05,-.065),(.42,-.13,-.27),(.30,-.35,-.43),(.02,-.53,-.33)]]
for side_name,at,rz in [('Left',(-1.49,-.65,2.12),12),('Right',(1.48,-.60,1.78),-15)]:
 hand=bpy.data.objects.new('RageHand'+side_name+'Parts',None);bpy.context.collection.objects.link(hand);hand.parent=root;hand.location=at;hand.rotation_euler=(math.radians(8),0,math.radians(rz))
 palm=box('RagePalm'+side_name,(0,0,0),(.52,.27,.45),bevel=.085,group='RageHand'+side_name);palm.parent=hand
 for i,points in enumerate(fingers):
  o=sweep('RageFinger'+side_name+str(i),points,[.095,.10,.088,.043],[.085,.095,.082,.04],edge,'RageHand'+side_name)
  o.parent=hand
# Floating beveled eye shards. The aura will be a separate soft runtime effect.
for sign,label in [(-1,'Left'),(1,'Right')]:
 profile=[(-.15,.13),(.08,.055),(.145,-.10),(-.055,-.12)]
 if sign==1:profile=[(-x,z) for x,z in reversed(profile)]
 at=Vector((sign*.34,-.98,2.80));verts=[at+Vector((x,y,z)) for y in [-.025,.025] for x,z in profile]
 o=mesh('RageEye'+label,verts,[(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],[eyes],'RageHead')
sweep('RageHoodWisp',[(-.36,.33,3.23),(-.56,.31,3.47),(-.80,.29,3.53),(-.97,.23,3.37)],[.13,.10,.068,.005],[.07,.055,.04,.005],edge,'RageHead')
# Save a clean native source and ordinary eye-height views for critique.
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.view_settings.view_transform='Standard';scene.world.color=(.08,.085,.11)
scene.render.resolution_x=1100;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
sh=scene.display.shading;sh.light='STUDIO';sh.color_type='MATERIAL';sh.show_shadows=True;sh.show_specular_highlight=False;sh.background_type='WORLD';sh.show_object_outline=False
camdata=bpy.data.cameras.new('CleanBlockReview');cam=bpy.data.objects.new('CleanBlockReview',camdata);bpy.context.collection.objects.link(cam);scene.camera=cam;camdata.type='ORTHO';camdata.ortho_scale=4.75
for name,pos in [('front',(0,-7,1.6)),('three-quarter',(3.0,-7,1.6)),('side',(7,-1,1.6))]:
 cam.location=pos;cam.rotation_euler=(Vector((0,-.1,1.8))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'kuro-clean-block.blend'))
print('Clean forms',len([o for o in scene.objects if o.type=='MESH']))
