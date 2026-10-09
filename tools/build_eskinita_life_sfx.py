"""The Eskinita Alley's animal sounds, cut from REAL recordings. Nothing here is synthesised.

  py -3 tools/build_eskinita_life_sfx.py            fetch what is missing, then cut every cue
  py -3 tools/build_eskinita_life_sfx.py --fetch    only download and check the sources

Chickens (cluck, crow, alarm squawk), cats (meow, hiss, purr) and dogs (bark, growl, whine, pant,
yelp, the scrabble of claws) for `AlleyLifeSound` (the chickens of `AlleyChickens`, the cats and dogs
of `AlleyPets`). The method is tools/build_paete_skill_sfx.py's: Creative Commons 0 recordings from
Freesound's preview CDN, each sound's OWN PAGE read first and used only if it states CC0 and no
other Creative Commons licence, one request at a time, and every file's id, page, URL, author,
licence and SHA-256 kept in tools/eskinita_life_sfx_sources.json.

WHAT IS WHERE
  tools/eskinita_life_sfx_sources.json          the sources, what each is used for, what was measured and cut
  ArtSource/eskinita/sfx-sources/               the downloads, as fetched (high-quality MP3 previews)
  Assets/TumbangPreso/Art/audio/ambience/sfx_alley_<cue>_<n>.wav   the cues: mono, 44.1 kHz, 16 bit, peak 0.85

THE CUT. A recording is turned into EVENTS (stretches where its 10 ms envelope stays within `floor`
dB of its peak, joined across gaps shorter than `join`), and a cue takes the events its row asks
for: the loudest ones that are not longer than `longest` seconds, or (for `whole`) everything from
the first event to the last. Each cut gets 30 ms of lead-in room, a tail until it has fallen 34 dB
(at most `tail` s), an 80 Hz high-pass (handling rumble), a 4 ms fade in and a 40 ms fade out, and
is normalised to peak 0.85. No pitch change, no layering, no reverb, no generated signal of any kind.

⚠️ NOBODY HAS HEARD THESE. They were chosen by title, description and measurement only. The json
keeps each source's title, description and every cut's time span, so a wrong one is found fast:
change its row in CUES below (another source id, `pick`, `longest`) and rerun.
numpy, scipy, soundfile (libsndfile 1.1 or later, which decodes MP3).
"""
import argparse
import datetime
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
MANIFEST = ROOT / "tools/eskinita_life_sfx_sources.json"
CACHE = ROOT / "ArtSource/eskinita/sfx-sources"
OUT = ROOT / "Assets/TumbangPreso/Art/audio/ambience"
AGENT = "TumbangPresoLifeSfx/1.0 (indie game, CC0 animal recordings, one request at a time; thegrinchisthebest@gmail.com)"
CC0_MARK = "creativecommons.org/publicdomain/zero/1.0"
OTHER_LICENCES = ("creativecommons.org/licenses/by", "creativecommons.org/licenses/sampling")
TODAY = datetime.date.today().isoformat()

# cue -> rows of (freesound id, how many events to take, longest event in s, whole?, floor dB, join s, tail s)
CUES = {
    # ---- the alley's own animals (AlleyLifeSound)
    # 823065 is one hen scolding for 11 s: it is split finely (12 dB, 40 ms) and gives the squawks; the soft clucks are the others.
    "chicken_cluck": [("668804", 1, 0.8, False, 26, 0.06, 0.25), ("717602", 3, 0.8, False, 20, 0.05, 0.2), ("479596", 2, 0.6, False, 16, 0.05, 0.2)],
    "rooster_crow": [("435508", 1, 4.0, True, 30, 0.35, 0.5), ("233167", 1, 4.0, True, 30, 0.35, 0.5), ("233160", 1, 4.0, True, 30, 0.35, 0.5)],
    "chicken_squawk": [("316920", 1, 1.2, False, 28, 0.1, 0.3), ("823065", 3, 0.7, False, 12, 0.04, 0.15)],
    "cat_meow": [("448018", 1, 2.0, False, 30, 0.15, 0.4), ("756236", 1, 2.0, False, 30, 0.15, 0.4), ("511448", 1, 2.0, False, 30, 0.15, 0.4), ("763906", 1, 2.0, False, 30, 0.15, 0.4)],
    # 826834 and 819958 were fetched and DROPPED: their own pages say they are a person imitating a cat.
    "cat_hiss": [("485952", 1, 2.5, False, 28, 0.15, 0.4), ("146963", 2, 2.0, False, 24, 0.15, 0.4), ("679954", 1, 2.0, False, 24, 0.15, 0.4)],
    "cat_purr": [("436542", 1, 6.0, True, 34, 0.5, 0.4), ("585775", 1, 6.0, True, 34, 0.5, 0.4)],
    # 484297 was fetched and DROPPED: its page says it is a person imitating a dog. 536501 ("cartoonish") and 424076 ("imitation of chicken clucking") too.
    "dog_bark": [("630648", 1, 0.9, False, 28, 0.08, 0.35), ("535457", 1, 0.9, False, 28, 0.08, 0.35), ("277058", 1, 0.9, False, 28, 0.08, 0.35),
                 ("800278", 1, 0.9, False, 28, 0.08, 0.35)],
    "dog_growl": [("829986", 1, 3.0, False, 26, 0.3, 0.4), ("483190", 1, 3.0, False, 26, 0.3, 0.4)],
    "dog_whine": [("742053", 1, 2.5, False, 28, 0.25, 0.4), ("462660", 2, 2.5, False, 28, 0.25, 0.4)],
    "dog_pant": [("827433", 1, 5.0, True, 34, 0.6, 0.3), ("724909", 1, 5.0, True, 34, 0.6, 0.3)],
    "dog_yelp": [("735365", 1, 1.5, False, 28, 0.12, 0.35), ("452180", 2, 1.2, False, 28, 0.1, 0.35)],
    "dog_scrabble": [("208654", 1, 4.0, True, 24, 0.5, 0.3)],
    # ---- the neighbourhood round the alley (AlleySoundscape's scattered one-shots)
    "amb_motorbike": [("484486", 1, 10.0, True, 30, 1.5, 0.6), ("405020", 1, 10.0, True, 30, 1.5, 0.6)],
    "amb_gate": [("682775", 1, 3.5, True, 30, 0.5, 0.5), ("833566", 1, 3.0, True, 30, 0.5, 0.5)],
    "amb_dishes": [("209009", 2, 1.5, False, 28, 0.15, 0.3), ("478079", 1, 4.0, True, 30, 0.4, 0.3), ("209002", 1, 3.0, True, 30, 0.3, 0.3)],
    "amb_bell": [("767307", 1, 4.0, True, 32, 0.6, 0.6), ("200318", 1, 4.0, True, 32, 0.6, 0.6)],
    "amb_horn": [("629923", 3, 1.2, False, 22, 0.12, 0.3)],
    "amb_cloth": [("245839", 1, 5.0, True, 30, 0.8, 0.4), ("280205", 1, 5.0, True, 30, 0.8, 0.4), ("701647", 1, 5.0, True, 30, 0.8, 0.4)],
    "amb_wings": [("689998", 1, 4.0, True, 30, 0.5, 0.4), ("414671", 1, 2.5, True, 30, 0.4, 0.3), ("543118", 1, 3.0, True, 30, 0.4, 0.3)],
}

# The beds: name -> (freesound id, loop seconds, RMS it is brought to). A bed is the STEADIEST stretch of its
# recording (the window whose loudest second is nearest its median second, so no close event sits in it),
# closed into a loop by an equal-power crossfade of its last 3 s into its first 3 s. Lengths differ on
# purpose, so the layers never line up the same way twice.
# The first id is the one wanted; the rest are stand-ins used, in order, when it could not be fetched.
BEDS = {
    "neighbourhood": (("653095", "399025", "149935"), 61.0, 0.06),   # birds, distant traffic, a breeze: the barangay's hum
    "street": (("835521",), 47.0, 0.06),                            # suburban Ho Chi Minh City traffic: motorbikes on the road outside
    "kids": (("868247",), 53.0, 0.05),                              # children playing in a park, far off
    "sparrows": (("320287", "628949", "512805"), 43.0, 0.05),        # sparrows on a suburban street: the maya on the roofs
    "wind": (("460114",), 37.0, 0.05),                              # a windy suburb: up on the top terrace and the roofs
}


def load_manifest():
    if MANIFEST.exists():
        return json.loads(MANIFEST.read_text(encoding="utf-8"))
    return {"about": "Provenance for the Eskinita Alley's animal sounds (chickens, cats, dogs). tools/build_eskinita_life_sfx.py "
                     "reads this, fills in what it fetches and measures, and cuts the cues from it. Every source is a real "
                     "recording under Creative Commons 0, confirmed on the sound's own Freesound page. Nobody has heard the cuts.",
            "cache": "ArtSource/eskinita/sfx-sources", "output": "Assets/TumbangPreso/Art/audio/ambience/sfx_alley_<cue>_<n>.wav",
            "sources": [], "cues": {}}


def save_manifest(m):
    MANIFEST.write_text(json.dumps(m, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")


def get(url):
    req = urllib.request.Request(url, headers={"User-Agent": AGENT})
    with urllib.request.urlopen(req, timeout=75) as f:
        return f.read()


def fetch(m, pause=1.5):
    """Download what is missing. The sound's own page is read first and must state CC0 and no
    other Creative Commons licence; the file's URL, author and description come from that page."""
    import soundfile as sf
    CACHE.mkdir(parents=True, exist_ok=True)
    known = {s["id"]: s for s in m["sources"]}
    wanted = [(cue, row[0]) for cue, rows in CUES.items() for row in rows] + [("bed_" + name, ident) for name, row in BEDS.items() for ident in row[0]]
    for cue, ident in wanted:
        if ident not in known:
            s = {"id": ident, "origin": "freesound", "page": f"https://freesound.org/s/{ident}/", "use": cue, "file": f"{cue}_fs{ident}.mp3"}
            m["sources"].append(s)
            known[ident] = s
    used = {ident for _cue, ident in wanted}
    for s in m["sources"]:
        s["used"] = s["id"] in used
        if not s["used"] and not s.get("dropped"):
            s["dropped"] = "fetched, then not used: see the note in CUES (a person's imitation, or a cut that did not separate)"
    for s in m["sources"]:
        path = CACHE / s["file"]
        if not path.exists():
            if s.get("skipped", "").startswith("its page") or not s.get("used", True):
                continue
            try:
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
                s["fetched"] = TODAY
                data = get(s["url"])
                time.sleep(pause)
                path.write_bytes(data)
                s.pop("skipped", None)
            except Exception as e:                          # a failed URL is skipped and reported, never substituted
                s["skipped"] = "download failed: %s" % e
                print("SKIPPED %s: %s" % (s["id"], e))
                save_manifest(m)
                continue
            save_manifest(m)
        data = path.read_bytes()
        s["sha256"], s["bytes"] = hashlib.sha256(data).hexdigest(), len(data)
        info = sf.info(str(path))
        s["seconds"] = round(info.frames / info.samplerate, 3)
        s["format"] = "%s, %d Hz, %d ch" % (info.format, info.samplerate, info.channels)
        print("%-8s %-40s %7d bytes %6.2f s  %s | %s" % (s["id"], s["file"], len(data), s["seconds"], s.get("name_on_page", ""), s.get("description", "")[:90]))
    save_manifest(m)


def decode(path):
    import soundfile as sf
    x, sr = sf.read(str(path), dtype="float64", always_2d=True)
    x = x.mean(axis=1)
    if sr != SR:
        x = signal.resample_poly(x, SR, sr)
    return x


def envelope(x, ms=10.0):
    n = max(1, int(SR * ms / 1000))
    return np.sqrt(np.convolve(x * x, np.ones(n) / n, mode="same"))


def events(x, floor_db, join):
    """(start, end, peak) in samples of every stretch within `floor_db` of the recording's peak envelope."""
    env = envelope(x)
    top = env.max()
    if top <= 0:
        return []
    on = env > top * 10 ** (-floor_db / 20)
    edges = np.flatnonzero(np.diff(np.concatenate(([0], on.astype(np.int8), [0]))))
    spans = [[int(a), int(b)] for a, b in zip(edges[::2], edges[1::2])]
    merged = []
    for a, b in spans:
        if merged and a - merged[-1][1] < join * SR:
            merged[-1][1] = b
        else:
            merged.append([a, b])
    return [(a, b, float(np.abs(x[a:b]).max())) for a, b in merged if b - a > 0.03 * SR]


def cut(x, a, b, tail):
    env = envelope(x)
    level = env[a:b].max() * 10 ** (-34 / 20)
    end = b
    stop = min(len(x), b + int(tail * SR))
    while end < stop and env[end] > level:
        end += 1
    a = max(0, a - int(0.03 * SR))
    y = x[a:end].copy()
    y = signal.sosfiltfilt(signal.butter(2, 80, "highpass", fs=SR, output="sos"), y)
    fi, fo = int(0.004 * SR), min(int(0.04 * SR), len(y) // 3)
    y[:fi] *= np.linspace(0, 1, fi)
    y[-fo:] *= np.linspace(1, 0, fo)
    peak = np.abs(y).max()
    return (y * (PEAK / peak) if peak > 0 else y), a, end


def write(path, x):
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(np.round(np.clip(x, -1, 1) * 32767).astype(np.int16).tobytes())


def centroid(x):
    spec = np.abs(np.fft.rfft(x * np.hanning(len(x))))
    return float((spec * np.fft.rfftfreq(len(x), 1 / SR)).sum() / max(spec.sum(), 1e-12))


def build(m):
    OUT.mkdir(parents=True, exist_ok=True)
    known = {s["id"]: s for s in m["sources"]}
    m["cues"] = {}
    for old in OUT.glob("sfx_alley_*.wav"):
        old.unlink()
    for cue, rows in CUES.items():
        n = 0
        made = []
        for ident, take, longest, whole, floor_db, join, tail in rows:
            s = known.get(ident)
            path = CACHE / s["file"] if s else None
            if not s or not path.exists() or not s.get("licence_confirmed"):
                print("  %-16s source %s is not available: skipped" % (cue, ident))
                continue
            x = decode(path)
            ev = events(x, floor_db, join)
            if not ev:
                continue
            if whole:
                a, b = ev[0][0], ev[-1][1]
                if (b - a) / SR > longest:
                    b = a + int(longest * SR)
                chosen = [(a, b)]
            else:
                fit = [e for e in ev if (e[1] - e[0]) / SR <= longest] or ev
                chosen = sorted(sorted(fit, key=lambda e: -e[2])[:take])
                chosen = [(a, b) for a, b, _ in chosen]
            for a, b in chosen:
                y, a2, b2 = cut(x, a, b, tail)
                n += 1
                name = f"sfx_alley_{cue}_{n}.wav"
                write(OUT / name, y)
                made.append({"file": name, "source": ident, "author": s.get("author", ""), "from_s": round(a2 / SR, 3), "to_s": round(b2 / SR, 3),
                             "seconds": round(len(y) / SR, 3), "centroid_hz": round(centroid(y)), "events_in_source": len(ev)})
                print("  %-34s %5.2f s  from fs%s %.2f..%.2f (%d events in it)  centroid %d Hz" %
                      (name, len(y) / SR, ident, a2 / SR, b2 / SR, len(ev), centroid(y)))
        m["cues"][cue] = made
    m["beds"] = {}
    for old in OUT.glob("alley_bed_*.wav"):
        old.unlink()
    for name, (idents, seconds, rms) in BEDS.items():
        ident = next((i for i in idents if known.get(i) and (CACHE / known[i]["file"]).exists() and known[i].get("licence_confirmed")), None)
        if ident is None:
            print("  bed %-14s none of its sources %s is available: skipped" % (name, ", ".join(idents)))
            continue
        s = known[ident]
        path = CACHE / s["file"]
        x = decode(path)
        x = signal.sosfiltfilt(signal.butter(2, 60, "highpass", fs=SR, output="sos"), x)
        fade = 3.0
        need = int((seconds + fade) * SR)
        if len(x) < need:
            seconds = max(8.0, len(x) / SR - fade - 0.2)
            need = int((seconds + fade) * SR)
        total = np.concatenate(([0.0], np.cumsum(x * x)))            # a one-second RMS every half second
        starts = np.arange(0, len(x) - SR, SR // 2)
        sec = np.sqrt((total[starts + SR] - total[starts]) / SR + 1e-12)
        span = int((seconds + fade) * 2)
        best, start = 1e9, 0
        for k in range(0, max(1, len(sec) - span)):
            w = sec[k:k + span]
            score = w.max() / max(np.median(w), 1e-9)
            if score < best:
                best, start = score, k
        a = max(0, min(start * (SR // 2), len(x) - need))
        seg = x[a:a + need].copy()
        n, f = int(seconds * SR), int(fade * SR)
        t = np.linspace(0, np.pi / 2, f)
        loop = seg[:n].copy()
        loop[:f] = seg[:f] * np.sin(t) + seg[n:n + f] * np.cos(t)
        level = np.sqrt((loop * loop).mean())
        loop *= rms / max(level, 1e-9)
        peak = np.abs(loop).max()
        if peak > PEAK:
            loop *= PEAK / peak
        out = f"alley_bed_{name}.wav"
        write(OUT / out, loop)
        seam = abs(loop[0] - loop[-1])
        m["beds"][name] = {"file": out, "source": ident, "author": s.get("author", ""), "from_s": round(a / SR, 2), "seconds": round(seconds, 2),
                           "rms": round(float(np.sqrt((loop * loop).mean())), 4), "peak": round(float(np.abs(loop).max()), 3),
                           "loudest_second_over_median": round(float(best), 2), "seam_step": round(float(seam), 5), "centroid_hz": round(centroid(loop[:SR * 8]))}
        print("  %-28s %5.1f s  from fs%s at %.1f s  rms %.3f peak %.2f  loudest second / median %.2f  seam %.5f" %
              (out, seconds, ident, a / SR, m["beds"][name]["rms"], m["beds"][name]["peak"], best, seam))
    save_manifest(m)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--fetch", action="store_true")
    args = ap.parse_args()
    m = load_manifest()
    fetch(m)
    if not args.fetch:
        build(m)


main()
