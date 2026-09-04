using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso
{
    /// <summary>
    /// The single failure path a match takes when the peer holding it loses the session, and the
    /// latch that stops that peer inheriting authority over the match it was a client in.
    ///
    /// ⚠️⚠️ THE DEFECT THIS CLOSES IS ONE EXPRESSION. `NetSession.IsHost` is
    /// <c>_nm == null || !_nm.IsListening || _nm.IsServer</c>, and every clause of it is right for
    /// the case it was written for: no transport is the offline game, and a server is a host. The
    /// state it does NOT describe is a CLIENT whose transport has just stopped, and that peer
    /// satisfies the middle clause. So the moment a host disappears, every client in the room
    /// answers `IsHost == true`, and `NetAuthority.ShouldResolve()` is exactly `IsHost`.
    /// **Four peers that were obeying one referee become four referees, in the same arena, each
    /// awarding its own points.** `MatchRpc.HandleClientDisconnected` already carries a
    /// capitalised note about this expression from the other direction: adding an `IsHost` guard
    /// there broke the handler because "it answers TRUE the moment the transport stops listening,
    /// which is precisely the state a peer is in while it is being disconnected."
    ///
    /// ⚠️⚠️ IT REVOKES DECIDING, NOT DRAWING, AND THE DIFFERENCE IS THE WHOLE DESIGN.
    /// `docs/TODO.md` § 143.9: host migration is deliberately unsupported and that is not the
    /// problem; the requirement is that the failure is ONE outcome on every peer. A peer that
    /// stops resolving cannot award a point, cannot advance a round and cannot move a taya, so
    /// two peers that have both lost the host cannot end up describing different matches. They
    /// both stop, at the state they last agreed on, and they both say the same thing about why.
    ///
    /// ⚠️ NOTHING HERE DECIDES POLICY. Whether a bracket match is replayed, resumed or forfeited
    /// is a ruling and lives in `Attention.md` (§ 143.9 says so in as many words). This is the
    /// software behaviour under whatever that ruling turns out to be: the match stops cleanly,
    /// the reason is recorded, and the player reaches a screen they can act from.
    ///
    /// ⚠️⚠️ IT CLEARS ITSELF ON A SCENE CHANGE AND THAT IS DELIBERATE RATHER THAN TIDY. The
    /// alternative is a `Clear()` that every exit path has to remember, and `CLAUDE.md` § 4a is
    /// blunt about what happens to rules somebody has to remember: three of them went stale and
    /// each one cost a shipped defect. A latch that outlived its match would take the SOLO game
    /// down with it, `ShouldResolve()` is what runs single player, so the disarm has to be
    /// something that cannot be forgotten. Leaving the arena is that signal, it is observable, and
    /// `MatchRpc.HandleClientDisconnected` is already the thing that causes it.
    /// </summary>
    public static class MatchAbandon
    {
        /// <summary>Why the last session ended, or <see cref="SessionEndCause.None"/>.</summary>
        public static SessionEndCause Cause { get; private set; } = SessionEndCause.None;

        /// <summary>The raw reason, kept for a bundle. ⚠️ NEVER SHOWN: it is host-authored text.</summary>
        public static string RawReason { get; private set; } = "";

        /// <summary>The round the match was on when it stopped, for the diagnostic line.</summary>
        public static int RoundNumber { get; private set; }

        /// <summary>How many rounds it was meant to run.</summary>
        public static int TotalRounds { get; private set; }

        /// <summary>
        /// True while this peer must not resolve anything.
        ///
        /// ⚠️ IT IS A SEPARATE FIELD FROM `Cause` RATHER THAN DERIVED FROM IT, because they stop
        /// being true at different times: the reason is worth printing on the screen the player
        /// lands on, and the revocation must end the instant they are out of the arena or the
        /// next solo match cannot resolve its own tags.
        /// </summary>
        public static bool AuthorityRevoked { get; private set; }

        /// <summary>The player-facing line, or "" when there is nothing worth saying.</summary>
        public static string PlayerLine => SessionEndRules.PlayerLine(Cause);

        /// <summary>The operator's line: what happened AND what it did to the match.</summary>
        public static string Diagnostic =>
            SessionEndRules.Diagnostic(Cause, RoundNumber, TotalRounds);

        /// <summary>
        /// Record that this peer's session ended, and revoke its authority if that is what the
        /// cause means.
        ///
        /// ⚠️ THE ROUND IS READ HERE AND NOT LATER. By the time anything prints the diagnostic the
        /// arena has usually been torn down, and "abandoned at round 0 of 0" is the same
        /// non-answer the cold start's own report gave in § 143.15.
        /// </summary>
        public static void Note(string rawReason, bool wasLocal)
        {
            Cause = SessionEndRules.Classify(rawReason, wasLocal);
            RawReason = rawReason ?? "";
            AuthorityRevoked = SessionEndRules.RevokesAuthority(Cause);

            var match = GameServices.Match;
            RoundNumber = match != null ? match.RoundNumber : 0;
            TotalRounds = match != null ? match.TotalRounds : 0;

            if (AuthorityRevoked)
                Debug.LogWarning("[Abandon] " + Diagnostic);
        }

        /// <summary>
        /// Give authority back. Called on a scene change and by every session start path.
        ///
        /// ⚠️ THE CAUSE SURVIVES A `Clear` AND THE REVOCATION DOES NOT. The screen the player
        /// lands on wants to print why they are there; nothing after that wants to be unable to
        /// resolve. `Forget` is the one that wipes both, and only a new session calls it.
        /// </summary>
        public static void Clear() => AuthorityRevoked = false;

        /// <summary>Wipe it entirely. A new session is not a continuation of the last one.</summary>
        public static void Forget()
        {
            Cause = SessionEndCause.None;
            RawReason = "";
            RoundNumber = 0;
            TotalRounds = 0;
            AuthorityRevoked = false;
        }

        /// <summary>
        /// ⚠️⚠️ THE DISARM IS A SUBSCRIPTION RATHER THAN A CALL SITE, for the reason in the class
        /// note: a caller that has to remember is a caller that eventually does not. Leaving the
        /// scene the abandoned match was in is the event, and it is the same event
        /// `MatchRpc.HandleClientDisconnected` already produces by navigating to the lobby.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // ⚠️ ADDITIVE LOADS DO NOT COUNT. `MapPreviewSurface` loads arenas additively and
            // caches them (`docs/TODO.md` § 126.8b); treating that as "the player left the match"
            // would hand authority back to a peer still standing in the abandoned arena.
            if (mode != LoadSceneMode.Single) return;

            Clear();
        }
    }
}
