#!/usr/bin/env python3
"""
The bot probe across several seeds, so a number from it can mean something.

WHY THIS EXISTS
---------------
`CLAUDE.md` § 7.1 is unusually blunt about `BotBehaviourProbe`:

    "ITS NUMBERS ARE LIVENESS FLOORS, NEVER COMPARISONS AT n = 1. ... eight matches at
    the shipped settings spread from 58 to 100 throws, about 20 per cent, and two runs
    of one build with one seed are still not identical."

and `docs/TODO.md` § 16 carries the arithmetic for how many runs an arm of an A/B has to
buy before its answer means anything (three, for anything worth 20 per cent).

**Nothing automated that.** Every balance argument in this repository that cites the probe
cites ONE run, and the entry that says a fixed step solved the noise (§ 10) is contradicted
by the measurement that says it did not (§ 16). So this runs the probe across a list of
seeds and reports the SPREAD, which is the only honest way to quote it.

⚠️⚠️ THE DEFAULT SEED IS NOT TOUCHED AND MUST NOT BE. `BotBehaviourProbe` hard-codes
20260823 and its own note says why: *"a seed picked to make a red run green is a
measurement of nothing."* This passes `-tp-bot-seed` per run, which the probe reads only
when it is given, so the gate keeps measuring exactly what it measured yesterday. **Varying
the seed deliberately to measure noise and varying it to make a number look better are
opposite acts, and only the first one is done here.**

⚠️ THIS IS NOT PART OF THE QUALIFICATION GATE. It is minutes per seed, and a spread is
evidence for a decision rather than a pass or a fail. `tools/qualify.py` does not run it.

USAGE
-----
  python tools/bot_sweep.py --seeds 5
  python tools/bot_sweep.py --seeds 8 --mode Classic --map Eskinita
"""

import argparse
import datetime
import hashlib
import json
import math
import os
import pathlib
import re
import statistics
import subprocess
import sys

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

ROOT = pathlib.Path(__file__).resolve().parent.parent
LOGS = ROOT / "Logs"
REPORTS = ROOT / "docs" / "reports"

# ⚠️⚠️ RESOLVED PER MACHINE. `CLAUDE.md` § 7's table has three machines and two of them are not
# Windows; this was a Windows literal, so on the Mac every seed launched a path that does not
# exist and the sweep reported "no report written" for all of them, which reads as the PROBE
# failing rather than as the harness looking in the wrong place.
UNITY_VERSION = "6000.5.8f1"

UNITY = next(
    (pathlib.Path(c) for c in (
        rf"C:\Program Files\Unity\Hub\Editor\{UNITY_VERSION}\Editor\Unity.exe",
        f"/Applications/Unity/Hub/Editor/{UNITY_VERSION}/Unity.app/Contents/MacOS/Unity",
        os.path.expanduser(
            f"~/Applications/Unity/Hub/Editor/{UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"),
    ) if pathlib.Path(c).exists()),
    pathlib.Path("Unity"))

BUILD_TARGET = "OSXUniversal" if sys.platform == "darwin" else "Win64"

# ⚠️⚠️ THE FILES A SWEEP IS A MEASUREMENT **OF**. A spread is only comparable against another
# spread taken with the same numbers in the game, and "the same commit" is too coarse: a docs
# commit changes the SHA and nothing else, and a session that runs a sweep, edits `Balance.cs`
# and runs another has two sweeps at two SHAs and no way to say which difference is which.
# Fingerprinting the tuning files means two sweeps carrying the same digest were measuring the
# same game whatever the SHA says.
CONFIG_FILES = [
    "Packages/com.tumbangpreso.core/Runtime/Balance.cs",
    "Packages/com.tumbangpreso.core/Runtime/AiTuning.cs",
    "Packages/com.tumbangpreso.core/Runtime/MatchRules.cs",
    "Packages/com.tumbangpreso.core/Runtime/ThrowRules.cs",
]

# ⚠️ THE SEEDS ARE FIXED AND WRITTEN DOWN RATHER THAN RANDOM. A sweep whose inputs change
# every run cannot be compared against yesterday's sweep, which is the whole point of
# measuring a spread. Adding a seed is fine; changing one silently is not.
SEEDS = [20260823, 1, 7, 4242, 20260904, 99991, 31337, 5150]

METRICS = {
    "lata knocks": re.compile(r"lata knocks\s+(\d+)"),
    "tags": re.compile(r"tags\s+(\d+)"),
    "sabotages": re.compile(r"sabotages\s+(\d+)"),
    "throws": re.compile(r"throws\s+(\d+)"),
    "retrievals": re.compile(r"retrievals\s+(\d+)"),
    "camp penalties": re.compile(r"camp penalties\s+(\d+)"),
    "idle penalties": re.compile(r"idle penalties\s+(\d+)"),
    "skill uses": re.compile(r"skill uses\s+(\d+)"),
    "ultimate uses": re.compile(r"ultimate uses\s+(\d+)"),
}


def head_sha():
    r = subprocess.run(["git", "rev-parse", "HEAD"], cwd=str(ROOT),
                       capture_output=True, text=True)
    return r.stdout.strip() if r.returncode == 0 else "UNKNOWN"


def config_digest():
    """
    A short digest of the files that decide how the game plays.

    ⚠️ IT IS THE SOURCE TEXT AND NOT THE PARSED NUMBERS, deliberately: parsing would need this
    script to know which constants matter, which is a list that goes stale exactly the way
    `CLAUDE.md` § 7.1's audit count did. A comment-only edit therefore moves the digest, which
    is a false alarm in the safe direction: it says "these two sweeps may not be comparable"
    when they are, rather than the reverse.
    """
    h = hashlib.sha256()
    for relative in CONFIG_FILES:
        path = ROOT / relative
        h.update(relative.encode("utf-8"))
        h.update(path.read_bytes() if path.exists() else b"MISSING")

    return h.hexdigest()[:12]


def run_probe(seed, mode, map_name):
    """One Unity launch, one seed. Returns the parsed report or None."""
    report = LOGS / f"bot-behaviour-{mode}-{map_name}.txt"
    if report.exists():
        report.unlink()

    cmd = [str(UNITY), "-batchmode", "-runTests", "-projectPath", str(ROOT),
           "-buildTarget", BUILD_TARGET, "-testPlatform", "PlayMode",
           "-testCategory", "!WallClock;!ThumbFloor",
           "-testFilter", "TumbangPreso.PlayTests.BotBehaviourProbe",
           "-testResults", str(LOGS / f"bot-sweep-{seed}.xml"),
           "-logFile", str(LOGS / f"bot-sweep-{seed}.log"),
           "-tp-bot-seed", str(seed)]

    subprocess.run(cmd, cwd=str(ROOT), capture_output=True, text=True, errors="replace")

    if not report.exists():
        return None

    text = report.read_text(encoding="utf-8", errors="replace")
    row = {"seed": seed}
    for name, pattern in METRICS.items():
        m = pattern.search(text)
        row[name] = int(m.group(1)) if m else None
    return row


def summarise(rows, name):
    values = [r[name] for r in rows if r.get(name) is not None]
    if not values:
        return None

    out = {
        "n": len(values),
        "min": min(values),
        "max": max(values),
        "mean": round(statistics.mean(values), 1),
        "median": statistics.median(values),
    }
    out["stdev"] = round(statistics.pstdev(values), 1) if len(values) > 1 else 0.0

    # ⚠️ THE SPREAD IS THE HEADLINE, NOT THE MEAN. `CLAUDE.md` § 7.1 quotes "58 to 100
    # throws, about 20 per cent" as the reason a single run cannot be compared, so the
    # number a reader needs first is how wide the band is relative to the middle of it.
    out["spread_pct"] = (round(100.0 * (out["max"] - out["min"]) / out["mean"], 1)
                         if out["mean"] else 0.0)
    return out


def compare(before, after):
    """
    Whether a change moved anything past the noise, per metric.

    ⚠️⚠️ THIS IS THE QUESTION THE SWEEP EXISTS TO ANSWER AND IT COULD NOT ANSWER IT. A spread
    tells a reader that one run means nothing; it does not tell them whether TWO SETS of runs
    differ. `docs/TODO.md` § 16 carries the arithmetic and the sweep carried the inputs, and
    the join between them was a person doing it in their head, which is exactly where "58 to
    100 throws" gets read as "the change made it worse".
    
    ⚠️⚠️ WELCH'S t, NOT A PERCENTAGE, AND THE DIFFERENCE MATTERS HERE. A 20 per cent move on a
    metric whose own spread is 20 per cent is nothing; the same move on one that never varies is
    everything, and a percentage cannot tell those apart. Welch's form is the one that does not
    assume the two arms have the same variance, which is the whole point of comparing a retuned
    game against a shipped one.
    
    ⚠️⚠️ AND IT REPORTS THREE VERDICTS RATHER THAN A BOOLEAN, because "not measured" and "not
    different" are opposite findings that a p-value alone conflates. **UNDER-SAMPLED** is what
    two runs an arm earns: § 16's own answer is three for anything worth 20 per cent, and a
    t-test on n=2 is arithmetic performed on nothing.
    
    ⚠️ NO SciPy AND NO TABLE. |t| >= 2.0 is roughly the two-sided 5 per cent point for the
    handful of degrees of freedom a sweep of five to eight seeds has, and stating the STATISTIC
    beside the verdict means a reader who wants a real threshold has the number to apply it to.
    Importing a stats package into a repository whose gate is `python` on a venue laptop is a
    trade this file will not make.
    """
    rows = []

    for name in METRICS:
        a = [r[name] for r in before.get("runs", []) if r.get(name) is not None]
        b = [r[name] for r in after.get("runs", []) if r.get(name) is not None]

        row = {"metric": name, "n_before": len(a), "n_after": len(b)}

        if len(a) < 2 or len(b) < 2:
            row["verdict"] = "UNDER-SAMPLED"
            row["detail"] = "fewer than two runs on one side"
            rows.append(row)
            continue

        mean_a, mean_b = statistics.mean(a), statistics.mean(b)
        var_a = statistics.variance(a)
        var_b = statistics.variance(b)

        row["mean_before"] = round(mean_a, 1)
        row["mean_after"] = round(mean_b, 1)
        row["change_pct"] = round(100.0 * (mean_b - mean_a) / mean_a, 1) if mean_a else 0.0

        pooled = math.sqrt((var_a / len(a)) + (var_b / len(b)))
        row["t"] = round((mean_b - mean_a) / pooled, 2) if pooled > 0.0 else 0.0

        if len(a) < 3 or len(b) < 3:
            # § 16: three runs an arm for anything worth 20 per cent. Two is a direction.
            row["verdict"] = "UNDER-SAMPLED"
            row["detail"] = "two runs an arm is a direction, not a measurement (§ 16 says three)"
        elif abs(row["t"]) >= 2.0:
            row["verdict"] = "MOVED"
            row["detail"] = f"{row['change_pct']:+.1f}% at |t| {abs(row['t']):.2f}"
        else:
            row["verdict"] = "no change measured"
            row["detail"] = (f"{row['change_pct']:+.1f}% is inside the noise "
                             f"(|t| {abs(row['t']):.2f} < 2.0)")

        rows.append(row)

    return rows


def report_comparison(before, after, rows):
    lines = []
    lines.append("# Did the change move anything?")
    lines.append("")
    lines.append(f"- **Before** `{before.get('sha', '?')[:12]}` config "
                 f"`{before.get('config', 'unknown')}`, {len(before.get('runs', []))} run(s)")
    lines.append(f"- **After**  `{after.get('sha', '?')[:12]}` config "
                 f"`{after.get('config', 'unknown')}`, {len(after.get('runs', []))} run(s)")
    lines.append(f"- **Mode** {after.get('mode')} on {after.get('map')}")
    lines.append("")

    if before.get("config") == after.get("config"):
        lines.append("⚠️⚠️ **THE TWO SWEEPS CARRY THE SAME CONFIG DIGEST**, so `Balance.cs`, "
                     "`AiTuning.cs`, `MatchRules.cs` and `ThrowRules.cs` are byte-identical "
                     "between them. Any difference below is the noise floor being measured "
                     "twice, which is a useful thing to know and is not a balance finding.")
        lines.append("")

    if before.get("mode") != after.get("mode") or before.get("map") != after.get("map"):
        lines.append(f"⚠️⚠️ **THESE TWO SWEEPS ARE NOT THE SAME ARM.** Before is "
                     f"{before.get('mode')} on {before.get('map')} and after is "
                     f"{after.get('mode')} on {after.get('map')}. Nothing below is a comparison.")
        lines.append("")

    common = set(r["seed"] for r in before.get("runs", [])) & \
             set(r["seed"] for r in after.get("runs", []))
    lines.append(f"- **Seeds in common** {len(common)}: "
                 f"{', '.join(str(s) for s in sorted(common)) or 'none'}")
    lines.append("")
    lines.append("⚠️ **A SEED IS ONLY COMPARABLE AGAINST ITSELF.** `BotBehaviourProbe` is seeded "
                 "and `CLAUDE.md` § 7.1 forbids changing the seed to make a run pass; two sweeps "
                 "over different seed sets are two different questions.")
    lines.append("")
    lines.append("| Metric | before | after | change | t | verdict |")
    lines.append("|---|---|---|---|---|---|")

    for r in rows:
        if "mean_before" not in r:
            lines.append(f"| {r['metric']} | - | - | - | - | **{r['verdict']}**: {r['detail']} |")
            continue

        lines.append(f"| {r['metric']} | {r['mean_before']} | {r['mean_after']} | "
                     f"{r['change_pct']:+.1f}% | {r['t']:+.2f} | "
                     f"{'**MOVED**' if r['verdict'] == 'MOVED' else r['verdict']} |")

    lines.append("")
    lines.append("⚠️⚠️ **`MOVED` IS NOT `WORSE` AND THIS FILE DOES NOT KNOW THE DIFFERENCE.** It "
                 "reports that a number changed by more than the noise; whether more throws and "
                 "fewer tags is the game anybody wants is a design judgement and belongs to a "
                 "person. `docs/VISION.md` § 2 is where that argument is had.")
    lines.append("")
    lines.append("⚠️ **`no change measured` IS NOT `NO CHANGE`.** With five seeds an arm this "
                 "cannot see a move smaller than roughly the spread; a real but small effect "
                 "reads exactly like nothing. Buy more seeds before concluding a change did "
                 "nothing.")

    return lines


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--seeds", type=int, default=5, help="how many of the fixed seeds to run")
    ap.add_argument("--mode", default="HeroStrike", choices=["Classic", "HeroStrike"])
    ap.add_argument("--map", dest="map_name", default="Eskinita")
    ap.add_argument("--out", help="where to write the sweep json (default Logs/bot-sweep.json)")
    ap.add_argument("--compare", nargs=2, metavar=("BEFORE.json", "AFTER.json"),
                    help="answer 'did this change move anything past the noise' from two sweeps")
    args = ap.parse_args()

    if args.compare:
        before = json.loads(pathlib.Path(args.compare[0]).read_text(encoding="utf-8"))
        after = json.loads(pathlib.Path(args.compare[1]).read_text(encoding="utf-8"))

        rows = compare(before, after)
        lines = report_comparison(before, after, rows)

        REPORTS.mkdir(parents=True, exist_ok=True)
        out = REPORTS / f"bot-sweep-compare-{after.get('sha', 'unknown')[:12]}.md"
        out.write_text("\n".join(lines) + "\n", encoding="utf-8")

        print("\n".join(lines))
        print(f"\nwritten: {out}")
        return 0

    seeds = SEEDS[:max(1, min(args.seeds, len(SEEDS)))]
    rows = []

    for seed in seeds:
        print(f"  seed {seed} ...", flush=True)
        row = run_probe(seed, args.mode, args.map_name)
        if row is None:
            print(f"    no report written; the run did not reach the probe")
            continue
        rows.append(row)
        print("    " + ", ".join(f"{k} {v}" for k, v in row.items() if k != "seed"))

    if not rows:
        print("no runs produced a report.", file=sys.stderr)
        return 1

    sha = head_sha()
    config = config_digest()
    lines = []
    lines.append("# Bot behaviour across seeds")
    lines.append("")
    lines.append(f"- **Commit** `{sha}`")
    lines.append(f"- **Config digest** `{config}` "
                 f"({', '.join(pathlib.Path(f).name for f in CONFIG_FILES)})")
    lines.append(f"- **Generated** {datetime.datetime.now().isoformat(timespec='seconds')}")
    lines.append(f"- **Mode** {args.mode} on {args.map_name}")
    lines.append(f"- **Seeds** {', '.join(str(r['seed']) for r in rows)}")
    lines.append("")
    lines.append("⚠️⚠️ **READ THE SPREAD BEFORE THE MEAN.** `CLAUDE.md` § 7.1: these numbers are "
                 "liveness floors, never comparisons at n = 1. A change worth less than the "
                 "spread below has not been measured by this probe, however different the two "
                 "runs look.")
    lines.append("")
    lines.append("| Metric | n | min | max | mean | median | stdev | spread |")
    lines.append("|---|---|---|---|---|---|---|---|")

    for name in METRICS:
        s = summarise(rows, name)
        if s is None:
            continue
        lines.append(f"| {name} | {s['n']} | {s['min']} | {s['max']} | {s['mean']} | "
                     f"{s['median']} | {s['stdev']} | **{s['spread_pct']}%** |")

    lines.append("")
    lines.append("## Every run")
    lines.append("")
    header = ["seed"] + list(METRICS)
    lines.append("| " + " | ".join(header) + " |")
    lines.append("|" + "---|" * len(header))
    for r in rows:
        lines.append("| " + " | ".join(str(r.get(h, "")) for h in header) + " |")

    REPORTS.mkdir(parents=True, exist_ok=True)
    out = REPORTS / f"bot-sweep-{sha[:12]}.md"
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")

    payload = {
        "sha": sha,
        # ⚠️ THE CONFIG DIGEST IS WHAT MAKES TWO SWEEPS COMPARABLE, NOT THE SHA. See
        # `config_digest`: a docs commit moves the SHA and changes nothing about the game.
        "config": config,
        "mode": args.mode,
        "map": args.map_name,
        "generated": datetime.datetime.now().isoformat(timespec="seconds"),
        "runs": rows,
        "summary": {n: summarise(rows, n) for n in METRICS},
    }

    destination = pathlib.Path(args.out) if args.out else (LOGS / "bot-sweep.json")
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(payload, indent=2), encoding="utf-8")

    print("\n".join(lines))
    print(f"\nwritten: {out}")
    print(f"          {destination}")
    print()
    print("⚠️ To answer 'did a change move anything', keep this json and run the sweep again "
          "after the change, then:")
    print(f"    python tools/bot_sweep.py --compare {destination} <the-new-one>.json")
    return 0


if __name__ == "__main__":
    sys.exit(main())
