using System.Collections;
using System.Reflection;
using TumbangPreso.Abilities;
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
    [Timeout(1000000)]
    public sealed class SpectatorCorrectedUltimateProbe
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
        [UnityTest] public IEnumerator HeroGroup0()=>Capture(GameMode.HeroStrike,0);

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
        private IEnumerator Capture(GameMode mode,int group)
        {
            var picks=CoveringPicks(mode);Assert.AreEqual(4,picks.Count,"Current complete covering set must match the four declared cases.");
            int pick=picks[group];string output="Logs/spectator-corrected-ultimate1006/observations/"+mode+"-"+pick;
            Directory.CreateDirectory(output);
            NetAuthority.Provider=null;SceneFlow.SelectedMode=mode;SceneFlow.PinSelectedRules(CustomGameRules.Defaults(mode));
            Settings.SettingsStore.Current.CharacterPick=pick;
            GameLaunch.AllBots=false;GameLaunch.Spectator=false;GameLaunch.SoloSeat=1;
            GameLaunch.GuidedTutorial=false;GameLaunch.TrainingRange=false;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Arena);yield return null;
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
            float start=Time.unscaledTime,clock=round.TimeLeft,sample=0;int image=0;int beganRound=GameServices.Match.RoundNumber;
            var stamps = new Dictionary<string,float>(); var captured = new HashSet<string>();
            var answered = typeof(HeroAbilitySystem).GetField("_answeredAt", BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.IsNotNull(answered);
            File.WriteAllText(output+"/answers.tsv", "elapsed\tround\tslot\thero\tabilitySlot\tability\tanswer\tcharge\tcost\tbeat\tshot\tcaption\n");
            var pulses=new Queue<(CharacterMotor actor,string hero,int sequence)>(); int sequence=0; var wanted=new HashSet<string>();float lastWanted=-999;
            System.Action<CharacterMotor,HeroKit,HeroAbility> onUltimate=(actor,kit,ability)=>
            {
                if(actor==null||kit==null)return;
                int number=++sequence; if(kit.HeroId=="cheska"||kit.HeroId=="dante"){wanted.Add(kit.HeroId);lastWanted=Time.unscaledTime;}
                File.AppendAllText(output+"/ultimate-presentations.tsv",(Time.unscaledTime-start).ToString("F3")+"\t"+GameServices.Match.RoundNumber+"\t"+actor.PlayerSlot+"\t"+kit.HeroId+"\t"+ability.Name+"\t"+director.Beat+"\t"+PresentationClock.Held+"\n");
                pulses.Enqueue((actor,kit.HeroId,number));
            };
            HeroAbilitySystem.UltimateStarted+=onUltimate;
            try
            {
            while(Time.unscaledTime-start<900 && GameServices.Match.RoundNumber<8
                && !(wanted.Contains("cheska")&&wanted.Contains("dante")&&!PresentationClock.Held&&Time.unscaledTime-lastWanted>8))
            {
                float elapsed=Time.unscaledTime-start;
                if(pulses.Count>0)
                {
                    var pulse=pulses.Dequeue();
                    yield return GameplayShots.Render(camera,"presented-"+pulse.hero+"-"+pulse.sequence,flipCanvases:true,outDir:output,observedSubject:pulse.actor,width:1600,height:900);
                }
                foreach(var body in cast)
                {
                    var powers=body.AbilitySystem; var kit=powers!=null?powers.Kit:null; if(kit==null)continue;
                    var values=(float[])answered.GetValue(powers);
                    for(int key=0;key<3;key++)
                    {
                        string identity=body.PlayerSlot+":"+key; float stamp=values[key];
                        if(float.IsNegativeInfinity(stamp)||(stamps.TryGetValue(identity,out float old)&&old==stamp))continue;
                        stamps[identity]=stamp; var slot=(HeroAbilitySystem.Slot)key;var answer=powers.LastAnswer(slot);
                        var ability=key==0?kit.Skill1:key==1?kit.Skill2:kit.Ultimate;
                        File.AppendAllText(output+"/answers.tsv", elapsed.ToString("F3")+"\t"+GameServices.Match.RoundNumber+"\t"+body.PlayerSlot+"\t"+kit.HeroId+"\t"+slot+"\t"+(ability!=null?ability.Name:"missing")+"\t"+answer+"\t"+kit.UltimateCharge.ToString("F3")+"\t"+kit.UltimateCost+"\t"+director.Beat+"\t"+director.Shot+"\t"+director.ShotName()+"\n");
                        string picture=kit.HeroId+"-"+key;
                        if(answer==HeroKit.CastOutcome.Cast&&captured.Add(picture))
                            yield return GameplayShots.Render(camera,"accepted-"+picture,flipCanvases:true,outDir:output,observedSubject:body,width:1600,height:900);
                    }
                }
                if(elapsed>=sample)
                {
                    sample=elapsed+.5f;
                    File.AppendAllText(output+"/timeline.tsv",elapsed.ToString("F3")+"\t"+GameServices.Match.RoundNumber+"\t"+round.TimeLeft.ToString("F3")+"\t"+director.Beat+"\t"+director.Shot+"\t"+director.ShotName()+"\t"+director.Cuts+"\t"+director.SafePoseFallbacks+"\n");
                }
                if(elapsed>=image*5 && image<3)
                {
                    yield return GameplayShots.Render(camera,"cast-natural-"+image,flipCanvases:true,outDir:output,width:1600,height:900);image++;
                }
                yield return null;
            }
            }
            finally { HeroAbilitySystem.UltimateStarted-=onUltimate; }
            File.WriteAllText(output+"/stop.json","{\"round\":"+GameServices.Match.RoundNumber+",\"elapsed\":"+(Time.unscaledTime-start).ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+",\"presentations\":"+sequence+"}");
            Assert.AreEqual(3,Directory.GetFiles(output,"cast-natural-*.png").Length);
            Assert.IsTrue(GameServices.Match.RoundNumber>beganRound||round.TimeLeft<clock);
            File.WriteAllText(output+"/final-charges.tsv",string.Join("\n",cast.Select(p=>p.PlayerSlot+"\t"+p.AbilitySystem.Kit.HeroId+"\t"+p.AbilitySystem.Kit.UltimateCharge+"\t"+p.AbilitySystem.Kit.UltimateCost)));
        }
    }
}
