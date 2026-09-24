"""
Pose sheets of a hero's CAST clips as they ship in the glb (SKILL-FX-1).

    python tools/cast_sheet.py phaister hero-phaister-eclipse [--times a,b,c] [--out file.png]

Samples the authored glb action (the one `tools/author_hero_action.py` bakes) on the real skinned
model, from an opponent's three-quarter view and a side view. Silhouette and timing check only, not
the engine look; every frame says so.
"""
import argparse, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import intro_pose_preview as ipp

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

def sheet(hero, action, times=None, out=None, views=((2.4, 1.3, 4.6), (4.6, 1.1, .3))):
    model = ipp.Model(os.path.join(ROOT, "Assets/TumbangPreso/Art/characters/persons", f"team-{hero}.glb"))
    _, duration = model.action(action, 0)
    times = times or [round(duration * i / 7, 3) for i in range(8)]
    frames = []
    for view in views:
        for t in times:
            pose, _ = model.action(action, t)
            tris, cols = model.skinned(pose)
            tris = tris * 2.38
            img = ipp.render(tris, cols, np.array(view), np.array((0, .95, 0)), 42, size=(400, 300),
                             label=f"{action} t={t:.2f}/{duration:.2f}")
            frames.append(img)
    w, h = frames[0].size
    cols = len(times)
    out_img = Image.new("RGB", (w * cols, h * len(views)), (255, 255, 255))
    for i, f in enumerate(frames):
        out_img.paste(f, ((i % cols) * w, (i // cols) * h))
    out = out or os.path.join(ROOT, "docs/reports/skill-performances-2026-09-24/sheets", f"{action}.png")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    out_img.save(out)
    return out

if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("hero"); ap.add_argument("action")
    ap.add_argument("--times"); ap.add_argument("--out")
    a = ap.parse_args()
    print(sheet(a.hero, a.action, [float(x) for x in a.times.split(",")] if a.times else None, a.out))
