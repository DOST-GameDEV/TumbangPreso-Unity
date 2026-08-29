using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// One player's line in one match.
    ///
    /// ⚠️⚠️ COUNTS AND DURATIONS ONLY. NO RATES ARE STORED, EVER. Every rate on the profile
    /// screen (knockdowns per throw, tags per round defended, shove hit rate) is computed from
    /// two fields in here at read time. A stored rate is a number that cannot be re-derived
    /// after a balance change and cannot be summed across matches, and the first thing anybody
    /// does with a career page is add two seasons together. `MatchRecordRules.Rate` is the one
    /// divider and it is the only place the empty-denominator case is decided.
    ///
    /// ⚠️ IT IS ENGINE-FREE ON PURPOSE, PER `FUTURE.md` § 0.5 RULE 3. The shape crosses the
    /// wire, is written by a Cloud Code script, is read back by the profile screen, and is
    /// asserted by `dotnet test` in about 40 ms without an editor. A `UnityEngine` type anywhere
    /// in here costs all three of those.
    /// </summary>
    [Serializable]
    public sealed class PlayerMatchStats
    {
        public int Slot;

        /// <summary>The UGS player id, or the local token offline. Empty for a bot.</summary>
        public string PlayerId = "";

        /// <summary>`display#1234` as it was shown in the lobby, for the history rows.</summary>
        public string Handle = "";

        public bool IsBot;

        /// <summary>`Roster` ids, never indices. See the ⚠️ on <see cref="MatchRecordRules"/>.</summary>
        public string CharacterId = "";
        public string SlipperId = "";

        public int Score;

        /// <summary>1 to 4. Ties share the better number: 1, 2, 2, 4.</summary>
        public int Placement;

        // The verbs, as counts.
        public int Throws;
        public int Knockdowns;
        public int Retrievals;

        /// <summary>
        /// Retrievals made with the taya inside
        /// <see cref="MatchRecordRules.PressureRadius"/>, which is the distance from which the
        /// taya could have tagged you with a lunge from where they were standing.
        ///
        /// ⚠️ THIS IS THE STAT `VISION.md` § 0 IS ABOUT, AND A PLAIN RETRIEVAL COUNT IS NOT IT.
        /// *"Throwing is safe and free; going back in for your tsinelas is the only moment you
        /// can be caught."* A slipper collected while the taya is across the box measures
        /// walking; the same pickup made inside the taya's reach measures the game.
        /// </summary>
        public int RetrievalsUnderPressure;

        public int Tags;
        public int Sabotages;

        /// <summary>Rounds this seat was the taya. Derived, so a per-round rate is fair by
        /// construction: everybody defends the same number of times.</summary>
        public int RoundsDefended;

        /// <summary>
        /// `ScoreEvent.DefenseTick` awards, NOT seconds.
        ///
        /// ⚠️ THE TICK IS THE THING THAT HAPPENED AND THE SECOND IS AN INTERPRETATION OF IT.
        /// `Balance.DefenseTickInterval` is 1.0 today and a record written now must still read
        /// correctly if it moves, so the conversion lives in
        /// <see cref="MatchRecordRules.PassiveDefenceSeconds"/> rather than being baked in here.
        /// </summary>
        public int DefenceTicks;

        public int TayaCampPenalties;
        public int UnretrievedSlipperPenalties;

        /// <summary>
        /// Seconds from the round-1 whistle to this seat's first throw of the match.
        ///
        /// ⚠️ -1 MEANS NEVER THREW AND IS NOT THE SAME AS 0. Averaging a never-threw match in
        /// as zero would report the most passive player in the room as the most aggressive.
        /// </summary>
        public float TimeToFirstThrow = -1.0f;

        /// <summary>Longest unbroken stretch, in seconds, as the only attacker still holding a
        /// tsinelas. The clip-worthy one.</summary>
        public float LongestLastAttacker;

        public int ShoveAttempts;
        public int ShoveHits;
        public int LungeAttempts;
        public int LungeHits;

        /// <summary>Metres, whole match. Divided by <see cref="MatchRecord.Rounds"/> for the
        /// per-round figure the profile shows, so Classic and Hero Strike compare.</summary>
        public float DistanceTravelled;

        /// <summary>
        /// This seat's score at the moment the FINAL round began.
        ///
        /// ⚠️ IT IS HERE SO CLUTCH RATE CAN BE DERIVED RATHER THAN RAISED, which `FUTURE.md`
        /// § 19.2 check 4 states outright: there is no `Clutch` score event and nothing should
        /// go looking for one. Storing the score rather than the placement keeps the derivation
        /// honest if the tie rule ever changes, because the placement can be recomputed from
        /// four numbers and the numbers cannot be recovered from a placement.
        /// </summary>
        public int ScoreAtFinalRound;
    }

    /// <summary>
    /// One finished match, whole. Written once, by one writer.
    /// </summary>
    [Serializable]
    public sealed class MatchRecord
    {
        /// <summary>
        /// Minted by the host at the whistle.
        ///
        /// ⚠️⚠️ IT IS WHAT MAKES SUBMISSION IDEMPOTENT, WHICH THE OFFLINE QUEUE REQUIRES. A
        /// queued record is submitted on the next sign-in, and the one failure mode of a queue
        /// that survives a crash is submitting twice. `ProfileRules.Apply` refuses an id it has
        /// already seen, so a double submission costs a wasted call rather than a doubled career.
        /// </summary>
        public string MatchId = "";

        /// <summary>`GameMode` as its enum name, never its int. Rule 5 of `FUTURE.md` § 0.5:
        /// wire-facing identity is string ids.</summary>
        public string Mode = "";

        /// <summary>`SceneFlow.MapEntry.Id`.</summary>
        public string MapId = "";

        public int Rounds;
        public float DurationSeconds;

        /// <summary>Round-trip "O" format, UTC.</summary>
        public string PlayedUtc = "";

        /// <summary>The winner's slot, or -1 for an honest draw, exactly as
        /// <see cref="Scoreboard.WinningSlot"/> reports it.</summary>
        public int WinningSlot = -1;

        /// <summary>True when the match ran over Relay or LAN rather than against bots only.</summary>
        public bool Online;

        /// <summary>
        /// Which slot was the taya in each round, index 0 being round 1.
        ///
        /// ⚠️ STORED RATHER THAN RE-DERIVED, EVEN THOUGH `MatchRules.DefenderSlotFor` IS PURE.
        /// The match detail screen prints "who was taya each round" for a record that may have
        /// been written by an older build, and a schedule change would silently rewrite history
        /// for every match ever played. The live match derives it; the record remembers it.
        /// </summary>
        public int[] DefenderByRound = Array.Empty<int>();

        public PlayerMatchStats[] Players = Array.Empty<PlayerMatchStats>();
    }

    /// <summary>
    /// Everything that can be asked of a match record without an engine.
    ///
    /// ⚠️ ROSTER IDS, NEVER ROSTER INDICES, IN ANYTHING SAVED. `Roster`'s own header draws the
    /// line: *"The `Id` is the stable key for anything saved to disk; the index is only ever a
    /// wire format."* A saved index survives exactly until somebody appends to the list on one
    /// machine, and then a year of history says the wrong character.
    /// </summary>
    public static class MatchRecordRules
    {
        /// <summary>
        /// Competition ranking over four scores: 1, 2, 2, 4.
        ///
        /// ⚠️ TIES SHARE THE BETTER PLACEMENT AND DO NOT CONSUME IT TWICE. Two players level on
        /// the top score are both 1st and the next is 3rd. `MatchDirector.Ranking` breaks the
        /// same tie by seat order because a BOARD has to draw the rows in some order and must
        /// not reshuffle between frames; a PLACEMENT is a claim about who did better, and seat
        /// order is not an answer to that. `Scoreboard.WinningSlot` makes the same distinction
        /// by returning -1 rather than picking the lower seat.
        /// </summary>
        public static int[] Placements(int[] scores)
        {
            if (scores == null) return Array.Empty<int>();

            var result = new int[scores.Length];
            for (int i = 0; i < scores.Length; i++)
            {
                int better = 0;
                for (int j = 0; j < scores.Length; j++)
                    if (scores[j] > scores[i]) better++;
                result[i] = better + 1;
            }
            return result;
        }

        /// <summary>Fills every <see cref="PlayerMatchStats.Placement"/> from the scores on the
        /// record itself, so the record is self-consistent before it is stored.</summary>
        public static void AssignPlacements(MatchRecord record)
        {
            if (record?.Players == null || record.Players.Length == 0) return;

            var scores = new int[record.Players.Length];
            for (int i = 0; i < record.Players.Length; i++) scores[i] = record.Players[i]?.Score ?? 0;

            int[] places = Placements(scores);
            for (int i = 0; i < record.Players.Length; i++)
                if (record.Players[i] != null) record.Players[i].Placement = places[i];
        }

        /// <summary>Seconds the lata stood while this seat defended.</summary>
        public static float PassiveDefenceSeconds(PlayerMatchStats stats)
            => stats == null ? 0.0f : stats.DefenceTicks * Balance.DefenseTickInterval;

        /// <summary>
        /// How close the taya has to be for a retrieval to count as made under pressure: 2.30 m.
        ///
        /// ⚠️⚠️ IT IS DERIVED FROM MEASURED GAMEPLAY NUMBERS AND IS NOT A TASTE THRESHOLD, which
        /// is `VISION.md` § 4's last rule. It is exactly the taya's lunge reach from a standing
        /// start: `Balance.LungeSpeed²/(2·Balance.Friction)` is the distance the dash covers,
        /// because `CLAUDE.md` § 4 requires every impulse to be written as a distance and solved
        /// for a speed, and that is 7.746²/(2·30) = **1.00 m**; `Balance.LungeTagRadius` is the
        /// **1.30 m** the sweep then reaches. So the question this stat asks is the only one
        /// worth asking of a pickup: could the defender have taken you for it, right then,
        /// without moving first.
        ///
        /// ⚠️ IT LIVES HERE RATHER THAN IN `Balance` DELIBERATELY. `Balance` holds numbers the
        /// MATCH reads, and nothing in the match reads this: a stat threshold placed among them
        /// is the next reader's excuse to make gameplay depend on it. It cannot drift either,
        /// because it is computed from the two constants rather than copied from them.
        /// </summary>
        public const float PressureRadius =
            Balance.LungeSpeed * Balance.LungeSpeed / (2.0f * Balance.Friction) + Balance.LungeTagRadius;

        /// <summary>
        /// Won the match from last place going into the final round.
        ///
        /// ⚠️ DERIVED AT READ TIME, NEVER RAISED AS AN EVENT. `FUTURE.md` § 19.2 check 4 says so
        /// explicitly, and it is the right call: a clutch is a statement about the whole match
        /// that is only true once it has ended, so an event raised mid-match would have to be
        /// taken back.
        ///
        /// ⚠️ TIED-LAST STILL COUNTS AS LAST. Two players level at the bottom of the final round
        /// were both losing it, and refusing the tie would make the stat depend on whether
        /// somebody else happened to match your score.
        /// </summary>
        public static bool IsClutch(MatchRecord record, int slot)
        {
            if (record?.Players == null || record.WinningSlot != slot) return false;
            if (record.Players.Length < 2) return false;

            var entering = new int[record.Players.Length];
            int self = -1;
            for (int i = 0; i < record.Players.Length; i++)
            {
                entering[i] = record.Players[i]?.ScoreAtFinalRound ?? 0;
                if (record.Players[i] != null && record.Players[i].Slot == slot) self = i;
            }
            if (self < 0) return false;

            int[] places = Placements(entering);
            return places[self] == LastPlacementIn(places);
        }

        private static int LastPlacementIn(int[] places)
        {
            int worst = 0;
            foreach (int p in places) if (p > worst) worst = p;
            return worst;
        }

        /// <summary>
        /// The one divider, and the one decision about an empty denominator.
        ///
        /// ⚠️ ZERO OVER ZERO IS 0, NOT NaN, AND THE CALLER IS EXPECTED TO ASK
        /// <see cref="IsReportable"/> FIRST. A NaN reaches the screen as "NaN%", which is how a
        /// stat page tells a player it is broken. Hiding the row is the answer, not printing a
        /// zero and hoping, which is why the two are separate calls.
        /// </summary>
        public static float Rate(float numerator, float denominator)
            => denominator <= 0.0f ? 0.0f : numerator / denominator;

        /// <summary>
        /// ⚠️⚠️ `FUTURE.md` § 2.2 AS A FUNCTION: *"DO NOT SHOW A STAT YOU WILL NOT DEFEND."*
        /// Every number on a public profile becomes an argument in a lobby, and a hit rate over
        /// three attempts is not a fact about a player, it is a fact about three attempts. The
        /// screen hides the row and says why rather than printing a confident 100 per cent.
        ///
        /// ⚠️ THE FLOOR IS A STARTING POINT, NOT BALANCE. `FUTURE.md` § 0.6 is explicit that
        /// numbers in the plan are illustrations; this one is the smallest denominator at which
        /// a percentage stops swinging by more than a tenth per event, which is 10.
        /// </summary>
        public const int MinimumSampleForARate = 10;

        public static bool IsReportable(float denominator, int minimum = MinimumSampleForARate)
            => denominator >= minimum;

        /// <summary>
        /// Brings a record from any source into range before it is stored or summed.
        ///
        /// ⚠️ THE SERVER RUNS THE EQUIVALENT OF THIS AND IS THE AUTHORITY. This copy exists so
        /// the offline queue stores something sane and so the tests can assert the shape in 40 ms;
        /// it is not a substitute for the validation in `ugs/cloud-code/match-record.js`, per
        /// `FUTURE.md` § 0.5 rule 6.
        /// </summary>
        public static MatchRecord Normalise(MatchRecord record)
        {
            record ??= new MatchRecord();
            record.Mode = string.IsNullOrWhiteSpace(record.Mode) ? GameMode.Classic.ToString() : record.Mode.Trim();
            record.MapId = (record.MapId ?? "").Trim();
            record.MatchId = (record.MatchId ?? "").Trim();
            record.PlayedUtc = (record.PlayedUtc ?? "").Trim();
            record.Rounds = Clamp(record.Rounds, 0, 64);
            record.DurationSeconds = Clamp(record.DurationSeconds, 0.0f, 24.0f * 3600.0f);
            record.DefenderByRound ??= Array.Empty<int>();
            record.Players ??= Array.Empty<PlayerMatchStats>();

            foreach (var p in record.Players)
            {
                if (p == null) continue;
                p.PlayerId = (p.PlayerId ?? "").Trim();
                p.Handle = AccountRules.TrySplitHandle(p.Handle, out string name, out string tag)
                    ? AccountRules.Handle(name, tag)
                    : (p.Handle ?? "").Trim();
                p.CharacterId = (p.CharacterId ?? "").Trim();
                p.SlipperId = (p.SlipperId ?? "").Trim();
                p.Score = Clamp(p.Score, 0, int.MaxValue);
                p.Throws = Clamp(p.Throws, 0, int.MaxValue);
                p.Knockdowns = Clamp(p.Knockdowns, 0, int.MaxValue);
                p.Retrievals = Clamp(p.Retrievals, 0, int.MaxValue);
                p.RetrievalsUnderPressure = Clamp(p.RetrievalsUnderPressure, 0, p.Retrievals);
                p.Tags = Clamp(p.Tags, 0, int.MaxValue);
                p.Sabotages = Clamp(p.Sabotages, 0, int.MaxValue);
                p.RoundsDefended = Clamp(p.RoundsDefended, 0, record.Rounds);
                p.DefenceTicks = Clamp(p.DefenceTicks, 0, int.MaxValue);
                p.TayaCampPenalties = Clamp(p.TayaCampPenalties, 0, int.MaxValue);
                p.UnretrievedSlipperPenalties = Clamp(p.UnretrievedSlipperPenalties, 0, int.MaxValue);
                p.ShoveHits = Clamp(p.ShoveHits, 0, int.MaxValue);
                p.ShoveAttempts = Clamp(p.ShoveAttempts, p.ShoveHits, int.MaxValue);
                p.LungeHits = Clamp(p.LungeHits, 0, int.MaxValue);
                p.LungeAttempts = Clamp(p.LungeAttempts, p.LungeHits, int.MaxValue);
                p.DistanceTravelled = Clamp(p.DistanceTravelled, 0.0f, 1_000_000.0f);
                p.LongestLastAttacker = Clamp(p.LongestLastAttacker, 0.0f, record.DurationSeconds);
                p.ScoreAtFinalRound = Clamp(p.ScoreAtFinalRound, 0, p.Score);

                // -1 is the honest "never threw"; anything else is clamped into the match.
                if (p.TimeToFirstThrow >= 0.0f)
                    p.TimeToFirstThrow = Clamp(p.TimeToFirstThrow, 0.0f, record.DurationSeconds);
                else
                    p.TimeToFirstThrow = -1.0f;
            }

            AssignPlacements(record);
            return record;
        }

        /// <summary>The record this player is in, or null. Bots are never looked up by id.</summary>
        public static PlayerMatchStats LineFor(MatchRecord record, string playerId)
        {
            if (record?.Players == null || string.IsNullOrEmpty(playerId)) return null;
            foreach (var p in record.Players)
                if (p != null && !p.IsBot && p.PlayerId == playerId) return p;
            return null;
        }

        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
