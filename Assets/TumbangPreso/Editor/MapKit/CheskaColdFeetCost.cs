using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// What COLD FEET costs at each stage of its life, MEASURED on this machine's GPU: renders it from the caster's eyes in a preview scene, waits
    /// for the GPU each time by reading a pixel back, and takes away the same render with the flakes switched off. Also the CPU time of `Pose`, the
    /// only thing it does per frame. A film proves the look only. Request: `Temp/coldfeet-cost.request` holding a tag, out of Play.
    /// Answers `Logs/paete-ability-film/done_coldfeetcost_tag.txt`.
    /// </summary>
    [InitializeOnLoad]
    internal static class CheskaColdFeetCost
    {
        private const string Request = "Temp/coldfeet-cost.request";
        static CheskaColdFeetCost() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            string tag = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            try { File.WriteAllText("Logs/paete-ability-film/done_coldfeetcost_" + tag + ".txt", Measure()); }
            catch (System.Exception e) { File.WriteAllText("Logs/paete-ability-film/failed_coldfeetcost_" + tag + ".txt", e.ToString()); }
        }

        private static string Measure()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var holder = new GameObject("ColdFeetCost");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holder, scene);
            var look = Visual.CheskaColdFeetLook.Build(holder.transform, Core.CryoRules.ColdFeetRadius, -1, true);
            var eye = new GameObject("Eye");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(eye, scene);
            var cam = eye.AddComponent<Camera>();
            cam.scene = scene; cam.enabled = false; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.5f, 0.45f, 0.4f, 1f);
            cam.fieldOfView = 65f;
            eye.transform.position = new Vector3(0f, 1.25f, -3.5f);
            eye.transform.LookAt(new Vector3(0f, 0.1f, 0f));
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            var read = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            float Clock(int n)
            {
                var w = new Stopwatch();
                for (int i = 0; i < n; i++)
                {
                    w.Start();
                    cam.Render();
                    RenderTexture.active = rt;
                    read.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
                    RenderTexture.active = null;
                    w.Stop();
                }
                return (float)w.Elapsed.TotalMilliseconds / n;
            }
            float life = Core.CryoRules.ColdFeetSeconds;
            var report = new System.Text.StringBuilder("1920x1080, caster's eyes (the biggest it gets on screen), this machine's GPU through the editor; each figure is the render with the flakes on less the render with them off, 400 renders each, 3 passes\n");
            foreach (var r in look.Parts) r.enabled = false;
            Clock(100);
            var stages = new (string name, float at)[] { ("growing, 0.12 s", 0.12f), ("formed, 1.0 s", 1f), ("melting, last 0.3 s", life - 0.3f), ("melting, last 0.1 s", life - 0.1f) };
            foreach (var s in stages)
            {
                float sum = 0f;
                for (int pass = 0; pass < 3; pass++)
                {
                    foreach (var r in look.Parts) r.enabled = false;
                    float off = Clock(400);
                    foreach (var r in look.Parts) r.enabled = true;
                    look.Pose(s.at, life);
                    sum += Clock(400) - off;
                }
                report.AppendLine("  " + s.name + ": " + (sum / 3f).ToString("0.000") + " ms");
            }
            var p = Stopwatch.StartNew();
            for (int i = 0; i < 20000; i++) look.Pose(i * 0.0001f, life);
            report.AppendLine("  Pose, CPU: " + (p.Elapsed.TotalMilliseconds / 20000.0).ToString("0.00000") + " ms a call");
            report.AppendLine("  " + Visual.CheskaColdFeetLook.BakeReport);
            cam.targetTexture = null;
            Object.DestroyImmediate(read); Object.DestroyImmediate(rt);
            EditorSceneManager.ClosePreviewScene(scene);
            return report.ToString();
        }
    }
}
