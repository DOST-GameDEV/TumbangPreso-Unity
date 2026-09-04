namespace TumbangPreso.Core
{
    /// <summary>
    /// Why a networked session ended, as one of a small closed set.
    ///
    /// ⚠️⚠️ A SENTENCE IS NOT A CAUSE. `NetSession.PlayerFacingDisconnectReason` already turns the
    /// transport's envelope into words a player can read, and `ClassifyDisconnect` already reduces
    /// it to a telemetry bucket, and neither of those is the thing the GAME has to branch on.
    /// `docs/TODO.md` § 143.9's requirement is that host loss "reaches ONE explicit failure path
    /// with a stated reason rather than pretending the match ended normally", and a free-text
    /// string cannot be the input to that decision: it is host-authored, it varies by transport
    /// version, and every reader would have to re-derive its own meaning from it.
    ///
    /// ⚠️ THREE READERS, ONE ANSWER. The telemetry bucket, the player-facing line and the
    /// authority latch all ask the same question, and until this type they each answered it their
    /// own way off the same string. `docs/TODO.md` § 94.1 is the general form of that fault: four
    /// copies of "which line is mine" all agreed on the wrong value because each was free to
    /// derive it again.
    /// </summary>
    public enum SessionEndCause
    {
        /// <summary>Nothing has ended. The resting value.</summary>
        None = 0,

        /// <summary>This machine chose to leave: quit to menu, back out of a lobby, close.</summary>
        LocalQuit = 1,

        /// <summary>
        /// The host stopped answering, or said goodbye. ⚠️ THE TWO ARE ONE EVENT ON THE WIRE and
        /// `docs/TODO.md` § 140.5 says so: an alt-F4 and a pulled cable are indistinguishable to a
        /// peer, and inventing a distinction here would be inventing evidence.
        /// </summary>
        HostLost = 2,

        /// <summary>The host refused or removed this peer, and said so.</summary>
        RemovedByHost = 3,

        /// <summary>Protocol mismatch. ⚠️ THE ONE CAUSE A PLAYER CAN ACTUALLY FIX.</summary>
        VersionMismatch = 4,

        /// <summary>The room was full.</summary>
        LobbyFull = 5,

        /// <summary>A live connection was replaced by the same player reconnecting.</summary>
        Replaced = 6,
    }

    /// <summary>
    /// Classifying a session ending, and saying what it means for the match that was running.
    ///
    /// ⚠️⚠️ THE POINT OF THIS FILE IS <see cref="RevokesAuthority"/>, NOT THE LABELS.
    /// `NetSession.IsHost` reads `_nm == null || !_nm.IsListening || _nm.IsServer`, which is
    /// correct for the offline game and is the reason a CLIENT that has just lost its host
    /// answers **true** to it: its transport has stopped listening, so the first clause fires and
    /// the peer that was obeying a referee one frame ago is now claiming to be one.
    /// `NetAuthority.ShouldResolve()` is `IsHost`, so for as long as that peer is still standing in
    /// the arena it may resolve tags, award points and advance rounds **on a match nobody else is
    /// playing any more**. That is the "zombie match" § 143.9 names, and it is not a hypothetical:
    /// the same expression is what made `MatchRpc.HandleClientDisconnected` need its capitalised
    /// note about not adding an `IsHost` guard.
    ///
    /// ⚠️ ENGINE-FREE, so the whole rule can be asserted without a transport (`CLAUDE.md` § 4).
    /// </summary>
    public static class SessionEndRules
    {
        /// <summary>
        /// What the raw disconnect reason means.
        ///
        /// ⚠️⚠️ IT MATCHES ON WHAT THE HOST ITSELF WROTE AND NEVER ON NETCODE'S ENVELOPE.
        /// `ApproveConnection` authors "Game version mismatch (network protocol 23)", "This game
        /// is full: ..." and "Replaced by reconnect"; Netcode wraps its own transport events in
        /// square brackets and those describe the mechanism rather than the cause
        /// (`PlayerFacingDisconnectReason` carries the same note). **A bracketed reason is host
        /// loss**, which is the honest reading: the transport gave up and nobody said why.
        ///
        /// ⚠️ `local` WINS OVER EVERYTHING. A player who pressed QUIT is not a player who was
        /// dropped, however the transport describes the teardown behind them.
        /// </summary>
        public static SessionEndCause Classify(string reason, bool wasLocal)
        {
            if (wasLocal) return SessionEndCause.LocalQuit;
            if (string.IsNullOrWhiteSpace(reason)) return SessionEndCause.HostLost;

            string trimmed = reason.Trim();
            if (trimmed.Length > 0 && trimmed[0] == '[') return SessionEndCause.HostLost;

            string lower = trimmed.ToLowerInvariant();

            if (lower.Contains("protocol") || lower.Contains("version"))
                return SessionEndCause.VersionMismatch;
            if (lower.Contains("full")) return SessionEndCause.LobbyFull;
            if (lower.Contains("replaced")) return SessionEndCause.Replaced;
            if (lower.Contains("host left") || lower.Contains("host shutting") ||
                lower.Contains("host is leaving"))
                return SessionEndCause.HostLost;

            // ⚠️ ANYTHING ELSE THE HOST AUTHORED IS THE HOST REMOVING THIS PEER, which is what
            // "could not join this game" (a block) and "missing approved identity" both are. It
            // is deliberately NOT `HostLost`: the referee is alive and made a decision.
            return SessionEndCause.RemovedByHost;
        }

        /// <summary>The telemetry bucket. ⚠️ A LABEL WITH NO SPACE IN IT, which `TelemetryRules
        /// .Label` requires, and one value per cause so a rate is groupable.</summary>
        public static string TelemetryLabel(SessionEndCause cause)
        {
            switch (cause)
            {
                case SessionEndCause.LocalQuit: return "local";
                case SessionEndCause.HostLost: return "dropped";
                case SessionEndCause.RemovedByHost: return "removed";
                case SessionEndCause.VersionMismatch: return "version";
                case SessionEndCause.LobbyFull: return "full";
                case SessionEndCause.Replaced: return "replaced";
                default: return "other";
            }
        }

        /// <summary>
        /// Whether a peer that ended this way must stop deciding anything.
        ///
        /// ⚠️⚠️ TRUE FOR EVERY CAUSE EXCEPT A LOCAL QUIT, AND THAT IS THE WIDE ANSWER ON PURPOSE.
        /// The narrow one would be "only host loss", and it is wrong for the same reason
        /// `NetAuthority.ShouldRequest`'s note gives about the lunge: a peer removed by the host,
        /// a peer refused for its protocol and a peer whose host vanished are in **identical
        /// local state**, a stopped transport with an arena still on screen, and any of them
        /// resolving a tag is the same defect. A local quit is the one case where the player is
        /// already navigating away under their own steam.
        ///
        /// ⚠️ IT IS NOT "STOP THE GAME". Bodies keep interpolating and the screen keeps drawing;
        /// what stops is DECIDING. A frozen picture with one line saying why beats an arena that
        /// keeps awarding points nobody else will ever see.
        /// </summary>
        public static bool RevokesAuthority(SessionEndCause cause)
            => cause != SessionEndCause.None && cause != SessionEndCause.LocalQuit;

        /// <summary>
        /// The line the player reads, when the raw reason is not already a sentence worth showing.
        ///
        /// ⚠️ IT NAMES HOST LOSS RATHER THAN SAYING "DISCONNECTED". § 143.9: the diagnostic must
        /// "identify host loss rather than pretending the match ended normally", and a player told
        /// only that they were disconnected goes looking for a fault in their own wifi.
        /// </summary>
        public static string PlayerLine(SessionEndCause cause)
        {
            switch (cause)
            {
                case SessionEndCause.HostLost:
                    return "The host left and the match cannot continue.";
                case SessionEndCause.RemovedByHost:
                    return "The host ended your connection.";
                case SessionEndCause.VersionMismatch:
                    return "This game was built from a different version. Both machines have to "
                           + "run the same build.";
                case SessionEndCause.LobbyFull:
                    return "That game is full.";
                case SessionEndCause.Replaced:
                    return "You reconnected from somewhere else.";
                case SessionEndCause.LocalQuit:
                    return "";
                default:
                    return "Lost connection to the host.";
            }
        }

        /// <summary>
        /// The one line an operator or a crash bundle needs, which is not the player's line.
        ///
        /// ⚠️ IT SAYS WHAT THE MATCH IS, NOT ONLY WHY. § 143.9's requirement is that the
        /// diagnostic identify host loss "rather than pretending the match ended normally", and
        /// "abandoned" is the word that distinguishes it from a match that ran out of rounds.
        /// </summary>
        public static string Diagnostic(SessionEndCause cause, int roundNumber, int totalRounds)
        {
            if (cause == SessionEndCause.None) return "no session has ended";

            string state = RevokesAuthority(cause)
                ? $"ABANDONED at round {roundNumber} of {totalRounds}; this peer may no longer "
                  + "resolve anything"
                : $"left voluntarily at round {roundNumber} of {totalRounds}";

            return $"{cause}: {state}";
        }
    }
}
