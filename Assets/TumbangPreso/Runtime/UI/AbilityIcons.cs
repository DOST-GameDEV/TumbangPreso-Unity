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
        private const int Size = 64;

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
                    // Centred, normalised to roughly -1..1 so every shape below can be written
                    // in the same coordinates whatever Size happens to be.
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
        /// ⚠️ EVERY SHAPE IS AN ANTI-ALIASED DISTANCE TEST, NOT A PIXEL PLOT. A hard in/out test
        /// bakes a 64 px icon with staircase edges that survive being scaled to a 44-unit tile
        /// and read as a compression artefact. `Edge` softens across roughly one pixel.
        /// </summary>
        private static float Coverage(AbilityGlyph glyph, float u, float v)
        {
            switch (glyph)
            {
                case AbilityGlyph.Zone:
                    // A ring seen flat on the ground: a wide ellipse outline with a dot in it.
                    return Mathf.Max(EllipseRing(u, v, 0.82f, 0.46f, 0.13f),
                                     Disc(u, v, 0.14f));

                case AbilityGlyph.Wall:
                    // A slab with two mortar lines, so it cannot be read as a plain box.
                    return Sub(Box(u, v, 0.72f, 0.52f),
                               Mathf.Max(Box(u, v + 0.04f, 0.74f, 0.035f),
                                         Box(u - 0.02f, v - 0.28f, 0.035f, 0.24f)));

                case AbilityGlyph.Dash:
                    // Two chevrons. Motion reads as repetition more than as an arrowhead.
                    return Mathf.Max(Chevron(u + 0.30f, v, 0.34f, 0.14f),
                                     Chevron(u - 0.22f, v, 0.34f, 0.14f));

                case AbilityGlyph.Shield:
                    return Shield(u, v);

                case AbilityGlyph.Burst:
                    // A core with six spokes.
                    return Mathf.Max(Disc(u, v, 0.26f), Spokes(u, v, 6, 0.34f, 0.86f, 0.085f));

                case AbilityGlyph.Projectile:
                    // A head with a speed tail behind it.
                    return Mathf.Max(Disc(u - 0.34f, v, 0.28f),
                            Mathf.Max(Box(u + 0.22f, v, 0.34f, 0.075f),
                            Mathf.Max(Box(u + 0.34f, v - 0.32f, 0.26f, 0.065f),
                                      Box(u + 0.34f, v + 0.32f, 0.26f, 0.065f))));

                case AbilityGlyph.Phase:
                    // A solid body and its offset outline: here, and also not here.
                    return Mathf.Max(Sub(Disc(u + 0.20f, v, 0.44f), Disc(u - 0.16f, v, 0.46f)),
                                     Ring(u - 0.16f, v, 0.44f, 0.085f));

                case AbilityGlyph.Slam:
                    // An arrow coming down onto a ground line.
                    return Mathf.Max(Box(u, v + 0.72f, 0.70f, 0.10f),
                            Mathf.Max(Box(u, v - 0.30f, 0.11f, 0.44f),
                                      Triangle(u, v + 0.18f, 0.34f, 0.30f)));

                case AbilityGlyph.Empower:
                    // A chevron stack pointing up: the same thing, but more of it.
                    return Mathf.Max(Chevron(v - 0.34f, u, 0.34f, 0.13f),
                            Mathf.Max(Chevron(v + 0.04f, u, 0.34f, 0.13f),
                                      Chevron(v + 0.42f, u, 0.34f, 0.13f)));

                default:
                    return Disc(u, v, 0.6f);
            }
        }

        // ------------------------------------------------------------------ primitives

        /// <summary>One pixel of softness, in the normalised space every shape uses.</summary>
        private static float Edge(float distance)
        {
            const float feather = 2.0f / Size;
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

        /// <summary>A "&gt;" shape: two arms of a right angle, open to the left.</summary>
        private static float Chevron(float u, float v, float size, float thickness)
        {
            float d = Mathf.Abs(Mathf.Abs(v) - (u + size)) * 0.7071f;
            float inside = (u + size >= Mathf.Abs(v) - thickness) && u <= 0.05f ? d : 1.0f;
            if (u < -size - thickness || u > 0.12f) return 0.0f;
            return Edge(inside - thickness * 0.5f);
        }

        private static float Triangle(float u, float v, float halfW, float height)
        {
            // Points DOWN: widest at the top, tip at -height.
            if (v < -height || v > 0.0f) return 0.0f;
            float t = Mathf.InverseLerp(-height, 0.0f, v);
            return Edge(Mathf.Abs(u) - halfW * t);
        }

        private static float Shield(float u, float v)
        {
            // Flat across the top, curving into a point at the bottom.
            if (v > 0.72f || v < -0.86f) return 0.0f;
            float t = Mathf.InverseLerp(0.72f, -0.86f, v);
            float halfW = Mathf.Lerp(0.62f, 0.0f, t * t);
            return Edge(Mathf.Abs(u) - halfW);
        }

        private static float Spokes(float u, float v, int count, float inner, float outer,
                                    float thickness)
        {
            float best = 0.0f;
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.PI * 2.0f * i / count;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                // Rotate the point into the spoke's own frame, then it is just a box.
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
