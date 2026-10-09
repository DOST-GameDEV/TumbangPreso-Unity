using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Paete's ability body clips as filmstrips, rendered inside the OPEN editor without touching the open scene.
    ///
    /// `PaeteReviewProbe` is batch only: it opens Ilalim ng Tulay in Single mode, which would replace whatever the owner
    /// has open. This one copies `FpvHandLifeProbe` instead. Everything is built in a PREVIEW scene that is closed again,
    /// nothing is opened, created as a scene or saved, and it never runs in Play mode.
    ///
    /// Each clip is BUILT FROM THE TABLE (`HeroAbilityClips.BuildPaeteAuthored`), not loaded from the baked `.anim`, so
    /// a change to `HeroAbilityClips.Paete.cs` shows here with no re-bake. The body is the roster's own Paete, dressed
    /// by `ToonSkin` as the game dresses a body. Every curve is evaluated and written by hand, as `PaeteReviewProbe`
    /// learned to (`SampleAnimation` changed nothing on this rig in edit mode), and that includes the forearm rotation
    /// and scale curves a clip may carry (`HeroAbilityClips`, THE ELBOWS), so a fold or a stretch shows in the strip.
    ///
    /// Body only: no vines, no plants. One row of frames per view, 12 evenly spaced times plus the clip's punch times.
    ///
    /// Ask for it from the menu, or put lines of `subject tag` in `Temp/paete-ability-film.request` (subject is `vine`,
    /// `sprout`, `command`, `thorns`, `sentry` or `all`); the open editor picks it up within a second when not playing.
    /// Writes `Logs/paete-ability-film/<subject>_<tag>_side.png` and `_front.png`, then `done_<tag>.txt` naming what
    /// was written, or `failed_<tag>.txt` with the exception.
    /// </summary>
    [InitializeOnLoad]
    public static partial class PaeteAbilityFilm
    {
        private const string Request = "Temp/paete-ability-film.request", OutDir = "Logs/paete-ability-film";
        private const int Width = 320, Height = 400, EvenFrames = 12;
        private static double _nextPoll;

        private static readonly (string subject, string clip)[] Subjects =
        {
            ("vine", "hero-paete-vine"), ("sprout", "hero-paete-sprout"), ("command", "hero-paete-command"),
            ("thorns", "hero-paete-thorns"), ("sentry", "hero-paete-sentry"),
        };

        static PaeteAbilityFilm() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            // Whoever asked has usually just changed a table, and an editor in the background does not look for
            // changes by itself: look now, and if that starts a compile, the request waits for the reload.
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling) return;
            string[] lines = File.ReadAllLines(Request);
            File.Delete(Request);
            foreach (string line in lines)
            {
                string[] words = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0) continue;
                // `introfx <hero> <tag>` films another hero's cutscene; `introfx <tag>` is still Paete's.
                if (words.Length > 2 && words[0].ToLowerInvariant() == "introfx") { RunIntroFor(words[1], words[2]); continue; }
                Run(words[0], words.Length > 1 ? words[1] : "v00");
            }
        }

        [MenuItem("Tumbang Preso/Paete/Film Abilities")]
        private static void RunFromMenu() => Run("all", "menu");

        public static void Run(string subject, string tag)
        {
            Directory.CreateDirectory(OutDir);
            // A tag ends up in file names, and it came from a file anyone can write.
            tag = new string((tag ?? "v00").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
            if (tag.Length == 0) tag = "v00";
            if (subject == "bake")
            {
                // BAKE: the game plays the baked .anim files, not the tables, so a table change is not seen in Play
                // until this has run. `PaeteMotionAuthor.Bake` rewrites the existing clip assets in place (the roster
                // keeps pointing at them), so nothing else is rebuilt.
                string bakeDone = Path.Combine(OutDir, "done_" + tag + ".txt"), bakeFailed = Path.Combine(OutDir, "failed_" + tag + ".txt");
                if (File.Exists(bakeDone)) File.Delete(bakeDone);
                if (File.Exists(bakeFailed)) File.Delete(bakeFailed);
                try
                {
                    var art = RosterBook.Load()?.FindPersonArt("paete");
                    if (art == null || art.Model == null) throw new InvalidOperationException("The roster has no Paete model.");
                    var baked = PaeteMotionAuthor.Bake(art.Model);
                    AssetDatabase.SaveAssets();
                    File.WriteAllText(bakeDone, "baked " + baked.Length + " clips: " + string.Join(", ", baked.Select(c => c != null ? c.name : "null")));
                    Debug.Log("[PaeteAbilityFilm] baked " + baked.Length + " Paete clips.");
                }
                catch (Exception failure) { File.WriteAllText(bakeFailed, failure.ToString()); Debug.LogError("[PaeteAbilityFilm] bake failed: " + failure); }
                return;
            }
            // The effect films (`PaeteAbilityFilm.Fx.cs`): what an ability puts in the world, not his body.
            // `fadewalls <tag>`: the character prototype map's two grid materials onto the map's own shader, in place (what
            // `CharacterPrototypeMap.MakeMaterial` now does when the map is rebuilt), so the sky can be opened through its walls
            // for Paete's ultimate without rebuilding the map under whoever is testing in it.
            if (subject != null && subject.ToLowerInvariant() == "fadewalls")
            {
                // The walls and blocks (the dark grid) on the map's shader; the floor (the pale grid) on Standard, where it was:
                // on the map's shader it stippled away round a crouched camera (`CharacterPrototypeMap.MakeMaterial`).
                int moved = 0;
                foreach (var (path, name) in new[] { ("Assets/TumbangPreso/Scenes/Temp/prototype-grid.mat", "Standard"),
                                                     ("Assets/TumbangPreso/Scenes/Temp/prototype-grid-dark.mat", TumbangPreso.Visual.NearFade.ShaderName) })
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    var wanted = Shader.Find(name);
                    if (material == null || wanted == null) continue;
                    if (material.shader != wanted) material.shader = wanted;
                    // The walls open for the sky and do not fade at the lens (`CharacterPrototypeMap.MakeMaterial`).
                    if (material.HasProperty("_NearFadeStart")) { material.SetFloat("_NearFadeStart", 0.001f); material.SetFloat("_NearFadeEnd", 0f); }
                    EditorUtility.SetDirty(material); moved++;
                }
                AssetDatabase.SaveAssets();
                File.WriteAllText(Path.Combine(OutDir, "done_" + tag + ".txt"), moved + " prototype materials changed shader");
                return;
            }
            // The ultimate's cutscene through its own cameras (`PaeteAbilityFilm.Intro.cs`).
            if (subject != null && subject.ToLowerInvariant() == "introfx" && !EditorApplication.isPlayingOrWillChangePlaymode)
            { RunIntro(tag); return; }
            if (subject != null && FxFilms.ContainsKey(subject.ToLowerInvariant()) && !EditorApplication.isPlayingOrWillChangePlaymode)
            { RunFx(subject.ToLowerInvariant(), tag); return; }
            string donePath = Path.Combine(OutDir, "done_" + tag + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + tag + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.WriteAllText(failedPath, "The editor is in Play mode. The film only runs outside it.");
                Debug.LogWarning("[PaeteAbilityFilm] not in Play mode.");
                return;
            }

            var scene = EditorSceneManager.NewPreviewScene();
            RenderTexture rt = null;
            AnimationClip[] clips = null;
            var owned = new List<Object>();
            try
            {
                subject = (subject ?? "").ToLowerInvariant();
                var wanted = Subjects.Where(s => subject == "all" || s.subject == subject).ToArray();
                if (wanted.Length == 0)
                    throw new ArgumentException("Unknown subject '" + subject + "'. Use vine, sprout, command, thorns, sentry or all.");

                var entry = RosterBook.Load()?.FindPersonArt("paete");
                if (entry == null || entry.Model == null) throw new InvalidOperationException("Paete's roster art is missing.");

                var camGo = InScene(new GameObject("~PaeteFilmCamera"), scene);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false;
                cam.nearClipPlane = 0.05f; cam.farClipPlane = 200f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.40f, 0.46f, 0.56f);
                // The sun of `FpvHandLifeProbe`. Its yaw is set per view (`Shoot`), so each view is lit from the same
                // side of the lens that probe lights its hands from.
                var sun = InScene(new GameObject("~PaeteFilmSun"), scene).AddComponent<Light>();
                sun.type = LightType.Directional; sun.intensity = 1.05f; sun.color = new Color(1f, 0.98f, 0.95f);
                sun.shadows = LightShadows.Soft;
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);

                // A plain ground. The backdrop is the camera's flat clear colour.
                var ground = InScene(GameObject.CreatePrimitive(PrimitiveType.Plane), scene);
                ground.name = "~PaeteFilmGround";
                Object.DestroyImmediate(ground.GetComponent<Collider>());
                ground.transform.localScale = new Vector3(8f, 1f, 8f);
                var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    var plain = new Material(shader) { color = new Color(0.56f, 0.56f, 0.53f), hideFlags = HideFlags.HideAndDontSave };
                    if (plain.HasProperty("_Glossiness")) plain.SetFloat("_Glossiness", 0f);
                    owned.Add(plain);
                    ground.GetComponent<MeshRenderer>().sharedMaterial = plain;
                }

                // The body as the game stands and dresses it (`PaeteReviewProbe.Body`).
                var body = InScene((GameObject)Object.Instantiate(entry.Model), scene);
                body.name = "~PaeteFilmBody";
                body.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, CharacterVisual.PersonModelYaw, 0));
                body.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                ToonSkin.Apply(body, ToonSkin.PersonOutlineWidth, entry.Palette);
                foreach (var animator in body.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                // An edit-mode render can draw a skinned mesh from its last skinning; this makes it re-skin each render.
                foreach (var skin in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                { skin.forceMatrixRecalculationPerRender = true; skin.updateWhenOffscreen = true; }

                var rigAnimator = body.GetComponentInChildren<Animator>(true);
                var root = rigAnimator != null ? rigAnimator.transform : body.transform;
                // The rest pose, put back before every frame, so a bone one clip keys and the next does not
                // (a forearm) never carries over from the frame before.
                var bones = root.GetComponentsInChildren<Transform>(true);
                var rest = bones.Select(b => (b.localPosition, b.localRotation, b.localScale)).ToArray();

                // Framed off the body as it stands: the frame is 1.75 bodies tall, so raised arms, a stretched
                // forearm and the ultimate's kneel all stay inside it.
                var bounds = new Bounds(body.transform.position, Vector3.zero);
                foreach (var r in body.GetComponentsInChildren<Renderer>(true)) bounds.Encapsulate(r.bounds);
                float tall = Mathf.Max(0.5f, bounds.max.y);
                var look = new Vector3(0f, tall * 0.55f, 0f);
                cam.fieldOfView = 30f;
                float distance = tall * 0.875f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                // He faces +Z. The side view is from his right; the front view is three-quarter, off his right shoulder.
                Vector3 sideEye = look + new Vector3(1f, 0.12f, 0f).normalized * distance;
                Vector3 frontEye = look + new Vector3(0.6f, 0.15f, 1f).normalized * distance;

                rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = rt;
                var shot = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                owned.Add(shot);

                clips = HeroAbilityClips.BuildPaeteAuthored(root);
                var report = new System.Text.StringBuilder();
                foreach (var (name, clipName) in wanted)
                {
                    var clip = clips.FirstOrDefault(c => c != null && c.name == clipName);
                    if (clip == null) throw new InvalidOperationException("No clip " + clipName);
                    var curves = AnimationUtility.GetCurveBindings(clip)
                        .Select(binding => (binding, curve: AnimationUtility.GetEditorCurve(clip, binding))).ToArray();
                    float[] times = Times(clip.length, HeroAbilityClips.PunchTimesOf(clipName));

                    var side = new Texture2D(Width * times.Length, Height, TextureFormat.RGB24, false);
                    var front = new Texture2D(Width * times.Length, Height, TextureFormat.RGB24, false);
                    owned.Add(side); owned.Add(front);
                    for (int i = 0; i < times.Length; i++)
                    {
                        for (int b = 0; b < bones.Length; b++)
                        { bones[b].localPosition = rest[b].localPosition; bones[b].localRotation = rest[b].localRotation; bones[b].localScale = rest[b].localScale; }
                        Pose(root, clipName, curves, times[i]);
                        Shoot(cam, sun, rt, shot, sideEye, look); side.SetPixels(i * Width, 0, Width, Height, shot.GetPixels());
                        Shoot(cam, sun, rt, shot, frontEye, look); front.SetPixels(i * Width, 0, Width, Height, shot.GetPixels());
                    }
                    side.Apply(); front.Apply();
                    string sidePath = Path.Combine(OutDir, name + "_" + tag + "_side.png");
                    string frontPath = Path.Combine(OutDir, name + "_" + tag + "_front.png");
                    File.WriteAllBytes(sidePath, side.EncodeToPNG());
                    File.WriteAllBytes(frontPath, front.EncodeToPNG());

                    // EVERY FRAME, for a video (`tools/video_paete_ability.py` joins them): thirty a second, the front
                    // view beside the side view, and a third of a second held on the last pose.
                    string frames = Path.Combine(OutDir, name + "_" + tag + "_frames");
                    if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                    Directory.CreateDirectory(frames);
                    var pair = new Texture2D(Width * 2, Height, TextureFormat.RGB24, false);
                    owned.Add(pair);
                    int count = Mathf.CeilToInt((clip.length + .3f) * 30f);
                    for (int f = 0; f < count; f++)
                    {
                        for (int b = 0; b < bones.Length; b++)
                        { bones[b].localPosition = rest[b].localPosition; bones[b].localRotation = rest[b].localRotation; bones[b].localScale = rest[b].localScale; }
                        Pose(root, clipName, curves, Mathf.Min(clip.length, f / 30f));
                        Shoot(cam, sun, rt, shot, frontEye, look); pair.SetPixels(0, 0, Width, Height, shot.GetPixels());
                        Shoot(cam, sun, rt, shot, sideEye, look); pair.SetPixels(Width, 0, Width, Height, shot.GetPixels());
                        pair.Apply();
                        File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), pair.EncodeToPNG());
                    }

                    int forearm = curves.Count(c => c.binding.path.EndsWith("forearm-left") || c.binding.path.EndsWith("forearm-right"));
                    report.AppendLine(sidePath.Replace('\\', '/'));
                    report.AppendLine(frontPath.Replace('\\', '/'));
                    report.AppendLine("  " + clipName + ", " + clip.length.ToString("0.00") + " s, " + curves.Length + " curves, " + forearm
                        + " of them on a forearm, frames at " + string.Join(" ", times.Select(t => t.ToString("0.00"))));
                }
                File.WriteAllText(donePath, report.ToString());
                Debug.Log("[PaeteAbilityFilm] " + subject + " " + tag + " written to " + OutDir);
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, failure.ToString());
                Debug.LogError("[PaeteAbilityFilm] " + failure);
            }
            finally
            {
                if (rt != null) { RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(rt); }
                if (clips != null) foreach (var clip in clips) if (clip != null) Object.DestroyImmediate(clip);
                foreach (var thing in owned) if (thing != null) Object.DestroyImmediate(thing);
                // Everything else lives in the preview scene and goes with it.
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        /// <summary>Moves a new object out of the open scene and into the preview scene, at once.</summary>
        private static GameObject InScene(GameObject go, Scene scene)
        {
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }

        /// <summary>Twelve evenly spaced times across the clip, plus each punch time that is not already one of them.</summary>
        private static float[] Times(float length, float[] punches)
        {
            var times = new List<float>();
            for (int i = 0; i < EvenFrames; i++) times.Add(length * i / (EvenFrames - 1));
            foreach (float punch in punches)
                if (punch >= 0f && punch <= length && !times.Any(t => Mathf.Abs(t - punch) < 1f / 120f)) times.Add(punch);
            times.Sort();
            return times.ToArray();
        }

        /// <summary>
        /// Every curve evaluated and written by hand (`PaeteReviewProbe.Pose`), with the scale curves a stretched
        /// forearm adds. A path that does not resolve throws instead of posing nothing.
        /// </summary>
        private static void Pose(Transform root, string clipName, (EditorCurveBinding binding, AnimationCurve curve)[] curves, float time)
        {
            var euler = new Dictionary<Transform, Vector3>(); var position = new Dictionary<Transform, Vector3>();
            var scale = new Dictionary<Transform, Vector3>();
            foreach (var (binding, curve) in curves)
            {
                var bone = string.IsNullOrEmpty(binding.path) ? root : root.Find(binding.path);
                if (bone == null) throw new InvalidOperationException(clipName + " binds a missing path " + binding.path);
                float value = curve.Evaluate(time);
                string property = binding.propertyName;
                int axis = property.EndsWith(".x") ? 0 : property.EndsWith(".y") ? 1 : 2;
                if (property.StartsWith("localEulerAngles"))
                {
                    if (!euler.TryGetValue(bone, out var e)) e = bone.localEulerAngles;
                    e[axis] = value; euler[bone] = e;
                }
                else if (property.StartsWith("m_LocalPosition") || property.StartsWith("localPosition"))
                {
                    if (!position.TryGetValue(bone, out var p)) p = bone.localPosition;
                    p[axis] = value; position[bone] = p;
                }
                else if (property.StartsWith("m_LocalScale") || property.StartsWith("localScale"))
                {
                    if (!scale.TryGetValue(bone, out var s)) s = bone.localScale;
                    s[axis] = value; scale[bone] = s;
                }
                else throw new InvalidOperationException(clipName + " carries a curve this film cannot pose: " + property);
            }
            foreach (var kv in euler) kv.Key.localEulerAngles = kv.Value;
            foreach (var kv in position) kv.Key.localPosition = kv.Value;
            foreach (var kv in scale) kv.Key.localScale = kv.Value;
        }

        private static void Shoot(Camera cam, Light sun, RenderTexture rt, Texture2D shot, Vector3 eye, Vector3 look)
        {
            cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
            // `FpvHandLifeProbe` holds its sun at (45, -30) to a lens that looks along +Z: the same, turned with this lens.
            sun.transform.rotation = Quaternion.Euler(45f, cam.transform.eulerAngles.y - 30f, 0f);
            cam.Render();
            var keep = RenderTexture.active; RenderTexture.active = rt;
            shot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); shot.Apply();
            RenderTexture.active = keep;
        }
    }
}
