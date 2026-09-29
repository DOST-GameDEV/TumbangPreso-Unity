"""The Ilalim ng Tulay sign kit: every shop sign on the east side of Taft (ILALIM-1.3, east kit).

  py -3 tools/author_ilalim_textures_eastside.py      # paints every sign face listed in SIGNS
  (the geometry is built by tools/author_ilalim_eastside.py, which imports this module)

This module is two things:
  * SIGNS, the ONE table of every sign: which of the eleven sign systems it uses, its size,
    its words and its colours. The texture script paints each face from its row
    (ArtSource/ilalim/textures/east_sign_<key>.png) and the eastside script builds the body
    for it. It imports without Blender, so the texture script can read the table.
  * build(key, Buf, collection, matrix): the chunky Blender body for that row's system, with the
    painted face on it, placed by `matrix`.

THE RULES (docs/Ilalim_Ng_Tulay.md section 10.4, KANTO_DESIGN_GUIDE.md section 11.1):
  * Every name is HAND-NAMED and original: a person or family plus the trade, the way Manila
    shops are named ("KARINDERYA NI ALING DORY", "JOVEN'S XEROX"). No real brand or chain.
  * ONE recorded exception: PC Express, the hero storefront facing the overclock pad, carries
    its official red and blue mark on a lightbox (the owner's brand exception, Ilalim_Ng_Tulay.md
    section 8.3). Its registered-mark badge is left off, as on the real storefront. Nothing else
    uses those colours.
  * THE ELEVEN SYSTEMS, each with its own silhouette, mounting, material and letter style:
      lightbox        deep framed box on brackets, acrylic face     the traced mark / clean sans
      fascia          thick timber frame, painted board             hand-painted, wide, heavy
      blade           tall narrow panel projecting on two brackets  italic, condensed, runs up
      aboard          two leaning chalkboards on the pavement       chalky script
      placard         small enamel plate on concrete                stencil
      tarp            printed cloth, slight sag, tied at 4 corners   thin and wide
      pylon           double-sided box on its own post              wide, heavy
      hung            small panel on two drop rods under an awning  condensed
      painted         no plate: paint on the facade itself          very wide, weathered
      banner          tall narrow cloth between two wall bands      condensed, letters stacked
      tin             corrugated sheet nailed to a post frame       uneven hand-painted
    No two neighbouring businesses draw from the same system (the order along the row is in
    tools/author_ilalim_eastside.py, SHOP_ROW).
  * Role hues (Art_Direction.md section 1): nothing near offence orange #f87020 or defence blue
    #0080e8. Reds are maroon and brick, greens are bottle and teal-green, yellows are mustard.
  * CHUNKY: thick rounded frames and posts, no thin hardware, the lettering lives in the painted
    face. Every layer penetrates the one it sits on by 1 to 2 cm.

THE FRAME every builder works in: +Z up, the wall is the plane y = 0 with the building at y < 0,
and the face looks along +Y. The origin is the bottom centre of the sign ON the wall. Seen from
+Y the text reads along -X, so a face's u runs along -X. A sign on the east shop row (whose
wall faces the court, -X in the world) is placed with a +90 degree turn about Z.
"""
import math

try:                      # the texture script imports the table without Blender
    import bmesh          # noqa: F401
    from mathutils import Matrix, Vector
except ImportError:       # pragma: no cover
    Matrix = Vector = None


# ------------------------------------------------------------------ the table
# key: system, w and h (metres, the painted face), lines [(text, relative height)], and the
# palette bg (field), fg (letters), accent (rules, borders, icons). `extra` carries per-system
# options. Keep names under ~22 characters on a face under 6 m: 0.3 m letters read across the
# road at the game's eye.
SIGNS = {
    # The hero storefront: the official mark, the one brand exception.
    "pcx": dict(system="lightbox", w=5.0, h=1.15, lines=[], mark="pcx",
                bg="f4f1ea", fg="1b1b1b", accent="1b1b1b"),
    # West East Center podium, south to north.
    "cell": dict(system="hung", w=2.3, h=0.62,
                 lines=[("RAMOS CELLPHONE REPAIR", 1.0), ("E-LOAD  •  CASE  •  CHARGER", 0.55)],
                 bg="f2ecd8", fg="264a3f", accent="8a2f36", extra={"drop": 0.05}),
    "cell_board": dict(system="aboard", w=0.62, h=0.86,
                       lines=[("BUKAS!", 1.0), ("REPAIR", 0.62), ("UNLOCK", 0.62), ("E-LOAD", 0.62)],
                       bg="2c3a33", fg="efeadb", accent="e8d27a"),
    "print": dict(system="fascia", w=6.0, h=0.80,
                  lines=[("JOVEN'S XEROX", 1.0), ("PRINT • BOOKBIND • THESIS • ID PHOTO", 0.40)],
                  bg="efd98f", fg="6a1e24", accent="2e4a3c"),
    "eatery": dict(system="tarp", w=5.4, h=1.05,
                   lines=[("KARINDERYA NI ALING DORY", 1.0), ("ULAM  •  SILOG  •  PARES  •  SABAW", 0.46)],
                   bg="fbf3de", fg="9c2227", accent="3f7a3c", extra={"icon": "bowl"}),
    "dental": dict(system="lightbox", w=3.4, h=0.82,
                   lines=[("DRA. REYES", 1.0), ("DENTAL CLINIC  •  2F", 0.5)],
                   bg="f5f2ea", fg="7a2230", accent="2f5d50"),
    "review": dict(system="tarp", w=6.6, h=1.05,
                   lines=[("BAUTISTA REVIEW CENTER", 1.0), ("NURSING  •  NLE  •  IELTS  •  ENROLL NA! 4F", 0.5)],
                   bg="f6f0dc", fg="2a4d3a", accent="a02a36", extra={"icon": "cap"}),
    # The Astral Tower podium, south to north.
    "dorm_a": dict(system="placard", w=0.66, h=0.48, lines=[("BEDSPACE", 1.0), ("FOR RENT", 0.8)],
                   bg="f1eee4", fg="8e2328", accent="8e2328"),
    "dorm_b": dict(system="placard", w=0.66, h=0.48, lines=[("ROOM", 1.0), ("NEAR UP-PGH", 0.62)],
                   bg="e9d77a", fg="1f2a24", accent="1f2a24"),
    "dorm_c": dict(system="placard", w=0.66, h=0.48, lines=[("LADIES DORM", 0.8), ("WITH AIRCON", 0.7)],
                   bg="d9e5d6", fg="233f33", accent="233f33"),
    "pisonet": dict(system="painted", w=6.0, h=1.28,
                    lines=[("PISONET NI KUYA JUN", 1.0), ("1 PISO = 5 MINUTO  •  PRINT  •  WIFI", 0.42)],
                    bg="ede4c9", fg="2f5a45", accent="8a2f36"),
    "botika": dict(system="blade", w=0.86, h=2.7, lines=[("BOTIKA LUZ", 1.0), ("MEDICAL SUPPLY", 0.42)],
                   bg="2f6b4f", fg="f5f0e0", accent="f5f0e0", extra={"icon": "cross"}),
    "scrubs": dict(system="banner", w=0.78, h=3.3, lines=[("SCRUBS", 1.0), ("UNIFORM • BP • STETHO", 0.3)],
                   bg="f1ecdf", fg="6e2350", accent="2f6b4f"),
    # Round the corner on Padre Faura, under the tower.
    "lugaw": dict(system="fascia", w=5.2, h=0.8,
                  lines=[("LUGAWAN NI MANG TOTOY", 1.0), ("GOTO • ARROZ CALDO • TOKWA'T BABOY", 0.42)],
                  bg="e3ecd9", fg="2d4b2f", accent="7a2a2e"),
    # The fried-chicken place in the old KFC building: an invented Filipino business.
    "manok_pylon": dict(system="pylon", w=2.6, h=1.9,
                        lines=[("MANOK NI", 0.8), ("MANG CARDING", 1.0), ("UNLI RICE", 0.6)],
                        bg="f3e7c4", fg="8f2126", accent="3a5a2a"),
    "manok_fascia": dict(system="fascia", w=8.0, h=1.15,
                         lines=[("PRITONG MANOK NI MANG CARDING", 1.0), ("CRISPY  •  MAY SABAW  •  24 ORAS", 0.42)],
                         bg="8f2126", fg="f6e7b8", accent="e0c160"),
    # Vista GL Taft's shops and the vulcanizing shed by its drive.
    "laundry": dict(system="hung", w=2.6, h=0.66,
                    lines=[("LABAHAN NI ATE MARLENE", 1.0), ("WASH • DRY • FOLD • PER KILO", 0.5)],
                    bg="dcebe6", fg="2b4a55", accent="7a3a52"),
    "siomai": dict(system="blade", w=0.8, h=2.2, lines=[("SIOMAI NI TATA BEN", 1.0), ("SIOPAO • GULAMAN", 0.42)],
                   bg="e8cf62", fg="5a1c1c", accent="5a1c1c", extra={"icon": "steam"}),
    "vulcan": dict(system="tin", w=1.9, h=1.05, lines=[("VULCANIZING", 1.0), ("KA OSCAR", 0.75)],
                   bg="c9c6bd", fg="a3242a", accent="1f1f1f"),
}

SYSTEMS = ("lightbox", "fascia", "blade", "aboard", "placard", "tarp", "pylon", "hung", "painted",
           "banner", "tin")

# The sign kit's own materials: (texture, tint, normal strength, tile) as the eastside
# MATERIALS dict takes them. Faces are fixed-UV (0..1 over the face) and get one material each.
BODY_MATERIALS = {
    "east_sign_metal":  ("east_tin", (0.22, 0.22, 0.23), 0.3, (2.0, 2.0)),
    "east_sign_timber": ("east_timber", None, 0.3, (1.5, 1.5)),
    "east_sign_post":   ("east_tin", (0.30, 0.33, 0.30), 0.3, (2.0, 2.0)),
    "east_sign_rope":   (None, (0.62, 0.58, 0.48), 0, None),
    "east_sign_steel":  ("east_tin", (0.55, 0.55, 0.53), 0.3, (2.0, 2.0)),
    "east_sign_tinsheet": ("east_tin", (0.80, 0.80, 0.78), 0.5, (2.0, 2.0)),
}


def face_material(key):
    return f"east_sign_{key}"


# ------------------------------------------------------------------ builders

def _rr(hw, hd, r):
    import author_ilalim_lrt as L
    return L.rounded_rect(hw, hd, r)


def _face(buf, key, x0, x1, z0, z1, y, back=False, sag=None, rows=1, cols=1):
    """The painted face: a grid of quads on the plane y (facing +Y, or -Y when `back`), u running
    along -X so the words read from the front (and along +X on a back face, so they read from
    behind too). `sag(u, v)` may push each vertex along Y (cloth bulge)."""
    mat = face_material(key)
    verts = []
    for j in range(rows + 1):
        row = []
        for i in range(cols + 1):
            u, v = i / cols, j / rows
            x = x1 - u * (x1 - x0) if not back else x0 + u * (x1 - x0)
            dy = sag(u, v) if sag else 0.0
            row.append((Vector((x, y + (dy if not back else -dy), z0 + v * (z1 - z0))), (u, v)))
        verts.append(row)
    for j in range(rows):
        for i in range(cols):
            quad = [verts[j][i], verts[j][i + 1], verts[j + 1][i + 1], verts[j + 1][i]]
            buf.quad([q[0] for q in quad], mat, [q[1] for q in quad])


def lightbox(buf_cls, key, spec, depth=0.34):
    """A deep framed box standing 12 cm off the wall on four chunky stubs: a dark rounded return,
    and the acrylic face 1.5 cm proud of it."""
    w, h = spec["w"], spec["h"]
    y0 = 0.12
    body = buf_cls(f"sign_{key}_box")
    frame = [(x, z) for x, z in _rr(w / 2 + 0.09, h / 2 + 0.09, 0.08)]
    body.extrude_y([(x, z + h / 2 + 0.09) for x, z in frame], y0, y0 + depth, "east_sign_metal")
    for sx in (-1, 1):
        for zz in (0.3, 0.7):
            z = 0.09 + zz * h
            body.tube([Vector((sx * (w / 2 - 0.5), -0.03, z)), Vector((sx * (w / 2 - 0.5), y0 + 0.04, z))],
                      0.05, "east_sign_metal", sides=8)
    face = buf_cls(f"sign_{key}_face")
    _face(face, key, -w / 2, w / 2, 0.09, h + 0.09, y0 + depth + 0.015)
    return [(body, 0.03), (face, 0)]


def fascia(buf_cls, key, spec):
    """A painted board inside a thick rounded timber frame, wall-flush."""
    w, h = spec["w"], spec["h"]
    body = buf_cls(f"sign_{key}_frame")
    outer = _rr(w / 2 + 0.13, h / 2 + 0.13, 0.1)
    inner = _rr(w / 2 - 0.01, h / 2 - 0.01, 0.05)
    body.frame_y(outer, inner, -0.02, 0.16, "east_sign_timber", zc=h / 2 + 0.13)
    board = buf_cls(f"sign_{key}_board")
    board.extrude_y([(x, z + h / 2 + 0.13) for x, z in _rr(w / 2 + 0.02, h / 2 + 0.02, 0.04)], -0.02, 0.09,
                    "east_sign_timber")
    _face(board, key, -w / 2, w / 2, 0.13, h + 0.13, 0.098)
    return [(body, 0.03), (board, 0.01)]


def blade(buf_cls, key, spec):
    """A tall narrow double-sided panel standing out from the wall on two chunky brackets. Origin on
    the wall; the panel spans y 0.35..0.35+w and z 0..h."""
    w, h = spec["w"], spec["h"]
    y0 = 0.35
    body = buf_cls(f"sign_{key}_blade")
    prof = [(x, z) for x, z in _rr(0.08, h / 2 + 0.06, 0.05)]
    # the panel is a thick slab in the X-Z plane, swept along Y (out of the wall)
    body.extrude_y([(x, z + h / 2) for x, z in prof], y0 - 0.06, y0 + w + 0.06, "east_sign_metal")
    for z in (h * 0.18, h * 0.82):
        body.tube([Vector((0, -0.04, z)), Vector((0, y0 + 0.02, z))], 0.05, "east_sign_metal", sides=8)
    face = buf_cls(f"sign_{key}_faces")
    # Faces on both sides (x = +/-0.092), painted with the words running UP the blade; each side's
    # u runs to the viewer's right, so both read.
    m, uvs = face_material(key), [(0, 0), (1, 0), (1, 1), (0, 1)]
    face.quad([Vector((0.092, y0, 0.0)), Vector((0.092, y0 + w, 0.0)), Vector((0.092, y0 + w, h)),
               Vector((0.092, y0, h))], m, uvs)
    face.quad([Vector((-0.092, y0 + w, 0.0)), Vector((-0.092, y0, 0.0)), Vector((-0.092, y0, h)),
               Vector((-0.092, y0 + w, h))], m, uvs)
    return [(body, 0.025), (face, 0)]


def aboard(buf_cls, key, spec):
    """Two chalkboards leaning together on the pavement, joined at the top. Origin on the ground
    at the board's centre; the faces look along +Y and -Y."""
    w, h = spec["w"], spec["h"]
    lean = math.radians(11)
    body = buf_cls(f"sign_{key}_aboard")
    face = buf_cls(f"sign_{key}_aboard_face")
    for s in (-1, 1):
        rot = Matrix.Rotation(s * lean, 4, "X")
        top = Matrix.Translation((0, 0, h + 0.12))
        xf = top @ rot @ Matrix.Translation((0, s * 0.03, -(h + 0.12)))
        outer = _rr(w / 2 + 0.06, (h + 0.12) / 2, 0.05)
        inner = _rr(w / 2 - 0.01, h / 2 - 0.01, 0.03)
        zc = (h + 0.12) / 2
        body.frame_y(outer, inner, -0.025, 0.025, "east_sign_timber", zc=zc, xform=xf)
        body.extrude_y([(x, z + zc) for x, z in _rr(w / 2 + 0.01, h / 2 + 0.01, 0.02)],
                       -0.010, 0.010, "east_sign_timber", xform=xf)
        n = len(face.bm.verts)
        _face(face, key, -w / 2 + 0.01, w / 2 - 0.01, zc - h / 2 + 0.01, zc + h / 2 - 0.01, 0.016 * s, back=(s < 0))
        face.transform_new(n, xf)
    return [(body, 0.015), (face, 0)]


def placard(buf_cls, key, spec):
    """A small enamel plate with rounded corners, sunk into the concrete it is fixed to."""
    w, h = spec["w"], spec["h"]
    body = buf_cls(f"sign_{key}_plate")
    body.extrude_y([(x, z + h / 2) for x, z in _rr(w / 2 + 0.02, h / 2 + 0.02, 0.05)], -0.015, 0.02, "east_sign_steel")
    _face(body, key, -w / 2, w / 2, 0.0, h, 0.026)
    return [(body, 0.008)]


def tarp(buf_cls, key, spec):
    """Printed cloth with a slight belly, tied by ropes at four corners to hooks in the wall."""
    w, h = spec["w"], spec["h"]
    cloth = buf_cls(f"sign_{key}_tarp")
    sag = lambda u, v: 0.05 + 0.05 * math.sin(math.pi * u) * math.sin(math.pi * v) - 0.025 * math.sin(math.pi * u) * (1 - v)
    _face(cloth, key, -w / 2, w / 2, 0.0, h, 0.0, sag=sag, rows=6, cols=12)
    cloth.solidify_back(0.012)
    ties = buf_cls(f"sign_{key}_ties")
    for x in (-w / 2 - 0.02, w / 2 + 0.02):
        for z in (-0.02, h + 0.02):
            hook = Vector((x + math.copysign(0.14, x), -0.03, z + (0.1 if z > 0 else -0.1)))
            ties.tube([Vector((x - math.copysign(0.03, x), 0.05, z)), hook], 0.018, "east_sign_rope", sides=6)
            ties.blob(hook, (0.045, 0.045, 0.045), "east_sign_metal")
    return [(cloth, 0), (ties, 0)]


def pylon(buf_cls, key, spec, post_h=3.4):
    """A double-sided box on its own post, standing on a footing. Origin on the ground; the
    faces look along +Y and -Y."""
    w, h = spec["w"], spec["h"]
    body = buf_cls(f"sign_{key}_pylon")
    body.extrude_z(_rr(0.3, 0.3, 0.1), -0.1, 0.14, "east_sign_post")
    body.extrude_z(_rr(0.16, 0.16, 0.06), 0.1, post_h + 0.1, "east_sign_post", top_scale=0.92)
    box = _rr(w / 2 + 0.1, h / 2 + 0.1, 0.1)
    body.extrude_y([(x, z + post_h + h / 2) for x, z in box], -0.22, 0.22, "east_sign_metal")
    body.extrude_y([(x, z + post_h + h + 0.17) for x, z in _rr(w / 2 + 0.16, 0.09, 0.05)], -0.26, 0.26, "east_sign_metal")
    face = buf_cls(f"sign_{key}_pylon_face")
    for s in (1, -1):
        _face(face, key, -w / 2, w / 2, post_h, post_h + h, s * 0.235, back=(s < 0))
    return [(body, 0.03), (face, 0)]


def hung(buf_cls, key, spec):
    """A small panel on two drop rods under an awning. Origin at the TOP of the rods, where they
    pass into the awning; the panel hangs below and faces +Y."""
    w, h = spec["w"], spec["h"]
    drop = spec.get("extra", {}).get("drop", 0.34)
    body = buf_cls(f"sign_{key}_hung")
    for s in (-1, 1):
        body.tube([Vector((s * (w / 2 - 0.25), 0, 0.08)), Vector((s * (w / 2 - 0.25), 0, -drop - 0.06))],
                  0.022, "east_sign_metal", sides=6)
    top = -drop
    body.extrude_y([(x, z + top - h / 2) for x, z in _rr(w / 2 + 0.05, h / 2 + 0.05, 0.05)], -0.035, 0.035,
                   "east_sign_steel")
    face = buf_cls(f"sign_{key}_hung_face")
    for s in (1, -1):
        _face(face, key, -w / 2, w / 2, top - h, top, s * 0.041, back=(s < 0))
    return [(body, 0.012), (face, 0)]


def painted(buf_cls, key, spec):
    """No plate at all: the words are painted on the facade. A decal 1.5 cm proud of the wall, its
    painted field faded at the edges (alpha in the face texture)."""
    w, h = spec["w"], spec["h"]
    face = buf_cls(f"sign_{key}_paint")
    _face(face, key, -w / 2, w / 2, 0.0, h, 0.015)
    return [(face, 0)]


def banner(buf_cls, key, spec):
    """A tall narrow printed cloth between two wall bands (rods), bellied a little."""
    w, h = spec["w"], spec["h"]
    cloth = buf_cls(f"sign_{key}_banner")
    sag = lambda u, v: 0.07 + 0.05 * math.sin(math.pi * v) * math.sin(math.pi * u)
    _face(cloth, key, -w / 2, w / 2, 0.0, h, 0.0, sag=sag, rows=10, cols=3)
    cloth.solidify_back(0.01)
    rods = buf_cls(f"sign_{key}_rods")
    for z in (-0.02, h + 0.02):
        rods.tube([Vector((-w / 2 - 0.1, 0.08, z)), Vector((w / 2 + 0.1, 0.08, z))], 0.03, "east_sign_metal", sides=8)
        for s in (-1, 1):
            rods.tube([Vector((s * (w / 2 + 0.06), 0.08, z)), Vector((s * (w / 2 + 0.06), -0.04, z))], 0.03,
                      "east_sign_metal", sides=8)
    return [(cloth, 0), (rods, 0.01)]


def tin(buf_cls, key, spec):
    """A corrugated tin sheet, a little crooked, nailed to a two-post timber frame. Origin on the
    ground at the frame's centre; the sheet faces +Y."""
    w, h = spec["w"], spec["h"]
    frame = buf_cls(f"sign_{key}_frame")
    base = 0.9
    for s in (-1, 1):
        frame.extrude_z(_rr(0.06, 0.06, 0.02), -0.05, base + h + 0.25, "east_sign_timber",
                        offset=(s * (w / 2 - 0.12), -0.05), lean=(s * 0.02, 0.0))
    for z in (base + 0.15, base + h - 0.15):
        frame.extrude_y([(x, zz + z) for x, zz in _rr(w / 2 + 0.05, 0.05, 0.02)], -0.12, -0.02, "east_sign_timber")
    sheet = buf_cls(f"sign_{key}_sheet")
    wave = lambda u, v: 0.018 * math.sin(u * w / 0.076 * math.tau) + 0.03 * v * u
    _face(sheet, key, -w / 2, w / 2, base, base + h, 0.0, sag=wave, rows=2, cols=max(8, int(w / 0.038)))
    sheet.solidify_back(0.006, mat="east_sign_tinsheet")
    tilt = Matrix.Rotation(math.radians(2.5), 4, "Y")
    sheet.transform_all(Matrix.Translation((0, 0, base + h / 2)) @ tilt @ Matrix.Translation((0, 0, -(base + h / 2))))
    return [(frame, 0.012), (sheet, 0)]


BUILDERS = {"lightbox": lightbox, "fascia": fascia, "blade": blade, "aboard": aboard, "placard": placard,
            "tarp": tarp, "pylon": pylon, "hung": hung, "painted": painted, "banner": banner, "tin": tin}


def build(key, buf_cls, collection, matrix):
    """Build SIGNS[key] as objects in `collection`, placed by `matrix`. Returns the objects."""
    spec = SIGNS[key]
    objs = []
    for buf, bevel in BUILDERS[spec["system"]](buf_cls, key, spec):
        o = buf.finish(collection, bevel=bevel, segments=2, local=True)
        o.matrix_world = matrix
        objs.append(o)
    return objs
