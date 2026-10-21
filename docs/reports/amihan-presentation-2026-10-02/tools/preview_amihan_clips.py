"""
Offline pose filmstrips of Amihan's LIVE cast clips, read straight from
Assets/TumbangPreso/Runtime/Visual/HeroAbilityClips.Amihan.cs.

Why: her live casts are keyed in C# (`PoseKey`), not in the introduction table, so
`tools/author_ultimate_intros.py --preview` cannot show them. This parses the same
source, converts each key to the raw Unity local eulers `PoseKey` writes, samples it
with the clip builder's punch and hold shape, and skins the real team-amihan.glb with
`tools/intro_pose_preview.py`. It is a silhouette and timing check from fixed witness
cameras, not the engine look and not acceptance.

    python docs/reports/amihan-presentation-2026-10-02/tools/preview_amihan_clips.py storm --out sheet.png
    python .../preview_amihan_clips.py dash --times 0,.09,.13,.2,.34,.48,.66 --view side
"""

import argparse
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "tools"))
SOURCE = os.path.join(ROOT, "Assets", "TumbangPreso", "Runtime", "Visual", "HeroAbilityClips.Amihan.cs")
MODEL = os.path.join(ROOT, "Assets", "TumbangPreso", "Art", "characters", "persons", "team-amihan.glb")
CONSTANTS = {"Core.AmihanRules.StormSurgeGatherSeconds": 1.5, "AmihanHoverSeconds": 3.4}
BONES = ("torso", "head", "arm-left", "arm-right", "leg-left", "leg-right")
# Witness cameras in her space (+z is her facing, +x her right), at the shipped person scale.
VIEWS = {
    "side": ((4.4, 1.3, 0.4), (0, 1.0, 0.4), 40),
    "front": ((2.4, 1.5, 4.6), (0, 1.0, 0), 40),
    "back": ((-1.8, 1.9, -4.4), (0, 0.9, 1.0), 44),
    "top": ((3.2, 5.0, -2.2), (0, 0.6, 0.8), 44),
}


def _number(text, names):
    text = text.strip().rstrip("f")
    if text in CONSTANTS:
        return CONSTANTS[text]
    if text in names:
        return names[text]
    return float(text.replace("f", ""))


def _vector(text, names):
    text = text.strip()
    if text in names:
        return names[text]
    m = re.fullmatch(r"V\(([^)]*)\)", text)
    if not m:
        raise ValueError("not a vector: " + text)
    return tuple(float(x.strip().rstrip("f")) for x in m.group(1).split(","))


def _split(args):
    out, depth, cur = [], 0, ""
    for ch in args:
        if ch == "(":
            depth += 1
        elif ch == ")":
            depth -= 1
        if ch == "," and depth == 0:
            out.append(cur)
            cur = ""
        else:
            cur += ch
    out.append(cur)
    return [a.strip() for a in out]


def parse(clip):
    """The keys, punches and hold of BuildAmihan<clip> as written in the source."""
    src = open(SOURCE, encoding="utf-8").read()
    m = re.search(r"private static AnimationClip BuildAmihan" + clip + r"\(.*?\n        }\n", src, re.S)
    if not m:
        raise SystemExit("no clip BuildAmihan" + clip)
    body = m.group(0)
    names, keys, punches, hold = {}, [], [], None
    for stmt in re.findall(r"[^;{}]+;", body):
        stmt = " ".join(stmt.split())
        for name, value in re.findall(r"var (\w+) = (V\([^)]*\))", stmt):
            names[name] = _vector(value, names)
        if stmt.startswith("PoseKey("):
            args = _split(stmt[len("PoseKey("):-2])
            t, y = _number(args[1], names), _number(args[2], names)
            v = [_vector(a, names) for a in args[3:]]
            while len(v) < 6:
                v.append((0.0, 0.0, 0.0))
            torso, head, left, right, ll, rl = v
            raw = {"torso": torso, "head": head,
                   "arm-left": (left[0], left[1], 80 - left[2]),
                   "arm-right": (right[0], right[1], -80 - right[2]),
                   "leg-left": (ll[0], ll[1], -ll[2]), "leg-right": (rl[0], rl[1], -rl[2])}
            keys.append((t, y, raw))
        elif stmt.startswith("b.PunchAt("):
            punches.append(_number(stmt[len("b.PunchAt("):-2], names))
        elif stmt.startswith("b.HoldAt("):
            a = _split(stmt[len("b.HoldAt("):-2])
            hold = (_number(a[0], names), _number(a[1], names))
    keys.sort(key=lambda k: k[0])
    if hold:
        at, seconds = hold
        held = [k for k in keys if k[0] < at - 1e-3]
        at_key = next(k for k in keys if abs(k[0] - at) < 1e-3)
        held += [at_key, (at + seconds, at_key[1], at_key[2])]
        held += [(k[0] + seconds, k[1], k[2]) for k in keys if k[0] > at + 1e-3]
        keys = held
    return keys, punches


def _track(points, t, punches):
    if t <= points[0][0]:
        return points[0][1]
    if t >= points[-1][0]:
        return points[-1][1]
    for (t0, v0), (t1, v1) in zip(points, points[1:]):
        if t0 <= t <= t1:
            u = (t - t0) / max(1e-6, t1 - t0)
            if any(abs(t1 - p) < 1e-3 for p in punches):
                u = u ** 2.1
            elif any(abs(t0 - p) < 1e-3 for p in punches):
                u = 1 - (1 - u) ** 1.6
            else:
                u = u * u * (3 - 2 * u)
            return v0 + (v1 - v0) * u
    return points[-1][1]


def sample(keys, punches, t):
    pose = {}
    for b in BONES:
        pose[b] = tuple(_track([(k[0], k[2][b][i]) for k in keys], t, punches) for i in range(3))
    lift = _track([(k[0], k[1]) for k in keys], t, punches)
    return pose, lift


def sheet(clip, times, views, out, label=""):
    import numpy as np
    from PIL import Image
    import intro_pose_preview as ipp
    keys, punches = parse(clip)
    model = ipp.Model(MODEL)
    scale = 2.38
    times = times or [k[0] for k in keys]
    frames = []
    floor0 = None
    for view in views:
        for t in times:
            raw, lift = sample(keys, punches, t)
            pose = {b: (ipp.quat_euler(*raw[b]), None) for b in BONES}
            tris, cols = model.skinned(pose)
            tris = tris * scale
            if floor0 is None:
                rest = {b: (ipp.quat_euler(*keys[0][2][b]), None) for b in BONES}
                floor0 = (model.skinned(rest)[0] * scale)[:, :, 1].min()
            tris[:, :, 1] += -floor0 + lift
            eye, look, fov = VIEWS[view]
            hit = " PUNCH" if any(abs(t - p) < 1e-3 for p in punches) else ""
            frames.append(ipp.render(tris, cols, np.array(eye), np.array(look), fov, size=(400, 300),
                                     label=f"{clip} {view} t={t:.2f}{hit} {label}"))
    cols_n = len(times)
    w, h = frames[0].size
    img = Image.new("RGB", (w * cols_n, h * len(views)), (255, 255, 255))
    for i, f in enumerate(frames):
        img.paste(f, ((i % cols_n) * w, (i // cols_n) * h))
    img.save(out)
    return out


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("clip", help="Dash, Updraft, Hover, Whirlwind or Storm")
    ap.add_argument("--times", default=None)
    ap.add_argument("--views", default="side,front")
    ap.add_argument("--out", required=True)
    ap.add_argument("--label", default="")
    a = ap.parse_args()
    times = [float(x) for x in a.times.split(",")] if a.times else None
    print(sheet(a.clip[0].upper() + a.clip[1:], times, a.views.split(","), a.out, a.label))
