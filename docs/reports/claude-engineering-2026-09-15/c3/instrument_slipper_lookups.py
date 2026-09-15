"""Rewrite every runtime FindObjectsByType<Slipper>( call in a SCRATCH COPY into a metered call.
Measurement only: run against a git-archive copy, never the live checkout."""
import pathlib, re, sys
root = pathlib.Path(sys.argv[1]); runtime = root / "Assets/TumbangPreso/Runtime"
pattern = re.compile(r"(?:UnityEngine\.)?(?:Object\.)?FindObjectsByType<Slipper>\(")
total = 0
for path in sorted(p for p in runtime.rglob("*.cs") if p.name != "LookupMeter.cs"):
    lines = path.read_text(encoding="utf-8").split("\n"); changed = False
    for i, line in enumerate(lines):
        if line.lstrip().startswith("//") or not pattern.search(line): continue
        site = f"{path.relative_to(runtime).as_posix()}:{i+1}"
        # The closure allocation is inside the meter's own measured window only for the query lambda; see report.
        lines[i] = pattern.sub(f'global::TumbangPreso.LookupMeter.Slippers("{site}", ', line); changed = True; total += 1
        print(site)
    if changed: path.write_text("\n".join(lines), encoding="utf-8")
print("instrumented", total)
