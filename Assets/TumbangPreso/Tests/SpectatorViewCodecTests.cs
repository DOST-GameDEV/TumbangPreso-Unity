using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class SpectatorViewCodecTests
    {
        private static SpectatorViewState Valid() => new() {
            Match = 12, Round = 3, Slot = 1, Epoch = 7, Sequence = 9, SentAt = 10,
            Eye = new Vector3(2, 5, -7), Rotation = Quaternion.Euler(31, 137, -9), Fov = 93, EyeDrop = .6f,
            HandsSeat = CameraSystem.CameraRig.ViewmodelSeat, HandsRotation = Quaternion.Euler(2, -3, 0),
            Action = new FixedString64Bytes("ground-call"), Aim = new FixedString64Bytes("magma-palms"),
            ActionTime = .73f, ActionSequence = 4, VisualPhase = 100, Flags = 3
        };
        [Test] public void EyeLookLensAndHandStateSurviveTheActualWriterReader()
        {
            var expected = Valid(); var writer = new FastBufferWriter(SpectatorViewState.MaxWireBytes, Allocator.Temp);
            try
            {
                expected.Write(writer); var reader = new FastBufferReader(writer, Allocator.Temp);
                try
                {
                    Assert.IsTrue(SpectatorViewState.TryRead(ref reader, out var actual));
                    Assert.AreEqual(expected.Match, actual.Match); Assert.AreEqual(expected.Epoch, actual.Epoch);
                    Assert.AreEqual(expected.Eye, actual.Eye); Assert.That(Quaternion.Angle(expected.Rotation, actual.Rotation), Is.LessThan(.001f));
                    Assert.AreEqual(expected.Fov, actual.Fov); Assert.AreEqual(expected.EyeDrop, actual.EyeDrop);
                    Assert.AreEqual(expected.Action, actual.Action); Assert.AreEqual(expected.Aim, actual.Aim);
                    Assert.AreEqual(expected.ActionTime, actual.ActionTime); Assert.AreEqual(expected.HandsSeat, actual.HandsSeat);
                    Assert.AreEqual(expected.ActionSequence, actual.ActionSequence); Assert.AreEqual(expected.Flags, actual.Flags);
                }
                finally { reader.Dispose(); }
            }
            finally { writer.Dispose(); }
        }
        [Test] public void EmptyAndLongestNamesFitTheDeclaredPacketBounds()
        {
            foreach (int count in new[] { 0, 61 })
            {
                var state = Valid(); state.Action = new FixedString64Bytes(new string('a', count)); state.Aim = new FixedString64Bytes(new string('b', count));
                var writer = new FastBufferWriter(SpectatorViewState.MaxWireBytes, Allocator.Temp);
                try { state.Write(writer); Assert.AreEqual(count == 0 ? SpectatorViewState.MinWireBytes : SpectatorViewState.MaxWireBytes, writer.Length); }
                finally { writer.Dispose(); }
            }
        }
        [Test] public void EveryTruncationAndTrailingByteRejectsWithoutAView()
        {
            var writer = new FastBufferWriter(SpectatorViewState.MaxWireBytes + 1, Allocator.Temp);
            try
            {
                Valid().Write(writer); byte[] bytes = writer.ToArray();
                for (int length = 0; length < bytes.Length; length++)
                {
                    var truncated = new byte[length]; System.Array.Copy(bytes, truncated, length);
                    var reader = new FastBufferReader(truncated, Allocator.Temp);
                    try { Assert.IsFalse(SpectatorViewState.TryRead(ref reader, out _), "Accepted prefix of " + length); }
                    finally { reader.Dispose(); }
                }
                writer.WriteValueSafe((byte)0); var extra = new FastBufferReader(writer, Allocator.Temp);
                try { Assert.IsFalse(SpectatorViewState.TryRead(ref extra, out _)); } finally { extra.Dispose(); }
            }
            finally { writer.Dispose(); }
        }
        [Test] public void NonFiniteAndOutOfRangeCameraDataRejects()
        {
            var state = Valid(); state.Eye.x = float.NaN; Assert.IsFalse(state.IsValid);
            state = Valid(); state.Fov = float.PositiveInfinity; Assert.IsFalse(state.IsValid);
            state = Valid(); state.Fov = 180; Assert.IsFalse(state.IsValid);
            state = Valid(); state.Rotation = default; Assert.IsFalse(state.IsValid);
            state = Valid(); state.EyeDrop = -1; Assert.IsFalse(state.IsValid);
            state = Valid(); state.HandsSeat = Vector3.one * 20; Assert.IsFalse(state.IsValid);
            state = Valid(); state.ActionTime = float.NaN; Assert.IsFalse(state.IsValid);
            state = Valid(); state.Action = new FixedString64Bytes("../not-a-clip"); Assert.IsFalse(state.IsValid);
        }
        [Test] public void OldAndFutureViewClocksCannotPretendToBeLive()
        { var state = Valid(); Assert.IsTrue(state.Timely(10.2f)); Assert.IsFalse(state.Timely(12)); Assert.IsFalse(state.Timely(8)); }
        [Test] public void SerialOrderRejectsDuplicatesAndAllowsWrap()
        {
            Assert.IsFalse(SpectatorViewState.Newer(9, 9)); Assert.IsFalse(SpectatorViewState.Newer(8, 9));
            Assert.IsTrue(SpectatorViewState.Newer(10, 9)); Assert.IsTrue(SpectatorViewState.Newer(1, uint.MaxValue));
            Assert.IsFalse(SpectatorViewState.Newer(0, uint.MaxValue));
        }
    }
}
