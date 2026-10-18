#!/usr/bin/env bash
# Payo basin dressing kit (tools/payo_pieces/plaza_kit.py). Writes under Logs/payo/pieces/plaza/.
#   bash tools/payo_pieces/plaza_run.sh build          the plinth, planter (court stone + the court's normal map) and vine
#   bash tools/payo_pieces/plaza_run.sh review [vN]    renders alone and in place: plaza/shots/vN_*.png, sheet plaza/plaza_vN.png
#   bash tools/payo_pieces/plaza_run.sh install        copies the three glbs into Assets/TumbangPreso/Art/Payo/Models
set -eo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
T="$ROOT/tools/payo_pieces"
O="$ROOT/Logs/payo/pieces/plaza"
W="$(cygpath -m "$O" 2>/dev/null || echo "$O")"
CO="$ROOT/Logs/payo/pieces/calm/court"
mkdir -p "$O/build" "$O/shots"
case $1 in
  build)
    "$B" -b --python "$T/plaza_kit.py" -- "$W" 2>&1 | grep -E "PLAZA|Error|Traceback|line [0-9]" || true
    for n in plaza_plinth plaza_planter; do
      $PY -I "$T/calm_glb.py" "$O/build/${n}_raw.glb" "$CO/court_platform-atlas.png" "$O/$n.glb" "$CO/court_platform_normal.png" | sed "s#.*/Logs/#Logs/#"
    done
    ;;
  review)
    V="${2:-v1}"
    "$B" -b --python "$T/plaza_review.py" -- "$W" "$(cygpath -m "$ROOT")" "$V" 2>&1 | grep -E "SHOT|Error|Traceback" || true
    ;;
  install)
    for n in plaza_plinth plaza_planter plaza_vine plaza_vine_long; do cp "$O/$n.glb" "$ROOT/Assets/TumbangPreso/Art/Payo/Models/$n.glb"; done
    ;;
esac
