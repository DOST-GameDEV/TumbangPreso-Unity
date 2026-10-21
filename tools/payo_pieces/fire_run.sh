#!/usr/bin/env bash
# Payo night fires (tools/payo_pieces/fire_kit.py, 2026-10-19). Writes under Logs/payo/pieces/fire/.
#   bash tools/payo_pieces/fire_run.sh build          paint the atlas, build both pieces, add the normal map and tangents
#   bash tools/payo_pieces/fire_run.sh review [vN]    renders each piece alone (turntable) and the pair: fire/shots/vN_*.png
#   bash tools/payo_pieces/fire_run.sh install        copies the two glbs into Assets/TumbangPreso/Art/Payo/Models
set -eo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
T="$ROOT/tools/payo_pieces"
O="$ROOT/Logs/payo/pieces/fire"
W="$(cygpath -m "$O" 2>/dev/null || echo "$O")"
mkdir -p "$O/build" "$O/shots"
case $1 in
  build)
    $PY -I "$T/fire_paint.py" "$W"
    "$B" -b --python "$T/fire_kit.py" -- "$W" 2>&1 | grep -E "FIRE KIT|Error|Traceback|line [0-9]" || true
    for n in fire_pedestal fire_torch; do
      $PY -I "$T/calm_glb.py" "$W/build/${n}_raw.glb" "$W/fire_kit-atlas.png" "$W/$n.glb" "$W/fire_kit_normal.png" | sed "s#.*/Logs/#Logs/#"
    done
    ;;
  review)
    V="${2:-v1}"
    "$B" -b --python "$T/fire_review.py" -- "$W" "$V" 2>&1 | grep -E "Error|Traceback|line [0-9]" || true
    $PY -I "$T/fire_sheet.py" "$W" "$V"
    ;;
  install)
    for n in fire_pedestal fire_torch; do cp "$O/$n.glb" "$ROOT/Assets/TumbangPreso/Art/Payo/Models/$n.glb"; done
    ;;
esac
