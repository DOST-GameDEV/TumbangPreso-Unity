#!/bin/bash
# usage: cc_draft.sh <Assembly> <draft dir>   (run from the kanto worktree root)
# Compiles the assembly with every .cs in <draft dir> standing in for the project file of the same name; a .cs with no
# such project file is added as a new one. RUNTIME_REF, if set, replaces the runtime reference (an editor draft that
# needs members a runtime draft adds). Run cc_rt.sh / cc_ed.sh once first: this reuses their rsp and ref dlls.
A="$1"; D="$2"
O="C:/Users/StarX/AppData/Local/Temp/claude/C--Users-StarX-Desktop-SCHOOL-career-building-projects-TumbangPreso-Unity--claude-worktrees-kanto-blender-assembly-909442/410a99e0-f30a-4abc-9f52-d80c489984a2/scratchpad/out_int"
mkdir -p "$D/out"
cp "$O/$A.rsp" "$D/out/$A.rsp"
sed -i -e "s#^-out:.*#-out:\"$D/out/$A.dll\"#" -e "s#^-refout:.*#-refout:\"$D/out/$A.ref.dll\"#" "$D/out/$A.rsp"
if [ -n "$RUNTIME_REF" ]; then sed -i "s#[^\"]*TumbangPreso\.Runtime\.ref\.dll#$RUNTIME_REF#" "$D/out/$A.rsp"; fi
n=0; m=0
for f in "$D"/*.cs; do
  b=$(basename "$f")
  if grep -q "[/\\]$b\"" "$D/out/$A.rsp"; then sed -i -e "/^\"[^\"]*[/\\]$b\"/d" "$D/out/$A.rsp"; n=$((n+1)); else m=$((m+1)); fi
  echo "\"$f\"" >> "$D/out/$A.rsp"
done
# Files saved into the project since that rsp was made (it is a snapshot) are in neither list: add them.
case "$A" in TumbangPreso.Runtime) DIR=Assets/TumbangPreso/Runtime ;; *) DIR=Assets/TumbangPreso/Editor ;; esac
tr '\\' '/' < "$D/out/$A.rsp" | grep -o "[^/\"]*\.cs\"" | tr -d '"' | sort -u > "$D/out/known.txt"
k=0
for f in $(git ls-files -o --exclude-standard "$DIR" | grep '\.cs$'); do
  b=$(basename "$f")
  if ! grep -qxF "$b" "$D/out/known.txt"; then echo "\"$f\"" >> "$D/out/$A.rsp"; k=$((k+1)); fi
done
echo "swapped $n files, added $m, picked up $k saved since"
dotnet "C:/Program Files/dotnet/sdk/10.0.400/Roslyn/bincore/csc.dll" -nologo "@$D/out/$A.rsp" 2>&1 | grep -E "error|rror CS" | sort -u | head -60
rc=${PIPESTATUS[0]}; echo "exit $rc"; exit $rc
