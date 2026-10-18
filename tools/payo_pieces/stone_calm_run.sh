#!/usr/bin/env bash
# The stone family's CALM pass (Logs/payo/pieces/CALM_STYLE.md), every piece but the monolith (the pilot's).
# Writes only under Logs/payo/pieces/calm/stone/; reads the approved glbs from ArtSource/payo/pieces-approved-2026-10-12/
# (core, tower_head) and Logs/payo/pieces/build/stone/ (the rest), never writes either.
#   bash tools/payo_pieces/stone_calm_run.sh build [piece ...]     repaint, prove the UVs, swap atlas + normal map into the glb
#   bash tools/payo_pieces/stone_calm_run.sh review <piece> [note] old vs calm at 8, 40, 120 m: calm/stone/<piece>_calm.png
#   bash tools/payo_pieces/stone_calm_run.sh stage [note]          the whole stage, layout 0: calm/stone/stage_together.png
set -eo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
T="$ROOT/tools/payo_pieces"
C="$ROOT/Logs/payo/pieces/calm/stone"
SHOTS="$C/shots"
src() { case $1 in core|tower_head|monolith) echo "$ROOT/ArtSource/payo/pieces-approved-2026-10-12/stone/$1.glb" ;; *) echo "$ROOT/Logs/payo/pieces/build/stone/$1.glb" ;; esac; }
lift() { case $1 in ext_*) echo 52.7 ;; mover_*) echo 4.5 ;; pad_*|pickup_*) echo 0 ;; *) echo 13.7 ;; esac; }
mkdir -p "$C" "$SHOTS"
case $1 in
  build)
    shift
    NAMES="$*"
    [ -z "$NAMES" ] && NAMES="core tower_head $($PY "$T/stone_blocks.py" names)"
    $PY "$T/stone_build.py" $NAMES --calm | grep -v "^wrote" || true
    $PY "$T/stone_calm_check.py" $NAMES
    for n in $NAMES; do
      $PY "$T/calm_glb.py" "$(src $n)" "$C/$n-atlas.png" "$C/$n.glb" "$C/${n}_normal.png" | sed "s#.*/Logs/#Logs/#"
    done
    cp "$ROOT/Logs/payo/pieces/build/stone/blocks.json" "$C/blocks.json"
    ;;
  review)
    n=$2
    "$B" -b --python "$T/calm_review.py" -- piece $n "$(src $n)" "$C/$n.glb" $(lift $n) "$SHOTS" 2>&1 | grep -E "Error|Traceback" || true
    $PY "$T/calm_sheet.py" piece $n "$SHOTS" "$C/${n}_calm.png" "${3:-}"
    ;;
  stage)
    "$B" -b --python "$T/stone_stage_review.py" -- --calm "--shots=$SHOTS" 2>&1 | grep -E "Error|Traceback|STAGE" || true
    $PY "$T/stone_calm_stage_sheet.py" "$SHOTS" "$C/stage_together.png" "${2:-}"
    ;;
esac
