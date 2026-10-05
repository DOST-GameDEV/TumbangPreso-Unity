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
    public sealed class CustomBotPolicyLoadTests
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
        [UnityTest] public IEnumerator CustomNoneSurvivesEnabledPreferenceAtActualInstall()
        {
            Rules(0); Settings.SettingsStore.Current.AiDifficulty=(int)Difficulty.Normal;
            AIController.ApplyDifficulty(AIController.NoBotsIndex);
            for(int i=1;i<Balance.PlayerCount;i++) _session.Lobby.Admit(100+i,"load-human-"+i,"Logical human"+i);
            yield return LoadActualArena();
            Assert.IsFalse(AIController.BotsEnabled,"Actual custom NONE install reloaded an unrelated enabled preference.");
            Assert.AreEqual((int)Difficulty.Normal,Settings.SettingsStore.Current.AiDifficulty,"Session policy must not overwrite this machine's preference.");
        }
        [UnityTest] public IEnumerator CustomEnabledTierSurvivesNonePreferenceAtActualInstall()
        {
            Rules(CustomGameRules.MaxBots,Difficulty.Astig);
            Settings.SettingsStore.Current.AiDifficulty=AIController.NoBotsIndex;
            AIController.ApplyDifficulty((int)Difficulty.Astig);
            yield return LoadActualArena();
            Assert.IsTrue(AIController.BotsEnabled&&AIController.ActiveDifficulty==Difficulty.Astig,
                "Actual custom install must obey the current enabled room tier rather than saved NONE.");
            Assert.Greater(Object.FindObjectsByType<AIController>(FindObjectsSortMode.None).Length,0,
                "Enabled room policy must reach actual bot controller installation.");
            Assert.AreEqual(AIController.NoBotsIndex,Settings.SettingsStore.Current.AiDifficulty);
        }
        [UnityTest] public IEnumerator QueuedAcceptedBotsKeepTheirPreferenceThroughActualInstall()
        {
            Rules(0); Settings.SettingsStore.Current.AiDifficulty=(int)Difficulty.Astig;
            HubQueueWatch.Begin(GameMode.HeroStrike,QueueStake.Casual); HubQueueWatch.AcceptBots();
            AIController.ApplyDifficulty((int)Difficulty.Astig);
            yield return LoadActualArena();
            Assert.IsTrue(HubQueueWatch.QueueRoom,"Queue ownership must survive into the actual install.");
            Assert.IsTrue(AIController.BotsEnabled&&AIController.ActiveDifficulty==Difficulty.Astig);
        }
    }
}
