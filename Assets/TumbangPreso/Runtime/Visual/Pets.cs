namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ ALL PETS ARE OFF (owner, 2026-10-11: *"can u also disable all pets for now make it a rule in agents md"*, *"turn off
    /// all pets it doesnt make sense that the ghost is there"*). No roster pet is spawned beside a body, in first person, in the
    /// menus or in a replay. Every place that would draw one asks this switch; a roster entry keeps its `PetModel` so turning
    /// them back on is this one line. The pet's abilities that cannot work without it refuse to cast instead of failing
    /// silently (`NemuHeroKit`'s KURO: FETCH!).
    /// </summary>
    public static class Pets
    {
        public static readonly bool Enabled = false;

        /// <summary>The pet model to spawn, or null while pets are off.</summary>
        public static UnityEngine.GameObject Allow(UnityEngine.GameObject petModel) => Enabled ? petModel : null;
    }
}
