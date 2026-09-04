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
VISION = ROOT / "docs" / "VISION.md"

MODIFIER = re.compile(r'new\s+Modifier\(\s*"([^"]+)"\s*,\s*("(?:[^"\\]|\\.)*"(?:\s*\+\s*"(?:[^"\\]|\\.)*")*)',
                      re.S)
CASE = re.compile(r'case\s+"([^"]+)"\s*:')


def main():
    findings = []

    preset = PRESET.read_text(encoding="utf-8", errors="replace")
    guard = GUARD.read_text(encoding="utf-8", errors="replace")

    named = {}
    for m in MODIFIER.finditer(preset):
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
    print(f"{len(named)} tournament modifiers named, {len(read_cases)} read cases, "
          f"{len(write_cases)} write cases, {len(findings)} finding(s).")

    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main())
