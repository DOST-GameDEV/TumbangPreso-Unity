using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class RecordedCompanionTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        private static RecordedMatchClip SmallClip(int seat, int holder)
        {
            RecordedPoseTrack.Sample Pose(float time) => new RecordedPoseTrack.Sample
            { Time=time,Holder=holder,Positions=new[]{Vector3.zero},Rotations=new[]{Quaternion.identity},
                Scales=new[]{Vector3.one},Active=new[]{true} };
            return new RecordedMatchClip {MatchId=1,Id=1,Round=1,Actor=0,Subject=-1,
                Mode=GameMode.HeroStrike,Map="Eskinita",Reason="Seat bounds",Start=0,End=.5f,Contact=.25f,
                Objects=new[]{new RecordedObjectTrack{Kind=RecordedObjectKind.Slipper,Seat=seat,Skin=0,
                    Pose=new RecordedPoseTrack(new[]{""},new[]{Pose(0),Pose(.5f)})}}};
        }

        private static byte[] WithVersion(byte[] bytes, int version)
        {
            using var raw=new MemoryStream();
            using(var packed=new MemoryStream(bytes))
            using(var inflate=new DeflateStream(packed,CompressionMode.Decompress))inflate.CopyTo(raw);
            raw.Position=4;
            using(var writer=new BinaryWriter(raw,Encoding.UTF8,true))writer.Write(version);
            raw.Position=0;using var output=new MemoryStream();
            using(var deflate=new DeflateStream(output,System.IO.Compression.CompressionLevel.Fastest,true))raw.CopyTo(deflate);
            return output.ToArray();
        }

        [Test]
        public void Version14PlayerClipsRemainReadableAndRejectCompanionSeats()
        {
            var old=WithVersion(SmallClip(1,1).Encode(),14);
            Assert.IsTrue(RecordedMatchClip.TryDecode(old,out var clip,out var reason),reason);
            Assert.AreEqual(1,clip.Objects[0].Pose.Samples[0].Holder);
            var incompatible=WithVersion(SmallClip(5,5).Encode(),14);
            Assert.IsFalse(RecordedMatchClip.TryDecode(incompatible,out _,out _));
        }

        [Test]
        public void CurrentCompanionSeatsAndHoldersRemainBounded()
        {
            Assert.DoesNotThrow(()=>SmallClip(7,7).Encode());
            Assert.Throws<InvalidDataException>(()=>SmallClip(8,7).Encode());
            Assert.Throws<InvalidDataException>(()=>SmallClip(7,8).Encode());
        }

        [UnityTest]
        public IEnumerator ARealSummonedDollAndItsHeldSlipperRecordAndPlayWithoutGameplayCopies()
        {
            yield return MapRetrievalProbe.Load("Eskinita", GameMode.HeroStrike);
            Object.FindAnyObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            foreach (var actor in GameServices.Round.Players)
            { actor.Intent.Clear(); actor.Intent.Parked = true; }
            var owner = GameServices.Round.PlayerAt(1);
            var doll = VoodooDollBody.Spawn(owner, owner.transform.position + Vector3.right * 1.3f, 0, false);
            Assert.IsNotNull(doll);
            doll.Intent.Parked = true;
            int seat = CompanionSeats.For(owner.PlayerSlot);
            var shoe = doll.GetComponent<VoodooDollBody>().Shoe;
            Assert.AreEqual(seat, shoe.SeatOfOrigin);
            Assert.AreSame(doll, shoe.Holder);
            yield return new WaitForSeconds(3);
            var archive = Object.FindAnyObjectByType<MatchReplayArchive>();
            var history = archive.GetComponent<MatchPoseHistory>();
            float end = history.ForSeat(0).Newest;
            Assert.IsTrue(archive.TryCaptureSession(end - 1, end, 1, out var clip, out var reason), reason);
            byte[] bytes = null;
            Assert.DoesNotThrow(() => bytes = clip.EncodeLocal(), "The actual companion slipper must be a valid recorded object.");
            Assert.IsTrue(RecordedMatchClip.TryDecodeLocal(bytes, out var saved, out var error), error);
            var bodyTrack = saved.Objects.SingleOrDefault(t => t.Kind.ToString() == "Companion" && t.Seat == seat);
            Assert.IsNotNull(bodyTrack, "The doll must be recorded with its own body, not only its floating slipper.");
            var shoeTrack = saved.Objects.Single(t => t.Kind == RecordedObjectKind.Slipper && t.Seat == seat);
            Assert.IsTrue(shoeTrack.Pose.Samples.Any(s => s.Holder == seat));
            int actors = Object.FindObjectsByType<CharacterMotor>(FindObjectsInactive.Include).Length;
            int slippers = Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include).Length;
            var stage = new GameObject("Companion standalone recording witness");
            try
            {
                using (var view = new RecordedWorldView(stage.transform, saved, true))
                {
                    Assert.IsTrue(view.Ready, view.UnavailableReason);
                    view.Draw(saved.Start, false); view.Draw(saved.End, false);
                    Assert.AreEqual(actors, Object.FindObjectsByType<CharacterMotor>(FindObjectsInactive.Include).Length);
                    Assert.AreEqual(slippers, Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include).Length);
                    var copyRoot = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                        .First(t => t.name == "~RecordedWorld");
                    Assert.IsEmpty(copyRoot.GetComponentsInChildren<CharacterMotor>(true));
                    Assert.IsEmpty(copyRoot.GetComponentsInChildren<Carrier>(true));
                    Assert.IsEmpty(copyRoot.GetComponentsInChildren<AIController>(true));
                    Assert.IsEmpty(copyRoot.GetComponentsInChildren<VoodooDollBody>(true));
                    Assert.AreSame(shoe, doll.GetComponent<Carrier>().Held);
                    Debug.Log("[RecordedCompanion] seat=" + seat + " bytes=" + bytes.Length + " objects=" + saved.Objects.Length + " held samples=" + shoeTrack.Pose.Samples.Count(s => s.Holder == seat));
                }
            }
            finally { Object.Destroy(stage); }
        }
    }
}
