#!/bin/bash
# usage: film.sh <minutes to wait for the first> <hero> <tag> [<hero> <tag> ...]   films only, saves nothing.
# The editor only takes a request when it is not playing, so the FIRST film waits as long as asked; the rest get 5 min.
K="C:/Users/StarX/Desktop/SCHOOL/career-building/projects/TumbangPreso-Unity/.claude/worktrees/kanto-blender-assembly-909442"
cd "$K" || exit 1
WAIT=$(( $1 * 12 )); shift
while [ -n "$1" ]; do
  HERO="$1"; TAG="$2"; shift 2
  rm -f "Logs/paete-ability-film/done_${HERO}_${TAG}.txt" "Logs/paete-ability-film/failed_${HERO}_${TAG}.txt"
  printf 'introfx %s %s\n' "$HERO" "$TAG" > Temp/paete-ability-film.request
  for i in $(seq 1 $WAIT); do sleep 5; [ -f "Logs/paete-ability-film/done_${HERO}_${TAG}.txt" ] && break; [ -f "Logs/paete-ability-film/failed_${HERO}_${TAG}.txt" ] && break; done
  WAIT=60
  if [ -f "Logs/paete-ability-film/failed_${HERO}_${TAG}.txt" ]; then echo "FAILED $HERO $TAG"; head -12 "Logs/paete-ability-film/failed_${HERO}_${TAG}.txt"; continue; fi
  if [ ! -f "Logs/paete-ability-film/done_${HERO}_${TAG}.txt" ]; then echo "NO FILM $HERO $TAG"; rm -f Temp/paete-ability-film.request; exit 1; fi
  py -3 tools/cutscene_rework/video.py "${HERO}_${TAG}" | tail -1
done
