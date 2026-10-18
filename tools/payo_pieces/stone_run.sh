#!/usr/bin/env bash
# One round for one stone piece: build, weld, review, sheet.   bash tools/payo_pieces/stone_run.sh <piece> <version> [reference picture]
set -e
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
$PY tools/payo_pieces/stone_build.py "$1" | tail -1
"$B" -b --python tools/payo_pieces/stone_weld.py -- "$1" 2>&1 | grep -E "WELDED|Error|Traceback|rror" || true
"$B" -b --python tools/payo_pieces/stone_review.py -- "$1" 2>&1 | grep -E "Error|Traceback|rror|line [0-9]" || true
$PY tools/payo_pieces/stone_build.py sheet "$1" "$2" $3
