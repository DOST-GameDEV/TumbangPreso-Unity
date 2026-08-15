using System;
using System.Collections.Generic;
using System.Text;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>How a peer arriving mid-match is handled.</summary>
    public enum MidMatchRuling
    {
        /// <summary>A seat is free: sit down and play.</summary>
        Seat,

        /// <summary>This token was in THIS match and left. Give the seat back.</summary>
        Reclaim,

        /// <summary>No seat. Watch, and take the next one that opens.</summary>
        Spectate,

        /// <summary>Nothing available at all.</summary>
        Refuse,
    }

    /// <summary>One connected peer.</summary>
    public sealed class PeerRecord
    {
        public int PeerId;
        public string Token = "";
        public string Name = "";
        public int Seat = -1;
        public bool Spectator;
        public int CharacterPick = -1;
        public int CanPick = -1;
        public int SlipperPick = -1;
    }

    /// <summary>
    /// The lobby: who is here, which seat they hold, who leads, and what happens when somebody
    /// arrives late or comes back.
    ///
    /// ⚠️⚠️ THIS IS TRANSPORT AGNOSTIC ON PURPOSE AND IT IS WHERE THE REAL COMPLEXITY LIVES.
    /// Reconnection, seat reclamation, leader election and join codes are pure bookkeeping, and
    /// none of it needs to know whether Mirror or Netcode for GameObjects is carrying the
    /// bytes. Writing it here means the transport adapter in Phase 5 is thin, and means all of
    /// this can be unit tested without a network at all, which is the only way anybody will
    /// ever test a four peer reconnect properly.
    ///
    /// ⚠️ EVERY DECISION IS HOST-SIDE. A client asks; this answers. Nothing here may be driven
    /// from a client message without the host re-checking it.
    /// </summary>
    public sealed class LobbySession
    {
        public const int DefaultPort = 8910;
        public const int MaxPlayers = 4;

        /// <summary>
        /// ⚠️ THE ALPHABET EXCLUDES EVERY CONFUSABLE CHARACTER. No 0/O, no 1/I/L. A join code
        /// gets read aloud across a room or typed off somebody's screen, and "was that an oh or
        /// a zero" is a support problem you only get to solve once per tournament.
        /// </summary>
        public const string JoinCodeAlphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
        public const int JoinCodeLength = 4;

        public const string MatchFullMessage =
            "Match already started, every open seat is taken. Try again when it ends.";

        private readonly Dictionary<int, PeerRecord> _peers = new Dictionary<int, PeerRecord>();

        /// <summary>Tokens that held a seat in THIS match, so a returning peer is recognised.</summary>
        private readonly HashSet<string> _seenThisMatch = new HashSet<string>();

        /// <summary>Seats vacated mid-match, held for their original token.</summary>
        private readonly Dictionary<int, string> _heldSeats = new Dictionary<int, string>();

        public string JoinCode { get; private set; } = "";
        public int LeaderPeerId { get; private set; }
        public bool MatchInProgress { get; set; }
        public bool IsDedicated { get; set; }

        public event Action<string> JoinCodeChanged;
        public event Action<int> LeaderChanged;

        public IEnumerable<PeerRecord> Peers => _peers.Values;
        public int PeerCount => _peers.Count;

        // -------------------------------------------------------------------

        public void OpenLobby(System.Random rng)
        {
            JoinCode = MintJoinCode(rng);
            JoinCodeChanged?.Invoke(JoinCode);

            _seenThisMatch.Clear();
            _heldSeats.Clear();
        }

        public static string MintJoinCode(System.Random rng)
        {
            var sb = new StringBuilder(JoinCodeLength);
            for (int i = 0; i < JoinCodeLength; i++)
                sb.Append(JoinCodeAlphabet[rng.Next(JoinCodeAlphabet.Length)]);

            return sb.ToString();
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// The host's answer to an arriving peer. ⚠️ THE ORDER OF THESE BRANCHES IS THE RULE:
        /// a returning player gets their OWN seat back before any free seat is handed to a
        /// newcomer, or a reconnecting player watches somebody else take their place.
        /// </summary>
        public MidMatchRuling RuleOnArrival(string token)
        {
            if (string.IsNullOrEmpty(token)) return MidMatchRuling.Refuse;

            // 1. They were here, in THIS match, and left. Their seat is waiting.
            foreach (var kv in _heldSeats)
                if (kv.Value == token) return MidMatchRuling.Reclaim;

            // 2. A seat is genuinely free.
            if (FreeSeatCount() > 0) return MidMatchRuling.Seat;

            // 3. Full, but a match ending will free seats. Watch until then.
            return MatchInProgress ? MidMatchRuling.Spectate : MidMatchRuling.Refuse;
        }

        public PeerRecord Admit(int peerId, string token, string name)
        {
            var record = new PeerRecord
            {
                PeerId = peerId,
                Token = token ?? "",

                // ⚠️ SANITISED ONCE, HERE, ON ARRIVAL. Not at draw time, and not on the client.
                Name = Settings.GameSettings.SanitiseName(name),
            };

            var ruling = RuleOnArrival(record.Token);
            switch (ruling)
            {
                case MidMatchRuling.Reclaim:
                    record.Seat = ReclaimSeatFor(record.Token);
                    break;

                case MidMatchRuling.Seat:
                    record.Seat = FirstFreeSeat();
                    break;

                default:
                    record.Seat = -1;
                    record.Spectator = true;
                    break;
            }

            _peers[peerId] = record;
            if (record.Seat >= 0) _seenThisMatch.Add(record.Token);

            ClaimLeaderIfVacant(peerId);
            return record;
        }

        /// <summary>
        /// ⚠️ A SEAT IS HELD, NOT FREED, WHEN SOMEBODY DROPS MID-MATCH. Their character keeps
        /// playing under AI so the match is not ruined for the other three, and the seat waits
        /// for its token. Freeing it immediately means a reconnecting player finds a stranger
        /// in their chair with their score.
        /// </summary>
        public void Depart(int peerId)
        {
            if (!_peers.TryGetValue(peerId, out var record)) return;

            if (MatchInProgress && record.Seat >= 0 && !string.IsNullOrEmpty(record.Token))
                _heldSeats[record.Seat] = record.Token;

            _peers.Remove(peerId);

            if (LeaderPeerId == peerId) ReassignLeader();
        }

        private int ReclaimSeatFor(string token)
        {
            foreach (var kv in _heldSeats)
            {
                if (kv.Value != token) continue;

                int seat = kv.Key;
                _heldSeats.Remove(seat);
                return seat;
            }
            return -1;
        }

        public int FreeSeatCount()
        {
            int taken = 0;
            foreach (var p in _peers.Values)
                if (p.Seat >= 0) taken++;

            return Mathf.Max(0, MaxPlayers - taken - _heldSeats.Count);
        }

        public int FirstFreeSeat()
        {
            for (int seat = 0; seat < MaxPlayers; seat++)
            {
                if (_heldSeats.ContainsKey(seat)) continue;

                bool used = false;
                foreach (var p in _peers.Values)
                    if (p.Seat == seat) { used = true; break; }

                if (!used) return seat;
            }
            return -1;
        }

        public PeerRecord PeerInSeat(int seat)
        {
            foreach (var p in _peers.Values)
                if (p.Seat == seat) return p;

            return null;
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ A DEDICATED SERVER IS A REFEREE AND MUST NEVER BE THE LEADER. It holds no seat,
        /// so a lobby whose leader is the server has nobody who can actually press start. This
        /// is not a corner case: it is how the Singapore VPS runs, and it breaks in a way that
        /// is invisible when testing locally as a listen host.
        /// </summary>
        private void ClaimLeaderIfVacant(int peerId)
        {
            if (LeaderPeerId != 0) return;
            if (IsDedicated && peerId == 1) return; // the server itself

            LeaderPeerId = peerId;
            LeaderChanged?.Invoke(peerId);
        }

        private void ReassignLeader()
        {
            LeaderPeerId = 0;

            foreach (var p in _peers.Values)
            {
                if (p.Spectator || p.Seat < 0) continue;
                if (IsDedicated && p.PeerId == 1) continue;

                LeaderPeerId = p.PeerId;
                break;
            }

            LeaderChanged?.Invoke(LeaderPeerId);
        }

        public bool IsLeader(int peerId) => peerId != 0 && peerId == LeaderPeerId;

        /// <summary>
        /// A dedicated server's own peer holds no seat and plays nothing — it referees.
        /// </summary>
        public bool IsSeatlessReferee(int peerId) => IsDedicated && peerId == 1;

        /// <summary>
        /// How many READY presses the host is waiting for: one per connected HUMAN peer.
        ///
        /// ⚠️⚠️ COUNTS PEERS, NOT CHARACTERS, AND THAT IS THE WHOLE REASON A LONE HOST CAN
        /// START. A match always has four characters — the empty seats are filled with bots —
        /// but an AI cannot press R. Counting characters leaves a solo host waiting forever
        /// for three bots to agree.
        ///
        /// ⚠️ SPECTATORS ARE NOT COUNTED. They hold no seat and own no character, so they have
        /// nothing to ready; counting them hangs the gate forever on a press nobody can make.
        ///
        /// ⚠️ FLOORED AT 1 so a host whose peer list has not populated yet still needs its own
        /// press rather than starting instantly on an empty count.
        /// </summary>
        public int PlayingPeerCount(int localPeerId)
        {
            int count = 0;

            foreach (var p in _peers.Values)
            {
                if (IsSeatlessReferee(p.PeerId)) continue;

                // The local peer counts even while its own spectator flag is in flight.
                if (p.PeerId == localPeerId || !p.Spectator) count++;
            }

            return count < 1 ? 1 : count;
        }

        /// <summary>Everyone actually holding a seat: no referee, no spectators.</summary>
        public List<int> SeatedPeerIds()
        {
            var ids = new List<int>();

            foreach (var p in _peers.Values)
            {
                if (IsSeatlessReferee(p.PeerId) || p.Spectator) continue;
                ids.Add(p.PeerId);
            }

            return ids;
        }

        public int SeatedPeerCount() => SeatedPeerIds().Count;

        /// <summary>
        /// ⚠️ PICKS ARE VALIDATED HOST-SIDE, ALWAYS. A client sends an index; an index off the
        /// end of a roster must resolve to neutral rather than crash or produce a stronger
        /// unit, because a peer on an older build legitimately sends indices this build has no
        /// entry for.
        /// </summary>
        public void SetPicks(int peerId, int character, int can, int slipper)
        {
            if (!_peers.TryGetValue(peerId, out var record)) return;

            record.CharacterPick = Validate(character, Roster.People.Count);
            record.CanPick = Validate(can, Roster.Cans.Count);
            record.SlipperPick = Validate(slipper, Roster.Slippers.Count);
        }

        private static int Validate(int index, int count) =>
            (index < 0 || index >= count) ? -1 : index;

        public void EndMatch()
        {
            MatchInProgress = false;
            _heldSeats.Clear();
            _seenThisMatch.Clear();
        }
    }
}
