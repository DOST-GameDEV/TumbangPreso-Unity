"""Original rolled court coating for Sa Bubong; draft output is outside Assets."""
import argparse,json,random
from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter

ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--out',default='Logs/roofdeck-surface-v1')
out=ROOT/p.parse_args().out;out.mkdir(parents=True,exist_ok=True)
rng=random.Random(913240);size=1024
cloud=Image.new('L',(32,32));cloud.putdata([rng.randrange(108,147) for _ in range(32*32)])
cloud=cloud.resize((size,size),Image.Resampling.BICUBIC).filter(ImageFilter.GaussianBlur(8))
im=Image.new('RGB',(size,size));height=Image.new('L',(size,size),128)
pixels=im.load();hp=height.load();cp=cloud.load()
for y in range(size):
    for x in range(size):
        grain=rng.randrange(-5,6);broad=(cp[x,y]-128)*.32
        pixels[x,y]=tuple(round(c+grain+broad) for c in (117,132,121))
        hp[x,y]=128+grain
# Small worn spots reveal grey concrete,without a large noisy camouflage pattern.
d=ImageDraw.Draw(im)
for _ in range(1900):
    x=rng.randrange(size);y=rng.randrange(size);r=rng.choice([1,1,2])
    d.ellipse((x,y,x+r,y+r),fill=(132,137,125))
im.save(out/'roof-court-paint.png')
height=height.filter(ImageFilter.GaussianBlur(.7));hp=height.load()
normal=Image.new('RGB',(size,size));np=normal.load()
for y in range(size):
    for x in range(size):
        dx=(hp[(x+1)%size,y]-hp[(x-1)%size,y])*.5
        dy=(hp[x,(y+1)%size]-hp[x,(y-1)%size])*.5
        np[x,y]=(round(128-dx),round(128+dy),255)
normal.save(out/'roof-court-normal.png')
(out/'surface-source.json').write_text(json.dumps({'seed':913240,'repeatMetres':4,
    'intention':'Faded green recreation-court coating over roof concrete','source':'Original procedural artwork'},indent=2))
print('Authored original roof court coating and relief.')
