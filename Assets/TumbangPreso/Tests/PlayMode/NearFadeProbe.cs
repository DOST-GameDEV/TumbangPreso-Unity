using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Walks a camera into one of Eskinita's electric posts and photographs the approach, so the
    /// near-camera dither can be judged from pictures instead of from an argument.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE TWO OPPOSITE REPORTS ARE BOTH CONSISTENT WITH THE SOURCE. The pass
    /// was predicted to leave a GHOST OUTLINE around a dissolved post, because `WorldOutline` reads
    /// a depth-normals prepass filled through a REPLACEMENT shader, and a replacement shader brings
    /// its own fragment code, so `NearFade`'s `clip()` is invisible to it. What was reported from
    /// the played build on 2026-08-28 was the opposite: *"the dither fade works for the outlines,
    /// but not the objects"*. Those two cannot both be true, and neither can be settled by reading
    /// the shader again. This renders the actual frames.
    ///
    /// ⚠️ THREE DISTANCES, SPANNING THE BAND ON PURPOSE. `NearFade.FadeStartMetres` is 1.80 and
    /// `FadeEndMetres` is 0.35, so 2.50 m must be untouched, 1.10 m must be visibly stippled, and
    /// 0.20 m must be gone completely. A single frame cannot distinguish "the fade is broken" from
    /// "the camera was outside the band".
    ///
    /// ⚠️ IT MUST RUN IN PLAY MODE. `NearFade.Install` is reached from `EnvColourPass.Start`, and
    /// the outline's `OnRenderImage` sits on the opaque hook, which does not fire under
    /// `Camera.Render()` outside play mode. An edit-mode version of this photographs nothing, which
    /// cost four captures to learn on `WorldOutlineProbe`.
    /// </summary>
    public sealed class NearFadeProbe
    {
        private const int Width = 900;
        private const int Height = 600;
        private const string OutDir = "Logs/shots-near-fade";

        [UnityTest]
        [Category("WallClock")]
        public IEnumerator PhotographTheApproachToAPost()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 40; i++) yield return null;

            // The posts are `Dressing/Kable/Poste_0..11`. Any of them will do; take the first that
            // actually carries a renderer.
            Transform post = null;

            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (!t.name.StartsWith("Poste")) continue;
                if (t.GetComponentInChildren<MeshRenderer>(true) == null) continue;
                post = t;
                break;
            }

            Assert.IsNotNull(post, "no Poste_* with a renderer in Eskinita, so there is nothing to walk into");

            var report = new StringBuilder();
            report.AppendLine("=== NEAR FADE PROBE ===");
            report.AppendLine($"post={post.name} at {post.position}");

            // ⚠️ THE SHADER ON THE POST IS THE FIRST THING TO REPORT. If this does not read
            // `TumbangPreso/NearFade` then nothing downstream matters and every frame below is
            // measuring the wrong question.
            var renderers = post.GetComponentsInChildren<MeshRenderer>(true);
            report.AppendLine($"renderers under it: {renderers.Length}");

            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                foreach (var m in mats)
                {
                    report.AppendLine($"  {r.name}: shader={(m == null ? "NULL" : m.shader.name)}" +
                                      (m != null && m.HasProperty("_NearFadeStart")
                                          ? $" start={m.GetFloat("_NearFadeStart")} end={m.GetFloat("_NearFadeEnd")} cell={m.GetFloat("_NearFadeCell")}"
                                          : " (no fade properties)"));
                }
            }

            Directory.CreateDirectory(OutDir);

            // Eye height, looking horizontally at the post from decreasing distance.
            var bounds = renderers[0].bounds;
            float eyeY = bounds.center.y;

            foreach (float d in new[] { 2.50f, 1.10f, 0.20f })
            {
                var eye = new Vector3(bounds.center.x, eyeY, bounds.center.z - d);
                Shoot(eye, bounds.center, d, report);
                yield return null;
            }

            Debug.Log(report.ToString());
            Assert.Pass();
        }

        private static void Shoot(Vector3 eye, Vector3 lookAt, float metres, StringBuilder report)
        {
            var go = new GameObject($"NearFadeProbeCam_{metres:F2}");
            go.transform.position = eye;
            go.transform.rotation = Quaternion.LookRotation(lookAt - eye, Vector3.up);

            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 95.0f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200.0f;
            cam.depthTextureMode |= DepthTextureMode.DepthNormals;

            // Same stack the gameplay rig builds, in the same order, so the picture is the game's
            // rather than a bare camera's.
            var outline = go.AddComponent<WorldOutline>();
            outline.PrototypeEnabled = true;
            go.AddComponent<ColourGrade>().AdoptFromScene();

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            string path = Path.Combine(OutDir, $"post_{metres:F2}m.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            report.AppendLine($"wrote {path}");

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }
    }
}
