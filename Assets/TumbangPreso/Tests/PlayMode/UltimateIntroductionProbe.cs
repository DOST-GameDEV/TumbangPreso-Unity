using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    // Art qualification only. The shared accepted-cast phase remains a separate
    // integration gate; this cannot activate an ability or pause the match.
    public sealed class UltimateIntroductionProbe
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest, Timeout(90000)]
        public IEnumerator SixDistinctRenderOnlyIntroductionPerformances()
            => Study(new[] { "sean", "phaister", "zack", "nemu", "dante", "cheska" }, false);
        [UnityTest, Timeout(90000)]
        public IEnumerator NemuKeepsBothCharactersFramedAcrossGrowthAndAspect()
            => Study(new[] { "nemu" }, true);
        private static IEnumerator Study(string[] heroes, bool checkFraming)
        {
            yield return MapRetrievalProbe.Load("Eskinita", GameMode.HeroStrike);
            var actor = GameServices.Round.PlayerAt(1);
            var visual = actor.GetComponent<CharacterVisual>();
            foreach (var other in GameServices.Round.Players)
                if (other != actor) other.Teleport(new Vector3(-9, other.transform.position.y, 8 + other.PlayerSlot * 3));
            var camera = new GameObject("IntroductionArtWitness").AddComponent<Camera>();
            camera.CopyFrom(Camera.main); camera.enabled = false; camera.tag = "Untagged";
            camera.fieldOfView = 45; camera.aspect = 16f / 9; camera.cullingMask &= ~(1 << 5);
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var sourceCamera = Camera.main.GetComponent<CameraRig>(); sourceCamera.SetActive(false);
            Vector3[] offsets = { new Vector3(2.2f, 1.1f, 3.8f), new Vector3(1.7f, 1.2f, 4),
                new Vector3(-2.4f, 1.3f, 3.6f), new Vector3(2.2f, 1.2f, 3.8f),
                new Vector3(-2.7f, 1.0f, 4), new Vector3(1.4f, 1.3f, 3.4f) };
            // Reuse the existing editor motion-strip geometry audit rather than
            // validating root keys against a copy of the new grounding calculation.
            var audit = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("TumbangPreso.EditorTools.ClipMotionStrip")).First(t => t != null);
            const BindingFlags privateStatic = BindingFlags.NonPublic | BindingFlags.Static;
            var measure = audit.GetMethod("Measure", privateStatic);
            var report = new StringBuilder("Private body studies. Ground clearance uses the existing motion-strip deformed-vertex audit.\n");
            try
            {
                for (int i = 0; i < heroes.Length; i++)
                {
                    string hero = heroes[i];
                    actor.CharacterIndex = Roster.IndexIn(Roster.GetPeople(GameMode.HeroStrike), hero);
                    var art = RosterBook.Load().People.First(p => p.Id == hero);
                    visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
                    actor.Teleport(new Vector3(0, actor.transform.position.y, -4)); actor.transform.rotation = Quaternion.identity;
                    actor.Intent.Clear(); actor.Intent.Parked = true;
                    yield return new WaitForSeconds(.15f);
                    var track = new MatchPoseHistory.Track(actor, visual.Model);
                    track.Record(Time.time); track.Record(Time.time + .05f);
                    var stage = new GameObject("IntroductionRenderCopy"); stage.SetActive(false);
                    var copy = track.Clone(stage.transform); Assert.IsNotNull(copy);
                    track.Apply(copy, track.Newest);
                    var clip = HeroAbilityClips.BuildUltimateIntroduction(copy.Root.transform, hero);
                    Assert.IsNotNull(clip); Assert.IsTrue(clip.legacy, "Runtime-authored sampling must work in the native player."); Assert.AreEqual(2.8f, clip.length, .01f);
                    int score = GameServices.Match.ScoreFor(1); Vector3 at = actor.transform.position;
                    bool active = visual.Model.activeSelf;
                    var held = actor.GetComponent<Carrier>().Held;
                    bool heldActive = held != null && held.gameObject.activeSelf;
                    if (held != null) held.gameObject.SetActive(false); // Body study only; no floating live prop.
                    if (visual.Companion != null) visual.Companion.gameObject.SetActive(false);
                    visual.Model.SetActive(false); stage.SetActive(true); copy.ShowOnlyForCapture(true);
                    // The study has only this camera. Give the isolated copy a
                    // real contact shadow and place its neutral feet on this street.
                    clip.SampleAnimation(copy.Root, 0);
                    var surfaces = copy.Root.GetComponentsInChildren<Renderer>();
                    float low = surfaces.Min(r => r.bounds.min.y);
                    stage.transform.position += Vector3.up * (Slipper.GroundY(actor.transform.position) - low);
                    foreach (var surface in surfaces) surface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    camera.transform.position = at + offsets[i]; camera.transform.LookAt(at + Vector3.up * .9f);
                    clip.SampleAnimation(copy.Root, 0);
                    var box = (Bounds)audit.GetMethod("WorldBox", privateStatic).Invoke(null, new object[] { copy.Root });
                    audit.GetMethod("Calibrate", privateStatic).Invoke(null, new object[] { copy.Root, box.size.y, report, hero });
                    float Low(float age)
                    {
                        var sample = measure.Invoke(null, new object[] { copy.Root, age });
                        return (float)sample.GetType().GetField("LowestVertex").GetValue(sample);
                    }
                    float floor = Low(0), lowest = 0, highest = 0;
                    HeroIntroductionScene scene = null;
                    bool withScene = checkFraming || Environment.GetEnvironmentVariable("TUMP_INTRO_SCENE") == "1";
                    try
                    {
                        Assert.IsEmpty(stage.GetComponentsInChildren<MonoBehaviour>(true));
                        Assert.IsEmpty(stage.GetComponentsInChildren<Collider>(true));
                        if (withScene)
                        {
                            var random = UnityEngine.Random.state;
                            scene = new HeroIntroductionScene(stage.transform, hero, actor, copy);
                            Assert.AreEqual(random, UnityEngine.Random.state, "Scene construction changed gameplay RNG.");
                            Assert.IsEmpty(scene.Root.GetComponentsInChildren<CharacterMotor>(true));
                            Assert.IsEmpty(scene.Root.GetComponentsInChildren<Abilities.HeroAbilitySystem>(true));
                            Assert.IsTrue(scene.Root.GetComponentsInChildren<Collider>(true).All(c => !c.enabled));
                            Assert.IsTrue(scene.StartSound(), hero + " must resolve its retained theme mapping.");
                            scene.SetVisibleForCapture(true);
                        }
                        yield return ImprovementEvidenceProbe.Record(camera, hero + (withScene ? "-introduction-scene" : "-introduction-body"), 2.8f,
                            drive: age =>
                            {
                                clip.SampleAnimation(copy.Root, Mathf.Min(age, 2.8f));
                                if (scene != null)
                                {
                                    scene.Sample(age); scene.Shot(age, out var eye, out var target, out var lens, camera.aspect);
                                    camera.transform.position = eye; camera.transform.LookAt(target); camera.fieldOfView = lens;
                                    if (checkFraming)
                                    {
                                        Assert.IsTrue(scene.TryCharacterBounds(out var bounds));
                                        foreach (float aspect in new[] { 4f / 3, 16f / 9, 21f / 9 })
                                        {
                                            camera.aspect = aspect;
                                            scene.Shot(age, out eye, out target, out lens, aspect);
                                            camera.transform.position = eye; camera.transform.LookAt(target); camera.fieldOfView = lens;
                                            for (int corner = 0; corner < 8; corner++)
                                            {
                                                var atCorner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                                                    (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                                                var screen = camera.WorldToViewportPoint(atCorner);
                                                Assert.Greater(screen.z, camera.nearClipPlane);
                                                Assert.That(screen.x, Is.InRange(.075f, .925f), "Horizontal silhouette cropped at " + aspect);
                                                Assert.That(screen.y, Is.InRange(.075f, .925f), "Vertical silhouette cropped at " + aspect);
                                            }
                                        }
                                        camera.aspect = 16f / 9;
                                        scene.Shot(age, out eye, out target, out lens, camera.aspect);
                                        camera.transform.position = eye; camera.transform.LookAt(target); camera.fieldOfView = lens;
                                    }
                                }
                                float clearance = Low(age) - floor;
                                lowest = Mathf.Min(lowest, clearance); highest = Mathf.Max(highest, clearance);
                            });
                        report.AppendLine(FormattableString.Invariant($"{hero}: clearance {lowest:F5} to {highest:F5} metres"));
                        Assert.GreaterOrEqual(lowest, -.015f, hero + " penetrated its standing support plane.");
                        Assert.LessOrEqual(highest, .015f, hero + " floated above its standing support plane.");
                        Assert.AreEqual(at, actor.transform.position);
                        Assert.AreEqual(score, GameServices.Match.ScoreFor(1));
                    }
                    finally
                    {
                        scene?.Dispose();
                        visual.Model.SetActive(active);
                        if (held != null) held.gameObject.SetActive(heldActive);
                        if (visual.Companion != null) visual.Companion.gameObject.SetActive(true);
                        stage.SetActive(false); Object.Destroy(stage); Object.Destroy(clip);
                    }
                }
            }
            finally
            {
                string folder = Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs/improvement-baseline-v1";
                Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "introduction-grounding.txt"), report.ToString());
                sourceCamera.SetActive(true); Object.Destroy(camera.gameObject);
            }
        }
    }
}
