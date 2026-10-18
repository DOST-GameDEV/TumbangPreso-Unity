#!/usr/bin/env bash
# Payo's pads remodelled as carved temple stone (tools/payo_pieces/pad_meso.py). Writes ONLY under Logs/payo/pieces/pads/.
#   bash tools/payo_pieces/pad_run.sh build [names]    paint + parts, weld in Blender, add the normal map: pads/<name>.glb
#   bash tools/payo_pieces/pad_run.sh review [vN] [props]  new vs the current Payo glb vs the Arena original: pads/pads_vN.png
set -eo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
T="$ROOT/tools/payo_pieces"
O="$ROOT/Logs/payo/pieces/pads"
W="$(cygpath -m "$O" 2>/dev/null || echo "$O")"
mkdir -p "$O/build" "$O/shots"
case $1 in
  build)
    shift
    NAMES="$*"
    [ -z "$NAMES" ] && NAMES="pad_geyser pad_speed pickup_plinth"
    $PY -I "$T/pad_meso.py" $NAMES | grep -v "^wrote" || true
    "$B" -b --python "$T/stone_weld.py" -- $NAMES "--out=$W/build" 2>&1 | grep -E "WELDED|Error|Traceback" || true
    for n in $NAMES; do
      $PY -I "$T/calm_glb.py" "$O/build/$n.glb" "$O/build/$n-atlas.png" "$O/$n.glb" "$O/build/${n}_normal.png" | sed "s#.*/Logs/#Logs/#"
    done
    ;;
  review)
    V="${2:-v1}"
    shift; shift || true
    "$B" -b --python "$T/pad_review.py" -- "$W" "$(cygpath -m "$ROOT")" $* 2>&1 | grep -E "SHOT|Error|Traceback" || true
    $PY -I "$T/pad_sheet.py" "$O/shots" "$O/pads_$V.png" "$V" $*
    ;;
esac
