#!/usr/bin/env python3
"""
The PlayMode suite as a GATE: isolated groups, one launch each, one aggregated verdict.

WHY THIS EXISTS
---------------
`docs/TODO.md` § 126.8 measured the full single-process PlayMode run three times and
this pass measured it a fourth:

    run 1  155 cases, 113 passed, 42 failed
    run 2  155 cases, 114 passed, 41 failed   (eleven suites swapped sides)
    run 3  155 cases,  99 passed, 56 failed
    run 4  165 cases, 107 passed, 50 failed   (e85b0fc, this pass)

"THE COUNT BARELY MOVED AND THE RED SET LARGELY CHANGED, WHICH IS THE FINDING.
A gate whose red set moves is not measuring the code."

The cause is not fifty defects. It is cross-fixture lifetime leakage: objects,
scenes, overlays and one cloud session outliving the test that made them. The
fourth run's clearest single piece of evidence is a SETTINGS test failing with
CarryTests' assertion message ("a held slipper drifted 7.945 m from the hand"),
which is one fixture being blamed for another's leak.

§ 126.8 named exactly two ways out and built neither:

  1. every fixture tears its world down  - attempted in § 126.8d, measured moving
     eleven failures from one side to the other, and WITHDRAWN. Its own note says
     what the right version needs first: "a measurement nobody has taken: WHICH
     persistent object a match install depends on."
  2. the suite is declared to run in named groups, and a single-process full run
     stops being quoted as a gate at all.

THIS IS (2). It needs no measurement of persistent objects because it removes the
question: a fixture cannot inherit a world from a fixture in a different process.

WHAT MAKES IT A GATE RATHER THAN AN EXCUSE
------------------------------------------
⚠️⚠️ A GROUP IS AN ISOLATION BOUNDARY, NEVER AN EXEMPTION. § 126.8d bans a third
category exclusion by name, because a category meaning "these tests do not work
next to each other" hides the finding rather than recording it. So:

  - EVERY discovered fixture is in exactly ONE group. A fixture in none is a
    failure; a fixture in two is a failure. Both are checked before anything runs.
  - Coverage is asserted against the RESULTS, not only the plan: every fixture in
    a group must actually appear in that group's xml. A group that silently ran
    nothing fails instead of passing, which is § 126.8c's total="0" made impossible
    at the aggregate level as well as the per-run one.
  - The aggregate is the number quoted. Not the best group, not a subset.

USAGE
-----
  python tools/playmode_suite.py --plan            # print the partition, run nothing
  python tools/playmode_suite.py --group match     # one group
  python tools/playmode_suite.py --gate            # every group, then the verdict
  python tools/playmode_suite.py --gate --twice    # the nationals gate
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
TESTS = ROOT / "Assets" / "TumbangPreso" / "Tests" / "PlayMode"
LOGS = ROOT / "Logs"
OUT = LOGS / "playmode-suite"

# ⚠️⚠️ RESOLVED PER MACHINE. `CLAUDE.md` § 7 has three machines in its table and two of them are
# not Windows, and its own warning applies to this line as written: *"a note that is true on one
# machine and written as a fact about 'here' sends whoever is on another one hunting."* As a
# Windows literal this gate could not run at all on the Mac, and every group reported the launch
# failure as the GROUP failing, which is § 143.1's whole complaint about a suite whose red set
# is not about the code.
UNITY_VERSION = "6000.5.8f1"

UNITY = next(
    (pathlib.Path(c) for c in (
        rf"C:\Program Files\Unity\Hub\Editor\{UNITY_VERSION}\Editor\Unity.exe",
        f"/Applications/Unity/Hub/Editor/{UNITY_VERSION}/Unity.app/Contents/MacOS/Unity",
        os.path.expanduser(
            f"~/Applications/Unity/Hub/Editor/{UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"),
    ) if pathlib.Path(c).exists()),
    pathlib.Path("Unity"))

# ⚠️ THE TARGET FOLLOWS THE MACHINE AND IS STILL ALWAYS STATED. `run_group`'s note is right that a
# silent launch inherits whatever the last build left the editor under; what was wrong was
# stating Win64 as a constant on a machine with no Windows Standalone module to switch to.
BUILD_TARGET = "OSXUniversal" if sys.platform == "darwin" else "Win64"
NAMESPACE = "TumbangPreso.PlayTests"

CATEGORIES = "!WallClock;!ThumbFloor"

# --------------------------------------------------------------------------
# THE PARTITION
#
# ⚠️⚠️ EVERY GROUP CARRIES THE REASON ITS MEMBERS CANNOT SHARE A PROCESS WITH THE
# OTHERS. A partition with no reasons is a partition somebody reshuffles to make a
# number go green, which is the thing this file exists to prevent.
# --------------------------------------------------------------------------

GROUPS = [
    ("destroyer", """
        ⚠️⚠️ `InputSurfaceProbe` ALONE, AND `CLAUDE.md` § 7 ALREADY SAID SO IN CAPITALS:
        "RUN IT ALONE. It loads every scene in the build settings and opens every overlay it
        can discover, so it is the most destructive fixture in the suite: in a twelve-suite
        run it took most of the group down with it and the numbers were meaningless."
        § 126.8b sorted a whole run by start time and found it clean for 57 cases, then this
        fixture's five, then 44 failures out of the remaining 97. It is one fixture and it is
        its own group.
     """, [
        "InputSurfaceProbe",
    ]),

    ("screens", """
        Everything that builds a menu, an overlay or a screen. They contend for the same
        canvases, the same EventSystem and the same `DontDestroyOnLoad` chrome
        (`MatchResult`, `PausePanel`, `BootSting`), and § 126.8d measured that isolating
        them from the match fixtures turns every one of them green.
     """, [
        "AspectRatioProbes", "CustomCharacterScreenProbe", "CustomGameScreenProbe",
        "HeroPickerLayoutProbe", "HudLayoutProbe", "HudOverflowProbe", "LoadoutSurfaceProbe",
        "LobbyChatProbe", "LobbyChatStripProbe", "LobbyStyleProbe", "LobbyTypingProbe",
        "NestedCanvasProbe", "NetworkedLobbyTypingProbe", "PaperPurityProbe",
        "PhaseSurfaceLayoutProbe", "PlayerHubLayoutProbe", "PreviewDragProbe",
        "QueueCardLayoutProbe", "SettingsScrollProbe", "SettingsWheelProbe",
        "UiClickProbe", "UiRuntimeShots", "WardrobeSheetProbe", "ModelPreviewProbe",
        "ModelPreviewTests", "MatchRecordIdentityProbe",
    ]),

    ("match", """
        Everything that installs an arena and runs a round. `MapPreviewSurface` loads arenas
        ADDITIVELY AND CACHES THEM (§ 126.8b), so a cached map landing inside a screen suite
        brings a whole arena's lights, cameras and post stack with it, and the reverse leaves
        a match fixture with no main camera. Keeping the two apart is the same boundary read
        from either side.
     """, [
        "AiLaneTests", "ArenaBoundsProbe", "BotMotionProbe", "CarriedSlipperSelfHideProbe",
        "CarryTests", "EmoteCameraProbe", "EmoteLifecycleProbe", "FppFrameProbe",
        "RetrievalSlideTests",
        "FppOccluderProbe", "LandedHighlightTests", "LataFloatProbe", "MatchRunTests",
        "MultiplayerModelSwapSelfHideProbe", "ScoreWitnessProbe", "SeatAnnouncementTests",
        "SessionRestartTests", "SoloPracticeTests", "SteeringTests", "StunFrostTests",
        "TrainingStreetProbe", "TutorialDefenderProbe", "TutorialLessonHonestyProbe",
        "VolcanicZoneTests", "InputEdgeTests", "InputReaderTests", "MatchSoakProbe", "ScoreIdempotencyTests",
    ]),

    ("capture", """
        The fixtures whose job is to photograph something. They replace cameras, drive the
        render pipeline and write files, and a replaced camera is what several of the
        `MissingReferenceException: the object of type 'UnityEngine.Camera' has been
        destroyed` failures are holding.
     """, [
        "AntiAliasStateProbe", "CosmeticSurfaceProbe", "GameplayShots", "ModelFacingProbe",
        "MsaaResolveProbe", "NationalsShowcaseProbe", "NearFadeProbe", "ToneSweep",
        "WorldOutlineCoverageProbe", "MatchFrameRateProbe", "HudPerformanceProbe",
        "ReplayCaptureProbe",
    ]),

    ("services", """
        ⚠️ THE ONLY GROUP WHOSE STATE LIVES ON SOMEBODY ELSE'S SERVER, so "tear the world
        down" does not reach it and neither does isolation from this side. § 126.8's own note:
        six of its cases went red in one run having passed in the previous one with nothing
        changed that touches authentication. In batch mode `NetIdentity` refuses UGS sign-in
        by design and these report SKIPPED with the reason.
     """, [
        "UgsServicesProbe", "OnlineSignInProbe", "CloudEndpointActionProbe",
    ]),

    ("bots", """
        The long seeded probes. `BotBehaviourProbe` runs whole matches in both modes on two
        maps and is minutes rather than seconds; it shares nothing with the screens and would
        otherwise dominate a group's wall time.
     """, [
        "BotBehaviourProbe", "AiDiagnosticProbe",
    ]),
]


def discover_fixtures():
    """
    Every PlayMode fixture, read out of the source rather than listed.

    ⚠️⚠️ DISCOVERED, NOT ENUMERATED, AND `CLAUDE.md` § 4a SAYS WHY IN THE INPUT LAYER'S OWN
    WORDS: "`InputSurfaceProbe` DISCOVERS SCREENS INSTEAD OF LISTING THEM ... `UiClickProbe`
    still carries a hard-coded list of five screens and is the § 124.11 fault pre-installed."
    A partition over a hand-written list silently stops covering a fixture added next week,
    and the failure mode is the gate going green over less than it did yesterday.
    """
    found = {}
    for path in sorted(TESTS.glob("*.cs")):
        text = path.read_text(encoding="utf-8", errors="replace")

        # A fixture is a class that carries at least one test attribute.
        #
        # ⚠️⚠️ THE ATTRIBUTE IS NOT ALWAYS ALONE IN ITS BRACKETS, AND THE FIRST VERSION OF THIS
        # LINE REQUIRED IT TO BE. `BotBehaviourProbe` writes `[UnityTest, Timeout(MatchTimeoutMs)]`
        # on every one of its five cases, so an exact `[UnityTest]` match discovered 67 fixtures
        # where there are 68 and dropped the longest-running probe in the suite. **The coverage
        # check is what caught it**, by noticing a planned fixture that discovery said did not
        # exist, which is the whole argument for asserting the partition against the source
        # instead of trusting a list.
        if not re.search(r"\[\s*(Test|UnityTest)\s*[,\]]", text):
            continue

        for m in re.finditer(r"^\s*(?:public\s+)?(?:sealed\s+|partial\s+|abstract\s+)*class\s+(\w+)",
                             text, re.M):
            name = m.group(1)
            # An abstract base carries no cases of its own.
            if re.search(rf"abstract\s+class\s+{name}\b", text):
                continue
            found.setdefault(name, path.name)

    return found


EXCLUDED_CATEGORIES = {"WallClock", "ThumbFloor"}


def strip_comments_cs(text):
    """C# with its comments blanked in place, so line numbers survive."""
    out, i, n = [], 0, len(text)
    while i < n:
        if text[i] == '"':
            out.append(text[i]); i += 1
            while i < n:
                if text[i] == "\\":
                    out.append("  "); i += 2; continue
                out.append(text[i])
                if text[i] == '"':
                    i += 1; break
                i += 1
            continue
        if text.startswith("//", i):
            j = text.find("\n", i); j = n if j < 0 else j
            out.append(" " * (j - i)); i = j; continue
        if text.startswith("/*", i):
            j = text.find("*/", i); j = n if j < 0 else j + 2
            out.append(" " * (j - i)); i = j; continue
        out.append(text[i]); i += 1
    return "".join(out)

TEST_ATTRIBUTE = re.compile(r"\[\s*(?:Test|UnityTest)\b[^\]]*\]")
# ⚠️ BOTH SPELLINGS. `AiDiagnosticProbe` writes `[Category(WallClock)]` against a const
# rather than a string literal, and a regex that only knew the quoted form reported it
# as a fixture that went missing.
CATEGORY = re.compile(r'Category\(\s*"?(\w+)"?\s*\)')


def fixtures_silenced_by_category():
    """
    Fixtures whose every case is excluded by `CATEGORIES`, and which must therefore report
    nothing.

    ⚠️⚠️ WITHOUT THIS THE COVERAGE CHECK CAN NEVER GO GREEN, AND THE FIRST FULL GATE RUN
    PROVED IT: seven fixtures were reported as "never ran" and all seven are
    `[Category("WallClock")]`, which the shipped filter excludes on purpose.
    `AiDiagnosticProbe` runs a round at 1x for about eighty real seconds and its verdict
    depends on how busy the machine is (`CLAUDE.md` § 7); the other six photograph things
    at real time for the same reason.

    **A gate that cannot be green is a gate that gets ignored**, which is the failure this
    whole file exists to prevent, so the check has to know the difference between a fixture
    that was silenced deliberately and one that went missing.

    ⚠️ IT IS DERIVED FROM THE SOURCE AND NOT LISTED, for `discover_fixtures`' reason: a
    hand-written list of exclusions stops being true the moment somebody adds or removes a
    category, and it fails in the direction that hides a missing fixture.
    """
    silenced = {}

    for path in sorted(TESTS.glob("*.cs")):
        # ⚠️⚠️ COMMENTS STRIPPED FIRST. `MsaaResolveProbe`'s class note contains the words
        # "A `[UnityTest]` coroutine resumes", and counting that as a case made the fixture
        # look like it had one unexcluded test and therefore should have run. This is
        # `audit_audio_reach.py`'s lesson exactly: it "was the only audit that did not strip
        # comments before looking for a gate", so a comment ABOUT a gate registered as one.
        text = strip_comments_cs(path.read_text(encoding="utf-8", errors="replace"))
        lines = text.splitlines()

        # A category on the CLASS applies to every case in it.
        class_wide = set()
        for m in re.finditer(r"^\s*(?:public\s+)?(?:sealed\s+|partial\s+)*class\s+(\w+)",
                             text, re.M):
            line_no = text[:m.start()].count("\n")
            window = "\n".join(lines[max(0, line_no - 6):line_no])
            class_wide.update(CATEGORY.findall(window))

        cases = 0
        excluded = 0
        for m in TEST_ATTRIBUTE.finditer(text):
            cases += 1
            line_no = text[:m.start()].count("\n")
            # ⚠️ THE WHOLE CONTIGUOUS ATTRIBUTE BLOCK, IN BOTH DIRECTIONS. A window of
            # PRECEDING lines found one fixture of the seven: this codebase writes
            # `[UnityTest]` and then `[Category("WallClock")]` on the NEXT line, so the
            # category that silences a case sits after the attribute that names it.
            first = line_no
            while first > 0 and lines[first - 1].strip().startswith("["):
                first -= 1
            last = line_no
            while last + 1 < len(lines) and lines[last + 1].strip().startswith("["):
                last += 1

            window = "\n".join(lines[first:last + 1])
            found = set(CATEGORY.findall(window)) | class_wide
            if found & EXCLUDED_CATEGORIES:
                excluded += 1

        if cases > 0 and excluded == cases:
            for m in re.finditer(r"^\s*(?:public\s+)?(?:sealed\s+|partial\s+)*class\s+(\w+)",
                                 text, re.M):
                silenced[m.group(1)] = sorted(class_wide | EXCLUDED_CATEGORIES & set(
                    CATEGORY.findall(text)))

    return silenced


def partition_problems(fixtures):
    """Every way the plan can be wrong, checked before a single launch is paid for."""
    problems = []
    placed = {}

    for group, _why, members in GROUPS:
        for name in members:
            if name in placed:
                problems.append(f"{name} is in both '{placed[name]}' and '{group}'")
            placed[name] = group

    for name in sorted(fixtures):
        if name not in placed:
            problems.append(f"{name} ({fixtures[name]}) is in NO group. Every fixture runs "
                            f"exactly once: add it to the group whose world it shares.")

    for name in sorted(placed):
        if name not in fixtures:
            problems.append(f"{name} is in group '{placed[name]}' and no longer exists in "
                            f"{TESTS.name}/. Remove it, or the plan is describing a suite "
                            f"that is not there.")

    return problems


def run_group(group, members, log_suffix=""):
    """
    One Unity launch, one group.

    ⚠️⚠️ `-testFilter` IS SEMICOLON SEPARATED. A comma-joined list is read as one impossible
    name, matches nothing, and writes a well formed xml saying result="Passed" total="0" in
    thirteen seconds. § 126.8c lost a whole launch to exactly this and it is indistinguishable
    from the other cause of the same file.

    ⚠️ `-buildTarget Win64` ON EVERY LAUNCH. An Android build leaves the editor under
    UNITY_ANDROID and the next unqualified launch compiles the suite against those defines.

    ⚠️ NO `-nographics`. Unity selects NullGfxDevice, the first offscreen camera dies inside
    it, no xml is written, and the process still exits 0.
    """
    OUT.mkdir(parents=True, exist_ok=True)
    xml = OUT / f"{group}{log_suffix}.xml"
    log = OUT / f"{group}{log_suffix}.log"
    if xml.exists():
        xml.unlink()

    started = datetime.datetime.now().timestamp()
    test_filter = ";".join(f"{NAMESPACE}.{m}" for m in members)

    cmd = [str(UNITY), "-batchmode", "-runTests", "-projectPath", str(ROOT),
           "-buildTarget", BUILD_TARGET, "-testPlatform", "PlayMode",
           "-testCategory", CATEGORIES, "-testFilter", test_filter,
           "-testResults", str(xml), "-logFile", str(log)]

    proc = subprocess.run(cmd, cwd=str(ROOT), capture_output=True, text=True, errors="replace")
    return read_group_xml(group, xml, started, members, proc.returncode)


def read_group_xml(group, xml, started, members, exit_code):
    row = {"group": group, "xml": str(xml), "exit": exit_code, "expected": sorted(members),
           "failures": [], "ran_fixtures": [], "missing_fixtures": []}

    if not xml.exists():
        row["ok"] = False
        row["reason"] = "NO XML. The launch wrote nothing; a PlayMode crash still exits 0."
        return row

    if xml.stat().st_mtime < started - 1:
        row["ok"] = False
        row["reason"] = "STALE XML, written before this launch started."
        return row

    try:
        root = ET.parse(str(xml)).getroot()
    except Exception as e:
        row["ok"] = False
        row["reason"] = f"UNPARSEABLE XML: {e}"
        return row

    def num(a):
        try:
            return int(root.get(a))
        except (TypeError, ValueError):
            return None

    row.update(total=num("total"), passed=num("passed"), failed=num("failed"),
               skipped=num("skipped"), duration=root.get("duration"))

    ran = set()
    for case in root.iter("test-case"):
        full = case.get("fullname") or ""
        m = re.match(rf"{re.escape(NAMESPACE)}\.(\w+)", full)
        if m:
            ran.add(m.group(1))
        if case.get("result") == "Failed":
            msg = ""
            f = case.find("failure")
            if f is not None:
                t = f.find("message")
                if t is not None and t.text:
                    msg = " ".join(t.text.split())[:300]
            row["failures"].append({"name": full, "message": msg})

    row["ran_fixtures"] = sorted(ran)

    # ⚠️⚠️ COVERAGE IS ASSERTED AGAINST THE RESULTS AND NOT AGAINST THE PLAN. This is the check
    # that makes a group honest: a filter typo, a renamed fixture or a fixture whose cases were
    # all excluded by category produces a green run over less than it claimed, and nothing else
    # in the pipeline would notice. § 126.8c is the same fault one level down.
    silenced = fixtures_silenced_by_category()
    missing = [m for m in sorted(members) if m not in ran and m not in silenced]
    row["missing_fixtures"] = missing
    row["silenced_fixtures"] = [m for m in sorted(members) if m in silenced]

    if row["total"] in (None, 0):
        row["ok"] = False
        row["reason"] = ('ZERO TESTS. A well formed green xml over nothing. Either the filter '
                         'matched no fixture, or something destroyed the runner mid-run.')
        return row

    if missing:
        row["ok"] = False
        row["reason"] = (f"{len(missing)} planned fixture(s) never reported a case: "
                         f"{', '.join(missing)}. A group that runs less than it claims is a "
                         f"green run over a smaller suite.")
        return row

    row["ok"] = not row["failed"]
    row["reason"] = (f"{row['passed']} passed of {row['total']}" if row["ok"]
                     else f"{row['failed']} failed of {row['total']}")
    return row


def aggregate(rows, fixtures):
    total = sum(r.get("total") or 0 for r in rows)
    passed = sum(r.get("passed") or 0 for r in rows)
    failed = sum(r.get("failed") or 0 for r in rows)
    skipped = sum(r.get("skipped") or 0 for r in rows)

    ran = set()
    for r in rows:
        ran.update(r.get("ran_fixtures") or [])

    silenced = fixtures_silenced_by_category()
    never_ran = sorted(f for f in fixtures if f not in ran and f not in silenced)

    ok = all(r.get("ok") for r in rows) and not never_ran and total > 0
    return {
        "total": total, "passed": passed, "failed": failed, "skipped": skipped,
        "groups": rows, "fixtures_discovered": len(fixtures),
        "fixtures_ran": len(ran), "fixtures_never_ran": never_ran,
        "fixtures_silenced_by_category": sorted(silenced),
        "ok": ok,
    }


def head_sha():
    r = subprocess.run(["git", "rev-parse", "HEAD"], cwd=str(ROOT),
                       capture_output=True, text=True)
    return r.stdout.strip() if r.returncode == 0 else "UNKNOWN"


def print_summary(agg, label=""):
    print()
    print(f"PLAYMODE GATE {label}".rstrip())
    print(f"  commit            {head_sha()}")
    silenced = agg.get("fixtures_silenced_by_category") or []
    print(f"  fixtures          {agg['fixtures_ran']} ran of {agg['fixtures_discovered']} "
          f"discovered, {len(silenced)} silenced by category")
    print(f"  cases             {agg['total']} total, {agg['passed']} passed, "
          f"{agg['failed']} failed, {agg['skipped']} skipped")
    print()
    for r in agg["groups"]:
        print(f"  {'OK  ' if r.get('ok') else 'FAIL'}  {r['group']:<10} {r.get('reason', '')}")
    if silenced:
        print()
        print(f"  excluded by {'/'.join(sorted(EXCLUDED_CATEGORIES))} on purpose, so reporting "
              f"nothing is correct: {', '.join(silenced)}")
    if agg["fixtures_never_ran"]:
        print()
        print(f"  ⚠️  {len(agg['fixtures_never_ran'])} fixture(s) never ran: "
              f"{', '.join(agg['fixtures_never_ran'])}")
    print()
    print(f"  VERDICT: {'GREEN' if agg['ok'] else 'RED'}")
    print()

    for r in agg["groups"]:
        if r.get("failures"):
            print(f"  --- {r['group']} ---")
            for f in r["failures"]:
                print(f"    {f['name'].replace(NAMESPACE + '.', '')}")
                if f["message"]:
                    print(f"        {f['message'][:200]}")
            print()


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--plan", action="store_true", help="print the partition and check it")
    ap.add_argument("--group", help="run one group by name")
    ap.add_argument("--gate", action="store_true", help="run every group and aggregate")
    ap.add_argument("--twice", action="store_true",
                    help="the nationals gate: the whole thing, back to back, both green")
    args = ap.parse_args()

    fixtures = discover_fixtures()
    problems = partition_problems(fixtures)

    if args.plan or problems:
        print(f"{len(fixtures)} fixtures discovered in {TESTS}")
        for group, why, members in GROUPS:
            print(f"\n  {group}  ({len(members)})")
            print("   " + " ".join(why.split())[:400])
            for m in members:
                print(f"      {m}")
        if problems:
            print("\nPARTITION PROBLEMS, nothing was run:")
            for p in problems:
                print(f"  - {p}")
            return 2
        if args.plan:
            return 0

    OUT.mkdir(parents=True, exist_ok=True)

    if args.group:
        match = [g for g in GROUPS if g[0] == args.group]
        if not match:
            print(f"no such group: {args.group}", file=sys.stderr)
            return 2
        row = run_group(match[0][0], match[0][2])
        print(json.dumps(row, indent=2)[:4000])
        return 0 if row["ok"] else 1

    if not args.gate:
        ap.print_help()
        return 0

    passes = 2 if args.twice else 1
    results = []

    for attempt in range(1, passes + 1):
        suffix = f"-pass{attempt}" if passes > 1 else ""
        rows = [run_group(g, m, suffix) for g, _w, m in GROUPS]
        agg = aggregate(rows, fixtures)
        agg["pass"] = attempt
        agg["sha"] = head_sha()
        results.append(agg)
        print_summary(agg, f"pass {attempt} of {passes}" if passes > 1 else "")

        (OUT / f"summary{suffix}.json").write_text(json.dumps(agg, indent=2), encoding="utf-8")

        if not agg["ok"] and passes > 1:
            print("Second pass skipped: the first was red, and a nationals candidate has to be "
                  "green twice from a stable state.")
            break

    green = all(a["ok"] for a in results) and len(results) == passes

    # ⚠️ TWO GREEN RUNS THAT DISAGREE ABOUT WHAT THEY RAN ARE NOT TWO GREEN RUNS. § 126.8's
    # finding is a moving red set, so the gate compares the SHAPE of the two passes as well as
    # their verdicts.
    if green and passes > 1:
        if results[0]["total"] != results[1]["total"]:
            print(f"REFUSED: pass 1 ran {results[0]['total']} cases and pass 2 ran "
                  f"{results[1]['total']}. A gate whose case count moves is not measuring "
                  f"the code.")
            green = False

    (OUT / "gate.json").write_text(json.dumps(
        {"sha": head_sha(), "generated": datetime.datetime.now().isoformat(timespec="seconds"),
         "passes": results, "green": green}, indent=2), encoding="utf-8")

    return 0 if green else 1


if __name__ == "__main__":
    sys.exit(main())
