using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TumbangPreso.Map;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// PAYO'S WORLD OCCLUSION, BAKED PER VERTEX (owner, 2026-10-29: asked whether the world AO can be baked, then
    /// "continue" to a Payo bake test; the goal is the best frame rate on every graphics tier).
    ///
    /// WHY PER VERTEX AND NOT THE LIGHTMAPPER: none of Payo's surface shaders (LagoonPainted, PayoLand, LagoonRock,
    /// LagoonFoliage) is lightmap-aware in the way the look needs (the bright look multiplies the pixel by a violet
    /// cavity hue after lighting, not a grey into the indirect term), LagoonRock already spends UV1 on its own bake, and
    /// a lightmap on a map this size costs texture memory and a second texture read a pixel on the integrated GPUs it is
    /// meant to help. A vertex attribute is free to read and the four shaders already run a vertex function.
    ///
    /// HOW: every static vertex casts `Rays` cosine-weighted rays over its normal's hemisphere, `Radius` metres long
    /// (the screen pass's own radius times 1.5, since that pass reaches a metre in screen depth), against MeshColliders
    /// copied into a preview scene from every static opaque renderer near the court (the map itself is never given a
    /// collider). A hit nearer the vertex shades more. The light that reaches it goes to UV2.x, and UV2.y = 1 marks the
    /// vertex as baked (an unbaked mesh reads 0 there and is left alone). The shader term lives in
    /// Runtime/Map/PayoBakedAO.cs; with its switch off it multiplies by one.
    ///
    /// ⚠️ PER VERTEX MEANS AS FINE AS THE MESH: a big quad of terrace or wall gets its occlusion only at its corners, so
    /// a wall's foot on a coarse floor fades over the whole quad. Instanced meshes that are not static-batched are left
    /// out, so GPU instancing keeps working (the flora); a shared mesh is copied per renderer, so each copy can carry
    /// its own place's occlusion.
    /// </summary>
    public static partial class PayoSceneBuilder
    {
        /// <summary>Whether `build` bakes it. False until the owner approves (`aobake` in Temp/payo.request bakes the saved scene).</summary>
        private const bool BakeWorldAOOnBuild = true;   // every build bakes (owner, 2026-10-30: "turn it on by default")
        private const string BakedAOPath = Root + "/Generated/PayoBakedAO.asset";
        private const int AoRays = 24;
        private const float AoRadius = 1.5f, AoReach = 320f, AoGain = 1.6f;

        private static readonly HashSet<string> AoShaders = new HashSet<string>
            { "TumbangPreso/LagoonPainted", "TumbangPreso/PayoLand", "TumbangPreso/LagoonRock", "TumbangPreso/LagoonFoliage" };

        /// <summary>The builder's one-line hook: bakes when `BakeWorldAOOnBuild` is on, a fresh build's meshes being the importer's own.</summary>
        private static string BakedAOStep(Transform root)
        {
            if (!BakeWorldAOOnBuild) return "baked AO: off (BakeWorldAOOnBuild)";
            return BakeWorldAO(root, true);
        }

        /// <summary>`aobake`: bakes the SAVED scene in place and saves it (the request watcher reopens what was open).</summary>
        internal static string BakeSavedScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == MapName);
            if (root == null) throw new InvalidOperationException("no '" + MapName + "' root in " + ScenePath);
            string said = BakeWorldAO(root.transform, false);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + ScenePath);
            AssetDatabase.SaveAssets();
            return said;
        }

        private static bool IsStaticOpaque(Renderer r)
        {
            if (GameObjectUtility.GetStaticEditorFlags(r.gameObject) == 0) return false;
            var mats = r.sharedMaterials;
            return mats.Length > 0 && mats.All(m => m != null && m.renderQueue <= 2450 && m.shader != null && m.shader.name != PayoBlades.ShaderName);
        }

        private static bool Near(Bounds b) => new Vector2(b.center.x, b.center.z).magnitude - b.extents.magnitude < AoReach;

        internal static string BakeWorldAO(Transform root, bool fresh)
        {
            var clock = Stopwatch.StartNew();
            var filters = root.GetComponentsInChildren<MeshFilter>(true)
                .Where(f => f.sharedMesh != null && f.TryGetComponent(out MeshRenderer r) && IsStaticOpaque(r) && Near(r.bounds)).ToList();
            // Occluders: every static opaque mesh near the court, whatever its shader.
            var physics = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (var f in filters)
                {
                    if (!f.sharedMesh.isReadable) continue;
                    var go = new GameObject("o");
                    SceneManager.MoveGameObjectToScene(go, physics);
                    go.transform.SetPositionAndRotation(f.transform.position, f.transform.rotation);
                    go.transform.localScale = f.transform.lossyScale;
                    go.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh;
                }
                var space = physics.GetPhysicsScene();

                // Targets: the four painted shaders; instanced, unbatched shared meshes stay out.
                var users = filters.GroupBy(f => f.sharedMesh).ToDictionary(g => g.Key, g => g.Count());
                Object container = AssetDatabase.LoadMainAssetAtPath(BakedAOPath);
                if (fresh && container != null) { AssetDatabase.DeleteAsset(BakedAOPath); container = null; }
                if (container == null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(BakedAOPath));
                    container = new Mesh { name = "PayoBakedAO" };   // an empty mesh as the container: the copies are its sub-assets
                    AssetDatabase.CreateAsset(container, BakedAOPath);
                }
                int baked = 0, copies = 0, skippedInstanced = 0, skippedUv2 = 0, skippedUnreadable = 0;
                long vertices = 0, rays = 0, hits = 0;
                double shade = 0;
                var dirs = HemisphereDirections(AoRays);
                foreach (var f in filters)
                {
                    var r = f.GetComponent<MeshRenderer>();
                    if (!r.sharedMaterials.Any(m => m != null && AoShaders.Contains(m.shader.name))) continue;
                    var mesh = f.sharedMesh;
                    bool batched = GameObjectUtility.GetStaticEditorFlags(f.gameObject).HasFlag(StaticEditorFlags.BatchingStatic);
                    if (users[mesh] > 1 && !batched && r.sharedMaterials.Any(m => m.enableInstancing)) { skippedInstanced++; continue; }
                    if (!mesh.isReadable) { skippedUnreadable++; continue; }
                    if (mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord2)) { skippedUv2++; continue; }
                    // ⚠️ A STREAM, NOT A COPY (2026-10-30: GitHub refused the push, the 455 mesh copies made this asset 244 MB
                    // against its 100 MB limit). Each renderer gets a mesh holding ONLY UV2 (4 bytes a vertex: x the light that
                    // reaches it, y 1 for baked) as its `additionalVertexStreams`; the map keeps its own meshes.
                    var ao = BakeMesh(space, f.transform, mesh, dirs, ref rays, ref hits);
                    var data = new byte[ao.Length * 4];
                    for (int v = 0; v < ao.Length; v++) { data[v * 4] = (byte)Mathf.RoundToInt(Mathf.Clamp01(ao[v]) * 255f); data[v * 4 + 1] = 255; shade += 1 - ao[v]; }
                    var stream = new Mesh { name = f.name + "_ao" };
                    stream.SetVertexBufferParams(ao.Length, new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.TexCoord2, UnityEngine.Rendering.VertexAttributeFormat.UNorm8, 4));
                    stream.SetVertexBufferData(data, 0, 0, data.Length);
                    stream.UploadMeshData(false);
                    AssetDatabase.AddObjectToAsset(stream, container);
                    r.additionalVertexStreams = stream;
                    copies++;
                    vertices += ao.Length; baked++;
                }
                EditorUtility.SetDirty(container);
                var store = root.GetComponent<PayoBakedAO>();
                if (store == null) root.gameObject.AddComponent<PayoBakedAO>();
                string said = $"baked AO: {baked} meshes, {vertices} vertices ({copies} UV2 streams in {BakedAOPath}), {rays} rays, {hits} hits, mean shade {(vertices > 0 ? shade / vertices : 0):0.000}; " +
                              $"left out: {skippedInstanced} instanced, {skippedUv2} with their own UV2, {skippedUnreadable} unreadable; {clock.Elapsed.TotalSeconds:0.0} s";
                Debug.Log(Tag + said);
                return said;
            }
            finally { EditorSceneManager.ClosePreviewScene(physics); }
        }

        private static Vector3[] HemisphereDirections(int n)
        {
            // Cosine-weighted, golden-angle spiral round +Z (the vertex's normal once turned into place).
            var d = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float u = (i + .5f) / n, phi = i * 2.39996323f, s = Mathf.Sqrt(u);
                d[i] = new Vector3(s * Mathf.Cos(phi), s * Mathf.Sin(phi), Mathf.Sqrt(1 - u));
            }
            return d;
        }

        private static float[] BakeMesh(PhysicsScene space, Transform t, Mesh mesh, Vector3[] dirs, ref long rays, ref long hits)
        {
            var verts = mesh.vertices; var normals = mesh.normals;
            int n = verts.Length, k = dirs.Length;
            var ao = new float[n];
            if (normals.Length != n) { for (int i = 0; i < n; i++) ao[i] = 1; return ao; }
            var m = t.localToWorldMatrix; var nm = m.inverse.transpose;
            const int Chunk = 8192;
            var query = new QueryParameters(~0, false, QueryTriggerInteraction.Ignore, false);
            for (int start = 0; start < n; start += Chunk)
            {
                int count = Mathf.Min(Chunk, n - start);
                var commands = new NativeArray<RaycastCommand>(count * k, Allocator.TempJob);
                var results = new NativeArray<RaycastHit>(count * k, Allocator.TempJob);
                try
                {
                    for (int v = 0; v < count; v++)
                    {
                        Vector3 p = m.MultiplyPoint3x4(verts[start + v]);
                        Vector3 nn = nm.MultiplyVector(normals[start + v]).normalized;
                        if (nn == Vector3.zero) nn = Vector3.up;
                        // A per-vertex turn of the spiral, so neighbours do not share a pattern.
                        var turn = Quaternion.AngleAxis((start + v) * 137.508f, nn) * Quaternion.FromToRotation(Vector3.forward, nn);
                        Vector3 from = p + nn * .02f;
                        for (int j = 0; j < k; j++) commands[v * k + j] = new RaycastCommand(space, from, turn * dirs[j], query, AoRadius);
                    }
                    RaycastCommand.ScheduleBatch(commands, results, 256, 1).Complete();
                    for (int v = 0; v < count; v++)
                    {
                        float occ = 0;
                        for (int j = 0; j < k; j++)
                        {
                            var h = results[v * k + j];
                            if (h.normal == Vector3.zero) continue;   // a miss leaves the hit empty
                            float near = 1 - h.distance / AoRadius;
                            occ += near * near; hits++;
                        }
                        ao[start + v] = 1 - Mathf.Clamp01(occ / k * AoGain);
                    }
                    rays += (long)count * k;
                }
                finally { commands.Dispose(); results.Dispose(); }
            }
            return ao;
        }
    }
}
