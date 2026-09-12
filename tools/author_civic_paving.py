"""Original seamless civic paving with measured slab joints and restrained grain.

One texture repeat covers 6m, divided into sixteen 1.5m concrete/stone panels.
Generate into Logs for inspection; publication is separate from Unity runs.
"""
import argparse
import json
import random
from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter

ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--out',default='Logs/civic-paving-v1');a=p.parse_args()
out=ROOT/a.out;out.mkdir(parents=True,exist_ok=True)
rng=random.Random(734081)
size=1024;panel=256
image=Image.new('RGB',(size,size));draw=ImageDraw.Draw(image)
height=Image.new('L',(size,size),128);hd=ImageDraw.Draw(height)
for x in range(4):
    for y in range(4):
        variation=rng.randrange(-5,6)
        base=(148+variation,144+variation,132+variation)
        left=x*panel;top=y*panel
        draw.rectangle((left,top,left+panel-1,top+panel-1),fill=base)
        # A12mm dark joint, not a white line or an additional game boundary.
        draw.line((left,top,left+panel-1,top),fill=(119,117,108),width=2)
        draw.line((left,top,left,top+panel-1),fill=(119,117,108),width=2)
        hd.line((left,top,left+panel-1,top),fill=112,width=2)
        hd.line((left,top,left,top+panel-1),fill=112,width=2)
        # Fine aggregate is deliberately low contrast and sparse, with one quiet
        # repair at the perimeter of the repeat rather than damage on every slab.
        for i in range(2300):
            px=left+rng.randrange(2,panel);py=top+rng.randrange(2,panel)
            delta=rng.choice([-12,-9,-6,6,9])
            draw.point((px,py),fill=tuple(c+delta for c in base))
        if x==3 and y==0:
            repair=[(left+188,top+2),(left+253,top+2),(left+253,top+44),(left+226,top+35),(left+209,top+18)]
            draw.polygon(repair,fill=tuple(c-8 for c in base))
# A low-amplitude, periodic substrate avoids a perfectly digital flat fill.
cloud=[[rng.randrange(117,140) for _ in range(32)] for _ in range(32)]
pixels=image.load()
for y in range(size):
    for x in range(size):
        gx=x*32/size;gy=y*32/size;ix=int(gx);iy=int(gy);fx=gx-ix;fy=gy-iy
        fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy)
        lo=cloud[iy][ix]*(1-fx)+cloud[iy][(ix+1)%32]*fx
        hi=cloud[(iy+1)%32][ix]*(1-fx)+cloud[(iy+1)%32][(ix+1)%32]*fx
        delta=round((lo*(1-fy)+hi*fy-128)*.35)
        pixels[x,y]=tuple(max(0,min(255,c+delta)) for c in pixels[x,y])
image.save(out/'civic-concrete-albedo.png')
# Derivatives wrap at the repeat edges, so the normal has no artificial seam.
h=height.filter(ImageFilter.GaussianBlur(.65));hp=h.load();normal=Image.new('RGB',(size,size));np=normal.load()
for y in range(size):
    for x in range(size):
        dx=(hp[(x+1)%size,y]-hp[(x-1)%size,y])*.65
        dy=(hp[x,(y+1)%size]-hp[x,(y-1)%size])*.65
        np[x,y]=(round(128-dx),round(128+dy),255)
normal.save(out/'civic-concrete-normal.png')
(out/'paving-source.json').write_text(json.dumps({'repeatMetres':6,'slabMetres':1.5,'jointMetres':.012,'pixels':size,'seed':734081,'license':'Original procedural artwork'},indent=2)+'\n')
print('Authored civic paving,6m repeat,1.5m slabs,12mm joints.')
