using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class OpeningCameraCompositionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [Test, Explicit("Actual arena composition capture, without runtime match bootstrap")]
        public void PhotographActualArenaAndCharactersUsingProductionCameraSamples()
        {
            Assert.AreNotEqual(GraphicsDeviceType.Null, SystemInfo.graphicsDeviceType);
            string output = Environment.GetEnvironmentVariable("TUMP_OPENING_CAPTURE");
            Assert.IsNotEmpty(output); Directory.CreateDirectory(output);
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.OpenScene("Assets/TumbangPreso/Scenes/Maps/Eskinita.unity", OpenSceneMode.Single);
            GameObject root = null;
            try
            {
                root = new GameObject("OpeningCompositionStage");
                var camera = new GameObject("ReviewCamera", typeof(Camera)).GetComponent<Camera>();
                camera.transform.SetParent(root.transform); camera.aspect = 16f / 9; camera.fieldOfView = 70;
                var arrival = root.AddComponent<MatchArrivalPresentation>();
                void Set(string name, object value) => typeof(MatchArrivalPresentation).GetField(name, Private).SetValue(arrival, value);
                void Call(string name, params object[] args) => typeof(MatchArrivalPresentation).GetMethod(name, Private).Invoke(arrival, args);
                var players = (CharacterMotor[])typeof(MatchArrivalPresentation).GetField("_players", Private).GetValue(arrival);
                var poses = (CharacterAnimator[])typeof(MatchArrivalPresentation).GetField("_poses", Private).GetValue(arrival);
                string[] heroes = { "zack", "cheska", "dante", "nemu" };
                var transforms = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
                for (int i = 0; i < 4; i++)
                {
                    var spawn = transforms.Single(t => t.name == "Spawn" + i);
                    var actor = new GameObject("ReviewSeat" + i); actor.transform.SetParent(root.transform);
                    actor.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
                    var motor = actor.AddComponent<CharacterMotor>(); typeof(CharacterMotor).GetMethod("Awake", Private).Invoke(motor, null); motor.PlayerSlot = i; motor.IsPerson = true; motor.Mode = GameMode.HeroStrike; motor.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, heroes[i]); players[i] = motor;
                    var visual = actor.AddComponent<CharacterVisual>();
                    typeof(CharacterVisual).GetMethod("Awake", Private).Invoke(visual, null);
                    var visualRoot = new GameObject("Visual").transform; visualRoot.SetParent(actor.transform, false); visual.SetModelRoot(visualRoot);
                    var entry = AssetDatabase.LoadAssetAtPath<RosterEntryAsset>("Assets/TumbangPreso/Resources/Roster/person_" + heroes[i] + ".asset");
                    Assert.IsNotNull(entry); visual.ApplyModel(entry.Model, Color.white, entry.Clips, entry.Palette);
                    poses[i] = actor.GetComponent<CharacterAnimator>();
                }
                Set("_camera", camera); Set("_position", players[1].transform.position + Vector3.up * 1.6f);
                Set("_rotation", players[1].transform.rotation); Set("_fov", 70f);
                var can = new GameObject("ReviewLata"); can.transform.SetParent(root.transform); can.AddComponent<Lata>();
                var book = AssetDatabase.LoadAssetAtPath<RosterBook>("Assets/TumbangPreso/Resources/RosterBook.asset");
                var canArt = book.CanArt(0);
                if (canArt != null && canArt.Model != null) ToonSkin.Apply(Object.Instantiate(canArt.Model, can.transform), ToonSkin.PropOutlineWidth);
                Call("BuildCaption");
                var captureCanvas = (Canvas)typeof(MatchArrivalPresentation).GetField("_canvas", Private).GetValue(arrival); captureCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                captureCanvas.worldCamera = camera; captureCanvas.planeDistance = .5f;
                var map = SceneFlow.PreviewFor(SceneFlow.Eskinita); Physics.SyncTransforms(); Call("PrepareShots", map);
                // No simulated readiness/network claim: scene geometry and real posed roster only.
                float[] times = { 0, 1.4f, 2.65f, 2.8f, 3.25f, 4.35f, 5.45f, 6.55f, 7.2f, 7.55f, 7.9f, 8.25f, 8.6f };
                foreach (float time in times)
                {
                    Call("Sample", time, false, map);
                    foreach (var pose in poses)
                    {
                        typeof(CharacterAnimator).GetMethod("RestoreArrivalPose", Private).Invoke(pose, null);
                        typeof(CharacterAnimator).GetMethod("ApplyArrivalPose", Private).Invoke(pose, null);
                    }
                    Canvas.ForceUpdateCanvases();
                    Capture(camera, Path.Combine(output, "opening-" + time.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + ".png"));
                }
                Assert.AreEqual(70, camera.fieldOfView);
                Assert.Less(Vector3.Distance(camera.transform.position, players[1].transform.position + Vector3.up * 1.6f), .001f);
                // Remove the temporary UI immediately before destroying its owner in EditMode.
                var canvas = Object.FindFirstObjectByType<Canvas>(); if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
                arrival.Cancel();
            }
            catch (Exception error)
            {
                File.WriteAllText(Path.Combine(output, "failure.txt"), error.ToString()); throw;
            }
            finally
            {
                if (root != null)
                {
                    var canvas = Object.FindFirstObjectByType<Canvas>(); if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
                    Object.DestroyImmediate(root);
                }
                if (setup.Any(s => s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
        private static void Capture(Camera camera, string path)
        {
            var rt = RenderTexture.GetTemporary(640, 360, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            var image = new Texture2D(640, 360, TextureFormat.RGB24, false);
            try { camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); }
            finally { camera.targetTexture = oldTarget; RenderTexture.active = oldActive; RenderTexture.ReleaseTemporary(rt); Object.DestroyImmediate(image); }
        }
    }
}
