using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class DanteHandoffReviewTests
    {
        int _quality,_seat;bool _cameraMotion,_reduced,_bots,_spectator;
        Camera _witness;
        int _captureRate;System.Func<double> _filmClock;GameObject _clockObject;
        [DefaultExecutionOrder(-31000)] sealed class CaptureClock : MonoBehaviour
        { public double Seconds;void Update(){Seconds+=1d/60;} }
        [UnitySetUp] public IEnumerator Before()
        {
            _quality=QualitySettings.GetQualityLevel();_seat=GameLaunch.SoloSeat;
            _captureRate=Time.captureFramerate;_filmClock=SharedUltimatePhase.FilmClock;
            _bots=GameLaunch.AllBots;_spectator=GameLaunch.Spectator;
            _cameraMotion=Settings.SettingsStore.Current.CinematicCameraMotion;
            _reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            if(_witness!=null)Object.Destroy(_witness.gameObject);
            SharedUltimatePhase.FilmClock=_filmClock;Time.captureFramerate=_captureRate;
            if(_clockObject!=null)Object.Destroy(_clockObject);
            SharedUltimatePhase.Instance?.Cancel();PresentationClock.RequestScale(1);
            yield return PlayModeWorld.Reset();
            QualitySettings.SetQualityLevel(_quality,true);GameLaunch.SoloSeat=_seat;
            GameLaunch.AllBots=_bots;GameLaunch.Spectator=_spectator;
            Settings.SettingsStore.Current.CinematicCameraMotion=_cameraMotion;
            Settings.SettingsStore.Current.ReducedUiMotion=_reduced;
        }
        [UnityTest,Timeout(120000)] public IEnumerator AcceptedIntroductionHandsBackToRealOwnerAndObserverViews()
        {
            int low=System.Array.IndexOf(QualitySettings.names,"Low");
            if(low>=0)QualitySettings.SetQualityLevel(low,true);
            yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();yield return new WaitForSeconds(3.6f);
            foreach(var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None))brain.enabled=false;
            foreach(var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None))input.enabled=false;
            var actor=GameServices.Round.PlayerAt(1);
            foreach(var other in GameServices.Round.Players)
            {other.Intent.Clear();other.Intent.Parked=true;if(other!=actor)other.Teleport(new Vector3(6,.18f,8+other.PlayerSlot));}
            actor.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,"dante");
            var art=RosterBook.Load().FindPersonArt("dante");
            var cast=art.Clips.Single(c=>c!=null&&c.name=="hero-dante-fissure");
            Assert.AreEqual(1f,cast.length,.002f);
            var sample=Object.Instantiate(art.Model);
            try
            {
                cast.SampleAnimation(sample,0);
                var bones=sample.GetComponentsInChildren<Transform>(true);
                string[] names={"torso","head","arm-left","arm-right","leg-left","leg-right"};
                Vector3[] expected={new Vector3(29.6f,-11.6f,0),new Vector3(-8.56f,7.4f,0),
                    new Vector3(-69,29,74.7f),new Vector3(-69,-31.9f,-74.7f),
                    new Vector3(-22.35f,0,-9.95f),new Vector3(18.2f,0,9.95f)};
                for(int i=0;i<names.Length;i++)
                    Assert.Less(Quaternion.Angle(bones.Single(b=>b.name==names[i]).localRotation,
                        Quaternion.Euler(expected[i])),.1f,"Imported first pose must continue introduction: "+names[i]);
            }
            finally {Object.Destroy(sample);}
            actor.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            actor.AbilitySystem.BindHero("dante");actor.Teleport(new Vector3(0,.18f,-5));actor.transform.rotation=Quaternion.identity;
            actor.Intent.Parked=false;actor.Intent.AimPoint=actor.transform.position+Vector3.forward*8;actor.Intent.FaceAimPoint=true;
            actor.AbilitySystem.Kit.AddUltimateCharge(actor.AbilitySystem.Kit.UltimateCost);
            Camera.main.GetComponent<CameraRig>().Follow(actor,true);
            Camera.main.GetComponent<CameraRig>().SetAimSource(AimSource.Movement);
            Settings.SettingsStore.Current.CinematicCameraMotion=true;Settings.SettingsStore.Current.ReducedUiMotion=false;
            float until=Time.realtimeSinceStartup+5;
            while(UltimateIntroductionCache.Find(actor,actor.GetComponent<Carrier>().Held!=null)==null&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsNotNull(UltimateIntroductionCache.Find(actor,actor.GetComponent<Carrier>().Held!=null));
            _clockObject=new GameObject("Dante fixed film clock");var clock=_clockObject.AddComponent<CaptureClock>();
            SharedUltimatePhase.FilmClock=()=>clock.Seconds;Time.captureFramerate=60;
            actor.Intent.Set(Verb.Ultimate,true);actor.Intent.BufferPress(Verb.Ultimate);yield return null;
            actor.Intent.Set(Verb.Ultimate,false);
            until=Time.realtimeSinceStartup+1;
            while(!SharedUltimatePhase.BlocksActions&&Time.realtimeSinceStartup<until)yield return null;
            var phase=SharedUltimatePhase.Instance;Assert.IsTrue(phase!=null&&phase.Active);
            double began=clock.Seconds;
            while(phase.Active&&clock.Seconds-began<3.45)yield return null;
            string directory=System.Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/dante-handoff-review";
            yield return GameplayShots.Render(Camera.main,"intro-near-stamp",true,directory);
            until=Time.realtimeSinceStartup+30;
            while(phase.Active&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsFalse(phase.Active);Assert.IsFalse(PresentationClock.Held);
            _witness=new GameObject("Dante handoff observer").AddComponent<Camera>();_witness.enabled=false;_witness.fieldOfView=52;
            bool sawWave=Object.FindAnyObjectByType<DanteDriftWave>()!=null;
            int maximumBands=0;double firstBand=-1;
            double handback=clock.Seconds;var trace=new System.Text.StringBuilder("frame,film_seconds,action,bands\n");
            for(int frame=0;frame<42;frame++)
            {
                actor.Intent.Set(Verb.Ultimate,false);actor.Intent.AimPoint=actor.transform.position+Vector3.forward*8;
                actor.Intent.FaceAimPoint=true;var wave=Object.FindAnyObjectByType<DanteDriftWave>();sawWave|=wave!=null;
                if(wave!=null){maximumBands=Mathf.Max(maximumBands,wave.ReleasedBands);
                    if(wave.ReleasedBands>0&&firstBand<0)firstBand=clock.Seconds-handback;}
                _witness.transform.position=actor.transform.position+new Vector3(3,2.3f,3.6f);
                _witness.transform.LookAt(actor.transform.position+Vector3.up);
                yield return GameplayShots.Render(Camera.main,"owner-"+frame.ToString("D3"),false,directory,null,960,540);
                yield return GameplayShots.Render(_witness,"observer-"+frame.ToString("D3"),false,directory,actor,960,540);
                trace.AppendLine(frame+","+(clock.Seconds-handback).ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+","+
                    actor.GetComponentInChildren<CharacterAnimator>().CurrentClipName+","+(wave!=null?wave.ReleasedBands:0));
            }
            File.WriteAllText(Path.Combine(directory,"fixed-frames.csv"),trace.ToString());
            Assert.AreEqual(5,maximumBands,"All five live bands must remain.");
            Assert.GreaterOrEqual(firstBand,.38,"No early release during the existing warning.");
            Assert.Less(firstBand,.55,"No added warning delay.");
            Assert.IsTrue(sawWave,"Actual shared handoff never produced its live wave.");
            Assert.AreEqual(.4f,actor.AbilitySystem.Kit.Ultimate.Windup);
            Assert.IsFalse(actor.AbilitySystem.Kit.Ultimate.ReservedForIntroduction);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory,"scope.txt"),"Actual accepted shared introduction and real owner CameraRig; observational motion review, not a verdict that the handoff reads well.\n");
        }
    }
}
