using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.Visual;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// ⚠️ THE EFFECT FILMS (2026-10-07, the ability rework: "rework the effects for other 2 abilities now"). The body
    /// films in the main file show his clip and nothing else; these show what an ability PUTS IN THE WORLD (the plant,
    /// the rattan, and every short effect round them), acted out on a timeline with the game's own builders, in the
    /// same preview scene and the same open editor.
    ///
    /// A film is an <see cref="FxFilm"/>: it builds its things in `Begin`, and in `Step` it does at each moment what the
    /// game would (poses the bodies by their age, spawns the effects when they would be spawned). The short effects are
    /// `Visual.PaeteFx`, which this steps itself, because nothing runs `Update` outside Play.
    ///
    /// Request: `bloomfx <tag>` or `thornfx <tag>` in `Temp/paete-ability-film.request`. Writes
    /// `Logs/paete-ability-film/<name>_<tag>_strip.png` (chosen moments, one row per view) and every frame at 30 a
    /// second in `<name>_<tag>_frames/` (the views side by side), for `tools/video_paete_ability.py`.
    /// </summary>
    public static partial class PaeteAbilityFilm
    {
        internal const int FxWidth = 480, FxHeight = 400;

        /// <summary>What a film is given to build in and shoot with.</summary>
        internal sealed class FxStage
        {
            public Scene Scene;
            public Camera Camera;
            public Light Sun;
            public RenderTexture Target;
            public Texture2D Shot;

            /// <summary>A new object for the film, in the film's scene.</summary>
            public GameObject New(string name)
            {
                var go = new GameObject(name);
                SceneManager.MoveGameObjectToScene(go, Scene);
                return go;
            }

            /// <summary>Effects are made in whatever scene is open: bring them into the film's.</summary>
            public void Adopt()
            {
                foreach (var fx in PaeteFx.Live)
                {
                    if (fx == null) continue;
                    var root = fx.transform.root.gameObject;
                    if (root.scene != Scene) SceneManager.MoveGameObjectToScene(root, Scene);
                }
            }

            /// <summary>Brings any other loose object a builder made (it must have no parent) into the film's scene.</summary>
            public void Adopt(GameObject loose)
            {
                if (loose != null && loose.transform.parent == null && loose.scene != Scene) SceneManager.MoveGameObjectToScene(loose, Scene);
            }

            public Color[] Shoot(Vector3 eye, Vector3 look, float fov)
            {
                Camera.fieldOfView = fov;
                Camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
                Sun.transform.rotation = Quaternion.Euler(45f, Camera.transform.eulerAngles.y - 30f, 0f);
                Camera.Render();
                var keep = RenderTexture.active; RenderTexture.active = Target;
                Shot.ReadPixels(new Rect(0, 0, FxWidth, FxHeight), 0, 0); Shot.Apply();
                RenderTexture.active = keep;
                return Shot.GetPixels();
            }
        }

        /// <summary>One ability's effects, acted out.</summary>
        internal abstract class FxFilm
        {
            public abstract string Name { get; }
            public abstract float Seconds { get; }
            /// <summary>The moments in the strip.</summary>
            public abstract float[] StripTimes { get; }
            /// <summary>Where each view looks from, at what, and how wide.</summary>
            public abstract (Vector3 eye, Vector3 look, float fov)[] Views { get; }
            public abstract void Begin(FxStage stage);
            /// <summary>Called once per frame with the film's clock AFTER the step and the step itself.</summary>
            public abstract void Step(FxStage stage, float t, float dt);
        }

        private static readonly Dictionary<string, Func<FxFilm>> FxFilms = new Dictionary<string, Func<FxFilm>>
        {
            { "bloomfx", () => new BloomFilm() },
            { "thornfx", () => new ThornFilm() },
            { "sentryfx", () => new SentryFilm() },
            { "stagefx", () => new SentryStageFilm() },
        };

        private static void RunFx(string subject, string tag)
        {
            string donePath = Path.Combine(OutDir, "done_" + tag + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + tag + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            var scene = EditorSceneManager.NewPreviewScene();
            var owned = new List<Object>();
            RenderTexture rt = null;
            try
            {
                var film = FxFilms[subject]();
                var stage = new FxStage { Scene = scene };
                var camGo = stage.New("~PaeteFxCamera");
                stage.Camera = camGo.AddComponent<Camera>();
                stage.Camera.scene = scene; stage.Camera.enabled = false;
                stage.Camera.nearClipPlane = 0.05f; stage.Camera.farClipPlane = 200f;
                stage.Camera.clearFlags = CameraClearFlags.SolidColor; stage.Camera.backgroundColor = new Color(0.40f, 0.46f, 0.56f);
                stage.Sun = stage.New("~PaeteFxSun").AddComponent<Light>();
                stage.Sun.type = LightType.Directional; stage.Sun.intensity = 1.05f; stage.Sun.color = new Color(1f, 0.98f, 0.95f);
                stage.Sun.shadows = LightShadows.Soft;
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);

                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                SceneManager.MoveGameObjectToScene(ground, scene);
                ground.name = "~PaeteFxGround";
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

                rt = new RenderTexture(FxWidth, FxHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                stage.Camera.targetTexture = rt; stage.Target = rt;
                stage.Shot = new Texture2D(FxWidth, FxHeight, TextureFormat.RGB24, false);
                owned.Add(stage.Shot);

                PaeteFx.FinishAll();
                film.Begin(stage);
                stage.Adopt();

                var views = film.Views;
                float[] moments = film.StripTimes;
                var strip = new Texture2D(FxWidth * moments.Length, FxHeight * views.Length, TextureFormat.RGB24, false);
                var pair = new Texture2D(FxWidth * views.Length, FxHeight, TextureFormat.RGB24, false);
                owned.Add(strip); owned.Add(pair);
                string frames = Path.Combine(OutDir, film.Name + "_" + tag + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);

                const float dt = 1f / 30f;
                int count = Mathf.CeilToInt(film.Seconds * 30f) + 1, next = 0;
                for (int f = 0; f < count; f++)
                {
                    float t = f * dt;
                    // The effects made up to the last frame move first; then the film does this moment's things.
                    if (f > 0) PaeteFx.StepAll(dt);
                    film.Step(stage, t, f > 0 ? dt : 0f);
                    stage.Adopt();
                    bool inStrip = next < moments.Length && t >= moments[next] - 0.5f * dt;
                    for (int v = 0; v < views.Length; v++)
                    {
                        var pixels = stage.Shoot(views[v].eye, views[v].look, views[v].fov);
                        pair.SetPixels(v * FxWidth, 0, FxWidth, FxHeight, pixels);
                        if (inStrip) strip.SetPixels(next * FxWidth, (views.Length - 1 - v) * FxHeight, FxWidth, FxHeight, pixels);
                    }
                    if (inStrip) next++;
                    pair.Apply();
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), pair.EncodeToPNG());
                }
                strip.Apply();
                string stripPath = Path.Combine(OutDir, film.Name + "_" + tag + "_strip.png");
                File.WriteAllBytes(stripPath, strip.EncodeToPNG());
                File.WriteAllText(donePath, stripPath.Replace('\\', '/') + "\n" + count + " frames in " + frames.Replace('\\', '/')
                    + "\nstrip at " + string.Join(" ", moments.Select(m => m.ToString("0.00"))) + "\n");
                Debug.Log("[PaeteAbilityFilm] " + subject + " " + tag + " written to " + OutDir);
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, failure.ToString());
                Debug.LogError("[PaeteAbilityFilm] " + failure);
            }
            finally
            {
                PaeteFx.FinishAll();
                if (rt != null) { RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(rt); }
                foreach (var thing in owned) if (thing != null) Object.DestroyImmediate(thing);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
