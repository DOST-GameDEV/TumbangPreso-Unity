#!/usr/bin/env python3
"""
The tournament modifier list and the code that reads it must not drift apart.

WHY THIS EXISTS
---------------
`TournamentPreset.Modifiers` (engine-free, in the core) names every developer or practice
switch a nationals match must not be carrying, and `TournamentGuard` (Unity side) reads
and clears the live values. The split is forced: `Packages/com.tumbangpreso.core/` may
never reference `UnityEngine` (`CLAUDE.md` § 4), so the list and the accessors cannot
live in the same file.

⚠️⚠️ A SPLIT LIKE THAT IS EXACTLY WHERE THINGS GO STALE, AND THIS REPOSITORY HAS THE
RECEIPTS. `CLAUDE.md` § 7 said "all FIVE editor checks" while § 7.1 listed seven, and
§ 7.1 said "three" audits while the folder held six. Both times the COUNT was the copy
that rotted, because the person adding the seventh thing edits the list and not the
sentence above it.

`TournamentGuardTests` asserts the same property from inside Unity, and this asserts it
from the source, in about forty milliseconds, without an editor launch. That matters
because the EditMode suite is a several-minute launch and this is what actually stops a
bad commit (`InputContractTests`' own argument: "a bound only a twelve-minute PlayMode
run can enforce is a bound somebody edits a string past on a Friday").

WHAT IT CHECKS
--------------
  1. Every name in `TournamentPreset.Modifiers` has a `case` in TournamentGuard.Read
     AND in TournamentGuard.Write. A name in neither reads as "off" and is silently
     never checked, which is the failure this whole pair of files exists to prevent.
  2. Every `case` in the guard is a name the core actually lists, so a switch that was
     removed from the roster does not leave a dead accessor behind.
  3. Every modifier carries a reason. A list of bare field names is a list somebody
     deletes a row from during a tidy-up.
  4. `TournamentPreset.Mode` is Classic, which is `docs/VISION.md` § 1.1's ruling.
  5. ⚠️⚠️ **COMPLETENESS.** Every SETTABLE static gameplay switch in the runtime, and
     every `-tp-` launch switch, is on one of the two lists.

⚠️⚠️ CHECK 5 IS THE ONE THAT WAS MISSING AND IT IS THE ONLY ONE THAT CAN SEE SWITCH
NUMBER NINE. Checks 1 to 4 prove two lists agree with each other, in both directions,
and a static added tomorrow to NEITHER file is invisible to all four of them: it is not
on the roster, so no case is missing, and it has no accessor, so no accessor is dead.
**The whole failure mode that roster exists for lives exactly in that blind spot**, and
`docs/TODO.md` § 145.3 is the entry. `TournamentPreset.NotModifiers` is where a switch is
dismissed, with the reason it cannot change a match.

⚠️⚠️ **SETTABLE** IS THE FILTER AND IT IS WHAT KEEPS THIS FROM BEING THE NOISY GATE
DEVELOPERS LEARN TO IGNORE. There are forty-one static bools in the runtime and most are
derived properties, `NetAuthority.IsHost`, `Panel.AnyOpen`, `PracticeSandbox.Active` 
which nothing outside can write, so nothing can LEAVE one set, which is the entire hazard
`TournamentGuard` exists for. Thirteen are settable and eight of those are already on the
roster.

⚠️ AND IT DOES NOT SWEEP THE EDITOR OR THE TESTS. `Assets/TumbangPreso/Editor` does not
ship and `Assets/TumbangPreso/Tests` is `UNITY_INCLUDE_TESTS`-gated, so neither can leave
a flag set in a player. Widening the sweep to them would add rows nobody can act on, which
is how a gate stops being read.

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
PRESET = ROOT / "Packages" / "com.tumbangpreso.core" / "Runtime" / "TournamentPreset.cs"
GUARD = ROOT / "Assets" / "TumbangPreso" / "Runtime" / "TournamentGuard.cs"
RUNTIME = ROOT / "Assets" / "TumbangPreso" / "Runtime"
VISION = ROOT / "docs" / "VISION.md"

MODIFIER = re.compile(r'new\s+Modifier\(\s*"([^"]+)"\s*,\s*("(?:[^"\\]|\\.)*"(?:\s*\+\s*"(?:[^"\\]|\\.)*")*)',
                      re.S)
CASE = re.compile(r'case\s+"([^"]+)"\s*:')

# ⚠️⚠️ A FIELD OR AN AUTO-PROPERTY WITH A PUBLIC SETTER, AND NOTHING ELSE. The three shapes are
# `public static bool X;`, `public static bool X = true;` and `public static bool X { get; set; }`.
# The `[^>]` after the `=` is what excludes an expression-bodied property (`=> ...`), which is
# derived state nothing outside can write.
SETTABLE = re.compile(
    r'^\s*(?:public|internal)\s+static\s+bool\s+([A-Z][A-Za-z0-9_]*)\s*(?:;|=[^>]|\{\s*get;\s*set;)',
    re.M)

# Every `-tp-` switch the runtime reads, wherever it reads it.
LAUNCH_SWITCH = re.compile(r'"(-tp-[a-z0-9-]+)"')

# The map from a launch switch to the static it leaves behind, out of the core's own table.
LAUNCH_MAP = re.compile(r'case\s+"(-tp-[a-z0-9-]+)"\s*:\s*return\s+"([^"]*)"\s*;')


def main():
    findings = []

    preset = PRESET.read_text(encoding="utf-8", errors="replace")
    guard = GUARD.read_text(encoding="utf-8", errors="replace")

    # ⚠️⚠️ THE FILE HOLDS TWO `Modifier[]` ARRAYS NOW AND THEY MEAN OPPOSITE THINGS. Reading the
    # whole file for `new Modifier(` would put every EXEMPTION on the roster, which would make
    # check 1 demand a `TournamentGuard` case for `-tp-profile` and check 5 pass by accident. The
    # split is on the declaration rather than on the word, because the word appears in the section
    # comment above it first.
    marker = "public static readonly Modifier[] NotModifiers"
    if marker not in preset:
        print("TournamentPreset.NotModifiers is gone. The completeness check has nowhere to "
              "dismiss a switch, so every settable static in the game would be a finding.")
        return 1

    roster_text, exempt_text = preset.split(marker, 1)

    named = {}
    for m in MODIFIER.finditer(roster_text):
        why = re.sub(r'"\s*\+\s*"', "", m.group(2)).strip('"')
        named[m.group(1)] = why

    if not named:
        print("could not read any modifier out of TournamentPreset.cs. The shape of the "
              "list changed and this audit is now blind, which is worse than a finding.")
        return 1

    # Split the guard into its two switches so a name present in one and not the other is caught.
    read_body = guard.split("private static bool Read(", 1)[-1].split("private static void Write(", 1)[0]
    write_body = guard.split("private static void Write(", 1)[-1].split("public static List<Reading>", 1)[0]

    read_cases = set(CASE.findall(read_body))
    write_cases = set(CASE.findall(write_body))

    for name, why in sorted(named.items()):
        if name not in read_cases:
            findings.append(f"{name} is named in TournamentPreset.Modifiers and has no case in "
                            f"TournamentGuard.Read, so it reads as UNREADABLE and is never checked")
        if name not in write_cases:
            findings.append(f"{name} is named in TournamentPreset.Modifiers and has no case in "
                            f"TournamentGuard.Write, so Apply cannot clear it")
        if len(why) < 40:
            findings.append(f"{name} is on the list with no reason written down. The name is the "
                            f"part that gets forgotten; a bare list gets a row deleted in a tidy-up")

    for case in sorted(read_cases | write_cases):
        if case not in named:
            findings.append(f"TournamentGuard handles \"{case}\" and the core does not list it. "
                            f"Either add it to TournamentPreset.Modifiers with its reason, or "
                            f"delete the dead accessor")

    # ---- 5. completeness -------------------------------------------------
    #
    # ⚠️⚠️ THIS IS THE CHECK THE OTHER FOUR CANNOT MAKE. See the header: a switch on NEITHER list
    # satisfies every list-agreement test there is, because agreement is a property of the two
    # lists rather than of the game.
    exempt = {}
    for m in MODIFIER.finditer(exempt_text):
        why = re.sub(r'"\s*\+\s*"', "", m.group(2)).strip('"')
        exempt[m.group(1)] = why

    if not exempt:
        findings.append("TournamentPreset.NotModifiers is empty or unreadable. Without it the "
                        "completeness check below can only report every settable static in the "
                        "game as unaccounted for, which is a gate nobody can pass.")

    for name, why in sorted(exempt.items()):
        if len(why) < 40:
            findings.append(f"{name} is exempted from the tournament roster with no reason "
                            f"written down. 'It is fine' is how a row gets deleted in a tidy-up "
                            f"by somebody who cannot tell whether it was ever thought about")
        if name in named:
            findings.append(f"{name} is on Modifiers AND on NotModifiers. One of the two is "
                            f"wrong: the guard clears it and the roster says it is none of its "
                            f"business")

    accounted = set(named) | set(exempt)

    for path in sorted(RUNTIME.rglob("*.cs")):
        text = path.read_text(encoding="utf-8", errors="replace")
        owner = path.stem

        for m in SETTABLE.finditer(text):
            full = f"{owner}.{m.group(1)}"
            if full in accounted:
                continue

            findings.append(
                f"{full} ({path.relative_to(ROOT)}) is a settable static gameplay switch on "
                f"NEITHER TournamentPreset.Modifiers nor NotModifiers. Every switch this game "
                f"has for testing survives a scene change by definition, and an operator "
                f"starting the next bracket match inherits whatever the last one left behind. "
                f"Add it to the roster with its reason, or to the exemption list with the "
                f"reason it cannot change a match")

    # ---- 5b. the launch switches ----------------------------------------
    #
    # ⚠️ A `-tp-` SWITCH IS ACCOUNTED FOR IN ONE OF TWO WAYS: it is on `NotModifiers` by its own
    # name, or `TournamentPreset.LaunchSwitchModifier` maps it to a static that IS on the roster.
    # Listing a switch AND the static it writes would be two rows for one hazard, which is how a
    # roster starts disagreeing with itself.
    mapped = dict(LAUNCH_MAP.findall(preset))

    switches = set()
    for path in sorted(RUNTIME.rglob("*.cs")):
        text = path.read_text(encoding="utf-8", errors="replace")
        switches.update(LAUNCH_SWITCH.findall(text))

    for switch in sorted(switches):
        if switch in accounted:
            continue

        if switch in mapped:
            target = mapped[switch]
            if target == "" or target in named:
                continue

            findings.append(f"{switch} maps to \"{target}\", which is not on "
                            f"TournamentPreset.Modifiers. A launch switch that sets an "
                            f"unlisted static is a modifier with no tournament-safe value")
            continue

        findings.append(
            f"{switch} is a launch switch on NEITHER TournamentPreset.NotModifiers nor "
            f"LaunchSwitchModifier. Say which static it leaves set, or say why it leaves none")

    mode = re.search(r"public\s+const\s+GameMode\s+Mode\s*=\s*GameMode\.(\w+)\s*;", preset)
    if not mode:
        findings.append("TournamentPreset.Mode could not be read")
    elif mode.group(1) != "Classic":
        findings.append(f"TournamentPreset.Mode is {mode.group(1)}. docs/VISION.md § 1.1 says "
                        f"CLASSIC IS THE TOURNAMENT RULESET UNTIL SOMEONE SAYS OTHERWISE, so "
                        f"changing this is a tournament ruling and the document has to move first")

    # ⚠️ AND THE DOCUMENT IS ASSERTED FROM THIS SIDE TOO, so the constant and the sentence
    # cannot be changed independently. This is § 5's drift rule pointed at a ruling rather
    # than at a balance number.
    try:
        vision = VISION.read_text(encoding="utf-8", errors="replace")
        if "CLASSIC IS THE TOURNAMENT RULESET" not in vision.upper():
            findings.append("docs/VISION.md no longer states which mode is the tournament "
                            "ruleset, and TournamentPreset.Mode is pinned to it")
    except OSError as e:
        findings.append(f"could not read docs/VISION.md: {e}")

    for f in findings:
        print(f)

    print()
    print(f"{len(named)} tournament modifiers named, {len(exempt)} exempted with a reason, "
          f"{len(read_cases)} read cases, {len(write_cases)} write cases, "
          f"{len(switches)} launch switches discovered, {len(findings)} finding(s).")

    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main())
