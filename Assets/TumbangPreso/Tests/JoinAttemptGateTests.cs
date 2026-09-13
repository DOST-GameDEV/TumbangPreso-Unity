using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Net;

namespace TumbangPreso.Tests
{
    public sealed class JoinAttemptGateTests
    {
        [Test]
        public async Task DelayedCancelledAttemptCannotReplaceOrCleanUpNewerSession()
        {
            var gate = new JoinAttemptGate();
            using var cancel = new CancellationTokenSource();
            var first = gate.Begin(cancel.Token);
            var delayed = new TaskCompletionSource<bool>();
            string transport = "";
            async Task FinishOld()
            {
                await delayed.Task;
                if (first.CanContinue) transport = "old";
                else if (first.OwnsSession) transport = "cancelled";
            }
            var old = FinishOld(); cancel.Cancel();
            var current = gate.Begin();
            Assert.IsTrue(current.CanContinue); transport = "new";
            delayed.SetResult(true); await old;
            Assert.AreEqual("new", transport);
            Assert.IsFalse(first.CanContinue); Assert.IsFalse(first.OwnsSession);
        }
        [Test]
        public void AlreadyCancelledRequestDoesNotSupersedeTheCurrentAttempt()
        {
            var gate = new JoinAttemptGate(); var current = gate.Begin();
            using var cancel = new CancellationTokenSource(); cancel.Cancel();
            Assert.IsFalse(gate.Begin(cancel.Token).CanContinue);
            Assert.IsTrue(current.CanContinue);
        }
        [Test]
        public void ExplicitStopInvalidatesPendingContinuationWithoutCancellingTheNextAttempt()
        {
            var gate = new JoinAttemptGate(); var old = gate.Begin();
            gate.Invalidate(); Assert.IsFalse(old.CanContinue);
            var current = gate.Begin(); Assert.IsTrue(current.CanContinue); Assert.IsFalse(old.OwnsSession);
        }
    }
}
