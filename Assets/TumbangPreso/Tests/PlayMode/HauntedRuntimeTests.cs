using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class HauntedRuntimeTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host = true;
            public bool IsHost => Host;
            public bool IsNetworked => !Host;
            public int LocalSlot => Host ? 0 : 1;
            public int LocalPeerId => LocalSlot;
            public bool IsSeatlessReferee => false;
        }
        private INetProvider _provider;
        private CharacterMotor _motor;
        private Peer _peer;
        private CustomRules _rules;
        private bool _pinned, _bots, _spectator;
        private int _seat;
        [UnitySetUp] public IEnumerator Before()
        {
            _provider = NetAuthority.Provider; _rules = SceneFlow.SelectedRules.Clone();
            _pinned = SceneFlow.RulesPinned; _bots = GameLaunch.AllBots;
            _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            yield return PlayModeWorld.Reset();
            _peer = new Peer(); NetAuthority.Provider = _peer;
            _motor = new GameObject("Haunted body").AddComponent<CharacterMotor>();
            _motor.enabled = false; _motor.PlayerSlot = 0; _motor.Mode = GameMode.HeroStrike;
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _provider;
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }
        private void Tick(float dt) => typeof(CharacterMotor).GetMethod("StepStatuses",
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_motor, new object[] { dt });
        [Test] public void HauntedRefreshesWithoutStackingAndExpiresWithoutBlockingActions()
        {
            int edges = 0; _motor.StatusGained += (who, kind) => { if (kind == StatusKind.Haunted) edges++; };
            _motor.ApplyHaunted(); Assert.AreEqual(7.5f, _motor.HauntedLeft);
            Assert.IsTrue(_motor.CanAct()); Assert.AreEqual(1f, _motor.StatusSpeedScale);
            Tick(2); _motor.ApplyHaunted(); Assert.AreEqual(7.5f, _motor.HauntedLeft);
            Assert.AreEqual(1, edges); Tick(7.51f); Assert.IsFalse(_motor.IsHaunted);
        }
        [Test] public void HauntedIgnoresStatusImmunityButAnExplicitCleanseStillEndsIt()
        {
            var system = _motor.gameObject.AddComponent<HeroAbilitySystem>(); system.BindHero("dante");
            using (NetCue.SuppressRelay()) system.Kit.Skill1.Activate(new AbilityContext(_motor, null, null));
            Assert.IsTrue(system.IsImmuneToStatuses);
            _motor.ApplyHaunted(); Assert.IsTrue(_motor.IsHaunted);
            _motor.CleanseStatuses(); Assert.IsFalse(_motor.IsHaunted);
            _motor.ApplyHaunted(); _motor.ClearStatuses(); Assert.IsFalse(_motor.IsHaunted);
        }
        [Test] public void AnObserverCannotCreateHauntedButCanAdoptAuthoritativeRemainingTime()
        {
            _peer.Host = false;
            _motor.ApplyHaunted(); Assert.IsFalse(_motor.IsHaunted);
            _motor.ApplyNetworkStatuses(0, 0, 0, 3);
            Assert.AreEqual(3f, _motor.HauntedLeft); Assert.IsTrue(_motor.IsHaunted);
            _motor.ApplyNetworkStatuses(0, 0, 0, 0); Assert.IsFalse(_motor.IsHaunted);
        }
        [Test] public void InvalidHauntDurationsCannotPoisonTheTimer()
        {
            _motor.ApplyHaunted(float.NaN); _motor.ApplyHaunted(float.PositiveInfinity); _motor.ApplyHaunted(-1);
            Assert.IsFalse(_motor.IsHaunted);
            _motor.ApplyHaunted(100); Assert.AreEqual(7.5f, _motor.HauntedLeft);
            _motor.ApplyNetworkStatuses(0, 0, 0, float.NaN); Assert.IsFalse(_motor.IsHaunted);
        }
        [UnityTest] public IEnumerator HauntedHidesBothRealHudMarkersAndExpiryRestoresThem()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike);
            GameServices.Round.BeginRound();
            var who = GameServices.Round.PlayerAt(1); who.enabled = false;
            who.IsDefender = false;
            var shoe = who.GetComponent<Carrier>().Held; Assert.IsNotNull(shoe);
            using (NetCue.SuppressRelay()) shoe.HostDisarm();
            shoe.enabled = false; shoe.transform.position = new Vector3(5, .15f, 5);
            who.Teleport(new Vector3(-5, .12f, -5));
            var can = GameServices.Round.Lata;
            var recall = new GameObject("Haunted recall").AddComponent<SlipperRecall>();
            recall.Build(recall.transform);
            var arrows = new GameObject("Haunted can marker").AddComponent<OffscreenIndicators>();
            Canvas.ForceUpdateCanvases();
            recall.Track(who, shoe); arrows.UpdateArrows(who, can.transform);
            Assert.IsTrue(recall.Drawing); Assert.IsTrue(arrows.CanMarkerVisible);
            who.ApplyHaunted(); recall.Track(who, shoe); arrows.UpdateArrows(who, can.transform);
            Assert.IsFalse(recall.Drawing); Assert.IsFalse(arrows.CanMarkerVisible);
            who.ApplyNetworkStatuses(0, 0, 0, 0);
            recall.Track(who, shoe); arrows.UpdateArrows(who, can.transform);
            Assert.IsTrue(recall.Drawing); Assert.IsTrue(arrows.CanMarkerVisible);
        }
    }
}
