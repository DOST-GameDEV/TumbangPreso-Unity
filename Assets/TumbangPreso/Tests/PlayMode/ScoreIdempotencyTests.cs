using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// One gameplay event pays once, however many times the request arrives.
    ///
    /// ⚠️⚠️ THE ARCHITECTURE IS RIGHT AND THAT IS AN ARGUMENT RATHER THAN A TEST. The audits say
    /// so on this commit: **49 effect call sites, 0 ungated on another body; 59 wire entry points,
    /// 0 unreachable; 61 named messages, 0 mismatched.** Every one of those is a statement about
    /// SHAPE, and a duplicate is not a shape problem. A replayed request is well formed, correctly
    /// gated, and from a legitimate peer; it is the same message twice.
    ///
    /// ⚠️⚠️ AND A DUPLICATE IS ORDINARY ON A REAL LINK, NOT AN ATTACK. `docs/TODO.md` § 137's
    /// bad-wifi table is what a venue looks like. A client that does not see its own request take
    /// effect will send it again, netcode will redeliver on some paths, and a player who does not
    /// see the can go over presses throw twice. **None of that is cheating and all of it must pay
    /// once.**
    ///
    /// ⚠️ SO THESE TEST THE GAMEPLAY LAYER RATHER THAN THE WIRE, DELIBERATELY. `MatchRpc` needs a
    /// live `NetworkManager` and two processes; `tools/net_link.py` is what covers that. What is
    /// asserted here is the property the wire depends on: **the host-side resolver is idempotent**,
    /// so redelivery cannot create a point no matter how the packet got there. If that is true, a
    /// duplicate at any layer above is harmless by construction.
    /// </summary>
    public class ScoreIdempotencyTests
    {
        private GameObject _root;
        private INetProvider _providerBefore;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return PlayModeWorld.Reset();
            _providerBefore = NetAuthority.Provider;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            NetAuthority.Provider = _providerBefore;
            if (_root != null) Object.DestroyImmediate(_root);
            yield return PlayModeWorld.Reset();
        }

        [UnityTest]
        public IEnumerator OneKnockdownPaysOnceHoweverManyArrive()
        {
            BuildMatch();
            yield return null;

            var lata = GameServices.Round.Lata;
            var match = GameServices.Match;

            // Seat 1 is an attacker in round 1 (the taya is seat 0).
            lata.HostKnockDown(1);
            int afterOne = match.ScoreFor(1);
            Assert.AreEqual(Balance.ScoreLataKnocked, afterOne,
                            "the first knockdown did not pay");

            // ⚠️ FIVE MORE, THE WAY A RETRY STORM ARRIVES. `HostKnockDown` opens with
            // `if (!_isUpright) return;`, so the can being ALREADY DOWN is what makes the
            // replay free. That guard is the idempotency and this is what asserts it.
            for (int i = 0; i < 5; i++) lata.HostKnockDown(1);

            Assert.AreEqual(afterOne, match.ScoreFor(1),
                            "a replayed knockdown paid again: one gameplay event created " +
                            "several points");
        }

        [UnityTest]
        public IEnumerator ATayaIsNotPaidForKnockingTheirOwnCanOver()
        {
            // ⚠️ A PORTED BUG, KEPT AS A REGRESSION. `round_manager.gd` reads
            // `if by_slot >= 0 and by_slot != MatchManager.defender_slot:` and only the `>= 0`
            // half made it across, so the defender's own slipper or body paid them 100. The can
            // spends most of a round on its side, so standing it up and knocking it over was a
            // loop worth 100 a go.
            BuildMatch();
            yield return null;

            var lata = GameServices.Round.Lata;
            int taya = GameServices.Match.DefenderSlot;

            lata.HostKnockDown(taya);

            Assert.AreEqual(0, GameServices.Match.ScoreFor(taya),
                            "the taya was paid for knocking their own can over");
        }

        [UnityTest]
        public IEnumerator AKnockdownOutsideALiveRoundPaysNothing()
        {
            BuildMatch();
            yield return null;

            GameServices.Round.EndRound();
            yield return null;

            GameServices.Round.Lata.HostKnockDown(1);

            Assert.AreEqual(0, GameServices.Match.ScoreFor(1),
                            "a knockdown between rounds created a point");
        }

        [UnityTest]
        public IEnumerator OneTagPaysOnceHoweverManyArrive()
        {
            BuildMatch();
            yield return null;

            var round = GameServices.Round;
            var match = GameServices.Match;
            var taya = round.PlayerAt(match.DefenderSlot);
            var victim = round.PlayerAt(1);

            victim.HoldingSlipper = true;
            victim.transform.position = Vector3.zero;
            yield return null;

            Assume.That(victim.IsTaggable(), Is.True,
                        "the victim was not taggable, so this test proves nothing");

            round.ResolveTag(taya, victim);
            int afterOne = match.ScoreFor(taya.PlayerSlot);
            Assert.AreEqual(Balance.ScoreTag, afterOne, "the first tag did not pay");

            // ⚠️ THE GUARD HERE IS NOT A FLAG, IT IS THE VICTIM'S STATE. A tag takes the slipper
            // home with them, and `IsTaggable()` requires `HoldingSlipper`, so the second request
            // arrives at somebody who is no longer a legal target. That is a better guard than a
            // "already scored" bool because it cannot go stale.
            for (int i = 0; i < 5; i++) round.ResolveTag(taya, victim);

            Assert.AreEqual(afterOne, match.ScoreFor(taya.PlayerSlot),
                            "a replayed tag paid again");
        }

        [UnityTest]
        public IEnumerator ATagOnAnUntaggableSeatPaysNothing()
        {
            BuildMatch();
            yield return null;

            var round = GameServices.Round;
            var match = GameServices.Match;
            var taya = round.PlayerAt(match.DefenderSlot);
            var victim = round.PlayerAt(1);

            victim.HoldingSlipper = false;   // empty handed: nothing to punish
            yield return null;

            round.ResolveTag(taya, victim);

            Assert.AreEqual(0, match.ScoreFor(taya.PlayerSlot),
                            "a tag on a seat that is not taggable created a point");
        }

        [UnityTest]
        public IEnumerator AClientCannotCreateAPointAtAll()
        {
            // ⚠️⚠️ THIS IS `VISION.md` § 4's FIRST RULE AS A TEST. "The host decides everything
            // that scores. One function awards every point. A point that can only be created in
            // one place cannot be created on a client at all." The guard is inside
            // `MatchDirector.AddScore` rather than at its call sites, on purpose, so this asserts
            // the one place instead of the eight.
            BuildMatch();
            yield return null;

            var match = GameServices.Match;
            NetAuthority.Provider = new ClientProviderStub();

            match.AddScore(1, ScoreEvent.LataKnocked);
            match.AddScore(1, ScoreEvent.Tag);
            GameServices.Round.Lata.HostKnockDown(1);

            Assert.AreEqual(0, match.ScoreFor(1),
                            "a peer that is not the host created points");
        }

        [UnityTest]
        public IEnumerator TheWarmupBufferRefusesEveryAward()
        {
            // A point banked during the intermission is a point from a round that is not running.
            BuildMatch();
            yield return null;

            var match = GameServices.Match;
            match.IsWarmupBuffer = true;

            match.AddScore(1, ScoreEvent.LataKnocked);

            Assert.AreEqual(0, match.ScoreFor(1), "an award landed during the warm-up buffer");
        }

        [UnityTest]
        public IEnumerator ARestoreCompletingTwiceRestoresOnce()
        {
            // The lata reset is a channel: Start opens it, Complete closes it. A duplicated
            // Complete must not restore a can that is already up, and must not re-open the
            // protection window, which would make the objective invulnerable for twice as long.
            BuildMatch();
            yield return null;

            var lata = GameServices.Round.Lata;

            lata.HostKnockDown(1);
            Assert.IsFalse(lata.IsUpright, "the setup knockdown did not take");

            lata.HostRestore();
            Assert.IsTrue(lata.IsUpright);

            lata.HostRestore();
            lata.HostRestore();

            Assert.IsTrue(lata.IsUpright, "the can did not survive a replayed restore");

            yield return new WaitForSeconds(Balance.ThrowRestoreCooldown + 0.15f);

            Assert.IsFalse(lata.IsProtected,
                           "a replayed restore extended the protection window, so the objective " +
                           "was invulnerable for longer than the authored beat");
        }

        // -------------------------------------------------------------------

        private sealed class ClientProviderStub : INetProvider
        {
            public bool IsHost => false;
            public bool IsNetworked => true;
            public int LocalSlot => 1;
            public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }

        /// <summary>
        /// A live round with four seats and a can, built rather than loaded for
        /// `MatchRunTests.BuildWorld`'s reason: a test that depends on a scene asset fails for
        /// two causes and cannot say which.
        /// </summary>
        private void BuildMatch()
        {
            _root = new GameObject("IdempotencyWorld");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.SetParent(_root.transform);
            ground.transform.localScale = Vector3.one * 6.0f;

            var lataGo = new GameObject("Lata");
            lataGo.transform.SetParent(_root.transform);
            var lata = lataGo.AddComponent<Lata>();

            GameServices.Round.Clear();
            GameServices.Round.Lata = lata;

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var go = new GameObject($"Seat{slot}");
                go.transform.SetParent(_root.transform);

                var cc = go.AddComponent<CharacterController>();
                cc.height = 1.6f;
                cc.radius = 0.35f;
                cc.center = new Vector3(0, 0.8f, 0);

                var m = go.AddComponent<CharacterMotor>();
                m.PlayerSlot = slot;
                m.CharacterIndex = slot;
                m.IsDefender = slot == 0;

                go.AddComponent<Carrier>();
                go.AddComponent<CombatVerbs>();

                go.transform.position = new Vector3(slot - 1.5f, 0.1f, 1.0f);
                GameServices.Round.Register(m);
            }

            GameServices.Match.StartMatch();
            GameServices.Round.BeginRound();
        }
    }
}
