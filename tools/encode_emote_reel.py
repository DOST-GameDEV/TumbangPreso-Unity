"""Encode the emote reel frames (`EmoteReel`) into one MP4 per hero and one of all of them.

    python tools/encode_emote_reel.py [--keep-frames]

Reads Logs/emote-reel/<hero>/f_00000.png ... (30 fps) and writes Logs/emote-reel/<hero>_emotes.mp4 and
Logs/emote-reel/all_heroes_emotes.mp4. The frame folders are deleted afterwards unless --keep-frames.
"""
import argparse
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
REEL = ROOT / 'Logs' / 'emote-reel'


def ffmpeg():
    found = shutil.which('ffmpeg')
    if found:
        return found
    try:
        import imageio_ffmpeg
        return imageio_ffmpeg.get_ffmpeg_exe()
    except ImportError as error:
        raise SystemExit('Add ffmpeg to PATH or pip install imageio-ffmpeg.') from error


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--keep-frames', action='store_true')
    args = parser.parse_args()
    tool = ffmpeg()
    made = []
    for folder in sorted(p for p in REEL.iterdir() if p.is_dir()):
        if not any(folder.glob('f_*.png')):
            continue
        out = REEL / f'{folder.name}_emotes.mp4'
        subprocess.run([tool, '-y', '-loglevel', 'error', '-framerate', '30', '-i', str(folder / 'f_%05d.png'),
                        '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '18', str(out)], check=True)
        made.append(out)
        print('wrote', out)
    if len(made) > 1:
        listing = REEL / 'reel_list.txt'
        listing.write_text(''.join(f"file '{m.as_posix()}'\n" for m in made))
        out = REEL / 'all_heroes_emotes.mp4'
        subprocess.run([tool, '-y', '-loglevel', 'error', '-f', 'concat', '-safe', '0', '-i', str(listing),
                        '-c', 'copy', str(out)], check=True)
        listing.unlink()
        print('wrote', out)
    if not args.keep_frames:
        for m in made:
            shutil.rmtree(REEL / m.name.replace('_emotes.mp4', ''), ignore_errors=True)


if __name__ == '__main__':
    main()
