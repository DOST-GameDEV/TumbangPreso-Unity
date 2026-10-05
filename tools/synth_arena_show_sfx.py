"""The Arena's show sounds: the stage's transformation between rounds, the catch drone, the pyro and
the crowd's roar, by deterministic synthesis.

  py -3 tools/synth_arena_show_sfx.py
  py -3 tools/synth_arena_show_sfx.py --only lock        (any substring of the file names)

Owner, 2026-10-05, after playing the map: "map transformation is so dull, theres no emphasis on
it. could also use some screenshake", and "there should be more vfx overall in the map, including
the drone stuff". The transformation is three beats (Runtime/Map/ArenaStage.cs, BreakBeats) and
each has its sound; the cues are fired by Runtime/Map/ArenaShow.cs, ArenaDrone.cs and
ArenaAmbience.cs on every peer from state every peer already has, so none of them is relayed.

Writes these into Assets/TumbangPreso/Resources/Sfx/ (44100 Hz, mono, 16 bit, each normalised to
0.85 peak like every other cue there; the mix level is the cue's row in Runtime/Audio/AudioCues.cs):
    sfx_arena_alarm.wav        2.30 s  the alarm: three klaxon calls over a riser that ends on the undock
    sfx_arena_undock.wav       0.70 s  every moving platform lets go: a latch clunk and a breath of air
    sfx_arena_thruster.wav     0.90 s  a hover emitter's burst: a blown, pitched whoosh
    sfx_arena_lock.wav         0.55 s  one platform locking: a low thud, a steel clank, a short sub
    sfx_arena_reveal.wav       2.40 s  the reveal: a boom, a bright shimmer falling away
    sfx_arena_crowd_roar.wav   3.60 s  the stands going up: many voices, swelling then settling
    sfx_arena_pyro.wav         1.50 s  a firework or a pyro jet: a thump, then crackle
    The rescue drone, SAGIP, is a toy and sounds like one (owner, 2026-10-05: "drone design and ufo
    effect needs to be more stylized"; before this its two cues were a sonar ping and a thump):
    sfx_arena_drone_ping.wav   0.36 s  locking on: "bi-BIP!", two toy blips
    sfx_arena_drone_hum.wav    0.80 s  one beat of its hover, a wobbly buzz (struck again as it works)
    sfx_arena_drone_beam.wav   1.45 s  the haul: a slide whistle rising with the lift, a sparkle at the top
    sfx_arena_drone_set.wav    0.85 s  setting a body down: a soft boing and a ding
    sfx_arena_drone_zip.wav    0.60 s  gone: a slide whistle shot upward and a "ting"

Same house method as synth_ilalim_train_clack.py and synth_ilalim_life_sfx.py: numpy and scipy only,
fixed seeds, no recordings and nothing downloaded, so a rerun is byte-identical."""
import argparse
import wave
from pathlib import Path

import numpy as np
from scipy import signal

SR = 44100
PEAK = 0.85
OUT = Path(__file__).resolve().parents[1] / "Assets/TumbangPreso/Resources/Sfx"


def t_of(seconds):
    return np.arange(int(SR * seconds)) / SR


def band(x, low, high, order=2):
    b, a = signal.butter(order, [low / (SR / 2), high / (SR / 2)], "band")
    return signal.lfilter(b, a, x)


def lowpass(x, cut, order=2):
    b, a = signal.butter(order, cut / (SR / 2), "low")
    return signal.lfilter(b, a, x)


def highpass(x, cut, order=2):
    b, a = signal.butter(order, cut / (SR / 2), "high")
    return signal.lfilter(b, a, x)


def fade(x, fade_in=0.004, fade_out=0.03):
    """Raised-cosine ends to exact zero, so no cue clicks at either end."""
    n = len(x)
    a = min(n // 2, int(SR * fade_in))
    b = min(n // 2, int(SR * fade_out))
    y = x.copy()
    if a > 0:
        y[:a] *= 0.5 - 0.5 * np.cos(np.pi * np.arange(a) / a)
    if b > 0:
        y[-b:] *= 0.5 + 0.5 * np.cos(np.pi * np.arange(b) / b)
    return y


def sweep(t, f0, f1, curve=1.0):
    """A sine whose pitch runs from f0 to f1 over the whole of t (curve above 1 leaves late)."""
    share = (t / t[-1]) ** curve
    freq = f0 + (f1 - f0) * share
    return np.sin(2 * np.pi * np.cumsum(freq) / SR)


def place(out, x, at):
    i = int(at * SR)
    n = min(len(x), len(out) - i)
    if n > 0:
        out[i:i + n] += x[:n]


def alarm(rng):
    """Three two-tone klaxon calls, each shorter than the gap before the next, over a noise and
    tone riser that climbs to the last sample: the undock lands where this ends."""
    t = t_of(2.30)
    out = np.zeros(len(t))
    for k, start in enumerate((0.05, 0.72, 1.39)):
        tt = t_of(0.46)
        tone = np.where(tt < 0.23, 466.0, 370.0)                    # a falling minor third: "look out"
        phase = 2 * np.pi * np.cumsum(tone) / SR
        horn = np.sign(np.sin(phase)) * 0.35 + np.sin(phase) * 0.5 + np.sin(phase * 2.0) * 0.18
        horn = lowpass(horn, 2600) * (0.75 + 0.25 * np.sin(2 * np.pi * 11 * tt))
        place(out, fade(horn, 0.012, 0.05) * (0.60 + 0.08 * k), start)
    riser = band(rng.standard_normal(len(t)), 500, 6000) * (t / t[-1]) ** 2.2 * 0.55
    riser += sweep(t, 90, 520, 2.0) * (t / t[-1]) ** 1.6 * 0.38
    out += riser
    return fade(out, 0.005, 0.012)


def undock(rng):
    """A heavy latch letting go (a low knock, two ringing steel modes) and the air it lets out."""
    t = t_of(0.70)
    knock = np.sin(2 * np.pi * 62 * t + 0.4) * np.exp(-t * 16) * 1.0
    ring = (np.sin(2 * np.pi * 410 * t) * np.exp(-t * 24) * 0.42
            + np.sin(2 * np.pi * 1130 * t) * np.exp(-t * 40) * 0.26)
    tick = band(rng.standard_normal(len(t)), 1500, 6000) * np.exp(-t * 130) * 0.9
    air = band(rng.standard_normal(len(t)), 1800, 9000) * np.clip((t - 0.05) / 0.06, 0, 1) * np.exp(-np.clip(t - 0.05, 0, None) * 7.5) * 0.30
    return fade(knock + ring + tick + air, 0.002, 0.08)


def thruster(rng):
    """A hover emitter's burst: blown noise through a band that rises and falls, with a pitched core."""
    t = t_of(0.90)
    env = np.clip(t / 0.05, 0, 1) * np.exp(-np.clip(t - 0.08, 0, None) * 4.2)
    noise = rng.standard_normal(len(t))
    low = band(noise, 220, 900) * 0.9
    high = band(noise, 1800, 7000) * 0.5 * (0.6 + 0.4 * np.sin(2 * np.pi * 37 * t))
    core = sweep(t, 150, 96, 0.7) * 0.5 + sweep(t, 300, 190, 0.7) * 0.22
    return fade((low + high + core) * env, 0.004, 0.12)


def lock(rng):
    """One platform locking home: the thud of its weight, the clank of the dock, a short sub."""
    t = t_of(0.55)
    thud = sweep(t, 120, 46, 0.5) * np.exp(-t * 13) * 1.0
    sub = np.sin(2 * np.pi * 38 * t) * np.exp(-t * 7.5) * 0.55
    clank = (np.sin(2 * np.pi * 690 * t) * np.exp(-t * 30) * 0.40
             + np.sin(2 * np.pi * 1870 * t) * np.exp(-t * 46) * 0.24
             + np.sin(2 * np.pi * 2950 * t) * np.exp(-t * 70) * 0.12)
    tick = band(rng.standard_normal(len(t)), 1200, 7000) * np.exp(-t * 170) * 1.1
    return fade(thud + sub + clank + tick, 0.001, 0.08)


def reveal(rng):
    """The reveal: a boom whose pitch drops away, a wash of noise, and a bright shimmer of five
    partials that falls after it."""
    t = t_of(2.40)
    boom = sweep(t, 150, 34, 0.35) * np.exp(-t * 3.2) * 1.0
    crack = band(rng.standard_normal(len(t)), 300, 5000) * np.exp(-t * 30) * 0.9
    wash = band(rng.standard_normal(len(t)), 800, 9000) * np.exp(-t * 2.4) * np.clip(t / 0.03, 0, 1) * 0.30
    shimmer = np.zeros(len(t))
    for k, f in enumerate((1318.5, 1568.0, 1975.5, 2637.0, 3136.0)):   # an E minor seventh, high
        start = 0.03 + 0.045 * k
        tt = np.clip(t - start, 0, None)
        shimmer += np.sin(2 * np.pi * f * tt * (1.0 - 0.012 * tt)) * np.exp(-tt * 2.6) * (t >= start) * 0.11
    return fade(boom + crack + wash + shimmer, 0.001, 0.25)


def crowd_roar(rng):
    """The stands going up. Forty voices, each a band of noise with its own vowel formants and its
    own slow swell, so the sum has a crowd's texture and never one voice's pitch."""
    t = t_of(3.60)
    out = np.zeros(len(t))
    for _ in range(40):
        noise = rng.standard_normal(len(t))
        f1 = rng.uniform(520, 820)       # an open "aaa" to "ooo"
        f2 = rng.uniform(1000, 1500)
        voice = band(noise, f1 * 0.8, f1 * 1.25) + band(noise, f2 * 0.85, f2 * 1.2) * 0.6
        rise = rng.uniform(0.10, 0.55)
        hold = rng.uniform(1.2, 2.2)
        env = np.clip(t / rise, 0, 1) ** 1.5 * np.exp(-np.clip(t - hold, 0, None) * rng.uniform(1.2, 2.4))
        wobble = 1.0 + 0.25 * np.sin(2 * np.pi * rng.uniform(2.5, 6.0) * t + rng.uniform(0, 6.28))
        out += voice * env * wobble * rng.uniform(0.5, 1.0)
    air = band(rng.standard_normal(len(t)), 2500, 8000) * np.clip(t / 0.4, 0, 1) * np.exp(-np.clip(t - 1.6, 0, None) * 1.6) * 6.0
    whistle_t = np.clip(t - 0.9, 0, None)
    whistle = np.sin(2 * np.pi * (2400 + 500 * np.sin(2 * np.pi * 3.1 * whistle_t)) * whistle_t) * np.exp(-whistle_t * 3.5) * (t >= 0.9) * 2.2
    return fade(out + air + whistle, 0.02, 0.5)


def pyro(rng):
    """A shell or a jet: the thump of the lift, then crackle that thins out."""
    t = t_of(1.50)
    thump = sweep(t, 180, 50, 0.4) * np.exp(-t * 11) * 1.0
    burst = band(rng.standard_normal(len(t)), 400, 7000) * np.exp(-t * 22) * 0.8
    crackle = np.zeros(len(t))
    at = 0.10
    while at < 1.35:
        n = int(SR * 0.012)
        grain = band(rng.standard_normal(n), 2500, 11000) * np.exp(-np.arange(n) / SR * 380)
        place(crackle, grain * rng.uniform(0.25, 0.7) * np.exp(-at * 1.9), at)
        at += rng.exponential(0.018) + 0.004
    return fade(thump + burst + crackle, 0.001, 0.15)


def toy(phase, bright=0.35):
    """A toy's voice: a sine with a little square in it, so it reads as a gadget and not as a siren."""
    return np.sin(phase) * (1 - bright) + np.tanh(np.sin(phase) * 4.0) * bright


def drone_ping(rng):
    """SAGIP locking on: "bi-BIP!", two toy blips a fourth apart, the second one bent upward."""
    t = t_of(0.36)
    out = np.zeros(len(t))
    for start, f0, f1, length, gain in ((0.0, 988.0, 988.0, 0.075, 0.8), (0.105, 1319.0, 1480.0, 0.20, 1.0)):
        tt = t_of(length)
        freq = f0 + (f1 - f0) * (tt / tt[-1]) ** 0.5
        blip = toy(2 * np.pi * np.cumsum(freq) / SR) * np.exp(-tt * (5.0 if length > 0.1 else 2.0))
        place(out, fade(blip, 0.003, 0.02) * gain, start)
    out += band(rng.standard_normal(len(t)), 4000, 9000) * np.exp(-t * 90) * 0.12       # the click of the claw
    return fade(lowpass(out, 7000), 0.002, 0.05)


def drone_hum(rng):
    """Its hover, one beat of it (the game strikes it again every half second, each a little
    higher as it pulls): a round buzzy note that wobbles, with the fan's flutter on top."""
    t = t_of(0.80)
    wobble = 1.0 + 0.045 * np.sin(2 * np.pi * 6.5 * t) + 0.02 * np.sin(2 * np.pi * 11.0 * t + 1.0)
    phase = 2 * np.pi * np.cumsum(168.0 * wobble) / SR
    note = toy(phase, 0.25) * 0.8 + np.sin(phase * 2.0) * 0.22 + np.sin(phase * 3.01) * 0.10
    flutter = 0.72 + 0.28 * np.sin(2 * np.pi * 21.0 * t)
    air = band(rng.standard_normal(len(t)), 900, 3200) * 0.10 * (0.6 + 0.4 * np.sin(2 * np.pi * 21.0 * t + 0.6))
    env = np.clip(t / 0.12, 0, 1) * np.clip((t[-1] - t) / 0.30, 0, 1)                    # long ends: the beats overlap
    return fade(lowpass(note * flutter + air, 3200) * env, 0.02, 0.1)


def drone_beam(rng):
    """The haul: a slide whistle going up for as long as the lift (1.4 s), a wobble in it, air
    rising under it, and a sparkle at the top."""
    t = t_of(1.45)
    share = (t / t[-1]) ** 1.5
    freq = (300.0 + 1250.0 * share) * (1.0 + 0.022 * np.sin(2 * np.pi * 9.0 * t))
    phase = 2 * np.pi * np.cumsum(freq) / SR
    whistle = (np.sin(phase) + np.sin(phase * 2.0) * 0.18) * (0.35 + 0.65 * share) * np.clip(t / 0.05, 0, 1)
    noise = rng.standard_normal(len(t))
    air = (band(noise, 500, 1600) * (1 - share) + band(noise, 1600, 6000) * share) * (0.10 + 0.30 * share)
    out = whistle * 0.8 + air
    for k, f in enumerate((2093.0, 2637.0, 3136.0)):                                     # the sparkle: three high notes at the top
        start = 1.18 + 0.055 * k
        tt = np.clip(t - start, 0, None)
        out += np.sin(2 * np.pi * f * tt) * np.exp(-tt * 14) * (t >= start) * 0.22
    return fade(out, 0.004, 0.12)


def drone_set(rng):
    """Set down: a soft boing as the feet touch (a pitch that drops and springs) and a bright ding
    over it, the bell of a jeepney's fare box."""
    t = t_of(0.85)
    spring = 150.0 + 110.0 * np.exp(-t * 9) * np.cos(2 * np.pi * 13.0 * t)
    boing = np.sin(2 * np.pi * np.cumsum(spring) / SR) * np.exp(-t * 7.5) * 0.75
    puff = band(rng.standard_normal(len(t)), 400, 2600) * np.exp(-t * 22) * 0.22
    ding = np.zeros(len(t))
    for f, gain, decay in ((1568.0, 1.0, 5.0), (3136.0, 0.42, 7.0), (4699.0, 0.22, 10.0), (2349.0, 0.30, 6.0)):
        tt = np.clip(t - 0.06, 0, None)
        ding += np.sin(2 * np.pi * f * tt) * np.exp(-tt * decay) * (t >= 0.06) * gain
    return fade(boing + puff + ding * 0.55, 0.001, 0.2)


def drone_zip(rng):
    """Gone: a slide whistle shot upward in a third of a second, and a tiny "ting" where it went."""
    t = t_of(0.60)
    share = np.clip(t / 0.34, 0, 1) ** 1.6
    freq = 420.0 + 3000.0 * share
    phase = 2 * np.pi * np.cumsum(freq) / SR
    zipped = (np.sin(phase) + np.sin(phase * 2.0) * 0.15) * np.clip(t / 0.02, 0, 1) * np.clip((0.36 - t) / 0.05, 0, 1) * 0.8
    air = band(rng.standard_normal(len(t)), 1500, 8000) * np.clip(t / 0.05, 0, 1) * np.exp(-np.clip(t - 0.10, 0, None) * 12) * 0.30
    tt = np.clip(t - 0.36, 0, None)
    ting = (np.sin(2 * np.pi * 3520.0 * tt) + np.sin(2 * np.pi * 5274.0 * tt) * 0.4) * np.exp(-tt * 16) * (t >= 0.36) * 0.5
    return fade(zipped + air + ting, 0.002, 0.08)


SOUNDS = (
    ("sfx_arena_alarm", alarm, 2301),
    ("sfx_arena_undock", undock, 2302),
    ("sfx_arena_thruster", thruster, 2303),
    ("sfx_arena_lock", lock, 2304),
    ("sfx_arena_reveal", reveal, 2305),
    ("sfx_arena_crowd_roar", crowd_roar, 2306),
    ("sfx_arena_pyro", pyro, 2307),
    ("sfx_arena_drone_ping", drone_ping, 2308),
    ("sfx_arena_drone_set", drone_set, 2309),
    ("sfx_arena_drone_hum", drone_hum, 2310),
    ("sfx_arena_drone_beam", drone_beam, 2311),
    ("sfx_arena_drone_zip", drone_zip, 2312),
)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    args = ap.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    for name, build, seed in SOUNDS:
        if args.only and args.only not in name:
            continue
        x = build(np.random.default_rng(seed))
        x = x / np.abs(x).max() * PEAK
        path = OUT / (name + ".wav")
        with wave.open(str(path), "wb") as w:
            w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
            w.writeframes((x * 32767).astype(np.int16).tobytes())
        print("%-24s %.2f s  rms %.3f  ends %d %d" % (name, len(x) / SR, float(np.sqrt((x ** 2).mean())), int(x[0] * 32767), int(x[-1] * 32767)))


if __name__ == "__main__":
    main()
