"""Encode captured game frames using their measured real-time spacing.

A fixed20fps input would speed up a capture that dropped frames. This uses the
recorded timestamps instead, so review playback preserves ordinary action timing.
"""
import argparse
import csv
from pathlib import Path
import subprocess
import shutil

parser=argparse.ArgumentParser();parser.add_argument('folder');parser.add_argument('--ffmpeg')
parser.add_argument('--sampled-images',action='store_true',help='Metrics were recorded every frame, images intentionally sampled less often.')
args=parser.parse_args()
root=Path(args.folder).resolve();ffmpeg=args.ffmpeg or shutil.which('ffmpeg')
if not ffmpeg:
    try:
        import imageio_ffmpeg
        ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
    except ImportError as error:
        raise SystemExit('Provide --ffmpeg,add ffmpeg to PATH,or use the existing imageio_ffmpeg runtime.') from error
for timing in sorted(root.glob('*/frames.csv')):
    with timing.open(newline='') as handle:rows=list(csv.DictReader(handle))
    if len(rows)<2:raise ValueError('Nonzero motion coverage required: '+str(timing))
    times=[float(row['real_seconds']) for row in rows]
    if any(b<=a for a,b in zip(times,times[1:])):raise ValueError('Non-monotonic timestamps: '+str(timing))
    for label,folder in [(timing.parent.name if timing.parent.name.endswith('-frames') else 'observer',timing.parent),('owner',timing.parent/'owner')]:
        if not folder.exists():continue
        selected=rows
        if args.sampled_images:
            selected=[row for row in rows if any((folder/(f"{int(row['frame']):05d}"+ext)).exists() for ext in ('.jpg','.png'))]
            if len(selected)<2:raise ValueError('Need at least two deliberately sampled images: '+str(folder))
        times=[float(row['real_seconds']) for row in selected]
        listing=timing.parent/(label+'-timed-frames.txt')
        lines=[]
        for i,row in enumerate(selected):
            frame=folder/(f"{int(row['frame']):05d}.jpg")
            if not frame.exists():frame=folder/(f"{int(row['frame']):05d}.png")
            if not frame.exists():raise FileNotFoundError(frame)
            path=frame.as_posix().replace("'","'\\''")
            duration=times[i+1]-times[i] if i+1<len(times) else times[-1]-times[-2]
            lines.extend(["file '"+path+"'",f'duration {duration:.6f}'])
        lines.append("file '"+path+"'");listing.write_text('\n'.join(lines)+'\n',encoding='utf-8')
        output=timing.parent/(label+'-ordinary-speed.mp4')
        command=[ffmpeg,'-y','-hide_banner','-loglevel','error','-f','concat','-safe','0','-i',str(listing)]
        sound=timing.parent/'game-audio.wav'
        if sound.exists():
            # Engine output only, with the captured callback/first-frame clocks.
            # This is review sync, not sample-accurate latency certification.
            info=dict(line.split('=',1) for line in (timing.parent/'game-audio.txt').read_text().splitlines() if '=' in line)
            origin=float((timing.parent/'capture-start.txt').read_text())
            trim=origin+times[0]-float(info['first_callback_real'])
            command+=['-i',str(sound),'-map','0:v:0','-map','1:a:0','-af',
                (f'atrim=start={trim:.6f},asetpts=PTS-STARTPTS' if trim>=0 else f'adelay={-trim*1000:.3f}:all=1'),
                '-c:a','aac','-b:a','192k','-shortest']
            output=timing.parent/(label+'-ordinary-speed-with-audio.mp4')
        command+=['-fps_mode','vfr','-c:v','libx264','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(output)]
        subprocess.run(command,check=True)
        print(output, 'images',len(selected),'metric rows',len(rows),'recorded seconds',round(times[-1]-times[0],3))
