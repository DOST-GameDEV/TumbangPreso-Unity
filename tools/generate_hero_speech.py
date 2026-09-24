"""
Every hero line spoken in that hero's own voice (VOICE-1, owner 2026-09-24: "PLEASE make the voices
now", "download whatever u would need to make natural free voice").

    <venv>/python tools/generate_hero_speech.py --audition          # one line per hero, to listen
    <venv>/python tools/generate_hero_speech.py                     # every line this script may write
    <venv>/python tools/generate_hero_speech.py --only zack.can.1   # one line
    <venv>/python tools/generate_hero_speech.py --hero nemu         # one hero

The venv is outside the repository (`C:\\Users\\Matthew\\tts-venv`) and so are the model files
(`C:\\Users\\Matthew\\tts-models`); nothing here ships them. What ships is the WAV each line
produces.

⚠️⚠️ WHY TWO MODELS. Both are free, local and offline.
  1. Kokoro 82M (Apache 2.0, `kokoro-onnx`) DESIGNS each hero's voice: its speaker vectors can be
     blended, so a hero is a mix of presets at a pace (`CAST` below) rather than one stock voice
     every other Kokoro project also uses. It reads a short in-character paragraph that becomes
     the hero's reference.
  2. Chatterbox (Resemble AI, MIT) SPEAKS each line in that reference voice with its emotion
     exaggeration dial, because a game bark is performed, not read: a knockdown is shouted, an
     ultimate warning is urgent, Nemu's aside is barely there. Kokoro alone reads every line at
     the same level (it has no emotion input), which is what a bark must not do.
  3. faster-whisper (MIT) LISTENS to every take and rejects one whose words are not the line.
     Chatterbox occasionally drops or repeats a word or trails off into a breath; three takes are
     made and the best faithful one kept, and if none is faithful the Kokoro reading ships
     instead, so no line is ever left with a wrong sentence.

⚠️⚠️ A RECORDING STILL WINS. `docs/HUMAN.md` Table E is the team's recording list and a take dropped
in under `Resources/HeroVo/hvo_<id>.wav` is never overwritten: this script writes only files named
in its ledger (`tools/hero_voice_generated.txt`, shared with `generate_hero_voice.py`, whose babble
these replace) or missing ones.

⚠️ THE CAPTION IS THE LINE AS WRITTEN; THE VOICE READS A RESPELLING. The models are English, and
read "Sige" as "sigh" and "Tumbang" as "tum-bang". `SAY` respells the Filipino words and names so
they are pronounced as a Filipino kid would say them; `HeroLines.cs` is not touched.

⚠️ SEEDED PER LINE ID AND TAKE (crc32), never per row position, so re-running one line does not
move another, the rule `generate_hero_audio.py` set.
"""
import argparse
import difflib
import os
import re
import sys
import zlib

import numpy as np
import soundfile as sf
from scipy.signal import butter, resample_poly, sosfilt

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from generate_hero_voice import LEDGER, OUT, read_lines, META  # noqa: E402  one copy of the script reader

import hashlib  # noqa: E402

MODELS = os.environ.get("TUMP_TTS_MODELS", r"C:\Users\Matthew\tts-models")
REF_DIR = os.path.join(MODELS, "tump-references")
SR_OUT = 44100

# ------------------------------------------------------------------------------------ the cast
#
# Each hero is a blend of Kokoro presets and a pace, chosen from `CHARACTER_ORIGINS.md`, then a
# delivery scale that multiplies every line's emotion exaggeration. The heroes are teenage street
# players in a cute world, so every voice is a young one; none is the stock narrator.
#   voices: [(preset, weight)], speed, delivery scale, reference paragraph (in character), youth
#
# ⚠️ YOUTH IS A SHIFT IN SEMITONES OF PITCH AND THROAT TOGETHER. Every Kokoro preset is an adult
# (measured 2026-09-24 by autocorrelation on one sentence: am_onyx 88 Hz, am_michael 118, am_liam
# 130, am_eric 164, af_kore 150, af_bella 206), and a street player in this cast is a teenager.
# Kokoro reads the line slower by the shift's ratio and the samples are then played faster by the
# same ratio, so the pace comes back to normal while pitch AND formants rise: a smaller throat, the
# same idea as the babble's `tract` (1.10 to 1.24 there; 2 to 3 semitones is 1.12 to 1.19 here).
CAST = {
    # Patient, few words, even and sure; keeps the score.
    "sean": ([("am_liam", 0.6), ("am_michael", 0.4)], 0.96, 0.85,
             "One clean opening is all I ever need. I watch, I wait, and when the line is there, "
             "I take it. Zack can rush all he likes. I'm keeping score.", 3),
    # Quick, showy, lazy slide of a kid who makes it look easy.
    "zack": ([("am_eric", 0.5), ("am_puck", 0.5)], 1.10, 1.15,
             "Did you see that? Off the angle, first try! Tell the neighbours, okay? "
             "Roof rules. I found the spot before anyone else even looked.", 3),
    # Low, slow, level; the fair one, stone and mountain.
    "dante": ([("am_fenrir", 0.5), ("am_michael", 0.5)], 0.90, 0.8,
              "Straighten the can. Play fair, everyone gets another try. "
              "I will hold this ground as long as it takes. Stone holds.", 1.5),
    # Clear, clipped, exact; the quiet planner.
    "cheska": ([("af_kore", 0.55), ("af_sarah", 0.45)], 1.02, 0.8,
               "I walked the edges already. The footing is fine on the left and slippery on the "
               "right. Take the long way. And who's got the water?", 2),
    # Breathy, drifting, soft; half here, Kuro at her shoulder.
    "nemu": ([("af_nicole", 0.6), ("af_sky", 0.4)], 0.90, 0.6,
             "Oh, are we starting? Hm. Kuro says hi. I was only looking at the water for a bit. "
             "Don't worry, I saw where you were going.", 3),
    # Theatrical, wide, a reveal in every sentence; the stage is hers.
    "phaister": ([("af_bella", 0.6), ("af_heart", 0.4)], 1.04, 1.25,
                 "Places, everyone! Tonight's trick? Watch closely, because the moon is about to "
                 "disappear. And the crowd goes wild. Ta-da!", 2),
    # Bouncy, warm, teasing; the kid from the water village with an idea.
    "rafi": ([("am_adam", 0.5), ("am_eric", 0.3), ("em_alex", 0.2)], 1.06, 1.1,
             "I've got an idea. Strap's fixed, let's play! Bet you didn't see that bend coming. "
             "No hard feelings, okay? Next time, bring a boat.", 2.5),
}

# How hard each moment is performed (Chatterbox exaggeration before the hero's scale; cfg weight
# lower is faster and more urgent). A bark shouted on every line is as flat as one read on every
# line, so only the peaks are loud: the knockdown, the ultimate warning, the tag.
DELIVERY = {
    "skill1": (0.60, 0.50), "skill2": (0.60, 0.50),
    "ultally": (0.85, 0.35), "ultopp": (0.70, 0.45),
    "round": (0.50, 0.50), "tag": (0.70, 0.45), "tagged": (0.45, 0.50),
    "can": (0.80, 0.40), "lead": (0.55, 0.50), "win": (0.70, 0.45),
    "banter": (0.55, 0.50), "reply": (0.55, 0.50),
}

# Respellings for the voice only (see the header). Word-boundary, case-kept by the first letter.
SAY = {
    "tumbang": "toom-bahng", "tara": "tah-rah", "sige": "see-geh", "hala": "hah-lah",
    "grabe": "grah-beh", "ayos": "ah-yohs", "kuro": "koo-roh", "nemu": "neh-moo",
    "cheska": "chess-kah", "rafi": "rah-fee", "dante": "dahn-teh", "ta-da": "tah-dah",
}


def spoken(text):
    def sub(m):
        w = m.group(0)
        r = SAY[w.lower()]
        return r[0].upper() + r[1:] if w[0].isupper() else r
    pattern = r"\b(" + "|".join(re.escape(k) for k in sorted(SAY, key=len, reverse=True)) + r")\b"
    return re.sub(pattern, sub, text, flags=re.I)


def kind_of(line_id):
    return line_id.split(".")[1]


# ------------------------------------------------------------------------------------ models

_kokoro = None
_tts = None
_whisper = None


def kokoro():
    global _kokoro
    if _kokoro is None:
        from kokoro_onnx import Kokoro
        _kokoro = Kokoro(os.path.join(MODELS, "kokoro-v1.0.onnx"), os.path.join(MODELS, "voices-v1.0.bin"))
    return _kokoro


def hero_style(hero):
    k = kokoro()
    blend = sum(k.get_voice_style(name) * w for name, w in CAST[hero][0])
    return blend


def kokoro_say(hero, text):
    ratio = 2 ** (CAST[hero][4] / 12)
    samples, sr = kokoro().create(spoken(text), voice=hero_style(hero), speed=CAST[hero][1] / ratio, lang="en-us")
    # Fewer samples at the same rate: pitch and formants up by `ratio`, pace back to normal.
    shifted = resample_poly(np.asarray(samples, dtype=np.float64), 1000, int(round(1000 * ratio)))
    return shifted.astype(np.float32), sr


def reference(hero):
    """The hero's reference voice for Chatterbox, made once by Kokoro from the paragraph."""
    os.makedirs(REF_DIR, exist_ok=True)
    path = os.path.join(REF_DIR, hero + ".wav")
    if not os.path.exists(path):
        audio, sr = kokoro_say(hero, CAST[hero][3])
        sf.write(path, audio, sr)
    return path


def chatterbox():
    global _tts
    if _tts is None:
        import torch
        from chatterbox.tts import ChatterboxTTS
        _tts = ChatterboxTTS.from_pretrained(device="cuda" if torch.cuda.is_available() else "cpu")
    return _tts


def whisper():
    global _whisper
    if _whisper is None:
        from faster_whisper import WhisperModel
        import torch
        cuda = torch.cuda.is_available()
        _whisper = WhisperModel("small.en", device="cuda" if cuda else "cpu",
                                compute_type="float16" if cuda else "int8")
    return _whisper


def words(text):
    return re.findall(r"[a-z0-9']+", text.lower().replace("-", " "))


def fidelity(audio, sr, text):
    """How much of the line the take actually says, 0 to 1, by Whisper against the spoken text.
    Word-level sequence match on the respelled line, so "toom-bahng" heard as "tumbang" still
    lands close letter by letter rather than failing a whole word."""
    a16 = resample_poly(audio, 16000, sr).astype(np.float32)
    segs, _ = whisper().transcribe(a16, language="en", beam_size=5, condition_on_previous_text=False)
    heard = " ".join(s.text for s in segs)
    want = " ".join(words(spoken(text)))
    got = " ".join(words(heard))
    return difflib.SequenceMatcher(None, want.replace(" ", ""), got.replace(" ", "")).ratio(), heard.strip()


# ------------------------------------------------------------------------------------ finishing

def finish(audio, sr):
    """Trim, clean, level. -6 dBFS peak is HUMAN.md's number; the RMS target keeps a whisper and a
    shout from landing at the same loudness only because both were peak-normalised."""
    audio = np.asarray(audio, dtype=np.float64)
    if sr != SR_OUT:
        audio = resample_poly(audio, SR_OUT, sr)
    audio = sosfilt(butter(2, 70, "hp", fs=SR_OUT, output="sos"), audio)
    env = np.abs(audio)
    gate = max(env.max() * 0.02, 1e-4)
    idx = np.where(env > gate)[0]
    if len(idx):
        start = max(0, idx[0] - int(0.03 * SR_OUT))
        end = min(len(audio), idx[-1] + int(0.12 * SR_OUT))
        audio = audio[start:end]
    fade = int(0.008 * SR_OUT)
    audio[:fade] *= np.linspace(0, 1, fade)
    audio[-fade * 4:] *= np.linspace(1, 0, fade * 4)
    rms = np.sqrt(np.mean(audio ** 2)) or 1e-6
    audio *= 10 ** (-19 / 20) / rms
    peak = np.max(np.abs(audio)) or 1.0
    ceiling = 10 ** (-6 / 20)
    if peak > ceiling:
        audio *= ceiling / peak
    return audio.astype(np.float32)


def expected_seconds(text):
    return 0.35 + len(words(text)) / 2.9


def perform(line_id, text, takes=3, log=None):
    """Best faithful Chatterbox take, else the Kokoro reading. Returns (audio@44.1k, source, score, heard)."""
    import torch
    hero = line_id.split(".")[0]
    exag, cfg = DELIVERY[kind_of(line_id)]
    exag = float(np.clip(exag * CAST[hero][2], 0.25, 1.2))
    ref = reference(hero)
    best = None
    for take in range(takes):
        torch.manual_seed(zlib.crc32(f"{line_id}#{take}".encode()))
        wav = chatterbox().generate(spoken(text), audio_prompt_path=ref, exaggeration=exag,
                                    cfg_weight=cfg, temperature=0.8)
        audio = wav.squeeze(0).cpu().numpy()
        sr = chatterbox().sr
        score, heard = fidelity(audio, sr, text)
        dur = len(audio) / sr
        # A take much longer than the line reads is a trailing breath or a repeat.
        overlong = max(0.0, dur - 1.8 * expected_seconds(text) - 0.6)
        rank = score - 0.15 * overlong
        if log:
            log(f"  {line_id} take {take}: {score:.2f} {dur:.2f}s '{heard}'")
        if best is None or rank > best[0]:
            best = (rank, audio, sr, score, heard)
        if score >= 0.95 and overlong == 0:
            break
    if best[3] >= 0.8:
        return finish(best[1], best[2]), "chatterbox", best[3], best[4]
    audio, sr = kokoro_say(hero, text)
    score, heard = fidelity(audio, sr, text)
    return finish(audio, sr), "kokoro", score, heard


def write(line_id, audio):
    name = "hvo_" + line_id.replace(".", "_")
    path = os.path.join(OUT, name + ".wav")
    sf.write(path, audio, SR_OUT, subtype="PCM_16")
    if not os.path.exists(path + ".meta"):
        guid = hashlib.md5(("hero-voice/" + name).encode()).hexdigest()
        open(path + ".meta", "w").write(META.format(guid=guid))
    return name


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only")
    ap.add_argument("--hero")
    ap.add_argument("--audition", help="write one line per hero into this folder instead of Resources")
    ap.add_argument("--takes", type=int, default=3)
    args = ap.parse_args()
    lines = read_lines()
    generated = set(open(LEDGER).read().split()) if os.path.exists(LEDGER) else set()
    report = []

    def log(s):
        print(s, flush=True)
        report.append(s)

    if args.audition:
        os.makedirs(args.audition, exist_ok=True)
        picks = ["sean.round.1", "zack.can.1", "dante.ultally.1", "cheska.round.2",
                 "nemu.skill2.1", "phaister.ultally.2", "rafi.win.1"]
        for line_id in picks:
            audio, src, score, heard = perform(line_id, lines[line_id], args.takes, log)
            sf.write(os.path.join(args.audition, line_id.replace(".", "_") + ".wav"), audio, SR_OUT)
            log(f"{line_id} [{src} {score:.2f}] '{lines[line_id]}' heard '{heard}'")
        return

    for line_id, text in sorted(lines.items()):
        if args.only and line_id != args.only:
            continue
        if args.hero and not line_id.startswith(args.hero + "."):
            continue
        name = "hvo_" + line_id.replace(".", "_")
        path = os.path.join(OUT, name + ".wav")
        if os.path.exists(path) and name not in generated:
            log(f"{line_id}: a recording, left alone")
            continue
        audio, src, score, heard = perform(line_id, text, args.takes, log)
        write(line_id, audio)
        generated.add(name)
        log(f"{line_id} [{src} {score:.2f} {len(audio) / SR_OUT:.2f}s] '{text}' heard '{heard}'")
    with open(LEDGER, "w") as f:
        f.write("\n".join(sorted(generated)) + "\n")
    with open(os.path.join(HERE, "..", "Logs", "hero-speech-report.txt"), "a", encoding="utf-8") as f:
        f.write("\n".join(report) + "\n")


if __name__ == "__main__":
    main()
