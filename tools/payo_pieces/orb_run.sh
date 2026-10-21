#!/usr/bin/env bash
# Payo's stamina orb (tools/payo_pieces/orb_build.py). Writes under Logs/payo/pieces/orb/ (install: the game's glb).
#   bash tools/payo_pieces/orb_run.sh build          meshes + atlas + normal map: orb/pickup_orb.glb
#   bash tools/payo_pieces/orb_run.sh review [vN]    Blender shots over the plinth: orb/orb_vN.png
#   bash tools/payo_pieces/orb_run.sh install        copy to Assets/TumbangPreso/Art/Payo/Models/pickup_orb.glb
set -eo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
T="$ROOT/tools/payo_pieces"
O="$ROOT/Logs/payo/pieces/orb"
W="$(cygpath -m "$O" 2>/dev/null || echo "$O")"
mkdir -p "$O/build" "$O/shots"
case $1 in
  build)   $PY -I "$T/orb_build.py" build ;;
  review)
    "$B" -b --python "$T/orb_review.py" -- "$W" "$(cygpath -m "$ROOT")" 2>&1 | grep -E "SHOT|Error|Traceback" || true
    $PY -I "$T/orb_build.py" sheet "${2:-v1}"
    ;;
  install) $PY -I "$T/orb_build.py" install ;;
esac
