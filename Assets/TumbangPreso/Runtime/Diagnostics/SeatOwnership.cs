using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    /// <summary>
    /// Who claims which chair, read off the live game, in the shape
    /// <see cref="MatchInvariants.CheckSeatClaims"/> can ask questions of.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THE PREVIOUS PRODUCER MADE THE CHECK UNFALSIFIABLE.
    /// `FailureBundle` built its owner array as <c>owners[slot] = "seat" + slot</c>, so every
    /// entry was distinct by construction and the duplicate-owner rule could never fire on it,
    /// ever, in any state. A checker fed a synthetic key is a green light wired to nothing, which
    /// is `docs/TODO.md` § 96's fault one layer down: the probe asserted the plate was on screen,
    /// which is not the same claim as "somebody can reach it".
    ///
    /// ⚠️ THE TOKEN IS THE IDENTITY AND NOT THE SEAT AND NOT THE PEER ID. `NetAuthority
    /// .LocalPeerId` carries the argument: peer ids are reused across a reconnect, seats are
    /// reused across a match, and the durable connection token is the only namespace in which
    /// "the same person is in two chairs" is a statement about a person.
    ///
    /// ⚠️ IT ANSWERS FOR AN OFFLINE MATCH TOO. With no lobby, the bodies in the arena are the
    /// claims: one per seat, driven, nobody spectating. That is exactly true of single player and
    /// keeps every invariant meaningful in a probe with no transport.
    /// </summary>
    public static class SeatOwnership
    {
        /// <summary>
        /// Every claim on every chair, right now.
        ///
        /// ⚠️⚠️ IT WALKS THE PEERS AND THEN THE BODIES, AND THE TWO CAN DISAGREE. That is the
        /// whole reason it is a list rather than an array: a peer record saying "I am in seat 2"
        /// beside a body in seat 2 driven by a different token is the reconnect fault
        /// (`NetSession.OnClientConnected` disconnects the stale socket AFTER the new one takes
        /// the chair, and warns that otherwise "it can keep submitting movement and verbs for the
        /// same player"). Collapsing them into one answer per seat here would be answering the
        /// question the checker was going to ask.
        /// </summary>
        public static SeatClaim[] Claims()
        {
            var claims = new List<SeatClaim>();
            var lobby = Net.NetSession.Instance != null ? Net.NetSession.Instance.Lobby : null;

            if (lobby != null)
            {
                foreach (var peer in lobby.Peers)
                {
                    if (peer == null || string.IsNullOrEmpty(peer.Token)) continue;

                    // ⚠️ A SPECTATOR'S SEAT IS -1 AND IT IS STILL A ROW. Dropping it would delete
                    // the one fact `docs/TODO.md` § 141 is about, which is a peer that believes it
                    // is spectating while something is driving a body on its behalf.
                    bool driving = peer.Seat >= 0 && !peer.Spectator &&
                                   DrivenByAPerson(peer.Seat);

                    claims.Add(new SeatClaim(peer.Token, peer.Seat, driving, peer.Spectator));
                }

                // ⚠️ A HELD CHAIR IS A CLAIM THAT IS NOT DRIVING, and saying so is what stops the
                // checker reporting the reconnect window as a second driver every time it works.
                foreach (var held in lobby.HeldSeats)
                {
                    if (string.IsNullOrEmpty(held.Value)) continue;
                    claims.Add(new SeatClaim(held.Value, held.Key, driving: false,
                                             spectating: false));
                }

                return claims.ToArray();
            }

            // ---- offline ----------------------------------------------------
            var round = GameServices.Round;
            if (round == null) return claims.ToArray();

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var unit = round.PlayerAt(slot);
                if (unit == null || unit.IsBot) continue;

                claims.Add(new SeatClaim($"local:{slot}", slot, driving: true, spectating: false));
            }

            return claims.ToArray();
        }

        /// <summary>
        /// Whether a body in that seat exists and is being driven by a person rather than a bot.
        ///
        /// ⚠️ A BOT IN A HANDED-OVER CHAIR IS NOT A SECOND DRIVER. `SeatHandover.SeatOrigin
        /// .HandedToBot` is a seat whose peer record may still exist for the reconnect window
        /// while an `AIController` presses the buttons, and counting both would report the
        /// takeover feature as an ownership fault every time somebody's wifi died.
        /// </summary>
        private static bool DrivenByAPerson(int seat)
        {
            var round = GameServices.Round;
            if (round == null) return false;

            var unit = round.PlayerAt(seat);
            return unit != null && !unit.IsBot;
        }

        /// <summary>The four chairs as a tidy array, for a report. ⚠️ LOSSY; nothing checks it.</summary>
        public static string[] DrivenSeats() => MatchInvariants.DrivenSeats(Claims());
    }
}
