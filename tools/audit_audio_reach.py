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
            if "ShouldResolve()" in line and "return" in line:
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
