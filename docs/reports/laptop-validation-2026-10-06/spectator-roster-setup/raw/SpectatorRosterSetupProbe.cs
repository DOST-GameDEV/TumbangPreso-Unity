using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SpectatorRosterSetupProbe
    {
        private bool _bots,_watch,_guided,_training,_pinned;
        private int _seat,_pick;
        private GameMode _mode;
        private CustomRules _rules;
        private INetProvider _provider;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots=GameLaunch.AllBots;_watch=GameLaunch.Spectator;_seat=GameLaunch.SoloSeat;
            _guided=GameLaunch.GuidedTutorial;_training=GameLaunch.TrainingRange;
            _mode=SceneFlow.SelectedMode;_rules=SceneFlow.SelectedRules.Clone();_pinned=SceneFlow.RulesPinned;
            _pick=Settings.SettingsStore.Current.CharacterPick;_provider=NetAuthority.Provider;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            Settings.SettingsStore.Current.CharacterPick=_pick;NetAuthority.Provider=_provider;
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_watch;GameLaunch.SoloSeat=_seat;
            GameLaunch.GuidedTutorial=_guided;GameLaunch.TrainingRange=_training;SceneFlow.SelectedMode=_mode;
            SceneFlow.AdoptRemoteRules(_rules);
            if(_pinned)SceneFlow.PinSelectedRules(_rules);else SceneFlow.UnpinSelectedRules();
        }
        [UnityTest] public IEnumerator PublicPickThenWatchPreservesInstalledCastAndGivesFourAiDrivers()
        {
            const int pick=1;
            NetAuthority.Provider=null;SceneFlow.SelectedMode=GameMode.HeroStrike;
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            Settings.SettingsStore.Current.CharacterPick=pick;
            GameLaunch.AllBots=false;GameLaunch.Spectator=false;GameLaunch.SoloSeat=1;
            GameLaunch.GuidedTutorial=false;GameLaunch.TrainingRange=false;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Arena);
            yield return null;
            var installer=Object.FindFirstObjectByType<MatchInstaller>();Assert.IsNotNull(installer);
            var round=GameServices.Round;Assert.IsNotNull(round);
            var cast=round.Players.Where(p=>p!=null).OrderBy(p=>p.PlayerSlot).ToArray();Assert.AreEqual(4,cast.Length);
            for(int slot=0;slot<4;slot++)
                Assert.AreEqual(slot==1?pick:MatchInstaller.ResolveAiCharacterIndex(slot,pick,GameMode.HeroStrike),cast[slot].CharacterIndex);
            var ids=cast.Select(p=>p.CharacterIndex).ToArray();
            Assert.IsFalse(cast[1].IsBot,"The selected seat begins as a human.");
            GameLaunch.Spectator=true;installer.RebindLocalSeat(-1,true);
            yield return null;
            foreach(var body in cast)
            {
                Assert.IsFalse(body.GetComponents<PlayerInputReader>().Any(r=>r.enabled));
                Assert.IsTrue(body.GetComponents<AIController>().Any(ai=>ai.enabled),"Every body needs an actual AI input source.");
            }
            CollectionAssert.AreEqual(ids,cast.Select(p=>p.CharacterIndex).ToArray());
            Assert.IsFalse(cast[1].IsBot,"Input handoff must not be misreported as rewriting seat provenance.");
            var camera=Object.FindFirstObjectByType<SpectatorCamera>();Assert.IsNotNull(camera);Assert.IsTrue(camera.enabled);
            // Autopilot is installed lazily by spectator Update, after arrival owns the view.
            var runner=Object.FindFirstObjectByType<SliceRunner>();Assert.IsNotNull(runner);runner.Begin();
            var gate=Object.FindFirstObjectByType<ReadyGate>();
            while(MatchArrivalPresentation.Active || (gate!=null && (gate.AwaitingReady || gate.CountingDown))) yield return null;
            yield return null; yield return null;
            Assert.IsNotNull(camera.GetComponent<SpectatorDirector>());
            Directory.CreateDirectory("Logs/spectator-roster-setup1006");
            File.WriteAllText("Logs/spectator-roster-setup1006/installed-cast.txt",string.Join("\n",cast.Select(p=>p.PlayerSlot+" "+Roster.PersonIdAt(p.Mode,p.CharacterIndex)+" IsBot="+p.IsBot+" AI="+p.GetComponents<AIController>().Any(ai=>ai.enabled))));
        }
    }
}
