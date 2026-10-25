"""Render AUDITION drafts of Zack's speedster-kit cues (2026-10-25 rework, docs/reports/zack-kit-2026-10-25/plan.md section 9).

Nothing here ships: hero skill sounds stay silent until the owner picks each by ear (`AudioCues.SkillSfxOn`, the tump-sfx
method). This writes one WAV per named cue to an audition folder (default `Logs/zack-sfx-drafts/`) and never touches
Resources.

The family (plan section 0): dry snaps, a buzzing hum at mains frequency, a wire twang, a rising whine for speed, a pop for
the decoy. Each cue is built for what the player must hear: the snap's onset is the information, the tail stays short so
cues repeated every few seconds stay readable. Synthesis only (numpy); no samples, no voices.

Run:  python tools/build_zack_sfx_drafts.py [--out <folder>]
"""
import argparse
import os
import wave

import numpy as np

SR = 48000
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RNG = np.random.default_rng(20261025)


def t_axis(seconds):
    return np.arange(int(SR * seconds)) / SR


def env(n, attack, decay, sustain=0.0, release=None):
    """Attack/decay envelope in seconds over n samples, exponential decay to `sustain`, optional release at the end."""
    t = np.arange(n) / SR
    e = np.where(t < attack, t / max(attack, 1e-6), sustain + (1 - sustain) * np.exp(-(t - attack) / max(decay, 1e-6)))
    if release:
        r = int(release * SR)
        e[-r:] *= np.linspace(1, 0, r)
    return e


def noise(n, colour=0.0):
    """White noise, tilted darker by a one-pole low-pass of `colour` (0 white, toward 1 darker)."""
    x = RNG.standard_normal(n)
    if colour > 0:
        y = np.empty_like(x); acc = 0.0
        for i in range(n):
            acc = colour * acc + (1 - colour) * x[i]; y[i] = acc
        x = y / (np.max(np.abs(y)) + 1e-9)
    return x


def highpass(x, cutoff):
    a = np.exp(-2 * np.pi * cutoff / SR); y = np.empty_like(x); prev_x = prev_y = 0.0
    for i in range(len(x)):
        prev_y = a * (prev_y + x[i] - prev_x); prev_x = x[i]; y[i] = prev_y
    return y


def crackle(n, density, seed_scale=1.0):
    """Sparse dry clicks: electricity as short broadband ticks, not a hiss."""
    out = np.zeros(n)
    count = int(n / SR * density)
    for _ in range(count):
        at = RNG.integers(0, max(1, n - 240))
        length = RNG.integers(20, 160)
        out[at:at + length] += RNG.standard_normal(length) * np.exp(-np.arange(length) / (length / 4)) * RNG.uniform(.3, 1) * seed_scale
    return out


def snap(seconds=0.12, bright=3000):
    n = int(SR * seconds)
    return highpass(noise(n), bright) * env(n, .0008, .018) + crackle(n, 120, .4) * env(n, .001, .05)


def hum(seconds, base=60.0, harmonics=6, buzz=0.35):
    t = t_axis(seconds)
    x = sum((1 / k) * np.sign(np.sin(2 * np.pi * base * k * t)) * (buzz if k % 2 == 0 else 1) for k in range(1, harmonics + 1))
    return x / harmonics


def whine(seconds, f0, f1):
    t = t_axis(seconds)
    f = np.geomspace(f0, f1, len(t))
    phase = 2 * np.pi * np.cumsum(f) / SR
    return np.sin(phase) * .6 + np.sin(2 * phase) * .25 + np.sin(3 * phase) * .1


def twang(seconds, f, decay):
    t = t_axis(seconds)
    bend = f * (1 + .04 * np.exp(-t * 30))
    phase = 2 * np.pi * np.cumsum(bend) / SR
    return (np.sin(phase) + .4 * np.sin(2.01 * phase) + .2 * np.sin(3.02 * phase)) * np.exp(-t / decay)


def pad(x, seconds):
    n = int(SR * seconds)
    return np.pad(x, (0, max(0, n - len(x))))[:n]


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def at(x, seconds, total):
    out = np.zeros(int(SR * total)); i = int(SR * seconds)
    out[i:i + len(x)] += x[:max(0, len(out) - i)]
    return out


def finish(x, peak_db=-3.0):
    x = x - np.mean(x)
    peak = np.max(np.abs(x)) + 1e-9
    return x / peak * 10 ** (peak_db / 20)


CUES = {}


def cue(fn):
    CUES[fn.__name__] = fn
    return fn


@cue
def sfx_cast_zack_backtrack():
    # The sharp inhale of current (a reversed crackle swell), then the snap.
    total = .55
    inhale = crackle(int(SR * .3), 260)[::-1] * np.linspace(0, 1, int(SR * .3)) ** 2
    return mix(at(inhale * .6, 0, total), at(snap(.15, 2500), .3, total))


@cue
def sfx_zack_backtrack_land():
    # A short static rewind: a whine falling fast, a tape-stop chatter, and a thump in the knees.
    total = .5
    rewind = whine(.3, 2400, 300) * env(int(SR * .3), .005, .12)
    chatter = crackle(int(SR * .3), 400, .5) * np.linspace(1, 0, int(SR * .3))
    thump = np.sin(2 * np.pi * 70 * t_axis(.18)) * env(int(SR * .18), .002, .05)
    return mix(at(rewind * .5, 0, total), at(chatter, 0, total), at(thump, .22, total))


@cue
def sfx_cast_zack_afterimage():
    # The copy steps out of him: a crackle at his feet and a quick doubled snap, slightly detuned.
    total = .4
    return mix(at(snap(.1, 3200), 0, total), at(snap(.1, 2600) * .7, .045, total), at(crackle(int(SR * .25), 200) * .4, .02, total))


@cue
def sfx_zack_afterimage_pop():
    # The decoy's dry pop and a "zip": a pitched burst falling, then flecks.
    total = .45
    pop = np.sin(2 * np.pi * np.cumsum(np.geomspace(900, 180, int(SR * .08))) / SR) * env(int(SR * .08), .001, .03)
    zip_ = whine(.12, 1800, 4200) * env(int(SR * .12), .002, .04) * .5
    return mix(at(pop, 0, total), at(zip_, .03, total), at(crackle(int(SR * .3), 90) * .5, .05, total))


@cue
def sfx_zack_afterimage_fizzle():
    # It stumbles and fizzles from the feet up: a soft hiss rising out and thinning.
    total = .5
    n = int(SR * .45)
    fizz = highpass(noise(n), 1800) * np.linspace(.8, 0, n) * (0.5 + 0.5 * np.sin(2 * np.pi * 9 * t_axis(.45)))
    return mix(at(fizz * .6, 0, total), at(crackle(n, 70) * np.linspace(1, 0, n) * .5, 0, total))


@cue
def sfx_cast_zack_livewire():
    # He throws the coil: a whip of wire through the air and post B dropping.
    total = .45
    whip = highpass(noise(int(SR * .14)), 2200) * np.linspace(0, 1, int(SR * .14)) ** 3
    drop = np.sin(2 * np.pi * 95 * t_axis(.12)) * env(int(SR * .12), .002, .04)
    return mix(at(whip * .6, 0, total), at(drop, .15, total))


@cue
def sfx_zack_livewire_taut():
    # The wire whips out and snaps taut with a twang, then settles into its hum.
    total = .9
    tw = twang(.6, 196, .18)
    buzz = hum(.7) * env(int(SR * .7), .08, 10, sustain=1, release=.2) * .25
    return mix(at(snap(.06, 3000) * .5, 0, total), at(tw, .02, total), at(buzz, .15, total))


@cue
def sfx_zack_livewire_zap():
    # An attacker crosses: a hard flash, the wire jumps (a short detuned twang), the buzz spikes.
    total = .6
    flash = highpass(noise(int(SR * .05)), 1500) * env(int(SR * .05), .0005, .012)
    spike = hum(.4, base=60, harmonics=9, buzz=.9) * env(int(SR * .4), .001, .09)
    return mix(at(flash, 0, total), at(spike * .7, 0, total), at(twang(.35, 220, .08) * .5, .01, total), at(crackle(int(SR * .4), 300) * .6, 0, total))


@cue
def sfx_zack_livewire_snap():
    # A normal wire snaps: a bright crack and the two halves whipping down, ending in three falling sparks.
    total = .7
    crack = snap(.08, 4000)
    whipdown = twang(.4, 330, .07) * .6
    sparks = sum(at(snap(.04, 5000) * .3, .25 + k * .07, total) for k in range(3))
    return mix(at(crack, 0, total), at(whipdown, .02, total), sparks)


@cue
def sfx_zack_momentum_full():
    # Momentum reaches its third step: a short rising whine and a click into gear. Quiet: it is earned, not announced.
    total = .45
    rise = whine(.3, 500, 1400) * env(int(SR * .3), .12, 10, sustain=1, release=.08) * .5
    return mix(at(rise, 0, total), at(snap(.04, 3500) * .5, .28, total))


@cue
def sfx_status_zapped():
    # The victim: a locked buzz for the second they are held, crackling, cut off clean at the shake-off.
    total = 1.05
    held = hum(1.0, base=60, harmonics=8, buzz=.7) * env(int(SR * 1.0), .003, 10, sustain=1, release=.06) * .5
    return mix(at(snap(.06, 2500), 0, total), at(held, 0, total), at(crackle(int(SR * 1.0), 140) * .4, 0, total))


@cue
def sfx_ult_theme_zack():
    # A placeholder sketch of the cutscene's sting (not music): the storm's call, the stop (everything drops out), the
    # poke's ring, and the let go landing in a rising whine as he sets off. Timed to the 4.5 s cut.
    total = 4.5
    call = whine(1.0, 120, 260) * env(int(SR * 1.0), .3, 10, sustain=1, release=.2) * .3
    stop = snap(.12, 2000)
    ring = twang(.8, 880, .25) * .25
    letgo = mix(snap(.15, 1800), whine(1.6, 300, 1800) * env(int(SR * 1.6), .1, 10, sustain=1, release=.3) * .5)
    return mix(at(call, 0, total), at(stop, 1.05, total), at(ring, 1.9, total), at(letgo, 2.75, total))


def write(path, x):
    data = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(ROOT, "Logs", "zack-sfx-drafts"))
    args = ap.parse_args()
    os.makedirs(args.out, exist_ok=True)
    for name, fn in CUES.items():
        path = os.path.join(args.out, name + ".wav")
        write(path, finish(fn()))
        print("wrote", path)


if __name__ == "__main__":
    main()
