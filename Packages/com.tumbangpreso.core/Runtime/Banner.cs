using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// What a player has chosen to wear next to their name.
    ///
    /// ⚠️⚠️ ONE OBJECT, NOT SEVEN SLOTS, AND `FUTURE.md` PHASE 5 CUT THE OTHER SIX BY NAME. An
    /// earlier version of that phase listed a nameplate, a title, a badge, an emblem, a frame, a
    /// border, a mastery number and an avatar as SEPARATE cosmetic slots, each with its own
    /// inventory category, its own UI row and its own wire field. **They all do the same job:
    /// they say who you are next to your name.** So there is one banner, everything that used to
    /// be a slot is a field on it, and there is one object to author, one to replicate and one to
    /// earn things for.
    ///
    /// ⚠️⚠️ STRING IDS, NEVER INDICES, AND THIS IS THE LAST CHEAP MOMENT TO DECIDE IT.
    /// `FUTURE.md` PHASE 5 is explicit and `Roster.Slippers` records at length what inserting a
    /// row into a wire-facing list costs: every peer resolves these ids, so an index that shifts
    /// dresses somebody else's character. A few extra bytes removes the whole class permanently.
    ///
    /// ⚠️ IT CARRIES NO NUMBER, FOR THE REASON `Reward` CARRIES NONE. `FUTURE.md` § 0.5 rule 4:
    /// nothing on a progression track may change a gameplay number.
    /// `BannerTests.ABannerCannotCarryAGameplayNumber` walks this type by reflection, the same
    /// way `ProgressionTests.ARewardCannotCarryAGameplayNumber` walks that one, because the rule
    /// is only worth anything if adding a field in a hurry fails a test.
    /// </summary>
    [Serializable]
    public sealed class BannerSelection
    {
        public string TitleId = "";
        public string BadgeId = "";
        public string BorderId = "";
        public string PaletteId = "";

        /// <summary>
        /// The three numbers this player wants shown beside their name.
        ///
        /// ⚠️ `INSPIRATION.md` § 2.5 IS THE ARGUMENT AND IT IS THE CHEAPEST STATUS IN THE GAME.
        /// Three chosen stat trackers and a title next to your name in a lobby buys more status
        /// per hour of work than any model in the project, because status is text and a number
        /// and always has been.
        /// </summary>
        public string[] Trackers = Array.Empty<string>();
    }

    /// <summary>
    /// What a player may wear, and what happens to a choice they have not earned.
    ///
    /// ⚠️⚠️ THE SERVER RUNS THIS TOO, WHICH IS WHY IT IS IN THE CORE AND WHY IT IS A PURE
    /// FUNCTION. `FUTURE.md` § 0.5 rule 6: a client never writes what it owns. A banner arrives
    /// from a peer as four ids and a list, and the only safe way to draw it is to ask this what
    /// that player had actually earned. **`Normalise` is the whole security model of cosmetics**,
    /// and it is deliberately the same call on both sides.
    ///
    /// ⚠️ NOTHING IS STORED THAT CAN BE DERIVED. `ProgressionRules.AccountRewards` already makes
    /// "what have I earned" a pure function of level, so there is no inventory document, no
    /// migration when the table grows, and no way for a stored inventory and the table to
    /// disagree. That property is worth keeping and this file does not break it.
    /// </summary>
    public static class BannerRules
    {
        /// <summary>
        /// How many stat trackers a banner shows.
        ///
        /// ⚠️ THREE, AND THE NUMBER IS THE POINT RATHER THAN A LIMIT TO RAISE LATER. A banner
        /// showing everything says nothing; the choice of WHICH three is the expression. Raising
        /// this turns a statement into a dump, which is § 92.1 fault 4 one screen over.
        /// </summary>
        public const int TrackerSlots = 3;

        /// <summary>
        /// The stat trackers a banner may show, by id.
        ///
        /// ⚠️⚠️ EVERY ONE OF THESE IS A NUMBER THE CAREER TAB ALREADY DERIVES, AND THAT IS A
        /// HARD REQUIREMENT RATHER THAN A CONVENIENCE. A tracker that needs a new stored counter
        /// is a new field on every profile document and a new thing to migrate; a tracker that
        /// reads `ProfileRules` is free forever. **If a tracker cannot be computed from
        /// `CareerTotals`, it is not a tracker, it is a feature request.**
        ///
        /// ⚠️ AND A RATE ONLY APPEARS WHEN ITS SAMPLE CAN CARRY IT. `ProfileRules.IsReportable`
        /// and `FUTURE.md` § 2.2: do not show a stat you will not defend. A banner is the most
        /// public surface in the game, so a win rate over three matches would be an argument in
        /// every lobby.
        /// </summary>
        public static readonly string[] TrackerIds =
        {
            "matches",
            "wins",
            "win_rate",
            "knockdowns",
            "retrievals",
            "tags",
            "longest_streak",
            "hours",
        };

        public static bool IsTracker(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            foreach (var known in TrackerIds)
                if (known == id) return true;
            return false;
        }

        /// <summary>
        /// Everything this profile has earned: the account track plus every hero's mastery track.
        ///
        /// ⚠️ MASTERY IS INCLUDED AND IT IS THE HALF THAT MAKES THE BANNER WORTH WEARING. Account
        /// level is the same climb for everybody; a hero title is a statement about what you
        /// play. `ProgressionRules.MasteryRewards` is already per hero and already only exists
        /// for the six.
        /// </summary>
        public static List<Reward> Earned(PlayerProfile profile)
        {
            var found = new List<Reward>();
            if (profile == null) return found;

            found.AddRange(ProgressionRules.AccountRewards(
                ProgressionRules.LevelForXp(profile.Xp)));

            if (profile.Mastery == null) return found;

            foreach (var mastery in profile.Mastery)
            {
                if (mastery == null || string.IsNullOrEmpty(mastery.Id)) continue;
                found.AddRange(ProgressionRules.MasteryRewards(mastery.Id, mastery.Level));
            }

            return found;
        }

        /// <summary>Whether this profile has earned a specific reward.</summary>
        public static bool Owns(PlayerProfile profile, RewardKind kind, string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            foreach (var reward in Earned(profile))
                if (reward != null && reward.Kind == kind && reward.Id == id) return true;

            return false;
        }

        /// <summary>
        /// The banner this profile is actually allowed to wear, with everything else dropped.
        ///
        /// ⚠️⚠️ IT DROPS RATHER THAN REFUSES, AND THE DIFFERENCE MATTERS ON THE RECEIVING END.
        /// A peer sends four ids; if one of them is a title they never earned, refusing the whole
        /// banner would let one bad field blank a legitimate one and would give a griefer a way
        /// to make somebody else's banner disappear. **Dropping the field is the smallest correct
        /// answer**: the honest parts still draw.
        ///
        /// ⚠️ AND AN EMPTY SELECTION IS LEGAL. A player who has earned nothing, or who wants
        /// nothing shown, has a banner with their handle on it and no decoration. That is the
        /// state every account starts in and it has to look deliberate rather than broken.
        ///
        /// ⚠️ IT NEVER RETURNS NULL, so no caller has to null-check a cosmetic.
        /// </summary>
        public static BannerSelection Normalise(PlayerProfile profile, BannerSelection selection)
        {
            var clean = new BannerSelection();
            if (selection == null) return clean;

            if (Owns(profile, RewardKind.Title, selection.TitleId))
                clean.TitleId = selection.TitleId;

            if (Owns(profile, RewardKind.Badge, selection.BadgeId))
                clean.BadgeId = selection.BadgeId;

            if (Owns(profile, RewardKind.Border, selection.BorderId))
                clean.BorderId = selection.BorderId;

            if (Owns(profile, RewardKind.Palette, selection.PaletteId))
                clean.PaletteId = selection.PaletteId;

            clean.Trackers = CleanTrackers(selection.Trackers);
            return clean;
        }

        /// <summary>
        /// ⚠️ DUPLICATES ARE DROPPED, NOT COUNTED. Three slots holding the same tracker is a
        /// banner that says one thing three times, and it is what a UI with three identical
        /// dropdowns produces by default.
        /// </summary>
        private static string[] CleanTrackers(string[] wanted)
        {
            if (wanted == null || wanted.Length == 0) return Array.Empty<string>();

            var kept = new List<string>(TrackerSlots);

            foreach (var id in wanted)
            {
                if (kept.Count >= TrackerSlots) break;
                if (!IsTracker(id)) continue;
                if (kept.Contains(id)) continue;
                kept.Add(id);
            }

            return kept.ToArray();
        }
    }
}
