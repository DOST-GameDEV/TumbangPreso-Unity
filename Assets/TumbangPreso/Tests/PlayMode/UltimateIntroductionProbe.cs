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
        {
            yield return MapRetrievalProbe.Load("Eskinita", GameMode.HeroStrike);
            var actor = GameServices.Round.PlayerAt(1);
            var visual = actor.GetComponent<CharacterVisual>();
            foreach (var other in GameServices.Round.Players)
                if (other != actor) other.Teleport(new Vector3(-9, other.transform.position.y, 8 + other.PlayerSlot * 3));
            var camera = new GameObject("IntroductionArtWitness").AddComponent<Camera>();
            camera.CopyFrom(Camera.main); camera.enabled = false; camera.tag = "Untagged";
            camera.fieldOfView = 45; camera.cullingMask &= ~(1 << 5);
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var sourceCamera = Camera.main.GetComponent<CameraRig>(); sourceCamera.SetActive(false);
            Vector3[] offsets = { new Vector3(2.2f, 1.1f, 3.8f), new Vector3(1.7f, 1.2f, 4),
                new Vector3(-2.4f, 1.3f, 3.6f), new Vector3(2.2f, 1.2f, 3.8f),
                new Vector3(-2.7f, 1.0f, 4), new Vector3(1.4f, 1.3f, 3.4f) };
            string[] heroes = { "sean", "phaister", "zack", "nemu", "dante", "cheska" };
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
                    yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                    var track = new MatchPoseHistory.Track(actor, visual.Model);
                    track.Record(Time.time); track.Record(Time.time + .05f);
                    var stage = new GameObject("IntroductionRenderCopy"); stage.SetActive(false);
                    var copy = track.Clone(stage.transform); Assert.IsNotNull(copy);
                    track.Apply(copy, track.Newest);
                    var clip = HeroAbilityClips.BuildUltimateIntroduction(copy.Root.transform, hero);
                    Assert.IsNotNull(clip); Assert.AreEqual(2.8f, clip.length, .01f);
                    int score = GameServices.Match.ScoreFor(1); Vector3 at = actor.transform.position;
                    bool active = visual.Model.activeSelf;
                    var held = actor.GetComponent<Carrier>().Held;
                    bool heldActive = held != null && held.gameObject.activeSelf;
                    if (held != null) held.gameObject.SetActive(false); // Body study only; no floating live prop.
                    visual.Model.SetActive(false); stage.SetActive(true); copy.ShowOnlyForCapture(true);
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
                    try
                    {
                        Assert.IsEmpty(stage.GetComponentsInChildren<MonoBehaviour>(true));
                        Assert.IsEmpty(stage.GetComponentsInChildren<Collider>(true));
                        yield return ImprovementEvidenceProbe.Record(camera, hero + "-introduction-body", 2.8f,
                            drive: age =>
                            {
                                clip.SampleAnimation(copy.Root, Mathf.Min(age, 2.8f));
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
                        visual.Model.SetActive(active);
                        if (held != null) held.gameObject.SetActive(heldActive);
                        Object.Destroy(stage); Object.Destroy(clip);
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
