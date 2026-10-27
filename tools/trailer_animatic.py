"""Cut the trailer animatic from the rendered in-engine shots, existing film clips and title cards.

  py -3 tools/trailer_animatic.py            # writes Logs/trailer/animatic_v<N>.mp4 and a contact sheet
  py -3 tools/trailer_animatic.py --version 2

Inputs: Logs/trailer/frames/<film>/frame_####.png + shots.txt (TrailerRecording PlayMode tests),
Logs/map-films/*_intro_film.mp4 for placeholder shots, fonts and sounds from Assets/.
The edit decision list (EDL) and the sound cues below are the animatic; docs/trailer/ANIMATIC.md explains them.
All compositing (aspect ratio, grade, grain, cards) is done here per frame so it can animate.
"""
import argparse
import math
import os
import shutil
import subprocess
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

W, H, FPS = 1920, 1080, 24
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Logs", "trailer")
FRAMES = os.path.join(OUT, "frames")
WORK = os.path.join(OUT, "work")
DISPLAY = os.path.join(ROOT, "Assets/TumbangPreso/Resources/UI/fonts/DarumadropOne-Regular.ttf")
BODY = os.path.join(ROOT, "Assets/TumbangPreso/Resources/UI/fonts/WorkSans-Bold.ttf")
AMB = "Assets/TumbangPreso/Art/audio/ambience/"
SFX = "Assets/TumbangPreso/Resources/Sfx/"
MUSIC = "Assets/TumbangPreso/Resources/Music/"

# Brand colours, from tump_logo.png.
INK = (22, 8, 12)
MAROON = (140, 18, 38)
CREAM = (240, 226, 214)
LIME = (187, 208, 71)
GOLD = (233, 182, 72)

PILLAR = 240                               # 4:3 inside 16:9
SCOPE = int(round((H - W / 2.39) / 2))     # 2.39:1 bars

# ---------------------------------------------------------------------------------------------- the edit
# (id, source, look, duration, extra). Sources: ("film", name, shot, offset) | ("clip", mp4, start) | ("card", lines) | ("black",)
# Sound cues refer to a segment by its id, so segments can be added, cut or reordered freely.
EDL = [
    # ACT 1 · THE THROW
    ("open", ("black",), "black", 1.0, {}),
    ("can", ("film", "street", "can", 0.5), "street", 1.6, {}),
    ("kid", ("film", "street", "kid", 0.3), "street", 1.9, {}),
    ("windup", ("film", "street", "windup", 0.15), "street", 0.95, {}),
    ("throw", ("film", "street", "throw", 1.1), "street", 2.0, {}),
    ("spin", ("film", "arena", "spin", 0.0), "widen", 1.6, {}),
    ("crane", ("film", "arena", "crane", 0.6), "scope", 2.2, {}),
    ("crack", ("film", "arena", "crack", 0.05), "scope", 1.15, {}),
    ("card1", ("card", ["IN THIS WORLD,", "THE BIGGEST GAME ON EARTH"]), "card", 2.2, {}),
    ("card2", ("card", ["STARTED ON OUR STREETS."]), "card", 2.0, {"accent": True}),
    # ACT 2 · OUR WORLD
    ("archive", ("film", "street", "archive", 0.0), "newsreel", 3.0, {"caption": "PANDAIGDIG 1974  ·  MAYNILA"}),
    ("vhs", ("film", "street", "vhs", 0.0), "vhs", 2.5, {"caption": "JUN. 14 1996"}),
    ("train", ("clip", "Logs/map-films/Ermita_intro_film.mp4", 8.6), "placeholder", 2.0, {"tag": "PLACEHOLDER  ·  HERO-ART TRAIN WRAP COMES NEXT"}),
    ("jeep", ("clip", "Logs/map-films/BGC_intro_film.mp4", 3.0), "placeholder", 1.5, {"tag": "PLACEHOLDER  ·  \"RAGO\" JEEPNEY COMES NEXT"}),
    ("rack", ("film", "street", "rack", 0.0), "street", 2.0, {}),
    ("laugh", ("clip", "Logs/map-films/Tondo_intro_film.mp4", 12.5), "placeholder", 2.5, {"tag": "FROM THE TONDO MAP FILM  ·  RE-SHOOT WITHOUT HUD"}),
    ("elder", ("film", "street", "elder", 0.2), "street", 3.3, {}),
    ("card3", ("card", ["EVERY STREET HAS A COURT."]), "card", 1.6, {}),
    ("ident", ("card", ["PANDAIGDIG", "2026"]), "ident", 1.6, {}),
    ("card4", ("card", ["AND EVERY YEAR,", "THE WHOLE COUNTRY STOPS TO WATCH."]), "card", 2.2, {}),
    ("tv", ("film", "street", "tv", 0.0), "street", 5.2, {}),
]

# Sound: (file, at seconds, volume, options). The kid's "Taya!" is the only voice in Acts 1 and 2; the rest is
# guide music (TEMP: the team is writing the real score) and recorded effects from the game.
def cues(at, total):
    return [
        (AMB + "eskinita_street.wav", 0.0, 0.55, {"dur": at["spin"], "fade_out": 0.15}),
        (AMB + "alley_bed_kids.wav", 0.3, 0.35, {"dur": at["spin"] - 0.3, "fade_out": 0.15}),
        (AMB + "sfx_alley_rooster_crow_1.wav", 0.35, 0.35, {}),
        (AMB + "sfx_life_kid_taya_1.wav", at["kid"] + 0.35, 0.9, {}),
        (SFX + "throw_charge.wav", at["windup"] + 0.1, 0.5, {}),
        (SFX + "throw_whoosh.wav", at["windup"] + 0.85, 0.9, {}),
        (SFX + "sfx_arena_crowd_erupt.wav", at["spin"], 1.0, {"dur": 4.6, "fade_out": 0.8}),
        (SFX + "sfx_arena_crowd_bed_roar.wav", at["spin"] + 0.2, 0.6, {"dur": at["card1"] - at["spin"] - 0.2, "fade_out": 0.3}),
        (SFX + "lata_knockdown.wav", at["crack"] + 0.3, 1.0, {}),
        (MUSIC + "ost_menu.mp3", at["card1"] + 0.4, 0.55, {"dur": total - at["card1"] - 0.4, "fade_in": 1.2, "fade_out": 1.0, "tag": "TEMP GUIDE MUSIC"}),
        (AMB + "kanto_horn_jeepney_1.wav", at["jeep"] + 0.3, 0.45, {}),
        (AMB + "sfx_taho_call_1.wav", at["rack"] + 0.2, 0.35, {}),
        (AMB + "sfx_life_kid_giggle_2.wav", at["laugh"] + 0.4, 0.6, {}),
        (SFX + "sfx_arena_crowd_bed_lively.wav", at["tv"], 0.35, {"dur": total - at["tv"], "filter": "highpass=f=600,lowpass=f=3200", "fade_in": 0.5}),
        (AMB + "sfx_life_cheer_1.wav", at["tv"] + 3.5, 0.9, {}),
        (AMB + "sfx_life_cheer_2.wav", at["tv"] + 3.58, 0.8, {}),
        (SFX + "sfx_arena_crowd_cheer.wav", at["tv"] + 3.5, 0.45, {"filter": "highpass=f=600,lowpass=f=3200", "dur": 1.5, "fade_out": 0.4}),
    ]


# ---------------------------------------------------------------------------------------------- sources

_shots = {}


def film_frame(film, shot, t):
    if film not in _shots:
        table = {}
        with open(os.path.join(FRAMES, film, "shots.txt"), encoding="utf-8") as f:
            for line in f:
                name, a, b = line.strip().split("\t")
                table[name] = (int(a), int(b))
        _shots[film] = table
    a, b = _shots[film][shot]
    i = min(b, a + int(round(t * FPS)))
    return Image.open(os.path.join(FRAMES, film, f"frame_{i:04d}.png")).convert("RGB")


def clip_frames(mp4, start, dur, key):
    folder = os.path.join(WORK, "clip_" + key)
    if not os.path.isdir(folder) or not os.listdir(folder):
        os.makedirs(folder, exist_ok=True)
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-ss", f"{start:.3f}", "-t", f"{dur + 0.2:.3f}", "-i", os.path.join(ROOT, mp4),
                        "-vf", f"fps={FPS},scale={W}:{H}:flags=lanczos", os.path.join(folder, "f_%04d.png")], check=True)
    return folder


# ---------------------------------------------------------------------------------------------- looks

rng = np.random.default_rng(7)


def ease(t):
    t = min(1.0, max(0.0, t))
    return t * t * (3 - 2 * t)


def back(t, k=1.7):
    t = min(1.0, max(0.0, t)) - 1.0
    return 1.0 + t * t * ((k + 1) * t + k)


def grain(a, amount):
    return a + rng.normal(0.0, amount, a.shape[:2])[..., None]


def vignette(a, strength):
    y, x = np.ogrid[-1:1:complex(0, H), -1:1:complex(0, W)]
    v = 1.0 - strength * np.clip(x * x * 0.6 + y * y, 0, 1.4)
    return a * v[..., None]


def bars(a, pillar, scope):
    if pillar > 0:
        a[:, :pillar] = 0; a[:, W - pillar:] = 0
    if scope > 0:
        a[:scope] = 0; a[H - scope:] = 0
    return a


def street(a, t, d):
    a = a * np.array([1.05, 1.0, 0.92]) + np.array([6, 2, -4])
    a = vignette(grain(a, 5.0), 0.28)
    return bars(a, PILLAR, 0)


def widen(a, t, d):
    k = ease((t - 0.1) / 0.5)
    a = vignette(grain(a, 3.0), 0.18 * (1 - k) + 0.1)
    return bars(a, int(PILLAR * (1 - k)), int(SCOPE * k))


def scope(a, t, d):
    return bars(vignette(a, 0.12), 0, SCOPE)


def newsreel(a, t, d):
    g = a @ np.array([0.30, 0.59, 0.11])
    a = np.stack([g * 1.08 + 18, g * 0.95 + 8, g * 0.76], -1)
    a = (a - 128) * 1.18 + 128
    a = a * (0.93 + 0.07 * rng.random())
    a = grain(a, 14.0)
    a = vignette(a, 0.55)
    for _ in range(rng.integers(0, 3)):
        x = int(rng.integers(PILLAR, W - PILLAR)); a[:, x:x + 2] *= 0.6
    return bars(a, PILLAR, 0)


def vhs(a, t, d):
    a = a.copy()
    a[..., 0] = np.roll(a[..., 0], 5, axis=1); a[..., 2] = np.roll(a[..., 2], -4, axis=1)
    a = (a - 128) * 0.92 + 128
    a = a * np.array([1.06, 1.0, 1.08])
    a[::3] *= 0.9
    a = grain(a, 9.0)
    band = int((t * 220) % H); a[band:band + 18] = a[band:band + 18] * 0.7 + 60
    return bars(a, PILLAR, 0)


def placeholder(a, t, d):
    return bars(a, 0, SCOPE)


LOOKS = {"street": street, "widen": widen, "scope": scope, "newsreel": newsreel, "vhs": vhs, "placeholder": placeholder}

_fonts = {}


def font(path, size):
    key = (path, size)
    if key not in _fonts:
        _fonts[key] = ImageFont.truetype(path, size)
    return _fonts[key]


def outlined(draw, xy, text, f, fill, ink=(10, 4, 6), width=6, anchor="mm"):
    draw.text(xy, text, font=f, fill=fill, anchor=anchor, stroke_width=width, stroke_fill=ink)


def card(lines, t, d, accent=False, ident=False):
    a = np.zeros((H, W, 3), np.float32) + np.array(INK, np.float32)
    a = vignette(grain(a + np.array([10, 0, 2]) * (1 - abs(np.linspace(-1, 1, W)))[None, :, None], 3.0), 0.4)
    img = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
    draw = ImageDraw.Draw(img)
    pop = back((t - 0.05) / 0.32)
    out = 1.0 - ease((t - (d - 0.25)) / 0.25)
    if ident:
        big = font(DISPLAY, int(220 * (0.85 + 0.15 * pop)))
        outlined(draw, (W / 2, H / 2 - 50), lines[0], big, GOLD, width=9)
        small = font(BODY, 70)
        outlined(draw, (W / 2, H / 2 + 120), lines[1], small, CREAM, width=4)
        sub = font(BODY, 34)
        draw.text((W / 2, H / 2 + 200), "ANG GINTONG LATA", font=sub, fill=(200, 160, 90), anchor="mm")
    else:
        size = 104 if max(len(s) for s in lines) < 26 else 86
        f = font(DISPLAY, int(size * (0.9 + 0.1 * pop)))
        y0 = H / 2 - (len(lines) - 1) * size * 0.62
        for i, line in enumerate(lines):
            colour = LIME if accent else CREAM
            outlined(draw, (W / 2, y0 + i * size * 1.24), line, f, colour, width=7)
    a = np.asarray(img).astype(np.float32) * min(ease(t / 0.12), out) + np.array(INK) * (1 - min(ease(t / 0.12), out))
    return a


def caption(img, text, look):
    draw = ImageDraw.Draw(img)
    if look == "newsreel":
        f = font(BODY, 40)
        draw.rectangle([PILLAR + 60, H - 150, PILLAR + 60 + draw.textlength(text, font=f) + 40, H - 88], fill=(18, 12, 8))
        draw.text((PILLAR + 80, H - 119), text, font=f, fill=(232, 214, 180), anchor="lm")
    elif look == "vhs":
        f = font(BODY, 44)
        draw.text((PILLAR + 60, 70), "PLAY ▶", font=f, fill=(235, 235, 235), anchor="lm", stroke_width=2, stroke_fill=(20, 20, 20))
        draw.text((W - PILLAR - 60, H - 80), text, font=f, fill=(235, 235, 235), anchor="rm", stroke_width=2, stroke_fill=(20, 20, 20))


def tag(img, text):
    draw = ImageDraw.Draw(img)
    f = font(BODY, 30)
    w = draw.textlength(text, font=f)
    draw.rectangle([40, SCOPE + 24, 40 + w + 36, SCOPE + 72], fill=(200, 30, 40))
    draw.text((58, SCOPE + 48), text, font=f, fill=(255, 255, 255), anchor="lm")


def stamp(img, label):
    draw = ImageDraw.Draw(img)
    draw.text((W - 30, H - 22), label, font=font(BODY, 22), fill=(150, 150, 150), anchor="rs")


# ---------------------------------------------------------------------------------------------- render

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--version", type=int, default=1)
    ap.add_argument("--no-audio", action="store_true")
    ap.add_argument("--sound-only", action="store_true", help="reuse the last picture render, remix the sound")
    args = ap.parse_args()
    os.chdir(ROOT)
    os.makedirs(WORK, exist_ok=True)
    name = f"animatic_v{args.version}"
    silent = os.path.join(WORK, name + "_picture.mp4")

    starts, t0 = [], 0.0
    at = {}
    for seg in EDL:
        starts.append(t0); at[seg[0]] = t0; t0 += seg[3]
    total = t0

    if args.sound_only and os.path.exists(silent):
        EDL_RUN = []
    else:
        EDL_RUN = EDL
    enc = None if not EDL_RUN else subprocess.Popen(["ffmpeg", "-v", "error", "-y", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W}x{H}", "-r", str(FPS), "-i", "-",
                            "-c:v", "libx264", "-preset", "medium", "-crf", "17", "-pix_fmt", "yuv420p", silent], stdin=subprocess.PIPE)
    sheet = []
    for k, (sid, src, look, dur, extra) in enumerate(EDL_RUN):
        n = int(round(dur * FPS))
        folder = clip_frames(src[1], src[2], dur, f"{os.path.splitext(os.path.basename(src[1]))[0]}_{src[2]:.2f}_{dur:.2f}") if src[0] == "clip" else None
        for i in range(n):
            t = i / FPS
            if src[0] == "black":
                a = np.zeros((H, W, 3), np.float32)
            elif src[0] == "card":
                a = card(src[1], t, dur, accent=extra.get("accent", False), ident=look == "ident")
            else:
                if src[0] == "film":
                    img = film_frame(src[1], src[2], src[3] + t)
                else:
                    files = sorted(os.listdir(folder))
                    img = Image.open(os.path.join(folder, files[min(i, len(files) - 1)])).convert("RGB")
                a = LOOKS[look](np.asarray(img).astype(np.float32), t, dur)
            img = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
            if "caption" in extra:
                caption(img, extra["caption"], look)
            if "tag" in extra:
                tag(img, extra["tag"])
            stamp(img, f"TUMP TRAILER ANIMATIC v{args.version}  ·  {starts[k] + t:05.2f}")
            if i == n // 2:
                sheet.append(img.resize((480, 270)))
            enc.stdin.write(img.tobytes())
        print(f"{starts[k]:6.2f}  {dur:4.2f}  {sid:8s} {look:11s} {src[0]}", flush=True)
    if enc is not None:
        enc.stdin.close(); enc.wait()

    cols = 5
    if not sheet:
        sheet = [Image.new('RGB', (480, 270))]
    rows = math.ceil(len(sheet) / cols)
    contact = Image.new("RGB", (cols * 480, rows * 270), (0, 0, 0))
    for i, im in enumerate(sheet):
        contact.paste(im, ((i % cols) * 480, (i // cols) * 270))
    if EDL_RUN:
        contact.save(os.path.join(OUT, name + "_sheet.png"))

    final = os.path.join(OUT, name + ".mp4")
    if args.no_audio:
        shutil.copyfile(silent, final)
    else:
        inputs, chains, labels = [], [], []
        for j, (path, at, vol, o) in enumerate(cues(at, total)):
            inputs += ["-i", path]
            f = []
            if "dur" in o: f.append(f"atrim=0:{o['dur']:.3f}")
            if "filter" in o: f.append(o["filter"])
            if "fade_in" in o: f.append(f"afade=t=in:d={o['fade_in']}")
            if "fade_out" in o and "dur" in o: f.append(f"afade=t=out:st={max(0.0, o['dur'] - o['fade_out']):.3f}:d={o['fade_out']}")
            f += [f"volume={vol}", "aformat=sample_rates=48000:channel_layouts=stereo", f"adelay={int(at * 1000)}|{int(at * 1000)}"]
            chains.append(f"[{j + 1}:a]{','.join(f)}[a{j}]"); labels.append(f"[a{j}]")
        graph = ";".join(chains) + f";{''.join(labels)}amix=inputs={len(labels)}:normalize=0,atrim=0:{total:.3f},alimiter=limit=0.9[mix]"
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", silent] + inputs +
                       ["-filter_complex", graph, "-map", "0:v", "-map", "[mix]", "-c:v", "copy", "-c:a", "aac", "-b:a", "192k", final], check=True)
    share = os.path.join(OUT, name + "_share.mp4")
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", final, "-vf", "scale=1280:720,hqdn3d=3:3:4:4", "-c:v", "libx264", "-preset", "slow",
                    "-crf", "27", "-c:a", "aac", "-b:a", "128k", share], check=True)
    print(f"wrote {final}  ({total:.2f} s) and {share} (small review copy for sending)")


if __name__ == "__main__":
    main()
