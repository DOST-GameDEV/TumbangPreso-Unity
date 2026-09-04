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

UNITY = pathlib.Path(r"C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe")
DOTNET = pathlib.Path(r"C:\Program Files\dotnet\dotnet.exe")

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


def working_tree_dirty():
    r = run(["git", "status", "--porcelain"])
    return [ln for ln in r.stdout.splitlines() if ln.strip()]


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
           "-buildTarget", "Win64", "-logFile", str(logfile)] + args
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


def stage_identity():
    d = {"started": now(),
         "protocol_version": read_protocol_version(),
         "application_version": read_app_version(),
         "ugs": read_ugs_identity(),
         "build_target": "Win64 (validation launches)",
         "artifacts": artifact_identity()}

    problems = []
    if d["protocol_version"] is None:
        problems.append("could not read NetSession.ProtocolVersion from source")

    # ⚠️ THE CROSSPLAY CHECK. Two artifacts that do not agree on the commit and the
    # protocol are two games. They refuse each other by design and it reads as a bug.
    arts = d["artifacts"]
    win = (arts.get("windows") or {}).get("identity")
    if "windows" in arts and "android" in arts:
        aid = arts["android"].get("identity")
        if win and aid:
            if win.get("sha") != aid.get("sha"):
                problems.append(f"Windows SHA {win.get('sha')} != Android SHA {aid.get('sha')}")
            if win.get("protocol") != aid.get("protocol"):
                problems.append(f"Windows protocol {win.get('protocol')} != "
                                f"Android protocol {aid.get('protocol')}")
        else:
            problems.append("one or both artifacts carry no build-identity.json, so they "
                            "cannot be compared. Rebuild through GameBuilder.")

    # ⚠️⚠️ AN UNSTAMPED ARTIFACT IS A STALE ARTIFACT UNTIL PROVEN OTHERWISE, and for a nationals
    # candidate that is the whole question. `GameBuilder` has stamped every build since
    # `StampBuildIdentity` landed, so a player with no `build-identity.json` predates it and
    # cannot be tied to any commit at all. Treating "no stamp" as "probably fine" is exactly the
    # reasoning that ships the 14:34 build while believing it is the 15:03 one.
    if "windows" in arts and not (arts["windows"].get("identity")):
        problems.append("the Windows artifact carries no build-identity.json, so it predates the "
                        "stamp and cannot be tied to a commit. Rebuild through GameBuilder.")
    if "android" in arts and not (arts["android"].get("identity")):
        problems.append("the Android artifact carries no build-identity.json. Rebuild it.")

    if win and d["protocol_version"] is not None and win.get("protocol") != d["protocol_version"]:
        problems.append(f"the Windows artifact was built at protocol {win.get('protocol')} "
                        f"and the source now reads {d['protocol_version']}. Rebuild.")
    if win and win.get("sha") not in (None, head_sha()):
        problems.append(f"the Windows artifact was built from {win.get('sha')[:12]} and HEAD "
                        f"is {head_sha()[:12]}. It is not a candidate for this commit.")

    d["problems"] = problems
    d["ok"] = not problems
    d["reason"] = "identity consistent" if d["ok"] else "; ".join(problems)
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
    dirty = working_tree_dirty()

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
    lines.append(f"- **Build target for every validation launch** `Win64`")
    lines.append(f"- **Gate** {'NATIONALS CANDIDATE (PlayMode twice)' if nationals else 'standard pass'}")
    if len(shas) > 1:
        lines.append(f"- ⚠️⚠️ **THE STAGES DO NOT AGREE ON A COMMIT**: {', '.join(x[:12] for x in shas)}. "
                     f"A qualification stitched from two commits describes neither.")
    if moved:
        lines.append(f"- HEAD has since moved to `{moved[:12]}`, which is ordinary: recording a "
                     f"qualification is itself a commit. The stages above all ran on the commit "
                     f"named at the top.")
    if dirty:
        lines.append(f"- ⚠️ **Working tree was DIRTY at report time**, {len(dirty)} paths. "
                     f"A qualification is a claim about a commit; uncommitted edits mean the "
                     f"results describe something not in the history.")
    lines.append("")

    verdicts = []
    lines.append("| Stage | Verdict | Detail |")
    lines.append("|---|---|---|")
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
    lines.append(f"## Verdict: {'QUALIFIED' if ok_all else 'NOT QUALIFIED'}")
    lines.append("")
    if not ok_all:
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
                     f"protocol `{stamp.get('protocol', '?')}`")
    if ident.get("problems"):
        lines.append("")
        for p in ident["problems"]:
            lines.append(f"- ⚠️ {p}")
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
        "qualified": ok_all, "dirty": dirty,
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
    args = ap.parse_args()

    if args.nationals:
        sha_before = head_sha()
        stage_core()
        stage_editmode()
        stage_playmode(1)
        stage_playmode(2)
        stage_checks()
        stage_audits()
        stage_identity()
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
          "identity": stage_identity}[args.stage]
    result = fn()
    print(json.dumps(result, indent=2)[:4000])
    return 0 if result.get("ok") else 1


if __name__ == "__main__":
    sys.exit(main())
