"""Refine the live cast's grip, footwear and everyday clothing on its existing rigs.

The named faces, hair, skeleton, palettes and animation bytes remain intact. Added
geometry uses the existing skin joints and atlas. Inday's long imported hand props
are removed: they were the false 'palm' reaching the floor during the slide.
Run while Unity is closed. Rebuild the roster and render before accepting output.
An extras stamp makes repeated invocations a no-op rather than stacking geometry.
"""
from collections import Counter
import hashlib
import itertools
import json
from pathlib import Path
import re
import struct
import numpy as np
from glb_mesh_dump import read_glb, read_accessor

ROOT = Path(__file__).resolve().parents[1]
VERSION = "cast-grip-and-clothing-v1"


def cell(slot):
    return ((2*(slot%8)+1.5)/16, (13.5 if slot>=8 else 9.5)/16)


def slot(uv):
    return min(7,int(uv[0]*16)//2)+(8 if uv[1]*16>=12 else 0)


def write(path,gltf,blob):
    while len(blob)%4: blob.append(0)
    gltf["buffers"][0]["byteLength"]=len(blob)
    text=json.dumps(gltf,separators=(",",":"),ensure_ascii=False).encode()
    text+=b" "*((-len(text))%4)
    result=struct.pack("<III",0x46546c67,2,12+8+len(text)+8+len(blob))
    result+=struct.pack("<II",len(text),0x4e4f534a)+text
    result+=struct.pack("<II",len(blob),0x004e4942)+blob
    path.write_bytes(result)


def animation_digest(g,b):
    h=hashlib.sha256()
    for a in g.get("animations",[]):
        h.update(a["name"].encode())
        for sampler in a["samplers"]:
            for key in ["input","output"]:
                h.update(repr(read_accessor(g,b,sampler[key])).encode())
    return h.hexdigest()


class Geometry:
    def __init__(self, attributes, indices):
        self.data=attributes
        self.indices=indices

    def face(self,points,bone,color,center):
        p=np.asarray(points,dtype=float)
        normal=np.cross(p[1]-p[0],p[2]-p[0])
        if np.dot(normal,p.mean(0)-center)<0:
            p=p[::-1]; normal=-normal
        normal/=np.linalg.norm(normal)
        start=len(self.data["POSITION"])
        for point in p:
            self.data["POSITION"].append(tuple(point))
            self.data["NORMAL"].append(tuple(normal))
            self.data["TEXCOORD_0"].append(cell(color))
            self.data["JOINTS_0"].append((bone,0,0,0))
            self.data["WEIGHTS_0"].append((1.,0.,0.,0.))
            if "TEXCOORD_1" in self.data:self.data["TEXCOORD_1"].append(cell(color))
            if "TANGENT" in self.data:
                tangent=np.cross(normal,(0,1,0) if abs(normal[1])<.9 else (1,0,0))
                tangent/=np.linalg.norm(tangent)
                self.data["TANGENT"].append((*tangent,1.))
        for i in range(1,len(p)-1): self.indices += [start,start+i,start+i+1]

    def bevel(self,center,size,bone,color,radius=.003):
        c=np.asarray(center,dtype=float); h=np.asarray(size,dtype=float)/2
        r=min(radius,float(h.min())*.45)
        corners={}
        for signs in itertools.product([-1,1],repeat=3):
            for axis in range(3):
                p=(h-r)*np.array(signs,dtype=float);p[axis]=h[axis]*signs[axis]
                corners[(signs,axis)]=p+c
        for axis in range(3):
            others=[j for j in range(3) if j!=axis]
            for sign in [-1,1]:
                points=[]
                for a,b in [(-1,-1),(-1,1),(1,1),(1,-1)]:
                    s=[0,0,0];s[axis]=sign;s[others[0]]=a;s[others[1]]=b
                    points.append(corners[(tuple(s),axis)])
                self.face(points,bone,color,c)
        for a,b in [(0,1),(0,2),(1,2)]:
            free=3-a-b
            for sa,sb in itertools.product([-1,1],repeat=2):
                signs=[]
                for sf in [-1,1]:
                    s=[0,0,0];s[a]=sa;s[b]=sb;s[free]=sf;signs.append(tuple(s))
                self.face([corners[(signs[0],a)],corners[(signs[1],a)],
                           corners[(signs[1],b)],corners[(signs[0],b)]],bone,color,c)
        for s in itertools.product([-1,1],repeat=3):
            self.face([corners[(s,a)] for a in range(3)],bone,color,c)


def author(character,path):
    g,original=read_glb(path)
    if g.get("extras",{}).get("castFinish")==VERSION:
        return {"id":character,"file":str(path.relative_to(ROOT)),"state":"already authored",
                "animation_sha256":animation_digest(g,original)}
    before=animation_digest(g,original)
    nodes=g["nodes"]
    body_node=next(n for n in nodes if "skin" in n and "body" in g["meshes"][n["mesh"]].get("name","").lower())
    skin=g["skins"][body_node["skin"]]
    names=[nodes[i]["name"] for i in skin["joints"]]
    mesh=g["meshes"][body_node["mesh"]]
    assert len(mesh["primitives"])==1, path
    primitive=mesh["primitives"][0]
    attributes={k:list(read_accessor(g,original,v)) for k,v in primitive["attributes"].items()}
    assert set(attributes)<= {"POSITION","NORMAL","TEXCOORD_0","TEXCOORD_1","TANGENT","JOINTS_0","WEIGHTS_0"},set(attributes)
    indices=[i[0] for i in read_accessor(g,original,primitive["indices"])]
    positions=np.asarray(attributes["POSITION"])
    bones=np.array([j[int(np.argmax(w))] for j,w in zip(attributes["JOINTS_0"],attributes["WEIGHTS_0"])])
    slots=np.array([slot(uv) for uv in attributes["TEXCOORD_0"]])
    old_vertices=len(positions);old_triangles=len(indices)//3
    removed=0
    if character=="inday":
        keep=[]
        for at in range(0,len(indices),3):
            triangle=indices[at:at+3]
            is_prop=all(names[bones[i]] in ["arm-left","arm-right"] and slots[i] in [5,11] for i in triangle)
            if is_prop and any(abs(positions[i][0])>.40 for i in triangle): removed+=1
            else: keep.extend(triangle)
        indices=keep
    geom=Geometry(attributes,indices)
    original_used=set(indices)
    hands=[]
    for bone_name in ["arm-left","arm-right"]:
        bone=names.index(bone_name)
        candidates=[i for i in original_used if bones[i]==bone and slots[i] in [13,14,15]]
        if not candidates:
            # Work gloves are part of Mang Kanor's identity. Their terminal material
            # remains the glove, rather than inventing an exposed skin-colored hand.
            members=[i for i in original_used if bones[i]==bone]
            far=max(abs(positions[i][0]) for i in members)
            candidates=[i for i in members if abs(positions[i][0])>far-.10]
        skin_points=positions[candidates]
        # The distal skin, not a cuff/weapon bound, supplies the palm profile.
        far=float(np.abs(skin_points[:,0]).max())
        distal=[i for i in candidates if abs(positions[i][0])>far-.12]
        palm=positions[distal]
        lo,hi=palm.min(0),palm.max(0)
        tone=Counter(slots[distal]).most_common(1)[0][0]
        side=1 if bone_name=="arm-left" else -1
        height=float(hi[1]-lo[1])
        center=((far-.044)*side,float((lo[1]+hi[1])*.5-.015),float(hi[2]+.009))
        size=(.058,max(.036,min(.05,height*.40)),.045)
        geom.bevel(center,size,bone,int(tone),.009)
        # A compact second plane gives a thumb pad rather than a square peg.
        geom.bevel((center[0]+side*.016,center[1]-.011,center[2]+.004),(.032,.031,.038),bone,int(tone),.007)
        hands.append({"bone":bone_name,"skin_slot":int(tone),"palm_max":far})
    # A beveled toe/sole edge follows each existing foot's own size and material.
    # Bare feet retain their skin; this does not put every person into a new shoe.
    for bone_name in ["leg-left","leg-right"]:
        bone=names.index(bone_name)
        members=np.where(bones==bone)[0]
        points=positions[members];lo,hi=points.min(0),points.max(0)
        low=[i for i in members if positions[i][1]<lo[1]+.055]
        color=int(Counter(slots[low]).most_common(1)[0][0])
        width=(hi[0]-lo[0])*.83
        geom.bevel(((lo[0]+hi[0])/2,lo[1]+.023,hi[2]-.025),
                   (width,.035,.055),bone,color,.007)
    torso=names.index("torso")
    ids=np.where(bones==torso)[0];p=positions[ids];lo,hi=p.min(0),p.max(0)
    cloth=Counter(int(slots[i]) for i in ids if slots[i] not in [8,13,14,15]).most_common(1)[0][0]
    # The file's front is +Z. Unity's PersonModelYaw describes another space;
    # glb_face_side.py and the in-engine turnaround establish this orientation.
    front=float(hi[2]+.004);hip=float(lo[1]);chest=float(lo[1]+(hi[1]-lo[1])*.68)
    if character in ["bayan","kuya_boy"]:
        x=float(lo[0]*.68)
        geom.bevel((x,hip+.012,front+.012),(.054,.112,.021),torso,12,.003)
        for y in [-.025,.006,.037]: geom.bevel((x,hip+y,front+.024),(.048,.004,.005),torso,cloth,.001)
    elif character=="inday":
        geom.bevel((0,hip+.014,front+.003),(.195,.092,.015),torso,0,.005)
        for x in [-.047,.047]: geom.bevel((x,hip+.039,front+.012),(.056,.006,.009),torso,cloth,.002)
    elif character=="bebang":
        for x in [-.095,-.047,0,.047,.095]:
            geom.bevel((x,hip+.035,front),(.023,.013,.010),torso,cloth,.003)
    elif character in ["maring","totoy","ate_girlie","tikboy","jun_jun","lola_pacing","mang_kanor","aling_nena"]:
        x=float(lo[0]*.52)
        geom.bevel((x,chest,front),(.061,.052,.013),torso,cloth,.004)
        geom.bevel((x,chest+.023,front+.009),(.06,.006,.006),torso,12 if character in ["totoy","ate_girlie"] else cloth,.002)
        if character in ["lola_pacing","aling_nena","jun_jun"]:
            for y in [chest-.02,chest+.018,chest+.053]:
                geom.bevel((0,y,front+.008),(.009,.010,.009),torso,12,.002)
    # Heroes retain their already detailed costume language. Added cuff fasteners
    # and sculpted grips refine it without putting cultural symbols on every hero.
    else:
        for hand in hands:
            bone=names.index(hand["bone"]);side=1 if hand["bone"]=="arm-left" else -1
            points=positions[np.where(bones==bone)[0]]
            y=float(np.median(points[:,1]));z=float(points[:,2].max()+.002)
            geom.bevel(((hand["palm_max"]-.11)*side,y,z),(.016,.019,.008),bone,cloth,.003)

    if character=="inday":
        # HandAnchor and the slide solver inspect vertex arrays, not only triangles.
        # Keeping discarded prop vertices would leave the false palm bound intact.
        used=sorted(set(geom.indices));remap={old:new for new,old in enumerate(used)}
        attributes={name:[values[i] for i in used] for name,values in attributes.items()}
        geom.indices=[remap[i] for i in geom.indices]
    blob=bytearray(original)
    def add(values,kind,component,fmt):
        while len(blob)%4:blob.append(0)
        offset=len(blob)
        for value in values:blob.extend(struct.pack("<"+fmt*len(value),*value))
        view=len(g["bufferViews"])
        g["bufferViews"].append({"buffer":0,"byteOffset":offset,"byteLength":len(blob)-offset})
        accessor={"bufferView":view,"componentType":component,"count":len(values),"type":kind}
        if kind=="VEC3":
            a=np.asarray(values);accessor["min"]=a.min(0).tolist();accessor["max"]=a.max(0).tolist()
        g["accessors"].append(accessor)
        return len(g["accessors"])-1
    for name,values in attributes.items():
        kind=g["accessors"][primitive["attributes"][name]]["type"]
        primitive["attributes"][name]=add(values,kind,5123 if name=="JOINTS_0" else 5126,"H" if name=="JOINTS_0" else "f")
    primitive["indices"]=add([(i,) for i in geom.indices],"SCALAR",5125,"I")
    g.setdefault("extras",{})["castFinish"]=VERSION
    write(path,g,blob)
    after_g,after_b=read_glb(path)
    assert animation_digest(after_g,after_b)==before, "Animation bytes changed"
    return {"id":character,"file":str(path.relative_to(ROOT)),"old_vertices":old_vertices,
            "new_vertices":len(attributes["POSITION"]),"old_triangles":old_triangles,
            "new_triangles":len(geom.indices)//3,"removed_prop_triangles":removed,
            "hands":hands,"animation_sha256":before,"state":"authored"}


if __name__=="__main__":
    source=(ROOT/"Assets/TumbangPreso/Editor/RosterBookBuilder.cs").read_text(encoding="utf-8")
    table=source.split("PersonModels =",1)[1].split("};",1)[0]
    entries=re.findall(r'\{\s*"([^"]+)"\s*,\s*"([^"]+\.glb)"',table)
    results=[]
    for character,relative in entries:
        if character.startswith("custom"):continue
        result=author(character,ROOT/"Assets/TumbangPreso/Art"/relative)
        results.append(result);print(character,result["state"])
    report=ROOT/"Logs/cast-finish.json"
    report.write_text(json.dumps(results,indent=2)+"\n")
