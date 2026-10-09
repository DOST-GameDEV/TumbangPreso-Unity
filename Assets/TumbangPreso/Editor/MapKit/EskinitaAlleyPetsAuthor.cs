using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE ALLEY'S CATS AND DOGS, ITS ANIMAL VOICES AND ITS AMBIENCE (owner, 2026-10-09: "can you make
    /// actual moving cats and dogs, with cat and dog chases as a background life event. sfx too" and
    /// "the map's ambience sfx is missing too"). Called by `EskinitaAlleyLifeAuthor.Build`, after the
    /// chickens, and like it everything handed to the runtime is measured from the built scene:
    ///   * ONE GRID OF THE ALLEY'S FLOORS for `AlleyPets` (0.25 m cells, each with its height): a cell
    ///     is floor where a cast from above meets a walkable slope at the height the alley's middle
    ///     has at that z (so the terraces and the wall to wall stairs, and never a side flight, a
    ///     ledge or a roof), clear of walls, outside the chalk square, off the bounce tarps and
    ///     outside every placed prop's bounds;
    ///   * the PERCHES a cat escapes up: the tops of the dama table, a bench, a crate stack and the
    ///     tepee's ridge, and every low flat ledge of the collision itself (0.45 to 1.8 m over the
    ///     floor beside it), each with the floor cell beside it;
    ///   * where each animal lives: a tan askal and a calico cat on the top terrace, a white askal
    ///     and a white cat on the low end, each with resting spots against a wall, and the tuxedo
    ///     cat up on the ledge the old loafing statue lay on, with the stretch of ledge it may walk;
    ///   * `AlleyLifeSound` filled with the animal recordings and `AlleySoundscape` with the beds and
    ///     the scattered one-shots (Art/audio/ambience/sfx_alley_*.wav and alley_bed_*.wav, cut from
    ///     CC0 recordings by tools/build_eskinita_life_sfx.py). A cue with no file is reported.
    /// ⚠️ Cosmetic and local: no colliders, not networked, no NavMesh, nothing gameplay reads.
    /// </summary>
    internal static class EskinitaAlleyPetsAuthor
    {
        private const string Root = "Assets/TumbangPreso/Art/EskinitaAlley";
        private const string Audio = "Assets/TumbangPreso/Art/audio/ambience";
        private const float Cell = .25f;
        private static readonly string[] PerchModels = { "prop_crates_stack", "prop_table_dama", "prop_bench_wood" };
        private static readonly Vector3 TepeeRidge = new Vector3(-.3f, .685f, -.05f);
        // Where the kit's two cat statues were (Unity axes): the loafer's ledge first, the sitter's second.
        private static readonly Vector3[] LedgeSpots = { new Vector3(-7.4f, 3.5f, .9f), new Vector3(7.3f, 3.4f, -7.6f) };

        public static string Build(Transform root, Transform life, Dictionary<string, Material> materials, List<MeshCollider> colliders, Vector3 can,
            List<(string model, Transform t, Bounds b)> props, List<(Vector3 at, float r)> pads, AlleyChickens chickens, Action<string> warn)
        {
            // ---- the voices
            var sound = new GameObject("Voices").AddComponent<AlleyLifeSound>();
            sound.transform.SetParent(life, false);
            var banks = new List<AlleyLifeSound.Bank>();
            void Bank(string cue, string files, float gain, float near, float far)
            {
                var clips = Clips(files);
                if (clips.Length == 0) warn($"sound: no recordings for '{cue}' ({Audio}/sfx_alley_{files}_N.wav): it is SILENT until tools/build_eskinita_life_sfx.py has cut them");
                banks.Add(new AlleyLifeSound.Bank { Cue = cue, Clips = clips, Gain = gain, Near = near, Far = far });
            }
            Bank("chicken_cluck", "chicken_cluck", .4f, 1.5f, 16f);
            Bank("rooster_crow", "rooster_crow", .65f, 3f, 40f);
            Bank("chicken_squawk", "chicken_squawk", .55f, 2f, 26f);
            Bank("chicken_flap", "amb_wings", .45f, 2f, 20f);
            Bank("cat_meow", "cat_meow", .45f, 1.5f, 18f);
            Bank("cat_hiss", "cat_hiss", .5f, 1.5f, 18f);
            Bank("cat_purr", "cat_purr", .3f, .8f, 6f);
            Bank("dog_bark", "dog_bark", .65f, 3f, 40f);
            Bank("dog_growl", "dog_growl", .45f, 2f, 18f);
            Bank("dog_whine", "dog_whine", .45f, 2f, 18f);
            Bank("dog_pant", "dog_pant", .35f, 1.2f, 10f);
            Bank("dog_yelp", "dog_yelp", .6f, 2.5f, 30f);
            Bank("dog_scrabble", "dog_scrabble", .4f, 2f, 20f);
            sound.Banks = banks.ToArray();
            if (chickens != null) { chickens.Sound = sound; EditorUtility.SetDirty(chickens); }

            // ---- the ambience
            var scape = new GameObject("Soundscape").AddComponent<AlleySoundscape>();
            scape.transform.SetParent(root, false);
            scape.Neighbourhood = Bed("neighbourhood", warn); scape.Street = Bed("street", warn); scape.Kids = Bed("kids", warn);
            scape.Sparrows = Bed("sparrows", warn); scape.Wind = Bed("wind", warn);
            scape.CourtY = can.y;
            var shots = new List<AlleySoundscape.Shot>();
            void Shot(string name, string files, AlleySoundscape.Place where, float gain, float weight, float near, float far)
            {
                var clips = Clips(files);
                if (clips.Length == 0) { warn($"ambience: no recordings for the one-shot '{name}' (sfx_alley_{files}_N.wav): left out"); return; }
                shots.Add(new AlleySoundscape.Shot { Name = name, Clips = clips, Where = where, Gain = gain, Weight = weight, Near = near, Far = far });
            }
            Shot("motorbike on the street", "amb_motorbike", AlleySoundscape.Place.Street, .5f, 1.2f, 6f, 80f);
            Shot("far rooster", "rooster_crow", AlleySoundscape.Place.Far, .45f, .8f, 8f, 120f);
            Shot("dog a block away", "dog_bark", AlleySoundscape.Place.Far, .4f, 1f, 8f, 120f);
            Shot("gate", "amb_gate", AlleySoundscape.Place.Houses, .4f, .6f, 4f, 60f);
            Shot("dishes from a kitchen", "amb_dishes", AlleySoundscape.Place.Houses, .35f, 1f, 3f, 45f);
            Shot("vendor's hand bell", "amb_bell", AlleySoundscape.Place.Street, .4f, .5f, 5f, 70f);
            Shot("vendor's horn", "amb_horn", AlleySoundscape.Place.Street, .4f, .5f, 5f, 70f);
            Shot("cloth in a gust", "amb_cloth", AlleySoundscape.Place.Overhead, .35f, 1f, 3f, 30f);
            Shot("pigeons' wings", "amb_wings", AlleySoundscape.Place.Roofs, .35f, .8f, 4f, 40f);
            scape.Shots = shots.ToArray();
            int beds = new[] { scape.Neighbourhood, scape.Street, scape.Kids, scape.Sparrows, scape.Wind }.Count(c => c != null);

            // ---- the floor
            int width = Mathf.CeilToInt(18f / Cell), height = Mathf.CeilToInt(37f / Cell);
            var origin = new Vector2(-width * Cell * .5f, -height * Cell * .5f);
            var walk = new bool[width * height]; var heights = new float[width * height];
            var hitY = new float[width * height]; var hitOk = new bool[width * height];
            var rowY = new float[height];
            Vector3 At(int i, int j, float y) => new Vector3(origin.x + (i + .5f) * Cell, y, origin.y + (j + .5f) * Cell);
            for (int j = 0; j < height; j++)
            {
                var middle = new List<float>();
                for (int i = 0; i < width; i++)
                {
                    var p = At(i, j, 2.4f);
                    if (!Cast(colliders, p, Vector3.down, 3.8f, out var hit) || hit.normal.y < .78f) continue;
                    hitOk[j * width + i] = true; hitY[j * width + i] = hit.point.y;
                    if (Mathf.Abs(p.x) < 3f) middle.Add(hit.point.y);
                }
                middle.Sort();
                rowY[j] = middle.Count > 0 ? middle[middle.Count / 2] : float.NaN;
            }
            int cells = 0;
            for (int j = 0; j < height; j++)
                for (int i = 0; i < width; i++)
                {
                    int k = j * width + i;
                    if (!hitOk[k] || float.IsNaN(rowY[j]) || Mathf.Abs(hitY[k] - rowY[j]) > .12f) continue;
                    var p = At(i, j, hitY[k]);
                    if (InChalk(p.x, p.z, can, .2f)) continue;
                    if (pads.Any(q => Mathf.Abs(q.at.y - p.y) < 2.5f && (q.at.x - p.x) * (q.at.x - p.x) + (q.at.z - p.z) * (q.at.z - p.z) < q.r * q.r)) continue;
                    if (props.Any(q => q.b.min.y < p.y + .6f && q.b.max.y > p.y + .05f && p.x > q.b.min.x - .1f && p.x < q.b.max.x + .1f && p.z > q.b.min.z - .1f && p.z < q.b.max.z + .1f)) continue;
                    bool wall = false;
                    // ⚠️ 0.3 m, MORE THAN A CELL: a wall that is one sheet of triangles is only met from its front, so the
                    // ring of refused cells outside it must be a whole cell thick or the flood below leaks through it.
                    foreach (var d in Around)
                        if (Cast(colliders, p + Vector3.up * .2f, d, .3f, out _)) { wall = true; break; }
                    if (wall) continue;
                    walk[k] = true; heights[k] = hitY[k];
                }
            // ⚠️ ONLY WHAT CAN BE WALKED TO FROM THE COURT. The ground goes on under and behind the houses at the
            // same height, and the first bake stood a dog and a cat indoors. Flooded from just outside the chalk square.
            {
                var keep = new bool[walk.Length]; var queue = new Queue<int>();
                float r = Balance.ConfinementRadius + .6f;
                foreach (var seed in new[] { new Vector3(can.x, can.y, can.z + r), new Vector3(can.x, can.y, can.z - r), new Vector3(can.x + 3f, can.y, can.z + r), new Vector3(can.x - 3f, can.y, can.z - r) })
                {
                    int ci = Mathf.FloorToInt((seed.x - origin.x) / Cell), cj = Mathf.FloorToInt((seed.z - origin.y) / Cell);
                    for (int dj = -2; dj <= 2; dj++)
                        for (int di = -2; di <= 2; di++)
                        {
                            int q = (cj + dj) * width + ci + di;
                            if (ci + di < 0 || cj + dj < 0 || ci + di >= width || cj + dj >= height || !walk[q] || keep[q]) continue;
                            keep[q] = true; queue.Enqueue(q);
                        }
                }
                while (queue.Count > 0)
                {
                    int c = queue.Dequeue(), ci = c % width, cj = c / width;
                    foreach (var (di, dj) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        int ni = ci + di, nj = cj + dj;
                        if (ni < 0 || nj < 0 || ni >= width || nj >= height) continue;
                        int q = nj * width + ni;
                        if (!walk[q] || keep[q] || Mathf.Abs(heights[q] - heights[c]) > .22f) continue;
                        keep[q] = true; queue.Enqueue(q);
                    }
                }
                for (int k = 0; k < walk.Length; k++) { walk[k] = keep[k]; if (keep[k]) cells++; }
            }
            if (cells < 200) warn($"pets: only {cells} floor cells were found in the alley; the cats and dogs will hardly move");
            int CellNear(Vector3 p, float within, float yWithin)
            {
                int pick = -1; float best = within * within;
                int ci = Mathf.FloorToInt((p.x - origin.x) / Cell), cj = Mathf.FloorToInt((p.z - origin.y) / Cell), r = Mathf.CeilToInt(within / Cell);
                for (int j = Mathf.Max(0, cj - r); j <= Mathf.Min(height - 1, cj + r); j++)
                    for (int i = Mathf.Max(0, ci - r); i <= Mathf.Min(width - 1, ci + r); i++)
                    {
                        if (!walk[j * width + i] || Mathf.Abs(heights[j * width + i] - p.y) > yWithin) continue;
                        var c = At(i, j, 0); float d = (c.x - p.x) * (c.x - p.x) + (c.z - p.z) * (c.z - p.z);
                        if (d < best) { best = d; pick = j * width + i; }
                    }
                return pick;
            }
            Vector3 Centre(int k) => At(k % width, k / width, heights[k]);

            // ---- the perches
            var perches = new List<AlleyPets.Perch>();
            void Perch(Vector3 top)
            {
                if (InChalk(top.x, top.z, can, 0f) || perches.Any(q => Vector3.Distance(q.Top, top) < 1.4f)) return;
                if (pads.Any(q => (q.at.x - top.x) * (q.at.x - top.x) + (q.at.z - top.z) * (q.at.z - top.z) < (q.r + .2f) * (q.r + .2f))) return;
                int foot = CellNear(new Vector3(top.x, top.y - .9f, top.z), 1.3f, 1.1f);
                if (foot < 0) return;
                float rise = top.y - heights[foot];
                if (rise < .3f || rise > 1.85f) return;
                perches.Add(new AlleyPets.Perch { Top = top, Foot = Centre(foot) });
            }
            foreach (var p in props)
            {
                if (p.model == "prop_manok_cage") Perch(p.t.TransformPoint(TepeeRidge));
                else if (Array.IndexOf(PerchModels, p.model) >= 0) Perch(new Vector3(p.b.center.x, p.b.max.y, p.b.center.z));
            }
            int propPerches = perches.Count;
            for (int j = 0; j < height && perches.Count < 40; j++)
                for (int i = 0; i < width && perches.Count < 40; i++)
                {
                    if (walk[j * width + i] || float.IsNaN(rowY[j])) continue;
                    var from = At(i, j, rowY[j] + 2.3f);
                    if (!Cast(colliders, from, Vector3.down, 2.6f, out var hit) || hit.normal.y < .95f) continue;
                    float rise = hit.point.y - rowY[j];
                    if (rise < .45f || rise > 1.8f) continue;
                    bool flat = true;
                    foreach (var d in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                        if (!Cast(colliders, from + d * .14f, Vector3.down, 2.6f, out var side) || Mathf.Abs(side.point.y - hit.point.y) > .03f) { flat = false; break; }
                    if (flat) Perch(hit.point);
                }

            // ---- who lives where
            var holder = new GameObject("Pets").transform;
            holder.SetParent(life, false);
            var pets = holder.gameObject.AddComponent<AlleyPets>();
            pets.GridOrigin = origin; pets.Cell = Cell; pets.Width = width; pets.Height = height; pets.Walkable = walk; pets.Heights = heights;
            pets.Perches = perches.ToArray(); pets.Chickens = chickens; pets.Sound = sound;
            var list = new List<AlleyPets.Pet>();
            var said = new List<string>();
            var taken = new List<Vector3>();
            if (chickens != null) foreach (var g in chickens.Groups) taken.Add(g.Home);
            var random = new System.Random(90210);

            // A terrace's resting spots: floor cells against a wall (shade, and out of everybody's way), well apart.
            List<Vector3> Spots(float zFrom, float zTo, int count, float apart)
            {
                var good = new List<Vector3>(); var open = new List<Vector3>();
                for (int j = 2; j < height - 2; j++)
                    for (int i = 2; i < width - 2; i++)
                    {
                        if (!walk[j * width + i]) continue;
                        var c = At(i, j, heights[j * width + i]);
                        if (c.z < zFrom || c.z > zTo || Mathf.Abs(c.y - rowY[j]) > .03f) continue;
                        // Level floor all round it (not a stair), and a wall within two cells on some side.
                        bool level = true, walled = false;
                        if (Mathf.Abs(c.x) > 9f) continue;
                        for (int dj = -1; dj <= 1 && level; dj++)
                            for (int di = -1; di <= 1; di++)
                                if (!walk[(j + dj) * width + i + di] || Mathf.Abs(heights[(j + dj) * width + i + di] - c.y) > .02f) { level = false; break; }
                        if (!level) continue;
                        foreach (var d in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                            if (Cast(colliders, c + Vector3.up * .3f, d, 1.3f, out _)) { walled = true; break; }
                        if (walled) good.Add(c); else open.Add(c);
                    }
                var picked = new List<Vector3>();
                for (int tries = 0; tries < 400 && picked.Count < count && good.Count > 0; tries++)
                {
                    var c = good[random.Next(good.Count)];
                    if (picked.Any(q => Vector3.Distance(q, c) < apart) || taken.Any(q => Vector3.Distance(q, c) < 1.8f)) continue;
                    picked.Add(c);
                }
                // Not enough wall to go round (the props line it): any level floor of the terrace will do.
                for (int tries = 0; tries < 400 && picked.Count < count && open.Count > 0; tries++)
                {
                    var c = open[random.Next(open.Count)];
                    if (picked.Any(q => Vector3.Distance(q, c) < apart) || taken.Any(q => Vector3.Distance(q, c) < 1.8f)) continue;
                    picked.Add(c);
                }
                taken.AddRange(picked);
                return picked;
            }
            Transform Stand(string model, Vector3 at, float yaw)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/pet_{model}.glb");
                if (prefab == null) { warn($"pets: missing model Models/pet_{model}.glb (run tools/author_eskinita_pets.py)"); return null; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
                go.name = model;
                go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
                EskinitaAlleyLifeAuthor.Dress(go, materials, warn, "pets");
                foreach (string part in new[] { "trunk", "head", "tail", "leg_fl", "leg_fr", "leg_bl", "leg_br" })
                    if (go.GetComponentsInChildren<Transform>(true).All(t => t.name != part)) warn($"pets: pet_{model}.glb has no part named '{part}'; it will not be posed");
                return go.transform;
            }
            void Ground(string model, bool dog, float zFrom, float zTo, string where)
            {
                var spots = Spots(zFrom, zTo, 3, 2.6f);
                if (spots.Count == 0) { warn($"pets: no resting spot for {model} on the {where}; it is left out"); return; }
                var t = Stand(model, spots[0], (float)random.NextDouble() * 360f);
                if (t == null) return;
                list.Add(new AlleyPets.Pet { Name = model, Root = t, Dog = dog, Home = spots[0], Spots = spots.ToArray() });
                said.Add($"{model} on the {where} at ({spots[0].x:0.0}, {spots[0].y:0.0}, {spots[0].z:0.0}) with {spots.Count} spots");
            }
            // The wall to wall stairs are where the alley's middle changes height: the terraces are either side of them.
            float topEnd = float.NaN, lowEnd = float.NaN;
            for (int j = 0; j < height; j++)
            {
                if (float.IsNaN(rowY[j])) continue;
                float z = origin.y + (j + .5f) * Cell;
                if (rowY[j] > can.y + .5f) topEnd = float.IsNaN(topEnd) ? z : Mathf.Max(topEnd, z);
                if (rowY[j] < can.y - .5f) lowEnd = float.IsNaN(lowEnd) ? z : Mathf.Min(lowEnd, z);
            }
            if (float.IsNaN(topEnd)) topEnd = -10.5f;
            if (float.IsNaN(lowEnd)) lowEnd = 10.5f;
            Ground("dog_tan", true, -99f, topEnd, "top terrace");
            Ground("cat_calico", false, -99f, topEnd, "top terrace");
            Ground("dog_white", true, lowEnd, 99f, "low end");
            Ground("cat_white", false, lowEnd, 99f, "low end");

            // The ledge cat: where the loafing statue lay, and as far along that ledge as it stays level.
            bool ledged = false;
            foreach (var spot in LedgeSpots)
            {
                if (ledged || !Cast(colliders, spot + Vector3.up * 1.2f, Vector3.down, 2.2f, out var top) || top.normal.y < .95f) continue;
                float y = top.point.y, zA = spot.z, zB = spot.z;
                for (int dir = -1; dir <= 1; dir += 2)
                    for (float z = spot.z; Mathf.Abs(z - spot.z) < 8f; z += dir * .2f)
                    {
                        var p = new Vector3(spot.x, y + .6f, z);
                        if (!Cast(colliders, p, Vector3.down, 1f, out var h) || Mathf.Abs(h.point.y - y) > .04f) break;
                        if (Cast(colliders, new Vector3(spot.x, y + .2f, z), Vector3.forward * dir, .3f, out _)) break;
                        // Not through whatever stands on the ledge (a shelf of pots): placed things have no collision.
                        if (props.Any(q => q.b.min.y < y + .5f && q.b.max.y > y + .05f && spot.x > q.b.min.x - .15f && spot.x < q.b.max.x + .15f && z > q.b.min.z - .2f && z < q.b.max.z + .2f)) break;
                        if (dir < 0) zA = z; else zB = z;
                    }
                if (zB - zA < 1.4f) continue;
                var a = new Vector3(spot.x, y, zA + .3f); var b = new Vector3(spot.x, y, zB - .3f);
                var t = Stand("cat_tuxedo", new Vector3(spot.x, y, Mathf.Clamp(spot.z, a.z, b.z)), spot.x < 0 ? 90f : -90f);
                if (t == null) break;
                list.Add(new AlleyPets.Pet { Name = "cat_tuxedo", Root = t, Dog = false, Home = t.position, Ledge = true, LedgeA = a, LedgeB = b });
                said.Add($"cat_tuxedo on the ledge at ({spot.x:0.0}, {y:0.0}, {zA:0.0}..{zB:0.0})");
                ledged = true;
            }
            if (!ledged) warn("pets: no level ledge was found where the cat statues were; the ledge cat is left out");
            pets.Pets = list.ToArray();
            EditorUtility.SetDirty(pets); EditorUtility.SetDirty(sound); EditorUtility.SetDirty(scape);
            return $"pets: {list.Count} animals on {cells} floor cells with {perches.Count} perches ({propPerches} on props, {perches.Count - propPerches} ledges): {string.Join("; ", said)}; " +
                   $"voices: {banks.Count(b => b.Clips.Length > 0)} of {banks.Count} cues have recordings ({banks.Sum(b => b.Clips.Length)} clips); " +
                   $"ambience: {beds} of 5 beds, {shots.Count} one-shot kinds ({shots.Sum(s => s.Clips.Length)} clips)";
        }

        private static readonly Vector3[] Around =
        {
            Vector3.right, Vector3.left, Vector3.forward, Vector3.back,
            new Vector3(.7071f, 0, .7071f), new Vector3(-.7071f, 0, .7071f), new Vector3(.7071f, 0, -.7071f), new Vector3(-.7071f, 0, -.7071f),
        };

        private static bool InChalk(float x, float z, Vector3 can, float margin)
        {
            float r = Balance.ConfinementRadius + margin;
            return Mathf.Abs(x - can.x) < r && Mathf.Abs(z - can.z) < r;
        }

        private static bool Cast(List<MeshCollider> colliders, Vector3 from, Vector3 direction, float length, out RaycastHit nearest)
        {
            nearest = default; bool any = false; float best = float.MaxValue;
            var ray = new Ray(from, direction);
            foreach (var c in colliders)
                if (c != null && c.Raycast(ray, out var hit, length) && hit.distance < best) { best = hit.distance; nearest = hit; any = true; }
            return any;
        }

        /// <summary>sfx_alley_NAME_1.wav, _2 ... as far as they go.</summary>
        private static AudioClip[] Clips(string name)
        {
            var found = new List<AudioClip>();
            for (int n = 1; n < 40; n++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Audio}/sfx_alley_{name}_{n}.wav");
                if (clip == null) break;
                found.Add(clip);
            }
            return found.ToArray();
        }

        /// <summary>A bed imports as ADPCM held compressed in memory: a minute of PCM is 5 MB a bed, and Vorbis
        /// can pad a loop's seam (the click `LagoonCoveLife.Clip` avoids with PCM on its two short beds).</summary>
        private static AudioClip Bed(string name, Action<string> warn)
        {
            string path = $"{Audio}/alley_bed_{name}.wav";
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) { warn($"ambience: no bed '{name}' ({path}): that layer is SILENT until tools/build_eskinita_life_sfx.py has cut it"); return null; }
            var s = importer.defaultSampleSettings;
            if (s.compressionFormat != AudioCompressionFormat.ADPCM || s.loadType != AudioClipLoadType.CompressedInMemory || !importer.forceToMono)
            {
                s.compressionFormat = AudioCompressionFormat.ADPCM; s.loadType = AudioClipLoadType.CompressedInMemory;
                importer.defaultSampleSettings = s; importer.forceToMono = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ------------------------------------------------------------------ the motion sheets

        private const int Tile = 480, TileH = 300;

        /// <summary>The cats and dogs stepped by hand in edit mode: one calm sheet per animal (ten moments
        /// over 40 s), then each event staged and photographed every half second from the dog noticing
        /// to it trotting home, and a chicken burst and its walk back in. Nothing is saved.</summary>
        internal static string Motion(int version)
        {
            var log = new StringBuilder();
            var written = new List<string>();
            Directory.CreateDirectory("Logs/eskinita");

            // ---- calm
            Open(out var pets, out var chickens, out var camera);
            if (pets == null) throw new InvalidOperationException("the built scene has no cats or dogs (build it first)");
            try
            {
                var times = new[] { 2f, 6f, 10f, 14f, 18f, 22f, 26f, 30f, 35f, 40f };
                var sheets = pets.Pets.Select(_ => new Texture2D(Tile * 5, TileH * 2, TextureFormat.RGB24, false)).ToList();
                float time = 0f;
                for (int m = 0; m < times.Length; m++)
                {
                    while (time < times[m]) { time += 1f / 30f; pets.Step(1f / 30f); if (chickens != null) chickens.Step(1f / 30f); }
                    log.AppendLine($"calm t {times[m]:0}: {pets.Describe()}");
                    for (int p = 0; p < pets.Pets.Length; p++)
                    {
                        var at = pets.Pets[p].Root.position;
                        float size = pets.Pets[p].Dog ? 1f : .7f;
                        var eye = at + new Vector3(-Mathf.Sign(at.x) * 1.9f, .75f, .9f) * size;
                        camera.fieldOfView = 34f;
                        camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(at + Vector3.up * .22f * size - eye));
                        sheets[p].SetPixels32(m % 5 * Tile, (1 - m / 5) * TileH, Tile, TileH, EskinitaAlleyLifeAuthor.Shot(camera, Tile, TileH));
                    }
                }
                for (int p = 0; p < sheets.Count; p++)
                {
                    sheets[p].Apply();
                    string path = $"Logs/eskinita/pets_motion_v{version}_calm_{pets.Pets[p].Name}.png";
                    File.WriteAllBytes(path, sheets[p].EncodeToPNG()); written.Add(Path.GetFileName(path));
                    Object.DestroyImmediate(sheets[p]);
                }
            }
            finally { Object.DestroyImmediate(camera.gameObject); }

            // ---- the events, each from a fresh scene
            foreach (var (kind, name, frames) in new[] { (1, "chase", 24), (2, "swat", 18), (3, "chickens", 18) })
            {
                Open(out pets, out chickens, out camera);
                try
                {
                    for (int s = 0; s < 45; s++) { pets.Step(1f / 30f); if (chickens != null) chickens.Step(1f / 30f); }
                    bool started = pets.StageEvent(kind);
                    log.AppendLine($"{name}: staged {started}. {pets.Describe()}");
                    if (!started) continue;
                    // The camera stays across the alley from the pair and follows the point between them (a fixed one lost the run).
                    var dogAt = pets.EventDog.position;
                    var catAt = pets.EventCat != null ? pets.EventCat.position : (chickens != null ? chickens.Groups.OrderBy(g => Vector3.Distance(g.Home, dogAt)).First().Home : dogAt);
                    var mid = (dogAt + catAt) * .5f;
                    var eye = new Vector3(mid.x > 0 ? mid.x - 3.8f : mid.x + 3.8f, mid.y + 1.5f, mid.z + (mid.z > 0 ? 1.2f : -1.2f));
                    camera.fieldOfView = 56f;
                    camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(mid + new Vector3(0, .25f, mid.z > 0 ? 1.2f : -1.2f) - eye));
                    int cols = 6, rows = Mathf.CeilToInt(frames / (float)cols);
                    var sheet = new Texture2D(Tile * cols, TileH * rows, TextureFormat.RGB24, false);
                    for (int f = 0; f < frames; f++)
                    {
                        for (int s = 0; s < 15; s++) { pets.Step(1f / 30f); if (chickens != null) chickens.Step(1f / 30f); }
                        var now = pets.EventCat != null ? (pets.EventDog.position + pets.EventCat.position) * .5f : (pets.EventDog.position + catAt) * .5f;
                        float wide = pets.EventCat != null ? Vector3.Distance(pets.EventDog.position, pets.EventCat.position) : 2f;
                        var from = new Vector3(mid.x > 0 ? now.x - 3.2f - wide * .5f : now.x + 3.2f + wide * .5f, now.y + 1.5f, now.z);
                        camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(now + Vector3.up * .3f - from));
                        sheet.SetPixels32(f % cols * Tile, (rows - 1 - f / cols) * TileH, Tile, TileH, EskinitaAlleyLifeAuthor.Shot(camera, Tile, TileH));
                        log.AppendLine($"{name} t {(f + 1) * .5f:0.0}: {pets.Describe()}  [last cue {(pets.Sound != null ? pets.Sound.LastCue : "none")}]");
                    }
                    sheet.Apply();
                    string path = $"Logs/eskinita/pets_motion_v{version}_{name}.png";
                    File.WriteAllBytes(path, sheet.EncodeToPNG()); written.Add(Path.GetFileName(path));
                    Object.DestroyImmediate(sheet);
                }
                finally { Object.DestroyImmediate(camera.gameObject); }
            }

            // ---- a chicken burst, and the same bird walking back in
            Open(out pets, out chickens, out camera);
            try
            {
                if (chickens != null && chickens.Groups.Length > 0)
                {
                    var g = chickens.Groups[0];
                    var sheet = new Texture2D(Tile * 6, TileH * 2, TextureFormat.RGB24, false);
                    void Frame(int f)
                    {
                        var eye = g.Home + new Vector3(-Mathf.Sign(g.Home.x) * 3.4f, 1.8f, .6f);
                        camera.fieldOfView = 42f;
                        camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(g.Home + Vector3.up * .25f - eye));
                        sheet.SetPixels32(f % 6 * Tile, (1 - f / 6) * TileH, Tile, TileH, EskinitaAlleyLifeAuthor.Shot(camera, Tile, TileH));
                    }
                    for (int s = 0; s < 30; s++) chickens.Step(1f / 30f);
                    Frame(0);
                    chickens.BurstForReview(0, 0);
                    log.AppendLine("burst: bird 0 of group 0 burst. " + chickens.Describe());
                    // The feathers are made in Play only by a real hit's pool; here they are the same pool, stepped by hand.
                    for (int f = 1; f < 6; f++) { for (int s = 0; s < 8; s++) chickens.Step(1f / 30f); Frame(f); }
                    float waited = 0f;
                    while (waited < 70f && chickens.Describe().Contains("BURST")) { chickens.Step(1f / 20f); waited += 1f / 20f; }
                    log.AppendLine($"burst: it came back after {waited:0.0} s more. " + chickens.Describe());
                    for (int f = 6; f < 12; f++) { Frame(f); for (int s = 0; s < 12; s++) chickens.Step(1f / 30f); }
                    log.AppendLine("burst: 2.4 s later. " + chickens.Describe() + $"  bursts {chickens.Bursts}");
                    sheet.Apply();
                    string path = $"Logs/eskinita/pets_motion_v{version}_chicken_burst.png";
                    File.WriteAllBytes(path, sheet.EncodeToPNG()); written.Add(Path.GetFileName(path));
                    Object.DestroyImmediate(sheet);
                }
            }
            finally { Object.DestroyImmediate(camera.gameObject); }
            File.WriteAllText($"Logs/eskinita/pets_motion_v{version}.txt", log.ToString());
            return $"pets motion v{version}: {string.Join(", ", written)} and pets_motion_v{version}.txt";
        }

        private static void Open(out AlleyPets pets, out AlleyChickens chickens, out Camera camera)
        {
            EditorSceneManager.OpenScene(EskinitaAlleySceneBuilder.ScenePath, OpenSceneMode.Single);
            pets = Object.FindObjectsByType<AlleyPets>(FindObjectsSortMode.None).FirstOrDefault();
            chickens = Object.FindObjectsByType<AlleyChickens>(FindObjectsSortMode.None).FirstOrDefault();
            if (pets != null) pets.Begin(20261009);
            if (chickens != null) chickens.Begin(20261009);
            camera = new GameObject("Pets motion witness") { hideFlags = HideFlags.DontSave }.AddComponent<Camera>();
            camera.enabled = false; camera.nearClipPlane = .05f; camera.farClipPlane = 400;
            try { camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene(); camera.gameObject.AddComponent<WorldOutline>().PrototypeEnabled = true; }
            catch (Exception e) { Debug.LogWarning("[EskinitaAlley] pets motion: no grade or outline on the witness: " + e.Message); }
        }
    }
}
