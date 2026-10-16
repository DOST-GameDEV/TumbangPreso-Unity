"""Basilio's (Dante's) sounds, DRAFTS for the owner's ear (`docs/reports/dante-presentation-2026-10-16/plan.md`, his sound family).

Skill sounds stay switched off in the game until the owner picks a draft by ear (`AudioCues.ReworkedSkillSfx`), and an
unapproved skill sound has no file in the game. So these are written to the report's `audio-drafts/` and mixed into the review
films; an approved draft is copied to `Resources/Sfx` and registered then. numpy only, seeded: a rebuild writes identical bytes.
Synthesised from scratch (the 2026-10-01 sound authorisation). No voice anywhere: the motif is an instrument.

    python tools/build_dante_audio.py

HIS INSTRUMENTS (he is matter: light gets no pure tones), each typed once below:
  grind    stone stick-slip: a train of uneven impulses through a low resonant band, the slips speeding with pressure
  thud     a low struck mass: a falling sub sine under a short lowpassed burst
  crack    fracture ticks: very short bright clicks, faster as pressure builds
  sizzle   magma hiss in seams: high band noise with sparse crackle
  rumble   sub noise, felt more than heard
  chunk    a rock landing: a short resonant knock, two partials, a grit tail
  horn     THE MOTIF (the crest's sound, shown three times): a low synthesised carabao horn, tambuli-like. A buzzing lip tone
           (odd-heavy partials over ~98 Hz) bending up into pitch, breathy, through two fixed formants. An instrument, never a voice.
Every cue is TRANSIENT / BODY / TAIL, one recipe per cue, never one recipe for two cues.

The cutscene's theme (`sfx_ult_theme_dante`) is timed beat for beat to `HeroIntroductionScene.Dante.cs` (plan section 5.3); its
loudness, 0.1 s RMS, is written beside it so the loudest moment is the stomp, not the setup.
"""
import json
import os
import wave

import numpy as np

RATE = 44100
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "reports", "dante-presentation-2026-10-16", "audio-drafts")
rng = np.random.default_rng(20261016)


# ---------------------------------------------------------------------------------------------- helpers
def n_of(seconds): return int(round(seconds * RATE))
def t_axis(seconds): return np.arange(n_of(seconds)) / RATE


def band(x, lo, hi, order=4):
    """A soft band-pass by FFT mask (Butterworth-like magnitude), so the edges never ring."""
    if len(x) == 0: return x
    f = np.fft.rfftfreq(len(x), 1 / RATE)
    g = np.ones_like(f)
    if lo > 0: g *= 1 / np.sqrt(1 + (lo / np.maximum(f, 1e-3)) ** (2 * order))
    if hi > 0: g *= 1 / np.sqrt(1 + (f / hi) ** (2 * order))
    return np.fft.irfft(np.fft.rfft(x) * g, len(x))


def noise(seconds): return rng.standard_normal(n_of(seconds))


def decay(seconds, attack, release):
    t = t_axis(seconds)
    a = np.clip(t / max(attack, 1e-4), 0, 1)
    return a * np.exp(-np.maximum(0, t - attack) / max(release, 1e-4))


def place(track, clip, at, gain=1.0):
    i = n_of(at)
    if i >= len(track): return track
    j = min(len(track), i + len(clip))
    track[i:j] += clip[:j - i] * gain
    return track


def fade_tail(x, seconds=0.03):
    k = min(len(x), n_of(seconds))
    if k: x[-k:] *= np.cos(np.linspace(0, np.pi / 2, k)) ** 2
    return x


def norm(x, peak=0.85):
    m = np.max(np.abs(x)) or 1.0
    return x * (peak / m)


# ---------------------------------------------------------------------------------------------- instruments
def grind(seconds, rate_from=38.0, rate_to=60.0, lo=90, hi=900, rough=0.35):
    """Stick-slip: impulses at an uneven, accelerating rate through a low resonant band; a little scrape noise between."""
    n = n_of(seconds); x = np.zeros(n)
    t, i = 0.0, 0
    while t < seconds:
        rate = rate_from + (rate_to - rate_from) * (t / seconds)
        x[min(n - 1, n_of(t))] += 1.0 + rng.uniform(-.4, .4)
        t += (1 / rate) * (1 + rng.uniform(-rough, rough)); i += 1
    body = band(x, lo, hi, 2) * 6.0 + band(noise(seconds), lo * 2, hi * 2) * 0.12
    return body * decay(seconds, 0.03, seconds * 0.8)


def thud(seconds=0.9, f0=78.0, f1=42.0, weight=1.0):
    t = t_axis(seconds)
    f = f1 + (f0 - f1) * np.exp(-t / 0.09)
    sub = np.sin(2 * np.pi * np.cumsum(f) / RATE) * np.exp(-t / (0.22 * weight + 0.1))
    hit = band(noise(seconds), 40, 520) * np.exp(-t / 0.035) * 0.9
    return sub * 0.9 + hit


def crack(count, seconds, accelerate=True, bright=3200):
    """`count` fracture ticks over `seconds`, closer together toward the end when accelerating."""
    n = n_of(seconds); x = np.zeros(n)
    for k in range(count):
        u = (k / max(1, count - 1))
        at = seconds * (1 - (1 - u) ** 1.8) if accelerate else seconds * u
        tick = band(noise(0.012), 900, bright) * np.exp(-t_axis(0.012) / 0.003) * rng.uniform(.6, 1.0)
        place(x, tick, min(at, seconds - 0.013))
    return x


def sizzle(seconds, density=40):
    t = t_axis(seconds)
    hiss = band(noise(seconds), 2600, 9000) * 0.18
    pops = np.zeros(len(t))
    for _ in range(int(density * seconds)):
        place(pops, band(noise(0.006), 1500, 7000) * np.exp(-t_axis(0.006) / 0.0015), rng.uniform(0, seconds - 0.007), rng.uniform(.3, .8))
    return (hiss + pops) * (0.6 + 0.4 * np.sin(2 * np.pi * 1.3 * t) ** 2)


def rumble(seconds, lo=24, hi=70):
    t = t_axis(seconds)
    return band(noise(seconds), lo, hi, 3) * 5.0 * (0.7 + 0.3 * np.sin(2 * np.pi * 0.9 * t + 1.1))


def chunk(f=210.0, size=1.0, grit=0.5):
    seconds = 0.35 * size + 0.1
    t = t_axis(seconds)
    knock = (np.sin(2 * np.pi * f * t) + 0.55 * np.sin(2 * np.pi * f * 2.37 * t)) * np.exp(-t / (0.045 * size))
    tail = band(noise(seconds), 300, 2600) * np.exp(-t / (0.09 * size)) * grit * 0.6
    hit = band(noise(seconds), 60, 700) * np.exp(-t / 0.012)
    return knock * 0.7 + tail + hit * 0.8


def horn(seconds=1.1, f=98.0, bend=0.12, breath=0.18):
    """The carabao-horn motif: a lip-buzz tone bending up into pitch, odd-heavy partials, two fixed formants, breath."""
    t = t_axis(seconds)
    pitch = f * (1 - bend * np.exp(-t / 0.12)) * (1 + 0.006 * np.sin(2 * np.pi * 4.2 * t) * np.clip(t / 0.4, 0, 1))
    phase = 2 * np.pi * np.cumsum(pitch) / RATE
    tone = np.zeros_like(t)
    for k, a in [(1, 1.0), (2, .35), (3, .62), (4, .22), (5, .40), (6, .12), (7, .24), (9, .14), (11, .08)]:
        tone += a * np.sin(k * phase)
    tone = band(tone, 70, 0) * 0.6 + band(tone, 480, 760) * 1.3 + band(tone, 1100, 1500) * 0.5
    air = band(noise(seconds), 600, 2400) * breath
    shape = np.clip(t / 0.09, 0, 1) * np.clip((seconds - t) / 0.35, 0, 1)
    return (tone + air * (0.5 + 0.5 * shape)) * shape


# ---------------------------------------------------------------------------------------------- the cues
def cue_concussed_ring():
    """The victim's ears (2.6 s): a high ring of two near partials beating, under which the mix is muffled by the game."""
    s = 2.6; t = t_axis(s)
    ring = np.sin(2 * np.pi * 3150 * t) + 0.7 * np.sin(2 * np.pi * 3178 * t) + 0.25 * np.sin(2 * np.pi * 4720 * t)
    env = np.clip(t / 0.02, 0, 1) * np.exp(-t / 1.4)
    knock = chunk(170, 0.8, 0.2)
    x = ring * env * 0.25
    return place(x, knock, 0.0, 0.8)


def cue_concussed():
    """What everyone hears on the victim: a dull stone knock and three pebbles settling round the head."""
    x = np.zeros(n_of(0.8))
    place(x, thud(0.5, 120, 70, 0.4), 0.0, 0.7)
    for at, f in [(0.07, 520), (0.15, 610), (0.26, 470)]:
        place(x, chunk(f, 0.35, 0.3), at, 0.35)
    return x


def cue_earthbound():
    """Heels digging in: a short grind scrape, his weight in it."""
    x = np.zeros(n_of(0.7))
    place(x, grind(0.55, 52, 30, 120, 1200, 0.45), 0.0, 1.0)
    place(x, thud(0.4, 90, 55, 0.3), 0.0, 0.35)
    return x


def cue_unstoppable_cast():
    """TELL crack ticks speeding up (0 to .15), the THUD and each status's shell breaking (.15), then a chunk per plate rising (.15 to .55)."""
    x = np.zeros(n_of(1.2))
    place(x, crack(9, 0.15), 0.0, 0.8)
    place(x, thud(0.8, 82, 44, 1.0), 0.15, 1.0)
    shell = band(noise(0.18), 1800, 7500) * np.exp(-t_axis(0.18) / 0.04)
    place(x, shell, 0.155, 0.6)
    for k, f in enumerate([150, 168, 190, 214, 240, 270, 300]):
        place(x, chunk(f, 0.55, 0.25), 0.18 + k * 0.06, 0.55)
    return x


def cue_unstoppable_refuse():
    """A refused status: a dull clink off a plate, no ring."""
    x = chunk(640, 0.3, 0.15)
    return x * 0.8


def cue_unstoppable_warning():
    """The last 2 s: a low grind under the cooling seams."""
    return grind(2.0, 22, 34, 70, 500, 0.3) * 0.7


def cue_unstoppable_crumble():
    """The plates drop and break: a chunk cascade, falling in pitch."""
    x = np.zeros(n_of(1.1))
    for k, (at, f) in enumerate([(0.0, 300), (0.05, 262), (0.11, 240), (0.16, 205), (0.24, 180), (0.3, 160), (0.37, 140)]):
        place(x, chunk(f, 0.7, 0.6), at, 0.6)
    place(x, rumble(0.9, 30, 90), 0.05, 0.3)
    return x


def cue_boulder_cast():
    """Grit pulled into the palm (a grind, .25), the crust closing (a small thud)."""
    x = np.zeros(n_of(0.8))
    place(x, grind(0.25, 70, 110, 200, 1800, 0.3), 0.0, 0.8)
    place(x, thud(0.5, 110, 66, 0.35), 0.25, 0.8)
    return x


def cue_boulder_sizzle():
    """The crust's seam, near only: a quiet hiss with sparse pops (loops; its ends are crossfaded)."""
    s = 2.0
    x = sizzle(s, 30) * 0.5
    k = n_of(0.2)
    x[:k] = x[:k] * np.linspace(0, 1, k) + x[-k:] * np.linspace(1, 0, k)
    return x[:-k]


def cue_boulder_throw():
    """A low whum on release: the weight of the crusted slipper leaving the hand."""
    s = 0.45; t = t_axis(s)
    whum = band(noise(s), 70, 320) * np.sin(np.pi * np.clip(t / s, 0, 1)) * 1.8
    return whum


def cue_boulder_hit():
    """On a player: the crust bursting (a chunk) and a bright ring that becomes their Concussed ring."""
    x = np.zeros(n_of(1.0))
    place(x, chunk(230, 0.9, 0.8), 0.0, 1.0)
    t = t_axis(0.8)
    place(x, (np.sin(2 * np.pi * 2900 * t) + .5 * np.sin(2 * np.pi * 2925 * t)) * np.exp(-t / 0.35) * 0.18, 0.02)
    return x


def cue_boulder_land():
    """On the court: a dry chunk, the crust cracking where it fell."""
    return chunk(185, 0.6, 1.0)


def cue_boulder_flake():
    """Flaking: a few dry ticks and grains."""
    x = np.zeros(n_of(0.6))
    place(x, crack(5, 0.45, accelerate=False, bright=2600), 0.0, 0.6)
    return x


def cue_bastion_cast():
    """The stamp, cracks running sideways (.0 to .2), then seven slabs rising centre first then outward (.2 to .45)."""
    x = np.zeros(n_of(1.2))
    place(x, thud(0.5, 95, 60, 0.4), 0.0, 0.7)
    place(x, crack(10, 0.2), 0.02, 0.7)
    for k, (at, f, pan) in enumerate([(0.20, 150, 1.0), (0.235, 165, .9), (0.235, 162, .9), (0.27, 180, .8), (0.27, 176, .8),
                                       (0.305, 196, .7), (0.305, 192, .7)]):
        place(x, chunk(f, 0.8, 0.5), at, 0.55 * pan)
    place(x, grind(0.35, 40, 60, 80, 700), 0.2, 0.35)
    return x


def cue_bastion_grind():
    """While he moves with it: the slabs dragging through the court (loops)."""
    return grind(1.5, 30, 30, 70, 650, 0.5) * 0.6


def cue_bastion_reflect():
    """A slipper struck off the field: a hard stone clack."""
    x = chunk(420, 0.4, 0.3)
    return place(x, band(noise(0.05), 1500, 6000) * np.exp(-t_axis(0.05) / 0.006), 0.0, 0.5)


def cue_bastion_sink():
    """The slabs crack and sink back, outside first: a chunk cascade into a grind."""
    x = np.zeros(n_of(1.0))
    for k, (at, f) in enumerate([(0.0, 196), (0.0, 192), (0.06, 180), (0.06, 176), (0.12, 165), (0.12, 162), (0.18, 150)]):
        place(x, chunk(f, 0.7, 0.6), at, 0.4)
    place(x, grind(0.6, 40, 22, 60, 500), 0.1, 0.5)
    return x


def cue_drift_fuse():
    """The fuse: a fast crack run racing away."""
    x = np.zeros(n_of(0.5))
    place(x, crack(22, 0.4), 0.0, 0.9)
    place(x, sizzle(0.4, 60), 0.0, 0.4)
    return x


def cue_drift_blast(k):
    """Blast k (0 to 4): a thud and a chunk, each a semitone higher, the fifth the heaviest."""
    st = 2 ** (k / 12)
    heavy = 1.0 + (0.6 if k == 4 else 0.0)
    x = np.zeros(n_of(1.3))
    place(x, thud(1.0, 80 * st, 40 * st, heavy), 0.0, 1.0)
    place(x, chunk(200 * st, 0.9 * heavy, 0.7), 0.01, 0.8)
    if k == 4: place(x, rumble(1.0), 0.02, 0.6)
    return x


def cue_drift_rumble():
    """Court-wide, under the cascade."""
    return rumble(2.4) * np.clip(t_axis(2.4) / 0.1, 0, 1) * np.clip((2.4 - t_axis(2.4)) / 0.8, 0, 1)


def cue_drift_settle():
    """The aftermath: slabs dropping back, settling chunks."""
    x = np.zeros(n_of(1.4))
    for at, f in [(0.0, 170), (0.18, 150), (0.31, 190), (0.55, 140), (0.8, 160)]:
        place(x, chunk(f, 0.8, 0.8), at, 0.45)
    return x


def cue_theme():
    """The cutscene, 7.0 s, beat for beat (plan section 5.3; times are the stage's)."""
    s = 7.0; x = np.zeros(n_of(s))
    # 0.00 cut-in accent: a single low rumble from far under the court.
    place(x, rumble(1.2, 22, 60) * decay(1.2, 0.05, 0.7), 0.0, 0.8)
    # 0.45 knock one; 0.80 knock two: a hollow stone knock with a long echo. Something far below answers the second.
    def knock(f):
        k = chunk(f, 0.9, 0.2)
        echo = np.zeros(n_of(1.4)); place(echo, k, 0.0); place(echo, band(k, 80, 900), 0.21, 0.35); place(echo, band(k, 80, 600), 0.43, 0.15)
        return echo
    place(x, knock(150), 0.45, 0.8)
    place(x, knock(146), 0.80, 0.8)
    place(x, thud(0.6, 50, 32, 0.6), 0.93, 0.45)
    # 0.95 to 1.15 his eye opens: the horn motif, short and low.
    place(x, horn(0.75, 92, 0.10, 0.2), 0.96, 0.22)
    # 1.15 swell drawn in to the compression; 1.30 to 1.50 near silence.
    t = t_axis(0.15)
    place(x, band(noise(0.15), 120, 3000) * (t / 0.15) ** 2 * 0.5, 1.15)
    # 1.55 THE PUNCH: thud, cut dead.
    place(x, fade_tail(thud(0.5, 85, 46, 1.0)), 1.55, 1.5)
    # 1.70 to 3.10 the continents: crack runs, plates grinding apart (1.95, 2.30), a rumble building.
    place(x, crack(16, 0.5), 1.70, 0.6)
    place(x, grind(0.6, 30, 45, 70, 600), 1.95, 0.55)
    place(x, grind(0.7, 26, 40, 60, 550), 2.30, 0.6)
    r = rumble(1.4); place(x, r * np.linspace(0.2, 1.0, len(r)), 1.75, 0.55)
    # 3.10 the light dives into the seam: a sizzle drop.
    sz = sizzle(0.35, 70); place(x, sz * np.linspace(1, 0, len(sz)), 3.10, 0.7)
    # 3.30 heave one: the horns break the surface.
    place(x, grind(0.45, 34, 26, 60, 500), 3.30, 0.75)
    # 3.75 the eyes ignite: the motif, full.
    place(x, horn(1.4, 98, 0.14, 0.22), 3.72, 0.36)
    # 4.00 to 5.20 three heaves (4.0, 4.5, 4.95): a grind and a thud per hand plant.
    for at in (4.0, 4.5, 4.95):
        place(x, grind(0.4, 30, 24, 60, 480), at, 0.6)
        place(x, thud(0.6, 70, 40, 0.7), at + 0.12, 0.6)
    # 5.30 the swell drawn in again, the rumble peaking, crack ticks speeding to the hold.
    r = rumble(1.3); place(x, r * np.linspace(0.4, 1.0, len(r)), 5.30, 0.6)
    place(x, crack(18, 1.25), 5.35, 0.5)
    t = t_axis(1.3); place(x, band(noise(1.3), 100, 2600) * (t / 1.3) ** 3 * 0.45, 5.30)
    # 6.60 to 6.85 the held fists: silence (everything before it ends by .6).
    i0, i1 = n_of(6.60), n_of(6.86)
    x[i0:i1] *= np.linspace(1, 0, i1 - i0) ** 3
    x[i1:] = 0
    # 6.88 THE STOMP: the biggest thud of his kit, and the tail carrying into play.
    place(x, thud(0.12, 72, 36, 1.8), 6.88, 2.4)
    place(x, chunk(160, 1.0, 0.9)[:n_of(0.12)], 6.885, 1.4)
    return x


CUES = {
    "sfx_status_concussed_ring": cue_concussed_ring, "sfx_status_concussed": cue_concussed,
    "sfx_dante_earthbound_scrape": cue_earthbound,
    "sfx_cast_dante_shield": cue_unstoppable_cast, "sfx_dante_shield_refuse": cue_unstoppable_refuse,
    "sfx_dante_shield_warning": cue_unstoppable_warning, "sfx_dante_shield_crumble": cue_unstoppable_crumble,
    "sfx_cast_dante_boulder": cue_boulder_cast, "sfx_dante_boulder_sizzle": cue_boulder_sizzle,
    "sfx_dante_boulder_throw": cue_boulder_throw, "sfx_dante_boulder_hit": cue_boulder_hit,
    "sfx_dante_boulder_land": cue_boulder_land, "sfx_dante_boulder_flake": cue_boulder_flake,
    "sfx_cast_dante_barrier": cue_bastion_cast, "sfx_dante_barrier_grind": cue_bastion_grind,
    "sfx_dante_barrier_reflect": cue_bastion_reflect, "sfx_dante_barrier_sink": cue_bastion_sink,
    "sfx_dante_drift_fuse": cue_drift_fuse, "sfx_dante_drift_rumble": cue_drift_rumble, "sfx_dante_drift_settle": cue_drift_settle,
    "sfx_ult_theme_dante": cue_theme,
}
for _k in range(5):
    CUES["sfx_dante_drift_blast_%d" % (_k + 1)] = (lambda k=_k: cue_drift_blast(k))


def write(path, x):
    pcm = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE); w.writeframes(pcm.tobytes())


def main():
    os.makedirs(OUT, exist_ok=True)
    table = {}
    for name, make in CUES.items():
        x = norm(make(), 0.85 if not name.startswith("sfx_dante_boulder_sizzle") else 0.4)
        write(os.path.join(OUT, name + ".wav"), x)
        rms = float(np.sqrt(np.mean(x * x)))
        table[name] = {"seconds": round(len(x) / RATE, 3), "peak": round(float(np.max(np.abs(x))), 3), "rms": round(rms, 4)}
        if name == "sfx_ult_theme_dante":
            w = n_of(0.1)
            table[name]["rms_per_0.1s"] = [round(float(np.sqrt(np.mean(x[i:i + w] ** 2))), 3) for i in range(0, len(x), w)]
    with open(os.path.join(OUT, "loudness.json"), "w", encoding="utf-8") as f:
        json.dump(table, f, indent=1)
    loud = table["sfx_ult_theme_dante"]["rms_per_0.1s"]
    print("theme loudest 0.1 s at %.1f s (the stomp is at 6.88)" % (loud.index(max(loud)) / 10))
    print("wrote %d cues to %s" % (len(table), OUT))


if __name__ == "__main__":
    main()
