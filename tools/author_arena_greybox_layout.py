"""Writes tools/arena_greybox_layout.json: the arena grey-box's five layouts and its stadium.

    py -3 tools/author_arena_greybox_layout.py

The numbers live here so a ramp's box is computed, not typed. Unity coordinates (x right, y up,
z forward), metres. `ArenaSceneBuilder` and `tools/view_arena_greybox.py` both read the JSON.

Rules every layout keeps (docs/ARENA_MAP_BRIEF.md, the agreed design):
  - the can at the centre in plan, at `canHeight`;
  - solid floor under the taya's mark (0, -2.5) and the attackers' marks (x -1.8, 0, 1.8 at z 9);
  - every platform that matters reachable on foot, bridges at least 2.5 m wide, ramps under 25 degrees
    (the bots have no pathfinding); a BONUS platform may be reachable only by jump pad;
  - the footprint inside +/-12 m, the walls at +/-14 m.
A piece keeps its id across layouts and travels; an id missing from a layout sinks into the pit.
"""
import json, math, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
THICK = 1.0


def slab(pid, x0, x1, z0, z1, top=0.0, kind="slab", thick=THICK):
    return {"id": pid, "kind": kind,
            "center": [round((x0 + x1) / 2, 4), round(top - thick / 2, 4), round((z0 + z1) / 2, 4)],
            "size": [round(x1 - x0, 4), thick, round(z1 - z0, 4)], "rotation": [0, 0, 0]}


def ramp(pid, high, yaw_down, run, rise, width):
    """A box whose top face runs from `high` (x, y, z: the middle of its upper edge) down `rise`
    over `run` metres toward `yaw_down` (degrees: 0 north, 90 east, 180 south, 270 west)."""
    a = math.atan2(rise, run)
    dx, dz = math.sin(math.radians(yaw_down)), math.cos(math.radians(yaw_down))
    length = math.hypot(run, rise) + 0.3                 # a little into both ends, so there is no lip
    mx, my, mz = high[0] + dx * run / 2, high[1] - rise / 2, high[2] + dz * run / 2
    nx, ny, nz = dx * math.sin(a), math.cos(a), dz * math.sin(a)
    return {"id": pid, "kind": "ramp",
            "center": [round(mx - nx * THICK / 2, 4), round(my - ny * THICK / 2, 4), round(mz - nz * THICK / 2, 4)],
            "size": [width, THICK, round(length, 4)], "rotation": [round(math.degrees(a), 4), yaw_down, 0]}


def speed(x, z, y=0.0, along_x=True):
    return {"center": [x, y, z], "halfSize": [1.5, 0.75] if along_x else [0.75, 1.5], "yaw": 0}


def ring(inner=8.0, outer=12.0):
    """The outer walkway, four pieces that share ids wherever a layout has one."""
    return [slab("north", -outer, outer, inner, outer), slab("south", -outer, outer, -outer, -inner),
            slab("east", inner, outer, -inner, inner), slab("west", -outer, -inner, -inner, inner)]


# ---------------------------------------------------------------- 0. PLAZA: the open floor
plaza = {"name": "plaza", "canHeight": 0.0, "pieces": [
    slab("centre", -6, 6, -6, 6),
    slab("north", -9, 9, 8, 12), slab("south", -9, 9, -12, -8),
    slab("east", 8, 12, -6, 6), slab("west", -12, -8, -6, 6),
    slab("linkNorth", -3, 3, 6, 8, kind="bridge"), slab("linkSouth", -3, 3, -8, -6, kind="bridge"),
    slab("linkEast", 6, 8, -3, 3, kind="bridge"), slab("linkWest", -8, -6, -3, 3, kind="bridge")],
    "jumpPads": [[-4, 0, 4], [4, 0, -4]],
    "speedPads": [speed(-6, 10), speed(6, -10)],
    "pickups": [[10, 0, 0], [-10, 0, 0], [0, 0, -10]]}

# ---------------------------------------------------------------- 1. TORE: the can on a tower
tore = {"name": "tore", "canHeight": 1.2, "pieces": [
    slab("centre", -4, 4, -4, 4, top=1.2, kind="dais", thick=2.2),
    slab("north", -5, 5, 7, 12), slab("south", -5, 5, -12, -7),
    slab("east", 7, 12, -5, 5), slab("west", -12, -7, -5, 5),
    ramp("linkNorth", (0, 1.2, 4), 0, 3, 1.2, 4), ramp("linkSouth", (0, 1.2, -4), 180, 3, 1.2, 4),
    ramp("linkEast", (4, 1.2, 0), 90, 3, 1.2, 4), ramp("linkWest", (-4, 1.2, 0), 270, 3, 1.2, 4),
    slab("cornerNorthEast", 7, 12, 7, 12), slab("cornerSouthWest", -12, -7, -12, -7),
    slab("bridgeA", 5, 7, 8, 11, kind="bridge"), slab("bridgeB", 8, 11, 5, 7, kind="bridge"),
    slab("bridgeC", -7, -5, -11, -8, kind="bridge"), slab("bridgeD", -11, -8, -7, -5, kind="bridge")],
    "jumpPads": [[9.5, 0, 9.5], [-9.5, 0, -9.5]],
    "speedPads": [speed(9.5, -2.5, along_x=False), speed(-9.5, 2.5, along_x=False)],
    "pickups": [[0, 0, -9.5], [9.5, 0, 3], [-9.5, 0, -3]]}

# ---------------------------------------------------------------- 2. KRUS: four catwalks to a ring
krus = {"name": "krus", "canHeight": 0.0, "pieces": [
    slab("centre", -4, 4, -4, 4)] + ring() + [
    slab("linkNorth", -1.5, 1.5, 4, 8, kind="bridge"), slab("linkSouth", -1.5, 1.5, -8, -4, kind="bridge"),
    slab("linkEast", 4, 8, -1.5, 1.5, kind="bridge"), slab("linkWest", -8, -4, -1.5, 1.5, kind="bridge")],
    "jumpPads": [[10, 0, 10], [-10, 0, -10]],
    "speedPads": [speed(10, 0, along_x=False), speed(-10, 0, along_x=False)],
    "pickups": [[10, 0, -10], [-10, 0, 10], [0, 0, -10]]}

# ---------------------------------------------------------------- 3. HUKAY: the can in a sunken bowl
hukay = {"name": "hukay", "canHeight": -1.2, "pieces": [
    slab("centre", -5, 5, -5, 5, top=-1.2)] + ring() + [
    ramp("linkNorth", (0, 0, 8), 180, 3, 1.2, 6), ramp("linkSouth", (0, 0, -8), 0, 3, 1.2, 6),
    ramp("linkEast", (8, 0, 0), 270, 3, 1.2, 6), ramp("linkWest", (-8, 0, 0), 90, 3, 1.2, 6)],
    "jumpPads": [[-3.5, -1.2, 3.5], [3.5, -1.2, -3.5]],
    "speedPads": [speed(0, 10.6), speed(0, -10)],
    "pickups": [[10, 0, 10], [-10, 0, -10], [10, 0, -10]]}

# ---------------------------------------------------------------- 4. ENTABLADO: high ground and two lofts
entablado = {"name": "entablado", "canHeight": 0.0, "pieces": [
    slab("north", -10, 10, 4, 12, top=1.2, kind="dais", thick=2.2),         # the attackers' balcony
    slab("centre", -10, 10, -5, 4),
    slab("south", -4, 4, -12, -5),
    ramp("linkEast", (8, 1.2, 4), 180, 3, 1.2, 4), ramp("linkWest", (-8, 1.2, 4), 180, 3, 1.2, 4),
    slab("cornerNorthEast", 8, 12, -11, -7, top=3.0, thick=0.6),            # the lofts: jump pad only
    slab("cornerSouthWest", -12, -8, -11, -7, top=3.0, thick=0.6)],
    "jumpPads": [{"center": [6.5, 0, -3.5], "speed": 15.5}, {"center": [-6.5, 0, -3.5], "speed": 15.5}],
    "speedPads": [speed(0, -8.5, along_x=False), speed(0, 10.5, y=1.2)],
    "pickups": [[10, 3.0, -9], [-10, 3.0, -9], [0, 0, -11]]}


# ---------------------------------------------------------------- the stadium round it
def surround():
    out = []

    def add(pid, kind, cx, cy, cz, sx, sy, sz, yaw=0):
        out.append({"id": pid, "kind": kind, "center": [round(cx, 3), round(cy, 3), round(cz, 3)],
                    "size": [round(sx, 3), round(sy, 3), round(sz, 3)], "rotation": [0, yaw, 0]})

    front, tread, rise, base = 16.0, 1.2, 0.7, -6.0
    # East and west: eight tiers of benches. South: six, with the host's booth. North: four, under the screen.
    for side, yaw, tiers in (("east", 90, 8), ("west", 270, 8), ("south", 180, 6), ("north", 0, 4)):
        dx, dz = math.sin(math.radians(yaw)), math.cos(math.radians(yaw))
        for i in range(tiers):
            top = 0.8 + i * rise
            dist = front + (i + 0.5) * tread
            length = 2 * (front + i * tread)              # each tier a little longer, so the corners close
            along_x = dz != 0 and abs(dz) > 0.5
            add("%sTier%d" % (side, i), "stand", dx * dist, (top + base) / 2, dz * dist,
                length if along_x else tread, top - base, tread if along_x else length)
        # The barrier between the moat and the first row.
        along_x = abs(dz) > 0.5
        add(side + "Barrier", "rig", dx * (front - 0.3), (1.6 + base) / 2, dz * (front - 0.3),
            2 * front if along_x else 0.6, 1.6 - base, 0.6 if along_x else 2 * front)
    add("screen", "screen", 0, 9.5, 22.5, 18, 7, 0.6)                       # the big board over the north stand
    add("screenLegWest", "tower", -8, 2.5, 22.5, 1, 9, 1); add("screenLegEast", "tower", 8, 2.5, 22.5, 1, 9, 1)
    add("booth", "booth", 0, 6.4, -24.6, 7, 3, 3)                           # the host, over the south stand
    for sx in (-1, 1):
        for sz in (-1, 1):
            add("tower%s%s" % ("E" if sx > 0 else "W", "N" if sz > 0 else "S"), "tower", sx * 27, 4, sz * 27, 2.5, 20, 2.5)
    # The lighting rig: a square truss over the stage, above the ceiling the slippers know (12 m).
    for pid, cx, cz, sx, sz in (("rigNorth", 0, 15, 31, 0.8), ("rigSouth", 0, -15, 31, 0.8),
                                ("rigEast", 15, 0, 0.8, 31), ("rigWest", -15, 0, 0.8, 31)):
        add(pid, "rig", cx, 13.6, cz, sx, 0.8, sz)
    return out


DATA = {
    "notes": "Arena grey-box. WRITTEN BY tools/author_arena_greybox_layout.py: edit that, not this. Unity "
             "coordinates (x right, y up, z forward), metres. Boxes only: size is the box's own x, y, z before "
             "rotation; rotation is Unity Euler degrees (z, then x, then y). A piece keeps its id across layouts "
             "and travels; an id missing from a layout sinks into the pit. 'surround' is the stadium: static, the "
             "same in every layout, no colliders.",
    "stageTop": 0.0, "wallHalf": 14.0, "wallHeight": 12.0, "pitY": -6.0,
    "layouts": [plaza, tore, krus, hukay, entablado],
    "surround": surround(),
    "spawns": {"taya": [0, 0, -2.5], "attackers": [[-1.8, 0, 9], [0, 0, 9], [1.8, 0, 9]]},
}


def check():
    """Floor under every mark and the can, in every layout (flat pieces only; ramps are ignored)."""
    marks = [DATA["spawns"]["taya"]] + DATA["spawns"]["attackers"] + [[0, 0, 0]]
    for lay in DATA["layouts"]:
        for m in marks:
            under = [p for p in lay["pieces"] if p["rotation"][0] == 0
                     and abs(m[0] - p["center"][0]) <= p["size"][0] / 2 - 0.4
                     and abs(m[2] - p["center"][2]) <= p["size"][2] / 2 - 0.4]
            tops = [p["center"][1] + p["size"][1] / 2 for p in under]
            assert tops and -4.0 < max(tops) < 2.0, (lay["name"], m, tops)
        ids = [p["id"] for p in lay["pieces"]]
        assert len(ids) == len(set(ids)), (lay["name"], "duplicate id")
        for p in lay["pieces"]:
            c, s = p["center"], p["size"]
            if p["kind"] != "ramp":
                assert abs(c[0]) + s[0] / 2 <= 12.01 and abs(c[2]) + s[2] / 2 <= 12.01, (lay["name"], p["id"])


if __name__ == "__main__":
    check()
    path = os.path.join(ROOT, "tools", "arena_greybox_layout.json")
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(DATA, f, indent=1)
    print("wrote", path, len(DATA["layouts"]), "layouts,", len(DATA["surround"]), "surround boxes")
