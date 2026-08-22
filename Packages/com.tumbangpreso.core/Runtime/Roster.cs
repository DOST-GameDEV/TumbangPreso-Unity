using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>Which of the three meters a trait lookup means. The keys are internal
    /// identifiers, never display text: each tab renames them (a Person's SPEED is a
    /// lata's RESET is a tsinelas's FLIGHT) but the key and the multiplier are one.</summary>
    public enum Trait
    {
        /// <summary>PERSON: SPEED · LATA: RESET · TSINELAS: FLIGHT</summary>
        Bilis,

        /// <summary>PERSON: POWER · LATA: REBOUND · TSINELAS: IMPACT</summary>
        Lakas,

        /// <summary>PERSON: GRIT · LATA: STANCE · TSINELAS: RECOVERY</summary>
        Tatag,
    }

    /// <summary>One roster entry: a stable id, a display name, and three trait points.</summary>
    public sealed class RosterEntry
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Bilis;
        public readonly int Lakas;
        public readonly int Tatag;

        public RosterEntry(string id, string name, int bilis, int lakas, int tatag)
        {
            Id = id;
            Name = name;
            Bilis = bilis;
            Lakas = lakas;
            Tatag = tatag;
        }

        public int Points(Trait t)
        {
            switch (t)
            {
                case Trait.Bilis: return Bilis;
                case Trait.Lakas: return Lakas;
                default: return Tatag;
            }
        }
    }

    /// <summary>
    /// The three pick lists and the ONE conversion from points to a multiplier.
    ///
    /// ⚠️ INDEX ORDER IS PART OF THE NETWORK CONTRACT. character_index, can_index and
    /// slipper_index all cross the wire as plain ints, so every peer must resolve the
    /// same index to the same entry. APPEND; never reorder or delete, or two peers on
    /// different builds quietly render different people. The `Id` is the stable key for
    /// anything saved to disk; the index is only ever a wire format.
    ///
    /// ⚠️ EVERY LOOKUP FALLS BACK TO NEUTRAL RATHER THAN THROWING. These are read on the
    /// spawn path from a replicated int, and three legitimate cases produce an index this
    /// build has no entry for: an AI slot has no pick (-1), a command-line host session
    /// never passed the CHARACTER screen, and a peer on an older build can send an index
    /// off the end. All three must produce a playable unit carrying the balance
    /// everything else was tuned against, not a crash and not a silently stronger one.
    /// </summary>
    public static class Roster
    {
        public const int TraitMin = 1;
        public const int TraitMax = 5;
        public const int TraitNeutral = 3;

        /// <summary>
        /// The twelve original Kenney street characters for Classic mode.
        /// </summary>
        public static readonly IReadOnlyList<RosterEntry> ClassicPeople = new[]
        {
            //                id              name           bilis lakas tatag
            new RosterEntry("bayan",       "BERTO",           2,    4,    5),
            new RosterEntry("maring",      "MARING",          5,    2,    2),
            new RosterEntry("totoy",       "TOTOY",           5,    2,    3),
            new RosterEntry("inday",       "INDAY",           3,    4,    4),
            new RosterEntry("kuya_boy",    "KUYA BOY",        3,    5,    3),
            new RosterEntry("ate_girlie",  "ATE GIRLIE",      4,    3,    3),
            new RosterEntry("tikboy",      "TIKBOY",          4,    4,    2),
            new RosterEntry("bebang",      "BEBANG",          2,    5,    5),
            new RosterEntry("jun_jun",     "JUN-JUN",         5,    1,    2),
            new RosterEntry("lola_pacing", "LOLA PACING",     1,    4,    5),
            new RosterEntry("mang_kanor",  "MANG KANOR",      5,    3,    2),
            new RosterEntry("aling_nena",  "ALING NENA",      2,    3,    5),
        };

        /// <summary>
        /// The five playable action heroes for Hero Strike mode.
        /// </summary>
        public static readonly IReadOnlyList<RosterEntry> HeroPeople = new[]
        {
            //                id              name           bilis lakas tatag
            new RosterEntry("dante",       "DANTE",           2,    4,    5),
            new RosterEntry("cheska",      "CHESKA",          3,    4,    4),
            new RosterEntry("sean",        "SEAN",            3,    5,    3),
            new RosterEntry("zack",        "ZACK",            4,    3,    3),
            new RosterEntry("nemu",        "NEMU",            4,    3,    4),
        };

        /// <summary>
        /// Master list containing all playable characters across all modes.
        /// </summary>
        public static readonly IReadOnlyList<RosterEntry> AllPeople = new[]
        {
            new RosterEntry("bayan",       "BERTO",           2,    4,    5),
            new RosterEntry("maring",      "MARING",          5,    2,    2),
            new RosterEntry("totoy",       "TOTOY",           5,    2,    3),
            new RosterEntry("inday",       "INDAY",           3,    4,    4),
            new RosterEntry("kuya_boy",    "KUYA BOY",        3,    5,    3),
            new RosterEntry("ate_girlie",  "ATE GIRLIE",      4,    3,    3),
            new RosterEntry("tikboy",      "TIKBOY",          4,    4,    2),
            new RosterEntry("bebang",      "BEBANG",          2,    5,    5),
            new RosterEntry("jun_jun",     "JUN-JUN",         5,    1,    2),
            new RosterEntry("lola_pacing", "LOLA PACING",     1,    4,    5),
            new RosterEntry("mang_kanor",  "MANG KANOR",      5,    3,    2),
            new RosterEntry("aling_nena",  "ALING NENA",      2,    3,    5),
            new RosterEntry("dante",       "DANTE",           2,    4,    5),
            new RosterEntry("cheska",      "CHESKA",          3,    4,    4),
            new RosterEntry("sean",        "SEAN",            3,    5,    3),
            new RosterEntry("zack",        "ZACK",            4,    3,    3),
            new RosterEntry("nemu",        "NEMU",            4,    3,    4),
        };

        /// <summary>Default people list for global lookups.</summary>
        public static readonly IReadOnlyList<RosterEntry> People = AllPeople;

        /// <summary>Get roster characters for the given game mode.</summary>
        public static IReadOnlyList<RosterEntry> GetPeople(GameMode mode) =>
            mode == GameMode.HeroStrike ? HeroPeople : ClassicPeople;

        /// <summary>
        /// The four cans. RESET / REBOUND / STANCE.
        ///
        /// ⚠️ RETUNED AGAINST THE MESHES, so every row is derivable from the shape that
        /// was actually drawn rather than from an abstract role: a tall empty can topples
        /// and rights instantly, a flat disc is hard to tip AND quick to right with no
        /// mass to rebound, a squat tin half full of set paint does not move and is a job
        /// to stand up, a solid ribbed tin punishes the throw. Each owns exactly one 5.
        ///
        /// ⚠️ THE THREE LATA STATS ARE THREE ROUTES TO ONE GOAL AND THAT IS THE DESIGN.
        /// A taya wants the can upright, because that is what passive defence pays for.
        /// STANCE refuses the knockdown, RESET shortens the recovery, REBOUND punishes
        /// the attempt. A can that did all three would be the correct answer.
        /// </summary>
        public static readonly IReadOnlyList<RosterEntry> Cans = new[]
        {
            new RosterEntry("pasip",   "PASIP",         5,    1,    1),
            new RosterEntry("boyben",  "BOYBEN",        1,    3,    5),
            new RosterEntry("decades", "DECADES TUNA",  4,    1,    4),
            new RosterEntry("metal",   "KALAWANG",      2,    5,    3),
        };

        /// <summary>
        /// The four slippers. FLIGHT / IMPACT / RECOVERY.
        ///
        /// ⚠️ ENTRY 0 STAYS NEUTRAL ON PURPOSE. TSINELAS is what an unpicked slipper and
        /// every -1 fallback resolve to, so giving it a non-neutral row would silently
        /// retune every AI seat and every peer that never reached the CHARACTER screen.
        /// The same holds for the cans' entry 0 through the neutral fallback.
        ///
        /// ⚠️ FLIGHT IS THE NARROWEST STAT IN THE GAME, spanning only 2..4. That ceiling
        /// is not taste: the AI inverts the range equation against LaunchSpeed to decide
        /// how long to charge a throw, so a per-skin launch speed is an error term inside
        /// a solve that lives in another file. 5% sits inside the margin it already
        /// charges to; 20% would make every bot holding a slow slipper fall short, which
        /// reads as an AI regression rather than as a balance change.
        /// </summary>
        public static readonly IReadOnlyList<RosterEntry> Slippers = new[]
        {
            new RosterEntry("tsinelas", "TSINELAS",  3,    3,    3),
            new RosterEntry("crocs",    "CROCS",     2,    5,    2),
            new RosterEntry("pantulog", "PANTULOG",  3,    1,    5),
            new RosterEntry("sike",     "IKE",       4,    2,    3),
        };

        /// <summary>
        /// ⚠️⚠️ THE ONE CONVERSION FROM POINTS TO A MULTIPLIER, FOR ALL THREE TABS.
        /// Three points below neutral and three above, times a per-point constant the
        /// CALLER owns, because a point of SPEED is worth 5% and a point of POWER is
        /// worth 7%: which constant applies is a property of the stat, not of this
        /// function.
        ///
        /// ⚠️ NEUTRAL IS EXACTLY 1.0 BY CONSTRUCTION. That is what makes "no pick", "an
        /// AI seat", "a peer on an older build" and "entry 0" all play the same game.
        /// Three separate implementations of `1 + (points - 3) * per_point` is how that
        /// stops being true on one of them.
        /// </summary>
        public static float TraitScale(int points, float perPoint)
        {
            int clamped = points < TraitMin ? TraitMin : (points > TraitMax ? TraitMax : points);
            return 1.0f + (clamped - TraitNeutral) * perPoint;
        }

        /// <summary>Wrapped, never-throwing lookup. See the class note on why.</summary>
        /// <summary>
        /// The index of an entry by its stable id, or -1.
        ///
        /// ⚠️ BY ID, NEVER BY A TYPED INDEX. `character_roster.gd` records that these tables are
        /// APPEND-ONLY because the index is a wire format, and it has been reordered before: a
        /// literal index in a caller silently becomes a different character or a different shoe
        /// the next time somebody inserts an entry, and nothing errors.
        /// </summary>
        public static int IndexIn(IReadOnlyList<RosterEntry> entries, string id)
        {
            if (entries == null || id == null) return -1;

            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Id == id) return i;

            return -1;
        }

        public static RosterEntry At(IReadOnlyList<RosterEntry> entries, int index)
        {
            if (entries == null || entries.Count == 0) return null;
            int n = entries.Count;
            int wrapped = ((index % n) + n) % n; // Godot posmod
            return entries[wrapped];
        }

        /// <summary>Trait points for an entry, or neutral when the index has no entry.</summary>
        public static int TraitPoints(IReadOnlyList<RosterEntry> entries, int index, Trait t)
        {
            if (entries == null || index < 0 || index >= entries.Count) return TraitNeutral;
            int points = entries[index].Points(t);
            return points < TraitMin ? TraitMin : (points > TraitMax ? TraitMax : points);
        }

        public static int PersonTrait(int index, Trait t) => TraitPoints(People, index, t);
        public static int PersonTrait(int index, Trait t, GameMode mode) => TraitPoints(GetPeople(mode), index, t);
        public static int CanTrait(int index, Trait t) => TraitPoints(Cans, index, t);
        public static int SlipperTrait(int index, Trait t) => TraitPoints(Slippers, index, t);

        // -------------------------------------------------------------------
        // THE PERSON SCALES. These three are what gameplay actually asks for.
        // -------------------------------------------------------------------

        public static float PersonSpeedScale(int index) =>
            TraitScale(PersonTrait(index, Trait.Bilis), Balance.TraitSpeedPerPoint);

        public static float PersonSpeedScale(int index, GameMode mode) =>
            TraitScale(PersonTrait(index, Trait.Bilis, mode), Balance.TraitSpeedPerPoint);

        public static float PersonPowerScale(int index) =>
            TraitScale(PersonTrait(index, Trait.Lakas), Balance.TraitPowerPerPoint);

        public static float PersonPowerScale(int index, GameMode mode) =>
            TraitScale(PersonTrait(index, Trait.Lakas, mode), Balance.TraitPowerPerPoint);

        /// <summary>
        /// ⚠️ FLOORED AT 0.1, MATCHING character_base.gd. Grit DIVIDES incoming knockback
        /// and stagger, so a scale that could reach zero is a divide by zero at the one
        /// moment the game is resolving a hit. The floor can never be reached by any
        /// authored row; it exists so that a future per-point change cannot make it so.
        /// </summary>
        public static float PersonGritScale(int index)
        {
            float scale = TraitScale(PersonTrait(index, Trait.Tatag), Balance.TraitGritPerPoint);
            return scale < 0.1f ? 0.1f : scale;
        }

        public static float PersonGritScale(int index, GameMode mode)
        {
            float scale = TraitScale(PersonTrait(index, Trait.Tatag, mode), Balance.TraitGritPerPoint);
            return scale < 0.1f ? 0.1f : scale;
        }

        // -------------------------------------------------------------------
        // THE PROP SCALES.
        // -------------------------------------------------------------------

        /// <summary>How fast the taya stands this can back up: divides ResetChannelTime.</summary>
        public static float CanResetScale(int index) =>
            TraitScale(CanTrait(index, Trait.Bilis), Balance.TraitSpeedPerPoint);

        /// <summary>How far this can throws a slipper that hits it.</summary>
        public static float CanReboundScale(int index) =>
            TraitScale(CanTrait(index, Trait.Lakas), Balance.TraitPowerPerPoint);

        /// <summary>How hard this can is to knock over: divides the hit MARGIN.
        /// ⚠️ Floored at 0.1 exactly as lata.gd does, because it divides.</summary>
        public static float CanStanceScale(int index)
        {
            float scale = TraitScale(CanTrait(index, Trait.Tatag), Balance.TraitGritPerPoint);
            return scale < 0.1f ? 0.1f : scale;
        }

        /// <summary>Multiplies LaunchSpeed. Deliberately the narrowest stat in the game.</summary>
        public static float SlipperFlightScale(int index) =>
            TraitScale(SlipperTrait(index, Trait.Bilis), Balance.TraitSpeedPerPoint);

        /// <summary>Multiplies the push a body-block deals to the blocker.</summary>
        public static float SlipperImpactScale(int index) =>
            TraitScale(SlipperTrait(index, Trait.Lakas), Balance.TraitPowerPerPoint);

        /// <summary>Divides ThrowLockTime: armed again sooner after a pickup.
        /// ⚠️ Floored at 0.1 exactly as slipper.gd does, because it divides.</summary>
        public static float SlipperRecoveryScale(int index)
        {
            float scale = TraitScale(SlipperTrait(index, Trait.Tatag), Balance.TraitGritPerPoint);
            return scale < 0.1f ? 0.1f : scale;
        }

        /// <summary>
        /// What this slipper actually launches at, which is what the CHARACTER screen's
        /// MAX POWER row must print.
        ///
        /// ⚠️ IT IS NOT THE SAME NUMBER FOR ALL FOUR, AND THAT WAS THE FIX. There is one
        /// LaunchSpeed in the game, but the skin's FLIGHT scale multiplies it on the way
        /// out, so reporting the bare constant told the player the baseline rather than
        /// what the pick in front of them does. Four slippers printing an identical
        /// "19 m/s" under four visibly different FLIGHT meters is a live row that says
        /// nothing. Scaled through the one conversion so it cannot drift from flight.
        /// </summary>
        public static float SlipperLaunchSpeed(int index)
        {
            if (index < 0 || index >= Slippers.Count) return -1.0f;
            return Balance.LaunchSpeed * SlipperFlightScale(index);
        }
    }
}
