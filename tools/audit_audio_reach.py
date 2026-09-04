"""Which peers reach each GameServices.Audio call site.

Walks the runtime tree, and for every audio call reports the enclosing method
signature and whether a `NetAuthority.ShouldResolve()` early-return is open at
that brace depth. A gated call is HOST ONLY: silent on every client.
"""
import re
import os

ROOT = "Assets/TumbangPreso/Runtime"
SIG = re.compile(r'^(public|private|protected|internal)\s+[\w<>\[\],\. ]+\s+\w+\s*\(')

rows = []
for dirpath, _, filenames in os.walk(ROOT):
    for fn in filenames:
        if not fn.endswith(".cs"):
            continue
        path = os.path.join(dirpath, fn)
        lines = open(path, encoding="utf-8", errors="replace").read().split("\n")
        depth = 0
        gates = []
        for i, line in enumerate(lines):
            stripped = line.strip()
            is_comment = (stripped.startswith("//") or stripped.startswith("*")
                          or stripped.startswith("/*"))

            # A GATE IN A COMMENT IS NOT A GATE, and this audit was the only one of the
            # three that believed it was. `audit_ability_authority.py` strips comments
            # before looking for a gate and `audit_presentation_reach.py` does the same;
            # this loop tested the raw line, so a doc comment containing the words
            # "ShouldResolve()" and "return" registered a gate at its own brace depth and
            # then covered every method below it in the file.
            #
            # It reported 5 HOST-ONLY sites and all five were false, including the three
            # in NetCue itself: the class that EXISTS to stop a cue being host-only was
            # reported as host-only, because its own header explains the gate it replaces.
            # MatchRpc's two cue relays were the same, from three comments there. A reader
            # trusting that output goes hunting for a bug in the fix.
            if "ShouldResolve()" in line and "return" in line and not is_comment:
                gates.append(depth)
            if ("GameServices.Audio" in line
                    and not stripped.startswith("//")
                    and not stripped.startswith("///")):
                sig = ""
                for j in range(i, -1, -1):
                    s = lines[j].strip()
                    if SIG.match(s):
                        sig = s
                        break
                gated = any(g <= depth for g in gates)
                cue = re.search(r'"([a-z_0-9]+)"', line)
                rows.append((
                    path.replace(os.sep, "/"), i + 1,
                    cue.group(1) if cue else "?",
                    sig[:76],
                    "HOST-ONLY" if gated else "",
                ))
            depth += line.count("{") - line.count("}")
            gates = [g for g in gates if g <= depth]

for r in sorted(rows, key=lambda r: (r[4] == "", r[0], r[1])):
    print("%-10s %s:%d  %-26s %s" % (r[4], r[0], r[1], r[2], r[3]))
print(len(rows), "call sites,",
      sum(1 for r in rows if r[4]), "host-only")
