using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class IceSnapshotBatchTests
    {
        [Test]
        public void AimTokensSeparateHoldsAndMovementEpochsWithoutExposingTargets()
        {
            long first = AbilityAimSnapshot.MakeToken(3, 1);
            long next = AbilityAimSnapshot.MakeToken(3, 2);
            long handover = AbilityAimSnapshot.MakeToken(4, 1);
            Assert.Greater(next, first); Assert.Greater(handover, next);
            Assert.IsTrue(AbilityAimSnapshot.MatchesEpoch(first, 3));
            Assert.IsFalse(AbilityAimSnapshot.MatchesEpoch(first, 4));
            Assert.IsFalse(AbilityAimSnapshot.MatchesEpoch(0, 0));
            Assert.IsTrue(default(AbilityAimSnapshot).IsValid);
            var invalid = new AbilityAimSnapshot { Slot = 4 };
            Assert.IsFalse(invalid.IsValid);
            invalid = new AbilityAimSnapshot { Held = float.NaN };
            Assert.IsFalse(invalid.IsValid);
            foreach (var field in typeof(AbilityAimSnapshot).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                Assert.AreNotEqual(typeof(Vector3), field.FieldType, "Private target points must not enter body-aim replication.");
        }

        [Test]
        public void AimTailRoundTripsItsExactShapeAndClosesAtTheEndOfAPose()
        {
            var original = new AbilityAimSnapshot
            { Slot = 2, AbilityId = new FixedString64Bytes("phaister_skill2"), Held = .75f, Token = AbilityAimSnapshot.MakeToken(3, 4) };
            using var writer = new FastBufferWriter(AbilityAimSnapshot.MaxWireBytes, Allocator.Temp);
            writer.WriteNetworkSerializable(original);
            Assert.AreEqual(15 + original.AbilityId.Length, writer.Length);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            var input = reader;
            Assert.IsTrue(AbilityAimSnapshot.TryRead(ref input, out var restored));
            foreach (var field in typeof(AbilityAimSnapshot).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                Assert.AreEqual(field.GetValue(original), field.GetValue(restored), field.Name);
        }

        [Test]
        public void WorldRecoveryCannotCrossAnUltimateHandbackOrNewerCohort()
        {
            var header = new WorldSnapshotHeader { UltimatePhase = 5, UltimateStage = 1 };
            Assert.IsTrue(header.IncludesUltimateState(4, 2));
            Assert.IsTrue(header.IncludesUltimateState(5, 1));
            Assert.IsFalse(header.IncludesUltimateState(5, 2), "A preparation snapshot erased handback effects.");
            Assert.IsFalse(header.IncludesUltimateState(6, 0));
            header.UltimateStage = 2;
            Assert.IsTrue(header.IncludesUltimateState(5, 2));
            Assert.IsFalse(header.IncludesUltimateState(5, 3));
        }

        [Test]
        public void PreparedWorldRecoveryAgesPreparationThenLifeUsingOnlySimulationTime()
        {
            var snapshot = new PreparedWorldSnapshot { RoundClock = 50, Preparation = 2, Remaining = 5, Centre = Vector3.up * 4 };
            Assert.IsTrue(snapshot.TryAge(50, 3, 5, out float prep, out float live));
            Assert.AreEqual(2, prep); Assert.AreEqual(5, live);
            Assert.IsTrue(snapshot.TryAge(49, 3, 5, out prep, out live));
            Assert.AreEqual(1, prep); Assert.AreEqual(5, live);
            Assert.IsTrue(snapshot.TryAge(47, 3, 5, out prep, out live));
            Assert.AreEqual(0, prep); Assert.AreEqual(4, live);
            Assert.IsTrue(snapshot.TryAge(40, 3, 5, out prep, out live));
            Assert.AreEqual(0, prep); Assert.AreEqual(0, live);
            Assert.IsFalse(snapshot.TryAge(float.NaN, 3, 5, out _, out _));
            snapshot.Preparation = 4;
            Assert.IsFalse(snapshot.TryAge(50, 3, 5, out _, out _));
            snapshot.Preparation = 0; snapshot.Remaining = 0;
            Assert.IsTrue(snapshot.TryAge(50, 3, 5, out prep, out live));
            Assert.AreEqual(0, prep); Assert.AreEqual(0, live);
        }

        [Test]
        public void PreparedWorldSnapshotRoundTripsEveryFieldAndKeepsItsExactBound()
        {
            var original = new PreparedWorldSnapshot
            {
                Seat = 2, Round = 3, Generation = 4, Match = 567,
                AbilityId = new FixedString64Bytes("phaister_ultimate"),
                Centre = new Vector3(1, 4, -7), Preparation = .5f, Remaining = 5, RoundClock = 33
            };
            using var writer = new FastBufferWriter(PreparedWorldSnapshot.MaxWireBytes, Allocator.Temp);
            writer.WriteNetworkSerializable(original);
            Assert.AreEqual(46 + original.AbilityId.Length, writer.Length);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            var input = reader;
            Assert.IsTrue(PreparedWorldSnapshot.TryRead(ref input, out var restored));
            foreach (var field in typeof(PreparedWorldSnapshot).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                Assert.AreEqual(field.GetValue(original), field.GetValue(restored), field.Name);
        }

        [Test]
        public void SkillCastCodecRoundTripsEveryFieldIncludingAirAimAndCommandIdentity()
        {
            object boxed = new SkillCastMessage();
            var fields = typeof(SkillCastMessage).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            for (int i = 0; i < fields.Length; i++)
            {
                object value;
                if (fields[i].FieldType == typeof(int)) value = 7 + i;
                else if (fields[i].FieldType == typeof(long)) value = 1234567L + i;
                else if (fields[i].FieldType == typeof(float)) value = 2.25f + i;
                else if (fields[i].FieldType == typeof(bool)) value = true;
                else if (fields[i].FieldType == typeof(Vector3)) value = new Vector3(i + 1, i + 2, i + 3);
                else if (fields[i].FieldType == typeof(FixedString64Bytes)) value = new FixedString64Bytes("phaister_skill2d");
                else { Assert.Fail("Add a nondefault sample for " + fields[i].Name); return; }
                fields[i].SetValue(boxed, value);
            }
            var original = (SkillCastMessage)boxed;
            using var writer = new FastBufferWriter(SkillCastMessage.MaxWireBytes, Allocator.Temp);
            writer.WriteNetworkSerializable(original);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            var input = reader;
            Assert.IsTrue(SkillCastMessage.TryRead(ref input, out var restored));
            foreach (var field in fields)
                Assert.AreEqual(field.GetValue(original), field.GetValue(restored), field.Name);
        }

        [Test]
        public void SkillCastCodecRejectsMalformedIdentityTrailingDataAndInvalidFields()
        {
            var cast = new SkillCastMessage
            {
                Seat = 1, Slot = 1, AbilityId = new FixedString64Bytes("paete_skill2"), Match = 123, Round = 1,
                Event = 2, Request = 1, Forward = Vector3.forward, AimPoint = new Vector3(4, 3, 5)
            };
            Assert.IsTrue(cast.IsValid(true));
            Assert.IsFalse(cast.IsValid(false));
            cast.Event = 0;
            Assert.IsTrue(cast.IsValid(false));
            cast.AimPoint = new Vector3(0, float.NaN, 0);
            Assert.IsFalse(cast.IsValid(false));
            cast.AimPoint = Vector3.up;
            using (var writer = new FastBufferWriter(SkillCastMessage.MaxWireBytes, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(cast);
                writer.WriteValueSafe((byte)1);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                var input = reader;
                Assert.IsFalse(SkillCastMessage.TryRead(ref input, out _));
            }
            using (var writer = new FastBufferWriter(128, Allocator.Temp))
            {
                writer.WriteValueSafe(1); writer.WriteValueSafe(1); writer.WriteValueSafe(ushort.MaxValue);
                writer.WriteBytesSafe(new byte[98]);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                var input = reader;
                Assert.IsFalse(SkillCastMessage.TryRead(ref input, out _));
            }
            using (var writer = new FastBufferWriter(4, Allocator.Temp))
            {
                writer.WriteValueSafe(1);
                using var reader = new FastBufferReader(writer, Allocator.Temp);
                var input = reader;
                Assert.IsFalse(SkillCastMessage.TryRead(ref input, out _));
            }
        }

        [Test]
        public void WorldSnapshotHeaderRoundTripsEveryDeclaredField()
        {
            object boxed = new WorldSnapshotHeader();
            var fields = typeof(WorldSnapshotHeader).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            for (int i = 0; i < fields.Length; i++)
            {
                object value;
                if (fields[i].FieldType == typeof(int)) value = 7 + i;
                else if (fields[i].FieldType == typeof(long)) value = 1234567L + i;
                else if (fields[i].FieldType == typeof(float)) value = 40.25f + i;
                else if (fields[i].FieldType == typeof(FixedString128Bytes)) value = new FixedString128Bytes("snapshot-test-scene");
                else { Assert.Fail("Add a nondefault sample for " + fields[i].Name); return; }
                fields[i].SetValue(boxed, value);
            }
            var original = (WorldSnapshotHeader)boxed;
            using var writer = new FastBufferWriter(WorldSnapshotHeader.MaxWireBytes, Allocator.Temp);
            writer.WriteNetworkSerializable(original);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            var input = reader;
            Assert.IsTrue(WorldSnapshotHeader.TryRead(ref input, out var restored));
            foreach (var field in fields)
                Assert.AreEqual(field.GetValue(original), field.GetValue(restored), field.Name + " was omitted from the common serializer.");
        }

        [Test]
        public void WorldSnapshotClockAndIdentityCannotCrossMatchOrPauseBoundaries()
        {
            var header = new WorldSnapshotHeader
            { Generation = 1, Match = 17, Round = 2, Scene = new FixedString128Bytes("court"), RoundClock = 50 };
            Assert.IsTrue(header.Matches(17, 2, "court"));
            Assert.IsFalse(header.Matches(18, 2, "court"));
            Assert.IsFalse(header.Matches(17, 3, "court"));
            Assert.IsFalse(header.Matches(17, 2, "another"));
            header.SentAt = 1000;
            Assert.IsTrue(header.TryAge(50, out float age));
            Assert.Zero(age, "Wall time must not age effects while the round clock is held.");
            Assert.IsTrue(header.TryAge(47.5f, out age));
            Assert.AreEqual(2.5f, age);
            Assert.IsFalse(header.TryAge(float.NaN, out _));
            header.OwnerRequest = -1;
            Assert.IsFalse(header.Matches(17, 2, "court"));
        }

        [Test]
        public void WorldSnapshotHeaderRejectsTruncationAndOversizedSceneStorage()
        {
            using (var shortWriter = new FastBufferWriter(8, Allocator.Temp))
            {
                shortWriter.WriteValueSafe(1);
                using var shortReader = new FastBufferReader(shortWriter, Allocator.Temp);
                var input = shortReader;
                Assert.IsFalse(WorldSnapshotHeader.TryRead(ref input, out _));
            }
            using var writer = new FastBufferWriter(128, Allocator.Temp);
            writer.WriteValueSafe(1); writer.WriteValueSafe(2); writer.WriteValueSafe(ushort.MaxValue);
            writer.WriteBytesSafe(new byte[64]);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            var malformed = reader;
            Assert.IsFalse(WorldSnapshotHeader.TryRead(ref malformed, out _));
        }

        private static WorldEffectSnapshot.Field Sheet()=>new WorldEffectSnapshot.Field{
            Type=WorldEffectSnapshot.Kind.Sheet,Position=Vector3.zero,Forward=Vector3.forward,
            Duration=5,Remaining=2,Radius=2.3f,Owner=1,FirstScale=.55f,SecondScale=1};

        [Test]
        public void IncompleteDuplicateAndOldItemsCannotCompleteABatch()
        {
            var batch=new WorldEffectSnapshot.Batch(4,2);
            Assert.IsFalse(batch.Add(3,0,Sheet()));
            Assert.IsTrue(batch.Add(4,0,Sheet()));
            Assert.IsFalse(batch.Add(4,0,Sheet()));
            Assert.IsFalse(batch.Finish(4,out _));
            Assert.IsFalse(batch.Add(4,2,Sheet()));
            Assert.IsTrue(batch.Add(4,1,Sheet()));
            Assert.IsTrue(batch.Finish(4,out var fields));Assert.AreEqual(2,fields.Length);
            Assert.IsFalse(batch.Finish(4,out _));Assert.IsFalse(batch.Add(4,1,Sheet()));
        }

        [Test]
        public void MalformedFieldsCannotReplaceAValidSnapshot()
        {
            var field=Sheet();Assert.IsTrue(WorldEffectSnapshot.Valid(field));
            field.Remaining=float.PositiveInfinity;Assert.IsFalse(WorldEffectSnapshot.Valid(field));
            field=Sheet();field.Owner=999;Assert.IsFalse(WorldEffectSnapshot.Valid(field));
            field=Sheet();field.Radius=-1;Assert.IsFalse(WorldEffectSnapshot.Valid(field));
            field=Sheet();field.Type=(WorldEffectSnapshot.Kind)17;Assert.IsFalse(WorldEffectSnapshot.Valid(field));
            var batch=new WorldEffectSnapshot.Batch(1,1);Assert.IsFalse(batch.Add(1,0,field));
            Assert.IsFalse(batch.Finish(1,out _));
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>new WorldEffectSnapshot.Batch(1,WorldEffectSnapshot.MaxFields+1));
        }

        [Test]
        public void ACompleteEmptySnapshotIsValidButCanOnlyApplyOnce()
        {
            var batch=new WorldEffectSnapshot.Batch(8,0);
            Assert.IsFalse(batch.Finish(7,out _));
            Assert.IsTrue(batch.Finish(8,out var fields));Assert.IsEmpty(fields);
            Assert.IsFalse(batch.Finish(8,out _));
        }

        [Test]
        public void AdditionalFieldKindsRejectInvalidDirectionStrengthAndPillarSide()
        {
            foreach(var kind in new[]{WorldEffectSnapshot.Kind.Fire,WorldEffectSnapshot.Kind.Shock,
                WorldEffectSnapshot.Kind.Crater,WorldEffectSnapshot.Kind.Hex,WorldEffectSnapshot.Kind.Fissure})
            {
                var field=Sheet();field.Type=kind;field.FirstScale=1;
                Assert.True(WorldEffectSnapshot.Valid(field),kind.ToString());
                if(kind==WorldEffectSnapshot.Kind.Fissure)
                {
                    field.FirstScale=0;Assert.False(WorldEffectSnapshot.Valid(field));
                    field.FirstScale=-1;Assert.True(WorldEffectSnapshot.Valid(field));
                }
                else
                {
                    field.Radius=0;Assert.False(WorldEffectSnapshot.Valid(field));field.Radius=1;
                    if(kind==WorldEffectSnapshot.Kind.Hex || kind==WorldEffectSnapshot.Kind.Shock)
                    {field.FirstScale=0;Assert.False(WorldEffectSnapshot.Valid(field));field.FirstScale=1;}
                }
                if(kind==WorldEffectSnapshot.Kind.Fire || kind==WorldEffectSnapshot.Kind.Shock || kind==WorldEffectSnapshot.Kind.Fissure)
                {field.Forward=Vector3.zero;Assert.False(WorldEffectSnapshot.Valid(field));}
            }
        }
    }
}
