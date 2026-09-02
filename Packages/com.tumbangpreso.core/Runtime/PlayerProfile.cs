using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// Everything one player has ever done, in one mode, as counts.
    ///
    /// ⚠️ THE SAME FIELDS AS `PlayerMatchStats`, SUMMED, AND THAT PAIRING IS THE POINT. A career
    /// total that cannot be reproduced by adding up the records under it is a number nobody can
    /// argue with, and `FUTURE.md` § 2.2 is explicit that every stat on a profile becomes an
    /// argument in a lobby. `ProfileRules.Add` is the only thing that writes any of it.
    /// </summary>
    [Serializable]
    public sealed class CareerTotals
    {
        public int Matches;
        public int Wins;
        public int Draws;

        /// <summary>Index 0 is 1st place. Length is <see cref="Balance.PlayerCount"/>.
        /// ⚠️ A 4-PLAYER GAME HAS FOUR OUTCOMES, NOT TWO (`FUTURE.md` § 2.1). A win rate alone
        /// cannot tell a steady 2nd from a steady 4th, and those are different players.</summary>
        public int[] Placements = new int[Balance.PlayerCount];

        public float SecondsPlayed;

        public int Throws;
        public int Knockdowns;
        public int Retrievals;
        public int RetrievalsUnderPressure;
        public int Tags;
        public int Sabotages;
        public int RoundsDefended;
        public int DefenceTicks;
        public int TayaCampPenalties;
        public int UnretrievedSlipperPenalties;
        public int ShoveAttempts;
        public int ShoveHits;
        public int LungeAttempts;
        public int LungeHits;
        public float DistanceTravelled;

        /// <summary>Summed only over matches where a throw happened, so the average below is
        /// honest about its own denominator. See <see cref="PlayerMatchStats.TimeToFirstThrow"/>
        /// for why a never-threw match cannot be averaged in as zero.</summary>
        public float FirstThrowSecondsTotal;
        public int MatchesWithAThrow;

        public float LongestLastAttacker;
        public int Clutches;

        /// <summary>Matches finished in last place at the start of the final round, whether or
        /// not they were then won. The denominator clutch rate needs.</summary>
        public int ComebackChances;

        public int CurrentWinStreak;
        public int LongestWinStreak;
        public int TotalScore;
        public int BestScore;
    }

    /// <summary>Career totals for one mode. ⚠️ Classic and Hero Strike are never merged
    /// (`FUTURE.md` § 2.1 item 3): they are separate games, and a combined knockdown count is a
    /// number about neither of them.</summary>
    [Serializable]
    public sealed class ModeRecord
    {
        public string Mode = "";
        public CareerTotals Totals = new CareerTotals();
    }

    /// <summary>Games and wins on one roster entry, keyed by its stable id.</summary>
    [Serializable]
    public sealed class PickRecord
    {
        public string Id = "";
        public int Games;
        public int Wins;
        public int Score;
    }

    /// <summary>
    /// The career document. One per player, written only by the Cloud Code endpoint.
    ///
    /// ⚠️⚠️ IDENTITY IS NOT DUPLICATED IN HERE. The display name, tag, bio, country and pronouns
    /// live in `AccountProfile` and are owned by Authentication and the `player-account`
    /// endpoint. Two documents that both claim to know a player's name is two names, and the
    /// one on the scoreboard would be whichever loaded last. This document holds the id and
    /// nothing else about who the player is.
    /// </summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public string PlayerId = "";

        /// <summary>
        /// ⚠️⚠️ PHASE 4 OWNS THE CURVE AND THIS PHASE OWNS ONLY THE FIELD. `FUTURE.md` § 2.1
        /// draws level and border on the header card, so the document has to carry them from day
        /// one or every profile written before Phase 4 has to be migrated. Nothing in this phase
        /// awards XP: the value stays 0 and the level stays 1, and the header hides the row
        /// rather than drawing a level nobody earned. Inventing a curve here would put a
        /// progression number in the one file `FUTURE.md` § 0.6 says must never carry balance.
        /// </summary>
        public int Level = 1;
        public int Xp;

        /// <summary>
        /// One entry per hero this account has played, and none for the other twelve characters.
        ///
        /// ⚠️ THE PLAYED COUNT FOR EVERY CHARACTER IS ALREADY IN <see cref="Characters"/>, which
        /// is what `FUTURE.md` PHASE 4 means by the other twelve keeping a count and no path. This
        /// list is the PATH, and it exists only for the six in `Roster.HeroPeople`.
        /// <see cref="ProgressionRules.HasMasteryPath"/> is the one gate.
        /// </summary>
        public List<MasteryRecord> Mastery = new List<MasteryRecord>();

        /// <summary>Consecutive AFK matches. Cleared by any match that pays.</summary>
        public int AfkStrikes;

        /// <summary>Matches still owed nothing after an earned suspension. Counts down on
        /// matches that would otherwise have paid, never on further AFK ones.</summary>
        public int XpPenaltyMatches;

        /// <summary>
        /// ⚠️⚠️ PHASE 9 OWNS THESE AND THEY ARE NOW A VIEW OF <see cref="Rank"/>, NOT A
        /// SECOND COPY OF IT. They were reserved in Phase 2 so the header card would have fields
        /// to draw before there was a ladder, and every document written since carries them. They
        /// are rewritten from `Rank` by <see cref="ProfileRules.Normalise"/> for the same reason
        /// the level is re-derived from the XP one field up: **the arithmetic is what was earned
        /// and the label is a view of it**, so a stored tier that disagrees is a document written
        /// by an older build rather than a fact.
        ///
        /// ⚠️ NOTHING SHOULD READ THESE TO DECIDE ANYTHING. `RatingRules.TierFor(profile.Rank.Rating)`
        /// is the question; these are here so an older screen and an older endpoint keep working.
        /// </summary>
        public string RankTier = "";
        public int RankPoints;
        public string PeakRankTier = "";

        /// <summary>
        /// The ladder state: the rating, how sure the system is of it, and the season.
        ///
        /// ⚠️⚠️ THE PLAYER NEVER SEES ANY NUMBER IN HERE. `FUTURE.md` § 9: "the player never
        /// sees the number, only the tier". `RatingRules` has the whole model and this document
        /// only stores it.
        ///
        /// ⚠️ WRITTEN ONLY BY THE ENDPOINT, AND ONLY FOR A WITNESSED RESULT. `FUTURE.md`
        /// § 0.5 rule 6 and § 19.9's constraint: ratings go through the Phase 8 corroboration and
        /// nowhere else. A client that writes this into its local cache is writing a preview.
        /// </summary>
        public RankState Rank = new RankState();

        /// <summary>
        /// Matches abandoned in the last <see cref="IntegrityRules.AbandonMemoryDays"/> days, as
        /// UTC stamps, and the queue cooldown they bought.
        ///
        /// ⚠️⚠️ STAMPS RATHER THAN A COUNTER, BECAUSE A COUNTER CANNOT FORGET. The whole
        /// design of the escalation is that a bad week does not follow somebody into next month
        /// (`IntegrityRules.AbandonMemoryDays`), and a counter that only goes up would need a
        /// scheduled job to decay it. A short list of stamps expires by being read.
        ///
        /// ⚠️ IT IS CAPPED BY THE SAME WINDOW THAT EXPIRES IT, so it cannot grow: the endpoint
        /// drops anything older than the window every time it writes one.
        /// </summary>
        public List<string> AbandonsUtc = new List<string>();

        /// <summary>When quick match opens again. Empty means now.</summary>
        public string CooldownUntilUtc = "";

        /// <summary>
        /// Ranked matches this player has submitted that nobody has corroborated yet.
        ///
        /// ⚠️⚠️ THIS LIST IS WHAT MAKES A WITNESSED RATING POSSIBLE AT ALL WITH PER-PLAYER
        /// DOCUMENTS. Cloud Save is keyed by player id, so the endpoint cannot write into the
        /// first submitter's profile when the SECOND submitter turns up and agrees with them.
        /// So the first submitter's rating is computed, parked in the match's own shared verdict
        /// record, and this list is the note that says to go and collect it. The next `load` does,
        /// which every client already calls at boot and after every match. **No extra request and
        /// no polling.**
        ///
        /// ⚠️ A DISPUTED MATCH DROPS OFF THIS LIST WITHOUT PAYING, which is the whole point of
        /// parking the rating rather than applying it optimistically.
        /// </summary>
        public List<string> PendingRankedMatchIds = new List<string>();

        /// <summary>⚠️ PHASE 5 OWNS THIS. Stable roster ids, append-only, same contract as
        /// `Roster`'s lists.</summary>
        public string[] Inventory = Array.Empty<string>();

        public string CreatedUtc = "";
        public string UpdatedUtc = "";

        /// <summary>Every mode ever played, one entry each.</summary>
        public List<ModeRecord> Modes = new List<ModeRecord>();

        public List<PickRecord> Characters = new List<PickRecord>();
        public List<PickRecord> Slippers = new List<PickRecord>();

        /// <summary>
        /// The ids of matches already counted, newest last.
        ///
        /// ⚠️⚠️ THIS IS WHAT MAKES SUBMISSION IDEMPOTENT AND IT IS NOT OPTIONAL. The offline
        /// queue in `FUTURE.md` § 19.2 step 6 exists to submit a match played with no connection
        /// on the next sign-in, and a queue that survives a crash will eventually submit the same
        /// record twice. Without this a player's career doubles a match every time their Wi-Fi
        /// drops at the wrong moment, and nothing anywhere would report an error.
        ///
        /// ⚠️ IT IS CAPPED, SO IT IS A REPLAY WINDOW RATHER THAN A LEDGER. See
        /// <see cref="ProfileRules.AppliedIdMemory"/> for the size and why.
        /// </summary>
        public List<string> AppliedMatchIds = new List<string>();
    }

    /// <summary>
    /// A career, a history and the rules for adding a match to both.
    ///
    /// ⚠️ EVERY METHOD IS PURE AND ENGINE-FREE, WHICH IS WHY THE HARD PARTS ARE HERE RATHER THAN
    /// IN THE CLOUD CODE SCRIPT. Idempotency, streaks, comeback denominators and history trimming
    /// are all counting, and `MatchResult`'s own header records that every bug the rematch vote
    /// ever had was a counting bug. Counting can be asserted in a millisecond.
    /// </summary>
    public static class ProfileRules
    {
        /// <summary>
        /// How many match ids a profile remembers for the replay check.
        ///
        /// ⚠️ 200, WHICH IS TWICE <see cref="HistoryLimit"/> ON PURPOSE. The window has to
        /// outlive the history, or a record that has just been rolled into the totals could be
        /// resubmitted and counted a second time; keeping it at exactly 100 would make the oldest
        /// record in the history simultaneously the newest one outside the guard.
        /// </summary>
        public const int AppliedIdMemory = 200;

        /// <summary>
        /// ⚠️ `FUTURE.md` § 2.3: keep 100 full records and roll the rest into the totals. The
        /// totals are not derived from the history, they are accumulated as records arrive, so
        /// dropping the 101st loses the row and never the numbers.
        /// </summary>
        public const int HistoryLimit = 100;

        public static ModeRecord ModeFor(PlayerProfile profile, string mode)
        {
            profile.Modes ??= new List<ModeRecord>();
            mode = string.IsNullOrWhiteSpace(mode) ? GameMode.Classic.ToString() : mode.Trim();

            foreach (var m in profile.Modes)
                if (m != null && m.Mode == mode) return m;

            var added = new ModeRecord { Mode = mode, Totals = new CareerTotals() };
            profile.Modes.Add(added);
            return added;
        }

        /// <summary>
        /// Brings a career loaded from anywhere into a shape the screen can read.
        ///
        /// ⚠️⚠️ THE PLACEMENT ARRAY IS THE ONE FIELD THAT CAN ARRIVE THE WRONG LENGTH AND
        /// CRASH A READER. Everything else on `CareerTotals` is a scalar, but this is an
        /// array sized from `Balance.PlayerCount`, and a profile is JSON written by a
        /// server and stored for months: a document written before that constant moved,
        /// or truncated by a serialiser, hands the profile screen an index it does not
        /// have. It is RESIZED rather than replaced, so a shorter array keeps the counts
        /// it does carry.
        /// </summary>
        public static PlayerProfile Normalise(PlayerProfile profile)
        {
            profile ??= new PlayerProfile();
            profile.Modes ??= new List<ModeRecord>();
            profile.Characters ??= new List<PickRecord>();
            profile.Slippers ??= new List<PickRecord>();
            profile.AppliedMatchIds ??= new List<string>();
            profile.Inventory ??= Array.Empty<string>();
            profile.Mastery ??= new List<MasteryRecord>();
            profile.AbandonsUtc ??= new List<string>();
            profile.PendingRankedMatchIds ??= new List<string>();
            profile.Rank ??= new RankState();
            if (profile.Level < 1) profile.Level = 1;
            if (profile.Xp < 0) profile.Xp = 0;
            if (profile.AfkStrikes < 0) profile.AfkStrikes = 0;
            if (profile.XpPenaltyMatches < 0) profile.XpPenaltyMatches = 0;

            // ⚠️ THE LEVEL IS RE-DERIVED FROM THE XP RATHER THAN TRUSTED. Level is a pure
            // function of Xp (`ProgressionRules.LevelForXp`), so a stored level that disagrees is
            // a document written by an older curve or by hand, and the XP is the thing that was
            // actually earned. Same argument as `MatchRecord`'s "counts only, no stored rates".
            profile.Level = ProgressionRules.LevelForXp(profile.Xp);

            // ⚠️⚠️ THE TIER LABELS ARE REWRITTEN FROM THE RATING, SAME RULE AS THE LEVEL ONE
            // LINE UP. A stored `RankTier` that disagrees with `Rank.Rating` is a document written
            // before the thresholds moved, and the rating is the thing that was actually earned.
            if (profile.Rank.MatchesThisSeason > 0 || profile.Rank.PeakTier > 0)
            {
                profile.RankTier = RatingRules.TierName(RatingRules.TierFor(profile.Rank.Rating));
                profile.RankPoints = (int)System.Math.Round(profile.Rank.Rating);
                profile.PeakRankTier = RatingRules.TierName((RankTier)profile.Rank.PeakTier);
            }

            foreach (var m in profile.Mastery)
            {
                if (m == null) continue;
                if (m.Xp < 0) m.Xp = 0;
                m.Level = ProgressionRules.MasteryLevelForXp(m.Xp);
            }

            foreach (var mode in profile.Modes)
            {
                if (mode == null) continue;
                mode.Totals ??= new CareerTotals();

                var places = mode.Totals.Placements;
                if (places != null && places.Length == Balance.PlayerCount) continue;

                var resized = new int[Balance.PlayerCount];
                for (int i = 0; places != null && i < places.Length && i < resized.Length; i++)
                    resized[i] = places[i];
                mode.Totals.Placements = resized;
            }

            return profile;
        }

        private static PickRecord PickFor(List<PickRecord> list, string id)
        {
            foreach (var p in list)
                if (p != null && p.Id == id) return p;

            var added = new PickRecord { Id = id };
            list.Add(added);
            return added;
        }

        /// <summary>
        /// Adds one match to a career.
        ///
        /// Returns false and changes NOTHING when the record has already been counted, when the
        /// player is not in it, or when the record is a bot-only match. All three are ordinary
        /// rather than errors: a replayed queue entry, a spectated match, and a Practice game.
        ///
        /// ⚠️⚠️ IT IS ALL-OR-NOTHING. An early return halfway through would leave a career with
        /// a match counted in one place and not another, which is unrecoverable without the
        /// record that produced it. Everything that can refuse the record is checked before the
        /// first field is written.
        /// </summary>
        public static bool Apply(PlayerProfile profile, MatchRecord record, string playerId)
            => Apply(profile, record, playerId, out _);

        /// <summary>
        /// The same call, handing back what the match paid.
        ///
        /// ⚠️⚠️ IT IS AN `out` RATHER THAN A `LastAward` PROPERTY, AND A TEST IS WHY. The
        /// first version of this parked the award on a static so the results board could read it
        /// afterwards. xUnit runs test CLASSES in parallel, two of them applied a match at the
        /// same moment, and one read the other one. **That is not a test artefact.** The same
        /// global would be read by the results board while the offline queue flushed a second
        /// record on a background task, and the symptom in the game would be a player seeing
        /// somebody else XP about once in a hundred matches, which nobody would ever reproduce.
        /// A return value cannot race.
        ///
        /// ⚠️ `award` IS NULL WHENEVER THIS RETURNS FALSE. A refused record paid nothing, and
        /// handing back a zeroed award would let a caller draw an empty XP bar for a match that
        /// simply was not counted.
        /// </summary>
        public static bool Apply(PlayerProfile profile, MatchRecord record, string playerId,
                                 out XpAward award)
        {
            award = null;
            if (profile == null || record == null) return false;
            if (string.IsNullOrEmpty(playerId)) return false;
            if (string.IsNullOrWhiteSpace(record.MatchId)) return false;

            profile.AppliedMatchIds ??= new List<string>();
            if (profile.AppliedMatchIds.Contains(record.MatchId)) return false;

            var line = MatchRecordRules.LineFor(record, playerId);
            if (line == null) return false;

            // Snapshot the derived shelf before this match changes its source totals. The result
            // board receives only the difference, so an achievement interrupts once and never
            // re-announces on every later match.
            var achievementsBefore = AchievementRules.UnlockedIds(profile);

            profile.PlayerId = string.IsNullOrEmpty(profile.PlayerId) ? playerId : profile.PlayerId;
            profile.Characters ??= new List<PickRecord>();
            profile.Slippers ??= new List<PickRecord>();

            var totals = ModeFor(profile, record.Mode).Totals;
            totals.Placements ??= new int[Balance.PlayerCount];

            bool won = record.WinningSlot == line.Slot;
            bool drew = record.WinningSlot < 0 && line.Placement == 1;

            totals.Matches++;
            if (won) totals.Wins++;
            if (drew) totals.Draws++;

            int place = line.Placement;
            if (place >= 1 && place <= totals.Placements.Length) totals.Placements[place - 1]++;

            totals.SecondsPlayed += record.DurationSeconds;
            totals.Throws += line.Throws;
            totals.Knockdowns += line.Knockdowns;
            totals.Retrievals += line.Retrievals;
            totals.RetrievalsUnderPressure += line.RetrievalsUnderPressure;
            totals.Tags += line.Tags;
            totals.Sabotages += line.Sabotages;
            totals.RoundsDefended += line.RoundsDefended;
            totals.DefenceTicks += line.DefenceTicks;
            totals.TayaCampPenalties += line.TayaCampPenalties;
            totals.UnretrievedSlipperPenalties += line.UnretrievedSlipperPenalties;
            totals.ShoveAttempts += line.ShoveAttempts;
            totals.ShoveHits += line.ShoveHits;
            totals.LungeAttempts += line.LungeAttempts;
            totals.LungeHits += line.LungeHits;
            totals.DistanceTravelled += line.DistanceTravelled;
            totals.TotalScore += line.Score;
            if (line.Score > totals.BestScore) totals.BestScore = line.Score;

            if (line.TimeToFirstThrow >= 0.0f)
            {
                totals.FirstThrowSecondsTotal += line.TimeToFirstThrow;
                totals.MatchesWithAThrow++;
            }

            if (line.LongestLastAttacker > totals.LongestLastAttacker)
                totals.LongestLastAttacker = line.LongestLastAttacker;

            // ⚠️ THE DENOMINATOR IS COUNTED WHETHER OR NOT THE COMEBACK LANDED, which is what
            // makes clutch RATE mean anything. Counting only the successes would give a player
            // who has never been behind an undefined rate and a player who came back once from
            // one chance a perfect one.
            if (WasLastEnteringTheFinalRound(record, line))
            {
                totals.ComebackChances++;
                if (won) totals.Clutches++;
            }

            // ⚠️ A DRAW BREAKS A STREAK RATHER THAN EXTENDING IT. `Scoreboard.WinningSlot`
            // returns -1 for a tie at the top on purpose, calling it an honest draw; a streak
            // that survives one is claiming a win the rules refused to award.
            if (won)
            {
                totals.CurrentWinStreak++;
                if (totals.CurrentWinStreak > totals.LongestWinStreak)
                    totals.LongestWinStreak = totals.CurrentWinStreak;
            }
            else
            {
                totals.CurrentWinStreak = 0;
            }

            if (!string.IsNullOrEmpty(line.CharacterId))
            {
                var pick = PickFor(profile.Characters, line.CharacterId);
                pick.Games++;
                pick.Score += line.Score;
                if (won) pick.Wins++;
            }

            if (!string.IsNullOrEmpty(line.SlipperId))
            {
                var pick = PickFor(profile.Slippers, line.SlipperId);
                pick.Games++;
                pick.Score += line.Score;
                if (won) pick.Wins++;
            }

            profile.AppliedMatchIds.Add(record.MatchId);
            while (profile.AppliedMatchIds.Count > AppliedIdMemory)
                profile.AppliedMatchIds.RemoveAt(0);

            profile.UpdatedUtc = record.PlayedUtc;
            if (string.IsNullOrEmpty(profile.CreatedUtc)) profile.CreatedUtc = record.PlayedUtc;

            // ⚠️⚠️ PROGRESSION IS PAID HERE, INSIDE THE IDEMPOTENCY GUARD, AND NOWHERE ELSE.
            // Every early return above has already refused a duplicate, a missing line and a
            // bot-only match, so reaching this point IS the definition of "this match counted".
            // Awarding XP from a second call site beside `Apply` would work perfectly until the
            // offline queue resubmitted a record, which is the one thing it exists to do, and
            // then it would pay the same match twice with nothing reporting an error.
            award = ProgressionRules.Award(profile, record, line);
            foreach (var achievement in AchievementRules.Catalog)
            {
                if (achievementsBefore.Contains(achievement.Id)) continue;
                if (!AchievementRules.IsUnlocked(achievement, profile)) continue;
                award.Unlocked.Add(new Reward(achievement.RewardKind, achievement.RewardId,
                                              achievement.RewardLabel));
            }
            return true;
        }


        /// <summary>
        /// Whether this line went into the final round tied-last or worse. The denominator of
        /// clutch rate, and the reason <see cref="MatchRecordRules.IsClutch"/> can stay a
        /// question about one finished match.
        /// </summary>
        public static bool WasLastEnteringTheFinalRound(MatchRecord record, PlayerMatchStats line)
        {
            if (record?.Players == null || line == null || record.Players.Length < 2) return false;

            var entering = new int[record.Players.Length];
            int self = -1;
            for (int i = 0; i < record.Players.Length; i++)
            {
                entering[i] = record.Players[i]?.ScoreAtFinalRound ?? 0;
                if (ReferenceEquals(record.Players[i], line)) self = i;
            }
            if (self < 0) return false;

            int[] places = MatchRecordRules.Placements(entering);
            int worst = 0;
            foreach (int p in places) if (p > worst) worst = p;
            return places[self] == worst;
        }

        /// <summary>
        /// Newest first, capped at <see cref="HistoryLimit"/>, with duplicates refused.
        ///
        /// ⚠️ THE CAP IS APPLIED HERE AND AGAIN ON THE SERVER. `FUTURE.md` § 0.5 rule 6 makes the
        /// endpoint the authority over everything stored, and this copy exists so the OFFLINE
        /// queue on disk cannot grow without bound on a machine that never signs in.
        /// </summary>
        public static List<MatchRecord> Remember(List<MatchRecord> history, MatchRecord record,
                                                 int limit = HistoryLimit)
        {
            history ??= new List<MatchRecord>();
            if (record == null || string.IsNullOrWhiteSpace(record.MatchId)) return history;

            foreach (var existing in history)
                if (existing != null && existing.MatchId == record.MatchId) return history;

            history.Insert(0, record);
            while (history.Count > limit) history.RemoveAt(history.Count - 1);
            return history;
        }

        // -------------------------------------------------------------------
        // § THE RATES, WHICH ARE THE ONLY THING THE SCREEN IS ALLOWED TO PRINT
        //
        // ⚠️⚠️ EVERY ONE OF THESE RETURNS A VALUE AND A SEPARATE `Reportable` ANSWER, because
        // `FUTURE.md` § 2.2 forbids showing a stat that will not survive an argument: *"If a
        // stat is noisy at low sample size, hide it until the sample supports it and say why."*
        // A single float cannot express "I do not have enough games to say", and a screen given
        // one will print 100 per cent over two attempts.
        // -------------------------------------------------------------------

        public static float WinRate(CareerTotals t)
            => t == null ? 0.0f : MatchRecordRules.Rate(t.Wins, t.Matches);

        public static float KnockdownsPerThrow(CareerTotals t)
            => t == null ? 0.0f : MatchRecordRules.Rate(t.Knockdowns, t.Throws);

        public static float TagsPerRoundDefended(CareerTotals t)
            => t == null ? 0.0f : MatchRecordRules.Rate(t.Tags, t.RoundsDefended);

        public static float ShoveHitRate(CareerTotals t)
            => t == null ? 0.0f : MatchRecordRules.Rate(t.ShoveHits, t.ShoveAttempts);

        public static float LungeHitRate(CareerTotals t)
            => t == null ? 0.0f : MatchRecordRules.Rate(t.LungeHits, t.LungeAttempts);

        public static float RetrievalsUnderPressureRate(CareerTotals t)
            => t == null ? 0.0f : MatchRecordRules.Rate(t.RetrievalsUnderPressure, t.Retrievals);

        public static float ClutchRate(CareerTotals t)
            => t == null ? 0.0f : MatchRecordRules.Rate(t.Clutches, t.ComebackChances);

        public static float AverageTimeToFirstThrow(CareerTotals t)
            => t == null ? 0.0f : MatchRecordRules.Rate(t.FirstThrowSecondsTotal, t.MatchesWithAThrow);

        public static float PassiveDefenceSeconds(CareerTotals t)
            => t == null ? 0.0f : t.DefenceTicks * Balance.DefenseTickInterval;

        /// <summary>Metres per round, so a 4-round Classic career and an 8-round Hero Strike one
        /// are the same question.</summary>
        public static float DistancePerRound(CareerTotals t, int roundsPerMatch)
            => t == null ? 0.0f : MatchRecordRules.Rate(t.DistanceTravelled, (float)t.Matches * roundsPerMatch);

        public static float HoursPlayed(CareerTotals t)
            => t == null ? 0.0f : t.SecondsPlayed / 3600.0f;

        /// <summary>The pick with the most games, or null. Ties fall to the higher score, then to
        /// the first seen, so the answer is stable between screen refreshes.</summary>
        public static PickRecord Favourite(List<PickRecord> picks)
        {
            PickRecord best = null;
            if (picks == null) return null;

            foreach (var p in picks)
            {
                if (p == null || p.Games <= 0) continue;
                if (best == null || p.Games > best.Games ||
                    (p.Games == best.Games && p.Score > best.Score))
                    best = p;
            }
            return best;
        }
    }
}
