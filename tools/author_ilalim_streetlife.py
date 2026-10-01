"""Model the STREET LIFE of Ilalim ng Tulay: fiesta, parols, parked cars, tarps, pots (ILALIM-1.3).

  py -3 tools/author_ilalim_textures_streetlife.py [--sheet N]     # paint the life_ textures first
  blender -b --python tools/author_ilalim_streetlife.py -- [--preview N] [--only a,b] [--no-check]

Writes ArtSource/ilalim/streetlife.blend with ONE top-level placed collection, "streetlife
(placed)", which tools/author_ilalim_city.py links (it is in its OPTIONAL_KITS). The prototypes sit
at the origin in hidden collections whose names contain "prototype", which the city skips. With
--preview it writes versioned close-ups, rendered against the linked context kits, to
Logs/ilalim-blender/life_<shot>_vN.png (an existing file is never overwritten).

Owner, 2026-09-30: "can you think of a way to make the place look more lively, more unique
building shapes etc?", then "proceed". Four agents split that pass; this kit is the street life.

HOW IT FINDS ITS PLACES. Nothing here is typed in from a render. At build time the script LINKS,
read-only, the kits it has to fit (street.blend: ground, furniture, fences; trees.blend;
eastside.blend; heritage.blend; sarisari.blend) into the scene, and asks them:
  * the real street poles (objects street_pole, street_pole_tx, street_pole_coil and their
    copies), their lean and their crossarm direction, so every tie and bracket wraps its pole;
  * the Padre Faura kerb lines at any x (the road surface is the ground class at z 0.000), so
    parked cars stand in the kerb lanes;
  * the floor under any point (pavement 0.212, lot 0.24, a shop step 0.37), so nothing floats
    or sinks by more than its 1 cm;
  * the fence and wall faces, by horizontal rays, and the gaps between fence pillars, so every
    tarp hangs between pillars 4 to 5 cm in front of the bars and every wall parol has a wall.
It then checks what it built: every object against every context object near it (BVH overlap),
the hard rules below, and the height of the bunting over the road; the report is printed. The
context links are removed before the save, so streetlife.blend holds only this kit (and its
links to vehicles.blend for the parked cars). If eastside.blend is being rewritten when the
script runs, the link is retried.

KEPT CLEAR (hard rules, checked on every vertex and every vehicle footprint):
  * the play area |x| < 11 and |y| < 16.5: nothing inside, nothing overhanging;
  * Taft's lanes |x| < 6.65 along the whole length, and the LRT-1 deck above |x| < 5.3: the
    bunting never crosses Taft; it is strung across Padre Faura east and west of the corners;
  * the corner lot x 11.8..22.8, y 38.3..47.3 (Bebang's sari-sari store);
  * Padre Faura's moving traffic: the city places a tricycle at (-24, 30.5) in the west arm's
    SOUTH kerb lane and a sedan at (-42, 33) in its middle lane, and a hatchback at (26, 30) in
    the east arm's middle lane. So the west arm is parked on its NORTH kerb only, and on the east
    arm the cars keep to the two outer thirds of the road, clear of the hatchback;
  * bunting stays above 4.5 m over any road (rope and pennants).

WHAT IS PLACED (Blender X = game x east, Y = game z north; heights absolute; the build prints the
final list, the checks and the triangle count):
  * BANDERITAS, 13 spans: chunky sagging ropes (5.6 cm) tied at 6.0 m to the street poles and at
    6.2 m to the bamboo poles (between two nodes), with big pennant cards (about 0.46 x 0.54 m,
    1.4 cm thick) every 0.62 m, each wrapped over the rope by a fat sleeve, twisted and curled a
    little. Eight designs in one atlas (life_pennants), two shapes (triangle, swallowtail). The
    pennants are cards and carry no bevel, as the trees kit's leaf cards.
      west arm   across on the pole pairs at x -43 and -60, the diagonals -26 to -44 and -42 to
                 -62, and one run along the south pavement from the corner pole (-10.9, 24.6) to
                 (-26, 25.8), tied at 4.3 m UNDER that pole line's own cables. Not -26 to -26
                 straight across: the fig at (-28, 38) swallows the north pole's top;
      east arm   the east arm's own poles at x 40 and 54 stand inside fig canopies, so bamboo poles
                 stand in the canopy gaps: on the north lots just behind the pavement (x 27, 37, 47,
                 60) and at the FRONT of the south pavement, 0.45 m behind the kerb (x 37, 47, 58),
                 because the pole line's cables run along its back. Spans from the pole at (26, 23.7)
                 and the south bamboos to the north ones: four across, three diagonal;
      side drive the service drive east of Taft south of the court (the drive past the Manok
                 grill), one short span between two bamboo poles at x 12.45, y -36.95 and -39.5,
                 clear of the Vista GL's upper floors, which overhang the drive from 4.5 m up.
    Every span over a road stays above 4.5 m (the check prints each span's lowest point).
  * BAMBOO FIESTA POLES, 9: 7.2 m, fat (8 cm at the foot), a node every 0.4 m (the texture's
    rings sit on the geometry's bulges), set in an old tyre filled with concrete; linked
    duplicates of one prototype, each with its own yaw and lean.
  * PAROLS, 29: chunky five-point stars (0.84 m across, 15 cm deep, pillowed), a proud round
    medallion, two striped tails with fat tassels; four colourways (life_parols atlas). The
    material's colour is also wired to its emission at strength 0, with the custom property
    "unity_emission" = 1.5 on it, so they can glow at dusk later. Hung from steel brackets:
      * on every Taft pole outside the play area within 70 m (facing the court), except the two
        east poles at y +/-17.3, whose arms would sit over the shop row's awnings;
      * on the Padre Faura poles (facing Taft), except the one inside the fig at (-26, 38.7);
      * near the top of four bamboo poles (north ones on the lot side, south ones toward Taft);
      * on the Astral podium's Padre Faura face at x 26.6, 37 and 46.6 (the bracket plate is
        found by a ray, and the parol is skipped if the wall is not there). The east row's upper
        floor south of the court was tried and dropped: the West East Center overhangs it.
  * PARKED VEHICLES, 20: collection instances of the vehicles kit (veh_* face +X; a parked car on
    one-way Padre Faura faces west), linked from vehicles.blend: five on the west arm's north kerb
    (the narrow ones nearest the corner), nine along both kerbs of the east arm with a tricycle
    line at the corner, three in the PGH parking behind the campus fence (noses to the fence, seen
    through the pickets), a tricycle on the service drive, and two PEDICABS modelled here (a
    chunky bicycle with a roofed sidecar, disc wheels, no spokes, a painted KUYA BOY panel), one
    in the tricycle line and one on the service drive.
  * TARPAULINS, 5, tied at their grommets by chunky rope to the fence rails or into the wall,
    each in a pillar gap found by rays at rail height and 4.5 cm in front of the rails: the
    birthday tarp on the PGH fence north of the court and the anti-rabies tarp south of it (both
    face the court), the barangay fiesta greeting (2.4 x 0.8) on the PGH fence along Padre Faura,
    the graduation tarp on the Supreme Court's white fence, and the councillor's Christmas
    greeting on the Astral podium's wall. The ties are their own objects ("<tarp> ties").
  * NOTICES, 4, strapped to poles: the bedspace board on the pole at (26, 23.7) turned toward the
    corner, DAHAN-DAHAN MAY MGA BATA on (-42, 26.3), BAWAL MAG-VIDEOKE on (-26, 38.7) under the
    fig, and the TODA TERMINAL board on the service drive's north bamboo pole.
  * POTTED PLANTS, 5 prototypes in rows of two or three: clay pots, a painted tin can, a plastic
    bucket and a cut-down trough, hollow, the soil 4 cm under the rim; a potted santan (the trees
    kit's shrub leaf at pot scale, crimson flower-ball cards), spider lily, a round shrub, a lily
    in bloom and the three-ball trough. Each pot finds its wall with its own ray and stands its
    plant's reach off it, and is nudged or dropped if its footprint would touch anything: on the
    step of the east row's south shops (y -17..-21.5), by the lugaw eatery on the Astral podium,
    under the birthday tarp, and at the feet of two bamboo poles.

THE HOUSE STYLE (KANTO_DESIGN_GUIDE.md sections 2 to 5, LAGOON_REWORK_GUIDE.md section 2), through
the prop kit's PBuf (tools/author_ilalim_props.py): chunky members, live bevels with hardened
normals, nothing sharing a plane (ties and brackets sink 1 to 2 cm into their poles, feet 1 cm
into the floor, tarps hang 4 to 5 cm off the bars), world UVs, flat drawn textures. Foliage is the
trees kit's (tools/author_ilalim_trees.py): shingled leaf cards, two tints.

ROLE HUES: nothing near #f87020 or #0080e8. The pennants and parols are crimson, butter yellow,
leaf green, fuchsia, cream, violet, jade and maroon. Every name is invented.
"""
import math
import random
import re
import sys
import time
from pathlib import Path

import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_ilalim_props as P                          # noqa: E402  (PBuf, materials)
from author_ilalim_props import PBuf, frame              # noqa: E402
from author_ilalim_lrt import fillet, rounded_rect       # noqa: E402
import author_ilalim_trees as T                          # noqa: E402  (foliage, leaf materials)

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "ilalim"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"
UP = Vector((0, 0, 1))

# The atlas numbers, the same as tools/author_ilalim_textures_streetlife.py.
PENNANT_COLS, PENNANT_ROWS = 4, 2
PENNANT_SHAPES = ["tri", "tri", "swallow", "tri", "swallow", "tri", "tri", "swallow"]
PAROL_R, PAROL_RI, PAROL_CENTRE = 0.42, 0.19, 0.12
PAROL_CELL, PAROL_ATLAS = 512, 1024
TAIL = (14, 14, 110, 120)
RIM = (455, 40)

# The hard rules.
PLAY_X, PLAY_Y = 11.0, 16.5
TAFT_LANES_X = 6.65
LOT = (11.8, 22.8, 38.3, 47.3)
BUNTING_MIN_Z = 4.5
CITY_TRAFFIC = [("veh_tricycle", (-24.0, 30.5), math.pi), ("veh_sedan_grey", (-42.0, 33.0), math.pi),
                ("veh_hatch_maroon", (26.0, 30.0), math.pi)]

TIE_STREET, TIE_BAMBOO = 6.0, 6.2        # the bamboo tie sits between the nodes at 6.0 and 6.4
BAMBOO_H = 7.2

P.M.update({
    "life_bamboo":        ("life_bamboo", None, 0.7, 0.0, 0, False),
    "life_rope":          (None, (0.72, 0.64, 0.48), 0.9, 0.0, 0, False),
    "life_pennants":      ("life_pennants", None, 0.55, 0.0, 0, False),
    "life_parols":        ("life_parols", None, 0.5, 0.0, 0, False),
    "life_tarp_back":     ("prop_tarp", (0.92, 0.90, 0.86), 0.8, 0.0, 0, False),
    "life_tarp_birthday": ("life_tarp_birthday", None, 0.7, 0.0, 0, False),
    "life_tarp_grad":     ("life_tarp_grad", None, 0.7, 0.0, 0, False),
    "life_tarp_fiesta":   ("life_tarp_fiesta", None, 0.7, 0.0, 0, False),
    "life_tarp_rabies":   ("life_tarp_rabies", None, 0.7, 0.0, 0, False),
    "life_tarp_pasko":    ("life_tarp_pasko", None, 0.7, 0.0, 0, False),
    "life_sign_bedspace": ("life_sign_bedspace", None, 0.85, 0.0, 0, False),
    "life_sign_dahan":    ("life_sign_dahan", None, 0.5, 0.2, 0, False),
    "life_sign_videoke":  ("life_sign_videoke", None, 0.85, 0.0, 0, False),
    "life_sign_toda":     ("life_sign_toda", None, 0.8, 0.0, 0, False),
    "life_sign_pedicab":  ("life_sign_pedicab", None, 0.6, 0.1, 0, False),
    "life_clay":          ("life_clay", None, 0.85, 0.0, 0, True),
    "life_soil":          ("tree_pit_soil_albedo", None, 0.95, 0.0, 0, False),
    "life_tin_green":     ("prop_paint", (0.33, 0.55, 0.38), 0.5, 0.2, 0, True),
    "life_tin_red":       ("prop_paint", (0.62, 0.18, 0.18), 0.5, 0.2, 0, True),
    "life_tin_yellow":    ("prop_paint", (0.86, 0.72, 0.30), 0.5, 0.2, 0, True),
    "life_pedicab_body":  ("prop_paint", (0.26, 0.46, 0.31), 0.5, 0.1, 0, True),
})


def local(coll, name):
    """The LOCAL datablock called `name`, never a linked one from the context kits."""
    for d in coll:
        if d.name == name and d.library is None:
            return d
    return None


def make_materials_local():
    """Create every material this kit uses BEFORE the context kits are linked, and make the props
    and trees kits' lookups prefer local materials. Otherwise a lookup by name can hand back a
    context kit's linked material of the same name (the trees kit's leaves, the prop kit's
    steel), which disappears when the context links are removed before the save."""
    for name in P.M:
        P.material(name)
    for kind in ("shrub", "hedge", "lily"):
        for which in ("light", "dark"):
            T.leaf_material(kind, which)
    T.card_material("tree_flower_ixora", "tree_ixora", (0.62, 0.05, 0.12), gain=1.25)
    T.card_material("tree_flower_lily", "tree_lily_flower", (1.0, 1.0, 1.0), gain=1.0)
    T.wood_material("tree_lily_scape")
    p_material, t_card, t_wood = P.material, T.card_material, T.wood_material
    P.material = lambda name: local(bpy.data.materials, name) or p_material(name)
    T.card_material = lambda name, *a, **k: local(bpy.data.materials, name) or t_card(name, *a, **k)
    T.wood_material = lambda name: local(bpy.data.materials, name) or t_wood(name)


def emissive_ready(name, unity_strength):
    """Wire the albedo into the emission at strength 0, so the parols can be lit later."""
    m = P.material(name)
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    tex = next(n for n in nodes if n.type == "TEX_IMAGE")
    links.new(tex.outputs["Color"], bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = 0.0
    m["unity_emission"] = unity_strength


def const_uv(b, faces, uv):
    for f in faces:
        b.set_uvs(f, {v: uv for v in f.verts})


def rz(a):
    return Matrix.Rotation(a, 4, "Z")


def horiz(v):
    v = Vector((v.x, v.y, 0.0))
    return v.normalized() if v.length > 1e-6 else Vector((1, 0, 0))


# ------------------------------------------------------------------ the context (read-only links)

LINKS = {"street": ["street ground", "street furniture (placed)", "street fences and walls"],
         "trees": ["trees over Ilalim"], "eastside": ["eastside"], "heritage": ["heritage west of Taft"],
         "sarisari": ["sarisari (placed)"]}
POLE_NAME = re.compile(r"^street_pole(_tx|_coil)?(\.\d+)?$")


class Pole:
    """A pole the kit ties to: a street pole (tapered concrete, maybe leaning) or a bamboo pole."""

    def __init__(self, base, axis=UP, kind="street", out=None, name=""):
        self.base, self.axis, self.kind, self.name = Vector(base), Vector(axis).normalized(), kind, name
        self.out = out or Vector((1, 0, 0))

    def centre(self, z):
        return self.base + self.axis * ((z - self.base.z) / self.axis.z)

    def radius(self, z):
        h = z - self.base.z
        if self.kind == "street":
            return 0.18 * (1 - 0.38 * (h + 0.35) / 9.95)
        return bamboo_r(h)


class Context:
    def __init__(self):
        self.scene = bpy.context.scene
        self.cols, self.libs = [], set()
        for kit, names in LINKS.items():
            path = SOURCE / f"{kit}.blend"
            for attempt in range(4):
                try:
                    with bpy.data.libraries.load(str(path), link=True) as (src, dst):
                        dst.collections = [n for n in names if n in src.collections]
                    break
                except Exception as e:                      # a kit being rewritten right now
                    print(f"[ilalim-life] linking {kit} failed ({e}), retrying")
                    time.sleep(8)
            for c in dst.collections:
                if c is not None:
                    self.scene.collection.children.link(c)
                    self.cols.append(c)
                    self.libs.add(c.library)
        self.dg = bpy.context.evaluated_depsgraph_get()
        self.meshes = [o for c in self.cols for o in c.all_objects if o.type == "MESH"]
        self.ground = [o for c in self.cols if c.name == "street ground" for o in c.all_objects if o.type == "MESH"]
        self._bvh = {}
        self._bbox = {}
        self.poles = []
        for o in self.meshes:
            if POLE_NAME.match(o.name):
                mw = o.matrix_world
                self.poles.append(Pole(mw.translation.copy(), mw.to_3x3() @ UP, "street",
                                       horiz(mw.to_3x3() @ Vector((1, 0, 0))), o.name))
        print(f"[ilalim-life] context: {len(self.meshes)} meshes, {len(self.poles)} street poles")

    # -- geometry caches ----------------------------------------------------------------------

    def bvh(self, o):
        key = o.name_full
        if key not in self._bvh:
            ev = o.evaluated_get(self.dg)
            me = ev.to_mesh()
            mw = o.matrix_world
            verts = [mw @ v.co for v in me.vertices]
            polys = [tuple(p.vertices) for p in me.polygons]
            ev.to_mesh_clear()
            self._bvh[key] = BVHTree.FromPolygons(verts, polys) if polys else None
        return self._bvh[key]

    def bbox(self, o):
        key = o.name_full
        if key not in self._bbox:
            pts = [o.matrix_world @ Vector(c) for c in o.bound_box]
            self._bbox[key] = (Vector([min(p[i] for p in pts) for i in range(3)]),
                               Vector([max(p[i] for p in pts) for i in range(3)]))
        return self._bbox[key]

    # -- queries --------------------------------------------------------------------------------

    def ray(self, origin, direction, dist=50.0, skip=("tree_",)):
        o = Vector(origin)
        d = Vector(direction).normalized()
        travelled = 0.0
        while travelled < dist:
            hit, loc, nrm, _i, ob, _m = self.scene.ray_cast(self.dg, o, d, distance=dist - travelled)
            if not hit:
                return None
            if not (ob.name.startswith(skip) and not ob.name.startswith("tree_pit")):
                return loc, nrm, ob.name
            travelled += (loc - o).length + 0.003
            o = loc + d * 0.003
        return None

    def floor(self, x, y, top=2.6):
        """The walkable surface under (x, y): the first upward-facing hit under `top` that is not a
        tree or a car."""
        o = Vector((x, y, top))
        for _ in range(30):
            hit, loc, nrm, _i, ob, _m = self.scene.ray_cast(self.dg, o, Vector((0, 0, -1)), distance=10)
            if not hit:
                return 0.0, None
            if nrm.z > 0.85 and not ob.name.startswith(("tree_fig", "tree_mango", "tree_narra", "tree_rain", "veh_")):
                return loc.z, ob.name
            o = loc + Vector((0, 0, -0.003))
        return 0.0, None

    def ground_z(self, x, y):
        """The ground classes only (road 0, kerb 0.15, pavement 0.212, lot 0.24)."""
        best = None
        for o in self.ground:
            bv = self.bvh(o)
            if bv is None:
                continue
            h = bv.ray_cast(Vector((x, y, 5.0)), Vector((0, 0, -1)), 20.0)
            if h[0] is not None and (best is None or h[0].z > best):
                best = h[0].z
        return best if best is not None else 0.0

    def road_edges(self, x, y_mid=31.0, span=12.0, step=0.05):
        """Padre Faura's road surface (z < 0.05) at this x: (south edge, north edge)."""
        ys = [y_mid - span + k * step for k in range(int(2 * span / step) + 1)]
        road = [self.ground_z(x, y) < 0.05 for y in ys]
        k0 = min(range(len(ys)), key=lambda k: abs(ys[k] - y_mid) if road[k] else 1e9)
        lo = hi = k0
        while lo > 0 and road[lo - 1]:
            lo -= 1
        while hi < len(ys) - 1 and road[hi + 1]:
            hi += 1
        return ys[lo], ys[hi]

    def pole_at(self, x, y, within=2.0):
        best = min(self.poles, key=lambda p: (p.base.x - x) ** 2 + (p.base.y - y) ** 2)
        if (best.base.x - x) ** 2 + (best.base.y - y) ** 2 > within ** 2:
            raise ValueError(f"no street pole near ({x}, {y})")
        return best

    def fence_gap(self, start, along, into, z, lo, hi, want, width):
        """Scan a fence from `start + along * s` (s in lo..hi) with rays `into` it at rail height z.
        A pillar is a sample standing 10 cm or more proud of the local median (the fence may run at
        a slant); a gap is a run of samples on the rail line. Returns (centre on the tarp plane,
        outward normal) for the gap nearest `want` that is wider than `width`, or None."""
        along, into = Vector(along).normalized(), Vector(into).normalized()
        samples = []
        s = lo
        while s <= hi + 1e-6:
            o = Vector(start) + along * s + Vector((0, 0, z))
            h = self.ray(o, into, 3.0)
            if h:
                samples.append((s, (h[0] - o).length, h[0]))
            else:
                samples.append((s, None, None))
            s += 0.05
        on_rail = []
        for s0, d, _p in samples:
            near = sorted(d2 for s2, d2, _q in samples if d2 is not None and abs(s2 - s0) <= 0.8)
            med = near[len(near) // 2] if near else None
            on_rail.append(d is not None and med is not None and abs(d - med) < 0.05)
        gaps, run = [], []
        for smp, ok in zip(samples, on_rail):
            if ok:
                run.append(smp)
            else:
                if run:
                    gaps.append(run)
                run = []
        if run:
            gaps.append(run)
        gaps = [g for g in gaps if g[-1][0] - g[0][0] >= width + 0.06]
        if not gaps:
            return None
        g = min(gaps, key=lambda g: abs((g[0][0] + g[-1][0]) / 2 - want))
        a, b = g[0][2], g[-1][2]
        line = horiz(b - a)
        n = UP.cross(line)
        if n.dot(into) > 0:
            n = -n
        mid = (a + b) / 2
        if g[-1][0] - g[0][0] > width + 1.0:
            s_want = min(max(want, g[0][0] + width / 2 + 0.05), g[-1][0] - width / 2 - 0.05)
            f = (s_want - g[0][0]) / (g[-1][0] - g[0][0])
            mid = a.lerp(b, f)
        return mid + n * 0.045, n

    def touching(self, centre, half, z0, z1, yaw=0.0, skip=("ground", "road markings")):
        """The names of context meshes a box (half extents `half` across and along, z0..z1, turned by
        `yaw`) would touch."""
        c = Vector((centre[0], centre[1], 0))
        d = Vector((math.cos(yaw), math.sin(yaw), 0))
        e = Vector((-d.y, d.x, 0))
        pts = [c + d * sa * half[1] + e * sb * half[0] + Vector((0, 0, z)) for z in (z0, z1)
               for sa, sb in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        box = BVHTree.FromPolygons(pts, [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3),
                                         (3, 7, 4, 0)])
        lo = Vector([min(p[i] for p in pts) for i in range(3)])
        hi = Vector([max(p[i] for p in pts) for i in range(3)])
        out = []
        for o in self.meshes:
            if o.name.startswith(skip):
                continue
            blo, bhi = self.bbox(o)
            if blo.x > hi.x or bhi.x < lo.x or blo.y > hi.y or bhi.y < lo.y or blo.z > hi.z or bhi.z < lo.z:
                continue
            bv = self.bvh(o)
            if bv and box.overlap(bv):
                out.append(o.name)
        return out

    def close(self):
        for c in self.cols:
            if c.name in self.scene.collection.children:
                self.scene.collection.children.unlink(c)
        for lib in list(self.libs):
            bpy.data.libraries.remove(lib)


# ------------------------------------------------------------------ small builders

def bamboo_r(h):
    return 0.08 - 0.018 * max(0.0, min(h, BAMBOO_H)) / BAMBOO_H


def tie(b, pole, z, turns=2):
    """A chunky rope lashing: two fat turns round the pole, sunk 1 cm into it."""
    for k in range(turns):
        zz = z - 0.065 * k
        c = pole.centre(zz)
        b.torus(c, pole.radius(zz) + 0.012, 0.03, "life_rope", axis="z", seg=16, sides=7)


def sag_path(a, b, sag, n):
    return [a + (b - a) * (k / n) - Vector((0, 0, 4 * sag * (k / n) * (1 - k / n))) for k in range(n + 1)]


def pennant_uv(k, u, v):
    r, q = divmod(k, PENNANT_COLS)
    return ((q + u) / PENNANT_COLS, 1 - (r + 1) / PENNANT_ROWS + v / PENNANT_ROWS)


def pennant(b, p, x, k, rng):
    """One banderita at rope point p, rope direction x: a fat card hanging under a sleeve."""
    y = Vector((0, 0, -1))
    y = (y - x * x.dot(y)).normalized()
    z = x.cross(y)
    tw = math.radians(rng.uniform(-14, 14))
    y, z = y * math.cos(tw) + z * math.sin(tw), z * math.cos(tw) - y * math.sin(tw)
    w, L, t = 0.46 * rng.uniform(0.95, 1.05), 0.54 * rng.uniform(0.94, 1.06), 0.014
    if PENNANT_SHAPES[k] == "tri":
        pts = [(-w / 2, -0.012), (w / 2, -0.012), (0.0, L)]
    else:
        pts = [(-w / 2, -0.012), (w / 2, -0.012), (w / 2, L * 0.92), (0.0, L * 0.62), (-w / 2, L * 0.92)]
    prof = fillet(pts, 0.035, 2)
    curl = rng.uniform(-0.06, 0.06)

    def at(u, v, s):
        return p + x * u + y * v + z * (s + curl * (max(v, 0) / L) ** 2)

    faces = b.loft([[at(u, v, -t / 2) for u, v in prof], [at(u, v, t / 2) for u, v in prof]], "life_pennants")
    idx = b.mi("life_pennants")
    for f in faces:
        f.material_index = idx
    caps = faces[-2:]
    for f in caps:
        front = f.normal.dot(z) > 0
        uvs = {}
        for v in f.verts:
            d = v.co - p
            u, vv = d.dot(x), d.dot(y)
            uu = (u / w + 0.5) if front else (0.5 - u / w)
            uvs[v] = pennant_uv(k, min(0.99, max(0.01, uu)), min(0.99, max(0.01, 1 - vv / L)))
        b.set_uvs(f, uvs)
    const_uv(b, faces[:-2], pennant_uv(k, 0.5, 0.45))
    sleeve = b.tube([p - x * w * 0.48, p, p + x * w * 0.48], 0.042, "life_pennants", sides=6)
    const_uv(b, sleeve, pennant_uv(k, 0.5, 0.95))


def bunting(col, name, pa, za, pb, zb, rng, sag=None):
    """A chunky rope from pole pa at za to pole pb at zb, tied at both ends, hung with pennants.
    Returns the object and the rope's lowest point."""
    b = PBuf(name)
    ca, cb = pa.centre(za), pb.centre(zb)
    d = horiz(cb - ca)
    a = ca + d * (pa.radius(za) - 0.012)
    e = cb - d * (pb.radius(zb) - 0.012)
    L = (e - a).length
    sag = sag if sag is not None else min(0.045 * L + 0.15, 0.78)
    n = max(12, int(L / 0.3))
    path = sag_path(a, e, sag, n)
    b.tube(path, 0.028, "life_rope", sides=8)
    tie(b, pa, za)
    tie(b, pb, zb)
    # Pennants every 0.62 m of rope, starting clear of the ties.
    seg = [(path[i + 1] - path[i]).length for i in range(n)]
    total = sum(seg)
    count = int((total - 1.0) / 0.62)
    start = (total - (count - 1) * 0.62) / 2
    last = -1
    for i in range(count):
        s = start + i * 0.62
        acc, j = 0.0, 0
        while j < n - 1 and acc + seg[j] < s:
            acc += seg[j]
            j += 1
        f = (s - acc) / seg[j]
        p = path[j].lerp(path[j + 1], f)
        x = (path[j + 1] - path[j]).normalized()
        k = rng.randrange(len(PENNANT_SHAPES))
        if k == last:
            k = (k + 1 + rng.randrange(len(PENNANT_SHAPES) - 1)) % len(PENNANT_SHAPES)
        last = k
        pennant(b, p, x, k, rng)
    mid = (a + e) / 2
    # The pennants are cards and carry no bevel (as the trees kit's leaf cards); the rope is round.
    obj = b.finish(col, origin=(mid.x, mid.y, 0.0), bevel=0.0)
    return obj, min(p.z for p in path)


def bracket(b, pole, z, out, reach=0.62):
    """A steel parol bracket clamped to a pole: an arm, a brace and a drop pin. Returns the hook."""
    c, r = pole.centre(z), pole.radius(z)
    b.torus(c, r + 0.012, 0.03, "prop_steel_dark", axis="z", seg=12, sides=8)
    p1 = c + out * (r + reach)
    b.tube([c + out * (r - 0.02), p1], 0.028, "prop_steel_dark", sides=8)
    zb = z - 0.42
    cb, rb = pole.centre(zb), pole.radius(zb)
    b.torus(cb, rb + 0.012, 0.03, "prop_steel_dark", axis="z", seg=12, sides=8)
    b.tube([cb + out * (rb - 0.02), c + out * (r + reach * 0.55) + Vector((0, 0, -0.012))], 0.022,
           "prop_steel_dark", sides=6)
    b.tube([p1 + Vector((0, 0, 0.02)), p1 + Vector((0, 0, -0.12))], 0.02, "prop_steel_dark", sides=6)
    return p1 + Vector((0, 0, -0.1))


def wall_bracket(b, hit, normal, reach=0.55):
    """A bracket on a wall: a plate sunk 1 cm into the wall, an arm out along the normal, a brace."""
    n = horiz(normal)
    side = UP.cross(n)
    plate = hit + n * 0.0
    b.rbox(plate + n * 0.006, (0.14, 0.03, 0.26), "prop_steel_dark", r=0.01,
           rot=frame(side, n, UP, (0, 0, 0)))
    p1 = hit + n * reach
    b.tube([hit - n * 0.01 + Vector((0, 0, 0.08)), p1 + Vector((0, 0, 0.08))], 0.026, "prop_steel_dark", sides=8)
    b.tube([hit - n * 0.01 + Vector((0, 0, -0.1)), hit + n * reach * 0.6 + Vector((0, 0, 0.07))], 0.02,
           "prop_steel_dark", sides=6)
    b.tube([p1 + Vector((0, 0, 0.1)), p1 + Vector((0, 0, -0.04))], 0.02, "prop_steel_dark", sides=6)
    return p1 + Vector((0, 0, -0.02))


def board_on_pole(b, pole, z, n, w, h, decal, seed, t=0.035, mat="prop_wood"):
    """A hand-painted board strapped to a pole, its back 8 mm into the pole's round face."""
    n = horiz(n)
    c, r = pole.centre(z), pole.radius(z)
    right = UP.cross(n)
    at = c + n * (r + t / 2 - 0.008)
    b.panel(frame(right, UP, n, at), w, h, t, mat, decal, r=0.02, jitter=0.006, seed=seed)
    for dz in (h * 0.3, -h * 0.3):
        zc = z + dz
        cc, rr = pole.centre(zc), pole.radius(zc)
        atz = at + Vector((0, 0, dz))
        path = [atz + right * (w / 2 + 0.012) + n * (t / 2 + 0.008), atz + right * (w / 2 + 0.012) - n * (t / 2)]
        for k in range(9):
            a = k / 8 * math.pi
            path.append(cc + (right * math.cos(a) - n * math.sin(a)) * (rr + 0.012))
        path += [atz - right * (w / 2 + 0.012) - n * (t / 2), atz - right * (w / 2 + 0.012) + n * (t / 2 + 0.008),
                 atz + n * (t / 2 + 0.01), atz + right * (w / 2 + 0.012) + n * (t / 2 + 0.008)]
        b.tube(path, 0.013, "prop_steel_dark", sides=6)


# ------------------------------------------------------------------ prototypes

def parol_uv(k, px, py):
    r, q = divmod(k, 2)
    return ((q * PAROL_CELL + px) / PAROL_ATLAS, 1 - (r * PAROL_CELL + py) / PAROL_ATLAS)


def star_points(R, Ri):
    return [((R if i % 2 == 0 else Ri) * math.cos(math.radians(90 + 36 * i)),
             (R if i % 2 == 0 else Ri) * math.sin(math.radians(90 + 36 * i))) for i in range(10)]


def proto_parol(col, k):
    """One parol at the origin: the hook at (0, 0, 0), the star below it in the local X-Z plane,
    its faces toward -Y and +Y."""
    b = PBuf(f"life_parol_{k}")
    R, Ri = PAROL_R, PAROL_RI
    cz = -0.30 - R
    prof = fillet(star_points(R, Ri), 0.05, 1)
    rings = []
    for y, s in ((-0.075, 0.9), (-0.045, 0.99), (0.045, 0.99), (0.075, 0.9)):
        rings.append([Vector((x * s, y, cz + zz * s)) for x, zz in prof])
    faces = b.loft(rings, "life_parols")
    for f in faces[-2:]:
        front = f.normal.y < 0
        uvs = {}
        for v in f.verts:
            u = v.co.x / (2 * R * 0.9) + 0.5
            u = u if front else 1 - u
            w = (v.co.z - cz) / (2 * R * 0.9) + 0.5
            uvs[v] = parol_uv(k, u * PAROL_CELL, (1 - w) * PAROL_CELL)
        b.set_uvs(f, uvs)
    const_uv(b, faces[:-2], parol_uv(k, *RIM))
    # The medallion, proud of both faces by 2 cm.
    circle = [(PAROL_CENTRE * math.cos(a), PAROL_CENTRE * math.sin(a)) for a in (i / 16 * math.tau for i in range(16))]
    faces = b.loft([[Vector((x, y, cz + zz)) for x, zz in circle] for y in (-0.095, 0.095)], "life_parols")
    for f in faces[-2:]:
        front = f.normal.y < 0
        uvs = {}
        for v in f.verts:
            u = v.co.x / (2 * R) + 0.5
            u = u if front else 1 - u
            w = (v.co.z - cz) / (2 * R) + 0.5
            uvs[v] = parol_uv(k, u * PAROL_CELL, (1 - w) * PAROL_CELL)
        b.set_uvs(f, uvs)
    const_uv(b, faces[:-2], parol_uv(k, *RIM))
    # Two tails from the lower points: chunky segments alternating the two tail colours, a fat
    # tassel at each end.
    rng = random.Random(40 + k)
    band_a = parol_uv(k, TAIL[0] + 20, TAIL[1] + 6)
    band_b = parol_uv(k, TAIL[0] + 20, TAIL[1] + 24)
    for side in (-1, 1):
        a = math.radians(270 + side * 36)
        top = Vector((R * 0.62 * math.cos(a), 0.0, cz + R * 0.62 * math.sin(a)))
        length = 1.0 + rng.uniform(-0.08, 0.08)
        nseg = 7
        pts = []
        for i in range(nseg + 1):
            f = i / nseg
            pts.append(top + Vector((side * 0.06 * f + 0.035 * math.sin(f * 5.0 + side), 0.02 * math.sin(f * 3.0),
                                     -length * f)))
        for i in range(nseg):
            p0, p1 = pts[i], pts[i + 1]
            d = (p1 - p0).normalized()
            across = Vector((1, 0, 0))
            across = (across - d * across.dot(d)).normalized()
            thick = d.cross(across).normalized()
            prof2 = [(-0.045, -0.009), (0.045, -0.009), (0.045, 0.009), (-0.045, 0.009)]
            ring0 = [p0 - d * 0.01 + across * u + thick * v for u, v in prof2]
            ring1 = [p1 + d * 0.01 + across * u + thick * v for u, v in prof2]
            segf = b.loft([ring0, ring1], "life_parols")
            const_uv(b, segf, band_a if i % 2 == 0 else band_b)
        tip = pts[-1]
        tf = b.lathe(tip + Vector((0, 0, -0.14)), [(0.01, 0.0), (0.045, 0.02), (0.058, 0.07), (0.04, 0.12),
                                                   (0.012, 0.15)], "life_parols", sides=8)
        const_uv(b, tf, band_a)
    # The hanger: a fat wire from the hook down to the top point.
    b.tube([Vector((0, 0, 0.02)), Vector((0, 0, -0.1)), Vector((0, 0, cz + R * 0.9 - 0.02))], 0.013,
           "prop_steel_dark", sides=6)
    b.torus(Vector((0, 0, 0.0)), 0.035, 0.012, "prop_steel_dark", axis="y", seg=8, sides=5)
    o = b.finish(col, bevel=0.008, segments=1)
    # Only the sharp (90 degree) edges take the bevel: the ribbon edges and the rim, not the pillow.
    o.modifiers["Bevel"].angle_limit = math.radians(60)
    return o


def proto_bamboo(col):
    """A bamboo fiesta pole in an old tyre filled with concrete. Node bulges every 0.4 m of local z,
    where the texture draws its rings."""
    b = PBuf("life_bamboo_pole", top=BAMBOO_H)
    prof = [(bamboo_r(0) * 1.02, -0.30)]
    z = 0.4
    while z < BAMBOO_H - 0.05:
        prof.append((bamboo_r(z - 0.05), z - 0.05))
        prof.append((bamboo_r(z) * 1.11, z + 0.01))
        z += 0.4
    prof.append((bamboo_r(BAMBOO_H), BAMBOO_H))
    b.lathe(Vector((0, 0, 0)), [(r, zz) for r, zz in prof], "life_bamboo", sides=12)
    b.torus(Vector((0, 0, 0.1)), 0.30, 0.11, "prop_rubber", axis="z", seg=18, sides=12)
    b.lathe(Vector((0, 0, 0)), [(0.30, -0.01), (0.30, 0.13), (0.27, 0.165)], "prop_concrete", sides=20)
    return b.finish(col, bevel=0.01)


def proto_pedicab(col):
    """A pedicab, local +X forward: the bicycle on +Y, the roofed sidecar on -Y."""
    b = PBuf("life_pedicab", top=1.75)
    yb = 0.42

    def wheel(cx, cy, cz, R):
        b.torus(Vector((cx, cy, cz)), R - 0.045, 0.05, "prop_rubber", axis="y", seg=22, sides=8)
        disc = [(math.cos(a) * (R - 0.07), math.sin(a) * (R - 0.07)) for a in (i / 18 * math.tau for i in range(18))]
        b.loft([[Vector((cx + x, cy + y, cz + z)) for x, z in disc] for y in (-0.022, 0.022)], "prop_plastic_cream")
        b.blob(Vector((cx, cy, cz)), (0.05, 0.06, 0.05), "prop_steel_dark")

    wheel(-0.58, yb, 0.33, 0.33)
    wheel(0.60, yb, 0.33, 0.33)
    wheel(-0.10, -0.98, 0.29, 0.29)
    fr = "prop_steel_dark"
    for path in ([(-0.58, yb, 0.33), (-0.12, yb, 0.36), (0.42, yb, 0.86)],
                 [(-0.12, yb, 0.36), (-0.24, yb, 0.96)],
                 [(-0.58, yb, 0.33), (-0.22, yb, 0.84), (0.42, yb, 0.90)],
                 [(0.44, yb, 1.02), (0.47, yb, 0.80), (0.60, yb, 0.33)],
                 [(0.44, yb, 1.02), (0.40, yb, 1.10)],
                 [(0.40, yb - 0.26, 1.10), (0.40, yb + 0.26, 1.10)],
                 [(-0.12, yb, 0.36), (-0.12, -0.10, 0.36)],
                 [(0.42, yb, 0.86), (0.35, -0.10, 0.50)],
                 [(-0.10, -0.98, 0.29), (-0.10, -0.60, 0.33)]):
        b.tube([Vector(p) for p in path], 0.03, fr, sides=8)
    b.blob(Vector((-0.24, yb, 0.99)), (0.14, 0.08, 0.05), "prop_plastic_maroon")
    for s in (-1, 1):
        b.blob(Vector((0.40, yb + s * 0.27, 1.10)), (0.03, 0.06, 0.03), "prop_rubber")
    # The sidecar tub: a side profile swept across y, rounded; a cushion seat and backrest.
    side = fillet([(-0.78, 0.26), (0.46, 0.26), (0.64, 0.44), (0.64, 0.74), (0.46, 0.82), (0.12, 0.82),
                   (0.06, 0.62), (-0.52, 0.62), (-0.60, 1.08), (-0.78, 1.08)], 0.05, 2)
    b.extrude_y(side, -1.10, -0.16, "life_pedicab_body")
    b.rbox(Vector((-0.23, -0.63, 0.66)), (0.56, 0.84, 0.10), "prop_plastic_maroon", r=0.04)
    b.rbox(Vector((-0.55, -0.63, 0.88)), (0.08, 0.80, 0.36), "prop_plastic_maroon", r=0.03)
    b.panel(frame(Vector((1, 0, 0)), UP, Vector((0, -1, 0)), (-0.14, -1.108, 0.50)), 0.9, 0.36, 0.014,
            "life_pedicab_body", "life_sign_pedicab", r=0.02)
    # The roof: four posts and a sagging tarp roof.
    for x in (-0.74, 0.40):
        for y in (-1.04, -0.22):
            b.tube([Vector((x, y, 0.70 if x > 0 else 1.0)), Vector((x, y, 1.68))], 0.022, fr, sides=6)
    rows, cols = 5, 7
    grid = []
    for i in range(rows):
        f = i / (rows - 1)
        row = []
        for j in range(cols):
            g = j / (cols - 1)
            row.append(Vector((-0.86 + 1.40 * f, -1.16 + 1.06 * g, 1.72 - 0.03 * math.sin(math.pi * g) + 0.04 * f)))
        grid.append(row)
    b.sheet(grid, 0.02, lambda i, j: "prop_tarp_maroon")
    return b.finish(col, bevel=0.01, segments=1)


POTS = ["pot_santan_clay", "pot_lily_can", "pot_shrub_bucket", "pot_bloom_clay", "pot_trough"]
# Half the room each pot takes along a row, and how far its centre stands off a wall (the plant's
# reach, so no leaf goes into the wall).
POT_HALF = {"pot_santan_clay": 0.25, "pot_lily_can": 0.27, "pot_shrub_bucket": 0.27, "pot_bloom_clay": 0.32,
            "pot_trough": 0.42}
POT_REACH = {"pot_santan_clay": 0.33, "pot_lily_can": 0.38, "pot_shrub_bucket": 0.33, "pot_bloom_clay": 0.5,
             "pot_trough": 0.24}


def proto_pots(parent):
    """Five potted plants at the origin, each in its own collection (the pot, then the plant)."""
    protos = {}

    def pot(name, prof, mat, soil):
        b = PBuf(name + "_pot", top=prof[-3][1])
        b.lathe(Vector((0, 0, 0)), prof, mat, sides=18,
                mat_of=lambda f: "life_soil" if f.normal.z > 0.9 and abs(f.calc_center_median().z - soil) < 0.004 else mat)
        return b

    for k, name in enumerate(POTS):
        c = bpy.data.collections.new(f"streetlife {name} (prototype)")
        parent.children.link(c)
        rng = random.Random(500 + k)
        if name == "pot_santan_clay":
            soil = 0.30
            b = pot(name, [(0.125, -0.01), (0.13, 0.0), (0.1333, 0.03), (0.175, 0.28), (0.2, 0.29), (0.205, 0.335), (0.19, 0.345),
                           (0.172, 0.335), (0.165, soil)], "life_clay", soil)
            b.finish(c, bevel=0.008)
            # A potted santan: the trees kit's shrub leaf at pot scale and crimson flower-ball cards.
            m = T.Mesh()
            lobes = [(Vector((0, 0, soil + 0.2)), (0.2, 0.2, 0.18)),
                     (Vector((0.12, 0.06, soil + 0.14)), (0.13, 0.13, 0.12)),
                     (Vector((-0.1, -0.09, soil + 0.15)), (0.13, 0.13, 0.12))]
            for cc, rr in lobes:
                T.foliage(m, "shrub", cc, rr, 0.1, rng, lobes=lobes, floor_z=soil + 0.005, cover=2.6)
            fm = T.card_material("tree_flower_ixora", "tree_ixora", (0.62, 0.05, 0.12), gain=1.25)
            for i in range(9):
                cc, rr = lobes[i % 3]
                a = i * 2.4 + rng.uniform(-0.3, 0.3)
                d = Vector((math.cos(a) * 0.7, math.sin(a) * 0.7, 0.7)).normalized()
                p = cc + Vector((d.x * rr[0], d.y * rr[1], d.z * rr[2]))
                face = (d + Vector((0, 0, 0.5))).normalized()
                tip = face.cross(Vector((1, 0, 0))).cross(face).normalized()
                T.flat_card(m, fm, p - tip * 0.06, tip, face, 0.12, 0.12, cc - Vector((0, 0, 0.2)))
            T.obj(c, name + "_plant", m.build(name + "_plant"))
        elif name == "pot_lily_can":
            soil = 0.17
            b = pot(name, [(0.105, -0.01), (0.11, 0.0), (0.11, 0.03), (0.11, 0.19), (0.118, 0.2), (0.112, 0.21), (0.1, 0.2),
                           (0.1, soil)], "life_tin_green", soil)
            b.finish(c, bevel=0.005)
            m = T.Mesh()
            T.lily_clump(m, rng, at=(0, 0, soil), scale=0.5, flowers=0)
            T.obj(c, name + "_plant", m.build(name + "_plant"))
        elif name == "pot_shrub_bucket":
            soil = 0.28
            b = pot(name, [(0.12, -0.01), (0.125, 0.0), (0.1287, 0.03), (0.16, 0.30), (0.172, 0.31), (0.168, 0.325), (0.152, 0.31),
                           (0.148, soil)], "prop_plastic_white", soil)
            b.finish(c, bevel=0.005)
            m = T.Mesh()
            T.foliage(m, "shrub", (0, 0, soil + 0.2), (0.24, 0.24, 0.2), 0.12, rng, floor_z=soil + 0.005)
            T.obj(c, name + "_plant", m.build(name + "_plant"))
        elif name == "pot_bloom_clay":
            soil = 0.26
            b = pot(name, [(0.11, -0.01), (0.115, 0.0), (0.1206, 0.03), (0.16, 0.24), (0.182, 0.25), (0.186, 0.29), (0.172, 0.3),
                           (0.155, 0.29), (0.15, soil)], "life_clay", soil)
            b.finish(c, bevel=0.008)
            m = T.Mesh()
            T.lily_clump(m, rng, at=(0, 0, soil), scale=0.62, flowers=2)
            T.obj(c, name + "_plant", m.build(name + "_plant"))
        else:
            soil = 0.17
            b = PBuf(name + "_pot", top=0.215)
            rings = []
            for hw, hd, z in ((0.34, 0.12, -0.01), (0.343, 0.123, 0.03), (0.36, 0.14, 0.2), (0.37, 0.15, 0.21), (0.35, 0.13, 0.215),
                              (0.34, 0.12, soil)):
                rings.append([Vector((x, y, z)) for x, y in rounded_rect(hw, hd, 0.05)])
            b.loft(rings, "life_tin_red",
                   mat_of=lambda f: "life_soil" if f.normal.z > 0.9 and abs(f.calc_center_median().z - soil) < 0.004
                   else "life_tin_red")
            b.finish(c, bevel=0.006)
            m = T.Mesh()
            lobes = [(Vector((x, 0, soil + 0.12)), (0.15, 0.13, 0.13)) for x in (-0.22, 0.0, 0.22)]
            for cc, rr in lobes:
                T.foliage(m, "hedge", cc, rr, 0.1, rng, lobes=lobes, floor_z=soil + 0.005)
            T.obj(c, name + "_plant", m.build(name + "_plant"))
        protos[name] = c
    return protos


def place_copy(proto_col, target, loc, yaw=0.0, tilt=(0.0, 0.0), tag=""):
    out = []
    for o in proto_col.objects:
        c = o.copy()
        c.name = (o.name + tag) if tag else o.name
        base = Matrix.Translation(Vector(loc)) @ rz(yaw) @ Matrix.Rotation(tilt[0], 4, "X") @ \
            Matrix.Rotation(tilt[1], 4, "Y")
        c.matrix_world = base @ o.matrix_basis
        target.objects.link(c)
        out.append(c)
    return out


# ------------------------------------------------------------------ the placements

class Kit:
    def __init__(self, ctx, placed, protos_parent):
        self.ctx, self.placed, self.pp = ctx, placed, protos_parent
        self.rng = random.Random(2026)
        self.cols = {}
        self.parol_protos = []
        self.report = []

    def col(self, name):
        if name not in self.cols:
            c = bpy.data.collections.new(name)
            self.placed.children.link(c)
            self.cols[name] = c
        return self.cols[name]

    # -- prototypes -------------------------------------------------------------------------------

    def build_protos(self):
        for k in range(4):
            c = bpy.data.collections.new(f"streetlife parol_{k} (prototype)")
            self.pp.children.link(c)
            proto_parol(c, k)
            self.parol_protos.append(c)
        self.bamboo = bpy.data.collections.new("streetlife bamboo pole (prototype)")
        self.pp.children.link(self.bamboo)
        proto_bamboo(self.bamboo)
        self.pedicab = bpy.data.collections.new("streetlife pedicab (prototype)")
        self.pp.children.link(self.pedicab)
        proto_pedicab(self.pedicab)
        self.pots = proto_pots(self.pp)
        emissive_ready("life_parols", 1.5)

    # -- bamboo poles -------------------------------------------------------------------------------

    def bamboo_pole(self, x, y, tag):
        z, what = self.ctx.floor(x, y)
        lean = (math.radians(self.rng.uniform(-1.5, 1.5)), math.radians(self.rng.uniform(-1.5, 1.5)))
        yaw = self.rng.uniform(0, math.tau)
        place_copy(self.bamboo, self.col("streetlife bamboo poles"), (x, y, z - 0.01), yaw, lean, tag=f" {tag}")
        axis = rz(yaw).to_3x3() @ Matrix.Rotation(lean[0], 3, "X") @ Matrix.Rotation(lean[1], 3, "Y") @ UP
        pole = Pole(Vector((x, y, z - 0.01)), axis, "bamboo", name=f"bamboo {tag}")
        pole.floor = what
        return pole

    # -- bunting ------------------------------------------------------------------------------------

    def spans(self):
        ctx = self.ctx
        col = self.col("streetlife banderitas")
        P_ = ctx.pole_at
        b = {}
        # East arm: the fig canopies swallow the east arm's own poles at x 40 and 54, so bamboo poles
        # stand in the canopy gaps, on the lot just behind each pavement.
        for tag, x, side in (("EN27", 27.0, "n"), ("EN37", 37.2, "n"), ("EN47", 47.3, "n"), ("EN60", 59.6, "n"),
                             ("ES37", 36.8, "s"), ("ES47", 47.0, "s"), ("ES58", 58.3, "s")):
            ys, yn = ctx.road_edges(x)
            if side == "n":
                y = yn + 0.4
                while ctx.ground_z(x, y) < 0.23 and y < yn + 6:
                    y += 0.05
                b[tag] = self.bamboo_pole(x, y + 0.45, tag)
            else:
                # On the south side the pole line's cables run along the back of the pavement, so the
                # bamboo stands at its FRONT, 0.45 m behind the kerb, and the rope leaves the cables.
                b[tag] = self.bamboo_pole(x, ys - 0.35 - 0.45, tag)
        # The service drive east of Taft, south of the court (y -37..-39.5), clear of the Vista GL's
        # upper floors and air-conditioners, which overhang the drive's south edge from 4.5 m up.
        b["DR_n"] = self.bamboo_pole(12.45, -36.95, "DR_n")
        b["DR_s"] = self.bamboo_pole(12.45, -39.5, "DR_s")
        self.bamboos = b
        west = {k: P_(*xy) for k, xy in (("WS26", (-26.0, 25.8)), ("WN26", (-26.0, 38.7)), ("WS42", (-42.0, 26.3)),
                                         ("WN44", (-44.0, 39.3)), ("WS59", (-58.7, 26.9)), ("WN62", (-62.0, 39.9)),
                                         ("SWC", (-10.9, 24.6)))}
        self.west = west
        es = P_(26.0, 23.7)
        self.es26 = es
        # West arm. Not WS26 to WN26 straight across: the fig at (-28, 38) swallows that pole's top.
        plan = [
            (west["WS42"], west["WN44"]), (west["WS59"], west["WN62"]),
            (west["WS26"], west["WN44"]), (west["WS42"], west["WN62"]), (west["SWC"], west["WS26"]),
            (es, b["EN27"]), (b["ES37"], b["EN37"]), (b["ES47"], b["EN47"]), (b["ES58"], b["EN60"]),
            (es, b["EN37"]), (b["ES37"], b["EN47"]), (b["ES47"], b["EN60"]),
            (b["DR_n"], b["DR_s"]),
        ]
        self.spans_built = []
        for k, (pa, pb) in enumerate(plan):
            za = TIE_STREET if pa.kind == "street" else TIE_BAMBOO
            zb = TIE_STREET if pb.kind == "street" else TIE_BAMBOO
            sag = 0.35 if (pa, pb) == (b["DR_n"], b["DR_s"]) else None
            if (pa, pb) == (west["SWC"], west["WS26"]):
                # Along the south pavement, under the pole line's own cables (they droop to 4.8).
                za = zb = 4.3
                sag = 0.3
            obj, low = bunting(col, f"life_banderitas_{k:02d} {pa.name}-{pb.name}", pa, za, pb, zb, self.rng, sag)
            self.spans_built.append((obj, pa.name, pb.name, low))

    # -- parols -------------------------------------------------------------------------------------

    def hang_parol(self, hook, normal, k, tag):
        n = horiz(normal)
        yaw = math.atan2(n.x, -n.y) + math.radians(self.rng.uniform(-8, 8))
        tilt = (math.radians(self.rng.uniform(-2.5, 2.5)), 0.0)
        place_copy(self.parol_protos[k % 4], self.col("streetlife parols"), hook, yaw, tilt, tag=f" {tag}")

    def parols(self):
        ctx = self.ctx
        col = self.col("streetlife parols")
        count = 0
        k = 0
        # Taft poles outside the play area, facing the court; arms toward the street. Not the two
        # east poles at y +/-17.3: the shop row's awnings (to y +/-24) sit under their arms.
        b = PBuf("life_parol_brackets taft")
        for pole in sorted(ctx.poles, key=lambda p: (p.base.x, p.base.y)):
            x, y = pole.base.x, pole.base.y
            if abs(abs(x) - 10.6) < 0.5 and 17.0 < abs(y) < 70.0 and not (x > 0 and abs(y) < 24.0):
                out = Vector((-math.copysign(1, x), 0, 0))
                hook = bracket(b, pole, 4.95, out, reach=0.6)
                self.hang_parol(hook, Vector((0, -math.copysign(1, y), 0)), k, pole.name)
                k, count = k + 1, count + 1
        b.finish(col, bevel=0.006, segments=1).modifiers["Bevel"].angle_limit = math.radians(60)
        # Padre Faura poles: arms over the street (the pole's crossarm side), faces toward Taft. Not
        # WN26, whose street side is inside the fig at (-28, 38).
        b = PBuf("life_parol_brackets faura")
        for key in ("WS26", "WS42", "WN44", "WS59", "WN62"):
            pole = self.west[key]
            hook = bracket(b, pole, 4.95, pole.out, reach=0.6)
            self.hang_parol(hook, Vector((1, 0, 0)), k, pole.name)
            k, count = k + 1, count + 1
        hook = bracket(b, self.es26, 4.95, self.es26.out, reach=0.6)
        self.hang_parol(hook, Vector((-1, 0, 0)), k, self.es26.name)
        k, count = k + 1, count + 1
        b.finish(col, bevel=0.006, segments=1).modifiers["Bevel"].angle_limit = math.radians(60)
        # Bamboo poles on the east arm: a parol near the top, above the rope's tie (6.2), between the
        # nodes at 6.8 and 7.2: on the lot side of the north poles, and toward Taft on the south
        # poles (their lot side has the pole line's cables).
        b = PBuf("life_parol_brackets bamboo")
        for tag in ("EN27", "EN47", "ES37", "ES58"):
            pole = self.bamboos[tag]
            out = Vector((0, 1, 0)) if tag.startswith("EN") else Vector((-1, 0, 0))
            hook = bracket(b, pole, 6.95, out, reach=0.7)
            self.hang_parol(hook, Vector((-1, 0, 0)), k, pole.name)
            k, count = k + 1, count + 1
        b.finish(col, bevel=0.006, segments=1).modifiers["Bevel"].angle_limit = math.radians(60)
        # Building fronts: the Astral podium's Padre Faura face, only where a ray finds the wall. The
        # bracket plates sink 1 cm into the wall on purpose. (The east row's upper floor south of the
        # court was tried and dropped: the West East Center's slabs overhang it.)
        b = PBuf("life_parol_brackets walls")
        for (o, d, want) in (((26.6, 22.9, 4.6), (0, -1, 0), ("east_astral",)),
                             ((37.0, 22.9, 4.6), (0, -1, 0), ("east_astral",)),
                             ((46.6, 22.9, 4.6), (0, -1, 0), ("east_astral",))):
            h = ctx.ray(Vector(o), Vector(d), 6.0)
            if not h or not h[2].startswith(want):
                self.report.append(f"parol at {o}: no {want} wall found ({h[2] if h else 'nothing'}), skipped")
                continue
            hook = wall_bracket(b, h[0], h[1])
            self.hang_parol(hook, h[1], k, f"wall {o[0]:.0f},{o[1]:.0f}")
            k, count = k + 1, count + 1
        b.finish(col, bevel=0.006, segments=1).modifiers["Bevel"].angle_limit = math.radians(60)
        self.report.append(f"parols hung: {count}")

    # -- vehicles -----------------------------------------------------------------------------------

    def vehicles(self):
        ctx = self.ctx
        with bpy.data.libraries.load(str(SOURCE / "vehicles.blend"), link=True) as (src, dst):
            dst.collections = [n for n in src.collections if n.startswith("veh_")]
        self.veh = {c.name: c for c in dst.collections if c is not None}
        self.veh_box = {}
        for name, c in self.veh.items():
            pts = [o.matrix_world @ Vector(v) for o in c.all_objects if o.type == "MESH" for v in o.bound_box]
            self.veh_box[name] = (Vector([min(p[i] for p in pts) for i in range(3)]),
                                  Vector([max(p[i] for p in pts) for i in range(3)]))
        col = self.col("streetlife parked vehicles")
        self.parked = []

        def park(name, x, y, heading, floor=None):
            z = floor if floor is not None else ctx.floor(x, y)[0]
            e = bpy.data.objects.new(f"{name} parked @ {x:.0f},{y:.0f}", None)
            e.instance_type = "COLLECTION"
            e.instance_collection = self.veh[name]
            e.location = (x, y, z)
            e.rotation_euler = (0, 0, heading + math.radians(self.rng.uniform(-1.5, 1.5)))
            col.objects.link(e)
            self.parked.append((e, name))

        west = math.pi
        # West arm, NORTH kerb only (the city's moving tricycle uses the south kerb lane).
        # The narrow cars near the corner: the city's sedan runs the middle lane at y 33 (to 34.1).
        for name, x in (("veh_hatch_maroon", -30.2), ("veh_taxi", -37.4), ("veh_jeepney_green", -48.6),
                        ("veh_uv_express", -55.2), ("veh_sedan_grey", -63.0)):
            ys, yn = ctx.road_edges(x)
            hw = self.veh_box[name][1].y
            park(name, x, yn - hw - 0.3, west)
        # East arm: both kerb lanes, clear of the city's hatchback at (26, 30).
        for name, x, side in (("veh_tricycle", 20.6, "s"), ("veh_tricycle", 22.9, "s"), ("veh_sedan_grey", 32.6, "s"),
                              ("veh_jeepney_taft", 44.2, "s"), ("veh_uv_express", 56.8, "s"),
                              ("veh_uv_express", 31.2, "n"), ("veh_hatch_maroon", 39.4, "n"),
                              ("veh_sedan_grey", 50.2, "n"), ("veh_taxi", 62.6, "n")):
            ys, yn = ctx.road_edges(x)
            lo, hi = self.veh_box[name][0].y, self.veh_box[name][1].y
            gap = 0.3
            # Heading west turns the box: local +y becomes world -y.
            y = ys + hi + gap if side == "s" else yn + lo - gap
            park(name, x, y, west)
        # PGH parking behind the campus fence, noses to the fence, seen through the pickets.
        for name, y in (("veh_sedan_grey", -6.4), ("veh_taxi", -3.4), ("veh_uv_express", 2.8)):
            park(name, -16.6, y, 0.0)
        # The service drive east of Taft: a tricycle waiting by the TODA board.
        park("veh_tricycle", 14.3, -38.9, west)
        # Pedicabs: one in the tricycle line on the east arm, one on the service drive.
        for (x, y, h, tag) in ((18.2, None, west, "faura"), (16.6, -40.35, west, "drive")):
            if y is None:
                ys, yn = ctx.road_edges(x)
                y = ys + 0.62 + 0.3
            z = ctx.floor(x, y)[0]
            objs = place_copy(self.pedicab, self.col("streetlife pedicabs"), (x, y, z - 0.01), h, tag=f" {tag}")
            self.parked.append((objs[0], "pedicab"))

    # -- tarps and notices --------------------------------------------------------------------------

    def tarp(self, name, tex, centre, normal, w, h, back):
        """A tarpaulin sheet hanging `back` metres in front of what it is tied to, and its rope ties
        (a separate object, "<name> ties", which sinks into the rails or the wall on purpose)."""
        n = horiz(normal)
        right = UP.cross(n)
        b = PBuf(name, top=centre.z + h / 2)
        rows, cols = 5, 9
        grid = []
        for i in range(rows):
            row = []
            for j in range(cols):
                u = -w / 2 + w * j / (cols - 1)
                v = -h / 2 + h * i / (rows - 1)
                bill = 0.022 * math.sin(math.pi * j / (cols - 1)) * math.sin(math.pi * i / (rows - 1))
                row.append(centre + right * u + UP * v + n * bill)
            grid.append(row)
        b.sheet(grid, 0.008, lambda i, j: "life_tarp_back", normal=n,
                uv_fn=lambda i, j: (j / (cols - 1), i / (rows - 1)), decal=tex)
        col = self.col("streetlife tarps and notices")
        origin = (centre.x, centre.y, centre.z - h / 2)
        b.finish(col, origin=origin, bevel=0.004, segments=1)
        t = PBuf(name + " ties")
        for u in (-w / 2 + 0.045, 0.0, w / 2 - 0.045):
            for v in (-h / 2 + 0.045, h / 2 - 0.045):
                p = centre + right * u + UP * v - n * 0.004
                t.tube([p + n * 0.02, p - n * (back * 0.5) + UP * 0.03, p - n * (back + 0.03)], 0.016, "life_rope",
                       sides=6)
        t.finish(col, origin=origin, bevel=0.0)

    def tarps(self):
        ctx = self.ctx
        # The PGH fence along Taft (rails at x about -11.2), facing the court, north and south of it.
        for name, tex, want, w, h, lo, hi in (("life_tarp_birthday", "life_tarp_birthday", 20.3, 2.4, 1.2, 17.6, 23.5),
                                              ("life_tarp_rabies", "life_tarp_rabies", -21.0, 2.0, 1.2, -26.0, -17.2)):
            g = ctx.fence_gap((-9.2, 0, 0), (0, 1, 0), (-1, 0, 0), 0.84, lo, hi, want, w)
            if g is None:
                self.report.append(f"{name}: no fence gap found")
                continue
            c, n = g
            c.z = ctx.floor(-10.6, c.y)[0] + 0.72 + h / 2
            self.tarp(name, tex, c, n, w, h, 0.045)
        # The PGH fence along Padre Faura, facing the street (north).
        g = ctx.fence_gap((0, 27.2, 0), (1, 0, 0), (0, -1, 0), 0.84, -42.0, -34.0, -37.6, 2.4)
        if g:
            c, n = g
            c.z = 0.24 + 0.78 + 0.4
            self.tarp("life_tarp_fiesta", "life_tarp_fiesta", c, n, 2.4, 0.8, 0.045)
        else:
            self.report.append("life_tarp_fiesta: no fence gap found")
        # The Supreme Court's white fence, north side of Padre Faura, facing the street (south).
        x0 = -37.0
        ys, yn = ctx.road_edges(x0)
        g = ctx.fence_gap((0, yn + 1.3, 0), (1, 0, 0), (0, 1, 0), 0.9, -41.0, -33.0, x0, 2.0)
        if g:
            c, n = g
            c.z = 0.24 + 0.5 + 0.6          # under the arched tops of the white pickets
            self.tarp("life_tarp_grad", "life_tarp_grad", c, n, 2.0, 1.2, 0.045)
        else:
            self.report.append("life_tarp_grad: no fence gap found")
        # The councillor's Christmas greeting on the Astral podium's Padre Faura wall.
        h = ctx.ray(Vector((45.3, 23.0, 2.6)), Vector((0, -1, 0)), 5.0)
        if h and h[2].startswith("east_astral"):
            c = h[0] + horiz(h[1]) * 0.05
            c.z = ctx.floor(45.3, h[0].y + 0.5)[0] + 2.1 + 0.5
            self.tarp("life_tarp_pasko", "life_tarp_pasko", c, h[1], 2.4, 1.0, 0.05)
        else:
            self.report.append(f"life_tarp_pasko: no podium wall ({h[2] if h else 'nothing'})")

    def notices(self):
        ctx = self.ctx
        b = PBuf("life_notices")
        board_on_pole(b, self.es26, 2.0, Vector((-0.8, 0.45, 0)), 0.6, 0.9, "life_sign_bedspace", 61)
        board_on_pole(b, self.west["WS42"], 2.2, Vector((0.25, 1, 0)), 0.9, 0.6, "life_sign_dahan", 62, t=0.02,
                      mat="prop_steel_dark")
        board_on_pole(b, self.west["WN26"], 2.1, Vector((0.3, -1, 0)), 0.9, 0.45, "life_sign_videoke", 63)
        board_on_pole(b, self.bamboos["DR_n"], 1.9, Vector((-1, 0, 0)), 0.9, 0.5, "life_sign_toda", 64)
        b.finish(self.col("streetlife tarps and notices"), bevel=0.006, segments=1)

    # -- potted plants ------------------------------------------------------------------------------

    def cluster(self, anchor, along, wall, names, tag):
        """A row of pots from `anchor` along `along`. With `wall` (a direction), each pot finds the
        wall with its own ray and stands its plant's reach off it. A pot whose footprint would touch
        anything is nudged along the row, and dropped (and reported) if it still does."""
        ctx = self.ctx
        along = horiz(along)
        s = 0.0
        for k, name in enumerate(names):
            half = POT_HALF[name]
            s += half
            placed = False
            for tries in range(4):
                q = Vector((anchor[0], anchor[1], 0)) + along * s
                z, _w = ctx.floor(q.x, q.y)
                if wall is not None:
                    wd = horiz(wall)
                    h = ctx.ray(Vector((q.x, q.y, z + 0.5)) - wd * 1.2, wd, 2.5)
                    if h:
                        q = h[0] - wd * POT_REACH[name]
                        q.z = 0.0
                        z, _w = ctx.floor(q.x, q.y)
                touch = ctx.touching(q, (POT_REACH[name] * 0.85, half * 0.9), z + 0.06, z + 0.75,
                                     math.atan2(along.y, along.x))
                if not touch:
                    placed = True
                    break
                s += 0.25
            if not placed:
                self.report.append(f"pot {tag}{k} ({name}) dropped: touches {touch}")
                s += half
                continue
            yaw = math.atan2(along.y, along.x) if name == "pot_trough" else self.rng.uniform(0, math.tau)
            place_copy(self.pots[name], self.col("streetlife potted plants"), (q.x, q.y, z - 0.01), yaw,
                       tag=f" {tag}{k}")
            s += half + self.rng.uniform(0.02, 0.08)

    def plants(self):
        ctx = self.ctx
        # By the shop doors at the south end of the east row, on its step against the facade.
        self.cluster((11.3, -17.0), Vector((0, -1, 0)), Vector((1, 0, 0)),
                     ["pot_santan_clay", "pot_bloom_clay", "pot_shrub_bucket"], "shopA")   # the lily's leaves
                                                                                  # reached the shopfront
        self.cluster((11.3, -19.6), Vector((0, -1, 0)), Vector((1, 0, 0)),
                     ["pot_bloom_clay", "pot_trough", "pot_santan_clay"], "shopB")
        # By the lugaw eatery on the Astral podium's Padre Faura face.
        self.cluster((18.4, 21.5), Vector((1, 0, 0)), Vector((0, -1, 0)), ["pot_shrub_bucket", "pot_santan_clay"], "lugawW")
        self.cluster((25.9, 21.5), Vector((1, 0, 0)), Vector((0, -1, 0)),
                     ["pot_bloom_clay", "pot_lily_can", "pot_santan_clay"], "lugawE")
        # Under the birthday tarp, against the PGH fence (outside the play area).
        self.cluster((-10.6, 21.9), Vector((0, 1, 0)), Vector((-1, 0, 0)),
                     ["pot_lily_can", "pot_santan_clay", "pot_trough"], "fence")
        # At the feet of two bamboo poles on the east arm's north lots.
        for tag, names in (("EN27", ["pot_bloom_clay", "pot_shrub_bucket"]), ("EN47", ["pot_santan_clay", "pot_lily_can"])):
            p = self.bamboos[tag].base
            self.cluster((p.x + 0.2, p.y + 0.55), Vector((1, 0, 0)), None, names, "b" + tag)

    # -- checks -------------------------------------------------------------------------------------

    def check(self):
        ctx = self.ctx
        out = self.report
        mine = [o for o in self.placed.all_objects if o.type == "MESH"]
        bad = []
        for o in mine:
            mw = o.matrix_world
            verts = [mw @ v.co for v in o.data.vertices]
            for v in verts[::3]:
                if abs(v.x) < PLAY_X and abs(v.y) < PLAY_Y:
                    bad.append(f"{o.name}: in the play area at ({v.x:.2f}, {v.y:.2f}, {v.z:.2f})")
                    break
                if abs(v.x) < TAFT_LANES_X:
                    bad.append(f"{o.name}: over Taft's lanes at ({v.x:.2f}, {v.y:.2f}, {v.z:.2f})")
                    break
                if LOT[0] < v.x < LOT[1] and LOT[2] < v.y < LOT[3]:
                    bad.append(f"{o.name}: in the sari-sari lot at ({v.x:.2f}, {v.y:.2f})")
                    break
        # Bunting over roads: every 7th vertex of every span, the road under it.
        for obj, a, b_, low in self.spans_built:
            mw = obj.matrix_world
            worst = None
            for v in list(obj.data.vertices)[::7]:
                w = mw @ v.co
                if ctx.ground_z(w.x, w.y) < 0.05 and (worst is None or w.z < worst):
                    worst = w.z
            flag = "" if worst is None or worst >= BUNTING_MIN_Z else "  <-- TOO LOW"
            out.append(f"span {obj.name}: rope low {low:.2f}, lowest over road {worst if worst is None else round(worst, 2)}{flag}")
        # Clashes: my meshes against the context meshes whose boxes they touch.
        boxes = [(c, ctx.bbox(c)) for c in ctx.meshes]
        expected = ("ground", "kerbs", "road markings", "street_pole")
        for o in mine:
            mw = o.matrix_world
            verts = [mw @ v.co for v in o.data.vertices]
            if not verts:
                continue
            lo = Vector([min(v[i] for v in verts) for i in range(3)]) - Vector((0.02,) * 3)
            hi = Vector([max(v[i] for v in verts) for i in range(3)]) + Vector((0.02,) * 3)
            # The lowest 3 cm of anything standing on a floor sinks into it on purpose.
            foot = lo.z + 0.02 + 0.045
            polys = [tuple(p.vertices) for p in o.data.polygons if max(verts[i].z for i in p.vertices) > foot]
            me = BVHTree.FromPolygons(verts, polys)
            hits = []
            for c, (clo, chi) in boxes:
                if clo.x > hi.x or chi.x < lo.x or clo.y > hi.y or chi.y < lo.y or clo.z > hi.z or chi.z < lo.z:
                    continue
                other = ctx.bvh(c)
                if other is None:
                    continue
                pairs = me.overlap(other)
                if pairs:
                    zs = [sum(verts[i].z for i in polys[a_]) / len(polys[a_]) - (lo.z + 0.02) for a_, _b in pairs]
                    hits.append((c.name, f"{len(pairs)} tris at +{min(zs):.2f}..+{max(zs):.2f} m"))
            odd = [(n_, k) for n_, k in hits if not n_.startswith(expected)]
            if odd and (o.name.endswith(" ties") or "brackets walls" in o.name):
                out.append(f"intended contact (ties into rails, plates into walls): {o.name}: {odd}")
            elif odd:
                bad.append(f"{o.name}: touches {odd}")
        # Vehicles: the footprint box (raised 6 cm off the floor) against the context and the rules.
        for e, name in self.parked:
            if name == "pedicab":
                continue
            lo, hi = self.veh_box[name]
            corners = [Vector((x, y, z)) for x in (lo.x, hi.x) for y in (lo.y, hi.y) for z in (lo.z + 0.06, hi.z)]
            mw = e.matrix_world
            pts = [mw @ c for c in corners]
            box = BVHTree.FromPolygons(pts, [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4),
                                             (1, 5, 7, 3)])
            blo = Vector([min(p[i] for p in pts) for i in range(3)])
            bhi = Vector([max(p[i] for p in pts) for i in range(3)])
            touch = []
            for c, (clo, chi) in boxes:
                if clo.x > bhi.x or chi.x < blo.x or clo.y > bhi.y or chi.y < blo.y or clo.z > bhi.z or chi.z < blo.z:
                    continue
                if c.name.startswith(("ground", "road markings")):
                    continue
                other = ctx.bvh(c)
                if other and box.overlap(other):
                    touch.append(c.name)
            if touch:
                bad.append(f"{e.name}: footprint touches {touch}")
            for cname, (cx, cy), ch in CITY_TRAFFIC:
                clo, chi = self.veh_box[cname]
                if abs(cx - e.location.x) < (chi.x - clo.x) / 2 + (bhi.x - blo.x) / 2 + 0.3 and \
                        abs(cy - e.location.y) < (chi.y - clo.y) / 2 + (bhi.y - blo.y) / 2 + 0.2:
                    bad.append(f"{e.name}: overlaps the city's moving {cname} at ({cx}, {cy})")
            if abs(e.location.x) < PLAY_X + 2 and abs(e.location.y) < PLAY_Y + 2:
                bad.append(f"{e.name}: near the play area")
            if LOT[0] - 2 < e.location.x < LOT[1] and LOT[2] - 1 < e.location.y < LOT[3]:
                bad.append(f"{e.name}: at the sari-sari lot")
            f, what = ctx.floor(e.location.x, e.location.y)
            out.append(f"parked {e.name}: floor {what} {f:.3f}, box y {blo.y:.2f}..{bhi.y:.2f}")
        out.append("CHECK: " + ("clean" if not bad else f"{len(bad)} problems"))
        out.extend("  ! " + s for s in bad)


# ------------------------------------------------------------------ budget, light, preview

def triangles(objs):
    dg = bpy.context.evaluated_depsgraph_get()
    total, per = 0, {}
    for o in objs:
        if o.type != "MESH":
            continue
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        me.calc_loop_triangles()
        n = len(me.loop_triangles)
        ev.to_mesh_clear()
        total += n
        key = o.users_collection[0].name if o.users_collection else "?"
        per[key] = per.get(key, 0) + n
    return total, per


def lighting():
    scene = bpy.context.scene
    world = bpy.data.worlds.new("sky")
    scene.world = world
    if world.node_tree is None:
        world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.60, 0.74, 0.92, 1)
    bg.inputs["Strength"].default_value = 0.7
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.angle, sun.data.color = 4.6, math.radians(2.5), (1.0, 0.86, 0.68)
    sun.rotation_euler = Vector((0.86, 0.22, -0.46)).normalized().to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(sun)
    scene.view_settings.view_transform = "AgX"
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        pass


SHOTS = [
    ("faura_west", (-17.0, 31.6, 1.7), (-60.0, 33.0, 5.0), 18),
    ("faura_east", (15.5, 29.0, 1.7), (60.0, 27.0, 5.0), 18),
    ("court_ne", (7.6, 14.0, 1.46), (25.0, 32.0, 4.0), 16.5),
    ("parol_taft", (-7.0, 11.0, 2.4), (-10.0, 17.4, 4.1), 24),
    ("tarp_birthday", (-5.5, 20.4, 1.6), (-11.0, 20.6, 1.4), 22),
    ("tarp_rabies", (-5.5, -21.0, 1.6), (-11.0, -21.0, 1.4), 22),
    ("pots_shop", (8.0, -21.8, 1.3), (11.4, -21.8, 0.5), 22),
    ("drive_toda", (6.0, -39.0, 1.7), (14.0, -39.0, 2.4), 22),
    ("pgh_parking", (-8.5, -2.0, 1.5), (-17.0, -2.0, 0.9), 22),
    ("aerial_faura", (0.0, -12.0, 38.0), (0.0, 32.0, 0.0), 22),
    ("podium", (30.5, 29.5, 1.7), (37.0, 20.5, 3.2), 16),
    ("tarp_grad", (-37.0, 33.0, 1.6), (-37.0, 40.5, 1.5), 22),
    ("tarp_fiesta", (-37.8, 30.5, 1.6), (-37.8, 25.0, 1.4), 22),
]


def preview(version, only=None):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 1000
    scene.collection.objects.link(cam)
    scene.camera = cam
    for name, pos, tgt, lens in SHOTS:
        if only and name not in only:
            continue
        path = PREVIEWS / f"life_{name}_v{version}.png"
        if path.exists():
            print("[ilalim-life] exists, skipped", path)
            continue
        pos, tgt = Vector(pos), Vector(tgt)
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("[ilalim-life] preview", path)
    bpy.data.objects.remove(cam)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    only = argv[argv.index("--only") + 1].split(",") if "--only" in argv else None
    bpy.ops.wm.read_factory_settings(use_empty=True)
    make_materials_local()
    ctx = Context()
    placed = bpy.data.collections.new("streetlife (placed)")
    protos = bpy.data.collections.new("streetlife (prototypes)")
    kit = Kit(ctx, placed, protos)
    kit.build_protos()
    kit.spans()
    kit.parols()
    kit.vehicles()
    kit.tarps()
    kit.notices()
    kit.plants()
    scene = bpy.context.scene
    scene.collection.children.link(placed)
    scene.collection.children.link(protos)
    protos.hide_render = True
    for o in protos.all_objects:
        o.hide_set(True) if o.name in bpy.context.view_layer.objects else None
    bpy.context.view_layer.update()
    if "--no-check" not in argv:
        kit.check()
    tri, per = triangles(list(placed.all_objects))
    ptri, _ = triangles(list(protos.all_objects))
    vtri = 0
    for e, name in kit.parked:
        if name != "pedicab":
            vtri += sum(len(p.vertices) - 2 for o in kit.veh[name].all_objects if o.type == "MESH"
                        for p in o.data.polygons)
    print("[ilalim-life] REPORT")
    for line in kit.report:
        print("   ", line)
    print(f"[ilalim-life] triangles: placed meshes {tri} (after bevel), prototypes {ptri}, parked vehicle instances {vtri} (before their bevels)")
    for k, v in sorted(per.items()):
        print(f"    {k}: {v}")
    lighting()
    if version:
        preview(version, only)
    ctx.close()
    lost = sorted({f"{o.name}[{k}]" for o in list(placed.all_objects) + list(protos.all_objects) if o.type == "MESH"
                   for k, m in enumerate(o.data.materials) if m is None or m.library is not None})
    print(f"[ilalim-life] material slots empty or linked after the context left: {lost if lost else 'none'}")
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "streetlife.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True, relative_remap=True)
    backup = SOURCE / "streetlife.blend1"
    if backup.exists():
        backup.unlink()
    print(f"[ilalim-life] saved {out}; {len(list(placed.all_objects))} placed objects")


if __name__ == "__main__":
    main()
