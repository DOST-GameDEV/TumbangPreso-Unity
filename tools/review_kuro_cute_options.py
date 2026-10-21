"""Kuro, remodelled cuter: CONCEPTS as quick blockouts side by side (owner, 2026-10-06, of Kuro riding Nemu's
first-person sleeve: "looks cute, but we need to remodel kuro to be more cutesy").

  blender -b --python tools/review_kuro_cute_options.py -- --version=v1
  py -3 tools/review_kuro_cute_options.py --join --version=v1

Writes Logs/kuro-cute/kuro_options_<vN>.png: three blockouts, each from the front, three-quarter, and peeking over
a sleeve cuff at the size he rides her hand. Nothing here is the game's model: the chosen one is built properly.
His identity is kept in all three: the near-black plum body, lavender eyes and tail, the peach blush.

  A  MOCHI      a soft dumpling: wider than tall, huge low eyes, a curl on top, nub arms, a scalloped hem.
  B  KUBO       still a block (the game's voxel Kuro) but rounded off and big-headed, ear tufts, paws, a flame tail.
  C  TALUKBONG  a little hooded ghost in a cowl like Nemu's own, a pale face inside, sleepy lids, a floppy hood tip.
"""
import math, os, sys

JOIN = "--join" in sys.argv
if not JOIN:
    import bpy
    from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGS = os.path.join(ROOT, "Logs", "kuro-cute")
VERSION = next((a.split("=")[1] for a in sys.argv if a.startswith("--version=")), "v1")

PLUM, PLUM2, LAV, PALE, PEACH, INK, WHITE, CUFF, FACE = ((0.105, 0.075, 0.17), (0.17, 0.12, 0.27), (0.67, 0.36, 0.94),
    (0.90, 0.80, 1.0), (1.0, 0.62, 0.45), (0.02, 0.02, 0.03), (1, 1, 1), (0.137, 0.11, 0.204), (0.86, 0.80, 0.95))
_m = {}


def mat(rgb, glow=0.55):
    key = (rgb, glow)
    if key not in _m:
        m = bpy.data.materials.new("flat")
        m.use_nodes = True
        b = m.node_tree.nodes.get("Principled BSDF")
        b.inputs["Base Color"].default_value = (*rgb, 1)
        b.inputs["Roughness"].default_value = 0.9
        b.inputs["Emission Color"].default_value = (*rgb, 1)
        b.inputs["Emission Strength"].default_value = glow
        _m[key] = m
    return _m[key]


def ink_mat():
    if "ink" not in _m:
        m = bpy.data.materials.new("ink")
        m.use_nodes = True
        b = m.node_tree.nodes.get("Principled BSDF")
        b.inputs["Base Color"].default_value = (*INK, 1)
        b.inputs["Emission Color"].default_value = (*INK, 1)
        b.inputs["Roughness"].default_value = 1
        m.use_backface_culling = True
        _m["ink"] = m
    return _m["ink"]


def finish(o, colour, ink=0.012, smooth=True, glow=0.55):
    o.data.materials.append(mat(colour, glow))
    if smooth:
        for p in o.data.polygons:
            p.use_smooth = True
    if ink > 0:
        o.data.materials.append(ink_mat())
        s = o.modifiers.new("ink", "SOLIDIFY")
        s.thickness = ink; s.offset = 1; s.use_flip_normals = True; s.material_offset = 1; s.use_rim = False
    return o


def ball(at, size, colour, ink=0.012, rot=(0, 0, 0), glow=0.55):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=28, ring_count=16, location=at, rotation=rot)
    o = bpy.context.object
    o.scale = size if isinstance(size, tuple) else (size, size, size)
    bpy.ops.object.transform_apply(scale=True)
    return finish(o, colour, ink, glow=glow)


def box(at, size, colour, round_=0.3, ink=0.012, rot=(0, 0, 0), glow=0.55):
    bpy.ops.mesh.primitive_cube_add(size=1, location=at, rotation=rot)
    o = bpy.context.object
    o.scale = size
    bpy.ops.object.transform_apply(scale=True)
    b = o.modifiers.new("round", "BEVEL")
    b.width = round_ * min(size); b.segments = 5; b.limit_method = "NONE"
    bpy.ops.object.modifier_apply(modifier="round")
    return finish(o, colour, ink, glow=glow)


def cone(at, r, h, colour, rot=(0, 0, 0), ink=0.012):
    bpy.ops.mesh.primitive_cone_add(vertices=14, radius1=r, radius2=r * .08, depth=h, location=at, rotation=rot)
    return finish(bpy.context.object, colour, ink)


F = -1  # the face is on -y (the camera's side)


def eye(x, z, y, w, h, lid=0.0, colour=LAV):
    """A big flat oval eye with a pale shine, on the front; `lid` 0..1 closes it from the top (sleepy)."""
    ball((x, y, z), (w, .02, h), colour, ink=0.006, glow=.9)
    ball((x - w * .28, y - .012, z + h * .30), (w * .34, .012, h * .30), WHITE, ink=0, glow=1.2)
    ball((x + w * .30, y - .012, z - h * .34), (w * .16, .012, h * .14), PALE, ink=0, glow=1.2)
    if lid > 0:
        box((x, y - .006, z + h * (1 - lid)), (w * 2.3, .04, h * 2 * lid), PLUM, round_=.1, ink=0)


def blush(x, z, y, s=.045):
    ball((x, y, z), (s, .01, s * .55), PEACH, ink=0, glow=.9)


def option_a(o):
    """MOCHI: a dumpling."""
    x = o
    ball((x, 0, .30), (.36, .31, .27), PLUM)                                     # the body, wider than tall
    for k, dx in enumerate((-.21, 0, .21)):                                       # a hem of three scallops
        ball((x + dx, 0, .075), (.125, .14, .09), PLUM)
    ball((x + .30, .08, .02), (.07, .07, .055), LAV, glow=.8)                     # a little tail curl, off to one side
    ball((x + .37, .08, -.05), (.04, .04, .035), LAV, glow=.8)
    ball((x - .37, -.04, .22), (.075, .07, .06), PLUM)                            # nub arms
    ball((x + .37, -.04, .22), (.075, .07, .06), PLUM)
    ball((x + .03, 0, .60), (.06, .06, .06), PLUM)                                # the curl on top
    ball((x + .09, 0, .665), (.04, .04, .04), LAV, glow=.8)
    eye(x - .145, .30, -.29, .085, .105); eye(x + .145, .30, -.29, .085, .105)
    blush(x - .245, .20, -.27); blush(x + .245, .20, -.27)
    ball((x, -.30, .215), (.022, .012, .016), PALE, ink=.004, glow=1)             # a dot of a mouth


def option_b(o):
    """KUBO: the block, rounded."""
    x = o
    box((x, 0, .34), (.58, .50, .50), PLUM, round_=.42)                           # a big rounded head-body
    box((x, .02, .06), (.30, .26, .16), LAV, round_=.45, glow=.8)                 # a stubby flame tail, two steps
    box((x + .05, .03, -.07), (.17, .15, .12), LAV, round_=.45, glow=.8)
    box((x + .10, .03, -.17), (.08, .08, .07), PALE, round_=.45, glow=1)
    cone((x - .22, 0, .64), .085, .15, PLUM, rot=(0, math.radians(-16), 0))       # ear tufts on the top corners
    cone((x + .22, 0, .64), .085, .15, PLUM, rot=(0, math.radians(16), 0))
    box((x - .30, -.10, .17), (.12, .12, .10), PLUM2, round_=.45)                 # paws
    box((x + .30, -.10, .17), (.12, .12, .10), PLUM2, round_=.45)
    eye(x - .135, .36, -.25, .08, .10); eye(x + .135, .36, -.25, .08, .10)
    blush(x - .215, .25, -.245, .05); blush(x + .215, .25, -.245, .05)
    for dx in (-.022, .022):                                                       # a small cat mouth
        ball((x + dx, -.262, .262), (.024, .01, .014), PALE, ink=.004, glow=1)


def option_c(o):
    """TALUKBONG: hooded."""
    x = o
    ball((x, 0, .33), (.31, .29, .31), PLUM)                                      # the hood
    cone((x + .04, .05, .66), .12, .22, PLUM, rot=(math.radians(-18), math.radians(22), 0))   # its tip, flopped over
    ball((x + .12, .11, .76), (.045, .045, .045), LAV, glow=.8)
    ball((x, -.115, .30), (.235, .19, .215), FACE, ink=.006, glow=.75)             # the pale face inside it
    box((x, -.03, .075), (.50, .40, .07), LAV, round_=.4, glow=.8)                 # the cowl's band, as on her sleeves
    for dx in (-.17, 0, .17):                                                       # a ragged hem under the band
        cone((x + dx, 0, -.03), .085, .15, PLUM, rot=(math.radians(180), 0, 0))
    ball((x - .30, -.08, .17), (.07, .07, .06), PLUM)                              # mittens poking out
    ball((x + .30, -.08, .17), (.07, .07, .06), PLUM)
    eye(x - .095, .32, -.30, .06, .075, lid=.42, colour=PLUM); eye(x + .095, .32, -.30, .06, .075, lid=.42, colour=PLUM)
    blush(x - .155, .245, -.285, .04); blush(x + .155, .245, -.285, .04)
    cone((x + .02, -.305, .225), .016, .03, WHITE, rot=(math.radians(180), 0, 0), ink=.003)   # one tiny fang
    ball((x, -.30, .245), (.035, .01, .01), PLUM, ink=0)


def sleeve(x):
    """A stand-in for her cuff, across his lower half: he peeks over it."""
    box((x, -.38, -.02), (1.05, .10, .50), CUFF, round_=.1, ink=.012, rot=(math.radians(-12), 0, 0))
    box((x, -.42, .235), (1.09, .10, .07), LAV, round_=.2, ink=.012, rot=(math.radians(-12), 0, 0), glow=.8)


def render(name, cam_at, look, ortho, res=(1500, 560)):
    cam_data = bpy.data.cameras.new("c"); cam = bpy.data.objects.new("c", cam_data)
    bpy.context.scene.collection.objects.link(cam)
    cam_data.type = "ORTHO"; cam_data.ortho_scale = ortho
    cam.location = cam_at
    cam.rotation_euler = (Vector(look) - Vector(cam_at)).to_track_quat("-Z", "Y").to_euler()
    s = bpy.context.scene
    s.camera = cam; s.render.resolution_x, s.render.resolution_y = res
    s.render.filepath = os.path.join(LOGS, name)
    bpy.ops.render.render(write_still=True)
    return s.render.filepath


def main():
    os.makedirs(LOGS, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    s = bpy.context.scene
    s.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else "BLENDER_EEVEE"
    s.view_settings.view_transform = "Standard"
    w = bpy.data.worlds.new("w"); s.world = w; w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.40, 0.46, 0.56, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN")); s.collection.objects.link(sun)
    sun.data.energy = 2.2; sun.rotation_euler = (math.radians(50), 0, math.radians(-30))

    xs = (-1.25, 0, 1.25)
    option_a(xs[0]); option_b(xs[1]); option_c(xs[2])
    front = render("_front.png", (0, -6, .30), (0, 0, .30), 3.9)
    quarter = render("_quarter.png", (3.2, -5.2, 1.5), (0, 0, .28), 3.9)
    for x in xs:
        sleeve(x)
    peek = render("_peek.png", (0, -6, 1.7), (0, 0, .25), 3.9)

    print("rows written; joining needs Pillow, which Blender's Python lacks: run this file with --join under system Python")


def join():
    from PIL import Image, ImageDraw
    paths = [os.path.join(LOGS, n) for n in ("_front.png", "_quarter.png", "_peek.png")]
    rows = [Image.open(p).convert("RGB") for p in paths]
    W, H = rows[0].size
    sheet = Image.new("RGB", (W, H * 3 + 40), (20, 22, 30))
    d = ImageDraw.Draw(sheet)
    for k, label in enumerate(("A  MOCHI", "B  KUBO", "C  TALUKBONG")):
        d.text((W * (k * 2 + 1) // 6 - 30, 14), label, fill=(255, 255, 255))
    for k, im in enumerate(rows):
        sheet.paste(im, (0, 40 + k * H))
        d.text((8, 46 + k * H), ("front", "three-quarter", "over her cuff")[k], fill=(255, 255, 255))
    out = os.path.join(LOGS, "kuro_options_%s.png" % VERSION)
    sheet.save(out)
    for p in paths:
        os.remove(p)
    print("wrote", out)


join() if JOIN else main()
