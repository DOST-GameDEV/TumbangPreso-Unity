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

        /// <summary>
        /// Whether each seat has done anything at all this round, and how far it has walked.
        ///
        /// ⚠️⚠️ THESE ARE THE AFK SIGNAL AND MOVEMENT IS HALF OF IT ON PURPOSE. `FUTURE.md`
        /// PHASE 4 asks for a seat that has not acted for a whole round to be paid nothing, and
        /// the obvious implementation, reading `InputIntent`, does not work: the host never
        /// receives a remote player intent, only their transform through
        /// `MatchRpc.SubmitMoveServerRpc`. It would have caught the local seat and the bots and
        /// nobody else, which is exactly the wrong three. Position arrives for every seat, this
        /// class already samples it for `DistanceTravelled`, and
        /// `ProgressionRules.AfkRoundMetres` is the bar.
        ///
        /// ⚠️ THE VERB HALF IS NOT REDUNDANT WITH THE DISTANCE HALF. A taya who plants
        /// themselves by the lata and punches everything that comes near has barely moved and is
        /// playing the game hard, and the anti-camp clock already owns whether that is legal.
        /// </summary>
        private readonly bool[] _actedThisRound = new bool[Balance.PlayerCount];
        private readonly float[] _roundDistance = new float[Balance.PlayerCount];

        /// <summary>Rounds whose activity has been committed to the lines. See
        /// <see cref="CommitRoundActivity"/> and the padding in <see cref="OnMatchEnded"/>.</summary>
        private int _roundsCommitted;

        /// <summary>
        /// This machine's own frame times over the live rounds of the current match.
        ///
        /// ⚠️⚠️ IT IS FILLED ON EVERY PEER, ABOVE THIS CLASS'S HOST GATE, AND THAT IS THE ONE
        /// THING ABOUT IT THAT MATTERS. Everything else here is host-only because a NUMBER IN THE
        /// MATCH may only be created in one place; a frame rate is not a number in the match, it
        /// is a property of the machine reading it, and `FUTURE.md` § 3 asks for the distribution
        /// ACROSS machines. Collected behind the gate it would be the host's frame rate reported
        /// once per peer, which is a plausible-looking answer to a question nobody asked. The
        /// telemetry match-started count in `OnRoundStarted` already sits above the gate for the
        /// same reason, and its comment carries the other half of the argument.
        ///
        /// ⚠️ NOTHING IN THE MATCH READS IT. `MatchRecord` does not carry it, it never crosses the
        /// wire, and it is not in the career: it goes to `TelemetrySink` and nowhere else. This
        /// class's header rule stands, that a stat which changes what it measures is not a stat.
        /// </summary>
        private readonly FrameRateHistogram _frameRate = new FrameRateHistogram();

        /// <summary>The current match's frame sample, so a probe can assert it filled.</summary>
        public FrameRateHistogram FrameRate => _frameRate;

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

            SampleFrameRate(round);

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

            // ⚠️ THE FRAME SAMPLE IS CLEARED PER MATCH, HERE, RATHER THAN AFTER IT IS SENT. A
            // match that is abandoned, disconnected from, or never adopted never reaches
            // `Adopt`, so a sampler cleared on send would carry the abandoned match's frames
            // into the next one and report a number describing two matches on two different
            // maps. The first round of a match is the one moment every peer reaches, whatever
            // happened to the match before it.
            if (round <= 1) _frameRate.Clear();

            // ⚠️⚠️ THE WITNESS STARTS ON EVERY PEER AT ROUND 1, ABOVE THE HOST GATE, FOR THE
            // SAME REASON TELEMETRY DOES. `ScoreWitness` is the peer's independent tally of what
            // the host announced during play, and a tally only the host kept would be the host
            // agreeing with itself. `docs/TODO.md` § 103.
            //
            // ⚠️ ROUND 1 IS THE TEST FOR "SAW THE START", and it is exact rather than
            // approximate: a peer that arrives by backfill or by reconnect during round 3 reaches
            // `OnRoundStarted` with `round == 3`, never with 1, so it never calls `Begin` and its
            // `Complete` stays false from construction. `ScoreWitness`' header is why that has to
            // be silence rather than a dispute.
            if (round <= 1)
                Net.ScoreWitness.Ensure(gameObject).Begin(GameServices.Match, sawTheStart: true);

            // ⚠️⚠️ AND THE MACHINE WRITES DOWN THAT IT IS IN A MATCH, WHICH IS THE LEAVER
            // PENALTY. `CareerStore.InMatchSinceUtc` has the reasoning: a penalty that only fires
            // when somebody presses QUIT is a penalty on the polite. **Only a networked match with
            // another human in it counts**, because leaving a practice match against bots costs
            // nobody anything.
            if (round <= 1 && NetAuthority.IsNetworked)
            {
                int humans = 0;
                var lobby = Net.NetSession.Instance?.Lobby;
                if (lobby != null) humans = lobby.SeatedPeerCount();

                Net.CareerStore.Instance?.NoteMatchStarted(humans > 1);
            }

            if (!NetAuthority.ShouldResolve()) return;

            if (round <= 1) BeginMatch();
            if (!_running) return;

            // ⚠️ THE ROUND THAT JUST ENDED IS COMMITTED HERE, AT THE START OF THE NEXT ONE.
            // There is no round-ended event on `MatchDirector` that fires for the last round and
            // for every round before it, so the two boundaries this class already sees are the
            // start of a round and the end of the match, and between them they cover all of them.
            if (round > 1) CommitRoundActivity();

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
            _roundsCommitted = 0;
            _running = true;
            Array.Clear(_defenderByRound, 0, _defenderByRound.Length);

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                _lines[slot] = new PlayerMatchStats { Slot = slot, TimeToFirstThrow = -1.0f };
                _hasPosition[slot] = false;
                _actedThisRound[slot] = false;
                _roundDistance[slot] = 0.0f;
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

                // ⚠️⚠️ THE ACCOUNT ID, NEVER `ConnectionToken`, AND THE DIFFERENCE MEANT NO
                // CAREER HAD EVER REACHED THE SERVER. `ugs/cloud-code/match-record.js` finds the
                // submitter's line with `p.PlayerId === context.playerId`, and `context.playerId`
                // is the UGS Authentication player id and nothing else. `ConnectionToken` reads
                // `IsGuest ? PlayerId : NetIdentity.Token`, and `IsGuest` is the TOURNAMENT guest
                // flag rather than "signed in anonymously", so for an ordinary player it returns
                // `NetIdentity.Token`, which falls back to the machine's local settings token the
                // moment UGS is not signed in at the whistle. A record stamped that way is refused
                // 422 on every retry for ever. Measured 2026-08-30: three records queued behind
                // each other on the player's own machine, all four lines carrying
                // `GameSettings.PlayerToken`. `docs/TODO.md` § 94.1.
                //
                // ⚠️ AND THE REMOTE HALF HAS TO MOVE WITH IT, because every peer submits the SAME
                // record from its own session and looks itself up by its own account id.
                // `PeerRecord.AccountPlayerId` is what protocol 16 put in the approval hello for
                // the impersonation guard; `PeerRecord.Token` is the connection token and is the
                // same wrong answer one machine further away.
                if (!networked || slot == localSeat)
                {
                    line.PlayerId = account != null ? account.PlayerId : Net.NetIdentity.Token;
                    if (account != null) line.Handle = account.LobbyName;
                    continue;
                }

                var peer = net.Lobby?.PeerInSeat(slot);
                line.PlayerId = peer?.AccountPlayerId ?? "";
                if (peer != null && !string.IsNullOrWhiteSpace(peer.Name)) line.Handle = peer.Name;
            }
        }

        private static string CharacterIdFor(CharacterMotor motor)
        {
            // ⚠️ ONE OWNER. This was five lines of roster lookup here and was about to become a
            // second copy in `MatchInstaller`, which needs the same id to look a palette loadout
            // up. `docs/TODO.md` § 94.1 records what four hand-written copies of one identity
            // lookup cost, and the answer was one accessor rather than four careful edits.
            return motor == null ? "" : Roster.PersonIdAt(motor.Mode, motor.CharacterIndex);
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
            CommitRoundActivity();

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

                // ⚠️⚠️ THE STAKES COME OFF THE QUEUE THAT PRODUCED THE ROOM, NOT OFF A
                // TOGGLE. `INSPIRATION.md` § 3.1: the mode is the ruleset and the queue is the
                // stakes, so a match is ranked because a ranked queue put these four people in
                // one room, and there is nothing in `Design.md` that a ranked match plays
                // differently. A room hosted by hand or joined by code is never ranked, which is
                // the same rule read from the other side: four friends in a private lobby cannot
                // arrange a rating between themselves.
                Ranked = Net.Matchmaker.Current != null &&
                         Net.Matchmaker.Current.Stake == Core.QueueStake.Ranked,
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

            PadUnplayedRounds(record);
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

            // ⚠️⚠️ THE PEER'S OWN DIGEST TRAVELS WITH THE RECORD, AND IT IS COMPUTED FROM
            // THE TALLY RATHER THAN FROM THE RECORD BEING ADOPTED. Hashing the record here would
            // hash the host's own JSON on all four machines and prove nothing at all;
            // `ScoreWitness.Digest` substitutes this peer's counted scores first. That distinction
            // is the entire value of Phase 8 and it is one line, so it is worth being loud about.
            string witness = GetComponent<Net.ScoreWitness>()?.Digest(record) ?? "";

            Net.CareerStore.Instance?.Record(record, witness);
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

        /// <summary>
        /// Counts one frame, if a round is actually being played.
        ///
        /// ⚠️⚠️ `RoundActive` IS THE WINDOW, AND IT IS THE WINDOW BECAUSE OF WHAT IT EXCLUDES.
        /// It is false through the scene load, the countdown, every gap between rounds and the
        /// whole results board, so none of those frames reach the sample. `docs/TODO.md` § 90.3
        /// left the frame rate open on exactly this point: a percentile over everything includes
        /// a loading screen, and a loading screen renders at whatever it likes.
        ///
        /// ⚠️⚠️ IT IS `unscaledDeltaTime`, NEVER `deltaTime`, AND THE DIFFERENCE IS THE WHOLE
        /// MEASUREMENT. `Time.deltaTime` is scaled: at a `timeScale` of 0.5 a machine holding a
        /// steady 60 fps would report 30, and at 0 every frame is zero-length and the histogram
        /// drops it, so a paused stretch would silently leave the sample rather than be measured
        /// as the slow frames a pause menu actually renders. A frame rate is wall clock per
        /// rendered frame by definition, and `unscaledDeltaTime` is that.
        /// ⚠️⚠️ `Time.captureDeltaTime` DOES NOT REACH `unscaledDeltaTime`, AND THIS COMMENT SAID
        /// THE OPPOSITE UNTIL IT WAS MEASURED. Under a captured step of 16.67 ms the sample read
        /// **2.13 ms per frame**, which is the batchmode editor's real wall clock, so a probe run
        /// fills this histogram with several hundred frames per second rather than a tidy 60.
        /// `MatchFrameRateProbe.UnderACapturedStepTheSampleReadsWallClockAndNothingSendsIt` is
        /// that number, and it exists so nobody restores the comfortable version of this sentence.
        ///
        /// ⚠️⚠️ IT IS HARMLESS, AND THE REASON IS THE ONLY THING KEEPING IT HARMLESS.
        /// `TelemetrySink.Flush` returns immediately when no account is signed in, and a probe
        /// never signs in, so nothing a probe measures has ever left the machine. **Do not make
        /// this method read the captured step to tidy the number up.** That would put a
        /// fabricated 60 fps into the sample of any future run that DOES sign in, which is a
        /// worse failure than an obviously silly one: 469 fps is visibly not a player, and a
        /// clean 60 is indistinguishable from one.
        ///
        /// ⚠️⚠️ AND IT DOES NOT COUNT WHEN TELEMETRY IS OFF, WHICH IS NOT THE SAME AS NOT
        /// SENDING. `docs/TODO.md` § 90.3: *"turning it off stops the counting, not only the
        /// sending"*, because a buffer that fills anyway is a buffer a later version can decide
        /// to flush, and that is the same thing as having no opt-out. The histogram is 256
        /// integers rather than a growing list, so this gate buys nothing in memory; it is the
        /// rule being kept where it would be easiest to quietly not keep it.
        /// </summary>
        private void SampleFrameRate(RoundDirector round)
        {
            if (!round.RoundActive || !Net.TelemetrySink.Enabled) return;
            _frameRate.Add(Time.unscaledDeltaTime);
        }

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

            // ⚠️⚠️ THE FRAME RATE IS THIS MACHINE'S AND IS REPORTED WHATEVER SEAT IT SAT IN, so
            // it is outside the human-seat guard above. A spectator, a player whose line is
            // missing from the record and a machine running four bots all rendered the same
            // match at whatever rate they managed, and `FUTURE.md` § 3's question is about the
            // machine rather than about the player. The mode and map come from the RECORD rather
            // than from `SceneFlow`, so a match that ended on a map this peer switched away from
            // is still labelled with the map it was played on.
            telemetry.NoteFrameRate(record.Mode, record.MapId, _frameRate);

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

            // ⚠️ A PENALTY IS AN ACTION TOO. A seat collecting a taya-camp penalty is standing
            // in the wrong place on purpose, which is a decision; the anti-camp clock is what
            // punishes it, and paying it nothing for the whole match as well would punish the
            // same thing twice through two systems that were designed separately.
            NoteActed(slot);

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
            NoteActed(slot);

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
            NoteActed(slot);
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
            NoteActed(slot);
            if (hit) line.ShoveHits++;
        }

        /// <summary>The lunge is two calls because it is two moments: the dash is spent when it
        /// is thrown, and the sweep that hits runs for frames afterwards.</summary>
        public void NoteLungeAttempt(int slot)
        {
            if (!_running || !NetAuthority.ShouldResolve()) return;

            var line = LineFor(slot);
            if (line == null) return;

            line.LungeAttempts++;
            NoteActed(slot);
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
                    float step = Vector3.Distance(_lastPosition[slot], here);
                    var line = _lines[slot];
                    if (line != null) line.DistanceTravelled += step;
                    _roundDistance[slot] += step;
                }

                _lastPosition[slot] = here;
                _hasPosition[slot] = true;
            }
        }

        /// <summary>
        /// Marks a seat as having played this round. Called from every verb the host resolves.
        ///
        /// ⚠️ IT IS NOT GATED AND DOES NOT NEED TO BE: every caller is already behind the same
        /// `_running` and `NetAuthority.ShouldResolve()` pair, and adding a third copy of the gate
        /// here is the kind of duplication that makes the next reader wonder which one is load
        /// bearing.
        /// </summary>
        private void NoteActed(int slot)
        {
            if (slot >= 0 && slot < _actedThisRound.Length) _actedThisRound[slot] = true;
        }

        /// <summary>
        /// Closes one round of activity into the lines and starts the next one clean.
        ///
        /// ⚠️⚠️ THE FIRST COMMIT IS WHAT TURNS -1 INTO A MEASUREMENT. `ActiveRounds` starts at
        /// -1 meaning "nobody measured this", which is what every record from before this phase
        /// carries and what `ProgressionRules.WasAfk` refuses to read as AFK. A host that reaches
        /// this method has measured the seat, so the sentinel is replaced before anything is
        /// counted into it.
        /// </summary>
        private void CommitRoundActivity()
        {
            _roundsCommitted++;

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var line = _lines[slot];
                if (line != null)
                {
                    if (line.ActiveRounds < 0) line.ActiveRounds = 0;
                    if (_actedThisRound[slot] || _roundDistance[slot] >= ProgressionRules.AfkRoundMetres)
                        line.ActiveRounds++;
                }

                _actedThisRound[slot] = false;
                _roundDistance[slot] = 0.0f;
            }
        }

        /// <summary>
        /// Credits every seat with the rounds that never happened.
        ///
        /// ⚠️⚠️ WITHOUT THIS, EVERY PLAYER IN A MATCH THAT ENDED EARLY IS FLAGGED AFK. The
        /// record `Rounds` is the SCHEDULED total (`Mathf.Max(_roundsSeen, TotalRounds)`), so a
        /// Hero Strike match that ended after three rounds still says 8, and a seat that played
        /// all three of them would read 3 active out of 8 and be paid nothing for a game it
        /// played properly. A round nobody played is not a round somebody sat out.
        /// </summary>
        private void PadUnplayedRounds(MatchRecord record)
        {
            int unplayed = record.Rounds - _roundsCommitted;
            if (unplayed <= 0) return;

            foreach (var line in record.Players)
                if (line != null && line.ActiveRounds >= 0) line.ActiveRounds += unplayed;
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
