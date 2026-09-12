"""Encode captured game frames using their measured real-time spacing.

A fixed20fps input would speed up a capture that dropped frames. This uses the
recorded timestamps instead, so review playback preserves ordinary action timing.
"""
import argparse
import csv
from pathlib import Path
import subprocess
import shutil

parser=argparse.ArgumentParser();parser.add_argument('folder');parser.add_argument('--ffmpeg');args=parser.parse_args()
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
    for label,folder in [('observer',timing.parent),('owner',timing.parent/'owner')]:
        if not folder.exists():continue
        listing=timing.parent/(label+'-timed-frames.txt')
        lines=[]
        for i,row in enumerate(rows):
            frame=folder/(f"{int(row['frame']):05d}.jpg")
            if not frame.exists():raise FileNotFoundError(frame)
            path=frame.as_posix().replace("'","'\\''")
            duration=times[i+1]-times[i] if i+1<len(times) else times[-1]-times[-2]
            lines.extend(["file '"+path+"'",f'duration {duration:.6f}'])
        lines.append("file '"+path+"'");listing.write_text('\n'.join(lines)+'\n',encoding='utf-8')
        output=timing.parent/(label+'-ordinary-speed.mp4')
        subprocess.run([ffmpeg,'-y','-hide_banner','-loglevel','error','-f','concat','-safe','0','-i',str(listing),
            '-fps_mode','vfr','-c:v','libx264','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(output)],check=True)
        print(output, 'frames',len(rows),'recorded seconds',round(times[-1]-times[0],3))
