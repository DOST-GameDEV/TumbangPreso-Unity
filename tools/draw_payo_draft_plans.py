"""PAYO (draft): writes tools/payo_layouts_draft.json and the plan sheet Logs/payo/draft/payo_draft_plans_vN.png.

    py -3 tools/draw_payo_draft_plans.py [--v=N]

A DRAFT FOR THE OWNER TO REACT TO. Nothing here is read by the game. Edit THIS file, never the JSON.

COORDINATES. Unity: x right, y up, z forward, metres, the can at the origin (as tools/arena_layouts.json).

THE IDEA. Arena's stage is hover plates that travel sideways and change shape. Stone cannot do that, so here
every piece is a MONOLITH with a fixed square or rectangular plan that only ever moves UP AND DOWN in its own
shaft. A layout is nothing but a `top` per block; a block missing from a layout has sunk under the mist.
29 blocks on one master plan:

    core            10 x 10 at the origin: the can and the taya's mark, in every layout
    slabN/E/S/W     10 x 6 against each side of the core (slabN carries the attackers' marks, in every layout)
    cornerNE..      6 x 6 in the court's corners. core + slabs + corners at one height = a 22 x 22 m court
    bridgeN/E/S/W   4 x 3 causeways over the 3 m slot between the court and the outer ring
    midN/E/S/W      10 x 8 outer blocks
    stone1..8       5 x 4 stepping stones between the mids and the towers (2 m gaps), clockwise from north north east
    towerNE..       8 x 8 corner towers

A block may be a STAIR in a layout: `stairDown` names the direction its top descends, from `top` to `low`
(the slab is cut into treads that rise by different amounts; the collider would be a ramp).

THE NUMBERS THE CHECK USES ARE THE GAME'S (Packages/com.tumbangpreso.core/Runtime/Balance.cs, read 2026-10-12):
jump 5.75 m/s against gravity 20 = an apex of 0.83 m and 0.575 s in the air; an attacker runs 5.0 m/s and
sprints 7.5 (a taya 7.5 and 11.25); the capsule steps 0.3 m. So a jump climbs 0.6 m at most, a flat running
jump carries an attacker 2.9 m and a sprinting one 4.3 m. check() classes every block:
    walk    on foot from the can (touching blocks no more than 0.3 m apart in height, stairs, bridges)
    jump    needs a running jump (reach at 5.0 m/s covers the gap plus 0.3 m of margin)
    sprint  needs a sprinting jump (7.5 m/s)
    bonus   only a geyser (jump pad) reaches it; listed in the layout's `bonus`
and asserts nothing is unreachable, that every non-bonus block can get back to the core, and that the marks
have floor.

MOVERS (second draft, owner 2026-10-12: "you can try having smaller moving platforms"). The 29 monoliths still change
only between rounds. Added to them, a layout may carry a few small stone slabs (3 x 3 or 4 x 4) that move DURING the
round, kind "mover": they travel a straight line between `from` and `to` (Unity x, y, z of the slab's centre and top),
and their place is a closed form of the match clock, nothing sent:
    u = (t / period + phase) mod 1                 travel = period / 2 - dwell
    u in [0, travel/period)            going,  f = smoothstep(u * period / travel)
    then `dwell` seconds resting at `to`, then coming back the same way, then `dwell` seconds resting at `from`
THE CHECK IGNORES MOVERS when it classes the blocks, so every layout is passable with every mover at its worst
position; a mover only ever adds a way. It asserts a mover never overlaps a standing block in plan.
"""
import json, math, os, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
VER = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--v=")), "1")
WALL = 22.0            # the play walls: a square, half this
PIT = 30.0             # the shaft: a square, half this
LIP = 33.0             # the pit's upper step: a square, half this (a 3 m ledge 4 m down runs round the shaft)
FLOOR = -45.0          # the shaft's drawn floor; the mist lies at -26
SUNK = -30.0           # where a block's top waits when it is out of a layout
JUMP_V, GRAV, RUN, SPRINT, STEP = 5.75, 20.0, 5.0, 7.5, 0.3
GEYSER = 15.5          # Arena's pad speed: apex 6.0 m

PLAN = {"core": (0, 0, 10, 10)}
for n, (x, z, sx, sz) in {"N": (0, 8, 10, 6), "S": (0, -8, 10, 6), "E": (8, 0, 6, 10), "W": (-8, 0, 6, 10)}.items():
    PLAN["slab" + n] = (x, z, sx, sz)
for n, (x, z) in {"NE": (8, 8), "SE": (8, -8), "SW": (-8, -8), "NW": (-8, 8)}.items():
    PLAN["corner" + n] = (x, z, 6, 6)
    PLAN["tower" + n] = (x / 8 * 18, z / 8 * 18, 8, 8)
for n, (x, z, sx, sz) in {"N": (0, 12.5, 4, 3), "S": (0, -12.5, 4, 3), "E": (12.5, 0, 3, 4), "W": (-12.5, 0, 3, 4)}.items():
    PLAN["bridge" + n] = (x, z, sx, sz)
for n, (x, z, sx, sz) in {"N": (0, 18, 10, 8), "S": (0, -18, 10, 8), "E": (18, 0, 8, 10), "W": (-18, 0, 8, 10)}.items():
    PLAN["mid" + n] = (x, z, sx, sz)
for i, (x, z, sx, sz) in enumerate([(9.5, 16, 5, 4), (16, 9.5, 4, 5), (16, -9.5, 4, 5), (9.5, -16, 5, 4),
                                    (-9.5, -16, 5, 4), (-16, -9.5, 4, 5), (-16, 9.5, 4, 5), (-9.5, 16, 5, 4)]):
    PLAN["stone%d" % (i + 1)] = (x, z, sx, sz)


def layout(name, blurb, can, tops, stairs=None, bonus=(), jump=(), speed=(), pickups=(), movers=()):
    pieces = []
    for pid, (x, z, sx, sz) in PLAN.items():
        if pid not in tops:
            continue
        p = {"id": pid, "kind": "block", "x": x, "z": z, "sx": sx, "sz": sz, "top": tops[pid], "shaftTo": FLOOR}
        if stairs and pid in stairs:
            p["stairDown"], p["low"] = stairs[pid]
        pieces.append(p)
    return {"name": name, "blurb": blurb, "canHeight": can, "bonus": list(bonus), "pieces": pieces,
            "jumpPads": [{"x": x, "z": z, "y": None, "speed": GEYSER, "serves": s} for (x, z, s) in jump],
            "speedPads": [{"x": x, "z": z, "y": None, "along": a, "halfSize": [0.75, 1.5]} for (x, z, a) in speed],
            "pickups": [{"x": x, "z": z, "y": None} for (x, z) in pickups],
            "movers": [dict(m, kind="mover") for m in movers]}


def mover(mid, sx, sz, frm, to, period, phase, dwell, serves=None, note=""):
    return {"id": mid, "sx": sx, "sz": sz, "from": list(frm), "to": list(to), "period": period, "phase": phase,
            "dwell": dwell, "serves": serves, "note": note}


def tops(**groups):
    out = {}
    for key, v in groups.items():
        for pid in PLAN:
            if pid == key or (pid.startswith(key) and pid[len(key):] in ("N", "E", "S", "W", "NE", "SE", "SW", "NW")
                              or (key == "stone" and pid.startswith("stone"))):
                out[pid] = v
    return out


# ---------------------------------------------------------------- 0. LIWASAN: the whole court up
L0 = tops(core=0.0, slab=0.0, corner=0.0, bridge=0.0, mid=0.0, stone=0.3)
L0.update(towerNE=0.6, towerSW=0.6, towerNW=4.5, towerSE=4.5)
del L0["stone8"], L0["stone4"]
liwasan = layout("liwasan", "The plaza. Every court block level: a 22 m square, four causeways, a full outer ring. Two "
                            "slabs ride up and across from the north and south blocks to the high towers.",
                 0.0, L0, bonus=("towerNW", "towerSE"),
                 movers=(mover("liftNW", 3, 3, (-6.5, 0.0, 16), (-12.5, 4.5, 16), 16.0, 0.0, 3.0, "towerNW",
                               "rides from the north block up and over to the north west tower"),
                         mover("liftSE", 3, 3, (6.5, 0.0, -16), (12.5, 4.5, -16), 16.0, 0.5, 3.0, "towerSE",
                               "rides from the south block up and over to the south east tower")),
                 jump=((8, 8, None), (-8, -8, None)),
                 speed=((18, 0, "+z"), (-18, 0, "-z")),
                 pickups=((-18, 18), (18, -18), (0, -18)))

# ---------------------------------------------------------------- 1. HAGDAN: the can on a stepped mound
L1 = tops(core=1.2, corner=0.0)
L1.update(slabN=0.0, slabS=0.6, slabE=1.2, slabW=1.2, bridgeE=0.0, bridgeW=0.0, midE=0.0, midW=0.0, midN=4.2, midS=4.2,
          stone1=0.3, stone2=0.3, stone5=0.3, stone6=0.3, towerNE=0.6, towerSW=0.6)
hagdan = layout("hagdan", "The stairs. The core lifts the can 1.2 m; the east and west slabs become stairs; the north "
                          "and south causeways sink, so the way out is east and west.",
                1.2, L1, stairs={"slabE": ("+x", 0.0), "slabW": ("-x", 0.0)}, bonus=("midN", "midS"),
                movers=(mover("ferryN", 3, 3, (0, 0.15, 12.5), (9.5, 0.15, 12.5), 17.0, 0.0, 2.5, None,
                              "shuttles along the north slot: a moving causeway from the court to stone1"),
                        mover("ferryS", 3, 3, (0, 0.15, -12.5), (-9.5, 0.15, -12.5), 17.0, 0.5, 2.5, None,
                              "shuttles along the south slot: a moving causeway from the court to stone5")),
                jump=((9.5, 16, "midN"), (-9.5, -16, "midS")),
                speed=((-8, 8, "+x"), (8, -8, "-x")),
                pickups=((0, 18), (0, -18), (0, -8)))

# ---------------------------------------------------------------- 2. PULO: the court broken into islands
L2 = tops(core=0.0, stone=0.3, tower=0.6)
L2.update(slabN=0.0, slabS=0.0, cornerNW=0.3, cornerSE=0.3, bridgeN=0.0, bridgeS=0.0, midN=0.0, midS=0.0, midE=4.2, midW=4.2)
del L2["stone3"], L2["stone7"]
pulo = layout("pulo", "The islands. The east and west slabs and two corners sink: the court is a 10 m spine with "
                      "open air on both sides, and the outer ring is a chain of hops.",
              0.0, L2, bonus=("midE", "midW"),
              movers=(mover("pistonEa", 4, 4, (7, -0.6, 3), (7, 0.0, 3), 12.0, 0.0, 3.0, None, "east wing, north half"),
                      mover("pistonEb", 4, 4, (7, -0.6, -3), (7, 0.0, -3), 12.0, 0.5, 3.0, None, "east wing, south half, in turn"),
                      mover("pistonWa", 4, 4, (-7, -0.6, -3), (-7, 0.0, -3), 12.0, 0.0, 3.0, None, "west wing, south half"),
                      mover("pistonWb", 4, 4, (-7, -0.6, 3), (-7, 0.0, 3), 12.0, 0.5, 3.0, None, "west wing, north half, in turn")),
              jump=((16, 9.5, "midE"), (-16, -9.5, "midW")),
              speed=((0, 18, "+x"), (0, -18, "-x")),
              pickups=((18, 0), (-18, 0), (8, -8)))

# ---------------------------------------------------------------- 3. LUSONG: the mortar, a court that dips to the can
# (owner 2026-10-12: five layouts, as Arena.) Named for the rice mortar. The court is a shallow bowl in 0.3 m steps:
# corners at 0, slabs one step down, the core two steps down with the can at -0.6. Every step is walkable, so the
# whole court is still "walk", but a slipper thrown from the rim comes down at the can and the taya looks up at
# everyone. The outer ring climbs the other way: mids +0.3, stones +0.6, and the north east and south west towers
# are the bonus blocks.
L3 = tops(core=-0.6, slab=-0.3, corner=0.0, bridge=0.0, mid=0.3, stone=0.6, tower=0.6)
L3.update(towerNE=4.5, towerSW=4.5)
lusong = layout("lusong", "The mortar. The court dips to the can in two 0.3 m steps (corners 0, slabs -0.3, core -0.6) and the outer "
                          "ring climbs away from it. Everything on the court is still on foot.",
                -0.6, L3, bonus=("towerNE", "towerSW"),
                jump=((9.5, 16, "towerNE"), (-9.5, -16, "towerSW")),
                speed=((8, -8, "-x"), (-8, 8, "+x")),
                pickups=((18, 18), (-18, -18), (0, -8)))

# ---------------------------------------------------------------- 4. BAITANG: the tiers, the whole stage a staircase to the gate
# Rows of blocks step up 0.3 m at a time from the valley side to the gate side, like the paddies themselves: the north
# outer row and the court's north row at 0, the core's row +0.3 (the can at +0.3), the court's south row +0.6, the
# south outer row +0.9, its two towers +1.2. The attackers start at the bottom and climb to the can; the taya has the
# slope behind it. The two NORTH towers are the bonus blocks, standing either side of the view down the valley.
L4 = tops(core=0.3, corner=0.0, stone=0.3, mid=0.3, bridge=0.3, tower=1.2)
L4.update(slabN=0.0, cornerNE=0.0, cornerNW=0.0, bridgeN=0.0, midN=0.0, stone1=0.0, stone8=0.0,
          slabE=0.3, slabW=0.3, bridgeE=0.3, bridgeW=0.3, midE=0.3, midW=0.3, stone2=0.3, stone7=0.3,
          slabS=0.6, cornerSE=0.6, cornerSW=0.6, stone3=0.6, stone6=0.6, bridgeS=0.75,
          midS=0.9, stone4=0.9, stone5=0.9, towerSE=1.2, towerSW=1.2, towerNE=4.5, towerNW=4.5)
baitang = layout("baitang", "The tiers. Rows of blocks step up 0.3 m at a time from the valley side to the gate side, as the paddies "
                            "do: the attackers climb to the can, and the two north towers stand high either side of the view.",
                 0.3, L4, bonus=("towerNE", "towerNW"),
                 jump=((9.5, 16, "towerNE"), (-9.5, 16, "towerNW")),
                 speed=((18, 0, "-z"), (-18, 0, "+z")),
                 pickups=((18, 18), (-18, 18), (0, -18)))

LAYOUTS = [liwasan, hagdan, pulo, lusong, baitang]
SPAWNS = {"taya": [0, 0, -2.5], "attackers": [[-1.8, 0, 9], [0, 0, 9], [1.8, 0, 9]]}


# ---------------------------------------------------------------- geometry
def rect(p):
    return p["x"] - p["sx"] / 2, p["x"] + p["sx"] / 2, p["z"] - p["sz"] / 2, p["z"] + p["sz"] / 2


def height_on(p, x, z, inset=0.0):
    x0, x1, z0, z1 = rect(p)
    if not (x0 + inset <= x <= x1 - inset and z0 + inset <= z <= z1 - inset):
        return None
    if "stairDown" in p:
        d = p["stairDown"]
        t = {"+x": (x - x0) / (x1 - x0), "-x": (x1 - x) / (x1 - x0), "+z": (z - z0) / (z1 - z0), "-z": (z1 - z) / (z1 - z0)}[d]
        return p["top"] + (p["low"] - p["top"]) * t
    return p["top"]


def floor_at(lay, x, z, inset=0.0):
    best = (None, None)
    for p in lay["pieces"]:
        h = height_on(p, x, z, inset)
        if h is not None and (best[0] is None or h > best[0]):
            best = (h, p["id"])
    return best


def edge_height(p, side):
    """The block's top along the side facing `side` (+x, -x, +z, -z): (low, high) of that edge."""
    if "stairDown" not in p:
        return p["top"], p["top"]
    d = p["stairDown"]
    if side == d:
        return p["low"], p["low"]
    if side[1] == d[1]:
        return p["top"], p["top"]
    return min(p["low"], p["top"]), max(p["low"], p["top"])


def reach(dh, speed):
    """How far a jump carries at `speed` when it must land `dh` higher (negative: lower). None if it cannot climb."""
    disc = JUMP_V * JUMP_V - 2 * GRAV * dh
    if disc < 0:
        return None
    return speed * (JUMP_V + math.sqrt(disc)) / GRAV


def link(a, b):
    """How a body gets from block a to block b: 'walk', 'jump', 'sprint' or None. Also the gap."""
    ax0, ax1, az0, az1 = rect(a)
    bx0, bx1, bz0, bz1 = rect(b)
    ox = min(ax1, bx1) - max(ax0, bx0)
    oz = min(az1, bz1) - max(az0, bz0)
    if ox >= 1.5 and oz < 0:
        gap, side = -oz, ("+z" if bz0 >= az1 - 1e-6 else "-z")
    elif oz >= 1.5 and ox < 0:
        gap, side = -ox, ("+x" if bx0 >= ax1 - 1e-6 else "-x")
    elif ox >= 1.5 and abs(oz) < 1e-6:
        gap, side = 0.0, ("+z" if bz0 >= az1 - 1e-6 else "-z")
    elif oz >= 1.5 and abs(ox) < 1e-6:
        gap, side = 0.0, ("+x" if bx0 >= ax1 - 1e-6 else "-x")
    else:
        return None, None
    back = {"+x": "-x", "-x": "+x", "+z": "-z", "-z": "+z"}[side]
    alo, ahi = edge_height(a, side)
    blo, bhi = edge_height(b, back)
    if gap < 1e-6:
        # touching: somewhere along the shared edge the two tops must be within a step, or a jump
        best = min(abs(bl - al) for al in (alo, ahi) for bl in (blo, bhi)) if (alo != ahi or blo != bhi) else abs(blo - alo)
        if alo != ahi or blo != bhi:
            lo, hi = max(alo, blo), min(ahi, bhi)
            best = 0.0 if lo <= hi else best
        up = blo - ahi if blo > ahi else 0.0
        if best <= STEP + 1e-6:
            return "walk", 0.0
        if up <= 0.6 + 1e-6 or bhi < alo:
            return "jump", 0.0
        return None, 0.0
    dh = blo - ahi                                   # the best case along the edge
    if dh > 0.6 + 1e-6 or dh < -8.0:
        return None, gap
    for kind, v in (("jump", RUN), ("sprint", SPRINT)):
        r = reach(dh, v)
        if r is not None and r >= gap + 0.3:
            return kind, gap
    return None, gap


RANK = {"walk": 0, "jump": 1, "sprint": 2}


def classify(lay):
    """Each block's class: the cheapest worst step on a route from the core. Bonus blocks by pad only."""
    P = {p["id"]: p for p in lay["pieces"]}
    cost = {"core": 0}
    todo = ["core"]
    while todo:
        a = todo.pop(0)
        for b in P:
            if b == a or b in lay["bonus"] or a in lay["bonus"]:
                continue
            k, _ = link(P[a], P[b])
            if k is None:
                continue
            c = max(cost[a], RANK[k])
            if b not in cost or c < cost[b]:
                cost[b] = c
                todo.append(b)
    names = {0: "walk", 1: "jump", 2: "sprint"}
    return {b: names[c] for b, c in cost.items()}


def can_return(lay, start):
    P = {p["id"]: p for p in lay["pieces"]}
    seen, todo = {start}, [start]
    while todo:
        a = todo.pop()
        if a == "core":
            return True
        for b in P:
            if b not in seen and link(P[a], P[b])[0] is not None:
                seen.add(b)
                todo.append(b)
    return False


def settle_and_check():
    report = []
    for lay in LAYOUTS:
        ids = [p["id"] for p in lay["pieces"]]
        assert len(ids) == len(set(ids))
        for p in lay["pieces"]:
            x0, x1, z0, z1 = rect(p)
            assert max(abs(x0), abs(x1), abs(z0), abs(z1)) <= WALL + 1e-6, p["id"]
        h, pid = floor_at(lay, 0, 0, 0.45)
        assert pid == "core" and abs(h - lay["canHeight"]) < 1e-6 and -1.5 <= h <= 1.5, lay["name"]
        h, pid = floor_at(lay, SPAWNS["taya"][0], SPAWNS["taya"][2], 0.45)
        assert pid == "core", lay["name"]
        for (x, _, z) in SPAWNS["attackers"]:
            h, pid = floor_at(lay, x, z, 0.45)
            assert h is not None and -4.0 <= h <= 2.0 and "stairDown" not in [p for p in lay["pieces"] if p["id"] == pid][0], lay["name"]
        for group in ("jumpPads", "speedPads", "pickups"):
            for it in lay[group]:
                h, pid = floor_at(lay, it["x"], it["z"], 0.5)
                assert h is not None, (lay["name"], group, it)
                it["y"], it["on"] = round(h, 3), pid
        assert len(lay["jumpPads"]) == 2 and len(lay["speedPads"]) == 2 and len(lay["pickups"]) == 3
        cls = classify(lay)
        P = {p["id"]: p for p in lay["pieces"]}
        for b in lay["bonus"]:
            assert b not in cls, (lay["name"], b, "a bonus block is reachable without its geyser")
            cls[b] = "bonus"
            lift = [m for m in lay["movers"] if m["serves"] == b]
            if lift:
                assert abs(lift[0]["to"][1] - P[b]["top"]) < 1e-6, (lay["name"], b, "the lift stops short")
                continue
            pad = [j for j in lay["jumpPads"] if j["serves"] == b]
            assert pad, (lay["name"], b, "no geyser serves it")
            apex = pad[0]["y"] + pad[0]["speed"] ** 2 / (2 * GRAV)
            assert apex >= P[b]["top"] + 1.0, (lay["name"], b, "the geyser does not clear it")
            assert pad[0]["on"] in cls and cls[pad[0]["on"]] != "bonus"
            assert link(P[pad[0]["on"]], P[b])[1] is not None and link(P[pad[0]["on"]], P[b])[1] <= 2.5, (lay["name"], b)
        for pid in ids:
            assert pid in cls, (lay["name"], pid, "UNREACHABLE")
            if cls[pid] != "bonus":
                assert can_return(lay, pid), (lay["name"], pid, "no way back to the core")
        for m in lay["movers"]:                     # a mover never passes through a standing block
            for k in range(21):
                f = k / 20.0
                mx = m["from"][0] + (m["to"][0] - m["from"][0]) * f
                mz = m["from"][2] + (m["to"][2] - m["from"][2]) * f
                for p in lay["pieces"]:
                    x0, x1, z0, z1 = rect(p)
                    ox = min(x1, mx + m["sx"] / 2) - max(x0, mx - m["sx"] / 2)
                    oz = min(z1, mz + m["sz"] / 2) - max(z0, mz - m["sz"] / 2)
                    assert not (ox > 1e-6 and oz > 1e-6), (lay["name"], m["id"], "passes through", p["id"])
            assert max(abs(v) for v in (m["from"][0], m["to"][0], m["from"][2], m["to"][2])) + 2.0 <= WALL
        lay["reach"] = {pid: cls[pid] for pid in ids}
        count = {k: sum(1 for v in cls.values() if v == k) for k in ("walk", "jump", "sprint", "bonus")}
        report.append("%-8s %2d blocks up (%d sunk): walk %d, jump %d, sprint %d, bonus %d" % (
            lay["name"], len(ids), len(PLAN) - len(ids), count["walk"], count["jump"], count["sprint"], count["bonus"]))
    return report


DATA = {
    "notes": "PAYO DRAFT, NOT READ BY THE GAME. WRITTEN BY tools/draw_payo_draft_plans.py: edit that, not this. Unity "
             "coordinates (x right, y up, z forward), metres, the can at the origin. Every piece is kind 'block': a "
             "monolith with a FIXED plan (centre x, z; size sx, sz) whose walking surface is at 'top' and whose shaft "
             "goes down to 'shaftTo'. A block keeps its id and its plan across layouts and only rises or sinks; an id "
             "missing from a layout waits with its top at 'sunkTop', under the mist. 'stairDown' (+x, -x, +z, -z) makes "
             "the top a flight that descends that way from 'top' to 'low'. Pads and pickups are placed by x, z (not "
             "bearing and radius: the stage is a grid); a speed pad's 'along' is its direction of travel. 'bonus' lists "
             "the blocks only a geyser (jump pad) or a lift reaches; 'reach' is the check's class for every block (walk, jump, "
             "sprint, bonus). 'movers' are small slabs that move DURING the round, kind 'mover': the centre of the slab's "
             "top travels the straight line 'from' to 'to' (x, y, z), out and back once every 'period' seconds, resting "
             "'dwell' seconds at each end, offset by 'phase' (a fraction of the period), a closed form of the match clock. "
             "The reach classes ignore movers: each layout is passable with every mover at its worst position.",
    "stageTop": 0.0, "wallHalf": WALL, "pitHalf": PIT, "pitLipHalf": LIP, "pitFloor": FLOOR, "mistY": -26.0,
    "catchY": -22.0, "sunkTop": SUNK,
    "spawns": SPAWNS,
    "layouts": LAYOUTS,
}


# ---------------------------------------------------------------- the sheet
def draw(path):
    from PIL import Image, ImageDraw, ImageFont

    def font(size, bold=False):
        for name in (("arialbd.ttf",) if bold else ()) + ("arial.ttf", "DejaVuSans.ttf"):
            try:
                return ImageFont.truetype(name, size)
            except OSError:
                pass
        return ImageFont.load_default()

    S = 12.0                                   # pixels a metre
    PW, PH, TOP, STRIP = 840, 840, 176, 520
    W, H = PW * len(LAYOUTS) + 80, TOP + PH + 76 + STRIP
    im = Image.new("RGB", (W, H), (236, 233, 224))
    d = ImageDraw.Draw(im)
    F, FB, FS, FT, FH = font(15), font(15, True), font(12), font(40, True), font(24, True)
    INK, MIST, TERR = (38, 40, 42), (196, 208, 204), (156, 190, 120)
    OCHRE, RED, STEAM = (196, 146, 44), (160, 52, 40), (120, 176, 170)

    d.text((40, 24), "PAYO: five layouts and their movers", font=FT, fill=INK)
    d.text((42, 76), "One master plan of 29 stone blocks. A block never moves sideways: a layout is a height for each block, and a block that is "
                     "out of a layout waits under the mist. Small teal slabs move DURING a round. Heights in metres.", font=F, fill=INK)
    d.text((42, 98), "Reach classes use the game's own jump: apex 0.83 m, 2.9 m flat at a run, 4.3 m at a sprint. Walk = on foot from the can. "
                     "Nothing here has run in the game.", font=F, fill=INK)

    d.text((42, 120), "LIGHT: the first half is DAY (Apolaki's sun). At HALF TIME, while the blocks move, the map turns to NIGHT (Mayari's moon, fireflies, shooting stars) "
                      "and stays night. Layout order is shuffled per match as in Arena, so any layout can fall in either half.", font=F, fill=(36, 60, 128))

    def shade(top):
        v = int(140 + min(1.0, max(-0.2, top / 1.2)) * 34 + max(0.0, top - 1.2) / 3.3 * 40)
        return (v, v + 2, v + 4)

    for li, lay in enumerate(LAYOUTS):
        ox, oy = 40 + li * PW, TOP
        cx, cy = ox + PW / 2 - 10, oy + PH / 2 + 14

        def px(x, z):
            return cx + x * S, cy - z * S

        def box(x0, x1, z0, z1, **kw):
            a, b = px(x0, z1), px(x1, z0)
            d.rectangle([a[0], a[1], b[0], b[1]], **kw)

        box(-34.4, 34.4, -33.6, 33.6, fill=TERR)
        for k in range(3):                      # a hint of terraces round the plan
            r = 34.2 - k * 0.45
            box(-r, r, -r + 0.6, r - 0.6, outline=(120, 160, 96))
        box(-LIP, LIP, -LIP, LIP, fill=(150, 152, 150), outline=INK)
        box(-PIT, PIT, -PIT, PIT, fill=MIST, outline=INK, width=2)
        a, b = px(-WALL, WALL), px(WALL, -WALL)
        for t in range(0, 44 * 2, 2):           # the play walls, dashed
            u = -WALL + t * 0.5
            for (p0, p1) in ((px(u, WALL), px(u + 0.5, WALL)), (px(u, -WALL), px(u + 0.5, -WALL)),
                             (px(WALL, u), px(WALL, u + 0.5)), (px(-WALL, u), px(-WALL, u + 0.5))):
                d.line([p0, p1], fill=(110, 110, 120), width=1)
        up = {p["id"]: p for p in lay["pieces"]}
        for pid, (x, z, sx, sz) in PLAN.items():
            if pid in up:
                continue
            x0, x1, z0, z1 = x - sx / 2, x + sx / 2, z - sz / 2, z + sz / 2
            a, b = px(x0, z1), px(x1, z0)
            d.rectangle([a[0], a[1], b[0], b[1]], outline=(168, 182, 180))
            d.line([a, b], fill=(168, 182, 180))
        for pid, p in up.items():
            x0, x1, z0, z1 = rect(p)
            cls = lay["reach"][pid]
            a, b = px(x0, z1), px(x1, z0)
            d.rectangle([a[0], a[1], b[0], b[1]], fill=shade(p["top"]),
                        outline=OCHRE if cls == "bonus" else INK, width=4 if cls == "bonus" else 2)
            if "stairDown" in p:
                n = 6
                for k in range(1, n):
                    t = k / n
                    if p["stairDown"][1] == "x":
                        xx = x0 + (x1 - x0) * t
                        d.line([px(xx, z0), px(xx, z1)], fill=INK)
                    else:
                        zz = z0 + (z1 - z0) * t
                        d.line([px(x0, zz), px(x1, zz)], fill=INK)
                label = "%+.1f > %s" % (p["top"], ("%+.1f" % p["low"]) if p["low"] else "0")
            else:
                label = "%+.1f" % p["top"] if p["top"] else "0"
            m = px(p["x"], p["z"])
            small = min(p["sx"], p["sz"]) < 5
            busy = any(abs(it["x"] - p["x"]) < 2.2 and abs(it["z"] - p["z"]) < 2.2
                       for g in ("jumpPads", "speedPads", "pickups") for it in lay[g])
            if busy and not small:                  # something sits on the middle: the label goes to the top left corner
                tag = {"walk": "", "jump": " jump", "sprint": " sprint", "bonus": " BONUS"}[cls]
                d.text((a[0] + 6, a[1] + 5), label + tag, font=FB, fill=(96, 70, 20) if cls == "bonus" else INK)
            elif busy:
                pass
            elif "stairDown" in p:
                d.rectangle([m[0] - 30, m[1] - 9, m[0] + 30, m[1] + 9], fill=shade(p["top"]))
                d.text(m, label, font=FS, fill=INK, anchor="mm")
            elif pid == "core":
                d.text((m[0] + 30, m[1] - 52), label, font=FB, fill=INK, anchor="mm")
            else:
                d.text((m[0], m[1] - (0 if small else 7)), label, font=FB if not small else FS, fill=INK, anchor="mm")
                if not small:
                    tag = {"walk": "", "jump": "jump", "sprint": "sprint", "bonus": "BONUS"}[cls]
                    d.text((m[0], m[1] + 9), tag, font=FS, fill=(96, 70, 20) if cls == "bonus" else (70, 74, 80), anchor="mm")
                elif cls in ("jump", "sprint") and pid.startswith("stone"):
                    pass
        # the can, the marks
        c = px(0, 0)
        d.ellipse([c[0] - 16, c[1] - 16, c[0] + 16, c[1] + 16], outline=(250, 250, 244), width=2)
        d.ellipse([c[0] - 4, c[1] - 4, c[0] + 4, c[1] + 4], fill=(40, 40, 40))
        t = px(0, -2.5)
        d.polygon([(t[0], t[1] - 7), (t[0] - 6, t[1] + 5), (t[0] + 6, t[1] + 5)], fill=(40, 40, 40))
        for (x, _, z) in SPAWNS["attackers"]:
            q = px(x, z)
            d.ellipse([q[0] - 4, q[1] - 4, q[0] + 4, q[1] + 4], fill=(250, 250, 244), outline=INK)
        for j in lay["jumpPads"]:
            q = px(j["x"], j["z"])
            d.ellipse([q[0] - 10, q[1] - 10, q[0] + 10, q[1] + 10], fill=STEAM, outline=INK)
            d.text(q, "G", font=FB, fill=INK, anchor="mm")
            if not j["serves"]:
                continue
            tgt = up[j["serves"]]
            e = px(tgt["x"], tgt["z"])
            vx, vy = e[0] - q[0], e[1] - q[1]
            n = math.hypot(vx, vy)
            for k in range(2, 9):
                s = 12 + (n - 60) * k / 9
                d.ellipse([q[0] + vx / n * s - 2, q[1] + vy / n * s - 2, q[0] + vx / n * s + 2, q[1] + vy / n * s + 2], fill=(60, 110, 106))
        for sp in lay["speedPads"]:
            q = px(sp["x"], sp["z"])
            dx, dz = {"+x": (1, 0), "-x": (-1, 0), "+z": (0, 1), "-z": (0, -1)}[sp["along"]]
            hx, hz = (1.5 if dx else 0.75), (1.5 if dz else 0.75)
            a, b = px(sp["x"] - hx, sp["z"] + hz), px(sp["x"] + hx, sp["z"] - hz)
            d.rectangle([a[0], a[1], b[0], b[1]], fill=OCHRE, outline=INK)
            for k in (-1, 0, 1):
                tip = (q[0] + dx * (k * 10 + 6), q[1] - dz * (k * 10 + 6))
                d.line([(tip[0] - dx * 6 - dz * 6, tip[1] + dz * 6 - dx * 6), tip, (tip[0] - dx * 6 + dz * 6, tip[1] + dz * 6 + dx * 6)], fill=INK, width=2)
        for pk in lay["pickups"]:
            q = px(pk["x"], pk["z"])
            d.regular_polygon((q[0], q[1], 8), 4, rotation=45, fill=RED, outline=INK)

        MOV = (36, 112, 128)
        for m in lay["movers"]:                     # movers: solid at `from`, dashed at `to`, the travel between
            (fx, fy, fz), (tx, ty, tz) = m["from"], m["to"]
            hx, hz = m["sx"] / 2, m["sz"] / 2
            a, b = px(fx - hx, fz + hz), px(fx + hx, fz - hz)
            d.rectangle([a[0], a[1], b[0], b[1]], fill=(150, 196, 202), outline=MOV, width=3)
            if (fx, fz) != (tx, tz):
                a2, b2 = px(tx - hx, tz + hz), px(tx + hx, tz - hz)
                for t in range(0, 12):
                    u0, u1 = t / 12.0, (t + 0.5) / 12.0
                    for (p0, p1) in (((a2[0], a2[1]), (b2[0], a2[1])), ((a2[0], b2[1]), (b2[0], b2[1])),
                                     ((a2[0], a2[1]), (a2[0], b2[1])), ((b2[0], a2[1]), (b2[0], b2[1]))):
                        d.line([(p0[0] + (p1[0] - p0[0]) * u0, p0[1] + (p1[1] - p0[1]) * u0),
                                (p0[0] + (p1[0] - p0[0]) * u1, p0[1] + (p1[1] - p0[1]) * u1)], fill=MOV, width=2)
                q0, q1 = px(fx, fz), px(tx, tz)
                d.line([q0, q1], fill=MOV, width=3)
                vx, vy = q1[0] - q0[0], q1[1] - q0[1]
                n = math.hypot(vx, vy)
                vx, vy = vx / n, vy / n
                for (q, sgn) in ((q1, 1), (q0, -1)):
                    d.polygon([(q[0], q[1]), (q[0] - sgn * vx * 10 - vy * 6, q[1] - sgn * vy * 10 + vx * 6),
                               (q[0] - sgn * vx * 10 + vy * 6, q[1] - sgn * vy * 10 - vx * 6)], fill=MOV)
                text = "%+.1f > %+.1f" % (fy, ty) if fy != ty else "%+.2f" % fy
                d.text(((q0[0] + q1[0]) / 2, (q0[1] + q1[1]) / 2 + 14), text.replace("+0.0", "0"), font=FS, fill=MOV, anchor="mm")
            else:
                q0 = px(fx, fz)
                d.line([(q0[0], q0[1] - 12), (q0[0], q0[1] + 12)], fill=MOV, width=3)
                for sgn in (-1, 1):
                    d.polygon([(q0[0], q0[1] + sgn * 16), (q0[0] - 6, q0[1] + sgn * 7), (q0[0] + 6, q0[1] + sgn * 7)], fill=MOV)
                d.text((q0[0] + 16, q0[1]), "%+.1f" % fy if m["phase"] else "0", font=FS, fill=MOV, anchor="lm")

        # dimensions that matter
        def dim(p0, p1, text, off=(0, -12)):
            a, b = px(*p0), px(*p1)
            d.line([a, b], fill=RED, width=2)
            for q in (a, b):
                d.line([(q[0] - 4, q[1] - 4), (q[0] + 4, q[1] + 4)], fill=RED, width=2)
            c0 = ((a[0] + b[0]) / 2 + off[0], (a[1] + b[1]) / 2 + off[1])
            tw = d.textlength(text, font=FS) / 2 + 3
            d.rectangle([c0[0] - tw, c0[1] - 7, c0[0] + tw, c0[1] + 7], fill=(244, 240, 230))
            d.text(c0, text, font=FS, fill=RED, anchor="mm")

        if li == 0:
            dim((-3.4, 11), (-3.4, 14), "3.0", off=(-14, 0))
            dim((5, 20.6), (7, 20.6), "2.0")
            dim((12, 20.6), (14, 20.6), "2.0")
            dim((-11, -11.9), (-2.4, -11.9), "court 22 x 22 m", off=(0, 12))
            dim((22, -25), (30, -25), "8.0 open", off=(0, 11))
            dim((-30, -31.6), (30, -31.6), "pit 60 x 60, 45 deep; fall caught at -22", off=(0, -10))
        if li == 1:
            dim((-5, -4.2), (5, -4.2), "dais 10 x 10", off=(0, -10))
            dim((11, 12.4), (11, 14), "3.0", off=(16, 0))
        if li == 2:
            dim((-5, 3.4), (5, 3.4), "spine 10 wide", off=(0, -11))
            dim((18.6, 5), (18.6, 7), "2.0", off=(14, 0))
        d.text((ox + 6, oy - 4), "%d. %s" % (li + 1, lay["name"].upper()), font=FH, fill=INK)
        d.text((ox + 190, oy + 4), "can %+.1f m" % lay["canHeight"] if lay["canHeight"] else "can at 0", font=F, fill=INK)
        # the blurb, wrapped
        words, line, yy = lay["blurb"].split(), "", oy + PH + 22
        for wd in words:
            if d.textlength(line + " " + wd, font=F) > PW - 60:
                d.text((ox + 6, yy), line, font=F, fill=INK); yy += 19; line = wd
            else:
                line = (line + " " + wd).strip()
        d.text((ox + 6, yy), line, font=F, fill=INK)
        yy += 22
        seen = set()
        for m in lay["movers"]:
            kind = m["id"].rstrip("NSEWab")
            if kind in seen:
                continue
            seen.add(kind)
            n = sum(1 for q in lay["movers"] if q["id"].rstrip("NSEWab") == kind)
            dist = math.dist(m["from"], m["to"])
            travel = m["period"] / 2 - m["dwell"]
            d.text((ox + 6, yy), "MOVING: %d x %s, %g x %g m, %.1f m in %.1f s (%.1f m/s), rests %.1f s at each end, period %g s, the pair half a period apart."
                   % (n, kind, m["sx"], m["sz"], dist, travel, dist / travel, m["dwell"], m["period"]), font=FS, fill=(36, 112, 128))
            yy += 16

    # ---------------- the legend and the rising strip
    y0 = TOP + PH + 112
    d.line([(40, y0 - 14), (W - 40, y0 - 14)], fill=(150, 150, 150))
    lx, ly = 44, y0
    d.text((lx, ly), "KEY", font=FH, fill=INK); ly += 40
    rows = [("can and its chalk ring; triangle = taya's mark (0, -2.5); pale dots = attackers' marks (z +9)", "can"),
            ("G = geyser (jump pad, apex 6 m); dots point at the block it serves", "g"),
            ("ochre slab with chevrons = speed pad and its direction", "s"),
            ("red diamond = stamina pickup", "p"),
            ("ochre outline = BONUS block (a geyser only)", "b"),
            ("crossed outline = a block sunk under the mist in this layout", "x"),
            ("teal slab = a MOVER (moves during the round): solid where it starts, dashed where it turns", "m"),
            ("dashed square = the play walls (44 m); pale = open pit; grey rim = the pit's lip", "w"),
            ("'jump' = needs a running jump; unlabelled = on foot. Every small stone is a jump. Lighter = higher", "t")]
    for text, k in rows:
        if k == "can":
            d.ellipse([lx, ly, lx + 16, ly + 16], outline=INK, width=2); d.ellipse([lx + 6, ly + 6, lx + 10, ly + 10], fill=INK)
        elif k == "g":
            d.ellipse([lx, ly, lx + 16, ly + 16], fill=STEAM, outline=INK)
        elif k == "s":
            d.rectangle([lx, ly + 2, lx + 16, ly + 14], fill=OCHRE, outline=INK)
        elif k == "p":
            d.regular_polygon((lx + 8, ly + 8, 8), 4, rotation=45, fill=RED, outline=INK)
        elif k == "b":
            d.rectangle([lx, ly, lx + 16, ly + 16], fill=(200, 202, 204), outline=OCHRE, width=3)
        elif k == "x":
            d.rectangle([lx, ly, lx + 16, ly + 16], outline=(150, 164, 162)); d.line([(lx, ly), (lx + 16, ly + 16)], fill=(150, 164, 162))
        elif k == "w":
            d.rectangle([lx, ly, lx + 16, ly + 16], fill=MIST, outline=INK)
        elif k == "m":
            d.rectangle([lx, ly, lx + 16, ly + 16], fill=(150, 196, 202), outline=(36, 112, 128), width=3)
        d.text((lx + 26, ly), text, font=F, fill=INK); ly += 26

    sx0, sw = 900, (PW * 3 + 80 - 940) / 4.0
    d.text((sx0, y0), "A BLOCK RISING BETWEEN ROUNDS (section through the pit, same scale in every frame)", font=FH, fill=INK)
    frames = [("1. Last round's layout", "The tower is out: its top waits at -30, under the mist (-26).", -30.0, 0),
              ("2. The call (alarm beat)", "The shaft shakes; water and dust shed; birds leave the terraces.", -29.0, 1),
              ("3. The rise (the move)", "Stone grinds up about 10 m/s; each block locks in its own beat.", -11.0, 2),
              ("4. Locked", "A thud, a ring of dust, runoff down its sides. The round can start.", 0.6, 3)]
    sc, base = 6.3, y0 + 124
    for i, (title, text, top, st) in enumerate(frames):
        fx = sx0 + i * sw
        gx = fx + 26

        def Y(h):
            return base + (-h) * sc

        d.rectangle([gx, Y(0) - 46, gx + sw - 60, Y(-45)], fill=(218, 226, 232))
        d.rectangle([gx, Y(-26), gx + sw - 60, Y(-45)], fill=MIST)
        for k in range(5):
            yy = Y(-26) + k * 5
            d.line([(gx, yy), (gx + sw - 60, yy)], fill=(214, 222, 218))
        d.rectangle([gx, Y(-1), gx + 40, Y(-45)], fill=(124, 128, 128), outline=INK)       # the pit wall
        d.rectangle([gx - 10, Y(-1) - 10, gx + 40, Y(-1)], fill=TERR, outline=INK)
        nx = gx + 40 + 8 * sc
        d.rectangle([nx, Y(0.3), nx + 5 * sc, Y(-45)], fill=(150, 154, 156), outline=INK)     # the neighbour stone
        tx = nx + 5 * sc + 2 * sc
        d.rectangle([tx, Y(top), tx + 8 * sc, Y(-45)], fill=(176, 180, 182), outline=INK, width=2)
        d.rectangle([tx, Y(top), tx + 8 * sc, Y(top) + 6], fill=(206, 208, 206), outline=INK)
        mx = tx + 8 * sc + 3 * sc
        d.rectangle([mx, Y(0), mx + 9 * sc, Y(-45)], fill=(160, 164, 166), outline=INK)      # the court
        if top < -24:
            d.rectangle([gx + 41, Y(-26), gx + sw - 60, Y(-45)], fill=MIST)
            d.rectangle([tx, Y(top), tx + 8 * sc, Y(-45)], outline=(160, 174, 172))
            d.rectangle([nx, Y(-26), nx + 5 * sc, Y(-45)], outline=(160, 174, 172))
        if st == 2:
            for k in range(3):
                ax = tx + 8 + k * 16
                d.line([(ax, Y(top) - 10), (ax, Y(top) - 40)], fill=INK, width=2)
                d.polygon([(ax - 5, Y(top) - 34), (ax + 5, Y(top) - 34), (ax, Y(top) - 44)], fill=INK)
        if st == 3:
            d.ellipse([tx - 12, Y(top) - 5, tx + 8 * sc + 12, Y(top) + 5], outline=(120, 110, 90), width=2)
        fy = Y(0)                               # a 1.75 m figure on the court
        d.rectangle([mx + 20, fy - 1.75 * sc, mx + 20 + 0.5 * sc, fy], fill=INK)
        d.line([(gx, Y(-22)), (gx + sw - 60, Y(-22))], fill=RED)
        d.text((gx + sw - 62, Y(-22) - 8), "catch -22", font=FS, fill=RED, anchor="rm")
        d.text((gx + sw - 62, Y(-26) + 9), "mist -26", font=FS, fill=(90, 110, 106), anchor="rm")
        d.text((tx + 4 * sc, Y(top) - 56 if st == 2 else Y(top) - 16), "%+.1f" % top, font=FB, fill=INK, anchor="mm")
        d.text((fx + 26, y0 + 40), title, font=FB, fill=INK)
        words, line, yy = text.split(), "", Y(-45) + 8
        for wd in words:
            if d.textlength(line + " " + wd, font=FS) > sw - 64:
                d.text((gx, yy), line, font=FS, fill=INK); yy += 15; line = wd
            else:
                line = (line + " " + wd).strip()
        d.text((gx, yy), line, font=FS, fill=INK)
    im.save(path)


if __name__ == "__main__":
    rep = settle_and_check()
    with open(os.path.join(ROOT, "tools", "payo_layouts_draft.json"), "w", encoding="utf-8") as f:
        json.dump(DATA, f, indent=1)
    out = os.path.join(ROOT, "Logs", "payo", "draft")
    os.makedirs(out, exist_ok=True)
    for line in rep:
        print(line)
    for lay in LAYOUTS:
        print(lay["name"], {k: v for k, v in lay["reach"].items() if v != "walk"})
    draw(os.path.join(out, "payo_draft_plans_v%s.png" % VER))
    print("wrote the JSON and the sheet")
