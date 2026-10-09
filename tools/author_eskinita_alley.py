"""ESKINITA ALLEY: the from-scratch rebuild of Eskinita as a tight, stepped Manila back alley (2026-10-09).

Owner, 2026-10-08: "rebuild from scratch"; "eskinita is a small/tight map to reflect the concept (alleyway)"; "i like
that idea that its a multi level / varied height map"; "climbable ladders or just jump pads to get up ... want something
unique, maintaining the same fun illustrated stylized look"; of three blockouts, "go with 2, fix what you see wrong";
"the planks are floating with no end"; then, leaving for four hours, "surprise me with something as good as arena and the
other maps". Layout 2 (the stepped alley) of tools/author_eskinita_blockout.py is the plan this file builds for real.

  blender -b --python tools/author_eskinita_alley.py -- [--review N] [--no-export] [--grey]

Writes ArtSource/eskinita/eskinita_alley.blend, Assets/TumbangPreso/Art/EskinitaAlley/Models/*.glb (the architecture in
a few big pieces and `collision.glb`), `eskinita_alley_layout.json` (Unity axes) and, with --review, pictures to
Logs/eskinita/alley_<camera>_vN.png. The props are another kit (tools/author_eskinita_props.py); they are PLACED here by
name (`PLACE`) and appended from ArtSource/eskinita/props.blend for the review pictures.

AXES. Blender x is across the alley, y along it (south is -y, the low end), z up. glTFast negates x, so a Blender point
(x, y, z) is Unity (-x, z, -y) and a yaw of t degrees is -t: the layout file is written in Unity's.

THE RULES OF THE LAYOUT (each one from a fault found in the grey blockout):
  * every house is solid and runs back to the outer bound (x 13.6), so there is no slot behind a house to fall into;
  * the game has no ladder to climb (its body is a CharacterController, step 0.3 m, slope 45 degrees), so every way up
    is a STAIR (collision: a ramp under 40 degrees) or a BOUNCE TARP (a JumpPad on a stretched trapal);
  * nothing bounces under a ledge, and no pad or stair stands inside the 14 m chalk box;
  * nothing hangs in the air: bridges rest a metre onto a roof at each end on a beam, lines hang between poles.
COLLISION IS ITS OWN FILE: plain boxes and ramps, exactly the walkable shape; the art is drawn to it and carries none.
"""
import json
import math
import os
import random
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parent.parent
ART = ROOT / "Assets/TumbangPreso/Art/EskinitaAlley"
MODELS, TEXTURES = ART / "Models", ART / "Textures"
SOURCE = ROOT / "ArtSource/eskinita"
LOGS = ROOT / "Logs/eskinita"
ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
REVIEW = int(ARGV[ARGV.index("--review") + 1]) if "--review" in ARGV else 0
EXPORT = "--no-export" not in ARGV
GREY = "--grey" in ARGV

STEP = 0.9
Z0, Z1, Z2 = 0.0, STEP, 2 * STEP
DOWN = Z1            # what every height is lowered by on the way out: the can's floor is the game's zero
MID = 8.5            # the can's terrace runs y -MID..MID
AX, AY = 8.0, 17.5   # the alley's half width and half length
OUT = 13.6           # the outer bound: every house runs back to it
# ⚠️ THE TWO END TERRACES ARE NARROW (v2, 2026-10-09). The blockout kept all 35 m at 16 m wide, and built for real it
# read as a plaza with houses round it, not an alley (owner: "eskinita is a small/tight map to reflect the concept
# (alleyway)"). Only the can's terrace has to hold the 14 m chalk box; the ends are 10 m between their walls, so the
# map is an alley that opens into one small court and closes again, and each plank bridge spans 10 m and not 18.
EX = 5.0


def front(y):
    return AX if -MID <= y <= MID else EX


# ------------------------------------------------------------------ materials (name: texture, tint, tiling)
# Tiling is 1 / the texture's tile in metres; UVs are metres. "" is a flat tint. Cards (windows, doors) use 0..1 UVs.
MAT = {
    "ek_floor": ("floor", (0.88, 0.85, 0.82), 0.25), "ek_wire": ("", (0.13, 0.12, 0.14), 1.0), "ek_piko": ("prop_piko", (1, 1, 1), 1.0), "ek_street": ("floor", (0.62, 0.60, 0.60), 0.25), "ek_asphalt": ("asphalt", (0.92, 0.90, 0.92), 0.25),
    "ek_roadpaint": ("", (0.93, 0.88, 0.66), 1.0),
    "ek_concrete": ("concrete", (0.86, 0.83, 0.78), 0.5), "ek_coping": ("concrete", (0.96, 0.92, 0.84), 0.5),
    "ek_deck": ("concrete", (0.80, 0.74, 0.66), 0.5), "ek_chb": ("chb", (1, 1, 1), 0.5),
    "ek_chb_warm": ("chb", (1.0, 0.90, 0.78), 0.5),
    "ek_wall_cream": ("plaster", (0.96, 0.88, 0.70), 0.333), "ek_wall_mint": ("plaster", (0.60, 0.82, 0.68), 0.333),
    "ek_wall_salmon": ("plaster", (0.95, 0.62, 0.54), 0.333), "ek_wall_butter": ("plaster", (0.98, 0.83, 0.42), 0.333),
    "ek_wall_lilac": ("plaster", (0.76, 0.67, 0.88), 0.333), "ek_wall_sage": ("plaster", (0.66, 0.76, 0.54), 0.333),
    "ek_wall_peach": ("plaster", (0.99, 0.76, 0.58), 0.333), "ek_wall_aqua": ("plaster", (0.42, 0.76, 0.68), 0.333),
    "ek_wall_rose": ("plaster", (0.88, 0.50, 0.54), 0.333), "ek_wall_white": ("plaster", (0.95, 0.93, 0.87), 0.333),
    "ek_band_green": ("plaster", (0.24, 0.50, 0.40), 0.333), "ek_band_red": ("plaster", (0.64, 0.25, 0.22), 0.333),
    "ek_band_brown": ("plaster", (0.52, 0.36, 0.26), 0.333), "ek_band_teal": ("plaster", (0.18, 0.46, 0.45), 0.333),
    "ek_band_mustard": ("plaster", (0.78, 0.58, 0.18), 0.333), "ek_band_plum": ("plaster", (0.46, 0.30, 0.46), 0.333),
    "ek_planks_brown": ("planks", (0.66, 0.46, 0.30), 0.5), "ek_planks_green": ("planks", (0.46, 0.66, 0.52), 0.5),
    "ek_planks_cream": ("planks", (0.95, 0.86, 0.68), 0.5), "ek_planks_red": ("planks", (0.72, 0.32, 0.26), 0.5),
    "ek_tin_grey": ("tin", (0.84, 0.86, 0.86), 0.5), "ek_tin_green": ("tin", (0.42, 0.66, 0.54), 0.5),
    "ek_tin_red": ("tin", (0.76, 0.34, 0.28), 0.5), "ek_tin_rust": ("tin_rust", (1, 1, 1), 0.5),
    "ek_tiles": ("tiles_clay", (1, 1, 1), 0.5), "ek_sawali": ("sawali", (1, 1, 1), 1.0),
    "ek_wood": ("wood", (0.74, 0.54, 0.36), 1.0), "ek_wood_dark": ("wood", (0.46, 0.31, 0.20), 1.0),
    "ek_pipe_green": ("paint", (0.20, 0.46, 0.36), 1.0), "ek_pipe_red": ("paint", (0.70, 0.22, 0.20), 1.0),
    "ek_pipe_cream": ("paint", (0.95, 0.90, 0.76), 1.0),
    "ek_riser_red": ("paint", (0.88, 0.30, 0.28), 1.0), "ek_riser_yellow": ("paint", (0.98, 0.80, 0.22), 1.0),
    "ek_riser_green": ("paint", (0.34, 0.70, 0.42), 1.0), "ek_riser_teal": ("paint", (0.16, 0.64, 0.60), 1.0),
    "ek_riser_pink": ("paint", (0.95, 0.56, 0.68), 1.0), "ek_riser_purple": ("paint", (0.58, 0.42, 0.76), 1.0),
    "ek_trapal_teal": ("trapal_teal", (1, 1, 1), 1.0), "ek_trapal_red": ("trapal_red", (1, 1, 1), 1.0),
    "ek_trapal_yellow": ("trapal_yellow", (1, 1, 1), 1.0),
    "ek_jalousie": ("jalousie", (1, 1, 1), 1.0), "ek_slide": ("window_slide", (1, 1, 1), 1.0),
    "ek_door_green": ("door_wood", (0.42, 0.68, 0.56), 1.0), "ek_door_brown": ("door_wood", (0.70, 0.48, 0.32), 1.0),
    "ek_door_red": ("door_wood", (0.80, 0.34, 0.30), 1.0), "ek_door_cream": ("door_wood", (0.96, 0.88, 0.70), 1.0),
    "ek_gate_green": ("gate", (0.30, 0.56, 0.46), 1.0), "ek_gate_red": ("gate", (0.72, 0.28, 0.24), 1.0),
    "ek_gate_cream": ("gate", (0.94, 0.88, 0.72), 1.0),
    "ek_frame_white": ("paint", (0.96, 0.94, 0.88), 1.0), "ek_frame_dark": ("paint", (0.24, 0.20, 0.18), 1.0),
    "ek_frame_green": ("paint", (0.22, 0.44, 0.34), 1.0),
    "ek_grille": ("grille", (0.24, 0.22, 0.20), 1.0), "ek_grille_white": ("grille", (0.96, 0.94, 0.88), 1.0),
    "ek_chalk": ("", (0.98, 0.98, 0.95), 1.0), "ek_rope": ("", (0.80, 0.72, 0.52), 1.0),
}
CUTOUT = {"ek_grille", "ek_grille_white", "ek_piko"}
WALLS = ["ek_wall_cream", "ek_wall_mint", "ek_wall_salmon", "ek_wall_butter", "ek_wall_lilac", "ek_wall_sage", "ek_wall_peach",
         "ek_wall_aqua", "ek_wall_rose", "ek_wall_white"]
BANDS = ["ek_band_green", "ek_band_red", "ek_band_brown", "ek_band_teal", "ek_band_mustard", "ek_band_plum"]
UPPER = ["ek_planks_brown", "ek_planks_green", "ek_planks_cream", "ek_planks_red", "ek_tin_grey", "ek_tin_green", "ek_sawali"]
ROOFS = ["ek_tin_rust", "ek_tin_grey", "ek_tin_green", "ek_tin_red"]
RISERS = ["ek_riser_red", "ek_riser_yellow", "ek_riser_green", "ek_riser_teal", "ek_riser_pink", "ek_riser_purple"]
DOORS = ["ek_door_green", "ek_door_brown", "ek_door_red", "ek_door_cream", "ek_gate_green", "ek_gate_red", "ek_gate_cream"]
FRAMES = ["ek_frame_white", "ek_frame_dark", "ek_frame_green"]


def material(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    tex, tint, tiling = MAT[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.85
    if GREY:
        tint, tex = (0.6, 0.6, 0.6), ""
    bsdf.inputs["Base Color"].default_value = (*tint, 1)
    m.diffuse_color = (*tint, 1)
    path = TEXTURES / (tex + "_albedo.png")
    if tex and path.exists():
        nodes, links = m.node_tree.nodes, m.node_tree.links
        img = nodes.new("ShaderNodeTexImage")
        img.image = bpy.data.images.load(str(path), check_existing=True)
        uv, mp = nodes.new("ShaderNodeUVMap"), nodes.new("ShaderNodeMapping")
        mp.inputs["Scale"].default_value = (tiling, tiling, 1)
        links.new(uv.outputs["UV"], mp.inputs["Vector"]); links.new(mp.outputs["Vector"], img.inputs["Vector"])
        mix = nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(img.outputs["Color"], mix.inputs[6]); mix.inputs[7].default_value = (*tint, 1)
        links.new(mix.outputs[2], bsdf.inputs["Base Color"])
        if name in CUTOUT:
            links.new(img.outputs["Alpha"], bsdf.inputs["Alpha"])
            m.use_backface_culling = False
            if hasattr(m, "surface_render_method"):
                m.surface_render_method = "DITHERED"
    return m


# ------------------------------------------------------------------ the mesh buffer
class Buf:
    """Quads with a material each and UVs in metres. `frame` maps local (u, w, z) to the world, so a house built once
    faces whichever way its frame turns it (frames are rotations: winding stays right)."""

    def __init__(self, name):
        self.name, self.v, self.f, self.uv, self.m, self.mats = name, [], [], [], [], []
        self.frame = Matrix.Identity(4)

    def mat(self, name):
        if name not in self.mats:
            self.mats.append(name)
        return self.mats.index(name)

    def quad(self, pts, mat, uvs=None):
        """pts counter-clockwise seen from outside, in LOCAL coordinates."""
        a, b, c = Vector(pts[0]), Vector(pts[1]), Vector(pts[2])
        n = (b - a).cross(c - a)
        if uvs is None:
            ax = max(range(3), key=lambda i: abs(n[i]))
            pick = {0: (1, 2), 1: (0, 2), 2: (0, 1)}[ax]
            uvs = [(p[pick[0]], p[pick[1]]) for p in pts]
        base = len(self.v)
        self.v += [tuple(self.frame @ Vector(p)) for p in pts]
        self.f.append(tuple(range(base, base + len(pts))))
        self.uv += list(uvs)
        self.m.append(self.mat(mat))

    def box(self, x0, x1, y0, y1, z0, z1, mat, top=None, skip=""):
        """A closed box. `top` is another material for its upper face; `skip` drops faces by letter (x X y Y z Z).

        ⚠️ ITS EIGHT CORNERS ARE ITS OWN, SHARED BY ITS SIX FACES AND BY NOTHING ELSE. Boxes used to be loose quads welded
        afterwards by distance, so two boxes that TOUCHED were welded to each other; the bevel then rounded edges that
        belonged to two solids at once and pulled their faces askew (owner, 2026-10-09, of the roof stairs, whose steps
        are stacked boxes: "lots more z fighting"). Nothing is welded across boxes now."""
        x0, x1 = min(x0, x1), max(x0, x1); y0, y1 = min(y0, y1), max(y0, y1); z0, z1 = min(z0, z1), max(z0, z1)
        c = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
        base = len(self.v)
        self.v += [tuple(self.frame @ Vector(q)) for q in c]
        for letter, idx, m in (("x", (3, 0, 4, 7), mat), ("X", (1, 2, 6, 5), mat), ("y", (0, 1, 5, 4), mat), ("Y", (2, 3, 7, 6), mat),
                               ("z", (3, 2, 1, 0), mat), ("Z", (4, 5, 6, 7), top or mat)):
            if letter in skip:
                continue
            pick = {"x": (1, 2), "X": (1, 2), "y": (0, 2), "Y": (0, 2), "z": (0, 1), "Z": (0, 1)}[letter]
            self.f.append(tuple(base + i for i in idx))
            self.uv += [(c[i][pick[0]], c[i][pick[1]]) for i in idx]
            self.m.append(self.mat(m))

    def ramp(self, x0, x1, y0, y1, z0, z1, mat, base=None):
        """A wedge rising along +y from (y0, z0) to (y1, z1), solid down to `base` (default the lower of the two)."""
        b = min(z0, z1) if base is None else base
        self.quad([(x0, y0, z0), (x1, y0, z0), (x1, y1, z1), (x0, y1, z1)], mat)
        self.quad([(x0, y1, b), (x1, y1, b), (x1, y0, b), (x0, y0, b)], mat)
        self.quad([(x0, y0, b), (x1, y0, b), (x1, y0, z0), (x0, y0, z0)], mat)
        self.quad([(x1, y1, b), (x0, y1, b), (x0, y1, z1), (x1, y1, z1)], mat)
        self.quad([(x0, y1, b), (x0, y0, b), (x0, y0, z0), (x0, y1, z1)], mat)
        self.quad([(x1, y0, b), (x1, y1, b), (x1, y1, z1), (x1, y0, z0)], mat)

    def obj(self, bevel=0.0, collection=None, shift=True):
        me = bpy.data.meshes.new(self.name)
        # ⚠️ THE CAN'S TERRACE IS HEIGHT ZERO IN THE GAME. This file is typed with the low end at 0 and the can at 0.9;
        # every mesh is lowered by that here, and every placement, pad and camera with it (`DOWN`). The game takes the
        # court's floor from a ray cast down from 0.7 m over the can BEFORE the can is seated, so with the can's floor
        # at 0.9 it found the plate under the map (the play probe: "CourtBoundaryPresentation floor y -0.020"), and
        # it stands the attackers at (x, 0, 9). Every other map has its court at zero; so does this one now.
        vs = [(x, y, z - DOWN) for (x, y, z) in self.v] if shift else self.v
        me.from_pydata(vs, [], self.f)
        layer = me.uv_layers.new(name="UVMap")
        for i, uv in enumerate(self.uv):
            layer.data[i].uv = uv
        for name in self.mats:
            me.materials.append(material(name))
        me.polygons.foreach_set("material_index", self.m)
        me.update()
        o = bpy.data.objects.new(self.name, me)
        (collection or bpy.context.scene.collection).objects.link(o)
        if bevel > 0:
            mod = o.modifiers.new("Bevel", "BEVEL")
            mod.width, mod.segments, mod.limit_method, mod.angle_limit = bevel, 1, "ANGLE", math.radians(40)
            mod.harden_normals = True
            me.polygons.foreach_set("use_smooth", [True] * len(me.polygons))
        return o


def frame(origin, u, w):
    """Local x runs along `u`, local y back along `w` (both unit world vectors, u x w = up)."""
    m = Matrix.Identity(4)
    m.col[0][:3], m.col[1][:3], m.col[2][:3], m.col[3][:3] = u, w, (0, 0, 1), origin
    return m


WEST = lambda y0, x=AX: frame((-x, y0, 0), (0, 1, 0), (-1, 0, 0))      # front faces +x; local u runs north
EAST = lambda y1, x=AX: frame((x, y1, 0), (0, -1, 0), (1, 0, 0))       # front faces -x; local u runs south
NORTH = lambda x0, y: frame((x0, y, 0), (1, 0, 0), (0, 1, 0))     # front faces -y
SOUTH = lambda x1, y: frame((x1, y, 0), (-1, 0, 0), (0, -1, 0))   # front faces +y

# ------------------------------------------------------------------ a house
OPEN = {  # kind: (width, height, sill above its storey's floor, card material or None for "pick a door")
    "door": (0.95, 2.05, 0.06, None), "jal": (1.15, 1.15, 0.95, "ek_jalousie"), "slide": (1.6, 1.15, 0.95, "ek_slide"),
    "small": (0.7, 0.6, 1.5, "ek_jalousie"),
}


OPENINGS = []
IN = 0.012


def facade(shell, trim, cards, fr, length, bands, floors, rng, framem, blocked=(), doors=0, kinds=("jal", "slide"), step_base=None):
    """One wall as a grid of quads in the frame `fr` (the wall is the plane y = 0, x 0..length), with its openings left
    out and pushed in 14 cm: no window is a picture on a flat wall. `bands` are (z0, z1, material) bottom to top,
    `floors` the height each storey's openings are measured from (the first is the ground storey)."""
    for b in (shell, trim, cards):
        b.frame = fr
    wall_top = bands[-1][1]
    opens = []

    def fits(u0, u1, g):
        if u0 < 0.4 or u1 > length - 0.4:
            return False
        if g and any(u0 < b1 + 0.15 and u1 > b0 - 0.15 for b0, b1 in blocked):
            return False
        return all(u1 < o[0] - 0.45 or u0 > o[1] + 0.45 or (o[5] != g) for o in opens)

    def add(kind, uc, floor, g):
        w, h, sill, card = OPEN[kind]
        if kind != "door":
            sill = min(sill, wall_top - 0.16 - h - floor)          # a short upper storey sets its windows lower
            if sill < 0.45:
                return False
        if floor + sill + h > wall_top - 0.1 or not fits(uc - w / 2, uc + w / 2, g):
            return False
        opens.append((uc - w / 2, uc + w / 2, floor + sill, floor + sill + h, card or rng.choice(DOORS), g, kind))
        return True

    for storey, floor in enumerate(floors):
        g = storey == 0
        if g:
            for _ in range(doors):
                spots = [0.9 + 0.15 * k for k in range(int((length - 1.8) / 0.15) + 1)]
                rng.shuffle(spots)
                for uc in spots:
                    if add("door", uc, floor, True):
                        break
        n = max(1, int(length / 2.3))
        for k in range(n):
            kind, uc = rng.choice(list(kinds)), (k + 0.5) * length / n + rng.uniform(-0.25, 0.25)
            for off in (0, 0.35, -0.35, 0.7, -0.7, 1.0, -1.0):
                if add(kind, uc + off, floor, g) or add("jal", uc + off, floor, g):
                    break
    for o in opens:                                           # where the wall is open, in the world (for `clear_of_openings`)
        a, c = fr @ Vector((o[0], 0, o[2])), fr @ Vector((o[1], 0, o[3]))
        OPENINGS.append((min(a.x, c.x), max(a.x, c.x), min(a.y, c.y), max(a.y, c.y), o[2], o[3]))
    us = sorted({0.0, length, *[o[0] for o in opens], *[o[1] for o in opens]})
    zs = sorted({b[0] for b in bands} | {wall_top} | {o[2] for o in opens} | {o[3] for o in opens})

    def band_at(zc):
        return next((b[2] for b in bands if b[0] <= zc < b[1]), bands[-1][2])

    for i in range(len(us) - 1):
        for j in range(len(zs) - 1):
            u0, u1, z0, z1 = us[i], us[i + 1], zs[j], zs[j + 1]
            uc, zc = (u0 + u1) / 2, (z0 + z1) / 2
            if any(o[0] < uc < o[1] and o[2] < zc < o[3] for o in opens):
                continue
            shell.quad([(u0, 0, z0), (u1, 0, z0), (u1, 0, z1), (u0, 0, z1)], band_at(zc))
    rec = 0.14
    for (u0, u1, z0, z1, card, g, kind) in opens:
        rm = band_at((z0 + z1) / 2)
        shell.quad([(u0, 0, z0), (u0, rec, z0), (u0, rec, z1), (u0, 0, z1)], rm)
        shell.quad([(u1, rec, z0), (u1, 0, z0), (u1, 0, z1), (u1, rec, z1)], rm)
        shell.quad([(u0, 0, z1), (u0, rec, z1), (u1, rec, z1), (u1, 0, z1)], rm)
        shell.quad([(u0, rec, z0), (u0, 0, z0), (u1, 0, z0), (u1, rec, z0)], rm)
        cards.quad([(u0, rec, z0), (u1, rec, z0), (u1, rec, z1), (u0, rec, z1)], card, [(0, 0), (1, 0), (1, 1), (0, 1)])
        if kind == "door":
            sb = floors[0] if step_base is None else step_base
            # ⚠️ EVERY FRAME PIECE STANDS A CENTIMETRE INTO ITS OPENING (owner, 2026-10-09: "z fighting on all window sills").
            # Sills, jambs, heads and steps ended exactly on the opening's own edges, so each lay in the same plane as
            # the wall's reveal for the few centimetres they shared, and flickered. `IN` is that centimetre.
            trim.box(u0 - 0.12, u1 + 0.12, -0.34, 0.05, sb - 0.02, z0 + IN, "ek_concrete")              # the step
            trim.box(u0 - 0.09, u0 + IN, -0.035, 0.03, z0, z1, framem); trim.box(u1 - IN, u1 + 0.09, -0.035, 0.03, z0, z1, framem)
            trim.box(u0 - 0.10, u1 + 0.10, -0.04, 0.03, z1 - IN, z1 + 0.10, framem)
            if rng.random() < 0.6:                                                                  # a little tin awning
                awning(trim, u0 - 0.3, u1 + 0.3, z1 + 0.34, 0.62, rng.choice(["ek_tin_rust", "ek_tin_green", "ek_tin_red", "ek_tin_grey"]))
        else:
            trim.box(u0 - 0.10, u1 + 0.10, -0.07, 0.04, z0 - 0.09, z0 + IN, framem)                     # the sill
            trim.box(u0 - 0.07, u0 + IN, -0.03, 0.03, z0, z1, framem); trim.box(u1 - IN, u1 + 0.07, -0.03, 0.03, z0, z1, framem)
            trim.box(u0 - 0.08, u1 + 0.08, -0.035, 0.03, z1 - IN, z1 + 0.08, framem)
            r = rng.random()
            if g and r < 0.5 and kind != "small":                                                    # a security grille
                gm = rng.choice(["ek_grille", "ek_grille_white"])
                cards.quad([(u0 - 0.03, -0.06, z0 - 0.03), (u1 + 0.03, -0.06, z0 - 0.03), (u1 + 0.03, -0.06, z1 + 0.03), (u0 - 0.03, -0.06, z1 + 0.03)],
                           gm, [(0, 0), (u1 - u0, 0), (u1 - u0, z1 - z0), (0, z1 - z0)])
            elif r > 0.66:
                awning(trim, u0 - 0.15, u1 + 0.15, z1 + 0.24, 0.5, rng.choice(["ek_tin_rust", "ek_tin_green", "ek_trapal_red", "ek_trapal_yellow", "ek_trapal_teal"]))
            elif r > 0.5 and not g:                                                                  # a plant shelf under an upper window
                trim.box(u0 - 0.05, u1 + 0.05, -0.30, 0.02, z0 - 0.16, z0 - 0.11, "ek_wood_dark")
                for u in (u0 + 0.05, u1 - 0.05):
                    trim.box(u - 0.025, u + 0.025, -0.26, 0.02, z0 - 0.34, z0 - 0.15, "ek_wood_dark")
    return opens


def house(shell, trim, cards, fr, length, depth, base, top, rng, blocked=(), look=None, split=None, back=True, eave=0.16,
          doors=1, side0=None, side1=None):
    """One solid house in the local frame: its front is the plane y = 0 from x 0..length, it runs back to y = depth,
    from z `base` to `top` (its flat, walkable roof). `blocked` are (u0, u1) stretches of the GROUND storey that get no
    opening (a stair or ramp stands against them). `side0` and `side1` are (metres, floor outside) for a side wall
    whose first metres stand open to the court (the alley is narrower than the court, so four houses show a flank):
    that stretch is a real wall with windows, measured from the floor it faces."""
    look = look or {}
    two = (top - base) > 3.4
    wall = look.get("wall") or rng.choice(WALLS)
    band = look.get("band") or rng.choice(BANDS)
    ground = look.get("ground") or rng.choice([wall, wall, "ek_chb", "ek_chb_warm"])
    upper = look.get("upper") or rng.choice(UPPER + [wall, rng.choice(WALLS)])
    roofm = look.get("roof") or (rng.choice(ROOFS) if two else rng.choice(["ek_deck", "ek_deck", "ek_tin_grey", "ek_tin_rust"]))
    framem = look.get("frame") or rng.choice(FRAMES)
    split = split if split is not None else (base + 2.55 if two else None)
    roof_t = 0.14
    wall_top = top - roof_t + 0.02
    raw = ground in ("ek_chb", "ek_chb_warm")

    def bands_from(bottom, floor):
        bs = [(bottom, floor + 0.85, ground if raw else band)]
        if two and split > floor + 1.2:
            bs += [(floor + 0.85, split, ground), (split, wall_top, upper)]
        elif two:
            bs += [(floor + 0.85, wall_top, upper)]
        else:
            bs += [(floor + 0.85, wall_top, ground)]
        return bs

    storeys = [base] + ([split] if two else [])
    while two and storeys[-1] + 2.7 + 2.0 < wall_top:
        storeys.append(storeys[-1] + 2.7)
    facade(shell, trim, cards, fr, length, bands_from(base, base), storeys, rng, framem, blocked, doors)
    plain = bands_from(base, base)
    for which, side in ((0, side0), (1, side1)):
        e = 0.0
        if side:
            e, outside = side
            f2 = fr @ (frame((0, e, 0), (0, -1, 0), (1, 0, 0)) if which == 0 else frame((length, 0, 0), (0, 1, 0), (-1, 0, 0)))
            floors = [max(base, outside)] if not two else ([split] if outside > base + 0.5 else list(storeys))
            if outside > base + 0.5 and two:
                floors = [-50.0, split]                       # no ground storey to open on this flank: it is half buried
            facade(shell, trim, cards, f2, e, bands_from(min(base, outside) - 0.15, max(base, outside)), floors, rng, framem)
            for b in (shell, trim, cards):
                b.frame = fr
            # the corner post where the flank meets the front, and the belt round it
            u = 0.0 if which == 0 else length
            trim.box(u - 0.13, u + 0.13, -0.05, 0.21, min(base, outside) - 0.1, wall_top, "ek_coping")
        for (z0, z1, m) in plain:
            mm = m if m not in BANDS else (ground if ground not in BANDS else wall)
            if which == 0:
                shell.quad([(0, depth, z0), (0, e, z0), (0, e, z1), (0, depth, z1)], mm)
            else:
                shell.quad([(length, e, z0), (length, depth, z0), (length, depth, z1), (length, e, z1)], mm)
    for b in (shell, trim, cards):
        b.frame = fr
    if back:
        for (z0, z1, m) in plain:
            mm = m if m not in BANDS else (ground if ground not in BANDS else wall)
            shell.quad([(length, depth, z0), (0, depth, z0), (0, depth, z1), (length, depth, z1)], mm)
    # ⚠️ THE ROOF, THE POSTS AND THE BELTS STAND 2 CM PAST THE HOUSE'S FLANKS (owner, 2026-10-09: "z fighting on the trims and
    # flat facing wall-roof meets. i think you should just extend them everso slightly. happens to all houses with a
    # flat sidewall"). They ended exactly on the flank wall's plane, and their end faces flickered against it.
    PAST = 0.02
    shell.box(-PAST, length + PAST, -eave, depth + PAST, top - roof_t, top, "ek_concrete" if roofm == "ek_deck" else "ek_wood_dark", top=roofm, skip="z")
    shell.quad([(-PAST, depth + PAST, top - roof_t), (length + PAST, depth + PAST, top - roof_t), (length + PAST, -eave, top - roof_t), (-PAST, -eave, top - roof_t)], "ek_wood_dark")
    # THE FRAME SHOWS: a concrete post at each end of the front and a beam between the storeys, standing 4 cm proud,
    # the way a Manila house is a concrete frame filled in with block. It also parts each house from its neighbour.
    for (u0, u1) in ((-PAST, 0.22), (length - 0.22, length + PAST)):
        trim.box(u0, u1, -0.045, 0.05, base - 0.05, wall_top, "ek_coping")
    if two:
        for fl in storeys[1:]:
            trim.box(-PAST - 0.005, length + PAST + 0.005, -0.06, 0.05, fl - 0.16, fl + 0.02, "ek_coping")
        for k in range(int(length / 0.9) + 1):                                                      # rafter ends under the eave
            u = 0.25 + k * (length - 0.5) / max(1, int(length / 0.9))
            trim.box(u - 0.045, u + 0.045, -eave + 0.02, 0.06, top - roof_t - 0.12, top - roof_t + 0.02, "ek_wood_dark")
    else:
        trim.box(-PAST - 0.005, length + PAST + 0.005, -0.05, 0.05, top - roof_t - 0.02, top - 0.02, "ek_coping")              # the roof slab's edge beam
    if rng.random() < 0.6:                                                                          # a downspout by one post
        u = 0.32 if rng.random() < 0.5 else length - 0.32
        trim.box(u - 0.04, u + 0.04, -0.09, -0.01, base + 0.05, wall_top - 0.1, rng.choice(["ek_pipe_cream", "ek_pipe_green", "ek_pipe_red"]))
    return {"wall": wall, "roof": roofm, "upper": upper if two else ground}


def awning(trim, u0, u1, z, out, mat):
    """A sloped sheet on two little brackets, dropping away from the wall."""
    f = trim.frame
    trim.quad([(u0, -out, z - 0.22), (u1, -out, z - 0.22), (u1, 0.02, z), (u0, 0.02, z)], mat)
    trim.quad([(u1, -out, z - 0.25), (u0, -out, z - 0.25), (u0, 0.02, z - 0.03), (u1, 0.02, z - 0.03)], "ek_wood_dark")
    trim.quad([(u0, -out, z - 0.25), (u1, -out, z - 0.25), (u1, -out, z - 0.22), (u0, -out, z - 0.22)], "ek_wood_dark")
    for u in (u0 + 0.08, u1 - 0.08):
        trim.box(u - 0.025, u + 0.025, -out + 0.05, 0.03, z - 0.30, z - 0.25, "ek_wood_dark")


# ------------------------------------------------------------------ the plan
# Houses: (y0, y1, roof top (absolute), floor it stands on). All 5.6 m deep, to the outer bound.
WEST_H = [(-17.5, -13.0, 2.6, Z0), (-13.0, -MID, 5.0, Z0), (-MID, -3.5, 3.5, Z1), (-3.5, 3.0, 5.8, Z1), (3.0, MID, 3.4, Z1),
          (MID, 13.0, 6.6, Z2), (13.0, 17.5, 4.4, Z2)]
EAST_H = [(-17.5, -12.0, 2.5, Z0), (-12.0, -MID, 5.0, Z0), (-MID, -2.0, 5.9, Z1), (-2.0, 3.6, 3.5, Z1), (3.6, MID, 5.6, Z1),
          (MID, 12.2, 6.6, Z2), (12.2, 17.5, 4.3, Z2)]
# What stands against a ground-floor wall (no door or window behind it), in y.
WEST_BLOCK = [(-17.5, -13.0), (-12.9, -MID), (-8.4, -3.5), (13.4, 16.6)]
EAST_BLOCK = [(3.6, 7.8), (MID, 12.9), (-6.6, -3.8)]
# Ledges out over the alley: (x0, x1, y0, y1, top).
LEDGES = [(-8, -6.7, -3.5, 3.0, 3.45), (-8, -6.8, 3.0, MID, 3.4), (6.8, 8, -2.0, 3.6, 3.5), (3.7, 5, -17.5, -12.0, 2.5),
          (3.8, 5, 12.2, 17.5, 4.3)]
# Stairs: (x0, x1, y_from, y_to, z_from, z_to, style). They rise from y_from to y_to. Style: "painted" (concrete, the
# risers each a colour), "wood" (roof steps), "ramp" (a smooth concrete ramp).
STAIRS = [
    # ⚠️ EACH RISE IS ONE FLIGHT, WALL TO WALL (owner, 2026-10-09, after playing: "the bot ais struggle to get around this
    # map"). A bot walks STRAIGHT at where it is going (`AIController.Goto`: a bearing, eight keys, and a sidestep when it
    # stops moving); it has no path. With two flights, a ramp, cheek walls and a 0.9 m wall between them at each rise,
    # the straight line from one terrace to the next usually ended in a wall. Now every straight line along the alley
    # climbs. The ramps and the cheek walls are gone with it.
    (-EX, EX, -MID - 1.8, -MID, Z0, Z1, "painted"),
    (-EX, EX, MID, MID + 1.8, Z1, Z2, "painted"),
    (-5.0, -3.9, -13.2, -16.9, Z0, 2.6, "painted"),                # south terrace, up to the west low roof
    # ⚠️ THE TWO COURT STAIRS STAND 1.5 M CLEAR OF THE HOUSE BEHIND THEIR FOOT (owner, 2026-10-09: "theres no space on this
    # staircase.. its the one i pointed out earlier"; his earlier note on it, "no gap", I read backwards and closed the
    # 30 cm there was). Their foot faced a wall, so the only way on was sideways over the first steps. Now a body walks
    # into the corner and turns up the flight. Each ends where its balcony begins; the short landings are gone.
    (-8.0, -6.9, -7.0, -3.5, Z1, 3.5, "painted"),                  # the can's terrace, west
    (7.0, 8.0, 7.0, 3.6, Z1, 3.5, "painted"),                      # the can's terrace, east
    # roof to roof
    (-12.6, -11.5, -16.4, -13.0, 2.6, 5.0, "wood"), (-13.4, -12.3, -6.3, -MID, 3.5, 5.0, "wood"), (-9.6, -8.5, -6.7, -3.5, 3.5, 5.8, "wood"),
    (-13.2, -12.1, 6.4, 3.0, 3.4, 5.8, "wood"), (-13.2, -12.1, 16.2, 13.0, 4.4, 6.6, "wood"),
    (12.3, 13.4, -15.5, -12.0, 2.5, 5.0, "wood"), (11.0, 12.1, -9.8, -MID, 5.0, 5.9, "wood"), (12.3, 13.4, 1.4, -2.0, 3.5, 5.9, "wood"),
    (8.5, 9.6, 0.7, 3.6, 3.5, 5.6, "wood"), (12.3, 13.4, 7.1, MID, 5.6, 6.6, "wood"), (12.3, 13.4, 15.5, 12.2, 4.3, 6.6, "wood"),
]
# The one stair that runs along x (against the north end wall): x_from, x_to, y0, y1, z_from, z_to.
XSTAIR = (0.2, 3.8, 16.4, 17.5, Z2, 4.3)
# Bounce tarps: (x0, x1, y0, y1, top, launch speed, trapal). Gravity is 20: the apex is top + v*v/40.
# Every tarp is 2 m square: the sheet itself is the JumpPad's own model (`pad_trapal`, built below), which is square.
# ⚠️ A TARP STANDS 0.28 M OFF ITS FLOOR (it was 0.75): under the body's 0.3 m step, so anybody WALKS onto it, a bot too
# (`MapRoutes`: a bot cannot jump up onto a thing, it can only walk). The launch speeds are raised to reach the same roofs.
TARP_H = 0.28
# ⚠️ EACH TARP TOUCHES THE EDGE OF WHAT IT THROWS A BODY ONTO (its middle is a metre from it), AND THROWS HIGH (play probe 9,
# 2026-10-09: with the movement rework a body bounced from a standstill drifts only about a metre in the air, and two of
# the three landings that had passed a day earlier fell short, one back under its ledge).
TARPS = [(1.7, 3.7, -16.7, -14.7, TARP_H, 13.0, "ek_trapal_red"), (-5.0, -3.0, 13.6, 15.6, Z2 + TARP_H, 12.5, "ek_trapal_teal"),
         (-11.6, -9.6, 6.5, 8.5, 3.4 + TARP_H, 13.5, "ek_trapal_yellow")]
BRIDGES = [(-MID - 1.6, -MID - 0.7, 5.0), (MID + 0.7, MID + 1.6, 6.6)]
LOW_WALLS = []
# Props that are solid: name -> (half x, half y, height) of a box at the prop's place (before yaw).
SOLID = {"poste": (0.16, 0.16, 7.0), "poste_short": (0.14, 0.14, 5.0), "water_drum": (0.32, 0.32, 0.95),
         "water_drum_yellow": (0.32, 0.32, 0.95), "bench_wood": (0.9, 0.25, 0.45), "crates_stack": (0.25, 0.2, 0.9), "kariton": (0.75, 0.45, 0.9),
         "water_tank": (0.6, 0.6, 1.8), "basketball_hoop": (0.12, 0.12, 3.6), "tires_stack": (0.35, 0.35, 0.8), "sacks": (0.5, 0.35, 0.6),
         "table_dama": (0.4, 0.4, 0.5), "tricycle": (0.8, 1.3, 1.6)}
# PLACE: (prop, x, y, z, yaw degrees). A prop's front faces -y at yaw 0; +90 turns it to face +x (against the west wall),
# -90 to face -x (against the east wall), 180 to face +y.
PLACE = [
    ("gate_arch", 0.0, -17.95, Z0, 180), ("basketball_hoop", -2.4, 17.0, Z2, 0), ("sarisari_front", 8.0, -5.2, Z1, -90),
    ("poste", 4.6, -11.2, Z0, -90), ("poste", -4.6, 11.6, Z2, 90), ("poste_short", -4.6, -12.4, Z0, 90), ("poste_short", 4.6, 11.0, Z2, -90),
    ("banderitas_17m", 0.0, -6.2, 6.5, 4), ("banderitas_17m", 0.0, -0.8, 6.7, -6), ("banderitas_17m", 0.0, 5.0, 6.6, 5),
    ("laundry_line_6m", -11.0, -15.2, 2.6 + 1.9, 90), ("laundry_line_6m", 10.6, 0.8, 3.5 + 1.9, 90), 
    ("laundry_line_6m", 9.0, 14.8, 4.3 + 1.9, 90), ("laundry_line_6m", 8.6, -14.6, 2.5 + 1.9, 0),
    ("water_drum", -7.55, -1.6, Z1, 90), ("water_drum_yellow", -7.5, -0.8, Z1, 90), ("pail_stack", -7.55, 0.0, Z1, 60), ("batya", -7.3, 4.6, Z1, 90),
    ("water_drum", 4.55, -16.9, Z0, -90), ("jerrycans", 4.5, -13.0, Z0, -90), ("water_drum_yellow", 4.5, 13.6, Z2, -90),
    ("plant_pots_a", -7.5, 6.6, Z1, 90), ("plant_pots_b", 7.5, -7.6, Z1, -90), ("plant_pots_c", -4.5, 11.0, Z2, 90), ("plant_pots_a", 4.5, 15.8, Z2, -90),
    ("plant_pots_b", -3.4, -17.2, Z0, 20), 
    ("plant_pots_b", -7.3, -2.2, 3.45, 90), ("plant_pots_c", 7.35, 1.6, 3.5, -90), ("plant_tall", 4.3, -13.9, Z0, 0), ("plant_tall", 7.3, -8.0, Z1, 0),
    ("plant_pots_a", 4.35, -15.0, 2.5, -90), ("plant_pots_b", 4.4, 16.0, 4.3, -90), ("plant_tall", -4.4, 12.6, Z2, 0),
    ("monobloc", 6.9, -3.3, Z1, -60), ("monobloc", -7.2, 3.6, Z1, 110), ("bench_wood", 7.55, 0.6, Z1, -90), ("table_dama", 3.9, -11.4, Z0, 15),
    ("monobloc", 3.2, -11.0, Z0, 150), ("monobloc", 4.2, -12.3, Z0, 20),
    ("kariton", -1.9, -15.6, Z0, 25), ("crates_stack", 7.6, -6.9, Z1, -90), ("sacks", 7.4, -3.9, Z1, -90), ("tires_stack", -4.5, 12.9, Z2, 0),
    ("crates_stack", 4.6, 12.9, Z2, -90),
    ("roof_tire", -10.2, -11.4, 5.0, 0), ("roof_tire", -7.2, -9.6, 5.0, 40), ("roof_blocks", -9.0, -12.2, 5.0, 20), ("roof_tire", -10.4, 0.8, 5.8, 0),
    ("roof_blocks", -12.0, -1.8, 5.8, 70), ("roof_tire", -9.3, 2.0, 5.8, 10), ("roof_tire", -10.6, 11.6, 6.6, 0), ("roof_blocks", -7.4, 9.6, 6.6, 0),
    ("roof_tire", 10.4, -10.8, 5.0, 0), ("roof_blocks", 7.6, -11.2, 5.0, 30), ("roof_tire", 10.8, -5.4, 5.9, 0), ("roof_tire", 12.4, -3.4, 5.9, 0),
    ("roof_blocks", 9.4, -7.2, 5.9, 50), ("roof_tire", 10.6, 5.6, 5.6, 0), ("roof_blocks", 9.2, 7.6, 5.6, 0), ("roof_tire", 7.2, 10.8, 6.6, 0),
    ("water_tank", -12.2, 1.4, 5.8, 0), ("water_tank", 12.3, -6.0, 5.9, 0), ("water_tank", -9.4, 11.6, 6.6, 0), ("antenna", -9.0, -11.6, 5.0, 0),
    ("antenna", 12.6, 5.0, 5.6, 0), ("satellite_dish", 9.6, 11.6, 6.6, 140), ("satellite_dish", -9.2, -2.9, 5.8, 200), ("antenna", 10.9, 11.0, 6.6, 0),
    ("manok_cage", -7.3, 1.2, 3.45, 90),
    ("manok_cage", 4.2, 14.4, Z2, -90),
    ("ac_unit", -5.0, -12.2, 3.6, 90), ("ac_unit", 8.0, -7.4, 4.7, -90), ("ac_unit", -5.0, 10.2, 5.4, 90), ("ac_unit", 8.0, 6.4, 4.6, -90),
    ("meter_box", -8.0, -2.9, Z1 + 1.5, 90), ("meter_box", 8.0, -7.9, Z1 + 1.5, -90), ("meter_box", 5.0, -15.6, 1.5, -90), ("mailbox", -8.0, 4.0, Z1 + 1.2, 90),
    ("street_lamp_wall", -5.0, -10.4, 3.9, 90), ("street_lamp_wall", 8.0, 2.4, 5.2, -90), ("street_lamp_wall", -5.0, 14.9, 5.3, 90),
    ("sign_brgy", 5.0, -13.8, 1.3, -90), ("shrine", -3.9, 17.5, Z2 + 1.0, 0), ("sign_brgy", -5.0, 9.9, Z2 + 1.4, 90),
    
]

# TREES in the yards beyond the bounds: the park trees the owner approved for Kanto (docs/KANTO_DESIGN_GUIDE.md section
# 11), copied as they are with their own materials. (model, x, y, yaw, scale): the trunk stands in a yard, the crown
# shows over the roofs.
TREES = [("tree_broad_lime", -18.5, -4.0, 20, 1.5), ("tree_round", 18.8, 6.5, 80, 1.6), ("tree_round", -19.5, 14.0, 140, 1.35),
         ("tree_lean_olive", 17.6, -14.5, 200, 1.5), ("tree_broad_lime", 1.5, 28.5, 60, 1.9), ("tree_round", -10.5, 27.0, 10, 1.4),
         ("tree_lean_olive", 12.0, 26.0, 300, 1.5), ("tree_round", -23.0, -33.0, 0, 1.5), ("tree_broad_lime", 22.0, -31.0, 120, 1.6),
         ("tree_lean_olive", -31.0, 3.0, 30, 1.7), ("tree_broad_lime", 31.0, -5.0, 70, 1.6), ("tree_round", 28.0, 19.0, 250, 1.8),
         ("tree_broad_lime", -27.0, 25.0, 170, 1.7), ("tree_lean_olive", -8.0, -33.0, 90, 1.5), ("tree_round", 9.0, -34.0, 15, 1.6)]
TREE_MATS = {"leaf_light": (0.6285, 0.8869, 0.2784), "leaf_dark": (0.3815, 0.6563, 0.2515), "leaf_light_lime": (0.8279, 0.9816, 0.3815),
             "leaf_dark_lime": (0.5889, 0.7928, 0.2784), "leaf_light_deep": (0.4723, 0.7633, 0.3815), "leaf_dark_deep": (0.2784, 0.5459, 0.2784),
             "leaf_light_olive": (0.7928, 0.8613, 0.4144), "leaf_dark_olive": (0.5679, 0.6472, 0.3024)}

rng = random.Random(20261009)
drng_road = random.Random(31)
shell, trim, cards = Buf("alley_houses"), Buf("alley_trim"), Buf("alley_cards")
class ColBuf(Buf):
    """The collision. ⚠️ EVERY PIECE IS ALSO KEPT AS A SOLID (owner, 2026-10-09, in play: "bug, slipper ended up under the
    map"). The one collision mesh is a hollow shell: its floors are single faces with nothing behind them, so a fast
    slipper that got through one lay inside the terrace for good. Unity now also gets each box as a BoxCollider and each
    stair as a convex wedge (`solids` in the layout): a thing that ends up inside one is pushed back out."""

    def __init__(self, name):
        super().__init__(name)
        self.boxes, self.wedges = [], []

    def box(self, x0, x1, y0, y1, z0, z1, mat, top=None, skip=""):
        self.boxes.append((min(x0, x1), max(x0, x1), min(y0, y1), max(y0, y1), min(z0, z1), max(z0, z1)))
        super().box(x0, x1, y0, y1, z0, z1, mat, top, skip)

    def ramp(self, x0, x1, y0, y1, z0, z1, mat, base=None):
        b = min(z0, z1) if base is None else base
        self.wedges.append([(x0, y0, b), (x1, y0, b), (x1, y1, b), (x0, y1, b), (x0, y0, z0), (x1, y0, z0), (x1, y1, z1), (x0, y1, z1)])
        super().ramp(x0, x1, y0, y1, z0, z1, mat, base)


ground, steps, col = Buf("alley_ground"), Buf("alley_steps"), ColBuf("collision")
district, dtrim = Buf("district"), Buf("district_trim")

# ---------------------------------------------------------------- the floor, the rises, the street outside
ground.box(-EX - 0.2, EX + 0.2, -AY, -MID, -0.6, Z0, "ek_chb_warm", top="ek_floor")
ground.box(-AX - 0.2, AX + 0.2, -MID, MID, -0.6, Z1, "ek_chb_warm", top="ek_floor")
# ⚠️ THE TOP TERRACE IS NOTCHED FOR ITS THREE FLIGHTS. They climb INTO it (the chalk box leaves no room on the can's
# side), and the first build laid the slab whole over them: the play probe could not climb any of the three
# ("a y 1.80 surface lies over them"). (x0, x1, where the slab starts in y).
NORTH_STRIPS = [(-EX - 0.2, -EX, MID), (-EX, EX, MID + 1.8), (EX, EX + 0.2, MID)]
# ⚠️ THE TWO HOUSES BESIDE THAT FLIGHT STAND ON THE TOP TERRACE, so their fronts began 0.9 m over the foot of the stair and
# the notch ran 20 cm under them: a dark slot at each end of the flight with the inside of the house in it (owner,
# 2026-10-09, circling both ends). Each front now has a footing down to the court's floor for the length of the flight.
for sx in (-1, 1):
    steps.box(sx * (EX - 0.03), sx * (EX + 0.3), MID - 0.02, MID + 1.95, Z1 - 0.12, Z2 + 0.03, "ek_chb_warm")
for (x0, x1, ys) in NORTH_STRIPS:
    ground.box(x0, x1, ys, AY, -0.6, Z2, "ek_chb_warm", top="ek_floor")
# ⚠️ THE ROAD THE ALLEY OPENS ON IS A ROAD (owner, 2026-10-09, looking over the low wall at a bare slab with one tricycle on
# it: "road and street life is missing"). An asphalt carriageway of two lanes with a worn centre line, a kerbed sidewalk
# each side, and the alley's own floor run out through the arch as an apron. What moves on it is Unity's
# (Editor/MapKit/EskinitaAlleyStreetAuthor.cs); these numbers are the ones it was told: lanes y -26.1..-18.9, centre
# -22.5, sidewalk tops 0.10 over the road.
ROAD0, ROAD1 = -26.1, -18.9
ground.box(-70, 70, ROAD0, ROAD1, -0.6, -0.02, "ek_asphalt")
for (x0, x1) in ((-70, -EX - 0.25), (EX + 0.25, 70)):
    ground.box(x0, x1, ROAD1, -AY, -0.6, 0.10, "ek_chb_warm", top="ek_street")
    steps.box(x0, x1, ROAD1 - 0.02, ROAD1 + 0.16, -0.05, 0.112, "ek_coping")                    # the kerb
ground.box(-EX - 0.25, EX + 0.25, ROAD1, -AY, -0.6, 0.0, "ek_chb_warm", top="ek_floor")       # the apron in the arch's mouth
ground.box(-70, 70, -27.5, ROAD0, -0.6, 0.10, "ek_chb_warm", top="ek_street")
steps.box(-70, 70, ROAD0 - 0.16, ROAD0 + 0.02, -0.05, 0.112, "ek_coping")
k = -66.0
while k < 66:                                                                               # the centre line, dashed and worn
    if drng_road.random() > 0.18:
        ground.box(k, k + 2.0 + drng_road.uniform(-0.3, 0.2), -22.56, -22.44, -0.03, -0.008, "ek_roadpaint")
    k += 5.0
ground.box(-70, 70, -70, -27.5, -0.6, -0.04, "ek_street"); ground.box(-70, -AX - 0.2, -AY, 70, -0.6, -0.06, "ek_street")
ground.box(AX + 0.2, 70, -AY, 70, -0.6, -0.06, "ek_street"); ground.box(-AX - 0.2, AX + 0.2, AY, 70, -0.6, -0.06, "ek_street")
col.box(-AX, AX, -AY - 12, -MID, -1.0, Z0, "ek_floor"); col.box(-AX, AX, -MID, MID, -1.0, Z1, "ek_floor")
for (x0, x1, ys) in [(-AX, -EX, MID), (EX, AX, MID), (-EX, EX, MID + 1.8)]:
    col.box(x0, x1, ys, AY, -1.0, Z2, "ek_floor")
col.box(-OUT, OUT, -AY - 12, AY, -1.0, -0.02, "ek_floor")


def flight(b, x0, x1, ya, yb, za, zb, style, k0=0):
    """Real steps from (ya, za) up to (yb, zb); the collision under them is one ramp."""
    run, rise = abs(yb - ya), zb - za
    d = 1 if yb > ya else -1
    base = za if style != "wood" else None
    if style == "ramp":
        f0, f1 = (ya, yb) if d > 0 else (yb, ya)
        z_lo, z_hi = (za, zb) if d > 0 else (zb, za)
        b.ramp(x0, x1, f0, f1, z_lo, z_hi, "ek_concrete", base=za - 0.05)
    else:
        n = max(2, round(rise / 0.19))
        for k in range(n):
            y0, y1 = ya + d * run * k / n, ya + d * run * (k + 1) / n
            z1 = za + rise * (k + 1) / n
            z0 = za + rise * k / n
            wood = style == "wood"
            # Solid steps either way: a layer per step, each running to the top of the flight. On a roof they are
            # timber with dark risers; in the alley they are concrete with every riser painted its own colour.
            b.box(x0, x1, y0, yb, za - 0.03 if k == 0 else z0, z1, "ek_wood" if wood else "ek_concrete")
            ry = y0 - d * 0.012
            b.box(x0 + 0.02, x1 - 0.02, ry, y0 + d * 0.02, z0 + 0.012, z1 - 0.03, "ek_wood_dark" if wood else RISERS[(k + k0) % len(RISERS)])
    lo, hi = min(ya, yb), max(ya, yb)
    zl, zh = (za, zb) if d > 0 else (zb, za)
    col.ramp(x0, x1, lo, hi, zl, zh, "ek_floor", base=za - 0.2 if style != "wood" else za - 0.05)


for k, (x0, x1, ya, yb, za, zb, style) in enumerate(STAIRS):
    flight(steps, x0, x1, ya, yb, za, zb, style, k * 2)
# The stair along the north end wall (it runs along x): built in a turned frame, where local y is world x.
xa, xb, y0, y1, za, zb = XSTAIR
steps.frame = col.frame = Matrix(((0, 1, 0, 0), (1, 0, 0, 0), (0, 0, 1, 0), (0, 0, 0, 1)))    # swaps x and y (a mirror: faces flip)
n = round((zb - za) / 0.19)
for k in range(n):
    xk0, xk1 = xa + (xb - xa) * k / n, xa + (xb - xa) * (k + 1) / n
    zk0, zk1 = za + (zb - za) * k / n, za + (zb - za) * (k + 1) / n
    steps.frame = Matrix.Identity(4)
    steps.box(xk0, xb, y0, y1, za - 0.03 if k == 0 else zk0, zk1, "ek_concrete")
    steps.box(xk0 - 0.012, xk0 + 0.02, y0 + 0.02, y1 - 0.02, zk0 + 0.012, zk1 - 0.03, RISERS[(k + 3) % len(RISERS)])
steps.frame = col.frame = Matrix.Identity(4)
# its collision ramp, written directly (rising along +x)
col.wedges.append([(xa, y0, za - 0.2), (xb, y0, za - 0.2), (xb, y1, za - 0.2), (xa, y1, za - 0.2), (xa, y0, za), (xb, y0, zb), (xb, y1, zb), (xa, y1, za)])
col.quad([(xa, y1, za), (xa, y0, za), (xb, y0, zb), (xb, y1, zb)], "ek_floor")
col.quad([(xa, y0, za - 0.2), (xb, y0, za - 0.2), (xb, y0, zb), (xa, y0, za)], "ek_floor")
col.quad([(xb, y1, za - 0.2), (xa, y1, za - 0.2), (xa, y1, za), (xb, y1, zb)], "ek_floor")
col.quad([(xb, y0, za - 0.2), (xb, y1, za - 0.2), (xb, y1, zb), (xb, y0, zb)], "ek_floor")

for (x0, x1, y0, y1, z0, z1) in LOW_WALLS:
    steps.box(x0, x1, y0, y1, z0 - 0.05, z1 - 0.08, "ek_chb")
    steps.box(x0 - 0.05, x1 + 0.05, y0 - 0.05, y1 + 0.05, z1 - 0.08, z1, "ek_coping")
    col.box(x0, x1, y0, y1, z0 - 0.05, z1, "ek_floor")
for i, (x0, x1, y0, y1, top) in enumerate(LEDGES):
    steps.box(x0, x1, y0, y1, top - 0.14, top, "ek_concrete", top="ek_deck")
    col.box(x0, x1, y0, y1, top - 0.14, top, "ek_floor")
    s = -1 if x0 < 0 else 1
    n = max(2, int((y1 - y0) / 1.3))
    for k in range(n + 1):                                                                    # the beam ends that carry it
        y = y0 + 0.12 + k * (y1 - y0 - 0.24) / n
        xin, xout = (x0, x1 - 0.06) if s < 0 else (x0 + 0.06, x1)
        steps.box(xin, xout, y - 0.06, y + 0.06, top - 0.34, top - 0.13, "ek_concrete")

# ---------------------------------------------------------------- the houses
LOOKS_W = [dict(wall="ek_wall_mint", band="ek_band_green", ground="ek_wall_mint", roof="ek_deck"),
           dict(wall="ek_wall_cream", band="ek_band_red", ground="ek_chb_warm", upper="ek_planks_red", roof="ek_tin_rust"),
           dict(wall="ek_wall_butter", band="ek_band_mustard", ground="ek_wall_butter", roof="ek_deck"),
           dict(wall="ek_wall_salmon", band="ek_band_plum", ground="ek_wall_salmon", upper="ek_planks_cream", roof="ek_tin_green"),
           dict(wall="ek_wall_white", band="ek_band_teal", ground="ek_wall_white", roof="ek_deck"),
           dict(wall="ek_wall_lilac", band="ek_band_plum", ground="ek_wall_lilac", upper="ek_tin_grey", roof="ek_tin_red"),
           dict(wall="ek_wall_peach", band="ek_band_brown", ground="ek_wall_peach", roof="ek_tin_grey")]
LOOKS_E = [dict(wall="ek_wall_peach", band="ek_band_brown", ground="ek_wall_peach", roof="ek_deck"),
           dict(wall="ek_wall_aqua", band="ek_band_teal", ground="ek_wall_aqua", upper="ek_planks_brown", roof="ek_tin_grey"),
           dict(wall="ek_wall_cream", band="ek_band_green", ground="ek_wall_cream", upper="ek_wall_sage", roof="ek_tin_rust"),
           dict(wall="ek_wall_rose", band="ek_band_red", ground="ek_wall_rose", roof="ek_deck"),
           dict(wall="ek_wall_butter", band="ek_band_mustard", ground="ek_chb", upper="ek_planks_green", roof="ek_tin_red"),
           dict(wall="ek_wall_white", band="ek_band_green", ground="ek_wall_white", upper="ek_sawali", roof="ek_tin_green"),
           dict(wall="ek_wall_mint", band="ek_band_teal", ground="ek_wall_mint", roof="ek_tin_rust")]
for i, (y0, y1, top, base) in enumerate(WEST_H):
    blocked = [(b0 - y0, b1 - y0) for b0, b1 in WEST_BLOCK]
    house(shell, trim, cards, WEST(y0, front((y0 + y1) / 2)), y1 - y0, OUT - front((y0 + y1) / 2), base, top, rng, blocked, LOOKS_W[i], eave=0.16 if top - base > 3.4 else 0.0,
          side1=(AX - EX, Z1) if i == 1 else None, side0=(AX - EX, Z1) if i == 5 else None)
    col.box(-OUT, -front((y0 + y1) / 2), y0, y1, -1.0, top, "ek_floor")
for i, (y0, y1, top, base) in enumerate(EAST_H):
    blocked = [(y1 - b1, y1 - b0) for b0, b1 in EAST_BLOCK]
    house(shell, trim, cards, EAST(y1, front((y0 + y1) / 2)), y1 - y0, OUT - front((y0 + y1) / 2), base, top, rng, blocked, LOOKS_E[i], eave=0.16 if top - base > 3.4 else 0.0,
          side0=(AX - EX, Z1) if i == 1 else None, side1=(AX - EX, Z1) if i == 5 else None)
    col.box(front((y0 + y1) / 2), OUT, y0, y1, -1.0, top, "ek_floor")
for b in (shell, trim, cards):
    b.frame = Matrix.Identity(4)
# The house that ends the alley at its top (the hoop stands before it), and the two that flank the gate outside.
house(shell, trim, cards, NORTH(-EX - 0.5, AY + 0.02), 2 * EX + 1.0, 7.0, Z2, Z2 + 7.4, rng, [(5.4, 9.6)],
      dict(wall="ek_wall_cream", band="ek_band_red", ground="ek_wall_cream", upper="ek_planks_brown", roof="ek_tin_rust"), doors=2)
for b in (shell, trim, cards):
    b.frame = Matrix.Identity(4)

# ---------------------------------------------------------------- nothing is fixed across a window
# ⚠️ A THING ON A WALL GOES ON WALL (owner, 2026-10-09, of a lamp and an air conditioner over a window: "lamp and bridge sup on
# a window?"). The doors and windows are dealt by each house's own dice, and the wall fittings were typed at fixed spots
# without looking. Each one is now slid along its wall to the nearest place where no opening is behind it.
FITTING = {"ac_unit": (0.8, 0.75), "meter_box": (0.6, 1.35), "mailbox": (0.4, 0.7), "street_lamp_wall": (0.45, 0.6), "sign_brgy": (1.05, 0.85)}


def clear_of_openings(name, x, y, z):
    w, h = FITTING[name]

    def hit(yy):
        return any(abs((o[0] + o[1]) / 2 - x) < 0.3 and o[2] - 0.15 < yy + w / 2 and o[3] + 0.15 > yy - w / 2 and o[4] - 0.12 < z + h and o[5] + 0.12 > z
                   for o in OPENINGS)
    for k in range(0, 40):
        for sgn in (1, -1):
            yy = y + sgn * k * 0.1
            if abs(yy) < AY - 0.5 and front(yy) == front(y) and not hit(yy):
                return yy
    return y


def on_the_wall(name, x, y, z):
    """A fitting's height, kept under the top of the wall it is on. ⚠️ Two lamps were typed higher than the low houses they
    were put on and hung in the air over the roof (owner, 2026-10-09: "floating lamp post")."""
    hs = WEST_H if x < 0 else EAST_H
    top = next((t for (y0, y1, t, b_) in hs if y0 <= y <= y1), 99.0)
    return min(z, top - FITTING[name][1] - 0.35)


_placed = []
for (n, x, y, z, yaw) in PLACE:
    if n in FITTING and abs(abs(x) - front(y)) < 0.05:
        z = on_the_wall(n, x, y, z)
        y = clear_of_openings(n, x, y, z)
        z = on_the_wall(n, x, y, z)
    _placed.append((n, x, y, z, yaw))
PLACE = _placed

# ---------------------------------------------------------------- bridges, tarps, poles
BX = EX + 1.0
for (y0, y1, z) in BRIDGES:
    n = 9
    for k in range(n):                                                                        # separate planks, a hand's gap between
        xa, xb = -BX + 2 * BX * k / n, -BX + 2 * BX * (k + 1) / n
        steps.box(xa + 0.012, xb - 0.012, y0 + (0.03 if k % 3 == 1 else 0), y1 - (0.04 if k % 4 == 2 else 0), z + 0.04, z + 0.10, "ek_wood")
    for yy in (y0 + 0.12, y1 - 0.12):                                                         # two long bearers under the planks
        steps.box(-BX, BX, yy - 0.06, yy + 0.06, z - 0.10, z + 0.045, "ek_wood_dark")
    for sx in (-1, 1):
        steps.box(sx * (EX - 0.02), sx * (EX + 0.5), y0 - 0.25, y1 + 0.25, z - 0.26, z + 0.032, "ek_wood_dark")   # the beam on each roof's edge. ⚠️ Its top stands 3 cm
        # PROUD of the roof: it was level with it, and the two flickered (owner, 2026-10-09: "on the edges of the bridges there are z-fighting planks").
        # ⚠️ NO KNEE BRACES (owner, 2026-10-09, of the two under each bridge end: "weird z-fighting"). They were loose
        # sheets, not solid timbers, they flickered against each other, and they came down across whatever window
        # or air conditioner the wall had there. The bridge sits a metre onto each roof on its beam; that carries it.
    for k in range(7):                                                                        # the rail on the far side
        x = -BX + 0.1 + (2 * BX - 0.2) * k / 6
        steps.box(x - 0.04, x + 0.04, y1 - 0.09, y1 - 0.01, z + 0.09, z + 1.0, "ek_wood_dark")
    steps.box(-BX, BX, y1 - 0.10, y1 - 0.0, z + 0.93, z + 1.01, "ek_wood"); steps.box(-BX, BX, y1 - 0.085, y1 - 0.015, z + 0.5, z + 0.56, "ek_wood")
    col.box(-BX, BX, y0, y1, z - 0.1, z + 0.10, "ek_floor"); col.box(-BX, BX, y1 - 0.1, y1, z + 0.1, z + 1.0, "ek_floor")
PADS = []
PAD_HALF = 0.93
for (x0, x1, y0, y1, top, speed, trapal) in TARPS:
    floor_z = top - TARP_H
    for (px, py) in ((x0 + 0.07, y0 + 0.07), (x1 - 0.07, y0 + 0.07), (x0 + 0.07, y1 - 0.07), (x1 - 0.07, y1 - 0.07)):
        steps.box(px - 0.045, px + 0.045, py - 0.045, py + 0.045, floor_z - 0.03, top + 0.10, "ek_pipe_green")
    for (a0, a1, b0, b1) in ((x0, x1, y0 + 0.03, y0 + 0.10), (x0, x1, y1 - 0.10, y1 - 0.03)):
        steps.box(a0 + 0.02, a1 - 0.02, b0, b1, top - 0.10, top - 0.03, "ek_pipe_green")
    for (a0, a1) in ((x0 + 0.03, x0 + 0.10), (x1 - 0.10, x1 - 0.03)):
        steps.box(a0, a1, y0 + 0.02, y1 - 0.02, top - 0.11, top - 0.04, "ek_pipe_green")
    col.box(x0, x1, y0, y1, floor_z, top, "ek_floor")
    PADS.append(((x0 + x1) / 2, (y0 + y1) / 2, top - 0.03, PAD_HALF, speed, "pad_" + MAT[trapal][0]))
ROOF_AT = {}


def roof_at(x, y):
    hs = WEST_H if x < 0 else EAST_H
    for (y0, y1, top, base) in hs:
        if y0 <= y <= y1:
            return top
    return 0.0


for (name, x, y, z, yaw) in PLACE:                                                            # a bamboo pole under each end of a hung line
    span = {"banderitas_17m": 8.5, "banderitas_8m": 4.0, "laundry_line_6m": 3.0}.get(name)
    if not span:
        continue
    a = math.radians(yaw)
    for s in (-1, 1):
        px, py = x + s * span * math.cos(a), y + s * span * math.sin(a)
        if name.startswith("banderitas"):
            px = s * (front(py) + 0.45) if abs(px) > 7 else px
        foot = roof_at(px, py) if abs(px) > front(py) else (Z0 if py < -MID else Z1 if py < MID else Z2)
        if z - foot > 0.3:
            steps.box(px - 0.035, px + 0.035, py - 0.035, py + 0.035, foot - 0.02, z + 0.12, "ek_wood")
            steps.box(px - 0.08, px + 0.08, py - 0.08, py + 0.08, foot - 0.02, foot + 0.18, "ek_concrete")
col.box(7.2, 8.0, -6.6, -3.8, Z1, Z1 + 2.45, "ek_floor")                # the sari-sari store's front
for (name, x, y, z, yaw) in PLACE:
    if name in SOLID:
        hx, hy, h = SOLID[name]
        if abs(yaw) % 180 > 45:
            hx, hy = hy, hx
        col.box(x - hx, x + hx, y - hy, y + hy, z, z + h, "ek_floor")

# ---------------------------------------------------------------- wires overhead, chalk on the floor
def tube(b, pts, r, mat):
    """A square-section line through the points (wires, ropes)."""
    for a, c in zip(pts, pts[1:]):
        a, c = Vector(a), Vector(c)
        d = (c - a).normalized()
        side = d.cross(Vector((0, 0, 1)))
        side = side.normalized() if side.length > 1e-4 else Vector((1, 0, 0))
        up = side.cross(d).normalized()
        ring = [side * r + up * r, -side * r + up * r, -side * r - up * r, side * r - up * r]
        for k in range(4):
            p, q = ring[k], ring[(k + 1) % 4]
            b.quad([tuple(a + q), tuple(a + p), tuple(c + p), tuple(c + q)], mat, [(0, 0), (0, 0), (0, 0), (0, 0)])


def wire(b, a, c, sag, r=0.010, n=9):
    a, c = Vector(a), Vector(c)
    tube(b, [tuple(a.lerp(c, k / n) - Vector((0, 0, sag * 4 * (k / n) * (1 - k / n)))) for k in range(n + 1)], r, "ek_wire")


wires = Buf("alley_wires")
# The two short posts stood THROUGH the court's ledges (found by the route bake: the ledge's way points were shut in);
# they stand on the end terraces now, where no ledge is over them.
POLES = [(5.2, -30.0, 7.4), (4.6, -11.2, 6.85), (-4.6, -12.4, 4.85), (4.6, 11.0, 6.65), (-4.6, 11.6, 8.6), (-5.0, 30.0, 9.4)]
for a, c in zip(POLES, POLES[1:]):
    for k, (dz, sag) in enumerate(((0.0, 0.55), (-0.22, 0.75), (-0.4, 0.5))):
        off = Vector((0.06 * (k - 1), 0.05 * (k - 1), dz))
        wire(wires, Vector(a) + off, Vector(c) + off, sag)
for (px, py, pz), drops in ((POLES[1], [(-5.02, -12.0, 4.6), (5.02, -15.0, 2.3), (-5.02, -9.4, 4.4)]), (POLES[2], [(-8.02, -6.0, 3.3), (8.02, -5.0, 5.3), (-8.02, -2.0, 5.2)]),
                            (POLES[3], [(8.02, 6.0, 5.2), (8.02, 1.0, 3.3), (-8.02, 5.8, 3.2)]), (POLES[4], [(-5.02, 10.0, 6.2), (5.02, 10.4, 6.1), (-5.02, 15.0, 4.2), (4.0, 17.5, 6.8)])):
    for d in drops:
        wire(wires, (px, py, pz - 0.5), d, 0.35, r=0.008)
# Piko (hopscotch) drawn in chalk on the low terrace, and a second by the hoop.
for (cx, cy, cz, turn) in ((-1.4, -13.6, Z0, 0.0), (1.6, 12.6, Z2, math.pi)):
    w, l = 0.75, 2.6
    c, sn = math.cos(turn), math.sin(turn)
    pts = [(cx + c * a - sn * b_, cy + sn * a + c * b_, cz + 0.008) for a, b_ in ((-w, -l), (w, -l), (w, l), (-w, l))]
    cards.quad(pts, "ek_piko", [(0, 0), (1, 0), (1, 1), (0, 1)])

# THE BOUNCE TARP'S SHEET IS THE JUMP PAD'S OWN MODEL (JumpPad.Model: parts pad_base, pad_cushion, pad_chevron, pad_ring,
# painted by one texture, the trapal). The cushion is the striped sheet that squashes; the frame it hangs in is static art.
padc = bpy.data.collections.new("Pad")
bpy.context.scene.collection.children.link(padc)
PAD_OBJS = []


def pad_part(name, build, flat_uv=None):
    b = Buf(name)
    build(b)
    if flat_uv:
        b.uv = [flat_uv] * len(b.uv)
    else:                                               # the sheet: metres onto the right half (2 m of trapal, 2 by 2 tiles)
        b.uv = [(0.5 + ((u + H) / 2.0) * 0.5, (v + H) / 2.0) for (u, v) in b.uv]
    o = b.obj(0.0, padc, shift=False)
    PAD_OBJS.append(o)


H = 0.93
pad_part("pad_cushion", lambda b: b.box(-H, H, -H, H, 0.0, 0.03, "ek_trapal_red"))
pad_part("pad_base", lambda b: [b.box(sx * (H - 0.04), sx * (H + 0.02), -H, H, -0.02, 0.035, "ek_trapal_red") for sx in (-1, 1)], (0.5 + 0.19 * 0.25, 0.25))
# ⚠️ THE RISING FRAMES AND THE CHEVRONS ARE THE PAVEMENT PAD'S OWN, FROM ILALIM NG TULAY (owner, 2026-10-09, of the first
# cut, where I had drawn a chevron lying flat: "jump pad vfx chevrons are facing up. i suggest for the jump pad vfx you
# use the vfx of the jump pads from ilalim ng tulay"). Their meshes are taken from that prop as they are, sized to this
# sheet, and painted the tarp's off-white (every UV on one cream stripe: the pad wears ONE texture, the trapal).
bpy.ops.import_scene.gltf(filepath=str(ROOT / "Assets/TumbangPreso/Resources/Map/JumpPad/jump_pad.glb"))
for o in list(bpy.context.selected_objects):
    keep = o.type == "MESH" and (o.name.startswith("pad_chevron") or o.name.startswith("pad_ring"))
    if not keep:
        continue
    # ⚠️ ITS OWN SHAPE ONLY, NOT WHERE IT STOOD IN THE PROP: JumpPad places the parts itself, so a chevron that brought its
    # height with it floated at twice that ("chevron vfx on the trampoline here is too high").
    o.data.transform(o.matrix_world.to_3x3().to_4x4())
    o.data.transform(Matrix.Scale(H / 0.85, 4))
    o.matrix_world = Matrix.Identity(4)
    o.parent = None
    for layer in o.data.uv_layers:                              # its own painting, which is the left half of the pad's texture
        for d in layer.data:
            d.uv = (d.uv[0] * 0.5, d.uv[1])
    for c in list(o.users_collection):
        c.objects.unlink(o)
    padc.objects.link(o)
    o.name = "pad_chevron" if o.name.startswith("pad_chevron") else "pad_ring"
    o.data.name = o.name
    PAD_OBJS.append(o)
for o in list(bpy.context.scene.objects):                       # whatever else came in with it
    if o.name.startswith("pad_") and o not in PAD_OBJS:
        bpy.data.objects.remove(o, do_unlink=True)
print("[alley] pad parts:", sorted(o.name for o in PAD_OBJS))

# ---------------------------------------------------------------- way points for the bots (Runtime/Map/MapRoutes.cs)
# A bot walks straight at its goal. These are the points it is sent through when the straight line cannot be walked:
# the foot and head of every stair, both ends of each bridge, every ledge and roof, each tarp and where it lands a body,
# a few on each floor, and the two alley-side corners of everything that stands against a wall (the things a bot
# hugging a wall used to walk into). Unity bakes which pairs can be walked; nothing here says so. NODE 0 IS THE CAN'S FLOOR.
ROUTE, ROUTE_PADS = [(0.0, 2.0, Z1)], []


def node(x, y, z):
    ROUTE.append((x, y, z)); return len(ROUTE) - 1


def floor_at(x, y):
    if abs(x) > front(y):
        return roof_at(x, y)
    return Z0 if y < -MID else Z1 if y <= MID else Z2


for (x, y) in ((-4, -4), (4, -4), (-4, 4), (4, 4), (0, -6.5), (0, 6.5), (-6, 0), (6, 0), (-6.3, -7.6), (6.3, -7.6), (-6.3, 7.6), (6.3, 7.6),
               (0, -12.0), (0, -15.5), (-2.5, -13.0), (3.0, -11.5), (0, 12.0), (0, 15.0), (2.5, 13.5), (-2.0, 11.5)):
    node(x, y, floor_at(x, y))
for (x0, x1, ya, yb, za, zb, style) in STAIRS:
    d = 1 if yb > ya else -1
    lanes = (-3.0, 0.0, 3.0) if x1 - x0 > 6 else ((x0 + x1) / 2,)
    for x in lanes:
        # The foot's point keeps a body's width off whatever wall stands past it (the two court stairs begin at a house's
        # flank: a point 0.6 m before them was inside the house, and nothing could be routed up either stair).
        fy = ya - d * 0.6
        if 6.5 < abs(x) < AX and za == Z1:
            fy = max(-MID + 0.9, min(MID - 0.9, fy))
        node(x, fy, za); node(x, ya + d * 0.35, za + (zb - za) * 0.35 / abs(yb - ya)); node(x, yb - d * 0.15, zb)
        if not (x0 == -5.0 and yb < -16):                      # the low end's roof stair ends at the map's end: no floor past it
            node(x, yb + d * 0.6, zb)
node(-5.7, -16.4, 2.6)                                           # ... its roof is stepped onto sideways
xa, xb, y0, y1, za, zb = XSTAIR
node(xa - 0.6, (y0 + y1) / 2, za); node(xb - 0.15, (y0 + y1) / 2, zb); node(xb + 0.6, (y0 + y1) / 2, zb)
for s_ in (-1, 1):
    for (y0, y1, top, base) in (WEST_H if s_ < 0 else EAST_H):
        f = front((y0 + y1) / 2)
        node(s_ * (f + OUT) / 2, (y0 + y1) / 2, top); node(s_ * (f + 0.9), (y0 + y1) / 2, top)
        node(s_ * (f + 0.9), y0 + 0.8, top); node(s_ * (f + 0.9), y1 - 0.8, top)
for (x0, x1, y0, y1, top) in LEDGES:
    xm = (x0 + x1) / 2
    node(xm, (y0 + y1) / 2, top)
    if y1 - y0 > 2:
        node(xm, y0 + 0.5, top); node(xm, y1 - 0.5, top)
for (y0, y1, z) in BRIDGES:
    for sx in (-1, 1):
        node(sx * (EX + 1.7), (y0 + y1) / 2 - 0.1, z); node(sx * (EX - 0.6), (y0 + y1) / 2 - 0.1, z + 0.1)
for (x0, x1, y0, y1, top, speed, trapal), land in zip(TARPS, ((4.35, -15.7, 2.5), (-6.2, 14.6, 4.4), (-10.6, 9.5, 6.6))):
    ROUTE_PADS.append((node((x0 + x1) / 2, (y0 + y1) / 2, top), node(*land)))
# Things against the walls: (x0, x1, y0, y1). A node off each alley-side corner.
AGAINST = [(min(x0, x1), max(x0, x1), min(ya, yb), max(ya, yb)) for (x0, x1, ya, yb, za, zb, st) in STAIRS[2:5]]
AGAINST += [(x0, x1, y0, y1) for (x0, x1, y0, y1, top, sp, tr) in TARPS[:2]] + [(7.2, 8.0, -6.6, -3.8)]
for (name, x, y, z, yaw) in PLACE:
    if name in SOLID and abs(x) < front(y) and name not in ("tricycle",) and abs(y) < AY:
        hx, hy, h = SOLID[name]
        AGAINST.append((x - max(hx, hy), x + max(hx, hy), y - max(hx, hy), y + max(hx, hy)))
for (x0, x1, y0, y1) in AGAINST:
    xs = x1 + 0.75 if (x0 + x1) / 2 < 0 else x0 - 0.75           # the side toward the middle of the alley
    for y in (y0 - 0.75, y1 + 0.75):
        if abs(y) < AY - 0.4 and abs(xs) < front(y) - 0.4:
            node(xs, y, floor_at(xs, y))
node(XSTAIR[0] - 0.75, XSTAIR[2] - 0.75, Z2); node(XSTAIR[1] + 0.3, XSTAIR[2] - 0.75, Z2)
print("[alley] route way points:", len(ROUTE), "pads:", len(ROUTE_PADS))

# ---------------------------------------------------------------- the district beyond the bounds
drng = random.Random(77)


def far_house(x0, x1, y0, y1, base, h, face):
    """A neighbour: a painted box with a pitched tin or tile roof and a few window cards on the side that faces us."""
    wall = drng.choice(WALLS + ["ek_chb", "ek_chb_warm", "ek_planks_brown", "ek_planks_cream", "ek_tin_grey"])
    roofm = drng.choice(["ek_tin_rust", "ek_tin_rust", "ek_tin_grey", "ek_tin_green", "ek_tin_red", "ek_tiles", "ek_tiles"])
    district.box(x0, x1, y0, y1, base - 0.5, base + h, wall, skip="zZ")
    along_x = (x1 - x0) > (y1 - y0)
    rise = drng.uniform(0.7, 1.5)
    e = 0.3
    if along_x:
        ym = (y0 + y1) / 2
        district.quad([(x0 - e, y0 - e, base + h - 0.1), (x1 + e, y0 - e, base + h - 0.1), (x1 + e, ym, base + h + rise), (x0 - e, ym, base + h + rise)], roofm)
        district.quad([(x1 + e, y1 + e, base + h - 0.1), (x0 - e, y1 + e, base + h - 0.1), (x0 - e, ym, base + h + rise), (x1 + e, ym, base + h + rise)], roofm)
        district.quad([(x0, y1, base + h), (x0, y0, base + h), (x0, ym, base + h + rise)], wall)
        district.quad([(x1, y0, base + h), (x1, y1, base + h), (x1, ym, base + h + rise)], wall)
    else:
        xm = (x0 + x1) / 2
        district.quad([(x0 - e, y1 + e, base + h - 0.1), (x0 - e, y0 - e, base + h - 0.1), (xm, y0 - e, base + h + rise), (xm, y1 + e, base + h + rise)], roofm)
        district.quad([(x1 + e, y0 - e, base + h - 0.1), (x1 + e, y1 + e, base + h - 0.1), (xm, y1 + e, base + h + rise), (xm, y0 - e, base + h + rise)], roofm)
        district.quad([(x0, y0, base + h), (x1, y0, base + h), (xm, y0, base + h + rise)], wall)
        district.quad([(x1, y1, base + h), (x0, y1, base + h), (xm, y1, base + h + rise)], wall)
    # window cards on the face toward the alley, 2 cm proud, one row per storey
    storeys = max(1, int(h / 2.7))
    for s in range(storeys):
        zc = base + 1.5 + s * 2.7
        if zc + 0.7 > base + h:
            break
        if face in ("x", "X"):
            xf = x0 - 0.02 if face == "x" else x1 + 0.02
            n = max(1, int((y1 - y0) / 2.6))
            for k in range(n):
                yc = y0 + (k + 0.5) * (y1 - y0) / n
                pts = [(xf, yc + 0.55, zc - 0.55), (xf, yc - 0.55, zc - 0.55), (xf, yc - 0.55, zc + 0.55), (xf, yc + 0.55, zc + 0.55)]
                dtrim.quad(pts if face == "x" else pts[::-1], drng.choice(["ek_jalousie", "ek_slide"]), [(0, 0), (1, 0), (1, 1), (0, 1)] if face == "x" else [(1, 1), (1, 0), (0, 0), (0, 1)])
        else:
            yf = y0 - 0.02 if face == "y" else y1 + 0.02
            n = max(1, int((x1 - x0) / 2.6))
            for k in range(n):
                xc = x0 + (k + 0.5) * (x1 - x0) / n
                pts = [(xc - 0.55, yf, zc - 0.55), (xc + 0.55, yf, zc - 0.55), (xc + 0.55, yf, zc + 0.55), (xc - 0.55, yf, zc + 0.55)]
                dtrim.quad(pts if face == "y" else pts[::-1], drng.choice(["ek_jalousie", "ek_slide"]), [(0, 0), (1, 0), (1, 1), (0, 1)] if face == "y" else [(1, 1), (1, 0), (0, 0), (0, 1)])


def near_house(fr, length, depth, h):
    """A NEIGHBOUR JUST OUTSIDE THE BOUNDS IS A REAL HOUSE (owner, 2026-10-09, from a roof in play: "outer houses are too
    obviously too simple"). They were painted boxes with one window card; from the roofs they are the whole view. So
    each is built by the same `house` as the alley's own: a concrete frame, a belt at every storey, pushed-in windows
    with sills and awnings on its front AND both flanks, and then either a pitched tin or tile roof with gables and
    deep eaves, or a flat deck with a parapet."""
    info = house(shell, trim, cards, fr, length, depth, 0.0, h, drng, doors=0, side0=(depth, 0.0), side1=(depth, 0.0),
                 look=dict(roof=drng.choice(["ek_deck", "ek_tin_grey"])))
    for b in (shell, trim, cards):
        b.frame = fr
    if drng.random() < 0.62:
        roofm = drng.choice(["ek_tin_rust", "ek_tin_rust", "ek_tin_green", "ek_tin_red", "ek_tiles", "ek_tiles", "ek_tin_grey"])
        rise, e, z = drng.uniform(0.9, 1.6), 0.38, h + 0.02
        mid = depth / 2
        shell.quad([(-e, -e, z - 0.12), (length + e, -e, z - 0.12), (length + e, mid, z + rise), (-e, mid, z + rise)], roofm)
        shell.quad([(length + e, depth + e, z - 0.12), (-e, depth + e, z - 0.12), (-e, mid, z + rise), (length + e, mid, z + rise)], roofm)
        shell.quad([(length + e, -e, z - 0.17), (-e, -e, z - 0.17), (-e, mid, z + rise - 0.05), (length + e, mid, z + rise - 0.05)], "ek_wood_dark")
        shell.quad([(-e, depth + e, z - 0.17), (length + e, depth + e, z - 0.17), (length + e, mid, z + rise - 0.05), (-e, mid, z + rise - 0.05)], "ek_wood_dark")
        wm = info["upper"]
        shell.quad([(0, depth, z - 0.14), (0, 0, z - 0.14), (0, mid, z + rise - 0.08)], wm)
        shell.quad([(length, 0, z - 0.14), (length, depth, z - 0.14), (length, mid, z + rise - 0.08)], wm)
        trim.box(-e, length + e, -e - 0.03, -e + 0.03, z - 0.21, z - 0.09, "ek_wood_dark")          # the fascia board on the eave
        trim.box(-e, length + e, mid - 0.07, mid + 0.07, z + rise - 0.03, z + rise + 0.06, "ek_wood_dark")   # the ridge cap
    else:
        for (a0, a1, b0, b1) in ((0, length, 0.0, 0.16), (0, length, depth - 0.16, depth), (0, 0.16, 0.16, depth - 0.16), (length - 0.16, length, 0.16, depth - 0.16)):
            trim.box(a0, a1, b0, b1, h - 0.02, h + 0.62, "ek_chb", top="ek_coping")
        if drng.random() < 0.7:                                                                    # a stair-head hut on the deck
            u0, w0 = drng.uniform(0.5, max(0.6, length - 3.0)), drng.uniform(0.6, max(0.7, depth - 3.2))
            wm = drng.choice(WALLS)
            shell.box(u0, u0 + 2.4, w0, w0 + 2.6, h - 0.02, h + 2.3, wm, top="ek_tin_rust")
            trim.box(u0 - 0.15, u0 + 2.55, w0 - 0.15, w0 + 2.75, h + 2.3, h + 2.38, "ek_tin_rust")
    for b in (shell, trim, cards):
        b.frame = Matrix.Identity(4)


# The row that stands right behind the play houses on each side: mostly a storey taller, so the roofs have a backdrop
# and the bound behind them is a wall and not thin air.
for s in (-1, 1):
    y = -AY
    while y < AY + 6:
        ln = drng.uniform(4.5, 8.0)
        top_here = roof_at(s * 10, min(AY, y + ln / 2))
        h = max(top_here + drng.choice([-1.2, 1.4, 2.2, 3.0, 3.6]), 3.0)
        d = drng.uniform(6, 10)
        near_house(WEST(y, OUT + 0.08) if s < 0 else EAST(y + ln - 0.05, OUT + 0.08), ln - 0.05, d, h)
        y += ln
# Rings of houses further out, growing taller and paler in the haze; a gap is left for the street at the south.
for ring in range(1, 6):
    r0 = OUT + 9 + (ring - 1) * 11
    for s in (-1, 1):
        y = -AY - 9 - ring * 6
        while y < AY + 12 + ring * 9:
            ln = drng.uniform(5, 10)
            if not (-27.2 < y + ln / 2 < -18.0):
                h = drng.uniform(3.0, 6.5) + ring * drng.uniform(0.4, 1.7)
                d = drng.uniform(6, 9.5)
                x0 = r0 + drng.uniform(0, 1.5)
                if ring == 1:
                    near_house(WEST(y, x0) if s < 0 else EAST(y + ln - 0.3, x0), ln - 0.3, d, h)
                else:
                    far_house(*((-x0 - d, -x0) if s < 0 else (x0, x0 + d)), y, y + ln - 0.3, 0.0, h, "X" if s < 0 else "x")
            y += ln
for ring in range(0, 5):                                                                      # north of the end house, and south across the street
    yn = AY + 7.6 + ring * 10
    ys = -27.6 - ring * 10
    x = -OUT - 20 - ring * 8
    while x < OUT + 20 + ring * 8:
        ln = drng.uniform(5, 10)
        if ring == 0:
            near_house(NORTH(x, yn), ln - 0.3, drng.uniform(6, 9), drng.uniform(5.0, 8.5))
            near_house(SOUTH(x + ln - 0.3, ys), ln - 0.3, drng.uniform(6, 9), drng.uniform(3.4, 6.8))
        else:
            far_house(x, x + ln - 0.3, yn, yn + drng.uniform(6, 9), 0.0, drng.uniform(4.5, 8.0) + ring * 1.3, "y")
            far_house(x, x + ln - 0.3, ys - drng.uniform(6, 9), ys, 0.0, drng.uniform(3.2, 6.5) + ring * 1.2, "Y")
        x += ln
# The corner houses on the street, either side of the gate (they hide the play houses' bare south walls from the street).
# A parapet where the house behind a roof is lower than it (the bound there must look like something).
for s in (-1, 1):
    for (y0, y1, top, base) in (WEST_H if s < 0 else EAST_H):
        steps.box(s * (OUT - 0.16), s * OUT, y0 + 0.02, y1 - 0.02, top - 0.02, top + 0.55, "ek_chb", top="ek_coping")
for (xa, xb) in ((-OUT, -AX), (AX, OUT)):                                                      # and across the two ends of the roofs
    for (yy, hs) in ((-AY, 0), (AY, -1)):
        top = (WEST_H if xa < 0 else EAST_H)[hs][2]
        if yy < 0:
            steps.box(xa + 0.02, xb - 0.02, yy, yy + 0.16, top - 0.02, top + 0.55, "ek_chb", top="ek_coping")

# ---------------------------------------------------------------- objects
kit = bpy.data.collections.new("Alley")
bpy.context.scene.collection.children.link(kit)
objs = {
    "alley_ground": ground.obj(0.0, kit), "alley_houses": shell.obj(0.0, kit), "alley_trim": trim.obj(0.018, kit),
    "alley_cards": cards.obj(0.0, kit), "alley_steps": steps.obj(0.016, kit), "district": district.obj(0.0, kit),
    "district_trim": dtrim.obj(0.0, kit), "alley_wires": wires.obj(0.0, kit),
}
colc = bpy.data.collections.new("Collision")
bpy.context.scene.collection.children.link(colc)
col_obj = col.obj(0.0, colc)
col_obj.display_type = "WIRE"; col_obj.hide_render = True
padc.hide_render = True
for o in PAD_OBJS:
    o.hide_render = True
for n, o in objs.items():
    print("[alley] %-14s %6d faces, %d materials" % (n, len(o.data.polygons), len(o.data.materials)))

# Props for the review pictures: appended from the props kit, when it exists.
props_blend = SOURCE / "props.blend"
placed = bpy.data.collections.new("Props")
bpy.context.scene.collection.children.link(placed)
missing = set()
if props_blend.exists():
    with bpy.data.libraries.load(str(props_blend), link=False) as (src, dst):
        dst.collections = [c for c in src.collections if c.startswith("prop_")]
    have = {c.name: c for c in bpy.data.collections if c.name.startswith("prop_")}
    for (name, x, y, z, yaw) in PLACE:
        c = have.get("prop_" + name)
        if not c:
            missing.add(name); continue
        e = bpy.data.objects.new("place_" + name, None)
        e.instance_type, e.instance_collection = "COLLECTION", c
        e.location, e.rotation_euler = (x, y, z - DOWN), (0, 0, math.radians(yaw))
        placed.objects.link(e)
print("[alley] props missing from the kit:", sorted(missing) if props_blend.exists() else "(no props.blend yet)")

# ---------------------------------------------------------------- light, cameras, save
sc = bpy.context.scene
ids = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items]
sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in ids else "BLENDER_EEVEE"
w = bpy.data.worlds.new("sky"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.70, 0.90, 1)
w.node_tree.nodes["Background"].inputs[1].default_value = 0.55
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); sc.collection.objects.link(sun)
sun.data.energy, sun.data.color, sun.data.angle = 5.6, (1.0, 0.86, 0.66), math.radians(2)
# Late afternoon: 32 degrees up, from the south-west, raking up the alley and across it onto the east houses.
SUN_DIR = Vector((-0.50, -0.62, 0.60)).normalized()                                           # toward the sun
sun.rotation_euler = (-SUN_DIR).to_track_quat("-Z", "Y").to_euler()
sc.view_settings.view_transform = "Standard"
try:
    sc.eevee.use_shadows = True
except Exception:
    pass
sc.render.resolution_x, sc.render.resolution_y = 1600, 900
EYE = 1.25
CAMS = [  # name, eye, look, vertical field of view (95 across is about 63 up and down at 16:9)
    ("air", (30, -40, 36), (0, 0, 2), 34), ("plan", (0, 0.01, 70), (0, 0, 0), None),
    ("eye_south", (0.8, -15.6, Z0 + EYE), (0, 0, 2.8), 63), ("eye_can", (1.6, -1.2, Z1 + EYE), (-1, 9.5, Z1 + 2.4), 63),
    ("eye_top", (-3.0, 15.6, Z2 + EYE), (0.5, 0, 1.6), 63), ("eye_west", (-6.0, -4.0, Z1 + EYE), (8, 3, 3.4), 63),
    ("eye_east", (6.0, 5.0, Z1 + EYE), (-8, -2, 3.6), 63), ("roof_south", (-9.6, -11.2, 5.0 + EYE), (3, 6, 1.8), 63),
    ("roof_north", (10.4, 11.0, 6.6 + EYE), (-4, -6, 1.6), 63), ("gate", (2.2, -27.0, 1.6), (0, -12, 3.4), 55),
    ("stairs", (-1.0, -13.4, Z0 + EYE), (-1.6, -8.5, 1.0), 50),
    ("stair_side", (-4.6, -7.6, Z1 + EYE), (-7.4, -6.0, Z1 + 1.0), 50), ("roof_stair", (-11.6, -5.0, 3.5 + EYE), (-9.0, -5.2, 4.4), 50),
    ("rise_s", (2.0, -11.6, Z0 + EYE), (5.4, -8.8, 0.6), 50), ("rise_n", (-1.5, 6.0, Z1 + EYE), (-5.4, 9.3, 1.3), 50),
]
for (name, eye, look, fov) in CAMS:
    cd = bpy.data.cameras.new(name); co = bpy.data.objects.new("cam_" + name, cd); sc.collection.objects.link(co)
    eye, look = (eye[0], eye[1], eye[2] - DOWN), (look[0], look[1], look[2] - DOWN)
    co.location = eye
    co.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat("-Z", "Y").to_euler()
    cd.clip_end = 600
    if fov is None:
        cd.type, cd.ortho_scale = "ORTHO", 46
    else:
        cd.sensor_fit, cd.angle_y = "VERTICAL", math.radians(fov)
SOURCE.mkdir(parents=True, exist_ok=True); LOGS.mkdir(parents=True, exist_ok=True); MODELS.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "eskinita_alley.blend"), compress=True)

# ---------------------------------------------------------------- export
if EXPORT:
    for n, o in list(objs.items()) + [("collision", col_obj)]:
        bpy.ops.object.select_all(action="DESELECT")
        o.hide_render = False
        o.select_set(True); bpy.context.view_layer.objects.active = o
        bpy.ops.export_scene.gltf(filepath=str(MODELS / (n + ".glb")), export_format="GLB", use_selection=True, export_apply=True, export_yup=True,
                                  export_image_format="NONE", export_materials="EXPORT")
    col_obj.hide_render = True

    def U(x, y, z):
        return [round(-x, 4), round(z - DOWN, 4), round(-y, 4)]
    bpy.ops.object.select_all(action="DESELECT")
    for o in PAD_OBJS:
        o.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(MODELS / "pad_trapal.glb"), export_format="GLB", use_selection=True, export_apply=True, export_yup=True,
                              export_image_format="NONE", export_materials="EXPORT")
    names = sorted({m for b in (ground, shell, trim, cards, steps, district, dtrim, wires) for m in b.mats})
    layout = {
        "gameplay": {"half_x": OUT, "half_z": AY, "can": U(0, 0, Z1), "box_y": 0.0, "wall_height": 18},
        "materials": [{"name": n, "texture": MAT[n][0], "tint": list(MAT[n][1]), "tiling": MAT[n][2], "foliage": False, "emissive": False,
                       "glossy": False, "cutout": n in CUTOUT} for n in names],
        "placements": [{"model": n, "position": [0, 0, 0], "yaw": 0, "scale": 1.0} for n in objs]
        + [{"model": "prop_" + n, "position": U(x, y, z), "yaw": -yaw, "scale": 1.0} for (n, x, y, z, yaw) in PLACE]
        + [{"model": n, "position": U(x, y, -0.05 + (Z2 if y > MID else Z1 if y > -MID else Z0)), "yaw": -yaw, "scale": sc_} for (n, x, y, yaw, sc_) in TREES],
        "pads": [{"position": U(x, y, z), "radius": round(r, 3), "speed": v, "model": "pad_trapal", "paint": t, "prefix": "pad_", "model_half": PAD_HALF}
                 for (x, y, z, r, v, t) in PADS],
        "spawns": [U(0, 0, Z1 + 0.1), U(-3, 9.0, Z1 + 0.6), U(0, -9.0, Z0 + 0.6), U(3, 9.0, Z1 + 0.6)],
        "probe_stairs": [{"foot": U((x0 + x1) / 2, ya, za), "head": U((x0 + x1) / 2, yb, zb)} for (x0, x1, ya, yb, za, zb, st) in STAIRS]
        + [{"foot": U(XSTAIR[0], (XSTAIR[2] + XSTAIR[3]) / 2, XSTAIR[4]), "head": U(XSTAIR[1], (XSTAIR[2] + XSTAIR[3]) / 2, XSTAIR[5])}],
        "probe_bridges": [{"foot": U(-BX, (y0 + y1) / 2 - 0.1, z + 0.1), "head": U(BX, (y0 + y1) / 2 - 0.1, z + 0.1)} for (y0, y1, z) in BRIDGES],
        "probe_pad_targets": [2.5 - DOWN, 4.4 - DOWN, 6.6 - DOWN],
        "routes": {"nodes": [{"p": U(*n)} for n in ROUTE], "pads": [{"a": a, "b": b} for (a, b) in ROUTE_PADS]},
        "solids": {"boxes": [{"c": U((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), "s": [round(x1 - x0, 4), round(z1 - z0, 4), round(y1 - y0, 4)]}
                             for (x0, x1, y0, y1, z0, z1) in col.boxes],
                   "wedges": [{"p": [c for pt in w for c in U(*pt)]} for w in col.wedges]},
        "review": [{"name": n, "at": U(*e), "look": U(*l), "fov": 95 if f == 63 else (f or 60)} for (n, e, l, f) in CAMS if f],
    }
    import shutil
    kanto = ROOT / "Assets/TumbangPreso/Art/Kanto"
    for n in sorted({t[0] for t in TREES}):
        shutil.copyfile(kanto / "Models" / (n + ".glb"), MODELS / (n + ".glb"))
    for t in ("bark_albedo.png", "bark_normal.png", "asphalt_albedo.png", "asphalt_normal.png"):
        shutil.copyfile(kanto / "Textures" / t, TEXTURES / t)
    layout["materials"] += [{"name": n, "texture": "leaf", "tint": list(c), "tiling": 1.0, "foliage": True, "emissive": False, "glossy": False,
                             "cutout": False} for n, c in TREE_MATS.items()]
    layout["materials"] += [{"name": n, "texture": "bark", "tint": [1, 1, 1], "tiling": 1.0, "foliage": False, "emissive": False, "glossy": False,
                             "cutout": False} for n in ("trunk", "trunk_grey")]
    layout["materials"] += [{"name": "leaf_core", "texture": "", "tint": [0.2784, 0.4812, 0.2031], "tiling": 1.0, "foliage": False, "emissive": False,
                             "glossy": False, "cutout": False}]
    (ART / "eskinita_alley_layout.json").write_text(json.dumps(layout, indent=1), encoding="utf-8")
    print("[alley] exported", len(objs) + 1, "models and the layout;", len(names), "materials,", len(PLACE), "props placed")

if REVIEW:
    only = ARGV[ARGV.index("--cams") + 1].split(",") if "--cams" in ARGV else None
    for (name, eye, look, fov) in CAMS:
        if only and name not in only:
            continue
        sc.camera = bpy.data.objects["cam_" + name]
        sc.render.filepath = str(LOGS / ("alley_%s_v%d.png" % (name, REVIEW)))
        bpy.ops.render.render(write_still=True)
print("ALLEY_OK")
