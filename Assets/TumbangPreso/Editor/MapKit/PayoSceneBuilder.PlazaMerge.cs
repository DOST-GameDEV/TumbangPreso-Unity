using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE PLAZA'S DRESSING, MERGED (2026-10-29). Owner: "go ahead and merge the plaza dressing" (integrated GPUs are the
    /// target; measured before: about 630 set-passes and 1.3 ms in the town view, because the ink's mask pass draws every
    /// renderer on its own and the foliage was kept out of static batching).
    ///
    /// Every plain mesh renderer under Plaza is merged into one mesh per (area chunk, material, shadow mode), saved under
    /// Generated as the market's merge does. The chunks are the plaza's sides (west, east, south, north) so culling still
    /// works. Left as they are: anything under a LODGroup (the swaying, LOD'd plants), a fire (`PayoFlame`), a light, any
    /// script, a collider, an Animator, a lantern or a night glow material, and any material whose shader reads the object's
    /// transform other than LagoonFoliage (LagoonRock's triplanar scale).
    ///
    /// LagoonFoliage reads the object's world position for its per-plant colour nudge. The merged mesh carries each piece's
    /// own world position in UV channel 3 (xyz, w = 1) and the shader uses it when w is 1, so every leaf keeps its colour.
    /// </summary>
    public static partial class PayoSceneBuilder
    {
        private static readonly Dictionary<Shader, bool> ReadsTransformCache = new Dictionary<Shader, bool>();

        private static bool ShaderReadsTransform(Shader s)
        {
            if (s == null) return true;
            if (ReadsTransformCache.TryGetValue(s, out bool v)) return v;
            v = false;
            string path = AssetDatabase.GetAssetPath(s);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".shader"))
            {
                try { string src = File.ReadAllText(path); v = src.Contains("unity_ObjectToWorld") || src.Contains("unity_WorldToObject") || src.Contains("UNITY_MATRIX_M"); }
                catch (IOException) { v = true; }
            }
            return ReadsTransformCache[s] = v;
        }

        private static string PlazaChunk(Vector3 p) => p.x < -30.0f ? "west" : p.x > 30.0f ? "east" : p.z < -30.0f ? "south" : "north";

        private static string PlazaMerge(Transform root)
        {
            var plaza = root.Find("Plaza");
            if (plaza == null) return "plaza merge: no Plaza group";
            var glowNames = new HashSet<string>(Glows) { "py_window", "py_inlay" };
            var buckets = new Dictionary<(string chunk, Material mat, ShadowCastingMode shadow, bool receive, int layer), List<(Mesh mesh, int sub, Matrix4x4 m, Vector3 origin, bool live)>>();
            var done = new List<MeshRenderer>();
            var why = new Dictionary<string, int>();
            void Keep(string w) => why[w] = why.TryGetValue(w, out int n) ? n + 1 : 1;
            Matrix4x4 toGroup = plaza.worldToLocalMatrix;
            foreach (var r in plaza.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                if (mf.sharedMesh.name.StartsWith("payo_")) { Keep("already merged"); continue; }
                if (r.GetComponentInParent<LODGroup>(true) != null) { Keep("LODGroup"); continue; }
                if (r.GetComponentInParent<PayoFlame>(true) != null) { Keep("fire"); continue; }
                if (r.GetComponentInParent<Animator>(true) != null || r.GetComponentInParent<Animation>(true) != null) { Keep("animated"); continue; }
                if (r.GetComponentInChildren<Light>(true) != null || r.GetComponentInChildren<Collider>(true) != null) { Keep("light or collider"); continue; }
                bool script = false;
                for (var t = r.transform; t != null && t != plaza; t = t.parent)
                    if (t.GetComponents<MonoBehaviour>().Any(b => b != null) || Plants.Contains(t)) { script = true; break; }
                if (script) { Keep("script or sway"); continue; }
                if (Lanterns.Contains(r)) { Keep("lantern"); continue; }
                var mats = r.sharedMaterials;
                if (mats.Length != mf.sharedMesh.subMeshCount || mats.Any(m => m == null)) { Keep("material count"); continue; }
                if (mats.Any(m => glowNames.Contains(m.name))) { Keep("night glow"); continue; }
                if (mats.Any(m => m.shader.name != FoliageShader && ShaderReadsTransform(m.shader))) { Keep("shader reads transform"); continue; }
                Vector3 origin = r.transform.position;
                // A bulul, a plinth or a small dense prop keeps the live crease AO on the baked map (owner, 2026-10-30: "no ao
                // on the creases of the statue face"): its vertices carry UV3.x = 1 in the chunk (PayoSceneBuilder.BakedAO.cs).
                bool live = IsLiveAOPiece(r);
                for (int i = 0; i < mats.Length; i++)
                {
                    var key = (PlazaChunk(r.bounds.center), mats[i], r.shadowCastingMode, r.receiveShadows, r.gameObject.layer);
                    if (!buckets.TryGetValue(key, out var list)) buckets[key] = list = new List<(Mesh, int, Matrix4x4, Vector3, bool)>();
                    list.Add((mf.sharedMesh, i, toGroup * r.transform.localToWorldMatrix, origin, live));
                }
                done.Add(r);
            }

            int made = 0, tris = 0;
            var names = new HashSet<string>();
            foreach (var kv in buckets)
            {
                var (chunk, mat, shadow, receive, layer) = kv.Key;
                bool foliage = mat.shader.name == FoliageShader;
                var P = new List<Vector3>(); var N = new List<Vector3>(); var T = new List<Vector4>(); var U = new List<Vector2>(); var C = new List<Color>(); var O = new List<Vector4>(); var I = new List<int>();
                bool colours = kv.Value.Any(e => e.mesh.colors.Length > 0);
                bool flagged = !foliage && kv.Value.Any(e => e.live);
                foreach (var (mesh, sub, m, origin, live) in kv.Value)
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
                        if (foliage) O.Add(new Vector4(origin.x, origin.y, origin.z, 1.0f));
                        else if (flagged) O.Add(new Vector4(live ? 1.0f : 0.0f, 0.0f, 0.0f, 0.0f));
                    }
                    var idx = mesh.GetTriangles(sub);
                    for (int k = 0; k < idx.Length; k += 3)
                    {
                        I.Add(b + idx[k]);
                        if (flip) { I.Add(b + idx[k + 2]); I.Add(b + idx[k + 1]); }
                        else { I.Add(b + idx[k + 1]); I.Add(b + idx[k + 2]); }
                    }
                }
                string name = $"payo_plaza_{chunk}_{mat.name}" + (shadow == ShadowCastingMode.Off ? "_noshadow" : "");
                while (!names.Add(name)) name += "_b";
                var merged = new Mesh { name = name };
                merged.indexFormat = P.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                merged.SetVertices(P); merged.SetNormals(N); merged.SetTangents(T); merged.SetUVs(0, U);
                if (colours) merged.SetColors(C);
                if (foliage || flagged) merged.SetUVs(3, O);
                merged.SetTriangles(I, 0);
                merged.RecalculateBounds();
                string path = $"{Generated}/{name}.asset";
                var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (old != null) { EditorUtility.CopySerialized(merged, old); merged = old; }
                else AssetDatabase.CreateAsset(merged, path);
                var go = new GameObject($"plaza merged {chunk} {mat.name}");
                go.layer = layer;
                go.transform.SetParent(plaza, false);
                go.AddComponent<MeshFilter>().sharedMesh = merged;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                Finish(go, true);
                // the pieces' own shadow modes, as they were placed (a vine casts none; Finish would give it one)
                mr.shadowCastingMode = shadow; mr.receiveShadows = receive;
                made++; tris += I.Count / 3;
            }
            foreach (var r in done)
            {
                if (r == null) continue;
                var outer = PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);
                if (outer != null) PrefabUtility.UnpackPrefabInstance(outer, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            foreach (var r in done)
            {
                if (r == null) continue;
                var go = r.gameObject;
                Object.DestroyImmediate(r.GetComponent<MeshFilter>());
                Object.DestroyImmediate(r);
                for (var t = go.transform; t != null && t != plaza && t.childCount == 0 && t.GetComponents<Component>().Length == 1;)
                {
                    var up = t.parent; Object.DestroyImmediate(t.gameObject); t = up;
                }
            }
            return $"plaza merge: {done.Count} renderers into {made} meshes ({tris} triangles); kept: {string.Join(", ", why.Select(w => w.Key + " x" + w.Value))}";
        }
    }
}
