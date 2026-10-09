"""
New announcer lines in the recorded announcer's voice, from tools/arena_announcer_lines.json.

THESE ARE AI-GENERATED TAKES OF A TEAMMATE'S VOICE, MADE WITH THEIR CONSENT (2026-10-05). They are
not recordings. Everything this writes says so: Assets/TumbangPreso/Resources/Vo/AI_CLONED_LINES.md
lists every file. A real recording under the same file name replaces a cloned take with no code change.

RUN IT (from the repository root, with the voice environment's Python, never the system one's packages):

  C:/Users/StarX/.cache/tump-voice/venv/Scripts/python.exe tools/clone_announcer_lines.py
  ... --only arena_tore        only the ids containing this text
  ... --more 1                 another round of candidates (new seeds) on top of what is cached
  ... --deliver-only           no model: choose and deliver again from the cached candidates and scores
                               (after editing a "pick" in the json, or a threshold below)
  then, for the stadium's take of each line (it reads Resources/Vo):
  py -3 tools/synth_arena_crowd_sfx.py --no-vo --only pa

THE ENVIRONMENT (all of it outside the repository, under C:/Users/StarX/.cache/tump-voice, or
TUMP_VOICE_CACHE): `venv` is `py -3 -m venv --system-site-packages`, so it uses the system's CUDA
torch (2.9.0+cu129, which the RTX 5070 needs) and the system's transformers, and adds only
chatterbox-tts 0.1.7, the packages it imports that the system lacked (s3tokenizer, conformer,
diffusers, librosa, resemble-perth, omegaconf, pyloudnorm and theirs) and torchaudio 2.9.0. No
second torch. Its gradio, pykakasi and spacy-pkuseg (a demo page, Japanese, Chinese) are not installed. `models/chatterbox` is the multilingual
Chatterbox (Resemble AI, MIT), 3.21 GB, pinned to the revision in the json; `models/whisper-base`
is OpenAI's Whisper base (Apache-2.0), 0.29 GB, the local recogniser that checks each take can be
understood. Nothing is sent anywhere: both models are loaded from those folders.

WHAT IT DOES, PER TAKE IN THE JSON
  1. builds the reference: the eleven recorded takes in the json's order, 0.12 s apart (measured
     with five takes held out: all takes together beat any single take, 0.82 against 0.77 to 0.78);
  2. generates candidates: every way of saying the text (`say`, or for a Filipino line the model's
     Swahili, Malay, Spanish and English modes, since it has no Filipino one) at three settings of
     exaggeration and pace (cfg weight: lower is slower), each with its own seed;
  3. generates the same words in the model's built-in voice, twice: the "stranger". A clone is only
     a clone if it is closer to the announcer than a stranger saying the same thing;
  4. measures every candidate: `sim` (cosine of the model's own voice-encoder embedding with the
     centroid of the eleven recorded takes), `margin` (sim minus the stranger's), `asr` (what Whisper
     hears against the written line, letters compared after a light phonetic fold), syllable nuclei
     against the count expected, duration, the longest silence inside, a cut start or end, clipping;
  5. keeps the best candidate that breaks no rule, rates it GOOD, PASSABLE or FAILED (the thresholds
     are the constants below), and delivers GOOD and PASSABLE takes of WIRED lines as
     Resources/Vo/vo_<id>_<n>.wav: 44.1 kHz mono 16-bit, trimmed (40 ms lead, 180 ms tail), faded,
     peak at -6 dBFS like the recorded takes. A FAILED take is not delivered and an older file of
     its name is removed, so the line stays silent and the PA sting marks the moment. A line with
     "wiring": "none" goes to Logs/arena/voice/unwired instead of Resources.
  6. writes Logs/arena/voice: candidates/<id>_<n>/*.wav (every candidate, as generated, 24 kHz),
     sheets/<id>_<n>.png (the contact sheet), index.html (every candidate with a player, to audition),
     report.txt, and the manifest in Resources/Vo.

TO OVERRULE A CHOICE: listen in Logs/arena/voice/index.html, put the candidate's file stem in the
take's "pick" in the json ("none" ships nothing), and run with --deliver-only.

A recorded take is never overwritten: ids in HUMAN are refused.
"""
import argparse
import difflib
import html
import json
import logging
import os
import re
import sys
import warnings
import zlib
from pathlib import Path

import numpy as np

warnings.filterwarnings("ignore")
logging.disable(logging.WARNING)

ROOT = Path(__file__).resolve().parents[1]
LINES = ROOT / "tools" / "arena_announcer_lines.json"
VO = ROOT / "Assets" / "TumbangPreso" / "Resources" / "Vo"
PA = ROOT / "Assets" / "TumbangPreso" / "Resources" / "ArenaPa"
OUT = ROOT / "Logs" / "arena" / "voice"
CACHE = Path(os.environ.get("TUMP_VOICE_CACHE", str(Path.home() / ".cache" / "tump-voice")))
MODELS = CACHE / "models"

# The announcer's own recordings (docs/HUMAN.md, delivered 2026-08-01). Never written to.
HUMAN = ("clock_10_1", "clock_30_1", "count_1_1", "count_2_1", "count_3_1", "count_go_1", "count_go_2",
         "match_draw_1", "match_draw_2", "match_win_1", "match_win_2")

GEN_SR, GAME_SR = 24000, 44100
# (exaggeration, cfg weight). A lower cfg weight is a slower, more deliberate read.
SETTINGS = ((0.5, 0.5), (0.7, 0.4), (0.9, 0.3), (0.6, 0.6))
FIL_MODES = ("sw", "ms", "es", "en")
ASR_LANGUAGE = {"fil": "tl", "en": "en"}

# ---- the rules a candidate must not break, and the ratings (set from the data: see report.txt) ----
ASR_REJECT = 0.60            # heard as something else
GAP_SECONDS = 0.35           # the longest silence inside a line; 0.60 if the line has two phrases
ASR_GOOD, ASR_PASS = 0.90, 0.75
SIM_GOOD, SIM_PASS = 0.70, 0.62      # recorded takes against each other: 0.70 to 0.89 (leave one out)
MARGIN_GOOD, MARGIN_PASS = 0.08, 0.03  # closer to the announcer than the stranger by this much

PEAK = 10 ** (-6.0 / 20.0)
LEAD, TAIL, FADE_IN, FADE_OUT = 0.040, 0.180, 0.008, 0.120


# ------------------------------------------------------------------ text

def fold(text):
    """Letters only, with the spellings that sound the same folded together."""
    s = re.sub(r"[^a-z]", "", text.lower())
    for a, b in (("ph", "p"), ("ce", "se"), ("ci", "si"), ("c", "k"), ("z", "s"), ("q", "k"), ("x", "ks"),
                 ("oo", "u"), ("ee", "i"), ("ai", "ay"), ("ei", "ey"), ("oi", "oy"), ("au", "aw")):
        s = s.replace(a, b)
    return re.sub(r"(.)\1+", r"\1", s)


def heard(a, b):
    return difflib.SequenceMatcher(None, fold(a), fold(b)).ratio()


def syllables(text, language):
    total = 0
    for word in re.findall(r"[a-z']+", text.lower()):
        word = word.replace("'", "")
        if language == "en":
            if word.endswith("ed") and len(word) > 3 and not word.endswith(("ted", "ded")):
                word = word[:-2]
            elif word.endswith("e") and len(word) > 3 and not word.endswith("le"):
                word = word[:-1]
            total += max(1, len(re.findall(r"[aeiouy]+", word)))
        else:
            total += max(1, len(re.findall(r"[aeiou]", word)))
    return total


# ------------------------------------------------------------------ audio, without the models

def read_vo(name, rate):
    import librosa
    x, _ = librosa.load(str(VO / ("vo_" + name + ".wav")), sr=rate)
    return x


def frames_db(x, rate, ms=10):
    n = max(1, int(rate * ms / 1000))
    count = len(x) // n
    if count == 0:
        return np.array([-120.0])
    r = np.sqrt((x[:count * n].reshape(count, n) ** 2).mean(axis=1) + 1e-12)
    return 20 * np.log10(r / (r.max() + 1e-12) + 1e-9)


def active(x, rate, floor=-42.0, ms=5):
    """First and last sample of the sound: frames within `floor` dB of the loudest."""
    db = frames_db(x, rate, ms)
    n = max(1, int(rate * ms / 1000))
    on = np.where(db > floor)[0]
    if len(on) == 0:
        return 0, len(x)
    return on[0] * n, min(len(x), (on[-1] + 1) * n)


def nuclei(x, rate):
    """Syllable nuclei: peaks of the speech-band envelope. A count, to set against the syllables."""
    from scipy import signal
    import librosa
    y = librosa.resample(x.astype(np.float32), orig_sr=rate, target_sr=16000) if rate != 16000 else x
    sos = signal.butter(4, [300, 3400], btype="band", fs=16000, output="sos")
    env = np.abs(signal.sosfiltfilt(sos, y))
    sos = signal.butter(2, 9, btype="low", fs=16000, output="sos")
    env = np.maximum(signal.sosfiltfilt(sos, env), 1e-7)
    db = 20 * np.log10(env / env.max())
    peaks, _ = signal.find_peaks(db, prominence=3.5, distance=int(0.11 * 16000), height=-24)
    return int(len(peaks))


def measure_shape(raw, text, language, count):
    """Everything about a candidate that needs no model. `raw` is as generated."""
    s, e = active(raw, GEN_SR)
    x = raw[s:e]
    seconds = len(x) / GEN_SR
    db = frames_db(x, GEN_SR)
    quiet, run, longest = db < -35.0, 0, 0
    for q in quiet:
        run = run + 1 if q else 0
        longest = max(longest, run)
    gap = longest * 0.010
    two = bool(re.search(r"[!.,?]\s+\S", text))
    peak = float(np.abs(raw).max()) + 1e-9
    head = float(np.sqrt((raw[:int(0.008 * GEN_SR)] ** 2).mean())) / peak
    tail = float(np.sqrt((raw[-int(0.008 * GEN_SR):] ** 2).mean())) / peak
    flags = []
    if seconds < 0.11 * count + 0.15: flags.append("too short")
    if seconds > 0.38 * count + 0.95: flags.append("too long")
    if gap > (0.60 if two else GAP_SECONDS): flags.append("silence inside")
    if s < int(0.004 * GEN_SR) and head > 0.06: flags.append("cut start")
    if len(raw) - e < int(0.004 * GEN_SR) and tail > 0.06: flags.append("cut end")
    if int((np.abs(raw) >= 0.999).sum()) > 4: flags.append("clipped")
    found = nuclei(x, GEN_SR)
    return {"seconds": round(seconds, 3), "gap": round(gap, 3), "nuclei": found, "syllables": count, "flags": flags}


def deliver(raw):
    """As the game takes it: 44.1 kHz, trimmed with a short lead and tail, faded, -6 dBFS peak."""
    import librosa
    from scipy import signal
    x = librosa.resample(raw.astype(np.float32), orig_sr=GEN_SR, target_sr=GAME_SR, res_type="soxr_hq").astype(np.float64)
    x = signal.sosfiltfilt(signal.butter(2, 60, btype="high", fs=GAME_SR, output="sos"), x)
    s, e = active(x, GAME_SR)
    lead, tail = int(LEAD * GAME_SR), int(TAIL * GAME_SR)
    x = np.concatenate([np.zeros(max(0, lead - s)), x[max(0, s - lead):min(len(x), e + tail)], np.zeros(max(0, e + tail - len(x)))])
    n = int(FADE_IN * GAME_SR)
    x[:n] *= 0.5 - 0.5 * np.cos(np.pi * np.arange(n) / n)
    n = int(FADE_OUT * GAME_SR)
    x[-n:] *= 0.5 + 0.5 * np.cos(np.pi * np.arange(n) / n)
    x *= PEAK / (np.abs(x).max() + 1e-12)
    rms = 20 * np.log10(np.sqrt((x ** 2).mean()) + 1e-12)
    # The recorded takes are -22.8 to -17.8 dB RMS at this peak. Hotter than that is turned down.
    if rms > -17.8:
        x *= 10 ** ((-18.5 - rms) / 20.0)
    return x


def level(x):
    return 20 * np.log10(np.abs(x).max() + 1e-12), 20 * np.log10(np.sqrt((x ** 2).mean()) + 1e-12)


# ------------------------------------------------------------------ the models

class Models:
    def __init__(self, order):
        import torch
        import soundfile as sf
        from transformers import WhisperForConditionalGeneration, WhisperProcessor
        from transformers.utils import logging as tlog
        from chatterbox.mtl_tts import ChatterboxMultilingualTTS
        tlog.set_verbosity_error()
        self.torch = torch
        device = "cuda" if torch.cuda.is_available() else "cpu"
        self.device = device
        self.processor = WhisperProcessor.from_pretrained(MODELS / "whisper-base")
        self.whisper = WhisperForConditionalGeneration.from_pretrained(MODELS / "whisper-base").to(device).eval()
        self.tts = ChatterboxMultilingualTTS.from_local(MODELS / "chatterbox", device)
        self.stranger = self.tts.conds
        if self.stranger is None:
            raise SystemExit("models/chatterbox/conds.pt (the model's built-in voice) is missing")

        (OUT / "refs").mkdir(parents=True, exist_ok=True)
        parts = []
        for name in order:
            parts += [read_vo(name, GAME_SR), np.zeros(int(0.12 * GAME_SR), dtype=np.float32)]
        ref = OUT / "refs" / "reference_all.wav"
        sf.write(str(ref), np.concatenate(parts), GAME_SR)
        self.tts.prepare_conditionals(str(ref), exaggeration=0.5)
        self.clone = self.tts.conds

        self.real = np.stack([self.embed(read_vo(name, 16000), 16000) for name in HUMAN])
        c = self.real.mean(axis=0)
        self.centroid = c / np.linalg.norm(c)
        # Each recorded take against the other ten: what "the same person" scores on this measure.
        self.same = []
        for i in range(len(HUMAN)):
            o = np.delete(self.real, i, axis=0).mean(axis=0)
            self.same.append(float(self.real[i] @ (o / np.linalg.norm(o))))

    def embed(self, x, rate):
        import librosa
        if rate != 16000:
            x = librosa.resample(x.astype(np.float32), orig_sr=rate, target_sr=16000)
        e = self.tts.ve.embeds_from_wavs([x.astype(np.float32)], sample_rate=16000)[0]
        return e / np.linalg.norm(e)

    def sim(self, x, rate):
        return float(self.embed(x, rate) @ self.centroid)

    def hear(self, x, rate, language):
        import librosa
        if rate != 16000:
            x = librosa.resample(x.astype(np.float32), orig_sr=rate, target_sr=16000)
        pad = np.zeros(4000, dtype=np.float32)
        feats = self.processor(np.concatenate([pad, x.astype(np.float32), pad]), sampling_rate=16000,
                               return_tensors="pt").input_features.to(self.device)
        with self.torch.no_grad():
            ids = self.whisper.generate(feats, language=language, task="transcribe", max_new_tokens=40, num_beams=5)
        return self.processor.batch_decode(ids, skip_special_tokens=True)[0].strip()

    def say(self, text, mode, seed, exaggeration, cfg, stranger=False):
        self.torch.manual_seed(seed)
        if self.device == "cuda":
            self.torch.cuda.manual_seed_all(seed)
        np.random.seed(seed % (2 ** 31))
        self.tts.conds = self.stranger if stranger else self.clone
        wav = self.tts.generate(text, language_id=mode, exaggeration=exaggeration, cfg_weight=cfg, temperature=0.8)
        return wav.squeeze(0).numpy().astype(np.float32)


# ------------------------------------------------------------------ candidates

def ways(take):
    """(mode, spoken text) for every way this take is tried."""
    if take.get("say"):
        return [tuple(w) for w in take["say"]]
    if take["language"] == "en":
        return [("en", take["text"])]
    return [(mode, take["text"]) for mode in FIL_MODES]


def plan(key, take, rounds):
    """The candidates of a take: (stem, mode, spoken, exaggeration, cfg, seed). Stable across runs."""
    base = zlib.crc32(key.encode()) % 100000
    out, k = [], 0
    for r in range(rounds):
        w = ways(take)
        per = 3 if len(w) >= 4 else (4 if len(w) > 1 else len(SETTINGS))
        repeats = 3 if len(w) == 1 else 1
        for mode, spoken in w:
            for j in range(per):
                for q in range(repeats):
                    ex, cfg = SETTINGS[(j + r) % len(SETTINGS)]
                    seed = base + k
                    out.append(("%02d_%s_e%02d_c%02d_s%d" % (k, mode, round(ex * 100), round(cfg * 100), seed), mode, spoken, ex, cfg, seed))
                    k += 1
    return out


def score(c):
    count, found = c["syllables"], c["nuclei"]
    off = abs(found - count)
    # The nuclei count is right on 7 of the 11 recorded takes and within one on 10, so it only
    # weighs a little, and only a count that is far off costs much.
    syl = 1.0 if off == 0 else (0.8 if off == 1 else (0.4 if off == 2 and count >= 5 else 0.0))
    margin = max(-0.1, min(0.25, c["margin"]))
    value = 0.45 * c["asr"] + 0.27 * min(1.0, max(0.0, (c["sim"] - 0.55) / 0.35)) + 0.18 * (margin + 0.1) / 0.35 + 0.10 * syl
    return value - (0.5 if c["flags"] else 0.0)


def rate(c):
    if c["flags"] or c["asr"] < ASR_PASS or c["sim"] < SIM_PASS or c["margin"] < MARGIN_PASS:
        return "FAILED"
    off = abs(c["nuclei"] - c["syllables"])
    if c["asr"] >= ASR_GOOD and c["sim"] >= SIM_GOOD and c["margin"] >= MARGIN_GOOD and off <= (2 if c["syllables"] >= 5 else 1):
        return "GOOD"
    return "PASSABLE"


def generate(models, key, line, take, rounds, folder):
    import soundfile as sf
    folder.mkdir(parents=True, exist_ok=True)
    cache = folder / "scores.json"
    scores = json.loads(cache.read_text(encoding="utf-8")) if cache.exists() else {}
    language = take["language"]
    count = take.get("syllables") or syllables(take["text"], language)

    # The stranger: the same words, the same mode, the model's built-in voice.
    other = scores.get("_stranger", {})
    for mode in sorted({m for m, _ in ways(take)}):
        if mode in other:
            continue
        spoken = next(s for m, s in ways(take) if m == mode)
        sims = []
        for q in range(2):
            raw = models.say(spoken, mode, 7000 + q, 0.5, 0.5, stranger=True)
            s, e = active(raw, GEN_SR)
            sims.append(models.sim(raw[s:e], GEN_SR))
            sf.write(str(folder / ("stranger_%s_%d.wav" % (mode, q))), raw, GEN_SR)
        other[mode] = round(float(np.mean(sims)), 4)
    scores["_stranger"] = other

    for stem, mode, spoken, ex, cfg, seed in plan(key, take, rounds):
        path = folder / (stem + ".wav")
        if stem in scores and path.exists() and scores[stem].get("spoken") == spoken:
            continue
        raw = models.say(spoken, mode, seed, ex, cfg)
        sf.write(str(path), raw, GEN_SR)
        c = measure_shape(raw, take["text"], language, count)
        s, e = active(raw, GEN_SR)
        x = raw[s:e]
        c["transcript"] = models.hear(x, GEN_SR, ASR_LANGUAGE[language])
        c["asr"] = round(heard(c["transcript"], take["text"]), 3)
        if language == "en" and c["asr"] < ASR_GOOD:
            # An English line with a Filipino word in it ("Lata is down!", "Taya!"): the English
            # recogniser respells the word, so the Tagalog one is asked as well and the better kept.
            again = models.hear(x, GEN_SR, "tl")
            if heard(again, take["text"]) > c["asr"]:
                c["transcript"], c["asr"] = again, round(heard(again, take["text"]), 3)
        c["sim"] = round(models.sim(x, GEN_SR), 4)
        c.update({"mode": mode, "spoken": spoken, "exaggeration": ex, "cfg": cfg, "seed": seed})
        if c["asr"] < ASR_REJECT:
            c["flags"].append("misheard")
        scores[stem] = c
    cache.write_text(json.dumps(scores, indent=1, ensure_ascii=False), encoding="utf-8")
    return scores


def choose(scores, take, taken):
    other = scores.get("_stranger", {})
    rows = []
    for stem, c in scores.items():
        if stem.startswith("_") or c.get("spoken") not in [w[1] for w in ways(take)]:
            continue      # the stranger's row, or a candidate of an earlier wording of this take
        c = dict(c)
        c["stem"] = stem
        c["margin"] = round(c["sim"] - other.get(c["mode"], 0.0), 4)
        c["score"] = round(score(c), 4)
        c["rating"] = rate(c)
        rows.append(c)
    rows.sort(key=lambda c: -c["score"])
    pick = take.get("pick")
    if pick == "none":
        return rows, None
    if pick:
        for c in rows:
            if c["stem"] == pick:
                c["picked"] = True
                return rows, c
        raise SystemExit("pick '%s' is not a candidate" % pick)
    for c in rows:
        if c["rating"] != "FAILED" and c["stem"] not in taken:
            return rows, c
    return rows, None


# ------------------------------------------------------------------ what the owner looks at

def sheet(path, title, rows, chosen, folder, top=8):
    import librosa
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    import soundfile as sf
    shown = [("recorded: vo_clock_30_1 (\"Thirty seconds left!\")", read_vo("clock_30_1", GEN_SR), None)]
    for c in rows[:top]:
        x, _ = sf.read(str(folder / (c["stem"] + ".wav")))
        shown.append((None, x, c))
    longest = max(4.0, max(len(x) for _, x, _ in shown) / GEN_SR)
    fig, axes = plt.subplots(len(shown), 1, figsize=(13, 1.25 * len(shown) + 0.6), squeeze=False)
    fig.suptitle(title, fontsize=11, x=0.01, ha="left")
    for ax, (label, x, c) in zip(axes[:, 0], shown):
        mel = librosa.feature.melspectrogram(y=np.asarray(x, dtype=np.float32), sr=GEN_SR, n_fft=1024, hop_length=128, n_mels=64, fmax=8000)
        ax.imshow(librosa.power_to_db(mel, ref=np.max), origin="lower", aspect="auto", cmap="magma", vmin=-70, vmax=0,
                  extent=(0, len(x) / GEN_SR, 0, 8))
        ax.set_xlim(0, longest * 2.6)
        ax.set_yticks([])
        ax.set_xticks(np.arange(0, longest + 0.01, 0.5))
        ax.tick_params(labelsize=6)
        if c is not None:
            label = "%s%s   %s   score %.2f\nheard \"%s\" (asr %.2f)   sim %.3f  margin %+.3f   %.2f s  nuclei %d/%d  %s" % (
                "SHIPPED  " if chosen is not None and c["stem"] == chosen["stem"] else "", c["stem"], c["rating"], c["score"],
                c["transcript"], c["asr"], c["sim"], c["margin"], c["seconds"], c["nuclei"], c["syllables"],
                ("FLAGS: " + ", ".join(c["flags"])) if c["flags"] else "")
        ax.text(longest * 1.02, 4, label, fontsize=7, va="center", family="monospace",
                color="darkgreen" if c is not None and chosen is not None and c["stem"] == chosen["stem"] else "black")
        for side in ("top", "right"):
            ax.spines[side].set_visible(False)
    fig.tight_layout(rect=(0, 0, 1, 0.97))
    path.parent.mkdir(parents=True, exist_ok=True)
    fig.savefig(str(path), dpi=110)
    plt.close(fig)


def page(results, calibration):
    rows = ["<!doctype html><meta charset='utf-8'><title>Announcer clone candidates</title>",
            "<style>body{font:13px system-ui;margin:16px;background:#fff;color:#111}table{border-collapse:collapse;margin:4px 0 22px}"
            "td,th{border:1px solid #ccc;padding:3px 6px;text-align:left}tr.ship{background:#dff3df}tr.fail{color:#999}"
            "audio{height:26px;width:230px}h2{margin:18px 0 2px;font-size:15px}img{max-width:100%}</style>",
            "<h1>Announcer clone: every candidate</h1>",
            "<p>AI-generated takes of the announcer's voice (Chatterbox Multilingual, local), made with the announcer's consent, 2026-10-05. "
            "Green is what was delivered. To overrule: put a candidate's name in that take's <code>pick</code> in "
            "<code>tools/arena_announcer_lines.json</code> and run <code>tools/clone_announcer_lines.py --deliver-only</code>.</p>",
            "<p>%s</p>" % html.escape(calibration),
            "<p>The recorded announcer, for the ear: " + " ".join(
                "<audio controls preload='none' src='../../../Assets/TumbangPreso/Resources/Vo/vo_%s.wav'></audio>" % n
                for n in ("clock_30_1", "match_win_2", "count_go_1")) + "</p>"]
    for r in results:
        rows.append("<h2>%s take %d: \"%s\" [%s] %s</h2>" % (r["id"], r["n"], html.escape(r["text"]), html.escape(r["caption"]),
                                                             r["rating"] if r["chosen"] else "NOTHING DELIVERED"))
        rows.append("<div>%s. Wiring: %s</div>" % (html.escape(r["when"]), html.escape(r["wiring"])))
        if r["delivered"]:
            rows.append("<div>Delivered: <code>%s</code> <audio controls preload='none' src='%s'></audio></div>" % (
                html.escape(r["delivered"]), html.escape(os.path.relpath(ROOT / r["delivered"], OUT).replace("\\", "/"))))
        rows.append("<table><tr><th>candidate</th><th>listen</th><th>rating</th><th>score</th><th>heard</th><th>asr</th><th>sim</th>"
                    "<th>margin</th><th>s</th><th>nuclei</th><th>flags</th></tr>")
        for c in r["rows"]:
            cls = "ship" if r["chosen"] and c["stem"] == r["chosen"]["stem"] else ("fail" if c["rating"] == "FAILED" else "")
            rows.append("<tr class='%s'><td>%s</td><td><audio controls preload='none' src='candidates/%s/%s.wav'></audio></td><td>%s</td>"
                        "<td>%.2f</td><td>%s</td><td>%.2f</td><td>%.3f</td><td>%+.3f</td><td>%.2f</td><td>%d/%d</td><td>%s</td></tr>" % (
                            cls, c["stem"], r["key"], c["stem"], c["rating"], c["score"], html.escape(c["transcript"]), c["asr"], c["sim"],
                            c["margin"], c["seconds"], c["nuclei"], c["syllables"], html.escape(", ".join(c["flags"]))))
        rows.append("</table><img loading='lazy' src='sheets/%s.png'>" % r["key"])
    (OUT / "index.html").write_text("\n".join(rows), encoding="utf-8")


def manifest(spec, results, calibration):
    model = spec["model"]
    shipped = [r for r in results if r["delivered"] and r["delivered"].startswith("Assets")]
    out = ["# AI-cloned announcer lines",
           "",
           "**The files listed here are not recordings.** They are AI-generated speech in the announcer's voice, made on %s"
           " with a voice-cloning model run locally. The announcer, a member of the team, agreed to their voice being cloned for"
           " new announcer lines in this game. The eleven recorded takes (`vo_count_*`, `vo_clock_*`, `vo_match_*`) are the"
           " announcer's own voice and are not in this list." % spec["date"],
           "",
           "- **Model:** %s, `%s` at revision `%s` (%s), run with %s. Licence: %s. Its output carries Resemble AI's Perth"
           " watermark (inaudible), which marks it as generated." % (model["name"], model["repo"], model["revision"], model["weights"],
                                                                      model["package"], model["licence"]),
           "- **Reference:** the eleven recorded takes, joined in this order: %s." % ", ".join("`vo_%s`" % n for n in spec["reference_takes"]),
           "- **Checked with:** OpenAI Whisper base (Apache-2.0), locally, and the model's own voice encoder. Nothing was sent to an online service.",
           "- **Made by:** `tools/clone_announcer_lines.py` from `tools/arena_announcer_lines.json`. Every candidate and its scores:"
           " `Logs/arena/voice/index.html` (not committed).",
           "- **To replace one with a real recording:** record the line (docs/HUMAN.md), save it under the same file name, rerun"
           " `py -3 tools/synth_arena_crowd_sfx.py --no-vo --only pa`, and delete its row here. No code changes.",
           "- **Keep `--no-vo` on that tool** while these files are here: without it the crowd's voices are built from every"
           " file in this folder, cloned ones included.",
           "- The stadium versions in `Resources/ArenaPa/pa_<file>.wav` are these files through the PA filter, so they are generated too.",
           "",
           calibration,
           "",
           "| File | Line (as written) | Caption | Said to the model as | Settings | Heard by Whisper | asr | sim | margin | Rating |",
           "|---|---|---|---|---|---|---|---|---|---|"]
    for r in shipped:
        c = r["chosen"]
        out.append("| `%s` | %s | %s | `%s` mode: \"%s\" | exaggeration %.1f, cfg %.1f, seed %d | \"%s\" | %.2f | %.3f | %+.3f | %s%s |" % (
            Path(r["delivered"]).name, r["text"], r["caption"], c["mode"], c["spoken"], c["exaggeration"], c["cfg"], c["seed"],
            c["transcript"], c["asr"], c["sim"], c["margin"], c["rating"], " (picked by hand)" if c.get("picked") else ""))
    missing = [r for r in results if not r["delivered"]]
    if missing:
        out += ["", "## Not delivered", "", "No candidate passed for these, so there is no file and the line stays silent:", ""]
        out += ["- `vo_%s_%d` (\"%s\")" % (r["id"], r["n"], r["text"]) for r in missing]
    (VO / "AI_CLONED_LINES.md").write_text("\n".join(out) + "\n", encoding="utf-8")


# ------------------------------------------------------------------ main

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    ap.add_argument("--more", type=int, default=0)
    ap.add_argument("--deliver-only", action="store_true")
    args = ap.parse_args()

    import soundfile as sf
    spec = json.loads(LINES.read_text(encoding="utf-8"))
    OUT.mkdir(parents=True, exist_ok=True)
    state = OUT / "state.json"
    known = json.loads(state.read_text(encoding="utf-8")) if state.exists() else {}
    rounds = 1 + args.more

    models = None
    if not args.deliver_only:
        models = Models(spec["reference_takes"])
        known["same"] = models.same
    same = known.get("same", [])
    calibration = ("On the speaker measure (`sim`, cosine with the centroid of the eleven recorded takes), each recorded take"
                   " against the other ten scores %.2f to %.2f (mean %.2f). `margin` is `sim` minus what the model's built-in voice"
                   " scores saying the same words." % (min(same), max(same), float(np.mean(same)))) if same else ""

    results, report = [], []
    for line in spec["lines"]:
        taken = set()
        for take in line["takes"]:
            key = "%s_%d" % (line["id"], take["n"])
            if key in HUMAN:
                raise SystemExit(key + " is a recorded take and is never overwritten")
            folder = OUT / "candidates" / key
            wired = not line["wiring"].startswith("none")
            target = (VO if wired else OUT / "unwired") / ("vo_" + key + ".wav")
            if args.only and args.only not in line["id"]:
                # Not this run's: keep what is on record for the page and the manifest.
                if key in known.get("results", {}):
                    results.append(known["results"][key])
                continue
            if models is not None:
                # A take with the same words as an earlier one gets its own seeds through its key.
                generate(models, key, line, take, max(rounds, known.get("rounds", {}).get(key, 1)), folder)
                known.setdefault("rounds", {})[key] = max(rounds, known.get("rounds", {}).get(key, 1))
            cache = folder / "scores.json"
            if not cache.exists():
                print("%-24s no candidates yet" % key)
                continue
            rows, chosen = choose(json.loads(cache.read_text(encoding="utf-8")), take, taken)
            delivered = None
            if chosen is not None and (chosen["rating"] != "FAILED" or chosen.get("picked")):
                raw, _ = sf.read(str(folder / (chosen["stem"] + ".wav")))
                x = deliver(raw)
                target.parent.mkdir(parents=True, exist_ok=True)
                sf.write(str(target), x, GAME_SR, subtype="PCM_16")
                delivered = str(target.relative_to(ROOT)).replace("\\", "/")
                chosen["peak_db"], chosen["rms_db"] = [round(v, 2) for v in level(x)]
                chosen["delivered_seconds"] = round(len(x) / GAME_SR, 3)
                taken.add(chosen["stem"])
            else:
                chosen = None
            # Nothing of an unwired or undelivered take stays in Resources.
            if not wired or chosen is None:
                for stale in (VO / ("vo_" + key + ".wav"), PA / ("pa_vo_" + key + ".wav")) + (() if wired or chosen else (target,)):
                    for gone in (stale, stale.with_name(stale.name + ".meta")):
                        if gone.exists():
                            gone.unlink()
            sheet(OUT / "sheets" / (key + ".png"), "%s  \"%s\"  [%s]" % (key, take["text"], line["caption"]), rows, chosen, folder)
            r = {"key": key, "id": line["id"], "n": take["n"], "text": take["text"], "caption": line["caption"], "when": line["when"],
                 "wiring": line["wiring"], "rows": rows, "chosen": chosen, "delivered": delivered,
                 "rating": chosen["rating"] if chosen else "FAILED"}
            results.append(r)
            known.setdefault("results", {})[key] = r
            best = chosen or (rows[0] if rows else None)
            text = "%-22s %-8s %-26s" % (key, r["rating"] if chosen else "FAILED", "\"" + take["text"] + "\"")
            if best:
                text += " %s  heard \"%s\" asr %.2f sim %.3f margin %+.3f  %.2f s nuclei %d/%d %s" % (
                    best["stem"], best["transcript"], best["asr"], best["sim"], best["margin"], best["seconds"], best["nuclei"],
                    best["syllables"], ("-> " + delivered) if delivered else "(best of %d, not delivered: %s)" % (
                        len(rows), ", ".join(best["flags"]) or "below the thresholds"))
            print(text)
            report.append(text)

    state.write_text(json.dumps(known, indent=1, ensure_ascii=False), encoding="utf-8")
    page(results, calibration)
    manifest(spec, results, calibration)
    (OUT / "report.txt").write_text(calibration + "\n\n" + "\n".join(report) + "\n", encoding="utf-8")
    print("\n" + calibration)
    print("Audition: %s" % (OUT / "index.html"))
    print("Now bake the stadium versions: py -3 tools/synth_arena_crowd_sfx.py --no-vo --only pa")


if __name__ == "__main__":
    main()
