"""Original pegged clothing for Eskinita and Sa Bubong, authored in Blender.

Native source uses metre-scale garments, a fixed sagging rope, separate cloth
pivots at the pegs and simple faceted folds. Drafts stay outside Unity imports.
"""
import argparse,json,math,sys
from pathlib import Path
import bpy,bmesh
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--out',default='Logs/resident-laundry-v1')
args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
out=ROOT/args.out;out.mkdir(parents=True,exist_ok=True)

def material(name,color):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    node=m.node_tree.nodes.get('Principled BSDF');node.inputs['Base Color'].default_value=(*color,1)
    node.inputs['Roughness'].default_value=.92
    return m

def mesh(name,vertices,faces,mat):
    data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update()
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
    obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj);data.materials.append(mat)
    return obj

def rope(points,mat):
    vertices=[];faces=[];sides=6
    for j,p in enumerate(points):
        axis=(Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])).normalized()
        u=axis.cross(Vector((0,1,0))).normalized();v=axis.cross(u).normalized()
        for n in range(sides):vertices.append(Vector(p)+(u*math.cos(n*math.tau/sides)+v*math.sin(n*math.tau/sides))*.010)
    for j in range(len(points)-1):
        for n in range(sides):faces.append((j*sides+n,j*sides+(n+1)%sides,(j+1)*sides+(n+1)%sides,(j+1)*sides+n))
    mesh('Fixed clothesline',vertices,faces,mat)

def box(name,at,size,mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=at);obj=bpy.context.object;obj.name=name
    obj.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    obj.data.materials.append(mat);return obj

def garment(kind,number,at,slope,mat):
    outlines={
        'Tee':[(-.34,-.08),(-.2,0),(-.12,-.015),(0,-.075),(.12,-.015),(.2,0),(.34,-.08),(.43,-.26),(.28,-.30),(.20,-.19),(.21,-.69),(-.20,-.70),(-.20,-.19),(-.28,-.30),(-.43,-.26)],
        'Shorts':[(-.27,0),(.27,0),(.30,-.53),(.07,-.56),(0,-.29),(-.06,-.55),(-.30,-.53)],
        'Towel':[(-.26,0),(.26,0),(.255,-.39),(.27,-.82),(.05,-.80),(-.14,-.83),(-.27,-.81),(-.25,-.39)]}
    outline=outlines[kind];verts=[]
    for side in [-1,1]:
        for x,z in outline:
            fold=(math.sin(x*12+number)*.022+math.sin(-z*5)*.025)*min(1,-z*3)
            verts.append((x,fold+side*.009,z))
    count=len(outline);faces=[tuple(range(count-1,-1,-1)),tuple(range(count,count*2))]
    for n in range(count):faces.append((n,(n+1)%count,(n+1)%count+count,n+count))
    obj=mesh(f'Cloth_{kind}_{number}',verts,faces,mat);obj.location=at;obj.rotation_euler[1]=-math.atan(slope)
    return obj

specs=[('courtyard-line',4.4,.16,[(.65,'Tee',0),(1.62,'Shorts',1),(2.47,'Towel',2),(3.48,'Tee',3)]),
       ('alley-line',18.0,.48,[(2.0,'Towel',2),(3.0,'Tee',0),(4.0,'Shorts',1),(5.0,'Tee',3),
                              (11.4,'Tee',0),(12.45,'Towel',4),(13.4,'Shorts',1),(14.45,'Tee',2)])]
for name,span,sag,clothes in specs:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    rope_mat=material('Worn braided line',(.27,.25,.19));peg=material('Wooden clothespins',(.56,.42,.25))
    colors=[(.72,.73,.63),(.18,.27,.34),(.50,.60,.54),(.55,.36,.28),(.63,.56,.68)]
    cloth=[material('Cotton '+str(i),c) for i,c in enumerate(colors)]
    def height(x):return -4*sag*(x/span)*(1-x/span)
    rope([(span*j/48,0,height(span*j/48)) for j in range(49)],rope_mat)
    for i,(x,kind,color) in enumerate(clothes):
        slope=-4*sag/span*(1-2*x/span)
        garment(kind,i,(x,0,height(x)),slope,cloth[color])
        for dx in [-.18,.18]:box(f'Peg_{i}_{dx}',(x+dx,-.015,height(x+dx)-.018),(.025,.048,.075),peg)
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(name+'.blend')))
    bpy.ops.export_scene.gltf(filepath=str(out/(name+'.glb')),export_format='GLB',export_yup=True)
    (out/(name+'.json')).write_text(json.dumps({'spanMetres':span,'sagMetres':sag,'garments':len(clothes),
        'anchorLocalUnity':[[0,0,0],[span,0,0]],'source':'Original Blender geometry, no imported photographs'},indent=2))
    scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.view_settings.view_transform='Standard'
    scene.world.color=(.22,.24,.24);shade=scene.display.shading;shade.light='STUDIO';shade.color_type='MATERIAL'
    shade.show_shadows=True;shade.show_specular_highlight=False;shade.background_type='WORLD'
    data=bpy.data.cameras.new('Laundry review');camera=bpy.data.objects.new('Laundry review',data)
    bpy.context.collection.objects.link(camera);scene.camera=camera
    camera.location=(span*.5,-9,1.1);camera.rotation_euler=(Vector((span*.5,0,-.4))-camera.location).to_track_quat('-Z','Y').to_euler()
    data.type='ORTHO';data.ortho_scale=span+1
    scene.render.resolution_x=1600;scene.render.resolution_y=420;scene.render.resolution_percentage=100
    scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
print('Authored two original resident laundry assemblies.')
