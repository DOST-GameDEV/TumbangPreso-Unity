"""PAETE'S RUN: what each stage IS, and how the grey blockout builds it (2026-10-10).

Owner, of a course whose hops were one row of stumps after another: "the jumps are all so uniform.. needs more
uniqueness and variety per stage. add floating chained boulders etc". So every leg of the course (1-based, as numbered
on the map) is its own obstacle: `STAGE` names it and says it in words; `build` makes it in grey for the blockout. A leg
not listed, or named "plain", is built as its plain kind. No obstacle is used twice, bar the geysers and the swings'
three kinds.

`build` is called by tools/author_paete_run_blockout.py with that file's own helpers in `K` (materials and the little
mesh makers), so this file has no Blender scene of its own.
"""
import math

STAGE = {
    1: ("plain", "the start straight"),
    2: ("chained_boulders", "CHAINED BOULDERS: boulders hung on chains, swaying; hop one to the next"),
    3: ("three_beams", "THREE BEAMS: the lane splits into narrow beams, one of them broken"),
    4: ("arches", "vine swings under a row of broken arches"),
    5: ("pendulums", "THE GAUNTLET: run a ledge while boulders swing across it"),
    6: ("geyser", "a sand geyser throws you up a tier"),
    7: ("long_swing", "one long swing from a single high spur"),
    8: ("gapped_bridge", "BROKEN BRIDGE: a causeway with spans missing"),
    9: ("colonnade", "COLONNADE: hop the tops of standing and fallen columns"),
    10: ("plain", "the cliff edge"),
    11: ("dive", "CLIFF DIVE into the first pool"),
    12: ("swim", "swim the pool"),
    13: ("mill_wheels", "MILL WHEELS: turning stone wheels lying flat, each a moving step"),
    14: ("geyser", "geyser"),
    15: ("chain_swing", "swings from hanging chains that sway themselves"),
    16: ("rolling_logs", "LOG ROLL: run across logs that turn underfoot"),
    17: ("tilting_slabs", "SEESAWS: slabs on pivots that tip as you land"),
    18: ("stair_blocks", "GIANT STEPS: a run up blocks sized for a giant"),
    19: ("geyser", "geyser"),
    20: ("arches", "swing to the summit"),
    21: ("plain", "THE SUMMIT"),
    22: ("chute", "THE SUMMIT SLIDE"),
    23: ("plain", "a ledge"),
    24: ("long_swing", "a swing out and down"),
    25: ("wall_ledges", "WALL LEDGES: narrow shelves stepping down a cliff face"),
    26: ("plain", "the second cliff"),
    27: ("dive", "CLIFF DIVE into the second pool"),
    28: ("swim", "swim the second pool"),
    29: ("floating_rocks", "FLOATING ROCKS: boulders chained to the bed, bobbing in the water"),
    30: ("crumble", "CRUMBLING WALK: slabs that fall away behind you"),
    31: ("chute", "THE LAST SLIDE"),
    32: ("swim", "the last pool"),
    33: ("drum_stones", "DRUMS: round stone drums at wild heights"),
    34: ("geyser", "geyser"),
    35: ("chain_swing", "the last swing"),
    36: ("plain", "the finish ledge"),
}


def build(name, A, C, K):
    """Builds leg A to C as the named obstacle. False if the name is not one of these (the caller draws the plain leg)."""
    V, up = K["Vector"], K["Vector"]((0, 0, 1))
    f, s = K["side_of"](A, C)
    ln = (C - A).length
    ball, drum, rod, block, strip, box, posts, frame_over = (K[n] for n in ("ball", "drum", "rod", "block", "strip", "box", "posts", "frame_over"))
    ROCK, SAND, CHAIN, POST, WATER, PAD, VINE, WOOD, FLOOR = (K[n] for n in ("ROCK", "SAND", "CHAIN", "POST", "WATER", "PAD", "VINE", "WOOD", "FLOOR"))

    def along(k, n):
        return A.lerp(C, (k + 0.5) / n)

    if name == "chained_boulders":
        n = max(4, int(ln / 6.5))
        for k in range(n):
            mid = along(k, n)
            p = mid + s * ((2.2 if k % 2 else -2.0) + math.sin(k * 2.1) * 0.8)
            r = 1.5 + 0.5 * math.sin(k * 1.7)
            sway = s * (1.3 * math.sin(k * 2.6))
            top = p.z + 9.0
            frame_over(mid, s, top, 6.0)
            ball(p + sway - up * (r * 0.75), r, ROCK)
            rod(p + sway + up * (r * 0.05), V((p.x, p.y, top)), 0.09, CHAIN)
    elif name == "three_beams":
        strip([A, A + f * 4], 4.0, SAND); strip([C - f * 4, C], 4.0, SAND)
        for off, a0, a1 in ((-2.8, 0.0, 1.0), (0.0, 0.0, 0.55), (0.3, 0.68, 1.0), (2.8, 0.25, 1.0)):
            strip([A.lerp(C, 0.1 + 0.8 * a0) + s * off, A.lerp(C, 0.1 + 0.8 * a1) + s * off], 0.45, SAND, 0.5)
        posts([A.lerp(C, k / 6) for k in range(7)], 12.0)
    elif name == "pendulums":
        strip([A, C], 2.2, SAND); posts([A.lerp(C, k / 6) for k in range(7)], 12.0)
        n = max(3, int(ln / 7))
        for k in range(n):
            p = along(k, n)
            top = p.z + 10.0
            frame_over(p, s, top, 5.5)
            ang = 0.75 * math.sin(k * 2.3 + 0.6)
            b = V((p.x, p.y, top)) + s * (8.2 * math.sin(ang)) - up * (8.2 * math.cos(ang))
            ball(b, 1.5, ROCK, 1.0); rod(V((p.x, p.y, top)), b + up * 1.2, 0.1, CHAIN)
    elif name == "gapped_bridge":
        cuts = [0.0, 0.2, 0.3, 0.52, 0.6, 0.78, 0.9, 1.0]
        for a0, a1 in zip(cuts[0::2], cuts[1::2]):
            seg = [A.lerp(C, a0), A.lerp(C, a1)]
            strip(seg, 4.0, SAND, 1.2); block(seg[0].lerp(seg[1], 0.5), 0.5, FLOOR, seg[0].z - 1.2, POST)
    elif name == "colonnade":
        n = max(5, int(ln / 4.6))
        for k in range(n):
            mid = along(k, n)
            p = mid + s * (2.6 if k % 2 else -2.6)
            if k % 4 == 2:                                # a fallen one, lying across as a beam
                drum(mid - up * 0.9, 0.9, 8.5, ROCK, s + f * 0.25); block(mid, 0.25, FLOOR, mid.z - 1.8, POST)
            else:
                h = (0.0, 1.1, -0.5, 0.6)[k % 4]
                drum(V((p.x, p.y, (FLOOR + p.z + h) / 2)), 1.15, p.z + h - FLOOR, ROCK)
                drum(V((p.x, p.y, p.z + h - 0.2)), 1.5, 0.4, SAND)
    elif name == "mill_wheels":
        n = max(4, int(ln / 6.5))
        for k in range(n):
            p = along(k, n) + s * (1.8 if k % 2 else -1.8)
            r = 2.6 - 0.5 * (k % 3)
            drum(p - up * 0.3, r, 0.6, ROCK); drum(p - up * 0.2, 0.5, 1.2, CHAIN); block(p, 0.3, FLOOR, p.z - 0.6, POST)
            rod(p - up * 0.02 - f * (r * 0.8), p - up * 0.02 + f * (r * 0.8), 0.12, SAND)          # a spoke, to show it turns
    elif name == "rolling_logs":
        n = max(5, int(ln / 3.6))
        for k in range(n):
            p = along(k, n)
            drum(p - up * 0.75, 0.75, 8.5, WOOD, s)
            if k % 2 == 0:
                for sd in (-1, 1):
                    block(p + s * (sd * 4.6), 0.22, FLOOR, p.z - 0.6, POST)
    elif name == "tilting_slabs":
        n = max(4, int(ln / 6.5))
        for k in range(n):
            p = along(k, n) + s * (1.6 if k % 2 else -1.6)
            tilt = (0.32, -0.26, 0.2, -0.34)[k % 4]
            a0, a1 = p - f * 2.4 - up * (2.4 * math.sin(tilt)), p + f * 2.4 + up * (2.4 * math.sin(tilt))
            strip([a0, a1], 1.9, ROCK, 0.45); drum(p - up * 0.7, 0.4, 3.8, CHAIN, s); block(p, 0.25, FLOOR, p.z - 0.9, POST)
    elif name == "stair_blocks":
        n = max(4, int(ln / 6.0))
        for k in range(n):
            p = along(k, n)
            w = 3.6 - (k % 2) * 1.2
            box(p.x - w, p.x + w, p.y - 2.6, p.y + 2.6, FLOOR, p.z + (0.0, 1.3, -0.4, 0.9)[k % 4], ROCK)
    elif name == "wall_ledges":
        w0, w1 = A + s * 4.5, C + s * 4.5
        K["mesh"]("cliff", [tuple(w0 + up * 9), tuple(w1 + up * 9), (w1.x, w1.y, FLOOR), (w0.x, w0.y, FLOOR)], [(0, 1, 2, 3), (3, 2, 1, 0)], POST)
        n = max(5, int(ln / 4.6))
        for k in range(n):
            p = along(k, n) + s * (3.4 - (k % 3) * 1.1)
            half = 1.0 + 0.5 * (k % 2)
            strip([p - f * half, p + f * half], 0.9, ROCK, 0.5)
    elif name == "floating_rocks":
        zw = min(A.z, C.z) - 3.0
        strip([V((A.x, A.y, zw)), V((C.x, C.y, zw))], 5.5, WATER, 0.15)
        n = max(4, int(ln / 6.0))
        for k in range(n):
            p = along(k, n) + s * (2.0 if k % 2 else -2.0)
            ball(p - up * 1.0, 1.6 + 0.3 * (k % 2), ROCK, 0.7); rod(p - up * 1.8, V((p.x + 0.6, p.y, zw - 4.0)), 0.09, CHAIN)
    elif name == "crumble":
        n = max(6, int(ln / 3.4))
        for k in range(n):
            p = along(k, n)
            drop = 0.0 if k > n * 0.45 else (n * 0.45 - k) * 1.3
            tip = 0.0 if drop == 0 else 0.5
            strip([p - f * 1.5 - up * (drop - tip), p + f * 1.5 - up * (drop + tip)], 3.6, SAND if drop == 0 else ROCK, 0.5)
        posts([A.lerp(C, k / 6) for k in range(7)], 14.0)
    elif name == "drum_stones":
        n = max(5, int(ln / 5.0))
        for k in range(n):
            p = along(k, n) + s * ((2.4, -1.0, 1.2, -2.6)[k % 4])
            h = (0.0, 1.6, -0.6, 2.4, 0.8)[k % 5]
            drum(V((p.x, p.y, (FLOOR + p.z + h) / 2)), (1.7, 1.2, 2.0, 1.0)[k % 4], p.z + h - FLOOR, ROCK)
    elif name == "geyser":
        strip([A - f * 3.0, A + f * 3.0], 4.0, SAND); block(A, 0.3, FLOOR, A.z - 0.5, POST)
        drum(A + up * 0.15, 1.6, 0.3, PAD)
        for k in range(1, 7):
            drum(A + up * (k * 1.5), 1.1 - k * 0.14, 1.2, WATER)
        for k in range(1, 9):
            t = k / 9
            p = A.lerp(C, t) + up * (4.0 * math.sin(math.pi * t))
            block(p, 0.16, p.z + 1.0, p.z + 1.3, PAD)
    elif name in ("arches", "long_swing", "chain_swing"):
        n = 1 if name == "long_swing" else max(2, round(ln / 16.0))
        top = max(A.z, C.z) + (13.0 if name == "long_swing" else 8.0)
        for k in range(n):
            p = along(k, n)
            if name == "arches":
                for sd in (-1, 1):
                    block(p + s * (sd * 5.0), 0.7, FLOOR, top - 2.0, ROCK)
                for j in range(9):                                   # the arch's ring of stones, a vine on its keystone
                    a = math.pi * j / 8
                    q = V((p.x, p.y, top - 2.0)) + s * (5.0 * math.cos(a)) + up * (3.0 * math.sin(a))
                    block(q, 0.7, q.z - 0.6, q.z + 0.6, ROCK if j != 4 else VINE)
            elif name == "long_swing":
                block(p + s * 7.0, 0.9, FLOOR, top + 1.0, ROCK)
                strip([V((p.x, p.y, top)) + s * 7.0, V((p.x, p.y, top)) - s * 1.0], 0.5, VINE, 0.7)
            else:
                frame_over(p, s, top + 2.0, 5.0)
                hang = V((p.x, p.y, top - 2.5)) + s * (1.5 * math.sin(k * 2.2))
                rod(V((p.x, p.y, top + 2.0)), hang, 0.09, CHAIN); ball(hang, 0.7, VINE, 1.0)
            for j in range(9):
                q = A.lerp(C, (k + j / 8) / n)
                dip = (5.0 if name == "long_swing" else 3.0) * math.sin(math.pi * j / 8)
                block(q, 0.14, q.z + 1.1 - dip, q.z + 1.4 - dip, VINE)
    else:
        return False
    return True
