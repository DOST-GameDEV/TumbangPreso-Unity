#!/usr/bin/env python3
"""The harnesses' OWN verdicts and destructive paths, asserted without launching anything.

WHAT THIS AUDIT IS FOR
======================

The other `audit_*.py` files read the GAME's source and answer questions no test can. This one
reads the harnesses that grade the game, because three of them were caught doing the same thing
the game's own gates were caught doing: printing a failure and returning success.

  * `tools/net_matrix.py` could print `DIVERGED: ...` and exit 0. Its per-row `expect` string
    was prose that nothing read, so a shell running the disconnect matrix was told it passed.
  * `tools/qualify.py` dropped every untracked file, so an untracked `.cs` that COMPILES and an
    untracked `Resources/` asset that SHIPS both certified as `SHA X / tree clean`.
  * `tools/cold_start.py --clean-profile` used a FIXED backup directory and deleted whatever was
    already sitting on it. A pre-existing backup is what a crash, a power cut or an interrupted
    restore leaves behind, and in every one of those cases it is the only copy there is. This
    harness has already destroyed one real profile through a different fault in the same block.

WARNING: NONE OF THESE COULD BE ASSERTED BY RUNNING THE HARNESS, WHICH IS WHY THEY ARE HERE.
A net matrix row is fifteen minutes of two game processes; a cold start is a minute of a real
player and, for the destructive half, a real profile directory. Both are graded by pure
functions that take a parsed report or a path and answer a verdict, so the verdicts can be
driven over synthetic inputs in milliseconds. `tools/bot_sweep.py`'s comparison was verified the
same way before any Unity launch was spent on it.

WARNING: AND THE DESTRUCTIVE CASES RUN ON REAL DIRECTORIES, IN A TEMPORARY TREE. A test that
mocked `shutil.move` would prove the calls were made in the right order and nothing about what
is on disk afterwards, and "what is on disk afterwards" is the entire question.

    python tools/audit_harness_contracts.py
"""

import json
import os
import pathlib
import re
import shutil
import sys
import tempfile

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parent
sys.path.insert(0, str(HERE))

import cold_start        # noqa: E402
import net_matrix        # noqa: E402
import qualify           # noqa: E402

FINDINGS = []
CHECKS = [0]


def check(ok, what):
    CHECKS[0] += 1
    if not ok:
        FINDINGS.append(what)


# ---------------------------------------------------------------------------
# 1. The untracked-source rule, and the C# copy of it
# ---------------------------------------------------------------------------

SOURCE_PATHS = [
    "Assets/TumbangPreso/Runtime/SecretVerb.cs",
    "Assets/TumbangPreso/Runtime/SecretVerb.cs.meta",
    "Assets/TumbangPreso/Art/shaders/Toon2.shader",
    "Assets/TumbangPreso/Resources/UI/new_art.png",
    "Assets/Prefabs/Lata2.prefab",
    "Assets/Scenes/Eskinita2.unity",
    "Assets/StreamingAssets/rules.json",
    "Packages/com.tumbangpreso.core/Runtime/NewRule.cs",
    "Packages/manifest.json",
    "Core/Extra.cs",
    "Core.Tests/ExtraTests.cs",
    "ProjectSettings/ProjectSettings.asset",
    "tools/qualify.py",
    "ugs/cloud-code/match-record.js",
    "NewFolderInventedNextYear/thing.cs",
]

OUTPUT_PATHS = [
    "Logs/play.xml",
    "Logs/ui/character-select.png",
    "Library/ArtifactDB",
    "Temp/UnityLockfile",
    "Builds/macOS/TumbangPreso.app",
    "build/apk/tumbangpreso.apk",
    "obj/Debug/x.dll",
    "bin/Release/y.dll",
    "UserSettings/Layouts/default.dwlt",
    "scratchpad/patch_thing.py",
    "docs/reports/qualification-abc123456789.md",
]


def audit_untracked_rule():
    for path in SOURCE_PATHS:
        check(qualify.is_source_sensitive(path),
              f"qualify.is_source_sensitive({path!r}) is False. It compiles or ships, so a "
              f"report calling the tree clean certifies a commit that does not contain it.")

    for path in OUTPUT_PATHS:
        check(not qualify.is_source_sensitive(path),
              f"qualify.is_source_sensitive({path!r}) is True. It cannot reach a build, and a "
              f"gate that fails on it is a gate every developer learns to pass with a flag.")

    check(qualify.is_source_sensitive('"Assets/Space Name.cs"'),
          "git quotes a path with a space in it, and `git ls-files --others` is where those "
          "arrive from")
    check(not qualify.is_source_sensitive('"Logs/a b.log"'),
          "and the quotes must not stop an output root being recognised")
    check(qualify.is_source_sensitive("Assets\\Windows\\Path.cs"),
          "a backslash separator is the same path; both callers run on Windows as often as not")

    # ⚠️⚠️ THE TRACKED HALF IS A CONTENT QUESTION AND IS NOT CLASSIFIED AT ALL. `working_tree`
    # asks `git diff --name-only HEAD` rather than `git status --porcelain`, because porcelain
    # reported a file byte-identical to HEAD on this machine (`core.autocrlf` true, Unity YAML
    # stored as LF). `docs/TODO.md` § 149.11.
    source = (HERE / "qualify.py").read_text(encoding="utf-8", errors="replace")
    check("git\", \"diff\", \"--name-only\", \"HEAD\"" in source
          or '"diff", "--name-only", "HEAD"' in source,
          "working_tree must ask git a CONTENT question for the tracked half")
    check('"ls-files", "--others", "--exclude-standard"' in source,
          "and --exclude-standard is what makes .gitignore the first filter")


def audit_tree_rule_contract():
    """The C# and Python copies of the non-source roots have to be the same list.

    WARNING: `IntegrityRules.Digest` IS THE PRECEDENT AND IT IS THE REASON THIS EXISTS.
    A rule written in C# and again in another language, with nothing comparing the two, is a rule
    that silently disagrees with itself: `tools/check_digest_contract.js` was built because a
    disagreement there would dispute every match in the game and log nothing. A disagreement here
    is a build stamp saying `clean` and a qualification saying `dirty` about one tree, and each
    of the two looks authoritative on its own.
    """
    cs = ROOT / "Packages" / "com.tumbangpreso.core" / "Runtime" / "WorkingTreeRules.cs"
    if not cs.exists():
        FINDINGS.append(f"{cs} is missing: the C# half of the working-tree rule is gone")
        CHECKS[0] += 1
        return

    text = cs.read_text(encoding="utf-8", errors="replace")
    block = re.search(r"NonSourceUntrackedRoots\s*=\s*\{(?P<body>.*?)\};", text, re.S)
    if not block:
        FINDINGS.append("WorkingTreeRules.NonSourceUntrackedRoots could not be read out of the "
                        "C# source. If it was renamed, this audit is what has to move with it.")
        CHECKS[0] += 1
        return

    # ⚠️ COMMENT LINES ARE STRIPPED FIRST. Every root in that array carries a `//` note beside it
    # and one of those notes quotes a path, so a naive string sweep would read a comment as a row.
    body = re.sub(r"//[^\n]*", "", block.group("body"))
    csharp = re.findall(r'"([^"]+)"', body)

    check(sorted(csharp) == sorted(qualify.NON_SOURCE_UNTRACKED_ROOTS),
          "the C# and Python copies of the non-source roots have drifted:\n"
          f"      C#     {sorted(csharp)}\n"
          f"      Python {sorted(qualify.NON_SOURCE_UNTRACKED_ROOTS)}")


# ---------------------------------------------------------------------------
# 2. The net matrix verdicts
# ---------------------------------------------------------------------------

def peer(role="CLIENT", rounds=1, active="True", sampled=45.0, seats=None,
         defender="0", protocol="24"):
    return {
        "role": role, "networked": "True", "slot": "1", "protocol": protocol,
        "sampled": str(sampled), "round": str(rounds), "active": active,
        "defender": defender, "mode": "Classic", "map": "Eskinita",
        "structural": "AAAA1111", "hash": "BBBB2222",
        "seats": seats if seats is not None else [
            {"seat": i, "char": 0, "bot": True, "taya": i == 0, "score": 100,
             "travelled": 12.0} for i in range(4)
        ],
    }


def row(name, host, client):
    scenario = next(s for _, s in
                    [("wifi", x) for x in net_matrix.WIFI] +
                    [("disc", x) for x in net_matrix.DISCONNECT]
                    if s.name == name)
    return {"scenario": scenario, "host": host, "client": client, "work": "", "killed_at": None}


def audit_net_matrix_verdicts():
    # A clean row where both peers agree.
    ok, faults = net_matrix.evaluate(row("clean, direct", peer(role="HOST"), peer()))
    check(ok, f"a clean agreeing row must pass, got {faults}")

    # Hard divergence: the two peers disagree about who is taya.
    diverged = peer()
    diverged["seats"] = [dict(s) for s in diverged["seats"]]
    diverged["seats"][1]["taya"] = True
    ok, faults = net_matrix.evaluate(row("clean, direct", peer(role="HOST"), diverged))
    check(not ok, "two peers naming different tayas must FAIL, and this row exited 0 for its "
                  "whole life")
    check(any("diverged" in f for f in faults), f"the fault has to say so, got {faults}")

    # A missing report on a row where nobody was killed.
    ok, _ = net_matrix.evaluate(row("300 ms round trip", peer(role="HOST"), None))
    check(not ok, "a client that wrote no report on a row it was supposed to survive is a hang")

    ok, _ = net_matrix.evaluate(row("300 ms round trip", None, peer()))
    check(not ok, "and a host that wrote none is worse")

    # The client falling back to hosting where it should have stayed joined.
    ok, faults = net_matrix.evaluate(
        row("600 ms round trip", peer(role="HOST"), peer(role="HOST")))
    check(not ok, "a client reporting role HOST on a latency row lost its connection. It is the "
                  "most misleading file this harness can produce and it used to print as prose.")

    # The deliberate client kill: the host has to have kept refereeing PAST the kill.
    ok, faults = net_matrix.evaluate(
        row("client quits mid-match", peer(role="HOST", sampled=58.0), None))
    check(ok, f"a killed client with a host that carried on is the PASS for that row, got {faults}")

    ok, _ = net_matrix.evaluate(
        row("client quits mid-match", peer(role="HOST", sampled=58.0, active="False"), None))
    check(not ok, "a host that stopped refereeing when its peer left is exactly what that row "
                  "exists to catch")

    ok, _ = net_matrix.evaluate(
        row("client quits mid-match", peer(role="HOST", sampled=18.0), None))
    check(not ok, "a host that sampled BEFORE the kill says nothing about what happened after "
                  "it, and 'still alive' is not the claim")

    ok, _ = net_matrix.evaluate(
        row("client quits mid-match", peer(role="HOST", sampled=58.0), peer()))
    check(not ok, "a client that wrote a report on the row that kills it means the kill did not "
                  "take, so the row measured nothing")

    # The deliberate host kill: the client must reach the defined terminal behaviour.
    ok, faults = net_matrix.evaluate(row("host quits mid-match", None, peer(role="HOST")))
    check(ok, f"falling back to its own lobby is the defined correct end, got {faults}")

    ok, _ = net_matrix.evaluate(row("host quits mid-match", None, None))
    check(not ok, "a client that wrote nothing after losing its host hung or died with it")

    ok, faults = net_matrix.evaluate(
        row("host quits mid-match", None, peer(role="CLIENT", active="True")))
    check(not ok, "a client still playing a live round against a host that is gone is the worst "
                  "result that row can produce, and it is the one that looks fine on screen")

    # The permanent outage: the host stays, the client goes terminal.
    ok, faults = net_matrix.evaluate(
        row("link dies permanently", peer(role="HOST", sampled=58.0), peer(role="HOST")))
    check(ok, f"both processes alive, neither reachable, is the row's own prediction: {faults}")

    ok, _ = net_matrix.evaluate(
        row("link dies permanently", None, peer(role="HOST")))
    check(not ok, "the host is not killed on that row, so a missing host report is a crash")

    # Every scenario has to carry a machine-checkable expectation.
    for s in net_matrix.WIFI + net_matrix.DISCONNECT:
        check(s.host in (net_matrix.HOST_REFEREES, net_matrix.HOST_GONE),
              f"{s.name} does not say what it expects of the host")
        check(s.client in (net_matrix.CLIENT_JOINED, net_matrix.CLIENT_GONE,
                           net_matrix.CLIENT_TERMINAL),
              f"{s.name} does not say what it expects of the client")
        check(bool((s.expect or "").strip()),
              f"{s.name} has no written expectation. A row with no prediction beside it is a "
              f"number nobody can be wrong about.")


# ---------------------------------------------------------------------------
# 3. The cold start's destructive paths, on real temporary directories
# ---------------------------------------------------------------------------

def seed(path, marker):
    path.mkdir(parents=True, exist_ok=True)
    (path / "settings.json").write_text(marker, encoding="utf-8")


def marker_of(path):
    f = path / "settings.json"
    return f.read_text(encoding="utf-8") if f.exists() else None


def audit_profile_vault():
    with tempfile.TemporaryDirectory() as tmp:
        home = pathlib.Path(tmp)

        # --- no profile: nothing is created and nothing is deleted -----------
        vault = cold_start.ProfileVault(home / "absent" / "Tumbang Preso")
        check(vault.set_aside() is None, "a machine with no profile has nothing to set aside")
        ok, _ = vault.restore()
        check(ok, "and restoring it is a no-op rather than an error")
        check(not (home / "absent" / "Tumbang Preso").exists(),
              "restoring nothing must not CREATE a profile directory")

        # --- the ordinary round trip ----------------------------------------
        profile = home / "round-trip" / "Tumbang Preso"
        seed(profile, "REAL")
        vault = cold_start.ProfileVault(profile)
        backup = vault.set_aside()
        check(backup is not None and backup.exists(), "the profile has to be moved aside")
        check(not profile.exists(), "and the live path emptied, or the run is not cold")
        check((backup / cold_start.RESTORE_NOTE).exists(),
              "a backup directory with a GUID in its name says nothing to whoever finds it in "
              "six weeks. The restore note is what makes it recoverable by hand.")

        seed(profile, "RUN")           # the run makes its own throwaway profile
        ok, message = vault.restore()
        check(ok, f"the restore failed: {message}")
        check(marker_of(profile) == "REAL",
              f"the real profile was not put back, {marker_of(profile)!r} is on disk")
        check(not (profile / cold_start.RESTORE_NOTE).exists(),
              "the restore note is the backup's, not the profile's")

        # --- A PRE-EXISTING BACKUP IS THE ONLY COPY SOMEBODY HAS ------------
        #
        # WARNING: THIS IS THE ONE THE FIXED PATH COULD NOT SURVIVE. `<profile>.coldstart-backup`
        # was a constant and the old code ran `rmtree` on it before moving the live profile into
        # its place, so a backup left behind by a previous crash was destroyed and replaced by
        # the profile the crashed run had already half-written.
        profile = home / "pre-existing" / "Tumbang Preso"
        seed(profile, "CURRENT")
        stale = profile.with_name(profile.name + cold_start.BACKUP_PREFIX + "20260101-000000-dead")
        seed(stale, "THE ONLY GOOD COPY")

        vault = cold_start.ProfileVault(profile)
        backup = vault.set_aside()
        check(backup != stale, "a run must never choose a backup path that already exists")
        check(marker_of(stale) == "THE ONLY GOOD COPY",
              "A PRE-EXISTING BACKUP WAS DESTROYED. After a crash it is the only copy of "
              "somebody's settings, rebinds and career that exists.")

        ok, message = vault.restore()
        check(ok, f"restore failed: {message}")
        check(marker_of(profile) == "CURRENT",
              "the current profile has to come back even when a stale backup is beside it")
        check(marker_of(stale) == "THE ONLY GOOD COPY",
              "and it is still there after the restore too")

        # --- an exception in the run body still restores ---------------------
        profile = home / "raises" / "Tumbang Preso"
        seed(profile, "REAL")
        vault = cold_start.ProfileVault(profile)
        try:
            vault.set_aside()
            raise RuntimeError("the player crashed half way through")
        except RuntimeError:
            pass
        finally:
            vault.restore()
        check(marker_of(profile) == "REAL",
              "a crash between the move and the restore must not leave somebody's profile "
              "parked under a name they have no reason to look for")

        # --- a malformed handle refuses to touch anything --------------------
        profile = home / "malformed" / "Tumbang Preso"
        seed(profile, "REAL")
        vault = cold_start.ProfileVault(profile)
        vault.set_aside()
        seed(profile, "RUN")

        real_backup = vault.backup
        for bad in ([{"seat": 0}], "a string", profile.with_name("somebody-elses-folder")):
            vault.backup = bad
            ok, message = vault.restore()
            check(not ok, f"a backup handle of {bad!r} must be refused outright")
            check("REFUSED" in message, f"and say so: {message}")
            check(marker_of(profile) == "RUN",
                  "⚠️ NOTHING IS DELETED ON A REFUSAL. The shadowed-variable fault ran its "
                  "destructive half FIRST and only then failed on the restore.")

        vault.backup = real_backup
        ok, _ = vault.restore()
        check(ok and marker_of(profile) == "REAL", "and the real handle still works afterwards")

        # --- a failed restore keeps both copies ------------------------------
        profile = home / "failing" / "Tumbang Preso"
        seed(profile, "REAL")
        vault = cold_start.ProfileVault(profile)
        backup = vault.set_aside()
        seed(profile, "RUN")

        moved = []
        original_move = shutil.move

        def one_bad_move(src, dst):
            # The first move (the run's profile out of the way) is allowed; the second, which is
            # the backup coming home, fails. That is the window where the old shape had already
            # deleted the live path and had not yet restored the good one.
            moved.append(src)
            if len(moved) > 1:
                raise OSError("the volume went away")
            return original_move(src, dst)

        shutil.move = one_bad_move
        try:
            ok, message = vault.restore()
        finally:
            shutil.move = original_move

        check(not ok, "a failed restore has to report failure")
        check(backup.exists() and marker_of(backup) == "REAL",
              "AND THE REAL PROFILE MUST STILL BE ON DISK. Both copies survive a failure, which "
              "is the entire reason the order is move, move, delete.")
        check(str(backup) in message and "BOTH" in message,
              f"the error has to name where both copies are, got: {message}")

        discarded = [d for d in profile.parent.glob("*" + cold_start.DISCARD_PREFIX + "*")]
        check(len(discarded) == 1 and marker_of(discarded[0]) == "RUN",
              "and the run's own throwaway is recoverable too rather than deleted")


def audit_cold_start_flags():
    """`--clean-profile` is opt in, and the tournament run asserts the preset."""
    source = (HERE / "cold_start.py").read_text(encoding="utf-8", errors="replace")

    check("vault.set_aside()" in source and "if args.clean_profile" in source,
          "the only call to set_aside must be behind --clean-profile, so a default run cannot "
          "reach the destructive path at all")

    # ⚠️ COMMENT LINES ARE EXCLUDED, because two of the ⚠️ notes in that file QUOTE the call that
    # destroyed a real profile, and a rule that counted those would push the next reader into
    # deleting the reasoning to pass a gate. `CLAUDE.md` § 3: the record of why is the comment.
    calls = [ln for ln in source.splitlines()
             if "shutil.rmtree" in ln and not ln.lstrip().startswith("#")]
    check(len(calls) == 1,
          f"there must be exactly one rmtree CALL in cold_start.py and it must be the guarded "
          f"one in ProfileVault.restore; found {len(calls)}: {calls}")
    check(calls and "discard" in calls[0],
          f"the one rmtree must target a discard directory this class created, never the live "
          f"profile and never a backup; it reads {calls[0].strip() if calls else '(none)'}")

    check(cold_start.TOURNAMENT_ALLOWED_MODIFIERS == frozenset(),
          "a tournament cold start that forgave its own modifier would be § 145.3's hole one "
          "level up")

    # The preset assertions themselves.
    check(cold_start.tournament_reasons(
        {"mode": "Classic", "ruleset": "OK", "modifiers": "none"}) == [],
        "a Classic run with the tournament rule set and no modifiers has to pass")

    reasons = cold_start.tournament_reasons(
        {"mode": "HeroStrike", "ruleset": "OK", "modifiers": "none"})
    check(any("Classic" in r for r in reasons),
          "⚠️ THE RECORDED GREEN COLD START AT 87346b8 IS HERO STRIKE. docs/VISION.md § 1.1 says "
          "Classic is the tournament ruleset, so booting the other one has to fail this.")

    reasons = cold_start.tournament_reasons({"mode": "Classic", "ruleset": "OK",
                                             "modifiers": "GameLaunch.AllBots"})
    check(reasons, "a practice or debug modifier left set has to fail a tournament run")

    reasons = cold_start.tournament_reasons({"mode": "Classic", "ruleset": "OK"})
    check(reasons, "a player too old to print the modifier line cannot be certified; absence is "
                   "not agreement")

    reasons = cold_start.tournament_reasons({"mode": "Classic", "modifiers": "none",
                                             "ruleset": "rounds is 8, tournament is 4"})
    check(reasons, "and a rule set that is not the tournament one has to fail with the sentence")


# ---------------------------------------------------------------------------
# 4. The candidate artifact rules
# ---------------------------------------------------------------------------

def audit_candidate_rules():
    good = {"sha": "a" * 40, "protocol": 24, "target": "StandaloneWindows64",
            "appVersion": "1.0.0", "ugsProject": "dcf0831e", "ugsEnvironment": "production",
            "builtAt": "2026-09-05 08:18:13", "treeState": "clean", "dirty": False}

    check(qualify.candidate_faults("Windows", good, "a" * 40, 24) == [],
          "a clean stamped artifact at the right SHA and protocol is a candidate")

    dirty = dict(good, treeState="dirty", dirty=True)
    check(qualify.candidate_faults("Windows", dirty, "a" * 40, 24),
          "⚠️⚠️ A DIRTY ARTIFACT AT THE RIGHT SHA IS NOT A CANDIDATE. Build with an edited .cs, "
          "revert the edit, and every SHA comparison passes over a player that contains code in "
          "no commit.")

    legacy = dict(good)
    legacy.pop("treeState")
    check(qualify.candidate_faults("Windows", legacy, "a" * 40, 24),
          "a stamp with no treeState predates the three-way answer and reads as unknown")

    unknown = dict(good, treeState="unknown")
    check(qualify.candidate_faults("Windows", unknown, "a" * 40, 24),
          "unknown is not clean and folding it either way is wrong")

    check(qualify.candidate_faults("Windows", good, "b" * 40, 24),
          "a different SHA is not this commit's candidate")
    check(qualify.candidate_faults("Windows", good, "a" * 40, 25),
          "a protocol the source has moved past refuses every peer by design")
    check(qualify.candidate_faults("Windows", None, "a" * 40, 24),
          "no stamp at all cannot be tied to a commit")

    for field in ("ugsProject", "ugsEnvironment", "appVersion", "target", "builtAt"):
        blank = dict(good)
        blank[field] = ""
        check(qualify.candidate_faults("Windows", blank, "a" * 40, 24),
              f"a blank {field} looks like agreement and is not one")

    lying = dict(good, dirty=True)
    check(qualify.candidate_faults("Windows", lying, "a" * 40, 24),
          "treeState clean and dirty true at once means something other than GameBuilder wrote "
          "the record")

    for field, _ in qualify.CROSSPLAY_FIELDS:
        check(field in good,
              f"{field} is compared between two artifacts and nothing stamps it")

    for field in ("ugsProject", "ugsEnvironment"):
        check(field in [f for f, _ in qualify.CROSSPLAY_FIELDS],
              f"⚠️⚠️ {field} MUST BE PART OF CROSSPLAY IDENTITY. Two players from one commit "
              f"resolving join codes in different UGS namespaces do not refuse each other; they "
              f"never find each other's rooms, and it reads as an EMPTY LOBBY.")


# ---------------------------------------------------------------------------

def main():
    audit_untracked_rule()
    audit_tree_rule_contract()
    audit_net_matrix_verdicts()
    audit_profile_vault()
    audit_cold_start_flags()
    audit_candidate_rules()

    for f in FINDINGS:
        print("  FINDING  " + f)

    print(f"audit_harness_contracts: {CHECKS[0]} checks, {len(FINDINGS)} finding(s).")
    return 1 if FINDINGS else 0


if __name__ == "__main__":
    sys.exit(main())
