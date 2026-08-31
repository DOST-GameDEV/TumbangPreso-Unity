using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// One peer's own count of what it saw happen, kept so it can be compared with what the host
    /// says happened.
    ///
    /// ⚠️⚠️ THIS CLASS EXISTS BECAUSE `FUTURE.md` § 8.1 ASSUMED SOMETHING THIS CODEBASE DOES NOT
    /// DO, AND THE ASSUMPTION IS THE WHOLE MECHANISM. That section says "the clients already have
    /// everything needed. Every peer derives the scoreboard from the scoring events it already
    /// receives, because that is how the HUD stays in sync." **They receive the events and they
    /// did not derive anything from them.** What actually happens at the whistle is
    /// `MatchRpc.BroadcastMatchRecord`: the host serialises its finished `MatchRecord` and every
    /// peer calls `GameServices.Stats.Adopt` on it. So all four submissions were byte-identical
    /// copies of one machine's opinion, and comparing them would have proved that JSON round-trips.
    ///
    /// **A corroboration scheme built on that would have been theatre**, and it is the kind of
    /// theatre that is worse than nothing, because a rank nobody can trust is worse than no rank
    /// (`FUTURE.md` § 9's own opening line).
    ///
    /// ⚠️⚠️ SO THE COMPARISON IS AGAINST THE EVENT STREAM, WHICH IS THE ONE THING THE HOST
    /// COMMITTED TO DURING PLAY. `MatchDirector.AddScore` is the single writer of every point in
    /// the game and it announces every one of them (`Scored`), reliably, to every peer, because
    /// three quarters of the game's feedback hangs off that announcement. A host that wants a
    /// better scoreboard has to either lie in play, where the toast, the sting and the scoreboard
    /// pulse all fire on three other machines, or lie at submission, where this catches it.
    ///
    /// ⚠️ THE HONEST LIMIT, WRITTEN DOWN RATHER THAN IMPLIED: **a modified host that awards itself
    /// points DURING the match is not caught by this.** Every peer sees the same fabricated events
    /// and tallies the same fabricated total, so all four agree. `IntegrityRules`' header says the
    /// same thing from the other end, and `FUTURE.md` § 8.2's dedicated servers are the answer to
    /// it. What this stops is the cheap attack: play a normal match, submit a better one.
    ///
    /// ⚠️⚠️ A PEER THAT DID NOT SEE THE WHOLE MATCH SUBMITS NOTHING, AND SILENCE IS NOT A
    /// DISPUTE. Backfill (`Matchmaker.OfferBackfillSeat`) puts people into a running match on
    /// purpose, and a reconnect after a dropout does the same; both miss the events that went out
    /// before they arrived, so both would tally short and accuse an honest host.
    /// <see cref="Complete"/> is the gate and `IntegrityRules.Corroborate` treats an absent digest
    /// as silence by design.
    /// </summary>
    public sealed class ScoreWitness : MonoBehaviour
    {
        private readonly int[] _scores = new int[Balance.PlayerCount];
        private MatchDirector _director;

        /// <summary>
        /// False once this peer knows it missed something, and it never becomes true again inside
        /// a match.
        /// </summary>
        public bool Complete { get; private set; }

        /// <summary>The running tally, for a probe and for the comparison.</summary>
        public int ScoreFor(int slot)
            => slot < 0 || slot >= _scores.Length ? 0 : _scores[slot];

        public static ScoreWitness Ensure(GameObject on)
        {
            var existing = on.GetComponent<ScoreWitness>();
            return existing != null ? existing : on.AddComponent<ScoreWitness>();
        }

        /// <summary>
        /// Begin counting. ⚠️ CALLED AT THE WHISTLE AND NOT AT SCENE LOAD: a match that has
        /// already started when this machine arrives is a match this machine cannot witness, and
        /// <paramref name="sawTheStart"/> is how the caller says so.
        /// </summary>
        public void Begin(MatchDirector director, bool sawTheStart)
        {
            Unhook();

            Array.Clear(_scores, 0, _scores.Length);
            Complete = sawTheStart;
            _director = director;

            if (_director != null) _director.Scored += OnScored;
        }

        /// <summary>
        /// ⚠️ A MISSED WINDOW IS DECLARED RATHER THAN GUESSED AT. Anything that could have cost
        /// this peer an event calls this: a reconnect, a backfill arrival, a transport hiccup that
        /// dropped the handler. It is one way, because a peer that has missed one point can never
        /// be sure how many.
        /// </summary>
        public void MarkIncomplete() => Complete = false;

        private void OnScored(int slot, ScoreEvent e)
        {
            if (slot < 0 || slot >= _scores.Length) return;

            // ⚠️⚠️ `MatchRules.PointsFor` IS THE SAME FUNCTION THE HOST'S `Scoreboard.Add` USES,
            // WHICH IS WHY THIS CAN BE AN EXACT COMPARISON RATHER THAN A TOLERANCE. Both sides
            // read the value out of the engine-free core, so two machines cannot disagree about
            // what a knockdown is worth without disagreeing about `Balance.cs`, and a build with a
            // different `Balance.cs` is refused at connection approval by the protocol check.
            _scores[slot] += MatchRules.PointsFor(e);
        }

        private void Unhook()
        {
            if (_director != null) _director.Scored -= OnScored;
            _director = null;
        }

        private void OnDestroy() => Unhook();

        /// <summary>
        /// The host's record with this peer's own scores and placements substituted in.
        ///
        /// ⚠️⚠️ ONLY THE CONTESTED FIELDS ARE REPLACED, AND THE REST ARE TAKEN FROM THE HOST ON
        /// PURPOSE. A witness is not a second author of the match: the match id, the map, the
        /// round count and who sat where are facts this peer has no independent measurement of and
        /// no reason to dispute. What it measured is the SCORE, so that is what it substitutes,
        /// and the placement follows from the score by `MatchRecordRules.AssignPlacements` rather
        /// than being an opinion of its own.
        ///
        /// ⚠️ THE RETURNED RECORD IS A COPY AND THE HOST'S IS NOT TOUCHED. The career submission
        /// uses the host's record and the digest uses this one; writing into the shared object
        /// would make the career report the witness's numbers, which is exactly the confusion this
        /// whole phase exists to keep apart.
        /// </summary>
        public MatchRecord AsWitnessed(MatchRecord hostRecord)
        {
            if (hostRecord == null) return null;

            var copy = JsonUtility.FromJson<MatchRecord>(JsonUtility.ToJson(hostRecord));
            if (copy?.Players == null) return null;

            foreach (var line in copy.Players)
            {
                if (line == null) continue;
                line.Score = ScoreFor(line.Slot);
            }

            MatchRecordRules.AssignPlacements(copy);

            // ⚠️ THE WINNER FOLLOWS THE PLACEMENTS RATHER THAN BEING COPIED, or a host could move
            // one field the digest reads and nothing here would notice.
            copy.WinningSlot = -1;
            foreach (var line in copy.Players)
                if (line != null && line.Placement == 1) { copy.WinningSlot = line.Slot; break; }

            return copy;
        }

        /// <summary>
        /// This peer's digest of the match, or an empty string when it cannot honestly produce one.
        /// </summary>
        public string Digest(MatchRecord hostRecord)
        {
            if (!Complete) return "";

            var witnessed = AsWitnessed(hostRecord);
            return witnessed == null ? "" : IntegrityRules.Digest(witnessed);
        }
    }
}
