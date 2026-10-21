"""Makes Kuro cuter and more expressive, in place, on pet-nemu-ghost.glb.

Owner, 2026-09-24: "can u make kuro's expressions cuter both ingame and in the animation", then
"make it more expressive hehe".

What was wrong, measured on the board (ArtSource/home-scene, PhaisterBoard frame 2):
  * His blush sat in palette slot 10, the same violet family as his eyes (slot 2), so at a glance he
    read as a face with FOUR eyes. It moves to slot 13, the warm peach of Nemu's own palette
    (person_team-nemu.tres), which is the only warm colour his owner's palette has and reads as a
    blush on a dark violet body. Smaller, and a touch further out and down, where cheeks are.
  * His resting mouth was a square as big as a pupil. It is smaller and flatter now.
  * Three authored faces (cat, dizzy, shy) for eleven idle gestures, so most of his personality was
    a squash of the same two eyes. Seven new parts give five more gestures a face of their own.

⚠️ THE NEW PARTS ARE AUTHORED GEOMETRY UNDER `KuroExpressions`, EXACTLY LIKE THE THREE THAT
EXIST (tools/author_kuro.py): flat extruded silhouettes, baked into the same space as the cross eyes
and the cat mouth, carrying the same node translations restore_backup_characters.py gave those, so
they land on the restored face. `GhostPetCompanion.PoseIdleExpression` shows one set at a time and
collapses the rest, and `KuroFormTests` asserts every set. Nothing here touches the rage form, the rig
or any animation; both are digested before and after.

Idempotent: a second run finds the marker in `extras.cuteKuro` and does nothing.
Usage: python tools/cute_kuro.py
"""
import hashlib
import math
from pathlib import Path

import numpy as np

from author_cast_finish import add_accessor, animation_digest, write
from glb_mesh_dump import read_accessor, read_glb

ROOT = Path(__file__).resolve().parents[1]
GHOST = ROOT / "Assets/TumbangPreso/Art/characters/pets/pet-nemu-ghost.glb"
MARK = "2026-09-24 cuter and more expressive"

# The face, read off the glb rather than guessed: the cross eyes' and the cat mouth's baked centres,
# and the node offsets that put them on the restored face.
EYE_L = (-0.019, 0.029)
EYE_R = (0.019, 0.029)
MOUTH = (0.0, -0.008)
EYE_T = [0, -0.024, 0.0062]
MOUTH_T = [0, -0.006, 0.0062]
Z_BACK, Z_FRONT = 0.0384, 0.0396


def stroke(points, width):
    path = [np.array(p, float) for p in points]
    a, b = [], []
    for i, p in enumerate(path):
        t = path[min(i + 1, len(path) - 1)] - path[max(0, i - 1)]
        t /= np.linalg.norm(t)
        n = np.array([-t[1], t[0]]) * width * 0.5
        a.append(tuple(p + n))
        b.append(tuple(p - n))
    return a + list(reversed(b))


def oval(rx=0.5, ry=0.5, n=18):
    return [(math.cos(i * 2 * math.pi / n) * rx, math.sin(i * 2 * math.pi / n) * ry) for i in range(n)]


def heart(n=28):
    pts = []
    for i in range(n):
        t = i * 2 * math.pi / n
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((x / 34, (y + 2.5) / 34))
    return pts


def star(n=4, r0=0.5, r1=0.13):
    return [((r0 if i % 2 == 0 else r1) * math.cos(math.pi / 2 + i * math.pi / n), (r0 if i % 2 == 0 else r1) * math.sin(math.pi / 2 + i * math.pi / n)) for i in range(2 * n)]


def area(poly):
    return 0.5 * sum(poly[i][0] * poly[(i + 1) % len(poly)][1] - poly[(i + 1) % len(poly)][0] * poly[i][1] for i in range(len(poly)))


def ear_clip(poly):
    """Triangles of a simple polygon, counter-clockwise."""
    pts = list(poly)
    if area(pts) < 0:
        pts.reverse()
    idx = list(range(len(pts)))
    tris = []

    def inside(p, a, b, c):
        d = lambda u, v, w: (u[0] - w[0]) * (v[1] - w[1]) - (v[0] - w[0]) * (u[1] - w[1])
        s1, s2, s3 = d(p, a, b), d(p, b, c), d(p, c, a)
        return not ((s1 < 0 or s2 < 0 or s3 < 0) and (s1 > 0 or s2 > 0 or s3 > 0))

    guard = 0
    while len(idx) > 3 and guard < 10000:
        guard += 1
        for k in range(len(idx)):
            i0, i1, i2 = idx[k - 1], idx[k], idx[(k + 1) % len(idx)]
            a, b, c = pts[i0], pts[i1], pts[i2]
            if (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]) <= 1e-12:
                continue
            if any(inside(pts[j], a, b, c) for j in idx if j not in (i0, i1, i2)):
                continue
            tris.append((i0, i1, i2))
            idx.pop(k)
            break
    tris.append(tuple(idx))
    return pts, tris


def prism(poly, centre, scale, z0=Z_BACK, z1=Z_FRONT):
    """A flat extruded silhouette with one normal per face, baked at `centre` in face space."""
    pts, tris = ear_clip(poly)
    world = [(centre[0] + x * scale[0], centre[1] + y * scale[1]) for x, y in pts]
    pos, nrm, ind = [], [], []

    def add(p, n):
        pos.append(p)
        nrm.append(n)
        return len(pos) - 1

    front = [add((x, y, z1), (0, 0, 1)) for x, y in world]
    back = [add((x, y, z0), (0, 0, -1)) for x, y in world]
    for a, b, c in tris:
        ind += [front[a], front[b], front[c], back[a], back[c], back[b]]
    for i in range(len(world)):
        j = (i + 1) % len(world)
        (x0, y0), (x1, y1) = world[i], world[j]
        n = np.array([y1 - y0, -(x1 - x0), 0.0])
        n = tuple((n / (np.linalg.norm(n) or 1)).tolist())
        q = [add((x0, y0, z0), n), add((x1, y1, z0), n), add((x1, y1, z1), n), add((x0, y0, z1), n)]
        ind += [q[0], q[1], q[2], q[0], q[2], q[3]]
    return pos, nrm, ind


def ring(centre, scale, inner=0.62, n=20):
    """An "o": the space between two ovals, front and back faces plus both walls."""
    pos, nrm, ind = [], [], []

    def add(p, nn):
        pos.append(p)
        nrm.append(nn)
        return len(pos) - 1

    out = [(centre[0] + math.cos(i * 2 * math.pi / n) * 0.5 * scale[0], centre[1] + math.sin(i * 2 * math.pi / n) * 0.5 * scale[1]) for i in range(n)]
    inn = [(centre[0] + math.cos(i * 2 * math.pi / n) * 0.5 * inner * scale[0], centre[1] + math.sin(i * 2 * math.pi / n) * 0.5 * inner * scale[1]) for i in range(n)]
    for z, nz, flip in ((Z_FRONT, 1, False), (Z_BACK, -1, True)):
        o = [add((x, y, z), (0, 0, nz)) for x, y in out]
        m = [add((x, y, z), (0, 0, nz)) for x, y in inn]
        for i in range(n):
            j = (i + 1) % n
            t = [o[i], o[j], m[j], o[i], m[j], m[i]]
            ind += [t[0], t[2], t[1], t[3], t[5], t[4]] if flip else t
    return pos, nrm, ind


def main():
    g, blob = read_glb(GHOST)
    if g.get("extras", {}).get("cuteKuro") == MARK:
        print("Kuro is already cute:", MARK)
        return
    data = bytearray(blob)
    nodes = {n.get("name"): i for i, n in enumerate(g["nodes"])}
    rage_before = hashlib.sha256(repr([n for n in g["nodes"] if "Rage" in n.get("name", "")]).encode()).hexdigest()
    anim_before = animation_digest(g, data)

    def prim(name):
        return g["meshes"][g["nodes"][nodes[name]]["mesh"]]["primitives"][0]

    # 1 · The blush: peach (slot 13: atlas column 10, row 13), smaller, out and down to the cheeks.
    for side, sx in (("l", -1), ("r", 1)):
        p = prim("ghost-blush-" + side)
        uv = read_accessor(g, data, p["attributes"]["TEXCOORD_0"])
        p["attributes"]["TEXCOORD_0"] = add_accessor(g, data, [(10.5 / 16, 13.5 / 16)] * len(uv), "VEC2", 5126, "f")
        verts = np.array(read_accessor(g, data, p["attributes"]["POSITION"])) * np.array([0.72, 0.62, 1.0])
        p["attributes"]["POSITION"] = add_accessor(g, data, verts.tolist(), "VEC3", 5126, "f")
        t = g["nodes"][nodes["ghost-blush-" + side]]["translation"]
        g["nodes"][nodes["ghost-blush-" + side]]["translation"] = [t[0] + sx * 0.0025, t[1] - 0.0015, t[2]]

    # 2 · The resting mouth: a small flat dash instead of a pupil-sized square.
    p = prim("ghost-mouth-dot")
    verts = np.array(read_accessor(g, data, p["attributes"]["POSITION"])) * np.array([0.8, 0.5, 1.0])
    p["attributes"]["POSITION"] = add_accessor(g, data, verts.tolist(), "VEC3", 5126, "f")

    # 3 · The new faces.
    def material(name, rgb):
        g["materials"].append({"name": name, "pbrMetallicRoughness": {"baseColorFactor": [*rgb, 1], "metallicFactor": 0, "roughnessFactor": 1}})
        return len(g["materials"]) - 1

    spirit = prim("KuroCrossLeft")["material"]
    sparkle = material("Kuro sparkle", (1.0, 0.92, 1.0))
    heart_mat = material("Kuro heart", (1.0, 0.18, 0.5))
    tongue_mat = material("Kuro tongue", (1.0, 0.38, 0.55))
    group = nodes["KuroExpressions"]

    def part(name, mesh, mat, translation):
        pos, nrm, ind = mesh
        a = {"POSITION": add_accessor(g, data, pos, "VEC3", 5126, "f"), "NORMAL": add_accessor(g, data, nrm, "VEC3", 5126, "f")}
        idx = add_accessor(g, data, [(i,) for i in ind], "SCALAR", 5125, "I")
        g["meshes"].append({"name": name, "primitives": [{"attributes": a, "indices": idx, "material": mat, "mode": 4}]})
        g["nodes"].append({"name": name, "mesh": len(g["meshes"]) - 1, "translation": translation})
        g["nodes"][group]["children"].append(len(g["nodes"]) - 1)

    happy = stroke([(-0.5, -0.28), (-0.27, 0.14), (0, 0.32), (0.27, 0.14), (0.5, -0.28)], 0.27)
    sleepy = stroke([(-0.5, 0.12), (-0.24, -0.1), (0.24, -0.1), (0.5, 0.12)], 0.24)
    grin = [(-0.5, 0.3)] + [(math.cos(math.pi + i * math.pi / 12) * 0.5, 0.3 + math.sin(math.pi + i * math.pi / 12) * 0.8) for i in range(1, 12)] + [(0.5, 0.3)]
    for side, c in (("L", EYE_L), ("R", EYE_R)):
        part("KuroHappyEye" + side, prism(happy, c, (0.017, 0.015)), spirit, EYE_T)
        part("KuroSleepEye" + side, prism(sleepy, c, (0.017, 0.011)), spirit, EYE_T)
        part("KuroHeartEye" + side, prism(heart(), c, (0.019, 0.019), Z_BACK + 0.0002, Z_FRONT + 0.0004), heart_mat, EYE_T)
        # A four-point glint high on the outer side of each eye, in front of it.
        sx = -1 if side == "L" else 1
        part("KuroSparkle" + side, prism(star(), (c[0] + sx * 0.0055, c[1] + 0.0075), (0.012, 0.012), Z_FRONT + 0.0004, Z_FRONT + 0.0012), sparkle, EYE_T)
    part("KuroOhMouth", ring(MOUTH, (0.0105, 0.012)), spirit, MOUTH_T)
    part("KuroGrinMouth", prism(grin, (MOUTH[0], MOUTH[1] + 0.002), (0.02, 0.011)), spirit, MOUTH_T)
    part("KuroTongue", prism(oval(0.5, 0.42), (MOUTH[0] + 0.002, MOUTH[1] - 0.0045), (0.0065, 0.0065), Z_FRONT + 0.0002, Z_FRONT + 0.0006), tongue_mat, MOUTH_T)

    rage_after = hashlib.sha256(repr([n for n in g["nodes"] if "Rage" in n.get("name", "")]).encode()).hexdigest()
    assert rage_after == rage_before, "The rage form changed"
    assert animation_digest(g, data) == anim_before, "His motion changed"
    g.setdefault("extras", {})["cuteKuro"] = MARK
    write(GHOST, g, data)
    print("Kuro is cuter:", GHOST.relative_to(ROOT))


if __name__ == "__main__":
    main()
