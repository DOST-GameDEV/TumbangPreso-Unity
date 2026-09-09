"""Author bounded, foot-supported walk/run cycles on the existing seven-bone cast.

The source sprint swings rigid legs almost horizontal. These cycles keep a clear
support foot and a modest forward body line without adding a rig or gameplay rule.
Only walk/sprint are replaced; hands, hero actions and retrieval clips remain.
Run in Blender while Unity is closed, then rebuild the roster.
"""
import json
import math
from pathlib import Path
import re
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from glb_action import BONES,Rig,rotations,append_action

root=Path(__file__).resolve().parents[1]
source=(root/"Assets/TumbangPreso/Editor/RosterBookBuilder.cs").read_text(encoding="utf-8")
table=source.split("PersonModels =",1)[1].split("};",1)[0]
entries=re.findall(r'\{\s*"([^"]+)"\s*,\s*"([^"]+\.glb)"',table)
reports=[]
for character,relative in entries:
    path=root/"Assets/TumbangPreso/Art"/relative
    for name,period,amplitude,arm_swing,lean in [("walk",.72,28,12,2),("sprint",.48,44,24,4)]:
        rig=Rig(path)
        feet=rig.owned_by("leg-left")+rig.owned_by("leg-right")
        foot_floor=min(rig.height_of(v,rig.rest) for v in feet)
        count=round(period*60)+1
        times=[period*i/(count-1) for i in range(count)]
        tracks={bone:[] for bone in BONES};positions=[]
        for i,t in enumerate(times):
            phase=math.tau*i/(count-1)
            stride=math.sin(phase)
            angles={"root":(lean,0,0),"torso":(lean*2,2*stride,0),
                    "head":(-lean*2,-stride,0),
                    "leg-left":(-amplitude*stride,0,0),"leg-right":(amplitude*stride,0,0),
                    "arm-left":(arm_swing*stride,0,0),"arm-right":(-arm_swing*stride,0,0)}
            rots=rotations(angles);posed=rig.posed(rots)
            y=foot_floor-min(rig.height_of(v,posed) for v in feet)
            positions.append((0.,float(y),0.))
            for bone,q in rots.items():tracks[bone].append((q.x,q.y,q.z,q.w))
        report=append_action(rig,name,times,tracks,positions,order=BONES,replace=True)
        report.update(id=character,leg_amplitude_degrees=amplitude,root_lift_max=max(p[1] for p in positions))
        reports.append(report);print("GAIT "+json.dumps(report),flush=True)
(root/"Logs/grounded-gait-authoring.json").write_text(json.dumps(reports,indent=2)+"\n")
