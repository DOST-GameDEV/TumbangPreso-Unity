using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class SharedUltimatePhaseTests
    {
        private bool _cameraMotion, _reduced;
        [UnitySetUp] public IEnumerator Before()
        {
            _cameraMotion=Settings.SettingsStore.Current.CinematicCameraMotion; _reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            SharedUltimatePhase.Instance?.Cancel(); PresentationClock.RequestScale(1);
            Settings.SettingsStore.Current.CinematicCameraMotion=_cameraMotion; Settings.SettingsStore.Current.ReducedUiMotion=_reduced;
            yield return PlayModeWorld.Reset();
        }
        private static void Hero(CharacterMotor actor, string hero)
        {
            actor.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,hero);
            var art=RosterBook.Load().People.First(p=>p.Id==hero);
            actor.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
            actor.AbilitySystem.BindHero(hero); actor.AbilitySystem.Kit.AddUltimateCharge(actor.AbilitySystem.Kit.UltimateCost);
            actor.Intent.Clear(); actor.Intent.Parked=false;
        }
        private static void Press(CharacterMotor actor, Verb verb)
        { actor.Intent.Set(verb,true); actor.Intent.BufferPress(verb); }

        [UnityTest, Timeout(60000)]
        public IEnumerator TwoAcceptedCastsShareOnePhaseAndPreserveTheFullLiveWarning()
        {
            yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
            Hud.Instance.ShowReadyPrompt(false);
            var round=GameServices.Round;
            var sean=round.PlayerAt(1);var phaister=round.PlayerAt(2);var later=round.PlayerAt(3);
            Hero(sean,"sean");Hero(phaister,"phaister");Hero(later,"zack");
            sean.Teleport(new Vector3(0,.18f,-5));phaister.Teleport(new Vector3(-3,.18f,-5));later.Teleport(new Vector3(6,.18f,5));
            Camera.main.GetComponent<CameraRig>().Follow(sean,true);
            Settings.SettingsStore.Current.CinematicCameraMotion=true;Settings.SettingsStore.Current.ReducedUiMotion=false;
            yield return null;
            float started=Time.realtimeSinceStartup;
            Press(sean,Verb.Ultimate);Press(phaister,Verb.Ultimate);
            float limit=Time.realtimeSinceStartup+1;
            while(!SharedUltimatePhase.BlocksActions&&Time.realtimeSinceStartup<limit)yield return null;
            var phase=SharedUltimatePhase.Instance;
            Assert.IsNotNull(phase);Assert.IsTrue(phase.Active);Assert.AreEqual(2,phase.Commits.Count);
            Assert.AreEqual(0,Time.timeScale);Assert.AreEqual(0,sean.AbilitySystem.Kit.UltimateCharge);
            Assert.AreEqual(0,phaister.AbilitySystem.Kit.UltimateCharge);
            Assert.AreEqual(0,phaister.AbilitySystem.Kit.Ultimate.WindupRemaining,"The playable ritual must not start inside the introduction.");
            Assert.IsTrue(phaister.AbilitySystem.Kit.Ultimate.ReservedForIntroduction);
            float clock=round.TimeLeft;Vector3 at=sean.transform.position;
            Press(later,Verb.Ultimate);sean.Intent.Move=Vector2.up;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(clock,round.TimeLeft,.001f);Assert.AreEqual(at,sean.transform.position);
            Assert.AreEqual(later.AbilitySystem.Kit.UltimateCost,later.AbilitySystem.Kit.UltimateCharge,.001f);
            yield return GameplayShots.Render(Camera.main,"shared-sean-phaister-introduction",true,outDir:"Logs/shared-phase-v1");
            PresentationClock.RequestScale(.5f);Assert.AreEqual(0,Time.timeScale);
            while(phase.Active&&Time.realtimeSinceStartup-started<4)yield return null;
            Assert.IsFalse(phase.Active);Assert.AreEqual(.5f,Time.timeScale);
            Assert.That(Time.realtimeSinceStartup-started,Is.InRange(2.65f,3.1f));
            Assert.AreEqual(1.55f,phaister.AbilitySystem.Kit.Ultimate.WindupRemaining,.08f);
            Assert.IsFalse(phaister.AbilitySystem.Kit.Ultimate.ReservedForIntroduction);
            Assert.Greater(sean.AbilitySystem.Kit.Ultimate.WindupRemaining,.2f);
            yield return new WaitForSecondsRealtime(.35f);
            Assert.IsFalse(phase.Active,"A held button from the pause cannot launch the queued third ultimate.");
            Assert.AreEqual(later.AbilitySystem.Kit.UltimateCost,later.AbilitySystem.Kit.UltimateCharge,.001f);
            later.Intent.Set(Verb.Ultimate,false);yield return null;
            Press(later,Verb.Ultimate);yield return null;yield return null;
            Assert.IsTrue(phase.Active,"Fresh input after resume must still work.");
            Assert.AreEqual(1,phase.Commits.Count);
        }
        [UnityTest]
        public IEnumerator ReducedViewAndRoundCancellationReleaseOnlyTheirOwnClockHold()
        {
            yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
            Hud.Instance.ShowReadyPrompt(false);var actor=GameServices.Round.PlayerAt(1);Hero(actor,"dante");
            Settings.SettingsStore.Current.CinematicCameraMotion=false;Settings.SettingsStore.Current.ReducedUiMotion=true;
            var camera=Camera.main;
            yield return null;Press(actor,Verb.Ultimate);yield return null;yield return null;
            var phase=SharedUltimatePhase.Instance;Assert.IsTrue(phase.Active);
            var pose=camera.transform.rotation;
            PresentationClock.RequestScale(0);actor.Intent.LookDelta=new Vector2(400,300);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.Less(Quaternion.Angle(pose,camera.transform.rotation),.1f);
            GameServices.Round.EndRound();yield return null;yield return null;
            Assert.IsFalse(phase.Active);Assert.IsFalse(PresentationClock.Held);
            Assert.AreEqual(0,Time.timeScale,"An operator pause requested during the phase must survive its cancellation.");
            Assert.IsFalse(actor.AbilitySystem.Kit.Ultimate.ReservedForIntroduction);
            Assert.IsNull(GameObject.Find("SharedUltimateCanvas"));
        }
    }
}
