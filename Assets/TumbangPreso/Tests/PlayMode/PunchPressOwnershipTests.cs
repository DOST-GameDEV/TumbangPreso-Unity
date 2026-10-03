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
    public sealed class PunchPressOwnershipTests
    {
        private sealed class Client : INetProvider
        {
            public bool IsHost => false; public bool IsNetworked => true;
            public int LocalSlot => 0; public int LocalPeerId => 2; public bool IsSeatlessReferee => false;
        }
        private sealed class Solo : INetProvider
        {
            public bool IsHost => true; public bool IsNetworked => false;
            public int LocalSlot => 0; public int LocalPeerId => 0; public bool IsSeatlessReferee => false;
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
            _body = new GameObject("Predictive defender punch input");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 0; _motor.Mode = GameMode.Classic; _motor.IsDefender = true; _motor.RoundActive = true;
            _verbs = _body.AddComponent<CombatVerbs>(); _verbs.enabled = false;
            NetAuthority.Provider = new Client(); _motor.Intent.Clear(); _motor.Intent.CommitFrame(); _motor.Intent.Parked = false;
            yield return null;
            Assert.IsTrue(_motor.CanAct()); Assert.IsTrue(NetAuthority.ShouldRequest()); Assert.IsNull(MatchRpc.Instance);
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
        private void Predict()
        {
            _motor.Intent.Set(Verb.SpecialAbility, true); Assert.IsTrue(_motor.Intent.JustPressed(Verb.SpecialAbility));
            Step(); Assert.Greater(_verbs.PunchCooldownLeft, 0);
        }
        private void Refuse()
        {
            _verbs.RollBackRefusedVerb(MatchRpc.DeniedVerb.Punch); Assert.Zero(_verbs.PunchCooldownLeft);
        }
        [Test] public void RefusalCannotReuseTheAlreadyPredictedPunchEdgeBeforeCommit()
        {
            Predict(); Refuse(); Assert.IsTrue(_motor.Intent.JustPressed(Verb.SpecialAbility)); Step();
            Assert.Zero(_verbs.PunchCooldownLeft, "The same already-predicted punch edge committed again after refusal.");
            Assert.IsTrue(_motor.Intent.JustPressed(Verb.SpecialAbility), "The consumer must not commit the producer's edge.");
        }
        [Test] public void CommittingAHeldPressPreventsARepeatedPunchAfterRefusal()
        {
            Predict(); Refuse(); _motor.Intent.CommitFrame(); Assert.IsFalse(_motor.Intent.JustPressed(Verb.SpecialAbility));
            Step(); Assert.Zero(_verbs.PunchCooldownLeft); Assert.IsTrue(_motor.Intent.Pressed(Verb.SpecialAbility));
        }
        [Test] public void ReleaseCommitAndFreshPressCanPredictAnotherPunch()
        {
            Predict(); Refuse(); _motor.Intent.Set(Verb.SpecialAbility, false); Step(); _motor.Intent.CommitFrame();
            _motor.Intent.Set(Verb.SpecialAbility, true); Step(); Assert.Greater(_verbs.PunchCooldownLeft, 0);
        }
        [Test] public void ACooldownBlockedInitialEdgeIsStillAvailableBeforePrediction()
        {
            NetAuthority.Provider = new Solo(); Assert.IsTrue(_verbs.HostResolvePunch(_body.transform.position, Vector3.forward));
            NetAuthority.Provider = new Client(); Assert.Greater(_verbs.PunchCooldownLeft, 0);
            _motor.Intent.Set(Verb.SpecialAbility, true); Step(); Refuse();
            Assert.IsTrue(_motor.Intent.JustPressed(Verb.SpecialAbility)); Step(); Assert.Greater(_verbs.PunchCooldownLeft, 0);
        }
    }
}
