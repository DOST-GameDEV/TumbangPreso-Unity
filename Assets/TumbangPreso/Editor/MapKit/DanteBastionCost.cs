using System;
using System.IO;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// WHAT BASTION COSTS, MEASURED (`FxStudyDanteBastion`). A request file `Temp/dante-bastion-cost.request` makes one copy of
    /// the bench effect in a preview scene (the owner's open scene is not touched) and reports, at the moments that matter:
    /// renderers on, draw calls (renderers times passes; nothing assumed to batch) and triangles, for the WHOLE bench and
    /// for the WALL ALONE (the slabs and the field, which is what a match would draw besides pooled dust); what posing a
    /// frame costs (300 calls) and allocates; and what making the wall costs, cold (its shared mesh built) and warm.
    /// Editor, outside Play: a guide to the size of things, not what a phone will do.
    /// </summary>
    [InitializeOnLoad]
    internal static class DanteBastionCost
    {
        private const string Request = "Temp/dante-bastion-cost.request", Out = "Logs/paete-ability-film/dantebastion_cost.txt";
        private static double _next;
        static DanteBastionCost() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            string what = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            if (what.StartsWith("boulder")) { Boulder(what); return; }
            // `b4` measures BASTION b4 (its field as filmed); anything else the b3 sample.
            var look = what == "b4" ? FxStudyDanteBastion.B4 : FxStudyDanteBastion.Look.B3;
            var report = new System.Text.StringBuilder();
            Scene scene = default;
            try
            {
                scene = EditorSceneManager.NewPreviewScene();
                var holder = new GameObject("~BastionCost");
                SceneManager.MoveGameObjectToScene(holder, scene);
                var cold = System.Diagnostics.Stopwatch.StartNew();
                var fx = new FxStudyDanteBastion(holder.transform, look);
                double made = cold.Elapsed.TotalMilliseconds;
                var warm = System.Diagnostics.Stopwatch.StartNew();
                var second = new FxStudyDanteBastion(holder.transform, look);
                double madeWarm = warm.Elapsed.TotalMilliseconds;
                second.Dispose();
                report.AppendLine("making the bench effect: " + made.ToString("0.00") + " ms the first time in this editor session's domain (or since its shared mesh was last built), " + madeWarm.ToString("0.00") + " ms again");
                foreach (float age in new[] { 0.60f, 1.20f, 2.50f, 5.00f, 7.00f, 8.00f })
                {
                    fx.Sample(age, new Vector3(5f, 2f, -3f));
                    Count(fx.Root, false, out int r, out int c, out int t);
                    Count(fx.Root, true, out int wr, out int wc, out int wt);
                    long before = GC.GetAllocatedBytesForCurrentThread();
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    for (int n = 0; n < 300; n++) fx.Sample(age, new Vector3(5f, 2f, -3f));
                    double pose = clock.Elapsed.TotalMilliseconds / 300.0;
                    long bytes = (GC.GetAllocatedBytesForCurrentThread() - before) / 300;
                    report.AppendLine(age.ToString("0.00") + " s: bench " + r + " renderers, " + c + " draw calls, " + t + " triangles | the wall alone " + wr + " renderers, " + wc + " draw calls, " + wt + " triangles | posing the bench " + pose.ToString("0.0000") + " ms a frame, " + bytes + " bytes a frame");
                }
                fx.Dispose();
            }
            catch (Exception failure) { report.AppendLine("FAILED: " + failure); }
            finally { if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene); }
            Directory.CreateDirectory(Path.GetDirectoryName(Out));
            File.WriteAllText(look == FxStudyDanteBastion.Look.B3 ? Out : Out.Replace(".txt", "_b4.txt"), report.ToString());
        }

        /// <summary>
        /// THE BOULDER'S MOMENTS, MEASURED AS A MATCH MAKES THEM (`DanteBoulderMoment`): the cast's copy and the hit's copy of the
        /// approved effect, each made ten times (the first cold), then played frame by frame through its life at 60 a second
        /// with every frame timed, and what it allocates. Written to `Logs/paete-ability-film/danteboulder_cost_<tag>.txt`.
        /// </summary>
        private static void Boulder(string what)
        {
            string tag = what.Length > 8 ? what.Substring(8).Trim() : "now";
            var report = new System.Text.StringBuilder();
            Scene scene = default;
            try
            {
                scene = EditorSceneManager.NewPreviewScene();
                var holder = new GameObject("~BoulderCost");
                SceneManager.MoveGameObjectToScene(holder, scene);
                var eye = new Vector3(-3f, 1.6f, -3f);
                // Made and freed as a match does it: through the pool where the effect has one (found by name, so this
                // measures the effect before the pool existed as well), otherwise made new and disposed.
                var type = typeof(FxStudyDanteBoulder);
                var forMoment = type.GetMethod("ForMoment");
                var release = type.GetMethod("Release");
                FxStudyDanteBoulder Make(Transform parent, int part) => forMoment != null
                    ? (FxStudyDanteBoulder)forMoment.Invoke(null, new object[] { parent, new Vector3(0f, 0.6f, 0f), new Vector3(4.2f, 0.6f, 0f), 1f, part })
                    : new FxStudyDanteBoulder(parent, new Vector3(0f, 0.6f, 0f), new Vector3(4.2f, 0.6f, 0f), false, 1f, 0.6f, part);
                void Free(FxStudyDanteBoulder fx) { if (release != null) release.Invoke(null, new object[] { fx }); else fx.Dispose(); }
                var spare = type.GetField("SpareHolder");
                if (spare != null) spare.SetValue(null, holder.transform);
                report.AppendLine(forMoment != null ? "through the pool (`ForMoment`, `Release`)" : "made new and disposed each time (no pool in this build)");
                foreach (var (name, part, from, until) in new[] { ("cast", FxStudyDanteBoulder.PartCast, FxStudyDanteBoulder.CastMoment - 0.08f, 2.15f), ("hit", FxStudyDanteBoulder.PartHit, FxStudyDanteBoulder.HitMoment, 5.4f) })
                {
                    var made = new double[10];
                    for (int n = 0; n < 10; n++)
                    {
                        var clock = System.Diagnostics.Stopwatch.StartNew();
                        var fx = Make(holder.transform, part);
                        fx.Sample(from, eye);
                        made[n] = clock.Elapsed.TotalMilliseconds;
                        if (n < 9) Free(fx);
                        else
                        {
                            double total = 0, worst = 0; int frames = 0;
                            long before = GC.GetAllocatedBytesForCurrentThread();
                            for (float age = from; age < until; age += 1f / 60f)
                            {
                                var frame = System.Diagnostics.Stopwatch.StartNew();
                                fx.Sample(age, eye);
                                double ms = frame.Elapsed.TotalMilliseconds; total += ms; worst = Math.Max(worst, ms); frames++;
                            }
                            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                            report.AppendLine(name + ": made in " + made[0].ToString("0.000") + " ms cold, then " + Avg(made, 1, 9).ToString("0.000") + " ms on average (casts 2 to 9); playing it, " + frames + " frames: "
                                + (total / frames).ToString("0.0000") + " ms a frame, worst " + worst.ToString("0.000") + " ms, " + (bytes / Math.Max(1, frames)) + " bytes a frame");
                            Free(fx);
                        }
                    }
                }
            }
            catch (Exception failure) { report.AppendLine("FAILED: " + failure); }
            finally
            {
                var spare = typeof(FxStudyDanteBoulder).GetField("SpareHolder");
                if (spare != null) spare.SetValue(null, null);
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            }
            File.WriteAllText("Logs/paete-ability-film/danteboulder_cost_" + tag + ".txt", report.ToString());
        }

        private static double Avg(double[] values, int from, int to) { double s = 0; for (int i = from; i <= to; i++) s += values[i]; return s / (to - from + 1); }

        private static void Count(Transform root, bool wallOnly, out int renderers, out int calls, out int triangles)
        {
            renderers = calls = triangles = 0;
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (!renderer.enabled || renderer.sharedMaterial == null) continue;
                if (wallOnly && renderer.name != "slabs" && renderer.name != "field") continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                int passes = Mathf.Max(1, renderer.sharedMaterial.passCount), own = 0;
                for (int s = 0; s < filter.sharedMesh.subMeshCount; s++) own += (int)(filter.sharedMesh.GetIndexCount(s) / 3);
                renderers++; calls += passes; triangles += own * passes;
            }
        }
    }
}
