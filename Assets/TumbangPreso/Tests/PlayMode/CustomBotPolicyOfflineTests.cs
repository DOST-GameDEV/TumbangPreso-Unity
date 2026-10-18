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
    public sealed class CustomBotPolicyOfflineTests
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
        private IEnumerator LoadActualArena()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlow.Arena);
            yield return null; yield return null;
            var installer=Object.FindFirstObjectByType<MatchInstaller>();
            Assert.IsNotNull(installer);
            Assert.IsTrue(installer.IsPrepared, installer.InstallationError);
        }
        [UnityTest] public IEnumerator OfflineNoneStillUsesSavedPreferenceAndKeepsOnlyHumanSeat()
        {
            _session.Stop(); SceneFlow.Networked=false; HubQueueWatch.End();
            Rules(CustomGameRules.MaxBots,Difficulty.Astig);
            Settings.SettingsStore.Current.AiDifficulty=AIController.NoBotsIndex;
            AIController.ApplyDifficulty((int)Difficulty.Astig);
            yield return LoadActualArena();
            Assert.IsFalse(AIController.BotsEnabled,"Offline saved NONE must not inherit network room policy.");
            Assert.AreEqual(1,Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).Length,
                "Offline NONE must retain its single human seat, not standing bot bodies.");
            Assert.AreEqual(AIController.NoBotsIndex,Settings.SettingsStore.Current.AiDifficulty);
        }
    }
}
