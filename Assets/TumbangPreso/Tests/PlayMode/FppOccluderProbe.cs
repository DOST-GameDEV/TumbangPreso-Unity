using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Names whatever is drawing in front of the first-person camera, because reading the source
    /// twice has now produced two wrong answers.
    ///
    /// ⚠️⚠️ 🧑 2026-08-28 PHOTOGRAPHED A LARGE CYAN CARD READING "CHESKA / zack" FILLING THE RIGHT
    /// OF THE FRAME, moving with the head bob. Two diagnoses were reasoned from source and both
    /// were wrong: first a post-process viewport clip, then the nameplate's own `mine` gate. The
    /// thing this probe exists to stop is a third guess. It reports what is ACTUALLY there.
    ///
    /// ⚠️ IT LISTS RENDERERS, NOT JUST THE ONES A THEORY PREDICTS. `ApplyFppSelfHide` sets
    /// `ShadowsOnly` on every renderer under the bound character AT BIND TIME, so anything
    /// attached to that body LATER keeps drawing and is invisible to a code reading of the hide
    /// path. The `hiddenByRig` column is what separates "the hide missed it" from "it is not under
    /// the character at all".
    ///
    /// ⚠️ AND IT PRINTS THE FULL HIERARCHY PATH. "NameplateLabel" appears under a seat, under a
    /// viewmodel and under a preview; only the path says which one is in the way.
    /// </summary>
    public sealed class FppOccluderProbe
    {
        /// <summary>How far in front of the eye still counts as "in your face". The FPP eye sits
        /// at 1.25 m and the nameplate label hangs at about 1.8 m, so anything inside 2 m is
        /// either the player's own body or something wrongly parented to it.</summary>
        private const float NearMetres = 2.0f;

        [UnityTest]
        [Category("WallClock")]
        public IEnumerator NameWhatIsDrawingInFrontOfTheFppCamera()
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

            Assert.IsNotNull(rig, "no active CameraRig in the arena, so there is no FPP view to probe");

            var cam = rig.Camera;
            var eye = cam.transform.position;

            var report = new StringBuilder();
            report.AppendLine("=== FPP OCCLUDER PROBE ===");
            report.AppendLine($"rig.IsLocalFpp={rig.IsLocalFpp}");
            report.AppendLine($"rig.Following={(rig.Following == null ? "NULL" : rig.Following.name)}");
            report.AppendLine($"eye={eye}");
            report.AppendLine();

            // Everything the rig believes it hid, so a missing entry is meaningful.
            var underCharacter = new HashSet<Renderer>();

            if (rig.Following != null)
            {
                foreach (var r in rig.Following.GetComponentsInChildren<Renderer>(true))
                    underCharacter.Add(r);
            }

            var near = new List<(float d, string line)>();

            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;

                float d = Vector3.Distance(eye, r.bounds.center);
                if (d > NearMetres) continue;

                // ⚠️ SHADOWS-ONLY IS THE HIDDEN STATE, NOT `enabled == false`. `ApplyFppSelfHide`
                // deliberately keeps the renderer on so the player keeps their own ground shadow,
                // so a renderer that is "hidden" is still enabled and still has bounds.
                bool hidden = r.shadowCastingMode == ShadowCastingMode.ShadowsOnly;
                if (hidden) continue;

                near.Add((d, $"  {d,5:F2}m  underCharacter={underCharacter.Contains(r),-5}  " +
                             $"{r.GetType().Name,-20} {Path(r.transform)}"));
            }

            near.Sort((a, b) => a.d.CompareTo(b.d));

            report.AppendLine($"VISIBLE renderers within {NearMetres} m of the eye: {near.Count}");
            foreach (var n in near) report.AppendLine(n.line);

            Debug.Log(report.ToString());

            // ⚠️ THIS IS A PROBE, NOT A GATE. It prints and passes. Turning it into an assertion
            // would need a number nobody has measured yet, and a red test whose bound was invented
            // teaches the next session to raise the bound.
            Assert.Pass();
        }

        private static string Path(Transform t)
        {
            var parts = new List<string>();

            while (t != null)
            {
                parts.Add(t.name);
                t = t.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
