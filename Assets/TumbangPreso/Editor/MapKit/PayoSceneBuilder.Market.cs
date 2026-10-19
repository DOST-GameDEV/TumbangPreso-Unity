using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TumbangPreso.Map;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE SOUTH TERRACE'S MARKET AND GALLERY (2026-10-19). Owner, of the wide paved terrace between the pit's south lip
    /// (z -30) and the tall south wall (z -60.8), with the two carabao statues in its middle: "this central area of the
    /// basin wall needs some work too. its still barren, with the same sparsity of elements as the side walls, but it
    /// works for the sidewalls because its already pretty small in area. this one is large in area so it needs something
    /// more. maybe like some audience watching, or a marketplace area."
    ///
    /// Everything already there stays (the approved basin dressing, `PayoSceneBuilder.Plaza.cs`, and the art's carabao,
    /// torches, gong, offering table, banners and trees). Added round them:
    ///  * THE GALLERY along the lip, facing the court: stone viewing steps (`market_gallery`, the court's own stone and
    ///    atlas, 0.30 and 0.60 m tiers) in the bays between the planters, and a crowd on them and behind them: sitters on
    ///    the front tier, standing, cheering and clapping people on the back tier and on the paving behind.
    ///  * THE MARKET in the band behind it (z -42 to -53): open stalls along the south side of an east-west lane, thatch
    ///    (`market_stall_thatch`, the town's hut roof) and woven-mat lean-tos (`market_stall_mat`), fronts to the court;
    ///    ground vendors' mats on the lane's north side; a cloth line either side of the middle; a cooking hearth with
    ///    stools; goods (rice sheaves, bananas, pots, baskets of areca, betel and tubers, folded blankets, the art's jars,
    ///    baskets and winnowing trays). Two clusters, west and east, gaps between them, the middle (|x| under 4.5, the
    ///    gate-to-carabao-to-court line) left open.
    /// All the market pieces are one material (`py_market`, one painted atlas: tools/payo_pieces/market_kit.py and
    /// market_paint.py), static and batched; the crowd is one material (`payo_audience`), static and batched, no script,
    /// no light. This terrace is OUTSIDE the play walls (|x|, |z| 22) and nothing here has a collider: no walk path, jump
    /// route or bot route meets it.
    ///
    /// ⚠️ THE CROWD'S MODELS are the six of ArtSource/payo/audience/ (copied to Art/Payo/Models, never commit ArtSource),
    /// named by pose in `CrowdModels`: change a list there to swap or add figures. They are the cast's size in model units
    /// and take the cast's scale (`CharacterVisual.PersonScale`); standing pivots between the feet, sitting pivots at the
    /// underside of the hips (`SeatBack` behind a ledge's edge, the feet hanging 0.12 model units under it).
    ///
    /// ⚠️ FIRE: no light is added here. Left open for the fire pedestals and torches (PayoSceneBuilder.Fire.cs): the lane's
    /// two ends (x +-31, z -46.5), either side of the middle line on the lane (x +-4.5, z -46.5), and the gallery's outer
    /// ends (x +-29.2, z -34.0). See `FireSpots`.
    /// Hooked from `PlazaBody` (one line); seated on the paving by `PlazaPut` like the rest of the basin.
    /// </summary>
    public static partial class PayoSceneBuilder
    {
        /// <summary>The market pieces were modelled facing Blender +y, the art's other way round: a half turn puts their
        /// fronts where a placement's yaw says.</summary>
        private const float MarketTurn = 180.0f;
        /// <summary>The crowd at the cast's own scale (adults 0.72 model units, 1.71 m).</summary>
        private const float CrowdScale = CharacterVisual.PersonScale;
        /// <summary>A sitter's pivot this far behind the ledge's edge, metres (0.1 model units).</summary>
        private const float SeatBack = 0.03f;

        internal enum CrowdPose { Stand, Cheer, Clap, Sit }

        /// <summary>THE CROWD'S MODELS BY POSE (Art/Payo/Models/NAME.glb): swap or add figures here and rebuild.</summary>
        // declared before CrowdModels: static fields initialise in order
        private static readonly string[] CrowdIds = { "elder_man", "woman", "young_man", "elder_woman", "boy", "girl" };
        internal static readonly Dictionary<CrowdPose, string[]> CrowdModels = new Dictionary<CrowdPose, string[]>
        {
            // audience v2 (2026-10-19, the cast's pipeline, baked static poses, 5,000 to 5,900 triangles each)
            [CrowdPose.Stand] = CrowdIds.Select(id => "audience_v2_" + id + "_stand").ToArray(),
            [CrowdPose.Cheer] = CrowdIds.Select(id => "audience_v2_" + id + "_cheer").ToArray(),
            [CrowdPose.Clap] = CrowdIds.Select(id => "audience_v2_" + id + "_clap").ToArray(),
            [CrowdPose.Sit] = CrowdIds.Select(id => "audience_v2_" + id + "_sit").ToArray(),
        };
        /// <summary>COST CAP (owner fighting frame drops; each v2 figure is about 5,500 triangles): at most this many
        /// figures, taken evenly from the slots; each culled past about 70 m by a LODGroup.</summary>
        private const int CrowdCap = 28;
        private const float CrowdCullScreen = 0.018f;

        /// <summary>Kept open for the fire pedestals and torches (the market places nothing within 1.2 m of them).</summary>
        internal static readonly Vector3[] FireSpots =
        {
            new Vector3(-31.0f, PlazaY, -46.5f), new Vector3(31.0f, PlazaY, -46.5f),
            new Vector3(-4.5f, PlazaY, -46.5f), new Vector3(4.5f, PlazaY, -46.5f),
            new Vector3(-29.2f, PlazaY, -34.0f), new Vector3(29.2f, PlazaY, -34.0f),
        };

        internal struct CrowdSlot { public Vector3 At; public float Yaw; public CrowdPose Pose; public string Where; }

        private static int _marketPieces, _marketDropped, _marketMissing;
        private static readonly List<CrowdSlot> CrowdSlots = new List<CrowdSlot>();

        private static string Market(Transform plaza, Dictionary<string, Material> named)
        {
            _marketPieces = _marketDropped = _marketMissing = 0;
            CrowdSlots.Clear();
            // the two new materials, whatever the art's materials file says (it is rewritten by tools/payo_export_unity.py)
            if (!Specs.ContainsKey("py_market"))
                Specs["py_market"] = new JObject { ["name"] = "py_market", ["kind"] = "painted", ["texture"] = "market", ["vertex_colour"] = false };
            foreach (var id in CrowdIds)
                if (!Specs.ContainsKey("audience_v2_" + id))
                    Specs["audience_v2_" + id] = new JObject { ["name"] = "audience_v2_" + id, ["kind"] = "painted", ["texture"] = "", ["vertex_colour"] = false };
            if (!Specs.ContainsKey("payo_audience"))
                Specs["payo_audience"] = new JObject { ["name"] = "payo_audience", ["kind"] = "painted", ["texture"] = "", ["vertex_colour"] = false };
            var rng = new System.Random(20261019);
            var market = Group(plaza, "market");
            var crowd = Group(plaza, "audience");

            // ---- THE GALLERY: viewing steps in the bays between the lip's planters (|x| 6.5 to 12.5 and 16 to 22 are planted)
            var galleries = new List<GameObject>();
            foreach (var (x, z) in new[] { (-3.1f, -33.3f), (3.1f, -33.3f), (-25.3f, -33.6f), (25.3f, -33.6f) })
            {
                var g = MarketPut("market_gallery", market, new Vector3(x, PlazaY, z), 0.0f, 1.0f, named);
                if (g != null) galleries.Add(g);
            }
            foreach (var g in galleries) GallerySlots(g.transform.localPosition, rng);
            // standing at the rail in the narrow bays between the planters
            foreach (float s in new[] { -1.0f, 1.0f })
                foreach (float x in new[] { 13.1f, 14.0f, 15.2f })
                    CrowdAdd(new Vector3(s * x + (float)(rng.NextDouble() - 0.5) * 0.2f, PlazaY, -32.5f - (float)rng.NextDouble() * 0.4f),
                            rng.NextDouble() < 0.5 ? CrowdPose.Cheer : CrowdPose.Stand, "rail", rng);

            // ---- THE MARKET: stalls on the lane's south side, fronts to the court (yaw about 0)
            var stalls = new List<GameObject>();
            foreach (var (model, x, z, yaw) in new[]
            {
                ("market_stall_thatch", -25.0f, -50.6f, 6.0f), ("market_stall_mat", -19.2f, -50.0f, -4.0f), ("market_stall_thatch", -8.8f, -51.2f, 3.0f),
                ("market_stall_mat", 8.6f, -50.2f, 5.0f), ("market_stall_thatch", 19.4f, -50.8f, -3.0f), ("market_stall_mat", 26.0f, -49.7f, -8.0f),
                // and one at each far end of the lane's north side, turned in toward the lane and the court
                ("market_stall_thatch", -31.6f, -42.6f, 35.0f), ("market_stall_thatch", 31.4f, -42.4f, -32.0f),
            })
            {
                var st = MarketPut(model, market, new Vector3(x, PlazaY, z), yaw, 1.0f, named);
                if (st == null) continue;
                stalls.Add(st);
                bool thatch = model == "market_stall_thatch";
                if (thatch)
                {
                    // the counter's goods, the back shelf's, and the vendor behind the counter
                    MarketOn(st, rng.NextDouble() < 0.5 ? "market_bananas" : "market_baskets", -0.7f, 0.68f, 0.70f, 0.75f, market, named);
                    MarketOn(st, rng.NextDouble() < 0.5 ? "market_pots" : "market_cloth_stack", 0.7f, 0.68f, 0.70f, 0.7f, market, named);
                    MarketOn(st, "market_pots", -0.7f, -0.8f, 0.45f, 0.6f, market, named);
                    MarketOn(st, "market_rice", 0.6f, -0.85f, 0.45f, 0.55f, market, named);
                    MarketVendor(st, 0.2f, -0.25f, 0.0f, CrowdPose.Stand, rng);
                }
                else
                {
                    MarketOn(st, "market_cloth_stack", -0.75f, 0.3f, 0.024f, 0.9f, market, named);
                    MarketOn(st, rng.NextDouble() < 0.5 ? "market_pots" : "market_baskets", 0.75f, 0.35f, 0.024f, 0.85f, market, named);
                    MarketOn(st, "market_bananas", 0.05f, 0.55f, 0.024f, 0.9f, market, named);
                    // the vendor on the back bench (its top 0.39 m, its front edge 0.57 m behind the middle)
                    MarketVendor(st, -0.4f, -0.57f - SeatBack, 0.39f, CrowdPose.Sit, rng);
                }
            }
            // ground vendors on the lane's north side: a mat, its goods, the seller behind it facing the lane
            foreach (var (x, z, yaw, a, b, c) in new[]
            {
                (-22.5f, -43.6f, 4.0f, "market_rice", "bilao_rice", "market_baskets"),
                (-10.8f, -43.4f, -5.0f, "market_pots", "tapuy_jar_small", "market_bananas"),
                (10.6f, -43.6f, 3.0f, "market_baskets", "market_cloth_stack", "bilao_rice"),
                (23.5f, -43.2f, -6.0f, "market_bananas", "market_pots", "market_rice"),
            })
            {
                var m = MarketPut("market_mat", market, new Vector3(x, PlazaY, z), yaw, 1.0f, named);
                if (m == null) continue;
                MarketOn(m, a, -0.55f, 0.2f, 0.024f, 0.9f, market, named);
                MarketOn(m, b, 0.2f, -0.25f, 0.024f, 0.9f, market, named);
                MarketOn(m, c, 0.6f, 0.25f, 0.024f, 0.85f, market, named);
                var seller = new Vector3(x + (float)(rng.NextDouble() - 0.5) * 0.6f, PlazaY, z + 1.05f);
                if (MarketOpen(seller)) CrowdAdd(seller, rng.NextDouble() < 0.5 ? CrowdPose.Clap : CrowdPose.Stand, "market", rng, 180.0f + yaw);
            }
            // a cloth line either side of the middle, in front of the trees
            MarketPut("market_cloth_line", market, new Vector3(-14.2f, PlazaY, -47.0f), 8.0f, 1.0f, named);
            MarketPut("market_cloth_line", market, new Vector3(14.0f, PlazaY, -47.4f), -6.0f, 1.0f, named);
            // the cooking corner: the art's hearth with its pot (its flame burns at night, no light), stools round it
            Vector3 fire = new Vector3(18.6f, PlazaY, -45.9f);
            var hearth = MarketPut("hearth_pot", market, fire, 20.0f, 1.0f, named);
            if (hearth != null)
                foreach (float a in new[] { 40.0f, 150.0f, 260.0f })
                {
                    var p = fire + new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 1.15f, 0.0f, Mathf.Cos(a * Mathf.Deg2Rad) * 1.15f);
                    var stool = MarketPut("market_stool", market, p, a, 1.0f, named);
                    if (stool == null || a == 150.0f) continue;
                    // a sitter on the stool (its top 0.36 m), facing the fire
                    float face = a + 180.0f;
                    CrowdAdd(p + Vector3.up * 0.36f - Quaternion.Euler(0, face, 0) * Vector3.forward * 0.06f, CrowdPose.Sit, "market", rng, face, 6.0f);
                }
            // benches for the tired, and sitters on some
            foreach (var (x, z, yaw, sit) in new[] { (-25.2f, -47.1f, 0.0f, true), (8.4f, -47.3f, 180.0f, false), (-30.0f, -42.8f, 90.0f, true) })
            {
                var bench = MarketPut("market_bench", market, new Vector3(x, PlazaY, z), yaw, 1.0f, named);
                if (bench == null || !sit) continue;
                CrowdAdd(new Vector3(x, PlazaY + 0.44f, z) - Quaternion.Euler(0, yaw, 0) * Vector3.forward * 0.04f, CrowdPose.Sit, "market", rng, yaw, 8.0f);
            }
            // the art's own baskets and jars between the stalls, and shoppers in the lane
            foreach (var (model, x, z, yaw) in new[]
            {
                ("basket_stack", -27.9f, -48.4f, 20.0f), ("tapuy_jar_large", -16.6f, -48.6f, 0.0f), ("tapuy_jar_small", -16.0f, -48.1f, 40.0f),
                ("pasiking", -5.9f, -49.6f, 200.0f), ("market_rice", 5.9f, -49.0f, 70.0f), ("basket_stack", 11.3f, -48.6f, 150.0f),
                ("tapuy_jar_large", 22.4f, -48.3f, 10.0f), ("market_baskets", 28.6f, -47.2f, 300.0f), ("bilao_rice", -12.0f, -49.6f, 30.0f),
            })
                MarketPut(model, market, new Vector3(x, PlazaY, z), yaw, 1.0f, named);
            foreach (var (x, z, yaw, pose) in new[]
            {
                (-24.3f, -47.9f, 175.0f, CrowdPose.Stand), (-19.8f, -47.6f, 190.0f, CrowdPose.Clap), (-9.6f, -48.4f, 170.0f, CrowdPose.Stand),
                (-11.4f, -45.0f, 20.0f, CrowdPose.Clap), (9.3f, -47.7f, 185.0f, CrowdPose.Stand), (19.9f, -48.0f, 160.0f, CrowdPose.Clap),
                (22.8f, -45.1f, 10.0f, CrowdPose.Stand),
            })
            {
                var p = new Vector3(x, PlazaY, z);
                if (MarketOpen(p)) CrowdAdd(p, pose, "market", rng, yaw, 10.0f);
            }

            // ---- the crowd, as the slots say (made before the merge below)
            int made = 0;
            var byModel = new Dictionary<string, int>();
            // the cap: an even pick through the slots (gallery first in the list, then rail, then market)
            var picked = new List<CrowdSlot>();
            float step = Mathf.Max(1.0f, CrowdSlots.Count / (float)CrowdCap);
            for (float f = 0.0f; f < CrowdSlots.Count && picked.Count < CrowdCap; f += step) picked.Add(CrowdSlots[(int)f]);
            foreach (var s in picked)
            {
                var list = CrowdModels[s.Pose];
                string model = list[rng.Next(list.Length)];
                if (CrowdFigure(model, crowd, s, rng.NextDouble() < 0.4, named) == null) continue;
                made++;
                byModel[model] = byModel.TryGetValue(model, out int n) ? n + 1 : 1;
            }
            // ---- ONE MESH PER MATERIAL: the market's pieces and the crowd merged (measured 2026-10-19 before the merge: 141
            // renderers added about 135 set-pass calls, because the ink's mask pass draws every renderer on its own).
            string merged = MarketMerge(market, "market") + ", " + "crowd kept as single static figures with a LODGroup each";
            string said = $"market: {stalls.Count} stalls, {galleries.Count} gallery steps, {_marketPieces} market pieces in all ({_marketDropped} dropped, not on open paving), " +
                          $"{made} crowd figures (cap {CrowdCap}) of {CrowdSlots.Count} slots ({CrowdSlots.Count(s => s.Where == "gallery")} gallery, {CrowdSlots.Count(s => s.Where == "rail")} rail, {CrowdSlots.Count(s => s.Where == "market")} market; " +
                          $"{string.Join(", ", byModel.Select(kv => kv.Value + " " + kv.Key))})" + (_marketMissing > 0 ? $", {_marketMissing} MISSING models" : "") + "; merged: " + merged;
            try
            {
                Directory.CreateDirectory("Logs/payo/unity/market");
                File.WriteAllText("Logs/payo/unity/market/build.txt", said + "\n");
                var json = new JArray(CrowdSlots.Select(s => new JObject
                {
                    ["pose"] = s.Pose.ToString().ToLowerInvariant(), ["where"] = s.Where, ["yaw"] = System.Math.Round(s.Yaw, 1),
                    ["position"] = new JArray(System.Math.Round(s.At.x, 3), System.Math.Round(s.At.y, 3), System.Math.Round(s.At.z, 3)),
                }));
                File.WriteAllText("Logs/payo/unity/market/audience_slots.json", json.ToString());
            }
            catch (IOException) { }
            return said;
        }

        /// <summary>
        /// Every mesh renderer under `group` merged into one static mesh per material (saved under Generated), the
        /// originals removed. A mirrored figure (negative scale) has its triangles turned back. A piece with a fire
        /// (`PayoFlame`, the hearth) is left as it is: its flame is lit and stretched by the night.
        /// </summary>
        private static string MarketMerge(Transform group, string tag)
        {
            var byMat = new Dictionary<Material, List<(Mesh mesh, int sub, Matrix4x4 m)>>();
            var done = new List<MeshRenderer>();
            Matrix4x4 toGroup = group.worldToLocalMatrix;
            foreach (var r in group.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || r.GetComponentInParent<PayoFlame>(true) != null) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length && i < mf.sharedMesh.subMeshCount; i++)
                {
                    if (mats[i] == null) continue;
                    if (!byMat.TryGetValue(mats[i], out var list)) byMat[mats[i]] = list = new List<(Mesh, int, Matrix4x4)>();
                    list.Add((mf.sharedMesh, i, toGroup * r.transform.localToWorldMatrix));
                }
                done.Add(r);
            }
            int made = 0, tris = 0;
            foreach (var kv in byMat)
            {
                var P = new List<Vector3>(); var N = new List<Vector3>(); var T = new List<Vector4>(); var U = new List<Vector2>(); var C = new List<Color>(); var I = new List<int>();
                bool colours = kv.Value.Any(e => e.mesh.colors.Length > 0);
                foreach (var (mesh, sub, m) in kv.Value)
                {
                    var p = mesh.vertices; var n = mesh.normals; var t = mesh.tangents; var u = mesh.uv; var c = mesh.colors;
                    int b = P.Count;
                    bool flip = m.determinant < 0.0f;
                    var nm = m.inverse.transpose;
                    for (int v = 0; v < p.Length; v++)
                    {
                        P.Add(m.MultiplyPoint3x4(p[v]));
                        N.Add(n.Length > v ? nm.MultiplyVector(n[v]).normalized : Vector3.up);
                        if (t.Length > v) { var d = m.MultiplyVector(new Vector3(t[v].x, t[v].y, t[v].z)).normalized; T.Add(new Vector4(d.x, d.y, d.z, flip ? -t[v].w : t[v].w)); }
                        else T.Add(new Vector4(1, 0, 0, 1));
                        U.Add(u.Length > v ? u[v] : Vector2.zero);
                        if (colours) C.Add(c.Length > v ? c[v] : Color.white);
                    }
                    var idx = mesh.GetTriangles(sub);
                    for (int k = 0; k < idx.Length; k += 3)
                    {
                        I.Add(b + idx[k]);
                        if (flip) { I.Add(b + idx[k + 2]); I.Add(b + idx[k + 1]); }
                        else { I.Add(b + idx[k + 1]); I.Add(b + idx[k + 2]); }
                    }
                }
                var merged = new Mesh { name = $"payo_{tag}_{kv.Key.name}" };
                merged.indexFormat = P.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
                merged.SetVertices(P); merged.SetNormals(N); merged.SetTangents(T); merged.SetUVs(0, U);
                if (colours) merged.SetColors(C);
                merged.SetTriangles(I, 0);
                merged.RecalculateBounds();
                string path = $"{Generated}/payo_{tag}_{kv.Key.name}.asset";
                var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (old != null) { EditorUtility.CopySerialized(merged, old); merged = old; }
                else AssetDatabase.CreateAsset(merged, path);
                var go = new GameObject($"{tag} merged {kv.Key.name}");
                go.transform.SetParent(group, false);
                go.AddComponent<MeshFilter>().sharedMesh = merged;
                go.AddComponent<MeshRenderer>().sharedMaterial = kv.Key;
                Finish(go, true);
                made++; tris += I.Count / 3;
            }
            foreach (var r in done)
            {
                var outer = PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);
                if (outer != null) PrefabUtility.UnpackPrefabInstance(outer, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            foreach (var r in done)
            {
                var go = r.gameObject;
                Object.DestroyImmediate(r.GetComponent<MeshFilter>());
                Object.DestroyImmediate(r);
                // the emptied piece roots and nodes go too
                for (var t = go.transform; t != null && t != group && t.childCount == 0 && t.GetComponents<Component>().Length == 1;)
                {
                    var up = t.parent; Object.DestroyImmediate(t.gameObject); t = up;
                }
            }
            return $"{tag} {done.Count} renderers into {made} ({tris} triangles)";
        }

        /// <summary>The crowd slots of one gallery step (its middle at `c`, its front tier toward the court, +z):
        /// sitters on the front tier's edge, standing people on the back tier and on the paving behind it.</summary>
        private static void GallerySlots(Vector3 c, System.Random rng)
        {
            for (float x = -2.6f; x <= 2.61f; x += 0.95f)
            {
                if (rng.NextDouble() < 0.25) continue;
                CrowdAdd(new Vector3(c.x + x + MarketJitter(rng, 0.15f), PlazaY + 0.30f, c.z + 0.9f - SeatBack), CrowdPose.Sit, "gallery", rng);
            }
            for (float x = -2.7f; x <= 2.71f; x += 0.8f)
            {
                if (rng.NextDouble() < 0.15) continue;
                CrowdAdd(new Vector3(c.x + x + MarketJitter(rng, 0.12f), PlazaY + 0.60f, c.z - 0.45f + MarketJitter(rng, 0.1f)), CrowdStanding(rng), "gallery", rng);
            }
            for (float x = -2.5f; x <= 2.51f; x += 1.0f)
            {
                if (rng.NextDouble() < 0.35) continue;
                var p = new Vector3(c.x + x + MarketJitter(rng, 0.2f), PlazaY, c.z - 1.4f + MarketJitter(rng, 0.15f));
                if (MarketOpen(p)) CrowdAdd(p, CrowdStanding(rng), "gallery", rng);
            }
        }

        private static float MarketJitter(System.Random rng, float r) => (float)(rng.NextDouble() - 0.5) * 2.0f * r;

        private static CrowdPose CrowdStanding(System.Random rng)
        {
            double r = rng.NextDouble();
            return r < 0.4 ? CrowdPose.Cheer : r < 0.7 ? CrowdPose.Clap : CrowdPose.Stand;
        }

        /// <summary>A slot facing `yaw` (by default the court's middle), turned a little at random (+-`spread` degrees).</summary>
        private static void CrowdAdd(Vector3 at, CrowdPose pose, string where, System.Random rng, float? yaw = null, float spread = 15.0f)
        {
            float face = yaw ?? Mathf.Atan2(-at.x, -at.z) * Mathf.Rad2Deg;
            CrowdSlots.Add(new CrowdSlot { At = at, Yaw = face + MarketJitter(rng, spread), Pose = pose, Where = where });
        }

        /// <summary>A crowd figure: static (batched with the rest), the cast's scale, mirrored when asked, no collider.</summary>
        private static GameObject CrowdFigure(string model, Transform parent, CrowdSlot s, bool mirror, Dictionary<string, Material> named)
        {
            var prefab = Model(model);
            if (prefab == null) { _marketMissing++; Warn("market: missing crowd model Models/" + model + ".glb"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = model + " " + s.Pose.ToString().ToLowerInvariant();
            go.transform.localPosition = s.At;
            go.transform.localRotation = Quaternion.Euler(0.0f, s.Yaw, 0.0f);
            go.transform.localScale = new Vector3(mirror ? -CrowdScale : CrowdScale, CrowdScale, CrowdScale);
            Rematerial(go, named);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            // static but NOT static-batched, so the GPU instances repeats; culled past about 70 m
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, (StaticEditorFlags)~0 & ~StaticEditorFlags.BatchingStatic);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials) if (m != null && !m.enableInstancing) { m.enableInstancing = true; EditorUtility.SetDirty(m); }
            var lod = go.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(CrowdCullScreen, go.GetComponentsInChildren<Renderer>(true)) });
            lod.RecalculateBounds();
            return go;
        }

        /// <summary>A market piece on the paving (seated like the basin's pieces), its front turned to `yaw`.</summary>
        private static GameObject MarketPut(string model, Transform parent, Vector3 at, float yaw, float scale, Dictionary<string, Material> named)
        {
            foreach (var f in FireSpots)
                if (new Vector2(f.x - at.x, f.z - at.z).sqrMagnitude < 1.44f) { Warn("market: " + model + " skipped, on a fire spot"); return null; }
            int dropped = _plazaDropped, missing = _plazaMissing;
            bool ours = model.StartsWith("market_");
            var go = PlazaPut(model, parent, at, ours ? yaw + MarketTurn : yaw, scale, true, named);
            if (go != null) _marketPieces++;
            else if (_plazaDropped > dropped) _marketDropped++;
            else if (_plazaMissing > missing) _marketMissing++;
            return go;
        }

        /// <summary>Goods set ON a piece (a counter, a shelf, a mat) at a point given in the piece's own Blender metres
        /// (x right, y front, z up): not seated (they are not on the paving), static.</summary>
        private static GameObject MarketOn(GameObject on, string model, float bx, float by, float bz, float scale, Transform parent, Dictionary<string, Material> named)
        {
            var p = MarketPoint(on, bx, by, bz);
            float yaw = on.transform.localEulerAngles.y + (float)((bx * 37.0f + by * 53.0f) % 40.0f);
            var go = PlazaPut(model, parent, p, yaw, scale, true, named);
            if (go != null) _marketPieces++;
            return go;
        }

        /// <summary>A point of a market piece in its Blender metres, in the root's frame (Blender x, y, z to glTF to Unity is
        /// (-x, z, -y)).</summary>
        private static Vector3 MarketPoint(GameObject piece, float bx, float by, float bz) =>
            piece.transform.parent.InverseTransformPoint(piece.transform.TransformPoint(new Vector3(-bx, bz, -by)));

        /// <summary>A vendor at a stall's own point, facing the stall's front.</summary>
        private static void MarketVendor(GameObject stall, float bx, float by, float bz, CrowdPose pose, System.Random rng)
        {
            var dir = stall.transform.TransformDirection(Vector3.back);   // Blender +y, the front
            CrowdAdd(MarketPoint(stall, bx, by, bz), pose, "market", rng, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 8.0f);
        }

        /// <summary>True when a ray from above meets the open paving at `p` (no seated piece there).</summary>
        private static bool MarketOpen(Vector3 p)
        {
            if (PlazaGround.Count == 0) return true;
            foreach (var f in FireSpots)
                if (new Vector2(f.x - p.x, f.z - p.z).sqrMagnitude < 1.0f) return false;
            if (!Physics.Raycast(new Vector3(p.x, PlazaY + 40.0f, p.z), Vector3.down, out var hit, 80.0f, ~0, QueryTriggerInteraction.Ignore)) return false;
            return PlazaGround.Contains(hit.collider) && Mathf.Abs(hit.point.y - PlazaY) < 0.12f;
        }
    }
}
