"""Bake an edge matte for the supplied photo; original RGB source stays unchanged.

Developer dependency: pymatting==1.1.16 in a dedicated environment. See
https://pymatting.github.io/alpha.html and /foreground.html.
"""
from pathlib import Path
import numpy as np
from scipy import ndimage
from PIL import Image, ImageDraw
from pymatting import estimate_alpha_cf, estimate_foreground_ml
import time
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'ArtSource/ui/owner-ui-edits-2026-09-15/background_mainmenu_clean.png'
OUT=ROOT/'Logs/matte-authoring';OUT.mkdir(exist_ok=True)
BOX=(1170,0,1810,350)
image=np.array(Image.open(SOURCE).convert('RGB').crop(BOX)).astype(np.float64)/255
h,w=image.shape[:2];yy,xx=np.indices((h,w));x=xx+BOX[0];y=yy
# Conservative colour seeds, with all fine edges left unknown.
r,g,b=image[:,:,0],image[:,:,1],image[:,:,2]
fg_candidate=(b<.345)|((y>268)&(b>g+.01))
fg=ndimage.binary_erosion(fg_candidate,iterations=2)
# Interior anchors only. These are not cutout contours.
seed=Image.new('L',(w,h),0);d=ImageDraw.Draw(seed)
def poly(points,value=255):d.polygon([(a-BOX[0],b) for a,b in points],fill=value)
poly([(1170,43),(1207,45),(1260,154),(1237,157),(1237,343),(1170,343)])
poly([(1259,228),(1309,216),(1337,236),(1335,312),(1260,312)])
fg|=np.array(seed)>0
fg[y>334]=True
# Broad possible-building regions prevent cream walls becoming sky seeds;
# their actual perimeter remains unknown for the matting solver to recover.
possible=Image.new('L',(w,h),0);d=ImageDraw.Draw(possible)
poly([(1170,20),(1220,22),(1294,170),(1259,187),(1259,216),(1170,220)])
poly([(1237,195),(1322,187),(1368,222),(1368,335),(1237,335)])
building=np.array(possible)>0
bg_colour=(b>.405)&(g>.57)&(g-b>.055)
near_fg=ndimage.distance_transform_edt(~fg_candidate)
bg=bg_colour&(near_fg>10)&~building
# Keep the faint, blurred wires unknown all the way back to opaque poles.
p=np.stack((x,y),axis=-1);wire=np.full((h,w),1000.)
for a,c in [((1238,103),(1380,151)),((1380,151),(1490,145)),((1490,145),(1580,115)),
            ((1243,126),(1468,214)),((1468,214),(1675,262)),((1275,171),(1380,151)),
            ((1380,174),(1510,222)),((1510,222),(1675,263))]:
 a=np.array(a);ab=np.array(c)-a;t=np.clip(np.sum((p-a)*ab,axis=-1)/np.sum(ab*ab),0,1)
 wire=np.minimum(wire,np.linalg.norm(p-a-t[:,:,None]*ab,axis=-1))
manual_bg=Image.new('L',(w,h),0);d=ImageDraw.Draw(manual_bg)
poly([(1230,4),(1423,4),(1423,124),(1279,124),(1262,102),(1230,60)])
bg|=np.array(manual_bg)>0
bg&=wire>7
bg[y>318]=False
trimap=np.full((h,w),.5);trimap[bg]=0;trimap[fg]=1
Image.fromarray(np.uint8(trimap*255)).save(OUT/'trimap.png')
print('seeds',int(fg.sum()),int(bg.sum()),'unknown',int(((trimap>0)&(trimap<1)).sum()),flush=True)
t=time.time();alpha=estimate_alpha_cf(image,trimap,laplacian_kwargs={'epsilon':1e-6},cg_kwargs={'maxiter':2000})
print('alpha seconds',time.time()-t,flush=True)
foreground,background=estimate_foreground_ml(image,alpha,return_background=True,n_big_iterations=5)
visibility=1-alpha
Image.fromarray(np.uint8(np.rint(np.clip(alpha,0,1)*255))).save(OUT/'foreground-alpha.png')
Image.fromarray(np.uint8(np.rint(np.clip(background,0,1)*255))).save(OUT/'old-background.png')
np.save(OUT/'visibility.npy',visibility);np.save(OUT/'background.npy',background)
sky=np.stack((.245151+.057191*x/1920+.227202*y/1080,.572371+.087356*x/1920+.048443*y/1080,.502873+.010417*x/1920-.036591*y/1080),axis=-1)
composite=np.clip(image+visibility[:,:,None]*(sky-background),0,1)
Image.fromarray(np.uint8(np.rint(composite*255))).save(OUT/'clear-composite.png')
print('done',time.time()-t,flush=True)

import json,hashlib
asset=SOURCE.parent/'derived';asset.mkdir(exist_ok=True)
mask=np.zeros((1080,1920),dtype=np.uint8)
mask[BOX[1]:BOX[3],BOX[0]:BOX[2]]=np.uint8(np.rint(np.clip(visibility,0,1)*255))
Image.fromarray(mask).save(asset/'main-sky-cutout.png')
Image.fromarray(np.uint8(np.rint(np.clip(background,0,1)*255))).save(asset/'main-sky-background-data.png')
(asset/'sky-cutout-manifest.json').write_text(json.dumps({
 'source':SOURCE.name,'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
 'kind':'closed-form foreground matte and original background contribution',
 'method':'pymatting 1.1.16 closed-form alpha; multilevel background estimate',
 'crop_xyxy':BOX,'source_photo_modified':False,
 'mask_sha256':hashlib.sha256((asset/'main-sky-cutout.png').read_bytes()).hexdigest(),
 'background_data_sha256':hashlib.sha256((asset/'main-sky-background-data.png').read_bytes()).hexdigest()
},indent=2),encoding='utf-8')
