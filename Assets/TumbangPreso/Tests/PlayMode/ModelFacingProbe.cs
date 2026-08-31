using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// WHICH WAY DOES THE RIG FACE?
    ///
    /// ⚠️⚠️ THE GODOT REPO ANSWERS THIS WITH A CONSTANT AND A PROBE, AND THE PORT CARRIED
    /// NEITHER. `character_visual.gd:212` holds `PERSON_MODEL_YAW_DEG = 180.0` and applies it to
    /// the instanced model, and its own header records the cost of not having it: *"the attacker
    /// spawns facing backward, reported across more than ten sessions"*, chased through the yaw
    /// maths every time, when the fault was that the rig wears its face on the wrong axis. Its
    /// note ends by naming `tools/model_facing_probe.tscn` as the thing that finally measured it
    /// rather than inferring it.
    ///
    /// This is that probe. A seat is pinned to yaw 0, and two cameras photograph it from +Z and
    /// -Z. Whichever picture has the eyes in it is the direction the model's face points, and
    /// that is the only fact needed to decide the constant.
    /// </summary>
    public class ModelFacingProbe
    {
        private const string OutDir = "Logs/shots-facing";

        [UnityTest]
        public IEnumerator ARigIsPhotographedFromBothSides()
        {
            Directory.CreateDirectory(OutDir);

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < 25; i++) yield return null;

            CharacterMotor who = null;

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                // Not the local seat: its body renders ShadowsOnly for the first-person view
                // and would photograph as an empty street.
                var r = m.GetComponentInChildren<SkinnedMeshRenderer>();
                if (r == null || r.shadowCastingMode ==
                    UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) continue;

                who = m;
                break;
            }

            Assert.IsNotNull(who, "no visible seat to photograph");

            // The AI would re-steer the body (Steer() re-derives rotation from the move axis)
            // within the next few frames, undoing the pin below before the shot is taken.
            var ai = who.GetComponent<AIController>();
            if (ai != null) ai.enabled = false;

            // Pinned to yaw 0, so "+Z" in the file name means the world axis and nothing else.
            who.transform.rotation = Quaternion.identity;

            for (int i = 0; i < 3; i++) yield return null;

            Vector3 at = who.transform.position + new Vector3(0.0f, 0.95f, 0.0f);

            yield return Shot(at + new Vector3(0.0f, 0.0f, 3.2f), at, "from-plus-z");
            yield return Shot(at + new Vector3(0.0f, 0.0f, -3.2f), at, "from-minus-z");

            // ⚠️⚠️ THE PICTURES ARE THE MEASUREMENT, RETAKEN 2026-08-18. `PersonModelYaw = 180`
            // was reported as "cans are floating [and] when you emote, the character turns
            // around instead of the original view", and the emote camera opens BEHIND the body
            // by design (see CameraRig.BeginEmoteView). A screenshot of that real camera showed
            // the FACE, not the back of the head — the model was mounted backward.
            //
            // Retaking this exact photograph pair against a single pinned, AI-disabled seat
            // (the earlier version of this probe picked "the first visible bot", whose AI could
            // re-steer it, i.e. rotate the body, in the frames between the pin and the shot,
            // which is why this had been misread as -Z once before) showed the OPPOSITE of what
            // the old comment here recorded: from-plus-z is the FACE and from-minus-z is the
            // BACK. So `Model.transform.forward` (the object's own +Z after whatever correction
            // is applied) must be ALIGNED with the body's forward, not opposite it, and
            // `PersonModelYaw` is 0, not 180. Asserting the relationship rather than the
            // constant means a future rig with its face on the other axis fails here instead of
            // shipping backwards again.
            var visual = who.GetComponentInChildren<Visual.CharacterVisual>();
            Assert.IsNotNull(visual, "the seat has no CharacterVisual");
            Assert.IsNotNull(visual.Model, "the seat has no instanced model");

            Assert.Greater(Vector3.Dot(visual.Model.transform.forward, who.transform.forward), 0.9f,
                "The model is not turned to face the way the body walks. " +
                $"CharacterVisual.PersonModelYaw is {Visual.CharacterVisual.PersonModelYaw}; " +
                "see Logs/shots-facing for which way this rig's face actually points.");
        }

        private static IEnumerator Shot(Vector3 from, Vector3 at, string name)
        {
            var go = new GameObject("FacingCam");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 40.0f;
            cam.nearClipPlane = 0.05f;

            cam.transform.position = from;
            cam.transform.LookAt(at);

            var rt = new RenderTexture(960, 720, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            yield return null;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(960, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            File.WriteAllBytes($"{OutDir}/{name}.png", tex.EncodeToPNG());

            // ⚠️ THE CAMERA LETS GO OF THE TARGET BEFORE THE TARGET IS RELEASED, or Unity logs
            // an error and the test runner fails the case on the log alone.
            cam.targetTexture = null;

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);

            Debug.Log($"[Facing] wrote {OutDir}/{name}.png");
        }
    }
}
