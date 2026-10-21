using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorUltimateFootprintTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private INetProvider _previous;
        private GameObject _root;
        private CharacterMotor _actor;
        private HeroAbilitySystem _abilities;
        private SpectatorInterestModel _model;
        private sealed class Solo : INetProvider
        { public bool IsHost => true; public bool IsNetworked => false; public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false; }
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset(); _previous = NetAuthority.Provider; NetAuthority.Provider = new Solo();
            GameServices.Ensure(); GameServices.Round.Clear(); GameServices.Match.ApplySnapshot(new int[4], 1, true); GameServices.Round.BeginRound();
            _root = new GameObject("Ultimate interest source"); _actor = _root.AddComponent<CharacterMotor>(); _actor.enabled = false;
            _actor.PlayerSlot = 1; _actor.Mode = GameMode.HeroStrike; _actor.RoundActive = true; _actor.IsDefender = false;
            _abilities = _root.AddComponent<HeroAbilitySystem>(); _abilities.enabled = false;
            GameServices.Round.Register(_actor); _model = new SpectatorInterestModel(); _model.Hook();
            yield return null;
        }
        [UnityTearDown] public IEnumerator After()
        {
            _model?.Unhook(); if (_root != null) Object.Destroy(_root);
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _previous;
        }
        private void Pulse(string hero)
        {
            _abilities.BindHero(hero);
            var kit = _abilities.Kit; Assert.IsFalse(kit.Ultimate.IsActive); Assert.IsFalse(kit.Ultimate.IsWindingUp);
            // Isolate the accepted-presentation notification after an instant cast.
            // This does not execute/force an ability or qualify natural gameplay.
            typeof(SpectatorInterestModel).GetMethod("OnUltimateStarted", Hidden).Invoke(_model, new object[] { _actor, kit, kit.Ultimate });
        }
        [TestCase("cheska"),TestCase("dante"),TestCase("amihan")]
        public void NonCircularCourtEffectsRequestTheWideShot(string hero)
        {
            Pulse(hero);Assert.IsFalse(_abilities.Kit.Ultimate.HasTelegraph);
            var interest=_model.Decide();Assert.AreEqual(SpectatorBeat.Ultimate,interest.Beat);
            Assert.AreEqual(ShotType.UltimateWide,interest.Shot);
        }
        [Test] public void ExistingCircularFloodRetainsItsWideShot()
        { Pulse("rafi");Assert.AreEqual(ShotType.UltimateWide,_model.Decide().Shot); }
        [Test] public void ExistingOverclockZapRetainsItsWideShot()
        { Pulse("zack");Assert.AreEqual(ShotType.UltimateWide,_model.Decide().Shot); }
    }
}
