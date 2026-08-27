using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Takes the heat out of a volcanic surface over the life of the zone that owns it.
    ///
    /// ⚠️⚠️ THIS IS THE MOTION CHANNEL, AND `Hero_Strike_Balance.md` § 8.5 ITEM 2 IS THE ENTRY
    /// IT CLOSES FOR DANTE: *"a spent effect and a live one look identical, so a player cannot
    /// tell whether a patch of ice is about to expire. That is a real gameplay read and it is
    /// free."* Seismic Stomp leaves a 4.0 s decal on the court and, until this, second 0.2 and
    /// second 3.9 were the same picture. A player deciding whether to cross it had nothing to
    /// read but their own count.
    ///
    /// ⚠️ ROCK GOING OUT IS THE MOST LEGIBLE POSSIBLE VERSION OF THAT READ, AND IT IS THE ONLY
    /// ONE THAT COSTS NOTHING. The alternative everybody reaches for is fading the whole effect,
    /// which says "this is being deleted" rather than "this is expiring", and on ground it is
    /// worse than that: a half-transparent fracture is a fracture you can see the road through.
    /// § 8.5 item 2 already names the rule for the auras — *"fade the rim, not the whole
    /// thing"* — and this is that rule where the rim is the magma.
    ///
    /// ⚠️ THE CRUST IS DELIBERATELY LEFT ALONE. Cooled basalt does not go anywhere, and the
    /// street keeping its scar for the four seconds the ability is on it is `docs/TODO.md` § 27.4's
    /// motif: displacement is the one element whose signature is that you can see where the
    /// fight was afterwards. What ends is the glow, not the damage.
    ///
    /// ⚠️ IT IS NOT AN `IVfxTimeline`, AND THAT IS A DECISION RATHER THAN AN OMISSION.
    /// `VfxTimeline.StepAll` winds EVERY implementer in the scene to the same fraction of its
    /// own life, and `AbilityShowcaseProbe.Solo` calls it at 0.35. The probe spawns the lava
    /// decal with a 60 s life so it can be photographed at all, so implementing the interface
    /// would wind this to 21 seconds of cooling and photograph Dante's stomp as a cold rock in
    /// every capture from here on. A persistent zone's age is not the thing those frames are
    /// for; the transients are, and they have their own implementers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VolcanicCooling : MonoBehaviour
    {
        /// <summary>How long the zone lives. Cooling completes with it.</summary>
        public float Seconds = 4.0f;

        /// <summary>
        /// How much of the life is spent at full heat before it starts going out.
        ///
        /// ⚠️ A HOLD, BECAUSE A HAZARD MUST NOT LOOK SPENT WHILE IT IS STILL DANGEROUS. Dante's
        /// decal is decoration and this is only a look, but the same component is the obvious
        /// thing to hang on a zone that does resolve, and a glow that starts dropping on frame
        /// one would teach a player to walk into live ground. The same shape as the aura curve
        /// in `docs/TODO.md` § 8 item 4: hold, then fall.
        /// </summary>
        public float HoldFraction = 0.45f;

        private Renderer[] _renderers;
        private float _elapsed;
        private static readonly int CoolId = Shader.PropertyToID("_Cool");

        /// <summary>
        /// Attach to a spawned zone and take its children with it.
        ///
        /// ⚠️ THE RENDERERS ARE COLLECTED ONCE, AT ATTACH. Every piece of one of these zones is
        /// created by its spawner before this is added, and nothing is added later; walking the
        /// hierarchy every frame would be `GetComponentsInChildren` in an `Update` on up to
        /// twenty renderers, which is the shape of allocation `docs/TODO.md` § 15 measured
        /// costing the 6x probe an eighth of its frames.
        /// </summary>
        public static VolcanicCooling Attach(GameObject go, float seconds, float hold = 0.45f)
        {
            if (go == null) return null;

            var cooling = go.AddComponent<VolcanicCooling>();
            cooling.Seconds = seconds;
            cooling.HoldFraction = hold;
            cooling._renderers = go.GetComponentsInChildren<Renderer>(true);
            return cooling;
        }

        private void Update()
        {
            if (_renderers == null || Seconds <= 0.0f) return;

            _elapsed += Time.deltaTime;

            float life = Mathf.Clamp01(_elapsed / Seconds);
            float hold = Mathf.Clamp01(HoldFraction);

            // Zero through the hold, then a smooth ramp to fully out at the end of the life.
            float cool = life <= hold
                ? 0.0f
                : Mathf.SmoothStep(0.0f, 1.0f, (life - hold) / Mathf.Max(0.001f, 1.0f - hold));

            foreach (var r in _renderers)
            {
                if (r == null) continue;

                // ⚠️ `sharedMaterial`, BECAUSE `VfxMaterial.Volcanic` ALREADY BUILT A MATERIAL
                // PER RENDERER AND `VfxRenderTag` OWNS IT. Touching `.material` here would make
                // Unity clone it a second time, which both leaks the clone past the tag that is
                // supposed to free it and writes the cool value into a copy nothing draws.
                var m = r.sharedMaterial;
                if (m == null || !m.HasProperty(CoolId)) continue;

                m.SetFloat(CoolId, cool);
            }
        }
    }
}
