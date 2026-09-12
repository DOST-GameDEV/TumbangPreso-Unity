"""Original chunky Philippine street-stall studies from the owner's references.

No photographic pixels, logos or watermarks are embedded. Z up, -Y serving front.
Use --publish only outside a Unity run; otherwise all output stays in Logs.
"""
import argparse
import math
import shutil
import sys
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--out',default='Logs/street-stalls-v1');p.add_argument('--publish',action='store_true')
args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
out=ROOT/args.out;out.mkdir(parents=True,exist_ok=True)

def mat(name,color,rough=.8,alpha=1):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,alpha);m.use_nodes=True
    n=m.node_tree.nodes.get('Principled BSDF');n.inputs['Base Color'].default_value=(*color,alpha)
    n.inputs['Roughness'].default_value=rough;n.inputs['Alpha'].default_value=alpha
    return m

def box(name,at,size,material,bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.name=name;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material)
    if bevel:
        b=o.modifiers.new('Broad edge','BEVEL');b.width=bevel;b.segments=1;bpy.ops.object.modifier_apply(modifier=b.name)
    return o

def cylinder(name,at,radius,depth,material,axis='Z',vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=at)
    o=bpy.context.object;o.name=name;o.data.materials.append(material)
    if axis=='X':o.rotation_euler.y=math.pi/2
    if axis=='Y':o.rotation_euler.x=math.pi/2
    return o

def beam(name,a,b,width,material):
    a,b=Vector(a),Vector(b);o=box(name,(a+b)/2,(width,width,(b-a).length),material)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o

def mesh(name,vertices,faces,material,two_sided=False):
    if two_sided:
        count=len(vertices)
        vertices=vertices+list(vertices)
        faces=faces+[tuple(i+count for i in reversed(f)) for f in faces]
    m=bpy.data.meshes.new(name);m.from_pydata(vertices,[],faces);m.materials.append(material)
    o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);return o

def umbrella(at,colors,metal):
    x,y,z=at
    cylinder('Umbrella pole',(x,y,z/2),.042,z,metal,vertices=8)
    cylinder('Weighted umbrella foot',(x,y,.11),.20,.22,metal,vertices=8)
    for i in range(8):
        a=i*math.tau/8;b=(i+1)*math.tau/8;c=(a+b)/2
        v=[(x,y,z+.27),(x+.66*math.cos(a),y+.66*math.sin(a),z+.12),
           (x+1.13*math.cos(a),y+1.13*math.sin(a),z-.10),
           (x+1.16*math.cos(c),y+1.16*math.sin(c),z-.17),
           (x+1.13*math.cos(b),y+1.13*math.sin(b),z-.10),
           (x+.66*math.cos(b),y+.66*math.sin(b),z+.12)]
        mesh('Umbrella cloth panel',v,[(0,1,2,3,4,5)],colors[i%len(colors)],True)
        beam('Umbrella rib',(x,y,z+.22),v[2],.02,metal)

def trolley(paint,metal,rubber):
    box('Sturdy cart body',(0,0,.50),(1.42,.79,.66),paint,.045)
    box('Heavy worktop',(0,-.015,.90),(1.57,.93,.13),metal,.03)
    box('Lower cart rail',(0,0,.22),(1.51,.83,.12),metal,.025)
    for x in [-.57,.57]:
        for y in [-.29,.29]:cylinder('Cart wheel',(x,y,.14),.14,.11,rubber,'X',10)
    for x in [-.62,.62]:beam('Push handle support',(x,.34,.58),(x,.55,.88),.065,metal)
    beam('Push handle',(-.62,.55,.88),(.62,.55,.88),.065,metal)

def bowl(name,at,radius,material):
    x,y,z=at;v=[]
    for r,h in [(radius*.38,0),(radius,.12),(radius*.92,.13),(radius*.36,.04)]:
        for i in range(12):v.append((x+r*math.cos(i*math.tau/12),y+r*math.sin(i*math.tau/12),z+h))
    faces=[]
    for row in range(3):
        for i in range(12):faces.append((row*12+i,row*12+(i+1)%12,(row+1)*12+(i+1)%12,(row+1)*12+i))
    faces.append(tuple(range(36,48)))
    return mesh(name,v,faces,material)

def snack(name,at,radius,material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8,ring_count=4,radius=radius,location=at)
    o=bpy.context.object;o.name=name;o.data.materials.append(material);return o

for kind in ['frying-cart','fruit-cart','pares-cart','clothes-accessories']:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    metal=mat('Dull stainless steel',(.50,.53,.49),.43);dark=mat('Dark cookware',(.055,.063,.054),.58)
    rubber=mat('Rubber',(.065,.065,.06));wood=mat('Worn timber',(.36,.25,.15))
    paint=mat('Muted oxblood cart',(.40,.15,.16));cream=mat('Warm cloth',(.70,.65,.51))
    green=mat('Green shade',(.23,.41,.31));plum=mat('Plum shade',(.34,.24,.34))
    golden=mat('Fried golden snacks',(.67,.45,.12));pale=mat('Fishballs and bread',(.72,.63,.41))
    sauce=mat('Dark sauce',(.15,.075,.028));bottle=mat('Sauce bottle',(.66,.62,.47))
    red=mat('Watermelon flesh',(.64,.095,.15));rind=mat('Watermelon rind',(.12,.29,.13))
    glass=mat('Display glass',(.56,.73,.65),.3,.12)
    if kind!='clothes-accessories':
        trolley(paint if kind!='fruit-cart' else wood,metal,rubber)
        umbrella((.58,.29,2.30),[green,cream,plum],metal)
    if kind=='frying-cart':
        bowl('Frying wok',(-.16,-.10,.96),.31,dark)
        for i in range(10):
            a=i*2.4;r=.075+(i%3)*.055
            snack('Fishball',(-.16+math.cos(a)*r,-.10+math.sin(a)*r,1.085),.042,pale)
        for i in range(4):snack('Kwek-kwek',(-.24+i*.08,-.22,1.12),.058,golden)
        box('Draining tray',(.42,-.08,.98),(.40,.46,.065),metal,.025)
        for i in range(4):
            o=cylinder('Kikiam',(.30+i*.08,-.12,1.047),.037,.22,golden,'Y',8)
        for x in [-.57,-.40]:
            cylinder('Sauce container',(x,.23,1.10),.072,.27,bottle)
            cylinder('Sauce in container',(x,.23,1.025),.074,.095,sauce)
        beam('Skewer cup',(.59,.22,.96),(.59,.22,1.12),.11,wood)
        for i in range(4):beam('Skewer',(.56+i*.022,.22,1.05),(.54+i*.025,.22,1.30),.012,cream)
        beam('Tongs',(-.34,-.05,1.16),(-.49,.22,1.21),.025,metal)
        beam('Tongs',(-.30,-.05,1.16),(-.49,.22,1.21),.025,metal)
    elif kind=='fruit-cart':
        box('Display case base',(-.16,0,1.0),(1.08,.70,.11),metal,.025)
        for x in [-.69,.37]:
            for y in [-.34,.34]:box('Display frame',(x,y,1.25),(.055,.055,.51),metal)
        box('Display case roof',(-.16,0,1.53),(1.12,.74,.075),metal,.02)
        box('Display front glass',(-.16,-.34,1.26),(1.00,.015,.45),glass)
        box('Display back glass',(-.16,.34,1.26),(1.00,.015,.45),glass)
        for x in [-.52,-.17,.17]:
            # Original simple wedges, with peel/pith/flesh layers.
            for radius,y,material in [(.16,-.035,rind),(.143,-.044,cream),(.13,-.053,red)]:
                vertices=[(x,y,1.06)]+[(x+radius*math.cos(a),y,1.06+radius*math.sin(a)) for a in [i*math.pi/8 for i in range(9)]]
                wedge=mesh('Watermelon wedge',vertices,[tuple(range(10))],material)
                bpy.context.view_layer.objects.active=wedge;wedge.select_set(True)
                thick=wedge.modifiers.new('Fruit slice thickness','SOLIDIFY');thick.thickness=.07 if material==rind else .045
                bpy.ops.object.modifier_apply(modifier=thick.name);wedge.select_set(False)
        for i in range(3):
            box('Cut fruit portion',(-.44+i*.25,.19,1.10),(.18,.14,.10),golden,.025)
        box('Preparation board',(.58,-.05,.995),(.23,.57,.045),wood)
        beam('Fruit knife',(.57,-.16,1.025),(.57,.15,1.025),.025,metal)
    elif kind=='pares-cart':
        for x,r in [(-.31,.23),(.23,.20)]:
            cylinder('Cooking pot',(x,.02,1.13),r,.32,metal)
            cylinder('Pot lid',(x,.02,1.31),r+.01,.04,metal)
            cylinder('Lid knob',(x,.02,1.36),.055,.06,dark,vertices=8)
        box('Bowls tray',(.55,-.08,.99),(.27,.48,.05),metal)
        for y in [-.20,0,.2]:bowl('Serving bowl',(.55,y,1.02),.10,cream)
        cylinder('Water jug',(-.60,.26,1.10),.075,.28,bottle)
        beam('Ladle handle',(-.17,-.19,1.35),(-.46,-.26,1.15),.025,metal)
        bowl('Ladle bowl',(-.46,-.26,1.10),.065,metal)
    else:
        for x in [-1.05,1.05]:
            box('Weighted stall foot',(x,0,.12),(.32,.74,.24),wood,.025)
            box('Stall post',(x,.25,1.22),(.095,.095,2.22),metal)
        beam('Clothing rail',(-1.05,.05,1.87),(1.05,.05,1.87),.055,metal)
        for i,material in enumerate([cream,green,paint,plum]):
            x=-.76+i*.48
            beam('Hanger', (x-.16,.04,1.76),(x,.04,1.84),.015,metal)
            beam('Hanger', (x,.04,1.84),(x+.16,.04,1.76),.015,metal)
            beam('Hanger hook',(x,.04,1.84),(x,.04,1.89),.015,metal)
            outline=[(-.17,0),(-.28,-.14),(-.20,-.23),(-.14,-.16),(-.14,-.56),(.14,-.56),(.14,-.16),(.20,-.23),(.28,-.14),(.17,0),(.065,-.05),(-.065,-.05)]
            vertices=[(x+px,-.005,1.75+pz) for px,pz in outline]
            mesh('Hanging shirt',vertices,[tuple(range(len(vertices)))],material,True)
            for z in [1.37,1.46]:box('Wide shirt stripe',(x,-.016,z),(.27,.017,.035),wood if material==cream else cream)
        box('Stock chest',(0,.19,.36),(1.85,.55,.53),wood,.045)
        for x in [-.60,-.2,.2,.6]:box('Folded garment',(x,.15,.67),(.34,.37,.08),cream if x<0 else green,.012)
        umbrella((0,.22,2.31),[paint,cream,green],metal)
        box('Accessory rack upright',(.99,-.32,.85),(.06,.06,1.35),metal)
        for z in [.65,.9,1.15]:
            for x in [.77,1.01,1.25]:
                box('Phone case card',(x,-.36,z),(.16,.045,.20),cream if x<1 else plum,.015)
                box('Phone camera opening',(x-.045,-.39,z+.065),(.035,.014,.035),dark,.006)
    bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=bpy.context.selected_objects[0]
    bpy.ops.object.join();model=bpy.context.object;model.name=kind
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(kind+'.blend')))
    bpy.ops.export_scene.gltf(filepath=str(out/(kind+'.glb')),export_format='GLB',use_selection=True,export_animations=False)
    if args.publish:
        for ext,folder in [('.blend',ROOT/'MapSource/environment/street-stalls'),('.glb',ROOT/'Assets/TumbangPreso/Art/models/street-stalls')]:
            folder.mkdir(parents=True,exist_ok=True);shutil.copy2(out/(kind+ext),folder/(kind+ext))
    scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.view_settings.view_transform='Standard'
    scene.world.color=(.14,.15,.16);shade=scene.display.shading;shade.light='STUDIO';shade.color_type='MATERIAL'
    shade.show_shadows=True;shade.show_specular_highlight=False;shade.background_type='WORLD'
    camera=bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'));bpy.context.collection.objects.link(camera);scene.camera=camera
    camera.data.type='ORTHO';camera.data.ortho_scale=3.7;camera.location=(4,-7,3.8)
    camera.rotation_euler=(Vector((0,0,1.3))-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_x=1050;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
    scene.render.filepath=str(out/(kind+'.png'));bpy.ops.render.render(write_still=True)
    print(kind,'vertices',len(model.data.vertices),'faces',len(model.data.polygons))
