using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TumbangPreso.Core;
using Xunit;

namespace TumbangPreso.Core.Tests
{
    /// <summary>
    /// XP, levels, mastery and the AFK rule. `FUTURE.md` PHASE 4, `docs/TODO.md` § 91.
    /// </summary>
    public sealed class ProgressionTests
    {
        // -------------------------------------------------------------------
        // § FIXTURES
        // -------------------------------------------------------------------

        /// <summary>
        /// A finished four-player match where seat 0 wins, everybody played every round, and
        /// nobody hit any objective. The floor a match pays.
        /// </summary>
        private static MatchRecord Match(string id, string characterId = "maring",
                                         int rounds = Balance.Rounds, int activeRounds = -2)
        {
            if (activeRounds == -2) activeRounds = rounds;

            var players = new PlayerMatchStats[Balance.PlayerCount];
            for (int i = 0; i < players.Length; i++)
                players[i] = new PlayerMatchStats
                {
                    Slot = i,
                    PlayerId = $"player-{i}",
                    Handle = $"Seat {i}#000{i}",
                    CharacterId = i == 0 ? characterId : "totoy",
                    SlipperId = "tsinelas",
                    Score = 400 - i * 100,
                    ScoreAtFinalRound = 400 - i * 100,
                    ActiveRounds = activeRounds,
                };

            var record = new MatchRecord
            {
                MatchId = id,
                Mode = GameMode.Classic.ToString(),
                MapId = "eskinita",
                Rounds = rounds,
                DurationSeconds = 360.0f,
                PlayedUtc = DateTime.UtcNow.ToString("O"),
                WinningSlot = 0,
                Players = players,
            };

            MatchRecordRules.Normalise(record);
            return record;
        }

        private static PlayerMatchStats Line(MatchRecord record, int slot) => record.Players[slot];

        // -------------------------------------------------------------------
        // § WHAT A MATCH PAYS
        // -------------------------------------------------------------------

        [Fact]
        public void TheBreakdownAddsUpToExactlyWhatTheMatchPays()
        {
            var record = Match("m1");
            var line = Line(record, 0);
            line.Knockdowns = 3;
            line.RetrievalsUnderPressure = 1;
            line.Tags = 2;
            line.Sabotages = 1;

            int summed = 0;
            foreach (var part in ProgressionRules.Breakdown(record, line)) summed += part.Xp;

            Assert.Equal(ProgressionRules.MatchXp(record, line), summed);
        }

        /// <summary>
        /// ⚠️ `FUTURE.md` PHASE 4 AS AN ASSERTION: *"Weight completion heavily and placement
        /// lightly, so leaving is the only thing that costs."* If somebody ever raises the
        /// placement table above completion, winning becomes the point and finishing stops being
        /// it, which is the exact inversion the whole phase is written against.
        /// </summary>
        [Fact]
        public void CompletionPaysMoreThanWinningDoes()
        {
            Assert.True(ProgressionRules.CompletionXp > ProgressionRules.PlacementXp[0],
                "completion must outweigh first place");

            foreach (int placement in ProgressionRules.PlacementXp)
                Assert.True(placement > 0, "every placement pays something");
        }

        /// <summary>
        /// ⚠️ THE LOSER EARNS MOST OF WHAT THE WINNER DOES, AND THAT IS THE ENGINE OF THE PHASE:
        /// *"give a player who just lost a reason to queue again."* The bound is 70 per cent
        /// rather than a specific ratio, so the tables can move without this test becoming a
        /// second copy of them.
        /// </summary>
        [Fact]
        public void FinishingLastStillPaysMostOfWhatWinningPays()
        {
            var record = Match("m2");
            int first = ProgressionRules.MatchXp(record, Line(record, 0));
            int last = ProgressionRules.MatchXp(record, Line(record, 3));

            Assert.True(last >= first * 0.7f, $"last paid {last} against {first} for first");
            Assert.True(last < first, "winning must still be worth something");
        }

        [Fact]
        public void EveryObjectivePaysOnceRatherThanPerEvent()
        {
            var record = Match("m3");
            var once = Line(record, 0);
            once.Knockdowns = 1;

            var many = Line(record, 1);
            many.Knockdowns = 40;

            int extraForOne = ProgressionRules.MatchXp(record, once)
                              - ProgressionRules.PlacementXp[once.Placement - 1];
            int extraForForty = ProgressionRules.MatchXp(record, many)
                                - ProgressionRules.PlacementXp[many.Placement - 1];

            Assert.Equal(extraForOne, extraForForty);
        }

        [Fact]
        public void ThePenaltyFreeObjectiveIsRefusedByEitherPenalty()
        {
            var record = Match("m4");
            var clean = Line(record, 0);
            var camped = Line(record, 1);
            var dropped = Line(record, 2);

            camped.TayaCampPenalties = 1;
            dropped.UnretrievedSlipperPenalties = 1;

            Assert.Contains(ProgressionRules.Breakdown(record, clean),
                            l => l.Label == "NO PENALTIES");
            Assert.DoesNotContain(ProgressionRules.Breakdown(record, camped),
                                  l => l.Label == "NO PENALTIES");
            Assert.DoesNotContain(ProgressionRules.Breakdown(record, dropped),
                                  l => l.Label == "NO PENALTIES");
        }

        [Fact]
        public void ABotIsNeverPaidAndNeverAppearsInABreakdown()
        {
            var record = Match("m5");
            var line = Line(record, 0);
            line.IsBot = true;

            Assert.Equal(0, ProgressionRules.MatchXp(record, line));
            Assert.Empty(ProgressionRules.Breakdown(record, line));
        }

        // -------------------------------------------------------------------
        // § AFK
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE ONE THAT PROTECTS EVERY MATCH EVER PLAYED. `ActiveRounds` is -1 on every
        /// record written before this phase, on every record still sitting in an offline queue,
        /// and on every record sent by a peer running an older build. Reading -1 as "played no
        /// rounds" would mark all of them AFK and strike out accounts for games they played.
        /// </summary>
        [Fact]
        public void AnUnmeasuredRecordIsNeverAfk()
        {
            var record = Match("m6", activeRounds: -1);
            var line = Line(record, 0);

            Assert.Equal(-1, line.ActiveRounds);
            Assert.False(ProgressionRules.WasAfk(record, line));
            Assert.True(ProgressionRules.MatchXp(record, line) > 0);
        }

        [Fact]
        public void MissingOneWholeRoundIsAfkAndPaysNothing()
        {
            var record = Match("m7", activeRounds: Balance.Rounds - 1);
            var line = Line(record, 0);

            Assert.True(ProgressionRules.WasAfk(record, line));
            Assert.Equal(0, ProgressionRules.MatchXp(record, line));

            var breakdown = ProgressionRules.Breakdown(record, line);
            Assert.Single(breakdown);
            Assert.Equal(0, breakdown[0].Xp);
        }

        [Fact]
        public void PlayingEveryRoundIsNotAfk()
        {
            var record = Match("m8", activeRounds: Balance.Rounds);
            Assert.False(ProgressionRules.WasAfk(record, Line(record, 0)));
        }

        /// <summary>
        /// ⚠️ THE MOVEMENT BAR IS DERIVED FROM MOVEMENT AND ROUND LENGTH, NOT PICKED. If somebody
        /// replaces it with a round number this fails, which is the point: `VISION.md` § 4's last
        /// rule is that every number that matters was measured and says what it was measured
        /// against.
        /// </summary>
        [Fact]
        public void TheAfkMovementBarIsDerivedAndIsFarBelowAPlayedRound()
        {
            Assert.Equal(Balance.Speed * Balance.AttackerSpeedScale * ProgressionRules.AfkActiveSeconds,
                         ProgressionRules.AfkRoundMetres, 4);

            // `BotBehaviourProbe` measures live seats at roughly 130 m a round. The bar has to
            // separate "did nothing at all" from "played badly", so it sits an order of
            // magnitude under that.
            Assert.True(ProgressionRules.AfkRoundMetres < 13.0f,
                        $"the bar is {ProgressionRules.AfkRoundMetres} m, which is not 'did nothing'");
            Assert.True(ProgressionRules.AfkRoundMetres > 0.0f);
        }

        // -------------------------------------------------------------------
        // § ESCALATION
        // -------------------------------------------------------------------

        [Fact]
        public void ThreeAfkMatchesEarnASuspensionAndTheNextGoodMatchesPayNothing()
        {
            var profile = new PlayerProfile();

            for (int i = 0; i < ProgressionRules.AfkStrikesBeforePenalty; i++)
            {
                var afk = Match($"afk-{i}", activeRounds: 0);
                Assert.True(ProfileRules.Apply(profile, afk, "player-0", out XpAward award));
                Assert.True(award.Afk);
            }

            Assert.Equal(0, profile.Xp);
            Assert.Equal(ProgressionRules.AfkPenaltyMatches, profile.XpPenaltyMatches);

            for (int i = 0; i < ProgressionRules.AfkPenaltyMatches; i++)
            {
                var good = Match($"served-{i}");
                Assert.True(ProfileRules.Apply(profile, good, "player-0", out XpAward served));
                Assert.True(served.Suspended);
                Assert.Equal(0, profile.Xp);
            }

            var paid = Match("paid");
            Assert.True(ProfileRules.Apply(profile, paid, "player-0", out XpAward last));
            Assert.False(last.Suspended);
            Assert.True(profile.Xp > 0);
        }

        /// <summary>
        /// ⚠️ IF AN AFK MATCH SPENT THE SUSPENSION, THE FASTEST WAY OUT OF ONE WOULD BE TO KEEP
        /// STANDING STILL, which is the opposite of what the rule is for.
        /// </summary>
        [Fact]
        public void AnAfkMatchDoesNotServeTheSuspension()
        {
            var profile = new PlayerProfile { XpPenaltyMatches = 2 };
            var afk = Match("afk-again", activeRounds: 0);

            ProfileRules.Apply(profile, afk, "player-0");

            Assert.Equal(2, profile.XpPenaltyMatches);
        }

        [Fact]
        public void OneCleanMatchClearsTheStrikesRatherThanDecrementingThem()
        {
            var profile = new PlayerProfile();

            ProfileRules.Apply(profile, Match("a", activeRounds: 0), "player-0");
            ProfileRules.Apply(profile, Match("b", activeRounds: 0), "player-0");
            Assert.Equal(2, profile.AfkStrikes);

            ProfileRules.Apply(profile, Match("c"), "player-0");
            Assert.Equal(0, profile.AfkStrikes);
            Assert.Equal(0, profile.XpPenaltyMatches);
        }

        // -------------------------------------------------------------------
        // § THE CURVE
        // -------------------------------------------------------------------

        [Fact]
        public void TheLevelCurveIsFlatAndUncappedAndStartsAtOne()
        {
            Assert.Equal(1, ProgressionRules.LevelForXp(0));
            Assert.Equal(1, ProgressionRules.LevelForXp(-500));
            Assert.Equal(1, ProgressionRules.LevelForXp(ProgressionRules.XpPerLevel - 1));
            Assert.Equal(2, ProgressionRules.LevelForXp(ProgressionRules.XpPerLevel));

            // ⚠️ FLAT MEANS EVERY LEVEL COSTS THE SAME, FOREVER. A rising curve is diminishing
            // returns wearing a different hat, and `FUTURE.md` PHASE 4 cut two of those.
            for (int level = 1; level < 500; level++)
            {
                int atLevel = ProgressionRules.XpPerLevel * (level - 1);
                Assert.Equal(level, ProgressionRules.LevelForXp(atLevel));
                Assert.Equal(level + 1, ProgressionRules.LevelForXp(atLevel + ProgressionRules.XpPerLevel));
            }
        }

        [Fact]
        public void TheBarNeverLeavesTheCurrentLevel()
        {
            for (int xp = 0; xp < ProgressionRules.XpPerLevel * 4; xp += 137)
            {
                int into = ProgressionRules.XpIntoLevel(xp);
                Assert.InRange(into, 0, ProgressionRules.XpPerLevel - 1);
            }
        }

        [Fact]
        public void ANewBorderArrivesEveryFiftyLevelsAndTheFirstOneIsEarned()
        {
            Assert.Equal(0, ProgressionRules.BorderForLevel(1));
            Assert.Equal(0, ProgressionRules.BorderForLevel(ProgressionRules.LevelsPerBorder - 1));
            Assert.Equal(1, ProgressionRules.BorderForLevel(ProgressionRules.LevelsPerBorder));
            Assert.Equal(4, ProgressionRules.BorderForLevel(ProgressionRules.LevelsPerBorder * 4));
        }

        // -------------------------------------------------------------------
        // § MASTERY
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ THE SIX HEROES ONLY, `FUTURE.md` PHASE 4, narrowed from eighteen on 2026-08-31.
        /// The other twelve keep a played count in `PlayerProfile.Characters` and no path.
        /// </summary>
        [Fact]
        public void OnlyTheSixHeroesHaveAMasteryPath()
        {
            Assert.Equal(6, Roster.HeroPeople.Count);

            foreach (var hero in Roster.HeroPeople)
                Assert.True(ProgressionRules.HasMasteryPath(hero.Id), hero.Id);

            foreach (var person in Roster.ClassicPeople)
                if (!ProgressionRules.HasMasteryPath(person.Id))
                    Assert.False(ProgressionRules.HasMasteryPath(person.Id), person.Id);

            Assert.False(ProgressionRules.HasMasteryPath("maring"));
            Assert.False(ProgressionRules.HasMasteryPath(""));
            Assert.False(ProgressionRules.HasMasteryPath(null));
        }

        [Fact]
        public void PlayingAHeroLevelsThatHeroAndNothingElse()
        {
            var profile = new PlayerProfile();
            var record = Match("hero", characterId: "zack");

            ProfileRules.Apply(profile, record, "player-0");

            var zack = ProgressionRules.MasteryFor(profile, "zack");
            Assert.True(zack.Xp > 0);
            Assert.Equal(profile.Xp, zack.Xp);

            foreach (var m in profile.Mastery)
                if (m.Id != "zack") Assert.Equal(0, m.Xp);
        }

        [Fact]
        public void PlayingAStreetCharacterLevelsNoMasteryAtAll()
        {
            var profile = new PlayerProfile();
            ProfileRules.Apply(profile, Match("street", characterId: "maring"), "player-0");

            Assert.True(profile.Xp > 0);
            Assert.Empty(profile.Mastery);
        }

        /// <summary>⚠️ MASTERY IS SLOWER THAN THE ACCOUNT ON PURPOSE, because it is per hero and
        /// there are six of them. A mastery number that moved as fast as the account level would
        /// say nothing about the hero.</summary>
        [Fact]
        public void MasteryCostsMorePerLevelThanTheAccountDoes()
        {
            Assert.True(ProgressionRules.MasteryXpPerLevel > ProgressionRules.XpPerLevel);
            Assert.Equal(1, ProgressionRules.MasteryLevelForXp(0));
            Assert.Equal(2, ProgressionRules.MasteryLevelForXp(ProgressionRules.MasteryXpPerLevel));
        }

        // -------------------------------------------------------------------
        // § IDEMPOTENCY
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE OFFLINE QUEUE RESUBMITS, WHICH IS WHAT IT IS FOR. `ProgressionRules.Award` is
        /// called from inside `ProfileRules.Apply` so the one idempotency guard covers the career
        /// and the XP together; awarding from a second call site would double a level every time
        /// somebody's connection dropped at the wrong moment.
        /// </summary>
        [Fact]
        public void ResubmittingAMatchPaysItOnce()
        {
            var profile = new PlayerProfile();
            var record = Match("once", characterId: "sean");

            Assert.True(ProfileRules.Apply(profile, record, "player-0"));
            int afterFirst = profile.Xp;
            int masteryAfterFirst = ProgressionRules.MasteryFor(profile, "sean").Xp;

            Assert.False(ProfileRules.Apply(profile, record, "player-0"));
            Assert.Equal(afterFirst, profile.Xp);
            Assert.Equal(masteryAfterFirst, ProgressionRules.MasteryFor(profile, "sean").Xp);
        }

        [Fact]
        public void ARefusedApplyHandsBackNoAward()
        {
            var profile = new PlayerProfile();
            var record = Match("kept");

            Assert.True(ProfileRules.Apply(profile, record, "player-0", out XpAward first));
            Assert.NotNull(first);

            Assert.False(ProfileRules.Apply(profile, record, "player-0", out XpAward again));
            Assert.Null(again);
        }

        [Fact]
        public void NormalisingRederivesTheLevelFromTheXpRatherThanTrustingIt()
        {
            var profile = new PlayerProfile { Xp = ProgressionRules.XpPerLevel * 7, Level = 99 };
            ProfileRules.Normalise(profile);

            Assert.Equal(8, profile.Level);
        }

        // -------------------------------------------------------------------
        // § RULE 4: NOTHING ON A TRACK TOUCHES A GAMEPLAY NUMBER
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ `FUTURE.md` § 0.5 RULE 4 AS A TEST, AND IT IS DELIBERATELY STRUCTURAL RATHER
        /// THAN A LIST. *"Nothing on any progression track may change a gameplay number. Write
        /// the test that proves it."* A test that walked today's reward table and checked each
        /// row would pass forever and prove nothing about the row somebody adds next year. This
        /// asserts the shape instead: a `Reward` has no numeric member at all, so it cannot carry
        /// a speed, a radius, a cooldown or a score, and the first person to add one fails here.
        /// </summary>
        [Fact]
        public void ARewardCannotCarryAGameplayNumber()
        {
            var numeric = new HashSet<Type>
            {
                typeof(int), typeof(uint), typeof(long), typeof(ulong),
                typeof(short), typeof(ushort), typeof(byte), typeof(sbyte),
                typeof(float), typeof(double), typeof(decimal),
                typeof(int[]), typeof(float[]),
            };

            foreach (var field in typeof(Reward).GetFields(BindingFlags.Public | BindingFlags.Instance))
                Assert.False(numeric.Contains(field.FieldType),
                             $"Reward.{field.Name} is a {field.FieldType.Name}, which rule 4 forbids");

            foreach (var property in typeof(Reward).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                Assert.False(numeric.Contains(property.PropertyType),
                             $"Reward.{property.Name} is a {property.PropertyType.Name}, which rule 4 forbids");
        }

        /// <summary>
        /// ⚠️ EVERY KIND IS ONE OF THE FOUR `FUTURE.md` § 4.1 SIGNED OFF AS AFFORDABLE: a title,
        /// a badge, a palette and a border. An emote or a tsinelas skin is an art commitment on a
        /// schedule, which is what the season track was cut for.
        /// </summary>
        [Fact]
        public void EveryRewardIsATitleABadgeAPaletteOrABorder()
        {
            var allowed = new HashSet<RewardKind>
            {
                RewardKind.Title, RewardKind.Badge, RewardKind.Palette, RewardKind.Border,
            };

            Assert.Equal(allowed.Count, Enum.GetValues(typeof(RewardKind)).Length);

            foreach (var reward in ProgressionRules.AccountRewards(1000))
                Assert.Contains(reward.Kind, allowed);

            foreach (var hero in Roster.HeroPeople)
                foreach (var reward in ProgressionRules.MasteryRewards(hero.Id, 1000))
                    Assert.Contains(reward.Kind, allowed);
        }

        /// <summary>
        /// ⚠️ WHAT A PLAYER OWNS IS A FUNCTION OF THEIR LEVEL AND IS NOT STORED, so adding a
        /// title at level 30 grants it to everybody already past 30 with no migration and no way
        /// for a document and a table to disagree.
        /// </summary>
        [Fact]
        public void RewardsAreAPureFunctionOfLevelAndOnlyEverGrow()
        {
            int previous = 0;
            for (int level = 1; level <= 250; level++)
            {
                int owned = ProgressionRules.AccountRewards(level).Count;
                Assert.True(owned >= previous, $"level {level} owns fewer rewards than {level - 1}");
                previous = owned;
            }

            Assert.Equal(ProgressionRules.AccountRewards(50).Count,
                         ProgressionRules.AccountRewards(50).Count);
        }

        [Fact]
        public void EveryRewardIdIsUniqueAcrossBothTracks()
        {
            var seen = new HashSet<string>();

            foreach (var reward in ProgressionRules.AccountRewards(1000))
                Assert.True(seen.Add(reward.Id), reward.Id);

            foreach (var hero in Roster.HeroPeople)
                foreach (var reward in ProgressionRules.MasteryRewards(hero.Id, 1000))
                    Assert.True(seen.Add(reward.Id), reward.Id);
        }

        [Fact]
        public void ARewardEarnedOnTheWayUpIsReportedOnce()
        {
            var profile = new PlayerProfile { Xp = ProgressionRules.XpPerLevel * 3 };
            var record = Match("climb");

            // Land exactly on a level that pays, by topping up to one XP short of it.
            int target = ProgressionRules.AccountRewardLevels()[0];
            profile.Xp = ProgressionRules.XpPerLevel * (target - 1) - 1;

            ProfileRules.Apply(profile, record, "player-0", out XpAward award);

            Assert.Equal(target, award.LevelAfter);
            Assert.Single(award.Unlocked);
        }

        // -------------------------------------------------------------------
        // § THE SERVER COPY
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE CLOUD CODE SCRIPT CARRIES A SECOND COPY OF THE HERO LIST AND OF EVERY XP
        /// CONSTANT, AND THERE IS NO WAY AROUND IT. Cloud Code cannot import the C#, which is the
        /// same trade `player-account.js` records about `DisplayNameMax` and `match-record.js`
        /// records about the whole of `ProfileRules`. This reads the deployed source as text and
        /// fails if the two halves have drifted, which is the only signal that exists: a
        /// disagreement produces a well-formed answer with the wrong numbers in it, exactly as
        /// `docs/TODO.md` § 90.5 records.
        /// </summary>
        [Fact]
        public void TheServerScriptAgreesWithTheCoreAboutHeroesAndRates()
        {
            string script = ReadRepoFile(Path.Combine("ugs", "cloud-code", "match-record.js"));

            foreach (var hero in Roster.HeroPeople)
                Assert.Contains($"\"{hero.Id}\"", script);

            Assert.Contains($"const COMPLETION_XP = {ProgressionRules.CompletionXp};", script);
            Assert.Contains($"const XP_PER_LEVEL = {ProgressionRules.XpPerLevel};", script);
            Assert.Contains($"const MASTERY_XP_PER_LEVEL = {ProgressionRules.MasteryXpPerLevel};", script);
            Assert.Contains($"const OBJECTIVE_KNOCKDOWN_XP = {ProgressionRules.ObjectiveKnockdownXp};", script);
            Assert.Contains($"const OBJECTIVE_PRESSURE_RETRIEVAL_XP = {ProgressionRules.ObjectivePressureRetrievalXp};", script);
            Assert.Contains($"const OBJECTIVE_TAG_XP = {ProgressionRules.ObjectiveTagXp};", script);
            Assert.Contains($"const OBJECTIVE_SABOTAGE_XP = {ProgressionRules.ObjectiveSabotageXp};", script);
            Assert.Contains($"const OBJECTIVE_CLEAN_XP = {ProgressionRules.ObjectiveCleanXp};", script);
            Assert.Contains($"const AFK_STRIKES_BEFORE_PENALTY = {ProgressionRules.AfkStrikesBeforePenalty};", script);
            Assert.Contains($"const AFK_PENALTY_MATCHES = {ProgressionRules.AfkPenaltyMatches};", script);

            string placements = string.Join(", ", ProgressionRules.PlacementXp);
            Assert.Contains($"const PLACEMENT_XP = [{placements}];", script);
        }

        /// <summary>
        /// ⚠️ THE PATH IS WALKED UP RATHER THAN GUESSED, because the test assembly runs from
        /// `Core.Tests/bin/Debug/net9.0` and a relative path from there is three levels of
        /// somebody else's build layout. It asserts the file was FOUND: a test that quietly
        /// passes when it cannot find what it is checking is worse than no test.
        /// </summary>
        private static string ReadRepoFile(string relative)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);

            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(candidate)) return File.ReadAllText(candidate);
                dir = dir.Parent;
            }

            Assert.Fail($"could not find {relative} above {AppContext.BaseDirectory}");
            return "";
        }
    }
}
