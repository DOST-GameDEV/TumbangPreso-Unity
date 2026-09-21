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
        private float _ritualStart = -1;
        private double _ritualStartedAt;
        private void Started(CharacterMotor actor, HeroKit kit, HeroAbility ability)
        { if (kit.HeroId == "phaister") { _ritualStart = ability.WindupRemaining; _ritualStartedAt = Time.timeAsDouble; } }
        [UnitySetUp] public IEnumerator Before()
        {
            _cameraMotion=Settings.SettingsStore.Current.CinematicCameraMotion; _reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            yield return PlayModeWorld.Reset();
            _ritualStart = -1; HeroAbilitySystem.UltimateStarted += Started;
        }
        [UnityTearDown] public IEnumerator After()
        {
            HeroAbilitySystem.UltimateStarted -= Started;
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

        [UnityTest, Timeout(90000)]
        public IEnumerator SixDefaultUltimatesReservePresentAndResumeTheirActualAbility()
            => Profiles(new[] { "sean", "phaister", "zack", "nemu", "dante", "cheska" });
        [UnityTest, Timeout(30000)]
        public IEnumerator NemuDoesNotRepeatHisRevealedTransformation()
            => Profiles(new[] { "nemu" });
        private static IEnumerator Profiles(string[] heroes)
        {
            foreach (string hero in heroes)
            {
                yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
                Hud.Instance.ShowReadyPrompt(false); var actor=GameServices.Round.PlayerAt(1); Hero(actor,hero);
                actor.Teleport(new Vector3(0,.18f,-5));
                foreach(var other in GameServices.Round.Players)
                    if(other!=actor)other.Teleport(new Vector3(6,other.transform.position.y,-6+other.PlayerSlot*3));
                Camera.main.GetComponent<CameraRig>().Follow(actor,true);
                Settings.SettingsStore.Current.CinematicCameraMotion=true;Settings.SettingsStore.Current.ReducedUiMotion=false;
                yield return new WaitForSecondsRealtime(.15f);
                float readyBy=Time.realtimeSinceStartup+4;
                while(UltimateIntroductionCache.Find(actor,actor.GetComponent<Carrier>().Held!=null)==null&&Time.realtimeSinceStartup<readyBy)yield return null;
                Assert.IsNotNull(UltimateIntroductionCache.Find(actor,actor.GetComponent<Carrier>().Held!=null),hero+" failed to prewarm its real rig.");
                var familiar=actor.GetComponent<CharacterVisual>().Companion;
                float familiarBefore=familiar!=null?familiar.transform.localScale.magnitude:0;
                var ultimate=actor.AbilitySystem.Kit.Ultimate;
                Press(actor,Verb.Ultimate);
                if(ultimate.HoldToAim)
                {yield return new WaitForSecondsRealtime(.2f);actor.Intent.Set(Verb.Ultimate,false);}
                float until=Time.realtimeSinceStartup+1;
                while(!SharedUltimatePhase.BlocksActions&&Time.realtimeSinceStartup<until)yield return null;
                var phase=SharedUltimatePhase.Instance;
                Assert.IsTrue(phase!=null&&phase.Active,hero+" did not enter the real shared route.");
                Assert.AreEqual(1,phase.Commits.Count);Assert.AreEqual(0,actor.AbilitySystem.Kit.UltimateCharge);
                Assert.IsTrue(ultimate.ReservedForIntroduction);Assert.AreEqual(0,ultimate.WindupRemaining);
                Assert.IsNotNull(GameObject.Find("UltimateScene").GetComponent<UnityEngine.UI.RawImage>().texture,hero+" fell back because its prepared clip was missing.");
                yield return new WaitForSecondsRealtime(.7f);
                yield return GameplayShots.Render(Camera.main,hero+"-live-introduction",true,outDir:"Logs/shared-six-v1");
                until=Time.realtimeSinceStartup+4;
                while(phase.Active&&Time.realtimeSinceStartup<until)yield return null;
                Assert.IsFalse(phase.Active);Assert.IsFalse(PresentationClock.Held);
                Assert.IsFalse(ultimate.ReservedForIntroduction);Assert.IsTrue(ultimate.IsWindingUp||ultimate.IsActive);
                float revealedScale=0;
                if(hero=="nemu")
                {
                    Assert.IsFalse(familiar.IsDevouring,"The revealed visual cannot start the gameplay field early.");
                    revealedScale=familiar.transform.localScale.magnitude;
                    Assert.Greater(revealedScale,familiarBefore*6,"Kuro cannot shrink back to idle after the full introduction.");
                }
                yield return new WaitForSeconds(ultimate.Windup+.2f);
                Assert.IsFalse(ultimate.IsWindingUp,hero+" did not reach its real effect.");
                if(hero=="nemu")Assert.Greater(familiar.transform.localScale.magnitude,revealedScale*.92f,"Live activation repeated the grow-from-small reveal.");
                if(ultimate.Duration>.3f)Assert.IsTrue(ultimate.IsActive,hero+" lost its real active duration.");
                yield return GameplayShots.Render(Camera.main,hero+"-live-execution",true,outDir:"Logs/shared-six-v1");
                actor.Intent.Clear();
            }
        }

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
            // Rebinding heroes above is fixture setup. Wait for the same setup
            // warmup that the ordinary ready window gives the selected roster.
            float readyUntil=Time.realtimeSinceStartup+4;
            while((!PhaisterRitualWarmup.Ready||UltimateIntroductionCache.Find(sean,sean.GetComponent<Carrier>().Held!=null)==null||
                UltimateIntroductionCache.Find(phaister,phaister.GetComponent<Carrier>().Held!=null)==null)&&Time.realtimeSinceStartup<readyUntil)yield return null;
            Assert.IsTrue(PhaisterRitualWarmup.Ready);
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
            Assert.AreEqual(clock,phase.FrozenRoundTime,.001f);
            typeof(RoundDirector).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(round,null);
            Assert.AreEqual(clock,round.TimeLeft,.001f,"A physics callback already queued at receipt cannot advance the held clock");
            round.ApplySnapshot(clock+.2f,true,GameServices.Match.DefenderSlot,true);
            Assert.AreEqual(clock,round.TimeLeft,.001f,"An older snapshot cannot move the frozen presentation clock");
            round.ApplySnapshot(clock-.12f,true,GameServices.Match.DefenderSlot,true);
            Assert.AreEqual(clock,round.TimeLeft,.001f,"An already resumed host snapshot waits until local presentation release");
            Press(later,Verb.Ultimate);sean.Intent.Move=Vector2.up;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(clock,round.TimeLeft,.001f);Assert.AreEqual(at,sean.transform.position);
            Assert.AreEqual(later.AbilitySystem.Kit.UltimateCost,later.AbilitySystem.Kit.UltimateCharge,.001f);
            // Native six-hero screen/audio captures cover presentation. Synchronous
            // PNG encoding inside this deadline test would itself stall the player.
            // Keep the original duration bound and measure ordinary frame updates.
            PresentationClock.RequestScale(.5f);Assert.AreEqual(0,Time.timeScale);
            while(phase.Active&&Time.realtimeSinceStartup-started<4)yield return null;
            Assert.IsFalse(phase.Active);Assert.AreEqual(.5f,Time.timeScale);
            Debug.Log($"[SharedClock] input-to-accept={phase.Began-started:F4} hold={phase.ReleasedAt-phase.Began:F4} activation-ms={phase.ActivationMilliseconds:F3} observed={Time.realtimeSinceStartup-started:F4}");
            Assert.That(Time.realtimeSinceStartup-started,Is.InRange(2.65f,3.1f));
            Assert.AreEqual(1.55f,_ritualStart,.001f,"Measure the actual execution boundary, before the next frame legitimately advances its warning.");
            Assert.AreEqual(Mathf.Max(0,1.55f-(float)(Time.timeAsDouble-_ritualStartedAt)),phaister.AbilitySystem.Kit.Ultimate.WindupRemaining,.02f);
            Assert.IsFalse(phaister.AbilitySystem.Kit.Ultimate.ReservedForIntroduction);
            Assert.Greater(sean.AbilitySystem.Kit.Ultimate.WindupRemaining,.2f);
            yield return new WaitForSecondsRealtime(.35f);
            Assert.IsFalse(phase.Active,"A held button from the pause cannot launch the queued third ultimate.");
            Assert.AreEqual(later.AbilitySystem.Kit.UltimateCost,later.AbilitySystem.Kit.UltimateCharge,.001f);
            later.Intent.Set(Verb.Ultimate,false);yield return null;
            Press(later,Verb.Ultimate);yield return null;yield return null;
            Assert.IsTrue(later.AbilitySystem.IsAiming(HeroAbilitySystem.Slot.Ultimate),"Zack must retain his aimed hold before release.");
            later.Intent.Set(Verb.Ultimate,false);yield return null;yield return null;
            Assert.IsTrue(phase.Active,"Fresh aimed input after resume must still work.");
            Assert.AreEqual(1,phase.Commits.Count);
        }
        [UnityTest]
        public IEnumerator BlockedSceneShotFallsBackWithoutChangingTheSharedDuration()
        {
            yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
            var actor=GameServices.Round.PlayerAt(1);Hero(actor,"sean");
            actor.Teleport(new Vector3(0,.18f,-5));actor.transform.rotation=Quaternion.identity;
            Settings.SettingsStore.Current.CinematicCameraMotion=true;Settings.SettingsStore.Current.ReducedUiMotion=false;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="IntroductionCameraBlocker";
            wall.transform.position=new Vector3(0,2,-3);wall.transform.localScale=new Vector3(12,4,.3f);
            Physics.SyncTransforms();yield return null;
            Press(actor,Verb.Ultimate);yield return null;yield return null;
            var phase=SharedUltimatePhase.Instance;Assert.IsTrue(phase.Active);
            var image=GameObject.Find("UltimateScene").GetComponent<UnityEngine.UI.RawImage>();
            Assert.IsFalse(image.enabled,"Both blocked camera sides must retain the live view under the shared card.");
            Assert.IsTrue(PresentationClock.Held);Assert.AreEqual(0,actor.AbilitySystem.Kit.UltimateCharge);
            phase.Cancel();Object.Destroy(wall);yield return null;
            Assert.IsFalse(PresentationClock.Held);Assert.IsNull(GameObject.Find("SharedUltimateCanvas"));
        }
        [UnityTest]
        public IEnumerator FourCastersShareOneDeadlineAndASecondCohortDoesNotAccumulateState()
        {
            yield return MapRetrievalProbe.Load("Eskinita",GameMode.HeroStrike);
            var actors=GameServices.Round.Players.ToArray();string[] heroes={"sean","dante","cheska","phaister"};
            for(int i=0;i<4;i++)Hero(actors[i],heroes[i]);
            Settings.SettingsStore.Current.CinematicCameraMotion=false;Settings.SettingsStore.Current.ReducedUiMotion=true;
            float warm=Time.realtimeSinceStartup+4;
            while(!PhaisterRitualWarmup.Ready&&Time.realtimeSinceStartup<warm)yield return null;
            int starts=0;void Count(CharacterMotor actor,HeroKit kit,HeroAbility ability)=>starts++;
            HeroAbilitySystem.UltimateStarted+=Count;
            try
            {
                long previous=0;
                for(int run=0;run<2;run++)
                {
                    if(run>0)
                    {
                        GameServices.Round.EndRound();GameServices.Match.AdvanceRound();
                        foreach(var actor in actors){actor.ClearStun();actor.ClearTrip();actor.AbilitySystem.Kit.AddUltimateCharge(100);actor.Intent.Set(Verb.Ultimate,false);actor.Intent.CommitFrame();}
                        yield return null;
                    }
                    foreach(var actor in actors)Press(actor,Verb.Ultimate);
                    float until=Time.realtimeSinceStartup+1;while(!SharedUltimatePhase.BlocksActions&&Time.realtimeSinceStartup<until)yield return null;
                    var phase=SharedUltimatePhase.Instance;Assert.IsTrue(phase.Active);Assert.AreEqual(4,phase.Commits.Count);Assert.Greater(phase.PhaseId,previous);
                    yield return null;
                    var title=GameObject.Find("UltimateName");Assert.IsNotNull(title);
                    Assert.AreEqual("ULTIMATES INCOMING",title.GetComponent<UnityEngine.UI.Text>().text);
                    foreach(var actor in actors)
                    {
                        var label=GameObject.Find("CohortAbility"+actor.PlayerSlot);Assert.IsNotNull(label);
                        Assert.AreEqual(actor.AbilitySystem.Kit.Ultimate.Name,label.GetComponent<UnityEngine.UI.Text>().text,"A shared phase mislabeled another accepted ability as the primary one.");
                    }
                    previous=phase.PhaseId;double began=phase.Began;
                    foreach(var actor in actors)Assert.AreEqual(0,actor.AbilitySystem.Kit.UltimateCharge);
                    while(phase.Active&&SharedUltimatePhase.Now-began<4)yield return null;
                    Assert.IsFalse(phase.Active);Assert.That(phase.ReleasedAt-began,Is.InRange(2.79,3.05));Assert.AreEqual((run+1)*4,starts);
                    Assert.IsFalse(PresentationClock.Held);Assert.IsEmpty(phase.Commits);
                }
            }
            finally{HeroAbilitySystem.UltimateStarted-=Count;}
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
