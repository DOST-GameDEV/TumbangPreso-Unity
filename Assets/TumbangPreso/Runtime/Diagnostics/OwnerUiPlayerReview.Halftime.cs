using System;
using System.Collections;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        private IEnumerator HalftimeOnly()
        {
            _deadline=Time.realtimeSinceStartup+200;
            Stage("real native halftime, both modes, recorded legal catch and full return");
            yield return WaitFor(()=>Find("GuestAccount")!=null||Find("ContinueAccount")!=null||Find("StartButton")!=null,80);
            if(Find("GuestAccount")!=null)yield return Click("GuestAccount");else if(Find("ContinueAccount")!=null)yield return Click("ContinueAccount");
            Settings.SettingsStore.Current.Fullscreen=false;Screen.SetResolution(1280,720,FullScreenMode.Windowed);
            Settings.SettingsStore.Current.ReducedUiMotion=false;Settings.SettingsStore.Current.CinematicCameraMotion=true;
            foreach(var mode in new[]{GameMode.Classic,GameMode.HeroStrike})
            {
                SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode));SceneFlow.SelectedMap=SceneFlow.Eskinita;
                yield return Click("StartButton");yield return Click(mode==GameMode.Classic?"ClassicButton":"HeroStrikeButton");yield return Click("PracticeButton");
                yield return Click("PrimaryButton");yield return StartReadyRound();
                var watcher=Object.FindAnyObjectByType<PauseWatcher>();var round=GameServices.Round;var match=GameServices.Match;
                foreach(var ai in Object.FindObjectsByType<AIController>())ai.enabled=false;
                foreach(var reader in Object.FindObjectsByType<PlayerInputReader>())reader.enabled=false;
                foreach(var actor in round.Players){actor.Intent.Clear();actor.Intent.Parked=true;actor.ClearStun();actor.ClearTrip();actor.Teleport(new Vector3(6,.12f,-6+actor.PlayerSlot*3));}
                var taya=round.PlayerAt(0);var victim=round.PlayerAt(1);
                taya.Teleport(new Vector3(0,.12f,-4));victim.Teleport(new Vector3(0,.12f,-3));taya.transform.forward=Vector3.forward;victim.transform.forward=Vector3.forward;
                var shoe=Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include).First(s=>s.SeatOfOrigin==1);shoe.gameObject.SetActive(true);
                if(!shoe.HostForceEquip(victim))throw new InvalidOperationException("Replay fixture could not equip the legal victim");
                var rig=Camera.main.GetComponent<CameraRig>();rig.Follow(victim,true);Hud.Instance.Bind(victim);Hud.Instance.ShowReadyPrompt(false);
                yield return new WaitForSeconds(2.5f);
                if(!taya.GetComponent<CombatVerbs>().HostResolvePunch(taya.transform.position,taya.transform.forward))throw new InvalidOperationException("Replay catch was refused");
                yield return new WaitForSeconds(1.6f);
                var archive=Object.FindAnyObjectByType<MatchReplayArchive>();if(archive.Clips.Count==0)throw new InvalidOperationException("Catch was not retained: "+archive.LastSkip);
                while(match.RoundNumber<4){round.EndRound();match.AdvanceRound();yield return null;}
                var listener=Object.FindObjectsByType<AudioListener>().FirstOrDefault(l=>l.enabled&&l.gameObject.activeInHierarchy);
                if(listener==null)throw new InvalidOperationException("Missing real audio listener");
                var sound=listener.gameObject.AddComponent<ReviewAudioCapture>();sound.Begin(12);
                string name=mode+"-halftime-motion";var movie=StartCoroutine(RecordCatchMotion(name,11.5f));
                round.EndRound();match.BeginIntermission();
                yield return WaitFor(()=>HalftimePresentation.Instance?.HasReplay==true,2);
                yield return new WaitForSecondsRealtime(2.2f);yield return Shot(mode+"-halftime-contact");
                yield return new WaitForSecondsRealtime(3.8f);yield return Shot(mode+"-halftime-standings");
                yield return WaitFor(()=>match.RoundNumber==5,5);yield return Shot(mode+"-halftime-return");
                yield return movie;sound.Save(Path.Combine(_folder,name));Object.Destroy(sound);
                if(PresentationClock.Held)throw new InvalidOperationException("Halftime did not release control");
                Stage(mode+" native full-screen replay, standings, next taya and round5 return captured");
                var pause=Panel.Open<PausePanel>(watcher);pause.Local=watcher.Local;
                yield return WaitFor(()=>Find("LeaveMatch")!=null);yield return Click("LeaveMatch");yield return WaitFor(()=>GameObject.Find("OwnerHomeCanvas")!=null);
            }
        }
    }
}
