#!/usr/bin/env python3
"""
The one canonical nationals qualification path, tied to an exact git SHA.

WHY THIS EXISTS
---------------
A verification pass in this repository is four toolchains (dotnet, Unity EditMode,
Unity PlayMode, the editor checks) plus eight source audits, and every one of them
has its own way of looking green while proving nothing:

  - PlayMode with `-nographics` CRASHES, writes no `.xml`, and still exits 0.
  - The full PlayMode suite in ONE process has been 42, 41, 56 and 50 red on four
    commits, with the red set moving each time: it measures cross-fixture leakage
    rather than the code, so this runs it in isolated groups instead.
  - A run whose objects were destroyed mid-flight writes a WELL FORMED `.xml` that
    says `result="Passed"` and `total="0"`.
  - `-testFilter` is semicolon separated; a comma-joined list matches nothing and
    produces that same empty green file in thirteen seconds.
  - `-batchmode -quit` exits before compiling scripts and returns 0.
  - A `.xml` left over from a previous, different commit reads exactly like a fresh one.

So this script asserts on the XML, never on the exit code, and it refuses a result
it cannot tie to the commit it was asked about. `docs/TODO.md` § 142 is the entry.

WHAT IT REFUSES
---------------
  missing xml, stale xml (written before this run started), total="0",
  zero discovered tests, a run whose HEAD moved underneath it, and a
  PlayMode pass that only ran once when a nationals candidate was asked for.

⚠️⚠️ AND SINCE 2026-09-05 IT REFUSES A DIRTY TREE, WHICH IT USED TO WARN ABOUT AND
CERTIFY ANYWAY. `docs/TODO.md` § 145.1: a report saying **QUALIFIED at SHA X** must mean
the tested source IS SHA X, and a printed line saying "the working tree was dirty" over
the word QUALIFIED is a note somebody has to read rather than a gate. Tracked changes
fail.

⚠️⚠️ AND SINCE 2026-09-05 UNTRACKED **SOURCE** FAILS IT TOO, WHICH IS THE OTHER HALF AND WAS
THE BIGGER HOLE. This paragraph used to read "untracked files do not fail it", and in a Unity
project that is unsafe: an untracked `.cs` under `Assets/` COMPILES, an untracked `.shader`,
`.prefab`, `.unity` or `Resources/` asset SHIPS, and `ProjectSettings/` decides the build target
and the UGS project. All of them change the artifact while HEAD points at a commit that does not
contain them, and this report printed `SHA X / tree clean` over the top. `.gitignore` still
removes `Logs/`, `Library/`, `Builds/` and the build stamps before the classifier ever sees
them; what is left is DEFAULT-DENY, so a directory nobody has thought about is dirty rather than
silently forgiven. `is_source_sensitive` and `docs/TODO.md` § 145.9.

⚠️⚠️ A THIRD VERDICT EXISTS AND IT IS NOT A FAILURE: **NON-QUALIFIABLE**, for a run where
every stage passed and the checkout state could not be established at all, no `git` on
PATH, a source export, a `git status` that failed. Calling that NOT QUALIFIED would say a
test failed when none did; calling it QUALIFIED is the fault above.

⚠️ NOTHING HERE STOPS A LOCAL DIRTY BUILD. `GameBuilder` records the tree state and builds
anyway, deliberately: building with uncommitted changes at a venue at 8 a.m. is a
legitimate thing to do. **The strictness belongs in the certification path**, which is
this file.

USAGE
-----
  python tools/qualify.py --stage core
  python tools/qualify.py --stage editmode
  python tools/qualify.py --stage playmode          # one pass
  python tools/qualify.py --stage checks
  python tools/qualify.py --stage audits
  python tools/qualify.py --stage report

  python tools/qualify.py --nationals               # everything, PlayMode TWICE

Every stage writes `Logs/qualify/<stage>.json`; `--stage report` reads those and
writes `docs/reports/qualification-<sha>.md` plus `Logs/qualify/summary.json`.
A stage whose json is missing reports NOT RUN rather than passing by absence.
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
import xml.etree.ElementTree as ET
import zipfile

# ⚠️⚠️ THE CONSOLE HERE IS cp1252 AND THIS FILE PRINTS THE REPOSITORY'S OWN ⚠ MARKS.
# `CLAUDE.md` § 7.1 records `audit_audio_reach.py` dying on a UnicodeEncodeError part way
# through its own output, "which looks like a crash in the thing it is auditing", and the
# remedy written down there is to remember to set PYTHONIOENCODING. Remembering is not a
# mechanism: the script sets its own stream up instead, so it cannot be run wrongly.
try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

ROOT = pathlib.Path(__file__).resolve().parent.parent
LOGS = ROOT / "Logs"
OUT = LOGS / "qualify"
REPORTS = ROOT / "docs" / "reports"

# ⚠️⚠️ RESOLVED PER MACHINE, BECAUSE THERE ARE THREE OF THEM AND TWO ARE NOT WINDOWS.
# `CLAUDE.md` § 7 carries the whole table and the warning that goes with it: *"a note that is
# true on one machine and written as a fact about 'here' sends whoever is on another one
# hunting."* These were two Windows literals, so on the Mac in that table every stage of this
# gate launched a path that does not exist and reported the failure as the STAGE failing.
UNITY_VERSION = "6000.5.8f1"


def _first_existing(*candidates):
    for c in candidates:
        if c and pathlib.Path(c).exists():
            return pathlib.Path(c)
    return pathlib.Path(candidates[0]) if candidates else None


UNITY = _first_existing(
    rf"C:\Program Files\Unity\Hub\Editor\{UNITY_VERSION}\Editor\Unity.exe",
    f"/Applications/Unity/Hub/Editor/{UNITY_VERSION}/Unity.app/Contents/MacOS/Unity",
    os.path.expanduser(
        f"~/Applications/Unity/Hub/Editor/{UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"),
)

DOTNET = _first_existing(
    r"C:\Program Files\dotnet\dotnet.exe",
    "/usr/local/share/dotnet/dotnet",
    "/opt/homebrew/bin/dotnet",
    shutil.which("dotnet") or "",
)

# ⚠️⚠️ THE BUILD TARGET FOLLOWS THE MACHINE, AND IT IS NAMED IN THE REPORT RATHER THAN ASSUMED.
# `unity()`'s note is right that a target has to be stated: an Android build leaves the editor
# under UNITY_ANDROID and the next silent launch measures a platform nobody ships at nationals.
# What was wrong is that Win64 was stated as a CONSTANT: `CLAUDE.md` § 7's Mac has no Windows
# Standalone module at all, so every launch there asked for a target the editor cannot switch to.
#
# ⚠️ A QUALIFICATION RUN FOR THE NATIONALS PLAYER STILL HAPPENS ON A WINDOWS MACHINE, and the
# report says which target it ran under so a reader can tell the two apart rather than assuming.
BUILD_TARGET = "OSXUniversal" if sys.platform == "darwin" else "Win64"

# ⚠️ WHAT A SHIPPED PLAYER'S STAMP SAYS WHEN IT IS THE NATIONALS ONE. `BuildTarget.ToString()`
# spells it out, so this is not `BUILD_TARGET` above with different capitalisation: that one is
# the `-buildTarget` switch and this one is the value in `build-identity.json`.
NATIONALS_TARGET = "StandaloneWindows64"

# The default PlayMode exclusions, and both are decisions rather than convenience.
#   WallClock  - AiDiagnosticProbe runs a round at 1x for ~80 real seconds, so its
#                verdict depends on how busy the machine is. CLAUDE.md § 7.
#   ThumbFloor - InputSurfaceProbe measures every menu control against the 144-unit
#                touch floor and is a shrinking gap rather than a flake.
PLAYMODE_CATEGORIES = "!WallClock;!ThumbFloor"


# --------------------------------------------------------------------------
# git identity
# --------------------------------------------------------------------------

def run(cmd, **kw):
    return subprocess.run(cmd, cwd=str(ROOT), capture_output=True, text=True,
                          errors="replace", **kw)


def head_sha():
    r = run(["git", "rev-parse", "HEAD"])
    return r.stdout.strip() if r.returncode == 0 else "UNKNOWN"


def head_branch():
    r = run(["git", "rev-parse", "--abbrev-ref", "HEAD"])
    return r.stdout.strip() if r.returncode == 0 else "UNKNOWN"


# --------------------------------------------------------------------------
# WHICH UNTRACKED FILES ARE SOURCE
#
# WARNING: THIS FUNCTION DROPPED EVERY `??` ROW AND THAT IS UNSAFE IN A UNITY PROJECT. The
# reasoning written here was "`Logs/`, `Builds/` and a scratch file are not differences in the
# source that was tested", which is true of those three and false of the general case. An
# untracked `Assets/TumbangPreso/Runtime/Foo.cs` COMPILES. An untracked `.shader`, `.prefab`,
# `.unity` or anything under `Resources/` SHIPS. `ProjectSettings/` decides the splash, the
# protocol's own project id and the build target. Every one of those changes the artifact while
# HEAD still points at a commit that does not contain it, and the report said `SHA X / tree
# clean` over the top of it. `docs/TODO.md` section 145.9.
#
# WARNING: `.gitignore` IS THE FIRST FILTER AND IT IS ALREADY DOING MOST OF THIS WORK.
# `git status --porcelain` does not list ignored files at all, so `Library/`, `Temp/`, `Logs/`,
# `Builds/`, `obj/`, `bin/`, the build stamps and the shader-variant collection never reach this
# classifier. What DOES reach it is a file somebody added that git has not been told to ignore,
# and the honest default for that is "source".
#
# WARNING: SO THE RULE IS DEFAULT-DENY WITH A SHORT, WRITTEN, TESTED LIST OF NON-SOURCE ROOTS,
# AND NOT AN ALLOWLIST OF SOURCE DIRECTORIES. An allowlist of source directories is the brittle
# shape: somebody adds `Assets/NewThing/` next year, nobody edits the list, and the gate goes
# quiet in exactly the direction nobody checks. Under default-deny a directory nobody has
# thought about is DIRTY, which is loud, and the only way to make the gate quieter is to write
# a row here with a reason attached.
# --------------------------------------------------------------------------

# Roots whose untracked contents genuinely cannot reach a build. Each row carries the reason it
# is on the list, because a path with no reason is a path the next person deletes or copies.
NON_SOURCE_UNTRACKED_ROOTS = (
    # Editor and toolchain output. All of these are in `.gitignore` as well, so they should
    # never reach this classifier; they are named anyway because a `.gitignore` edit must not
    # silently turn build output into a certification failure at a venue.
    "Logs/",
    "Library/",
    "Temp/",
    "obj/",
    "bin/",
    "Build/",
    "Builds/",
    "build/",
    "UserSettings/",
    "MemoryCaptures/",
    "Recordings/",
    ".utmp/",
    ".vs/",
    ".vscode/",

    # One-shot working files. `.gitignore`'s own note: "they are worthless the moment they have
    # run", and the record of WHY a change was made is the comment in the code.
    "scratchpad/",

    # WARNING: `docs/reports/` IS THE ONE ROW THAT IS NOT OBVIOUS AND IT HAS TO BE HERE. This
    # gate WRITES `docs/reports/qualification-<sha>.md` as its own last act, so without this row
    # the first run leaves the tree non-certifiable and every run after it fails on the evidence
    # the previous run produced. A generated report is not source: nothing under it compiles,
    # ships, or changes a byte of the artifact.
    "docs/reports/",
)


def is_source_sensitive(path):
    """Whether an untracked path could have changed what was compiled or shipped.

    WARNING: THE DEFAULT IS TRUE. A path this function has never heard of is source.

    WARNING: GIT QUOTES A PATH WITH A SPACE OR A NON-ASCII CHARACTER IN IT. The surrounding
    quotes are stripped; the C-escapes inside one are not decoded, and that is safe by
    construction rather than by luck, because this rule is default-deny: a path this function
    mangles is still classified as SOURCE and still makes the tree dirty. Every parsing mistake
    fails towards refusing to certify.
    """
    row = (path or "").strip()
    if len(row) >= 2 and row[0] == '"' and row[-1] == '"':
        row = row[1:-1]

    row = row.replace("\\", "/").lstrip("./")
    if not row:
        return False

    for root in NON_SOURCE_UNTRACKED_ROOTS:
        if row == root.rstrip("/") or row.startswith(root):
            return False

    return True


def working_tree():
    """
    Whether the checkout matches the commit this report is about to certify.

    ⚠️⚠️ THE VERDICT IS THE POINT AND IT USED TO BE A WARNING. This function answered a LIST and
    `stage_report` printed a line saying the tree was dirty, and then went on to write
    **QUALIFIED** underneath it. `docs/TODO.md` § 145.1: *"A release report saying QUALIFIED at
    SHA X must mean the tested source actually corresponds to SHA X."* A note somebody has to
    read is not a gate; every ⚠️ in this repository exists because a note was not read once.

    ⚠️⚠️ AND "CANNOT TELL" IS ITS OWN ANSWER. `git` not being on PATH, a source export with no
    repository and a `git` that fails are all states this can genuinely be in, and every one of
    them means the SHA at the top of the report is unverifiable. `GameBuilder.WorkingTreeState`
    makes the same three-way distinction for the same reason, and the brief for both is the same
    sentence: **do not turn "cannot determine" into "clean"**.

    ⚠️⚠️ AND UNTRACKED FILES USED TO BE DROPPED WHOLESALE, WHICH IS UNSAFE IN A UNITY PROJECT.
    An untracked `.cs` under `Assets/` compiles; an untracked `.shader`, `.prefab`, `.unity` or
    `Resources/` asset ships; `ProjectSettings/` decides the build target and the UGS project.
    Every one of those is source this report would have certified as `SHA X / tree clean` while
    testing something that is not in X. `is_source_sensitive` is the rule and it is default-deny;
    `docs/TODO.md` § 145.9 is the entry.

    ⚠️ `.gitignore` STILL DOES THE BULK OF IT. `--porcelain` does not list ignored files at all,
    so `Logs/`, `Library/`, `Builds/` and the build stamps never reach the classifier. What does
    reach it is a file somebody added and git was never told to ignore.

    ⚠️⚠️ AND IT ASKS `git diff` RATHER THAN `git status`, WHICH IS NOT A TIDY-UP: `--porcelain`
    REPORTED A FILE THAT IS BYTE-IDENTICAL TO HEAD. Measured 2026-09-05 on this Windows machine,
    where `core.autocrlf` is **true** and the repository stores Unity's YAML with LF:

        $ git hash-object ProjectSettings/QualitySettings.asset
        de28c89de5ec71e15fa2ce09487409d3a2a48898
        $ git rev-parse HEAD:ProjectSettings/QualitySettings.asset
        de28c89de5ec71e15fa2ce09487409d3a2a48898     <- the same blob
        $ git status --porcelain ProjectSettings/
         M ProjectSettings/QualitySettings.asset      <- and still "modified"

    `git diff-index` answers `M` with a **zero destination hash**, which is git saying "the stat
    cache says this changed and I have not compared contents", and `safecrlf` keeps it there
    because checking the file out again would write CRLF over the LF Unity just wrote.
    `git update-index --refresh` does not clear it. Every Unity launch that rewrites a tracked
    YAML file lands in this state, so a gate reading `--porcelain` reports a DIRTY TREE on a
    checkout nobody has edited, and `docs/TODO.md` § 145.1 turned that into a refusal.

    ⚠️ THE ALTERNATIVE WAS A `.gitattributes` PINNING UNITY YAML TO LF, AND IT WAS MEASURED AND
    REJECTED FOR NOW: **1373 of the 1987 tracked Unity YAML files are CRLF on disk here**, so
    that change is a renormalisation touching two thirds of the project, days before nationals.
    It is the right long-term answer and it is written down in § 149.11; it is not the right
    thing to do in this session.

    ⚠️ NOTHING IS FORGIVEN BY THIS. `git diff --name-only HEAD` is a CONTENT comparison, so a
    file whose bytes equal the commit is not a difference in the source that was tested, which is
    exactly and only what this function is asking. A real edit still lists.

    Returns (state, rows) where state is "clean", "dirty" or "unknown".
    """
    edited = run(["git", "diff", "--name-only", "HEAD"])
    if edited.returncode != 0:
        return "unknown", [(edited.stderr or "git diff failed").strip()[:200]]

    # ⚠️ `--exclude-standard` IS WHAT MAKES `.gitignore` THE FIRST FILTER. Without it every
    # `Library/` artefact arrives here and the classifier does work `.gitignore` already did.
    others = run(["git", "ls-files", "--others", "--exclude-standard"])
    if others.returncode != 0:
        return "unknown", [(others.stderr or "git ls-files failed").strip()[:200]]

    rows = [" M " + line.strip() for line in edited.stdout.splitlines() if line.strip()]

    for line in others.stdout.splitlines():
        path = line.strip()
        if path and is_source_sensitive(path):
            rows.append("?? " + path)

    return ("dirty" if rows else "clean"), rows


def working_tree_dirty():
    """⚠️ KEPT AS THE OLD SHAPE FOR THE JSON SUMMARY, which records a list of paths."""
    return working_tree()[1]


def now():
    return datetime.datetime.now().isoformat(timespec="seconds")


def write_stage(name, payload):
    OUT.mkdir(parents=True, exist_ok=True)
    payload.setdefault("stage", name)
    payload.setdefault("sha", head_sha())
    payload.setdefault("finished", now())
    (OUT / f"{name}.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
    return payload


def read_stage(name):
    p = OUT / f"{name}.json"
    if not p.exists():
        return None
    try:
        return json.loads(p.read_text(encoding="utf-8"))
    except Exception:
        return None


# --------------------------------------------------------------------------
# The XML gate. This is the part that exists because of every false green above.
# --------------------------------------------------------------------------

def read_nunit_xml(path, started_at):
    """
    Returns (ok, detail-dict). `ok` is False for every shape of false green this
    repository has actually seen, each one named in the reason.
    """
    d = {"path": str(path), "total": None, "passed": None, "failed": None,
         "skipped": None, "inconclusive": None, "result": None, "duration": None,
         "failures": []}

    p = pathlib.Path(path)
    if not p.exists():
        d["reason"] = ("NO XML. The run wrote no results file at all. A PlayMode launch "
                       "with -nographics dies inside NullGfxDevice and still exits 0.")
        return False, d

    mtime = p.stat().st_mtime
    d["written"] = datetime.datetime.fromtimestamp(mtime).isoformat(timespec="seconds")
    if started_at is not None and mtime < started_at - 1:
        d["reason"] = (f"STALE XML. {p.name} was written at {d['written']}, before this run "
                       f"started. A leftover file from an earlier commit reads exactly like a "
                       f"fresh pass.")
        return False, d

    try:
        root = ET.parse(str(p)).getroot()
    except Exception as e:
        d["reason"] = f"UNPARSEABLE XML: {e}"
        return False, d

    def num(attr):
        v = root.get(attr)
        try:
            return int(v)
        except (TypeError, ValueError):
            return None

    d["total"] = num("total")
    d["passed"] = num("passed")
    d["failed"] = num("failed")
    d["skipped"] = num("skipped")
    d["inconclusive"] = num("inconclusive")
    d["result"] = root.get("result")
    d["duration"] = root.get("duration")

    testcasecount = num("testcasecount")
    if testcasecount is not None:
        d["testcasecount"] = testcasecount

    # Collect the failing cases so the report names them rather than a count.
    for case in root.iter("test-case"):
        if case.get("result") == "Failed":
            msg = ""
            fail = case.find("failure")
            if fail is not None:
                m = fail.find("message")
                if m is not None and m.text:
                    msg = " ".join(m.text.split())[:400]
            d["failures"].append({"name": case.get("fullname") or case.get("name"),
                                  "message": msg})

    if d["total"] in (None, 0):
        d["reason"] = ('ZERO TESTS. total="0" with result="Passed" is the worst of the three '
                       'false greens: the file is present, well formed and green. Either '
                       'something destroyed the runner\'s own objects mid-run, or a '
                       '-testFilter matched nothing (it is semicolon separated, not comma).')
        return False, d

    if d["failed"]:
        d["reason"] = f"{d['failed']} FAILED of {d['total']}."
        return False, d

    d["reason"] = f"{d['passed']} passed of {d['total']}."
    return True, d


# --------------------------------------------------------------------------
# Stages
# --------------------------------------------------------------------------

def stage_core():
    started = datetime.datetime.now().timestamp()
    trx = LOGS / "qualify-core.trx"
    if trx.exists():
        trx.unlink()
    LOGS.mkdir(exist_ok=True)

    # ⚠️⚠️ dotnet IS NOT INSTALLED ON EVERY MACHINE THIS RUNS ON AND SAYING SO IS THE ANSWER.
    # `CLAUDE.md` § 7's Mac table: *"NOT INSTALLED. `dotnet test Core.Tests/...`, the cheapest
    # signal in the repo and the one § 2.1b tells you to run freely, cannot run at all"*. A
    # missing toolchain reported as a failing stage sends somebody hunting a broken test; a
    # missing toolchain reported as green is `docs/TODO.md` § 145.1's fault one stage along.
    if DOTNET is None or not DOTNET.exists():
        return write_stage("core", {
            "ok": False, "started": now(), "unavailable": True,
            "reason": f"dotnet is not installed on this machine ({sys.platform}), so the "
                      f"engine-free rules cannot be run here. The same numbers are asserted by "
                      f"the EditMode suite, which is a several-minute launch rather than 40 ms. "
                      f"A nationals certification has to come off a machine with dotnet."})

    r = run([str(DOTNET), "test", "Core.Tests/TumbangPreso.Core.Tests.csproj",
             "--logger", f"trx;LogFileName={trx}"])
    text = r.stdout + r.stderr

    m = re.search(r"Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)", text)
    if not m:
        return write_stage("core", {
            "ok": False, "started": now(),
            "reason": "Could not read a test count out of dotnet test's output.",
            "tail": text[-2000:]})

    failed, passed, skipped, total = (int(x) for x in m.groups())
    ok = failed == 0 and total > 0
    reason = (f"{passed} passed of {total}." if ok
              else (f"{failed} failed of {total}." if total else
                    "ZERO TESTS discovered. A green run over no tests is not a pass."))
    return write_stage("core", {"ok": ok, "started": now(), "reason": reason,
                                "total": total, "passed": passed, "failed": failed,
                                "skipped": skipped, "exit": r.returncode})


def unity(args, logfile, timeout=None):
    """
    Every validation launch names its build target explicitly.

    WHY -buildTarget Win64 IS NOT OPTIONAL: an Android build leaves the editor under
    UNITY_ANDROID, and the next launch that does not say otherwise compiles and runs
    the tests against THAT define set. The suite then measures a platform nobody is
    shipping to at nationals, and it does it silently.
    """
    cmd = [str(UNITY), "-batchmode", "-projectPath", str(ROOT),
           "-buildTarget", BUILD_TARGET, "-logFile", str(logfile)] + args
    return subprocess.run(cmd, cwd=str(ROOT), capture_output=True, text=True,
                          errors="replace", timeout=timeout)


def stage_editmode():
    started = datetime.datetime.now().timestamp()
    xml = LOGS / "qualify-editmode.xml"
    if xml.exists():
        xml.unlink()
    r = unity(["-runTests", "-nographics", "-testPlatform", "EditMode",
               "-testResults", str(xml)], LOGS / "qualify-editmode.log")
    ok, detail = read_nunit_xml(xml, started)
    detail["exit"] = r.returncode
    detail["started"] = now()
    detail["ok"] = ok
    return write_stage("editmode", detail)


def stage_playmode(pass_number=1):
    """
    PlayMode, through `tools/playmode_suite.py` and NEVER as one process.

    ⚠️⚠️ THE SINGLE-PROCESS FULL RUN IS NOT A GATE AND THIS STAGE USED TO BE ONE. It has been
    measured four times (42, 41, 56 and 50 failures) and the RED SET MOVES between runs on
    unchanged code, because fixtures inherit each other's objects, scenes and overlays. A
    stage that ran it would have made this whole script an authoritative wrapper around a
    number that does not describe the commit. `docs/TODO.md` § 143.1 and § 126.8.

    So the suite runs in isolated groups, one Unity launch each, and the aggregate is the
    verdict. **Coverage is asserted against the results**: every discovered fixture must
    appear in exactly one group AND report a case, so a filter typo or a renamed fixture
    fails instead of quietly shrinking what "green" covers.
    """
    started = datetime.datetime.now().timestamp()
    suffix = f"-pass{pass_number}" if pass_number > 1 else ""

    r = subprocess.run([sys.executable, str(ROOT / "tools" / "playmode_suite.py"), "--gate"],
                       cwd=str(ROOT), capture_output=True, text=True, errors="replace")

    summary_path = LOGS / "playmode-suite" / f"summary{suffix}.json"
    if not summary_path.exists():
        summary_path = LOGS / "playmode-suite" / "summary.json"

    d = {"started": now(), "pass_number": pass_number, "exit": r.returncode,
         "categories": PLAYMODE_CATEGORIES, "failures": []}

    if not summary_path.exists():
        d["ok"] = False
        d["reason"] = ("the grouped suite wrote no summary, so it did not finish. Run "
                       "`python tools/playmode_suite.py --gate` and read its output.")
        return write_stage(f"playmode{pass_number}", d)

    if summary_path.stat().st_mtime < started - 1:
        d["ok"] = False
        d["reason"] = "STALE summary, written before this run started."
        return write_stage(f"playmode{pass_number}", d)

    agg = json.loads(summary_path.read_text(encoding="utf-8"))

    d.update(total=agg.get("total"), passed=agg.get("passed"),
             failed=agg.get("failed"), skipped=agg.get("skipped"),
             fixtures_discovered=agg.get("fixtures_discovered"),
             fixtures_ran=agg.get("fixtures_ran"),
             fixtures_never_ran=agg.get("fixtures_never_ran"),
             groups=[{"group": g["group"], "ok": g.get("ok"), "reason": g.get("reason")}
                     for g in agg.get("groups", [])])

    for group in agg.get("groups", []):
        for f in group.get("failures", []):
            d["failures"].append({"name": f"[{group['group']}] {f['name']}",
                                  "message": f.get("message", "")})

    d["ok"] = bool(agg.get("ok"))
    d["reason"] = (f"{d['passed']} passed of {d['total']} across "
                   f"{len(agg.get('groups', []))} isolated groups, "
                   f"{d['fixtures_ran']} of {d['fixtures_discovered']} fixtures. "
                   + ("" if d["ok"] else f"{d['failed']} failed."))
    return write_stage(f"playmode{pass_number}", d)


def stage_checks():
    started = datetime.datetime.now().timestamp()
    report = LOGS / "checks.txt"
    if report.exists():
        report.unlink()
    r = unity(["-executeMethod", "TumbangPreso.EditorTools.Checks.RunAll"],
              LOGS / "qualify-checks.log")

    d = {"exit": r.returncode, "started": now()}
    if not report.exists():
        d["ok"] = False
        d["reason"] = ("Checks.RunAll wrote no Logs/checks.txt. The launch did not reach the "
                       "method, which an exit code alone cannot tell you.")
        return write_stage("checks", d)

    text = report.read_text(encoding="utf-8", errors="replace")
    d["report"] = text.strip()
    failed = [ln.strip() for ln in text.splitlines() if ln.strip().startswith("FAIL")]
    d["failed_checks"] = failed
    d["ok"] = "RESULT: OK" in text and not failed
    d["reason"] = ("all checks passed in one launch" if d["ok"]
                   else f"{len(failed)} check(s) red: {failed}")
    return write_stage("checks", d)


AUDITS = [
    ("ability authority", "audit_ability_authority.py"),
    ("request call sites", "audit_request_call_sites.py"),
    ("wire payloads", "audit_wire_payloads.py"),
    ("audio reach", "audit_audio_reach.py"),
    ("presentation reach", "audit_presentation_reach.py"),
    ("cue relay", "audit_cue_relay.py"),
    ("shader stripping", "audit_shader_stripping.py"),
    ("ability stat drift", "audit_ability_stat_drift.py"),
    ("event subscriptions", "audit_event_subscriptions.py"),
    ("tournament defaults", "audit_tournament_defaults.py"),
    ("gameplay clocks", "audit_gameplay_clocks.py"),

    # ⚠️⚠️ THE ONE THAT AUDITS THE GRADERS RATHER THAN THE GAME, AND IT IS GATING FOR THE SAME
    # REASON THE OTHERS ARE. `net_matrix.py` printed `DIVERGED` and exited 0; this file dropped
    # every untracked source file; `cold_start.py --clean-profile` deleted a pre-existing backup.
    # A harness that grades the release and cannot itself be graded is where the next false green
    # comes from. `docs/TODO.md` § 145.13.
    ("harness contracts", "audit_harness_contracts.py"),

    # ⚠️⚠️ NaN IS THE ONE THE OBVIOUS GUARD LETS THROUGH. Every ordinary C# comparison against
    # NaN is false, so `if (v > limit) return false;` PASSES one and `Mathf.Clamp` returns one
    # unchanged. A spectator could set `Time.timeScale` to NaN on the host and, through
    # `SyncTime`, on every peer. `docs/TODO.md` § 149.9.
    ("wire finite values", "audit_wire_finite.py"),
]


# ⚠️⚠️ INFORMATIONAL, NOT GATING, AND THE DISTINCTION IS ARGUED RATHER THAN CONVENIENT.
# `audit_cue_audio.py` asks whether each cue file contains an audible, unclipped sound, and it
# flags 11 of 117 on this commit: three UI clicks with a DC offset around -0.11, and eight others.
# Every one of those is pre-existing and none is a correctness fault.
#
# ⚠️ THE REASON IT CANNOT GATE IS `CLAUDE.md` § 6: "SOURCED SFX ARE PROVISIONAL UNTIL 🧑 HEARS
# THEM IN PLAY." Twenty-four sourced cues are awaiting exactly that judgement (`Attention.md`
# § 13), so gating on their measured quality would make QUALIFIED unreachable until somebody
# finishes a listening pass that is deliberately not on the critical path.
#
# ⚠️⚠️ AND IT IS STILL RUN AND STILL REPORTED, because the other way to "fix" a permanently red
# audit is to delete it, and then nobody ever learns the number. A gate that cannot be green is a
# gate that gets ignored; an audit whose findings are printed and not counted is a finding that
# stays visible. If a cue ever goes SILENT rather than merely offset, that is a correctness fault
# and belongs back in the gating list.
INFORMATIONAL_AUDITS = [
    ("cue audio", "audit_cue_audio.py"),
]


def stage_audits():
    """
    The source audits, promoted from "somebody remembered to run them" to a gate.

    ⚠️ THEY EXIT NON-ZERO ON A FINDING, which is the whole reason they can gate. An
    audit that only prints is an audit that goes unread; `docs/TODO.md` § 143.3.
    """
    env = dict(os.environ)
    env["PYTHONIOENCODING"] = "utf-8"   # audit_audio_reach dies on a UnicodeEncodeError without it

    rows = []
    for label, script in AUDITS + INFORMATIONAL_AUDITS:
        path = ROOT / "tools" / script
        if not path.exists():
            rows.append({"audit": label, "script": script, "ok": False,
                         "reason": "script missing"})
            continue
        r = subprocess.run([sys.executable, str(path)], cwd=str(ROOT),
                           capture_output=True, text=True, errors="replace", env=env)
        tail = [ln for ln in (r.stdout or "").strip().splitlines() if ln.strip()]
        gating = (label, script) in AUDITS
        rows.append({"audit": label, "script": script, "ok": r.returncode == 0,
                     "gating": gating, "exit": r.returncode,
                     "summary": tail[-1] if tail else "(no output)"})

    ok = all(row["ok"] for row in rows if row["gating"])
    return write_stage("audits", {"ok": ok, "started": now(), "audits": rows,
                                  "reason": ("all audits clean" if ok else
                                             ", ".join(r["audit"] for r in rows
                                                       if not r["ok"] and r["gating"])
                                             + " reported findings")})


# --------------------------------------------------------------------------
# Identity: what a peer compares, and what a player can read off a build
# --------------------------------------------------------------------------

def read_protocol_version():
    """
    ⚠️ READ FROM SOURCE, NEVER FROM A DOCUMENT. This number has gone stale in the one
    paragraph that exists to warn about it going stale four times. Peers on different
    numbers refuse each other by design, so a copied number sends somebody hunting a
    network bug that is a rebuild.
    """
    src = ROOT / "Assets" / "TumbangPreso" / "Runtime" / "Net" / "NetSession.cs"
    m = re.search(r"public\s+const\s+int\s+ProtocolVersion\s*=\s*(\d+)\s*;",
                  src.read_text(encoding="utf-8", errors="replace"))
    return int(m.group(1)) if m else None


def read_app_version():
    src = ROOT / "ProjectSettings" / "ProjectSettings.asset"
    text = src.read_text(encoding="utf-8", errors="replace")
    m = re.search(r"^\s*bundleVersion:\s*(.+)$", text, re.M)
    return m.group(1).strip() if m else None


def read_ugs_identity():
    """The project a join code is resolved in. A machine on a different project reads
    a live lobby as an empty one rather than as an error."""
    out = {}
    p = ROOT / "ProjectSettings" / "ProjectSettings.asset"
    text = p.read_text(encoding="utf-8", errors="replace")
    for key, attr in (("cloudProjectId", "project_id"),
                      ("organizationId", "organization"),
                      ("projectName", "project_name")):
        m = re.search(rf"^\s*{key}:\s*(.+)$", text, re.M)
        if m:
            out[attr] = m.group(1).strip()
    return out


def artifact_identity():
    """
    What is actually sitting on this machine as a shippable player, and whether the
    two platforms agree. A Windows build from one commit beside an Android build from
    another is two games that refuse each other correctly and read as a bug.
    """
    found = {}
    desktop = pathlib.Path(os.path.expanduser("~")) / "Desktop"

    win = desktop / "TumbangPreso-Unity"
    exe = win / "TumbangPreso.exe"
    if exe.exists():
        stamp = win / "TumbangPreso_Data" / "StreamingAssets" / "build-identity.json"
        row = {"path": str(exe),
               "written": datetime.datetime.fromtimestamp(exe.stat().st_mtime)
                                   .isoformat(timespec="seconds"),
               "bytes": exe.stat().st_size}
        if stamp.exists():
            try:
                row["identity"] = json.loads(stamp.read_text(encoding="utf-8"))
            except Exception as e:
                row["identity_error"] = str(e)
        else:
            row["identity"] = None
        found["windows"] = row

    for apk in sorted(ROOT.glob("build/**/*.apk")) + sorted(desktop.glob("*.apk")):
        row = {
            "path": str(apk),
            "written": datetime.datetime.fromtimestamp(apk.stat().st_mtime)
                                .isoformat(timespec="seconds"),
            "bytes": apk.stat().st_size,
        }

        # ⚠️ AN .apk IS A ZIP AND `StreamingAssets` LANDS UNDER `assets/`. That is the whole
        # reason `GameBuilder.StampBuildIdentity` writes the record to StreamingAssets as well
        # as to Resources: a Resources asset is compiled into `resources.assets` and cannot be
        # read from outside the artifact at all, so a tool comparing two platforms would have
        # nothing to compare.
        try:
            with zipfile.ZipFile(apk) as z:
                for name in ("assets/build-identity.json", "assets/bin/Data/build-identity.json"):
                    if name in z.namelist():
                        row["identity"] = json.loads(z.read(name).decode("utf-8"))
                        break
                else:
                    row["identity"] = None
        except Exception as e:
            row["identity"] = None
            row["identity_error"] = str(e)

        found["android"] = row
        break

    return found


# ⚠️⚠️ WHAT A CANDIDATE STAMP HAS TO CARRY, AND WHY A MISSING FIELD IS A REJECTION RATHER THAN
# A SHRUG. Every one of these is read by a person or a tool deciding whether two machines are
# running the same game, and a blank field LOOKS LIKE AGREEMENT. `GameBuilder.UgsProjectId`'s own
# header records the first stamped player shipping `"ugsProject": ""` because
# `CloudProjectSettings.projectId` answers empty in batch mode, which is where every build that
# matters is made.
CANDIDATE_FIELDS = ("sha", "protocol", "target", "appVersion",
                    "ugsProject", "ugsEnvironment", "builtAt", "treeState")

# ⚠️ THE FIELDS TWO ARTIFACTS MUST AGREE ON BEFORE THEY CAN BE CALLED ONE RELEASE.
# `docs/TODO.md` § 145.10: the SHA and the protocol were already compared, and they are not
# enough. Two players from one commit that resolve Relay and Lobby against DIFFERENT UGS
# identities do not refuse each other; **they simply never find each other's rooms**, and
# `CLAUDE.md` § 4a records that this reads as an EMPTY LOBBY rather than as an error, which is
# the single most expensive failure shape at a venue.
CROSSPLAY_FIELDS = (
    ("sha", "the commit"),
    ("protocol", "the wire format, which refuses a mismatch by design"),
    ("ugsProject", "the namespace a join code is resolved in: a different project reads as an "
                   "EMPTY LOBBY rather than as an error"),
    ("ugsEnvironment", "the other half of that namespace"),
    ("appVersion", "what the LAN beacon, the lobby record and the approval hello compare"),
)


def candidate_faults(label, stamp, sha, protocol):
    """Why one stamped artifact is not a release candidate. Empty means it is one.

    ⚠️⚠️ A DIRTY ARTIFACT WITH THE RIGHT SHA IS NOT A CANDIDATE, AND THAT IS THE CASE THIS
    FUNCTION EXISTS FOR. Build with an edited `.cs`, revert the edit, and HEAD is clean while the
    artifact on disk contains code that is in no commit. Every SHA comparison in the world passes
    and the player is not the commit it names. `docs/TODO.md` § 145.10.
    """
    if not stamp:
        return [f"the {label} artifact carries no build-identity.json, so it predates the stamp "
                f"and cannot be tied to a commit at all. Rebuild through GameBuilder."]

    faults = []

    missing = [f for f in CANDIDATE_FIELDS
               if stamp.get(f) is None or (isinstance(stamp.get(f), str) and not stamp[f].strip())]
    if missing:
        faults.append(f"the {label} artifact's stamp is missing {', '.join(missing)}. A blank "
                      f"identity field looks like agreement and is not one; an artifact too old "
                      f"to carry them is not a candidate and has to be rebuilt.")

    tree = (stamp.get("treeState") or "").strip().lower()
    if tree == "dirty":
        faults.append(f"the {label} artifact was built from a DIRTY working tree, so the SHA it "
                      f"names is not what is inside it. Reverting the edit afterwards does not "
                      f"change the artifact.")
    elif tree != "clean":
        # ⚠️ EMPTY OR `unknown` IS NOT `clean`, AND A PRE-2026-09-05 STAMP IS EXACTLY THIS. Its
        # `dirty: false` came from an mtime heuristic that could not see an ordinary unstaged
        # edit; believing it would carry that blind spot forward into the gate built to replace
        # it. `BuildIdentity.StateOf` makes the same three-way distinction.
        faults.append(f"the {label} artifact's tree state is {tree or 'absent'}, which is not "
                      f"clean and is not the same as clean. It cannot be certified.")

    # ⚠️ THE BOOL IS CHECKED AS WELL AS THE STRING. `dirty` is written as "not proven clean", so
    # a record whose two halves disagree is a record something has edited by hand.
    if stamp.get("dirty") is True and tree == "clean":
        faults.append(f"the {label} artifact says treeState clean and dirty true at once. One of "
                      f"the two was written by something other than GameBuilder.")

    if sha and stamp.get("sha") and stamp["sha"] != sha:
        faults.append(f"the {label} artifact was built from {stamp['sha'][:12]} and the candidate "
                      f"commit is {sha[:12]}.")

    if protocol is not None and stamp.get("protocol") != protocol:
        faults.append(f"the {label} artifact was built at protocol {stamp.get('protocol')} and "
                      f"this source reads {protocol}. Peers on different numbers refuse each "
                      f"other by design.")

    return faults


def stage_identity(candidate=False):
    d = {"started": now(),
         "protocol_version": read_protocol_version(),
         "application_version": read_app_version(),
         "ugs": read_ugs_identity(),
         "build_target": f"{BUILD_TARGET} (validation launches)",
         "nationals_target": NATIONALS_TARGET,
         "candidate": bool(candidate),
         "artifacts": artifact_identity()}

    problems = []
    if d["protocol_version"] is None:
        problems.append("could not read NetSession.ProtocolVersion from source")

    arts = d["artifacts"]
    win = (arts.get("windows") or {}).get("identity")
    aid = (arts.get("android") or {}).get("identity")

    # ⚠️⚠️ THE CROSSPLAY CHECK, AND IT IS FIVE FIELDS RATHER THAN TWO. Two artifacts that do not
    # agree on the commit and the protocol are two games that refuse each other. Two that agree
    # on both and resolve join codes in different UGS namespaces are two games that CANNOT SEE
    # each other, with no error anywhere, which is worse.
    if "windows" in arts and "android" in arts:
        if win and aid:
            for field, why in CROSSPLAY_FIELDS:
                a, b = win.get(field), aid.get(field)
                if a != b:
                    problems.append(f"Windows {field} {a!r} != Android {field} {b!r} ({why})")
                elif candidate and isinstance(a, str) and not a.strip():
                    problems.append(f"both artifacts carry a blank {field}, so nothing proves "
                                    f"they agree about it ({why})")
        else:
            problems.append("one or both artifacts carry no build-identity.json, so they "
                            "cannot be compared. Rebuild through GameBuilder.")

    # ⚠️⚠️ AN UNSTAMPED ARTIFACT IS A STALE ARTIFACT UNTIL PROVEN OTHERWISE. `GameBuilder` has
    # stamped every build since `StampBuildIdentity` landed, so a player with no
    # `build-identity.json` predates it and cannot be tied to any commit. Treating "no stamp" as
    # "probably fine" is the reasoning that ships the 14:34 build believing it is the 15:03 one.
    if "windows" in arts and not win:
        problems.append("the Windows artifact carries no build-identity.json, so it predates the "
                        "stamp and cannot be tied to a commit. Rebuild through GameBuilder.")
    if "android" in arts and not aid:
        problems.append("the Android artifact carries no build-identity.json. Rebuild it.")

    if win and d["protocol_version"] is not None and win.get("protocol") != d["protocol_version"]:
        problems.append(f"the Windows artifact was built at protocol {win.get('protocol')} "
                        f"and the source now reads {d['protocol_version']}. Rebuild.")
    if win and win.get("sha") not in (None, head_sha()):
        problems.append(f"the Windows artifact was built from {win.get('sha')[:12]} and HEAD "
                        f"is {head_sha()[:12]}. It is not a candidate for this commit.")

    # ---- the candidate gate ------------------------------------------------
    #
    # ⚠️⚠️ SEPARATE FROM `problems` BECAUSE THE TWO ANSWER DIFFERENT QUESTIONS. The rows above
    # are "is anything here inconsistent", which is worth printing on any run. These are "would
    # this exact artifact be the one shipped to a nationals machine", and a development pass on a
    # laptop is allowed to fail them while its tests are perfectly valid.
    candidate_faults_found = []
    if candidate:
        if "windows" not in arts:
            candidate_faults_found.append(
                "there is no Windows artifact on this machine, and the nationals player is the "
                "Windows one. CLAUDE.md § 7.")
        else:
            candidate_faults_found += candidate_faults("Windows", win, head_sha(),
                                                       d["protocol_version"])
            if win and win.get("target") != NATIONALS_TARGET:
                candidate_faults_found.append(
                    f"the Windows artifact's target is {win.get('target')} and the nationals "
                    f"player is {NATIONALS_TARGET}.")

        want = (d["ugs"].get("project_id") or "").strip()
        got = ((win or {}).get("ugsProject") or "").strip()
        if want and got and want != got:
            candidate_faults_found.append(
                f"the artifact resolves join codes in UGS project {got} and this source is "
                f"{want}. A machine on a different project reads a live lobby as an empty one.")

        if aid:
            candidate_faults_found += candidate_faults("Android", aid, head_sha(),
                                                       d["protocol_version"])

    d["problems"] = problems
    d["candidate_problems"] = candidate_faults_found
    d["ok"] = not problems and not candidate_faults_found
    d["reason"] = ("identity consistent" if d["ok"]
                   else "; ".join(problems + candidate_faults_found))
    return write_stage("identity", d)


# --------------------------------------------------------------------------
# The report
# --------------------------------------------------------------------------

ORDER = ["core", "editmode", "playmode1", "playmode2", "checks", "audits", "identity"]

LABEL = {
    "core": "Core.Tests (engine-free rules)",
    "editmode": "Unity EditMode",
    "playmode1": "PlayMode, isolated groups, pass 1 of 2",
    "playmode2": "PlayMode, isolated groups, pass 2 of 2",
    "checks": "Checks.RunAll (every editor check, one launch)",
    "audits": "Source audits",
    "identity": "Release artifact identity",
}


def stage_report(nationals=False):
    """
    ⚠️⚠️ THE REPORT IS KEYED TO THE COMMIT THE STAGES RAN ON, NOT TO CURRENT HEAD, AND THE
    FIRST VERSION HAD THIS BACKWARDS IN A WAY THAT MADE THE TOOL UNUSABLE. It compared every
    stage against `git rev-parse HEAD`, so **committing the report voided the report**: the
    act of recording a qualification moved HEAD past the commit it described, and the next
    regeneration marked all seven stages WRONG COMMIT.
    ⚠️ THE CHECK THAT MATTERS IS THAT THE STAGES AGREE WITH EACH OTHER. Results stitched
    together from two different commits are the thing worth refusing; HEAD moving afterwards
    is ordinary and is reported as a note rather than as a failure.
    """
    branch = head_branch()
    tree_state, dirty = working_tree()

    stages = {name: read_stage(name) for name in ORDER}

    ran = [s for s in stages.values() if s and s.get("sha")]
    shas = sorted({s["sha"] for s in ran})
    sha = shas[0] if len(shas) == 1 else head_sha()
    moved = head_sha() if (len(shas) == 1 and shas[0] != head_sha()) else None

    required = list(ORDER)
    if not nationals:
        required.remove("playmode2")

    lines = []
    lines.append("# Nationals qualification report")
    lines.append("")
    lines.append(f"- **Commit** `{sha}`")
    lines.append(f"- **Branch** `{branch}`")
    lines.append(f"- **Generated** {now()}")
    lines.append(f"- **Build target for every validation launch** `{BUILD_TARGET}`"
                 + ("" if BUILD_TARGET == "Win64" else
                    " ⚠️ **NOT the nationals target.** `CLAUDE.md` § 7: this machine has no "
                    "Windows Standalone module, so the shipped player has to be validated and "
                    "built on one that does."))
    lines.append(f"- **Gate** {'NATIONALS CANDIDATE (PlayMode twice)' if nationals else 'standard pass'}")
    if len(shas) > 1:
        lines.append(f"- ⚠️⚠️ **THE STAGES DO NOT AGREE ON A COMMIT**: {', '.join(x[:12] for x in shas)}. "
                     f"A qualification stitched from two commits describes neither.")
    if moved:
        lines.append(f"- HEAD has since moved to `{moved[:12]}`, which is ordinary: recording a "
                     f"qualification is itself a commit. The stages above all ran on the commit "
                     f"named at the top.")
    lines.append(f"- **Working tree** `{tree_state}`"
                 + (f", {len(dirty)} tracked path(s)" if dirty else ""))
    lines.append("")

    verdicts = []
    lines.append("| Stage | Verdict | Detail |")
    lines.append("|---|---|---|")

    # ⚠️⚠️ THE TREE IS A STAGE NOW AND NOT A NOTE, WHICH IS THE WHOLE OF `docs/TODO.md` § 145.1.
    # It sits FIRST because it is the claim every other row rests on: a green PlayMode suite on a
    # tree that is not the commit above proves something about source nobody can check out.
    if tree_state == "clean":
        verdicts.append(True)
        lines.append("| Working tree | PASS | `git status --porcelain` is empty of tracked "
                     "changes, so the source tested IS the commit above |")
    elif tree_state == "dirty":
        verdicts.append(False)
        added = [d for d in dirty if d.startswith("??")]
        edited = [d for d in dirty if not d.startswith("??")]
        # ⚠️ THE TWO HALVES ARE NAMED SEPARATELY BECAUSE THEY NEED DIFFERENT ACTIONS. An edited
        # tracked file is committed or stashed; an untracked source file has never been in the
        # history at all and is the half this gate used to ignore outright (§ 145.9).
        parts = []
        if edited:
            parts.append(f"{len(edited)} tracked path(s) edited: "
                         + ", ".join(d[3:] for d in edited[:5])
                         + (" ..." if len(edited) > 5 else ""))
        if added:
            parts.append(f"{len(added)} untracked SOURCE path(s), which compile or ship and are "
                         f"in no commit: " + ", ".join(d[3:] for d in added[:5])
                         + (" ..." if len(added) > 5 else ""))
        lines.append(f"| Working tree | **DIRTY** | {'; '.join(parts)} against `{sha[:12]}`. "
                     f"**The results below describe source that is not in the history.** |")
    else:
        verdicts.append(False)
        lines.append(f"| Working tree | **UNKNOWN** | the checkout state could not be "
                     f"established ({dirty[0] if dirty else 'no reason given'}). Not the same as "
                     f"clean, and not certifiable. |")

    for name in required:
        s = stages.get(name)
        if s is None:
            verdicts.append(False)
            lines.append(f"| {LABEL[name]} | **NOT RUN** | no `Logs/qualify/{name}.json`. "
                         f"A stage that did not run does not pass by absence. |")
            continue
        if s.get("sha") != sha:
            verdicts.append(False)
            lines.append(f"| {LABEL[name]} | **WRONG COMMIT** | ran at "
                         f"`{(s.get('sha') or '?')[:12]}`, HEAD is `{sha[:12]}` |")
            continue
        ok = bool(s.get("ok"))
        verdicts.append(ok)
        detail = s.get("reason", "")
        if name.startswith("playmode") or name == "editmode":
            detail = (f"total {s.get('total')}, passed {s.get('passed')}, "
                      f"failed {s.get('failed')}, skipped {s.get('skipped')}. {detail}")
        lines.append(f"| {LABEL[name]} | {'PASS' if ok else '**FAIL**'} | {detail} |")

    lines.append("")
    ok_all = all(verdicts)

    # ⚠️⚠️ THREE VERDICTS RATHER THAN TWO, AND THE THIRD IS THE ONE THAT USED TO BE SILENT.
    # `NON-QUALIFIABLE` is what a report says when it could not establish the thing it is
    # certifying. Folding it into NOT QUALIFIED would tell somebody a test failed when none did,
    # and folding it into QUALIFIED is the fault § 145.1 is about.
    if ok_all:
        verdict = "QUALIFIED"
    elif tree_state == "unknown" and all(verdicts[1:]):
        verdict = "NON-QUALIFIABLE"
    else:
        verdict = "NOT QUALIFIED"

    lines.append(f"## Verdict: {verdict}")
    lines.append("")

    if verdict == "NON-QUALIFIABLE":
        lines.append("⚠️⚠️ **EVERY STAGE PASSED AND THE SOURCE COULD NOT BE TIED TO A COMMIT.** "
                     "That is not a test failure and it is not a certification either: the "
                     "results are real and there is no way to say what they are results ABOUT. "
                     "Run this on a machine with `git` on PATH, in a checkout rather than an "
                     "export.")
        lines.append("")
    elif tree_state == "dirty":
        lines.append("⚠️⚠️ **THE WORKING TREE IS DIRTY, SO THIS IS NOT A CERTIFICATION HOWEVER "
                     "GREEN THE STAGES ARE.** A local dirty BUILD is a legitimate thing to make "
                     "at a venue at 8 a.m. and nothing stops you (`GameBuilder` records the "
                     "state and builds anyway). What cannot happen is a report claiming a commit "
                     "for source that is not in it. Commit or stash, then re-run.")
        lines.append("")

    if not ok_all and verdict == "NOT QUALIFIED":
        lines.append("A stage above is red or missing. **A green subset is not a release "
                     "certification**: the full PlayMode suite has been 42 red and then 56 red "
                     "on commits where every targeted run anybody had bothered with was green.")
        lines.append("")

    ident = stages.get("identity") or {}
    lines.append("## Identity")
    lines.append("")
    lines.append(f"- `NetSession.ProtocolVersion` = **{ident.get('protocol_version')}** "
                 f"(read from source, never from a document)")
    lines.append(f"- Application version = **{ident.get('application_version')}**")
    ugs = ident.get("ugs") or {}
    lines.append(f"- UGS project `{ugs.get('project_id')}`, organization "
                 f"`{ugs.get('organization')}`")
    arts = ident.get("artifacts") or {}
    for platform in ("windows", "android"):
        a = arts.get(platform)
        if not a:
            lines.append(f"- {platform.title()} artifact: **none on this machine**")
            continue
        stamp = a.get("identity") or {}
        lines.append(f"- {platform.title()} artifact `{a['path']}` written {a['written']}, "
                     f"SHA `{(stamp.get('sha') or 'UNSTAMPED')[:12]}`, "
                     f"protocol `{stamp.get('protocol', '?')}`, tree "
                     f"`{stamp.get('treeState') or 'unknown'}`, target "
                     f"`{stamp.get('target') or '?'}`, app `{stamp.get('appVersion') or '?'}`, "
                     f"UGS `{stamp.get('ugsProject') or '(blank)'}` / "
                     f"`{stamp.get('ugsEnvironment') or '(blank)'}`")
    if ident.get("problems"):
        lines.append("")
        for p in ident["problems"]:
            lines.append(f"- ⚠️ {p}")

    # ⚠️⚠️ THE CANDIDATE ROWS ARE PRINTED APART FROM THE CONSISTENCY ONES BECAUSE THEY ANSWER A
    # DIFFERENT QUESTION. "Is anything here inconsistent" is worth knowing on any run; "would
    # this exact artifact be the one carried to General Santos" is only asked of a candidate,
    # and a development pass is allowed to fail it with every test green. § 145.10.
    if ident.get("candidate_problems"):
        lines.append("")
        lines.append("**Not a release candidate:**")
        for p in ident["candidate_problems"]:
            lines.append(f"- ⚠️⚠️ {p}")
    lines.append("")

    for name in required:
        s = stages.get(name) or {}
        fails = s.get("failures") or []
        if fails:
            lines.append(f"### {LABEL[name]}: {len(fails)} failing")
            lines.append("")
            for f in fails[:80]:
                lines.append(f"- `{f['name']}`  {f['message']}")
            if len(fails) > 80:
                lines.append(f"- ... and {len(fails) - 80} more, in `{s.get('path')}`")
            lines.append("")

    audits = (stages.get("audits") or {}).get("audits") or []
    if audits:
        lines.append("### Source audits")
        lines.append("")
        lines.append("| Audit | Verdict | Summary |")
        lines.append("|---|---|---|")
        for a in audits:
            verdict = ("OK" if a["ok"]
                       else ("**FINDINGS**" if a.get("gating", True) else "findings (not gating)"))
            lines.append(f"| `{a['script']}` | {verdict} | {a.get('summary', '')} |")
        lines.append("")

    checks = stages.get("checks") or {}
    if checks.get("report"):
        lines.append("### Editor checks")
        lines.append("")
        lines.append("```")
        lines.append(checks["report"])
        lines.append("```")
        lines.append("")

    REPORTS.mkdir(parents=True, exist_ok=True)
    out = REPORTS / f"qualification-{sha[:12]}.md"
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")

    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "summary.json").write_text(json.dumps({
        "sha": sha, "branch": branch, "generated": now(), "nationals": nationals,
        "verdict": verdict, "qualified": ok_all,
        "tree_state": tree_state, "dirty": dirty,
        "stages": {n: {"ok": bool((stages.get(n) or {}).get("ok")),
                       "ran": stages.get(n) is not None,
                       "sha": (stages.get(n) or {}).get("sha")} for n in ORDER},
        "identity": {k: ident.get(k) for k in
                     ("protocol_version", "application_version", "ugs", "artifacts")},
    }, indent=2), encoding="utf-8")

    print("\n".join(lines))
    print(f"\nwritten: {out}")
    return ok_all


# --------------------------------------------------------------------------

def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--stage", choices=ORDER + ["playmode", "report"], help="run one stage")
    ap.add_argument("--nationals", action="store_true",
                    help="the full gate: every stage, PlayMode twice, then the report")
    ap.add_argument("--candidate", action="store_true",
                    help="apply the release-candidate artifact rules to --stage identity: a "
                         "clean stamped tree, the nationals build target, and a UGS identity "
                         "that matches this source. Implied by --nationals")
    args = ap.parse_args()

    if args.nationals:
        sha_before = head_sha()
        stage_core()
        stage_editmode()
        stage_playmode(1)
        stage_playmode(2)
        stage_checks()
        stage_audits()
        stage_identity(candidate=True)
        if head_sha() != sha_before:
            print(f"REFUSED: HEAD moved from {sha_before} to {head_sha()} during the run. "
                  f"These results describe two different commits.", file=sys.stderr)
            return 2
        return 0 if stage_report(nationals=True) else 1

    if not args.stage:
        ap.print_help()
        return 0

    if args.stage == "report":
        return 0 if stage_report(nationals=False) else 1

    fn = {"core": stage_core, "editmode": stage_editmode,
          "playmode": lambda: stage_playmode(1),
          "playmode1": lambda: stage_playmode(1),
          "playmode2": lambda: stage_playmode(2),
          "checks": stage_checks, "audits": stage_audits,
          "identity": lambda: stage_identity(candidate=args.candidate)}[args.stage]
    result = fn()
    print(json.dumps(result, indent=2)[:4000])
    return 0 if result.get("ok") else 1


if __name__ == "__main__":
    sys.exit(main())
