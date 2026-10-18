using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Map;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE BASIN, DRESSED (2026-10-19). Owner: "need you to fix up the rest of the barren stuff here" (the paved ring round
    /// the stage: big empty grey paving, plain walls, a few tiny props), "get a parallel agent to work whats on the basin
    /// walls itself", and "bulul statues should be larger and look more grand" then "current model for bulul is okay,
    /// they just need to be sized up, no need to remodel".
    ///
    /// The court platform (court_platform.glb, tools/payo_pieces/court_build.py) is a horseshoe: paving at y -6 between the
    /// pit's rim lip (|x| 30, z -30; its top -5.65, its pit side at 29.8) and the outer walls (|x| 44, z -62). The outer
    /// coping is a low parapet on the north half and, from z -7 southward, a retaining wall stepping up to y 6.2, its
    /// plaza face at |x| 42.8 and z -60.8. All of it lies OUTSIDE the play walls (|x|, |z| 22): nothing here is reached
    /// by a player or a slipper, and nothing here has a collider. Dressing still keeps to the walls, the corners and the
    /// plinths, leaving the paving's middle open (it is the ring the eye runs round from the court).
    ///
    ///  * The bulul: the art's own models, unchanged, sized up (seated 1.0 to 1.4, standing 1.0 to 1.7) and raised on
    ///    `plaza_plinth` (tools/payo_pieces/plaza_kit.py: stepped, the walls' fret course carved round it, court stone),
    ///    turned to face the court; a new standing pair guards the south corners. Fire bowls flank each plinth.
    ///  * The walls: creeper curtains (`plaza_vine`, `plaza_vine_long`) spill over the pit's rim lip into the pit and down
    ///    the tall retaining walls' plaza faces, clear of their sun panels and the pit's niches.
    ///  * The paving: stone planters (`plaza_planter`, its rim the copings' woven band) of crotons, gumamela, monstera,
    ///    banana and grass along the parapets and the south wall; round-crowned trees in the south corners; tall cloth banners along
    ///    the lip (tall enough to show over the court's floor from the middle); a rice-pounding corner either side of the
    ///    gate; grass and fern in the joints at the walls' feet.
    /// Hooked from `Build` right after `Dressing`. Returns one line for the build log.
    /// </summary>
    public static partial class PayoSceneBuilder
    {
        private const float PlazaY = -6.0f, LipTop = -5.65f, LipFace = 29.8f, RetainTop = 6.2f, RetainFace = 42.8f, RetainFaceS = -60.8f;

        private static int _plazaPieces, _plazaPlants, _plazaMissing, _plazaMoved, _plazaDropped;
        /// <summary>Colliders lent for this build only: the court platform's mesh (the paving to seat on) and a box round
        /// every piece already seated (so nothing is put inside another). All are removed before the scene is saved.</summary>
        private static readonly List<Collider> PlazaLent = new List<Collider>();
        private static readonly HashSet<Collider> PlazaGround = new HashSet<Collider>();
        private static Transform _plazaBoxes;
        private static readonly Dictionary<string, int> PlazaWhy = new Dictionary<string, int>();
        private static void PlazaNo(GameObject go, string why)
        {
            string key = go.name.Split(' ')[0] + " " + why;
            PlazaWhy[key] = PlazaWhy.TryGetValue(key, out int n) ? n + 1 : 1;
        }

        /// <summary>
        /// 2026-10-19 (owner: "ambient occlusion flashing and bleeding through walls"): EVERY piece meant to stand on the paving
        /// (asked for at `PlazaY`) is SEATED: a ray from 40 m above, against everything solid in the scene (the stage's own
        /// boxes, the court platform's mesh lent a collider, the pieces already seated), at its middle and four points of its
        /// footprint. It is kept only if every ray first meets the court platform's open paving at y -6 (+-0.12, facing up);
        /// otherwise it is dropped (it would be inside a wall, the lip, the inlet's basin, another piece, or under the court).
        /// </summary>
        private static string Plaza(Transform root, Dictionary<string, Material> named)
        {
            PlazaLent.Clear(); PlazaGround.Clear();
            var court = root.Find("Dressing/court_platform");
            if (court != null)
                foreach (var mf in court.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    PlazaLent.Add(mc); PlazaGround.Add(mc);
                }
            if (PlazaGround.Count == 0) Warn("plaza: no court_platform under Dressing: pieces are NOT seated (nothing to seat on)");
            _plazaBoxes = Group(root, "~plaza seating boxes");
            Physics.SyncTransforms();
            try { return PlazaBody(root, named); }
            finally
            {
                foreach (var c in PlazaLent) if (c != null) Object.DestroyImmediate(c);
                PlazaLent.Clear(); PlazaGround.Clear();
                if (_plazaBoxes != null) Object.DestroyImmediate(_plazaBoxes.gameObject);
                Physics.SyncTransforms();
            }
        }

        /// <summary>Seats a piece on the paving (see `Plaza`): true and moved to the paving's height, or false (drop it).</summary>
        private static bool PlazaSeat(GameObject go)
        {
            if (PlazaGround.Count == 0) return true;
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return true;
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            Vector3 c = b.center, e = b.extents * 0.7f;
            var points = new[] { c, c + new Vector3(e.x, 0, e.z), c + new Vector3(-e.x, 0, e.z), c + new Vector3(e.x, 0, -e.z), c + new Vector3(-e.x, 0, -e.z) };
            float y = PlazaY;
            for (int i = 0; i < points.Length; i++)
            {
                var from = new Vector3(points[i].x, PlazaY + 40.0f, points[i].z);
                if (!Physics.Raycast(from, Vector3.down, out var hit, 80.0f, ~0, QueryTriggerInteraction.Ignore)) { PlazaNo(go, "nothing under it"); return false; }
                if (!PlazaGround.Contains(hit.collider)) { PlazaNo(go, "on " + (hit.collider.transform.parent == _plazaBoxes ? "another piece" : hit.collider.name)); return false; }
                if (Mathf.Abs(hit.point.y - PlazaY) > 0.12f || hit.normal.y < 0.97f) { PlazaNo(go, "on the court at y " + hit.point.y.ToString("0.0")); return false; }
                if (i == 0) y = hit.point.y;
            }
            var p = go.transform.position;
            if (Mathf.Abs(p.y - y) > 0.005f) { go.transform.position = new Vector3(p.x, y, p.z); _plazaMoved++; }
            // a box round it, so nothing later is seated inside it
            var box = new GameObject("box").AddComponent<BoxCollider>();
            box.transform.SetParent(_plazaBoxes, false);
            box.transform.position = new Vector3(b.center.x, y + b.size.y * 0.5f + 0.02f, b.center.z);
            box.size = new Vector3(b.size.x * 0.92f, b.size.y, b.size.z * 0.92f);
            Physics.SyncTransforms();
            return true;
        }

        private static string PlazaBody(Transform root, Dictionary<string, Material> named)
        {
            _plazaPieces = _plazaPlants = _plazaMissing = _plazaMoved = _plazaDropped = 0;
            PlazaWhy.Clear();
            var plaza = Group(root, "Plaza");
            var flames = new List<PayoFlame>();
            string bulul = PlazaBulul(root, plaza, named, flames);

            // ---- the walls: creepers over the pit's rim lip (front into the pit) and down the retaining walls
            var vines = Group(plaza, "vines");
            var rng = new System.Random(1019);
            int vined = 0;
            float[] niches = { -14.0f, 0.0f, 14.0f };                       // the pit's west wall niches (z), -19.4 .. -14.6 deep
            for (int side = -1; side <= 1; side += 2)
            {
                int k = 0;
                for (float z = -26.0f; z <= 52.0f; z += 6.5f, k++)
                {
                    float zz = z + (float)(rng.NextDouble() - 0.5) * 2.4f;
                    bool longer = k % 2 == 1 && !(side < 0 && niches.Any(n => Mathf.Abs(n - zz) < 3.5f));
                    if (Mathf.Abs(zz - 30.0f) < 1.5f) continue;              // the pit floor's north edge (the gorge corner)
                    if (Vine(longer ? "plaza_vine_long" : "plaza_vine", vines, new Vector3(side * LipFace, LipTop, zz), side > 0 ? 270.0f : 90.0f, named, rng)) vined++;
                }
            }
            for (float x = -24.0f; x <= 24.0f; x += 8.0f)                    // the pit's south wall, seen straight on from the court
                if (Vine(((int)(x / 8.0f)) % 2 == 0 ? "plaza_vine_long" : "plaza_vine", vines, new Vector3(x + (float)(rng.NextDouble() - 0.5) * 2.0f, LipTop, -LipFace), 0.0f, named, rng)) vined++;
            // the tall retaining walls (top 6.2): the west and east faces from z -22 to -60.8, the south face out to |x| 30,
            // clear of the sun panels (west z -32, -52; east z -34, -54; south x -37.5, 36.5)
            foreach (var (x, z, yaw) in new[]
            {
                (-RetainFace, -25.5f, 90.0f), (-RetainFace, -42.0f, 90.0f), (-RetainFace, -58.0f, 90.0f),
                (RetainFace, -24.5f, 270.0f), (RetainFace, -44.0f, 270.0f), (RetainFace, -59.0f, 270.0f),
                (-41.2f, RetainFaceS, 0.0f), (-33.0f, RetainFaceS, 0.0f), (32.5f, RetainFaceS, 0.0f), (40.6f, RetainFaceS, 0.0f),
            })
                if (Vine("plaza_vine_long", vines, new Vector3(x, RetainTop, z), yaw, named, rng)) vined++;

            // ---- the paving: planters of plants along the parapets and the south wall
            var beds = Group(plaza, "planters");
            int planters = 0;
            foreach (var (x, z, yaw) in new[]
            {
                (-41.4f, 40.0f, 90.0f), (41.4f, 40.0f, 90.0f), (-41.4f, 26.0f, 90.0f), (41.4f, 26.0f, 90.0f),
                (-41.4f, -8.0f, 90.0f), (41.4f, -8.0f, 90.0f), (-41.4f, -42.0f, 90.0f), (41.4f, -42.0f, 90.0f),
                (-8.5f, -59.4f, 0.0f), (8.5f, -59.4f, 0.0f), (-20.5f, -59.4f, 0.0f), (20.5f, -59.4f, 0.0f),
                // and along the lip's paving side, between the banners, framing the ring from the court
                (-9.5f, -32.55f, 0.0f), (9.5f, -32.55f, 0.0f), (-19.0f, -32.55f, 0.0f), (19.0f, -32.55f, 0.0f),
                (-32.55f, -16.0f, 90.0f), (32.55f, -16.0f, 90.0f), (-32.55f, 28.0f, 90.0f), (32.55f, 28.0f, 90.0f),
                (-32.55f, 41.0f, 90.0f), (32.55f, 41.0f, 90.0f),
            })
                if (Planter(beds, new Vector3(x, PlazaY, z), yaw, named, rng)) planters++;

            // ---- trees in the south corners, under the retaining walls' creepers (the town's tree_round)
            var trees = Group(plaza, "trees");
            foreach (var (model, x, z, yaw, scale) in new[]
            {
                ("tree_round", -39.6f, -35.5f, 40.0f, 1.0f), ("tree_round", 39.8f, -36.5f, 200.0f, 1.1f),
                ("tree_round", -14.5f, -54.5f, 120.0f, 0.95f), ("tree_round", 14.5f, -54.5f, 300.0f, 0.9f),
            })
                PlazaPlant(model, trees, new Vector3(x, PlazaY, z), yaw, scale, named, true);

            // ---- banners along the lip: 7.5 m, so their cloth shows over the court's floor from its middle
            var flags = Group(plaza, "banners");
            for (int side = -1; side <= 1; side += 2)
                foreach (float z in new[] { -22.0f, -10.0f, 20.0f, 36.0f })
                    PlazaPut("banner", flags, new Vector3(side * 31.9f, PlazaY, z), side > 0 ? 270.0f : 90.0f, 1.35f, true, named);
            foreach (float x in new[] { -26.0f, 26.0f })
                PlazaPut("banner", flags, new Vector3(x, PlazaY, -31.9f), 0.0f, 1.35f, true, named);

            // ---- a rice-pounding corner either side of the gate (the court's people dry and pound rice here)
            var work = Group(plaza, "work");
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                PlazaPut("drying_rack", work, new Vector3(s * 28.6f, PlazaY, -59.2f), s > 0 ? 180.0f : 0.0f, 1.0f, true, named);
                PlazaPut("mortar_pestles", work, new Vector3(s * 25.6f, PlazaY, -55.6f), s > 0 ? 200.0f : 160.0f, 1.0f, true, named);
                PlazaPut("bilao_rice", work, new Vector3(s * 27.6f, PlazaY, -55.0f), 20.0f, 1.0f, true, named);
                PlazaPut("bilao_rice", work, new Vector3(s * 29.0f, PlazaY, -55.9f), 75.0f, 0.9f, true, named);
                PlazaPut("basket_stack", work, new Vector3(s * 31.0f, PlazaY, -58.6f), 0.0f, 1.0f, true, named);
                PlazaPut("pasiking", work, new Vector3(s * 24.6f, PlazaY, -58.9f), s > 0 ? 190.0f : 170.0f, 1.0f, true, named);
                PlazaPut("tapuy_jar_large", work, new Vector3(s * 30.6f, PlazaY, -56.6f), 30.0f, 1.0f, true, named);
                PlazaPut("tapuy_jar_small", work, new Vector3(s * 31.4f, PlazaY, -55.9f), 0.0f, 1.0f, true, named);
            }

            // ---- grass and fern in the joints at the walls' feet and round the plinths
            var joints = Group(plaza, "joints");
            int tufts = 0;
            for (int side = -1; side <= 1; side += 2)
            {
                for (float z = -29.0f; z <= 54.0f; z += 2.2f)
                {
                    if (rng.NextDouble() < 0.55) continue;
                    float x = side * (31.85f + (float)rng.NextDouble() * 0.5f);
                    if (Tuft(joints, new Vector3(x, PlazaY, z + (float)(rng.NextDouble() - 0.5) * 1.2f), named, rng)) tufts++;
                }
                for (float z = -60.0f; z <= 54.0f; z += 2.6f)
                {
                    if (rng.NextDouble() < 0.65) continue;
                    float x = side * (42.0f - (float)rng.NextDouble() * 0.4f);
                    if (Tuft(joints, new Vector3(x, PlazaY, z), named, rng)) tufts++;
                }
            }
            for (float x = -41.0f; x <= 41.0f; x += 2.4f)
            {
                if (Mathf.Abs(x) < 5.5f || rng.NextDouble() < 0.6) continue;
                if (Tuft(joints, new Vector3(x, PlazaY, -60.0f + (float)rng.NextDouble() * 0.4f), named, rng)) tufts++;
                if (Mathf.Abs(x) < 29.0f && rng.NextDouble() < 0.6 && Tuft(joints, new Vector3(x, PlazaY, -31.8f - (float)rng.NextDouble() * 0.4f), named, rng)) tufts++;
            }
            // a few out in the paving's joints (the slabs are 3 m: the joints run on the 3 m grid)
            for (int i = 0; i < 12; i++)
            {
                bool south = i % 3 == 0;
                float x = south ? -40.0f + (float)rng.NextDouble() * 80.0f : (i % 2 == 0 ? 1 : -1) * (33.0f + (float)rng.NextDouble() * 8.0f);
                float z = south ? -58.0f + (float)rng.NextDouble() * 24.0f : -28.0f + (float)rng.NextDouble() * 80.0f;
                if (rng.NextDouble() < 0.5) x = Mathf.Round(x / 3.0f) * 3.0f; else z = Mathf.Round(z / 3.0f) * 3.0f;
                if (Tuft(joints, new Vector3(x, PlazaY, z), named, rng, 0.55f)) tufts++;
            }

            // the new fire bowls' night lights, as the art's lights are given to their fires
            var lamps = root.Find("NightLights") ?? Group(root, "NightLights");
            foreach (var f in flames) Lamp(lamps, f.transform.position + Vector3.up * 2.0f, 7.0f, new[] { f });

            return $"plaza: {bulul}; {vined} creepers, {planters} planters, {tufts} joint tufts, {_plazaPieces} pieces and {_plazaPlants} plants in all; " +
                   $"seated on the paving by rays: {_plazaMoved} moved to it, {_plazaDropped} dropped (not on open paving: {string.Join(", ", PlazaWhy.Select(w => w.Key + " x" + w.Value))})" +
                   (_plazaMissing > 0 ? $", {_plazaMissing} MISSING models" : "");
        }

        /// <summary>The art's bulul near the court, sized up on plinths and turned to the court; a new pair at the south corners.</summary>
        private static string PlazaBulul(Transform root, Transform plaza, Dictionary<string, Material> named, List<PayoFlame> flames)
        {
            var dressing = root.Find("Dressing");
            var guard = Group(plaza, "bulul");
            int raised = 0, added = 0;
            // (model, art x sign, its new place, statue scale, plinth scale)
            var plan = new List<(string model, Vector3 at, float scale, float plinth)>
            {
                ("bulul_seated", new Vector3(-37.0f, PlazaY, 6.0f), 1.4f, 1.2f), ("bulul_seated", new Vector3(37.0f, PlazaY, 6.0f), 1.4f, 1.2f),
                ("bulul_standing", new Vector3(-37.2f, PlazaY, 48.6f), 1.7f, 0.9f), ("bulul_standing", new Vector3(37.2f, PlazaY, 48.6f), 1.7f, 0.9f),
            };
            var used = new HashSet<Transform>();
            foreach (var p in plan)
            {
                var group = dressing != null ? dressing.Find(p.model) : null;
                Transform statue = null;
                if (group != null)
                    foreach (Transform t in group)
                    {
                        if (used.Contains(t)) continue;
                        Vector3 q = t.localPosition;
                        if (Mathf.Abs(q.x) > 50.0f || Mathf.Abs(q.z) > 70.0f || Mathf.Sign(q.x) != Mathf.Sign(p.at.x)) continue;
                        statue = t; break;
                    }
                if (statue == null) { Warn("plaza: no " + p.model + " of the art near " + p.at + ": placing one"); }
                Raise(statue, p.model, guard, p.at, p.scale, p.plinth, named, flames);
                if (statue != null) { used.Add(statue); raised++; } else added++;
            }
            // the new pair: the south corners, guarding the gate's side of the court
            foreach (float s in new[] { -1.0f, 1.0f })
            {
                Raise(null, "bulul_standing", guard, new Vector3(s * 37.0f, PlazaY, -54.0f), 1.7f, 0.9f, named, flames);
                added++;
            }
            return $"{raised} bulul of the art sized up on plinths, {added} added (seated 1.4, standing 1.7)";
        }

        /// <summary>A plinth at `at` with a bulul on top facing the court (the art's statue moved there, or a new one), and two
        /// fire bowls flanking it on the court's side.</summary>
        private static void Raise(Transform statue, string model, Transform parent, Vector3 at, float scale, float plinthScale, Dictionary<string, Material> named, List<PayoFlame> flames)
        {
            // facing the court: front = (sin yaw, cos yaw) toward the origin
            float yaw = Mathf.Atan2(-at.x, -at.z) * Mathf.Rad2Deg;
            if (model == "bulul_seated") yaw = at.x < 0 ? 90.0f : 270.0f;                // square to the stage, as the art had it (98.5, 261.5)
            // the plinth stays square to the court (its frieze runs with the walls'); the statue turns on it
            float square = Mathf.Round(yaw / 90.0f) * 90.0f;
            PlazaPut("plaza_plinth", parent, at, square, plinthScale, true, named);
            Vector3 top = at + Vector3.up * (3.8f * plinthScale);
            if (statue == null)
            {
                var made = PlazaPut(model, parent, top, yaw, scale, true, named);
                if (made == null) return;
            }
            else
            {
                statue.localPosition = top;
                statue.localRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
                statue.localScale = Vector3.one * scale;
            }
            // two fire bowls on the plinth's court side, either hand
            Vector3 front = new Vector3(Mathf.Sin(square * Mathf.Deg2Rad), 0.0f, Mathf.Cos(square * Mathf.Deg2Rad));
            Vector3 right = new Vector3(front.z, 0.0f, -front.x);
            float half = 4.9f * plinthScale;
            foreach (float s in new[] { -1.0f, 1.0f })
            {
                Vector3 bowl = at + front * (half - 0.9f) + right * s * (half + 1.1f);
                if (Mathf.Abs(bowl.x) < 31.6f) bowl.x = Mathf.Sign(bowl.x) * 31.6f;
                if (Mathf.Abs(bowl.x) > 42.0f) bowl.x = Mathf.Sign(bowl.x) * 42.0f;
                var go = PlazaPut("fire_bowl", parent, bowl, square, 1.0f, true, named);
                if (go != null && go.GetComponent<PayoFlame>() is PayoFlame f) flames.Add(f);
            }
        }

        private static bool Vine(string model, Transform parent, Vector3 at, float yaw, Dictionary<string, Material> named, System.Random rng)
        {
            float scale = 0.85f + (float)rng.NextDouble() * 0.35f;
            var go = PlazaPut(model, parent, at, yaw, scale, false, named);
            if (go == null) return false;
            // no shadow from a leaf curtain lying on its wall (it would only darken the stone in specks)
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return true;
        }

        private static readonly string[] BedPlants = { "plant_croton_0", "plant_bush_1", "plant_monstera_2", "plant_croton_2", "plant_tuft_1", "plant_bush_3", "plant_groundcover_1", "plant_tuft_2" };

        private static bool Planter(Transform parent, Vector3 at, float yaw, Dictionary<string, Material> named, System.Random rng)
        {
            var bed = PlazaPut("plaza_planter", parent, at, yaw, 1.0f, true, named);
            if (bed == null) return false;
            Vector3 along = new Vector3(Mathf.Cos(yaw * Mathf.Deg2Rad), 0.0f, -Mathf.Sin(yaw * Mathf.Deg2Rad));   // the trough's long axis
            Vector3 soil = at + Vector3.up * 0.6f;
            // a banana at one end, then a row of mixed plants
            float end = rng.NextDouble() < 0.5 ? -1.0f : 1.0f;
            PlazaPlant("payo_plant_broadleaf_0", parent, soil + along * end * 2.1f, (float)rng.NextDouble() * 360.0f, 0.95f + (float)rng.NextDouble() * 0.3f, named, false);
            for (float t = -2.3f; t <= 2.31f; t += 0.75f)
            {
                if (Mathf.Abs(t - end * 2.1f) < 0.7f) continue;
                string model = BedPlants[rng.Next(BedPlants.Length)];
                float scale = model.StartsWith("plant_tuft") || model.StartsWith("plant_groundcover") ? 0.7f : 0.9f + (float)rng.NextDouble() * 0.35f;
                PlazaPlant(model, parent, soil + along * t, (float)rng.NextDouble() * 360.0f, scale, named, false);
            }
            return true;
        }

        private static bool Tuft(Transform parent, Vector3 at, Dictionary<string, Material> named, System.Random rng, float size = 1.0f)
        {
            string model = rng.NextDouble() < 0.6 ? "cover_grass" : "cover_fern";
            float scale = (model == "cover_grass" ? 0.55f + (float)rng.NextDouble() * 0.45f : 0.45f + (float)rng.NextDouble() * 0.3f) * size;
            return PlazaPut(model, parent, at, (float)rng.NextDouble() * 360.0f, scale, true, named) != null;
        }

        /// <summary>A plant as `Dressing` makes one: not static, levels of detail, swaying near the court.</summary>
        private static GameObject PlazaPlant(string model, Transform parent, Vector3 at, float yaw, float scale, Dictionary<string, Material> named, bool tree)
        {
            var go = PlazaPut(model, parent, at, yaw, scale, false, named);
            if (go == null) return null;
            PlantLod(go, model, tree, named);
            _plazaPlants++;
            return go;
        }

        /// <summary>One of the art's models at a place under `parent` (the root's frame), wearing the map's materials, no
        /// collider, its flames made into a fire.</summary>
        private static GameObject PlazaPut(string model, Transform parent, Vector3 at, float yaw, float scale, bool still, Dictionary<string, Material> named)
        {
            var prefab = Model(model);
            if (prefab == null) { _plazaMissing++; Warn("plaza: missing model Models/" + model + ".glb"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
            go.transform.localScale = Vector3.one * scale;
            Rematerial(go, named);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            if (Mathf.Abs(at.y - PlazaY) < 0.01f && !PlazaSeat(go)) { Object.DestroyImmediate(go); _plazaDropped++; return null; }
            Finish(go, still);
            Fires(go);
            _plazaPieces++;
            return go;
        }
    }
}
