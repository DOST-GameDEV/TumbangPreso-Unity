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
        public IEnumerator ScheduledBreakUsesOneDeadlineAndNeverRunsAfterTheFinalRound()
        {
            Assert.IsFalse(HalftimePresentation.IsMiddleBreak(2,4));
            Assert.IsTrue(HalftimePresentation.IsMiddleBreak(3,6));
            Assert.IsTrue(HalftimePresentation.IsMiddleBreak(4,8));
            Assert.IsFalse(HalftimePresentation.IsMiddleBreak(8,8));
            yield return MapRetrievalProbe.Load("Eskinita");
            var round=GameServices.Round;var match=GameServices.Match;
            round.EndRound();match.BeginIntermission();var phase=HalftimePresentation.Instance;
            Assert.IsTrue(phase.Active);Assert.IsFalse(phase.IsHalftime);Assert.AreEqual(3,phase.Duration);
            double began=SharedUltimatePhase.Now;
            while(match.RoundNumber==1&&SharedUltimatePhase.Now-began<4)yield return null;
            Assert.AreEqual(2,match.RoundNumber);Assert.That(SharedUltimatePhase.Now-began,Is.InRange(2.9,3.5));
            while(match.RoundNumber<4){round.EndRound();match.AdvanceRound();yield return null;}
            round.EndRound();match.BeginIntermission();
            Assert.IsTrue(phase.IsHalftime);Assert.IsTrue(PresentationClock.Held);Assert.IsTrue(PresentationClock.BlocksInput);
            Assert.AreEqual(10,phase.Duration);int score=match.ScoreFor(1);float time=Time.time;
            match.SkipBuffer();Assert.IsFalse(match.SkipRequested,"Halftime has one shared end");
            yield return new WaitForSecondsRealtime(.4f);
            Assert.AreEqual(time,Time.time);Assert.IsNotNull(phase.FallbackReason);Assert.AreEqual(score,match.ScoreFor(1));
            // Join the same deadline late; duplicates cannot restart its ten seconds.
            Assert.IsFalse(phase.Receive(phase.MatchId,phase.CompletedRound,phase.NextTaya,SharedUltimatePhase.Now,0,true,1));
            double end=phase.Began+10;
            while(SharedUltimatePhase.Now<end+.15)yield return null;
            Assert.AreEqual(5,match.RoundNumber);Assert.IsFalse(PresentationClock.Held);Assert.IsFalse(phase.Active);
            while(match.RoundNumber<8){round.EndRound();match.AdvanceRound();yield return null;}
            round.EndRound();match.BeginIntermission();Assert.IsFalse(phase.Active);Assert.IsFalse(match.MatchInProgress);
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
                view.Draw(clip.Contact,false);Assert.IsTrue(view.Target.IsCreated());
                var capture=new Texture2D(view.Target.width,view.Target.height,TextureFormat.RGB24,false);
                var previous=RenderTexture.active;RenderTexture.active=view.Target;capture.ReadPixels(new Rect(0,0,view.Target.width,view.Target.height),0,0);capture.Apply();RenderTexture.active=previous;
                System.IO.Directory.CreateDirectory("Logs/replay-retained-view");System.IO.File.WriteAllBytes("Logs/replay-retained-view/contact.png",capture.EncodeToPNG());Object.Destroy(capture);
            }
            round.EndRound();match.AdvanceRound();yield return new WaitForSeconds(.2f);
            Assert.AreEqual(1,archive.Clips.Count,"A round reset cannot delete the halftime shortlist.");
            CollectionAssert.AreEqual(bytes,archive.Clips[0].Bytes);
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
