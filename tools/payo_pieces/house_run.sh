#!/bin/sh
# tools/payo_pieces/house_run.sh <name> <vN>: build, paint, export, shoot, sheet.
set -e
HERE="$(cd "$(dirname "$0")" && pwd)"
BL="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
PY="/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe"
"$BL" -b --python "$HERE/house_$1.py" 2>&1 | grep -E "BUILT|Error|error|Traceback|line " || true
"$PY" "$HERE/house_paint.py" "house_$1"
"$BL" -b --python "$HERE/house_finish.py" -- "house_$1" 2>&1 | grep -E "GLB|Error|error|Traceback|line " || true
"$PY" "$HERE/house_paint.py" sheet "house_$1" "$2"
