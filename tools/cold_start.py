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

⚠️⚠️ AND A GREEN SMOKE RUN IS NOT A NATIONALS COLD START, WHICH IS WHAT `--tournament` IS FOR.
`docs/TODO.md` § 145.8. The recorded green run at `87346b8` proved a shipped player launched,
reached round 1, moved four seats and scored, and it is a **macOS player playing HERO STRIKE**.
`docs/VISION.md` § 1.1 says in as many words that **CLASSIC is the tournament ruleset**, so that
run is useful general coverage and is not evidence about the path a bracket match takes.

    python tools/cold_start.py --tournament    the CLASSIC preset, asserted rather than assumed
    python tools/cold_start.py --nationals     that, plus the release artifact's own identity

⚠️ `--tournament` DROPS `-tp-allbots` ON PURPOSE. `GameLaunch.AllBots` is one of the switches
`TournamentPreset.Modifiers` forbids in a bracket match, so a tournament cold start that set it
would be asserting the preset while breaking it. Without it seat 0 is a person standing still
and the other three are bots that drive, which is a live round with every modifier at its safe
value at once.

⚠️⚠️ THE SMOKE PATH IS KEPT AND IS NOT REDUNDANT. It is the only row that exercises four DRIVEN
seats, and `-tp-allbots` is the switch that gets a fourth body moving. Deleting it to make the
tool tidier would trade coverage for a shorter argument list.

USAGE
-----
  python tools/cold_start.py
  python tools/cold_start.py --clean-profile --seconds 60
  python tools/cold_start.py --nationals --clean-profile
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
import uuid

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

ROOT = pathlib.Path(__file__).resolve().parent.parent
LOGS = ROOT / "Logs"
REPORTS = ROOT / "docs" / "reports"

COMPANY = "BH Studios"
PRODUCT = "Tumbang Preso"

# ⚠️⚠️ THE NATIONALS PLAYER IS THE WINDOWS ONE AND THIS FILE SAYS SO RATHER THAN ASSUMING IT.
# `CLAUDE.md` § 7: the Mac in that table has no Windows Standalone module at all, so a cold start
# that runs there is a development smoke test and not a certification. `--nationals` refuses any
# other target by name instead of quietly certifying a build nobody ships.
NATIONALS_TARGET = "StandaloneWindows64"

# ⚠️ THE ONE MODIFIER A DRIVEN COLD START IS ALLOWED TO LEAVE SET, AND IT IS THE EMPTY SET.
# `--tournament` runs WITHOUT `-tp-allbots` precisely so this can be empty: the local seat is a
# human standing still and the three unoccupied seats are bots that drive, which is a legal
# tournament configuration for a one-machine test rather than a switch the guard has to forgive.
# ⚠️ A LIST WITH ANYTHING IN IT WOULD BE THIS GATE FORGIVING ITSELF. `docs/TODO.md` § 145.3's
# whole argument is that a modifier nobody declared is the hazard; a cold start that declared its
# own exemption would be the same hole one level up.
TOURNAMENT_ALLOWED_MODIFIERS = frozenset()


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


# ---------------------------------------------------------------------------
# SETTING A REAL PROFILE ASIDE, WHICH THIS HARNESS HAS ALREADY DESTROYED ONE OF
#
# WARNING: THE FIRST FAULT WAS A SHADOWED VARIABLE AND IT DELETED A REAL MAC PROFILE. A later
# step bound a list of seat rows to `moved`, the `finally` read that as "a backup exists", ran
# `shutil.rmtree` on a profile it had never set aside, and only THEN failed trying to restore a
# Python list. The destructive half ran first. That instance is fixed.
#
# WARNING: THE SECOND FAULT IS THE ONE THIS CLASS EXISTS FOR AND IT SURVIVED THAT FIX. THE
# BACKUP PATH WAS A CONSTANT AND THE RESTORE PATH DELETED WHATEVER WAS ALREADY ON IT. A
# pre-existing `<profile>.coldstart-backup` is not junk: it is what a previous crash, an
# interrupted restore, a power cut or a half-finished manual recovery leaves behind, and in
# every one of those cases it is the ONLY copy of somebody's settings, rebinds and career there
# is. `rmtree(moved)` on it, followed by moving the live profile into its place, destroys the
# good copy and keeps the one this harness just made.
#
# WARNING: SO THERE ARE TWO PROPERTIES HERE AND NEITHER OF THEM IS "BE CAREFUL":
#
#   1. A backup directory is UNIQUE PER RUN. Timestamp plus a GUID, so nothing this class
#      creates can collide with anything anybody left behind, and a stale backup from a crashed
#      run is simply never touched again.
#   2. `rmtree` is NEVER pointed at the live profile or at a backup. The live profile is MOVED
#      to a discard path and deleted only after the restore has already succeeded, so a failure
#      at any point leaves two recoverable copies on disk and an error naming both.
#
# WARNING: AND THE RESTORE METADATA IS WRITTEN INSIDE THE BACKUP. A directory named
# `Tumbang Preso.coldstart-backup-20260905-081500-3f9ac1b2` says nothing to whoever finds it in
# six weeks; `.coldstart-restore.json` inside it names the path it came from and when.
# ---------------------------------------------------------------------------

BACKUP_PREFIX = ".coldstart-backup-"
DISCARD_PREFIX = ".coldstart-discard-"
RESTORE_NOTE = ".coldstart-restore.json"


def _stamp():
    return datetime.datetime.now().strftime("%Y%m%d-%H%M%S") + "-" + uuid.uuid4().hex[:8]


class ProfileVault:
    """
    Moves one `persistentDataPath` aside for the length of a run, and puts it back.

    WARNING: NOTHING HERE RUNS UNLESS `set_aside` WAS CALLED AND SUCCEEDED. A default run never
    constructs a backup path at all, so the whole destructive surface sits behind
    `--clean-profile` by construction rather than behind an `if` somebody has to keep correct.
    """

    def __init__(self, profile):
        self.profile = pathlib.Path(profile)
        self.backup = None

    def set_aside(self):
        """Move the profile to a fresh backup directory. Returns the backup path, or None."""
        if not self.profile.exists():
            # Nothing to do is not a failure. A machine that has never run the game has no
            # profile, and that is already the cold state this switch is trying to produce.
            return None

        backup = self.profile.with_name(self.profile.name + BACKUP_PREFIX + _stamp())

        # WARNING: FAIL CLOSED RATHER THAN OVERWRITE. A GUID collision is not credible; a path
        # that exists anyway means something about this machine is not what this class believes,
        # and the safe answer to that is to refuse rather than to pick.
        if backup.exists():
            raise RuntimeError(
                "refusing to set the profile aside: %s already exists. Nothing was moved."
                % backup)

        shutil.move(str(self.profile), str(backup))
        self.backup = backup

        try:
            (backup / RESTORE_NOTE).write_text(json.dumps({
                "original": str(self.profile),
                "created": datetime.datetime.now().isoformat(timespec="seconds"),
                "tool": "tools/cold_start.py --clean-profile",
                "restore": "move this directory back to the path named in 'original'",
            }, indent=2), encoding="utf-8")
        except Exception:
            # The note is a courtesy and the move is the fact. A read-only volume must not turn
            # a successful set-aside into an exception that leaves the profile parked under a
            # name with no explanation at all.
            pass

        return backup

    def _refusal(self):
        """Why this vault must not touch anything, or None when it is safe to restore."""
        if self.backup is None:
            return None

        if not isinstance(self.backup, pathlib.Path):
            return ("the backup handle is %r, which is not a path. Nothing was deleted."
                    % (self.backup,))

        if BACKUP_PREFIX not in self.backup.name:
            return ("the backup handle is %s, which does not carry this tool's own %s prefix. "
                    "Nothing was deleted." % (self.backup, BACKUP_PREFIX))

        if self.backup.parent != self.profile.parent:
            return ("the backup handle is %s, which is not beside the profile at %s. Nothing "
                    "was deleted." % (self.backup, self.profile))

        if not self.backup.exists():
            return ("the backup directory %s is gone: something outside this run removed it. "
                    "Nothing was deleted." % self.backup)

        return None

    def restore(self):
        """
        Put the real profile back. Returns (ok, message).

        WARNING: THE ORDER IS MOVE, MOVE, THEN DELETE, AND THAT IS THE WHOLE SAFETY ARGUMENT.
        The obvious body is `rmtree(profile); move(backup, profile)` and it has exactly one
        failure mode, which is fatal: anything that goes wrong between those two lines has
        already destroyed the live path and has not yet restored the good one.
        """
        refusal = self._refusal()
        if refusal is not None:
            return False, "REFUSED to restore: " + refusal

        if self.backup is None:
            return True, "nothing was set aside, so nothing was restored"

        discard = None
        if self.profile.exists():
            discard = self.profile.with_name(self.profile.name + DISCARD_PREFIX + _stamp())
            try:
                shutil.move(str(self.profile), str(discard))
            except Exception as e:
                return False, (
                    "could not move the run's throwaway profile out of the way (%s). BOTH copies "
                    "are intact: the run's at %s and yours at %s. Move the second over the first "
                    "by hand." % (e, self.profile, self.backup))

        try:
            shutil.move(str(self.backup), str(self.profile))
        except Exception as e:
            return False, (
                "could not restore the profile (%s). BOTH copies are intact and neither was "
                "deleted: yours is at %s and the run's throwaway is at %s. Move the first to %s."
                % (e, self.backup, discard or "(none)", self.profile))

        try:
            (self.profile / RESTORE_NOTE).unlink()
        except Exception:
            pass

        # WARNING: THE ONLY `rmtree` IN THIS FILE, AND IT IS GUARDED ON THE TARGET'S OWN NAME
        # RATHER THAN ON A FLAG ABOUT IT. A variable-name mistake cannot point this at the live
        # profile, because the live profile's name does not carry this prefix and the guard reads
        # the path being deleted rather than the variable holding it.
        if discard is not None and DISCARD_PREFIX in discard.name and discard.exists():
            shutil.rmtree(discard, ignore_errors=True)

        self.backup = None
        return True, "profile restored: %s" % self.profile


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
                         ("defender", r"defender\s*:\s*(-?\d+)"),

                         # The tournament block. `NetStateReport` prints these so a harness can
                         # assert the PRESET a match was played under, and not merely that some
                         # match happened. `docs/TODO.md` section 145.8.
                         ("mode", r"^mode\s*:\s*(\S+)"),
                         ("ruleset", r"tournament ruleset\s*:\s*(.+)$"),
                         ("modifiers", r"tournament modifiers\s*:\s*(.+)$")):
        m = re.search(pattern, text, re.MULTILINE)
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


# ---------------------------------------------------------------------------
# IS THIS ARTIFACT A CANDIDATE, AND WAS THIS A TOURNAMENT MATCH
#
# WARNING: THE PREVIOUS COLD START PROVED "SOME MATCH RAN" AND WAS READ AS "THE NATIONALS PATH
# WORKS". The recorded green run at `87346b8` is a macOS player playing HERO STRIKE, and
# `docs/VISION.md` section 1.1 says in as many words that CLASSIC is the tournament ruleset.
# Both of those are useful smoke coverage and neither is the claim a release needs, so the
# preset is asserted here rather than assumed by whoever reads the table.
# ---------------------------------------------------------------------------

def _repo_expectations():
    """The protocol and the UGS identity THIS SOURCE resolves against, read from the repo.

    WARNING: READ, NEVER COPIED. `tools/qualify.py` already owns both readers and the protocol
    number has gone stale in five separate paragraphs whose whole job was to warn about it going
    stale. Importing is the only way this file cannot become the sixth.
    """
    sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
    import qualify  # noqa: E402

    return qualify.read_protocol_version(), qualify.read_ugs_identity()


def candidate_step(identity, sha, nationals):
    """Whether the artifact on disk is the exact thing a release would ship."""
    reasons = []
    protocol, ugs = _repo_expectations()

    if (identity.get("sha") or "") != sha:
        reasons.append("the stamp says %s and HEAD is %s"
                       % ((identity.get("sha") or "?")[:12], sha[:12]))

    tree = (identity.get("treeState") or "").strip().lower()
    if tree != "clean":
        # WARNING: AN EMPTY `treeState` IS `unknown` AND NOT `clean`. A player built before
        # 2026-09-05 carries only the old `dirty` bool, which came from an mtime heuristic that
        # could not see an ordinary unstaged edit. `BuildIdentity.StateOf` makes the same
        # three-way distinction for the same reason.
        reasons.append("the artifact's working tree was %s at build time, so its SHA is not a "
                       "claim about what is in it" % (tree or "unknown"))

    if protocol is not None and identity.get("protocol") != protocol:
        reasons.append("it was built at protocol %s and this source reads %s: two peers on "
                       "different numbers refuse each other by design"
                       % (identity.get("protocol"), protocol))

    if nationals:
        if identity.get("target") != NATIONALS_TARGET:
            reasons.append("it is a %s build and the nationals player is %s"
                           % (identity.get("target"), NATIONALS_TARGET))

        want_project = (ugs.get("project_id") or "").strip()
        got_project = (identity.get("ugsProject") or "").strip()

        if not got_project:
            reasons.append("it carries no UGS project id, so nothing can prove it resolves join "
                           "codes in the same namespace as the other machine. A blank looks "
                           "like agreement and is not one")
        elif want_project and got_project != want_project:
            reasons.append("it resolves join codes in UGS project %s and this source is %s: a "
                           "lobby in the other namespace reads as EMPTY rather than as an error"
                           % (got_project, want_project))

        if not (identity.get("ugsEnvironment") or "").strip():
            reasons.append("it carries no UGS environment, which is the other half of the "
                           "namespace a join code is resolved in")

    return {
        "name": "the artifact is a nationals candidate" if nationals
                else "the artifact matches this commit",
        "seconds": 0.0,
        "faults": [],
        "ok": not reasons,
        "detail": "; ".join(reasons) if reasons else (
            "SHA %s, tree %s, protocol %s, %s, UGS %s/%s"
            % (sha[:12], identity.get("treeState"), identity.get("protocol"),
               identity.get("target"), identity.get("ugsProject"),
               identity.get("ugsEnvironment"))),
    }


def tournament_reasons(state):
    """Why the match that just ran was not the tournament one. Empty means it was.

    WARNING: IT FAILS ON A MISSING FIELD RATHER THAN SKIPPING IT. A player that predates the
    tournament block in `NetStateReport` prints nothing for these, and reading absence as
    agreement is exactly how a cold start comes back green while proving less than it printed.
    """
    reasons = []

    mode = state.get("mode")
    if mode is None:
        reasons.append("the report carries no `mode` line, so the ruleset cannot be read at all")
    elif mode != "Classic":
        reasons.append("it booted %s and the tournament preset is Classic (docs/VISION.md 1.1)"
                       % mode)

    ruleset = (state.get("ruleset") or "").strip()
    if not ruleset:
        reasons.append("the report carries no `tournament ruleset` line: this player predates "
                       "the preset block and cannot be certified as a tournament run")
    elif ruleset != "OK":
        reasons.append("the rule set was not the tournament one: " + ruleset)

    modifiers = (state.get("modifiers") or "").strip()
    if not modifiers:
        reasons.append("the report carries no `tournament modifiers` line")
    elif modifiers != "none":
        left = {m.strip() for m in modifiers.split(",") if m.strip()}
        stray = sorted(left - set(TOURNAMENT_ALLOWED_MODIFIERS))
        if stray:
            reasons.append("a practice or debug modifier was set: " + ", ".join(stray))

    return reasons


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--clean-profile", action="store_true",
                    help="move persistentDataPath aside first and restore it afterwards")
    ap.add_argument("--seconds", type=int, default=45, help="how long the driven match runs")
    ap.add_argument("--tournament", action="store_true",
                    help="drive the CLASSIC tournament preset instead of a generic bot match, "
                         "and assert the preset rather than merely that a match happened")
    ap.add_argument("--nationals", action="store_true",
                    help="--tournament, plus the artifact identity gates a release candidate "
                         "has to pass: the release build target, a clean tree stamp, the "
                         "protocol and the UGS project this source resolves join codes in")
    args = ap.parse_args()

    # ⚠️ THE NATIONALS GATE IS THE TOURNAMENT ONE WITH THE ARTIFACT CHECKS ON TOP, so it can
    # never be the weaker of the two by somebody forgetting to pass both.
    if args.nationals:
        args.tournament = True

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

    vault = ProfileVault(profile_dir())

    try:
        if args.clean_profile:
            backup = vault.set_aside()
            if backup is not None:
                print(f"  profile moved aside: {vault.profile} -> {backup.name}")
            else:
                print(f"  no profile at {vault.profile}; this machine is already cold")

        # ---- 0. is this artifact a candidate at all -----------------------
        steps.append(candidate_step(identity, sha, nationals=args.nationals))

        # ---- 1. does it launch at all, and can it say what it is ----------
        print("  1. identity ...", flush=True)
        step = run_player(exe, ["-tp-identity", "-batchmode", "-nographics"],
                          timeout=180, log=LOGS / "coldstart-identity.log")
        step["name"] = "launches and identifies itself"
        step["faults"] = read_log_faults(pathlib.Path(step["log"]))
        step["ok"] = step["log_bytes"] > 0 and not step["faults"]
        steps.append(step)

        # ---- 2. host a whole match with four bots, from nothing -----------
        print(f"  2. hosting a driven {'CLASSIC tournament' if args.tournament else 'bot'} "
              f"match for {args.seconds}s ...", flush=True)
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
        # ⚠️⚠️ THE TOURNAMENT RUN DROPS `-tp-allbots` AND THAT IS THE POINT OF IT, NOT A
        # SHORTCUT. `GameLaunch.AllBots` is one of the eight switches `TournamentPreset.Modifiers`
        # names as forbidden in a bracket match, so a "tournament" cold start that set it would
        # be asserting the preset while breaking it. Without it `MatchInstaller.HumanSeat` is
        # seat 0, that seat is a person standing still, and the three unoccupied seats are bots
        # that drive: a live round, real movement, and every tournament modifier at its safe
        # value at the same time.
        drive = [] if args.tournament else ["-tp-allbots"]
        preset = ["-tp-tournament"] if args.tournament else []

        step = run_player(exe, ["-tp-host", "8910", "-tp-profile", "coldstart"] + drive + preset +
                               ["-tp-autostart", "1",
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
            "name": ("a real CLASSIC tournament round became active" if args.tournament
                     else "a real round became active"),
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

            reasons += tournament_reasons(state) if args.tournament else []

            match_step["ok"] = not reasons
            match_step["detail"] = "; ".join(reasons) if reasons else (
                f"round {state['round']}, active, "
                f"{len(driving)} seats driving, {state.get('sampled')} s sampled")

        steps.append(match_step)

        if report.exists():
            # ⚠️ BY NAME RATHER THAN BY INDEX. This was `steps[1]`, which was the arena step
            # until a candidate-artifact row went in front of it; a positional reference into a
            # list somebody appends to is the same class of stale pointer as a count beside a
            # list (`CLAUDE.md` § 7's "all five editor checks" against seven).
            for s in steps:
                if s["name"].startswith("reaches the arena"):
                    s["report_head"] = "\n".join(
                        report.read_text(encoding="utf-8", errors="replace").splitlines()[:24])
                    break

    finally:
        # ⚠️⚠️ RESTORED IN A `finally`, ALWAYS. A crash between the move and the restore would
        # otherwise leave somebody's settings, rebinds and career sitting under a name they have
        # no reason to look for.
        #
        # ⚠️ AND THE WHOLE DESTRUCTIVE SURFACE IS IN `ProfileVault` RATHER THAN HERE. This block
        # used to hold the `rmtree`, the path arithmetic and the guard, which is three chances to
        # get it wrong in a `finally` that runs after an exception. `tools/audit_harness_contracts
        # .py` drives that class over temporary directories, including the case where a backup
        # from a previous crashed run is already on disk, which the fixed path could not survive.
        restored, message = vault.restore()
        if message:
            print(("  " + message) if restored else message,
                  file=(sys.stdout if restored else sys.stderr))

    ok = all(s["ok"] for s in steps)

    lines = []
    lines.append("# Cold start of the shipped player")
    lines.append("")
    lines.append(f"- **Commit** `{sha}`")
    lines.append(f"- **Artifact** `{exe}`")
    lines.append(f"- **Built** {identity.get('builtAt')}, protocol {identity.get('protocol')}, "
                 f"{identity.get('target')}")
    lines.append(f"- **Profile cleared** {'yes' if args.clean_profile else 'NO (opt in with --clean-profile)'}")
    lines.append(f"- **Preset** {'CLASSIC tournament (docs/VISION.md 1.1)' if args.tournament else 'generic bot match, mode as shipped'}")
    lines.append(f"- **Gate** {'NATIONALS CANDIDATE' if args.nationals else 'smoke'}")
    lines.append(f"- **Artifact tree state** `{identity.get('treeState') or 'unknown'}`, "
                 f"UGS `{identity.get('ugsProject')}` / `{identity.get('ugsEnvironment')}`")
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
