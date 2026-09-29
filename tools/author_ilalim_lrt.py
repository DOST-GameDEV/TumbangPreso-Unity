"""Model the LRT-1 guideway kit for the Ilalim ng Tulay rebuild (ILALIM-1.3, first kit).

  py -3 tools/author_ilalim_textures.py            # paint the textures first
  blender -b --python tools/author_ilalim_lrt.py -- [--preview N]

Writes ArtSource/ilalim/lrt_kit.blend. With --preview it also writes versioned renders to
Logs/ilalim-blender/lrt_<shot>_vN.png.

Owner, 2026-09-29: "proceed. i want you to give me models for the LRT way. we're still following
that artistic stylized handdrawn design."

THE REFERENCE (docs/reports/ilalim-rework-2026-09-29/research.md section 4): LRT-1 over Taft is a
narrow grey concrete box deck with a tall panelled parapet and a flat dark underside, catenary
masts on the deck, and square piers. The game keeps two legs per pier row (guide section 0.4)
and joins them under one hammerhead cap, so each row still reads as one LRT-1 pier.

THE HOUSE STYLE (KANTO_DESIGN_GUIDE.md section 2, LAGOON_REWORK_GUIDE.md section 2):
  * real, editable Blender models, few objects named by role, each with a live Bevel modifier
    (hardened normals), so every edge is soft and chunky;
  * CHUNKY AND ORGANIC, NEVER FIDDLY: thick rounded members, a small taper, lean and size
    jitter per piece. Detail lives in the painted textures (tools/author_ilalim_textures.py),
    not in thin geometry;
  * NO TWO SURFACES SHARE A PLANE: parts that meet penetrate, and span ends stop short of each
    other at a real expansion joint;
  * world-scale UVs, so a pour line is the same size on every surface.

THE CONTRACT (guide section 1, from Editor/MapKit/IlalimNgTulayBuilder.cs):
  * soffit at 8.0 (ViaductSoffit): the girder's lowest face;
  * deck top at 9.04 (GuidewayTop), width 10.5 (GuidewayWidth);
  * tracks at x +/-2.35, rail head at 9.19 (RailHead), where the train stands;
  * pier legs 1.4 m square at x +/-4.45 (PillarWorldHalf 0.70), live rows at y +/-10;
  * every pier is its own material, `lrt_pier`, so the Unity builder can put it on the
    TumbangPreso/NearFade shader that the AO NearGuard depends on.
Blender X is the game's x (east), and Blender Y is the game's z (north). All numbers are metres.

THE KIT, each piece a prototype in its own collection at the origin, then assembled over the
court with linked duplicates:
  * lrt_pier: two tapered, rounded legs on footings; a hammerhead cap with sloped ends;
    bearing pads; a drain pipe with clamps down the east leg.
  * lrt_span_<L>: one girder span of length L. Rounded box section with a ballast trough, a
    panelled parapet on both sides with its coping, cable troughs, and BALLASTED track:
    a gravel bed, chunky concrete sleepers and rust-brown rails. LRT-1 runs on ballast, not
    slab track (review of v3, research.md section 8).
  * lrt_gantry: a two-post portal spanning both tracks, with knee braces, drop hangers and
    insulators, at every pier row. It is what the photographs show; the first kit's single
    cantilever masts were wrong. The contact wires sag a little between gantries.

DIRT AND GRIME (owner, review of v3: "you should take a look at how the lrt way actually looks.
theres no dirt or grime on what you have"). Concrete materials multiply two drawn overlays
from tools/author_ilalim_textures.py, grime_drips and grime_splash, through two extra UV maps
that every piece writes:
  * UVGrime: u along the surface, v = metres below the edge the water runs from (the coping
    for the parapet, the cap for the piers), in 4 m.
  * UVSplash: v = metres above the road, in 2 m, on the pier legs. On the underside it is
    stretched inward from the deck edges as a ragged soot band.
Tiling concrete stays clean; the grime sits where water and traffic put it.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "ilalim"
TEXTURES = SOURCE / "textures"
PREVIEWS = ROOT / "Logs" / "ilalim-blender"
TILE_M = 4.0
UP = Vector((0, 0, 1))

SOFFIT = 8.0
DECK_TOP = SOFFIT + 1.04
DECK_HALF = 10.5 / 2
TRACK_X = 2.35
GAUGE_HALF = 0.72
RAIL_HEAD = DECK_TOP + 0.15
PIER_X = 4.45
PIER_HALF = 0.70
CAP_TOP = 7.80
PIER_ROWS = (-94.0, -69.0, -44.0, -19.0, -10.0, 10.0, 19.0, 44.0, 69.0, 94.0)
JOINT = 0.03                 # half the expansion joint between span ends
GANTRY_X = 4.3
WIRE_Z = DECK_TOP + 4.7
TROUGH = 0.20                # the ballast trough's depth below the walkways

# Material: (texture, tint multiplier or None for the texture's own colour, normal strength)
MATERIALS = {
    "lrt_concrete":  ("lrt_concrete", None, 0.6),
    "lrt_pier":      ("lrt_concrete", (1.0, 0.99, 0.97), 0.6),
    "lrt_coping":    ("lrt_concrete", (1.07, 1.07, 1.06), 0.5),
    "lrt_soffit":    ("lrt_soffit", None, 0.5),
    "lrt_deck_top":  ("lrt_track_bed", None, 0.4),
    "lrt_plinth":    ("lrt_track_bed", (0.88, 0.88, 0.88), 0.4),
    "lrt_rail":      ("lrt_steel", (0.44, 0.32, 0.25), 0.3),
    "lrt_mast":      ("lrt_steel", (0.56, 0.57, 0.56), 0.3),
    "lrt_ballast":   ("lrt_ballast", None, 0.25),
    "lrt_sleeper":   ("lrt_concrete", (0.80, 0.79, 0.77), 0.5),
    "lrt_pipe":      ("lrt_steel", (0.36, 0.40, 0.39), 0.3),
    "lrt_bearing":   (None, (0.13, 0.13, 0.14), 0),
    "lrt_insulator": (None, (0.36, 0.24, 0.18), 0),
    "lrt_wire":      (None, (0.07, 0.07, 0.07), 0),
}
# Directional textures (the concrete's pour lines, the soffit's joints) are left out: the rotated
# second sample drew their lines diagonally across the piers and girder (review v2).
ANTI_TILE = {"lrt_track_bed"}
GRIMED = {"lrt_concrete", "lrt_pier", "lrt_coping", "lrt_soffit", "lrt_sleeper"}


def material(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    tex, tint, strength = MATERIALS[name]
    m = bpy.data.materials.new(name)
    if m.node_tree is None:
        m.use_nodes = True
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bsdf = nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.85
    if tex is None:
        bsdf.inputs["Base Color"].default_value = (*tint, 1)
        m.diffuse_color = (*tint, 1)
        return m
    uv = nodes.new("ShaderNodeUVMap")
    albedo = nodes.new("ShaderNodeTexImage")
    albedo.image = bpy.data.images.load(str(TEXTURES / f"{tex}_albedo.png"), check_existing=True)
    links.new(uv.outputs["UV"], albedo.inputs["Vector"])
    colour = albedo.outputs["Color"]
    if tex in ANTI_TILE:
        # The same texture again, rotated 37 degrees and scaled 0.61, blended in through a big
        # soft noise mask, so the repeat never lines up (the Kanto roof fix).
        rot = nodes.new("ShaderNodeMapping")
        rot.inputs["Rotation"].default_value = (0, 0, math.radians(37))
        rot.inputs["Scale"].default_value = (0.61, 0.61, 1)
        rot.inputs["Location"].default_value = (0.37, 0.71, 0)
        links.new(uv.outputs["UV"], rot.inputs["Vector"])
        second = nodes.new("ShaderNodeTexImage")
        second.image = albedo.image
        links.new(rot.outputs["Vector"], second.inputs["Vector"])
        noise = nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = 0.35
        noise.inputs["Detail"].default_value = 0.0
        links.new(uv.outputs["UV"], noise.inputs["Vector"])
        ramp = nodes.new("ShaderNodeMapRange")
        ramp.inputs["From Min"].default_value, ramp.inputs["From Max"].default_value = 0.4, 0.6
        links.new(noise.outputs["Fac"], ramp.inputs["Value"])
        blend = nodes.new("ShaderNodeMix")
        blend.data_type = "RGBA"
        links.new(ramp.outputs["Result"], blend.inputs["Factor"])
        links.new(albedo.outputs["Color"], blend.inputs[6])
        links.new(second.outputs["Color"], blend.inputs[7])
        colour = blend.outputs[2]
    if tint is not None:
        mix = nodes.new("ShaderNodeMix")
        mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
        mix.inputs["Factor"].default_value = 1.0
        links.new(colour, mix.inputs[6])
        mix.inputs[7].default_value = (*tint, 1)
        colour = mix.outputs[2]
    if name in GRIMED:
        # The positional grime: each overlay is a multiplier (white = clean) on its own UV map.
        for image, layer in (("grime_drips", "UVGrime"), ("grime_splash", "UVSplash")):
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
    normal = nodes.new("ShaderNodeTexImage")
    normal.image = bpy.data.images.load(str(TEXTURES / f"{tex}_normal.png"), check_existing=True)
    normal.image.colorspace_settings.name = "Non-Color"
    links.new(uv.outputs["UV"], normal.inputs["Vector"])
    nmap = nodes.new("ShaderNodeNormalMap")
    nmap.inputs["Strength"].default_value = strength
    links.new(normal.outputs["Color"], nmap.inputs["Color"])
    links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
    avg = (0.72, 0.71, 0.69) if tint is None else tuple(0.8 * c for c in tint)
    m.diffuse_color = (*avg, 1)
    return m


# ------------------------------------------------------------------ geometry helpers

def fillet(points, radius, segments=3):
    """Round every corner of a closed 2D polygon with an arc of `radius`, limited so it never
    eats more than 40 per cent of either neighbouring edge."""
    out = []
    n = len(points)
    for i in range(n):
        p0, p1, p2 = Vector(points[i - 1]), Vector(points[i]), Vector(points[(i + 1) % n])
        a, b = (p0 - p1), (p2 - p1)
        la, lb = a.length, b.length
        a.normalize()
        b.normalize()
        ang = math.acos(max(-1.0, min(1.0, a.dot(b))))
        if ang > math.radians(175) or ang < 1e-3:
            out.append(tuple(p1))
            continue
        t = min(radius / math.tan(ang / 2), 0.4 * la, 0.4 * lb)
        s, e = p1 + a * t, p1 + b * t
        for k in range(segments + 1):
            u = k / segments
            # A quadratic Bezier through the corner: a soft, slightly full curve.
            q = s * (1 - u) ** 2 + p1 * 2 * u * (1 - u) + e * u * u
            out.append((q.x, q.y))
    return out


def rounded_rect(hw, hd, r):
    return fillet([(-hw, -hd), (hw, -hd), (hw, hd), (-hw, hd)], r, 3)


class Buf:
    """One object's worth of geometry in a single bmesh, with material indices."""

    def __init__(self, name, drip_top=None, splash=False, soffit_edge=False):
        self.name, self.bm, self.mats = name, bmesh.new(), []
        # Grime mapping: `drip_top` is the z the water runs down from (a number, or a function of
        # a face's centre z); `splash` maps the road splash; `soffit_edge` maps undersides inward
        # from the deck edge.
        self.drip_top, self.splash, self.soffit_edge = drip_top, splash, soffit_edge

    def mi(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def loft(self, rings, mat, cap=True, mat_of=None):
        """Faces between consecutive rings of equal length (closed loops), capped at both ends.
        `mat_of(face)` may override the material per face after the normals are known."""
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
        idx = self.mi(mat)
        for f in faces:
            f.material_index = self.mi(mat_of(f)) if mat_of else idx
        return faces

    def extrude_y(self, profile_xz, y0, y1, mat, mat_of=None, xform=None):
        """A closed x-z profile swept straight along y."""
        rings = []
        for y in (y0, y1):
            ring = [Vector((x, y, z)) for x, z in profile_xz]
            if xform:
                ring = [xform @ v for v in ring]
            rings.append(ring)
        return self.loft(rings, mat, mat_of=mat_of)

    def extrude_z(self, profile_xy, z0, z1, mat, top_scale=1.0, offset=(0.0, 0.0), lean=(0.0, 0.0)):
        """A closed x-y profile swept up z, tapering to `top_scale` and leaning by `lean` metres."""
        ox, oy = offset
        bottom = [Vector((ox + x, oy + y, z0)) for x, y in profile_xy]
        top = [Vector((ox + lean[0] + x * top_scale, oy + lean[1] + y * top_scale, z1)) for x, y in profile_xy]
        return self.loft([bottom, top], mat)

    def tube(self, path, radius, mat, sides=8):
        """A round tube along a polyline, with a ring at every point and capped ends."""
        rings = []
        for i, p in enumerate(path):
            d = (path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]).normalized()
            side = d.cross(UP)
            if side.length < 1e-4:
                side = d.cross(Vector((1, 0, 0)))
            side.normalize()
            up = side.cross(d).normalized()
            rings.append([p + (side * math.cos(a) + up * math.sin(a)) * radius
                          for a in (k / sides * math.tau for k in range(sides))])
        return self.loft(rings, mat)

    def blob(self, center, radii, mat):
        r = bmesh.ops.create_icosphere(self.bm, subdivisions=2, radius=1.0,
                                       matrix=Matrix.Translation(center) @ Matrix.Diagonal((*radii, 1)))
        idx = self.mi(mat)
        for f in {f for v in r["verts"] for f in v.link_faces}:
            f.material_index = idx
            f.smooth = True

    def world_uvs(self):
        uv = self.bm.loops.layers.uv.verify()
        for f in self.bm.faces:
            n = f.normal
            if abs(n.z) > 0.7:
                for l in f.loops:
                    l[uv].uv = (l.vert.co.x / TILE_M, l.vert.co.y / TILE_M)
                continue
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            for l in f.loops:
                l[uv].uv = (l.vert.co.dot(t) / TILE_M, l.vert.co.z / TILE_M)
        self.grime_uvs()

    def grime_uvs(self):
        drip = self.bm.loops.layers.uv.new("UVGrime")
        splash = self.bm.loops.layers.uv.new("UVSplash")
        clean = 0.999
        for f in self.bm.faces:
            n = f.normal
            cz = f.calc_center_median().z
            side = abs(n.z) <= 0.7
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            top = self.drip_top(cz) if callable(self.drip_top) else self.drip_top
            for l in f.loops:
                co = l.vert.co
                if side and top is not None:
                    l[drip].uv = (co.dot(t) / 8.0, min(clean, max(0.0, (top - co.z) / 4.0)))
                else:
                    l[drip].uv = (co.x / 8.0, clean)
                if side and self.splash:
                    l[splash].uv = (co.dot(t) / 8.0, min(clean, max(0.0, co.z / 2.0)))
                elif n.z < -0.7 and self.soffit_edge:
                    # The underside takes the SPLASH drawing, stretched 2.2 times, as a ragged
                    # soot band creeping in from each deck edge. Drip tongues mapped here drew
                    # wavy wood-grain stripes across the sloped webs (review v4).
                    l[splash].uv = (co.y / 8.0, min(clean, max(0.0, (DECK_HALF - abs(co.x)) / 4.4)))
                else:
                    l[splash].uv = (co.x / 8.0, clean)

    def finish(self, collection, bevel=0.04, segments=2, smooth=True):
        self.world_uvs()
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        for m in self.mats:
            mesh.materials.append(material(m))
        # Big flat faces are shaded flat; only the small rounded ones are smooth. Smooth shading
        # over long faces drew diagonal streaks across the piers and cap (review v1).
        for p in mesh.polygons:
            p.use_smooth = smooth and p.area < 0.35
        obj = bpy.data.objects.new(self.name, mesh)
        collection.objects.link(obj)
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


# ------------------------------------------------------------------ the pier

def pier(col, seed=1):
    """Two legs under one hammerhead cap. Legs taper 3 per cent and lean a fraction of a degree
    each, so the pair reads as poured, not extruded."""
    rng = random.Random(seed)
    # Water runs off the cap's edges and from under the cap down the legs; traffic splashes the feet.
    body = Buf("lrt_pier", drip_top=lambda z: CAP_TOP if z > CAP_TOP - 1.2 else CAP_TOP - 0.95, splash=True)
    for s in (-1, 1):
        # A low footing, barely proud of the road and only 0.1 wider than the leg. The first
        # version flared 0.25 m and 0.14 high and read as a Greek column base (review v1).
        body.extrude_z(rounded_rect(0.80, 0.80, 0.24), -0.12, 0.06, "lrt_pier", top_scale=0.97,
                       offset=(s * PIER_X, 0))
        lean = (rng.uniform(-0.03, 0.03), rng.uniform(-0.03, 0.03))
        body.extrude_z(rounded_rect(PIER_HALF + 0.02, PIER_HALF + 0.02, 0.2), 0.05, CAP_TOP - 0.55, "lrt_pier",
                       top_scale=0.95, offset=(s * PIER_X, 0), lean=lean)
    # The cap: a hammerhead tucked INSIDE the deck width, deepest over the legs, its underside
    # bowing up a little between them and its ends sloping up. The first cap ran past the deck
    # with a flat lintel underside and read as a temple cornice (review v1).
    cap = [(-5.6, CAP_TOP - 0.40), (-4.9, CAP_TOP - 1.05), (-3.0, CAP_TOP - 0.95), (0.0, CAP_TOP - 0.84),
           (3.0, CAP_TOP - 0.95), (4.9, CAP_TOP - 1.05), (5.6, CAP_TOP - 0.40), (5.6, CAP_TOP), (-5.6, CAP_TOP)]
    body.extrude_y(fillet(cap, 0.22, 3), -0.88, 0.88, "lrt_pier")
    body.finish(col, bevel=0.05)

    parts = Buf("lrt_pier_fittings", drip_top=CAP_TOP)
    # Bearings: one pair under each span end, penetrating both cap and soffit.
    for x in (-2.6, 2.6):
        for y in (-0.45, 0.45):
            parts.extrude_z(rounded_rect(0.26, 0.2, 0.06), CAP_TOP - 0.04, SOFFIT + 0.05, "lrt_bearing",
                            offset=(x, y))
    # The drain pipe: out of the cap's underside, down the east leg's outer face, kicked out at
    # the foot. Chunky, with two fat clamps.
    x = PIER_X + PIER_HALF + 0.14
    path = [Vector((PIER_X + 0.9, 0.35, CAP_TOP - 0.7)), Vector((x, 0.35, CAP_TOP - 1.1)),
            Vector((x, 0.35, 0.6)), Vector((x + 0.12, 0.35, 0.32)), Vector((x + 0.35, 0.35, 0.26))]
    parts.tube(path, 0.1, "lrt_pipe", sides=10)
    for z in (1.6, 4.4):
        parts.extrude_z(rounded_rect(0.17, 0.17, 0.08), z - 0.07, z + 0.07, "lrt_pipe", offset=(x, 0.35))
    parts.finish(col, bevel=0.02)


# ------------------------------------------------------------------ a girder span

def girder_profile():
    """The box girder's cross-section: flat soffit at 8.0, sloping webs, thin wings out to the
    deck edge, flat top at 9.04. Every corner softly rounded."""
    # Walkways at DECK_TOP outside x 3.95; between them a ballast trough TROUGH deep.
    right = [(3.4, SOFFIT), (4.6, SOFFIT + 0.5), (DECK_HALF, SOFFIT + 0.64), (DECK_HALF, DECK_TOP),
             (3.95, DECK_TOP), (3.75, DECK_TOP - TROUGH)]
    left = [(-x, z) for x, z in reversed(right)]
    return fillet(right + left, 0.12, 3)


def span(col, length, seed):
    """One span, running y 0..length (the joints are left by the caller's placement)."""
    rng = random.Random(seed)
    y0, y1 = JOINT, length - JOINT

    def by_normal(f):
        if f.normal.z < -0.6:
            return "lrt_soffit"
        if f.normal.z > 0.6:
            return "lrt_deck_top"
        return "lrt_concrete"

    girder = Buf(f"lrt_span_{int(length)}_girder", drip_top=DECK_TOP, soffit_edge=True)
    girder.extrude_y(girder_profile(), y0, y1, "lrt_concrete", mat_of=by_normal)
    # Cable troughs inside each parapet, sunk into the deck.
    for s in (-1, 1):
        trough = fillet([(4.62, DECK_TOP - 0.03), (4.98, DECK_TOP - 0.03), (4.98, DECK_TOP + 0.26), (4.62, DECK_TOP + 0.26)], 0.06, 2)
        girder.extrude_y([(s * x, z) for x, z in trough], y0 + 0.2, y1 - 0.2, "lrt_coping")
    girder.finish(col, bevel=0.06)

    # The parapet: LRT-1's tall outer panels, a coping on each, with a joint between panels.
    # Each panel leans and rises by a hair, so the run is drawn, not ruled.
    parapet = Buf(f"lrt_span_{int(length)}_parapet", drip_top=DECK_TOP + 1.28)
    count = max(2, round((y1 - y0) / 2.4))
    step = (y1 - y0) / count
    panel = fillet([(5.10, SOFFIT + 0.72), (5.38, SOFFIT + 0.78), (5.38, DECK_TOP + 1.12), (5.10, DECK_TOP + 1.12)], 0.07, 2)
    coping = fillet([(4.98, DECK_TOP + 1.06), (5.50, DECK_TOP + 1.04), (5.50, DECK_TOP + 1.26), (4.98, DECK_TOP + 1.28)], 0.09, 3)
    for s in (-1, 1):
        for k in range(count):
            a, b = y0 + k * step + 0.03, y0 + (k + 1) * step - 0.03
            lean = Matrix.Rotation(math.radians(rng.uniform(-0.4, 0.4)), 4, "Y")
            rise = Matrix.Translation((0, 0, rng.uniform(-0.02, 0.02)))
            pivot = Matrix.Translation((s * 5.24, 0, DECK_TOP))
            xf = pivot @ lean @ rise @ pivot.inverted()
            parapet.extrude_y([(s * x, z) for x, z in panel], a, b, "lrt_concrete", xform=xf)
            parapet.extrude_y([(s * x, z) for x, z in coping], a - 0.01, b + 0.01, "lrt_coping", xform=xf)
    parapet.finish(col, bevel=0.03)

    # Ballasted track, as LRT-1 really is: a gravel bed filling the trough, chunky concrete
    # sleepers bedded in it, and rust-brown rails on the sleepers with their head at RAIL_HEAD.
    track = Buf(f"lrt_span_{int(length)}_track")
    bed_top = RAIL_HEAD - 0.13 - 0.07
    bed = [(-3.66, DECK_TOP - TROUGH - 0.03), (3.66, DECK_TOP - TROUGH - 0.03), (3.62, bed_top - 0.04),
           (3.2, bed_top), (1.1, bed_top + 0.02), (-1.1, bed_top + 0.02), (-3.2, bed_top), (-3.62, bed_top - 0.04)]
    track.extrude_y(fillet(bed, 0.2, 3), y0 + 0.02, y1 - 0.02, "lrt_ballast")
    sleeper = Buf(f"lrt_span_{int(length)}_sleepers")
    count = int((y1 - y0 - 0.4) / 0.75)
    for tx in (-TRACK_X, TRACK_X):
        for k in range(count):
            y = y0 + 0.4 + k * (y1 - y0 - 0.8) / max(1, count - 1) + rng.uniform(-0.03, 0.03)
            sleeper.extrude_z(rounded_rect(1.15, 0.14, 0.05), bed_top - 0.08, RAIL_HEAD - 0.13, "lrt_sleeper",
                              top_scale=0.97, offset=(tx + rng.uniform(-0.02, 0.02), y))
    sleeper.finish(col, bevel=0.02)
    rail = fillet([(-0.07, RAIL_HEAD - 0.13), (0.07, RAIL_HEAD - 0.13), (0.035, RAIL_HEAD - 0.1), (0.035, RAIL_HEAD - 0.05),
                   (0.055, RAIL_HEAD - 0.04), (0.055, RAIL_HEAD), (-0.055, RAIL_HEAD), (-0.055, RAIL_HEAD - 0.04),
                   (-0.035, RAIL_HEAD - 0.05), (-0.035, RAIL_HEAD - 0.1)], 0.015, 2)
    for tx in (-TRACK_X, TRACK_X):
        for g in (-GAUGE_HALF, GAUGE_HALF):
            track.extrude_y([(tx + g + px, z - 0.005) for px, z in rail], y0, y1, "lrt_rail")
    track.finish(col, bevel=0.012)


# ------------------------------------------------------------------ the catenary mast

def gantry(col, seed=3):
    """A two-post portal over both tracks: tapered posts on the walkways, a chunky crossbeam,
    knee braces at the corners, and a drop hanger with an insulator over each track."""
    rng = random.Random(seed)
    g = Buf("lrt_gantry")
    beam_z = WIRE_Z + 1.0
    for s in (-1, 1):
        g.extrude_z(rounded_rect(0.17, 0.14, 0.05), DECK_TOP - 0.03, beam_z + 0.2, "lrt_mast", top_scale=0.82,
                    offset=(s * GANTRY_X, 0), lean=(rng.uniform(-0.02, 0.02), 0))
        g.extrude_z(rounded_rect(0.28, 0.25, 0.07), DECK_TOP - 0.03, DECK_TOP + 0.2, "lrt_mast", offset=(s * GANTRY_X, 0))
        g.tube([Vector((s * (GANTRY_X - 0.05), 0, beam_z - 1.05)), Vector((s * (GANTRY_X - 1.1), 0, beam_z - 0.05))],
               0.055, "lrt_mast", sides=8)
        g.tube([Vector((s * TRACK_X, 0, beam_z - 0.1)), Vector((s * TRACK_X, 0, WIRE_Z + 0.45))], 0.04, "lrt_mast", sides=8)
        g.blob(Vector((s * TRACK_X, 0, WIRE_Z + 0.36)), (0.07, 0.07, 0.15), "lrt_insulator")
        g.tube([Vector((s * TRACK_X, 0, WIRE_Z + 0.25)), Vector((s * (TRACK_X - 0.25), 0, WIRE_Z + 0.02))], 0.022, "lrt_wire", sides=6)
    beam = fillet([(-GANTRY_X - 0.3, beam_z - 0.15), (GANTRY_X + 0.3, beam_z - 0.15), (GANTRY_X + 0.3, beam_z + 0.15),
                   (-GANTRY_X - 0.3, beam_z + 0.15)], 0.06, 2)
    g.extrude_y(beam, -0.12, 0.12, "lrt_mast")
    g.finish(col, bevel=0.015)


def wires(col, ys):
    """Contact wires over both tracks, sagging a little between masts."""
    w = Buf("lrt_wires")
    for tx in (-TRACK_X, TRACK_X):
        path = []
        for a, b in zip(ys, ys[1:]):
            for k in range(6):
                t = k / 6
                path.append(Vector((tx, a + (b - a) * t, WIRE_Z - 0.18 * math.sin(math.pi * t))))
        path.append(Vector((tx, ys[-1], WIRE_Z)))
        w.tube(path, 0.03, "lrt_wire", sides=6)
    w.finish(col, bevel=0, smooth=True)


# ------------------------------------------------------------------ assembly and review

def instance(proto_col, target, loc, mirror_x=False):
    for o in proto_col.objects:
        c = o.copy()                   # linked duplicate: shares the prototype's mesh
        c.location = Vector(loc)
        if mirror_x:
            c.scale = (-1, 1, 1)
        target.objects.link(c)


def assemble():
    kit = collection("kit (prototypes)")
    kit.hide_render = True
    run = collection("guideway over the court")
    protos = {}
    p = collection("lrt_pier", kit)
    pier(p)
    protos["pier"] = p
    lengths = sorted({round(b - a) for a, b in zip(PIER_ROWS, PIER_ROWS[1:])})
    for L in lengths:
        c = collection(f"lrt_span_{L}", kit)
        span(c, float(L), seed=L)
        protos[L] = c
    gc = collection("lrt_gantry", kit)
    gantry(gc)
    for y in PIER_ROWS:
        instance(protos["pier"], run, (0, y, 0))
        instance(gc, run, (0, y, 0))
    for a, b in zip(PIER_ROWS, PIER_ROWS[1:]):
        instance(protos[round(b - a)], run, (0, a, 0))
    wires(run, list(PIER_ROWS))
    print(f"[ilalim-lrt] kit: pier, spans {lengths}, gantry; {len(run.objects)} placed objects")


def review_set():
    """A plain stand-in for the street, so the renders read at the game's scale: road, kerbs,
    pavements, chalk and a 1.7 m figure. None of it is part of the kit."""
    col = collection("review stand-ins")

    def slab(name, x0, x1, y0, y1, top, colour):
        m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        if m.node_tree is None:
            m.use_nodes = True
        m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*colour, 1)
        me = bpy.data.meshes.new(name)
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(((x0 + x1) / 2, (y0 + y1) / 2, top - 0.2))
                              @ Matrix.Diagonal((x1 - x0, y1 - y0, 0.4, 1)))
        bm.to_mesh(me)
        bm.free()
        me.materials.append(m)
        col.objects.link(bpy.data.objects.new(name, me))

    slab("stand-in road", -6.65, 6.65, -120, 120, 0.0, (0.22, 0.22, 0.24))
    for s in (-1, 1):
        slab("stand-in kerb", *sorted((s * 6.65, s * 7.0)), -120, 120, 0.15, (0.92, 0.92, 0.9))
        slab("stand-in pavement", *sorted((s * 7.0, s * 11.0)), -120, 120, 0.212, (0.62, 0.58, 0.52))
        slab("stand-in lot", *sorted((s * 11.0, s * 80.0)), -120, 120, 0.24, (0.64, 0.64, 0.56))
        slab("stand-in chalk", -7, 7, s * 7 - 0.06, s * 7 + 0.06, 0.012, (1, 1, 1))
    fig = bpy.data.meshes.new("figure")
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=12, radius1=0.25, radius2=0.25, depth=1.7,
                          matrix=Matrix.Translation((2.0, -6.0, 0.85)))
    bm.to_mesh(fig)
    bm.free()
    fm = bpy.data.materials.new("figure")
    fm.use_nodes = True
    fm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.85, 0.35, 0.55, 1)
    fig.materials.append(fm)
    col.objects.link(bpy.data.objects.new("1.7 m figure", fig))


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
    sun.rotation_euler = Vector((0.80, -0.25, -0.55)).normalized().to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(sun)
    scene.view_settings.view_transform = "AgX"
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                for space in area.spaces:
                    if space.type == "VIEW_3D":
                        space.shading.type = "MATERIAL"
                        space.clip_end = 2000


def preview(version):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    cam.data.clip_end = 2000
    scene.collection.objects.link(cam)
    scene.camera = cam
    eye = 18 / math.tan(math.radians(95 / 2))
    shots = [
        ("court_eye", Vector((0.0, -9.0, 1.25)), Vector((0, 20, 5)), eye),
        ("pier_close", Vector((8.5, -4.0, 1.7)), Vector((4.4, -10, 4.0)), 26),
        ("cap_detail", Vector((9.5, -5.5, 5.8)), Vector((2.5, -10, 7.9)), 30),
        ("deck_top", Vector((9.0, -17.0, 13.5)), Vector((0, 2, 10.5)), 28),
        ("aerial", Vector((34.0, -46.0, 24.0)), Vector((0, 4, 7)), 30),
        ("elevation", Vector((38.0, -2.0, 5.0)), Vector((0, -2, 6)), 30),
        ("grime_side", Vector((13.0, -22.0, 3.2)), Vector((4.5, -8.0, 7.0)), 30),
    ]
    for name, pos, tgt, lens in shots:
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(PREVIEWS / f"lrt_{name}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[ilalim-lrt] preview", scene.render.filepath)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    bpy.ops.wm.read_factory_settings(use_empty=True)
    assemble()
    review_set()
    lighting()
    SOURCE.mkdir(parents=True, exist_ok=True)
    out = SOURCE / "lrt_kit.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(out), compress=True)
    backup = SOURCE / "lrt_kit.blend1"
    if backup.exists():
        backup.unlink()
    print("[ilalim-lrt] saved", out)
    if version:
        preview(version)


if __name__ == "__main__":
    main()
