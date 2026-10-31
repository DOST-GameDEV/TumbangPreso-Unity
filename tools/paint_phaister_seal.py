"""Paints phaister_seal.png, the linework of Phaister's sky circle: three layers that turn against each other.
R the outer ring and its script, G the two seven-pointed star polygons, B the inner ring, its wings and its script.
Her own script: every glyph is built from a moth's parts (a spine, a pair of wings, feelers, a stitch, a dot).
No moons and no crosses: the owner rejected those on the hex circle. A fixed seed, so a repaint is the same picture."""
import math, random, sys
from PIL import Image, ImageDraw
SS, OUT, EXTENT = 4096, 2048, 1.05   # supersampled size, final size, the radius the picture's edge stands for

def px(r, a):
    k = SS * 0.5 / EXTENT
    return (SS * 0.5 + math.cos(a) * r * k, SS * 0.5 + math.sin(a) * r * k)

def poly(d, pts, w):
    d.line(pts, fill=255, width=w, joint="curve")

def circle(d, r, w, n=720):
    poly(d, [px(r, i / n * math.tau) for i in range(n + 1)], w)

def small(d, c, r, w):
    k = SS * 0.5 / EXTENT
    x, y = SS * .5 + c[0] * k, SS * .5 + c[1] * k
    R = r * k
    d.ellipse((x - R, y - R, x + R, y + R), outline=255, width=w)

def strokes(rng):
    """One glyph, as polylines in a cell: x -0.5 to 0.5 across, y 0 to 1 up."""
    out = []
    def curve(p0, c, p1, n=8):
        return [((1-t)**2*p0[0]+2*t*(1-t)*c[0]+t*t*p1[0], (1-t)**2*p0[1]+2*t*(1-t)*c[1]+t*t*p1[1]) for t in (i/n for i in range(n+1))]
    kind = rng.randrange(5)
    top = rng.uniform(.82, 1.0)
    if kind in (0, 1, 2):
        out.append([(0, .04), (0, top)])                                   # the spine
    if kind in (0, 3):                                                      # a pair of wings
        y = rng.uniform(.35, .6); h = rng.uniform(.25, .4)
        for s in (-1, 1): out.append(curve((0, y), (s * .55, y + h), (s * .36, y - h * .6)))
    if kind in (1, 4):                                                      # a folded pair, a V from the root
        y = rng.uniform(.12, .3)
        for s in (-1, 1): out.append(curve((0, y), (s * .15, y + .3), (s * .4, y + .62)))
    if kind == 2:                                                           # a loop on one side of the spine
        s = rng.choice((-1, 1)); y = rng.uniform(.3, .6)
        out.append(curve((0, y), (s * .62, y + .1), (0, y + .28)))
    if rng.random() < .55:                                                  # feelers
        for s in (-1, 1): out.append(curve((0, top), (s * .12, top + .02), (s * .3, top - .14), 5))
    if rng.random() < .4:                                                   # a stitch, one-sided so it never makes a cross
        s = rng.choice((-1, 1)); y = rng.uniform(.1, .3)
        out.append([(s * .08, y), (s * .4, y - .04)])
    return out

def band(d, r0, r1, count, w, rng, gap_every=0):
    per = math.tau / count
    for i in range(count):
        if gap_every and i % gap_every == 0: continue
        a = i * per
        for s in strokes(rng):
            poly(d, [px(r0 + (r1 - r0) * y, a + x * per * .82) for x, y in s], w)

def layer_outer(rng):
    im = Image.new("L", (SS, SS)); d = ImageDraw.Draw(im)
    circle(d, .985, 13); circle(d, .958, 6); circle(d, .815, 7); circle(d, .797, 13)
    band(d, .832, .94, 84, 6, rng)
    for i in range(7):                                                       # seven beads on the rim, each with a feeler pair
        a = i / 7 * math.tau
        c = (math.cos(a) * 1.005, math.sin(a) * 1.005)
        small(d, c, .027, 8)
    for i in range(168):                                                     # the fine tick scale between the two outer lines
        a = i / 168 * math.tau
        poly(d, [px(.958, a), px(.985 if i % 4 == 0 else .972, a)], 4)
    return im

def layer_star(rng):
    im = Image.new("L", (SS, SS)); d = ImageDraw.Draw(im)
    R = .775
    for step, w in ((3, 11), (2, 6)):
        for i in range(7):
            a, b = i / 7 * math.tau - math.pi / 2, (i + step) / 7 * math.tau - math.pi / 2
            poly(d, [px(R, a), px(R, b)], w)
    circle(d, .560, 5)
    for i in range(7):
        a = i / 7 * math.tau - math.pi / 2
        c = (math.cos(a) * R, math.sin(a) * R)
        small(d, c, .040, 8); small(d, c, .016, 6)
        c = (math.cos(a + math.pi / 7) * .36 * 1.555, math.sin(a + math.pi / 7) * .36 * 1.555)
        small(d, c, .020, 6)
    return im

def layer_inner(rng):
    im = Image.new("L", (SS, SS)); d = ImageDraw.Draw(im)
    circle(d, .495, 11); circle(d, .478, 5); circle(d, .385, 6); circle(d, .345, 12)
    band(d, .395, .468, 56, 5, rng)
    for i in range(7):                                                       # seven moths' wings opening off the ring, outlines only
        a0 = (i + .5) / 7 * math.tau - math.pi / 2
        for s in (-1, 1):
            pts = []
            for k in range(17):
                t = k / 16
                r = .495 + .10 * math.sin(t * math.pi) ** .8 + .035 * t
                pts.append(px(r, a0 + s * (.015 + .17 * t)))
            pts2 = [px(.495 + .05 * math.sin(k / 16 * math.pi), a0 + s * (.015 + .17 * k / 16)) for k in range(17)]
            poly(d, pts, 6); poly(d, pts2, 4)
    return im

rng = random.Random(7)
chs = [f(rng).resize((OUT, OUT), Image.LANCZOS) for f in (layer_outer, layer_star, layer_inner)]
Image.merge("RGB", chs).save(sys.argv[1])
