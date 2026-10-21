#!/usr/bin/env bash
# The CALM texture pass for one piece (Logs/payo/pieces/CALM_STYLE.md): repaint, put the atlas and normal map into a
# copy of the approved glb, render old against calm, lay out the sheet. Writes only under Logs/payo/pieces/calm/.
#   bash tools/payo_pieces/calm_run.sh house house_tower
#   bash tools/payo_pieces/calm_run.sh gate gate_portal
#   bash tools/payo_pieces/calm_run.sh stone monolith
#   bash tools/payo_pieces/calm_run.sh together
set -eo pipefail
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
APP=ArtSource/payo/pieces-approved-2026-10-12
C=Logs/payo/pieces/calm
SHOTS=$C/shots
lift() { [ "$1" = monolith ] && echo 13.7 || echo 0; }
if [ "$1" = together ]; then
  "$B" -b --python tools/payo_pieces/calm_review.py -- together $SHOTS \
    house_tower $APP/house/house_tower.glb $C/house/house_tower.glb 0 -24 \
    gate_portal $APP/gate/gate_portal.glb $C/gate/gate_portal.glb 0 0 \
    monolith $APP/stone/monolith.glb $C/stone/monolith.glb 13.7 20 2>&1 | grep -E "Error|Traceback|rror" || true
  $PY tools/payo_pieces/calm_sheet.py together $SHOTS $C/pilot_together.png "${2:-}"
  exit 0
fi
FAM=$1; NAME=$2
case $FAM in
  house) $PY tools/payo_pieces/house_paint.py --calm $NAME; ATLAS=$C/house/$NAME.png ;;
  gate)  $PY tools/payo_pieces/gate_build.py $NAME --calm | tail -2; ATLAS=$C/gate/$NAME-atlas.png ;;
  stone) $PY tools/payo_pieces/stone_build.py $NAME --calm | tail -2; ATLAS=$C/stone/$NAME-atlas.png ;;
esac
NORMAL=$C/$FAM/${NAME}_normal.png
$PY tools/payo_pieces/calm_glb.py $APP/$FAM/$NAME.glb $ATLAS $C/$FAM/$NAME.glb $NORMAL
"$B" -b --python tools/payo_pieces/calm_review.py -- piece $NAME $APP/$FAM/$NAME.glb $C/$FAM/$NAME.glb $(lift $NAME) $SHOTS 2>&1 | grep -E "Error|Traceback|rror" || true
$PY tools/payo_pieces/calm_sheet.py piece $NAME $SHOTS $C/$FAM/${NAME}_calm.png "${3:-}"
