"""Original readable sign faces, using the repository's licensed typefaces.

These are source-authored raster lettering on physical signs, not imported photos
or hundreds of floating glyph blocks. Actual mounting is owned by MapPlaceAuthor.
"""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/TumbangPreso/Art/PlaceRework/Signs'
OUT.mkdir(parents=True,exist_ok=True)
FONT=ROOT/'Assets/TumbangPreso/Resources/UI/fonts'
signs={
 'Laundry':('LABADA','WASH / DRY / FOLD','#d6ceae','#3b4b3e'),
 'Print':('PRINT & COPY','SCHOOL / OFFICE','#dbd7c6','#374e4a'),
 'Repair':('COMPUTER REPAIR','PARTS / UPGRADES','#454c48','#e1dcc9'),
 'Load':('TINDAHAN','LOAD / GENERAL GOODS','#d9c28a','#4e382a'),
 'Clothing':('UKAY-UKAY','CLOTHES / ACCESSORIES','#635262','#e7dec7'),
 'Barber':('BARBER SHOP','HAIRCUTS','#ddd7c5','#583634'),
 'Pares':('PARES & MAMI','HOT MEALS','#e0cfaa','#633c2e'),
 'Pisonet':('PISONET','COMPUTER RENTAL / PRINT / COPY','#ddd5bb','#414d40'),
 'Bakery':('PANADERIA','PANDESAL / FRESH BREAD','#74533b','#e5d4ae'),
 'Hardware':('VULCANIZING','TYRES / TOOLS / REPAIRS','#d9d3c0','#35433e'),
}

def fit(text,max_width,size,font_name='WorkSans-Bold.ttf'):
    while True:
        font=ImageFont.truetype(str(FONT/font_name),size)
        if font.getlength(text)<=max_width or size<=16:return font
        size-=1

for name,(title,subtitle,bg,fg) in signs.items():
    image=Image.new('RGB',(1024,256),bg);draw=ImageDraw.Draw(image)
    if name in ['Bakery','Clothing','Repair']:
        draw.rectangle((13,13,1010,242),outline=fg,width=3)
    elif name in ['Print','Load']:
        draw.rectangle((0,174,1024,256),fill=fg)
    else:
        # Painted service boards use a broad header and quiet fasteners, not
        # identical framed plaques on every business in the street.
        for x,y in [(17,17),(1007,17),(17,239),(1007,239)]:
            draw.ellipse((x-3,y-3,x+3,y+3),fill=fg)
    title_font='DarumadropOne-Regular.ttf' if name in ['Bakery','Clothing'] else 'WorkSans-Bold.ttf'
    font=fit(title,930,99,title_font)
    draw.text((512,101),title,font=font,fill=fg,anchor='mm')
    if name in ['Bakery','Clothing','Repair']:draw.line((165,172,859,172),fill=fg,width=2)
    draw.text((512,212 if name in ['Print','Load'] else 205),subtitle,
              font=fit(subtitle,890,34,'WorkSans-Regular.ttf'),fill=bg if name in ['Print','Load'] else fg,anchor='mm')
    image.save(OUT/(name+'.png'))

image=Image.new('RGB',(600,400),'#d9d0b8');draw=ImageDraw.Draw(image)
draw.rectangle((12,12,587,387),outline='#6e3935',width=5)
for i,line in enumerate(['BAWAL','UMIHI DITO']):
    draw.text((300,110+i*150),line,font=fit(line,540,88),fill='#6e3935',anchor='mm')
image.save(OUT/'Bawal.png')

image=Image.new('RGBA',(1024,400),(0,0,0,0));draw=ImageDraw.Draw(image)
font=fit('TARA, LARO!',940,148,'DarumadropOne-Regular.ttf')
draw.text((512,181),'TARA, LARO!',font=font,fill='#654b66',stroke_width=5,stroke_fill='#cdbf9c',anchor='mm')
draw.line((110,300,842,274),fill='#654b66',width=15)
draw.line((842,274,916,292),fill='#654b66',width=11)
image.save(OUT/'LaroGraffiti.png')

source=ROOT/'MapSource/environment/signs';source.mkdir(parents=True,exist_ok=True)
(source/'street-signs.json').write_text(json.dumps(signs,indent=2)+'\n',encoding='utf-8')
print('Authored',len(signs)+2,'original sign faces.')
