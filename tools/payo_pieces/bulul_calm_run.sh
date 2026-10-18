#!/usr/bin/env bash
# The CALM pass for one guardian (Logs/payo/pieces/CALM_STYLE.md): repaint both atlases, height and normal map, put the
# primary atlas and the normal map into a copy of the approved glb, render old against calm, lay out the sheet.
# Writes only under Logs/payo/pieces/calm/ (bulul/ and shots/).     bash tools/payo_pieces/bulul_calm_run.sh bulul_post [note] [paint|review]
set -eo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd -W)"
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
APP="$ROOT/ArtSource/payo/pieces-approved-2026-10-12/bulul"
C="$ROOT/Logs/payo/pieces/calm/bulul"
SHOTS="$ROOT/Logs/payo/pieces/calm/bulul/shots"
NAME=$1
mkdir -p "$C" "$SHOTS"
if [ "${3:-}" != "review" ]; then
  $PY "$ROOT/tools/payo_pieces/bulul_paint.py" "$NAME" --calm --out "$C"
  $PY "$ROOT/tools/payo_pieces/calm_glb.py" "$APP/$NAME.glb" "$C/$NAME-atlas.png" "$C/$NAME.glb" "$C/${NAME}_normal.png"
fi
if [ "${3:-}" != "paint" ]; then
  "$B" -b --python "$ROOT/tools/payo_pieces/calm_review.py" -- piece "$NAME" "$APP/$NAME.glb" "$C/$NAME.glb" 0 "$SHOTS" 2>&1 | grep -E "Error|Traceback|rror" || true
  # a close look framed to the figure as well (the small guardians are 1.2 m: tiny at the pilot's 8 m)
  D=$($PY -c "import json;h=json.load(open(r'$ROOT/Logs/payo/pieces/bulul/work/$NAME.json'))['height'];import math;print(max(3,int(round(h*1.5+1))))")
  $PY -c "import json;h=json.load(open(r'$ROOT/Logs/payo/pieces/bulul/work/$NAME.json'))['height'];json.dump({'prefix':'${NAME}_close','res':[960,640],'look':[0,0,h*0.45],'dists':[[$D,h*0.35,38]],'items':[{'old':r'$APP/$NAME.glb','calm':r'$C/$NAME.glb','x':0,'y':0}]},open(r'$SHOTS/${NAME}_close.json','w'))"
  "$B" -b --python "$ROOT/tools/payo_pieces/prop_calm_review.py" -- group "$SHOTS/${NAME}_close.json" "$SHOTS" 2>&1 | grep -E "Error|Traceback|rror" || true
  $PY "$ROOT/tools/payo_pieces/prop_calm_sheet.py" piece "$NAME" "$SHOTS" "$C/${NAME}_calm.png" $D "${2:-}"
fi
