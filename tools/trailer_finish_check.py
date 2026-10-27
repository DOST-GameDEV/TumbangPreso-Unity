"""Extract ordered final-movie evidence without opening a player or waking the display."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[1]
MEDIA=ROOT/'Logs/trailer/bring-it-home'


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('tag')
    parser.add_argument('--sweep-fps',type=int,choices=(1,2,3,5),help='Additional ordered review samples over the entire picture')
    args=parser.parse_args()
    work=MEDIA/'edits'/args.tag;witness=json.loads((work/'picture-witness.json').read_text())
    movie=Path(witness['master'])
    if hashlib.sha256(movie.read_bytes()).hexdigest()!=witness['sha256']:raise ValueError('Picture differs from witness')
    qa=work/'qa';qa.mkdir(exist_ok=True)
    overview=list(range(0,3600,120))+[3599]
    pairs=[max(0,n) for cut in witness['cuts'] for n in (cut['master_from']-1,cut['master_from'])]
    windows={'opening-contact':list(range(108,151,3)), 'pickup':list(range(2877,2916,3)),
        'escape':list(range(3000,3091,6)), 'identity':list(range(3354,3391,3))}
    sweep=list(range(0,3600,30//args.sweep_fps)) if args.sweep_fps else []
    selection=sorted(set(overview+pairs+sum(windows.values(),[])+sweep))
    def balanced(terms):
        if len(terms)==1:return terms[0]
        middle=len(terms)//2
        return '('+balanced(terms[:middle])+'+'+balanced(terms[middle:])+')'
    expression='select='+balanced([f'eq(n\\,{n})' for n in selection])
    subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-i',str(movie),'-vf',expression,
        '-fps_mode','vfr','-q:v','2',str(qa/'frame_%04d.jpg')],check=True)
    pictures={frame:qa/f'frame_{i+1:04d}.jpg' for i,frame in enumerate(selection)}
    font=ImageFont.truetype('C:/Windows/Fonts/consola.ttf',17) if Path('C:/Windows/Fonts/consola.ttf').exists() else ImageFont.load_default()
    def sheet(name,frames,columns=3):
        canvas=Image.new('RGB',(480*columns,((len(frames)+columns-1)//columns)*297),(18,18,20));draw=ImageDraw.Draw(canvas)
        for i,frame in enumerate(frames):
            picture=Image.open(pictures[frame]).resize((480,270),Image.Resampling.LANCZOS)
            x=i%columns*480;y=i//columns*297;canvas.paste(picture,(x,y))
            draw.text((x+8,y+273),f'{frame:04}  {frame/30:07.3f}s',font=font,fill='white')
        canvas.save(qa/(name+'.jpg'),quality=94)
    for number,start in enumerate((0,1200,2400),1):sheet(f'overview-{number:02}',[n for n in overview if start<=n<start+1200])
    for name,frames in windows.items():sheet(name,frames)
    for index in range(0,len(pairs),18):sheet(f'cut-boundaries-{index//18+1:02}',pairs[index:index+18],2)
    for index in range(0,len(sweep),12):sheet(f'sweep-{index//12+1:02}',sweep[index:index+12])
    report={'picture_sha256':witness['sha256'],'selected_frames':selection,
        'whole_picture_sweep_fps':args.sweep_fps,'whole_picture_sweep_frames':sweep,
        'evidence':'Ordered decoded samples, contact windows and both sides of cut boundaries',
        'limits':'These files support visual sequence inspection. No UI playback or owner creative acceptance is claimed.'}
    (qa/'review-evidence.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(f'Extracted {len(selection)} final-picture frames to {qa}')


if __name__=='__main__':main()
