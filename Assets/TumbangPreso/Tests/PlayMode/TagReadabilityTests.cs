using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// CAN THE TAYA TAG YOU RIGHT NOW (owner, 2026-10-23: *"we need to make it more obvious kasi whenever the defender can
    /// taya someone and when they cant"*, *"maybe use pacman as reference"*). One function answers it
    /// (`RoundDirector.CanBeTaggedNow`), the tag and every cue read it, the body wears it on every gameplay camera, and it
    /// blinks before it flips. Design: docs/reports/tag-readability-2026-10-23/README.md. Frames and clips for review
    /// go to Logs/tag-readability.
    /// </summary>
    public sealed class TagReadabilityTests
    {
        private const string Out = "Logs/tag-readability";
        private bool _reduced;

        [UnitySetUp] public IEnumerator Before() { _reduced = SettingsStore.Current.ReducedUiMotion; yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After()
        {
            SettingsStore.Current.ReducedUiMotion = _reduced; Time.captureFramerate = 0;
            yield return PlayModeWorld.Reset();
        }

        private CharacterMotor _taya, _runner, _other;
        private Lata _can;

        private IEnumerator Open()
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita); yield return new WaitForSeconds(.2f);
            foreach (var ai in Object.FindObjectsByType<AIController>()) ai.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>()) input.enabled = false;
            Object.FindAnyObjectByType<SliceRunner>().Begin(); yield return null;
            foreach (var actor in GameServices.Round.Players) { actor.Intent.Clear(); actor.ClearStun(); actor.ClearTrip(); }
            var round = GameServices.Round; _can = round.Lata;
            float until = Time.unscaledTime + 4;
            while (_can.IsProtected && Time.unscaledTime < until) yield return null;
            _taya = _runner = _other = null;
            // Neither attacker is this screen's own seat: its body is hidden from its own first-person camera.
            foreach (var actor in round.Players)
            {
                if (actor.IsDefender) _taya = actor;
                else if (actor.PlayerSlot == GameLaunch.SoloSeat) continue;
                else if (_runner == null) _runner = actor;
                else if (_other == null) _other = actor;
            }
            Assert.IsNotNull(_taya); Assert.IsNotNull(_runner); Assert.IsNotNull(_other);
            Vector3 mark = _can.transform.position;
            _runner.Teleport(mark + Vector3.back * 3.0f + Vector3.right * 2.0f);
            _taya.Teleport(mark + Vector3.back * 5.2f + Vector3.left * .6f);
            // Outside the box with their slipper: safe, beside a taggable runner for contrast.
            _other.Teleport(mark + Vector3.back * (Confinement.Radius + 1.2f) + Vector3.right * 3.5f);
            foreach (var actor in round.Players)
                if (actor != _taya && actor != _runner && actor != _other) actor.Teleport(mark + Vector3.forward * 9f);
            _runner.HoldingSlipper = true;
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsTrue(_runner.IsInsideBox(), "The staged runner stands inside the box.");
        }

        private static float Rim(CharacterMotor actor, Camera camera)
        {
            var visual = actor.GetComponent<CharacterVisual>();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(CharacterVisual).GetMethod("BeginReadability", flags).Invoke(visual, new object[] { camera });
            var block = new MaterialPropertyBlock();
            visual.Model.GetComponentInChildren<SkinnedMeshRenderer>().GetPropertyBlock(block);
            float cue = block.GetFloat("_TayaCue");
            typeof(CharacterVisual).GetMethod("EndReadability", flags).Invoke(visual, new object[] { camera });
            return cue;
        }

        private static bool VulnerableRow(CharacterMotor actor)
        {
            var rows = new List<StatusRow>();
            StatusStack.Collect(actor, actor.GetComponent<Carrier>(), actor.GetComponent<CombatVerbs>(), rows);
            foreach (var row in rows) if (row.Label == "VULNERABLE") return true;
            return false;
        }

        /// <summary>The drawn fill climbs in about a fifth of a second and drains in a third; wait past both.</summary>
        private static IEnumerator Settle() { yield return new WaitForSecondsRealtime(TagExposure.DrainSeconds + .2f); }

        /// <summary>
        /// The rule, and that every cue agrees with it: in the box with a slipper and the can up is taggable; no slipper, or
        /// outside the box, is safe; Vulnerable is taggable outside; a downed can makes nobody taggable even though
        /// `IsTaggable` still says their state exposes them (the taya's rim, brackets and the VULNERABLE row used to miss that).
        /// The body cue shows on a camera that follows nobody (a spectator's), not only the taya's.
        /// </summary>
        [UnityTest]
        public IEnumerator TaggableNowIsTheTagRuleAndEveryCueReadsIt()
        {
            yield return Open();
            var round = GameServices.Round;
            var watcher = new GameObject("SpectatorView").AddComponent<Camera>(); watcher.enabled = false;
            var tayaView = Camera.main; var rig = tayaView.GetComponent<CameraSystem.CameraRig>();
            try
            {
                rig.Follow(_taya);
                yield return Settle();
                Assert.IsTrue(round.CanBeTaggedNow(_runner), "In the box, slipper in hand, can up: taggable.");
                Assert.IsTrue(VulnerableRow(_runner));
                Assert.AreEqual(TagExposure.State.Taggable, TagExposure.Read(_runner, out float level)); Assert.AreEqual(1, level);
                Assert.AreEqual(1, TagExposure.Fill(_runner), "The fill has climbed to the top.");
                Assert.IsTrue(CharacterVisual.CatchableFor(tayaView, _runner), "The taya's brackets.");
                Assert.AreEqual(1, Rim(_runner, watcher), "A spectator sees the taggable body.");
                Assert.AreEqual(1, Rim(_runner, tayaView), "The taya sees the taggable body.");
                Assert.IsTrue(TayaIdentity.DangerShown(_runner));

                _runner.HoldingSlipper = false; yield return null;
                Assert.IsFalse(round.CanBeTaggedNow(_runner), "No slipper: safe.");
                Assert.IsFalse(VulnerableRow(_runner));
                Assert.Greater(TagExposure.Fill(_runner), .5f, "The fill drains rather than vanishing...");
                yield return Settle();
                Assert.AreEqual(0, TagExposure.Fill(_runner), "...and is gone once drained.");
                Assert.AreEqual(0, Rim(_runner, watcher));

                _runner.HoldingSlipper = true;
                _runner.Teleport(_can.transform.position + Vector3.back * (Confinement.Radius + 1.5f));
                yield return Settle();
                Assert.IsFalse(_runner.IsInsideBox());
                Assert.IsFalse(round.CanBeTaggedNow(_runner), "Outside the box: safe.");
                Assert.AreEqual(0, Rim(_runner, watcher));

                _runner.ApplyVulnerable(); yield return null;
                Assert.IsTrue(round.CanBeTaggedNow(_runner), "Vulnerable: taggable anywhere.");
                Assert.Less(TagExposure.Fill(_runner), 1, "The fill climbs rather than appearing whole.");
                yield return Settle();
                Assert.AreEqual(1, Rim(_runner, watcher));

                _runner.Teleport(_can.transform.position + Vector3.back * 1.1f + Vector3.right * .6f);
                for (int i = 0; i < 2; i++) yield return null;
                _can.HostKnockDown(_other.PlayerSlot);
                yield return Settle();
                Assert.IsFalse(_can.IsUpright);
                Assert.IsTrue(_runner.IsTaggable(), "Their state still exposes them...");
                Assert.IsFalse(round.CanBeTaggedNow(_runner), "...but no tag lands while the can is down.");
                Assert.IsFalse(round.TryResolveTag(_taya, _runner), "The tag asks the same function.");
                Assert.IsFalse(VulnerableRow(_runner), "VULNERABLE must not show while the can is down.");
                Assert.IsFalse(CharacterVisual.CatchableFor(tayaView, _runner), "Nor the taya's brackets.");
                Assert.AreEqual(0, Rim(_runner, tayaView), "Nor the taya's view of the body.");
                Assert.AreEqual(0, Rim(_runner, watcher), "Nor anyone else's.");
                Assert.IsFalse(TayaIdentity.DangerShown(_runner));
            }
            finally { Object.Destroy(watcher.gameObject); }
        }

        /// <summary>
        /// The fill tells how close a flip is: the last second of Vulnerable drains it with the time left, unless the box
        /// and slipper keep them exposed anyway; while the taya stands the can up, a body that will be taggable fills with
        /// the channel, and is full the moment the can stands. Reduced UI motion jumps to the level instead of animating.
        /// </summary>
        [UnityTest]
        public IEnumerator FillTellsHowCloseTheFlipIs()
        {
            yield return Open();
            _runner.HoldingSlipper = false;
            _runner.Teleport(_can.transform.position + Vector3.back * (Confinement.Radius + 1.5f));
            for (int i = 0; i < 2; i++) yield return null;
            _runner.ApplyVulnerable(); yield return null;
            Assert.AreEqual(TagExposure.State.Taggable, TagExposure.Read(_runner, out float level)); Assert.AreEqual(1, level, "A fresh Vulnerable is full.");
            yield return new WaitUntil(() => _runner.VulnerableLeft < .6f);
            Assert.AreEqual(TagExposure.State.Taggable, TagExposure.Read(_runner, out level));
            Assert.That(level, Is.InRange(.05f, .62f), "The last second of Vulnerable drains with the time left.");
            Assert.Less(TagExposure.Fill(_runner), .9f, "The drawn fill follows it down.");
            yield return new WaitUntil(() => !_runner.IsVulnerable);
            Assert.AreEqual(TagExposure.State.Safe, TagExposure.Read(_runner, out _));

            // Ending while the box and slipper keep them exposed: no drain, they are not about to be safe.
            _runner.Teleport(_can.transform.position + Vector3.back * 1.1f + Vector3.right * .6f); _runner.HoldingSlipper = true;
            for (int i = 0; i < 2; i++) yield return null;
            _runner.ApplyVulnerable(.5f); yield return null;
            TagExposure.Read(_runner, out level); Assert.AreEqual(1, level);
            yield return new WaitUntil(() => !_runner.IsVulnerable);

            // Reduced UI motion: the fill is the level at once.
            SettingsStore.Current.ReducedUiMotion = true;
            _runner.HoldingSlipper = false; yield return null; yield return null;
            Assert.AreEqual(0, TagExposure.Fill(_runner), "Reduced UI motion jumps to the level.");
            SettingsStore.Current.ReducedUiMotion = false;
            _runner.HoldingSlipper = true; yield return Settle();

            // The can is down, then the taya stands it up.
            yield return KnockAndChannel();
            var clock = LataClockPresentation.For(_can);
            float by = Time.unscaledTime + 3;
            while (clock.RestoreRatio <= .2f && Time.unscaledTime < by) yield return null;
            Assert.Greater(clock.RestoreRatio, .2f, "The taya's channel is filling.");
            Assert.AreEqual(TagExposure.State.Arming, TagExposure.Read(_runner, out level), "About to be taggable.");
            Assert.AreEqual(clock.RestoreRatio, level, .01f, "The fill level is the channel.");
            Assert.IsTrue(TayaIdentity.DangerShown(_runner), "The arcs come in with the body.");
            Assert.AreEqual(TagExposure.State.Safe, TagExposure.Read(_other, out _), "A safe attacker outside the box stays empty.");
            float seen = TagExposure.Fill(_runner);
            by = Time.unscaledTime + 4;
            while (!_can.IsUpright && Time.unscaledTime < by) { seen = Mathf.Max(seen, TagExposure.Fill(_runner)); yield return null; }
            Assert.IsTrue(_can.IsUpright, "The channel stood the can up.");
            Assert.Greater(seen, .6f, "The fill climbed with the channel.");
            yield return Settle();
            Assert.AreEqual(TagExposure.State.Taggable, TagExposure.Read(_runner, out level)); Assert.AreEqual(1, TagExposure.Fill(_runner), "Full once the can stands.");
        }

        /// <summary>
        /// The local attacker's screen-edge frame rises with their body's fill while the can is being stood up, in the
        /// colour they chose, and the setting is local and clamped.
        /// </summary>
        [UnityTest]
        public IEnumerator LocalAttackerFrameRisesInTheChosenColour()
        {
            int colour = SettingsStore.Current.TaggableColour;
            try
            {
                yield return Open();
                var local = GameServices.Round.PlayerAt(GameLaunch.SoloSeat);
                Assert.IsFalse(local.IsDefender);
                local.Teleport(_can.transform.position + Vector3.back * 1.2f + Vector3.left * .5f); local.HoldingSlipper = true;
                var effects = Object.FindFirstObjectByType<TumpHudEffects>();
                for (int i = 0; i < 20; i++) yield return null;
                Assert.IsTrue(effects.DangerFrameVisible);
                Assert.AreEqual(TagColours.ColourOf(TagColours.Default).b, effects.FrameColour.b, .01f, "Taya blue by default.");
                SettingsStore.Current.TaggableColour = 1; yield return null; yield return null;
                Assert.AreEqual(TagColours.ColourOf(1).r, effects.FrameColour.r, .01f, "The frame takes the chosen colour.");
                SettingsStore.Current.TaggableColour = 99; SettingsStore.Current.Validate();
                Assert.AreEqual(TagColours.All.Length - 1, SettingsStore.Current.TaggableColour, "Clamped on load.");

                yield return KnockAndChannel();
                float low = 1, high = 0, by = Time.unscaledTime + 3;
                while (!_can.IsUpright && Time.unscaledTime < by)
                {
                    if (TagExposure.Read(local, out _) == TagExposure.State.Arming)
                    { float a = effects.FrameColour.a; low = Mathf.Min(low, a); high = Mathf.Max(high, a); }
                    yield return null;
                }
                Assert.Greater(high - low, .25f, "The frame rises with the restore channel.");
            }
            finally { SettingsStore.Current.TaggableColour = colour; }
        }

        private IEnumerator KnockAndChannel()
        {
            _can.HostKnockDown(_other.PlayerSlot); Hitstop.End();
            yield return new WaitForSecondsRealtime(.3f);
            _taya.Teleport(_can.transform.position + Vector3.back * .6f);
            for (int i = 0; i <= Balance.SpawnSettleFrames; i++) yield return new WaitForFixedUpdate();
            _taya.Intent.Parked = false; _taya.Intent.Set(Verb.Grab, true);
        }

        /// <summary>
        /// Review frames and clips, as the taya, an attacker and a spectator, in every state: taggable, safe, can down, the
        /// can coming up, and Vulnerable running out. No asserts beyond staging: these are for looking at.
        /// </summary>
        [UnityTest]
        public IEnumerator ReviewEveryStateFromEveryViewer()
        {
            yield return Open();
            System.IO.Directory.CreateDirectory(Out);
            var cam = new GameObject("TagReview").AddComponent<Camera>(); cam.enabled = false;
            cam.fieldOfView = 60; cam.nearClipPlane = .05f; cam.allowHDR = Camera.main.allowHDR;
            try
            {
                yield return Stills(cam, "1-taggable");
                yield return Clip(cam, "1-taggable", 1.5f, Spectator);
                _runner.HoldingSlipper = false; yield return new WaitForSecondsRealtime(.5f);
                yield return Stills(cam, "2-safe");
                // Picking the slipper up and dropping it: the cue arriving and leaving, on video.
                _runner.HoldingSlipper = true; yield return null;
                yield return Clip(cam, "3-pickup-and-drop", 2.4f, Spectator, at => { if (at > 1.2f) _runner.HoldingSlipper = false; });
                _runner.HoldingSlipper = true;
                _can.HostKnockDown(_other.PlayerSlot); Hitstop.End(); yield return new WaitForSecondsRealtime(.6f);
                yield return Stills(cam, "4-can-down");
                // Stills while the can comes up, then knock it again and film a whole channel.
                yield return KnockedChannel();
                yield return Stills(cam, "5-can-coming-up");
                float by = Time.unscaledTime + 4;
                while (!_can.IsUpright && Time.unscaledTime < by) yield return null;
                _taya.Intent.Set(Verb.Grab, false); _taya.Teleport(_can.transform.position + Vector3.back * 2.8f + Vector3.left * .9f);
                yield return new WaitForSecondsRealtime(.2f);
                // A freshly stood can is protected and refuses a knockdown: wait that out first.
                by = Time.unscaledTime + 8;
                while (_can.IsProtected && Time.unscaledTime < by) yield return null;
                _can.HostKnockDown(_other.PlayerSlot); Hitstop.End(); yield return new WaitForSecondsRealtime(.4f);
                Assert.IsFalse(_can.IsUpright, "The second knockdown landed.");
                yield return KnockedChannel();
                yield return Clip(cam, "5-can-coming-up", 2.6f, Spectator);
                _runner.HoldingSlipper = false;
                // Vulnerable with no slipper: taggable anywhere, and safe the moment it runs out.
                _runner.ApplyVulnerable(2.2f); yield return null;
                yield return Stills(cam, "6-vulnerable");
                yield return Clip(cam, "6-vulnerable-ending", 2.4f, Spectator);
                // Every choice of the Taggable colour setting on the same taggable runner.
                _runner.Teleport(_can.transform.position + Vector3.back * 3.0f + Vector3.right * 2.0f); _runner.HoldingSlipper = true;
                yield return new WaitForSecondsRealtime(.5f);
                int chosen = SettingsStore.Current.TaggableColour;
                try
                {
                    for (int c = 0; c < TagColours.All.Length; c++)
                    {
                        SettingsStore.Current.TaggableColour = c; yield return null;
                        Spectator(cam);
                        yield return GameplayShots.Render(cam, "7-colour-" + TagColours.LabelOf(c).Replace(" ", "-").ToLowerInvariant(), false, outDir: Out, width: 960, height: 540);
                    }
                }
                finally { SettingsStore.Current.TaggableColour = chosen; }
            }
            finally { Object.Destroy(cam.gameObject); }
        }

        private IEnumerator KnockedChannel()
        {
            _taya.Teleport(_can.transform.position + Vector3.back * .6f);
            for (int i = 0; i <= Balance.SpawnSettleFrames; i++) yield return new WaitForFixedUpdate();
            _taya.Intent.Parked = false; _taya.Intent.Set(Verb.Grab, true);
        }

        // A spectator high over the court, looking at the runner.
        private static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.back; }
        private void Spectator(Camera cam) => Look(cam, _runner.transform.position + new Vector3(5.5f, 5.0f, -4.5f));
        private void Look(Camera cam, Vector3 from)
        {
            Vector3 aim = _runner.transform.position + Vector3.up * .7f;
            cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(aim - from));
        }

        private IEnumerator Stills(Camera cam, string state)
        {
            // First person, as the gameplay camera is: the viewer's own body is not drawn.
            yield return FirstPerson(cam, _taya, state + "-taya");
            yield return FirstPerson(cam, _other, state + "-attacker");
            Spectator(cam); yield return GameplayShots.Render(cam, state + "-spectator", false, outDir: Out, width: 960, height: 540);
        }

        private IEnumerator FirstPerson(Camera cam, CharacterMotor viewer, string name)
        {
            Vector3 eye = viewer.transform.position + Vector3.up * 1.3f + Flat(_runner.transform.position - viewer.transform.position) * .2f;
            Look(cam, eye);
            var hidden = new List<Renderer>();
            foreach (var r in viewer.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; hidden.Add(r); }
            try { yield return GameplayShots.Render(cam, name, false, outDir: Out, width: 960, height: 540); }
            finally { foreach (var r in hidden) if (r != null) r.enabled = true; }
        }

        /// <summary>A numbered frame sequence at 30 fps of game time, for tools/encode to turn into a clip.</summary>
        private IEnumerator Clip(Camera cam, string name, float seconds, System.Action<Camera> place, System.Action<float> step = null)
        {
            string dir = System.IO.Path.Combine(Out, "clips", name);
            if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
            System.IO.Directory.CreateDirectory(dir);
            const int w = 960, h = 540;
            var rt = new RenderTexture(w, h, 24, cam.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32);
            var resolved = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var read = new Texture2D(w, h, TextureFormat.RGB24, false);
            Time.captureFramerate = 30;
            try
            {
                int frames = Mathf.Max(1, Mathf.RoundToInt(seconds * 30));
                for (int f = 0; f < frames; f++)
                {
                    step?.Invoke(f / 30f);
                    yield return null;
                    place(cam);
                    cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
                    Graphics.Blit(rt, resolved);
                    var was = RenderTexture.active; RenderTexture.active = resolved;
                    read.ReadPixels(new Rect(0, 0, w, h), 0, 0); read.Apply(); RenderTexture.active = was;
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, f.ToString("D4") + ".png"), read.EncodeToPNG());
                }
            }
            finally { Time.captureFramerate = 0; Object.Destroy(rt); Object.Destroy(resolved); Object.Destroy(read); }
        }
    }
}
