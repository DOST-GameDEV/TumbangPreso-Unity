"""Stick-figure preview of the emote clips' key poses, before a Unity bake.

Reads the PoseKey tables in HeroAbilityClips.Emotes.cs and draws each key from the front and the side, for a
long-armed rig (EmoteFit short 0) and the shortest (short 1), using the rig's axes: Euler order ZXY, arms
T-posed along X, -X pitch forward, the elbow folding toward the arm's front. It is an approximation of the
seven-bone rig for checking silhouettes and twists; the native strips (EmoteMotionAuthor.WireAndReview) decide.

    python tools/emote_pose_preview.py [EmoteCheer EmoteLaugh ...]

Writes Logs/motion/emote_pose_<Clip>.png.
"""
import re, sys, numpy as np
from scipy.spatial.transform import Rotation as R
from PIL import Image, ImageDraw
import os
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
src = open(os.path.join(ROOT, 'Assets/TumbangPreso/Runtime/Visual/HeroAbilityClips.Emotes.cs'), encoding='utf-8').read()
OUT = os.path.join(ROOT, 'Logs', 'motion')
os.makedirs(OUT, exist_ok=True)

class Fit:
    def __init__(s, short): s.Short = short
    def Push(s, v, e): return v + e * s.Short

def V(x, y, z): return np.array([x, y, z], float)
Zero = V(0, 0, 0); EmoteRestLeft = V(0, 0, 14); EmoteRestRight = V(0, 0, -14)
def Fore(f, s=1.0): return f
def eul(v): return R.from_euler('zxy', [v[2], v[0], v[1]], degrees=True)

def pose(torso, head, left, right, legL=Zero, legR=Zero, foreL=0, foreR=0, arm=0.36):
    hip = V(0, 0.48, 0); pts = {}
    Rt = eul(torso)
    sh_y = 0.30
    pts['hip'] = hip; pts['neck'] = hip + Rt.apply(V(0, sh_y, 0))
    Rh = Rt * eul(head); pts['head'] = pts['neck'] + Rh.apply(V(0, 0.15, 0)); pts['nose'] = pts['head'] + Rh.apply(V(0, 0, 0.09))
    up, lo = arm * 0.5, arm * 0.5
    for side, a, f, sx in (('L', left, foreL, -1), ('R', right, foreR, 1)):
        e = V(a[0], a[1], (80 - a[2]) if side == 'L' else (-80 - a[2]))
        Ra = Rt * eul(e)
        sh = hip + Rt.apply(V(0.17 * sx, sh_y - 0.02, 0)); el = sh + Ra.apply(V(up * sx, 0, 0))
        Rf = Ra * eul(V(0, f * (1 if side == 'L' else -1), 0)); ha = el + Rf.apply(V(lo * sx, 0, 0))
        pts['sh' + side], pts['el' + side], pts['ha' + side] = sh, el, ha
    for side, l, sx in (('L', legL, -1), ('R', legR, 1)):
        Rl = eul(V(l[0], l[1], -l[2])); pts['hp' + side] = hip + V(0.08 * sx, 0, 0); pts['ft' + side] = pts['hp' + side] + Rl.apply(V(0, -0.48, 0))
    low = min(p[1] for k, p in pts.items())
    for k in pts: pts[k] = pts[k] - V(0, low, 0)
    return pts

def draw(d, pts, ox, oy, view, sc=150):
    def P(p):
        x = -p[0] if view == 'front' else p[2]
        return (ox + x * sc, oy - p[1] * sc)
    bones = [('hip', 'neck'), ('shL', 'elL'), ('elL', 'haL'), ('shR', 'elR'), ('elR', 'haR'), ('hpL', 'ftL'), ('hpR', 'ftR'), ('shL', 'shR'), ('hpL', 'hpR')]
    for a, b in bones:
        col = (255, 120, 60) if a.endswith('R') or b.endswith('R') else (240, 230, 200)
        d.line([P(pts[a]), P(pts[b])], fill=col, width=5)
    hx, hy = P(pts['head']); d.ellipse([hx - 14, hy - 14, hx + 14, hy + 14], outline=(240, 230, 200), width=4)
    d.line([P(pts['head']), P(pts['nose'])], fill=(80, 200, 255), width=4)
    d.line([(ox - 70, oy), (ox + 70, oy)], fill=(90, 90, 90), width=1)

def clip_keys(name, short):
    m = re.search(r'private static AnimationClip ' + name + r'\(.*?\n        \{(.*?)\n        \}', src, re.S)
    body = m.group(1); env = {'V': V, 'Fore': Fore, 'Zero': Zero, 'EmoteRestLeft': EmoteRestLeft, 'EmoteRestRight': EmoteRestRight, 'fit': Fit(short), 'DanceBeat': 0.48, 'Mathf': None}
    keys = []
    env['PoseKey'] = lambda b, t, y, *a: keys.append((t, a))
    class B:
        def PunchAt(s, t): pass
        def HoldAt(s, t, h): pass
        def Build(s): return None
    env['b'] = B()
    for line in body.split('\n'):
        line = line.strip()
        if not line or line.startswith('//') or line.startswith('var b ='): continue
        line = re.sub(r'(\d)f\b', r'\1', line).rstrip(';')
        line = re.sub(r'^(Vector3|float|var)\s+', '', line)
        if line.startswith('return'): continue
        # split "a = x, b = y" declarations
        parts = re.split(r',\s*(?=[A-Za-z_]\w*\s*=[^=])', line) if re.match(r'^[A-Za-z_]\w*\s*=[^=]', line) else [line]
        for p in parts: exec(p, env)
    return keys

names = sys.argv[1:] or ['EmoteCheer', 'EmoteNoWay', 'EmoteTooEasy', 'EmoteLaugh', 'EmoteDance', 'EmoteComeOn', 'EmoteGoodGame']
for name in names:
    rows = []
    for short, arm in ((0.0, 0.40), (1.0, 0.30)):
        keys = clip_keys(name, short); W = 170
        img = Image.new('RGB', (W * len(keys), 2 * 200 + 20), (30, 22, 16)); d = ImageDraw.Draw(img)
        for i, (t, a) in enumerate(keys):
            for j, view in enumerate(('front', 'side')):
                draw(d, pose(*a, arm=arm), i * W + W // 2, 180 + j * 200, view)
            d.text((i * W + 6, 4), f'{t:.2f}  {"short" if short else "long"}', fill=(255, 186, 0))
        rows.append(img)
    out = Image.new('RGB', (max(r.width for r in rows), sum(r.height for r in rows)), (0, 0, 0)); y = 0
    for r in rows: out.paste(r, (0, y)); y += r.height
    out.save(os.path.join(OUT, f'emote_pose_{name}.png')); print(name, len(keys))
