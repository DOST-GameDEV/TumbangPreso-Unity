using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// Who has pressed REMATCH, and whether that is everybody.
    ///
    /// ⚠️⚠️ IT IS ENGINE-FREE BECAUSE EVERY BUG THIS EVER HAD WAS A COUNTING BUG. `docs/TODO.md`
    /// § 1 is the last PARTIAL row in the ledger and its warning is that the wire half cannot be
    /// finished honestly without two real processes on a LAN. That is true of the TRANSPORT. It
    /// is not true of the rules the transport carries, and those are the parts that went wrong
    /// in the ready gate before they went wrong here: a host whose own press arrives with a
    /// sender id of 0 and therefore never satisfies its own gate, a peer whose second press is
    /// counted twice, and a peer that leaves mid-vote and strands everybody still watching.
    /// Here they are assertions that run in a millisecond from a terminal.
    ///
    /// ⚠️ IT COUNTS PEERS, NEVER SEATS. Four seats are always filled; bot-filled ones cannot
    /// press a button. `ReadyGate` learned this and `MatchResult` copies it, which is why the
    /// expected count is passed IN rather than derived here: this class must not acquire an
    /// opinion about where that number comes from.
    /// </summary>
    public sealed class RematchVote
    {
        private readonly HashSet<int> _voters = new HashSet<int>();

        public int Count => _voters.Count;

        public void Clear() => _voters.Clear();

        /// <summary>
        /// Record a vote. Returns false when it changed nothing.
        ///
        /// ⚠️⚠️ A SENDER ID OF 0 IS THE HOST, RESOLVED AT THE DOOR. `ReadyGate.DeclareReady`
        /// carries the same line and the same note: in Godot the host's own press came through
        /// with 0 rather than with its real id, and the fix has to be here rather than in a
        /// second code path, or the host can never satisfy a gate it is itself part of.
        /// </summary>
        public bool Add(int peerId, int hostPeerId)
        {
            if (peerId == 0) peerId = hostPeerId;
            return _voters.Add(peerId);
        }

        /// <summary>A peer disconnected. Returns false when it was not voting anyway.</summary>
        public bool Remove(int peerId) => _voters.Remove(peerId);

        public bool HasVoted(int peerId, int hostPeerId)
            => _voters.Contains(peerId == 0 ? hostPeerId : peerId);

        /// <summary>
        /// Is the gate open?
        ///
        /// ⚠️ ZERO VOTES NEVER SATISFIES IT, however small `expected` gets. A lobby that empties
        /// out drives `expected` toward zero, and `Count >= expected` alone would then start a
        /// rematch that literally nobody asked for.
        /// </summary>
        public bool Satisfied(int expected) => _voters.Count > 0 && _voters.Count >= expected;
    }
}
