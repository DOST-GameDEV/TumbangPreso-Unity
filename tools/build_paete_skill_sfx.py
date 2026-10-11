"""Paete's skill sounds, cut from real recordings of wood, rope and leaves. DRAFTS, for the owner's ear.

  py -3 tools/build_paete_skill_sfx.py             (fetch what is missing, then build every draft)
  py -3 tools/build_paete_skill_sfx.py --fetch     (only download and check the sources)
  py -3 tools/build_paete_skill_sfx.py --analyse   (numbers of every SOURCE, written into the json)
  py -3 tools/build_paete_skill_sfx.py --only fire (any substring of the draft names)

Paete is a living wooden figure whose arms are braided vines. His sounds are living wood: creaks,
taut rope, leaves, hollow knocks. The owner threw out a purely synthesised set and liked the Arena
crowd that was cut from real recordings (tools/build_arena_crowd_from_recordings.py), so this
follows that script's method: Creative Commons 0 recordings from Freesound's preview CDN, each
sound's own page read first and used only if it states CC0, one request at a time, every file's
id, page, URL, author, licence and SHA-256 kept in tools/paete_sfx_sources.json.

WHAT IS WHERE.
  tools/paete_sfx_sources.json     the sources (ten kinds, the `use` of each) and what was measured
  ArtSource/paete/sfx-sources/     the downloads, as fetched (Freesound's high-quality MP3 previews)
  Logs/paete-sfx-drafts/           the drafts, README.txt and report.txt. NOTHING goes into Assets/.

THE DRAFTS: only his first ability, LIANA LEAP (he coils, fires vines from both arms, they catch,
he is reeled in and lands). Four cues, each in three variants a, b, c, and one preview of the whole
move per variant letter:
  liana_coil_*   about 0.25 s  a quick wooden creak that tightens upward (the 0.12 s wind-up tell)
  liana_fire_*   about 0.35 s  the vines whip out: a crack at the front, a swish, a little wood
  liana_catch_*  about 0.30 s  a rope snapping taut: a thwack, then a short creaky strain
  liana_land_*   about 0.50 s  a soft heavy thump with a leafy rustle settling after it
  liana_sequence_*             coil 0.00, fire 0.12, catch 0.26, a rising rush of air 0.26 to 0.60
                               made from the swish material, land 0.62

A SECOND SET, d, e, f (CUES2, SEQUENCE2), was made after the owner heard a, b, c over the animation
and said "a mix between B and c.. idk it sounds quite unnatural": one direction in three shades, with
the processing that a recording never has taken out. The list is above CUES2. Its previews use the
game's own cue times: coil 0.00, fire 0.14, catch 0.28, land 0.70. a, b, c are still written, unchanged.

NOBODY HAS HEARD d, e, f EITHER. The cuts are chosen and checked by measurement only (where the energy
is in time, spectral centroid, peak, loudness, the ends). The recipes are the CUES table below:
change a number there and rerun. Deterministic: no randomness at all in the drafts.
numpy, scipy, soundfile (libsndfile 1.1 or later, which decodes MP3).
"""
import argparse
import hashlib
import html
import json
import re
import time
import urllib.request
import wave
from pathlib import Path

import numpy as np
from scipy import signal

SR = 44100
PEAK = 0.85
ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "tools/paete_sfx_sources.json"
CACHE = ROOT / "ArtSource/paete/sfx-sources"
OUT = ROOT / "Logs/paete-sfx-drafts"
AGENT = "TumbangPresoPaeteSfx/1.0 (indie game, CC0 foley recordings, one request at a time; thegrinchisthebest@gmail.com)"
CC0_MARK = "creativecommons.org/publicdomain/zero/1.0"
OTHER_LICENCES = ("creativecommons.org/licenses/",)          # Attribution, NonCommercial, Sampling+: never used
TODAY = "2026-10-07"
HOT = 1.35                                                    # a draft is never bent from higher than this


# ------------------------------------------------------------------ the manifest and the downloads

def load_manifest():
    return json.loads(MANIFEST.read_text(encoding="utf-8"))


def save_manifest(m):
    MANIFEST.write_text(json.dumps(m, indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")


def get(url):
    req = urllib.request.Request(url, headers={"User-Agent": AGENT})
    with urllib.request.urlopen(req, timeout=120) as f:
        return f.read()


def fetch(m, pause=1.5):
    """Download what is missing. The sound's own page is read first and must state CC0 and no other
    Creative Commons licence; the file's URL, author and description are taken from that page."""
    import soundfile as sf
    CACHE.mkdir(parents=True, exist_ok=True)
    for s in m["sources"]:
        path = CACHE / s["file"]
        if not path.exists():
            if s.get("skipped", "").startswith("its page"):
                continue
            try:
                page = get(s["page"]).decode("utf-8", "replace")
                time.sleep(pause)
                if CC0_MARK not in page or any(o in page for o in OTHER_LICENCES):
                    s["licence_confirmed"] = False
                    s["skipped"] = "its page does not state Creative Commons 0 (and only that)"
                    print("SKIPPED %2d %s: page does not state CC0" % (s["n"], s["id"]))
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
                s["fetched"] = TODAY
                data = get(s["url"])
                time.sleep(pause)
                path.write_bytes(data)
                s.pop("skipped", None)
            except Exception as e:                          # a failed URL is skipped and reported, never substituted
                s["skipped"] = "download failed: %s" % e
                print("SKIPPED %2d %s: %s" % (s["n"], s["id"], e))
                save_manifest(m)
                continue
            save_manifest(m)
        data = path.read_bytes()
        sha = hashlib.sha256(data).hexdigest()
        if s.get("sha256") and s["sha256"] != sha:
            print("WARNING %2d %s: SHA-256 differs from the manifest (the cuts may not land where they did)" % (s["n"], s["file"]))
        s["sha256"], s["bytes"] = sha, len(data)
        info = sf.info(str(path))
        s["seconds"] = round(info.frames / info.samplerate, 3)
        s["format"] = "%s, %d Hz, %d ch" % (info.format, info.samplerate, info.channels)
        print("%2d %-44s %8d bytes %6.2f s  %s" % (s["n"], s["file"], len(data), s["seconds"], s["format"]))
    save_manifest(m)


_decoded = {}


def by_id(m, ident):
    return next(x for x in m["sources"] if x["id"] == str(ident))


def source(m, ident):
    """A source by its Freesound id, mono at 44.1 kHz, float64. Stereo is averaged."""
    ident = str(ident)
    if ident in _decoded:
        return _decoded[ident]
    import soundfile as sf
    s = by_id(m, ident)
    path = CACHE / s["file"]
    if not path.exists():
        raise FileNotFoundError("source %s (%s) is not in %s: run --fetch" % (ident, s["file"], CACHE))
    x, rate = sf.read(str(path), dtype="float64", always_2d=True)
    x = x.mean(axis=1)
    if rate != SR:
        g = np.gcd(SR, rate)
        x = signal.resample_poly(x, SR // g, rate // g)
    _decoded[ident] = x
    return x


def span(m, ident, start, end):
    """Seconds `start`..`end` of a source. Several of these clips begin on their event (a knock at
    0.00 s), so a span may start at 0: libsndfile's decoder removes the MP3 encoder's delay, and
    every layer is faded in anyway."""
    x = source(m, ident)
    total = len(x) / SR
    if start < 0 or end > total + 1e-9 or end <= start:
        raise ValueError("source %s: %.3f..%.3f is outside 0..%.3f" % (ident, start, end, total))
    return x[int(round(start * SR)):int(round(end * SR))].copy()


# ------------------------------------------------------------------ small tools

def sos(x, kind, cut, order=2):
    return signal.sosfilt(signal.butter(order, cut, btype=kind, fs=SR, output="sos"), x)


def fade(x, come=0.003, go=0.03):
    """Half-cosine in over `come` seconds and out over `go`."""
    x = x.copy()
    a, r = min(len(x) // 2, int(SR * come)), min(len(x) // 2, int(SR * go))
    if a > 0:
        x[:a] *= 0.5 - 0.5 * np.cos(np.pi * np.arange(a) / a)
    if r > 0:
        x[len(x) - r:] *= 0.5 + 0.5 * np.cos(np.pi * np.arange(r) / r)
    return x


def repitch(x, ratio):
    """Faster and higher by `ratio` (a tape's speed), by band-limited resampling."""
    if abs(ratio - 1.0) < 1e-6:
        return x
    return signal.resample(x, max(8, int(round(len(x) / ratio))))


def glide(x, r0, r1):
    """A tape whose speed moves from `r0` to `r1` over the sound (geometrically): the pitch of
    everything in it slides by that much. This is how a creak is made to TIGHTEN upward."""
    n_in = len(x)
    # Output sample k reads the input at the running sum of the speed.
    est = int(n_in / ((r0 + r1) / 2.0) * 1.5) + 8
    speed = r0 * (r1 / r0) ** (np.arange(est) / max(1.0, n_in / np.sqrt(r0 * r1)))
    pos = np.concatenate([[0.0], np.cumsum(speed)[:-1]])
    pos = pos[pos < n_in - 1]
    # Band-limit before reading faster than 1.
    top = max(r0, r1)
    if top > 1.0:
        x = sos(x, "low", min(20000.0, 0.45 * SR / top), 6)
    return np.interp(pos, np.arange(n_in), x)


def envelope(x, ms=5.0):
    """The level of `x`, smoothed over `ms`."""
    k = max(1, int(SR * ms / 1000.0))
    return np.sqrt(np.convolve(x ** 2, np.ones(k) / k, mode="same"))


def onset(x, share=0.15):
    """The first sample where the 2 ms level reaches `share` of its greatest."""
    e = envelope(x, 2.0)
    return int(np.argmax(e >= e.max() * share))


def loud(x, seconds=0.1):
    """The RMS of the loudest `seconds` of it: what a short one-shot is levelled by."""
    w = max(1, min(len(x), int(SR * seconds)))
    c = np.concatenate([[0.0], np.cumsum(x ** 2)])
    return float(np.sqrt(((c[w:] - c[:-w]) / w).max()))


def soft(x):
    """Nothing over PEAK leaves here: a tall sample is bent from 0.6 up, never cut."""
    knee = 0.6
    over = np.abs(x) > knee
    x = x.copy()
    x[over] = np.sign(x[over]) * (knee + (PEAK - knee) * np.tanh((np.abs(x[over]) - knee) / (PEAK - knee)))
    return x


def centroid(x):
    if len(x) < 64:
        return 0.0
    mag = np.abs(np.fft.rfft(x * np.hanning(len(x))))
    f = np.fft.rfftfreq(len(x), 1.0 / SR)
    return float((f * mag).sum() / (mag.sum() + 1e-12))


def write(path, x):
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(np.round(x * 32767).astype(np.int16).tobytes())


NOTES = {
    "588244": "its page says it is SYNTHESISED, not a recording: kept for reference, not used by any draft",
    "615761": "its page says it is Freesound 529925 with an echo added: the dry 529925 is used instead",
    "529925": "the crack is followed by a second, hissier event from 0.20 s: only the first is cut",
    "675833": "old film library transfer; the crack rings for 0.4 s, so it is cut with a fast decay",
}

# ------------------------------------------------------------------ listening without ears: a source

def events(x, least=0.12, most=8):
    """Where the loud events of a recording begin (seconds) and how tall each is (dB under the
    tallest), from the 5 ms level of everything above 150 Hz."""
    e = envelope(sos(x, "high", 150.0), 5.0)
    top = e.max() + 1e-12
    peaks, _ = signal.find_peaks(e, height=top * 0.2, distance=int(SR * least))
    peaks = sorted(peaks, key=lambda p: -e[p])[:most]
    out = []
    for p in sorted(peaks):
        a = max(0, p - int(SR * 0.08))
        under = np.nonzero(e[a:p + 1] < e[p] * 0.15)[0]
        start = a + (int(under[-1]) if len(under) else 0)
        out.append((round(start / SR, 3), round(20 * np.log10(e[p] / top), 1)))
    return out


def measure_source(x):
    """Length, the loud events, how fast the tallest dies, how noisy the quiet parts are, and how
    bright it is: enough to choose a cut without hearing it."""
    e = envelope(x, 10.0)
    top = e.max() + 1e-12
    p = int(np.argmax(e))
    after = e[p:]
    def fall(db):
        under = np.nonzero(after < top * 10 ** (-db / 20.0))[0]
        return round(float(under[0]) / SR, 3) if len(under) else None
    floor = 20 * np.log10(np.percentile(e, 10) / top + 1e-9)
    tail = 20 * np.log10(e[int(len(e) * 0.85):].mean() / top + 1e-9)
    w = x[max(0, p - int(SR * 0.05)):p + int(SR * 0.15)]
    return {"seconds": round(len(x) / SR, 3), "peak": round(float(np.abs(x).max()), 3),
            "loudest_at": round(p / SR, 3), "events": events(x),
            "falls_20db_in": fall(20), "falls_40db_in": fall(40),
            "floor_db": round(float(floor), 1), "tail_db": round(float(tail), 1),
            "centroid_hz": int(centroid(w)), "centroid_all_hz": int(centroid(x))}


def sounds_line(s, r):
    ev = ", ".join("%.2f" % t for t, _ in r["events"][:6])
    quick = "dies 20 dB in %.2f s" % r["falls_20db_in"] if r["falls_20db_in"] is not None else "never falls 20 dB (sustained)"
    noise = "quiet between events" if r["floor_db"] < -40 else "some bed between events" if r["floor_db"] < -25 else "continuous sound, no silence"
    return "%.2f s; loud events at %s s (loudest %.2f); %s; floor %.0f dB, last 15%% at %.0f dB under the peak (%s); centroid about %d Hz at the loudest" % (
        r["seconds"], ev or "none found", r["loudest_at"], quick, r["floor_db"], r["tail_db"], noise, r["centroid_hz"])


def analyse(m):
    for s in m["sources"]:
        if s.get("skipped"):
            continue
        r = measure_source(source(m, s["id"]))
        s["description"] = re.sub(r"\s+", " ", re.sub(r"<[^>]+>", " ", s.get("description", ""))).strip()
        if s["id"] in NOTES:
            s["note"] = NOTES[s["id"]]
        s["measured"] = r
        s["sounds"] = sounds_line(s, r)
        print("%2d %-18s %-8s %s" % (s["n"], s["use"], s["id"], s["sounds"]))
    save_manifest(m)


# ------------------------------------------------------------------ the recipes

# A layer: `src` (Freesound id), `a`..`b` (seconds of it), `at` (where it is laid in the cue),
# `gain`, `pitch` (tape speed: over 1 is higher, smaller, quicker; under 1 is lower and bigger),
# `glide` (r0, r1: a tape speed that MOVES, so the pitch slides), `hp` / `lp` (Hz; every layer is
# high-passed at 70 and low-passed at 11 kHz unless it says otherwise: no rumble, no harsh top),
# `fin` / `fout` (fades, s), `decay` (an exponential fall with this time constant, s, from the
# layer's start: cuts a ringing tail), `flat` (even out the layer's own level first: divide by its
# 30 ms level, floored at this share of its greatest), `swell` (a rise to the end, this exponent),
# `keep_lead` (do not trim the layer to its own onset). Each layer is levelled by its loudest 50 ms before
# `gain`, so gains are comparable between sources recorded at very different levels.
CUES = {
    "liana_coil": {
        "seconds": 0.25, "level": 0.24, "tail": 0.035,
        "what": "the wind-up: a quick wooden creak that tightens upward in pitch",
        "variants": {
            "a": {"why": "a clean door-hinge creak sliding up an octave, over a low woody body: the plain reading",
                  "layers": [
                      {"src": 390123, "a": 0.24, "b": 0.56, "glide": (0.85, 1.7), "gain": 1.0, "flat": 0.3, "swell": 0.6},
                      {"src": 456814, "a": 1.44, "b": 1.76, "glide": (0.9, 1.5), "gain": 0.55, "flat": 0.3, "swell": 0.6, "lp": 5000}]},
            "b": {"why": "HEAVIER: a real tree's creak and a floorboard, both pitched down then sliding up: a big trunk bending",
                  "layers": [
                      {"src": 797994, "a": 0.72, "b": 1.08, "glide": (0.5, 1.4), "gain": 1.0, "flat": 0.3, "swell": 0.6},
                      {"src": 802594, "a": 0.40, "b": 0.72, "glide": (0.6, 1.4), "gain": 0.6, "flat": 0.3, "swell": 0.6}]},
            "c": {"why": "TIGHTER and ropier: a thin door squeak sliding up an octave with a rope's leathery creak under it, swelling to the end",
                  "layers": [
                      {"src": 588509, "a": 0.06, "b": 0.42, "glide": (1.0, 2.0), "gain": 1.0, "flat": 0.3, "swell": 1.2, "lp": 9000},
                      {"src": 664929, "a": 2.26, "b": 2.50, "glide": (0.9, 1.5), "gain": 0.7, "flat": 0.3, "swell": 1.0}]},
        }},
    "liana_fire": {
        "seconds": 0.35, "level": 0.24, "tail": 0.08,
        "what": "the vines whip out: a crack at the front, a fast swish, a little wood",
        "variants": {
            "a": {"why": "a real whip crack, then a long bamboo stick's swish, with a slapstick's wooden clack pitched down under the crack",
                  "layers": [
                      {"src": 529925, "a": 0.060, "b": 0.19, "gain": 1.0, "fout": 0.03, "lp": 9000},
                      {"src": 855844, "a": 0.07, "b": 0.42, "at": 0.012, "pitch": 0.8, "gain": 0.6, "keep_lead": True, "fin": 0.02, "fout": 0.15},
                      {"src": 278974, "a": 0.0, "b": 0.12, "pitch": 0.7, "gain": 0.35, "decay": 0.03}]},
            "b": {"why": "HEAVIER: an old film library whip crack pitched down, a thin stick's lower swish, and a wood block's knock in the front",
                  "layers": [
                      {"src": 675833, "a": 0.03, "b": 0.30, "pitch": 0.8, "gain": 1.0, "decay": 0.05, "lp": 8000},
                      {"src": 352719, "a": 0.08, "b": 0.40, "at": 0.012, "pitch": 0.8, "gain": 0.7, "hp": 180, "keep_lead": True, "fin": 0.02, "fout": 0.12},
                      {"src": 218460, "a": 0.0, "b": 0.14, "gain": 0.4, "decay": 0.04}]},
            "c": {"why": "SHARPER: the slapstick itself is the crack, with a hollow bamboo swish and a rope's whoosh after it",
                  "layers": [
                      {"src": 278974, "a": 0.0, "b": 0.14, "pitch": 0.85, "gain": 0.5, "decay": 0.035, "lp": 10000},
                      {"src": 850168, "a": 0.02, "b": 0.30, "at": 0.008, "pitch": 0.75, "gain": 0.9, "keep_lead": True, "fin": 0.01, "fout": 0.1},
                      {"src": 473583, "a": 0.07, "b": 0.40, "at": 0.03, "pitch": 0.85, "gain": 0.7, "keep_lead": True, "fin": 0.02, "fout": 0.12}]},
        }},
    "liana_catch": {
        "seconds": 0.30, "level": 0.24, "tail": 0.07,
        "what": "the vines catch: a rope snapping taut (a thwack), then a short creaky strain",
        "variants": {
            "a": {"why": "a rope snatched tight with a wood hit under it, then a leathery rope creak that tightens",
                  "layers": [
                      {"src": 802697, "a": 1.33, "b": 1.52, "gain": 1.0, "decay": 0.045},
                      {"src": 547414, "a": 0.12, "b": 0.26, "gain": 0.5, "decay": 0.04},
                      {"src": 664929, "a": 2.26, "b": 2.56, "at": 0.07, "glide": (0.8, 1.25), "gain": 0.42, "fin": 0.01, "fout": 0.09}]},
            "b": {"why": "HEAVIER: the rope snatch pitched down over a dull thud on earth, then a tree's own creak as the strain",
                  "layers": [
                      {"src": 802697, "a": 2.56, "b": 2.78, "pitch": 0.75, "gain": 1.0, "decay": 0.06},
                      {"src": 536766, "a": 0.10, "b": 0.30, "gain": 0.6, "lp": 1800, "decay": 0.06},
                      {"src": 797994, "a": 0.72, "b": 1.00, "at": 0.08, "glide": (0.8, 1.1), "gain": 0.42, "fin": 0.01, "fout": 0.09, "lp": 7000}]},
            "c": {"why": "SHARPER: the rope snatch pitched up with a bamboo stick's hit and a slapstick's clack in the snap, then a thin squeak and stretched leather's dry ticking sliding up",
                  "layers": [
                      {"src": 802697, "a": 1.33, "b": 1.52, "pitch": 1.3, "gain": 1.0, "decay": 0.035},
                      {"src": 386888, "a": 0.11, "b": 0.22, "gain": 0.4, "decay": 0.025, "lp": 9000},
                      {"src": 278974, "a": 0.0, "b": 0.12, "pitch": 0.85, "gain": 0.25, "decay": 0.03, "lp": 10000},
                      {"src": 588509, "a": 0.40, "b": 0.70, "at": 0.06, "glide": (1.2, 1.9), "gain": 0.38, "flat": 0.3, "fin": 0.008, "fout": 0.09, "lp": 9000},
                      {"src": 559079, "a": 14.56, "b": 14.92, "at": 0.06, "glide": (1.0, 1.6), "gain": 0.18, "fin": 0.008, "fout": 0.08, "keep_lead": True}]},
        }},
    "liana_land": {
        "seconds": 0.50, "level": 0.24, "tail": 0.16,
        "what": "he lands: a soft heavy thump with a leafy rustle settling after it",
        "variants": {
            "a": {"why": "something heavy landing on dirt, then a shaken tree's leaves settling",
                  "layers": [
                      {"src": 536766, "a": 0.10, "b": 0.36, "gain": 1.0, "lp": 3000, "decay": 0.08},
                      {"src": 540278, "a": 0.13, "b": 0.62, "at": 0.05, "gain": 0.36, "hp": 700, "lp": 8000, "keep_lead": True, "fin": 0.05, "decay": 0.25}]},
            "b": {"why": "HEAVIER: a heavy body fall on dirt pitched down, with the dirt thud an octave lower under it, and a bush struck",
                  "layers": [
                      {"src": 504626, "a": 0.38, "b": 0.62, "pitch": 0.8, "gain": 1.0, "lp": 1500, "decay": 0.09},
                      {"src": 536766, "a": 0.10, "b": 0.36, "pitch": 0.6, "gain": 0.7, "lp": 900, "decay": 0.12},
                      {"src": 106113, "a": 0.02, "b": 0.56, "at": 0.06, "gain": 0.36, "hp": 700, "lp": 8000, "keep_lead": True, "fin": 0.05, "decay": 0.25}]},
            "c": {"why": "WOODIER and lighter: a landing onto sticks with a low wood block's hollow knock in it (a wooden body), then bamboo leaves",
                  "layers": [
                      {"src": 364690, "a": 0.09, "b": 0.36, "gain": 1.0, "lp": 2000, "decay": 0.08},
                      {"src": 218460, "a": 0.0, "b": 0.20, "pitch": 0.6, "gain": 0.45, "decay": 0.06},
                      {"src": 178615, "a": 0.12, "b": 0.62, "at": 0.06, "gain": 0.36, "hp": 700, "lp": 8000, "keep_lead": True, "fin": 0.05, "decay": 0.25}]},
        }},
}

# The whole move, per variant letter. The rush of air while he is reeled in is the variant's own
# swish, slowed to more than twice its length (over an octave down), then sped up as it plays so
# it RISES, with its level rising too.
SEQUENCE = {"at": {"liana_coil": 0.0, "liana_fire": 0.12, "liana_catch": 0.26, "liana_land": 0.62},
            "air": (0.26, 0.60), "air_gain": 0.45,
            "air_from": {"a": {"src": 855844, "a": 0.07, "b": 0.36},
                         "b": {"src": 352719, "a": 0.08, "b": 0.36, "hp": 180},
                         "c": {"src": 473583, "a": 0.10, "b": 0.36}}}


def one_layer(m, L):
    x = span(m, L["src"], L["a"], L["b"])
    x = x - x.mean()
    x = sos(x, "high", L.get("hp", 70.0))
    x = sos(x, "low", L.get("lp", 11000.0))
    if not L.get("keep_lead"):
        x = x[max(0, onset(x) - int(SR * 0.002)):]
    if L.get("glide"):
        x = glide(x, *L["glide"])
    x = repitch(x, L.get("pitch", 1.0))
    t = np.arange(len(x)) / SR
    if L.get("flat"):
        e = envelope(x, 30.0)
        x = x / np.maximum(e, e.max() * L["flat"])
    if L.get("decay"):
        x = x * np.exp(-t / L["decay"])
    if L.get("swell"):
        x = x * (0.25 + 0.75 * (t / max(t[-1], 1e-9)) ** L["swell"])
    x = fade(x, L.get("fin", 0.0015), L.get("fout", 0.03))
    return x / (loud(x, 0.05) + 1e-12) * L.get("gain", 1.0)


def lay(out, x, at):
    a = int(round(at * SR))
    if a + len(x) > len(out):
        out = np.concatenate([out, np.zeros(a + len(x) - len(out))])
    out[a:a + len(x)] += x
    return out


# ------------------------------------------------------------------ the second set: d, e, f
#
# Owner, 2026-10-07, on a, b, c heard over the animation: "a mix between B and c.. idk it sounds
# quite unnatural". So ONE direction in three shades: the weight and low body of b with the tight,
# sharp attack of c.   d = even blend,   e = leaning b (heavier),   f = leaning c (snappier).
#
# What a, b, c did that a recording never does, and what d, e, f do instead (`gentle` layers):
#   - a, b, c resampled sources by up to an octave (0.5 to 2.0). Here every `pitch` and every end
#     of a `glide` is held within 3 semitones (0.841 to 1.189): the script refuses anything wider.
#     Where a lower or higher sound was wanted, a different recording was chosen.
#   - a, b, c gated tails with fast exponential decays and 30 ms fades to hit short lengths. Here
#     each recording runs out the way it was recorded: most cues are 60 to 140 ms longer (each is
#     as long as its own recordings last, no longer), and only the last 70 to 80 ms are faded. (`decay` is used only on leaf beds, which have no end of their own.)
#   - a, b, c started every layer on the same sample. Here the body layer comes 6 to 12 ms after
#     the attack layer, as two things struck by one event do.
#   - a, b, c low-passed every layer at 11 kHz with a second-order filter, and thumps far lower.
#     Here nothing is low-passed but the landing thumps, and those with a first-order (6 dB an
#     octave) slope at 3 kHz, so the air above 8 kHz stays.
#   - a, b, c evened out the level of creaks (`flat`), which lifts the quiet grain between the
#     creak's own pulses. Not used here.
#   - a, b, c pitched one rope recording three ways for the three catches. Here d and f use two
#     different takes of it, unshifted, and e uses no rope at all.
#   - at most three layers a cue, most often an attack and a body.
SEMITONES_3 = (2 ** (-3 / 12.0), 2 ** (3 / 12.0))

CUES2 = {
    "liana_coil": {
        "seconds": 0.34, "level": 0.24, "tail": 0.07,
        "what": "the wind-up: a wooden creak that tightens upward (a timber's weight, a hinge's edge)",
        # The spans were chosen because they RISE as recorded (brightness and level both climb over
        # the span: searched for by measurement). The glide only leans on that, 4 semitones in all.
        "variants": {
            "d": {"why": "EVEN: a low timber creak that rises on its own, with a door hinge's creak (also rising as recorded) 10 ms behind it for the edge; both nudged up by 4 semitones over their length",
                  "layers": [
                      {"src": 113362, "a": 1.42, "b": 1.80, "glide": (0.9, 1.14), "gain": 1.0, "swell": 0.5},
                      {"src": 390123, "a": 0.08, "b": 0.44, "at": 0.010, "glide": (0.92, 1.14), "gain": 0.6}]},
            "e": {"why": "HEAVIER: the low timber creak leads, with an old sailing boat's wood stressing 10 ms behind it (both rise as recorded): no hinge",
                  "layers": [
                      {"src": 113362, "a": 1.42, "b": 1.80, "glide": (0.88, 1.12), "gain": 1.0, "swell": 0.5},
                      {"src": 675785, "a": 2.68, "b": 3.06, "at": 0.010, "glide": (0.9, 1.12), "gain": 0.6}]},
            "f": {"why": "SNAPPIER: the door hinge's rising creak leads, with the low timber creak 8 ms behind it and quieter for body",
                  "layers": [
                      {"src": 390123, "a": 0.08, "b": 0.44, "glide": (0.92, 1.16), "gain": 1.0},
                      {"src": 113362, "a": 1.42, "b": 1.80, "at": 0.008, "glide": (0.9, 1.14), "gain": 0.45, "swell": 0.5}]},
        }},
    "liana_fire": {
        "seconds": 0.48, "level": 0.24, "tail": 0.08,
        "what": "the vines whip out: a crack at the front with weight in it, then a swish that runs out on its own",
        "variants": {
            "d": {"why": "EVEN: the film library whip crack as recorded (its own ring left on it), with a hollow bamboo swish 8 ms behind",
                  "layers": [
                      {"src": 675833, "a": 0.02, "b": 0.50, "gain": 1.0},
                      {"src": 850168, "a": 0.02, "b": 0.42, "at": 0.008, "gain": 0.4, "keep_lead": True, "fin": 0.008}]},
            "e": {"why": "HEAVIER: the same crack 2 semitones lower, with a thin stick's lower swish 10 ms behind",
                  "layers": [
                      {"src": 675833, "a": 0.02, "b": 0.50, "pitch": 0.89, "gain": 1.0},
                      {"src": 352719, "a": 0.08, "b": 0.52, "at": 0.010, "gain": 0.4, "hp": 150, "keep_lead": True, "fin": 0.015}]},
            "f": {"seconds": 0.30,
                  "why": "SNAPPIER: a brighter, shorter whip crack as recorded (cut before the second sound that follows it in the file), with a long bamboo stick's fuller swish 6 ms behind as the body",
                  "layers": [
                      {"src": 529925, "a": 0.055, "b": 0.20, "gain": 1.0, "fout": 0.05},
                      {"src": 855844, "a": 0.07, "b": 0.50, "at": 0.006, "gain": 0.8, "keep_lead": True, "fin": 0.015}]},
        }},
    "liana_catch": {
        "seconds": 0.40, "level": 0.24, "tail": 0.08,
        "what": "the vines catch: a snap taut with a low body under it, then a creak of strain that runs out",
        "variants": {
            "d": {"seconds": 0.36,
                  "why": "EVEN: a rope snatched tight (its second take, unshifted) with a thud on earth 8 ms behind, then a rope's leathery creak",
                  "layers": [
                      {"src": 802697, "a": 2.56, "b": 2.92, "gain": 1.0},
                      {"src": 536766, "a": 0.10, "b": 0.44, "at": 0.008, "gain": 0.5},
                      {"src": 664929, "a": 2.26, "b": 2.58, "at": 0.085, "gain": 0.4, "fin": 0.01}]},
            "e": {"why": "HEAVIER: no rope: a wood hit, the thud on earth 10 ms behind it (cut in at its rise) and louder, then a tree's own creak as the strain",
                  "layers": [
                      {"src": 547414, "a": 0.12, "b": 0.42, "gain": 0.7},
                      {"src": 536766, "a": 0.125, "b": 0.44, "at": 0.010, "gain": 1.0, "keep_lead": True, "fin": 0.005},
                      {"src": 797994, "a": 0.72, "b": 1.04, "at": 0.09, "gain": 0.45, "fin": 0.01}]},
            "f": {"why": "SNAPPIER: the rope snatch's first take, unshifted and cut in at its snap, with a bamboo stick's hit 6 ms behind, then the rope's second, shorter creak",
                  "layers": [
                      {"src": 802697, "a": 1.362, "b": 1.72, "gain": 1.0, "keep_lead": True, "fin": 0.004},
                      {"src": 386888, "a": 0.11, "b": 0.30, "at": 0.006, "gain": 0.35},
                      {"src": 664929, "a": 2.80, "b": 3.08, "at": 0.085, "gain": 0.4, "fin": 0.01}]},
        }},
    "liana_land": {
        "seconds": 0.64, "level": 0.24, "tail": 0.08,
        "what": "he lands: a heavy thump with a woody knock in its front, and leaves settling after it",
        # The one filter of the set: the landings are rolled off above 3 kHz at 6 dB an octave (a
        # SOFT thump), which leaves their air in; the leaves are not filtered at the top at all.
        "variants": {
            "d": {"why": "EVEN: a landing onto sticks (its own crackle left in) with the thud on earth 10 ms behind and as loud, for weight, and bamboo leaves settling",
                  "layers": [
                      {"src": 364690, "a": 0.09, "b": 0.60, "gain": 0.8, "lp": 3000},
                      {"src": 536766, "a": 0.10, "b": 0.44, "at": 0.010, "gain": 1.0},
                      {"src": 178615, "a": 0.12, "b": 0.72, "at": 0.07, "gain": 0.3, "hp": 500, "keep_lead": True, "fin": 0.05, "decay": 0.3}]},
            "e": {"why": "HEAVIER: a heavy body fall on dirt as recorded (it has a second, smaller bump of its own 0.2 s on), the thud 2 semitones lower 12 ms behind, and a struck bush",
                  "layers": [
                      {"src": 504626, "a": 0.38, "b": 0.98, "gain": 0.8, "lp": 3000},
                      {"src": 536766, "a": 0.10, "b": 0.44, "at": 0.012, "pitch": 0.89, "gain": 1.0},
                      {"src": 106113, "a": 0.02, "b": 0.62, "at": 0.07, "gain": 0.3, "hp": 500, "keep_lead": True, "fin": 0.05, "decay": 0.3}]},
            "f": {"why": "SNAPPIER: the landing onto sticks leads, with a wood block's hollow knock 6 ms behind (2 semitones down: a wooden body), and a shaken tree's leaves, quieter",
                  "layers": [
                      {"src": 364690, "a": 0.09, "b": 0.60, "gain": 1.0, "lp": 3000},
                      {"src": 218460, "a": 0.0, "b": 0.28, "at": 0.006, "pitch": 0.89, "gain": 0.6},
                      {"src": 540278, "a": 0.13, "b": 0.72, "at": 0.07, "gain": 0.25, "hp": 500, "keep_lead": True, "fin": 0.05, "decay": 0.3}]},
        }},
}

# The game's own cue times (the body's landing squash is at 0.70 s). The rush of air while he is
# reeled in is no longer one swish slowed by more than an octave: it is four different swishes, from
# the lowest to the highest, each within 2 semitones of how it was recorded, one after
# the other and each louder than the last, so the rise is in the choice of recordings.
SEQUENCE2 = {"at": {"liana_coil": 0.0, "liana_fire": 0.14, "liana_catch": 0.28, "liana_land": 0.70},
             "air": (0.26, 0.74), "air_gain": 0.4,
             "air_layers": [{"src": 352719, "a": 0.02, "b": 0.50, "at": 0.00, "gain": 0.3, "hp": 150, "keep_lead": True, "fin": 0.04},
                            {"src": 473583, "a": 0.02, "b": 0.42, "at": 0.10, "gain": 0.5, "keep_lead": True, "fin": 0.03},
                            {"src": 855844, "a": 0.03, "b": 0.50, "at": 0.20, "gain": 0.8, "keep_lead": True, "fin": 0.03},
                            {"src": 850168, "a": 0.00, "b": 0.30, "at": 0.31, "gain": 1.0, "keep_lead": True, "fin": 0.02}],
             "air_pitch": {"d": 1.0, "e": 0.9, "f": 1.1}}

SETS = (("abc", CUES, SEQUENCE, False), ("def", CUES2, SEQUENCE2, True))


def gentle_layer(m, L):
    """A layer of the second set: see the list above CUES2."""
    for r in ([L["pitch"]] if "pitch" in L else []) + list(L.get("glide", ())):
        if not SEMITONES_3[0] - 1e-9 <= r <= SEMITONES_3[1] + 1e-9:
            raise ValueError("source %s: speed %.3f is more than 3 semitones from the recording" % (L["src"], r))
    if L.get("flat"):
        raise ValueError("source %s: `flat` is not used in the second set" % L["src"])
    x = span(m, L["src"], L["a"], L["b"])
    x = sos(x - x.mean(), "high", L.get("hp", 60.0))
    if L.get("lp"):
        x = sos(x, "low", L["lp"], 1)
    if not L.get("keep_lead"):
        x = x[max(0, onset(x) - int(SR * 0.005)):]
    if L.get("glide"):
        x = glide(x, *L["glide"])
    x = repitch(x, L.get("pitch", 1.0))
    t = np.arange(len(x)) / SR
    if L.get("decay"):
        x = x * np.exp(-t / L["decay"])
    if L.get("swell"):
        x = x * (0.4 + 0.6 * (t / max(t[-1], 1e-9)) ** L["swell"])
    x = fade(x, L.get("fin", 0.003), L.get("fout", 0.07))
    return x / (loud(x, 0.05) + 1e-12) * L.get("gain", 1.0)


def staged_air(m, letter):
    a0, a1 = SEQUENCE2["air"]
    out = np.zeros(1)
    for L in SEQUENCE2["air_layers"]:
        out = lay(out, gentle_layer(m, dict(L, pitch=SEQUENCE2["air_pitch"][letter])), L["at"])
    n = int(round((a1 - a0) * SR))
    out = np.concatenate([out, np.zeros(max(0, n - len(out)))])[:n]
    out = fade(out, 0.02, 0.08)
    return out / (loud(out, 0.1) + 1e-12)


def finish(x, seconds, level, tail):
    """Cut to length, fade the ends, level by the loudest 100 ms, bend what is over the peak."""
    x = sos(x, "high", 60.0)
    n = int(round(seconds * SR))
    x = np.concatenate([x, np.zeros(max(0, n - len(x)))])[:n]
    x = fade(x, 0.0015, tail)
    x = x * level / (loud(x, 0.1) + 1e-12)
    over = float(np.abs(x).max())
    if over > HOT:                                    # too tall to bend cleanly: it stays quieter instead
        x, over = x * HOT / over, HOT
    x = fade(soft(x), 0.0015, 0.004)
    return x, over


def build_cue(m, name, letter, table=None, gentle=False):
    c = (table or CUES)[name]
    out = np.zeros(1)
    for L in c["variants"][letter]["layers"]:
        out = lay(out, (gentle_layer if gentle else one_layer)(m, L), L.get("at", 0.0))
    return finish(out, c["variants"][letter].get("seconds", c["seconds"]), c["level"], c["tail"])


def rising_air(m, letter):
    """The reel-in: the variant's swish, slowed to 0.42 of its speed, its speed then climbing from
    0.75 to 1.7 so the pitch rises, and its level rising to just before the landing."""
    a0, a1 = SEQUENCE["air"]
    A = SEQUENCE["air_from"][letter]
    x = span(m, A["src"], A["a"], A["b"])
    x = sos(sos(x - x.mean(), "high", A.get("hp", 120.0)), "low", 9000.0)
    x = repitch(x, 0.42)
    x = glide(x, 0.75, 1.7)
    n = int(round((a1 - a0) * SR))
    x = np.concatenate([x, np.zeros(max(0, n - len(x)))])[:n]
    # The swish's own level is a bump: flatten it (divide by its 30 ms level), then give it the rise.
    e = envelope(x, 30.0)
    x = x / np.maximum(e, e.max() * 0.3)
    t = np.arange(n) / n
    x = x * (0.15 + 0.85 * t ** 1.6)
    x = fade(x, 0.03, 0.05)
    return x / (loud(x, 0.1) + 1e-12)


def build_sequence(m, letter, cues, table=None, seq=None, gentle=False):
    table, seq = table or CUES, seq or SEQUENCE
    out = np.zeros(int(SR * 1.6))
    for name, at in seq["at"].items():
        out = lay(out, cues[name + "_" + letter], at)
    air = (staged_air if gentle else rising_air)(m, letter) * table["liana_fire"]["level"] * seq["air_gain"]
    out = lay(out, air, seq["air"][0])
    end = seq["at"]["liana_land"] + table["liana_land"]["seconds"]
    out = out[:int(round(end * SR))]
    peak = float(np.abs(out).max())
    return fade(soft(out), 0.0015, 0.004), peak, air


# ------------------------------------------------------------------ listening without ears: a draft

def band_centroid(x, a, b):
    return centroid(x[int(a * SR):int(b * SR)])


def window_db(x, a, b, ref):
    s = x[int(a * SR):int(b * SR)]
    return 20 * np.log10(np.sqrt((s ** 2).mean()) / ref + 1e-9) if len(s) else -180.0


def measure(name, x, over):
    """Everything that can be said of a draft without hearing it, and what is wrong with it."""
    n = len(x)
    T = n / SR
    e2 = envelope(x, 2.0)
    top = e2.max() + 1e-12
    energy = x ** 2
    total = energy.sum() + 1e-12
    thirds = [float(energy[int(n * k / 3):int(n * (k + 1) / 3)].sum() / total) for k in range(3)]
    ref = loud(x, 0.05)
    r = {"name": name, "seconds": T, "peak": float(np.abs(x).max()), "rms": float(np.sqrt(energy.mean())),
         "loud": loud(x, 0.1), "bent_from": over, "clipped": int((np.abs(x) >= 0.999).sum()),
         "peak_at": float(np.argmax(e2)) / SR, "lead_ms": 1000.0 * float(np.argmax(e2 >= top * 10 ** (-30 / 20.0))) / SR,
         "centre": float((np.arange(n) * energy).sum() / total) / SR, "thirds": thirds,
         "centroid": centroid(x), "c_first": band_centroid(x, 0, T / 3), "c_last": band_centroid(x, 2 * T / 3, T),
         "ends": (int(round(x[0] * 32767)), int(round(x[-1] * 32767))),
         "tail_db": window_db(x, T - 0.05, T, ref), "last_db": window_db(x, T - 0.008, T, ref), "dc": float(x.mean())}
    e10 = envelope(x, 10.0)
    r["sounds_until"] = float(np.nonzero(e10 >= e10.max() * 10 ** (-35 / 20.0))[0][-1]) / SR
    kind = name.rsplit("_", 1)[0]
    bad = []
    if r["clipped"] or r["peak"] > PEAK + 1e-6:
        bad.append("over the peak")
    if max(abs(r["ends"][0]), abs(r["ends"][1])) > 40:
        bad.append("an end is not at zero (click)")
    if r["lead_ms"] > 30:
        bad.append("%.0f ms of near silence at the start" % r["lead_ms"])
    # The coil is a creak that tightens until it is let go: it is loud to its end by design, and
    # only its last 8 ms are held to account. Everything else must have died away by its end.
    if kind == "liana_coil":
        if r["last_db"] > -18:
            bad.append("the last 8 ms is only %.0f dB under the loudest (it would click off)" % r["last_db"])
    elif r["tail_db"] > -12:
        bad.append("the last 50 ms is only %.0f dB under the loudest (cut off, or a noisy tail)" % r["tail_db"])
    if r["sounds_until"] < 0.7 * T and not kind.endswith("sequence"):
        bad.append("nothing above -35 dB after %.2f s of %.2f s (padded with silence)" % (r["sounds_until"], T))
    if "level" in CUES.get(kind, {}) and r["loud"] < CUES[kind]["level"] * 10 ** (-2.0 / 20.0):
        bad.append("%.1f dB quieter than its fellows (held down to keep its peak clean)" % (20 * np.log10(CUES[kind]["level"] / r["loud"])))
    if kind == "liana_coil":
        r["rise"] = r["c_last"] / (r["c_first"] + 1e-9)
        if r["rise"] < (1.08 if name[-1] in "def" else 1.2):
            bad.append("the creak does not rise (centroid x%.2f from first to last third)" % r["rise"])
        if r["thirds"][0] > 0.45:
            bad.append("front-heavy for a wind-up (first third has %.0f%% of the energy)" % (100 * r["thirds"][0]))
        if r["thirds"][2] < 0.15:
            bad.append("it dies before the end (last third has %.0f%% of the energy)" % (100 * r["thirds"][2]))
    if kind == "liana_fire":
        r["swish_db"] = window_db(x, 0.06, 0.20, ref)
        if r["peak_at"] > 0.04:
            bad.append("the crack is not at the front (peak at %.3f s)" % r["peak_at"])
        if r["swish_db"] < -22:
            bad.append("no swish after the crack (%.0f dB)" % r["swish_db"])
    if kind == "liana_catch":
        r["strain_db"] = window_db(x, 0.10, 0.25, ref)
        if r["peak_at"] > 0.04:
            bad.append("the thwack is not at the front (peak at %.3f s)" % r["peak_at"])
        if not -26 < r["strain_db"] < -5:
            bad.append("the strain after the thwack is at %.0f dB (wanted 5 to 26 dB under it)" % r["strain_db"])
    if kind == "liana_land":
        r["c_thump"] = band_centroid(x, 0.0, 0.06)
        r["c_rustle"] = band_centroid(x, 0.18, 0.45)
        r["rustle_db"] = window_db(x, 0.18, 0.40, ref)
        r["rustle_top_db"] = window_db(sos(x, "high", 2500.0, 4), 0.06, 0.20, ref)
        if r["peak_at"] > 0.08:
            bad.append("the thump is late (peak at %.3f s)" % r["peak_at"])
        if r["c_thump"] > (2000 if name[-1] in "def" else 1500):
            bad.append("the thump is not low (centroid %d Hz)" % r["c_thump"])
        if r["c_rustle"] < 1.5 * r["c_thump"]:
            bad.append("no leafy rustle after the thump (centroid %d Hz after, %d Hz at it)" % (r["c_rustle"], r["c_thump"]))
        if r["rustle_top_db"] > -9:
            bad.append("the leaves come in as loud as the thump (%.0f dB above 2.5 kHz at 0.06 to 0.20 s): not a SOFT thump" % r["rustle_top_db"])
        if not -30 < r["rustle_db"] < -6:
            bad.append("the rustle is at %.0f dB (wanted 6 to 30 dB under the thump)" % r["rustle_db"])
    r["bad"] = bad
    return r


def report_line(r):
    extra = ""
    for k, label in (("rise", "rise x%.2f"), ("swish_db", "swish %.0f dB"), ("strain_db", "strain %.0f dB"),
                     ("c_thump", "thump %d Hz"), ("c_rustle", "rustle %d Hz"), ("rustle_db", "rustle %.0f dB"), ("rustle_top_db", "leaves come in at %.0f dB")):
        if k in r:
            extra += "  " + label % r[k]
    return "%-18s %5.3f s  peak %.3f (bent from %.2f)  rms %.3f  loud %.3f  peak at %.3f  lead %4.1f ms  centre %.3f  thirds %2.0f/%2.0f/%2.0f%%  centroid %5d Hz (%5d -> %5d)  tail %4.0f dB  sounds until %.2f  ends %d %d%s  %s" % (
        r["name"], r["seconds"], r["peak"], r["bent_from"], r["rms"], r["loud"], r["peak_at"], r["lead_ms"], r["centre"],
        100 * r["thirds"][0], 100 * r["thirds"][1], 100 * r["thirds"][2], r["centroid"], r["c_first"], r["c_last"],
        r["tail_db"], r["sounds_until"], r["ends"][0], r["ends"][1], extra, "OK" if not r["bad"] else "WRONG: " + "; ".join(r["bad"]))


def src_names(m, layers):
    return ", ".join("%s (%s, by %s)" % (L["src"], by_id(m, L["src"])["title"].lower(), by_id(m, L["src"])["author"]) for L in layers)


def write_readme(m, rows, seq_rows):
    by = {r["name"]: r for r in rows}
    L = ["PAETE, LIANA LEAP: sound drafts for the owner to listen to", "",
         "Written by tools/build_paete_skill_sfx.py. Do not edit by hand: change the CUES tables there and rerun.",
         "Nothing here is in the game. 44100 Hz, mono, 16 bit, peak at most 0.85.", "",
         "NOBODY HAS HEARD THESE. They were cut and checked by a machine that cannot listen: it measured where",
         "the energy is, how bright each part is, the peak, the loudness and the ends. It cannot say that any",
         "of them sounds good, or like wood, or natural. report.txt has the numbers.", "",
         "Every source is a Creative Commons 0 recording from Freesound (the id is the number in",
         "freesound.org/s/<id>/). Each sound's page was read before the download and states CC0.",
         "Provenance, authors and SHA-256: tools/paete_sfx_sources.json. Downloads: ArtSource/paete/sfx-sources/.", "",
         "=" * 100,
         "SECOND SET: d, e, f   (2026-10-07, after the owner heard a, b, c: \"a mix between B and c.. idk it",
         "sounds quite unnatural\")", "",
         "One direction in three shades: the weight and low body of b with the tight, sharp attack of c.",
         "  d  the even blend",
         "  e  leaning b (heavier)",
         "  f  leaning c (snappier)", "",
         "What was changed to take the processing out of them (nobody has heard whether it worked):",
         "  - no recording is sped up or slowed by more than 3 semitones (a, b, c went as far as an octave);",
         "    where a lower or higher sound was wanted, a different recording was chosen",
         "  - each recording runs out the way it was recorded: no fast gates; most cues are 60 to 140 ms longer",
         "    than before (each as long as its own recordings last), and only the last 70 to 80 ms are faded",
         "  - the body layer starts 6 to 12 ms after the attack layer, not on the same sample",
         "  - nothing is low-passed but the landing thumps, and those gently (6 dB an octave from 3 kHz): the air",
         "    above 8 kHz is left in",
         "  - the creaks' level is no longer evened out (that lifted the grain between a creak's pulses); the coil",
         "    spans were chosen because they rise in brightness and level AS RECORDED, and are only nudged further",
         "  - the catch is a different recording in each: d and f are two takes of the rope, unshifted; e has no rope",
         "  - two layers a cue where that was enough, never more than three", ""]

    def cues_of(table):
        out = []
        for name, c in table.items():
            out += ["%s  (about %.2f s)  %s" % (name, c["seconds"], c["what"])]
            for letter, v in c["variants"].items():
                r = by[name + "_" + letter]
                out += ["  %s_%s.wav  %.2f s" % (name, letter, r["seconds"]),
                        "      %s" % v["why"],
                        "      from: %s" % src_names(m, v["layers"])]
            out.append("")
        return out
    L += cues_of(CUES2)
    T = SEQUENCE2["at"]
    L += ["liana_sequence_d.wav, liana_sequence_e.wav, liana_sequence_f.wav  the whole move at the game's own cue times:",
          "      coil at %.2f s, fire at %.2f, catch at %.2f, land at %.2f (the body's landing squash)." % (
              T["liana_coil"], T["liana_fire"], T["liana_catch"], T["liana_land"]),
          "      From %.2f to %.2f a rising rush of air (he is being reeled in): four different swishes, from the lowest" % SEQUENCE2["air"],
          "      to the highest, one after the other and each louder, none shifted by more than 2 semitones (d as recorded,",
          "      e 2 semitones down, f 2 up).",
          "      air from: %s" % src_names(m, SEQUENCE2["air_layers"]), "",
          "=" * 100,
          "FIRST SET: a, b, c   (kept for comparison; the owner found them unnatural)", "",
          "Three separate readings of the move:",
          "  a  the plain reading",
          "  b  heavier and lower (a big trunk)",
          "  c  tighter, sharper, ropier", ""]
    L += cues_of(CUES)
    L += ["liana_sequence_a.wav, liana_sequence_b.wav, liana_sequence_c.wav  the whole move with that letter's four cues:",
          "      coil at 0.00 s, fire at 0.12, catch at 0.26, land at 0.62 (NOT the game's times: see d, e, f), and from",
          "      0.26 to 0.60 a rising rush of air made from that letter's swish, slowed by more than an octave and",
          "      then sped up as it plays."]
    for letter, A in SEQUENCE["air_from"].items():
        L.append("      %s: air from %s" % (letter, src_names(m, [A])))
    L += ["", "=" * 100, "MEASURED (report.txt has all of it):"]
    for r in rows + seq_rows:
        L.append("  %-18s %.2f s, peak %.2f, loudest at %.3f s, centroid %d Hz: %s" % (
            r["name"], r["seconds"], r["peak"], r["peak_at"], r["centroid"],
            "matches its description by the numbers" if not r["bad"] else "WRONG: " + "; ".join(r["bad"])))
    (OUT / "README.txt").write_text("\n".join(L) + "\n", encoding="utf-8", newline="\n")


def build(m, only):
    OUT.mkdir(parents=True, exist_ok=True)
    rows, seq_rows, used = [], [], {}
    for letters, table, seq, gentle in SETS:
        cues = {}
        for name, c in table.items():
            for letter, v in c["variants"].items():
                x, over = build_cue(m, name, letter, table, gentle)
                cues[name + "_" + letter] = x
                r = measure(name + "_" + letter, x, over)
                if gentle:
                    # What can be measured of "let it run out": how long the draft takes to fall the
                    # last 20 dB to its end. A gate does it in a few ms; a recording's own tail takes longer.
                    late = [L.get("at", 0.0) for L in v["layers"][1:]]
                    r["offsets_ms"] = [round(1000 * a, 1) for a in late]
                    if any(a < 0.005 for a in late):
                        r["bad"].append("a layer starts with the attack layer (offsets %s ms)" % r["offsets_ms"])
                rows.append(r)
                for Lr in v["layers"]:
                    used.setdefault(str(Lr["src"]), []).append("%s_%s %.2f..%.2f s" % (name, letter, Lr["a"], Lr["b"]))
                if not only or only in r["name"]:
                    write(OUT / (r["name"] + ".wav"), x)
        for letter in letters:
            x, peak, air = build_sequence(m, letter, cues, table, seq, gentle)
            r = measure("liana_sequence_" + letter, x, peak)
            half = len(air) // 2
            r["air_rise"] = centroid(air[half:]) / (centroid(air[:half]) + 1e-9)
            r["air_swell_db"] = 20 * np.log10(np.sqrt((air[half:] ** 2).mean()) / (np.sqrt((air[:half] ** 2).mean()) + 1e-12))
            if r["air_rise"] < 1.15 or r["air_swell_db"] < 3:
                r["bad"].append("the air does not rise (centroid x%.2f, level %+.1f dB, second half over first)" % (r["air_rise"], r["air_swell_db"]))
            seq_rows.append(r)
            if not only or only in r["name"]:
                write(OUT / (r["name"] + ".wav"), x)
        if gentle:
            for A in seq["air_layers"]:
                used.setdefault(str(A["src"]), []).append("liana_sequence_%s air %.2f..%.2f s" % ("/".join(letters), A["a"], A["b"]))
        else:
            for letter, A in seq["air_from"].items():
                used.setdefault(str(A["src"]), []).append("liana_sequence_%s air %.2f..%.2f s" % (letter, A["a"], A["b"]))
    lines = [report_line(r) for r in rows]
    lines += [report_line(r) + "  [air: centroid x%.2f, level %+.1f dB, second half over first]" % (r["air_rise"], r["air_swell_db"]) for r in seq_rows]
    print("\n".join(lines))
    if not only:
        (OUT / "report.txt").write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
        write_readme(m, rows, seq_rows)
        for s in m["sources"]:
            s["used_by"] = used.get(s["id"], [])
        save_manifest(m)
    return rows + seq_rows


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    ap.add_argument("--fetch", action="store_true")
    ap.add_argument("--analyse", action="store_true")
    args = ap.parse_args()
    m = load_manifest()
    missing = [s for s in m["sources"] if not (CACHE / s["file"]).exists() and not s.get("skipped")]
    if args.fetch or missing:
        fetch(m)
        if args.fetch:
            return
    if args.analyse or any("measured" not in s for s in m["sources"] if not s.get("skipped")):
        analyse(m)
        if args.analyse:
            return
    build(m, args.only)


if __name__ == "__main__":
    main()
