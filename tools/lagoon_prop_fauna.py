"""Lagoon Cove FAUNA KIT: the seabirds that wheel over the cove and the reef fish that school under
the stilt village, built as separate moving parts so the runtime can flock and animate them.

  blender -b --python tools/lagoon_prop_fauna.py -- --export        # write every .glb
  blender -b --python tools/lagoon_prop_fauna.py -- --preview N     # render the review sheets
  blender -b --python tools/lagoon_prop_fauna.py -- --preview N --only pigeon   # pigeon sheets only

WHY THIS KIT EXISTS. OWNER, 2026-09-27, on Lagoon Cove: "add birds and fish (via boids)". The
runtime spawns flocks of birds and schools of fish and moves them with a boids simulation, then
flaps the wings and wags the tails in code. So a model here is not one mesh: it is a root empty
with a few child objects whose ORIGINS sit exactly at the joints the code rotates about. No
armature, no animation clips: rotating a child transform is the cheapest animation there is, and
a school of forty fish must stay cheap.

MODELS (each a root empty named as the model, at the model's centre of mass):

  * "fauna_seabird"   a chunky cartoon tern. Children "body" (with the beak and eyes in it),
                      "wing_l", "wing_r" and "tail". Each wing's origin is its SHOULDER; the
                      wings are modelled spread flat in the glide pose, so flapping is a rotation
                      about the wing's local FORWARD axis (Blender -Y, Unity +Z). The tail's
                      origin is its root, for a small pitch or fan wobble.
  * "fauna_pigeon"    a plump cartoon city rock pigeon for the Kanto court (0.66 m span, 0.41 m
                      long). Children "body" (with the legs and feet), "head" (origin at the
                      neck, pitch it to peck), "wing_l", "wing_r" (origins at the shoulders, as
                      the tern) and "tail". The recommended ground fold is FOLD_BEST: sweep 84
                      degrees back, THEN roll 22 degrees about the body's forward axis (see
                      fold_wings). Its sheets: pigeon_lineup, pigeon_close, pigeon_fold,
                      pigeon_court (12 m, 1.4 m eye, 95 degrees), pigeon_court_zoom, pigeon_flock.
  * "fauna_fish_a"    a yellow tang: a tall round DISC with long fins along back and belly.
  * "fauna_fish_b"    a pink-and-cream banded fish with a tall swept pink sail of a dorsal fin.
  * "fauna_fish_c"    a fat sea-green parrotfish torpedo with a lime belly and a square tail.
  * "fauna_fish_d"    a slim silver jack with a deep forked YELLOW tail.
                      Every fish has children "body" and "tail"; the tail's origin is the tail
                      JOINT, so wagging is a rotation about the tail's local UP axis (Blender +Z,
                      Unity +Y). Four species, four silhouettes (disc, sail, torpedo, fork), so a
                      mixed school reads as several kinds even as dark shapes against the light.

CONVENTION (the runtime assumes it; do not change one without the other). Metres, real scale.
Every model faces Blender -Y (nose toward -Y), up is +Z, and the root origin is the centre of mass
(the volume centroid of all its closed parts). Blender's standard glTF export (+Y up) turns
-Y forward into glTF +Z, which Unity's importer keeps as +Z: the model faces +Z in Unity, Unity's
own forward. The bird's LEFT wing lies on Blender +X; after the importer's handedness flip it is
on Unity -X, which is the left of a +Z-facing body, so wing_l stays the left wing.

ONE MESH PER PART, SEVERAL MATERIALS, as in lagoon_prop_seabed: each child is one mesh object
whose faces carry material indices. The fins and the beak are closed shells merged INTO the body
mesh (they never move on their own); only the parts the code rotates are separate objects.

CHUNKY, CUTE, NEVER FIDDLY (Art_Direction.md section 0, LAGOON_REWORK_GUIDE.md section 2). Every
form is a fat lofted shell with a slightly squared (superellipse) cross-section, the fins are
soft PILLOWS rather than thin blades, the eyes are big and dark, and no part has a detail smaller
than about 3 cm. Real feathers, scales, fin rays and gill slits are exactly the detail the house
style leaves out. The bird is judged from BELOW at 20 to 60 m against a peach sunset sky, which
is how a player sees it: what matters there is a bold white cross with dark wingtips, a dark cap
and a long forked tail, not anything on its back.

MATERIALS: flat colours only, one Principled BSDF per colour, roughness 0.7, no textures, names
"fauna_<colour>" (COLOURS below, sRGB hex; the material stores the linear value). ROLE HUES
(Art_Direction.md section 1): nothing within 15 degrees of offence orange #f87020 (hue 24) or 25
degrees of defence blue #0080e8 (hue 207); greys under 20 per cent saturation are exempt.
audit_palette() measures every colour at import and refuses a violation. That is why there is no
orange clownfish, no blue tang and no orange beak here: the tang is YELLOW, the beak pale yellow.

TRIANGLES: birds under about 1500, fish under about 800 (a school is dozens of them).
check() prints each model's size and triangle count; --export refuses a model over budget.

EXPORT writes Assets/TumbangPreso/Art/LagoonCove/Fauna/<model>.glb, one per model: only that
model's objects selected, Y up, modifiers applied, hierarchy and object names kept, no animations,
cameras or lights. Nothing is saved to a .blend.

PREVIEW writes Logs/lagoon-blender/: fauna_lineup_vN.png (every model side by side on pale sand
beside a 1.6 m pink scale cylinder, three-quarter view), fauna_below_vN.png (the bird close, seen
from straight below against a peach sky, its underside design) and fauna_sky_vN.png (a flock at
8, 20, 40 and 60 m from a 1.25 m eye at the game's 95 degree field of view: the readability test
that matters). An existing render is never overwritten: bump N.
"""

import colorsys
import math
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
OUT_DIR = REPO / "Assets" / "TumbangPreso" / "Art" / "LagoonCove" / "Fauna"
LOG_DIR = REPO / "Logs" / "lagoon-blender"

# sRGB hex. Every colour is a mid-to-light value because the fish are seen through turquoise water
# and the bird against a bright sky: a dark or muddy colour turns into a black blob at 30 m.
COLOURS = {
    "fauna_white": "f7f3ea",        # the tern's body and underwing, a warm white, never pure
    "fauna_grey": "b9b5af",         # the tern's back and wing tops, a soft warm grey
    "fauna_cap": "2b2727",          # the tern's cap, warm near-black
    "fauna_tip": "3d3836",          # the tern's wingtips and tail tips, a touch lighter than the cap
    "fauna_shade": "7f7a75",        # the tern's underwing trailing edge, a mid warm grey (see wing)
    "fauna_beak": "f2d46b",         # pale butter yellow (hue 47), well clear of offence orange
    "fauna_eye": "1a1616",          # every eye: one big dark dot, ink-style, no white
    "fauna_yellow": "f5cc1e",       # fish_a, the tang
    "fauna_yellow_deep": "e9b91c",  # fish_a fins, fish_d tail: a slightly deeper yellow (hue 45)
    "fauna_pink": "f07ca2",         # fish_b bands
    "fauna_cream": "f5e6c6",        # fish_b bands, fish_c snout
    "fauna_seagreen": "3eba8a",     # fish_c back
    "fauna_lime": "a6d24a",         # fish_c belly, fins and tail
    "fauna_silver": "dcdad4",       # fish_d flanks and belly, warm silver
    "fauna_silver_dark": "8f8c86",  # fish_d back and dorsal fin
    # The pigeon's greys are the rock pigeon's blue-grey kept DESATURATED (under 6 per cent)
    # so they stay exempt from the role-hue rule and can never read as the defence blue.
    "fauna_pigeon_grey": "a6a3a8",         # body, rump and tail
    "fauna_pigeon_pale": "c6c3c7",         # wing tops and underwing, lighter than the body
    "fauna_pigeon_dark": "5f5c63",         # head, chest and primaries
    "fauna_pigeon_neck_green": "7fb58f",   # neck patch, soft green (hue 138), mid value
    "fauna_pigeon_neck_purple": "a584b2",  # neck patch, soft purple (hue 283), mid value
    "fauna_pigeon_eyering": "e05a74",      # eye ring, pink-red (hue 348), clear of orange
    "fauna_pigeon_feet": "e0808e",         # legs and feet, pinkish red (hue 351)
}
ROLE_HUES = ((24.0, 15.0, "offence orange #f87020"), (207.0, 25.0, "defence blue #0080e8"))
TRI_BUDGET = {"fauna_seabird": 1500, "fauna_pigeon": 1200}
FISH_TRI_BUDGET = 800


def _rgb(hexs):
    return tuple(int(hexs[i:i + 2], 16) / 255 for i in (0, 2, 4))


def audit_palette():
    """Every colour measured against the role hues. Raises on a violation, returns the table."""
    rows = []
    for name, hexs in COLOURS.items():
        h, s, _v = colorsys.rgb_to_hsv(*_rgb(hexs))
        hue = h * 360
        for role, gap, label in ROLE_HUES:
            d = abs((hue - role + 180) % 360 - 180)
            if s >= 0.2 and d < gap:
                raise ValueError(f"[fauna] {name} hue {hue:.0f} is {d:.0f} deg from {label}")
        rows.append((name, hexs, round(hue), round(s, 2)))
    return rows


audit_palette()

try:
    import bpy
    import bmesh
    from mathutils import Matrix, Vector
except ImportError:          # imported outside Blender: only the palette audit is available
    bpy = None

if bpy is not None:

    # ============================================================ geometry helpers
    # Every part is built into one bmesh from closed SHELLS. Each face carries two int layers:
    # "shell" (which shell it came from), "station" (which band of a loft) and "seg" (which
    # segment round the ring), so the colour rule can paint by region after the normals are
    # settled. ⚠️ Colour boundaries follow the loft's own edges (station and ring segment), never
    # a per-face normal test: on a mesh this coarse a normal test zigzags (v1 of the jack's back).

    def _srgb_to_linear(c):
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4

    def material(name):
        m = bpy.data.materials.get(name)
        if m:
            return m
        rgb = tuple(_srgb_to_linear(c) for c in _rgb(COLOURS[name]))
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.7
        bsdf.inputs["Metallic"].default_value = 0.0
        m.diffuse_color = (*rgb, 1.0)
        m.roughness = 0.7
        return m

    def chaikin(points, iterations=2):
        """Round a closed control polygon. Two passes keep the shape's intent (a fork stays a fork)
        while every corner becomes a soft round, the chunky cartoon edge."""
        pts = [Vector(p) for p in points]
        for _ in range(iterations):
            out = []
            for i, a in enumerate(pts):
                b = pts[(i + 1) % len(pts)]
                out += [a.lerp(b, 0.25), a.lerp(b, 0.75)]
            pts = out
        return pts

    def resample(points, k):
        """k points evenly spaced by arc length round a closed polygon, so a fin's triangle count
        is set by k, not by how many control points it happened to be drawn with."""
        pts = [Vector(p) for p in points]
        segs = [(pts[i], pts[(i + 1) % len(pts)]) for i in range(len(pts))]
        total = sum((b - a).length for a, b in segs)
        out, acc, si = [], 0.0, 0
        for j in range(k):
            target = total * j / k
            while acc + (segs[si][1] - segs[si][0]).length < target:
                acc += (segs[si][1] - segs[si][0]).length
                si += 1
            a, b = segs[si]
            ln = (b - a).length or 1e-9
            out.append(a.lerp(b, (target - acc) / ln))
        return out

    def superellipse(center, u, v, a, b, n, p=2.6):
        """A ring round center in the plane (u, v). p > 2 squares the ellipse a little: the
        slightly boxy section is what makes a round fish look like it belongs in a blocky cast."""
        ring = []
        for i in range(n):
            t = 2 * math.pi * i / n
            c, s = math.cos(t), math.sin(t)
            x = math.copysign(abs(c) ** (2 / p), c) * a
            y = math.copysign(abs(s) ** (2 / p), s) * b
            ring.append(Vector(center) + Vector(u) * x + Vector(v) * y)
        return ring

    class Part:
        """One child object under construction: a bmesh plus the shell and station tags."""

        def __init__(self):
            self.bm = bmesh.new()
            self.shell_layer = self.bm.faces.layers.int.new("shell")
            self.station_layer = self.bm.faces.layers.int.new("station")
            self.seg_layer = self.bm.faces.layers.int.new("seg")
            self.ring_n = {}
            self.shell_names = []

        def _tag(self, faces, shell, station, seg=0):
            if shell not in self.shell_names:
                self.shell_names.append(shell)
            sid = self.shell_names.index(shell)
            for f in faces:
                f[self.shell_layer] = sid
                f[self.station_layer] = station
                f[self.seg_layer] = seg

        def loft(self, shell, rings, start_pole, end_pole):
            """A closed tube through rings (equal length), capped with a fan to a pole at each
            end. The pole is where a nose, tail tip or wingtip comes to a round point."""
            bm = self.bm
            vr = [[bm.verts.new(p) for p in ring] for ring in rings]
            n = len(rings[0])
            for i in range(len(vr) - 1):
                for j in range(n):
                    f = bm.faces.new((vr[i][j], vr[i][(j + 1) % n], vr[i + 1][(j + 1) % n],
                                      vr[i + 1][j]))
                    self._tag([f], shell, i, j)
            ps = bm.verts.new(start_pole)
            pe = bm.verts.new(end_pole)
            for j in range(n):
                self._tag([bm.faces.new((ps, vr[0][(j + 1) % n], vr[0][j]))], shell, -1, j)
                self._tag([bm.faces.new((pe, vr[-1][j], vr[-1][(j + 1) % n]))], shell,
                          len(vr) - 1, j)
            self.ring_n[shell] = n

        def pillow(self, shell, center, u, v, outline, t, k=12, inset=0.72):
            """A soft slab: the outline (2D, in the plane u, v) as its rim, both faces domed in
            toward the middle. It is the chunky fin: thick at the root, soft at the edge, never
            a razor blade that vanishes edge-on."""
            u, v = Vector(u).normalized(), Vector(v).normalized()
            nrm = u.cross(v).normalized()
            pts = resample(chaikin(outline), k)
            cen = sum(pts, Vector((0, 0))) / len(pts)
            c = Vector(center)

            def at(p2, off):
                return c + u * p2.x + v * p2.y + nrm * off

            mid = [at(p, 0) for p in pts]
            lo = [at(cen + (p - cen) * inset, -t / 2) for p in pts]
            hi = [at(cen + (p - cen) * inset, t / 2) for p in pts]
            self.loft(shell, [lo, mid, hi], at(cen, -t / 2), at(cen, t / 2))

        def sphere(self, shell, center, radius, segments=8, rings=5, scale=(1, 1, 1)):
            mat = Matrix.Translation(center) @ Matrix.Diagonal((*scale, 1))
            res = bmesh.ops.create_uvsphere(self.bm, u_segments=segments, v_segments=rings,
                                            radius=radius, matrix=mat)
            faces = {f for vtx in res["verts"] for f in vtx.link_faces}
            self._tag(faces, shell, 0)

        def cone(self, shell, base, tip, r_base, r_tip, segments=8, squash=0.8):
            base, tip = Vector(base), Vector(tip)
            d = tip - base
            rot = d.to_track_quat("Z", "Y").to_matrix().to_4x4()
            mat = (Matrix.Translation((base + tip) / 2) @ rot
                   @ Matrix.Diagonal((1, squash, 1, 1)))
            res = bmesh.ops.create_cone(self.bm, cap_ends=True, segments=segments,
                                        radius1=r_base, radius2=r_tip, depth=d.length,
                                        matrix=mat)
            faces = {f for vtx in res["verts"] for f in vtx.link_faces}
            self._tag(faces, shell, 0)

        def finish(self, rule):
            """Settle outward normals, paint every face by the rule, and return the bmesh.
            rule(shell, station, centroid, normal, a) -> material name, where a is the ring
            angle of the face's segment in radians (sin(a) > 0 is the upper half of a loft)."""
            bm = self.bm
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
            mats = []
            for f in bm.faces:
                shell = self.shell_names[f[self.shell_layer]]
                a = 2 * math.pi * (f[self.seg_layer] + 0.5) / self.ring_n.get(shell, 1)
                f.smooth = True
                name = rule(shell, f[self.station_layer], f.calc_center_median(), f.normal, a)
                if name not in mats:
                    mats.append(name)
                f.material_index = mats.index(name)
            return bm, mats

    def volume_moments(bm):
        """Signed volume and first moment of a closed mesh (tetrahedra from the origin). Summed
        over every part it gives the centre of mass the root origin must sit on."""
        vol, mom = 0.0, Vector((0, 0, 0))
        for f in bm.faces:
            vs = [v.co for v in f.verts]
            for i in range(1, len(vs) - 1):
                a, b, c = vs[0], vs[i], vs[i + 1]
                dv = a.dot(b.cross(c)) / 6
                vol += dv
                mom += (a + b + c) / 4 * dv
        return vol, mom

    def assemble(model, parts):
        """parts: list of (child name, joint in model space, Part, rule). Builds the root empty at
        the centre of mass and every child with its origin at its joint."""
        built = []
        vol, mom = 0.0, Vector((0, 0, 0))
        for name, joint, part, rule in parts:
            bm, mats = part.finish(rule)
            dv, dm = volume_moments(bm)
            vol += dv
            mom += dm
            built.append((name, Vector(joint), bm, mats))
        com = mom / vol
        coll = bpy.data.collections.new(model)
        bpy.context.scene.collection.children.link(coll)
        root = bpy.data.objects.new(model, None)
        root.empty_display_type = "PLAIN_AXES"
        root.empty_display_size = 0.2
        coll.objects.link(root)
        for name, joint, bm, mats in built:
            bmesh.ops.translate(bm, vec=-joint, verts=bm.verts)
            me = bpy.data.meshes.new(f"{model}_{name}")
            bm.to_mesh(me)
            bm.free()
            for m in mats:
                me.materials.append(material(m))
            for p in me.polygons:
                p.use_smooth = True
            ob = bpy.data.objects.new(name, me)
            coll.objects.link(ob)
            ob.parent = root
            ob.location = joint - com
        root["fauna_model"] = model
        return root

    # ============================================================ the seabird
    # A chunky cartoon tern. Proportions are pushed for cuteness and for the view from below: a
    # big round head (0.16 m across on a 0.18 m body), a stubby body, broad wings and a long
    # forked tail, so the silhouette at 60 m is an unmistakable white cross with dark ends.

    # Wing length from the shoulder; span = 2 * (0.066 + 0.585) = 1.30 m, the top of the brief's
    # range, because every centimetre of wing is what survives at 60 m.
    BIRD_SPAN_HALF = 0.585
    SHOULDER = Vector((0.066, -0.035, 0.042))

    def build_seabird():
        body = Part()
        # (y, z centre, half width, half height), nose to tail. The head bulges, the neck pinches
        # a little, the body is widest just behind the shoulders, and it tapers into the tail.
        st = [(-0.228, 0.050, 0.042, 0.040), (-0.205, 0.052, 0.074, 0.070),
              (-0.170, 0.052, 0.088, 0.082), (-0.132, 0.048, 0.084, 0.078),
              (-0.100, 0.034, 0.078, 0.070), (-0.060, 0.016, 0.092, 0.080),
              (0.000, 0.004, 0.104, 0.088), (0.060, 0.002, 0.096, 0.080),
              (0.120, 0.008, 0.072, 0.058), (0.170, 0.014, 0.046, 0.036),
              (0.200, 0.018, 0.026, 0.021)]
        rings = [superellipse((0, y, z), (1, 0, 0), (0, 0, 1), a, b, 16, 2.3) for y, z, a, b in st]
        body.loft("body", rings, (0, -0.240, 0.050), (0, 0.214, 0.019))
        # A short thick beak, pointed but blunt-tipped, dipping slightly: the pale yellow is the
        # brightest warm spot on the bird and reads even from below.
        body.cone("beak", (0, -0.212, 0.040), (0, -0.335, 0.026), 0.030, 0.006, 8, 0.78)
        # Big dark eyes just under the cap line, set wide so they read from the side and below.
        for sx in (1, -1):
            body.sphere("eye", (sx * 0.066, -0.196, 0.058), 0.019, 8, 5)

        def body_rule(shell, station, c, n, a):
            if shell == "beak":
                return "fauna_beak"
            if shell == "eye":
                return "fauna_eye"
            # The cap: forehead, crown and nape, a clean helmet whose lower edge runs along a
            # ring row just above the eyes (stations -1 to 2 are the head, 3 the nape).
            up = math.sin(a)
            if (station <= 2 and up > 0.2) or (station == 3 and up > 0.55):
                return "fauna_cap"
            if 4 <= station <= 8 and up > 0.55:
                return "fauna_grey"     # the mantle, continuous with the wing tops
            return "fauna_white"

        wings = []
        for side, name in ((1, "wing_l"), (-1, "wing_r")):
            w = Part()
            # Span stations s in [0, 1] from shoulder to tip. The chord is broad at the root and
            # narrows to a round-pointed tip; the leading edge sweeps back toward the tip, the
            # tern's crooked swept wing simplified to one clean curve. A slight dihedral lifts
            # the tip 4 cm so the glide pose is not a dead flat plank.
            ss = [0.0, 0.14, 0.28, 0.42, 0.56, 0.68, 0.79, 0.88, 0.95]
            rings = []
            for s in ss:
                x = -0.035 + s * (BIRD_SPAN_HALF + 0.035)
                chord = 0.28 * (1 - 0.45 * s) * math.sqrt(max(0.0, 1 - s ** 4)) + 0.012
                y_le = -0.080 + 0.20 * s ** 1.7
                thick = 0.060 * (1 - 0.60 * s)
                zc = 0.04 * s
                ring = []
                for i in range(10):
                    t = 2 * math.pi * i / 10
                    u = (1 - math.cos(t)) / 2           # 0 leading edge, 1 trailing edge
                    camber = 0.018 * math.sin(math.pi * u) * (1 - s)
                    z = zc + camber + thick / 2 * math.sin(t) * (1 - 0.65 * u)
                    ring.append(Vector((side * x, y_le + chord * u, z)))
                rings.append(ring)
            tip_x = side * BIRD_SPAN_HALF
            root_c = sum(rings[0], Vector((0, 0, 0))) / len(rings[0])
            w.loft("wing", rings, (root_c.x - side * 0.01, root_c.y, root_c.z),
                   (tip_x, -0.080 + 0.20 + 0.03, 0.04))

            def wing_rule(shell, station, c, n, a):
                # The outer bands (s from 0.68) are the dark tip, top and bottom. ⚠️ v2 proved
                # that from below, lit only by the sky, a white underwing takes the sky's own peach
                # and VANISHES against it: only the tips read. So the underwing also carries a
                # mid-grey TRAILING-EDGE band (the two ring segments nearest the trailing edge,
                # about the back third of the chord), the dark rear edge real terns show, which
                # draws the wing's shape as a bold graphic line against the sunset.
                if station >= 5:
                    return "fauna_tip"
                if math.sin(a) > 0:
                    return "fauna_grey"
                return "fauna_shade" if math.cos(a) < -0.5 else "fauna_white"

            j = Vector((side * SHOULDER.x, SHOULDER.y, SHOULDER.z))
            # The wing rings were drawn relative to the shoulder; shift them into model space.
            bmesh.ops.translate(w.bm, vec=j, verts=w.bm.verts)
            wings.append((name, j, w, wing_rule))

        tail = Part()
        # A tern's long FORK, flat and horizontal, drawn from its root (inside the body's tail
        # end) back to two fat round tips. Its origin is the root so the code can tilt it.
        tj = Vector((0, 0.175, 0.020))
        fork = [(0.0, -0.035), (0.045, -0.025), (0.070, 0.050), (0.115, 0.215), (0.080, 0.215),
                (0.030, 0.110), (0.0, 0.095), (-0.030, 0.110), (-0.080, 0.215), (-0.115, 0.215),
                (-0.070, 0.050), (-0.045, -0.025)]
        tail.pillow("tail", tj, (1, 0, 0), (0, 1, 0), fork, 0.030, k=16, inset=0.70)

        def tail_rule(shell, station, c, n, a):
            # Pillow stations: 1 and 2 are the upper band and cap, the grey top of the tail.
            if c.y > tj.y + 0.150:
                return "fauna_tip"
            return "fauna_grey" if station in (1, 2) else "fauna_white"

        return assemble("fauna_seabird",
                        [("body", Vector((0, 0, 0)), body, body_rule)] + wings
                        + [("tail", tj, tail, tail_rule)])

    # ============================================================ the pigeon
    # OWNER, 2026-09-27, for the Kanto city map: pigeons on the court, and "no i wanna use a newer
    # model for pigeons" (not the older ambient-life kalapati.glb). A chunky city rock pigeon in
    # the tern's cast: the same lofted shells, the same wing construction and pivots, but PLUMP
    # and ROUND where the tern is long and swept, half its span (0.66 m to 1.30 m), with a big
    # puffed chest, a round dark head and short broad wings. It is seen mostly on the GROUND at 10
    # to 25 m, standing and pecking with its wings folded, so it also has chunky pink feet, a
    # separate "head" child to bob and peck with, and colour marks big enough to read at 25 m:
    # the dark head and chest, the green and purple neck band, two dark wing bars, the dark tail
    # band.
    #
    # Children of "fauna_pigeon" and their pivots (model space, before the centre-of-mass shift):
    #   body    the torso, neck band, legs and feet (feet never move on their own)
    #   head    origin at the NECK joint (0, -0.090, 0.060): pitch it about local X to peck
    #           (positive Blender X rotation puts the beak down) and bob it along Y
    #   wing_l  origin at the left SHOULDER (+0.040, -0.064, 0.072), spread flat along +X
    #   wing_r  origin at the right SHOULDER (-0.040, -0.064, 0.072), spread along -X
    #   tail    origin at the tail root (0, 0.110, 0.030), a fan the code can tilt or spread

    # ⚠️ The shoulder sits well IN from the flank, forward and high (v2). In v1 it was at the
    # flank (x 0.058, y -0.040): folded, the wing's leading edge stood 2 cm proud of the body
    # and the tip ran 4 cm past the tail. In flight the extra root is simply buried in the body.
    PIGEON_SHOULDER = Vector((0.040, -0.064, 0.072))
    PIGEON_SPAN_HALF = 0.290               # span = 2 * (0.040 + 0.290) = 0.66 m
    PIGEON_NECK = Vector((0, -0.090, 0.060))
    PIGEON_TAIL = Vector((0, 0.110, 0.030))

    def build_pigeon():
        body = Part()
        # (y, z centre, half width, half height), neck to tail. The front is raised into the neck
        # and swells straight into a puffed chest, the widest point just behind it: the pigeon's
        # whole character is that round front.
        st = [(-0.128, 0.064, 0.034, 0.036), (-0.114, 0.050, 0.058, 0.066),
              (-0.090, 0.032, 0.076, 0.088), (-0.050, 0.016, 0.090, 0.094),
              (0.000, 0.012, 0.092, 0.086), (0.045, 0.016, 0.080, 0.072),
              (0.082, 0.022, 0.058, 0.050), (0.110, 0.028, 0.036, 0.030),
              (0.128, 0.030, 0.022, 0.018)]
        rings = [superellipse((0, y, z), (1, 0, 0), (0, 0, 1), a, b, 14, 2.3)
                 for y, z, a, b in st]
        body.loft("body", rings, (0, -0.138, 0.068), (0, 0.138, 0.030))
        for sx in (1, -1):
            # A short fat leg from inside the belly, then a round three-lobed MITTEN of a foot:
            # toes as lobes of one soft pad, never three thin sticks.
            # ⚠️ Short and stubby: v1's legs (5.7 cm of visible shin) read as stilts at 12 m.
            body.cone("leg", (sx * 0.032, 0.012, -0.055), (sx * 0.034, 0.004, -0.094), 0.015,
                      0.012, 6, 1.0)
            toes = [(0.000, 0.022), (0.022, 0.004), (0.034, -0.036), (0.014, -0.030),
                    (0.000, -0.056), (-0.014, -0.030), (-0.034, -0.036), (-0.022, 0.004)]
            body.pillow("foot", (sx * 0.035, 0.000, -0.098), (1, 0, 0), (0, 1, 0), toes, 0.016,
                        k=10, inset=0.7)

        def body_rule(shell, station, c, n, a):
            if shell in ("leg", "foot"):
                return "fauna_pigeon_feet"
            up = math.sin(a)
            # Stations -1 to 1 are the neck and upper chest. The iridescent patch is one chunky
            # band: green over the nape and neck top, purple down the neck sides, and the dark
            # chest below it, the order a rock pigeon wears them.
            if station <= 1:
                if up > 0.10:
                    return "fauna_pigeon_neck_green"
                if up > -0.35:
                    return "fauna_pigeon_neck_purple"
                return "fauna_pigeon_dark"
            if station == 2 and up < -0.35:
                return "fauna_pigeon_dark"      # the dark lower chest runs back one band
            return "fauna_pigeon_grey"

        head = Part()
        hc = Vector((0, -0.110, 0.122))
        # A round head, a little wider than tall, big for the body on purpose (cuteness), set on
        # top of the neck so the neck band shows below it.
        head.sphere("head", hc, 0.048, 10, 6, (1.0, 1.08, 0.96))
        # A short dark beak, blunt, dipping a little, with the pale CERE as a soft lump on top of
        # its base: the one pale mark on the face.
        head.cone("beak", hc + Vector((0, -0.040, -0.006)), hc + Vector((0, -0.080, -0.016)),
                  0.014, 0.004, 8, 0.85)
        head.sphere("cere", hc + Vector((0, -0.050, 0.004)), 0.011, 6, 3, (1.0, 1.4, 0.7))
        for sx in (1, -1):
            # Pink-red eye ring as a bigger disc behind a dark eye: the ring shows as a rim.
            head.sphere("eyering", hc + Vector((sx * 0.034, -0.018, 0.012)), 0.015, 8, 4,
                        (0.6, 1, 1))
            head.sphere("eye", hc + Vector((sx * 0.042, -0.019, 0.012)), 0.0095, 6, 4,
                        (0.6, 1, 1))

        def head_rule(shell, station, c, n, a):
            return {"head": "fauna_pigeon_dark", "beak": "fauna_cap", "cere": "fauna_white",
                    "eyering": "fauna_pigeon_eyering", "eye": "fauna_eye"}[shell]

        wings = []
        for side, name in ((1, "wing_l"), (-1, "wing_r")):
            w = Part()
            # The tern's wing, cut short and broad: a wide chord, a gentle sweep, a round-pointed
            # tip. The ring samples the chord EVENLY on top (seven points, six equal segments),
            # so the two dark WING BARS can be exactly two top segments (a third to a half, and
            # two thirds to five sixths of the chord) on the inner wing, each about 3 cm wide.
            ss = [0.0, 0.18, 0.36, 0.54, 0.70, 0.83, 0.94]
            rings = []
            for s in ss:
                # The root starts only 5 mm inboard of the shoulder: the shoulder is already
                # 5 cm inside the flank, and v3 showed a longer buried root swinging forward
                # out of the body as a jagged stub by the neck when the wing folds.
                x = -0.005 + s * (PIGEON_SPAN_HALF + 0.005)
                chord = 0.180 * (1 - 0.40 * s) * math.sqrt(max(0.0, 1 - s ** 4)) + 0.010
                y_le = -0.050 + 0.080 * s ** 1.6
                thick = 0.042 * (1 - 0.55 * s)
                zc = 0.020 * s
                top, bot = [], []
                for k in range(7):
                    u = k / 6
                    h = thick / 2 * math.sqrt(max(0.0, 4 * u * (1 - u))) * (1 - 0.45 * u)
                    camber = 0.012 * math.sin(math.pi * u) * (1 - s)
                    top.append(Vector((side * x, y_le + chord * u, zc + camber + h)))
                    bot.append(Vector((side * x, y_le + chord * u, zc + camber - h * 0.6)))
                rings.append(top + list(reversed(bot[1:6])))
            root_c = sum(rings[0], Vector((0, 0, 0))) / len(rings[0])
            w.loft("wing", rings, (root_c.x - side * 0.008, root_c.y, root_c.z),
                   (side * PIGEON_SPAN_HALF, -0.050 + 0.080 + 0.02, 0.020))

            def wing_rule(shell, station, c, n, a):
                seg = int(a * 12 / (2 * math.pi))       # 0 to 5 top (front to back), 6 to 11 under
                if station >= 4:
                    return "fauna_pigeon_dark"          # the dark primaries, s from 0.70
                # The two bars cover only the MIDDLE of the inner wing (stations 1 and 2, about
                # 11 cm of span): folded, the rigid wing turns spanwise stripes lengthwise, and
                # v2's full-length stripes read as racing stripes; short dashes read as bars.
                if seg < 6 and station in (1, 2) and seg in (2, 4):
                    return "fauna_tip"                  # the two wing bars
                return "fauna_pigeon_pale"

            j = Vector((side * PIGEON_SHOULDER.x, PIGEON_SHOULDER.y, PIGEON_SHOULDER.z))
            bmesh.ops.translate(w.bm, vec=j, verts=w.bm.verts)
            wings.append((name, j, w, wing_rule))

        tail = Part()
        # A short rounded-square FAN from inside the rump, grey with the dark terminal band.
        fan = [(0.000, -0.030), (0.034, -0.020), (0.050, 0.055), (0.052, 0.104), (0.000, 0.112),
               (-0.052, 0.104), (-0.050, 0.055), (-0.034, -0.020)]
        tail.pillow("tail", PIGEON_TAIL, (1, 0, 0), (0, 1, 0), fan, 0.024, k=14, inset=0.72)

        def tail_rule(shell, station, c, n, a):
            return "fauna_tip" if c.y > PIGEON_TAIL.y + 0.072 else "fauna_pigeon_grey"

        return assemble("fauna_pigeon",
                        [("body", Vector((0, 0, 0)), body, body_rule),
                         ("head", PIGEON_NECK, head, head_rule)] + wings
                        + [("tail", PIGEON_TAIL, tail, tail_rule)])

    def fold_wings(root, sweep=84.0, droop=10.0, sweep_first=False):
        """Pose the wings as the runtime folds them on the ground, in BLENDER terms. Blender's
        +Z swing of the left wing is Unity's -Y swing (the handedness flip), so the runtime's
        "left -84, right +84 about up" is (left +84, right -84) here; the droop is a roll about
        the forward axis that lowers the left wing's outer edge (Blender +Y for the left wing,
        which is a roll about Unity +Z of the same sign).

        sweep_first=False: droop about the wing's OWN forward axis first, then swing (Blender
        euler XYZ). The droop then tips the folded wing's rear end down.
        sweep_first=True: swing first, then roll about the BODY's forward axis (euler ZYX, in
        Unity localRotation = AngleAxis(droop, forward) * AngleAxis(sweep, up)). The roll then
        tilts each folded wing's outer edge down over the flank, the two wings meeting in a low
        ridge along the back, which is how a pigeon's folded wings sit."""
        for ch in root.children:
            side = 1 if ch.name.startswith("wing_l") else -1 if ch.name.startswith("wing_r") else 0
            if side:
                ch.rotation_mode = "ZYX" if sweep_first else "XYZ"
                ch.rotation_euler = (0, side * math.radians(droop), side * math.radians(sweep))

    # ============================================================ the fish
    # Each fish is a lofted body (stations nose to tail), fins as pillows merged into the body,
    # two big dark eyes, and a separate tail whose origin is the tail joint. Body stations are
    # (y, z centre, half width, half height).

    def _surface_x(stations, y, z, p=2.6):
        """Half width of the body at (y, z): where to set an eye so it sits ON the skin."""
        for (y0, z0, a0, b0), (y1, z1, a1, b1) in zip(stations, stations[1:]):
            if y0 <= y <= y1:
                t = (y - y0) / (y1 - y0)
                zc, a, b = z0 + (z1 - z0) * t, a0 + (a1 - a0) * t, b0 + (b1 - b0) * t
                q = min(0.999, abs(z - zc) / b)
                return a * (1 - q ** p) ** (1 / p)
        return 0.0

    def build_fish(model, spec):
        body = Part()
        st = spec["stations"]
        # 16 segments round the body: 14 showed a polygonal outline on the disc-shaped tang.
        rings = [superellipse((0, y, z), (1, 0, 0), (0, 0, 1), a, b, 16) for y, z, a, b in st]
        body.loft("body", rings, spec["nose"], spec["rear"])
        for shell, outline, t in spec["median_fins"]:
            # Median fins (dorsal, anal) stand in the body's mid plane: u = back (+Y), v = up.
            body.pillow(shell, (0, 0, 0), (0, 1, 0), (0, 0, 1), outline, t, k=12)
        py, pz, psize = spec["pectoral"]
        for sx in (1, -1):
            # A small paddle on each flank behind the eye, swept back and a little out.
            x0 = _surface_x(st, py, pz) - 0.006
            paddle = [(0, -0.3 * psize), (0.9 * psize, -0.45 * psize), (1.2 * psize, 0),
                      (0.9 * psize, 0.35 * psize), (0, 0.25 * psize)]
            body.pillow("pectoral", (sx * x0, py, pz), (sx * 0.45, 1, -0.2), (0, 0, 1), paddle,
                        0.016, k=10, inset=0.7)
        ey, ez, er = spec["eye"]
        for sx in (1, -1):
            # Big dark cartoon eyes pushed half out of the skin, the house's ink-dot face.
            ex = _surface_x(st, ey, ez) - er * 0.45
            body.sphere("eye", (sx * ex, ey, ez), er, 8, 5, (0.8, 1, 1))

        tail = Part()
        tj = Vector(spec["tail_joint"])
        tail.pillow("tail", tj, (0, 1, 0), (0, 0, 1), spec["tail"], spec["tail_t"], k=16,
                    inset=0.70)
        return assemble(model, [("body", Vector((0, 0, 0)), body, spec["body_rule"]),
                                ("tail", tj, tail, spec["tail_rule"])])

    def _fins(rule_map, default):
        return lambda shell, station, c, n, a: rule_map.get(shell, default)(c, n, a)

    def _const(name):
        return lambda c, n, a: name

    FISH = {
        # A: yellow tang. A tall round DISC, the dorsal and anal fins running most of its length
        # so the whole thing reads as one bright coin, and a small crescent tail.
        "fauna_fish_a": dict(
            stations=[(-0.180, -0.010, 0.022, 0.026), (-0.150, 0.000, 0.040, 0.062),
                      (-0.110, 0.010, 0.052, 0.100), (-0.060, 0.014, 0.058, 0.124),
                      (0.000, 0.014, 0.058, 0.128), (0.050, 0.012, 0.050, 0.112),
                      (0.090, 0.010, 0.038, 0.078), (0.120, 0.008, 0.026, 0.044),
                      (0.140, 0.008, 0.019, 0.028)],
            nose=(0, -0.192, -0.012), rear=(0, 0.150, 0.008),
            median_fins=[
                ("dorsal", [(-0.100, 0.100), (-0.060, 0.170), (0.020, 0.200), (0.085, 0.160),
                            (0.125, 0.060), (0.070, 0.070), (-0.020, 0.100)], 0.026),
                ("anal", [(-0.030, -0.090), (0.000, -0.175), (0.070, -0.160), (0.120, -0.055),
                          (0.070, -0.070)], 0.026)],
            pectoral=(-0.050, -0.030, 0.050),
            eye=(-0.112, 0.040, 0.026),
            tail_joint=(0, 0.132, 0.008),
            tail=[(-0.015, -0.022), (0.030, -0.040), (0.095, -0.100), (0.110, -0.070),
                  (0.080, 0.000), (0.110, 0.070), (0.095, 0.100), (0.030, 0.040),
                  (-0.015, 0.022)],
            tail_t=0.026,
            body_rule=_fins({"eye": _const("fauna_eye"), "body": _const("fauna_yellow")},
                            _const("fauna_yellow_deep")),
            tail_rule=lambda s, st, c, n, a: "fauna_yellow_deep",
        ),
        # B: pink-and-cream bands, a deep oval with a tall swept SAIL of a dorsal fin (a cartoon
        # bannerfish without the fiddly filament). Five wide bands, each at least 5 cm, so the
        # stripes still read as stripes through the water.
        "fauna_fish_b": dict(
            stations=[(-0.190, -0.005, 0.022, 0.024), (-0.160, 0.000, 0.044, 0.052),
                      (-0.120, 0.008, 0.060, 0.086), (-0.060, 0.012, 0.070, 0.110),
                      (0.000, 0.012, 0.072, 0.114), (0.060, 0.010, 0.064, 0.098),
                      (0.110, 0.008, 0.046, 0.066), (0.150, 0.006, 0.028, 0.034),
                      (0.170, 0.006, 0.020, 0.024)],
            nose=(0, -0.202, -0.006), rear=(0, 0.180, 0.006),
            median_fins=[
                ("dorsal", [(-0.090, 0.085), (-0.050, 0.170), (0.020, 0.250), (0.075, 0.255),
                            (0.105, 0.190), (0.135, 0.060), (0.040, 0.085)], 0.034),
                ("anal", [(0.000, -0.090), (0.030, -0.160), (0.090, -0.150), (0.130, -0.050),
                          (0.080, -0.060)], 0.026)],
            pectoral=(-0.060, -0.030, 0.052),
            eye=(-0.130, 0.035, 0.026),
            tail_joint=(0, 0.160, 0.006),
            tail=[(-0.015, -0.022), (0.040, -0.050), (0.100, -0.072), (0.125, 0.000),
                  (0.100, 0.072), (0.040, 0.050), (-0.015, 0.022)],
            tail_t=0.024,
            body_rule=_fins({
                "eye": _const("fauna_eye"),
                "body": lambda c, n, a: ("fauna_pink" if (c.y < -0.095 or -0.040 < c.y < 0.030
                                                       or c.y > 0.095) else "fauna_cream"),
            }, _const("fauna_pink")),
            tail_rule=lambda s, st, c, n, a: "fauna_pink",
        ),
        # C: sea-green parrotfish. The fattest, longest body, a blunt rounded head with a cream
        # snout, a lime belly under a sea-green back, and a broad square-ended tail.
        "fauna_fish_c": dict(
            stations=[(-0.245, -0.005, 0.030, 0.030), (-0.225, 0.000, 0.056, 0.056),
                      (-0.190, 0.005, 0.073, 0.076), (-0.130, 0.010, 0.083, 0.088),
                      (-0.050, 0.012, 0.086, 0.090), (0.040, 0.012, 0.078, 0.082),
                      (0.110, 0.010, 0.058, 0.060), (0.160, 0.008, 0.036, 0.036),
                      (0.185, 0.008, 0.024, 0.024)],
            nose=(0, -0.255, -0.006), rear=(0, 0.196, 0.008),
            median_fins=[
                ("dorsal", [(-0.140, 0.075), (-0.100, 0.125), (0.020, 0.135), (0.130, 0.090),
                            (0.150, 0.040), (0.050, 0.070)], 0.026),
                ("anal", [(0.020, -0.070), (0.050, -0.115), (0.130, -0.090), (0.150, -0.040),
                          (0.080, -0.050)], 0.024)],
            pectoral=(-0.140, -0.030, 0.060),
            eye=(-0.180, 0.040, 0.027),
            # ⚠️ The tail is 12 per cent shorter than first drawn: v4 measured the parrotfish at
            # 0.563 m, over the brief's 0.55 m ceiling for the school.
            tail_joint=(0, 0.166, 0.008),
            tail=[(-0.015, -0.024), (0.044, -0.060), (0.106, -0.095), (0.123, -0.060),
                  (0.110, 0.000), (0.123, 0.060), (0.106, 0.095), (0.044, 0.060),
                  (-0.015, 0.024)],
            tail_t=0.028,
            body_rule=_fins({
                "eye": _const("fauna_eye"),
                "body": lambda c, n, a: ("fauna_cream" if c.y < -0.232 else
                                         "fauna_lime" if math.sin(a) < -0.3 else
                                         "fauna_seagreen"),
                "dorsal": _const("fauna_seagreen"),
            }, _const("fauna_lime")),
            tail_rule=lambda s, st, c, n, a: "fauna_lime",
        ),
        # D: silver jack. The slimmest body, a dark silver back over pale silver flanks, and a
        # deep FORKED yellow tail: the one fish whose silhouette is all about the tail.
        "fauna_fish_d": dict(
            stations=[(-0.210, 0.000, 0.020, 0.026), (-0.180, 0.004, 0.036, 0.055),
                      (-0.130, 0.008, 0.048, 0.080), (-0.060, 0.010, 0.054, 0.092),
                      (0.020, 0.010, 0.050, 0.085), (0.090, 0.008, 0.038, 0.060),
                      (0.140, 0.006, 0.022, 0.032), (0.165, 0.006, 0.015, 0.018)],
            nose=(0, -0.222, 0.000), rear=(0, 0.176, 0.006),
            median_fins=[
                ("dorsal", [(-0.070, 0.080), (-0.030, 0.150), (0.010, 0.150), (0.060, 0.085),
                            (0.000, 0.090)], 0.024),
                ("anal", [(0.000, -0.075), (0.030, -0.125), (0.070, -0.120), (0.100, -0.050),
                          (0.050, -0.060)], 0.022)],
            pectoral=(-0.110, -0.025, 0.048),
            eye=(-0.150, 0.030, 0.025),
            tail_joint=(0, 0.150, 0.006),
            tail=[(-0.015, -0.016), (0.050, -0.055), (0.140, -0.130), (0.150, -0.095),
                  (0.075, 0.000), (0.150, 0.095), (0.140, 0.130), (0.050, 0.055),
                  (-0.015, 0.016)],
            tail_t=0.024,
            body_rule=_fins({
                "eye": _const("fauna_eye"),
                "body": lambda c, n, a: ("fauna_silver_dark" if math.sin(a) > 0.45
                                         else "fauna_silver"),
                "dorsal": _const("fauna_silver_dark"),
                "anal": _const("fauna_yellow_deep"),
            }, _const("fauna_silver")),
            tail_rule=lambda s, st, c, n, a: "fauna_yellow_deep",
        ),
    }
    MODELS = ["fauna_seabird", "fauna_pigeon"] + list(FISH)

    def build(model):
        if model == "fauna_seabird":
            return build_seabird()
        if model == "fauna_pigeon":
            return build_pigeon()
        return build_fish(model, FISH[model])

    def check(root):
        """Size (x, y, z extent in metres, in the root's frame) and triangle count."""
        tris, lo, hi = 0, Vector((1e9,) * 3), Vector((-1e9,) * 3)
        for ob in root.children:
            me = ob.data
            tris += sum(len(p.vertices) - 2 for p in me.polygons)
            for v in me.vertices:
                # ⚠️ ob.location, not ob.matrix_local: the matrix is stale until the depsgraph
                # updates, and v3 measured the bird 13 cm short because of it. Children carry
                # no rotation or scale, so location is the whole transform.
                w = ob.location + v.co
                lo = Vector(map(min, lo, w))
                hi = Vector(map(max, hi, w))
        size = hi - lo
        return {"tris": tris, "size": tuple(round(c, 3) for c in size), "lo": lo, "hi": hi,
                "children": sorted(o.name for o in root.children)}

    def _budget(model):
        return TRI_BUDGET.get(model, FISH_TRI_BUDGET)

    def reset():
        bpy.ops.wm.read_factory_settings(use_empty=True)

    # ============================================================ export
    def export_all():
        OUT_DIR.mkdir(parents=True, exist_ok=True)
        for model in MODELS:
            # ⚠️ One model per EMPTY scene: every fish has a child called "body", and Blender
            # would rename the second one "body.001" in a shared scene, which the runtime would
            # then fail to find by name.
            reset()
            root = build(model)
            rep = check(root)
            print(f"[fauna] {model} size {rep['size']} tris {rep['tris']} {rep['children']}")
            if rep["tris"] > _budget(model):
                raise SystemExit(f"[fauna] {model} is {rep['tris']} tris, over {_budget(model)}")
            if any("." in n for n in rep["children"]):
                raise SystemExit(f"[fauna] {model} child names were suffixed: {rep['children']}")
            bpy.ops.object.select_all(action="DESELECT")
            root.select_set(True)
            for ch in root.children:
                ch.select_set(True)
            bpy.context.view_layer.objects.active = root
            path = OUT_DIR / f"{model}.glb"
            bpy.ops.export_scene.gltf(filepath=str(path), export_format="GLB", use_selection=True,
                                      export_yup=True, export_apply=True,
                                      export_animations=False, export_cameras=False,
                                      export_lights=False, export_extras=False)
            print("[fauna] wrote", path)

    # ============================================================ preview
    def _plain(name, rgb):
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        b = m.node_tree.nodes.get("Principled BSDF")
        b.inputs["Base Color"].default_value = (*rgb, 1)
        b.inputs["Roughness"].default_value = 0.8
        return m

    def _stage(sky, sun_dir, sun_energy, view="AgX"):
        scene = bpy.context.scene
        world = bpy.data.worlds.new("world")
        world.use_nodes = True
        bg = world.node_tree.nodes["Background"]
        bg.inputs["Color"].default_value = (*sky, 1)
        bg.inputs["Strength"].default_value = 1.0
        scene.world = world
        sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
        sun.data.energy, sun.data.color = sun_energy, (1.0, 0.86, 0.70)
        sun.data.angle = math.radians(3)
        sun.rotation_euler = sun_dir
        scene.collection.objects.link(sun)
        cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
        cam.data.clip_end = 500
        scene.collection.objects.link(cam)
        scene.camera = cam
        scene.render.engine = "BLENDER_EEVEE"
        scene.render.resolution_x, scene.render.resolution_y = 1600, 900
        scene.view_settings.view_transform = view
        for look in (("AgX - Punchy", "Punchy") if view == "AgX" else ()):
            try:
                scene.view_settings.look = look
                break
            except TypeError:
                continue
        return cam

    def _shoot(cam, pos, tgt, path, lens=None, fov=None):
        if path.exists():
            raise SystemExit(f"[fauna] {path} exists: never overwrite a render, bump --preview")
        cam.location = pos
        if fov:
            cam.data.sensor_fit = "HORIZONTAL"
            cam.data.angle = math.radians(fov)
        else:
            cam.data.lens = lens
        cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        bpy.context.scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("[fauna] preview", path)

    # The cove's sunset sky behind the birds, sRGB f8c39a (a warm peach) as linear light. v1 used
    # AgX Punchy here and the sky came out a muddy mauve, which hid how the bird actually reads.
    PEACH_SKY = (0.94, 0.55, 0.33)

    def preview(version):
        LOG_DIR.mkdir(parents=True, exist_ok=True)
        # 1. The lineup: every model floating 0.3 m over pale sand (its shadow shows the plan
        # shape), bird first, fish after it, the 1.6 m scale cylinder at the far end. Each model
        # is YAWED 55 degrees for the picture only (nose toward the camera's right), so one shot
        # from the front shows every face and flank in three-quarter view.
        reset()
        cam = _stage((0.55, 0.60, 0.62), (math.radians(50), 0, math.radians(-140)), 4.0)
        scene = bpy.context.scene
        g = bpy.data.meshes.new("sand")
        bm = bmesh.new()
        bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=100)
        bm.to_mesh(g)
        bm.free()
        g.materials.append(_plain("sand_pale", (0.78, 0.66, 0.46)))
        scene.collection.objects.link(bpy.data.objects.new("sand", g))
        x, centres = 0.0, {}
        for model in MODELS:
            root = build(model)
            rep = check(root)
            reach = max(rep["size"][0], rep["size"][1])
            x -= reach * 0.5
            root.location = (x, 0.0, 0.30 - rep["lo"].z)
            root.rotation_euler = (0, 0, math.radians(-55))
            centres[model] = x
            print(f"[fauna] {model} size {rep['size']} tris {rep['tris']}")
            x -= reach * 0.5 + 0.22
        ref = bpy.data.meshes.new("scale_ref_1m60")
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=0.12, radius2=0.12,
                              depth=1.6)
        bmesh.ops.translate(bm, vec=(0, 0, 0.8), verts=bm.verts)
        bm.to_mesh(ref)
        bm.free()
        ref.materials.append(_plain("scale_pink", (0.95, 0.30, 0.55)))
        o = bpy.data.objects.new("scale_ref_1m60", ref)
        o.location = (x - 0.1, 0.35, 0)
        scene.collection.objects.link(o)
        mid = x / 2
        _shoot(cam, (mid + 0.4, -4.4, 1.7), (mid, 0, 0.42),
               LOG_DIR / f"fauna_lineup_v{version}.png", lens=40)
        # Close-up of the four fish alone, so faces, fins and band edges can be judged.
        fmid = (centres["fauna_fish_a"] + centres["fauna_fish_d"]) / 2
        _shoot(cam, (fmid + 0.15, -2.3, 0.85), (fmid, 0, 0.45),
               LOG_DIR / f"fauna_fish_close_v{version}.png", lens=30)

        # 2. The bird from straight below against a peach sunset sky: the underside design.
        reset()
        cam = _stage(PEACH_SKY, (math.radians(78), 0, math.radians(-120)), 3.5, "Standard")
        bird = build("fauna_seabird")
        bird.location = (0, 0, 3.0)
        bird.rotation_euler = (0, math.radians(-8), math.radians(20))
        _shoot(cam, (0.0, 0.0, 0.0), (0.0, 0.0001, 3.0), LOG_DIR / f"fauna_below_v{version}.png",
               lens=45)

        # 3. The game's view: a 1.25 m eye, 95 degree field of view, looking up 30 degrees, birds
        # at 8, 20, 40 and 60 m, banked and headed differently as a boids flock would be.
        reset()
        cam = _stage(PEACH_SKY, (math.radians(80), 0, math.radians(-100)), 3.5, "Standard")
        eye = Vector((0, 0, 1.25))
        pitch = math.radians(30)
        fwd = Vector((0, math.cos(pitch), math.sin(pitch)))
        for i, (dist, az, el, head, bank) in enumerate(
                [(8, -18, 6, 70, -18), (20, 16, -4, -40, 12), (40, -4, 12, 160, 8),
                 (60, 30, 4, 100, -10), (60, -34, -2, 20, 15)]):
            d = Matrix.Rotation(math.radians(az), 3, "Z") @ Matrix.Rotation(
                math.radians(el), 3, "X") @ fwd
            b = build("fauna_seabird")
            b.name = f"fauna_seabird_{i}"
            b.location = eye + d.normalized() * dist
            b.rotation_euler = (0, math.radians(bank), math.radians(head))
        _shoot(cam, eye, eye + fwd, LOG_DIR / f"fauna_sky_v{version}.png", fov=95)

    def _ground(name, rgb, size=100):
        g = bpy.data.meshes.new(name)
        bm = bmesh.new()
        bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=size)
        bm.to_mesh(g)
        bm.free()
        g.materials.append(_plain(name, rgb))
        bpy.context.scene.collection.objects.link(bpy.data.objects.new(name, g))

    def _scale_ref(loc):
        ref = bpy.data.meshes.new("scale_ref_1m60")
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=0.12, radius2=0.12,
                              depth=1.6)
        bmesh.ops.translate(bm, vec=(0, 0, 0.8), verts=bm.verts)
        bm.to_mesh(ref)
        bm.free()
        ref.materials.append(_plain("scale_pink", (0.95, 0.30, 0.55)))
        o = bpy.data.objects.new("scale_ref_1m60", ref)
        o.location = loc
        bpy.context.scene.collection.objects.link(o)

    def _standing(loc, yaw, peck=0.0, sweep=84.0, droop=10.0, sweep_first=False):
        """A pigeon on the ground: wings folded as the runtime folds them, feet on z = 0, the
        head pitched down by peck degrees."""
        root = build("fauna_pigeon")
        rep = check(root)
        fold_wings(root, sweep, droop, sweep_first)
        if peck:
            next(c for c in root.children if c.name.startswith("head")).rotation_euler = (
                math.radians(peck), 0, 0)
        root.location = (loc[0], loc[1], -rep["lo"].z)
        root.rotation_euler = (0, 0, math.radians(yaw))
        return root

    # Kanto is a daytime city court: a pale warm-grey paving and a soft daylight sky behind the
    # pigeon shots (a preview stand-in, never a UI colour).
    PAVING = (0.62, 0.58, 0.52)
    # The recommended fold (sweep, droop, sweep_first), chosen from pigeon_fold_v2 onward.
    FOLD_BEST = (84.0, 22.0, True)
    DAY_SKY = (0.62, 0.66, 0.70)

    def preview_pigeon(version, sweep=84.0, droop=10.0):
        LOG_DIR.mkdir(parents=True, exist_ok=True)
        # 1. Beside the tern and the 1.6 m cylinder: the pigeon spread (flying) and standing.
        reset()
        cam = _stage((0.55, 0.60, 0.62), (math.radians(50), 0, math.radians(-140)), 4.0)
        _ground("sand_pale", (0.78, 0.66, 0.46))
        tern = build("fauna_seabird")
        rep = check(tern)
        tern.location = (0.0, 0.0, 0.30 - rep["lo"].z)
        tern.rotation_euler = (0, 0, math.radians(-55))
        pig = build("fauna_pigeon")
        rep = check(pig)
        print(f"[fauna] fauna_pigeon size {rep['size']} tris {rep['tris']} {rep['children']}")
        pig.location = (-0.95, 0.0, 0.30 - rep["lo"].z)
        pig.rotation_euler = (0, 0, math.radians(-55))
        _standing((-1.6, 0.0), -55, 0.0, *FOLD_BEST)
        _scale_ref((-2.2, 0.35, 0))
        _shoot(cam, (-0.9, -3.4, 1.3), (-1.0, 0, 0.35),
               LOG_DIR / f"pigeon_lineup_v{version}.png", lens=40)
        # 2. Close three-quarter view: spread and standing side by side.
        _shoot(cam, (-1.15, -1.75, 0.62), (-1.28, 0, 0.22),
               LOG_DIR / f"pigeon_close_v{version}.png", lens=45)

        # 3. The fold: three standing pigeons (profile, three-quarter, from behind), seen from 35
        # degrees up, to judge whether the folded wings lie along the body.
        reset()
        cam = _stage((0.55, 0.60, 0.62), (math.radians(45), 0, math.radians(-150)), 4.0)
        _ground("paving", PAVING)
        # Back row: the pose as the runtime states it (sweep 84, droop 10, droop applied first).
        # Front row: FOLD_BEST, the recommended pose. Same three yaws in each row.
        for i, yaw in enumerate((90, 35, 180)):
            _standing((0.45 - 0.45 * i, 0.35), yaw, 0.0, sweep, droop, False)
            _standing((0.45 - 0.45 * i, -0.15), yaw, 0.0, *FOLD_BEST)
        _shoot(cam, (0.0, -1.75, 1.15), (0.0, 0.1, 0.08),
               LOG_DIR / f"pigeon_fold_v{version}.png", lens=42)

        # 4. THE MAIN VIEW: pigeons landed on the court, from 12 m at a 1.4 m eye, the game's 95
        # degree field of view; then the same spot through a long lens to see what is there.
        reset()
        cam = _stage(DAY_SKY, (math.radians(50), 0, math.radians(-130)), 4.0)
        _ground("paving", PAVING, 300)
        for (x, y, yaw, peck) in [(0.0, 0.0, 30, 0), (0.5, 0.35, 150, 40), (-0.45, 0.25, 250, 0),
                                  (0.3, -0.4, 300, 35), (-0.2, 0.75, 100, 0)]:
            _standing((x, y), yaw, peck, *FOLD_BEST)
        eye = Vector((0.0, -12.0, 1.4))
        _shoot(cam, eye, (0.0, 0.0, 0.1), LOG_DIR / f"pigeon_court_v{version}.png", fov=95)
        _shoot(cam, eye, (0.0, 0.0, 0.12), LOG_DIR / f"pigeon_court_zoom_v{version}.png",
               lens=260)

        # 5. A small flock overhead, 5 to 22 m, from a 1.4 m eye looking up 30 degrees at 95
        # degrees, a couple mid-flap (wings rolled up or down about the forward axis).
        reset()
        cam = _stage(DAY_SKY, (math.radians(70), 0, math.radians(-110)), 4.0)
        eye = Vector((0, 0, 1.4))
        pitch = math.radians(30)
        fwd = Vector((0, math.cos(pitch), math.sin(pitch)))
        for i, (dist, az, el, head, bank, flap) in enumerate(
                [(5, -14, 4, 60, -10, 0), (9, 10, -3, 40, 5, 30), (12, -4, 9, 50, 0, -25),
                 (16, 20, 5, 30, 10, 0), (22, -22, 0, 70, -5, 20), (22, 2, 14, 45, 0, 0)]):
            d = Matrix.Rotation(math.radians(az), 3, "Z") @ Matrix.Rotation(
                math.radians(el), 3, "X") @ fwd
            b = build("fauna_pigeon")
            for ch in b.children:
                if ch.name.startswith("wing_l"):
                    ch.rotation_euler = (0, -math.radians(flap), 0)
                elif ch.name.startswith("wing_r"):
                    ch.rotation_euler = (0, math.radians(flap), 0)
            b.location = eye + d.normalized() * dist
            b.rotation_euler = (0, math.radians(bank), math.radians(head))
        _shoot(cam, eye, eye + fwd, LOG_DIR / f"pigeon_flock_v{version}.png", fov=95)

    def main():
        argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
        if "--export" in argv:
            export_all()
        if "--preview" in argv:
            version = int(argv[argv.index("--preview") + 1])
            only = argv[argv.index("--only") + 1] if "--only" in argv else None
            if only != "pigeon":
                preview(version)
            if only in (None, "pigeon"):
                preview_pigeon(version)
        if not argv:
            for row in audit_palette():
                print("[fauna]", row)

    if __name__ == "__main__":
        main()
else:
    if __name__ == "__main__":
        for row in audit_palette():
            print("[fauna]", row)
