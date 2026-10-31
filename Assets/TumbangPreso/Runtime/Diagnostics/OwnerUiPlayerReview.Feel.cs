using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    // The owner's review film of the October 31 feel pass: real player, real picture, UI and game audio.
    public sealed partial class OwnerUiPlayerReview
    {
        private IEnumerator FeelClip(string name,float seconds,Func<IEnumerator> action)
        {
            var listener=Object.FindObjectsByType<AudioListener>().FirstOrDefault(l=>l.enabled&&l.gameObject.activeInHierarchy);
            if(listener==null)throw new InvalidOperationException("Missing real audio listener");
            var sound=listener.gameObject.AddComponent<ReviewAudioCapture>();sound.Begin(seconds+1);
            var movie=StartCoroutine(RecordCatchMotion(name,seconds));
            yield return action();
            yield return movie;
            sound.Save(Path.Combine(_folder,name));Object.Destroy(sound);
            Stage("filmed "+name);
        }
        private static void FeelPlace(CharacterMotor who,Vector3 at,Vector3 look)
        {
            who.Intent.Clear();who.Intent.Parked=true;who.ClearStun();who.ClearTrip();who.Teleport(at);
            Vector3 d=look-at;d.y=0;if(d.sqrMagnitude>.01f)who.transform.forward=d.normalized;
        }
        private static void FeelArm(CharacterMotor who)
        {
            var shoe=Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include).First(s=>s.SeatOfOrigin==who.PlayerSlot);
            shoe.gameObject.SetActive(true);
            if(!shoe.HostForceEquip(who))throw new InvalidOperationException("Film could not arm seat "+who.PlayerSlot);
        }
        // The same private release the input path takes, so the thrower's own kick is in the picture.
        private static void FeelThrow(CharacterMotor who,Vector3 aim)
        {
            Vector3 origin=who.transform.position+Vector3.up*1.45f+(aim-who.transform.position).normalized*.35f;
            typeof(Carrier).GetMethod("ReleaseTo",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(who.GetComponent<Carrier>(),new object[]{1f,0f,origin,aim});
        }
        private IEnumerator FeelOnly()
        {
            _deadline=Time.realtimeSinceStartup+420;
            Stage("feel film: throw, can, slipper hit, tag replay, halftime, spectator");
            yield return WaitFor(()=>Find("GuestAccount")!=null||Find("ContinueAccount")!=null||Find("StartButton")!=null,80);
            if(Find("GuestAccount")!=null)yield return Click("GuestAccount");else if(Find("ContinueAccount")!=null)yield return Click("ContinueAccount");
            var look=Settings.SettingsStore.Current;
            look.Fullscreen=false;look.ReducedUiMotion=false;look.ReducedEffects=false;look.CinematicCameraMotion=true;
            Screen.SetResolution(1280,720,FullScreenMode.Windowed);
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));SceneFlow.SelectedMap=SceneFlow.Eskinita;
            // The match itself is what is filmed: start it as the menus do, without walking their current layout.
            GameLaunch.Reset();GameLaunch.Spectator=false;GameLaunch.AllBots=false;GameLaunch.TrainingRange=false;
            SceneFlow.StartMatch();yield return StartReadyRound();
            var watcher=Object.FindAnyObjectByType<PauseWatcher>();var round=GameServices.Round;var match=GameServices.Match;
            foreach(var ai in Object.FindObjectsByType<AIController>())ai.enabled=false;
            foreach(var reader in Object.FindObjectsByType<PlayerInputReader>())reader.enabled=false;
            var taya=round.Players.First(p=>p.IsDefender);var attackers=round.Players.Where(p=>!p.IsDefender).ToArray();
            var a=attackers[0];var b=attackers[1];var c=attackers[2];var can=round.Lata;
            var rig=Camera.main.GetComponent<CameraRig>();
            Vector3 mark=can.Mark,away=new Vector3(6,.12f,6);
            void Park(){foreach(var p in round.Players)FeelPlace(p,away+Vector3.right*p.PlayerSlot*1.5f,mark);}
            Vector3 groundA=new Vector3(mark.x,.12f,mark.z-3.6f);
            Hud.Instance.ShowReadyPrompt(false);

            // 1. The throw and the can, on the scorer's own screen.
            Park();FeelPlace(a,groundA,mark);FeelArm(a);rig.Follow(a,true);Hud.Instance.Bind(a);
            yield return WaitFor(()=>!can.IsProtected,8);
            yield return FeelClip("1-throw-and-can",3.6f,()=>FeelThrowAt(a,mark+Vector3.up*.2f));
            if(can.IsUpright)throw new InvalidOperationException("The filmed throw missed the can");
            yield return new WaitForSecondsRealtime(1.6f);   // retained for the halftime replay
            can.HostRestore();
            yield return WaitFor(()=>!can.IsProtected,8);

            // 2. A slipper hits you, on the victim's screen. 3. The same hit, from a third player.
            Vector3 side=new Vector3(mark.x+7,.12f,mark.z-7);
            for(int take=0;take<2;take++)
            {
                Park();
                Vector3 victimAt=side,throwerAt=side+Vector3.forward*5;
                FeelPlace(b,victimAt,throwerAt);FeelPlace(a,throwerAt,victimAt);FeelArm(a);
                var viewer=take==0?b:c;
                if(take==1)FeelPlace(c,side+new Vector3(-4,0,2.5f),side+Vector3.forward);
                rig.Follow(viewer,true);Hud.Instance.Bind(viewer);
                yield return new WaitForSecondsRealtime(.3f);
                yield return FeelClip(take==0?"2-hit-your-view":"3-hit-from-outside",3,()=>FeelThrowAt(a,victimAt+Vector3.up*1));
                yield return new WaitForSecondsRealtime(.5f);
            }

            // 4. Tagged: the victim's replay.
            Park();FeelPlace(taya,new Vector3(0,.12f,-4),new Vector3(0,.12f,-3));FeelPlace(b,new Vector3(0,.12f,-3),new Vector3(0,.12f,-2));FeelArm(b);
            rig.Follow(b,true);Hud.Instance.Bind(b);
            yield return new WaitForSecondsRealtime(2.5f);
            yield return FeelClip("4-tagged-replay",6,()=>FeelPunch(taya));
            yield return WaitFor(()=>b.CanAct(),8);

            // 5. Halftime.
            var archive=Object.FindAnyObjectByType<MatchReplayArchive>();
            if(archive.Clips.Count==0)throw new InvalidOperationException("Nothing retained for halftime: "+archive.LastSkip);
            while(match.RoundNumber<4){round.EndRound();match.AdvanceRound();yield return null;}
            yield return FeelClip("5-halftime-replay",10.5f,()=>FeelHalftime(round,match));
            yield return WaitFor(()=>match.RoundNumber==5,5);
            var pause=Panel.Open<PausePanel>(watcher);pause.Local=watcher.Local;
            yield return WaitFor(()=>Find("LeaveMatch")!=null);yield return Click("LeaveMatch");yield return new WaitForSecondsRealtime(5);

            // 6. Spectator: real bots play; the replay key's own request once something notable happened.
            look.ReducedUiMotion=false;
            GameLaunch.Reset();GameLaunch.Spectator=true;GameLaunch.AllBots=true;GameLaunch.TrainingRange=false;
            SceneFlow.StartMatch();
            yield return WaitFor(()=>Object.FindAnyObjectByType<SpectatorCamera>()!=null,60);
            yield return StartReadyRound();
            var spectator=Object.FindAnyObjectByType<SpectatorCamera>();
            if(spectator==null)throw new InvalidOperationException("No spectator camera");
            float moment=-1;
            Action<Visual.MatchFlair.Kind,int,int,Vector3,float> seen=(kind,actor,subject,at,strength)=>
                {if(kind==Visual.MatchFlair.Kind.Tag||kind==Visual.MatchFlair.Kind.LataDown)moment=Time.realtimeSinceStartup;};
            Visual.MatchFlair.Presented+=seen;
            float give=Time.realtimeSinceStartup+100;
            yield return new WaitUntil(()=>Time.realtimeSinceStartup>give||moment>0&&Time.realtimeSinceStartup-moment>1.6f);
            Visual.MatchFlair.Presented-=seen;
            yield return FeelClip("6-spectator-replay",9.5f,()=>FeelSpectatorReplay(spectator));
            yield return new WaitForSecondsRealtime(1.5f);
            SpectatorCamera.ProbeUsePictureRing=true;
            yield return FeelClip("7-spectator-fallback",9.5f,()=>FeelSpectatorReplay(spectator));
            SpectatorCamera.ProbeUsePictureRing=false;
            Stage("feel film complete");
        }
        private static IEnumerator FeelThrowAt(CharacterMotor who,Vector3 aim)
        {
            yield return new WaitForSecondsRealtime(.7f);
            FeelThrow(who,aim);
        }
        private static IEnumerator FeelPunch(CharacterMotor taya)
        {
            yield return new WaitForSecondsRealtime(.5f);
            if(!taya.GetComponent<CombatVerbs>().HostResolvePunch(taya.transform.position,taya.transform.forward))
                throw new InvalidOperationException("Filmed catch was refused");
            yield return new WaitForSecondsRealtime(.9f);
            if(!Object.FindAnyObjectByType<CatchReconstruction>().Playing)throw new InvalidOperationException("The tag replay did not play");
        }
        private static IEnumerator FeelHalftime(RoundDirector round,MatchDirector match)
        {
            yield return new WaitForSecondsRealtime(.4f);
            round.EndRound();match.BeginIntermission();
        }
        private static IEnumerator FeelSpectatorReplay(SpectatorCamera spectator)
        {
            yield return new WaitForSecondsRealtime(.8f);
            SpectatorCamera.ProbeReplayRequest=true;
            yield return new WaitForSecondsRealtime(.3f);
            if(!spectator.Replaying)throw new InvalidOperationException("Spectator replay did not start");
        }
    }
}
