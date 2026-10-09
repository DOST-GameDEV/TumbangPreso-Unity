using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Visual;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// ⚠️ THE ULTIMATE'S CUTSCENE, FILMED OUTSIDE PLAY (2026-10-08). MAKILING'S EMBRACE's introduction is the only place
    /// the guardian's rise is seen in the game, and it could only be looked at by playing a match. This builds the real
    /// `HeroIntroductionScene` for Paete in the film's preview scene (as `AmihanReviewProbe.Introduction` does in batch),
    /// samples it on its own clock and shoots it through its own authored cameras.
    ///
    /// Request `introfx <tag>`. Writes `Logs/paete-ability-film/introfx_<tag>_strip.png` (sixteen moments) and every
    /// frame at 30 a second in `introfx_<tag>_frames/`. ⚠️ A plain stage: no map, no other players (so THE TAKE has no
    /// one to take), no sound, and the game's colour grade is not applied.
    ///
    /// ⚠️ ANY HERO (2026-10-08, for the other eight cutscenes' rework): request `introfx <hero> <tag>`. The files are then
    /// named `introfx_<hero>_<tag>_...` and `done_<hero>_<tag>.txt`. A caster with empty hands only: nothing here gives the
    /// stand-in a slipper, so the `<hero>-held` tables are not filmed yet.
    /// </summary>
    public static partial class PaeteAbilityFilm
    {
        private const int IntroWidth = 640, IntroHeight = 360;

        private static readonly string[] IntroHeroes = { "paete", "sean", "phaister", "zack", "nemu", "dante", "cheska", "rafi", "amihan" };

        private static void RunIntroFor(string hero, string tag)
        {
            Directory.CreateDirectory(OutDir);
            // Both words end up in file names, and they came from a file anyone can write.
            tag = new string((tag ?? "v00").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
            if (tag.Length == 0) tag = "v00";
            hero = (hero ?? "").ToLowerInvariant();
            if (Array.IndexOf(IntroHeroes, hero) < 0)
            {
                File.WriteAllText(Path.Combine(OutDir, "failed_" + tag + ".txt"), "No hero called '" + hero + "'. One of: " + string.Join(", ", IntroHeroes));
                return;
            }
            RunIntro(tag, hero);
        }

        private static void RunIntro(string tag, string hero = "paete")
        {
            // What the files are called. `tag` alone still chooses the film's options below (`wall`, `arms`).
            string named = hero == "paete" ? tag : hero + "_" + tag;
            string donePath = Path.Combine(OutDir, "done_" + named + ".txt"), failedPath = Path.Combine(OutDir, "failed_" + named + ".txt");
            if (File.Exists(donePath)) File.Delete(donePath);
            if (File.Exists(failedPath)) File.Delete(failedPath);
            // The shot table is a text asset regenerated outside Unity: pick it up now (film v5 shot the old table).
            UnityEditor.AssetDatabase.Refresh();
            // The parsed tables are cached for the life of the scripts (`UltimatePerformance.Cache`), and nothing here reloads
            // them: without this a film asked for after regenerating a table shoots the one read before.
            typeof(UltimatePerformance).GetMethod("Reset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.Invoke(null, null);
            var scene = EditorSceneManager.NewPreviewScene();
            var owned = new List<Object>();
            RenderTexture rt = null;
            // The close looks at his arms (`arms`, below) are filmed twice the size: what is looked for there is small.
            int width = tag.StartsWith("arms") ? IntroWidth * 2 : IntroWidth, height = tag.StartsWith("arms") ? IntroHeight * 2 : IntroHeight;
            HeroIntroductionScene intro = null;
            PaeteGroundRoots liveRoots = null;
            AnimationClip clip = null;
            try
            {
                GameObject In(GameObject go) { SceneManager.MoveGameObjectToScene(go, scene); return go; }

                var cam = In(new GameObject("~IntroCamera")).AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false;
                cam.nearClipPlane = 0.05f; cam.farClipPlane = 260f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.40f, 0.46f, 0.56f);
                var sun = In(new GameObject("~IntroSun")).AddComponent<Light>();
                sun.type = LightType.Directional; sun.intensity = 1.05f; sun.color = new Color(1f, 0.98f, 0.95f);
                sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                Shader.SetGlobalFloat("_CharacterSmoothShade", ToonSkin.CharacterSmoothShade);

                var ground = In(GameObject.CreatePrimitive(PrimitiveType.Plane));
                ground.name = "~IntroGround";
                Object.DestroyImmediate(ground.GetComponent<Collider>());
                ground.transform.localScale = new Vector3(10f, 1f, 10f);
                var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    var plain = new Material(shader) { color = new Color(0.56f, 0.56f, 0.53f), hideFlags = HideFlags.HideAndDontSave };
                    if (plain.HasProperty("_Glossiness")) plain.SetFloat("_Glossiness", 0f);
                    owned.Add(plain);
                    ground.GetComponent<MeshRenderer>().sharedMaterial = plain;
                }

                // A tag that begins `wall` stands a wall 16 m behind him and a roof over it, in the map's own shader
                // (`TumbangPreso/NearFade`), to look at her as an ENCLOSED map shows her: the cutscene opens the sky through
                // whatever wears that shader (`NearFade.OpenSky`), so these should dissolve round her and nothing else.
                if (tag.StartsWith("wall"))
                {
                    var fade = Shader.Find(NearFade.ShaderName);
                    var brick = new Material(fade != null ? fade : shader) { color = new Color(0.62f, 0.50f, 0.42f), hideFlags = HideFlags.HideAndDontSave };
                    owned.Add(brick);
                    foreach (var (at, size) in new[] { (new Vector3(0f, 9f, -16.6f), new Vector3(70f, 18f, 1f)), (new Vector3(-22f, 9f, -4f), new Vector3(1f, 18f, 26f)),
                                                       (new Vector3(0f, 18.5f, -8f), new Vector3(70f, 1f, 18f)) })
                    {
                        var wall = In(GameObject.CreatePrimitive(PrimitiveType.Cube));
                        wall.name = "~IntroWall";
                        Object.DestroyImmediate(wall.GetComponent<Collider>());
                        wall.transform.SetPositionAndRotation(at, Quaternion.identity);
                        wall.transform.localScale = size;
                        wall.GetComponent<MeshRenderer>().sharedMaterial = brick;
                    }
                }
                var art = RosterBook.Load()?.FindPersonArt(hero);
                if (art == null || art.Model == null) throw new InvalidOperationException("The roster has no art for " + hero + ".");
                var sourceGo = In(new GameObject("~IntroSource"));
                sourceGo.AddComponent<CharacterController>();
                var source = sourceGo.AddComponent<CharacterMotor>();
                var body = In((GameObject)Object.Instantiate(art.Model));
                body.name = "~IntroBody";
                body.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f));
                body.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                ToonSkin.Apply(body, ToonSkin.PersonOutlineWidth, art.Palette);
                foreach (var a in body.GetComponentsInChildren<Animator>(true)) a.enabled = false;
                foreach (var skin in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                { skin.forceMatrixRecalculationPerRender = true; skin.updateWhenOffscreen = true; }
                var animator = body.GetComponentInChildren<Animator>(true);
                var copy = new MatchPoseHistory.Copy(body);
                copy.ShowOnlyForCapture(true);
                clip = HeroAbilityClips.BuildUltimateIntroduction(animator != null ? animator.transform : body.transform, hero);
                var stage = In(new GameObject("~IntroStage"));
                // ⚠️ A stage that shows the OTHER players (`HeroIntroductionScene.Others.cs`) has nobody to copy outside Play:
                // three of the cast are stood on the court for it, switched off until a stage adopts them, and a can's place
                // is named. Not for Paete: his TAKE asks the round itself and is filmed with nobody, as before.
                HeroIntroductionScene.FilmStandIns.Clear(); HeroIntroductionScene.FilmCan = null;
                // Nemu's stage copies Kuro off her live body and the film's caster has none: stand the roster's own pet
                // beside her, bound and painted the way `CharacterVisual.ApplyModel` does it, for the stage to copy.
                HeroIntroductionScene.FilmCompanion = null;
                GameObject filmPet = null;
                if (hero == "nemu" && art.PetModel != null)
                {
                    filmPet = In((GameObject)Object.Instantiate(art.PetModel));
                    filmPet.name = "~IntroKuro";
                    filmPet.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                    var companion = filmPet.AddComponent<GhostPetCompanion>();
                    companion.Bind(body.transform, new Vector3(-0.52f, 0.50f, -0.05f), CharacterVisual.PersonScale);
                    GhostPetCompanion.ApplyAppearance(filmPet, art.Palette);
                    filmPet.transform.SetPositionAndRotation(new Vector3(-.95f, .65f, .15f), Quaternion.identity);
                    HeroIntroductionScene.FilmCompanion = companion;
                }
                if (hero != "paete")
                {
                    var places = new[] { (new Vector3(-3.4f, 0f, 3.6f), 150f), (new Vector3(3.0f, 0f, 6.4f), 215f), (new Vector3(-1.2f, 0f, 9.0f), 175f) };
                    int stood = 0;
                    foreach (string other in IntroHeroes)
                    {
                        if (other == hero || other == "paete" || stood >= places.Length) continue;
                        var otherArt = RosterBook.Load()?.FindPersonArt(other);
                        if (otherArt == null || otherArt.Model == null) continue;
                        var holder = In(new GameObject("~IntroStandIn-" + other));
                        holder.transform.SetPositionAndRotation(places[stood].Item1, Quaternion.Euler(0f, places[stood].Item2, 0f));
                        var stand = (GameObject)Object.Instantiate(otherArt.Model, holder.transform);
                        stand.transform.localPosition = Vector3.zero;
                        stand.transform.localRotation = Quaternion.Euler(0f, CharacterVisual.PersonModelYaw, 0f);
                        stand.transform.localScale = Vector3.one * CharacterVisual.PersonScale;
                        ToonSkin.Apply(stand, ToonSkin.PersonOutlineWidth, otherArt.Palette);
                        foreach (var a in stand.GetComponentsInChildren<Animator>(true)) a.enabled = false;
                        holder.SetActive(false);
                        HeroIntroductionScene.FilmStandIns.Add(holder);
                        stood++;
                    }
                    HeroIntroductionScene.FilmCan = new Vector3(0f, 0f, 5.2f);
                }
                intro = new HeroIntroductionScene(stage.transform, hero, source, copy);
                // Cheska's first-person arm writes where it really is on every frame it is up (`HeroIntroductionScene.CheskaArmLog`).
                HeroIntroductionScene.CheskaArmLog = hero == "cheska" ? new System.Text.StringBuilder() : null;
                // The stage has its own copy of him now; the one it copied is not drawn a second time.
                if (filmPet != null) foreach (var surface in filmPet.GetComponentsInChildren<Renderer>(true)) surface.enabled = false;
                intro.SetVisibleForCapture(true);

                rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = rt;
                var shot = new Texture2D(width, height, TextureFormat.RGB24, false);
                owned.Add(shot);
                string frames = Path.Combine(OutDir, "introfx_" + named + "_frames");
                if (Directory.Exists(frames)) foreach (string old in Directory.GetFiles(frames)) File.Delete(old);
                Directory.CreateDirectory(frames);

                float seconds = intro.Seconds;
                int count = Mathf.CeilToInt(seconds * 30f) + 1;
                // ⚠️ A tag that begins `armsl` is THE LIVE KNEEL'S ROOTS, not the cutscene (2026-10-08): he is held in the
                // cutscene's last pose, which is the pose play picks up from, and `PaeteGroundRoots` is bound to him and
                // posed on `PaeteGroundCall`'s own clock and numbers (the roots in at once, the three hauls, the rise at
                // 1.95 drawing them back out). `armslfp` is the same with his body not drawn, as on his own first-person
                // screen: what is left in the world there, without the first-person arms that reach down to it.
                // (`PaeteGroundCall` itself reads the court with a physics ray, which in this preview scene would hit
                // whatever map the owner has open; so its numbers are repeated here instead of it being run.)
                bool live = hero == "paete" && tag.StartsWith("armsl"), ownView = live && tag.StartsWith("armslfp");
                if (live)
                {
                    count = 100;
                    var skins = new List<Renderer>();
                    foreach (var skin in body.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skins.Add(skin);
                    liveRoots = new PaeteGroundRoots(stage.transform, PaeteProp.Palette);
                    liveRoots.BindBody(skins, PaeteProp.Palette, living: false);
                    liveRoots.BodyShown = !ownView;
                    if (ownView) copy.ShowOnlyForCapture(false);
                }
                var livePulses = new List<float> { 0.05f, 0.75f, 1.25f };
                const int columns = 8, rows = 3;
                var strip = new Texture2D(width * columns, height * rows, TextureFormat.RGB24, false);
                owned.Add(strip);
                int next = 0;
                for (int f = 0; f < count; f++)
                {
                    float t = Mathf.Min(seconds, f / 30f);
                    if (clip != null) clip.SampleAnimation(animator != null ? animator.gameObject : body, live ? seconds : t);
                    if (!live) intro.Sample(t, audible: false);
                    else
                    {
                        float age = f / 30f, retract = Mathf.Clamp01((age - PaeteGroundCall.RiseAt) / 0.22f), haul = 0f;
                        foreach (float h in PaeteGroundCall.Heaves) haul = Mathf.Max(haul, GrowthVfx.Envelope(age, h - 0.05f, 0.08f, h + 0.3f, 0.18f));
                        haul = Mathf.Max(haul, GrowthVfx.Envelope(age, 0.72f, 0.05f, 0.95f, 0.15f));
                        liveRoots.Pose(age, new Vector3(-0.40f, 0.06f, 0.98f), new Vector3(0.38f, 0.06f, 1.0f), new Vector3(0.22f, 0.04f, -0.42f), 0f, 0f,
                                       Mathf.Clamp01(age / 0.14f), haul, retract, 1f - Mathf.Clamp01((age - PaeteGroundCall.RiseAt) / 0.6f), livePulses);
                    }
                    // Whatever the scene made loose in the open scene belongs here.
                    foreach (var fx in PaeteFx.Live)
                        if (fx != null && fx.transform.root.gameObject.scene != scene) SceneManager.MoveGameObjectToScene(fx.transform.root.gameObject, scene);
                    intro.Shot(t, out var eye, out var focus, out float fov);
                    // A tag that begins `arms` leaves the authored shots and looks at his hands on the court from close by,
                    // the whole film through (2026-10-08, for his arms growing into the court: in the real ROOT shot they are
                    // forty pixels at the bottom of the frame, behind her meadow). `armsb` is the same from over his right
                    // shoulder, looking down his arms; `armsw` is all of him from the front, for the vines on his body.
                    if (tag.StartsWith("armsb")) { eye = new Vector3(1.25f, 1.95f, -0.55f); focus = new Vector3(-0.05f, 0.15f, 0.95f); fov = 40f; }
                    else if (tag.StartsWith("armsw")) { eye = new Vector3(0.9f, 1.05f, 3.3f); focus = new Vector3(0f, 0.75f, 0.2f); fov = 36f; }
                    else if (tag.StartsWith("arms")) { eye = new Vector3(1.55f, 0.85f, 2.75f); focus = new Vector3(0f, 0.28f, 0.75f); fov = 36f; }
                    cam.fieldOfView = fov;
                    cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(focus - eye, Vector3.up));
                    cam.Render();
                    var keep = RenderTexture.active; RenderTexture.active = rt;
                    shot.ReadPixels(new Rect(0, 0, width, height), 0, 0); shot.Apply();
                    RenderTexture.active = keep;
                    File.WriteAllBytes(Path.Combine(frames, "f" + f.ToString("000") + ".png"), shot.EncodeToPNG());
                    if (next < columns * rows && f >= (count - 1) * next / (columns * rows - 1))
                    {
                        strip.SetPixels((next % columns) * width, (rows - 1 - next / columns) * height, width, height, shot.GetPixels());
                        next++;
                    }
                }
                strip.Apply();
                string stripPath = Path.Combine(OutDir, "introfx_" + named + "_strip.png");
                File.WriteAllBytes(stripPath, strip.EncodeToPNG());
                File.WriteAllText(donePath, stripPath.Replace('\\', '/') + "\n" + count + " frames, " + seconds.ToString("0.00") + " s, in " + frames.Replace('\\', '/') + "\n");
                Debug.Log("[PaeteAbilityFilm] introfx " + named + " written to " + OutDir);
            }
            catch (Exception failure)
            {
                File.WriteAllText(failedPath, failure.ToString());
                Debug.LogError("[PaeteAbilityFilm] " + failure);
            }
            finally
            {
                try { liveRoots?.Dispose(); } catch (Exception e) { Debug.LogWarning("[PaeteAbilityFilm] live roots dispose: " + e.Message); }
                try { intro?.Dispose(); } catch (Exception e) { Debug.LogWarning("[PaeteAbilityFilm] intro dispose: " + e.Message); }
                if (HeroIntroductionScene.CheskaArmLog != null)
                { File.WriteAllText(Path.Combine(OutDir, "arm_" + named + ".txt"), HeroIntroductionScene.CheskaArmLog.ToString()); HeroIntroductionScene.CheskaArmLog = null; }
                HeroIntroductionScene.FilmStandIns.Clear(); HeroIntroductionScene.FilmCan = null; HeroIntroductionScene.FilmCompanion = null;
                PaeteFx.FinishAll();
                if (clip != null) Object.DestroyImmediate(clip);
                if (rt != null) { RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(rt); }
                foreach (var thing in owned) if (thing != null) Object.DestroyImmediate(thing);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
