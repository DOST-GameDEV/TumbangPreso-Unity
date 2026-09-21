using System;
using System.Collections;
using System.IO;
using System.Linq;
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
        private IEnumerator LiveUltimatesOnly()
        {
            _deadline=Time.realtimeSinceStartup+260;
            bool retained=Environment.GetCommandLineArgs().Contains("-tp-hero-replays");
            Stage("real shared ultimate route, six controlled native actor profiles");
            yield return WaitFor(()=>Find("GuestAccount")!=null||Find("ContinueAccount")!=null||Find("StartButton")!=null,80);
            if(Find("GuestAccount")!=null)yield return Click("GuestAccount");
            else if(Find("ContinueAccount")!=null)yield return Click("ContinueAccount");
            Settings.SettingsStore.Current.Fullscreen=false;Screen.SetResolution(1280,720,FullScreenMode.Windowed);
            Settings.SettingsStore.Current.CinematicCameraMotion=true;Settings.SettingsStore.Current.ReducedUiMotion=false;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));SceneFlow.SelectedMap=SceneFlow.Eskinita;
            yield return Click("StartButton");yield return Click("HeroStrikeButton");yield return Click("PracticeButton");
            yield return Click("PrimaryButton");yield return StartReadyRound();
            var actor=Object.FindAnyObjectByType<PauseWatcher>().Local;
            var rig=Camera.main.GetComponent<CameraRig>();rig.Follow(actor,true);rig.SetAimSource(AimSource.Movement);
            int index=0;
            foreach(string hero in new[]{"sean","phaister","zack","nemu","dante","cheska"})
            {
                if(index++>0){GameServices.Round.EndRound();GameServices.Match.AdvanceRound();}
                Stage(hero+" accepted introduction, return and live effect");
                foreach(var ai in Object.FindObjectsByType<AIController>())ai.enabled=false;
                foreach(var reader in Object.FindObjectsByType<PlayerInputReader>())reader.enabled=false;
                foreach(var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>())switcher.enabled=false;
                foreach(var person in GameServices.Round.Players)
                {person.IsBot=true;person.Intent.Clear();person.Intent.Parked=person!=actor;person.ClearStun();person.ClearTrip();if(person!=actor)person.Teleport(new Vector3(6,person.transform.position.y,-6+person.PlayerSlot*3));}
                actor.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,hero);
                var art=RosterBook.Load().People.First(p=>p.Id==hero);
                actor.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                actor.AbilitySystem.BindHero(hero);actor.AbilitySystem.Kit.AddUltimateCharge(100);
                actor.Teleport(new Vector3(0,actor.transform.position.y,-5));actor.transform.rotation=Quaternion.identity;
                actor.Intent.Parked=false;actor.Intent.Set(Verb.Ultimate,false);actor.Intent.CommitFrame();
                Hud.Instance.Bind(actor);Hud.Instance.ShowReadyPrompt(false);
                yield return new WaitForSecondsRealtime(.2f);
                yield return WaitFor(()=>UltimateIntroductionCache.Find(actor,actor.GetComponent<Carrier>().Held!=null)!=null,4);
                if(retained)yield return new WaitForSeconds(2.7f);
                var listener=Object.FindObjectsByType<AudioListener>().FirstOrDefault(l=>l.enabled&&l.gameObject.activeInHierarchy);
                if(listener==null)throw new InvalidOperationException("No actual game output listener");
                var sound=listener.gameObject.AddComponent<ReviewAudioCapture>();sound.Begin(8);
                int starts=0;
                void Started(CharacterMotor who,HeroKit kit,HeroAbility ability){if(who==actor)starts++;}
                HeroAbilitySystem.UltimateStarted+=Started;
                try
                {
                    string movieName=hero+"-live-ultimate-motion";
                    var movie=StartCoroutine(RecordCatchMotion(movieName,7.5f));
                    yield return new WaitForSecondsRealtime(.25f);
                    actor.Intent.AimPoint=Vector3.zero;actor.Intent.FaceAimPoint=true;
                    actor.Intent.Set(Verb.Ultimate,true);actor.Intent.BufferPress(Verb.Ultimate);
                    if(actor.AbilitySystem.Kit.Ultimate.HoldToAim)yield return new WaitForSecondsRealtime(.2f);
                    else yield return null;
                    actor.Intent.Set(Verb.Ultimate,false);
                    yield return WaitFor(()=>SharedUltimatePhase.Instance!=null&&SharedUltimatePhase.Instance.Active,2);
                    if(!actor.AbilitySystem.Kit.Ultimate.ReservedForIntroduction||actor.AbilitySystem.Kit.UltimateCharge!=0)
                        throw new InvalidOperationException(hero+" did not reserve exactly once");
                    yield return WaitFor(()=>!SharedUltimatePhase.Instance.Active,4);
                    if(PresentationClock.Held||actor.AbilitySystem.Kit.Ultimate.ReservedForIntroduction||starts!=1)
                        throw new InvalidOperationException(hero+" did not resume exactly one real ability");
                    if(retained)
                    {
                        var round=GameServices.Round;var scorer=round.Players.First(p=>p!=actor&&!p.IsDefender);
                        var shoe=Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include).First(s=>s.SeatOfOrigin==scorer.PlayerSlot);
                        int serial=round.Lata.HostKnockdownSerial;
                        shoe.HostThrow(scorer,round.Lata.transform.position+new Vector3(0,1.2f,-2),Vector3.forward*12);
                        yield return WaitFor(()=>round.Lata.HostKnockdownSerial==serial+1,2);
                        yield return new WaitForSeconds(1.6f);
                    }
                    yield return movie;sound.Save(Path.Combine(_folder,movieName));
                    File.WriteAllText(Path.Combine(_folder,movieName,"accepted.txt"),"Real InputIntent to shared phase; one reserved fee and one live execution; staged profile/round, not human freeform play.\n");
                }
                finally{HeroAbilitySystem.UltimateStarted-=Started;sound.enabled=false;Object.Destroy(sound);actor.Intent.Clear();}
                if(retained)yield return RetainedHeroView(hero);
            }
            Stage("six real shared introductions returned into live abilities with actual game audio");
        }
        private IEnumerator RetainedHeroView(string hero)
        {
            var archive=Object.FindAnyObjectByType<MatchReplayArchive>();
            var saved=archive.Clips.FirstOrDefault(c=>c.Clip.Round==GameServices.Match.RoundNumber);
            if(saved==null)throw new InvalidOperationException(hero+" did not retain its live exchange: "+archive.LastSkip);
            if(!RecordedMatchClip.TryDecode(saved.Bytes,out var clip,out var error))throw new InvalidOperationException(error);
            var listener=Object.FindObjectsByType<AudioListener>().First(l=>l.enabled&&l.gameObject.activeInHierarchy);
            var sound=listener.gameObject.AddComponent<ReviewAudioCapture>();sound.Begin(5);
            string name=hero+"-retained-exchange";PresentationClock.Hold();
            try
            {
                using var view=new RecordedWorldView(transform,clip);
                if(!view.Ready)throw new InvalidOperationException(hero+" replay unavailable: "+view.UnavailableReason);
                var movie=StartCoroutine(RecordCatchMotion(name,4.4f));float began=Time.realtimeSinceStartup;
                while(Time.realtimeSinceStartup-began<4.4f)
                {
                    float age=Time.realtimeSinceStartup-began,before=clip.Contact-clip.Start-.18f;
                    float offset=age<=before?age:age<=before+.86f?before+(age-before)*.5f:age-.43f;
                    view.Draw(clip.Start+offset);yield return null;
                }
                yield return movie;sound.Save(Path.Combine(_folder,name));
                File.WriteAllText(Path.Combine(_folder,name,"clip.txt"),"retained-bytes="+saved.Bytes.Length+" objects="+clip.Objects.Length+" fields="+clip.FieldFrames.Max(f=>f.Fields.Length)+"\n");
            }
            finally{PresentationClock.Release();sound.enabled=false;Object.Destroy(sound);}
            Stage(hero+" actual retained body/props/effects/weather/audio playback captured");
        }
    }
}
