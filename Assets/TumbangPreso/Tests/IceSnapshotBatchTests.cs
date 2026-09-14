using NUnit.Framework;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class IceSnapshotBatchTests
    {
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
