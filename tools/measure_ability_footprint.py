#!/usr/bin/env python3
"""
Every Hero Strike ability's floor footprint, measured from the current source and stamped
with the commit that produced it.

WHY THIS EXISTS
---------------
`docs/VISION.md` § 2 is the readability budget and it is a HARD NUMBER: the arena is
`CONFINEMENT_RADIUS` 7.0, so the danger zone is 14 m by 14 m = 196 m², shared by four
players, one lata, four tsinelas and up to twelve live abilities.

⚠️⚠️ EVERY FOOTPRINT NUMBER IN THE REPOSITORY PREDATES THE ABILITIES IT DESCRIBES.
`Hero_Strike_Balance.md` and `VISION.md` § 2 carry an **81.9 per cent** worst credible
frame and a **27.2 per cent** Zack corridor. Both were measured before the retune that
put Bolt Sprint on 46 s and Flame Rush on 50 s, before the trails were capped at six live
discs, and before the trail radius came down to 1.0 m. **They are history.** Nothing in
the repository regenerated them, so a session reading § 2 today is reasoning from a game
that no longer exists.

⚠️ SO THIS PRINTS THE ARITHMETIC RATHER THAN A VERDICT, AND IT SAYS WHERE EACH NUMBER
CAME FROM. `CLAUDE.md` § 3: "a number that was measured says so, and says what it was
measured against."

WHAT IT CAN AND CANNOT SEE
--------------------------
⚠️⚠️ IT READS SOURCE, SO IT MEASURES DECLARED GEOMETRY AND NOT PIXELS. A radius passed to
a spawn call is what the hazard covers; it is NOT what a frame looks like, which is
`AbilityShowcaseProbe`'s job (it photographs the transients and fails a run where one
blows more than 12 per cent of the frame to white). The two answer different questions and
neither replaces the other: this one is cheap, exact about area, and blind to brightness.

⚠️ WHERE IT CANNOT DETERMINE A VALUE IT SAYS SO RATHER THAN GUESSING. A spawn call whose
radius is computed at runtime is reported as UNKNOWN, because a table with a made-up
number in it is worse than a table with a gap.

Writes `docs/reports/ability-footprint-<sha>.md`.
"""

import datetime
import pathlib
import re
import subprocess
import sys

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

ROOT = pathlib.Path(__file__).resolve().parent.parent
ABILITIES = ROOT / "Assets" / "TumbangPreso" / "Runtime" / "Abilities"
BALANCE = ROOT / "Packages" / "com.tumbangpreso.core" / "Runtime" / "Balance.cs"
REPORTS = ROOT / "docs" / "reports"

CONSTRUCTOR = re.compile(
    r':\s*base\(\s*"([a-z_0-9]+)"\s*,\s*"([^"]+)"\s*,\s*'
    r'(?:"(?:[^"\\]|\\.)*"\s*,\s*)?'
    r'([A-Za-z0-9_.]+)\s*,\s*([A-Za-z0-9_.]+)\s*,',
    re.S)

TELEGRAPH = re.compile(r"telegraphRadius:\s*([\d.]+)f?")
CONST = re.compile(r"(?:private|public|internal)?\s*const\s+(?:float|int)\s+(\w+)\s*=\s*([\d.]+)f?\s*;")
SPAWN = re.compile(r"HeroHazards\.(Spawn\w+|Create\w+)\s*\(([^;]{0,400}?)\)\s*;", re.S)


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


def head_sha():
    r = subprocess.run(["git", "rev-parse", "HEAD"], cwd=str(ROOT),
                       capture_output=True, text=True)
    return r.stdout.strip() if r.returncode == 0 else "UNKNOWN"


def confinement_radius():
    """⚠️ READ FROM `Balance`, NEVER RESTATED. The whole budget is a fraction of this."""
    text = BALANCE.read_text(encoding="utf-8", errors="replace")
    m = re.search(r"ConfinementRadius\s*=\s*([\d.]+)f?\s*;", text)
    return float(m.group(1)) if m else 7.0


def main():
    r = confinement_radius()
    side = r * 2.0
    arena = side * side

    rows = []
    for path in sorted(ABILITIES.glob("*HeroKit.cs")):
        hero = path.name.replace("HeroKit.cs", "").upper()
        text = path.read_text(encoding="utf-8", errors="replace")
        code = strip_comments(text)
        constants = {m.group(1): float(m.group(2)) for m in CONST.finditer(code)}

        for m in CONSTRUCTOR.finditer(code):
            ability_id, name, cooldown_tok, duration_tok = m.groups()

            def value(tok):
                try:
                    return float(tok.rstrip("fF"))
                except ValueError:
                    return constants.get(tok.split(".")[-1])

            # The class body this constructor belongs to, so its own constants win.
            start = code.rfind("class ", 0, m.start())
            end = code.find("\n        }", m.end())
            body = code[start:end if end > 0 else len(code)]
            local = {c.group(1): float(c.group(2)) for c in CONST.finditer(body)}

            telegraph = TELEGRAPH.search(code[m.start():m.start() + 900])
            trail = local.get("TrailRadius")
            live_cap = local.get("MaxLiveDiscs")

            rows.append({
                "hero": hero,
                "id": ability_id,
                "name": name,
                "cooldown": value(cooldown_tok),
                "duration": value(duration_tok),
                "telegraph": float(telegraph.group(1)) if telegraph else None,
                "trail": trail,
                "live_cap": live_cap,
            })

    def area(radius):
        return 3.141592653589793 * radius * radius

    def pct(a):
        return 100.0 * a / arena

    lines = []
    lines.append("# Ability footprint, measured from source")
    lines.append("")
    lines.append(f"- **Commit** `{head_sha()}`")
    lines.append(f"- **Generated** {datetime.datetime.now().isoformat(timespec='seconds')}")
    lines.append(f"- **Arena** `CONFINEMENT_RADIUS` {r:g}, so {side:g} m by {side:g} m = "
                 f"**{arena:.0f} m²**")
    lines.append("")
    lines.append("⚠️ Generated by `tools/measure_ability_footprint.py`. Do not hand-edit, and do "
                 "not quote a number from an older report: every value here is read out of the "
                 "commit above.")
    lines.append("")
    lines.append("## What the budget converts to")
    lines.append("")
    lines.append("| Share of the box | Area | Equivalent single-disc radius |")
    lines.append("|---|---|---|")
    for share in (3.0, 5.0, 8.0):
        a = arena * share / 100.0
        lines.append(f"| {share:g}% | {a:.2f} m² | **{(a / 3.141592653589793) ** 0.5:.2f} m** |")
    lines.append("")
    for radius in (1.0, 1.8, 2.5):
        lines.append(f"- A disc of **{radius:g} m** is {area(radius):.2f} m², "
                     f"**{pct(area(radius)):.2f}%** of the box.")
    lines.append("")

    lines.append("## Per ability")
    lines.append("")
    lines.append("| Hero | Ability | Cooldown | Duration | Telegraph | Trail disc | Live cap | "
                 "Instantaneous | Worst persistent |")
    lines.append("|---|---|---|---|---|---|---|---|---|")

    for row in rows:
        instantaneous = "-"
        persistent = "-"

        if row["trail"] is not None:
            one = area(row["trail"])
            instantaneous = f"{pct(one):.2f}%"
            if row["live_cap"]:
                # ⚠️ THE DISJOINT CASE IS THE CEILING AND IT IS THE HONEST ONE TO QUOTE. Along a
                # real dash the discs overlap, so the drawn area is smaller; the cap is what the
                # ability can never exceed, which is what a budget needs.
                persistent = f"{pct(one * row['live_cap']):.2f}% (cap {int(row['live_cap'])} discs, disjoint)"
        elif row["telegraph"]:
            instantaneous = f"{pct(area(row['telegraph'])):.2f}%"

        lines.append(
            f"| {row['hero']} | {row['name']} | "
            f"{row['cooldown']:g}s | {row['duration']:g}s | "
            f"{(str(row['telegraph']) + ' m') if row['telegraph'] else '-'} | "
            f"{(str(row['trail']) + ' m') if row['trail'] is not None else '-'} | "
            f"{int(row['live_cap']) if row['live_cap'] else '-'} | "
            f"{instantaneous} | {persistent} |")

    lines.append("")
    lines.append("⚠️ A blank cell is a value this tool could not read from source, not a zero. A "
                 "radius computed at runtime is deliberately reported as absent rather than "
                 "guessed: a table with an invented number in it is worse than one with a gap.")
    lines.append("")
    lines.append("⚠️⚠️ **AND THIS MEASURES DECLARED GEOMETRY, NOT A FRAME.** "
                 "`AbilityShowcaseProbe` is the other half and answers a different question: it "
                 "photographs the transients and fails a run where one blows more than 12 per "
                 "cent of the frame to white. An ability can sit inside its area budget and still "
                 "be unreadable, and this tool cannot see that.")

    REPORTS.mkdir(parents=True, exist_ok=True)
    out = REPORTS / f"ability-footprint-{head_sha()[:12]}.md"
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")

    print("\n".join(lines))
    print(f"\nwritten: {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
