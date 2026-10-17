#!/bin/bash
# usage: gale.sh <tag> <draft dir>      (run from the kanto worktree root with Git Bash)
# The gale film's whole cycle, the way fxbench.sh is a bench film's. <draft dir> holds what this session is working on:
# AmihanWhirlwind.cs and AmihanVfx.cs at its top, PaeteAbilityFilm.cs and PaeteAbilityFilm.Gale.cs in its `editor` folder.
#   1. compiles the runtime and then the editor OUTSIDE Unity with the drafts standing in; a failure saves nothing;
#   2. checks an editor is there (Temp/UnityLockfile) and takes the one film lock;
#   3. copies what changed to where each file lives, asks the open editor for `gale <tag>` and waits for it;
#   4. joins the frames into an mp4 unless the tag begins `scout`.
# Prints `wrote ...`, or FAILED with the exception, or NO FILM if the editor never answered (in Play, compiling, or closed).
# It never starts Unity.
K="C:/Users/StarX/Desktop/SCHOOL/career-building/projects/TumbangPreso-Unity/.claude/worktrees/kanto-blender-assembly-909442"
S="$(cd "$(dirname "$0")" && pwd -W | tr '\' '/')"
TAG="$1"; D="$2"
cd "$K" || exit 1
OUT="Logs/paete-ability-film"; NAME="fxbench_gale_${TAG}"
unset RUNTIME_REF
bash "$S/cc_draft.sh" TumbangPreso.Runtime "$D" || { echo "RUNTIME COMPILE FAILED, nothing saved"; exit 1; }
export RUNTIME_REF="$D/out/TumbangPreso.Runtime.ref.dll"
bash "$S/cc_draft.sh" TumbangPreso.Editor "$D/editor" || { echo "EDITOR COMPILE FAILED, nothing saved"; exit 1; }
[ -f Temp/UnityLockfile ] || { echo "NO EDITOR (no Temp/UnityLockfile): nothing saved, nothing filmed"; exit 1; }
LOCK="$K/Temp/cutscene-film.lock"
for i in $(seq 1 400); do
  mkdir "$LOCK" 2>/dev/null && break
  [ -n "$(find "$LOCK" -maxdepth 0 -mmin +12 2>/dev/null)" ] && rmdir "$LOCK" 2>/dev/null
  sleep 3
done
trap 'rmdir "$LOCK" 2>/dev/null' EXIT
put() { cmp -s "$1" "$2" || { cp "$1" "$2"; echo "saved $2"; }; }
put "$D/AmihanWhirlwind.cs" Assets/TumbangPreso/Runtime/Visual/AmihanWhirlwind.cs
put "$D/AmihanVfx.cs" Assets/TumbangPreso/Runtime/Visual/AmihanVfx.cs
put "$D/editor/PaeteAbilityFilm.cs" Assets/TumbangPreso/Editor/MapKit/PaeteAbilityFilm.cs
put "$D/editor/PaeteAbilityFilm.Gale.cs" Assets/TumbangPreso/Editor/MapKit/PaeteAbilityFilm.Gale.cs
rm -f "$OUT/done_$NAME.txt" "$OUT/failed_$NAME.txt"
printf 'gale %s\n' "$TAG" > Temp/paete-ability-film.request
for i in $(seq 1 110); do sleep 5; [ -f "$OUT/done_$NAME.txt" ] && break; [ -f "$OUT/failed_$NAME.txt" ] && break; done
if [ -f "$OUT/failed_$NAME.txt" ]; then echo "FAILED gale $TAG"; head -14 "$OUT/failed_$NAME.txt"; exit 1; fi
if [ ! -f "$OUT/done_$NAME.txt" ]; then echo "NO FILM gale $TAG"; rm -f Temp/paete-ability-film.request; exit 1; fi
cat "$OUT/done_$NAME.txt"
case "$TAG" in scout*) exit 0 ;; esac
py -3 tools/cutscene_rework/fxvideo.py "gale_${TAG}" | tail -1
