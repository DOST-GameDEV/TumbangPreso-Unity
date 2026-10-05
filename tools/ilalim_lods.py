"""Level-of-detail meshes for the Ilalim ng Tulay export (tools/export_ilalim_unity.py imports this).

WHY. The owner, 2026-10-04: "its primarily not laggy for the server host but it is for players
joining too", and then "add these optimization fixes". The map is about 6.4 million placed
triangles and every object was either drawn at full detail or culled; there were no real LODs.
WORKING_RULES says not to decimate as a speculative optimization: this is the asked-for one, and
the LOD0 meshes are not touched by anything here.

THREE KINDS OF LOD, each made from the same linked kit object the LOD0 came from:

  * BEVEL-FREE (LOD1 of every hard-surface kit: buildings, roof kit, stations, landmarks, the
    guideway, trunks and limbs). The kits build chunky low-poly meshes and add LIVE Bevel
    modifiers; the export applies them, so most of a building's triangles are bevel segments.
    The LOD1 is the same object evaluated with its Bevel modifiers switched off (every other
    modifier, Solidify included, still applied): the same silhouette, the same faces, the same
    UV maps and material slots, and bounds within the bevel's width of the LOD0's (2 cm at most). Made on a LOCAL
    COPY of the object, so the linked kit is never edited.

  * THINNED CARDS (LOD1 and LOD2 of the broadleaf canopies, the hedge and anything else that is
    nothing but loose leaf cards: author_ilalim_trees.py foliage() and flat_card()). A share of
    the cards is kept, evenly through each material's cards in the order the kit laid them (clump
    by clump, top to bottom on a Fibonacci sphere, so the thinning is even over every clump and
    the light and dark tints keep their ratio), and each kept card is scaled about its own centre
    by a little over 1 / sqrt(share), so the canopy covers the same area with fewer, larger leaves. UVs and the
    clump normals are copied corner for corner, so the foliage shader shades the LOD as the same
    soft ball. The cards that carry the canopy's six extremes are always kept and every scaled
    card is slid back inside the LOD0 bounding box, so the bounds are the LOD0's exactly.

  * COLLAPSED (LOD2 of the same hard-surface kits): the bevel-free mesh through a Decimate
    (collapse) modifier, at the strongest ratio that leaves its bounds where they were and every
    material slot in use. This one does change faces and smears the UVs a little, so it is only
    for where the whole object is a few dozen pixels tall (about 5 per cent of the screen height).

Nothing here writes a file; the export script writes, reads back and checks every LOD .glb.
"""
import math

import bpy
import numpy as np

# Never given a LOD, whatever they would save: the ground, the kerbs and the median planter, the
# chalk and the road markings, the train, the moving traffic (a parked car's mesh that the traffic
# also drives is the traffic's).
SKIP_GROUPS = {"StreetGround", "StreetMarkings", "Street", "Train", "Traffic"}
# Prop-like kits: a prototype is skipped when ANY of its placements stands in the play area (a
# swap must never be seen on the things a player stands next to).
PLAY_GROUPS = {"Props", "SariSari", "StreetFurniture", "StreetFences", "StreetLife", "ColumnSigns", "Lilies"}
# The play area in the Blender frame (x0, x1, y0, y1): the lot court, across Taft, to the shop fronts.
PLAY = (-35.0, 11.0, 3.4, 39.2)
SKIP_MESHES = {"lrt_pier"}          # the near-fade bake (the export's PIER_MESH)
MIN_SAVING = 0.40                   # a LOD must drop at least this share of the LOD0's triangles
MIN_PLACED = 2500                   # and the prototype must place at least this many triangles
LOD2_GAIN = 0.30                    # a LOD2 must drop at least this share of the LOD1's
CARD_KEEP = (0.50, 0.12)            # share of leaf cards kept at LOD1, LOD2
# A flat card enlarged on a curved clump opens gaps at its corners, so the kept cards grow a
# little more than the area they stand in for (measured on the review renders).
CARD_GROW = (1.08, 1.15)
COLLAPSE_RATIOS = (0.40, 0.50, 0.60) # tried in order; the first that keeps the bounds and the materials is kept
COLLAPSE_BOUNDS_TOL = 0.04          # metres, under the export's own read-back tolerance
COLLAPSE_PREFIX = "tree_"           # only these prototypes get a collapsed LOD2 (see lods())
COLLAPSE_MIN_TRIS = 400             # a mesh already under this at LOD1 is left alone


def tri_count(me):
    return sum(len(p.vertices) - 2 for p in me.polygons)


def coords(me):
    co = np.empty(len(me.vertices) * 3, dtype=np.float64)
    me.vertices.foreach_get("co", co)
    return co.reshape(-1, 3)


def sphere_diameter(me):
    """The diameter of the sphere round the bounding box's centre that holds every vertex."""
    co = coords(me)
    if len(co) == 0:
        return 0.0
    centre = (co.min(axis=0) + co.max(axis=0)) / 2
    return float(2 * np.linalg.norm(co - centre, axis=1).max())


def slots_used(me):
    """Which material slots a mesh's faces use (a LOD that lost one would lose a submesh)."""
    mi = np.empty(len(me.polygons), dtype=np.int64)
    me.polygons.foreach_get("material_index", mi)
    return set(np.unique(mi).tolist())


def in_play(corners):
    """Whether a placement's world bounding-box corners (n x 3) reach into the play area."""
    return not (corners[:, 0].min() > PLAY[1] or corners[:, 0].max() < PLAY[0] or
                corners[:, 1].min() > PLAY[3] or corners[:, 1].max() < PLAY[2])


def is_cards(me):
    """Nothing but loose quads (leaf cards): every face a quad, no vertex shared."""
    n = len(me.polygons)
    if n == 0 or len(me.vertices) != 4 * n or len(me.loops) != 4 * n:
        return False
    return True


def evaluate(jobs):
    """Evaluate LOCAL COPIES of kit objects with every Bevel modifier off. `jobs` is a list of
    (object, collapse ratio or None); returns one new local mesh per job, in order. The copies
    are linked into the scene for one depsgraph pass and removed again; the linked originals are
    never touched."""
    if not jobs:
        return []
    col = bpy.data.collections.new("ilalim lod pass")
    bpy.context.scene.collection.children.link(col)
    copies = []
    for orig, ratio in jobs:
        c = orig.copy()
        col.objects.link(c)
        c.parent = None
        c.hide_viewport = c.hide_render = False
        for m in c.modifiers:
            if m.type == "BEVEL":
                m.show_viewport = m.show_render = False
        if ratio is not None:
            d = c.modifiers.new("lod collapse", "DECIMATE")
            d.decimate_type, d.ratio, d.use_collapse_triangulate = "COLLAPSE", ratio, True
        copies.append(c)
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    meshes = [bpy.data.meshes.new_from_object(c.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
              for c in copies]
    for c in copies:
        bpy.data.objects.remove(c)
    bpy.data.collections.remove(col)
    bpy.context.view_layer.update()
    return meshes


def thin_cards(me, keep, grow, name):
    """A new mesh holding the share `keep` of `me`'s leaf cards, each scaled by grow / sqrt(share kept)
    about its own centre (see the docstring). Every UV map, the material slots and the corner
    normals are carried over."""
    n = len(me.polygons)
    co = coords(me)
    lv = np.empty(len(me.loops), dtype=np.int64)
    me.loops.foreach_get("vertex_index", lv)
    ls = np.empty(n, dtype=np.int64)
    me.polygons.foreach_get("loop_start", ls)
    mi = np.empty(n, dtype=np.int64)
    me.polygons.foreach_get("material_index", mi)
    smooth = np.empty(n, dtype=bool)
    me.polygons.foreach_get("use_smooth", smooth)
    corner = ls[:, None] + np.arange(4)[None, :]            # n x 4 loop indices
    quad = co[lv[corner]]                                    # n x 4 x 3
    kept = np.zeros(n, dtype=bool)
    for k in np.unique(mi):
        idx = np.flatnonzero(mi == k)
        m = max(1, int(round(len(idx) * keep)))
        kept[idx[np.floor((np.arange(m) + 0.5) * len(idx) / m).astype(np.int64)]] = True
    lo, hi = co.min(axis=0), co.max(axis=0)
    for ax in range(3):
        kept[quad[:, :, ax].min(axis=1).argmin()] = True
        kept[quad[:, :, ax].max(axis=1).argmax()] = True
    scale = grow / math.sqrt(kept.mean())
    q = quad[kept]
    centre = q.mean(axis=1, keepdims=True)
    q = centre + (q - centre) * scale
    q += (np.maximum(lo - q.min(axis=1), 0.0) - np.maximum(q.max(axis=1) - hi, 0.0))[:, None, :]
    q = np.clip(q, lo, hi)          # a card grown wider than a small clump (the hedge's LOD2) is trimmed to it
    loops = corner[kept].ravel()
    m = int(kept.sum())
    out = bpy.data.meshes.new(name)
    out.from_pydata(q.reshape(-1, 3).tolist(), [], np.arange(4 * m).reshape(-1, 4).tolist())
    for mat in me.materials:
        out.materials.append(mat)
    out.polygons.foreach_set("material_index", mi[kept].astype(np.int32))
    out.polygons.foreach_set("use_smooth", smooth[kept])
    for layer in me.uv_layers:
        uv = np.empty(len(me.loops) * 2, dtype=np.float32)
        layer.data.foreach_get("uv", uv)
        new = out.uv_layers.new(name=layer.name)
        new.data.foreach_set("uv", uv.reshape(-1, 2)[loops].ravel())
    nrm = np.empty(len(me.loops) * 3, dtype=np.float32)
    me.corner_normals.foreach_get("vector", nrm)
    out.normals_split_custom_set(nrm.reshape(-1, 3)[loops].tolist())
    out.update()
    return out


def build(protos, log):
    """Give every prototype worth it its LOD meshes: p["lods"] = {1: mesh, 2: mesh} (2 optional).
    Reads p["mesh"] (the LOD0, before its UV maps are rewritten), p["orig"], p["tris"],
    p["users"], p["group"], p["groups"] (every group that places it), p["inPlay"], p["cards"] (loose leaf cards on the foliage shader) and
    the mesh name in the key. Returns why each skipped
    prototype was skipped: {reason: [names]}."""
    skipped = {}
    todo = []

    def skip(key, why):
        skipped.setdefault(why, []).append(key[1])

    for key, p in protos.items():
        p["lods"] = {}
        if p["groups"] & SKIP_GROUPS:
            skip(key, f"group {sorted(p['groups'] & SKIP_GROUPS)[0]} (ground, kerbs, chalk, train, traffic)")
        elif key[1] in SKIP_MESHES:
            skip(key, "the LRT pier (near-fade bake)")
        elif p["groups"] & PLAY_GROUPS and p["inPlay"]:
            skip(key, "a prop, fence or furniture piece standing in the play area")
        elif p["tris"] * p["users"] < MIN_PLACED:
            skip(key, f"places under {MIN_PLACED} triangles")
        else:
            todo.append(key)
    # One depsgraph pass for every bevel-free mesh, then one per collapse ratio still needed.
    hard = [k for k in todo if not protos[k]["cards"]]
    for key, me in zip(hard, evaluate([(protos[k]["orig"], None) for k in hard])):
        p = protos[key]
        if tri_count(me) <= p["tris"] * (1 - MIN_SAVING):
            p["lods"][1] = me
        else:
            skip(key, f"bevel-free saves under {round(MIN_SAVING * 100)} per cent (no bevel, or few bevelled edges)")
            bpy.data.meshes.remove(me)
    # ⚠️ ONLY THE TREES' WOOD GETS A COLLAPSED LOD2 (2026-10-05). Measured in Unity, a build with the
    # hard-surface kits' LOD2 drew 5.951 M mean triangles against 5.967 M without (0.25 per cent):
    # past 80 m the cull and the occlusion have already taken what a LOD2 would save, and the 138
    # files cost 29 MB in a repository with no LFS. A trunk keeps its LOD2 so it swaps with its canopy.
    left = [k for k in hard if 1 in protos[k]["lods"] and tri_count(protos[k]["lods"][1]) >= COLLAPSE_MIN_TRIS
            and k[1].startswith(COLLAPSE_PREFIX)]
    for ratio in COLLAPSE_RATIOS:
        left = [k for k in left if 2 not in protos[k]["lods"]]
        for key, me in zip(left, evaluate([(protos[k]["orig"], ratio) for k in left])):
            p = protos[key]
            a, b = coords(p["mesh"]), coords(me)
            off = max(np.abs(a.min(axis=0) - b.min(axis=0)).max(), np.abs(a.max(axis=0) - b.max(axis=0)).max())                 if len(b) else 1e9
            if off <= COLLAPSE_BOUNDS_TOL and tri_count(me) <= tri_count(p["lods"][1]) * (1 - LOD2_GAIN)                     and slots_used(me) == slots_used(p["lods"][1]):
                p["lods"][2] = me
            else:
                bpy.data.meshes.remove(me)
    for key in todo:
        p = protos[key]
        if not p["cards"]:
            continue
        for level, keep, grow in zip((1, 2), CARD_KEEP, CARD_GROW):
            p["lods"][level] = thin_cards(p["mesh"], keep, grow, f"{key[1]} lod{level}")
    for why, names in sorted(skipped.items()):
        log(f"no LOD, {why}: {len(names)}")
    return skipped
