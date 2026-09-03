using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// One frame of the arena, photographed at a range of ambient settings, so the right one
    /// can be SOLVED against the Godot reference instead of guessed at.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE THE GRADE HAS NOW BEEN "FIXED" FOUR TIMES BY EYE AND MISSED
    /// FOUR TIMES, most recently by overshooting: the pass that stopped the world being washed
    /// out made the street 2.6x too dark, and both were shipped as done. 🧑 2026-08-18: *"u keep
    /// comparing it urself until it matches"*, *"u dont stop unless ur sure its the exact same"*.
    ///
    /// A single-sample loop cannot converge here, because each attempt costs a whole play run
    /// and the thing being tuned is not monotonic in anything obvious: `ambient_light_color` is
    /// warm, the sky it is mixed with is cold, and the ACES curve on the camera compresses the
    /// two differently. So this captures the WHOLE curve in one run and `tools/compare_tone.py`
    /// reads the answer off it.
    ///
    /// ⚠️ IT MOVES ONLY `RenderSettings`, NOT THE IMPORTER. The importer writes these values
    /// into the scene, and re-running it per candidate would cost an editor launch each. The
    /// winning value goes back into `TscnImporter.ApplyEnvironment` afterwards, which is the
    /// only place allowed to decide it in a shipped build.
    /// </summary>
    public class ToneSweep
    {
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        private const string OutDir = "Logs/shots-tone";

        /// <summary>
        /// The candidates, as (ambient sRGB, label). Godot's model is
        ///
        ///     ambient = lerp(srgb_to_linear(colour), sky_radiance, sky_contribution) * energy
        ///
        /// with Eskinita authoring colour #A09385, energy 1.65 and contribution 0.35, and the
        /// panorama measuring (0.4125, 0.4857, 0.6052) in linear. That prediction is `mix`; the
        /// others bracket it, because the prediction is exactly the kind of thing that has been
        /// wrong three times already and a bracket costs nothing once the run is happening.
        /// </summary>
        private static readonly (float r, float g, float b) Mix = (0.615f, 0.594f, 0.601f);

        /// <summary>
        /// ⚠️⚠️ ROUND ONE OF THIS SWEEP PROVED IT IS NOT AN AMBIENT PROBLEM, WHICH IS THE WHOLE
        /// REASON IT SWEEPS RATHER THAN GUESSES. Fitting the six frames per band gives two
        /// answers that cannot both be satisfied by one number:
        ///
        ///     to land the ROAD      on the reference, ambient must be about (1.14, 1.02, 0.87)
        ///     to land the BUILDINGS on the reference, ambient must be about (0.29, 0.15, ~0)
        ///
        /// A single scalar cannot be four times too small and twice too large at once, so the
        /// ROAD specifically is receiving less light than the same road does in Godot while
        /// everything standing on it is receiving about the right amount. That is a shadowing
        /// difference, not a grading one: the floor is one 200 x 200 m quad, and a directional
        /// shadow map stretched over a cascade that size acnes a large flat surface into
        /// self-shadow — which is exactly "uniformly dark, and warm because only the ambient is
        /// left".
        ///
        /// So round two varies the shadow terms as well, and the ambient candidates it keeps are
        /// the two that bracket the buildings rather than the road.
        /// </summary>
        private static readonly (string label, System.Action apply)[] Setups =
        {
            ("a-asis", () => Ambient(Mix)),
            ("b-sky65-key64", () => { Ambient(SkyHeavy); Key(1.15f * 0.636f); }),
            ("c-sky65-key60", () => { Ambient(SkyHeavy); Key(1.15f * 0.60f); }),
            ("d-sky65-key70", () => { Ambient(SkyHeavy); Key(1.15f * 0.70f); }),
            ("e-mix-key68", () => { Ambient(Mix); Key(1.15f * 0.68f); }),
            ("f-sky65x1.1-key64", () => { Ambient(Scale(SkyHeavy, 1.1f)); Key(1.15f * 0.636f); }),
            ("g-sky65x0.9-key64", () => { Ambient(Scale(SkyHeavy, 0.9f)); Key(1.15f * 0.636f); }),
            ("h-sky100-key64", () => { Ambient((0.681f, 0.801f, 0.999f)); Key(1.15f * 0.636f); }),
        };

        /// <summary>
        /// § THE AMBIENT THAT ACTUALLY FITS, AND IT IS THE SKY-WEIGHTED MIX.
        ///
        /// ⚠️ SOLVED FROM THE REFERENCE, NOT DERIVED FROM THE DOCS. Inverting the tonemap on the
        /// Godot road median (79, 69, 71) against the floor's own authored albedo gives the
        /// incident light on that surface as (1.111, 0.983, 0.877). Godot's stated 65% colour /
        /// 35% sky mix predicts (0.615, 0.594, 0.601) of ambient, which leaves a residue that no
        /// single key strength can satisfy on all three channels. The 35/65 weighting — colour
        /// 0.35, sky 0.65 — gives (0.645, 0.689, 0.785), and with the key scaled it lands R and G
        /// exactly and B within 6%.
        ///
        /// Sky radiance measured off `sky_panorama.png` itself: linear mean (0.4125, 0.4857,
        /// 0.6052).
        /// </summary>
        private static readonly (float r, float g, float b) SkyHeavy = (0.645f, 0.689f, 0.785f);

        private static (float, float, float) Scale((float r, float g, float b) c, float k)
            => (c.r * k, c.g * k, c.b * k);

        /// <summary>
        /// ⚠️⚠️ SHADOWS WERE NOT BEING DRAWN AT ALL, AND THAT IS WHY THE ROAD WOULD NOT RESPOND
        /// TO ANY SHADOW SETTING. Round two switched the key light between Soft and None and
        /// dropped `shadowStrength` to 0.15, and every frame came back byte-identical on the
        /// road band. A light whose shadows are already off cannot be turned further off. In
        /// batch mode the active quality level is whatever the project's default is, and this
        /// project's has shadows disabled — so a street with a 39-degree sun and two rows of
        /// two-storey houses had not a single cast shadow in it, which is most of why the same
        /// asphalt reads flat here and modelled in the Godot frame.
        /// </summary>
        private static void Quality()
        {
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowDistance = 120.0f;
            QualitySettings.shadowCascades = 4;

            Shadows(true, 0.62f);
        }

        private static void Key(float intensity)
        {
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) light.intensity = intensity;
        }

        /// <summary>The chain of names up to the scene root, so a hit names its LAYER as well as
        /// its mesh. `EnvColourPass` classifies by the layer, so that is the half that matters.
        /// </summary>
        private static string NodePath(Transform t)
        {
            var parts = new System.Collections.Generic.List<string>();

            for (var p = t; p != null; p = p.parent) parts.Add(p.name);
            parts.Reverse();

            return string.Join("/", parts);
        }

        private static void Ambient((float r, float g, float b) linear)
        {
            // ⚠️ THE LINEAR VALUE IS WRITTEN THROUGH `.gamma`, the same round trip
            // `TscnImporter.Energised` performs, because this field converts on assignment.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight =
                new Color(linear.r, linear.g, linear.b, 1.0f).gamma;
        }

        private static void Shadows(bool on, float strength)
        {
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional) continue;

                light.shadows = on ? LightShadows.Soft : LightShadows.None;
                light.shadowStrength = strength;
            }
        }

        [UnityTest]
        public IEnumerator TheAmbientCurveIsPhotographed()
        {
            Directory.CreateDirectory(OutDir);

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 20; i++) yield return null;

            Assert.IsNotNull(GameServices.Round, "The arena registered no round.");

            var cam = Camera.main;
            Assert.IsNotNull(cam, "No main camera in the arena.");

            // ⚠️⚠️ WHAT THE BAND IS LOOKING AT, BEFORE ANY MORE OF IT IS TUNED. Round two showed
            // the road ignoring the shadow settings completely while sitting 1.7x under the
            // reference, which no lighting term explains — so the next question is not "how much
            // light" but "what surface". The band is a rectangle of screen; a raycast through it
            // is the only thing that says which renderer and which albedo is actually under it.
            // Guessing that from the scene hierarchy is how `EnvColourPass` ended up with a
            // group table that matches the dressing and not the street.
            var report = new System.Text.StringBuilder();

            foreach (var (name, x, y) in new[]
            {
                ("sky", 0.75f, 0.11f), ("buildings", 0.50f, 0.42f), ("road", 0.50f, 0.66f),
            })
            {
                // Screen space has its origin at the BOTTOM left and the bands are measured from
                // the top, so the y is flipped here rather than in the band table.
                var ray = cam.ScreenPointToRay(
                    new Vector3(Screen.width * x, Screen.height * (1.0f - y), 0.0f));

                if (!Physics.Raycast(ray, out var hit, 400.0f, ~0,
                                     QueryTriggerInteraction.Ignore))
                {
                    report.AppendLine($"{name,-10} nothing hit (sky)");
                    continue;
                }

                var renderer = hit.collider.GetComponentInParent<Renderer>();
                var material = renderer != null ? renderer.sharedMaterial : null;

                string albedo = "n/a";

                if (material != null)
                {
                    foreach (string property in new[] { "_BaseColor", "_Color", "baseColorFactor" })
                    {
                        if (!material.HasProperty(property)) continue;

                        Color c = material.GetColor(property);
                        albedo = $"{property} stored({c.r:0.000},{c.g:0.000},{c.b:0.000}) " +
                                 $"srgb({c.gamma.r:0.000},{c.gamma.g:0.000},{c.gamma.b:0.000})";
                        break;
                    }
                }

                report.AppendLine($"{name,-10} {hit.distance:0.0} m  '{hit.collider.name}' " +
                                  $"under '{NodePath(hit.collider.transform)}'  " +
                                  $"shader {(material == null ? "none" : material.shader.name)}  " +
                                  $"{albedo}");
            }

            File.WriteAllText("Logs/band-probe.txt", report.ToString());
            Debug.Log("[Tone] band probe\n" + report);

            foreach (var (label, apply) in Setups)
            {
                // Every setup starts from the map's own shadow settings, so a variant that does
                // not touch them is measuring the shipped state rather than the previous
                // variant's leftovers.
                Shadows(true, 0.62f);
                apply();

                yield return null;
                yield return null;

                yield return Shoot(cam, label);
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE SAME RENDER PATH `GameplayShots` USES, AND THE FIRST VERSION OF THIS FILE FELL
        /// INTO BOTH OF THE TRAPS THAT FUNCTION'S COMMENTS ALREADY DOCUMENT. An ARGB32 target
        /// overrides `Camera.allowHDR`, which clamps the frame before `ColourGrade`'s roll-off
        /// ever sees it; and `ReadPixels` out of an HDR (linear) target into a PNG applies no
        /// transfer function, so the file is a stop and a half dark. A sweep photographed
        /// through either of those is a sweep of the harness.
        ///
        /// ⚠️ AND NO `WaitForEndOfFrame`. It never resumes under `-batchmode`, which is why the
        /// first run of this test sat in the scene until it was killed rather than failing.
        /// </summary>
        private static IEnumerator Shoot(Camera cam, string name)
        {
            bool hdr = cam.allowHDR;

            var rt = new RenderTexture(1920, 1080, 24,
                hdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32,
                hdr ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default);

            var previous = cam.targetTexture;
            cam.targetTexture = rt;

            yield return null;
            yield return null;

            Canvas.ForceUpdateCanvases();
            cam.Render();

            var resolved = hdr
                ? RenderTexture.GetTemporary(1920, 1080, 0, RenderTextureFormat.ARGB32,
                                             RenderTextureReadWrite.sRGB)
                : null;

            if (resolved != null) Graphics.Blit(rt, resolved);

            var active = RenderTexture.active;
            RenderTexture.active = resolved != null ? resolved : rt;

            var shot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            shot.Apply();

            RenderTexture.active = active;
            cam.targetTexture = previous;

            if (resolved != null) RenderTexture.ReleaseTemporary(resolved);
            rt.Release();
            Object.DestroyImmediate(rt);

            File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), shot.EncodeToPNG());
            Object.DestroyImmediate(shot);
        }
    }
}
