namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Each hero's passive, as the player reads it. Names and wording follow the owner's Wiki
    /// table; numbers match the code that applies the passive (named beside each entry).
    /// Paete's MAKILING'S BLESSING has a name but no rule on the Wiki yet, so it has no entry.
    /// </summary>
    public static class HeroPassives
    {
        public readonly struct Passive
        {
            public readonly string Name, Description;
            /// <summary>A standing kit rule that is not the passive, shown on its own line under it (or null).</summary>
            public readonly string KitRule;
            public Passive(string name, string description, string kitRule = null)
            { Name = name; Description = description; KitRule = kitRule; }
            public bool Exists => !string.IsNullOrEmpty(Name);
        }

        public static Passive For(string heroId)
        {
            switch (heroId)
            {
                // AmihanHeroKit: 1.25x movement for 2.5 s after every cast.
                case "amihan": return new Passive("SECOND WIND",
                    "Amihan gains Second Wind every time she casts an ability, moving 25% faster for 2.5 seconds.");
                // CombatVerbs shove: CheskaHeroKit victims get Chilled.
                case "cheska": return new Passive("CHILLING TOUCH",
                    "Yasmin inflicts Chilled on any player she shoves.");
                // DanteHeroKit.IncomingKnockbackDistanceScale = GeoRules.EarthboundDistanceScale.
                // DanteHeroKit MOUNTAIN FORM: GeoRules.MountainSeconds, MountainThrowScale, MountainHitStun and the launch.
                case "dante": return new Passive("EARTHBOUND",
                    "Dante takes 50% reduced shove distance.",
                    "MOUNTAIN FORM, 30 s after his ultimate: immune, throws 25% faster, a hit knocks the player away.");
                // RafiHeroKit: 1.2x movement for 1.5 s on a genuine own retrieve.
                case "rafi": return new Passive("BACKWASH",
                    "Retrieving your own thrown slipper grants 20% movement speed for 1.5 seconds, once per throw. Dropping and picking it up again does not count.");
                // ZackHeroKit.OnObjectiveAwarded: 5 s off each non-ultimate cooldown per point.
                case "zack": return new Passive("AMPED-UP",
                    "Isagani reduces his ability cooldowns by 5 seconds each time he gains an objective point.");
                // SeanHeroKit Steady Ember: 1.25x charge rate for 4 s after a genuine own retrieve.
                case "sean": return new Passive("HOT STREAK",
                    "Retrieving your own thrown slipper makes your next throw charge 25% faster, once within 4 seconds. Maximum power is unchanged.");
                // CharacterMotor.DeadWeightSpeedScale (owner 2026-10-16, replacing KURO!); NemuHeroKit.ShareBasicCooldown.
                case "nemu": return new Passive("DEAD WEIGHT",
                    "Rivals near any of Nemu's ghosts move 10% slower.",
                    "Her abilities share one cooldown.");
                // CharacterMotor.Voodoo: the curse mark's speed exchange.
                case "phaister": return new Passive("VOODOO",
                    "While one of Phaister's curse marks is active, the marked player moves 10% slower and Phaister moves 10% faster.");
                default: return default;
            }
        }
    }
}
