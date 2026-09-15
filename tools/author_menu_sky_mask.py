"""Bake distance-to-occluder data for the supplied menu, without editing its art."""
from pathlib import Path
import json
import hashlib
import numpy as np
from scipy import ndimage
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'ArtSource/ui/owner-ui-edits-2026-09-15/background_mainmenu_clean.png'
OUT=SOURCE.parent/'derived'

def main():
    rgb=np.array(Image.open(SOURCE)).astype(float)/255
    if rgb.shape[:2]!=(1080,1920):
        raise ValueError("Sky holdouts are authored for the supplied 1920x1080 composition; reauthor them for a new background.")
    y,x=np.indices(rgb.shape[:2]);p=np.stack((x,y),axis=-1)
    sky=(rgb[:,:,2]>.39)&(rgb[:,:,1]>.54)&(x>=1220)&(x<=1750)&(y<=326)
    # The blurred wires share colours with cloud shadows. Protect their paths
    # explicitly, as well as the foreground building and palm silhouettes.
    lines=[((1238,103),(1380,151)),((1380,151),(1490,145)),((1490,145),(1580,115)),
           ((1243,126),(1468,214)),((1468,214),(1675,262)),((1275,171),(1380,151)),
           ((1380,174),(1510,222)),((1510,222),(1675,263))]
    for a,b in lines:
        a=np.array(a);ab=np.array(b)-a
        t=np.clip(np.sum((p-a)*ab,axis=-1)/np.sum(ab*ab),0,1)
        distance=np.linalg.norm(p-a-t[:,:,None]*ab,axis=-1)
        sky &= distance>9
    roof=np.where(y<180,1199+y*.5,1267)
    sky &= x>roof
    sky &= ~((y>187)&(x<1380))
    sky &= ~((x>=1391)&(x<=1540)&(y>=187)&(y<=332))
    distance=ndimage.distance_transform_edt(sky)
    # 64px feather and a <=36px offset produce a continuous, non-folding flow
    # near occluders. The shader samples once, avoiding blended duplicate edges.
    data=np.uint8(np.rint(np.clip((distance-3)/64,0,1)*255))
    OUT.mkdir(exist_ok=True)
    target=OUT/'main-sky-mask.png';Image.fromarray(data).save(target)
    (OUT/'manifest.json').write_text(json.dumps({'source':SOURCE.name,
        'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        'output':target.name,'output_sha256':hashlib.sha256(target.read_bytes()).hexdigest(),
        'kind':'linear distance data, not replacement artwork','feather_pixels':64,
        'protected_edge_pixels':3,'size':list(data.shape[::-1])},indent=2))
    print('Baked sky distance data; original illustration unchanged.')

if __name__=='__main__':main()
