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

        /// <summary>
        /// Successful casts of this slot needed to unlock the variant.
        ///
        /// ⚠️⚠️ THE COUNTER IS CASTS, NOT WINS, XP OR ONLINE MATCHES. Phase 10 promises that
        /// every unlock can be earned in Practice against bots, so the event has to exist before
        /// a career record and without a service. A successful cast is also the one event all
        /// twelve skills share and the one that actually teaches the player the button.
        /// </summary>
        public int ChallengeTarget { get; }

        /// <summary>Whether that challenge can be finished in Practice against bots. Must be true.</summary>
        public bool PracticeSafe { get; }

        public bool IsDefault => string.IsNullOrEmpty(Challenge);

        public AbilityVariant(string id, string heroId, int slot, string baseAbility,
                              string glyphName, string name, string description,
                              float gain, float cost, string gainLabel, string costLabel,
                              string challenge = "", bool practiceSafe = true,
                              int challengeTarget = 0)
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
            ChallengeTarget = string.IsNullOrEmpty(challenge) ? 0 : Math.Max(1, challengeTarget);
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

            // ⚠️⚠️ 25 PER CENT IS THE SMALLEST NUMBER IN THIS TABLE ON PURPOSE. DO NOT RAISE IT.
            // The gain is a RADIUS, so it is already the largest change in the table by the thing
            // the player actually sees: 2.2 m to 2.75 m is 56 per cent more floor. It is also
            // the only row that grows a footprint, and `docs/VISION.md` § 2 rule 1 asks a skill
            // for 1.8 to 2.5 m; 2.75 is over that already and a "bigger" number here would be
            // bought straight out of the readability budget the whole mode is balanced against.
            // ⚠️⚠️ THE ROW NAMES WHAT HAPPENS TO A PERSON NOW, NOT WHAT HAPPENS TO A NUMBER.
            // 🧑 2026-09-02: *"i want each loadout skill to feel thoroughly unique and actually
            // add value and feel like a niche kit that is great in the game"*. Twelve alternates
            // that all read "+N% something / -N% something else" are twelve spreadsheet rows, and
            // a player cannot feel 25 per cent of a knockback. `DanteHeroKit` sweeps feet on this
            // variant as of the same day, so the sentence and the game now say the same thing.
            new AbilityVariant("dante.1.tremor", "dante", 1, "SEISMIC STOMP", "DanteStomp",
                "Long Tremor", "Reaches half again as much street, and sweeps feet instead of throwing bodies. They stay where they fell.",
                0.25f, -0.25f, "Wider break, takes them down", "They land at your feet, not away",
                "Use Seismic Stomp eight times", true, 8),

            new AbilityVariant("dante.2.carapace", "dante", 2, "DEMONIC CARAPACE", "DanteShield",
                "Demonic Carapace", "The carapace as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("dante.2.plating", "dante", 2, "DEMONIC CARAPACE", "DanteShield",
                "Heavy Plating", "Stays up much longer, but you walk while it does. A wall you can place yourself, not an escape.",
                0.30f, -0.30f, "Walk in and take what you need", "You are slow the whole time",
                "Use Demonic Carapace six times", true, 6),

            // ---------------------------------------------------------------
            // CHESKA. PERMAFROST SHEET / ICE BARRICADE. `CheskaHeroKit`.
            // ---------------------------------------------------------------
            new AbilityVariant("cheska.1.sheet", "cheska", 1, "PERMAFROST SHEET", "CheskaFrostSheet",
                "Permafrost Sheet", "The sheet as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("cheska.1.blackice", "cheska", 1, "PERMAFROST SHEET", "CheskaFrostSheet",
                "Black Ice", "A patch half the size that nobody keeps their feet on. For a doorway, not a lane.",
                0.35f, -0.35f, "Nobody crosses it standing", "Covers a doorway, not a lane",
                "Use Permafrost Sheet eight times", true, 8),

            new AbilityVariant("cheska.2.barricade", "cheska", 2, "ICE BARRICADE", "CheskaBarricade",
                "Ice Barricade", "The barricade as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("cheska.2.spires", "cheska", 2, "ICE BARRICADE", "CheskaBarricade",
                "Split Spires", "Two thin pillars set wide instead of one wall. Covers more of the approach, and a body can slip between them.",
                0.40f, -0.40f, "Covers the whole approach", "There is a gap, and they will find it",
                "Use Ice Barricade six times", true, 6),

            // ---------------------------------------------------------------
            // SEAN. FLAME RUSH / IGNITION CANNON. `SeanHeroKit`.
            // ---------------------------------------------------------------
            new AbilityVariant("sean.1.rush", "sean", 1, "FLAME RUSH", "SeanRush",
                "Flame Rush", "The rush as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("sean.1.afterburn", "sean", 1, "FLAME RUSH", "SeanRush",
                "Afterburn", "A short burst that leaves the road burning far longer. You are not escaping with this one, you are closing a lane behind you.",
                0.30f, -0.30f, "The road stays lit behind you", "You do not get far",
                "Use Flame Rush eight times", true, 8),

            new AbilityVariant("sean.2.cannon", "sean", 2, "IGNITION CANNON", "SeanIgnite",
                "Ignition Cannon", "The cannon as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("sean.2.flare", "sean", 2, "IGNITION CANNON", "SeanIgnite",
                "Flare Shot", "A flat, fast shot that cracks in a tight circle. Made for hitting the lata, not for catching whoever is standing near it.",
                0.25f, -0.25f, "Hard to read, hard to dodge", "You have to actually hit it",
                "Use Ignition Cannon eight times", true, 8),

            // ---------------------------------------------------------------
            // ZACK. BOLT SPRINT / MAGNET. `ZackHeroKit`.
            // ---------------------------------------------------------------
            new AbilityVariant("zack.1.sprint", "zack", 1, "BOLT SPRINT", "ZackSprint",
                "Bolt Sprint", "The sprint as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            // ⚠️⚠️ 45 PER CENT, UP FROM 30, AND THE GAIN REACHES TWO NUMBERS RATHER THAN ONE.
            // At 30 per cent this row was the one alternate in the twelve a player could not
            // feel: it bought 0.075 s of extra stagger on a 0.25 s stumble, which is four
            // frames. It now scales the stagger AND divides the 1.1 s re-shock interval, so a
            // runner caught in the lane is shocked half again as often and for half again as
            // long, which is a decision (take the long way round) rather than a statistic.
            // ⚠️ THE COST IS THE ONE THAT MATTERS MOST TO THE ROOM. `docs/VISION.md` § 2 records
            // Zack's corridor at 27.2 per cent of the box off a 6.0 s cooldown, more floor than
            // any ultimate; a 0.45 width cut takes one dash's lane to about 8 per cent.
            new AbilityVariant("zack.1.arcline", "zack", 1, "BOLT SPRINT", "ZackSprint",
                "Arc Line", "One thin live wire instead of a corridor. Whoever follows you down it is shocked harder and shocked again sooner.",
                0.45f, -0.45f, "Punishes anyone who follows", "One lane wide, easy to step around",
                "Use Bolt Sprint eight times", true, 8),

            // ⚠️⚠️ THE SLOT IS MAGNET NOW AND `BaseAbility` HAD TO MOVE WITH IT.
            // `HeroLoadoutTests` reads the kit and compares this string to the ability's real
            // name, which is the whole reason that field exists: STATIC CHARGE was deleted in
            // `ZackHeroKit` on 2026-09-02 because it was Sean's Ignition Cannon with a different
            // element on it, and a variant table naming an ability that no longer exists is the
            // exact fault § 108.3 records about `berto`.
            new AbilityVariant("zack.2.charge", "zack", 2, "MAGNET", "ZackOvercharge",
                "Magnet", "The pull as it is tuned. One charge, back on a knockdown.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            // ⚠️ THE ALTERNATE SELLS THE THROW AND PAYS WITH THE WINDOW, which is the same trade
            // Snap Discharge made and the one row of the old pair that still describes something
            // the ability does. What changed is what it is attached to.
            new AbilityVariant("zack.2.discharge", "zack", 2, "MAGNET", "ZackOvercharge",
                "Snap Discharge", "The tsinelas comes back hot and does not stay that way. Throw it now or throw it cold.",
                0.50f, -0.50f, "Almost no time to read it", "Throw it now or lose it",
                "Use Magnet eight times", true, 8),

            // ---------------------------------------------------------------
            // NEMU. PHANTOM VEIL / ASTRAL HIJACK. `NemuHeroKit`.
            // ---------------------------------------------------------------
            new AbilityVariant("nemu.1.veil", "nemu", 1, "PHANTOM VEIL", "NemuPhase",
                "Phantom Veil", "The veil as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("nemu.1.fade", "nemu", 1, "PHANTOM VEIL", "NemuPhase",
                "Long Fade", "Untouchable for much longer, at a walk. You are crossing the court, not running out of a corner.",
                0.35f, -0.35f, "Long enough to walk in and out", "You are walking, not running",
                "Use Phantom Veil eight times", true, 8),

            new AbilityVariant("nemu.2.hijack", "nemu", 2, "ASTRAL HIJACK", "NemuAstralPet",
                "Astral Hijack", "The hijack as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            // ⚠️ THERE IS NO LEASH RADIUS TO SELL, AND THE ROW USED TO PROMISE ONE. Kuro flies
            // free while possessed and the ability ends on its own duration
            // (`GhostPetCompanion`), so "+30% reach" named a parameter that does not exist. What
            // does exist is his speed, and 40 per cent of it is the difference between arriving
            // while somebody is still bent over their tsinelas and arriving after.
            new AbilityVariant("nemu.2.leash", "nemu", 2, "ASTRAL HIJACK", "NemuAstralPet",
                "Short Leash", "Kuro darts across the street and drops you back almost at once. A look, not a scouting run.",
                0.40f, -0.40f, "He is there before they move", "One look and you are back",
                "Use Astral Hijack six times", true, 6),

            // ---------------------------------------------------------------
            // PHAISTER. HEX / SHADOW BLINK. `PhaisterHeroKit`.
            // ⚠️ THE HERO THE PREVIOUS TABLE LEFT OUT ENTIRELY.
            // ---------------------------------------------------------------
            new AbilityVariant("phaister.1.hex", "phaister", 1, "HEX", "PhaisterHexSigil",
                "Hex Sigil", "The sigil as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            // ⚠️ THE GAIN REACHES THREE NUMBERS, for the reason Arc Line's note above gives: a
            // 40 per cent gain on a 0.35 s stagger alone is a tenth of a second and reads as
            // nothing. It scales the drag, the stagger and the rate the ward re-bites, so a
            // 1.44 m brand is a place one attacker genuinely cannot cross rather than a smaller
            // version of the same puddle.
            new AbilityVariant("phaister.1.brand", "phaister", 1, "HEX", "PhaisterHexSigil",
                "Slow Brand", "A tight ward that holds much harder and bites again much sooner. One person genuinely cannot cross it.",
                0.40f, -0.40f, "They are not getting out of it", "Small enough to walk around",
                "Use Hex eight times", true, 8),

            new AbilityVariant("phaister.2.blink", "phaister", 2, "SHADOW BLINK", "PhaisterShadowBlink",
                "Shadow Blink", "The blink as it is tuned.",
                0.0f, 0.0f, "As tuned", "As tuned"),

            new AbilityVariant("phaister.2.stride", "phaister", 2, "SHADOW BLINK", "PhaisterShadowBlink",
                "Long Stride", "Reaches much further, and takes long enough to aim that the taya can watch you decide.",
                0.30f, -0.30f, "Across the court in one press", "They can see it coming",
                "Use Shadow Blink eight times", true, 8),
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
        /// counters landed on 2026-09-01 (`AbilityChallengeProgress`, counted by
        /// `GameSettings.NoteAbilityCast` off a successful local cast), so this is `true` now and
        /// the flag stays as the one switch that opens every alternate for a tournament build.
        ///
        /// ⚠️ `static readonly` RATHER THAN `const`, AND THE COMPILER IS WHY. A `const false`
        /// lets the compiler fold the branch and report the ledger lookup below it as unreachable,
        /// which this project builds as an error. The point of the flag is that the path stays
        /// compiled and reachable; a `const` would delete it.
        /// </summary>
        public static readonly bool ChallengesEnforced = true;

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

    /// <summary>
    /// One locally earned challenge, persisted in settings and incremented by successful casts.
    ///
    /// ⚠️⚠️ THIS IS LOCAL RATHER THAN A CAREER TOTAL ON PURPOSE. Practice matches are excluded
    /// from <see cref="PlayerProfile"/> by design, while Phase 10 explicitly requires every
    /// variant to be earnable in Practice against bots. Putting this on the career would make the
    /// promise false; sending one Cloud Code write per cast would break the free-tier rule.
    /// Sidegrades remain budget-neutral, so a tampered local counter cannot buy power.
    /// </summary>
    [Serializable]
    public sealed class AbilityChallengeProgress
    {
        public string VariantId = "";
        public int Count;
    }

    public static class HeroBuildRules
    {
        public static int ChallengeCount(List<AbilityChallengeProgress> counters, string variantId)
        {
            if (counters == null || string.IsNullOrEmpty(variantId)) return 0;
            foreach (var row in counters)
                if (row != null && string.Equals(row.VariantId, variantId,
                                                  StringComparison.OrdinalIgnoreCase))
                    return Math.Max(0, row.Count);
            return 0;
        }

        /// <summary>
        /// Counts one successful skill cast toward the alternate in that hero and slot.
        /// Defaults have no challenge and are deliberately skipped. The count caps at the target
        /// so a long-running profile cannot overflow and a completed row stops changing disk.
        /// </summary>
        public static bool NoteSuccessfulCast(List<AbilityChallengeProgress> counters,
                                              string heroId, int slot)
        {
            if (counters == null || string.IsNullOrEmpty(heroId) || slot < 1 || slot > 2)
                return false;

            bool changed = false;
            foreach (var variant in HeroLoadoutRules.VariantsFor(heroId, slot))
            {
                if (variant.IsDefault || variant.ChallengeTarget <= 0) continue;

                AbilityChallengeProgress row = null;
                foreach (var candidate in counters)
                    if (candidate != null && string.Equals(candidate.VariantId, variant.Id,
                                                           StringComparison.OrdinalIgnoreCase))
                    {
                        row = candidate;
                        break;
                    }

                if (row == null)
                {
                    row = new AbilityChallengeProgress { VariantId = variant.Id };
                    counters.Add(row);
                }

                int next = Math.Min(variant.ChallengeTarget, Math.Max(0, row.Count) + 1);
                if (next == row.Count) continue;
                row.Count = next;
                changed = true;
            }
            return changed;
        }

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
        ///
        /// ⚠️⚠️ THE `PlayerProfile` OVERLOAD THIS REPLACED IS DELETED RATHER THAN LEFT BESIDE IT.
        /// The ledger moved to `AbilityChallengeProgress`, which is local and Practice-safe, so
        /// the profile has nothing to say about an unlock any more: `RewardKind.AbilityVariant`
        /// is awarded by no track in the game (`ProgressionTests` asserts that both tables stay
        /// off it), so the old overload would have answered "locked" for all twelve alternates
        /// forever, silently, exactly the way `LoadoutRules.PaletteFor` did in § 101.1. An
        /// overload that can only ever return the fallback is worse than no overload.
        ///
        /// A null ledger is the receiving/network path: the host still validates hero, slot and
        /// budget, but cannot prove an offline Practice counter and therefore does not pretend to.
        /// </summary>
        public static AbilityVariant Equipped(HeroBuild build, string heroId, int slot,
                                              List<AbilityChallengeProgress> counters)
        {
            var fallback = HeroLoadoutRules.DefaultFor(heroId, slot);
            if (build == null) return fallback;

            string wanted = slot == 1 ? build.Slot1VariantId : build.Slot2VariantId;
            var variant = HeroLoadoutRules.VariantById(wanted);

            if (variant == null) return fallback;
            if (!string.Equals(variant.HeroId, heroId, StringComparison.OrdinalIgnoreCase)) return fallback;
            if (variant.Slot != slot) return fallback;
            if (!HeroLoadoutRules.IsBudgetNeutral(variant)) return fallback;
            if (counters != null && !IsUnlocked(counters, variant)) return fallback;

            return variant;
        }

        /// <summary>
        /// Whether this install has finished the challenge behind a variant.
        ///
        /// ⚠️⚠️ THE LEDGER IS THE LOCAL CAST COUNTER, NOT `BannerRules.Owns`. The first draft of
        /// this read the reward ledger every cosmetic unlock reads, which is the right instinct
        /// and the wrong ledger here: a reward is written by `match-record.js` off a submitted
        /// career, and Phase 10 promises every alternate can be earned in Practice against bots,
        /// where no record is ever submitted and the service may not be reachable at all. Reading
        /// the reward ledger made the promise false and nothing logged it.
        ///
        /// ⚠️ A DEFAULT IS ALWAYS UNLOCKED, so a fresh account has a complete, legal build for
        /// every hero on its first launch and the screen is never empty.
        /// </summary>
        public static bool IsUnlocked(List<AbilityChallengeProgress> counters,
                                      AbilityVariant variant)
        {
            if (variant == null) return false;
            if (variant.IsDefault) return true;
            if (!HeroLoadoutRules.ChallengesEnforced) return true;
            return ChallengeCount(counters, variant.Id) >= variant.ChallengeTarget;
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

        /// <summary>
        /// Host-side validation for a build received over the wire. Unlock counters are local and
        /// Practice-safe, so the host cannot verify them; it can and does reject unknown ids,
        /// another hero's option, the wrong slot and anything outside the budget rule.
        /// </summary>
        public static HeroBuild NormaliseForWire(HeroBuild build, string heroId)
        {
            var one = Equipped(build, heroId, 1, null);
            var two = Equipped(build, heroId, 2, null);
            return new HeroBuild
            {
                HeroId = heroId ?? "",
                Slot1VariantId = one?.Id ?? "",
                Slot2VariantId = two?.Id ?? "",
            };
        }

        public static string Encode(HeroBuild build, string heroId)
        {
            var clean = NormaliseForWire(build, heroId);
            return "B1:" + clean.HeroId + "|" + clean.Slot1VariantId + "|" + clean.Slot2VariantId;
        }

        public static HeroBuild Decode(string wire, string heroId)
        {
            if (string.IsNullOrEmpty(wire) || !wire.StartsWith("B1:", StringComparison.Ordinal))
                return NormaliseForWire(null, heroId);

            string[] parts = wire.Substring(3).Split('|');
            var read = new HeroBuild
            {
                HeroId = parts.Length > 0 ? parts[0] : "",
                Slot1VariantId = parts.Length > 1 ? parts[1] : "",
                Slot2VariantId = parts.Length > 2 ? parts[2] : "",
            };
            if (!string.Equals(read.HeroId, heroId, StringComparison.OrdinalIgnoreCase))
                return NormaliseForWire(null, heroId);
            return NormaliseForWire(read, heroId);
        }
    }
}
