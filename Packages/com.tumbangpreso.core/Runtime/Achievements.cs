using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    public enum AchievementTier
    {
        Bronze = 0,
        Silver = 1,
        Gold = 2
    }

    public sealed class Achievement
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public AchievementTier Tier { get; set; }
        public int TargetCount { get; set; }
        public RewardKind RewardKind { get; set; }
        public string RewardId { get; set; }
        public string RewardLabel { get; set; }

        public Achievement(string id, string title, string description, AchievementTier tier,
            int target, RewardKind rKind, string rId, string rLabel)
        {
            Id = id;
            Title = title;
            Description = description;
            Tier = tier;
            TargetCount = target;
            RewardKind = rKind;
            RewardId = rId;
            RewardLabel = rLabel;
        }
    }

    public static class AchievementRules
    {
        public static readonly List<Achievement> Catalog = new List<Achievement>
        {
            // -------------------------------------------------------------
            // BRONZE TIER (Street Basics & Rookie Milestones)
            // -------------------------------------------------------------
            new Achievement("ach.unang_tumba", "Unang Tumba", "Knock down the lata for the very first time.",
                AchievementTier.Bronze, 1, RewardKind.Title, "title.unang_tumba", "BATANG BAGUHAN"),

            new Achievement("ach.larong_kalye", "Larong Kalye", "Complete 5 matches in any game mode.",
                AchievementTier.Bronze, 5, RewardKind.Badge, "badge.larong_kalye", "STREET ROOKIE"),

            new Achievement("ach.dobleng_laro", "Dobleng Laro", "Play at least one match in Classic and Hero Strike.",
                AchievementTier.Bronze, 2, RewardKind.Title, "title.dobleng_laro", "VERSATILE"),

            new Achievement("ach.unang_panalo", "Unang Tagumpay", "Win your first match in Tumbang Preso.",
                AchievementTier.Bronze, 1, RewardKind.Badge, "badge.unang_panalo", "FIRST BLOOD"),

            new Achievement("ach.tulong_tropa", "Tropa sa Kanto", "Add your first friend to your social list.",
                AchievementTier.Bronze, 1, RewardKind.Title, "title.tropa", "TROPA"),

            // -------------------------------------------------------------
            // SILVER TIER (District Competitor & Seasoned Slipper Fighter)
            // -------------------------------------------------------------
            new Achievement("ach.bantay_lata", "Bantay ng Lata", "Record 50 total lata knockdowns.",
                AchievementTier.Silver, 50, RewardKind.Title, "title.bantay_lata", "LATA HUNTER"),

            new Achievement("ach.salisi_master", "Salisi Master", "Successfully retrieve your tsinelas 25 times from the circle.",
                AchievementTier.Silver, 25, RewardKind.Badge, "badge.salisi_master", "SLIPPER THIEF"),

            new Achievement("ach.hero_squad", "Barangay Squad", "Play at least 3 matches on all 6 canonical heroes.",
                AchievementTier.Silver, 6, RewardKind.Title, "title.barangay_squad", "ALL-AROUND"),

            new Achievement("ach.sampu_panalo", "Sampu sa Kalye", "Win 10 matches in Hero Strike mode.",
                AchievementTier.Silver, 10, RewardKind.Badge, "badge.sampu_panalo", "HERO STRIKER"),

            new Achievement("ach.level_sampo", "Bihasang Manlalaro", "Reach account Level 10.",
                AchievementTier.Silver, 10, RewardKind.Border, "border.silver_street", "SILVER TIMBER BORDER"),

            // -------------------------------------------------------------
            // GOLD TIER (Barangay Legend & Street Champion)
            // -------------------------------------------------------------
            new Achievement("ach.kampeon_rank", "Kampeon ng Barangay", "Reach Kampeon (Tier 3) or higher in competitive Ranked.",
                AchievementTier.Gold, 1, RewardKind.Badge, "badge.kampeon_gold", "GOLD CHAMPION CREST"),

            new Achievement("ach.isang_daan", "Isang Daan Tumba", "Accumulate 100 total career lata knockdowns.",
                AchievementTier.Gold, 100, RewardKind.Title, "title.isang_daan", "CENTURY STRIKER"),

            new Achievement("ach.walang_mintis", "Walang Mintis", "Achieve a 5-match winning streak.",
                AchievementTier.Gold, 5, RewardKind.Title, "title.walang_mintis", "UNTOUCHABLE"),

            new Achievement("ach.dalubhasa_hero", "Dalubhasa sa Hero", "Reach Level 10 Hero Mastery on any canonical hero.",
                AchievementTier.Gold, 10, RewardKind.Border, "border.gold_sunburst", "GOLD SUNBURST BORDER"),

            new Achievement("ach.alamat_bayan", "Alamat ng Bayan", "Achieve Alamat (Tier 4) peak competitive rank.",
                AchievementTier.Gold, 1, RewardKind.Title, "title.alamat_bayan", "LIVING LEGEND")
        };

        public static List<Achievement> Tier(AchievementTier tier)
        {
            return Catalog.FindAll(a => a.Tier == tier);
        }

        public static Achievement ById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return Catalog.Find(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public static int ProgressFor(Achievement achievement, PlayerProfile profile)
        {
            if (achievement == null || profile == null) return 0;

            var classic = ProfileRules.ModeFor(profile, "Classic")?.Totals ?? new CareerTotals();
            var heroStrike = ProfileRules.ModeFor(profile, "HeroStrike")?.Totals ?? new CareerTotals();

            switch (achievement.Id)
            {
                case "ach.unang_tumba":
                case "ach.bantay_lata":
                case "ach.isang_daan":
                    return classic.Knockdowns + heroStrike.Knockdowns;

                case "ach.larong_kalye":
                    return classic.Matches + heroStrike.Matches;

                case "ach.unang_panalo":
                    return classic.Wins + heroStrike.Wins;

                case "ach.dobleng_laro":
                    int cPlayed = classic.Matches > 0 ? 1 : 0;
                    int hPlayed = heroStrike.Matches > 0 ? 1 : 0;
                    return cPlayed + hPlayed;

                case "ach.sampu_panalo":
                    return heroStrike.Wins;

                case "ach.level_sampo":
                    return ProgressionRules.LevelForXp(profile.Xp);

                case "ach.kampeon_rank":
                    if (profile.Rank == null || profile.Rank.MatchesThisSeason == 0) return 0;
                    return (int)RatingRules.TierFor(profile.Rank.Rating) >= (int)RankTier.Kampeon ? 1 : 0;

                case "ach.alamat_bayan":
                    if (profile.Rank == null || profile.Rank.MatchesThisSeason == 0) return 0;
                    return (int)RatingRules.TierFor(profile.Rank.Rating) >= (int)RankTier.Alamat ? 1 : 0;

                default:
                    return 0;
            }
        }

        public static bool IsUnlocked(Achievement achievement, PlayerProfile profile)
        {
            return ProgressFor(achievement, profile) >= achievement.TargetCount;
        }
    }
}
