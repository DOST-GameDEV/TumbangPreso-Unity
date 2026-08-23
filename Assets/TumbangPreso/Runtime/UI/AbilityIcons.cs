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

        // -------------------------------------------------------------------
        // BESPOKE HERO GLYPHS (Unique across all 15 abilities)
        // -------------------------------------------------------------------

        /// <summary>Dante Skill 1: Seismic Stomp (Heavy downward ground slam with shockwaves).</summary>
        DanteStomp,

        /// <summary>Dante Skill 2: Demonic Carapace (Solid heavy armored crest).</summary>
        DanteShield,

        /// <summary>Dante Ultimate: Titan Fissure (Jagged tectonic ground split).</summary>
        DanteFissure,

        /// <summary>Sean Skill 1: Flame Rush (Forward flame rocket chevrons).</summary>
        SeanRush,

        /// <summary>Sean Skill 2: Ignition Cannon (Empowered flaming fireball / throw).</summary>
        SeanIgnite,

        /// <summary>Sean Ultimate: Supernova (High-impact 8-point explosive supernova star).</summary>
        SeanSupernova,

        /// <summary>Cheska Skill 1: Permafrost Sheet (Frosted ice ground zone with crystal shards).</summary>
        CheskaFrostSheet,

        /// <summary>Cheska Skill 2: Ice Barricade (Three crystalline ice pillars).</summary>
        CheskaBarricade,

        /// <summary>Cheska Ultimate: Glacial Nova (6-point radial blizzard snowflake).</summary>
        CheskaNova,

        /// <summary>Zack Skill 1: Bolt Sprint (Dynamic high-speed lightning streak bolt).</summary>
        ZackSprint,

        /// <summary>Zack Skill 2: Static Charge (Electrified spark core with orbital charge arcs).</summary>
        ZackOvercharge,

        /// <summary>Zack Ultimate: Thunderstrike (Overhead thundercloud with downward lightning strike).</summary>
        ZackThunderstrike,

        /// <summary>Nemu Skill 1: Ghost Step (Crescent moon spirit phase).</summary>
        NemuPhase,

        /// <summary>Nemu Skill 2: Astral Projection (Kuro the ghost companion pet silhouette).</summary>
        NemuAstralPet,

        /// <summary>Nemu Ultimate: Seance Void (Gravitational swirling spiral vortex).</summary>
        NemuSeanceVoid,
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
                case AbilityGlyph.Zone:
                case AbilityGlyph.CheskaFrostSheet:
                    return "GROUND ZONE";
                case AbilityGlyph.Wall:
                case AbilityGlyph.CheskaBarricade:
                    return "BLOCKER";
                case AbilityGlyph.Dash:
                case AbilityGlyph.SeanRush:
                case AbilityGlyph.ZackSprint:
                    return "MOBILITY";
                case AbilityGlyph.Shield:
                case AbilityGlyph.DanteShield:
                    return "PROTECTION";
                case AbilityGlyph.Burst:
                case AbilityGlyph.SeanSupernova:
                case AbilityGlyph.CheskaNova:
                    return "AREA BURST";
                case AbilityGlyph.Projectile:
                    return "PROJECTILE";
                case AbilityGlyph.Phase:
                case AbilityGlyph.NemuPhase:
                    return "EVASION";
                case AbilityGlyph.Slam:
                case AbilityGlyph.DanteStomp:
                    return "FROM ABOVE";
                case AbilityGlyph.Empower:
                case AbilityGlyph.SeanIgnite:
                case AbilityGlyph.ZackOvercharge:
                    return "TSINELAS BUFF";
                case AbilityGlyph.DanteFissure:
                    return "SEISMIC CRACK";
                case AbilityGlyph.ZackThunderstrike:
                    return "LIGHTNING STRIKE";
                case AbilityGlyph.NemuAstralPet:
                    return "GHOST COMPANION";
                case AbilityGlyph.NemuSeanceVoid:
                    return "SEANCE VOID";
                default:
                    return "POWER";
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
        /// The glyph shapes, as signed-distance coverage in a -1..1 square.
        /// </summary>
        private static float Coverage(AbilityGlyph glyph, float u, float v)
        {
            switch (glyph)
            {
                case AbilityGlyph.Zone:
                    return EllipseRing(u, v, 0.82f, 0.54f, 0.26f);

                case AbilityGlyph.Wall:
                    return Mathf.Max(Mathf.Max(
                        Box(u + 0.44f, v + 0.16f, 0.18f, 0.50f),
                        Box(u, v - 0.02f, 0.20f, 0.68f)),
                        Box(u - 0.44f, v + 0.16f, 0.18f, 0.50f));

                case AbilityGlyph.Dash:
                    return Mathf.Max(
                        Chevron(u - 0.30f, v, 0.62f, Stroke),
                        Chevron(u + 0.16f, v, 0.62f, Stroke));

                case AbilityGlyph.Shield:
                case AbilityGlyph.DanteShield:
                    return Crest(u, v, 1.36f, 1.72f);

                case AbilityGlyph.Burst:
                    return Mathf.Max(
                        Disc(u, v, 0.26f),
                        Spokes(u, v, 6, 0.40f, 0.94f, Stroke));

                case AbilityGlyph.Projectile:
                    return Mathf.Max(
                        RightTriangle(u - 0.30f, v, 0.44f, 0.60f),
                        Box(u + 0.60f, v, 0.26f, Stroke * 0.55f));

                case AbilityGlyph.Phase:
                case AbilityGlyph.NemuPhase:
                    // A thick crescent with the caster half gone
                    return Mathf.Max(
                        Sub(Disc(u, v, 0.84f), Disc(u - 0.34f, v + 0.05f, 0.76f)),
                        Disc(u - 0.40f, v - 0.14f, 0.17f));

                case AbilityGlyph.Slam:
                case AbilityGlyph.DanteStomp:
                    // Down arrow/stomp slamming onto a ground bar
                    return Mathf.Max(Mathf.Max(
                        Box(u, v - 0.27f, Stroke, 0.45f),
                        DownTriangle(u, v + 0.18f, 0.46f, 0.44f)),
                        Box(u, v + 0.80f, 0.66f, 0.11f));

                case AbilityGlyph.Empower:
                case AbilityGlyph.ZackSprint:
                    return Bolt(u, v);

                case AbilityGlyph.DanteFissure:
                    // Jagged vertical split crack with branching fissures
                    return FissureCrack(u, v);

                case AbilityGlyph.SeanRush:
                    // High-thrust flame chevrons
                    return Mathf.Max(Mathf.Max(
                        Chevron(u - 0.36f, v, 0.68f, Stroke * 1.15f),
                        Chevron(u + 0.08f, v, 0.68f, Stroke * 1.15f)),
                        RightTriangle(u + 0.62f, v, 0.30f, 0.36f));

                case AbilityGlyph.SeanIgnite:
                    // Flaming slipper / rising flame silhouette
                    return FlameBurst(u, v);

                case AbilityGlyph.SeanSupernova:
                    // 8-ray brilliant exploding star
                    return SupernovaStar(u, v);

                case AbilityGlyph.CheskaFrostSheet:
                    // Elliptical frost ring with diagonal crystal facets
                    return FrostSheet(u, v);

                case AbilityGlyph.CheskaBarricade:
                    // 3 sharp crystalline ice pillars
                    return IceBarricade(u, v);

                case AbilityGlyph.CheskaNova:
                    // 6-point snowflake
                    return Snowflake(u, v);

                case AbilityGlyph.ZackOvercharge:
                    // Static charge orb with orbiting electric sparks
                    return StaticOrb(u, v);

                case AbilityGlyph.ZackThunderstrike:
                    // Storm cloud striking lightning down
                    return ThunderstrikeCloud(u, v);

                case AbilityGlyph.NemuAstralPet:
                    // Kuro the ghost companion pet silhouette
                    return KuroGhostPet(u, v);

                case AbilityGlyph.NemuSeanceVoid:
                    // Swirling spiral seance vortex
                    return SpiralVoid(u, v);

                default:
                    return Disc(u, v, 0.62f);
            }
        }

        // ------------------------------------------------------------------ primitives

        /// <summary>
        /// The one stroke weight in the set.
        ///
        /// ⚠️⚠️ 0.16 OF THE HALF-SQUARE, WHICH IS ABOUT 10 OF 128 TEXTURE PIXELS AND 3 OF THE
        /// 40 SCREEN PIXELS A DECK GLYPH ACTUALLY OCCUPIES. Anything thinner disappears, and
        /// that is not a guess: the previous set ran 0.06 to 0.09 and every glyph in it was a
        /// smudge. If a new glyph "needs" a finer line, it needs fewer parts instead.
        /// </summary>
        private const float Stroke = 0.16f;

        private static float Edge(float distance)
        {
            const float feather = 2.5f / Size;
            return Mathf.Clamp01(0.5f - distance / feather);
        }

        private static float Sub(float shape, float hole) => Mathf.Clamp01(shape - hole);

        private static float Disc(float u, float v, float r)
            => Edge(Mathf.Sqrt(u * u + v * v) - r);

        private static float EllipseDisc(float u, float v, float rx, float ry)
        {
            float d = Mathf.Sqrt((u / rx) * (u / rx) + (v / ry) * (v / ry)) - 1.0f;
            return Edge(d * ry);
        }

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

        /// <summary>
        /// A capsule between two points.
        /// </summary>
        private static float Segment(float u, float v, float ax, float ay, float bx, float by,
                                     float halfThickness)
        {
            float pax = u - ax, pay = v - ay;
            float bax = bx - ax, bay = by - ay;

            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;

            return Edge(Mathf.Sqrt(dx * dx + dy * dy) - halfThickness);
        }

        private static float Bolt(float u, float v)
        {
            float upper = Segment(u, v, 0.20f, 0.86f, -0.26f, 0.08f, Stroke * 0.85f);
            float cross = Segment(u, v, -0.26f, 0.08f, 0.20f, 0.02f, Stroke * 0.85f);
            float lower = Segment(u, v, 0.20f, 0.02f, -0.16f, -0.86f, Stroke * 0.85f);
            return Mathf.Max(upper, Mathf.Max(cross, lower));
        }

        /// <summary>A fat chevron pointing right, of the given vertical span.</summary>
        private static float Chevron(float u, float v, float halfSpan, float thickness)
        {
            if (Mathf.Abs(v) > halfSpan) return 0.0f;

            float leg = u + Mathf.Abs(v) * 0.9f;
            return Edge(Mathf.Abs(leg) - thickness * 0.5f);
        }

        private static float DownTriangle(float u, float v, float halfW, float height)
        {
            if (v < -height || v > 0.0f) return 0.0f;

            float t = Mathf.InverseLerp(-height, 0.0f, v);
            return Edge(Mathf.Abs(u) - halfW * t);
        }

        private static float RightTriangle(float u, float v, float halfH, float length)
        {
            if (u < -length || u > 0.0f) return 0.0f;

            float t = Mathf.InverseLerp(-length, 0.0f, u);
            return Edge(Mathf.Abs(v) - halfH * (1.0f - t));
        }

        /// <summary>A solid shield crest: straight shoulders, tapering to a point.</summary>
        private static float Crest(float u, float v, float width, float height)
        {
            if (v > height * 0.5f || v < -height * 0.5f) return 0.0f;

            float t = Mathf.InverseLerp(height * 0.5f, -height * 0.5f, v);
            float curve = 1.0f - Mathf.Pow(t, 4.0f);
            return Edge(Mathf.Abs(u) - width * 0.5f * Mathf.Max(0.02f, curve));
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

                float su = u * cos + v * sin;
                float sv = -u * sin + v * cos;

                best = Mathf.Max(best, Segment(su, sv, inner, 0.0f, outer, 0.0f, thickness * 0.5f));
            }

            return best;
        }

        // ------------------------------------------------------------------ bespoke helpers

        private static float FissureCrack(float u, float v)
        {
            float s1 = Segment(u, v, 0.05f, 0.85f, -0.15f, 0.20f, Stroke * 0.9f);
            float s2 = Segment(u, v, -0.15f, 0.20f, 0.15f, -0.25f, Stroke * 0.9f);
            float s3 = Segment(u, v, 0.15f, -0.25f, -0.05f, -0.85f, Stroke * 0.9f);
            float b1 = Segment(u, v, -0.15f, 0.20f, -0.65f, 0.45f, Stroke * 0.75f);
            float b2 = Segment(u, v, 0.15f, -0.25f, 0.65f, -0.40f, Stroke * 0.75f);
            return Mathf.Max(Mathf.Max(s1, s2), Mathf.Max(s3, Mathf.Max(b1, b2)));
        }

        private static float FlameBurst(float u, float v)
        {
            float body = EllipseDisc(u, v + 0.18f, 0.45f, 0.55f);
            float tip = DownTriangle(u, v - 0.42f, 0.32f, 0.65f);
            float leftSpur = Segment(u, v, -0.25f, -0.05f, -0.52f, 0.30f, Stroke * 0.85f);
            float rightSpur = Segment(u, v, 0.25f, -0.05f, 0.52f, 0.25f, Stroke * 0.85f);
            return Mathf.Max(Mathf.Max(body, tip), Mathf.Max(leftSpur, rightSpur));
        }

        private static float SupernovaStar(float u, float v)
        {
            float core = Disc(u, v, 0.28f);
            float s4 = Spokes(u, v, 4, 0.24f, 0.92f, Stroke * 1.25f);
            float d4 = Spokes(u * 0.7071f - v * 0.7071f, u * 0.7071f + v * 0.7071f, 4, 0.24f, 0.72f, Stroke * 0.85f);
            return Mathf.Max(core, Mathf.Max(s4, d4));
        }

        private static float FrostSheet(float u, float v)
        {
            float ring = EllipseRing(u, v, 0.85f, 0.50f, Stroke * 1.1f);
            float shard1 = Box(u + 0.25f, v, 0.10f, 0.28f);
            float shard2 = Box(u - 0.25f, v, 0.10f, 0.28f);
            float centerDot = Disc(u, v, 0.16f);
            return Mathf.Max(Mathf.Max(ring, shard1), Mathf.Max(shard2, centerDot));
        }

        private static float IceBarricade(float u, float v)
        {
            float c1 = Box(u, v - 0.05f, 0.18f, 0.68f);
            float t1 = DownTriangle(u, v - 0.73f, 0.18f, 0.22f);
            float l1 = Box(u + 0.44f, v + 0.15f, 0.16f, 0.48f);
            float lt1 = DownTriangle(u + 0.44f, v - 0.33f, 0.16f, 0.18f);
            float r1 = Box(u - 0.44f, v + 0.15f, 0.16f, 0.48f);
            float rt1 = DownTriangle(u - 0.44f, v - 0.33f, 0.16f, 0.18f);
            return Mathf.Max(Mathf.Max(c1, t1), Mathf.Max(Mathf.Max(l1, lt1), Mathf.Max(r1, rt1)));
        }

        private static float Snowflake(float u, float v)
        {
            float rays = Spokes(u, v, 6, 0.0f, 0.90f, Stroke * 0.85f);
            float ring = EllipseRing(u, v, 0.38f, 0.38f, Stroke * 0.75f);
            float center = Disc(u, v, 0.18f);
            return Mathf.Max(Mathf.Max(rays, ring), center);
        }

        private static float StaticOrb(float u, float v)
        {
            float core = Disc(u, v, 0.30f);
            float orbit = EllipseRing(u, v, 0.72f, 0.72f, Stroke * 0.85f);
            float n1 = Disc(u - 0.62f, v, 0.14f);
            float n2 = Disc(u + 0.62f, v, 0.14f);
            float n3 = Disc(u, v - 0.62f, 0.14f);
            float n4 = Disc(u, v + 0.62f, 0.14f);
            return Mathf.Max(Mathf.Max(core, orbit), Mathf.Max(Mathf.Max(n1, n2), Mathf.Max(n3, n4)));
        }

        private static float ThunderstrikeCloud(float u, float v)
        {
            // Storm cloud at the top (v < 0 in Unity texture coords is bottom, v > 0 is top)
            float c1 = Disc(u - 0.32f, v - 0.38f, 0.28f);
            float c2 = Disc(u + 0.32f, v - 0.38f, 0.28f);
            float c3 = Disc(u, v - 0.46f, 0.34f);
            float cloud = Mathf.Max(c1, Mathf.Max(c2, c3));

            // Heavy lightning bolt striking down from cloud base
            float boltTop = Segment(u, v, 0.08f, 0.20f, -0.22f, -0.22f, Stroke * 0.85f);
            float boltCross = Segment(u, v, -0.22f, -0.22f, 0.14f, -0.26f, Stroke * 0.85f);
            float boltBot = Segment(u, v, 0.14f, -0.26f, -0.06f, -0.88f, Stroke * 0.85f);
            float strike = Mathf.Max(boltTop, Mathf.Max(boltCross, boltBot));

            return Mathf.Max(cloud, strike);
        }

        private static float KuroGhostPet(float u, float v)
        {
            // Head and body
            float head = Disc(u, v - 0.08f, 0.46f);
            float earL = DownTriangle(u - 0.26f, v - 0.42f, 0.15f, 0.24f);
            float earR = DownTriangle(u + 0.26f, v - 0.42f, 0.15f, 0.24f);
            float body = Box(u, v + 0.18f, 0.46f, 0.26f);
            float lobes = Mathf.Max(
                Disc(u - 0.30f, v + 0.44f, 0.16f),
                Mathf.Max(Disc(u, v + 0.44f, 0.16f), Disc(u + 0.30f, v + 0.44f, 0.16f)));

            float ghost = Mathf.Max(head, Mathf.Max(body, Mathf.Max(lobes, Mathf.Max(earL, earR))));

            // Cut out cute ghost eyes
            float eyeL = Disc(u - 0.16f, v - 0.10f, 0.09f);
            float eyeR = Disc(u + 0.16f, v - 0.10f, 0.09f);

            return Sub(ghost, Mathf.Max(eyeL, eyeR));
        }

        private static float SpiralVoid(float u, float v)
        {
            float core = Disc(u, v, 0.22f);
            float r1 = EllipseRing(u, v, 0.50f, 0.50f, Stroke * 0.70f);
            float r2 = EllipseRing(u, v, 0.82f, 0.82f, Stroke * 0.85f);
            float spiral1 = Segment(u, v, 0.0f, 0.20f, 0.65f, 0.45f, Stroke * 0.75f);
            float spiral2 = Segment(u, v, 0.0f, -0.20f, -0.65f, -0.45f, Stroke * 0.75f);
            return Mathf.Max(core, Mathf.Max(Mathf.Max(r1, r2), Mathf.Max(spiral1, spiral2)));
        }
    }
}

