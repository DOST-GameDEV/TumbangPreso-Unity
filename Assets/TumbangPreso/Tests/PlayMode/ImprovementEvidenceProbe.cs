using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    // Versioned ordinary views and continuous motion, through the actual runtime graph.
    // Capture timestamps are retained so encoding cannot silently speed up a slow frame.
    // These are automated observations, not a substitute for human feel approval.
    [Category("WallClock")]
    public sealed class ImprovementEvidenceProbe
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        private static string Output => Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs/improvement-baseline-v1";

        [UnityTest]
        public IEnumerator OrdinaryPlayOnEveryMap()
        {
            foreach (string map in new[] { SceneFlow.Eskinita, SceneFlow.BayanPlaza, SceneFlow.IlalimNgTulay })
            {
                SceneFlow.SelectedMode = GameMode.Classic;
                GameLaunch.AllBots = true;
                GameLaunch.Spectator = true;
                yield return SceneManager.LoadSceneAsync(map);
                yield return new WaitForSecondsRealtime(.5f);
                Object.FindFirstObjectByType<SliceRunner>().Begin();
                var witness = MakeWitness();
                witness.transform.position = new Vector3(0, 1.65f, -12);
                witness.transform.LookAt(new Vector3(0, .9f, 0));
                yield return Record(witness, map + "-ordinary", 8f);
                for (int view = 0; view < 4; view++)
                {
                    Vector3 direction = Quaternion.Euler(0, view * 90, 0) * Vector3.back;
                    witness.transform.position = direction * 10 + Vector3.up * 1.65f;
                    witness.transform.LookAt(new Vector3(0, 1.3f, 0));
                    yield return GameplayShots.Render(witness, map + "-eye-" + view, false, Output);
                }
                Object.Destroy(witness.gameObject);
                yield return PlayModeWorld.Reset();
            }
        }

        [UnityTest]
        public IEnumerator CarryChargeReleaseAndReturn()
        {
            SceneFlow.SelectedMode = GameMode.HeroStrike;
            GameLaunch.SoloSeat = 1;
            GameLaunch.Spectator = false;
            GameLaunch.AllBots = false;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return new WaitForSecondsRealtime(.5f);
            Object.FindFirstObjectByType<SliceRunner>().Begin();
            yield return new WaitForSecondsRealtime(.3f);
            var who = GameServices.Round.PlayerAt(1);
            Assert.IsNotNull(who);
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None))
                brain.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))
                reader.enabled = false;
            who.Intent.Clear();
            who.Intent.Parked = false;
            var witness = MakeWitness();
            yield return Record(witness, "carry-charge-release", 9f, who, t =>
            {
                who.Intent.Move = t > .7f && t < 5.2f ? new Vector2(.5f, -.35f) : Vector2.zero;
                who.Intent.Set(Verb.Sprint, t > 2f && t < 3.5f);
                who.Intent.Set(Verb.SpecialAbility, t > 3.5f && t < 5f);
                who.Intent.AimPoint = Vector3.zero;
                who.Intent.FaceAimPoint = true;
                if (t > 6f)
                {
                    var shoe = Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None)
                        .FirstOrDefault(s => s.SeatOfOrigin == 1 && s.State == SlipperState.Loose);
                    if (shoe != null)
                    {
                        Vector3 delta = shoe.transform.position - who.transform.position;
                        who.Intent.Move = new Vector2(delta.x, delta.z).normalized;
                        who.Intent.Set(Verb.Lunge, t > 6.3f && t < 6.6f);
                        who.Intent.Set(Verb.Grab, true);
                    }
                }
            });
            Object.Destroy(witness.gameObject);
        }

        private static Camera MakeWitness()
        {
            var camera = new GameObject("ImprovementWitness").AddComponent<Camera>();
            camera.enabled = false;
            camera.fieldOfView = 52;
            camera.nearClipPlane = .05f;
            camera.farClipPlane = 400;
            camera.allowHDR = true;
            camera.cullingMask &= ~(1 << 5);
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            return camera;
        }

        private static IEnumerator Record(Camera camera, string name, float seconds,
            CharacterMotor subject = null, Action<float> drive = null)
        {
            string folder = Path.Combine(Output, name);
            Directory.CreateDirectory(folder);
            var rt = new RenderTexture(960, 540, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var ldr = new RenderTexture(960, 540, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
            var log = new StringBuilder("frame,real_seconds,game_seconds,speed,held,leg_angle,action\n");
            var field = typeof(CharacterAnimator).GetField("_current", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            float start = Time.realtimeSinceStartup, gameStart = Time.time, next = 0;
            int frame = 0;
            var previous = camera.targetTexture;
            try
            {
                while (Time.realtimeSinceStartup - start < seconds)
                {
                    float t = Time.realtimeSinceStartup - start;
                    drive?.Invoke(t);
                    yield return null;
                    if (t < next) continue;
                    next = t + .05f;
                    if (subject != null)
                    {
                        camera.transform.position = subject.transform.position + new Vector3(3, 1.5f, 4);
                        camera.transform.LookAt(subject.transform.position + Vector3.up * .9f);
                    }
                    var renderers = subject != null ? subject.GetComponentsInChildren<Renderer>() : Array.Empty<Renderer>();
                    var shadow = renderers.Select(r => r.shadowCastingMode).ToArray();
                    for (int i = 0; i < renderers.Length; i++)
                        if (shadow[i] == ShadowCastingMode.ShadowsOnly) renderers[i].shadowCastingMode = ShadowCastingMode.On;
                    camera.targetTexture = rt;
                    camera.Render();
                    for (int i = 0; i < renderers.Length; i++) renderers[i].shadowCastingMode = shadow[i];
                    Graphics.Blit(rt, ldr);
                    var active = RenderTexture.active;
                    RenderTexture.active = ldr;
                    pixels.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                    pixels.Apply();
                    RenderTexture.active = active;
                    File.WriteAllBytes(Path.Combine(folder, $"{frame:D5}.jpg"), pixels.EncodeToJPG(90));
                    var leg = subject != null ? subject.GetComponentsInChildren<Transform>().FirstOrDefault(b => b.name == "leg-right") : null;
                    string action = subject != null ? (string)field.GetValue(subject.GetComponent<CharacterAnimator>()) : "ordinary";
                    log.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:F4},{2:F4},{3:F4},{4},{5:F3},{6}",
                        frame++, Time.realtimeSinceStartup-start, Time.time-gameStart,
                        subject != null ? new Vector2(subject.Velocity.x, subject.Velocity.z).magnitude : 0,
                        subject != null && subject.HoldingSlipper, leg != null ? leg.localEulerAngles.x : 0, action));
                }
            }
            finally
            {
                camera.targetTexture = previous;
                Object.DestroyImmediate(rt); Object.DestroyImmediate(ldr); Object.DestroyImmediate(pixels);
                File.WriteAllText(Path.Combine(folder, "frames.csv"), log.ToString());
            }
            Assert.Greater(frame, seconds * 4, "The capture must contain enough actual frames to review.");
        }
    }
}
