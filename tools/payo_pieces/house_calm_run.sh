#!/usr/bin/env bash
# The CALM pass for the house family, the rest after the pilot (Logs/payo/pieces/CALM_STYLE.md).
#   bash tools/payo_pieces/house_calm_run.sh "<note>" house_gong house_granary ...   paint, glb, review shots, sheet
#   bash tools/payo_pieces/house_calm_run.sh --paint-only house_gong ...           paint and glb only
# Writes only under Logs/payo/pieces/calm/ (house/ and shots/). Absolute paths throughout.
set -eo pipefail
R="$(cd "$(dirname "$0")/../.." && pwd)"
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
APP="$R/ArtSource/payo/pieces-approved-2026-10-12/house"
C="$R/Logs/payo/pieces/calm/house"
SHOTS="$R/Logs/payo/pieces/calm/shots"
ONLY=0
if [ "$1" = --paint-only ]; then ONLY=1; shift; NOTE=""; else NOTE="$1"; shift; fi
"$PY" "$R/tools/payo_pieces/house_paint.py" --calm --out "$C" "$@"
for N in "$@"; do
  "$PY" "$R/tools/payo_pieces/calm_glb.py" "$APP/$N.glb" "$C/$N.png" "$C/$N.glb" "$C/${N}_normal.png"
done
[ $ONLY = 1 ] && exit 0
for N in "$@"; do
  "$B" -b --python "$R/tools/payo_pieces/calm_review.py" -- piece $N "$APP/$N.glb" "$C/$N.glb" 0 "$SHOTS" 2>&1 | grep -E "Error|Traceback|rror" || true
  "$PY" "$R/tools/payo_pieces/calm_sheet.py" piece $N "$SHOTS" "$C/${N}_calm.png" "$NOTE"
done
