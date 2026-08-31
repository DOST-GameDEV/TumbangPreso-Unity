using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// Definition of an ability variant / sidegrade for Hero Strike heroes.
    ///
    /// ⚠️⚠️ UNCHANGED BUDGET LAW (FUTURE.md Phase 10, INSPIRATION.md § 5.4):
    /// Every variant is a strictly budget-neutral sidegrade. A variant increases one parameter
    /// (e.g. area, velocity, duration) while reducing another (e.g. range, windup, knockback).
    /// It must NEVER be a direct upgrade across all dimensions.
    /// </summary>
    public sealed class AbilityVariant
    {
        public string Id { get; set; }
        public string HeroId { get; set; }
        public int Slot { get; set; } // 1: Skill A, 2: Skill B
        public string Name { get; set; }
        public string Description { get; set; }
        public string StatBuff { get; set; }
        public string StatDebuff { get; set; }
        public float PowerModifier { get; set; } // e.g. +0.20
        public float CostModifier { get; set; }  // e.g. -0.20
        public string UnlockChallenge { get; set; }
        public bool UnlockedByDefault { get; set; }

        public AbilityVariant(string id, string heroId, int slot, string name, string description,
            string statBuff, string statDebuff, float powerMod, float costMod, string challenge, bool isDefault = false)
        {
            Id = id;
            HeroId = heroId;
            Slot = slot;
            Name = name;
            Description = description;
            StatBuff = statBuff;
            StatDebuff = statDebuff;
            PowerModifier = powerMod;
            CostModifier = costMod;
            UnlockChallenge = challenge;
            UnlockedByDefault = isDefault;
        }
    }

    public static class HeroLoadoutRules
    {
        public static readonly string[] CanonicalHeroes = { "berto", "sean", "dante", "cheska", "zack", "nemu" };

        private static readonly List<AbilityVariant> Variants = new List<AbilityVariant>
        {
            // ---------------------------------------------------------
            // BERTO (Tank / Defender)
            // ---------------------------------------------------------
            new AbilityVariant("berto.a1.default", "berto", 1, "Barricade Shield", "Deploy a solid wooden protective barrier.", "Standard 3m width", "Standard 5s duration", 0.0f, 0.0f, "Unlocked by default", true),
            new AbilityVariant("berto.a1.bulwark", "berto", 1, "Bulwark Fort", "Wider fortified defense wall.", "+25% Barrier Width", "-20% Barrier Duration", 0.25f, -0.20f, "Block 20 throws as Berto in Practice or matches"),

            new AbilityVariant("berto.a2.default", "berto", 2, "Ground Slam", "Slam the road to send shockwaves.", "Standard 4m radius", "Standard knockback impulse", 0.0f, 0.0f, "Unlocked by default", true),
            new AbilityVariant("berto.a2.quake", "berto", 2, "Shockwave Quake", "Expanded area tremor with softer knockback.", "+30% Shockwave Radius", "-25% Knockback Velocity", 0.30f, -0.25f, "Hit 15 attackers with Ground Slam"),

            // ---------------------------------------------------------
            // SEAN (Scout / Rusher)
            // ---------------------------------------------------------
            new AbilityVariant("sean.a1.default", "sean", 1, "Sprint Burst", "Sudden surge of foot speed.", "Standard speed boost", "Standard 4s duration", 0.0f, 0.0f, "Unlocked by default", true),
            new AbilityVariant("sean.a1.drift", "sean", 1, "Dash Drift", "Higher burst acceleration with shorter duration.", "+20% Burst Velocity", "-25% Boost Duration", 0.20f, -0.25f, "Perform 10 evasive dashes in Practice or matches"),

            new AbilityVariant("sean.a2.default", "sean", 2, "Quick Toss", "Rapid underhand slipper fling.", "Direct linear flight", "Standard release time", 0.0f, 0.0f, "Unlocked by default", true),
            new AbilityVariant("sean.a2.lob", "sean", 2, "Curve Lob", "High-arching throw over obstacles.", "High-Arc Obstacle Clearance", "+0.2s Flight Travel Time", 0.20f, -0.20f, "Knock down the lata 10 times as Sean"),

            // ---------------------------------------------------------
            // DANTE (Shooter / Sniper)
            // ---------------------------------------------------------
            new AbilityVariant("dante.a1.default", "dante", 1, "Sniper Aim", "Focused stance increasing slipper range and velocity.", "Maximum range", "1.2s Aim windup", 0.0f, 0.0f, "Unlocked by default", true),
            new AbilityVariant("dante.a1.snap", "dante", 1, "Quickdraw Snap", "Fast flick-throw with reduced maximum range.", "-35% Windup Time", "-20% Maximum Range", 0.35f, -0.20f, "Score 10 long-range hits as Dante"),

            new AbilityVariant("dante.a2.default", "dante", 2, "Ricochet Bank", "Bank shot bouncing off walls or ground.", "1 Target Ricochet", "Standard bounce velocity", 0.0f, 0.0f, "Unlocked by default", true),
            new AbilityVariant("dante.a2.shrapnel", "dante", 2, "Split Shrapnel", "Slipper splits on impact into twin deflections.", "Twin Bounce Splinters", "-25% Knockdown Impulse", 0.25f, -0.25f, "Execute 8 bank shots off walls"),

            // ---------------------------------------------------------
            // CHESKA (Playmaker / Trickster)
            // ---------------------------------------------------------
            new AbilityVariant("cheska.a1.default", "cheska", 1, "Decoy Slipper", "Toss a holographic chalk decoy slipper.", "1 Long Decoy (6s)", "Standard lure pull", 0.0f, 0.0f, "Unlocked by default", true),
            new AbilityVariant("cheska.a1.mirage", "cheska", 1, "Mirror Mirage", "Deploy dual quick decoys with shorter lifespan.", "2 Simultaneous Decoys", "-50% Decoy Linger Time", 0.50f, -0.50f, "Distract the guard 10 times as Cheska"),

            new AbilityVariant("cheska.a2.default", "cheska", 2, "Grapple Tether", "Reel back slipper or zip to boundary.", "Standard 8m reach", "Standard reel speed", 0.0f, 0.0f, "Unlocked by default", true),
            new AbilityVariant("cheska.a2.zip", "cheska", 2, "Zip Whip", "Fast-action retractor with shorter line reach.", "+25% Reel Speed", "-20% Tether Reach", 0.25f, -0.20f, "Retrieve slipper from inside the circle 10 times"),

            // ---------------------------------------------------------
            // ZACK (Brawler / Smasher)
            // ---------------------------------------------------------
            new AbilityVariant("zack.a1.default", "zack", 1, "Heavy Smash", "Crushing overhead slipper slam.", "High base damage", "Standard windup", 0.0f, 0.0f, "Unlocked by default", true),
            new AbilityVariant("zack.a1.overhead", "zack", 1, "Crushing Overhead", "Extended stun hit with a longer recovery telegraph.", "+30% Stun Duration", "+0.25s Windup Telegraph", 0.30f, -0.25f, "Stun opponents 12 times as Zack"),

            new AbilityVariant("zack.a2.default", "zack", 2, "Brace Guard", "Hunker down to resist slipper knockback.", "70% Knockback Reduction", "4s Duration", 0.0f, 0.0f, "Unlocked by default", true),
            new AbilityVariant("zack.a2.parry", "zack", 2, "Counter Parry", "Brief tight parry window granting instant recovery.", "100% Impact Deflection", "-75% Active Guard Window", 0.75f, -0.75f, "Block 10 direct hits in Practice or matches"),

            // ---------------------------------------------------------
            // NEMU (Tactician / Area Control)
            // ---------------------------------------------------------
            new AbilityVariant("nemu.a1.default", "nemu", 1, "Chalk Trap", "Draw a chalk slow zone on the asphalt.", "Standard 3m radius", "40% Movement Slow", 0.0f, 0.0f, "Unlocked by default", true),
            new AbilityVariant("nemu.a1.perimeter", "nemu", 1, "Chalk Perimeter", "Massive chalk boundary zone with milder slow.", "+40% Trap Perimeter Area", "-20% Slow Potency", 0.40f, -0.20f, "Catch 15 opponents in chalk traps as Nemu"),

            new AbilityVariant("nemu.a2.default", "nemu", 2, "Smoke Screen", "Kick up asphalt dust to break line-of-sight.", "Dense 5s Dust Cloud", "Standard dispersal radius", 0.0f, 0.0f, "Unlocked by default", true),
            new AbilityVariant("nemu.a2.flash", "nemu", 2, "Dust Flash", "Instant concussive dust flash with brief duration.", "Instant Flash Blind Burst", "-45% Cloud Duration", 0.45f, -0.45f, "Obscure the lata 8 times with dust")
        };

        public static IReadOnlyList<AbilityVariant> AllVariants => Variants;

        public static List<AbilityVariant> VariantsFor(string heroId, int slot)
        {
            var list = new List<AbilityVariant>();
            string cleanHero = (heroId ?? "").ToLowerInvariant();
            foreach (var v in Variants)
            {
                if (v.HeroId == cleanHero && v.Slot == slot)
                    list.Add(v);
            }
            return list;
        }

        public static AbilityVariant VariantById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return Variants.Find(v => v.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsValidSidegrade(AbilityVariant variant)
        {
            if (variant.UnlockedByDefault) return true;
            // Strict sidegrade check: PowerModifier > 0 must be balanced by CostModifier < 0
            return variant.PowerModifier > 0.0f && variant.CostModifier < 0.0f;
        }
    }
}
