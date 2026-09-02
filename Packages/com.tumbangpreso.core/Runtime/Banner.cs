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

            foreach (var mastery in profile.Mastery ?? new List<MasteryRecord>())
            {
                if (mastery == null || string.IsNullOrEmpty(mastery.Id)) continue;
                found.AddRange(ProgressionRules.MasteryRewards(mastery.Id, mastery.Level));
            }

            // Achievement rewards are derived from the same career totals as the shelf. There
            // is no inventory row to drift from the visible EARNED state.
            found.AddRange(AchievementRules.EarnedRewards(profile));

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

        /// <summary>
        /// What a peer is actually allowed to wear, given only what it told the room.
        ///
        /// ⚠️⚠️ THIS IS THE RECEIVING HALF OF COSMETICS AND IT RUNS ON THE HOST, ONCE, RATHER
        /// THAN ON EVERY PEER. `docs/TODO.md` § 98.2 step 2 said `Normalise` would run *"on the
        /// RECEIVING side against the sender's profile"*, and the host IS the receiving side:
        /// it takes the claim, authorises it here, and broadcasts the RESULT in the seat table.
        /// **That correction is worth more than the wording it replaces**, for three reasons this
        /// repository has already paid for:
        /// - Everybody in the room draws the same banner, because one machine decided it. Four
        ///   peers each normalising their own copy is four answers to one question, which is the
        ///   shape `docs/TODO.md` § 94.1 records four hand-written copies of.
        /// - It is the rule the rest of the lobby already follows. `LobbySeatInfo.Ready` carries
        ///   the same note: *"a peer never writes its own readiness into a table; it presses and
        ///   the host decides what the table says"*, and § 54 records what trusting a peer-written
        ///   field cost.
        /// - The claim's authorising numbers stop at the host. Only four ids and a palette go out
        ///   to the room, so a peer cannot read anybody else's XP off the wire.
        ///
        /// ⚠️⚠️ AND THE HONEST LIMIT, WRITTEN DOWN RATHER THAN IMPLIED: A MODIFIED CLIENT CAN
        /// CLAIM A LEVEL IT HAS NOT REACHED AND WEAR A TITLE EARLY. There is no server in a LAN
        /// match, the host is a player (`FUTURE.md` § 8.1), and asking the account endpoint per
        /// peer per cosmetic would put a network round trip in front of a lobby that must work
        /// with the cable out (`docs/TODO.md` § 97). **The claim is checked for CONSISTENCY, not
        /// for truth**: a peer cannot wear something that no level would ever grant, and nothing
        /// a banner carries can change a match. `FUTURE.md` § 0.5 rule 4 is what makes that
        /// acceptable: a cosmetic cannot move a gameplay number, so the worst case is somebody
        /// lying about their reading age.
        /// </summary>
        public static BannerSelection Authorise(BannerClaim claim)
        {
            if (claim == null) return new BannerSelection();
            return Normalise(claim.AsProfile(), claim.Banner);
        }

        /// <summary>
        /// The character palette this peer may actually be drawn in.
        ///
        /// ⚠️ IT GOES THROUGH `LoadoutRules` FOR THE REASON THAT FILE GIVES: the same ownership
        /// question, asked once. A palette worn on a character and a palette worn on a banner are
        /// the same earned object, and two checks that could disagree is the fault this phase is
        /// deliberately not repeating.
        /// </summary>
        public static string AuthorisePalette(BannerClaim claim, string characterId)
        {
            if (claim == null) return PaletteRules.DefaultId;
            return LoadoutRules.PaletteFor(claim.AsProfile(), characterId, claim.PaletteId);
        }

        /// <summary>
        /// The whole look a peer may wear, authorised once by the host and broadcast as a result.
        ///
        /// ⚠️⚠️ THE HOST DECIDES AND EVERY RECEIVER DRAWS THE ANSWER, which is
        /// `docs/TODO.md` § 101's correction to § 98.2 applied to the second half of the same
        /// object. One machine runs the ownership check; the authorising XP and mastery never
        /// leave it; everybody in the room draws the same character.
        /// </summary>
        public static CharacterLook AuthoriseLook(BannerClaim claim, string characterId)
        {
            if (claim == null) return CharacterLook.Default;

            var wanted = new CharacterLook(claim.PaletteId, claim.HueDegrees, claim.SaturationPercent);
            return LoadoutRules.LookFor(claim.AsProfile(), characterId, wanted);
        }
    }

    /// <summary>
    /// What one peer tells the room about how it wants to be drawn, plus exactly the facts that
    /// authorise it.
    ///
    /// ⚠️⚠️ THE AUTHORISING FACTS TRAVEL WITH THE CLAIM BECAUSE NOTHING ELSE ON THE WIRE CARRIES
    /// THEM. `BannerRules.Earned` is a pure function of a profile's XP and its mastery levels, so
    /// a receiver holding neither cannot tell an earned title from an invented one, and the peer
    /// it would have to ask is the peer making the claim. **Sending the two numbers is what turns
    /// "trust the ids" into "check the ids against something".**
    ///
    /// ⚠️ IT IS DELIBERATELY NOT A `PlayerProfile`. That document is 30 fields, a match history
    /// and a replay window, and none of it has any business crossing a lobby: a type that carries
    /// a career is a type somebody eventually reads a career out of. This carries the two things
    /// `Earned` reads and nothing else, which is also why <see cref="AsProfile"/> can be a shell.
    ///
    /// ⚠️ THE PALETTE RIDES WITH IT RATHER THAN SEPARATELY, because it is authorised by the same
    /// ownership question and would otherwise be a second wire field checked by a second rule.
    /// </summary>
    [Serializable]
    public sealed class BannerClaim
    {
        public BannerSelection Banner = new BannerSelection();

        /// <summary>The palette this peer is wearing on the character it picked.</summary>
        public string PaletteId = "";

        /// <summary>
        /// The free colour dial this peer has turned on that character.
        ///
        /// ⚠️⚠️ THEY ARE CLAIMED AND CLAMPED RATHER THAN CLAIMED AND CHECKED, AND THAT IS
        /// THE WHOLE DIFFERENCE BETWEEN AN EARNED COSMETIC AND AN EXPRESSIVE ONE. A palette id is
        /// a reward, so the receiver asks whether it was earned; a hue is a preference, so the
        /// only thing that can be wrong with it is that it is out of range.
        /// `CharacterLoadout.HueDegrees` has the reasoning and
        /// <see cref="PaletteRules.SaturationMin"/> has the bound the receiver applies.
        ///
        /// ⚠️ A MODIFIED CLIENT CANNOT PLAY AS A SHADOW. `LoadoutRules.LookFor` runs on the
        /// RECEIVING side too, so a saturation of zero on the wire is drawn at the floor.
        /// </summary>
        public int HueDegrees;
        public int SaturationPercent = 100;

        /// <summary>Claimed account XP. `ProgressionRules.LevelForXp` turns it into the level the
        /// account rewards are derived from.</summary>
        public int Xp;

        /// <summary>Claimed mastery levels, one per hero this peer has played.</summary>
        public MasteryRecord[] Mastery = Array.Empty<MasteryRecord>();

        /// <summary>
        /// The claim as the shape `BannerRules.Earned` reads.
        ///
        /// ⚠️ A FRESH SHELL EVERY CALL AND NEVER A CACHED ONE. It is two field copies and a list,
        /// and a cached profile is a profile somebody hands to something that writes to it.
        /// </summary>
        public PlayerProfile AsProfile()
        {
            var profile = new PlayerProfile { Xp = Math.Max(0, Xp) };

            if (Mastery == null) return profile;

            foreach (var record in Mastery)
                if (record != null && !string.IsNullOrEmpty(record.Id))
                    profile.Mastery.Add(record);

            return profile;
        }
    }

    /// <summary>
    /// The banner as a string, because the wire carries one field rather than twelve.
    ///
    /// ⚠️⚠️ ONE FIELD, AND THE ALTERNATIVE IS WHY. `MatchRpc.IdentifyServerRpc` already takes
    /// seven parameters that are read back in order, and its own header records what that costs:
    /// *"a peer writing five where the host reads seven misreads every field after the third"*.
    /// A banner is four ids, three trackers, a palette, an XP figure and up to six mastery pairs,
    /// which is **eighteen more chances to write the halves out of step**. `audit_wire_payloads.py`
    /// compares a writer to its reader field by field, so one field is one thing for it to check.
    ///
    /// ⚠️⚠️ AND IT IS NOT JSON, BECAUSE THE CORE MAY NOT SEE `UnityEngine`. `CLAUDE.md` § 4:
    /// `Packages/com.tumbangpreso.core/` is engine-free, so `JsonUtility` is not available to the
    /// half of this that both machines must agree on. Hand-rolled and tested beats a serialiser
    /// that only one side of the line can call.
    ///
    /// ⚠️⚠️ A FIELD CONTAINING A SEPARATOR IS DROPPED, NOT ESCAPED, AND THAT IS A DELIBERATE
    /// TRADE. Every id in this game is authored: reward ids are lowercase dotted
    /// (`title.rising`, `palette.alt1`), tracker ids are a fixed list in `TrackerIds`, and hero
    /// ids come from `Roster`. **None of them can contain `|` or `^` without somebody typing one
    /// on purpose**, so escaping would be machinery for a case that cannot arise, and dropping
    /// degrades exactly the way `Normalise` already does: the honest fields still draw.
    /// `BannerTests.AnIdCarryingASeparatorIsDroppedRatherThanCorruptingTheRest`.
    /// </summary>
    public static class BannerCodec
    {
        private const char Field = '|';
        private const char Item = '^';

        /// <summary>Whether this id may be written without breaking the frame. See the header.</summary>
        public static bool IsWritable(string id)
            => id == null || (id.IndexOf(Field) < 0 && id.IndexOf(Item) < 0);

        private static string Safe(string id)
            => IsWritable(id) ? (id ?? "") : "";

        /// <summary>What a peer WEARS: four ids and the trackers. No claim, no numbers.</summary>
        public static string EncodeSelection(BannerSelection selection)
        {
            if (selection == null) return "";

            string trackers = "";
            if (selection.Trackers != null)
                foreach (var id in selection.Trackers)
                {
                    if (!IsWritable(id) || string.IsNullOrEmpty(id)) continue;
                    if (trackers.Length > 0) trackers += Item;
                    trackers += id;
                }

            return string.Join(Field.ToString(), new[]
            {
                Safe(selection.TitleId),
                Safe(selection.BadgeId),
                Safe(selection.BorderId),
                Safe(selection.PaletteId),
                trackers,
            });
        }

        /// <summary>
        /// ⚠️ IT NEVER THROWS AND NEVER RETURNS NULL. A short, empty or malformed frame is a peer
        /// with no banner, which is a legal state every account starts in. `Roster.Slippers`
        /// records the rule for wire-facing ids: an id that does not resolve degrades rather than
        /// blanking, and a frame that does not parse is the same question one level up.
        /// </summary>
        public static BannerSelection DecodeSelection(string encoded)
        {
            var selection = new BannerSelection();
            if (string.IsNullOrEmpty(encoded)) return selection;

            var parts = encoded.Split(Field);

            if (parts.Length > 0) selection.TitleId = parts[0];
            if (parts.Length > 1) selection.BadgeId = parts[1];
            if (parts.Length > 2) selection.BorderId = parts[2];
            if (parts.Length > 3) selection.PaletteId = parts[3];

            if (parts.Length > 4 && parts[4].Length > 0)
                selection.Trackers = parts[4].Split(Item);

            return selection;
        }

        /// <summary>What a peer CLAIMS: the selection, the palette, and the two facts that
        /// authorise them.</summary>
        public static string EncodeClaim(BannerClaim claim)
        {
            if (claim == null) return "";

            string mastery = "";
            if (claim.Mastery != null)
                foreach (var record in claim.Mastery)
                {
                    if (record == null || string.IsNullOrEmpty(record.Id)) continue;
                    if (!IsWritable(record.Id)) continue;
                    if (mastery.Length > 0) mastery += Item;
                    mastery += record.Id + ":" + record.Level.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
                }

            // ⚠️ THE SELECTION IS ENCODED WITH `Item` AS ITS SEPARATOR SO IT CAN NEST. Reusing
            // `EncodeSelection` here would put `Field` characters inside a `Field`-delimited
            // frame, which is the classic way a hand-rolled format eats itself.
            var selection = claim.Banner ?? new BannerSelection();
            string trackers = "";
            if (selection.Trackers != null)
                foreach (var id in selection.Trackers)
                {
                    if (!IsWritable(id) || string.IsNullOrEmpty(id)) continue;
                    if (trackers.Length > 0) trackers += ",";
                    trackers += id;
                }

            return string.Join(Field.ToString(), new[]
            {
                Safe(selection.TitleId),
                Safe(selection.BadgeId),
                Safe(selection.BorderId),
                Safe(selection.PaletteId),
                trackers,
                Safe(claim.PaletteId),
                Math.Max(0, claim.Xp).ToString(System.Globalization.CultureInfo.InvariantCulture),
                mastery,

                // ⚠️⚠️ THE TWO DIAL FIELDS ARE APPENDED AT THE END AND NEVER INSERTED,
                // WHICH IS `Roster.Slippers`' RULE APPLIED TO A FRAME RATHER THAN TO A LIST. A
                // build that has never heard of them reads the seven fields it knows and stops,
                // and `DecodeClaim` answers the default for anything past the end of what it was
                // sent. Inserting a field in the middle would silently shift the mastery list one
                // place and dress the whole room from the wrong data with nothing logged.
                PaletteRules.ClampHue(claim.HueDegrees).ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                PaletteRules.ClampSaturation(claim.SaturationPercent).ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
            });
        }

        /// <summary>⚠️ NEVER THROWS. See <see cref="DecodeSelection"/>.</summary>
        public static BannerClaim DecodeClaim(string encoded)
        {
            var claim = new BannerClaim();
            if (string.IsNullOrEmpty(encoded)) return claim;

            var parts = encoded.Split(Field);

            if (parts.Length > 0) claim.Banner.TitleId = parts[0];
            if (parts.Length > 1) claim.Banner.BadgeId = parts[1];
            if (parts.Length > 2) claim.Banner.BorderId = parts[2];
            if (parts.Length > 3) claim.Banner.PaletteId = parts[3];
            if (parts.Length > 4 && parts[4].Length > 0) claim.Banner.Trackers = parts[4].Split(',');
            if (parts.Length > 5) claim.PaletteId = parts[5];

            if (parts.Length > 6 && int.TryParse(parts[6],
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int xp))
                claim.Xp = Math.Max(0, xp);

            if (parts.Length > 7 && parts[7].Length > 0)
            {
                var rows = parts[7].Split(Item);
                var records = new List<MasteryRecord>(rows.Length);

                foreach (var row in rows)
                {
                    int split = row.LastIndexOf(':');
                    if (split <= 0 || split >= row.Length - 1) continue;

                    if (!int.TryParse(row.Substring(split + 1),
                            System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture, out int level))
                        continue;

                    records.Add(new MasteryRecord
                    {
                        Id = row.Substring(0, split),
                        Level = Math.Max(1, level),
                    });
                }

                claim.Mastery = records.ToArray();
            }

            // ⚠️ AN OLDER FRAME STOPS HERE AND ANSWERS THE AUTHORED COLOURS, which is the
            // same degradation an unknown palette id already gets.
            if (parts.Length > 8 && int.TryParse(parts[8],
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int hue))
                claim.HueDegrees = PaletteRules.ClampHue(hue);

            if (parts.Length > 9 && int.TryParse(parts[9],
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int saturation))
                claim.SaturationPercent = PaletteRules.ClampSaturation(saturation);

            return claim;
        }
    }
}
