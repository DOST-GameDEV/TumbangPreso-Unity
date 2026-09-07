"""Independently sample an authored hero cast through Blender's glTF importer.

    blender --background --python tools/verify_hero_action.py -- --hero sean FILE

Same standing as `tools/verify_retrieval_slide.py`, and the same limitation stated in
the same place: this measures DEFORMED MESHES on a re-import, not keyframes and not a
photograph. ⚠️⚠️ **IT IS NOT UNITY MOTION PHOTOGRAPHY. `docs/TODO.md` § 151.16 is still
open and nothing here closes it.**

What it can honestly say, and each of these is a way one of these clips could ship
broken while looking fine in the authoring tool's own report:

* **The action is there under its exact name.** A misspelling is not an error anywhere
  in this project: `CharacterAnimator`'s chain silently falls through to the stock
  clip, and the character plays a kick instead of a cast with nothing logged.
* **It moves the rig.** A clip addresses transforms by PATH, so one whose paths no
  longer match imports perfectly and animates nothing. That is
  `PersonSwapProbe.CheckAnimationBinds`'s whole argument, repeated on the export side
  because that is where the fault would be introduced.
* **The strike is a strike.** ⚠️⚠️ **THE CHECK IS THE SPEED AND NOT THE POSE, AND THE
  FIRST VERSION OF IT ASSERTED THE WRONG THING.** It demanded that the punch frame be
  the furthest from rest, and Supernova failed it, correctly: that clip's furthest pose
  is the APEX, where both arms are 158 degrees overhead, while the impact is a 30 degree
  arm and a 64 degree fold. **A strike is not the biggest pose. It is the fastest
  arrival followed by a stop**, which is the argument `HeroAbilityClips.ClipBuilder`'s
  own header makes about tangents. So the frame-to-frame swing has to PEAK in the run
  into the punch and collapse immediately after it.
* **It ends where it started.** The action chain crossfades out of a cast, so a clip
  whose last frame is not the rest pose hands a rotated body back to the motor.
* **Its grounded beats clear the road.** The author solves for this; this is the
  independent read of the same number, off skinned vertices Blender deformed.
"""
import argparse
import json
import math
from pathlib import Path
import sys

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
from author_hero_action import HEROES

# A cast that moves no bone more than this is not a cast. The smallest extreme in
# Sean's kit is Ignition Cannon's chambered fist, which is a 92 degree arm swing.
STRIKE_FLOOR = 30.0

# How close to the punch the fastest frame has to be. One frame is 0.0167 s and the
# snap eases across the whole approach segment, so the peak lands in the last few frames
# before the impact rather than exactly on it.
PEAK_WINDOW = 0.06

# What "stops dead" is allowed to mean: the frame after the impact may carry at most
# this fraction of the peak speed. Smoothstep leaves a pose from rest, so a correctly
# built punch reads near zero here, and this is loose on purpose.
STOP_RATIO = 0.45

# What "ends where it started" is allowed to mean, in degrees on any one bone.
REST_TOLERANCE = 1.0

DEG = 57.29578


def measure(path, name, spec):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.fps = 60
    bpy.ops.import_scene.gltf(filepath=str(Path(path).resolve()))

    arm = next(o for o in scene.objects if o.type == "ARMATURE")
    clip = bpy.data.actions.get(name)
    assert clip is not None, f"{Path(path).name} has no action named exactly '{name}'"
    arm.animation_data.action = clip
    arm.animation_data.action_slot = clip.slots[0]
    for track in arm.animation_data.nla_tracks:
        track.mute = True

    meshes = [o for o in scene.objects
              if o.type == "MESH" and any(m.type == "ARMATURE" for m in o.modifiers)]
    assert meshes

    duration = spec["beats"][-1][0]
    assert abs(clip.frame_range[1] / 60 - duration) < 0.002, (
        name, clip.frame_range[1] / 60, duration)

    def frame(t):
        scene.frame_set(int(t * 60), subframe=t * 60 - int(t * 60))
        deps = bpy.context.evaluated_depsgraph_get()
        verts = []
        for o in meshes:
            ev = o.evaluated_get(deps)
            mesh = ev.to_mesh()
            verts.extend(ev.matrix_world @ v.co for v in mesh.vertices)
            ev.to_mesh_clear()
        bones = [b.matrix_channel.to_quaternion() for b in arm.pose.bones]
        return verts, bones

    rest_verts, rest_bones = frame(0.0)

    def apart(a, b):
        # ⚠️⚠️ `Quaternion.rotation_difference().angle` DOES NOT TAKE THE SHORTEST
        # PATH, and reading it raw produced a confident, wrong failure that cost a whole
        # diagnosis. It returns `2 * acos(w)` in [0, 2pi], so when the difference
        # quaternion lands on the far hemisphere a THREE degree change is reported as
        # 357. Supernova's arms pass -158 degrees overhead, which is exactly where that
        # happens, and the verifier duly announced a 21,429 deg/s frame in the middle of
        # the HANG, the quietest part of the clip. The exported samples were checked
        # pair by pair and hold no sign flip at all: the clip was always fine.
        return max(min(d, 2.0 * math.pi - d) for d in
                   (x.rotation_difference(y).angle for x, y in zip(a, b))) * DEG

    samples, previous = [], rest_bones
    for t in sorted({round(i / 60, 8) for i in range(int(duration * 60) + 1)}
                    | {b[0] for b in spec["beats"]}):
        if t > duration + 1e-6:
            continue
        verts, bones = frame(t)
        step = 0.0 if not samples else apart(previous, bones)
        previous = bones
        samples.append({"time": t,
                        "floor": round(min(v.z for v in verts), 5),
                        "top": round(max(v.z for v in verts), 5),
                        "forward": round(-min(v.y for v in verts), 5),
                        "swing": round(apart(rest_bones, bones), 3),
                        "step": round(step, 3)})

    by_time = {s["time"]: s for s in samples}
    punch = by_time[spec["punch"]]
    last = samples[-1]

    rates = [(b["time"], b["step"] / (b["time"] - a["time"]))
             for a, b in zip(samples, samples[1:]) if b["time"] > a["time"]]
    peak_t, peak = max(rates, key=lambda r: r[1])
    after = [r for t, r in rates if t > punch["time"]]

    assert abs(peak_t - punch["time"]) <= PEAK_WINDOW, (
        name, "the fastest frame is at", peak_t, "and the punch is at", punch["time"])
    assert after and after[0] <= peak * STOP_RATIO, (
        name, "it does not stop on the impact:", round(after[0], 1),
        "against a peak of", round(peak, 1))
    assert punch["swing"] > STRIKE_FLOOR, (name, punch["swing"])
    assert last["swing"] < REST_TOLERANCE, (
        name, "the last frame is", last["swing"], "degrees off rest")

    for t in spec["grounded"]:
        assert by_time[t]["floor"] > -0.005, (name, t, by_time[t]["floor"])

    return {"file": Path(path).name, "clip": name, "duration": duration,
            "punch": spec["punch"], "punch_swing_deg": punch["swing"],
            "peak_speed_deg_s": round(peak, 1), "peak_speed_at": peak_t,
            "speed_after_impact_deg_s": round(after[0], 1),
            "standing_height": round(by_time[0.0]["top"], 5),
            "top_range": [round(min(s["top"] for s in samples), 5),
                          round(max(s["top"] for s in samples), 5)],
            "airborne_lift": round(max(s["floor"] for s in samples), 5),
            "forward_range": [round(min(s["forward"] for s in samples), 5),
                              round(max(s["forward"] for s in samples), 5)],
            "samples": samples}


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--hero", required=True, choices=sorted(HEROES))
    ap.add_argument("--action")
    ap.add_argument("files", type=Path, nargs="+")
    args = ap.parse_args(sys.argv[sys.argv.index("--") + 1:]
                         if "--" in sys.argv else sys.argv[1:])

    actions = HEROES[args.hero]
    if args.action:
        actions = {args.action: actions[args.action]}

    for path in args.files:
        for name, spec in actions.items():
            print("HERO_VERIFIED " + json.dumps(measure(path, name, spec)))
