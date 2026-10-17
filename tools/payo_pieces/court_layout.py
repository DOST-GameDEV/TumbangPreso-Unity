"""PAYO court platform: the shared layout of its ONE atlas (pure Python, read by court_paint.py and court_build.py).

The platform is about 90 x 120 m of stone and 66 m tall, far too big for one unique atlas, so its atlas is a TRIM
SHEET: every row band runs the full width of the picture and tiles in U (the sampler repeats), and a face's V stays
inside its row. Vertical faces are cut into 6 m bands at fixed heights, each band mapped onto one wall row, shifted
along U by a different amount so no two bands look alike. The long soft light-to-dark gradient of the CALM rules
(Logs/payo/pieces/CALM_STYLE.md, rule 1) runs over the WHOLE height of a wall, so it is carried by the vertex colour
(COLOR_0, the Payo kit's usual `vertex_colour: true`) and not by the rows, which stay even in value.

Atlas pixels: x right, y DOWN (image rows). Blender / glTF conversion is done by the builder.
"""
SIZE = 2048
PPM = 64.0                       # pixels per metre everywhere in the atlas
PERIOD = SIZE / PPM              # 32 m: the U repeat of every tiling row
GUT = 8                          # gutter rows above and below a tiling row, filled with its own wrap

# tiling rows: (name, y0, content height px). Content starts at y0 + GUT.
PAVE = (0, 384)                  # 6 m of paving (two rows of 3 m slabs), tiles in U and V
WALL_TOP = (400, 384)            # the top 6 m band of every wall: plain course, the carved fret course, plain courses
WALL = (800, 384)                # every other 6 m band: plain big courses
TRIM = 1200                      # the copings, row band 1200..1650 (no V tiling: profiles)
PATCH = 1650                     # 1650..2048: non tiling patches

COURSE = 1.2                     # wall course height, metres
BAND = 6.0                       # vertical band height, metres (5 courses)
WALL_TOP_Z = -6.3                # the top of every wall face (under the copings); bands are counted down from here
FRET = (1.2, 2.4)                # the fret course inside WALL_TOP, metres below its top

# the copings' profiles, painted in TRIM, one row per profile face (metres tall), stacked with 3 px between
#   outer coping (parapet and retaining-wall cap): border course | cap underside in | cap inner face | cap top
#   | cap outer face (the woven band) | cap underside out.  The body faces under the cap use the wall rows.
#   pit rim lip: (border shared) | inner face | top | outer face | underside
#   string course: the pale band round the outside at the pit floor's level (top 0.1, face 0.6, under 0.1)
CAP_T = 0.45
_ROWS = [("border", 0.8), ("cap_under_in", 0.15), ("cap_in", CAP_T), ("cap_top", 1.35), ("cap_out", CAP_T),
         ("cap_under_out", 0.15), ("lip_in", 0.35), ("lip_top", 1.05), ("lip_out", 0.65), ("lip_under", 0.2),
         ("string", 0.8)]
ROWS = {}
_y = TRIM + 4
for _n, _m in _ROWS:
    ROWS[_n] = (_y, _m)                      # (first pixel row, metres): the face's V runs y .. y + m * PPM
    _y += int(round(_m * PPM)) + 3
assert _y <= PATCH, _y

# non tiling patches: (x0, y0, w px, h px)
PLAIN = (8, PATCH + 8, 560, 380)            # quoins, piers, frames, spouts, niche jambs: pale plain stone
NICHE_BACK = (600, PATCH + 8, 168, 310)     # 2.6 x 4.8 m: a dusty rose panel, one bold turquoise stepped diamond
SPOUT_PANEL = (800, PATCH + 8, 168, 168)    # 2.6 x 2.6 m: a carved disc, turquoise ring (the water mark)
GLYPH_PANEL = (1000, PATCH + 8, 168, 168)   # 2.6 x 2.6 m: the retaining walls' panel, a bold sun in ochre and red
DARK = (1200, PATCH + 8, 400, 380)          # niche and spout interiors, recess soffit: the same stone, plain


def wall_band(z):
    """The 6 m band a height falls in, counted down from WALL_TOP_Z (negative above it)."""
    import math
    return int(math.floor((WALL_TOP_Z - z) / BAND + 1e-6))


BAND_SHIFT = 7.37                            # metres of U shift per band so stacked bands never repeat
