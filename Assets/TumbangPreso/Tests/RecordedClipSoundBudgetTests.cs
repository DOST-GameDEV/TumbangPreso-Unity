using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class RecordedClipSoundBudgetTests
    {
        private static RecordedMatchClip Clip(int count)
        {
            RecordedPoseTrack.Sample Pose(float time) => new()
            {
                Time = time, Epoch = 1, Holder = -1,
                Positions = new[] { Vector3.zero }, Rotations = new[] { Quaternion.identity },
                Scales = new[] { Vector3.one }, Active = new[] { true }
            };
            var sounds = new RecordedWorldCue[count];
            for (int i = 0; i < count; i++) sounds[i] = new RecordedWorldCue
            {
                Time = count > 1 ? i / (float)(count - 1) : .5f,
                Id = "slipper.throw", Position = Vector3.one, Pitch = 1, Gain = .5f
            };
            return new RecordedMatchClip
            {
                MatchId = 1, Id = 1, Round = 1, Actor = 0, Subject = -1,
                Mode = GameMode.Classic, Map = UI.SceneFlow.Eskinita,
                Reason = "Bounded sound roundtrip", Start = 0, End = 1, Contact = .5f,
                Objects = new[] { new RecordedObjectTrack
                {
                    Kind = RecordedObjectKind.Can, Seat = -1, Skin = -1,
                    Pose = new RecordedPoseTrack(new[] { "" }, new[] { Pose(0), Pose(1) })
                } }, Sounds = sounds
            };
        }
        [TestCase(0)] [TestCase(1)] [TestCase(256)] [TestCase(257)] [TestCase(512)]
        public void EncoderAndDecoderAgreeWithinTheExistingRecorderSoundBudget(int count)
        {
            var clip = Clip(count); byte[] bytes = clip.Encode();
            Assert.Greater(bytes.Length, 8); Assert.Less(bytes.Length, RecordedMatchClip.ByteLimit);
            Assert.IsTrue(RecordedMatchClip.TryDecode(bytes, out var decoded, out var reason), reason);
            Assert.AreEqual(count, decoded.Sounds.Length);
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(clip.Sounds[i].Time, decoded.Sounds[i].Time);
                Assert.AreEqual(clip.Sounds[i].Id, decoded.Sounds[i].Id);
                Assert.AreEqual(clip.Sounds[i].Pitch, decoded.Sounds[i].Pitch);
                Assert.AreEqual(clip.Sounds[i].Gain, decoded.Sounds[i].Gain);
                Assert.AreEqual(clip.Sounds[i].Position, decoded.Sounds[i].Position);
            }
        }
        [Test] public void AClipBeyondTheExistingRecorderBudgetStillRefusesCleanly()
        {
            Assert.Throws<System.IO.InvalidDataException>(() => Clip(513).Encode());
        }
    }
}
