using System;
using System.IO;
using TumbangPreso.Visual;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// WHAT EACH EFFECT IN A MATCH COSTS, MEASURED, NOT ESTIMATED (`cloudcourt audit<tag>`). The owner, of the cloud: "the
    /// cloud is really laggy", and then of all of it: "remember this needs to be heavily optimized". The cloud turned out
    /// to cost 76 ms a frame on the processor and nobody had timed it, because a film is not real time. So every effect
    /// a match draws is built here the way the match builds it and posed frame after frame with a stopwatch round the
    /// posing alone, and the report gives: what one costs to MAKE (a cast), what a frame of it costs to POSE, how much
    /// managed memory the posing asked for, and how many draw calls and triangles it is at its busiest moment.
    ///
    /// ⚠️ It reads and runs those effects and changes nothing in them. It runs outside Play, in the editor, so the times
    /// are the editor's (slower than a built player) and they are the processor's share only: what the card then does
    /// with the triangles is not in them. Draw calls are counted as passes of the drawn pieces, not read from a frame.
    /// </summary>
    public static partial class PaeteAbilityFilm
    {
        /// <summary>
        /// THE FIRST CAST OF A SESSION, MEASURED (`cloudcourt audit` with `first` in the tag). Run straight after the editor
        /// has reloaded its code, so nothing of these effects has run yet. With `warm` in the tag it first runs the loading
        /// screen's warm-up (`HeroFxPrewarm`, timed: that is what it adds to loading), then times the first cast of each;
        /// without, it times the first casts cold. Then a second cast of each, for what a cast costs once warm. And the
        /// landing puffs' draw: how many triangles one live puff, eight, and none are drawn with.
        /// </summary>
        private static void RunFirstCast(string tag, string donePath, string failedPath)
        {
            var report = new System.Text.StringBuilder();
            var host = new GameObject("~FirstCast") { hideFlags = HideFlags.HideAndDontSave };
            GameObject lens = null;
            try
            {
                bool warm = tag.IndexOf("warm", StringComparison.OrdinalIgnoreCase) >= 0;
                Vector3 eye = new Vector3(0f, 1.6f, -7f);
                if (warm)
                {
                    lens = new GameObject("~FirstCastCamera") { hideFlags = HideFlags.DontSave };
                    var cam = lens.AddComponent<Camera>(); cam.enabled = false; cam.allowHDR = true;
                    lens.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(new Vector3(0f, 1f, 0f) - eye));
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var steps = HeroFxPrewarm.Run(null, cam);
                    int frames = 0;
                    while (steps.MoveNext()) frames++;
                    watch.Stop();
                    report.AppendLine("WARM-UP (what the loading screen now does): " + watch.Elapsed.TotalMilliseconds.ToString("0.0") + " ms over " + frames + " loading frames");
                }
                else report.AppendLine("COLD: no warm-up run first");
                var art = RosterBook.Load()?.FindPersonArt("zack");
                for (int round = 0; round < 2; round++)
                {
                    string when = round == 0 ? "first cast" : "second cast";
                    Time(report, when + ": Whirlwind", () => { var f = new GameObject("f"); f.transform.SetParent(host.transform, false); var w = AmihanWhirlwind.Build(f.transform, 2.5f); w.Pose(1f); return () => { w.Free(); Object.DestroyImmediate(f); }; });
                    Time(report, when + ": Drift", () => { var d = AmihanDashWake.Build(Vector3.zero, new Vector3(0f, 0f, 4f), host.transform, null); d.StepTo(0.3f); return () => Object.DestroyImmediate(d.gameObject); });
                    Time(report, when + ": Quick Circuit", () =>
                    {
                        var body = art != null && art.Model != null ? (GameObject)Object.Instantiate(art.Model) : new GameObject("body");
                        body.hideFlags = HideFlags.DontSave;
                        var flash = ZackCircuitFlash.Stage(body.transform, body, Vector3.right);
                        flash.StagedCut(); flash.StagedView(eye, false); flash.StepTo(FxStudyZackCircuit.Tell + 0.1f);
                        return () => { flash.Free(); Object.DestroyImmediate(flash.gameObject); Object.DestroyImmediate(body); };
                    });
                    Time(report, when + ": Boulder (cast moment)", () =>
                    {
                        var g = new GameObject("b"); g.transform.SetParent(host.transform, false);
                        var held = new Vector3(0f, 1f, 0f);
                        var b = new FxStudyDanteBoulder(g.transform, held, held + new Vector3(4.2f, 0f, 0f), false, 1f, 0.6f, FxStudyDanteBoulder.PartCast);
                        b.Sample(FxStudyDanteBoulder.CastMoment + 0.2f, eye);
                        return () => { b.Dispose(); Object.DestroyImmediate(g); };
                    });
                    Time(report, when + ": Featherfall (sample)", () => { var f = new FxStudyFeatherfall(host.transform, 0.5f, false); f.Pose(1f, 2.8f, 0.8f, 1f, -1f); return f.Dispose; });
                    Time(report, when + ": a landing puff (into a field already made)", () => { var field = new CloudPuffField(host.transform, "fc"); var w = System.Diagnostics.Stopwatch.StartNew(); field.Spawn(CloudPuffField.Puff.Landing(Vector3.zero), 0f); return field.Dispose; });
                }
                // The puff field's draw, narrowed to its live rows.
                var puffs = new CloudPuffField(host.transform, "span");
                puffs.Spawn(CloudPuffField.Puff.Landing(Vector3.zero), 0f); puffs.Tick(0.05f);
                int one = puffs.DrawnTriangles;
                for (int k = 0; k < 7; k++) puffs.Spawn(CloudPuffField.Puff.Landing(new Vector3(k, 0f, 0f)), 0.06f);
                puffs.Tick(0.07f); int eight = puffs.DrawnTriangles;
                for (int k = 0; k < 30; k++) puffs.Spawn(CloudPuffField.Puff.Landing(new Vector3(k, 0f, 0f)), 0.1f + k * 0.001f);
                puffs.Tick(0.2f); int full = puffs.DrawnTriangles;
                puffs.Tick(0.39f); int later = puffs.DrawnTriangles;
                var tick = System.Diagnostics.Stopwatch.StartNew();
                for (int f = 0; f < 600; f++) { puffs.Spawn(CloudPuffField.Puff.Landing(new Vector3(f % 9, 0f, 0f)), 1f + f / 60f); puffs.Tick(1f + f / 60f); }
                tick.Stop();
                report.AppendLine("PUFF FIELD DRAW: " + one + " triangles with one puff live, " + eight + " with eight, " + full + " with all 24 rows live, " + later
                    + " a moment later as the oldest retire; of " + CloudPuffField.Triangles + " in the mesh. A frame with a landing and the narrowing: "
                    + (tick.Elapsed.TotalMilliseconds / 600.0 * 1000.0).ToString("0.00") + " microseconds");
                puffs.Dispose();
                File.WriteAllText(donePath, report.ToString());
            }
            catch (Exception failure) { File.WriteAllText(failedPath, report + "\n" + failure); }
            finally { if (lens != null) Object.DestroyImmediate(lens); Object.DestroyImmediate(host); }
        }

        private static void Time(System.Text.StringBuilder report, string name, Func<Action> make)
        {
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var free = make();
                watch.Stop();
                free?.Invoke();
                report.AppendLine(name + ": " + watch.Elapsed.TotalMilliseconds.ToString("0.00") + " ms");
            }
            catch (Exception e) { report.AppendLine(name + ": could not be measured: " + e.GetType().Name + ": " + e.Message.Replace('\n', ' ')); }
        }

        /// <summary>
        /// TWO WHIRLWINDS AT ONCE STILL DRAW APART (`cloudcourt auditoverlap<tag>`). Since 2026-10-17 every whirlwind shares
        /// its meshes with every other, so this proves the sharing changed nothing that one draws because of another: whirlwind
        /// A is drawn alone, then again with B built beside it at another age (B hidden for the shot), and the two pictures
        /// must be the same; then B is drawn and compared with a whirlwind built alone and posed at B's age.
        /// </summary>
        private static void RunOverlap(string donePath, string failedPath)
        {
            var report = new System.Text.StringBuilder();
            var host = new GameObject("~Overlap") { hideFlags = HideFlags.DontSave };
            var target = new RenderTexture(480, 270, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            var read = new Texture2D(480, 270, TextureFormat.RGB24, false);
            try
            {
                var lens = new GameObject("lens") { hideFlags = HideFlags.DontSave }; lens.transform.SetParent(host.transform, false);
                var cam = lens.AddComponent<Camera>(); cam.enabled = false; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
                Vector3 eye = new Vector3(0f, 2f, -9f);
                lens.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(new Vector3(0f, 1.5f, 0f) - eye));
                AmihanWhirlwind.StagedEye = eye;
                Color32[] Shot() { cam.targetTexture = target; cam.Render(); var keep = RenderTexture.active; RenderTexture.active = target; read.ReadPixels(new Rect(0, 0, 480, 270), 0, 0); read.Apply(); RenderTexture.active = keep; cam.targetTexture = null; return read.GetPixels32(); }
                string Diff(Color32[] x, Color32[] y) { int most = 0; long sum = 0; for (int i = 0; i < x.Length; i++) { int d = Mathf.Max(Mathf.Abs(x[i].r - y[i].r), Mathf.Max(Mathf.Abs(x[i].g - y[i].g), Mathf.Abs(x[i].b - y[i].b))); most = Mathf.Max(most, d); sum += d; } return "mean " + (sum / (double)x.Length).ToString("0.000") + " of 255, largest " + most; }
                GameObject Front() { var f = new GameObject("front") { hideFlags = HideFlags.DontSave }; f.transform.SetParent(host.transform, false); return f; }
                var fa = Front(); var a = AmihanWhirlwind.Build(fa.transform, 2.5f); a.Pose(0.8f);
                var aAlone = Shot();
                var fb = Front(); var b = AmihanWhirlwind.Build(fb.transform, 2.5f); b.Pose(1.6f);
                a.Pose(0.8f); fb.SetActive(false);
                var aWithB = Shot();
                report.AppendLine("A alone against A with B built and posed beside it: " + Diff(aAlone, aWithB));
                fb.SetActive(true); fa.SetActive(false); b.Pose(1.6f);
                var bShot = Shot();
                a.Free(); Object.DestroyImmediate(fa);
                b.Free(); Object.DestroyImmediate(fb);
                var fc = Front(); var c = AmihanWhirlwind.Build(fc.transform, 2.5f); c.Pose(1.6f);
                var cAlone = Shot();
                report.AppendLine("B (built while A lived) against C built alone, both at B's age: " + Diff(bShot, cAlone));
                int lit = 0; foreach (var px in cAlone) if (px.r + px.g + px.b > 30) lit++;
                report.AppendLine("(the whirlwind covers " + (100.0 * lit / cAlone.Length).ToString("0.0") + "% of the shot, so the comparison is of something)");
                c.Free(); Object.DestroyImmediate(fc);
                File.WriteAllText(donePath, report.ToString());
            }
            catch (Exception failure) { File.WriteAllText(failedPath, report + "\n" + failure); }
            finally { AmihanWhirlwind.StagedEye = null; Object.DestroyImmediate(read); target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(host); }
        }

        private static void RunAudit(string tag)
        {
            string named = "fxbench_cloudcourt_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            Directory.CreateDirectory(OutDir);
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            var report = new System.Text.StringBuilder();
            if (tag.IndexOf("first", StringComparison.OrdinalIgnoreCase) >= 0) { RunFirstCast(tag, donePath, failedPath); return; }
            if (tag.IndexOf("overlap", StringComparison.OrdinalIgnoreCase) >= 0) { RunOverlap(donePath, failedPath); return; }
            report.AppendLine("effect | to make one (ms) | to pose a frame (ms) | heap growth over the frames (bytes) | collections | draw calls at its busiest | triangles at its busiest | notes");
            var host = new GameObject("~EffectAudit") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                Vector3 eye = new Vector3(0f, 1.6f, -7f);
                AmihanWhirlwind.StagedEye = eye;

                // Amihan's WHIRLWIND as the court stands it: under its moving front, posed by the gale's age.
                Audit(report, "Whirlwind (court)", 5, () =>
                {
                    var front = new GameObject("front"); front.transform.SetParent(host.transform, false);
                    var wind = AmihanWhirlwind.Build(front.transform, 2.5f);
                    return new AuditRun(front, t => wind.Pose(t % 2.4f), () => { wind.Free(); Object.DestroyImmediate(front); }, 1.2f);
                }, "approved and final: its files are not to be touched");

                // Her DRIFT as the dash builds it (no body to follow, so it is drawn out over the dash's own time).
                Audit(report, "Drift (dash wake)", 5, () =>
                {
                    var wake = AmihanDashWake.Build(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 5f), host.transform, null);
                    return new AuditRun(wake.gameObject, t => wake.StepTo(t % 1.4f), () => Object.DestroyImmediate(wake.gameObject), 0.5f);
                }, "");

                // Her FEATHERFALL sample (not yet in a match), at the hold.
                Audit(report, "Featherfall (sample, not in game)", 5, () =>
                {
                    var fx = new FxStudyFeatherfall(host.transform, 0.5f, false);
                    return new AuditRun(fx.Root.gameObject, t => fx.Pose(t, 2.8f, Mathf.Repeat(1f - t / 5f, 1f), t, -1f), fx.Dispose, 1.5f);
                }, "");

                // Zack's QUICK CIRCUIT as a cast builds it: the tell (bakes his body), then the cut (bakes it again).
                var art = RosterBook.Load()?.FindPersonArt("zack");
                if (art == null || art.Model == null) report.AppendLine("Zack's Quick Circuit | | | | | | | could not be measured: no model for 'zack' in the roster");
                else Audit(report, "Zack's Quick Circuit (tell and cut)", 5, () =>
                {
                    // The circuit stands itself in its body's scene, so the body is a loose object of the open scene (never saved,
                    // destroyed straight after), not a child of the hidden host, which belongs to no scene.
                    var body = (GameObject)Object.Instantiate(art.Model);
                    body.hideFlags = HideFlags.DontSave;
                    body.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                    var flash = ZackCircuitFlash.Stage(body.transform, body, Vector3.right);
                    if (flash == null) throw new InvalidOperationException("the circuit could not be staged on the model");
                    flash.StagedCut();
                    flash.StagedView(eye, false);
                    return new AuditRun(flash.gameObject, t => flash.StepTo(FxStudyZackCircuit.Tell + (t % 0.9f)), () => { flash.Free(); Object.DestroyImmediate(flash.gameObject); Object.DestroyImmediate(body); }, FxStudyZackCircuit.Tell + 0.2f);
                }, "the two bakes of his body are in the making time");

                // Dante's BOULDER as the match plays it: the cast moment and the hit moment, each its own copy of the study.
                Audit(report, "Dante's Boulder, cast moment", 5, () =>
                {
                    var go = new GameObject("BoulderCast"); go.transform.SetParent(host.transform, false);
                    var held = new Vector3(0f, 1f, 0f);
                    var fx = new FxStudyDanteBoulder(go.transform, held, held + new Vector3(4.2f, 0f, 0f), false, 1f, 0.6f, FxStudyDanteBoulder.PartCast);
                    return new AuditRun(go, t => fx.Sample(FxStudyDanteBoulder.CastMoment - 0.08f + (t % 2.0f), eye), () => { fx.Dispose(); Object.DestroyImmediate(go); }, 0.4f);
                }, "Dante's session's files");
                Audit(report, "Dante's Boulder, hit moment", 5, () =>
                {
                    var go = new GameObject("BoulderHit"); go.transform.SetParent(host.transform, false);
                    var struck = new Vector3(0f, 1f, 0f);
                    var fx = new FxStudyDanteBoulder(go.transform, struck + new Vector3(-4.2f, 0.05f, 0f), struck, false, 1f, 0.6f, FxStudyDanteBoulder.PartHit);
                    return new AuditRun(go, t => fx.Sample(FxStudyDanteBoulder.HitMoment + (t % 1.5f), eye), () => { fx.Dispose(); Object.DestroyImmediate(go); }, 0.3f);
                }, "Dante's session's files");

                // The bench's SPARKS on their own (`FxFlecks`): sixteen a frame, as the whirlwind's motes are.
                Audit(report, "Flecks alone, 16 sparks", 5, () =>
                {
                    var flecks = new FxFlecks("audit-flecks", host.transform, 0, "glow_soft");
                    return new AuditRun(flecks.Piece.Go, t =>
                    {
                        flecks.Begin(eye);
                        for (int i = 0; i < 16; i++) flecks.Add(new Vector3(Mathf.Sin(i + t), 1f + 0.1f * i, Mathf.Cos(i - t)), new Vector3(0f, 1f, 0.3f), 0.4f, 0.2f, 0.8f);
                        flecks.End(t);
                    }, () => { Object.DestroyImmediate(flecks.Mesh); Object.DestroyImmediate(flecks.Piece.Material); Object.DestroyImmediate(flecks.Piece.Go); }, 0.5f);
                }, "inside the court whirlwind, the drift, the boulder and most bench effects");

                // The OLD SMOKE (`FxSmoke`), as the bench's three plumes and inside the wisp. Nothing in a match makes it.
                Audit(report, "Old smoke: three plumes (bench 'smoke')", 3, () =>
                {
                    var fx = FxStudyEffect.Create("smoke", host.transform);
                    return new AuditRun(fx.Root.gameObject, t => fx.Sample(t % fx.Seconds, eye), fx.Dispose, 1.0f);
                }, "bench only: no match code builds FxSmoke");
                Audit(report, "Old smoke inside the wisp (bench 'wisp')", 3, () =>
                {
                    var fx = FxStudyEffect.Create("wisp", host.transform);
                    return new AuditRun(fx.Root.gameObject, t => fx.Sample(t % fx.Seconds, eye), fx.Dispose, 1.0f);
                }, "bench only");

                // The LANDING PUFFS in a busy case: eight landings every frame, so all 24 rows are always live.
                Audit(report, "Landing puffs, 8 landings every frame", 3, () =>
                {
                    var field = new CloudPuffField(host.transform, "audit-puffs");
                    float clock = 0f;
                    return new AuditRun(host.transform.Find("audit-puffs").gameObject, t =>
                    {
                        clock = t;
                        for (int k = 0; k < 8; k++) field.Spawn(CloudPuffField.Puff.Landing(new Vector3(k, 0f, 0f)), clock);
                        field.Tick(clock);
                    }, field.Dispose, 0.2f);
                }, "one mesh, one material for every puff in the match");

                // The cloud's two columns (F12), for comparison with what was measured on it.
                Audit(report, "Cloud, two columns with the line (F12)", 3, () =>
                {
                    var fx = new FxStudyCloud(host.transform, false, FxStudyCloud.Line);
                    return new AuditRun(fx.Root.gameObject, t => fx.Sample(t, eye), fx.Dispose, 2f);
                }, "");
                File.WriteAllText(donePath, report.ToString());
            }
            catch (Exception failure) { File.WriteAllText(failedPath, report + "\n" + failure); }
            finally { AmihanWhirlwind.StagedEye = null; Object.DestroyImmediate(host); }
        }

        private sealed class AuditRun
        {
            public readonly GameObject Root; public readonly Action<float> Pose; public readonly Action Free; public readonly float Busiest;
            public AuditRun(GameObject root, Action<float> pose, Action free, float busiest) { Root = root; Pose = pose; Free = free; Busiest = busiest; }
        }

        private static void Audit(System.Text.StringBuilder report, string name, int makes, Func<AuditRun> make, string note)
        {
            try
            {
                // What it costs to make: built and freed `makes` times, the building alone timed.
                var making = new System.Diagnostics.Stopwatch();
                for (int i = 0; i < makes; i++) { making.Start(); var once = make(); making.Stop(); once.Free(); }
                var run = make();
                const int Frames = 300;
                for (int f = 0; f < 20; f++) run.Pose(f / 60f);
                long held = GC.GetTotalMemory(false); int collections = GC.CollectionCount(0);
                var watch = System.Diagnostics.Stopwatch.StartNew();
                for (int f = 0; f < Frames; f++) run.Pose(f / 60f);
                watch.Stop();
                long grew = GC.GetTotalMemory(false) - held; collections = GC.CollectionCount(0) - collections;
                run.Pose(run.Busiest);
                int draws = 0; long triangles = 0;
                foreach (var r in run.Root.GetComponentsInChildren<Renderer>(false))
                {
                    if (!r.enabled || r.forceRenderingOff) continue;
                    Mesh mesh = r is SkinnedMeshRenderer s ? s.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh == null) continue;
                    int passes = r.sharedMaterial != null ? Mathf.Max(1, r.sharedMaterial.passCount) : 1;
                    draws += passes * Mathf.Max(1, mesh.subMeshCount);
                    for (int m = 0; m < mesh.subMeshCount; m++) triangles += mesh.GetIndexCount(m) / 3;
                }
                run.Free();
                report.AppendLine(name + " | " + (making.Elapsed.TotalMilliseconds / makes).ToString("0.00") + " | " + (watch.Elapsed.TotalMilliseconds / Frames).ToString("0.0000")
                                  + " | " + grew + " | " + collections + " | " + draws + " | " + triangles + " | " + note);
            }
            catch (Exception e) { report.AppendLine(name + " | | | | | | | could not be measured: " + e.GetType().Name + ": " + e.Message.Replace('\n', ' ')); }
        }
    }
}

// Measured with `cloudcourt audit<tag>`; reloaded for a cold measurement.
