"""Paint the Ilalim ng Tulay TREES AND PLANTING kit's textures, in the house illustrated style.

  py -3 tools/author_ilalim_textures_trees.py [--sheet N]

Writes ArtSource/ilalim/textures/tree_*.png and a review sheet to
Logs/ilalim-blender/tree_swatches_vN.png. Every file carries the `tree_` prefix. The models are
built by tools/author_ilalim_trees.py, which reads these files.

THE METHOD IS SETTLED, SO THIS SCRIPT ONLY DRAWS NEW SHAPES IN THE OLD HAND.
  * Leaf cards are Kanto's (docs/KANTO_DESIGN_GUIDE.md section 5, `leaf()` in
    tools/author_kanto_textures.py): ONE soft-painted greyscale drawing with a crisp alpha
    silhouette, tinted by its material, two tints a plant (light on top, dark underneath).
    Inside the silhouette the value rises from the stem (dark) to the tip (light), with one soft
    sunlit patch and only a whisper of a midrib. NO outline: "sharply outlined and not softly
    painted" was rejected.
  * Bark is VARIANT J, settled by the owner ("BARK IS SETTLED ON VARIANT J ... do not reopen
    it"). tree_bark is painted by the same J recipe, with the same numbers, from
    author_kanto_textures (its _streaks_j, patches and flat), at Kanto's 2 m tile. Species
    differ by a material TINT on it, never by a new bark drawing.
  * Every OTHER surface gets its own drawing (LAGOON_REWORK_GUIDE.md section 2: "never
    repurpose one texture's generator for another surface").

WHAT IS DRAWN, AND WHY (the species were chosen from photographs of Taft, Padre Faura and the
UP Manila campus; the Commons links are in the model script's docstring):
  tree_leaf_raintree  RAIN TREE (acacia, Samanea saman), the huge umbrella trees over PGH and the
                      Supreme Court: a feathery spray, a rachis with five pairs of chunky oblique
                      leaflets and one at the tip, the leaflets growing toward the tip as the real
                      ones do. Many sprays make the canopy's lacy, layered read.
  tree_leaf_narra     NARRA (Pterocarpus indicus), the national tree: a drooping spray of seven
                      alternate, egg-shaped leaflets with a short drawn-out tip.
  tree_leaf_mango     MANGO: the star-burst of long, narrow leaves that mango twigs end in (the
                      photo behind the TAFT AVE. / PADRE FAURA ST. blades), six to seven leaves
                      fanning from one point.
  tree_leaf_fig       The Padre Faura kerb trees: a small-leaved fig (balete, Ficus benjamina),
                      three small pointed leaves on a twig. Dense round crowns.
  tree_frond          MANILA PALM (Adonidia merrillii) and royal palm: a pinnate frond, broader
                      and stiffer leaflets than the Lagoon's coconut, combing forward, closing to
                      a point inside the card (the Lagoon lesson: never clip a tip at the edge).
  tree_lily_leaf      SPIDER LILY (Hymenocallis / Crinum) of the Taft median planter: a long
                      strap with a soft channel down it.
  tree_lily_flower    Its flower head, in colour: white spidery petals, drawn THICK so they stay
                      chunky, round a pale cup with a green-yellow throat.
  tree_shrub_leaf     SANTAN (Ixora) shrub: a pair of glossy oblong leaves.
  tree_ixora          Its flower ball: many small four-lobed florets, greyscale, tinted a deep
                      crimson far from offence orange #f87020 (Art_Direction.md section 1).
  tree_hedge_leaf     KAMUNING (Murraya) hedge: a sprig of small round leaves.
  tree_palm_trunk     The palms' trunk: pale grey with soft ring scars, each a wobbly feathered
                      band, two value steps at most. 2 m a tile, colour.
  tree_crownshaft     The smooth green crownshaft under a Manila palm's fronds: flat green with
                      broad soft vertical washes.
  tree_pit_soil       The soil in a kerb tree pit: flat dark earth with a few big soft patches of
                      dry dust and fallen leaves.
  tree_pit_kerb       The pit's concrete kerb: warm grey with two soft coats.
Positional grime and paint on the trunks, as overlays on the trunk's second UV map
(v = metres above the ground / 2):
  tree_splash         a multiplier: the soil splash band at the foot, ragged soft top, 2 m tall.
  tree_limewash       RGBA: the WHITE LIME PAINT on the lower trunk of Manila's street and campus
                      trees, a very Filipino detail. The paint's top edge wobbles and is feathered,
                      and it is dirtied toward the ground. A = where the paint is.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
import author_kanto_textures as KT          # noqa: E402  the bark J recipe, read-only

OUT = ROOT / "ArtSource" / "ilalim" / "textures"
SHEETS = ROOT / "Logs" / "ilalim-blender"


def _save_rgba(name, g, alpha):
    """A greyscale leaf drawing, tinted later by its material."""
    OUT.mkdir(parents=True, exist_ok=True)
    g = np.clip(g * 0.92, 0, 1)
    rgba = np.dstack([g, g, g, np.clip(alpha, 0, 1)])
    Image.fromarray((rgba * 255).astype(np.uint8), "RGBA").save(OUT / f"{name}_albedo.png")
    print("[tree-tex]", name)


def _save_colour(name, rgb, alpha=None):
    OUT.mkdir(parents=True, exist_ok=True)
    rgb = np.clip(rgb, 0, 1)
    if alpha is None:
        Image.fromarray((rgb * 255).astype(np.uint8), "RGB").save(OUT / f"{name}_albedo.png")
    else:
        rgba = np.dstack([rgb, np.clip(alpha, 0, 1)])
        Image.fromarray((rgba * 255).astype(np.uint8), "RGBA").save(OUT / f"{name}_albedo.png")
    print("[tree-tex]", name)


def _normal(name, h, strength):
    gy, gx = np.gradient(h * strength * 8)
    n = np.dstack((-gx, gy, np.ones_like(h)))
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    Image.fromarray(((n * 0.5 + 0.5) * 255).astype(np.uint8)).save(OUT / f"{name}_normal.png")


def _grid(w, h, width_units=1.0):
    """Card coordinates: X across (-width/2 .. width/2), Y from the base (0) up the card, in the
    same units, so a circle drawn here is a circle on the card."""
    v, u = np.mgrid[0:h, 0:w].astype(np.float32)
    scale = width_units / (w - 1)
    return (u - (w - 1) / 2) * scale, (h - 1 - v) * scale


def _smooth(shape, scale_px, seed):
    """Periodic smooth noise (features about scale_px across), unit variance."""
    white = np.random.default_rng(seed).standard_normal(shape)
    fy = np.fft.fftfreq(shape[0])[:, None]
    fx = np.fft.fftfreq(shape[1])[None, :]
    n = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-(fx ** 2 + fy ** 2) * scale_px * scale_px * 2)))
    return (n - n.mean()) / (n.std() + 1e-9)


class Card:
    """Draws soft leaves onto one card: each leaf adds its silhouette to the alpha and its own
    stem-to-tip value to the tone where it is on top. Leaves drawn later lie over earlier ones."""

    def __init__(self, w, h, width_units=1.0):
        self.X, self.Y = _grid(w, h, width_units)
        self.px = (w - 1) / width_units
        self.alpha = np.zeros_like(self.X)
        self.tone = np.zeros_like(self.X)

    def leaf(self, base, angle, length, width, bend=0.0, round_=0.6, point=0.85, light=0.0, taper_tip=0.25):
        """One leaf from `base` along `angle` (radians from +Y, positive to the right). `bend`
        curves it sideways over its length. The half-width profile is Kanto's leaf: widest a
        little below the middle, a blunt point at the tip."""
        bx, by = base
        ca, sa = np.cos(angle), np.sin(angle)
        dx, dy = self.X - bx, self.Y - by
        along = dx * sa + dy * ca
        s = along / length
        across = dx * ca - dy * sa - bend * length * np.clip(s, 0, 1) ** 2
        sc = np.clip(s, 0, 1)
        half = width / 2 * np.clip(np.sin(np.pi * sc ** point), 0, 1) ** round_ * (1 - taper_tip * sc)
        d = half - np.abs(across)
        a = np.clip(d * self.px / 2.5, 0, 1) * (s > 0) * (s < 1)
        xn = across / np.maximum(half, 1e-4)
        tone = 0.72 + 0.26 * sc ** 0.8                                          # stem dark to tip light
        tone = tone + 0.12 * np.exp(-(((xn + 0.4) / 0.6) ** 2 + ((sc - 0.6) / 0.22) ** 2))   # sunlit patch
        tone = tone + 0.035 * np.exp(-(xn / 0.12) ** 2) * (sc > 0.1)             # whisper of a midrib
        tone = tone - 0.06 * np.clip(1 - np.abs(d) / (0.25 * width), 0, 1) * (sc < 0.45)
        tone = tone + light
        top = a > 0.5
        self.tone = np.where(top, tone, np.where(self.alpha < 0.5, tone, self.tone))
        self.alpha = np.maximum(self.alpha, a)

    def stalk(self, pts, width, tone=0.7):
        """A soft stalk or rachis along a polyline, under the leaves drawn after it."""
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            dx, dy = x1 - x0, y1 - y0
            L2 = dx * dx + dy * dy
            t = np.clip(((self.X - x0) * dx + (self.Y - y0) * dy) / L2, 0, 1)
            dist = np.hypot(self.X - (x0 + t * dx), self.Y - (y0 + t * dy))
            a = np.clip((width / 2 - dist) * self.px / 2.0, 0, 1)
            self.tone = np.where(a > self.alpha, tone, self.tone)
            self.alpha = np.maximum(self.alpha, a)


# ------------------------------------------------------------------ leaf cards

def leaf_raintree(size=512):
    """A rain tree spray, bipinnate as Samanea's leaf is: a short stalk forking into two pinnae,
    each with six pairs of small, chunky, oblique leaflets, larger toward the tip. It gives the
    rain tree's canopy its fine, feathery, layered read next to the narra's big ovals. Every
    leaflet stays inside the card (the Lagoon lesson: never clip at the edge)."""
    rng = np.random.default_rng(11)
    c = Card(size, size, 1.0)
    c.stalk([(0.0, 0.0), (0.0, 0.16)], 0.018, 0.62)
    for side in (-1, 1):
        a0 = side * np.radians(24)
        at = lambda t: (np.sin(a0) * 0.72 * t + side * 0.04 * t * t, 0.15 + np.cos(a0) * 0.72 * t)
        c.stalk([at(k / 4) for k in range(5)], 0.012, 0.64)
        for k in range(6):
            t = 0.12 + k * 0.15
            L = 0.085 + 0.05 * k / 5
            for s2 in (-1, 1):
                ang = a0 + s2 * np.radians(rng.uniform(55, 66))
                c.leaf(at(t), ang, L * 1.35, L * 0.8, bend=s2 * 0.05, round_=0.5, point=0.75, taper_tip=0.15)
        c.leaf(at(1.0), a0, 0.13, 0.1, round_=0.5, point=0.75, taper_tip=0.15)
    _save_rgba("tree_leaf_raintree", c.tone, c.alpha)


def leaf_narra(w=384, h=640):
    """A narra spray: a drooping rachis with seven alternate egg-shaped leaflets, each with a short
    drawn-out tip (point > 1 makes the tip narrower)."""
    rng = np.random.default_rng(12)
    c = Card(w, h, 0.6)
    rach = [(0.0, 0.02), (0.01, 0.35), (0.0, 0.75), (-0.02, 0.95)]
    c.stalk(rach, 0.012, 0.66)
    for k in range(6):
        t = 0.15 + k * 0.13
        side = -1 if k % 2 else 1
        L = 0.26 + 0.03 * np.sin(np.pi * k / 5)
        c.leaf((0.0, t), side * np.radians(rng.uniform(48, 58)), L, L * 0.62, round_=0.5, point=1.2,
               taper_tip=0.35)
    c.leaf((-0.02, 0.8), np.radians(-3), 0.2, 0.13, round_=0.5, point=1.2, taper_tip=0.35)
    _save_rgba("tree_leaf_narra", c.tone, c.alpha)


def leaf_mango(size=512):
    """A mango whorl: six or seven long narrow leaves fanning up from one point near the base."""
    rng = np.random.default_rng(13)
    c = Card(size, size, 1.0)
    base = (0.0, 0.07)
    angles = np.radians([-44, -26, -9, 8, 25, 42])
    order = [0, 5, 1, 4, 2, 3]
    for i in order:
        a = angles[i] + np.radians(rng.uniform(-4, 4))
        L = 0.86 if abs(angles[i]) < 0.3 else 0.64
        c.leaf(base, a, L * rng.uniform(0.94, 1.0), 0.15, bend=np.sign(a) * 0.05, round_=0.5, point=0.9,
               taper_tip=0.3)
    c.stalk([(0, 0.0), (0, 0.09)], 0.03, 0.62)
    _save_rgba("tree_leaf_mango", c.tone, c.alpha)


def leaf_fig(w=384, h=512):
    """A fig twig: three small pointed ovals."""
    c = Card(w, h, 0.75)
    c.stalk([(0.0, 0.0), (0.02, 0.45), (0.0, 0.75)], 0.014, 0.62)
    c.leaf((0.01, 0.2), np.radians(-48), 0.36, 0.22, round_=0.5, point=1.1, taper_tip=0.4)
    c.leaf((0.02, 0.36), np.radians(44), 0.36, 0.22, round_=0.5, point=1.1, taper_tip=0.4)
    c.leaf((0.0, 0.58), np.radians(-4), 0.4, 0.24, round_=0.5, point=1.1, taper_tip=0.4)
    _save_rgba("tree_leaf_fig", c.tone, c.alpha)


def frond(w=512, h=1024, seed=21):
    """A Manila palm frond: a bowed midrib, about eighteen leaflets a side, broad and stiff, combing
    forward, longest a little below mid-frond, shortening into a point inside the card."""
    rng = np.random.default_rng(seed)
    c = Card(w, h, 1.0)                  # X -0.5..0.5, Y 0..2
    n = 17
    for side in (-1, 1):
        for k in range(n):
            t = 0.06 + 0.88 * k / n + rng.uniform(-0.006, 0.006)
            y = t * 1.94
            ox = 0.03 * np.sin(np.pi * t)
            length = 0.66 * min(1.0, t / 0.2) ** 0.5 * np.clip((0.98 - t) / 0.6, 0, 1) ** 0.8
            ang = side * np.radians(rng.uniform(40, 48))
            # Keep every leaflet inside the card: its tip must stay 3 per cent from the edges.
            fit_side = (0.47 - side * ox) / abs(np.sin(ang))
            fit_top = (1.96 - y) / np.cos(ang)
            length = min(length, fit_side, fit_top)
            if length < 0.03:
                continue
            c.leaf((ox, y), ang, length, 0.095, bend=-side * 0.06, round_=0.45, point=0.9, taper_tip=0.4)
    rib = [(0.0, 0.0)] + [(0.03 * np.sin(np.pi * t), t * 1.94) for t in np.linspace(0.1, 0.97, 8)]
    Card.stalk(c, rib, 0.022, 0.8)
    # Taper the rib to a point: fade the alpha of the rib's last few per cent.
    c.alpha *= np.clip((1.985 - c.Y) / 0.04, 0, 1)
    _save_rgba("tree_frond", c.tone, c.alpha)


def lily_leaf(w=128, h=1024):
    """A spider lily strap: parallel-sided, a long taper at the tip, a soft channel down the
    middle with a pale ridge on its lit side, darker at the foot."""
    X, Y = _grid(w, h, 0.125)
    v = Y / Y.max()
    half = 0.056 * np.clip(v * 12, 0, 1) ** 0.3 * np.clip((1 - v) / 0.3, 0, 1) ** 0.7
    d = half - np.abs(X)
    xn = X / np.maximum(half, 1e-4)
    tone = 0.6 + 0.3 * v ** 0.8
    tone = tone - 0.16 * np.exp(-(xn / 0.14) ** 2)
    tone = tone + 0.12 * np.exp(-((xn + 0.3) / 0.14) ** 2)
    tone = tone + 0.1 * np.exp(-(((xn + 0.4) / 0.5) ** 2 + ((v - 0.55) / 0.2) ** 2))
    tone = tone - 0.12 * np.clip(1 - v / 0.15, 0, 1)
    _save_rgba("tree_lily_leaf", tone, d * w / 0.125 / 2.5)


def lily_flower(size=512):
    """A spider lily head in colour: two flowers, each six THICK, softly pointed petals (drawn as
    leaves, so they are chunky and never hairs; v1's thin arms read as hairs) round a smooth pale
    cup with a green-yellow throat. White is a warm cream, never a pure paper white."""
    c = Card(size, size, 1.0)
    rgb = np.zeros(c.X.shape + (3,))
    alpha = np.zeros_like(c.X)
    cream = np.array([0.95, 0.94, 0.88])
    for cx, cy, rot, s in ((-0.14, 0.4, 0.2, 1.0), (0.17, 0.62, -0.15, 0.82)):
        pc = Card(size, size, 1.0)
        for k in range(6):
            pc.leaf((cx, cy), rot + k * np.pi / 3, 0.33 * s, 0.08 * s, bend=0.12, round_=0.5, point=0.7,
                    taper_tip=0.5)
        r = np.hypot(c.X - cx, c.Y - cy) / s
        cup = np.clip((0.1 - r) * size / 2.0, 0, 1)
        a = np.maximum(pc.alpha, cup)
        tone = 0.84 + 0.14 * np.clip(r / 0.33, 0, 1)
        col = cream[None, None, :] * tone[..., None]
        throat = np.exp(-(r / 0.06) ** 2)[..., None]
        col = col * (1 - throat) + np.array([0.70, 0.78, 0.42]) * throat
        col = col * (1 - 0.08 * np.exp(-((r - 0.09) / 0.018) ** 2)[..., None])
        rgb = np.where((a > 0.5)[..., None], col, rgb)
        alpha = np.maximum(alpha, a)
    _save_colour("tree_lily_flower", rgb, alpha)


def shrub_leaf(w=384, h=512):
    """Santan: a pair of glossy oblong leaves on a short stalk."""
    c = Card(w, h, 0.75)
    c.leaf((0.0, 0.04), np.radians(-26), 0.9, 0.3, round_=0.45, point=0.95, taper_tip=0.3)
    c.leaf((0.0, 0.04), np.radians(22), 0.92, 0.3, round_=0.45, point=0.95, taper_tip=0.3)
    _save_rgba("tree_shrub_leaf", c.tone, c.alpha)


def ixora(size=256):
    """The ixora ball: about eighteen four-lobed florets packed into a dome, each floret a soft
    round with a paler centre. Greyscale; the material tints it crimson."""
    rng = np.random.default_rng(31)
    X, Y = _grid(size, size, 1.0)
    Y = Y - 0.5
    alpha = np.zeros_like(X)
    tone = np.zeros_like(X)
    pts = []
    golden = np.pi * (3 - np.sqrt(5))
    for i in range(26):
        r = 0.33 * np.sqrt((i + 0.5) / 26)
        a = golden * i + rng.uniform(-0.2, 0.2)
        pts.append((r * np.cos(a), r * np.sin(a)))
    for px_, py_ in sorted(pts, key=lambda p: -np.hypot(*p)):
        dx, dy = X - px_, Y - py_
        r = np.hypot(dx, dy)
        th = np.arctan2(dy, dx)
        rad = 0.1 * (0.82 + 0.18 * np.abs(np.cos(2 * th)))
        a = np.clip((rad - r) * size / 2.0, 0, 1)
        t = 0.72 + 0.16 * (1 - r / 0.1) + 0.14 * (1 - np.hypot(px_, py_) / 0.4)
        t = t + 0.12 * np.exp(-(r / 0.025) ** 2)
        tone = np.where(a > 0.5, t, tone)
        alpha = np.maximum(alpha, a)
    _save_rgba("tree_ixora", tone, alpha)


def hedge_leaf(size=384):
    """Kamuning: a sprig of five small round leaves."""
    c = Card(size, size, 0.6)
    c.stalk([(0.0, 0.0), (0.0, 0.42)], 0.012, 0.62)
    for base, ang in (((0.0, 0.08), -60), ((0.0, 0.14), 58), ((0.0, 0.26), -52), ((0.0, 0.32), 50), ((0.0, 0.38), 0)):
        c.leaf(base, np.radians(ang), 0.2, 0.14, round_=0.5, point=0.8)
    _save_rgba("tree_hedge_leaf", c.tone, c.alpha)


# ------------------------------------------------------------------ tiling surfaces (2 m tiles, 1024 px)

S = 1024
TILE = 2.0
PPM = S / TILE


def _coat(img, shift, scale_m, coverage, seed, feather=0.6, stretch=(1.0, 1.0)):
    n = _smooth((S, S), scale_m * PPM, seed)
    if stretch != (1.0, 1.0):
        white = np.random.default_rng(seed).standard_normal((S, S))
        fy = np.fft.fftfreq(S)[:, None] * stretch[1]
        fx = np.fft.fftfreq(S)[None, :] * stretch[0]
        s = scale_m * PPM
        n = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-(fx ** 2 + fy ** 2) * s * s * 2)))
        n = (n - n.mean()) / n.std()
    edge = np.quantile(n, 1 - coverage)
    m = np.clip((n - edge) / feather + 0.5, 0, 1)
    m = m * m * (3 - 2 * m)
    return img * (1 + (np.asarray(shift) - 1) * m[..., None]), m


def bark():
    """Variant J, exactly: author_kanto_textures.bark_j's recipe and numbers, saved as tree_bark.
    (Kanto's own file is not touched.)"""
    base = KT.flat("7a5236")
    dark = KT._streaks_j(0.09, 131, 0.26, 0.5)
    light = KT._streaks_j(0.07, 133, 0.12, 0.5)
    img = base * (1 - dark * 0.2) * (1 + light * 0.12)
    img = KT.patches(img, np.array([1.05, 1.03, 1.0]), 0.8, 0.2, seed=135, feather=0.6)
    height = 1 - dark[..., 0] * 0.8 + light[..., 0] * 0.3
    _save_colour("tree_bark", img)
    _normal("tree_bark", (height - height.min()) / np.ptp(height), 2.0 / 8 * 3)
    print("[tree-tex] tree_bark (variant J)")


def palm_trunk():
    """Pale grey palm trunk with soft ring scars. v runs up the trunk (2 m a tile): rings every
    ~0.2 m, each a wobbly feathered band a step darker, with a pale lip above it; two broad soft
    vertical washes. Two value steps at most."""
    Yv, Xu = np.mgrid[0:S, 0:S] / PPM
    img = np.ones((S, S, 3)) * np.array([0.70, 0.68, 0.63])
    img, _ = _coat(img, (1.06, 1.05, 1.03), 0.3, 0.35, 211, stretch=(1.0, 5.0))
    img, _ = _coat(img, (0.93, 0.93, 0.92), 0.3, 0.25, 212, stretch=(1.0, 5.0))
    wob = _smooth((S, S), 0.9 * PPM, 213) * 0.008
    spacing = TILE / 10
    ph = (Yv + wob) % spacing
    d = np.minimum(ph, spacing - ph)
    ring = np.exp(-(d / 0.022) ** 2)
    lip = np.exp(-((ph - 0.035) / 0.02) ** 2)
    img = img * (1 - 0.13 * ring[..., None]) * (1 + 0.05 * lip[..., None])
    _save_colour("tree_palm_trunk", img)
    _normal("tree_palm_trunk", 1 - ring * 0.8 + lip * 0.2, 0.4)


def crownshaft():
    img = np.ones((S, S, 3)) * np.array([0.44, 0.58, 0.30])
    img, _ = _coat(img, (1.08, 1.07, 1.02), 0.2, 0.3, 221, stretch=(1.0, 6.0))
    img, _ = _coat(img, (0.92, 0.94, 0.95), 0.25, 0.2, 222, stretch=(1.0, 6.0))
    _save_colour("tree_crownshaft", img)
    _normal("tree_crownshaft", np.zeros((S, S)), 0.0)


def pit_soil():
    img = np.ones((S, S, 3)) * np.array([0.32, 0.25, 0.19])
    img, _ = _coat(img, (1.16, 1.13, 1.1), 0.35, 0.3, 231, feather=1.0)                  # dry dust
    img, _ = _coat(img, (1.18, 1.08, 0.84), 0.22, 0.1, 232, feather=0.8)      # fallen leaves
    img, _ = _coat(img, (0.9, 0.92, 0.86), 0.3, 0.14, 233, feather=1.0)                   # damp
    _save_colour("tree_pit_soil", img)
    _normal("tree_pit_soil", _smooth((S, S), 0.1 * PPM, 234) * 0.1, 0.3)


def pit_kerb():
    img = np.ones((S, S, 3)) * np.array([0.74, 0.72, 0.68])
    img, _ = _coat(img, (1.03, 1.03, 1.02), 0.7, 0.35, 241, feather=1.2)
    img, _ = _coat(img, (0.95, 0.95, 0.93), 0.6, 0.2, 242, feather=1.2)
    _save_colour("tree_pit_kerb", img)
    _normal("tree_pit_kerb", np.zeros((S, S)), 0.0)


# ------------------------------------------------------------------ positional trunk overlays

def _noise1d(n, scale_px, seed):
    white = np.random.default_rng(seed).standard_normal(n)
    f = np.fft.fftfreq(n)
    s = np.real(np.fft.ifft(np.fft.fft(white) * np.exp(-(f * scale_px) ** 2 * 2)))
    return (s - s.mean()) / (s.std() + 1e-9)


def splash(w=512, h=512):
    """Multiplier, 2 m tall (v = height / 2): the soil splash at the foot of a trunk. One dark warm
    band up to about 0.35 m whose top is a slow, rounded, feathered wave. (v2 hung square tongues
    above it, which read as boxes.)"""
    v = (h - 1 - np.mgrid[0:h, 0:w][0]) / h * 2.0
    top = 0.3 + 0.07 * _noise1d(w, w * 0.14, 251)[None, :] + 0.012 * _noise1d(w, w * 0.06, 253)[None, :]
    band = np.clip((top - v) / 0.09 + 0.5, 0, 1)
    band = band * band * (3 - 2 * band)
    a = np.ones((h, w, 3)) * (1 - band[..., None] * (1 - np.array([0.74, 0.68, 0.62])))
    Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8)).save(OUT / "tree_splash.png")
    print("[tree-tex] tree_splash")


def limewash(w=512, h=512):
    """RGBA, 2 m tall: the lime paint band on the lower trunk. The top edge sits near 1.1 m and
    wanders slowly, feathered over a few centimetres (v2's brush runs made a spiky edge). The white
    is a warm, slightly grey lime, one flat coat, dirtier over its lowest half metre."""
    v = (h - 1 - np.mgrid[0:h, 0:w][0]) / h * 2.0
    top = 1.08 + 0.05 * _noise1d(w, w * 0.18, 261)[None, :] + 0.015 * _noise1d(w, w * 0.07, 262)[None, :]
    mask = np.clip((top - v) / 0.03 + 0.5, 0, 1)
    col = np.ones((h, w, 3)) * np.array([0.92, 0.91, 0.86])
    col = col * (1 - np.clip(1 - v / 0.55, 0, 1)[..., None] * 0.18)
    rgba = np.dstack([np.clip(col, 0, 1), mask])
    Image.fromarray((rgba * 255).astype(np.uint8), "RGBA").save(OUT / "tree_limewash.png")
    print("[tree-tex] tree_limewash")


# ------------------------------------------------------------------ review sheet

LEAVES = ["tree_leaf_raintree", "tree_leaf_narra", "tree_leaf_mango", "tree_leaf_fig", "tree_frond",
          "tree_lily_leaf", "tree_lily_flower", "tree_shrub_leaf", "tree_ixora", "tree_hedge_leaf"]
SURFACES = ["tree_bark", "tree_palm_trunk", "tree_crownshaft", "tree_pit_soil", "tree_pit_kerb"]
# The tints the model script uses (light, dark), repeated here only for the sheet.
SHEET_TINT = {"tree_leaf_raintree": (0.34, 0.52, 0.14), "tree_leaf_narra": (0.44, 0.62, 0.16),
              "tree_leaf_mango": (0.20, 0.40, 0.12), "tree_leaf_fig": (0.36, 0.58, 0.12),
              "tree_frond": (0.50, 0.66, 0.20), "tree_lily_leaf": (0.34, 0.56, 0.20),
              "tree_shrub_leaf": (0.24, 0.46, 0.14), "tree_ixora": (0.74, 0.12, 0.20),
              "tree_hedge_leaf": (0.30, 0.52, 0.14)}


def sheet(version):
    SHEETS.mkdir(parents=True, exist_ok=True)
    cell = 260
    cols = 5
    rows = 2 + 1
    img = Image.new("RGB", (cols * (cell + 20) + 20, rows * (cell + 44) + 20), (228, 224, 214))
    d = ImageDraw.Draw(img)
    for k, name in enumerate(LEAVES):
        x, y = 20 + (k % cols) * (cell + 20), 20 + (k // cols) * (cell + 44)
        src = Image.open(OUT / f"{name}_albedo.png").convert("RGBA")
        a = np.asarray(src, dtype=float) / 255
        tint = SHEET_TINT.get(name)
        rgb = a[..., :3] * (np.array(tint) * 1.25 if tint else 1.0)
        bg = np.ones_like(rgb) * np.array([0.82, 0.86, 0.9])
        comp = rgb * a[..., 3:] + bg * (1 - a[..., 3:])
        tile = Image.fromarray((np.clip(comp, 0, 1) * 255).astype(np.uint8))
        tile.thumbnail((cell, cell), Image.LANCZOS)
        img.paste(tile, (x + (cell - tile.width) // 2, y))
        d.text((x, y + cell + 8), name, fill=(30, 30, 30))
    for k, name in enumerate(SURFACES):
        x, y = 20 + k * (cell + 20), 20 + 2 * (cell + 44)
        tile = Image.open(OUT / f"{name}_albedo.png").convert("RGB")
        rep = Image.new("RGB", (tile.width * 3, tile.height * 3))
        for i in range(3):
            for j in range(3):
                rep.paste(tile, (i * tile.width, j * tile.height))
        img.paste(rep.resize((cell, cell), Image.LANCZOS), (x, y))
        d.text((x, y + cell + 8), f"{name} (3 x 3, 6 m)", fill=(30, 30, 30))
    path = SHEETS / f"tree_swatches_v{version}.png"
    img.save(path)
    # The trunk overlays over bark, as they sit on a trunk (ground at the bottom).
    bark_img = np.asarray(Image.open(OUT / "tree_bark_albedo.png").convert("RGB"), dtype=float)[:512, :512] / 255
    sp = np.asarray(Image.open(OUT / "tree_splash.png").convert("RGB"), dtype=float) / 255
    lw = np.asarray(Image.open(OUT / "tree_limewash.png").convert("RGBA"), dtype=float) / 255
    plain = bark_img * sp
    painted = plain * (1 - lw[..., 3:]) + lw[..., :3] * sp * lw[..., 3:]
    both = np.concatenate([plain, np.ones((512, 16, 3)), painted], axis=1)
    Image.fromarray((np.clip(both, 0, 1) * 255).astype(np.uint8)).save(SHEETS / f"tree_trunk_overlays_v{version}.png")
    print("[tree-tex] sheet", path)


PAINTERS = [leaf_raintree, leaf_narra, leaf_mango, leaf_fig, frond, lily_leaf, lily_flower, shrub_leaf, ixora,
            hedge_leaf, bark, palm_trunk, crownshaft, pit_soil, pit_kerb, splash, limewash]


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for p in PAINTERS:
        p()
    version = int(sys.argv[sys.argv.index("--sheet") + 1]) if "--sheet" in sys.argv else 1
    sheet(version)


if __name__ == "__main__":
    main()
