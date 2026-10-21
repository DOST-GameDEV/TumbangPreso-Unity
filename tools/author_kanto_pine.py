"""Model Kanto's stylized PINE (a conifer) in Blender, and render review images of it.

  blender -b --python tools/author_kanto_pine.py -- --preview N

Writes Logs/kanto-blender/pine_eye_vN.png (the game's eye), pine_close_vN.png (a 3/4 close-up),
pine_full_vN.png (the whole tree) and pine_zoom_vN.png (the crown from the game's eye, zoomed,
which is where gaps show). Saves no .blend: the map build calls pine() itself.

WHY THIS EXISTS. The roster's "tall" tree was three round leaf balls stacked on a pole, and the
owner rejected it: "this tall tree is weird asf, can you actually model it like a pine tree?"
He then sent a reference for stylized pines: a short straight dark trunk showing only at the
foot, 6 to 9 stacked drooping skirts whose rims are cut into big jagged points, each tier dark
near the trunk and lighter toward a light rim, the whole a tall narrow cone to a sharp point.

HOW IT IS BUILT, AND WHY:
  * TIERS, NOT BALLS. 7 or 8 stacked skirts, each a frustum shell whose profile is CURVED
    (z drops by 0.85*t^0.6 + 0.15*t^3 of its height from the top ring to the rim): steep by
    the trunk, flattening outward, turning down at the rim. The steep top is what makes the
    tiers STEP, because each rim then overhangs a deep notch (a straight cone filled every
    notch and the tree read as one shaggy cone). Tiers shrink toward a sharp top. Each skirt is ~1.9 rim gaps tall,
    so its top runs up inside the tier above and pierces that tier's underside: there is no
    cavity round the trunk for the eye to find through a gap (earlier versions showed bark).
  * THE HOUSE FOLIAGE PATTERN (design guide 5), on a cone instead of a sphere. Every card lies
    ON its skirt, facing out along the surface normal, its tip running DOWN-and-OUT along the
    slope (+-spread), placed by a golden-angle spiral, so the cards shingle like roof tiles.
    Cards are the existing leaf drawing at ~0.36 of their length wide, so they read as needle
    sprays; no new texture.
  * THE JAGGED RIM IS CARDS TOO, not a cut mesh. A ring of bigger cards, evenly spaced round
    each rim and alternating long and short, hangs its pointed tips past the edge: the leaf
    drawing's own point makes each tooth, and the notch between two narrowing tips makes the
    serration. That kept the house rule (shaped see-through cards, no solid shell).
  * TWO TINTS ONLY, laid out as the reference's gradient: leaf_dark<tint> on the inner part of
    each skirt (the band just under the tier above, near the trunk) and on each tier's
    underside, leaf_light<tint> on the outer part and the rim teeth. No per-leaf colour (tried
    and reverted by the owner).
  * ROUNDED NORMALS per tier (Buf.clump_of): the skirt's cards are registered with a centre
    BELOW the rim, so the skirt faces the sky and the rim faces out into the light; the
    underside's cards with a centre ABOVE it, so it faces the ground and shades dark.
  * ONE TRUNK. A single straight, simply tapered tube() (author_kanto_city) in the bark
    material with uv_mode "trunk". It shows only below the bottom rim and stops inside the
    second tier: above that nothing can see it, and a trunk running up the middle only ever
    showed up as a red streak through a gap in the needles.
  * No two surfaces share a plane: the trunk sinks 5 cm below the ground, cards float 2 to 4 cm
    off their skirt and each tier's underside sits inside its own shell.
"""
import math
import random
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_kanto_models as K          # noqa: E402  (Buf, materials, lighting)
import author_kanto_city as C            # noqa: E402  (kit, tube, tree, leaf tints)

UP = Vector((0, 0, 1))
GOLDEN = math.pi * (3 - math.sqrt(5))


class Tier:
    """One drooping skirt: rim at (R, zb), top ring at (r_top, zt)."""

    def __init__(self, zb, zt, R, r_top, lobes, scallop, sag, off):
        self.zb, self.zt, self.R, self.r_top = zb, zt, R, r_top
        self.lobes, self.scallop, self.sag, self.off = lobes, scallop, sag, off
        self.H = zt - zb
        # Rounded-normal centres: below the rim for the skirt (it faces up and out), above the
        # tier for the underside (it faces down). One shared centre lit the underside.
        self.centre = Vector((0, 0, zb - self.H * 0.15))
        self.centre_under = Vector((0, 0, zb + self.H * 1.1))
        # The rim teeth take their normals from a centre far below, so they face the sky and
        # catch the most light: the reference's light rim on every tier.
        self.centre_rim = Vector((0, 0, zb - self.H * 1.2))

    def wave(self, phi):
        return 0.5 + 0.5 * math.cos(self.lobes * phi + self.off)

    def shell(self, t, phi):
        """The outer surface. t = 0 at the top ring, 1 at the rim."""
        w = self.wave(phi)
        R = self.R * (1 + self.scallop * (w - 0.5) * 2)
        r = self.r_top + (R - self.r_top) * t
        # THE PROFILE. Steep by the trunk, flattening outward, turning down again at the rim.
        # Steep-at-the-top is what makes the tiers STEP: at the height where the tier above
        # has its rim, this skirt is still narrow (about a third of its radius), so the rim
        # above overhangs a deep notch. A straight or bell-shaped cone filled every notch and
        # the whole tree read as one shaggy cone (v6).
        g = 0.85 * t ** 0.6 + 0.15 * t ** 3
        z = self.zt - self.H * g - self.sag * w * t ** 3
        return Vector((math.cos(phi) * r, math.sin(phi) * r, z))

    def under(self, t, phi):
        """The underside: a shallow cone from the trunk (t = 0) out to just inside the rim."""
        w = self.wave(phi)
        R = self.R * (1 + self.scallop * (w - 0.5) * 2) * 0.9
        r_in = 0.1
        r = r_in + (R - r_in) * t
        z = self.zb + 0.1 + self.H * 0.4 * (1 - t) ** 1.3
        return Vector((math.cos(phi) * r, math.sin(phi) * r, z))


def _frame(surface, t, phi, outward_up):
    """Point, down-slope tangent and outward normal of `surface` at (t, phi), numerically."""
    e = 1e-3
    p = surface(t, phi)
    dt = surface(min(t + e, 1.0), phi) - surface(max(t - e, 0.0), phi)
    dphi = surface(t, phi + e) - surface(t, phi - e)
    n = dt.cross(dphi)
    if n.length < 1e-9:
        n = Vector((math.cos(phi), math.sin(phi), 0))
    n.normalize()
    radial = Vector((math.cos(phi), math.sin(phi), 0))
    # Outward for the shell means up-and-out; for the underside, down-and-out.
    if (n.z if outward_up else -n.z) + 0.3 * n.dot(radial) < 0:
        n = -n
    return p, dt.normalized(), n


def _card(leaves, surface, centre, t, phi, length, width, mat, outward_up, ang, lift, back, rng, droop=0.0):
    """One card lying on `surface` at (t, phi), tip down-and-out along the slope, bent a
    further `droop` toward the ground (the rim teeth hang)."""
    p, down, n = _frame(surface, t, phi, outward_up)
    side = n.cross(down).normalized()
    tip = (down * math.cos(ang) + side * math.sin(ang) + n * lift - UP * droop).normalized()
    cn = (n - tip * n.dot(tip)).normalized()
    base = p + n * rng.uniform(0.02, 0.04) - tip * length * back
    leaves.leaf(base, tip, cn, length, width, mat, centre)


def _spiral(leaves, surface, centre, count, length, rng, mats, split, outward_up, spread, lift, r0, r1):
    """Golden-angle spiral over the surface, slightly denser toward the top ring than
    area-uniform (u ** 0.62 rather than sqrt), because the top ring is where gaps showed.
    `mats` is (inner, outer): cards inside `split` of the way to the rim wear the first."""
    for i in range(count):
        u = (i + 0.5) / count
        t = min(1.0, max(0.0, u ** 0.62 + rng.uniform(-0.02, 0.02)))
        phi = GOLDEN * i + rng.uniform(-0.25, 0.25)
        ln = length * rng.uniform(0.88, 1.12)
        mat = mats[0] if t < split else mats[1]
        _card(leaves, surface, centre, t, phi, ln, ln * 0.36, mat, outward_up,
              rng.uniform(-spread, spread), lift, 0.45, rng)


def pine(name, seed, tint="_deep", s=1.0):
    """A stylized conifer: origin at the trunk base on the ground, +Z up, about 7.4 m tall and
    3.6 m across at s = 1. Returns the collection, like author_kanto_city.tree()."""
    col = C.kit(name)
    rng = random.Random(seed)
    wood, leaves = K.Buf("trunk"), K.Buf("foliage", foliage=True)
    wood.uv_mode = "trunk"
    light, dark = "leaf_light" + tint, "leaf_dark" + tint

    n = rng.choice((7, 8))
    clear = rng.uniform(1.3, 1.45) * s        # the lowest rim; the trunk shows below it only
    top = rng.uniform(7.2, 7.6) * s           # the apex
    R0 = rng.uniform(1.42, 1.52) * s          # bottom skirt; the rim teeth add ~0.35 m
    top_h = 1.3 * s                           # the top tier is a plain cone to the point
    # Rim gaps shrink upward, so the tiers crowd toward the tip.
    weights = [1.0 - 0.07 * k for k in range(n - 1)]
    gaps = [(top - top_h - clear) * w / sum(weights) for w in weights]
    tiers, zb = [], clear
    for k in range(n):
        f = k / (n - 1)
        R = R0 * (1 - 0.8 * f)
        last = k == n - 1
        H = (top - zb) if last else gaps[k] * 1.9
        tiers.append(Tier(zb, zb + H, R, 0.02 * s if last else R * 0.08, lobes=rng.choice((5, 6, 7)),
                          scallop=0.04, sag=0.1 * s * (1 - 0.5 * f), off=rng.uniform(0, math.tau)))
        if not last:
            zb += gaps[k]

    # THE TRUNK: straight, simply tapered, no flare claws; it stops inside the second tier.
    stop = tiers[1].zb + tiers[1].H * 0.3
    zs = [-0.05, 0.4 * s, clear, (clear + stop) / 2, stop]
    rs = [0.25, 0.215, 0.18, 0.14, 0.1]
    C.tube(wood, [(0.0, 0.0, z) for z in zs], [r * s for r in rs], "trunk", sides=14)

    for k, tier in enumerate(tiers):
        f = k / (n - 1)
        length = (0.56 - 0.18 * f) * s
        card = length * length * 0.36 * 0.72                      # drawn area of one card
        slant = math.hypot(tier.R - tier.r_top, tier.H) * 1.1
        area = math.pi * (tier.R + tier.r_top) * slant
        # The skirt: dark near the trunk, where the tier above shades it, light outward.
        _spiral(leaves, tier.shell, tier.centre, int(area * 3.4 / card), length, rng,
                (dark, light), 0.6 if k < n - 1 else 0.3, True, 0.3, 0.1, tier.r_top, tier.R)
        # The dark underside, from the trunk out to just inside the rim.
        under_area = math.pi * (tier.R * 0.9) ** 2
        _spiral(leaves, tier.under, tier.centre_under, int(under_area * 1.8 / card) + 4, length, rng,
                (dark, dark), 1.0, False, 0.45, 0.12, 0.1, tier.R * 0.9)
        # THE JAGGED RIM: big teeth evenly round the edge, long and short alternately.
        rim_len = length * 1.7
        teeth = max(6, 2 * round(math.tau * tier.R / (rim_len * 0.5) / 2))
        for i in range(teeth):
            phi = i * math.tau / teeth + rng.uniform(-0.06, 0.06) + tier.off
            ln = rim_len * (1.0 if i % 2 == 0 else 0.72) * rng.uniform(0.94, 1.06)
            _card(leaves, tier.shell, tier.centre_rim, 0.88 + 0.05 * (i % 2), phi, ln, ln * 0.4, light, True,
                  rng.uniform(-0.08, 0.08), 0.04, 0.3, rng, droop=0.45)

    # THE TIP: a small crown of sprays pointing up and a little out, closing the apex.
    apex = Vector((0, 0, tiers[-1].zt))
    for i in range(5):
        a = i * math.tau / 5 + rng.uniform(-0.2, 0.2)
        out = Vector((math.cos(a), math.sin(a), 0))
        tip = (UP + out * 0.1).normalized()      # nearly upright, so the five meet in ONE point
        cn = (out - tip * out.dot(tip)).normalized()
        ln = 0.55 * s
        leaves.leaf(apex - UP * 0.4 * s + out * 0.02, tip, cn, ln, ln * 0.3, light, tiers[-1].centre_rim)

    wood.finish(col, bevel=0.03)
    leaves.finish(col)
    return col


# ------------------------------------------------------------------ preview

def _preview(version):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    K.USE_TEXTURES = True
    col = pine("pine_a", 1)
    faces = sum(len(o.data.polygons) for o in col.all_objects if o.type == "MESH")
    pts = [o.matrix_world @ Vector(c) for o in col.all_objects if o.type == "MESH" for c in o.bound_box]
    print(f"[kanto-pine] pine_a: {faces} faces, height {max(p.z for p in pts):.2f} m, "
          f"width {max(p.x for p in pts) - min(p.x for p in pts):.2f} m")
    # A second pine in the base tint and another seed, off to the left of the game-eye shot:
    # the reference's rims are a light lime, and "_deep" is the darker of the two choices.
    other = pine("pine_b", 2, tint="")
    for o in other.objects:
        o.location.x -= 5
    ref = C.tree("tree_round", style="round", seed=202)
    for o in ref.objects:
        o.location.x += 6
    ground = K.Buf("ground")
    ground.face([(-40, -40, 0), (40, -40, 0), (40, 40, 0), (-40, 40, 0)], "grass")
    ground.finish(bpy.context.scene.collection, bevel=0)
    K.setup_lighting()
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    K.PREVIEWS.mkdir(parents=True, exist_ok=True)
    eye = 18 / math.tan(math.radians(47.5))   # 95 degrees across, the game's eye
    for label, pos, look, lens in (("eye", Vector((3.0, -9.0, 1.25)), Vector((3.0, 0.0, 2.2)), eye),
                                   ("close", Vector((-3.4, -3.7, 3.0)), Vector((0.0, 0.0, 3.3)), 30),
                                   ("full", Vector((-6.5, -10.5, 3.2)), Vector((0.0, 0.0, 3.7)), 35),
                                   ("zoom", Vector((0.0, -9.0, 1.25)), Vector((0.0, 0.0, 4.6)), 75)):
        cam.location, cam.data.lens = pos, lens
        cam.data.sensor_fit = "HORIZONTAL"
        cam.rotation_euler = (look - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(K.PREVIEWS / f"pine_{label}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[kanto-pine] preview", scene.render.filepath)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if "--preview" in argv:
        _preview(int(argv[argv.index("--preview") + 1]))
