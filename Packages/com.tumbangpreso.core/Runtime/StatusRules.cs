using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// A named condition a body is under, shown to every player as an icon with a tooltip.
    ///
    /// ⚠️⚠️ THE OWNER'S TABLE, 2026-09-25, TRANSCRIBED AS DATA. He sent the four rows (status,
    /// description, tooltip) as the one list of statuses for the ability overhaul, with Amihan's
    /// kit the first to use them. `StatusRules` below is that table; `Core.Tests` asserts every
    /// number against it so a retune cannot drift from what he wrote.
    ///
    /// ⚠️ APPEND ONLY. The byte value crosses the wire on `SyncUnit` for the two statuses that are
    /// not stuns, and a renumbered member would repaint somebody else's icon on an older peer.
    /// </summary>
    public enum StatusKind : byte
    {
        None = 0,

        /// <summary>Wind. Drops the slipper in hand and blocks slipper retrieval.</summary>
        Whirled = 1,

        /// <summary>Ice. Half movement speed.</summary>
        Chilled = 2,

        /// <summary>Ice. No movement or interaction (an Ice-element stun).</summary>
        Frozen = 3,

        /// <summary>The taya's tag. No movement or interaction, cannot be removed or resisted.</summary>
        Tagged = 4,

        /// <summary>
        /// Growth (Paete's ultimate, 2026-09-25). No movement; throwing and skills still work
        /// (owner: *"THEY CAN STILL THROW AND USE SKILLS WHILE STUCK BTW theyre js rooted"*). Ends
        /// when they have held Interact for `PaeteRules.BreakFreeHoldSeconds`, when they are
        /// tagged, or when the sentry withers.
        /// </summary>
        Rooted = 5,
    }

    /// <summary>One row of the owner's status table.</summary>
    public sealed class StatusRule
    {
        public StatusKind Kind { get; }
        public string Name { get; }

        /// <summary>The owner's description column, verbatim.</summary>
        public string Description { get; }

        /// <summary>The owner's tooltip column, verbatim. What the HUD prints beside the icon.</summary>
        public string Tooltip { get; }

        public float Seconds { get; }

        /// <summary>Multiplier on movement speed while it runs. 1 = no change.</summary>
        public float SpeedScale { get; }

        public bool BlocksMovement { get; }
        public bool BlocksInteraction { get; }
        public bool BlocksSlipperRetrieval { get; }
        public bool DropsHeldSlipper { get; }

        /// <summary>False for Tagged: *"Cannot be removed"*, so no mash and no cleanse ends it.</summary>
        public bool Removable { get; }

        /// <summary>False for Tagged: *"or be immune to"*, so stun immunity does not stop it.</summary>
        public bool ImmunityApplies { get; }

        public StatusRule(StatusKind kind, string name, string description, string tooltip,
                          float seconds, float speedScale, bool blocksMovement,
                          bool blocksInteraction, bool blocksSlipperRetrieval,
                          bool dropsHeldSlipper, bool removable, bool immunityApplies)
        {
            Kind = kind; Name = name; Description = description; Tooltip = tooltip;
            Seconds = seconds; SpeedScale = speedScale; BlocksMovement = blocksMovement;
            BlocksInteraction = blocksInteraction; BlocksSlipperRetrieval = blocksSlipperRetrieval;
            DropsHeldSlipper = dropsHeldSlipper; Removable = removable; ImmunityApplies = immunityApplies;
        }
    }

    public static class StatusRules
    {
        /// <summary>*"Prevents slipper retrieval for 2.5 seconds."*</summary>
        public const float WhirledSeconds = 2.5f;

        /// <summary>*"Decreases movement speed by 50% for 5 seconds."*</summary>
        public const float ChilledSeconds = 5.0f;
        public const float ChilledSpeedScale = 0.5f;

        /// <summary>*"Prevents movement or interaction for 2.5 seconds."* Glacial Nova's number already.</summary>
        public const float FrozenSeconds = 2.5f;

        /// <summary>At most the sentry's life; the hold or a tag ends it sooner.</summary>
        public const float RootedSeconds = PaeteRules.SentryLifeSeconds;

        /// <summary>*"Prevents movement or interaction for 5 seconds."* The tag's own number.</summary>
        public const float TaggedSeconds = Balance.TagStunTime;

        private static readonly StatusRule[] Table =
        {
            new StatusRule(StatusKind.Whirled, "WHIRLED",
                "Drops slipper if currently in hand. Prevents slipper retrieval for 2.5 seconds.",
                "Disabled Slipper Retrieval",
                WhirledSeconds, 1.0f, blocksMovement: false, blocksInteraction: false,
                blocksSlipperRetrieval: true, dropsHeldSlipper: true, removable: true, immunityApplies: true),
            new StatusRule(StatusKind.Chilled, "CHILLED",
                "Decreases movement speed by 50% for 5 seconds.",
                "Reduced Movement Speed",
                ChilledSeconds, ChilledSpeedScale, blocksMovement: false, blocksInteraction: false,
                blocksSlipperRetrieval: false, dropsHeldSlipper: false, removable: true, immunityApplies: true),
            new StatusRule(StatusKind.Frozen, "FROZEN",
                "Prevents movement or interaction for 2.5 seconds.",
                "Disabled Movement and Interaction",
                FrozenSeconds, 0.0f, blocksMovement: true, blocksInteraction: true,
                blocksSlipperRetrieval: true, dropsHeldSlipper: false, removable: true, immunityApplies: true),
            new StatusRule(StatusKind.Tagged, "TAGGED",
                "Prevents movement or interaction for 5 seconds. Cannot be removed or be immune to.",
                "Disabled Movement and Interaction",
                TaggedSeconds, 0.0f, blocksMovement: true, blocksInteraction: true,
                blocksSlipperRetrieval: true, dropsHeldSlipper: false, removable: false, immunityApplies: false),
            // ⚠️ APPENDED, NOT INSERTED (the enum's note). Its seconds are the sentry's whole life:
            // what actually ends it early is the break-free hold or a tag.
            new StatusRule(StatusKind.Rooted, "ROOTED",
                "Pulled to the sentry and held by the roots. Throwing and skills still work. Hold Interact for 7 seconds to break free; a tag also frees you.",
                "Rooted: Hold Interact to Break Free",
                RootedSeconds, 0.0f, blocksMovement: true, blocksInteraction: false,
                blocksSlipperRetrieval: false, dropsHeldSlipper: false, removable: true, immunityApplies: true),
        };

        public static IReadOnlyList<StatusRule> All => Table;

        public static StatusRule For(StatusKind kind)
        {
            foreach (var rule in Table) if (rule.Kind == kind) return rule;
            return null;
        }

        /// <summary>
        /// ⚠️ OVERLAP IS `Max()`, NEVER ADDITIVE, FOR EVERY STATUS (`CLAUDE.md` § 4). A second
        /// Whirl inside the first refreshes it to its full length; two never add up to five
        /// seconds of a player unable to touch their slipper.
        /// </summary>
        public static float Refresh(float left, float seconds) => Math.Max(left, seconds);
    }

    /// <summary>
    /// ⚠️⚠️ A CARRY: A VELOCITY HELD FOR A SOLVED TIME, THEN LEFT TO DECAY AGAINST `Friction`.
    ///
    /// `CLAUDE.md` § 4: every impulse derives from `Friction`, write the distance and solve the
    /// speed as v^2 / (2 x Friction). A single impulse is capped at `Balance.MaxKnockbackSpeed`
    /// (16 m/s), which buys at most 16^2 / 60 = 4.27 m, and the ability overhaul asks for moves
    /// longer than that: Quick Dash's 5 m and Storm Surge's "very far" (owner, 2026-09-25: *"we want
    /// them to fall off the map or pushed to the edge"*). So the distance is still what is written,
    /// and the arithmetic grows one term: the body is held at `speed` for `SecondsFor` seconds and
    /// then released, and the release tail is exactly the impulse distance the old rule already
    /// used. Total = speed x hold + speed^2 / (2 x Friction).
    /// </summary>
    public static class CarryRules
    {
        /// <summary>The distance a release at this speed slides before `Friction` stops it.</summary>
        public static float TailDistance(float speed) => speed * speed / (2.0f * Balance.Friction);

        /// <summary>How long to hold <paramref name="speed"/> so the whole move covers
        /// <paramref name="distance"/>. Zero when the tail alone already covers it.</summary>
        public static float SecondsFor(float distance, float speed)
        {
            if (speed <= 0.0f) return 0.0f;
            return Math.Max(0.0f, (distance - TailDistance(speed)) / speed);
        }

        /// <summary>The whole distance a carry covers. The inverse of <see cref="SecondsFor"/>.</summary>
        public static float DistanceFor(float speed, float seconds) => speed * Math.Max(0.0f, seconds) + TailDistance(speed);

        /// <summary>The single-impulse speed that slides exactly <paramref name="distance"/>.</summary>
        public static float ImpulseFor(float distance) => (float)Math.Sqrt(2.0 * Balance.Friction * Math.Max(0.0f, distance));
    }
}
