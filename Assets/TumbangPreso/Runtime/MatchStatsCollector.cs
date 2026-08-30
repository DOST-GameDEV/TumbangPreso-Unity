using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Counts what happened in a match and hands one <see cref="MatchRecord"/> to
    /// <see cref="Net.CareerStore"/> when it ends.
    ///
    /// ⚠️⚠️ IT COLLECTS ON THE HOST AND NOWHERE ELSE, for the reason `MatchDirector`'s header
    /// gives about points: a number that can only be created in one place cannot be created on a
    /// client at all. Every entry point here opens with the same `NetAuthority.ShouldResolve()`
    /// gate the verbs themselves are behind, so a client counts nothing and cannot disagree with
    /// the host about its own match. In single player that gate is a host with no peers and
    /// always passes, so nothing needs special casing.
    ///
    /// ⚠️⚠️ AND IT IS DELIBERATELY NOT WIRED INTO `AddScore`'S GUARD. `MatchDirector.Scored`
    /// fires on a client too, through `ApplyNetworkScoreEvent`, whose whole job is to replay a
    /// point for presentation without touching the scoreboard. Subscribing to that event without
    /// the gate below would have every client counting the host's knockdowns as its own and
    /// submitting a second, disagreeing record for the same match.
    ///
    /// ⚠️ NOTHING IN HERE MAY CHANGE A GAMEPLAY NUMBER. It reads the match; it never writes to
    /// it. `FUTURE.md` § 0.5 rule 4 says this of progression and it is doubly true of a
    /// measurement: a stat that changes what it measures is not a stat.
    /// </summary>
    public sealed class MatchStatsCollector : MonoBehaviour
    {
        private readonly PlayerMatchStats[] _lines = new PlayerMatchStats[Balance.PlayerCount];
        private readonly Vector3[] _lastPosition = new Vector3[Balance.PlayerCount];
        private readonly bool[] _hasPosition = new bool[Balance.PlayerCount];
        private readonly bool[] _taggedThisRound = new bool[Balance.PlayerCount];
        private readonly int[] _defenderByRound = new int[64];

        private string _matchId = "";
        private string _mode = "";
        private string _mapId = "";
        private int _roundsSeen;
        private float _matchClock;
        private bool _running;

        /// <summary>When the sole untagged attacker's stretch began, or -1 while there is not
        /// exactly one. See <see cref="CloseLastAttackerStretch"/>.</summary>
        private float _lastAttackerSince = -1.0f;
        private int _lastAttackerSlot = -1;

        /// <summary>The finished record, kept so the end-of-match summary can read it after the
        /// board goes up. Null until a match has ended on this machine.</summary>
        public MatchRecord Last { get; private set; }

        /// <summary>Raised on every peer once a finished record is available, whether this
        /// machine counted it or received it from the host.</summary>
        public event Action<MatchRecord> RecordReady;

        private void OnEnable()
        {
            var match = GameServices.Match;
            if (match == null) return;

            match.RoundStarted += OnRoundStarted;
            match.MatchEnded += OnMatchEnded;
            match.Scored += OnScored;
        }

        private void OnDisable()
        {
            var match = GameServices.Match;
            if (match == null) return;

            match.RoundStarted -= OnRoundStarted;
            match.MatchEnded -= OnMatchEnded;
            match.Scored -= OnScored;
        }

        /// <summary>
        /// ⚠️ THE ROUND DIRECTOR IS SUBSCRIBED LAZILY RATHER THAN IN `OnEnable`. Both live on the
        /// `~GameServices` root and `GameServices.Ensure` adds them in one pass, so the order
        /// `AddComponent` happens to run in decides whether `GameServices.Round` is assigned yet
        /// when this component's `OnEnable` fires. That is the same undefined-order hazard
        /// `GameServices.Ensure` records about two `RuntimeInitializeOnLoadMethod` hooks, and the
        /// answer is the same: do not race it.
        /// </summary>
        private bool _roundSubscribed;

        private void Update()
        {
            var round = GameServices.Round;
            if (round == null) return;

            if (!_roundSubscribed)
            {
                round.Tagged += OnTagged;
                _roundSubscribed = true;
            }

            if (!_running || !NetAuthority.ShouldResolve()) return;
            if (!round.RoundActive) return;

            float dt = Time.deltaTime;
            _matchClock += dt;
            SampleDistance(round);
            TickLastAttacker();
        }

        private void OnDestroy()
        {
            if (_roundSubscribed && GameServices.Round != null)
                GameServices.Round.Tagged -= OnTagged;
        }

        // -------------------------------------------------------------------
        // § THE MATCH BOUNDARY
        // -------------------------------------------------------------------

        private void OnRoundStarted(int round, int defenderSlot)
        {
            // ⚠️⚠️ CLEARED ON EVERY PEER, ABOVE THE HOST GATE, AND THAT PLACEMENT IS THE
            // WHOLE POINT OF IT. `MatchResult` reads `Last` when the board goes up, and on a
            // CLIENT the record has not arrived yet at that moment: it comes over
            // `MatchRecord` a beat later. Leaving the previous match's record standing means
            // a client opens the results board showing the summary of the game before this
            // one, for as long as the broadcast takes, which is worse than showing nothing.
            // Every peer runs `StartMatch` on its own arena load, so every peer reaches this.
            if (round <= 1) Last = null;

            // ⚠️⚠️ TELEMETRY COUNTS THE MATCH ON EVERY PEER, ABOVE THE HOST GATE, FOR THE SAME
            // REASON THE LINE ABOVE IT IS THERE. `BeginMatch` is host-only because the RECORD is
            // authored once; a started-match count that only the host raised would report a
            // four-player online match as one match started and four finished, and the funnel's
            // `first_match_started` step would never fire for anybody who has only ever joined.
            // `docs/TODO.md` § 90.3.
            if (round <= 1) NoteMatchStartedToTelemetry();

            if (!NetAuthority.ShouldResolve()) return;

            if (round <= 1) BeginMatch();
            if (!_running) return;

            _roundsSeen = Mathf.Max(_roundsSeen, round);
            if (round - 1 >= 0 && round - 1 < _defenderByRound.Length)
                _defenderByRound[round - 1] = defenderSlot;

            var line = LineFor(defenderSlot);
            if (line != null) line.RoundsDefended++;

            // ⚠️⚠️ THE FINAL-ROUND SNAPSHOT IS THE ONLY THING CLUTCH RATE NEEDS, AND IT HAS TO BE
            // TAKEN HERE. `MatchRecordRules.IsClutch` asks who was last going INTO the last round;
            // once the round has been played the scores that answer it no longer exist anywhere.
            // `FUTURE.md` § 19.2 check 4 is explicit that there is no `Clutch` event to raise, so
            // four integers taken at one round boundary is the whole implementation.
            var match = GameServices.Match;
            if (match != null && round == match.TotalRounds)
                for (int slot = 0; slot < Balance.PlayerCount; slot++)
                    if (_lines[slot] != null) _lines[slot].ScoreAtFinalRound = match.ScoreFor(slot);

            ResetRoundRoles(defenderSlot);
        }

        private void BeginMatch()
        {
            _matchId = Guid.NewGuid().ToString("N");
            _mode = UI.SceneFlow.SelectedMode.ToString();
            _mapId = UI.SceneFlow.SelectedMap ?? "";
            _matchClock = 0.0f;
            _roundsSeen = 0;
            _running = true;
            Array.Clear(_defenderByRound, 0, _defenderByRound.Length);

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                _lines[slot] = new PlayerMatchStats { Slot = slot, TimeToFirstThrow = -1.0f };
                _hasPosition[slot] = false;
            }

            IdentifySeats();
        }

        /// <summary>
        /// Reads who is in each seat, once, at the whistle.
        ///
        /// ⚠️⚠️ IDENTITY IS TAKEN AT THE START AND NOT AT THE END, AND THAT IS NOT A DETAIL. The
        /// cast is torn down on the way to the results board and a peer that leaves in the last
        /// round is gone from the lobby by the time the record is written, so asking then gives a
        /// record with empty rows for exactly the players a match is worth remembering for.
        ///
        /// ⚠️ THE HOST READS EVERY SEAT'S TOKEN FROM ITS OWN `LobbySession`, which is the only
        /// place that mapping exists. `MatchRpc.GetSeatInfo` carries the NAME to a client and
        /// deliberately never carries the token: a peer's durable id is not something the other
        /// three players need, and putting it on the wire would hand every lobby a list of
        /// everybody's account ids.
        /// </summary>
        private void IdentifySeats()
        {
            var round = GameServices.Round;
            var net = Net.NetSession.Instance;
            var account = GameServices.Account;
            bool networked = net != null && net.IsNetworked;
            int localSeat = networked ? net.LocalSlot : -1;
            string[] slipperIds = SlipperIdsBySeat();

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var line = _lines[slot];
                var motor = round?.PlayerAt(slot);

                line.IsBot = motor == null || motor.IsBot;
                line.Handle = motor != null ? motor.DisplayName() : $"P{slot + 1}";
                line.CharacterId = CharacterIdFor(motor);
                line.SlipperId = slipperIds[slot];

                if (line.IsBot)
                {
                    // ⚠️ A BOT CARRIES NO ID AT ALL. `MatchRecordRules.LineFor` refuses to match a
                    // bot by id whatever it holds, and an empty string makes that impossible to
                    // get wrong twice.
                    line.PlayerId = "";
                    continue;
                }

                if (!networked || slot == localSeat)
                {
                    line.PlayerId = account != null ? account.ConnectionToken : Net.NetIdentity.Token;
                    if (account != null) line.Handle = account.LobbyName;
                    continue;
                }

                var peer = net.Lobby?.PeerInSeat(slot);
                line.PlayerId = peer?.Token ?? "";
                if (peer != null && !string.IsNullOrWhiteSpace(peer.Name)) line.Handle = peer.Name;
            }
        }

        private static string CharacterIdFor(CharacterMotor motor)
        {
            if (motor == null) return "";
            var list = motor.Mode == GameMode.HeroStrike ? Roster.HeroPeople : Roster.ClassicPeople;
            int index = motor.CharacterIndex;
            return index >= 0 && index < list.Count ? list[index].Id : "";
        }

        /// <summary>
        /// The skin each seat's tsinelas is wearing, read once at the whistle.
        ///
        /// ⚠️⚠️ KEYED ON `SeatOfOrigin`, NEVER ON `OwnerSlot`, AND THAT DIFFERENCE IS A REAL
        /// BUG THIS REPOSITORY HAS ALREADY PAID FOR ONCE. `docs/TODO.md` § 78.1: `OwnerSlot`
        /// is state the game rewrites every round, and `SliceRunner.EquipOwnedSlippers`
        /// disowns the taya's shoe by setting it to -1. A record written at the end of a
        /// round in which somebody defended would therefore have no slipper for that seat,
        /// and a per-tsinelas win rate would quietly under-count the taya's shoe every time.
        /// `Slipper.SeatOfOrigin` is assigned once per match and never moves.
        ///
        /// ⚠️ THE SCENE IS SEARCHED ONCE PER MATCH, NOT ONCE PER SEAT. `FindObjectsByType` is
        /// not free and there is no registry of slippers to ask instead; four calls to it at
        /// the whistle would be three more than the job needs.
        ///
        /// ⚠️ AN EMPTY ID IS A LEGITIMATE ANSWER. `MatchBootstrap`, which is the path the
        /// headless probes run, never assigns `SeatOfOrigin`, so a probe match records no
        /// slipper. That is correct: a probe has no player whose per-tsinelas record it
        /// could belong to. `ProfileRules.Apply` skips an empty id rather than inventing one.
        /// </summary>
        private static string[] SlipperIdsBySeat()
        {
            var ids = new string[Balance.PlayerCount];
            for (int i = 0; i < ids.Length; i++) ids[i] = "";

            var all = UnityEngine.Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None);
            foreach (var slipper in all)
            {
                if (slipper == null) continue;
                int seat = slipper.SeatOfOrigin;
                if (seat < 0 || seat >= ids.Length) continue;

                int skin = slipper.SkinIndex;
                ids[seat] = skin >= 0 && skin < Roster.Slippers.Count
                    ? Roster.Slippers[skin].Id
                    : "";
            }
            return ids;
        }

        private void OnMatchEnded(int winningSlot)
        {
            if (!_running || !NetAuthority.ShouldResolve()) return;
            _running = false;
            CloseLastAttackerStretch();

            var match = GameServices.Match;
            var record = new MatchRecord
            {
                MatchId = _matchId,
                Mode = _mode,
                MapId = _mapId,
                Rounds = Mathf.Max(_roundsSeen, match != null ? match.TotalRounds : 0),
                DurationSeconds = _matchClock,
                PlayedUtc = DateTime.UtcNow.ToString("O"),
                WinningSlot = winningSlot,
                Online = NetAuthority.IsNetworked,
                Players = new PlayerMatchStats[Balance.PlayerCount],
            };

            record.DefenderByRound = new int[record.Rounds];
            for (int i = 0; i < record.Rounds && i < _defenderByRound.Length; i++)
                record.DefenderByRound[i] = _defenderByRound[i];

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var line = _lines[slot] ?? new PlayerMatchStats { Slot = slot, TimeToFirstThrow = -1.0f };
                if (match != null) line.Score = match.ScoreFor(slot);
                record.Players[slot] = line;
            }

            MatchRecordRules.Normalise(record);
            Adopt(record);
            Net.MatchRpc.Instance?.BroadcastMatchRecord(record);
        }

        /// <summary>
        /// Takes a finished record as this machine's own, whether it counted it or received it.
        ///
        /// ⚠️ THE SUBMISSION IS PER PLAYER AND THE RECORD IS PER MATCH, and that separation is
        /// the point of routing every peer through here. `CareerStore` submits only THIS
        /// player's line, from this player's own authenticated session, so a host cannot write
        /// another player's career even though it authored the numbers. `docs/TODO.md` § 89.3
        /// carries the argument and what it does and does not fix.
        /// </summary>
        public void Adopt(MatchRecord record)
        {
            if (record == null) return;

            Last = record;
            Net.CareerStore.Instance?.Record(record);
            NoteMatchFinishedToTelemetry(record);
            RecordReady?.Invoke(record);
        }

        // -------------------------------------------------------------------
        // § TELEMETRY. `docs/TODO.md` § 90.3.
        //
        // ⚠️ IT HANGS OFF THE TWO POINTS THAT ALREADY EXIST RATHER THAN ADDING EVENTS. A match
        // start and a finished record are already the two moments this class is built around, so
        // telemetry costs two calls and no new bookkeeping. `FUTURE.md` § 3 asks for mode and map
        // split, match length distribution and pick rates, and all four are read off state that
        // is here anyway.
        // -------------------------------------------------------------------

        private void NoteMatchStartedToTelemetry()
        {
            var telemetry = GameServices.Telemetry;
            if (telemetry == null) return;

            var round = GameServices.Round;
            int humans = 0;
            int bots = 0;
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var motor = round?.PlayerAt(slot);
                if (motor == null) continue;
                if (motor.IsBot) bots++; else humans++;
            }

            telemetry.NoteMatchStarted(UI.SceneFlow.SelectedMode.ToString(),
                                       UI.SceneFlow.SelectedMap ?? "", humans, bots);
        }

        private void NoteMatchFinishedToTelemetry(MatchRecord record)
        {
            var telemetry = GameServices.Telemetry;
            if (telemetry == null || record?.Players == null) return;

            var net = Net.NetSession.Instance;
            int seat = net != null && net.IsNetworked ? net.LocalSlot : 0;
            var line = seat >= 0 && seat < record.Players.Length ? record.Players[seat] : null;

            // ⚠️⚠️ THE PICK IS RECORDED AT THE END, FROM THE RECORD, AND NOT AT CHARACTER SELECT.
            // `FUTURE.md` § 3 asks for "character and tsinelas pick and win rates", and both
            // halves of that sentence want the same row: a pick counted at selection time would
            // count a player who cycles the roster twelve times as twelve picks, would count a
            // match somebody backed out of, and would sit in a different event from the placement
            // that says whether the pick won. One event, at the one moment both facts exist.
            //
            // ⚠️ AND IT IS THIS MACHINE'S OWN SEAT ONLY. Every peer adopts the same record and
            // reports its own line, so four players produce four rows; a host reporting all four
            // would count each bot's assigned character as somebody's choice.
            if (line != null && !line.IsBot)
                telemetry.NotePick(record.Mode, line.CharacterId, line.SlipperId);

            // ⚠️ THE FINISH GOES LAST BECAUSE IT IS WHAT FLUSHES. `NoteMatchFinished` sends the
            // session's buffer, so anything noted after it would sit until quit, which is the
            // flush most likely to lose its race with the process shutting down.
            telemetry.NoteMatchFinished(record.Mode, record.MapId, record.Rounds,
                                        record.DurationSeconds, line?.Placement ?? 0);
        }

        // -------------------------------------------------------------------
        // § WHAT THE MATCH TELLS IT
        // -------------------------------------------------------------------

        private void OnScored(int slot, ScoreEvent e)
        {
            // ⚠️ THE GATE IS NOT REDUNDANT WITH `AddScore`'S. `ApplyNetworkScoreEvent` raises
            // this same event on a client for presentation, and that path has no host guard by
            // design. See this class's header.
            if (!_running || !NetAuthority.ShouldResolve()) return;

            var line = LineFor(slot);
            if (line == null) return;

            switch (e)
            {
                case ScoreEvent.LataKnocked: line.Knockdowns++; break;
                case ScoreEvent.Tag: line.Tags++; break;
                case ScoreEvent.Sabotage: line.Sabotages++; break;
                case ScoreEvent.DefenseTick: line.DefenceTicks++; break;
                case ScoreEvent.TayaCampPenalty: line.TayaCampPenalties++; break;
                case ScoreEvent.UnretrievedSlipperPenalty: line.UnretrievedSlipperPenalties++; break;
            }
        }

        /// <summary>Called from `Carrier.HostThrowAt`, which is already host-gated.</summary>
        public void NoteThrow(int slot)
        {
            if (!_running || !NetAuthority.ShouldResolve()) return;

            var line = LineFor(slot);
            if (line == null) return;

            line.Throws++;

            // ⚠️ FIRST OF THE MATCH, NOT FIRST OF THE ROUND. `FUTURE.md` § 2.2 wants a number
            // that separates two players on the same score by how early they commit, and a
            // per-round figure is dominated by whichever rounds they spent as the taya, who
            // cannot throw at all.
            if (line.TimeToFirstThrow < 0.0f) line.TimeToFirstThrow = _matchClock;
        }

        /// <summary>
        /// Called from `Carrier.NotifyHolding`, the one funnel every pickup goes through.
        ///
        /// ⚠️ ONLY YOUR OWN TSINELAS COUNTS, WHICH IS THE SAME LINE `Carrier` ALREADY DRAWS FOR
        /// THE HERO ECONOMY. Picking up somebody else's is a denial play and a good one, but it
        /// is not the run `VISION.md` § 0 says the game is built around and it carries none of
        /// the same risk. Counting both under one name would make the stat mean neither.
        /// </summary>
        public void NoteRetrieval(int slot, bool ownSlipper, float distanceToTaya)
        {
            if (!_running || !ownSlipper || !NetAuthority.ShouldResolve()) return;

            var line = LineFor(slot);
            if (line == null) return;

            line.Retrievals++;
            if (distanceToTaya >= 0.0f && distanceToTaya <= MatchRecordRules.PressureRadius)
                line.RetrievalsUnderPressure++;
        }

        /// <summary>Called from `CombatVerbs.HostResolveShove` once the verb has actually been
        /// spent, so a press refused by a cooldown or an empty stamina bar is not an attempt.</summary>
        public void NoteShoveAttempt(int slot, bool hit)
        {
            if (!_running || !NetAuthority.ShouldResolve()) return;

            var line = LineFor(slot);
            if (line == null) return;

            line.ShoveAttempts++;
            if (hit) line.ShoveHits++;
        }

        /// <summary>The lunge is two calls because it is two moments: the dash is spent when it
        /// is thrown, and the sweep that hits runs for frames afterwards.</summary>
        public void NoteLungeAttempt(int slot)
        {
            if (!_running || !NetAuthority.ShouldResolve()) return;

            var line = LineFor(slot);
            if (line != null) line.LungeAttempts++;
        }

        public void NoteLungeHit(int slot)
        {
            if (!_running || !NetAuthority.ShouldResolve()) return;

            var line = LineFor(slot);
            if (line != null) line.LungeHits++;
        }

        // -------------------------------------------------------------------
        // § THE TWO STATS NOTHING RAISES AN EVENT FOR
        // -------------------------------------------------------------------

        private void SampleDistance(RoundDirector round)
        {
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var motor = round.PlayerAt(slot);
                if (motor == null) { _hasPosition[slot] = false; continue; }

                Vector3 here = motor.transform.position;
                here.y = 0.0f;

                if (_hasPosition[slot])
                {
                    var line = _lines[slot];
                    if (line != null) line.DistanceTravelled += Vector3.Distance(_lastPosition[slot], here);
                }

                _lastPosition[slot] = here;
                _hasPosition[slot] = true;
            }
        }

        /// <summary>
        /// ⚠️⚠️ "LAST ATTACKER" IS AN INTERPRETATION, BECAUSE NOTHING IN THIS GAME IS ELIMINATED.
        /// `FUTURE.md` § 2.2 asks for *"longest survival as last attacker"*, which is a stat from
        /// a game where players go out. Here a tag costs a teleport, a stagger and the whole trip
        /// again, and the round carries on with all four. The reading that survives contact with
        /// the rules is **the last of the three attackers not yet caught this round**: while you
        /// are the only one the taya has not taken, you are the only one they can still take, and
        /// that is the clip. `docs/TODO.md` § 89.2 records the choice so nobody re-derives it as
        /// a bug.
        /// </summary>
        private void ResetRoundRoles(int defenderSlot)
        {
            CloseLastAttackerStretch();
            for (int slot = 0; slot < Balance.PlayerCount; slot++) _taggedThisRound[slot] = false;
            _taggedThisRound[defenderSlot] = true;
            EvaluateLastAttacker();
        }

        private void OnTagged(int defenderSlot, int attackerSlot)
        {
            if (!_running || !NetAuthority.ShouldResolve()) return;
            if (attackerSlot < 0 || attackerSlot >= _taggedThisRound.Length) return;

            if (_lastAttackerSlot == attackerSlot) CloseLastAttackerStretch();
            _taggedThisRound[attackerSlot] = true;
            EvaluateLastAttacker();
        }

        private void EvaluateLastAttacker()
        {
            int untagged = -1, count = 0;
            for (int slot = 0; slot < _taggedThisRound.Length; slot++)
            {
                if (_taggedThisRound[slot]) continue;
                untagged = slot;
                count++;
            }

            if (count == 1 && _lastAttackerSlot != untagged)
            {
                _lastAttackerSlot = untagged;
                _lastAttackerSince = _matchClock;
            }
            else if (count != 1)
            {
                _lastAttackerSlot = -1;
                _lastAttackerSince = -1.0f;
            }
        }

        private void TickLastAttacker()
        {
            if (_lastAttackerSlot < 0) return;

            var line = _lines[_lastAttackerSlot];
            if (line == null) return;

            float held = _matchClock - _lastAttackerSince;
            if (held > line.LongestLastAttacker) line.LongestLastAttacker = held;
        }

        private void CloseLastAttackerStretch()
        {
            TickLastAttacker();
            _lastAttackerSlot = -1;
            _lastAttackerSince = -1.0f;
        }

        private PlayerMatchStats LineFor(int slot)
            => slot >= 0 && slot < _lines.Length ? _lines[slot] : null;
    }
}
