using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// ⚠️⚠️ AMIHAN'S REVIEW FILMSTRIPS (2026-09-25). The owner: *"everytime u finish an iteration
    /// watch it all and berate it THen improve it even more"*. A single still of a transient says
    /// almost nothing about wind, which IS motion, so every subject here is a strip of frames
    /// across its whole life: her body cast (the real authored clip on the real model) and the
    /// effect on the same clock, the body travelling where the gameplay moves it. The cutscene is
    /// shot through its own authored cameras (`HeroIntroductionScene.ShotAt`), frame by frame.
    ///
    /// Edit mode, on Ilalim ng Tulay, like `AbilityShowcaseProbe`: nothing here casts an ability or
    /// touches a rule; it calls the same builders the game does and winds them with `StepTo`.
    /// Output: `Logs/amihan-review/<subject>_<Version>.png`, versioned because chat clients cache
    /// by filename (`CLAUDE.md` § 6.1).
    /// </summary>
    public static class AmihanReviewProbe
    {
        public const string Version = "v1";
        private const string OutDir = "Logs/amihan-review";
        private const int W = 640, H = 360;

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        [MenuItem("Tumbang Preso/Amihan Review Filmstrips")]
        public static void RunFromMenu() => Execute();

        public static bool Execute()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.OpenScene(IlalimNgTulayBuilder.ScenePath, OpenSceneMode.Single);
            foreach (var pass in Object.FindObjectsByType<EnvColourPass>(FindObjectsInactive.Exclude))
                pass.Apply();
            var baseline = new HashSet<GameObject>(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects());
            try
            {
                var art = RosterBook.Load().FindPersonArt("amihan");
                if (art == null || art.Model == null) throw new InvalidOperationException("Amihan's roster art is missing.");

                Strip("dash", art.Model, "hero-amihan-dash", 0.95f, 10,
                    (t, body) =>
                    {
                        body.transform.position = new Vector3(0, 0, DashTravel(t));
                        return null;
                    },
                    () => AmihanDashWake.Build(Vector3.zero, new Vector3(0, 0, AmihanRules.QuickDashDistance)).gameObject,
                    new Vector3(4.6f, 1.7f, 2.2f), new Vector3(0, 1.0f, 2.6f), 55);

                Strip("updraft", art.Model, "hero-amihan-updraft", 1.0f, 10,
                    (t, body) =>
                    {
                        body.transform.position = new Vector3(0, Mathf.Clamp01(t / AmihanRules.UpdraftRiseSeconds) * AmihanRules.UpdraftHeight, 0);
                        return null;
                    },
                    () => AmihanUpdraftLaunch.Build(Vector3.zero, AmihanRules.UpdraftHeight).gameObject,
                    new Vector3(3.6f, 2.2f, 4.6f), new Vector3(0, 2.0f, 0), 60);

                Strip("whirlwind", art.Model, "hero-amihan-whirlwind", AmihanRules.WhirlwindSeconds, 10,
                    (t, body) => null,
                    () => AmihanGaleFrontHost(),
                    new Vector3(5.5f, 3.6f, -3.0f), new Vector3(0, 0.6f, 5.0f), 62);

                Strip("storm", art.Model, "hero-amihan-storm", AmihanRules.StormSurgeGatherSeconds + AmihanStormFan.WallSeconds + 0.3f, 12,
                    (t, body) => null,
                    () => AmihanStormFan.Build(null, Vector3.zero, Vector3.forward, AmihanRules.StormSurgeGatherSeconds).gameObject,
                    new Vector3(2.2f, 5.0f, -6.0f), new Vector3(0, 0.0f, 8.0f), 64);

                Hit(art.Model);
                Introduction(art.Model);
                Debug.Log("[AmihanReviewProbe] wrote " + Version + " to " + OutDir);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                return false;
            }
            finally
            {
                foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                    if (!baseline.Contains(root)) Object.DestroyImmediate(root);
            }
        }

        private static GameObject AmihanGaleFrontHost()
        {
            var host = new GameObject("GaleHost");
            AmihanGaleFront.Build(host.transform, Vector3.zero, Vector3.forward, AmihanRules.WhirlwindSeconds,
                AmihanRules.WhirlwindSpeed, AmihanRules.WhirlwindStart, AmihanRules.WhirlwindWidth);
            return host;
        }

        /// <summary>Where the dash has carried her at <paramref name="t"/>: the held speed, then
        /// the slide out against `Friction`, exactly the motor's arithmetic.</summary>
        private static float DashTravel(float t)
        {
            float v = AmihanRules.QuickDashSpeed, hold = AmihanRules.QuickDashHoldSeconds;
            if (t <= hold) return v * t;
            float after = Mathf.Min(t - hold, v / Balance.Friction);
            return v * hold + v * after - 0.5f * Balance.Friction * after * after;
        }

        private static GameObject Body(GameObject model, Vector3 at)
        {
            var body = (GameObject)Object.Instantiate(model);
            body.name = "AmihanReviewBody";
            body.transform.SetPositionAndRotation(at, Quaternion.identity);
            body.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
            foreach (var animator in body.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            return body;
        }

        private static AnimationClip Clip(GameObject body, string name)
        {
            var animator = body.GetComponentInChildren<Animator>(true);
            var root = animator != null ? animator.transform : body.transform;
            return HeroAbilityClips.BuildAmihanAuthored(root).FirstOrDefault(c => c.name == name);
        }

        /// <summary>
        /// A strip of <paramref name="frames"/> frames across <paramref name="seconds"/>: the body's
        /// clip sampled and the effect stepped to the same moment.
        /// </summary>
        private static void Strip(string name, GameObject model, string clipName, float seconds, int frames,
                                  Func<float, GameObject, object> place, Func<GameObject> effect,
                                  Vector3 eye, Vector3 look, float fov)
        {
            var body = Body(model, Vector3.zero);
            var clip = Clip(body, clipName);
            var fx = effect();
            var timelines = fx.GetComponentsInChildren<MonoBehaviour>(true).OfType<IVfxTimeline>().ToArray();
            var shots = new List<Texture2D>();
            for (int i = 0; i < frames; i++)
            {
                float t = seconds * i / (frames - 1);
                if (clip != null) clip.SampleAnimation(body.GetComponentInChildren<Animator>(true) != null ? body.GetComponentInChildren<Animator>(true).gameObject : body, Mathf.Min(t, clip.length));
                place(t, body);
                foreach (var timeline in timelines) timeline.StepTo(t);
                shots.Add(Shoot(eye, look, fov, $"{name} t={t:0.00}s"));
            }
            Save(name, shots, 5);
            Object.DestroyImmediate(fx);
            Object.DestroyImmediate(body);
            if (clip != null) Object.DestroyImmediate(clip);
        }

        /// <summary>The contact burst and the Whirled mark on a victim, across their lives.</summary>
        private static void Hit(GameObject model)
        {
            var victim = Body(model, new Vector3(0, 0, 2));
            var hit = AmihanWindHit.Build(new Vector3(0, 0, 2), new Vector3(1, 0, .4f));
            var shots = new List<Texture2D>();
            for (int i = 0; i < 6; i++)
            {
                float t = AmihanWindHit.Life * i / 5.0f;
                hit.StepTo(t);
                shots.Add(Shoot(new Vector3(2.6f, 1.7f, -1.4f), new Vector3(0, 1.1f, 2), 50, $"hit t={t:0.00}s"));
            }
            Save("hit", shots, 3);
            Object.DestroyImmediate(hit.gameObject);
            Object.DestroyImmediate(victim);
        }

        /// <summary>STORM SURGE's introduction through its own authored shots, frame by frame.</summary>
        private static void Introduction(GameObject model)
        {
            var sourceGo = new GameObject("AmihanIntroSource");
            sourceGo.AddComponent<CharacterController>();
            var source = sourceGo.AddComponent<CharacterMotor>();
            var body = Body(model, Vector3.zero);
            var animator = body.GetComponentInChildren<Animator>(true);
            var copy = new MatchPoseHistory.Copy(body);
            copy.ShowOnlyForCapture(true);
            var clip = HeroAbilityClips.BuildUltimateIntroduction(animator != null ? animator.transform : body.transform, "amihan");
            var stage = new GameObject("AmihanIntroStage");
            var scene = new HeroIntroductionScene(stage.transform, "amihan", source, copy);
            scene.SetVisibleForCapture(true);
            var shots = new List<Texture2D>();
            int frames = 18;
            for (int i = 0; i < frames; i++)
            {
                float t = scene.Seconds * i / (frames - 1);
                if (clip != null) clip.SampleAnimation(animator != null ? animator.gameObject : body, t);
                scene.Sample(t, audible: false);
                scene.Shot(t, out var eye, out var focus, out float fov);
                shots.Add(Shoot(eye, focus, fov, $"intro t={t:0.00}s shot {scene.ShotIndexAt(t)}"));
            }
            Save("intro", shots, 6);
            scene.Dispose();
            Object.DestroyImmediate(stage);
            Object.DestroyImmediate(body);
            Object.DestroyImmediate(sourceGo);
            if (clip != null) Object.DestroyImmediate(clip);
        }

        private static Texture2D Shoot(Vector3 eye, Vector3 look, float fov, string label)
        {
            var camGo = new GameObject("~AmihanReviewCam");
            camGo.transform.position = eye;
            camGo.transform.rotation = Quaternion.LookRotation(look - eye, Vector3.up);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = 0.05f; cam.farClipPlane = 260; cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<ColourGrade>().AdoptFromScene();
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
            RenderTexture.active = null; cam.targetTexture = null;
            rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(camGo);
            tex.name = label;
            return tex;
        }

        private static void Save(string name, List<Texture2D> shots, int columns)
        {
            int rows = (shots.Count + columns - 1) / columns;
            var sheet = new Texture2D(W * columns, H * rows, TextureFormat.RGB24, false);
            var clear = new Color32[sheet.width * sheet.height];
            sheet.SetPixels32(clear);
            for (int i = 0; i < shots.Count; i++)
            {
                int x = (i % columns) * W, y = (rows - 1 - i / columns) * H;
                sheet.SetPixels(x, y, W, H, shots[i].GetPixels());
                Object.DestroyImmediate(shots[i]);
            }
            sheet.Apply();
            string path = Path.Combine(OutDir, $"amihan_{name}_{Version}.png");
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            Debug.Log("[AmihanReviewProbe] " + path);
        }
    }
}
