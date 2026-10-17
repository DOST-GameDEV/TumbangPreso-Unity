#!/bin/bash
# usage: fxbench.sh <effect> <tag> <draft dir>      (effect: trails, wisp, ...)
# The effects bench's whole cycle, the way agent_cycle.sh is a cutscene's. <draft dir> holds what this session is working
# on: runtime .cs files at its top (they stand in for, or are added to, Runtime/Visual/FxStudy), editor .cs files in its
# `editor` folder (Editor/MapKit), and .shader files at its top (Resources/Shaders). Any of the three may be absent.
#   1. compiles the runtime and then the editor OUTSIDE Unity with the drafts standing in; a failure saves nothing;
#   2. takes the one film lock, so no other session's save recompiles the editor in the middle of this film;
#   3. copies what changed into the project, asks the open editor for `fxbench <effect> <tag>` and waits for it;
#   4. joins the frames into an mp4 (fxvideo.py). Lay it beside the reference with fxcompare.py.
# Prints `wrote ...mp4`, or FAILED with the exception, or NO FILM if the editor never answered (in Play, or closed).
# A shader cannot be compiled out here: if a film comes back with a piece missing or magenta, read Logs/Editor.log.
K="C:/Users/StarX/Desktop/SCHOOL/career-building/projects/TumbangPreso-Unity/.claude/worktrees/kanto-blender-assembly-909442"
S="$(cd "$(dirname "$0")" && pwd -W | tr '\\' '/')"
FX="$1"; TAG="$2"; D="$3"
cd "$K" || exit 1
OUT="Logs/paete-ability-film"; NAME="fxbench_${FX}_${TAG}"
if ls "$D"/*.cs >/dev/null 2>&1; then
  bash "$S/cc_draft.sh" TumbangPreso.Runtime "$D" || { echo "RUNTIME COMPILE FAILED, nothing saved"; exit 1; }
  export RUNTIME_REF="$D/out/TumbangPreso.Runtime.ref.dll"
fi
if ls "$D"/editor/*.cs >/dev/null 2>&1; then
  bash "$S/cc_draft.sh" TumbangPreso.Editor "$D/editor" || { echo "EDITOR COMPILE FAILED, nothing saved"; exit 1; }
fi
LOCK="$K/Temp/cutscene-film.lock"
for i in $(seq 1 400); do
  mkdir "$LOCK" 2>/dev/null && break
  # A lock nobody has touched for 12 minutes belongs to a session that died.
  [ -n "$(find "$LOCK" -maxdepth 0 -mmin +12 2>/dev/null)" ] && rmdir "$LOCK" 2>/dev/null
  sleep 3
done
trap 'rmdir "$LOCK" 2>/dev/null' EXIT
save() { # <file> <directory it belongs in when the project has no file of that name> <where to look for one>
  local b t; b=$(basename "$1")
  t=$(git ls-files -co --exclude-standard "$3" | grep "/$b\$" | head -1)
  [ -z "$t" ] && { mkdir -p "$2"; t="$2/$b"; }
  cmp -s "$1" "$t" || { cp "$1" "$t"; echo "saved $t"; }
}
for f in "$D"/*.cs; do [ -f "$f" ] && save "$f" Assets/TumbangPreso/Runtime/Visual/FxStudy Assets/TumbangPreso/Runtime; done
for f in "$D"/editor/*.cs; do [ -f "$f" ] && save "$f" Assets/TumbangPreso/Editor/MapKit Assets/TumbangPreso/Editor; done
for f in "$D"/*.shader; do [ -f "$f" ] && save "$f" Assets/TumbangPreso/Resources/Shaders Assets/TumbangPreso/Resources/Shaders; done
rm -f "$OUT/done_$NAME.txt" "$OUT/failed_$NAME.txt"
printf 'fxbench %s %s\n' "$FX" "$TAG" > Temp/paete-ability-film.request
for i in $(seq 1 84); do sleep 5; [ -f "$OUT/done_$NAME.txt" ] && break; [ -f "$OUT/failed_$NAME.txt" ] && break; done
if [ -f "$OUT/failed_$NAME.txt" ]; then echo "FAILED $FX $TAG"; head -12 "$OUT/failed_$NAME.txt"; exit 1; fi
if [ ! -f "$OUT/done_$NAME.txt" ]; then echo "NO FILM $FX $TAG"; rm -f Temp/paete-ability-film.request; exit 1; fi
py -3 tools/cutscene_rework/fxvideo.py "${FX}_${TAG}" | tail -1
