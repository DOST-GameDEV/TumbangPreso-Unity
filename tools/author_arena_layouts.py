"""Writes tools/arena_layouts.json: the arena stage's five ROUND layouts, and a plan picture of them.

    py -3 tools/author_arena_layouts.py [--version=vN]

The game code (the stage's colliders, travel and spawns) and the art kit (tools/author_arena_stage.py,
one mesh per piece per layout) both read the JSON. Edit THIS file, never the JSON.

COORDINATES. Unity: x right, y up, z forward, metres. The can is the origin. A BEARING is degrees
clockwise from +z seen from above, so the point at (bearing b, radius r) is x = r sin b, z = r cos b.

PIECES (every one a hover plate; `top` is the height of its walking surface, `thick` its depth):
  disc  r                       a full disc about the origin
  ring  r0, r1                  a full annulus
  arc   r0, r1, a0, a1          an annular sector swept clockwise from bearing a0 to a1. a1 > a0
                                always; a0 may be negative (the crescent runs -70..70)
  ramp  bearing, r0, r1, z0, z1, width, thick
                                a straight piece along a bearing. In plan it is every point whose
                                sideways offset from the bearing's line is within width / 2 and
                                whose TRUE distance from the origin is between r0 and r1, so its
                                two ends are arcs of the circles r0 and r1 and sit flush against a
                                round neighbour with no crescent gap. Its walking height is linear
                                in that true distance: z0 at r0, z1 at r1. z0 == z1 is a bridge.

A piece keeps its id AND ITS KIND in every layout it appears in, and travels between its shapes;
an id missing from a layout sinks into the shaft. Ids are neutral (arc1..arc4, ramp1..ramp4,
link1..link4) because a piece changes bearing between layouts.

THE RULES check() ASSERTS, for every layout:
  - the can at the origin on solid floor at canHeight, canHeight within -1.5..1.5;
  - floor under the taya's mark (0, -2.5) AT THE CAN'S HEIGHT, and under the three attackers' marks
    within 2 m above and 4 m below y 0; each mark at least 0.45 m in from any edge;
  - everything inside radius 22 (the play walls);
  - no two pieces overlap in plan (that would be two faces in one plane);
  - ramps no steeper than 25 degrees; ramps and bridges at least 2.5 m wide;
  - both ends of every ramp land on a piece at that piece's height, across the ramp's whole width;
  - every piece reachable ON FOOT from the can, except the ones listed in the layout's `bonus`
    (jump pad only), and those must NOT be reachable on foot;
  - 2 jump pads, 2 speed pads, 3 stamina pickups, each on floor, its y that floor's height.

EXTRA KEYS beside the agreed schema (a JSON reader that ignores unknown keys loses nothing):
  layout.bonus   ids reachable only by jump pad (bots must not be sent there)
  notes          this explanation, short
"""
import json, math, os, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WALL = 22.0
JUMP = 15.5                      # m/s: at gravity 20 the apex is v*v/40 = 6.0 m


# ---------------------------------------------------------------- the pieces
def disc(pid, r, top=0.0, thick=1.0):
    return {"id": pid, "kind": "disc", "r": r, "top": top, "thick": thick}


def ring(pid, r0, r1, top=0.0, thick=1.0):
    return {"id": pid, "kind": "ring", "r0": r0, "r1": r1, "top": top, "thick": thick}


def arc(pid, r0, r1, a0, a1, top=0.0, thick=1.0):
    return {"id": pid, "kind": "arc", "r0": r0, "r1": r1, "a0": a0, "a1": a1, "top": top, "thick": thick}


def ramp(pid, bearing, r0, r1, z0=0.0, z1=0.0, width=3.0, thick=0.8):
    return {"id": pid, "kind": "ramp", "bearing": bearing, "r0": r0, "r1": r1, "z0": z0, "z1": z1,
            "width": width, "thick": thick}


# ---------------------------------------------------------------- geometry the check and the art share
def ang_diff(a, b):
    return (a - b + 180.0) % 360.0 - 180.0


def in_arc(bearing, a0, a1):
    return (bearing - a0) % 360.0 <= (a1 - a0) + 1e-9


def height_on(p, x, z, inset=0.0):
    """The walking height of piece `p` at (x, z), or None if the point is not on it (or is within
    `inset` metres of its edge)."""
    r = math.hypot(x, z)
    b = math.degrees(math.atan2(x, z))
    k = p["kind"]
    if k == "disc":
        return p["top"] if r <= p["r"] - inset else None
    if k == "ring":
        return p["top"] if p["r0"] + inset <= r <= p["r1"] - inset else None
    if k == "arc":
        if not (p["r0"] + inset <= r <= p["r1"] - inset):
            return None
        pad = math.degrees(inset / max(r, 0.01))
        if (p["a1"] - p["a0"]) < 2 * pad or not in_arc(b, p["a0"] + pad, p["a1"] - pad):
            return None
        return p["top"]
    if k == "ramp":
        a = math.radians(p["bearing"])
        along = x * math.sin(a) + z * math.cos(a)
        side = x * math.cos(a) - z * math.sin(a)
        lo, hi = min(p["r0"], p["r1"]), max(p["r0"], p["r1"])
        if along <= 0 or abs(side) > p["width"] / 2 - inset or not (lo + inset <= r <= hi - inset):
            return None
        t = (r - p["r0"]) / (p["r1"] - p["r0"])
        return p["z0"] + (p["z1"] - p["z0"]) * t
    raise ValueError(k)


def floor_at(lay, x, z, inset=0.0):
    """(height, piece id) of the highest floor at (x, z), or (None, None)."""
    best = (None, None)
    for p in lay["pieces"]:
        h = height_on(p, x, z, inset)
        if h is not None and (best[0] is None or h > best[0]):
            best = (h, p["id"])
    return best


def at(bearing, r):
    a = math.radians(bearing)
    return r * math.sin(a), r * math.cos(a)


def jump(lay, bearing, r, speed=JUMP):
    return {"bearing": bearing, "r": r, "y": None, "speed": speed}


def speed(bearing, r, along="tangent"):
    """halfSize is [half across the travel direction, half along it]. `tangent` travel is CLOCKWISE
    (toward a greater bearing), `radial` travel is OUTWARD; the chevrons are painted that way."""
    return {"bearing": bearing, "r": r, "y": None, "along": along, "halfSize": [0.75, 1.5]}


def pickup(bearing, r):
    return {"bearing": bearing, "r": r, "y": None}


# ---------------------------------------------------------------- 0. PLAZA: open and broad
# A wide drum, a walk and four outer petals: three concentric bands split by two narrow slots
# (2 m and 1.5 m) that a careless sprint drops into. The safest layout; the outer edge is the risk.
plaza = {"name": "plaza", "canHeight": 0.0, "bonus": [], "pieces": [
    disc("drum", 11.5),
    ring("walk", 13.5, 17.0),
    arc("arc1", 18.5, 21.5, 13.0, 77.0), arc("arc2", 18.5, 21.5, 103.0, 167.0),
    arc("arc3", 18.5, 21.5, 193.0, 257.0), arc("arc4", 18.5, 21.5, 283.0, 347.0),
    ramp("ramp1", 0.0, 11.5, 13.5, width=6.0), ramp("ramp2", 90.0, 11.5, 13.5, width=6.0),
    ramp("ramp3", 180.0, 11.5, 13.5, width=6.0), ramp("ramp4", 270.0, 11.5, 13.5, width=6.0),
    ramp("link1", 45.0, 17.0, 18.5, width=4.5), ramp("link2", 135.0, 17.0, 18.5, width=4.5),
    ramp("link3", 225.0, 17.0, 18.5, width=4.5), ramp("link4", 315.0, 17.0, 18.5, width=4.5)],
    "jumpPads": [jump(None, 120.0, 8.0), jump(None, 300.0, 8.0)],
    "speedPads": [speed(90.0, 15.25), speed(270.0, 15.25)],
    "pickups": [pickup(135.0 + 20.0, 20.0), pickup(315.0 + 20.0, 20.0), pickup(180.0, 15.25)]}

# ---------------------------------------------------------------- 1. TORE: the can on a raised drum
# A small drum 1.5 m up, four narrow ramps down to a ring walk, and only TWO long outer arcs (north
# east and south west), so the plan is lopsided: the other two quarters are open sky past the walk.
tore = {"name": "tore", "canHeight": 1.5, "bonus": [], "pieces": [
    disc("drum", 4.6, top=1.5),
    ring("walk", 8.0, 12.5),
    ramp("ramp1", 0.0, 4.6, 8.0, z0=1.5, z1=0.0, width=3.5), ramp("ramp2", 90.0, 4.6, 8.0, z0=1.5, z1=0.0, width=3.5),
    ramp("ramp3", 180.0, 4.6, 8.0, z0=1.5, z1=0.0, width=3.5), ramp("ramp4", 270.0, 4.6, 8.0, z0=1.5, z1=0.0, width=3.5),
    arc("arc1", 16.0, 21.0, -5.0, 95.0), arc("arc3", 16.0, 21.0, 175.0, 275.0),
    ramp("link1", 45.0, 12.5, 16.0, width=3.0), ramp("link3", 225.0, 12.5, 16.0, width=3.0)],
    "jumpPads": [jump(None, 135.0, 10.25), jump(None, 315.0, 10.25)],
    "speedPads": [speed(70.0, 18.5), speed(250.0, 18.5)],
    "pickups": [pickup(10.0, 18.5), pickup(190.0, 18.5), pickup(180.0, 10.25)]}

# ---------------------------------------------------------------- 2. KRUS: four catwalks and four pits
# A small centre, an outer ring, and four 2.6 m catwalks between them: a cross with a pit in each
# quarter (9 m across). The attackers start on a small apron half way along the north catwalk.
krus = {"name": "krus", "canHeight": 0.0, "bonus": [], "pieces": [
    disc("drum", 5.0),
    ring("walk", 14.0, 18.5),
    arc("apron", 7.6, 10.6, -24.0, 24.0),
    ramp("ramp1", 0.0, 5.0, 7.6, width=2.6), ramp("link1", 0.0, 10.6, 14.0, width=2.6),
    ramp("ramp2", 90.0, 5.0, 14.0, width=2.6), ramp("ramp3", 180.0, 5.0, 14.0, width=2.6),
    ramp("ramp4", 270.0, 5.0, 14.0, width=2.6)],
    "jumpPads": [jump(None, 45.0, 16.25), jump(None, 225.0, 16.25)],
    "speedPads": [speed(112.0, 16.25), speed(292.0, 16.25)],
    "pickups": [pickup(135.0, 16.25), pickup(315.0, 16.25), pickup(180.0, 16.25)]}

# ---------------------------------------------------------------- 3. HUKAY: the can in a sunken bowl
# An amphitheatre: the can 1.2 m DOWN on a small floor, four wide ramps up to the walk, and four
# outer terraces a metre ABOVE the walk. Three levels, the can at the bottom of all of them.
hukay = {"name": "hukay", "canHeight": -1.2, "bonus": [], "pieces": [
    disc("drum", 5.2, top=-1.2),
    ring("walk", 8.0, 13.0),
    ramp("ramp1", 0.0, 5.2, 8.0, z0=-1.2, z1=0.0, width=5.0), ramp("ramp2", 90.0, 5.2, 8.0, z0=-1.2, z1=0.0, width=5.0),
    ramp("ramp3", 180.0, 5.2, 8.0, z0=-1.2, z1=0.0, width=5.0), ramp("ramp4", 270.0, 5.2, 8.0, z0=-1.2, z1=0.0, width=5.0),
    arc("arc1", 15.5, 21.0, 7.0, 83.0, top=1.0), arc("arc2", 15.5, 21.0, 97.0, 173.0, top=1.0),
    arc("arc3", 15.5, 21.0, 187.0, 263.0, top=1.0), arc("arc4", 15.5, 21.0, 277.0, 353.0, top=1.0),
    ramp("link1", 45.0, 13.0, 15.5, z0=0.0, z1=1.0, width=3.5), ramp("link2", 135.0, 13.0, 15.5, z0=0.0, z1=1.0, width=3.5),
    ramp("link3", 225.0, 13.0, 15.5, z0=0.0, z1=1.0, width=3.5), ramp("link4", 315.0, 13.0, 15.5, z0=0.0, z1=1.0, width=3.5)],
    "jumpPads": [jump(None, 60.0, 3.7), jump(None, 240.0, 3.7)],
    "speedPads": [speed(35.0, 10.5), speed(215.0, 10.5)],
    "pickups": [pickup(70.0, 18.25), pickup(250.0, 18.25), pickup(160.0, 18.25)]}

# ---------------------------------------------------------------- 4. ENTABLADO: the high ground and two lofts
# The attackers start on a raised crescent (1.2 m up) that wraps the north half; a 3 m moat lies
# between it and the can's small island, crossed by two ramps. Two south stages hang off the island
# by bridges, with a wedge of open air between them, and each carries a jump pad to a loft 3 m up
# that nothing else reaches.
entablado = {"name": "entablado", "canHeight": 0.0, "bonus": ["arc1", "arc4"], "pieces": [
    disc("drum", 4.2),
    arc("apron", 7.2, 13.5, -72.0, 72.0, top=1.2),
    ramp("ramp2", 50.0, 4.2, 7.2, z0=0.0, z1=1.2, width=3.0), ramp("ramp4", 310.0, 4.2, 7.2, z0=0.0, z1=1.2, width=3.0),
    arc("arc2", 8.2, 15.5, 102.0, 172.0), arc("arc3", 8.2, 15.5, 188.0, 258.0),
    ramp("link2", 138.0, 4.2, 8.2, width=3.0), ramp("link3", 222.0, 4.2, 8.2, width=3.0),
    arc("arc1", 17.5, 21.5, 96.0, 134.0, top=3.0, thick=0.6), arc("arc4", 17.5, 21.5, 226.0, 264.0, top=3.0, thick=0.6)],
    "jumpPads": [jump(None, 116.0, 13.6), jump(None, 244.0, 13.6)],
    "speedPads": [speed(325.0, 10.35), speed(160.0, 11.85, along="radial")],
    "pickups": [pickup(115.0, 19.5), pickup(245.0, 19.5), pickup(200.0, 11.85)]}

LAYOUTS = [plaza, tore, krus, hukay, entablado]

DATA = {
    "notes": "The arena stage's five round layouts. WRITTEN BY tools/author_arena_layouts.py: edit that, not this. "
             "Unity coordinates (x right, y up, z forward), metres, the can at the origin. A bearing is degrees "
             "clockwise from +z: x = r sin b, z = r cos b. A ramp's plan is every point within width/2 of its bearing "
             "line whose true distance from the origin is between r0 and r1; its height is linear in that distance "
             "(z0 at r0, z1 at r1). An arc sweeps clockwise from a0 to a1 (a1 > a0, a0 may be negative). A piece keeps "
             "its id and kind across layouts and travels; an id missing from a layout sinks away. 'bonus' lists the "
             "pieces only a jump pad reaches. Speed pad halfSize is [across, along]; 'tangent' travel is clockwise, "
             "'radial' travel is outward.",
    "stageTop": 0.0, "pitRadius": 40.0, "wallHalf": WALL, "catchY": -4.5,   # above y -5 (the host refuses poses below it), below the lowest underside (-2.2)
    "spawns": {"taya": [0, 0, -2.5], "attackers": [[-1.8, 0, 9], [0, 0, 9], [1.8, 0, 9]]},
    "layouts": LAYOUTS,
}


def settle():
    """Every pad's and pickup's y is the floor under it."""
    for lay in LAYOUTS:
        for key in ("jumpPads", "speedPads", "pickups"):
            for it in lay[key]:
                x, z = at(it["bearing"], it["r"])
                h, pid = floor_at(lay, x, z, inset=0.9 if key != "pickups" else 0.5)
                assert h is not None, (lay["name"], key, it, "is not on a floor, or is too near an edge")
                assert next(p for p in lay["pieces"] if p["id"] == pid)["kind"] != "ramp", (lay["name"], key, "on a ramp")
                it["y"] = round(h, 4)


# ---------------------------------------------------------------- the check
def outer(p):
    return p["r"] if p["kind"] == "disc" else max(p["r0"], p["r1"])


def covers_edge(p, bearing, r, half_width, z, inner):
    """Does piece `p` present an edge at radius `r` and height `z`, across a ramp's width?"""
    if p["kind"] == "ramp":
        return False
    if abs(p["top"] - z) > 0.02:
        return False
    edge = (p["r"] if p["kind"] == "disc" else p["r1"]) if inner else (p.get("r0") if p["kind"] != "disc" else None)
    if edge is None or abs(edge - r) > 0.02:
        return False
    if p["kind"] == "arc":
        half = math.degrees(math.asin(min(1.0, half_width / r)))
        return in_arc(bearing - half, p["a0"], p["a1"]) and in_arc(bearing + half, p["a0"], p["a1"]) \
            and (p["a1"] - p["a0"]) >= 2 * half
    return True


def graph(lay):
    """Which pieces join on foot: a ramp end flush on a piece's edge at its height."""
    pieces = lay["pieces"]
    links = {p["id"]: set() for p in pieces}
    for q in pieces:
        if q["kind"] != "ramp":
            continue
        lo_r, lo_z, hi_r, hi_z = (q["r0"], q["z0"], q["r1"], q["z1"]) if q["r0"] < q["r1"] else (q["r1"], q["z1"], q["r0"], q["z0"])
        for r, z, inner in ((lo_r, lo_z, True), (hi_r, hi_z, False)):
            found = [p for p in pieces if covers_edge(p, q["bearing"], r, q["width"] / 2, z, inner)]
            assert found, (lay["name"], q["id"], "end at r %.2f z %.2f lands on nothing" % (r, z))
            for p in found:
                links[q["id"]].add(p["id"]); links[p["id"]].add(q["id"])
    return links


def check():
    marks = DATA["spawns"]
    kinds = {}
    for lay in LAYOUTS:
        name = lay["name"]
        ids = [p["id"] for p in lay["pieces"]]
        assert len(ids) == len(set(ids)), (name, "duplicate id")
        for p in lay["pieces"]:
            assert kinds.setdefault(p["id"], p["kind"]) == p["kind"], (name, p["id"], "changes kind between layouts")
            assert outer(p) <= WALL, (name, p["id"], "outside the play walls")
            if p["kind"] == "ramp":
                assert p["width"] >= 2.5, (name, p["id"], "narrower than 2.5 m")
                slope = math.degrees(math.atan2(abs(p["z1"] - p["z0"]), abs(p["r1"] - p["r0"])))
                assert slope <= 25.0, (name, p["id"], "slope %.1f" % slope)
            if p["kind"] == "arc":
                assert p["a1"] > p["a0"] and p["a1"] - p["a0"] < 360.0, (name, p["id"])
        assert -1.5 <= lay["canHeight"] <= 1.5, name
        # the can and the marks
        h, _ = floor_at(lay, 0.0, 0.0, inset=1.0)
        assert h is not None and abs(h - lay["canHeight"]) < 1e-6, (name, "no floor under the can at its height")
        h, _ = floor_at(lay, marks["taya"][0], marks["taya"][2], inset=0.45)
        assert h is not None and abs(h - lay["canHeight"]) < 1e-6, (name, "the taya's mark is not on the can's floor")
        for m in marks["attackers"]:
            h, _ = floor_at(lay, m[0], m[2], inset=0.45)
            assert h is not None and -4.0 <= h <= 2.0, (name, "no floor under an attacker's mark", m, h)
        # no two pieces overlap in plan
        step = 0.25
        n = int(WALL / step)
        for i in range(-n, n + 1):
            for j in range(-n, n + 1):
                x, z = i * step + 0.011, j * step + 0.007
                on = [p["id"] for p in lay["pieces"] if height_on(p, x, z) is not None]
                assert len(on) <= 1, (name, "pieces overlap at", x, z, on)
        # reach
        links = graph(lay)
        _, start = floor_at(lay, 0.0, 0.0)
        seen, todo = {start}, [start]
        while todo:
            for nb in links[todo.pop()]:
                if nb not in seen:
                    seen.add(nb); todo.append(nb)
        for p in lay["pieces"]:
            if p["id"] in lay["bonus"]:
                assert p["id"] not in seen, (name, p["id"], "is a bonus piece but is reachable on foot")
            else:
                assert p["id"] in seen, (name, p["id"], "cannot be reached on foot from the can")
        for m in marks["attackers"] + [marks["taya"]]:
            assert floor_at(lay, m[0], m[2])[1] in seen, (name, "a mark is on an unreachable piece")
        assert len(lay["jumpPads"]) == 2 and len(lay["speedPads"]) == 2 and len(lay["pickups"]) == 3, name
        for key in ("jumpPads", "speedPads", "pickups"):
            for it in lay[key]:
                assert it["y"] is not None
                x, z = at(it["bearing"], it["r"])
                for m in marks["attackers"] + [marks["taya"], [0, 0, 0]]:
                    assert math.hypot(x - m[0], z - m[2]) > 2.0, (name, key, "sits on a mark")
    return True


# ---------------------------------------------------------------- the plan picture
def outline(p, step=3.0):
    """The piece's plan outline as (x, z) points."""
    k = p["kind"]
    if k == "disc":
        return [at(a, p["r"]) for a in frange(0, 360, step)]
    if k in ("ring", "arc"):
        a0, a1 = (0.0, 360.0) if k == "ring" else (p["a0"], p["a1"])
        return [at(a, p["r1"]) for a in frange(a0, a1, step)] + [at(a, p["r0"]) for a in frange(a1, a0, -step)]
    lo, hi = min(p["r0"], p["r1"]), max(p["r0"], p["r1"])
    h = p["width"] / 2
    a = math.radians(p["bearing"])
    pts = []
    for r, order in ((hi, 1), (lo, -1)):
        for i in range(-8, 9):
            s = h * i / 8 * order
            along = math.sqrt(max(r * r - s * s, 0.0))
            pts.append((along * math.sin(a) + s * math.cos(a), along * math.cos(a) - s * math.sin(a)))
    return pts


def frange(a0, a1, step):
    n = max(1, int(abs(a1 - a0) / abs(step)))
    return [a0 + (a1 - a0) * i / n for i in range(n + 1)]


def picture(path):
    from PIL import Image, ImageDraw, ImageFont
    S = 2                                                    # supersample
    cell, pad, head = 760 * S, 30 * S, 150 * S
    W, H = cell * 3 + pad * 4, (cell + head) * 2 + pad * 3
    img = Image.new("RGB", (W, H), (10, 13, 26))
    d = ImageDraw.Draw(img, "RGBA")

    def font(size):
        for f in ("bahnschrift.ttf", "arialbd.ttf", "arial.ttf"):
            fp = os.path.join("C:/Windows/Fonts", f)
            if os.path.exists(fp):
                return ImageFont.truetype(fp, size * S)
        return ImageFont.load_default()

    big, mid, small = font(34), font(19), font(15)
    scale = (cell / 2 - 14 * S) / 24.0                       # px per metre: 24 m from the can to the cell's edge
    blurb = {
        "plaza": "Open and broad. Three bands, two narrow slots (2 m, 1.5 m). All at y 0.",
        "tore": "The can on a drum 1.5 m up. Four ramps (23.8 deg), a ring walk, two long arcs only.",
        "krus": "A small centre, four 2.6 m catwalks, a 9 m pit in each quarter. Attackers start on the apron.",
        "hukay": "The can 1.2 m down. Wide ramps up to the walk, terraces 1 m above it.",
        "entablado": "Attackers on a crescent 1.2 m up, a 3 m moat, two lofts 3 m up by jump pad only.",
    }
    for n, lay in enumerate(LAYOUTS + [None]):
        col, row = n % 3, n // 3
        ox = pad + col * (cell + pad) + cell / 2
        oy = pad + row * (cell + head + pad) + head + cell / 2

        def px(x, z):
            return (ox + x * scale, oy - z * scale)

        if lay is None:                                      # the legend cell
            x0, y0 = ox - cell / 2 + 20 * S, oy - cell / 2 - head + 20 * S
            d.text((x0, y0), "LEGEND", font=big, fill=(235, 240, 250))
            rows = [((225, 230, 238), "deck at y 0"), ((255, 244, 214), "deck above y 0 (lighter, warmer)"),
                    ((150, 160, 185), "deck below y 0 (darker)"), ((170, 200, 215), "ramp or bridge (arrow points UP hill)"),
                    ((60, 235, 200), "jump pad (15.5 m/s, apex 6 m)"), ((185, 240, 70), "speed pad, chevrons along travel"),
                    ((190, 140, 255), "stamina pickup"), ((230, 60, 140), "the taya's mark"),
                    ((250, 250, 250), "the attackers' marks, and the can at the centre")]
            for i, (c, t) in enumerate(rows):
                yy = y0 + (70 + i * 44) * S
                d.rectangle([x0, yy, x0 + 34 * S, yy + 26 * S], fill=c + (255,))
                d.text((x0 + 50 * S, yy), t, font=mid, fill=(215, 222, 235))
            yy = y0 + (70 + len(rows) * 44 + 20) * S
            d.text((x0, yy), "The dashed square is the play wall (22 m). The dotted circle is r 22.", font=mid, fill=(170, 180, 200))
            d.text((x0, yy + 34 * S), "Bearings clockwise from north (+z, up the page). x is right.", font=mid, fill=(170, 180, 200))
            d.text((x0, yy + 68 * S), "Numbers on a piece are its height in metres when not 0.", font=mid, fill=(170, 180, 200))
            d.text((x0, yy + 102 * S), "A * marks a bonus piece: jump pad only.", font=mid, fill=(170, 180, 200))
            # the 1.8 m figure against a 5 m bar, drawn at three times the plans' scale so it can be read
            sc = scale * 3
            bx, by = x0 + 20 * S, yy + 330 * S
            d.line([bx, by, bx + 5 * sc, by], fill=(235, 240, 250), width=3 * S)
            for k in range(6):
                d.line([bx + k * sc, by - 6 * S, bx + k * sc, by + 6 * S], fill=(235, 240, 250), width=2 * S)
            d.text((bx, by + 12 * S), "5 m (this strip is 3 x the plans' scale)", font=mid, fill=(215, 222, 235))
            fx, fy = bx + 1.0 * sc, by - 4 * S
            hgt = 1.8 * sc
            d.ellipse([fx - 0.14 * sc, fy - hgt, fx + 0.14 * sc, fy - hgt + 0.28 * sc], fill=(250, 250, 250))
            d.rounded_rectangle([fx - 0.24 * sc, fy - hgt + 0.3 * sc, fx + 0.24 * sc, fy - 0.8 * sc], radius=4 * S, fill=(250, 250, 250))
            d.rectangle([fx - 0.2 * sc, fy - 0.8 * sc, fx - 0.04 * sc, fy], fill=(250, 250, 250))
            d.rectangle([fx + 0.04 * sc, fy - 0.8 * sc, fx + 0.2 * sc, fy], fill=(250, 250, 250))
            d.text((bx, by - hgt - 40 * S), "a 1.8 m figure, standing; a ramp at 25 degrees; a 2.6 m catwalk", font=mid, fill=(215, 222, 235))
            d.polygon([(bx + 2.0 * sc, by - 4 * S), (bx + 4.6 * sc, by - 4 * S), (bx + 4.6 * sc, by - 4 * S - 2.6 * sc * math.tan(math.radians(25)))], fill=(170, 200, 215))
            d.text((bx, by + 46 * S), "In the plans a mark is a 0.6 m dot: a figure from above.", font=mid, fill=(170, 180, 200))
            continue

        d.text((ox - cell / 2, oy - cell / 2 - head + 8 * S), "%d  %s" % (n, lay["name"].upper()), font=big, fill=(235, 240, 250))
        d.text((ox - cell / 2, oy - cell / 2 - head + 58 * S), blurb[lay["name"]], font=small, fill=(180, 190, 210))
        d.text((ox - cell / 2, oy - cell / 2 - head + 84 * S), "can height %.1f m, %d pieces, bonus: %s" % (
            lay["canHeight"], len(lay["pieces"]), ", ".join(lay["bonus"]) or "none"), font=small, fill=(180, 190, 210))
        d.rectangle([ox - cell / 2, oy - cell / 2, ox + cell / 2, oy + cell / 2], fill=(4, 6, 14, 255))
        # walls
        a, b = px(-WALL, WALL), px(WALL, -WALL)
        for k in range(0, 88, 2):
            t0, t1 = k / 88.0, (k + 1) / 88.0
            for (p0, p1) in (((a[0], a[1]), (b[0], a[1])), ((a[0], b[1]), (b[0], b[1])), ((a[0], a[1]), (a[0], b[1])), ((b[0], a[1]), (b[0], b[1]))):
                d.line([p0[0] + (p1[0] - p0[0]) * t0, p0[1] + (p1[1] - p0[1]) * t0, p0[0] + (p1[0] - p0[0]) * t1, p0[1] + (p1[1] - p0[1]) * t1],
                       fill=(120, 130, 160), width=2 * S)
        for k in range(0, 360, 4):
            d.line([px(*at(k, WALL)), px(*at(k + 2, WALL))], fill=(70, 80, 110), width=S)
        for p in sorted(lay["pieces"], key=lambda q: (q["kind"] == "ramp", q.get("top", 0))):
            top = p["top"] if p["kind"] != "ramp" else (p["z0"] + p["z1"]) / 2
            if p["kind"] == "ramp":
                base = (170, 200, 215)
            elif top > 0.05:
                base = (255, 244, 214)
            elif top < -0.05:
                base = (150, 160, 185)
            else:
                base = (225, 230, 238)
            pts = [px(x, z) for x, z in outline(p)]
            d.polygon(pts, fill=base + (255,))
            if p["kind"] == "ring":
                for rr in (p["r0"], p["r1"]):
                    cc = [px(*at(a_, rr)) for a_ in frange(0, 360, 3.0)]
                    d.line(cc, fill=(30, 40, 70), width=2 * S)
            else:
                d.line(pts + [pts[0]], fill=(30, 40, 70), width=2 * S)
            if p["kind"] == "ramp":
                lo_end, hi_end = (p["r0"], p["r1"]) if p["z0"] < p["z1"] else (p["r1"], p["r0"])
                if abs(p["z1"] - p["z0"]) > 0.01:
                    span = hi_end - lo_end
                    p0, p1 = px(*at(p["bearing"], lo_end + span * 0.2)), px(*at(p["bearing"], lo_end + span * 0.8))
                    d.line([p0, p1], fill=(20, 30, 60), width=3 * S)
                    ang = math.atan2(p1[1] - p0[1], p1[0] - p0[0])
                    for s in (-1, 1):
                        d.line([p1, (p1[0] - 12 * S * math.cos(ang + s * 0.5), p1[1] - 12 * S * math.sin(ang + s * 0.5))], fill=(20, 30, 60), width=3 * S)
            else:
                if p["kind"] == "disc":
                    lx, lz = at(90, p["r"] * 0.55)
                elif p["kind"] == "ring":
                    lx, lz = at(200, (p["r0"] + p["r1"]) / 2)
                else:
                    lx, lz = at((p["a0"] + p["a1"]) / 2 + (p["a1"] - p["a0"]) * 0.3, (p["r0"] + p["r1"]) / 2)
                label = p["id"] + ("*" if p["id"] in lay["bonus"] else "") + ("" if abs(top) < 0.05 else " %+.1f" % top)
                tw = d.textlength(label, font=small)
                d.text((px(lx, lz)[0] - tw / 2, px(lx, lz)[1] - 9 * S), label, font=small, fill=(20, 30, 60))
        for it in lay["speedPads"]:
            x, z = at(it["bearing"], it["r"])
            yaw = math.radians(it["bearing"] + (90.0 if it["along"] == "tangent" else 0.0))
            fx_, fz_ = math.sin(yaw), math.cos(yaw)
            sx_, sz_ = math.cos(yaw), -math.sin(yaw)
            ha, hl = it["halfSize"]
            quad = [px(x + sx_ * a_ * ha + fx_ * l_ * hl, z + sz_ * a_ * ha + fz_ * l_ * hl) for a_, l_ in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
            d.polygon(quad, fill=(185, 240, 70, 255))
            for k in (-0.5, 0.2):
                tip = px(x + fx_ * (k + 0.5) * hl, z + fz_ * (k + 0.5) * hl)
                for s in (-1, 1):
                    d.line([tip, px(x + sx_ * s * ha * 0.7 + fx_ * (k - 0.1) * hl, z + sz_ * s * ha * 0.7 + fz_ * (k - 0.1) * hl)], fill=(30, 60, 10), width=2 * S)
        for it in lay["jumpPads"]:
            x, z = at(it["bearing"], it["r"])
            c = px(x, z)
            r = 0.85 * scale
            d.ellipse([c[0] - r, c[1] - r, c[0] + r, c[1] + r], fill=(60, 235, 200, 255), outline=(10, 60, 50), width=2 * S)
        for it in lay["pickups"]:
            x, z = at(it["bearing"], it["r"])
            c = px(x, z)
            r = 0.5 * scale
            d.polygon([(c[0], c[1] - r), (c[0] + r, c[1]), (c[0], c[1] + r), (c[0] - r, c[1])], fill=(190, 140, 255, 255), outline=(40, 10, 80))
        c = px(0, 0)
        d.ellipse([c[0] - 0.22 * scale, c[1] - 0.22 * scale, c[0] + 0.22 * scale, c[1] + 0.22 * scale], fill=(250, 250, 250), outline=(20, 30, 60), width=S)
        t = DATA["spawns"]["taya"]
        c = px(t[0], t[2])
        d.ellipse([c[0] - 0.3 * scale, c[1] - 0.3 * scale, c[0] + 0.3 * scale, c[1] + 0.3 * scale], fill=(230, 60, 140))
        for m in DATA["spawns"]["attackers"]:
            c = px(m[0], m[2])
            d.ellipse([c[0] - 0.3 * scale, c[1] - 0.3 * scale, c[0] + 0.3 * scale, c[1] + 0.3 * scale], fill=(250, 250, 250), outline=(120, 80, 200), width=2 * S)
    img = img.resize((W // S, H // S), Image.LANCZOS)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)


def area(lay):
    """Floor area in square metres, by sampling."""
    step, n, total = 0.25, int(WALL / 0.25), 0
    for i in range(-n, n + 1):
        for j in range(-n, n + 1):
            if floor_at(lay, i * step + 0.011, j * step + 0.007)[0] is not None:
                total += 1
    return total * step * step


if __name__ == "__main__":
    version = "v1"
    for a in sys.argv:
        if a.startswith("--version="):
            version = a.split("=", 1)[1]
    settle()
    check()
    path = os.path.join(ROOT, "tools", "arena_layouts.json")
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(DATA, f, indent=1)
    for lay in LAYOUTS:
        print("%-10s can %+.1f  pieces %2d  floor %4.0f m2  bonus %s" % (lay["name"], lay["canHeight"], len(lay["pieces"]), area(lay), lay["bonus"]))
    pic = os.path.join(ROOT, "Logs", "arena", "stage", "layouts_plan_%s.png" % version)
    picture(pic)
    print("wrote", path)
    print("wrote", pic)
