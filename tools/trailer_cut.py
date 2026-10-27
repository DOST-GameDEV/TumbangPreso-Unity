"""Build the silent 120-second trailer with strict cinematic/gameplay source boundaries."""
import argparse
import csv
import hashlib
import json
from pathlib import Path
import re
import subprocess

from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parents[1]
MEDIA=ROOT/"Logs/trailer/bring-it-home"
FONT=ROOT/"Assets/TumbangPreso/Resources/UI/fonts/PaalalabasDisplayWide.otf"
LOGO=ROOT/"Assets/TumbangPreso/Resources/UI/brand/tump_logo.png"
FPS=30
ENCODE=["-c:v","libx264","-threads","4","-preset","medium","-crf","17","-pix_fmt","yuv420p",
    "-r","30","-g","60","-keyint_min","60","-sc_threshold","0","-color_primaries","bt709",
    "-color_trc","bt709","-colorspace","bt709","-video_track_timescale","90000","-movflags","+faststart","-an"]


def run(command,**kwargs):
    subprocess.run(command,check=True,**kwargs)


def probe(path,count=False):
    command=["ffprobe","-v","error"]
    if count:command += ["-count_frames"]
    return json.loads(subprocess.check_output(command+["-show_streams","-show_format","-of","json",str(path)],text=True))


def check_movie(path,frames):
    data=probe(path,True)
    if any(s["codec_type"]=="audio" for s in data["streams"]):raise ValueError("Silent picture contains audio: "+str(path))
    video=next(s for s in data["streams"] if s["codec_type"]=="video")
    if int(video["nb_read_frames"])!=frames:raise ValueError("Wrong encoded frame count: "+str(path))
    if abs(float(data["format"]["duration"])-frames/FPS)>.04:raise ValueError("Encoded duration drift: "+str(path))
    return data


def stamp(frame):
    return f"{frame//108000:02}:{frame//1800%60:02}:{frame//30%60:02}:{frame%30:02}"


def title_frames(folder):
    folder.mkdir(parents=True,exist_ok=True)
    image=Image.open(LOGO).convert("RGBA");box=image.getchannel("A").getbbox()
    if box is None:raise ValueError("TUMP logo is empty")
    image=image.crop(box)
    font=ImageFont.truetype(str(FONT),66)
    for frame in range(240):
        t=frame/FPS
        u=min(1,t/.28);v=u-1;ease=1+2.2*v*v*v+1.2*v*v
        scale=.88+.12*ease
        width=round(1160*scale);height=round(width*image.height/image.width)
        logo=image.resize((width,height),Image.Resampling.LANCZOS)
        canvas=Image.new("RGBA",(1920,1080),(24,20,20,255))
        y=round(442-height/2+12*(1-u))
        canvas.alpha_composite(logo,(round((1920-width)/2),y))
        lettering=Image.new("RGBA",(1920,1080),(0,0,0,0));draw=ImageDraw.Draw(lettering)
        alpha=round(255*max(0,min(1,(t-.4)/.25)))
        draw.text((960,930),"YOUR TURN.",font=font,anchor="mm",fill=(244,228,188,alpha),stroke_width=0)
        canvas.alpha_composite(lettering)
        canvas.convert("RGB").save(folder/f"frame_{frame:05d}.png",compress_level=3)


def label_image(text,path):
    canvas=Image.new("RGBA",(1920,1080),(0,0,0,0))
    draw=ImageDraw.Draw(canvas);font=ImageFont.truetype(str(FONT),52)
    draw.text((76,66),text,font=font,fill=(248,235,202,255),stroke_width=4,stroke_fill=(22,20,18,230))
    canvas.save(path)


def source(cut,work):
    kind=cut["kind"]
    if kind=="graphics":
        if cut.get("source")!="title":raise ValueError("Unknown end graphics")
        folder=work/"title-frames"
        if len(list(folder.glob("frame_*.png")))!=240:title_frames(folder)
        return folder,"png",0,None
    tag=cut["take"]
    if not re.fullmatch(r"[A-Za-z0-9_-]+",tag):raise ValueError("Unsafe take name")
    take=MEDIA/tag
    meta=json.loads((take/"capture.json").read_text(encoding="utf-8"))
    expected="original-cinema" if kind=="cinema" else "native-gameplay"
    if meta["kind"]!=expected or meta["probe"]:raise ValueError("Wrong material in "+cut["id"])
    if (meta["width"],meta["height"],meta["fps"])!=(1920,1080,FPS):raise ValueError("Final sources must be genuine 1080p30")
    start=cut.get("start_frame",0);frames=cut["frames"]
    if start<0 or start+frames>meta["frames"]:raise ValueError("Source range outside take")
    if kind=="gameplay":
        if not meta.get("external_visibility_scope"):raise ValueError("Native capture predates clean visibility/marker qualification")
        if cut["view"] not in ("fpv","line","side"):raise ValueError("Unknown native view")
        rows=list(csv.DictReader((take/"telemetry.tsv").open(encoding="utf-8"),delimiter="\t"))[start:start+frames]
        if len(rows)!=frames or any(r["held_clock"]!="0" or r["fpv_valid"]!="1" for r in rows):
            raise ValueError("Native range contains a held/cutscene/unqualified player-view frame")
        folder=take/cut["view"]
    else:folder=take
    return folder,"jpg",start,meta


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--edl",type=Path,default=ROOT/"docs/trailer/PICTURE_EDL.json")
    parser.add_argument("--tag",required=True)
    parser.add_argument("--no-share",action="store_true")
    parser.add_argument("--title-only",action="store_true",help="Render and validate the final 8-second brand card")
    args=parser.parse_args()
    if not re.fullmatch(r"[A-Za-z0-9_-]+",args.tag):parser.error("Unsafe output tag")
    work=MEDIA/"edits"/args.tag;clips=work/"clips";clips.mkdir(parents=True,exist_ok=True)
    if args.title_only:
        folder=work/"title-frames";title_frames(folder)
        output=work/"title-review.mp4"
        run(["ffmpeg","-hide_banner","-loglevel","error","-y","-framerate","30","-i",str(folder/"frame_%05d.png"),
            "-vf","scale=out_range=tv:out_color_matrix=bt709,setsar=1","-frames:v","240",*ENCODE,str(output)])
        check_movie(output,240);print("Validated 8-second brand card: "+str(output));return
    edl=json.loads(args.edl.read_text(encoding="utf-8-sig"))
    position=0;witness=[];ids=set()
    for cut in edl["cuts"]:
        identity=cut["id"]
        if not re.fullmatch(r"[A-Za-z0-9_-]+",identity) or identity in ids:raise ValueError("Invalid/duplicate cut ID")
        ids.add(identity)
        frames=cut["frames"]
        if not isinstance(frames,int) or frames<=0:raise ValueError("Invalid cut length")
        end=position+frames
        expected="cinema" if position<600 or 2640<=position<3360 else "gameplay" if position<2640 else "graphics"
        boundary=600 if position<600 else 2640 if position<2640 else 3360 if position<3360 else 3600
        if cut["kind"]!=expected or end>boundary:raise ValueError("Cinematic/gameplay block boundary violation at "+identity)
        folder,extension,start,meta=source(cut,work)
        pattern=folder/f"frame_%05d.{extension}"
        for n in range(start,start+frames):
            if not (folder/f"frame_{n:05d}.{extension}").is_file():raise ValueError("Missing source frame")
        output=clips/f"{identity}.mp4"
        label=cut.get("label")
        filters="scale=in_range=pc:out_range=tv:out_color_matrix=bt709,setsar=1"
        if extension=="jpg":filters="scale=in_range=pc:out_range=tv:in_color_matrix=bt601:out_color_matrix=bt709,setsar=1"
        signature=hashlib.sha256(json.dumps({"cut":cut,"meta":meta,"filters":filters,"encode":ENCODE,
            "first":hashlib.sha256((folder/f"frame_{start:05d}.{extension}").read_bytes()).hexdigest(),
            "last":hashlib.sha256((folder/f"frame_{start+frames-1:05d}.{extension}").read_bytes()).hexdigest(),
            "logo":hashlib.sha256(LOGO.read_bytes()).hexdigest() if extension=="png" else None},sort_keys=True).encode()).hexdigest()
        receipt=clips/f"{identity}.json"
        cached=output.exists() and receipt.exists() and json.loads(receipt.read_text()).get("signature")==signature
        if not cached:
            command=["ffmpeg","-hide_banner","-loglevel","error","-y","-framerate","30","-start_number",str(start),"-i",str(pattern)]
            if label:
                path=work/f"{identity}-label.png";label_image(label,path)
                command += ["-i",str(path),"-filter_complex",f"[0:v]{filters}[base];[base][1:v]overlay=0:0:enable='lt(n,48)'[picture]","-map","[picture]"]
            else:command += ["-vf",filters]
            command += ["-frames:v",str(frames),*ENCODE,str(output)]
            run(command)
            check_movie(output,frames)
            receipt.write_text(json.dumps({"signature":signature,"cut":cut},indent=2),encoding="utf-8")
        witness.append({**cut,"master_from":position,"master_to":end,"timecode":stamp(position),
            "encoded":output.name,"sha256":hashlib.sha256(output.read_bytes()).hexdigest()})
        print(f"{identity}: {stamp(position)} - {stamp(end)} ({cut['kind']})",flush=True)
        position=end
    if position!=3600:raise ValueError(f"Trailer must have 3600 frames; got {position}")
    concat=clips/"concat.txt"
    concat.write_text("\n".join(f"file '{item['encoded']}'" for item in witness)+"\n",encoding="utf-8")
    master=MEDIA/f"{args.tag}.mp4"
    run(["ffmpeg","-hide_banner","-loglevel","error","-y","-f","concat","-safe","1","-i",str(concat),"-map","0:v:0","-c","copy","-an","-movflags","+faststart",str(master)])
    details=check_movie(master,3600)
    run(["ffmpeg","-hide_banner","-loglevel","error","-i",str(master),"-f","null","-"])
    report={"master":str(master),"frames":3600,"duration":120,"audio_streams":0,"block_frames":[0,600,2640,3360,3600],
        "sha256":hashlib.sha256(master.read_bytes()).hexdigest(),"cuts":witness,"probe":details,"full_decode":"passed"}
    if not args.no_share:
        share=MEDIA/f"{args.tag}_share.mp4"
        run(["ffmpeg","-hide_banner","-loglevel","error","-y","-i",str(master),"-vf","scale=1280:720:flags=lanczos",
            "-c:v","libx264","-threads","4","-preset","medium","-crf","22","-pix_fmt","yuv420p","-an","-movflags","+faststart",str(share)])
        check_movie(share,3600);report["share"]=str(share)
    (work/"picture-witness.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
    print(json.dumps({k:v for k,v in report.items() if k not in ("cuts","probe")},indent=2))


if __name__=="__main__":main()
