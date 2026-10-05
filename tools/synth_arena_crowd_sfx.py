"""The Arena's crowd and its public address: the sound of 26,000 people in an open-roofed bowl.

  py -3 tools/synth_arena_crowd_sfx.py                    (everything)
  py -3 tools/synth_arena_crowd_sfx.py --only chant       (any substring of the file names)
  py -3 tools/synth_arena_crowd_sfx.py --report           (also the pictures and numbers in Logs/arena/audio/)
  py -3 tools/synth_arena_crowd_sfx.py --no-vo            (no grains of the recorded announcer in the crowd)

THE CROWD CUES BELOW ARE NO LONGER WRITTEN BY THIS TOOL (2026-10-05). The owner rejected the synthesised
crowd ("just sounds like noise and not actual crowd cheers"); every sfx_arena_crowd_* and sfx_arena_chant_*
file is now cut from real recordings by tools/build_arena_crowd_from_recordings.py. This tool still writes
the four sfx_arena_pa_* stings and the announcer's stadium takes (`--no-vo --only pa`, unchanged), and it
skips every crowd cue unless `--synthetic-crowd` is given. The recipes stay as a record.

Owner, 2026-10-05, after playing the map: "there should be reverbey crowd cheers, chants, and an
announcer". Until now the map had one 3.6 s roar (tools/synth_arena_show_sfx.py) and nothing
continuous. Runtime/Map/ArenaCrowdAudio.cs plays what this writes; the mix level of each is its row
in Runtime/Audio/AudioCues.cs.

HOW A CROWD IS MADE HERE. Not from one band of noise. A POOL of several hundred separate voices is
synthesised first (a glottal pulse train with its own pitch, drift and jitter, plus breath, through
three formant resonators in a vowel's shape: talk in syllables, shouts, long "wooo"s, open roars,
laughs, finger whistles). A bed is then a few thousand of those voices laid down at random times,
each resampled to its own pitch and given its own level (most of them far and quiet, a few near),
over a wash of vocal-band noise that stands for the thousands nobody can pick out. Claps are
hundreds of clappers, each a filtered noise burst repeating at its own rate with its own jitter.
A chant is the same voices gated to a rhythm, every voice with its own timing slop, its own pitch
and its own moment of joining in and falling away. Drums, the torotot (the plastic horn a Filipino
crowd brings) and a pea whistle are small physical-ish synths.

THE RECORDED ANNOUNCER IS ALSO IN THE CROWD, AS GRAINS (unless --no-vo). The eleven delivered
announcer takes in Resources/Vo are the only human voices in the repository. A few thousand short
windowed grains of them, each at its own pitch, are scattered under the synthetic voices: real
throats multiplied sound far more like a crowd than any synthesis. No word survives it (a grain is
90 to 260 ms, hundreds overlap, and the bowl's reverb follows). Say so to the people who recorded
them; `--no-vo` builds the same files from synthesis alone.

THE BOWL. Everything is convolved with a synthesised impulse response of the stadium: a diffuse
tail (3.4 s in the lows, 2.8 s in the middle, 1.3 s at the top, as the open air eats the highs), and
the slap off the far stands (they are 85 to 180 m from the stage: 0.29 s and 0.53 s). There are three
of them: the far thousands' (mostly tail), the nearest rows' and a chanting section's (mostly
straight, some tail), and the PA's (straight, with the strongest slap). A bed is the far crowd and
the near one mixed, because a crowd that is all tail is wind. A LOOP is
convolved CIRCULARLY (in the frequency domain, at the loop's own length), and every voice laid into
it wraps round its end, so its last sample runs into its first with the tail intact: there is no
seam to crossfade.

THE PUBLIC ADDRESS. The existing announcer takes are also written out as the stadium would play
them (band limited 320 to 3800 Hz, a presence peak, a touch of drive, then the bowl with a stronger
slap): Resources/ArenaPa/pa_<take>.wav, 22050 Hz (nothing above 4 kHz is left in them).
ArenaCrowdAudio plays those in place of the dry takes ON THIS MAP ONLY. A NEW RECORDING DROPPED INTO
Resources/Vo NEEDS THIS RUN AGAIN (`--no-vo --only pa`) or it plays dry. The map's new lines (a list
in ArenaCrowdAudio.cs) are AI-CLONED TAKES of the announcer's voice, made with their consent on
2026-10-05 by tools/clone_announcer_lines.py and listed in Resources/Vo/AI_CLONED_LINES.md; a line
with no file is marked by one of the four `sfx_arena_pa_*` stings below. ONLY THE ELEVEN RECORDED
TAKES (`RECORDED`) ARE EVER USED AS THE CROWD'S GRAINS: a cloned take gets its PA version and
nothing else.

Writes into Assets/TumbangPreso/Resources/Sfx/ (44100 Hz, mono, 16 bit). One-shots are normalised
to 0.85 peak like every other cue; the five beds are normalised by RMS (a bed is mixed by its
loudness, not by its tallest sample) and checked to stay under 0.85:

  BEDS (seamless loops)
    sfx_arena_crowd_bed_calm      22.0 s  the murmur: thousands talking, a far shout now and then
    sfx_arena_crowd_bed_lively    18.0 s  the same crowd awake: shouts, "wooo"s, scattered claps, whistles
    sfx_arena_crowd_bed_roar      12.0 s  everybody up: open roars, screams, whistles, applause under it
    sfx_arena_crowd_bed_tension   10.0 s  a held breath: a low "oooo" from thousands, the talk hushed
    sfx_arena_crowd_bed_applause  10.0 s  applause: 420 clappers, a few cheers over it
  REACTIONS
    sfx_arena_crowd_erupt          6.4 s  the eruption: a knocked can, the reveal, the match's end
    sfx_arena_crowd_cheer          3.4 s  a smaller cheer: a block, a bank shot, a round beginning
    sfx_arena_crowd_ooh            3.2 s  the collective "ooooh", falling: a tag, a fall into the shaft
    sfx_arena_crowd_aww            2.4 s  "ohhh!" up and down: a near miss
    sfx_arena_crowd_gasp           2.0 s  a rising "ooo" as a throw is in the air, cut off unresolved
    sfx_arena_crowd_laugh          3.8 s  laughter: the balloon is hit
  CHANTS (each comes up, runs and dies away inside the file)
    sfx_arena_chant_tumbang_preso 12.2 s  "TUM-BANG! PRE-SO!" clap clap, clap-clap-clap, three times
    sfx_arena_chant_taya           8.7 s  "TA-YA! TA-YA!" six calls over a bass drum
    sfx_arena_chant_tumba          8.3 s  "TUM-BA! TUM-BA!" seven calls speeding up as the can is threatened
    sfx_arena_chant_stomp         10.4 s  stomp-stomp-CLAP on the stands, six times
    sfx_arena_chant_ooh_hey        4.6 s  a slow building "oooooooh" and a "HEY!" on a throw lined up
    sfx_arena_chant_drums          9.8 s  a bass drum, torotot horns and a whistle from one section
    sfx_arena_chant_horns          3.2 s  three torotot blasts answered by a whistle
  PUBLIC ADDRESS STINGS (through the PA and the bowl)
    sfx_arena_pa_chime             3.6 s  ding-dong: the stage is about to change
    sfx_arena_pa_organ             4.9 s  the organ's "charge" riff: a rally in a lull, the match's start
    sfx_arena_pa_horn              3.2 s  an air horn: the balloon is popped
    sfx_arena_pa_fanfare           3.5 s  three brass notes: a fallen player is brought back

numpy and scipy only, fixed seeds, nothing downloaded: a rerun is byte-identical (with the same
takes in Resources/Vo). NOBODY HAS HEARD ANY OF IT: `--report` is as far as a machine can listen."""
import argparse
import wave
from pathlib import Path

import numpy as np
from scipy import signal

SR = 44100
PEAK = 0.85
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/TumbangPreso/Resources/Sfx"
VO = ROOT / "Assets/TumbangPreso/Resources/Vo"
PA_OUT = ROOT / "Assets/TumbangPreso/Resources/ArenaPa"
PA_SR = 22050
REPORT = ROOT / "Logs/arena/audio"

VOWELS = {
    "a": ((780, 1250, 2600), (1.0, 0.70, 0.30)),
    "e": ((520, 1850, 2550), (1.0, 0.55, 0.30)),
    "i": ((310, 2250, 2950), (1.0, 0.40, 0.25)),
    "o": ((500, 900, 2500), (1.0, 0.65, 0.18)),
    "u": ((340, 780, 2350), (1.0, 0.45, 0.12)),
    "n": ((270, 1100, 2400), (1.0, 0.12, 0.05)),      # a nasal: the M of TUM, the NG of BANG
}
BANDWIDTH = (90.0, 120.0, 170.0)


# ------------------------------------------------------------------ small tools

def sos_filter(x, kind, cut, order=2):
    return signal.sosfilt(signal.butter(order, cut, btype=kind, fs=SR, output="sos"), x)


def band(x, low, high, order=2):
    return sos_filter(x, "band", [low, high], order)


def lowpass(x, cut, order=2):
    return sos_filter(x, "low", cut, order)


def highpass(x, cut, order=2):
    return sos_filter(x, "high", cut, order)


def slow(rng, n, rate):
    """A smooth random line, about `rate` turns a second, mean 0 and of unit size."""
    points = max(3, int(n / SR * rate) + 3)
    return np.interp(np.linspace(0, points - 1, n), np.arange(points), rng.standard_normal(points))


def window(n, attack, release):
    """1, coming up over `attack` seconds and going down over `release`, as half cosines."""
    env = np.ones(n)
    a, r = min(n // 2, max(1, int(SR * attack))), min(n // 2, max(1, int(SR * release)))
    env[:a] = 0.5 - 0.5 * np.cos(np.pi * np.arange(a) / a)
    env[n - r:] = 0.5 + 0.5 * np.cos(np.pi * np.arange(r) / r)
    return env


def add(out, x, at, wrap):
    """Lay `x` into `out` from sample `at`. A loop's voices wrap round its end."""
    n = len(out)
    if wrap:
        at %= n
        first = min(len(x), n - at)
        out[at:at + first] += x[:first]
        rest = x[first:]
        while len(rest) > 0:
            part = min(len(rest), n)
            out[:part] += rest[:part]
            rest = rest[part:]
    elif at < n:
        at = max(0, at)
        part = min(len(x), n - at)
        out[at:at + part] += x[:part]


def unit(x):
    peak = np.abs(x).max()
    return x / peak if peak > 0 else x


# ------------------------------------------------------------------ one voice

def glottal(phase):
    """The flow through the folds over one cycle, differentiated: a pulse train whose harmonics
    fall away at about 12 dB an octave, as a throat's do."""
    g = phase % 1.0
    opening = 0.58
    flow = np.where(g < opening, 0.5 - 0.5 * np.cos(np.pi * g / opening),
                    np.where(g < opening + 0.14, np.cos(0.5 * np.pi * (g - opening) / 0.14), 0.0))
    return np.diff(flow, prepend=flow[0])


def formant(x, vowel, scale):
    freqs, gains = VOWELS[vowel]
    out = np.zeros(len(x))
    for f, g, bw in zip(freqs, gains, BANDWIDTH):
        f = min(f * scale, SR * 0.45)
        b, a = signal.iirpeak(f, f / bw, fs=SR)
        out += signal.lfilter(b, a, x) * g
    return out


def voiced(rng, f0, scale, vowels, masks, breath, bright=1.0):
    """A sung or shouted sound. `f0` is the pitch in Hz, a sample each; `vowels` and `masks` say
    which vowel shape holds when (the masks sum to 1)."""
    n = len(f0)
    jitter = 1.0 + 0.012 * slow(rng, n, 14.0)
    source = unit(glottal(np.cumsum(f0 * jitter) / SR))
    air = highpass(rng.standard_normal(n), 1500.0) * breath * 0.5
    x = source * (1.0 + 0.08 * slow(rng, n, 9.0)) + air
    out = np.zeros(n)
    for vowel, mask in zip(vowels, masks):
        out += formant(x, vowel, scale) * mask
    if bright < 1.0:
        out = lowpass(out, 1100.0 + 3900.0 * bright)
    return out


def person(rng):
    """A throat: its speaking pitch and how much shorter than a grown man's its tract is."""
    kind = rng.random()
    if kind < 0.50:
        return rng.uniform(95, 150), rng.uniform(0.96, 1.05)          # a man
    if kind < 0.88:
        return rng.uniform(170, 250), rng.uniform(1.12, 1.22)         # a woman
    return rng.uniform(240, 320), rng.uniform(1.25, 1.38)             # a child


def talk(rng):
    """A few syllables of talk: nobody's words, only their rhythm and their vowels."""
    base, scale = person(rng)
    count = int(rng.integers(3, 9))
    durations = rng.uniform(0.11, 0.24, count)
    gaps = rng.uniform(0.01, 0.06, count)
    n = int(SR * (durations.sum() + gaps.sum())) + 64
    f0 = np.zeros(n)
    env = np.zeros(n)
    kinds = list(rng.choice(["a", "e", "i", "o", "u"], 3, replace=False))
    masks = [np.zeros(n) for _ in kinds]
    burst = np.zeros(n)
    at = 0
    for k in range(count):
        m = int(SR * durations[k])
        pitch = base * (1.0 - 0.12 * k / count) * rng.uniform(0.92, 1.16)
        f0[at:at + m] = pitch * (1.0 + 0.05 * np.sin(np.linspace(0, np.pi, m)))
        env[at:at + m] = window(m, 0.02, 0.04) * rng.uniform(0.5, 1.0)
        masks[int(rng.integers(3))][at:at + m] = 1.0
        if rng.random() < 0.4:
            c = int(SR * rng.uniform(0.02, 0.05))
            burst[at:at + c] += band(rng.standard_normal(c), 2000, 6000) * window(c, 0.003, 0.02) * 0.25
        at += m + int(SR * gaps[k])
    f0[f0 == 0] = base
    total = np.maximum(sum(masks), 1e-6)
    smooth = np.hanning(int(SR * 0.015))
    smooth /= smooth.sum()
    masks = [np.convolve(m / total, smooth, mode="same") for m in masks]
    return voiced(rng, f0, scale, kinds, masks, 0.08, rng.uniform(0.42, 0.7)) * env + burst


def shout(rng):
    """One to three shouted syllables: "hoy", "go", "wey"."""
    base, scale = person(rng)
    base *= rng.uniform(1.7, 2.3)
    count = int(rng.integers(1, 4))
    parts = []
    for _ in range(count):
        m = int(SR * rng.uniform(0.18, 0.5))
        t = np.linspace(0, 1, m)
        f0 = base * rng.uniform(0.9, 1.2) * (1.0 + 0.18 * np.sin(np.pi * t) - 0.10 * t)
        first, second = rng.choice(["a", "e", "o", "i"], 2, replace=False)
        y = voiced(rng, f0, scale, [first, second], [1.0 - t, t], 0.22) * window(m, 0.015, 0.08)
        parts.append(y)
        parts.append(np.zeros(int(SR * rng.uniform(0.02, 0.12))))
    return np.concatenate(parts)


def woo(rng):
    """A long "wooo" that climbs and comes down."""
    _, scale = person(rng)
    m = int(SR * rng.uniform(0.7, 2.2))
    t = np.linspace(0, 1, m)
    low, high = rng.uniform(330, 480), rng.uniform(540, 820)
    f0 = low + (high - low) * np.sin(np.pi * np.clip(t * 1.25, 0, 1)) ** 0.7 + 12.0 * slow(rng, m, 5.0)
    blend = np.sin(np.pi * t)
    return voiced(rng, f0, max(scale, 1.12), ["u", "o"], [1.0 - 0.6 * blend, 0.6 * blend], 0.12) * window(m, 0.08, 0.3)


def aah(rng):
    """An open roar, rough with breath, held."""
    base, scale = person(rng)
    m = int(SR * rng.uniform(1.0, 2.8))
    t = np.linspace(0, 1, m)
    f0 = base * rng.uniform(1.5, 2.2) * (1.0 + 0.05 * slow(rng, m, 2.0) - 0.08 * t)
    other = rng.choice(["e", "o"])
    lean = 0.35 * (0.5 + 0.5 * slow(rng, m, 1.0).clip(-1, 1))
    tremor = 1.0 + 0.18 * np.sin(2 * np.pi * rng.uniform(5, 8) * t * m / SR + rng.uniform(0, 6.28))
    return voiced(rng, f0, scale, ["a", other], [1.0 - lean, lean], 0.45) * window(m, rng.uniform(0.1, 0.3), 0.4) * tremor


def ooh(rng, seconds, fall, vowels=("o", "u"), rise=0.0):
    """One voice of a collective "ooooh": held, and falling (or, with `rise`, climbing first)."""
    base, scale = person(rng)
    m = int(SR * seconds * rng.uniform(0.8, 1.1))
    t = np.linspace(0, 1, m)
    shape = 1.0 + rise * np.sin(np.pi * np.clip(t * 2.2, 0, 1)) - fall * t ** 1.3
    f0 = base * rng.uniform(1.25, 1.7) * shape * (1.0 + 0.015 * slow(rng, m, 3.0))
    return voiced(rng, f0, scale, list(vowels), [1.0 - t, t], 0.2, 0.8) * window(m, 0.12, 0.45 * seconds)


def laugh(rng):
    """Ha-ha-ha: bursts at five or so a second, stepping down."""
    base, scale = person(rng)
    count = int(rng.integers(4, 10))
    rate = rng.uniform(4.3, 6.2)
    step = int(SR / rate)
    out = np.zeros(step * count + int(SR * 0.2))
    vowel = rng.choice(["a", "e", "a"])
    pitch = base * rng.uniform(1.5, 2.1)
    for k in range(count):
        m = int(step * rng.uniform(0.5, 0.72))
        t = np.linspace(0, 1, m)
        f0 = pitch * (1.0 - 0.045 * k) * (1.08 - 0.16 * t)
        y = voiced(rng, f0, scale, [vowel], [np.ones(m)], 0.5) * window(m, 0.012, 0.06)
        puff = highpass(rng.standard_normal(m), 2500.0) * window(m, 0.004, 0.05) * 0.06      # the H
        add(out, (y + puff) * (1.0 - 0.06 * k), k * step + int(rng.normal(0, step * 0.04)), False)
    return out


def whistle(rng):
    """A finger whistle: a pure tone that bends, with a little air."""
    m = int(SR * rng.uniform(0.3, 1.2))
    t = np.linspace(0, 1, m)
    low, high = rng.uniform(1700, 2400), rng.uniform(2500, 3300)
    f0 = low + (high - low) * (np.sin(np.pi * t) if rng.random() < 0.5 else t ** 0.6)
    f0 = f0 * (1.0 + 0.012 * np.sin(2 * np.pi * rng.uniform(5, 8) * t * m / SR))
    tone = np.sin(2 * np.pi * np.cumsum(f0) / SR)
    air = band(rng.standard_normal(m), 1800, 5000) * 0.12
    return (tone + air) * window(m, 0.02, 0.08)


def clap(rng):
    """One pair of hands: a burst of noise with that pair's own ring."""
    m = int(SR * rng.uniform(0.012, 0.03))
    centre = rng.uniform(900, 2800)
    y = band(rng.standard_normal(m + 64), centre * 0.6, min(centre * 1.9, 9000))[64:]
    return unit(y * np.exp(-np.arange(m) / SR * rng.uniform(140, 320)))


def build_pool(seed):
    rng = np.random.default_rng(seed)
    pool = {
        "talk": [talk(rng) for _ in range(220)],
        "shout": [shout(rng) for _ in range(140)],
        "woo": [woo(rng) for _ in range(70)],
        "aah": [aah(rng) for _ in range(110)],
        "laugh": [laugh(rng) for _ in range(60)],
        "whistle": [whistle(rng) for _ in range(30)],
        "clap": [clap(rng) for _ in range(64)],
    }
    for name in pool:
        pool[name] = [unit(x) for x in pool[name]]
    return pool


# The announcer's own recordings (docs/HUMAN.md). Every other vo_* file is an AI-cloned take.
RECORDED = ("vo_clock_10_1", "vo_clock_30_1", "vo_count_1_1", "vo_count_2_1", "vo_count_3_1", "vo_count_go_1",
            "vo_count_go_2", "vo_match_draw_1", "vo_match_draw_2", "vo_match_win_1", "vo_match_win_2")


def load_vo():
    """The delivered announcer takes, as floats at this file's rate (they are 44100 Hz mono)."""
    takes = []
    for path in sorted(VO.glob("vo_*.wav")):
        with wave.open(str(path), "rb") as w:
            if w.getsampwidth() != 2:
                continue
            x = np.frombuffer(w.readframes(w.getnframes()), dtype="<i2").astype(np.float64) / 32768.0
            if w.getnchannels() > 1:
                x = x.reshape(-1, w.getnchannels()).mean(axis=1)
            if w.getframerate() != SR:
                x = signal.resample_poly(x, SR, w.getframerate())
            takes.append((path.stem, x))
    return takes


# ------------------------------------------------------------------ many voices

def scatter(rng, n, voices, count, gain=(0.05, 1.0), pitch=(0.86, 1.18), wrap=True, when=None, power=2.5):
    """`count` voices from a pool laid into `n` samples: each at its own pitch, most of them far
    and quiet (`power`), at a random time or at the time `when` gives."""
    out = np.zeros(n)
    for _ in range(count):
        u = voices[int(rng.integers(len(voices)))]
        r = rng.uniform(*pitch)
        m = int(len(u) / r)
        if m < 8:
            continue
        y = np.interp(np.arange(m) * r, np.arange(len(u)), u)
        g = gain[0] + (gain[1] - gain[0]) * rng.random() ** power
        at = int(rng.integers(n)) if when is None else int(when(rng) * SR)
        add(out, y * g, at, wrap)
    return out


def grains(rng, n, takes, count, wrap=True, when=None):
    """Short windowed grains of the recorded takes, each at its own pitch: real throats, no words."""
    out = np.zeros(n)
    if not takes:
        return out
    for _ in range(count):
        x = takes[int(rng.integers(len(takes)))][1]
        m = int(SR * rng.uniform(0.09, 0.26))
        if len(x) <= m + 2:
            continue
        start = int(rng.integers(len(x) - m))
        r = rng.uniform(0.72, 1.38)
        k = int(m / r)
        y = np.interp(np.arange(k) * r, np.arange(m), x[start:start + m]) * np.hanning(k)
        at = int(rng.integers(n)) if when is None else int(when(rng) * SR)
        add(out, y * (0.1 + 0.9 * rng.random() ** 2.5), at, wrap)
    return out


def wash(rng, n, tilt=0.0):
    """The thousands nobody can pick out: noise in the shape of a great many voices, built in the
    frequency domain so a loop of it has no seam, breathing slowly on whole turns of the loop."""
    spectrum = np.fft.rfft(rng.standard_normal(n))
    f = np.maximum(np.fft.rfftfreq(n, 1.0 / SR), 1.0)
    octave = np.log2(f)
    shape = (np.exp(-(octave - np.log2(620.0)) ** 2 / (2 * 0.85 ** 2))
             + 0.5 * np.exp(-(octave - np.log2(1500.0 + 600.0 * tilt)) ** 2 / (2 * 0.5 ** 2))
             + (0.05 + 0.10 * tilt) * np.exp(-(octave - np.log2(3600.0)) ** 2 / (2 * 0.6 ** 2)))
    shape[f < 90.0] *= (f[f < 90.0] / 90.0) ** 2
    x = np.fft.irfft(spectrum * shape, n)
    t = np.arange(n) / n
    breathe = 1.0
    for turns in (1, 2, 3, 5, 8, 13):
        breathe = breathe + 0.07 * np.sin(2 * np.pi * (turns * t + rng.random()))
    return unit(x) * breathe


def clappers(rng, n, pool, people, wrap=True, start=None, stop=None, rate=(2.8, 5.2)):
    """Applause: every clapper claps at their own rate with their own jitter. `start` and `stop`
    (seconds, each a function of the random stream) bound a clapper in a one-shot."""
    out = np.zeros(n)
    for _ in range(people):
        sample = pool[int(rng.integers(len(pool)))]
        level = 0.08 + 0.92 * rng.random() ** 3
        period = SR / rng.uniform(*rate)
        at = rng.uniform(0, period) + (start(rng) * SR if start else 0.0)
        end = stop(rng) * SR if stop else n
        while at < end:
            add(out, sample * level * rng.uniform(0.7, 1.0), int(at), wrap)
            at += period * rng.uniform(0.9, 1.1)
    return out


# ------------------------------------------------------------------ the bowl

def bowl_ir(seed, dry, wet, slaps, seconds=3.6):
    """The stadium's impulse response: the dry sound, the slap off the far stands, and a diffuse
    tail that lasts longest in the lows."""
    rng = np.random.default_rng(seed)
    n = int(SR * seconds)
    t = np.arange(n) / SR
    noise = rng.standard_normal(n)
    low = lowpass(noise, 400.0)
    high = highpass(noise, 3000.0)
    middle = noise - low - high
    tail = low * np.exp(-6.91 * t / 3.4) + middle * np.exp(-6.91 * t / 2.8) + high * np.exp(-6.91 * t / 1.3) * 0.7
    # A hundred metres of open air: the whole tail leans dark, and is not there at once.
    tail = lowpass(tail, 5000.0, 1) * (1.0 - np.exp(-t / 0.05))
    tail /= np.sqrt((tail ** 2).sum())
    ir = tail * wet
    for at, gain in slaps:
        centre = int(SR * at)
        m = int(SR * 0.05)
        burst = lowpass(rng.standard_normal(m), 3200.0) * np.exp(-0.5 * ((np.arange(m) - m / 2) / (SR * 0.007)) ** 2)
        burst /= np.sqrt((burst ** 2).sum())
        ir[centre - m // 2:centre - m // 2 + m] += burst * gain * 0.55
        ir[centre] += gain * 0.45
    ir[0] += dry
    return ir


CROWD_IR = bowl_ir(4101, 0.6, 1.0, ((0.31, 0.35), (0.55, 0.25)))
PA_IR = bowl_ir(4102, 1.0, 0.55, ((0.29, 0.50), (0.53, 0.30)))
# The people in the nearest rows, and a section that is chanting: more of them arrives straight.
NEAR_IR = bowl_ir(4103, 1.0, 0.45, ((0.31, 0.28), (0.55, 0.18)))


def in_bowl_loop(x, ir=CROWD_IR):
    """A loop in the bowl: convolved circularly, so its tail runs round into its own start."""
    n = len(x)
    folded = np.zeros(n)
    for start in range(0, len(ir), n):
        part = ir[start:start + n]
        folded[:len(part)] += part
    return np.fft.irfft(np.fft.rfft(x) * np.fft.rfft(folded), n)


def in_bowl(x, ir=CROWD_IR, tail=2.4):
    """A one-shot in the bowl: its tail kept for `tail` seconds and faded out over the last of it."""
    y = signal.fftconvolve(x, ir)[:len(x) + int(SR * tail)]
    out = int(SR * min(tail, 1.2))
    y[-out:] *= 0.5 + 0.5 * np.cos(np.pi * np.arange(out) / out)
    return y


# ------------------------------------------------------------------ the beds

def round_the_loop(x, filtered):
    """A filter run over a loop: twice round, keeping the second lap, so it has no start."""
    return filtered(np.concatenate([x, x]))[len(x):]


def near_and_far(far, near, share, air=0.0):
    """A bed is two crowds: the far thousands, deep in the bowl, and the few hundred in the
    nearest rows, who arrive almost dry and are the ones an ear picks out as people. `air` is
    where the distance starts taking the far crowd's top away, in Hz."""
    far = unit(far)
    if air > 0.0:
        far = unit(round_the_loop(far, lambda x: lowpass(x, air, 1)))
    return in_bowl_loop(far) + in_bowl_loop(unit(near), NEAR_IR) * share


def bed_calm(rng, pool, takes):
    n = int(SR * 22.0)
    far = unit(scatter(rng, n, pool["talk"], 2600, (0.04, 1.0), power=3.0) + grains(rng, n, takes, 2400) * 0.9) + wash(rng, n) * 0.30
    near = scatter(rng, n, pool["talk"], 110, (0.10, 1.0), power=1.6)
    near += scatter(rng, n, pool["shout"], 14, (0.15, 0.7), power=1.5)
    near += scatter(rng, n, pool["laugh"], 12, (0.15, 0.7), power=1.5)
    near += scatter(rng, n, pool["whistle"], 2, (0.05, 0.12))
    return near_and_far(far, near, 1.1, 2800.0)


def bed_lively(rng, pool, takes):
    n = int(SR * 18.0)
    far = scatter(rng, n, pool["talk"], 1500, (0.04, 0.8), power=3.0)
    far += grains(rng, n, takes, 2600)
    far += scatter(rng, n, pool["shout"], 480, (0.05, 1.0))
    far += scatter(rng, n, pool["woo"], 140, (0.05, 0.8))
    far += scatter(rng, n, pool["aah"], 90, (0.05, 0.5))
    far += clappers(rng, n, pool["clap"], 90) * 0.5
    far = unit(far) + wash(rng, n, 0.4) * 0.35
    near = scatter(rng, n, pool["shout"], 70, (0.15, 1.0), power=1.4)
    near += scatter(rng, n, pool["woo"], 26, (0.15, 1.0), power=1.4)
    near += scatter(rng, n, pool["talk"], 90, (0.15, 0.8), power=1.3)
    near += scatter(rng, n, pool["whistle"], 9, (0.05, 0.25))
    near += clappers(rng, n, pool["clap"], 14) * 0.5
    return near_and_far(far, near, 0.9, 4200.0)


def bed_roar(rng, pool, takes):
    n = int(SR * 12.0)
    far = scatter(rng, n, pool["aah"], 900, (0.05, 1.0))
    far += scatter(rng, n, pool["woo"], 520, (0.05, 1.0))
    far += scatter(rng, n, pool["shout"], 620, (0.05, 0.9))
    far += grains(rng, n, takes, 2200) * 0.8
    far += scatter(rng, n, pool["whistle"], 50, (0.03, 0.3))
    far += clappers(rng, n, pool["clap"], 260) * 0.7
    far = unit(far) + wash(rng, n, 1.0) * 0.55
    near = scatter(rng, n, pool["aah"], 50, (0.15, 1.0), power=1.4)
    near += scatter(rng, n, pool["woo"], 50, (0.15, 1.0), power=1.4)
    near += scatter(rng, n, pool["shout"], 90, (0.15, 1.0), power=1.4)
    near += scatter(rng, n, pool["whistle"], 12, (0.05, 0.3))
    near += clappers(rng, n, pool["clap"], 30) * 0.6
    return near_and_far(far, near, 0.6)


def bed_tension(rng, pool, takes):
    n = int(SR * 10.0)
    hum = [unit(ooh(rng, 3.2, 0.03, ("u", "o"))) for _ in range(60)]
    far = scatter(rng, n, hum, 420, (0.05, 1.0), pitch=(0.7, 1.0))
    far += round_the_loop(scatter(rng, n, pool["talk"], 500, (0.03, 0.5), power=3.0), lambda x: lowpass(x, 1400.0)) * 0.6
    far += round_the_loop(grains(rng, n, takes, 700), lambda x: lowpass(x, 1600.0)) * 0.5
    far = unit(far) + round_the_loop(wash(rng, n), lambda x: lowpass(x, 900.0)) * 0.35
    near = round_the_loop(scatter(rng, n, pool["talk"], 40, (0.15, 0.8), power=1.3), lambda x: lowpass(x, 2200.0))
    return near_and_far(far, near, 0.4)


def bed_applause(rng, pool, takes):
    n = int(SR * 10.0)
    far = unit(clappers(rng, n, pool["clap"], 420))
    far += unit(scatter(rng, n, pool["woo"], 70, (0.05, 0.7))) * 0.30
    far += unit(scatter(rng, n, pool["shout"], 120, (0.05, 0.7))) * 0.25
    far += unit(grains(rng, n, takes, 700) + 1e-9) * 0.2
    far = far + wash(rng, n, 0.6) * 0.2
    near = clappers(rng, n, pool["clap"], 26)
    near += scatter(rng, n, pool["whistle"], 5, (0.03, 0.12))
    near += scatter(rng, n, pool["woo"], 8, (0.1, 0.5))
    return near_and_far(far, near, 0.55)


# ------------------------------------------------------------------ the reactions

def swell(n, rise, hold, fall):
    """An eruption's shape over `n` samples: up in `rise` seconds, held, then falling away."""
    t = np.arange(n) / SR
    return np.clip(t / rise, 0, 1) ** 1.3 * np.exp(-np.clip(t - rise - hold, 0, None) / fall)


def erupt(rng, pool, takes):
    n = int(SR * 4.6)
    onset = lambda r: abs(r.normal(0.0, 0.16)) + (r.uniform(0.3, 2.6) if r.random() < 0.45 else 0.0)
    x = scatter(rng, n, pool["aah"], 700, (0.05, 1.0), wrap=False, when=onset)
    x += scatter(rng, n, pool["woo"], 360, (0.05, 1.0), wrap=False, when=onset)
    x += scatter(rng, n, pool["shout"], 520, (0.05, 1.0), wrap=False, when=onset)
    x += grains(rng, n, takes, 1500, wrap=False, when=lambda r: abs(r.normal(0.0, 0.2)) + r.uniform(0, 3.0)) * 0.7
    x += scatter(rng, n, pool["whistle"], 40, (0.03, 0.3), wrap=False, when=lambda r: r.uniform(0.3, 3.4))
    x += clappers(rng, n, pool["clap"], 300, False, lambda r: r.uniform(0.2, 1.4), lambda r: r.uniform(2.6, 4.4)) * 0.6
    x = unit(x) + wash(rng, n, 1.0) * 0.9
    return in_bowl(x * swell(n, 0.22, 1.9, 1.25), tail=1.8)


def cheer(rng, pool, takes):
    n = int(SR * 2.2)
    onset = lambda r: abs(r.normal(0.0, 0.14)) + (r.uniform(0.2, 1.0) if r.random() < 0.3 else 0.0)
    x = scatter(rng, n, pool["woo"], 160, (0.05, 1.0), wrap=False, when=onset)
    x += scatter(rng, n, pool["shout"], 260, (0.05, 1.0), wrap=False, when=onset)
    x += scatter(rng, n, pool["aah"], 120, (0.05, 0.8), wrap=False, when=onset)
    x += grains(rng, n, takes, 500, wrap=False, when=lambda r: r.uniform(0, 1.4)) * 0.6
    x += clappers(rng, n, pool["clap"], 160, False, lambda r: r.uniform(0.15, 0.8), lambda r: r.uniform(1.2, 2.1)) * 0.6
    x = unit(x) + wash(rng, n, 0.6) * 0.5
    return in_bowl(x * swell(n, 0.16, 0.7, 0.6), tail=1.2)


def many(rng, n, make, count, spread, gain=(0.05, 1.0)):
    """`count` freshly made voices all starting together, give or take `spread` seconds."""
    out = np.zeros(n)
    for _ in range(count):
        y = unit(make(rng))
        g = gain[0] + (gain[1] - gain[0]) * rng.random() ** 2.0
        add(out, y * g, int(abs(rng.normal(0.0, spread)) * SR), False)
    return out


def crowd_ooh(rng, pool, takes):
    """A tag, a fall: 260 voices on a falling "ooooh"."""
    n = int(SR * 2.4)
    x = many(rng, n, lambda r: ooh(r, 1.7, 0.24), 260, 0.11)
    x = unit(x) + wash(rng, n) * 0.35 * swell(n, 0.2, 0.6, 0.6)
    return in_bowl(x * swell(n, 0.14, 1.0, 0.6), tail=0.8)


def crowd_aww(rng, pool, takes):
    """A near miss: "ohhh!", up and straight back down, with the breath in it."""
    n = int(SR * 1.6)
    x = many(rng, n, lambda r: ooh(r, 0.95, 0.20, ("o", "a"), rise=0.16), 240, 0.07)
    air = band(rng.standard_normal(n), 900, 4200) * swell(n, 0.08, 0.1, 0.25) * 0.12
    return in_bowl((unit(x) + air) * swell(n, 0.07, 0.55, 0.4), tail=0.8)


def crowd_gasp(rng, pool, takes):
    """A throw in the air: a rising "ooo" that is cut off before it resolves."""
    n = int(SR * 1.25)
    def one(r):
        base, scale = person(r)
        m = int(SR * r.uniform(0.85, 1.15))
        t = np.linspace(0, 1, m)
        f0 = base * r.uniform(1.2, 1.6) * (1.0 + 0.30 * t ** 1.5)
        return voiced(r, f0, scale, ["u", "o"], [1.0 - t, t], 0.3, 0.8) * t ** 1.4 * window(m, 0.1, 0.07)
    x = many(rng, n, one, 220, 0.06)
    air = band(rng.standard_normal(n), 1200, 5000) * (np.arange(n) / n) ** 2 * 0.10
    return in_bowl((unit(x) + air) * window(n, 0.05, 0.10), tail=0.75)


def crowd_laugh(rng, pool, takes):
    n = int(SR * 2.8)
    onset = lambda r: abs(r.normal(0.0, 0.22)) + (r.uniform(0.3, 1.5) if r.random() < 0.4 else 0.0)
    x = scatter(rng, n, pool["laugh"], 340, (0.05, 1.0), pitch=(0.85, 1.25), wrap=False, when=onset)
    x += scatter(rng, n, pool["shout"], 30, (0.05, 0.4), wrap=False, when=onset)
    x += grains(rng, n, takes, 300, wrap=False, when=lambda r: r.uniform(0, 2.0)) * 0.3
    return in_bowl(unit(x) * swell(n, 0.2, 1.1, 0.7), tail=1.0)


# ------------------------------------------------------------------ the chants

def syllable(rng, f0, seconds, vowel, scale, onset="", nasal=False, accent=1.0):
    """One shouted syllable of a chant: a consonant's burst or hiss, the vowel, a nasal close."""
    n = max(int(SR * seconds), 256)
    t = np.arange(n) / SR
    pitch = f0 * (1.0 + 0.06 * accent * np.exp(-t / 0.07)) * (1.0 - 0.05 * t / seconds)
    close = np.clip((t / seconds - 0.62) / 0.22, 0, 1) if nasal else np.zeros(n)
    y = voiced(rng, pitch, scale, [vowel, "n"], [1.0 - close, close], 0.28)
    soft = onset in ("b", "y", "m", "h")
    y *= window(n, 0.03 if soft else 0.012, 0.05) * (1.0 - 0.45 * close)
    y = unit(y)
    if onset in ("t", "p", "k"):
        m = int(SR * 0.016)
        low, high = {"t": (3000, 7500), "p": (500, 2200), "k": (1500, 4500)}[onset]
        y[:m] += band(rng.standard_normal(m + 64), low, high)[64:] * np.exp(-np.arange(m) / SR * 260) * 0.5
    elif onset == "s":
        m = int(SR * 0.07)
        hiss = band(rng.standard_normal(m + 64), 4500, 9500)[64:] * window(m, 0.02, 0.02) * 0.35
        y = np.concatenate([hiss, y])
    elif onset == "h":
        m = int(SR * 0.04)
        y[:m] += highpass(rng.standard_normal(m + 64), 1800)[64:] * window(m, 0.005, 0.03) * 0.3
    return y * accent


def bass_drum(rng, weight=1.0):
    """A marching bass drum from the stands: the head's note falling, and the beater's slap."""
    m = int(SR * 0.42)
    t = np.arange(m) / SR
    tone = np.sin(2 * np.pi * np.cumsum(68.0 + 95.0 * np.exp(-t / 0.03)) / SR) * np.exp(-t / 0.11)
    second = np.sin(2 * np.pi * np.cumsum(151.0 + 60.0 * np.exp(-t / 0.03)) / SR) * np.exp(-t / 0.07) * 0.4
    skin = band(rng.standard_normal(m + 64), 400.0, 3200.0)[64:] * np.exp(-t / 0.014) * 0.7
    return (tone + second + skin) * weight


def torotot(rng, seconds, pitch=None):
    """The plastic horn: a harsh reed with two formants, sagging as the breath runs out."""
    m = int(SR * seconds)
    t = np.arange(m) / SR
    f0 = (pitch or rng.uniform(380, 520)) * (1.0 + 0.02 * np.exp(-t / 0.03) - 0.05 * (t / seconds) ** 2)
    phase = np.cumsum(f0) / SR
    reed = sum(np.sin(2 * np.pi * h * phase) / h ** 0.8 for h in range(1, 18))
    buzz = reed * (1.0 + 0.2 * np.sin(2 * np.pi * 31.0 * t)) + band(rng.standard_normal(m), 800, 4000) * 0.3
    y = band(buzz, 900, 1500) + band(buzz, 2200, 3200) * 0.7 + buzz * 0.25
    return unit(y * window(m, 0.02, 0.07))


def pea_whistle(rng, seconds):
    m = int(SR * seconds)
    t = np.arange(m) / SR
    f0 = 2950.0 + 170.0 * np.sin(2 * np.pi * 36.0 * t) + 60.0 * slow(rng, m, 3.0)
    y = np.sin(2 * np.pi * np.cumsum(f0) / SR) * (0.75 + 0.25 * np.sin(2 * np.pi * 36.0 * t + 1.0))
    return (y + band(rng.standard_normal(m), 2500, 6000) * 0.15) * window(m, 0.015, 0.05)


def stomp(rng):
    """A foot on a stand's deck: the thud, and the deck's rattle."""
    m = int(SR * 0.16)
    t = np.arange(m) / SR
    thud = band(rng.standard_normal(m + 128), 60, 220)[128:] * np.exp(-t / 0.04) * 2.0
    rattle = band(rng.standard_normal(m + 128), 250, 1400)[128:] * np.exp(-t / 0.022) * 1.0
    return unit(thud + rattle)


def chant(rng, pool, seconds, beats, words, hits, voices=130, hands=0, feet=0, stomps=(), drum=(), slop=0.022):
    """A chant from one section of the stands.

    `beats` is the time in seconds of every beat. `words` are (beat, length in beats, vowel, onset,
    nasal close, accent, semitones). `hits` are (beat, accent) for the hands, `stomps` the beats
    the feet come down on, `drum` (beat, weight) for a bass drum. Every voice has its own pitch,
    its own lateness (how far along the section it sits, plus its own sloppiness), and joins and
    leaves on its own: a third are in from the first word, the rest come in over the first half,
    and half of them fall away over the last quarter."""
    n = int(SR * seconds)
    total = len(beats) - 1
    when = lambda beat: float(np.interp(beat, np.arange(len(beats)), beats))
    end = float(beats[-1])

    def comes_and_goes(early):
        joins = 0.0 if rng.random() < early else rng.uniform(0.0, 0.5) * end
        leaves = end * (1.0 if rng.random() < 0.5 else rng.uniform(0.74, 1.0))
        return joins, leaves

    out = np.zeros(n)
    for _ in range(voices):
        base, scale = person(rng)
        base *= rng.uniform(1.45, 1.9)
        late = rng.uniform(0.0, 0.05) + rng.normal(0.0, slop)
        joins, leaves = comes_and_goes(0.33)
        level = 0.08 + 0.92 * rng.random() ** 2.2
        for beat, length, vowel, onset, nasal, accent, semis in words:
            at = when(beat)
            if at < joins or at > leaves:
                continue
            held = (when(min(beat + length, total)) - at) * rng.uniform(0.55, 0.75)
            y = syllable(rng, base * 2.0 ** (semis / 12.0), held, vowel, scale, onset, nasal, accent)
            add(out, y * level, int((at + late + rng.normal(0.0, 0.012)) * SR), False)
    if voices:
        out = unit(out)

    def together(samples, people, events, sloppiness, gain):
        layer = np.zeros(n)
        for _ in range(people):
            sample = samples[int(rng.integers(len(samples)))]
            late = rng.uniform(0.0, 0.05) + rng.normal(0.0, sloppiness)
            joins, leaves = comes_and_goes(0.4)
            level = 0.08 + 0.92 * rng.random() ** 2.5
            for beat, accent in events:
                at = when(beat)
                if joins <= at <= leaves:
                    add(layer, sample * level * accent, int((at + late + rng.normal(0.0, 0.01)) * SR), False)
        return unit(layer) * gain

    if hands and hits:
        out += together(pool["clap"], hands, hits, 0.018, 0.8)
    if feet and stomps:
        out += together([stomp(rng) for _ in range(12)], feet, [(beat, 1.0) for beat in stomps], 0.02, 0.6)
    for beat, weight in drum:
        add(out, bass_drum(rng, weight) * 0.4, int((when(beat) + 0.03) * SR), False)
    return out


def shape_chant(x, seconds, come=1.2, go=1.6):
    """Up out of the murmur and back into it, then the bowl."""
    n = len(x)
    t = np.arange(n) / SR
    env = np.clip(0.25 + 0.75 * t / come, 0, 1) * np.clip((seconds - t) / go, 0, 1) ** 0.8
    return in_bowl(unit(x) * env, NEAR_IR, tail=1.4)


def steady(bpm, count, start=0.15):
    return start + np.arange(count + 1) * 60.0 / bpm


def chant_tumbang_preso(rng, pool, takes):
    """TUM-BANG! PRE-SO! clap clap, clap-clap-clap. Three times, 138 to the minute."""
    beats = steady(138, 24)
    words, hits = [], []
    for rep in range(3):
        b = rep * 8
        words += [(b, 0.9, "u", "t", True, 0.9, 0), (b + 1, 1.0, "a", "b", True, 1.0, 3),
                  (b + 2, 0.8, "e", "p", False, 0.9, 0), (b + 3, 1.0, "o", "s", False, 1.0, -2)]
        hits += [(b + 4, 1.0), (b + 5, 1.0), (b + 6, 0.9), (b + 6.5, 0.9), (b + 7, 1.0)]
    seconds = float(beats[-1]) + 0.2
    return shape_chant(chant(rng, pool, seconds, beats, words, hits, voices=130, hands=180), seconds)


def chant_taya(rng, pool, takes):
    """TA-YA! TA-YA! Six calls, a bass drum under each TA."""
    beats = steady(104, 12)
    words, drum = [], []
    for rep in range(6):
        b = rep * 2
        words += [(b, 0.5, "a", "t", False, 0.9, 0), (b + 0.5, 1.0, "a", "y", False, 1.0, 4)]
        drum.append((b, 1.0 if rep > 0 else 0.6))
    seconds = float(beats[-1]) + 0.2
    return shape_chant(chant(rng, pool, seconds, beats, words, (), voices=140, drum=drum), seconds, 1.0, 1.5)


def chant_tumba(rng, pool, takes):
    """TUM-BA! TUM-BA! Seven calls that speed up from 96 to 168 a minute and get louder."""
    gaps = 60.0 / np.linspace(96, 168, 14)
    beats = 0.15 + np.concatenate([[0.0], np.cumsum(gaps)])
    words, hits = [], []
    for rep in range(7):
        b = rep * 2
        grow = 0.7 + 0.3 * rep / 6.0
        words += [(b, 0.6, "u", "t", True, 0.85 * grow, 0), (b + 0.6, 1.2, "a", "b", False, grow, 3 + rep * 0.25)]
        hits.append((b, grow))
    seconds = float(beats[-1]) + 0.15
    x = chant(rng, pool, seconds, beats, words, hits, voices=150, hands=120)
    return shape_chant(x * np.linspace(0.5, 1.0, len(x)) ** 1.5, seconds, 0.8, 0.5)


def chant_stomp(rng, pool, takes):
    """Stomp, stomp, CLAP. Six times, on the stands' own floor."""
    beats = steady(84, 12)
    stomps = [b for rep in range(6) for b in (rep * 2, rep * 2 + 0.5)]
    hits = [(rep * 2 + 1, 1.0) for rep in range(6)]
    seconds = float(beats[-1]) + 0.3
    x = chant(rng, pool, seconds, beats, (), hits, voices=0, hands=260, feet=240, stomps=stomps)
    return shape_chant(x, seconds, 1.4, 1.8)


def chant_ooh_hey(rng, pool, takes):
    """A slow building "oooooooh" over a throw being lined up, and a "HEY!" with a clap."""
    n = int(SR * 3.3)
    def rising(r):
        base, scale = person(r)
        m = int(SR * r.uniform(2.3, 2.6))
        t = np.linspace(0, 1, m)
        f0 = base * r.uniform(1.15, 1.45) * (1.0 + 0.42 * t ** 1.8)
        return voiced(r, f0, scale, ["o", "u"], [1.0 - 0.5 * t, 0.5 * t], 0.2, 0.85) * (0.15 + 0.85 * t ** 1.6) * window(m, 0.25, 0.05)
    x = unit(many(rng, n, rising, 240, 0.08))
    hey = np.zeros(n)
    for _ in range(220):
        base, scale = person(rng)
        y = syllable(rng, base * rng.uniform(1.8, 2.3), rng.uniform(0.28, 0.4), "e", scale, "h", False, 1.0)
        add(hey, y * (0.1 + 0.9 * rng.random() ** 2), int((2.62 + rng.uniform(0, 0.07) + rng.normal(0, 0.02)) * SR), False)
    claps = np.zeros(n)
    for _ in range(260):
        add(claps, pool["clap"][int(rng.integers(64))] * rng.random(), int((2.64 + rng.uniform(0, 0.08) + rng.normal(0, 0.02)) * SR), False)
    return in_bowl(x * 0.7 + unit(hey) + unit(claps) * 0.6, tail=1.3)


def chant_drums(rng, pool, takes):
    """One section's band: boom, boom, boom-boom-boom, the torotots answering, a whistle on top."""
    beats = steady(122, 16)
    n = int(SR * (float(beats[-1]) + 0.4))
    out = np.zeros(n)
    when = lambda beat: np.interp(beat, np.arange(len(beats)), beats)
    horns = [rng.uniform(390, 500) for _ in range(5)]
    for rep in range(4):
        b = rep * 4
        level = (0.55, 0.85, 1.0, 0.8)[rep]
        for beat, weight in ((b, 1.0), (b + 1, 0.9), (b + 2, 0.8), (b + 2.5, 0.8), (b + 3, 1.0)):
            add(out, bass_drum(rng, weight) * level * 0.55, int((when(beat) + rng.normal(0, 0.006)) * SR), False)
        if rep >= 1:
            for pitch in horns[:2 + rep]:
                for beat, length in ((b + 3.5, 0.22), (b + 4.0, 0.42)):
                    if beat < 16:
                        add(out, torotot(rng, length * rng.uniform(0.85, 1.1), pitch * rng.uniform(0.985, 1.015)) * 0.30 * level,
                            int((when(beat) + rng.normal(0, 0.02)) * SR), False)
        if rep in (1, 3):
            add(out, pea_whistle(rng, 0.5) * 0.16, int(when(b + 1.5) * SR), False)
    return shape_chant(out, n / SR - 0.4, 0.6, 1.2)


def chant_horns(rng, pool, takes):
    """Three torotot blasts from a handful of horns, and a whistle answering."""
    n = int(SR * 1.9)
    out = np.zeros(n)
    horns = [rng.uniform(380, 520) for _ in range(6)]
    for at, length in ((0.05, 0.22), (0.38, 0.22), (0.72, 0.6)):
        for pitch in horns:
            add(out, torotot(rng, length * rng.uniform(0.8, 1.1), pitch * rng.uniform(0.985, 1.015)) * rng.uniform(0.4, 1.0),
                int((at + rng.normal(0, 0.025)) * SR), False)
    add(out, pea_whistle(rng, 0.38) * 0.5, int(1.42 * SR), False)
    return in_bowl(unit(out), tail=1.3)


# ------------------------------------------------------------------ the public address

def through_pa(x, drive=2.0):
    """A horn loudspeaker a long way off: band limited, a presence peak, a touch of drive."""
    y = lowpass(highpass(unit(x), 320.0, 4), 3800.0, 4)
    b, a = signal.iirpeak(2000.0, 1.2, fs=SR)
    y = unit(y + signal.lfilter(b, a, y) * 0.6)
    # The drive bends the wave unevenly, which a loudspeaker cannot play: the offset is taken out again.
    return highpass(np.tanh(y * drive) / np.tanh(drive), 250.0, 2)


def pa_sting(x):
    return in_bowl(through_pa(x, 1.6), PA_IR, tail=2.0)


def bar(freq, seconds, t0, n):
    """A struck chime bar: its three partials, each ringing down at its own rate."""
    out = np.zeros(n)
    m = int(SR * seconds)
    t = np.arange(m) / SR
    y = (np.sin(2 * np.pi * freq * t) * np.exp(-t / 0.55) + np.sin(2 * np.pi * freq * 2.76 * t) * np.exp(-t / 0.18) * 0.35
         + np.sin(2 * np.pi * freq * 5.4 * t) * np.exp(-t / 0.06) * 0.2)
    add(out, y * window(m, 0.002, 0.1), int(SR * t0), False)
    return out


def pa_chime(rng, pool, takes):
    n = int(SR * 1.6)
    return pa_sting(bar(659.26, 1.0, 0.02, n) + bar(523.25, 1.1, 0.50, n))


def organ(freq, seconds):
    m = int(SR * seconds)
    t = np.arange(m) / SR
    vibrato = 1.0 + 0.004 * np.sin(2 * np.pi * 6.2 * t)
    phase = np.cumsum(freq * vibrato) / SR
    y = sum(np.sin(2 * np.pi * h * phase) * g for h, g in ((0.5, 0.5), (1, 1.0), (2, 0.8), (3, 0.55), (4, 0.5), (6, 0.3), (8, 0.25)))
    return y * window(m, 0.008, 0.03)


def pa_organ(rng, pool, takes):
    """The organ's "charge": G C E G, E, G held."""
    eighth = 60.0 / 152.0 / 2.0 * 1.5
    notes = ((392.0, 1), (523.25, 1), (659.26, 1), (783.99, 1.5), (659.26, 0.8), (783.99, 4.0))
    n = int(SR * (sum(length for _, length in notes) * eighth + 0.1))
    out = np.zeros(n)
    at = 0.03
    for freq, length in notes:
        add(out, organ(freq, length * eighth * 0.94), int(at * SR), False)
        at += length * eighth
    return pa_sting(out)


def pa_horn(rng, pool, takes):
    """An air horn: two reeds a major third apart, straight on and straight off."""
    m = int(SR * 1.15)
    t = np.arange(m) / SR
    out = np.zeros(m)
    for freq in (370.0, 466.2):
        phase = np.cumsum(freq * (1.0 - 0.03 * np.exp(-t / 0.05))) / SR
        out += sum(np.sin(2 * np.pi * h * phase) / h ** 0.7 for h in range(1, 22))
    out = out + band(out, 1000, 2400) * 0.8
    return pa_sting(out * window(m, 0.03, 0.09))


def pa_fanfare(rng, pool, takes):
    """Three brass notes, the last one held: C, C, G."""
    notes = ((523.25, 0.16, 0.03), (523.25, 0.16, 0.24), (783.99, 0.95, 0.45))
    n = int(SR * 1.5)
    out = np.zeros(n)
    for freq, seconds, at in notes:
        m = int(SR * seconds)
        t = np.arange(m) / SR
        bright = 0.35 + 0.65 * np.clip(t / 0.05, 0, 1) * np.exp(-t / 1.2)
        phase = np.cumsum(freq * (1.0 + 0.003 * np.sin(2 * np.pi * 5.5 * t))) / SR
        y = sum(np.sin(2 * np.pi * h * phase) * bright ** (h - 1) / h ** 0.5 for h in range(1, 14))
        add(out, y * window(m, 0.015, 0.06), int(at * SR), False)
    return pa_sting(out)


def announcer(takes):
    """Every delivered take as the stadium plays it: PA_SR mono, the take's own loudness kept."""
    PA_OUT.mkdir(parents=True, exist_ok=True)
    rows = []
    for name, x in takes:
        y = signal.fftconvolve(through_pa(x), PA_IR)[:len(x) + int(SR * 2.0)]
        out = int(SR * 1.0)
        y[-out:] *= 0.5 + 0.5 * np.cos(np.pi * np.arange(out) / out)
        # As loud through the body of the line as the dry take is (its energy is spread out now, so
        # the peak that gives that is higher), and never over the ceiling.
        y = y * np.sqrt((x ** 2).mean()) / np.sqrt((y[:len(x)] ** 2).mean())
        y = signal.resample_poly(y, PA_SR, SR)
        y = y * min(1.0, PEAK / np.abs(y).max())
        path = PA_OUT / ("pa_" + name + ".wav")
        write(path, y, PA_SR)
        rows.append((path, y, PA_SR))
        print("%-34s %.2f s  rms %.3f" % (path.name, len(y) / PA_SR, float(np.sqrt((y ** 2).mean()))))
    return rows


# ------------------------------------------------------------------ the list

# (name, build, seed, a loop's RMS or None for a one-shot at 0.85 peak)
SOUNDS = (
    ("sfx_arena_crowd_bed_calm", bed_calm, 4201, 0.14),
    ("sfx_arena_crowd_bed_lively", bed_lively, 4202, 0.15),
    ("sfx_arena_crowd_bed_roar", bed_roar, 4203, 0.17),
    ("sfx_arena_crowd_bed_tension", bed_tension, 4204, 0.13),
    ("sfx_arena_crowd_bed_applause", bed_applause, 4205, 0.14),
    ("sfx_arena_crowd_erupt", erupt, 4211, None),
    ("sfx_arena_crowd_cheer", cheer, 4212, None),
    ("sfx_arena_crowd_ooh", crowd_ooh, 4213, None),
    ("sfx_arena_crowd_aww", crowd_aww, 4214, None),
    ("sfx_arena_crowd_gasp", crowd_gasp, 4215, None),
    ("sfx_arena_crowd_laugh", crowd_laugh, 4216, None),
    ("sfx_arena_chant_tumbang_preso", chant_tumbang_preso, 4221, None),
    ("sfx_arena_chant_taya", chant_taya, 4222, None),
    ("sfx_arena_chant_tumba", chant_tumba, 4223, None),
    ("sfx_arena_chant_stomp", chant_stomp, 4224, None),
    ("sfx_arena_chant_ooh_hey", chant_ooh_hey, 4225, None),
    ("sfx_arena_chant_drums", chant_drums, 4226, None),
    ("sfx_arena_chant_horns", chant_horns, 4227, None),
    ("sfx_arena_pa_chime", pa_chime, 4231, None),
    ("sfx_arena_pa_organ", pa_organ, 4232, None),
    ("sfx_arena_pa_horn", pa_horn, 4233, None),
    ("sfx_arena_pa_fanfare", pa_fanfare, 4234, None),
)


def write(path, x, rate=SR):
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(rate)
        w.writeframes(np.round(x * 32767).astype(np.int16).tobytes())


def finish(x, loop_rms):
    x = x - x.mean()
    if loop_rms is None:
        return x / np.abs(x).max() * PEAK
    x = x * loop_rms / np.sqrt((x ** 2).mean())
    peak = np.abs(x).max()
    # A bed's rare tall sample is bent, never cut: nothing over 0.85 leaves here.
    if peak > PEAK:
        knee = 0.6
        over = np.abs(x) > knee
        x[over] = np.sign(x[over]) * (knee + (PEAK - knee) * np.tanh((np.abs(x[over]) - knee) / (PEAK - knee)))
    return x


# ------------------------------------------------------------------ listening without ears

def seam_score(x, rate):
    """How the join of a loop compares with any other place in it. The last 60 ms and the first
    60 ms are put end to end and the 20 ms across the join is compared, by spectrum, with the
    20 ms before it; the same is done at 200 places inside the file. Returned: the join's
    spectral correlation, the inside's median and 5th percentile, and the step in the waveform
    at the join against the median step between neighbouring samples."""
    w = int(rate * 0.02)
    def likeness(a, b):
        A = np.abs(np.fft.rfft(a * np.hanning(len(a)))); B = np.abs(np.fft.rfft(b * np.hanning(len(b))))
        return float((A * B).sum() / (np.sqrt((A ** 2).sum() * (B ** 2).sum()) + 1e-12))
    joined = np.concatenate([x[-3 * w:], x[:3 * w]])
    at_join = likeness(joined[3 * w - w:3 * w], joined[3 * w:3 * w + w])
    rng = np.random.default_rng(1)
    inside = []
    for _ in range(200):
        k = int(rng.integers(w, len(x) - w))
        inside.append(likeness(x[k - w:k], x[k:k + w]))
    step = abs(float(x[0] - x[-1])) / (float(np.median(np.abs(np.diff(x)))) + 1e-12)
    return at_join, float(np.median(inside)), float(np.percentile(inside, 5)), step


def report(rows):
    """Pictures and numbers for every file: a spectrogram, a loudness curve, the peak, the RMS,
    the DC, clipped samples, the spectral balance in four bands, a loop's seam, a one-shot's tail."""
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    REPORT.mkdir(parents=True, exist_ok=True)
    lines = ["%-34s %6s %6s %6s %7s %5s  %-23s  %s" % ("file", "sec", "peak", "rms", "dc", "clip", "<300 300-1k 1k-4k >4k %", "notes")]
    for path, x, rate, loop in rows:
        seconds = len(x) / rate
        peak, rms, dc = float(np.abs(x).max()), float(np.sqrt((x ** 2).mean())), float(x.mean())
        clipped = int((np.abs(x) >= 0.999).sum())
        f, power = signal.welch(x, rate, nperseg=4096)
        total = power.sum() + 1e-20
        bands = [power[(f >= lo) & (f < hi)].sum() / total * 100 for lo, hi in ((0, 300), (300, 1000), (1000, 4000), (4000, 1e9))]
        hop = int(rate * 0.05)
        frames = len(x) // hop
        level = 20 * np.log10(np.sqrt((x[:frames * hop].reshape(frames, hop) ** 2).mean(axis=1)) + 1e-9)
        notes = []
        if loop:
            join, middle, low, step = seam_score(x, rate)
            notes.append("seam %.2f (inside median %.2f, 5th pct %.2f), step %.1fx" % (join, middle, low, step))
            notes.append("level spread %.1f dB" % (np.percentile(level, 95) - np.percentile(level, 5)))
            if join < low:
                notes.append("SEAM WORSE THAN 95% OF THE FILE")
        else:
            notes.append("last 50 ms %.0f dB, first sample %.4f, last %.4f" % (level[-1], x[0], x[-1]))
            loud = np.argmax(level)
            after = np.where(level[loud:] < level[loud] - 20)[0]
            notes.append("20 dB down %.2f s after the loudest moment" % (after[0] * 0.05 if len(after) else float("nan")))
        if peak > 0.86: notes.append("PEAK OVER 0.85")
        if clipped: notes.append("CLIPPED")
        if abs(dc) > 0.002: notes.append("DC")
        lines.append("%-34s %6.2f %6.3f %6.3f %7.4f %5d  %4.0f %5.0f %5.0f %4.0f      %s"
                     % (path.name, seconds, peak, rms, dc, clipped, bands[0], bands[1], bands[2], bands[3], "; ".join(notes)))

        fig, axes = plt.subplots(2, 1, figsize=(11, 6), gridspec_kw={"height_ratios": [3, 1]})
        shown = np.concatenate([x, x[:int(rate * 2)]]) if loop else x
        axes[0].specgram(shown, NFFT=2048, Fs=rate, noverlap=1536, cmap="magma", vmin=-120, vmax=-30)
        axes[0].set_ylim(0, min(10000, rate / 2))
        axes[0].set_title(path.name + ("   (a loop: its first 2 s are drawn again after the white line)" if loop else ""))
        if loop:
            axes[0].axvline(seconds, color="white", linewidth=0.8)
        axes[1].plot(np.arange(frames) * 0.05, level)
        axes[1].set_ylim(-70, 0); axes[1].set_xlim(0, len(shown) / rate); axes[1].set_ylabel("dBFS, 50 ms")
        axes[1].grid(alpha=0.3)
        fig.tight_layout()
        fig.savefig(REPORT / (path.stem + ".png"), dpi=80)
        plt.close(fig)

    # The bowl itself.
    fig, axes = plt.subplots(2, 1, figsize=(11, 5))
    for ir, name, ax in ((CROWD_IR, "the crowd's bowl", axes[0]), (PA_IR, "the PA's bowl", axes[1])):
        t = np.arange(len(ir)) / SR
        hop = int(SR * 0.01)
        frames = len(ir) // hop
        ax.plot(np.arange(frames) * 0.01, 10 * np.log10((ir[:frames * hop].reshape(frames, hop) ** 2).sum(axis=1) + 1e-12))
        ax.set_title(name + ": energy per 10 ms, dB"); ax.set_ylim(-70, 5); ax.grid(alpha=0.3)
    fig.tight_layout(); fig.savefig(REPORT / "_bowl_impulse_responses.png", dpi=80); plt.close(fig)

    (REPORT / "report.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("\n".join(lines))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    ap.add_argument("--report", action="store_true")
    ap.add_argument("--no-vo", action="store_true")
    ap.add_argument("--synthetic-crowd", action="store_true")
    args = ap.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)

    takes = load_vo()
    crowd_takes = [] if args.no_vo else [take for take in takes if take[0] in RECORDED]
    pool = build_pool(4100)
    rows = []
    for name, build, seed, loop_rms in SOUNDS:
        if args.only and args.only not in name:
            continue
        # THE CROWD IS REAL RECORDINGS SINCE 2026-10-05 (tools/build_arena_crowd_from_recordings.py; the owner
        # rejected this synthesis: "just sounds like noise"). Only the PA's stings are written from here,
        # so no run of this tool can put the synthetic crowd back over the recordings by accident.
        if not name.startswith("sfx_arena_pa_") and not args.synthetic_crowd:
            continue
        x = finish(build(np.random.default_rng(seed), pool, crowd_takes), loop_rms)
        path = OUT / (name + ".wav")
        write(path, x)
        rows.append((path, x, SR, loop_rms is not None))
        print("%-34s %.2f s  rms %.3f  peak %.3f" % (name, len(x) / SR, float(np.sqrt((x ** 2).mean())), float(np.abs(x).max())))

    if not args.only or args.only.startswith("pa"):
        rows += [(path, y, rate, False) for path, y, rate in announcer(takes)]

    if args.report:
        report(rows)


if __name__ == "__main__":
    main()
