"""Phaister's sounds (HERO-10): original, deterministic, one recipe per cue. Writes only her files.

The baseline is `docs/HERO_KIT_METHOD.md` section 5: every cue has a TRANSIENT, a BODY in the ability's own texture and a
TAIL that says it is over; the hero's element is a family of instruments and no two abilities share a recipe. Plan:
`docs/reports/phaister-kit-2026-09-27/plan.md` section 3.

SHE IS A WITCH (owner: *"make her a frigging witchh not a showman"*), SO HER FAMILY IS WINGS, CLOTH, PIN METAL, CANDLE AND
MOONLIGHT. Paete's is wood and leaf (`build_paete_audio.py`, whose filters this borrows and whose instruments it does not).
Each of hers is named where it is defined:
- `flutter`: many wings beating, each an amplitude-modulated band of noise at its own beat rate (a moth beats about 30 times
  a second, far slower than a fly), summed; grains of air, never a hiss.
- `buzz`: a beetle in flight, a low sawtooth hum through a nasal band, its pitch wobbling.
- `stitch`: a needle through cloth, a tiny tear of noise that ends in a pop.
- `tick`: a pin's bright metal, three inharmonic partials that die fast.
- `moon`: moonlight, a glassy chord of pure partials with a slow bloom and a slightly sharp top that shimmers.
- `candle`: a flame catching, a low whoomph of noise and a few crackles.
- `musicbox`: the doll's voice, struck tines (fast decay, a metallic second partial), played slightly out of tune.
- `ash`: something crumbling to ash, dense quiet clicks through a low band, thinning out.
- `hex`: HER MOTIF, three falling glassy tones (a minor third then a tritone down), heard every time her sigil is drawn.

Only numpy (via Paete's builder's filters); every random source seeded, so a rebuild writes identical bytes.

Run: python tools/build_phaister_audio.py
"""
import json
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_paete_audio as pa  # noqa: E402  (filters and the writer only; none of his instruments)
from build_paete_audio import RATE, times, noise, svf, one_pole_low, env_ar, window, sweep, finish  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]


# ------------------------------------------------------------------ her instruments

def flutter(t, seed, rates, centre, density):
    """Many wings: each (beat Hz, gain, phase) in `rates` is a band of noise opened and shut at its own beat rate; `density`
    (an array over t) is how many are in the air."""
    n = len(t)
    out = np.zeros(n)
    base = svf(noise(n, seed), centre, 1.2) + 0.5 * svf(noise(n, seed + 1), centre * 2.1, 1.6)
    for hz, gain, phase in rates:
        gate = np.clip(np.sin(2 * np.pi * hz * t + phase), 0, 1) ** 2
        out += base * gate * gain
    return out * np.broadcast_to(np.asarray(density, dtype=float), (n,))


def buzz(t, pitch, wobble, centre, shape):
    """A beetle: a sawtooth at `pitch` (Hz, may be an array) wobbling by `wobble`, through a nasal band."""
    f = np.broadcast_to(np.asarray(pitch, dtype=float), t.shape) * (1 + wobble * np.sin(2 * np.pi * 7.3 * t))
    phase = np.cumsum(f) / RATE
    saw = 2 * (phase % 1.0) - 1
    return svf(saw, centre, 3.0) * shape


def stitch(t, seed, at, gain=1.0):
    """A needle through cloth: 30 ms of tearing noise rising in pitch, then a tiny pop as it comes through."""
    local = t - at
    on = (local >= 0) & (local < 0.03)
    tear = svf(noise(len(t), seed), 2200 + 60000 * np.clip(local, 0, 0.03), 2.5) * on * np.clip(local / 0.03, 0, 1)
    pop = np.sin(2 * np.pi * 1800 * np.maximum(0, local - 0.03)) * np.exp(-np.maximum(0, local - 0.03) / 0.006) * (local >= 0.03)
    return (tear * 0.8 + pop * 0.6) * gain


def tick(t, at, pitch, gain=1.0):
    """A pin's bright metal: inharmonic partials (1, 2.92, 5.37) dying in 20 to 60 ms."""
    local = np.maximum(0, t - at)
    on = t >= at
    return sum(np.sin(2 * np.pi * pitch * r * local) * g * np.exp(-local / d)
               for r, g, d in [(1.0, 1.0, 0.06), (2.92, 0.5, 0.03), (5.37, 0.3, 0.018)]) * on * gain


def moon(t, at, root, seconds, gain=1.0):
    """Moonlight: a glassy chord (root, fifth, octave, and a ninth a hair sharp that shimmers against the octave), blooming
    over 80 ms and fading over `seconds`."""
    local = np.maximum(0, t - at)
    on = t >= at
    env = np.clip(local / 0.08, 0, 1) * np.exp(-local / max(0.05, seconds * 0.45))
    tones = [(1.0, 0.6), (1.5, 0.45), (2.0, 0.4), (2.25 * 1.004, 0.25), (4.0, 0.08)]
    return sum(np.sin(2 * np.pi * root * r * local) * g for r, g in tones) * env * on * gain


def candle(t, seed, at, gain=1.0):
    """A flame catching: a low whoomph (noise through a falling low band, 0.2 s) and three crackles after it."""
    local = t - at
    on = local >= 0
    body = svf(noise(len(t), seed), 900 * np.exp(-np.maximum(0, local) / 0.12) + 140, 0.9) * env_ar(np.maximum(0, local), 0.02, 0.09) * on
    crackle = np.zeros(len(t))
    for k, dt in enumerate((0.05, 0.11, 0.19)):
        i = int((at + dt) * RATE)
        if 0 <= i < len(t):
            crackle[i] = 1.0 - 0.25 * k
    crackle = svf(crackle, 3200, 1.8) * 1.6
    return (body * 1.6 + crackle) * gain


def musicbox(t, at, notes, detune, gain=1.0):
    """Struck tines: each (offset s, Hz) a sine with a metallic 3.0x partial, 0.5 s decay; `detune` bends each note flat by
    that fraction as it rings (the doll's tune going wrong)."""
    out = np.zeros(len(t))
    for offset, hz in notes:
        local = np.maximum(0, t - at - offset)
        on = t >= at + offset
        f = hz * (1 - detune * np.clip(local / 0.5, 0, 1))
        phase = 2 * np.pi * np.cumsum(np.where(on, f, 0.0)) / RATE
        out += (np.sin(phase) + 0.25 * np.sin(3.0 * phase)) * np.exp(-local / 0.5) * on
    return out * gain


def ash(t, seed, at, seconds, gain=1.0):
    """Crumbling to ash: dense quiet clicks through a low-mid band, thinning over `seconds`."""
    local = t - at
    dens = 2400 * np.clip(1 - local / seconds, 0, 1) * (local >= 0)
    rng = np.random.default_rng(seed)
    hits = (rng.random(len(t)) < dens / RATE) * rng.normal(0, 1, len(t))
    return (svf(hits, 1400, 1.1) + 0.5 * svf(hits, 3600, 1.4)) * gain


def hex_motif(t, at, root, gain=1.0):
    """HER MOTIF: three falling glassy tones, 70 ms apart (root, a minor third down, a tritone below that)."""
    out = np.zeros(len(t))
    for k, ratio in enumerate((1.0, 0.8409, 0.5946)):
        local = np.maximum(0, t - at - 0.07 * k)
        on = t >= at + 0.07 * k
        f = root * ratio
        out += (np.sin(2 * np.pi * f * local) + 0.3 * np.sin(2 * np.pi * f * 2.005 * local)) * np.exp(-local / 0.28) * on * (1 - 0.15 * k)
    return out * gain


WINGS = [(28.0, 1.0, 0.0), (31.5, 0.8, 1.3), (25.0, 0.9, 2.1), (34.0, 0.6, 0.7), (29.3, 0.7, 2.9), (22.5, 0.5, 4.1)]


# ------------------------------------------------------------------ the recipes

def blink_cast():
    """VANISHING ACT, where she LEAVES (0.55 s): a candle-flame whoomph as she bursts, the swarm's wings rising dense and
    streaming away (their band sweeping up as they go), a beetle buzz under it, dying out along the lane."""
    s = 0.55
    t = times(s)
    burst = candle(t, 9101, 0.0, 1.0)
    dens = np.clip(t / 0.03, 0, 1) * np.exp(-np.maximum(0, t - 0.05) / 0.16)
    wings = svf(flutter(t, 9102, WINGS, 2400, dens), sweep(t, 1800, 4200, 0.35), 0.8) * 1.4
    beetle = buzz(t, sweep(t, 150, 190, 0.3), 0.05, 900, window(t, 0.01, 0.3, 0.05)) * 0.35
    return finish("sfx_cast_phaister_blink", burst + wings + beetle, s)


def swarm_knit():
    """VANISHING ACT, where she ARRIVES (0.9 s): the swarm arriving and spiralling in (wings swelling, the band falling as it
    packs tight), her motif as the sigil flares, and a last few wings settling on her hat."""
    s = 0.9
    t = times(s)
    dens = np.clip(t / 0.12, 0, 1) * np.exp(-np.maximum(0, t - 0.18) / 0.10) + 0.25 * window(t, 0.35, 0.8, 0.1)
    wings = svf(flutter(t, 9111, WINGS[:4], 2600, dens), sweep(t, 3600, 1600, 0.25), 0.8) * 1.3
    motif = hex_motif(t, 0.20, 1320, 0.55)
    return finish("sfx_phaister_swarm_knit", wings + motif, s)


def doll_cast():
    """MANIKA MISCHIEF's press (0.6 s): a needle pricking the doll, a breath of air (her whisper, synthesized as air only,
    never a voice), the overhand throw's swish and the pins rattling inside as it tumbles away."""
    s = 0.6
    t = times(s)
    prick = stitch(t, 9121, 0.02, 1.0)
    breath = svf(noise(len(t), 9122), 2800, 0.8) * env_ar(np.maximum(0, t - 0.08), 0.06, 0.08) * (t >= 0.08) * 0.25
    swish = svf(noise(len(t), 9123), sweep(t - 0.28, 700, 2600, 0.12), 1.4) * env_ar(np.maximum(0, t - 0.28), 0.03, 0.06) * (t >= 0.28) * 0.9
    rattle = sum(tick(t, 0.32 + 0.045 * k, 3100 + 170 * (k % 3), 0.18 - 0.02 * k) for k in range(6))
    return finish("sfx_cast_phaister_doll", prick + breath + swish + rattle, s)


def manika_land():
    """The doll missing (0.8 s): a soft cloth thump, then one sad tine going flat."""
    s = 0.8
    t = times(s)
    thump = one_pole_low(noise(len(t), 9131), 320) * env_ar(t, 0.004, 0.05) * 2.5
    tine = musicbox(t, 0.2, [(0.0, 659.3)], 0.05, 0.35)
    return finish("sfx_phaister_manika_land", thump + tine, s, 0.55)


def manika_steal():
    """The doll taking its victim (1.4 s): a slap, a sucking chime rising as their colour drains in, then the doll's tune on
    a music box, detuning as it rings (their head is hers to turn now)."""
    s = 1.4
    t = times(s)
    slap = svf(noise(len(t), 9141), 1500, 0.9) * env_ar(t, 0.002, 0.03) * 1.4
    suck = np.sin(2 * np.pi * np.cumsum(sweep(t, 300, 1200, 0.25)) / RATE) * window(t, 0.02, 0.27, 0.04) * 0.35
    tune = musicbox(t, 0.3, [(0.0, 880.0), (0.14, 739.99), (0.28, 659.26), (0.46, 587.33), (0.62, 523.25)], 0.035, 0.45)
    return finish("sfx_phaister_manika_steal", slap + suck + tune, s)


def manika_crumble():
    """The doll let go (0.6 s): a few stitches popping, then it crumbles to ash."""
    s = 0.6
    t = times(s)
    pops = sum(stitch(t, 9151 + k, 0.02 + 0.05 * k, 0.5) for k in range(3))
    dust = ash(t, 9155, 0.12, 0.45, 0.7)
    return finish("sfx_phaister_manika_crumble", pops + dust, s, 0.5)


def pin_cast():
    """SPOTLIGHT PIN's press (0.7 s): the pin drawn from the hat band (two ticks), the stab, and her motif as the sigils sweep
    the cone, with a thin glassy streak under the sweep moving up in pitch left to right."""
    s = 0.7
    t = times(s)
    draw = tick(t, 0.0, 2600, 0.6) + tick(t, 0.06, 3100, 0.45)
    stab = svf(noise(len(t), 9161), 1900, 1.2) * env_ar(np.maximum(0, t - 0.16), 0.004, 0.03) * (t >= 0.16) * 0.8
    motif = hex_motif(t, 0.18, 1480, 0.6)
    streak = np.sin(2 * np.pi * np.cumsum(sweep(np.maximum(0, t - 0.18), 1200, 2400, 0.2)) / RATE) * window(t, 0.18, 0.40, 0.04) * 0.15
    return finish("sfx_cast_phaister_pin", draw + stab + motif + streak, s)


def moonlight_on():
    """The moonlight dropping on a player (1.2 s): a pin of light striking the court (a bright tick), and the moon chord
    blooming over it."""
    s = 1.2
    t = times(s)
    strike = tick(t, 0.06, 2200, 0.8)
    chord = moon(t, 0.02, 523.25, 1.1, 0.6)
    return finish("sfx_phaister_moonlight_on", strike + chord, s, 0.6)


def moonlight_off():
    """The moonlight ending (0.5 s): a small snap as the beam goes out, then the pin cooling to ash."""
    s = 0.5
    t = times(s)
    snap_ = svf(noise(len(t), 9171), 4200, 1.6) * env_ar(t, 0.002, 0.012) * 1.2
    fall = moon(t, 0.0, 392.0, 0.25, 0.25)
    dust = ash(t, 9172, 0.08, 0.35, 0.4)
    return finish("sfx_phaister_moonlight_off", snap_ + fall + dust, s, 0.45)


def glitch(t, seed, start, end, rate, gain=1.0):
    """The eye forming (owner: *"glitchy and unstable as fuck ... pulsate"*): a crackle chopped by a stepped gate that
    stutters at `rate` steps a second, each step its own pitch of buzz or silence, like a signal breaking up."""
    n = len(t)
    rng = np.random.default_rng(seed)
    steps = int((end - start) * rate) + 2
    levels = rng.random(steps) > 0.35
    pitches = 180 + 900 * rng.random(steps)
    idx = np.clip(((t - start) * rate).astype(int), 0, steps - 1)
    inside = (t >= start) & (t < end)
    gate = levels[idx] * inside
    f = pitches[idx]
    phase = np.cumsum(f) / RATE
    square = np.sign(np.sin(2 * np.pi * phase))
    crackle = svf(noise(n, seed + 1), 3200, 1.5)
    return (square * 0.35 + crackle * 0.8) * gate * gain


def theme():
    """OMEN's cutscene, 4.0 s, timed to its beats (`tools/author_ultimate_intros.py` phaister(); the method: one theme file,
    a cut-in accent on frame 0, a swell drawn in before the impact and cut dead on it, the impact in her own material, her
    motif where her sign is drawn, a tail):
      0.00       the cut-in: a candle-flame whoomph and the night falling (a low swell)
      0.15-1.45  SURGE: wings rising round her, dense and circling (the flutter band swinging left and right), a rising wind
      1.45-2.55  THE EYE: the wings sucked in and gone; the eye forming, GLITCHING and pulsing (a stuttering crackle whose rate
                 climbs), a low pulse on the beat of its throb; her motif as it steadies in her hands (2.2)
      2.55-2.80  the throw: a whoosh upward, the swell drawn in hard
      2.80       the IMPACT, cut dead on it: a deep whump and the moon chord blooming, a two-frame silence before it
      2.85-4.00  the maelstrom: a turning wind and many wings, fading as play returns."""
    s = 4.0
    t = times(s)
    cut_in = candle(t, 9181, 0.0, 1.2) + one_pole_low(noise(len(t), 9182), 90) * env_ar(t, 0.3, 0.6) * 1.5
    circling = 0.6 + 0.4 * np.sin(2 * np.pi * 1.4 * t)
    wings = svf(flutter(t, 9183, WINGS, 2400, window(t, 0.15, 1.5, 0.3) * circling), sweep(t, 1600, 3000, 1.45), 0.8) * 1.2
    wind = svf(noise(len(t), 9184), sweep(t, 300, 1400, 2.7), 1.1) * np.clip((t - 0.2) / 2.3, 0, 1) ** 2 * window(t, 0.2, 2.8, 0.02) * 1.1
    stutter = glitch(t, 9185, 1.5, 2.55, 14.0, 0.6) + glitch(t, 9186, 2.1, 2.55, 26.0, 0.5)
    throb = np.sin(2 * np.pi * 55 * t) * (0.5 + 0.5 * np.sin(2 * np.pi * 3.7 * t)) ** 2 * window(t, 1.5, 2.55, 0.05) * 0.9
    motif = hex_motif(t, 2.2, 1320, 0.55)
    throw = svf(noise(len(t), 9187), sweep(np.maximum(0, t - 2.55), 500, 3500, 0.25), 1.4) * window(t, 2.55, 2.78, 0.03) * 1.2
    silence = 1.0 - window(t, 2.745, 2.80, 0.005)
    whump = np.sin(2 * np.pi * 48 * np.maximum(0, t - 2.8) * (1 - 0.3 * np.maximum(0, t - 2.8))) * np.exp(-np.maximum(0, t - 2.8) / 0.35) * (t >= 2.8) * 2.2
    chord = moon(t, 2.8, 392.0, 1.2, 0.9)
    maelstrom = svf(noise(len(t), 9188), 700 + 300 * np.sin(2 * np.pi * 0.9 * t), 1.0) * window(t, 2.85, 4.0, 0.3) * 0.9
    tail = svf(flutter(t, 9189, WINGS[:4], 2000, window(t, 2.9, 4.0, 0.4)), 2200, 0.8) * 0.8
    mix = (cut_in + wings + wind + stutter + throb + motif + throw) * silence + whump + chord + maelstrom + tail
    return finish("sfx_ult_theme_phaister", mix, s, 0.75)


if __name__ == "__main__":
    rows = [blink_cast(), swarm_knit(), doll_cast(), manika_land(), manika_steal(), manika_crumble(), pin_cast(),
            moonlight_on(), moonlight_off(), theme()]
    report = {
        "provenance": "Original deterministic synthesis (numpy only); no external samples, voices or paid API.",
        "listening": "Not yet heard by the owner in the game mix. Peak and RMS are measurements, not approval.",
        "cues": rows,
    }
    out = ROOT / "docs/reports/phaister-kit-2026-09-27/phaister-audio.json"
    out.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Authored {len(rows)} Phaister cues; nothing else touched.")
