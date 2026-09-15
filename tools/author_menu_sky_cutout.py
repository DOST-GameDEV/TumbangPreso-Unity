"""Sky-opening opacity data for compositing clouds behind the untouched photo."""
from pathlib import Path
import hashlib,json
import numpy as np
from scipy import ndimage
from PIL import Image,ImageDraw

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'ArtSource/ui/owner-ui-edits-2026-09-15/background_mainmenu_clean.png'

def smooth(lo,hi,value):
    t=np.clip((value-lo)/(hi-lo),0,1);return t*t*(3-2*t)

def main():
    image=Image.open(SOURCE)
    if image.size!=(1920,1080):raise ValueError('Reauthor sky boundary for a changed composition.')
    rgb=np.array(image).astype(float);y,x=np.indices(rgb.shape[:2]);p=np.stack((x,y),axis=-1)
    region=Image.new('L',image.size,0);draw=ImageDraw.Draw(region)
    boundary=[(1184,0),(1780,0),(1780,284),(1693,296),(1645,277),(1588,300),
              (1555,309),(1518,296),(1468,315),(1432,302),(1388,310),(1350,300),
              (1280,312),(1252,203),(1252,177),(1284,166),(1192,33)]
    draw.polygon(boundary,fill=255)
    # This sunlit house is warm like the clouds, but is foreground architecture.
    draw.polygon([(1240,208),(1313,198),(1358,225),(1357,323),(1248,328)],fill=0)
    # Cloud and teal-sky pixels have blue/green light absent from opaque wood and
    # foliage. Retain their soft original boundaries; never regenerate the town.
    clear=smooth(74,102,rgb[:,:,2])*smooth(128,157,rgb[:,:,1])
    clear*=smooth(4,16,rgb[:,:,1]-rgb[:,:,2])*(np.array(region)/255)
    boundary_distance=ndimage.distance_transform_edt(np.array(region)>0)
    clear*=np.where(rgb[:,:,1]-rgb[:,:,0]>50,smooth(0,16,boundary_distance),1)
    # Preserve the original soft, warm horizon and every small distant building.
    clear*=1-smooth(260,292,y)
    # Faint painted wires need a narrow detail holdout, not the broad bands used
    # by the rejected UV-warp experiment. Detect their dark centres within paths.
    wire=np.full((1080,1920),1000.0)
    for a,b in [((1238,103),(1380,151)),((1380,151),(1490,145)),((1490,145),(1580,115)),
                ((1243,126),(1468,214)),((1468,214),(1675,262)),((1275,171),(1380,151)),
                ((1380,174),(1510,222)),((1510,222),(1675,263))]:
        a=np.array(a);ab=np.array(b)-a;t=np.clip(np.sum((p-a)*ab,axis=-1)/np.sum(ab*ab),0,1)
        wire=np.minimum(wire,np.linalg.norm(p-a-t[:,:,None]*ab,axis=-1))
    dark=ndimage.gaussian_filter(rgb[:,:,1],4)-rgb[:,:,1]
    detail=(1-smooth(5,10,wire))*smooth(.8,3.5,dark)
    clear*=1-detail
    out=SOURCE.parent/'derived';out.mkdir(exist_ok=True)
    target=out/'main-sky-cutout.png';Image.fromarray(np.uint8(np.rint(clear*255))).save(target)
    (out/'sky-cutout-manifest.json').write_text(json.dumps({'source':SOURCE.name,
        'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'output':target.name,
        'output_sha256':hashlib.sha256(target.read_bytes()).hexdigest(),
        'kind':'sky opening opacity, original RGB remains untouched','boundary':boundary},indent=2))
    print('Authored sky opening with original foreground/line detail holdouts.')

if __name__=='__main__':main()
