#!/usr/bin/env bash
# One review round for single pieces: shoot each and lay out its sheet.   bash tools/payo_pieces/terrace_piece.sh <version> <piece> [...]
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
v=$1; shift
"$B" -b --python tools/payo_pieces/terrace_review.py -- piece "$@" 2>&1 | grep -E "Error|Traceback|line [0-9]" || true
for n in "$@"; do
  $PY tools/payo_pieces/terrace_sheet.py ${n}_v$v 2 "${n}_front=$n: three-quarter, 1.75 m figure" "${n}_close=close" "${n}_above=from above" "${n}_back=from behind"
done
