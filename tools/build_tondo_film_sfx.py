"""The Tondo film's score and cues (Eskinita's opening), built from REAL recordings around one shared theme.

  py -3 tools/build_tondo_film_sfx.py              fetch what is missing, then build every Tondo file
  py -3 tools/build_tondo_film_sfx.py --fetch      only download and check the sources
  py -3 tools/build_tondo_film_sfx.py --only clap  build only the files whose name contains "clap"

Owner, 2026-10-23: "this game has horrible sfx rn", "thoroughly think abt how to make sfx cohesive kasi rn at current
state its js noise and a bunch of sounds taht dont have a theme and dont work well together or dont support each
other". docs/reports/sfx-2026-10-23/research.md is the why; this is the how. The earlier recipes (band noise, sine
bells, Karplus-Strong strings) were in tools/synth_map_film_sfx.py and are kept as the "before" files in
docs/reports/sfx-2026-10-23/tondo/before/.

THE THEME (the one rule set every file below obeys, research.md section 4)
  The band lives in the alley. Everything is something the eskinita's kids could play or touch: a toy glockenspiel
  and xylophone, a harp standing in for the rondalla's plucked strings, a marimba and a log drum for warm wood,
  claves, a triangle, real hands, chalk, a wooden door, a tin can. No sine bells, no noise pretending to be an object.
  One key and grid: C major pentatonic at 100 bpm (0.6 s a beat), on the film's own beat table.
  One motif: do-mi-sol-do (C5 E5 G5 C6). The lullaby opens on it, the four doors each ring its next note, the taya
  sting completes it an octave up.
  One room: every file goes through ROOM, the same small concrete alley.
  Foley in the cue, the tuned answer in the score: the film plays the door and clap files at per-seat pitches
  (TondoFilm.cs Walkers and Count), so their tuned layer is written into the SCORE at the cue's film time instead.
  Lanes: the score keeps the lows and leaves a pocket on each cue; chalk is high grain, the door a mid wooden knock,
  the claps a bright snap, the sting a wide moment that leaves 300 to 3000 Hz to the kid's recorded "Taya!".

WHERE THINGS ARE
  tools/tondo_film_sfx_sources.json        every source: page, author, licence, SHA-256, and what it is used for
  ArtSource/tondo-film/sfx-sources/        the downloads as fetched (Freesound HQ previews, VSCO 2 CE WAVs)
  Assets/TumbangPreso/Resources/Sfx/sfx_film_tondo_{score,chalk,door,clap,taya}.wav   mono, 44.1 kHz, 16 bit

Sources: Freesound sounds whose own page states CC0 and no other Creative Commons licence (the method of
tools/build_eskinita_life_sfx.py), and VSCO 2 Community Edition (github.com/sgossner/VSCO-2-CE, CC0 1.0).
numpy, scipy, and ffmpeg on PATH for decoding. Fixed seeds, so a rebuild from the same sources is byte-identical.
No human voice is made or cut here; the kid's "Taya!" is SidewalkLife.KidTaya, a recording the film borrows.
"""
import argparse
import datetime
import hashlib
import html
import json
import re
import shutil
import subprocess
import tempfile
import time
import urllib.request
import wave
from pathlib import Path

import numpy as np
from scipy import signal

import tump_synth as ts

SR = 44100
PEAK = 0.85
ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "tools/tondo_film_sfx_sources.json"
PROFILES = ROOT / "tools/tump_sfx_profiles.json"
CACHE = ROOT / "ArtSource/tondo-film/sfx-sources"
OUT = ROOT / "Assets/TumbangPreso/Resources/Sfx"
AGENT = "TumbangPresoFilmSfx/1.0 (indie game, CC0 recordings, one request at a time)"
CC0_MARK = "creativecommons.org/publicdomain/zero/1.0"
OTHER_LICENCES = ("creativecommons.org/licenses/by", "creativecommons.org/licenses/sampling")
VSCO_RAW = "https://raw.githubusercontent.com/sgossner/VSCO-2-CE/master/"
TODAY = datetime.date.today().isoformat()

# Freesound id -> what it is for. Chosen by title, description and measurement; the owner's ear decides.
FREESOUND = {
    # hands (T-04): several different single claps, so the four syllables are four mixes of real hands
    "151219": "clap", "463687": "clap", "789991": "clap", "860150": "clap", "561089": "clap", "475559": "clap",
    # chalk on concrete (T-02), and a rubber tsinelas scuff for the skid
    "440442": "chalk", "350307": "chalk", "560366": "skid", "452865": "skid",
    # a light wooden door (T-03): the latch, the creak
    "757194": "door", "778462": "door", "196148": "door", "213404": "door",
    # the tin can, the game's own object, as the taya sting's accent (T-05)
    "500029": "can", "178183": "can", "829242": "can",
}

# VSCO 2 CE files: the toy band.
VSCO = [
    "Percussion/Glock/glock_medium_G4.wav", "Percussion/Glock/glock_medium_C5.wav", "Percussion/Glock/glock_medium_G5.wav",
    "Percussion/Glock/glock_medium_C6.wav", "Percussion/Glock/glock_medium_G6.wav", "Percussion/Glock/glock_medium_C7.wav",
    "Percussion/Xylo/Xylo_Medium_G4_ff_01_far.wav", "Percussion/Xylo/Xylo_Medium_C5_ff_01_far.wav",
    "Percussion/Xylo/Xylo_Medium_G5_ff_01_far.wav", "Percussion/Xylo/Xylo_Medium_C6_ff_01_far.wav",
    "Percussion/Xylo/Xylo_Medium_G6_ff_01_far.wav",
    "Percussion/Marimba/Marimba_hit_Outrigger_F1_loud_01.wav", "Percussion/Marimba/Marimba_hit_Outrigger_C2_loud_01.wav",
    "Percussion/Marimba/Marimba_hit_Outrigger_G2_loud_01.wav", "Percussion/Marimba/Marimba_hit_Outrigger_F3_loud_01.wav",
    "Percussion/Marimba/Marimba_hit_Outrigger_C4_loud_01.wav", "Percussion/Marimba/Marimba_hit_Outrigger_G4_loud_01.wav",
    "Strings/Harp/KSHarp_A2_mf.wav", "Strings/Harp/KSHarp_C3_mf.wav", "Strings/Harp/KSHarp_E3_mf.wav", "Strings/Harp/KSHarp_G3_mf.wav",
    "Strings/Harp/KSHarp_B3_mf.wav", "Strings/Harp/KSHarp_D4_mf.wav", "Strings/Harp/KSHarp_F4_mf.wav", "Strings/Harp/KSHarp_A4_mf.wav",
    "Strings/Harp/KSHarp_C5_mf.wav", "Strings/Harp/KSHarp_E5_mf.wav", "Strings/Harp/KSHarp_G5_mf.wav",
    "Percussion/Claves1_Hit_v2_rr1_Sum.wav", "Percussion/Claves1_Hit_v2_rr2_Sum.wav",
    "Percussion/LogDrumLo_MedM_v2_rr1_Sum.wav", "Percussion/LogDrumHi_MedM_v2_rr1_Sum.wav", "Percussion/LogDrumHi_MedM_v2_rr2_Sum.wav",
    "Percussion/Triangle3-Hit_v1_rr1_Sum.wav", "Percussion/Tamb1-Shake_v1_rr1_Sum.wav", "Percussion/Tamb1-Shake_v1_rr2_Sum.wav",
    "Percussion/Guiro-Hit_v1_rr1_Sum.wav",
]


# ------------------------------------------------------------------ the ledger and the downloads

def load_manifest():
    if MANIFEST.exists():
        return json.loads(MANIFEST.read_text(encoding="utf-8"))
    return {"about": "Provenance for the Tondo film's score and cues (docs/reports/sfx-2026-10-23). tools/build_tondo_film_sfx.py "
                     "reads this and fills in what it fetches. Freesound sounds are used only if their own page states CC0 and no "
                     "other Creative Commons licence; VSCO 2 Community Edition is CC0 1.0 by its repository's licence. "
                     "Only the owner's ears approve a sound.",
            "cache": "ArtSource/tondo-film/sfx-sources", "sources": []}


def save_manifest(m):
    MANIFEST.write_text(json.dumps(m, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")


def get(url):
    req = urllib.request.Request(url, headers={"User-Agent": AGENT})
    with urllib.request.urlopen(req, timeout=90) as f:
        return f.read()


def fetch(m, pause=1.5):
    CACHE.mkdir(parents=True, exist_ok=True)
    known = {s["id"]: s for s in m["sources"]}
    for ident, use in FREESOUND.items():
        if ident not in known:
            known[ident] = {"id": ident, "origin": "freesound", "page": f"https://freesound.org/s/{ident}/", "use": use, "file": f"{use}_fs{ident}.mp3"}
            m["sources"].append(known[ident])
    for path in VSCO:
        ident = "vsco:" + path
        if ident not in known:
            known[ident] = {"id": ident, "origin": "VSCO 2 Community Edition", "page": "https://github.com/sgossner/VSCO-2-CE",
                            "url": VSCO_RAW + path, "use": "band", "file": "vsco_" + Path(path).name,
                            "author": "Versilian Studios (Sam Gossner)", "licence": "Creative Commons 0 1.0 (the repository's LICENSE)",
                            "licence_confirmed": True}
            m["sources"].append(known[ident])
    for s in m["sources"]:
        path = CACHE / s["file"]
        if not path.exists():
            try:
                if s["origin"] == "freesound":
                    page = get(s["page"]).decode("utf-8", "replace")
                    time.sleep(pause)
                    if CC0_MARK not in page or any(o in page for o in OTHER_LICENCES):
                        s["licence_confirmed"] = False
                        s["skipped"] = "its page does not state Creative Commons 0 (and only that)"
                        print("SKIPPED %s: page does not state CC0" % s["id"])
                        continue
                    url = re.search(r"previews/\d+/%s_\d+-hq\.mp3" % s["id"], page)
                    who = re.search(r"<title>Freesound - (.*?) by ([^<]+)</title>", page, re.S)
                    said = re.search(r'<meta name="twitter:description" content="([^"]*)"', page)
                    if not url or not who:
                        raise ValueError("no preview URL or author on the page")
                    s["url"] = "https://cdn.freesound.org/" + url.group(0)
                    s["name_on_page"] = html.unescape(who.group(1)).strip()
                    s["author"] = html.unescape(who.group(2)).strip()
                    s["description"] = re.sub(r"\s+", " ", html.unescape(said.group(1))).strip()[:400] if said else ""
                    s["licence"] = "Creative Commons 0 1.0 (as stated on the sound's page)"
                    s["licence_confirmed"] = True
                data = get(s["url"])
                time.sleep(pause)
                path.write_bytes(data)
                s["fetched"] = TODAY
                s.pop("skipped", None)
            except Exception as e:                          # a failed source is skipped and reported, never substituted
                s["skipped"] = "download failed: %s" % e
                print("SKIPPED %s: %s" % (s["id"], e))
                save_manifest(m)
                continue
        data = path.read_bytes()
        s["sha256"], s["bytes"] = hashlib.sha256(data).hexdigest(), len(data)
        s["seconds"] = round(len(decode(path)) / SR, 3)
        save_manifest(m)
        print("%-46s %8d bytes %6.2f s  %s" % (s["file"], len(data), s["seconds"], s.get("name_on_page", "")[:50]))
    save_manifest(m)


def ffmpeg():
    exe = shutil.which("ffmpeg")
    if not exe:
        raise SystemExit("ffmpeg is needed on PATH to decode the sources")
    return exe


_decoded = {}


def decode(path):
    """Any source to mono float at 44.1 kHz, through ffmpeg."""
    path = Path(path)
    if path not in _decoded:
        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp) / "x.wav"
            subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-i", str(path), "-ac", "1", "-ar", str(SR), "-sample_fmt", "s16", str(out)], check=True)
            with wave.open(str(out)) as w:
                _decoded[path] = np.frombuffer(w.readframes(w.getnframes()), dtype="<i2").astype(np.float64) / 32768.0
    return _decoded[path]


# ------------------------------------------------------------------ analysis: the references, measured

# name -> (kind, file in CACHE, around which second the event is, how long to look, extra)
# kind: "modal" (struck/plucked: partials and decays), "texture" (noisy: spectrum, envelope, grain rate), "creak".
REFS = {
    # 1 lata: the tin can
    "lata_coffee": ("modal", "can_fs178183.mp3", 0.0, 0.04, {}),
    "lata_finger": ("modal", "can_fs500029.mp3", 0.29, 0.04, {}),
    # 2 toy glock
    "glock_C5": ("modal", "vsco_glock_medium_C5.wav", 0.0, 0.3, {"near": ts.hz("C5")}),
    "glock_G5": ("modal", "vsco_glock_medium_G5.wav", 0.0, 0.3, {"near": ts.hz("G5")}),
    "glock_C6": ("modal", "vsco_glock_medium_C6.wav", 0.0, 0.3, {"near": ts.hz("C6")}),
    # 3 kahoy: wood
    "xylo_C5": ("modal", "vsco_Xylo_Medium_C5_ff_01_far.wav", 0.0, 0.15, {"near": ts.hz("C5")}),
    "xylo_G5": ("modal", "vsco_Xylo_Medium_G5_ff_01_far.wav", 0.0, 0.15, {"near": ts.hz("G5")}),
    "xylo_C6": ("modal", "vsco_Xylo_Medium_C6_ff_01_far.wav", 0.0, 0.15, {"near": ts.hz("C6")}),
    "marimba_C2": ("modal", "vsco_Marimba_hit_Outrigger_C2_loud_01.wav", 0.0, 0.4, {"near": ts.hz("C2")}),
    "marimba_G2": ("modal", "vsco_Marimba_hit_Outrigger_G2_loud_01.wav", 0.0, 0.4, {"near": ts.hz("G2")}),
    "marimba_C4": ("modal", "vsco_Marimba_hit_Outrigger_C4_loud_01.wav", 0.0, 0.3, {"near": ts.hz("C4")}),
    "claves": ("modal", "vsco_Claves1_Hit_v2_rr1_Sum.wav", 0.0, 0.05, {}),
    "logdrum_lo": ("modal", "vsco_LogDrumLo_MedM_v2_rr1_Sum.wav", 0.0, 0.08, {}),
    "logdrum_hi": ("modal", "vsco_LogDrumHi_MedM_v2_rr1_Sum.wav", 0.0, 0.08, {}),
    "triangle": ("modal", "vsco_Triangle3-Hit_v1_rr1_Sum.wav", 0.0, 0.3, {}),
    # 4 rondalla pluck
    "harp_C3": ("modal", "vsco_KSHarp_C3_mf.wav", 0.0, 0.4, {"near": ts.hz("C3"), "partials": 14}),
    "harp_G3": ("modal", "vsco_KSHarp_G3_mf.wav", 0.0, 0.4, {"near": ts.hz("G3"), "partials": 14}),
    "harp_C5": ("modal", "vsco_KSHarp_C5_mf.wav", 0.0, 0.3, {"near": ts.hz("C5"), "partials": 12}),
    # 5 kamay: hands
    "clap_a": ("texture", "clap_fs463687.mp3", 0.09, 0.25, {}),
    "clap_b": ("texture", "clap_fs561089.mp3", 0.0, 0.25, {}),
    "clap_c": ("texture", "clap_fs789991.mp3", 0.58, 0.25, {}),
    "clap_d": ("texture", "clap_fs860150.mp3", 0.01, 0.25, {}),
    "clap_a_body": ("modal", "clap_fs463687.mp3", 0.09, 0.02, {"partials": 6}),
    # 6 kalye: street foley
    "chalk_scrape": ("texture", "chalk_fs440442.mp3", 0.16, 3.5, {}),
    "chalk_stroke": ("texture", "chalk_fs350307.mp3", 1.10, 0.4, {}),
    "slap_a": ("texture", "skid_fs560366.mp3", 0.72, 0.2, {}),
    "slap_b": ("texture", "skid_fs560366.mp3", 1.16, 0.2, {}),
    "scuff": ("texture", "skid_fs452865.mp3", 0.0, 0.12, {}),
    "door_creak": ("creak", "door_fs196148.mp3", 0.28, 0.47, {}),
    "door_latch": ("modal", "door_fs757194.mp3", 0.10, 0.03, {"partials": 8}),
    "door_latch_tex": ("texture", "door_fs757194.mp3", 0.10, 0.08, {}),
    "door_bump": ("modal", "door_fs213404.mp3", 0.21, 0.06, {"partials": 8, "hi": 4000}),
    "door_bump_tex": ("texture", "door_fs213404.mp3", 0.21, 0.12, {}),
    "shaker": ("texture", "vsco_Tamb1-Shake_v1_rr1_Sum.wav", 0.0, 0.4, {}),
}


def analyse():
    """Measure every reference into tools/tump_sfx_profiles.json (committed: a rebuild needs only this file)."""
    m = load_manifest()
    by_file = {s["file"]: s for s in m["sources"]}
    out = {"about": "Numbers measured from CC0 reference recordings by tools/build_tondo_film_sfx.py --analyse (tools/tump_synth.py "
                    "does the measuring). The synthesiser builds every TUMP sound from these; no reference audio ships. "
                    "modal: partials {f Hz, db rel. loudest, decay dB/s}, the first is the fundamental when 'near' was given. "
                    "texture: sixth-octave band levels from 62.5 Hz, attack, decay to -20/-40 dB, grains per second. "
                    "creak: stick-slip pulse rate (Hz) every hop_s, and the body's resonances.",
           "bands_hz": ts.SIXTH_OCTAVES, "profiles": {}}
    for name, (kind, file, at, length, extra) in REFS.items():
        x = decode(CACHE / file)
        if kind == "creak":
            prof = ts.creak(x, at, length)
        else:
            a = max(0, int((at - 0.02) * SR)) if at > 0 else 0
            seg = x[a:]
            if kind == "modal":
                prof = {"partials": ts.modal(seg, partials=extra.get("partials", 10), window=length, near=extra.get("near"),
                                             hi=extra.get("hi", 16000.0))}
            else:
                prof = ts.texture(seg, length=length)
        src = by_file[file]
        prof["from"] = {"source": src["id"], "page": src["page"], "author": src.get("author", ""), "at_s": at}
        out["profiles"][name] = prof
        brief = prof.get("partials") or prof.get("body")
        print("%-15s %-8s %s" % (name, kind, ("f0 %.1f, %d partials, decays %s" % (brief[0]["f"], len(brief), [p["decay"] for p in brief[:5]]))
                                  if brief else ("attack %.2f ms, -20 dB at %.0f ms, %.0f grains/s" % (prof["attack_ms"], prof["decay20_ms"], prof["grains_per_s"]))))
    PROFILES.write_text(json.dumps(out, indent=1) + "\n", encoding="utf-8")
    return out


# ------------------------------------------------------------------ the palette (plan.md section 2), generated

P = {}          # the profiles, loaded in main()


def tuned(name, floor_db=-36.0, lowest=0.0):
    """A pitched profile as (root Hz, partials): the root is the LOUDEST partial (VSCO's far mics and the bars' own
    resonators leave faint neighbours, which are dropped below floor_db, and handling rumble below `lowest`)."""
    parts = [p for p in P[name]["partials"] if p["db"] > floor_db and p["f"] > lowest]
    root = max(parts, key=lambda p: p["db"])
    return root["f"], sorted(parts, key=lambda p: p["f"])


def nearest(prefix, f):
    """Of an instrument's measured notes, the profile nearest in pitch to f."""
    names = [n for n in P if n.startswith(prefix + "_") and "_" not in n[len(prefix) + 1:]]
    return min(names, key=lambda n: abs(np.log2(tuned(n)[0] / f)))


def mallet(prefix, f, seconds, hardness=0.6, seed=0, decay_scale=1.0, click=0.03, brightness=1.0):
    """Instruments 2, 3, 4: a note of a measured instrument at f Hz, from its nearest measured note."""
    root, parts = tuned(nearest(prefix, f))
    return ts.strike(parts, seconds, pitch=f / root, hardness=hardness, seed=seed, decay_scale=decay_scale,
                     click=click, brightness=brightness, f_ref=root)


def glock(note, seconds=2.4, hardness=0.55, seed=0, **kw):
    return mallet("glock", ts.hz(note), seconds, hardness, seed, **kw)


def xylo(note, seconds=0.9, hardness=0.6, seed=0, **kw):
    return mallet("xylo", ts.hz(note), seconds, hardness, seed, **kw)


def marimba(note, seconds=2.0, hardness=0.35, seed=0, **kw):
    return mallet("marimba", ts.hz(note), seconds, hardness, seed, **kw)


def pluck(note, seconds=2.0, hardness=0.75, seed=0, **kw):
    """4 rondalla pluck: the measured harp's partials, plucked near the bridge (hard), plus the finger's scrape."""
    return mallet("harp", ts.hz(note), seconds, hardness, seed, click=0.07, **kw)


def wood(name, seconds=0.5, hardness=0.7, seed=0, pitch=1.0, lowest=0.0, decay_scale=1.0, click=0.08):
    """Claves, log drums, door wood: an untuned measured body. Decays measured from a 50 ms window of a damped
    impact read long (round 1's door bump rang like a bell): impacts that are not instruments take decay_scale 4-6."""
    root, parts = tuned(name, floor_db=-30, lowest=lowest)
    return ts.strike(parts, seconds, pitch=pitch, hardness=hardness, seed=seed, click=click, f_ref=root, decay_scale=decay_scale)


def lata(seconds=1.2, pitch=1.0, hardness=0.85, seed=0, which="lata_coffee", decay_scale=1.0):
    """1 the tin can: its measured inharmonic partials, a hard tip."""
    root, parts = tuned(which, floor_db=-24)
    return ts.strike(parts, seconds, pitch=pitch, hardness=hardness, seed=seed, click=0.12, f_ref=root, decay_scale=decay_scale)


def clap_hand(name, seed=0, body="clap_a_body"):
    """5 one hand clap: the measured clap's coloured burst, plus the cupped palms' measured resonances."""
    pr = P[name]
    y = ts.burst(pr["bands"], 0.5, min(pr["decay20_ms"], 22.0), seed=seed, seconds=0.2)
    # the crack: the first millisecond and a half of palms meeting, broadband
    n = int(0.0015 * SR)
    crack = ts.highpass(np.random.default_rng(seed + 7).normal(0, 1, n), 900) * np.hanning(n)
    ts.place(y, crack / (np.abs(crack).max() + 1e-9) * 1.4, 0.0)
    # the cupped palms ring briefly (round 1: at the measured decay they sang a steady 3 kHz tone, so x4 here)
    _, parts = tuned(body, floor_db=-14)
    res = ts.strike(parts, 0.2, hardness=1.0, seed=seed + 50, click=0.0, decay_scale=4.0)
    return y + 0.18 * res / (np.abs(res).max() + 1e-9)


def normal(y, peak=1.0):
    return y / (np.abs(y).max() + 1e-12) * peak


def finish(y, wet=0.16, hp=120.0, drive=1.15):
    """Every file: the house high-pass (lows belong to the score's marimba and log drum), THE room, the glue."""
    y = ts.highpass(y, hp)
    y = ts.room(y, wet)
    return ts.glue(normal(y, 0.95), drive)


# ------------------------------------------------------------------ the film's times (TondoFilm.cs constants)
#
# CHALK 3.6: the chalk cue fires at 3.65 (ChalkAt + 0.05); the camera Punch at 3.6 + 0.8 x 2.8 = 5.84.
# DOORS: seat k's door at 6.4 + 0.9 k + 0.08. MAIBA: syllables 11.75, 12.15, 12.55, 12.95 (YA, with the sting and the
# kid's recorded "Taya!", 0.55 s). TayaCut 14.3. CLOSE 16.2. End 19.6. 100 bpm (0.6 s a beat). C major pentatonic.

CHALK_CUE = 3.65
SKID_IN_CHALK = 2.15                       # the skid inside the chalk file lands on the Punch: 3.65 + 2.15 = 5.80
DOOR_TIMES = [6.4 + 0.9 * k + 0.08 for k in range(4)]
SYLLABLES = [11.75, 12.15, 12.55, 12.95]
YA = SYLLABLES[3]
TAYA_CUT, CLOSE = 14.3, 16.2
BEAT = 0.6
MOTIF = ["C5", "E5", "G5", "C6"]           # do-mi-sol-do: the lullaby opens on it, the doors ring it, the sting completes it


# ------------------------------------------------------------------ the cues (foley in the cue, research.md section 4 rule 6)

def tondo_clap():
    """T-04: the huddle's clap, three small hands together (kids' hands: a little higher than the grown-up takes)."""
    y = np.zeros(int(SR * 0.4))
    for k, (name, at, g) in enumerate([("clap_a", 0.0, 1.0), ("clap_d", 0.006, 0.65), ("clap_b", 0.011, 0.5)]):
        ts.place(y, ts.shift(clap_hand(name, seed=40 + k), 2.0), at, g)
    y = ts.peak_eq(y, 2600, 3.0, 1.2)                         # the snap, in the claps' own lane
    return ts.fade(finish(y, wet=0.07, hp=280)[: int(SR * 0.3)], 0.0005, 0.08)


def tondo_chalk():
    """T-02: a stick of chalk run fast along concrete, the kid's steps pressing it, then a tsinelas skid at 2.15 s."""
    n = int(SR * 2.7)
    t = np.arange(n) / SR
    run = t < SKID_IN_CHALK
    # round 1 ran 66+ grains a second over a loud bed: a wall of noise. The real scrape is separate grains with gaps.
    scrape = ts.grains(P["chalk_scrape"]["bands"], 2.7, P["chalk_scrape"]["grains_per_s"] * 1.15, seed=9,
                       grain_ms=(3, 11), level=(0.15, 1.0), speed=[0.8, 1.0, 1.2, 1.4], bed=0.04)
    steps = 0.65 + 0.35 * np.abs(np.sin(np.pi * t / 0.21))          # each running step presses the chalk harder
    stroke = ts.noise_like(P["chalk_stroke"]["bands"], 2.7, seed=10) * 0.08
    body = (scrape + stroke) * steps * np.clip(t / 0.06, 0, 1) * np.where(run, 1.0, np.exp(-(t - SKID_IN_CHALK) * 30))
    body = ts.band(body, 1100, 9500)
    y = normal(body, 0.6)
    # the skid: the rubber sole planting and scuffing (stick-slip squeak falling in pitch), and the slap
    sq_len = 0.24
    squeak = ts.stick_slip(np.linspace(1150, 640, 12), sq_len / 12, [{"f": 1900, "db": 0, "decay": 400}, {"f": 3100, "db": -6, "decay": 500}],
                           sq_len, seed=11, jitter=0.05)
    squeak *= np.exp(-np.linspace(0, 3.5, len(squeak)))
    scuff = ts.burst(P["scuff"]["bands"], 2.0, 120, seed=12, seconds=0.26)
    slap = ts.burst(P["slap_b"]["bands"], P["slap_b"]["attack_ms"], P["slap_b"]["decay20_ms"], seed=13, seconds=0.2)
    ts.place(y, normal(slap, 0.9), SKID_IN_CHALK)
    ts.place(y, normal(scuff, 0.55), SKID_IN_CHALK + 0.01)
    ts.place(y, normal(squeak, 0.3), SKID_IN_CHALK + 0.02)
    return ts.fade(finish(y, wet=0.15, hp=300), 0.003, 0.12)


def tondo_door():
    """T-03: a light wooden house door: the metal latch, the hinge's stick-slip creak, the bump on its stop at 0.5 s."""
    y = np.zeros(int(SR * 0.9))
    latch = ts.strike([p for p in P["door_latch"]["partials"] if p["f"] > 300], 0.12, hardness=1.0, seed=20, click=0.2, decay_scale=4.0)
    latch_tex = ts.burst(P["door_latch_tex"]["bands"], 0.8, 20, seed=21, seconds=0.08)
    ts.place(y, normal(latch, 0.45), 0.0)
    ts.place(y, normal(latch_tex, 0.5), 0.0)
    cr = P["door_creak"]
    creak = ts.stick_slip(cr["rate"][:17], cr["hop_s"] * (0.36 / (17 * cr["hop_s"])), cr["body"], 0.36, seed=22, jitter=0.3, rate_scale=0.4)
    creak *= np.sin(np.linspace(0, np.pi, len(creak))) ** 0.7     # the swing speeds up and slows to the stop
    ts.place(y, normal(ts.band(creak, 350, 4000), 0.55), 0.1)
    bump = wood("door_bump", 0.3, hardness=0.35, seed=23, lowest=70, decay_scale=8.0, click=0.0)
    bump_tex = ts.burst(P["door_bump_tex"]["bands"], 1.0, 35, seed=24, seconds=0.15)
    ts.place(y, normal(ts.lowpass(bump, 1800), 0.8), 0.5)
    ts.place(y, normal(ts.lowpass(bump_tex, 3000), 0.55), 0.5)
    return ts.fade(finish(y, wet=0.2, hp=110), 0.0005, 0.1)


def tondo_taya():
    """T-05: the count lands. The lata rings with a marimba and log-drum floor, and the toy glock runs the motif an
    octave up to a bright C. A dip at 300 to 3000 Hz keeps the kid's real "Taya!" clear."""
    y = np.zeros(int(SR * 1.7))
    ts.place(y, normal(lata(1.2, pitch=1.0, seed=30), 0.85), 0.0)
    ts.place(y, normal(lata(0.6, pitch=1.5, seed=31, which="lata_finger", decay_scale=2.0), 0.2), 0.004)
    ts.place(y, normal(marimba("C3", 1.4, hardness=0.5, seed=32), 0.55), 0.0)
    ts.place(y, normal(wood("logdrum_lo", 0.6, hardness=0.4, seed=33), 0.5), 0.0)
    for i, n in enumerate(["C6", "E6", "G6", "C7"]):
        ts.place(y, normal(glock(n, 1.4, hardness=0.6, seed=34 + i), 0.45 + 0.1 * i), 0.05 + 0.055 * i)
    ts.place(y, normal(wood("triangle", 1.2, hardness=0.9, seed=39), 0.25), 0.215)
    y = ts.peak_eq(y, 1100, -6.0, 0.45)
    return ts.fade(finish(y, wet=0.2, hp=70), 0.0005, 0.2)


# ------------------------------------------------------------------ T-01 the score: the toy band on the beat table

def tondo_score():
    rng = np.random.default_rng(1023)
    total = 20.5
    tr = np.zeros(int(SR * total))

    def human(at, ms=8):
        return at + rng.uniform(-ms, ms) / 1000

    def harp_chord(names, at, gain, spread=0.035, seconds=3.0, hard=0.6):
        for k, nm in enumerate(names):
            ts.place(tr, normal(pluck(nm, seconds, hardness=hard, seed=int(rng.integers(1e6))), gain), at + k * spread)

    # SAMPAY 0 to 3.6: the music box under the laundry opens on the motif, a rolled harp chord under it.
    tune = MOTIF + ["A5", "G5"] + ["E5", "D5", "C5", "D5", "E5", "G5"]
    for i, n in enumerate(tune):
        at = 0.25 + i * BEAT * 0.5 if i < 6 else 0.25 + 3 * BEAT + (i - 6) * BEAT * 0.5
        ts.place(tr, normal(glock(n, 2.6, hardness=0.45, seed=100 + i), 0.30 + 0.04 * (i == 0)), human(at, 5))
    harp_chord(["C3", "G3", "E4", "C5"], 0.0, 0.22, spread=0.06, seconds=3.6)
    harp_chord(["G2", "D3", "B3"], 2.4, 0.16, spread=0.05)

    # CHALK 3.6 to 6.4: plucks run with the kid in the mid register (the chalk owns 1-9 kHz), marimba under it.
    run = ["C4", "E4", "G4", "A4", "G4", "E4", "D4", "E4", "G4", "C5"]
    for i, n in enumerate(run):
        ts.place(tr, normal(pluck(n, 0.9, hardness=0.8, seed=200 + i), 0.34), human(3.6 + i * BEAT * 0.5))
    for k, n in enumerate(["A2", "A2", "E2", "A2"]):
        ts.place(tr, normal(marimba(n, 1.0, seed=210 + k), 0.4), 3.6 + k * BEAT)
    # the skid's tuned answer on the Punch: a xylophone flick G5 to C6
    ts.place(tr, normal(xylo("G5", 0.5, seed=220), 0.32), CHALK_CUE + SKID_IN_CHALK - 0.04)
    ts.place(tr, normal(xylo("C6", 0.7, seed=221), 0.4), CHALK_CUE + SKID_IN_CHALK)

    # DOORS 6.4 to 10.2: the street groove. Marimba bass on the beat, harp strums off it, claves on 2 and 4, a soft
    # shaker. Each door rings the motif's next note on the glock at its own time; no strum within 0.12 s of a door.
    bass = ["F2", "F2", "C3", "F2", "G2", "G2", "D3", "G2"]
    for k in range(8):
        at = 6.4 + k * BEAT
        ts.place(tr, normal(marimba(bass[k], 1.2, hardness=0.4, seed=300 + k), 0.5), human(at, 4))
        strum = 6.4 + k * BEAT + BEAT * 0.5
        if min(abs(strum - d) for d in DOOR_TIMES) > 0.12:
            harp_chord(["F4", "A4", "C5"] if k < 4 else ["G4", "B4", "D5"], human(strum, 4), 0.14, spread=0.012, seconds=0.8, hard=0.8)
        if k % 2 == 1:
            ts.place(tr, normal(wood("claves", 0.3, seed=310 + k), 0.2), human(at, 4))
        for off in (0.25, 0.75):
            sh = ts.burst(P["shaker"]["bands"], 6.0, 40, seed=320 + k * 2 + int(off * 4), seconds=0.12)
            ts.place(tr, normal(ts.highpass(sh, 4000), 0.07 + 0.03 * (off > 0.5)), human(at + BEAT * off, 6))
    for k, d in enumerate(DOOR_TIMES):
        ts.place(tr, normal(glock(MOTIF[k], 1.8, hardness=0.6, seed=330 + k), 0.3), d + 0.01)

    # GATHER and MAIBA: the band drops to a held harp chord; each clap gets claves and a xylophone step up the scale.
    harp_chord(["A2", "E3", "C4", "E4"], 10.25, 0.18, spread=0.07, seconds=3.0, hard=0.5)
    for k, (at, n) in enumerate(zip(SYLLABLES[:3], ["C5", "D5", "E5"])):
        ts.place(tr, normal(wood("claves", 0.3, seed=400 + k), 0.16), at)
        ts.place(tr, normal(xylo(n, 0.6, hardness=0.55, seed=410 + k), 0.24), at + 0.005)
    # YA: the floor only (the sting is the top, the kid's voice the middle): low marimba C and a log drum.
    ts.place(tr, normal(marimba("C2", 2.0, hardness=0.5, seed=420), 0.55), YA)
    ts.place(tr, normal(wood("logdrum_lo", 0.6, hardness=0.45, seed=421), 0.45), YA)

    # The laugh, after the voice has finished (YA + 0.6): a bouncing xylophone giggle over marimba bounces.
    giggle = ["G5", "E5", "G5", "E5", "A5", "G5", "E5", "C5", "D5", "E5"]
    for i, n in enumerate(giggle):
        ts.place(tr, normal(xylo(n, 0.45, hardness=0.7, seed=500 + i), 0.24), human(YA + 0.6 + i * 0.1, 5))
    for k, n in enumerate(["C3", "G2", "A2", "G2"]):
        ts.place(tr, normal(marimba(n, 0.8, seed=510 + k), 0.38), YA + 0.3 + k * BEAT * 0.5)
    harp_chord(["C4", "E4", "G4"], YA + 0.6, 0.12, spread=0.02, seconds=1.4)

    # TAYA 14.3: the taya squares up: two determined log-drum hits and a low A minor.
    ts.place(tr, normal(wood("logdrum_lo", 0.7, hardness=0.5, seed=600), 0.5), TAYA_CUT)
    ts.place(tr, normal(wood("logdrum_hi", 0.6, hardness=0.5, seed=601), 0.45), TAYA_CUT + 0.75)
    ts.place(tr, normal(marimba("A2", 1.6, seed=602), 0.4), TAYA_CUT)
    harp_chord(["A2", "E3", "C4"], TAYA_CUT, 0.14, spread=0.05, seconds=1.6)

    # CLOSE: the lullaby comes home on the music box, resolving on the motif's C.
    for i, n in enumerate(["E5", "D5", "C5", "G4", "C5"]):
        ts.place(tr, normal(glock(n, 2.8, hardness=0.45, seed=700 + i), 0.28), human(15.6 + i * BEAT, 5))
    harp_chord(["C3", "G3", "E4", "G4", "C5"], 15.4, 0.18, spread=0.08, seconds=5.0, hard=0.5)
    ts.place(tr, normal(marimba("C2", 3.0, hardness=0.3, seed=710), 0.4), 15.4)

    tr = ts.room(ts.highpass(tr, 45), 0.24)
    env = np.ones_like(tr)
    tail = int(SR * 2.0)
    env[-tail:] = np.linspace(1, 0, tail) ** 2
    return ts.glue(normal(tr * env, 0.95), 1.1)


BUILDS = {
    "sfx_film_tondo_score.wav": tondo_score,
    "sfx_film_tondo_chalk.wav": tondo_chalk,
    "sfx_film_tondo_door.wav": tondo_door,
    "sfx_film_tondo_clap.wav": tondo_clap,
    "sfx_film_tondo_taya.wav": tondo_taya,
}


# THE LOUDNESS LADDER: each file's peak (the house default is PEAK, 0.85). Measured in the film's own mix with the
# alley bed (render_map_film.py --bed): at the old 0.85 the taya sting (x 0.8 at its call) and the kid's recorded
# "Taya!" (x 0.75) summed to +1.25 dBFS at YA, and the score met the first door at -0.2 dBFS. The score is the bed,
# the sting must leave the voice its room.
LEVEL = {"sfx_film_tondo_score.wav": 0.70, "sfx_film_tondo_taya.wav": 0.60}


def write(path, y):
    y = np.asarray(y, dtype=np.float64)
    y = y - np.mean(y)
    y = y / (np.abs(y).max() or 1.0) * LEVEL.get(path.name, PEAK)
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(np.round(np.clip(y, -1, 1) * 32767).astype("<i2").tobytes())
    print(f"wrote {path.name}  {len(y) / SR:.2f} s")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--fetch", action="store_true")
    parser.add_argument("--only", default="")
    parser.add_argument("--analyse", action="store_true")
    parser.add_argument("--out", default=str(OUT))
    args = parser.parse_args()
    if args.fetch:
        fetch(load_manifest())
        return
    if args.analyse or not PROFILES.exists():
        analyse()
    P.update(json.loads(PROFILES.read_text(encoding="utf-8"))["profiles"])
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    for name, build in BUILDS.items():
        if args.only and args.only not in name:
            continue
        write(out / name, build())


if __name__ == "__main__":
    main()
