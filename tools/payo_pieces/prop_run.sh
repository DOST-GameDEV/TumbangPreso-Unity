#!/usr/bin/env bash
# One round for one set of props: build, weld, review, sheet.   bash tools/payo_pieces/prop_run.sh <set letter> <version> [prop ...]
set -e
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
L=$1; V=$2; shift; shift
$PY tools/payo_pieces/prop_set_$L.py
"$B" -b --python tools/payo_pieces/prop_weld.py -- $L "$@" 2>&1 | grep -E "WELDED|Error|Traceback|rror|line [0-9]" || true
"$B" -b --python tools/payo_pieces/prop_review.py -- $L 2>&1 | grep -E "Error|Traceback|rror|line [0-9]" || true
$PY tools/payo_pieces/prop_sheet.py $L $V
