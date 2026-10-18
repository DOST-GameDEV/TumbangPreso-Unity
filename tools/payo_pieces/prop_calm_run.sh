#!/usr/bin/env bash
# The CALM pass for one set of props (Logs/payo/pieces/CALM_STYLE.md): repaint the set's atlas with its height and
# normal maps (same pieces, same layout), put them into a copy of every approved prop glb of the set, render the set
# OLD against CALM, lay out the sheet. Writes only under Logs/payo/pieces/calm/prop/.
#   bash tools/payo_pieces/prop_calm_run.sh <set letter> [note] [paint|review]
set -eo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd -W)"
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
APP="$ROOT/ArtSource/payo/pieces-approved-2026-10-12/prop"
C="$ROOT/Logs/payo/pieces/calm/prop"
SHOTS="$C/shots"
L=$1
mkdir -p "$C" "$SHOTS"
if [ "${3:-}" != "review" ]; then
  $PY "$ROOT/tools/payo_pieces/prop_set_$L.py" --calm --out "$C"
  for P in $($PY -c "import json;print(' '.join(p['name'] for p in json.load(open(r'$APP/parts/set$L.json'))['props']))"); do
    # the approved parts must come out of the calm run unchanged: same pieces, same UVs
    cmp -s "$APP/parts/$P.glb" "$C/parts/$P.glb" || { echo "PARTS CHANGED: $P"; exit 1; }
    $PY "$ROOT/tools/payo_pieces/calm_glb.py" "$APP/$P.glb" "$C/set$L-atlas.png" "$C/$P.glb" "$C/set${L}_normal.png" | sed 's#.*/##'
  done
fi
if [ "${3:-}" != "paint" ]; then
  "$B" -b --python "$ROOT/tools/payo_pieces/prop_calm_review.py" -- set $L "$APP" "$C" "$SHOTS" 2>&1 | grep -E "Error|Traceback|rror" || true
  $PY "$ROOT/tools/payo_pieces/prop_calm_sheet.py" set set$L "$SHOTS" "$C/set${L}_calm.png" "${2:-}"
fi
