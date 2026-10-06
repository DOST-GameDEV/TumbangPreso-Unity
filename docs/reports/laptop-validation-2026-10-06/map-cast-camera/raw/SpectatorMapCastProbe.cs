using System.Collections;
using System.Collections.Generic;
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
    public sealed class SpectatorMapCastProbe
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
        [UnityTest] public IEnumerator Eskinita_Classic_Group0()=>Capture(SceneFlow.Eskinita,GameMode.Classic,0);
        [UnityTest] public IEnumerator Eskinita_Classic_Group1()=>Capture(SceneFlow.Eskinita,GameMode.Classic,1);
        [UnityTest] public IEnumerator Eskinita_Classic_Group2()=>Capture(SceneFlow.Eskinita,GameMode.Classic,2);
        [UnityTest] public IEnumerator Eskinita_Classic_Group3()=>Capture(SceneFlow.Eskinita,GameMode.Classic,3);
        [UnityTest] public IEnumerator Eskinita_HeroStrike_Group0()=>Capture(SceneFlow.Eskinita,GameMode.HeroStrike,0);
        [UnityTest] public IEnumerator Eskinita_HeroStrike_Group1()=>Capture(SceneFlow.Eskinita,GameMode.HeroStrike,1);
        [UnityTest] public IEnumerator Eskinita_HeroStrike_Group2()=>Capture(SceneFlow.Eskinita,GameMode.HeroStrike,2);
        [UnityTest] public IEnumerator Eskinita_HeroStrike_Group3()=>Capture(SceneFlow.Eskinita,GameMode.HeroStrike,3);
        [UnityTest] public IEnumerator BayanPlaza_Classic_Group0()=>Capture(SceneFlow.BayanPlaza,GameMode.Classic,0);
        [UnityTest] public IEnumerator BayanPlaza_Classic_Group1()=>Capture(SceneFlow.BayanPlaza,GameMode.Classic,1);
        [UnityTest] public IEnumerator BayanPlaza_Classic_Group2()=>Capture(SceneFlow.BayanPlaza,GameMode.Classic,2);
        [UnityTest] public IEnumerator BayanPlaza_Classic_Group3()=>Capture(SceneFlow.BayanPlaza,GameMode.Classic,3);
        [UnityTest] public IEnumerator BayanPlaza_HeroStrike_Group0()=>Capture(SceneFlow.BayanPlaza,GameMode.HeroStrike,0);
        [UnityTest] public IEnumerator BayanPlaza_HeroStrike_Group1()=>Capture(SceneFlow.BayanPlaza,GameMode.HeroStrike,1);
        [UnityTest] public IEnumerator BayanPlaza_HeroStrike_Group2()=>Capture(SceneFlow.BayanPlaza,GameMode.HeroStrike,2);
        [UnityTest] public IEnumerator BayanPlaza_HeroStrike_Group3()=>Capture(SceneFlow.BayanPlaza,GameMode.HeroStrike,3);
        [UnityTest] public IEnumerator IlalimNgTulay_Classic_Group0()=>Capture(SceneFlow.IlalimNgTulay,GameMode.Classic,0);
        [UnityTest] public IEnumerator IlalimNgTulay_Classic_Group1()=>Capture(SceneFlow.IlalimNgTulay,GameMode.Classic,1);
        [UnityTest] public IEnumerator IlalimNgTulay_Classic_Group2()=>Capture(SceneFlow.IlalimNgTulay,GameMode.Classic,2);
        [UnityTest] public IEnumerator IlalimNgTulay_Classic_Group3()=>Capture(SceneFlow.IlalimNgTulay,GameMode.Classic,3);
        [UnityTest] public IEnumerator IlalimNgTulay_HeroStrike_Group0()=>Capture(SceneFlow.IlalimNgTulay,GameMode.HeroStrike,0);
        [UnityTest] public IEnumerator IlalimNgTulay_HeroStrike_Group1()=>Capture(SceneFlow.IlalimNgTulay,GameMode.HeroStrike,1);
        [UnityTest] public IEnumerator IlalimNgTulay_HeroStrike_Group2()=>Capture(SceneFlow.IlalimNgTulay,GameMode.HeroStrike,2);
        [UnityTest] public IEnumerator IlalimNgTulay_HeroStrike_Group3()=>Capture(SceneFlow.IlalimNgTulay,GameMode.HeroStrike,3);
        [UnityTest] public IEnumerator SaBubong_Classic_Group0()=>Capture(SceneFlow.SaBubong,GameMode.Classic,0);
        [UnityTest] public IEnumerator SaBubong_Classic_Group1()=>Capture(SceneFlow.SaBubong,GameMode.Classic,1);
        [UnityTest] public IEnumerator SaBubong_Classic_Group2()=>Capture(SceneFlow.SaBubong,GameMode.Classic,2);
        [UnityTest] public IEnumerator SaBubong_Classic_Group3()=>Capture(SceneFlow.SaBubong,GameMode.Classic,3);
        [UnityTest] public IEnumerator SaBubong_HeroStrike_Group0()=>Capture(SceneFlow.SaBubong,GameMode.HeroStrike,0);
        [UnityTest] public IEnumerator SaBubong_HeroStrike_Group1()=>Capture(SceneFlow.SaBubong,GameMode.HeroStrike,1);
        [UnityTest] public IEnumerator SaBubong_HeroStrike_Group2()=>Capture(SceneFlow.SaBubong,GameMode.HeroStrike,2);
        [UnityTest] public IEnumerator SaBubong_HeroStrike_Group3()=>Capture(SceneFlow.SaBubong,GameMode.HeroStrike,3);
        [UnityTest] public IEnumerator LagoonCove_Classic_Group0()=>Capture(SceneFlow.LagoonCove,GameMode.Classic,0);
        [UnityTest] public IEnumerator LagoonCove_Classic_Group1()=>Capture(SceneFlow.LagoonCove,GameMode.Classic,1);
        [UnityTest] public IEnumerator LagoonCove_Classic_Group2()=>Capture(SceneFlow.LagoonCove,GameMode.Classic,2);
        [UnityTest] public IEnumerator LagoonCove_Classic_Group3()=>Capture(SceneFlow.LagoonCove,GameMode.Classic,3);
        [UnityTest] public IEnumerator LagoonCove_HeroStrike_Group0()=>Capture(SceneFlow.LagoonCove,GameMode.HeroStrike,0);
        [UnityTest] public IEnumerator LagoonCove_HeroStrike_Group1()=>Capture(SceneFlow.LagoonCove,GameMode.HeroStrike,1);
        [UnityTest] public IEnumerator LagoonCove_HeroStrike_Group2()=>Capture(SceneFlow.LagoonCove,GameMode.HeroStrike,2);
        [UnityTest] public IEnumerator LagoonCove_HeroStrike_Group3()=>Capture(SceneFlow.LagoonCove,GameMode.HeroStrike,3);
        [UnityTest] public IEnumerator Kanto_Classic_Group0()=>Capture(SceneFlow.Kanto,GameMode.Classic,0);
        [UnityTest] public IEnumerator Kanto_Classic_Group1()=>Capture(SceneFlow.Kanto,GameMode.Classic,1);
        [UnityTest] public IEnumerator Kanto_Classic_Group2()=>Capture(SceneFlow.Kanto,GameMode.Classic,2);
        [UnityTest] public IEnumerator Kanto_Classic_Group3()=>Capture(SceneFlow.Kanto,GameMode.Classic,3);
        [UnityTest] public IEnumerator Kanto_HeroStrike_Group0()=>Capture(SceneFlow.Kanto,GameMode.HeroStrike,0);
        [UnityTest] public IEnumerator Kanto_HeroStrike_Group1()=>Capture(SceneFlow.Kanto,GameMode.HeroStrike,1);
        [UnityTest] public IEnumerator Kanto_HeroStrike_Group2()=>Capture(SceneFlow.Kanto,GameMode.HeroStrike,2);
        [UnityTest] public IEnumerator Kanto_HeroStrike_Group3()=>Capture(SceneFlow.Kanto,GameMode.HeroStrike,3);

        private static List<int> CoveringPicks(GameMode mode)
        {
            int count=Roster.GetPeople(mode).Count;
            var remaining=new HashSet<int>(Enumerable.Range(0,count));var picks=new List<int>();
            while(remaining.Count>0)
            {
                int best=-1,bestGain=0;
                for(int pick=0;pick<count;pick++)
                {
                    var ids=new HashSet<int>{pick};
                    foreach(int slot in new[]{0,2,3})ids.Add(MatchInstaller.ResolveAiCharacterIndex(slot,pick,mode));
                    int gain=ids.Count(remaining.Contains);
                    if(gain>bestGain){best=pick;bestGain=gain;}
                }
                Assert.Greater(bestGain,0);picks.Add(best);remaining.Remove(best);
                foreach(int slot in new[]{0,2,3})remaining.Remove(MatchInstaller.ResolveAiCharacterIndex(slot,best,mode));
            }
            return picks;
        }
        private IEnumerator Capture(string map,GameMode mode,int group)
        {
            var picks=CoveringPicks(mode);Assert.AreEqual(4,picks.Count,"Current complete covering set must match the four declared cases.");
            int pick=picks[group];string output="Logs/spectator-map-cast1006/"+map+"/"+mode+"-"+pick;
            Directory.CreateDirectory(output);
            NetAuthority.Provider=null;SceneFlow.SelectedMode=mode;SceneFlow.PinSelectedRules(CustomGameRules.Defaults(mode));
            Settings.SettingsStore.Current.CharacterPick=pick;
            GameLaunch.AllBots=false;GameLaunch.Spectator=false;GameLaunch.SoloSeat=1;
            GameLaunch.GuidedTutorial=false;GameLaunch.TrainingRange=false;
            yield return SceneManager.LoadSceneAsync(map);yield return null;
            var installer=Object.FindFirstObjectByType<MatchInstaller>();Assert.IsNotNull(installer);
            var round=GameServices.Round;Assert.IsNotNull(round);
            var cast=round.Players.Where(p=>p!=null).OrderBy(p=>p.PlayerSlot).ToArray();Assert.AreEqual(4,cast.Length);
            for(int slot=0;slot<4;slot++)Assert.AreEqual(slot==1?pick:MatchInstaller.ResolveAiCharacterIndex(slot,pick,mode),cast[slot].CharacterIndex);
            GameLaunch.Spectator=true;installer.RebindLocalSeat(-1,true);yield return null;
            foreach(var body in cast)
            {
                Assert.IsFalse(body.GetComponents<PlayerInputReader>().Any(r=>r.enabled));
                Assert.IsTrue(body.GetComponents<AIController>().Any(ai=>ai.enabled));
            }
            Assert.IsFalse(cast[1].IsBot,"One former human retains its seat provenance.");
            var runner=Object.FindFirstObjectByType<SliceRunner>();Assert.IsNotNull(runner);runner.Begin();
            var gate=Object.FindFirstObjectByType<ReadyGate>();
            while(MatchArrivalPresentation.Active || (gate!=null && (gate.AwaitingReady || gate.CountingDown)))yield return null;
            yield return null;yield return null;
            var watcher=Object.FindFirstObjectByType<SpectatorCamera>();Assert.IsNotNull(watcher);
            var director=watcher.GetComponent<SpectatorDirector>();Assert.IsNotNull(director);director.Engaged=true;
            var camera=watcher.GetComponent<Camera>();Assert.IsTrue(camera.enabled);
            File.WriteAllText(output+"/installed-cast.txt",string.Join("\n",cast.Select(p=>p.PlayerSlot+" "+Roster.PersonIdAt(mode,p.CharacterIndex)+" IsBot="+p.IsBot)));
            float start=Time.unscaledTime,clock=round.TimeLeft,sample=0;int image=0;
            while(Time.unscaledTime-start<15)
            {
                float elapsed=Time.unscaledTime-start;
                if(elapsed>=sample)
                {
                    sample=elapsed+.5f;
                    File.AppendAllText(output+"/timeline.tsv",elapsed.ToString("F3")+"\t"+round.TimeLeft.ToString("F3")+"\t"+director.Beat+"\t"+director.Shot+"\t"+director.ShotName()+"\t"+director.Cuts+"\t"+director.SafePoseFallbacks+"\n");
                }
                if(elapsed>=image*5 && image<3)
                {
                    yield return GameplayShots.Render(camera,"cast-natural-"+image,flipCanvases:true,outDir:output,width:1600,height:900);image++;
                }
                yield return null;
            }
            Assert.AreEqual(3,Directory.GetFiles(output,"cast-natural-*.png").Length);
            Assert.Less(round.TimeLeft,clock);
        }
    }
}
