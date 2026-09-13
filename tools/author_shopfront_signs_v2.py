"""Original shop-specific layouts and brush lettering; no borrowed photographs.

Uses licensed repository fonts for printed signs. Bawal letters are original
stroke paths. Write drafts to Logs, inspect, then publish outside Unity runs.
"""
from pathlib import Path
import argparse
import random
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
FONTS=ROOT/'Assets/TumbangPreso/Resources/UI/fonts'
parser=argparse.ArgumentParser()
parser.add_argument('--out',default='Logs/shop-signs-owner-v2')
out=ROOT/parser.parse_args().out;out.mkdir(parents=True,exist_ok=True)

def canvas(color):
    im=Image.new('RGB',(1280,320),color)
    return im,ImageDraw.Draw(im)

def label(draw,at,text,size,color,font='WorkSans-Bold.ttf',anchor='la'):
    draw.text(at,text,font=ImageFont.truetype(str(FONTS/font),size),fill=color,anchor=anchor)

def save(im,name):im.save(out/(name+'.png'))

# A blue laundry tarpaulin: a simple washer symbol, service name and big verbs.
im,d=canvas('#285c86')
d.rectangle((0,242,1280,320),fill='#e6e2c9')
d.rounded_rectangle((40,45,215,221),radius=12,outline='#e6e2c9',width=8)
d.ellipse((65,96,190,216),outline='#e6e2c9',width=8)
d.line((64,75,190,75),fill='#e6e2c9',width=6)
label(d,(266,8),'LABADA',150,'#f0e5c7')
label(d,(280,175),'WASH  /  DRY  /  FOLD',51,'#f0e5c7')
label(d,(640,247),'LAUNDRY SERVICE',49,'#285c86',anchor='ma')
save(im,'Laundry')

# A low-cost print shop panel: service words earn more space than a brand name.
im,d=canvas('#ebe6d5')
label(d,(34,12),'PRINT',146,'#b13429')
d.line((565,33,565,204),fill='#314d70',width=5)
label(d,(620,12),'COPY',146,'#314d70')
d.rectangle((0,226,1280,320),fill='#d4b849')
label(d,(640,238),'SCAN     LAMINATE     SCHOOL PROJECTS',46,'#253b51',anchor='ma')
save(im,'Print')

# Computer shop vinyl: left-aligned, stacked, white field and a blue service strip.
im,d=canvas('#dedfd8')
d.rectangle((0,0,30,320),fill='#274f70')
label(d,(66,4),'COMPUTER',74,'#203b52')
label(d,(60,82),'REPAIR',150,'#263746')
d.rectangle((870,0,1280,320),fill='#274f70')
for y,text in [(23,'LAPTOP'),(115,'DESKTOP'),(207,'UPGRADES')]:
    label(d,(902,y),text,49,'#e9e5d5')
save(im,'Repair')

# A small shop's painted fascia: a red trade name, no corporate subtitle lockup.
im,d=canvas('#ddd4af')
label(d,(52,3),'TINDAHAN',137,'#973b2c','DarumadropOne-Regular.ttf')
d.line((55,207,1210,211),fill='#567050',width=6)
label(d,(60,235),'LOAD    ICE    COLD DRINKS',57,'#3f5a3e')
save(im,'Load')

# Ukay tarpaulin. Large red words and an offset cream price-style patch.
im,d=canvas('#b7ab64')
label(d,(34,3),'UKAY',145,'#8a3028')
label(d,(496,76),'UKAY',145,'#8a3028')
d.polygon([(942,23),(1240,8),(1260,289),(953,304)],fill='#e7ddbd')
label(d,(1100,61),'PRE-',61,'#463b31',anchor='ma')
label(d,(1100,137),'LOVED',61,'#463b31',anchor='ma')
label(d,(41,235),'CLOTHES & BAGS',47,'#433b2e')
save(im,'Clothing')

# Barber fascia: the familiar stripe belongs to the trade, not every business.
im,d=canvas('#e9e0cc')
for x in (0,1190):
    for y in range(-100,360,90):
        d.polygon([(x,y),(x+90,y+60),(x+90,y+103),(x,y+43)],fill='#a43e35' if y%180==80 else '#42627a')
label(d,(640,13),'BARBER SHOP',124,'#303d43',anchor='ma')
label(d,(640,206),'HAIRCUT  /  SHAVE',58,'#984338',anchor='ma')
save(im,'Barber')

# Food stall sign: warm hand-painted lettering on red timber, a menu-like bottom.
im,d=canvas('#8f3829')
label(d,(30,0),'PARES & MAMI',124,'#e4c25d','DarumadropOne-Regular.ttf')
for y in (12,307):d.line((0,y,1280,y),fill='#733429',width=6)
label(d,(54,231),'BEEF PARES    MAMI    RICE',56,'#e6d5a7')
save(im,'Pares')

# Pisonet vinyl, with the rental unit as the largest numeral. No gameplay charge.
im,d=canvas('#d8ddd4')
label(d,(24,0),'PISO NET',124,'#314d78')
label(d,(45,207),'PRINT / SCAN',55,'#464e50')
d.rectangle((867,0,1280,320),fill='#d4b858')
label(d,(928,-20),'1',222,'#9f3b32')
label(d,(1090,32),'PESO',51,'#4b4333')
label(d,(910,244),'4 MINUTES',46,'#4b4333')
save(im,'Pisonet')

# Bakery lettering painted directly on a cream wooden fascia, simple bread mark.
im,d=canvas('#dbc59b')
label(d,(38,10),'PANADERIA',132,'#91472e','DarumadropOne-Regular.ttf')
label(d,(52,234),'FRESH PANDESAL',52,'#65472f')
for x,y in [(988,210),(1070,237),(1152,211)]:
    d.ellipse((x,y,x+92,y+55),fill='#bd8c49',outline='#795638',width=3)
    d.line((x+28,y+8,x+46,y+33),fill='#e3c181',width=5)
save(im,'Bakery')

# Tyre shop: a blunt stencilled service word on faded yellow, no decorative frame.
im,d=canvas('#c4ae58')
label(d,(22,28),'VULCANIZING',139,'#30352e')
label(d,(47,233),'TYRE REPAIR   /   AIR   /   PATCH',47,'#393e32')
save(im,'Hardware')

# Brush warning, intentionally without a backing rectangle or printed typeface.
# Normalized paths describe each separate brush stroke.
glyphs={
 'B':[[(0,1),(0,0),(.65,0),(1,.18),(.72,.46),(0,.46)],[(.72,.46),(1,.7),(.74,1),(0,1)]],
 'A':[[(0,1),(.48,0),(1,1)],[(.2,.62),(.78,.62)]],
 'W':[[(0,0),(.22,1),(.5,.49),(.75,1),(1,0)]],
 'L':[[(0,0),(0,1),(1,1)]],
 'U':[[(0,0),(0,.79),(.18,1),(.76,1),(1,.78),(1,0)]],
 'M':[[(0,1),(0,0),(.5,.59),(1,0),(1,1)]],
 'I':[[(.1,0),(.9,0)],[(.5,0),(.5,1)],[(.1,1),(.9,1)]],
 'H':[[(0,0),(0,1)],[(1,0),(1,1)],[(0,.5),(1,.5)]],
 'D':[[(0,1),(0,0),(.57,0),(1,.22),(1,.77),(.57,1),(0,1)]],
 'T':[[(0,0),(1,0)],[(.5,0),(.5,1)]],
 'O':[[(.23,0),(.76,0),(1,.26),(1,.78),(.76,1),(.23,1),(0,.76),(0,.24),(.23,0)]],
}
im=Image.new('RGBA',(640,560),(0,0,0,0));d=ImageDraw.Draw(im);rng=random.Random(9713)
for row,word in enumerate(['BAWAL','UMIHI','DITO']):
    advance=108 if row<2 else 112;start=(640-len(word)*advance+19)//2
    for index,char in enumerate(word):
        x=start+index*advance;y=30+row*178+rng.randint(-4,4)
        width=76+rng.randint(-5,5);height=113+rng.randint(-6,6);lean=rng.uniform(-6,6)
        for stroke in glyphs[char]:
            points=[(round(x+a*width+b*lean),round(y+b*height)) for a,b in stroke]
            d.line(points,fill=(129+rng.randrange(-8,9),43,32,237),width=rng.randint(10,14),joint='curve')
            # Fine dry-brush gaps follow the stroke, not random noise over the wall.
            if rng.random()<.5:
                d.line([(px+3,py) for px,py in points],fill=(137,46,34,95),width=1)
save(im,'Bawal')
print('Authored 10 independent shop layouts and original surface brush lettering.')
