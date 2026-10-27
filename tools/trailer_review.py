"""Encode silent trailer takes and build time-labelled framing/sequence evidence."""
import argparse
import json
from pathlib import Path
import subprocess

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("tag")
    parser.add_argument("--encode", action="store_true")
    parser.add_argument("--step", type=float, default=1.0, help="Seconds between review frames for a motion take")
    parser.add_argument("--view", choices=("fpv", "line", "side", "follow"), default="fpv", help="Native gameplay camera")
    parser.add_argument("--start", type=float)
    parser.add_argument("--end", type=float)
    args = parser.parse_args()
    if not args.tag.replace("-", "").replace("_", "").isalnum():
        parser.error("Unsafe tag")
    take = ROOT / "Logs/trailer/bring-it-home" / args.tag
    meta = json.loads((take / "capture.json").read_text(encoding="utf-8"))
    native = meta["kind"] == "native-gameplay"
    frame_folder = take / args.view if native else take
    suffix = "-" + args.view if native else ""
    frames = sorted(frame_folder.glob("frame_*.jpg"))
    if len(frames) != meta["frames"]:
        raise SystemExit(f"Incomplete take: {len(frames)} of {meta['frames']} frames")
    times = [float(line.split("\t")[1]) for line in (take / "frame-times.tsv").read_text(encoding="utf-8").splitlines()]
    selection = list(range(len(frames))) if meta["probe"] else sorted(set([0, len(frames)-1, *range(0, len(frames), max(1, round(meta["fps"]*args.step)))]))
    if args.start is not None or args.end is not None:
        selection = [i for i in selection if (args.start is None or times[i] >= args.start) and (args.end is None or times[i] <= args.end)]
    if not selection:
        raise SystemExit("Review window contains no selected frames")
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 17)
    except OSError:
        font = ImageFont.load_default()
    thumb_width = 480
    thumb_height = round(thumb_width * meta["height"] / meta["width"])
    columns = 3
    sheet = Image.new("RGB", (thumb_width*columns, ((len(selection)+columns-1)//columns)*(thumb_height+27)), (18,18,20))
    draw = ImageDraw.Draw(sheet)
    for n, index in enumerate(selection):
        image = Image.open(frames[index]).convert("RGB")
        image.thumbnail((thumb_width, thumb_height))
        x, y = (n%columns)*thumb_width, (n//columns)*(thumb_height+27)
        sheet.paste(image, (x,y))
        draw.text((x+8,y+thumb_height+3), f"{index:05d}   director {times[index]:06.2f}s", font=font, fill=(235,235,235))
    sheet_name = f"review{suffix}-sheet.jpg"
    sheet.save(take / sheet_name, quality=95)
    summaries = []
    for path in frames:
        small = np.asarray(Image.open(path).convert("RGB").resize((96,54)), dtype=float)
        summaries.append({"frame":path.name,"mean":round(float(small.mean()),3),"std":round(float(small.std()),3)})
    report = {"take":args.tag,"capture":meta,"frame_count":len(frames),"view":args.view if native else "cinema","review_sheet":sheet_name,
        "near_black_frames":[s["frame"] for s in summaries if s["mean"]<3 and s["std"]<3],
        "limits":"Contact sheet and frame statistics support framing/blank-frame review, not motion or creative acceptance."}
    if args.encode:
        if meta["probe"]:
            raise SystemExit("A sparse probe is not a motion take. Do not encode it as a false continuous film.")
        movie = take.parent / f"{args.tag}{suffix}.mp4"
        subprocess.run(["ffmpeg","-hide_banner","-loglevel","error","-y","-framerate",str(meta["fps"]),
            "-i",str(frame_folder/"frame_%05d.jpg"),"-an","-c:v","libx264","-preset","medium","-crf","17",
            "-pix_fmt","yuv420p","-movflags","+faststart",str(movie)], check=True)
        details = json.loads(subprocess.check_output(["ffprobe","-v","error","-show_streams","-show_format","-of","json",str(movie)], text=True))
        if any(stream["codec_type"] == "audio" for stream in details["streams"]):
            raise SystemExit("Silent picture unexpectedly contains an audio stream")
        subprocess.run(["ffmpeg","-hide_banner","-loglevel","error","-i",str(movie),"-f","null","-"],check=True)
        report.update(movie=str(movie), encoded=details, full_file_decode="passed", audio_streams=0)
    (take/f"review{suffix}-report.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
    print(json.dumps({key:value for key,value in report.items() if key!="encoded"},indent=2))


if __name__ == "__main__":
    main()
