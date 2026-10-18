"""MAYARI, THE MOON GODDESS, AS A GHOST IN THE SKY (2026-10-19), the reward of Payo's moon easter egg (`Map.PayoMoon`).

The owner, of the first reward (a constellation of star beads and lines, `moon_cute.py` STARS): "you should model an
actual godess like the paete ult. then ghost effect her, place her in the sky, and make stars glow across her
sillhouete". So she is a real modelled figure here, in the spirit of Mariang Makiling in Paete's ultimate (a tall calm
woman in the cast's soft rounded style, drawn as one luminous hue, brighter at the edges, see-through in the middle, the
hem dissolving into mist; reference only, nothing of Paete's is used or changed), but MOON-themed:
  * a serene round face, eyes closed, a small smile, blush; one long lock of hair falls over her left eye (in the old
    story Mayari lost an eye, and the moon is the dimmer light for it; the art's variant A had her peeking);
  * very long hair streaming sideways in a night wind, five locks, each its own path;
  * a baro't saya: a fitted bodice, a folded panuelo over the shoulders, bell sleeves, a long saya flaring and trailing
    back into the mist, a sash at the waist;
  * a crescent diadem with a star, and a large crescent halo behind her head;
  * her arms forward, hands cupped either side of the space where the cute moon Buwan floats: she holds the moon.
Built with tools/hand_companion_kit.py (rounded forms, smooth normals), typed part by part.

THE GAME FILE `Resources/Map/PayoMoon/goddess.bytes` ("KNL1", the Kanlit format; Unity space, x flipped):
  A = (part, value 0..1 (how bright this cloth is in her one hue), sway weight 0..1, reveal order 0..1 (0 at her hands,
       1 at the furthest point: she forms outward from the moon)),
  B = (the sway pivot xyz (the hair's root), 0), normal = the surface's, uv = (her height 0 hem .. 1 crown, 0).
  Parts: 0 skin and bodice, 1 hair, 2 face ink (closed eyes, mouth), 3 gold (diadem, halo, earrings), 4 the saya and
  sleeves (they ripple), 5 the panuelo and sash (brightest cloth).
`PayoMayariGhost.shader` draws it. AUTHORING SPACE: metres-like units, her hem at y 0, crown about 4.3, +z toward the
court. HANDS = where the moon floats (the game puts this point on the moon).

  PY=/c/Users/StarX/AppData/Local/Python/pythoncore-3.14-64/python.exe (always -I)
  $PY -I tools/payo_pieces/mayari_goddess.py stage | unity      bake (stage: Logs/payo/pieces/moon/bake; unity: Resources)
  "$B" -b --python tools/payo_pieces/mayari_goddess.py -- render <ABS_ROOT> [stage]     review shots
  $PY -I tools/payo_pieces/mayari_goddess.py sheet vN           Logs/payo/unity/moon/goddess_vN.png
"""
import json
import math
import os
import struct
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
SHOTS = os.path.join(ROOT, "Logs", "payo", "pieces", "moon", "goddess_shots")
UNITY_OUT = os.path.join(ROOT, "Assets", "TumbangPreso", "Resources", "Map", "PayoMoon")
STAGE_OUT = os.path.join(ROOT, "Logs", "payo", "pieces", "moon", "bake")
SHEETS = os.path.join(ROOT, "Logs", "payo", "unity", "moon")
HANDS = (0.0, 2.05, 0.82)

try:
    import bpy
    INBL = True
except ImportError:
    INBL = False

if not INBL:
    import numpy as np
    sys.path.insert(0, os.path.join(ROOT, "tools"))
    import hand_companion_kit as kit
    from hand_companion_kit import ellipsoid, lathe, moved, tube, join

    SKIN, HAIR, INK, GOLD, SAYA, BRIGHT = 0, 1, 2, 3, 4, 5
    VALUE = {SKIN: .78, HAIR: .38, INK: .05, GOLD: 1.0, SAYA: .62, BRIGHT: .95}

    def smooth_normals(pos, tris):
        n = np.zeros_like(pos)
        a, b, c = pos[tris[:, 0]], pos[tris[:, 1]], pos[tris[:, 2]]
        fn = np.cross(b - a, c - a)
        for k in range(3):
            np.add.at(n, tris[:, k], fn)
        n /= np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-9)
        return n.astype(np.float32)

    HEAD_C = np.array((0.0, 3.12, 0.04), np.float32)
    HEAD_R = np.array((.38, .42, .36), np.float32)

    def on_head(yaw, pitch, lift=0.0):
        y, p = math.radians(yaw), math.radians(pitch)
        d = np.array((math.sin(y) * math.cos(p), math.sin(p), math.cos(y) * math.cos(p)), np.float32)
        n = d / HEAD_R
        n /= np.linalg.norm(n)
        return HEAD_C + HEAD_R * d + n * lift, n

    def face_stroke(pts, r, yaw, pitch):
        """A line laid on the face at (yaw, pitch): built flat in xy, turned to the surface there."""
        at, n = on_head(yaw, pitch, r * .5)
        x = np.cross(np.array((0, 1, 0), np.float32), n); x /= np.linalg.norm(x)
        y = np.cross(n, x)
        M = np.stack([x, y, n], 1)
        p, nr, t = tube(pts, r, seg=6)
        q = p.copy()
        q[:, 2] -= (q[:, 0] ** 2 + q[:, 1] ** 2) / (2 * float(HEAD_R.mean()))
        return (q @ M.T + at).astype(np.float32), (nr @ M.T).astype(np.float32), t

    def parts():
        """(name, part id, mesh, sway pivot or None, sway weight per vertex or a number)."""
        out = []
        # ---- THE SAYA: flaring from the waist to a wide hem, trailing back as she floats, the hem in soft waves
        prof = [(1.08, 0.0), (1.0, .3), (.86, .7), (.68, 1.1), (.5, 1.45), (.36, 1.72), (.3, 1.82)]
        fine = []
        for (r0, y0), (r1, y1) in zip(prof, prof[1:]):
            for k in range(4):
                t = k / 4
                fine.append((r0 + (r1 - r0) * t, y0 + (y1 - y0) * t))
        fine.append(prof[-1])
        saya = lathe(fine, seg=40, squash_z=.82)
        p = saya[0].copy()
        ang = np.arctan2(p[:, 2], p[:, 0])
        low = np.clip(1 - p[:, 1] / 1.2, 0, 1)
        rad = np.sqrt(p[:, 0] ** 2 + p[:, 2] ** 2)
        wave = 1 + .07 * np.sin(ang * 7 + .6) * low ** 1.5
        p[:, 0] *= wave; p[:, 2] *= wave
        p[:, 2] -= .32 * low ** 2            # it trails back
        p[:, 1] += .05 * np.sin(ang * 5) * low ** 2
        saya = (p, smooth_normals(p, saya[2]), saya[2])
        out.append(("saya", SAYA, saya, (0, 1.82, 0), np.clip(1 - p[:, 1] / 1.82, 0, 1) ** 1.5 * .8))
        # ---- THE SASH at the waist, and its two tails down the front of the saya
        sash = tube([(.33 * math.cos(a), 1.8, .27 * math.sin(a)) for a in np.linspace(0, 2 * math.pi, 30)], .045, seg=8)
        out.append(("sash", BRIGHT, sash, (0, 1.8, .3), .1))
        # ---- THE BODICE: fitted from the waist up to the neck
        bod = lathe([(.3, 1.78), (.32, 1.95), (.34, 2.12), (.33, 2.28), (.27, 2.44), (.16, 2.58), (.0, 2.62)], seg=28, squash_z=.76)
        out.append(("bodice", SKIN, bod, None, 0.0))
        # ---- THE PANUELO: a folded kerchief over her shoulders, crossing to a point at her breast
        pan = ellipsoid((.5, .1, .27), at=(0, 2.47, -.01), seg=24, rings=10)
        pt = lathe([(.16, 0.0), (.1, -.14), (.0, -.26)], seg=12, at=(0, 2.44, .21), squash_z=.35)
        out.append(("panuelo", BRIGHT, join(pan, pt), None, 0.0))
        # ---- THE NECK and HEAD
        neck = tube([(0, 2.55, .0), (0, 2.78, .02)], .085, seg=12)
        head = ellipsoid(tuple(HEAD_R), at=tuple(HEAD_C), seg=28, rings=18)
        out.append(("head", SKIN, join(neck, head), None, 0.0))
        # ---- THE ARMS, forward, bell sleeves, hands cupped either side of the moon
        for side in (-1, 1):
            sh = np.array((side * .42, 2.42, .0), np.float32)
            el = np.array((side * .62, 2.0, .32), np.float32)
            wr = np.array((side * .52, 2.02, .72), np.float32)
            sleeve = tube([tuple(sh), tuple(sh * .5 + el * .5), tuple(el)], [.13, .16, .22], seg=14)
            fore = tube([tuple(el), tuple(wr)], [.07, .06], seg=10)
            hand = moved(ellipsoid((.075, .11, .13), seg=12, rings=8), turn=(20, 0, side * 25), at=tuple(wr + (side * -.03, .02, .1)))
            thumb = moved(ellipsoid((.03, .06, .03), seg=8, rings=6), turn=(0, 0, side * 50), at=tuple(wr + (side * -.08, .1, .1)))
            out.append(("sleeve_%d" % side, SAYA, sleeve, tuple(sh), .12))
            out.append(("arm_%d" % side, SKIN, join(fore, hand, thumb), None, 0.0))
        # ---- THE HAIR: a cap over the crown and back of the head, bangs, and five long locks streaming to her left
        cap = ellipsoid((.405, .45, .37), at=(0, 3.17, -.07), seg=28, rings=18)
        out.append(("cap", HAIR, cap, None, 0.0))
        bangs = []
        for yaw, pitch, w in ((-46, 40, .11), (-22, 47, .12), (2, 49, .12), (26, 46, .11), (48, 38, .1)):
            at, n = on_head(yaw, pitch, -.01)
            b = ellipsoid((w, w * .75, w * .4), seg=12, rings=8)
            x = np.cross(np.array((0, 1, 0), np.float32), n); x /= np.linalg.norm(x)
            yv = np.cross(n, x)
            M = np.stack([x, yv, n], 1)
            bangs.append(((b[0] @ M.T + at).astype(np.float32), (b[1] @ M.T).astype(np.float32), b[2]))
        out.append(("bangs", HAIR, join(*bangs), None, 0.0))
        # the lost-eye lock: from the parting over her LEFT eye (the figure's +x) down past her cheek to her chin
        lost = [(.04, 3.5, .31), (.13, 3.32, .37), (.2, 3.12, .35), (.24, 2.92, .29), (.24, 2.72, .2)]
        out.append(("lost_eye_lock", HAIR, tube(lost, [.05, .075, .07, .055, .035], seg=12), (.04, 3.5, .31), .25))
        # five long locks from the back of the head, each typed, streaming down and out to her left in the wind
        # v2: the locks are flattened wavy ribbons that fall from the back of her head behind her shoulders and down her
        # back, drifting to her left in the wind and parting into tips (v1's were straight round sticks, a spider).
        # Each: (root, typed (dx, dy) steps, its wave (amplitude, waves along it, phase), its widest width, its depth).
        LOCKS = [
            ((.18, 3.34, -.26), [(.18, -.35), (.22, -.45), (.25, -.5), (.28, -.5), (.3, -.45), (.32, -.4)], (.09, 2.0, 0.0), .2, -.34),
            ((.0, 3.3, -.34), [(.1, -.4), (.15, -.5), (.2, -.55), (.24, -.55), (.26, -.5), (.28, -.45)], (.08, 2.3, 1.2), .22, -.44),
            ((-.2, 3.24, -.28), [(.02, -.4), (.08, -.5), (.12, -.55), (.16, -.55), (.18, -.5), (.2, -.4)], (.07, 2.1, 2.4), .2, -.42),
            ((.32, 3.4, -.14), [(.28, -.22), (.3, -.32), (.3, -.4), (.3, -.42), (.28, -.4)], (.1, 1.7, .6), .16, -.26),
            ((-.34, 3.12, -.12), [(-.06, -.35), (-.04, -.45), (.0, -.45), (.04, -.4)], (.05, 1.6, 1.8), .13, -.16),
        ]
        for k, (root, steps, (amp, waves, phase), widest, depth) in enumerate(LOCKS):
            ctrl = [np.array(root, np.float32)]
            for dx, dy in steps:
                ctrl.append(ctrl[-1] + np.array((dx, dy, 0), np.float32))
            ctrl = np.array(ctrl)
            ctrl[1:, 2] = depth
            n = 26
            path, width = [], []
            seglen = len(ctrl) - 1
            for j in range(n + 1):
                t = j / n
                f = t * seglen
                i = min(int(f), seglen - 1)
                u = f - i
                c = ctrl[i] * (1 - u) + ctrl[i + 1] * u
                c = c + np.array((amp * math.sin(t * waves * 2 * math.pi + phase) * t, 0, 0), np.float32)
                path.append(tuple(c.tolist()))
                width.append(.03 + (widest - .03) * math.sin(math.pi * min(1.0, t * 1.15 + .15)) ** .7)
            m = tube(path, width, seg=12)
            q = m[0].copy()
            # flatten into a ribbon: squash each ring's depth about its own centre
            centres = np.repeat(np.array([path[0]] * 2 + path + [path[-1]] * 2, np.float32), 13, axis=0)[:len(q)]
            q[:, 2] = centres[:, 2] + (q[:, 2] - centres[:, 2]) * .42
            m = (q, smooth_normals(q, m[2]), m[2])
            rootv = np.array(root, np.float32)
            d = np.linalg.norm(q - rootv, axis=1)
            out.append(("lock_%d" % k, HAIR, m, tuple(rootv), np.clip(d / 2.2, 0, 1)))
        # ---- THE FACE: closed eyes (the right one shown; the left under its lock), a small smile, blush
        eye = [(-.1 + .2 * t, -.045 * math.sin(math.pi * t), 0) for t in np.linspace(0, 1, 11)]
        lashes = [face_stroke([(x, y, 0), (x * 1.15, y - .045, 0)], .011, -22, 2) for x, y in ((-.065, -.03), (0, -.045), (.065, -.03))]
        mouth = [(-.04 + .08 * t, -.016 * math.sin(math.pi * t), 0) for t in np.linspace(0, 1, 7)]
        ink = join(face_stroke(eye, .016, -22, 2), face_stroke(mouth, .011, 0, -26), *lashes)
        out.append(("face", INK, ink, None, 0.0))
        at, n = on_head(-36, -14, -.005)
        blush = moved(ellipsoid((.07, .04, .012), seg=12, rings=6), at=tuple(at))
        out.append(("blush", BRIGHT, blush, None, 0.0))
        # ---- GOLD: the crescent diadem with a star, earrings, and the great crescent halo behind her head
        dia = []
        for k in range(17):
            a = math.radians(200 + 140 * k / 16)
            dia.append((.14 * math.cos(a), 3.6 + .14 * math.sin(a) + .14, .3))
        diadem = tube(dia, [.012 + .035 * math.sin(math.pi * k / 16) for k in range(17)], seg=8)
        so = [((.07 if j % 2 == 0 else .03) * math.sin(math.radians(36 * j)), (.07 if j % 2 == 0 else .03) * math.cos(math.radians(36 * j))) for j in range(10)][::-1]
        star = moved(kit.slab(so, .03), at=(0, 3.66, .33))
        ear = moved(lathe([(.0, 0.0), (.03, .03), (.025, .07), (.0, .1)], seg=8), at=(-.39, 2.88, .02))
        halo = []
        # v3: big enough to show round her hair (seen in Play: a smaller one hid behind her head but for one horn), over
        # her head from one side to the other, its horns down by her shoulders
        for k in range(33):
            a = math.radians(-20 + 220 * k / 32)
            halo.append((1.02 * math.cos(a), 3.2 + 1.02 * math.sin(a), -.62))
        halo_m = tube(halo, [.01 + .07 * math.sin(math.pi * k / 32) ** 1.2 for k in range(33)], seg=10)
        out.append(("gold", GOLD, join(diadem, star, ear, halo_m), None, 0.0))
        return out

    def write_knl(path, P, N, UV, A, Bv, T):
        P = np.asarray(P, np.float32).copy(); N = np.asarray(N, np.float32).copy(); Bv = np.asarray(Bv, np.float32).copy()
        P[:, 0] *= -1; N[:, 0] *= -1; Bv[:, 0] *= -1
        T = np.asarray(T, np.int32).reshape(-1, 3)[:, ::-1].reshape(-1)
        with open(path, "wb") as f:
            f.write(b"KNL1" + struct.pack("<ii", len(P), len(T)))
            for arr in (P, N, np.asarray(UV, np.float32), np.asarray(A, np.float32), Bv):
                f.write(np.ascontiguousarray(arr, np.float32).tobytes())
            f.write(np.ascontiguousarray(T, np.int32).tobytes())
        print("wrote %s: %d vertices, %d triangles" % (os.path.relpath(path, ROOT), len(P), len(T) // 3))
        return len(T) // 3

    def bake(out_dir):
        os.makedirs(out_dir, exist_ok=True)
        P, N, UV, A, Bv, T = [], [], [], [], [], []
        base = 0
        hands = np.array(HANDS, np.float32)
        everything = parts()
        far = max(float(np.linalg.norm(m[0] - hands, axis=1).max()) for _, _, m, _, _ in everything)
        for name, part, mesh, pivot, sway in everything:
            p, n, t = mesh
            k = len(p)
            order = np.linalg.norm(p - hands, axis=1) / far
            sw = np.full(k, float(sway), np.float32) if np.isscalar(sway) else np.asarray(sway, np.float32)
            a = np.stack([np.full(k, float(part)), np.full(k, VALUE[part]), sw, order], 1)
            pv = np.array(pivot if pivot is not None else (0, 0, 0), np.float32)
            b = np.concatenate([np.tile(pv, (k, 1)), np.zeros((k, 1), np.float32)], 1)
            uv = np.stack([np.clip(p[:, 1] / 4.3, 0, 1), np.zeros(k)], 1)
            P.append(p); N.append(n); UV.append(uv); A.append(a); Bv.append(b); T.append(t + base)
            base += k
        tris = write_knl(os.path.join(out_dir, "goddess.bytes"), np.concatenate(P), np.concatenate(N), np.concatenate(UV),
                         np.concatenate(A), np.concatenate(Bv), np.concatenate(T))
        print("GODDESS", tris, "triangles; hands", HANDS)

    def sheet(version):
        from PIL import Image, ImageDraw
        names = ["solid_front", "solid_quarter", "solid_side", "solid_back", "ghost_front", "ghost_quarter", "with_moon"]
        tiles = [(n, os.path.join(SHOTS, n + ".png")) for n in names if os.path.exists(os.path.join(SHOTS, n + ".png"))]
        ims = [Image.open(p).convert("RGB") for _, p in tiles]
        w, h = ims[0].size
        cols = 4
        rows = (len(ims) + cols - 1) // cols
        out = Image.new("RGB", (w * cols, (h + 20) * rows), (12, 10, 24))
        d = ImageDraw.Draw(out)
        for k, (im, (n, _)) in enumerate(zip(ims, tiles)):
            x, y = (k % cols) * w, (k // cols) * (h + 20)
            out.paste(im, (x, y + 20)); d.text((x + 6, y + 4), "MAYARI goddess %s  %s" % (version, n), fill=(255, 255, 255))
        os.makedirs(SHEETS, exist_ok=True)
        path = os.path.join(SHEETS, "goddess_%s.png" % version)
        out.save(path); print("wrote", path)

    if __name__ == "__main__":
        cmd = sys.argv[1] if len(sys.argv) > 1 else "stage"
        if cmd == "stage":
            bake(STAGE_OUT)
        elif cmd == "unity":
            bake(UNITY_OUT)
        elif cmd == "sheet":
            sheet(sys.argv[2] if len(sys.argv) > 2 else "v1")

else:
    import numpy as np
    from mathutils import Vector

    def read_knl(path):
        data = open(path, "rb").read()
        v, i = struct.unpack("<ii", data[4:12])
        at = 12

        def take(n):
            nonlocal at
            arr = np.frombuffer(data, np.float32, n, at); at += n * 4
            return arr
        P = take(v * 3).reshape(-1, 3); N = take(v * 3).reshape(-1, 3); UV = take(v * 2).reshape(-1, 2)
        A = take(v * 4).reshape(-1, 4); Bv = take(v * 4).reshape(-1, 4)
        T = np.frombuffer(data, np.int32, i, at).reshape(-1, 3)
        return P, N, UV, A, Bv, T

    def tb(p):
        return (float(p[0]), float(p[2]), float(p[1]))

    def mesh_obj(name, P, T, mask, mats, face_mat):
        me = bpy.data.meshes.new(name)
        me.from_pydata([tb(p) for p in P], [], [(int(t[0]), int(t[2]), int(t[1])) for t, k in zip(T, mask) if k])
        for m in mats:
            me.materials.append(m)
        for poly, k in zip(me.polygons, [f for f, k in zip(face_mat, mask) if k]):
            poly.material_index = int(k)
            poly.use_smooth = True
        o = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(o)
        return o

    def render():
        argv = sys.argv[sys.argv.index("--") + 1:]
        root = argv[1] if len(argv) > 1 else ROOT
        stage = "stage" in argv
        res = os.path.join(root, "Logs", "payo", "pieces", "moon", "bake") if stage else os.path.join(root, "Assets", "TumbangPreso", "Resources", "Map", "PayoMoon")
        shots = os.path.join(root, "Logs", "payo", "pieces", "moon", "goddess_shots")
        os.makedirs(shots, exist_ok=True)
        bpy.ops.wm.read_factory_settings(use_empty=True)
        sc = bpy.context.scene
        engines = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
        sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in engines else "BLENDER_EEVEE"
        sc.view_settings.view_transform = "Standard"
        w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
        bg = w.node_tree.nodes["Background"]
        bg.inputs[0].default_value = (0.03, 0.035, 0.1, 1); bg.inputs[1].default_value = 1.0
        s = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(s)
        s.data.energy = 3.0; s.rotation_euler = Vector((.4, .8, -.5)).normalized().to_track_quat("-Z", "Y").to_euler()
        cam = bpy.data.objects.new("c", bpy.data.cameras.new("c")); sc.collection.objects.link(cam); sc.camera = cam

        def aim(eye, look, fov):
            cam.data.sensor_fit = "VERTICAL"; cam.data.angle_y = math.radians(fov)
            cam.location = Vector(tb(eye))
            cam.rotation_euler = (Vector(tb(look)) - Vector(tb(eye))).to_track_quat("-Z", "Y").to_euler()

        def shoot(name, res_xy=(640, 800)):
            sc.render.resolution_x, sc.render.resolution_y = res_xy
            sc.render.filepath = os.path.join(shots, name + ".png")
            bpy.ops.render.render(write_still=True)
            print("SHOT", name)

        P, N, UV, A, Bv, T = read_knl(os.path.join(res, "goddess.bytes"))
        first = T[:, 0]
        part = A[first, 0].astype(int)
        # SOLID: her one hue by value per part, for the shapes
        hue = (0.86, 0.82, 1.0)

        def solid(name, value):
            m = bpy.data.materials.new(name); m.use_nodes = True
            b = m.node_tree.nodes["Principled BSDF"]
            c = tuple(min(1.0, hue[i] * (0.25 + .75 * value)) for i in range(3))
            b.inputs["Base Color"].default_value = (*c, 1); b.inputs["Roughness"].default_value = .6
            m.use_backface_culling = True
            return m
        vals = {0: .78, 1: .38, 2: .05, 3: 1.0, 4: .62, 5: .95}
        solids = [solid("v%d" % k, vals[k]) for k in range(6)]
        sm = mesh_obj("goddess", P, T, np.ones(len(T), bool), solids, part)
        for n, (eye, fov) in {"solid_front": ((0, 2.2, 9.5), 30), "solid_quarter": ((6.5, 2.6, 6.8), 30),
                              "solid_side": ((9.5, 2.2, 0), 30), "solid_back": ((-1.5, 2.6, -9.5), 30)}.items():
            aim(eye, (0.4, 2.15, 0), fov); shoot(n)
        # GHOST: see-through in the middle, glowing at the edges, the hem fading: an approximation of the game's shader
        g = bpy.data.materials.new("ghost"); g.use_nodes = True
        nt = g.node_tree; nt.nodes.clear()
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        lw = nt.nodes.new("ShaderNodeLayerWeight"); lw.inputs["Blend"].default_value = .55
        em = nt.nodes.new("ShaderNodeEmission"); em.inputs["Color"].default_value = (0.95, 0.9, 1.0, 1)
        tr = nt.nodes.new("ShaderNodeBsdfTransparent")
        add = nt.nodes.new("ShaderNodeAddShader")
        mix = nt.nodes.new("ShaderNodeMixShader")
        mul = nt.nodes.new("ShaderNodeMath"); mul.operation = "MULTIPLY"; mul.inputs[1].default_value = 3.0
        nt.links.new(lw.outputs["Facing"], mul.inputs[0])
        nt.links.new(mul.outputs[0], em.inputs["Strength"])
        em2 = nt.nodes.new("ShaderNodeEmission"); em2.inputs["Color"].default_value = (0.55, 0.5, 0.95, 1); em2.inputs["Strength"].default_value = .35
        nt.links.new(em.outputs[0], add.inputs[0]); nt.links.new(em2.outputs[0], add.inputs[1])
        mix.inputs["Fac"].default_value = .45
        nt.links.new(tr.outputs[0], mix.inputs[1]); nt.links.new(add.outputs[0], mix.inputs[2])
        nt.links.new(mix.outputs[0], out.inputs["Surface"])
        try:
            g.surface_render_method = "BLENDED"
        except Exception:
            g.blend_method = "BLEND"
        sm.data.materials.clear(); sm.data.materials.append(g)
        for poly in sm.data.polygons:
            poly.material_index = 0
        aim((0, 2.2, 9.5), (0.4, 2.15, 0), 30); shoot("ghost_front")
        aim((6.5, 2.6, 6.8), (0.4, 2.15, 0), 30); shoot("ghost_quarter")
        # WITH THE MOON: the crescent (moon.bytes) in her hands at the game's scale (the moon's radius 1 is 1/1.8 of hers)
        mp = os.path.join(res, "moon.bytes")
        if os.path.exists(mp):
            Pm, Nm, UVm, Am, Bm, Tm = read_knl(mp)
            keep = (Am[Tm[:, 0], 0] < 10) | (Am[Tm[:, 0], 0] == 15)
            moon = bpy.data.materials.new("moon"); moon.use_nodes = True
            mb = moon.node_tree.nodes["Principled BSDF"]
            mb.inputs["Base Color"].default_value = (1.0, .82, .3, 1); mb.inputs["Emission Color"].default_value = (1.0, .82, .3, 1)
            mb.inputs["Emission Strength"].default_value = 1.0
            mo = mesh_obj("moon", Pm, Tm, keep, [moon], np.zeros(len(Tm), int))
            k = 1.0 / 1.8
            mo.scale = (k, k, k)
            mo.location = Vector(tb((-HANDS[0], HANDS[1], HANDS[2])))
            aim((0, 1.6, 11.5), (0.3, 2.2, 0), 34); shoot("with_moon")
        print("RENDERED", shots)

    render()
