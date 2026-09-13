using System.Threading;

namespace TumbangPreso.Net
{
    /// <summary>Per-attempt ownership across asynchronous joins. Contains no transport or UI behavior.</summary>
    public sealed class JoinAttemptGate
    {
        private int _version;
        public Attempt Begin(CancellationToken cancellation = default)
            => cancellation.IsCancellationRequested ? default : new Attempt(this, Interlocked.Increment(ref _version), cancellation);
        public void Invalidate() => Interlocked.Increment(ref _version);
        public readonly struct Attempt
        {
            private readonly JoinAttemptGate _owner;
            private readonly int _version;
            private readonly CancellationToken _cancellation;
            internal Attempt(JoinAttemptGate owner, int version, CancellationToken cancellation)
            { _owner = owner; _version = version; _cancellation = cancellation; }
            public bool OwnsSession => _owner != null && Volatile.Read(ref _owner._version) == _version;
            public bool CanContinue => OwnsSession && !_cancellation.IsCancellationRequested;
        }
    }
}
