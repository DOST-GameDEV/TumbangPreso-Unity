"""Cut a live ultimate recording to its trailer window and deliver it.

  python tools/cut_trailer_shot.py Logs/trailer/35_rago-mayon-awakens --events
  python tools/cut_trailer_shot.py Logs/trailer/35_rago-mayon-awakens --zero 40 --frames 90 --views owner@0,wide@31 --deliver DIR

`TrailerShots.RecordUltimate` (Assets/TumbangPreso/Tests/PlayMode/TrailerShots.cs) writes the folder: owner/ and wide/
(frame_00000.png onward, 1920x1080, 60 fps of game time from `Lead` seconds before the press), events.csv (per frame:
seconds from the press, whether the cutscene's picture is up, the ultimate charge, and each rival's height, speed and
status) and shot.txt.

  --events          print the frames worth cutting on: the cutscene up and down, each rival's first status change and
                    first fast move. Nothing is written.
  --zero N          the recorded frame that becomes the shot's frame 0 (the prompt's count).
  --frames N        the shot's length in frames, handles excluded.
  --handles N       frames added either side (default 12, docs/TRAILER_PROMPTS.md).
  --views SPEC      which view plays from which SHOT frame on: "owner@0,wide@31" plays owner/ from the first frame
                    (the in handle included) and wide/ from shot frame 31 on. Default: wide throughout.
  --deliver DIR     write DIR/shot-NN.mov: picture only, ProRes 422 HQ, 60 fps, handles included.

Writes cut/frame_00000.png onward and cut.json (the source of every output frame) beside the recording.
Needs ffmpeg on PATH.
"""
import argparse
import csv
import json
import re
import shutil
import subprocess
from pathlib import Path

FPS = 60


def ffmpeg():
    exe = shutil.which("ffmpeg")
    if not exe:
        raise SystemExit("ffmpeg is not on PATH")
    return exe


def print_events(folder):
    rows = list(csv.DictReader(open(folder / "events.csv", newline="", encoding="utf-8")))
    if not rows:
        raise SystemExit("events.csv is empty")
    rivals = sorted({k.split("_")[0] for k in rows[0] if re.match(r"r\d+_", k)})
    press = next((int(r["frame"]) for r in rows if float(r["seconds_from_press"]) >= 0.0), 0)
    print(f"{len(rows)} frames; the press on frame {press}")
    last_up = "0"
    for r in rows:
        if r["scene_up"] != last_up:
            print(f"  frame {r['frame']:>4} ({float(r['seconds_from_press']):+.3f} s): cutscene {'UP' if r['scene_up'] == '1' else 'down'}")
            last_up = r["scene_up"]
    for column, label in (("phase_up", "shared phase"), ("ult_active", "ultimate active"), ("phase_shot", "cutscene shot"), ("probe", "probe")):
        if column not in rows[0]:
            continue
        last = rows[0][column]
        for r in rows[1:]:
            if r[column] != last:
                print(f"  frame {r['frame']:>4} ({float(r['seconds_from_press']):+.3f} s): {label} {last} -> {r[column]}")
                last = r[column]
    spent = next((r for r in rows if float(r["ult_charge"]) < float(rows[0]["ult_charge"]) - 1), None)
    if spent:
        print(f"  frame {spent['frame']:>4} ({float(spent['seconds_from_press']):+.3f} s): charge spent")
    for rv in rivals:
        last = "-"
        moved = False
        for r in rows:
            s = r[f"{rv}_status"]
            if s != last:
                print(f"  frame {r['frame']:>4} ({float(r['seconds_from_press']):+.3f} s): {rv} {last} -> {s}")
                last = s
            if not moved and float(r[f"{rv}_speed"]) > 3.0 and float(r["seconds_from_press"]) >= 0.0:
                print(f"  frame {r['frame']:>4} ({float(r['seconds_from_press']):+.3f} s): {rv} first moves fast "
                      f"({float(r[f'{rv}_speed']):.1f} m/s, y {float(r[f'{rv}_y']):.2f})")
                moved = True
        peak = max(rows, key=lambda r: float(r[f"{rv}_y"]))
        print(f"  {rv}: highest y {float(peak[f'{rv}_y']):.2f} on frame {peak['frame']}")


def parse_views(spec):
    out = []
    for part in spec.split(","):
        view, _, at = part.strip().partition("@")
        out.append((int(at or "-100000"), view))
    out.sort()
    return out


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("folder")
    parser.add_argument("--events", action="store_true")
    parser.add_argument("--zero", type=int)
    parser.add_argument("--frames", type=int)
    parser.add_argument("--handles", type=int, default=12)
    parser.add_argument("--views", default="wide@0")
    parser.add_argument("--number", type=int)
    parser.add_argument("--deliver", default="")
    args = parser.parse_args()
    folder = Path(args.folder).resolve()
    if args.events:
        print_events(folder)
        return
    if args.zero is None or args.frames is None:
        raise SystemExit("--zero and --frames are needed to cut")
    number = args.number if args.number is not None else int(re.match(r"(\d+)", folder.name).group(1))
    views = parse_views(args.views)
    first_view = views[0][1]

    cut = folder / "cut"
    if cut.exists():
        shutil.rmtree(cut)
    cut.mkdir()
    sources = []
    for i, shot_frame in enumerate(range(-args.handles, args.frames + args.handles)):
        view = first_view
        for at, v in views:
            if shot_frame >= at:
                view = v
        src = folder / view / f"frame_{args.zero + shot_frame:05d}.png"
        if not src.exists():
            raise SystemExit(f"shot frame {shot_frame} needs {src}, which is not on disk")
        shutil.copyfile(src, cut / f"frame_{i:05d}.png")
        sources.append({"shot_frame": shot_frame, "view": view, "recorded_frame": args.zero + shot_frame})
    receipt = {"number": number, "zero": args.zero, "frames": args.frames, "handles": args.handles, "fps": FPS,
               "views": args.views, "sources": sources}
    (folder / "cut.json").write_text(json.dumps(receipt, indent=1), encoding="utf-8")
    print(f"cut {len(sources)} frames ({args.frames} + 2 x {args.handles} handles) from recorded frame {args.zero - args.handles}")

    if args.deliver:
        deliver = Path(args.deliver)
        deliver.mkdir(parents=True, exist_ok=True)
        mov = deliver / f"shot-{number:02d}.mov"
        subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", str(cut / "frame_%05d.png"),
                        "-c:v", "prores_ks", "-profile:v", "3", "-pix_fmt", "yuv422p10le", "-vendor", "apl0", "-r", str(FPS),
                        "-an", str(mov)], check=True)
        print(f"delivered {mov}")


if __name__ == "__main__":
    main()
