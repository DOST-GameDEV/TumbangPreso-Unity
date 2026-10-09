#!/bin/bash
# usage: cycle.sh <hero> <tag> <edit.py or -> <runtime files to draft, by basename...>
# Copies the named runtime files from the project to the draft folder, runs the edit script on them and the table
# script, regenerates the hero's tables only, compiles outside Unity, and ONLY THEN saves and asks for the film.
K="C:/Users/StarX/Desktop/SCHOOL/career-building/projects/TumbangPreso-Unity/.claude/worktrees/kanto-blender-assembly-909442"
S="$(cd "$(dirname "$0")" && pwd -W | tr '\\' '/')"
HERO="$1"; TAG="$2"; EDIT="$3"; shift 3
cd "$K" || exit 1
rm -rf "$S/rt"; mkdir -p "$S/rt"
for b in "$@"; do f=$(git ls-files -co --exclude-standard "Assets/TumbangPreso/Runtime" | grep "/$b\$" | head -1); cp "$f" "$S/rt/" || exit 1; echo "$f" >> "$S/rt/paths.txt"; done
cp tools/author_ultimate_intros.py "$S/author.before.py"
if [ "$EDIT" != "-" ]; then py -3 "$EDIT" "$S/rt" "$K/tools/author_ultimate_intros.py" || { cp "$S/author.before.py" tools/author_ultimate_intros.py; echo "EDIT FAILED, table script restored"; exit 1; }; fi
bash "$S/cc_draft.sh" TumbangPreso.Runtime "$S/rt" || { cp "$S/author.before.py" tools/author_ultimate_intros.py; echo "COMPILE FAILED, nothing saved, table script restored"; exit 1; }
py -3 tools/author_ultimate_intros.py > /dev/null || exit 1
for f in "$S/intros_backup/"*.txt; do b=$(basename "$f"); case "$b" in ${HERO}*) ;; *) cmp -s "$f" "Assets/TumbangPreso/Resources/UltimateIntros/$b" || cp "$f" "Assets/TumbangPreso/Resources/UltimateIntros/$b" ;; esac; done
while read -r f; do cp "$S/rt/$(basename "$f")" "$f"; done < "$S/rt/paths.txt"
git status --short Assets/TumbangPreso/Resources/UltimateIntros
