#!/usr/bin/env python3
"""
Gameplay timing must not read the wall clock, and persistent timestamps must.

WHY THIS EXISTS
---------------
Two different clocks, two different jobs, and using the wrong one fails in a way that
never shows up in a test run:

  * WALL CLOCK (`DateTime.Now`, `DateTime.UtcNow`) is what a human's calendar says. It
    jumps when the system clock is corrected, when a timezone changes, and across a
    daylight saving boundary. It is the RIGHT answer for anything that has to mean the
    same thing after the process restarts: when a match was played, when an account
    proof expires, which mirror this week is.

  * GAME TIME (`Time.deltaTime`, and the accumulators built on it) is what the match
    says. It respects pause, it respects `Time.timeScale`, and it cannot jump. It is
    the ONLY right answer for a cooldown, a round clock, a stun, a stamina drain or a
    reconnect window.

⚠️⚠️ A COOLDOWN ON THE WALL CLOCK IS A COOLDOWN A PLAYER CAN SKIP BY CHANGING THE
SYSTEM TIME, and a round clock on it is a round that ends early when a machine
resynchronises with NTP mid-match. Neither reproduces in a test run and both are
catastrophic in a bracket.

THE MEASUREMENT ON e85b0fc
--------------------------
This audit found NO defect. Every one of the 14 `DateTime` reads in `Runtime/` is a
persistent timestamp, a calendar fact or a network deadline, and every gameplay timer
already runs on game time. **That is the result worth gating**: the property is true
today and nothing was stopping the next session from breaking it.

⚠️ AND `Time.realtimeSinceStartup` IS THE THIRD CLOCK AND THE SUBTLE ONE. It is
monotonic, so it cannot jump, but it is UNSCALED: it keeps running while the game is
paused and it ignores `Time.timeScale`. That makes it correct for a UI animation and for
a probe measuring wall time, and wrong for anything a paused game should not be losing.
`docs/VISION.md` § 4: "the anti-camp and anti-stall clocks HOLD rather than run while a
unit cannot act."

Exits non-zero on any finding. `tools/qualify.py --stage audits` runs it.
"""

import pathlib
import re
import sys

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

ROOT = pathlib.Path(__file__).resolve().parent.parent
RUNTIME = ROOT / "Assets" / "TumbangPreso" / "Runtime"
CORE = ROOT / "Packages" / "com.tumbangpreso.core" / "Runtime"

WALL = re.compile(r"\bDateTime\s*\.\s*(Now|UtcNow)\b")
UNSCALED = re.compile(r"\bTime\s*\.\s*(realtimeSinceStartup|unscaledTime|unscaledDeltaTime)\b")

# ⚠️⚠️ THE FILES WHERE A WALL-CLOCK READ IS A DEFECT BY DEFINITION. These decide match
# outcomes: the round clock, the role schedule, every cooldown, every stun, the stamina
# model, throw legality and the anti-stall clocks. A timestamp has no business in any of
# them, so this list is a rule rather than a heuristic.
GAMEPLAY_CRITICAL = {
    "RoundDirector.cs", "MatchDirector.cs", "SliceRunner.cs", "MatchHost.cs",
    "CharacterMotor.cs", "CombatVerbs.cs", "Carrier.cs", "Slipper.cs", "Lata.cs",
    "StatusStack.cs", "StunElement.cs", "HazardZone.cs", "Hitstop.cs",
    "AIController.cs", "AiPlan.cs", "ReadyGate.cs", "BufferSkipVote.cs",
    "LastTsinelasDirector.cs", "KillPlane.cs", "TrajectoryPreview.cs",
    "HeroAbility.cs", "HeroAbilitySystem.cs", "HeroKit.cs", "HeroHazards.cs",
    "AbilityContext.cs", "HazardVolume.cs", "HazardMap.cs",
    "CheskaHeroKit.cs", "DanteHeroKit.cs", "NemuHeroKit.cs",
    "PhaisterHeroKit.cs", "SeanHeroKit.cs", "ZackHeroKit.cs",
}

# Every wall-clock read that is CORRECT, with the reason. A row that stops matching is an
# error, for `audit_cue_relay.py`'s reason: an allowlist that covers nothing today will
# cover something new by accident tomorrow.
PERSISTENT_OK = [
    ("BuildIdentity.cs", "when this build was made. It has to survive the process."),
    ("MatchInstaller.cs", "CustomGameRules.MirrorIndex takes the DATE: which mirror is "
                          "live this week is a calendar fact, not a match timer."),
    ("MatchStatsCollector.cs", "PlayedUtc on a match record. A career page shows when a "
                               "match was played, which game time cannot answer."),
    ("CareerStore.cs", "InMatchSinceUtc, persisted across a crash so an abandoned match "
                       "can be detected on the next launch."),
    ("GoogleSignIn.cs", "a consent dialog deadline. It is a network wait outside the "
                        "match and nothing about it decides a round."),
    ("Matchmaker.cs", "a queue deadline that must survive the app being restarted."),
    ("NetIdentity.cs", "the sign-in retry cooldown, which is about a remote service "
                       "rather than about play."),
    ("PlayerAccount.cs", "an account proof expiry, which is agreed with a server and has "
                         "to be the same clock the server used."),
    ("PlayerHub.cs", "displaying a date to a person."),
    ('FailureBundle.cs', 'when the bundle was written, and the timestamp in its filename. A '
                         'crash report whose time is game time is a crash report nobody can '
                         'line up against a log or against what somebody remembers happening.'),
]


# ⚠️ THE UNSCALED CLOCK IS CORRECT IN EXACTLY TWO GAMEPLAY FILES, AND BOTH SAY WHY IN
# THEIR OWN HEADERS. A row that stops matching is an error for the same reason as above.
UNSCALED_OK = [
    ("Hitstop.cs", "Hitstop is the thing that SETS Time.timeScale to near zero for impact "
                   "feel, so a freeze measured in scaled time would never end. Using the "
                   "unscaled clock is what makes it releasable, not an oversight."),
    ("Lata.cs", "DriveDownPulse is a visual sine driving the down-beacon's collar and rim. "
                "A beacon that stops breathing while the game is paused reads as broken, "
                "and nothing about it decides a round."),
]


def strip_comments(text):
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


def main():
    findings = []
    wall_sites = 0
    hits = {f: 0 for f, _why in PERSISTENT_OK}

    for path in sorted(list(RUNTIME.rglob("*.cs")) + list(CORE.rglob("*.cs"))):
        code = strip_comments(path.read_text(encoding="utf-8", errors="replace"))

        for m in WALL.finditer(code):
            wall_sites += 1
            line = code[:m.start()].count("\n") + 1

            if path.name in GAMEPLAY_CRITICAL and path.name not in hits:
                findings.append(
                    f"{path.name}:{line}  reads DateTime.{m.group(1)} in a file that decides "
                    f"match outcomes. A gameplay timer on the wall clock jumps when the system "
                    f"clock is corrected. Use game time.")
                continue

            if path.name in hits:
                hits[path.name] += 1
                continue

            findings.append(
                f"{path.name}:{line}  reads DateTime.{m.group(1)} and is not on the list of "
                f"sites where a wall clock is correct. Either it is a persistent timestamp, in "
                f"which case add it to PERSISTENT_OK with the reason, or it is a gameplay timer, "
                f"in which case it is a bug.")

    # ⚠️ THE UNSCALED CLOCK INSIDE A GAMEPLAY FILE IS A SEPARATE AND QUIETER FAULT. It cannot
    # jump, so it is not the same class, but it keeps counting while the game is paused, and
    # `docs/VISION.md` § 4 requires the anti-camp and anti-stall clocks to HOLD.
    unscaled_hits = {f: 0 for f, _w in UNSCALED_OK}

    for path in sorted(RUNTIME.rglob("*.cs")):
        if path.name not in GAMEPLAY_CRITICAL:
            continue
        code = strip_comments(path.read_text(encoding="utf-8", errors="replace"))
        for m in UNSCALED.finditer(code):
            line = code[:m.start()].count("\n") + 1

            if path.name in unscaled_hits:
                unscaled_hits[path.name] += 1
                continue

            findings.append(
                f"{path.name}:{line}  uses Time.{m.group(1)} in a gameplay-critical file. It is "
                f"monotonic but UNSCALED, so it keeps running while the game is paused. VISION "
                f"§ 4 requires the anti-stall clocks to hold rather than run.")

    stale = [f for f, n in hits.items() if n == 0]
    for f in stale:
        findings.append(f"ALLOWLIST STALE: {f} is listed as a correct wall-clock site and no "
                        f"longer reads one. Remove the row.")

    for f, n in unscaled_hits.items():
        if n == 0:
            findings.append(f"ALLOWLIST STALE: {f} is listed as a correct unscaled-clock site "
                            f"and no longer reads one. Remove the row.")

    for f in findings:
        print(f)

    print()
    print(f"{wall_sites} wall-clock reads across Runtime/ and the core, "
          f"{len(GAMEPLAY_CRITICAL)} gameplay-critical files checked, {len(findings)} finding(s).")

    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main())
