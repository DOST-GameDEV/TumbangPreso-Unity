"""Paint the textures for Ilalim ng Tulay's LRT-1 train and road vehicles (ILALIM-1.3, train and
vehicle kit), in the house illustrated style.

  py -3 tools/author_ilalim_textures_train.py [--sheet N]

Writes ArtSource/ilalim/textures/train_*.png and veh_*.png, and a swatch sheet
Logs/ilalim-blender/train_swatches_vN.png. The models are tools/author_ilalim_train.py and
tools/author_ilalim_vehicles.py. It imports the drawing helpers of tools/author_ilalim_textures.py
(coat, flat, smooth, hexcol) read-only; every surface here has its own drawing.

THE STYLE (KANTO_DESIGN_GUIDE.md section 3, LAGOON_REWORK_GUIDE.md section 2): flat fills, a few
LARGE feathered patches, low contrast, no grain, no noise, no streaks, no airbrushed blur.

TILING SURFACES (4 m tiles, world-scale UVs):
  * train_body: the lemon-to-mustard yellow of the cars (never toward offence orange #f87020).
    Big sun-faded paler patches, and a few duller ones where the paint was touched up.
  * train_band: the deep navy window band (never near defence blue #0080e8). Long soft washes
    stretched along the car, as road dust and sun leave on a dark paint.
  * train_door: teal-grey door leaves. One soft sheen, and a duller wear zone per door that the
    model's grime overlays do not reach.
  * train_roof: pale warm grey roof with LARGE dark soot patches, stretched along the car. The
    roof is the dirtiest surface of an LRT-1 car (brake and pantograph dust).
  * train_under: the dark underframe and bogies, with rust-brown brake-dust patches.
  * train_glass: near-black teal with one broad soft paler band: a sky reflection.
  * train_rubber: the bellows between cars: warm charcoal with broad soft dust patches.
  * veh_paint: NEUTRAL light coat for every vehicle body; each material tints it. Big faded
    patches only, so a jeepney's paint looks sun-baked, not new.
  * veh_chrome: NEUTRAL bright coat with one broad soft dark horizon band, the one drawn mark
    of polished stainless steel on a jeepney.
  * veh_glass: vehicle windows, a vertical gradient (darker at the top, as a car's glass reads
    under a roof), different from the train's diagonal sky band.
  * veh_rubber: tyres, warm black with a soft pale dust ring toward the lower edge.

GRIME OVERLAYS (multipliers, white = clean), mapped by the models' UVGrime and UVSplash maps:
  * train_drips: 8 m wide, 2 m down from the car's roof gutter. A narrow soft band under the
    gutter, and SHORT narrow brown tongues. Different from the guideway's grime_drips, which
    are long grey concrete stains.
  * train_skirt: 8 m wide, 1.2 m up from the body's lower edge. A brown dust band with a ragged
    soft top, and low fans where wheels throw dust up.
  * veh_dust: 8 m wide, 1.2 m up from the road. Road dust on a vehicle's lower body.

PAINTED PANELS (0..1 UVs on a thin panel): the destination signs and the hand-painted liveries.
Hand lettered in the jeepney sign-painter's manner: flat letters, a darker drop shadow, a
painted border, no gradients. Every name is invented; station and street names are real
(Baclaran, Fernando Poe Jr., Divisoria, Quiapo, Lawton, Taft, Padre Faura).
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
import author_ilalim_textures as T        # noqa: E402  (read-only helpers)

OUT = T.OUT
SHEETS = T.SHEETS
SIZE = T.SIZE
X, Y = T.X, T.Y
coat, flat, smooth, hexcol = T.coat, T.flat, T.smooth, T.hexcol
FONTS = Path("C:/Windows/Fonts")


def save(name, albedo):
    OUT.mkdir(parents=True, exist_ok=True)
    albedo = np.clip(albedo, 0, 1)
    Image.fromarray((albedo * 255).astype(np.uint8)).save(OUT / f"{name}_albedo.png")
    print("[ilalim-train-tex]", name)


# ------------------------------------------------------------------ train tiling surfaces

def train_body():
    img = flat("e2b83a")
    # Sun fade: broad paler, slightly greyer patches.
    img = coat(img, (1.045, 1.04, 1.10), 1.5, 0.30, seed=701, feather=1.3)
    # Touched-up paint: a few duller, warmer patches, smaller.
    img = coat(img, (0.95, 0.93, 0.88), 0.8, 0.14, seed=702, feather=1.0)
    save("train_body", img)


def train_band():
    img = flat("2a3854")
    img = coat(img, (1.10, 1.09, 1.06), 1.3, 0.26, seed=711, feather=1.6, stretch=(0.35, 1.0))
    img = coat(img, (0.93, 0.93, 0.95), 0.9, 0.18, seed=712, feather=1.2)
    save("train_band", img)


def train_door():
    img = flat("4f6a6e")
    img = coat(img, (1.06, 1.06, 1.05), 1.0, 0.24, seed=721, feather=1.4)
    img = coat(img, (0.93, 0.92, 0.90), 0.5, 0.16, seed=722, feather=0.9)
    save("train_door", img)


def train_roof():
    img = flat("bdb9b0")
    # The soot: big soft patches stretched along the car (u is across, v along on a roof).
    img = coat(img, (0.80, 0.78, 0.75), 1.3, 0.34, seed=731, feather=1.3, stretch=(1.0, 0.45))
    img = coat(img, (0.90, 0.87, 0.82), 0.8, 0.22, seed=732, feather=1.0)
    img = coat(img, (1.03, 1.03, 1.02), 0.9, 0.12, seed=733, feather=1.0)
    save("train_roof", img)


def train_under():
    img = flat("403c37")
    img = coat(img, (1.18, 1.02, 0.86), 1.0, 0.28, seed=741, feather=1.2)
    img = coat(img, (0.88, 0.88, 0.88), 0.7, 0.18, seed=742, feather=1.0)
    save("train_under", img)


def train_glass():
    img = flat("1e2b2d")
    # One broad soft diagonal sky band per tile.
    # X + Y keeps the band continuous across the tile's edges (a 0.6 slope left a seam).
    d = ((X + Y) % 4.0) / 4.0
    band = np.exp(-((d - 0.5) / 0.14) ** 2)
    img = img * (1 + 0.28 * band[..., None])
    save("train_glass", img)


def train_rubber():
    img = flat("35322f")
    img = coat(img, (1.12, 1.10, 1.06), 1.0, 0.25, seed=751, feather=1.2)
    save("train_rubber", img)


# ------------------------------------------------------------------ vehicle tiling surfaces

def veh_paint():
    img = flat("dcdcdc")
    img = coat(img, (1.05, 1.05, 1.05), 1.2, 0.30, seed=801, feather=1.3)
    img = coat(img, (0.95, 0.95, 0.95), 0.7, 0.18, seed=802, feather=1.0)
    save("veh_paint", img)


def veh_chrome():
    img = flat("d8d8d8")
    # ONE broad soft dark horizon band per tile, the drawn reflection of the street on polished
    # steel. On the vehicles v is height, so it sits about 0.9 m up round the whole jeepney.
    # (v1 drew a thin line every metre and read as ruled streaks.)
    wave = 0.06 * np.sin(X * np.pi / 2)
    h = (Y + wave) % 4.0
    # Image row 0 is the TOP of the tile (v = 1), so 0.9 m up the vehicle is 3.1 m down the image.
    band = np.clip(1 - np.abs(h - 3.1) / 0.28, 0, 1)
    band = band * band * (3 - 2 * band)
    img = img * (1 - 0.30 * band[..., None])
    img = coat(img, (1.04, 1.04, 1.04), 1.0, 0.25, seed=811, feather=1.2)
    save("veh_chrome", img)


def veh_glass():
    img = flat("1f2e2e")
    # A soft rise and fall with height, continuous (v1's sawtooth drew a hard step every metre).
    t = 0.5 - 0.5 * np.cos(Y * np.pi)
    img = img * (0.88 + 0.20 * t[..., None])
    save("veh_glass", img)


def veh_rubber():
    img = flat("2b2a28")
    img = coat(img, (1.18, 1.15, 1.10), 0.4, 0.20, seed=821, feather=0.8)
    save("veh_rubber", img)


# ------------------------------------------------------------------ grime overlays

def _noise1d(n, scale_px, seed):
    return T._noise1d(n, scale_px, seed)


def _mult(name, a):
    T._mult(name, a)


def train_drips():
    W_M, H_M, ppm = 8.0, 2.0, 200
    w, h = int(W_M * ppm), int(H_M * ppm)
    v, u = np.mgrid[0:h, 0:w] / ppm
    a = np.ones((h, w, 3))
    edge = 0.10 + 0.03 * _noise1d(w, 0.6 * ppm, 901)[None, :]
    band = np.clip((edge - v) / 0.05 + 0.5, 0, 1)
    a *= 1 - 0.12 * band[..., None]
    rng = np.random.default_rng(902)
    brown = np.array([0.84, 0.80, 0.74])
    for k in range(22):
        u0 = rng.uniform(0, W_M)
        width = rng.uniform(0.04, 0.14)
        length = rng.uniform(0.25, 1.1)
        phase = rng.uniform(0, 6.28)
        du = np.abs(((u - u0 - 0.02 * np.sin(v * 4 + phase) + W_M / 2) % W_M) - W_M / 2)
        taper = np.clip(1 - v / length, 0, 1) ** 0.6
        half = width * (0.35 + 0.65 * taper)
        mask = np.clip((half - du) / 0.018 + 0.5, 0, 1) * (v < length)
        mask *= rng.uniform(0.35, 0.7)
        a *= 1 - mask[..., None] * (1 - brown)
    _mult("train_drips", a)


def train_skirt():
    W_M, H_M, ppm = 8.0, 1.2, 200
    w, h = int(W_M * ppm), int(H_M * ppm)
    v, u = np.mgrid[0:h, 0:w] / ppm
    a = np.ones((h, w, 3))
    top = 0.30 + 0.08 * _noise1d(w, 0.5 * ppm, 911)[None, :]
    band = np.clip((top - v) / 0.08 + 0.5, 0, 1)
    dust = np.array([0.80, 0.74, 0.66])
    a *= 1 - band[..., None] * (1 - dust)
    rng = np.random.default_rng(912)
    fans = np.zeros((h, w))
    for k in range(5):
        u0 = k * W_M / 5 + rng.uniform(-0.4, 0.4)
        width = rng.uniform(0.45, 0.8)
        height = rng.uniform(0.45, 0.65)
        du = ((u - u0 + W_M / 2) % W_M) - W_M / 2
        # A low fan: a half ellipse of dust thrown up from the wheels, flat-filled. Fans are
        # merged (max), so overlaps never stack into darker rings (v1 read as bubbles).
        r = (du / width) ** 2 + (v / height) ** 2
        fans = np.maximum(fans, np.clip((1 - r) / 0.12 + 0.5, 0, 1))
    a *= 1 - 0.4 * fans[..., None] * (1 - np.array([0.88, 0.84, 0.78]))
    _mult("train_skirt", a)


def veh_dust():
    W_M, H_M, ppm = 8.0, 1.2, 200
    w, h = int(W_M * ppm), int(H_M * ppm)
    v, u = np.mgrid[0:h, 0:w] / ppm
    a = np.ones((h, w, 3))
    top = 0.42 + 0.10 * _noise1d(w, 0.35 * ppm, 921)[None, :]
    band = np.clip((top - v) / 0.10 + 0.5, 0, 1)
    a *= 1 - band[..., None] * (1 - np.array([0.76, 0.70, 0.62]))
    rng = np.random.default_rng(922)
    for k in range(12):
        u0 = rng.uniform(0, W_M)
        width = rng.uniform(0.05, 0.16)
        length = rng.uniform(0.15, 0.4)
        du = np.abs(((u - u0 + W_M / 2) % W_M) - W_M / 2)
        # Drawn upward tongues from the band: rain splash dried on the paint.
        vv = v - top
        half = width * np.clip(1 - vv / length, 0, 1) ** 0.5
        mask = np.clip((half - du) / 0.015 + 0.5, 0, 1) * (vv > -0.05) * (vv < length) * rng.uniform(0.3, 0.6)
        a *= 1 - mask[..., None] * (1 - np.array([0.86, 0.82, 0.76]))
    _mult("veh_dust", a)


# ------------------------------------------------------------------ painted panels

def _font(name, size):
    for f in (name, "ariblk.ttf", "impact.ttf", "arialbd.ttf"):
        p = FONTS / f
        if p.exists():
            return ImageFont.truetype(str(p), size)
    return ImageFont.load_default()


class Panel:
    """A painted board drawn at 2x and reduced once, so letter edges are clean but not blurred."""

    def __init__(self, w, h, bg):
        self.w, self.h, self.s = w, h, 2
        self.img = Image.new("RGB", (w * 2, h * 2), bg)
        self.d = ImageDraw.Draw(self.img)

    def rect(self, box, fill, radius=0, outline=None, width=0):
        s = self.s
        self.d.rounded_rectangle([c * s for c in box], radius=radius * s, fill=fill, outline=outline, width=width * s)

    def ellipse(self, box, fill):
        self.d.ellipse([c * self.s for c in box], fill=fill)

    def poly(self, pts, fill):
        self.d.polygon([(x * self.s, y * self.s) for x, y in pts], fill=fill)

    def text(self, xy, s, font, size, fill, shadow=None, shadow_off=(4, 4), stroke=0, stroke_fill=None,
             anchor="mm", shear=0.0, fit=None):
        """Letters with a flat drop shadow. `fit` = max width in px: the size shrinks to fit.
        `shear` slants the letters (a painted italic)."""
        size = int(size)
        f = _font(font, size * self.s)
        if fit:
            while f.getlength(s) > fit * self.s and size > 8:
                size -= 2
                f = _font(font, size * self.s)
        layer = Image.new("L", self.img.size, 0)
        ld = ImageDraw.Draw(layer)
        x, y = xy[0] * self.s, xy[1] * self.s
        ld.text((x, y), s, font=f, fill=255, anchor=anchor, stroke_width=stroke * self.s)
        if shear:
            layer = layer.transform(layer.size, Image.AFFINE, (1, shear, -shear * y, 0, 1, 0), resample=Image.BILINEAR)
        if shadow:
            sh = Image.new("L", layer.size, 0)
            sh.paste(layer, (shadow_off[0] * self.s, shadow_off[1] * self.s))
            self.img.paste(shadow, (0, 0), sh)
        if stroke:
            self.img.paste(stroke_fill, (0, 0), layer)
            inner = Image.new("L", self.img.size, 0)
            ImageDraw.Draw(inner).text((x, y), s, font=f, fill=255, anchor=anchor)
            if shear:
                inner = inner.transform(inner.size, Image.AFFINE, (1, shear, -shear * y, 0, 1, 0), resample=Image.BILINEAR)
            layer = inner
        self.img.paste(fill, (0, 0), layer)

    def save(self, name):
        OUT.mkdir(parents=True, exist_ok=True)
        self.img.resize((self.w, self.h), Image.LANCZOS).save(OUT / f"{name}_albedo.png")
        print("[ilalim-train-tex]", name)


def scallops(p, y, x0, x1, r, fill):
    """A row of painted half-circle scallops along a border, a jeepney panel staple."""
    n = int((x1 - x0) / (2 * r))
    step = (x1 - x0) / n
    for k in range(n):
        cx = x0 + (k + 0.5) * step
        p.ellipse((cx - r, y - r, cx + r, y + r), fill)


# The train's destination blinds: near-black glass, pale lemon LED letters (never amber, which
# would drift toward offence orange).
def train_dest(name, text):
    p = Panel(1024, 256, (22, 24, 26))
    p.rect((10, 10, 1014, 246), (30, 33, 36), radius=18)
    p.text((512, 128), text, "impact.ttf", 170, (243, 228, 140), fit=940)
    p.save(name)


# The route strip on each car's side above the doors, painted on the navy band.
def train_route():
    p = Panel(1024, 128, (42, 56, 84))
    p.text((512, 64), "BACLARAN  -  FERNANDO POE JR.", "arialbd.ttf", 64, (238, 226, 192), fit=960)
    p.save("train_route")


def jeep_side(name, bg, border, ink, shadow, route, via, accent):
    p = Panel(2048, 256, bg)
    p.rect((6, 6, 2042, 250), None, radius=24, outline=border, width=14)
    scallops(p, 240, 60, 1988, 11, accent)
    # Swooshes at both ends: the painted "wings" of a jeepney side panel.
    for sgn, x in ((1, 40), (-1, 2008)):
        p.poly([(x, 40), (x + sgn * 190, 70), (x + sgn * 240, 128), (x + sgn * 190, 186), (x, 216),
                (x + sgn * 90, 128)], accent)
    p.text((1024, 100), route, "impact.ttf", 140, ink, shadow=shadow, shadow_off=(6, 6), fit=1500)
    p.text((1024, 196), via, "arialbd.ttf", 40, ink, fit=1100)
    p.save(name)


def jeep_board(name, bg, border, ink, shadow, text, star):
    p = Panel(1024, 256, bg)
    p.rect((8, 8, 1016, 248), None, radius=30, outline=border, width=16)
    for x in (70, 954):
        p.poly([(x, 80), (x + 18, 116), (x + 58, 120), (x + 26, 144), (x + 36, 184), (x, 162), (x - 36, 184),
                (x - 26, 144), (x - 58, 120), (x - 18, 116)], star)
    p.text((512, 128), text, "georgiab.ttf", 118, ink, shadow=shadow, shadow_off=(5, 5), fit=740)
    p.save(name)


def jeep_low(name, bg, ink, shadow, text, accent):
    p = Panel(1024, 192, bg)
    p.rect((4, 4, 1020, 188), None, radius=20, outline=accent, width=10)
    p.text((512, 96), text, "FRSCRIPT.TTF", 140, ink, shadow=shadow, shadow_off=(4, 4), fit=900)
    p.save(name)


def bus_side():
    p = Panel(2048, 320, (122, 22, 26))
    p.rect((0, 20, 2048, 46), (228, 180, 58))
    p.rect((0, 274, 2048, 300), (228, 180, 58))
    p.text((880, 150), "Buenaventura", "georgiab.ttf", 190, (246, 236, 208), shadow=(60, 10, 12),
           shadow_off=(7, 7), shear=-0.22, fit=1380)
    p.text((1790, 168), "LINER", "impact.ttf", 110, (228, 180, 58), shadow=(60, 10, 12), shadow_off=(5, 5))
    p.save("veh_bus_side")


def bus_route():
    p = Panel(2048, 160, (238, 229, 205))
    p.text((1024, 80), "AIRCON   *   BACLARAN - LAWTON - QUIAPO   *   via TAFT", "arialbd.ttf", 88,
           (122, 22, 26), fit=1950)
    p.save("veh_bus_route")


def uv_windscreen():
    p = Panel(1024, 160, (240, 238, 228))
    p.text((380, 80), "UV Express", "georgiab.ttf", 100, (32, 46, 78), fit=620)
    p.text((850, 56), "BACLARAN -", "arialbd.ttf", 40, (32, 46, 78))
    p.text((850, 108), "QUIAPO", "arialbd.ttf", 40, (32, 46, 78))
    p.save("veh_uv_windscreen")


def uv_side():
    p = Panel(1024, 256, (236, 236, 230))
    p.rect((0, 0, 1024, 26), (120, 22, 28))
    p.text((512, 110), "UV EXPRESS SERVICE", "impact.ttf", 110, (32, 46, 78), fit=960)
    p.text((512, 205), "TAFT - P. FAURA - LAWTON", "arialbd.ttf", 50, (120, 22, 28), fit=900)
    p.save("veh_uv_side")


def taxi_door():
    p = Panel(1024, 320, (232, 232, 226))
    p.rect((0, 30, 1024, 70), (40, 110, 70))
    p.text((512, 160), "SALVACION", "impact.ttf", 120, (40, 110, 70), fit=900)
    p.text((512, 262), "TAXI  -  TX 218", "arialbd.ttf", 62, (40, 40, 40))
    p.save("veh_taxi_door")


PAINTERS = [train_body, train_band, train_door, train_roof, train_under, train_glass, train_rubber,
            veh_paint, veh_chrome, veh_glass, veh_rubber]
OVERLAYS = [train_drips, train_skirt, veh_dust]


def panels():
    train_dest("train_dest_baclaran", "BACLARAN")
    train_dest("train_dest_fpj", "FERNANDO POE JR.")
    train_route()
    # The Taft jeepney: stainless silver body, mustard panels with navy letters.
    jeep_side("veh_jeep_taft_side", (226, 184, 60), (34, 46, 74), (34, 46, 74), (120, 26, 30),
              "BACLARAN - DIVISORIA", "via TAFT  -  P. FAURA  -  LAWTON", (120, 26, 30))
    jeep_board("veh_jeep_taft_board", (34, 46, 74), (226, 184, 60), (226, 184, 60), (12, 14, 20),
               "MAGKAPATID", (238, 228, 200))
    jeep_low("veh_jeep_taft_low", (226, 184, 60), (120, 26, 30), (34, 46, 74), "Lola Enchang", (34, 46, 74))
    # The second jeepney: bottle green, cream panels with maroon letters.
    jeep_side("veh_jeep_green_side", (238, 228, 200), (24, 84, 50), (120, 24, 28), (24, 84, 50),
              "PASAY - QUIAPO", "via TAFT  -  UN AVE  -  MABINI", (24, 84, 50))
    jeep_board("veh_jeep_green_board", (120, 24, 28), (238, 228, 200), (238, 228, 200), (40, 8, 10),
               "HARI NG TAFT", (226, 184, 60))
    jeep_low("veh_jeep_green_low", (238, 228, 200), (24, 84, 50), (120, 24, 28), "Anak ni Mang Tonyo", (120, 24, 28))
    bus_side()
    bus_route()
    uv_windscreen()
    uv_side()
    taxi_door()


def swatch_sheet(version):
    SHEETS.mkdir(parents=True, exist_ok=True)
    names = [p.__name__ for p in PAINTERS]
    cell = 300
    cols = 6
    rows = (len(names) + cols - 1) // cols
    decals = ["train_dest_baclaran", "train_dest_fpj", "train_route", "veh_jeep_taft_side", "veh_jeep_taft_board",
              "veh_jeep_taft_low", "veh_jeep_green_side", "veh_jeep_green_board", "veh_jeep_green_low",
              "veh_bus_side", "veh_bus_route", "veh_uv_windscreen", "veh_uv_side", "veh_taxi_door"]
    over = ["train_drips", "train_skirt", "veh_dust"]
    W = cols * (cell + 20) + 20
    H = rows * (cell + 50) + 40 + len(over) * 170 + ((len(decals) + 1) // 2) * 330 + 40
    sheet = Image.new("RGB", (W, H), (245, 242, 235))
    draw = ImageDraw.Draw(sheet)
    for k, name in enumerate(names):
        tile = Image.open(OUT / f"{name}_albedo.png").convert("RGB")
        if name.startswith("veh_") and name in ("veh_paint", "veh_chrome", "veh_rubber"):
            a = np.asarray(tile) / 255 * (hexcol("c9a640") if name == "veh_paint" else hexcol("b0b4b4"))
            if name == "veh_rubber":
                a = np.asarray(tile) / 255
            tile = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
        rep = Image.new("RGB", (SIZE * 2, SIZE * 2))
        for i in range(2):
            for j in range(2):
                rep.paste(tile, (i * SIZE, j * SIZE))
        rep = rep.resize((cell, cell), Image.LANCZOS)
        x, y = 20 + (k % cols) * (cell + 20), 20 + (k // cols) * (cell + 50)
        sheet.paste(rep, (x, y))
        draw.text((x, y + cell + 8), f"{name} (2x2, 8 m)", fill=(40, 40, 40))
    y = 20 + rows * (cell + 50)
    base = np.asarray(Image.open(OUT / "train_body_albedo.png").convert("RGB"), dtype=float) / 255
    for name in over:
        o = np.asarray(Image.open(OUT / f"{name}.png").convert("RGB"), dtype=float) / 255
        h, w = o.shape[:2]
        reps = np.tile(base, (h // SIZE + 1, w // SIZE + 1, 1))[:h, :w]
        shown = np.clip(reps * o, 0, 1)
        if name == "train_drips":
            shown = np.flipud(shown)
        im = Image.fromarray((shown * 255).astype(np.uint8))
        im = im.resize((W - 40, int((W - 40) * h / w)), Image.LANCZOS)
        sheet.paste(im, (20, y))
        draw.text((20, y + im.size[1] + 2), f"{name} over train_body", fill=(40, 40, 40))
        y += im.size[1] + 20
    y += 10
    half = (W - 60) // 2
    row_h = 0
    for k, name in enumerate(decals):
        im = Image.open(OUT / f"{name}_albedo.png").convert("RGB")
        im = im.resize((half, max(20, int(half * im.size[1] / im.size[0]))), Image.LANCZOS)
        x = 20 + (k % 2) * (half + 20)
        sheet.paste(im, (x, y))
        row_h = max(row_h, im.size[1]) if k % 2 else im.size[1]
        if k % 2 == 1:
            y += row_h + 16
    path = SHEETS / f"train_swatches_v{version}.png"
    sheet.save(path)
    print("[ilalim-train-tex] sheet", path)


def main():
    for paint in PAINTERS + OVERLAYS:
        paint()
    panels()
    version = int(sys.argv[sys.argv.index("--sheet") + 1]) if "--sheet" in sys.argv else 1
    swatch_sheet(version)


if __name__ == "__main__":
    main()
