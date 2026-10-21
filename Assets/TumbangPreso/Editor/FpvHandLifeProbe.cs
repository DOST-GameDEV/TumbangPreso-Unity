using System.IO;
using TumbangPreso.CameraSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// A hero's first-person hands and the thing that lives on them (`ViewmodelArms.HandLife`), played through one
    /// script of everything a player does and written out frame by frame: standing, walking, sprinting, a hop, a long
    /// fall and its landing, picking up a slipper, winding up, the throw, being tagged, a cast.
    ///
    /// ⚠️ IT RUNS IN THE OPEN EDITOR, WITHOUT TOUCHING THE OPEN SCENE. `FpvNaturalArmProbe` is batch only (it opens a new
    /// scene and quits), and the owner's editor is usually open on this folder. This one builds everything in a PREVIEW
    /// scene and closes it again. Start it from the menu, or drop lines of `hero tag` into `Temp/fpv-hand-life.request`
    /// (an outside tool can then ask the open editor for a render; it is picked up within a second when not in Play).
    ///
    /// The arms are mounted as `CameraRig` mounts them (the seat, the 95 degree lens, framing at full weight), and the
    /// companion is fed the mood the script plays (`ViewmodelArms.ProbeMood`): the same code the game runs.
    /// Writes `Logs/shots-fpv-hands/<tag>/f0000.png ...` at 24 frames a second and `done.txt` (or `failed.txt`);
    /// `tools/sheet_fpv_hand_life.py <tag>` joins them into a contact sheet and a gif.
    /// </summary>
    [InitializeOnLoad]
    public static class FpvHandLifeProbe
    {
        private const string Request = "Temp/fpv-hand-life.request", OutRoot = "Logs/shots-fpv-hands";
        private const int Width = 640, Height = 360, Fps = 24;
        private static double _nextPoll;

        static FpvHandLifeProbe() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll) return;
            _nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            // Whoever asked has usually just changed a script or a model, and an editor in the background does not look
            // for changes by itself: look now, and if that starts a compile, the request waits for the reload.
            AssetDatabase.Refresh();
            if (EditorApplication.isCompiling) return;
            // One `hero tag` a line: several heroes can be asked for at once.
            string[] lines = File.ReadAllLines(Request);
            File.Delete(Request);
            foreach (string line in lines)
            {
                string[] words = line.Trim().Split(' ');
                if (words.Length == 0 || words[0].Length == 0) continue;
                Run(words[0], words.Length > 1 ? words[1] : "v00");
            }
        }

        [MenuItem("Tumbang Preso/First Person/Render Hand Life (all heroes)")]
        private static void RunAll()
        {
            foreach (string hero in new[] { "nemu", "dante", "sean", "zack", "cheska", "rafi", "amihan", "phaister", "paete" }) Run(hero, "menu");
        }

        public static void Run(string hero, string tag)
        {
            string dir = Path.Combine(OutRoot, hero + "_" + tag);
            if (Directory.Exists(dir)) foreach (string old in Directory.GetFiles(dir)) File.Delete(old);
            Directory.CreateDirectory(dir);
            var scene = EditorSceneManager.NewPreviewScene();
            RenderTexture rt = null;
            try
            {
                var camGo = new GameObject("~HandLifeCamera");
                SceneManager.MoveGameObjectToScene(camGo, scene);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = scene; cam.enabled = false;
                cam.fieldOfView = CameraRig.FppFieldOfView; cam.nearClipPlane = 0.01f; cam.farClipPlane = 100f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.40f, 0.46f, 0.56f);
                var sunGo = new GameObject("~HandLifeSun");
                SceneManager.MoveGameObjectToScene(sunGo, scene);
                var sun = sunGo.AddComponent<Light>();
                sun.type = LightType.Directional; sun.intensity = 1.05f; sun.color = new Color(1f, 0.98f, 0.95f);
                sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
                Shader.SetGlobalFloat("_CharacterSmoothShade", Visual.ToonSkin.CharacterSmoothShade);

                var mount = new GameObject("~ViewmodelArms");
                mount.transform.SetParent(camGo.transform, false);
                mount.transform.localPosition = CameraRig.ViewmodelSeat + Vector3.down * 0.08f;
                mount.transform.localScale = Vector3.one * 0.64f;
                var arms = mount.AddComponent<ViewmodelArms>();
                arms.EnsureBuilt(); arms.SetCharacter(hero); arms.SetHolding(false);

                rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                var shot = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                Random.InitState(7);

                // THE SCRIPT. Times in seconds; `beats.txt` names each for the sheet.
                const float walkAt = 7f, runAt = 9.5f, stopAt = 11.5f, hopAt = 12.2f, dropAt = 13.6f, pickAt = 17.6f, windAt = 19.4f,
                    throwAt = 20.5f, tagAt = 22.2f, freeAt = 24.2f, castAt = 25.6f, end = 27.4f;
                File.WriteAllText(Path.Combine(dir, "beats.txt"),
                    "0 standing\n" + walkAt + " walking\n" + runAt + " sprinting\n" + stopAt + " stops\n" + hopAt + " a hop\n" + dropAt + " a long fall\n"
                    + pickAt + " a slipper in hand\n" + windAt + " winding up\n" + throwAt + " thrown\n" + tagAt + " tagged\n" + freeAt + " free again\n" + castAt + " a cast\n");

                const float dt = 1f / 60f, gravity = 20f;
                float time = 0f, speed = 0f, height = 0f, gait = 0f, castLeft = 0f; bool grounded = true, holding = false; int frame = 0;
                bool hopped = false, dropped = false, thrown = false;
                var log = new System.Text.StringBuilder();
                while (time < end)
                {
                    var mood = new ViewmodelArms.HandMood { Grounded = true, Charge = -1f, Free = true };
                    if (time >= walkAt && time < stopAt) { mood.Walk = 1f; mood.Run = time >= runAt ? 1f : 0f; gait += dt * (time >= runAt ? 2.7f : 1.8f); }
                    mood.GaitPhase = gait;
                    if (!hopped && time >= hopAt) { hopped = true; grounded = false; speed = 7f; height = 0f; }
                    if (!dropped && time >= dropAt) { dropped = true; grounded = false; speed = 0f; height = 5.2f; }
                    if (!grounded) { speed = Mathf.Max(-14f, speed - gravity * dt); height += speed * dt; if (height <= 0f && speed < 0f) { grounded = true; height = 0f; } }
                    mood.Grounded = grounded; mood.VerticalSpeed = grounded ? 0f : speed;
                    bool wantHold = time >= pickAt && time < throwAt;
                    if (wantHold != holding) { holding = wantHold; if (!holding && !thrown) { thrown = true; arms.SetCharge(-1f); } arms.SetHolding(holding); }
                    mood.Carrying = holding;
                    if (holding && time >= windAt) { mood.Charge = Mathf.Clamp01((time - windAt) / (throwAt - windAt)); arms.SetCharge(mood.Charge); mood.Free = false; }
                    mood.Tagged = time >= tagAt && time < freeAt;
                    if (time >= castAt && time < castAt + dt) castLeft = .9f;
                    castLeft -= dt; mood.Casting = castLeft > 0f;
                    if (mood.Casting && hero == "paete") mood.Action = "vine-reach";
                    arms.ProbeMood = mood;
                    arms.StepVisuals(dt, snap: false);
                    if (time * Fps >= frame)
                    {
                        cam.Render();
                        var keep = RenderTexture.active; RenderTexture.active = rt;
                        shot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); shot.Apply();
                        RenderTexture.active = keep;
                        File.WriteAllBytes(Path.Combine(dir, "f" + frame.ToString("0000") + ".png"), shot.EncodeToPNG());
                        frame++;
                    }
                    time += dt;
                }
                Object.DestroyImmediate(shot);
                foreach (var r in mount.GetComponentsInChildren<Renderer>(true))
                    if (r.transform.root == camGo.transform && r.GetComponentInParent<ViewmodelArms>() != null && r.name.StartsWith("ghost-body"))
                        log.AppendLine(r.name + " at " + camGo.transform.InverseTransformPoint(r.bounds.center).ToString("F3") + " size " + r.bounds.size.ToString("F3"));
                File.WriteAllText(Path.Combine(dir, "done.txt"), frame + " frames at " + Fps + " a second\n" + log);
                Debug.Log("[HandLife] " + hero + " " + tag + ": " + frame + " frames in " + dir);
            }
            catch (System.Exception failure)
            {
                File.WriteAllText(Path.Combine(dir, "failed.txt"), failure.ToString());
                Debug.LogError("[HandLife] " + failure);
            }
            finally
            {
                if (rt != null) { RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(rt); }
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
