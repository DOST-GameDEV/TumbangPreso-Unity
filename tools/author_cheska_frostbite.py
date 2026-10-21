"""Append Cheska's distinct held-shoe preparation, preserving every old GLB byte."""
import json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'tools'))
from author_hero_action import author
# t, contact, rootY/Z, torso XYZ, head XY, left/right leg pitch/spread,
# left/right arm pitch/spread. Rigid limbs: no invented elbows or crouch.
SPEC={'punch':.30,'beats':[
 (0.00,1,0,0, 0,0,0, 0,0, 0,0,0,0, 0,0,-85,-6),
 (0.12,1,0,0, 2,-4,0, 7,-8, 0,0,0,0, -28,-12,-92,-12),
 (0.30,1,0,0, 3,-6,0, 11,-12, 0,0,0,0, -88,-32,-95,-15),
 (0.44,1,0,0, 3,-6,0, 11,-12, 0,0,0,0, -86,-30,-95,-15),
 (0.58,1,0,0, 1,-2,0, 5,-5, 0,0,0,0, -28,-10,-90,-10),
 (0.76,1,0,0, 0,0,0, 0,0, 0,0,0,0, 0,0,-85,-6),
 ],'grounded':(0,.12,.30,.44,.58,.76)}
if __name__=='__main__':
 path=Path(sys.argv[sys.argv.index('--')+1]);result=author(path,'hero-cheska-frostbite',SPEC,replace=True)
 print(json.dumps(result,indent=2))
