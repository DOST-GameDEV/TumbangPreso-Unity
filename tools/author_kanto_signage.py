"""Shop signage for the Kanto sample map: shop-name boards, hanging blade signs, billboards.

  blender -b --python tools/author_kanto_signage.py -- --preview N [--design soda|halo|sun]
  blender -b --python tools/author_kanto_signage.py -- --zoo N      (every name, icon and design)

Renders a test wall carrying four fascia signs, two blade signs and a rooftop billboard to
Logs/kanto-blender/signage_front_vN.png, signage_eye_vN.png (the game's eye from across the
street), signage_close_vN.png and signage_rear_vN.png; --zoo writes signage_zoo*_vN.png. It saves
no .blend: this file is a kit that tools/author_kanto_city.py (or anything else) imports.

WHY. The references (Tiny Talisman's "Stylized Modern City", Brainchild's cartoon town) put a
shop name over every shopfront, round hanging signs over the pavement and big illustrated
billboards on the roofs. Ours had none (docs/KANTO_DESIGN_GUIDE.md section 10, "street life").
The game is Filipino, so the shops are the ones on any Manila street: sari-sari store,
panaderia, karinderya, botika, barbero.

THE API. Every builder takes `dest`, which is EITHER a bpy collection (the sign becomes its own
objects in it: a bevelled body and a separate, unbevelled text object) OR a kit `K.Buf` (the
geometry is merged into that buffer, which the caller finishes). Prefer a collection: a
building buffer's 3 cm bevel is too big for letter strokes. `at` is an optional 4x4 matrix that
places the whole sign; `K.Facade.frame(u, z)` is already the right matrix for a fascia or a
blade sign, because its columns are (along the wall, out of the wall, up).

  fascia_sign(dest, text, width, style="board", seed=0, at=None)
      A shop-name board `width` m wide and 0.70 to 0.85 m tall. Styles:
        "board"    a painted plank panel inside a thick rounded rim, two bolts
        "letters"  raised individual 3D letters standing on a thin iron rail (one line)
        "lightbox" a boxy sign with a cap and lip and a cream face panel
  blade_sign(dest, content, seed=0, at=None)
      A round or shield-shaped hanging sign on an iron wall bracket. `content` is an icon name
      (ICONS: bread, cup, mortar, scissors, bowl), a shop name from SHOP_ICON, or 1 to 3 letters.
  billboard(dest, seed=0, w=8, h=4, at=None, design=None)
      A rooftop billboard on a steel frame with a catwalk and three lamps, and a flat-colour
      illustration made of geometry plus a short Filipino slogan. DESIGNS: soda, halo, sun.
      No real brands, logos or people.

⚠️ THE FACING CONVENTION, for all three: the local frame has +Z up and the wall (or the roof
edge) is the plane y = 0 with the building at y < 0.
  * fascia_sign: the face looks along +Y. Origin = bottom centre of the sign ON the wall face.
    The back sinks 2 cm into the wall (y = -0.02); the front is at about y = +0.2.
    Seen from +Y the text reads left to right, which is along -X.
  * blade_sign: origin = where the bracket arm meets the wall face. The arm runs along +Y at
    z = 0 to y = 1.27; the sign (1 m across) hangs below it, centred at y = 0.72, z = -0.74, and
    spans about z = -0.24 to -1.3. The sign is double sided: it faces +X and -X and reads correctly from both.
  * billboard: origin = bottom centre of the frame ON the roof. The face looks along +Y; the
    board spans z = 1.4 to 1.4 + h, the catwalk and lamps stick out to y = +0.9 and the steel
    frame and kickers run back to y = -2.35. The base plates sink 2 cm into the roof.

⚠️ NO TWO SURFACES SHARE A PLANE (KANTO_DESIGN_GUIDE section 2). Every layer of a sign sits
1.5 to 2 cm proud of the one under it and sinks 1 to 1.5 cm into it: rims overlap panels,
letters sink into boards, illustration layers are stacked at 3 cm steps 4.5 cm thick, brackets
and legs penetrate what they hold.

⚠️ ROLE HUES (Art_Direction.md section 1): nothing near offence orange #f87020 or defence blue
#0080e8. Bread crust is a yellow tan, the sun is gold with yellow rays, the sky is teal, the
ube is purple. Keep new colours out of both.

TEXT is a bpy FONT curve (Arial Black if the machine has it, then Impact, Arial Bold, Segoe UI
Bold, then Blender's built-in font), extruded and bevelled, converted to a mesh through the
evaluated depsgraph and fitted to its box: the words are scaled to the box and, when that makes
the letters much bigger, split onto two lines. Across a 10 m road a 0.3 m letter is about 18
pixels at the game's 95 degree 1600 px view, which reads.
"""
import math
import random
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_kanto_models as K          # noqa: E402  (Buf, material, setup_lighting)

ROOT = Path(__file__).resolve().parents[1]
PREVIEWS = ROOT / "Logs" / "kanto-blender"

SHOP_NAMES = [
    "SARI-SARI STORE", "PANADERIA", "KARINDERYA", "BOTIKA", "KAPEHAN", "BARBERO",
    "LUGAWAN", "BIGASAN", "TAHO", "KAKANIN", "LABADA", "HARDWARE", "SIOPAO", "LECHON",
    "MERYENDA", "BUKO JUICE",
]
# The hanging sign that goes with a shop, when there is an obvious picture for it.
SHOP_ICON = {"PANADERIA": "bread", "KAPEHAN": "cup", "BOTIKA": "mortar", "BARBERO": "scissors",
             "LUGAWAN": "bowl", "KARINDERYA": "bowl", "SIOPAO": "bowl", "MERYENDA": "cup"}

# New materials, linear RGB. Reused from the kit instead of added: "sign" (yellow), "wood",
# "metal" (steel frame), "white", "frame" (cream). Every hue here was checked against the two
# role colours: the nearest to #f87020 is sign_crust at hue 39 (orange is 22), and nothing
# sits between hue 180 and 240.
PALETTE_ADD = {
    "sign_red":   (0.60, 0.06, 0.05),
    "sign_cream": (0.94, 0.86, 0.66),
    "sign_gold":  (0.96, 0.70, 0.10),
    "sign_ink":   (0.05, 0.035, 0.03),
    "sign_teal":  (0.04, 0.30, 0.27),
    "sign_leaf":  (0.08, 0.34, 0.13),
    "sign_plum":  (0.28, 0.06, 0.16),
    "sign_pink":  (0.88, 0.40, 0.45),
    "sign_mint":  (0.45, 0.74, 0.55),
    "sign_ube":   (0.28, 0.10, 0.45),
    "sign_crust": (0.60, 0.40, 0.12),
    "sign_iron":  (0.07, 0.07, 0.07),
}
K.PALETTE.update(PALETTE_ADD)

# (board, text, rim) for the painted "board" style. Dark boards take cream text, light boards
# take red or ink, so every pair clears a strong value contrast at 10 m.
BOARD_COMBOS = [
    ("sign_red", "sign_cream", "sign_gold"),
    ("sign_leaf", "sign_cream", "sign_gold"),
    ("sign", "sign_red", "sign_red"),
    ("sign_teal", "sign_cream", "sign_cream"),
    ("sign_cream", "sign_red", "sign_leaf"),
    ("sign_plum", "sign_gold", "sign_cream"),
    ("sign_mint", "sign_ink", "sign_cream"),
    ("sign_pink", "sign_ink", "sign_cream"),   # cream on pink measured too soft (zoo v3)
]
# (body, text, cap) for the "lightbox" style; its face is always cream.
LIGHTBOX_COMBOS = [
    ("sign_red", "sign_red", "sign_iron"),
    ("sign_leaf", "sign_leaf", "sign_gold"),
    ("sign_teal", "sign_teal", "sign_iron"),
    ("sign_plum", "sign_plum", "sign_gold"),
    ("sign_gold", "sign_red", "sign_red"),
]
LETTER_COLOURS = ["sign_red", "sign_leaf", "sign_plum", "sign_teal"]
# (disc, rim, icon accent) for blade signs.
BLADE_COMBOS = [
    ("sign_red", "sign_gold", "sign_cream"),
    ("sign_leaf", "sign_cream", "sign_gold"),
    ("sign_teal", "sign_cream", "sign_gold"),
    ("sign_cream", "sign_red", "sign_red"),
    ("sign_plum", "sign_gold", "sign_cream"),
]

# The sign builds its face in a FLAT frame: X along the reading direction, Y up, Z towards
# the viewer. These map that frame onto the local one. Each is a proper rotation (det +1),
# so nothing is mirrored and the text reads correctly from the side it faces.
FACE_Y = Matrix(((-1, 0, 0, 0), (0, 0, 1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))   # faces +Y, reads along -X
SIDE_A = Matrix(((0, 0, 1, 0), (1, 0, 0, 0), (0, 1, 0, 0), (0, 0, 0, 1)))    # faces +X, reads along +Y
SIDE_B = Matrix(((0, 0, -1, 0), (-1, 0, 0, 0), (0, 1, 0, 0), (0, 0, 0, 1)))  # faces -X, reads along -Y

FONT_DIR = Path("C:/Windows/Fonts")
FONT_FILES = {"bold": ["ariblk.ttf", "impact.ttf", "arialbd.ttf", "segoeuib.ttf"],
              "tall": ["impact.ttf", "ariblk.ttf", "arialbd.ttf"]}


# ------------------------------------------------------------------ plumbing

class _Frame:
    """Transform every vertex added to `buf` inside the block by `matrix` on the way out.
    Nested frames compose: the innermost is applied first."""

    def __init__(self, buf, matrix):
        self.buf, self.m = buf, matrix

    def __enter__(self):
        self.n = len(self.buf.bm.verts)
        return self

    def __exit__(self, kind, *_):
        if kind is None:
            bm = self.buf.bm
            bm.verts.ensure_lookup_table()
            verts = [bm.verts[i] for i in range(self.n, len(bm.verts))]
            if verts:
                bmesh.ops.transform(bm, matrix=self.m, verts=verts)
        return False


def _slug(text):
    return "".join(c.lower() if c.isalnum() else "_" for c in text).strip("_")[:24] or "sign"


def _targets(dest, name, bevel):
    """(body buffer, text buffer, finish). A Buf destination takes everything itself."""
    if isinstance(dest, K.Buf):
        return dest, dest, lambda: []
    body, text = K.Buf(name), K.Buf(name + "_text")

    def finish():
        objs = [body.finish(dest, bevel=bevel, segments=2, angle=40)]
        if text.bm.verts:
            t = text.finish(dest, bevel=0)
            for p in t.data.polygons:   # flat letters: welded caps and sides must not smooth together
                p.use_smooth = False
            objs.append(t)
        else:
            text.bm.free()
        return objs
    return body, text, finish


def _font(key):
    for f in FONT_FILES[key]:
        p = FONT_DIR / f
        if p.exists():
            return bpy.data.fonts.load(str(p), check_existing=True)
    return None   # Blender's built-in font


def _font_mesh(body, size, extrude, bevel, font):
    cu = bpy.data.curves.new("sign_text", type="FONT")
    cu.body = body
    if font is not None:
        cu.font = font
    cu.size = size
    cu.align_x, cu.align_y = "CENTER", "CENTER"
    cu.space_line = 0.95
    cu.extrude = extrude
    cu.bevel_depth, cu.bevel_resolution = bevel, 1
    cu.resolution_u = 5
    ob = bpy.data.objects.new("sign_text_tmp", cu)
    bpy.context.scene.collection.objects.link(ob)
    bpy.context.view_layer.update()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(bpy.context.evaluated_depsgraph_get()))
    bpy.data.objects.remove(ob)
    bpy.data.curves.remove(cu)
    return me


def _bounds(me):
    xs, ys, zs = ([v.co[i] for v in me.vertices] for i in range(3))
    return min(xs), max(xs), min(ys), max(ys), min(zs), max(zs)


def _two_lines(text):
    words = text.split()
    best = None
    for i in range(1, len(words)):
        a, b = " ".join(words[:i]), " ".join(words[i:])
        if best is None or max(len(a), len(b)) < max(len(best[0]), len(best[1])):
            best = (a, b)
    return best[0] + "\n" + best[1]


def text(buf, body, box_w, box_h, cx, cy, z_back, mat, extrude=0.01, bevel=0.005,
         font="bold", lines=2, anchor="center"):
    """Words fitted into a box on the flat frame's XY plane, standing from z_back towards +Z.
    anchor "bottom" puts the letters' feet on cy (for letters standing on a rail).
    Returns the fitted (width, height)."""
    fnt = _font(font)
    options = [body]
    if lines >= 2 and " " in body.strip():
        options.append(_two_lines(body))
    best = None
    for opt in options:
        me = _font_mesh(opt, 1.0, 0.0, 0.0, fnt)
        x0, x1, y0, y1, *_ = _bounds(me)
        bpy.data.meshes.remove(me)
        s = min((box_w - 2 * bevel) / (x1 - x0), (box_h - 2 * bevel) / (y1 - y0))
        # Two lines only when they buy clearly bigger letters: one line reads faster.
        if best is None or s > best[0] * 1.15:
            best = (s, opt)
    s, opt = best
    me = _font_mesh(opt, s, extrude, bevel, fnt)
    x0, x1, y0, y1, z0, _ = _bounds(me)
    oy = (cy - y0) if anchor == "bottom" else cy - (y0 + y1) / 2
    off = Vector((cx - (x0 + x1) / 2, oy, z_back - z0))
    idx = buf.mi(mat)
    vs = [buf.bm.verts.new(v.co + off) for v in me.vertices]
    for p in me.polygons:
        try:
            buf.bm.faces.new([vs[i] for i in p.vertices]).material_index = idx
        except ValueError:
            pass
    bpy.data.meshes.remove(me)
    return x1 - x0, y1 - y0


# ------------------------------------------------------------------ flat shapes

def rrect(cx, cy, w, h, r, seg=5):
    """A rounded rectangle, counter-clockwise."""
    r = max(0.005, min(r, w / 2 - 0.001, h / 2 - 0.001))
    pts = []
    for (qx, qy), a0 in (((w / 2 - r, h / 2 - r), 0), ((-w / 2 + r, h / 2 - r), 90),
                         ((-w / 2 + r, -h / 2 + r), 180), ((w / 2 - r, -h / 2 + r), 270)):
        for i in range(seg + 1):
            a = math.radians(a0 + 90 * i / seg)
            pts.append((cx + qx + r * math.cos(a), cy + qy + r * math.sin(a)))
    return pts


def ellipse(cx, cy, rx, ry, n=24, a0=0.0, a1=math.tau):
    full = abs(a1 - a0) >= math.tau - 1e-6
    count = n if full else n + 1
    return [(cx + rx * math.cos(a0 + (a1 - a0) * i / n), cy + ry * math.sin(a0 + (a1 - a0) * i / n))
            for i in range(count)]


def circle(cx, cy, r, n=24):
    return ellipse(cx, cy, r, r, n)


def star(cx, cy, r_out, r_in, n):
    return [(cx + (r_out if i % 2 == 0 else r_in) * math.cos(math.pi / 2 + i * math.pi / n),
             cy + (r_out if i % 2 == 0 else r_in) * math.sin(math.pi / 2 + i * math.pi / n)) for i in range(2 * n)]


def bar(p, q, w0, w1=None):
    """A tapered bar from p to q, w0 wide at p and w1 at q."""
    w1 = w0 if w1 is None else w1
    p, q = Vector((*p, 0)), Vector((*q, 0))
    d = (q - p).normalized()
    n = Vector((-d.y, d.x, 0))
    return [tuple((p - n * w0 / 2)[:2]), tuple((q - n * w1 / 2)[:2]),
            tuple((q + n * w1 / 2)[:2]), tuple((p + n * w0 / 2)[:2])]


def ring(buf, cx, cy, r0, r1, z0, z1, mat, a0=0.0, a1=math.tau, n=28):
    """A flat annulus (or an arc of one) extruded from z0 to z1."""
    full = abs(a1 - a0) >= math.tau - 1e-6
    steps = n if full else max(3, int(n * abs(a1 - a0) / math.tau) + 1)
    count = steps if full else steps + 1
    rings = []
    for i in range(count):
        a = a0 + (a1 - a0) * i / steps
        c, s = math.cos(a), math.sin(a)
        rings.append([buf.bm.verts.new((cx + r * c, cy + r * s, z)) for r, z in ((r0, z0), (r1, z0), (r1, z1), (r0, z1))])
    idx = buf.mi(mat)
    for i in range(count if full else count - 1):
        a, b = rings[i], rings[(i + 1) % count]
        for j in range(4):
            buf.bm.faces.new((a[j], a[(j + 1) % 4], b[(j + 1) % 4], b[j])).material_index = idx
    if not full:
        buf.bm.faces.new(rings[0]).material_index = idx
        buf.bm.faces.new(list(reversed(rings[-1]))).material_index = idx


def layers(base):
    """Stacked illustration layers: each 4.5 cm thick, 3 cm in front of the last, so each
    stands 1.5 cm proud of the one under it and sinks 1.5 cm into it."""
    return lambda k: (base + 0.03 * k, base + 0.03 * k + 0.045)


def strut(buf, p, q, t, mat):
    """A square bar from p to q (local 3D), overshooting both ends by t/2 so it penetrates."""
    p, q = Vector(p), Vector(q)
    d = q - p
    rot = d.to_track_quat("Z", "Y").to_matrix().to_4x4()
    buf.box(Matrix.Translation((p + q) / 2) @ rot, (t, t, d.length + t), mat)


def box(buf, c, size, mat):
    buf.box(Matrix.Translation(c), size, mat)


# ------------------------------------------------------------------ icons (flat frame)
# Each draws a picture about 2s across centred on (cx, cy), from layer L(0) upward.

def icon_bread(buf, cx, cy, s, L, acc):
    loaf = [(cx + 0.95 * s * math.cos(a), cy - 0.12 * s + 0.62 * s * math.sin(a))
            for a in (math.pi * i / 14 for i in range(15))]
    loaf += [(cx - 0.86 * s, cy - 0.45 * s), (cx + 0.86 * s, cy - 0.45 * s)]
    buf.prism(loaf, *L(0), "sign_crust")
    for dx in (-0.42, 0.0, 0.42):
        buf.prism(bar((cx + (dx - 0.1) * s, cy - 0.05 * s), (cx + (dx + 0.12) * s, cy + 0.33 * s), 0.13 * s),
                  *L(1), "sign_cream")


def icon_cup(buf, cx, cy, s, L, acc):
    buf.prism(rrect(cx, cy - 0.66 * s, 1.55 * s, 0.17 * s, 0.08 * s), *L(0), acc)
    ring(buf, cx + 0.5 * s, cy - 0.05 * s, 0.17 * s, 0.31 * s, *L(0), "white", -math.radians(85), math.radians(85), 20)
    body = [(cx - 0.56 * s, cy + 0.32 * s), (cx - 0.46 * s, cy - 0.44 * s), (cx - 0.3 * s, cy - 0.6 * s),
            (cx + 0.3 * s, cy - 0.6 * s), (cx + 0.46 * s, cy - 0.44 * s), (cx + 0.56 * s, cy + 0.32 * s)]
    buf.prism(body, *L(1), "white")
    buf.prism(rrect(cx, cy - 0.08 * s, 0.98 * s, 0.17 * s, 0.05 * s), *L(2), acc)
    for x, y, h in ((-0.2, 0.62, 0.2), (0.16, 0.74, 0.24)):
        buf.prism(ellipse(cx + x * s, cy + y * s, 0.085 * s, h * s, 16), *L(0), "white")


def icon_mortar(buf, cx, cy, s, L, acc):
    buf.prism(bar((cx - 0.05 * s, cy - 0.1 * s), (cx + 0.55 * s, cy + 0.85 * s), 0.2 * s, 0.26 * s), *L(0), "wood")
    bowl = ellipse(cx, cy - 0.05 * s, 0.72 * s, 0.72 * s, 16, math.pi, math.tau)
    buf.prism(bowl, *L(1), "white")
    buf.prism(rrect(cx, cy - 0.05 * s, 1.72 * s, 0.22 * s, 0.09 * s), *L(2), acc)
    buf.prism(rrect(cx, cy - 0.84 * s, 0.72 * s, 0.18 * s, 0.07 * s), *L(0), acc)


def icon_scissors(buf, cx, cy, s, L, acc):
    buf.prism(bar((cx + 0.36 * s, cy - 0.42 * s), (cx - 0.34 * s, cy + 0.98 * s), 0.22 * s, 0.05 * s), *L(0), "white")
    buf.prism(bar((cx - 0.36 * s, cy - 0.42 * s), (cx + 0.34 * s, cy + 0.98 * s), 0.22 * s, 0.05 * s), *L(1), "white")
    for side in (-1, 1):
        ring(buf, cx + side * 0.42 * s, cy - 0.62 * s, 0.13 * s, 0.28 * s, *L(2), "sign_ink")
    buf.prism(circle(cx, cy + 0.3 * s, 0.08 * s, 12), *L(3), acc)


def icon_bowl(buf, cx, cy, s, L, acc):
    buf.prism(bar((cx + 0.1 * s, cy - 0.2 * s), (cx + 0.72 * s, cy + 0.85 * s), 0.09 * s), *L(0), "wood")
    buf.prism(bar((cx + 0.28 * s, cy - 0.2 * s), (cx + 0.92 * s, cy + 0.72 * s), 0.09 * s), *L(0), "wood")
    for x, y, h in ((-0.45, 0.42, 0.18), (-0.12, 0.52, 0.22)):
        buf.prism(ellipse(cx + x * s, cy + y * s, 0.08 * s, h * s, 16), *L(0), "white")
    buf.prism(ellipse(cx, cy - 0.05 * s, 0.82 * s, 0.72 * s, 16, math.pi, math.tau), *L(1), "white")
    buf.prism(rrect(cx, cy - 0.3 * s, 1.34 * s, 0.14 * s, 0.05 * s), *L(2), acc)
    buf.prism(rrect(cx, cy - 0.8 * s, 0.62 * s, 0.16 * s, 0.06 * s), *L(0), "white")


ICONS = {"bread": icon_bread, "cup": icon_cup, "mortar": icon_mortar, "scissors": icon_scissors, "bowl": icon_bowl}


# ------------------------------------------------------------------ fascia

def fascia_sign(dest, text_, width, style="board", seed=0, at=None):
    """A shop-name board. Face along +Y, bottom centre at the origin on the wall (y = 0); see
    the module docstring. Returns the new objects (an empty list when merging into a Buf)."""
    rng = random.Random(seed)
    at = at if at is not None else Matrix.Identity(4)
    body, words, finish = _targets(dest, "sign_" + _slug(text_), bevel=0.014)
    if style == "board":
        H, r = 0.85, 0.17
        board_c, text_c, rim_c = BOARD_COMBOS[rng.randrange(len(BOARD_COMBOS))]
        cy = H / 2
        with _Frame(body, at), _Frame(body, FACE_Y):
            outline = [Vector((x, y, 0)) for x, y in rrect(0, cy, width - 0.06, H - 0.06, r)]
            body.sweep(outline, [(-0.11, -0.02), (0.03, -0.02), (0.03, 0.17), (-0.11, 0.17)], rim_c,
                       Vector((0, cy, 0)), closed=True)
            # Two painted planks, the lower one set 7 mm back, so the panel reads as timber.
            inner_w, inner_h = width - 0.2, H - 0.2
            body.prism(rrect(0, cy + inner_h / 4 - 0.005, inner_w, inner_h / 2 + 0.01, 0.06), -0.01, 0.12, board_c)
            body.prism(rrect(0, cy - inner_h / 4 + 0.005, inner_w, inner_h / 2 + 0.01, 0.06), -0.01, 0.113, board_c)
            for side in (-1, 1):
                body.cylinder(Vector((side * (width / 2 - 0.09), cy, 0.17)), 0.04, 0.035, "sign_iron", sides=12)
        with _Frame(words, at), _Frame(words, FACE_Y):
            text(words, text_, width - 0.46, H - 0.4, 0, cy, 0.105, text_c, extrude=0.01, bevel=0.006)
    elif style == "letters":
        colour = LETTER_COLOURS[rng.randrange(len(LETTER_COLOURS))]
        rail_h = 0.09
        with _Frame(words, at), _Frame(words, FACE_Y):
            tw, _ = text(words, text_, width * 0.94, 0.6, 0, rail_h - 0.02, 0.03, colour, extrude=0.03,
                         bevel=0.01, lines=1, anchor="bottom")
        with _Frame(body, at), _Frame(body, FACE_Y):
            body.prism(rrect(0, rail_h / 2, tw + 0.3, rail_h, 0.035), -0.02, 0.15, "sign_iron")
            for side in (-1, 1):   # end caps, a touch of colour on the iron
                body.prism(rrect(side * (tw / 2 + 0.15), rail_h / 2, 0.1, rail_h + 0.04, 0.03), -0.02, 0.17, colour)
    elif style == "lightbox":
        H = 0.8
        body_c, text_c, cap_c = LIGHTBOX_COMBOS[rng.randrange(len(LIGHTBOX_COMBOS))]
        lip_top, cap_bottom = 0.055, H - 0.04
        # The coloured body shows as a 17 cm border round the face: at 10 cm (zoo v3) the
        # sign read as a thin picture frame, not a box.
        face_cy, face_h = (lip_top + cap_bottom) / 2, cap_bottom - lip_top - 0.12
        with _Frame(body, at), _Frame(body, FACE_Y):
            body.prism(rrect(0, H / 2, width - 0.1, H - 0.02, 0.07), -0.02, 0.24, body_c)
            body.prism(rrect(0, H + 0.01, width, 0.1, 0.04), -0.02, 0.31, cap_c)
            body.prism(rrect(0, 0.02, width - 0.06, 0.07, 0.03), -0.02, 0.27, cap_c)
            body.prism(rrect(0, face_cy, width - 0.44, face_h, 0.05), 0.2, 0.255, "sign_cream")
        with _Frame(words, at), _Frame(words, FACE_Y):
            text(words, text_, width - 0.62, face_h - 0.1, 0, face_cy, 0.245, text_c, extrude=0.008,
                 bevel=0.005, font="tall")
    else:
        raise ValueError(f"unknown fascia style {style!r}")
    return finish()


# ------------------------------------------------------------------ blade sign

def _shield(w):
    pts = [(-w, 0.78 * w), (-0.35 * w, 0.78 * w), (0, 0.9 * w), (0.35 * w, 0.78 * w), (w, 0.78 * w)]
    for i in range(0, 11):
        t = i / 10
        pts.append((w * (1 - t * t), -1.1 * w * t))
    for i in range(9, -1, -1):
        t = i / 10
        pts.append((-w * (1 - t * t), -1.1 * w * t))
    # drop the duplicated bottom point
    return [p for i, p in enumerate(pts) if i == 0 or (abs(p[0] - pts[i - 1][0]) + abs(p[1] - pts[i - 1][1])) > 1e-6]


def blade_sign(dest, content, seed=0, at=None):
    """A double-sided hanging sign on an iron bracket. The arm leaves the wall (y = 0) along +Y
    at z = 0; see the module docstring. Returns the new objects."""
    rng = random.Random(seed)
    at = at if at is not None else Matrix.Identity(4)
    content = SHOP_ICON.get(content, content)
    body, words, finish = _targets(dest, "blade_" + _slug(content), bevel=0.012)
    disc_c, rim_c, acc = BLADE_COMBOS[rng.randrange(len(BLADE_COMBOS))]
    shape = rng.choice(("round", "round", "shield"))
    R = 0.5
    centre = Matrix.Translation((0, 0.72, -0.74))
    if shape == "round":
        poly, top = circle(0, 0, R, 40), math.sqrt(R * R - 0.28 ** 2)
    else:
        poly, top = _shield(R), 0.78 * R
    with _Frame(body, at):
        # Bracket: wall plate, square arm, a ball finial, a diagonal brace and a scroll.
        box(body, (0, 0.01, -0.2), (0.12, 0.06, 0.52), "sign_iron")
        box(body, (0, 0.62, 0), (0.05, 1.26, 0.06), "sign_iron")
        body.blob(Vector((0, 1.27, 0)), (0.055, 0.055, 0.055), "sign_iron", subdiv=2)
        strut(body, (0, 0.03, -0.42), (0, 0.66, -0.01), 0.04, "sign_iron")
        with _Frame(body, SIDE_A):
            ring(body, 0.3, -0.15, 0.1, 0.135, -0.017, 0.017, "sign_iron", n=24)
        # Two hangers from the arm into the top of the rim.
        for dy in (-0.28, 0.28):
            box(body, (0, 0.72 + dy, -(0.74 - top) / 2 + 0.02), (0.03, 0.03, 0.74 - top + 0.06), "sign_iron")
        with _Frame(body, centre), _Frame(body, SIDE_A):
            body.prism(poly, -0.035, 0.035, disc_c)
            body.sweep([Vector((x, y, 0)) for x, y in poly], [(-0.05, -0.055), (0.03, -0.055), (0.03, 0.055), (-0.05, 0.055)],
                       rim_c, Vector((0, 0, 0)), closed=True)
    icon = ICONS.get(content)
    L = layers(0.025)
    for side in (SIDE_A, SIDE_B):
        target = body if icon else words
        with _Frame(target, at), _Frame(target, centre), _Frame(target, side):
            if icon:
                icon(body, 0, 0.0 if shape == "round" else 0.03, 0.32, L, acc)
            else:
                text(words, content[:3], 0.56, 0.46, 0, 0.0, 0.025, acc if disc_c != "sign_cream" else "sign_red",
                     extrude=0.012, bevel=0.006, lines=1)
    return finish()


# ------------------------------------------------------------------ billboard

def art_soda(buf, cx, cy, s, L):
    buf.prism(star(cx, cy, 1.08 * s, 0.88 * s, 14), *L(0), "sign_gold")   # 1.18 crowded the slogan (v4)
    bottle = [(-0.42, -0.9), (-0.34, -0.98), (0.34, -0.98), (0.42, -0.9), (0.42, 0.2), (0.2, 0.5),
              (0.16, 0.86), (-0.16, 0.86), (-0.2, 0.5), (-0.42, 0.2)]
    buf.prism([(cx + x * s, cy + y * s) for x, y in bottle], *L(1), "sign_leaf")
    buf.prism(rrect(cx, cy + 0.9 * s, 0.44 * s, 0.16 * s, 0.05 * s), *L(2), "sign_red")
    buf.prism(rrect(cx, cy - 0.36 * s, 0.96 * s, 0.5 * s, 0.08 * s), *L(2), "sign_cream")
    buf.prism(circle(cx, cy - 0.36 * s, 0.15 * s, 20), *L(3), "sign_red")
    buf.prism(rrect(cx - 0.27 * s, cy + 0.12 * s, 0.08 * s, 0.34 * s, 0.035 * s), *L(2), "white")
    for x, y, r in ((0.72, 0.35, 0.11), (0.9, 0.72, 0.07), (-0.74, 0.12, 0.08), (0.66, -0.28, 0.06), (-0.66, 0.6, 0.06)):
        buf.prism(circle(cx + x * s, cy + y * s, r * s, 16), *L(2), "white")


def art_halo(buf, cx, cy, s, L):
    def hw(y):   # the glass's half width at height y (units of s)
        return 0.375 + (y + 0.7) / 1.25 * (0.55 - 0.375)
    glass = [(-0.375, -0.7), (0.375, -0.7), (0.55, 0.55), (-0.55, 0.55)]
    buf.prism([(cx + x * s, cy + y * s) for x, y in glass], *L(0), "white")
    for y0, y1, mat in ((-0.62, -0.3, "sign_red"), (-0.3, -0.02, "sign_cream"), (-0.02, 0.25, "sign_leaf"),
                        (0.25, 0.47, "sign_ube")):
        a, b = hw(y0) - 0.08, hw(y1) - 0.08
        buf.prism([(cx - a * s, cy + y0 * s), (cx + a * s, cy + y0 * s), (cx + b * s, cy + y1 * s),
                   (cx - b * s, cy + y1 * s)], *L(1), mat)
    buf.prism(rrect(cx, cy - 0.82 * s, 0.62 * s, 0.2 * s, 0.06 * s), *L(1), "sign_cream")
    buf.prism(rrect(cx + 0.3 * s, cy + 0.63 * s, 0.34 * s, 0.22 * s, 0.05 * s), *L(1), "sign_gold")
    buf.prism(bar((cx + 0.3 * s, cy + 0.45 * s), (cx + 0.8 * s, cy + 1.1 * s), 0.08 * s), *L(3), "white")
    buf.prism(ellipse(cx - 0.12 * s, cy + 0.5 * s, 0.4 * s, 0.4 * s, 16, 0, math.pi), *L(2), "sign_ube")
    buf.prism(circle(cx - 0.12 * s, cy + 0.93 * s, 0.1 * s, 16), *L(3), "sign_red")


def art_sun(buf, cx, cy, s, L):
    for k in range(12):
        a, da = k * math.tau / 12, math.radians(11)
        buf.prism([(cx + 0.7 * s * math.cos(a - da), cy + 0.7 * s * math.sin(a - da)),
                   (cx + 1.22 * s * math.cos(a), cy + 1.22 * s * math.sin(a)),
                   (cx + 0.7 * s * math.cos(a + da), cy + 0.7 * s * math.sin(a + da))], *L(0), "sign")
    buf.prism(circle(cx, cy, 0.8 * s, 36), *L(1), "sign_gold")
    for side in (-1, 1):
        buf.prism(circle(cx + side * 0.44 * s, cy - 0.16 * s, 0.13 * s, 16), *L(2), "sign_pink")
        buf.prism(ellipse(cx + side * 0.26 * s, cy + 0.2 * s, 0.075 * s, 0.13 * s, 16), *L(2), "sign_ink")
    ring(buf, cx, cy + 0.02 * s, 0.34 * s, 0.45 * s, *L(2), "sign_ink", math.radians(200), math.radians(340), 24)


# design -> (art, background, rim, slogan colour, slogan)
DESIGNS = {
    "soda": (art_soda, "sign_red", "sign_cream", "sign_cream", "PAMPALAMIG!"),
    "halo": (art_halo, "sign_mint", "sign_plum", "sign_plum", "HALO-HALO NA!"),
    "sun":  (art_sun, "sign_teal", "sign_gold", "sign", "MAGANDANG UMAGA!"),
}


def billboard(dest, seed=0, w=8.0, h=4.0, at=None, design=None):
    """A rooftop billboard. Face along +Y, bottom centre of the frame at the origin on the roof;
    see the module docstring. Returns the new objects."""
    rng = random.Random(seed)
    at = at if at is not None else Matrix.Identity(4)
    design = design or rng.choice(sorted(DESIGNS))
    art, bg, rim, ink, slogan = DESIGNS[design]
    body, words, finish = _targets(dest, "billboard_" + design, bevel=0.018)
    zb, top = 1.4, 1.4 + h
    board = Matrix.Translation((0, 0, zb)) @ FACE_Y
    with _Frame(body, at):
        with _Frame(body, board):
            outline = [Vector((x, y, 0)) for x, y in rrect(0, h / 2, w, h, 0.22)]
            body.sweep(outline, [(-0.14, -0.26), (0.05, -0.26), (0.05, 0.2), (-0.14, 0.2)], rim,
                       Vector((0, h / 2, 0)), closed=True)
            body.prism(rrect(0, h / 2, w - 0.2, h - 0.2, 0.14), -0.22, 0.03, bg)
            body.prism(rrect(0, h / 2, w - 0.3, h - 0.3, 0.1), -0.32, -0.18, "sign_iron")
            art(body, -0.26 * w, h / 2, 0.4 * h, layers(0.02))
        # The steel frame: two columns on base plates, two girders into the back of the board,
        # an X brace, kickers running back to their own plates.
        cols = (-0.32 * w, 0.32 * w)
        for x in cols:
            box(body, (x, -0.71, 0.03), (0.5, 0.5, 0.1), "metal")
            box(body, (x, -0.71, (zb + 0.85 * h) / 2), (0.22, 0.22, zb + 0.85 * h), "metal")
            box(body, (x, -2.2, 0.03), (0.4, 0.4, 0.1), "metal")
            strut(body, (x, -0.71, zb + 0.3 * h), (x, -2.2, 0.06), 0.14, "metal")
        for z in (zb + 0.25 * h, zb + 0.75 * h):
            box(body, (0, -0.44, z), (0.9 * w, 0.36, 0.18), "metal")
        strut(body, (cols[0], -0.71, 0.35), (cols[1], -0.71, zb - 0.15), 0.1, "metal")
        strut(body, (cols[1], -0.71, 0.35), (cols[0], -0.71, zb - 0.15), 0.1, "metal")
        # Catwalk on arms, and three gooseneck lamps over the top edge.
        box(body, (0, 0.22, 1.18), (w + 0.2, 1.05, 0.08), "sign_iron")
        for x in cols:
            box(body, (x, -0.04, 1.1), (0.12, 1.6, 0.12), "metal")
            strut(body, (x, -0.71, 0.55), (x, 0.55, 1.1), 0.1, "metal")
        for x in (-0.33 * w, 0.0, 0.33 * w):
            strut(body, (x, -0.34, top - 0.6), (x, -0.34, top + 0.45), 0.08, "sign_iron")
            strut(body, (x, -0.34, top + 0.45), (x, 0.78, top + 0.45), 0.08, "sign_iron")
            box(body, (x, 0.8, top + 0.39), (0.44, 0.3, 0.16), "sign_iron")
            box(body, (x, 0.8, top + 0.3), (0.34, 0.2, 0.04), "sign_cream")
    with _Frame(words, at), _Frame(words, board):
        text(words, slogan, 0.5 * w, 0.55 * h, 0.2 * w, h / 2, 0.02, ink, extrude=0.02, bevel=0.01)
    return finish()


# ------------------------------------------------------------------ preview

def _scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    K.USE_TEXTURES = True
    try:
        import author_kanto_city  # noqa: F401  (its palette: plaster, paving, glass)
        mats = "plaster_cream", "paving"
    except Exception as exc:   # the city script is edited by others; the preview must still run
        print("[signage] city palette unavailable:", exc)
        mats = "white", "ground"
    col = bpy.data.collections.new("signage_test")
    bpy.context.scene.collection.children.link(col)
    return col, mats


def _test_wall(col, mats, width, shops_x):
    """A plaster block 7 m tall and 8 m deep (so a billboard stands on a roof), with a teal
    shop window under every sign, and a paved ground."""
    wall = K.Buf("test_wall")
    box(wall, (0, -4, 3.5), (width, 8, 7), mats[0])
    for x in shops_x:
        box(wall, (x, -0.05, 1.35), (2.8, 0.12, 2.3), "glass")
    wall.finish(col, bevel=0.03)
    ground = K.Buf("test_ground")
    box(ground, (0, 4, -0.05), (width + 30, 40, 0.1), mats[1])
    ground.finish(col, bevel=0)


def _render(shots, version):
    scene = bpy.context.scene
    K.setup_lighting()
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    for label, pos, tgt, lens in shots:
        cam.location = pos
        cam.data.lens, cam.data.sensor_width, cam.data.sensor_fit = lens, 36, "HORIZONTAL"
        cam.rotation_euler = (Vector(tgt) - Vector(pos)).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = str(PREVIEWS / f"signage_{label}_v{version}.png")
        bpy.ops.render.render(write_still=True)
        print("[signage] preview", scene.render.filepath)


def preview(version, design="halo"):
    """The review wall: four fascia signs (three styles), two blade signs, one billboard."""
    col, mats = _scene()
    _test_wall(col, mats, 18, (-6.6, -2.2, 2.2, 6.6))
    up = Matrix.Translation
    for i, (x, name, style) in enumerate(((-6.6, "SARI-SARI STORE", "board"), (-2.2, "PANADERIA", "letters"),
                                          (2.2, "BOTIKA", "lightbox"), (6.6, "KARINDERYA", "board"))):
        fascia_sign(col, name, 3.6, style, seed=i + 3, at=up((x, 0, 2.72)))
    blade_sign(col, "PANADERIA", seed=1, at=up((-4.4, 0, 4.6)))
    blade_sign(col, "BARBERO", seed=4, at=up((4.4, 0, 4.6)))
    billboard(col, seed=0, design=design, at=up((0, -1.0, 7.0)))
    eye_lens = 18 / math.tan(math.radians(47.5))   # the game's 95 degree horizontal view
    _render((("front", (0, 28, 6.6), (0, 0, 6.6), 45),
             ("eye", (1.5, 12, 1.25), (0, 0, 4.2), eye_lens),
             ("close", (-7.5, 6.5, 3.6), (-3.6, 0, 3.6), 40),
             ("rear", (9, -12, 13), (0, -1.5, 9.5), 35)), version)


def zoo(version):
    """Everything at once, for checking: every shop name, every icon, every billboard design.
    Blade signs are turned to face the camera here; on a street they face along it."""
    col, mats = _scene()
    xs = [-17.5 + 5 * i for i in range(8)]
    _test_wall(col, mats, 42, xs)
    up = Matrix.Translation
    styles = ("board", "letters", "lightbox")
    for i, name in enumerate(SHOP_NAMES):
        x, z = xs[i % 8], 2.72 if i < 8 else 5.2
        fascia_sign(col, name, 4.2, styles[i % 3], seed=i, at=up((x, 0, z)))
    turn = Matrix.Rotation(math.radians(90), 4, "Z")
    for i, content in enumerate(list(ICONS) + ["TP"]):
        blade_sign(col, content, seed=i, at=up((-15 + 5 * i, 1.6, 4.6)) @ turn)
    for i, d in enumerate(sorted(DESIGNS)):
        billboard(col, seed=i, design=d, at=up((-13 + 13 * i, -1.0, 7.0)))
    _render((("zoo", (0, 46, 7), (0, 0, 7), 40),
             ("zoo_blades", (-3, 9, 4.2), (-3, 0, 4.0), 30),
             ("zoo_boards", (0, 30, 13), (0, 0, 10), 40)), version)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    design = argv[argv.index("--design") + 1] if "--design" in argv else "halo"
    if "--preview" in argv:
        preview(int(argv[argv.index("--preview") + 1]), design)
    if "--zoo" in argv:
        zoo(int(argv[argv.index("--zoo") + 1]))


if __name__ == "__main__":
    main()
