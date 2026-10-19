#!/usr/bin/env bash
# Payo market and gallery pieces for the basin's south terrace (tools/payo_pieces/market_kit.py). Writes under
# Logs/payo/pieces/market/.
#   bash tools/payo_pieces/market_run.sh build          paint the shared atlas, build every piece, add normal maps + tangents
#   bash tools/payo_pieces/market_run.sh review [vN]    renders: the market row, the goods row, alone (market/shots/vN_*)
#   bash tools/payo_pieces/market_run.sh install        copies the glbs into Art/Payo/Models, the atlas into Art/Payo/Textures
set -eo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
T="$ROOT/tools/payo_pieces"
O="$ROOT/Logs/payo/pieces/market"
W="$(cygpath -m "$O" 2>/dev/null || echo "$O")"
TW="$(cygpath -m "$T" 2>/dev/null || echo "$T")"
CO="$ROOT/Logs/payo/pieces/calm/court"
MARKET="market_stall_thatch market_stall_mat market_cloth_line market_rice market_bananas market_pots market_baskets market_cloth_stack market_mat market_stool market_bench"
mkdir -p "$O/build" "$O/shots"
case $1 in
  build)
    $PY -I "$T/market_paint.py" "$W"
    "$B" -b --python "$TW/market_kit.py" -- "$W" 2>&1 | grep -E "MARKET|PLAZA|Error|Traceback|line [0-9]" || true
    for n in $MARKET; do
      $PY -I "$T/calm_glb.py" "$O/build/${n}_raw.glb" "$O/market_albedo.png" "$O/$n.glb" "$O/market_normal.png" | sed "s#.*/Logs/#Logs/#"
    done
    $PY -I "$T/calm_glb.py" "$O/build/market_gallery_raw.glb" "$CO/court_platform-atlas.png" "$O/market_gallery.glb" "$CO/court_platform_normal.png" | sed "s#.*/Logs/#Logs/#"
    ;;
  review)
    V="${2:-v1}"
    G=""; for n in market_stall_thatch market_stall_mat market_cloth_line market_gallery; do G="$G $W/$n.glb"; done
    "$B" -b --python "$TW/market_review.py" -- "$W/shots" "${V}_big" $G 2>&1 | grep -E "SHOT|Error|Traceback" || true
    G=""; for n in market_rice market_bananas market_pots market_baskets market_cloth_stack market_mat market_stool market_bench; do G="$G $W/$n.glb"; done
    "$B" -b --python "$TW/market_review.py" -- "$W/shots" "${V}_goods" $G 2>&1 | grep -E "SHOT|Error|Traceback" || true
    ;;
  install)
    for n in $MARKET market_gallery; do cp "$O/$n.glb" "$ROOT/Assets/TumbangPreso/Art/Payo/Models/$n.glb"; done
    cp "$O/market_albedo.png" "$ROOT/Assets/TumbangPreso/Art/Payo/Textures/market_albedo.png"
    cp "$O/market_normal.png" "$ROOT/Assets/TumbangPreso/Art/Payo/Textures/market_normal.png"
    ;;
esac
