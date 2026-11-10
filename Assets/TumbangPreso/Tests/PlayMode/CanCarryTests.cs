using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// CAN-CARRY-1024: the reset is a held carry. Hold Grab beside the downed can, it comes up into the taya's hands, the
    /// body walks itself to the mark, sets it down and it stands. Letting go or a stun drops it, still down, at their feet.
    /// A client draws the carry from the host's snapshot alone.
    /// </summary>
    public sealed class CanCarryTests
    {
        private bool _bots, _spectator, _guided;
        private float _timeScale;

        [UnitySetUp] public IEnumerator Before()
        {
            Lata.LogCarry = true;
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _guided = GameLaunch.GuidedTutorial;
            _timeScale = Time.timeScale;
            yield return PlayModeWorld.Reset(); Time.timeScale = 1;
        }

        [UnityTearDown] public IEnumerator After()
        {
            Hitstop.End(); Time.timeScale = 1;
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.GuidedTutorial = _guided;
            Time.timeScale = _timeScale;
        }

        private static IEnumerator Load()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.Classic);
            var gate = Object.FindFirstObjectByType<ReadyGate>();
            if (gate != null && gate.AwaitingReady)
            {
                gate.StartLocalCountdown();
                yield return new WaitUntil(() => !gate.CountingDown);
            }
            GameServices.Match.StartMatch(); GameServices.Round.BeginRound();
            foreach (var actor in GameServices.Round.Players)
            { actor.Intent.Clear(); actor.Intent.CommitFrame(); actor.Intent.Parked = true; }
            yield return null;
        }

        private static float Flat(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }

        /// <summary>The can down, 2.6 m off its mark, the taya beside it and everyone else out of the way.</summary>
        private static IEnumerator Stage(out CharacterMotor taya, out Lata can, out Vector3 mark)
        {
            var round = GameServices.Round;
            taya = round.PlayerAt(GameServices.Match.DefenderSlot);
            can = round.Lata;
            mark = can.Mark;
            return StageRoutine(taya, can, mark);
        }

        private static IEnumerator StageRoutine(CharacterMotor taya, Lata can, Vector3 mark)
        {
            var round = GameServices.Round;
            yield return new WaitForSeconds(can.ProtectionLeft + .05f);
            can.HostKnockDown(-1); Hitstop.End();
            yield return new WaitForSeconds(Balance.ToppleTime + .1f);
            Assert.IsFalse(can.IsUpright);
            Vector3 off = mark + Vector3.right * 2.6f;
            can.transform.position = new Vector3(off.x, can.transform.position.y, off.z);
            taya.Teleport(can.transform.position + Vector3.right * .6f);
            foreach (var actor in round.Players)
                if (actor != taya) actor.Teleport(mark + new Vector3(-5, 0, 5));
            for (int i = 0; i <= Balance.SpawnSettleFrames; i++) yield return new WaitForFixedUpdate();
            taya.Intent.Parked = false;
            Assert.IsTrue(taya.GetComponent<Carrier>().HasResetTarget, "Staged beside a downed, cool can.");
        }

        [UnityTest] public IEnumerator HoldingGrabCarriesTheCanBackToItsMark()
        {
            yield return Load();
            yield return Stage(out var taya, out var can, out var mark);
            var carrier = taya.GetComponent<Carrier>();
            float startGap = Flat(taya.transform.position, mark);
            taya.Intent.Set(Verb.Grab, true);
            yield return null;
            Assert.IsTrue(can.IsCarriedBy(taya.PlayerSlot), "Holding Grab beside the can starts the pickup.");
            Assert.AreEqual(Lata.CarryPhase.Pickup, can.Carrying);
            Assert.Greater(carrier.ChannelRatio, 0);

            float deadline = Time.time + 2f;
            while (can.Carrying != Lata.CarryPhase.Carry && Time.time < deadline) yield return null;
            Assert.AreEqual(Lata.CarryPhase.Carry, can.Carrying, "The pickup leads into the carry.");
            Assert.IsTrue(carrier.IsCarryWalking);

            // In the hands: the drawn can is up at the chest, in front of the body.
            yield return new WaitForSeconds(.25f);
            Assert.IsTrue(can.PresentedEnds(out var bottom, out var top));
            float ground = taya.transform.position.y;
            Assert.Greater(bottom.y - ground, .15f, $"The carried can is held up, not dragged (base {bottom.y - ground:0.00} m up).");
            var palm = taya.GetComponent<Visual.CharacterVisual>()?.HandAnchor;
            if (palm != null)
                Assert.Less(Mathf.Abs((bottom.y + top.y) * .5f - palm.position.y), .25f, "Seen by anyone else, the can is in the hands, not up at the face.");
            Assert.Less(Flat((bottom + top) * .5f, taya.transform.position), 1.0f, "The carried can is in the taya's hands.");
            Assert.Less(Flat(taya.transform.position, mark), startGap - .2f, "The body walks itself toward the mark.");
            Assert.Greater(carrier.ChannelRatio, .2f);

            deadline = Time.time + 4f;
            while (!can.IsUpright && Time.time < deadline) yield return null;
            taya.Intent.Set(Verb.Grab, false);
            Assert.IsTrue(can.IsUpright, $"Carried back and set down (phase {can.Carrying}, {Flat(taya.transform.position, mark):0.00} m from the mark).");
            Assert.That(can.ProtectionLeft, Is.InRange(3.3f, 3.5f), "An accepted reset starts the 3.5-second protection window.");
            Assert.IsFalse(can.IsCarried);
            Assert.Less(Flat(can.transform.position, mark), .05f, "It stands on its mark.");
            Assert.Less(Flat(taya.transform.position, mark), Lata.HoldReach + Lata.ArriveSlack + .1f);
            Assert.AreEqual(0, carrier.ChannelRatio, .001f);
        }

        [UnityTest] public IEnumerator DefenderHudShowsRestoreAndTimedProtectionInFirstPerson()
        {
            yield return Load();
            yield return Stage(out var taya, out var can, out var mark);
            Hud.Instance.Bind(taya);
            var readout = Hud.Instance.GetComponent<TumpMatchReadout>();
            var rig = Camera.main.GetComponent<CameraSystem.CameraRig>();
            rig.Follow(taya); rig.SetAimSource(CameraSystem.AimSource.Movement);
            taya.transform.rotation = Quaternion.LookRotation(Vector3.left);
            var pitch = typeof(CameraSystem.CameraRig).GetField("_pitchDeg", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            pitch.SetValue(rig, 42f);
            yield return null;
            var prompt = System.Array.Find(readout.Canvas.GetComponentsInChildren<UnityEngine.UI.Text>(true), text => text.name == "ActionPrompt");
            StringAssert.Contains("restore can", prompt.text.ToLowerInvariant());
            yield return TumpUiCapture.Capture("TJ-defender-restore-refined-960x540", readout.Canvas, 960, 540,
                checkPalette: false, includeWorld: true);

            taya.Intent.Set(Verb.Grab, true);
            var prompts = new System.Collections.Generic.HashSet<string>();
            float deadline = Time.time + 4f;
            while (!can.IsUpright && Time.time < deadline)
            {
                prompts.Add(prompt.text);
                yield return null;
            }
            taya.Intent.Set(Verb.Grab, false);
            Assert.IsTrue(can.IsUpright, "The real hold-to-restore action should set the can back down.");
            CollectionAssert.IsSubsetOf(new[] { "Lifting the can", "Carrying to the mark", "Setting it down" }, prompts);
            taya.Teleport(mark + Vector3.right * 1.45f);
            taya.transform.rotation = Quaternion.LookRotation(Vector3.left);
            pitch.SetValue(rig, 28f);
            yield return null;
            var state = System.Array.Find(readout.Canvas.GetComponentsInChildren<UnityEngine.UI.Text>(true), text => text.name == "RoundLabel");
            StringAssert.StartsWith("CAN PROTECTED", state.text);
            float scale = Time.timeScale;
            Time.timeScale = 0;
            try
            {
                yield return TumpUiCapture.Capture("TJ-defender-protected-3p5-seconds-960x540", readout.Canvas, 960, 540,
                    checkPalette: false, includeWorld: true);
            }
            finally { Time.timeScale = scale; }

            taya.Teleport(mark + Vector3.right * (Balance.TayaCampClearRadius + .8f));
            yield return new WaitForSeconds(can.ProtectionLeft + .1f);
            Assert.IsEmpty(prompt.text, "An idle defender should see instructions only when an action is available.");
        }

        [UnityTest] public IEnumerator LettingGoDropsTheCanAtTheTayasFeet()
        {
            yield return Load();
            yield return Stage(out var taya, out var can, out var mark);
            taya.Intent.Set(Verb.Grab, true);
            float deadline = Time.time + 2f;
            while (can.Carrying != Lata.CarryPhase.Carry && Time.time < deadline) yield return null;
            yield return new WaitForSeconds(.3f);
            taya.Intent.Set(Verb.Grab, false);
            yield return null; yield return null;
            Assert.IsFalse(can.IsCarried, "Letting go drops it.");
            Assert.IsFalse(can.IsUpright, "A dropped can is still down.");
            Assert.Less(Flat(can.transform.position, taya.transform.position), 1.0f, "It drops at the taya's feet.");
            Assert.Greater(Flat(can.transform.position, mark), .5f, "It did not reach the mark.");
            Assert.IsTrue(taya.GetComponent<Carrier>().HasResetTarget, "The taya may pick it up again.");
        }

        [UnityTest] public IEnumerator AStunDropsTheCan()
        {
            yield return Load();
            yield return Stage(out var taya, out var can, out _);
            taya.Intent.Set(Verb.Grab, true);
            float deadline = Time.time + 2f;
            while (can.Carrying != Lata.CarryPhase.Carry && Time.time < deadline) yield return null;
            Assert.AreEqual(Lata.CarryPhase.Carry, can.Carrying);
            taya.ApplyStagger(.6f);
            yield return null; yield return null;
            Assert.IsFalse(can.IsCarried, "A stun drops the can.");
            Assert.IsFalse(can.IsUpright);
            taya.Intent.Set(Verb.Grab, false);
        }

        private sealed class Client : INetProvider
        {
            public int Slot;
            public bool IsHost => false; public bool IsNetworked => true;
            public int LocalSlot => Slot; public int LocalPeerId => 2; public bool IsSeatlessReferee => false;
        }

        [UnityTest] public IEnumerator AClientDrawsTheCarryFromTheHostSnapshot()
        {
            yield return Load();
            var round = GameServices.Round;
            var can = round.Lata;
            var taya = round.PlayerAt(GameServices.Match.DefenderSlot);
            yield return new WaitForSeconds(can.ProtectionLeft + .05f);
            can.HostKnockDown(-1); Hitstop.End();
            yield return new WaitForSeconds(Balance.ToppleTime + .1f);
            taya.Teleport(can.Mark + Vector3.right * 2.0f);
            for (int i = 0; i <= Balance.SpawnSettleFrames; i++) yield return new WaitForFixedUpdate();

            var previous = NetAuthority.Provider;
            NetAuthority.Provider = new Client { Slot = (taya.PlayerSlot + 1) % Balance.PlayerCount };
            try
            {
                can.ApplyCarrySnapshot(taya.PlayerSlot, (byte)Lata.CarryPhase.Carry, .2f);
                Assert.IsTrue(can.IsCarriedBy(taya.PlayerSlot), "The snapshot names the carrier.");
                Assert.AreEqual(.84f, can.RaiseShapeFor(taya.PlayerSlot), .001f, "Their body holds the lift through the walk.");
                Assert.Greater(taya.GetComponent<Carrier>().ChannelRatio, .2f, "Their HUD progress reads the carry on this peer.");
                Assert.IsTrue(can.PresentedEnds(out var bottom, out var _top));
                Assert.Greater(bottom.y - taya.transform.position.y, .15f, $"This peer draws the can in the carrier's hands (carried {can.IsCarried} {can.Carrying}, base {bottom} top {_top} taya {taya.transform.position} can {can.transform.position}).");
                can.ApplyCarrySnapshot(-1, 0, 0);
                Assert.IsFalse(can.IsCarried, "An empty carry suffix ends it here too.");
            }
            finally { NetAuthority.Provider = previous; }
        }
    }
}
