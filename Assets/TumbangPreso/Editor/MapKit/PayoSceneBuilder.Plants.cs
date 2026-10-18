using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TumbangPreso.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// PAYO'S TREES AND PLANTS AT A BUDGET (2026-10-18). The approved scene places 12,585 plants (3,981 trees, 8,590 flora);
    /// drawn whole they are 35 M triangles, and each swayed on the CPU every frame. Here every plant gets a `LODGroup`:
    ///  * LOD0 the model as authored, near;
    ///  * LOD1 `Models/MODEL_lod1.glb` where it exists (tools/payo_plant_lods.py: the same materials and UVs, a spread subset
    ///    of the leaf cards scaled up so the crown keeps its mass, the trunk decimated), mid and far;
    ///  * culled beyond: trees only past the far valley (so from the court the valley still reads as forest), flora by its size.
    /// The distances are worked out for a 60 degree vertical field of view at lodBias 1, as the blades' are.
    /// The materials are instanced (`BuildMaterial` sets it) and the plants are never static-batched, so one tree model
    /// at one LOD draws as a few instanced batches, not thousands of calls. Only plants within `SwayReach` of the court
    /// sway (`PayoLife`, CPU); every other plant is static (less batching) and costs no script time.
    /// </summary>
    public static partial class PayoSceneBuilder
    {
        /// <summary>Plants nearer the court than this sway; the rest stand still.</summary>
        private const float SwayReach = 120.0f;
        private const float LodReferenceFov = 60.0f;
        /// <summary>Trees: full model to this camera distance, LOD1 to the cull (past the valley's far side).</summary>
        private const float TreeFull = 70.0f, TreeCull = 2600.0f;
        /// <summary>Flora: full model to this distance; culled at this many times its size (clamped), LOD1 between where it has one.</summary>
        private const float FloraFull = 30.0f, FloraCullPerMetre = 60.0f, FloraCullMin = 70.0f, FloraCullMax = 170.0f;

        private static string _plantSaid = "";
        private static int _plantLod1, _plantSway, _plantNoLod1;
        private static readonly HashSet<string> NoLod1 = new HashSet<string>();

        /// <summary>
        /// THE RIVER RIBBON IS UPSIDE DOWN in the export (payo_river.glb: every face and normal points down, which Blender,
        /// drawing both sides, never showed; the land and the other water ribbons face up). Unity culls back faces, so from
        /// above the river was not drawn and its rock bed showed grey. Each mesh is copied once into Generated with its
        /// triangles' order and its normals turned over, and the placement wears the copy.
        /// </summary>
        private static void FaceUp(GameObject go)
        {
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>(true))
            {
                var source = filter.sharedMesh;
                if (source == null) continue;
                var mesh = Object.Instantiate(source);
                mesh.name = source.name + "_up";
                var normals = mesh.normals;
                float y = 0f;
                foreach (var n in normals) y += n.y;
                if (normals.Length == 0 || y >= 0f) { Object.DestroyImmediate(mesh); continue; }
                for (int i = 0; i < normals.Length; i++) normals[i] = -normals[i];
                mesh.normals = normals;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var tris = mesh.GetTriangles(sub);
                    for (int t = 0; t < tris.Length; t += 3) (tris[t + 1], tris[t + 2]) = (tris[t + 2], tris[t + 1]);
                    mesh.SetTriangles(tris, sub);
                }
                string path = $"{Generated}/{mesh.name}.asset";
                if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(mesh, path);
                filter.sharedMesh = mesh;
                _faceUp++;
            }
        }
        private static int _faceUp;

        private static void PlantLod(GameObject go, string model, bool tree, Dictionary<string, Material> named)
        {
            var lod0 = go.GetComponentsInChildren<Renderer>(true);
            Renderer[] lod1 = System.Array.Empty<Renderer>();
            var prefab = Model(model + "_lod1");
            if (prefab != null)
            {
                var low = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
                low.name = "LOD1";
                low.transform.localPosition = Vector3.zero; low.transform.localRotation = Quaternion.identity; low.transform.localScale = Vector3.one;
                Rematerial(low, named);
                foreach (var c in low.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                Finish(low, false);
                lod1 = low.GetComponentsInChildren<Renderer>(true);
                _plantLod1++;
            }
            else { _plantNoLod1++; NoLod1.Add(model); }

            var group = go.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.SetLODs(new[] { new LOD(0.5f, lod0), new LOD(0.01f, lod1) });
            group.RecalculateBounds();
            Vector3 s = go.transform.lossyScale;
            float size = Mathf.Max(group.size * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)), 0.1f);
            float tan = Mathf.Tan(LodReferenceFov * 0.5f * Mathf.Deg2Rad);
            float Height(float distance) => Mathf.Clamp(size / (2.0f * distance * tan), 0.0001f, 0.99f);
            float full = tree ? TreeFull : FloraFull;
            float cull = tree ? TreeCull : Mathf.Clamp(size * FloraCullPerMetre, FloraCullMin, FloraCullMax);
            if (lod1.Length > 0)
            {
                float h0 = Height(full), h1 = Mathf.Min(Height(cull), h0 * 0.99f);
                group.SetLODs(new[] { new LOD(h0, lod0), new LOD(h1, lod1) });
            }
            else group.SetLODs(new[] { new LOD(Height(cull), lod0) });
            foreach (var r in lod1) if (!tree) r.shadowCastingMode = ShadowCastingMode.Off;   // a far bush's shadow is under a pixel

            Vector3 at = go.transform.position;
            if (new Vector2(at.x, at.z).magnitude < SwayReach) { Plants.Add(go.transform); _plantSway++; }
            else
            {
                var flags = (StaticEditorFlags)~0 & ~StaticEditorFlags.BatchingStatic & ~StaticEditorFlags.ContributeGI;
                foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
            }
            _plantSaid = $"plants: {_plantLod1 + _plantNoLod1} with LODs ({_plantLod1} with a LOD1 model, {_plantNoLod1} full then culled" +
                         $"{(NoLod1.Count > 0 ? ": " + string.Join(", ", NoLod1) : "")}), {_plantSway} sway within {SwayReach:0} m, the rest static";
        }

        // ------------------------------------------------------------------ the far plants, instanced (2026-10-18)

        /// <summary>Plants farther than this from the court (on the flat) are not objects: PayoFlora draws them instanced.
        /// The camera stays within about 40 m of the court in play, so every instanced plant is past the ink's fade (95 m).</summary>
        internal const float NearPlants = 140.0f;
        /// <summary>Plants farther than this are instanced (2026-10-18, owner: "i genuinely get lower frames when looking at the
        /// city compared to the distance"): the town stands 60 to 200 m south, and its plants as objects (one renderer each,
        /// redrawn one by one in the ink's prepass) were the largest share of the town view's extra set-passes. They lose
        /// only the CPU sway (sway now within this) and the ink, which at this distance is mostly faded. `NearPlants` stays
        /// the line for the land and buildings leaving the ink's prepass, so the town's houses keep their ink.</summary>
        internal const float InstancedPlants = 80.0f;

        private static readonly List<(string model, bool tree, Vector3 at, float yaw, float scale)> FarPlants = new List<(string, bool, Vector3, float, float)>();

        /// <summary>True (and the plant listed for PayoFlora) when a plant placement stands past NearPlants.</summary>
        private static bool ListFar(JToken p, string model, bool tree)
        {
            var position = p["position"] as JArray;
            if (position == null || position.Count < 3 || p["scale3"] != null) return false;
            var at = new Vector3((float)position[0], (float)position[1], (float)position[2]);
            if (new Vector2(at.x, at.z).magnitude < InstancedPlants) return false;
            if (Model(model) == null) return false;
            FarPlants.Add((model, tree, at, Num(p["yaw"], 0.0f), Num(p["scale"], 1.0f)));
            return true;
        }

        /// <summary>The model's drawn parts (mesh, submesh, map material, place in the model) and its size.</summary>
        private static PayoFlora.Part[] Parts(GameObject prefab, bool shadows, Dictionary<string, Material> named, ref Bounds bounds, ref bool any)
        {
            var parts = new List<PayoFlora.Part>();
            if (prefab == null) return parts.ToArray();
            var toRoot = prefab.transform.worldToLocalMatrix;
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<Renderer>();
                var mesh = filter.sharedMesh;
                if (renderer == null || mesh == null) continue;
                var local = toRoot * filter.transform.localToWorldMatrix;
                var mats = renderer.sharedMaterials;
                for (int sub = 0; sub < mesh.subMeshCount && sub < mats.Length; sub++)
                {
                    if (mats[sub] == null) continue;
                    var m = MaterialOf(mats[sub].name.Replace(" (Instance)", "").Trim(), mats[sub], named) ?? mats[sub];
                    bool cast = shadows && !(m.name == "py_decal" || NoShadow.Contains(m.name)) && m.shader != null && m.shader.name != WaterShader && m.shader.name != FallShader;
                    if (!m.enableInstancing) { m.enableInstancing = true; EditorUtility.SetDirty(m); }
                    parts.Add(new PayoFlora.Part { Mesh = mesh, Submesh = sub, Material = m, Local = local, Shadows = cast });
                }
                var b = mesh.bounds;
                for (int c = 0; c < 8; c++)
                {
                    var corner = local.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1)));
                    if (!any) { bounds = new Bounds(corner, Vector3.zero); any = true; } else bounds.Encapsulate(corner);
                }
            }
            return parts.ToArray();
        }

        /// <summary>
        /// THE FAR LAND LEAVES THE INK'S PREPASS. `WorldOutline` builds its edges from a depth-normals prepass: the whole
        /// opaque scene drawn again through Unity's replacement shader, one draw per renderer, sorted by distance (so the
        /// instancing and batching of the main pass do not happen there). Its edges are gone past `InkFadeEnd` (95 m), so a
        /// land or terrace piece wholly past `NearPlants` from the court (the camera stays within about 40 m of it in play)
        /// gives the prepass nothing but cost. Such a piece wears a copy of its materials (Generated/Far, `_far`) whose
        /// `RenderType` is overridden to `PayoFar`, a type the replacement shader has no subshader for, so it is skipped
        /// there and drawn exactly as before everywhere else (main pass, shadows, the depth texture).
        /// 2026-10-18 (owner: "huge fps drops when looking at the main city"): not only the land, every opaque piece of
        /// the dressing wholly past `NearPlants` (the town's houses, walls, trees and props up the slope): looking at the
        /// town drew about 1,000 more set-passes than looking down the valley, half of them in this prepass.
        /// </summary>
        private static string InkClear(Transform root)
        {
            string folder = Generated + "/Far";
            EnsureFolder(folder);
            var copies = new Dictionary<Material, Material>();
            int pieces = 0;
            var holders = new List<Transform>();
            var dressing = root.Find("Dressing");
            if (dressing != null) foreach (Transform piece in dressing) holders.Add(piece);
            if (root.Find("Terraces") != null) holders.Add(root.Find("Terraces"));
            foreach (var holder in holders)
                foreach (var r in holder.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var b = r.bounds;
                    float x = Mathf.Max(0.0f, Mathf.Abs(b.center.x) - b.extents.x), z = Mathf.Max(0.0f, Mathf.Abs(b.center.z) - b.extents.z);
                    if (new Vector2(x, z).magnitude < NearPlants) continue;
                    var mats = r.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var m = mats[i];
                        if (m == null) continue;
                        string type = m.GetTag("RenderType", true, "");
                        if (type != "Opaque" && type != "TransparentCutout" && type != "") continue;
                        if (!copies.TryGetValue(m, out var far))
                        {
                            string path = $"{folder}/{m.name}_far.mat";
                            far = AssetDatabase.LoadAssetAtPath<Material>(path);
                            if (far == null) { far = new Material(m); AssetDatabase.CreateAsset(far, path); }
                            else { far.shader = m.shader; far.CopyPropertiesFromMaterial(m); }
                            far.name = m.name + "_far";
                            far.shaderKeywords = m.shaderKeywords;
                            far.renderQueue = m.renderQueue;
                            far.enableInstancing = m.enableInstancing;
                            far.SetOverrideTag("RenderType", PayoFlora.FarRenderType);
                            EditorUtility.SetDirty(far);
                            copies[m] = far;
                        }
                        mats[i] = far; changed = true;
                    }
                    if (changed) { r.sharedMaterials = mats; pieces++; }
                }
            return $"{pieces} far land, terrace and dressing renderers out of the ink's prepass ({copies.Count} far material copies)";
        }

        /// <summary>The far plants as one PayoFlora under the root. Returns what it made.</summary>
        private static string FarFlora(Transform root, Dictionary<string, Material> named)
        {
            if (FarPlants.Count == 0) return "no instanced plants";
            var holder = Group(root, "PayoFlora");
            var flora = holder.gameObject.AddComponent<PayoFlora>();
            var models = new List<PayoFlora.Model>();
            var index = new Dictionary<string, int>();
            var plants = new List<PayoFlora.Plant>(FarPlants.Count);
            foreach (var f in FarPlants)
            {
                if (!index.TryGetValue(f.model, out int m))
                {
                    var bounds = new Bounds(); bool any = false;
                    var lod0 = Parts(Model(f.model), true, named, ref bounds, ref any);
                    var low = Model(f.model + "_lod1");
                    var lod1 = low != null ? Parts(low, f.tree, named, ref bounds, ref any) : System.Array.Empty<PayoFlora.Part>();
                    float size = any ? Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) : 1.0f;
                    models.Add(new PayoFlora.Model { Name = f.model, Lod0 = lod0, Lod1 = lod1, Full = f.tree ? TreeFull : FloraFull, Size = Mathf.Max(size, 0.1f) });
                    index[f.model] = m = models.Count - 1;
                }
                float s = f.scale > 0.0f ? f.scale : 1.0f;
                float cull = f.tree ? TreeCull : Mathf.Clamp(models[m].Size * s * FloraCullPerMetre, FloraCullMin, FloraCullMax);
                plants.Add(new PayoFlora.Plant { Position = f.at, Yaw = f.yaw, Scale = s, Model = m, Cull = cull });
            }
            flora.Models = models.ToArray();
            flora.Plants = plants.ToArray();
            flora.Refresh();
            int trees = FarPlants.Count(f => f.tree);
            return $"{FarPlants.Count} far plants instanced by PayoFlora ({trees} trees, {FarPlants.Count - trees} flora, {models.Count} models, past {InstancedPlants:0} m from the court)";
        }
    }
}
