using System;
using System.Text.RegularExpressions;

namespace TumbangPreso.Core
{
    /// <summary>
    /// What the join screen says while a join is waiting, and when it fails.
    ///
    /// ⚠️⚠️ A HOST WHOSE BEACON ARRIVES BUT WHOSE GAME PORT DOES NOT ANSWER LOOKS STUCK, AND IT IS THE
    /// COMMON CASE OVER A VPN (2026-10-20: a LAN row over Hamachi, "it's stuck on joining", the screen
    /// reading "Joining 25.17.144.85:8910..." with nothing changing). The LAN beacon is an OUTBOUND
    /// broadcast from the host, so it gets through a host firewall that drops inbound UDP on the game
    /// port; Windows files Hamachi and similar adapters under Public networks, where a game allowed
    /// on Private only is blocked. The transport then retries for about 24 s with nothing on screen,
    /// and the failure that followed read "Could not reach ... ([Disconnect Event]...)", or, for a
    /// host on another build, "Could not reach" over a version refusal the host had stated plainly.
    ///
    /// So the waiting line counts the seconds and, past <see cref="NoAnswerHintSeconds"/>, names the
    /// usual cause; and the failure line says what happened in the player's words: no answer, another
    /// version (with both numbers), a full room, or the host's own sentence. Everything else keeps
    /// the old headline and its detail.
    ///
    /// ⚠️ ENGINE-FREE, so every sentence is asserted in Core.Tests. It reads only the status text
    /// `NetSession` already writes ("disconnected: reason", the seat-admission timeout) and
    /// `SessionEndRules.Classify`, so there is one reading of a host's reason, not two.
    /// </summary>
    public static class JoinFailureRules
    {
        /// <summary>How long a join to an address waits before the screen names the likely cause.</summary>
        public const float NoAnswerHintSeconds = 8f;

        private static readonly Regex ProtocolNumber = new Regex(@"protocol\s+(\d+)", RegexOptions.IgnoreCase);

        /// <summary>True for an address join (LAN, VPN, typed IP), false for a room code.</summary>
        public static bool LooksLikeAddress(string target) =>
            !string.IsNullOrWhiteSpace(target) &&
            (target.Contains(".") || target.Contains(":") ||
             target.Trim().Equals("localhost", StringComparison.OrdinalIgnoreCase));

        /// <summary>The line shown while a join is in flight, refreshed once a second.</summary>
        public static string WaitingLine(string target, float seconds)
        {
            int whole = seconds > 0 && !float.IsNaN(seconds) ? (int)Math.Min(seconds, 9999f) : 0;
            string line = "Joining " + target + "... " + whole + " s";
            if (seconds >= NoAnswerHintSeconds && LooksLikeAddress(target))
                line += "   No answer yet: check the host's firewall.";
            return line;
        }

        /// <summary>
        /// The line shown when a join to <paramref name="target"/> ended without a seat, from the
        /// session's last status and this build's protocol.
        /// </summary>
        public static string FailureLine(string target, string netStatus, int localProtocol)
        {
            string status = (netStatus ?? "").Trim();
            const string Disconnected = "disconnected";
            bool disconnected = status.StartsWith(Disconnected, StringComparison.OrdinalIgnoreCase);
            string reason = disconnected ? status.Substring(Disconnected.Length).TrimStart(':', ' ') : status;

            // Nobody said why: the transport gave up (Netcode's own bracketed event) or the seat
            // never came within its deadline. That is a host that does not answer.
            bool silent = disconnected && (reason.Length == 0 || reason[0] == '[');
            if (silent || status.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0)
                return "No answer from " + target + ". The host's firewall may be blocking the game.";

            if (disconnected)
            {
                switch (SessionEndRules.Classify(reason, wasLocal: false))
                {
                    case SessionEndCause.VersionMismatch:
                        var number = ProtocolNumber.Match(reason);
                        return number.Success
                            ? "That host runs another version (protocol " + number.Groups[1].Value + ", yours " +
                              localProtocol + "). Use the same build."
                            : "That host runs another version of the game. Use the same build.";
                    case SessionEndCause.LobbyFull:
                        return "That room is full.";
                    case SessionEndCause.HostLost:
                        return "The host closed the room.";
                    default:
                        return reason;   // the host's own sentence: a block, a skills mismatch
                }
            }

            return status.Length == 0 ? "Could not reach " + target + "."
                : "Could not reach " + target + ".  (" + status + ")";
        }
    }
}
