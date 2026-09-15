"""Author ground/occluder mask data; the owner's illustration is never changed."""
from pathlib import Path
import hashlib,json
import numpy as np
from PIL import Image,ImageDraw
from scipy import ndimage

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'ArtSource/ui/owner-ui-edits-2026-09-15/background_mainmenu_clean.png'
OUT=SOURCE.parent/'derived'

def main():
    image=Image.open(SOURCE)
    if image.size!=(1920,1080):raise ValueError('Reauthor ground polygons for a different composition.')
    mask=Image.new('L',image.size,0);draw=ImageDraw.Draw(mask)
    draw.polygon([(0,735),(545,697),(817,679),(987,644),(1035,590),(1104,563),
                  (1515,540),(1728,532),(1790,598),(1919,645),(1919,1079),(0,1079)],fill=255)
    # Visible solids interrupt the wind field. Their painted shadows stay ground.
    solids={
        'can':[(1538,648),(1571,634),(1688,633),(1743,646),(1751,688),(1750,917),
               (1708,941),(1645,952),(1584,931),(1567,899),(1555,693)],
        'slipper':[(1240,706),(1278,691),(1320,687),(1361,670),(1427,671),(1473,712),
                   (1524,734),(1540,758),(1525,782),(1461,795),(1320,775),(1250,750)],
        'corner_plants':[(803,670),(868,648),(909,575),(971,579),(1032,632),(999,658),(906,680)],
        'left_leaf':[(223,889),(270,889),(315,908),(293,922),(244,916)],
        'near_leaf':[(1029,954),(1077,936),(1144,950),(1156,967),(1110,984),(1048,971)],
        'foreground_foliage':[(0,878),(47,895),(56,932),(153,928),(219,952),(245,1079),(0,1079)]
    }
    for polygon in solids.values():draw.polygon(polygon,fill=0)
    ground=np.array(mask)>0;rgb=np.array(image.convert('RGB')).astype(int)
    yy,xx=np.indices(ground.shape)
    ground &= ~((xx<360)&(yy>840)&(rgb[:,:,1]>=rgb[:,:,0]-5)&(rgb[:,:,2]<100))
    distance=ndimage.distance_transform_edt(ground)
    alpha=np.uint8(np.clip(distance/4,0,1)*255)
    rgba=np.full((1080,1920,4),255,dtype=np.uint8);rgba[:,:,3]=alpha
    OUT.mkdir(exist_ok=True);target=OUT/'main-ground-mask.png';Image.fromarray(rgba).save(target)
    (OUT/'ground-manifest.json').write_text(json.dumps({'source':SOURCE.name,
        'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        'output':target.name,'output_sha256':hashlib.sha256(target.read_bytes()).hexdigest(),
        'kind':'ground opacity data, not replacement artwork','protected_solids':solids,
        'edge_feather_pixels':4},indent=2))
    print('Ground mask covers the sandy plane and protects painted foreground objects.')

if __name__=='__main__':main()
