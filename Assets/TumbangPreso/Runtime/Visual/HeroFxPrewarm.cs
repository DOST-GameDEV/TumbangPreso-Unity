using System;
using System.Collections;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Builds each reworked hero effect once, offscreen, while the loading curtain is up, draws it through the match camera,
    /// and frees it: so the FIRST whirlwind, drift, quick circuit, boulder and landing puff of a match are not the ones that
    /// load their pictures, compile their code and build their pipeline states in the middle of a round.
    ///
    /// ⚠️ WHY. Measured in the editor (`cloudcourt audit`): the first cast of an effect in a session cost 10 to 14 ms, about
    /// four times a later one, because that is when its textures are loaded, its code is compiled and its shader meets the
    /// real camera for the first time. At 60 frames a second a frame is 16.7 ms, so the first whirlwind of a match was a
    /// visible hitch. The owner, of all of the effects: "remember this needs to be heavily optimized".
    ///
    /// ⚠️ THE SHADERS ARE ALREADY IN THE WARM-UP COLLECTION (`ShaderWarmupCollection` takes every `.shader` in `Assets/`).
    /// What a collection cannot do is load an effect's textures, run its code once, or build the pipeline state for the
    /// real mesh layout and target, which is `ArenaPrewarm`'s argument for drawing the real scene. This is the same for the
    /// effects that only exist once somebody casts: each is built with the very calls a cast makes, drawn once into a
    /// target of the camera's own format, one per loading frame, and thrown away. It changes nothing in any of them.
    ///
    /// ⚠️ NOTHING OF IT IS SEEN AND NOTHING OF IT IS KEPT, except the match's one field of landing puffs, which is made
    /// here (empty) so that the first landing does not build it.
    /// </summary>
    public static class HeroFxPrewarm
    {
        /// <summary>How many loading frames <see cref="Run"/> takes. Used for progress.</summary>
        public const int Steps = 5;

        /// <summary>The effects, one a frame. `camera` null finds the match camera as `ArenaPrewarm` does.</summary>
        public static IEnumerator Run(Action<float> progress, Camera camera = null)
        {
            if (camera == null) camera = FindCamera();
            var root = new GameObject("HeroFxPrewarm") { hideFlags = HideFlags.DontSave };
            RenderTexture target = null;
            float began = Time.realtimeSinceStartup;
            try
            {
                Vector3 at = Vector3.zero, eye = at + new Vector3(0f, 1.6f, -6f);
                if (camera != null)
                {
                    Vector3 ahead = camera.transform.forward; ahead.y = 0f;
                    ahead = ahead.sqrMagnitude > 1e-4f ? ahead.normalized : Vector3.forward;
                    at = camera.transform.position + ahead * 6f;
                    at.y = VfxShapes.GroundAt(at, camera.transform.position.y - 1.6f, 8f);
                    eye = camera.transform.position;
                    int width = Mathf.Max(320, Screen.width / 2), height = Mathf.Max(180, Screen.height / 2);
                    target = new RenderTexture(width, height, 24, camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default)
                    {
                        name = "HeroFxPrewarm",
                        antiAliasing = camera.allowMSAA ? Mathf.Max(1, QualitySettings.antiAliasing) : 1,
                    };
                }
                for (int step = 0; step < Steps; step++)
                {
                    var holder = new GameObject("step" + step) { hideFlags = HideFlags.DontSave };
                    holder.transform.SetParent(root.transform, false);
                    holder.transform.position = at;
                    Action free = null;
                    try { free = Build(step, holder.transform, at, eye); }
                    catch (Exception e) { Debug.LogWarning("[HeroFxPrewarm] step " + step + " could not be warmed: " + e.Message); }
                    Draw(camera, target);
                    try { free?.Invoke(); } catch (Exception e) { Debug.LogWarning("[HeroFxPrewarm] step " + step + " could not be freed: " + e.Message); }
                    Kill(holder);
                    progress?.Invoke((step + 1) / (float)Steps);
                    yield return null;
                }
            }
            finally
            {
                if (target != null) { target.Release(); Kill(target); }
                Kill(root);
                Debug.Log($"[HeroFxPrewarm] warmed {Steps} hero effects in {Time.realtimeSinceStartup - began:F2} s.");
            }
        }

        /// <summary>Builds step `step`'s effect under `holder`, posed at a busy moment, and says how to free it.</summary>
        private static Action Build(int step, Transform holder, Vector3 at, Vector3 eye)
        {
            switch (step)
            {
                case 0:
                {
                    // Amihan's WHIRLWIND, as the gale stands it under its front.
                    var wind = AmihanWhirlwind.Build(holder, 2.5f);
                    wind.Pose(1.0f);
                    return wind.Free;
                }
                case 1:
                {
                    // Her DRIFT, as the dash builds it.
                    var wake = AmihanDashWake.Build(at, at + Vector3.forward * 4f, holder, null);
                    wake.StepTo(0.3f);
                    return () => Kill(wake.gameObject);
                }
                case 2:
                {
                    // Zack's LIVE WIRE, overcharged and humming, as the field's own look builds it (render only).
                    var a = at + Vector3.left * 2f; var b = at + Vector3.right * 2f;
                    var wire = new Net.WorldEffectSnapshot.Field { Type = Net.WorldEffectSnapshot.Kind.LiveWire, EventId = 1,
                        Position = a, Forward = Vector3.right, Radius = 4f, Duration = Core.ZackRules.LiveWireLifeBound,
                        Remaining = Core.ZackRules.LiveWireLifeBound, SecondScale = 1f, Path = new[] { a, b } };
                    var look = ZackLiveWireLook.Build(holder, wire, renderOnly: true);
                    look.StepTo(1.0f);
                    return () => Kill(look.gameObject);
                }
                case 3:
                {
                    // Dante's BOULDER: its modelled stone, its cast and its hit, as `DanteBoulderMoment` stands them.
                    var stone = ReworkProp.Spawn("boulder", holder, ReworkProp.GeoPalette);
                    var held = new Vector3(0f, 1f, 0f);
                    var cast = new FxStudyDanteBoulder(holder, held, held + new Vector3(4.2f, 0f, 0f), false, 1f, 0.6f, FxStudyDanteBoulder.PartCast);
                    cast.Sample(FxStudyDanteBoulder.CastMoment + 0.2f, eye);
                    var hit = new FxStudyDanteBoulder(holder, held + new Vector3(-4.2f, 0.05f, 0f), held, false, 1f, 0.6f, FxStudyDanteBoulder.PartHit);
                    hit.Sample(FxStudyDanteBoulder.HitMoment + 0.1f, eye);
                    return () => { cast.Dispose(); hit.Dispose(); if (stone != null) Kill(stone); };
                }
                default:
                {
                    // The LANDING PUFFS: the match's field made now, empty, and a field of the same kind drawn with a puff in it.
                    CloudPuffs.Prepare();
                    var field = new CloudPuffField(holder, "prewarm-puffs");
                    field.Spawn(CloudPuffField.Puff.Landing(at), 0f);
                    field.Hold(0.15f);
                    return field.Dispose;
                }
            }
        }

        private static void Draw(Camera camera, RenderTexture target)
        {
            if (camera == null || target == null) return;
            var keep = camera.targetTexture; float aspect = camera.aspect;
            try { camera.targetTexture = target; camera.aspect = target.width / (float)target.height; camera.Render(); }
            finally { camera.targetTexture = keep; camera.aspect = aspect; }
        }

        private static Camera FindCamera()
        {
            var main = Camera.main;
            if (main != null && main.isActiveAndEnabled && main.targetTexture == null) return main;
            foreach (var camera in Camera.allCameras)
                if (camera.targetTexture == null) return camera;
            return null;
        }

        private static void Kill(UnityEngine.Object thing)
        {
            if (thing == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(thing); else UnityEngine.Object.DestroyImmediate(thing);
        }
    }
}
