using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ReplayRetentionTests
    {
        [UnitySetUp]public IEnumerator Before()=>PlayModeWorld.Reset();
        [UnityTearDown]public IEnumerator After()=>PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator FirstBoulderLoadKeepsReplayTopologyAndStandalonePlayback()
        {
            yield return MapRetrievalProbe.Load("Eskinita",Core.GameMode.HeroStrike);
            Object.FindAnyObjectByType<MatchArrivalPresentation>()?.Cancel();
            var round=GameServices.Round;round.BeginRound();
            foreach(var actor in round.Players)
            {
                var brain=actor.GetComponent<AIController>();if(brain!=null)brain.enabled=false;
                actor.Intent.Clear();actor.Intent.Parked=true;
            }
            var shoe=round.PlayerAt(3).GetComponent<Carrier>().Held;Assert.IsNotNull(shoe);
            var archive=Object.FindAnyObjectByType<MatchReplayArchive>();Assert.IsNotNull(archive);
            yield return new WaitForSeconds(.3f);
            float start=Time.time;
            yield return new WaitForSeconds(.35f);
            Assert.IsTrue(archive.TryCaptureSession(start,Time.time,1,out var before,out string error,true),error);
            var first=before.Objects.Single(o=>o.Kind==RecordedObjectKind.Slipper&&o.Seat==3);
            int stone=System.Array.FindIndex(first.Pose.Paths,p=>p=="BoulderStone"||p.EndsWith("/BoulderStone"));
            Assert.GreaterOrEqual(stone,0,"Prepare the actual runtime stone before its first use.");
            Assert.IsTrue(first.Pose.Samples.All(s=>!s.Active[stone]),"An unloaded shoe must keep the prepared stone hidden.");
            Assert.IsTrue(shoe.HostLoadTimedAffinity(SlipperAffinity.Concussed,Core.GeoRules.BoulderLoadSeconds));
            yield return new WaitForSeconds(.9f);
            Assert.IsTrue(archive.TryCaptureSession(start,Time.time,2,out var after,out error,true),error);
            var loaded=after.Objects.Single(o=>o.Kind==RecordedObjectKind.Slipper&&o.Seat==3);
            CollectionAssert.AreEqual(first.Pose.Paths,loaded.Pose.Paths);
            Assert.IsTrue(loaded.Pose.Samples.Any(s=>s.Active[stone]),"The recorded load must actually show its stone.");
            var owner=new GameObject("Boulder recording playback proof");
            try
            {
                using(var view=new RecordedWorldView(owner.transform,after,true))
                {Assert.IsTrue(view.Ready,view.UnavailableReason);view.Draw(after.End);}
            }
            finally{Object.Destroy(owner);}
        }
        [UnityTest]
        public IEnumerator DelayedHalftimeCanOpenAnAlreadyRetainedHighlight()=>DelayedHalftime(false);
        [UnityTest]
        public IEnumerator HalftimeWaitsForItsNominatedHighlightBytes()=>DelayedHalftime(true);
        [UnityTest]
        public IEnumerator MissingNominatedHighlightStillFallsBackWithinTheSharedBreak()=>DelayedHalftime(true,true);
        [UnityTest]
        public IEnumerator HalftimeRetainedBlindfoldHighlightDrawsUntilStandings()=>DelayedHalftime(false,false,true);
        [UnityTest]
        public IEnumerator FourRoundHostNominatesReplayAtItsMiddleAndAdvancesAfterStandings()
        {
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            try
            {
                Settings.SettingsStore.Current.ReducedUiMotion=false;
                yield return MapRetrievalProbe.Load("Eskinita");
                Object.FindAnyObjectByType<MatchArrivalPresentation>()?.Cancel();
                var rules=UI.SceneFlow.SelectedRules.Clone();rules.Rounds=4;UI.SceneFlow.PinSelectedRules(rules);
                var round=GameServices.Round;var match=GameServices.Match;
                round.BeginRound();
                foreach(var actor in round.Players)actor.Teleport(new Vector3(6,.12f,-6+actor.PlayerSlot*3));
                var defender=round.PlayerAt(0);var victim=round.PlayerAt(1);
                defender.Teleport(new Vector3(0,.12f,-4));victim.Teleport(new Vector3(0,.12f,-3));
                defender.transform.forward=Vector3.forward;
                yield return new WaitForSeconds(2.5f);
                Assert.IsTrue(defender.GetComponent<CombatVerbs>().HostResolvePunch(defender.transform.position,defender.transform.forward));
                yield return new WaitForSeconds(1.6f);
                var archive=Object.FindAnyObjectByType<MatchReplayArchive>();
                Assert.IsNotEmpty(archive.Clips,archive.LastSkip);
                long nominated=archive.Clips[0].Clip.Id;
                round.EndRound();match.AdvanceRound();yield return null;
                round.EndRound();RoundBreakFreezeTests.DrawFrame();match.BeginIntermission();
                var phase=HalftimePresentation.Instance;
                Assert.IsTrue(phase.Active);Assert.IsTrue(phase.IsHalftime,"A four-round host must schedule halftime after round two.");
                Assert.AreEqual(2,phase.CompletedRound);Assert.AreEqual(nominated,phase.ClipId);
                double began=phase.Began;bool captured=false;
                while(SharedUltimatePhase.Now-began<5.6)
                {
                    Assert.IsNull(phase.FallbackReason);
                    if(SharedUltimatePhase.Now-began>=1)
                    {
                        Assert.IsTrue(phase.HasReplay,"The normal host break must draw its nominated footage.");
                        if(!captured){SaveReplayFrame(phase.ReplayFrame,"four-round-host-highlight.png");captured=true;}
                    }
                    yield return null;
                }
                while(SharedUltimatePhase.Now-began<6)yield return null;
                Assert.IsTrue(UI.RoleSwapCard.Showing);Assert.IsNull(phase.FallbackReason);
                Assert.AreEqual(2,match.RoundNumber);Assert.IsTrue(PresentationClock.Held);
                while(phase.Active&&SharedUltimatePhase.Now-began<10.5)yield return null;
                Assert.AreEqual(3,match.RoundNumber);Assert.IsFalse(phase.Active);Assert.IsFalse(PresentationClock.Held);
                round.EndRound();match.AdvanceRound();yield return null;
                round.EndRound();match.BeginIntermission();
                Assert.IsFalse(match.MatchInProgress);Assert.IsFalse(phase.Active,"The final round cannot start another halftime.");
            }
            finally {Settings.SettingsStore.Current.ReducedUiMotion=reduced;}
        }
        [UnityTest,Explicit]
        public IEnumerator CapturedOwnerHighlightRendersEveryRecordedFrame()
        {
            string path=System.Environment.GetEnvironmentVariable("TUMP_REPLAY_CLIP");
            Assert.IsTrue(System.IO.File.Exists(path),"Set TUMP_REPLAY_CLIP to the saved failing canonical clip.");
            Assert.IsTrue(RecordedMatchClip.TryDecode(System.IO.File.ReadAllBytes(path),out var clip,out string error),error);
            yield return MapRetrievalProbe.Load(clip.Map,clip.Mode);
            Object.FindAnyObjectByType<MatchArrivalPresentation>()?.Cancel();
            var owner=new GameObject("Owner highlight playback proof");
            try
            {
                using(var view=new RecordedWorldView(owner.transform,clip,true))
                {
                    Assert.IsTrue(view.Ready,view.UnavailableReason);
                    foreach(var frame in clip.FieldFrames)view.Draw(Mathf.Clamp(frame.Time,clip.Start,clip.End));
                    view.Draw(clip.Contact);
                    var previous=RenderTexture.active;Texture2D image=null;
                    try
                    {
                        RenderTexture.active=view.Target;
                        image=new Texture2D(view.Target.width,view.Target.height,TextureFormat.RGB24,false);
                        image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();
                        string folder=System.IO.Path.GetFullPath("Logs/halftime-live-1021");System.IO.Directory.CreateDirectory(folder);
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,"actual-owner-highlight.png"),image.EncodeToPNG());
                    }
                    finally {RenderTexture.active=previous;if(image!=null)Object.DestroyImmediate(image);}
                    Assert.IsNotNull(view.Target);Assert.IsTrue(view.Target.IsCreated());
                }
            }
            finally {Object.DestroyImmediate(owner);}
        }
        private static IEnumerator DelayedHalftime(bool delayedBytes,bool missingBytes=false,bool blindfold=false)
        {
            bool reduced=Settings.SettingsStore.Current.ReducedUiMotion;
            try
            {
                Settings.SettingsStore.Current.ReducedUiMotion=false;
                yield return MapRetrievalProbe.Load("Eskinita");
                Object.FindAnyObjectByType<MatchArrivalPresentation>()?.Cancel();
                var round=GameServices.Round;var match=GameServices.Match;
                round.BeginRound();
                if(blindfold)
                {
                    Abilities.NemuBlindfold.Place(new Vector3(4,0,4),Vector3.forward,1,1);
                    var shoe=round.PlayerAt(3).GetComponent<Carrier>().Held;
                    Assert.IsNotNull(shoe);
                    Assert.IsTrue(shoe.HostLoadTimedAffinity(SlipperAffinity.Concussed,Core.GeoRules.BoulderLoadSeconds));
                }
                foreach(var actor in round.Players)
                    actor.Teleport(new Vector3(6,.12f,-6+actor.PlayerSlot*3));
                var defender=round.PlayerAt(0);var victim=round.PlayerAt(1);
                defender.Teleport(new Vector3(0,.12f,-4));victim.Teleport(new Vector3(0,.12f,-3));
                defender.transform.forward=Vector3.forward;
                yield return new WaitForSeconds(2.5f);
                Assert.IsTrue(defender.GetComponent<CombatVerbs>().HostResolvePunch(defender.transform.position,defender.transform.forward));
                yield return new WaitForSeconds(1.6f);
                var archive=Object.FindAnyObjectByType<MatchReplayArchive>();
                Assert.IsNotEmpty(archive.Clips,archive.LastSkip);
                var retained=archive.Clips[0];var clip=retained.Clip;
                if(blindfold)Assert.IsTrue(clip.FieldFrames.Any(f=>f.Fields.Any(s=>s.State.Type==Net.WorldEffectSnapshot.Kind.Blindfold)),
                    "The regression needs the actual captured effect that broke the owner's replay.");
                if(blindfold)
                {
                    var shoe=round.PlayerAt(3).GetComponent<Carrier>().Held;
                    Assert.IsTrue(shoe.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="BoulderStone"),"The actual loaded shoe must build its runtime shape.");
                    var recorded=clip.Objects.Single(o=>o.Kind==RecordedObjectKind.Slipper&&o.Seat==3);
                    Assert.IsTrue(recorded.Pose.Paths.Any(p=>p=="BoulderStone"||p.Contains("/BoulderStone")),
                        "Record the runtime shape under the equipped skin's actual hierarchy: "+string.Join(",",recorded.Pose.Paths));
                }
                while(match.RoundNumber<4){round.EndRound();match.AdvanceRound();yield return null;}
                round.EndRound();
                RoundBreakFreezeTests.DrawFrame();
                var phase=HalftimePresentation.Ensure();
                // A peer receives the host's timestamp, not a fresh local start.
                var clips=(System.Collections.Generic.List<MatchReplayArchive.Retained>)typeof(MatchReplayArchive)
                    .GetField("_clips",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(archive);
                if(delayedBytes)clips.Clear();
                double began=SharedUltimatePhase.Now-(delayedBytes ? .25 : .8);
                Assert.IsTrue(phase.Receive(match.PresentationMatchId,4,0,began,clip.Id,true,1));
                yield return null;yield return null;
                if(delayedBytes)
                {
                    Assert.IsNull(phase.FallbackReason,"A first missing-byte check cannot abandon the nominated clip.");
                    Assert.IsFalse(phase.HasReplay);
                    yield return new WaitForSecondsRealtime(.9f);
                    Assert.IsNull(phase.FallbackReason,"A short transport delay cannot abandon footage that still fits.");
                    if(!missingBytes)clips.Add(retained);
                    yield return null;yield return null;
                }
                if(missingBytes)
                {
                    while(phase.FallbackReason==null&&SharedUltimatePhase.Now-began<5.8)yield return null;
                    Assert.IsFalse(phase.HasReplay);
                    Assert.AreEqual("Replay unavailable on this screen",phase.FallbackReason);
                    Assert.Less(SharedUltimatePhase.Now-began,phase.Duration,"Missing bytes cannot extend the break.");
                }
                else
                {
                    Assert.IsTrue(phase.HasReplay,"A complete highlight was rejected solely because the break arrived late: "+phase.FallbackReason);
                    Assert.IsNull(phase.FallbackReason);
                    if(blindfold)
                    {
                        bool captured=false;
                        while(SharedUltimatePhase.Now-began<5.6)
                        {
                            Assert.IsTrue(phase.HasReplay,phase.FallbackReason);
                            Assert.IsNull(phase.FallbackReason);
                            if(!captured&&SharedUltimatePhase.Now-began>=3)
                            {SaveReplayFrame(phase.ReplayFrame,"new-halftime-highlight.png");captured=true;}
                            yield return null;
                        }
                        while(SharedUltimatePhase.Now-began<6)yield return null;
                        Assert.IsNull(phase.FallbackReason,"Drawing a later recorded field cannot abandon the clip.");
                        Assert.IsTrue(UI.RoleSwapCard.Showing,"The complete replay must yield to standings.");
                    }
                }
                Assert.AreEqual(began,phase.Began,"Opening footage cannot restart the shared break deadline.");
                Assert.IsTrue(PresentationClock.Held);
                phase.End(false);
                Assert.IsFalse(PresentationClock.Held);
            }
            finally { Settings.SettingsStore.Current.ReducedUiMotion=reduced; }
        }
        private static void SaveReplayFrame(RenderTexture target,string name)
        {
            var previous=RenderTexture.active;
            var resolved=RenderTexture.GetTemporary(target.width,target.height,0,RenderTextureFormat.ARGB32);
            Texture2D image=null;
            try
            {
                Graphics.Blit(target,resolved);RenderTexture.active=resolved;
                image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();
                string folder=System.IO.Path.GetFullPath("Logs/halftime-live-1021");System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,name),image.EncodeToPNG());
            }
            finally {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(resolved);if(image!=null)Object.DestroyImmediate(image);}
        }
        [UnityTest]
        public IEnumerator RetainedCatchPreservesVictimCoatHeldPropAndSeparateWorldAudio()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            var round=GameServices.Round;var taya=round.PlayerAt(0);var victim=round.PlayerAt(1);
            foreach(var actor in round.Players)actor.Teleport(new Vector3(6,.12f,-6+actor.PlayerSlot*3));
            taya.Teleport(new Vector3(0,.12f,-4));victim.Teleport(new Vector3(0,.12f,-3));taya.transform.forward=Vector3.forward;
            var shoe=Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include).First(s=>s.SeatOfOrigin==1);shoe.gameObject.SetActive(true);Assert.IsTrue(shoe.HostForceEquip(victim));
            yield return new WaitForSeconds(2.5f);
            Assert.IsTrue(taya.GetComponent<CombatVerbs>().HostResolvePunch(taya.transform.position,taya.transform.forward));
            yield return new WaitForSeconds(1.6f);
            var archive=Object.FindAnyObjectByType<MatchReplayArchive>();Assert.AreEqual(1,archive.Clips.Count,archive.LastSkip);
            Assert.IsTrue(RecordedMatchClip.TryDecode(archive.Clips[0].Bytes,out var clip,out var error),error);
            Assert.AreEqual("CATCH",clip.Reason);Assert.AreEqual(1,clip.Subject);
            var body=clip.Objects.First(o=>o.Kind==RecordedObjectKind.Player&&o.Seat==1);
            Assert.IsTrue(body.Pose.Samples.Any(s=>s.HasCoat&&s.Frost>.3f),"The actual caught coat survives into the retained aftermath");
            Assert.IsTrue(clip.Objects.First(o=>o.Kind==RecordedObjectKind.Slipper&&o.Seat==1).Pose.Samples.Any(s=>s.Holder==1));
            Assert.IsTrue(clip.Sounds.Any(s=>s.Id=="tag"||s.Id=="downed"),"The accepted contact sound is retained");
            int recorded=0;void Heard(string id,Vector3 at,float pitch,float gain)=>recorded++;
            AudioDirector.WorldCuePlayed+=Heard;
            try{var sound=clip.Sounds[0];GameServices.Audio.PlayReplayCue(sound.Id,sound.Pitch,sound.Gain,0);Assert.AreEqual(0,recorded,"Replay sound cannot record/relay itself");}
            finally{AudioDirector.WorldCuePlayed-=Heard;GameServices.Audio.StopReplayCues();}
        }
        [UnityTest]
        public IEnumerator ScheduledBreakUsesOneDeadlineAndNeverRunsAfterTheFinalRound()
        {
            int mip=QualitySettings.globalTextureMipmapLimit;
            QualitySettings.globalTextureMipmapLimit=2;
            try
            {
                Assert.IsTrue(HalftimePresentation.IsMiddleBreak(2,4));
                Assert.IsFalse(HalftimePresentation.IsMiddleBreak(1,2));
                Assert.IsTrue(HalftimePresentation.IsMiddleBreak(3,6));
                Assert.IsTrue(HalftimePresentation.IsMiddleBreak(4,8));
                Assert.IsFalse(HalftimePresentation.IsMiddleBreak(8,8));
                yield return MapRetrievalProbe.Load("Eskinita");
                var round=GameServices.Round;var match=GameServices.Match;
                RoundBreakFreezeTests.DrawFrame();
                round.EndRound();match.BeginIntermission();var phase=HalftimePresentation.Instance;
                Assert.IsTrue(phase.Active);Assert.IsFalse(phase.IsHalftime);Assert.AreEqual(3.5f,phase.Duration);
                Assert.IsTrue(PresentationClock.Held);Assert.IsTrue(PresentationClock.BlocksInput);
                Assert.IsFalse(phase.HasReplay);Assert.IsNotNull(phase.FrozenFrame);
                double began=phase.Began;
                while(match.RoundNumber==1&&SharedUltimatePhase.Now-began<4.5)yield return null;
                Assert.AreEqual(2,match.RoundNumber);Assert.That(SharedUltimatePhase.Now-began,Is.InRange(3.4,4.0));
                while(match.RoundNumber<4){round.EndRound();match.AdvanceRound();yield return null;}
                round.EndRound();match.BeginIntermission();
                Assert.IsTrue(phase.IsHalftime);Assert.IsTrue(PresentationClock.Held);Assert.IsTrue(PresentationClock.BlocksInput);
                Assert.AreEqual(10,phase.Duration);int score=match.ScoreFor(1);float time=Time.time;
                match.SkipBuffer();Assert.IsFalse(match.SkipRequested,"Halftime has one shared end");
                yield return new WaitForSecondsRealtime(.4f);
                Assert.AreEqual(time,Time.time);Assert.AreEqual("No complete highlight this half",phase.FallbackReason);Assert.IsNotNull(phase.FrozenFrame);Assert.IsFalse(phase.HasReplay);Assert.AreEqual(score,match.ScoreFor(1));
                Assert.IsTrue(Object.FindObjectsByType<UI.OffscreenIndicators>().All(i=>!i.CanMarkerVisible),"Live world markers cannot paint over halftime footage or standings");
                // Join the same deadline late; duplicates cannot restart its ten seconds.
                Assert.IsFalse(phase.Receive(phase.MatchId,phase.CompletedRound,phase.NextTaya,SharedUltimatePhase.Now,0,true,1));
                double end=phase.Began+10;
                while(SharedUltimatePhase.Now<end+.15)yield return null;
                Assert.AreEqual(5,match.RoundNumber);Assert.IsFalse(PresentationClock.Held);Assert.IsFalse(phase.Active);
                while(match.RoundNumber<8){round.EndRound();match.AdvanceRound();yield return null;}
                round.EndRound();match.BeginIntermission();Assert.IsFalse(phase.Active);Assert.IsFalse(match.MatchInProgress);
            }
            finally { QualitySettings.globalTextureMipmapLimit=mip; }
        }
        [UnityTest]
        public IEnumerator AllPersistentFieldFamiliesUseRenderOnlyPlayback()
        {
            yield return MapRetrievalProbe.Load("Eskinita",Core.GameMode.HeroStrike);
            var position=new Vector3(3,0,3);
            Abilities.HeroHazards.SpawnIceSheet(position,2,8,1,1,silent:true);
            Abilities.HeroHazards.SpawnIceBarricade(position+Vector3.right*3,Vector3.forward,8,1,1,false,silent:true);
            Abilities.HeroHazards.SpawnFireTrail(position+Vector3.back*3,1,8,1,Vector3.forward);
            Abilities.HeroHazards.SpawnShockTrail(position+Vector3.left*3,1,8,2,1,Vector3.forward);
            Abilities.HeroHazards.SpawnSupernovaCrater(position+Vector3.forward*3,2,8,1);
            Abilities.HeroHazards.SpawnHexSigil(position+Vector3.right*5,2,8,3,1,silent:true);
            Visual.DanteFissurePillar.Create(position+Vector3.left*5,Vector3.forward,1,8);
            yield return null;
            var fields=Net.WorldEffectSnapshot.Capture();Assert.AreEqual(7,fields.Count);
            var stage=new GameObject("FieldReplayProof");
            int score=GameServices.Match.ScoreFor(1);
            foreach(var field in fields)
            {
                using(var view=new RecordedFieldView(stage.transform,field))
                {
                    view.Sample(field,0);view.Visible(true);
                    Assert.IsEmpty(view.Root.GetComponentsInChildren<Collider>(true));
                    Assert.IsEmpty(view.Root.GetComponentsInChildren<Abilities.HazardVolume>(true));
                    Assert.AreEqual(7,Net.WorldEffectSnapshot.Capture().Count,"Render copies cannot enter snapshots as live fields");
                    Assert.Greater(view.Root.GetComponentsInChildren<Renderer>(true).Length,0);
                }
            }
            Assert.AreEqual(score,GameServices.Match.ScoreFor(1));Object.Destroy(stage);
        }
        [UnityTest]
        public IEnumerator RealExchangeRetainsDetachedBodiesAndPropsAcrossTheRoundBoundary()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            var round=GameServices.Round;var match=GameServices.Match;
            var archive=Object.FindAnyObjectByType<MatchReplayArchive>();Assert.IsNotNull(archive);
            var thrower=round.PlayerAt(1);var shoe=thrower.GetComponent<Carrier>().Held;
            foreach(var actor in round.Players)actor.Teleport(new Vector3(6,actor.transform.position.y,-5+actor.PlayerSlot*3));
            yield return new WaitForSeconds(2.5f);
            int serial=round.Lata.HostKnockdownSerial;
            shoe.HostThrow(thrower,round.Lata.transform.position+new Vector3(0,1.2f,-2),Vector3.forward*12);
            float until=Time.time+2;
            while(round.Lata.IsUpright&&Time.time<until)yield return null;
            Assert.AreEqual(serial+1,round.Lata.HostKnockdownSerial);
            yield return new WaitForSeconds(1.6f);
            Assert.AreEqual(1,archive.Clips.Count,archive.LastSkip);
            var retained=archive.Clips[0];var bytes=(byte[])retained.Bytes.Clone();
            Assert.Less(bytes.Length,RecordedMatchClip.ByteLimit);
            Assert.IsTrue(RecordedMatchClip.TryDecode(bytes,out var clip,out var error),error);
            Assert.AreEqual(4,clip.Objects.Count(o=>o.Kind==RecordedObjectKind.Player));
            Assert.AreEqual(1,clip.Objects.Count(o=>o.Kind==RecordedObjectKind.Can));
            Assert.GreaterOrEqual(clip.Objects.Count(o=>o.Kind==RecordedObjectKind.Slipper),3);
            Assert.AreEqual(1,clip.Round);Assert.AreEqual("CAN KNOCKDOWN",clip.Reason);
            Assert.IsTrue(clip.Objects.All(o=>o.Pose.Samples.Any(s=>Mathf.Abs(s.Time-clip.Contact)<.00001f)),"Exact contact keys survive compression");
            using(var view=new RecordedWorldView(archive.transform,clip))
            {
                Assert.IsTrue(view.Ready,"A clear actual-court replay angle must render");
                var liveEffect=GameObject.CreatePrimitive(PrimitiveType.Sphere);liveEffect.name="LiveEffectIsolationProof";
                liveEffect.GetComponent<Collider>().enabled=false;Visual.VfxRenderTag.Attach(liveEffect);
                Visual.ComicPopup.Spawn(Vector3.up*2,"CURRENT WORLD",Color.magenta,2);
                yield return null;
                var liveCanvases=Object.FindObjectsByType<Visual.ComicPopup>().SelectMany(p=>p.GetComponentsInChildren<Canvas>()).ToArray();
                var life=Object.FindFirstObjectByType<AmbientLife>();Assert.IsNotNull(life);
                life.enabled=false;
                // Staged visibility witness, not an ordinary animal route: this
                // present-time dog must never enter the recorded exchange.
                var dog=life.transform.Find("Ambient aspin-tan");Assert.IsNotNull(dog);
                dog.position=new Vector3(-1.5f,.12f,-1.5f);
                var ambient=life.GetComponentsInChildren<Renderer>(true);Assert.IsNotEmpty(ambient);
                ambient[ambient.Length-1].forceRenderingOff=true;
                var ambientFlags=ambient.Select(r=>r.forceRenderingOff).ToArray();
                var dogAt=dog.position;
                bool observed=false,ambientHidden=false;
                void BeforeRender(Camera camera)
                {if(camera.name!="RecordedWorldCamera")return;observed=true;ambientHidden=ambient.All(r=>r.forceRenderingOff);Assert.IsTrue(liveEffect.GetComponent<Renderer>().forceRenderingOff);Assert.IsTrue(liveCanvases.All(c=>!c.enabled));}
                Camera.onPreRender+=BeforeRender;
                try{view.Draw(clip.Contact,false);}
                finally{Camera.onPreRender-=BeforeRender;}
                Assert.IsTrue(observed);Assert.IsFalse(liveEffect.GetComponent<Renderer>().forceRenderingOff);Assert.IsTrue(liveCanvases.All(c=>c.enabled));Object.Destroy(liveEffect);
                Assert.IsTrue(view.Target.IsCreated());
                var capture=new Texture2D(view.Target.width,view.Target.height,TextureFormat.RGB24,false);
                var previous=RenderTexture.active;RenderTexture.active=view.Target;capture.ReadPixels(new Rect(0,0,view.Target.width,view.Target.height),0,0);capture.Apply();RenderTexture.active=previous;
                string output=System.Environment.GetEnvironmentVariable("TUMP_AMBIENT_REPLAY_OUT")??"Logs/replay-retained-view";
                System.IO.Directory.CreateDirectory(output);System.IO.File.WriteAllBytes(System.IO.Path.Combine(output,"contact.png"),capture.EncodeToPNG());Object.Destroy(capture);
                CollectionAssert.AreEqual(ambientFlags,ambient.Select(r=>r.forceRenderingOff).ToArray(),"Replay changed live ambient visibility");
                Assert.AreEqual(dogAt,dog.position,"Replay moved a live animal");
                life.enabled=true;
                Assert.IsTrue(ambientHidden,"Present-time animals leaked into the retained past event");
            }
            round.EndRound();match.AdvanceRound();yield return new WaitForSeconds(.2f);
            Assert.AreEqual(1,archive.Clips.Count,"A round reset cannot delete the halftime shortlist.");
            CollectionAssert.AreEqual(bytes,archive.Clips[0].Bytes);
            // Remote clients rebuild the can art when its replicated skin changes.
            // Simulate that actual source replacement, not just a rotated stat.
            var oldModel=MatchReplayArchive.PropModel(round.Lata.gameObject);oldModel.name="RetiredCanArt";oldModel.SetActive(false);
            var replacement=Object.Instantiate(RosterBook.Load().Cans.Last().Model,round.Lata.transform);replacement.name="Visual";
            Object.Destroy(oldModel);yield return null;
            using(var laterView=new RecordedWorldView(archive.transform,clip))
            {Assert.IsTrue(laterView.Ready,"Recorded art remains playable after the can stats and parked shoe rotate: "+laterView.UnavailableReason);laterView.Draw(clip.Contact,false);}
            var pose=clip.Objects.First(o=>o.Kind==RecordedObjectKind.Player&&o.Seat==1).Pose;
            var history=Object.FindAnyObjectByType<MatchPoseHistory>();
            var stage=new GameObject("RetainedRenderOnlyProof");stage.SetActive(false);
            var detachedSource=new MatchPoseHistory.Track(round.PlayerAt(1),round.PlayerAt(1).GetComponent<TumbangPreso.Visual.CharacterVisual>().Model);
            detachedSource.Record(Time.time);detachedSource.Record(Time.time+.05f);
            var copy=detachedSource.Clone(stage.transform);Assert.IsNotNull(copy);
            var bones=pose.Bind(copy.Root);Assert.IsNotNull(bones);
            int score=match.ScoreFor(1);var live=round.PlayerAt(1).transform.position;
            pose.Apply(bones,clip.Contact);Assert.AreEqual(score,match.ScoreFor(1));Assert.AreEqual(live,round.PlayerAt(1).transform.position);
            Assert.IsEmpty(stage.GetComponentsInChildren<MonoBehaviour>(true));Assert.IsEmpty(stage.GetComponentsInChildren<Collider>(true));
            Object.Destroy(stage);
            var broken=bytes.Take(bytes.Length/2).ToArray();Assert.IsFalse(RecordedMatchClip.TryDecode(broken,out _,out _));
            match.ResetForNewMatch();yield return null;yield return null;
            Assert.AreEqual(0,archive.Clips.Count,"Match reset clears the shortlist even when sampling has stopped.");
            yield return PlayModeWorld.Reset();
            Debug.Log("[ReplayRetention] retained bytes="+bytes.Length+" objects="+clip.Objects.Length+" duration="+clip.Duration);
        }
    }
}
