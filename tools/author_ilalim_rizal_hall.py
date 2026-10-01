"""Model Rizal Hall, the hero building of the Ilalim ng Tulay rebuild (ILALIM-1.3).

  py -3 tools/author_ilalim_textures_rizal.py      # paint the textures first
  blender -b --python tools/author_ilalim_rizal_hall.py -- [--preview N] [--shots a,b]

Writes ArtSource/ilalim/rizal_hall.blend. With --preview N it then loads the blockout around it
for context (tools/author_ilalim_blockout.py's ilalim_blockout.blend, minus its own Rizal Hall
stand-ins, and the real LRT-1 guideway linked from lrt_kit.blend) and writes versioned renders to
Logs/ilalim-blender/rizal_<shot>_vN.png. The context is never saved into rizal_hall.blend.

Owner, 2026-09-29: the map is set at UP Manila's Padre Faura corner "cuz we wanna see our
school's Rizal Hall in the game", and later "we need RH to be more visible". So this is the ONE
hero building, and it must read from the court about 80 m away as well as in close-ups.

THE REFERENCE (docs/reports/ilalim-rework-2026-09-29/research.md section 3, and the Commons
photographs listed there): William E. Parsons, 1921. A three-storey quadrangle in ivory stucco
facing SOUTH onto Padre Faura across a lawn with the Oblation. A colonnade of tall round Ionic
columns spans the two lower storeys and carries an entablature with a dentil cornice and round
rosettes; the third storey rises behind it, with window aircon units standing on the cornice.
A red-brown hipped roof with deep eaves and exposed rafters, dark steel-sash windows in deep
reveals, iron balcony railings in the end bays, maroon serif "RIZAL HALL" over a moulded
entrance, front steps facing the Oblation.

WHERE IT STANDS: the footprint is OSM's "Rizal Hall" polygon from ArtSource/ilalim/osm_layout.json
after author_ilalim_blockout.sightline_override() (the compound moved 26 m east so the hall reads
from the court). The polygon is rectilinear to within a few cm, turned 1.43 degrees from the
map's axes, so the building is modelled in its own frame (u along the front to the east, w
inward to the north) and every object carries the same location and rotation. The frame's origin
is on the front wall line, straight behind the Oblation, which is also the portico's centre
line: the portico faces the statue, as the steps do in the photographs.

THE HOUSE STYLE (KANTO_DESIGN_GUIDE.md section 2, LAGOON_REWORK_GUIDE.md section 2, and the LRT
kit tools/author_ilalim_lrt.py, whose fillet() and rounded_rect() are imported here):
  * real, editable models built by this script, few objects named by role, each with a live
    Bevel modifier (hardened normals);
  * CHUNKY AND ORGANIC, NEVER FIDDLY: columns 1.0 m across, a 1.9 m entablature, 20 cm dentils,
    fat volutes; the detail lives in the painted textures;
  * NO TWO SURFACES SHARE A PLANE: attached parts penetrate 2 to 5 cm; the roof is ONE welded
    shell (an exact boolean union of one hipped solid per wing), so intersecting wings never
    stack coplanar slopes or fascias;
  * the walls are ONE welded shell per building: each facade is a grid whose window cells are
    recessed 34 cm with real reveals, not boxes stuck on a wall;
  * world-scale UVs, except the window sashes and the door, which are drawn per unit and mapped
    0..1 per opening (four sash variants, chosen per window).

DECISIONS FOR THE OWNER:
  * The lettering is at its real place and roughly its real size, over the entrance behind the
    columns (0.5 m type, fitted to 2.5 m, on a panel that fits the central intercolumn). It reads
    from Padre Faura, not from the court. The
    blockout's 1 m letters on the entablature were a stand-in; the building itself (the red roof,
    the colonnade, the dark windows) is what carries from the court.
  * The Oblation is the UP Manila one (2023 photograph): a dark bronze figure, arms out, on a
    craggy bronze pedestal and a round granite drum. It is stylized and chunky: a smooth
    clothed-looking silhouette, no anatomy.
  * The review renders use a sun from the south-west (late afternoon just after the September
    equinox, when Manila's sun is a little south), so the south front is lit. The map's final
    sun is the assembly's call.

GRIME (owner, on the LRT kit: "theres no dirt or grime on what you have"). Drawn, positional,
multiplied through two extra UV maps as on the LRT kit, with this kit's own drawings:
  * UVGrime maps rizal_grime_sill: on every wall cell, u is the position in its window BAY (the
    window centred, one of four variants per bay) and v is metres below the nearest ledge above:
    a sill band (row 0: stains under the sill, where rain and aircon drip run) or the cornice
    (row 1: a continuous band with broader tongues);
  * UVSplash maps rizal_grime_splash: metres above the ground, on the plinth, the lower walls,
    the steps' cheeks, the column bases and the Oblation's drum.
The plinth is also painted darker (rizal_plinth).

THE OBJECTS (collection "rizal_hall", plus "rizal_oblation"):
  rizal_walls, rizal_plinth, rizal_bands (sill courses and the main cornice), rizal_sills,
  rizal_roof (one welded shell: slopes, fascia gutter, eave soffit), rizal_roof_caps (ridge and
  hip caps), rizal_rafters, rizal_downpipes, rizal_portico (podium, steps, ceiling slab),
  rizal_columns, rizal_entablature (with dentils and rosettes), rizal_entrance (door frame and
  name panel), rizal_lettering, rizal_balconies, rizal_aircon, rizal_oblation_base,
  rizal_oblation_figure.
"""
import json
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
import author_ilalim_blockout as BLOCK  # noqa: E402  (read-only: layout, sightline_override)
from author_ilalim_lrt import fillet, rounded_rect  # noqa: E402

SOURCE = ROOT / "ArtSource" / "ilalim"
TEXTURES = SOURCE / "textures"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"
TILE_M = 4.0
UP = Vector((0, 0, 1))

# ------------------------------------------------------------------ the numbers
G = BLOCK.LOT_TOP                  # the compound's ground, 0.24
F1 = G + 1.0                       # the raised ground floor (the plinth and podium top)
F2 = F1 + 4.8
F3 = F2 + 4.8
SOFFIT = F3 + 4.2                  # eave soffit, 15.04
EAVE_TOP = SOFFIT + 0.3            # top of the fascia gutter
WALL_TOP = EAVE_TOP                # walls run up into the roof shell
WALL_BOTTOM = G - 0.25
OVERHANG = 1.3
PITCH = math.radians(24)
WIN_W = 1.7
REVEAL = 0.34
BAY = 3.6
# (floor, sill, head) per storey. The third storey's sill clears the portico roof.
STOREYS = [(F1, F1 + 0.9, F1 + 3.4), (F2, F2 + 0.9, F2 + 3.4), (F3, F3 + 1.2, F3 + 3.4)]
SILL_DROP = 0.2                    # the ledge under each sill that the stains hang from
CORNICE_BOTTOM = SOFFIT - 0.62
DRIP_TOPS = [s - SILL_DROP for _, s, _ in STOREYS]
# The portico
COLS_U = [-12.6, -9.0, -5.4, -1.8, 1.8, 5.4, 9.0, 12.6]
COL_W = -2.9                       # column centres, in front of the wall (w < 0 is south)
ENT_FRONT = -3.4                   # the entablature's outer face
ENT_HALF = 13.4
ARCH_BOTTOM = 9.7
ENT_TOP = 11.62
PODIUM_FRONT = -3.95
DOOR_W, DOOR_H = 2.4, 3.4

# Material: (texture, tint or None, normal strength, grime overlays?). Tints are written as the
# colour one means on screen (sRGB) and converted to linear in material(); written straight into
# the node they washed out (review v1: the iron rails rendered mid-grey, the maroon went salmon).
MATERIALS = {
    "rizal_stucco":   ("rizal_stucco", None, 0.4, True),
    "rizal_reveal":   ("rizal_stucco", (0.93, 0.92, 0.9), 0.4, False),
    "rizal_trim":     ("rizal_trim", None, 0.4, True),
    "rizal_column":   ("rizal_column", None, 0.4, True),
    "rizal_plinth":   ("rizal_plinth", None, 0.4, True),
    "rizal_steps":    ("rizal_steps", None, 0.5, True),
    "rizal_roof":     ("rizal_roof", None, 0.8, False),
    "rizal_roof_cap": ("rizal_roof", (0.84, 0.82, 0.82), 0.5, False),
    "rizal_eave":     ("rizal_eave", None, 0.4, False),
    "rizal_rafter":   ("rizal_eave", (0.93, 0.92, 0.9), 0.3, False),
    "rizal_gutter":   ("rizal_paint", (0.42, 0.16, 0.14), 0.3, False),
    "rizal_sash":     ("rizal_sash", None, 0.0, False),
    "rizal_door":     ("rizal_door", None, 0.0, False),
    "rizal_iron":     ("rizal_paint", (0.2, 0.21, 0.2), 0.3, False),
    "rizal_ac":       ("rizal_paint", (0.95, 0.94, 0.9), 0.3, True),
    "rizal_ac_grille": ("rizal_paint", (0.45, 0.45, 0.44), 0.3, False),
    "rizal_letter":   (None, (0.46, 0.1, 0.12), 0.0, False),
    "rizal_bronze":   ("rizal_bronze", None, 0.4, False),
    "rizal_stone":    ("rizal_stone", None, 0.4, True),
}


ANTI_TILE = {"rizal_roof", "rizal_roof_cap"}


def material(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    tex, tint, strength, grimed = MATERIALS[name]
    if tint is not None:
        tint = tuple(c ** 2.2 for c in tint)
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.4 if name == "rizal_bronze" else 0.85
    if name == "rizal_bronze":
        bsdf.inputs["Metallic"].default_value = 0.35
    if tex is None:
        bsdf.inputs["Base Color"].default_value = (*tint, 1)
        m.diffuse_color = (*tint, 1)
        return m
    uv = nodes.new("ShaderNodeUVMap")
    uv.uv_map = "UVMap"
    albedo = nodes.new("ShaderNodeTexImage")
    albedo.image = bpy.data.images.load(str(TEXTURES / f"{tex}_albedo.png"), check_existing=True)
    links.new(uv.outputs["UV"], albedo.inputs["Vector"])
    colour = albedo.outputs["Color"]
    if name in ANTI_TILE:
        # The same texture again at another scale DOWN THE SLOPE only (so the ribs stay straight),
        # blended in through a big soft noise mask: the 4 m repeat read as polka dots from the
        # air (review v3).
        remap = nodes.new("ShaderNodeMapping")
        remap.inputs["Scale"].default_value = (1.0, 0.61, 1)
        remap.inputs["Location"].default_value = (0.37, 0.71, 0)
        links.new(uv.outputs["UV"], remap.inputs["Vector"])
        second = nodes.new("ShaderNodeTexImage")
        second.image = albedo.image
        links.new(remap.outputs["Vector"], second.inputs["Vector"])
        noise = nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = 0.3
        noise.inputs["Detail"].default_value = 0.0
        links.new(uv.outputs["UV"], noise.inputs["Vector"])
        ramp = nodes.new("ShaderNodeMapRange")
        ramp.inputs["From Min"].default_value, ramp.inputs["From Max"].default_value = 0.4, 0.6
        links.new(noise.outputs["Fac"], ramp.inputs["Value"])
        blend = nodes.new("ShaderNodeMix")
        blend.data_type = "RGBA"
        links.new(ramp.outputs["Result"], blend.inputs["Factor"])
        links.new(colour, blend.inputs[6])
        links.new(second.outputs["Color"], blend.inputs[7])
        colour = blend.outputs[2]
    if tint is not None:
        mix = nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(colour, mix.inputs[6])
        mix.inputs[7].default_value = (*tint, 1)
        colour = mix.outputs[2]
    if grimed:
        for image, layer in (("rizal_grime_sill", "UVGrime"), ("rizal_grime_splash", "UVSplash")):
            guv = nodes.new("ShaderNodeUVMap")
            guv.uv_map = layer
            gtex = nodes.new("ShaderNodeTexImage")
            gtex.image = bpy.data.images.load(str(TEXTURES / f"{image}.png"), check_existing=True)
            gtex.image.colorspace_settings.name = "Non-Color"
            links.new(guv.outputs["UV"], gtex.inputs["Vector"])
            mul = nodes.new("ShaderNodeMix")
            mul.data_type, mul.blend_type = "RGBA", "MULTIPLY"
            mul.inputs["Factor"].default_value = 1.0
            links.new(colour, mul.inputs[6])
            links.new(gtex.outputs["Color"], mul.inputs[7])
            colour = mul.outputs[2]
    links.new(colour, bsdf.inputs["Base Color"])
    npath = TEXTURES / f"{tex}_normal.png"
    if strength > 0 and npath.exists():
        normal = nodes.new("ShaderNodeTexImage")
        normal.image = bpy.data.images.load(str(npath), check_existing=True)
        normal.image.colorspace_settings.name = "Non-Color"
        links.new(uv.outputs["UV"], normal.inputs["Vector"])
        nmap = nodes.new("ShaderNodeNormalMap")
        nmap.inputs["Strength"].default_value = strength
        links.new(normal.outputs["Color"], nmap.inputs["Color"])
        links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
    m.diffuse_color = (0.8, 0.76, 0.66, 1) if tint is None else (*tint, 1)
    return m


# ------------------------------------------------------------------ the footprint and frame

class Frame:
    """The building's own frame: u along the front wall to the east, w inward to the north, z up.
    `origin` is the world point on the front wall line straight behind the Oblation."""

    def __init__(self, poly, oblation):
        # The front edge: the long edge with the lowest mean world y.
        edges = [(poly[i], poly[(i + 1) % len(poly)]) for i in range(len(poly))]
        front = min((e for e in edges if math.dist(*e) > 20), key=lambda e: e[0][1] + e[1][1])
        (ax, ay), (bx, by) = front
        if bx < ax:
            (ax, ay), (bx, by) = (bx, by), (ax, ay)
        self.theta = math.atan2(by - ay, bx - ax)
        t = (oblation[0] - ax) / (bx - ax)
        self.origin = Vector((oblation[0], ay + t * (by - ay), 0.0))
        self.matrix = Matrix.Translation(self.origin) @ Matrix.Rotation(self.theta, 4, "Z")
        self.inverse = self.matrix.inverted()

    def local(self, x, y):
        v = self.inverse @ Vector((x, y, 0))
        return v.x, v.y

    def world(self, u, w, z=0.0):
        return self.matrix @ Vector((u, w, z))


def rectify(poly):
    """Square the OSM polygon up in the local frame: drop collinear points, then give each
    near-horizontal edge one w and each near-vertical edge one u (the mean of its ends). OSM's
    tracing wobbles by up to 0.3 m; the real building is square."""
    pts = [list(p) for p in poly]
    changed = True
    while changed:
        changed = False
        for i in range(len(pts)):
            a, b, c = pts[i - 1], pts[i], pts[(i + 1) % len(pts)]
            d1 = (b[0] - a[0], b[1] - a[1])
            d2 = (c[0] - b[0], c[1] - b[1])
            cross = d1[0] * d2[1] - d1[1] * d2[0]
            if abs(cross) < 0.05 * math.hypot(*d1) * math.hypot(*d2):
                pts.pop(i)
                changed = True
                break
    n = len(pts)
    for i in range(n):
        a, b = pts[i], pts[(i + 1) % n]
        if abs(b[0] - a[0]) > abs(b[1] - a[1]):
            a[1] = b[1] = (a[1] + b[1]) / 2
        else:
            a[0] = b[0] = (a[0] + b[0]) / 2
    front = min(p[1] for p in pts)
    pts = [[round(x, 3), round(y - front, 3)] for x, y in pts]
    area = sum(pts[i][0] * pts[(i + 1) % n][1] - pts[(i + 1) % n][0] * pts[i][1] for i in range(n)) / 2
    if area < 0:
        pts.reverse()
    return [tuple(p) for p in pts]


def wings(poly):
    """The wings as rectangles (u0, u1, w0, w1), each coordinate snapped to the squared footprint.
    Their union is the footprint; each gets its own hipped roof, and the roofs are welded."""
    us = sorted({p[0] for p in poly})
    ws = sorted({p[1] for p in poly})

    def su(v):
        return min(us, key=lambda x: abs(x - v))

    def sw(v):
        return min(ws, key=lambda x: abs(x - v)) if min(abs(x - v) for x in ws) < 0.5 else v

    rough = [
        ("front", -26.4, 30.7, 0.0, 23.3),     # the front range, with the portico
        ("west", -26.4, -14.1, 0.0, 26.8),     # its deeper west end
        ("east", 11.1, 30.7, 0.0, 50.0),       # the east wing, running back to the north range
        ("annex", 16.8, 27.0, 40.0, 54.3),     # the stair block at the north-east corner
        ("north", -14.3, 16.8, 49.5, 62.7),    # the north range, closing the courtyard
    ]
    out = [(name, su(a), su(b), sw(c), sw(d)) for name, a, b, c, d in rough]
    # A guard: every wing must lie inside the footprint (the OSM data changing would move them).
    for name, a, b, c, d in out:
        for x, y in ((a + 0.2, c + 0.2), (b - 0.2, c + 0.2), (b - 0.2, d - 0.2), (a + 0.2, d - 0.2)):
            assert BLOCK.point_in_poly(x, y, poly), f"wing {name} leaves the footprint at {(x, y)}"
    return out


class Edge:
    def __init__(self, a, b):
        self.a = Vector((a[0], a[1], 0))
        d = Vector((b[0] - a[0], b[1] - a[1], 0))
        self.L = d.length
        self.t = d / self.L
        self.n = Vector((self.t.y, -self.t.x, 0))       # outward, for a counter-clockwise footprint

    def at(self, s, z, d=0.0):
        return self.a + self.t * s + self.n * d + Vector((0, 0, z))


def offset_ring(poly, d, z):
    """The footprint offset outward by d (mitred), at height z."""
    n = len(poly)
    out = []
    for i in range(n):
        p0, p1, p2 = Vector(poly[i - 1]), Vector(poly[i]), Vector(poly[(i + 1) % n])
        e0, e1 = (p1 - p0).normalized(), (p2 - p1).normalized()
        n0, n1 = Vector((e0.y, -e0.x)), Vector((e1.y, -e1.x))
        m = (n0 + n1) / (1 + n0.dot(n1))
        q = p1 + m * d
        out.append(Vector((q.x, q.y, z)))
    return out


# ------------------------------------------------------------------ the mesh buffer

class RBuf:
    """One object's worth of geometry in a bmesh, with material indices and per-face UV control:
    `fix` gives a face explicit UVs (per vertex), `grime` its UVGrime coordinates, and `splash_z`
    the ground height the UVSplash map measures from (None: no splash)."""

    CLEAN_GRIME = (0.002, 0.998)
    CLEAN_SPLASH = (0.5, 0.995)

    def __init__(self, name, splash_z=None):
        self.name, self.bm, self.mats = name, bmesh.new(), []
        self.fix, self.grime = {}, {}
        self.splash_z = splash_z
        self.grime_fn = None          # optional function(face) -> {vert: uv}

    def mi(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def face(self, cos, mat, expect=None):
        f = self.bm.faces.new([self.bm.verts.new(c) for c in cos])
        f.normal_update()
        if expect is not None and f.normal.dot(expect) < 0:
            f.normal_flip()
        f.material_index = self.mi(mat)
        return f

    def loft(self, rings, mat, cap=True, mat_of=None):
        vs = [[self.bm.verts.new(c) for c in ring] for ring in rings]
        k = len(rings[0])
        faces = []
        for r0, r1 in zip(vs, vs[1:]):
            for j in range(k):
                faces.append(self.bm.faces.new((r0[j], r0[(j + 1) % k], r1[(j + 1) % k], r1[j])))
        if cap:
            faces.append(self.bm.faces.new(list(reversed(vs[0]))))
            faces.append(self.bm.faces.new(vs[-1]))
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        for f in faces:
            f.material_index = self.mi(mat_of(f) if mat_of else mat)
        return faces

    def box(self, e, s0, s1, d0, d1, z0, z1, mat):
        """A box in an edge's frame: s along the wall, d outward, z up."""
        ring = lambda z: [e.at(s0, z, d0), e.at(s1, z, d0), e.at(s1, z, d1), e.at(s0, z, d1)]
        return self.loft([ring(z0), ring(z1)], mat)

    def abox(self, u0, u1, w0, w1, z0, z1, mat):
        """An axis-aligned box in the building frame."""
        ring = lambda z: [Vector((u0, w0, z)), Vector((u1, w0, z)), Vector((u1, w1, z)), Vector((u0, w1, z))]
        return self.loft([ring(z0), ring(z1)], mat)

    def prism(self, profile_uv, z0, z1, mat, top_scale=1.0, centre=(0.0, 0.0), mat_of=None):
        cx, cy = centre
        bottom = [Vector((cx + x, cy + y, z0)) for x, y in profile_uv]
        top = [Vector((cx + x * top_scale, cy + y * top_scale, z1)) for x, y in profile_uv]
        return self.loft([bottom, top], mat, mat_of=mat_of)

    def revolve(self, centre, profile, mat, sides=20, squash=(1.0, 1.0)):
        """Rings of radius r at height z around a vertical axis at `centre` (u, w)."""
        cu, cw = centre
        rings = []
        for r, z in profile:
            rings.append([Vector((cu + r * squash[0] * math.cos(a), cw + r * squash[1] * math.sin(a), z))
                          for a in (k / sides * math.tau for k in range(sides))])
        return self.loft(rings, mat)

    def tube(self, path, radius, mat, sides=8, radii=None):
        rings = []
        for i, p in enumerate(path):
            d = (path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]).normalized()
            side = d.cross(UP)
            if side.length < 1e-4:
                side = d.cross(Vector((1, 0, 0)))
            side.normalize()
            up = side.cross(d).normalized()
            r = radii[i] if radii else radius
            rings.append([p + (side * math.cos(a) + up * math.sin(a)) * r
                          for a in (k / sides * math.tau for k in range(sides))])
        return self.loft(rings, mat)

    def sweep(self, path, profile, mat, closed=False, mat_of=None):
        """A closed (d, z) profile swept along a 2D path (u, w), mitred at the corners, d outward
        (to the right of the direction of travel)."""
        n = len(path)
        rings = []
        for i in range(n):
            p = Vector(path[i])
            if closed or 0 < i < n - 1:
                p0, p2 = Vector(path[i - 1]), Vector(path[(i + 1) % n])
                e0, e1 = (p - p0).normalized(), (p2 - p).normalized()
                n0, n1 = Vector((e0.y, -e0.x)), Vector((e1.y, -e1.x))
                m = (n0 + n1) / (1 + n0.dot(n1))
            else:
                e = (Vector(path[1]) - Vector(path[0])) if i == 0 else (p - Vector(path[i - 1]))
                e.normalize()
                m = Vector((e.y, -e.x))
            rings.append([Vector((p.x + m.x * d, p.y + m.y * d, z)) for d, z in profile])
        if closed:
            rings.append([v.copy() for v in rings[0]])
        return self.loft(rings, mat, cap=not closed, mat_of=mat_of)

    # -------------------------------------------------------------- UVs and the object
    def write_uvs(self):
        bm = self.bm
        uv = bm.loops.layers.uv.new("UVMap")
        gl = bm.loops.layers.uv.new("UVGrime")
        sl = bm.loops.layers.uv.new("UVSplash")
        for f in bm.faces:
            f.normal_update()
            n = f.normal
            fix = self.fix.get(f)
            gr = self.grime.get(f)
            if gr is None and self.grime_fn:
                gr = self.grime_fn(f)
            side = abs(n.z) <= 0.7
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            for l in f.loops:
                co = l.vert.co
                if fix is not None:
                    l[uv].uv = fix[l.vert]
                elif not side:
                    l[uv].uv = (co.x / TILE_M, co.y / TILE_M)
                else:
                    l[uv].uv = (co.dot(t) / TILE_M, co.z / TILE_M)
                l[gl].uv = gr[l.vert] if gr is not None else self.CLEAN_GRIME
                if side and self.splash_z is not None:
                    l[sl].uv = (co.dot(t) / 8.0, min(0.995, max(0.0, (co.z - self.splash_z) / 1.5)))
                else:
                    l[sl].uv = self.CLEAN_SPLASH

    def finish(self, col, frame, bevel=0.03, segments=2, smooth_area=0.35, weld=True):
        self.write_uvs()
        if weld:
            bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=1e-4)
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        return make_object(col, self.name, mesh, self.mats, frame, bevel, segments, smooth_area)


def make_object(col, name, mesh, mats, frame, bevel=0.03, segments=2, smooth_area=0.35):
    for m in mats:
        mesh.materials.append(material(m))
    # Big flat faces are shaded flat; only the small rounded ones are smooth (the LRT kit's rule:
    # smooth shading over long faces drew diagonal streaks).
    for p in mesh.polygons:
        p.use_smooth = p.area < smooth_area
    obj = bpy.data.objects.new(name, mesh)
    col.objects.link(obj)
    obj.location = frame.origin
    obj.rotation_euler = (0, 0, frame.theta)
    if bevel:
        mod = obj.modifiers.new("Bevel", "BEVEL")
        mod.width, mod.segments = bevel, segments
        mod.limit_method, mod.angle_limit = "ANGLE", math.radians(35)
        mod.harden_normals, mod.use_clamp_overlap = True, True
    return obj


def collection(name, parent=None):
    c = bpy.data.collections.new(name)
    (parent or bpy.context.scene.collection).children.link(c)
    return c


# ------------------------------------------------------------------ the windows

class Opening:
    def __init__(self, edge, c, storey, width, z0, z1, kind="window", variant=0, depth=REVEAL):
        self.edge, self.c, self.storey, self.width = edge, c, storey, width
        self.z0, self.z1, self.kind, self.variant, self.depth = z0, z1, kind, variant, depth
        self.ac = False


def plan_openings(edges, front_index, rng):
    """Window columns per facade. The front follows the portico's bays; every other facade
    centres as many 3.6 m bays as fit with 2 m of solid wall at each end."""
    out = []
    for ei, e in enumerate(edges):
        if ei == front_index:
            # The frame's u equals s minus the front edge's start u.
            u0 = e.a.x
            us = [-10.8, -7.2, -3.6, 0.0, 3.6, 7.2, 10.8]
            u = 14.4
            while u + WIN_W / 2 + 1.0 < e.a.x + e.L:
                us.append(u)
                u += BAY
            u = -14.4
            while u - WIN_W / 2 - 3.0 > u0:
                us.append(u)
                u -= BAY
            cs = sorted(x - u0 for x in us)
        else:
            n = int((e.L - 2 * 2.0 - WIN_W) // BAY) + 1 if e.L >= 2 * 2.0 + WIN_W else 0
            cs = [e.L / 2 + (i - (n - 1) / 2) * BAY for i in range(n)]
        for c in cs:
            for k, (floor, sill, head) in enumerate(STOREYS):
                if ei == front_index and k == 0 and abs(c + e.a.x) < 0.01:
                    out.append(Opening(ei, c, k, DOOR_W, floor, floor + DOOR_H, "door", depth=0.5))
                    continue
                out.append(Opening(ei, c, k, WIN_W, sill, head, variant=rng.randrange(4)))
    return out


def recess(buf, e, o, ss, zs):
    """A window or door opening recessed o.depth into the wall, its reveals and back split at the
    facade grid's breakpoints `ss` and `zs`, so every vertex welds to its neighbour."""
    D = o.depth
    s0, s1, z0, z1 = ss[0], ss[-1], zs[0], zs[-1]
    for a, b in zip(ss, ss[1:]):
        buf.face([e.at(a, z0), e.at(b, z0), e.at(b, z0, -D), e.at(a, z0, -D)], "rizal_reveal", UP)
        buf.face([e.at(a, z1), e.at(b, z1), e.at(b, z1, -D), e.at(a, z1, -D)], "rizal_reveal", -UP)
    for a, b in zip(zs, zs[1:]):
        buf.face([e.at(s0, a), e.at(s0, a, -D), e.at(s0, b, -D), e.at(s0, b)], "rizal_reveal", e.t)
        buf.face([e.at(s1, a), e.at(s1, a, -D), e.at(s1, b, -D), e.at(s1, b)], "rizal_reveal", -e.t)
    mat = "rizal_door" if o.kind == "door" else "rizal_sash"
    for a, b in zip(ss, ss[1:]):
        for c, d in zip(zs, zs[1:]):
            back = buf.face([e.at(a, c, -D), e.at(b, c, -D), e.at(b, d, -D), e.at(a, d, -D)], mat, e.n)
            uvs = {}
            for v in back.verts:
                s = (v.co - e.a).dot(e.t)
                fu = (s - s0) / (s1 - s0)
                fv = (v.co.z - z0) / (z1 - z0)
                uvs[v] = (fu, fv) if o.kind == "door" else ((o.variant + min(0.999, max(0.001, fu))) / 4, fv)
            buf.fix[back] = uvs


def walls(col, frame, poly, edges, openings, rng):
    buf = RBuf("rizal_walls", splash_z=G)
    tops = sorted(DRIP_TOPS + [CORNICE_BOTTOM])
    for ei, e in enumerate(edges):
        ops = [o for o in openings if o.edge == ei]
        centres = sorted({round(o.c, 4) for o in ops})
        if not centres:
            n = max(1, round(e.L / BAY))
            centres = [e.L * (i + 0.5) / n for i in range(n)]
        variants = {c: rng.randrange(4) for c in centres}
        sb = {0.0, round(e.L, 4)}
        for o in ops:
            sb |= {round(o.c - o.width / 2, 4), round(o.c + o.width / 2, 4)}
        for a, b in zip(centres, centres[1:]):
            sb.add(round((a + b) / 2, 4))
        zb = {WALL_BOTTOM, WALL_TOP, *tops}
        for o in ops:
            zb |= {o.z0, o.z1}
        sb = sorted(x for x in sb if -1e-6 <= x <= e.L + 1e-6)
        zb = sorted({round(z, 4) for z in zb})
        for o in ops:
            lo, hi = o.c - o.width / 2, o.c + o.width / 2
            recess(buf, e, o, [x for x in sb if lo - 1e-3 <= x <= hi + 1e-3],
                   [z for z in zb if o.z0 - 1e-3 <= z <= o.z1 + 1e-3])
        for i in range(len(sb) - 1):
            s0, s1 = sb[i], sb[i + 1]
            if s1 - s0 < 1e-3:
                continue
            sc = (s0 + s1) / 2
            ck = min(centres, key=lambda c: abs(c - sc))
            for j in range(len(zb) - 1):
                z0, z1 = zb[j], zb[j + 1]
                zc = (z0 + z1) / 2
                if any(o.c - o.width / 2 - 1e-3 <= s0 and s1 <= o.c + o.width / 2 + 1e-3
                       and o.z0 - 1e-3 <= z0 and z1 <= o.z1 + 1e-3 for o in ops):
                    continue
                f = buf.face([e.at(s0, z0), e.at(s1, z0), e.at(s1, z1), e.at(s0, z1)], "rizal_stucco", e.n)
                top = min((t for t in tops if t >= zc), default=None)
                if top is None:
                    continue
                row = 1 if top == CORNICE_BOTTOM else 0
                var = variants[ck]
                g = {}
                for v in f.verts:
                    s = (v.co - e.a).dot(e.t)
                    uu = min(0.995, max(0.005, (s - ck) / 4.0 + 0.5))
                    g[v] = ((var + uu) / 4, (row * 4.5 + min(4.47, max(0.0, top - v.co.z))) / 9)
                buf.grime[f] = g
    # The wall top, hidden in the roof shell.
    buf.face([Vector((x, y, WALL_TOP)) for x, y in poly], "rizal_stucco", UP)
    return buf.finish(col, frame, bevel=0.03)


# ------------------------------------------------------------------ bands, plinth, sills

def bands(col, frame, poly, edges):
    """The plinth, the two sill courses and the main cornice: profiles swept round the footprint.
    Each profile is a closed loop (d outward, z) ordered so that its faces point outward."""
    plinth = RBuf("rizal_plinth", splash_z=G)
    prof = [(-0.08, WALL_BOTTOM), (0.14, WALL_BOTTOM), (0.14, F1 - 0.16), (0.06, F1 - 0.02), (-0.08, F1 - 0.02)]
    _ring_loft(plinth, poly, prof, "rizal_plinth")
    plinth.finish(col, frame, bevel=0.04)

    b = RBuf("rizal_bands")
    for _, sill, _ in STOREYS[1:]:
        z = sill
        prof = [(-0.06, z - SILL_DROP), (0.12, z - SILL_DROP), (0.18, z - 0.1), (0.18, z + 0.03), (-0.06, z + 0.03)]
        _ring_loft(b, poly, prof, "rizal_trim")
    zc = CORNICE_BOTTOM
    prof = [(-0.06, zc), (0.1, zc), (0.18, zc + 0.14), (0.18, zc + 0.3), (0.36, zc + 0.42), (0.36, SOFFIT + 0.05),
            (-0.06, SOFFIT + 0.05)]
    _ring_loft(b, poly, prof, "rizal_trim")
    b.finish(col, frame, bevel=0.03)


def _ring_loft(buf, poly, profile, mat):
    """Sweep a closed (d, z) profile round the footprint. The profile runs counter-clockwise in
    the (d, z) plane (outward along the bottom, up the face, back along the top), so with the
    footprint counter-clockwise the quads come out facing away from the section's centre."""
    k = len(poly)
    rings = [offset_ring(poly, d, z) for d, z in profile]
    vs = [[buf.bm.verts.new(c) for c in ring] for ring in rings]
    m = len(vs)
    cd = sum(d for d, _ in profile) / m
    cz = sum(z for _, z in profile) / m
    centre_ring = offset_ring(poly, cd, cz)
    for a in range(m):
        r0, r1 = vs[a], vs[(a + 1) % m]
        for j in range(k):
            f = buf.bm.faces.new((r0[j], r0[(j + 1) % k], r1[(j + 1) % k], r1[j]))
            f.normal_update()
            mid = (centre_ring[j] + centre_ring[(j + 1) % k]) / 2
            if (f.calc_center_median() - mid).dot(f.normal) < 0:
                f.normal_flip()
            f.material_index = buf.mi(mat)


def sills(col, frame, edges, openings):
    """Ground-floor sills, one chunky ledge per window (the upper floors sit on the courses)."""
    buf = RBuf("rizal_sills")
    for o in openings:
        if o.storey != 0 or o.kind != "window":
            continue
        e = edges[o.edge]
        buf.box(e, o.c - o.width / 2 - 0.16, o.c + o.width / 2 + 0.16, -0.06, 0.2, o.z0 - SILL_DROP, o.z0 + 0.03,
                "rizal_trim")
    buf.finish(col, frame, bevel=0.03)


# ------------------------------------------------------------------ the roof

def hip_solid(u0, u1, w0, w1, lift=0.0):
    """A closed hipped solid over the eave rectangle: flat soffit underneath, a vertical fascia,
    four slopes at PITCH meeting at a ridge."""
    bm = bmesh.new()
    a, b = u1 - u0, w1 - w0
    tp = math.tan(PITCH)
    z0, z1 = SOFFIT + lift, EAVE_TOP + lift
    bot = [bm.verts.new((u0, w0, z0)), bm.verts.new((u1, w0, z0)), bm.verts.new((u1, w1, z0)), bm.verts.new((u0, w1, z0))]
    eav = [bm.verts.new((u0, w0, z1)), bm.verts.new((u1, w0, z1)), bm.verts.new((u1, w1, z1)), bm.verts.new((u0, w1, z1))]
    if a >= b:
        h = z1 + b / 2 * tp
        r0 = bm.verts.new((u0 + b / 2, (w0 + w1) / 2, h))
        r1 = bm.verts.new((max(u1 - b / 2, u0 + b / 2 + 0.02), (w0 + w1) / 2, h))
        slopes = [(eav[0], eav[1], r1, r0), (eav[2], eav[3], r0, r1), (eav[1], eav[2], r1), (eav[3], eav[0], r0)]
    else:
        h = z1 + a / 2 * tp
        r0 = bm.verts.new(((u0 + u1) / 2, w0 + a / 2, h))
        r1 = bm.verts.new(((u0 + u1) / 2, max(w1 - a / 2, w0 + a / 2 + 0.02), h))
        slopes = [(eav[1], eav[2], r1, r0), (eav[3], eav[0], r0, r1), (eav[0], eav[1], r0), (eav[2], eav[3], r1)]
    faces = [bm.faces.new(list(reversed(bot)))]
    for i in range(4):
        faces.append(bm.faces.new((bot[i], bot[(i + 1) % 4], eav[(i + 1) % 4], eav[i])))
    for s in slopes:
        faces.append(bm.faces.new(s))
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    return bm


def roof(col, frame, poly, wing_rects):
    """ONE welded roof: the exact boolean union of a hipped solid per wing. Slopes take the
    roof texture with UVs running down the slope (the ribs follow the fall), the fascia is the
    maroon gutter band of the photographs, the underside is the eave soffit."""
    scratch = collection("scratch roof")
    objs = []
    for k, (name, u0, u1, w0, w1) in enumerate(wing_rects):
        bm = hip_solid(u0 - OVERHANG, u1 + OVERHANG, w0 - OVERHANG, w1 + OVERHANG)
        me = bpy.data.meshes.new(f"roof_{name}")
        bm.to_mesh(me)
        bm.free()
        o = bpy.data.objects.new(f"roof_{name}", me)
        scratch.objects.link(o)
        objs.append(o)
    base = objs[0]
    for o in objs[1:]:
        mod = base.modifiers.new(f"union {o.name}", "BOOLEAN")
        mod.operation, mod.solver, mod.object = "UNION", "EXACT", o
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(base.evaluated_get(dg))
    for o in objs:
        bpy.data.objects.remove(o)
    bpy.data.collections.remove(scratch)

    bm = bmesh.new()
    bm.from_mesh(me)
    bpy.data.meshes.remove(me)
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(0.5), verts=bm.verts, edges=bm.edges)
    mats = ["rizal_roof", "rizal_gutter", "rizal_eave"]
    uv = bm.loops.layers.uv.new("UVMap")
    gl = bm.loops.layers.uv.new("UVGrime")
    sl = bm.loops.layers.uv.new("UVSplash")
    cosp = math.cos(PITCH)
    for f in bm.faces:
        f.normal_update()
        n = f.normal
        if n.z > 0.3:
            f.material_index = 0
            t = UP.cross(n).normalized()
            dh = Vector((n.x, n.y, 0)).normalized()
            for l in f.loops:
                l[uv].uv = (l.vert.co.dot(t) / TILE_M, l.vert.co.dot(dh) / cosp / TILE_M)
        elif n.z < -0.7:
            f.material_index = 2
            for l in f.loops:
                l[uv].uv = (l.vert.co.x / TILE_M, l.vert.co.y / TILE_M)
        else:
            f.material_index = 1
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            for l in f.loops:
                l[uv].uv = (l.vert.co.dot(t) / TILE_M, l.vert.co.z / TILE_M)
        for l in f.loops:
            l[gl].uv = RBuf.CLEAN_GRIME
            l[sl].uv = RBuf.CLEAN_SPLASH
    # Ridge and hip caps along every CONVEX edge between two slopes (valleys get none).
    caps = RBuf("rizal_roof_caps")
    for e in bm.edges:
        if len(e.link_faces) != 2:
            continue
        fa, fb = e.link_faces
        if fa.normal.z <= 0.3 or fb.normal.z <= 0.3 or fa.normal.dot(fb.normal) > 0.995:
            continue
        mid = (e.verts[0].co + e.verts[1].co) / 2
        if (fa.calc_center_median() - mid).dot(fb.normal) > 0:
            continue                     # concave: a valley
        p0, p1 = e.verts[0].co.copy(), e.verts[1].co.copy()
        d = (p1 - p0).normalized()
        caps.tube([p0 - d * 0.05, p1 + d * 0.05], 0.13, "rizal_roof_cap", sides=8)
    mesh = bpy.data.meshes.new("rizal_roof")
    bm.to_mesh(mesh)
    bm.free()
    make_object(col, "rizal_roof", mesh, mats, frame, bevel=0.03, smooth_area=0.0)
    caps.finish(col, frame, bevel=0.02, smooth_area=0.5, weld=False)


def rafters(col, frame, poly, edges):
    """Exposed rafter tails under the eaves, every 0.9 m, kept clear of the corners."""
    buf = RBuf("rizal_rafters")
    n = len(poly)
    for i, e in enumerate(edges):
        s = 1.6
        while s < e.L - 1.6:
            buf.box(e, s - 0.08, s + 0.08, -0.1, OVERHANG - 0.1, SOFFIT - 0.24, SOFFIT + 0.03, "rizal_rafter")
            s += 0.9
    buf.finish(col, frame, bevel=0.025)


def downpipes(col, frame, edges, front_index):
    """Chunky maroon downpipes from the gutter to the ground, near the front corners and on the
    east wing: gooseneck under the eave, down the wall, a kick at the foot."""
    buf = RBuf("rizal_downpipes")
    spots = [(front_index, 0.9), (front_index, edges[front_index].L - 0.9)]
    for i, e in enumerate(edges):
        if i != front_index and e.L > 30:
            spots.append((i, e.L / 2 + 1.8))
    for ei, s in spots:
        e = edges[ei]
        path = [e.at(s, EAVE_TOP - 0.15, OVERHANG - 0.2), e.at(s, SOFFIT - 0.3, OVERHANG - 0.25),
                e.at(s, SOFFIT - 0.9, 0.16), e.at(s, G + 0.55, 0.16), e.at(s, G + 0.22, 0.34), e.at(s, G + 0.2, 0.5)]
        buf.tube(path, 0.075, "rizal_gutter", sides=10)
        for z in (F2 - 0.3, F3 - 0.3, 3.0):
            buf.box(e, s - 0.12, s + 0.12, -0.03, 0.27, z - 0.06, z + 0.06, "rizal_iron")
    buf.finish(col, frame, bevel=0.015, smooth_area=0.5, weld=False)


# ------------------------------------------------------------------ the portico

def portico(col, frame):
    buf = RBuf("rizal_portico", splash_z=G)

    def by_normal(f):
        return "rizal_steps" if f.normal.z > 0.7 else "rizal_plinth"

    # The podium: the raised ground floor carried out under the colonnade.
    buf.prism(rounded_rect(14.5, (0.2 - PODIUM_FRONT) / 2, 0.25), WALL_BOTTOM, F1,
              "rizal_plinth", centre=(0.0, (0.2 + PODIUM_FRONT) / 2), mat_of=by_normal)
    # Five risers of 0.2 down to the lawn, facing the Oblation; each step a little narrower.
    for k in range(4):
        top = F1 - 0.2 * (k + 1)
        front = PODIUM_FRONT - 0.34 * (k + 1)
        half = 8.6 - 0.12 * k
        buf.prism(rounded_rect(half, (PODIUM_FRONT + 0.1 - front) / 2, 0.08), WALL_BOTTOM, top, "rizal_steps",
                  centre=(0.0, (PODIUM_FRONT + 0.1 + front) / 2), mat_of=by_normal)
    # Cheek walls at the ends of the steps.
    for s in (-1, 1):
        buf.abox(s * 8.55 - 0.45, s * 8.55 + 0.45, PODIUM_FRONT - 1.45, PODIUM_FRONT + 0.1, WALL_BOTTOM, F1 - 0.12,
                 "rizal_plinth")
    # The portico's ceiling and roof slab, tucked inside the entablature and into the wall.
    slab = buf.abox(-ENT_HALF + 0.3, ENT_HALF - 0.3, ENT_FRONT + 0.3, 0.1, 10.02, ENT_TOP - 0.24, "rizal_trim")
    for f in slab:
        if f.normal.z < -0.7:
            f.material_index = buf.mi("rizal_eave")
    buf.finish(col, frame, bevel=0.04)


def columns(col, frame):
    """Eight Ionic columns, 1.0 m across, spanning the two lower storeys. Chunky: a square plinth,
    a fat torus base, a shaft with a little entasis, an echinus ring, two fat volute bolsters
    (their round ends face the street) with stepped eyes, and a square abacus."""
    shaft = RBuf("rizal_columns", splash_z=F1)
    cap = RBuf("rizal_capitals")
    rng = random.Random(7)
    base_top = F1 + 0.55
    shaft_top = ARCH_BOTTOM - 0.66
    for u in COLS_U:
        c = (u + rng.uniform(-0.01, 0.01), COL_W)
        cap.prism(rounded_rect(0.64, 0.64, 0.1), F1 - 0.03, F1 + 0.3, "rizal_trim", centre=c)
        cap.revolve(c, [(0.62, F1 + 0.28), (0.63, F1 + 0.36), (0.6, F1 + 0.44), (0.53, F1 + 0.47),
                        (0.54, F1 + 0.52), (0.5, base_top + 0.02)], "rizal_trim")
        rings = []
        for k in range(9):
            t = k / 8
            z = base_top + (shaft_top - base_top) * t
            r = 0.5 * (1 - 0.13 * t ** 1.4) * (1 + 0.012 * math.sin(math.pi * t))
            rings.append((r, z))
        shaft.revolve(c, rings, "rizal_column", sides=22)
        # The astragal and the echinus under the volutes.
        cap.revolve(c, [(0.47, shaft_top - 0.08), (0.5, shaft_top - 0.02), (0.48, shaft_top + 0.04),
                        (0.52, shaft_top + 0.1), (0.58, shaft_top + 0.2), (0.5, shaft_top + 0.26)], "rizal_trim")
        # The volutes: two bolsters running front to back, their round ends facing the street.
        zc = shaft_top + 0.3
        for s in (-1, 1):
            vu = c[0] + s * 0.5
            path = [Vector((vu, c[1] - 0.48, zc)), Vector((vu, c[1] + 0.48, zc))]
            cap.tube(path, 0.25, "rizal_trim", sides=14)
            for w_end, sign in ((c[1] - 0.48, -1), (c[1] + 0.48, 1)):
                cap.tube([Vector((vu, w_end - sign * 0.02, zc)), Vector((vu, w_end + sign * 0.04, zc))], 0.17,
                         "rizal_trim", sides=12)
                cap.tube([Vector((vu, w_end - sign * 0.02, zc)), Vector((vu, w_end + sign * 0.085, zc))], 0.075,
                         "rizal_trim", sides=10)
        # The canalis joining the volutes, and the abacus.
        cap.abox(c[0] - 0.5, c[0] + 0.5, c[1] - 0.44, c[1] + 0.44, zc - 0.04, zc + 0.26, "rizal_trim")
        cap.prism(rounded_rect(0.66, 0.62, 0.06), ARCH_BOTTOM - 0.18, ARCH_BOTTOM + 0.02, "rizal_trim", centre=c)
    shaft.finish(col, frame, bevel=0.02, smooth_area=1.0)
    cap.finish(col, frame, bevel=0.025, smooth_area=0.2)


def entablature(col, frame):
    """The entablature over the colonnade, returning to the wall at both ends: a two-fascia
    architrave, a plain frieze with round rosettes, a bed moulding, chunky dentils, the corona
    and its cymatium."""
    buf = RBuf("rizal_entablature")
    path = [(-ENT_HALF, 0.15), (-ENT_HALF, ENT_FRONT), (ENT_HALF, ENT_FRONT), (ENT_HALF, 0.15)]
    prof = [(-1.0, ARCH_BOTTOM), (0.0, ARCH_BOTTOM), (0.0, 9.98), (0.06, 10.02), (0.06, 10.2), (0.14, 10.26),
            (0.14, 10.32), (0.02, 10.34), (0.02, 10.92), (0.14, 10.96), (0.14, 11.02), (0.16, 11.04), (0.16, 11.24),
            (0.58, 11.28), (0.62, 11.52), (0.54, ENT_TOP), (-1.0, ENT_TOP)]
    faces = buf.sweep(path, prof, "rizal_trim")

    def grime(f):
        g = {}
        t = UP.cross(f.normal)
        t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
        for v in f.verts:
            g[v] = (v.co.dot(t) / 16.0, (4.5 + min(4.47, max(0.0, 11.24 - v.co.z))) / 9)
        return g
    for f in faces:
        if abs(f.normal.z) < 0.7 and f.calc_center_median().z < 11.24:
            buf.grime[f] = grime(f)
    buf.finish(col, frame, bevel=0.025)

    # Dentils along the three runs, and rosettes on the frieze.
    d = RBuf("rizal_dentils")
    runs = [((-ENT_HALF, -0.5), (-ENT_HALF, ENT_FRONT)), ((-ENT_HALF, ENT_FRONT), (ENT_HALF, ENT_FRONT)),
            ((ENT_HALF, ENT_FRONT), (ENT_HALF, -0.5))]
    for a, b in runs:
        e = Edge(a, b)
        count = int((e.L - 0.3) / 0.42)
        for k in range(count):
            s = 0.3 + k * 0.42
            d.box(e, s - 0.1, s + 0.1, 0.1, 0.4, 11.0, 11.3, "rizal_trim")
    # Rosettes: a fat disc, a raised ring and a boss, over every column and on the returns.
    front = Edge((-ENT_HALF, ENT_FRONT), (ENT_HALF, ENT_FRONT))
    spots = [(front, u + ENT_HALF) for u in COLS_U]
    for a, b in ((( -ENT_HALF, -0.3), (-ENT_HALF, ENT_FRONT)), ((ENT_HALF, ENT_FRONT), (ENT_HALF, -0.3))):
        e = Edge(a, b)
        spots.append((e, e.L * 0.55))
    zc = 10.63
    for e, s in spots:
        for r, d0, d1 in ((0.25, -0.02, 0.09), (0.17, 0.05, 0.13), (0.07, 0.1, 0.18)):
            p0, p1 = e.at(s, zc, d0), e.at(s, zc, d1)
            d.tube([p0, p1], r, "rizal_trim", sides=16)
    d.finish(col, frame, bevel=0.02, smooth_area=0.05, weld=False)


def entrance(col, frame, font_path):
    """The moulded entrance on the front wall at the portico's centre: pilasters, capitals, the
    name panel, two consoles and a cornice cap, with RIZAL HALL in maroon serif capitals."""
    buf = RBuf("rizal_entrance", splash_z=F1)
    front = Edge((-30.0, 0.0), (30.0, 0.0))
    s = 30.0
    top = F1 + DOOR_H
    # The whole frame fits the central intercolumn (1.3 m clear either side of the centre line),
    # so the columns never cut the name when seen square on (review v1).
    for side in (-1, 1):
        a, b = sorted((side * (DOOR_W / 2 - 0.05), side * (DOOR_W / 2 + 0.2)))
        buf.box(front, s + a, s + b, -0.05, 0.2, F1 - 0.03, top + 0.28, "rizal_trim")
        a2, b2 = sorted((side * (DOOR_W / 2 - 0.1), side * (DOOR_W / 2 + 0.26)))
        buf.box(front, s + a2, s + b2, -0.05, 0.27, top + 0.18, top + 0.42, "rizal_trim")
        # Scroll consoles under the cap's ends: a fat block with a rounded nose.
        c0, c1 = sorted((side * 1.36, side * 1.56))
        buf.box(front, s + c0, s + c1, -0.05, 0.3, top + 0.95, top + 1.44, "rizal_trim")
        cx = s + side * 1.46
        buf.tube([front.at(cx - 0.11, top + 1.02, 0.26), front.at(cx + 0.11, top + 1.02, 0.26)], 0.1, "rizal_trim", sides=12)
    # The name panel and the cornice cap.
    buf.box(front, s - 1.45, s + 1.45, -0.05, 0.22, top + 0.4, top + 1.42, "rizal_trim")
    buf.box(front, s - 1.72, s + 1.72, -0.05, 0.44, top + 1.4, top + 1.64, "rizal_trim")
    buf.finish(col, frame, bevel=0.03)

    curve = bpy.data.curves.new("RIZAL HALL", "FONT")
    curve.body = "RIZAL HALL"
    if font_path and Path(font_path).exists():
        curve.font = bpy.data.fonts.load(font_path, check_existing=True)
    curve.size = 0.5
    curve.align_x, curve.align_y = "CENTER", "CENTER"
    curve.extrude = 0.04
    curve.bevel_depth = 0.008
    curve.bevel_resolution = 1
    curve.resolution_u = 4
    tmp = bpy.data.objects.new("tmp text", curve)
    bpy.context.scene.collection.objects.link(tmp)
    tmp.rotation_euler = (math.pi / 2, 0, 0)
    tmp.location = (0.0, -0.24, top + 0.9)
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(tmp.evaluated_get(dg))
    # Fit the name inside the panel: 2.5 m wide at most.
    xs = [v.co.x for v in me.vertices]
    k = min(1.0, 2.5 / (max(xs) - min(xs)))
    if k < 1.0:
        me.transform(Matrix.Diagonal((k, k, 1, 1)))
    me.transform(tmp.matrix_world)
    bpy.data.objects.remove(tmp)
    bpy.data.curves.remove(curve)
    me.name = "rizal_lettering"
    bm = bmesh.new()
    bm.from_mesh(me)
    uvl = bm.loops.layers.uv.verify()
    for layer in ("UVGrime", "UVSplash"):
        lay = bm.loops.layers.uv.new(layer)
        for f in bm.faces:
            for l in f.loops:
                l[lay].uv = RBuf.CLEAN_GRIME
    for f in bm.faces:
        for l in f.loops:
            l[uvl].uv = (l.vert.co.x, l.vert.co.z)
    bm.to_mesh(me)
    bm.free()
    make_object(col, "rizal_lettering", me, ["rizal_letter"], frame, bevel=0, smooth_area=0.0)


def railing(buf, a, b, height=1.0):
    """A chunky iron railing from a to b (3D points at the base): a fat top rail, a bottom rail,
    square bars every 0.2 m and a heavier post every 1.2 m."""
    d = b - a
    L = d.length
    t = d / L
    buf.tube([a + UP * (height - 0.03), b + UP * (height - 0.03)], 0.045, "rizal_iron", sides=8)
    buf.tube([a + UP * 0.12, b + UP * 0.12], 0.03, "rizal_iron", sides=6)
    n = max(1, int(L / 0.2))
    side = Vector((t.y, -t.x, 0))
    for k in range(n + 1):
        p = a + t * (L * k / n)
        r = 0.035 if k % 6 else 0.05
        ring = lambda z: [p + t * sx * r + side * sy * r + UP * z for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        buf.loft([ring(-0.02), ring(height - 0.02)], "rizal_iron")


def balconies(col, frame, edges, front_index):
    """Iron-railed balconies: the second floor of the colonnade's end bays (between the end
    columns and the wall, as in the Commons photograph), and one on each end bay of the front."""
    slab = RBuf("rizal_balcony_slabs")
    iron = RBuf("rizal_balconies")
    z = F2
    for s in (-1, 1):
        u0, u1 = sorted((s * 9.42, s * 12.18))
        slab.abox(u0, u1, COL_W - 0.05, 0.1, z - 0.24, z + 0.02, "rizal_trim")
        railing(iron, Vector((u0 + 0.02, COL_W + 0.02, z)), Vector((u1 - 0.02, COL_W + 0.02, z)))
    e = edges[front_index]
    for u in (-21.6, 25.2):
        s = u - e.a.x
        slab.box(e, s - 1.3, s + 1.3, -0.1, 0.95, z - 0.24, z + 0.02, "rizal_trim")
        # Two chunky brackets under each slab.
        for k in (-0.9, 0.9):
            slab.box(e, s + k - 0.1, s + k + 0.1, -0.05, 0.8, z - 0.62, z - 0.2, "rizal_trim")
        a0, a1 = e.at(s - 1.24, z, 0.9), e.at(s + 1.24, z, 0.9)
        railing(iron, a0, a1)
        railing(iron, e.at(s - 1.24, z, 0.0), a0)
        railing(iron, a1, e.at(s + 1.24, z, 0.0))
    slab.finish(col, frame, bevel=0.03)
    iron.finish(col, frame, bevel=0.01, smooth_area=0.05, weld=False)


def aircon(col, frame, edges, openings, rng):
    """Window aircon units, the most Manila detail on the building: a casing that sits in the
    bottom of the sash and sticks out over the sill, a darker grille with three fat louvres, and
    two angle brackets. Split-type condensers stand on the portico roof, as in the photograph."""
    buf = RBuf("rizal_aircon")
    W, H = 0.72, 0.48
    for o in openings:
        if o.kind != "window" or rng.random() > 0.3:
            continue
        o.ac = True
        e = edges[o.edge]
        s = o.c + rng.uniform(-0.35, 0.35)
        z0 = o.z0 - 0.02
        out = 0.34 + rng.uniform(-0.03, 0.03)
        buf.box(e, s - W / 2, s + W / 2, -0.3, out, z0, z0 + H, "rizal_ac")
        buf.box(e, s - W / 2 + 0.06, s + W / 2 - 0.06, out - 0.03, out + 0.015, z0 + 0.06, z0 + H - 0.06, "rizal_ac_grille")
        for k in range(3):
            zz = z0 + 0.13 + k * 0.11
            buf.box(e, s - W / 2 + 0.1, s + W / 2 - 0.1, out, out + 0.035, zz - 0.022, zz + 0.022, "rizal_ac")
        for k in (-0.26, 0.26):
            buf.tube([e.at(s + k, z0 - 0.5, -0.03), e.at(s + k, z0 + 0.02, out - 0.06)], 0.025, "rizal_iron", sides=6)
    # Split condensers on the portico roof.
    front = Edge((-30.0, 0.0), (30.0, 0.0))
    for u in (-8.1, -3.3, 6.6):
        s = u + 30.0
        z0 = ENT_TOP - 0.26
        buf.box(front, s - 0.44, s + 0.44, 0.7, 1.05, z0 + 0.1, z0 + 0.72, "rizal_ac")
        buf.tube([front.at(s + 0.1, z0 + 0.41, 1.03), front.at(s + 0.1, z0 + 0.41, 1.075)], 0.23, "rizal_ac_grille", sides=16)
        for k in (-0.34, 0.34):
            buf.box(front, s + k - 0.05, s + k + 0.05, 0.66, 1.1, z0 - 0.02, z0 + 0.12, "rizal_iron")
    buf.finish(col, frame, bevel=0.012, smooth_area=0.05, weld=False)


# ------------------------------------------------------------------ the Oblation

def oblation(col, frame, at):
    """The UP Manila Oblation, stylized and chunky: a granite step and drum, a craggy bronze
    pedestal, and the figure standing on it, head up, arms flung out and a little raised,
    facing Padre Faura. A smooth clothed-looking silhouette with no anatomy."""
    cu, cw = at
    rng = random.Random(11)
    base = RBuf("rizal_oblation_base", splash_z=G)
    base.revolve((cu, cw), [(3.05, WALL_BOTTOM), (3.05, G + 0.18), (2.95, G + 0.22)], "rizal_stone", sides=40)
    base.revolve((cu, cw), [(2.45, G), (2.45, G + 0.62), (2.3, G + 0.78), (2.1, G + 0.8)], "rizal_stone", sides=40)
    base.finish(col, frame, bevel=0.04)

    rock = RBuf("rizal_oblation_rock")
    # The pedestal: a rough bronze rock, flat-shaded facets so it reads craggy, narrowing as it
    # rises and leaning a little back.
    rings = []
    z0, z1 = G + 0.74, G + 3.1
    for k in range(9):
        t = k / 8
        z = z0 + (z1 - z0) * t
        rx, ry = 0.98 - 0.4 * t, 0.8 - 0.32 * t
        ring = []
        for j in range(11):
            a = j / 11 * math.tau
            jit = 1 + 0.08 * math.sin(3 * a + k * 1.7) + rng.uniform(-0.08, 0.08)
            ring.append(Vector((cu + rx * jit * math.cos(a), cw + 0.06 * t + ry * jit * math.sin(a),
                                z + 0.05 * math.sin(2 * a + k))))
        rings.append(ring)
    rock.loft(rings, "rizal_bronze")
    # The seal medallion on its front.
    rock.tube([Vector((cu, cw - 0.62, G + 1.7)), Vector((cu, cw - 0.86, G + 1.7))], 0.3, "rizal_bronze", sides=20)
    rock.finish(col, frame, bevel=0.02, smooth_area=0.02, weld=False)
    fig = RBuf("rizal_oblation_figure")
    # The figure, 2.5 m, standing on the rock: smooth rounded forms, no anatomy. Legs close
    # together, the right foot a little forward, hips and chest as soft ovals, the head tipped
    # back toward the sky, the arms flung out and raised about 25 degrees.
    fz = z1 - 0.08
    S = 16
    for s, fwd in ((-1, 0.0), (1, -0.08)):
        fig.tube([Vector((cu + s * 0.1, cw + fwd, fz)), Vector((cu + s * 0.11, cw + fwd * 0.5, fz + 0.5)),
                  Vector((cu + s * 0.12, cw, fz + 0.8)), Vector((cu + s * 0.12, cw, fz + 1.14))], 0.0,
                 "rizal_bronze", sides=S, radii=[0.085, 0.105, 0.13, 0.15])
        fig.tube([Vector((cu + s * 0.1, cw + fwd + 0.07, fz + 0.05)), Vector((cu + s * 0.1, cw + fwd - 0.2, fz + 0.05))],
                 0.0, "rizal_bronze", sides=S, radii=[0.075, 0.065])
    torso = []
    for z, rx, ry in ((0.98, 0.26, 0.15), (1.1, 0.29, 0.17), (1.32, 0.23, 0.145), (1.58, 0.26, 0.16),
                      (1.8, 0.3, 0.165), (1.9, 0.22, 0.13)):
        torso.append([Vector((cu + rx * math.cos(a), cw + ry * math.sin(a), fz + z))
                      for a in (k / 20 * math.tau for k in range(20))])
    fig.loft(torso, "rizal_bronze")
    fig.tube([Vector((cu, cw, fz + 1.86)), Vector((cu, cw + 0.02, fz + 2.04))], 0.075, "rizal_bronze", sides=S)
    fig.tube([Vector((cu, cw + 0.02, fz + 1.98)), Vector((cu, cw + 0.04, fz + 2.08)), Vector((cu, cw + 0.07, fz + 2.2)),
              Vector((cu, cw + 0.09, fz + 2.3))], 0.0, "rizal_bronze", sides=S, radii=[0.07, 0.125, 0.12, 0.06])
    for s in (-1, 1):
        sh = Vector((cu + s * 0.25, cw, fz + 1.8))
        el = sh + Vector((s * 0.38, 0.0, 0.15))
        hand = el + Vector((s * 0.36, -0.03, 0.2))
        fig.tube([sh - Vector((s * 0.06, 0, 0)), sh, el, hand, hand + Vector((s * 0.13, -0.01, 0.07))], 0.0,
                 "rizal_bronze", sides=S, radii=[0.1, 0.1, 0.075, 0.06, 0.035])
    fig.finish(col, frame, bevel=0.01, smooth_area=100.0, weld=False)


# ------------------------------------------------------------------ build

def build():
    layout = BLOCK.sightline_override(json.loads(BLOCK.LAYOUT.read_text(encoding="utf-8")))
    hall = next(b for b in layout["buildings"] if "Rizal Hall" in b["name"])
    obl = tuple(next(p["at"] for p in layout["points"] if "Oblation" in p["name"]))
    frame = Frame(hall["poly"], obl)
    poly = rectify([frame.local(x, y) for x, y in hall["poly"]])
    rects = wings(poly)
    edges = [Edge(poly[i], poly[(i + 1) % len(poly)]) for i in range(len(poly))]
    front_index = min(range(len(edges)), key=lambda i: (abs(edges[i].a.y) + abs(edges[i].n.y + 1), -edges[i].L))
    obl_local = frame.local(*obl)
    print(f"[rizal] frame origin {tuple(round(c, 2) for c in frame.origin)} theta {math.degrees(frame.theta):.2f} deg")
    print(f"[rizal] footprint {[(round(x, 2), round(y, 2)) for x, y in poly]}")
    print(f"[rizal] wings {rects}; Oblation at local {tuple(round(c, 2) for c in obl_local)}")

    rng = random.Random(1921)
    hall_col = collection("rizal_hall")
    openings = plan_openings(edges, front_index, rng)
    walls(hall_col, frame, poly, edges, openings, rng)
    bands(hall_col, frame, poly, edges)
    sills(hall_col, frame, edges, openings)
    roof(hall_col, frame, poly, rects)
    rafters(hall_col, frame, poly, edges)
    downpipes(hall_col, frame, edges, front_index)
    portico(hall_col, frame)
    columns(hall_col, frame)
    entablature(hall_col, frame)
    font = next((f for f in ("C:/Windows/Fonts/timesbd.ttf", "C:/Windows/Fonts/georgiab.ttf") if Path(f).exists()), None)
    entrance(hall_col, frame, font)
    balconies(hall_col, frame, edges, front_index)
    aircon(hall_col, frame, edges, openings, rng)
    ob = collection("rizal_oblation")
    oblation(ob, frame, obl_local)
    wins = sum(1 for o in openings if o.kind == "window")
    print(f"[rizal] {wins} windows, {sum(o.ac for o in openings)} with aircon; "
          f"{sum(len(o.data.polygons) for o in bpy.data.objects if o.type == 'MESH')} faces before bevel")
    return frame, layout


# ------------------------------------------------------------------ review

def lighting():
    scene = bpy.context.scene
    world = bpy.data.worlds.new("sky")
    scene.world = world
    if world.node_tree is None:
        world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.74, 0.80, 0.90, 1)
    bg.inputs["Strength"].default_value = 0.6
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.angle, sun.data.color = 3.8, math.radians(3), (1.0, 0.88, 0.72)
    # From the south-west, late afternoon just after the equinox, so the south front is lit.
    sun.rotation_euler = Vector((0.72, 0.42, -0.55)).normalized().to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(sun)
    scene.view_settings.view_transform = "AgX"
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                for space in area.spaces:
                    if space.type == "VIEW_3D":
                        space.shading.type = "MATERIAL"
                        space.clip_end = 3000


def load_context():
    """The blockout around the hall (minus its Rizal Hall stand-ins) and the real guideway, for
    the review renders only."""
    path = SOURCE / "ilalim_blockout.blend"
    with bpy.data.libraries.load(str(path)) as (src, dst):
        dst.collections = ["ilalim_blockout"]
    root = dst.collections[0]
    bpy.context.scene.collection.children.link(root)
    drop = []
    for o in root.all_objects:
        n = o.name
        if n.startswith("Rizal Hall") or "Oblation" in n or o.users_collection[0].name.startswith("Rizal Hall portico"):
            drop.append(o)
        elif o.type == "FONT":
            drop.append(o)
    for o in drop:
        bpy.data.objects.remove(o)
    for c in root.children_recursive:
        if c.name.startswith("LRT-1") or c.name.startswith("Gameplay"):
            c.hide_render = True
    lrt = SOURCE / "lrt_kit.blend"
    with bpy.data.libraries.load(str(lrt), link=True) as (src, dst):
        dst.collections = ["guideway over the court"]
    bpy.context.scene.collection.children.link(dst.collections[0])


def preview(version, frame, layout, only=None):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 3000
    cam.data.clip_start = 0.1
    scene.collection.objects.link(cam)
    scene.camera = cam
    eye = 18 / math.tan(math.radians(95 / 2))
    W = frame.world
    portico_c = W(0.0, -2.0, 8.0)
    faura = [pt for r in layout["roads"] if "Padre Faura" in r["name"] for pt in r["line"]]
    ox = frame.origin.x
    near = min(faura, key=lambda pt: abs(pt[0] - ox))
    faura_south = near[1] - 3 * 3.2 / 2 - BLOCK.SIDEWALK / 2
    pave = BLOCK.PAVE_TOP
    shots = [
        ("court_spawn", Vector((3.0, -9.0, 1.25)), portico_c, eye),
        ("court_pavement", Vector((-8.0, 16.0, pave + 1.25)), portico_c, eye),
        ("faura_front", Vector((ox + 6.0, faura_south, pave + 1.25)), W(0.0, -1.0, 8.5), eye),
        ("portico_close", W(5.0, -10.0, 1.6), W(0.0, -1.5, 7.2), 24),
        ("entrance_close", W(-1.2, -7.0, 1.9), W(0.0, 0.0, 5.2), 30),
        ("door_front", W(-0.6, -4.6, 2.2), W(0.0, 0.0, 3.4), 30),
        ("capital_close", W(-2.6, -8.0, 8.6), W(-5.4, -3.0, 9.9), 32),
        ("facade_grime", W(21.0, -8.5, 2.2), W(21.5, 0.0, 7.0), 26),
        ("eave_corner", W(33.5, -5.0, 2.0), W(30.0, -0.5, 14.2), 24),
        ("oblation_close", W(3.6, -14.2, 2.4), W(0.2, -9.0, 3.9), 34),
        ("aerial", W(70.0, -70.0, 60.0), W(3.0, 26.0, 4.0), 30),
        ("aerial_court", Vector((25.0, -55.0, 45.0)), W(0.0, 20.0, 4.0), 28),
    ]
    for name, pos, tgt, lens in shots:
        if only and name not in only:
            continue
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(PREVIEWS / f"rizal_{name}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[rizal] preview", scene.render.filepath)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    only = argv[argv.index("--shots") + 1].split(",") if "--shots" in argv else None
    bpy.ops.wm.read_factory_settings(use_empty=True)
    frame, layout = build()
    lighting()
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "rizal_hall.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = SOURCE / "rizal_hall.blend1"
    if backup.exists():
        backup.unlink()
    print("[rizal] saved", out)
    if version:
        load_context()
        preview(version, frame, layout, only)


if __name__ == "__main__":
    main()
