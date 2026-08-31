using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// One option in one ability slot: a sidegrade at the same budget, not an upgrade.
    ///
    /// ⚠️⚠️ EVERY VARIANT IS BUDGET NEUTRAL AND `HeroLoadoutTests` ASSERTS IT.
    /// `docs/FUTURE.md` PHASE 10: *"Every option is a sidegrade at the same ability budget.
    /// Nothing unlocks more damage, range, duration or a shorter cooldown. A test asserts it."*
    /// A variant raises exactly one parameter and lowers another by the same fraction, so
    /// `Gain + Cost` is zero on every non-default row. **Without the test this is a comment, and a
    /// comment cannot stop the next person adding a strictly better option.**
    ///
    /// ⚠️⚠️ THE CHALLENGE MUST BE COMPLETABLE AGAINST BOTS, WHICH IS THE RULE THAT MAKES THE
    /// WHOLE SYSTEM SAFE IN A RANKED GAME. `FUTURE.md` PHASE 10 and `INSPIRATION.md` § 5.4: the
    /// gate costs time spent learning a character, never matches won against people, so nobody has
    /// to grind ranked to be equipped for ranked. `PracticeSafe` is that rule as a field rather
    /// than as a sentence, and it is asserted too.
    /// </summary>
    public sealed class AbilityVariant
    {
        public string Id { get; }
        public string HeroId { get; }

        /// <summary>1 for skill one, 2 for skill two. ⚠️ THE ULTIMATE HAS NO VARIANTS AND THAT IS
        /// DELIBERATE: an ultimate is banked once or twice a match and reading which one an
        /// opponent has is already a skill (`docs/VISION.md` § 1.1). Two readings of the same
        /// ultimate would make the tell unreliable rather than deeper.</summary>
        public int Slot { get; }

        /// <summary>The name of the ability this slot actually holds, from the hero kit.</summary>
        public string BaseAbility { get; }

        /// <summary>
        /// The glyph, as the NAME of an `AbilityGlyph` value.
        ///
        /// ⚠️ A STRING BECAUSE `AbilityGlyph` LIVES IN THE UNITY ASSEMBLY AND THIS FILE MAY NEVER
        /// SEE IT (`CLAUDE.md` § 4: the core must never acquire a `UnityEngine` reference).
        /// `HeroLoadoutTests` asserts every name here is one of the bespoke hero glyphs, so a
        /// typo fails in 300 ms rather than drawing a blank tile.
        ///
        /// ⚠️⚠️ AND EVERY VARIANT OF A SLOT SHARES ITS SLOT'S GLYPH, WHICH IS `VISION.md` § 3
        /// RATHER THAN LAZINESS. *"The icon says what the power does to the WORLD, not what
        /// element it is made of."* A sidegrade does not change the job: Chalk Perimeter is still
        /// a zone, Quickdraw is still the same throw. Two icons for one job would teach the
        /// player that the icon means the build rather than the ability.
        /// </summary>
        public string GlyphName { get; }

        public string Name { get; }
        public string Description { get; }

        /// <summary>What the variant buys, as a fraction. 0 on a default.</summary>
        public float Gain { get; }

        /// <summary>What it pays, as a NEGATIVE fraction. 0 on a default.</summary>
        public float Cost { get; }

        public string GainLabel { get; }
        public string CostLabel { get; }

        /// <summary>The Risk of Rain 2 style challenge that unlocks it. Empty on a default.</summary>
        public string Challenge { get; }

        /// <summary>Whether that challenge can be finished in Practice against bots. Must be true.</summary>
        public bool PracticeSafe { get; }

        public bool IsDefault => string.IsNullOrEmpty(Challenge);

        public AbilityVariant(string id, string heroId, int slot, string baseAbility,
                              string glyphName, string name, string description,
                              float gain, float cost, string gainLabel, string costLabel,
                              string challenge = "", bool practiceSafe = true)
        {
            Id = id;
            HeroId = heroId;
            Slot = slot;
            BaseAbility = baseAbility;
            GlyphName = glyphName;
            Name = name;
            Description = description;
            Gain = gain;
            Cost = cost;
            GainLabel = gainLabel;
            CostLabel = costLabel;
            Challenge = challenge;
            PracticeSafe = practiceSafe;
        }
    }

    /// <summary>
    /// The pool of ability variants, per hero, per slot.
    ///
    /// ⚠️⚠️ THE TABLE THIS REPLACES DESCRIBED A GAME THAT DOES NOT EXIST, AND THAT IS
    /// `docs/TODO.md` § 108.3. It listed six heroes as `berto, sean, dante, cheska, zack, nemu`
    /// and gave them `Barricade Shield`, `Ground Slam`, `Sprint Burst`, `Quick Toss`, `Sniper
    /// Aim`, `Decoy Slipper`, `Chalk Trap` and `Smoke Screen`. **Not one of those abilities is in
    /// this repository.** The real kits are in `Assets/TumbangPreso/Runtime/Abilities/*HeroKit.cs`
    /// and every name below is transcribed from the `base(...)` call that registers it.
    ///
    /// ⚠️⚠️ AND `berto` IS NOT A HERO. `Roster.HeroPeople` is DANTE, CHESKA, SEAN, ZACK, NEMU and
    /// **PHAISTER**; `bayan`, whose display name is BERTO, is the first of the twelve CLASSIC
    /// street characters and has no kit at all. The old table offered ability sidegrades to a
    /// character with no abilities and silently dropped one of the six who has them.
    /// `HeroLoadoutTests` now checks this list against `Roster.HeroPeople` directly, so the two
    /// cannot drift again.
    ///
    /// ⚠️ TWO OPTIONS PER SLOT, TWELVE SLOTS, TWENTY-FOUR ROWS. `FUTURE.md` PHASE 10 asks for *"a
    /// small pool of options per slot, not a ladder of upgrades"*, and § 0.5 rule 11b is the test
    /// for growing it: what the PLAYER has to hold in their head. Two readings of one ability is a
    /// choice; five is a spreadsheet.
    /// </summary>
    public static class HeroLoadoutRules
    {
        /// <summary>
        /// ⚠️ DERIVED FROM `Roster.HeroPeople` RATHER THAN TYPED OUT AGAIN. The list it replaces
        /// was a hand-written array that disagreed with the roster in two places at once.
        /// </summary>
        public static IReadOnlyList<string> HeroIds
        {
            get
            {
                var ids = new List<string>(Roster.HeroPeople.Count);
                foreach (var entry in Roster.HeroPeople) ids.Add(entry.Id);
                return ids;
            }
        }

        private static readonly List<AbilityVariant> Variants = new List<AbilityVariant>
        {
            // ---------------------------------------------------------------
            // DANTE. SEISMIC STOMP / DEMONIC CARAPACE. `DanteHeroKit`.
            // ---------------------------------------------------------------
            new AbilityVariant("dante.1.stomp", "dante", 1, "SEISMIC STOMP", "DanteStomp",
                "Seismic Stomp", "The stomp as it is tuned. One heavy shock at the measured radius.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("dante.1.tremor", "dante", 1, "SEISMIC STOMP", "DanteStomp",
                "Long Tremor", "The shock travels further and arrives softer.",
                0.25f, -0.25f, "+25% radius", "-25% knockback",
                "Stomp three attackers at once, twice", true),

            new AbilityVariant("dante.2.carapace", "dante", 2, "DEMONIC CARAPACE", "DanteShield",
                "Demonic Carapace", "The carapace as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("dante.2.plating", "dante", 2, "DEMONIC CARAPACE", "DanteShield",
                "Heavy Plating", "Holds longer and slows you while it is up.",
                0.30f, -0.30f, "+30% duration", "-30% move speed while up",
                "Take twenty slipper hits with the carapace up", true),

            // ---------------------------------------------------------------
            // CHESKA. PERMAFROST SHEET / ICE BARRICADE. `CheskaHeroKit`.
            // ---------------------------------------------------------------
            new AbilityVariant("cheska.1.sheet", "cheska", 1, "PERMAFROST SHEET", "CheskaFrostSheet",
                "Permafrost Sheet", "The sheet as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("cheska.1.blackice", "cheska", 1, "PERMAFROST SHEET", "CheskaFrostSheet",
                "Black Ice", "A smaller sheet that is much harder to stand on.",
                0.35f, -0.35f, "+35% slip", "-35% floor area",
                "Make three attackers slip on one sheet", true),

            new AbilityVariant("cheska.2.barricade", "cheska", 2, "ICE BARRICADE", "CheskaBarricade",
                "Ice Barricade", "The barricade as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("cheska.2.spires", "cheska", 2, "ICE BARRICADE", "CheskaBarricade",
                "Split Spires", "Two narrow pillars instead of one wall. Wider cover, easier to run between.",
                0.40f, -0.40f, "+40% span", "-40% wall thickness",
                "Block ten throws with a barricade", true),

            // ---------------------------------------------------------------
            // SEAN. FLAME RUSH / IGNITION CANNON. `SeanHeroKit`.
            // ---------------------------------------------------------------
            new AbilityVariant("sean.1.rush", "sean", 1, "FLAME RUSH", "SeanRush",
                "Flame Rush", "The rush as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("sean.1.afterburn", "sean", 1, "FLAME RUSH", "SeanRush",
                "Afterburn", "A shorter run that leaves a trail living longer behind it.",
                0.30f, -0.30f, "+30% trail life", "-30% dash distance",
                "Knock down two attackers in one rush", true),

            new AbilityVariant("sean.2.cannon", "sean", 2, "IGNITION CANNON", "SeanIgnite",
                "Ignition Cannon", "The cannon as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("sean.2.flare", "sean", 2, "IGNITION CANNON", "SeanIgnite",
                "Flare Shot", "Faster in the air, and it stops burning sooner where it lands.",
                0.25f, -0.25f, "+25% flight speed", "-25% burn time",
                "Hit the lata with an ignited tsinelas five times", true),

            // ---------------------------------------------------------------
            // ZACK. BOLT SPRINT / STATIC CHARGE. `ZackHeroKit`.
            // ---------------------------------------------------------------
            new AbilityVariant("zack.1.sprint", "zack", 1, "BOLT SPRINT", "ZackSprint",
                "Bolt Sprint", "The sprint as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("zack.1.arcline", "zack", 1, "BOLT SPRINT", "ZackSprint",
                "Arc Line", "A narrower lane that shocks harder.",
                0.30f, -0.30f, "+30% stun", "-30% trail width",
                "Grind past three attackers in one sprint", true),

            new AbilityVariant("zack.2.charge", "zack", 2, "STATIC CHARGE", "ZackOvercharge",
                "Static Charge", "The charge as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("zack.2.discharge", "zack", 2, "STATIC CHARGE", "ZackOvercharge",
                "Snap Discharge", "Charges twice as fast and holds half as long.",
                0.50f, -0.50f, "+50% charge rate", "-50% hold time",
                "Land ten overcharged throws", true),

            // ---------------------------------------------------------------
            // NEMU. PHANTOM VEIL / ASTRAL HIJACK. `NemuHeroKit`.
            // ---------------------------------------------------------------
            new AbilityVariant("nemu.1.veil", "nemu", 1, "PHANTOM VEIL", "NemuPhase",
                "Phantom Veil", "The veil as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("nemu.1.fade", "nemu", 1, "PHANTOM VEIL", "NemuPhase",
                "Long Fade", "Stay unseen longer, and move slower while you are.",
                0.35f, -0.35f, "+35% duration", "-35% move speed while veiled",
                "Retrieve a tsinelas from inside the box while veiled, five times", true),

            new AbilityVariant("nemu.2.hijack", "nemu", 2, "ASTRAL HIJACK", "NemuAstralPet",
                "Astral Hijack", "The hijack as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("nemu.2.leash", "nemu", 2, "ASTRAL HIJACK", "NemuAstralPet",
                "Short Leash", "Reaches further and lets go sooner.",
                0.30f, -0.30f, "+30% reach", "-30% hold",
                "Hijack the taya three times in one match", true),

            // ---------------------------------------------------------------
            // PHAISTER. HEX / SHADOW BLINK. `PhaisterHeroKit`.
            // ⚠️ THE HERO THE PREVIOUS TABLE LEFT OUT ENTIRELY.
            // ---------------------------------------------------------------
            new AbilityVariant("phaister.1.hex", "phaister", 1, "HEX", "PhaisterHexSigil",
                "Hex Sigil", "The sigil as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("phaister.1.brand", "phaister", 1, "HEX", "PhaisterHexSigil",
                "Slow Brand", "Bites harder on one target and covers less ground.",
                0.40f, -0.40f, "+40% effect", "-40% sigil radius",
                "Hex the same attacker in three separate rounds", true),

            new AbilityVariant("phaister.2.blink", "phaister", 2, "SHADOW BLINK", "PhaisterShadowBlink",
                "Shadow Blink", "The blink as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("phaister.2.stride", "phaister", 2, "SHADOW BLINK", "PhaisterShadowBlink",
                "Long Stride", "Further, with a longer wind-up you can be read on.",
                0.30f, -0.30f, "+30% distance", "-30% cast speed",
                "Blink out of the box carrying a tsinelas, five times", true),
        };

        /// <summary>
        /// Whether a variant's challenge has to be finished before it can be equipped.
        ///
        /// ⚠️⚠️ FALSE, ON PURPOSE, AND THE REASON IS THE ONE PROPERTY PHASE 10 DESIGNED IN.
        /// The per-hero challenge counters are not built (`docs/TODO.md` § 108.3), so with this
        /// true every alternate would read LOCKED on every account, and a screen of locked rows is
        /// exactly the fault `docs/TODO.md` § 92.1 records: *fifteen rows of `0/0 (needs 10
        /// throws)` that taught a new player the game was broken.*
        ///
        /// **Handing them all out early costs nothing, and that is provable rather than hoped.**
        /// `IsBudgetNeutral` is asserted on all 24 rows by `HeroLoadoutTests`: every alternate
        /// raises one parameter and lowers another by the same fraction, so an account with
        /// everything unlocked is not stronger than one with nothing, only differently shaped.
        /// `FUTURE.md` PHASE 10 is explicit that this is what the sidegrade rule buys.
        ///
        /// ⚠️ IT IS A FLAG RATHER THAN A DELETION so the ledger path stays live and tested. The
        /// day the counters land this becomes `true` and nothing else moves.
        ///
        /// ⚠️ `static readonly` RATHER THAN `const`, AND THE COMPILER IS WHY. A `const false`
        /// lets the compiler fold the branch and report the ledger lookup below it as unreachable,
        /// which this project builds as an error. The point of the flag is that the path stays
        /// compiled and reachable; a `const` would delete it.
        /// </summary>
        public static readonly bool ChallengesEnforced = false;

        public static IReadOnlyList<AbilityVariant> AllVariants => Variants;

        /// <summary>The options in one slot, defaults first.</summary>
        public static List<AbilityVariant> VariantsFor(string heroId, int slot)
        {
            var list = new List<AbilityVariant>();
            if (string.IsNullOrEmpty(heroId)) return list;

            string clean = heroId.ToLowerInvariant();

            foreach (var v in Variants)
                if (v.HeroId == clean && v.Slot == slot) list.Add(v);

            return list;
        }

        public static AbilityVariant VariantById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return Variants.Find(v => string.Equals(v.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The option a slot falls back to. Never null for a real hero and slot.</summary>
        public static AbilityVariant DefaultFor(string heroId, int slot)
        {
            foreach (var v in VariantsFor(heroId, slot))
                if (v.IsDefault) return v;

            return null;
        }

        /// <summary>
        /// Whether a variant respects the budget.
        ///
        /// ⚠️⚠️ A DEFAULT PASSES BY BEING FLAT, NOT BY BEING EXEMPT. The version this replaces
        /// returned `true` for any default before looking at its numbers, so a default with a
        /// gain and no cost would have been legal and would have made "the option you did not
        /// choose" the strictly worse one. Both arms are checked here.
        ///
        /// ⚠️ THE TOLERANCE IS 0.0005 BECAUSE THESE ARE AUTHORED TO TWO DECIMAL PLACES and a
        /// float comparison against zero would fail on `0.35f - 0.35f`.
        /// </summary>
        public static bool IsBudgetNeutral(AbilityVariant variant)
        {
            if (variant == null) return false;

            if (variant.IsDefault)
                return Math.Abs(variant.Gain) < 0.0005f && Math.Abs(variant.Cost) < 0.0005f;

            return variant.Gain > 0.0f
                   && variant.Cost < 0.0f
                   && Math.Abs(variant.Gain + variant.Cost) < 0.0005f;
        }
    }

    /// <summary>
    /// Which variant a player has equipped, per hero, per slot.
    ///
    /// ⚠️⚠️ THIS DID NOT EXIST AND THAT IS WHY THE LOADOUT SCREEN'S EQUIP BUTTON DID NOTHING.
    /// `docs/TODO.md` § 108.3: the button was created with a `Button` component, a label reading
    /// SELECT, and **no `onClick` listener at all**, because there was nowhere for a choice to go.
    /// A screen for choosing something, with no store behind it, is a screen that cannot choose.
    ///
    /// ⚠️ A LIST OF PAIRS, NOT A DICTIONARY, for the reason `GameSettings.CharacterLoadouts`
    /// records: `JsonUtility` cannot serialise a `Dictionary` and answers an empty one with no
    /// error, so a setting stored in one silently resets on every launch.
    /// </summary>
    [Serializable]
    public sealed class HeroBuild
    {
        public string HeroId = "";
        public string Slot1VariantId = "";
        public string Slot2VariantId = "";
    }

    public static class HeroBuildRules
    {
        /// <summary>
        /// The variant this player has equipped in this slot, checked rather than trusted.
        ///
        /// ⚠️⚠️ THE SAME SHAPE AS `LoadoutRules.PaletteFor`, AND FOR THE SAME REASON:
        /// `settings.json` is a plain text file on the player's disk, and this one decides a
        /// GAMEPLAY number rather than a colour. A variant id that names another hero's option, an
        /// option in the other slot, an option that does not exist, or an option whose challenge
        /// is unfinished all resolve to the default. **The check runs on the receiving side for a
        /// peer's build as well**, so a modified client cannot equip something it did not earn:
        /// the receiver decides what it draws and what it simulates, not the sender.
        /// </summary>
        public static AbilityVariant Equipped(PlayerProfile profile, HeroBuild build,
                                              string heroId, int slot)
        {
            var fallback = HeroLoadoutRules.DefaultFor(heroId, slot);
            if (build == null) return fallback;

            string wanted = slot == 1 ? build.Slot1VariantId : build.Slot2VariantId;
            var variant = HeroLoadoutRules.VariantById(wanted);

            if (variant == null) return fallback;
            if (!string.Equals(variant.HeroId, heroId, StringComparison.OrdinalIgnoreCase)) return fallback;
            if (variant.Slot != slot) return fallback;
            if (!HeroLoadoutRules.IsBudgetNeutral(variant)) return fallback;
            if (!IsUnlocked(profile, variant)) return fallback;

            return variant;
        }

        /// <summary>
        /// Whether the account has finished the challenge behind a variant.
        ///
        /// ⚠️⚠️ IT READS THE SAME REWARD LEDGER EVERY OTHER UNLOCK READS, `BannerRules.Owns`,
        /// rather than growing a second one. `docs/TODO.md` § 101 is the entry about a palette
        /// that could never be equipped because two halves of the code named the same reward
        /// differently; one ledger is how that stops being possible.
        ///
        /// ⚠️ A DEFAULT IS ALWAYS UNLOCKED, so a fresh account has a complete, legal build for
        /// every hero on its first launch and the screen is never empty.
        /// </summary>
        public static bool IsUnlocked(PlayerProfile profile, AbilityVariant variant)
        {
            if (variant == null) return false;
            if (variant.IsDefault) return true;
            if (!HeroLoadoutRules.ChallengesEnforced) return true;

            return BannerRules.Owns(profile, RewardKind.AbilityVariant, variant.Id);
        }

        /// <summary>The build row for a hero, created on demand. Mirrors `LoadoutRules.RowFor`.</summary>
        public static HeroBuild RowFor(List<HeroBuild> builds, string heroId)
        {
            if (builds == null || string.IsNullOrEmpty(heroId)) return null;

            foreach (var row in builds)
                if (row != null && row.HeroId == heroId) return row;

            var added = new HeroBuild { HeroId = heroId };
            builds.Add(added);
            return added;
        }
    }
}
