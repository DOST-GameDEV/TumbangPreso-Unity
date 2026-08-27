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
        /// ⚠️ MaxConnections (12) is deliberately larger than MaxPlayers (4). Four seats is a
        /// design rule, twelve connections is a capacity ceiling, and the gap is what lets
        /// spectators attend a full match. Relay allocations use this count.
        /// </summary>
        public const int MaxConnections = 12;

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
        /// <summary>
        /// Transport id of the lobby leader, or -1 when nobody can lead yet.
        ///
        /// ⚠️ ZERO IS A REAL NETCODE CLIENT ID. It used to mean both "no leader" and the
        /// listen host, so the host could never satisfy IsLeader and a dedicated lobby could
        /// not distinguish an empty chair from client 0. A sentinel must not be a legal value
        /// of the thing it represents.
        /// </summary>
        public int LeaderPeerId { get; private set; } = -1;
        public bool MatchInProgress { get; set; }
        public bool IsDedicated { get; set; }

        public event Action<string> JoinCodeChanged;
        public event Action<int> LeaderChanged;

        public IEnumerable<PeerRecord> Peers => _peers.Values;
        public int PeerCount => _peers.Count;

        // -------------------------------------------------------------------

        /// <summary>
        /// Opens a brand new lobby.
        ///
        /// ⚠️⚠️ IT CLEARS THE PEER TABLE, THE LEADER AND THE MATCH FLAG, NOT JUST THE SEAT
        /// HOLDS. This object outlives a session: `NetSession` owns one `LobbySession` for the
        /// lifetime of the process, so hosting, quitting to the menu and hosting again reached
        /// this method with the previous match's peers, its leader id and `MatchInProgress`
        /// still set. The visible faults were a second lobby that already believed it had four
        /// players, a leader id belonging to a transport that no longer exists (so nobody could
        /// change the map), and `RuleOnArrival` answering Spectate to the first person to join
        /// a brand new lobby because the last match had never been marked finished.
        /// </summary>
        public void OpenLobby(System.Random rng)
        {
            Reset();

            JoinCode = MintJoinCode(rng);
            JoinCodeChanged?.Invoke(JoinCode);
        }

        /// <summary>
        /// Forgets everything about the previous session. Separate from <see cref="EndMatch"/>,
        /// which ends a MATCH inside a lobby that keeps its peers.
        /// </summary>
        public void Reset()
        {
            _peers.Clear();
            _seenThisMatch.Clear();
            _heldSeats.Clear();
            MatchInProgress = false;

            if (LeaderPeerId != -1)
            {
                LeaderPeerId = -1;
                LeaderChanged?.Invoke(LeaderPeerId);
            }
        }

        public void SetJoinCode(string code)
        {
            string sanitized = code ?? "";
            if (JoinCode == sanitized) return;
            JoinCode = sanitized;
            JoinCodeChanged?.Invoke(JoinCode);
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
            => Admit(peerId, token, name, out _);

        /// <summary>
        /// Admits a transport and reports an older still-connected transport replaced by the
        /// same durable token. The caller may disconnect that stale socket after the new one has
        /// taken the seat, closing the fast-reconnect window without ever freeing the chair.
        /// </summary>
        public PeerRecord Admit(int peerId, string token, string name, out int replacedPeerId)
        {
            replacedPeerId = -1;
            var record = new PeerRecord
            {
                PeerId = peerId,
                Token = token ?? "",

                // ⚠️ SANITISED ONCE, HERE, ON ARRIVAL. Not at draw time, and not on the client.
                Name = Settings.GameSettings.SanitiseName(name),
            };

            // A replacement connection can arrive before the transport's generous 30 second
            // timeout declares the old one dead. Same durable token means the new transport
            // connection takes over the existing peer record immediately; otherwise a quick
            // relaunch is misclassified as a newcomer and receives a different seat.
            PeerRecord replaced = null;
            if (!string.IsNullOrEmpty(record.Token))
            {
                foreach (var connected in _peers.Values)
                {
                    if (connected.Token != record.Token) continue;
                    replaced = connected;
                    break;
                }
            }

            if (replaced != null)
            {
                replacedPeerId = replaced.PeerId;
                record.Seat = replaced.Seat;
                record.Spectator = replaced.Spectator;
                record.CharacterPick = replaced.CharacterPick;
                record.CanPick = replaced.CanPick;
                record.SlipperPick = replaced.SlipperPick;
                _peers.Remove(replaced.PeerId);

                if (LeaderPeerId == replaced.PeerId)
                {
                    LeaderPeerId = peerId;
                    LeaderChanged?.Invoke(peerId);
                }
            }

            // ⚠ THE DEDICATED SERVER'S OWN PEER IS RULED ON BEFORE ARRIVAL RULES APPLY, because
            // it is not arriving to play. IsSeatlessReferee was already honoured by leader
            // election and by both peer counts, but not here, so on a dedicated host the server
            // process took seat 0 and the first real player was handed seat 1. A four player
            // match then had three human seats and a referee holding the fourth.
            // ⚠️ THREE BRANCHES, AND THE FIRST TWO ARE NOT ARRIVALS AT ALL. A replaced transport
            // has already had its seat copied above, and a dedicated server's own peer is a
            // referee. Only the third is somebody asking for a chair.
            //
            // ⚠️ THE SECOND FAST-RECONNECT LOOKUP THAT USED TO LIVE HERE IS DELETED. It searched
            // `_peers` for the same token a second time, after the block above had already
            // found, copied and REMOVED that record, so it could never match. Two searches for
            // one fact is how one of them silently stops being exercised.
            if (replaced == null && IsSeatlessReferee(peerId))
            {
                record.Seat = -1;
                record.Spectator = true;
            }
            else if (replaced == null)
            {
                switch (RuleOnArrival(record.Token))
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

                // ⚠️ A SEAT THAT COULD NOT BE FOUND IS A SPECTATOR, NOT SEAT -1 WITH THE FLAG
                // OFF. `FirstFreeSeat` returns -1 when the table disagrees with `FreeSeatCount`,
                // and a record with `Seat == -1` and `Spectator == false` is read as a player by
                // `PlayingPeerCount` and by the ready gate, which then waits for a press from
                // somebody who has no body to press it with.
                if (record.Seat < 0) record.Spectator = true;
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
        public PeerRecord Depart(int peerId)
        {
            if (!_peers.TryGetValue(peerId, out var record)) return null;

            if (MatchInProgress && record.Seat >= 0 && !string.IsNullOrEmpty(record.Token))
                _heldSeats[record.Seat] = record.Token;

            _peers.Remove(peerId);

            if (LeaderPeerId == peerId) ReassignLeader();
            return record;
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

        /// <summary>
        /// Looks up transport identity, not gameplay seat. These integers often happen to
        /// match for the first few local connections, which is why passing a peer id into
        /// PeerInSeat survived until a reconnect received client id 5 and silently found no
        /// seat at all.
        /// </summary>
        public PeerRecord PeerById(int peerId)
            => _peers.TryGetValue(peerId, out var peer) ? peer : null;

        public bool IsSeatOccupied(int seat) => PeerInSeat(seat) != null || _heldSeats.ContainsKey(seat);

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ A DEDICATED SERVER IS A REFEREE AND MUST NEVER BE THE LEADER. It holds no seat,
        /// so a lobby whose leader is the server has nobody who can actually press start. This
        /// is not a corner case: it is how the Singapore VPS runs, and it breaks in a way that
        /// is invisible when testing locally as a listen host.
        /// </summary>
        private void ClaimLeaderIfVacant(int peerId)
        {
            if (LeaderPeerId >= 0) return;
            if (IsDedicated && peerId == 1) return; // the server itself

            LeaderPeerId = peerId;
            LeaderChanged?.Invoke(peerId);
        }

        private void ReassignLeader()
        {
            LeaderPeerId = -1;

            foreach (var p in _peers.Values)
            {
                if (p.Spectator || p.Seat < 0) continue;
                if (IsDedicated && p.PeerId == 1) continue;

                LeaderPeerId = p.PeerId;
                break;
            }

            LeaderChanged?.Invoke(LeaderPeerId);
        }

        public bool IsLeader(int peerId) => peerId >= 0 && peerId == LeaderPeerId;

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
        /// How many of the four chairs are unavailable to a newcomer: seated peers plus seats
        /// being HELD for somebody who dropped mid-match.
        ///
        /// ⚠️⚠️ THIS IS NOT `SeatedPeerCount` AND IT IS NOT `PeerCount`, AND THE BROWSERS WERE
        /// SHOWING ONE NUMBER FOR ALL THREE. `PeerCount` counts every connection, so a lobby with
        /// two players and six spectators advertised "8/4" and was filtered out of the LAN list as
        /// full. `SeatedPeerCount` under-reports the other way: a seat held for a disconnected
        /// player is not free, and a browser that says 3/4 for a match that will refuse the next
        /// arrival is worse than one that says 4/4.
        /// </summary>
        public int OccupiedSeatCount()
        {
            int taken = 0;
            foreach (var p in _peers.Values)
                if (!IsSeatlessReferee(p.PeerId) && !p.Spectator && p.Seat >= 0) taken++;

            return Mathf.Clamp(taken + _heldSeats.Count, 0, MaxPlayers);
        }

        /// <summary>Every attached human, spectators included, referee excluded.</summary>
        public int ConnectedHumanCount()
        {
            int count = 0;
            foreach (var p in _peers.Values)
                if (!IsSeatlessReferee(p.PeerId)) count++;

            return count;
        }

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

        /// <summary>
        /// Marks the match as running. The counterpart of EndMatch, and the switch every
        /// mid-match rule reads: Depart only holds a seat while this is true, and RuleOnArrival
        /// only answers Spectate rather than Refuse while this is true.
        /// </summary>
        /// ⚠ Held seats are cleared here, not just in EndMatch. A held seat means "somebody in
        /// THIS match left it", which is what RuleOnArrival branch 1 promises, so carrying one
        /// across a match boundary would hand a fresh match's seat to whoever held it in the
        /// last one. _seenThisMatch is deliberately NOT cleared: peers admitted during the
        /// lobby phase, before the match starts, were legitimately seen in it.
        public void StartMatch()
        {
            MatchInProgress = true;
            _heldSeats.Clear();
        }

        public void EndMatch()
        {
            MatchInProgress = false;
            _heldSeats.Clear();
            _seenThisMatch.Clear();
            SetJoinCode("");
        }
    }
}
