#!/usr/bin/env bash
# One round for one guardian piece.   bash tools/payo_pieces/bulul_run.sh <piece> <name> <version> [build|paint] [extra sheet args]
# <piece> is the function in bulul_build.py (seated), <name> the file name it writes (bulul_seated).
# The 4th word says where to start: build (shape, bake, paint, review) or paint (paint, review).
PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe
B="/c/Program Files/Blender Foundation/Blender 5.0/blender.exe"
FROM=${4:-build}
if [ "$FROM" = "build" ]; then
  "$B" -b --python tools/payo_pieces/bulul_build.py -- "$1" 2>&1 | grep -E "BUILT|WELD|Error|Traceback|rror|line [0-9]"
fi
$PY tools/payo_pieces/bulul_paint.py "$2" 2>&1 | tail -4
"$B" -b --python tools/payo_pieces/bulul_review.py -- "$2" 2>&1 | grep -E "REVIEWED|Error|Traceback|rror|line [0-9]"
$PY tools/payo_pieces/bulul_sheet.py "$2" "$3" $5 $6
# the second atlas (wood for a stone piece, stone for a wooden one) and its own sheet, when asked:  ... build both
if [ "$5" = "both" ] || [ "$6" = "both" ]; then
  ALT=$($PY -c "import json,sys;print('wood' if json.load(open('Logs/payo/pieces/bulul/work/$2.json')).get('primary','stone')=='stone' else 'stone')")
  $PY tools/payo_pieces/bulul_paint.py "$2" $ALT 2>&1 | tail -1
  "$B" -b --python tools/payo_pieces/bulul_review.py -- "$2" --variant=$ALT 2>&1 | grep -E "REVIEWED|Error|Traceback|rror|line [0-9]"
  $PY tools/payo_pieces/bulul_sheet.py "$2" "$3" --variant=$ALT
  $PY tools/payo_pieces/bulul_sheet.py "$2" "$3" $5 $6
fi
