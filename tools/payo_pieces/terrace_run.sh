#!/usr/bin/env bash
# One round of the terrace test: paint, build, shoot the stack, lay out the sheet.   bash tools/payo_pieces/terrace_run.sh <version> [nopaint]
set -e
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
[ "$2" = "nopaint" ] || $PY tools/payo_pieces/terrace_paint.py wall ground plant | tail -0
"$B" -b --python tools/payo_pieces/terrace_build.py 2>&1 | grep -E "PIECE terrace_straight|Error|Traceback|line [0-9]" || true
"$B" -b --python tools/payo_pieces/terrace_review.py -- stack 2>&1 | grep -E "Error|Traceback|line [0-9]" || true
$PY tools/payo_pieces/terrace_sheet.py terrace_stack_v$1 2 "stack_below=FROM BELOW, eye 1.7 m, 7 m from the first wall" "stack_side=from the side" "stack_above=from above" "stack_close=close, with a 1.75 m figure" "stack_far=FROM BELOW, eye 1.7 m, 22 m away"
