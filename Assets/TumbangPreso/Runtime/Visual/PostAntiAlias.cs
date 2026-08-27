using TumbangPreso.Settings;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The FXAA pass, and the one place that measures whether MSAA actually reached the frame.
    ///
    /// ⚠️⚠️ IT IS A SECOND COMPONENT RATHER THAN A BRANCH INSIDE `ColourGrade`, AND THE REASON
    /// IS THE ORDER. FXAA thresholds luma against display-referred numbers, so it has to see a
    /// frame that has already been tonemapped and graded. `ColourGrade` is what performs that
    /// conversion (`Awake` sets `allowHDR`, and Eskinita's ambient alone is (1.02, 0.96, 0.86)
    /// before a light is counted), and Unity runs image effects in COMPONENT ORDER. Folding the
    /// filter into `ColourGrade`'s single blit would mean filtering the raw HDR frame, where a
    /// 4.0 sky against a 0.9 wall clears any threshold and the filter blurs the whole boundary
    /// rather than its staircase.
    ///
    /// ⚠️⚠️ AND IT IS ATTACHED ONLY TO THE TWO GAMEPLAY CAMERAS, NOT WHEREVER `ColourGrade` IS.
    /// `ColourGrade` is also carried by the character portrait, the map preview and six editor
    /// probe benches. Those all render into a `targetTexture` built with `antiAliasing = 4` or
    /// `8` already, so a filter on top would soften a picture that is not aliased, and the probe
    /// images are what this project compares itself against between sessions. Changing them
    /// changes the measuring stick. `CameraRig` and `SpectatorCamera` add this; nothing else does.
    ///
    /// ⚠️ THE PASS IS SKIPPED, NOT DISABLED, WHEN THE MODE HAS NO FXAA. The component stays on
    /// the camera so a player switching modes from the pause menu takes effect on the next frame
    /// rather than on the next match. An `OnRenderImage` that only blits still costs a
    /// full-screen copy, which is the price of that, and it is the same trade `ColourGrade`
    /// already makes on its identity path.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class PostAntiAlias : MonoBehaviour
    {
        private Camera _camera;
        private Material _material;
        private bool _shaderMissing;

        private static readonly int EdgeThresholdId = Shader.PropertyToID("_EdgeThreshold");
        private static readonly int EdgeThresholdMinId = Shader.PropertyToID("_EdgeThresholdMin");
        private static readonly int SpanId = Shader.PropertyToID("_Span");

        private void Awake() => _camera = GetComponent<Camera>();

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            ReportOnce(_camera, source);

            if (!AntiAliasModes.FxaaActive || _shaderMissing)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (_material == null)
            {
                // ⚠️ A SHADER ONLY `Shader.Find` REACHES IS STRIPPED FROM THE PLAYER. Same rule
                // `ToonSkin` and `ColourGrade` carry: it has to be listed in
                // `GameBuilder.EnsureRuntimeShaders` or the setting does nothing in a build while
                // working perfectly in the editor, which is the worst version of this bug
                // because the editor is where it gets checked.
                var shader = Shader.Find("TumbangPreso/Fxaa");

                if (shader == null)
                {
                    // ⚠️ NOT `enabled = false`. Disabling the component removes the image effect
                    // and therefore removes a link from the camera's post chain, and on the
                    // spectator that chain also carries the replay capture. A latched flag keeps
                    // the chain the same shape and just passes the frame through.
                    _shaderMissing = true;
                    Debug.LogWarning("[AA] TumbangPreso/Fxaa is missing, so anti-aliasing is " +
                                     "MSAA only. Check GameBuilder.EnsureRuntimeShaders.");

                    Graphics.Blit(source, destination);
                    return;
                }

                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            // ⚠️ SET EVERY FRAME RATHER THAN ONCE. The material is created lazily and lives as
            // long as the camera does, and these are the only three numbers on it, so pushing
            // them is cheaper than reasoning about which code path could have changed one.
            _material.SetFloat(EdgeThresholdId, 0.166f);
            _material.SetFloat(EdgeThresholdMinId, 0.0833f);
            _material.SetFloat(SpanId, 8.0f);

            Graphics.Blit(source, destination, _material);
        }

        private void OnDestroy()
        {
            if (_material != null) DestroyImmediate(_material);
        }

        // ------------------------------------------------------------------ the measurement

        private static bool _reported;

        /// <summary>
        /// Log, once per process, what the engine actually handed the FIRST image effect on a
        /// screen camera.
        ///
        /// ⚠️⚠️ THIS EXISTS BECAUSE "IS MSAA REACHING THE FRAME" CANNOT BE ANSWERED BY READING
        /// THE SETTINGS. `QualitySettings.antiAliasing` reports what was REQUESTED and reads
        /// back the same whether or not the rasteriser honoured it. What settles it is the
        /// sample count on the render target the camera was actually given, and the only place
        /// that target is visible from script is the `source` argument of the first
        /// `OnRenderImage` in the chain. So both effects call this and the first one to run
        /// wins.
        ///
        /// ⚠️ `targetTexture == null` IS THE FILTER, and it is doing real work rather than
        /// guarding a null. A camera writing into a `targetTexture` takes its sample count off
        /// that texture and ignores `QualitySettings.antiAliasing` entirely, so the character
        /// portrait and the map preview would answer a different question and, being built
        /// during the menu, would answer it first and latch the flag.
        ///
        /// ⚠️ AND IT IS ONE LINE, ALWAYS, RATHER THAN A DEVELOPMENT-BUILD LOG. The whole reason
        /// this was worth writing is that the shipped player is the configuration nobody can
        /// inspect, and a line in `Player.log` is how the next report of "it still looks jagged"
        /// gets answered in a minute instead of in a session.
        /// </summary>
        public static void ReportOnce(Camera camera, RenderTexture source)
        {
            if (_reported) return;
            if (camera == null || camera.targetTexture != null) return;

            _reported = true;

            int delivered = source != null ? source.antiAliasing : 0;

            Debug.Log($"[AA] mode requested {AntiAliasModes.RequestedSamples}x MSAA" +
                      $" + FXAA {(AntiAliasModes.FxaaActive ? "on" : "off")};" +
                      $" QualitySettings.antiAliasing {QualitySettings.antiAliasing};" +
                      $" camera '{camera.name}' allowMSAA {camera.allowMSAA}," +
                      $" allowHDR {camera.allowHDR}," +
                      $" path {camera.actualRenderingPath};" +
                      $" target delivered {delivered} sample(s)," +
                      $" format {(source != null ? source.format.ToString() : "none")};" +
                      $" device supportsMultisampledTextures" +
                      $" {SystemInfo.supportsMultisampledTextures}");
        }

        /// <summary>Test seam. A PlayMode suite that loads two arenas would otherwise get one
        /// line for the first and silence for the second.</summary>
        public static void ResetReportForTests() => _reported = false;
    }
}
