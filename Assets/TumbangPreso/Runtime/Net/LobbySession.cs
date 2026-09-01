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

        // ⚠️ THE RAW CLAIM IS KEPT BESIDE THE RESOLVED NAME BECAUSE THE ANSWER ARRIVES LATER.
        // `docs/TODO.md` § 88.1c: the account endpoint is asked whether this peer owns the handle
        // it claimed, and that is a network round trip the lobby must not wait for. So arrival
        // resolves a usable name immediately from the claim, and `ApplyHandleCheck` recomputes it
        // from these three fields when the answer lands. Without the claim stored, the second
        // pass would have nothing to re-derive from but its own first guess.
        public string ClaimedName = "";
        public string AccountPlayerId = "";
        public AccountRules.HandleCheck HandleTrust = AccountRules.HandleCheck.NotAsked;

        /// <summary>
        /// What this peer is allowed to wear, decided by the host when the claim arrived.
        ///
        /// ⚠️⚠️ THE CLAIM IS NOT KEPT AND THAT IS DELIBERATE. `BannerRules.Authorise` is a pure
        /// function, so storing the answer rather than the question means nothing downstream can
        /// accidentally read an unauthorised id: **there is no unauthorised id on this record to
        /// read.** The same reasoning `docs/TODO.md` § 94.1 arrived at the hard way, where four
        /// copies of "which line is mine" all agreed on the wrong value because each was free to
        /// derive it again.
        ///
        /// ⚠️ NEVER NULL, so no drawing code has to null-check a cosmetic.
        /// </summary>
        public BannerSelection Banner = new BannerSelection();

        /// <summary>The character palette this peer may wear, authorised with the banner.</summary>
        public string Look = "";

        /// <summary>
        /// The custom character this peer is bringing, as a `C3` frame, or empty for a roster one.
        ///
        /// ⚠️ NORMALISED ON ARRIVAL, NOT AS SENT, which is the same rule as `Banner` two fields
        /// up: what is stored is the host's answer and not the peer's question, so nothing
        /// downstream can read an out-of-range index or a mixed hero kit. It is re-encoded through
        /// `CustomCharacterRules.Normalise` in `MatchRpc.HostAuthoriseCosmetics`.
        /// </summary>
        public string Custom = "";

        /// <summary>The checked Hero Strike build, never the peer's raw claim.</summary>
        public string Build = "";
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
        /// How many people may WATCH on top of the four who play.
        ///
        /// ⚠️⚠️ 🧑 2026-08-29: *"make it so taht more than 4 ppl can join, like up to 8 ppl can
        /// join but only the first 4 are players and last 4 are spectators"*. Four seats is a
        /// design rule and this is the gallery; together they are the room.
        ///
        /// ⚠️ THE MECHANISM WAS ALREADY THERE AND ONLY THE CEILING AND THE LOBBY RULE WERE
        /// WRONG. `Admit` has always answered a seatless arrival with `Seat = -1, Spectator =
        /// true`, and everything downstream — the camera, the HUD, the ready quorum, the skip
        /// vote — already excludes them. What refused a fifth person was `RuleOnArrival`'s last
        /// line, which said `MatchInProgress ? Spectate : Refuse`: **a running match could be
        /// watched and a LOBBY could not.** That is now `Spectate` either way.
        /// </summary>
        public const int MaxSpectators = 4;

        /// <summary>
        /// ⚠️ MaxConnections is deliberately larger than MaxPlayers. Four seats is a design rule,
        /// the total is a capacity ceiling, and the gap is what lets spectators attend. Relay
        /// allocations use this count.
        ///
        /// ⚠️⚠️ IT IS DERIVED NOW, AND IT WAS THE LITERAL 12 WITH FOUR SEATS UNDER IT. Two
        /// numbers with no arithmetic between them is how "how many can watch" became a thing
        /// nobody could answer without counting: **8 = 4 + 4** says it, and moving either half
        /// moves the total without a second edit.
        /// </summary>
        public const int MaxConnections = MaxPlayers + MaxSpectators;

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

            // 3. Every seat is taken. Watch.
            //
            // ⚠️⚠️ IT USED TO BE `MatchInProgress ? Spectate : Refuse`, SO A RUNNING MATCH COULD
            // BE WATCHED AND A LOBBY COULD NOT. 🧑 2026-08-29: *"make it so taht more than 4 ppl
            // can join, like up to 8 ppl can join but only the first 4 are players and last 4 are
            // spectators"*. A fifth person turning up before START MATCH was turned away, and
            // that is the case a tournament actually has — everybody arrives at once, four sit
            // down, the rest want to watch the same room rather than be told to come back.
            //
            // ⚠️ THE CAPACITY REFUSAL LIVES IN `NetSession.ApproveConnection` AND NOT HERE, and
            // that split is deliberate. This method answers "what is this person FOR"; the
            // transport answers "is there room at all", it answers it before a peer record
            // exists, and it is the only one of the two that can put a sentence on the wire for
            // the player to read. A refusal invented here would be a peer admitted and then
            // silently made useless.
            //
            // ⚠️ `Refuse` IS STILL REACHABLE AND STILL MEANS SOMETHING: an empty token, on the
            // first line. That is a malformed arrival rather than a full room.
            return MidMatchRuling.Spectate;
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

                // ⚠️ ACCOUNT HANDLE VALIDATED ONCE, HERE, ON ARRIVAL. The display name and
                // discriminator travel together, so a clipped suffix cannot impersonate a real
                // account handle on the scoreboard.
                //
                // ⚠️⚠️ AND THE NAME RESOLVES NOW RATHER THAN AFTER THE ACCOUNT ENDPOINT ANSWERS,
                // WHICH IS `FUTURE.md` § 0.5 RULE 7 IN ONE LINE. Verification is a network round
                // trip; a lobby that waited for it would be a LAN match sitting behind a login,
                // and the nationals are in a hall whose Wi-Fi may not exist. `ApplyHandleCheck`
                // upgrades or demotes this the moment there is an answer, and never before.
                Name = AccountRules.ArrivalHandle(name, token),
                ClaimedName = name ?? "",
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
        /// Records the account endpoint's answer about an arriving peer and re-resolves its name.
        /// `docs/TODO.md` § 88.1c. Answers true when the visible name changed.
        ///
        /// ⚠️⚠️ THE RESULT IS RE-DERIVED FROM THE ORIGINAL CLAIM, NOT PATCHED ONTO THE CURRENT
        /// NAME. Arrival already turned the claim into something showable, so a second pass that
        /// edited `Name` would be resolving a resolved value: a demotion would re-tag a tag, and
        /// a late second answer for the same peer would compound rather than replace. One rule,
        /// one input, run again.
        ///
        /// ⚠️ AND IT REFUSES AN ANSWER FOR A PEER THAT HAS SINCE BEEN REPLACED. Verification is
        /// async and a reconnect inside the fast-reconnect window mints a new proof for the same
        /// token, so the old answer can land after the new transport has taken the record. The
        /// player id is the guard: an answer about somebody else is dropped.
        /// </summary>
        public bool ApplyHandleCheck(int peerId, string accountPlayerId,
                                     AccountRules.HandleCheck check, string ownedHandle)
        {
            if (!_peers.TryGetValue(peerId, out var record)) return false;
            if (!string.IsNullOrEmpty(record.AccountPlayerId) &&
                record.AccountPlayerId != accountPlayerId) return false;

            record.AccountPlayerId = accountPlayerId ?? "";
            record.HandleTrust = check;

            string resolved = AccountRules.VerifiedArrivalHandle(
                record.ClaimedName, record.Token, check, ownedHandle);

            if (resolved == record.Name) return false;
            record.Name = resolved;
            return true;
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

        /// <summary>
        /// A peer asking to move chairs, or asking to leave the table altogether.
        /// <paramref name="seat"/> is 0..<see cref="MaxPlayers"/>-1 for a chair and -1 for
        /// "spectate". Returns true when the lobby actually changed.
        ///
        /// ⚠️⚠️ SEAT CHOICE WAS A CLIENT-SIDE STATIC AND THEREFORE WAS NOT A FEATURE AT ALL.
        /// The lobby's four seat buttons wrote `GameLaunch.SoloSeat`, which only the OFFLINE
        /// practice match reads: in a networked lobby the row a player pressed was drawn from
        /// `NetSession.LocalSlot`, nothing sent the choice anywhere, and the buttons were made
        /// non-interactable for everybody except the host on top of that. 🧑, 2026-08-27: *"a
        /// player cannot switch from p1 to p4"*. They could not switch to anything.
        ///
        /// ⚠️ IT IS A LOBBY MOVE AND IT IS REFUSED ONCE THE MATCH IS RUNNING. Seats carry a
        /// score, a role in the taya rotation and a body standing in the street; swapping two of
        /// them mid-round has no defined answer and nobody has asked for one. `MatchInProgress`
        /// is the switch, the same one <see cref="RuleOnArrival"/> and <see cref="Depart"/> read.
        ///
        /// ⚠️ A HELD SEAT IS NOT FREE. It belongs to somebody who dropped out of THIS match and
        /// is waiting for their token, which is the promise branch 1 of `RuleOnArrival` makes.
        /// </summary>
        public bool TryTakeSeat(int peerId, int seat)
        {
            if (!_peers.TryGetValue(peerId, out var record)) return false;

            // A dedicated server referees. It holds no chair and may not take one.
            if (IsSeatlessReferee(peerId)) return false;

            if (MatchInProgress) return false;

            if (seat < -1 || seat >= MaxPlayers) return false;

            if (seat < 0)
            {
                if (record.Seat < 0 && record.Spectator) return true;

                record.Seat = -1;
                record.Spectator = true;

                // ⚠️ A SPECTATOR CANNOT LEAD. `ReassignLeader` already skips them, but it is
                // only reached from `Depart`, so a leader who chose to spectate would otherwise
                // keep the map, the mode and the start button while holding no seat.
                if (LeaderPeerId == peerId) ReassignLeader();
                return true;
            }

            if (_heldSeats.ContainsKey(seat)) return false;

            var sitting = PeerInSeat(seat);
            if (sitting != null) return sitting.PeerId == peerId;   // already there: idempotent

            record.Seat = seat;
            record.Spectator = false;
            if (!string.IsNullOrEmpty(record.Token)) _seenThisMatch.Add(record.Token);

            // Somebody who was spectating and has just sat down is now electable, and on a
            // lobby whose only seated peer left there may be no leader at all.
            ClaimLeaderIfVacant(peerId);
            return true;
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ A DEDICATED SERVER IS A REFEREE AND MUST NEVER BE THE LEADER. It holds no seat,
        /// so a lobby whose leader is the server has nobody who can actually press start. This
        /// is not a corner case for the supported Linux server build, and it breaks in a way that
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
        /// The leader, as the HOST has just told this client it is.
        ///
        /// ⚠️⚠️ THE FIELD ALREADY TRAVELLED AND WAS THROWN AWAY AT THE DOOR. `SendSeating` writes
        /// `lobby.LeaderPeerId` into the `Seating` payload and `OnSeatingMsg` read it into a local
        /// called `leaderId` and never used it, so every client's `LeaderPeerId` stayed at the -1
        /// it is constructed with. Anything a client wanted to ask about the leader — starting
        /// with "whose name goes on WAITING FOR HOST" — had no answer available.
        ///
        /// ⚠️ IT IS A SEPARATE METHOD FROM `ClaimLeaderIfVacant` AND `ReassignLeader` BECAUSE
        /// THOSE TWO DECIDE AND THIS ONE OBEYS. Election is a host rule and stays on the host;
        /// a client applies the answer it was sent and elects nobody, which is the same split
        /// `HostAssignSeat` and `OnSeatingMsg` already keep for seats.
        /// </summary>
        public void ApplyLeaderFromHost(int peerId)
        {
            int resolved = peerId < 0 ? -1 : peerId;
            if (LeaderPeerId == resolved) return;

            LeaderPeerId = resolved;
            LeaderChanged?.Invoke(resolved);
        }

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
        ///
        /// ⚠️⚠️ THE LOCAL PEER USED TO BE EXEMPT FROM THE SPECTATOR TEST AND THAT EXEMPTION
        /// HUNG THE GATE. 🧑 2026-08-30: *"R doesnt work if theres a spectator"*. The line read
        /// `if (p.PeerId == localPeerId || !p.Spectator) count++;` under a comment saying the
        /// local peer counts *"even while its own spectator flag is in flight"* — but the second
        /// half of that `||` already counts every peer whose flag is not set, so the FIRST half
        /// could only ever fire for a local peer whose flag was set. It did not protect a
        /// decision in flight; it counted a decision already taken, the wrong way.
        ///
        /// What that costs: `ReadyGate.Update` refuses to send a press for `GameLaunch.Spectator`
        /// (§ 78.6, and it is right to — the set and the total must come from one population),
        /// so a HOST who clicked SPECTATE in its own lobby was counted in a quorum it could never
        /// vote in. Everybody else pressed R, the tally stopped one short, and the match never
        /// started. `BufferSkipVote.Needed` and `MatchResult.ExpectedVotes` are the same call and
        /// hung the same way, so the buffer skip and the rematch died with it.
        ///
        /// ⚠️ THE IN-FLIGHT CASE IS COVERED BY THE FLOOR, NOT BY AN EXEMPTION. A peer that has
        /// no record here yet is not counted at all, the count reaches 0, and the floor of 1
        /// asks for its own press.
        ///
        /// ⚠️ AND THE PEER ID ARGUMENT IS GONE WITH IT rather than left unused. It existed only
        /// to serve that exemption, every caller passed the same expression, and an argument that
        /// no longer decides anything is the next reader's false lead. `ReadyGate`'s note about
        /// it being a PEER id and not a SEAT is kept there, because the collision it records is
        /// still what this method would suffer if the argument ever came back.
        /// </summary>
        public int PlayingPeerCount()
        {
            int count = 0;

            foreach (var p in _peers.Values)
            {
                if (IsSeatlessReferee(p.PeerId)) continue;
                if (p.Spectator) continue;

                count++;
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

        /// <summary>How many seated guests can answer the host's READY question. The host is
        /// excluded because its action is START MATCH, not READY; including it creates an
        /// impossible 3/4 tally when three guests have all readied.</summary>
        public int ReadyVoterCount(int hostPeerId)
        {
            int count = 0;

            foreach (var p in _peers.Values)
            {
                if (p.PeerId == hostPeerId) continue;
                if (IsSeatlessReferee(p.PeerId) || p.Spectator || p.Seat < 0) continue;
                count++;
            }

            return count;
        }

        public bool IsReadyVoter(int peerId, int hostPeerId)
        {
            if (peerId == hostPeerId) return false;

            var peer = PeerById(peerId);
            return peer != null && !IsSeatlessReferee(peerId) &&
                   !peer.Spectator && peer.Seat >= 0;
        }

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
        /// <summary>
        /// How many connected peers are watching rather than playing.
        ///
        /// ⚠️ IT COUNTS THE FLAG, NOT `PeerCount - SeatedPeerCount`. A peer mid-admission has a
        /// record with no seat and no spectator flag for one call, and the subtraction would
        /// report them as an audience member; `Admit`'s own note records the same distinction
        /// costing the ready gate a press it waited on forever.
        ///
        /// ⚠️ AND THE DEDICATED SERVER IS NOT IN THE GALLERY. It is a referee, it is marked
        /// `Spectator` so nothing hands it a body, and counting it would advertise a room as
        /// having one more watcher than it has people.
        /// </summary>
        public int SpectatorCount()
        {
            int watching = 0;
            foreach (var p in _peers.Values)
            {
                if (p == null || !p.Spectator) continue;
                if (IsSeatlessReferee(p.PeerId)) continue;
                watching++;
            }

            return watching;
        }

        /// <summary>Is there room for one more person, in any role?</summary>
        public bool HasRoomForAnother() => ConnectedHumanCount() < MaxConnections;

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

        /// <summary>
        /// The match is over and everybody is back on the lobby screen.
        ///
        /// ⚠️⚠️ NOTHING CLEARED `MatchInProgress` ON THE WAY BACK, AND THAT IS WHY SPECTATE AND
        /// THE FOUR SEAT BUTTONS WENT DEAD AFTER THE FIRST MATCH. 🧑 2026-08-29: *"spectate
        /// button dont work in multiplayer"*.
        ///
        /// `HostStartMatch` sets it and only `NetSession.Stop` cleared it, which happens when the
        /// whole session ends. So from the first START MATCH of a session until the process left
        /// the lobby entirely, this flag stayed true — and `TryTakeSeat` opens with
        /// `if (MatchInProgress) return false;`. **Every seat request after the first match was
        /// refused in silence**, including the one SPECTATE sends. From the button it is
        /// indistinguishable from a control that was never wired up, which is exactly how it was
        /// reported.
        ///
        /// ⚠️ IT IS NOT `EndMatch`, AND THE DIFFERENCE IS THE JOIN CODE. `EndMatch` also clears
        /// `_seenThisMatch` and the code, which is right when a session is being torn down and
        /// wrong here: the lobby draws that code for people to type, and wiping it on the way
        /// back from a match would leave the room unjoinable with the room still open.
        ///
        /// ⚠️ THE HELD SEATS GO, THOUGH. A held chair means "somebody in THIS match left it", and
        /// that promise expires with the match — carrying one into the next one is what
        /// `StartMatch`'s own note says would hand a fresh match's seat to whoever held it in the
        /// last one.
        /// </summary>
        public void ReturnToLobby()
        {
            MatchInProgress = false;
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
