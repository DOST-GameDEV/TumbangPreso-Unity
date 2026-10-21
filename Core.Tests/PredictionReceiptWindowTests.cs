using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    public sealed class PredictionReceiptWindowTests
    {
        [Fact]
        public void OldOrDuplicateRefusalsCannotUndoANewerActionOrAnotherChannel()
        {
            var receipts = new PredictionReceiptWindow(4);
            long old = receipts.Begin(0), other = receipts.Begin(1), current = receipts.Begin(0);
            Assert.False(receipts.TryDeny(0, old));
            Assert.False(receipts.TryDeny(1, current));
            Assert.True(receipts.TryDeny(0, current));
            Assert.False(receipts.TryDeny(0, current));
            Assert.True(receipts.TryDeny(1, other));
            Assert.False(receipts.TryDeny(-1, current));
            Assert.False(receipts.TryDeny(4, current));
            Assert.False(receipts.TryDeny(2, 0));
        }

        [Fact]
        public void ResetRetiresTheScopeWithoutReusingAnOldRequestIdentity()
        {
            var receipts = new PredictionReceiptWindow(4);
            long old = receipts.Begin(3);
            receipts.Reset();
            Assert.False(receipts.TryDeny(3, old));
            long current = receipts.Begin(3);
            Assert.True(current > old);
            Assert.False(receipts.TryDeny(3, old));
            Assert.True(receipts.TryDeny(3, current));
        }
    }
}
