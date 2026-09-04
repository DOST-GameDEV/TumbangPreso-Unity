"""Which peers SEND each NetCue call, as opposed to which peers HEAR it.

`audit_audio_reach.py` asks the first question a cue has: does it reach everybody,
or is it stuck behind a host gate and silent on three screens. This asks the
second one, which nothing asked until 2026-09-04 and which has the opposite
failure mode.

`NetCue.Play` does two things on one line: it plays locally AND it relays. So a
call site is correct in exactly two shapes, and wrong in a third:

  * HOST-ONLY    - only the host reaches the line. It plays there and relays once,
                   and the other three peers hear that one copy. This is the shape
                   `NetCue` was written for (`Carrier.HostThrowAt`, `Lata.SetUpright`).

  * SUPPRESSED   - every peer reaches the line and a `using (NetCue.SuppressRelay())`
                   is open, so every machine plays it locally and nobody sends. This
                   is the shape `HeroAbilitySystem.PlayCastConfirm` uses, and its note
                   says why: a cue that also relayed would be broadcast once per peer,
                   so four players means four copies of one cast a few tens of
                   milliseconds apart.

  * UNGATED      - every peer reaches the line and nothing suppresses. Each one plays
                   locally and each one relays, so a cue is heard once for itself plus
                   once per other peer. THIS IS THE FLAM, and it is the thing this
                   audit exists to find. The deleted `sfx_lrt_rumble` is the receipt.

⚠️⚠️ THE GATE IS USUALLY IN THE CALLER, AND AN AUDIT THAT ONLY READS THE ENCLOSING
METHOD IS WRONG ABOUT MOST OF THIS FILE TREE. `Slipper.Land` plays `slipper_land`
with no gate of its own; it is reached only from `FixedUpdate`, which opens with
`if (!NetAuthority.ShouldResolve()) return;`. The first version of this audit
called that a flam, along with eleven more like it. So gatedness is PROPAGATED:
a method with no gate of its own is host-only when every call to it inside its own
file is host-only, iterated to a fixed point.

⚠️⚠️ AND A KIT IS SUPPRESSED BY ITS CALLER FOR THE SAME REASON. Every entry point
into a `HeroKit` or `HeroAbility` runs inside a scope opened in
`HeroAbilitySystem` (`Kit.Tick`, `CastWithContext`, the rollback), so a
`NetCue.Play` written inside a kit is suppressed without saying so at the call
site. Those rows read KIT, and the wrapping statements are asserted below so that
deleting one fails here rather than going quiet in a match.

⚠️ THE PROPAGATION IS WITHIN ONE FILE ONLY, which is why a row can still be wrong
in the safe direction: a private helper called from another file reads UNGATED
until somebody looks. That is the direction to be wrong in, and every UNGATED row
is meant to be read rather than counted.

⚠️ IT EXITS NON-ZERO ON AN UNGATED ROW, so it gates a verification pass like the
other audits in this folder.

`docs/TODO.md` § 135.6.
"""
import os
import re
import sys

ROOT = "Assets/TumbangPreso/Runtime"
SIG = re.compile(r"^(?:public|private|protected|internal)[\w<>\[\],\.\?\s]*?\s(\w+)\s*\(")
TYPE = re.compile(r"^\s*(?:public|internal|private|protected|sealed|abstract|static|partial|\s)*"
                  r"class\s+(\w+)\s*(?::\s*([\w\.,<>\s]+))?", re.M)
CALL = re.compile(r"NetCue\.(?:Play|PlayVaried|PlayImpact)\s*\(")
CUE = re.compile(r'"([A-Za-z_0-9]+)"')
GATE = re.compile(r"(?:ShouldResolve\(\)|NetAuthority\.IsHost)")

WRAPPED = [
    "using (NetCue.SuppressRelay()) Kit.Tick(_context, dt);",
    "using (NetCue.SuppressRelay()) outcome = CastWithContext(slot, _context);",
    "using (NetCue.SuppressRelay()) ability.RollBackPredictedCast(_context);",
]

# ⚠️⚠️ THE FOURTH CORRECT SHAPE, AND IT IS THE ONE THIS TOOL CANNOT SEE FOR ITSELF.
# A line reached only from a LOCAL INPUT read runs on exactly one peer, for a reason that
# has nothing to do with an authority gate: a remote body has no input reader, and the
# host's copy of a seat a client owns has neither a reader nor an `AIController`. So the
# press happens once, in one process, and `NetCue.Play` relaying from there is correct and
# is in fact the only way the other three peers hear it at all.
#
# ⚠️ IT IS A NAMED LIST RATHER THAN A PATTERN, AND THAT IS DELIBERATE. "Somewhere above this
# line there is an `Intent` read" is exactly the kind of inference that made the first
# version of this tool wrong about 42 of 48 rows. Each row below names the file, the cue and
# the GUARD LINE that makes the claim true, and the guard is asserted to still exist, so
# deleting it fails here rather than going quiet in a match. That is `WRAPPED`'s contract
# applied to the other half of the problem.
#
# `docs/TODO.md` § 135.6 carried this row as open work needing "an OWNER-DRIVEN verdict the
# tool cannot currently express".
OWNER_DRIVEN = [
    {
        "file": "CombatVerbs.cs",
        "cue": "bump_swing",
        "guard": "if (!_motor.Intent.JustPressed(Verb.Grab)) return;",
        "why": "StepShove runs only on the peer whose input filled Intent, so the swing is "
               "played once and relayed once.",
    },
]


def strip_comment(line):
    s = line.strip()
    if s.startswith("//") or s.startswith("*") or s.startswith("/*"):
        return ""
    return line


def kit_types(files):
    """Every type entered through HeroAbilitySystem's suppressed calls."""
    names = {"HeroKit", "HeroAbility"}
    changed = True
    while changed:
        changed = False
        for _, text, _ in files:
            for m in TYPE.finditer(text):
                bases = m.group(2) or ""
                if any(b in bases for b in names) and m.group(1) not in names:
                    names.add(m.group(1))
                    changed = True
    return names


def scan(path, lines):
    """Per-file: method spans, their own gates, in-file calls, and cue sites."""
    depth = 0
    gates, suppress, types = [], [], []
    methods = {}          # name -> {"gated","suppressed","calls":set()}
    current = None
    stack = []            # (depth, method name)
    sites = []

    for i, raw in enumerate(lines):
        line = strip_comment(raw)
        stripped = line.strip()

        t = TYPE.match(raw)
        if t and line:
            types.append((depth, t.group(1)))

        m = SIG.match(stripped)
        if m and line and not stripped.endswith(";"):
            current = m.group(1)
            methods.setdefault(current, {"gated": False, "suppressed": False, "calls": set()})
            stack.append((depth, current))

        if stripped.startswith("if") and GATE.search(stripped) and "return" in stripped:
            gates.append(depth)
            if current:
                methods[current]["gated"] = True

        if stripped.startswith("using") and "NetCue.SuppressRelay()" in stripped:
            suppress.append(depth)
            if current:
                methods[current]["suppressed"] = True

        if line and current:
            for name in re.findall(r"\b(\w+)\s*\(", line):
                if name != current:
                    methods[current]["calls"].add(name)

        if CALL.search(line):
            sig = ""
            for j in range(i, -1, -1):
                s = lines[j].strip()
                if SIG.match(s) and not s.endswith(";"):
                    sig = s
                    break
            cue = CUE.search(line)
            sites.append({
                "path": path, "line": i + 1,
                "cue": cue.group(1) if cue else "?",
                "type": types[-1][1] if types else "?",
                "method": current or "?",
                "sig": sig[:62],
                "gated_here": any(g <= depth for g in gates),
                "suppressed_here": any(g <= depth for g in suppress),
            })

        depth += line.count("{") - line.count("}")
        gates = [g for g in gates if g <= depth]
        suppress = [g for g in suppress if g <= depth]
        types = [t for t in types if t[0] <= depth]
        stack = [s for s in stack if s[0] <= depth]
        current = stack[-1][1] if stack else None

    return methods, sites


def propagate(methods, key):
    """A method with no gate of its own inherits one when every in-file caller has it."""
    callers = {name: set() for name in methods}
    for name, info in methods.items():
        for callee in info["calls"]:
            if callee in callers:
                callers[callee].add(name)

    for _ in range(12):
        changed = False
        for name, info in methods.items():
            if info[key] or not callers[name]:
                continue
            if all(methods[c][key] for c in callers[name]):
                info[key] = True
                changed = True
        if not changed:
            break
    return methods


def owner_driven(site):
    """True when this exact file and cue is on the OWNER_DRIVEN list."""
    for rule in OWNER_DRIVEN:
        if site["path"].endswith("/" + rule["file"]) and site["cue"] == rule["cue"]:
            return True
    return False


def main():
    files = []
    for dirpath, _, filenames in os.walk(ROOT):
        for fn in sorted(filenames):
            if fn.endswith(".cs"):
                p = os.path.join(dirpath, fn).replace(os.sep, "/")
                text = open(p, encoding="utf-8", errors="replace").read()
                files.append((p, text, text.split("\n")))

    kits = kit_types(files)
    rows = []

    for path, _, lines in files:
        methods, sites = scan(path, lines)
        if not sites:
            continue
        propagate(methods, "gated")
        propagate(methods, "suppressed")

        for s in sites:
            info = methods.get(s["method"], {"gated": False, "suppressed": False})
            if s["suppressed_here"] or info["suppressed"]:
                verdict = "SUPPRESSED"
            elif s["type"] in kits:
                verdict = "KIT"
            elif s["gated_here"] or info["gated"]:
                verdict = "HOST-ONLY"
            elif owner_driven(s):
                # ⚠️ CHECKED AFTER THE GATES, NEVER BEFORE THEM. A row that is ALSO host-gated
                # should read HOST-ONLY, because that is the stronger claim and the one a
                # reader can verify from the source without consulting this list.
                verdict = "OWNER-DRIVEN"
            else:
                verdict = "UNGATED"
            rows.append((verdict, s))

    order = {"UNGATED": 0, "OWNER-DRIVEN": 1, "SUPPRESSED": 2, "KIT": 3, "HOST-ONLY": 4}
    rows.sort(key=lambda r: (order[r[0]], r[1]["path"], r[1]["line"]))

    for verdict, s in rows:
        print("%-11s %s:%d  %-24s %-26s %s"
              % (verdict, s["path"], s["line"], s["cue"], s["type"], s["sig"]))

    system = os.path.join(ROOT, "Abilities", "HeroAbilitySystem.cs").replace(os.sep, "/")
    text = open(system, encoding="utf-8", errors="replace").read()
    missing = [w for w in WRAPPED if w not in text]

    # ⚠️ THE OWNER-DRIVEN GUARDS ARE ASSERTED FOR `WRAPPED`'S REASON. An OWNER-DRIVEN verdict
    # is a claim that one specific line keeps the cue on one peer. If that line is edited
    # away, the verdict silently becomes a lie and the row goes on reading green, which is
    # worse than never having claimed it.
    lost = []
    for rule in OWNER_DRIVEN:
        hit = [t for p, t, _ in files if p.endswith("/" + rule["file"])]
        if not hit or rule["guard"] not in hit[0]:
            lost.append("%s: %s" % (rule["file"], rule["guard"]))

    counts = {k: len([r for r in rows if r[0] == k]) for k in order}
    print()
    print("%d NetCue call sites: %d host-only, %d suppressed, %d inside a kit, "
          "%d owner-driven, %d UNGATED."
          % (len(rows), counts["HOST-ONLY"], counts["SUPPRESSED"],
             counts["KIT"], counts["OWNER-DRIVEN"], counts["UNGATED"]))

    if missing:
        print()
        print("MISSING SUPPRESSION SCOPE in HeroAbilitySystem, so every KIT row above is a flam:")
        for w in missing:
            print("   " + w)

    if lost:
        print()
        print("AN OWNER-DRIVEN GUARD IS GONE, so its row is claiming a peer count nobody checks:")
        for w in lost:
            print("   " + w)

    return 1 if (counts["UNGATED"] or missing or lost) else 0


if __name__ == "__main__":
    sys.exit(main())
