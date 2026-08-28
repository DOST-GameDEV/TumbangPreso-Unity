using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// Who has pressed REMATCH, and whether that is everybody.
    ///
    /// ⚠️⚠️ IT IS ENGINE-FREE BECAUSE EVERY BUG THIS EVER HAD WAS A COUNTING BUG. The wire still
    /// needs a two-process gameplay run, but the rules it carries can be asserted in a
    /// millisecond: peer zero remains a real voter, a second press changes nothing, and a peer
    /// that leaves mid-vote cannot strand everybody still watching.
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
        /// ⚠️⚠️ NGO CLIENT ID 0 IS THE HOST'S REAL PEER ID. Do not remap it to a seat. Seats and
        /// transport peers are different namespaces, and a host in seat 1 beside client 1 would
        /// otherwise collapse two voters into one set entry and leave the gate permanently short.
        /// </summary>
        public bool Add(int peerId) => _voters.Add(peerId);

        /// <summary>A peer disconnected. Returns false when it was not voting anyway.</summary>
        public bool Remove(int peerId) => _voters.Remove(peerId);

        public bool HasVoted(int peerId) => _voters.Contains(peerId);

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
