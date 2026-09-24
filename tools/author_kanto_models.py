"""Model the Kanto sample map's buildings in Blender.

Run with Blender 5.0 in background mode:
  blender -b --python tools/author_kanto_models.py -- brick_corner [--textured] [--preview N]

--textured wires in the painted textures from tools/author_kanto_textures.py (run that first).

Writes ArtSource/kanto/<model>.blend, and with --preview versioned Eevee renders to
Logs/kanto-blender/<model>_<shot>_vN.png.

HOW THE MODEL IS BUILT, AND WHY. The first pass placed ~800 bevelled boxes and was rejected:
"the walls arent even one piece", with z-fighting where boxes shared a face. So:
  * The WALL SHELL is ONE welded, closed mesh. Each facade is cut into a grid on the window
    edges; the window cells are merged and pushed IN, so every recess, reveal and glass pane
    belongs to the same surface. Ground floor stone and upper brick are material zones of it.
  * TRIM is SWEPT: cornice, string courses, plinth, fascia, sills and lintels are moulding
    profiles swept along the facade path with mitred corners, one continuous piece each.
  * WINDOW FRAMES are single cut-out meshes (a grid with the panes left out, then solidified),
    standing in the recess IN FRONT of the glass, never coplanar with it.
  * Nothing that touches another piece shares a plane with it: attached parts penetrate.
Objects are few and named by role (shell, trim, frames, awnings, props), each with a live
Bevel modifier, so the .blend stays editable by hand.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "ArtSource" / "kanto"
PREVIEWS = ROOT / "Logs" / "kanto-blender"
UP = Vector((0, 0, 1))

TEXTURES = SOURCE / "textures"
TILE_M = 2.0   # every texture covers 2 m; must match tools/author_kanto_textures.py
# Which painted texture each material wears. Anything not listed wears the neutral "paint"
# detail multiplied by its palette colour, so one texture serves trim, frames and awnings.
TEXTURED = {"brick": "brick", "stone_blocks": "stone_blocks", "stone": "stone", "stone_shade": "stone",
            "roof": "roof", "glass": "glass", "wood": "wood"}
USE_TEXTURES = False
# UV multiplier per texture: 0.35 makes the roof's 2 m tile cover about 5.7 m.
TEX_SCALE = {"roof": 0.35, "stone": 0.5, "paint": 0.5}
ANTI_TILE = {"roof", "stone", "paint"}
NEUTRAL_TEX = {"paint", "plaster"}   # grey coats, always tinted by the material colour

PALETTE = {
    "stone_blocks": (0.93, 0.85, 0.68),
    "brick":       (0.56, 0.22, 0.16),
    "stone":       (0.93, 0.85, 0.68),
    "stone_shade": (0.80, 0.70, 0.52),
    "trim":        (0.12, 0.28, 0.25),
    "glass":       (0.16, 0.50, 0.50),
    "frame":       (0.94, 0.92, 0.84),
    "roof":        (0.30, 0.29, 0.30),
    "metal":       (0.36, 0.40, 0.40),
    "awning_a":    (0.78, 0.17, 0.15),
    "awning_b":    (0.96, 0.92, 0.80),
    "awning_c":    (0.20, 0.48, 0.30),
    "tank":        (0.40, 0.60, 0.56),
    "sign":        (0.97, 0.78, 0.22),
    "wood":        (0.46, 0.29, 0.17),
    "plant":       (0.52, 0.74, 0.20),
    "leaf_light":  (0.30, 0.64, 0.05),
    "leaf_dark":   (0.10, 0.33, 0.04),
    "leaf_core":   (0.06, 0.20, 0.03),
    "soil":        (0.16, 0.09, 0.05),
    "white":       (0.90, 0.90, 0.88),
    "ground":      (0.33, 0.32, 0.35),
}


def material(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    r, g, b = PALETTE[name]
    m.diffuse_color = (r, g, b, 1)
    if m.node_tree is None:
        m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (r, g, b, 1)
    bsdf.inputs["Roughness"].default_value = 0.15 if name == "glass" else 0.8
    if name.startswith(("leaf_light", "leaf_dark")):
        leaf_material(m, bsdf, (r, g, b))
        return m
    if USE_TEXTURES and name not in ("ground", "leaf_core", "soil"):
        paint(m, bsdf, name, (r, g, b))
    return m


def leaf_material(m, bsdf, rgb):
    """The drawn leaf, tinted, cut out by its own alpha, and lit from both sides.
    Always textured: a leaf card without its silhouette is just a green square."""
    nodes, links = m.node_tree.nodes, m.node_tree.links
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(str(TEXTURES / "leaf_albedo.png"), check_existing=True)
    mix = nodes.new("ShaderNodeMix")
    mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
    mix.inputs["Factor"].default_value = 1.0
    links.new(tex.outputs["Color"], mix.inputs[6])
    mix.inputs[7].default_value = (*(min(1.0, c * 1.25) for c in rgb), 1)
    links.new(mix.outputs[2], bsdf.inputs["Base Color"])
    links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
    bsdf.inputs["Roughness"].default_value = 0.7
    m.use_backface_culling = False
    if hasattr(m, "surface_render_method"):
        m.surface_render_method = "DITHERED"
    else:
        m.blend_method = "CLIP"


def paint(m, bsdf, name, rgb):
    """Wire a painted albedo and its normal map into the material, on the UV map."""
    nodes, links = m.node_tree.nodes, m.node_tree.links
    tex = TEXTURED.get(name, "paint")
    albedo = nodes.new("ShaderNodeTexImage")
    albedo.image = bpy.data.images.load(str(TEXTURES / f"{tex}_albedo.png"), check_existing=True)
    scale = TEX_SCALE.get(tex, 1.0)
    uv = nodes.new("ShaderNodeUVMap")
    mapping = nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (scale, scale, 1)
    links.new(uv.outputs["UV"], mapping.inputs["Vector"])
    links.new(mapping.outputs["Vector"], albedo.inputs["Vector"])
    colour_out = albedo.outputs["Color"]
    if tex in ANTI_TILE:
        # ANTI-TILING. The roof read as wallpaper: the same 2 m of patches nine times over.
        # A second sample of the SAME texture, rotated 37 degrees, scaled by 0.61 and shifted,
        # is blended in through a big soft noise mask, so no two tiles look alike and no edge
        # of the repeat lines up. Directional textures (brick, planks) are left out, since
        # rotating them would tilt the courses.
        rot = nodes.new("ShaderNodeMapping")
        rot.inputs["Rotation"].default_value = (0, 0, math.radians(37))
        rot.inputs["Scale"].default_value = (scale * 0.61, scale * 0.61, 1)
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
        colour_out = blend.outputs[2]
    normal = nodes.new("ShaderNodeTexImage")
    normal.image = bpy.data.images.load(str(TEXTURES / f"{tex}_normal.png"), check_existing=True)
    normal.image.colorspace_settings.name = "Non-Color"
    links.new(mapping.outputs["Vector"], normal.inputs["Vector"])
    nmap = nodes.new("ShaderNodeNormalMap")
    nmap.inputs["Strength"].default_value = 1.0
    links.new(normal.outputs["Color"], nmap.inputs["Color"])
    links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
    # Neutral textures (the grey "paint" and "plaster" coats) are ALWAYS multiplied by the
    # material's own colour; without that every plaster building rendered white.
    if name in TEXTURED and name != "stone_shade" and TEXTURED[name] not in NEUTRAL_TEX:
        links.new(colour_out, bsdf.inputs["Base Color"])
        return
    # Tinted: the neutral texture (or darkened stone) multiplied by this material's colour.
    mix = nodes.new("ShaderNodeMix")
    mix.data_type, mix.blend_type = "RGBA", "MULTIPLY"
    mix.inputs["Factor"].default_value = 1.0
    links.new(colour_out, mix.inputs[6])
    # Colour sockets are LINEAR and so is PALETTE. The neutral texture averages ~0.83 once
    # decoded from sRGB, so the tint is lifted by its reciprocal to land on the palette colour.
    # (The first pass gamma-encoded the tint as well, which washed every awning out to pastel.)
    tint = (0.82, 0.78, 0.70) if name == "stone_shade" else tuple(min(1.0, c * 1.2) for c in rgb)
    mix.inputs[7].default_value = (*tint, 1)
    links.new(mix.outputs[2], bsdf.inputs["Base Color"])


# ------------------------------------------------------------------ mesh buffer

class Buf:
    """One object's worth of geometry, built in a single bmesh with material indices."""

    def __init__(self, name, foliage=False):
        self.name, self.bm, self.mats, self.welded = name, bmesh.new(), [], {}
        # A foliage buffer keeps each card's own 0..1 UVs (the leaf drawing) instead of
        # world-scale ones, and gets rounded normals from its clump centres in finish().
        self.foliage = foliage
        self.clump_of = {}   # face index -> clump centre

    def leaf(self, at, tip, normal, length, width, mat, clump):
        """One leaf card: a quad from `at` along `tip`, facing `normal`, UVs 0..1."""
        side = normal.cross(tip).normalized() * (width / 2)
        base = at - tip * (length * 0.08)
        end = at + tip * (length * 0.92)
        cos = [base - side, base + side, end + side, end - side]
        f = self.bm.faces.new([self.bm.verts.new(c) for c in cos])
        f.material_index = self.mi(mat)
        uv = self.bm.loops.layers.uv.verify()
        for loop, st in zip(f.loops, ((0, 0), (1, 0), (1, 1), (0, 1))):
            loop[uv].uv = st
        self.clump_of[len(self.bm.faces) - 1] = clump

    def blob(self, center, radii, mat, subdiv=2):
        r = bmesh.ops.create_icosphere(self.bm, subdivisions=subdiv, radius=1.0,
                                       matrix=Matrix.Translation(center) @ Matrix.Diagonal((*radii, 1)))
        self._paint(r["verts"], mat)

    def mi(self, mat):
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def vert(self, co, weld=False):
        if not weld:
            return self.bm.verts.new(co)
        key = tuple(round(c, 4) for c in co)
        v = self.welded.get(key)
        if v is None:
            v = self.welded[key] = self.bm.verts.new(co)
        return v

    def face(self, cos, mat, weld=False):
        f = self.bm.faces.new([self.vert(c, weld) for c in cos])
        f.material_index = self.mi(mat)
        return f

    def _paint(self, verts, mat):
        idx = self.mi(mat)
        for v in verts:
            for f in v.link_faces:
                f.material_index = idx

    def box(self, matrix, size, mat):
        """A box of `size` centred on `matrix`'s origin, in its frame. Props only."""
        r = bmesh.ops.create_cube(self.bm, size=1.0, matrix=matrix @ Matrix.Diagonal((*size, 1)))
        self._paint(r["verts"], mat)

    def cylinder(self, center, radius, depth, mat, sides=16, top_scale=1.0):
        r = bmesh.ops.create_cone(self.bm, cap_ends=True, segments=sides, radius1=radius,
                                  radius2=radius * top_scale, depth=depth, matrix=Matrix.Translation(center))
        self._paint(r["verts"], mat)

    def prism(self, poly, z0, z1, mat):
        """Extrude a world-XY polygon from z0 to z1 as one closed piece."""
        bottom = [Vector((x, y, z0)) for x, y in poly]
        top = [Vector((x, y, z1)) for x, y in poly]
        self.face(top, mat)
        self.face(list(reversed(bottom)), mat)
        n = len(poly)
        for i in range(n):
            j = (i + 1) % n
            self.face([bottom[i], bottom[j], top[j], top[i]], mat)

    def sweep(self, path, profile, mat, inside, closed=False):
        """Sweep a closed 2D `profile` of (out, up) along `path`, mitring every corner.
        `out` is measured away from `inside`, a point inside the building."""
        n = len(path)
        segs = [(path[(i + 1) % n] - path[i]) for i in range(n if closed else n - 1)]
        normals = []
        for i, d in enumerate(segs):
            d = Vector((d.x, d.y, 0)).normalized()
            o = Vector((-d.y, d.x, 0))
            mid = path[i] + segs[i] / 2
            if o.dot(Vector((mid.x - inside.x, mid.y - inside.y, 0))) < 0:
                o = -o
            normals.append(o)
        rings = []
        for i in range(n):
            if closed:
                a, b = normals[i - 1], normals[i % len(normals)]
            else:
                a, b = normals[max(0, i - 1)], normals[min(i, len(normals) - 1)]
            m = (a + b).normalized()
            m = m / max(0.3, m.dot(a))
            rings.append([self.bm.verts.new(path[i] + m * o + UP * u) for o, u in profile])
        k, idx = len(profile), self.mi(mat)
        for i in range(n if closed else n - 1):
            r0, r1 = rings[i], rings[(i + 1) % n]
            for j in range(k):
                self.bm.faces.new((r0[j], r0[(j + 1) % k], r1[(j + 1) % k], r1[j])).material_index = idx
        if not closed:
            for ring in (rings[0], rings[-1]):
                self.bm.faces.new(ring).material_index = idx

    def world_uvs(self):
        """Real-world-scale UVs: walls project horizontally along the face, floors from above.
        A brick is then the same size on every surface, and courses run level round corners."""
        uv = self.bm.loops.layers.uv.verify()
        if getattr(self, "uv_mode", None) == "keep":
            return   # the builder wrote its own UVs (tree limbs: one cylindrical map per limb)
        if getattr(self, "uv_mode", None) == "trunk":
            # TREES: u runs AROUND the tree's own vertical axis (one texture tile per turn),
            # v up it, on every face of trunk and limbs alike. With per-face projection the
            # bark pattern broke at every facet and at every trunk-to-limb join (owner: "fix
            # the seams where the trunk and limbs meet so the pattern wraps continuously").
            for f in self.bm.faces:
                us = [math.atan2(l.vert.co.y, l.vert.co.x) / math.tau for l in f.loops]
                if max(us) - min(us) > 0.5:            # a face across the -pi/+pi cut
                    us = [u + 1 if u < 0 else u for u in us]
                for l, u in zip(f.loops, us):
                    l[uv].uv = (u, l.vert.co.z / TILE_M)
            return
        for f in self.bm.faces:
            n = f.normal
            if 0.3 < n.z < 0.97 and self.mats[f.material_index].startswith("roof_tile"):
                # PITCHED TILE ROOFS: u along the eaves, v up the slope, so tile courses run
                # parallel to the ridge and every course's lower edge points DOWNHILL on both
                # slopes (a top-down projection flipped them on one side).
                down = Vector((n.x, n.y, 0)).normalized()
                t = UP.cross(down)
                for l in f.loops:
                    l[uv].uv = (l.vert.co.dot(t) / TILE_M, -l.vert.co.dot(down) / n.z / TILE_M)
                continue
            if abs(n.z) > 0.7:
                for l in f.loops:
                    l[uv].uv = (l.vert.co.x / TILE_M, l.vert.co.y / TILE_M)
                continue
            t = UP.cross(n)
            t = t.normalized() if t.length > 1e-6 else Vector((1, 0, 0))
            for l in f.loops:
                l[uv].uv = (l.vert.co.dot(t) / TILE_M, l.vert.co.z / TILE_M)

    def finish(self, collection, bevel=0.03, segments=2, angle=40):
        if not self.foliage:
            bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=1e-4)
            # A buffer of loose, hand-wound faces (the city ground) must NOT be recalculated:
            # recalc orients each disconnected face arbitrarily and can turn a road upside down.
            if not getattr(self, "keep_winding", False):
                bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
            self.world_uvs()
        mesh = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        for m in self.mats:
            mesh.materials.append(material(m))
        for p in mesh.polygons:
            p.use_smooth = True
        if self.foliage:
            # ROUNDED FOLIAGE NORMALS. Every card's normal points away from the centre of the
            # clump it belongs to, so a clump of flat cards shades like one soft ball: lit on
            # top, dark underneath. This is the trick behind the reference's leafy trees; with
            # each card's own flat normal the clump reads as scattered confetti.
            normals = []
            for p in mesh.polygons:
                c = self.clump_of.get(p.index)
                for li in p.loop_indices:
                    v = mesh.vertices[mesh.loops[li].vertex_index].co
                    n = (v - c).normalized() if c is not None else p.normal
                    normals.append(n)
            mesh.normals_split_custom_set(normals)
            bevel = 0
        obj = bpy.data.objects.new(self.name, mesh)
        collection.objects.link(obj)
        if bevel:
            mod = obj.modifiers.new("Bevel", "BEVEL")
            mod.width, mod.segments = bevel, segments
            mod.limit_method, mod.angle_limit = "ANGLE", math.radians(angle)
            mod.harden_normals, mod.use_clamp_overlap = True, True
        return obj


# ------------------------------------------------------------------ facades

class Facade:
    """One straight wall of a footprint: u runs along it, +out away from the building."""

    def __init__(self, a, b, inside):
        self.a, self.b = Vector((*a, 0)), Vector((*b, 0))
        self.d = (self.b - self.a).normalized()
        self.length = (self.b - self.a).length
        o = Vector((-self.d.y, self.d.x, 0))
        if o.dot((self.a + self.b) / 2 - Vector((*inside, 0))) < 0:
            o = -o
        self.out = o
        self.openings = []   # (u0, u1, z0, z1, depth, back_material)

    def at(self, u, z, out=0.0):
        return self.a + self.d * u + UP * z + self.out * out

    def frame(self, u, z, out=0.0):
        m = Matrix.Identity(4)
        m.col[0][:3], m.col[1][:3], m.col[2][:3] = self.d, self.out, UP
        m.col[3][:3] = self.at(u, z, out)
        return m


def wall_shell(name, facades, height, zone_z, zones, roof_mat):
    """ONE closed mesh for the building's walls with every opening recessed into it.
    `facades` must run consecutively round the footprint."""
    buf = Buf(name)
    zc = {0.0, height, zone_z}
    for f in facades:
        for u0, u1, z0, z1, *_ in f.openings:
            zc.update((z0, z1))
    zc = sorted(zc)
    groups_all, top_ring, bottom_ring = [], [], []
    for f in facades:
        uc = {0.0, f.length}
        for u0, u1, *_ in f.openings:
            uc.update((u0, u1))
        uc = sorted(uc)
        groups = [[] for _ in f.openings]
        for ui in range(len(uc) - 1):
            for zi in range(len(zc) - 1):
                u0, u1, z0, z1 = uc[ui], uc[ui + 1], zc[zi], zc[zi + 1]
                mat = zones[0] if z1 <= zone_z + 1e-6 else zones[1]
                face = buf.face([f.at(u0, z0), f.at(u1, z0), f.at(u1, z1), f.at(u0, z1)], mat, weld=True)
                um, zm = (u0 + u1) / 2, (z0 + z1) / 2
                for k, (ou0, ou1, oz0, oz1, *_r) in enumerate(f.openings):
                    if ou0 < um < ou1 and oz0 < zm < oz1:
                        groups[k].append(face)
        groups_all += [(o, g, f) for o, g in zip(f.openings, groups)]
        top_ring += [f.at(u, height) for u in uc[:-1]]
        bottom_ring += [f.at(u, 0.0) for u in uc[:-1]]
    buf.face(top_ring, roof_mat, weld=True)
    buf.face(list(reversed(bottom_ring)), zones[0], weld=True)
    bm = buf.bm
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for (u0, u1, z0, z1, depth, back), faces, f in groups_all:
        if len(faces) > 1:
            faces = bmesh.ops.dissolve_faces(bm, faces=faces)["region"]
        r = bmesh.ops.extrude_face_region(bm, geom=faces)
        moved = [g for g in r["geom"] if isinstance(g, bmesh.types.BMVert)]
        bmesh.ops.translate(bm, verts=moved, vec=-f.out * depth)
        for g in r["geom"]:
            if isinstance(g, bmesh.types.BMFace):
                g.material_index = buf.mi(back)
        stale = [x for x in faces if x.is_valid]
        if stale:
            bmesh.ops.delete(bm, geom=stale, context="FACES_ONLY")
    return buf


def window_frame(buf, facade, u, zc, w, h, cols, bar, depth, out, mat, transom=None):
    """ONE frame mesh: the window rectangle cut into bars, panes left out, then solidified."""
    mids_x = [-w / 2 + w * c / cols for c in range(1, cols)]
    # `transom` may be one height or a list: a loft window's grid has several.
    mids_z = [] if transom is None else (list(transom) if isinstance(transom, (list, tuple)) else [transom])
    bars_x = [(-w / 2, -w / 2 + bar), (w / 2 - bar, w / 2)] + [(x - bar * 0.4, x + bar * 0.4) for x in mids_x]
    bars_z = [(-h / 2, -h / 2 + bar), (h / 2 - bar, h / 2)] + [(z - bar * 0.4, z + bar * 0.4) for z in mids_z]
    xs = sorted({v for pair in bars_x for v in pair})
    zs = sorted({v for pair in bars_z for v in pair})
    buf.welded.clear()
    front = []
    for i in range(len(xs) - 1):
        for j in range(len(zs) - 1):
            xm, zm = (xs[i] + xs[i + 1]) / 2, (zs[j] + zs[j + 1]) / 2
            if not (any(a < xm < b for a, b in bars_x) or any(a < zm < b for a, b in bars_z)):
                continue
            front.append(buf.face([facade.at(u + xs[i], zc + zs[j], out), facade.at(u + xs[i + 1], zc + zs[j], out),
                                   facade.at(u + xs[i + 1], zc + zs[j + 1], out), facade.at(u + xs[i], zc + zs[j + 1], out)],
                                  mat, weld=True))
    buf.welded.clear()
    bmesh.ops.solidify(buf.bm, geom=front, thickness=depth)


def moulding(buf, facades, idx, z, profile, mat, inside, closed=False):
    """Sweep `profile` along consecutive facades `idx` (open) or the whole footprint (closed)."""
    if closed:
        path = [f.a + UP * z for f in facades]
    else:
        fs = [facades[i] for i in idx]
        path = [fs[0].a + UP * z] + [f.b + UP * z for f in fs]
    buf.sweep(path, profile, mat, Vector((*inside, 0)), closed=closed)


def awning(buf, facade, u, z, width, depth, drop, stripes, stripe_w=0.55):
    """A curved canvas awning: quarter-round profile, one stripe per panel, scalloped tabs."""
    n = max(3, int(round(width / stripe_w)))
    w, steps = width / n, 8
    for i in range(n):
        sag = 0.035 * math.sin(math.pi * (i + 0.5) / n)
        u0, u1 = u - width / 2 + i * w, u - width / 2 + (i + 1) * w
        outer = [(depth * math.sin(math.pi / 2 * s / steps), z - sag + drop * (math.cos(math.pi / 2 * s / steps) - 1)) for s in range(steps + 1)]
        prof = outer + [(o * 0.96 - 0.02, zz - 0.06) for o, zz in reversed(outer)]
        mat = stripes[i % len(stripes)]
        rings = [[facade.at(uu, zz, o) for o, zz in prof] for uu in (u0 + 0.004, u1 - 0.004)]
        k = len(prof)
        for j in range(k):
            buf.face([rings[0][j], rings[0][(j + 1) % k], rings[1][(j + 1) % k], rings[1][j]], mat)
        buf.face(rings[0], mat)
        buf.face(list(reversed(rings[1])), mat)
        zt = z - sag - drop
        tab = [facade.at(u0 + 0.01, zt - 0.04, depth - 0.03), facade.at(u1 - 0.01, zt - 0.04, depth - 0.03),
               facade.at(u1 - 0.01, zt - 0.24, depth - 0.03), facade.at((u0 + u1) / 2, zt - 0.38, depth - 0.03),
               facade.at(u0 + 0.01, zt - 0.24, depth - 0.03)]
        back = [p - facade.out * 0.05 for p in tab]
        buf.face(tab, mat)
        buf.face(list(reversed(back)), mat)
        for j in range(len(tab)):
            buf.face([tab[j], back[j], back[(j + 1) % len(tab)], tab[(j + 1) % len(tab)]], mat)


def quoins(buf, g, f, z0, z1, mat, t=0.12, back=0.3):
    """L-shaped stone blocks wrapping the corner where facade g ends and facade f starts."""
    c = f.a
    df, dg = f.d, -g.d
    of, og = f.out, g.out
    q = 0
    z = z0
    while z + 0.44 < z1:
        la, lb = (1.0, 0.66) if q % 2 == 0 else (0.66, 1.0)
        pts = [c + og * t + of * t, c + df * la + of * t, c + df * la - of * back,
               c - of * back - og * back, c + dg * lb - og * back, c + dg * lb + og * t]
        buf.prism([(p.x, p.y) for p in pts], z, z + 0.44, mat)
        z += 0.56
        q += 1


# ------------------------------------------------------------------ the brick corner block

CORNICE = [(-0.12, 0.00), (0.10, 0.00), (0.10, 0.26), (0.20, 0.30), (0.20, 0.46), (0.42, 0.52),
           (0.62, 0.62), (0.80, 0.74), (0.80, 0.96), (0.66, 1.04), (-0.12, 1.04)]
COURSE = [(-0.12, -0.12), (0.12, -0.10), (0.18, -0.02), (0.18, 0.06), (0.12, 0.12), (-0.12, 0.12)]
PLINTH = [(-0.12, 0.0), (0.22, 0.0), (0.22, 0.34), (0.10, 0.50), (-0.12, 0.50)]
FASCIA = [(-0.12, 0.0), (0.24, 0.0), (0.24, 0.62), (0.40, 0.68), (0.40, 0.86), (0.30, 0.92), (-0.12, 0.92)]
PARAPET = [(-0.55, -0.12), (0.02, -0.12), (0.02, 0.95), (-0.55, 0.95)]
COPING = [(-0.62, 0.0), (0.12, 0.0), (0.16, 0.08), (0.12, 0.18), (-0.62, 0.18)]
SILL = [(-0.40, -0.14), (0.16, -0.14), (0.22, -0.06), (0.22, 0.0), (0.14, 0.04), (-0.40, 0.04)]
LINTEL = [(-0.10, 0.03), (0.12, 0.03), (0.16, 0.10), (0.16, 0.26), (0.10, 0.30), (-0.10, 0.30)]
CAPITAL = [(-0.10, 0.0), (0.22, 0.0), (0.30, 0.10), (0.30, 0.22), (-0.10, 0.22)]


DOOR_W, DOOR_H = 2.0, 2.9


def DOOR_U(f):
    return f.length / 2


def keystone(trim, f, u, ztop):
    key = [(-0.14, 0.0), (0.14, 0.0), (0.20, 0.42), (-0.20, 0.42)]
    fr = [f.at(u + x, ztop + z, 0.24) for x, z in key]
    bk = [f.at(u + x, ztop + z, -0.05) for x, z in key]
    trim.face(fr, "stone_shade")
    trim.face(list(reversed(bk)), "stone_shade")
    for j in range(4):
        trim.face([fr[j], bk[j], bk[(j + 1) % 4], fr[(j + 1) % 4]], "stone_shade")


def foliage(leaves, center, radii, count, leaf_len, rng, floor_z=None, lift=0.25, spread=0.35, basis=None, tint=""):
    """A CLUMP OF SHAPED LEAVES: the house pattern for every plant, pot shrub to street tree.

    SHINGLED, NOT SCATTERED. The first version pointed every leaf in a random direction and
    was rejected: "all leaves go in random directions rather than a cohesive ball". Here each
    card lies ON the ellipsoid's surface, facing out along the surface normal, with its tip
    running DOWN the surface (the tangent of "down"), rotated by at most `spread` radians and
    lifted off the surface by `lift`. Leaves overlap like roof tiles and the silhouette is one
    ball. Near the top pole, where "down the surface" is undefined, the tip runs outward.

    There is NO CORE. It only ever filled gaps, and density does that without a green ball
    showing through. Leaves whose base falls below `floor_z` (inside a planter) are skipped,
    so a clump set down into the soil grows out of it instead of hovering above it.
    `leaves` must be a foliage Buf. A tree is several clumps on branches.
    `tint` picks a leaf pair (leaf_light<tint>, leaf_dark<tint>) for a WHOLE tree. Variation is
    per tree, never per leaf: per-leaf colour was tried and reverted by the owner."""
    center = Vector(center)
    placed = 0
    golden = math.pi * (3 - math.sqrt(5))
    for i in range(count):
        # Even coverage (a Fibonacci sphere), jittered, so there are no bald patches.
        zf = 1 - 2 * (i + 0.5) / count
        r = math.sqrt(max(0.0, 1 - zf * zf))
        a = golden * i + rng.uniform(-0.3, 0.3)
        d = Vector((math.cos(a) * r, math.sin(a) * r, zf))
        # `radii` are along `basis` (the facade's along/out/up frame), NOT world X/Y/Z. With
        # world axes a clump stretched along the wall on one street stuck straight out of it
        # on the other.
        if basis is not None:
            d = (basis @ d).normalized()
        local = (basis.transposed() @ d) if basis is not None else d
        offset = Vector((local.x * radii[0], local.y * radii[1], local.z * radii[2]))
        p = center + ((basis @ offset) if basis is not None else offset) * rng.uniform(0.9, 1.02)
        if floor_z is not None and p.z < floor_z:
            continue
        down = -UP - d * (-UP).dot(d)
        if down.length < 0.2:
            t = Vector((rng.gauss(0, 1), rng.gauss(0, 1), 0))
            down = t - d * t.dot(d)
        down.normalize()
        side = d.cross(down)
        ang = rng.uniform(-spread, spread)
        tip = (down * math.cos(ang) + side * math.sin(ang) + d * lift).normalized()
        n = (d - tip * d.dot(tip)).normalized()
        length = leaf_len * rng.uniform(0.85, 1.15)
        leaves.leaf(p - tip * length * 0.35, tip, n, length, length * 0.7,
                    ("leaf_light" if d.z > -0.2 else "leaf_dark") + tint, center)
        placed += 1


def planter(buf, f, u, sill_top, length, height=0.28, depth=0.28, wall=0.035):
    """A HOLLOW window-box: floor, four walls, and soil 4 cm below the rim. It used to be a
    solid box with a soil slab whose top was exactly level with the box's, which z-fought.
    Returns the soil's top height."""
    z0 = sill_top
    buf.box(f.frame(u, z0 + wall / 2, 0.05), (length, depth, wall), "wood")
    for side in (-1, 1):
        buf.box(f.frame(u, z0 + height / 2, 0.05 + side * (depth / 2 - wall / 2)), (length, wall, height), "wood")
        # End walls 3 mm lower than the side walls, so their tops never share a plane.
        buf.box(f.frame(u + side * (length / 2 - wall / 2), z0 + (height - 0.003) / 2, 0.05), (wall, depth - 2 * wall + 0.004, height - 0.003), "wood")
    soil_top = z0 + height - 0.04
    PLANTER_SPOTS.append((f.at(u, soil_top, 0.05), f.out.copy()))
    buf.box(f.frame(u, (z0 + wall + soil_top) / 2, 0.05), (length - 2 * wall - 0.004, depth - 2 * wall - 0.004, soil_top - z0 - wall), "soil")
    return soil_top


AC_SPOTS = []
PLANTER_SPOTS = []   # (front point, outward) of each AC unit, for the preview's close-up


def ac_unit(buf, f, u, z_bottom, out):
    """A window air-conditioner: casing, a recessed front with louvres, side vents, a darker
    back frame. The first version was a bare white box, "just untextured rectangles"."""
    W, H, D = 0.72, 0.46, 0.5
    zc = z_bottom + H / 2
    AC_SPOTS.append((f.at(u, zc, out + D / 2), f.out.copy()))
    buf.box(f.frame(u, zc, out), (W, D, H), "white")
    # Front panel, recessed 1 cm, then five louvres standing proud of it.
    buf.box(f.frame(u, zc, out + D / 2 - 0.01), (W - 0.08, 0.04, H - 0.08), "metal")
    for k in range(5):
        buf.box(f.frame(u, zc - H / 2 + 0.09 + k * 0.07, out + D / 2 + 0.005),
                (W - 0.12, 0.03, 0.035), "white")
    # Side vent slots, one set per side.
    for side in (-1, 1):
        for k in range(4):
            buf.box(f.frame(u + side * (W / 2 + 0.002), zc + 0.08 - k * 0.06, out + 0.08),
                    (0.012, D * 0.45, 0.025), "metal")
    # A darker band where the unit meets the window, and a little badge.
    buf.box(f.frame(u, zc, out - D / 2 + 0.04), (W + 0.02, 0.08, H + 0.02), "metal")
    buf.box(f.frame(u + W * 0.3, zc + H / 2 - 0.07, out + D / 2 + 0.01), (0.1, 0.02, 0.035), "sign")


def brick_corner():
    col = bpy.data.collections.new("brick_corner")
    bpy.context.scene.collection.children.link(col)
    rng = random.Random(4)
    W, D, CUT = 18.0, 18.0, 4.2
    GROUND, STOREY, UPPER = 4.6, 3.4, 4
    TOP = GROUND + STOREY * UPPER
    RECESS = 0.34
    # Facade i runs foot[i] -> foot[i+1]: 0 the cut corner, 1 the +Y street, 2 and 3 party
    # walls, 4 the -X street. So the street path 4, 0, 1 is continuous.
    foot = [(0, -CUT), (CUT, 0), (W, 0), (W, -D), (0, -D)]
    inside = (W / 2, -D / 2)
    ins = Vector((*inside, 0))
    facades = [Facade(foot[i], foot[(i + 1) % len(foot)], inside) for i in range(len(foot))]
    order = [4, 0, 1]

    windows, shops = [], []
    for fi in order:
        f = facades[fi]
        corner = fi == 0
        bays = max(2, round(f.length / 3.3))
        bay = f.length / bays
        for s in range(UPPER):
            z0 = GROUND + s * STOREY
            for k in range(bays):
                u = (k + 0.5) * bay
                w, h = bay - 1.05, STOREY - 1.45
                zc = z0 + 0.95 + h / 2
                f.openings.append((u - w / 2, u + w / 2, zc - h / 2, zc + h / 2, RECESS, "glass"))
                windows.append((f, u, zc, w, h, fi, s, k))
        if corner:
            # THE ENTRANCE IS A DOORWAY TO THE GROUND, NOT A FRAME IN A SHOP WINDOW. The first
            # version stood a door frame inside the display glass, above the stone base, and
            # "the door entrance makes no sense" was the right verdict. It is recessed 0.6 m
            # into the wall so it reads as a way in, with a step and a stone head.
            f.openings.append((DOOR_U(f) - DOOR_W / 2, DOOR_U(f) + DOOR_W / 2, 0.0, DOOR_H, 0.6, "glass"))
            continue
        for k in range(bays):
            u = (k + 0.5) * bay
            w = bay - 0.95
            f.openings.append((u - w / 2, u + w / 2, 0.85, 3.35, 0.22, "glass"))
            shops.append((f, u, w, corner))

    wall_shell("shell", facades, TOP, GROUND, ("stone_blocks", "brick"), "roof").finish(col, bevel=0.035)

    trim = Buf("trim")
    # The plinth stops either side of the doorway instead of running across it.
    door = facades[0]
    du = DOOR_U(door)
    trim.sweep([facades[4].at(0, 0), door.at(0, 0), door.at(du - DOOR_W / 2 - 0.05, 0)], PLINTH, "stone_shade", ins)
    trim.sweep([door.at(du + DOOR_W / 2 + 0.05, 0), facades[1].at(0, 0), facades[1].at(facades[1].length, 0)], PLINTH, "stone_shade", ins)
    moulding(trim, facades, order, 3.55, FASCIA, "trim", inside)
    for s in range(UPPER):
        moulding(trim, facades, order, GROUND + s * STOREY, COURSE, "stone", inside)
    moulding(trim, facades, None, TOP - 1.0, CORNICE, "stone", inside, closed=True)
    moulding(trim, facades, None, TOP + 0.04, PARAPET, "brick", inside, closed=True)
    moulding(trim, facades, None, TOP + 0.97, COPING, "stone", inside, closed=True)
    for f, u, zc, w, h, fi, s, k in windows:
        # A sill a centimetre or two off level, like a hand-laid one.
        wob = rng.uniform(-0.015, 0.015)
        trim.sweep([f.at(u - w / 2 - 0.22, zc - h / 2 + wob), f.at(u + w / 2 + 0.22, zc - h / 2 - wob)], SILL, "stone", ins)
        trim.sweep([f.at(u - w / 2 - 0.16, zc + h / 2), f.at(u + w / 2 + 0.16, zc + h / 2)], LINTEL, "stone", ins)
        keystone(trim, f, u, zc + h / 2)
    # The doorway: a lintel and keystone over it, a stone step, pilasters either side.
    trim.sweep([door.at(du - DOOR_W / 2 - 0.16, DOOR_H), door.at(du + DOOR_W / 2 + 0.16, DOOR_H)], LINTEL, "stone", ins)
    keystone(trim, door, du, DOOR_H)
    trim.box(door.frame(du, 0.05, -0.12), (DOOR_W + 0.5, 0.96, 0.14), "stone_shade")
    for uu in (0.4, du - DOOR_W / 2 - 0.45, du + DOOR_W / 2 + 0.45, door.length - 0.4):
        trim.box(door.frame(uu, 1.77, 0.06), (0.62, 0.36, 3.46), "stone")
        trim.sweep([door.at(uu - 0.42, 3.38), door.at(uu + 0.42, 3.38)], CAPITAL, "stone", ins)
    for f, u, w, corner in shops:
        for side in (-1, 1):
            uu = min(max(u + side * (w / 2 + 0.475), 0.4), f.length - 0.4)
            trim.box(f.frame(uu, 1.77, 0.06), (0.62, 0.36, 3.46), "stone")
            trim.sweep([f.at(uu - 0.42, 3.38), f.at(uu + 0.42, 3.38)], CAPITAL, "stone", ins)
    quoins(trim, facades[3], facades[4], GROUND + 0.3, TOP - 1.0, "stone")
    quoins(trim, facades[1], facades[2], GROUND + 0.3, TOP - 1.0, "stone")
    trim.finish(col, bevel=0.03)

    frames = Buf("frames")
    for f, u, zc, w, h, *_ in windows:
        window_frame(frames, f, u, zc, w - 0.02, h - 0.02, cols=2, bar=0.13, depth=0.12,
                     out=-RECESS + 0.16, mat="frame", transom=h * 0.2)
    for f, u, w, corner in shops:
        # Cream, like the upper windows. Dark teal frames on teal glass disappeared.
        window_frame(frames, f, u, 2.1, w - 0.02, 2.48, cols=3, bar=0.14, depth=0.12,
                     out=-0.07, mat="frame", transom=0.7)
    # Double doors at the back of the recess, glazed, with a transom light over them.
    window_frame(frames, door, du, DOOR_H / 2, DOOR_W - 0.04, DOOR_H - 0.04, cols=2, bar=0.15, depth=0.1,
                 out=-0.6 + 0.14, mat="wood", transom=DOOR_H / 2 - 0.62)
    frames.finish(col, bevel=0.015, segments=1)

    cloth = Buf("awnings")
    awning(cloth, facades[4], facades[4].length / 2, 3.5, facades[4].length - 1.6, 1.3, 0.72, ["awning_c", "awning_b"])
    awning(cloth, facades[1], facades[1].length / 2, 3.5, facades[1].length - 1.6, 1.3, 0.72, ["awning_a", "awning_b"])
    cloth.finish(col, bevel=0.012, segments=1)

    props = Buf("props")
    # Door kick panels (the solid lower part of each leaf) and a shop sign over the door.
    props.box(door.frame(du, 0.5, -0.6 + 0.08), (DOOR_W - 0.1, 0.06, 0.86), "wood")
    props.box(door.frame(du, 3.2, 0.3), (1.6, 0.12, 0.5), "sign")
    leaves = Buf("foliage", foliage=True)
    for f, u, zc, w, h, fi, s, k in windows:
        # SITTING ON THE SILL, NOT HOVERING IN FRONT OF IT. The sill's top is 4 cm above the
        # window bottom and reaches 0.22 m out; the old boxes were centred 0.37 m out, past the
        # sill's nose, and 1 cm above it. They now sit back on the sill and bed 5 mm into it.
        sill_top = zc - h / 2 + 0.035
        if (s + k + fi) % 3 == 0:
            soil = planter(props, f, u, sill_top, w * 0.72)
            # THREE ROUND CLUMPS, the owner's preference over one long hedge, each sunk into the
            # soil so it grows out of the bed. `count` covers the whole sphere; the part below
            # the soil is skipped.
            basis = f.frame(0, 0).to_3x3()
            for p in (-1, 0, 1):
                foliage(leaves, f.at(u + p * w * 0.23, soil + 0.07, 0.05), (0.2, 0.18, 0.2), 230, 0.13, rng,
                        floor_z=soil - 0.02, basis=basis)
        elif (s * 7 + k * 3 + fi) % 5 == 1:
            ac_unit(props, f, u + w * 0.2, sill_top - 0.005, 0.02)
    tx, ty, tz = 12.5, -12.0, TOP
    for dx in (-1.1, 1.1):
        for dy in (-1.1, 1.1):
            props.box(Matrix.Translation((tx + dx, ty + dy, tz + 1.4)), (0.22, 0.22, 2.8), "metal")
    props.box(Matrix.Translation((tx, ty, tz + 2.87)), (2.9, 2.9, 0.18), "wood")
    props.cylinder((tx, ty, tz + 4.23), 1.35, 2.5, "tank", sides=20)
    for hz in (3.33, 4.23, 5.13):
        props.cylinder((tx, ty, tz + hz), 1.40, 0.12, "metal", sides=20)
    props.cylinder((tx, ty, tz + 5.83), 1.5, 0.7, "metal", sides=20, top_scale=0.15)
    props.box(Matrix.Translation((6.5, -12.5, tz + 1.3)), (3.2, 3.6, 2.6), "brick")
    props.box(Matrix.Translation((6.5, -12.5, tz + 2.71)), (3.6, 4.0, 0.22), "stone")
    bx, by = 9.0, -2.8
    for k in range(3):
        props.box(Matrix.Translation((bx - 3 + k * 3, by, tz + 1.7)), (0.2, 0.2, 3.4), "metal")
    props.box(Matrix.Translation((bx, by, tz + 5.1)), (7.6, 0.3, 3.8), "metal")
    props.box(Matrix.Translation((bx, by + 0.2, tz + 5.1)), (7.2, 0.12, 3.4), "sign")
    props.finish(col, bevel=0.03)
    leaves.finish(col)
    return col


MODELS = {"brick_corner": brick_corner}


# ------------------------------------------------------------------ preview

def setup_lighting():
    """A sun and a sky SAVED IN THE .blend. Without them Blender's Rendered viewport shows a
    black model, because the file had no light at all; the preview renders added their own
    and threw them away. Also opens every 3D view in Material Preview."""
    scene = bpy.context.scene
    world = bpy.data.worlds.new("sky")
    scene.world = world
    if world.node_tree is None:
        world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.62, 0.78, 0.95, 1)
    bg.inputs["Strength"].default_value = 0.55
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy, sun.data.angle, sun.data.color = 4.2, math.radians(3), (1.0, 0.93, 0.82)
    sun.rotation_euler = (Vector((9, -9, 0)) - Vector((-30, 12, 42))).to_track_quat("-Z", "Y").to_euler()
    scene.collection.objects.link(sun)
    scene.view_settings.view_transform = "AgX"
    try:
        scene.view_settings.look = "AgX - Punchy"
    except TypeError:
        pass
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                for space in area.spaces:
                    if space.type == "VIEW_3D":
                        space.shading.type = "MATERIAL"


def preview(name, version):
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    gm = bpy.data.meshes.new("ground")
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=60, matrix=Matrix.Translation((9, -9, -0.01)))
    bm.to_mesh(gm)
    bm.free()
    gm.materials.append(material("ground"))
    scene.collection.objects.link(bpy.data.objects.new("ground", gm))
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    target = Vector((6, -6, 9))
    shots = []
    if PLANTER_SPOTS:
        spot, outward = PLANTER_SPOTS[0]
        shots.append(("planter", spot + outward * 1.3 + UP * 1.1 + outward.cross(UP) * 0.5, 40, spot + UP * 0.1))
    if AC_SPOTS:
        front, outward = AC_SPOTS[0]
        shots.append(("ac", front + outward * 2.2 + UP * 0.5 + outward.cross(UP) * 0.9, 45, front))
    for label, pos, lens, tgt in shots + [("street", Vector((-16, 15, 3.0)), 32, target), ("aerial", Vector((-28, 26, 30)), 38, target),
                                  ("close", Vector((-7, 5, 9)), 40, Vector((3, -2, 10))),
                                  ("detail", Vector((-2.2, 1.2, 6.2)), 35, Vector((0.6, -1.2, 6.4))),
                                  ("roof", Vector((2, 6, 42)), 30, Vector((9, -9, 18))),
                                  ("door", Vector((-3.5, 3.5, 1.7)), 30, Vector((2.1, -2.1, 1.6)))]:
        cam.location, cam.data.lens = pos, lens
        cam.rotation_euler = (tgt - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(PREVIEWS / f"{name}_{label}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[kanto] preview", scene.render.filepath)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    name = argv[0] if argv else "brick_corner"
    version = int(argv[argv.index("--preview") + 1]) if "--preview" in argv else 0
    global USE_TEXTURES
    USE_TEXTURES = "--textured" in argv
    bpy.ops.wm.read_factory_settings(use_empty=True)
    col = MODELS[name]()
    print(f"[kanto] {name}: {len(col.objects)} objects, "
          f"{sum(len(o.data.polygons) for o in col.objects)} faces before bevel")
    SOURCE.mkdir(parents=True, exist_ok=True)
    setup_lighting()
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / f"{name}.blend"), compress=True)
    backup = SOURCE / f"{name}.blend1"
    if backup.exists():
        backup.unlink()
    if version:
        preview(name, version)


if __name__ == "__main__":
    main()
