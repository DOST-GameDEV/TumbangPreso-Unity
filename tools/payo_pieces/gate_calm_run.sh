#!/usr/bin/env bash
# The CALM pass for the gate family, the rest after the pilot (Logs/payo/pieces/CALM_STYLE.md).
#   bash tools/payo_pieces/gate_calm_run.sh "<note>" gate_totem_a gate_totem_b gate_stair gate_guardians
# --matched puts every stone on the one shared calm stone (165, 156, 140). Writes only under Logs/payo/pieces/calm/.
set -eo pipefail
R="$(cd "$(dirname "$0")/../.." && pwd)"
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
APP="$R/ArtSource/payo/pieces-approved-2026-10-12/gate"
C="$R/Logs/payo/pieces/calm/gate"
SHOTS="$R/Logs/payo/pieces/calm/shots"
NOTE="$1"; shift
for N in "$@"; do
  "$PY" "$R/tools/payo_pieces/gate_build.py" $N --calm --matched --out "$C" | tail -1
  "$PY" "$R/tools/payo_pieces/calm_glb.py" "$APP/$N.glb" "$C/$N-atlas.png" "$C/$N.glb" "$C/${N}_normal.png"
  "$B" -b --python "$R/tools/payo_pieces/calm_review.py" -- piece $N "$APP/$N.glb" "$C/$N.glb" 0 "$SHOTS" 2>&1 | grep -E "Error|Traceback|rror" || true
  "$PY" "$R/tools/payo_pieces/calm_sheet.py" piece $N "$SHOTS" "$C/${N}_calm.png" "$NOTE"
done
