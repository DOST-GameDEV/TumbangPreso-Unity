using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class MapPreviewTransitionTests
    {
        private bool _preview,_bots,_spectator;
        private int _seat;
        private string _map;private bool _pinned;private CustomRules _rules;
        [UnitySetUp]public IEnumerator Before()
        {
            _map=SceneFlow.SelectedMap;_pinned=SceneFlow.RulesPinned;_rules=SceneFlow.SelectedRules.Clone();
            _preview=MatchInstaller.PreviewOnly;_bots=GameLaunch.AllBots;
            _spectator=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            yield return PlayModeWorld.Reset();
            MatchInstaller.PreviewOnly=false;GameLaunch.AllBots=false;GameLaunch.Spectator=false;GameLaunch.SoloSeat=1;
        }
        [UnityTearDown]public IEnumerator After()
        {
            MatchInstaller.PreviewOnly=false;yield return PlayModeWorld.Reset();
            MatchInstaller.PreviewOnly=_preview;GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;GameLaunch.SoloSeat=_seat;
            SceneFlow.SelectedMap=_map;SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator TheLastDestinationWinsWhilePreviewLoadingFinishes()
        {
            var root=new GameObject("Changing destination during preview",typeof(RectTransform),typeof(RawImage));
            root.AddComponent<MapPreviewSurface>().Show(SceneFlow.SaBubong);
            Assert.IsTrue(MatchInstaller.PreviewOnly);
            SceneFlow.Go(SceneFlow.SaBubong);SceneFlow.Go(SceneFlow.MainMenu);
            float until=Time.realtimeSinceStartup+20;
            while(Time.realtimeSinceStartup<until&&UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!=SceneFlow.MainMenu)yield return null;
            Assert.AreEqual(SceneFlow.MainMenu,UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            Assert.IsFalse(MatchInstaller.PreviewOnly);Assert.IsTrue(root==null);
        }

        [UnityTest,Timeout(60000)]
        public IEnumerator StartingWhileTheMapPreviewLoadsBuildsTheRealMatch()
        {
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));SceneFlow.SelectedMap=SceneFlow.SaBubong;
            var root=new GameObject("Pending map preview",typeof(RectTransform),typeof(RawImage));
            var preview=root.AddComponent<MapPreviewSurface>();preview.Show(SceneFlow.SaBubong);
            Assert.IsTrue(MatchInstaller.PreviewOnly,"The regression must request start during the actual additive preview load");
            SceneFlow.StartMatch();
            float until=Time.realtimeSinceStartup+20;
            while(Time.realtimeSinceStartup<until&&(Hud.Instance==null||Object.FindAnyObjectByType<ReadyGate>()==null))yield return null;
            Assert.IsFalse(MatchInstaller.PreviewOnly,"A cancelled preview left the real installer disabled");
            Assert.IsTrue(root==null,"The preview setup survived the real single-scene match transition");
            Assert.IsNotNull(Hud.Instance,"The real match did not create its HUD");
            var gate=Object.FindAnyObjectByType<ReadyGate>();Assert.IsNotNull(gate,"The real match did not offer ready");
            Assert.IsTrue(gate.AwaitingReady);gate.StartLocalCountdown();
            until=Time.realtimeSinceStartup+6;
            while(Time.realtimeSinceStartup<until&&!GameServices.Round.RoundActive)yield return null;
            Assert.IsTrue(GameServices.Round.RoundActive,"The actual ready gate never began the round");
        }
    }
}
