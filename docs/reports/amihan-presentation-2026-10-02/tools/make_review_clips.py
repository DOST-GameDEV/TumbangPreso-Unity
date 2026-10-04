"""Silent evidence clips for the Airburst films: a labelled 2x2 grid played twice at normal speed, then a
labelled slow-motion section. No audio is added (the assignment excludes all SFX work; audio is not evaluated).

usage: python make_review_clips.py <film dir> <out.mp4> "<title>" [slow_from_s slow_to_s] [--slow 0.4]
"""
import os, subprocess, sys, tempfile, shutil

FFMPEG = r"C:\Users\matth\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0-full_build\bin\ffmpeg.exe"
FONT = "C\\:/Windows/Fonts/arialbd.ttf"
VIEWS = [("owner", "HER SCREEN (cutscene is the shared overlay)"), ("observer", "OBSERVER OUTSIDE THE FAN"),
         ("victim", "PLAYER INSIDE THE FAN"), ("wide", "THE COURT")]


def esc(text):
    return text.replace("\\", "\\\\").replace(":", "\\:").replace("'", "\u2019").replace(",", "\\,")


def run(args):
    r = subprocess.run([FFMPEG, "-hide_banner", "-loglevel", "error", "-y", *args], capture_output=True, text=True)
    if r.returncode != 0:
        raise SystemExit(r.stderr)


def grid(film, out, banner, start=None, end=None, speed=1.0):
    inputs, filters = [], []
    skip = os.environ.get("SKIP_VIEW", "")
    for i, (view, label) in enumerate(VIEWS):
        if view == skip:
            seconds = (end - start) if start is not None else len(os.listdir(os.path.join(film, "owner"))) / 30.0
            inputs += ["-f", "lavfi", "-t", f"{seconds:.3f}", "-i", "color=c=0x202020:s=1280x720:r=30"]
            label = os.environ.get("SKIP_LABEL", "view unavailable in this run")
            filters.append(f"[{i}:v]scale=640:360,drawtext=fontfile='{FONT}':text='{esc(label)}':x=(w-text_w)/2:y=170:fontsize=18:fontcolor=white[v{i}]")
            continue
        inputs += ["-framerate", "30", "-i", os.path.join(film, view, "%05d.jpg")]
        trim = ""
        if start is not None:
            trim = f"trim=start={start}:end={end},setpts=PTS-STARTPTS,"
        filters.append(f"[{i}:v]{trim}scale=640:360,drawbox=x=0:y=0:w=640:h=30:color=black@0.55:t=fill,"
                       f"drawtext=fontfile='{FONT}':text='{esc(label)}':x=10:y=7:fontsize=17:fontcolor=white[v{i}]")
    stack = "".join(f"[v{i}]" for i in range(4)) + "xstack=inputs=4:layout=0_0|640_0|0_360|640_360[g]"
    tail = (f"[g]drawtext=fontfile='{FONT}':text='film time %{{pts\:hms\:{start or 0}}}':x=1040:y=696:fontsize=18:fontcolor=white,"
            f"setpts=PTS/{speed},drawbox=x=0:y=690:w=1030:h=30:color=black@0.7:t=fill,"
            f"drawtext=fontfile='{FONT}':text='{esc(banner)}':x=12:y=696:fontsize=17:fontcolor=yellow,fps=30[o]")
    run([*inputs, "-filter_complex_threads", "1", "-filter_complex", ";".join(filters) + ";" + stack + ";" + tail, "-map", "[o]",
         "-c:v", "libx264", "-threads", "2", "-pix_fmt", "yuv420p", "-crf", "26", "-preset", "medium", out])


def main():
    film, out, title = sys.argv[1], sys.argv[2], sys.argv[3]
    slow = None
    if len(sys.argv) >= 6:
        slow = (float(sys.argv[4]), float(sys.argv[5]))
    factor = 0.4
    tmp = tempfile.mkdtemp()
    try:
        parts = []
        for n in (1, 2):
            p = os.path.join(tmp, f"normal{n}.mp4")
            grid(film, p, f"{title}  |  NORMAL SPEED  pass {n} of 2  |  silent, audio not evaluated")
            parts.append(p)
        if slow:
            p = os.path.join(tmp, "slow.mp4")
            grid(film, p, f"{title}  |  SLOW MOTION {factor}x  {slow[0]:.1f} to {slow[1]:.1f} s  |  silent",
                 start=slow[0], end=slow[1], speed=factor)
            parts.append(p)
        listing = os.path.join(tmp, "list.txt")
        with open(listing, "w") as fh:
            for p in parts:
                fh.write(f"file '{p}'\n")
        run(["-f", "concat", "-safe", "0", "-i", listing, "-c", "copy", out])
    finally:
        assert os.path.commonpath([os.path.realpath(tmp), os.path.realpath(tempfile.gettempdir())]) == os.path.realpath(tempfile.gettempdir()), "Temporary cleanup path escaped temp root"
        shutil.rmtree(tmp, ignore_errors=True)
    print(out)


if __name__ == "__main__":
    main()
