using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// ⚠️⚠️ MARIANG MAKILING, LOOKED AT BEFORE SHE IS FILMED (HERO-9, 2026-09-26). The owner on the first
    /// in-match film: *"how she looks needs to be refined"*, then *"pls imporv ehow the girl in his cutscene
    /// looks too"*. A spirit seen for a second inside a cut cannot be judged, so this photographs her whole on
    /// the court the film uses (Bayan Plaza): as the ghost the cutscene draws (`SpiritGhost.shader`), and
    /// SOLID in her own palette under the toon shader, because a translucent render hides a bad form and the
    /// solid one shows it. Paete stands in front of her in his cupped-hands pose from the introduction's own
    /// table, so her size and her place over his shoulder are judged against him, from the CALL shot's angle.
    ///
    /// ⚠️ v4 (2026-09-26 night, direction.md 5.14): her FULL FORM as the shader now draws it (forming, formed, turning back: the
    /// owner's *"she sstarts translucent to full forma nd translucent again"*), and HER MEADOW (`PaeteMeadow`, `meadow.glb`): grown,
    /// at the instant of his palms' touch (the makahiya folded), and wilting from its edge in, so the models are judged whole
    /// before the cutscene hides them in motion.
    ///
    /// Nothing casts an ability or touches a rule; the scene is never saved. Output:
    /// `Logs/paete-review/paete_makiling_<Version>.png` and `paete_meadow_<Version>.png`, versioned (`CLAUDE.md` § 6.1).
    ///   python tools/run_unity_guarded.py -batchmode -tp-profile presentation-validation-20260921 -executeMethod TumbangPreso.EditorTools.MapKit.PaeteSpiritReviewProbe.Run -logFile Logs/paete-spirit.log
    /// </summary>
    public static class PaeteSpiritReviewProbe
    {
        public const string Version = "v5";
        private const string OutDir = "Logs/paete-review";
        private const string Plaza = "Assets/TumbangPreso/Scenes/Maps/BayanPlaza.unity";
        private const int W = 640, H = 480;

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        public static bool Execute()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.OpenScene(Plaza, OpenSceneMode.Single);
            foreach (var pass in Object.FindObjectsByType<EnvColourPass>(FindObjectsInactive.Exclude)) pass.Apply();
            var baseline = new HashSet<GameObject>(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects());
            try
            {
                var art = RosterBook.Load().FindPersonArt("paete");
                if (art == null || art.Model == null) throw new InvalidOperationException("Paete's roster art is missing.");
                // Where the film stands him: the plaza's open court, facing +Z.
                var host = new GameObject("SpiritReviewHost");
                host.transform.position = new Vector3(0f, GroundAt(Vector3.zero), -6f);

                var body = (GameObject)Object.Instantiate(art.Model, host.transform);
                body.name = "PaeteSpiritReviewBody";
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.Euler(0, CharacterVisual.PersonModelYaw, 0);
                body.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                ToonSkin.Apply(body, ToonSkin.PersonOutlineWidth, art.Palette);
                foreach (var animator in body.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                foreach (var skin in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                { skin.forceMatrixRecalculationPerRender = true; skin.updateWhenOffscreen = true; }
                var clip = HeroAbilityClips.BuildUltimateIntroduction(body.transform, "paete");
                if (clip == null) throw new InvalidOperationException("No Paete introduction clip.");

                var shots = new List<Texture2D>();
                // Four rows of her: the ghost as she gives him the light (the CALL), FORMING (the solid line half way up her),
                // her FULL FORM (her own colours, the cast's two bands, her ink), and TURNING BACK to spirit from the feet.
                var calling = new MakilingSpirit.Look { Presence = 1f, Lean = 18f, Bow = 20f, Turn = -12f, Reach = 0.5f, Light = 1f, Wind = 0.3f };
                (string who, float form, float unform)[] rows = { ("ghost calling", 0f, 0f), ("forming", 0.55f, 0f), ("full form", 1f, 0f), ("turning back", 1f, 0.5f) };
                foreach (var row in rows)
                {
                    var spirit = new MakilingSpirit(host.transform, SpiritAt, SpiritYaw, SpiritScale);
                    Pose(body.transform, clip, 0.66f);
                    var look = calling; look.Form = row.form; look.Unform = row.unform;
                    spirit.Pose(0.66f, look);
                    string who = row.who;
                    var her = host.transform.TransformPoint(SpiritAt);
                    float headY = her.y + 2.5f * SpiritScale;
                    // 1 THE CALL, as the redirect frames it: front, low three-quarter from his right, her head over his.
                    shots.Add(Shoot(host.transform.TransformPoint(new Vector3(1.7f, .8f, 4.1f)), host.transform.TransformPoint(new Vector3(.35f, 1.9f, -.4f)), 42, $"{who} call shot"));
                    // 2 Her, whole, from the front.
                    shots.Add(Shoot(her + host.transform.rotation * new Vector3(0f, 1.9f, 7.4f), her + Vector3.up * 1.75f, 40, $"{who} front"));
                    // 3 Her face, close.
                    shots.Add(Shoot(her + host.transform.rotation * new Vector3(.2f, 2.9f, 1.9f), new Vector3(her.x, headY, her.z), 36, $"{who} face"));
                    // 4 From her left, whole.
                    shots.Add(Shoot(her + host.transform.rotation * new Vector3(-6.8f, 2.1f, 1.6f), her + Vector3.up * 1.75f, 40, $"{who} side"));
                    // 5 The ROOT shot's angle (v5): close and low on his front right, her in close behind him.
                    var close = new Vector3(0.05f, 0f, -1.15f);
                    spirit.Model.transform.localPosition = close;
                    Pose(body.transform, clip, 1.9f);
                    var bent = look; bent.Lean = 24f; bent.Bow = 30f; bent.Reach = 0.34f; bent.Open = 0.15f; bent.Light = 0f;
                    spirit.Pose(1.9f, bent);
                    shots.Add(Shoot(host.transform.TransformPoint(new Vector3(2.2f, 1.2f, 3.2f)), host.transform.TransformPoint(new Vector3(.02f, 1.25f, .3f)), 48, $"{who} root shot"));
                    if (spirit.Model != null) Object.DestroyImmediate(spirit.Model);
                }
                Save("makiling", shots, 5);

                // HER MEADOW: grown (the wave done), at his palms' touch (the makahiya folded shut), and wilting from its edge in.
                var meadowShots = new List<Texture2D>();
                var hands = new Vector3(0f, 0f, 1.0f);
                (string who, float t)[] stages = { ("grown", 2.0f), ("the touch", 99.12f), ("wilting", 4.2f) };
                foreach (var stage in stages)
                {
                    var meadow = new PaeteMeadow(host.transform, SpiritAt, hands, 0f);
                    // The touch is posed as its own instant (slam at 99), fully grown.
                    if (stage.t > 90f) meadow.Pose(stage.t, 99f, null, 200f, 201f, new Vector3(0f, 0f, 5.5f));
                    else meadow.Pose(stage.t, 1.22f, new[] { 3.15f, 3.55f, 3.95f }, 3.6f, 4.5f, new Vector3(0f, 0f, 5.5f));
                    Pose(body.transform, clip, stage.t > 90f ? 1.3f : 0.3f);
                    meadowShots.Add(Shoot(host.transform.TransformPoint(new Vector3(2.1f, .9f, 4.6f)), host.transform.TransformPoint(new Vector3(.6f, .5f, -.5f)), 46, $"meadow {stage.who} call angle"));
                    meadowShots.Add(Shoot(host.transform.TransformPoint(new Vector3(.8f, 6.5f, .6f)), host.transform.TransformPoint(new Vector3(.8f, 0f, -.4f)), 50, $"meadow {stage.who} from above"));
                    meadowShots.Add(Shoot(host.transform.TransformPoint(new Vector3(1.0f, .55f, -.9f)), host.transform.TransformPoint(new Vector3(1.72f, .15f, -1.88f)), 40, $"meadow {stage.who} fern and bush"));
                    meadowShots.Add(Shoot(host.transform.TransformPoint(new Vector3(2.75f, .55f, 1.75f)), host.transform.TransformPoint(new Vector3(2.1f, .12f, 1.0f)), 40, $"meadow {stage.who} makahiya"));
                    meadowShots.Add(Shoot(host.transform.TransformPoint(new Vector3(2.9f, .75f, -3.3f)), host.transform.TransformPoint(new Vector3(2.2f, .2f, -2.2f)), 44, $"meadow {stage.who} gumamela, sampaguita"));
                    meadow.Dispose();
                }
                Save("meadow", meadowShots, 5);
                Object.DestroyImmediate(clip);
                Debug.Log("[PaeteSpiritReviewProbe] wrote " + Version);
                return true;
            }
            catch (Exception error) { Debug.LogException(error); return false; }
            finally
            {
                foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                    if (!baseline.Contains(root)) Object.DestroyImmediate(root);
            }
        }

        // Her place in the introduction (`HeroIntroductionScene.Paete`), kept in step by hand while both are being
        // redirected: behind his RIGHT shoulder (direction.md 5.13).
        private static readonly Vector3 SpiritAt = new Vector3(1.0f, 0f, -1.2f);
        private const float SpiritYaw = -14f, SpiritScale = 1.1f;

        private static float GroundAt(Vector3 at)
            => Physics.Raycast(at + Vector3.up * 30f, Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;

        /// <summary>Every curve evaluated and written by hand (`PaeteReviewProbe.Pose`'s reason).</summary>
        private static void Pose(Transform root, AnimationClip clip, float t)
        {
            var euler = new Dictionary<Transform, Vector3>(); var position = new Dictionary<Transform, Vector3>();
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var bone = string.IsNullOrEmpty(binding.path) ? root : root.Find(binding.path);
                if (bone == null) continue;
                float v = AnimationUtility.GetEditorCurve(clip, binding).Evaluate(t);
                string prop = binding.propertyName;
                int axis = prop.EndsWith(".x") ? 0 : prop.EndsWith(".y") ? 1 : 2;
                if (prop.StartsWith("localEulerAngles"))
                {
                    if (!euler.TryGetValue(bone, out var e)) e = bone.localEulerAngles;
                    e[axis] = v; euler[bone] = e;
                }
                else if (prop.StartsWith("m_LocalPosition") || prop.StartsWith("localPosition"))
                {
                    if (bone == root) continue; // the grounding root key would lift the whole host
                    if (!position.TryGetValue(bone, out var p)) p = bone.localPosition;
                    p[axis] = v; position[bone] = p;
                }
            }
            foreach (var kv in euler) kv.Key.localEulerAngles = kv.Value;
            foreach (var kv in position) kv.Key.localPosition = kv.Value;
        }

        private static Texture2D Shoot(Vector3 eye, Vector3 look, float fov, string label)
        {
            var camGo = new GameObject("~SpiritReviewCam");
            camGo.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = .05f; cam.farClipPlane = 400; cam.clearFlags = CameraClearFlags.Skybox;
            cam.allowHDR = true;
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
            sheet.SetPixels32(new Color32[sheet.width * sheet.height]);
            for (int i = 0; i < shots.Count; i++)
            {
                int x = (i % columns) * W, y = (rows - 1 - i / columns) * H;
                sheet.SetPixels(x, y, W, H, shots[i].GetPixels());
                Object.DestroyImmediate(shots[i]);
            }
            sheet.Apply();
            string path = Path.Combine(OutDir, $"paete_{name}_{Version}.png");
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            Debug.Log("[PaeteSpiritReviewProbe] " + path);
        }
    }
}
