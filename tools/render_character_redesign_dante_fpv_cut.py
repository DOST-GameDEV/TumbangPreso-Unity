"""Render what the player would see if Dante's first-person arms were CUT STRAIGHT FROM HIS BODY MODEL.

    "C:/Program Files/Blender Foundation/Blender 5.0/blender.exe" -b --python tools/render_character_redesign_dante_fpv_cut.py -- fpvcut01
    py -3 tools/render_character_redesign_dante_fpv_cut.py fpvcut01        (stacks and labels the frames)

A RENDER ONLY: no model is written. Owner, 2026-10-06: the first-person arms should come directly
from the live body model, "like how minecraft does it", not from a purpose-built one.

The cut is `ViewmodelArmAuthor.Extract` extended to follow the elbow: every triangle of
dante-redesign.glb whose three vertices carry 0.99 or more on `arm-<side>` PLUS `forearm-<side>`,
in the rest pose (arm straight), in the arm bone's bind space, mapped (-side z, side x - first, -y)
and scaled so the length is 0.84. The body's own atlas and UVs. Four mocks from the player's eye,
with the placement, lens and rest transforms of tools/render_character_redesign_dante_fpv_arms.py
(imported, not copied):
    natural    uniform scale only, no width squeeze
    squeezed   x and z scaled so the larger section is 0.34, as `ViewmodelArms.ApplyRosterArm` does today
each as baked (the eye on +Z, the body arm's UNDERSIDE) and rolled 180 degrees about its length
(the eye on the body arm's TOP).
The shading is a soft ramp standing in for the game's smooth (wrapped Lambert) look. It is an
imitation in EEVEE, not `Toon.shader`; nothing here has been through Unity.
"""
import math
import os
import sys

TOOLS = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, TOOLS)
import render_character_redesign_dante_fpv_arms as R  # noqa: E402  the eye, the lens, the rest transforms

BODY = R.D + "dante-redesign.glb"
BODY_ATLAS = R.D + "dante-redesign-atlas.png"
BODY_HAND = (0.222, 0.292)               # author_character_redesign_dante.py build_hand, x from the middle


def cut(side):
    """(points in the first-person frame, uvs, triangles, facts) for one arm of the body model."""
    import build_person_voxel as bpv
    g, buf = bpv.read_glb(BODY)
    positions, uv, triangles = [], [], []
    pivot = 0.0
    for node in g["nodes"]:
        if "mesh" not in node or "skin" not in node:
            continue
        skin = g["skins"][node["skin"]]
        names = [g["nodes"][j]["name"] for j in skin["joints"]]
        if "arm-" + side not in names:
            continue
        own = {names.index(n) for n in ("arm-" + side, "forearm-" + side) if n in names}
        bind = bpv.read_accessor(g, buf, skin["inverseBindMatrices"])[names.index("arm-" + side)]
        pivot = abs(bind[12])
        for prim in g["meshes"][node["mesh"]]["primitives"]:
            att = prim["attributes"]
            P = bpv.read_accessor(g, buf, att["POSITION"])
            T = bpv.read_accessor(g, buf, att["TEXCOORD_0"])
            J = bpv.read_accessor(g, buf, att["JOINTS_0"])
            W = bpv.read_accessor(g, buf, att["WEIGHTS_0"])
            I = [i[0] for i in bpv.read_accessor(g, buf, prim["indices"])]
            owned = [sum(w for j, w in zip(J[i], W[i]) if j in own) > 0.99 for i in range(len(P))]
            remap = {}
            for t in range(0, len(I), 3):
                tri = I[t:t + 3]
                if not all(owned[i] for i in tri):
                    continue
                for i in tri:
                    if i not in remap:
                        remap[i] = len(positions)
                        # glTFast mirrors x; the bind pose is a translation (the rig has no rest rotation)
                        positions.append((-P[i][0] - bind[12], P[i][1] + bind[13], P[i][2] + bind[14]))
                        uv.append((T[i][0], 1.0 - T[i][1]))
                    triangles.append(remap[i])
    s = math.copysign(1.0, sum(p[0] for p in positions) / len(positions))
    first = min(p[0] * s for p in positions)
    last = max(p[0] * s for p in positions)
    scale = R.ARM_LENGTH / (last - first)
    raw = [(-s * p[2], s * p[0] - first, -p[1]) for p in positions]
    span = lambda pts, a: max(p[a] for p in pts) - min(p[a] for p in pts)
    out = [tuple(c * scale for c in p) for p in raw]
    section = max(span(out, 0), span(out, 2))
    hand = [((x - pivot) - first) * scale for x in BODY_HAND]
    far = [p for p in out if p[1] > hand[0] + 0.02]
    facts = dict(tris=len(triangles) // 3, squeeze=min(1.0, R.SECTION_CAP / section))
    print("%s arm cut: %d triangles; natural length %.4f (starts %.4f from the pivot), section %.4f (x) by %.4f (z); scale %.3f"
          % (side, facts["tris"], last - first, first, span(raw, 0), span(raw, 2), scale))
    print("   at 0.84 long: %.3f (x) by %.3f (z), x %.3f..%.3f, z %.3f..%.3f; squeeze to 0.34 is %.3f"
          % (span(out, 0), span(out, 2), min(p[0] for p in out), max(p[0] for p in out),
             min(p[2] for p in out), max(p[2] for p in out), facts["squeeze"]))
    print("   hand block y %.3f..%.3f, %.3f (x) by %.3f (z), z %.3f..%.3f  (shared arm: y 0.620..0.840, 0.316 by 0.300, z -0.150..0.150)"
          % (hand[0], hand[1], span(far, 0), span(far, 2), min(p[2] for p in far), max(p[2] for p in far)))
    return out, uv, [tuple(triangles[i:i + 3]) for i in range(0, len(triangles), 3)], facts


if R.bpy is not None:
    bpy = R.bpy
    from mathutils import Vector

    def soft_material(image):
        """A soft ramp over the diffuse term: an imitation of the game's wrapped-Lambert smooth shading."""
        m = bpy.data.materials.get("soft")
        if m:
            return m
        m = bpy.data.materials.new("soft"); m.use_nodes = True
        nt = m.node_tree; nt.nodes.clear()
        dif = nt.nodes.new("ShaderNodeBsdfDiffuse"); dif.inputs[0].default_value = (1, 1, 1, 1)
        s2r = nt.nodes.new("ShaderNodeShaderToRGB")
        ramp = nt.nodes.new("ShaderNodeValToRGB"); ramp.color_ramp.interpolation = "EASE"
        ramp.color_ramp.elements[0].position = 0.0; ramp.color_ramp.elements[0].color = (0.50, 0.52, 0.60, 1)
        ramp.color_ramp.elements[1].position = 0.75; ramp.color_ramp.elements[1].color = (1.0, 0.97, 0.90, 1)
        tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = image; tex.interpolation = "Linear"
        mul = nt.nodes.new("ShaderNodeMixRGB"); mul.blend_type = "MULTIPLY"; mul.inputs[0].default_value = 1
        em = nt.nodes.new("ShaderNodeEmission"); out = nt.nodes.new("ShaderNodeOutputMaterial")
        nt.links.new(dif.outputs[0], s2r.inputs[0]); nt.links.new(s2r.outputs[0], ramp.inputs[0])
        nt.links.new(tex.outputs[0], mul.inputs[1]); nt.links.new(ramp.outputs[0], mul.inputs[2])
        nt.links.new(mul.outputs[0], em.inputs[0]); nt.links.new(em.outputs[0], out.inputs[0])
        return m

    CUT = {}

    def arm(side, put, outline_scale, width=1.0, roll=False):
        verts, uvs, tris, _ = CUT[side]
        k = -1.0 if roll else 1.0      # a half turn about the arm's length
        image = bpy.data.images.get("body-atlas") or bpy.data.images.load(BODY_ATLAS)
        image.name = "body-atlas"
        return R.mesh_object("cut-" + side, [put((k * v[0] * width, v[1], k * v[2] * width)) for v in verts], tris,
                             soft_material(image), uvs, R.OUTLINE_NEW * outline_scale)

    def main():
        args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["fpvcut00"]
        out = R.BASE_OUT + "/" + args[0]
        os.makedirs(out, exist_ok=True)
        for side in ("right", "left"):
            CUT[side] = cut(side)
        for tag, squeezed, roll in (("natural", False, False), ("natural_rolled", False, True),
                                    ("squeezed", True, False), ("squeezed_rolled", True, True)):
            R.reset()
            for side in ("right", "left"):
                w = CUT[side][3]["squeeze"] if squeezed else 1.0
                arm(side, lambda p, s=side: R.to_view(p, s), R.RIG_SCALE, w, roll)
            R.aim((0, 0, 0), (0, 0, -1), (0, 1, 0), fov=R.FOV, res=(1920, 1080))
            R.render("%s/fp_%s.png" % (out, tag))
        bench = lambda v: Vector(R.to_bench(v))
        mid = (0.0, 0.42, 0.0)
        for side in ("right", "left"):
            R.reset(); arm(side, R.to_bench, 1.0)
            for tag, toward, up in (("top", (0, 0, -1), (-1, 0, 0)), ("under", (0, 0, 1), (1, 0, 0)),
                                    ("xpos", (1, 0, 0), (0, 0, 1)), ("xneg", (-1, 0, 0), (0, 0, 1))):
                R.aim(bench(mid) + bench(toward) * 3.0, -bench(toward), bench(up), ortho=1.0, res=(1300, 760))
                R.render("%s/turn_%s_%s.png" % (out, side, tag))
            R.aim(bench((0, 0.84, 0)) + bench((0, 1, 0)) * 3.0, -bench((0, 1, 0)), bench((0, 0, 1)), ortho=0.62, res=(760, 760))
            R.render("%s/turn_%s_end.png" % (out, side))

    if __name__ == "__main__":
        main()
elif __name__ == "__main__":
    from PIL import Image, ImageDraw, ImageFont

    V = sys.argv[1]
    F = "%s/%s/" % (R.BASE_OUT, V)
    BG = (34, 36, 44)

    def tile(name, label, width, crop=None):
        img = Image.open(F + name).convert("RGB")
        if crop:
            w, h = img.size
            img = img.crop((int(crop[0] * w), int(crop[1] * h), int(crop[2] * w), int(crop[3] * h)))
        img = img.resize((width, int(img.height * width / img.width)), Image.LANCZOS)
        d = ImageDraw.Draw(img)
        try:
            f = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 20)
        except OSError:
            f = ImageFont.load_default()
        box = d.textbbox((0, 0), label, font=f)
        d.rectangle([0, 0, box[2] + 20, box[3] + 16], fill=BG)
        d.text((10, 6), label, font=f, fill=(236, 238, 244))
        return img

    def stack(rows, path, gap=8):
        heights = [max(t.height for t in row) for row in rows]
        widths = [sum(t.width for t in row) + gap * (len(row) - 1) for row in rows]
        sheet = Image.new("RGB", (max(widths) + 2 * gap, sum(heights) + gap * (len(rows) + 1)), BG)
        y = gap
        for row, h in zip(rows, heights):
            x = gap
            for t in row:
                sheet.paste(t, (x, y)); x += t.width + gap
            y += h + gap
        sheet.save(path)
        print("WROTE", path)

    lower = (0.10, 0.40, 0.90, 1.0)
    stack([[tile("fp_natural.png", "%s  1 natural: uniform scale, as baked (eye on the body arm underside)" % V, 1000, lower),
            tile("fp_natural_rolled.png", "1 natural, rolled 180 about its length (eye on the body arm top)", 1000, lower)],
           [tile("fp_squeezed.png", "2 squeezed to 0.34 (ApplyRosterArm today), as baked", 1000, lower),
            tile("fp_squeezed_rolled.png", "2 squeezed to 0.34, rolled 180", 1000, lower)]],
          "%s/%s_four_mocks.png" % (R.BASE_OUT, V))
    stack([[tile("fp_natural.png", "%s  natural, as baked: the whole frame (95 degree lens, rest pose)" % V, 1000),
            tile("fp_natural_rolled.png", "natural, rolled 180: the whole frame", 1000)]],
          "%s/%s_whole_frame.png" % (R.BASE_OUT, V))
    rows = []
    for side in ("right", "left"):
        rows.append([tile("turn_%s_top.png" % side, "%s  %s arm, natural cut: top of the body arm (-Z)" % (V, side), 800),
                     tile("turn_%s_under.png" % side, "underside of the body arm (+Z, what the eye gets today)", 800)])
        rows.append([tile("turn_%s_xpos.png" % side, "side, +X (his %s)" % ("back" if side == "right" else "front"), 700),
                     tile("turn_%s_xneg.png" % side, "side, -X (his %s)" % ("front" if side == "right" else "back"), 700),
                     tile("turn_%s_end.png" % side, "end-on at the fist", 409)])
    stack(rows, "%s/%s_turnaround.png" % (R.BASE_OUT, V))
