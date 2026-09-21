using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        [Serializable] private sealed class WholeMatchReceipt
        {
            public string mode,view;
            public int rounds,winner=-2,throws,canHits,tags,ultimateExecutions,phases,halftimes;
            public float realSeconds,ultimateHoldSeconds;
            public int[] scores;
            public string scope="Default eight-round normal-speed match; four real bot input writers; no staged hits, score grants, forced round advancement or human-play claim.";
        }
        private sealed class WholeCapture
        { public string Name,Error;public float Began;public bool Done; }

        private IEnumerator WholeMatchesOnly()
        {
            _deadline=Time.realtimeSinceStartup+2200;
            yield return WaitFor(()=>Find("GuestAccount")!=null||Find("ContinueAccount")!=null||Find("StartButton")!=null,80);
            if(Find("GuestAccount")!=null)yield return Click("GuestAccount");
            else if(Find("ContinueAccount")!=null)yield return Click("ContinueAccount");
            Settings.SettingsStore.Current.Fullscreen=false;Screen.SetResolution(1280,720,FullScreenMode.Windowed);
            foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
            {
                bool spectator=mode==GameMode.HeroStrike;
                SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode));SceneFlow.SelectedMap=SceneFlow.Eskinita;
                Stage(mode+" complete default match, four active input writers");
                yield return Click("StartButton");yield return Click(mode==GameMode.Classic?"ClassicButton":"HeroStrikeButton");
                yield return Click("PracticeButton");
                if(GameLaunch.Spectator!=spectator)yield return Click("SpectateButton");
                yield return Click("PrimaryButton");yield return StartReadyRound();
                foreach(var reader in Object.FindObjectsByType<PlayerInputReader>())reader.enabled=false;
                foreach(var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>())switcher.enabled=false;
                var match=GameServices.Match;var round=GameServices.Round;
                if(match.TotalRounds!=8||SceneFlow.SelectedRules.RoundSeconds!=90)throw new InvalidOperationException("Whole-match route must preserve shipped eight-round/90-second rules.");
                foreach(var actor in round.Players)
                {
                    actor.IsBot=true;actor.Intent.Clear();actor.Intent.Parked=false;
                    var brain=actor.GetComponent<AIController>()??actor.gameObject.AddComponent<AIController>();brain.enabled=true;
                }
                if(spectator)Object.FindAnyObjectByType<SpectatorDirector>().Engaged=true;
                else
                {
                    var local=Object.FindAnyObjectByType<PauseWatcher>().Local;
                    var rig=Camera.main.GetComponent<CameraRig>();rig.Follow(local,true);rig.SetAimSource(AimSource.Movement);Hud.Instance.Bind(local);
                }
                var receipt=new WholeMatchReceipt{mode=mode.ToString(),view=spectator?"live spectator":"owner"};
                var seenRounds=new HashSet<int>{match.RoundNumber};var phases=new HashSet<long>();var halves=new HashSet<int>();
                var events=new StringBuilder("real,round,event,actor,subject\n");
                var states=new StringBuilder("real,round,left,phase,halftime,p1,p2,p3,p4\n");
                float began=Time.realtimeSinceStartup,last=began,nextSample=began;
                bool ended=false,halfRecorded=false,ultimateRecorded=false;
                WholeCapture capture=null;
                void Outcome(MatchFlair.Kind kind,int actor,int subject,Vector3 at,float strength)
                {
                    if(kind==MatchFlair.Kind.Throw)receipt.throws++;
                    if(kind==MatchFlair.Kind.LataDown)receipt.canHits++;
                    if(kind==MatchFlair.Kind.Tag)receipt.tags++;
                    events.AppendLine(FormattableString.Invariant($"{Time.realtimeSinceStartup-began:F4},{match.RoundNumber},{kind},{actor},{subject}"));
                }
                void Cast(CharacterMotor actor,HeroKit kit,HeroAbility ability){receipt.ultimateExecutions++;}
                void End(int winner){receipt.winner=winner;ended=true;}
                MatchFlair.Presented+=Outcome;HeroAbilitySystem.UltimateStarted+=Cast;match.MatchEnded+=End;
                StartFrameWindow(mode+"-whole-match");
                void Window(string name)
                {
                    capture=new WholeCapture{Name=mode+"-whole-"+name,Began=Time.realtimeSinceStartup};
                    StartCoroutine(WholeMatchWindow(capture));
                }
                Window("opening");
                try
                {
                    while(!ended)
                    {
                        float now=Time.realtimeSinceStartup;
                        var phase=SharedUltimatePhase.Instance;bool holding=phase!=null&&phase.Active;
                        if(holding){receipt.ultimateHoldSeconds+=now-last;phases.Add(phase.PhaseId);}
                        last=now;seenRounds.Add(match.RoundNumber);
                        if(HalftimePresentation.Playing)halves.Add(HalftimePresentation.Instance.CompletedRound);
                        if(capture!=null&&capture.Error!=null)throw new InvalidOperationException(capture.Error);
                        if(capture!=null&&!capture.Done&&now-capture.Began>22)throw new InvalidOperationException("Whole-match capture did not finish: "+capture.Name);
                        if(capture==null||capture.Done)
                        {
                            if(HalftimePresentation.Playing&&!halfRecorded){halfRecorded=true;Window("halftime");}
                            else if(holding&&!ultimateRecorded){ultimateRecorded=true;Window("first-ultimate");}
                        }
                        if(now>=nextSample)
                        {
                            nextSample=now+1;
                            states.AppendLine(FormattableString.Invariant($"{now-began:F4},{match.RoundNumber},{round.TimeLeft:F4},{holding},{HalftimePresentation.Playing},{match.ScoreFor(0)},{match.ScoreFor(1)},{match.ScoreFor(2)},{match.ScoreFor(3)}"));
                            if(!float.IsFinite(round.TimeLeft)||round.Players.Count!=4)throw new InvalidOperationException("Whole match lost a participant or finite clock.");
                        }
                        yield return null;
                    }
                    receipt.realSeconds=Time.realtimeSinceStartup-began;receipt.rounds=seenRounds.Count;
                    receipt.phases=phases.Count;receipt.halftimes=halves.Count;receipt.scores=Enumerable.Range(0,4).Select(match.ScoreFor).ToArray();
                    if(capture!=null)yield return WaitFor(()=>capture.Done,20);
                    yield return WaitFor(()=>Find("ResultMainMenu")!=null,8);
                    yield return Shot(mode+"-whole-match-result");
                    if(receipt.rounds!=8||receipt.halftimes!=1||receipt.throws==0||receipt.canHits==0||receipt.tags==0)
                        throw new InvalidOperationException("Complete match lacked eight rounds, halftime or representative real exchanges.");
                    if(PresentationClock.Held||SharedUltimatePhase.Instance!=null&&SharedUltimatePhase.Instance.Active)
                        throw new InvalidOperationException("Match end retained a presentation hold.");
                    Stage(mode+" whole match ended naturally; "+receipt.phases+" shared phases, "+receipt.ultimateHoldSeconds.ToString("F2")+" held seconds");
                }
                finally
                {
                    MatchFlair.Presented-=Outcome;HeroAbilitySystem.UltimateStarted-=Cast;match.MatchEnded-=End;
                    StopFrameWindow();File.WriteAllText(Path.Combine(_folder,mode+"-whole-match.json"),JsonUtility.ToJson(receipt,true));
                    File.WriteAllText(Path.Combine(_folder,mode+"-whole-events.csv"),events.ToString());
                    File.WriteAllText(Path.Combine(_folder,mode+"-whole-state.csv"),states.ToString());
                }
                yield return Click("ResultMainMenu");yield return WaitFor(()=>GameObject.Find("OwnerHomeCanvas")!=null);
            }
        }

        private IEnumerator WholeMatchWindow(WholeCapture capture)
        {
            var listener=Object.FindObjectsByType<AudioListener>().FirstOrDefault(l=>l.enabled&&l.gameObject.activeInHierarchy);
            if(listener==null){capture.Error="No game audio listener";capture.Done=true;yield break;}
            var sound=listener.gameObject.AddComponent<ReviewAudioCapture>();sound.Begin(8);
            try
            {
                yield return RecordCatchMotion(capture.Name,12);
                try{sound.Save(Path.Combine(_folder,capture.Name));}catch(Exception error){capture.Error=error.ToString();}
            }
            finally{sound.enabled=false;Object.Destroy(sound);capture.Done=true;}
        }
    }
}
