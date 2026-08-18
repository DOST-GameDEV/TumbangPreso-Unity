using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// What the road is actually made of, and what colour it actually is.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE `[Env] repainted 418 of 494` IS NOT AN ANSWER TO "did the road
    /// get its tint". The pass reports one aggregate count, and a road that falls into none of
    /// its groups is silently part of the 76 it skipped: the number looks healthy either way.
    /// The same class of silent miss is recorded twice already in `EnvColourPass` (the roof
    /// atlas that never swapped, the property block that wrote to nothing), so the third time
    /// it gets a probe rather than another guess.
    ///
    /// It prints, per top-level group, how many renderers it holds and what the largest one's
    /// albedo is, so "which node is the street" and "what did the pass do to it" are both
    /// answered from the scene rather than from the layer-name table.
    ///
    ///     Unity.exe -batchmode -quit -executeMethod TumbangPreso.EditorTools.GroundProbe.Run
    /// </summary>
    public static class GroundProbe
    {
        private const string Out = "Logs/ground-probe.txt";

        private static readonly string[] Colours =
        {
            "_BaseColor", "_Color", "baseColorFactor", "_TintColor",
        };

        public static void Run()
        {
            var report = new StringBuilder();

            report.AppendLine($"colour space: {QualitySettings.activeColorSpace}");

            foreach (string scene in new[]
            {
                "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity",
            })
            {
                EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);

                report.AppendLine();
                report.AppendLine($"== {Path.GetFileNameWithoutExtension(scene)}");
                report.AppendLine($"   ambient  {RenderSettings.ambientLight}");
                report.AppendLine($"   fog      {RenderSettings.fog} {RenderSettings.fogColor} " +
                                  $"{RenderSettings.fogStartDistance}..{RenderSettings.fogEndDistance}");

                foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                    report.AppendLine($"   light    {light.type} {light.color} " +
                                      $"intensity {light.intensity} shadow {light.shadowStrength}");

                // Every top-level node under the arena root, with what it holds. The group
                // table in EnvColourPass is a list of these names, so a name that is not
                // printed here is a row of that table matching nothing.
                foreach (var root in UnityEngine.SceneManagement.SceneManager
                                     .GetActiveScene().GetRootGameObjects())
                {
                    Walk(root.transform, report, 0);
                }
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText(Out, report.ToString());
            Debug.Log(report.ToString());
        }

        private static void Walk(Transform t, StringBuilder report, int depth)
        {
            var own = t.GetComponentsInChildren<Renderer>(true);
            if (own.Length == 0) return;

            // Two levels is the whole shape the dressing has: group, then instance.
            if (depth <= 1)
            {
                report.AppendLine($"{new string(' ', 3 + depth * 3)}{t.name}  " +
                                  $"({own.Length} renderers){Biggest(own)}");
            }

            if (depth >= 1) return;

            foreach (Transform child in t) Walk(child, report, depth + 1);
        }

        private static string Biggest(IReadOnlyList<Renderer> renderers)
        {
            Renderer best = null;
            float area = 0.0f;

            foreach (var r in renderers)
            {
                var size = r.bounds.size;
                float a = size.x * size.z;

                if (a <= area) continue;
                area = a;
                best = r;
            }

            if (best == null) return "";

            var material = best.sharedMaterial;
            if (material == null) return "  [no material]";

            foreach (string name in Colours)
            {
                if (!material.HasProperty(name)) continue;

                Color stored = material.GetColor(name);

                return $"  biggest '{best.name}' {area:0} m2  {name} stored {Fmt(stored)}" +
                       $" srgb {Fmt(stored.gamma)}  shader {material.shader.name}";
            }

            return $"  biggest '{best.name}' {area:0} m2  no colour property" +
                   $"  shader {material.shader.name}";
        }

        private static string Fmt(Color c) => $"({c.r:0.000},{c.g:0.000},{c.b:0.000})";
    }
}
