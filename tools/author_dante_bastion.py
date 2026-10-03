"""Author only Dante's shoulder-led following-barrier brace."""
import json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'tools'))
from author_hero_action import author
# Rigid legs planted; asymmetric shoulder draw, forward set, weighted recovery.
SPEC={'punch':.28,'beats':[
 (0.00,1,0,0, 0,0,0, 0,0, 0,0,0,0, 0,0,0,0),
 (0.12,1,0,0, -4,12,-2, -2,-8, 0,0,0,0, -30,18,-45,-10),
 (0.28,1,0,0, 12,-5,2, -8,4, 0,0,0,0, -82,15,-95,-20),
 (0.39,1,0,0, 10,-4,1, -6,3, 0,0,0,0, -78,14,-90,-18),
 (0.58,1,0,0, 5,-1,0, -3,1, 0,0,0,0, -40,9,-42,-10),
 (0.80,1,0,0, 0,0,0, 0,0, 0,0,0,0, 0,0,0,0),
 ],'grounded':(0,.12,.28,.39,.58,.80)}
if __name__=='__main__':
 path=Path(sys.argv[sys.argv.index('--')+1]);print(json.dumps(author(path,'hero-dante-bastion',SPEC,replace=True),indent=2))
