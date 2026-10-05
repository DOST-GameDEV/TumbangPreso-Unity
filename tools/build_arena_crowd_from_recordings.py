"""The Arena's crowd, cut from real crowd recordings.

  py -3 tools/build_arena_crowd_from_recordings.py              (fetch what is missing, then build every cue)
  py -3 tools/build_arena_crowd_from_recordings.py --only ooh   (any substring of the cue names)
  py -3 tools/build_arena_crowd_from_recordings.py --fetch      (only download and check the sources)
  py -3 tools/build_arena_crowd_from_recordings.py --analyse    (pictures and numbers of every SOURCE)
  py -3 tools/build_arena_crowd_from_recordings.py --report     (pictures and numbers of every CUE written)

Owner, 2026-10-05, on the synthesised crowd (tools/synth_arena_crowd_sfx.py): "the crowd just sounds
like noise and not actual crowd cheers. like i dont hear any crowd chanting, reactions to certain
happenings in the game". He approved 28 CC0 recordings (Freesound's preview CDN and Wikimedia
Commons). This cuts the crowd cues out of them, under the names Runtime/Map/ArenaCrowdAudio.cs
already plays. The synthesiser still owns the four PA stings and the announcer's stadium takes
(`--only pa`); it must not be run without `--only pa` again or it writes its crowd over this one.

WHAT IS WHERE.
  tools/arena_crowd_sources.json    the 28 sources (id, page, URL, author, place, licence, SHA-256)
                                    and the CUT LIST: every cue, its recipe, its source seconds.
                                    This script only executes it: change a number there and rerun.
  ~/.cache/tump-audio/crowd-src/    the downloads. NOT in the repository (no LFS, about 80 MB).
  Assets/TumbangPreso/Resources/Sfx/ARENA_CROWD_SOURCES.md   the same provenance for people, written
                                    by this script from the json (never edit it by hand).
  Logs/arena/audio/sources/         --analyse: one picture and one table per source.
  Logs/arena/audio/recorded/        --report: one picture per cue, and report.txt.

NOBODY HAS HEARD ANY OF IT. The cuts were chosen from the analysis below, by a machine that cannot
listen: a spectrogram, a loudness curve, an onset/beat measure, a tonal measure (sustained narrow
spectral peaks: music, an organ, a horn, a jingle) and a speech measure (syllable-rate modulation
of the voice band: a PA announcer or one close voice). The json marks every cue whose source span
could not be cleared with confidence (`audition`), and ARENA_CROWD_SOURCES.md lists them first.

THE RECIPES (the `build` of a cue in the json):
  loop     one span of one source made into a seamless loop: the span's tail is crossfaded, equal
           power, into the audio just BEFORE the span's start, so the join is continuous audio.
  layers   one-shot: spans laid at offsets, each with its own gain, pitch, fades and filters.
  claps    single unison claps cut out of a source, sequenced to a rhythm and multiplied into a
           crowd (many copies, each with its own timing slop, level and pitch).
`bowl` on a layer is a LIGHT send into the synthesiser's bowl impulse (near rows): only the dry
studio takes get it. The stadium recordings already have their own stadium.

Deterministic: fixed seeds, no time, no randomness outside numpy's seeded generators. numpy, scipy,
soundfile (libsndfile 1.1 or later, which decodes MP3), matplotlib for the pictures.
"""
import argparse
import hashlib
import json
import sys
import time
import urllib.request
import wave
from pathlib import Path

import numpy as np
from scipy import signal

SR = 44100
PEAK = 0.85
ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "tools/arena_crowd_sources.json"
OUT = ROOT / "Assets/TumbangPreso/Resources/Sfx"
PEOPLE = OUT / "ARENA_CROWD_SOURCES.md"
CACHE = Path.home() / ".cache/tump-audio/crowd-src"
SRC_REPORT = ROOT / "Logs/arena/audio/sources"
CUE_REPORT = ROOT / "Logs/arena/audio/recorded"
AGENT = "TumbangPresoArenaCrowd/1.0 (indie game, CC0 crowd recordings, one request at a time; thegrinchisthebest@gmail.com)"
CC0_MARK = "creativecommons.org/publicdomain/zero/1.0"
# An MP3 begins and ends with the encoder's padding (LAME: 1105 samples of nothing, and more at the
# end). Nothing is ever cut from the first or last of these seconds of an MP3.
MP3_GUARD = 0.06


# ------------------------------------------------------------------ the manifest and the downloads

def load_manifest():
    return json.loads(MANIFEST.read_text(encoding="utf-8"))


def save_manifest(m):
    MANIFEST.write_text(json.dumps(m, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")


def get(url):
    req = urllib.request.Request(url, headers={"User-Agent": AGENT})
    with urllib.request.urlopen(req, timeout=120) as f:
        return f.read()


def fetch(m, pause=1.5):
    """Download what is missing. A Freesound sound's page is read first and must still say CC0."""
    import soundfile as sf
    CACHE.mkdir(parents=True, exist_ok=True)
    for s in m["sources"]:
        path = CACHE / s["file"]
        if not path.exists():
            try:
                if s["origin"] == "freesound":
                    page = get(s["page"]).decode("utf-8", "replace")
                    time.sleep(pause)
                    if CC0_MARK not in page:
                        s["licence_confirmed"] = False
                        s["skipped"] = "its page no longer states Creative Commons 0"
                        print("SKIPPED %2d %s: page does not state CC0" % (s["n"], s["id"]))
                        continue
                    s["licence_confirmed"] = True
                data = get(s["url"])
                time.sleep(pause)
                path.write_bytes(data)
                s.pop("skipped", None)
            except Exception as e:                          # a failed URL is skipped and reported, never substituted
                s["skipped"] = "download failed: %s" % e
                print("SKIPPED %2d %s: %s" % (s["n"], s["id"], e))
                continue
        data = path.read_bytes()
        sha = hashlib.sha256(data).hexdigest()
        if s.get("sha256") and s["sha256"] != sha:
            print("WARNING %2d %s: SHA-256 differs from the manifest (the cuts may not land where they did)" % (s["n"], s["file"]))
        s["sha256"], s["bytes"] = sha, len(data)
        info = sf.info(str(path))
        s["seconds"] = round(info.frames / info.samplerate, 3)
        s["format"] = "%s, %d Hz, %d ch" % (info.format, info.samplerate, info.channels)
        print("%2d %-44s %9d bytes %7.1f s  %s" % (s["n"], s["file"], len(data), s["seconds"], s["format"]))
    save_manifest(m)


_decoded = {}


def source(m, n):
    """Source `n`, mono at 44.1 kHz, as float64. The two channels of a stereo source are averaged."""
    if n in _decoded:
        return _decoded[n]
    import soundfile as sf
    s = next(x for x in m["sources"] if x["n"] == n)
    path = CACHE / s["file"]
    if not path.exists():
        raise FileNotFoundError("source %d (%s) is not in %s: run --fetch" % (n, s["file"], CACHE))
    x, rate = sf.read(str(path), dtype="float64", always_2d=True)
    x = x.mean(axis=1)
    if rate != SR:
        g = np.gcd(SR, rate)
        x = signal.resample_poly(x, SR // g, rate // g)
    _decoded[n] = x
    return x


def span(m, n, start, end):
    """Seconds `start`..`end` of source `n`. Never the padded ends of an MP3."""
    x = source(m, n)
    s = next(v for v in m["sources"] if v["n"] == n)
    guard = MP3_GUARD if s["file"].endswith(".mp3") else 0.0
    total = len(x) / SR
    if start < guard or end > total - guard:
        raise ValueError("source %d: %.2f..%.2f is outside %.2f..%.2f" % (n, start, end, guard, total - guard))
    return x[int(round(start * SR)):int(round(end * SR))].copy()


# ------------------------------------------------------------------ listening without ears: a source

def measures(x):
    """Per 0.5 s: level (dB), tonal (how many narrow spectral peaks have stood for half a second,
    150 Hz to 6 kHz: music, a horn, an organ), speech (syllable-rate modulation depth of the voice
    band: an announcer or one close voice), beat (how regular the onsets are, 0..1: drumming,
    clapping in time, a chant), tempo (bpm of that regularity), centroid (Hz)."""
    nfft, hop = 4096, 1024
    f, t, Z = signal.stft(x, SR, nperseg=nfft, noverlap=nfft - hop, window="hann", padded=False, boundary=None)
    mag = np.abs(Z) + 1e-9
    db = 20 * np.log10(mag)
    frames = SR / hop

    # Tonal: a bin 10 dB over its neighbourhood (a 330 Hz median across frequency), held 0.5 s.
    env = signal.medfilt2d(db, kernel_size=(31, 1))
    lo, hi = np.searchsorted(f, 150.0), np.searchsorted(f, 6000.0)
    peak = (db[lo:hi] - env[lo:hi]) > 10.0
    k = max(1, int(frames * 0.5))
    held = signal.convolve2d(peak.astype(float), np.ones((1, k)) / k, mode="same") > 0.85
    tonal = held.sum(axis=0).astype(float)

    centroid = (f[:, None] * mag).sum(axis=0) / mag.sum(axis=0)
    level = 10 * np.log10((mag ** 2).mean(axis=0) + 1e-12)

    # Onsets: half-wave rectified spectral flux of the log spectrum.
    flux = np.maximum(0.0, np.diff(np.log1p(30 * mag), axis=1)).sum(axis=0)
    flux = np.concatenate([[0.0], flux])
    flux = flux - signal.medfilt(flux, 2 * int(frames * 0.4) + 1)
    flux = np.maximum(flux, 0.0)

    # Speech: the 300..3400 Hz envelope's 2..9 Hz modulation against its mean.
    vlo, vhi = np.searchsorted(f, 300.0), np.searchsorted(f, 3400.0)
    voice = np.sqrt((mag[vlo:vhi] ** 2).mean(axis=0))
    sos = signal.butter(2, [2.0, 9.0], btype="band", fs=frames, output="sos")
    mod = signal.sosfiltfilt(sos, voice)

    step = int(frames * 0.5)
    win = int(frames * 4.0)
    rows = []
    for a in range(0, len(t), step):
        c = slice(max(0, a - win // 2), min(len(t), a + win // 2))
        fl = flux[c]
        beat, bpm = 0.0, 0.0
        if fl.std() > 0 and len(fl) > win // 2:
            ac = np.correlate(fl - fl.mean(), fl - fl.mean(), "full")[len(fl) - 1:]
            ac = ac / (ac[0] + 1e-12)
            l0, l1 = int(frames * 0.25), min(len(ac) - 1, int(frames * 1.5))
            j = l0 + int(np.argmax(ac[l0:l1]))
            beat, bpm = float(ac[j]), 60.0 * frames / j
        vc = voice[c]
        rows.append((t[a], float(level[a:a + step].mean()), float(tonal[a:a + step].mean()),
                     float(mod[c].std() / (vc.mean() + 1e-12)), beat, bpm, float(centroid[a:a + step].mean())))
    return np.array(rows), (f, t, db)


def analyse(m, only=""):
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    SRC_REPORT.mkdir(parents=True, exist_ok=True)
    for s in m["sources"]:
        if s.get("skipped") or (only and only not in s["file"]):
            continue
        x = source(m, s["n"])
        rows, (f, t, db) = measures(x)
        stem = s["file"].rsplit(".", 1)[0]
        np.savetxt(SRC_REPORT / (stem + ".csv"), rows, fmt="%.2f", delimiter=",",
                   header="second,level_db,tonal_peaks,speech_mod,beat,bpm,centroid_hz", comments="")
        fig, ax = plt.subplots(5, 1, figsize=(max(10, min(40, len(x) / SR / 6)), 11), sharex=True,
                               gridspec_kw={"height_ratios": [4, 1, 1, 1, 1]})
        top = np.searchsorted(f, 8000.0)
        ax[0].pcolormesh(t[::2], f[:top:2], db[:top:2, ::2], vmin=db.max() - 80, vmax=db.max(), shading="auto", cmap="magma")
        ax[0].set_yscale("symlog", linthresh=400); ax[0].set_ylim(60, 8000); ax[0].set_ylabel("Hz")
        ax[0].set_title("%d  %s  (%s, %s)" % (s["n"], s["title"], s["author"], s["place"]), fontsize=9)
        for a, col, name in ((ax[1], 1, "level dB"), (ax[2], 2, "tonal peaks"), (ax[3], 3, "speech mod"), (ax[4], 4, "beat")):
            a.plot(rows[:, 0], rows[:, col], lw=0.8); a.set_ylabel(name, fontsize=8); a.grid(alpha=0.3)
        ax[4].set_xlabel("seconds")
        ax[4].xaxis.set_major_locator(plt.MultipleLocator(10 if len(x) / SR > 60 else 2 if len(x) / SR > 12 else 0.5))
        fig.tight_layout()
        fig.savefig(SRC_REPORT / (stem + ".png"), dpi=70)
        plt.close(fig)
        print("%2d %-40s %6.1f s  level %.1f..%.1f dB  tonal med %.1f max %.1f  speech med %.2f  beat med %.2f max %.2f" % (
            s["n"], stem, len(x) / SR, np.percentile(rows[:, 1], 5), np.percentile(rows[:, 1], 95),
            np.median(rows[:, 2]), rows[:, 2].max(), np.median(rows[:, 3]), np.median(rows[:, 4]), rows[:, 4].max()))


# ------------------------------------------------------------------ small tools

def sos(x, kind, cut, order=2):
    return signal.sosfilt(signal.butter(order, cut, btype=kind, fs=SR, output="sos"), x)


def fade(x, come, go):
    """Half-cosine in over `come` seconds and out over `go`."""
    x = x.copy()
    a, r = min(len(x) // 2, int(SR * come)), min(len(x) // 2, int(SR * go))
    if a > 0:
        x[:a] *= 0.5 - 0.5 * np.cos(np.pi * np.arange(a) / a)
    if r > 0:
        x[len(x) - r:] *= 0.5 + 0.5 * np.cos(np.pi * np.arange(r) / r)
    return x


def loud(x, seconds=0.4):
    """The RMS of the loudest `seconds` of it: what a one-shot is levelled by."""
    w = min(len(x), int(SR * seconds))
    c = np.concatenate([[0.0], np.cumsum(x ** 2)])
    return float(np.sqrt(((c[w:] - c[:-w]) / w).max()))


def soft(x):
    """Nothing over PEAK leaves here: a tall sample is bent from 0.6 up, never cut."""
    knee = 0.6
    over = np.abs(x) > knee
    x = x.copy()
    x[over] = np.sign(x[over]) * (knee + (PEAK - knee) * np.tanh((np.abs(x[over]) - knee) / (PEAK - knee)))
    return x


def repitch(x, ratio):
    """Faster and higher by `ratio` (a tape's speed), by band-limited resampling."""
    if abs(ratio - 1.0) < 1e-6:
        return x
    return signal.resample(x, max(8, int(round(len(x) / ratio))))


def bowl_wet(seed=4103, wet=0.45, slaps=((0.31, 0.28), (0.55, 0.18)), seconds=3.6):
    """The wet part of tools/synth_arena_crowd_sfx.py's NEAR_IR (the nearest rows' bowl): the same
    code and seed, without the dry sample, so a studio take can be given a LITTLE of the stadium."""
    rng = np.random.default_rng(seed)
    n = int(SR * seconds)
    t = np.arange(n) / SR
    noise = rng.standard_normal(n)
    low = sos(noise, "low", 400.0)
    high = sos(noise, "high", 3000.0)
    middle = noise - low - high
    tail = low * np.exp(-6.91 * t / 3.4) + middle * np.exp(-6.91 * t / 2.8) + high * np.exp(-6.91 * t / 1.3) * 0.7
    tail = sos(tail, "low", 5000.0, 1) * (1.0 - np.exp(-t / 0.05))
    tail /= np.sqrt((tail ** 2).sum())
    ir = tail * wet
    for at, gain in slaps:
        centre = int(SR * at)
        k = int(SR * 0.05)
        burst = sos(rng.standard_normal(k), "low", 3200.0) * np.exp(-0.5 * ((np.arange(k) - k / 2) / (SR * 0.007)) ** 2)
        burst /= np.sqrt((burst ** 2).sum())
        ir[centre - k // 2:centre - k // 2 + k] += burst * gain * 0.55
        ir[centre] += gain * 0.45
    return ir


_wet = None


def in_bowl(x, send, tail=1.8):
    """`x` with `send` of the bowl under it. The dry sound is untouched."""
    global _wet
    if _wet is None:
        _wet = bowl_wet()
    y = np.zeros(len(x) + int(SR * tail))
    y[:len(x)] = x
    y += send * signal.fftconvolve(x, _wet)[:len(y)]
    return fade(y, 0.0, min(tail, 1.0))


def filtered(x, L, loop=False):
    """A layer's `highpass` and `lowpass` (Hz). A loop is filtered twice round, so it has no start."""
    def run(y):
        if L.get("highpass"):
            y = sos(y, "high", L["highpass"])
        if L.get("lowpass"):
            y = sos(y, "low", L["lowpass"], L.get("lowpass_order", 2))
        return y
    if not (L.get("highpass") or L.get("lowpass")):
        return x
    return run(np.concatenate([x, x]))[len(x):] if loop else run(x)


# ------------------------------------------------------------------ the three recipes

def one_loop(m, L, seconds):
    """`seconds` of a source from `from`, as a loop: its last `xfade` seconds are crossfaded (equal
    power) into the `xfade` seconds that come BEFORE `from` in the recording, so the sample after
    the loop's last is the recording's own next sample."""
    xf = L.get("xfade", 2.0)
    x = span(m, L["src"], L["from"] - xf, L["from"] + seconds)
    k = int(round(xf * SR))
    n = int(round(seconds * SR))
    pre, body = x[:k], x[k:k + n].copy()
    t = np.linspace(0.0, 1.0, k, endpoint=False)
    body[n - k:] = body[n - k:] * np.cos(t * np.pi / 2) + pre * np.sin(t * np.pi / 2)
    body = filtered(body - body.mean(), L, loop=True)
    return body / np.sqrt((body ** 2).mean()) * L.get("gain", 1.0)


def build_loop(m, c):
    out = sum(one_loop(m, L, c["seconds"]) for L in c["layers"])
    out = out - out.mean()
    out = out * c["rms"] / np.sqrt((out ** 2).mean())
    return soft(out)


def one_layer(m, L):
    x = span(m, L["src"], L["from"], L["to"])
    x = repitch(x, L.get("pitch", 1.0))
    x = filtered(x, L)
    x = fade(x, L.get("fade_in", 0.03), L.get("fade_out", 0.3))
    x = x / (loud(x) + 1e-12) * L.get("gain", 1.0)
    if L.get("bowl"):
        x = in_bowl(x, L["bowl"])
    return x


def lay(out, x, at):
    a = int(round(at * SR))
    if a + len(x) > len(out):
        out = np.concatenate([out, np.zeros(a + len(x) - len(out))])
    out[a:a + len(x)] += x
    return out


def level_shot(out, c):
    out = sos(out, "high", 40.0)                      # no DC, no rumble under the voices
    out = fade(out, c.get("fade_in", 0.0), c.get("fade_out", 0.0))
    return soft(out * c.get("level", 0.25) / (loud(out) + 1e-12))


def build_layers(m, c):
    out = np.zeros(1)
    for L in c["layers"]:
        out = lay(out, one_layer(m, L), L.get("at", 0.0))
    return level_shot(out, c)


def find_claps(x, least=0.16):
    """Where each clap of a clean unison-clapping recording begins (samples)."""
    env = np.abs(sos(x, "high", 900.0))
    env = signal.sosfiltfilt(signal.butter(2, 60.0, btype="low", fs=SR, output="sos"), env)
    floor = np.percentile(env, 60)
    peaks, _ = signal.find_peaks(env, height=max(floor * 4.0, env.max() * 0.18), distance=int(SR * least))
    starts = []
    for p in peaks:
        a = max(0, p - int(SR * 0.04))
        seg = env[a:p + 1]
        # The onset: the last place before the peak the envelope was under a fifth of it.
        under = np.nonzero(seg < env[p] * 0.2)[0]
        starts.append(a + (int(under[-1]) if len(under) else 0))
    return starts


def clap_pool(m, src):
    x = span(m, src["src"], src["from"], src["to"])
    starts = find_claps(x)
    pool = []
    for i, a in enumerate(starts):
        b = min(len(x), a + int(SR * 0.28))
        if i + 1 < len(starts):
            b = min(b, starts[i + 1] - int(SR * 0.012))
        one = x[max(0, a - int(SR * 0.004)):b]
        if len(one) < int(SR * 0.08):
            continue
        one = fade(one, 0.002, 0.06)
        pool.append(one / (np.abs(one).max() + 1e-12))
    return pool


def build_claps(m, c):
    """A rhythm of single unison claps (and stomps: the same claps slowed and low-passed), played
    by `people` copies, each late or early by its own slop, at its own level, pitch and distance."""
    rng = np.random.default_rng(c["seed"])
    pool = clap_pool(m, c["claps"])
    if len(pool) < 4:
        raise ValueError("%s: only %d claps found in source %d" % (c["name"], len(pool), c["claps"]["src"]))
    c["_claps_found"] = len(pool)

    hits = []                                         # (seconds, kind, accent)
    for bar in range(c["bars"]):
        for at, kind, accent in c["pattern"]:
            hits.append((0.3 + bar * c["bar_seconds"] + at, kind, accent))
    total = 0.3 + c["bars"] * c["bar_seconds"] + 0.6
    out = np.zeros(int(SR * total))
    slop = c.get("slop", 0.022)
    come, go = c.get("join_bars", 1.5) * c["bar_seconds"], c.get("leave_bars", 1.0) * c["bar_seconds"]
    last = 0.3 + c["bars"] * c["bar_seconds"]
    for person in range(c["people"]):
        late = rng.normal(0.0, slop * 0.6)
        gain = rng.uniform(0.08, 1.0) ** 2.2
        pitch = rng.uniform(0.9, 1.12)
        far = rng.uniform(1800.0, 9000.0)
        joins = rng.uniform(0.0, come)
        leaves = last - rng.uniform(0.0, go) if rng.random() < 0.6 else last
        for at, kind, accent in hits:
            if at < joins or at > leaves or rng.random() < 0.06:    # not in yet, gone, or missed this one
                continue
            one = pool[int(rng.integers(len(pool)))]
            if kind == "stomp":
                # A foot on the stand: the clap two and a half times slower, only its low end,
                # with a little of a second one's knock (slower by a third) over it.
                thud = sos(repitch(one, pitch * 0.4), "low", 260.0) * 2.4
                knock = sos(repitch(pool[int(rng.integers(len(pool)))], pitch * 0.66), "low", 900.0) * 0.3
                one = thud
                one[:min(len(one), len(knock))] += knock[:min(len(one), len(knock))]
            else:
                one = sos(repitch(one, pitch), "low", far, 1)
            out = lay(out, one * gain * accent, max(0.0, at + late + rng.normal(0.0, slop)))
    out = out / (loud(out) + 1e-12)
    for L in c.get("layers", []):                     # a real crowd under and after the rhythm
        out = lay(out, one_layer(m, L), L.get("at", 0.0))
    if c.get("bowl"):
        out = in_bowl(out, c["bowl"])
    return level_shot(out, c)


RECIPES = {"loop": build_loop, "layers": build_layers, "claps": build_claps}


# ------------------------------------------------------------------ listening without ears: a cue

def seam_score(x):
    """The join of a loop against any other place in it (tools/synth_arena_crowd_sfx.py's measure):
    the spectral likeness of the 20 ms either side of the join, the same at 200 places inside (median
    and 5th percentile), the waveform's step at the join against the median step between samples,
    and the level's step across the join (a second each side, dB: a loop that steps in loudness pumps)."""
    w = int(SR * 0.02)

    def likeness(a, b):
        A = np.abs(np.fft.rfft(a * np.hanning(len(a)))); B = np.abs(np.fft.rfft(b * np.hanning(len(b))))
        return float((A * B).sum() / (np.sqrt((A ** 2).sum() * (B ** 2).sum()) + 1e-12))
    at_join = likeness(x[-w:], x[:w])
    rng = np.random.default_rng(1)
    inside = [likeness(x[k - w:k], x[k:k + w]) for k in rng.integers(w, len(x) - w, 200)]
    step = abs(float(x[0] - x[-1])) / (float(np.median(np.abs(np.diff(x)))) + 1e-12)
    step_db = 20 * np.log10(np.sqrt((x[:SR] ** 2).mean()) / np.sqrt((x[-SR:] ** 2).mean()))
    return at_join, float(np.median(inside)), float(np.percentile(inside, 5)), step, float(step_db)


def crowd_shape(x):
    """What says "people" and not "noise": the share of the energy between 300 Hz and 4 kHz, and
    the TEXTURE of that band (how much its 50 ms level moves, as a coefficient of variation: steady
    filtered noise is near 0.05, a crowd of voices is several times that)."""
    f, p = signal.welch(x, SR, nperseg=8192)
    share = float(p[(f >= 300) & (f <= 4000)].sum() / p[f >= 40].sum())
    v = sos(sos(x, "high", 300.0), "low", 4000.0)
    w = int(SR * 0.05)
    lv = np.sqrt((v[:len(v) // w * w].reshape(-1, w) ** 2).mean(axis=1))
    lv = lv[lv > lv.max() * 0.05]
    return share, float(lv.std() / (lv.mean() + 1e-12))


def check(name, x, loop):
    row = dict(name=name, seconds=len(x) / SR, peak=float(np.abs(x).max()), rms=float(np.sqrt((x ** 2).mean())),
               loud=loud(x), dc=float(x.mean()), clipped=int((np.abs(x) >= 0.999).sum()))
    row["share"], row["texture"] = crowd_shape(x)
    if loop:
        row["seam"] = seam_score(x)
    return row


def write(path, x):
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(np.round(x * 32767).astype(np.int16).tobytes())


def pictures(rows, sounds):
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    CUE_REPORT.mkdir(parents=True, exist_ok=True)
    for row in rows:
        x = sounds[row["name"]]
        fig, ax = plt.subplots(2, 1, figsize=(11, 5), sharex=True, gridspec_kw={"height_ratios": [1, 3]})
        ax[0].plot(np.arange(len(x)) / SR, x, lw=0.3); ax[0].set_ylim(-1, 1); ax[0].set_title(row["name"], fontsize=9)
        f, t, Z = signal.stft(x, SR, nperseg=2048, noverlap=1536)
        db = 20 * np.log10(np.abs(Z) + 1e-9)
        top = np.searchsorted(f, 9000.0)
        ax[1].pcolormesh(t, f[:top], db[:top], vmin=db.max() - 75, vmax=db.max(), shading="auto", cmap="magma")
        ax[1].set_yscale("symlog", linthresh=400); ax[1].set_ylim(60, 9000)
        fig.tight_layout(); fig.savefig(CUE_REPORT / (row["name"] + ".png"), dpi=70); plt.close(fig)


def report_text(rows):
    lines = ["%-34s %6s %6s %6s %6s %7s %5s %6s %7s  %s" % (
        "cue", "sec", "peak", "rms", "loud", "dc", "clip", "voice", "texture", "loop: join / inside median / 5th pct / step / level step dB")]
    for r in rows:
        seam = ""
        if "seam" in r:
            seam = "%.3f / %.3f / %.3f / %.1f / %+.2f" % r["seam"]
        lines.append("%-34s %6.2f %6.3f %6.3f %6.3f %7.4f %5d %6.2f %7.2f  %s" % (
            r["name"], r["seconds"], r["peak"], r["rms"], r["loud"], r["dc"], r["clipped"], r["share"], r["texture"], seam))
    return lines


# ------------------------------------------------------------------ the provenance, for people

def spans_of(c):
    """Every (source, from, to) a cue was cut from."""
    out = []
    if c["build"] == "loop":
        for L in c["layers"]:
            out.append((L["src"], L["from"] - L.get("xfade", 2.0), L["from"] + c["seconds"]))
    else:
        if c["build"] == "claps":
            out.append((c["claps"]["src"], c["claps"]["from"], c["claps"]["to"]))
        for L in c.get("layers", []):
            out.append((L["src"], L["from"], L["to"]))
    return out


def write_people(m):
    used = {}
    for c in m["cues"]:
        for n, a, b in spans_of(c):
            used.setdefault(n, []).append("`%s` %.2f..%.2f s" % (c["name"], a, b))
    L = ["# The Arena's crowd: where every recording came from", "",
         "Written by `tools/build_arena_crowd_from_recordings.py` from `tools/arena_crowd_sources.json`. Do not edit by hand.", "",
         "Every crowd cue of the Arena (`sfx_arena_crowd_*` and `sfx_arena_chant_*`) is cut from the recordings below. All",
         "were published by their authors under Creative Commons 0 1.0 (a public domain dedication), as stated on each",
         "sound's own page when it was fetched on 2026-10-05. CC0 asks for no credit; the authors are named here anyway.",
         "The downloads are not in the repository (`~/.cache/tump-audio/crowd-src/`): the script fetches them again and",
         "checks each against its SHA-256. Freesound files are the site's high-quality MP3 previews, not the originals.", "",
         "Nobody has listened to any cue yet. The cuts were chosen by measurement (spectrogram, loudness, beat, tonal and",
         "speech measures, in `Logs/arena/audio/sources/`), which cannot recognise a club's song or a spoken word.", "",
         "## Audition these first", "",
         "Cues whose source span could not be cleared with confidence of music, a public address voice, one close voice or",
         "an identifiable club chant:", ""]
    flagged = [c for c in m["cues"] if c.get("audition")]
    L += ["### Highest risk", ""]
    for c in flagged:
        if c.get("audition_first"):
            L.append("- `%s`: %s" % (c["name"], c["audition"]))
    L += ["", "### The rest (a real club's crowd, or a small studio group, that measured clean)", ""]
    for c in flagged:
        if not c.get("audition_first"):
            L.append("- `%s`: %s" % (c["name"], c["audition"]))
    if not flagged:
        L.append("- none")
    L += ["", "## Still to be recorded by the team", "",
          "The game's own word chants cannot be cut from another crowd. Their synthesised versions were rejected and deleted;",
          "`ArenaCrowdAudio.Recorded` plays the cue in the last column in their place. To add one: record a group (ten or more",
          "voices, several passes layered), save it as `Resources/Sfx/<name>.wav` (44.1 kHz mono 16 bit) and put `<name>` back",
          "in `AudioCues.Live` with a `TrimDb` row of about -3.", "",
          "| Cue | Words | Rhythm | Plays meanwhile |", "|---|---|---|---|"]
    for r in m.get("to_be_recorded", []):
        L.append("| `%s` | %s | %s | `%s` |" % (r["name"], r["say"], r["rhythm"], r["meanwhile"]))
    L += ["", "## The cues", "", "| Cue | Seconds | Recipe | Cut from (source: seconds from..to) | What it is |", "|---|---|---|---|---|"]
    for c in m["cues"]:
        cut = "; ".join("%d: %.2f..%.2f" % s for s in spans_of(c))
        L.append("| `%s` | %.1f | %s | %s | %s |" % (c["name"], c.get("_seconds", 0.0), c["build"], cut, c.get("what", "")))
    L += ["", "## The sources", ""]
    for s in m["sources"]:
        L += ["### %d. %s" % (s["n"], s["title"]), "",
              "- Id: %s %s" % ("Freesound" if s["origin"] == "freesound" else "Wikimedia Commons", s["id"]),
              "- Page: %s" % s["page"], "- File fetched: %s" % s["url"],
              "- Author: %s" % s["author"], "- Place: %s" % s["place"],
              "- Licence as stated: %s%s" % (s["licence"], " (the page was read before the download and states it)" if s.get("licence_confirmed") else ""),
              "- Fetched: %s, %d bytes, %.1f s, %s" % (s["fetched"], s["bytes"], s["seconds"], s.get("format", "")),
              "- SHA-256: `%s`" % s["sha256"]]
        if s.get("skipped"):
            L.append("- NOT DOWNLOADED: %s" % s["skipped"])
        if s.get("note"):
            L.append("- Note: %s" % s["note"])
        L.append("- Used by: %s" % ("; ".join(used[s["n"]]) if s["n"] in used else "nothing"))
        L.append("")
    PEOPLE.write_text("\n".join(L), encoding="utf-8", newline="\n")


def build(m, only, report):
    OUT.mkdir(parents=True, exist_ok=True)
    rows, sounds = [], {}
    for c in m["cues"]:
        if only and only not in c["name"]:
            continue
        x = RECIPES[c["build"]](m, c)
        c["_seconds"] = round(len(x) / SR, 2)
        write(OUT / (c["name"] + ".wav"), x)
        row = check(c["name"], x, c["build"] == "loop")
        rows.append(row); sounds[c["name"]] = x
    lines = report_text(rows)
    print("\n".join(lines))
    if not only:
        save_manifest(m)
        write_people(m)
    if report:
        pictures(rows, sounds)
        (CUE_REPORT / "report.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    ap.add_argument("--fetch", action="store_true")
    ap.add_argument("--analyse", action="store_true")
    ap.add_argument("--report", action="store_true")
    args = ap.parse_args()
    m = load_manifest()
    missing = [s for s in m["sources"] if not (CACHE / s["file"]).exists() and not s.get("skipped")]
    if args.fetch or missing:
        fetch(m)
        if args.fetch:
            return
    if args.analyse:
        analyse(m, args.only)
        return
    build(m, args.only, args.report)


if __name__ == "__main__":
    main()
