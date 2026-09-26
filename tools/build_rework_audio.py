"""The roster rework's sounds (ABILITY-2, 2026-09-26): Cryo (Cheska), Geo (Dante), Necro (Nemu), Voodoo
(Phaister), the four new statuses, and Paete's ground call. Original and deterministic.

The baseline is docs/reports/amihan-kit-2026-09-25/direction.md section 4: every cue has a TRANSIENT,
a BODY in the ability's own texture and a TAIL that says it is over; the element is a family, the
ability is its own verb, and no two abilities share a recipe. The plan is
docs/reports/ability-rework-2026-09-26/plan.md section 5.

EACH POWER GETS ITS OWN INSTRUMENTS, NAMED WHERE THEY ARE DEFINED:
- Cryo: `glass` (inharmonic partials of a struck ice rod, the ratios of a free bar detuned so it
  shimmers rather than rings) and `crackle` (frost forming: dense tiny clicks through a very high band).
- Geo: `rumble` (low-passed noise under 90 Hz, felt more than heard), `stone` (a heavy knock with a gritty
  noise burst on top: rock on rock) and `grind` (slow stick-slip through low bands: slabs sliding).
- Necro: `wail` (noise through two moving vowel formants with a slow vibrato: a ghost's voice without a
  voice) and `puff` (a soft low-passed breath: Kuro moving).
- Voodoo: `cloth` (a short band of noise sweeping down: fabric through the air), `pin` (a thin bright
  sine with a fast decay and a tiny click: a needle) and `gravity` (a detuned stack of low partials whose
  pitch is pulled DOWN over time: the black hole's drone).
Paete keeps his own wood-and-leaf family from tools/build_paete_audio.py, imported, not copied.

Only numpy; every random source is seeded, so a rebuild writes identical bytes.
Run: python3 tools/build_rework_audio.py
"""
import json
import math
from pathlib import Path

import numpy as np

import build_paete_audio as pa
from build_paete_audio import RATE, times, noise, svf, one_pole_low, window, sweep, thump, finish

ROOT = Path(__file__).resolve().parents[1]


# ------------------------------------------------------------------ instruments

def glass(t, at, pitch, decay, gain=1.0, shimmer=0.004):
    local = np.maximum(0, t - at)
    on = t >= at
    parts = [(1.0, 1.0, 1.0), (2.76 + shimmer, 0.55, 0.7), (5.40 - shimmer, 0.35, 0.45), (8.93, 0.2, 0.3)]
    return sum(np.sin(2 * np.pi * pitch * r * local) * g * np.exp(-local / (decay * d)) for r, g, d in parts) * on * gain


def crackle(t, seed, density, centre=7200):
    return pa.rustle(t, seed, density, centre, q=2.4)


def rumble(t, seed, shape, cutoff=70.0):
    return one_pole_low(one_pole_low(noise(len(t), seed), cutoff), cutoff) * shape * 9.0


def stone(t, at, pitch, seed, gain=1.0):
    local = np.maximum(0, t - at)
    on = t >= at
    body = (np.sin(2 * np.pi * pitch * local) + 0.5 * np.sin(2 * np.pi * pitch * 1.83 * local)) * np.exp(-local / 0.09)
    grit = svf(noise(len(t), seed), 1400, 1.2) * np.exp(-local / 0.035)
    return (body + grit * 0.8) * on * gain


def grind(t, seed, rate, shape):
    return pa.creak(t, seed, rate, 0.6, [(180, 1.0), (420, 0.6)], 6.0, shape) * 2.2


def wail(t, seed, f1, f2, shape, vibrato=5.5):
    n = len(t)
    wob = 1 + 0.06 * np.sin(2 * np.pi * vibrato * t)
    src = noise(n, seed)
    return (svf(src, f1 * wob, 12.0) + 0.7 * svf(src, f2 * wob, 14.0)) * shape


def puff(t, at, seconds, seed, gain=1.0):
    return one_pole_low(noise(len(t), seed), 900) * window(t, at, at + seconds, seconds * 0.4) * gain * 2.5


def cloth(t, at, seed, gain=1.0):
    local = np.maximum(0, t - at)
    return svf(noise(len(t), seed), sweep(local, 3200, 700, 0.18), 1.6) * np.exp(-local / 0.07) * (t >= at) * gain


def pin(t, at, pitch, gain=1.0):
    local = np.maximum(0, t - at)
    click = (np.abs(local) < 0.0015) * (t >= at) * 1.5
    return (np.sin(2 * np.pi * pitch * local) * np.exp(-local / 0.06) + click) * (t >= at) * gain


def gravity(t, f0, fall, shape):
    f = f0 * (1 - fall * np.clip(t / t[-1], 0, 1))
    phase = np.cumsum(f) / RATE
    tone = sum(np.sin(2 * np.pi * phase * k * (1 + d)) / k for k, d in ((1, 0), (2, 0.003), (3, -0.004), (5, 0.006)))
    return tone * shape


# ------------------------------------------------------------------ CRYO (Cheska)

def cheska_coldfeet():
    """COLD FEET: frost racing out across the street (a crackle that spreads, dense then thinning),
    a low glassy settle as the patch locks, a cold breath tail."""
    s = 1.1
    t = times(s)
    spread = crackle(t, 9101, 5200 * window(t, 0.02, 0.6, 0.15))
    settle = glass(t, 0.08, 523, 0.35, 0.45) + glass(t, 0.14, 392, 0.4, 0.3)
    breath = one_pole_low(noise(len(t), 9102), 1400) * window(t, 0.2, 1.05, 0.3) * 0.9
    return finish("sfx_cast_cheska_coldfeet", spread + settle + breath, s)


def cheska_frostbite():
    """FROSTBITE: the slipper icing over in her hand: rising glass arpeggio and a tight crackle."""
    s = 0.7
    t = times(s)
    arp = glass(t, 0.0, 784, 0.25, 0.6) + glass(t, 0.08, 988, 0.25, 0.5) + glass(t, 0.16, 1319, 0.3, 0.55)
    ice = crackle(t, 9111, 3000 * window(t, 0.0, 0.35, 0.08))
    return finish("sfx_cast_cheska_frostbite", arp + ice, s)


def cheska_frostbite_hit():
    """A frosted slipper striking a body: a hard clack, then ice clamping shut round them."""
    s = 0.8
    t = times(s)
    clack = pa.knock(t, 0.0, 620, 0.03, 1.0)
    clamp = glass(t, 0.03, 330, 0.3, 0.9, 0.012) + crackle(t, 9121, 6000 * window(t, 0.02, 0.25, 0.05))
    return finish("sfx_cheska_frostbite_hit", clack + clamp, s)


def cheska_wall():
    """GLACIAL WALL: icicles punching up in a ripple, left to right (five glass knocks stepping up),
    over a groaning ice body."""
    s = 1.2
    t = times(s)
    rise = sum(glass(t, 0.05 + 0.07 * i, 220 * (1.12 ** i), 0.25, 0.7 - 0.05 * i, 0.01) for i in range(5))
    groan = pa.creak(t, 9131, sweep(t, 40, 90, s), 0.3, [(300, 1.0), (760, 0.4)], 10.0, window(t, 0.05, 1.0, 0.2)) * 1.6
    return finish("sfx_cast_cheska_glacialwall", rise + groan, s)


def cheska_wall_crack():
    """A slipper striking the wall: a split running through the ice."""
    s = 0.6
    t = times(s)
    split = pa.snap(t, 9141, 0.0, 26, 0.18, 4200, 1.3) + glass(t, 0.0, 260, 0.2, 0.6, 0.02)
    return finish("sfx_cheska_wall_crack", split, s)


def cheska_absolutezero():
    """ABSOLUTE ZERO: an in-breath, a beat of near silence, then the whole street freezing at once (a
    wide glass wash falling in pitch) and a cascade of ice clamps."""
    s = 2.4
    t = times(s)
    inhale = one_pole_low(noise(len(t), 9151), 2200) * window(t, 0.0, 0.45, 0.35) * 1.2
    wash = sum(glass(t, 0.6 + 0.02 * i, 1760 / (1.19 ** i), 0.9, 0.35) for i in range(6))
    sheet = crackle(t, 9152, 9000 * window(t, 0.58, 1.6, 0.4))
    clamps = sum(glass(t, 0.75 + 0.13 * i, 300 + 40 * i, 0.25, 0.4, 0.015) for i in range(4))
    sub = thump(t, 0.6, 46, 0.6)
    return finish("sfx_cast_cheska_absolutezero", inhale + wash + sheet + clamps + sub, s, 0.72)


# ------------------------------------------------------------------ GEO (Dante)

def dante_shield():
    """SHIELD: stone plates slamming shut round him one after another, then a low hum as it holds."""
    s = 1.0
    t = times(s)
    plates = sum(stone(t, 0.02 + 0.09 * i, 150 + 25 * i, 9201 + i, 0.8) for i in range(4))
    hum = np.sin(2 * np.pi * 82 * t) * window(t, 0.35, 0.98, 0.25) * 0.25
    return finish("sfx_cast_dante_shield", plates + hum, s)


def dante_boulder():
    """BOULDER: a grunting heave (a grind), the rock torn free (grit), a heavy whoosh as it leaves."""
    s = 0.8
    t = times(s)
    heave = grind(t, 9211, 70, window(t, 0.0, 0.3, 0.08))
    tear = stone(t, 0.25, 95, 9212, 0.9)
    whoosh = svf(noise(len(t), 9213), sweep(np.maximum(0, t - 0.28), 900, 260, 0.4), 1.5) * window(t, 0.28, 0.75, 0.15) * 1.4
    return finish("sfx_cast_dante_boulder", heave + tear + whoosh, s)


def dante_boulder_hit():
    """The boulder landing on someone: a thud you feel, then it rolls (a stuttering grind) and settles."""
    s = 1.0
    t = times(s)
    thud = stone(t, 0.0, 70, 9221, 1.3) + thump(t, 0.0, 52, 0.25)
    roll = grind(t, 9222, sweep(t, 30, 9, s), window(t, 0.06, 0.8, 0.2))
    return finish("sfx_dante_boulder_hit", thud + roll, s)


def dante_barrier():
    """BARRIER: three slabs grinding up out of the road in front of him and a humming field between."""
    s = 1.1
    t = times(s)
    slabs = sum(stone(t, 0.04 + 0.1 * i, 120 - 12 * i, 9231 + i, 0.7) for i in range(3))
    grinds = grind(t, 9234, 40, window(t, 0.0, 0.4, 0.1))
    field = (np.sin(2 * np.pi * 110 * t) + 0.5 * np.sin(2 * np.pi * 165 * t)) * window(t, 0.3, 1.05, 0.3) * 0.25
    return finish("sfx_cast_dante_barrier", slabs + grinds + field, s)


def dante_barrier_reflect():
    """A slipper bounced back off the field: a hard stone clack with a bright ring."""
    s = 0.45
    t = times(s)
    return finish("sfx_dante_barrier_reflect", stone(t, 0.0, 240, 9241, 1.0) + glass(t, 0.0, 880, 0.12, 0.3), s)


def dante_earthquake():
    """EARTHQUAKE: a rumble rising from nothing, the stamp (a sub hit), then cracks rolling away in all
    directions and the ground settling in heavy knocks."""
    s = 2.6
    t = times(s)
    rise = rumble(t, 9251, np.clip(t / 0.7, 0, 1) ** 2 * np.exp(-np.maximum(0, t - 0.7) / 0.9), 60)
    stamp = thump(t, 0.7, 40, 0.5) * 1.4 + stone(t, 0.7, 90, 9252, 1.0)
    cracks = pa.snap(t, 9253, 0.72, 90, 1.2, 2600, 1.2)
    settle = sum(stone(t, 1.2 + 0.28 * i, 80 - 6 * i, 9254 + i, 0.5) for i in range(4))
    return finish("sfx_cast_dante_earthquake", rise + stamp + cracks + settle, s, 0.74)


# ------------------------------------------------------------------ NECRO (Nemu)

def nemu_terrify():
    """TERRIFY: Kuro sent out (a puff), then his haunting: a hollow two-vowel wail that rises and hangs."""
    s = 1.5
    t = times(s)
    send = puff(t, 0.0, 0.25, 9301, 1.0)
    haunt = wail(t, 9302, sweep(t, 420, 700, s, 0.7), sweep(t, 1100, 1700, s), window(t, 0.15, 1.45, 0.35)) * 2.2
    return finish("sfx_cast_nemu_terrify", send + haunt, s)


def nemu_fetch():
    """KURO FETCH: a playful chirp (a quick upward wail) and a fast puff away."""
    s = 0.6
    t = times(s)
    chirp = wail(t, 9311, sweep(t, 600, 1300, 0.25), sweep(t, 1500, 2600, 0.25), window(t, 0.0, 0.28, 0.06), 9.0) * 2.0
    go = puff(t, 0.18, 0.35, 9312, 1.0)
    return finish("sfx_cast_nemu_fetch", chirp + go, s)


def nemu_fetch_drop():
    """The taya catches Kuro: a startled squeak and the slipper hitting the road."""
    s = 0.6
    t = times(s)
    squeak = wail(t, 9321, sweep(t, 1400, 700, 0.2), sweep(t, 2600, 1500, 0.2), window(t, 0.0, 0.2, 0.04), 12.0) * 2.0
    land = pa.knock(t, 0.18, 280, 0.05, 0.8)
    return finish("sfx_nemu_fetch_drop", squeak + land, s)


def nemu_guard():
    """KURO GUARD: Kuro swelling up big, a deep inflating whoosh and a low growl."""
    s = 1.0
    t = times(s)
    swell = svf(noise(len(t), 9331), sweep(t, 300, 1200, 0.6), 0.7, "low") * window(t, 0.0, 0.7, 0.3) * 2.5
    growl = wail(t, 9332, 180, 420, window(t, 0.35, 0.95, 0.2), 3.0) * 2.0
    return finish("sfx_cast_nemu_guard", swell + growl, s)


def nemu_guard_block():
    """A slipper bouncing off big Kuro: a soft rubbery bop."""
    s = 0.4
    t = times(s)
    bop = np.sin(2 * np.pi * sweep(t, 260, 140, 0.12) * t) * np.exp(-t / 0.08) + puff(t, 0.0, 0.1, 9341, 0.4)
    return finish("sfx_nemu_guard_block", bop, s)


# ------------------------------------------------------------------ VOODOO (Phaister)

def phaister_doll():
    """CURSE: DISORIENTED, the throw: cloth through the air and a little rattle of pins inside the doll."""
    s = 0.6
    t = times(s)
    throw = cloth(t, 0.0, 9401, 1.2)
    rattle = sum(pin(t, 0.05 + 0.035 * i, 2600 + 300 * i, 0.3) for i in range(4))
    return finish("sfx_cast_phaister_doll", throw + rattle, s)


def phaister_pin():
    """CURSE: VULNERABLE: a pin driven through the doll (a stab and a thin bright ring), then a chord
    that bends flat: something just went wrong for whoever is in front of her."""
    s = 1.0
    t = times(s)
    stab = pin(t, 0.0, 3100, 1.0) + cloth(t, 0.0, 9411, 0.4)
    bend = sweep(t, 1.0, 0.94, s)
    chord = (np.sin(2 * np.pi * 311 * bend * t) + np.sin(2 * np.pi * 466 * bend * t) * 0.7) * window(t, 0.08, 0.95, 0.3) * 0.35
    return finish("sfx_cast_phaister_pin", stab + chord, s)


def phaister_higop_cast():
    """HIGOP, the slow cast: power surging up through her (a rising, detuned whine), cloth whipping
    (bursts of `cloth`), pins lifting (rising pin notes)."""
    s = 2.3
    t = times(s)
    surge = gravity(t, 55, -1.2, np.clip(t / 2.2, 0, 1) ** 1.6) * 0.8
    whip = sum(cloth(t, 0.3 + 0.37 * i, 9421 + i, 0.6 + 0.08 * i) for i in range(5))
    pins = sum(pin(t, 0.4 + 0.3 * i, 1800 + 260 * i, 0.35) for i in range(6))
    return finish("sfx_cast_phaister_higop", surge + whip + pins, s)


def phaister_higop_open():
    """The hole opening: a sub drop, a tearing inrush, and the gravity drone falling in pitch."""
    s = 2.8
    t = times(s)
    drop = thump(t, 0.0, 38, 0.8) * 1.3
    rush = svf(noise(len(t), 9431), sweep(t, 3000, 300, 0.8), 0.7, "low") * window(t, 0.0, 0.9, 0.3) * 3.0
    drone = gravity(t, 70, 0.45, window(t, 0.2, 2.75, 0.6)) * 0.8
    return finish("sfx_phaister_higop_open", drop + rush + drone, s, 0.72)


def phaister_higop_close():
    """The collapse: the drone pulled to nothing and one hard thump."""
    s = 1.0
    t = times(s)
    fall = gravity(t, 90, 0.9, window(t, 0.0, 0.5, 0.2)) * 0.8
    shut = thump(t, 0.5, 44, 0.3) * 1.2 + pin(t, 0.5, 2200, 0.4)
    return finish("sfx_phaister_higop_close", fall + shut, s)


# ------------------------------------------------------------------ the four new statuses (on the victim)

def status_concussed():
    """Ears ringing after a knock: a high sine with a slow wobble and a dull thud under it."""
    s = 1.0
    t = times(s)
    ring = np.sin(2 * np.pi * (2850 + 40 * np.sin(2 * np.pi * 3 * t)) * t) * np.exp(-t / 0.45) * 0.5
    return finish("sfx_status_concussed", ring + thump(t, 0.0, 70, 0.12), s)


def status_feared():
    """A shriek stinger: a short falling wail and a heartbeat."""
    s = 0.8
    t = times(s)
    shriek = wail(t, 9501, sweep(t, 1500, 600, 0.35), sweep(t, 2900, 1400, 0.35), window(t, 0.0, 0.35, 0.05), 11.0) * 2.0
    heart = thump(t, 0.4, 60, 0.07) + thump(t, 0.58, 55, 0.07)
    return finish("sfx_status_feared", shriek + heart, s)


def status_disoriented():
    """The world going wrong: two detuned tones beating against each other and wobbling."""
    s = 1.2
    t = times(s)
    wob = 1 + 0.04 * np.sin(2 * np.pi * 1.7 * t)
    tone = (np.sin(2 * np.pi * 440 * wob * t) + np.sin(2 * np.pi * 447 * t) + 0.5 * np.sin(2 * np.pi * 660 * wob * t))
    return finish("sfx_status_disoriented", tone * window(t, 0.0, 1.15, 0.3) * 0.4, s)


def status_vulnerable():
    """A hollow, exposed ping: a pin note with a long ring and nothing under it."""
    s = 0.9
    t = times(s)
    return finish("sfx_status_vulnerable", pin(t, 0.0, 1760, 1.0) * 0.8 + np.sin(2 * np.pi * 880 * t) * np.exp(-t / 0.4) * 0.3, s)


# ------------------------------------------------------------------ PAETE: the ground call (TODO HERO-9)

def paete_ground_call():
    """MAKILING'S EMBRACE, cast (owner: *"I want him to be CALLING IT FROM THE GROUND"*): the old recipe
    described a seed and an overhand swish. Now: palms pressed to the court (a soil crunch), a root groan
    swelling underfoot, a rising rumble as the power gathers, and the knock of the ground answering."""
    # ⚠️⚠️ SHORTENED 2026-09-26 (Unity pass). This cue plays AFTER the introduction
    # (`ExecuteSharedUltimate` -> `PlayCastConfirm(afterIntroduction: true)`), at the instant the live root
    # vein starts its 0.45 s race and the tree bursts (`PaeteSentry.Flight`, `sfx_paete_sentry_burst`).
    # The 2.2 s cloud version swelled to 2.0 s and knocked at 1.95 s: 1.5 s after the tree was already up,
    # so its climax answered nothing. The kneel, press and gather are the cutscene's, and live in his theme
    # (`build_paete_audio.theme`, re-timed to the 4.6 s cutscene). What is left here is the heave that
    # launches the race: a soil crunch at the press and a groan pulling up to the burst, then out of the way.
    s = 0.8
    t = times(s)
    press = pa.snap(t, 9601, 0.0, 30, 0.08, 900, 0.8) + pa.knock(t, 0.0, 120, 0.10, 0.7) + thump(t, 0.0, 52, 0.25)
    groan = pa.creak(t, 9602, sweep(t, 30, 80, 0.45), 0.4, [(140, 1.0), (330, 0.5)], 12.0, window(t, 0.02, 0.5, 0.12)) * 3.0
    swell = rumble(t, 9603, np.clip(t / 0.45, 0, 1) ** 2 * window(t, 0.0, 0.6, 0.2), 80)
    return finish("sfx_cast_paete_sentry", press + groan + swell, s)


def paete_veins():
    """The root veins racing under the court to the spot: a travelling crackle of roots through soil,
    a grind that pans away (here: that fades and rises in pitch), and a pop where they surface."""
    s = 0.7
    t = times(s)
    race = pa.snap(t, 9611, 0.0, 70, 0.45, sweep(t, 700, 1600, 0.45), 1.0)
    grind_ = pa.creak(t, 9612, sweep(t, 60, 180, 0.45), 0.5, [(220, 1.0), (520, 0.4)], 8.0, window(t, 0.0, 0.5, 0.1)) * 2.0
    pop = pa.knock(t, 0.46, 190, 0.07, 0.9)
    return finish("sfx_paete_root_vein", race + grind_ + pop, s)


if __name__ == "__main__":
    rows = [cheska_coldfeet(), cheska_frostbite(), cheska_frostbite_hit(), cheska_wall(), cheska_wall_crack(),
            cheska_absolutezero(), dante_shield(), dante_boulder(), dante_boulder_hit(), dante_barrier(),
            dante_barrier_reflect(), dante_earthquake(), nemu_terrify(), nemu_fetch(), nemu_fetch_drop(),
            nemu_guard(), nemu_guard_block(), phaister_doll(), phaister_pin(), phaister_higop_cast(),
            phaister_higop_open(), phaister_higop_close(), status_concussed(), status_feared(),
            status_disoriented(), status_vulnerable(), paete_ground_call(), paete_veins()]
    report = {
        "provenance": "Original deterministic synthesis (numpy only); no external samples, voices or paid API.",
        "listening": "Not yet heard by the owner in the game mix. Peak and RMS are measurements, not approval.",
        "cues": rows,
    }
    out = ROOT / "docs/reports/ability-rework-2026-09-26/rework-audio.json"
    out.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Authored {len(rows)} rework cues.")
