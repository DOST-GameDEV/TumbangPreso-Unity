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
            _deadline=Time.realtimeSinceStartup+180;
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
                    yield return movie;sound.Save(Path.Combine(_folder,movieName));
                    File.WriteAllText(Path.Combine(_folder,movieName,"accepted.txt"),"Real InputIntent to shared phase; one reserved fee and one live execution; staged profile/round, not human freeform play.\n");
                }
                finally{HeroAbilitySystem.UltimateStarted-=Started;sound.enabled=false;Object.Destroy(sound);actor.Intent.Clear();}
            }
            Stage("six real shared introductions returned into live abilities with actual game audio");
        }
    }
}
