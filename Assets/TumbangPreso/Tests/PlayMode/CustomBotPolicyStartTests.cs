using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class CustomBotPolicyStartTests
    {
        private Scene _beforeScene, _preparation;
        private GameObject _controllerRoot;
        private ConvertedMatchSetup _controller;
        private NetSession _session;
        private CustomRules _rules;
        private bool _pinned, _networked, _bots;
        private Difficulty _difficulty;
        private int _savedDifficulty;
        [UnitySetUp] public IEnumerator Before()
        {
            _rules=SceneFlow.SelectedRules.Clone(); _pinned=SceneFlow.RulesPinned;
            _networked=SceneFlow.Networked; _bots=AIController.BotsEnabled;
            _difficulty=AIController.ActiveDifficulty;
            _savedDifficulty=Settings.SettingsStore.Current.AiDifficulty;
            yield return PlayModeWorld.Reset();
            HubQueueWatch.End();
            _beforeScene=SceneManager.GetActiveScene();
            _preparation=SceneManager.CreateScene(SceneFlow.MatchSetup);
            SceneManager.SetActiveScene(_preparation);
            _controllerRoot=new GameObject("Inactive custom bot policy controller");
            _controllerRoot.SetActive(false);
            _controller=_controllerRoot.AddComponent<ConvertedMatchSetup>();
            _session=NetSession.Ensure();
            var start=_session.StartHostAsync(18765);
            while(!start.IsCompleted) yield return null;
            Assert.IsTrue(start.Result); Assert.IsTrue(_session.IsHost);
            SceneFlow.Networked=true;
            Assert.IsNotNull(MatchRpc.Instance);
            Assert.IsFalse(MatchRpc.Instance.CharacterSelecting);
        }
        [UnityTearDown] public IEnumerator After()
        {
            _session?.Stop(); HubQueueWatch.End();
            if(_controllerRoot!=null) Object.DestroyImmediate(_controllerRoot);
            if(_beforeScene.IsValid()&&_beforeScene.isLoaded) SceneManager.SetActiveScene(_beforeScene);
            if(_preparation.IsValid()&&_preparation.isLoaded) yield return SceneManager.UnloadSceneAsync(_preparation);
            yield return PlayModeWorld.Reset();
            SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
            SceneFlow.Networked=_networked;
            Settings.SettingsStore.Current.AiDifficulty=_savedDifficulty;
            AIController.ApplyDifficulty((int)_difficulty); AIController.BotsEnabled=_bots;
        }
        private void Rules(int bots, Difficulty difficulty=Difficulty.Normal)
        {
            var rules=CustomGameRules.Defaults(GameMode.HeroStrike);
            rules.Bots=bots; rules.BotDifficulty=(int)difficulty;
            SceneFlow.AdoptRemoteRules(rules);
        }
        [UnityTest] public IEnumerator NoneRulesRefuseVacantSeatsDespiteEnabledSavedPolicy()
        {
            Rules(0); AIController.ApplyDifficulty((int)Difficulty.Normal);
            _controller.StartGame(); yield return null;
            Assert.IsTrue(!MatchRpc.Instance.CharacterSelecting&&!AIController.BotsEnabled,
                "Current NONE room must keep the existing four-human start gate and retire stale enabled policy.");
        }
        [UnityTest] public IEnumerator EnabledRoomRulesStartDespiteDisabledSavedPolicy()
        {
            Rules(CustomGameRules.MaxBots,Difficulty.Astig);
            AIController.ApplyDifficulty(AIController.NoBotsIndex);
            _controller.StartGame(); yield return null;
            Assert.IsTrue(MatchRpc.Instance.CharacterSelecting&&AIController.BotsEnabled&&AIController.ActiveDifficulty==Difficulty.Astig,
                "Allowed room bot policy must reach the actual selection consumer with its current difficulty.");
        }
        [UnityTest] public IEnumerator FourHumanSeatsCanStartWithNone()
        {
            Rules(0); AIController.ApplyDifficulty(AIController.NoBotsIndex);
            // Logical roster admissions on one real host, not three remote socket clients.
            for(int i=1;i<Balance.PlayerCount;i++) _session.Lobby.Admit(100+i,"bot-policy-human-"+i,"Logical human"+i);
            Assert.AreEqual(Balance.PlayerCount,_session.Lobby.OccupiedSeatCount());
            _controller.StartGame(); yield return null;
            Assert.IsTrue(MatchRpc.Instance.CharacterSelecting); Assert.IsFalse(AIController.BotsEnabled);
        }
        [UnityTest] public IEnumerator AlreadyDisabledNoneStillRefusesVacantSeats()
        {
            Rules(0); AIController.ApplyDifficulty(AIController.NoBotsIndex);
            _controller.StartGame(); yield return null;
            Assert.IsFalse(MatchRpc.Instance.CharacterSelecting); Assert.IsFalse(AIController.BotsEnabled);
        }
        [UnityTest] public IEnumerator QueueKeepsItsExplicitAcceptedBotPath()
        {
            Rules(0); AIController.ApplyDifficulty(AIController.NoBotsIndex);
            HubQueueWatch.Begin(GameMode.HeroStrike,QueueStake.Casual); HubQueueWatch.AcceptBots();
            _controller.StartGame(); yield return null;
            Assert.IsTrue(AIController.BotsEnabled); Assert.IsTrue(MatchRpc.Instance.QueueMapVoting);
        }
    }
}
