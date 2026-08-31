using System.Collections;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Reports whether anti-aliasing is actually reaching the frame, rather than whether it was
    /// configured.
    ///
    /// ⚠️⚠️ THE SETTING AND THE RESULT ARE DIFFERENT QUESTIONS HERE, WHICH IS THE WHOLE REASON THE
    /// AA WORK WAS BUILT AS MSAA PLUS FXAA RATHER THAN AS MSAA. A camera carrying any
    /// `OnRenderImage` renders into an engine-allocated intermediate, and `ColourGrade` guarantees
    /// there is always one. A multisampled floating-point intermediate is an allocation the engine
    /// is free to decline, and when it does, `QualitySettings.antiAliasing` still reads back the
    /// value that was set and nothing anywhere reports that the samples were dropped.
    ///
    /// ⚠️ SO THE LOAD-BEARING NUMBER IS `RenderTexture.active.antiAliasing` INSIDE AN IMAGE
    /// EFFECT, not `QualitySettings.antiAliasing`. That is the sample count on the target the post
    /// chain was actually handed. 1 means the MSAA rows are paying bandwidth for nothing and FXAA
    /// is carrying the entire result on its own.
    ///
    /// ⚠️ IT EXISTS BECAUSE `PostAntiAlias.ReportOnce` NEVER PRINTED. It is reached from
    /// `ColourGrade`, and across a full PlayMode run no `[AA]` line appeared in the log at all,
    /// which leaves "AA is configured and working", "AA is configured and silently dropped" and
    /// "the component was never added" indistinguishable. This separates them.
    /// </summary>
    public sealed class AntiAliasStateProbe
    {
        [UnityTest]
        [Category("WallClock")]
        public IEnumerator ReportWhetherAntiAliasingReachesTheFrame()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 40; i++) yield return null;

            CameraRig rig = null;

            foreach (var r in Object.FindObjectsByType<CameraRig>(FindObjectsSortMode.None))
            {
                if (r.Camera == null || !r.Camera.enabled) continue;
                rig = r;
                break;
            }

            Assert.IsNotNull(rig, "no active CameraRig, so there is no frame to ask about");

            var cam = rig.Camera;
            var report = new StringBuilder();

            report.AppendLine("=== ANTI-ALIAS STATE ===");
            report.AppendLine($"QualitySettings.GetQualityLevel = {QualitySettings.GetQualityLevel()} " +
                              $"({QualitySettings.names[QualitySettings.GetQualityLevel()]})");
            report.AppendLine($"QualitySettings.antiAliasing    = {QualitySettings.antiAliasing}");
            report.AppendLine($"camera.allowMSAA                = {cam.allowMSAA}");
            report.AppendLine($"camera.allowHDR                 = {cam.allowHDR}");
            report.AppendLine($"camera.actualRenderingPath      = {cam.actualRenderingPath}");
            report.AppendLine($"SystemInfo.supportsMultisampled = {SystemInfo.supportsMultisampledTextures}");

            var post = cam.GetComponent<PostAntiAlias>();
            var grade = cam.GetComponent<ColourGrade>();
            var outline = cam.GetComponent<WorldOutline>();

            report.AppendLine($"PostAntiAlias component         = {(post == null ? "ABSENT" : "present, enabled=" + post.enabled)}");
            report.AppendLine($"ColourGrade component           = {(grade == null ? "ABSENT" : "present, enabled=" + grade.enabled)}");
            report.AppendLine($"WorldOutline component          = {(outline == null ? "ABSENT" : "present, enabled=" + outline.enabled)}");

            // ⚠️ THE MEASUREMENT ITSELF. Render into a target REQUESTED with 4 samples and read
            // back what the descriptor actually carries. If the engine honours it the number is 4;
            // if it silently resolves early or declines the allocation the number is 1, and that is
            // the answer the whole AA design is hedging against.
            var desc = new RenderTextureDescriptor(1280, 720, RenderTextureFormat.ARGB32, 24)
            {
                msaaSamples = 4,
            };

            var rt = new RenderTexture(desc);
            rt.Create();

            report.AppendLine($"requested msaaSamples 4 -> RenderTexture.antiAliasing = {rt.antiAliasing}");

            var previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = previous;

            report.AppendLine($"after Render, rt.antiAliasing    = {rt.antiAliasing}");

            rt.Release();
            Object.DestroyImmediate(rt);

            Debug.Log(report.ToString());

            // A probe, not a gate: it prints and passes. The bound that would make this an
            // assertion is exactly the thing nobody has measured yet.
            Assert.Pass();
        }
    }
}
