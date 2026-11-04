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
        private int _ultimateStarts;
        private void Started(CharacterMotor actor, HeroKit kit, HeroAbility ability)
        { _ultimateStarts++; if (kit.HeroId == "phaister") { _ritualStart = ability.WindupRemaining; _ritualStartedAt = Time.timeAsDouble; } }
        [UnitySetUp] public IEnumerator Before()
        {
            _cameraMotion=Settings.SettingsStore.Current.CinematicCameraMotion; _reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            yield return PlayModeWorld.Reset();
            _ritualStart = -1; _ultimateStarts = 0; HeroAbilitySystem.UltimateStarted += Started;
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
        [UnityTest, Timeout(90000)]
        public IEnumerator RagoCurrentIntroductionReturnsToHisLiveAbility()
            => Profiles(new[] { "sean" });
        [UnityTest, Timeout(90000)]
        public IEnumerator DanteCurrentIntroductionReturnsToHisLiveAbility()
            => Profiles(new[] { "dante" });
        [UnityTest, Timeout(90000)]
        public IEnumerator YasminCurrentIntroductionReturnsToHerLiveAbility()
            => Profiles(new[] { "cheska" });
        [UnityTest, Timeout(120000)]
        public IEnumerator YasminOnBanaueUsesTheTrimmedIntroductionAndReturnsTheHud()
            => Profiles(new[] { "cheska" }, SceneFlow.Payo);
        [UnityTest, Timeout(90000)]
        public IEnumerator IlyasCurrentIntroductionReturnsToHisLiveAbility()
            => Profiles(new[] { "rafi" });

        [UnityTest, Timeout(120000)]
        public IEnumerator LiveBlindfoldSkillsAndCompanionsCannotObscureTheUltimatePicture()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita,GameMode.HeroStrike);
            var actor = GameServices.Round.PlayerAt(1); Hero(actor,"cheska");
            Camera.main.GetComponent<CameraRig>().Follow(actor,true);
            var ghost = NemuBlindfold.Place(actor.transform.position+Vector3.forward,Vector3.back,0);
            typeof(NemuBlindfold).GetMethod("Spring",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(ghost,new object[]{actor});
            var doll = VoodooDollBody.Spawn(GameServices.Round.PlayerAt(0),actor.transform.position+Vector3.right,0,false);
            var effect = GameObject.CreatePrimitive(PrimitiveType.Quad); effect.name="Live skill foreground regression";
            effect.transform.position=actor.transform.position+Vector3.forward; VfxRenderTag.Attach(effect);
            var effectLight=effect.AddComponent<Light>();effectLight.type=LightType.Point;
            var live = ghost.GetComponentsInChildren<Renderer>(true)
                .Concat(doll.GetComponentsInChildren<Renderer>(true)).Concat(effect.GetComponentsInChildren<Renderer>()).ToArray();
            UltimatePhaseView view=null; bool held=false,saw=false,hidden=true,lightOff=true;
            void Inspect(Camera camera)
            {
                if(camera.name!="UltimateSceneCamera")return;
                saw=true; hidden &= live.All(r=>r.forceRenderingOff);lightOff &= !effectLight.enabled;
            }
            try
            {
                typeof(PresentationClock).GetMethod("Hold",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);held=true;
                float until=Time.realtimeSinceStartup+8;
                while(UltimateIntroductionCache.Find(actor,actor.GetComponent<Carrier>().Held!=null)==null&&Time.realtimeSinceStartup<until)yield return null;
                Assert.IsNotNull(UltimateIntroductionCache.Find(actor,actor.GetComponent<Carrier>().Held!=null));
                view=new UltimatePhaseView(GameServices.Match.transform,new[]{new UltimateCommit(actor.PlayerSlot,1,
                    actor.transform.position,actor.transform.forward,actor.Intent.AimPoint,0)},UltimatePerformance.SecondsFor("cheska"));
                // Compare the scope with the immediate pre-render state, after
                // normal ghost/model warmup has completed its own visibility work.
                var original=live.Select(r=>r.forceRenderingOff).ToArray();
                var originallyEnabled=live.Select(r=>r.enabled).ToArray();
                Camera.onPreCull+=Inspect;
                view.Draw(.45f);
                Assert.IsTrue(saw,"The actual ultimate camera must have rendered.");
                Assert.IsTrue(hidden,"Live blindfold, skill and companion renderers obscured the ultimate picture.");
                Assert.IsTrue(lightOff,"The hidden skill's light must not contaminate the authored ultimate lighting.");
                for(int i=0;i<live.Length;i++)
                {
                    Assert.AreEqual(original[i],live[i].forceRenderingOff,"Live visibility was not restored after rendering.");
                    Assert.AreEqual(originallyEnabled[i],live[i].enabled,"A camera callback leaked its enabled state.");
                }
                Assert.IsTrue(effectLight.enabled);Assert.IsTrue(effect.GetComponent<Collider>().enabled);
                Assert.IsTrue(ghost.enabled);Assert.IsTrue(doll.gameObject.activeInHierarchy);
                // Simulate teardown after a render was interrupted inside its
                // visibility scope. Disposal must restore the same world flags.
                typeof(UltimatePhaseView).GetMethod("Hide",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                    .Invoke(view,new object[]{effect.transform});
                Assert.IsFalse(effectLight.enabled);
                view.Dispose();view=null;
                Assert.IsTrue(effectLight.enabled);Assert.IsFalse(effect.GetComponent<Renderer>().forceRenderingOff);
            }
            finally
            {
                Camera.onPreCull-=Inspect;view?.Dispose();
                if(held)typeof(PresentationClock).GetMethod("Release",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
                Object.Destroy(effect);if(ghost!=null)Object.Destroy(ghost.gameObject);if(doll!=null)Object.Destroy(doll.gameObject);
            }
        }

        private static IEnumerator Profiles(string[] heroes, string map = "Eskinita")
        {
            foreach (string hero in heroes)
            {
                yield return MapRetrievalProbe.Load(map,GameMode.HeroStrike);
                Hud.Instance.ShowReadyPrompt(false); var actor=GameServices.Round.PlayerAt(1); Hero(actor,hero);
                if (map != SceneFlow.Payo)
                {
                    actor.Teleport(new Vector3(0,.18f,-5));
                    foreach(var other in GameServices.Round.Players)
                        if(other!=actor)other.Teleport(new Vector3(6,other.transform.position.y,-6+other.PlayerSlot*3));
                }
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
                // Capture the phase's actual rendered target synchronously. The generic
                // screenshot helper yields layout frames and can outlast a short intro.
                var picture=GameObject.Find("UltimateScene").GetComponent<UnityEngine.UI.RawImage>();
                Assert.IsTrue(picture.enabled,hero+" created a texture but never displayed its cinematic. "+UltimatePhaseView.LastShotReport);
                if (map == SceneFlow.Payo)
                {
                    Assert.GreaterOrEqual(UltimatePhaseView.LastShot,
                        UltimatePerformance.For(hero).ShotIndexAt(UltimatePerformance.PlaybackStartFor(hero)),
                        "A blocked retained shot must not restore the cut heart or opening shots.");
                    var cast = GameObject.Find("RecordedCast-P"+(actor.PlayerSlot+1));
                    var body = cast.transform.GetChild(0);
                    float feet = WorldContactPresentation.ModelBottom(body.GetComponentsInChildren<Renderer>(), actor.transform.position.y);
                    Assert.Less(Mathf.Abs(feet-WorldGround.UnderFeet(actor.transform.position)), .35f,
                        "Yasmin's staged feet left the actual Banaue floor.");
                }
                var frame=(RenderTexture)picture.texture;
                var previousTarget=RenderTexture.active;
                var pixels=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);
                try
                {
                    RenderTexture.active=frame;
                    pixels.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);pixels.Apply();
                    string folder=System.Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/shared-six-v1";
                    System.IO.Directory.CreateDirectory(folder);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,hero+"-actual-phase-frame.png"),pixels.EncodeToPNG());
                }
                finally{RenderTexture.active=previousTarget;Object.Destroy(pixels);}
                yield return GameplayShots.Render(Camera.main,hero+"-live-introduction",true,
                    outDir:System.Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/shared-six-v1");
                until=Time.realtimeSinceStartup+4;
                while(phase.Active&&Time.realtimeSinceStartup<until)yield return null;
                Assert.IsFalse(phase.Active);Assert.IsFalse(PresentationClock.Held);
                if (map == SceneFlow.Payo)
                    Assert.IsTrue(Hud.Instance.GetComponent<TumpMatchReadout>().Canvas.isActiveAndEnabled,
                        "The Banaue HUD must return after the ultimate.");
                Assert.IsFalse(ultimate.ReservedForIntroduction);
                if(hero=="rafi")
                {
                    // Baha activates instantly into its host-owned moving field;
                    // unlike the older six profiles it has no ability-duration timer.
                    var flood=RafiWaterField.Active.SingleOrDefault(f=>f!=null&&f.Capture().Type==TumbangPreso.Net.WorldEffectSnapshot.Kind.Baha&&f.Capture().Owner==actor.PlayerSlot);
                    Assert.IsNotNull(flood,"Ilyas must create his actual owned Baha field after the introduction.");
                    Assert.Greater(flood.Remaining,0);
                }
                else if (hero == "cheska")
                {
                    // Her introduction is the windup; the actual global freeze is
                    // applied at release and has no separate active ability timer.
                    foreach (var victim in GameServices.Round.Players)
                        if (victim != actor)
                        {
                            Assert.Greater(victim.StunLeft, 0, "Absolute Zero must freeze every other real body.");
                            Assert.AreEqual(StunElement.Ice, victim.StunElement);
                            Assert.IsTrue(victim.IsChilled);
                        }
                }
                else Assert.IsTrue(ultimate.IsWindingUp||ultimate.IsActive);
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
                yield return GameplayShots.Render(Camera.main,hero+"-live-execution",true,
                    outDir:System.Environment.GetEnvironmentVariable("TUMP_EVIDENCE")??"Logs/shared-six-v1");
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
            while((UltimateIntroductionCache.Find(sean,sean.GetComponent<Carrier>().Held!=null)==null||
                UltimateIntroductionCache.Find(phaister,phaister.GetComponent<Carrier>().Held!=null)==null)&&Time.realtimeSinceStartup<readyUntil)yield return null;
            // HIGOP joins on release; arm its aim before the instant cast's receipt frame.
            Press(phaister,Verb.Ultimate);yield return null;yield return null;
            Assert.IsTrue(phaister.AbilitySystem.IsAiming(HeroAbilitySystem.Slot.Ultimate));
            Assert.IsFalse(SharedUltimatePhase.BlocksActions);
            Assert.AreEqual(phaister.AbilitySystem.Kit.UltimateCost,phaister.AbilitySystem.Kit.UltimateCharge);
            float started=Time.realtimeSinceStartup;
            Press(sean,Verb.Ultimate);phaister.Intent.Set(Verb.Ultimate,false);
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
            // REFINE-2.11: the cohort shares its LONGEST hero's authored introduction.
            double length=SharedUltimatePhase.CohortSeconds(phase.Commits);
            Assert.AreEqual(length,phase.Duration,.001);
            Assert.AreEqual(System.Math.Max(Visual.UltimatePerformance.SecondsFor("sean"),Visual.UltimatePerformance.SecondsFor("phaister")),length,.001);
            while(phase.Active&&Time.realtimeSinceStartup-started<length+1.5f)yield return null;
            Assert.IsFalse(phase.Active);Assert.AreEqual(.5f,Time.timeScale);
            Debug.Log($"[SharedClock] input-to-accept={phase.Began-started:F4} hold={phase.ReleasedAt-phase.Began:F4} activation-ms={phase.ActivationMilliseconds:F3} observed={Time.realtimeSinceStartup-started:F4}");
            Assert.That(Time.realtimeSinceStartup-started,Is.InRange((float)length-.15f,(float)length+.3f));
            Assert.AreEqual(2,_ultimateStarts,"Each accepted member must execute exactly once after the shared phase.");
            Assert.AreEqual(VoodooRules.HigopCastSeconds,_ritualStart,.001f,"Measure the actual execution boundary, before the next frame legitimately advances its warning.");
            Assert.AreEqual(Mathf.Max(0,VoodooRules.HigopCastSeconds-(float)(Time.timeAsDouble-_ritualStartedAt)),phaister.AbilitySystem.Kit.Ultimate.WindupRemaining,.02f);
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
                    // Stage only abilities that currently aim on hold. Phaister's
                    // sentient doll is an immediate cast, unlike her retired OMEN.
                    var aiming=actors.Where(a=>a.AbilitySystem.Kit.Ultimate.HoldToAim).ToArray();
                    foreach(var actor in aiming)Press(actor,Verb.Ultimate);
                    if(aiming.Length>0)
                    {
                        yield return null;yield return null;
                        foreach(var actor in aiming)
                        {
                            Assert.IsTrue(actor.AbilitySystem.IsAiming(HeroAbilitySystem.Slot.Ultimate));
                            Assert.AreEqual(actor.AbilitySystem.Kit.UltimateCost,actor.AbilitySystem.Kit.UltimateCharge);
                        }
                        Assert.IsFalse(SharedUltimatePhase.BlocksActions);
                    }
                    foreach(var actor in actors)if(!actor.AbilitySystem.Kit.Ultimate.HoldToAim)Press(actor,Verb.Ultimate);
                    foreach(var actor in aiming)actor.Intent.Set(Verb.Ultimate,false);
                    float until=Time.realtimeSinceStartup+1;while(!SharedUltimatePhase.BlocksActions&&Time.realtimeSinceStartup<until)yield return null;
                    var phase=SharedUltimatePhase.Instance;Assert.IsTrue(phase.Active);Assert.AreEqual(4,phase.Commits.Count);Assert.Greater(phase.PhaseId,previous);
                    yield return null;
                    var title=GameObject.Find("UltimateName");Assert.IsNotNull(title);
                    Assert.AreEqual("ULTIMATES INCOMING",title.GetComponent<UnityEngine.UI.Text>().text);
                    Canvas.ForceUpdateCanvases();
                    foreach(var actor in actors)
                    {
                        var label=GameObject.Find("CohortAbility"+actor.PlayerSlot);Assert.IsNotNull(label);
                        Assert.AreEqual(actor.AbilitySystem.Kit.Ultimate.Name,label.GetComponent<UnityEngine.UI.Text>().text,"A shared phase mislabeled another accepted ability as the primary one.");
                        var identity=GameObject.Find("CohortSeat"+actor.PlayerSlot).GetComponent<UnityEngine.UI.Text>();
                        Assert.Greater(identity.cachedTextGenerator.vertexCount,4,"The caster name exists as a string but is clipped out of the rendered card.");
                        Assert.Greater(label.GetComponent<UnityEngine.UI.Text>().cachedTextGenerator.vertexCount,4,"The accepted ability label generated no visible glyphs.");
                    }
                    previous=phase.PhaseId;double began=phase.Began;
                    foreach(var actor in actors)Assert.AreEqual(0,actor.AbilitySystem.Kit.UltimateCharge);
                    double length=phase.Duration;
                    Assert.AreEqual(SharedUltimatePhase.CohortSeconds(phase.Commits),length,.001,"Four casters must share their longest member's introduction.");
                    while(phase.Active&&SharedUltimatePhase.Now-began<length+1.2)yield return null;
                    Assert.IsFalse(phase.Active);Assert.That(phase.ReleasedAt-began,Is.InRange(length-.01,length+.25));Assert.AreEqual((run+1)*4,starts);
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
