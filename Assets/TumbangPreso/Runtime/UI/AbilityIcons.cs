using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// What KIND of thing an ability is, drawn as a shape rather than spelled out.
    ///
    /// ⚠️⚠️ THE GLYPH DESCRIBES THE SHAPE OF THE EFFECT, NOT THE ELEMENT. Fire, ice and
    /// lightning are already carried by the hero's colour, and a flame icon on a fire hero's
    /// three powers tells the player nothing about which of the three to press. What they need
    /// at a glance is *what it does to the world*: does it put something on the ground, put a
    /// wall in front of me, move me, protect me, or hit everything at once. Two heroes with
    /// completely different fiction share a glyph when they share a job, on purpose.
    ///
    /// ⚠️ ADD A GLYPH RATHER THAN REUSING A NEAR MISS. A wrong icon is worse than a generic
    /// one, because the player trusts it once and then stops trusting all of them.
    /// </summary>
    public enum AbilityGlyph
    {
        /// <summary>A patch of ground that does something to whoever stands on it.</summary>
        Zone,

        /// <summary>A solid thing placed in the world that bodies and slippers cannot pass.</summary>
        Wall,

        /// <summary>Moves the caster. Rushes, dashes, grinds.</summary>
        Dash,

        /// <summary>Protects the caster. Armour, immunity, carapace.</summary>
        Shield,

        /// <summary>Goes off around the caster and hits everything in reach.</summary>
        Burst,

        /// <summary>Sends something away from the caster at a target.</summary>
        Projectile,

        /// <summary>The caster stops being fully present. Phase, ghost, untargetable.</summary>
        Phase,

        /// <summary>Comes down from above onto a place. Slams, strikes, fissures.</summary>
        Slam,

        /// <summary>Changes the tsinelas rather than the world. Overcharge, curve, empower.</summary>
        Empower,
    }

    /// <summary>
    /// Procedural icons for the ability deck, the hold-to-inspect panel and character select.
    ///
    /// ⚠️⚠️ THEY ARE BAKED IN CODE, LIKE EVERY OTHER SURFACE IN THIS UI. `GodotTheme` already
    /// paints every box, border and shadow in the game into a cached texture rather than
    /// shipping PNGs, for the reason its own note gives: a baked file that drifts from the
    /// code that wanted it is indistinguishable from a broken conversion. Icons follow the same
    /// rule, and it also means the ability art is not blocked on the team's art queue
    /// (`CLAUDE.md` § 4a). When real icons land, replace `Bake` and nothing else changes.
    ///
    /// ⚠️ WHITE ON TRANSPARENT, TINTED AT THE USE SITE. One texture per glyph, ever, and the
    /// hero colour arrives through `Image.color`. Baking a coloured icon per hero would be
    /// five times the textures for no visual difference.
    /// </summary>
    public static class AbilityIcons
    {
        private const int Size = 128;

        private static readonly Dictionary<AbilityGlyph, Sprite> Cache =
            new Dictionary<AbilityGlyph, Sprite>();

        public static Sprite For(AbilityGlyph glyph)
        {
            if (Cache.TryGetValue(glyph, out var cached) && cached != null) return cached;

            var sprite = Bake(glyph);
            Cache[glyph] = sprite;
            return sprite;
        }

        /// <summary>
        /// A one-word name for the glyph, for the inspect panel's "what is this" line.
        ///
        /// ⚠️ IT SAYS THE JOB, NOT THE SHAPE. "GROUND ZONE" is useful; "circle" is noise.
        /// </summary>
        public static string LabelFor(AbilityGlyph glyph)
        {
            switch (glyph)
            {
                case AbilityGlyph.Zone: return "GROUND ZONE";
                case AbilityGlyph.Wall: return "BLOCKER";
                case AbilityGlyph.Dash: return "MOBILITY";
                case AbilityGlyph.Shield: return "PROTECTION";
                case AbilityGlyph.Burst: return "AREA BURST";
                case AbilityGlyph.Projectile: return "PROJECTILE";
                case AbilityGlyph.Phase: return "EVASION";
                case AbilityGlyph.Slam: return "FROM ABOVE";
                case AbilityGlyph.Empower: return "TSINELAS BUFF";
                default: return "POWER";
            }
        }

        // ------------------------------------------------------------------ baking

        private static Sprite Bake(AbilityGlyph glyph)
        {
            var pixels = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // Centred, normalised to roughly -1..1
                    float u = (x + 0.5f) / Size * 2.0f - 1.0f;
                    float v = (y + 0.5f) / Size * 2.0f - 1.0f;

                    float a = Coverage(glyph, u, v);
                    pixels[y * Size + x] = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(a));
                }
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "abilityglyph_" + glyph,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            tex.SetPixels(pixels);
            tex.Apply(false, false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f),
                                       100.0f, 0, SpriteMeshType.FullRect);
            sprite.name = tex.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>
        /// Modern tactical vector glyphs with smooth anti-aliasing.
        /// </summary>
        private static float Coverage(AbilityGlyph glyph, float u, float v)
        {
            switch (glyph)
            {
                case AbilityGlyph.Zone:
                    // Concentric tactical ground vortex rings with center diamond core
                    float outerRing = EllipseRing(u, v, 0.85f, 0.48f, 0.11f);
                    float innerRing = EllipseRing(u, v, 0.52f, 0.28f, 0.09f);
                    float coreDiamond = Diamond(u, v, 0.16f);
                    float topMarker = Box(u, v - 0.52f, 0.04f, 0.10f);
                    float botMarker = Box(u, v + 0.52f, 0.04f, 0.10f);
                    return Mathf.Max(Mathf.Max(outerRing, innerRing), Mathf.Max(coreDiamond, Mathf.Max(topMarker, botMarker)));

                case AbilityGlyph.Wall:
                    // 3 tactical reinforced barrier pillars with angled crowns
                    float p1 = BarrierPillar(u + 0.42f, v, 0.15f, 0.62f);
                    float p2 = BarrierPillar(u, v + 0.08f, 0.18f, 0.78f);
                    float p3 = BarrierPillar(u - 0.42f, v, 0.15f, 0.62f);
                    float bar = Box(u, v - 0.12f, 0.65f, 0.06f);
                    return Mathf.Max(Mathf.Max(p1, p2), Mathf.Max(p3, bar));

                case AbilityGlyph.Dash:
                    // Dynamic supersonic dual speed wings
                    float wing1 = AerodynamicChevron(u + 0.24f, v, 0.42f, 0.14f);
                    float wing2 = AerodynamicChevron(u - 0.22f, v, 0.42f, 0.14f);
                    return Mathf.Max(wing1, wing2);

                case AbilityGlyph.Shield:
                    // Stylized angular knight crest shield with inner core notch
                    float shieldOuter = CrestShield(u, v, 0.78f, 0.88f);
                    float shieldHole = CrestShield(u, v + 0.02f, 0.54f, 0.64f);
                    float shieldRim = Sub(shieldOuter, shieldHole);
                    float core = Diamond(u, v - 0.04f, 0.22f);
                    return Mathf.Max(shieldRim, core);

                case AbilityGlyph.Burst:
                    // Radiant 8-point shockwave starburst with glowing flare ring
                    float ring = Ring(u, v, 0.42f, 0.08f);
                    float center = Disc(u, v, 0.22f);
                    float rays = Spokes(u, v, 8, 0.38f, 0.88f, 0.065f);
                    return Mathf.Max(Mathf.Max(ring, center), rays);

                case AbilityGlyph.Projectile:
                    // Kinetic plasma bolt with twin aerodynamic trails
                    float head = Triangle(u - 0.38f, v, 0.32f, 0.55f);
                    float trail1 = Box(u + 0.20f, v + 0.22f, 0.32f, 0.065f);
                    float trail2 = Box(u + 0.28f, v, 0.40f, 0.08f);
                    float trail3 = Box(u + 0.20f, v - 0.22f, 0.32f, 0.065f);
                    return Mathf.Max(head, Mathf.Max(trail1, Mathf.Max(trail2, trail3)));

                case AbilityGlyph.Phase:
                    // Ethereal spirit wisp / dimensional rift portal
                    float wispRing = Ring(u, v, 0.65f, 0.11f);
                    float crescent = Sub(Disc(u + 0.12f, v, 0.44f), Disc(u - 0.18f, v + 0.05f, 0.46f));
                    float innerGlow = Disc(u + 0.10f, v, 0.18f);
                    return Mathf.Max(wispRing, Mathf.Max(crescent, innerGlow));

                case AbilityGlyph.Slam:
                    // Heavy downward seismic impact spike with cracked ground plate
                    float ground = Box(u, v - 0.68f, 0.78f, 0.09f);
                    float groundLeft = Box(u - 0.55f, v - 0.52f, 0.18f, 0.07f);
                    float groundRight = Box(u + 0.55f, v - 0.52f, 0.18f, 0.07f);
                    float spikeShaft = Box(u, v + 0.22f, 0.12f, 0.42f);
                    float spikeHead = Triangle(u, v - 0.18f, 0.38f, 0.42f);
                    return Mathf.Max(Mathf.Max(ground, Mathf.Max(groundLeft, groundRight)),
                                     Mathf.Max(spikeShaft, spikeHead));

                case AbilityGlyph.Empower:
                    // High-voltage overcharged lightning bolt diamond
                    float d1 = Diamond(u, v, 0.72f);
                    float d2 = Diamond(u, v, 0.50f);
                    float dRim = Sub(d1, d2);
                    float bolt1 = LightningSpike(u, v);
                    return Mathf.Max(dRim, bolt1);

                default:
                    return Disc(u, v, 0.6f);
            }
        }

        // ------------------------------------------------------------------ primitives

        private static float Edge(float distance)
        {
            const float feather = 2.5f / Size;
            return Mathf.Clamp01(0.5f - distance / feather);
        }

        private static float Sub(float shape, float hole) => Mathf.Clamp01(shape - hole);

        private static float Disc(float u, float v, float r)
            => Edge(Mathf.Sqrt(u * u + v * v) - r);

        private static float Ring(float u, float v, float r, float thickness)
            => Edge(Mathf.Abs(Mathf.Sqrt(u * u + v * v) - r) - thickness * 0.5f);

        private static float EllipseRing(float u, float v, float rx, float ry, float thickness)
        {
            float d = Mathf.Sqrt((u / rx) * (u / rx) + (v / ry) * (v / ry)) - 1.0f;
            return Edge(Mathf.Abs(d) * ry - thickness * 0.5f);
        }

        private static float Box(float u, float v, float halfW, float halfH)
        {
            float dx = Mathf.Abs(u) - halfW;
            float dy = Mathf.Abs(v) - halfH;
            return Edge(Mathf.Max(dx, dy));
        }

        private static float Diamond(float u, float v, float radius)
        {
            float d = (Mathf.Abs(u) + Mathf.Abs(v)) * 0.7071f - radius;
            return Edge(d);
        }

        private static float AerodynamicChevron(float u, float v, float size, float thickness)
        {
            float forward = u + Mathf.Abs(v) * 0.85f;
            float d = Mathf.Abs(forward - size * 0.5f);
            if (u < -size || u > size * 0.8f) return 0.0f;
            return Edge(d - thickness * 0.5f);
        }

        private static float Triangle(float u, float v, float halfW, float height)
        {
            // Pointing DOWN
            if (v < -height || v > 0.0f) return 0.0f;
            float t = Mathf.InverseLerp(-height, 0.0f, v);
            return Edge(Mathf.Abs(u) - halfW * t);
        }

        private static float BarrierPillar(float u, float v, float halfW, float halfH)
        {
            float body = Box(u, v, halfW, halfH);
            float notch = Triangle(u, v + halfH, halfW * 1.3f, 0.18f);
            return Mathf.Max(body, notch);
        }

        private static float CrestShield(float u, float v, float width, float height)
        {
            if (v > height * 0.5f || v < -height * 0.5f) return 0.0f;
            float t = Mathf.InverseLerp(height * 0.5f, -height * 0.5f, v);
            float curve = 1.0f - Mathf.Pow(t, 1.8f);
            float w = width * 0.5f * Mathf.Max(0.05f, curve);
            return Edge(Mathf.Abs(u) - w);
        }

        private static float LightningSpike(float u, float v)
        {
            float upper = Box(u - 0.08f + v * 0.25f, v - 0.22f, 0.08f, 0.32f);
            float lower = Box(u + 0.08f + v * 0.25f, v + 0.22f, 0.08f, 0.32f);
            float cross = Box(u, v, 0.26f, 0.07f);
            return Mathf.Max(Mathf.Max(upper, lower), cross);
        }

        private static float Spokes(float u, float v, int count, float inner, float outer, float thickness)
        {
            float best = 0.0f;
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.PI * 2.0f * i / count;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                float su = u * cos + v * sin;
                float sv = -u * sin + v * cos;

                float mid = (inner + outer) * 0.5f;
                float half = (outer - inner) * 0.5f;
                best = Mathf.Max(best, Box(su - mid, sv, half, thickness));
            }
            return best;
        }
    }
}
