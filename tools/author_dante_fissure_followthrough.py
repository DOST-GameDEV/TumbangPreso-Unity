"""Replace Dante's duplicate post-introduction windup with planted follow-through.

Run with Blender --background --python THIS -- MODEL.glb.
Angles are raw Unity local eulers, matching the introduction's final authored key.
Only hero-dante-fissure is replaced; the existing GLB binary and other clips stay.
"""
import json
import math
import sys
from pathlib import Path
from mathutils import Quaternion

sys.path.insert(0, str(Path(__file__).resolve().parent))
from glb_action import Rig, append_action

BONES = ('torso', 'head', 'arm-left', 'arm-right', 'leg-left', 'leg-right')
# Time, torso, head, left/right arms, left/right legs. No second overhead raise.
BEATS = [
    (0.00, (29.6,-11.6,0), (-8.56,7.4,0), (-69,29,74.7), (-69,-31.9,-74.7), (-22.35,0,-9.95), (18.2,0,9.95)),
    (0.16, (30,-11,0), (-9,7,0), (-68,28,75), (-68,-31,-75), (-22,0,-10), (18,0,10)),
    (0.40, (33,-10,0), (-11,6,0), (-64,26,76), (-64,-29,-76), (-22,0,-10), (18,0,10)),
    (0.54, (31,-9,0), (-10,5,0), (-62,24,76), (-62,-26,-76), (-21,0,-9), (17,0,9)),
    (0.76, (14,-4,0), (-5,2,0), (-30,10,72), (-30,-11,-72), (-10,0,-5), (8,0,5)),
    (1.00, (0,0,0), (0,0,0), (0,0,65), (0,0,-65), (0,0,0), (0,0,0)),
]


def gltf_rotation(raw):
    """Unity applies Euler Z, X, Y; glTFast's X reflection negates Y/Z."""
    x, y, z = [math.radians(v) for v in raw]
    q = Quaternion((0,1,0),y) @ Quaternion((1,0,0),x) @ Quaternion((0,0,1),z)
    return Quaternion((q.w,q.x,-q.y,-q.z))


def author(path):
    rig = Rig(path)
    before_count = len(rig.gltf['animations'])
    assert sum(a.get('name') == 'hero-dante-fissure' for a in rig.gltf['animations']) == 1
    times = [i/60 for i in range(61)]
    tracks = {bone: [] for bone in BONES}
    roots = []
    error = 0.0
    for t in times:
        a,b = next(((a,b) for a,b in zip(BEATS,BEATS[1:]) if t <= b[0]), (BEATS[-2],BEATS[-1]))
        u = max(0,min(1,(t-a[0])/(b[0]-a[0])))
        u = u*u*(3-2*u)
        raw = {bone: tuple(x+(y-x)*u for x,y in zip(a[i+1],b[i+1])) for i,bone in enumerate(BONES)}
        assert abs(raw['torso'][0]+raw['head'][0]) < 35
        rots = {bone:gltf_rotation(raw[bone]) for bone in BONES}
        low = rig.lowest(rig.posed(rots))
        root_y = rig.floor-low
        error = max(error,abs(low+root_y-rig.floor))
        roots.append((0,root_y,0))
        for bone,q in rots.items():
            tracks[bone].append((q.x,q.y,q.z,q.w))
    result = append_action(rig,'hero-dante-fissure',times,tracks,roots,replace=True)
    assert len(rig.gltf['animations']) == before_count
    result.update(total_clips=before_count, maximum_ground_error=error, initial_unity_eulers=dict(zip(BONES,BEATS[0][1:])))
    return result


if __name__ == '__main__':
    print(json.dumps(author(Path(sys.argv[sys.argv.index('--')+1])),indent=2))
