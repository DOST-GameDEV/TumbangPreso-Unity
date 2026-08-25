using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// A tall column of hero-coloured light standing off the player who just cast an ultimate.
    ///
    /// ⚠️⚠️ IT IS THE ONLY PART OF AN ULTIMATE'S PRESENTATION THAT IS NOT LOCAL, AND THAT IS
    /// WHAT IT IS FOR. Everything else `HeroAbilitySystem.PlayUltimatePresentation` does is a
    /// camera punch, a chromatic pulse and a hitstop, all of which happen on the machine that is
    /// looking. From another seat an ultimate was a flash and then consequences, identical to a
    /// skill, which for a power costing 90 to 150 earned points is the wrong reading.
    ///
    /// `docs/VISION.md` § 1.1 says Hero Strike exists for *"combos, timing, counterplay, reading
    /// which ultimate is banked"*. Counterplay needs something to read, and there was nothing at
    /// range that said an ultimate had gone off, let alone whose.
    ///
    /// ⚠️⚠️ IT IS VERTICAL BECAUSE THE FLOOR IS FULL. `docs/VISION.md` § 2 is an entire section
    /// on a 14 by 14 box that already carries four players, four tsinelas, a lata, the chalk and
    /// up to twelve live abilities, and rule 5 requires a mid-fight frame to still show most of
    /// that. Up is the one direction with room left, and a column costs no floor area at all.
    ///
    /// ⚠️ IT IS ALSO WHY IT MUST NOT BE OPAQUE OR WIDE. This stands where a player is standing,
    /// so anything solid hides the caster from the three people who most need to see them right
    /// now. It is a thin ghosted shaft plus a ground flare, and both fade out.
    ///
    /// `docs/Hero_Strike_Balance.md` § 4.3.
    /// </summary>
    public sealed class UltimateColumn : MonoBehaviour
    {
        private const float Life = 0.9f;
        private const float Height = 9.0f;
        private const float Radius = 0.55f;

        private float _left = Life;
        private Transform _shaft;
        private Transform _flare;
        private readonly Fade _shaftFade = new Fade();
        private readonly Fade _flareFade = new Fade();

        public static void Raise(Vector3 groundPosition, Color accent)
        {
            var go = new GameObject("UltimateColumn");
            go.transform.position = groundPosition;
            go.AddComponent<UltimateColumn>().Build(accent);
        }

        private void Build(Color accent)
        {
            // The shaft. A cylinder primitive is 2 m tall at scale 1, so the Y scale is half the
            // height wanted, and it is lifted by half again to stand ON the ground rather than
            // through it.
            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Shaft";
            shaft.transform.SetParent(transform, false);
            shaft.transform.localScale = new Vector3(Radius * 2.0f, Height * 0.5f, Radius * 2.0f);
            shaft.transform.localPosition = new Vector3(0.0f, Height * 0.5f, 0.0f);
            VfxMaterial.Ghost(shaft.GetComponent<Renderer>(),
                              new Color(accent.r, accent.g, accent.b, 0.42f), 1.6f);
            VfxMaterial.StripCollider(shaft);
            _shaft = shaft.transform;

            // The ground flare, so the column is anchored to a place and not floating. Kept
            // under 2.0 m so it cannot be mistaken for a hazard telegraph: nothing about this
            // is dangerous, and a ring the size of a skill's footprint would say otherwise.
            var flare = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flare.name = "Flare";
            flare.transform.SetParent(transform, false);
            flare.transform.localScale = new Vector3(1.9f, 0.02f, 1.9f);
            flare.transform.localPosition = new Vector3(0.0f, 0.04f, 0.0f);
            VfxMaterial.Ghost(flare.GetComponent<Renderer>(),
                              new Color(accent.r, accent.g, accent.b, 0.55f), 1.9f);
            VfxMaterial.StripCollider(flare);
            _flare = flare.transform;

            var lightGo = new GameObject("ColumnLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0.0f, 2.2f, 0.0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = accent;
            light.range = 9.0f;
            light.intensity = 5.0f;
            light.shadows = LightShadows.None;
        }

        private void Update()
        {
            _left -= Time.deltaTime;
            if (_left <= 0.0f)
            {
                Destroy(gameObject);
                return;
            }

            float t = 1.0f - _left / Life;

            // ⚠️ IT GROWS UPWARD FAST AND THINS AS IT GOES, rather than fading in place. A shaft
            // that simply appears and dims reads as a graphical artefact; one that shoots up
            // reads as a thing being released, which is the beat this is here to sell. Same
            // reasoning as `ExplosionVfxAnim`'s `Sqrt(t)` curve and for the same reason.
            if (_shaft != null)
            {
                float rise = Mathf.Sqrt(Mathf.Clamp01(t * 3.0f));
                float taper = Mathf.Lerp(1.0f, 0.35f, t);
                _shaft.localScale = new Vector3(Radius * 2.0f * taper,
                                                Height * 0.5f * rise,
                                                Radius * 2.0f * taper);
                _shaft.localPosition = new Vector3(0.0f, Height * 0.5f * rise, 0.0f);
                _shaftFade.Apply(_shaft.GetComponent<Renderer>(), Mathf.Lerp(0.42f, 0.0f, t * t));
            }

            if (_flare != null)
            {
                float spread = Mathf.Lerp(1.9f, 3.4f, Mathf.Sqrt(t));
                _flare.localScale = new Vector3(spread, 0.02f, spread);
                _flareFade.Apply(_flare.GetComponent<Renderer>(), Mathf.Lerp(0.55f, 0.0f, t));
            }
        }

        /// <summary>
        /// ⚠️ THE EMISSION HAS TO FALL WITH THE ALPHA AND FROM A REMEMBERED BASE. `VfxMaterial`
        /// lights these flatly through `_EmissionColor` so they are not shaded by the arena key
        /// light, and emission is ADDED after the blend: a shaft faded to alpha 0 with its glow
        /// left at full still deposits its colour on the frame and stays visible at zero
        /// opacity. `HeroHazards.Fader` records this being found the hard way on the explosion.
        /// </summary>
        private sealed class Fade
        {
            private Color _base;
            private bool _captured;

            public void Apply(Renderer target, float alpha)
            {
                if (target == null) return;

                var material = target.material;
                bool glows = material.HasProperty("_EmissionColor");

                if (!_captured)
                {
                    if (glows) _base = material.GetColor("_EmissionColor");
                    _captured = true;
                }

                float a = Mathf.Clamp01(alpha);

                var colour = material.color;
                colour.a = a;
                material.color = colour;

                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);

                if (glows)
                    material.SetColor("_EmissionColor",
                        new Color(_base.r, _base.g, _base.b, 1.0f) * a);
            }
        }
    }
}
