using NUnit.Framework;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class IceSnapshotBatchTests
    {
        private static IceWorldSnapshot.Field Sheet()=>new IceWorldSnapshot.Field{
            Type=IceWorldSnapshot.Kind.Sheet,Position=Vector3.zero,Forward=Vector3.forward,
            Duration=5,Remaining=2,Radius=2.3f,Owner=1,FirstScale=.55f,SecondScale=1};

        [Test]
        public void IncompleteDuplicateAndOldItemsCannotCompleteABatch()
        {
            var batch=new IceWorldSnapshot.Batch(4,2);
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
            var field=Sheet();Assert.IsTrue(IceWorldSnapshot.Valid(field));
            field.Remaining=float.PositiveInfinity;Assert.IsFalse(IceWorldSnapshot.Valid(field));
            field=Sheet();field.Owner=999;Assert.IsFalse(IceWorldSnapshot.Valid(field));
            field=Sheet();field.Radius=-1;Assert.IsFalse(IceWorldSnapshot.Valid(field));
            field=Sheet();field.Type=(IceWorldSnapshot.Kind)17;Assert.IsFalse(IceWorldSnapshot.Valid(field));
            var batch=new IceWorldSnapshot.Batch(1,1);Assert.IsFalse(batch.Add(1,0,field));
            Assert.IsFalse(batch.Finish(1,out _));
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>new IceWorldSnapshot.Batch(1,IceWorldSnapshot.MaxFields+1));
        }

        [Test]
        public void ACompleteEmptySnapshotIsValidButCanOnlyApplyOnce()
        {
            var batch=new IceWorldSnapshot.Batch(8,0);
            Assert.IsFalse(batch.Finish(7,out _));
            Assert.IsTrue(batch.Finish(8,out var fields));Assert.IsEmpty(fields);
            Assert.IsFalse(batch.Finish(8,out _));
        }
    }
}
