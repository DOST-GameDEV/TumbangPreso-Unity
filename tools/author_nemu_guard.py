"""Append Nemu's small companion command without changing her model or old actions."""
import json
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from author_hero_action import author

# Attention, compact forward instruction, held handoff, then quiet recovery.
# The small head tilt belongs to the child; Kuro performs the separate world task.
SPEC={'punch':.22,'beats':[
 (0.00,1,0,0, 0,0,0, 0,0, 0,0,0,0, 0,0,0,0),
 (0.10,1,0,0, 2,-7,-2, -3,12, 0,0,0,0, -32,4,-24,-4),
 (0.22,1,0,0, 5,-5,-2, -4,8, 0,0,0,0, -82,7,-24,-4),
 (0.38,1,0,0, 4,-4,-1, -3,7, 0,0,0,0, -80,7,-23,-4),
 (0.54,1,0,0, 2,-2,0, -1,3, 0,0,0,0, -40,3,-13,-2),
 (0.76,1,0,0, 0,0,0, 0,0, 0,0,0,0, 0,0,0,0),
 ],'grounded':(0,.10,.22,.38,.54,.76)}
if __name__=='__main__':
 path=Path(sys.argv[sys.argv.index('--')+1])
 print(json.dumps(author(path,'hero-nemu-guard',SPEC),indent=2))
