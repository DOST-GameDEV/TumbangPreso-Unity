"""Basilio's BOULDER, the stone the ability grows round his held tsinelas (`FxStudyDanteBoulder`): a cluster of rocks
pressed together on the shoe, and the rubble they break into, as a model and its painted atlas.

  py -3 tools/build_dante_boulder_fx.py

Writes Assets/TumbangPreso/Resources/Models/AbilityFx/dante_boulder.glb and dante_boulder_paint.png beside it.
Real metres, y up, +z the tsinelas's toe, the origin at the middle of the tsinelas's sole.

ITS FOURTH BUILD, AND THE FIRST TO THE OWNER'S OWN PICTURES. Of the third (sharp wedges with a keel) he sent three
pictures of rocks and the words "this is what good stylized rcks look like". Read from those pictures:

  * CHUNKY, CONVEX, SOFT-CORNERED. Each rock is an egg cut into ten to fourteen big facets: a top, a ring of facets
    leaning in above the middle, a ring leaning in below it, a flat foot. No thin wedge, no peak, no knife edge.
    So a rock here is the space inside planes laid round an EGG (`ROUND`, `egg`), each set a little nearer or
    farther than the egg's own skin so no two facets are a size, with one or two small facets at a corner.
  * THE EDGE NETWORK IS THE LOOK. A pale soft line along EVERY facet boundary, wider and brighter on the upper edges,
    thin and dim low down, a little uneven, swelling into a small pale patch where facets meet.
  * EVERY FACET IS A GRADIENT: lighter toward its upper edge, darker toward its lower, faintly mottled. Tops clearly
    paler and warmer, sides mid and cooler, the foot dark enough to sink into its own shadow.
  * ONE SMALL DENT a rock, with its own pale rim. No long cracks anywhere.
  * A CLUSTER: one big rock behind, smaller ones of different sizes pressed against it and tucked at its foot.

So the stone on the shoe is four rocks pressed together: the big one closed over the heel, a broad one under the
sole, a middling one on the left, a small one tucked low on the right. The toe and the strap stay out in front.
Rocks and shoe together are about 0.54 long, 0.46 wide and 0.38 high, against the shoe's own 0.51 m hit capsule.

HIS MOLTEN LIGHT LIVES IN THE GAPS. The pictures have no glowing cracks, so there are none drawn across faces. The
atlas's alpha (what `FxStudyRock.shader` lights when the stone is hot, and leaves dark when it is cold) is laid only
where one rock presses on another (`Atlas.paint`: how far each texel is from its neighbours' skins), and in the big
rock's one dent.

Colours are his stone (`build_hands_dante.PALETTE`) taken to the pictures' values: a warm sandy top, a cooler grey
side, a dark foot. Normals are smooth (one a corner), so the ink line round a rock does not split at an edge; the
facets read from the paint, not from the light.

Each rock is a named node with its origin at its OWN MIDDLE, sitting at its seat: crown, jaw, knuckle-l, knuckle-r.
Also chunk0 chunk1 chunk2 (rubble: the same rocks, small), and for the bench only a stand-in shoe and a stand-in body
1.6 m tall (flat palette colours, not painted).
"""
import math

import cv2
import numpy as np

import hand_companion_kit as kit
from hand_companion_kit import rounded, tube

from build_hands_dante import WRAP, CORD, SPARE

ATLAS, CELL, MARGIN = 1024, 112, 8


def rgb(h):
    return np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)], np.float32) / 255.0


TOP, SIDE, FOOT = rgb("#c9b88c"), rgb("#77756d"), rgb("#34333a")
LINE_WARM, LINE_COOL, DENT = rgb("#f1e6c4"), rgb("#b4b8ae"), rgb("#4a443f")


def around(elevation, turns, nears):
    """Planes of one ring: each leans `elevation` degrees up from level, at these turns (degrees round the rock), each
    this share of the egg's own reach from its middle."""
    e = math.radians(elevation)
    return [((math.cos(e) * math.sin(math.radians(t)), math.sin(e), math.cos(e) * math.cos(math.radians(t))), n) for t, n in zip(turns, nears)]


# Three different cuts of an egg: fourteen, eleven and eleven facets. (A ring of only three left corners sticking
# out a third past the egg, so no ring has fewer than four.) Each: the top, the upper ring, the lower ring, the
# foot, and the small facets taken off a corner.
ROUND = {
    "big": [((.08, 1, .05), .88)] + around(38, (20, 95, 170, 245, 320), (.92, .86, .95, .90, .84))
           + around(-20, (55, 130, 205, 280, 350), (.90, .94, .86, .92, .88)) + [((0, -1, 0), .80)] + around(68, (300,), (.985,)) + around(8, (100,), (.99,)),
    "mid": [((-.10, 1, .10), .84)] + around(30, (0, 72, 144, 216, 288), (.90, .86, .93, .88, .92))
           + around(-32, (36, 126, 216, 306), (.90, .88, .92, .86)) + [((0, -1, 0), .82)],
    "small": [((.06, 1, -.08), .86)] + around(28, (10, 85, 150, 225, 300), (.90, .86, .92, .88, .93))
             + around(-30, (45, 135, 215, 320), (.90, .88, .92, .86)) + [((0, -1, 0), .80)],
}

# name: seat, half size, tilt (degrees about x, y, z), which cut, the dent (a direction from its middle and a radius).
ROCKS = {
    # (Their first sizes, the old wedges' own, left an egg far smaller than the block it was cut from: a nugget on a
    # bare shoe. These fill the same outline the stone has always had.)
    "crown":     ((-0.005, 0.035, -0.090), (0.185, 0.195, 0.185), (-6, 20, 5), "big", ((.35, .95, .30), .034)),
    "jaw":       ((0.030, -0.075, 0.010), (0.140, 0.105, 0.140), (3, -25, -4), "mid", None),
    "knuckle-l": ((-0.125, -0.035, -0.010), (0.095, 0.115, 0.105), (8, 50, 12), "mid", ((-.8, .5, .4), .019)),
    "knuckle-r": ((0.135, -0.065, -0.030), (0.080, 0.085, 0.095), (-10, 140, -8), "small", None),
}
CHUNKS = {
    "chunk0": ((.072, .060, .066), (5, 30, -8), "small", ((.3, .9, .3), .014)),
    "chunk1": ((.050, .044, .060), (-12, 200, 6), "small", None),
    "chunk2": ((.040, .038, .040), (9, 110, 14), "small", None),
}


def unit(v):
    v = np.array(v, np.float64)
    return v / np.linalg.norm(v)


def turned(tilt):
    rx, ry, rz = (math.radians(a) for a in tilt)
    mx = np.array([[1, 0, 0], [0, math.cos(rx), -math.sin(rx)], [0, math.sin(rx), math.cos(rx)]])
    my = np.array([[math.cos(ry), 0, math.sin(ry)], [0, 1, 0], [-math.sin(ry), 0, math.cos(ry)]])
    mz = np.array([[math.cos(rz), -math.sin(rz), 0], [math.sin(rz), math.cos(rz), 0], [0, 0, 1]])
    return mz @ my @ mx


def egg(half, tilt, cut):
    """The planes of one rock as it sits (normal, distance from its middle): each of the cut's planes stood off an
    egg of this half size, then the whole rock tipped."""
    half, m = np.array(half, np.float64), turned(tilt)
    planes = []
    for n, near in ROUND[cut]:
        n = unit(n)
        planes.append((m @ n, float(np.linalg.norm(n * half)) * near))
    return planes


def faces_of(planes):
    """The faces of the solid inside the planes: (corners in order, counter-clockwise from outside; normal)."""
    faces = []
    for i, (n, d) in enumerate(planes):
        a = np.cross(n, (0, 1, 0) if abs(n[1]) < .9 else (1, 0, 0))
        a /= np.linalg.norm(a)
        b = np.cross(n, a)
        poly = [n * d + a * x + b * y for x, y in ((-5, -5), (5, -5), (5, 5), (-5, 5))]
        for j, (m2, d2) in enumerate(planes):
            if j == i or not poly:
                continue
            out = []
            for k in range(len(poly)):
                p, q = poly[k], poly[(k + 1) % len(poly)]
                sp, sq = float(p @ m2) - d2, float(q @ m2) - d2
                if sp <= 0:
                    out.append(p)
                if (sp < 0) != (sq < 0) and abs(sp - sq) > 1e-12:
                    out.append(p + (q - p) * (sp / (sp - sq)))
            poly = out
        clean = [p for k, p in enumerate(poly) if np.linalg.norm(p - poly[k - 1]) > 1e-3]
        if len(clean) >= 3:
            faces.append((np.array(clean), n))
    return faces


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


class Atlas:
    def __init__(self):
        self.image = np.zeros((ATLAS, ATLAS, 4), np.float32)
        self.image[:, :, :3] = SIDE
        self.used = 0
        rng = np.random.default_rng(11)
        mottle = cv2.GaussianBlur(rng.random((ATLAS, ATLAS)).astype(np.float32), (0, 0), 9)
        self.mottle = (mottle - mottle.mean()) / mottle.std()

    def paint(self, corners, normal, low, high, seat, others, dent):
        """Paints one face into the next cell and returns its corners' UVs (v down, as the kit writes them). `others`
        are the neighbouring rocks' planes and seats: where this face comes near one of them is a gap, and molten."""
        grid = ATLAS // CELL
        row, col = divmod(self.used, grid)
        self.used += 1
        c = corners.mean(axis=0)
        e1 = unit(corners[1] - corners[0])
        e2 = np.cross(normal, e1)
        flat = np.stack([(corners - c) @ e1, (corners - c) @ e2], axis=1)
        lo, hi = flat.min(axis=0), flat.max(axis=0)
        scale = (CELL - 2 * MARGIN) / float((hi - lo).max())
        mid = (lo + hi) * .5
        ys, xs = np.mgrid[0:CELL, 0:CELL].astype(np.float64)
        qx, qy = (xs + .5 - CELL * .5) / scale + mid[0], (ys + .5 - CELL * .5) / scale + mid[1]
        p = c + qx[..., None] * e1 + qy[..., None] * e2                     # where each texel is on the rock
        height = np.clip((p[..., 1] - low) / (high - low), 0, 1)
        ny = float(normal[1])

        # The facet: its tone by how far it looks up, toward the light a touch paler, a gradient up it, darker to the foot.
        colour = FOOT + (SIDE - FOOT) * smooth(-.75, -.15, ny)
        colour = colour + (TOP - colour) * smooth(.45, .80, ny)
        toward = float(normal @ unit((.5, 0, -.65)))
        colour = colour * (1 + .14 * toward) + np.array([.014, .004, -.014], np.float32) * toward
        img = np.broadcast_to(colour, (CELL, CELL, 3)).astype(np.float64).copy()
        face_low, face_high = corners[:, 1].min(), corners[:, 1].max()
        img *= (.84 + .22 * np.clip((p[..., 1] - face_low) / max(1e-6, face_high - face_low), 0, 1))[..., None]
        img *= (.50 + .50 * smooth(.02, .42, height))[..., None]
        y0, x0 = row * CELL, col * CELL
        img *= (1 + .035 * self.mottle[y0:y0 + CELL, x0:x0 + CELL])[..., None]

        # The edge network: a pale line inside every edge, wider and brighter the higher the edge is, a little uneven,
        # and a patch at each corner.
        uneven = 1 + .30 * np.sin(p[..., 0] * 47 + p[..., 2] * 31) * np.sin(p[..., 1] * 53 + p[..., 0] * 17)
        count = len(flat)
        inside = np.full((CELL, CELL), 1e9)
        for k in range(count):
            a, b = flat[k], flat[(k + 1) % count]
            along = (b - a) / np.linalg.norm(b - a)
            dist = (qx - a[0]) * -along[1] + (qy - a[1]) * along[0]
            inside = np.minimum(inside, dist)
        wide = (.0030 + .0050 * smooth(.15, .80, height)) * uneven
        strong = .30 + .62 * smooth(.20, .85, height)
        line = (1 - smooth(wide * .35, wide, inside)) * strong
        for k in range(count):
            d = np.hypot(qx - flat[k][0], qy - flat[k][1])
            line = np.maximum(line, (1 - smooth(wide * 1.1, wide * 2.3, d)) * strong)
        tint = LINE_COOL + (LINE_WARM - LINE_COOL) * smooth(.35, .80, height)[..., None]
        img = img + (tint - img) * line[..., None]

        alpha = np.zeros((CELL, CELL))
        # The gaps: where this rock's skin comes within a finger of a neighbour's, dark when cold and molten when hot.
        world = p + np.array(seat)
        for planes, where in others:
            rel = world - np.array(where)
            away = np.full((CELL, CELL), -1e9)
            for n, d in planes:
                away = np.maximum(away, rel @ n - d)
            near = 1 - smooth(.002, .020, away)
            img = img + (DENT * .6 - img) * (near * .8)[..., None]
            alpha = np.maximum(alpha, 1 - smooth(.000, .015, away))
        # The dent: an oval scooped out, darker inside and lighter toward its floor, a pale rim strongest underneath.
        if dent is not None:
            at, radius = dent
            rel = p - at
            d = np.linalg.norm(rel, axis=-1) / radius
            scoop = 1 - smooth(.80, 1.0, d)
            lower = np.clip(-(rel[..., 1]) / radius * .5 + .5, 0, 1)
            img = img + (DENT * (.75 + .5 * lower)[..., None] - img) * scoop[..., None]
            rim = np.clip(1 - np.abs(d - 1.08) / .16, 0, 1) * (.45 + .55 * lower)
            img = img + (LINE_WARM - img) * rim[..., None]
            alpha = np.maximum(alpha, (1 - smooth(.35, .75, d)) * .85)
        self.image[y0:y0 + CELL, x0:x0 + CELL, :3] = img
        self.image[y0:y0 + CELL, x0:x0 + CELL, 3] = alpha
        px = (flat - mid) * scale + CELL * .5
        return np.stack([(x0 + px[:, 0]) / ATLAS, (y0 + px[:, 1]) / ATLAS], axis=1).astype(np.float32)

    def save(self, path):
        out = np.clip(self.image * 255 + .5, 0, 255).astype(np.uint8)
        cv2.imwrite(str(path), cv2.cvtColor(out, cv2.COLOR_RGBA2BGRA))
        print("wrote %s: %d faces painted, %.1f%% of it molten" % (path.name, self.used, 100 * (self.image[:, :, 3] > .5).mean()))


def rock(atlas, planes, seat=(0, 0, 0), others=(), dent=None):
    """A rock as one painted piece: (mesh, uvs). Corners are not shared between faces (each has its own place in the
    atlas), but a corner's normal is the same on every face that meets there, so the ink line stays closed."""
    faces = faces_of(planes)
    low = min(float(c[:, 1].min()) for c, _ in faces)
    high = max(float(c[:, 1].max()) for c, _ in faces)
    if dent is not None:
        # The dent's middle is on the rock's skin, where a ray that way from its middle leaves it.
        way = unit(dent[0])
        dent = (way * min(d / float(n @ way) for n, d in planes if float(n @ way) > 1e-6), dent[1])
    pos, uvs, tris = [], [], []
    for corners, normal in faces:
        base = len(pos)
        uv = atlas.paint(corners, normal, low, high, seat, others, dent)
        pos.extend(corners)
        uvs.extend(uv)
        for k in range(1, len(corners) - 1):
            tris.append((base, base + k, base + k + 1))
    pos = np.array(pos, np.float32)
    nrm = np.zeros_like(pos)
    for corners, normal in faces:
        for c in corners:
            nrm[np.linalg.norm(pos - c.astype(np.float32), axis=1) < 1e-4] += normal.astype(np.float32)
    nrm /= np.maximum(np.linalg.norm(nrm, axis=1, keepdims=True), 1e-6)
    return (pos, nrm, np.array(tris, np.uint32)), np.array(uvs, np.float32)


def build():
    m = kit.Model("dante_boulder")
    m.folder = kit.ROOT / "Assets/TumbangPreso/Resources/Models/AbilityFx"
    atlas = Atlas()
    cut = {name: egg(half, tilt, which) for name, (_, half, tilt, which, _) in ROCKS.items()}
    lo = np.min([np.min([c.min(axis=0) for c, _ in faces_of(cut[n])], axis=0) + np.array(ROCKS[n][0]) for n in ROCKS], axis=0)
    hi = np.max([np.max([c.max(axis=0) for c, _ in faces_of(cut[n])], axis=0) + np.array(ROCKS[n][0]) for n in ROCKS], axis=0)
    print("the stone alone: %.3f wide, %.3f high, from %.3f to %.3f along the shoe (the shoe's toe is at 0.220)" % (hi[0] - lo[0], hi[1] - lo[1], lo[2], hi[2]))
    for name, (seat, _, _, _, dent) in ROCKS.items():
        others = [(cut[other], ROCKS[other][0]) for other in ROCKS if other != name]
        mesh, uv = rock(atlas, cut[name], seat, others, dent)
        m.add(name, [(mesh, uv)], at=seat)
    for name, (half, tilt, which, dent) in CHUNKS.items():
        mesh, uv = rock(atlas, egg(half, tilt, which), dent=dent)
        m.add(name, [(mesh, uv)])
    # The stand-ins. The shoe: a sole and a strap. The person: one rounded block and a head, in a quiet dark tone.
    m.add("shoe", [(rounded((.085, .024, .220), .022, seg=12, rings=6), WRAP),
                   (tube([(-.075, .015, .050), (0, .085, .105), (.075, .015, .050)], .018, seg=6), CORD)])
    m.add("body", [(rounded((.30, .52, .20), .16, at=(0, .52, 0), seg=14, rings=8), SPARE),
                   (rounded((.27, .27, .25), .12, at=(0, 1.33, 0), seg=14, rings=8), SPARE)])
    m.write()
    atlas.save(m.folder / "dante_boulder_paint.png")


if __name__ == "__main__":
    build()
