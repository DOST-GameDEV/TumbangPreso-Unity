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
    /// ⚠️⚠️ PAETE'S BODY-CAST FILMSTRIPS (HERO-9, 2026-09-26). The owner: *"i want u to really lock in and
    /// give paete his own animations taht make sense wiht his shit"*, *"i want each of his skill to have
    /// their own animation"*. A still says nothing about a cast, so every clip is a strip across its
    /// whole life on the REAL model and rig (`HeroAbilityClips.BuildPaeteAuthored`, the same builder the
    /// roster bake uses), shot from the side and from the front, on Ilalim ng Tulay like
    /// `AmihanReviewProbe`. The two shared clips (`rooted-struggle`, `plant-heave`) are shot on Paete
    /// and on Sean, because every rig wears them. The vine strip moves the body along the reel the
    /// motor would, so the drag reads as a drag.
    ///
    /// Nothing casts an ability or touches a rule. Output: `Logs/paete-review/<subject>_<Version>.png`,
    /// versioned because chat clients cache by filename (`CLAUDE.md` § 6.1).
    ///   python tools/run_unity_guarded.py -batchmode -executeMethod TumbangPreso.EditorTools.MapKit.PaeteReviewProbe.Run -logFile Logs/paete-review.log
    /// </summary>
    public static class PaeteReviewProbe
    {
        public const string Version = "v24";
        private const string OutDir = "Logs/paete-review";
        private const int W = 480, H = 360;

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        /// <summary>
        /// Only the trees (the sentry, the seedling, the thorn construct) and the break-out: the pass the
        /// modelled props are refined in (direction.md section 5), without re-filming every body clip.
        ///   python tools/run_unity_guarded.py -batchmode -executeMethod TumbangPreso.EditorTools.MapKit.PaeteReviewProbe.RunTrees -logFile Logs/paete-trees.log
        /// </summary>
        public static void RunTrees() => EditorApplication.Exit(Execute(treesOnly: true) ? 0 : 1);

        public static bool Execute() => Execute(false);

        public static bool Execute(bool treesOnly)
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.OpenScene(IlalimNgTulayBuilder.ScenePath, OpenSceneMode.Single);
            foreach (var pass in Object.FindObjectsByType<EnvColourPass>(FindObjectsInactive.Exclude)) pass.Apply();
            var baseline = new HashSet<GameObject>(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects());
            // ⚠️ REVIEW ONLY: the map slides 10 m along its road so he performs on plain asphalt. At the
            // court origin the chalk can ring sat under every frame and the owner read it as part of
            // the effect (2026-09-26, "whats that yellow shit"). The scene is never saved.
            foreach (var root in baseline) root.transform.position += new Vector3(0f, 0f, -12f);
            try
            {
                var book = RosterBook.Load();
                var paete = book.FindPersonArt("paete");
                var sean = book.FindPersonArt("sean");
                if (paete == null || paete.Model == null) throw new InvalidOperationException("Paete's roster art is missing.");

                if (treesOnly)
                {
                    // v23 (direction.md 5.14): the live ultimate is a kneel now, held down while the tree crawls out.
                    Strip("sentry", paete, "hero-paete-sentry", false, 3.0f, 16, null);
                    SentryFx(sean ?? paete);
                    PlantFx();
                    ThornFx();
                    Strip("breakout-sean", sean ?? paete, RootedMotion.Breakout, true, 0.9f, 10, null);
                    Debug.Log("[PaeteReviewProbe] wrote trees " + Version + " to " + OutDir);
                    return true;
                }
                float reach = PaeteRules.VineReachSeconds + PaeteRules.VineTellSeconds;
                float hold = PaeteRules.VineHoldSeconds(PaeteRules.VineRange);
                Strip("vine", paete, "hero-paete-vine", false, 1.0f, 12, t =>
                    new Vector3(0, 0, Mathf.Clamp01((t - reach) / hold) * 5.0f));
                Strip("sprout", paete, "hero-paete-sprout", false, .7f, 10, null);
                Strip("command", paete, "hero-paete-command", false, .5f, 8, null);
                Strip("thorns", paete, "hero-paete-thorns", false, 1.05f, 12, null);
                Strip("sentry", paete, "hero-paete-sentry", false, 3.0f, 16, null);
                Strip("struggle-paete", paete, RootedMotion.Struggle, true, 1.2f, 8, null);
                Strip("heave-paete", paete, RootedMotion.Heave, true, 1.4f, 10, null);
                Strip("breakout-paete", paete, RootedMotion.Breakout, true, 0.9f, 10, null);
                if (sean != null && sean.Model != null)
                {
                    Strip("struggle-sean", sean, RootedMotion.Struggle, true, 1.2f, 8, null);
                    Strip("heave-sean", sean, RootedMotion.Heave, true, 1.4f, 10, null);
                }
                VineFx(paete);
                SentryFx(sean ?? paete);
                PlantFx();
                ThornFx();
                Debug.Log("[PaeteReviewProbe] wrote " + Version + " to " + OutDir);
                return true;
            }
            catch (Exception error) { Debug.LogException(error); return false; }
            finally
            {
                foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                    if (!baseline.Contains(root)) Object.DestroyImmediate(root);
            }
        }

        // ⚠️ v1 FILMED HIM HEAD-ON FROM THE "SIDE" CAMERA: a person model's forward is not +Z, the game
        // turns it by `CharacterVisual.PersonModelYaw`, so every forward swing was foreshortened to
        // nothing and the strip looked static. v1 also skipped the palette (`ToonSkin.Apply`), so
        // he rendered in raw vertex orange. Both are the model review's own calls now.
        private static GameObject Body(RosterEntryAsset art)
        {
            var body = (GameObject)Object.Instantiate(art.Model);
            body.name = "PaeteReviewBody";
            body.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, CharacterVisual.PersonModelYaw, 0));
            body.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
            ToonSkin.Apply(body, ToonSkin.PersonOutlineWidth, art.Palette);
            foreach (var animator in body.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            return body;
        }

        private static void Strip(string name, RosterEntryAsset model, string clipName, bool shared, float seconds, int frames,
                                  Func<float, Vector3> travel)
        {
            var body = Body(model);
            var animator = body.GetComponentInChildren<Animator>(true);
            var root = animator != null ? animator.transform : body.transform;
            var clips = shared ? HeroAbilityClips.BuildRootedShared(root) : HeroAbilityClips.BuildPaeteAuthored(root);
            var clip = clips?.FirstOrDefault(c => c.name == clipName);
            if (clip == null) throw new InvalidOperationException("No clip " + clipName);
            var side = new List<Texture2D>(); var front = new List<Texture2D>();
            for (int i = 0; i < frames; i++)
            {
                float t = seconds * i / (frames - 1);
                Pose(root, clip, Mathf.Min(t, clip.length));
                Vector3 at = travel != null ? travel(t) : Vector3.zero;
                body.transform.position = at;
                Vector3 look = at + new Vector3(0, .8f, 0);
                var arm = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "arm-left");
                if (arm != null) Debug.Log($"[PaeteReviewProbe] {name} t={t:0.00} arm-left {arm.localEulerAngles}");
                var baked = Bake(body);
                side.Add(Shoot(look + new Vector3(3.6f, .5f, 0), look, 42, $"{name} side t={t:0.00}s"));
                front.Add(Shoot(look + new Vector3(1.2f, .4f, 3.4f), look, 42, $"{name} front t={t:0.00}s"));
                Unbake(body, baked);
            }
            Save(name, side.Concat(front).ToList(), frames);
            foreach (var c in clips) if (c != null) Object.DestroyImmediate(c);
            Object.DestroyImmediate(body);
        }

        /// <summary>
        /// ⚠️ EVERY CURVE EVALUATED AND WRITTEN BY HAND. v2 used `AnimationClip.SampleAnimation`, and on
        /// this Generic rig in edit mode it changed nothing: twelve identical frames of a clip whose
        /// bindings `PaeteMotionAuthor` had just verified. Evaluating the curves directly cannot
        /// silently no-op; a missing path throws instead.
        /// </summary>
        private static void Pose(Transform root, AnimationClip clip, float t)
        {
            var euler = new Dictionary<Transform, Vector3>(); var position = new Dictionary<Transform, Vector3>();
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var bone = string.IsNullOrEmpty(binding.path) ? root : root.Find(binding.path);
                if (bone == null) throw new InvalidOperationException(clip.name + " binds a missing path " + binding.path);
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
                    if (!position.TryGetValue(bone, out var p)) p = bone.localPosition;
                    p[axis] = v; position[bone] = p;
                }
            }
            foreach (var kv in euler) kv.Key.localEulerAngles = kv.Value;
            foreach (var kv in position) kv.Key.localPosition = kv.Value;
        }

        // ⚠️ v3 POSED THE BONES AND THE FRAMES STILL MATCHED: an edit-mode `Camera.Render` can draw a
        // skinned mesh from its last skinning. Each frame is drawn from `BakeMesh` copies instead, the
        // pose as the bones hold it right now, with the skinned renderers switched off meanwhile.
        private static List<GameObject> Bake(GameObject body)
        {
            // ⚠️ v5 DREW BAKED COPIES AND GOT THE SCALE WRONG BOTH WAYS (`BakeMesh` with and without
            // scale on a scaled rig). The real renderer is kept instead and told to re-skin on every
            // render, which is the whole of what the edit-mode frames were missing.
            foreach (var skin in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            { skin.forceMatrixRecalculationPerRender = true; skin.updateWhenOffscreen = true; }
            return new List<GameObject>();
        }

        private static void Unbake(GameObject body, List<GameObject> copies) { }

        // ------------------------------------------------------------------ THE EFFECTS, ON THEIR OWN CLOCKS

        /// <summary>LIANA LEAP on the real body: the braid leaves his forearms at the tell, catches a
        /// wall 5.5 m ahead, and reels him in, the body travelling as the motor would move it.</summary>
        private static void VineFx(RosterEntryAsset art)
        {
            var body = Body(art);
            body.AddComponent<CharacterMotor>();
            var animator = body.GetComponentInChildren<Animator>(true);
            var root = animator != null ? animator.transform : body.transform;
            var clips = HeroAbilityClips.BuildPaeteAuthored(root);
            var clip = clips.First(c => c.name == "hero-paete-vine");
            var anchor = new Vector3(0f, 1.3f, 5.5f);
            float tell = PaeteRules.VineTellSeconds, reach = PaeteRules.VineReachSeconds;
            float reel = PaeteRules.VineHoldSeconds(5.5f) + PaeteRules.VineReelSpeed / (2f * Balance.Friction);
            PaeteVineReach fx = null; float stepped = 0;
            var shots = new List<Texture2D>();
            int frames = 12; float seconds = tell + reach + reel + 0.2f;
            for (int i = 0; i < frames; i++)
            {
                float t = seconds * i / (frames - 1);
                Pose(root, clip, Mathf.Min(t, clip.length));
                body.transform.position = new Vector3(0, 0, Mathf.Clamp01((t - tell - reach) / reel) * 4.7f);
                if (t >= tell && fx == null) { fx = PaeteVineReach.Build(body.GetComponent<CharacterMotor>(), anchor, reach, reel); stepped = tell; }
                if (fx != null) { fx.Step(t - stepped); stepped = t; }
                var baked = Bake(body);
                shots.Add(Shoot(new Vector3(6.5f, 2.2f, 2.6f), new Vector3(0, 1.1f, 2.8f), 50, $"vinefx t={t:0.00}s"));
                shots.Add(Shoot(new Vector3(-1.4f, 1.9f, -2.6f), new Vector3(0, 1.2f, 3.5f), 50, $"vinefx behind t={t:0.00}s"));
                Unbake(body, baked);
            }
            Save("vinefx", Reorder(shots, frames), frames);
            if (fx != null) Object.DestroyImmediate(fx.gameObject);
            foreach (var c in clips) Object.DestroyImmediate(c);
            Object.DestroyImmediate(body);
        }

        /// <summary>The sentry from the seed landing to the pods dropping, with two bodies caught.</summary>
        private static void SentryFx(RosterEntryAsset victimArt)
        {
            var host = new GameObject("SentryHost");
            var sentry = PaeteSentryBody.Build(host.transform);
            // v14: the prisoners stand on the camera's side, so the tree wakes and looks toward the lens,
            // and they are HELD (Rooted) so the embrace limbs and the shin branches stay on them.
            sentry.SetFacing(Vector3.back);
            var victims = new List<CharacterMotor>();
            var coils = new List<PaeteRootCoil>();
            foreach (var at in new[] { new Vector3(1.55f, 0, -1.05f), new Vector3(-1.35f, 0, -1.35f) })
            {
                var v = Body(victimArt); v.transform.position = at;
                v.transform.rotation = Quaternion.LookRotation(-at.normalized) * Quaternion.Euler(0, CharacterVisual.PersonModelYaw, 0);
                var motor = v.AddComponent<CharacterMotor>();
                try { motor.ApplyRooted(PaeteRules.SentryLifeSeconds - 0.4f); } catch (Exception e) { Debug.LogWarning("[PaeteReviewProbe] rooted: " + e.Message); }
                victims.Add(motor);
            }
            sentry.SetTargets(victims);
            // v23: THE CRAWL (direction.md 5.14): the bulge, the claws, three hauls with their pauses, the crown, the wake at 1.75 s.
            float[] ages = { 0f, .1f, .2f, .3f, .45f, .6f, .75f, .9f, 1.05f, 1.2f, 1.4f, 1.6f, 1.85f, 2.4f, 6.2f, 10.0f };
            var shots = new List<Texture2D>();
            var ground = PaeteGroundBreak.Spawn(Vector3.zero, 2.2f);
            float lastAge = 0f;
            foreach (float a in ages)
            {
                sentry.Pose(a, Vector3.zero);
                ground.StepTo(Mathf.Min(a, 1.29f));
                if (a >= PaeteRules.SentryCatchSeconds + 0.3f && coils.Count == 0)
                    foreach (var v in victims) { PaeteRootCoil.Attach(v); var c = v.GetComponentInChildren<PaeteRootCoil>(); if (c != null) coils.Add(c); }
                foreach (var c in coils) if (c != null) c.Step(Mathf.Min(1f, a - lastAge));
                lastAge = a;
                var baked = victims.SelectMany(v => Bake(v.gameObject)).ToList();
                shots.Add(Shoot(new Vector3(8.5f, 4.6f, -12.5f), new Vector3(0, 4.0f, 0f), 52, $"sentry t={a:0.00}s"));
                shots.Add(Shoot(new Vector3(-2.2f, 1.2f, -8.6f), new Vector3(0, 3.6f, 0f), 62, $"sentry low t={a:0.00}s"));
                foreach (var v in victims) Unbake(v.gameObject, baked.Where(g => g != null).ToList());
            }
            Save("sentryfx", Reorder(shots, ages.Length), ages.Length);
            Object.DestroyImmediate(ground.gameObject);
            Object.DestroyImmediate(host);
            foreach (var v in victims) Object.DestroyImmediate(v.gameObject);
        }

        /// <summary>The seedling: pop-up, growing a slipper, the recoil, loosening, then pulled out.</summary>
        private static void PlantFx()
        {
            var host = new GameObject("PlantHost");
            var plant = PaetePlantBody.Build(host.transform);
            var shots = new List<Texture2D>();
            (float age, float loosen, bool pullable, float growth, float since, float pulled)[] states =
            {
                (0f, 0, false, 0, 99, -1), (.12f, 0, false, 0, 99, -1), (.25f, 0, false, 0, 99, -1), (.6f, 0, false, .1f, 99, -1),
                (5f, 0, false, .6f, 99, -1), (14f, 0, false, 1f, 99, -1), (14.08f, 0, false, 0, .08f, -1), (14.2f, 0, false, 0, .18f, -1),
                (22f, .3f, true, .5f, 99, -1), (34f, .8f, true, 1f, 99, -1), (34f, .8f, true, 1f, 99, .25f), (34f, .8f, true, 1f, 99, .6f),
            };
            foreach (var st in states)
            {
                if (st.pulled >= 0) plant.PosePulled(st.pulled, new Vector3(0, 0, -1.4f));
                else plant.Pose(st.age, st.loosen, st.pullable, st.growth, st.since);
                // v19 drew a room-sized black shape round the plant from 0.25 s to 14 s: name any renderer
                // under the host whose bounds are bigger than the plant could ever be.
                foreach (var r in host.GetComponentsInChildren<Renderer>(false))
                    if (r.enabled && r.bounds.size.magnitude > 3f)
                        Debug.Log($"[PaeteReviewProbe] plant {st.age:0.00}s oversized {r.name} {r.GetType().Name} bounds {r.bounds.size} scale {r.transform.lossyScale}");
                // ⚠️ From the FRONT three-quarter (v19 to v21 shot its back, so the wings, the lip and the rising bakya
                // never showed), with a close second row of the jug itself.
                shots.Add(Shoot(new Vector3(1.5f, 1.15f, 2.0f), new Vector3(0, .5f, 0), 44, $"plant {st.age:0.0}s"));
                shots.Add(Shoot(new Vector3(.75f, 1.05f, 1.0f), new Vector3(0, .8f, 0), 40, $"plant close {st.age:0.0}s"));
            }
            Save("plantfx", Reorder(shots, states.Length), states.Length);
            Object.DestroyImmediate(host);
        }

        /// <summary>THORN HARVEST's construct bursting and withering (no slippers: the vines to them are drawn in play).</summary>
        private static void ThornFx()
        {
            var host = new GameObject("ThornHost");
            var thorns = PaeteThornBody.Build(host.transform, new List<Slipper>());
            var shots = new List<Texture2D>();
            float[] ages = { 0f, .04f, .08f, .14f, .22f, .5f, 2.5f, 2.9f };
            var ground = PaeteGroundBreak.Spawn(Vector3.zero, 1.0f);
            foreach (float a in ages)
            {
                thorns.Pose(a, Vector3.zero);
                ground.StepTo(Mathf.Min(a, 1.29f));
                shots.Add(Shoot(new Vector3(2.0f, 1.6f, -2.2f), new Vector3(0, .4f, 0), 46, $"thorns t={a:0.00}s"));
            }
            Save("thornfx", shots, ages.Length);
            Object.DestroyImmediate(ground.gameObject);
            Object.DestroyImmediate(host);
        }

        /// <summary>Two views per frame were added frame by frame; the sheet wants one row per view.</summary>
        private static List<Texture2D> Reorder(List<Texture2D> shots, int frames)
        {
            var rows = new List<Texture2D>();
            for (int i = 0; i < frames; i++) rows.Add(shots[i * 2]);
            for (int i = 0; i < frames; i++) rows.Add(shots[i * 2 + 1]);
            return rows;
        }

        private static Texture2D Shoot(Vector3 eye, Vector3 look, float fov, string label)
        {
            var camGo = new GameObject("~PaeteReviewCam");
            camGo.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = .05f; cam.farClipPlane = 260; cam.clearFlags = CameraClearFlags.Skybox;
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
            Debug.Log("[PaeteReviewProbe] " + path);
        }
    }
}
