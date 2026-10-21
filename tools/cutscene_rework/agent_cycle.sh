#!/bin/bash
# usage: agent_cycle.sh <hero> <tag> <draft dir>
# For several sessions polishing different heroes' cutscenes at once. <draft dir> holds the .cs files this session owns
# (copies of the project's files, edited). It compiles the project OUTSIDE Unity with those standing in, and only if that is
# clean does it take the one lock, copy them over the project's files, ask the open editor for the film, wait for it and
# join the video. The lock is held from the save to the end of the film, so no other session's save recompiles the editor
# in the middle of somebody's film. A new .cs (no file of that name in the project) goes to Runtime/Visual.
K="C:/Users/StarX/Desktop/SCHOOL/career-building/projects/TumbangPreso-Unity/.claude/worktrees/kanto-blender-assembly-909442"
S="$(cd "$(dirname "$0")" && pwd -W | tr '\\' '/')"
HERO="$1"; TAG="$2"; D="$3"
cd "$K" || exit 1
bash "$S/cc_draft.sh" TumbangPreso.Runtime "$D" || { echo "COMPILE FAILED, nothing saved"; exit 1; }
LOCK="$K/Temp/cutscene-film.lock"
for i in $(seq 1 400); do
  mkdir "$LOCK" 2>/dev/null && break
  # A lock nobody has touched for 12 minutes belongs to a session that died.
  [ -n "$(find "$LOCK" -maxdepth 0 -mmin +12 2>/dev/null)" ] && rmdir "$LOCK" 2>/dev/null
  sleep 3
done
trap 'rmdir "$LOCK" 2>/dev/null' EXIT
for f in "$D"/*.cs; do
  b=$(basename "$f")
  t=$(git ls-files -co --exclude-standard "Assets/TumbangPreso/Runtime" | grep "/$b\$" | head -1)
  [ -z "$t" ] && t="Assets/TumbangPreso/Runtime/Visual/$b"
  cmp -s "$f" "$t" || { cp "$f" "$t"; echo "saved $t"; }
done
bash "$S/film.sh" 6 "$HERO" "$TAG"
