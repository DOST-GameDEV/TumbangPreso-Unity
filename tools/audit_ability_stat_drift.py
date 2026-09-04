#!/usr/bin/env python3
"""
Ability cooldowns asserted in comments that the constructors disagree with.

WHY THIS EXISTS
---------------
`docs/Design.md` opens with the rule this enforces one level down: "a number in the
code must match a number here, or one of the two is a bug." The ability kits are where
that discipline is densest, and therefore where it rots first: the comments are long,
authoritative, and they QUOTE EACH OTHER.

Five confirmed cases on e85b0fc, and the cross-references are why an audit beats a
one-time correction:

    ZackHeroKit.cs:86      "30 s, UP FROM 6.0"                     built with 46.0f
    SeanHeroKit.cs:58      "34 s ... Longer than Zack's 30"        built with 50.0f
    DanteHeroKit.cs:159    "45 s, UP FROM 9.0 ... LONGEST"         built with 62.0f
    NemuHeroKit.cs:41      "36 s ... between Sean's 34 and
                            Dante's 45"                            built with 52.0f
    PhaisterHeroKit.cs:128 "SHADOW BLINK (36.0 s cooldown)"        built with 52.0f

One stale 30 had already propagated into two other files as a comparison, and Nemu's
note repeats two more. A comment that is wrong is worse than a missing one: it reads
as measured, it gets quoted, and the next person reasons from it.

WHAT IT LOOKS FOR, AND WHY IT IS THREE NARROW PATTERNS RATHER THAN ONE WIDE ONE
------------------------------------------------------------------------------
⚠️⚠️ THE FIRST VERSION OF THIS AUDIT USED THE WIDE RULE: "a duration in a comment must
exist as a literal in the same file's code." It found all five real cases and three
false ones, and the false ones are instructive, because every one of them is a comment
doing its job:

  - `HeroAbility.cs:115` quotes the team asking for *"like 30 seconds to 45 seconds"*.
    That is the REQUEST, and it is supposed to survive the retune it caused.
  - `DanteHeroKit.cs:163` says *"At 9 s it was up for four seconds out of every nine"*.
    That is HISTORY, and `CLAUDE.md` § 3 asks for exactly that: record the reasoning,
    not just the change.
  - `CheskaHeroKit.cs:182` argues *"3.2 s is barely long enough to cross the box"*
    against the 6.0 it chose. That is an ARGUMENT about a rejected value.

**A rule that cannot tell a stale fact from a recorded reason will be switched off**, so
this only fires on the three shapes this codebase uses to ASSERT a current cooldown:

    1.  "46 s, UP FROM 6.0"          the retune sentence
    2.  "(52.0 s cooldown)"          the summary line
    3.  "Longer than Zack's 30"      a cross-reference to another hero's number

⚠️ FALSE NEGATIVES ARE ACCEPTED AND FALSE POSITIVES ARE NOT. A stale number written in
some fourth shape gets through. That is the correct trade for a check that gates a
release: an audit that cries wolf is an audit somebody adds to an ignore list.

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
ABILITIES = ROOT / "Assets" / "TumbangPreso" / "Runtime" / "Abilities"

HEROES = ("Zack", "Sean", "Dante", "Nemu", "Cheska", "Phaister")

# 1. The retune sentence: "46 s, UP FROM 6.0" / "34 s, up from 6.5"
RETUNE = re.compile(r"(?<![\d.])(\d+(?:\.\d+)?)\s*s\b[^.]{0,40}?\bUP FROM\b", re.I)

# 2. The summary line: "(52.0 s cooldown)" / "52 s cooldown"
SUMMARY = re.compile(r"(?<![\d.])(\d+(?:\.\d+)?)\s*s\s+cooldown\b", re.I)

# 3. A cross-reference: "Zack's 30", "between Sean's 34 and Dante's 45"
CROSSREF = re.compile(r"\b(" + "|".join(HEROES) + r")'s\s+(\d+(?:\.\d+)?)\b")

LITERAL_ARG = re.compile(r"[A-Za-z0-9_.]+")

CONSTRUCTOR = re.compile(
    r':\s*base\(\s*"([a-z_0-9]+)"\s*,\s*"([^"]+)"\s*,\s*'
    r'(?:"(?:[^"\\]|\\.)*"\s*,\s*)?'
    r'([A-Za-z0-9_.]+)\s*,\s*([A-Za-z0-9_.]+)\s*,',
    re.S)


def strip_comments(text):
    """Code only, comments blanked in place so line numbers survive."""
    out, i, n = [], 0, len(text)
    while i < n:
        c = text[i]
        if c == '"':
            out.append(c)
            i += 1
            while i < n:
                if text[i] == "\\":
                    out.append("  ")
                    i += 2
                    continue
                out.append(text[i])
                if text[i] == '"':
                    i += 1
                    break
                i += 1
            continue
        if text.startswith("//", i):
            j = text.find("\n", i)
            j = n if j < 0 else j
            out.append(" " * (j - i))
            i = j
            continue
        if text.startswith("/*", i):
            j = text.find("*/", i)
            j = n if j < 0 else j + 2
            out.append(" " * (j - i))
            i = j
            continue
        out.append(c)
        i += 1
    return "".join(out)


def numeric(v):
    try:
        return float(v)
    except (TypeError, ValueError):
        return None


def file_constants(code):
    out = {}
    for m in re.finditer(r"const\s+\w+\s+(\w+)\s*=\s*([\d.]+)[fF]?\s*;", code):
        v = numeric(m.group(2))
        if v is not None:
            out[m.group(1)] = v
    return out


def resolve(token, constants):
    v = numeric(token.rstrip("fF"))
    if v is not None:
        return v
    return constants.get(token.split(".")[-1])


def abilities_in(path):
    """(ability_id, display name, cooldown, start_line) for each constructor in a file."""
    text = path.read_text(encoding="utf-8", errors="replace")
    code = strip_comments(text)
    constants = file_constants(code)

    rows = []
    for m in CONSTRUCTOR.finditer(code):
        ability_id, name, cooldown_token, _duration = m.groups()
        cooldown = resolve(cooldown_token, constants)
        rows.append((ability_id, name, cooldown, code[:m.start()].count("\n") + 1))
    return text, rows


def hero_cooldowns():
    """Every cooldown each hero actually ships, for the cross-reference check."""
    out = {h: set() for h in HEROES}
    for path in sorted(ABILITIES.glob("*HeroKit.cs")):
        hero = path.name.replace("HeroKit.cs", "")
        if hero not in out:
            continue
        _text, rows = abilities_in(path)
        for _id, _name, cooldown, _line in rows:
            if cooldown:
                out[hero].add(round(cooldown, 4))
    return out


def attached_comment(text, constructor_line):
    """The comment block immediately above a constructor, as (line, text) pairs."""
    lines = text.splitlines()
    block = []
    n = constructor_line - 1
    while n >= 1:
        raw = lines[n - 1].strip()
        if raw.startswith("//") or raw.startswith("///") or raw.startswith("*") \
           or raw.startswith("/*") or raw.startswith("public ") or raw.endswith("*/"):
            if raw.startswith("//") or raw.startswith("*") or raw.startswith("/*") \
               or raw.endswith("*/"):
                block.append((n, raw))
            n -= 1
            continue
        break
    return list(reversed(block))


def main():
    findings = []
    constructors = 0
    per_hero = hero_cooldowns()

    for path in sorted(ABILITIES.glob("*.cs")):
        text, rows = abilities_in(path)
        constructors += len(rows)
        lines = text.splitlines()

        # ---- 1 and 2: a cooldown asserted about THIS ability -------------
        for ability_id, name, cooldown, line_no in rows:
            if not cooldown:
                # ⚠️ A ZERO COOLDOWN IS AN ULTIMATE OR A CHARGE ABILITY, NOT A ZERO-SECOND
                # COOLDOWN. Those are paid out of the ultimate economy or a charge count,
                # so prose near them is describing something this argument does not hold.
                continue

            claims = list(attached_comment(text, line_no))

            # The <summary> line naming the cooldown may sit further up, above the class,
            # so the whole file is searched for a summary claim that names this ability.
            for n, raw in enumerate(lines, 1):
                if name.upper() in raw.upper() and SUMMARY.search(raw):
                    claims.append((n, raw.strip()))

            for n, raw in claims:
                for pattern in (RETUNE, SUMMARY):
                    for m in pattern.finditer(raw):
                        value = numeric(m.group(1))
                        if value is None or abs(value - cooldown) < 0.05:
                            continue
                        findings.append(
                            f"{path.name}:{n}  {ability_id} ({name}) ships a cooldown of "
                            f"{cooldown:g}s and the comment asserts {value:g}s")

        # ---- 3: a cross-reference to another hero's cooldown -------------
        #
        # ⚠️⚠️ ONLY INSIDE A BLOCK THAT IS ALREADY ASSERTING A COOLDOWN, and the two false
        # positives that forced this are worth naming because both are good comments.
        # `HeroAbility.cs:187` says *"Nemu's 3.2 m void drew 7.5 m"*, which is a RADIUS, and
        # `PhaisterHeroKit.cs:338` says *"5 presses against Cheska's 9, Dante's 8, Zack's 7"*,
        # which is a PRESS COUNT. A possessive followed by a number is not a cooldown claim;
        # a possessive followed by a number, in the paragraph that is retuning a cooldown, is.
        # Sean's *"34 s, UP FROM 6.5. Longer than Zack's 30"* is exactly that paragraph.
        for _ability_id, _name, cooldown, line_no in rows:
            if not cooldown:
                continue

            block = attached_comment(text, line_no)
            asserting = any(RETUNE.search(raw) or SUMMARY.search(raw) for _n, raw in block)
            if not asserting:
                continue

            for n, raw in block:
                for m in CROSSREF.finditer(raw):
                    hero, value = m.group(1), numeric(m.group(2))
                    if value is None or value < 3.0:
                        continue

                    # "Zack's 2.4 m stomp" is a distance wearing the same shape.
                    tail = raw[m.end():m.end() + 3].lstrip()
                    if tail[:1] == "m" and (len(tail) < 2 or not tail[1].isalpha()):
                        continue

                    if round(value, 4) in per_hero.get(hero, set()):
                        continue

                    findings.append(
                        f"{path.name}:{n}  says \"{hero}'s {m.group(2)}\" and {hero} ships no "
                        f"ability with that cooldown "
                        f"({', '.join(f'{v:g}' for v in sorted(per_hero.get(hero, set()))) or 'none'})")

    for f in findings:
        print(f)

    print()
    print(f"{constructors} ability constructors across "
          f"{len(list(ABILITIES.glob('*.cs')))} files, {len(findings)} stat drift finding(s).")

    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main())
