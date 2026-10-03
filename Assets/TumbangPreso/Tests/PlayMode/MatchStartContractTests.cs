using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    [DefaultExecutionOrder(10000)]
    public sealed class ArrivalPoseObserver : MonoBehaviour
    {
        public Transform Arm;
        public Quaternion Rotation;
        public Camera CaptureCamera;
        public string CaptureDirectory;
        public int CaptureIndex = -1;
        private void LateUpdate()
        {
            if (Arm != null) Rotation = Arm.localRotation;
            if (CaptureIndex < 0 || CaptureCamera == null || string.IsNullOrEmpty(CaptureDirectory)) return;
            int index = CaptureIndex; CaptureIndex = -1;
            var target = RenderTexture.GetTemporary(640, 360, 24);
            var previousTarget = CaptureCamera.targetTexture; var previousActive = RenderTexture.active;
            var image = new Texture2D(640, 360, TextureFormat.RGB24, false);
            try
            {
                CaptureCamera.targetTexture = target; CaptureCamera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); image.Apply();
                System.IO.Directory.CreateDirectory(CaptureDirectory);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(CaptureDirectory, $"pose-{index}.png"), image.EncodeToPNG());
            }
            finally
            {
                CaptureCamera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target); Object.DestroyImmediate(image);
            }
        }
    }

    public sealed class MatchStartContractTests
    {
        private INetProvider _provider;
        private CustomRules _rules;
        private bool _pinned, _poseHold;
        private readonly List<GameObject> _owned = new List<GameObject>();
        [UnitySetUp] public IEnumerator Before()
        {
            _provider = NetAuthority.Provider; _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = null;
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_poseHold) { typeof(PresentationClock).GetMethod("Release", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null); _poseHold = false; }
            foreach (var go in _owned) if (go != null) Object.DestroyImmediate(go);
            _owned.Clear(); yield return PlayModeWorld.Reset(); NetAuthority.Provider = _provider;
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }
        private GameObject Make(string name) { var go = new GameObject(name); _owned.Add(go); return go; }

        [Test]
        public void MapVotingIsOptionalAndSurvivesCloningAndWireReplication()
        {
            var rules = CustomGameRules.Defaults(GameMode.Classic);
            Assert.IsFalse(rules.MapVote);
            rules.MapVote = true;
            Assert.IsTrue(rules.Clone().MapVote);
            Assert.IsTrue(CustomGameRules.Parse(CustomGameRules.ToWire(rules), GameMode.HeroStrike).MapVote);
            Assert.IsFalse(CustomGameRules.Parse("0|0|8|90|0|3|0|1|0|1", GameMode.Classic).MapVote);
        }

        [UnityTest]
        public IEnumerator CustomMapVoteToggleChangesTheRuleAndManualReadyIsAbsent()
        {
            bool wasNetworked = SceneFlow.Networked; SceneFlow.Networked = false;
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            var rules = CustomGameScreen.Ensure(); _owned.Add(rules.gameObject);
            try
            {
                rules.Open(); yield return null;
                var buttons = GameObject.Find("OwnerCustomGameCanvas").GetComponentsInChildren<Button>(true);
                buttons.Single(b => b.name == "RoomRulesTab").onClick.Invoke(); yield return null;
                Assert.IsFalse(SceneFlow.SelectedRules.MapVote);
                Assert.IsFalse(buttons.Any(b => b.name == "ManualReadyNext"));
                var toggle = buttons.Single(b => b.name == "MapVoteNext");
                Assert.IsTrue(toggle.IsInteractable()); toggle.onClick.Invoke(); yield return null;
                Assert.IsTrue(SceneFlow.SelectedRules.MapVote);
                toggle.onClick.Invoke(); yield return null;
                Assert.IsFalse(SceneFlow.SelectedRules.MapVote);
                rules.Close(); Assert.IsFalse(rules.IsOpen);
            }
            finally { SceneFlow.Networked = wasNetworked; }
        }

        [UnityTest, Timeout(15000)]
        public IEnumerator FiveSecondCountdownHoldsGameplayStartsOnceAndReleases()
        {
            var gate = Make("Automatic start").AddComponent<ReadyGate>();
            var ticks = new List<string>(); int starts = 0;
            gate.CountdownTick += ticks.Add; gate.RoundShouldBegin += () => starts++;
            gate.StartLocalCountdown(); gate.StartLocalCountdown();
            Assert.IsTrue(PresentationClock.Held); Assert.AreEqual(0, Time.timeScale);
            float until = Time.realtimeSinceStartup + 8;
            while (starts == 0 && Time.realtimeSinceStartup < until) yield return null;
            CollectionAssert.AreEqual(new[] { "5", "4", "3", "2", "1", "START!" }, ticks);
            Assert.AreEqual(1, starts); Assert.IsFalse(PresentationClock.Held);
            gate.StartLocalCountdown(); yield return null; Assert.AreEqual(1, starts);
        }

        [UnityTest]
        public IEnumerator DisablingCountdownReleasesItsHoldAndCannotStartLater()
        {
            var gate = Make("Cancelled start").AddComponent<ReadyGate>(); int starts = 0;
            gate.RoundShouldBegin += () => starts++;
            gate.StartLocalCountdown(); Assert.IsTrue(PresentationClock.Held);
            gate.enabled = false; yield return null;
            Assert.IsFalse(PresentationClock.Held); Assert.AreEqual(0, starts);
        }

        [UnityTest]
        public IEnumerator LegacyManualRuleCannotShowAnotherReadyPrompt()
        {
            var rules = CustomGameRules.Defaults(GameMode.Classic); rules.ManualReady = true; SceneFlow.PinSelectedRules(rules);
            var gate = Make("Legacy rules").AddComponent<ReadyGate>(); bool shown = true;
            gate.ReadyPromptChanged += show => shown = show;
            gate.Open(null); Assert.IsFalse(shown);
            yield return null;
            Assert.IsNotNull(gate.GetComponent<MatchArrivalPresentation>());
        }

        [UnityTest]
        public IEnumerator FourArrivalPosesDifferAndClearWithoutMovingThePhysicalRoot()
        {
            GameServices.Ensure();
            var motor = Make("Arrival performer").AddComponent<CharacterMotor>(); motor.enabled = false;
            motor.Mode = GameMode.HeroStrike; motor.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "zack");
            var visual = motor.gameObject.AddComponent<CharacterVisual>();
            var art = Resources.Load<RosterEntryAsset>("Roster/person_zack");
            visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            var animation = motor.GetComponent<CharacterAnimator>();
            var arm = visual.Model.GetComponentsInChildren<Transform>(true).First(t => t.name == "arm-right");
            var observer = motor.gameObject.AddComponent<ArrivalPoseObserver>(); observer.Arm = arm;
            string capture = System.Environment.GetEnvironmentVariable("TUMP_POSE_CAPTURE");
            if (!string.IsNullOrEmpty(capture) && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var camera = Make("Pose camera").AddComponent<Camera>(); camera.enabled = false;
                camera.transform.position = new Vector3(0, 1.1f, 3.4f);
                camera.transform.LookAt(new Vector3(0, .95f, 0)); camera.fieldOfView = 40;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.14f,.18f,.22f);
                var light = Make("Pose key").AddComponent<Light>(); light.type = LightType.Directional;
                light.intensity = 1.4f; light.transform.rotation = Quaternion.Euler(35, -25, 0);
                observer.CaptureCamera = camera; observer.CaptureDirectory = capture;
            }
            yield return null;
            typeof(PresentationClock).GetMethod("Hold", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null); _poseHold = true;
            var at = motor.transform.position;
            // Establish the neutral base in this same held phase. Reopening a new
            // phase deliberately restarts its idle clip, so it is not a cleanup baseline.
            animation.SetArrivalPose(0, .000001f); yield return null; yield return null;
            var neutral = observer.Rotation;
            var poses = new Quaternion[4];
            for (int slot = 0; slot < 4; slot++)
            {
                animation.SetArrivalPose(slot, 1); yield return null;
                observer.CaptureIndex = slot; yield return null;
                poses[slot] = observer.Rotation;
                Assert.AreEqual(at, motor.transform.position);
            }
            for (int a = 0; a < 4; a++) for (int b = a + 1; b < 4; b++)
                Assert.Greater(Quaternion.Angle(poses[a], poses[b]), 5);
            animation.SetArrivalPose(0, 0);
            Assert.Less(Quaternion.Angle(neutral, arm.localRotation), 2, "Removing the overlay must restore this phase's neutral base.");
            var cleared = arm.localRotation; animation.SetArrivalPose(0, 0);
            Assert.Less(Quaternion.Angle(cleared, arm.localRotation), .01f, "Repeated cleanup must be idempotent.");
        }
    }
}
