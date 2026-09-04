using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    /// <summary>
    /// The game noticing its own good moments and writing them down.
    ///
    /// ⚠️⚠️ IT CHANGES NO SCORE AND CAN NOT. `docs/VISION.md` § 4: every point is awarded in
    /// `MatchDirector.AddScore`, host-side, and nothing here reaches it. `docs/TODO.md` § 147's
    /// brief opens with the same rule and then says why: *"do not create fake hype by assigning
    /// arbitrary bonuses"*. This layer is a RECORD, and the value of a record is that it is
    /// trustworthy rather than that it is generous.
    ///
    /// ⚠️⚠️ IT IS `Hud.ReportStyle`'S ARGUMENT ONE LEVEL UP, AND IT IS NOT A REPLACEMENT FOR IT.
    /// Street Hype is Classic's bottom-of-screen identity and is deliberately cosmetic and
    /// deliberately LOCAL: it names a curve or a bank while it is happening, for the player who
    /// did it, and then it is gone. What nothing could answer afterwards is *what happened in
    /// this match and when*, which is what a replay, a spectator ticker and a post-match summary
    /// all need. `SpectatorCamera.QueueHighlight`'s own note records the shape of the gap: until
    /// the marker was stamped onto a captured frame, *"nothing in the buffer knew WHEN the tag
    /// was"*. This is that stamp, kept outside the ring so it outlives the ten seconds of pixels.
    ///
    /// ⚠️⚠️ IT RECORDS ON EVERY PEER, FROM `MatchFlair.Play`, AND THAT IS DELIBERATE RATHER THAN
    /// CONVENIENT. `MatchFlair` already replicates the EVENT to every machine and each one draws
    /// its own copy, so recording there gives every peer the same list without a single new byte
    /// on the wire. Recording host-side and broadcasting would be a new message carrying something
    /// every peer already knows. The one consequence worth stating: two peers' timestamps for the
    /// same moment differ by the latency, which is fine for a replay window and is why nothing
    /// compares these across peers.
    /// </summary>
    public static class MatchHighlights
    {
        /// <summary>The markers for the match this process is in.</summary>
        public static readonly HighlightLog Log = new HighlightLog();

        /// <summary>
        /// Seconds since the match began, on this peer.
        ///
        /// ⚠️ `Time.unscaledTime` RATHER THAN `Time.time`, for `SpectatorCamera`'s reason: the
        /// broadcast clock can be stopped or slowed by a spectator, and a marker whose timestamp
        /// moved with the pause would not line up with the replay frames, which are also stamped
        /// unscaled.
        /// </summary>
        public static float Now => _matchStart < 0.0f ? 0.0f : Time.unscaledTime - _matchStart;

        private static float _matchStart = -1.0f;

        /// <summary>
        /// A new match. ⚠️ THE LOG IS CLEARED HERE AND NOWHERE ELSE, so a marker cannot survive
        /// into a match it did not happen in, which is exactly the leak `docs/TODO.md` § 143.5
        /// found between two matches in one process.
        /// </summary>
        public static void BeginMatch()
        {
            _matchStart = Time.unscaledTime;
            Log.Clear();
            _closeCallCount = 0;
            _closeCallWindowFrom = 0.0f;
        }

        /// <summary>
        /// Records one moment.
        ///
        /// ⚠️ THE DEDUPE IS IN `HighlightLog.Add` AND NOT HERE. Every caller would otherwise have
        /// to remember it, and a knockdown reaches this layer from three different places inside
        /// one physics step.
        /// </summary>
        public static bool Note(HighlightKind kind, int actor, int subject = -1,
                                float measurement = 0.0f)
        {
            var match = GameServices.Match;

            return Log.Add(new HighlightMarker(
                kind, Now, match != null ? match.RoundNumber : 0, actor, subject, measurement,
                HighlightRules.ImportanceFor(kind, measurement)));
        }

        // -------------------------------------------------------------------
        // § THE TWO THAT NEED WATCHING RATHER THAN AN EVENT
        //
        // ⚠️⚠️ A CLOSE CALL IS THE ABSENCE OF A TAG, AND NOTHING CAN RAISE AN EVENT FOR THAT.
        // Every other kind on the list is something that HAPPENED and therefore has a call site:
        // a bank shot, a block, a knockdown, an ultimate. "Got within a lunge of the taya and was
        // not caught" is a thing that did NOT happen, so it can only be found by watching, which
        // is what `HighlightWatch` does one file over.
        //
        // ⚠️ THE STATE LIVES HERE RATHER THAN ON THE WATCHER SO IT SURVIVES THE WATCHER. A
        // spectator seat, a scene reload and a rebuilt arena all destroy MonoBehaviours mid-match;
        // an evasion run counted on one of those would restart at zero for reasons the player
        // cannot see.
        // -------------------------------------------------------------------

        private static int _closeCallCount;
        private static float _closeCallWindowFrom;

        /// <summary>
        /// An attacker who was inside the taya's reach and got out of it.
        ///
        /// ⚠️ THE RUN IS COUNTED HERE AND NOT BY THE CALLER, so `EvasionRunCount` and
        /// `EvasionWindowSeconds` are read from the rules rather than restated by whoever is
        /// sampling. A third escape inside the window is its own marker on top of the third close
        /// call, which is right: they are different claims and a director might want either.
        /// </summary>
        public static void NoteCloseCall(int attacker, int taya, float metres)
        {
            if (!Note(HighlightKind.CloseCall, attacker, taya, metres)) return;

            float now = Now;
            if (now - _closeCallWindowFrom > HighlightRules.EvasionWindowSeconds)
            {
                _closeCallWindowFrom = now;
                _closeCallCount = 0;
            }

            _closeCallCount++;
            if (_closeCallCount >= HighlightRules.EvasionRunCount)
                Note(HighlightKind.EvasionRun, attacker, taya, _closeCallCount);
        }

        /// <summary>
        /// A tsinelas collected. Decides on its own whether it was clutch, last-second, or neither.
        ///
        /// ⚠️⚠️ THE DECISION IS HERE RATHER THAN AT THE PICKUP, because `Slipper.HostGrab` runs on
        /// the host only and this layer records on every peer (see the class note). The pickup
        /// announces the fact; what KIND of moment it was is a question about the world, and every
        /// machine has the world.
        /// </summary>
        public static void NoteRetrieval(int attacker, float metresFromTaya, float secondsLeft)
        {
            if (secondsLeft >= 0.0f && secondsLeft <= HighlightRules.LastSecondSeconds)
                Note(HighlightKind.LastSecondRetrieval, attacker, -1, secondsLeft);

            if (metresFromTaya >= 0.0f && metresFromTaya <= HighlightRules.CloseCallMetres)
                Note(HighlightKind.ClutchRetrieval, attacker, -1, metresFromTaya);
        }

        /// <summary>
        /// The can went over. Decides whether it was long, last-second, or neither.
        ///
        /// ⚠️ A BANK IS NOT DECIDED HERE. `MatchFlair.Kind.BankShot` is already raised by the
        /// thing that knows a tsinelas bounced off scenery, and re-deriving it from a position
        /// would be a second answer to a question the game has already answered.
        /// </summary>
        public static void NoteKnockdown(int thrower, float metres, float secondsLeft)
        {
            if (metres >= HighlightRules.LongKnockdownMetres)
                Note(HighlightKind.LongKnockdown, thrower, -1, metres);

            if (secondsLeft >= 0.0f && secondsLeft <= HighlightRules.LastSecondSeconds)
                Note(HighlightKind.LastSecondKnockdown, thrower, -1, secondsLeft);
        }

        /// <summary>The whole log, for a bundle or a report.</summary>
        public static string Report()
        {
            var lines = Log.Report();
            if (lines.Count == 0) return "  (no highlights recorded)";

            return "  " + string.Join("\n  ", lines) +
                   $"\n  {Log.Recorded} recorded, {Log.Deduplicated} folded into an existing marker.";
        }

        /// <summary>
        /// ⚠️ EDIT-MODE AND PLAY-MODE TESTS NEED THIS, for `GameServices.ResetStatics`' reason:
        /// domain reload can be disabled in the editor, in which case a static log survives
        /// between Play sessions and the second run starts with the first one's markers in it.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _matchStart = -1.0f;
            _closeCallCount = 0;
            _closeCallWindowFrom = 0.0f;
            Log.Clear();
        }
    }
}
