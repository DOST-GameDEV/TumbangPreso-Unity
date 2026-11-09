using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class RetrievalMomentsTests
    {
        private sealed class Solo : INetProvider
        {
            public bool IsHost => true; public bool IsNetworked => false;
            public int LocalSlot => 1; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }

        private INetProvider _provider;
        private MatchStatsCollector _stats;
        private GameObject _actorBody, _tayaBody, _canBody, _cueBody;
        private CharacterMotor _actor, _taya;
        private bool _network, _training, _tutorial, _spectator, _allBots;
        private int _escapes, _closeEscapes;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _provider = NetAuthority.Provider; NetAuthority.Provider = new Solo();
            _network = SceneFlow.Networked; SceneFlow.Networked = false;
            _training = GameLaunch.TrainingRange; _tutorial = GameLaunch.GuidedTutorial;
            _spectator = GameLaunch.Spectator; _allBots = GameLaunch.AllBots;
            GameLaunch.TrainingRange = GameLaunch.GuidedTutorial = GameLaunch.Spectator = GameLaunch.AllBots = false;
            Hitstop.End(); PresentationClock.RequestScale(1);
            SceneManager.SetActiveScene(SceneManager.CreateScene(SceneFlow.Eskinita)); GameServices.Ensure();
            _stats = GameServices.Stats; typeof(GameServices).GetProperty("Stats").SetValue(null, null);
            GameServices.Round.BeginRound(); GameServices.Round.enabled = false;
            _actorBody = new GameObject("Retrieval escape attacker");
            _actor = _actorBody.AddComponent<CharacterMotor>(); _actor.enabled = false;
            _actor.PlayerSlot = 1; _actor.Mode = GameMode.Classic; _actor.IsDefender = false;
            _actor.RoundActive = true; _actor.HoldingSlipper = true;
            _tayaBody = new GameObject("Retrieval escape taya");
            _taya = _tayaBody.AddComponent<CharacterMotor>(); _taya.enabled = false;
            _taya.PlayerSlot = 0; _taya.Mode = GameMode.Classic; _taya.IsDefender = true; _taya.RoundActive = true;
            _taya.transform.position = Vector3.back * (Confinement.Radius + 4);
            GameServices.Round.Register(_actor); GameServices.Round.Register(_taya);
            _canBody = new GameObject("Retrieval upright can");
            var can = _canBody.AddComponent<Lata>(); can.enabled = false; GameServices.Round.Lata = can;
            _cueBody = new GameObject("Retrieval moments under test"); _cueBody.AddComponent<RetrievalMoments>();
            _escapes = RetrievalMoments.EscapeCount; _closeEscapes = RetrievalMoments.CloseEscapeCount;
            Assert.IsTrue(_actor.IsInsideBox()); Assert.IsTrue(_actor.IsTaggable()); Assert.IsTrue(can.IsUpright);
            yield return null;
        }

        [UnityTearDown] public IEnumerator After()
        {
            if (_cueBody != null) Object.Destroy(_cueBody);
            if (_actorBody != null) Object.Destroy(_actorBody);
            if (_tayaBody != null) Object.Destroy(_tayaBody);
            if (_canBody != null) Object.Destroy(_canBody);
            yield return null;
            typeof(GameServices).GetProperty("Stats").SetValue(null, _stats);
            NetAuthority.Provider = _provider; SceneFlow.Networked = _network;
            GameLaunch.TrainingRange = _training; GameLaunch.GuidedTutorial = _tutorial;
            GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _allBots;
            Hitstop.End(); PresentationClock.RequestScale(1); yield return PlayModeWorld.Reset();
        }

        private static Vector3 Outside => Vector3.right * (Confinement.Radius + 0.25f);

        [UnityTest] public IEnumerator CarryingTheShoeAcrossTheBoundaryPresentsOneEscape()
        {
            _actor.transform.position = Outside;
            yield return null;
            Assert.IsFalse(_actor.IsInsideBox()); Assert.IsFalse(_actor.IsTaggable());
            Assert.AreEqual(_escapes + 1, RetrievalMoments.EscapeCount);
            Assert.AreEqual(_closeEscapes, RetrievalMoments.CloseEscapeCount);
            yield return null;
            Assert.AreEqual(_escapes + 1, RetrievalMoments.EscapeCount, "Remaining safe replayed the escape.");
        }

        [UnityTest] public IEnumerator TeleportingOutsideNeverPresentsAnEscape()
        {
            _actor.Teleport(Outside);
            yield return null;
            Assert.IsFalse(_actor.IsInsideBox(), "Fixture teleport must actually cross the boundary.");
            Assert.AreEqual(_escapes, RetrievalMoments.EscapeCount);
            Assert.AreEqual(_closeEscapes, RetrievalMoments.CloseEscapeCount);
        }

        [UnityTest] public IEnumerator EscapingAfterTheTayaCameClosePresentsACloseEscape()
        {
            _taya.transform.position = _actor.transform.position + Vector3.forward * 2;
            yield return null;
            _taya.transform.position = Vector3.back * (Confinement.Radius + 4);
            _actor.transform.position = Outside;
            yield return null;
            Assert.AreEqual(_escapes + 1, RetrievalMoments.EscapeCount);
            Assert.AreEqual(_closeEscapes + 1, RetrievalMoments.CloseEscapeCount, "The run forgot its closest approach.");
        }

        [UnityTest] public IEnumerator StillVulnerableOutsideNeverPresentsSafety()
        {
            _actor.ApplyVulnerable(10);
            yield return null;
            _actor.transform.position = Outside;
            yield return null;
            Assert.IsTrue(_actor.IsTaggable(), "Vulnerable remains taggable outside the box.");
            Assert.AreEqual(_escapes, RetrievalMoments.EscapeCount, "A still-taggable player was told they escaped.");
            Assert.AreEqual(_closeEscapes, RetrievalMoments.CloseEscapeCount);
        }
    }
}
