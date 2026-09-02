using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TumbangPreso.Core
{
    /// <summary>How a seat stopped being played, which decides whether it is punished.</summary>
    public enum DepartureKind
    {
        /// <summary>Played to the whistle. The only one that pays.</summary>
        Completed = 0,

        /// <summary>Pressed a button that says LEAVE, QUIT or BACK. Deliberate.</summary>
        Announced = 1,

        /// <summary>The socket went away with nothing announced.</summary>
        Dropped = 2,

        /// <summary>Dropped and came back inside the window. Not a leave at all.</summary>
        Returned = 3,
    }

    /// <summary>Why a player was reported. Six reasons, because a free-text box is a moderation
    /// queue and this project has nobody to staff one (`FUTURE.md` § 0.5 rule 11b).</summary>
    public enum ReportReason
    {
        None = 0,
        Cheating = 1,
        Griefing = 2,
        Afk = 3,
        OffensiveName = 4,
        OffensiveChat = 5,
        Other = 6,
    }

    /// <summary>What the endpoint decided about a submitted scoreboard.</summary>
    public enum ResultVerdict
    {
        /// <summary>First copy in. Nothing to compare it to yet.</summary>
        Pending = 0,

        /// <summary>A second peer submitted a copy that agrees. This is a real result.</summary>
        Witnessed = 1,

        /// <summary>Two peers disagree about what happened. Flagged, and nobody gets a rating.</summary>
        Disputed = 2,

        /// <summary>The record is impossible on its face and was refused before anybody witnessed it.</summary>
        Impossible = 3,
    }

    /// <summary>Why a record was refused outright. Named so a log line says which check fired.</summary>
    public enum SanityFault
    {
        None = 0,
        NoMatchId,
        NoPlayers,
        TooManyPlayers,
        ImpossibleDuration,
        ImpossibleRounds,
        ImpossibleScore,
        MoreKnockdownsThanThrows,
        MoreRetrievalsThanThrows,
        MoreHitsThanAttempts,
        DefenceLongerThanTheMatch,
        PlacementsDisagreeWithScores,
        ImpossibleTravel,
    }

    /// <summary>
    /// The integrity layer: what makes a result mean anything, and what happens to somebody who
    /// walks out of a match.
    ///
    /// ⚠️⚠️ AND THE FIRST THING IN THIS FILE IS A CORRECTION TO THE PLAN THAT COMMISSIONED IT.
    /// `FUTURE.md` § 8.1 describes the host submitting the scoreboard and ONE randomly chosen peer
    /// submitting an independent copy, "two submissions per match, not four", as a saving over the
    /// four-peer unanimous version. **That saving does not exist in this codebase, because all
    /// four human peers already submit the full record.** `match-record.js` has required since
    /// Phase 2 that the caller has a line in the record it submits, precisely so one player cannot
    /// write another player's career, so every human in a match already sends the whole scoreboard
    /// to the endpoint. Corroboration is therefore free and needs nothing new on the wire and no
    /// extra request: the endpoint compares the digests of submissions it is already receiving.
    ///
    /// ⚠️⚠️ WHICH ALSO REMOVES THE HARDEST QUESTION § 8.1 LEFT OPEN: WHO CHOOSES THE WITNESS.
    /// A witness chosen by the host is a witness chosen by the suspect. A witness derived from the
    /// match id is derived from a value the host minted. With every peer submitting, nobody
    /// chooses, and a lying host has to survive a check from three other machines rather than one.
    ///
    /// ⚠️⚠️ WHAT THIS DOES NOT STOP, WRITTEN DOWN RATHER THAN IMPLIED, PER § 19.8'S DONE-WHEN:
    /// - **A host that lies DURING the match is not caught by this.** `MatchDirector.AddScore` is
    ///   the single host-side writer (`CLAUDE.md` § 4) and every peer's scoreboard is built from
    ///   the score events that function broadcast, so a host that awards itself points in play
    ///   sends every peer the same inflated board and all four digests agree. This scheme catches
    ///   a host that plays honestly and then submits a better scoreboard, which is the cheap
    ///   attack and the one a script kiddie actually runs. The expensive attack needs a modified
    ///   build and is what § 8.2's dedicated servers are for.
    /// - **Two colluding players do not defeat it and four do.** Two agreeing submissions out of
    ///   four leaves two disagreeing ones, and <see cref="Corroborate"/> calls that DISPUTED
    ///   rather than witnessed. A room where every human is in on it is not a matchmaking problem.
    /// - **A player who never submits is not evidence of anything.** A phone losing signal at the
    ///   whistle looks exactly like a client refusing to corroborate, so a missing submission is
    ///   silence and only a CONTRADICTING one is a dispute.
    /// </summary>
    public static class IntegrityRules
    {
        /// <summary>How many agreeing human submissions make a result real.</summary>
        public const int WitnessesRequired = 2;

        // ------------------------------------------------------------------------------
        // The digest
        // ------------------------------------------------------------------------------

        /// <summary>
        /// The outcome-bearing fields of a record, in one canonical string.
        ///
        /// ⚠️⚠️ ONLY THE FIELDS AN ATTACKER WOULD WANT TO CHANGE ARE IN HERE, AND THAT IS A
        /// DELIBERATE NARROWING RATHER THAN LAZINESS. Distance travelled, time to first throw and
        /// the defence tick counter are per-machine measurements: they are sampled off each peer's
        /// own frame timing, so two honest clients disagree about them in the third decimal place
        /// every single match. Putting them in the digest would make every match disputed and the
        /// whole mechanism would be switched off within a week. What is in here is the score, the
        /// placement, who was a bot, which character, the round count and the winner: the things
        /// a result IS.
        ///
        /// ⚠️ THE PLAYER ORDER IS THE RECORD'S SEAT ORDER, WHICH IS AUTHORITATIVE AND IDENTICAL ON
        /// EVERY PEER. Sorting here would hide a record whose seats had been shuffled.
        ///
        /// ⚠️ INVARIANT CULTURE, EVERY TIME. A machine with a Filipino locale writes a decimal
        /// comma, and two honest peers on different regional settings would then disagree about a
        /// match they both saw correctly. There are no floats in this string for exactly that
        /// reason, and the ints go through `InvariantCulture` anyway so a future addition cannot
        /// reintroduce it quietly.
        /// </summary>
        public static string Canonical(MatchRecord record)
        {
            if (record == null) return "";

            var sb = new StringBuilder();
            sb.Append(record.MatchId ?? "").Append('|');
            sb.Append(record.Mode ?? "").Append('|');
            sb.Append(record.MapId ?? "").Append('|');
            sb.Append(record.Rounds.ToString(CultureInfo.InvariantCulture)).Append('|');
            sb.Append(record.WinningSlot.ToString(CultureInfo.InvariantCulture)).Append('|');

            // ⚠️ THE STAKES ARE HASHED. `MatchRecord.Ranked` says why: a flag the host writes
            // that decides whether a rating moves has to be a flag the other three agree with.
            sb.Append(record.Ranked ? "r" : "c").Append('|');

            var players = record.Players ?? Array.Empty<PlayerMatchStats>();

            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p == null) { sb.Append("-|"); continue; }

                sb.Append(p.Slot.ToString(CultureInfo.InvariantCulture)).Append(',');
                sb.Append(p.IsBot ? "b" : "h").Append(',');
                sb.Append(p.IsBot ? "" : (p.PlayerId ?? "")).Append(',');
                sb.Append(p.CharacterId ?? "").Append(',');
                sb.Append(p.Score.ToString(CultureInfo.InvariantCulture)).Append(',');
                sb.Append(p.Placement.ToString(CultureInfo.InvariantCulture)).Append('|');
            }

            return sb.ToString();
        }

        /// <summary>
        /// FNV-1a, 64 bit, over the canonical string.
        ///
        /// ⚠️⚠️ 64 BITS AND NOT 32, BECAUSE THIS IS COMPARED ACROSS A TRUST BOUNDARY. A 32-bit
        /// digest can be collided by brute force on a laptop, so a cheat could search for a
        /// scoreboard that flatters it and still matches the honest digest. 64 puts that out of
        /// reach of the machines this game is played on, and Cloud Code's Node runtime does the
        /// same arithmetic in `BigInt` so the two halves cannot drift.
        ///
        /// ⚠️ IT IS NOT A CRYPTOGRAPHIC HASH AND DOES NOT NEED TO BE. Nothing is being
        /// authenticated here; two independent parties are being asked whether they saw the same
        /// thing, and both of them are honest in the case this mechanism is for.
        ///
        /// ⚠️ HEX, LOWER CASE, ZERO PADDED TO SIXTEEN. The string is compared by the endpoint, so
        /// its FORMAT is part of the contract, not a display choice.
        /// </summary>
        public static string Digest(MatchRecord record)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            string canonical = Canonical(record);
            ulong hash = offset;

            for (int i = 0; i < canonical.Length; i++)
            {
                // UTF-16 code units, low byte then high byte, which is what the JS side walks too.
                ushort c = canonical[i];
                hash ^= (byte)(c & 0xFF);
                hash *= prime;
                hash ^= (byte)((c >> 8) & 0xFF);
                hash *= prime;
            }

            return hash.ToString("x16", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The verdict on a set of submissions for one match.
        ///
        /// ⚠️ A DISAGREEMENT BEATS AN AGREEMENT, ALWAYS, AND IT IS NOT A MAJORITY VOTE. Three
        /// agreeing and one dissenting is DISPUTED, not witnessed. A vote would mean three
        /// colluding players could ratify anything, and the whole point of the mechanism is that
        /// it is cheap to be honest and expensive to get everybody to lie.
        /// </summary>
        public static ResultVerdict Corroborate(IReadOnlyList<string> digests)
        {
            if (digests == null || digests.Count == 0) return ResultVerdict.Pending;

            string first = null;
            int agreeing = 0;

            for (int i = 0; i < digests.Count; i++)
            {
                if (string.IsNullOrEmpty(digests[i])) continue;
                if (first == null) { first = digests[i]; agreeing = 1; continue; }
                if (digests[i] != first) return ResultVerdict.Disputed;
                agreeing++;
            }

            if (first == null) return ResultVerdict.Pending;
            return agreeing >= WitnessesRequired ? ResultVerdict.Witnessed : ResultVerdict.Pending;
        }

        // ------------------------------------------------------------------------------
        // Sanity
        // ------------------------------------------------------------------------------

        /// <summary>
        /// The most points one seat can hold after a match of this shape, generously.
        ///
        /// ⚠️⚠️ IT IS A CEILING NOBODY CAN REACH, NOT A BALANCE ESTIMATE, AND THE DIFFERENCE IS
        /// WHY IT IS SAFE. A bound tuned to what a good player actually scores would refuse a
        /// genuinely extraordinary match, and refusing a real result is far worse than accepting a
        /// modest lie: the modest lie is caught by the digest check, and the refusal is a player
        /// being told their best game never happened. This assumes every second of every round is
        /// paid passive defence AND a knockdown lands every two seconds at the same time, which
        /// cannot both be true.
        /// </summary>
        public static int ScoreCeiling(int rounds, float durationSeconds)
        {
            float seconds = durationSeconds > 0.0f ? durationSeconds : rounds * Balance.RoundTime;
            if (seconds <= 0.0f) seconds = Balance.RoundTime;

            int passive = (int)(seconds * (Balance.ScoreDefensePerTick / Balance.DefenseTickInterval));
            int events = (int)(seconds / 2.0f) * Balance.ScoreLataKnocked;

            return passive + events + 1000;
        }

        /// <summary>
        /// Is this record possible at all?
        ///
        /// ⚠️ IT REFUSES ONLY THE IMPOSSIBLE, PER `FUTURE.md` § 19.8 STEP 6. Every check here is a
        /// statement about arithmetic rather than about play: you cannot knock the lata down more
        /// times than you threw, you cannot hit more shoves than you attempted, and a 90-second
        /// round cannot hold 400 seconds of defence. Anything a real match could produce passes.
        /// </summary>
        public static SanityFault Check(MatchRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.MatchId)) return SanityFault.NoMatchId;

            var players = record.Players ?? Array.Empty<PlayerMatchStats>();
            if (players.Length == 0) return SanityFault.NoPlayers;
            if (players.Length > Balance.PlayerCount) return SanityFault.TooManyPlayers;

            if (record.Rounds < 0 || record.Rounds > 64) return SanityFault.ImpossibleRounds;

            float longest = (record.Rounds + 1) * (Balance.RoundTime + 120.0f);
            if (record.DurationSeconds < 0.0f || record.DurationSeconds > longest)
                return SanityFault.ImpossibleDuration;

            int ceiling = ScoreCeiling(record.Rounds, record.DurationSeconds);
            float defenceCeiling = record.DurationSeconds + Balance.RoundTime;

            foreach (var p in players)
            {
                if (p == null) continue;

                if (p.Score < 0 || p.Score > ceiling) return SanityFault.ImpossibleScore;
                if (p.Knockdowns > p.Throws) return SanityFault.MoreKnockdownsThanThrows;
                if (p.Retrievals > p.Throws) return SanityFault.MoreRetrievalsThanThrows;
                if (p.ShoveHits > p.ShoveAttempts) return SanityFault.MoreHitsThanAttempts;
                if (p.LungeHits > p.LungeAttempts) return SanityFault.MoreHitsThanAttempts;

                if (MatchRecordRules.PassiveDefenceSeconds(p) > defenceCeiling)
                    return SanityFault.DefenceLongerThanTheMatch;

                // ⚠️ SPRINT SPEED TIMES THE WHOLE MATCH, DOUBLED. Nobody moves in a straight line
                // for eight rounds, so this only catches a teleport.
                if (p.DistanceTravelled > (Balance.Speed * Balance.SprintScale) * defenceCeiling * 2.0f)
                    return SanityFault.ImpossibleTravel;
            }

            // ⚠️ THE PLACEMENTS MUST BE THE ONES THE SCORES PRODUCE. This is the check that makes
            // rewriting a placement pointless without also rewriting the score it came from, and
            // the score is what the other three peers are looking at.
            var scores = new int[players.Length];
            for (int i = 0; i < players.Length; i++) scores[i] = players[i]?.Score ?? 0;

            var expected = MatchRecordRules.Placements(scores);
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null) continue;
                if (players[i].Placement != expected[i]) return SanityFault.PlacementsDisagreeWithScores;
            }

            return SanityFault.None;
        }

        // ------------------------------------------------------------------------------
        // Leavers
        // ------------------------------------------------------------------------------

        /// <summary>
        /// How long a seat is held for a peer that vanished before the leave becomes real.
        ///
        /// ⚠️⚠️ THIS IS THE WHOLE OF "DISTINGUISH A LEAVE FROM A DISCONNECT" AND IT IS NOT
        /// GUESSWORK ABOUT WHY A SOCKET CLOSED. `FUTURE.md` § 19.8 step 3: "or a player with bad
        /// internet is punished for their ISP". There is exactly one honest signal available, and
        /// it is whether they came BACK. A peer that announced a leave is a leave immediately; a
        /// peer that dropped is nothing at all until this window expires, and if they reconnect
        /// inside it the departure never happened. `LobbySession.Depart` already holds the seat
        /// against the durable token, which is the mechanism this number is the length of.
        ///
        /// ⚠️ 120 SECONDS IS MORE THAN A ROUND. It has to outlast a Wi-Fi handover and a phone
        /// changing cell, and the cost of it being generous is one bot for two minutes, which the
        /// match already handles because empty seats are bots.
        /// </summary>
        public const float ReconnectWindowSeconds = 120.0f;

        /// <summary>
        /// Escalating queue cooldowns, in seconds, by how many abandons are on the record.
        ///
        /// ⚠️⚠️ THE FIRST ONE IS ZERO AND THAT IS DELIBERATE. `FUTURE.md` § 19.8 step 4 asks for
        /// escalation and does not ask for punishment on a first offence. One abandoned match is a
        /// doorbell, a brownout or a parent turning the router off, and this audience is students
        /// on home connections in Metro Manila. The second one costs two minutes, which is long
        /// enough to notice and short enough not to end the evening.
        ///
        /// ⚠️ IT IS AN ARRAY RATHER THAN A FORMULA SO THE SHAPE IS READABLE AT A GLANCE and a
        /// retune is one edited number rather than a re-derived exponent.
        /// </summary>
        public static readonly int[] CooldownSeconds = { 0, 120, 600, 1800, 3600 };

        /// <summary>
        /// How long abandons are remembered. ⚠️ SEVEN DAYS, SO A BAD NIGHT DOES NOT FOLLOW SOMEBODY
        /// INTO NEXT MONTH. A permanent counter is a permanent punishment for one week of bad
        /// internet, and the behaviour this is aimed at is a habit rather than an incident.
        /// </summary>
        public const int AbandonMemoryDays = 7;

        public static int CooldownFor(int recentAbandons)
        {
            if (recentAbandons <= 0) return 0;
            int i = recentAbandons >= CooldownSeconds.Length ? CooldownSeconds.Length - 1 : recentAbandons;
            return CooldownSeconds[i];
        }

        /// <summary>
        /// The sentence the queue shows while a cooldown is running.
        ///
        /// ⚠️ IT SAYS WHY. A queue button that is simply dead is a bug report; a queue button that
        /// says what happened and when it comes back is a consequence. `CLAUDE.md` § 6.3: a dead
        /// end is a bug.
        /// </summary>
        public static string CooldownLabel(int secondsRemaining)
        {
            if (secondsRemaining <= 0) return "";

            int minutes = (secondsRemaining + 59) / 60;
            string when = minutes <= 1 ? "a minute" : $"{minutes} minutes";
            return $"You left a match early. Quick match opens again in {when}.";
        }

        /// <summary>
        /// Whether this departure costs anything.
        ///
        /// ⚠️ A DISCONNECT THAT NEVER CAME BACK STILL COUNTS, and that is not a contradiction of
        /// the paragraph above. The window is what separates the two: inside it, nothing happened;
        /// outside it, the other three played three-on-one for the rest of the match and the
        /// reason does not change what they experienced. What the distinction buys is that the
        /// player who reconnects is not punished at all, which is the case that was actually
        /// unfair.
        /// </summary>
        public static bool IsAbandon(DepartureKind kind)
            => kind == DepartureKind.Announced || kind == DepartureKind.Dropped;

        /// <summary>
        /// Ranked additionally loses rating for an abandon, by treating the leaver as last.
        ///
        /// ⚠️ IT IS NOT AN EXTRA PENALTY NUMBER. `FUTURE.md` § 9 has no leaver-specific rating
        /// arithmetic and inventing one would be a second tuning surface. Finishing last is what
        /// leaving a four-player match amounts to, so the existing pairwise expansion says it
        /// already and there is nothing new to balance.
        /// </summary>
        /// ⚠️ PLACEMENTS ARE 1-BASED IN THIS CODEBASE (`MatchRecordRules.Placements`
        /// returns 1, 2, 2, 4), so last place in a four-player match is 4 and not 3. Writing
        /// `PlayerCount - 1` here would have scored an abandon as THIRD, which is a leaver being
        /// paid for the seat they walked out of.
        public const int AbandonPlacement = Balance.PlayerCount;

        // ------------------------------------------------------------------------------
        // Rate limits
        // ------------------------------------------------------------------------------

        /// <summary>
        /// The shortest gap between two writes from one player, in seconds.
        ///
        /// ⚠️⚠️ `FUTURE.md` § 19.8 STEP 5: "a free tier is a budget an abusive client can spend."
        /// This is not about cheating, it is about a client in a retry loop costing the project
        /// its service. A real match takes six minutes at the very least, so a five-second floor
        /// between career writes is invisible to every honest player and caps a runaway client at
        /// twelve writes a minute instead of as many as it can issue.
        /// </summary>
        public const int WriteFloorSeconds = 5;

        /// <summary>The most career writes one player may make in an hour.</summary>
        public const int WritesPerHour = 60;

        /// <summary>The most reports one player may file in a day, because a report is a write
        /// too and a grudge is a loop with a person in it.</summary>
        public const int ReportsPerDay = 10;
    }
}
