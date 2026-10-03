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
    public sealed class ShoveSlidePressRecoveryTests
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
        private GameObject _body, _shoeBody;
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
            _body = new GameObject("Registered shared-button recovery actor");
            _motor = _body.AddComponent<CharacterMotor>(); _motor.enabled = false;
            _motor.PlayerSlot = 0; _motor.Mode = GameMode.Classic;
            _verbs = _body.AddComponent<CombatVerbs>(); _verbs.enabled = false;
            GameServices.Round.Register(_motor); NetAuthority.Provider = new Client(); Role(2);
            _motor.Intent.Clear(); _motor.Intent.CommitFrame(); _motor.Intent.Parked = false;
            yield return null;
            Assert.IsTrue(_motor.CanAct()); Assert.IsFalse(_motor.IsDefender);
            Assert.IsTrue(NetAuthority.ShouldRequest()); Assert.IsNull(MatchRpc.Instance);
        }
        [UnityTearDown] public IEnumerator After()
        {
            GameServices.Round.Unregister(_motor);
            if (_body != null) Object.Destroy(_body); if (_shoeBody != null) Object.Destroy(_shoeBody);
            yield return null;
            typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            typeof(MatchRpc).GetProperty("Instance").SetValue(null, _rpc); NetAuthority.Provider = _provider;
            SceneFlow.Networked = _network; GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots; PracticeSandbox.Wanted = _sandbox;
            Hitstop.End(); PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }
        private void Role(int round)
        {
            GameServices.Match.ApplySnapshot(new int[Balance.PlayerCount], round, true);
            GameServices.Round.ApplySnapshot(80, true, (round - 1) % Balance.PlayerCount, true);
            Assert.IsTrue(_motor.RoundActive);
        }
        private void Step() => typeof(CombatVerbs).GetMethod("Update", Hidden).Invoke(_verbs, null);
        private void PredictShove()
        {
            float stamina = _motor.Stamina.Current;
            _motor.Intent.Set(Verb.Lunge, true); Assert.IsTrue(_motor.Intent.JustPressed(Verb.Lunge)); Step();
            Assert.Greater(_verbs.ShoveCooldownLeft, 0); Assert.Zero(_verbs.SlideCooldownLeft);
            Assert.AreEqual(stamina, _motor.Stamina.Current, .001f, "The owner-approved shove remains free.");
        }
        private void RefuseShove()
        {
            _verbs.RollBackRefusedVerb(MatchRpc.DeniedVerb.Shove); Assert.Zero(_verbs.ShoveCooldownLeft);
        }
        private void Release() { _motor.Intent.Set(Verb.Lunge, false); Step(); _motor.Intent.CommitFrame(); }
        [Test] public void ShoveReleaseDuringStunRearmsTheFreshAttackerPress()
        {
            PredictShove(); RefuseShove(); _motor.ApplyStagger(.5f); Assert.IsFalse(_motor.CanAct());
            Release(); _motor.ClearStun(); Assert.IsTrue(_motor.CanAct()); PredictShove();
        }
        [Test] public void ShoveReleaseDuringDefenderRoleRearmsTheFreshAttackerPress()
        {
            PredictShove(); RefuseShove(); Role(1); Assert.IsTrue(_motor.IsDefender);
            Release(); Role(2); Assert.IsFalse(_motor.IsDefender); PredictShove();
        }
        [Test] public void OrdinaryShoveReleaseAndFreshPressStillWork()
        { PredictShove(); RefuseShove(); Release(); PredictShove(); }
        [Test] public void AnInitiallyBlockedShoveDoesNotSpendItsInputEdge()
        {
            NetAuthority.Provider = new Solo(); _verbs.HostResolveShove(_body.transform.position, Vector3.forward);
            NetAuthority.Provider = new Client(); Assert.Greater(_verbs.ShoveCooldownLeft, 0);
            _motor.Intent.Set(Verb.Lunge, true); Step(); RefuseShove();
            Assert.IsTrue(_motor.Intent.JustPressed(Verb.Lunge)); Step(); Assert.Greater(_verbs.ShoveCooldownLeft, 0);
        }
    }
}
