"""Author only Dante's weight-bearing held-shoe preparation."""
import json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'tools'))
from author_hero_action import author
# Rigid legs remain planted. Shoulder lift, weight catch, then controlled recovery.
SPEC={'punch':.32,'beats':[
 (0.00,1,0,0, 0,0,0, 0,0, 0,0,0,0, 0,0,-88,-12),
 (0.14,1,0,0, -3,-5,-2, -2,-6, 0,0,0,0, -18,18,-105,-18),
 (0.32,1,0,0, 10,5,4, 8,-6, 0,0,0,0, 10,26,-75,-20),
 (0.43,1,0,0, 8,4,3, 6,-5, 0,0,0,0, 8,24,-80,-20),
 (0.60,1,0,0, 4,1,1, 2,-2, 0,0,0,0, -8,12,-94,-15),
 (0.88,1,0,0, 0,0,0, 0,0, 0,0,0,0, 0,0,-88,-12),
 ],'grounded':(0,.14,.32,.43,.60,.88)}
if __name__=='__main__':
 path=Path(sys.argv[sys.argv.index('--')+1]);print(json.dumps(author(path,'hero-dante-boulder',SPEC,replace=True),indent=2))
