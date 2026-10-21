#!/usr/bin/env bash
# PAYO court platform: paint, build, add the normal map, review, sheet.  bash tools/payo_pieces/court_run.sh [shot names...]
# Blender resolves relative paths against C:\, so every path here is absolute.
set -e
R="$(cd "$(dirname "$0")/../.." && pwd -W)"
PY="${PY:-/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe}"
BL="${BL:-/c/Program Files/Blender Foundation/Blender 5.0/blender.exe}"
D="$R/Logs/payo/pieces/calm/court"
"$PY" "$R/tools/payo_pieces/court_paint.py"
"$BL" -b --python "$R/tools/payo_pieces/court_build.py" 2>&1 | grep -E "PART|COURT|Error|Traceback|line [0-9]" || true
"$PY" "$R/tools/payo_pieces/calm_glb.py" "$D/court_platform_raw.glb" "$D/court_platform-atlas.png" "$D/court_platform.glb" "$D/court_platform_normal.png"
rm -f "$D/court_platform_raw.glb"
"$BL" -b --python "$R/tools/payo_pieces/pool_build.py" 2>&1 | grep -E "POOL|Error|Traceback" || true
"$PY" "$R/tools/payo_pieces/pool_finish.py"
"$BL" -b --factory-startup --python "$R/tools/payo_pieces/pool_height_grid.py" 2>&1 | grep -E "GRID|Error|Traceback" || true
"$BL" -b --python "$R/tools/payo_pieces/court_review.py" -- "$D/court_platform.glb;$D/pool_basin.glb" "$D/shots" "$@" 2>&1 | grep -E "REVIEW|Error|Traceback" || true
"$PY" "$R/tools/payo_pieces/court_review.py" sheet "$D/shots" "$D/court_review.png"
