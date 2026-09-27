"""Lagoon Court water: the turquoise sea gradient and the painted foam line.

Called by tools/author_lagoon_cove.py. Two builders:

  build_sea(c, coast_distance, water_z, seabed_z, span=520.0, fine_box=(-150, -150, 120, 170))
      One flat grid at water_z whose per-vertex colour attribute "Col" runs from pale bright
      shallows at the sand, through turquoise, to a deeper teal offshore. The material reads the
      attribute through a Color Attribute node, so Eevee interpolates it smoothly across each
      face and there are no stair-steps.

  build_foam(c, coast_line, water_z)
      A flat, broken off-white ribbon just outside the shoreline, plus a thinner, sparser second
      line a few metres further out.

Reference: the saturated turquoise water with a pale band at the sand in Anastasia
Papaioanou's "Stylized Fishing Village" (docs/LAGOON_REWORK_GUIDE.md section 1).

WHY the sea grid is adaptive: coast_distance() tests about 260 segments per call in plain
Python. A 1.5 m grid over the whole 520 m sea would be 120k calls. Instead the fine grid covers
only the island's bounding box plus a margin, the distance is first sampled on a 6 m lattice,
and the exact value is computed only for fine vertices whose interpolated estimate lies inside
the colour ramp. Distance is 1-Lipschitz, so a 6 m lattice is off by at most about 4.3 m, and
the ramp is flat beyond 45 m, so everything else can take the interpolated estimate. Outside
the fine box a coarse 10 m ring carries the constant deep teal out to the horizon.

Colour rule (docs/Art_Direction.md section 1): nothing near defence blue #0080e8. Every stop
below keeps green at or above blue so the water stays a green-leaning turquoise.
"""

import math

import bmesh
import bpy
from mathutils import Vector, noise

# Ramp stops, linear RGB, as signed distance into the sea in metres (positive = further out).
SHALLOW = (0.02, 0.80, 0.70)     # 0 to 6 m: clear cyan over the sand (v11: paler read as milk)
TURQUOISE = (0.04, 0.72, 0.64)   # 6 to 25 m
DEEP = (0.012, 0.42, 0.40)       # beyond about 45 m
FOAM = (0.95, 0.97, 0.92)

FINE_STEP = 1.5
LATTICE = 6.0          # coarse distance lattice, a multiple of FINE_STEP
OUTER_STEP = 10.0
NOISE_Z = 3.17         # fixed noise slice: the whole module is deterministic


def _smooth(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3.0 - 2.0 * t)


def _mix(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


def _sea_colour(x, y, d):
    """d is the signed coast distance (negative in the sea). Returns linear RGBA."""
    # Low-frequency patchiness: the shallows boundary wanders a couple of metres and the value
    # breathes a few percent, so the band is not a perfect offset of the coast.
    n1 = noise.noise(Vector((x * 0.035, y * 0.035, NOISE_Z)))
    n2 = noise.noise(Vector((x * 0.012 + 11.3, y * 0.012 - 4.1, NOISE_Z)))
    out = -d + 1.8 * n1 + 4.0 * n2        # metres out to sea, wobbled
    col = SHALLOW
    col = _mix(col, TURQUOISE, _smooth(4.0, 11.0, out))
    col = _mix(col, DEEP, _smooth(24.0, 50.0, out))
    k = 1.0 + 0.04 * n1 + 0.03 * n2
    # ALPHA (owner, 2026-09-26: the water should be stylized like ANGRY MESH's "Stylized Water",
    # which is CLEAR over a visible sandy bottom in the shallows): see-through at the sand,
    # opaque by about 30 m out.
    alpha = 0.5 + 0.5 * _smooth(1.0, 30.0, out)   # v11: 0.28 let the pale sand wash it out
    return (col[0] * k, col[1] * k, col[2] * k, alpha)


def _sea_material():
    m = bpy.data.materials.get("sea_gradient")
    if m:
        return m
    m = bpy.data.materials.new("sea_gradient")
    m.diffuse_color = TURQUOISE + (1.0,)
    if m.node_tree is None:
        m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    attr = nt.nodes.new("ShaderNodeVertexColor")
    attr.layer_name = "Col"
    attr.location = (-320, 260)
    # CAUSTIC LINES: the reference water's signature is a net of bright cellular lines drifting
    # over the shallows. Two Voronoi "distance to edge" layers at different scales, thresholded
    # into thin lines, strongest where the water is clear and faint offshore. They lighten the
    # colour toward white and raise the alpha so the lines stay bright over the sand. In Unity
    # the same pattern animates in LagoonWater.shader (guide § 8 step 7); here it is still.
    coords = nt.nodes.new("ShaderNodeTexCoord")
    # Review v10: undistorted cells read as a cracked tile floor. A low-frequency noise bends the
    # coordinates so each cell wobbles like light through a moving surface.
    warp = nt.nodes.new("ShaderNodeTexNoise")
    warp.inputs["Scale"].default_value = 0.4    # bends WITHIN a cell (0.09 only drifted whole cells)
    nt.links.new(coords.outputs["Object"], warp.inputs["Vector"])
    bend = nt.nodes.new("ShaderNodeVectorMath")
    bend.operation = "MULTIPLY_ADD"
    bend.inputs[1].default_value = (1.3, 1.3, 0.0)
    nt.links.new(warp.outputs["Color"], bend.inputs[0])
    nt.links.new(coords.outputs["Object"], bend.inputs[2])
    lines = None
    for scale, offset in ((0.34, (0, 0, 0)), (0.55, (13.1, 7.3, 0))):
        shift = nt.nodes.new("ShaderNodeVectorMath")
        shift.operation = "ADD"
        shift.inputs[1].default_value = offset
        nt.links.new(bend.outputs["Vector"], shift.inputs[0])
        vor = nt.nodes.new("ShaderNodeTexVoronoi")
        vor.feature = "DISTANCE_TO_EDGE"
        vor.inputs["Scale"].default_value = scale
        nt.links.new(shift.outputs["Vector"], vor.inputs["Vector"])
        edge = nt.nodes.new("ShaderNodeMapRange")
        edge.inputs["From Min"].default_value = 0.0
        edge.inputs["From Max"].default_value = 0.035
        edge.inputs["To Min"].default_value = 1.0
        edge.inputs["To Max"].default_value = 0.0
        nt.links.new(vor.outputs["Distance"], edge.inputs["Value"])
        if lines is None:
            lines = edge.outputs["Result"]
        else:
            both = nt.nodes.new("ShaderNodeMath")
            both.operation = "MAXIMUM"
            nt.links.new(lines, both.inputs[0])
            nt.links.new(edge.outputs["Result"], both.inputs[1])
            lines = both.outputs["Value"]
    clear = nt.nodes.new("ShaderNodeMath")          # 1 in the clear shallows, 0 offshore
    clear.operation = "SUBTRACT"
    clear.inputs[0].default_value = 1.0
    nt.links.new(attr.outputs["Alpha"], clear.inputs[1])
    # Review v10: the net ran evenly to the horizon. Now it is squared toward the shallows
    # (clear^2, gone by about 25 m out) and broken into drifting PATCHES by a large noise mask.
    clear2 = nt.nodes.new("ShaderNodeMath")
    clear2.operation = "POWER"
    clear2.inputs[1].default_value = 2.0
    nt.links.new(clear.outputs["Value"], clear2.inputs[0])
    patch_noise = nt.nodes.new("ShaderNodeTexNoise")
    patch_noise.inputs["Scale"].default_value = 0.035
    nt.links.new(coords.outputs["Object"], patch_noise.inputs["Vector"])
    patch = nt.nodes.new("ShaderNodeMapRange")
    patch.inputs["From Min"].default_value = 0.42
    patch.inputs["From Max"].default_value = 0.62
    nt.links.new(patch_noise.outputs["Fac"], patch.inputs["Value"])
    mask = nt.nodes.new("ShaderNodeMath")
    mask.operation = "MULTIPLY"
    nt.links.new(clear2.outputs["Value"], mask.inputs[0])
    nt.links.new(patch.outputs["Result"], mask.inputs[1])
    strength = nt.nodes.new("ShaderNodeMath")
    strength.operation = "MULTIPLY"
    strength.use_clamp = True
    nt.links.new(lines, strength.inputs[0])
    nt.links.new(mask.outputs["Value"], strength.inputs[1])
    gain = nt.nodes.new("ShaderNodeMath")
    gain.operation = "MULTIPLY"
    gain.use_clamp = True
    gain.inputs[1].default_value = 1.6
    nt.links.new(strength.outputs["Value"], gain.inputs[0])
    strength = gain
    tint = nt.nodes.new("ShaderNodeMix")
    tint.data_type = "RGBA"
    tint.inputs["B"].default_value = (0.85, 1.0, 0.93, 1.0)
    nt.links.new(strength.outputs["Value"], tint.inputs["Factor"])
    nt.links.new(attr.outputs["Color"], tint.inputs["A"])
    nt.links.new(tint.outputs["Result"], bsdf.inputs["Base Color"])
    alpha = nt.nodes.new("ShaderNodeMath")
    alpha.operation = "MAXIMUM"
    nt.links.new(attr.outputs["Alpha"], alpha.inputs[0])
    nt.links.new(strength.outputs["Value"], alpha.inputs[1])
    nt.links.new(alpha.outputs["Value"], bsdf.inputs["Alpha"])
    if hasattr(m, "surface_render_method"):
        m.surface_render_method = "BLENDED"
    bsdf.inputs["Roughness"].default_value = 0.25
    # A little specular only (v12 tried 0.04: no visible change, the mint came from the seabed).
    for name, value in (("Specular IOR Level", 0.1), ("Specular", 0.1)):
        if name in bsdf.inputs:
            bsdf.inputs[name].default_value = value
            break
    return m


def build_sea(c, coast_distance, water_z, seabed_z, span=520.0, fine_box=(-150.0, -150.0, 120.0, 170.0)):
    """The sea plane with a baked shallow-to-deep colour gradient. seabed_z is accepted so the
    call site reads the same as the rest of the layout; the plane itself is opaque and sits at
    water_z, and the gradient is driven by distance from the coast, not by depth.

    fine_box (x0, y0, x1, y1) is where the 1.5 m grid goes. The default is the cove v6 coast's
    bounding box (about x -96..66, y -95..112) grown by about 54 m on every side, so the ramp has
    finished before the coarse ring starts. Grow it if the coast is ever moved outward."""
    del seabed_z
    half = span / 2.0
    x0, y0, x1, y1 = (max(-half, min(half, v)) for v in fine_box)
    # Make the fine box a whole number of lattice cells so both grids share their corners.
    x1 = x0 + math.ceil((x1 - x0) / LATTICE) * LATTICE
    y1 = y0 + math.ceil((y1 - y0) / LATTICE) * LATTICE

    calls = 0
    # 1. The coarse distance lattice.
    lx = int(round((x1 - x0) / LATTICE))
    ly = int(round((y1 - y0) / LATTICE))
    lat = [[coast_distance(x0 + i * LATTICE, y0 + j * LATTICE) for i in range(lx + 1)] for j in range(ly + 1)]
    calls += (lx + 1) * (ly + 1)

    # 2. The fine grid, exact only where the ramp is changing.
    per = int(round(LATTICE / FINE_STEP))
    nx, ny = lx * per, ly * per
    verts, cols = [], []
    for j in range(ny + 1):
        y = y0 + j * FINE_STEP
        cj, fj = divmod(j, per)
        if cj >= ly:
            cj, fj = ly - 1, per
        v = fj / per
        for i in range(nx + 1):
            x = x0 + i * FINE_STEP
            ci, fi = divmod(i, per)
            if ci >= lx:
                ci, fi = lx - 1, per
            u = fi / per
            est = ((lat[cj][ci] * (1 - u) + lat[cj][ci + 1] * u) * (1 - v)
                   + (lat[cj + 1][ci] * (1 - u) + lat[cj + 1][ci + 1] * u) * v)
            if -54.0 < est < 5.0 and not (fi == 0 and fj == 0):
                d = coast_distance(x, y)
                calls += 1
            else:
                d = est
            verts.append((x, y, water_z))
            cols.append(_sea_colour(x, y, d))
    faces = []
    w = nx + 1
    for j in range(ny):
        for i in range(nx):
            a = j * w + i
            faces.append((a, a + 1, a + 1 + w, a + w))

    # 3. The coarse outer ring, in four strips around the fine box (flat, so the T-junctions
    # along the seam cannot open a crack).
    def strip(ax, ay, bx, by):
        if bx - ax < 1e-6 or by - ay < 1e-6:
            return
        cx = max(1, int(round((bx - ax) / OUTER_STEP)))
        cy = max(1, int(round((by - ay) / OUTER_STEP)))
        base = len(verts)
        for j in range(cy + 1):
            y = ay + (by - ay) * j / cy
            for i in range(cx + 1):
                x = ax + (bx - ax) * i / cx
                verts.append((x, y, water_z))
                cols.append(_sea_colour(x, y, -200.0))
        for j in range(cy):
            for i in range(cx):
                a = base + j * (cx + 1) + i
                faces.append((a, a + 1, a + cx + 2, a + cx + 1))

    strip(-half, -half, half, y0)          # south
    strip(-half, y1, half, half)           # north
    strip(-half, y0, x0, y1)               # west
    strip(x1, y0, half, y1)                # east

    me = bpy.data.meshes.new("sea")
    me.from_pydata(verts, [], faces)
    attr = me.color_attributes.new("Col", "FLOAT_COLOR", "POINT")
    flat = [ch for col in cols for ch in col]
    attr.data.foreach_set("color", flat)
    me.color_attributes.active_color = attr
    me.color_attributes.render_color_index = me.color_attributes.find("Col")
    me.materials.append(_sea_material())
    me.update()
    o = bpy.data.objects.new("sea", me)
    o.visible_shadow = False   # clear water must not shade the seabed it shows
    c.objects.link(o)
    o["coast_distance_calls"] = calls
    print(f"[lagoon-water] sea: {len(verts)} verts, {len(faces)} faces, {calls} coast_distance calls")
    return o


# ---------------------------------------------------------------- foam

def _resample(pts, step):
    """A closed polyline resampled to an even spacing along its length."""
    n = len(pts)
    segs, total = [], 0.0
    for i in range(n):
        a, b = pts[i], pts[(i + 1) % n]
        L = math.hypot(b[0] - a[0], b[1] - a[1])
        segs.append((a, b, L, total))
        total += L
    count = max(8, int(total / step))
    out, k = [], 0
    for s_i in range(count):
        s = total * s_i / count
        while k < n - 1 and segs[k][3] + segs[k][2] < s:
            k += 1
        a, b, L, s0 = segs[k]
        t = 0.0 if L < 1e-9 else (s - s0) / L
        out.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, s))
    return out, total


def _outward_normals(pts, ccw):
    n = len(pts)
    normals = []
    for i in range(n):
        p0, p2 = pts[i - 1], pts[(i + 1) % n]
        p1 = pts[i]
        acc = [0.0, 0.0]
        for a, b in ((p0, p1), (p1, p2)):
            dx, dy = b[0] - a[0], b[1] - a[1]
            L = math.hypot(dx, dy) or 1.0
            # For a counter-clockwise loop the outside is to the right of travel.
            nx, ny = (dy / L, -dx / L) if ccw else (-dy / L, dx / L)
            acc[0] += nx
            acc[1] += ny
        L = math.hypot(*acc) or 1.0
        normals.append((acc[0] / L, acc[1] / L))
    return normals


def _ribbon(bm, layer, ring, normals, inner, outer_fn, cover_fn, z, colour):
    """Quads between inner and inner + width(s) * cover(s); cover 0 is a gap. Dashes taper to a
    point at each end, which is what makes it read as a brush stroke rather than a tube."""
    n = len(ring)
    prev = None
    for i in range(n + 1):
        x, y, s = ring[i % n]
        nx, ny = normals[i % n]
        g = cover_fn(s)
        if g <= 0.0:
            prev = None
            continue
        w = outer_fn(s) * g
        a = bm.verts.new((x + nx * inner, y + ny * inner, z))
        b = bm.verts.new((x + nx * (inner + w), y + ny * (inner + w), z))
        if prev is not None:
            f = bm.faces.new((prev[0], a, b, prev[1]))
            f.smooth = False
            for loop in f.loops:
                loop[layer] = colour
        prev = (a, b)


def _foam_material():
    m = bpy.data.materials.get("foam")
    if m:
        return m
    m = bpy.data.materials.new("foam")
    m.diffuse_color = FOAM + (1.0,)
    if m.node_tree is None:
        m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    attr = nt.nodes.new("ShaderNodeVertexColor")
    attr.layer_name = "Col"
    attr.location = (-320, 260)
    nt.links.new(attr.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.6
    # A little self-light so the painted line stays whiter than the sand beside it; lit only by
    # the sun it rendered a grey a shade darker than the beach and vanished at a distance.
    if "Emission Color" in bsdf.inputs:
        nt.links.new(attr.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = 0.8
    return m


def build_foam(c, coast_line, water_z):
    """The painted foam line just outside the shoreline, and a sparser second line further out."""
    area = sum(coast_line[i][0] * coast_line[(i + 1) % len(coast_line)][1]
               - coast_line[(i + 1) % len(coast_line)][0] * coast_line[i][1] for i in range(len(coast_line)))
    ccw = area > 0.0
    ring, _total = _resample(coast_line, 0.6)
    normals = _outward_normals([(p[0], p[1]) for p in ring], ccw)
    # Smooth the normals a little so tight bends do not fan the outer edge into spikes.
    for _ in range(3):
        m = len(normals)
        sm = []
        for i in range(m):
            ax = normals[i - 1][0] + 2 * normals[i][0] + normals[(i + 1) % m][0]
            ay = normals[i - 1][1] + 2 * normals[i][1] + normals[(i + 1) % m][1]
            L = math.hypot(ax, ay) or 1.0
            sm.append((ax / L, ay / L))
        normals = sm

    def n1(s, f, z):
        return noise.noise(Vector((s * f, 0.37, z)))

    def near_width(s):
        # 0.3 m inner edge, outer edge 0.8 to 2.2 m out, so the band is 0.5 to 1.9 m wide.
        return 0.5 + 1.4 * (0.5 + 0.5 * max(-1.0, min(1.0, 1.6 * n1(s, 0.045, 1.9))))

    def near_cover(s):
        # Mostly continuous with a few short gaps, each tapered at both ends.
        v = n1(s, 0.03, 5.3) + 0.5 * n1(s, 0.11, 8.8)
        return _smooth(-0.32, -0.08, v)

    def far_width(s):
        return 0.35 + 0.35 * (0.5 + 0.5 * n1(s, 0.07, 12.4))

    def far_cover(s):
        # Sparse dashes: only the crests of a faster noise.
        v = n1(s, 0.09, 15.6) + 0.35 * n1(s, 0.3, 21.2)
        return _smooth(0.12, 0.38, v)

    z = water_z + 0.03
    bm = bmesh.new()
    layer = bm.loops.layers.float_color.new("Col")
    _ribbon(bm, layer, ring, normals, 0.3, near_width, near_cover, z, FOAM + (1.0,))
    soft = tuple(ch * 0.82 for ch in FOAM) + (1.0,)
    _ribbon(bm, layer, ring, normals, 3.2, far_width, far_cover, z, soft)
    me = bpy.data.meshes.new("foam")
    bm.to_mesh(me)
    bm.free()
    # Loop colours were only a convenient way to paint per ribbon; store them per point, the same
    # domain as the sea, so the material reads one kind of attribute.
    loop_attr = me.color_attributes["Col"]
    per_point = [None] * len(me.vertices)
    for loop in me.loops:
        per_point[loop.vertex_index] = tuple(loop_attr.data[loop.index].color)
    me.color_attributes.remove(loop_attr)
    attr = me.color_attributes.new("Col", "FLOAT_COLOR", "POINT")
    attr.data.foreach_set("color", [ch for col in per_point for ch in (col or FOAM + (1.0,))])
    me.color_attributes.active_color = attr
    me.color_attributes.render_color_index = me.color_attributes.find("Col")
    me.materials.append(_foam_material())
    for p in me.polygons:
        p.use_smooth = False
    me.update()
    o = bpy.data.objects.new("foam", me)
    c.objects.link(o)
    print(f"[lagoon-water] foam: {len(me.vertices)} verts, {len(me.polygons)} faces, ccw={ccw}")
    return o
