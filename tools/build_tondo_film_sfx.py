"""The Tondo film's score and cues (Eskinita's opening), built from REAL recordings around one shared theme.

  py -3 tools/build_tondo_film_sfx.py              fetch what is missing, then build every Tondo file
  py -3 tools/build_tondo_film_sfx.py --fetch      only download and check the sources
  py -3 tools/build_tondo_film_sfx.py --only clap  build only the files whose name contains "clap"

Owner, 2026-10-23: "this game has horrible sfx rn", "thoroughly think abt how to make sfx cohesive kasi rn at current
state its js noise and a bunch of sounds taht dont have a theme and dont work well together or dont support each
other". docs/reports/sfx-2026-10-23/research.md is the why; this is the how. The earlier recipes (band noise, sine
bells, Karplus-Strong strings) were in tools/synth_map_film_sfx.py and are kept as the "before" files in
docs/reports/sfx-2026-10-23/tondo/before/.

THE THEME (round 2; the rules are written out above tondo_score's section, and the why in tondo/review.md)
  A barangay rondalla, one chord at a time: bandurria tune, guitar chords, guitar bass, all built from
  exact harmonics (tools/tump_synth.py string_note), every note damped at the next chord change, at most three parts.
  No bells, no bamboo, no balde: every sound has a reason on screen or a job in the music (round 3). Round 4: no
  foley at all (the run, the doors and the count are marked by the score); the strings are physical (Karplus-Strong).
  The analyse step still measures the CC0 references into tools/tump_sfx_profiles.json for later foley.
  Round 3: the story (tondo/spotting.md) in A major at 150 bpm, ending on E into the match soundtrack's A.
  Round 1 (a toy band: glockenspiel, xylophone, marimba, harp) was rejected: "WHY IS THERE A BELL", "0 harmony".

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


# ------------------------------------------------------------------ round 2: a rondalla in one key, less is more
#
# Owner on round 1: "ang saket sa tenga theres 0 harmony", "not js the bell ang gulo talaga", "completely revamp
# everything", "make ur own", "thoroughly think abt how to make everything harmonious and compliment each other
# sometimes less is more". So, round 2's rules (they replace round 1's toy band):
#   1. No bells of any kind: no glockenspiel, triangle, xylophone or marimba. The band is a barangay rondalla: a
#      bandurria for the tune, a guitar for the chords, the guitar's low strings for the bass. Nothing else plays
#      music. (Round 2's bamboo and balde were cut in round 3: "may random shit na fucks it up".)
#   2. In tune by construction: every string partial is an exact harmonic of an A440 equal-tempered note
#      (tump_synth.string_note).
#   3. One chord at a time: I, IV, V7 and vi in A (the shape of most Filipino folk songs). Every note is damped at the
#      next chord change, so no two chords ever ring together.
#   4. Each part one job, at most three at once: chords (guitar), root (bass), tune (bandurria). The tune rests while a
#      foley moment speaks (the chalk run, the four claps, the kid's "Taya!").
#   5. The foley has no pitched parts that could clash (round 1's creak and skid squeak were pitched sweeps).
#   6. Every sound has a reason ON SCREEN or a job in the music, or it is cut (round 3). The foley plays what the
#      picture shows (the chalk run is a kid's running feet, not two seconds of chalk).

P = {}          # the measured profiles (tools/tump_sfx_profiles.json), loaded in main()


def normal(y, peak=1.0):
    return y / (np.abs(y).max() + 1e-12) * peak


def finish(y, wet=0.12, hp=120.0, drive=1.1):
    """Every file: the house high-pass, THE room (lighter than round 1), the glue."""
    y = ts.highpass(y, hp)
    y = ts.room(y, wet)
    return ts.glue(normal(y, 0.95), drive)


# ------------------------------------------------------------------ the film's times (TondoFilm.cs constants)
#
# CHALK 3.6: the chalk cue fires at 3.65 (ChalkAt + 0.05); the camera Punch at 3.6 + 0.8 x 2.8 = 5.84.
# DOORS: seat k's door at 6.4 + 0.9 k + 0.08. MAIBA: syllables 11.75, 12.15, 12.55, 12.95 (YA, with the sting and the
# kid's recorded "Taya!", 0.55 s). TayaCut 14.3. CLOSE 16.2. End 19.6.

SKID = 5.80                                # the chalk kid's skid (3.6 + 0.97 x 0.8 x 2.8 = 5.77), with the camera Punch
DOOR_TIMES = [6.4 + 0.9 * k + 0.08 for k in range(4)]
SYLLABLES = [11.75, 12.15, 12.55, 12.95]
YA = SYLLABLES[3]
TAYA_CUT = 14.3

# ------------------------------------------------------------------ the story (tondo/spotting.md), in A major at 150 bpm
#
# Round 3 (owner: "think of how a story has a start climax and end, dapat may ganun din music", "transition it to the
# actual game"): the match soundtrack that cuts in on the countdown's first tick (Hud.ShowCountdownTick) is in A major
# at about 150 bpm, a beat every 0.40 s. The film is now in A on that pulse (its 0, 3.6 and 6.4 s cuts fall on it, and
# MAI BA TA YA is 0.40 s apart already), and it ENDS ON E, the chord that pulls to A, so the countdown is the last of
# the suspense and the match music arriving in A is the answer.

Q = 0.4                                   # one beat at 150 bpm
HANDOFF = 18.2                            # MapFilm.Sound fades the score from Handoff (End 19.6 - ReturnSeconds 1.4)

CHORDS = [(0.0, "A"), (2.4, "E"), (3.6, "A"), (4.8, "D"), (SKID, "E"), (6.15, None), (6.4, "A"), (7.6, "F#m"),
          (8.8, "D"), (9.6, "E"), (YA, "A"), (13.45, "D"), (13.85, "A"), (TAYA_CUT, "F#m"), (15.05, "E"),
          (16.8, "E7"), (20.5, None)]
VOICING = {"A": ["A3", "C#4", "E4"], "D": ["A3", "D4", "F#4"], "E": ["G#3", "B3", "E4"], "E7": ["G#3", "B3", "D4"],
           "F#m": ["A3", "C#4", "F#4"]}
ROOT = {"A": "A2", "D": "D2", "E": "E2", "E7": "E2", "F#m": "F#2"}


def chord_end(at):
    """When the chord sounding at `at` gives way to the next: every note of it is damped there."""
    for t, _ in CHORDS:
        if t > at + 1e-6:
            return t
    return 20.5


# ------------------------------------------------------------------ the film's one cue of its own
#
# Round 4: the chalk run, the doors and the count have no foley (owner: "why even give his running some sfx",
# "door opening sounds sucks", "MAIBA TAYA SOUND SUCKS"); the score marks the doors and the syllables.
# sfx_film_tondo_chalk, _door and _clap are deleted, and TondoFilm.cs no longer calls them.

def tondo_taya():
    """T-05: the climax, YA. Round 4 (owner: "MAIBA TAYA SOUND SUCKS"): one decisive hit, the whole band at once on
    A: a hard full down-strum, the bandurria's double course on A and C# picked once, the bass on A; then it rings
    and gets out of the way of the kid's real "Taya!". No tremolo answer."""
    n = int(SR * 1.6)
    gtr, band_, low = np.zeros(n), np.zeros(n), np.zeros(n)
    ts.strum(gtr, ["A2", "E3", "A3", "C#4", "E4"], 0.0, 1.3, "guitar", velocity=1.0, spread=0.007, seed=31, root="A0")
    for nm in ("A5", "C#6"):
        ts.place(band_, ts.string_note(ts.hz(nm) * 2 ** (ts.JUST_CENTS[(ts.midi(nm) - ts.midi("A0")) % 12] / 1200), 1.3,
                                       "bandurria", 0.9, seed=33 + len(nm), stop=1.2, release=0.15), 0.004)
    ts.place(low, ts.string_note(ts.hz("A2"), 1.4, "bass", 1.0, seed=35, stop=1.3, release=0.2), 0.0)
    y = (0.55 * normal(ts.body(gtr, "guitar")) + 0.35 * normal(ts.body(band_, "bandurria")) +
         0.45 * normal(ts.body(low, "bass")))
    env = np.ones(n)
    env[int(0.12 * SR):] = np.exp(-(np.arange(n - int(0.12 * SR)) / SR) * 2.2)   # duck under the voice after the hit
    return ts.fade(finish(y * env, wet=0.12, hp=40), 0.0005, 0.2)


# ------------------------------------------------------------------ T-01 the score: the story on the beat table

def tondo_score():
    total = 20.5
    n = int(SR * total)
    gtr, bass, band_ = np.zeros(n), np.zeros(n), np.zeros(n)
    seeds = iter(range(1000, 100000, 7))

    def g_strum(at, name, vel=0.5, down=True, end=None, notes=None):
        ts.strum(gtr, notes or VOICING[name], at, end or chord_end(at), "guitar", velocity=vel, down=down, seed=next(seeds),
                 root=ROOT[name][:-1] + "0")

    def g_note(at, note, vel=0.45, end=None):
        end = end or chord_end(at)
        ts.place(gtr, ts.string_note(ts.hz(note), end - at + 0.05, "guitar", vel, seed=next(seeds), stop=end - at), at)

    def b_note(at, note, vel=0.7, end=None):
        end = end or chord_end(at)
        ts.place(bass, ts.string_note(ts.hz(note), end - at + 0.05, "bass", vel, seed=next(seeds), stop=end - at), at)

    def tune(at, note, dur, vel=0.5):
        ts.place(band_, ts.string_note(ts.hz(note), dur + 0.05, "bandurria", vel, seed=next(seeds), stop=dur, release=0.02), at)

    def trem(at, notes, dur, vel=0.45, swell=True):
        ts.tremolo(band_, notes, at, at + dur, "bandurria", velocity=vel, seed=next(seeds), swell=swell)

    # START 0 to 3.6, the place: the guitar alone, picking A in half time; a soft bass A as TONDO lands (0.9);
    # to E at 2.4: a question (who comes?).
    for i, nm in enumerate(["A2", "E3", "A3", "C#4", "E4", "C#4"]):
        g_note(0.05 + Q * i, nm, 0.4 + 0.05 * (i == 0))
    b_note(0.9, "A2", 0.45)
    for i, nm in enumerate(["E3", "B3", "E4"]):            # light and open, no low third (E2 under G#3 beat, round 3)
        g_note(2.4 + Q * i, nm, 0.4)

    # THE CHALK RUN 3.6 to 6.4, the first kid: the groove starts, light (his feet are the star): bass on the beat,
    # the guitar off it, A then D. The skid is a stop: one E strum, then a breath of silence before the doors.
    for at, nm in ((3.6, "A2"), (4.0, "E2"), (4.4, "A2"), (4.8, "D2"), (5.2, "A2")):
        b_note(at, nm, 0.62, end=min(at + Q, chord_end(at)))
    for at in (3.8, 4.2, 4.6):
        g_strum(at, "A", 0.36, down=False, end=at + 0.3)
    for at in (5.0, 5.4):
        g_strum(at, "D", 0.36, down=False, end=at + 0.3)
    g_strum(SKID, "E", 0.62)
    b_note(SKID, "E2", 0.7)

    # RISING 6.4 to 10.2, the players arrive. Round 4 (owner: "door opening sounds sucks"): no door foley; each
    # player's entrance is the moment a new part of the band comes in, exactly on that door. Door 1 the bass; door 2
    # the guitar; door 3 the bandurria; door 4 everything. A, F#m, D, E.
    d1, d2, d3, d4 = DOOR_TIMES
    b_note(d1, "A2", 0.75, end=7.2)
    for at, nm in ((7.2, "E2"), (7.6, "F#2"), (8.4, "C#3"), (8.8, "D2"), (9.6, "E2"), (10.0, "B2")):
        b_note(at, nm, 0.62, end=min(at + 0.8, chord_end(at)))
    b_note(d4, "D2", 0.72, end=9.6)
    g_strum(d2, "A", 0.55, end=7.6)
    for at, nm in ((7.8, "F#m"), (8.6, "F#m"), (9.6, "E"), (9.8, "E")):
        g_strum(at, nm, 0.32, down=False, end=min(at + 0.35, chord_end(at)))
    g_strum(d3, "F#m", 0.5, end=8.8)
    g_strum(d4, "D", 0.62, end=9.6, notes=["D3", "A3", "D4", "F#4"])
    trem(d3, ["C#5"], 8.8 - d3 - 0.04, 0.4)
    trem(d4, ["F#5"], 9.6 - d4 - 0.04, 0.44)
    tune(9.6, "E5", 0.36, 0.45)
    tune(10.0, "G#5", 0.36, 0.45)

    # THE HUDDLE 10.2 to 11.3: the groove eases off to one soft E; the bass settles on E.
    g_strum(10.4, "E", 0.3, end=11.6)
    b_note(10.4, "E2", 0.5, end=11.75)

    # CLIMAX BUILD, the count. Round 4 (owner: "MAIBA TAYA SOUND SUCKS"; the picture is a fist POINTING at each kid,
    # nobody claps): the band counts. A held breath (a soft tremolo on E), then one plucked bandurria note on each
    # syllable climbing E, F#, G# with the bass doubling it two octaves down, each a little stronger; YA lands on A.
    trem(11.3, ["E5"], 0.42, 0.2)
    for at, top, low, v in ((SYLLABLES[0], "E5", "E3", 0.5), (SYLLABLES[1], "F#5", "F#3", 0.58), (SYLLABLES[2], "G#5", "G#3", 0.66)):
        tune(at, top, 0.36, v)
        b_note(at, low.replace("3", "2"), v + 0.05, end=at + 0.38)

    # CLIMAX, YA: the bass lands on A under the sting (the sting has the strum and the answer; the kid the middle).
    b_note(YA, "A2", 0.6, end=TAYA_CUT)
    # comic relief: two light up-strums while they laugh.
    g_strum(13.45, "D", 0.28, down=False); g_strum(13.85, "A", 0.28, down=False)

    # THE TURN 14.3: the taya squares up: two firm strums, F#m then E.
    b_note(TAYA_CUT, "F#2", 0.85); g_strum(TAYA_CUT, "F#m", 0.62)
    b_note(15.05, "E2", 0.7, end=15.6); g_strum(15.05, "E", 0.62, end=15.6)

    # THE END 15.6 on, into the game: E, leaning forward. The guitar on every beat, growing; the bass on the half
    # beat, then every beat; a bandurria tremolo on B rising to E. It stops on E7 and rings as the score fades: the
    # countdown, then the match music in A, answer it.
    beats = np.arange(15.6, 17.6, Q)
    for i, at in enumerate(beats):
        g_strum(at, "E" if at < 16.8 else "E7", 0.3 + 0.3 * i / len(beats), down=(i % 2 == 0), end=at + 0.32)
        if at < 16.8 and i % 2 == 0:
            b_note(at, "E2", 0.55, end=at + 0.7)
        elif at >= 16.8:
            b_note(at, "E2", 0.62, end=at + 0.36)
    trem(15.62, ["B4"], 1.15, 0.36)
    trem(16.8, ["E5"], 0.8, 0.42)
    g_strum(17.6, "E7", 0.75, end=20.3, notes=["E3", "G#3", "B3", "D4", "E4"])
    b_note(17.6, "E2", 0.8, end=20.3)
    trem(17.6, ["B4", "E5"], 1.2, 0.4)

    mix = (0.5 * normal(ts.body(gtr, "guitar")) + 0.55 * normal(ts.body(bass, "bass")) +
           0.42 * normal(ts.body(band_, "bandurria")))
    mix = ts.room(ts.highpass(mix, 40), 0.12)
    mix = ts.compress(mix, -20.0, 3.0)                     # round 4: physical strings are spiky (see tump_synth.compress)
    env = np.ones_like(mix)
    tail = int(SR * 1.2)
    env[-tail:] = np.linspace(1, 0, tail) ** 2
    return ts.glue(normal(mix * env, 0.95), 1.05)


BUILDS = {
    "sfx_film_tondo_score.wav": tondo_score,
    "sfx_film_tondo_taya.wav": tondo_taya,
}


# THE LOUDNESS LADDER: each file's peak (the house default is PEAK, 0.85). Measured in the film's own mix with the
# alley bed (render_map_film.py --bed): at the old 0.85 the taya sting (x 0.8 at its call) and the kid's recorded
# "Taya!" (x 0.75) summed to +1.25 dBFS at YA, and the score met the first door at -0.2 dBFS. The score is the bed,
# the sting must leave the voice its room. Round 2 (a full guitar strum and a balde in the sting) met the voice at
# +0.5 dBFS at 0.6, and true peak (between samples) still read +0.4 dBTP at 0.50: the target is -1 dBTP, so the
# score is 0.60, the sting 0.45, the clap 0.75, and the score's bass under YA softer.
LEVEL = {"sfx_film_tondo_score.wav": 0.65, "sfx_film_tondo_taya.wav": 0.45}   # round 4: -18 LUFS with the bed once compressed


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
