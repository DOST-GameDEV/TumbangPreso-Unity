using TumbangPreso.UI;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ AMIHAN'S KIT IS A PLACEHOLDER, AND DELIBERATELY DOES NOTHING (owner, 2026-09-25: "dont
    /// make skills yet js put placeholders"). She is the eighth hero, from Vigan, a wind character;
    /// her model, lore and roster row exist so she can be picked, played and seen on every screen,
    /// but no skill of hers has been designed. Each slot is a real <see cref="HeroAbility"/> so the
    /// HUD, the loadout screen, the cooldown dial and the network all treat her like any hero, and
    /// pressing one costs its cooldown and changes nothing in the world. Nothing here is a design
    /// decision about her kit: the glyphs are the generic ones and the names say PLACEHOLDER.
    ///
    /// When her skills are designed, replace the three abilities below (and her loadout rows in
    /// `HeroLoadout.cs`, her lines in `HeroLines.cs`), the way `RafiHeroKit` is built.
    /// Brief: ArtSource/amihan/concept-20260925/design-brief.md.
    /// </summary>
    public sealed class AmihanHeroKit : HeroKit
    {
        public override float UltimateCost => 16;

        public AmihanHeroKit() : base("amihan", "AMIHAN")
        {
            Skill1 = new Placeholder("amihan_skill1", "PLACEHOLDER 1", 8, AbilityGlyph.Dash);
            Skill2 = new Placeholder("amihan_skill2", "PLACEHOLDER 2", 10, AbilityGlyph.Zone);
            Ultimate = new Placeholder("amihan_ultimate", "PLACEHOLDER ULT", 0, AbilityGlyph.Burst);
        }

        private sealed class Placeholder : HeroAbility
        {
            public Placeholder(string id, string name, float cooldown, AbilityGlyph glyph)
                : base(id, name, "Not designed yet. Pressing it does nothing.", cooldown,
                       glyph: glyph, summary: "Not designed yet.") { }

            protected override void OnActivate(AbilityContext ctx) { }
        }
    }
}
