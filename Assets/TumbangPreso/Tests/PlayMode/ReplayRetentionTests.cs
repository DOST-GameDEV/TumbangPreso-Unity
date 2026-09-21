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
            var copy=history.ForSeat(1).Clone(stage.transform);Assert.IsNotNull(copy);
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
