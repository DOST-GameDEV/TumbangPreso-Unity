using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class PunchPressReleaseRecoveryTests
    {
        private sealed class Client : INetProvider
        {
            public bool IsHost => false; public bool IsNetworked => true;
            public int LocalSlot => 0; public int LocalPeerId => 2; public bool IsSeatlessReferee => false;
        }
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _body;
        private CharacterMotor _motor;
        private CombatVerbs _verbs;
        private INetProvider _provider;
        private MatchRpc _rpc;
        private MatchStatsCollector _stats;
        private bool _network, _training, _tutorial, _spectator, _allBots, _sandbox;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; _rpc = MatchRpc.Instance;
            typeof(MatchRpc).GetProperty("Instance").SetValue(null, null);
            _network = SceneFlow.Networked; SceneFlow.Networked = false;
            _training = GameLaunch.TrainingRange; _tutorial = GameLaunch.GuidedTutorial;
            _spectator = GameLaunch.Spectator; _allBots = GameLaunch.AllBots; _sandbox = PracticeSandbox.Wanted;
            GameLaunch.TrainingRange = GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false;
            PracticeSandbox.Clear(); Hitstop.End(); PresentationClock.RequestScale(1);
            SceneManager.SetActiveScene(SceneManager.CreateScene(SceneFlow.Eskinita)); GameServices.Ensure();
            _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            _body = new GameObject("Interrupted punch press recovery");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 0; _motor.Mode = GameMode.Classic; _motor.IsDefender = true; _motor.RoundActive = true;
            _verbs = _body.AddComponent<CombatVerbs>(); _verbs.enabled = false;
            NetAuthority.Provider = new Client(); _motor.Intent.Clear(); _motor.Intent.CommitFrame(); _motor.Intent.Parked = false;
            yield return null; Assert.IsTrue(_motor.CanAct()); Assert.IsTrue(NetAuthority.ShouldRequest()); Assert.IsNull(MatchRpc.Instance);
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_body != null) Object.Destroy(_body); yield return null;
            typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            typeof(MatchRpc).GetProperty("Instance").SetValue(null, _rpc); NetAuthority.Provider = _provider;
            SceneFlow.Networked = _network; GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots; PracticeSandbox.Wanted = _sandbox;
            Hitstop.End(); PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }
        private void Step() => typeof(CombatVerbs).GetMethod("Update", Hidden).Invoke(_verbs, null);
        private void RefusedPrediction()
        {
            _motor.Intent.Set(Verb.SpecialAbility, true); Step(); Assert.Greater(_verbs.PunchCooldownLeft, 0);
            _verbs.RollBackRefusedVerb(MatchRpc.DeniedVerb.Punch); Assert.Zero(_verbs.PunchCooldownLeft);
        }
        private void FreshPunch()
        {
            _motor.Intent.CommitFrame(); _motor.Intent.Set(Verb.SpecialAbility, true);
            Assert.IsTrue(_motor.Intent.JustPressed(Verb.SpecialAbility)); Step(); Assert.Greater(_verbs.PunchCooldownLeft, 0);
        }
        [Test] public void AReleaseObservedDuringStunAllowsAFreshPunchAfterRecovery()
        {
            RefusedPrediction(); _motor.ApplyStagger(.5f); Assert.IsTrue(_motor.IsStunned); Assert.IsFalse(_motor.CanAct());
            _motor.Intent.Set(Verb.SpecialAbility, false); Step(); Assert.Zero(_verbs.PunchCooldownLeft);
            _motor.ClearStun(); Assert.IsTrue(_motor.CanAct()); FreshPunch();
        }
        [Test] public void AReleaseObservedInTheAttackerRoleAllowsAFreshDefenderPunch()
        {
            RefusedPrediction(); _motor.IsDefender = false;
            _motor.Intent.Set(Verb.SpecialAbility, false); Step(); Assert.Zero(_verbs.PunchCooldownLeft);
            _motor.IsDefender = true; FreshPunch();
        }
    }
}
