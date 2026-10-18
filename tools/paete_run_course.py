"""PAETE'S RUN: THE COURSE, as data. The one place its way points are typed (2026-10-10).

Read by tools/draw_paete_run_course_maps.py (the map) and tools/author_paete_run_blockout.py (the grey 3D blockout);
whatever puts it in Unity reads it too. Metres; x and y on the flat, z up. A leg is one kind of going from where the
last one ended: run, chute (a slide), swing (the vine, over a gap), swim, hops (parkour), pad (a bounce), dive (a
jump off a cliff into the pool the next leg swims).

THE SHAPE IS "B, THE SWITCHBACKS" (owner, 2026-10-10: "i like b"), chosen over a knot after he refused a circle, a plain
descent and a straight line ("i dont want it to be a circle, i dont want it to be a plainly going down map, i dont want
it to essentialy be a straight line in one direction. the obstacle run should be long enough"). Long zigzags up a
stepped rock face by hops, pads and swings, then down in slides that pass under the climb.

SHORTCUTS (owner, same message: "alternative routes / harder shortcuts should also be an option"). Each leaves the
course at one way point and rejoins at a later one, is SHORTER and HARDER (a longer swing, a thin line of hops with a
fall under it), and a fall from it costs what any fall costs (set back, like Mario Kart).
"""


def chain(start, steps):
    legs, at = [], start
    for (kind, x, y, z) in steps:
        legs.append((kind, at, (x, y, z))); at = (x, y, z)
    return legs


# ⚠️ v2 (2026-10-10). Of the first course (27 legs, 880 m, five slides) the owner said: "the run looks too small and short,
# theres way too many slides imo". So: 36 legs over a site about 220 by 240 m and 52 m of height, lanes a third wider, and
# TWO slides, both long, each the pay-off of a long climb (the summit slide and the last descent). The other ways down
# are a CLIFF DIVE into a pool (twice), a swing, and hops downhill. Climbs are still hops, pads and swings.
START = (-90, 0, 0)
COURSE = chain(START, [
    ("run", -50, 0, 0), ("hops", -10, 8, 7), ("run", 30, 12, 7), ("swing", 75, 22, 10), ("run", 95, 40, 10), ("pad", 88, 56, 20),
    ("swing", 45, 62, 20), ("run", 10, 60, 20), ("hops", -30, 66, 27), ("run", -60, 70, 27), ("dive", -80, 84, 8), ("swim", -95, 110, 8),
    ("hops", -80, 134, 15), ("pad", -66, 142, 25), ("swing", -20, 140, 27), ("run", 20, 136, 27), ("hops", 55, 146, 36), ("run", 80, 160, 36),
    ("pad", 86, 176, 46), ("swing", 50, 190, 46), ("run", 20, 192, 46), ("chute", -50, 170, 26), ("run", -70, 160, 26), ("swing", -60, 120, 20),
    ("hops", -20, 104, 12), ("run", 10, 100, 12), ("dive", 40, 92, 2), ("swim", 80, 96, 2), ("hops", 110, 110, 8), ("run", 118, 84, 8),
    ("chute", 60, 30, -6), ("swim", 40, -24, -6), ("hops", 0, -40, 0), ("pad", -16, -40, 9), ("swing", -60, -30, 12), ("run", -90, -22, 12)])

# name, what it asks of a player, the leg it leaves AFTER (1-based, as numbered on the map), the leg it rejoins BEFORE, its legs
SHORTCUTS = [
    ("THE LONG VINE", "swings high over the first pool instead of diving into it", 10, 15,
     chain((-60, 70, 27), [("swing", -64, 106, 27), ("swing", -66, 142, 25)])),
    ("THE CHIMNEY", "a steep chain of swings straight up to the summit", 16, 21,
     chain((20, 136, 27), [("swing", 36, 164, 38), ("swing", 50, 190, 46)])),
    ("THE GOAT TRACK", "a thin line of small hops cutting the corner before the second pool", 23, 27,
     chain((-70, 160, 26), [("hops", -30, 128, 18), ("hops", 10, 100, 12)])),
]

SPEED = {"run": 7.0, "chute": 13.0, "swing": 11.0, "swim": 4.0, "hops": 5.0, "pad": 8.0, "dive": 12.0}       # m/s, GUESSES, for a time estimate
