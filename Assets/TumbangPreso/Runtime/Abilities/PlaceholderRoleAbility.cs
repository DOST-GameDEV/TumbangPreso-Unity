using TumbangPreso.UI;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️ A ROLE SLOT WAITING FOR ITS DESIGN (ABILITY-2, owner 2026-09-26: *"No idea for pyro and hydro rework
    /// yet"*, *"DOnt hhjave idea for sean and zack yet"*, and on what they do meanwhile: placeholders).
    /// Sean, Zack and Rafi are on the new four-slot shape so the screens, the deck and the role swap treat
    /// every hero alike; their existing second skill stays as the ATTACKING slot (each was already an
    /// attacker's power), and the DEFENDING slot is this: it casts, costs its cooldown and does nothing,
    /// and says so. Replace it when the owner designs the kit.
    /// </summary>
    public sealed class PlaceholderRoleAbility : HeroAbility
    {
        public PlaceholderRoleAbility(string id, string heroName, AbilityGlyph glyph)
            : base(id, "COMING SOON",
                   "Defending. " + heroName + "'s defending skill is still being designed. For now it does nothing.",
                   10.0f, 0.0f, glyph,
                   summary: "Being designed. Does nothing yet.") { }
    }
}
