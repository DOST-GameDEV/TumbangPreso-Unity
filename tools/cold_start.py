#!/usr/bin/env python3
"""
Drive the real shipped player from a clean state and prove it can reach a finished match.

WHY THIS EXISTS
---------------
Every measurement in this repository runs inside the editor, against a warm `Library`, with
whatever the last session left in `persistentDataPath`. **None of that exists on the laptop
somebody carries into a hall.** `docs/TODO.md` § 143.15: the cold-start question is whether
the ARTIFACT works with no developer state behind it, and nothing here has ever asked it.

⚠️⚠️ IT DRIVES THE .exe, NOT THE EDITOR, AND THAT IS THE ENTIRE POINT. `SceneScriptCheck`'s
header records a shipped build that hard-crashed on a map select with Core, EditMode,
PlayMode and four editor checks all green, because **the editor resolves by class name what
the player cannot**. A cold-start test that ran in the editor would be blind to exactly the
class of fault it exists to find.

HOW IT DRIVES A PLAYER WITH NO INPUT
------------------------------------
Through the switches `NetStateReport` already ships, which were built to put two real
processes on a link and compare what each believed:

    -tp-host 8910 -tp-profile host -tp-allbots -tp-netreport <file> -tp-netseconds <n>

⚠️ `-tp-allbots` IS WHAT MAKES IT MEAN ANYTHING. Without it nobody presses a key, every seat
stands still, and a process that survived doing nothing is not evidence. With it all four
seats play, so the distances, the casts and the props in the report are real numbers.

⚠️⚠️ AND `-tp-autostart` IS WHAT MAKES A MATCH HAPPEN, WHICH THE FIRST GREEN RUN OF THIS
HARNESS DID NOT DO. `docs/TODO.md` § 143.15: the run passed, its step said *"hosts a match
with four bots and finishes: PASS"*, and the state it captured said

    round           : 0
    round active    : False

**No round ever started.** The bodies moved, so the arena installed and the bots were
driving, and every assertion the harness made was about the process rather than about the
game. `tools/net_matrix.py` records this exact trap in its own source, in capitals:
*"`-tp-autostart 2` IS NOT OPTIONAL AND ITS ABSENCE IS SILENT. `-tp-host` loads the arena,
but `MatchInstaller.BuildReadyGate` opens a ready gate on any NETWORKED session, and nothing
presses through it without this switch."* One peer agreeing with itself that nothing happened
is not evidence either.

⚠️ IT IS `-tp-autostart 1` HERE AND NOT 2. The switch counts SEATED peers
(`LobbySession.PlayingPeerCount`), and a solo all-bots host is one of them.

⚠️⚠️ SO THE REPORT NOW SEPARATES TWO CLAIMS THAT USED TO BE ONE ROW. "The process launched,
reached the arena and exited cleanly" and "a match played" are different findings, and the
first was being printed under the second's name. Both are asserted; only the second one is
what a cold start is for.

⚠️⚠️ THE PROFILE IS NOT WIPED BY DEFAULT AND THAT IS DELIBERATE. Clearing
`persistentDataPath` destroys the settings, rebinds and career of whoever runs this, and on
this machine that is a person whose `Fullscreen` is false because he plays in a short wide
window (`CLAUDE.md` § 6.2b). `--clean-profile` moves the folder aside and puts it back, and
it is opt-in for that reason. **A truly clean MACHINE is a human test and stays in
`Attention.md`**; this is everything up to that.

USAGE
-----
  python tools/cold_start.py
  python tools/cold_start.py --clean-profile --seconds 60
"""

import argparse
import datetime
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import time

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

ROOT = pathlib.Path(__file__).resolve().parent.parent
LOGS = ROOT / "Logs"
REPORTS = ROOT / "docs" / "reports"

COMPANY = "BH Studios"
PRODUCT = "Tumbang Preso"


def head_sha():
    r = subprocess.run(["git", "rev-parse", "HEAD"], cwd=str(ROOT),
                       capture_output=True, text=True)
    return r.stdout.strip() if r.returncode == 0 else "UNKNOWN"


def player_path():
    """
    The shipped player on THIS machine, whichever platform it is.

    ⚠️⚠️ IT WAS WINDOWS-ONLY AND THAT IS EXACTLY THE FAULT `CLAUDE.md` § 7 WARNS ABOUT IN ITS
    OWN WORDS: *"a note that is true on one machine and written as a fact about 'here' sends
    whoever is on another one hunting."* The Mac in that table has **no Windows Standalone
    module at all**, so `GameBuilder.BuildWindows` has no target to write there and this
    harness could only ever refuse. A cold start that cannot run on the machine somebody is
    sitting at is a cold start nobody runs.

    ⚠️ THE ORDER IS THE PLATFORM'S OWN FIRST. A machine with both a Desktop .exe and a Builds
    .app has built both, and the one worth starting is the one that runs here.
    """
    home = pathlib.Path(os.path.expanduser("~"))
    root = pathlib.Path(__file__).resolve().parent.parent

    windows = home / "Desktop" / "TumbangPreso-Unity" / "TumbangPreso.exe"
    bundles = [root / "Builds" / "macOS" / "TumbangPreso.app",
               home / "Desktop" / "TumbangPreso.app"]

    def in_bundle(app):
        """
        ⚠️⚠️ THE BINARY INSIDE A .app IS NAMED AFTER `productName`, NOT AFTER THE BUNDLE, AND
        GUESSING IT IS WHAT MADE THE FIRST CROSS-PLATFORM RUN REFUSE. `ProjectSettings.asset`
        says `productName: Tumbang Preso`, **with a space**, so the executable is
        `Contents/MacOS/Tumbang Preso` while the bundle is `TumbangPreso.app`. The harness looked
        for `Contents/MacOS/TumbangPreso`, found nothing, and reported "there is no shipped player
        on this machine" at a player that had just been built successfully.

        ⚠️ SO IT IS GLOBBED RATHER THAN NAMED. A rename of `productName` cannot break this, which
        is the same argument `InputSurfaceProbe` makes about discovering screens instead of listing
        them (`CLAUDE.md` § 4a).
        """
        binaries = sorted((app / "Contents" / "MacOS").glob("*")) if app.exists() else []
        for binary in binaries:
            if binary.is_file() and os.access(binary, os.X_OK):
                return binary

        return None

    if sys.platform == "darwin":
        for app in bundles:
            found = in_bundle(app)
            if found is not None:
                return found

        return windows if windows.exists() else None

    if windows.exists():
        return windows

    for app in bundles:
        found = in_bundle(app)
        if found is not None:
            return found

    return None


def profile_dir():
    """
    Where the player keeps settings, career and social. Unity's persistentDataPath.

    ⚠️ THREE PLATFORMS, THREE ANSWERS, AND GUESSING WRONG IS DESTRUCTIVE HERE. `--clean-profile`
    MOVES this directory aside, so a path that resolves to the wrong place either does nothing
    (and the run is not cold) or moves somebody else's folder. Unity's own documented layouts
    are the authority: `%USERPROFILE%/AppData/LocalLow/<company>/<product>` on Windows,
    `~/Library/Application Support/<company>/<product>` on macOS.
    """
    home = pathlib.Path(os.path.expanduser("~"))

    if sys.platform == "darwin":
        return home / "Library" / "Application Support" / COMPANY / PRODUCT
    if os.name == "nt":
        return home / "AppData" / "LocalLow" / COMPANY / PRODUCT

    return home / ".config" / "unity3d" / COMPANY / PRODUCT


def artifact_identity(exe):
    """
    What the artifact says it is, read from the StreamingAssets stamp.

    ⚠️ A COLD START OF THE WRONG BUILD PROVES NOTHING, so this refuses before spending
    minutes launching a player from a commit nobody asked about.
    """
    # ⚠️ TWO LAYOUTS. A Windows player keeps StreamingAssets under `TumbangPreso_Data`; a macOS
    # bundle keeps it under `Contents/Resources/Data`. Reading only the first is why this
    # refused every Mac build with "the player carries no build-identity.json", which reads as
    # a missing stamp rather than as a harness that does not know where to look.
    stamp = None
    for candidate in (exe.parent / "TumbangPreso_Data" / "StreamingAssets" / "build-identity.json",
                      exe.parent.parent / "Resources" / "Data" / "StreamingAssets" / "build-identity.json"):
        if candidate.exists():
            stamp = candidate
            break

    if stamp is None:
        return None
    try:
        return json.loads(stamp.read_text(encoding="utf-8"))
    except Exception:
        return None


def run_player(exe, args, timeout, log):
    if log.exists():
        log.unlink()

    cmd = [str(exe), "-logFile", str(log)] + args
    started = time.time()
    try:
        proc = subprocess.run(cmd, cwd=str(exe.parent), timeout=timeout,
                              capture_output=True, text=True, errors="replace")
        code = proc.returncode
    except subprocess.TimeoutExpired:
        # ⚠️ A TIMEOUT IS A RESULT AND NOT AN ERROR HERE. A player that never exits is exactly
        # the failure a cold start is looking for, and it has to be reported rather than raised.
        code = None

    return {"seconds": round(time.time() - started, 1), "exit": code,
            "log": str(log), "log_bytes": log.stat().st_size if log.exists() else 0}


def read_state(report):
    """
    What the player believed at exit, out of its own `NetStateReport`.

    ⚠️⚠️ IT READS `round` AND `round active` BECAUSE THOSE ARE THE TWO FIELDS THE FIRST GREEN
    RUN CONTRADICTED ITSELF ON. `docs/TODO.md` § 143.15: the step claimed a match was hosted
    and finished, and the capture beside it read `round: 0` / `round active: False`. Parsing
    the report the harness was already writing is all that was ever needed; nothing read it.

    ⚠️ AND THE PER-SEAT DISTANCES, because "a round is active" and "anybody is playing" are
    also two claims. Four seats standing still inside a live round is `-tp-allbots` not having
    taken, which the report can see and a duration cannot.
    """
    if report is None or not report.exists():
        return None

    text = report.read_text(encoding="utf-8", errors="replace")
    out = {}

    for key, pattern in (("role", r"role\s*:\s*(\S+)"),
                         ("networked", r"networked\s*:\s*(\S+)"),
                         ("protocol", r"protocol\s*:\s*(\d+)"),
                         ("map", r"map\s*:\s*(\S+)"),
                         ("sampled", r"sampled\s*:\s*([\d.]+)"),
                         ("active", r"round active\s*:\s*(\S+)"),
                         ("defender", r"defender\s*:\s*(-?\d+)")):
        m = re.search(pattern, text)
        out[key] = m.group(1) if m else None

    m = re.search(r"^round\s*:\s*(-?\d+)", text, re.MULTILINE)
    out["round"] = int(m.group(1)) if m else None

    out["seats"] = []
    for m in re.finditer(
            r"^(\d)\s+(-?\d+)\s+(True|False)\s+(True|False)\s+(-?\d+)\s+([\d.]+)\s+(\d+)\s+(\d+)\s*$",
            text, re.MULTILINE):
        out["seats"].append({
            "seat": int(m.group(1)),
            "bot": m.group(3) == "True",
            "taya": m.group(4) == "True",
            "score": int(m.group(5)),
            "travelled": float(m.group(6)),
        })

    return out


def read_log_faults(log):
    """Exceptions and errors the player wrote, which is the half a report file cannot carry."""
    if not log.exists():
        return ["the player wrote no log at all"]

    text = log.read_text(encoding="utf-8", errors="replace")
    faults = []
    for line in text.splitlines():
        if re.search(r"\b(Exception|NullReference|Assertion failed|Fatal)\b", line):
            faults.append(line.strip()[:200])
    return faults[:40]


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--clean-profile", action="store_true",
                    help="move persistentDataPath aside first and restore it afterwards")
    ap.add_argument("--seconds", type=int, default=45, help="how long the driven match runs")
    args = ap.parse_args()

    sha = head_sha()
    exe = player_path()

    steps = []
    LOGS.mkdir(exist_ok=True)

    if exe is None:
        builder = "GameBuilder.BuildMac" if sys.platform == "darwin" else "GameBuilder.BuildWindows"
        print("REFUSED: there is no shipped player on this machine to cold start.\n"
              f"         Build one first: {builder}.", file=sys.stderr)
        return 2

    identity = artifact_identity(exe)
    if identity is None:
        print("REFUSED: the player carries no build-identity.json, so it predates the stamp "
              "and cannot be tied to a commit. Rebuild through GameBuilder.", file=sys.stderr)
        return 2

    if identity.get("sha") != sha:
        print(f"REFUSED: the player was built from {(identity.get('sha') or '?')[:12]} and HEAD "
              f"is {sha[:12]}. A cold start of a different commit proves nothing about this one.",
              file=sys.stderr)
        return 2

    moved = None
    profile = profile_dir()

    try:
        if args.clean_profile and profile.exists():
            moved = profile.with_name(profile.name + ".coldstart-backup")
            if moved.exists():
                shutil.rmtree(moved)
            shutil.move(str(profile), str(moved))
            print(f"  profile moved aside: {profile} -> {moved.name}")

        # ---- 1. does it launch at all, and can it say what it is ----------
        print("  1. identity ...", flush=True)
        step = run_player(exe, ["-tp-identity", "-batchmode", "-nographics"],
                          timeout=180, log=LOGS / "coldstart-identity.log")
        step["name"] = "launches and identifies itself"
        step["faults"] = read_log_faults(pathlib.Path(step["log"]))
        step["ok"] = step["log_bytes"] > 0 and not step["faults"]
        steps.append(step)

        # ---- 2. host a whole match with four bots, from nothing -----------
        print(f"  2. hosting a driven match for {args.seconds}s ...", flush=True)
        report = LOGS / "coldstart-netstate.txt"
        if report.exists():
            report.unlink()

        # ⚠️⚠️ `-tp-autostart 1` IS NOT OPTIONAL AND ITS ABSENCE IS SILENT. See the header:
        # without it `MatchInstaller.BuildReadyGate` opens a gate on this networked session and
        # nothing presses through it, so the process loads the arena, installs four bots that
        # wander, holds for the full duration and exits cleanly having played nothing.
        #
        # ⚠️ THE COUNT IS 1 BECAUSE `LobbySession.PlayingPeerCount` COUNTS SEATED PEERS AND
        # THERE IS ONE PROCESS. `net_matrix` passes 2 because it has two.
        step = run_player(exe, ["-tp-host", "8910", "-tp-profile", "coldstart", "-tp-allbots",
                                "-tp-autostart", "1",
                                "-tp-netreport", str(report),
                                "-tp-netseconds", str(args.seconds)],
                          timeout=args.seconds + 240,
                          log=LOGS / "coldstart-match.log")
        step["name"] = "reaches the arena and exits cleanly"
        step["faults"] = read_log_faults(pathlib.Path(step["log"]))
        step["report_written"] = report.exists()
        step["ok"] = report.exists() and not step["faults"]
        step["detail"] = ("clean" if step["ok"]
                          else "; ".join(step["faults"][:3]) or "no report written")
        steps.append(step)

        # ---- 3. did a match actually PLAY --------------------------------
        #
        # ⚠️⚠️ THIS IS A SEPARATE STEP BECAUSE IT IS A SEPARATE CLAIM, AND CONFLATING THE TWO IS
        # THE WHOLE OF `docs/TODO.md` § 143.15. The first green run of this harness printed
        # "hosts a match with four bots and finishes: PASS" beside a capture reading `round: 0`
        # and `round active: False`, and every assertion it made was about the PROCESS. A row
        # that can only be green is not a measurement.
        state = read_state(report)
        match_step = {
            "name": "a real round became active",
            "seconds": 0.0,
            "faults": [],
            "state": state,
        }

        if state is None:
            match_step["ok"] = False
            match_step["detail"] = ("no NetStateReport was written at all, so nothing is known "
                                    "about what the match did")
        else:
            reasons = []

            if state.get("round") is None or state["round"] < 1:
                reasons.append(f"round is {state.get('round')}, so no round ever started")

            if state.get("active") != "True":
                reasons.append(f"round active is {state.get('active')} at exit")

            if state.get("networked") != "True":
                reasons.append(f"networked is {state.get('networked')}: the ready gate this "
                               f"switch presses through only exists on a networked session")

            # ⚠️⚠️ NOT `moved`. THAT NAME BELONGS TO THE PROFILE BACKUP AND SHADOWING IT DELETED
            # SOMEBODY'S SETTINGS. `moved` is the `finally` block's flag for "a profile directory
            # was set aside and has to be put back"; a list of seats bound to it made that block
            # think a backup existed, so it ran `shutil.rmtree(profile)` on a profile it had never
            # moved and then failed trying to restore a Python list. **The destructive half ran
            # first.** See the guard in the `finally` below, which is what makes the class of
            # fault impossible rather than this rename, which only fixes the instance.
            driving = [s for s in state.get("seats", []) if s["travelled"] > 1.0]
            if len(driving) < 2:
                reasons.append(f"only {len(driving)} seat(s) travelled more than a metre, so the "
                               f"bots were not driving")

            match_step["ok"] = not reasons
            match_step["detail"] = "; ".join(reasons) if reasons else (
                f"round {state['round']}, active, "
                f"{len(driving)} seats driving, {state.get('sampled')} s sampled")

        steps.append(match_step)

        if report.exists():
            steps[1]["report_head"] = "\n".join(
                report.read_text(encoding="utf-8", errors="replace").splitlines()[:20])

    finally:
        # ⚠️⚠️ RESTORED IN A `finally`, ALWAYS. A crash between the move and the restore would
        # otherwise leave somebody's settings, rebinds and career sitting under a `.backup` name
        # they have no reason to look for.
        #
        # ⚠️⚠️ AND IT PROVES IT IS LOOKING AT THE BACKUP IT MADE BEFORE IT DELETES ANYTHING, WHICH
        # IS NOT PARANOIA: A SHADOWED VARIABLE MADE THIS BLOCK DESTROY A REAL PROFILE. A later
        # step bound a list of seat rows to `moved`, this block read that as "a backup exists",
        # ran `rmtree` on a profile it had never set aside, and only THEN failed trying to restore
        # a Python list. **The destructive half ran first**, which is the property that makes an
        # unguarded `rmtree` in a `finally` a bad shape however careful the surrounding code is.
        #
        # ⚠️ SO THE CONDITION IS THE THING ITSELF RATHER THAN A FLAG ABOUT IT: a real path, with
        # the name this function chose, that exists on disk. Nothing else can satisfy it, and a
        # `rmtree` guarded on the SOURCE existing cannot destroy a destination it cannot replace.
        expected = profile.with_name(profile.name + ".coldstart-backup")
        restorable = (isinstance(moved, pathlib.Path)
                      and moved == expected
                      and moved.exists())

        if moved is not None and not restorable:
            print(f"REFUSED to restore: the backup handle is {moved!r}, which is not "
                  f"{expected}. Nothing was deleted.", file=sys.stderr)
        elif restorable:
            if profile.exists():
                shutil.rmtree(profile)
            shutil.move(str(moved), str(profile))
            print(f"  profile restored: {profile}")

    ok = all(s["ok"] for s in steps)

    lines = []
    lines.append("# Cold start of the shipped player")
    lines.append("")
    lines.append(f"- **Commit** `{sha}`")
    lines.append(f"- **Artifact** `{exe}`")
    lines.append(f"- **Built** {identity.get('builtAt')}, protocol {identity.get('protocol')}, "
                 f"{identity.get('target')}")
    lines.append(f"- **Profile cleared** {'yes' if args.clean_profile else 'NO (opt in with --clean-profile)'}")
    lines.append(f"- **Generated** {datetime.datetime.now().isoformat(timespec='seconds')}")
    lines.append("")
    lines.append(f"## Verdict: {'PASS' if ok else 'FAIL'}")
    lines.append("")
    lines.append("| Step | Verdict | Seconds | Detail |")
    lines.append("|---|---|---|---|")
    for s in steps:
        detail = s.get("detail")
        if detail is None:
            detail = "clean" if s["ok"] else "; ".join(s["faults"][:3]) or "no report written"
        lines.append(f"| {s['name']} | {'PASS' if s['ok'] else '**FAIL**'} | {s['seconds']} | {detail} |")
    lines.append("")

    for s in steps:
        if s.get("report_head"):
            lines.append("### What the player believed at exit")
            lines.append("")
            lines.append("```")
            lines.append(s["report_head"])
            lines.append("```")
            lines.append("")

    lines.append("⚠️⚠️ **THE LAST TWO ROWS ARE DIFFERENT CLAIMS AND USED TO BE ONE.** *\"Reaches "
                 "the arena and exits cleanly\"* is about the PROCESS: it launched from a cleared "
                 "profile, identified itself, loaded a map, installed four bots and came back. "
                 "*\"A real round became active\"* is about the GAME. `docs/TODO.md` § 143.15 is "
                 "a green run of the first printed under the second's name, with `round: 0` in "
                 "its own capture.")
    lines.append("")
    lines.append("⚠️ **A truly clean MACHINE is still a human test.** This clears the profile at "
                 "most; it cannot clear a driver, a firewall rule, a codec or a Visual C++ "
                 "runtime that this machine has and a borrowed one does not. `Attention.md`.")

    REPORTS.mkdir(parents=True, exist_ok=True)
    out = REPORTS / f"cold-start-{sha[:12]}.md"
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")

    print("\n".join(lines))
    print(f"\nwritten: {out}")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
