using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// WORLD OCCLUSION ON THE OTHER MAPS (owner, 2026-10-31: "go ahead with 1 and 3", bake the world AO on the other maps
    /// the way Payo does). `survey` in `Temp/worldao.request` opens each map's saved scene additively and reports, per
    /// shader, what a bake would meet, because their builders and shaders differ from Payo's. The answer is
    /// `Logs/worldao/survey.txt` and `Temp/worldao.done`. Nothing is saved.
    /// </summary>
    [InitializeOnLoad]
    internal static class WorldAOBake
    {
        private const string Request = "Temp/worldao.request", Done = "Temp/worldao.done";
        private static readonly string[] Maps = { "Eskinita", "IlalimNgTulay", "LagoonCove", "Kanto", "Arena" };

        static WorldAOBake() => EditorApplication.update += Watch;

        private static void Watch()
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (File.Exists("Temp/cutscene-film.lock") && !File.ReadAllText("Temp/cutscene-film.lock").StartsWith("kanto worldao")) return;
            string verb = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            string said;
            try { said = verb == "survey" ? Survey() : "unknown verb " + verb; }
            catch (System.Exception e) { said = "FAIL " + e; }
            File.WriteAllText(Done, said + "\n");
        }

        private static string Survey()
        {
            Directory.CreateDirectory("Logs/worldao");
            var report = new StringBuilder();
            foreach (string map in Maps)
            {
                var scene = EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/" + map + ".unity", OpenSceneMode.Additive);
                try
                {
                    var rows = new Dictionary<string, int[]>();
                    var meshes = new HashSet<Mesh>();
                    long verts = 0;
                    foreach (var f in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshFilter>(true)))
                    {
                        if (f.sharedMesh == null || !f.TryGetComponent(out MeshRenderer r)) continue;
                        var flags = GameObjectUtility.GetStaticEditorFlags(f.gameObject);
                        foreach (var m in r.sharedMaterials.Where(m => m != null && m.shader != null))
                        {
                            // renderers, static, batching static, readable, has UV2, instancing, far-from-opaque, vertices
                            if (!rows.TryGetValue(m.shader.name, out var row)) rows[m.shader.name] = row = new int[8];
                            row[0]++;
                            if (flags != 0) row[1]++;
                            if (flags.HasFlag(StaticEditorFlags.BatchingStatic)) row[2]++;
                            if (f.sharedMesh.isReadable) row[3]++;
                            if (f.sharedMesh.HasVertexAttribute(VertexAttribute.TexCoord2)) row[4]++;
                            if (m.enableInstancing) row[5]++;
                            if (m.renderQueue > 2450) row[6]++;
                            row[7] += f.sharedMesh.vertexCount;
                        }
                        if (meshes.Add(f.sharedMesh)) verts += f.sharedMesh.vertexCount;
                    }
                    int skinned = scene.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
                    report.AppendLine($"== {map}: {meshes.Count} unique meshes, {verts} unique vertices, {skinned} skinned");
                    report.AppendLine("shader | renderers static batched readable uv2 instancing transparent vertices");
                    foreach (var kv in rows.OrderByDescending(x => x.Value[7]))
                        report.AppendLine(kv.Key + " | " + string.Join(" ", kv.Value));
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            File.WriteAllText("Logs/worldao/survey.txt", report.ToString());
            return "OK Logs/worldao/survey.txt";
        }
    }
}
