"""Which peers resolve each ability effect, and which of them may not.

The sibling of `audit_audio_reach.py`, asking the other half of the same question.
That one asks "who HEARS this"; this asks "who DECIDES this", and a wrong answer
here is worth more than a silent cue: a client that staggers a body it does not
own makes three other machines disagree about where the fight is.

⚠️⚠️ WHY IT SCANS THE ABILITY TREE AND NOT THE WHOLE RUNTIME. Every other verb in
the game (punch, lunge, shove, grab, throw) already goes through `MatchRpc` and
was authority-audited when it was written. `docs/TODO.md` § 25.1 is the finding
that the ability layer never was: `grep "Skill\\|Ultimate\\|Ability"` over
`Runtime/Net/` returned nothing outside comments, so eighteen powers reached the
bodies of four players with no host gate anywhere in the path.

A call is reported HOST-ONLY when a `NetAuthority.ShouldResolve()` early return
is open at its brace depth, exactly as the audio audit measures it. HOST-ONLY is
the CORRECT state for anything in the EFFECT column; a blank there is the defect.
"""
import re
import os

ROOT = "Assets/TumbangPreso/Runtime"

# Only the trees that own hero powers. `HeroHazards` is where the trails, walls
# and zones live and is by far the largest surface, so it is audited with the kits.
SUBTREES = ("Abilities",)

# ⚠️ THE LIST IS THE MUTATING SURFACE OF `CharacterMotor` PLUS SCORING, not every
# method an ability calls. Reading a position is free on any peer; changing where
# a body is, how fast it moves, or what the scoreboard says is not.
EFFECT = re.compile(
    r'\.(ApplyStagger|ApplyImpulse|ApplyTrip|ApplyStun|Teleport|ClearStun|ClearTrip'
    r'|Respawn|EnterSpeedZone|ExitSpeedZone|AddScore|ForceDrop|Drop)\s*\('
)

SIG = re.compile(r'^(public|private|protected|internal)\s+[\w<>\[\],\. ]+\s+\w+\s*\(')

# ⚠️ A CALL ON THE CASTER'S OWN BODY IS NOT THE SAME FAULT. `ctx.Motor` and the
# `_rooted` field in `HeroAbility` are the caster rooting itself for a wind-up,
# which every peer may do to its own copy without moving anybody else's body.
# They are reported in their own column rather than dropped, because "self" here
# means "the seat that cast it", and on a remote peer that seat is still a body
# the host owns.
SELFISH = re.compile(r'(ctx\.Motor|_rooted|_motor|caster|Ctx\.Motor)\s*[\?\.]')

rows = []
for dirpath, _, filenames in os.walk(ROOT):
    if not any(("%s%s%s" % (os.sep, s, os.sep)) in dirpath + os.sep for s in SUBTREES):
        continue
    for fn in sorted(filenames):
        if not fn.endswith(".cs"):
            continue
        path = os.path.join(dirpath, fn)
        lines = open(path, encoding="utf-8", errors="replace").read().split("\n")
        depth = 0
        gates = []
        for i, line in enumerate(lines):
            stripped = line.strip()
            is_comment = stripped.startswith("//") or stripped.startswith("///")

            if "ShouldResolve()" in line and "return" in line and not is_comment:
                gates.append(depth)

            m = EFFECT.search(line)
            if m and not is_comment:
                sig = ""
                for j in range(i, -1, -1):
                    s = lines[j].strip()
                    if SIG.match(s):
                        sig = s
                        break
                gated = any(g <= depth for g in gates)
                rows.append((
                    path.replace(os.sep, "/"), i + 1,
                    m.group(1),
                    sig[:64],
                    "HOST-ONLY" if gated else "",
                    "self" if SELFISH.search(line) else "other",
                ))

            depth += line.count("{") - line.count("}")
            gates = [g for g in gates if g <= depth]

# Ungated calls on somebody else's body first: that is the list to fix.
def rank(r):
    return (r[4] == "HOST-ONLY", r[5] == "self", r[0], r[1])


for r in sorted(rows, key=rank):
    print("%-10s %-6s %s:%d  %-16s %s" % (r[4], r[5], r[0], r[1], r[2], r[3]))

open_other = sum(1 for r in rows if not r[4] and r[5] == "other")
open_self = sum(1 for r in rows if not r[4] and r[5] == "self")
print()
print("%d effect call sites, %d host-gated, %d ungated on another body, %d ungated on the caster"
      % (len(rows), sum(1 for r in rows if r[4]), open_other, open_self))
