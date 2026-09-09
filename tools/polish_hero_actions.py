"""Reauthor all live hero actions and the two retained kit-borrowing rigs.

Run in Blender after model geometry is ready and while Unity is closed.
Only each named action is replaced. Other clips and model geometry are retained.
"""
import json
from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from author_hero_action import HEROES,author

root=Path(__file__).resolve().parents[1]
folder=root/"Assets/TumbangPreso/Art/characters/persons"
reports=[]
for hero,actions in HEROES.items():
    for filename in [f"team-{hero}.glb","team-custom.glb","team-custom-base.glb"]:
        for name,spec in actions.items():
            report=author(folder/filename,name,spec,replace=True)
            reports.append(report)
            print("HERO_FINISH "+json.dumps(report),flush=True)
(root/"Logs/hero-action-finish.json").write_text(json.dumps(reports,indent=2)+"\n")
