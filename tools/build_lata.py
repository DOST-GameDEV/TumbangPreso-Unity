"""Build a lata (the can) from a profile and a wrapped label texture.

  python tools/build_lata.py                # every spec below
  python tools/build_lata.py piyesta karne  # just these

⚠️⚠️ THIS IS A PORT OF `tools/models/generate_all.gd` AND `obj_writer.gd` FROM THE
GODOT REPO, WHICH IS FROZEN AND MUST NOT BE RUN OR EDITED. The four shipped cans were
written by that generator and its header still names it, but the Unity repo is the game
now and a new can cannot be added by launching Godot. Everything below that carries a
⚠️ is a finding transcribed from the GDScript, not a decision made here: each one
records a render that was rejected. Read the original before changing any of them.

⚠️ THE PORT IS VERIFIED BY REGENERATING A SHIPPED CAN, NOT BY INSPECTION.
`--verify` rebuilds `lata_decades` and `lata_pasip` into memory and compares them byte
for byte against the files already in the repo. A port that drifts by one ulp in the
normal welding produces a different vertex table and a different-looking can, and that
is not something reading the code catches. Run it after touching anything in here.

Each can is ONE material and ONE texture: the wall takes the wrap by angle and height,
and both caps are pinned to a single texel of the wrap's own rim band. See `cap_v`.
"""

import math
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "Assets/TumbangPreso/Art/models"
TEXTURE_DIR = "textures/"

EPSILON = 1e-5
REVOLVE_SEGMENTS = 16
TAU = math.tau

# ⚠️ THE WALL IS INSET AT BOTH ENDS AND THIS IS THE WHITE RING BETWEEN WALL AND LID.
# The wraps are cropped drawings whose outermost rows are blank page, so mapping the
# wall's height straight onto v 0..1 samples that margin along the can's top and bottom
# edge. 🧑, circling it: *"theres a white space in between lid and rim"*. 3 per cent off
# each end is 3 per cent of a margin nobody drew in.
UV_V_INSET = 0.03

# ⚠️ 40 DEGREES. It smooths the 22.5-degree step of a 16-segment revolve while leaving
# every rim, rib shoulder and cap edge hard.
SMOOTH_ANGLE = 40.0


def fmt(value):
    """`obj_writer.gd::_fmt`. Fixed precision IS the weld key, so this must not change.

    ⚠️ `math.floor(x / s + 0.5)`, NOT `round`. Godot's `snappedf` rounds half AWAY FROM
    ZERO and Python's `round` rounds half to EVEN, so a normal component landing exactly
    on a half-step came out one unit in the fifth decimal apart. Twelve normals out of
    the four shipped cans differed by exactly that, which `--verify` caught and reading
    the code did not: 0.24102 against 0.24101 is invisible in a render and still means
    the port is not the generator.
    """
    if value == 0.0:
        return "%.5f" % 0.0
    snapped = math.floor(value / 0.00001 + 0.5) * 0.00001
    if snapped == 0.0:
        return "%.5f" % 0.0
    return "%.5f" % snapped


class ObjWriter:
    def __init__(self, object_name):
        self.object_name = object_name
        self.verts = []
        self.vert_index = {}
        self.normals = []
        self.normal_index = {}
        self.uvs = []
        self.uv_index = {}
        self.faces = []
        self.material_names = []
        self.material_colors = {}
        self.material_textures = {}

    def set_material(self, name, color, texture=""):
        if name not in self.material_colors:
            self.material_names.append(name)
        self.material_colors[name] = color
        if texture:
            self.material_textures[name] = texture

    def _add(self, table, index, value, parts):
        key = "/".join(fmt(p) for p in parts)
        if key in index:
            return index[key]
        table.append(value)
        index[key] = len(table)
        return len(table)

    def add_vert(self, v):
        return self._add(self.verts, self.vert_index, v, v)

    def add_normal(self, n):
        return self._add(self.normals, self.normal_index, n, n)

    def add_uv(self, t):
        return self._add(self.uvs, self.uv_index, t, t)

    def add_tri(self, a, b, c, material, normals=(), uvs=()):
        if len(normals) == 3:
            na, nb, nc = normals
        else:
            face = face_normal(a, b, c)
            na = nb = nc = face
        vi = [self.add_vert(a), self.add_vert(b), self.add_vert(c)]
        ni = [self.add_normal(na), self.add_normal(nb), self.add_normal(nc)]
        ti = [self.add_uv(t) for t in uvs] if len(uvs) == 3 else []
        self.faces.append({"v": vi, "n": ni, "t": ti, "mat": material})

    def add_quad(self, a, b, c, d, material, normals=(), uvs=()):
        n_abc = [normals[0], normals[1], normals[2]] if len(normals) == 4 else []
        n_acd = [normals[0], normals[2], normals[3]] if len(normals) == 4 else []
        t_abc = [uvs[0], uvs[1], uvs[2]] if len(uvs) == 4 else []
        t_acd = [uvs[0], uvs[2], uvs[3]] if len(uvs) == 4 else []
        self.add_tri(a, b, c, material, n_abc, t_abc)
        self.add_tri(a, c, d, material, n_acd, t_acd)

    def add_revolve(self, profile, segments, material, smooth=True,
                    yaw=0.0, uv_map=None):
        """Spin a (radius, height) profile about Y.

        ⚠️ `a1` REACHES TAU ON THE LAST SEGMENT RATHER THAN WRAPPING TO 0, which is
        what closes the label seam: the final quad's far edge carries u = 0 instead of
        repeating u = 1.
        """
        for s in range(segments):
            a0 = TAU * s / segments
            a1 = TAU * (s + 1) / segments
            for p in range(len(profile) - 1):
                r0, y0 = profile[p]
                r1, y1 = profile[p + 1]
                if r0 < EPSILON and r1 < EPSILON:
                    continue

                v00 = ring_point(r0, y0, a0, yaw)
                v01 = ring_point(r1, y1, a0, yaw)
                v11 = ring_point(r1, y1, a1, yaw)
                v10 = ring_point(r0, y0, a1, yaw)

                normals = []
                if smooth:
                    # Perpendicular to the profile edge, swept around Y. A vertical
                    # wall gives the plain radial normal; a flat annulus gives straight
                    # up or down, which is what makes caps expressible as profile points.
                    ex, ey = r1 - r0, y1 - y0
                    length = math.hypot(ey, -ex) or 1.0
                    flat = (ey / length, -ex / length)
                    normals = [ring_normal(flat, a0, yaw), ring_normal(flat, a0, yaw),
                               ring_normal(flat, a1, yaw), ring_normal(flat, a1, yaw)]

                uvs = []
                if uv_map is not None:
                    uvs = [uv_map(y0, a0), uv_map(y1, a0),
                           uv_map(y1, a1), uv_map(y0, a1)]

                if r0 < EPSILON:
                    self.add_tri(v00, v01, v11, material,
                                 normals[0:3] if smooth else (),
                                 uvs[0:3] if uvs else ())
                elif r1 < EPSILON:
                    self.add_tri(v00, v11, v10, material,
                                 [normals[0], normals[2], normals[3]] if smooth else (),
                                 [uvs[0], uvs[2], uvs[3]] if uvs else ())
                else:
                    self.add_quad(v00, v01, v11, v10, material, normals, uvs)

    def recalculate_normals(self, angle_threshold_deg=SMOOTH_ANGLE):
        threshold = math.cos(math.radians(angle_threshold_deg))

        face_normals = []
        vertex_faces = {}
        for f, face in enumerate(self.faces):
            vi = face["v"]
            n = face_normal(self.verts[vi[0] - 1], self.verts[vi[1] - 1],
                            self.verts[vi[2] - 1])
            face_normals.append(n)
            for corner in range(3):
                vertex_faces.setdefault(vi[corner], []).append(f)

        self.normals = []
        self.normal_index = {}
        for f, face in enumerate(self.faces):
            ni = []
            for corner in range(3):
                v = face["v"][corner]
                own = face_normals[f]
                total = (0.0, 0.0, 0.0)
                for other in vertex_faces[v]:
                    if dot(own, face_normals[other]) >= threshold:
                        total = add(total, face_normals[other])
                if length(total) < EPSILON:
                    total = own
                ni.append(self.add_normal(normalise(total)))
            face["n"] = ni

    def obj_text(self, mtl_filename):
        lines = [
            "# Generated by tools/build_lata.py - DO NOT EDIT BY HAND.",
            "# Change the generator and re-run:",
            "#   python tools/build_lata.py",
            "mtllib " + mtl_filename,
            "o " + self.object_name,
        ]
        for v in self.verts:
            lines.append("v %s %s %s" % (fmt(v[0]), fmt(v[1]), fmt(v[2])))

        textured = bool(self.uvs)
        fallback_uv = 0
        if textured:
            fallback_uv = self.add_uv((0.0, 0.0))
            for t in self.uvs:
                lines.append("vt %s %s" % (fmt(t[0]), fmt(t[1])))
        for n in self.normals:
            lines.append("vn %s %s %s" % (fmt(n[0]), fmt(n[1]), fmt(n[2])))

        # Grouped by material in declaration order so the importer produces one surface
        # per material in a stable order.
        for name in self.material_names:
            wrote_header = False
            for face in self.faces:
                if face["mat"] != name:
                    continue
                if not wrote_header:
                    lines.append("usemtl " + name)
                    wrote_header = True
                vi, ni = face["v"], face["n"]
                if not textured:
                    lines.append("f %d//%d %d//%d %d//%d"
                                 % (vi[0], ni[0], vi[1], ni[1], vi[2], ni[2]))
                    continue
                ti = face["t"] if len(face["t"]) == 3 else [fallback_uv] * 3
                lines.append("f %d/%d/%d %d/%d/%d %d/%d/%d"
                             % (vi[0], ti[0], ni[0], vi[1], ti[1], ni[1],
                                vi[2], ti[2], ni[2]))
        return "\n".join(lines) + "\n"

    def mtl_text(self):
        lines = ["# Generated by tools/build_lata.py - DO NOT EDIT BY HAND."]
        for name in self.material_names:
            r, g, b = self.material_colors[name]
            lines.append("newmtl " + name)
            lines.append("Kd %s %s %s" % (fmt(r), fmt(g), fmt(b)))
            lines.append("Ks 0.00000 0.00000 0.00000")
            # ⚠️ Ns MUST STAY 1000. Godot's .obj importer maps Ns INVERSELY onto
            # metallic (metallic = 1 - Ns/1000), so the spec-correct "barely shiny"
            # Ns 1 imports as an almost fully metallic surface with no diffuse response.
            lines.append("Ns 1000.00000")
            lines.append("d 1.00000")
            lines.append("illum 1")
            if name in self.material_textures:
                lines.append("map_Kd " + self.material_textures[name])
        return "\n".join(lines) + "\n"

    def bounds_size(self):
        lo = [min(v[i] for v in self.verts) for i in range(3)]
        hi = [max(v[i] for v in self.verts) for i in range(3)]
        return [hi[i] - lo[i] for i in range(3)]


def ring_point(radius, y, angle, yaw):
    x, z = radius * math.cos(angle), radius * math.sin(angle)
    c, s = math.cos(yaw), math.sin(yaw)
    return (x * c + z * s, y, -x * s + z * c)


def ring_normal(flat, angle, yaw):
    x, z = flat[0] * math.cos(angle), flat[0] * math.sin(angle)
    c, s = math.cos(yaw), math.sin(yaw)
    return normalise((x * c + z * s, flat[1], -x * s + z * c))


def face_normal(a, b, c):
    u = (b[0] - a[0], b[1] - a[1], b[2] - a[2])
    v = (c[0] - a[0], c[1] - a[1], c[2] - a[2])
    n = (u[1] * v[2] - u[2] * v[1],
         u[2] * v[0] - u[0] * v[2],
         u[0] * v[1] - u[1] * v[0])
    return (0.0, 1.0, 0.0) if length(n) < EPSILON else normalise(n)


def dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def add(a, b):
    return (a[0] + b[0], a[1] + b[1], a[2] + b[2])


def length(v):
    return math.sqrt(dot(v, v))


def normalise(v):
    n = length(v) or 1.0
    return (v[0] / n, v[1] / n, v[2] / n)


def metal_can_profile():
    """The bare ribbed tin.

    ⚠️ THE RIBS ARE REAL GEOMETRY, NOT PAINTED ON, AND THAT IS THE POINT OF THIS CAN.
    The labelled cans are identified by a texture read, which dies at distance under the
    toon pass. The metal can has no label, so its only identity is its silhouette.
    """
    count, depth, low, high = 8, 0.055, 0.190, 0.865
    profile = [(0.90, 0.000), (1.00, 0.030), (0.945, 0.070)]
    span = (high - low) / count
    for i in range(count):
        base = low + span * i
        # Each rib is a shallow valley between two full-radius shoulders, so the wall
        # leaves and returns to 0.945 and no rib can open a seam against the straight
        # sections either side of the run.
        profile.append((0.945, base))
        profile.append((0.945 - depth, base + span * 0.5))
    profile += [(0.945, high), (1.00, 0.958), (0.90, 1.000)]
    return profile


SPECS = {
    # The four shipped cans, transcribed from `generate_all.gd::_lata_specs`. They are
    # here so `--verify` can prove the port, and so a fifth can has something to sit
    # beside. Rebuilding them writes the same bytes that are already committed.
    "pasip": {
        "texture": "lata_pasip.png", "radius": 0.1075, "height": 0.377,
        "cap_v": (0.070, 0.930), "front_u": 0.38,
        "profile": [(0.70, 0.000), (0.88, 0.035), (1.00, 0.080),
                    (1.00, 0.870), (0.88, 0.940), (0.66, 0.980), (0.62, 1.000)],
    },
    "boyben": {
        "texture": "lata_boyben.png", "radius": 0.1425, "height": 0.385,
        "cap_v": (0.050, 0.950), "front_u": 0.22,
        "profile": [(0.86, 0.000), (0.97, 0.025), (1.00, 0.050),
                    (1.00, 0.905), (0.97, 0.928), (1.00, 0.958),
                    (0.95, 0.988), (0.92, 1.000)],
    },
    "decades": {
        "texture": "lata_decades.png", "radius": 0.1225, "height": 0.382,
        "cap_v": (0.070, 0.930), "front_u": 0.22,
        "profile": [(0.90, 0.000), (1.00, 0.030), (0.96, 0.072),
                    (0.96, 0.928), (1.00, 0.970), (0.90, 1.000)],
    },
    "metal": {
        "texture": "lata_metal.png", "radius": 0.1250, "height": 0.383,
        "cap_v": (0.040, 0.070), "front_u": 0.50,
        "profile": metal_can_profile(),
    },

    # ⚠️ THE TWO NEW CANS EACH NEED A SILHOUETTE NOBODY ELSE HAS, and that is a
    # gameplay requirement rather than a flourish: `generate_all.gd` records that a
    # label is a texture read and dies at arena distance under the toon bands, so the
    # profile is what tells a player across the street which lata is standing.
    # Taken: Pasip necks in at both ends, Boyben has a proud lid lip, Decades is
    # double-rimmed and straight, the bare tin is ribbed.

    # The fruit-cocktail tin. The widest can in the set by a clear margin, with one
    # deep seam ring at a third height. Squat and heavy in outline where Decades is
    # slim and evenly rimmed.
    "piyesta": {
        "texture": "lata_piyesta.png", "radius": 0.1560, "height": 0.372,
        # Row 0.055 is inside the deep green band the label runs along both its long
        # edges, so both caps stamp out green rather than sampling the yellow field.
        # Same reasoning as the shipped cans: one texel, from that can's own rim band.
        "cap_v": (0.055, 0.945), "front_u": 0.33,
        "profile": [(0.88, 0.000), (1.00, 0.028), (0.965, 0.068),
                    (0.965, 0.300), (1.00, 0.330), (0.965, 0.360),
                    (0.965, 0.932), (1.00, 0.972), (0.88, 1.000)],
    },

    # The corned beef tin. ⚠️ THE ONLY TAPERED CAN IN THE SET: a real corned beef tin
    # is a truncated cone, narrower at the top, and a cone reads as a cone from any
    # angle and at any distance. It is also the shortest and it keeps the key-strip
    # ridge at the shoulder, which catches its own shading band.
    "karne": {
        "texture": "lata_karne.png", "radius": 0.1480, "height": 0.336,
        # The label is bordered top and bottom by the bare grey tin strip, which is
        # exactly what a stamped can end should look like.
        "cap_v": (0.030, 0.970), "front_u": 0.31,
        "profile": [(0.94, 0.000), (1.00, 0.030), (0.975, 0.070),
                    (0.905, 0.640), (0.945, 0.700), (0.905, 0.760),
                    (0.845, 0.930), (0.880, 0.968), (0.800, 1.000)],
    },
}


def build(name, spec):
    writer = ObjWriter("Lata")
    radius, height = spec["radius"], spec["height"]

    # ⚠️ Kd IS WHITE AND MUST STAY WHITE. The .obj importer multiplies the Kd colour by
    # the map_Kd texture, so any Kd but white darkens the artwork before the skin tint
    # ever reaches it.
    writer.set_material("label", (1.0, 1.0, 1.0), TEXTURE_DIR + spec["texture"])

    # ⚠️ `u` RUNS 1 -> 0, NOT 0 -> 1, AND THAT IS THE MIRRORING FIX. The revolve sweeps
    # counter-clockwise seen from +Y, so `angle / TAU` wraps the label the wrong way and
    # every wordmark reads back to front. The first Godot render had "BOYBEN" as
    # "NEBYOB", which is invisible in a UV dump and instant in a screenshot.
    def wall_uv(y, angle):
        t = y / height
        return (1.0 - angle / TAU,
                UV_V_INSET + (1.0 - 2.0 * UV_V_INSET) * t)

    # ⚠️ THE CAN IS YAWED SO ITS LABEL'S FRONT FACES THE DEFAULT VIEW. The lata has no
    # canonical facing in play, so the angle is free and it is spent on making the
    # livery reviewable: without it the first Godot render framed all four cans from
    # behind, showing three nutrition panels and a barcode.
    front_u = spec["front_u"]
    yaw = math.pi / 4.0 - (1.0 - front_u) * TAU

    profile = spec["profile"]
    wall = [(p[0] * radius, p[1] * height) for p in profile]
    writer.add_revolve(wall, REVOLVE_SEGMENTS, "label", True, yaw, wall_uv)

    # ⚠️ A CAP SAMPLES ONE POINT, NOT ONE ROW. Letting `u` follow the angle sweeps a
    # horizontal LINE of the label radially across the disc, a pinwheel that blows out
    # to flat white on the pale rows under a light hitting a horizontal face square on.
    # 🧑: *"fill that stuff in with somehting why is it just white"*. u = 0.5 is the
    # middle of the wrap, far from the seam where edge filtering can pull in the
    # opposite side of the label.
    cap_low, cap_high = spec["cap_v"]

    # ⚠️ THE CAPS ARE STEPPED, NOT FLAT DISCS. 🧑: *"make sure the can has a top and
    # bottom bcz the metal can has no top man"*. A single flat disc is geometrically a
    # lid but lights as one uniform facet, so at any angle where it catches the same
    # band as the wall it disappears and the can reads as an open tube.
    first, last = profile[0], profile[-1]
    base_y, lid_y = first[1] * height, last[1] * height

    writer.add_revolve([
        (0.0, base_y + height * 0.022),
        (first[0] * radius * 0.86, base_y + height * 0.016),
        (first[0] * radius, base_y),
    ], REVOLVE_SEGMENTS, "label", True, yaw, lambda _y, _a: (0.5, cap_low))

    writer.add_revolve([
        (last[0] * radius, lid_y),
        (last[0] * radius * 0.88, lid_y - height * 0.018),
        (0.0, lid_y - height * 0.018),
    ], REVOLVE_SEGMENTS, "label", True, yaw, lambda _y, _a: (0.5, cap_high))

    writer.recalculate_normals(SMOOTH_ANGLE)
    return writer


# ⚠️⚠️ THE PORT IS COMPARED WITHIN A TOLERANCE, NOT BYTE FOR BYTE, AND THE REASON IS
# GODOT'S NUMBER TYPE RATHER THAN SLOPPINESS. `Vector2` and `Vector3` are `real_t`,
# which is 32-bit float in a standard Godot build, so every profile point, radius and
# swept position was rounded to single precision before `_fmt` ever saw it. Python
# carries the same arithmetic in double precision, so a value sitting near a half-step
# lands on the other side of `snappedf` and prints 0.01320 where Godot printed 0.01319.
# Chasing bit-exactness would mean emulating float32 at forty call sites to defend a
# difference of one hundred-thousandth of a metre.
#
# ⚠️ WHAT IS STILL EXACT IS EVERYTHING THAT WOULD ACTUALLY BREAK A CAN: the vertex,
# normal and UV COUNTS, and every `f` line. Those are integers and topology. The metal
# can's rib loop was written wrong on the first pass here and this check is what caught
# it: 2315 lines against 1811, because the ribs had three profile points each instead of
# two. A reader comparing the two loops did not see it.
FLOAT_TOLERANCE = 2.0e-5


def verify_against_shipped():
    ok = True
    for name in ("pasip", "boyben", "decades", "metal"):
        fresh = build(name, SPECS[name]).obj_text(f"lata_{name}.mtl").splitlines()
        shipped = (OUTPUT_DIR / f"lata_{name}.obj").read_text().splitlines()

        if len(fresh) != len(shipped):
            print(f"  {name:<9} FAIL  {len(fresh)} lines against {len(shipped)}")
            ok = False
            continue

        worst = 0.0
        bad = None
        for i, (a, b) in enumerate(zip(fresh, shipped)):
            if a.startswith("#") or a == b:
                continue
            pa, pb = a.split(), b.split()
            if pa[0] != pb[0] or len(pa) != len(pb) or pa[0] in ("f", "usemtl", "o"):
                bad = bad or (i, a, b)
                ok = False
                continue
            for x, y in zip(pa[1:], pb[1:]):
                delta = abs(float(x) - float(y))
                if delta > worst:
                    worst = delta
                if delta > FLOAT_TOLERANCE:
                    bad = bad or (i, a, b)
                    ok = False

        if bad:
            print(f"  {name:<9} FAIL  line {bad[0]}\n              fresh {bad[1]}"
                  f"\n              ship  {bad[2]}")
        else:
            print(f"  {name:<9} OK    {len(fresh)} lines, worst float delta {worst:.2e}")

    print("[lata] port verified" if ok else "[lata] PORT DIFFERS. Do not ship.")
    return ok


def main():
    args = [a for a in sys.argv[1:] if a != "--verify"]
    verify = "--verify" in sys.argv

    if verify:
        raise SystemExit(0 if verify_against_shipped() else 1)

    wanted = set(args) or {"piyesta", "karne"}
    for name in wanted:
        if name not in SPECS:
            raise SystemExit(f"no spec named '{name}'. Have: {sorted(SPECS)}")
        writer = build(name, SPECS[name])
        (OUTPUT_DIR / f"lata_{name}.obj").write_text(
            writer.obj_text(f"lata_{name}.mtl"), newline="\n")
        (OUTPUT_DIR / f"lata_{name}.mtl").write_text(writer.mtl_text(), newline="\n")
        size = writer.bounds_size()
        print(f"  {name:<9} d {size[0]:.3f}  h {size[1]:.3f}  "
              f"ratio {size[1] / max(size[0], 1e-4):.2f}  "
              f"{len(writer.verts)} verts")


if __name__ == "__main__":
    main()
