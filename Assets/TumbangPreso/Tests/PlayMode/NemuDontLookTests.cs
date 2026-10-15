using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Nemu's DON'T LOOK (owner 2026-10-15, replacing KURO: SIT!): the ghost waits, hides after 3 s, and only the host springs
    /// it on the first other player near it: BLINDED, eyes covered 2 s, a 50 % slow wearing off over 5 s.
    /// </summary>
    public sealed class NemuDontLookTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host = true;
            public bool IsHost => Host;
            public bool IsNetworked => !Host;
            public int LocalSlot => Host ? 0 : 3;
            public int LocalPeerId => LocalSlot;
            public bool IsSeatlessReferee => false;
        }

        private INetProvider _prior;
        private Peer _peer;
        private CharacterMotor[] _players;
        private NemuHeroKit _kit;
        private AbilityContext _ctx;

        [UnitySetUp] public IEnumerator Before()
        {
            _prior = NetAuthority.Provider; yield return PlayModeWorld.Reset();
            GameServices.Ensure(); NetAuthority.Provider = _peer = new Peer();
            GameServices.Round.Clear(); GameServices.Match.ApplySnapshot(new int[4], 1, true);
            GameServices.Round.ApplySnapshot(100, true, 0, true);
            _players = new CharacterMotor[4];
            var at = new[] { Vector3.zero, new Vector3(20, 0, 0), new Vector3(-20, 0, 0), new Vector3(0, 0, -20) };
            for (int i = 0; i < 4; i++)
            {
                var root = new GameObject("Dont look seat " + i);
                _players[i] = root.AddComponent<CharacterMotor>(); _players[i].enabled = false;
                _players[i].PlayerSlot = i; _players[i].Mode = GameMode.HeroStrike;
                root.transform.position = at[i]; GameServices.Round.Register(_players[i]);
            }
            _players[3].IsDefender = true;
            var system = _players[0].gameObject.AddComponent<HeroAbilitySystem>();
            system.enabled = false; system.BindHero("nemu"); _kit = (NemuHeroKit)system.Kit;
            _ctx = new AbilityContext(_players[0], null, null, Vector3.zero, Vector3.forward, new Vector3(0, 0, 5));
        }

        [UnityTearDown] public IEnumerator After()
        {
            _kit?.ResetForRound(_ctx); NemuBlindfold.RetireAll();
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _prior;
        }

        [Test] public void TheSignatureIsDontLookUnderSitsIdentity()
        {
            Assert.AreEqual("nemu_skill1", _kit.Skill1.Id);
            Assert.AreEqual("DON'T LOOK", _kit.Skill1.Name);
            Assert.AreEqual(25f, _kit.Skill1.Cooldown);
            Assert.AreEqual(AbilityNetworkMode.HostConfirmed, _kit.Skill1.NetworkMode);
            Assert.IsFalse(_kit.Skill1.CanReactivate, "There is no teleport back any more.");
        }

        [Test] public void CastingLeavesOneGhostAndARecastMovesIt()
        {
            using (NetCue.SuppressRelay()) _kit.Skill1.Activate(_ctx);
            var first = NemuBlindfold.OwnedBy(0);
            Assert.IsNotNull(first, "The cast left no ghost.");
            Assert.IsFalse(first.Armed);
            // Shares the basic cooldown, as Sit did.
            Assert.AreEqual(_kit.Skill1.CooldownRemaining, _kit.AttackingSkill.CooldownRemaining, 1e-4f);
            var second = NemuBlindfold.Place(new Vector3(3, 0, 3), Vector3.forward, 0);
            Assert.AreSame(second, NemuBlindfold.OwnedBy(0), "Two ghosts were waiting at once.");
        }

        [UnityTest] public IEnumerator AnUnarmedGhostWaitsThenTheHostSpringsItOnTheFirstPlayerNear()
        {
            var blindfold = NemuBlindfold.Place(new Vector3(0, 0, 5), Vector3.forward, 0);
            _players[1].transform.position = new Vector3(0.5f, 0, 5.5f);
            yield return null; yield return null;
            Assert.IsFalse(_players[1].IsBlinded, "It sprang before its 3 s were up.");

            var armed = NemuBlindfold.Place(new Vector3(0, 0, 5), Vector3.forward, 0, age: NecroRules.DontLookArmSeconds + .1f);
            yield return null; yield return null;
            Assert.IsTrue(armed.Sprung);
            Assert.AreSame(_players[1], armed.Victim);
            Assert.AreEqual(5f, _players[1].BlindedLeft, .1f);
            Assert.IsTrue(_players[1].EyesCovered);
            Assert.AreEqual(.5f, _players[1].StatusSpeedScale, .02f);
            Assert.IsFalse(_players[2].IsBlinded);
        }

        [UnityTest] public IEnumerator NemuNeverTripsHerOwnGhost()
        {
            var blindfold = NemuBlindfold.Place(Vector3.zero, Vector3.forward, 0, age: NecroRules.DontLookArmSeconds + .1f);
            yield return null; yield return null;
            Assert.IsFalse(blindfold.Sprung);
            Assert.IsFalse(_players[0].IsBlinded);
        }

        [UnityTest] public IEnumerator AnObserverCannotSpringIt()
        {
            _peer.Host = false;
            var blindfold = NemuBlindfold.Place(new Vector3(20, 0, 0), Vector3.forward, 0, age: NecroRules.DontLookArmSeconds + .1f);
            yield return null; yield return null;
            Assert.IsFalse(_players[1].IsBlinded, "An observer wrote BLINDED.");
            Assert.IsFalse(blindfold.Sprung, "An observer sprang a ghost the host never did.");
            // A blinded neighbour is insufficient: only the explicit host ghost frame may name the victim.
            _players[1].ApplyNetworkBlinded(StatusRules.BlindedSeconds);
            yield return null; yield return null;
            Assert.IsFalse(blindfold.Sprung, "A different trap's victim sprang this waiting ghost.");
        }

        [Test] public void TheSlowDecaysAndTheBlindEndsAtTwoSeconds()
        {
            var body = _players[1];
            body.ApplyNetworkBlinded(5f);
            Assert.IsTrue(body.EyesCovered);
            Assert.AreEqual(.5f, body.BlindedSpeedScale, 1e-4f);
            body.ApplyNetworkBlinded(2.5f);
            Assert.IsFalse(body.EyesCovered, "Still blind after 2 s.");
            Assert.AreEqual(.75f, body.BlindedSpeedScale, 1e-4f);
            body.ApplyNetworkBlinded(float.NaN);
            Assert.IsFalse(body.IsBlinded);
            body.ApplyBlinded(); body.ApplyBlinded(100f);
            Assert.AreEqual(5f, body.BlindedLeft, 1e-4f, "BLINDED went past its 5 s.");
            body.CleanseStatuses();
            Assert.IsFalse(body.IsBlinded);
            body.ApplyBlinded(); body.ClearStatuses();
            Assert.IsFalse(body.IsBlinded);
        }

        [Test] public void ARejoinerGetsTheWaitingGhostAtTheSameAge()
        {
            var blindfold = NemuBlindfold.Place(new Vector3(2, 0, 4), Vector3.right, 0, age: 10f);
            var field = blindfold.Capture();
            Assert.IsTrue(WorldEffectSnapshot.Valid(field));
            Assert.AreEqual(20f, field.Remaining, .05f);
            var bad = field; bad.Owner = 9;
            Assert.IsFalse(WorldEffectSnapshot.Valid(bad));
            bad = field; bad.Duration = 60f;
            Assert.IsFalse(WorldEffectSnapshot.Valid(bad));
            Assert.IsTrue(WorldEffectSnapshot.Apply(new[] { field }, 1f));
            var restored = NemuBlindfold.OwnedBy(0);
            Assert.IsNotNull(restored);
            Assert.AreNotSame(blindfold, restored);
            Assert.AreEqual(11f, restored.Age, .05f);
            Assert.IsTrue(restored.Armed);
            Assert.IsFalse(restored.Sprung);
        }
    }
}
